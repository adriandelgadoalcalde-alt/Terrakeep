# Auditoría exhaustiva de la pestaña Exploración y de la barra superior, contra TEdit

Auditoría de referencia previa a una entrega. Compara **la interfaz completa de TEdit** (el editor
de mundos real de Terraria) contra **la pestaña Exploración** de Terrakeep y contra **la barra
superior global** de la app, y cataloga todas las mejoras posibles — estéticas e internas.

**Encargo del usuario (verbatim, 4-sep-2026)**:

> "quiero que opus haga otra auditoria muy muy exhaustiva comparando tedit i nuestra pestaña
> exploración y que diga todas las mejoras posibles que podemos implementar pulir etc asi como
> la barra de arriba donde esta lo de guardar cargar personaje donde busco etc la idea es pulir
> y mejorar a nivel estético interno etc ese apartado y que se tome todo el tiempo que necesite"

**Disciplina de este documento** (la misma de `ESPEC-buscador-mundo-tedit.md` y
`ESPEC-auditoria-redimensionado.md`):

- **Parte I (§1-§7) = HECHOS.** Cada afirmación sobre código lleva su fichero y su línea, de TEdit
  o de este proyecto, leídos de verdad. Nada de memoria ni de impresión visual.
- **Parte II (§8-§11) = PROPUESTA.** Pulido estético, pulido funcional y tabla priorizada con
  criterio de aceptación. Criterio mío, marcado como tal.
- **Parte III (§12) = HUECOS.** Lo que no verifiqué y por qué.

**Este documento no toca ni una línea de código de producción.** Es sólo el informe.

**Este documento NO repite `ESPEC-buscador-mundo-tedit.md`.** Aquel cubrió a fondo la
funcionalidad *Find* de TEdit (las 4 pestañas Cofres/Tiles/Paredes/Sprites, el bucle de fuerza
bruta, el acumulador con tope de 1000, la deduplicación por ancla, el overlay que oscurece). Aquí
se cubre **todo lo demás de la interfaz de TEdit** — menú, toolbar, barra de estado, minimapa,
activity bar, atajos, retícula, persistencia de vista, arrastrar y soltar — y la comparación real
contra el código de Terrakeep.

---

## Metadatos reales de la consulta a TEdit

En esta ronda **cloné el repositorio entero** en vez de descargar fichero a fichero (el volumen de
ficheros a leer lo justificaba):

```
git clone --depth 1 --branch main https://github.com/TEdit/Terraria-Map-Editor.git
```

| Dato | Valor real |
|---|---|
| Rama | `main` (confirmada, no `master`) |
| Commit exacto auditado | **`f5922611c428708684fe42ca55fa1cb636332a24`** |
| Asunto del commit | `Update ReactiveUI to 24.2.0 (#2366)` |
| Fecha de la clonación | 4-sep-2026 |

### Licencia de TEdit: **cerrado el hueco que arrastraban los dos documentos anteriores**

`ESPEC-buscador-mundo-tedit.md#6` y `ESPEC-ui-exploracion.md#17` dejaban abierto, los dos, que la
licencia de TEdit era `NOASSERTION` según la API de GitHub y que **nadie había abierto el
`LICENSE`**. Lo abrí. Es:

> **Microsoft Public License (MS-PL)** — `LICENSE:1-4`, literal:
> *"This source is subject to the Microsoft Public License. A copy can be found at
> https://opensource.org/licenses/MS-PL"*

**No es MIT.** Lo que esto significa en la práctica para Terrakeep, leyendo las cláusulas reales:

- `LICENSE`, cláusula **3(D)**: *"If you distribute any portion of the software in source code
  form, you may do so only under this license."* Es decir: **copiar código literal de TEdit
  obligaría a distribuir esa parte bajo MS-PL**, no bajo la licencia que Terrakeep elija.
- Cláusula **3(A)**: no concede derechos sobre el nombre ni el logo de TEdit.
- Cláusula **3(C)**: obliga a conservar los avisos de copyright/atribución.

**Consecuencia para todo este informe**: igual que en los dos precedentes, aquí no hay ni una
línea copiada de TEdit. Todo lo que se propone son **descripciones de comportamiento, fórmulas del
propio juego y formatos de fichero** — reimplementables libremente. Las tablas de ids y las
fórmulas de profundidad son datos de Terraria, no de TEdit. **Antes de copiar literalmente una
sola línea de su código hay que decidir conscientemente aceptar MS-PL en esa parte.**

---

# RESUMEN EJECUTIVO

Diecisiete hallazgos en Exploración y siete en la barra superior. **Cuatro son defectos reales, no
sólo "TEdit tiene algo que nosotros no"** — y de esos, uno hace que un mensaje que el código ya
calcula no se vea nunca:

| # | Qué pasa | Severidad |
|---|---|---|
| **E-01** | **"Sin resultados." nunca aparece en pantalla.** El `TextBlock` que lo muestra está dentro de un `Grid` cuya visibilidad se decide con `WorldSearchResults.Count > 0` — justo la condición que es falsa cuando no hay resultados. Buscar algo que este mundo no tiene da **cero realimentación** | **Alta** |
| **E-02** | El barrido del mundo (hasta 20 millones de tiles) **no tiene ningún indicador de "buscando" ni forma de cancelarlo desde la UI**, pese a que la cancelación ya está implementada por dentro | **Alta** |
| **E-03** | **Los marcadores de resultado escalan con el zoom** (viven dentro del `Grid` con `LayoutTransform`). A "Ajustar a la ventana" en un mundo Grande (zoom ≈ 0,09) una elipse de 9 px queda en menos de 1 px. TEdit hace explícitamente lo contrario | **Alta** |
| **E-04** | **Ninguna de las cuatro listas largas está virtualizada** (hasta 1000 filas de resultado, hasta 260 de inventario, cada una con su `Image`). TEdit virtualiza las dos equivalentes, con `Recycling` y `ScrollUnit="Pixel"` | **Alta** |

Y trece más de "capacidad que falta" en Exploración (minimapa, marcador de spawn del mundo,
profundidad/capa bajo el cursor, atajos de teclado, barra lateral redimensionable, memoria de la
vista por mundo, exportar el mapa, arrastrar y soltar, panel del mundo…), más siete de la barra
superior (es 100 % del personaje aunque estés mirando un mundo, sin agrupación visual, tres
idiomas de icono en la misma fila, sin atajo para "¿Dónde lo tengo?"…).

**Dos cosas que salieron bien y no hay que tocar**: el contraste de texto del tema **pasa AA**
(lo calculé, §7.2), y el conjunto de decisiones que Terrakeep tomó *apartándose* de TEdit
(filtrar candidatos a lo que existe de verdad en el mundo, buscar al teclear, sprites reales,
cabezas de NPC reales) son ventajas reales sobre TEdit que ningún cambio de esta lista debe
sacrificar (§6).

---

# PARTE I — HECHOS

## 1. Metodología y alcance

### 1.1 Qué leí, de verdad

**De TEdit** (commit `f592261`), enteros:

```
src/TEdit/MainWindow.xaml                       (598 líneas)  menú, toolbar, barra de estado, minimapa, activity bar
src/TEdit/UI/MouseTile.cs                       (154)         qué se muestra del tile bajo el cursor
src/TEdit/KeyboardShortcuts.cs                  (64)          el sistema de atajos viejo
src/TEdit/Render/RenderMiniMap.cs               (200)         el minimapa
src/TEdit/Input/InputBindingRegistry.cs         (237)         el sistema de atajos nuevo
src/TEdit/appsettings.yaml                                    los 30+ atajos por defecto reales
src/TEdit/Configuration/WorldViewStateManager.cs (parcial)    memoria de zoom/scroll por mundo
src/TEdit/View/Sidebar/FindSidebarView.xaml     (137)
src/TEdit/View/Sidebar/Controls/TileWallPickerControl.xaml (131)
src/TEdit/View/Sidebar/WorldAnalysis.xaml       (26)
src/TEdit/View/WorldRenderXna.xaml              (35)
LICENSE
```

**De TEdit**, por secciones localizadas (los dos ficheros son demasiado grandes para leerlos
enteros: 8757 y 4482 líneas):

```
src/TEdit/View/WorldRenderXna.xaml.cs   :47-74 (capas), :700-760 (CenterOnTile), :3822-3844 (DrawGrid),
                                        :6784-6850 (DrawPoints), :6902-6943 (DrawFindCrosshair),
                                        :8140-8200 (Zoom/ZoomFocus)
src/TEdit/MainWindow.xaml.cs            :290-430 (teclado), :930-970 (ZoomFocus/PanTo/minimapa/drop),
                                        :1080-1105 (plegar el panel lateral)
src/TEdit/App.xaml.cs                   :295-360 (carga de atajos)
src/TEdit/Input/InputService.cs         :60-560 (acciones por defecto)
src/TEdit.Terraria/World.FileV2.cs      :2082-2135 (cabecera después de RockLevel)
src/TEdit.Editor/WorldAnalysis.cs       :1-130
src/TEdit/Properties/Language.resx      (cadenas de barra de estado, profundidad y resultados)
```

**De Terrakeep**, enteros:

```
TerrasavrNative.App/ViewModels/ExplorationViewModel.cs   (1104 líneas)
TerrasavrNative.Core/WldFormat/WorldSearch.cs            (183)
TerrasavrNative.Core/WldFormat/WorldPresenceIndex.cs     (125)
TerrasavrNative.Core/WldFormat/WldHeader.cs              (53)
TerrasavrNative.Core/WldFormat/WldTile.cs                (20)
TerrasavrNative.App/Services/WorldRenderer.cs            (109)
```

**De Terrakeep**, por secciones:

```
TerrasavrNative.App/MainWindow.xaml       :1185-1330 (plantillas de píldora e inventario),
                                          :1330-1535 (LA BARRA SUPERIOR entera), :3350-3981 (EXPLORACIÓN entera)
TerrasavrNative.App/MainWindow.xaml.cs    :100-180 (atajos), :355-495 (gestos del mapa)
TerrasavrNative.App/Styles/Theme.xaml     :140-170 (texto), :240-500 (Button), :1265-1406 (los dos selectores)
TerrasavrNative.App/ViewModels/MainViewModel.cs  :364-480 (breakpoints), :780-910 (¿Dónde lo tengo?)
TerrasavrNative.Core/WldFormat/WldReader.cs      :1-300
TerrasavrNative.App/Converters/VisibilityConverters.cs :78-85
```

Y los dos documentos precedentes completos (`ESPEC-buscador-mundo-tedit.md`, 528 líneas;
`ESPEC-auditoria-redimensionado.md`, secciones 1-5 y resúmenes) más el índice y las secciones
13/16/17 de `ESPEC-ui-exploracion.md`.

### 1.2 Qué NO es esta auditoría

- **No ejecuté ninguna de las dos aplicaciones.** Ni TEdit ni Terrakeep. Todo es lectura de
  código. Donde una afirmación depende de ver píxeles, lo digo explícitamente (§12).
- **No compilé nada** ni pasé el arnés (`TerrasavrNative.App.Tests/Program.cs`).
- **No repito** la ingeniería inversa de *Find*: eso ya está en `ESPEC-buscador-mundo-tedit.md`.

---

## 2. La interfaz de TEdit que el documento previo NO cubrió

### 2.1 Anatomía completa de la ventana

`src/TEdit/MainWindow.xaml` (598 líneas). El reparto real, de fuera hacia dentro:

| Zona | Dónde | Qué es |
|---|---|---|
| Barra de título compacta | `:93-99` | `ui:TitleBar` de WPF-UI, `Height="24"`, integrada en el contenido (`ExtendsContentIntoTitleBar="True"`, `:21`) |
| **Menú** | `:103-268` | 6 menús: Archivo, Editar, Capas, Plugins, Debug, Ayuda |
| **Barra de estado** | `:269-299` | Abajo, `DockPanel.Dock="Bottom"` — 9 campos + barra de progreso |
| **Toolbar vertical** | `:300-335` | `DockPanel.Dock="Left"`, `WrapPanel` vertical de botones **32x32** |
| **Toolbar contextual** | `:336-438` | Arriba, cambia según la herramienta activa |
| **Minimapa + patrocinio** | `:337-344` | Arriba a la derecha, `Grid Width="300" Height="130"` |
| **Mapa** | `:448` | `View:WorldRenderXna`, columna `*` |
| **`GridSplitter`** | `:451-477` | Redimensiona el panel lateral |
| **Activity bar** | `:480-571` | `TabControl` de 16 pestañas de icono, columna `Width="440" MinWidth="50" MaxWidth="600"` (`:444`) |
| Snackbar | `:577-585` | Notificaciones no bloqueantes |
| Diálogos | `:588-596` | `ui:ContentDialogHost` |

La ventana además es **soltable**: `AllowDrop="True" Drop="WorldFileDrop"` (`:19-20`), manejado en
`MainWindow.xaml.cs:959-970` (coge `files[0]` y llama a `_vm.LoadWorld`).

### 2.2 La barra de estado: nueve campos siempre visibles

`MainWindow.xaml:269-299`. Es la pieza más densa de información de toda la aplicación y la que
más directamente compite con el `HoverInfo` de Terrakeep. Campo a campo, con su ancho real:

| Etiqueta | Enlace | Ancho | Qué muestra |
|---|---|---|---|
| Position | `MouseOverTile.MouseState.Location` | 75 | coordenada de tile |
| Depth | `MouseOverTile.DepthText` | 140 | **brújula + pies + capa** |
| Tile | `MouseOverTile.TileName` | 140 | `"{nombre} ({id})"` o `"[empty]"` |
| Wall | `MouseOverTile.WallName` | 140 | `"{nombre} ({id})"` |
| Extra | `MouseOverTile.TileExtras` | 100 | líquido + cantidad, inactivo, actuador, cables RGBY |
| Frame | `MouseOverTile.UV` | 50 | el UV real; **clic izquierdo abre el editor de UV** (`TextBox_PreviewMouseDown`, `MainWindow.xaml.cs:971-1000`) |
| Paint | `MouseOverTile.Paint` | 140 | pintura del tile y de la pared |
| Selection | `Selection.SelectionArea.Size` | 100 | tamaño de la selección |
| (sin etiqueta) | `DrawingModeText` | 80 | modo de dibujo activo |

Todo eso va dentro de un `Viewbox Stretch="Uniform" StretchDirection="DownOnly"` (`:276`): si la
ventana se estrecha, **la barra se encoge tipográficamente en vez de recortar campos**. Es un
recurso de diseño que resuelve de raíz el problema de la auditoría de redimensionado.

A la derecha, anclado (`:272-275`): `Progress.UserState` (texto) + `ProgressBar` de 100x20 ligada
a `Progress.ProgressPercentage` — **progreso determinado, con porcentaje real**.

**La profundidad es la fórmula GPS del propio juego.** `UI/MouseTile.cs:115-153`, con el
comentario literal *"Updates depth display text using Terraria's in-game GPS formulas. 1 tile = 2
feet. Depth is relative to surface; compass is relative to world center."*:

```
brújula  = tileX * 2 - tilesWide           → "{n}' East" / "{n}' West" / "Center"
pies     = (tileY * 2 - groundLevel * 2)
capa     = tileY > tilesHigh - 204   → Underworld
           tileY > rockLevel         → Caverns
           pies > 0                  → Underground
           spaceCheck < 1.0          → Space
           resto                     → Surface
```

donde `spaceCheck = (tileY - (65 + 10 * (tilesWide/4200)²)) / (groundLevel/5)` (`:141-144`,
comentario literal *"Space check: same formula as Terraria"*). El texto final es
`"{brújula}, {pies}' {capa}"` (`:152`).

