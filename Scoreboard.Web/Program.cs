using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

using Scoreboard.Web.Datos;

using Scoreboard.Web.Models;
using Scoreboard.Web.Servicios;


var builder = WebApplication.CreateBuilder(args);

// ===== DB MySQL (Pomelo) =====
builder.Services.AddDbContext<ContextoMarcador>(opciones =>
{
    var cadena = builder.Configuration.GetConnectionString("PorDefecto");
    opciones.UseMySql(
        cadena,
        ServerVersion.AutoDetect(cadena),
        my => my.EnableRetryOnFailure()
    );
});

// ===== Identity =====
builder.Services.AddDefaultIdentity<ApplicationUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequiredLength = 8;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<ContextoMarcador>()
.AddDefaultTokenProviders(); // opcional pero recomendado

// Cookies: a dónde mandar si falta login
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/Login";
});

// Todo el sitio exige estar autenticado (el Login/Registro deben tener [AllowAnonymous])
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Servicios (si existen en tu solución)
builder.Services.AddScoped<IEstadisticasService, EstadisticasService>();
// builder.Services.AddScoped<IMarcadorService, MarcadorService>(); // si lo usas

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
}

// Seed solo en dev (si tienes SeedData)
if (app.Environment.IsDevelopment())
{
    try
    {
        await Scoreboard.Web.Datos.SeedData.EnsureSeedDataAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Program")
            .LogError(ex, "Error ejecutando seed de datos");
    }
}

// HTTPS + estáticos + auth
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();   // ✅ antes de UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
