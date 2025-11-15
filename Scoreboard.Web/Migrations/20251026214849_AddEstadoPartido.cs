using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    public partial class AddEstadoPartido : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartidoId = table.Column<int>(nullable: false),
                    JugadorId = table.Column<int>(nullable: true),
                    Resultado = table.Column<int>(nullable: true),
                    RunsScored = table.Column<int>(nullable: false),
                    Fecha = table.Column<DateTime>(nullable: false),
                    SnapshotJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayLogs_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlayLogs_PartidoId",
                table: "PlayLogs",
                column: "PartidoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayLogs");
        }
    }
}
