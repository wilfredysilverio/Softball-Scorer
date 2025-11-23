using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Scoreboard.Web.Dtos;
using Scoreboard.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using System.Text.Json.Serialization;

namespace Scoreboard.Web.Servicios.Marcador
{
    public class MarcadorService : IMarcadorService
    {
        private readonly ContextoMarcador _db;
        private readonly ILogger<MarcadorService>? _logger;
        private readonly IHubContext<MarcadorHub>? _hub;

        public MarcadorService(ContextoMarcador db, ILogger<MarcadorService>? logger = null, IHubContext<MarcadorHub>? hub = null)
        {
            _db = db;
            _logger = logger;
            _hub = hub;
        }

        public async Task IniciarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            // Validaciones de estado: idempotente si ya está en curso; no permitir iniciar si finalizado
            if (partido.Estado == EstadoPartido.EnCurso)
            {
                // Nada que hacer (idempotente)
                return;
            }
            if (partido.Estado == EstadoPartido.Finalizado)
                throw new InvalidOperationException("No se puede iniciar un partido finalizado.");

            // Cargar lineups si existen (opcional para compatibilidad)
            var lineupCasa = await _db.Lineups.AsNoTracking().Where(l => l.PartidoId == partidoId && l.EquipoId == partido.EquipoCasaId).OrderBy(l => l.Orden).ToListAsync();
            var lineupVisita = await _db.Lineups.AsNoTracking().Where(l => l.PartidoId == partidoId && l.EquipoId == partido.EquipoVisitaId).OrderBy(l => l.Orden).ToListAsync();

            // Establecer valores iniciales segun requerimiento
            partido.Estado = EstadoPartido.EnCurso;
            partido.EntradaActual = 1;      // Primera entrada
            partido.Mitad = MitadEntrada.Alta; // Inicia alta normalmente
            partido.Outs = 0;               // Sin outs
            partido.B1 = partido.B2 = partido.B3 = false; // Bases limpias
            // Inicializar índices si hay lineups definidos
            if (lineupCasa.Count > 0) partido.IndexBateadorCasa = 0; else partido.IndexBateadorCasa = null;
            if (lineupVisita.Count > 0) partido.IndexBateadorVisita = 0; else partido.IndexBateadorVisita = null; // visitante batea primero cuando toque
            await _db.SaveChangesAsync();

            // Broadcast: notificar para que los clientes refresquen su marcador
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
        }

