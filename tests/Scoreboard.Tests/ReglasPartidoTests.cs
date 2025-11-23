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
    public class ReglasPartidoTests
    {
        private ContextoMarcador CreateContext(SqliteConnection conn)
        {
            var options = new DbContextOptionsBuilder<ContextoMarcador>()
                .UseSqlite(conn)
                .Options;
            return new ContextoMarcador(options);
        }

        private class FakeClientProxy : Microsoft.AspNetCore.SignalR.IClientProxy
        {
            public Task SendAsync(string method, object?[] args, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendCoreAsync(string method, object?[] args, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            public Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
        private class FakeHubContext : Microsoft.AspNetCore.SignalR.IHubContext<Scoreboard.Web.Hubs.MarcadorHub>
        {
            public FakeHubContext() { Clients = new FakeHubClients(); Groups = new FakeGroupManager(); }
            public Microsoft.AspNetCore.SignalR.IHubClients Clients { get; }
            public Microsoft.AspNetCore.SignalR.IGroupManager Groups { get; }
        }

        private async Task<(ContextoMarcador ctx, MarcadorService svc, Partido partido, Jugador bateador)> CreateSampleAsync(int entrada=1, MitadEntrada mitad=MitadEntrada.Alta)
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            var ctx = CreateContext(conn);
            ctx.Database.EnsureCreated();

            var equipoCasa = new Equipo { Nombre = "Casa" };
            var equipoVisita = new Equipo { Nombre = "Visita" };
            ctx.Equipos.AddRange(equipoCasa, equipoVisita);
            await ctx.SaveChangesAsync();

            var jugadorCasa = new Jugador { Nombre = "Juan", Apellido = "Perez", EquipoId = equipoCasa.Id, NumeroUniforme = 10 };
            var jugadorVisita = new Jugador { Nombre = "Luis", Apellido = "Gomez", EquipoId = equipoVisita.Id, NumeroUniforme = 2 };
            ctx.Jugadores.AddRange(jugadorCasa, jugadorVisita);
            await ctx.SaveChangesAsync();

            var partido = new Partido
            {
                EquipoCasaId = equipoCasa.Id,
                EquipoVisitaId = equipoVisita.Id,
                EntradaActual = entrada,
                Mitad = mitad,
                Estado = EstadoPartido.EnCurso
            };
            ctx.Partidos.Add(partido);
            await ctx.SaveChangesAsync();

            ctx.Lineups.Add(new LineupItem { PartidoId = partido.Id, EquipoId = equipoCasa.Id, JugadorId = jugadorCasa.Id, Orden = 1 });
            ctx.Lineups.Add(new LineupItem { PartidoId = partido.Id, EquipoId = equipoVisita.Id, JugadorId = jugadorVisita.Id, Orden = 1 });
            await ctx.SaveChangesAsync();

            var svc = new MarcadorService(ctx, NullLogger<MarcadorService>.Instance, new FakeHubContext());
            var bateador = mitad == MitadEntrada.Baja ? jugadorCasa : jugadorVisita;
            return (ctx, svc, partido, bateador);
        }

        [Fact]
        public async Task CambiaMitadAlLlegarATresOuts()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleAsync(1, MitadEntrada.Baja); // casa batea
            partido.Outs = 2;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Ponche);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, p2.Outs);
            Assert.Equal(MitadEntrada.Alta, p2.Mitad); // cambia a alta
            Assert.Equal(2, p2.EntradaActual); // inicia la siguiente entrada
        }

        [Fact]
        public async Task WalkOff_TerminaPartidoEnBajaSiLocalQuedaArriba()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleAsync(9, MitadEntrada.Baja);
            partido.CarrerasCasa = 0;
            partido.CarrerasVisita = 0;
            partido.B3 = true; // corredor en 3B
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Sencillo);
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(EstadoPartido.Finalizado, p2.Estado);
            Assert.True(p2.CarrerasCasa > p2.CarrerasVisita);
        }

        [Fact]
        public async Task EmpateTras9_AbreEntrada10()
        {
            var (ctx, svc, partido, jugador) = await CreateSampleAsync(9, MitadEntrada.Baja);
            partido.CarrerasCasa = 1;
            partido.CarrerasVisita = 1; // empate
            partido.Outs = 2;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, jugador.Id, ResultadoTurno.Ponche); // tercer out
            var p2 = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(EstadoPartido.EnCurso, p2.Estado);
            Assert.Equal(10, p2.EntradaActual); // extra inning
            Assert.Equal(MitadEntrada.Alta, p2.Mitad);
        }
    }
}
