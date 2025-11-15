namespace Scoreboard.Web.Modelos
{
    public enum EstadoPartido
    {
        NoIniciado = 0,
        EnCurso = 1,
        Suspendido = 2,
        Finalizado = 3
    }

    public enum MitadEntrada
    {
        Alta = 0,
        Baja = 1
    }

    // Resultados que usa tu MarcadorService
    public enum ResultadoTurno
    {
        Sencillo,
        Doble,
        Triple,
        Jonron,
        BasePorBolas,
        Golpe,
        Ponche,
        OutEnJuego,
        SacrificioFly,
        SacrificioToque,
        LlegaPorError
    }

    // ÚNICA definición de Posicion con alias para cubrir ambos estilos (P/C/_1B... y nombres en español)
    public enum Posicion
    {
        Desconocida = 0,

        Pitcher = 1, Lanzador = Pitcher, P = Pitcher,
        Catcher = 2, C = Catcher,

        Primera = 3, _1B = Primera,
        Segunda = 4, _2B = Segunda,
        Tercera = 5, _3B = Tercera,

        ShortStop = 6, SS = ShortStop,
        Left = 7, LF = Left,
        Center = 8, CF = Center,
        Right = 9, RF = Right,

        DesignatedHitter = 10, DH = DesignatedHitter,

        Utility = 11
    }
}
