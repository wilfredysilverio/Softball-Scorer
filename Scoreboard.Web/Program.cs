using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ContextoMarcador>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("PorDefecto"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("PorDefecto")));
});

// Identity (completo, con roles y tokens)
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

// Cookie auth (ruta de login/denegado)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Scoreboard.Web.Servicios.Marcador.IMarcadorService, Scoreboard.Web.Servicios.Marcador.MarcadorService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService, Scoreboard.Web.Servicios.EstadisticasService>();


var app = builder.Build();

// Solo en dev, evita forzar HTTPS si te da lío con certificados
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Orden correcto del pipeline
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
