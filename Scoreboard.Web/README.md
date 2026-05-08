# Scoreboard.Web

Aplicacion web principal del anotador de softbol.

Si estas entrando al proyecto por primera vez, lee primero:

```text
../README.md
../docs/MAPA_DEL_PROYECTO.md
../docs/ARQUITECTURA.md
../docs/FLUJO_MARCADOR.md
```

## Que contiene esta carpeta

```text
Controllers/  Controladores MVC
Views/        Pantallas Razor
Modelos/      Entidades del sistema
Datos/        DbContext, seed y configuracion de EF
Servicios/    Logica del marcador, estadisticas y reportes
Dtos/         Objetos simples para enviar datos
Helpers/      Funciones auxiliares
Hubs/         SignalR para marcador en vivo
Infra/        Identity, email y servicios de infraestructura
Migrations/   Migraciones de base de datos
wwwroot/      CSS, JavaScript y archivos estaticos
```

## Archivos clave

```text
Program.cs
```

Configura servicios, base de datos, Identity, MVC, SignalR y rutas.

```text
Servicios/Marcador/MarcadorService.cs
```

Contiene las reglas principales del juego.

```text
Controllers/PartidosController.cs
```

Recibe acciones relacionadas con partidos, lineup, marcador y reportes.

```text
Views/Partidos/VerPartido.cshtml
```

Pantalla principal del anotador.

```text
wwwroot/js/marcador.js
```

Actualiza el marcador en vivo desde el navegador.

```text
wwwroot/css/site.css
```

Estilos generales.

## Comandos utiles

Desde esta carpeta:

```powershell
dotnet build
dotnet run --launch-profile http
dotnet ef migrations list
dotnet ef database update
```

Desde la raiz del repo es preferible usar:

```powershell
dotnet build .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet build .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

## Login local

El usuario administrador inicial se configura con secretos locales o variables de entorno:

```powershell
dotnet user-secrets set "Auth:AdminUser" "admin@softball.local"
dotnet user-secrets set "Auth:AdminPass" "CAMBIA_ESTA_CLAVE"
```

## Base de datos local

La cadena `ConnectionStrings:PorDefecto` no debe guardarse con claves reales en GitHub.
Configura la conexion local con `dotnet user-secrets` o con variables de entorno del servidor.

## Nota importante sobre MySQL

El marcador usa transacciones manuales para guardar jugadas completas.

No activar `EnableRetryOnFailure()` en `UseMySql` sin cambiar tambien la forma en que se manejan las transacciones. Si se activa incorrectamente, registrar jugadas puede fallar.
