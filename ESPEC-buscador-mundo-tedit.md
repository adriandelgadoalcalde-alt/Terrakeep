# Buscador de objetos del mundo: ingeniería inversa del de TEdit y propuesta para Terrakeep

Investigación real del buscador ya existente de **TEdit** (github.com/TEdit/Terraria-Map-Editor),
para diseñar el buscador de "¿dónde está esto en el mundo?" de la pestaña **Exploración** de
Terrakeep. Terrakeep **no** va a ser un editor de mundos: solo localizador.

**Disciplina de este documento**: las secciones 1 a 4 son lectura directa de ficheros reales
(de TEdit descargados de la rama por defecto, o de este proyecto). Cada afirmación lleva su
fichero y su línea. La sección 5 es una **propuesta de diseño** — está separada a propósito y
todo lo que hay ahí es criterio mío, no hecho verificado. La sección 6 recoge lo que **no**
verifiqué.

**Metadatos reales del repositorio de TEdit** (consultados por mí vía API de GitHub,
`api.github.com/repos/TEdit/Terraria-Map-Editor`):

- Rama por defecto real: **`main`** (no `master`)
- Licencia declarada: `NOASSERTION` según la API — el encargo la daba por MIT; **no la
  verifiqué leyendo el `LICENSE`** (ver sección 6)
- 2 184 estrellas; 1 429 ficheros en el árbol de `main` (no truncado)

Todos los ficheros de TEdit que cito los descargué de
`https://raw.githubusercontent.com/TEdit/Terraria-Map-Editor/main/<ruta>` y los leí enteros.

---

## 1. Qué funcionalidad de búsqueda tiene TEdit REALMENTE

Encontré exactamente **tres** mecanismos relacionados, más un cuarto que resultó no serlo.
Los localicé buscando en el árbol completo del repo por `search|find|filter|locate|highlight|jump|goto|navigate`:

```
src/TEdit/ViewModel/FindSidebarViewModel.cs        <- el buscador
src/TEdit/View/Sidebar/FindSidebarView.xaml
src/TEdit.Tests/ViewModel/FindResultAccumulatorTests.cs
src/TEdit/ViewModel/FilterSidebarViewModel.cs      <- el filtro visual
src/TEdit/ViewModel/FilterManager.cs
src/TEdit/Render/FilterOverlayBuffer.cs
src/TEdit/UI/Controls/FilterablePickerControl.xaml
src/TEdit/Scripting/Api/FinderApi.cs               <- búsqueda por script
src/TEdit/Scripting/Examples/find-life-crystals.js
src/TEdit/ViewModel/WorldExplorerViewModel.cs      <- NO es esto (ver 1.4)
```

### 1.1 La barra lateral "Find" — el buscador propiamente dicho

`src/TEdit/ViewModel/FindSidebarViewModel.cs` (492 líneas) y su vista
`src/TEdit/View/Sidebar/FindSidebarView.xaml` (137 líneas).

**Busca exactamente cuatro cosas, en cuatro pestañas** (`FindSidebarView.xaml:120-134`):

| Pestaña | Qué busca | Origen de la lista |
|---|---|---|
| Cofres | **ítems dentro de contenedores** | `PickerDataSource.Items` |
| Tiles | **bloques no enmarcados** sobre el mapa | `PickerDataSource.TileBricks` |
| Paredes | **paredes** sobre el mapa | `PickerDataSource.Walls` |
| Sprites | **tiles enmarcados**, con variante por UV | árbol de `TileProperties` |

Las cuatro son **multiselección con casilla**, y las selecciones **persisten al cambiar de
pestaña** (comentario literal en `FindSidebarViewModel.cs:48`). Una sola búsqueda puede
combinar las cuatro a la vez: `ExecuteSearch` (`:157-218`) llama a `SearchContainers`
(`:246-302`) y a `SearchMap` (`:304-382`) según lo que haya marcado.

**Lo que NO busca** (lo comprobé leyendo el fichero entero, no por omisión):

- **NPCs** — no hay pestaña ni código de NPC en `FindSidebarViewModel`. TEdit sí tiene un
  `NpcNameEditor` y un `NpcSelectorView`, pero son de edición, no de localización.
- **Biomas** — no hay ninguna búsqueda de biomas. TEdit tiene `MorphTool` y
  `GenerateApi.Biomes.cs`, que **transforman o generan** biomas, no los localizan.
