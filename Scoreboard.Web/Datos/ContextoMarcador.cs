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
        }
    }
}
