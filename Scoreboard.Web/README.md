# Softball-Scorer

Aplicación web (ASP.NET Core MVC + EF Core + MySQL) para llevar el control de **Equipos**, **Jugadores** y **Partidos** de un equipo de softball.

## Requisitos
- .NET SDK 9.x
- MySQL 8.0 local

## Configuración
1. Crear base y usuario (si no existen):
   - Base: \softball\
   - Usuario: \softuser\, contraseña local (configurable)

2. Ajustar \ppsettings.Development.json\ (no se sube al repo):
\\\json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Database=softball;User=softuser;Password=<TU_CLAVE>;AllowPublicKeyRetrieval=True;SslMode=None;"
  }
}
\\\

> En \ppsettings.json\ deja \Password=<CONFIGURAR>\ o valores de ejemplo.

## Migraciones (EF Core)
\\\ash
dotnet ef database update
\\\

## Ejecutar
\\\ash
dotnet build -f net9.0
dotnet run
\\\
Abrir la URL que imprime la consola (ej.: \http://localhost:5118\)


.

## Módulos MVP
- CRUD Equipos
- CRUD Jugadores (asignación a equipo)
- CRUD Partidos (validación casa ≠ visita, marcador básico)