**Nota importante para §9**: Terrakeep ya tiene los tres datos de entrada
(`WldHeader.GroundLevel`, `RockLevel`, `TilesHigh` — `WldHeader.cs:20-25`) y ya calcula una
partición de zonas equivalente en `WldHeader.ZoneFor` (`:45-52`), con los **mismos umbrales** que
TEdit usa para pintar el fondo de su minimapa (`Render/RenderMiniMap.cs:41-51`: `<80` Space,
`> TilesHigh-192` Hell, `> RockLevel` Rock, `> GroundLevel` Earth, resto Sky). Es decir: la parte
cara ya está hecha.

### 2.3 El minimapa

`MainWindow.xaml:338-342`, siempre visible en la esquina superior derecha:

```xml
<Border BorderThickness="1" ...>
  <Grid Width="300" Height="130">
    <Image Source="{Binding MinimapImage}" Stretch="None"
           RenderOptions.BitmapScalingMode="HighQuality" MouseDown="Image_MouseDown" />
  </Grid>
</Border>
```

- Se genera en `Render/RenderMiniMap.cs:14-32`: calcula una `Resolution` entera
  (`max(ceil(TilesWide/300), ceil(TilesHigh/100))`) y pinta un `WriteableBitmap` de
  `TilesWide/Resolution × TilesHigh/Resolution` **muestreando un tile de cada `Resolution`**
  (`:73-74`, `worldX = x * Resolution`) — no promedia, coge el píxel representativo.
- **Es clicable y navega**: `MainWindow.xaml.cs:946-957` convierte la posición del clic a
  coordenada de mundo multiplicando por `RenderMiniMap.Resolution` y llama a
  `MapView.ZoomFocus(mapPointX, mapPointY)`.
- Se regenera cuando cambian los datos: `WorldViewModel.cs:3480`, `:4394`,
  `WorldViewModel.Editor.cs:362`, `:526-531`, e incluso desde los plugins
  (`BlockShufflePlugin.cs:102`).
- Opcionalmente pinta el **fondo por zona de profundidad** detrás
  (`UpdateMinimap(..., showBackground)`, `:60-93` + `GetBackgroundColor:37-57`), con alpha-blend
  del tile sobre el color de zona (`:82-86`).

### 2.4 Las capas de dibujo del mapa, y qué se dibuja siempre

`View/WorldRenderXna.xaml.cs:47-74` declara **24 capas** con orden explícito
(`LayerTilePixels = 1-0` es el fondo, `LayerFindCrosshair = 1-0.35` es lo más adelante). Las
relevantes para un visor de sólo lectura:

- `DrawPoints()` (`:6784-6850`) — se dibuja **siempre** que haya mundo:
  - cada NPC (textura real o marcador según `ShowTextures`),
  - **el punto de aparición del mundo**: `_textures["Spawn"]` en `SpawnX/SpawnY`, alpha 128
    (`:6796-6805`),
  - **la mazmorra**: `_textures["Dungeon"]` en `DungeonX/DungeonY`, alpha 128 (`:6807-6816`),
  - los 6 puntos de aparición de equipo con su color, si la semilla lo activa (`:6818-6849`).
- `DrawGrid()` (`:3822-3844`) — retícula cada 16 tiles, **sólo si el zoom da para ver texturas**
  (`AreTexturesVisible() => _zoom > TextureVisibilityZoomLevel`, `:45`).
- `DrawFindCrosshair()` (`:6902-6943`) — ver 2.5.
- Fondo por zona de profundidad con parallax (`DrawBackgroundGradient`, `:413`).

### 2.5 La retícula de resultado: tamaño **constante en píxeles de pantalla**

Esto es el punto que más directamente contradice lo que hace Terrakeep hoy.
`View/WorldRenderXna.xaml.cs:6919-6922`, con su comentario literal:

```csharp
// Fixed size crosshair that doesn't scale with zoom (minimum 24px, max 48px)
const int minCrosshairSize = 24;
const int maxCrosshairSize = 48;
int crosshairSize = Math.Clamp((int)(_zoom * 3), minCrosshairSize, maxCrosshairSize);
int outlineThickness = 3; // Fixed thickness
```

Es un **marco rojo hueco** (cuatro rectángulos de 3 px: `:6931-6942`), centrado en el centro del
tile (`+0.5f`, `:6915-6916`), dibujado en la capa más adelantada de todas. Sea cual sea el zoom,
**mide entre 24 y 48 píxeles reales de pantalla**.

Y el resaltado del conjunto de resultados es el ya documentado en
`ESPEC-buscador-mundo-tedit.md#1.3`: se **oscurece todo lo demás** con la máscara de
`FilterOverlayBuffer`, no se marca lo encontrado.

### 2.6 Zoom, pan y navegación a un resultado

`View/WorldRenderXna.xaml.cs`:

- `Zoom(int direction, ...)` (`:8148-8175`): pasos de **×2 / ÷2**, `MathHelper.Clamp(tempZoom,
  0.125F, 64F)` — nueve pasos discretos. Con posición de ratón conocida hace `LockOnTile` (el
  punto bajo el cursor se queda quieto, `:707-713`); sin ella, `CenterOnTile`.
- `ZoomFocus(int x, int y)` (`:8177-8195`): **fija `_zoom = 8`** y centra. Es lo que usa la
  navegación a un resultado cuando `AutoZoomOnNavigate` está marcado, y lo que usa el clic en el
  minimapa.
- `CenterOnTile(int x, int y)` (`:724-730`) + `ClampScroll()`: el equivalente del `PanTo`.
- El viewport tiene **barras de scroll propias** (`View/WorldRenderXna.xaml:19-20`) cuyos
  `Minimum/Maximum/ViewportSize` se recalculan en cada zoom (`:8163-8174`) — es decir, la barra
  refleja de verdad la porción visible del mundo, no del bitmap.

### 2.7 Atajos de teclado: dos sistemas, uno de ellos configurable y con conflictos detectados

TEdit tiene **dos** mecanismos conviviendo, y el código lo dice explícitamente
(`MainWindow.xaml.cs:293-296`: *"Try new InputService first … Fall back to old system for backward
compatibility"*).

**Sistema antiguo** — `KeyboardShortcuts.cs` (64 líneas): diccionario `(Key, ModifierKeys) →
comando`, poblado desde `appsettings.yaml` en `App.xaml.cs:332-347`, con **detección de duplicados
que avisa por el log** (`:343-346`, *"Duplicate shortcut … using first binding, ignoring"*).

**Sistema nuevo** — `Input/InputService.cs` (811 líneas) + `InputBindingRegistry.cs` (237): un
registro de acciones con id (`"file.save"`, `"nav.zoom.in"`, `"tool.brush"`…), **categoría**
(`File`/`Editing`/`Selection`/`Navigation`/`Tools`), **ámbito** (`InputScope.Application`),
**varios enlaces por acción** (teclado **y rueda**), personalización por usuario que tiene
precedencia (`GetBindings:37-46`), reseteo a los valores de fábrica (`ResetToDefaults:100-117`) y
**`DetectConflicts()`** (`:147-182`), que devuelve los pares de acciones que comparten combinación
dentro del mismo ámbito.

Los atajos de fábrica reales están en **`src/TEdit/appsettings.yaml`** — 30 entradas, agrupadas
por comentario (`# Clipboard`, `# History`, `# Selection`, `# File operations`, `# Navigation`,
`# Tools`):

```
Copy: ctrl+c        Undo: ctrl+z        SelectAll: ctrl+a     Open: ctrl+o
Paste: ctrl+v       Redo: ctrl+y        SelectNone: ctrl+d    Save: ctrl+s
                                        DeleteSelection: delete  SaveAs: ctrl+shift+s
ResetTool: escape   ScrollUp/Down/Left/Right: flechas    (+ shift = rápido)
Pan: space          ZoomIn: ctrl+oemplus  ZoomOut: ctrl+oemminus  ReloadWorld: f5
Arrow: a   Brush: b   Fill: f   Pencil: e   Picker: r   Point: p   Selection: s
Sprite2: t Eraser: z  Swap: x   Hammer: h   ToggleTile: q  ToggleWall: w
```

Y `InputService.cs:513-535` añade a `nav.zoom.in`/`nav.zoom.out` un **segundo enlace por rueda**
(`InputBinding.Wheel(MouseWheelDirection.Up/Down)`), de modo que rueda y teclado son la misma
acción configurable.

**Lo importante para Terrakeep no es la lista, es el patrón de descubribilidad**: cada `MenuItem`
y cada botón de la toolbar muestra su combinación real, resuelta en tiempo de ejecución por un
converter (`KeybindingConverter`), tanto en el `InputGestureText` del menú
(`MainWindow.xaml:113-123`, `:158-171`) como en el `ToolTip` del botón (`:301-333`). Nadie tiene
que aprenderse la lista: la ve donde la va a usar.

### 2.8 La activity bar: 16 paneles de icono, plegables y redimensionables

`MainWindow.xaml:480-571`. Es un `TabControl` con estilo `ActivityBarTabControl`, donde cada
`TabItem` es **sólo un icono de 24 px** (`ui:SymbolIcon` de la fuente Fluent, o un `Path` con
geometría propia para los tres que no tienen símbolo: cofre, estandarte, tile entity) y **el
rótulo vive en el `ToolTip`**. Los 16: propiedades del mundo, paleta, tiles especiales, sprites,
portapapeles, NPCs, análisis, bestiario, estandartes, tile entities, poderes creativos, filtro,
**buscar**, scripting, explorador NBT (sólo si `HasTwldData`), editor de jugador (sólo si el ajuste
está activo).

Dos gestos que merece la pena registrar:

- **Clic en la pestaña ya activa = plegar el panel** (`MainWindow.xaml.cs:1080-1105`): sube por el
  árbol visual hasta el `TabItem`, y si el índice pulsado coincide con el seleccionado llama a
  `CollapseSidePanel()` y marca `e.Handled = true` *"prevent tab from re-selecting"*.
- **`GridSplitter` de 5 px de ancho de agarre pero 1 px de línea visible** (`:451-477`), con el
  comentario literal *"VS Code style: subtle until hovered/dragged"*: en reposo es una línea de
  1 px del color de borde; al pasar el ratón o arrastrar pasa a 3 px del color de acento.

### 2.9 Realimentación al usuario: tres canales distintos

1. **Diálogo modal** cuando la acción no tiene sentido: `FindSidebarViewModel.cs:160-164` —
   sin mundo cargado, `App.DialogService.ShowWarningAsync(find_dialog_title,
   find_no_world_loaded)` con el texto real *"No world loaded."* (`Language.resx`).
2. **Resumen textual permanente**, `ResultSummary` (`FindSidebarViewModel.cs:105-110`), con **tres
   estados** y cadenas reales:
   - sin resultados → `find_no_results` = **`"No results"`**
   - normal → `find_result_summary` = `"{0} of {1}"`
   - topado → `find_result_summary_limited` = `"{0} of {1} shown ({2} total matches)"`

   **Y vive fuera de cualquier condición de recuento**: `FindSidebarView.xaml:41-45` lo coloca en
   la columna central de la barra de navegación, entre `Prev` y `Next`, siempre presente. Éste es
   exactamente el punto de E-01.
3. **Snackbar** no bloqueante (`App.xaml.cs:313`, `MainWindow.xaml:577-585`) para lo que no merece
   un modal.

Y un cuarto, para lo que **está seleccionado pero aún no buscado**: `SelectionSummary`
(`FindSidebarViewModel.cs:92-103`) compone *"N items, N tiles, N walls, N sprites"* o
`find_no_selection` = `"No selection"`, y se muestra encima de los botones
(`FindSidebarView.xaml:81-83`).

### 2.10 Virtualización: TEdit la pone explícitamente en las dos listas largas

- Lista de resultados: `FindSidebarView.xaml:55-61` — `ListBox` con
  `VirtualizingPanel.IsVirtualizing="True"` y `VirtualizationMode="Recycling"`, `Height="150"`.
- Picker de tiles/paredes/objetos: `TileWallPickerControl.xaml:56-65` — lo mismo **más**
  `VirtualizingPanel.ScrollUnit="Pixel"` y `ScrollViewer.HorizontalScrollBarVisibility="Disabled"`,
  con el comentario literal *"Virtualized ListBox with color swatches (no external ScrollViewer -
  ListBox handles its own)"*.

Ese comentario es la advertencia exacta que Terrakeep necesita: **meter la lista dentro de un
`ScrollViewer` externo desactiva la virtualización** (§5, E-04).

### 2.11 La fila del picker: icono real con respaldo de color

`TileWallPickerControl.xaml:66-121`. Dos capas mutuamente excluyentes resueltas con un
`DataTrigger` sobre `Source != null`:

- `Image` de 20x20 con `BitmapScalingMode="NearestNeighbor"` desde `ItemIdToPreviewConverter`
  (`:78-85`), oculta por defecto;
- `Border` de 14x14 con el color del tile como respaldo (`:88-100`);
- a la derecha, **el id entre corchetes en gris** (`:103-107`, `StringFormat='[{0}]'`);
- el nombre con `TextTrimming="CharacterEllipsis"`.

Terrakeep ya hace esencialmente esto en `InventoryRowTemplate` (`MainWindow.xaml:1281-1325`), con
la mejora de una caja de tamaño fijo 26x26 y `StretchDirection="DownOnly"`. **Lo único que no
tiene es el id visible** — que TEdit muestra siempre y que es lo que permite buscar por número.

### 2.12 Memoria de la vista, por mundo

`src/TEdit/Configuration/WorldViewStateManager.cs` (leído `:1-70`): un diccionario
`ruta normalizada → { ScrollX, ScrollY, Zoom }` persistido en `worldViewState.json` dentro del
`AppDataPaths.DataDir`, con **escritura atómica** (`:59-61`: escribe a `.tmp` y hace
`File.Move(..., overwrite: true)`) y `try/catch` silencioso comentado como *"Best-effort
persistence"* (`:63-66`). El renderer lo guarda en `SaveViewState()`
(`WorldRenderXna.xaml.cs:715-722`) y el propio comentario del `Flush` dice *"Call periodically
(e.g., every 30s) or on exit"*.

Es decir: **cierras TEdit mirando una cueva concreta de un mundo concreto, y al reabrir estás
exactamente ahí.**

### 2.13 El censo del mundo: `WorldAnalysis`

`TEdit.Editor/WorldAnalysis.cs` + `View/Sidebar/WorldAnalysis.xaml` (26 líneas). La vista es
minimalista a propósito: título, dos botones en `UniformGrid Columns="2"` (**Analizar** /
**Guardar**) y un `TextBox` de sólo texto con las dos barras de scroll (`:20-22`).

El contenido (`WorldAnalysis.cs:63-130` y siguientes): cabecera y flags del mundo, después
`===SECTION: Tiles===` con **`Air: {n} ({porcentaje:P2})`** (`:100-102`) y la lista de tipos de
tile ordenada descendentemente **con su porcentaje sobre el total** (`:104-116`), después
`===Wires===` con el recuento por color y el total (`:119-125`), y a continuación cofres con su
contenido, letreros, NPCs y tile entities.

Dos ideas aprovechables que Terrakeep no ha explotado: **el porcentaje** (no sólo el recuento
absoluto) y **poder volcarlo a un fichero de texto**.

---

## 3. Estado real de la pestaña Exploración de Terrakeep

`MainWindow.xaml:3350-3981` (631 líneas de XAML), `ExplorationViewModel.cs` (1104),
`MainWindow.xaml.cs:355-492`, `WorldRenderer.cs` (109), `WorldSearch.cs` (183),
`WorldPresenceIndex.cs` (125).

### 3.1 Reparto de la pantalla

