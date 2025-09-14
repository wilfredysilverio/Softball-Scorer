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


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
