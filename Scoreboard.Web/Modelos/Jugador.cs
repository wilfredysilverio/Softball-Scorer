using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Models;

public class Jugador
{
	public int Id { get; set; }

	[Required, StringLength(60)]
	public string Nombres { get; set; } = "";

	[Required, StringLength(60)]
	public string Apellidos { get; set; } = "";

	[Range(0, 99)]
	public int? Dorsal { get; set; }

	[Required]
	public int EquipoId { get; set; }
	public Equipo? Equipo { get; set; }

	

}