```
DockPanel (Margin 14)
├─ Top:    WrapPanel  — Cargar mundo · "Mundo: X" · píldora "Solo lectura" · − 100% + · Restablecer · Ajustar a la ventana   (:3358-3389)
├─ Top:    DockPanel  — "Tus mundos" · Actualizar · tira horizontal de píldoras de mundo                                      (:3396-3409)
├─ Bottom: TextBlock  — StatusMessage                                                                                          (:3411-3412)
└─ Grid de 2 columnas
   ├─ Col 0 (*)     Border → DockPanel
   │                 ├─ Bottom: caja de HoverInfo (aparece y desaparece)                                                       (:3437-3441)
   │                 └─ Grid → ScrollViewer "WorldMapScroll"                                                                   (:3450-3594)
   │                             └─ Grid con LayoutTransform(ScaleTransform Zoom)                                              (:3457-3460)
   │                                 ├─ Image WorldMapImage                     (el mapa, 1 px = 1 tile)                       (:3461)
   │                                 ├─ Image WorldHighlight                    (capa de minerales)                            (:3468)
   │                                 ├─ ItemsControl Npcs        → Canvas       (cabezas reales)                               (:3471-3520)
   │                                 ├─ ItemsControl CharacterSpawns → Canvas   (estrellas violetas)                           (:3527-3548)
   │                                 └─ ItemsControl WorldSearchResults → Canvas (elipses teal)                                (:3556-3592)
   │                 + Canvas MapTooltipCanvas (tooltip flotante)                                                              (:3604-3610)
   │                 + overlay de carga (#B0000000 + ProgressBar indeterminada)                                                (:3620-3626)
   │                 + overlay de estado vacío                                                                                 (:3640-3656)
   └─ Col 1 (Auto, MinWidth 260, MaxWidth por binding)  DockPanel                                                              (:3660-3978)
      ├─ Top: "Buscar en el mundo" + "Solo lo que existe en este mundo"                                                        (:3667-3672)
      ├─ Top: WrapPanel de 5 píldoras de categoría (Todo/NPCs/Cofres/Minerales/Objetos)                                        (:3678-3710)
      ├─ Top: TextBox de búsqueda (semántica variable por categoría)                                                           (:3717-3725)
      ├─ Bottom: bloque de resultados compartido (casilla de distancia · resumen · ‹ › · lista)                                (:3730-3799)
      └─ Grid con 4 DockPanel superpuestos, visibilidad exclusiva por SelectedCategory                                          (:3805-3977)
```

### 3.2 El renderizado del mapa

`Services/WorldRenderer.cs:25-77`. Un solo `WriteableBitmap` `Bgra32` de `TilesWide × TilesHigh`,
**un píxel por tile**, mezclado en el orden fondo de zona → pared → tile → líquido (`:43-72`),
`WritePixels` una vez y `Freeze()` (`:74-75`). El fondo se precalcula **por fila** porque sólo
depende de la profundidad (`:36-41`) — una optimización real y correcta.

El zoom **no vuelve a pintar nada**: es un `ScaleTransform` en el `LayoutTransform` del `Grid`
contenedor (`MainWindow.xaml:3458-3460`) con `BitmapScalingMode="NearestNeighbor"` en la `Image`.
Comparado con TEdit (chunks XNA + shaders + texturas reales a partir de cierto zoom) esto es
**deliberadamente mucho más simple**, y para un visor de sólo lectura es la decisión correcta:
`WorldRenderer.cs:10-13` lo dice literalmente.

Los gestos viven en el code-behind:
`OnWorldMapPreviewMouseWheel` (`MainWindow.xaml.cs:389-406`) hace zoom **centrado en el cursor**,
recalculando la coordenada de mundo antes y después y llamando a `UpdateLayout()` en medio (la
explicación del porqué está en el comentario `:380-388`, y es correcta);
`OnWorldMapMouseDown/Move/Up` (`:416-451`) hacen pan por arrastre con captura de ratón;
`OnWorldMapMouseMove` (`:435-451`) además actualiza el hover y reposiciona el tooltip flotante,
que se **voltea al otro lado si no cabe** (`PositionMapTooltip:457-476`);
`FitWorldMapToWindow` (`:368-375`) calcula `min(viewportW/pixelW, viewportH/pixelH)`;
`OnNavigateToTile` (`:487-492`) centra el `ScrollViewer` sin tocar el zoom.

### 3.3 El buscador

- **Motor**: `WorldSearch.Run` (`WorldSearch.cs:98-176`) — doble bucle `x→y` sobre toda la rejilla
  (`:107-123`) con `ct.ThrowIfCancellationRequested()` por columna (`:109`), más bucles cortos
  sobre NPCs (`:126-134`), cofres (`:139-148`), tile entities (`:154-163`) y letreros (`:165-173`).
  Acumulador con tope: `Add` cuenta siempre en `total` y sólo añade a la lista si
  `hits.Count < limit` (`:178-182`), con `DisplayLimit = 1000` por defecto (`:61`) — el mismo
  patrón real de `FindResultAccumulator` de TEdit.
- **Candidatos podados a lo que existe**: `BuildWorldSearchQuery` (`ExplorationViewModel.cs:978-1043`)
  recorre los catálogos completos pero descarta todo lo que `_presence` dice que no está
  (`:990`, `:997`, `:1004`, `:1011`, `:1025`). Esto **TEdit no lo hace en ningún sitio** (ya
  documentado en `ESPEC-ui-exploracion.md#2`).
- **Asíncrono y cancelable**: `RunWorldSearchAsyncWithQuery` (`:1061-1102`) cancela el
  `CancellationTokenSource` anterior, sube un contador de generación y descarta el resultado si
  llegó tarde (`:1086`). Debounce de 250 ms (`:240`, `:968`), y vaciar el cuadro limpia al
  instante sin esperarlo (`:957-967`).
- **Navegación circular** con módulo doble para que el negativo funcione
  (`MoveWorldSearchResult:538-548`), resaltado del actual (`UpdateCurrentWorldSearchHighlight:550-553`)
  y reordenación por distancia al spawn **sin repetir la búsqueda** (`ApplyWorldSearchOrder:559-581`).

### 3.4 La barra lateral, categoría a categoría

| Categoría | Colección | Contenido |
|---|---|---|
| **Todo** | `WorldSearchResults` | texto libre con `LibrarySearchGrammar` (comas = OR, espacios = AND, `#123`, `#100-200`) contra tiles + paredes + NPCs + líquidos + objetos de cofre + texto de letreros |
| **NPCs** | `NpcSearchResults` + `MissingNpcs` | 3 chips (Con casa / Sin casa / Bajo tierra, OR entre ellos y AND con el texto, `ApplyNpcFilter:914-934`), ordenación por profundidad cuando "Bajo tierra" está activo (`:930`), y `Expander` colapsado con los NPCs que faltan |
| **Cofres** | `Inventory` | 2 vistas: "Por tipo de cofre" (`ChestKindCounts`, con variante U/V real) y "Por lo que contienen" (`ChestItemCounts` + `TileEntityItemCounts` unidos, `:325-327`) |
| **Minerales** | `OreMetals`/`OreGems`/`OreTargets` | 3 grupos con cabecera, recuento de tiles **y de vetas** (`CountVeinsByType` en una única llamada combinada, con el hallazgo real de rendimiento documentado en `:346-347`: 5 s uno a uno frente a 255 ms combinado), + "Marcar en el mapa" / "Quitar marcas" |
| **Objetos** | `Inventory` | 3 vistas: Tiles / Paredes / Líquidos, ordenadas por recuento descendente |

Un solo `TextBox` sirve a las cinco y **cambia de semántica** (`OnWorldSearchTextChanged:943-969`):
en NPCs sincroniza `NpcSearchText`; en Cofres/Minerales/Objetos filtra el inventario en memoria de
forma inmediata; sólo en "Todo" lanza el barrido con debounce.

### 3.5 El hover

`UpdateHover(int tileX, int tileY)` (`ExplorationViewModel.cs:734-758`). Produce una única cadena:

```
"({x}, {y}) - {tile}"                      si la pared es vacía
"({x}, {y}) - {tile} / pared: {pared}"     si no
```

donde `{tile}` es `TileVariantName(Type, U, V)` si `IsActive`, o el nombre del líquido **sólo si el
tile no está activo** (`:749-753`), o `"(vacio)"`.

Se muestra en **dos sitios a la vez**: la caja fija bajo el mapa (`MainWindow.xaml:3437-3441`) y el
tooltip flotante que sigue al cursor (`:3604-3610`), los dos con
`Visibility="{Binding ... EmptyToCollapsed}"`.

---

## 4. Estado real de la barra superior global

`MainWindow.xaml:1344-1535`. Un `Border` con `BorderThickness="0,0,0,1"` y `Padding="16,8"`, en la
fila 0 del `Grid` raíz (`Height="Auto"`, `:1332`), sobre un `Grid` de tres columnas
(`Auto` / `*` / `Auto`):

**Columna 0 — identidad del personaje** (`:1389-1429`, visible sólo con personaje):
retrato real de 28×39,2 px (`PlayerPreviewRenderer`), `TextBox` del nombre editable in situ
(`:1400-1403`), punto `●` de "sin guardar" (`:1404-1406`), aviso `⚠` si el nombre no coincide con
el fichero (`:1411-1413`), píldora de dificultad (`:1414-1416`), insignia de Calamity
(`:1417-1420`) y línea de versión + fichero (`:1425`). Sin personaje, en su lugar un
`"Sin personaje cargado"` en `CaptionText` (`:1428-1429`).

**Columna 1 — franja de constantes vitales** (`:1442-1480`, visible sólo con personaje): un
`WrapPanel` (era `StackPanel`, cambiado por R-04a de la auditoría de redimensionado) con vida y
maná siempre (barras de 70×10 px con la cifra dentro a `FontSize="8.5"`), y defensa + dinero +
horas + último guardado sólo si `IsVitalsStripExpanded` (`:1463`), que hoy vale
`SizeClass != Compacto`, es decir **ancho ≥ 1320 px** (`MainViewModel.cs:378`, `:417`).

**Columna 2 — acciones** (`:1358-1388`), seis controles seguidos sin separadores:

| # | Rótulo | Tag | Padding | Visibilidad / habilitación |
|---|---|---|---|---|
| 1 | `Cargar personaje (.plr)...` | Accent | 14,8 (base) | siempre |
| 2 | `🔍 ¿Dónde lo tengo?` | — | 14,8 | **oculto** sin personaje (`Visibility` → `IsCharacterLoaded`) |
| 3 | `↶` | — | **8,3**, `FontSize=15` | `CanUndoEdit` (`MainViewModel.cs:149-156`) |
| 4 | `↷` | — | **8,3**, `FontSize=15` | `CanRedoEdit` (`:158-165`) |
| 5 | `Deshacer último guardado` | — | 14,8 | `CanUndoLastSave` (`:1128`) |
| 6 | `Guardar` | Orange | 14,8 | `IsCharacterLoaded` (`:1073`) |

El estilo base de `Button` (`Theme.xaml:248-254`) fija `FontSize="12.5"`, `Padding="14,8"`,
`CornerRadius="8"`, `BorderThickness="0"` y `Cursor="Hand"`, con animación de color de 120 ms y
elevación de −2 px al pasar el ratón **sólo** para las variantes de color sólido (`:373-378`).

**El popup "¿Dónde lo tengo?"** (`:1486-1533`): `Popup` anclado al botón con
`Placement="Bottom" StaysOpen="False" AllowsTransparency="True" PopupAnimation="Fade"`, `Width=400`,
borde de acento, sombra de tarjeta. Dentro: título, `TextBox` con `UpdateSourceTrigger=PropertyChanged`
y un tooltip que explica la gramática, resumen, y `ScrollViewer MaxHeight="360"` con las filas
(icono 24 px, nombre, "contenedor - slot N"). Al abrirse enfoca y selecciona el texto
(`MainWindow.xaml.cs:183-189`).

**Atajos globales existentes**, todos en `OnWindowKeyDown` (`MainWindow.xaml.cs:106-168`):
`Ctrl+S` guardar, `Ctrl+O` cargar, `Ctrl+Z`/`Ctrl+Y` deshacer/rehacer (cediendo el paso al
`TextBox` enfocado, `:126`, `:132`), `Ctrl+F` Librería contextual, `Esc` cancelar selección de
objeto/buff, `Ctrl+1…6` pestaña raíz. **No hay ningún atajo para "¿Dónde lo tengo?"** y **ninguno
para el mapa**.

---

## 5. Hallazgos — Exploración

Severidad: **Alta** = defecto real que se nota hoy · **Media** = capacidad ausente que un usuario
echará en falta · **Baja** = mejora clara pero prescindible para entregar.

---

### E-01 · Alta · **"Sin resultados." se calcula pero no se muestra nunca**

**Terrakeep**: `ExplorationViewModel.cs` asigna el texto en dos sitios reales —
`WorldSearchSummary = "Sin resultados."` cuando la consulta queda vacía (`:1078`) y cuando
`result.TotalCount == 0` (`:1091-1092`). Pero el único `TextBlock` que lo muestra
(`MainWindow.xaml:3750-3752`) está dentro de este `Grid`:

```xml
<Grid Margin="0,0,0,6"
      Visibility="{Binding Exploration.WorldSearchResults.Count, Converter={StaticResource CountToVis}}">
```

(`MainWindow.xaml:3743-3744`). Y `CountToVisibilityConverter`
(`Converters/VisibilityConverters.cs:78-85`) devuelve `Collapsed` para `count == 0`:

```csharp
value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
```

Verificado además que **no hay ningún otro punto de la vista que enlace `WorldSearchSummary`**: las
únicas apariciones en los 4165 renglones de `MainWindow.xaml` son las líneas 3750 y 3752.

**Efecto real**: buscar "corazón de vida" en un mundo que ya los tiene todos picados, o
equivocarse escribiendo, produce **exactamente nada**: la lista sigue vacía, no aparece ningún
mensaje, y el usuario no puede distinguir "no hay" de "no ha buscado todavía" ni de "se ha colgado".

**TEdit**: su `ResultSummary` es la columna central de la barra de navegación
(`FindSidebarView.xaml:41-45`), **sin ninguna condición de recuento**, y devuelve
`find_no_results` = `"No results"` precisamente cuando `Results.Count == 0`
(`FindSidebarViewModel.cs:105-110`).

---

### E-02 · Alta · **El barrido del mundo no avisa de que está en marcha ni deja cancelarlo**

**Terrakeep**: `WorldSearch.Run` recorre `TilesWide × TilesHigh` (`WorldSearch.cs:107-123`), que en
el mundo Grande real de esta máquina son **8400 × 2400 ≈ 20,2 millones de iteraciones**
(dimensiones citadas del comentario ya medido en `ExplorationViewModel.cs:185-187`). Se ejecuta en
`Task.Run` con `CancellationToken` (`:1085`) — todo correcto por dentro. **Pero:**

- No existe ninguna propiedad tipo `IsSearching` en toda la ViewModel (comprobado leyendo el
  fichero entero: los únicos indicadores de ocupado son `IsLoading`, `:191`, para la carga del
  mundo, e `IsScanningWorlds`, `:618`, para el escaneo de carpetas).
- `WorldSearchSummary` sólo se asigna **al terminar** (`:1091-1095`).
- El `CancellationTokenSource` (`_worldSearchCts`, `:241`) sólo lo cancela **otra búsqueda más
  nueva** (`:1063`) o cambiar el texto (`:961`). **No hay ningún botón "Cancelar"** en el XAML
  (`:3730-3799`).

**Efecto real**: en "Todo", 250 ms después de dejar de teclear arranca un barrido que puede durar
segundos, durante los cuales la barra lateral muestra **los resultados de la búsqueda anterior** y
ninguna señal de que hay algo en marcha. Es el mismo tipo de agujero que X-7/T-13 ya cerró para la
carga del mundo con el overlay de `IsLoading` (`MainWindow.xaml:3620-3626`), no cerrado aquí.

