# Instala Terrakeep para el usuario actual.
#
# Publica un build Release dependiente del framework (ver ..\TerrasavrNative.App\Properties\
# PublishProfiles\win-x64.pubxml - se probo autocontenido primero y salio un .exe de 140MB,
# casi tan pesado como la propia version Electron que se queria dejar atras; dependiente del
# framework pesa ~27MB, sobre todo los iconos reales, y el unico requisito es tener instalado
# el .NET Desktop Runtime 10 - razonable para uso propio en este PC), lo copia a
# %LocalAppData%\Programs\Terrakeep y crea un acceso directo en el menu Inicio (y en el
# Escritorio con -Desktop). No usa MSI/WiX ni ninguna herramienta externa - solo PowerShell +
# el objeto COM WScript.Shell, ya integrado en Windows.
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

Write-Host 'Publicando Terrakeep (Release, win-x64, dependiente del framework)...'
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

$exePath = Join-Path $installDir 'TerrasavrNative.App.exe'
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
Write-Host 'Requiere el .NET Desktop Runtime 10 (no es autocontenido, ver win-x64.pubxml).'
Write-Host "Para desinstalar: powershell -ExecutionPolicy Bypass -File `"$installDir\uninstall.ps1`""
