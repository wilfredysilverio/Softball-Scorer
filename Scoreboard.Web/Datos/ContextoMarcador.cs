using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Datos
{
    // Ahora heredamos de IdentityDbContext para que EF cree las tablas de usuarios/roles
    public class ContextoMarcador : IdentityDbContext<ApplicationUser>
    {
        public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones)
            : base(opciones) { }

        // Tus tablas de dominio
        public DbSet<Equipo> Equipos { get; set; } = default!;
        public DbSet<Jugador> Jugadores { get; set; } = default!;
        public DbSet<Partido> Partidos { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ¡IMPORTANTE! Deja que Identity configure sus tablas/relaciones
            base.OnModelCreating(modelBuilder);

            // Relaciones propias del dominio (las que ya tenías)
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
