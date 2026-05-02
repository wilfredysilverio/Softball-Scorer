using System;
using System.Collections.Generic;

namespace Scoreboard.Web.Modelos.ViewModels
{
    /// <summary>
    /// ViewModel del dashboard inicial.
    ///
    /// Se conecta con:
    /// - HomeController: que calcula sus valores.
    /// - Views/Home/Index.cshtml: que muestra tarjetas y listados.
    ///
    /// Flujo simple:
    /// 1. Reune totales del sistema.
    /// 2. Incluye partidos/jugadores recientes y lideres.
    /// 3. Permite mostrar un inicio operativo.
    ///
    /// Cuidado:
    /// Si el inicio crece mucho, mover calculos a un servicio.
    /// </summary>
    public class HomeDashboardVm
    {
        public int TotalEquipos { get; set; }
        public int TotalJugadores { get; set; }
        public int TotalPartidos { get; set; }
        public int PartidosEnCurso { get; set; }
        public int CarrerasAnotadas { get; set; }
        public int Jonrones { get; set; }
        public DateTime? ProximoJuego { get; set; }
        public PartidoPanelVm? PartidoPrincipal { get; set; }

        public List<JugadorRecienteVm> JugadoresRecientes { get; set; } = new();
        public List<PartidoRecienteVm> PartidosRecientes { get; set; } = new();
        public List<JugadorTopVm> MejoresJugadores { get; set; } = new();
    }

    public class JugadorRecienteVm
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class PartidoRecienteVm
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
    }

    public class PartidoPanelVm
    {
        public int Id { get; set; }
        public string EquipoCasa { get; set; } = string.Empty;
        public string EquipoVisita { get; set; } = string.Empty;
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
        public int EntradaActual { get; set; }
        public string Mitad { get; set; } = string.Empty;
        public int Outs { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class JugadorTopVm
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
        public int AB { get; set; }
        public int H { get; set; }
        public int HR { get; set; }
        public int RBI { get; set; }
        public decimal AVG { get; set; }
        public decimal OPS { get; set; }
    }
}
