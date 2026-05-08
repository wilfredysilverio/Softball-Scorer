# Guia para seguir escalando el proyecto

Esta guia indica como crecer el proyecto sin romper lo que ya funciona.

## Reglas de oro

1. No cambies nombres de carpetas de ASP.NET Core por gusto.
2. No muevas `Areas/Identity` si solo quieres entenderlo.
3. Si cambias una clase usada por EF Core, revisa migraciones.
4. Si cambias `MarcadorDto`, revisa `marcador.js`.
5. Si cambias ids HTML en vistas del marcador, revisa `marcador.js`.
6. Si cambias reglas de juego, agrega o actualiza pruebas.
7. Despues de cada bloque importante, ejecuta `dotnet build`.

## Como agregar una nueva funcion

Ejemplo: agregar una nueva jugada.

1. Revisar si ya existe en `ResultadoTurno`.
2. Agregar la regla en `MarcadorService.AplicarResultadoTurno`.
3. Guardar datos necesarios en `PlayLog` si aplica.
4. Actualizar la vista `VerPartido.cshtml` si hace falta boton o select.
5. Actualizar `marcador.js` solo si cambia la interaccion visual.
6. Crear prueba en `MarcadorServiceTests`.
7. Ejecutar `dotnet build` y `dotnet test`.

## Donde poner codigo nuevo

| Si vas a crear... | Ponlo en... |
| --- | --- |
| Regla de negocio | `Servicios` |
| Entidad de base de datos | `Modelos` |
| Pantalla MVC | `Views/<Controlador>` |
| Datos para una pantalla | `Modelos/ViewModels` o `ViewModels` segun el modulo |
| Datos para JavaScript/API | `Dtos` |
| Funcion pequena reusable | `Helpers` |
| Estilo visual | `wwwroot/css/site.css` |
| Interaccion del navegador | `wwwroot/js` |

## Cuando mover CSS o JavaScript

Mover a `wwwroot/css` si:

- el estilo se repite
- pertenece al diseno general
- ensucia mucho la vista

Mover a `wwwroot/js` si:

- hay muchos eventos
- la logica se usa en mas de una vista
- la vista queda dificil de leer

Mantener en `.cshtml` solo si:

- es muy pequeno
- necesita valores Razor directos
- no se repite

## Flujo recomendado para limpiar

1. Buscar referencias antes de borrar.
2. Borrar solo lo que no tenga referencias y no sea convencion del framework.
3. Compilar.
4. Ejecutar pruebas.
5. Probar manualmente en navegador.

## Pruebas manuales minimas

Despues de tocar el marcador:

1. Entrar al login.
2. Abrir un partido.
3. Registrar un hit.
4. Confirmar base ocupada.
5. Registrar un ponche.
6. Confirmar out.
7. Llegar a 3 outs.
8. Confirmar cambio de equipo al bate.
9. Abrir historial.
10. Descargar box score o jugada por jugada.

## Zonas de riesgo

| Zona | Riesgo |
| --- | --- |
| MySQL/XAMPP | Si no esta corriendo, la app muestra error 500. |
| `MarcadorService` | Puede cambiar resultados del partido. |
| `ContextoMarcador` | Puede romper migraciones o tablas. |
| `Program.cs` | Puede impedir login, rutas o conexion DB. |
| `Areas/Identity` | Puede romper recuperacion de contrasena. |

