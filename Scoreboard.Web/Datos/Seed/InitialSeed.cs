using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Datos.Seed
{
    public static class InitialSeed
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("InitialSeed");
            var db = sp.GetRequiredService<ContextoMarcador>();

            // Asegurar base creada (no migra, solo asegura para entornos dev/prueba)
            try { await db.Database.EnsureCreatedAsync(); }
            catch (Exception ex) { logger?.LogWarning(ex, "EnsureCreated falló (puede no ser necesario en entornos migrados)"); }

            // Equipos a crear
            var teamsToEnsure = new[]
            {
                new { Nombre = "Tigres de Villa Duarte", Ciudad = (string?)"Santo Domingo Este" },
                new { Nombre = "Los Industriales de Santo Domingo", Ciudad = (string?)"Santo Domingo" }
            };

            var ensuredTeams = new List<Equipo>();
            foreach (var t in teamsToEnsure)
            {
                var team = await db.Equipos.FirstOrDefaultAsync(e => e.Nombre == t.Nombre);
                if (team == null)
                {
                    team = new Equipo { Nombre = t.Nombre, Ciudad = t.Ciudad };
                    db.Equipos.Add(team);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Equipo creado: {Nombre}", t.Nombre);
                }
                ensuredTeams.Add(team);
            }

            // Definición de jugadores por equipo (12 c/u)
            var plantilla1 = new (string Nombre, string Apellido, int Numero, Posicion Pos)[]
            {
                ("Luis", "Morel", 2, Posicion.PrimeraBase),
                ("Kelvin", "Peña", 12, Posicion.SegundaBase),
                ("Daniel", "Paredes", 7, Posicion.Campocorto),
                ("Carlos", "Méndez", 9, Posicion.TerceraBase),
                ("José", "De la Cruz", 23, Posicion.JardinIzquierdo),
                ("Miguel", "Santana", 11, Posicion.JardinCentral),
                ("Rafael", "Gómez", 5, Posicion.JardinDerecho),
                ("Pedro", "Núñez", 17, Posicion.Lanzador),
                ("Juan", "Pérez", 4, Posicion.Receptor),
                ("Manuel", "Rodríguez", 30, Posicion.BateadorDesignado),
                ("Andrés", "Castillo", 14, Posicion.Utility),
                ("Víctor", "Morales", 19, Posicion.Utility),
            };

            var plantilla2 = new (string Nombre, string Apellido, int Numero, Posicion Pos)[]
            {
                ("Alejandro", "Vargas", 1, Posicion.Lanzador),
                ("Cristian", "Peña", 8, Posicion.Receptor),
                ("David", "Martínez", 6, Posicion.PrimeraBase),
                ("Gabriel", "Paredes", 16, Posicion.SegundaBase),
                ("Héctor", "Valdez", 18, Posicion.Campocorto),
                ("Iván", "Rosario", 13, Posicion.TerceraBase),
                ("Javier", "Guzmán", 24, Posicion.JardinIzquierdo),
                ("Kevin", "Lara", 3, Posicion.JardinCentral),
                ("Leonardo", "Fernández", 10, Posicion.JardinDerecho),
                ("Marcos", "Herrera", 21, Posicion.BateadorDesignado),
                ("Nicolás", "Cabrera", 25, Posicion.Utility),
                ("Óscar", "Jiménez", 27, Posicion.Utility),
            };

            async Task EnsurePlayersAsync(int equipoId, (string Nombre, string Apellido, int Numero, Posicion Pos)[] plantilla)
            {
                var existentes = await db.Jugadores.Where(j => j.EquipoId == equipoId).ToListAsync();
                int creados = 0;
                foreach (var p in plantilla)
                {
                    // evitar duplicados por EquipoId + Numero o por nombre completo
                    var yaExiste = existentes.Any(j => j.NumeroUniforme == p.Numero) ||
                                   existentes.Any(j => j.Nombre == p.Nombre && j.Apellido == p.Apellido);
                    if (yaExiste) continue;

                    db.Jugadores.Add(new Jugador
                    {
                        Nombre = p.Nombre,
                        Apellido = p.Apellido,
                        NumeroUniforme = p.Numero,
                        Posicion = p.Pos,
                        EquipoId = equipoId
                        // Activo = true // Requiere columna en BD; agregar migración si se desea persistir
                    });
                    creados++;
                }
                if (creados > 0) await db.SaveChangesAsync();
                if (creados > 0) logger?.LogInformation("Jugadores añadidos al equipo {EquipoId}: {Count}", equipoId, creados);
            }

            if (ensuredTeams.Count >= 2)
            {
                await EnsurePlayersAsync(ensuredTeams[0].Id, plantilla1);
                await EnsurePlayersAsync(ensuredTeams[1].Id, plantilla2);
            }
        }
    }
}