using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Scoreboard.Web.Dtos;
using Scoreboard.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;

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
            partido.Estado = EstadoPartido.EnCurso;
            await _db.SaveChangesAsync();

            // Broadcast nuevo estado
            var dto = await ObtenerMarcadorAsync(partidoId);
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
        }

        public async Task SuspenderPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            partido.Estado = EstadoPartido.Suspendido;
            await _db.SaveChangesAsync();
            var dto = await ObtenerMarcadorAsync(partidoId);
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
        }

        public async Task ReanudarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            partido.Estado = EstadoPartido.EnCurso;
            await _db.SaveChangesAsync();
            var dto = await ObtenerMarcadorAsync(partidoId);
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
        }

        public async Task FinalizarPartidoAsync(int partidoId)
        {
            var partido = await _db.Partidos.FindAsync(partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            partido.Estado = EstadoPartido.Finalizado;
            await _db.SaveChangesAsync();
            var dto = await ObtenerMarcadorAsync(partidoId);
            if (_hub != null)
                await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
        }

        public async Task<Partido> RegistrarTurnoAsync(int partidoId, int jugadorId, ResultadoTurno resultado)
        {
            // Transacción para registrar la jugada, actualizar partido y crear PlayLog + PlayerBattingStat
            using var tx = await _db.Database.BeginTransactionAsync();
            var partido = await _db.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.Estado != EstadoPartido.EnCurso) throw new InvalidOperationException("El partido no está en curso");

            // Snapshot previo (serialize Mitad as string to make restore robust)
            var snapshot = JsonSerializer.Serialize(new
            {
                partido.EntradaActual,
                Mitad = partido.Mitad.ToString(),
                partido.Outs,
                partido.B1,
                partido.B2,
                partido.B3,
                partido.CarrerasCasa,
                partido.CarrerasVisita,
                partido.HitsCasa,
                partido.HitsVisita,
                partido.ErroresCasa,
                partido.ErroresVisita
            });

            int runs = 0;
            int ab = 0;
            int h = 0;
            int hr = 0;
            int bb = 0;
            int so = 0;
            int sf = 0;
            int rbi = 0;

            // Determine which team is batting
            bool casaBatea = partido.Mitad == MitadEntrada.Baja;

            // Helper to add runs to correct team
            void AddRuns(int n)
            {
                runs += n;
                if (casaBatea) partido.CarrerasCasa += n; else partido.CarrerasVisita += n;
            }

            // Apply simplified baseball rules
            switch (resultado)
            {
                case ResultadoTurno.Sencillo:
                    ab = 1; h = 1;
                    // advance: runner on 3 scores
                    if (partido.B3) AddRuns(1);
                    partido.B3 = partido.B2; // runner from 2 -> 3
                    partido.B2 = partido.B1; // 1 -> 2
                    partido.B1 = true; // batter to 1st
                    rbi = runs;
                    break;
                case ResultadoTurno.Doble:
                    ab = 1; h = 1;
                    if (partido.B3) AddRuns(1);
                    if (partido.B2) AddRuns(1);
                    partido.B3 = partido.B1; // runner from 1 -> 3
                    partido.B2 = true; // batter to 2nd
                    partido.B1 = false;
                    rbi = runs;
                    break;
                case ResultadoTurno.Triple:
                    ab = 1; h = 1;
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
                    ab = 1; h = 1; hr = 1;
                    int scoredHr = 1 + (partido.B1 ? 1 : 0) + (partido.B2 ? 1 : 0) + (partido.B3 ? 1 : 0);
                    AddRuns(scoredHr);
                    partido.B1 = partido.B2 = partido.B3 = false;
                    rbi = scoredHr;
                    break;
                case ResultadoTurno.BasePorBolas:
                case ResultadoTurno.Golpe:
                    bb = 1;
                    // force advance
                    if (partido.B1 && partido.B2 && partido.B3)
                    {
                        AddRuns(1); // forced in
                    }
                    else
                    {
                        if (partido.B1 && partido.B2 && !partido.B3) partido.B3 = partido.B2;
                        if (partido.B1 && !partido.B2) partido.B2 = partido.B1;
                    }
                    partido.B1 = true;
                    rbi = runs;
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
                case ResultadoTurno.SacrificioToque:
                    sf = 1;
                    // If runner on third and less than 2 outs, score
                    if (partido.B3 && partido.Outs < 2)
                    {
                        AddRuns(1);
                        partido.B3 = false;
                        rbi = 1;
                    }
                    partido.Outs++;
                    break;
                case ResultadoTurno.LlegaPorError:
                    // Error: batter to first, no hit. Increment defensive team's error count.
                    if (casaBatea)
                    {
                        // casa at bat => visita committed error
                        partido.ErroresVisita++;
                    }
                    else
                    {
                        partido.ErroresCasa++;
                    }
                    // advance forced one
                    if (partido.B1 && partido.B2 && partido.B3)
                    {
                        AddRuns(1);
                    }
                    else
                    {
                        if (partido.B1 && !partido.B2) partido.B2 = partido.B1;
                    }
                    partido.B1 = true;
                    break;
                default:
                    break;
            }

            // If 3 outs, change side
            if (partido.Outs >= 3)
            {
                partido.Outs = 0;
                // switch mitad
                partido.Mitad = partido.Mitad == MitadEntrada.Alta ? MitadEntrada.Baja : MitadEntrada.Alta;
                // increment entry if we just finished bottom -> increment entry
                if (partido.Mitad == MitadEntrada.Alta)
                {
                    partido.EntradaActual++;
                }
                // clear bases
                partido.B1 = partido.B2 = partido.B3 = false;
            }

            // Persist PlayerBattingStat
            var stat = new PlayerBattingStat
            {
                JugadorId = jugadorId,
                PartidoId = partidoId,
                Fecha = DateTime.UtcNow,
                AB = ab,
                H = h,
                Doubles = resultado == ResultadoTurno.Doble ? 1 : 0,
                Triples = resultado == ResultadoTurno.Triple ? 1 : 0,
                HR = hr,
                R = runs,
                RBI = rbi,
                BB = bb,
                SO = so,
                HBP = 0,
                SF = sf
            };
            _db.PlayerBattingStats.Add(stat);

            // Add PlayLog
            var log = new PlayLog
            {
                PartidoId = partidoId,
                JugadorId = jugadorId,
                Resultado = resultado,
                RunsScored = runs,
                SnapshotJson = snapshot
            };
            _db.PlayLogs.Add(log);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Broadcast marcador actualizado
            try
            {
                var dto = await ObtenerMarcadorAsync(partidoId);
                if (_hub != null)
                    await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
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
            var last = await _db.PlayLogs.Where(pl => pl.PartidoId == partidoId && pl.IsActive).OrderByDescending(pl => pl.Fecha).FirstOrDefaultAsync();
            if (last == null) throw new InvalidOperationException("No hay jugadas para deshacer");

            var partido = await _db.Partidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");

            // Restore snapshot (best effort)
            if (!string.IsNullOrEmpty(last.SnapshotJson))
            {
                try
                {
                    var doc = JsonDocument.Parse(last.SnapshotJson);
                    var root = doc.RootElement;
                    partido.EntradaActual = root.GetProperty("EntradaActual").GetInt32();
                    partido.Mitad = Enum.Parse<MitadEntrada>(root.GetProperty("Mitad").GetString() ?? "Alta");
                    partido.Outs = root.GetProperty("Outs").GetInt32();
                    partido.B1 = root.GetProperty("B1").GetBoolean();
                    partido.B2 = root.GetProperty("B2").GetBoolean();
                    partido.B3 = root.GetProperty("B3").GetBoolean();
                    partido.CarrerasCasa = root.GetProperty("CarrerasCasa").GetInt32();
                    partido.CarrerasVisita = root.GetProperty("CarrerasVisita").GetInt32();
                    partido.HitsCasa = root.GetProperty("HitsCasa").GetInt32();
                    partido.HitsVisita = root.GetProperty("HitsVisita").GetInt32();
                    partido.ErroresCasa = root.GetProperty("ErroresCasa").GetInt32();
                    partido.ErroresVisita = root.GetProperty("ErroresVisita").GetInt32();
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
                var dto = await ObtenerMarcadorAsync(partidoId);
                if (_hub != null)
                    await _hub.Clients.Group($"partido-{partidoId}").SendAsync("ActualizarMarcador", dto);
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
            var undone = await _db.PlayLogs.Where(pl => pl.PartidoId == partidoId && !pl.IsActive).OrderByDescending(pl => pl.Fecha).FirstOrDefaultAsync();
            if (undone == null) throw new InvalidOperationException("No hay jugadas para rehacer");

            // Re-apply by invoking RegistrarTurnoAsync with the same jugador and resultado.
            // RegistrarTurnoAsync will create a new PlayLog (active) and the PlayerBattingStat.
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

            var maxInn = Math.Max(9, (partido.Entradas?.Max(e => (int?)e.NumeroInning) ?? 0));
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

            var dto = new MarcadorDto
            {
                PartidoId = partido.Id,
                EquipoCasa = partido.EquipoCasa?.Nombre ?? "",
                EquipoVisita = partido.EquipoVisita?.Nombre ?? "",
                CarrerasCasaPorInning = casa.Length == 9 ? casa : PadArray(casa, 9),
                CarrerasVisitaPorInning = vis.Length == 9 ? vis : PadArray(vis, 9),
                RCasa = partido.CarrerasCasa,
                RVisita = partido.CarrerasVisita,
                HCasa = partido.HitsCasa,
                HVisita = partido.HitsVisita,
                ECasa = partido.ErroresCasa,
                EVisita = partido.ErroresVisita,
                EntradaActual = partido.EntradaActual,
                Mitad = partido.Mitad.ToString(),
                Outs = partido.Outs,
                B1 = partido.B1,
                B2 = partido.B2,
                B3 = partido.B3,
                Estado = partido.Estado.ToString()
            };

            return dto;
        }

        private int[] PadArray(int[] src, int length)
        {
            var arr = new int[length];
            for (int i = 0; i < Math.Min(src.Length, length); i++) arr[i] = src[i];
            return arr;
        }
    }
}
