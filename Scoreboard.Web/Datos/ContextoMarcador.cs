using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.Identity;

namespace Scoreboard.Web.Datos
{
    public class ContextoMarcador : IdentityDbContext<Scoreboard.Web.Modelos.Identity.UsuarioAplicacion, Scoreboard.Web.Modelos.Identity.RolAplicacion, string>
    {
        public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones)
            : base(opciones)
        {
        }

        // ========== Tablas ==========
        public DbSet<Equipo> Equipos { get; set; } = default!;
        public DbSet<Jugador> Jugadores { get; set; } = default!;
        public DbSet<Partido> Partidos { get; set; } = default!;
        public DbSet<Entrada> Entradas { get; set; } = default!;
        public DbSet<PlayLog> PlayLogs { get; set; } = default!;
        public DbSet<PlayerBattingStat> PlayerBattingStats { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EQUIPO
            modelBuilder.Entity<Equipo>()
                .HasIndex(e => e.Nombre);

            // JUGADOR -> EQUIPO
            modelBuilder.Entity<Jugador>()
                .HasOne(j => j.Equipo)
                .WithMany()                     // Si tienes Equipo.Jugadores, puedes cambiar a .WithMany(e => e.Jugadores)
                .HasForeignKey(j => j.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);

            // �ndice �til (no unique por si acaso)
            modelBuilder.Entity<Jugador>()
                .HasIndex(j => new { j.EquipoId, j.NumeroUniforme })
                .IsUnique(false);

            // PARTIDO -> EQUIPOS
            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoCasa)
                .WithMany()
                .HasForeignKey(p => p.EquipoCasaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoVisita)
                .WithMany()
                .HasForeignKey(p => p.EquipoVisitaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ENTRADA -> PARTIDO (use explicit navigations)
            modelBuilder.Entity<Entrada>()
                .HasOne(e => e.Partido)
                .WithMany(p => p.Entradas)
                .HasForeignKey(e => e.PartidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PLAYLOG -> PARTIDO
            modelBuilder.Entity<PlayLog>()
                .Property(p => p.SnapshotJson)
                .HasColumnType("longtext");     // MySQL/Pomelo: JSON grande

            modelBuilder.Entity<PlayLog>()
                .HasOne<Partido>()
                .WithMany()                     // Si tienes Partido.PlayLogs, cambia a .WithMany(p => p.PlayLogs)
                .HasForeignKey(pl => pl.PartidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PlayerBattingStat -> Jugador/Partido (explicit navigations)
            modelBuilder.Entity<PlayerBattingStat>()
                .HasOne(s => s.Jugador)
                .WithMany()
                .HasForeignKey(s => s.JugadorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerBattingStat>()
                .HasOne(s => s.Partido)
                .WithMany(p => p.PlayerBattingStats)
                .HasForeignKey(s => s.PartidoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
