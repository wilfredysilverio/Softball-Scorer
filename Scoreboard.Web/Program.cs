using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Models; // <-- AQUÍ: Models (donde está ApplicationUser)

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ContextoMarcador>(options =>
{
    var cs = builder.Configuration.GetConnectionString("PorDefecto");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// Identity con roles y tokens
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
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

// MVC con política global: requiere usuario autenticado por defecto
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Servicios de tu app
builder.Services.AddScoped<Scoreboard.Web.Servicios.Marcador.IMarcadorService,
                           Scoreboard.Web.Servicios.Marcador.MarcadorService>();
builder.Services.AddScoped<Scoreboard.Web.Servicios.IEstadisticasService,
                           Scoreboard.Web.Servicios.EstadisticasService>();

var app = builder.Build();

// HTTPS opcional en Dev (déjalo activo en Prod)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    // app.UseHttpsRedirection(); // si te da lío en local, lo dejas comentado
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Semilla solo en Desarrollo
if (app.Environment.IsDevelopment())
{
    await Scoreboard.Web.Infra.IdentitySeeder.SeedAsync(app.Services);
}

app.Run();
