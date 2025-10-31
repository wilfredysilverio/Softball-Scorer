using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Servicios;
using Scoreboard.Web.Servicios.Marcador;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ContextoMarcador>(opciones =>
{
    var cadena = builder.Configuration.GetConnectionString("PorDefecto");
    opciones.UseMySql(
        cadena,
        ServerVersion.AutoDetect(cadena),
        my => my.EnableRetryOnFailure()
    );
});

builder.Services.AddScoped<IMarcadorService, MarcadorService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService, Scoreboard.Web.Servicios.EstadisticasService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.Marcador.IMarcadorService, Scoreboard.Web.Servicios.Marcador.MarcadorService>();
builder.Services.AddSignalR();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
    // En entornos no-Development forzamos HTTPS
    app.UseHttpsRedirection();
}

// Ejecutar seed de datos de ejemplo solo en Development
if (app.Environment.IsDevelopment())
{
    try
    {
        Scoreboard.Web.Datos.SeedData.EnsureSeedDataAsync(app.Services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
        logger?.LogError(ex, "Error ejecutando seed de datos");
    }
}

// No forzamos redirección HTTPS en Development para facilitar pruebas HTTP locales
app.UseStaticFiles();
app.UseRouting();

app.MapHub<Scoreboard.Web.Hubs.MarcadorHub>("/hubs/marcador");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
