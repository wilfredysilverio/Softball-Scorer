using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ContextoMarcador>(options =>
{
    var cs = builder.Configuration.GetConnectionString("PorDefecto");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// SignalR (tiempo real)
builder.Services.AddSignalR();

// CORS por entorno: lee Cors:AllowedOrigins; si no hay orígenes configurados, se rechazará al iniciar
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?.Where(o => !string.IsNullOrWhiteSpace(o))
    .ToArray();

if (configuredOrigins != null && configuredOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("rt", p =>
        {
            p.WithOrigins(configuredOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        });
    });
}
else
{
    // Registrar el servicio CORS sin política; se lanzará excepción más adelante si faltan orígenes
    builder.Services.AddCors();
}

// Health checks (DB)
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ContextoMarcador>("db");

// Identity con roles y tokens
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ContextoMarcador>()
    .AddDefaultTokenProviders();

// Configurar cookie de Identity (sin registrar un esquema de cookie aparte)
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews(options =>
{
    // Política global: requiere usuario autenticado por defecto
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Razor Pages para Identity (Forgot/Reset password UI)
builder.Services.AddRazorPages();

// IEmailSender: en Development usar DummyEmailSender (log)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, Scoreboard.Web.Infra.Email.DummyEmailSender>();
}

// Servicios de tu app
builder.Services.AddScoped<Scoreboard.Web.Servicios.Marcador.IMarcadorService, Scoreboard.Web.Servicios.Marcador.MarcadorService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService, Scoreboard.Web.Servicios.EstadisticasService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.IReportesService, Scoreboard.Web.Servicios.ReportesService>();

var app = builder.Build();

// Semilla de roles/usuario admin (antes de mapear endpoints)
await Scoreboard.Web.Infra.IdentitySeeder.SeedAsync(app.Services);
// Seed inicial de equipos y jugadores (idempotente)
await Scoreboard.Web.Datos.Seed.InitialSeed.EnsureAsync(app.Services);

// HTTPS opcional en Dev (déjalo activo en Prod)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    // app.UseHttpsRedirection(); // si tus pruebas locales con HTTP simple fallan al redirigir, déjalo comentado
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Verificación de CORS en arranque y logging de orígenes permitidos
var startupOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?.Where(o => !string.IsNullOrWhiteSpace(o))
    .ToArray();
if (startupOrigins == null || startupOrigins.Length == 0)
{
    throw new InvalidOperationException("CORS: falta configurar 'Cors:AllowedOrigins' en appsettings por entorno.");
}
app.Logger.LogInformation("CORS 'rt' orígenes permitidos: {Origins}", string.Join(", ", startupOrigins));

// CORS tras autenticación/autorización y antes de mapear endpoints
app.UseCors("rt");

// Mapa del hub de marcador
app.MapHub<Scoreboard.Web.Hubs.MarcadorHub>("/hubs/marcador");

// Health simple para tiempo real
app.MapGet("/health/realtime", () => Results.Ok(new { hub = "/hubs/marcador", ok = true }));

// Health endpoint público
app.MapHealthChecks("/health");

// Endpoint de desarrollo para crear 2 equipos con 11 jugadores c/u
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/seed-equipos-11", async (ContextoMarcador db) =>
    {
        async Task<(Equipo team, int created)> EnsureTeamWithPlayersAsync(string nombre, string? ciudad)
        {
            var team = await db.Equipos.FirstOrDefaultAsync(e => e.Nombre == nombre);
            if (team == null)
            {
                team = new Equipo { Nombre = nombre, Ciudad = ciudad };
                db.Equipos.Add(team);
                await db.SaveChangesAsync();
            }

            var existing = await db.Jugadores.Where(j => j.EquipoId == team.Id).ToListAsync();
            int created = 0;
            for (int i = 1; i <= 11; i++)
            {
                if (!existing.Any(j => j.NumeroUniforme == i))
                {
                    var posIndex = ((i - 1) % 11) + 1; // 1..11 mapea a Posicion
                    var jugador = new Jugador
                    {
                        Nombre = $"Jugador {i}",
                        Apellido = nombre,
                        NumeroUniforme = i,
                        Posicion = (Posicion)posIndex,
                        EquipoId = team.Id
                    };
                    db.Jugadores.Add(jugador);
                    created++;
                }
            }
            if (created > 0) await db.SaveChangesAsync();
            return (team, created);
        }

        var (t1, c1) = await EnsureTeamWithPlayersAsync("Tigres", "Ciudad A");
        var (t2, c2) = await EnsureTeamWithPlayersAsync("Leones", "Ciudad B");

        var totalT1 = await db.Jugadores.CountAsync(j => j.EquipoId == t1.Id);
        var totalT2 = await db.Jugadores.CountAsync(j => j.EquipoId == t2.Id);

        return Results.Ok(new
        {
            Mensaje = "Seed de equipos completado",
            Equipo1 = new { t1.Id, t1.Nombre, JugadoresCreados = c1, TotalJugadores = totalT1 },
            Equipo2 = new { t2.Id, t2.Nombre, JugadoresCreados = c2, TotalJugadores = totalT2 }
        });
    });
}

// Razor Pages (Identity UI)
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
