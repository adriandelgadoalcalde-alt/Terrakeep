<#
    Fase B (integracion en Terrakeep de escritorio, 15-sep-2026): sincroniza el CONTENIDO de la
    Guia de progresion desde TerrakeepMod (donde se autora y se verifica en vivo dentro del
    juego) hacia Terrakeep.Core (donde la usa la app de escritorio, sin tModLoader instalado).

    DECISION de mecanismo (ver bitacora.md para el razonamiento completo): COPIA con
    sincronizacion explicita, no referencia en vivo a la carpeta de Documentos. Un jugador que
    solo tiene Terrakeep (la app, no el mod) no tiene ninguna carpeta
    "Documents\...\TerrakeepMod\" en su maquina - si Terrakeep leyera de ahi en tiempo de
    ejecucion, la Guia sencillamente no existiria para el publico objetivo real de esta fase
    ("jugar la version mas pura vanilla... sin depender de tener el mod instalado"). La copia
    vive DENTRO del repo de Terrakeep, en Terrakeep.App/Assets/guia/ (comiteada) - el MISMO sitio
    real donde ya viven el resto de catalogos JSON de la app (ej. Assets/calamity/catalog.json),
    para que el glob `Content Include="Assets\**\*.json"` de Terrakeep.App.csproj los copie al
    output solo, sin tocar el .csproj cada vez. El CODIGO que los lee (GuideCatalog/
    GuideTextCatalog) vive aparte, en Terrakeep.Core/Guia/ - mismo reparto datos/codigo que ya
    usa CalamityCatalog.

    Que copia y por que:
    1. guia_progresion.json TAL CUAL (ya es JSON limpio, sin necesidad de convertir nada) - el
       arbol de tramos/pasos/requisitos, identico en las dos plataformas: el JSON dice QUE hace
       falta, cada motor (TerrakeepMod en vivo / Terrakeep.Core contra el .plr+.wld reales) dice
       COMO se comprueba.
    2. Los textos de la sub-clave "Guia" de los DOS hjson de localizacion (es-ES/en-US), aplanados
       a JSON plano por hjson-guia-a-json.js (usa la libreria real "hjson" de npm, no un parser
       hecho a mano) - mismas claves punteadas que ya usa el mod
       (Idiomas.Texto("Guia.Paso."+clave+".Titulo")), para que evaluar el mismo requisito en las
       dos plataformas muestre el mismo texto.

    Ejecutar SOLO cuando la Guia cambie en TerrakeepMod (tramo nuevo, texto retocado, etc.) - no
    es parte del build normal de Terrakeep (evita depender de Node/hjson en cada `dotnet build`
    de quien solo quiere compilar la app).
#>
$ErrorActionPreference = "Stop"

$repoTerrakeep = Split-Path -Parent $PSScriptRoot
$repoTerrakeepMod = "C:\Users\adrian\Documents\My Games\Terraria\tModLoader\ModSources\TerrakeepMod"
$destino = Join-Path $repoTerrakeep "Terrakeep.App\Assets\guia"
$node = "C:\Users\adrian\Downloads\dev-tools\node-v24.20.0-win-x64\node.exe"
$scriptHjson = Join-Path $repoTerrakeep "scripts\hjson-guia-a-json.js"

New-Item -ItemType Directory -Force -Path $destino | Out-Null

Write-Host "1/3: guia_progresion.json (copia directa, ya es JSON real)..."
Copy-Item -Force (Join-Path $repoTerrakeepMod "Assets\guia_progresion.json") (Join-Path $destino "guia_progresion.json")

Write-Host "2/3: textos es-ES (hjson -> JSON plano via la libreria real 'hjson')..."
& $node $scriptHjson (Join-Path $repoTerrakeepMod "Localization\es-ES_Mods.TerrakeepMod.hjson") (Join-Path $destino "textos.es.json")
if ($LASTEXITCODE -ne 0) { throw "Fallo convirtiendo es-ES (codigo $LASTEXITCODE)." }

Write-Host "3/3: textos en-US (hjson -> JSON plano via la libreria real 'hjson')..."
& $node $scriptHjson (Join-Path $repoTerrakeepMod "Localization\en-US_Mods.TerrakeepMod.hjson") (Join-Path $destino "textos.en.json")
if ($LASTEXITCODE -ne 0) { throw "Fallo convirtiendo en-US (codigo $LASTEXITCODE)." }

Write-Host ""
Write-Host "Sincronizacion completa. Archivos en $destino"
Get-ChildItem $destino | Select-Object Name, Length