**TEdit**: su búsqueda es síncrona y disparada por un botón explícito
(`find_search_world`, `FindSidebarView.xaml:102-109`), así que el usuario sabe exactamente cuándo
empieza; y la aplicación tiene una `ProgressBar` **determinada** con porcentaje y texto de estado
permanente en la barra de estado (`MainWindow.xaml:272-275`).

---

### E-03 · Alta · **Los marcadores de resultado escalan con el zoom y desaparecen al alejarse**

**Terrakeep**: los tres `ItemsControl` de marcadores (resultados `:3556`, NPCs `:3471`, spawns del
personaje `:3527`) son **hijos del `Grid` que lleva el `ScaleTransform`**:

```xml
<Grid HorizontalAlignment="Left" VerticalAlignment="Top">
    <Grid.LayoutTransform>
        <ScaleTransform ScaleX="{Binding Exploration.Zoom}" ScaleY="{Binding Exploration.Zoom}" />
    </Grid.LayoutTransform>
```
(`MainWindow.xaml:3457-3460`)

Un `LayoutTransform` se aplica a **todo** el subárbol. Por tanto la elipse de resultado
`Width="9" Height="9"` (`:3573`), la cabeza de NPC `Width="16"` (`:3510`) y la estrella
`FontSize="18"` (`:3540`) se multiplican por `Zoom`.

Los extremos reales son conocidos: `MinZoom = 0.02, MaxZoom = 6.0`
(`ExplorationViewModel.cs:891`), y el propio comentario que fijó `MinZoom`
(`:884-890`) dice literalmente que **"Ajustar a la ventana" calcula ~9,4 % con un viewport de
787 px en un mundo de 8400×2400**. A `Zoom = 0.094`, una elipse de 9 px mide **0,85 px** y el
borde blanco de 1,2 px mide 0,11 px. En el otro extremo, a `Zoom = 6` la misma elipse mide 54 px y
el marcador del resultado activo (15 px, `:3580-3581`) mide **90 px**.

**TEdit** hace exactamente lo contrario, y lo comenta:
`WorldRenderXna.xaml.cs:6919-6922`, *"Fixed size crosshair that doesn't scale with zoom (minimum
24px, max 48px)"* — `Math.Clamp((int)(_zoom * 3), 24, 48)`, con grosor fijo de 3 px.

**Agravante**: este hallazgo se combina con E-12. Como navegar a un resultado sólo desplaza
(`OnNavigateToTile:487-492` no toca `Zoom`), si vienes de "Ajustar a la ventana" el resultado te
deja centrado sobre un marcador de menos de un píxel.

Nota de rigor: esto es un **hecho estructural** del árbol visual (WPF aplica `LayoutTransform` a
los descendientes), no una observación en pantalla — no ejecuté la app. `ESPEC-ui-exploracion.md#17`
ya dejaba esto abierto (*"No verifiqué cómo se ven los marcadores a zoom bajo"*); esta lectura lo
resuelve por la vía del código, no por la visual.

---

### E-04 · Alta · **Ninguna de las cuatro listas largas está virtualizada**

**Terrakeep** — las cuatro son `ItemsControl` (que **no** virtualiza) dentro de un `ScrollViewer`
externo (que además **desactivaría** la virtualización aunque el panel la tuviera):

| Lista | XAML | Tamaño máximo real |
|---|---|---|
| Resultados de búsqueda | `:3760-3762` (`ScrollViewer MaxHeight="240"` + `ItemsControl`) | **1000** (`WorldSearch.cs:61`), cada fila = `Button` + `Border` + 3 `TextBlock` + `Border` de píldora |
| Inventario de Cofres | `:3908-3909` | 206 NetId distintos medidos en un mundo Grande (`WorldPresenceIndex.cs:14-16`) |
| Inventario de Objetos | `:3973-3974` | **260 tipos de tile** medidos (`ibid.`), cada fila con una `Image` de 26×26 |
| Lista de NPCs | `:3848-3851` | ~20, irrelevante |

Las dos primeras son las que importan. Un `ItemsControl` realiza **todos** sus contenedores en el
primer layout; con 1000 filas de 5 elementos visuales cada una son ~5000 objetos creados de golpe
al terminar una búsqueda amplia.

**TEdit** pone la virtualización explícitamente en las dos listas equivalentes, y su comentario
avisa del gotcha exacto: `TileWallPickerControl.xaml:55` — *"Virtualized ListBox with color
swatches (**no external ScrollViewer** - ListBox handles its own)"*, con
`IsVirtualizing="True"`, `VirtualizationMode="Recycling"` y `ScrollUnit="Pixel"` (`:59-61`);
`FindSidebarView.xaml:58-59` lo mismo para los resultados.

Nota de rigor: **no medí el coste**. La afirmación es estructural (WPF no virtualiza un
`ItemsControl` desnudo, y un `ScrollViewer` externo con `CanContentScroll=False` desactiva la
virtualización del panel interno); el impacto en milisegundos está sin medir.

---

### E-05 · Media-alta · **No hay minimapa ni ninguna vista general al hacer zoom**

**Terrakeep**: la única referencia espacial cuando estás ampliado son las dos barras del
`ScrollViewer` (`MainWindow.xaml:3450`). No hay ninguna vista en miniatura del mundo entero.

**TEdit**: minimapa permanente de 300×130 en la esquina superior derecha
(`MainWindow.xaml:338-342`), generado por muestreo (`RenderMiniMap.cs:14-32`) y **clicable para
saltar** (`MainWindow.xaml.cs:946-957` → `ZoomFocus`).

**Coste real de portarlo aquí**: bajísimo, y bastante menor que en TEdit — Terrakeep **ya tiene el
bitmap del mundo entero congelado** (`WorldRenderer.cs:74-75`). Un minimapa es un segundo `Image`
con el mismo `Source` y `Stretch="Uniform"` en una caja fija; no hace falta un segundo render.

---

### E-06 · Media-alta · **El punto de aparición del mundo y la mazmorra no se dibujan**

**Terrakeep**: `WldHeader.SpawnX/SpawnY` se lee de verdad (`WldReader.cs:136-137`,
`WldHeader.cs:22-23`) pero **su único uso en toda la aplicación es calcular la distancia**:
`ExplorationViewModel.cs:567`, dentro de `ApplyWorldSearchOrder`. No hay ningún marcador de spawn
en el mapa. Lo que sí se dibuja son los spawns **del personaje** (`CharacterSpawns`, `:3527-3548`),
que salen del `.plr`, no del `.wld` — y que el propio comentario reconoce que no están validados
contra ningún mundo (`ExplorationViewModel.cs:587-590`).

`DungeonX/DungeonY` **ni siquiera se leen**: `ReadHeader` se detiene tras `RockLevel`
(`WldReader.cs:139-154`), y en el formato real la mazmorra está sólo **cinco campos más allá**
(`World.FileV2.cs:2084-2090`: `Time`, `DayTime`, `MoonPhase`, `BloodMoon`, `IsEclipse`,
`DungeonX`, `DungeonY`).

**TEdit**: dibuja los dos siempre, con textura propia y alpha 128
(`WorldRenderXna.xaml.cs:6796-6816`), más los seis spawns de equipo (`:6818-6849`).

---

### E-07 · Media · **El hover da mucha menos información de la que ya se podría dar gratis**

Comparación campo a campo entre `UpdateHover` (`ExplorationViewModel.cs:734-758`) y la barra de
estado de TEdit (`MainWindow.xaml:276-296` + `MouseTile.cs:53-153`):

| Dato | TEdit | Terrakeep | ¿Está el dato ya disponible aquí? |
|---|---|---|---|
| Coordenada | sí | **sí** | — |
| Nombre de tile | sí, con id | **sí**, sin id | el id es `tile.Type`, a mano |
| Nombre de pared | sí, con id | **sí**, sin id | ídem |
| **Profundidad + capa** | sí (fórmula GPS real) | **no** | **sí**: `GroundLevel`/`RockLevel`/`TilesHigh` ya en `WldHeader`, y `ZoneFor:45-52` ya parte las 5 zonas |
| **Líquido y cantidad** | sí, siempre | **sólo si no hay bloque** (ver E-08) | `LiquidType`/`LiquidAmount` ya en `WldTile:12-13` |
| UV / frame | sí (y abre el editor) | no | `U`/`V` ya en `WldTile:14-15` |
| Pintura | sí | no | **no**: `WldReader` los descarta (`:230`, `:237`) |
| Cables / actuador | sí | no | **no**: ni se leen |
| Zoom actual | (no en la barra) | sí, en la barra superior | — |

Los dos primeros ausentes (**capa/profundidad** y **líquido**) son los que un jugador de Terraria
usa de verdad: nadie piensa en "y = 1450", piensa en "Cavernas" o "Infierno".

---

### E-08 · Media · **Un bloque bajo el agua no menciona el agua**

`ExplorationViewModel.cs:749-753`:

```csharp
string tileText = tile.IsActive
    ? _tileNames.TileVariantName(tile.Type, tile.U, tile.V)
    : tile.LiquidAmount > 0
        ? LiquidName(tile.LiquidType)
        : "(vacio)";
```

El líquido sólo se nombra en la rama `!IsActive`. Una casilla de arena sumergida, una veta de
mineral bajo lava o un bloque dentro de un lago de miel se anuncian sólo como "Arena" / "Mineral de
oro", sin ninguna mención del líquido — pese a que **el mapa sí lo pinta** (`WorldRenderer.cs:60-64`
mezcla el líquido encima del tile). Es decir: se ve azul y el texto no lo dice.

Éste es el **caso simétrico** del bug ya corregido el 2-sep-2026 y documentado justo encima
(`:743-748`, "pone que esta vacio" sobre agua real): entonces se arregló la rama de líquido sin
bloque; queda la de líquido **con** bloque.

**TEdit** lo pone siempre en `TileExtras` (`MouseTile.cs:76-78`), con la cantidad:
`$"{tile.LiquidType}: {tile.LiquidAmount}"`.

---

### E-09 · Media · **El mapa no tiene ningún atajo de teclado**

**Terrakeep**: `OnWindowKeyDown` (`MainWindow.xaml.cs:106-168`) cubre `Ctrl+S/O/Z/Y/F`, `Esc` y
`Ctrl+1…6`. **Nada del mapa**: ni zoom, ni pan, ni "siguiente resultado", ni "ajustar". Todo pasa
por el ratón: los botones `−`/`+`/`Restablecer`/`Ajustar a la ventana` (`:3377-3387`), la rueda
(`:3451`) y el arrastre (`:3452-3454`).

**TEdit**: 30 atajos de fábrica configurables (`appsettings.yaml`), con registro de acciones por
categoría y ámbito, varios enlaces por acción incluida la rueda, detección de conflictos
(`InputBindingRegistry.cs:147-182`) y —lo más relevante— **anunciados en el sitio donde se usan**,
tanto en el menú (`InputGestureText`, `MainWindow.xaml:113-123`) como en el `ToolTip` de cada botón
de la toolbar (`:301-333`), resueltos por `KeybindingConverter`.

El proyecto ya tiene el precedente de que esto importa: T-H/F1 de la segunda auditoría, citado
literalmente en `MainWindow.xaml:1359-1362` — *"un atajo que nadie descubre no mejora la
practicidad"*.

---

### E-10 · Media · **La barra lateral no se puede redimensionar ni plegar, y el mapa nunca puede ocupar todo**

**Terrakeep**: `<ColumnDefinition Width="Auto" MinWidth="260" />` (`MainWindow.xaml:3431`) más
`MaxWidth="{Binding ExplorationSidebarMaxWidth}"` en el `DockPanel` (`:3660`). El usuario no puede
cambiarlo, y **no hay forma de plegarla** para dejar el mapa a pantalla completa — que es
justamente lo que se quiere al examinar un mundo.

El propio comentario reconoce la tensión (`:3422-3424`): *"la columna crece de 220-300 a 260-380 -
inventario + resultados no cabian en el ancho anterior (TEdit diseña este mismo panel a 400px)"*.
Es decir: se sabe que 260-380 es estrecho para lo que tiene que caber, y aun así es fijo.

**TEdit**: `GridSplitter` real (`MainWindow.xaml:444`, `Width="440" MinWidth="50" MaxWidth="600"`;
plantilla en `:451-477`, 1 px en reposo → 3 px de acento al pasar el ratón) **y** plegado con clic
en la pestaña activa (`MainWindow.xaml.cs:1080-1105`).

---

### E-11 · Media · **No se recuerda dónde estabas mirando**

**Terrakeep**: `LoadFromPathAsync` fija `Zoom = 1.0` (`ExplorationViewModel.cs:825`) en cada carga,
y el `ScrollViewer` arranca en (0,0). El propio comentario que justificó "Ajustar a la ventana"
(`MainWindow.xaml:3382-3384`) dice que **el 100 % en un mundo de 8400×2400 significa ver el 12 %
del ancho** — así que el estado inicial es, por diseño, el peor posible, y hay que rehacer el
encuadre a mano en cada apertura.

**TEdit**: `WorldViewStateManager` guarda `{ScrollX, ScrollY, Zoom}` por ruta de mundo en
`worldViewState.json` con escritura atómica (`:29-67`) y lo restaura al reabrir.

Terrakeep ya tiene el precedente de persistir preferencias (`Settings.ExtraWorldFolders`,
`BackupHistoryCap`, `MainWindow.xaml:4036-4058`), así que la infraestructura existe.

---

### E-12 · Media-baja · **Ir a un resultado nunca ajusta el zoom, y no hay opción de que lo haga**

**Terrakeep**: `MoveWorldSearchResult` y `GoToWorldSearchHit` llaman a `NavigateToTile`
(`:528`, `:547`), que sólo desplaza el `ScrollViewer` (`MainWindow.xaml.cs:487-492`). El comentario
(`ExplorationViewModel.cs:544-546`) lo justifica citando el `"Default false - just pan, don't
zoom"` de TEdit — y la justificación es **correcta en cuanto al valor por defecto**.

Lo que no se portó es **la casilla**: TEdit tiene `AutoZoomOnNavigate` visible en la UI
(`FindSidebarView.xaml:90-92`, con su propio tooltip), que cuando se marca llama a `ZoomFocus`
(`WorldRenderXna.xaml.cs:8177-8180`, `_zoom = 8`). El usuario decide.

En Terrakeep, sin esa opción y con E-03 encima, la navegación desde un encuadre alejado es
prácticamente inútil.

---

### E-13 · Baja · **No se puede exportar el mapa**

**Terrakeep** genera un `WriteableBitmap` completo del mundo y lo congela
(`WorldRenderer.cs:29`, `:74-75`) — un `PngBitmapEncoder` sobre él son ~8 líneas. No existe.

**TEdit**: `ExportMapTilesCommand` en el menú Archivo (`MainWindow.xaml:145`) y
`ExportSelectionAsPngCommand` con cuatro escalas (1/4/8/16 px por tile, `:173-188`).

Es una de las cosas que un usuario de un visor de mundos pide antes que muchas otras (compartir el
mapa, planificar una construcción sobre él).

---

### E-14 · Baja · **No se puede arrastrar un `.wld` (ni un `.plr`) sobre la ventana**

**Terrakeep**: `AllowDrop` aparece exactamente dos veces en los 4165 renglones de
`MainWindow.xaml`, y las dos son slots de objeto (`:310`) y de buff (`:782`). La ventana no acepta
ficheros.

**TEdit**: `AllowDrop="True" Drop="WorldFileDrop"` en la propia ventana (`MainWindow.xaml:19-20`),
manejado en `MainWindow.xaml.cs:959-970`.

---

### E-15 · Baja · **Sin retícula ni escala de referencia**

**Terrakeep**: nada. No hay forma de estimar distancias sobre el mapa salvo leyendo coordenadas.

**TEdit**: `DrawGrid()` cada 16 tiles (`WorldRenderXna.xaml.cs:3822-3844`), conmutable desde el
menú Capas (`MainWindow.xaml:200`), y **sólo activa cuando el zoom da para verla**
(`AreTexturesVisible()`, `:45`), más borde del mundo conmutable (`:229-230`).

