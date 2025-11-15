DbCheck
=======

Herramienta pequeña para verificar la existencia y contenido de la tabla `PlayerBattingStats` en la base de datos.

Cómo usar
---

Opciones para proporcionar la cadena de conexión (key: `ConnectionStrings:PorDefecto`):

1) Variable de entorno (recomendada para evitar poner contraseñas en archivos):

```powershell
Set-Item -Path Env:ConnectionStrings__PorDefecto -Value "server=DB_HOST;user=DB_USER;password=DB_PASS;database=DB_NAME;TreatTinyAsBoolean=false"
Set-Location "C:\Users\Usuario\Desktop\Proyecto Final\Softball-Scorer\tools\DbCheck"
dotnet run
```

2) Archivo local `appsettings.json`: copia `appsettings.json.template` a `appsettings.json` y rellena la cadena.

3) Ejecutar la utilidad sin conexión: la herramienta detectará que no hay cadena y saldrá limpiamente mostrando un mensaje. Esto es útil si sólo quieres compilar/verificar.

Fallo común: "Host '...' is not allowed to connect" — significa que el servidor MySQL no permite conexiones desde tu host. En ese caso debes autorizar el host con GRANT en el servidor MySQL.
