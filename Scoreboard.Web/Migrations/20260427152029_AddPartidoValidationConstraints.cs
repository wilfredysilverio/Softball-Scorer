using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPartidoValidationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Carreras_NoNegativas",
                table: "Partidos",
                sql: "CarrerasCasa >= 0 AND CarrerasVisita >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Entrada_Outs_Validos",
                table: "Partidos",
                sql: "EntradaActual >= 1 AND Outs >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Equipos_Diferentes",
                table: "Partidos",
                sql: "EquipoCasaId <> EquipoVisitaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Errores_NoNegativos",
                table: "Partidos",
                sql: "ErroresCasa >= 0 AND ErroresVisita >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Partidos_Hits_NoNegativos",
                table: "Partidos",
                sql: "HitsCasa >= 0 AND HitsVisita >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Carreras_NoNegativas",
                table: "Partidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Entrada_Outs_Validos",
                table: "Partidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Equipos_Diferentes",
                table: "Partidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Errores_NoNegativos",
                table: "Partidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Partidos_Hits_NoNegativos",
                table: "Partidos");
        }
    }
}
