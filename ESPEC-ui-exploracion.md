# La UI de Exploración: auditoría de TEdit + del estado real de Terrakeep, y diseño de la barra lateral nueva

Segunda ronda de ingeniería inversa sobre **TEdit** (github.com/TEdit/Terraria-Map-Editor), esta
vez centrada en su **interfaz** — el hueco que dejó `ESPEC-buscador-mundo-tedit.md`, que cubrió
el motor de búsqueda y el formato de fichero pero apenas tocó cómo se ve y se organiza el panel
lateral real. Añade además una **auditoría completa del estado actual de la pestaña Exploración
de Terrakeep**, para que el diseño nuevo se apoye en lo que ya existe de verdad y no en abstracto.

**Encargo del usuario (verbatim, 4-sep-2026)**:

> "dile a opus que haga una super auditoría y programación inversa de tedit para ver como
> funciona y que te de instrucciones muy claras de como deberia quedar la ui de exploración para
> que podamos llevar a cabo todo lo anterior mencionado no buscamos un editor como tedit
> buscamos un visualizador de mundo de solo lectura pero que puedas buscar cualquier elemento en
> el mapa incluso el contenido de dentro de los cofres esparcidos por el mundo asi como si hay
> npcs escondidos en el subsuelo que puedas encontrarlos fácilmente y una nueva barra lateral de
> buscador en exploración que ya no solo salgan los npc eso se traslada dentro de una rama madre
> donde salgan varios botones como buscar npcs buscador de cofres buscador o marcador de
> minerales buscador de objetos en el mundo y que entre todas las opciones solo puedan salir los
> objetos que tiene ese mundo los que no ha habido suerte que en ese mundo se generen que no
> salgan en la búsqueda"

**Disciplina de este documento** (la misma que estableció `ESPEC-buscador-mundo-tedit.md`):

- **Parte I (secciones 1-7) = HECHOS.** Todo lleva su fichero y su línea reales. Lo de TEdit
  sale de un clon real de la rama `main` (`git clone --depth 1`, hecho en esta sesión); lo de
  Terrakeep, de leer los ficheros del repo enteros. Nada de memoria.
- **Parte II (secciones 8-16) = PROPUESTA.** Criterio mío, marcado como tal, no hecho verificado.
- **Parte III (sección 17) = HUECOS.** Lo que no llegué a comprobar.

