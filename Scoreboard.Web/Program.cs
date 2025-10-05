using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;

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

// Servicios de la aplicación
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService, Scoreboard.Web.Servicios.EstadisticasService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
