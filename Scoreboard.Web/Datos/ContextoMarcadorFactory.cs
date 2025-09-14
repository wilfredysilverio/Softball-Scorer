using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Scoreboard.Web.Datos;

// Fábrica para que EF Core pueda crear el Contexto en tiempo de diseño (migraciones)
// sin depender de AutoDetect ni conectarse realmente al servidor.
public class ContextoMarcadorFactory : IDesignTimeDbContextFactory<ContextoMarcador>
{
    public ContextoMarcador CreateDbContext(string[] args)
    {
        // Carga configuración desde appsettings*.json si está disponible
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Toma la cadena PorDefecto o usa una de respaldo
        var cadena = config.GetConnectionString("PorDefecto")
            ?? "Server=127.0.0.1;Port=3306;Database=softball;User=root;Password=TU_PASSWORD;TreatTinyAsBoolean=false;SslMode=None;";

        // ¡IMPORTANTE! Especificamos la versión para evitar AutoDetect (no intentará conectarse)
        var opciones = new DbContextOptionsBuilder<ContextoMarcador>()
            .UseMySql(
                cadena,
                new MySqlServerVersion(new Version(8, 0, 36)) // Ajusta si usas otra versión
            )
            .Options;

        return new ContextoMarcador(opciones);
    }
}
