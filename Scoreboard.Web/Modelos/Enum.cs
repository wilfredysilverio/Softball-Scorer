namespace Scoreboard.Web.Modelos
{
    public enum MitadEntrada
    {
        Alta = 0,
        Baja = 1
    }

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
        LlegaPorError,
        DoblePlay
    }

    public enum EventoCorredor
    {
        Ninguno = 0,
        RoboBase = 1,
        PassedBall = 2,
        WildPitch = 3
    }

    public enum BaseCorredor
    {
        Primera = 1,
        Segunda = 2,
        Tercera = 3
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

    public enum EstadoPartido
    {
        NoIniciado = 0,
        EnCurso = 1,
        Suspendido = 2,
        Finalizado = 3
    }
}