- **Letreros por texto** — `world.Signs` existe en el modelo, pero `SearchContainers` solo
  recorre `world.Chests` (`:252`) y `world.TileEntities` (`:274`).
- **Búsqueda por coordenada** — no hay "ir a X,Y". La navegación siempre parte de un
  resultado.

### 1.2 Cómo está implementado (a alto nivel, con las citas)

**Bucle de mapa: fuerza bruta sobre toda la rejilla.** `SearchMap:317-381`:

```csharp
for (int x = 0; x < world.TilesWide; x++)
  for (int y = 0; y < world.TilesHigh; y++) { ... }
```

Dentro de cada celda comprueba, en este orden: tile (`:324`, `tile.IsActive && tileIdSet.Contains(tile.Type)`),
pared (`:337`, `tile.Wall > 0 && wallIdSet.Contains(tile.Wall)`) y sprites (`:350-379`).

**Sprites: coincidencia opcional por UV.** `:352-362` — para cada sprite seleccionado, si el
nodo marcado era una **variante** concreta se exige que `tile.GetUV()` coincida; si el nodo
marcado era la **raíz** del árbol, vale cualquier variante. Eso lo decide
`SpriteTreePickerViewModel.GetSelectedSprites` (`:151-173`), que devuelve
`(TileId, Vector2Short? UV)` con `UV = null` para la raíz.

**Deduplicación por ancla.** Un sprite ocupa varios tiles, así que la lista de resultados
guarda uno solo por instancia: `:368-370` calcula `world.GetAnchor(x, y)`, lo empaqueta como
`(long)anchor.Y * 65536L + anchor.X` y lo mete en un `HashSet<long>`. En cambio el **resaltado
sí ilumina todos los tiles** del sprite: `AddSpriteToOverlay` (`:223-244`) lee
`TileProperty.GetFrameSize(tile.V)` y añade el rectángulo completo.

`World.GetAnchor(int x, int y)` está en `src/TEdit.Terraria/World.cs`: si el tile no es
enmarcado devuelve `(x,y)`; si lo es, resuelve el estilo por UV
(`WorldConfiguration.Sprites2 → GetStyleFromUV`) y calcula el desplazamiento con
`tile.U % ((TextureGrid.X + 2) * sizeTiles.X) / (TextureGrid.X + 2)` (y lo análogo en Y).

**Contenedores.** `SearchContainers:252-271` recorre `world.Chests` y, dentro, `chest.Items`,
casando `item.NetId` contra el conjunto seleccionado. `:274-301` hace lo mismo con
`world.TileEntities` que tengan `entity.Items` (marcos de ítem, maniquíes, percheros...),
casando por `item.Id`. En ambos casos la coordenada del resultado es la del **contenedor**,
no la del ítem.

**Tope de resultados.** `FindResultAccumulator` (`:24-35`) cuenta **todas** las coincidencias
en `TotalCount`, pero solo deja añadir a la lista visible las primeras
`MaxDisplayedResults = 1000` (`:46`). El resumen distingue las dos cifras (`ResultSummary`,
`:105-110`). El resaltado del mapa, en cambio, usa **todas** las posiciones, no las 1000
(comentario literal en `:205-206`).

**Distancia opcional al spawn.** Casilla `CalculateDistance` (`:57-58`). Cuando está marcada,
cada resultado lleva un `ExtraInfo` con `Vector2.Distance(spawn, ...)` redondeada (`:258-260`,
`:329-331`, `:342-344`, `:373-375`) y la lista se **reordena por distancia** al final
(`:196-203`). El spawn sale de `new Vector2(world.SpawnX, world.SpawnY)` (`:174`).

**Navegación.** `NavigateNext` / `NavigatePrevious` (`:416-430`) recorren la lista en círculo
con módulo. `GoToCurrentResult` (`:445-468`) pone la cruz (`ShowCrosshair`,
`CrosshairTileX/Y`) y llama a `_wvm.ZoomFocus?.Invoke(x, y)` o a `_wvm.PanTo?.Invoke(x, y)`
según la casilla `AutoZoomOnNavigate` — cuyo valor por defecto es **false**, con el comentario
literal `// Default false - just pan, don't zoom` (`:61`).

**El resultado como registro.** `FindResultItem(string Name, int X, int Y, string ResultType, string? ExtraInfo)`
(`:17-22`), con `DisplayText` = `"{Name} @ {X}, {Y} ({ResultType}) {ExtraInfo}"`.

