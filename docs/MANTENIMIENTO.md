# Mantenimiento del proyecto

Este documento ayuda a mantener el proyecto limpio.

## Objetivo

Que cualquier persona pueda:

- entender donde esta cada cosa,
- corregir errores sin romper reglas,
- agregar mejoras de forma ordenada,
- probar antes de entregar.

## Regla principal

No cambies muchas cosas a la vez.

Ejemplo correcto:

- primero arreglar marcador,
- luego mejorar diseno,
- luego actualizar documentacion.

Ejemplo peligroso:

- cambiar base de datos,
- cambiar vistas,
- cambiar servicios,
- cambiar login,
- cambiar CSS,
- todo en un solo cambio grande sin pruebas.

## Antes de empezar

Ejecuta:

```powershell
dotnet build .\Softball-Scorer.sln
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Si la solucion no muestra detalle del error, compila directo:

```powershell
dotnet build .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet build .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Si ya estaba fallando antes, anota el error.

## Despues de cambiar

Ejecuta:

```powershell
dotnet build .\Softball-Scorer.sln
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Tambien es valido verificar con:

```powershell
dotnet build .\Scoreboard.Web\Scoreboard.Web.csproj
dotnet build .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Tambien prueba manualmente:

1. Login.
2. Lista de partidos.
3. Abrir un partido.
4. Registrar hit.
5. Registrar ponche.
6. Registrar jonron.
7. Ver historial.
8. Ver estadisticas.

## Archivos temporales

No subir:

```text
bin/
obj/
*.log
.vs/
.idea/
```

Ya estan en `.gitignore`.

## Como agregar una nueva jugada

Ejemplo: agregar `TriplePlay`.

Pasos:

1. Agregar valor en `ResultadoTurno`.
2. Agregar logica en `MarcadorService.AplicarResultadoTurno`.
3. Agregar boton o opcion en `VerPartido.cshtml`.
4. Revisar si `marcador.js` necesita ajuste.
5. Agregar prueba en `MarcadorServiceTests.cs`.
6. Ejecutar pruebas.

## Como agregar una nueva estadistica

Pasos:

1. Agregar campo en `PlayerBattingStat` si debe guardarse.
2. Crear migracion.
3. Actualizar `MarcadorService` para llenar el dato.
4. Actualizar `EstadisticasService`.
5. Actualizar vistas de estadisticas.
6. Agregar pruebas.

## Como agregar una nueva pantalla

Pasos:

1. Crear accion en un controller.
2. Crear vista en `Views/<Modulo>/`.
3. Agregar enlace en `_Layout.cshtml` si debe aparecer en el menu.
4. Usar estilos existentes de `site.css`.
5. Probar en computadora y celular.

## Como revisar un bug del marcador

Orden recomendado:

1. Confirmar que el partido esta `EnCurso`.
2. Revisar que existe lineup.
3. Revisar bateador esperado.
4. Revisar POST en `PartidosController.RegistrarTurno`.
5. Revisar `MarcadorService.RegistrarTurnoAsync`.
6. Revisar `AplicarResultadoTurno`.
7. Revisar tabla `Partidos`.
8. Revisar tabla `PlayLogs`.
9. Revisar `marcador.js`.

## Senales de que algo esta mal organizado

- La vista calcula carreras.
- JavaScript decide reglas del juego sin backend.
- Hay codigo duplicado en varios controllers.
- Se cambia una entidad sin migracion.
- Se arregla un bug sin prueba.
- Hay textos mezclados en ingles y espanol sin razon.

## Estilo de texto

Usar espanol claro.

Preferir:

```text
Registrar turno
Definir lineup
Partido en curso
Historial reciente
```

Evitar:

```text
Submit batter event
Game flow status
Advanced runtime action
```

## Estilo visual

Mantener:

- tarjetas simples,
- botones claros,
- tablas limpias,
- buen espacio entre secciones,
- responsive para celular,
- fondo visual suave que no tape contenido.

## Cuando pedir ayuda

Pide revision si vas a tocar:

- migraciones viejas,
- login,
- transacciones,
- reglas de 3 outs,
- deshacer y rehacer jugadas,
- calculo de estadisticas acumuladas.
