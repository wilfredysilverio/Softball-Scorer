using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarTodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jugadores_EquipoId_Dorsal",
                table: "Jugadores");

            migrationBuilder.DropIndex(
                name: "IX_Equipos_Nombre",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Dorsal",
                table: "Jugadores");

            migrationBuilder.RenameColumn(
                name: "Nombres",
                table: "Jugadores",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "Apellidos",
                table: "Jugadores",
                newName: "Apellido");

            migrationBuilder.AddColumn<int>(
                name: "NumeroUniforme",
                table: "Jugadores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Posicion",
                table: "Jugadores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId",
                table: "Jugadores",
                column: "EquipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jugadores_EquipoId",
                table: "Jugadores");

            migrationBuilder.DropColumn(
                name: "NumeroUniforme",
                table: "Jugadores");

            migrationBuilder.DropColumn(
                name: "Posicion",
                table: "Jugadores");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Jugadores",
                newName: "Nombres");

            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "Jugadores",
                newName: "Apellidos");

            migrationBuilder.AddColumn<int>(
                name: "Dorsal",
                table: "Jugadores",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId_Dorsal",
                table: "Jugadores",
                columns: new[] { "EquipoId", "Dorsal" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_Nombre",
                table: "Equipos",
                column: "Nombre");
        }
    }
}