---

### E-16 · Baja · **No se muestra ni un dato de cabecera del mundo — y dos de ellos ya se leen y se tiran**

`WldReader.ReadHeader` **lee y descarta** dos campos valiosos:

```csharp
if (version == 179) reader.ReadInt32(); // Seed numerico
else reader.ReadString();                // Seed como texto      ← WldReader.cs:95-96
...
    reader.ReadInt32(); // GameMode                              ← WldReader.cs:111
```

Y se detiene en `RockLevel` (`:139-154`), a propósito y bien documentado
(`WldHeader.cs:9-12`). Pero justo después, en el mismo flujo secuencial, están
(`World.FileV2.cs:2084-2119`):

`Time`, `DayTime`, `MoonPhase`, `BloodMoon`, `IsEclipse`, **`DungeonX/Y`**, **`IsCrimson`**,
`DownedBoss1EyeofCthulhu` … `DownedGolemBoss`, `DownedSlimeKingBoss`, `SavedGoblin/Wizard/Mech`,
`DownedGoblins/Clown/Frost/Pirates`, `ShadowOrbSmashed`, `ShadowOrbCount`, `AltarCount`,
**`HardMode`**, invasión, lluvia, y los tres `SavedOreTiers`.

Es decir: **semilla, modo de juego (Clásico/Experto/Maestro/Viaje), Corrupción vs Carmesí, modo
difícil y la lista completa de jefes derrotados** están a un puñado de `Read*` de distancia, y hoy
Terrakeep no enseña ninguno.

Esto encaja especialmente bien con la app: ya existe una pestaña **"Desbloqueos"** del personaje;
el equivalente del mundo no existe.

**TEdit** expone todo eso en `WorldPropertiesView.xaml` (1067 líneas, con desplegables de modo de
juego, fase lunar, invasión, tiers de mineral…) y en el volcado de `WorldAnalysis`.

---

### E-17 · Baja · **El censo del mundo existe por dentro pero no hay una vista de conjunto**

`WorldPresenceIndex` (`WorldPresenceIndex.cs:20-41`) ya guarda `TileCounts`, `WallCounts`,
`LiquidCounts`, `SpriteVariantCounts`, `NpcCounts`, `ChestItemCounts`, `ChestKindCounts`,
`SignCount` y `TileEntityItemCounts`. Todo eso se usa **sólo** como inventario filtrable por
categoría. No hay ningún sitio donde ver "este mundo, de un vistazo".

Faltan además dos cosas que TEdit sí hace y que salen gratis de esos mismos datos:

- **Porcentajes**: `WorldAnalysis.cs:100-116` imprime `Air: {n} ({%})` y el porcentaje de cada
  tipo sobre el total. Terrakeep sólo muestra el absoluto (`CountLabel`,
  `ExplorationViewModel.cs:89-91`).
- **Volcar a fichero**: `AnalyzeWorldSaveCommand` (`WorldAnalysis.xaml:17`).

---

## 6. Hallazgos — barra superior global

---

### B-01 · Media · **La barra es 100 % del personaje aunque estés mirando un mundo**

Con la pestaña **Exploración** activa y sin personaje cargado, la barra superior muestra:

- columna 0: `"Sin personaje cargado"` (`MainWindow.xaml:1428-1429`);
- columna 1: **vacía** — la franja vital tiene `Visibility` ligada a `IsCharacterLoaded` (`:1443`);
- columna 2: **cinco botones**, de los cuales `Guardar` (`CanExecute = IsCharacterLoaded`,
  `MainViewModel.cs:1073`), `↶`/`↷` (`CanUndoEdit`/`CanRedoEdit`, `:149-165`) y
  `Deshacer último guardado` (`CanUndoLastSave`, `:1128`) están **deshabilitados**, es decir
  pintados al 40 % de opacidad (`Theme.xaml:331-333`) pero ocupando su sitio.

Y **ni un solo dato del mundo cargado**, que es lo que el usuario está mirando: ni el título (vive
abajo, `:3361`), ni el tamaño, ni el zoom, ni la coordenada.

Nótese que esto es exactamente el problema que H5-10 vino a resolver para la pestaña Personaje
—`"la cabecera global, lo unico visible en las 6 pestañas, esta hueca por dentro - todo el centro
vacio"` (comentario literal, `:1346-1348`)— reaparecido para las pestañas que no son de personaje.

---

### B-02 · Media · **Seis controles seguidos sin agrupación ni jerarquía**

`MainWindow.xaml:1358-1388`: un único `StackPanel Orientation="Horizontal"` con
`Cargar personaje` · `🔍 ¿Dónde lo tengo?` · `↶` · `↷` · `Deshacer último guardado` · `Guardar`,
separados sólo por `Margin="10,0,0,0"` (y `4,0,0,0` entre `↶` y `↷`).

Mezcla **tres familias**: archivo (1, 5, 6), búsqueda (2) e historial de edición (3, 4). Nada
indica que `Deshacer último guardado` (fichero, con `.bak`) y `↶` (una edición de slot) son cosas
completamente distintas — y sus tooltips lo explican bien (`:1379`, `:1386`) pero el ojo no lo ve.

**TEdit** separa por construcción (menú desplegable + toolbar de iconos) y dentro del menú usa
`<Separator Margin="1"/>` entre grupos: `MainWindow.xaml:117`, `:124`, `:141`, `:144`, `:146`,
`:154`, `:162`, `:169`, `:172`.

---

### B-03 · Media-baja · **Tres idiomas de icono y dos alturas de botón en la misma fila**

En seis controles conviven:

- **emoji**: `"🔍 ¿Dónde lo tengo?"` (`:1369`) — el único emoji de toda la barra;
- **glifos tipográficos**: `"↶"` / `"↷"` (`:1378`, `:1380`), a `FontSize="15"`;
- **texto puro**: los otros tres.

Y dos alturas: `↶`/`↷` llevan `Padding="8,3"` mientras el estilo base de `Button` fija
`Padding="14,8"` (`Theme.xaml:252`) — **10 px menos de alto** pegados a sus vecinos.

Un emoji además hereda el color del sistema, no el del tema: no responde a `Foreground` como el
resto de la fila, y su renderizado depende de la fuente de emoji instalada.

---

### B-04 · Media-baja · **"¿Dónde lo tengo?" no tiene atajo y desaparece por completo sin personaje**

- Sin atajo: la única forma de abrirlo es el botón (`ToggleWhereIsItCommand`,
  `MainViewModel.cs:804`). `Ctrl+F`, que es lo que un usuario probaría primero, está tomado por la
  Librería (`MainWindow.xaml.cs:136-151`).
- Sin personaje **se oculta del todo** (`Visibility` → `IsCharacterLoaded`, `:1372`), a diferencia
  de sus vecinos que sólo se deshabilitan — un tercer comportamiento distinto en la misma fila
  (siempre visible / deshabilitado / oculto).

Es una funcionalidad cara de construir (H5-05) y potente, y hoy sólo la descubre quien lee el
rótulo.

---

### B-05 · Baja · **Nada cierra el popup con `Escape` explícitamente**

`OnWindowKeyDown` (`MainWindow.xaml.cs:152-158`) trata `Escape` sólo para cancelar una selección de
objeto o de buff; en cualquier otro caso hace `return` sin consumir la tecla. No hay ningún
manejador que cierre `WhereIsItPopup`.

**Cautela**: el `Popup` tiene `StaysOpen="False"` (`MainWindow.xaml:1487`); **no verifiqué** si
WPF lo cierra por sí solo con `Escape` en esa configuración (§12).

---

### B-06 · Baja · **La franja vital deja el centro de la barra vacío en 3 de las 6 pestañas**

`Visibility="{Binding IsCharacterLoaded, ...}"` (`:1443`) es lo correcto para Personaje/Objetos/
Builds. Pero en **Exploración**, **Novedades** y **Acerca de** el dato relevante nunca es el
personaje, y la columna `*` (que se lleva todo el sobrante horizontal, `:1355`) queda en blanco a
cualquier tamaño de ventana — un desperdicio que la auditoría de redimensionado ya identificó en
su categoría H-09 para otras pantallas.

---

### B-07 · Baja · **El nombre del personaje es editable sin ninguna señal de que lo sea**

`MainWindow.xaml:1400-1403`: `TextBox` con `BorderThickness="0"`, fondo `BgElevatedBrush`,
`FontSize="14" FontWeight="SemiBold"` — visualmente **idéntico a un título de texto muerto**, que
es exactamente lo que era antes de H-1. La decisión de hacerlo editable es buena; lo que falta es
la *affordance*: hoy no hay ni cursor de texto anunciado, ni borde al pasar el ratón, ni ningún
indicio, y se puede modificar el nombre real del personaje por accidente al hacer clic para
enfocar la ventana.

---

## 7. Lo que Terrakeep tiene y TEdit NO — no perderlo de vista

### 7.1 Ventajas reales sobre TEdit

Cada una de estas es una decisión consciente ya tomada y documentada; **ninguna propuesta de la
Parte II debe sacrificarlas**.

| Ventaja | Dónde vive en Terrakeep | Qué hace TEdit |
|---|---|---|
| **Candidatos podados a lo que existe de verdad en el mundo** | `WorldPresenceIndex.cs` entero + `BuildWorldSearchQuery:990/997/1004/1011/1025` | Sus pickers salen siempre del catálogo completo del juego, cargado una vez y nunca podado (`ESPEC-ui-exploracion.md#2`). Entre el **65 % y el 96 %** de lo que ofrecería no existe en el mundo cargado |
| **Buscar al teclear**, con debounce y cancelación por generación, fuera del hilo de UI | `:240`, `:956-968`, `:1061-1102` | Botón explícito, búsqueda síncrona |
| **Búsqueda de NPCs** | `WorldSearch.cs:126-134` | No busca NPCs (verificado en el documento previo, §1.1) |
| **Búsqueda de líquidos** | `:120-121` | No |
| **Búsqueda por texto de letrero** | `:165-173` con predicado inyectado (`:60`) | No: `SearchContainers` sólo recorre cofres y tile entities |
| **Categoría Minerales con agrupado por vetas** | `OreVeinFinder`, `OreTileCatalog`, `MarkOresOnMap:479-511` | No existe el concepto de mineral en sus datos (`ESPEC-ui-exploracion.md#4`) |
| **Sprites reales de tile/pared/objeto en el inventario** | `InventoryRowTemplate:1281-1325` con `TileIconResolver`/`WallIconResolver`/`VanillaIconResolver` | Sólo objetos tienen preview; tiles y paredes son un cuadrado de color |
| **Nombres en español** | `TileNameCatalog`, `NpcNameCatalog`, `VanillaItemCatalog` | Tiene español de interfaz (`menu_language_es`) pero los nombres de tile salen de `TileProperties`, en inglés |
| **Cabezas reales de NPC sobre el mapa** con el índice real del juego | `NpcHeadProfile` + `HeadIconPath:120`, `:3496-3518` | Dibuja el sprite completo del NPC o un marcador |
| **Cruce personaje ↔ mundo** (spawns del `.plr` sobre el mapa) | `CharacterSpawns:216`, `:3527-3548` | No tiene concepto de personaje sobre el mapa (su editor de jugador es un panel aparte) |
| **Lanzador de mundos permanente** con escaneo de carpetas reales y menú contextual | `Worlds:617`, `RefreshWorldsAsync:663-689`, `WorldPillTemplate:1240-1274` | `WorldExplorerViewModel` es una ventana modal aparte |
| **Estado vacío guiado** y píldora **"Solo lectura"** | `:3640-3656`, `:3370-3374` | Un lienzo vacío |
| **Tooltip flotante que se voltea en los bordes** | `PositionMapTooltip:457-476` | Barra de estado fija abajo |
| **Zoom centrado en el cursor con paso fino (×1,25)** | `:389-406` + `ZoomStep:903` | Pasos de ×2/÷2 |

### 7.2 Comprobación de contraste: **sale bien, no hay que tocarlo**

Calculé el contraste WCAG 2.1 real del caso más apretado del panel — `CaptionText`
(`TextSecondaryBrush = #8a8fa3`, `FontSize="11"`, `Theme.xaml:159-163`) sobre el fondo de fila
`BgElevatedBrush = #1e2233` (`:18`), que es lo que usan la coordenada y el `CountLabel` de cada
resultado (`MainWindow.xaml:3788`, `:1322`):

```
L(#8a8fa3) = 0,2770      L(#1e2233) = 0,0166
contraste = (0,2770 + 0,05) / (0,0166 + 0,05) = 4,91 : 1
```

**4,91 : 1 supera el mínimo AA de 4,5 : 1** para texto normal. El caso peor real de la pestaña
—`DistanceLabel` a `FontSize="10.5"` (`:3789-3790`)— usa el mismo par de colores, así que también
pasa. **No propongo tocar la paleta de texto.**

Cautela: es un cálculo sobre los hexadecimales del tema, no una medición sobre píxeles pintados
(no ejecuté la app), y no cubre el texto sobre gradientes (`Tag="Accent"`, `Tag="Orange"`), que no
calculé.

---

# PARTE II — PROPUESTA

**Todo lo que sigue es criterio mío**, no hecho verificado. Está pensado para poder ejecutarse por
partes, y cada pieza aporta valor sola.

## 8. Pulido ESTÉTICO

Restricción autoimpuesta: **no inventar ni un color, ni un radio, ni una fuente nuevos**. Todo sale
de `Theme.xaml` (paleta `:16-52`, pinceles `:54-73`, gradientes `:78-92`, tipografía `:142-163`,
sombras `:99-101`).

---

### P-1 · Barra de estado del mapa, fija, en vez de una caja que aparece y desaparece

**Problema estético actual**: la caja de `HoverInfo` (`MainWindow.xaml:3437-3441`) tiene
`Visibility` ligada a `EmptyToCollapsed`. Al entrar y salir el ratón del mapa, **el mapa cambia de
alto** (la caja está en `DockPanel.Dock="Bottom"` del mismo `DockPanel`), lo que provoca un salto
de layout constante mientras se trabaja.

**Propuesta**: sustituirla por una franja de **altura fija** (`Height="26"`), siempre presente,
fondo `BgElevatedBrush`, `CornerRadius="0,0,8,8"` (ya lo tiene), con **campos separados** en vez de
una única cadena, al estilo de la barra de TEdit pero con el lenguaje del tema:

```
[coord]  ·  [capa · profundidad]  ·  [tile (id)]  ·  [pared (id)]  ·  [líquido]        [zoom 100%]
```

- Separadores: `Border Width="1" Background="{StaticResource BorderBrush0}" Margin="10,5"`.
- Etiquetas en `CaptionText`, valores en `BodyText` — jerarquía por peso, no por color.
- Sin dato → `—` (guion largo), nunca vacío: la franja no cambia de tamaño jamás.
- El **nombre de la capa coloreado** con el color real que `map_colors.json` usa para el fondo de
  esa zona, ya cargado en `MapColorCatalog.Global` y ya usado por `WorldRenderer.cs:39`: Espacio /
  Cielo / Superficie / Subterráneo / Cavernas / Infierno. Es color con significado, no decorativo.
- Mover ahí el indicador de zoom, que hoy vive lejos, arriba del todo (`:3378-3379`).

El tooltip flotante que sigue al cursor (`:3604-3610`) **se queda**: fue un pedido explícito del
usuario (1-sep-2026, citado en `:3596-3598`) y no estorba.

**Beneficio colateral**: adoptar el recurso del `Viewbox StretchDirection="DownOnly"` de TEdit
(`MainWindow.xaml:276`) haría que esta franja **se encogiera tipográficamente** en vez de recortar
campos en ventanas estrechas — exactamente la clase de defecto que la auditoría de redimensionado
persiguió durante 288 mediciones.

