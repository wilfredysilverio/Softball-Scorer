using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Datos;

public class ContextoMarcador : DbContext
{
    public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones) : base(opciones) { }

    // Tablas
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Jugador> Jugadores => Set<Jugador>();
    public DbSet<Partido> Partidos => Set<Partido>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Evitar borrado en cascada entre Partido y Equipo (dos FKs hacia la misma tabla)
        mb.Entity<Partido>()
          .HasOne(p => p.EquipoCasa)
          .WithMany()
          .HasForeignKey(p => p.EquipoCasaId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Partido>()
          .HasOne(p => p.EquipoVisita)
          .WithMany()
          .HasForeignKey(p => p.EquipoVisitaId)
          .OnDelete(DeleteBehavior.Restrict);

        // (Opcional) Índices útiles
        mb.Entity<Jugador>()
          .HasIndex(j => new { j.EquipoId, j.Dorsal });

        mb.Entity<Equipo>()
          .HasIndex(e => e.Nombre);
    }
}
