# Guia para desarrolladores

Esta guia explica como trabajar el proyecto sin romper lo que ya funciona.

## Primer dia en el proyecto

1. Abre la carpeta raiz:

```text
C:\Users\nowel\Documents\GitHub\Softball-Scorer
```

2. Revisa la solucion:

```text
Softball-Scorer.sln
```

3. Lee estos documentos:

- `README.md`
- `docs/ARQUITECTURA.md`
- `docs/FLUJO_MARCADOR.md`
- `docs/BASE_DE_DATOS.md`
- `docs/PRUEBAS.md`

## Comandos basicos

Compilar:

```powershell
dotnet build .\Softball-Scorer.sln
```

Ejecutar pruebas:

```powershell
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Aplicar migraciones:

```powershell
dotnet ef database update --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

Correr la app:

```powershell
dotnet run --project .\Scoreboard.Web\Scoreboard.Web.csproj --launch-profile http
```

Abrir:

```text
http://localhost:5118/Account/Login
```

## Reglas de trabajo

### Antes de cambiar algo

- Ejecuta `dotnet build`.
- Si vas a tocar marcador o estadisticas, ejecuta `dotnet test`.
- Revisa si ya existe un servicio que haga lo que necesitas.
- No dupliques logica en la vista.

### Al cambiar marcador

Toca principalmente:

```text
Scoreboard.Web/Servicios/Marcador/MarcadorService.cs
Scoreboard.Web/Controllers/PartidosController.cs
Scoreboard.Web/Views/Partidos/VerPartido.cshtml
Scoreboard.Web/wwwroot/js/marcador.js
tests/Scoreboard.Tests/MarcadorServiceTests.cs
```

No pongas reglas como "ponche suma out" dentro de JavaScript solamente. JavaScript puede mostrar, pero el backend debe guardar la verdad.

### Al cambiar base de datos

1. Cambia el modelo.
2. Revisa `ContextoMarcador.cs` si hace falta.
3. Crea migracion.
4. Aplica migracion.
5. Corre pruebas.

Comando para crear migracion:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

### Al cambiar diseno

Toca:

```text
Views/
wwwroot/css/site.css
wwwroot/js/
Views/Shared/_Layout.cshtml
```

Mantener:

- nombres en espanol,
- botones claros,
- pantallas responsivas,
- formularios simples,
- tablas faciles de leer.

## Archivos que no se deben editar a mano

No edites manualmente:

```text
bin/
obj/
*.log
```

Las migraciones se pueden leer, pero es mejor generarlas con `dotnet ef migrations add`.

## Que hacer si falla la app

### Error de conexion a MySQL

Revisa que XAMPP/MySQL este prendido.

Prueba:

```powershell
Test-NetConnection 127.0.0.1 -Port 3306
```

Debe decir:

```text
TcpTestSucceeded : True
```

### Error al compilar porque el exe esta en uso

Deten la app:

```powershell
Get-Process dotnet,Scoreboard.Web -ErrorAction SilentlyContinue | Stop-Process -Force
```

Luego compila otra vez.

### Las jugadas no cambian el marcador

Revisar en este orden:

1. `PartidosController.RegistrarTurno`
2. `MarcadorService.RegistrarTurnoAsync`
3. `AplicarResultadoTurno`
4. `wwwroot/js/marcador.js`
5. tabla `PlayLogs`
6. tabla `Partidos`

## Checklist antes de entregar cambios

- `dotnet build` pasa.
- `dotnet test` pasa.
- La app abre en `/Account/Login`.
- Puedes entrar con el usuario local.
- Puedes abrir `/Partidos`.
- Puedes abrir un partido.
- Puedes registrar hit, ponche y jonron.
- La base, out o carrera aparece visualmente.
- No se rompieron estadisticas.