**Los pickers.** `TileWallPickerViewModel` (`src/TEdit/ViewModel/Shared/TileWallPickerViewModel.cs`,
171 líneas) es una lista observable con un `ICollectionView` filtrado por texto; el filtro
(`FilterItem:118-124`) casa **el nombre sin distinguir mayúsculas O el id como texto**. Tiene
`CheckAll`/`UncheckAll` que actúan **solo sobre lo que el filtro deja visible** (`:129`,
`:139`) — un detalle de usabilidad que merece la pena copiar. `SpriteTreePickerViewModel`
(182 líneas) hace lo mismo pero como árbol de dos niveles: raíz = tile enmarcado con
`Frames?.Count > 0` (`:58-60`), hijos = cada `frame` con su `UV`.

### 1.3 El resaltado en el mapa

Está en `FilterManager` (`src/TEdit/ViewModel/FilterManager.cs`, 357 líneas) y en
`FilterOverlayBuffer` (`src/TEdit/Render/FilterOverlayBuffer.cs`, 108 líneas).

- `FilterManager.SetFindResults(IEnumerable<(int X, int Y)>)` (`:102-108`) vuelca las
  posiciones en un `HashSet<long>` con `PackPosition(x, y) => (long)y * 65536L + x` (`:97`) y
  activa `FindOverlayActive`.
- Comentario literal de `:80-83`: *"When true, the darken overlay uses find result positions
  instead of filter criteria. Found tiles render normally, everything else is
  darkened/desaturated."* Es decir: **no se pinta un marcador sobre lo encontrado; se oscurece
  todo lo demás.**
- `FilterOverlayBuffer` es una máscara de **un byte por tile** (`0 = despejado`,
  `255 = oscurecido`), troceada en chunks de hasta 256x256 con el mismo reparto que el mapa
  (`:7-24`), y cada chunk lleva un `ChunkStatus { Mixed, AllClear, AllDarkened }` para poder
  saltárselo entero. El comentario de `:9-11` dice que usar `byte[][]` en vez de `Color[][]`
  ahorra el 75 % de memoria.
- `FilterManager.Revision` (`:25`) es un contador que sube en **cada** mutación, para que el
  renderer sepa cuándo tiene que reconstruir la máscara. `DarkenAmount` y `DesaturateAmount`
  (`:60-74`) están marcados como *"Shader-only uniform — no rebuild needed"*.

El **filtro** (`FilterSidebarViewModel`, 217 líneas) es una función distinta del buscador:
en vez de listar posiciones, oculta o atenúa por criterio (tiles, paredes, líquidos, cables,
sprites), con dos modos `Hide`/`Darken` (`FilterManager.cs:21`) y tres modos de fondo
`Normal`/`Transparent`/`Custom` (`:22`). Comparte el mismo overlay que el buscador.

### 1.4 Búsqueda por script (`FinderApi`) y lo que no es buscador

`src/TEdit/Scripting/Api/FinderApi.cs` (46 líneas) expone al motor de scripts (Jint / Lua)
`finder.clear()`, `finder.addResult(name, x, y, resultType, extraInfo)`,
`finder.navigate(i)` y `finder.navigateFirst()`, con el mismo tope de 1000
(`MaxResults`, `:9`) y un aviso al superarlo (`:26-28`). Los resultados de un script van a
**la misma lista** de la barra lateral Find.

El barrido lo pone `BatchApi`: `FindTiles(Func<int,int,bool> predicate)`,
`FindTilesByType(int tileType, bool anchorOnly = false)` y `FindTilesByWall(int wallType)`
(`src/TEdit/Scripting/Api/BatchApi.cs:72`, `:137`, `:183`). El ejemplo real
`src/TEdit/Scripting/Examples/find-life-crystals.js` (15 líneas) hace exactamente eso para el
tile 12 (Corazón de Vida).

Dos cosas que **parecen** buscador y no lo son:

- **`WorldExplorerViewModel`** (581 líneas) es el selector de **ficheros** de mundo, con
  búsqueda por nombre de mundo y vista previa. No busca nada dentro del mundo.