---

### P-2 · Dos escalones de fondo en la barra lateral: entrada vs salida

**Problema actual**: los tres bloques de la barra lateral (cabecera + píldoras + cuadro de texto |
contenido de la categoría | resultados) están **todos sobre el mismo fondo**, sin ninguna
separación. Con 5 píldoras, un cuadro, un selector de vista, un botón, una lista de inventario, una
casilla, un resumen, dos flechas y otra lista, la columna es una sucesión indiferenciada de
elementos.

**Propuesta**, sin colores nuevos:

- El bloque de **resultados** (`:3730-3799`) va dentro de un `Border` con
  `Background="{StaticResource BgPrimaryBrush}"` (un escalón **por debajo** del fondo de panel) y
  `CornerRadius="8"`, con `Padding="10"`. Se lee de un vistazo como "lo que ha salido", frente a
  "lo que estoy pidiendo".
- Una línea de 1 px `BorderBrush0` entre el cuadro de búsqueda y el contenido de la categoría.
- Las cabeceras de grupo de Minerales (`:3935`, `:3938`, `:3941`) suben de `CaptionText` a
  `CaptionText` + `FontWeight="SemiBold"` + `Margin` superior de 10 px — hoy pesan lo mismo que las
  cifras que agrupan.

---

### P-3 · Marcador de resultado con la forma de retícula, tamaño constante y un pulso al llegar

Cierra E-03 por la vía estética además de la funcional (la implementación va en F-2).

- **Forma**: sustituir la elipse rellena (`:3573-3589`) por una **retícula hueca** de 4 barras
  (arriba/abajo/izquierda/derecha) en `TealBrush`, con hueco central: deja ver **qué** hay debajo,
  que es justo lo que un marcador de "está aquí" debe hacer. Es lo que hace TEdit
  (`WorldRenderXna.xaml.cs:6931-6942`) y por eso lo hace.
- **Tamaño**: 24 px de pantalla para el resultado activo, 10 px para el resto, **constantes**.
- **Pulso**: al llegar a un resultado (`GoToWorldSearchHit`/`MoveWorldSearchResult`), un
  `Storyboard` de un solo ciclo — `ScaleTransform` 1 → 1,6 → 1 en 400 ms con `EaseOut` + `Opacity`
  1 → 0 sobre un anillo. El proyecto ya usa `Storyboard`/`ColorAnimation`/`DoubleAnimation` en
  `Theme.xaml` (`:280-310`, `:373-398`) y ya tiene el precedente del "flash" de T-14 en Objetos, así
  que no es una técnica nueva.
- **Los NPCs y las estrellas de spawn**: mismo tratamiento de tamaño constante. Además, la estrella
  de spawn del personaje (`:3540`) y el marcador magenta de NPC sin cabeza (`:3514-3516`) compiten
  visualmente; con el marcador del mundo nuevo (F-7) serán **cuatro** familias de marcador sobre el
  mismo lienzo — conviene fijar una tabla de forma+color de una vez:

  | Marcador | Forma | Color |
  |---|---|---|
  | Resultado de búsqueda | retícula hueca | `TealBrush` |
  | Resultado **activo** | retícula hueca grande + pulso | `TealBrush` + borde blanco |
  | NPC | cabeza real (ya) / círculo | magenta (ya) |
  | Spawn del **personaje** | estrella (ya) | `AccentBrush` (ya) |
  | Spawn del **mundo** (nuevo) | casita / rombo | `OrangeBrush` |
  | Mazmorra (nueva) | rombo hueco | `CalamityBrush` |

---

### P-4 · Ordenar la barra superior: tres grupos, un solo idioma de icono, una sola altura

Cierra B-02 y B-03. Sin quitar ni un botón:

```
[ Cargar personaje (.plr)... ]  │  [ Buscar en el personaje ]  │  [ ↶ ] [ ↷ ] [ Deshacer último guardado ]  │  [ Guardar ]
       archivo (entrada)              búsqueda                        historial (edición + fichero)              archivo (salida)
```

- **Separadores**: `Border Width="1" Background="{StaticResource BorderBrush0}" Margin="12,4"`,
  entre los cuatro grupos. Es la misma idea del `<Separator Margin="1"/>` de TEdit
  (`MainWindow.xaml:117` y siguientes) trasladada a una toolbar horizontal.
- **Un solo idioma de icono**. Dos salidas coherentes; recomiendo la primera:
  1. **Quitar el emoji** `🔍` y dejar el rótulo en texto, como sus vecinos. Un emoji no obedece al
     `Foreground` del tema ni al estado deshabilitado, y es el único de toda la barra.
  2. O al revés: darle un icono a **todos** — pero eso es un rediseño mayor y requiere elegir una
     fuente de iconos, que el proyecto no tiene.
- **Una sola altura**: quitar el `Padding="8,3"` de `↶`/`↷` (`:1378`, `:1380`) y dejar que hereden
  el `14,8` del estilo base. Si se quiere que sigan siendo estrechos, `Padding="8,8"` — mismo alto,
  menos ancho.
- **Renombrar** `¿Dónde lo tengo?` → `Buscar en el personaje` (o mantener el rótulo y añadir el
  atajo al final, ver F-15): "¿Dónde lo tengo?" es simpático pero no dice **dónde** busca, y desde
  que existe también un buscador de mundo la ambigüedad es real.
- **Peso relativo**: hoy `Guardar` (`Tag="Orange"`) y `Cargar personaje` (`Tag="Accent"`) son los
  dos rellenos de la fila, y compiten. `Cargar` es una acción de arranque; `Guardar` es la
  consecuencia de todo el trabajo. Propongo bajar `Cargar personaje` a botón normal (sin `Tag`) una
  vez hay personaje cargado, y dejar el acento sólo cuando no lo hay — un `DataTrigger` sobre
  `IsCharacterLoaded`, mismo mecanismo que ya usa media app.

---

### P-5 · Un ritmo de espaciado único en la barra superior de Exploración

`MainWindow.xaml:3358-3389` usa hoy `Margin="20,0,0,0"` (dos veces), `Margin="10,0,0,0"`,
`Margin="8,0,0,0"` y `Margin="4,0,0,0"` en la misma fila. Propongo la escala que el resto del
proyecto ya usa de hecho — **4 / 6 / 10 / 14 / 20** — con una regla simple: 4 entre controles
hermanos (los `−`/`+`), 10 entre controles relacionados, 20 entre grupos. Sin cambiar el
`WrapPanel` (R-11 de la auditoría de redimensionado lo puso ahí a propósito).

---

### P-6 · Estado "sin resultados" con cuerpo, no un texto de 11 px

Cierra E-01 por el lado estético. Cuando la búsqueda termina con 0 resultados, en el hueco de la
lista:

- Texto principal en `BodyText`, centrado: **"Nada que coincida con «X» en este mundo"**.
- Segunda línea en `CaptionText`: la sugerencia real según el caso — en "Todo",
  *"Prueba con menos palabras, o con el id: #123"*; en las demás categorías,
  *"Este mundo no tiene ningún tile que coincida"*.
- Mismo patrón visual que el estado vacío que ya existe (`:3640-3656`), que está bien resuelto.

---

### P-7 · El id visible en la fila de inventario

`InventoryRowTemplate` (`:1281-1325`) muestra icono, nombre y recuento. Falta el id, que TEdit
muestra siempre a la derecha en gris (`TileWallPickerControl.xaml:103-107`,
`StringFormat='[{0}]'`). Terrakeep ya permite **buscar** por id (`LibrarySearchGrammar`, `#123`) y
ya filtra por id (`ApplyInventoryFilter:429`), pero **no enseña ninguno** — el usuario no puede
saber qué número escribir. Un `TextBlock` a la derecha, `CaptionText`, `[{Id}]`.

Cautela: esto añade ancho a una columna que la auditoría de redimensionado ya encontró apretada
(H-02). Debe ir con `TextTrimming` y volver a pasar la comprobación `AR-02` del arnés.

---

### P-8 · Detalles menores, todos de una línea

| Qué | Dónde | Cambio |
|---|---|---|
| El botón `Restablecer` no dice a qué restablece | `:3381` | `ToolTip="Volver al 100% (1 píxel = 1 tile)"` |
| `−` y `+` son los únicos botones sin tooltip de la fila | `:3377`, `:3380` | añadirlos, con el atajo cuando exista (F-9) |
| El `Expander` "NPCs que faltan" usa `CalamityBrush` (rojo) para nombres de NPC | `:3841` | el rojo en este tema significa Calamity/peligro; para "todavía no lo tienes" encaja mejor `TextSecondaryBrush` con la píldora ya existente, o `OrangeBrush` |
| La píldora `KindLabel` de cada resultado es siempre `TealBrush` | `:3784-3786` | un color por familia (Tile/Pared/Líquido/NPC/En cofre/Letrero/Veta) permite escanear la lista sin leer; la paleta ya tiene 6 tonos |
| El overlay de carga tapa el mapa con `#B0000000` opaco | `:3620` | con un mundo ya cargado y recargando otro, oscurecer al 69 % el mapa anterior es correcto; con lienzo vacío no aporta nada. Ligarlo a `IsWorldLoaded` |

---

## 9. Pulido FUNCIONAL / INTERNO

Ordenado por relación valor/esfuerzo, no por número de hallazgo.

---

### F-1 · Hacer visible el resumen de búsqueda siempre · cierra **E-01** · esfuerzo **trivial**

`MainWindow.xaml:3743-3752`. Sacar el `TextBlock` del resumen del `Grid` cuya visibilidad depende
del recuento, y dejarlo **fuera**, con su propia visibilidad `EmptyToCollapsed` (que ya tiene) —
así aparece siempre que haya texto que decir, incluido `"Sin resultados."`. Las flechas `‹`/`›` sí
deben seguir ligadas al recuento.

Es literalmente mover un `TextBlock` de sitio.

---

### F-2 · Marcadores de tamaño constante en pantalla · cierra **E-03** · esfuerzo **pequeño**

Tres caminos; recomiendo el (a):

**(a) Escala inversa por marcador.** Añadir un `RenderTransform` con un `ScaleTransform` cuyo
`ScaleX`/`ScaleY` estén ligados a `Exploration.Zoom` a través de un converter nuevo
`InverseValueConverter` (`1/valor`), con `RenderTransformOrigin="0.5,0.5"`. El marcador sigue
dentro del `Grid` escalado (así conserva su posición gratis por `Canvas.Left/Top` en coordenadas de
tile) pero se **des-escala** a sí mismo. Tres plantillas a tocar (`:3508-3518`, `:3539-3546`,
`:3572-3590`), un converter nuevo de 8 líneas, cero cambios de arquitectura.

Cautela real: un `RenderTransform` no afecta al layout, así que el marcador se dibujará
correctamente pero su **área de clic** no crecerá igual — hay que comprobar que el `ToolTip` y el
`Cursor="Hand"` siguen respondiendo donde se ve el marcador.

**(b)** Sacar los marcadores a un `Canvas` hermano sin transform y recalcular `Canvas.Left/Top` en
cada scroll/zoom. Correcto y es lo que hace TEdit por dentro, pero obliga a mover posicionamiento
al code-behind y a suscribirse a `ScrollChanged`.

**(c)** Dejarlo como está y compensar con F-3 (auto-zoom al navegar). No resuelve el problema, sólo
lo esconde.

---

### F-3 · Casilla "Acercar al ir a un resultado" · cierra **E-12** · esfuerzo **trivial**

Al lado de la casilla ya existente "Ordenar por distancia al spawn" (`:3740-3742`), una segunda:
**"Acercar al ir a un resultado"**, `[ObservableProperty] bool _autoZoomOnNavigate`, **apagada por
defecto** (igual que TEdit, `FindSidebarViewModel.cs:61`). Cuando está marcada,
`NavigateToTile` fija además un zoom de trabajo antes de centrar. TEdit usa `_zoom = 8`
(`WorldRenderXna.xaml.cs:8178`), que en su escala equivale a 8 píxeles por tile; el equivalente
razonable aquí es `Zoom = 4.0` (dentro del `MaxZoom = 6.0` ya existente).

El cambio en `OnNavigateToTile` (`MainWindow.xaml.cs:487-492`) es fijar el zoom, llamar a
`UpdateLayout()` y luego centrar — exactamente el mismo patrón que ya usa el zoom con rueda
(`:402-404`) y por el mismo motivo (que el `ScrollViewer` conozca el extent nuevo).

---

### F-4 · Indicador de "buscando" + botón Cancelar · cierra **E-02** · esfuerzo **pequeño**

1. `[ObservableProperty] private bool _isSearching;` en `ExplorationViewModel`, puesto a `true` al
   entrar en `RunWorldSearchAsyncWithQuery` (`:1061`) y a `false` en un `finally` — **con la
   guarda de generación** (`myGeneration == _worldSearchGeneration`), igual que ya hace
   `RefreshWorldsAsync:687`, para que una vuelta obsoleta no apague el indicador de la vuelta viva.
2. En el XAML, sobre la lista de resultados: una `ProgressBar IsIndeterminate="True" Height="3"`
   con `Foreground="{StaticResource AccentBrush}"` — el mismo lenguaje que el overlay de carga ya
   usa (`:3623`) y que el escaneo de mundos (`:3647`).
3. Un botón **Cancelar** (`Tag="Ghost"`) visible sólo con `IsSearching`, ligado a un
   `CancelWorldSearchCommand` nuevo que haga `_worldSearchCts?.Cancel()`. La infraestructura ya
   está entera; sólo falta el botón.

Recomiendo además **no** poner el indicador durante el filtrado en memoria de las otras cuatro
categorías (`ApplyInventoryFilter:422-435`): es inmediato y parpadearía.

---

### F-5 · Virtualizar las dos listas largas · cierra **E-04** · esfuerzo **pequeño**

Para la lista de resultados (`:3760-3762`) y los dos inventarios (`:3908-3909`, `:3973-3974`):
sustituir `ScrollViewer > ItemsControl` por un `ItemsControl` **con su propio `ScrollViewer` en la
plantilla** o directamente por un `ListBox` sin selección visual, con:

```xml
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.VirtualizationMode="Recycling"
VirtualizingPanel.ScrollUnit="Pixel"
ScrollViewer.CanContentScroll="True"
```

**El detalle que hay que respetar**: como avisa el comentario de TEdit
(`TileWallPickerControl.xaml:55`), **no puede quedar un `ScrollViewer` externo envolviéndolo**, o
la virtualización no se activa. Eso obliga a mover el `MaxHeight="240"` de la lista de resultados
del `ScrollViewer` al propio `ListBox`.

Cautela: las filas de inventario tienen `Visibility="{Binding IsMatch ...}"` (`:1282`) — con
virtualización por reciclado hay que confirmar que el filtro sigue comportándose (una fila
colapsada sigue ocupando un contenedor). Sería más limpio filtrar con un `ICollectionView` como
hace TEdit (`FilteredItemsView`, `TileWallPickerControl.xaml:58`) que con `IsMatch` por fila; pero
eso es un cambio mayor y el patrón `IsMatch` está establecido a propósito en este proyecto
(`WorldNpcRowViewModel:129`, con motivo real documentado en X-c).

---

### F-6 · Ampliar el hover a barra de estado real · cierra **E-07** y **E-08** · esfuerzo **medio**

En `UpdateHover` (`ExplorationViewModel.cs:734-758`), pasar de una cadena a **propiedades
separadas** (`HoverPosition`, `HoverDepth`, `HoverTile`, `HoverWall`, `HoverLiquid`), consumidas
por la franja de P-1. Contenido nuevo:

1. **El líquido, siempre** (E-08): sacar el líquido de la rama `!IsActive` y darle su propio
   campo, con la cantidad — `"Agua (255/255)"`. Corrección de 3 líneas.
