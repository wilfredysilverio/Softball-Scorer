using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    public partial class SincronizarTodo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId",
                table: "Jugadores",
                column: "EquipoId");

            migrationBuilder.Sql(@"
SET @idx := (
  SELECT INDEX_NAME
  FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'Jugadores'
    AND INDEX_NAME = 'IX_Jugadores_EquipoId_Dorsal'
  LIMIT 1
);
SET @sql := IF(@idx IS NOT NULL, 'DROP INDEX `IX_Jugadores_EquipoId_Dorsal` ON `Jugadores`', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
");

            migrationBuilder.Sql(@"
SET @idx2 := (
  SELECT INDEX_NAME
  FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'Equipos'
    AND INDEX_NAME = 'IX_Equipos_Nombre'
  LIMIT 1
);
SET @sql2 := IF(@idx2 IS NOT NULL, 'DROP INDEX `IX_Equipos_Nombre` ON `Equipos`', 'SELECT 1');
PREPARE stmt2 FROM @sql2; EXECUTE stmt2; DEALLOCATE PREPARE stmt2;
");

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

            migrationBuilder.Sql(@"
SET @col := (
  SELECT COLUMN_NAME
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'Jugadores'
    AND COLUMN_NAME = 'Dorsal'
  LIMIT 1
);
SET @sql3 := IF(@col IS NOT NULL, 'ALTER TABLE `Jugadores` DROP COLUMN `Dorsal`', 'SELECT 1');
PREPARE stmt3 FROM @sql3; EXECUTE stmt3; DEALLOCATE PREPARE stmt3;
");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_EquipoId_NumeroUniforme",
                table: "Jugadores",
                columns: new[] { "EquipoId", "NumeroUniforme" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jugadores_EquipoId_NumeroUniforme",
                table: "Jugadores");

            migrationBuilder.AddColumn<int>(
                name: "Dorsal",
                table: "Jugadores",
                type: "int",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Jugadores",
                newName: "Nombres");

            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "Jugadores",
                newName: "Apellidos");

            migrationBuilder.DropColumn(
                name: "NumeroUniforme",
                table: "Jugadores");

            migrationBuilder.DropColumn(
                name: "Posicion",
                table: "Jugadores");

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
