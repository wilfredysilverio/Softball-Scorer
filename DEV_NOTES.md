# DEV_NOTES

## 2025-11-17

- Aplicada la migración `20251117163446_AddBattingStatsModule` con:
  `dotnet ef database update --project Scoreboard.Web/Scoreboard.Web.csproj --startup-project Scoreboard.Web/Scoreboard.Web.csproj`.
- Suite de pruebas ejecutada exitosamente:
  `dotnet test tests/Scoreboard.Tests/Scoreboard.Tests.csproj`.
- Mejoras clave:
  - Vistas y controladores de estadísticas ahora muestran PA/OBP/SLG/OPS y el desglose por jugador usando `JugadoresDetalle`.
  - `EstadisticasService` agrega PA/OPS y ordena jugadores; nuevas pruebas cubren PA/OPS y ordenamiento.
  - `MarcadorService` y las pruebas se ajustaron para reflejar que `R` representa carreras anotadas por el bateador.
