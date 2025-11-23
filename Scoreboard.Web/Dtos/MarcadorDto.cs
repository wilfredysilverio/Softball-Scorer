using System;
using System.Collections.Generic;
namespace Scoreboard.Web.Dtos
{
    public class MarcadorDto
    {
        public int PartidoId { get; set; }
        public string EquipoCasa { get; set; } = "";
        public string EquipoVisita { get; set; } = "";
        public int[] CarrerasCasaPorInning { get; set; } = Array.Empty<int>();
        public int[] CarrerasVisitaPorInning { get; set; } = Array.Empty<int>();
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }
        public int EntradaActual { get; set; }
        public string Mitad { get; set; } = "Alta";
        public int Outs { get; set; }
        public bool B1 { get; set; }
        public bool B2 { get; set; }
        public bool B3 { get; set; }
        public string Estado { get; set; } = "NoIniciado";
        public List<MarcadorEntradaDto> Entradas { get; set; } = new();
        public MarcadorBateadorDto? BateadorEsperado { get; set; }
        public bool PuedeRegistrar { get; set; }
        public string? MotivoBloqueo { get; set; }
    }

    public class MarcadorEntradaDto
    {
        public int NumeroInning { get; set; }
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
    }

    public class MarcadorBateadorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int EquipoId { get; set; }
        public int? NumeroUniforme { get; set; }
    }
}