- **`WorldAnalysis`** (`src/TEdit.Editor/WorldAnalysis.cs`) genera un **informe de texto**:
  recuento de tiles por tipo ordenado descendentemente (`:67`, `:106-110`), recuento de
  cables por color (`:68`, `:80-83`, `:124-128`), volcado de cofres con su contenido
  (`:131-155`), letreros (`:156`), NPCs (`:164`), tile entities (`:171-177`) y un montón de
  propiedades del mundo. Es un **censo, no un localizador**: no da coordenadas de tiles ni
  navega. Aun así, es la mejor referencia de "qué se puede contar" en un mundo.

---

## 2. Qué datos del `.wld` hace falta tener parseados

### 2.1 Mapa real de secciones del `.wld`

Verificado en `src/TEdit.Terraria/World.FileV2.cs:1415-1500`, que después de cada sección
comprueba `b.BaseStream.Position != sectionPointers[n]`:

| Puntero | Sección | Versión mínima | ¿La lee Terrakeep hoy? |
|---|---|---|---|
| `pointers[0]` | Cabecera / flags | — | **parcial** (hasta `RockLevel`) |
| `pointers[1]` | **Tiles** | — | **sí** |
| `pointers[2]` | **Cofres** | — | **no** |
| `pointers[3]` | **Letreros** | — | **no** |
| `pointers[4]` | **NPCs** | — | **sí** |
| `pointers[5]` | **Tile entities** | ≥ 116 (≥ 122 en formato actual) | **no** |
| `pointers[6]` | Placas de presión con peso | ≥ 170 | no |
| `pointers[7]` | Town manager (pilones) | ≥ 189 | no |
| `pointers[8]` | Bestiario | ≥ 210 | no |
| `pointers[9]` | Poderes creativos | ≥ 220 | no |

Esto encaja con lo que ya tiene el proyecto:
`TerrasavrNative.Core/WldFormat/WldHeader.cs` define
`TilesSectionOffset => Pointers[1]` y `NpcsSectionOffset => Pointers[4]`.

### 2.2 Formato real de las secciones que faltan

**Cofres** — `World.FileV2.cs:1770-1823`:

```
Int16  totalChests
Int16  maxItems            // SOLO si version < 294
por cada cofre:
  Int32 X, Int32 Y, String Name
  Int32 MaxItems           // SOLO si version >= 294
  por cada slot (MaxItems):
     Int16 stackSize
     si stackSize > 0:  Int32 netId, Byte prefix
```

**Letreros** — `World.FileV2.cs:1828-1839`:

```
Int16 totalSigns
por cada letrero:  String text, Int32 x, Int32 y
```

TEdit descarta al cargar los letreros cuya casilla no sea realmente un letrero
(`:1443-1450`, `tile.IsActive && tile.IsSign()`).

**Tile entities** — `World.FileV2.cs:1953-1965` (`Int32 numEntities` + `TileEntity.Load` por
cada una). Los tipos reales, según el `switch` de `src/TEdit.Terraria/TileEntity.cs:20-77`:
`TrainingDummy`, `ItemFrame`, `LogicSensor`, `DisplayDoll` (maniquíes), `WeaponRack`,
`HatRack`, `FoodPlatter`, `TeleportationPylon`, `DeadCellsDisplayJar`, `CritterAnchor`,
`KiteAnchor`. De esas, las que llevan ítems son las que `SearchContainers` recorre.

**Placas de presión** — `World.FileV2.cs:1983-1995`: `Int32 count` + `Int32 PosX, Int32 PosY`
por cada una. Trivial, y da "dónde están los sensores".

### 2.3 Qué lee Terrakeep hoy — inventario exacto

Leído entero de `TerrasavrNative.Core/WldFormat/WldReader.cs` (289 líneas),
`WldHeader.cs`, `WldTile.cs` y `WldNpc.cs`:

**Cabecera** (`WldReader.ReadHeader:39-127`): `Version`, `Pointers[]`, `TileFrameImportant[]`,
`Title`, `WorldId`, `TilesHigh`, `TilesWide`, `SpawnX`, `SpawnY`, `GroundLevel`, `RockLevel`.
Se corta ahí a propósito (comentario de `WldHeader.cs:8-11`).

**Tiles** (`ReadOneTile:161-247`) → `WldTile(short Type, short Wall, byte LiquidType, byte LiquidAmount, short U, short V)`.
Se **descartan** al leer, aunque el lector avanza correctamente sobre sus bytes:
`TileColor` (`:202`), `WallColor` (`:210`), y el `header4` de versión ≥ 269 (`:172`).
**Nunca se leen** los bits de cables, actuador, media-baldosa/pendiente ni "invisible".