2. **Capa y profundidad** (E-07). Los datos ya están; la fórmula real está citada en §2.2. Dos
   avisos:
   - `ZoneFor` (`WldHeader.cs:45-52`) ya da la zona **para el fondo del mapa**, y usa umbrales
     ligeramente distintos de los de la capa GPS de TEdit (`ZoneFor` usa `<80` para Espacio; la
     fórmula GPS usa el `spaceCheck` con el ratio de tamaño del mundo). **No** reutilizar `ZoneFor`
     tal cual para el texto de capa: implementar la fórmula GPS aparte, o asumir explícitamente la
     diferencia por escrito. Yo implementaría la GPS: es la que el jugador ve en el juego.
   - La unidad: Terraria usa **pies** (1 tile = 2 pies). Traducirlo a "tiles bajo el suelo" sería
     más coherente con el resto de la app (`DepthLabel` de NPC ya dice
     `"Bajo tierra (profundidad {n})"` en tiles, `:115`) pero menos coherente con el juego.
     Recomiendo **mostrar los dos**: `"Cavernas · 412 tiles bajo el suelo"`.
3. **Los ids entre corchetes** junto al nombre de tile y pared, como TEdit (`MouseTile.cs:59`,
   `:68`) — y como pide P-7 para el inventario.

---

### F-7 · Marcador del spawn del mundo y de la mazmorra · cierra **E-06** · esfuerzo **pequeño**

- **Spawn**: dato ya disponible (`_world.Header.SpawnX/Y`). Una colección de una sola entrada, o
  directamente dos propiedades `WorldSpawnX/Y` + un `ContentPresenter` sobre el mismo `Canvas`.
  Forma y color según la tabla de P-3.
- **Mazmorra**: hay que ampliar `ReadHeader` con cinco lecturas más
  (`Time` double, `DayTime` bool, `MoonPhase` int, `BloodMoon` bool, `IsEclipse` bool) antes de
  `DungeonX`/`DungeonY` — el orden exacto está en `World.FileV2.cs:2084-2090`, y el lector de
  Terrakeep ya sigue ese mismo fichero como fuente. **Añadir a `WldHeader` sólo lo que se vaya a
  usar**, respetando la política documentada en `WldHeader.cs:9-12` (lo que no se necesita, no se
  parsea) — pero avanzando el flujo correctamente.

---

### F-8 · Minimapa · cierra **E-05** · esfuerzo **medio**

Aprovechando que el bitmap del mundo ya existe y está congelado:

- Un `Border` en la esquina **superior derecha del mapa**, dentro del mismo `Grid` que ya aloja el
  overlay de carga (`:3435`, `HorizontalAlignment="Right" VerticalAlignment="Top" Margin="10"`),
  con `Image Source="{Binding Exploration.WorldImage}" Stretch="Uniform"` en una caja de
  ~240×70 px (proporción de un mundo Grande: 8400/2400 = 3,5).
- Encima, un `Rectangle` con `Stroke="{StaticResource AccentBrush}"` que dibuje el **viewport
  actual**: su posición y tamaño salen de `WorldMapScroll.HorizontalOffset/VerticalOffset/
  ViewportWidth/ViewportHeight` divididos por `Zoom` y escalados al minimapa. Hay que suscribirse a
  `ScrollChanged` en el code-behind (es donde ya viven todos los gestos del mapa).
- Clic → `NavigateToTile(x, y)`, con la conversión inversa. TEdit hace exactamente esto
  (`MainWindow.xaml.cs:946-957`).
- **Plegable** (un botón `⌄` de 16 px en su esquina) y recordado en ajustes: en una ventana a
  `MinWidth=1080` un minimapa fijo se come sitio real.

Cautela de calidad: `Stretch="Uniform"` sobre un bitmap de 8400 px reducido a 240 px con el
`NearestNeighbor` que la `Image` grande usa daría un resultado ruidoso. Para el minimapa hay que
dejar el escalado por defecto (`HighQuality`), que es justamente lo que TEdit pone
(`MainWindow.xaml:340`).

---

### F-9 · Atajos de teclado del mapa, anunciados · cierra **E-09** · esfuerzo **pequeño**

Añadir a `OnWindowKeyDown` (`MainWindow.xaml.cs:106-168`), **sólo cuando `SelectedTabIndex == 4`**
para no colisionar con el resto de la app:

| Tecla | Acción | Comando existente |
|---|---|---|
| `+` / `-` | zoom | `ZoomInCommand` / `ZoomOutCommand` (`:905-906`) |
| `0` | ajustar a la ventana | `OnFitToWindowClick` (`:362`) |
| `1` | 100 % | `ZoomResetCommand` (`:907`) |
| `F3` / `Shift+F3` | resultado siguiente / anterior | `NextWorldSearchResultCommand` / `Previous…` (`:532-534`) |
| `Ctrl+Shift+F` | foco en el cuadro de búsqueda del mundo | (nuevo, mismo patrón que `Ctrl+F`, `:136-151`) |
| flechas | desplazar el mapa | (nuevo, sobre `WorldMapScroll`) |

**Y anunciarlos**, que es la mitad del valor: en el `ToolTip` de cada botón, como ya hace la app
por T-H/F1 (`Cargar personaje` → `"Ctrl+O"`, `:1363`) y como hace TEdit por converter. Además,
`Ctrl+1…6` ya está tomado por las pestañas raíz (`:163-167`), así que `1` a secas es seguro
mientras se compruebe que el foco no está en un `TextBox` — misma guarda que ya usan `Ctrl+Z`/`Y`
(`:126`, `:132`).

---

### F-10 · `GridSplitter` y plegado de la barra lateral · cierra **E-10** · esfuerzo **pequeño**

- Cambiar el `Grid` de dos columnas (`:3414-3432`) a tres: `*` / `Auto` / `Auto`, con un
  `GridSplitter Width="5"` en la del medio y la plantilla de TEdit (1 px en reposo, 3 px de acento
  al pasar el ratón, `MainWindow.xaml:451-477`) reimplementada con los pinceles del tema.
- Convertir la columna lateral a `Width="320" MinWidth="260" MaxWidth="520"`.
- Un botón `›` de plegado en la cabecera de la barra lateral (junto a "Buscar en el mundo",
  `:3668`) que alterne entre el ancho guardado y 0.
- Persistir el ancho junto con el resto de ajustes.

**Interacción real con la auditoría de redimensionado**: `ExplorationSidebarMaxWidth` (`:3660`) fue
puesto ahí por R-02/H-02 con un motivo medido y correcto (una `ColumnDefinition Width="Auto"` mide
con ancho infinito antes de aplicar el `MaxWidth`, y por eso el `WrapPanel` de píldoras no
envolvía). **Cambiar a un ancho fijo con `GridSplitter` elimina ese problema de raíz**, pero exige
volver a pasar `AR-02` del arnés a los 14 tamaños.

---

### F-11 · Recordar la vista por mundo · cierra **E-11** · esfuerzo **pequeño**

Un `Dictionary<string, (double Zoom, double OffsetH, double OffsetV)>` persistido junto al resto
de ajustes de la app, con la **misma escritura atómica** que usa TEdit (escribir a `.tmp` y
`File.Move(overwrite: true)`, `WorldViewStateManager.cs:59-61`) — un patrón que este proyecto ya
conoce (`CharacterFileService.WriteAtomic`, citado en `MainWindow.xaml:1382-1384`).

Guardar al cambiar de pestaña, al cargar otro mundo y al cerrar la ventana
(`OnWindowClosing` ya existe, `MainWindow.xaml:13`). Y una regla de sentido común que TEdit no
tiene: **si no hay estado guardado para ese mundo, arrancar en "Ajustar a la ventana"**, no al
100 % — el 100 % en un mundo Grande enseña el 12 % del ancho (`:3382-3384`).

---

### F-12 · Exportar el mapa a PNG · cierra **E-13** · esfuerzo **trivial**

Un botón en la barra superior de Exploración: `PngBitmapEncoder` + `BitmapFrame.Create(WorldImage)`
+ `SaveFileDialog`. Dos matices que lo hacen mejor que "guardar el bitmap":

- Ofrecer **con y sin la capa de resaltado** (`WorldHighlight`) — si hay marcas de mineral activas,
  exportarlas es justo lo que se querría.
- Nombre por defecto `{TítuloDelMundo}-mapa.png`.

---

### F-13 · Arrastrar y soltar sobre la ventana · cierra **E-14** · esfuerzo **trivial**

`AllowDrop="True"` en la `Window` (`MainWindow.xaml:12-13`) + un manejador que mire la extensión:
`.wld` → `Exploration.LoadFromPathAsync` y saltar a la pestaña 4; `.plr` → el mismo camino que
`OnLoadClick`. TEdit lo hace en 12 líneas (`MainWindow.xaml.cs:959-970`).

Cautela: hay dos `AllowDrop` internos (slots de objeto y de buff, `:310`, `:782`) — hay que
comprobar que el manejador de ventana no se traga sus `Drop` (el evento burbujea; marcar
`e.Handled` en los internos, que ya lo harán, o filtrar por `DataFormats.FileDrop`).

---

### F-14 · Panel "Este mundo" · cierra **E-16** y **E-17** · esfuerzo **medio**

Una sexta píldora de categoría, **"Este mundo"**, o mejor un `Expander` bajo la cabecera de la
barra lateral (para no tocar el `WrapPanel` de 5 píldoras que ya dio problemas de recorte). Con dos
bloques:

**(a) Identidad** — requiere conservar dos campos que hoy se leen y se tiran, y ampliar el flujo:

| Dato | Coste |
|---|---|
| **Semilla** | 0 — ya se lee, sólo hay que no descartarla (`WldReader.cs:95-96`) |
| **Modo de juego** (Clásico / Experto / Maestro / Viaje) | 0 — ya se lee (`:111`) |
| Tamaño y proporción | 0 — ya está |
| Versión del formato | 0 — ya está (`WldHeader.Version`) |
| **Corrupción / Carmesí** | bajo — `IsCrimson` está 12 campos más allá de `RockLevel` |
| **Modo difícil** | medio — `HardMode` está bastante más adelante (`World.FileV2.cs:2119`) |
| Jefes derrotados | medio — el bloque entero `:2094-2113` |

**(b) Censo**, todo ya calculado en `WorldPresenceIndex`:

- `% de aire` y top 10 de tiles **con porcentaje** (como `WorldAnalysis.cs:100-116`),
- número de cofres, letreros, NPCs, tile entities,
- tipos distintos de tile/pared presentes (los contadores de las píldoras ya los tienen,
  `ChestsPillCount`/`OresPillCount`/`ObjectsPillCount`, `:280-282`),
- y un botón **"Guardar informe (.txt)"**, como `AnalyzeWorldSaveCommand` de TEdit.

**Recomendación de alcance**: hacer sólo (a)-coste-0 + (b) en una primera pasada. Semilla y modo de
juego son literalmente borrar dos descartes; el resto de la cabecera exige avanzar el lector, que
es donde está el riesgo real de desalineación.

---

### F-15 · Barra superior consciente de la pestaña · cierra **B-01** y **B-06** · esfuerzo **medio**

La columna central (`Grid.Column="1"`, hoy sólo franja vital) pasa a tener **dos contenidos
excluyentes**, con el mismo mecanismo de `DataTrigger` sobre una propiedad de la ViewModel que la
app ya usa por todas partes:

- pestañas de personaje (0-3) → la franja vital de siempre, sin cambios;
- **Exploración (4)** → franja del **mundo**: título · tamaño (`8400×2400`) · zoom actual ·
  coordenada bajo el cursor · capa. Los datos son exactamente los de F-6, y así la información del
  mapa está disponible **incluso con la barra lateral plegada** (F-10).
- Y en la columna 2, ocultar (no sólo deshabilitar) los controles de personaje que no aplican
  cuando no hay personaje cargado — o al revés, mantenerlos deshabilitados pero **agrupados** (P-4)
  para que el hueco se lea como "cosas de personaje", no como botones rotos.

Es la extensión natural de lo que H5-10 hizo para Personaje, aplicada a las otras pestañas.

---

### F-16 · Atajo y descubribilidad de "¿Dónde lo tengo?" · cierra **B-04** · esfuerzo **trivial**

- `Ctrl+Shift+F` → `ToggleWhereIsItCommand`, con el mismo enfoque diferido que ya usa `Ctrl+F`
  (`MainWindow.xaml.cs:144-149`) — de hecho `OnWhereIsItPopupOpened` (`:183-189`) ya hace el foco,
  así que basta con ejecutar el comando.
- Añadir `"(Ctrl+Shift+F)"` al final del `ToolTip` (`:1371`), como hace el resto de la barra.
- Cuando no hay personaje: en vez de ocultarlo (`:1372`), **deshabilitarlo** con
  `ToolTipService.ShowOnDisabled="True"` y el texto *"Carga un personaje primero"* — que es
  exactamente el patrón que la propia app ya usa en las 5 píldoras de Exploración
  (`:3686`, `:3691`, `:3696`, `:3701`, `:3706`). Así la barra no cambia de forma al cargar.

---

### F-17 · `Escape` cierra el popup · cierra **B-05** · esfuerzo **trivial**

En `OnWindowKeyDown`, antes de la rama actual de `Escape` (`:152-158`):
`if (_viewModel.IsWhereIsItOpen) { _viewModel.IsWhereIsItOpen = false; e.Handled = true; return; }`.
Verificar primero si WPF ya lo hace solo con `StaysOpen="False"` (§12) para no añadir código
redundante.

---

### F-18 · Semillas de mejora que NO recomiendo hacer ahora

Por simetría con `ESPEC-ui-exploracion.md#16`, dejo por escrito lo que **no** haría, para que no se
cuele en una implementación posterior:

- **No** portar el motor de scripting de TEdit (`FinderApi`/`BatchApi`). Ya descartado y sigue
  siendo desproporcionado.
- **No** portar el overlay que oscurece el mundo (máscara por chunks + shader). El mapa es un
  `WriteableBitmap` congelado y esa decisión está tomada dos veces
  (`ESPEC-buscador-mundo-tedit.md#5.3`, `ESPEC-ui-exploracion.md#13`).
- **No** añadir un botón "Buscar" explícito. La app eligió buscar al teclear; F-4 resuelve el
  problema real (saber que está buscando) sin cambiar el gesto.
- **No** portar la *activity bar* de 16 iconos. Terrakeep tiene 5 categorías, no 16 paneles, y las
  píldoras con contador ya funcionan bien.
- **No** leer pintura, cables ni actuadores del `.wld` sólo para el hover. Es un cambio real de
  `WldReader` (`:230`, `:237` los descartan hoy) para un dato que un localizador no necesita.
- **No** tocar los umbrales de `SizeClass` (`MainViewModel.cs:378-389`): están medidos y
  documentados por la auditoría anterior.

---

## 10. Tabla de correcciones priorizada

Severidad tal como se definió en §5. **Esfuerzo** es criterio mío. **Criterio de aceptación** está
escrito para poder comprobarse con el arnés real (`TerrasavrNative.App.Tests/Program.cs`) o a mano
sin ambigüedad.

### Bloque A — Defectos reales (antes de entregar)

