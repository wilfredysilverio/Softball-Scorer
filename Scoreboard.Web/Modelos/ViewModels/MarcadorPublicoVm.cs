using System.Collections.Generic;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Modelos.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla publica del marcador.
    ///
    /// Se conecta con:
    /// - PartidosController.MarcadorPublico.
    /// - Views/Partidos/MarcadorPublico.cshtml.
    /// - marcador.js para actualizacion en vivo.
    ///
    /// Flujo simple:
    /// 1. Lleva partido, entradas e historial reciente.
    /// 2. Muestra el marcador sin controles de anotador.
    /// 3. Recibe actualizaciones del marcador.
    ///
    /// Cuidado:
    /// Debe mantenerse separado de VerPartidoVm porque es una vista de solo consulta.
    /// </summary>
    public class MarcadorPublicoVm
    {
        public Partido Partido { get; set; } = new();
        public List<Entrada> Entradas { get; set; } = new();
        public List<PlayLog> UltimasJugadas { get; set; } = new();
        public int Outs { get; set; }
    }
}