**NPCs** (`ReadNpcs:249-288`) → `WldNpc { Id, GivenName, TileX, TileY, Homeless, VariationIndex }`
+ `ShimmeredNpcTypes`.

**Nada más.** Cofres, letreros, tile entities, placas, pilones, bestiario y poderes creativos
no se leen en absoluto.

### 2.4 Qué haría falta añadir, por funcionalidad

| Para ofrecer... | Hace falta leer | ¿Ya está? |
|---|---|---|
| Buscar **tiles** por tipo | `WldTile.Type` | **sí** |
| Buscar **paredes** | `WldTile.Wall` | **sí** |
| Buscar **sprites** por variante (UV) | `WldTile.U/V` | **sí** |
| Buscar **líquidos** | `LiquidType`/`LiquidAmount` | **sí** |
| Buscar **NPCs** por tipo/nombre | `WldNpc` | **sí** (y ya hay UI) |
| Buscar **ítems en cofres** | sección `pointers[2]` | **no** |
| Buscar **texto en letreros** | sección `pointers[3]` | **no** |
| Buscar **ítems en maniquíes/marcos/percheros** | sección `pointers[5]` | **no** |
| Buscar **cables** | bits de cable del tile | **no** (ni se leen) |
| Buscar **pilones** | `pointers[5]` (tile entity) o `pointers[7]` | **no** |
| Distancia al spawn | `SpawnX/SpawnY` | **sí** |
| Ancla de sprite multi-tile | tamaño de frame por tile | **no** (ver 5.4) |

### 2.5 Catálogos de nombres: Terrakeep ya está sorprendentemente bien servido

- `TerrasavrNative.App/Assets/tile_names.json` — su estructura real (la inspeccioné) es
  `{ "tiles": { "<id>": { "name", "name_es"?, "frames"? { "u,v": { "name" } } } }, "walls": {...} }`.
  **Ya tiene el desglose por UV**, que es justo lo que necesita un picker de sprites tipo
  TEdit, y además nombres en español donde los hay.
  `TerrasavrNative.Core/Data/TileNameCatalog.cs:26-33` ya resuelve
  `TileVariantName(type, u, v)` con caída al nombre base.
- `vanilla_item_names.json` + `VanillaItemCatalog` — nombres de ítem en español para el picker
  de cofres.
- `npc_names.json`, `NpcIconResolver`, `NpcHeadIconResolver` — ya en uso en Exploración.
- `map_colors.json` — colores por tile/pared, sirven de muestra de color en la lista, igual
  que hace `PickerItemViewModel` en TEdit.
- Para Calamity: `CalamityCatalog` da nombres de ítem, pero **no** de tiles de Calamity, y
  TEdit tampoco (su `Data/TileOverrides/CalamityMod.json` mide 21 bytes, es decir, está vacío).

### 2.6 Lo que Exploración de Terrakeep ya tiene montado

Leído de `TerrasavrNative.App/ViewModels/ExplorationViewModel.cs`:

- Lista de NPCs con **búsqueda** (`Npcs`, `NpcSearchResults`, `MissingNpcs`, `:102-105`),
  con icono y coordenada.
- **Navegación a un tile ya funciona de extremo a extremo**: `NavigateToTileRequested`
  (`:127`), `NavigateToTile(x, y)` (`:136`), `GoToNpc` (`:130`), y el consumidor real en
  `MainWindow.xaml.cs:45` → `OnNavigateToTile` (`:487`), que ajusta los offsets del
  `ScrollViewer`. `MainViewModel.cs:352` ya lo usa para saltar al spawn de un personaje.
- **Zoom** con límites (`MinZoom = 0.02`, `MaxZoom = 6.0`, `:362`) y comandos
  `ZoomIn/Out/Reset` (`:376-378`).
- **Tooltip de hover** con nombre real de tile/pared por UV y nombre de líquido
  (`UpdateHover:252-277`).
- Renderizado del mundo a `WriteableBitmap` de 1 píxel por tile, en hilo de fondo
  (`WorldRenderer` + `LoadFromPathAsync`).

Es decir: **la mitad cara del buscador (navegar, hacer zoom, nombrar tiles) ya existe.**

---

## 3. Diferencias de arquitectura que condicionan el porte

