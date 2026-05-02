using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Datos
{
    /// <summary>
    /// DbContext principal del sistema.
    ///
    /// Se conecta con:
    /// - Modelos: Equipo, Jugador, Partido, Entrada, PlayLog, LineupItem y PlayerBattingStat.
    /// - ASP.NET Core Identity: para usuarios y roles.
    /// - MySQL/MariaDB: mediante Pomelo.EntityFrameworkCore.MySql.
    ///
    /// Flujo simple:
    /// 1. Define que tablas existen en la base de datos.
    /// 2. Configura relaciones y reglas de integridad.
    /// 3. Permite que controladores y servicios lean/escriban datos.
    ///
    /// Cuidado:
    /// Cambiar relaciones, nombres o propiedades puede requerir migraciones y afectar datos existentes.
    /// </summary>
    public class ContextoMarcador : IdentityDbContext<IdentityUser, IdentityRole, string>
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

            modelBuilder.Entity<Partido>()
                .ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Partidos_Equipos_Diferentes", "EquipoCasaId <> EquipoVisitaId");
                    tb.HasCheckConstraint("CK_Partidos_Carreras_NoNegativas", "CarrerasCasa >= 0 AND CarrerasVisita >= 0");
                    tb.HasCheckConstraint("CK_Partidos_Hits_NoNegativos", "HitsCasa >= 0 AND HitsVisita >= 0");
                    tb.HasCheckConstraint("CK_Partidos_Errores_NoNegativos", "ErroresCasa >= 0 AND ErroresVisita >= 0");
                    tb.HasCheckConstraint("CK_Partidos_Entrada_Outs_Validos", "EntradaActual >= 1 AND Outs >= 0");
                });

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
