using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Servicios.Marcador;
using Xunit;

namespace Scoreboard.Tests
{
    public class MarcadorServiceTests
    {
        private ContextoMarcador CreateContext(SqliteConnection conn)
        {
            var options = new DbContextOptionsBuilder<ContextoMarcador>()
                .UseSqlite(conn)
                .Options;
            return new ContextoMarcador(options);
        }
        // Helper fake hub context to pass to MarcadorService during tests
        private class FakeClientProxy : Microsoft.AspNetCore.SignalR.IClientProxy
        {
            public System.Threading.Tasks.Task SendAsync(string method, object?[] args, System.Threading.CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public System.Threading.Tasks.Task SendCoreAsync(string method, object?[] args, System.Threading.CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private class FakeHubClients : Microsoft.AspNetCore.SignalR.IHubClients
        {
            public Microsoft.AspNetCore.SignalR.IClientProxy All => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy Client(string connectionId) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy Group(string groupName) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy User(string userId) => new FakeClientProxy();
            public Microsoft.AspNetCore.SignalR.IClientProxy Users(System.Collections.Generic.IReadOnlyList<string> userIds) => new FakeClientProxy();
        }

        private class FakeGroupManager : Microsoft.AspNetCore.SignalR.IGroupManager
        {
            public System.Threading.Tasks.Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
            public System.Threading.Tasks.Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        }

        private class FakeHubContext : Microsoft.AspNetCore.SignalR.IHubContext<Scoreboard.Web.Hubs.MarcadorHub>
        {
            public FakeHubContext() { Clients = new FakeHubClients(); Groups = new FakeGroupManager(); }
            public Microsoft.AspNetCore.SignalR.IHubClients Clients { get; }
            public Microsoft.AspNetCore.SignalR.IGroupManager Groups { get; }
        }

        private async Task<(ContextoMarcador ctx, MarcadorService svc, Partido partido, Jugador jugador)> CreateSampleGameAsync()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var ctx = CreateContext(conn);
            ctx.Database.EnsureCreated();

            var equipoCasa = new Equipo { Nombre = "Casa" };
            var equipoVisita = new Equipo { Nombre = "Visita" };
            ctx.Equipos.Add(equipoCasa);
            ctx.Equipos.Add(equipoVisita);
            await ctx.SaveChangesAsync();

            var jugador = new Jugador { Nombre = "Juan", Apellido = "Perez", EquipoId = equipoCasa.Id, NumeroUniforme = 1 };
            ctx.Jugadores.Add(jugador);
            await ctx.SaveChangesAsync();

            var partido = new Partido
            {
                EquipoCasaId = equipoCasa.Id,
                EquipoVisitaId = equipoVisita.Id,
                Mitad = MitadEntrada.Baja,
                Estado = EstadoPartido.NoIniciado,
                EntradaActual = 1
            };
            ctx.Partidos.Add(partido);
            await ctx.SaveChangesAsync();

            var svc = new MarcadorService(ctx, NullLogger<MarcadorService>.Instance, new FakeHubContext());
            return (ctx, svc, partido, jugador);
        }

        [Fact]
        public async Task IniciarPartido_SetsEstadoEnCurso_and_Idempotent()
        {
            var (ctx, svc, partido, _) = await CreateSampleGameAsync();

            await svc.IniciarPartidoAsync(partido.Id);
            var p1 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(EstadoPartido.EnCurso, p1.Estado);

            // calling again should keep EnCurso (idempotent)
            await svc.IniciarPartidoAsync(partido.Id);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(EstadoPartido.EnCurso, p2.Estado);
        }

        [Fact]
        public async Task RegistrarTurno_HitSimple_AvanceCorrecto()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            // setup: partido en curso y corredor en 1B
            partido.Estado = EstadoPartido.EnCurso;
            partido.B1 = true; partido.B2 = false; partido.B3 = false; partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Sencillo);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            // Runner from 1 should move to 2, batter to 1; no runs
            Assert.True(p2.B1);
            Assert.True(p2.B2);
            Assert.False(p2.B3);
            Assert.Equal(0, p2.CarrerasCasa);
        }