| | TEdit | Terrakeep |
|---|---|---|
| Modelo de tile | `ITile` mutable con cables, pintura, pendiente | `WldTile` struct de solo lectura, 6 campos |
| Render del mapa | XNA/MonoGame por chunks + shaders | `WriteableBitmap` de WPF, 1 px por tile |
| Overlay | máscara `byte[][]` por chunk + shader | no existe |
| Propósito | editor completo | **solo lectura**, nunca se escribe un `.wld` |
| MVVM | ReactiveUI (`[Reactive]`, `WhenAnyValue`) | CommunityToolkit.Mvvm (`ObservableObject`) |

La consecuencia práctica es que el mecanismo de resaltado de TEdit (oscurecer todo menos lo
encontrado, con máscara por chunk y shader) **no se puede portar tal cual**: Terrakeep pinta el
mundo en un único `WriteableBitmap` congelado.

---

## 4. Resumen de los hechos que sostienen la propuesta

1. TEdit busca **ítems en contenedores, tiles, paredes y sprites**; **no** busca NPCs, biomas
   ni texto de letreros.
2. Su algoritmo es **fuerza bruta O(ancho × alto)** en un doble bucle, sin índice previo.
3. Deduplica por **ancla** de sprite y resalta **todos** los tiles del sprite.
4. Tope de **1000** resultados en lista, sin tope en el resaltado.
5. Distancia al spawn **opcional**, y reordenación por distancia.
6. Navegación circular anterior/siguiente + cruz + pan (zoom opcional, apagado por defecto).
7. El resaltado se hace **oscureciendo lo demás**, no marcando lo encontrado.
8. Terrakeep ya lee tiles (con UV), NPCs y spawn; **no** lee cofres, letreros ni tile entities.
9. Terrakeep ya tiene navegación a tile, zoom, tooltip por UV y catálogo de nombres con
   variantes por UV y traducción al español.

---

## 5. PROPUESTA DE DISEÑO para Terrakeep

**Todo lo que sigue es criterio mío, no hecho verificado.** Lo separo por fases para que se
pueda implementar por partes y cada una aporte valor sola.

### 5.1 Fase 1 — buscar lo que ya se lee (sin tocar `WldReader`)

Con lo que hay hoy en memoria se puede ofrecer ya: **tiles, paredes, sprites por variante,
líquidos y NPCs**. Cero cambios en el formato.

**`TerrasavrNative.Core/WldFormat/WorldSearch.cs`** (nuevo, en Core, sin dependencias de UI):

```csharp
public readonly record struct WorldSearchHit(int X, int Y, string Name, WorldSearchKind Kind, string? Extra);
public enum WorldSearchKind { Tile, Wall, Sprite, Liquid, Npc, ChestItem, Sign, TileEntityItem }

public sealed class WorldSearchQuery {
    public IReadOnlySet<int> TileTypes { get; init; }
    public IReadOnlySet<int> WallIds { get; init; }
    public IReadOnlyList<(int Type, (short U, short V)? Uv)> Sprites { get; init; }
    public IReadOnlySet<byte> LiquidTypes { get; init; }
    public IReadOnlySet<int> NpcIds { get; init; }
    public int DisplayLimit { get; init; } = 1000;
}
```

y un `WorldSearch.Run(WldWorld, WorldSearchQuery, TileNameCatalog, CancellationToken)` que
devuelva `(IReadOnlyList<WorldSearchHit> Hits, int TotalCount, IReadOnlyList<(int,int)> HighlightPositions)`.

Detalles que copiaría de TEdit tal cual:

- **Doble bucle x→y** (mismo orden que el RLE del `.wld`, así se aprovecha la caché).
- **`FindResultAccumulator`**: contar todo, mostrar los primeros N. Es un patrón bueno y
  barato; el usuario necesita saber "hay 12 400 bloques de tierra" aunque solo se listen 1000.
- **Distancia al spawn opcional** y reordenación por distancia.

Detalles donde me apartaría de TEdit:

- **`CancellationToken` y ejecución en `Task.Run`.** Un mundo grande son ~8 400 × 2 400 =
  20 millones de iteraciones; `ExplorationViewModel.LoadFromPathAsync` ya mide ~1,4 s solo para
  leer y pintar un mundo de 11 MB. La búsqueda debe salir del hilo de UI igual que el pintado,
  y poder cancelarse si el usuario cambia la selección.
- **Añadir NPCs y líquidos**, que TEdit no busca y en Terrakeep salen gratis: los NPCs ya
  están en `WldWorld.Npcs`, y el líquido ya está en `WldTile`.
