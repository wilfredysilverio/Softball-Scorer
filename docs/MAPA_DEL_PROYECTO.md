# Mapa del proyecto

Este documento sirve para encontrar rapido donde esta cada cosa.

## Raiz

```text
Softball-Scorer/
```

Contenido:

- `README.md`: entrada principal.
- `Softball-Scorer.sln`: solucion para abrir todo.
- `Scoreboard.Web/`: aplicacion web.
- `tests/`: pruebas.
- `docs/`: documentacion.
- `tools/`: herramientas de apoyo.

## Aplicacion web

```text
Scoreboard.Web/
```

### Controllers

```text
Scoreboard.Web/Controllers/
```

- `AccountController`: login y logout.
- `HomeController`: inicio.
- `EquiposController`: equipos.
- `JugadoresController`: jugadores.
- `PartidosController`: partidos, lineup, marcador y reportes.
- `EstadisticasController`: estadisticas.

### Views

```text
Scoreboard.Web/Views/
```

Pantallas por modulo:

- `Account/`
- `Home/`
- `Equipos/`
- `Jugadores/`
- `Partidos/`
- `Estadisticas/`
- `Shared/`

La pantalla del anotador esta aqui:

```text
Scoreboard.Web/Views/Partidos/VerPartido.cshtml
```

El layout general esta aqui:

```text
Scoreboard.Web/Views/Shared/_Layout.cshtml
```

### Modelos

```text
Scoreboard.Web/Modelos/
```

Entidades principales:

- `Equipo`
- `Jugador`
- `Partido`
- `Entrada`
- `LineupItem`
- `PlayLog`
- `PlayerBattingStat`

Enums:

```text
Scoreboard.Web/Modelos/Enum.cs
```

### Servicios

```text
Scoreboard.Web/Servicios/
```

Servicio principal del marcador:

```text
Scoreboard.Web/Servicios/Marcador/MarcadorService.cs
```

Interfaz:

```text
Scoreboard.Web/Servicios/Marcador/IMarcadorService.cs
```

Otros servicios:

- `EstadisticasService.cs`
- `ReportesService.cs`

### Datos

```text
Scoreboard.Web/Datos/
```

- `ContextoMarcador.cs`: configuracion de EF Core.
- `ContextoMarcadorFactory.cs`: soporte para migraciones.
- `Seed/InitialSeed.cs`: datos iniciales.

### JavaScript

```text
Scoreboard.Web/wwwroot/js/
```

Principal:

```text
Scoreboard.Web/wwwroot/js/marcador.js
```

### CSS

```text
Scoreboard.Web/wwwroot/css/site.css
```

Aqui vive gran parte del estilo general, responsive, tarjetas, tablas, marcador y fondo visual.

### Migraciones

```text
Scoreboard.Web/Migrations/
```

Historial de cambios en la base de datos.

## Pruebas

```text
tests/Scoreboard.Tests/
```

Archivos:

- `MarcadorServiceTests.cs`
- `EstadisticasServiceTests.cs`
- `ReglasPartidoTests.cs`
- `UndoRedoTests.cs`

## Herramientas

```text
tools/
```

- `DbCheck`: ayuda a revisar base de datos.
- `GameSimulator`: simulador de juego.

Estas herramientas son de apoyo. No son necesarias para usar la app normalmente.
