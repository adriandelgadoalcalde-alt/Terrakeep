# Terrasavr-Native / Terrakeep

## Idioma
SIEMPRE en español de España, nunca inglés - incluido justo después de
reanudar la sesión o de que el contexto se compacte/resuma solo. Ver
también la regla global en C:\Users\adrian\.claude\CLAUDE.md.

## Qué es este proyecto
Port nativo (C#/.NET 10, WPF) de Terrasavr-Calamity-Beta (hoy una app
Electron que envuelve un motor Haxe/OpenFL de YellowAfterlife, más una capa
propia de soporte de Calamity Mod). Repo git propio, separado del proyecto
Electron, hasta que tenga paridad real. Nombre propio: **"Terrakeep"**.

## Documentos que mandan
- `bitacora.md` (esta carpeta) - traspaso/contexto real, qué se ha hecho y
  verificado de verdad, qué queda pendiente. Se actualiza en el mismo turno
  en que se hace un cambio de fondo.
- `PROYECTO-TERRASAVR.md` (en `Downloads\Terrasavr-Win\`) - fuente de verdad
  del formato `.plr`/`.tplr` compartida con el proyecto hermano.

## Fuentes decompiladas reales - consultar SIEMPRE antes de suponer nada
Antes de suponer cómo se comporta Terraria/tModLoader/Calamity Mod (una
estadística de arma, un campo de guardado, una constante de juego...), mirar
el código real aquí. Nunca inventar un valor ni "sonar razonable" sin
comprobarlo:

- `C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\` -
  Terraria vanilla decompilado, **versión real 1.4.5.8** (confirmado en
  `Main.cs`: `assemblyVersionNumber = "1.4.5.8"` - sexta auditoría de
  Opus, 4-sep-2026; un comentario de un script viejo decía lo contrario
  por error, ya corregido). `Item.cs` con los bloques `SetDefaults#`
  reales, `ID\ItemID.cs`, `GameContent\Prefixes\PrefixLegacy.cs`,
  `Initializers\DyeInitializer.cs`...). Localización real en
  `Terraria.Localization.Content.{idioma}.{categoria}.json` - `Items`
  (nombres) y `Game` (con `BuffDescription` dentro, NO hay un
  `Buffs.json` aparte).