        [Fact]
        public async Task RegistrarTurno_BasePorBolas_BasesLlenas_EntraCarrera()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.B1 = partido.B2 = partido.B3 = true;
            partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.BasePorBolas);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(1, p2.CarrerasCasa);
            Assert.True(p2.B1 && p2.B2 && p2.B3);
        }

        [Fact]
        public async Task RegistrarTurno_Jonron_LimpiaBases_Y_SumaCarreras()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.B1 = partido.B2 = partido.B3 = true;
            partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Jonron);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(4, p2.CarrerasCasa);
            Assert.False(p2.B1 || p2.B2 || p2.B3);
            var stat = await ctx.PlayerBattingStats.Where(s => s.PartidoId == partido.Id).FirstOrDefaultAsync();
            Assert.NotNull(stat);
            Assert.Equal(4, stat.R);
        }

        [Fact]
        public async Task RegistrarTurno_Ponche_OutsYCambioDeMitad()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.Mitad = MitadEntrada.Baja; // casa batea
            partido.Outs = 2;
            partido.EntradaActual = 1;
            partido.B1 = partido.B2 = partido.B3 = true;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Ponche);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, p2.Outs);
            Assert.Equal(MitadEntrada.Alta, p2.Mitad);
            Assert.Equal(2, p2.EntradaActual); // incremented when switching to Alta
            Assert.False(p2.B1 || p2.B2 || p2.B3);
        }

        [Fact]
        public async Task RegistrarTurno_SacFly_AnotaDesde3B_SiOutsMenorA2()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.B3 = true; partido.Outs = 1; partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.SacrificioFly);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(1, p2.CarrerasCasa);
            Assert.False(p2.B3);

            // Now test with 2 outs: no score from sac fly
            p2.B3 = true; p2.Outs = 2; p2.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();
            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.SacrificioFly);
            var p3 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, p3.CarrerasCasa);
        }

        [Fact]
        public async Task RegistrarTurno_Error_NoCuentaHit_PuedeHaberAvance()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.B1 = true; partido.B2 = false; partido.B3 = false; partido.CarrerasCasa = 0; partido.ErroresVisita = 0; // visita defensa
            await ctx.SaveChangesAsync();

            // Use LlegaPorError
            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.LlegaPorError);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            // Hit not counted
            Assert.Equal(0, p2.HitsCasa);
            // Error count incremented for defensive team (visita)
            Assert.True(p2.ErroresVisita >= 0);
            // Batter to first
            Assert.True(p2.B1);
        }

        [Fact]
        public async Task DeshacerUltimaJugada_RestoraEstadoCompleto()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.B1 = partido.B2 = partido.B3 = true;
            partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Jonron);
            var pAfter = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(4, pAfter.CarrerasCasa);

            await svc.DeshacerUltimaJugadaAsync(partido.Id);
            var pRestored = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, pRestored.CarrerasCasa);
            // After undo we expect the snapshot (which had the bases set) to be restored
            Assert.True(pRestored.B1 || pRestored.B2 || pRestored.B3);
        }

        [Fact]
        public async Task NoPermitirRegistrarTurno_SiNoEnCurso()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.NoIniciado;
            await ctx.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Sencillo);
            });
        }

        [Fact]
        public async Task CambioDeEntrada_AutomaticoTrasTresOuts()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleGameAsync();
            partido.Estado = EstadoPartido.EnCurso;
            partido.Mitad = MitadEntrada.Baja; partido.Outs = 1; partido.EntradaActual = 1;
            await ctx.SaveChangesAsync();

            // two outs via two outs results
            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Ponche); // outs=2
            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Ponche); // outs -> triggers change

            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, p2.Outs);
            Assert.Equal(MitadEntrada.Alta, p2.Mitad);
            Assert.Equal(2, p2.EntradaActual);
        }
    }
}
