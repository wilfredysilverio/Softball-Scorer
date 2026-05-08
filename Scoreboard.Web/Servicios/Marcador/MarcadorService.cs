using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Scoreboard.Web.Dtos;
using Scoreboard.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Helpers;
using Scoreboard.Web.Modelos;
using System.Text.Json.Serialization;

namespace Scoreboard.Web.Servicios.Marcador
{
    /// <summary>
    /// Servicio central que aplica las reglas reales del anotador de softbol.
    ///
    /// Se conecta con:
    /// - ContextoMarcador: para leer y guardar partido, entrada, lineup, historial y estadisticas.
    /// - MarcadorHub: para notificar cambios en tiempo real.
    /// - PlayLog: para guardar el historial con snapshot antes/despues.
    /// - PlayerBattingStat: para guardar estadisticas ofensivas.
    ///
    /// Flujo simple:
    /// 1. Recibe una accion del partido, como hit, ponche, out o evento de corredor.
    /// 2. Calcula outs, bases, carreras, entrada y proximo bateador.
    /// 3. Guarda los cambios y notifica al frontend.
    ///
    /// Cuidado:
    /// Esta clase es el corazon del marcador. Cambios aqui pueden alterar resultados, historial y estadisticas.
    /// </summary>
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
            // Validaciones de estado: idempotente si ya estÃ¡ en curso; no permitir iniciar si finalizado
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
            // Inicializar Ã­ndices si hay lineups definidos
            if (lineupCasa.Count > 0) partido.IndexBateadorCasa = 0; else partido.IndexBateadorCasa = null;
            if (lineupVisita.Count > 0) partido.IndexBateadorVisita = 0; else partido.IndexBateadorVisita = null; // visitante batea primero cuando toque
            await _db.SaveChangesAsync();