| Corr. | Hallazgo | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **F-1** | E-01 | `MainWindow.xaml:3743-3752` | trivial | Con un mundo cargado, escribir en "Todo" un texto que no exista (p. ej. `zzzz`): tras el debounce, el árbol visual contiene un `TextBlock` **visible** con `"Sin resultados."`. Comprobable con el mismo recorrido de árbol que ya usa `AR-02` |
| **F-4** | E-02 | `ExplorationViewModel.cs:1061-1102` + `MainWindow.xaml:3730-3799` | pequeño | `IsSearching == true` entre el arranque del `Task.Run` y su fin; `IsSearching == false` tras cancelar; el botón Cancelar existe en el árbol visual sólo con `IsSearching`; una vuelta obsoleta que termine tarde **no** apaga el indicador de la vuelta viva |
| **F-2** | E-03 | `MainWindow.xaml:3508-3518`, `:3539-3546`, `:3572-3590` + converter nuevo | pequeño | El `ActualWidth` **en coordenadas de ventana** (`TransformToAncestor(window).TransformBounds`) de un marcador de resultado es el mismo (±1 px) a `Zoom=0.05`, `Zoom=1.0` y `Zoom=6.0`. Y el marcador sigue respondiendo al `ToolTip` en su área visible |
| **F-5** | E-04 | `MainWindow.xaml:3760-3762`, `:3908-3909`, `:3973-3974` | pequeño | Con 1000 resultados en la lista, el número de contenedores realizados (`ItemContainerGenerator`) es **menor que 100**; ningún `ScrollViewer` externo envuelve la lista |

### Bloque B — Capacidad ausente que se nota (recomendado antes de entregar)

| Corr. | Hallazgo | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **F-6** | E-07, E-08 | `ExplorationViewModel.cs:734-758` + `MainWindow.xaml:3437-3441` | medio | Sobre un tile activo con `LiquidAmount > 0`, el texto nombra **el tile y el líquido**. La capa mostrada coincide con la fórmula GPS en los 5 casos límite (Espacio / Cielo / Superficie / Subterráneo / Cavernas / Infierno), comprobado con coordenadas fijas sobre un mundo real |
| **F-3** | E-12 | `MainWindow.xaml.cs:487-492` + `ExplorationViewModel.cs` | trivial | La casilla existe, arranca **desmarcada**, y marcada hace que `Zoom` cambie al navegar; desmarcada, `Zoom` no cambia (comportamiento de hoy, sin regresión) |
| **P-1 + P-6** | E-01, estética | `MainWindow.xaml:3437-3441`, `:3730-3799` | medio | El alto del contenedor del mapa **no cambia** al entrar y salir el ratón del mapa (medido con `ActualHeight` antes y después) |
| **F-7** | E-06 | `MainWindow.xaml` (`Canvas` del mapa) + `WldReader.cs:139` | pequeño | Con un mundo cargado hay un marcador en `Header.SpawnX/Y` distinguible de los del personaje. Si se amplía el lector: `ReadHeader` sigue devolviendo `TilesWide/High/SpawnX/Y/GroundLevel/RockLevel` idénticos a los de hoy en los 3 mundos reales de esta máquina (prueba de no regresión del formato) |
| **F-9** | E-09 | `MainWindow.xaml.cs:106-168` | pequeño | Los 6 atajos funcionan **sólo** con la pestaña Exploración activa y **no** cuando el foco está en un `TextBox`; los tooltips de los botones equivalentes muestran la combinación |
| **P-4** | B-02, B-03 | `MainWindow.xaml:1358-1388` | pequeño | Los 6 controles de la barra tienen el **mismo alto real** (`ActualHeight`, medido en la ventana); hay 3 separadores en el árbol visual; no queda ningún emoji en la fila. Volver a pasar `AR-04`/`H-04` de la auditoría de redimensionado a los 14 tamaños |
| **F-16** | B-04 | `MainWindow.xaml.cs:106-168`, `MainWindow.xaml:1369-1372` | trivial | `Ctrl+Shift+F` abre el popup y el cuadro queda enfocado; sin personaje el botón **existe deshabilitado** y su tooltip se muestra |

### Bloque C — Mejoras de peso (después de entregar, o si sobra tiempo)

| Corr. | Hallazgo | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **F-10** | E-10 | `MainWindow.xaml:3414-3432`, `:3660` | pequeño | La barra lateral se puede arrastrar entre 260 y 520 px y plegar a 0; **`AR-02` sigue en verde a los 14 tamaños** y en las 5 categorías |
| **F-8** | E-05 | `MainWindow.xaml:3435` + `MainWindow.xaml.cs` | medio | El minimapa muestra el mundo entero; el rectángulo de viewport se mueve al hacer scroll; un clic centra el mapa en el punto correcto (±5 tiles) |
| **F-11** | E-11 | ajustes + `ExplorationViewModel.cs:825` | pequeño | Cargar mundo A, desplazarse, cargar B, volver a A → mismo zoom y offset (±2 px). Primer arranque de un mundo nunca visto → "ajustar a la ventana", no 100 % |
| **F-14** | E-16, E-17 | `WldReader.cs:95-96`, `:111`, `WldHeader.cs` + XAML | medio | Semilla y modo de juego se muestran y coinciden con lo que muestra el propio Terraria para ese mundo. La lectura del resto del `.wld` no cambia (misma prueba de no regresión que F-7) |
| **F-15** | B-01, B-06 | `MainWindow.xaml:1442-1480` | medio | En Exploración la columna central muestra datos del mundo; en las pestañas 0-3 muestra la franja vital de siempre, sin cambios |
| **F-12** | E-13 | nuevo | trivial | El PNG exportado tiene exactamente `TilesWide × TilesHigh` píxeles y es idéntico píxel a píxel al `WorldImage` |
| **F-13** | E-14 | `MainWindow.xaml:12-13` | trivial | Soltar un `.wld` sobre la ventana lo carga y salta a Exploración; soltar un objeto de la Librería sobre un slot **sigue funcionando** |

### Bloque D — Pulido estético suelto

| Corr. | Hallazgo | Fichero real | Esfuerzo |
|---|---|---|---|
| **P-2** (dos escalones de fondo en la barra lateral) | estética | `MainWindow.xaml:3730-3799`, `:3935-3941` | trivial |
| **P-3** (retícula + pulso + tabla de marcadores) | E-03, estética | `MainWindow.xaml:3572-3590` | pequeño |
| **P-5** (ritmo de espaciado) | estética | `MainWindow.xaml:3358-3389` | trivial |
| **P-7** (id visible en la fila de inventario) | descubribilidad | `MainWindow.xaml:1281-1325` | trivial (pero repasar `AR-02`) |
| **P-8** (5 detalles menores) | estética | varias | trivial |
| **B-07** (affordance del nombre del personaje) | B-07 | `MainWindow.xaml:1400-1403` | trivial |

### Orden de ejecución sugerido

1. **F-1** (trivial, cierra el peor defecto) → **F-3** → **F-16** → **F-17** → **P-8** — todos
   triviales, todos independientes, una sola sesión.
2. **F-4** y **F-5** — las dos tocan la misma zona del XAML de resultados; hacerlas juntas.
3. **F-2** + **P-3** — el marcador entero de una vez.
4. **F-6** + **P-1** — la barra de estado entera de una vez; **F-6 antes**, porque P-1 consume sus
   propiedades.
5. **P-4** + **F-15** — la barra superior entera de una vez, y volver a pasar la matriz de
   redimensionado.
6. **F-9**, **F-7**, **F-13**, **F-12** — independientes, en cualquier orden.
7. Bloque C, si hay tiempo: **F-10** antes que **F-8** (el minimapa compite por el mismo espacio que
   el mapa, y con la barra lateral plegable el reparto cambia).

---

## 11. Cómo verificarlo con el arnés que ya existe

`TerrasavrNative.App.Tests/Program.cs` ya tiene comprobaciones etiquetadas y reales para esta
pantalla, que sirven de plantilla y de red de seguridad:

- `H5-05-BOTON` (`:1010-1015`) — el botón de la barra superior es visible **en cualquier pestaña**,
  probado a propósito desde Exploración. **Cualquier cambio de P-4/F-16 debe mantenerlo en verde.**
- `H5-11-PILDORA` y `H5-11-TIRA-PERMANENTE` (`:2087-2100`) — la tira de mundos y el `IsCurrent`.
- `AR-02` (`:2532-2553`) — recorre las 5 categorías a varios anchos y **falla si la barra lateral
  recorta un solo píxel**. Es la comprobación que más riesgo corre con F-10 y P-7.
- `BUSCADOR-MUNDO-FASE2` (`:2326`) y `CATEGORIAS-EXPLORACION` (`:2530`).
- `S-C-MAPA` (`:3081-3098`) — "Ver en el mapa" salta a la pestaña 4 y pide el tile correcto;
  cualquier cambio en `NavigateToTile` (F-3) tiene que seguir pasándolo.

Comprobaciones nuevas que yo añadiría, una por corrección del Bloque A, con el mismo estilo de
etiqueta:

```
A8-01-SINRESULTADOS   busca "zzzz" -> existe un TextBlock VISIBLE con "Sin resultados."
A8-02-BUSCANDO        IsSearching true durante el Task.Run, false al acabar y al cancelar
A8-03-MARCADOR        ancho real del marcador en coords de ventana igual a Zoom 0.05 / 1.0 / 6.0
A8-04-VIRTUAL         con 1000 resultados, contenedores realizados < 100
A8-05-HOVER-LIQUIDO   sobre un tile activo con liquido, el texto nombra los dos
A8-06-BARRA-ALTURAS   los 6 botones de la cabecera tienen el mismo ActualHeight
```

---

# PARTE III

## 12. Riesgos conocidos y lo que NO verifiqué

### Sobre el método

- **No ejecuté ninguna de las dos aplicaciones.** Ni TEdit ni Terrakeep. Todo este documento es
  lectura de código fuente. Ninguna afirmación se apoya en una captura de pantalla.
- **No compilé nada** ni pasé el arnés. Ningún fragmento de la Parte II ha visto un compilador.
- **No medí ningún tiempo.** Las cifras que cito (~1,4 s para leer+pintar, 5 s vs 255 ms del
  recuento de vetas, 8400×2400, 260 tipos de tile, 65-96 % de candidatos inexistentes) son **citas
  de comentarios y documentos ya existentes en el proyecto**, no mediciones mías, y algunas de
  ellas —las de `ESPEC-ui-exploracion.md#6`— vienen a su vez de un port a JavaScript, no del C#
  real, según reconoce su propia sección 17.

### Hallazgos con una parte no verificada

- **E-03** (marcadores que escalan) es un **hecho estructural del árbol visual**, no una
  observación. Que un `LayoutTransform` afecta a todo el subárbol es comportamiento documentado de
  WPF y el XAML es inequívoco; **el efecto visual concreto a zoom bajo no lo vi.** Los tamaños en
  píxeles que doy son aritmética sobre valores del XAML.
- **E-04** (virtualización) igual: que un `ItemsControl` desnudo no virtualiza y que un
  `ScrollViewer` externo desactiva la virtualización del panel interno es comportamiento conocido de
  WPF, pero **no medí el coste real** ni conté contenedores realizados. Es posible que con 1000
  filas ligeras el impacto sea aceptable en una máquina moderna; el criterio de aceptación de F-5
  está escrito precisamente para medirlo.
- **B-05** (`Escape` y el popup): **no verifiqué** si WPF cierra por sí solo un `Popup` con
  `StaysOpen="False"` al pulsar `Escape`. Lo único que afirmo es que **no hay código propio que lo
  haga**. Comprobarlo antes de implementar F-17.
- **§7.2** (contraste): calculado con la fórmula WCAG sobre los hexadecimales de `Theme.xaml`, no
  medido sobre píxeles pintados. **No calculé** el contraste del texto sobre los cuatro gradientes
  (`AccentGradientBrush`, `OrangeGradientBrush`, `TealGradientBrush`, `PinkGradientBrush`), donde el
  valor cambia a lo largo del degradado.
- **F-6** (capa/profundidad): la fórmula GPS que transcribo de `MouseTile.cs:115-153` la leí entera,
  pero **no la comprobé contra el juego** ni contra un mundo real. El aviso sobre la divergencia
  entre `ZoneFor` y el `spaceCheck` de la fórmula GPS es una lectura comparada de los dos
  algoritmos, no una prueba con datos.

### Lo que no leí de TEdit

- **`ViewModel/WorldViewModel.cs` (4482 líneas)** y **`View/WorldRenderXna.xaml.cs` (8757)** no los
  leí enteros — sólo las secciones citadas en §1.1. Es posible que haya funcionalidad de interfaz
  relevante en las partes que no abrí.
- **`View/Sidebar/WorldPropertiesView.xaml` (1067 líneas)**: leí sólo su cabecera (`:1-90`) para
  saber qué expone. No repasé campo a campo.
- **`Render/PixelMapManager.cs`, `PixelMap.cs`, `Textures.cs`** y todo el detalle de cómo TEdit
  compone el mapa a partir de texturas reales. No hace falta para esta auditoría (Terrakeep no va a
  pintar texturas) pero es la razón por la que no comparo calidad de render.
- **`GlobalStyles.xaml`** (~70 KB, mismo hueco que `ESPEC-ui-exploracion.md#17`): los estilos
  `ActivityBarItem`/`ActivityBarTabControl` los cito por su uso en `MainWindow.xaml`, no por su
  `ControlTemplate`.
- **La toolbar contextual por herramienta** (`View/TopPanel/*`, 6 vistas, hasta 503 líneas cada
  una): son de edición pura, fuera de alcance para un visor de sólo lectura.
- **`Input/InputService.cs`** lo leí por secciones (`:60-560`); las últimas 250 líneas (persistencia
  de las personalizaciones, resolución de la rueda) no.

### Lo que no leí de Terrakeep

- **`MainWindow.xaml` completo**: leí `:1185-1535` y `:3350-3981` (unas 1000 de 4165 líneas). Las
  otras pestañas (Personaje, Objetos, Builds, Novedades) están fuera del encargo y no las repasé.
- **`MainViewModel.cs` completo** (leí `:140-180`, `:364-480`, `:780-910`, `:1060-1200`).
- **`Theme.xaml` completo**: leí `:140-170`, `:240-500` y `:1265-1406`. No repasé los estilos de
  `ComboBox`, `Slider`, `ScrollBar`, `TabItem` ni `NavCardButton`.
- **El arnés `TerrasavrNative.App.Tests/Program.cs`** (3400+ líneas) sólo lo recorrí por `grep`
  para localizar las comprobaciones de Exploración; **no lo leí**, así que la lista de §11 puede
  estar incompleta.

### Riesgos de las propuestas

- **F-5 (virtualización) y el filtro `IsMatch`**: el patrón de "atenuar/ocultar por fila en vez de
  quitar de la colección" está establecido a propósito en este proyecto por un bug real (X-c,
  `ExplorationViewModel.cs:122-129`). Virtualizar con reciclado y filas colapsadas es una
  combinación que **no he probado** y que puede dar saltos de scroll. El camino limpio sería un
  `ICollectionView` como el de TEdit, pero es un cambio mayor.
- **F-10 (`GridSplitter`) toca directamente lo que R-02/H-02 arregló.** El `MaxWidth` en el
  `DockPanel` en vez de en la `ColumnDefinition` está donde está por una medición real. Cambiar el
  reparto obliga a repetir `AR-02` entero.
- **F-7 y F-14 tocan `WldReader`**, que es el único punto donde un error no da un fallo visible sino
  **datos silenciosamente corruptos** por desalineación del flujo. La política actual
  (`WldHeader.cs:9-12`: no parsear lo que no se necesita) existe precisamente para evitarlo.
  Cualquier ampliación debe llevar la prueba de no regresión que propongo en el criterio de
  aceptación de F-7.
- **P-7 (id visible) y P-1 (barra de estado con más campos)** añaden ancho a dos sitios que la
  auditoría de redimensionado ya encontró apretados. Los dos necesitan repasar la matriz de 14
  tamaños.
- **La licencia MS-PL de TEdit** (§ metadatos) es un riesgo **legal**, no técnico, y sólo se
  materializa si alguien copia código literal. Este documento no lo hace y las propuestas están
  descritas para reimplementarse.

### Bloqueos de herramienta

**Ninguno.** El clon del repositorio, la lectura de los ficheros y todas las búsquedas funcionaron
a la primera. Nada que anotar en `bitacora.md` por ese lado.
