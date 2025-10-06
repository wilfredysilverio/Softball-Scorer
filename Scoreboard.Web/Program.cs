using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// 1) DB MySQL (Pomelo) como ya lo tenías
builder.Services.AddDbContext<ContextoMarcador>(opciones =>
{
    var cadena = builder.Configuration.GetConnectionString("PorDefecto");
    opciones.UseMySql(
        cadena,
        ServerVersion.AutoDetect(cadena),
        my => my.EnableRetryOnFailure()
    );
});

<<<<<<< HEAD
// 2) Identity (reglas sencillas en español)
builder.Services.AddDefaultIdentity<ApplicationUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequiredLength = 8;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<ContextoMarcador>();

// 3) Cookies: a dónde mandar si falta login
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/Login";
});

// 4) TODO el sitio exige estar autenticado (como Facebook)
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
=======
// Servicios de la aplicación
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService, Scoreboard.Web.Servicios.EstadisticasService>();

>>>>>>> origin/feature/wilfredy-backend-bd

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // ✅ va a Home/Index


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
