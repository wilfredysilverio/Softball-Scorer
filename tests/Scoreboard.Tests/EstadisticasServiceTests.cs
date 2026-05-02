using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Servicios;
using Xunit;

namespace Scoreboard.Tests
{
    public class EstadisticasServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ContextoMarcador> _options;

        public EstadisticasServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ContextoMarcador>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ContextoMarcador(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [Fact]
        public async Task ObtenerLineasTemporadaAsync_Computes_AVG_OBP_SLG_Correctly()
        {
            using var context = new ContextoMarcador(_options);

            var jugador = new Jugador { Nombre = "Test", Apellido = "Player", EquipoId = 1 };
            context.Jugadores.Add(jugador);
            context.Equipos.Add(new Equipo { Id = 1, Nombre = "Equipo", Ciudad = "Ciudad" });
            context.Equipos.Add(new Equipo { Id = 2, Nombre = "Rival", Ciudad = "Ciudad" });
            await context.SaveChangesAsync();

            // Añadir dos registros en la misma temporada
            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = jugador.EquipoId,
                PartidoId = 10,
                Fecha = new DateTime(2024, 5, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 4,
                H = 2,
                Doubles = 1,
                Triples = 0,
                HR = 0,
                R = 1,
                RBI = 1,
                BB = 1,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 5
            });

            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = jugador.EquipoId,
                PartidoId = 11,
                Fecha = new DateTime(2024, 6, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 3,
                H = 1,
                Doubles = 0,
                Triples = 0,
                HR = 1,
                R = 2,
                RBI = 2,
                BB = 0,
                SO = 1,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 3
            });

            // Ensure partidos referenced by stats exist (foreign keys)
            context.Partidos.Add(new Partido { Id = 10, EquipoCasaId = 1, EquipoVisitaId = 2, Fecha = new DateTime(2024, 5, 1), CarrerasCasa = 0, CarrerasVisita = 0 });
            context.Partidos.Add(new Partido { Id = 11, EquipoCasaId = 1, EquipoVisitaId = 2, Fecha = new DateTime(2024, 6, 1), CarrerasCasa = 0, CarrerasVisita = 0 });

            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var lineas = await service.ObtenerLineasTemporadaAsync(jugador.Id);

