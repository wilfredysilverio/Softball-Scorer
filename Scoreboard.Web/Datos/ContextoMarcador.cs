using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Models;   // Aquí vive ApplicationUser
using Scoreboard.Web.Modelos; // Aquí viven Equipo, Jugador, Partido, etc.

namespace Scoreboard.Web.Datos
{
    // Ahora el contexto usa ApplicationUser
    public class ContextoMarcador : IdentityDbContext<ApplicationUser>
    {
        public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones) : base(opciones) { }

        public DbSet<Equipo> Equipos { get; set; } = default!;
        public DbSet<Jugador> Jugadores { get; set; } = default!;
        public DbSet<Partido> Partidos { get; set; } = default!;
        public DbSet<PlayerBattingStat> PlayerBattingStats { get; set; } = default!;
        public DbSet<Entrada> Entradas { get; set; } = default!;
        public DbSet<PlayLog> PlayLogs { get; set; } = default!;
    public DbSet<LineupItem> Lineups { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // MUY importante: que Identity registre ApplicationUser
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Jugador>()
                .HasOne(j => j.Equipo)
                .WithMany()
                .HasForeignKey(j => j.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lineup configuration
            modelBuilder.Entity<LineupItem>()
                .HasOne(li => li.Partido)
                .WithMany() // we ignore the collections on Partido to avoid ambiguous mapping
                .HasForeignKey(li => li.PartidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LineupItem>()
                .HasOne(li => li.Equipo)
                .WithMany()
                .HasForeignKey(li => li.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LineupItem>()
                .HasOne(li => li.Jugador)
                .WithMany()
                .HasForeignKey(li => li.JugadorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Map LineupCasa and LineupVisita using filtered include via backing table
            modelBuilder.Entity<Partido>()
                .Ignore(p => p.LineupCasa)
                .Ignore(p => p.LineupVisita);

            // Player batting stats metadata
            modelBuilder.Entity<PlayerBattingStat>()
                .HasOne(s => s.Jugador)
                .WithMany()
                .HasForeignKey(s => s.JugadorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerBattingStat>()
                .HasOne(s => s.Equipo)
                .WithMany()
                .HasForeignKey(s => s.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerBattingStat>()
                .HasIndex(s => new { s.JugadorId, s.PartidoId, s.Fecha });

            modelBuilder.Entity<PlayerBattingStat>()
                .HasIndex(s => new { s.JugadorId, s.Temporada });
        }
    }
}
