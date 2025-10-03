// USINGS: son instrucciones para poder usar tipos de otros paquetes/espacios de nombres.
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Datos
{
    /// <Resumen>
    /// CLASE: ContextoMarcador
    /// - La clase hereda de DbContext (EF Core).
    /// - Representa la conexión y el modelo de tu base de datos.
    /// </Resumen>
    public class ContextoMarcador : DbContext
    {
        /// <Resumen>
        /// CONSTRUCTOR: recibe las opciones (cadena de conexión, proveedor MySQL, etc.).
        /// Llama al constructor base de DbContext.
        /// </Resumen>
        public ContextoMarcador(DbContextOptions<ContextoMarcador> opciones)
            : base(opciones)
        {
        }

        // ======== DbSet<T> => TABLAS =========
        // PROPIEDAD: Cada DbSet será una tabla en la BD.
        public DbSet<Equipo> Equipos { get; set; } = default!;
        public DbSet<Jugador> Jugadores { get; set; } = default!;
        public DbSet<Partido> Partidos { get; set; } = default!;

        /// <Resumen>
        /// MÉTODO: OnModelCreating
        /// - Configura relaciones y reglas entre entidades/tablas (Fluent API).
        /// </Resumen>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================== RELACIONES ==========================
            // PARTIDO -> EQUIPO (Casa)
            // Un Partido tiene un EquipoCasa (FK: EquipoCasaId).
            // WithMany() sin colección inversa (no tenemos List<Partido> en Equipo).
            // OnDelete(Restict) evita borrado en cascada que chocaría con EquipoVisita.
            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoCasa)
                .WithMany()
                .HasForeignKey(p => p.EquipoCasaId)
                .OnDelete(DeleteBehavior.Restrict);

            // PARTIDO -> EQUIPO (Visita)
            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoVisita)
                .WithMany()
                .HasForeignKey(p => p.EquipoVisitaId)
                .OnDelete(DeleteBehavior.Restrict);

            // JUGADOR -> EQUIPO
            // Un Jugador pertenece a un Equipo (FK: EquipoId).
            modelBuilder.Entity<Jugador>()
                .HasOne(j => j.Equipo)
                .WithMany()
                .HasForeignKey(j => j.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===================== RESTRICCIONES/VALIDACIONES =====================
            // modelBuilder.Entity<Equipo>()
            //     .Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            //
            // modelBuilder.Entity<Partido>()
            //     .Property(p => p.EntradaActual).HasDefaultValue(1);
            //
            // modelBuilder.Entity<Partido>()
            //     .Property(p => p.Mitad).HasDefaultValue(MitadEntrada.Alta);
        }
    }
}
