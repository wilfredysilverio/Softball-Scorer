using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Models
{
	public class Equipo
	{
		public int Id { get; set; }
		[Required, StringLength(80)]
		public string Nombre { get; set; } = "Ferreteria Yoel";

		[StringLength(80)]
		public string? Ciudad { get; set; } = "Santiago,Cienfuegos";

	}
}

