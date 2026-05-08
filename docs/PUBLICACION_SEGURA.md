# Publicacion segura

Esta guia explica que revisar antes de mandar el proyecto a otra persona o publicarlo en un servidor.

## En GitHub

Rama principal:

```text
main
```

Ramas que deben quedar vivas:

```text
main
feature/nowel-frontend-diseno
feature/wilfredy-backend-bd
```

Link del repositorio:

```text
https://github.com/wilfredysilverio/Softball-Scorer
```

## Secretos

No subas claves reales a GitHub.

Configura estos valores en el servidor o en `dotnet user-secrets` local:

```text
ConnectionStrings__PorDefecto
Auth__AdminUser
Auth__AdminPass
Cors__AllowedOrigins__0
```

Ejemplo local:

```powershell
dotnet user-secrets set "ConnectionStrings:PorDefecto" "Server=127.0.0.1;Port=3306;Database=softball;User=softuser;Password=TU_CLAVE_LOCAL;TreatTinyAsBoolean=false;SslMode=None;AllowPublicKeyRetrieval=True;" --project .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet user-secrets set "Auth:AdminUser" "admin@softball.local" --project .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet user-secrets set "Auth:AdminPass" "CAMBIA_ESTA_CLAVE" --project .\Scoreboard.Web\Scoreboard.Web.csproj
```

## Publicar en un servidor

Este proyecto no se puede publicar como GitHub Pages porque es ASP.NET Core con base de datos MySQL.
Necesita un hosting que ejecute .NET y una base de datos MySQL/MariaDB.

Opciones normales:

- Azure App Service + Azure Database for MySQL.
- Render/Railway/Fly.io si soportan .NET y MySQL externo.
- VPS con Linux, Nginx, .NET Runtime y MySQL/MariaDB.

Comando para generar los archivos de publicacion:

```powershell
dotnet publish .\Scoreboard.Web\Scoreboard.Web.csproj -c Release -o .\artifacts\publish\Scoreboard.Web
```

## Antes de compartir el link

- Verifica que GitHub Actions este en verde.
- Verifica que `main` sea la rama por defecto.
- Verifica que no haya claves reales en `appsettings.json`.
- Cambia la clave inicial del admin en el servidor.
- Usa HTTPS.
- Revisa que la base de datos no este expuesta publicamente.
