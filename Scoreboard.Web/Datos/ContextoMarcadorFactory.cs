using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Scoreboard.Web.Datos;

// F�brica para que EF Core pueda crear el Contexto en tiempo de dise�o (migraciones)
// sin depender de AutoDetect ni conectarse realmente al servidor.
public class ContextoMarcadorFactory : IDesignTimeDbContextFactory<ContextoMarcador>
{
    public ContextoMarcador CreateDbContext(string[] args)
    {
        // Carga configuraci�n desde appsettings*.json si est� disponible
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Toma la cadena PorDefecto o usa una de respaldo
        var cadena = config.GetConnectionString("PorDefecto")
            ?? "Server=127.0.0.1;Port=3306;Database=softball;User=root;Password=TU_PASSWORD;TreatTinyAsBoolean=false;SslMode=None;";

        // �IMPORTANTE! Especificamos la versi�n para evitar AutoDetect (no intentar� conectarse)
        var opciones = new DbContextOptionsBuilder<ContextoMarcador>()
            .UseMySql(
                cadena,
                new MySqlServerVersion(new Version(8, 0, 36)) // Ajusta si usas otra versi�n
            )
            .Options;

        return new ContextoMarcador(opciones);
    }
}