**Restricción dura que NO cambia**: Terrakeep sigue siendo un **visor de solo lectura**. Nunca
escribe un `.wld` (`TerrasavrNative.Core/WldFormat/WldWorld.cs:4-5`, literal: *"Solo lectura -
nunca se escribe un .wld desde esta app"*). Nada de lo que hay aquí lo cambia.

---

# PARTE I — HECHOS

## 1. La interfaz real de TEdit

### 1.1 No hay "pestañas": hay una *activity bar* de iconos, estilo VS Code

Lo primero que hay que entender es que TEdit **no** mete sus herramientas en pestañas de texto.
`src/TEdit/MainWindow.xaml:480-484` monta un `TabControl` lateral con un estilo propio:

```xml
<!-- Side Panel: Activity Bar + Content (click active tab to collapse) -->
<TabControl x:Name="SidePanelTabs" Grid.Column="2"
            Style="{StaticResource ActivityBarTabControl}"
            SelectedIndex="{Binding SelectedTabIndex, Mode=TwoWay}"
            PreviewMouseLeftButtonDown="SidePanelTabs_PreviewMouseLeftButtonDown">
```

Cada `TabItem` usa `Style="{StaticResource ActivityBarItem}"` y su `Header` es **un icono, no
texto**; el nombre de la herramienta vive solo en el `ToolTip`. Ejemplo real, el propio buscador
(`MainWindow.xaml:547-550`):

```xml
<TabItem Style="{StaticResource ActivityBarItem}" ToolTip="{x:Static p:Language.sidebar_find_tooltip}">
    <TabItem.Header><ui:SymbolIcon Symbol="Search20" Filled="True" FontSize="24"/></TabItem.Header>
    <Sidebar:FindSidebarView />
</TabItem>
```

El estilo `ActivityBarItem` está en `src/TEdit/Themes/GlobalStyles.xaml:852-878`: casilla
cuadrada de **48x48** (`ActivityBarItemSize`, `:30`) con `Padding` de 12 (`:32`).

Hay **quince** de estas barras laterales, en este orden real (`MainWindow.xaml:485-568`):
Propiedades del mundo (`Globe20`), Paleta (`PaintBrush20`), Tiles especiales, Sprites (`Couch20`),
Portapapeles (`Clipboard20`), NPC (`People20`), Análisis (`ChartMultiple20`), Bestiario
(`Trophy20`), Banners, Tile entities, Poderes creativos (`Sparkle20`), **Filtro** (`Filter20`),
**Buscar** (`Search20`), Scripting (`Code20`), Explorador NBT, Editor de jugador.

Detalle de usabilidad real: el `PreviewMouseLeftButtonDown` del `TabControl` implementa
*"click active tab to collapse"* — volver a pulsar el icono ya activo pliega el panel entero.

**Conclusión para el diseño**: el "botón madre con hijos" que pide el usuario **no existe como
tal en TEdit**. Lo más parecido es esta activity bar de iconos, que es un nivel *por encima* de
lo que nos ocupa: en TEdit, "Buscar" es UNA de las quince barras, y dentro de ella hay cuatro
pestañas. Es decir, TEdit sí usa dos niveles de jerarquía, pero el primero lo resuelve con
iconos y el segundo con pestañas de texto.

### 1.2 Anatomía real de `FindSidebarView.xaml` (137 líneas), bloque a bloque

El fichero declara `d:DesignHeight="700" d:DesignWidth="400"` (`:12`) — **400 px de ancho de
diseño**, dato relevante porque la columna lateral de Terrakeep hoy mide entre 220 y 300 (ver 5.4).

La raíz es un `DockPanel` (`:19`) con este reparto, en el orden real de declaración (que en un
`DockPanel` es el orden de prioridad):

| Orden | `DockPanel.Dock` | Contenido | Línea |
|---|---|---|---|
| 1 | `Top` | Título del panel (`SidebarPanelTitle`) | `:22` |
| 2 | `Bottom` | Barra de navegación + lista de resultados | `:25-75` |
| 3 | `Bottom` | Resumen de selección, opciones y botones de acción | `:78-117` |
| 4 | (relleno) | `TabControl` con los cuatro pickers | `:120-134` |

Es decir: **el picker ocupa todo el espacio sobrante y los resultados viven abajo, con altura
fija**. La lista de resultados es un `ListBox` de `Height="150"` **fija** (`:57`) con
virtualización explícita (`:58-59`):

```xml
<ListBox ItemsSource="{Binding Results}"
         SelectedItem="{Binding SelectedResult}"
         Height="150"
         VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
```

La plantilla de fila es **una sola línea de texto** (`:63-67`): `{Binding DisplayText}`, que es
`"{Name} @ {X}, {Y} ({ResultType}) {ExtraInfo}"` (`FindSidebarViewModel.cs:19-21`). Sin icono,
sin muestra de color, sin dos líneas.

La barra de navegación (`:28-52`) es un `Grid` de tres columnas `Auto | * | Auto`: botón
**Prev** con icono `ChevronLeft20`, el `ResultSummary` centrado, y botón **Next** con
`ChevronRight20`.

El bloque de opciones (`:86-93`) son **dos casillas en horizontal**: `find_calculate_distance`
y `find_auto_zoom` (esta última con tooltip propio).

Los botones de acción (`:96-116`) son un `Grid` `* | Auto`: **"Search World"** ocupando todo el
ancho con `Appearance="Primary"` e icono `Search20`, y **"Clear"** a su derecha con icono
`Dismiss20`. Es decir, TEdit **no busca mientras escribes**: hay un botón explícito de buscar.

Etiquetas reales, de `src/TEdit/Properties/Language.resx`:

| Clave | Valor real | Línea |
|---|---|---|
| `sidebar_find_title` | `Find & Replace` | `:1799-1800` |
| `find_tab_chests` | `Items in Chests` | `:512-513` |
| `find_search_world` | `Search World` | `:494-495` |
| `find_calculate_distance` | `Calculate Distance` | `:449-450` |
| `find_auto_zoom` | `Auto Zoom` | `:443-444` |
| `find_auto_zoom_tooltip` | `Zoom in when navigating to results (off = pan only)` | `:446-447` |
| `find_result_summary` | `{0} of {1}` | `:473-474` |
| `find_result_summary_limited` | `{0} of {1} shown ({2} total matches)` | `:476-477` |
| `find_no_results` | `No results` | `:461-462` |
| `find_no_selection` | `No selection` | `:464-465` |
| `find_selection_summary` | `Selected: {0}` | `:509-510` |
| `find_distance_label` | `Distance: {0}` | `:455-456` |
| `menu_filter_header_tiles/walls/sprites` | `Tiles` / `Walls` / `Sprites` | `:821-825`, `:818-819` |

(Ojo al título real: la barra se llama **"Find & Replace"** aunque el ViewModel no implemente
ningún reemplazo — el `FindSidebarViewModel` no tiene una sola línea de "replace".)

### 1.3 El picker real: `TileWallPickerControl.xaml` (131 líneas)

Es el control que se repite en tres de las cuatro pestañas. Un `Grid` de dos filas:

**Fila 0 — barra de búsqueda fija arriba** (`:27-53`), `Grid` de `* | Auto | Auto`:
- `ui:TextBox` con `PlaceholderText` y `Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"`
  (`:34-37`) — **filtra mientras escribes**.
- Botón **Check All** (`:39-45`) y botón **Uncheck All** (`:47-52`), ambos visibles solo si
  `ShowCheckboxes`.

**Fila 1 — `ListBox` virtualizado** (`:56-129`) con `VirtualizingPanel.IsVirtualizing="True"`,
`VirtualizationMode="Recycling"`, `ScrollUnit="Pixel"`. Cada fila (`:68-114`) es un `DockPanel`
con, de izquierda a derecha:

1. `CheckBox` (`:69-75`) — multiselección.
2. `Image` de vista previa del ítem, 20x20, `NearestNeighbor` (`:78-85`), oculta por defecto.
3. `Border` con un `Rectangle` de **14x14 relleno del color del tile** (`:88-100`) — la muestra
   de color, que actúa de reserva cuando no hay imagen.
4. Anclado a la **derecha**: el id entre corchetes, en gris (`:103-107`), `StringFormat='[{0}]'`.
5. El nombre, con `TextTrimming="CharacterEllipsis"` (`:110-113`).

El `DataTrigger` de `:116-119` intercambia imagen y muestra de color: si hay textura, se ve la
imagen y se oculta el cuadrito.

El ViewModel (`src/TEdit/ViewModel/Shared/TileWallPickerViewModel.cs`, 171 líneas) confirma dos
detalles de usabilidad que ya señalaba el documento anterior y que merece la pena repetir aquí
porque son de UI:

- El filtro casa **nombre O id como texto** (`:118-124`):
  ```csharp
  return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
      || item.Id.ToString().Contains(SearchText, StringComparison.Ordinal);
  ```
- `CheckAll`/`UncheckAll` actúan **solo sobre lo que el filtro deja visible** (`:129`, `:139`:
  `foreach (var item in AllItems.Where(x => FilterItem(x)))`). Escribir "ore" y pulsar "Check
  All" marca todos los minerales y nada más.

### 1.4 Las cuatro pestañas de Find, y por qué son cuatro

`FindSidebarView.xaml:120-134`:

```xml
<TabControl SelectedIndex="{Binding SelectedTabIndex}" Margin="0">
    <TabItem Header="{x:Static p:Language.find_tab_chests}">
        <controls:TileWallPickerControl DataContext="{Binding ItemPicker}" />
    </TabItem>
    <TabItem Header="{x:Static p:Language.menu_filter_header_tiles}">
        <controls:TileWallPickerControl DataContext="{Binding TilePicker}" />
    </TabItem>
    <TabItem Header="{x:Static p:Language.menu_filter_header_walls}">
        <controls:TileWallPickerControl DataContext="{Binding WallPicker}" />
    </TabItem>
    <TabItem Header="{x:Static p:Language.menu_filter_header_sprites}">
        <controls:SpriteTreePickerControl DataContext="{Binding SpritePicker}" />
    </TabItem>
</TabControl>
```

Las cuatro pestañas son **el mismo control** con cuatro fuentes de datos distintas (salvo
Sprites, que necesita un árbol de dos niveles). Y, crucialmente, **una sola búsqueda combina las
cuatro**: `ExecuteSearch` (`FindSidebarViewModel.cs:176-191`) llama a `SearchContainers` si hay
ítems marcados y a `SearchMap` si hay tiles/paredes/sprites marcados, en la misma pasada. Las
selecciones **persisten al cambiar de pestaña** (comentario literal, `:48`).

El resumen de selección (`:92-103`) concatena por pestaña: `"Selected: 3 items, 2 tiles"`.

### 1.5 `FilterSidebarView.xaml` (154 líneas) tiene la MISMA anatomía

Vale la pena citarlo porque demuestra que el patrón es deliberado y reutilizado, no casual:
`DockPanel` con título arriba (`:23`), bloque de controles abajo (`:26-131`) y `TabControl` de
pickers ocupando el resto (`:134-151`). Sus pestañas son cinco: Tiles, Paredes, **Líquidos**,
**Cables**, Sprites — es decir, `TileWallPickerViewModel` sabe servir también líquidos y cables
(`PickerDataSource.Liquids` / `.Wires`, `TileWallPickerViewModel.cs:14-21`, `:90-102`), aunque
la barra de búsqueda no los use.

Su bloque inferior tiene lo que la de Find no tiene: un `ui:ToggleSwitch` "Hide" (`:34`), dos
`Slider` de 0 a 100 para **oscurecer** y **desaturar** (`:44-51`, `:61-68`), tres `RadioButton`
de modo de fondo (`:77-92`) y los botones Enabled / Clear / Apply (`:118-129`).

### 1.6 `TabbedPickerControl.xaml` (157 líneas): el otro picker, el de Terraria vs Mods

Existe un segundo picker, más compacto, en `src/TEdit/UI/Controls/`. Su `Grid` (`:99-154`) es:
cuadro de búsqueda con icono `Search16` y `ClearButtonEnabled="True"` (`:107-114`), una
`UniformGrid` de **dos `ToggleButton`, "Terraria" y "Mods"** (`:117-129`), y el `ListBox`
virtualizado (`:132-153`) con `GroupStyle` para agrupar por nombre de mod (`:150-152`, estilo
`ModGroupHeaderStyle` de `:28-42`).

Es el precedente más cercano a "sub-botones dentro de una categoría" que hay en TEdit: dos
`ToggleButton` en línea que cambian la fuente de la misma lista. **No** se usa en la barra de
Find; sirve para elegir el tile/pared activo del pincel.

### 1.7 `WorldAnalysis.xaml` (26 líneas): la barra lateral de censo

Es la barra más simple del programa y la más relevante para nuestro problema. Un `DockPanel` con
título, una `UniformGrid` de dos botones — **"Analyze"** (`Appearance="Info"`) y **"Save"**
(`:16-17`) — y un `TextBox` gigante de solo texto (`:20-22`) con `AcceptsReturn` y las dos
barras de scroll.

Lo que ese `TextBox` contiene lo genera `src/TEdit.Editor/WorldAnalysis.cs` (344 líneas). Es el
único sitio de TEdit que calcula **qué hay de verdad en el mundo cargado** (`:67-99`):

```csharp
var tileCounts = new Dictionary<int, int>();
...
for (int x = 0; x < world.TilesWide; x++)
    for (int y = 0; y < world.TilesHigh; y++) {
        var tile = world.Tiles[x, y];
        ...
        if (tile.IsActive) {
            if (tileCounts.ContainsKey(tile.Type)) tileCounts[tile.Type] += 1;
            else tileCounts.Add(tile.Type, 1);
            activeTiles++;
        }
    }
```

y lo vuelca ordenado descendentemente con su porcentaje (`:106-120`):

```csharp
var tiles = tileCounts.OrderByDescending(kvp => kvp.Value);
foreach (var tilePair in tiles) { ... sb.WriteLine("{0}: {1} ({2:P2})", name, tilePair.Value, tilePair.Value / totalTiles); }
```

Luego vuelca cofres con su contenido completo (`:131-155`), letreros (`:156-162`), NPCs
(`:163-169`) y tile entities (`:170-...`). **Es texto plano: ni coordenadas navegables, ni
enlace al mapa, ni filtro.** Un censo, no un localizador — exactamente lo que decía el documento
anterior, ahora confirmado leyendo también su vista.

---

## 2. La pregunta clave: ¿filtra TEdit alguna vez sus candidatos a "lo que hay en el mundo"?

**Respuesta: NO. En ningún sitio.** Es un hecho negativo, comprobado, no una omisión mía.

**Evidencia 1 — los pickers se construyen una sola vez, en el constructor, sin mundo.**
`FindSidebarViewModel.cs:112-120`:

```csharp
public FindSidebarViewModel(WorldViewModel worldViewModel)
{
    _wvm = worldViewModel;

    // Initialize pickers
    ItemPicker = new TileWallPickerViewModel(PickerDataSource.Items);
    TilePicker = new TileWallPickerViewModel(PickerDataSource.TileBricks);
    WallPicker = new TileWallPickerViewModel(PickerDataSource.Walls);
    SpritePicker = new SpriteTreePickerViewModel();
```

**Evidencia 2 — la carga de datos es puramente estática.** `TileWallPickerViewModel.LoadData`
(`:60-103`) solo lee `WorldConfiguration.*`, que son las tablas globales del programa
(`Data/items.json`, `Data/tiles.json`, `Data/walls.json`), nunca `_wvm.CurrentWorld`:

```csharp
case PickerDataSource.TileBricks:
    foreach (var tile in WorldConfiguration.TileBricks.OrderBy(x => x.Name))
    { ... AllItems.Add(new PickerItemViewModel(tile.Id, tile.Name, tile.Color, itemId: tileItemId)); }
```

**Evidencia 3 — `LoadData` se llama exactamente una vez** (`:57`, dentro del constructor), y
`new TileWallPickerViewModel(...)` / `new SpriteTreePickerViewModel()` aparecen solo en **nueve
sitios** de todo el repo, los cinco de `FilterSidebarViewModel.cs:59-63` y los cuatro de
`FindSidebarViewModel.cs:117-120` — todos en constructores. Comprobado con un `grep` sobre el
clon completo.

**Evidencia 4 — no existe ninguna estructura de "qué hay en este mundo" reutilizable.** Busqué
en todo `src/` por `usedTiles|tilesUsed|presentIn|inWorld|existingTile|onlyInWorld|DistinctTile|tileCounts|TileCount`.
Lo único que aparece es: `WorldAnalysis.cs:67` (el `tileCounts` local del censo de 1.7, que se
tira al terminar), los buffers de render (`FilterOverlayBuffer.cs:52`, `PixelMapManager.cs:57`,
donde `tileCount` significa "número de chunks"), y `WorldConfiguration.TileCount`, que es el
tamaño del catálogo, no del mundo.

**Lo más cercano que sí existe, y no sirve para esto**: `SavedOreTiersCopper/Iron/Silver/Gold/
Cobalt/Mythril/Adamantite` (`src/TEdit/Scripting/Api/WorldInfoApi.cs:108-114`), que el `.wld`
guarda de verdad (`src/TEdit.Terraria/World.FileV2.cs:2337-2340` y `:2133-2135`; valen `-1` en
mundos antiguos, `:2344-2347`). Dicen **qué variante de mineral eligió la generación** de ese
mundo concreto (cobre o estaño, hierro o plomo…), que es una pizca de "qué hay en este mundo",
pero solo para siete familias de mineral y sin decir nada de los otros 700 tipos de tile.

**Consecuencia para el diseño**: lo que pide el usuario ("que solo salgan los objetos que tiene
ese mundo") **no tiene precedente en TEdit** y hay que inventarlo. No es un porte: es una
funcionalidad nueva. La buena noticia es que la pieza de cálculo ya existe conceptualmente —
el bucle de censo de `WorldAnalysis.cs:67-99` es literalmente el algoritmo que hace falta,
solo que TEdit lo tira a un `TextBox` en vez de alimentar con él sus pickers.

---

## 3. El resaltado sin tope: qué hace TEdit y cuánto le cuesta

`FindSidebarViewModel.cs:205-207`, comentario literal y su llamada:

```csharp
// Activate find overlay — use ALL matched positions (entire sprites highlighted),
// even though Results list is deduped to anchors only
FilterManager.SetFindResults(_overlayPositions);
```

`_overlayPositions` (`:155`) es una `List<(int X, int Y)>` **sin tope**: mientras
`FindResultAccumulator` corta la lista visible en `MaxDisplayedResults = 1000` (`:46`, `:24-35`),
`_overlayPositions.Add(...)` se ejecuta en `:326`, `:339` y `:365` **antes** de cualquier
comprobación de límite.

`FilterManager.SetFindResults` (`src/TEdit/ViewModel/FilterManager.cs:102-108`) vuelca eso en un
`HashSet<long>` con `PackPosition(x, y) => (long)y * 65536L + x` (`:97`). El comentario de
`:80-83` explica el mecanismo: *"When true, the darken overlay uses find result positions instead
of filter criteria. Found tiles render normally, everything else is darkened/desaturated."*

**El coste real** está en `src/TEdit/Render/FilterOverlayBuffer.cs`. Su cabecera (`:7-12`) es
explícita:

```csharp
/// Single-channel (byte) overlay buffer for the darken filter.
/// Mirrors PixelMapManager's chunk layout but uses byte[][] (1 byte/pixel)
/// instead of Color[][] (4 bytes/pixel), saving 75% memory.
/// Mask values: 0 = clear (no darkening), 255 = darkened.
```

`InitializeBuffers` (`:26-64`) trocea el mundo en chunks de hasta 256x256 buscando el divisor
exacto (`MaxTextureSize = 256`, `:15`; bucles de `:30-47`), y reserva `byte[tileCount][]` con
`tileCount = TilesX * TilesY` (`:52-63`). Cada chunk lleva además un
`ChunkStatus { Mixed, AllClear, AllDarkened }` (`:5`, `:57`) para poder saltárselo entero al
pintar.

**¿Es viable para "marcar todo el mineral de plata"?** En TEdit, sí, y por dos razones que **no**
se cumplen en Terrakeep:

1. La máscara es **1 byte por tile**, no un objeto de UI por posición. Para el mundo Grande real
   que medí (8400x2400, ver sección 6) son 20,2 MB de máscara — mucho, pero lineal y sin coste
   por resultado.
2. TEdit **repinta el mapa** por chunks con un shader que consume esa máscara
   (`FilterManager.Revision`, `:25`, es un contador que sube en cada mutación para que el
   renderer sepa cuándo reconstruir; `DarkenAmount`/`DesaturateAmount`, `:60-74`, están marcados
   *"Shader-only uniform — no rebuild needed"*).

Terrakeep pinta el mundo **una sola vez** a un `WriteableBitmap` que luego congela
(`WorldRenderer.cs:74-76`: `bitmap.WritePixels(...)`, `bitmap.Freeze()`). No hay pipeline al que
enchufar una máscara. Esto ya estaba señalado en `ESPEC-buscador-mundo-tedit.md#3` y sigue
siendo cierto.

---

## 4. Minerales en TEdit: no existe una categoría "mineral" en los datos

Comprobado leyendo el fichero de datos real, `src/TEdit.Terraria/Data/tiles.json` (754 entradas).
Los campos que existen, unión de todas las entradas:

```
id, isSolid, canBlend, textureGrid, frameGap, frameSize, name, key, color, isStone, mergeWith,
isGrass, special, isFramed, frames, isLight, placement, biomeVariants, isAnimated, isSolidTop,
textureWrap, isPlatform, buffRadius, buffName, buffColor, isCactus, saveSlope, largeFrameType
```

**No hay ningún campo de categoría, familia ni `isOre`.** Una entrada real, la del cobre:

```json
{"id":7,"isSolid":true,"canBlend":true,"textureGrid":[16,16],"frameGap":[2,2],
 "frameSize":[[1,1]],"name":"Copper Ore","key":"Copper","color":"#964316FF","mergeWith":0}
```

Cuando TEdit necesita la noción de "mineral", **la escribe a mano en el código**, dos veces:

**(a)** `src/TEdit/Scripting/Api/GenerateApi.cs:39-63`, tabla `OreTypes` (nombre, id de tile,
fuerza, pasos), consumida por `ListOreTypes()` (`GenerateApi.WorldGen.cs:10-19`):

| nombre | id | nombre | id |
|---|---|---|---|
| copper | 7 | cobalt | 107 |
| tin | 166 | palladium | 221 |
| iron | 6 | mythril | 108 |
| lead | 167 | orichalcum | 222 |
| silver | 9 | adamantite | 111 |
| tungsten | 168 | titanium | 223 |
| gold | 8 | chlorophyte | 211 |
| platinum | 169 | luminite | 408 |
| meteorite | 37 | | |
| hellstone | 58 | | |

**(b)** `src/TEdit/Editor/Plugins/SimpleOreGeneratorPlugin.cs:179-240`, `GetSelectedOres()`, la
misma lista más **obsidiana (56)**, **demonita (22)** y **crimtane (204)**, con su comentario de
id al lado de cada `selectedOres.Add(...)`. Ninguna gema.

**Gemas**: sus ids no salen en ninguna lista de TEdit, pero sí en `tiles.json` por nombre:
63 Sapphire, 64 Ruby, 65 Emerald, 66 Topaz, 67 Amethyst, 68 Diamond, 566 Amber Stone Block, y
**178 "Gems"** (las gemas sueltas de pared de cueva, un tile *framed* con 84 variantes de UV).

**Otros objetivos de exploración**, ids reales verificados en el mismo `tiles.json`: 12 Crystal
Heart (corazón de vida), 26 Altars (altar demoníaco/carmesí), 31 Orb Heart (orbe sombrío/corazón
carmesí), 129 Crystal Shard, 236 Life Fruit Plant, 237 Lihzahrd Altar, 238 Plantera's Bulb,
404 Desert Fossil Block, 407 Sturdy Fossil Block, 444 Bee Hive, 21/441/467/468 las cuatro
familias de cofre.

---

## 5. Estado REAL de la pestaña Exploración de Terrakeep (auditoría propia, ficheros leídos enteros)

### 5.1 El núcleo de datos (`TerrasavrNative.Core/WldFormat/`)

| Fichero | Líneas | Qué es |
|---|---|---|
| `WldWorld.cs` | 22 | `Header`, `Tiles[,]`, `Npcs`, `Chests`, `Signs`, `ShimmeredNpcTypes` |
| `WldHeader.cs` | 47 | cabecera parcial + `ZoneFor(y)` |
| `WldTile.cs` | 20 | `struct` de 6 campos: `Type`, `Wall`, `LiquidType`, `LiquidAmount`, `U`, `V` |
| `WldNpc.cs` | 14 | `Id`, `GivenName`, `TileX`, `TileY`, `Homeless`, `VariationIndex` |
| `WldChest.cs` | 20 | `WldChestItem(NetId, Stack, Prefix)` + `WldChest{X,Y,Name,Items}` |
| `WldSign.cs` | 16 | `X`, `Y`, `Text` |
| `WldReader.cs` | 376 | el lector completo |
| `WorldSearch.cs` | 148 | el motor de búsqueda |

Detalles que condicionan el diseño nuevo:

- `WldTile.IsActive => Type >= 0` (`WldTile.cs:17`); `Type` vale `-1` si el tile está vacío
  (`:9`, `:19` `Empty`).
- `WldWorld.Tiles` es `WldTile[,]` indexado `[x, y]` (`WldWorld.cs:9`), igual que `Main.tile`
  del juego.
- `WldHeader` se corta a propósito tras `GroundLevel`/`RockLevel` (`WldHeader.cs:9-12`), así que
  **`SavedOreTiers` no está leído** (ni hace falta, ver 11.1).
- Offsets ya expuestos: `TilesSectionOffset`, `ChestsSectionOffset`, `SignsSectionOffset`,
  `NpcsSectionOffset` (`WldHeader.cs:27-33`).
- `WldReader.Read(byte[], bool readContainers = true)` (`:22`) lee cabecera → tiles → cofres →
  letreros → NPCs, saltando por puntero cada vez (`:29`, `:36`, `:39`, `:43`). Tile entities
  quedan fuera a propósito (`:16-21`).
- `ReadHeader(byte[])` (`:55-60`) es la lectura **barata** que usa el lanzador de mundos.
- `ReadSigns` (`:358-375`) descarta letreros fantasma comprobando el tile real contra
  `SignTileTypes = [55, 85, 425, 573]` (`:356`).
- `ReadChests` (`:319-348`) respeta el umbral real `version < 294` para el `maxItems` global
  (`:325`, `:332`) y **omite los slots vacíos** (`:337-338`), así que `chest.Items` solo contiene
  objetos reales.
- El código de líquido 4 (Centelleo) es **sintético de este puerto**, nunca está en disco
  (`WldReader.cs:241-252`).

### 5.2 `WorldSearch.cs` (148 líneas): el motor actual

- `WorldSearchHit(int X, int Y, string Name, WorldSearchKind Kind)` (`:24`).
- `enum WorldSearchKind { Tile, Wall, Liquid, Npc, ChestItem, Sign }` (`:26`).
- `WorldSearchQuery` (`:28-48`) con `TileTypes`, `WallIds`, `LiquidTypes`, `NpcIds`,
  `ChestItemIds`, `SignTextPredicate` y `DisplayLimit = 1000` (`:44`), más `IsEmpty` (`:46-47`).
- `WorldSearchResult(IReadOnlyList<WorldSearchHit> Hits, int TotalCount)` (`:50`).
- `Run(world, query, tileNames, npcNames, itemNames, ct)` (`:81`): bucle `x→y` sobre toda la
  rejilla **solo si hace falta** (`wantsTileScan`, `:86-87`), luego NPCs (`:106-114`), cofres
  (`:119-128`) y letreros (`:130-138`).
- `Add` (`:143-147`) es el equivalente exacto de `FindResultAccumulator` de TEdit: cuenta todo,
  añade solo hasta el límite.
- `ct.ThrowIfCancellationRequested()` en cada columna (`:92`) — cancelable a media pasada.
- **No deduplica sprites multi-tile**, decisión documentada en `:17-23`.
- `LiquidName(byte)` (`:58-64`) es una copia deliberada de la de la App porque Core no puede
  depender de App (`:56-57`).
- `SignTextPredicate` es un `Func<string,bool>` para no acoplar Core a `LibrarySearchGrammar`,
  que vive en App (`:38-43`).

### 5.3 `ExplorationViewModel.cs` (662 líneas): inventario completo

**Cuatro ViewModels de fila** declarados en el mismo fichero:

| Clase | Líneas | Miembros reales |
|---|---|---|
| `WorldSearchHitRowViewModel` | `:26-53` | `TileX`, `TileY`, `Name`, `Position`, `KindLabel`, `[ObservableProperty] DistanceLabel`, `[ObservableProperty] IsCurrent` |
| `WorldNpcRowViewModel` | `:55-77` | `Id`, `Name`, `TileX`, `TileY`, `Position` (con `" - sin casa"` si `homeless`, `:61`), `IconPath`, `HeadIconPath`, `[ObservableProperty] IsMatch = true` |
| `CharacterSpawnRowViewModel` | `:85-90` | `Label`, `TileX`, `TileY` |
| `MissingNpcRowViewModel` | `:94-98` | `Name`, `IconPath` |

**Estado de la ViewModel principal** (`:104-182`):

- Dependencias inyectadas vía `CharacterFileService`: `_npcNames`, `_mapColors`, `_tileNames`,
  `_itemNames` (`:106-113`, asignadas en `:287-292`).
- `_allNpcs` (`:114`, lista completa sin filtrar) y `_world` (`:115`, el `WldWorld` cargado).
- Propiedades observables: `WorldImage`, `StatusMessage`, `WorldTitle`, `IsWorldLoaded`,
  `NpcSearchText`, `Zoom`, `HoverInfo`, `IsLoading` (`:117-131`).
- Derivadas: `IsNotLoading` (`:132`) e `IsEmpty => !IsWorldLoaded && !IsLoading` (`:144`), con
  sus `OnXChanged` que reemiten (`:133-137`, `:145`).
- Colecciones: `Npcs` (siempre completa, alimenta el mapa, `:149`), `NpcSearchResults` (la lista
  lateral filtrada, `:151`), `MissingNpcs` (`:152`), `CharacterSpawns` (`:156`),
  `WorldSearchResults` (`:167`), `Worlds` (`:278`).
- Buscador general: `WorldSearchText` (`:165`), `WorldSearchSummary` (`:166`),
  `ShowSpawnDistance` (`:175`), `_lastWorldSearchRows` (`:177`), `_worldSearchCurrentIndex`
  (`:178`), `_worldSearchDebounceTimer` de **250 ms** (`:180`), `_worldSearchCts` (`:181`),
  `_worldSearchGeneration` (`:182`).

**Comandos** (todos `[RelayCommand]`): `GoToWorldSearchHit` (`:184-190`),
`NextWorldSearchResult`/`PreviousWorldSearchResult` (`:192-195`), `RefreshWorlds` (`:324-350`),
`OpenFolder` (`:355-366`), `GoToNpc` (`:262-263`), `ZoomIn`/`ZoomOut`/`ZoomReset` (`:526-528`).

**Métodos clave**:

- `MoveWorldSearchResult(int delta)` (`:199-209`): circular con módulo, igual que TEdit.
- `UpdateCurrentWorldSearchHighlight()` (`:211-214`): marca `IsCurrent` en una sola fila.
- `ApplyWorldSearchOrder()` (`:220-242`): **único sitio que toca `WorldSearchResults`**;
  calcula/limpia `DistanceLabel` y reordena por distancia sin repetir el barrido; conserva la
  fila actual a través del reordenado.
- `SetCharacterSpawns(IEnumerable<(string,int,int)>)` (`:252-256`), llamado desde
  `MainViewModel.cs:316` y `:1010`.
- `NavigateToTile(int,int)` (`:269`) → dispara el evento `NavigateToTileRequested` (`:260`).
- `LoadFromPathAsync(string)` (`:443-500`): `IsLoading = true`, `Task.Run` con
  `WldReader.Read` + `WorldRenderer.Render` (`:449-454`), luego en el hilo de UI construye
  `_allNpcs` (`:458-462`), rellena `Npcs`, resetea `NpcSearchText`/`Zoom`/`HoverInfo`, **limpia
  el buscador general** (`:471-475`), calcula `MissingNpcs` contra `VanillaTownNpcRoster.Ids`
  (`:477-481`), pone `WorldTitle`/`IsWorldLoaded`/`StatusMessage` (`:483-486`), y
  `UpdateCurrentWorldPath` (`:487`). El `catch` (`:489-495`) deja `_world = null` y
  `IsWorldLoaded = false`.
- `ApplyNpcFilter()` (`:532-541`): `Contains` sin distinguir mayúsculas sobre `Name`; marca
  `IsMatch` en **todos** y reconstruye `NpcSearchResults` solo con los que casan.
- `UpdateHover(int,int)` (`:395-419`): resuelve nombre por variante de UV
  (`TileVariantName`), líquido, y pared; fuera de rango limpia.
- `BuildWorldSearchQuery(string)` (`:570-612`): **el punto exacto donde hoy se recorre el
  catálogo COMPLETO** — `_tileNames.AllTiles`, `_tileNames.AllWalls`, `_npcNames.All`,
  `LiquidCandidates` (`:567-568`), `_itemNames.AllEntries()` — casando cada entrada con
  `LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)`.
- `RunWorldSearchAsync()` (`:618-661`): cancela la anterior, sube `_worldSearchGeneration`,
  construye la query, y lanza `Task.Run(() => WorldSearch.Run(...))`; descarta el resultado si
  llegó una búsqueda más nueva (`:645`); compone `WorldSearchSummary` con las dos cifras
  (`:650-654`); traga `OperationCanceledException` (`:656-660`).
- `MinZoom = 0.02`, `MaxZoom = 6.0` (`:512`), `ZoomStep = 1.25` (`:524`, constante pública
  compartida con la rueda del ratón).

### 5.4 `MainWindow.xaml`: anatomía real del `TabItem "Exploración"` (líneas **3168-3622**)

Un `DockPanel` con `Margin="14"` (`:3169`) y este reparto:

1. **`Top` — barra de acciones** (`:3170-3200`): botón "Cargar mundo (.wld)…" (`Tag="Accent"`,
   `Click="OnLoadWorldClick"`), el título del mundo, la píldora **"Solo lectura"** (`:3181-3185`),
   y el grupo de zoom (`−`, porcentaje, `+`, "Restablecer", "Ajustar a la ventana").
2. **`Top` — tira de píldoras "Tus mundos"** (`:3207-3220`): `ItemsControl` horizontal con
   `WorldPillTemplate` (definida en `:1181`) y botón "Actualizar"; visible con
   `CountToVis` sobre `Worlds.Count`.
3. **`Bottom` — `StatusMessage`** (`:3222-3223`).
4. **Relleno — un `Grid` de dos columnas** (`:3225-3620`):
   - **Columna 0 (`*`)** = el mapa.
   - **Columna 1 (`Auto`, `MinWidth="220"`, `MaxWidth="300"`)** = el panel lateral (`:3233`).

**Columna 0, el mapa** (`:3236-3453`): un `Border` con un `Grid` que superpone tres cosas:

- Un `DockPanel` con la barra de `HoverInfo` abajo (`:3239-3243`) y el mapa arriba.
- El `ScrollViewer x:Name="WorldMapScroll"` (`:3252-3389`) con **cinco manejadores**:
  `PreviewMouseWheel`, `PreviewMouseLeftButtonDown`, `PreviewMouseMove`,
  `PreviewMouseLeftButtonUp`, `MouseLeave`, y `Cursor="Hand"`.
- Dentro, un `Grid` con `LayoutTransform` = `ScaleTransform` atado a `Exploration.Zoom`
  (`:3259-3262`) que contiene, en este orden de Z:
  1. `Image x:Name="WorldMapImage"` (`:3263-3265`), `Stretch="None"`,
     `BitmapScalingMode="NearestNeighbor"`, `IsHitTestVisible="False"`.
  2. **`ItemsControl` de NPCs** (`:3266-3315`), `ZIndex 1`.
  3. **`ItemsControl` de spawns del personaje** (`:3322-3343`), `ZIndex 2`.
  4. **`ItemsControl` de resultados de búsqueda** (`:3351-3387`), `ZIndex 3`.
- El `Canvas x:Name="MapTooltipCanvas"` con el tooltip flotante (`:3399-3405`).
- Dos overlays centrados: el de `IsLoading` (`:3415-3421`) y el de `IsEmpty` (`:3435-3451`).

**El patrón de marcador, usado ya tres veces** (NPCs `:3266-3315`, spawns `:3322-3343`,
resultados `:3351-3387`) es literalmente el mismo:

```xml
<ItemsControl ItemsSource="{Binding Exploration.XXX}" IsHitTestVisible="True">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate><Canvas IsItemsHost="True" /></ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemContainerStyle>
        <Style TargetType="ContentPresenter">
            <Setter Property="Canvas.Left" Value="{Binding TileX}" />
            <Setter Property="Canvas.Top" Value="{Binding TileY}" />
            <Setter Property="Panel.ZIndex" Value="N" />
        </Style>
    </ItemsControl.ItemContainerStyle>
    <ItemsControl.ItemTemplate> ... </ItemsControl.ItemTemplate>
</ItemsControl>
```

Como el `Canvas` vive dentro del `Grid` escalado, **`Canvas.Left`/`Top` están en coordenadas de
tile** y el marcador se centra con un `Margin` negativo de la mitad de su tamaño (`:3307`
`Margin="-8,-8,0,0"` para una cabeza de 16 px; `:3309` `-5.5` para una elipse de 11; `:3368`
`-4.5` para una de 9). El propio marcador **no** se escala con el zoom (está dentro del
`ScaleTransform`, así que sí crece; es un efecto conocido y aceptado hoy).

**Columna 1, el panel lateral** (`:3455-3619`), un `DockPanel` con, en orden:

1. `Expander "Buscar en el mundo"` (`:3465-3545`), `IsExpanded="False"`, deshabilitado sin
   mundo, con tooltip que explica la gramática (`:3468-3470`). Dentro: `TextBox` del buscador
   (`:3472-3475`), casilla "Ordenar por distancia al spawn" (`:3479-3481`), `Grid` con resumen y
   los botones `‹`/`›` (`:3482-3500`), y un `ScrollViewer MaxHeight="240"` con el `ItemsControl`
   de resultados (`:3501-3543`).
2. `TextBlock "NPCs de pueblo"` (`:3547`).
3. `TextBox` del buscador simple de NPCs (`:3554-3569`), con `Style` que cambia el tooltip según
   `IsWorldLoaded` (`:3559-3568`).
4. `Expander "NPCs que faltan"` anclado a `Bottom` (`:3576-3591`), `IsExpanded="False"`.
5. `ScrollViewer` de relleno con el `ItemsControl` de `NpcSearchResults` (`:3593-3618`).

**La plantilla de fila de resultado** (`:3505-3540`) es un `Button` transparente que envuelve un
`Border` de `CornerRadius="5"`; el borde se pone `TealBrush` cuando `IsCurrent` (`:3518-3520`);
dentro, nombre + píldora con `KindLabel` (`:3527-3529`), posición, y `DistanceLabel` con
`NullToVis`. La fila de NPC (`:3598-3615`) sigue el mismo patrón con icono de 28x28.

**El patrón "fila = `Button` que dispara un comando de la ViewModel"** se repite idéntico en
ambas listas:

```xml
Command="{Binding DataContext.Exploration.GoToNpcCommand, RelativeSource={RelativeSource AncestorType=Window}}"
CommandParameter="{Binding}"
```

### 5.5 `MainWindow.xaml.cs`: los gestos del mapa

| Manejador | Línea | Qué hace |
|---|---|---|
| suscripción a `NavigateToTileRequested` | `:45` | `_viewModel.Exploration.NavigateToTileRequested += OnNavigateToTile;` |
| `RefreshWorldsCommand.Execute(null)` | `:40` | rescaneo tras aplicar carpetas de Ajustes |
| `OnLoadWorldClick` | `:226-237` | diálogo de fichero → `LoadFromPathAsync` |
| (carga desde píldora) | `:247-253` | gemelo del anterior para una tarjeta de mundo |
| `OnFitToWindowClick` | `:362-374` | `UpdateLayout()` y `Zoom = Min(viewport/pixel)` |
| `OnWorldMapPreviewMouseWheel` | `:389-404` | zoom **anclado al cursor**: guarda `worldX/worldY`, aplica `ZoomStep`, `UpdateLayout()`, recoloca los dos offsets |
| `OnWorldMapMouseDown` / `Up` | `:416-427` | inicio/fin de arrastre, con `CaptureMouse` |
| `OnWorldMapMouseMove` | `:435-450` | `UpdateHover((int)pos.X, (int)pos.Y)`, arrastre, y reposicionado del tooltip |
| `PositionMapTooltip` | `:457-475` | coloca el `Border` dentro del `Canvas` evitando salirse |
| `OnWorldMapMouseLeave` | `:478-481` | limpia hover y oculta tooltip |
| `OnNavigateToTile` | `:487-491` | `ScrollTo{Horizontal,Vertical}Offset(tile * zoom - viewport/2)` |

Nota real de `:472-475`: la visibilidad del tooltip se fuerza con
`SetCurrentValue(UIElement.VisibilityProperty, ...)` **porque hay un binding encima** — asignar
la propiedad a secas lo rompería. Es un precedente a respetar si se añaden más overlays.

### 5.6 Catálogos ya disponibles

- **`TileNameCatalog`** (`TerrasavrNative.Core/Data/TileNameCatalog.cs`, 99 líneas):
  `TileName(type)` (`:23`), `WallName(id)` (`:24`), **`AllTiles`/`AllWalls`** (`:30-31`, añadidos
  para el buscador), y `TileVariantName(type, u, v)` con caída al nombre base (`:36-41`). El
  JSON (`TerrasavrNative.App/Assets/tile_names.json`, 790 KB) trae **754 tiles y 367 paredes**
  con nombres en español donde los hay y **frames por UV**. Verificado: `21 "Chests"` tiene 54
  frames (`0,0=Wooden Chest`, `36,0=Cofre de oro`, `108,0=Cofre de las sombras`…),
  `467` tiene 38, `178 "Gems"` tiene 84, `26` tiene 2 (`Altar demoníaco` / `Altar carmesí`).
- **`NpcNameCatalog`** (41 líneas): `GetName(id)` (`:21`) y `All` (`:27`).
- **`VanillaItemCatalog`**: `GetName(itemId)` (`:36`), `AllEntries()` (`:53`).
- **`MapColorCatalog`**, usado por `WorldRenderer` para el color real de cada tile/pared.
- **`VanillaTownNpcRoster.Ids`**, usado para `MissingNpcs`.
- **`NpcIconResolver` / `NpcHeadIconResolver` / `NpcHeadProfile`**, para iconos de lista y de mapa.

### 5.7 Piezas de UI reutilizables que YA existen

**Converters**, todos registrados globalmente en `TerrasavrNative.App/App.xaml:12-19`
(definidos en `TerrasavrNative.App/Converters/VisibilityConverters.cs`):

| Clave XAML | Clase | Comportamiento | Línea |
|---|---|---|---|
| `NullToVis` | `NullToVisibilityConverter` | no-null → Visible | `:37-44` |
| `NullToCollapsed` | `NullToCollapsedConverter` | null → Visible | `:48-55` |
| `EmptyToCollapsed` | `EmptyToCollapsedConverter` | cadena vacía → Collapsed | `:57-64` |
| `EmptyToVisible` | `EmptyToVisibleConverter` | cadena vacía → Visible | `:69-76` |
| `CountToVis` | `CountToVisibilityConverter` | `int > 0` → Visible | `:78-85` |
| `InverseBoolToVis` | `InverseBooleanToVisibilityConverter` | `true` → Collapsed | `:87-94` |
| `BoolToGridLength` | `BoolToGridLengthConverter` | `true` → `*` con peso, `false` → `Auto` | `:14-23` |
| `BoolToDouble` | `BoolToDoubleConverter` | `true` → número del parámetro, `false` → 0 | `:28-35` |

Más `BoolToVis` (el `BooleanToVisibilityConverter` de WPF) y `FractionToWidth`, declarados en
`MainWindow.xaml:15-18`.

**Estilos y recursos** (`TerrasavrNative.App/Styles/Theme.xaml`): `TitleText` (`:142`),
`SectionText` (`:148`), `BodyText` (`:154`), `CaptionText` (`:159`); pinceles `BgSecondaryBrush`
(`:55`), `BgElevatedBrush` (`:56`), `AccentBrush` (`:63`), `CalamityBrush` (`:67`), `TealBrush`
(`:69`); efecto `CardShadow` (`:99`). Los botones usan `Tag="Accent"` o `Tag="Ghost"` como
variante.

**Patrones repetidos que hay que seguir**:
1. **Marcador en el mapa** = `ItemsControl` + `Canvas` + `Canvas.Left/Top` en tiles + `Margin`
   negativo (tres usos, ver 5.4).
2. **Fila clicable** = `Button` transparente que envuelve un `Border`, con
   `Command="{Binding DataContext.Exploration.XxxCommand, RelativeSource={RelativeSource AncestorType=Window}}"`
   y `CommandParameter="{Binding}"`.
3. **Debounce de 250 ms** con `DispatcherTimer` antes de un trabajo caro
   (`ExplorationViewModel.cs:180`, `:301-305`, precedente citado: `AppearanceViewModel`).
4. **Generación + `CancellationTokenSource`** para descartar resultados obsoletos
   (`_scanGeneration` `:322`, `_worldSearchGeneration` `:182`).
5. **`Task.Run` para lo caro, hilo de UI para lo barato**, con `Freeze()` en el bitmap
   (`LoadFromPathAsync:449-454`, `WorldRenderer.cs:75`).

### 5.8 Convenciones de código y de comentario del proyecto

Leyendo `WorldSearch.cs` y `ExplorationViewModel.cs` enteros, el estilo real es muy marcado y
cualquier código nuevo debe imitarlo:

- **Comentarios en español sin tildes** dentro del código C# (`// Punto 4 del feedback del
  usuario...`), pero **con tildes en las cadenas de UI** que ve el usuario.
- Cada bloque no obvio lleva su **procedencia**: el código de auditoría (`X-c`, `H4-08`,
  `H5-11`, `H6-08`), la fecha o la cita literal del usuario, y el documento de referencia
  (`ESPEC-buscador-mundo-tedit.md#5.3`).
- Se documentan explícitamente **las decisiones de NO hacer algo** y por qué
  (`WorldSearch.cs:17-23` sobre no deduplicar sprites; `WldReader.cs:16-21` sobre tile entities).
- Se cita la **fuente real** de cada constante del juego (TEdit, código decompilado, medición
  propia), y se dice cuando algo es "sintético de este puerto".
- `CommunityToolkit.Mvvm`: `[ObservableProperty]` sobre campo `_camelCase`,
  `partial void OnXxxChanged`, `[RelayCommand]` sobre método privado.
- C# moderno: expresiones `switch`, colecciones `[]`, `record struct`, `required init`.

---

## 6. Medición real sobre mundos `.wld` de esta máquina

Para no diseñar a ciegas, **porté el lector de `WldReader.cs` a Node** (mismo algoritmo de
cabecera, RLE, cofres, letreros y NPCs) y censé tres mundos reales de
`Documents\My Games\Terraria\tModLoader\Worlds`. Los scripts quedaron en el scratchpad de la
sesión (`censo-wld.js`, `vetas.js`), no en el repo.

| Mundo | Tamaño | Tiles | **Tipos de tile presentes** | **Paredes presentes** | Cofres | **NetId distintos en cofres** | Letreros | NPCs |
|---|---|---|---|---|---|---|---|---|
| Afueras de Larvas de gusano | 8400x2400 | 20.160.000 | **260** de 754 | **125** de 367 | 560 | **206** | 0 | 2 |
| adriandres | 6400x1800 | 11.520.000 | **200** | **79** | 320 | **411** | 43 | 18 |
| El Musgo de Accidentes | 4200x1200 | 5.040.000 | **205** | **72** | 185 | **239** | 3 | 4 |

Y en el mundo Grande, además: **131 tipos de sprite** (*framed*) distintos, con **4.774
variantes de UV** realmente presentes; y los cuatro líquidos con 797.670 / 427.219 / 2.984 / 693
tiles respectivamente.

**Esto es la justificación numérica de la petición del usuario**: hoy el picker ofrecería
**754 tiles + 367 paredes + ~5.000 objetos + todos los NPCs**; el mundo real solo contiene
**260 + 125 + 206 + 2**. Entre el 65 % y el 96 % de lo que se ofrece hoy no existe en el mundo
cargado.

**Volumen de minerales en el mundo Grande** (los presentes, con su recuento real):

| id | mineral | tiles | id | mineral | tiles |
|---|---|---|---|---|---|
| 58 | Piedra infernal | 86.200 | 63 | Zafiro | 2.850 |
| 7 | Mineral de cobre | 64.289 | 64 | Rubí | 3.020 |
| 6 | Mineral de hierro | 59.640 | 67 | Amatista | 2.038 |
| 9 | Mineral de plata | 40.517 | 68 | Diamante | 1.835 |
| 169 | Mineral de platino | 24.992 | 66 | Topacio | 1.274 |
| 22 | Mineral endemoniado | 2.394 | 65 | Esmeralda | 1.163 |
| 166 | Mineral de estaño | 1.688 | 178 | Gemas sueltas | 1.588 |
| 167 | Mineral de plomo | 1.519 | 204 | Mineral carmesí | 310 |

**Agrupación en vetas** (componentes conexas de 8 vecinos, medido con un flood-fill real):

| tile | tiles | **vetas** | veta mayor | media |
|---|---|---|---|---|
| 7 (cobre) | 64.289 | **4.387** | 102 | 14,7 |
| 9 (plata) | 40.517 | **2.514** | 83 | 16,1 |
| 58 (piedra infernal) | 86.200 | **6.738** | 134 | 12,8 |
| 22 (demonita) | 2.394 | **288** | 28 | 8,3 |
| 63 (zafiro) | 2.850 | **564** | 23 | 5,1 |

**Coste medido**: leer el `.wld` de 11 MB y descomprimir el RLE a una rejilla de 20,16 millones
de celdas tarda **~90 ms** en Node (proceso completo incluido el arranque: 138 ms); cada
flood-fill adicional de un mineral entero, **~40 ms**. Es decir, tanto un censo completo del
mundo como el agrupado en vetas son **baratos** comparados con los ~1,4 s que el propio proyecto
ya mide para leer + pintar un mundo (`WldReader.cs:50-53`, `ExplorationViewModel.cs:124-130`).

*(Advertencia honesta: estas cifras son de un port a JavaScript, no del C# real. Ver sección 17.)*

---

## 7. Resumen de hechos

1. TEdit organiza sus quince barras laterales en una **activity bar de iconos de 48x48**, con
   colapsado al volver a pulsar. Dentro de "Find" hay **cuatro pestañas de texto** con el mismo
   control de picker.
2. `FindSidebarView` reparte así su espacio: el **picker manda** (relleno), los **resultados son
   una lista de 150 px de alto fija**, y hay un **botón explícito "Search World"** — no busca al
   teclear.
3. El picker filtra por **nombre O id**, muestra **muestra de color de 14x14 + nombre + `[id]`**,
   y `Check All`/`Uncheck All` actúan **solo sobre lo filtrado**.
4. **TEdit nunca filtra sus candidatos a lo que existe en el mundo cargado.** Los pickers se
   construyen en el constructor desde `WorldConfiguration.*` y no se recargan jamás. Comprobado
   por cuatro vías distintas.
5. Lo único que censa el mundo real es `WorldAnalysis.cs:67-120`, y su salida es **texto plano
   sin coordenadas navegables**.
6. TEdit resalta **oscureciendo todo lo demás**, con una máscara de **1 byte por tile** en chunks
   de 256x256 y un shader. Ese resaltado **no tiene tope**; el tope de 1000 es solo de la lista.
   **Ese mecanismo no es portable a Terrakeep**, que pinta un `WriteableBitmap` congelado.
7. **No existe una categoría "mineral" en los datos de TEdit.** `tiles.json` no tiene ningún
   campo de familia. TEdit escribe la lista de minerales **a mano en el código**, dos veces.
8. Terrakeep ya tiene: lector completo de tiles/cofres/letreros/NPCs, motor de búsqueda
   cancelable con tope y contador real, navegación circular, distancia al spawn, tres capas de
   marcadores en el mapa, zoom anclado al cursor, tooltip flotante, y catálogos de nombres con
   variantes por UV y traducción al español.
9. Terrakeep resuelve hoy sus candidatos recorriendo el **catálogo completo del juego**
   (`ExplorationViewModel.cs:570-612`), que es justo lo que el usuario pide cambiar.
10. Medido en mundos reales: entre el **65 % y el 96 %** de los candidatos que se ofrecen hoy no
    existen en el mundo cargado.
11. Un mineral común son **decenas de miles de tiles** repartidos en **miles de vetas**. Ni una
    lista ni una capa de marcadores WPF por posición pueden con eso.

---

# PARTE II — PROPUESTA DE DISEÑO

**Todo lo que sigue es criterio mío, no hecho verificado.** Está escrito para que se pueda
implementar directamente, sin volver a investigar ni tomar decisiones de diseño por el camino.

## 8. Decisiones de partida (y por qué)

**D1. La categoría manda, el texto filtra dentro de la categoría.** La petición del usuario
("que solo puedan salir los objetos que tiene ese mundo") no se cumple de verdad con solo
filtrar los resultados: hay que **enseñar la lista de lo que hay**. Un jugador que abre
"Minerales" quiere ver *los doce minerales que este mundo generó, con cuántos hay de cada uno* —
no escribir a ciegas "titanio" y recibir "Sin resultados". Por tanto cada categoría muestra, de
entrada y sin escribir nada, **un inventario real del mundo cargado**.

**D2. Se conserva el buscador de texto libre, como categoría "Todo".** Ya funciona, ya tiene
tests, y es lo único que atraviesa todas las fuentes a la vez (incluido el texto de letreros,
que no tiene catálogo). Convertirlo en una de las cinco categorías cuesta cero y no se pierde
nada. *Justificación contra TEdit*: TEdit no tiene búsqueda por texto libre — su equivalente es
marcar casillas — pero también busca las cuatro pestañas a la vez en una sola pasada
(`ExecuteSearch:176-191`); "Todo" es exactamente ese comportamiento.

**D3. Categorías como fila de píldoras, no como `TabControl` ni como árbol.** Tres opciones
consideradas:
- *`TabControl` horizontal* (lo de TEdit): cinco pestañas de texto no caben en 220-300 px sin
  romper a dos filas feas. TEdit se permite cuatro porque diseña a **400 px**
  (`FindSidebarView.xaml:12`).
- *Un `Expander` por categoría*: el panel ya tiene dos `Expander` ("Buscar en el mundo", "NPCs
  que faltan"); cinco más lo convierten en un acordeón imposible de recorrer, y permite el
  estado absurdo de tener varios abiertos a la vez con listas largas.
- *Fila de píldoras con `RadioButton` estilizados* (**elegida**): ocupa dos líneas de 24 px,
  siempre visible, deja claro que es **una** categoría a la vez, y reutiliza el look de píldora
  que la pestaña ya usa dos veces (la de "Solo lectura" `:3181-3185` y las de "Tus mundos"
  `WorldPillTemplate`). Es además el patrón más cercano a la activity bar de TEdit sin gastar
  una columna entera en iconos.

**D4. La columna lateral crece.** `MinWidth="220" MaxWidth="300"` (`MainWindow.xaml:3233`) se
queda corta para inventario + resultados. Propongo **`MinWidth="260" MaxWidth="380"`**.
Justificación real: TEdit diseña este mismo panel a 400 px.

**D5. Nada de esto escribe en el `.wld`.** No hay ningún camino nuevo hacia el disco. El índice
se calcula en memoria y muere con el mundo cargado.

## 9. Estructura literal de la barra lateral nueva

Sustituye el contenido actual del `DockPanel Grid.Column="1"` (`MainWindow.xaml:3455-3619`).
De arriba abajo:

### 9.0 Cabecera fija (siempre visible)

```
[TextBlock  "Buscar en el mundo"          estilo SectionText]
[TextBlock  "Solo lo que existe en este mundo"   estilo CaptionText, TextSecondaryBrush]
```

La segunda línea es la promesa explícita al usuario. Se muestra solo con `IsWorldLoaded`.

### 9.1 Selector de categoría (fila de píldoras)

Un `WrapPanel` con cinco `RadioButton` (`GroupName="ExploracionCategoria"`) estilizados como
píldora — botón redondeado con `BgElevatedBrush` de fondo y `AccentBrush` cuando está marcado.
En este orden y con estos textos exactos:

| # | Texto | Enlaza a | Estado inicial |
|---|---|---|---|
| 1 | **Todo** | `SelectedCategory = WorldSearchCategory.All` | **marcada por defecto** |
| 2 | **NPCs** | `.Npcs` | |
| 3 | **Cofres** | `.Chests` | |
| 4 | **Minerales** | `.Ores` | |
| 5 | **Objetos** | `.Objects` | |

Cada píldora lleva un contador real entre paréntesis en cuanto hay mundo: `NPCs (18)`,
`Cofres (560)`, `Minerales (16)`, `Objetos (260)`. Ese número sale del índice de la sección 10 y
es, por sí solo, media respuesta a la petición del usuario: se ve de un vistazo qué tiene el
mundo. Con el mundo sin cargar, las cinco píldoras van deshabilitadas
(`IsEnabled="{Binding Exploration.IsWorldLoaded}"`, con `ToolTipService.ShowOnDisabled="True"`
y el tooltip "Carga un mundo primero", igual que ya hace el `TextBox` de NPCs en `:3558-3565`).

**Estilo nuevo a añadir a `Theme.xaml`**: `x:Key="CategoryPill"`, `TargetType="RadioButton"`,
con `ControlTemplate` de `Border` `CornerRadius="99"`, `Padding="10,4"`, `Cursor="Hand"`, y un
`Trigger` sobre `IsChecked` que cambia `Background` a `AccentBrush` y `Foreground` a blanco.

### 9.2 Cuadro de búsqueda común

Un único `TextBox` **debajo** del selector, siempre presente, atado a `WorldSearchText`. Su
`Tag`/placeholder cambia con la categoría (usando el `EmptyToVisible` que ya existe para el
placeholder superpuesto, patrón `H4-01` ya usado en las tres librerías):

| Categoría | Placeholder |
|---|---|
| Todo | `Tile, pared, NPC, objeto de cofre, letrero…` |
| NPCs | `Filtrar los NPCs de este mundo` |
| Cofres | `Filtrar por tipo de cofre o por lo que contiene` |
| Minerales | `Filtrar minerales de este mundo` |
| Objetos | `Filtrar los objetos de este mundo` |

Semántica **distinta según la categoría**, y esto es deliberado:

- En **Todo** el texto lanza el barrido real (comportamiento actual, con su debounce de 250 ms).
- En **NPCs / Cofres / Minerales / Objetos** el texto **filtra el inventario en pantalla** (una
  lista de 260 entradas como mucho), sin tocar el mundo. Filtrado inmediato, sin debounce, sin
  `Task.Run`: es exactamente lo que hace `TileWallPickerViewModel.FilterItem` en TEdit
  (`:118-124`), y por coherencia con él casa **nombre O id**.

El tooltip de la gramática (`:3468-3470`) se conserva, pero solo se muestra en la categoría
"Todo" (es la única donde la gramática aplica de verdad).

### 9.3 Contenido por categoría

En todas, el bloque inferior es el mismo (9.4). Lo que cambia es el bloque superior.

#### A) "Todo" — el buscador actual, intacto

Sin inventario. `TextBox` → barrido → lista de resultados. Cero cambios de comportamiento
respecto a hoy, salvo el filtrado de candidatos de 10.3.

#### B) "NPCs" — sustituye a la lista suelta de hoy

Bloque superior: **tres chips de filtro** en fila (`ToggleButton` con el mismo estilo píldora,
pero multiselección independiente, no `RadioButton`):

```
[ Con casa (14) ]  [ Sin casa (4) ]  [ Bajo tierra (3) ]
```

- **Con casa** = `!Homeless`; **Sin casa** = `Homeless` (el dato ya está en
  `WldNpc.Homeless` y ya se muestra en `WorldNpcRowViewModel.Position`, `:61`).
- **Bajo tierra** = `TileY > _world.Header.GroundLevel`. Es el filtro que resuelve
  literalmente el "npcs escondidos en el subsuelo" del encargo. `GroundLevel` ya está leído
  (`WldHeader.cs:24`) y ya se usa para el fondo por zona (`ZoneFor`, `:39-46`).
- Ningún chip pulsado = todos. Los chips se combinan con OR entre ellos y AND con el texto.

Lista: la de hoy (`NpcSearchResults`, plantilla de `:3598-3615` sin tocar), **más** una línea
extra bajo la posición cuando el NPC está bajo tierra:
`"Bajo tierra (profundidad N)"`, con `N = TileY - (int)GroundLevel`. Y ordenación por
profundidad descendente cuando el chip "Bajo tierra" está activo, para que el más escondido
salga primero.

Debajo, el `Expander "NPCs que faltan"` (`:3576-3591`) **se mueve aquí dentro** sin tocar su
contenido: es información de NPCs y no tiene por qué ocupar sitio en las otras cuatro categorías.

#### C) "Cofres" — dos vistas, alternadas por un par de `ToggleButton`

Precedente real de este gesto: los dos `ToggleButton` "Terraria"/"Mods" de
`TabbedPickerControl.xaml:117-129`.

```
[ Por tipo de cofre ]   [ Por lo que contienen ]
```

- **Por tipo de cofre** (por defecto): lista de las variantes reales presentes, con recuento:
  ```
  Cofre de madera            312
  Cofre de oro                48
  Cofre de la jungla          12
  Cofre de las sombras         6
  Cofre atrapado              21
  ```
  La variante sale de `TileVariantName(tile.Type, tile.U, tile.V)` leyendo el tile en
  `(chest.X, chest.Y)`, que es el ancla del sprite. Verificado que el catálogo lo resuelve
  (5.6). Un cofre cuya casilla no sea un tile de cofre conocido cae en `TileName` y, si tampoco,
  en `"Tile #N"` — nunca se inventa.
  Pulsar una fila busca **todos los cofres de esa variante** y los vuelca como resultados.
- **Por lo que contienen**: lista de los **NetId realmente presentes** en algún cofre, con el
  número de slots (206 entradas en el mundo Grande medido, 411 en el mediano). Nombre resuelto
  con `VanillaItemCatalog.GetName`; un NetId de Calamity no reconocido se muestra como
  `Item #N` (mismo criterio ya establecido en `WorldSearch.cs:78-80`). Pulsar una fila busca
  los cofres que lo contienen.

Ambas vistas admiten multiselección con `CheckBox` (patrón de TEdit) y un botón
**"Buscar seleccionados"**; pero **un clic simple sobre una fila busca solo esa** — el caso
común no debe costar dos gestos.

#### D) "Minerales" — inventario + marcado (ver sección 11 para el detalle)

Bloque superior: lista de los minerales y gemas **presentes**, con dos cifras reales por fila:

```
[x]  Piedra infernal      86.200 tiles · 6.738 vetas
[ ]  Mineral de cobre     64.289 tiles · 4.387 vetas
[ ]  Mineral de plata     40.517 tiles · 2.514 vetas
[ ]  Zafiro                2.850 tiles ·   564 vetas
```

Con muestra de color a la izquierda (14x14, tomada de `MapColorCatalog.TileColor(id)` — el
mismo color con el que el mapa ya lo pinta; equivalente exacto del `Rectangle` de 14x14 de
`TileWallPickerControl.xaml:94-99`).

Debajo, dos botones:

```
[ Marcar en el mapa ]        [ Quitar marcas ]
```

**"Marcar en el mapa"** activa la capa de resaltado (11.3) — sin tope, todas las posiciones.
Además rellena la lista inferior con **las vetas**, no con los tiles.

#### E) "Objetos" — el inventario general del mundo

Bloque superior: tres `ToggleButton` de fuente, en la línea de C):

```
[ Tiles ]  [ Paredes ]  [ Líquidos ]
```

y debajo la lista de **lo presente** con recuento y muestra de color, ordenada por recuento
descendente por defecto (igual que `WorldAnalysis.cs:106`), con un conmutador de orden
alfabético. En Tiles, los tiles *framed* con variantes reales presentes se muestran como
**dos niveles**: la fila del tile (`Cofres · 378`) y, plegado, sus variantes de UV realmente
presentes (`Cofre de oro · 48`). Es el equivalente honesto del `SpriteTreePickerControl` de
TEdit, pero podado a lo que existe: en el mundo Grande son 131 sprites con 4.774 variantes, que
plegado es perfectamente manejable.

**Letreros** no necesitan lista propia (0 y 3 y 43 en los mundos medidos): se buscan por texto
desde "Todo".

### 9.4 Bloque inferior común: resultados

Idéntico para las cinco categorías y **calcado del que ya existe** (`:3479-3543`), solo movido:

```
[ ] Ordenar por distancia al spawn
[ resumen ..................... ]  [ ‹ ]  [ › ]
[ ScrollViewer MaxHeight=240 con las filas de resultado ]
```

Una única diferencia: el `MaxHeight="240"` fijo pasa a `*` dentro de un `Grid` de dos filas
(inventario `Auto`/`*`, resultados `*`), para que el reparto respire con la ventana. Es el mismo
razonamiento que llevó a que TEdit fije la lista a 150 px y deje el picker en relleno
(`FindSidebarView.xaml:25-75`), pero al revés: aquí el inventario suele ser corto (5-30 filas
tras filtrar) y los resultados largos.

## 10. El índice real de "qué existe en este mundo"

### 10.1 Dónde vive: `TerrasavrNative.Core/WldFormat/WorldPresenceIndex.cs` (fichero nuevo)

En Core, junto a `WorldSearch.cs`, porque es lógica pura sobre `WldWorld` sin nada de UI —
exactamente el mismo criterio que ya se aplicó a `WorldSearch`. **No** un campo de `WldWorld`:
`WldWorld` es el resultado literal de leer el fichero, y meterle un índice derivado mezclaría
"lo que dice el disco" con "lo que hemos calculado". **No** un método más de `WorldSearch`:
`WorldSearch` es sin estado y se ejecuta por consulta; el índice se calcula una vez por mundo.

### 10.2 Forma exacta

```csharp
// Que hay DE VERDAD en este mundo concreto - censo por tipo, calculado una sola vez al cargar.
//
// Peticion literal del usuario (4-sep-2026): "que entre todas las opciones solo puedan salir los
// objetos que tiene ese mundo, los que no ha habido suerte que en ese mundo se generen que no
// salgan en la busqueda". TEdit NO hace esto en ningun sitio (sus pickers salen siempre del
// catalogo completo del juego, WorldConfiguration.*, cargado en el constructor y nunca
// recargado - ver ESPEC-ui-exploracion.md#2): lo unico parecido que tiene es el censo en TEXTO
// de WorldAnalysis.cs:67-120, que se tira nada mas escribirlo. Esto es ese mismo censo, pero
// vivo y consultable.
//
// Medido en mundos reales de esta maquina (ESPEC-ui-exploracion.md#6): un mundo Grande de
// 8400x2400 contiene 260 tipos de tile de los 754 del catalogo, 125 paredes de 367 y 206 NetId
// distintos dentro de sus cofres - entre el 65% y el 96% de lo que hoy se ofrece como candidato
// no existe en el mundo cargado.
public sealed class WorldPresenceIndex
{
    // Recuento real de tiles por tipo (solo IsActive). La clave es el Type del tile.
    public required IReadOnlyDictionary<int, int> TileCounts { get; init; }
    // Recuento por id de pared (Wall != 0).
    public required IReadOnlyDictionary<int, int> WallCounts { get; init; }
    // Recuento por codigo de liquido (1=Agua, 2=Lava, 3=Miel, 4=Centelleo sintetico).
    public required IReadOnlyDictionary<byte, int> LiquidCounts { get; init; }
    // Variantes de sprite REALMENTE presentes, por (tipo, u, v) - solo para tiles enmarcados.
    // 4774 entradas en el mundo Grande medido: cabe de sobra en memoria y es lo que permite
    // ofrecer "Cofre de oro" en vez de "Cofres" a secas.
    public required IReadOnlyDictionary<(int Type, short U, short V), int> SpriteVariantCounts { get; init; }
    // Instancias reales por tipo de NPC.
    public required IReadOnlyDictionary<int, int> NpcCounts { get; init; }
    // NetId -> numero de slots ocupados por ese objeto en algun cofre real del mundo.
    public required IReadOnlyDictionary<int, int> ChestItemCounts { get; init; }
    // Cofres agrupados por la variante real de su casilla (Type,U,V del tile en chest.X/Y) -
    // lo que permite listar "Cofre de oro: 48" sin volver a recorrer nada.
    public required IReadOnlyDictionary<(int Type, short U, short V), int> ChestKindCounts { get; init; }
    public required int SignCount { get; init; }

    public bool HasTile(int type) => TileCounts.ContainsKey(type);
    public bool HasWall(int id) => WallCounts.ContainsKey(id);
    public bool HasNpc(int id) => NpcCounts.ContainsKey(id);
    public bool HasChestItem(int netId) => ChestItemCounts.ContainsKey(netId);

    public static WorldPresenceIndex Build(WldWorld world, CancellationToken ct = default) { ... }
}
```

`Build` hace **una sola pasada** `x→y` (mismo orden que el RLE, igual que `WorldSearch.Run:90-93`)
acumulando `TileCounts`, `WallCounts`, `LiquidCounts` y `SpriteVariantCounts`; luego tres bucles
cortos sobre `world.Npcs`, `world.Chests` (contenido) y `world.Chests` (variante de casilla), y
`SignCount = world.Signs.Count`. `ct.ThrowIfCancellationRequested()` una vez por columna.

Para saber si un tile es *framed* y por tanto merece entrada en `SpriteVariantCounts`, se usa
`world.Header.TileFrameImportant[type]`, que ya está leído (`WldHeader.cs:17`, poblado en
`WldReader.cs:83`) y es exactamente el criterio que usa el propio lector (`WldReader.cs:219`).

### 10.3 Dónde se calcula

**Dentro del `Task.Run` que ya existe** en `ExplorationViewModel.LoadFromPathAsync:449-454`:

```csharp
var (world, image, presence) = await Task.Run(() =>
{
    var w = WldReader.Read(File.ReadAllBytes(wldPath));
    var img = WorldRenderer.Render(w, _mapColors);
    var idx = WorldPresenceIndex.Build(w);
    return (w, img, idx);
});
```

Razones: (a) el mundo ya está en memoria y caliente en caché justo ahí; (b) el overlay de
"Leyendo y pintando el mapa…" ya está en pantalla, así que el usuario no ve ningún hueco nuevo;
(c) el sobrecoste medido es de **decenas de milisegundos** frente a los ~1,4 s que ya cuesta el
paso (sección 6). No hace falta ni un segundo `Task.Run`, ni una barra de progreso propia, ni
diferirlo a la primera vez que se abra una categoría (eso solo movería el freeze a un momento
peor, con el mapa ya visible).

Se guarda en un campo nuevo `private WorldPresenceIndex? _presence;` junto a `_world` (`:115`),
y se pone a `null` en el `catch` de `:489-495`, exactamente igual que `_world`.

### 10.4 Cómo cambia `BuildWorldSearchQuery`

En `ExplorationViewModel.cs:570-612`, cada uno de los cinco bucles gana una condición de
presencia. Ejemplo del primero:

```csharp
var tileTypes = new HashSet<int>();
foreach (var (id, name) in _tileNames.AllTiles)
{
    // Peticion del usuario: un tile que este mundo no genero NUNCA es candidato - antes se
    // ofrecian los 754 del catalogo aunque el mundo solo tuviera 260 (medicion real, ver
    // ESPEC-ui-exploracion.md#6).
    if (_presence != null && !_presence.HasTile(id)) continue;
    if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) tileTypes.Add(id);
}
```

Igual para paredes (`HasWall`), NPCs (`HasNpc`), objetos de cofre (`HasChestItem`) y líquidos
(`_presence.LiquidCounts.ContainsKey((byte)id)`).

**Efecto secundario bueno y medible**: el barrido se salta por completo el bucle de tiles cuando
la query queda vacía (`IsEmpty`, `WorldSearch.cs:46`), así que buscar algo que no existe pasa de
recorrer 20 millones de celdas a no recorrer ninguna.

**Efecto secundario a documentar en el código**: un NetId de Calamity real guardado en un cofre
sigue sin estar en `VanillaItemCatalog`, así que no será candidato por nombre — pero **sí
aparecerá en el inventario de la categoría "Cofres"**, porque ese inventario sale del índice
(del `.wld` real), no del catálogo. Es, de hecho, la primera vez que este proyecto puede enseñar
objetos de Calamity encontrados en el mundo.

### 10.5 Qué se enseña, exactamente, en cada inventario

| Categoría | Fuente exacta | Nombre mostrado | Recuento |
|---|---|---|---|
| NPCs | `world.Npcs` (ya en `_allNpcs`) | `NpcNameCatalog.GetName(id)` | — (es una instancia por fila) |
| Cofres / por tipo | `ChestKindCounts` | `TileVariantName(type, u, v)` | nº de cofres |
| Cofres / contenido | `ChestItemCounts` | `VanillaItemCatalog.GetName(netId)` | nº de slots |
| Minerales | `TileCounts` ∩ `OreTileCatalog.All` | `TileNameCatalog.TileName(id)` | tiles y vetas |
| Objetos / Tiles | `TileCounts` (+ `SpriteVariantCounts` como hijos) | `TileName` / `TileVariantName` | tiles |
| Objetos / Paredes | `WallCounts` | `WallName(id)` | tiles |
| Objetos / Líquidos | `LiquidCounts` | `WorldSearch.LiquidName(code)` | tiles |

## 11. Minerales, en detalle

### 11.1 Cómo se decide qué es un mineral: `TerrasavrNative.Core/Data/OreTileCatalog.cs` (nuevo)

**Hecho establecido en 4**: no existe ninguna categoría de mineral en los datos — ni en
`tiles.json` de TEdit, ni en el `tile_names.json` de este proyecto (que es un derivado suyo). La
lista hay que escribirla a mano, igual que hace TEdit dos veces. Es una tabla fija de 28 ids
reales del juego, no un dato inventado:

```csharp
// No existe ninguna categoria "mineral" en los datos: ni tile_names.json de este proyecto ni su
// fuente real (Data/tiles.json de TEdit, 754 entradas, campos comprobados uno a uno) tienen
// ningun campo de familia. El propio TEdit la escribe A MANO dos veces: GenerateApi.cs:39-63
// (tabla OreTypes) y SimpleOreGeneratorPlugin.cs:179-240 (GetSelectedOres, que ademas añade
// obsidiana/demonita/crimtane). Esta tabla es la union de las dos, mas las gemas (que TEdit no
// lista en ningun sitio; sus ids salen por nombre de Data/tiles.json real).
public static class OreTileCatalog
{
    // Minerales de veta - GenerateApi.cs:41-63 + SimpleOreGeneratorPlugin.cs:187-240.
    public static readonly IReadOnlyList<int> Metals =
    [
        7, 166,    // cobre / estaño
        6, 167,    // hierro / plomo
        9, 168,    // plata / tungsteno
        8, 169,    // oro / platino
        37,        // meteorito
        58,        // piedra infernal
        22, 204,   // demonita / crimtane
        56,        // obsidiana (solo en SimpleOreGeneratorPlugin, no en GenerateApi)
        107, 221,  // cobalto / paladio
        108, 222,  // mithril / oricalco
        111, 223,  // adamantita / titanio
        211,       // clorofita
        408,       // luminita
    ];

    // Gemas - ids reales de Data/tiles.json de TEdit por nombre (TEdit no tiene lista de gemas).
    // 178 "Gems" son las gemas SUELTAS de pared de cueva (tile enmarcado, 84 variantes de UV);
    // las otras seis son el bloque de piedra con la gema dentro.
    public static readonly IReadOnlyList<int> Gems = [63, 64, 65, 66, 67, 68, 566, 178];

    // Otros objetivos reales de exploracion subterranea - no son mineral, pero es exactamente lo
    // que un jugador busca con la misma intencion ("¿donde hay un corazon de vida?"). Ids
    // verificados uno a uno en Data/tiles.json de TEdit.
    public static readonly IReadOnlyList<int> Targets =
    [
        12,   // Corazon de vida (Crystal Heart)
        26,   // Altar demoniaco / carmesi
        31,   // Orbe sombrio / corazon carmesi
        129,  // Fragmento de cristal
        236,  // Fruta de la vida
        237,  // Altar lihzahrd
        238,  // Bulbo de Plantera
        404, 407, // Fosil del desierto / fosil resistente
        444,  // Colmena
    ];

    public static readonly IReadOnlySet<int> All = new HashSet<int>([..Metals, ..Gems, ..Targets]);
}
```

**No se usa `SavedOreTiers`** aunque el `.wld` lo guarde (hecho de 2): el índice real ya sabe qué
minerales hay porque los ha contado, lo cual es estrictamente más fiel (un mundo puede tener
mineral traído de otro sitio y colocado a mano, y `SavedOreTiers` no lo sabría). Además obligaría
a ampliar `WldHeader`, que está cortado a propósito.

La categoría "Minerales" muestra tres grupos con cabecera, en este orden: **Minerales**, **Gemas**,
**Otros objetivos** — y dentro de cada uno, solo los que `_presence.HasTile(id)`. En el mundo
Grande medido eso son 16 filas de las 34 posibles.

### 11.2 El problema real del volumen, y cómo se resuelve

**El dato duro** (sección 6): la piedra infernal son **86.200 tiles**; el cobre, **64.289**.
Ni la lista de resultados (tope 1000) ni la capa de marcadores WPF (una `Ellipse` por
posición → 86.200 elementos visuales) sirven. Agrupar en vetas lo reduce ×15, pero **4.387
vetas de cobre siguen siendo demasiadas** para un `ItemsControl` cuyo mayor uso hasta hoy son
18 NPCs.

Solución en **dos piezas independientes**, que es exactamente el reparto que hace TEdit
(resaltado sin tope + lista topada, `FindSidebarViewModel.cs:205-207`), adaptado a que aquí el
mapa es un bitmap congelado:

### 11.3 Pieza 1 — capa de resaltado como segundo `WriteableBitmap`

**`TerrasavrNative.App/Services/WorldHighlightRenderer.cs`** (fichero nuevo, hermano de
`WorldRenderer.cs`):

```csharp
// Capa de resaltado: un WriteableBitmap del MISMO tamaño que el mapa (1 pixel por tile),
// transparente salvo en los tiles que casan. Se dibuja como una segunda <Image> justo encima de
// WorldMapImage, dentro del mismo Grid escalado - asi hereda el zoom y el desplazamiento sin
// una sola linea de codigo extra.
//
// Por que no se puede portar el mecanismo real de TEdit: TEdit oscurece TODO menos lo
// encontrado, con una mascara de 1 byte por tile (FilterOverlayBuffer.cs:7-12) que consume un
// shader al repintar el mapa por chunks. Terrakeep pinta el mundo UNA vez y congela el bitmap
// (WorldRenderer.cs:74-76): no hay pipeline al que enchufar una mascara. Marcar en positivo
// sobre una capa aparte da el mismo resultado util (ver de un vistazo donde esta el mineral)
// sin tocar el renderer.
public static WriteableBitmap Render(WldWorld world, IReadOnlySet<int> tileTypes, Color color, CancellationToken ct)
```

Detalles de implementación:

- Formato `PixelFormats.Bgra32`, igual que `WorldRenderer` (`:29`) — probado y funcionando en
  este proyecto. Píxel `(0,0,0,0)` donde no hay coincidencia.
- **Halo de 1 tile**: además del tile que casa, se pinta su vecindad de 8 con el mismo color a
  media opacidad. Sin esto, a `Zoom = 0.1` (el necesario para ver un mundo Grande entero) una
  veta de 15 tiles ocupa 1,5 px y es invisible. Con halo ocupa ~3,5 px y se ve. El halo se pinta
  **primero**, para que el núcleo quede por encima.
- `Freeze()` al final, igual que `WorldRenderer.cs:75`, para poder generarlo en `Task.Run`.
- Cancelable con el mismo patrón de generación de `RunWorldSearchAsync`.

**Coste de memoria, dicho claro**: un `Bgra32` de 8400x2400 son **80,6 MB**, los mismos que ya
ocupa el mapa. Es el precio de esta decisión y hay que aceptarlo conscientemente. Mitigaciones,
en orden de preferencia:
1. **Una sola capa a la vez** (se regenera al cambiar de selección; no se acumulan capas).
2. **Liberarla al desmarcar** (`WorldHighlight = null` en "Quitar marcas" y al cargar otro
   mundo).
3. Si algún día molesta de verdad: `PixelFormats.Indexed1` con una `BitmapPalette` de dos
   colores (transparente + el del mineral) baja a **2,5 MB**. *No lo he verificado* — ver
   sección 17.

**En el XAML**, una sola `Image` nueva dentro del `Grid` escalado, entre el mapa y los
marcadores:

```xml
<Image Source="{Binding Exploration.WorldHighlight}" Stretch="None"
       RenderOptions.BitmapScalingMode="NearestNeighbor"
       HorizontalAlignment="Left" VerticalAlignment="Top" IsHitTestVisible="False" />
```

(entre `:3265` y `:3266`; el `ZIndex` por defecto la deja bajo los tres `ItemsControl` de
marcadores, que es lo correcto.)

### 11.4 Pieza 2 — la lista, agrupada por vetas

**`TerrasavrNative.Core/WldFormat/OreVeinFinder.cs`** (fichero nuevo):

```csharp
// Una veta = un grupo conexo (8 vecinos) de tiles del mismo tipo. Medido sobre un mundo real de
// 8400x2400 (ESPEC-ui-exploracion.md#6): el cobre son 64.289 tiles pero solo 4.387 vetas (media
// de 14,7 tiles, la mayor de 102); el zafiro, 2.850 tiles en 564 vetas. Agrupar divide la lista
// entre 15 y la vuelve util - "ir a la siguiente veta" es lo que un jugador quiere, no "ir al
// siguiente tile de cobre", que estaria pegado al anterior.
public readonly record struct OreVein(int CenterX, int CenterY, int TileCount, int Type);

public static class OreVeinFinder
{
    public static IReadOnlyList<OreVein> Find(WldWorld world, IReadOnlySet<int> tileTypes,
        int limit, out int totalVeins, CancellationToken ct = default);
}
```

- Flood-fill iterativo con pila explícita (**nunca recursivo**: la veta mayor medida es de 134
  tiles, pero un mundo trucado podría tener una losa entera del mismo tipo y reventar la pila).
- Marca de visitados: un `bool[]` plano de `TilesWide * TilesHigh` (20,2 MB en el mundo Grande,
  temporal, se libera al salir). Alternativa si molesta: `System.Collections.BitArray`, 2,5 MB.
- `CenterX/CenterY` = redondeo del centroide, que es donde debe apuntar la navegación.
- Devuelve las `limit` primeras vetas **ordenadas por tamaño descendente** (las vetas gordas
  primero: son las que interesan), y el total real por `out`. Mismo patrón "cuenta todo, muestra
  N" de `WorldSearch.Add:143-147`.
- Coste medido: ~40 ms por mineral (sección 6).

Las vetas se vuelcan a `WorldSearchResults` como `WorldSearchHit` normales, con un
`WorldSearchKind.OreVein` nuevo y `Name = $"{nombre} ({veta.TileCount} tiles)"`. Así **heredan
gratis** la navegación circular, el resaltado del actual, la ordenación por distancia al spawn y
el marcador en el mapa, sin duplicar nada.

**Tope de la lista de vetas**: 1000, el mismo que el resto (`WorldSearchQuery.DisplayLimit`,
`:44`). El resumen dice la verdad completa: `"1000 de 4.387 vetas (64.289 tiles) — el mapa las
marca todas"`. Esa última coletilla es importante: le dice al usuario que el tope es solo de la
lista, no del marcado. Es la traducción literal del comentario de `FindSidebarViewModel.cs:205-206`.

## 12. NPCs "escondidos en el subsuelo": qué cambia de verdad

Hoy ya funciona lo básico: todo NPC del `.wld` tiene coordenada real y sale en la lista y en el
mapa. Lo que falta, y es lo que el encargo pide, es que sea **fácil** encontrarlos. Cuatro
cambios concretos, todos baratos:

1. **El chip "Bajo tierra"** (`TileY > GroundLevel`, 9.3-B). Es la respuesta directa: un clic y
   la lista se queda solo con los que están enterrados. Hoy no hay forma de saberlo sin mirar
   las coordenadas de los 18 uno a uno.
2. **La profundidad, escrita**: `"Bajo tierra (profundidad 412)"` bajo la posición. Un `(3200,
   1450)` no le dice nada a nadie; "412 tiles bajo el nivel del suelo" sí.
3. **Ordenación por profundidad** cuando el chip está activo — el más escondido, arriba.
4. **Distinguir "sin casa"**: ya se muestra en el texto (`:61`) pero no se puede filtrar. Un NPC
   `Homeless` en el subsuelo es exactamente el caso de "se me perdió". Con los chips 1 y 4
   combinados sale en dos clics.

**Lo que ya está bien y no toco**: `Npcs` sigue completo siempre y el filtro solo marca
`IsMatch` (atenúa en el mapa, no oculta) — esa decisión (`X-c`, `:68-76` y
`MainWindow.xaml:3274-3287`) es correcta y los chips nuevos deben respetarla exactamente igual.

## 13. Qué se conserva tal cual

Confirmado explícitamente, para las cinco categorías nuevas:

| Pieza | ¿Se conserva? | Nota |
|---|---|---|
| Navegación circular `‹`/`›` con módulo (`:199-209`) | **Sí, tal cual** | Sirve igual para vetas, cofres y NPCs |
| Distancia al spawn opcional (`ShowSpawnDistance`, `ApplyWorldSearchOrder:220-242`) | **Sí, tal cual** | Apagada por defecto, como TEdit |
| Resaltado del resultado actual (`IsCurrent`, fila + marcador) | **Sí, tal cual** | |
| Marcadores = `ItemsControl` + `Canvas` (`:3351-3387`) | **Sí, tal cual** | Sigue siendo el mecanismo de **todos** los resultados, incluidas las vetas |
| **Nunca un overlay tipo shader/máscara sobre el mapa base** | **Se mantiene descartado** | El mapa es un `WriteableBitmap` congelado (`WorldRenderer.cs:74-76`) |
| `NavigateToTile` → `OnNavigateToTile` (pan, sin zoom) | **Sí, tal cual** | Coincide con el `AutoZoomOnNavigate = false` de TEdit |
| Debounce de 250 ms | **Solo en "Todo"** | En las otras cuatro el texto filtra una lista en memoria: inmediato |
| Solo lectura, píldora incluida (`:3181-3185`) | **Sí** | |

**La única excepción**, y es la del volumen: la categoría **Minerales** añade la capa
`WriteableBitmap` de 11.3 **además** de los marcadores, porque 86.200 posiciones no caben en un
`ItemsControl`. Los marcadores siguen ahí, pero marcan **vetas** (≤1000), no tiles. Esto no
contradice la decisión anterior: lo descartado era sustituir el mapa base por uno oscurecido,
no añadirle una capa transparente encima.

## 14. Cambios concretos por fichero

### 14.1 Ficheros nuevos

| Fichero | Qué contiene |
|---|---|
| `TerrasavrNative.Core/WldFormat/WorldPresenceIndex.cs` | El índice de 10.2 + `Build` |
| `TerrasavrNative.Core/WldFormat/OreVeinFinder.cs` | `OreVein` + `Find` (11.4) |
| `TerrasavrNative.Core/Data/OreTileCatalog.cs` | Las tres tablas de ids de 11.1 |
| `TerrasavrNative.App/Services/WorldHighlightRenderer.cs` | La capa de resaltado de 11.3 |
| `TerrasavrNative.Core.Tests/WldFormat/WorldPresenceIndexTests.cs` | Mundo sintético 5x5, mismo patrón que `WorldSearchTests.cs:15-38` |
| `TerrasavrNative.Core.Tests/WldFormat/OreVeinFinderTests.cs` | Dos vetas separadas, una en diagonal (comprueba los 8 vecinos), tope y total |

### 14.2 `TerrasavrNative.Core/WldFormat/WorldSearch.cs`

1. `enum WorldSearchKind` (`:26`): añadir **`OreVein`** al final (no en medio: el orden se
   refleja en `KindLabel`).
2. `WorldSearchQuery` (`:28-48`): añadir
   `public IReadOnlyDictionary<(int Type, short U, short V), bool>? SpriteVariants { get; init; }`
   para poder buscar "Cofre de oro" y no "cualquier cofre". En `Run`, dentro del bucle de tiles,
   comprobar la variante con `(tile.Type, tile.U, tile.V)` — es el equivalente al
   `uv.HasValue → tile.GetUV()` de TEdit (`FindSidebarViewModel.cs:356-362`), y aquí sale gratis
   porque `WldTile` ya guarda `U`/`V` (`WldTile.cs:14-15`).
   Actualizar `IsEmpty` (`:46-47`) en consecuencia.
3. Nada más. `Run` no se toca en su estructura: sigue siendo el mismo bucle.

### 14.3 `TerrasavrNative.App/ViewModels/ExplorationViewModel.cs`

1. **`enum WorldSearchCategory { All, Npcs, Chests, Ores, Objects }`** — al lado de las clases de
   fila, arriba del fichero.
2. **Tres ViewModels de fila nuevos**, en el mismo fichero y con el mismo estilo que los cuatro
   que ya hay:
   - `WorldInventoryRowViewModel` — una fila de inventario: `Id`, `Uv` (nullable), `Name`,
     `Count`, `SecondaryCount` (nullable, para las vetas), `SwatchColor`,
     `[ObservableProperty] IsChecked`, `[ObservableProperty] IsMatch`.
   - `WorldChestKindRowViewModel` — si la fila de cofre necesita algo más que la anterior;
     si no, reutilizar `WorldInventoryRowViewModel` y **no crear la clase**.
   - `OreVeinRowViewModel` — **no hace falta**: las vetas se vuelcan como
     `WorldSearchHitRowViewModel` (11.4).
3. **Campos nuevos**: `_presence` (10.3), `[ObservableProperty] WorldSearchCategory _selectedCategory`,
   `[ObservableProperty] BitmapSource? _worldHighlight`, `[ObservableProperty] bool _npcFilterWithHome`,
   `_npcFilterHomeless`, `_npcFilterUnderground`, `[ObservableProperty] int _chestViewMode`,
   `[ObservableProperty] int _objectsViewMode`.
4. **Colecciones nuevas**: `Inventory` (`ObservableCollection<WorldInventoryRowViewModel>`, la
   lista de la categoría activa) y `InventoryGroupHeader` (string, para "Minerales"/"Gemas"/…).
5. **`partial void OnSelectedCategoryChanged`** → `RebuildInventory()` + limpiar el cuadro de
   texto y los resultados (cambiar de categoría es empezar de cero, igual que cargar otro mundo
   los limpia hoy, `:471-475`).
6. **`RebuildInventory()`** nuevo: construye `Inventory` desde `_presence` según
   `SelectedCategory` (tabla de 10.5). Barato — como mucho 260 filas.
7. **`ApplyInventoryFilter()`** nuevo: marca `IsMatch` por nombre **o** id, gemelo exacto de
   `ApplyNpcFilter:532-541` y del `FilterItem` de TEdit.
8. **`OnWorldSearchTextChanged` (`:546-561`)**: bifurcar — si `SelectedCategory == All`,
   comportamiento actual (debounce + barrido); si no, `ApplyInventoryFilter()` directo.
9. **`BuildWorldSearchQuery` (`:570-612`)**: los cinco filtros de presencia de 10.4.
10. **`LoadFromPathAsync` (`:443-500`)**: calcular `_presence` en el `Task.Run` (10.3), llamar a
    `RebuildInventory()`, poner `SelectedCategory = All` y `WorldHighlight = null`. En el
    `catch`, `_presence = null` y `WorldHighlight = null`.
11. **Comandos nuevos**:
    - `SelectCategory(WorldSearchCategory)` — para las píldoras (o `IsChecked` por binding
      directo con un converter de enum; el proyecto **no** tiene aún un
      `EnumToBooleanConverter`, así que un comando es más barato que añadir uno).
    - `SearchInventoryRow(WorldInventoryRowViewModel)` — clic sobre una fila: construye la query
      con ese único id (o `(Type,U,V)`) y lanza `RunWorldSearchAsync`.
    - `SearchCheckedInventory()` — "Buscar seleccionados".
    - `MarkOresOnMap()` / `ClearOreMarks()` — 11.3 + 11.4.
    - `ToggleNpcFilter(string)` — los tres chips.
12. **`ApplyNpcFilter` (`:532-541`)**: añadir los tres chips a la condición y la ordenación por
    profundidad. La invariante de que `Npcs` nunca se filtra **no se toca**.

### 14.4 `TerrasavrNative.App/MainWindow.xaml`

1. **`:3233`** — `MinWidth="220" MaxWidth="300"` → `MinWidth="260" MaxWidth="380"` (D4).
2. **Entre `:3265` y `:3266`** — la `Image` de la capa de resaltado (11.3).
3. **`:3455-3619`** — reescribir el `DockPanel` lateral entero con la estructura de la sección 9.
   Se **conserva copiando literalmente**:
   - la plantilla de fila de resultado (`:3505-3540`) — se mueve, no se reescribe;
   - la plantilla de fila de NPC (`:3598-3615`) — se mueve, con la línea de profundidad añadida;
   - el `Expander "NPCs que faltan"` (`:3576-3591`) — se mueve dentro de la categoría NPCs;
   - la casilla de distancia, el resumen y los botones `‹`/`›` (`:3479-3500`) — se mueven al
     bloque común de 9.4.
   Se **elimina**: el `Expander "Buscar en el mundo"` como contenedor (`:3465`, `:3545`); su
   contenido pasa a ser el cuerpo permanente del panel.
4. **Plantilla nueva** `x:Key="InventoryRowTemplate"` — muestra de color 14x14 + `CheckBox` +
   nombre + recuento a la derecha, calcada de `TileWallPickerControl.xaml:68-114` con los
   estilos de este proyecto.

### 14.5 `TerrasavrNative.App/Styles/Theme.xaml`

Un estilo nuevo: `x:Key="CategoryPill"` (`TargetType="RadioButton"`), descrito en 9.1. Y, si se
usa el mismo look para los chips de NPC y los conmutadores de vista,
`x:Key="CategoryChip"` (`TargetType="ToggleButton"`) con el mismo `ControlTemplate`.

### 14.6 `TerrasavrNative.App/MainWindow.xaml.cs`

**Ningún cambio necesario.** La capa de resaltado vive dentro del mismo `Grid` escalado, así que
hereda zoom y desplazamiento; los marcadores nuevos usan el mecanismo que ya existe; y
`OnNavigateToTile` (`:487-491`) sirve igual para una veta que para un NPC.

### 14.7 `TerrasavrNative.Core/WldFormat/WldWorld.cs`, `WldReader.cs`, `WldHeader.cs`

**Ningún cambio.** Todo lo que hace falta ya se lee. Esto es deliberado: es la parte del código
más delicada del proyecto y no hay ninguna razón para tocarla en esta ronda.

## 15. Orden de implementación sugerido

Cada paso deja algo verificable y comiteable por sí solo:

1. **`WorldPresenceIndex` + tests + cálculo en `LoadFromPathAsync`.** Aún sin UI. Se verifica con
   una traza o un test contra un `.wld` real. *(Es el cimiento de todo lo demás.)*
2. **Filtrado de candidatos en `BuildWorldSearchQuery`** (10.4). Cambio de tres líneas por bucle,
   efecto inmediato y medible: buscar algo inexistente deja de recorrer el mundo.
3. **El selector de categorías + "Todo" + "Objetos"**, con el inventario y su filtro de texto.
   Es donde se ve por primera vez la promesa "solo lo que hay en este mundo".
4. **"NPCs"**: mover la lista actual dentro de la categoría y añadir los tres chips y la
   profundidad.
5. **"Cofres"**: las dos vistas.
6. **"Minerales"**: `OreTileCatalog` + `OreVeinFinder` + la lista de vetas (todavía sin capa).
7. **La capa de resaltado** (`WorldHighlightRenderer`). Es lo más caro en memoria y lo último,
   para poder medirlo con todo lo demás ya funcionando.

## 16. Qué NO hacer

- **No** añadir un botón "Buscar" al estilo de TEdit (`find_search_world`). Este proyecto ya
  eligió buscar al teclear con debounce, y en cuatro de las cinco categorías el texto ni siquiera
  toca el mundo.
- **No** portar `SpriteTreePickerViewModel` como árbol del catálogo completo. Las variantes se
  ofrecen podadas a lo presente (`SpriteVariantCounts`), que es 4.774 en el peor caso real
  medido, no las decenas de miles del catálogo.
- **No** intentar deduplicar sprites multi-tile en esta ronda. La decisión de
  `WorldSearch.cs:17-23` sigue vigente y necesita el `frameSize` de TEdit, que este proyecto no
  importa. El agrupado en vetas (11.4) resuelve por otra vía el caso donde más molestaba.
- **No** tocar `WldReader` ni leer tile entities. Sigue sin hacer falta.
- **No** añadir un tercer buscador con gramática propia. La gramática de `LibrarySearchGrammar`
  se queda en "Todo"; en las demás categorías, filtro por nombre **o** id, que es lo que hace
  TEdit (`TileWallPickerViewModel.cs:118-124`) y lo que un inventario corto pide.
- **No** escribir nunca en el `.wld`.

---

# PARTE III

## 17. Huecos y limitaciones de esta investigación

- **Las mediciones de la sección 6 son de un port a JavaScript del lector, no del C# real.**
  Reimplementé `WldReader` en Node para poder censar mundos sin compilar ni arrancar la app. Los
  **recuentos** (260 tipos de tile, 4.387 vetas de cobre, etc.) son datos del fichero y son
  fiables: el algoritmo es el mismo y los tres mundos se leyeron enteros sin desalinearse. Los
  **tiempos** (~90 ms el barrido, ~40 ms un flood-fill) son orientativos: V8 con `TypedArray` no
  es .NET, y no medí nada dentro de la aplicación real.
- **No compilé ni ejecuté nada de la propuesta.** Todo el código de la Parte II es diseño sobre
  papel; ningún fragmento pasó por el compilador.
- **`PixelFormats.Indexed1` con alfa en la paleta no lo verifiqué** (11.3, mitigación 3). Es la
  optimización obvia para la capa de resaltado (2,5 MB en vez de 80 MB), pero no comprobé que
  WPF respete el canal alfa de una `BitmapPalette` en un `WriteableBitmap` indexado. La
  propuesta usa `Bgra32`, que sí está probado en este proyecto.
- **No verifiqué cómo se ven los marcadores a zoom bajo.** El halo de 1 tile de 11.3 es un
  cálculo mío (una veta media de 14,7 tiles a `Zoom = 0.1` da ~1,5 px), no una comprobación
  visual. Puede necesitar 2 tiles de halo, o un tamaño que dependa del zoom.
- **No ejecuté TEdit.** Igual que en la ronda anterior: todo es lectura de código y de recursos.
  No vi su barra lateral funcionando, así que las proporciones que describo en 1.2 salen del
  XAML (`Height="150"`, `d:DesignWidth="400"`), no de una captura.
- **No leí `GlobalStyles.xaml` entero** (70 KB). Del estilo `ActivityBarItem` cité las líneas
  que localicé (`:852-878`, `:30`, `:32`) pero no repasé su `ControlTemplate` completo ni el de
  `ActivityBarTabControl`.
- **No leí `SpriteTreePickerControl.xaml`** más allá de saber que existe y que es el control de
  la cuarta pestaña. La propuesta no lo porta, así que no me pareció necesario; si alguien
  decide portar el árbol tal cual, hay que leerlo.
- **La licencia de TEdit sigue sin verificarse** (mismo hueco que
  `ESPEC-buscador-mundo-tedit.md#6`). La API de GitHub devuelve `NOASSERTION`. Todo este
  documento son descripciones de comportamiento, formatos y **tablas de ids del juego** (que son
  datos de Terraria, no de TEdit), no copias de su código. Aun así: **antes de copiar una línea
  literal de TEdit, hay que abrir su `LICENSE`.**
- **Los cofres de Calamity ("Void Vault") no los comprobé** en un mundo real: `ReadChests` ya
  respeta el `MaxItems` por cofre de la versión ≥ 294 (`WldReader.cs:332`), pero ninguno de los
  tres mundos que censé parece tener uno.
- **No hubo ningún bloqueo de herramienta.** El clon de TEdit, la API de GitHub, la lectura de
  los `.wld` reales y el port a Node funcionaron a la primera. Nada que anotar en `bitacora.md`
  por ese lado.