        public async Task SuspenderPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso)
                throw new InvalidOperationException("Solo se puede suspender un partido en progreso.");
            partido.Estado = EstadoPartido.Suspendido;
            await _db.SaveChangesAsync();
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
        }

        public async Task ReanudarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.Suspendido)
                throw new InvalidOperationException("Solo se puede reanudar un partido suspendido.");
            partido.Estado = EstadoPartido.EnCurso;
            await _db.SaveChangesAsync();
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
        }

        public async Task FinalizarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado == EstadoPartido.Finalizado)
                throw new InvalidOperationException("El partido ya está finalizado.");
            partido.Estado = EstadoPartido.Finalizado;
            await _db.SaveChangesAsync();
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
        }

        public async Task<Partido> RegistrarTurnoAsync(int partidoId, int? jugadorConfirmadoId, ResultadoTurno resultado)
        {
            // Transacción para registrar la jugada, actualizar partido y crear PlayLog + PlayerBattingStat
            using var tx = await _db.Database.BeginTransactionAsync();
            var partido = await _db.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso) throw new InvalidOperationException("El partido no está en curso");

            // Obtener o crear la entrada (inning) actual para llevar control por entrada
            var numeroInning = partido.EntradaActual;
            var entrada = partido.Entradas.FirstOrDefault(e => e.NumeroInning == numeroInning);
            if (entrada == null)
            {
                entrada = new Entrada
                {
                    PartidoId = partido.Id,
                    NumeroInning = numeroInning,
                    CarrerasCasa = 0,
                    CarrerasVisita = 0,
                    HitsCasa = 0,
                    HitsVisita = 0,
                    ErroresCasa = 0,
                    ErroresVisita = 0
                };
                _db.Entradas.Add(entrada);
                partido.Entradas.Add(entrada);
            }

            // Snapshot previo completo
            var before = BuildSnapshot(partido);

            bool casaBatea = partido.Mitad == MitadEntrada.Baja;
            int equipoBateoId = casaBatea ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var lineup = await _db.Lineups
                .Where(l => l.PartidoId == partidoId && l.EquipoId == equipoBateoId)
                .OrderBy(l => l.Orden)
                .Include(l => l.Jugador)
                .ToListAsync();
            if (!lineup.Any())
            {
                throw new InvalidOperationException("Debe definir el lineup del equipo que está bateando.");
            }

            int idxEsperado = casaBatea ? (partido.IndexBateadorCasa ?? 0) : (partido.IndexBateadorVisita ?? 0);
            if (idxEsperado < 0 || idxEsperado >= lineup.Count)
            {
                idxEsperado = 0;
            }
            var turnoActual = lineup[idxEsperado];
            var jugadorId = turnoActual.JugadorId;

            if (jugadorConfirmadoId.HasValue && jugadorConfirmadoId.Value != jugadorId)
            {
                throw new InvalidOperationException("El bateador proporcionado no coincide con el próximo en el lineup.");
            }

            var siguienteIdx = (idxEsperado + 1) % lineup.Count;

            var jugador = turnoActual.Jugador;
            if (jugador == null)
            {
                jugador = await _db.Jugadores.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jugadorId)
                    ?? throw new InvalidOperationException("Jugador no encontrado en la base de datos.");
            }

            var esPrimerTurno = !await _db.PlayerBattingStats.AsNoTracking()
                .AnyAsync(s => s.PartidoId == partidoId && s.JugadorId == jugadorId);

            int runs = 0;
            int ab = 0;
            int h = 0;
            int hr = 0;
            int bb = 0;
            int so = 0;
            int sf = 0;
            int sh = 0;
            int rbi = 0;
            int hbp = 0;
            int doubles = 0;
            int triples = 0;
            int pa = 1;
            int runsBateador = 0;
            int partidosJugados = esPrimerTurno ? 1 : 0;

            // Helper to add runs to correct team
            void AddRuns(int n)
            {
                runs += n;
                if (casaBatea)
                {
                    partido.CarrerasCasa += n;
                    entrada.CarrerasCasa += n; // actualizar por entrada
                }
                else
                {
                    partido.CarrerasVisita += n;
                    entrada.CarrerasVisita += n;
                }
            }

            int ForceAdvanceOneBase()
            {
                var occupancy = new int[3];
                if (partido.B1) occupancy[0]++;
                if (partido.B2) occupancy[1]++;
                if (partido.B3) occupancy[2]++;
                occupancy[0]++; // bateador toma primera
                var forcedRuns = 0;
                for (var idxBase = 0; idxBase < 3; idxBase++)
                {
                    while (occupancy[idxBase] > 1)
                    {
                        occupancy[idxBase]--;
                        if (idxBase == 2)
                        {
                            forcedRuns++;
                        }
                        else
                        {
                            occupancy[idxBase + 1]++;
                        }
                    }
                }
                partido.B1 = occupancy[0] > 0;
                partido.B2 = occupancy[1] > 0;
                partido.B3 = occupancy[2] > 0;
                return forcedRuns;
            }

            static void AdvanceExistingRunnersOneBase(ref bool runner1, ref bool runner2, ref bool runner3)
            {
                if (!runner3 && runner2)
                {
                    runner3 = true;
                    runner2 = false;
                }

                if (!runner2 && runner1)
                {
                    runner2 = true;
                    runner1 = false;
                }
            }

            void CambiarMitadSiEsNecesaria()
            {
                if (partido.Estado == EstadoPartido.Finalizado || partido.Outs < 3)
                {
                    return;
                }

                partido.Outs = 0;
                partido.B1 = partido.B2 = partido.B3 = false;
                partido.Mitad = partido.Mitad == MitadEntrada.Alta ? MitadEntrada.Baja : MitadEntrada.Alta;
                if (partido.Mitad == MitadEntrada.Alta)
                {
                    partido.EntradaActual++;
                }

                if (partido.Mitad == MitadEntrada.Baja)
                {
                    partido.IndexBateadorCasa ??= 0;
                }
                else
                {
                    partido.IndexBateadorVisita ??= 0;
                }
            }

            // Apply simplified baseball rules y actualizar totales por equipo
            switch (resultado)
            {
                case ResultadoTurno.Sencillo:
                    ab = 1; h = 1;
                    // Sumar hit al equipo al bate (totales y por entrada)
                    if (casaBatea) { partido.HitsCasa++; entrada.HitsCasa++; } else { partido.HitsVisita++; entrada.HitsVisita++; }
                    // advance: runner on 3 scores
                    if (partido.B3) AddRuns(1);
                    partido.B3 = partido.B2; // runner from 2 -> 3
                    partido.B2 = partido.B1; // 1 -> 2
                    partido.B1 = true; // batter to 1st
                    rbi = runs;
                    break;
                case ResultadoTurno.Doble:
                    ab = 1; h = 1; doubles = 1;
                    if (casaBatea) { partido.HitsCasa++; entrada.HitsCasa++; } else { partido.HitsVisita++; entrada.HitsVisita++; }
                    if (partido.B3) AddRuns(1);
                    if (partido.B2) AddRuns(1);
                    partido.B3 = partido.B1; // runner from 1 -> 3
                    partido.B2 = true; // batter to 2nd
                    partido.B1 = false;
                    rbi = runs;
                    break;
                case ResultadoTurno.Triple:
                    ab = 1; h = 1; triples = 1;
                    if (casaBatea) { partido.HitsCasa++; entrada.HitsCasa++; } else { partido.HitsVisita++; entrada.HitsVisita++; }
                    // all existing score
                    int scored = 0;
                    if (partido.B1) scored++;
                    if (partido.B2) scored++;
                    if (partido.B3) scored++;
                    AddRuns(scored);
                    partido.B3 = true;
                    partido.B2 = partido.B1 = false;
                    rbi = runs;
                    break;
                case ResultadoTurno.Jonron:
                    ab = 1; h = 1; hr = 1; runsBateador = 1;
                    if (casaBatea) { partido.HitsCasa++; entrada.HitsCasa++; } else { partido.HitsVisita++; entrada.HitsVisita++; }
                    int scoredHr = 1 + (partido.B1 ? 1 : 0) + (partido.B2 ? 1 : 0) + (partido.B3 ? 1 : 0);
                    AddRuns(scoredHr);
                    partido.B1 = partido.B2 = partido.B3 = false;
                    rbi = scoredHr;
                    break;
                case ResultadoTurno.BasePorBolas:
                case ResultadoTurno.Golpe:
                    if (resultado == ResultadoTurno.Golpe)
                    {
                        hbp = 1;
                    }
                    else
                    {
                        bb = 1;
                    }
                    ab = 0;
                    var forcedBolas = ForceAdvanceOneBase();
                    if (forcedBolas > 0)
                    {
                        AddRuns(forcedBolas);
                        rbi = forcedBolas;
                    }
                    break;
                case ResultadoTurno.Ponche:
                    ab = 1; so = 1;
                    partido.Outs++;
                    break;
                case ResultadoTurno.OutEnJuego:
                    ab = 1;
                    partido.Outs++;
                    break;
                case ResultadoTurno.SacrificioFly:
                    sf = 1;
                    var sfRunner1 = partido.B1;
                    var sfRunner2 = partido.B2;
                    var sfRunner3 = partido.B3;
                    if (sfRunner3)
                    {
                        AddRuns(1);
                        sfRunner3 = false;
                        rbi = 1;
                    }
                    AdvanceExistingRunnersOneBase(ref sfRunner1, ref sfRunner2, ref sfRunner3);
                    partido.Outs++;
                    partido.B1 = sfRunner1;
                    partido.B2 = sfRunner2;
                    partido.B3 = sfRunner3;
                    break;
                case ResultadoTurno.SacrificioToque:
                    sh = 1;
                    var shRunner1 = partido.B1;
                    var shRunner2 = partido.B2;
                    var shRunner3 = partido.B3;
                    if (shRunner3)
                    {
                        AddRuns(1);
                        shRunner3 = false;
                        rbi = 1;
                    }
                    AdvanceExistingRunnersOneBase(ref shRunner1, ref shRunner2, ref shRunner3);
                    partido.Outs++;
                    partido.B1 = shRunner1;
                    partido.B2 = shRunner2;
                    partido.B3 = shRunner3;
                    break;
                case ResultadoTurno.LlegaPorError:
                    ab = 1;
                    // Error: batter to first, no hit. Increment defensive team's error count.
                    if (casaBatea)
                    {
                        // casa at bat => visita committed error
                        partido.ErroresVisita++;
                        entrada.ErroresVisita++;
                    }
                    else
                    {
                        partido.ErroresCasa++;
                        entrada.ErroresCasa++;
                    }
                    var forcedError = ForceAdvanceOneBase();
                    if (forcedError > 0)
                    {
                        AddRuns(forcedError);
                        rbi = forcedError;
                    }
                    break;
                default:
                    break;
            }

            // Walk-off: si en la baja de la 9na (o más) el equipo de casa pasa arriba con esta jugada, termina el partido
            if (partido.Mitad == MitadEntrada.Baja && partido.EntradaActual >= 9 && partido.CarrerasCasa > partido.CarrerasVisita)
            {
                partido.Estado = EstadoPartido.Finalizado;
            }

            CambiarMitadSiEsNecesaria();

            if (casaBatea)
            {
                partido.IndexBateadorCasa = siguienteIdx;
            }
            else
            {
                partido.IndexBateadorVisita = siguienteIdx;
            }

            // Persist PlayerBattingStat
            var delta = new BattingStatDelta
            {
                Partidos = partidosJugados,
                AB = ab,
                PA = pa,
                H = h,
                Doubles = doubles,
                Triples = triples,
                HR = hr,
                RBI = rbi,
                R = runsBateador,
                BB = bb,
                SO = so,
                HBP = hbp,
                SF = sf,
                SH = sh
            };

            var stat = new PlayerBattingStat
            {
                JugadorId = jugadorId,
                EquipoId = jugador.EquipoId,
                PartidoId = partidoId,
                Fecha = DateTime.UtcNow,
                Temporada = partido.Fecha.Year,
                PartidosJugados = delta.Partidos,
                AB = delta.AB,
                PA = delta.PA,
                H = delta.H,
                Doubles = delta.Doubles,
                Triples = delta.Triples,
                HR = delta.HR,
                R = delta.R,
                RBI = delta.RBI,
                BB = delta.BB,
                SO = delta.SO,
                HBP = delta.HBP,
                SF = delta.SF,
                SH = delta.SH
            };
            _db.PlayerBattingStats.Add(stat);

            // Add PlayLog con Before/After
            var after = BuildSnapshot(partido);
            var wrapper = new PlaySnapshotWrapper { Before = before, After = after };
            var log = new PlayLog
            {
                PartidoId = partidoId,
                JugadorId = jugadorId,
                Resultado = resultado,
                RunsScored = runs,
                SnapshotJson = JsonSerializer.Serialize(wrapper),
                StatDeltaJson = JsonSerializer.Serialize(delta)
            };
            _db.PlayLogs.Add(log);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Broadcast marcador actualizado
            try
            {
                if (_hub != null)
                    await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error broadcasting marcador");
            }

            return partido;
        }

        public async Task<Partido> DeshacerUltimaJugadaAsync(int partidoId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            var last = await _db.PlayLogs.Where(pl => pl.PartidoId == partidoId && pl.IsActive).OrderByDescending(pl => pl.CreadoUtc).FirstOrDefaultAsync();
            if (last == null) throw new InvalidOperationException("No hay jugadas para deshacer");

            var partido = await _db.Partidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");

            // Restaurar snapshot completo (Before)
            if (!string.IsNullOrEmpty(last.SnapshotJson))
            {
                try
                {
                    var wrapper = JsonSerializer.Deserialize<PlaySnapshotWrapper>(last.SnapshotJson);
                    SnapshotPartido? prev = wrapper?.Before;
                    if (prev == null)
                    {
                        // retrocompatibilidad: si no es wrapper, intentar snapshot simple
                        prev = JsonSerializer.Deserialize<SnapshotPartido>(last.SnapshotJson);
                    }
                    if (prev != null)
                    {
                        await ApplySnapshotAsync(partido, prev);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error restaurando snapshot del PlayLog");
                    throw;
                }
            }

            // Remove last created PlayerBattingStat if exists roughly by time
            var stat = await _db.PlayerBattingStats.Where(s => s.PartidoId == partidoId)
                .OrderByDescending(s => s.Fecha).FirstOrDefaultAsync();
            if (stat != null)
            {
                _db.PlayerBattingStats.Remove(stat);
            }

            // Mark play as inactive (soft-delete) so we can support redo
            last.IsActive = false;
            _db.PlayLogs.Update(last);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            try
            {
                if (_hub != null)
                    await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error broadcasting marcador after undo");
            }

            return partido;
        }

        public async Task<Partido> RehacerUltimaJugadaAsync(int partidoId)
        {
            // Find last undone play for the partido
            var undone = await _db.PlayLogs.Where(pl => pl.PartidoId == partidoId && !pl.IsActive).OrderByDescending(pl => pl.CreadoUtc).FirstOrDefaultAsync();
            if (undone == null) throw new InvalidOperationException("No hay jugadas para rehacer");

            // Si tenemos snapshot After, aplícalo directamente; si no, fallback a re-registrar
            if (!string.IsNullOrEmpty(undone.SnapshotJson))
            {
                try
                {
                    var wrapper = JsonSerializer.Deserialize<PlaySnapshotWrapper>(undone.SnapshotJson);
                    if (wrapper?.After != null)
                    {
                        var partido = await _db.Partidos.Include(p=>p.Entradas).FirstAsync(p=>p.Id==partidoId);
                        await ApplySnapshotAsync(partido, wrapper.After);
                        // Marcar play como activo otra vez
                        undone.IsActive = true;
                        _db.PlayLogs.Update(undone);
                        await _db.SaveChangesAsync();

                        try
                        {
                            if (_hub != null)
                                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", new { PartidoId = partidoId });
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error broadcasting marcador after redo");
                        }
                        return partido;
                    }
                }
                catch { /* ignore and fallback */ }
            }

            if (!undone.JugadorId.HasValue || !undone.Resultado.HasValue)
                throw new InvalidOperationException("La jugada deshecha no contiene datos para rehacer (Jugador/Resultado faltante)");

            return await RegistrarTurnoAsync(partidoId, undone.JugadorId.Value, undone.Resultado.Value);
        }

        public async Task<MarcadorDto> ObtenerMarcadorAsync(int partidoId)
        {
            var partido = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.Entradas)
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == partidoId);

            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");

            var maxInn = Math.Max(9, Math.Max(partido.EntradaActual, partido.Entradas?.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max() ?? 0));
            var casa = new int[maxInn];
            var vis = new int[maxInn];

            if (partido.Entradas != null)
            {
                foreach (var e in partido.Entradas)
                {
                    var idx = Math.Clamp(e.NumeroInning - 1, 0, maxInn - 1);
                    casa[idx] = e.CarrerasCasa;
                    vis[idx] = e.CarrerasVisita;
                }
            }

            var (bateador, puedeRegistrar, motivoBloqueo) = await ObtenerContextoTurnoAsync(partido);

            var dto = new MarcadorDto
            {
                PartidoId = partido.Id,
                EquipoCasa = partido.EquipoCasa?.Nombre ?? "",
                EquipoVisita = partido.EquipoVisita?.Nombre ?? "",
                CarrerasCasaPorInning = casa,
                CarrerasVisitaPorInning = vis,
                CarrerasCasa = partido.CarrerasCasa,
                CarrerasVisita = partido.CarrerasVisita,
                HitsCasa = partido.HitsCasa,
                HitsVisita = partido.HitsVisita,
                ErroresCasa = partido.ErroresCasa,
                ErroresVisita = partido.ErroresVisita,
                EntradaActual = partido.EntradaActual,
                Mitad = partido.Mitad.ToString(),
                Outs = partido.Outs,
                B1 = partido.B1,
                B2 = partido.B2,
                B3 = partido.B3,
                Estado = partido.Estado.ToString(),
                Entradas = (partido.Entradas ?? new List<Entrada>())
                    .OrderBy(e => e.NumeroInning)
                    .Select(e => new MarcadorEntradaDto
                    {
                        NumeroInning = e.NumeroInning,
                        CarrerasCasa = e.CarrerasCasa,
                        CarrerasVisita = e.CarrerasVisita
                    })
                    .ToList(),
                BateadorEsperado = bateador,
                PuedeRegistrar = puedeRegistrar,
                MotivoBloqueo = motivoBloqueo
            };

            return dto;
        }

        private SnapshotPartido BuildSnapshot(Partido p)
        {
            var snap = new SnapshotPartido
            {
                EntradaActual = p.EntradaActual,
                Mitad = p.Mitad.ToString(),
                Outs = p.Outs,
                B1 = p.B1,
                B2 = p.B2,
                B3 = p.B3,
                CarrerasCasa = p.CarrerasCasa,
                CarrerasVisita = p.CarrerasVisita,
                HitsCasa = p.HitsCasa,
                HitsVisita = p.HitsVisita,
                ErroresCasa = p.ErroresCasa,
                ErroresVisita = p.ErroresVisita,
                Estado = (int)p.Estado,
                IndexBateadorCasa = p.IndexBateadorCasa,
                IndexBateadorVisita = p.IndexBateadorVisita,
                Entradas = (p.Entradas ?? new List<Entrada>())
                    .OrderBy(e => e.NumeroInning)
                    .Select(e => new SnapshotEntrada
                    {
                        NumeroInning = e.NumeroInning,
                        CarrerasCasa = e.CarrerasCasa,
                        CarrerasVisita = e.CarrerasVisita,
                        HitsCasa = e.HitsCasa,
                        HitsVisita = e.HitsVisita,
                        ErroresCasa = e.ErroresCasa,
                        ErroresVisita = e.ErroresVisita
                    })
                    .ToList()
            };
            return snap;
        }

        private async Task ApplySnapshotAsync(Partido p, SnapshotPartido s)
        {
            p.EntradaActual = s.EntradaActual;
            p.Mitad = Enum.Parse<MitadEntrada>(s.Mitad ?? "Alta");
            p.Outs = s.Outs;
            p.B1 = s.B1; p.B2 = s.B2; p.B3 = s.B3;
            p.CarrerasCasa = s.CarrerasCasa;
            p.CarrerasVisita = s.CarrerasVisita;
            p.HitsCasa = s.HitsCasa;
            p.HitsVisita = s.HitsVisita;
            p.ErroresCasa = s.ErroresCasa;
            p.ErroresVisita = s.ErroresVisita;
            p.Estado = (EstadoPartido)s.Estado;
            p.IndexBateadorCasa = s.IndexBateadorCasa;
            p.IndexBateadorVisita = s.IndexBateadorVisita;

            // Reemplazar entradas
            var existentes = _db.Entradas.Where(e => e.PartidoId == p.Id);
            _db.Entradas.RemoveRange(existentes);
            if (s.Entradas != null && s.Entradas.Count > 0)
            {
                foreach (var se in s.Entradas)
                {
                    _db.Entradas.Add(new Entrada
                    {
                        PartidoId = p.Id,
                        NumeroInning = se.NumeroInning,
                        CarrerasCasa = se.CarrerasCasa,
                        CarrerasVisita = se.CarrerasVisita,
                        HitsCasa = se.HitsCasa,
                        HitsVisita = se.HitsVisita,
                        ErroresCasa = se.ErroresCasa,
                        ErroresVisita = se.ErroresVisita
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task<(MarcadorBateadorDto? Bateador, bool PuedeRegistrar, string? MotivoBloqueo)> ObtenerContextoTurnoAsync(Partido partido)
        {
            if (partido.Estado != EstadoPartido.EnCurso)
            {
                var motivo = partido.Estado == EstadoPartido.Finalizado
                    ? "El partido está finalizado."
                    : "El partido debe estar en curso para registrar jugadas.";
                return (null, false, motivo);
            }

            var equipoBateaId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var lineup = await _db.Lineups.AsNoTracking()
                .Where(l => l.PartidoId == partido.Id && l.EquipoId == equipoBateaId)
                .OrderBy(l => l.Orden)
                .Include(l => l.Jugador)
                .ToListAsync();

            if (!lineup.Any())
            {
                return (null, false, "Debes definir el lineup del equipo al bate antes de registrar jugadas.");
            }

            var idx = partido.Mitad == MitadEntrada.Baja
                ? (partido.IndexBateadorCasa ?? 0)
                : (partido.IndexBateadorVisita ?? 0);
            if (idx < 0 || idx >= lineup.Count)
            {
                idx = 0;
            }

            var jugador = lineup[idx].Jugador;
            if (jugador == null)
            {
                jugador = await _db.Jugadores.AsNoTracking()
                    .FirstOrDefaultAsync(j => j.Id == lineup[idx].JugadorId);
            }
            if (jugador == null)
            {
                return (null, false, "No se pudo determinar el bateador esperado. Revisa el lineup.");
            }

            var dto = new MarcadorBateadorDto
            {
                Id = jugador.Id,
                Nombre = jugador.Nombre,
                Apellido = jugador.Apellido,
                EquipoId = jugador.EquipoId,
                NumeroUniforme = jugador.NumeroUniforme
            };

            return (dto, true, null);
        }

        private sealed record BattingStatDelta
        {
            public int Partidos { get; init; }
            public int AB { get; init; }
            public int PA { get; init; }
            public int H { get; init; }
            public int Doubles { get; init; }
            public int Triples { get; init; }
            public int HR { get; init; }
            public int RBI { get; init; }
            public int R { get; init; }
            public int BB { get; init; }
            public int SO { get; init; }
            public int HBP { get; init; }
            public int SF { get; init; }
            public int SH { get; init; }
        }
    }
}