- **Búsqueda por texto directa**, sin obligar a marcar casillas: un cuadro "escribe qué
  buscas" que case contra `tile_names.json` (`name` y `name_es`), `vanilla_item_names.json` y
  `npc_names.json` a la vez, y proponga los candidatos. El picker de casillas de TEdit está
  pensado para un editor donde ya sabes el id; para un localizador, escribir "corazón de vida"
  es mucho más natural. `LibrarySearchGrammar` del propio proyecto ya resuelve un problema
  parecido para la Librería.

### 5.2 Fase 2 — leer cofres, letreros y tile entities

Es lo que convierte el buscador en "dónde está mi objeto", que es lo que un jugador quiere de
verdad. Cambios propuestos:

**`WldWorld.cs`** — tres colecciones nuevas:

```csharp
public required IReadOnlyList<WldChest> Chests { get; init; }
public required IReadOnlyList<WldSign> Signs { get; init; }
public required IReadOnlyList<WldTileEntity> TileEntities { get; init; }
```

**Modelos nuevos** (mismo estilo minimalista que `WldNpc`):

```csharp
public sealed class WldChest   { int X, Y; string Name; IReadOnlyList<WldChestItem> Items; }
public readonly record struct WldChestItem(int Id, int Stack, byte Prefix);
public sealed class WldSign    { int X, Y; string Text; }
public sealed class WldTileEntity { byte Kind; int X, Y; IReadOnlyList<WldChestItem> Items; }
```

**`WldReader`** — tres lectores nuevos siguiendo el formato de la sección 2.2, y en
`WldHeader` tres offsets nuevos: `ChestsSectionOffset => Pointers[2]`,
`SignsSectionOffset => Pointers[3]`, `TileEntitiesSectionOffset => Pointers[5]`.

Cautelas que pondría por escrito en el propio código:

- El umbral `version < 294` del `maxItems` de cofres es real y está citado; hay que respetarlo
  o se desalinea el flujo.
- Los tile entities tienen un `Load` polimórfico por tipo en TEdit (`TileEntity.cs`); si el
  formato de alguno cambia entre versiones y no se soporta, **es preferible saltar la sección
  entera con `Pointers[6]`** que romper la carga del mundo. Terrakeep solo lee, así que
  saltarse una sección no corrompe nada: es exactamente la política de "lo que no se entiende
  no se inventa" que ya sigue `WldHeader`.
- `WldReader.Read` debería aceptar un parámetro tipo `bool readContainers` para no encarecer
  la lectura barata que ya usa el lanzador de mundos.

### 5.3 Fase 3 — UI

La pestaña Exploración ya tiene el mapa, el zoom, el tooltip y la navegación. Añadiría un
panel lateral con:

1. **Cuadro de búsqueda por texto** (fase 1) con resultados agrupados por tipo.
2. **Modo avanzado plegado** con las pestañas de TEdit (Tiles / Paredes / Sprites / Cofres /
   NPCs), multiselección persistente entre pestañas, filtro por nombre **o por id**, y
   `CheckAll`/`UncheckAll` que actúen solo sobre lo filtrado (`TileWallPickerViewModel:129`).
3. **Lista de resultados virtualizada** (`VirtualizingPanel.IsVirtualizing="True"`, como
   `FindSidebarView.xaml:58-59`) con "N de M (limitado a 1000 de 12 400)".
4. **Anterior / siguiente circulares**, clic en un resultado → `NavigateToTile(x, y)`, que ya
   funciona.
5. **Casilla "distancia al spawn"**, apagada por defecto.
6. **Marcador visual**. Aquí propongo apartarse de TEdit: en lugar de oscurecer todo el mapa
   (que exigiría rehacer el pipeline de render), dibujar los marcadores como una **capa de WPF
   por encima del `Image` del mapa** — una cruz para el resultado activo y puntos o un `Path`
   agregado para el resto. Es mucho menos código, escala bien porque el número de resultados
   mostrados está topado, y encaja con que el mapa sea un `WriteableBitmap` congelado. Si más
   adelante hiciera falta resaltar decenas de miles de posiciones, entonces sí tocaría una
   segunda capa `WriteableBitmap` de máscara.

**ViewModels nuevos** (nombres en la línea de los que ya hay):

