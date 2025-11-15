using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEntradaAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErroresCasa",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ErroresVisita",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HitsCasa",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HitsVisita",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Entradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    NumeroInning = table.Column<int>(type: "int", nullable: false),
                    CarrerasCasa = table.Column<int>(type: "int", nullable: false),
                    CarrerasVisita = table.Column<int>(type: "int", nullable: false),
                    HitsCasa = table.Column<int>(type: "int", nullable: false),
                    HitsVisita = table.Column<int>(type: "int", nullable: false),
                    ErroresCasa = table.Column<int>(type: "int", nullable: false),
                    ErroresVisita = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entradas_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_PartidoId",
                table: "Entradas",
                column: "PartidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entradas");

            migrationBuilder.DropColumn(
                name: "ErroresCasa",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "ErroresVisita",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "HitsCasa",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "HitsVisita",
                table: "Partidos");
        }
    }
}
