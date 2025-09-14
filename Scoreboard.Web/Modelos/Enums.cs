using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Models;

public enum MitadEntrada { Alta = 0, Baja = 1 }

public enum ResultadoTurno
{
    Sencillo, Doble, Triple, Jonron, BasePorBolas, Golpe,
    Ponche, OutEnJuego, SacrificioFly, SacrificioToque, LlegaPorError
}

public enum Posicion
{
    Lanzador = 1,
    Receptor = 2,
    PrimeraBase = 3,
    SegundaBase = 4,
    TerceraBase = 5,
    Campocorto = 6,
    JardinIzquierdo = 7,
    JardinCentral = 8,
    JardinDerecho = 9,
    BateadorDesignado = 10,
    Utility = 11
}