- `WorldSearchViewModel` — orquesta la consulta, la lista y la navegación; expone
  `SearchCommand`, `NextCommand`, `PreviousCommand`, `ClearCommand`.
- `WorldSearchResultViewModel` — una fila: icono, nombre, coordenada, tipo, distancia. Es el
  hermano de `WhereIsItResultViewModel`, que ya hace exactamente esto para el inventario del
  personaje ("¿Dónde lo tengo?"). **Merece la pena que las dos funciones se parezcan en la UI**:
  una busca en el personaje y otra en el mundo.
- `WorldSearchPickerViewModel` — el picker filtrable, reutilizable para tiles/paredes/ítems/NPCs.

### 5.4 Detalle pendiente: el ancla de los sprites multi-tile

TEdit deduplica con `World.GetAnchor`, que necesita el **tamaño de frame** de cada tile y su
`TextureGrid`, datos que están en `WorldConfiguration.TileProperties` (de `Data/tiles.json` de
TEdit) y que **Terrakeep no importa hoy** — `tile_names.json` solo trajo los nombres.

Tres opciones, en orden de coste:

1. **No deduplicar en la fase 1.** Un cofre de 2x2 saldría cuatro veces en la lista. Feo.
2. **Deduplicar por proximidad**: agrupar coincidencias del mismo `Type` que estén a ≤ 3 tiles
   de distancia y quedarse con la de menor `(y, x)`. Barato, aproximado, sin datos nuevos.
   No es fiel, pero para un localizador basta.
3. **Importar `frameSize`/`textureGrid` de `Data/tiles.json` de TEdit** ampliando el generador
   `generar-tile-names.js` que ya existe en el proyecto hermano, y portar `GetAnchor` fiel.
   Es la opción correcta y encaja con el precedente ya establecido en `CLAUDE.md` de usar TEdit
   como fuente para el formato `.wld`.

Recomendaría la 3 si se llega a la fase 2, y la 2 como apaño explícitamente documentado si
solo se hace la fase 1.

### 5.5 Qué NO copiar de TEdit

- El **motor de scripting** (Jint/Lua). `FinderApi`+`BatchApi` son elegantes, pero Terrakeep no
  es un editor y meter un intérprete de JavaScript por un buscador es desproporcionado.
- El **filtro visual** (`FilterSidebarViewModel`) — es una herramienta de edición
  (ver solo cierto tipo de bloque mientras dibujas), no de localización.
- El **overlay por chunks con shader** — ver 5.3 punto 6.
- **Escribir nada**. Terrakeep solo lee `.wld` (`WldWorld.cs:4`, literal: *"Solo lectura -
  nunca se escribe un `.wld` desde esta app"`). El buscador no cambia eso.

---

## 6. Huecos y limitaciones de esta investigación

- **La licencia de TEdit no la verifiqué.** El encargo la daba por MIT; la API de GitHub
  devuelve `NOASSERTION`, que suele significar que el fichero `LICENSE` no es reconocible
  automáticamente. No abrí el `LICENSE`. **Antes de portar código literal de TEdit hay que
  mirarlo.** Todo lo de este documento son descripciones de comportamiento y formatos de
  fichero, no copias de código.
- **Leí el buscador y el filtro enteros, pero no el renderer.** `PixelMapManager`,
  `WorldRenderXna` y cómo se aplica exactamente la máscara `FilterOverlayBuffer` en el shader
  los conozco solo por los comentarios de `FilterOverlayBuffer.cs`.
- **`TileEntity.Load` no lo leí campo a campo** — solo el `switch` de tipos (`:20-77`). El
  formato binario exacto de cada tipo de tile entity hay que sacarlo de ahí antes de
  implementar la fase 2.
- **No comprobé si TEdit tiene búsqueda desde el menú superior** además de la barra lateral:
  no abrí `WorldViewModel.Commands.cs` (es grande) ni los `.xaml` de menú.
- **No ejecuté TEdit.** Todo es lectura de código; no vi la herramienta funcionando.
- **No medí nada de rendimiento** del barrido propuesto sobre un mundo real de Terrakeep. La
  cifra de ~1,4 s que cito para leer+pintar sale del comentario ya existente en
  `WldReader.cs:26-31`, no de una medición mía.
- **No hubo ningún bloqueo de herramienta** en esta investigación (GitHub API, `curl` y el
  árbol del repo funcionaron a la primera). Nada que anotar en `bitacora.md` por ese lado.
