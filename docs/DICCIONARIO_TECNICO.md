# Diccionario tecnico del proyecto

Este diccionario explica terminos del framework y del proyecto en espanol simple.

| Termino | Significado simple | En este proyecto |
| --- | --- | --- |
| Account | Cuenta del usuario. | Login, registro, cerrar sesion y acceso denegado. |
| Identity | Sistema de ASP.NET Core para login, usuarios, roles, contrasenas y tokens. | Se usa para proteger el sistema y crear el admin inicial. |
| Areas | Modulos separados dentro de ASP.NET Core. | `Areas/Identity` guarda pantallas especiales de Identity. |
| Pages | Paginas Razor. | Identity usa `Pages/Account` para recuperar/restablecer contrasena. |
| Controller | Clase que recibe acciones del usuario. | `PartidosController`, `JugadoresController`, etc. |
| View | Pantalla que se renderiza al usuario. | Archivos `.cshtml` dentro de `Views`. |
| Model | Entidad o tabla del sistema. | `Jugador`, `Equipo`, `Partido`, `PlayLog`. |
| Service | Logica de negocio reusable. | `MarcadorService`, `EstadisticasService`, `ReportesService`. |
| DTO | Datos que viajan entre capas o hacia JavaScript. | `MarcadorDto` envia el estado del marcador. |
| ViewModel | Datos preparados para una pantalla. | `VerPartidoVm`, `HomeDashboardVm`. |
| DbContext | Puente entre C# y la base de datos. | `ContextoMarcador`. |
| Hub | Comunicacion en tiempo real con SignalR. | `MarcadorHub` avisa cuando cambia el marcador. |
| Razor | Forma de mezclar HTML con C#. | `@model`, `@foreach`, `@if` dentro de `.cshtml`. |
| Migration | Cambio versionado de la estructura de base de datos. | Carpeta `Migrations`. |
| Seed | Datos iniciales automaticos. | Admin inicial, roles y datos de ejemplo. |
| Namespace | Nombre logico donde vive una clase. | `Scoreboard.Web.Servicios.Marcador`. |
| using | Importa clases de otro namespace. | Evita escribir nombres largos. |
| wwwroot | Carpeta publica para archivos del navegador. | CSS, JavaScript, librerias e iconos. |
| SignalR | Tecnologia para tiempo real. | Actualiza marcadores sin refrescar manualmente. |
| EF Core | Entity Framework Core, ORM de .NET. | Convierte clases C# en consultas MySQL. |
| Pomelo MySQL | Proveedor EF Core para MySQL/MariaDB. | Permite que `ContextoMarcador` use MySQL. |
| AJAX/fetch | Peticion desde JavaScript sin recargar pagina. | `marcador.js` envia jugadas y refresca estado. |

## MVC normal vs Razor Pages de Identity

### MVC normal

MVC usa:

- `Controller`: recibe la accion.
- `View`: muestra pantalla.
- `Model/ViewModel`: datos.

Ejemplo:

```text
PartidosController.cs
Views/Partidos/VerPartido.cshtml
VerPartidoVm.cs
```

### Razor Pages de Identity

Razor Pages usa:

- `.cshtml`: pantalla.
- `.cshtml.cs`: logica de esa pantalla.

Ejemplo:

```text
Areas/Identity/Pages/Account/ResetPassword.cshtml
Areas/Identity/Pages/Account/ResetPassword.cshtml.cs
```

No se debe mover Identity a `Controllers` sin una razon clara, porque es una convencion propia de ASP.NET Core.

