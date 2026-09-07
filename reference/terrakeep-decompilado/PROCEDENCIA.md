# Procedencia de esta descompilación

Descompilación real (vía `ilspycmd`) de la propia build de Terrakeep -
demuestra que la cadena de herramientas de decompilación funciona en este
PC y sirve de referencia si alguna vez hiciera falta inspeccionar el
compilado final (p.ej. verificar que un instalador lleva el código
esperado), aunque el 100% del fuente ya esté en el propio repo.

Es un volcado puntual (build Debug del momento en que se generó, 2-sep-2026,
con el proyecto todavía llamado `TerrasavrNative.App` - renombrado a
`Terrakeep.App` el 7-sep-2026, el `.dll`/`.exe` generado ya se llamaba
`Terrakeep` desde antes por `AssemblyName`)
- NO se mantiene sincronizado automáticamente con cada commit. Regenerar
tras un cambio si hace falta consultarlo actualizado (rutas ya con el
nombre de proyecto actual):

```
dotnet build Terrakeep.App\Terrakeep.App.csproj
ilspycmd -o reference\terrakeep-decompilado Terrakeep.App\bin\Debug\net10.0-windows\Terrakeep.dll
```
