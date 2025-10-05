using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Datos
{
    public class ContextoMarcador : DbContext
    {
        public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones)
            : base(opciones) { }

        public DbSet<Equipo> Equipos { get; set; } = default!;
        public DbSet<Jugador> Jugadores { get; set; } = default!;
        public DbSet<Partido> Partidos { get; set; } = default!;
    public DbSet<Scoreboard.Web.Modelos.PlayerBattingStat> PlayerBattingStats { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<PlayerBattingStat>()
                .HasOne(p => p.Jugador)
                .WithMany()
                .HasForeignKey(p => p.JugadorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
