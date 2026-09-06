; Instalador real de Terrakeep, con Inno Setup (herramienta gratuita y muy establecida para
; esto - instalada esta misma sesion via `winget install JRSoftware.InnoSetup`, autonomia
; tecnica ya permitida por las reglas del proyecto). Pedido explicito del usuario: "un
; instalador .exe" - install.ps1/uninstall.ps1 (ver ese fichero, mismo directorio) ya hacian el
; trabajo real (publicar + copiar + accesos directos) pero exigian saber ejecutar PowerShell;
; esto empaqueta EXACTAMENTE lo mismo en un unico .exe de doble clic, con desinstalador nativo
; real en "Aplicaciones y características" de Windows (Inno Setup lo genera solo, no hace
; falta mantener uninstall.ps1 a mano para esto).
;
; Como generar el instalador (dos pasos, nunca uno solo):
;   1. dotnet publish TerrasavrNative.App\TerrasavrNative.App.csproj -c Release
;        -p:PublishProfile=win-x64
;      (publish AUTOCONTENIDO desde la auditoria final de Opus, 5-sep-2026, antes de publicar en
;      publico - decision explicita del usuario: la version dependiente del framework pesaba
;      ~27MB pero exigia el .NET Desktop Runtime 10 instalado en el PC del usuario, razonable
;      para desarrollo pero no para el publico general, que veria la app fallar al arrancar sin
;      ninguna explicacion. Autocontenido pesa ~140MB, sin esa dependencia).
;   2. "C:\Users\adrian\AppData\Local\Programs\Inno Setup 6\ISCC.exe" installer\TerrakeepSetup.iss
;      (ruta real de instalacion via winget en esta maquina - AppData\Local\Programs, no
;      Program Files).
; El .exe final queda en installer\output\TerrakeepSetup-<version>.exe.
;
; MyAppVersion se actualiza a mano en cada version real (mismo criterio ya establecido para
; TerrasavrNative.App.csproj <Version>/changelog.json - sincronizados los tres a mano, sin
; ninguna herramienta que los mantenga automaticamente en linea).
#define MyAppName "Terrakeep"
#define MyAppVersion "2.3.0"
#define MyAppPublisher "IncrediBad"
#define MyAppExeName "Terrakeep.exe"
#define MyPublishDir "..\TerrasavrNative.App\bin\Release\net10.0-windows\win-x64\publish"

[Setup]
AppId={{8F2C1E4A-5B3D-4C6E-9A7F-1D2E3B4C5D6E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
; Instalacion por usuario (sin pedir permisos de administrador) - mismo alcance real que
; install.ps1, que instalaba en %LocalAppData%\Programs\Terrakeep.
DefaultDirName={autopf}\{#MyAppName}
PrivilegesRequired=lowest
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=TerrakeepSetup-{#MyAppVersion}
SetupIconFile=..\TerrasavrNative.App\Assets\branding\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible
; Auditoria final de Opus (5-sep-2026): sin esto el .exe DEL PROPIO INSTALADOR no lleva ningun
; dato de autoria en sus Propiedades de Windows (salia como un ejecutable anonimo de Inno
; Setup) - es lo primero que mira alguien que se descarga un instalador de un sitio que no es
; el oficial, y lo unico que distingue el instalador real de una recompilacion ajena mientras
; no haya firma Authenticode de verdad.
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoDescription=Instalador de {#MyAppName}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Recursivo real, todo lo que "dotnet publish" dejo (exe + dll + Assets/*.json/*.png) - mismo
; contenido exacto que install.ps1 copiaba a mano con Copy-Item.
; Los .pdb (simbolos de depuracion) no se empaquetan nunca: no le sirven de nada a quien usa la
; app y le dan hecho el trabajo a quien quiera descompilarla. El csproj ya no los genera en
; Release (DebugType=none), esto es el segundo cinturon por si alguien empaqueta una carpeta de
; publish antigua o generada con otra configuracion.
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
