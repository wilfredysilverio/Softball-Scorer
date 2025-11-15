using System;
namespace Scoreboard.Web.Dtos
{
    public class MarcadorDto
    {
        public int PartidoId { get; set; }
        public string EquipoCasa { get; set; } = "";
        public string EquipoVisita { get; set; } = "";
        public int[] CarrerasCasaPorInning { get; set; } = new int[9];
        public int[] CarrerasVisitaPorInning { get; set; } = new int[9];
        public int RCasa { get; set; }
        public int RVisita { get; set; }
        public int HCasa { get; set; }
        public int HVisita { get; set; }
        public int ECasa { get; set; }
        public int EVisita { get; set; }
        public int EntradaActual { get; set; }
        public string Mitad { get; set; } = "Alta";
        public int Outs { get; set; }
        public bool B1 { get; set; }
        public bool B2 { get; set; }
        public bool B3 { get; set; }
        public string Estado { get; set; } = "NoIniciado";
    }
}
