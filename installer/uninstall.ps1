# Desinstala Terrakeep: borra los accesos directos y la carpeta de instalacion. Se copia
# dentro de %LocalAppData%\Programs\Terrakeep en cada instalacion (ver install.ps1) para poder
# desinstalar sin necesitar el repo a mano.

$ErrorActionPreference = 'Stop'

$installDir = Join-Path $env:LocalAppData 'Programs\Terrakeep'
$startMenuShortcut = Join-Path $env:AppData 'Microsoft\Windows\Start Menu\Programs\Terrakeep.lnk'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Terrakeep.lnk'

foreach ($path in @($startMenuShortcut, $desktopShortcut)) {
    if (Test-Path $path) { Remove-Item $path -Force }
}

if (Test-Path $installDir) {
    # Si este script se esta ejecutando DESDE $installDir (el caso normal, ver install.ps1),
    # no puede borrar la carpeta que lo contiene mientras sigue corriendo - se programa el
    # borrado en un proceso aparte con un pequeño retardo, igual que hacen los
    # desinstaladores reales de Windows.
    $cmd = "Start-Sleep -Seconds 1; Remove-Item -Recurse -Force '$installDir'"
    Start-Process powershell -ArgumentList '-NoProfile', '-WindowStyle', 'Hidden', '-Command', $cmd -WindowStyle Hidden
}

Write-Host 'Terrakeep desinstalado (la carpeta de instalacion se borrara en un momento).'