- `C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\` -
  tModLoader en sí, **versión real 1.4.4.9** (confirmado en `Main.cs`:
  `assemblyVersionNumber = "1.4.4.9"` - las dos carpetas van con
  versiones de Terraria DISTINTAS a propósito, no es un error: no
  asumir que comparten numeración de assets/ids sin comprobarlo).
- `C:\Users\adrian\Downloads\tModLoader-Decompiled\CalamityMod\` -
  Calamity Mod decompilado real (8101 archivos `.cs`, versión 2.2.2,
  confirmado real - una sesión anterior afirmó por error que "no había
  fuente real de Calamity" sin haber buscado bien; SÍ la hay). Cada arma es
  su propia clase con su propio `SetDefaults()`, no un switch numérico como
  vanilla.
- `C:\Users\adrian\Downloads\calamity_src_sparse\` - carpeta VACÍA (0
  archivos, comprobado), no es una fuente real, no perder tiempo mirando
  ahí.
- **Contenido crudo de un mod instalado** (localización/hjson/imágenes, NO
  compilado a C#): `Terrasavr-Calamity-Beta\resources\app\tmod-extract.js`
  (`node tmod-extract.js ruta\al\Mod.tmod [filtro]`) lee cualquier `.tmod`
  real - los de este PC están en
  `Documents\My Games\Terraria\tModLoader\Mods\`. Confirmado: el Calamity
  instalado aquí solo trae localización `en-US` (65 `.hjson` reales bajo
  `Localization\en-US\`), sin `es-ES` - no es que falte buscar mejor, esta
  build no la incluye. Ejemplo real ya usado:
  `Localization/en-US/Mods.CalamityMod.Buffs.hjson` (bloques
  `NombreInterno: { DisplayName: ..., Description: ... }`).
- `Terrasavr-Calamity-Beta\resources\app\local-site\script.js` - el motor
  JS real de Terrasavr (minificado). Versión legible ya generada una vez
  con `js-beautify` en `reference\terrasavr-real\script.beautified.js` (ver
  más abajo) - si hace falta refrescarla tras una versión nueva de
  Terrasavr, regenerar con `js-beautify script.js -o
  ..\..\..\..\Terrasavr-Native\reference\terrasavr-real\script.beautified.js`
  desde `local-site\`. `Terrasavr.es-ES.json` real (namespace `meta.item`
  para el texto de tooltips) está dentro de `local-site\lang\lang.zip`.

## Herramientas de decompilación/depuración en vivo
Índice completo real: `C:\Users\adrian\Downloads\herramientas.json` -
consultarlo siempre para rutas exactas, nunca invocar un ejecutable por su
nombre a secas (ruta absoluta + cwd).

- `dev-tools\ILSpy\ILSpy.exe` - lectura estática de un DLL/EXE .NET con
  interfaz.
- `dev-tools\dnSpyEx\dnSpy.exe` - el ÚNICO que se conecta a un PROCESO VIVO
  (depuración en caliente si hiciera falta) - nunca `dnSpy.Console.exe`
  (casca con `IOException` de consola).
- `ilspycmd` (herramienta global de `dotnet tool`, ya instalada en este PC -
  confirmar con `dotnet tool list -g`, instalar con
  `dotnet tool install -g ilspycmd` si hiciera falta, autonomía ya
  concedida) - más rápido que ILSpy con interfaz para volcados masivos.
  `ilspycmd -r` necesita un DIRECTORIO de referencias, no la ruta de un
  `.dll` suelto.
- `js-beautify` (paquete npm global) +
  `dev-tools\node-v24.20.0-win-x64\node.exe`/`npm.cmd` - para JS
  minificado (Terrasavr real).
- `tmod-extract.js` (ver arriba) para contenido crudo de cualquier `.tmod`.

## `reference\` - copia permanente dentro del propio repo
Además de documentar las rutas de arriba, hay una copia YA GENERADA dentro
de este repo (comiteada, no se borra con `dotnet clean`) para no depender
de rutas fuera del proyecto ni regenerar nada en el scratchpad de cada
sesión:

- `reference\terrasavr-real\script.beautified.js` - volcado legible del
  `script.js` real de Terrasavr (ver comando de regeneración arriba).
- `reference\terrakeep-decompilado\` - descompilación de la build propia de
  Terrakeep vía `ilspycmd` (nuestro propio código, sin problema de
  procedencia ajena) - sirve como comprobación real de que la cadena de
  herramientas funciona en este PC y como referencia si hiciera falta
  inspeccionar el compilado final (p.ej. verificar que el instalador lleva
  el código esperado), aunque el 100% del fuente ya esté en el propio repo.
  Regenerar con:
  `ilspycmd -o reference\terrakeep-decompilado
  TerrasavrNative.App\bin\Debug\net10.0-windows\TerrasavrNative.App.dll`

## Verdades del entorno WPF (no volver a descubrirlas)
- **Las capturas de pantalla son POCO FIABLES en este entorno** (confirmado
  por diferencial: hasta una build committeada y conocida-buena reproduce
  "ventana en blanco" en captura). Verificar de verdad con, por este orden
  de preferencia: (1) `dotnet build`/`dotnet test`; (2) UI Automation real
  vía PowerShell (`System.Windows.Automation`,
  `AutomationElement.FindFirst`/`InvokePattern`/`SelectionItemPattern`) -
  fiable para navegación/interacción sin coordenadas de píxel; (3)
  **`TerrasavrNative.App.Tests`** (auditoria de Opus, Bloque 6, T-21) - ya NO
  es un proyecto de consola temporal del scratchpad de cada sesión (así vivía
  antes, reconstruido desde cero cada vez que se perdía la sesión - cientos
  de líneas re-escritas, bugs ya resueltos vueltos a pisar sin querer): es un
  proyecto real y permanente del propio repo, en `TerrasavrNative.slnx` -
  `dotnet run --project TerrasavrNative.App.Tests` desde la raíz monta una
  `MainWindow` real, coloca datos reales, interactúa vía UI Automation real
  y deja capturas + líneas "esperado X, obtenido Y" en stdout. Al añadir una
  verificación nueva, AÑADIRLA AHÍ (no crear otro proyecto aparte) - así se
  seguirá acumulando sesión a sesión en vez de perderse. Usar SIEMPRE una
  COPIA de personaje/mundo real al probar guardado, nunca el fichero real
  del usuario.
- **`Setter.TargetName` no puede apuntar a un `Brush`** (error de
  compilación MC4111) - solo a un `FrameworkElement`/
  `FrameworkContentElement`. Para animar el color de un pincel con nombre,
  usar un `ColorAnimation` dentro de un `BeginStoryboard`, no un `Setter`.
- **Un `Storyboard` dentro de `Style.Triggers` (a diferencia de
  `ControlTemplate.Triggers`) no puede usar `TargetName`** (MC4011) - si
  hace falta animar una parte con nombre del propio control, la animación
  tiene que vivir dentro de `ControlTemplate.Triggers` (o usar
  `MultiTrigger` ahí), no en `Style.Triggers`.
- Poner `elemento.Visibility = ...` directo en code-behind sobre una
  propiedad que YA tiene un `Binding` activo en XAML rompe ese binding en
  silencio para siempre. Usar
  `elemento.SetCurrentValue(UIElement.VisibilityProperty, ...)` para
  empujar un valor sin desconectar el binding.
- `MinWidth`/`MinHeight` de `Window` SÍ se aplican de verdad a nivel de SO
  (confirmado con `SetWindowPos`+`GetWindowRect` reales vía P/Invoke,
  forzando un tamaño menor y comprobando que WPF lo clampa) - a diferencia
  de otros entornos de este mismo ecosistema de proyectos donde una
  propiedad declarada puede quedar sin aplicar en silencio (ver la lección
  de `min()`/`max()` de CSS en `Terrasavr-Calamity-Beta\CLAUDE.md`), aquí
  no hace falta desconfiar de esta propiedad en concreto.
- **Un arnés que instancia `TerrasavrNative.App.App` (la clase real de
  `App.xaml`) y llama a `Run()` crea una SEGUNDA `MainWindow` fantasma**
  (sin datos cargados) porque `App.xaml` tiene `StartupUri="MainWindow.xaml"`
  compilado - esto pasa aunque `Application.MainWindow` ya se haya asignado
  a mano antes de `Run()`, y esa segunda ventana comparte título con la
  real así que no es evidente por fuera (UI Automation puede acabar
  hablando con la ventana equivocada sin ningún error visible). Para un
  arnés que necesita una `MainWindow` YA cargada con datos de verdad, usar
  un `System.Windows.Application` en blanco (nunca
  `TerrasavrNative.App.App`) y añadir a mano solo los recursos que hagan
  falta (`Theme.xaml` + los conversores de `App.xaml.Resources`) - así solo
  existe la ventana creada explícitamente.
- **`FrameworkElement.Measure(availableSize)` recorta el ANCHO/ALTO que devuelve al
  `availableSize` de entrada** en esa dimension si esta ES FINITA - aunque el `MeasureOverride`
  real del propio elemento haya calculado (y usado de verdad para medir a sus hijos) un tamaño
  mayor. Confirmado real probando `SlotGridPanel` con `MinCell` forzando una rejilla mas ancha
  que el `availableSize` ofrecido: `DesiredSize.Width` sale igual al `availableSize.Width` de
  entrada, NO al ancho real que `MeasureOverride` calculo - la dimension que SÍ entra como
  `Infinity` (sin restriccion real) no se recorta, y es la via fiable para verificar el tamaño
  real elegido por un Panel a medida en un test fuera de una ventana real. No es un bug del
  Panel: protege contra un Panel mal comportado que pida mas sitio del que se le ofrecio - el
  scroll real (cuando el Panel vive dentro de un `ScrollViewer`, como todo uso real de
  `SlotGridPanel`) sigue funcionando bien, `ArrangeOverride` usa sus propios campos internos
  (`_cell`/`_cols`), no el `DesiredSize` ya recortado.
- `Path.GetTempPath()` desde un proceso lanzado en segundo plano vía Git
  Bash puede no resolver al mismo directorio que ve una sesión de
  PowerShell aparte - para un log de diagnóstico de un arnés, usar siempre
  una ruta absoluta fija (p.ej. dentro del propio scratchpad de la sesión),
  nunca depender de la carpeta temporal "por defecto".

## Reglas
- Commit antes de cualquier cambio grande, y TAMBIÉN después de cada cambio
  verificado (`dotnet build`/`dotnet test` en verde + verificación real),
  sin esperar a que se pida cada vez - mismo criterio que
  `Terrasavr-Calamity-Beta`.
- Si algo falla dos veces seguidas, parar y escribirlo en `bitacora.md`. No
  insistir en bucle.
- Permiso permanente para instalar herramientas/dependencias y automatizar
  arneses de prueba sin pedir permiso cada vez - ver la regla global en
  `C:\Users\adrian\.claude\CLAUDE.md`. Instalar siempre de forma no
  invasiva (global o scratchpad, nunca como dependencia de producción del
  proyecto sin que se pida explícitamente).
