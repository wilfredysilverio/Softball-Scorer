using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlayLogs_JugadorId",
                table: "PlayLogs",
                column: "JugadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayLogs_Jugadores_JugadorId",
                table: "PlayLogs",
                column: "JugadorId",
                principalTable: "Jugadores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayLogs_Jugadores_JugadorId",
                table: "PlayLogs");

            migrationBuilder.DropIndex(
                name: "IX_PlayLogs_JugadorId",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");
        }
    }
}
