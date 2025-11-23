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
    public class UndoRedoTests
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

        private async Task<(ContextoMarcador ctx, MarcadorService svc, Partido partido, Jugador j1, Jugador j2)> CreateGameWithLineupAsync()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            var ctx = CreateContext(conn);
            ctx.Database.EnsureCreated();

            var casa = new Equipo { Nombre = "Casa" };
            var vis = new Equipo { Nombre = "Visita" };
            ctx.Equipos.AddRange(casa, vis);
            await ctx.SaveChangesAsync();

            var j1 = new Jugador { Nombre = "J1", Apellido = "A", EquipoId = casa.Id, NumeroUniforme = 10 };
            var j2 = new Jugador { Nombre = "J2", Apellido = "B", EquipoId = casa.Id, NumeroUniforme = 11 };
            ctx.Jugadores.AddRange(j1, j2);
            await ctx.SaveChangesAsync();

            var partido = new Partido
            {
                EquipoCasaId = casa.Id,
                EquipoVisitaId = vis.Id,
                EntradaActual = 1,
                Mitad = MitadEntrada.Baja, // casa batea
                Estado = EstadoPartido.EnCurso,
                IndexBateadorCasa = 0,
                IndexBateadorVisita = null
            };
            ctx.Partidos.Add(partido);
            await ctx.SaveChangesAsync();

            // lineup casa 1-2
            ctx.Lineups.Add(new LineupItem { PartidoId = partido.Id, EquipoId = casa.Id, JugadorId = j1.Id, Orden = 1 });
            ctx.Lineups.Add(new LineupItem { PartidoId = partido.Id, EquipoId = casa.Id, JugadorId = j2.Id, Orden = 2 });
            await ctx.SaveChangesAsync();

            var svc = new MarcadorService(ctx, NullLogger<MarcadorService>.Instance, new FakeHubContext());
            return (ctx, svc, partido, j1, j2);
        }

        [Fact]
        public async Task Undo_Restaura_IndicesDeBateadores()
        {
            var (ctx, svc, partido, j1, j2) = await CreateGameWithLineupAsync();

            // J1 batea un sencillo
            await svc.RegistrarTurnoAsync(partido.Id, j1.Id, ResultadoTurno.Sencillo);
            var pAfter = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(1, pAfter.IndexBateadorCasa); // rota al siguiente

            // Undo debe regresar el índice
            await svc.DeshacerUltimaJugadaAsync(partido.Id);
            var pUndo = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, pUndo.IndexBateadorCasa);
        }

        [Fact]
        public async Task Redo_VuelveAlEstadoPosterior()
        {
            var (ctx, svc, partido, j1, j2) = await CreateGameWithLineupAsync();
            // bases limpias, outs 0
            await svc.RegistrarTurnoAsync(partido.Id, j1.Id, ResultadoTurno.Doble);
            var pAfter = await ctx.Partidos.FindAsync(partido.Id);
            Assert.True(pAfter.B2);
            Assert.False(pAfter.B1);
            var idxAfter = pAfter.IndexBateadorCasa;

            await svc.DeshacerUltimaJugadaAsync(partido.Id);
            var pUndo = await ctx.Partidos.FindAsync(partido.Id);
            Assert.False(pUndo.B1 || pUndo.B2 || pUndo.B3);

            // Redo debe restaurar snapshot After (segundabase ocupada y mismo indice)
            await svc.RehacerUltimaJugadaAsync(partido.Id);
            var pRedo = await ctx.Partidos.FindAsync(partido.Id);
            Assert.True(pRedo.B2);
            Assert.False(pRedo.B1);
            Assert.Equal(idxAfter, pRedo.IndexBateadorCasa);
        }

        [Fact]
        public async Task UndoRedo_RestauraOutsEntradasCarrerasBases()
        {
            var (ctx, svc, partido, j1, _) = await CreateGameWithLineupAsync();
            partido.Outs = 2;
            partido.EntradaActual = 1;
            partido.Mitad = MitadEntrada.Baja;
            partido.B1 = partido.B2 = partido.B3 = true;
            partido.CarrerasCasa = 0;
            await ctx.SaveChangesAsync();

            await svc.RegistrarTurnoAsync(partido.Id, j1.Id, ResultadoTurno.SacrificioFly);
            var despues = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, despues.Outs);
            Assert.Equal(MitadEntrada.Alta, despues.Mitad);
            Assert.Equal(2, despues.EntradaActual);
            Assert.Equal(1, despues.CarrerasCasa);
            Assert.False(despues.B1 || despues.B2 || despues.B3);

            await svc.DeshacerUltimaJugadaAsync(partido.Id);
            var restaurado = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(2, restaurado.Outs);
            Assert.Equal(MitadEntrada.Baja, restaurado.Mitad);
            Assert.Equal(1, restaurado.EntradaActual);
            Assert.Equal(0, restaurado.CarrerasCasa);
            Assert.True(restaurado.B1 && restaurado.B2 && restaurado.B3);

            await svc.RehacerUltimaJugadaAsync(partido.Id);
            var rehecho = await ctx.Partidos.FindAsync(partido.Id);
            Assert.Equal(0, rehecho.Outs);
            Assert.Equal(MitadEntrada.Alta, rehecho.Mitad);
            Assert.Equal(2, rehecho.EntradaActual);
            Assert.Equal(1, rehecho.CarrerasCasa);
            Assert.False(rehecho.B1 || rehecho.B2 || rehecho.B3);
        }
    }
}
