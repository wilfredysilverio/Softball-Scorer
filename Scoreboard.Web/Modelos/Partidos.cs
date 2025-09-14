using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Models;

public class Partido
{
    public int Id { get; set; }

    [Required] public int EquipoCasaId { get; set; }
    [Required] public int EquipoVisitaId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public int CarrerasCasa { get; set; }
    public int CarrerasVisita { get; set; }

    public int EntradaActual { get; set; } = 1;
    public MitadEntrada Mitad { get; set; } = MitadEntrada.Alta;
    public int Outs { get; set; } = 0;

    // Bases ocupadas
    public bool B1 { get; set; } = false;
    public bool B2 { get; set; } = false;
    public bool B3 { get; set; } = false;

    public Equipo? EquipoCasa { get; set; }
    public Equipo? EquipoVisita { get; set; }
}
