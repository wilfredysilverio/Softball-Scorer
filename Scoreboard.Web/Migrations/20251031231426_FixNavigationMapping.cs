using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixNavigationMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Original single-column index IX_Jugadores_EquipoId may not exist on the target DB; skip dropping it.

            migrationBuilder.UpdateData(
                table: "PlayLogs",
                keyColumn: "SnapshotJson",
                keyValue: null,
                column: "SnapshotJson",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotJson",
                table: "PlayLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Resultado",
                table: "PlayLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "JugadorId",
                table: "PlayLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreadoUtc",
                table: "PlayLogs",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "PlayLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattingStats_PartidoId",
                table: "PlayerBattingStats",
                column: "PartidoId");

            // IX_Jugadores_EquipoId_NumeroUniforme already exists in the database; skip creating it to avoid duplicate key error.

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_Nombre",
                table: "Equipos",
                column: "Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerBattingStats_Partidos_PartidoId",
                table: "PlayerBattingStats",
                column: "PartidoId",
                principalTable: "Partidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerBattingStats_Partidos_PartidoId",
                table: "PlayerBattingStats");

            migrationBuilder.DropIndex(
                name: "IX_PlayerBattingStats_PartidoId",
                table: "PlayerBattingStats");

            migrationBuilder.DropIndex(
                name: "IX_Jugadores_EquipoId_NumeroUniforme",
                table: "Jugadores");

            migrationBuilder.DropIndex(
                name: "IX_Equipos_Nombre",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "CreadoUtc",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PlayLogs");

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotJson",
                table: "PlayLogs",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Resultado",
                table: "PlayLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "JugadorId",
                table: "PlayLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId",
                table: "Jugadores",
                column: "EquipoId");
        }
    }
}
