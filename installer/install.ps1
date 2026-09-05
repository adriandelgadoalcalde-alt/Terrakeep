# Instala Terrakeep para el usuario actual.
#
# Pedido explicito del usuario (5-sep-2026): "un instalador .exe" - TerrakeepSetup.iss (mismo
# directorio) empaqueta este mismo publish+copia+accesos directos en un .exe real de doble clic
# (Inno Setup, con desinstalador nativo real en "Aplicaciones y caracteristicas") - es la via
# recomendada para cualquiera que no vaya a tocar el codigo. Este script sigue aqui como
# alternativa de linea de comandos (o para quien prefiera no instalar Inno Setup).
#
# Publica un build Release AUTOCONTENIDO (ver ..\TerrasavrNative.App\Properties\
# PublishProfiles\win-x64.pubxml - decision de la auditoria final de Opus, 5-sep-2026: la version
# dependiente del framework pesaba solo ~27MB pero exigia el .NET Desktop Runtime 10 instalado,
# razonable para esta maquina de desarrollo pero no para el publico general que se descargue
# Terrakeep sin saber que necesita nada mas - falla al arrancar sin explicacion. Autocontenido
# pesa ~140MB, sin esa dependencia), lo copia a %LocalAppData%\Programs\Terrakeep y crea un
# acceso directo en el menu Inicio (y en el Escritorio con -Desktop). No usa MSI/WiX ni ninguna
# herramienta externa - solo PowerShell + el objeto COM WScript.Shell, ya integrado en Windows.
#
# Uso: powershell -ExecutionPolicy Bypass -File installer\install.ps1 [-Desktop]

param(
    [switch]$Desktop
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'TerrasavrNative.App\TerrasavrNative.App.csproj'
$installDir = Join-Path $env:LocalAppData 'Programs\Terrakeep'

$publishDir = Join-Path $repoRoot 'TerrasavrNative.App\bin\Release\net10.0-windows\win-x64\publish'

# Borrar la carpeta de publicacion ANTES de publicar es imprescindible, no cosmetico: se
# confirmo en pruebas reales de esta misma sesion que "dotnet publish" reutilizando una
# carpeta con cache incremental de una publicacion ANTERIOR con ajustes distintos (aqui,
# autocontenido vs dependiente del framework) puede terminar sin copiar los Content (todo
# Assets/*.json y *.png, la app arranco con FileNotFoundException) sin ningun error ni aviso -
# el build en si sale "correcto". Publicar siempre a una carpeta limpia evita el problema.
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host 'Publicando Terrakeep (Release, win-x64, autocontenido)...'
& dotnet publish $appProject -c Release -p:PublishProfile=win-x64
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish fallo - revisa el error de arriba.' }

if (-not (Test-Path $publishDir)) { throw "No se encontro la carpeta publicada: $publishDir" }
$missingAssets = -not (Test-Path (Join-Path $publishDir 'Assets\calamity\catalog.json'))
if ($missingAssets) { throw "La publicacion no incluyo los Assets (falta catalog.json) - no instales esta build." }

Write-Host "Instalando en $installDir ..."
if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item (Join-Path $publishDir '*') $installDir -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot 'uninstall.ps1') (Join-Path $installDir 'uninstall.ps1') -Force

$exePath = Join-Path $installDir 'Terrakeep.exe'
$shell = New-Object -ComObject WScript.Shell

$startMenuDir = Join-Path $env:AppData 'Microsoft\Windows\Start Menu\Programs'
$startMenuShortcut = Join-Path $startMenuDir 'Terrakeep.lnk'
$sc = $shell.CreateShortcut($startMenuShortcut)
$sc.TargetPath = $exePath
$sc.WorkingDirectory = $installDir
$sc.IconLocation = $exePath
$sc.Description = 'Terrakeep - editor de personajes de Terraria (vanilla + Calamity Mod)'
$sc.Save()
Write-Host "Acceso directo creado: $startMenuShortcut"

if ($Desktop) {
    $desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Terrakeep.lnk'
    $sc2 = $shell.CreateShortcut($desktopShortcut)
    $sc2.TargetPath = $exePath
    $sc2.WorkingDirectory = $installDir
    $sc2.IconLocation = $exePath
    $sc2.Description = 'Terrakeep - editor de personajes de Terraria (vanilla + Calamity Mod)'
    $sc2.Save()
    Write-Host "Acceso directo en el Escritorio: $desktopShortcut"
}

Write-Host ''
Write-Host "Listo. Terrakeep instalado en $installDir"
Write-Host 'Build autocontenida - no requiere el .NET Desktop Runtime 10 instalado (ver win-x64.pubxml).'
Write-Host "Para desinstalar: powershell -ExecutionPolicy Bypass -File `"$installDir\uninstall.ps1`""