            // Broadcast: notificar para que los clientes refresquen su marcador
            await NotifyCambioMarcadorAsync(partidoId);
        }

        public async Task SuspenderPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso)
                throw new InvalidOperationException("Solo se puede suspender un partido en progreso.");
            partido.Estado = EstadoPartido.Suspendido;
            await _db.SaveChangesAsync();
            await NotifyCambioMarcadorAsync(partidoId);
        }

        public async Task ReanudarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.Suspendido)
                throw new InvalidOperationException("Solo se puede reanudar un partido suspendido.");
            partido.Estado = EstadoPartido.EnCurso;
            await _db.SaveChangesAsync();
            await NotifyCambioMarcadorAsync(partidoId);
        }

        public async Task FinalizarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado == EstadoPartido.Finalizado)
                throw new InvalidOperationException("El partido ya estÃ¡ finalizado.");
            partido.Estado = EstadoPartido.Finalizado;
            await _db.SaveChangesAsync();
            await NotifyCambioMarcadorAsync(partidoId);
        }

        public async Task<Partido> RegistrarTurnoAsync(int partidoId, int? jugadorConfirmadoId, ResultadoTurno resultado, EventoCorredor eventoCorredor = EventoCorredor.Ninguno, BaseCorredor baseEvento = BaseCorredor.Primera, bool permitirFueraTurno = false)
        {
            // TransacciÃ³n para registrar la jugada, actualizar partido y crear PlayLog + PlayerBattingStat
            using var tx = await _db.Database.BeginTransactionAsync();
            var partido = await _db.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso) throw new InvalidOperationException("El partido no estÃ¡ en curso");

            // Obtener o crear la entrada (inning) actual para llevar control por entrada
            var numeroInning = partido.EntradaActual;
            if (numeroInning < 1)
            {
                numeroInning = 1;
                partido.EntradaActual = 1;
            }
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
                throw new InvalidOperationException("Debe definir el lineup del equipo que estÃ¡ bateando.");
            }

            int idxEsperado = casaBatea ? (partido.IndexBateadorCasa ?? 0) : (partido.IndexBateadorVisita ?? 0);
            if (idxEsperado < 0 || idxEsperado >= lineup.Count)
            {
                idxEsperado = 0;
            }
            var turnoActual = lineup[idxEsperado];
            var jugadorEsperadoId = turnoActual.JugadorId;
            var jugadorId = jugadorEsperadoId;
            var esCorreccionManual = false;

            if (jugadorConfirmadoId.HasValue && jugadorConfirmadoId.Value != jugadorEsperadoId)
            {
                if (!permitirFueraTurno)
                {
                    throw new InvalidOperationException("El bateador proporcionado no coincide con el proximo en el lineup.");
                }

                if (!lineup.Any(l => l.JugadorId == jugadorConfirmadoId.Value))
                {
                    throw new InvalidOperationException("El bateador manual debe pertenecer al lineup del equipo que esta bateando.");
                }

                jugadorId = jugadorConfirmadoId.Value;
                esCorreccionManual = true;
            }
            var siguienteIdx = (idxEsperado + 1) % lineup.Count;

            var jugador = esCorreccionManual
                ? lineup.FirstOrDefault(l => l.JugadorId == jugadorId)?.Jugador
                : turnoActual.Jugador;
            if (jugador == null)
            {
                jugador = await _db.Jugadores.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jugadorId)
                    ?? throw new InvalidOperationException("Jugador no encontrado en la base de datos.");
            }

            var esPrimerTurno = !await _db.PlayerBattingStats.AsNoTracking()
                .AnyAsync(s => s.PartidoId == partidoId && s.JugadorId == jugadorId);
            var partidosJugados = esPrimerTurno ? 1 : 0;
            var outcome = AplicarResultadoTurno(partido, entrada, casaBatea, resultado, eventoCorredor, baseEvento);
            var delta = outcome.Stats with
            {
                Partidos = partidosJugados,
                PA = 1
            };
            var runs = outcome.RunsScored;

            // Walk-off: si en la baja de la 9na (o mÃ¡s) el equipo de casa pasa arriba con esta jugada, termina el partido
            if (partido.Mitad == MitadEntrada.Baja && partido.EntradaActual >= 9 && partido.CarrerasCasa > partido.CarrerasVisita)
            {
                partido.Estado = EstadoPartido.Finalizado;
            }

            if (casaBatea)
            {
                partido.IndexBateadorCasa = siguienteIdx;
            }
            else
            {
                partido.IndexBateadorVisita = siguienteIdx;
            }

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
                JugadorEsperadoId = jugadorEsperadoId,
                EquipoBateoId = equipoBateoId,
                Resultado = resultado,
                RunsScored = runs,
                EsCorreccionManual = esCorreccionManual,
                Nota = esCorreccionManual ? $"Bateador fuera de turno confirmado. Esperado: {jugadorEsperadoId}; registrado: {jugadorId}." : null,
                SnapshotJson = JsonSerializer.Serialize(wrapper),
                StatDeltaJson = JsonSerializer.Serialize(delta)
            };
            _db.PlayLogs.Add(log);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Broadcast marcador actualizado
            await NotifyCambioMarcadorAsync(partidoId);

            return partido;
        }

        public async Task<Partido> RegistrarEventoCorredorAsync(int partidoId, EventoCorredor eventoCorredor, BaseCorredor baseCorredor)
        {
            if (eventoCorredor == EventoCorredor.Ninguno)
            {
                throw new InvalidOperationException("Selecciona un evento vÃ¡lido de corredores.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            var partido = await _db.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso) throw new InvalidOperationException("El partido no estÃ¡ en curso");

            var numeroInning = partido.EntradaActual;
            if (numeroInning < 1)
            {
                numeroInning = 1;
                partido.EntradaActual = 1;
            }

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

            bool corredorDisponible = baseCorredor switch
            {
                BaseCorredor.Primera => partido.B1,
                BaseCorredor.Segunda => partido.B2,
                BaseCorredor.Tercera => partido.B3,
                _ => false
            };
            if (!corredorDisponible)
            {
                throw new InvalidOperationException("No hay corredor en la base seleccionada.");
            }

            var before = BuildSnapshot(partido);
            var baseIndex = (int)baseCorredor - 1;
            var runs = AvanzarCorredorDesdeBase(partido, baseIndex);
            if (runs > 0)
            {
                if (partido.Mitad == MitadEntrada.Baja)
                {
                    partido.CarrerasCasa += runs;
                    entrada.CarrerasCasa += runs;
                }
                else
                {
                    partido.CarrerasVisita += runs;
                    entrada.CarrerasVisita += runs;
                }
            }

            if (partido.Mitad == MitadEntrada.Baja && partido.EntradaActual >= 9 && partido.CarrerasCasa > partido.CarrerasVisita)
            {
                partido.Estado = EstadoPartido.Finalizado;
            }

            var after = BuildSnapshot(partido);
            var wrapper = new PlaySnapshotWrapper { Before = before, After = after };
            var log = new PlayLog
            {
                PartidoId = partidoId,
                Resultado = null,
                RunsScored = runs,
                SnapshotJson = JsonSerializer.Serialize(wrapper),
                StatDeltaJson = null
            };
            _db.PlayLogs.Add(log);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await NotifyCambioMarcadorAsync(partidoId);
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

            await NotifyCambioMarcadorAsync(partidoId);

            return partido;
        }

        public async Task<Partido> RehacerUltimaJugadaAsync(int partidoId)
        {
            // Find last undone play for the partido
            var undone = await _db.PlayLogs.Where(pl => pl.PartidoId == partidoId && !pl.IsActive).OrderByDescending(pl => pl.CreadoUtc).FirstOrDefaultAsync();
            if (undone == null) throw new InvalidOperationException("No hay jugadas para rehacer");

            // Si tenemos snapshot After, aplÃ­calo directamente; si no, fallback a re-registrar
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

                        await NotifyCambioMarcadorAsync(partidoId);
                        return partido;
                    }
                }
                catch { /* ignore and fallback */ }
            }

            if (!undone.JugadorId.HasValue || !undone.Resultado.HasValue)
                throw new InvalidOperationException("La jugada deshecha no contiene datos para rehacer (Jugador/Resultado faltante)");

            return await RegistrarTurnoAsync(partidoId, undone.JugadorId.Value, undone.Resultado.Value, EventoCorredor.Ninguno, BaseCorredor.Primera);
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

            var entradasNormalizadas = EntradaHelper.NormalizarEntradas(partido.Entradas);
            var entradaActualSegura = partido.EntradaActual < 1 ? 1 : partido.EntradaActual;
            var maxInn = Math.Max(9, Math.Max(entradaActualSegura, entradasNormalizadas.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max()));
            var casa = new int[maxInn];
            var vis = new int[maxInn];

            foreach (var e in entradasNormalizadas)
            {
                var idx = Math.Clamp(e.NumeroInning - 1, 0, maxInn - 1);
                casa[idx] = e.CarrerasCasa;
                vis[idx] = e.CarrerasVisita;
            }

            var (bateador, puedeRegistrar, motivoBloqueo) = await ObtenerContextoTurnoAsync(partido);
            var equipoBateandoId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var equipoBateando = partido.Mitad == MitadEntrada.Baja
                ? partido.EquipoCasa?.Nombre ?? "Casa"
                : partido.EquipoVisita?.Nombre ?? "Visitante";
            var lineupBateandoItems = await _db.Lineups.AsNoTracking()
                .Where(l => l.PartidoId == partido.Id && l.EquipoId == equipoBateandoId)
                .OrderBy(l => l.Orden)
                .Include(l => l.Jugador)
                .ToListAsync();
            var lineupBateando = lineupBateandoItems
                .Where(l => l.Jugador != null)
                .Select(l => new MarcadorBateadorDto
                {
                    Id = l.Jugador!.Id,
                    Nombre = l.Jugador.Nombre,
                    Apellido = l.Jugador.Apellido,
                    EquipoId = l.Jugador.EquipoId,
                    NumeroUniforme = l.Jugador.NumeroUniforme
                })
                .ToList();

            var dto = new MarcadorDto
            {
                PartidoId = partido.Id,
                EquipoCasa = partido.EquipoCasa?.Nombre ?? "",
                EquipoVisita = partido.EquipoVisita?.Nombre ?? "",
                EquipoBateandoId = equipoBateandoId,
                EquipoBateando = equipoBateando,
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
                Entradas = entradasNormalizadas
                    .Select(e => new MarcadorEntradaDto
                    {
                        NumeroInning = e.NumeroInning,
                        CarrerasCasa = e.CarrerasCasa,
                        CarrerasVisita = e.CarrerasVisita
                    })
                    .ToList(),
                BateadorEsperado = bateador,
                LineupBateando = lineupBateando,
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
            p.EntradaActual = Math.Max(1, s.EntradaActual);
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
                        NumeroInning = Math.Max(1, se.NumeroInning),
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
                    ? "El partido estÃ¡ finalizado."
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

        private void IncrementarOut(Partido partido, int outs = 1)
        {
            if (outs <= 0 || partido.Estado == EstadoPartido.Finalizado) return;
            partido.Outs += outs;
            CambiarMitadSiEsNecesaria(partido);
        }

        private void CambiarMitadSiEsNecesaria(Partido partido)
        {
            if (partido.Estado == EstadoPartido.Finalizado)
            {
                return;
            }

            while (partido.Outs >= 3)
            {
                partido.Outs -= 3;
                var estabaAlta = partido.Mitad == MitadEntrada.Alta;

                partido.B1 = partido.B2 = partido.B3 = false;
                partido.Mitad = estabaAlta ? MitadEntrada.Baja : MitadEntrada.Alta;
                if (!estabaAlta)
                {
                    partido.EntradaActual++;
                    if (partido.EntradaActual >= 9 && partido.CarrerasCasa != partido.CarrerasVisita)
                    {
                        partido.Estado = EstadoPartido.Finalizado;
                        partido.Outs = 0;
                        break;
                    }
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
        }

        private PlayProcessingResult AplicarResultadoTurno(Partido partido, Entrada entrada, bool casaBatea, ResultadoTurno resultado, EventoCorredor eventoCorredor, BaseCorredor baseEvento)
        {
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
            int runsBateador = 0;

            void AddRuns(int value, bool cuentaRbi = true)
            {
                if (value <= 0) return;
                runs += value;
                if (casaBatea)
                {
                    partido.CarrerasCasa += value;
                    entrada.CarrerasCasa += value;
                }
                else
                {
                    partido.CarrerasVisita += value;
                    entrada.CarrerasVisita += value;
                }
                if (cuentaRbi)
                {
                    rbi += value;
                }
            }

            void RegistrarHit()
            {
                if (casaBatea)
                {
                    partido.HitsCasa++;
                    entrada.HitsCasa++;
                }
                else
                {
                    partido.HitsVisita++;
                    entrada.HitsVisita++;
                }
            }

            if (eventoCorredor != EventoCorredor.Ninguno)
            {
                var baseIdx = (int)baseEvento - 1;
                if (baseIdx < 0 || baseIdx > 2)
                {
                    throw new InvalidOperationException("Seleccione una base vÃ¡lida para el evento del corredor.");
                }
                var runsPorEvento = AvanzarCorredorDesdeBase(partido, baseIdx);
                if (runsPorEvento > 0)
                {
                    AddRuns(runsPorEvento, cuentaRbi: false);
                }
            }

            switch (resultado)
            {
                case ResultadoTurno.Sencillo:
                    ab = 1;
                    h = 1;
                    RegistrarHit();
                    if (partido.B3) AddRuns(1);
                    partido.B3 = partido.B2;
                    partido.B2 = partido.B1;
                    partido.B1 = true;
                    break;
                case ResultadoTurno.Doble:
                    ab = 1;
                    h = 1;
                    doubles = 1;
                    RegistrarHit();
                    if (partido.B3) AddRuns(1);
                    if (partido.B2) AddRuns(1);
                    partido.B3 = partido.B1;
                    partido.B2 = true;
                    partido.B1 = false;
                    break;
                case ResultadoTurno.Triple:
                    ab = 1;
                    h = 1;
                    triples = 1;
                    RegistrarHit();
                    int scored = 0;
                    if (partido.B1) scored++;
                    if (partido.B2) scored++;
                    if (partido.B3) scored++;
                    AddRuns(scored);
                    partido.B3 = true;
                    partido.B2 = false;
                    partido.B1 = false;
                    break;
                case ResultadoTurno.Jonron:
                    ab = 1;
                    h = 1;
                    hr = 1;
                    runsBateador = 1;
                    RegistrarHit();
                    int scoredHr = 1 + (partido.B1 ? 1 : 0) + (partido.B2 ? 1 : 0) + (partido.B3 ? 1 : 0);
                    AddRuns(scoredHr);
                    partido.B1 = false;
                    partido.B2 = false;
                    partido.B3 = false;
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
                    var forcedBolas = ForceAdvanceOneBase(partido);
                    if (forcedBolas > 0)
                    {
                        AddRuns(forcedBolas);
                    }
                    break;
                case ResultadoTurno.Ponche:
                    ab = 1;
                    so = 1;
                    IncrementarOut(partido);
                    break;
                case ResultadoTurno.OutEnJuego:
                    ab = 1;
                    IncrementarOut(partido);
                    break;
                case ResultadoTurno.DoblePlay:
                    ab = 1;
                    IncrementarOut(partido, 2);
                    // Sin informaciÃ³n granular del corrido, asumimos que cae el corredor forzado mÃ¡s cercano al bateador
                    if (partido.B1)
                    {
                        partido.B1 = false;
                    }
                    else if (partido.B2)
                    {
                        partido.B2 = false;
                    }
                    else if (partido.B3)
                    {
                        partido.B3 = false;
                    }
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
                    }
                    (sfRunner1, sfRunner2, sfRunner3) = AdvanceExistingRunnersOneBase(sfRunner1, sfRunner2, sfRunner3);
                    partido.B1 = sfRunner1;
                    partido.B2 = sfRunner2;
                    partido.B3 = sfRunner3;
                    IncrementarOut(partido);
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
                    }
                    (shRunner1, shRunner2, shRunner3) = AdvanceExistingRunnersOneBase(shRunner1, shRunner2, shRunner3);
                    partido.B1 = shRunner1;
                    partido.B2 = shRunner2;
                    partido.B3 = shRunner3;
                    IncrementarOut(partido);
                    break;
                case ResultadoTurno.LlegaPorError:
                    ab = 1;
                    if (casaBatea)
                    {
                        partido.ErroresVisita++;
                        entrada.ErroresVisita++;
                    }
                    else
                    {
                        partido.ErroresCasa++;
                        entrada.ErroresCasa++;
                    }
                    var forcedError = ForceAdvanceOneBase(partido);
                    if (forcedError > 0)
                    {
                        AddRuns(forcedError);
                    }
                    break;
                default:
                    break;
            }

            var delta = new BattingStatDelta
            {
                AB = ab,
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

            return new PlayProcessingResult(delta, runs);
        }

        private int AvanzarCorredorDesdeBase(Partido partido, int baseIndex)
        {
            if (baseIndex < 0 || baseIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(baseIndex));
            }

            var occupancy = new int[3];
            if (partido.B1) occupancy[0]++;
            if (partido.B2) occupancy[1]++;
            if (partido.B3) occupancy[2]++;

            if (occupancy[baseIndex] == 0)
            {
                throw new InvalidOperationException("No hay corredor en la base seleccionada para el evento.");
            }

            occupancy[baseIndex]--;
            var runs = baseIndex == 2 ? 1 : 0;
            if (baseIndex < 2)
            {
                occupancy[baseIndex + 1]++;
            }

            for (var idx = 0; idx < 3; idx++)
            {
                while (occupancy[idx] > 1)
                {
                    occupancy[idx]--;
                    if (idx == 2)
                    {
                        runs++;
                    }
                    else
                    {
                        occupancy[idx + 1]++;
                    }
                }
            }

            partido.B1 = occupancy[0] > 0;
            partido.B2 = occupancy[1] > 0;
            partido.B3 = occupancy[2] > 0;

            return runs;
        }

        private int ForceAdvanceOneBase(Partido partido)
        {
            var occupancy = new int[3];
            if (partido.B1) occupancy[0]++;
            if (partido.B2) occupancy[1]++;
            if (partido.B3) occupancy[2]++;
            occupancy[0]++;
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

        private static (bool B1, bool B2, bool B3) AdvanceExistingRunnersOneBase(bool b1, bool b2, bool b3)
        {
            var runner1 = b1;
            var runner2 = b2;
            var runner3 = b3;

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

            return (runner1, runner2, runner3);
        }

        private async Task NotifyCambioMarcadorAsync(int partidoId)
        {
            if (_hub == null) return;
            try
            {
                var dto = await ObtenerMarcadorAsync(partidoId);
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error broadcasting marcador");
            }
        }

        private sealed record PlayProcessingResult(BattingStatDelta Stats, int RunsScored);

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