            Assert.Single(lineas);
            var linea = lineas.First();
            Assert.Equal("2024", linea.Temporada);
            Assert.Equal(2, linea.G);
            Assert.Equal(7, linea.AB);
            Assert.Equal(3, linea.H);
            // AVG = 3/7
            var expectedAvg = Math.Round((decimal)3 / 7, 3, MidpointRounding.AwayFromZero);
            Assert.Equal(expectedAvg, linea.AVG);
            // SLG = (H + Doubles + 2*Triples + 3*HR)/AB = (3 + 1 + 0 + 3*1)/7 = (7)/7 = 1.0
            Assert.Equal(1.0m, linea.SLG);
        }

        [Fact]
        public async Task ObtenerEstadisticasJugadorAsync_Returns_Null_When_Player_Not_Found()
        {
            using var context = new ContextoMarcador(_options);
            var service = new EstadisticasService(context);

            var res = await service.ObtenerEstadisticasJugadorAsync(9999);
            Assert.Null(res);
        }

        [Fact]
        public async Task ObtenerLineasTemporadaAsync_Computes_OBP_When_AB_Zero()
        {
            using var context = new ContextoMarcador(_options);

            var jugador = new Jugador { Nombre = "OBP", Apellido = "ZeroAB", EquipoId = 1 };
            context.Jugadores.Add(jugador);
            context.Equipos.Add(new Equipo { Id = 1, Nombre = "Equipo2", Ciudad = "Ciudad" });
            context.Equipos.Add(new Equipo { Id = 2, Nombre = "Rival2", Ciudad = "Ciudad" });
            await context.SaveChangesAsync();

            // AB = 0, pero BB and HBP exist, with SF present -> OBP = (H + BB + HBP)/(AB + BB + HBP + SF)
            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = jugador.EquipoId,
                PartidoId = 20,
                Fecha = new DateTime(2024, 7, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 0,
                H = 0,
                Doubles = 0,
                Triples = 0,
                HR = 0,
                R = 0,
                RBI = 0,
                BB = 2,
                SO = 0,
                HBP = 1,
                SF = 1,
                SH = 0,
                PA = 4
            });

            // Ensure partido referenced by stat exists
            context.Partidos.Add(new Partido { Id = 20, EquipoCasaId = 1, EquipoVisitaId = 2, Fecha = new DateTime(2024, 7, 1), CarrerasCasa = 0, CarrerasVisita = 0 });

            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var lineas = await service.ObtenerLineasTemporadaAsync(jugador.Id);
            Assert.Single(lineas);
            var linea = lineas.First();

            // Here H=0, BB=2, HBP=1, SF=1 => OBP = (0+2+1)/(0+2+1+1) = 3/4 = 0.75
            Assert.Equal(0.75m, Math.Round(linea.OBP, 6));
        }

        [Fact]
        public async Task ObtenerLineasTemporadaAsync_Groups_By_Year_Multiple_Seasons()
        {
            using var context = new ContextoMarcador(_options);

            var jugador = new Jugador { Nombre = "Multi", Apellido = "Season", EquipoId = 2 };
            context.Jugadores.Add(jugador);
            context.Equipos.Add(new Equipo { Id = 2, Nombre = "Equipo3", Ciudad = "Ciudad" });
            context.Equipos.Add(new Equipo { Id = 3, Nombre = "Rival3", Ciudad = "Ciudad" });
            await context.SaveChangesAsync();

            // 2023 stats
            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = jugador.EquipoId,
                PartidoId = 30,
                Fecha = new DateTime(2023, 5, 1),
                Temporada = 2023,
                PartidosJugados = 1,
                AB = 2,
                H = 1,
                Doubles = 0,
                Triples = 0,
                HR = 0,
                BB = 0,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 2
            });

            // 2024 stats
            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = jugador.EquipoId,
                PartidoId = 31,
                Fecha = new DateTime(2024, 6, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 3,
                H = 2,
                Doubles = 1,
                Triples = 0,
                HR = 0,
                BB = 1,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 4
            });

            // Ensure partidos referenced by stats exist
            context.Partidos.Add(new Partido { Id = 30, EquipoCasaId = 2, EquipoVisitaId = 3, Fecha = new DateTime(2023, 5, 1), CarrerasCasa = 0, CarrerasVisita = 0 });
            context.Partidos.Add(new Partido { Id = 31, EquipoCasaId = 2, EquipoVisitaId = 3, Fecha = new DateTime(2024, 6, 1), CarrerasCasa = 0, CarrerasVisita = 0 });

            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var lineas = await service.ObtenerLineasTemporadaAsync(jugador.Id);

            Assert.Equal(2, lineas.Count);
            // Order should be descending, first = 2024
            Assert.Equal("2024", lineas[0].Temporada);
            Assert.Equal(3, lineas[0].AB);
            Assert.Equal("2023", lineas[1].Temporada);
            Assert.Equal(2, lineas[1].AB);
        }

        [Fact]
        public async Task ObtenerEstadisticasEquipoAsync_Aggregates_PlayerStats_For_Team()
        {
            using var context = new ContextoMarcador(_options);

            // Equipo y dos jugadores
            var equipo = new Equipo { Id = 5, Nombre = "TeamTest", Ciudad = "City" };
            context.Equipos.Add(equipo);
            var j1 = new Jugador { Nombre = "P1", Apellido = "A", EquipoId = equipo.Id };
            var j2 = new Jugador { Nombre = "P2", Apellido = "B", EquipoId = equipo.Id };
            context.Jugadores.AddRange(j1, j2);
            await context.SaveChangesAsync();

            // Ensure opponent equipo exists for FK constraints
            context.Equipos.Add(new Equipo { Id = 99, Nombre = "Opponent", Ciudad = "Nowhere" });

            // Create partidos referenced by the batting stats first
            context.Partidos.Add(new Partido { Id = 100, EquipoCasaId = equipo.Id, EquipoVisitaId = 99, Fecha = new DateTime(2024, 4, 1), CarrerasCasa = 1, CarrerasVisita = 0 });
            context.Partidos.Add(new Partido { Id = 101, EquipoCasaId = 99, EquipoVisitaId = equipo.Id, Fecha = new DateTime(2024, 5, 1), CarrerasCasa = 2, CarrerasVisita = 3 });

            // Stats para ambos jugadores in 2024
            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = j1.Id,
                EquipoId = equipo.Id,
                PartidoId = 100,
                Fecha = new DateTime(2024, 4, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 4,
                H = 2,
                Doubles = 0,
                Triples = 0,
                HR = 0,
                BB = 1,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 5
            });

            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = j2.Id,
                EquipoId = equipo.Id,
                PartidoId = 101,
                Fecha = new DateTime(2024, 5, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 3,
                H = 1,
                Doubles = 0,
                Triples = 0,
                HR = 1,
                BB = 0,
                SO = 1,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 3
            });

            // Add one partido played by the team
            context.Partidos.Add(new Partido { Id = 200, EquipoCasaId = equipo.Id, EquipoVisitaId = 99, Fecha = new DateTime(2024, 4, 1), CarrerasCasa = 3, CarrerasVisita = 2 });
            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var vm = await service.ObtenerEstadisticasEquipoAsync(equipo.Id);

            Assert.NotNull(vm);
            Assert.Equal(2, vm.Jugadores);
            Assert.Equal(7, vm.AB);
            Assert.Equal(3, vm.H);
            // AVG = 3/7
            var expectedTeamAvg = Math.Round((decimal)3 / 7, 3, MidpointRounding.AwayFromZero);
            Assert.Equal(expectedTeamAvg, vm.AVG);
            // SLG = (H + Doubles + 2*Triples + 3*HR)/AB = (3 + 0 + 0 + 3*1)/7 = 6/7 ≈ 0.85714
            var expectedTeamSlg = Math.Round(((decimal)6) / 7, 3, MidpointRounding.AwayFromZero);
            Assert.Equal(expectedTeamSlg, vm.SLG);
        }

        [Fact]
        public async Task ObtenerEstadisticasJugadorAsync_Computes_PA_y_OPS()
        {
            using var context = new ContextoMarcador(_options);

            var equipo = new Equipo { Id = 50, Nombre = "OPS", Ciudad = "Ciudad" };
            var oponente = new Equipo { Id = 51, Nombre = "OPS Rival", Ciudad = "Ciudad" };
            context.Equipos.AddRange(equipo, oponente);
            var jugador = new Jugador { Nombre = "Slash", Apellido = "Line", EquipoId = equipo.Id };
            context.Jugadores.Add(jugador);
            await context.SaveChangesAsync();

            context.Partidos.Add(new Partido { Id = 501, EquipoCasaId = equipo.Id, EquipoVisitaId = oponente.Id, Fecha = new DateTime(2024, 8, 1), CarrerasCasa = 0, CarrerasVisita = 0 });

            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = jugador.Id,
                EquipoId = equipo.Id,
                PartidoId = 501,
                Fecha = new DateTime(2024, 8, 1),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 4,
                H = 2,
                Doubles = 1,
                Triples = 0,
                HR = 0,
                R = 1,
                RBI = 2,
                BB = 1,
                SO = 0,
                HBP = 1,
                SF = 1,
                SH = 1,
                PA = 8
            });

            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var vm = await service.ObtenerEstadisticasJugadorAsync(jugador.Id);

            Assert.NotNull(vm);
            Assert.Equal(8, vm!.PA);
            Assert.Equal(1.321m, Math.Round(vm.OPS, 3));
        }

        [Fact]
        public async Task ObtenerEstadisticasEquipoAsync_IncluyeJugadoresDetalleOrdenadosPorOPS()
        {
            using var context = new ContextoMarcador(_options);

            var equipo = new Equipo { Id = 60, Nombre = "Detalle", Ciudad = "Ciudad" };
            var oponente = new Equipo { Id = 61, Nombre = "Oponente", Ciudad = "Ciudad" };
            context.Equipos.AddRange(equipo, oponente);
            var slugger = new Jugador { Nombre = "Slug", Apellido = "One", EquipoId = equipo.Id, NumeroUniforme = 10 };
            var walker = new Jugador { Nombre = "Walk", Apellido = "Two", EquipoId = equipo.Id, NumeroUniforme = 5 };
            context.Jugadores.AddRange(slugger, walker);
            await context.SaveChangesAsync();

            context.Partidos.Add(new Partido { Id = 610, EquipoCasaId = equipo.Id, EquipoVisitaId = oponente.Id, Fecha = new DateTime(2024, 4, 10), CarrerasCasa = 0, CarrerasVisita = 0 });
            context.Partidos.Add(new Partido { Id = 611, EquipoCasaId = oponente.Id, EquipoVisitaId = equipo.Id, Fecha = new DateTime(2024, 4, 11), CarrerasCasa = 0, CarrerasVisita = 0 });

            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = slugger.Id,
                EquipoId = equipo.Id,
                PartidoId = 610,
                Fecha = new DateTime(2024, 4, 10),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 4,
                H = 3,
                Doubles = 1,
                Triples = 0,
                HR = 1,
                RBI = 3,
                R = 3,
                BB = 0,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 4
            });

            context.PlayerBattingStats.Add(new PlayerBattingStat
            {
                JugadorId = walker.Id,
                EquipoId = equipo.Id,
                PartidoId = 611,
                Fecha = new DateTime(2024, 4, 11),
                Temporada = 2024,
                PartidosJugados = 1,
                AB = 2,
                H = 1,
                Doubles = 0,
                Triples = 0,
                HR = 0,
                RBI = 1,
                R = 1,
                BB = 2,
                SO = 0,
                HBP = 0,
                SF = 0,
                SH = 0,
                PA = 4
            });

            await context.SaveChangesAsync();

            var service = new EstadisticasService(context);
            var vm = await service.ObtenerEstadisticasEquipoAsync(equipo.Id);

            Assert.NotNull(vm);
            Assert.Equal(2, vm!.JugadoresDetalle.Count);
            Assert.Equal(slugger.Id, vm.JugadoresDetalle.First().JugadorId);
            Assert.Equal(10, vm.JugadoresDetalle.First().Numero);
            Assert.True(vm.JugadoresDetalle.First().OPS > vm.JugadoresDetalle.Last().OPS);
        }
    }
}
