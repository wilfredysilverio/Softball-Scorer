using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddJugadorFotoPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilRuta",
                table: "Jugadores",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPerfilRuta",
                table: "Jugadores");
        }
    }
}
