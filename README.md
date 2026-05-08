# Softball Scorer

Sistema web para anotar partidos de softbol. Sirve para registrar equipos, jugadores, alineaciones, jugadas, carreras, outs, bases, historial y estadisticas del juego.

La idea del proyecto es que un anotador pueda llevar lo que pasa en el terreno desde una computadora o desde un celular.

## Para que sirve

- Crear equipos.
- Crear jugadores.
- Crear partidos.
- Definir quienes jugaran en cada partido.
- Anotar jugadas como hit, doble, triple, jonron, ponche, out y base por bolas.
- Ver bases ocupadas, outs, entrada actual y marcador.
- Ver historial de jugadas.
- Consultar estadisticas.
- Descargar reportes del partido.

## Como esta organizado

```text
Softball-Scorer/
  Scoreboard.Web/          Aplicacion web ASP.NET Core MVC
  tests/Scoreboard.Tests/  Pruebas automaticas
  tools/                   Herramientas de apoyo
  docs/                    Documentacion del proyecto
```

## Leer primero

Si no sabes programacion, empieza por aqui:

- [Guia para usuarios](docs/GUIA_USUARIO.md)
- [Diccionario del proyecto](docs/GLOSARIO.md)

Si vas a programar o revisar el codigo:

- [Guia para desarrolladores](docs/GUIA_DESARROLLADOR.md)
- [Arquitectura](docs/ARQUITECTURA.md)
- [Flujo del marcador](docs/FLUJO_MARCADOR.md)
- [Base de datos](docs/BASE_DE_DATOS.md)
- [Pruebas](docs/PRUEBAS.md)

## Requisitos

- Windows.
- .NET SDK 10.
- XAMPP, MariaDB o MySQL.
- MySQL escuchando en `127.0.0.1:3306`.
- Base de datos `softball`.

## Conexion local segura

La cadena de conexion y la clave del admin no se guardan en `appsettings.json`.
Para desarrollo local se guardan con `dotnet user-secrets`, que no se sube a GitHub.

```powershell
dotnet user-secrets set "ConnectionStrings:PorDefecto" "Server=127.0.0.1;Port=3306;Database=softball;User=softuser;Password=TU_CLAVE_LOCAL;TreatTinyAsBoolean=false;SslMode=None;AllowPublicKeyRetrieval=True;" --project .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet user-secrets set "Auth:AdminUser" "admin@softball.local" --project .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet user-secrets set "Auth:AdminPass" "CAMBIA_ESTA_CLAVE" --project .\Scoreboard.Web\Scoreboard.Web.csproj
```

## Levantar el proyecto

Desde la raiz del proyecto:

```powershell
dotnet build .\Softball-Scorer.sln
dotnet ef database update --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet run --project .\Scoreboard.Web\Scoreboard.Web.csproj --launch-profile http
```

Si el build de la solucion no muestra detalle en Windows, compila los proyectos directos:

```powershell
dotnet build .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet build .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Abrir:

```text
http://localhost:5118/Account/Login
```

Credenciales locales:

```text
Usuario: el valor de Auth:AdminUser
Clave: el valor de Auth:AdminPass
```

## Probar que todo funciona

```powershell
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

## Regla importante

No cambies la logica del marcador directamente en las vistas. Las reglas del juego viven en:

```text
Scoreboard.Web/Servicios/Marcador/MarcadorService.cs
```

Si se cambia una regla importante, tambien debe agregarse o actualizarse una prueba en:

```text
tests/Scoreboard.Tests/MarcadorServiceTests.cs
```
