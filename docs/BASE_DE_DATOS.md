# Base de datos

El proyecto usa MySQL o MariaDB con Entity Framework Core.

## Configuracion local

Archivos:

```text
Scoreboard.Web/appsettings.json
Scoreboard.Web/appsettings.Development.json
```

Cadena local:

```text
Server=127.0.0.1;
Port=3306;
Database=softball;
User=softuser;
Password=ClaveSegura123!;
TreatTinyAsBoolean=false;
SslMode=None;
AllowPublicKeyRetrieval=True;
```

## Revisar si MySQL esta activo

```powershell
Test-NetConnection 127.0.0.1 -Port 3306
```

Debe aparecer:

```text
TcpTestSucceeded : True
```

## DbContext

Archivo:

```text
Scoreboard.Web/Datos/ContextoMarcador.cs
```

Este archivo define las tablas principales y relaciones de Entity Framework.

## Factory de migraciones

Archivo:

```text
Scoreboard.Web/Datos/ContextoMarcadorFactory.cs
```

Sirve para que `dotnet ef` pueda crear migraciones sin depender del arranque completo de la aplicacion.

## Tablas principales

### Equipos

Guarda equipos.

Campos comunes:

- `Id`
- `Nombre`
- `Ciudad`

### Jugadores

Guarda jugadores.

Campos comunes:

- `Id`
- `Nombre`
- `Apellido`
- `NumeroUniforme`
- `Posicion`
- `EquipoId`

### Partidos

Guarda el estado actual del partido.

Campos importantes:

- `EquipoCasaId`
- `EquipoVisitaId`
- `Estado`
- `EntradaActual`
- `Mitad`
- `Outs`
- `B1`
- `B2`
- `B3`
- `CarrerasCasa`
- `CarrerasVisita`
- `HitsCasa`
- `HitsVisita`
- `ErroresCasa`
- `ErroresVisita`
- `IndexBateadorCasa`
- `IndexBateadorVisita`

### Entradas

Guarda las carreras, hits y errores por ini.

Campos importantes:

- `PartidoId`
- `NumeroInning`
- `CarrerasCasa`
- `CarrerasVisita`
- `HitsCasa`
- `HitsVisita`
- `ErroresCasa`
- `ErroresVisita`

### Lineups

Guarda el orden de bateo para un partido.

Campos importantes:

- `PartidoId`
- `EquipoId`
- `JugadorId`
- `Orden`

### PlayLogs

Guarda el historial de jugadas.

Campos importantes:

- `PartidoId`
- `JugadorId`
- `JugadorEsperadoId`
- `EquipoBateoId`
- `Resultado`
- `RunsScored`
- `SnapshotJson`
- `StatDeltaJson`
- `EsCorreccionManual`
- `Nota`
- `IsActive`
- `Kind`

### PlayerBattingStats

Guarda estadisticas ofensivas.

Campos importantes:

- `JugadorId`
- `EquipoId`
- `PartidoId`
- `AB`
- `PA`
- `H`
- `Doubles`
- `Triples`
- `HR`
- `R`
- `RBI`
- `BB`
- `SO`
- `HBP`
- `SF`
- `SH`

## Migraciones

Carpeta:

```text
Scoreboard.Web/Migrations/
```

Listar migraciones:

```powershell
dotnet ef migrations list --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

Crear migracion:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

Aplicar migraciones:

```powershell
dotnet ef database update --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

## Cuando crear una migracion

Crea migracion si cambias:

- una entidad en `Modelos/`,
- una relacion,
- una columna,
- una restriccion,
- un indice,
- una tabla.

No hace falta migracion si solo cambias:

- CSS,
- JavaScript,
- vistas,
- textos,
- documentacion,
- pruebas.

## Validaciones importantes

La base protege varias reglas:

- equipo casa y visitante no pueden ser iguales,
- carreras no pueden ser negativas,
- hits no pueden ser negativos,
- errores no pueden ser negativos,
- entrada debe ser valida,
- outs no pueden ser negativos.

## Errores comunes

### Unknown column

Significa que el modelo espera una columna que la base no tiene.

Solucion:

```powershell
dotnet ef database update --project .\Scoreboard.Web\Scoreboard.Web.csproj --startup-project .\Scoreboard.Web\Scoreboard.Web.csproj
```

### Unable to connect to any MySQL hosts

MySQL esta apagado o el puerto no esta disponible.

Solucion:

- abrir XAMPP,
- iniciar MySQL,
- confirmar puerto 3306.

### Access denied

Usuario o clave incorrecta.

Revisa `appsettings.Development.json`.
