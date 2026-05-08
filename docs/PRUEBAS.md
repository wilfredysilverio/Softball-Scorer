# Pruebas

El proyecto tiene pruebas automaticas para proteger reglas importantes.

## Carpeta de pruebas

```text
tests/Scoreboard.Tests/
```

## Ejecutar pruebas

Desde la raiz:

```powershell
dotnet test .\tests\Scoreboard.Tests\Scoreboard.Tests.csproj
```

Tambien puedes ejecutar todo desde la solucion:

```powershell
dotnet test .\Softball-Scorer.sln
```

## Archivos principales

### MarcadorServiceTests.cs

Prueba reglas del marcador:

- iniciar partido,
- registrar hit,
- registrar ponche,
- registrar jonron,
- bases ocupadas,
- carreras,
- cambio de entrada,
- lineup,
- bateador fuera de turno,
- deshacer jugada.

### EstadisticasServiceTests.cs

Prueba calculos de estadisticas.

### ReglasPartidoTests.cs

Prueba reglas generales de partido.

### UndoRedoTests.cs

Prueba deshacer y rehacer jugadas.

## Cuando agregar una prueba

Agrega una prueba cuando:

- cambias una regla del juego,
- cambias como se calculan carreras,
- cambias outs,
- cambias bases,
- cambias orden de bateo,
- cambias estadisticas,
- arreglas un bug importante.

## Casos minimos que siempre deben funcionar

### Hit

Resultado esperado:

- bateador llega a primera,
- no suma out,
- avanza el turno,
- se guarda historial.

### Ponche

Resultado esperado:

- suma 1 out,
- suma `SO`,
- no mueve bases,
- avanza el turno.

### Jonron

Resultado esperado:

- anota el bateador,
- anotan corredores en base,
- bases quedan limpias,
- marcador sube.

### Tres outs

Resultado esperado:

- cambia de alta a baja, o de baja al siguiente ini,
- bases quedan limpias,
- outs vuelven a 0.

### Bateador fuera de turno

Resultado esperado:

- sin confirmacion se rechaza,
- con confirmacion se guarda como correccion manual.

## Si una prueba falla

Lee el nombre de la prueba. Normalmente dice que regla se rompio.

Ejemplo:

```text
RegistrarTurno_Ponche_SumaOutYEstadistica
```

Eso indica que el problema esta relacionado con ponche, outs o estadisticas.

## Advertencia

No borres pruebas para que el proyecto compile.

Si una prueba falla, primero entiende que regla protege. Si la regla cambio a proposito, actualiza la prueba con cuidado.
