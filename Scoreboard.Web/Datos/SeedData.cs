using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Datos
{
    public static class SeedData
    {
        public static async Task EnsureSeedDataAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("SeedData");
            var db = provider.GetRequiredService<ContextoMarcador>();

            // Crear esquema si no existe (solo para development/local)
            try
            {
                await db.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "No se pudo EnsureCreated en la DB de seed.");
            }

            if (await db.Equipos.AnyAsync())
            {
                logger?.LogInformation("Seed: datos ya existen, omitiendo.");
                return;
            }

            logger?.LogInformation("Seed: creando datos de ejemplo...");

            var equipos = new[] {
                new Equipo { Nombre = "Raptors", Ciudad = "San Juan" },
                new Equipo { Nombre = "Cometas", Ciudad = "Carolina" },
                new Equipo { Nombre = "Toros", Ciudad = "Bayamón" }
            };
            db.Equipos.AddRange(equipos);
            await db.SaveChangesAsync();

            var jugadores = new[] {
                new Jugador { Nombre = "María", Apellido = "Gómez", NumeroUniforme = 7, Posicion = Posicion.Utility, EquipoId = equipos[0].Id },
                new Jugador { Nombre = "Ana", Apellido = "Rodríguez", NumeroUniforme = 12, Posicion = Posicion.Lanzador, EquipoId = equipos[1].Id },
                new Jugador { Nombre = "Laura", Apellido = "Pérez", NumeroUniforme = 3, Posicion = Posicion.Utility, EquipoId = equipos[2].Id }
            };
            db.Jugadores.AddRange(jugadores);
            await db.SaveChangesAsync();

            // Partidos en 2023 y 2024
            var partidos = new[] {
                new Partido { Fecha = new DateTime(2023,5,10), EquipoCasaId = equipos[0].Id, EquipoVisitaId = equipos[1].Id, CarrerasCasa = 5, CarrerasVisita = 3 },
                new Partido { Fecha = new DateTime(2023,6,15), EquipoCasaId = equipos[1].Id, EquipoVisitaId = equipos[2].Id, CarrerasCasa = 2, CarrerasVisita = 4 },
                new Partido { Fecha = new DateTime(2024,4,12), EquipoCasaId = equipos[2].Id, EquipoVisitaId = equipos[0].Id, CarrerasCasa = 1, CarrerasVisita = 6 },
                new Partido { Fecha = new DateTime(2024,6,20), EquipoCasaId = equipos[0].Id, EquipoVisitaId = equipos[2].Id, CarrerasCasa = 3, CarrerasVisita = 3 }
            };
            db.Partidos.AddRange(partidos);
            await db.SaveChangesAsync();

            // Estadísticas de bateo por jugador en partidos (mezcla 2023/2024)
            var stats = new[] {
                new PlayerBattingStat { JugadorId = jugadores[0].Id, PartidoId = partidos[0].Id, Fecha = partidos[0].Fecha, AB = 4, H = 2, Doubles = 1, Triples = 0, HR = 0, RBI = 1, BB = 0, SO = 1, HBP = 0, SF = 0 },
                new PlayerBattingStat { JugadorId = jugadores[0].Id, PartidoId = partidos[2].Id, Fecha = partidos[2].Fecha, AB = 3, H = 1, Doubles = 0, Triples = 0, HR = 1, RBI = 2, BB = 1, SO = 0, HBP = 0, SF = 0 },
                new PlayerBattingStat { JugadorId = jugadores[1].Id, PartidoId = partidos[1].Id, Fecha = partidos[1].Fecha, AB = 4, H = 3, Doubles = 0, Triples = 0, HR = 0, RBI = 2, BB = 0, SO = 0, HBP = 0, SF = 0 },
                new PlayerBattingStat { JugadorId = jugadores[2].Id, PartidoId = partidos[3].Id, Fecha = partidos[3].Fecha, AB = 5, H = 2, Doubles = 1, Triples = 0, HR = 0, RBI = 1, BB = 1, SO = 1, HBP = 0, SF = 0 }
            };
            db.PlayerBattingStats.AddRange(stats);
            await db.SaveChangesAsync();

            logger?.LogInformation("Seed: datos de ejemplo creados.");
        }
    }
}
