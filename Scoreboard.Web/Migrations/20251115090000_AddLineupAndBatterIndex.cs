using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Scoreboard.Web.Datos;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ContextoMarcador))]
    [Migration("20251115090000_AddLineupAndBatterIndex")]
    public partial class AddLineupAndBatterIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndexBateadorCasa",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IndexBateadorVisita",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lineups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    EquipoId = table.Column<int>(type: "int", nullable: false),
                    JugadorId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lineups_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lineups_Jugadores_JugadorId",
                        column: x => x.JugadorId,
                        principalTable: "Jugadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lineups_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lineups_EquipoId",
                table: "Lineups",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Lineups_JugadorId",
                table: "Lineups",
                column: "JugadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Lineups_PartidoId",
                table: "Lineups",
                column: "PartidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Lineups_Partido_Equipo_Orden",
                table: "Lineups",
                columns: new[] { "PartidoId", "EquipoId", "Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lineups");

            migrationBuilder.DropColumn(
                name: "IndexBateadorCasa",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "IndexBateadorVisita",
                table: "Partidos");
        }
    }
}
