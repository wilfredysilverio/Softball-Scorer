using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayLogAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EquipoBateoId",
                table: "PlayLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsCorreccionManual",
                table: "PlayLogs",
                type: "bit(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "JugadorEsperadoId",
                table: "PlayLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "PlayLogs",
                type: "varchar(240)",
                maxLength: 240,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipoBateoId",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "EsCorreccionManual",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "JugadorEsperadoId",
                table: "PlayLogs");

            migrationBuilder.DropColumn(
                name: "Nota",
                table: "PlayLogs");
        }
    }
}
