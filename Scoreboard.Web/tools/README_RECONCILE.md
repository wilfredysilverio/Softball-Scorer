# Herramientas de reconciliación neutralizadas

Este archivo fue neutralizado por petición del usuario. Las instrucciones originales
para ejecutar `reconcile_migrations.sql` han sido eliminadas del README.

Si deseas eliminar completamente estos archivos del repositorio en tu máquina,
ejecuta desde la raíz del repo en PowerShell:

```powershell
Remove-Item .\tools\reconcile_migrations.sql
Remove-Item .\tools\README_RECONCILE.md
Remove-Item -Recurse .\tools\ApplyReconcile
```

No se ejecutará ninguna herramienta de reconciliación ni script automático sin
tu autorización explícita.
