using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot_20260427 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
