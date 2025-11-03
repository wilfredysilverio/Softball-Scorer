using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scoreboard.Web.Datos;
using System;
using System.Linq;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var connection = configuration.GetConnectionString("PorDefecto") ?? Environment.GetEnvironmentVariable("ConnectionStrings__PorDefecto");

        if (string.IsNullOrWhiteSpace(connection))
        {
            // No connection string available; register a minimal context but avoid AutoDetect
            Console.WriteLine("No connection string found (PorDefecto). Set ConnectionStrings:PorDefecto in appsettings or environment.");
            return;
        }

        // Try AutoDetect server version; if it fails we'll still try UseMySql which may attempt auto-detect
        try
        {
            var sv = ServerVersion.AutoDetect(connection);
            services.AddDbContext<ContextoMarcador>(opts => opts.UseMySql(connection, sv));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Warning: ServerVersion.AutoDetect failed: " + ex.Message);
            // Register DbContext with AutoDetect in UseMySql (it may still fail at runtime when opening connection)
            services.AddDbContext<ContextoMarcador>(opts => opts.UseMySql(connection, ServerVersion.AutoDetect(connection)));
        }
    })
    .Build();

using var scope = builder.Services.CreateScope();
var provider = scope.ServiceProvider;
var db = provider.GetService<ContextoMarcador>();

Console.WriteLine("DB Check: PlayerBattingStats");

if (db == null)
{
    Console.WriteLine("DbContext not configured (missing connection string). Exiting.");
    return 0;
}

try
{
    var count = await db.PlayerBattingStats.CountAsync();
    Console.WriteLine($"PlayerBattingStats count: {count}");

    var sample = await db.PlayerBattingStats
        .Include(s => s.Jugador)
        .OrderByDescending(s => s.Fecha)
        .Take(5)
        .ToListAsync();

    foreach (var s in sample)
    {
        Console.WriteLine($"Id:{s.Id} JugadorId:{s.JugadorId} Jugador:{s.Jugador?.Nombre} {s.Jugador?.Apellido} Fecha:{s.Fecha:u} AB:{s.AB} H:{s.H} HR:{s.HR}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error querying DB: " + ex.Message);
    Console.WriteLine("If this is a permission/host error, ensure the DB allows connections from this host and the connection string is correct.");
}

return 0;