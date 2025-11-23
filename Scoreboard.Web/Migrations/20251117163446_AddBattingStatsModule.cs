using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBattingStatsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatDeltaJson",
                table: "PlayLogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EquipoId",
                table: "PlayerBattingStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PA",
                table: "PlayerBattingStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartidosJugados",
                table: "PlayerBattingStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SH",
                table: "PlayerBattingStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Temporada",
                table: "PlayerBattingStats",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE PlayerBattingStats p
                INNER JOIN Jugadores j ON j.Id = p.JugadorId
                SET p.EquipoId = j.EquipoId;
            ");

            migrationBuilder.Sql(@"
                UPDATE PlayerBattingStats
                SET Temporada = YEAR(Fecha);
            ");

            migrationBuilder.Sql(@"
                UPDATE PlayerBattingStats
                SET PA = AB + BB + HBP + SF + SH;
            ");

            migrationBuilder.Sql(@"
                WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY JugadorId, PartidoId ORDER BY Fecha, Id) AS rn
                    FROM PlayerBattingStats
                )
                UPDATE PlayerBattingStats p
                INNER JOIN Ranked r ON r.Id = p.Id
                SET p.PartidosJugados = CASE WHEN r.rn = 1 THEN 1 ELSE 0 END;
            ");

            migrationBuilder.Sql(@"
                UPDATE PlayerBattingStats
                SET EquipoId = 0
                WHERE EquipoId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE PlayerBattingStats
                SET Temporada = YEAR(COALESCE(Fecha, UTC_TIMESTAMP()))
                WHERE Temporada IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "EquipoId",
                table: "PlayerBattingStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Temporada",
                table: "PlayerBattingStats",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattingStats_EquipoId",
                table: "PlayerBattingStats",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattingStats_JugadorId_PartidoId_Fecha",
                table: "PlayerBattingStats",
                columns: new[] { "JugadorId", "PartidoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattingStats_JugadorId_Temporada",
                table: "PlayerBattingStats",
                columns: new[] { "JugadorId", "Temporada" });

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerBattingStats_Equipos_EquipoId",
                table: "PlayerBattingStats",
                column: "EquipoId",
                principalTable: "Equipos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerBattingStats_Equipos_EquipoId",
                table: "PlayerBattingStats");

            migrationBuilder.DropIndex(
                name: "IX_PlayerBattingStats_EquipoId",
                table: "PlayerBattingStats");

            migrationBuilder.DropIndex(
                name: "IX_PlayerBattingStats_JugadorId_PartidoId_Fecha",
                table: "PlayerBattingStats");

            migrationBuilder.DropIndex(
                name: "IX_PlayerBattingStats_JugadorId_Temporada",
                table: "PlayerBattingStats");

            migrationBuilder.DropColumn(
                name: "StatDeltaJson",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "EquipoId",
                table: "PlayerBattingStats");

            migrationBuilder.DropColumn(
                name: "PA",
                table: "PlayerBattingStats");

            migrationBuilder.DropColumn(
                name: "PartidosJugados",
                table: "PlayerBattingStats");

            migrationBuilder.DropColumn(
                name: "SH",
                table: "PlayerBattingStats");

            migrationBuilder.DropColumn(
                name: "Temporada",
                table: "PlayerBattingStats");
        }
    }
}
