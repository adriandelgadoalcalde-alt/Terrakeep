# Sprites reales en Exploración, botones de la barra lateral e insignias de Inicio

> Informe del advisor Opus, 4-sep-2026. Encargo real del usuario (verbatim, sin puntuación,
> tal cual lo escribió):
>
> *"faltan todos los sprites en exploración de que es cada cosa como lo que es un cofre dorado
> de agua etc solo salen cuadrados de colores pero no los sprites reales quiero todos los
> sprites de todos los objetos del mundo en el buscador de exploración y quiero que cambies
> todos los botones de exploración esos ovalados por algo que realmente vaya con la estética
> como el boton de cargar personaje o el de cargar mundo pero que se diferencien un poco de
> botones principales claro ademas en el inicio los personajes que tienen mod solo marcan
> calamity estaría bien que tuvieran otra etiqueta no solo calamity si no que haga referencia a
> que son personajes verdaderamente de tmodloader que s lo principal al igual que cuando un
> personaje es vanilla que tenga dicha etiqueta"*
>
> Tres tareas: **A)** sprites reales en el inventario de Exploración; **B)** rediseño de los
> selectores ovalados de esa barra lateral; **C)** separar "es de tModLoader" de "tiene
> Calamity" y añadir "Vanilla" en las tarjetas de Inicio.
>
> Este informe **no implementa nada**. Todo lo que dice está verificado contra datos o código
> reales de esta máquina; donde no lo esté, lo dice explícitamente.

---

## 0. Cómo se ha verificado cada cosa (para poder auditar este informe)

| Afirmación | Cómo se comprobó |
|---|---|
| Geometría de recorte de tiles | Prototipo real en Node sobre los **754** tiles de `tiles.json` y los `Tiles_{id}.xnb` reales de Steam; salida inspeccionada visualmente en hojas de contactos |
| El hueco de 2 px entre celdas está vacío | Volcado de píxeles reales de `Tiles_21.xnb` (fila y=8, x=14..21 y x=30..40) |
| El juego dibuja celda a celda | `tModLoader-Decompiled/tModLoader/Terraria/GameContent/Drawing/TileDrawing.cs:1951` |
| Geometría de recorte de paredes | `Framing.cs:119` (`wallFrameSize = 36,36`), `Framing.cs:135` (estilo 15), `WallDrawing.cs:213` (rect de 32×32) |
| Tipos de tile que son cofre | `Terraria/ID/TileID.cs:359` y `:367` |
| Formato real del `.tplr` y clave `usedMods` | Volcado NBT real de los **4** `.tplr` de esta máquina + `Terraria/ModLoader/IO/PlayerIO.cs:67,408-418` |
| Coste de leer el `.tplr` en Inicio | Arnés C# real (`dotnet run -c Release`) contra la carpeta real de personajes del usuario |

Los prototipos y hojas de contactos viven en el scratchpad de la sesión; **no** forman parte del
repo (son desechables). Lo que sí hay que crear de verdad es lo que dice la sección A.6/A.7.

---

# PARTE A — Sprites reales en el inventario de Exploración

## A.1 Qué hay hoy exactamente

`TerrasavrNative.App/ViewModels/ExplorationViewModel.cs:70`:

```csharp
public sealed partial class WorldInventoryRowViewModel(int id, short u, short v, string name, int count, int? veinCount, Color swatchColor) : ObservableObject
```

`SwatchColor` sale de `MapColorCatalog.TileColor(id)` / `WallColor(id)` — la paleta real del
**mapa** del juego portada de TEdit. No es un sprite: es el color de un píxel del minimapa.

`TerrasavrNative.App/MainWindow.xaml:1234-1239` lo pinta como un `Border` de 14×14 con
`CornerRadius="2"` y un `SolidColorBrush Color="{Binding SwatchColor}"`. De ahí los "cuadrados
de colores" del encargo.

Las **cinco** construcciones reales de la fila, y qué significa `Id/U/V` en cada una:

| Línea | Vista | `Id` | `U`,`V` | Icono que le corresponde |
|---|---|---|---|---|
| `ExplorationViewModel.cs:307` | Cofres → *Por tipo de cofre* | tipo de tile (21/88/467) | offset **real en píxeles** de la variante | icono de **variante de tile** |
| `:321` | Cofres → *Por lo que contienen* | **NetId de objeto** | 0,0 | icono de **objeto** (ya existe) |
| `:350` | Minerales (3 grupos) | tipo de tile | 0,0 | icono **base de tile** |
| `:373` | Objetos → Tiles | tipo de tile | 0,0 | icono **base de tile** |
| `:377` | Objetos → Paredes | id de pared | 0,0 | icono de **pared** |
| `:381` | Objetos → Líquidos | código 1-4 | 0,0 | ninguno (ver A.11) |

De estos, **solo el de objeto ya existe** en el proyecto (`VanillaIconResolver`). Los de tile y
pared son trabajo nuevo real.

## A.2 Corrección importante sobre "Cofres → Por lo que contienen"

El encargo asumía que ahí hay que replicar la rama `item.IsCalamity` de
`ItemSlotViewModel.UpdateFrom` (`ItemSlotViewModel.cs:270-284`). **No es así**, y el propio
código de Exploración ya lo documenta (`ExplorationViewModel.cs:158-161`):

> *"un NetId de Calamity real (que tModLoader asigna en tiempo de carga del mod, no coincide con
> el synthetic id que usa el resto de este puerto para .plr) cae en su propio 'Item #N' de
> fallback, nunca se inventa."*

`GameItem.IsCalamity` es `Id >= CalamityIds.ItemIdBase` (= **20 000 000**), un id **sintético e
interno de esta app**, que nunca aparece en un `.wld`. Un objeto modeado dentro de un cofre de un
mundo real trae el NetId de runtime de tModLoader (unos pocos miles), no 20 millones. Por tanto:

* La rama Calamity **nunca dispararía** para una fila de cofre — sería código muerto que además
  mentiría sobre la semántica del dato.
* Lo correcto es **solo** `VanillaIconResolver.GetIconPath(netId)`, que devuelve `null` para
  cualquier NetId sin `Item_{id}.png` extraído (incluidos todos los modeados) → respaldo al
  cuadradito de color, igual que hoy.

Esto hay que dejarlo escrito en un comentario en el punto de llamada, porque es exactamente el
tipo de cosa que la siguiente pasada "arreglaría" por error.

## A.3 Formato real de los iconos de TILE (verificado)

### A.3.1 Las dos fuentes

1. **Píxeles**: `C:\Program Files (x86)\Steam\steamapps\common\Terraria\Content\Images\Tiles_{id}.xnb`.
   Verificado: existen **los 754** ids `0..753` sin un solo hueco (comprobado uno a uno). Hay
   además 11 ficheros con nombre no numérico (`Tiles_5_0.xnb`, `Tiles_2_Beach.xnb`,
   `Tiles_59.bak.xnb`, `Tiles_199-gross.xnb`…) que **no** se tocan: el extractor lee
   exclusivamente `Tiles_{id}.xnb`.
2. **Geometría**: `Terrasavr-Calamity-Beta/resources/app/xnb-lzx-tool-refs/tiles.json` (TEdit
   real, **754** entradas, ids `0..753`).

> ⚠️ **`xnb-lzx-tool-refs/` está en el `.gitignore` del otro repo**
> (`Terrasavr-Calamity-Beta/resources/app/.gitignore:8`, comprobado con `git check-ignore -v`).
> No es parte de ningún repo. Si falta, hay que volver a bajar
> `src/TEdit.Terraria/Data/tiles.json` y `.../walls.json` de
> `github.com/TEdit/Terraria-Map-Editor` antes de poder ejecutar el extractor. Por eso **los PNG
> resultantes se comitean** (igual que los 11 417 PNG que el repo ya tiene) y el script es una
> herramienta de regeneración de un solo uso, no un paso de build.

### A.3.2 Estructura real de una entrada de `tiles.json`

Censo real de las 754 entradas (contado con Node, no estimado):

```
claves: id 754 | textureGrid 754 | frameGap 754 | frameSize 754 | name 754 | key 754 | color 754
        isSolid 339 | canBlend 355 | isFramed 412 | frames 412 | isAnimated 173 | isLight 153
        mergeWith 123 | isSolidTop 84 | placement 54 | special 37 | isStone 33 | largeFrameType 24
        textureWrap 17 | isGrass 10 | buffRadius/buffName/buffColor 8 | saveSlope 8 | isPlatform 7
        biomeVariants 2 | isCactus 1
textureGrid: [16,16] ×702, [20,20] ×17, [16,20] ×16, [16,18] ×4, [16,32] ×3, y 8 casos sueltos más
frameGap:    [2,2] ×748, [2,4] ×3, [2,3] ×1, [0,0] ×2 (ids 751 y 752, con textureGrid [18,18])
frameSize:   26 formas distintas; [[1,1]] ×436, [[2,2]] ×94, [[3,2]] ×55, [[6,3]] ×41 …
frames[]:    9546 entradas en total → name 9546, variety 8074, uv 9368, anchor 1325, size 317
```

Ejemplo real completo (id 21, el que pide el usuario):

```json
{"id":21,"isFramed":true,"textureGrid":[16,16],"frameGap":[2,2],"frameSize":[[2,2]],
 "isAnimated":true,"placement":"floor","name":"Chests","key":"Chest","color":"#...",
 "frames":[{"name":"Wooden Chest","uv":[0,0]},
           {"name":"Gold Chest","uv":[36,0]},
           {"name":"Gold Chest","variety":"Locked","uv":[72,0]},
           {"name":"Shadow Chest","uv":[108,0]}, … 54 en total]}
```

### A.3.3 La fórmula de recorte — y por qué NO es un rectángulo

El encargo proponía recortar un rectángulo de
`w*textureGrid + (w-1)*frameGap` (34×34 para un cofre). **Eso está mal**, y se puede demostrar
con píxeles reales. Volcado de `Tiles_21.xnb` (2000×114), fila `y=8`:

```
x=14: 105,105,105,255   x=15: 105,105,105,255   ← última columna de la celda 0
x=16:   0,  0,  0,  0   x=17:   0,  0,  0,  0   ← el hueco de 2 px: TRANSPARENTE PURO
x=18: 128,128,128,255   x=19: 128,128,128,255   ← primera columna de la celda 1
```

El hueco **no** es *edge bleed* (borde duplicado para el filtrado): son dos columnas
completamente transparentes. Un recorte de 34×34 metería una costura transparente de 2 px por el
centro del cofre.

Lo que hace el juego de verdad (`TileDrawing.cs:1951`, y otras 8 llamadas equivalentes en el
mismo fichero):

```csharp
Main.spriteBatch.Draw(drawData.drawTexture, normalTilePosition,
    new Rectangle(drawData.tileFrameX + drawData.addFrX, drawData.tileFrameY + drawData.addFrY, 16, 16), …);
```

Es decir: **cada celda de 16×16 se dibuja por separado**, y el `frameX` de la segunda celda de un
cofre vale 18 más que el de la primera (`textureGrid.X + frameGap.X`). El objeto completo se
compone celda a celda, sin el hueco.

**Fórmula correcta (composición celda a celda), verificada:**

```
Para un frame en (u, v) con tamaño (cw, ch) en CELDAS:
  destino = PNG de (cw * textureGrid.X) × (ch * textureGrid.Y)   ← 32×32 para un cofre
  para cy en 0..ch-1, cx en 0..cw-1:
      origen  = (u + cx*(textureGrid.X + frameGap.X),
                 v + cy*(textureGrid.Y + frameGap.Y))
      bitblt(origen, textureGrid.X × textureGrid.Y) → (cx*textureGrid.X, cy*textureGrid.Y)
```

Comprobado visualmente sobre las 129 variantes reales de cofre/cómoda: salen cofres de madera,
de oro (normal y cerrado), de sombra, barriles, papeleras, cómodas, cofres de la selva, del
abismo… reconocibles uno a uno.

### A.3.4 De dónde sale `(u, v)`

* **Variantes** (Cofres → *Por tipo de cofre*): el `(U, V)` que `WorldPresenceIndex.Build`
  guarda es literalmente `tile.U`/`tile.V` de la casilla en `chest.X, chest.Y`
  (`WorldPresenceIndex.cs:101-103`). Como `chest.X/Y` es la esquina **superior izquierda** del
  cofre, ese `(U,V)` coincide exactamente con el `frames[].uv` de `tiles.json`. Ya está
  demostrado en el propio repo: `WorldPresenceIndexTests.cs:126` afirma
  `ChestKindCounts[(21, 36, 0)] == 1` para un cofre de oro real, y `36,0` es justo el
  `uv` del frame *"Gold Chest"*.
* **Base** (Minerales, Objetos → Tiles): siempre `(0, 0)` con `frameSize[0]`. Deliberado: esas
  filas agrupan por `Type` a secas, y para un bloque no enmarcado el `U,V` de una instancia
  concreta del mundo es un recorte de *blending* con los vecinos, no un icono limpio.
  **Comprobado** que `(0,0)` sí da un bloque entero: la hoja `Tiles_7.xnb` (mineral de cobre) es
  una rejilla de 16×15 celdas de 18×18 y la celda `(0,0)` es un bloque completo y opaco.

### A.3.5 Los cuatro casos límite reales (medidos, no supuestos)

1. **178 frames sin clave `uv`** (de 9546). Son tiles con un único frame donde el `uv` es
   implícito. Ejemplo real: `{"id":12,"frames":[{"name":"Repaired Life Crystal","size":[2,2],"anchor":"Bottom"}]}`.
   → **Tratar `uv` ausente como `[0,0]`.**
2. **317 frames con clave `size` propia** (en celdas), que *manda* sobre `frameSize` del tile.
   Esto resuelve limpiamente los **3** tiles con más de un `frameSize`
   (`165 Cave Decos [[1,2],[1,1]]`, `185 Small Decos [[1,1],[2,1]]`,
   `233 Jungle Large Plants [[3,2],[2,2]]`): no hace falta adivinar nada, cada frame dice su
   tamaño. → **`size` del frame si existe, si no `frameSize[0]`.**
3. **`171 Christmas Tree`**: `tiles.json` dice `frameGap:[2,2]` y `frameSize:[[4,8]]`, pero el
   `Tiles_171.xnb` real mide **64×128** = exactamente `4*16 × 8*16`, es decir **sin relleno**.
   Es el único desajuste real de metadatos en las 754 entradas.
   → **Respaldo: si el recorte se sale de la hoja con el `frameGap` declarado, reintentar con
   `[0,0]`.** Con ese respaldo el árbol de Navidad sale perfecto (comprobado en la hoja de
   contactos).
4. **5 tiles cuyo recorte base sale 100 % transparente**: `373 Water Drip`, `374 Lava Drip`,
   `375 Honey Drip`, `461 Sand Drip`, `709 Magic Shimmer Dropper` (hojas de 18×18 con el sprite
   fuera de la celda `(0,0)`). → **No escribir PNG**; esas filas caen solas al cuadradito de
   color, que para una gota es de hecho más legible que un icono de 1 px.

### A.3.6 Desajuste de versión (documentar, no arreglar)

La instalación de Steam tiene `Tiles_{id}` hasta el **753** y `Wall_{id}` hasta el **367**;
`tiles.json` llega al 753 y `walls.json` al 366. O sea: **existe una pared (`367`) sin metadatos**
en el catálogo de TEdit. Esa pared se queda sin icono **y ya se queda hoy sin nombre**
(`TileNameCatalog` sale de la misma fuente) — comportamiento consistente, no una regresión nueva.
Los tiles de mods (ids muy por encima de 753) tampoco tendrán icono: caen al color, como hoy.

## A.4 Formato real de los iconos de PARED (verificado)

`walls.json` **no tiene** `textureGrid`, `frameGap` ni `frames[]`. Entrada real completa:

```json
{"id":1,"name":"Stone Wall","key":"Stone","color":"#353535FF","blendType":48}
```

Censo real de sus 367 entradas: `id/name/key/color` ×367, `blendType` ×159, `largeFrameType` ×22.
Nada más. Así que la geometría hay que sacarla del juego, no del catálogo. Y se saca entera:

* `Terraria/Framing.cs:119` → `wallFrameSize = new Point16(36, 36)`.
* `Terraria/GameContent/Drawing/WallDrawing.cs:213` → `new Rectangle(0, 0, 32, 32)`, y en
  `:247-248` `value.X = tile.wallFrameX(); value.Y = tile.wallFrameY() + Main.wallFrame[wall]*180;`
  dibujado en `(i*16 - 8, j*16 - 8)`. Es decir: **celdas de 36×36 en la hoja, de las que se pinta
  un rectángulo de 32×32**, y cada bloque de animación mide **180 px** de alto.
* ¿Qué celda es "pared rodeada de pared por los 4 lados"? `Framing.cs:400-409`: cuando los cuatro
  vecinos encajan, `style = 15` (más una de las 5 variantes de
  `centerWallFrameLookup[i%3][j%3]`), y `Framing.cs:135` declara
  `AddWallFrameLookup(15, 1,1, 2,1, 3,1, 2,5)` → las cuatro variantes están en las celdas de
  rejilla `(1,1)`, `(2,1)`, `(3,1)` y `(2,5)`.

**Decisión: el icono de una pared es un recorte de 32×32 en el píxel `(36, 36)`** — la celda
`(1,1)`, primera variante del estilo "rodeada por todos lados". Es literalmente el fotograma que
el juego usa en el interior macizo de una zona de pared.

Dimensiones reales medidas de los 366 `Wall_{id}.xnb` existentes:
`468×180` ×330, `468×1440` ×12, `468×1620` ×1, `468×360` ×5, `468×252` ×18.
Todos son de 468 px de ancho (13 columnas de 36) y de al menos 180 de alto (5 filas), así que
`(36,36)+32×32` **siempre cabe** — comprobado: 0 fuera de rango, 0 recortes vacíos.
El único ausente es `Wall_0.xnb` ("Sky", que es *no hay pared*: `WorldPresenceIndex` ya salta
`tile.Wall == 0`, `WorldPresenceIndex.cs:83`).

## A.5 Resultado real de la extracción (prototipo ya ejecutado)

| | ficheros | tamaño | tiempo |
|---|---|---|---|
| Tiles, base (`{id}.png`) | **749** | | |
| Tiles, variantes de cofre (`{id}_{u}_{v}.png`) | **129** | | |
| **Subtotal tiles** | **878** | **0,47 MB** | **834 ms** |
| Paredes (`{id}.png`) | **366** | **0,11 MB** | **536 ms** |
| **TOTAL NUEVO** | **1 244** | **0,58 MB** | **~1,4 s** |

Contexto: `TerrasavrNative.App/Assets/` ya tiene **11 417** PNG y **30 MB**. Esto añade un 11 %
de ficheros y un **2 %** de peso. El `.csproj` ya recoge cualquier PNG nuevo sin tocar nada:

```xml
<Content Include="Assets\**\*.png"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>
```
(`TerrasavrNative.App.csproj:11-13`)

## A.6 Script nuevo: `scripts/extraer-iconos-tiles.js`

Mismo patrón exacto que `scripts/extraer-iconos-vanilla.js` (mismo `require` cruzado de repos,
mismo `pngjs`, mismo `PNG.bitblt`, misma salida bajo `TerrasavrNative.App/Assets/`).

```js
// Encargo del usuario 4-sep-2026 ("faltan todos los sprites en exploracion de que es cada cosa
// como lo que es un cofre dorado de agua etc solo salen cuadrados de colores") - ver
// ESPEC-sprites-botones-badges.md#A.
//
// Extrae un icono real por TIPO de tile (frame base, 0,0) y ademas uno por VARIANTE de cofre/
// comoda (los 3 unicos tipos contenedores reales, TileID.Sets.BasicChest={21,467} y
// BasicDresser={88} - Terraria/ID/TileID.cs:359,367), que es la unica vista que usa el (u,v)
// real del .wld.
//
// GEOMETRIA REAL, no un rectangulo: el hueco de 2px entre celdas de la hoja de sprites esta
// TRANSPARENTE del todo (comprobado a nivel de pixel en Tiles_21.xnb: x=16,17 con alpha 0), asi
// que un recorte rectangular meteria una costura por el centro del objeto. El juego dibuja
// CELDA A CELDA (Terraria/GameContent/Drawing/TileDrawing.cs:1951, Rectangle(frameX,frameY,16,16)
// por cada tile de un objeto multi-casilla, con frameX avanzando textureGrid+frameGap) - esto
// compone lo mismo: bitblt de cada celda de textureGrid px, pegadas sin hueco.
//
// Uso: node scripts/extraer-iconos-tiles.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs, y que
//  xnb-lzx-tool-refs/tiles.json exista - esa carpeta esta gitignorada en el otro repo, si falta
//  hay que rebajar src/TEdit.Terraria/Data/tiles.json de github.com/TEdit/Terraria-Map-Editor)
// Salida: TerrasavrNative.App/Assets/vanilla/tile_icons/{id}.png  y  {id}_{u}_{v}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const TILES_JSON = path.join(__dirname, '..', '..', 'Terrasavr-Calamity-Beta', 'resources', 'app',
    'xnb-lzx-tool-refs', 'tiles.json');
const OUT_DIR = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'vanilla', 'tile_icons');

// Los 3 unicos tipos de tile que pueden ser el contenedor de un chest real del .wld
// (TileID.Sets.BasicChest = {21,467}, TileID.Sets.BasicDresser = {88}) - son los unicos para los
// que la UI pide un icono POR VARIANTE (u,v). Ampliar esta lista es la unica linea que hay que
// tocar si algun dia hiciera falta el arbol de variantes de Objetos->Tiles.
const TIPOS_CON_VARIANTE = [21, 88, 467];

const tiles = JSON.parse(fs.readFileSync(TILES_JSON, 'utf8'));
fs.mkdirSync(OUT_DIR, { recursive: true });

// Compone un icono limpio celda a celda. Devuelve null si el recorte no cabe en la hoja real.
function componer(src, u, v, celdasX, celdasY, textureGrid, frameGap) {
    const anchoNecesario = u + (celdasX - 1) * (textureGrid[0] + frameGap[0]) + textureGrid[0];
    const altoNecesario  = v + (celdasY - 1) * (textureGrid[1] + frameGap[1]) + textureGrid[1];
    if (anchoNecesario > src.width || altoNecesario > src.height) return null;
    const out = new PNG({ width: celdasX * textureGrid[0], height: celdasY * textureGrid[1] });
    for (let cy = 0; cy < celdasY; cy++) {
        for (let cx = 0; cx < celdasX; cx++) {
            PNG.bitblt(src.png, out,
                u + cx * (textureGrid[0] + frameGap[0]),
                v + cy * (textureGrid[1] + frameGap[1]),
                textureGrid[0], textureGrid[1],
                cx * textureGrid[0], cy * textureGrid[1]);
        }
    }
    return out;
}

// Respaldo real para el UNICO desajuste de metadatos de las 754 entradas: id=171 Christmas Tree
// dice frameGap [2,2] pero su Tiles_171.xnb real mide 64x128 = 4*16 x 8*16 exactos, sin relleno.
function icono(tile, src, u, v, tamCeldas) {
    const [cx, cy] = tamCeldas;
    return componer(src, u, v, cx, cy, tile.textureGrid, tile.frameGap)
        || componer(src, u, v, cx, cy, tile.textureGrid, [0, 0]);
}

const totalmenteTransparente = (png) => {
    for (let i = 3; i < png.data.length; i += 4) if (png.data[i] > 0) return false;
    return true;
};

let base = 0, variantes = 0, sinXnb = 0, sinRecorte = 0, transparentes = [];
for (const tile of tiles) {
    const xnbPath = path.join(STEAM_IMAGES, `Tiles_${tile.id}.xnb`);
    if (!fs.existsSync(xnbPath)) { sinXnb++; continue; }
    let src;
    try { src = xnbToPng(xnbPath); } catch (e) { console.log(`ERROR xnb id=${tile.id}: ${e.message}`); sinXnb++; continue; }

    // 1) icono base del TIPO: frame (0,0) con frameSize[0]. Deliberado, no el (u,v) real de una
    //    instancia del mundo: para un bloque no enmarcado ese (u,v) es un recorte de blending con
    //    los vecinos, no un icono limpio - y las vistas que usan este icono (Minerales,
    //    Objetos->Tiles) agrupan por Type a secas, sin variante.
    const iconoBase = icono(tile, src, 0, 0, tile.frameSize[0]);
    if (!iconoBase) { sinRecorte++; }
    else if (totalmenteTransparente(iconoBase)) { transparentes.push(`${tile.id}:${tile.name}`); }
    else { fs.writeFileSync(path.join(OUT_DIR, `${tile.id}.png`), PNG.sync.write(iconoBase)); base++; }

    // 2) variantes con nombre, solo para los tipos contenedores
    if (!TIPOS_CON_VARIANTE.includes(tile.id)) continue;
    for (const frame of (tile.frames || [])) {
        // 178 de los 9546 frames reales no traen 'uv' (frame unico, uv implicito en 0,0);
        // 317 traen su propio 'size' en celdas, que manda sobre frameSize del tile - eso resuelve
        // por si solo los 3 tiles con mas de un frameSize (165/185/233), sin adivinar nada.
        const uv = frame.uv || [0, 0];
        const tam = frame.size || tile.frameSize[0];
        const png = icono(tile, src, uv[0], uv[1], tam);
        if (!png || totalmenteTransparente(png)) continue;
        fs.writeFileSync(path.join(OUT_DIR, `${tile.id}_${uv[0]}_${uv[1]}.png`), PNG.sync.write(png));
        variantes++;
    }
}

console.log(`\nIconos de tile: ${base} base + ${variantes} variantes de cofre = ${base + variantes}`);
console.log(`  ${sinXnb} sin Tiles_{id}.xnb real, ${sinRecorte} sin recorte posible`);
console.log(`  ${transparentes.length} descartados por salir 100% transparentes: ${transparentes.join(', ')}`);
```

**Salida esperada exacta al ejecutarlo** (ya medida con el prototipo):

```
Iconos de tile: 749 base + 129 variantes de cofre = 878
  0 sin Tiles_{id}.xnb real, 0 sin recorte posible
  5 descartados por salir 100% transparentes: 373:Water Drip, 374:Lava Drip, 375:Honey Drip, 461:Sand Drip, 709:Magic Shimmer Dropper
```

Si esas cifras no salen **exactamente**, algo cambió (otra versión de Terraria, otro `tiles.json`)
y hay que investigarlo antes de comitear los PNG.

## A.7 Script nuevo: `scripts/extraer-iconos-paredes.js`

```js
// Gemelo de extraer-iconos-tiles.js para las paredes - ver ESPEC-sprites-botones-badges.md#A.4.
//
// walls.json de TEdit NO trae geometria (solo id/name/key/color/blendType/largeFrameType,
// comprobado sobre sus 367 entradas), asi que sale entera del codigo real del juego:
//   - Framing.cs:119   -> wallFrameSize = Point16(36, 36)  (celdas de 36x36 en la hoja)
//   - WallDrawing.cs:213 -> Rectangle(0,0,32,32)           (de cada celda se pinta 32x32)
//   - Framing.cs:400-409 + :135 -> con los 4 vecinos encajando, style=15, y
//     AddWallFrameLookup(15, 1,1, 2,1, 3,1, 2,5) pone sus variantes en las celdas (1,1)/(2,1)/
//     (3,1)/(2,5). Se coge la PRIMERA, celda (1,1) = pixel (36,36): es literalmente el fotograma
//     que el juego usa en el interior macizo de una zona de pared, el icono representativo obvio.
//
// Uso: node scripts/extraer-iconos-paredes.js
// Salida: TerrasavrNative.App/Assets/vanilla/wall_icons/{id}.png (32x32 cada uno)
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const WALLS_JSON = path.join(__dirname, '..', '..', 'Terrasavr-Calamity-Beta', 'resources', 'app',
    'xnb-lzx-tool-refs', 'walls.json');
const OUT_DIR = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'vanilla', 'wall_icons');

const FRAME = 36;   // wallFrameSize real
const DIBUJO = 32;  // lo que el juego pinta de cada celda
const CELDA_X = 1, CELDA_Y = 1; // estilo 15, primera variante

const walls = JSON.parse(fs.readFileSync(WALLS_JSON, 'utf8'));
fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0, sinXnb = [], fuera = [], vacias = [];
for (const wall of walls) {
    // id 0 = "Sky" = NO hay pared: no existe Wall_0.xnb y WorldPresenceIndex ya salta wall==0.
    const xnbPath = path.join(STEAM_IMAGES, `Wall_${wall.id}.xnb`);
    if (!fs.existsSync(xnbPath)) { sinXnb.push(`${wall.id}:${wall.name}`); continue; }
    let src;
    try { src = xnbToPng(xnbPath); } catch (e) { sinXnb.push(`${wall.id}:ERROR ${e.message}`); continue; }

    const x = CELDA_X * FRAME, y = CELDA_Y * FRAME;
    if (x + DIBUJO > src.width || y + DIBUJO > src.height) { fuera.push(`${wall.id}:${src.width}x${src.height}`); continue; }

    const out = new PNG({ width: DIBUJO, height: DIBUJO });
    PNG.bitblt(src.png, out, x, y, DIBUJO, DIBUJO, 0, 0);
    let opaco = false;
    for (let i = 3; i < out.data.length; i += 4) if (out.data[i] > 0) { opaco = true; break; }
    if (!opaco) { vacias.push(`${wall.id}:${wall.name}`); continue; }

    fs.writeFileSync(path.join(OUT_DIR, `${wall.id}.png`), PNG.sync.write(out));
    ok++;
}
console.log(`\nIconos de pared: ${ok} de ${walls.length}`);
console.log(`  sin Wall_{id}.xnb: ${sinXnb.join(', ') || '(ninguno)'}`);
console.log(`  fuera de rango: ${fuera.join(', ') || '(ninguno)'} | vacias: ${vacias.join(', ') || '(ninguna)'}`);
```

**Salida esperada exacta** (ya medida):

```
Iconos de pared: 366 de 367
  sin Wall_{id}.xnb: 0:Sky
  fuera de rango: (ninguno) | vacias: (ninguna)
```

## A.8 Clases C# nuevas — `TileIconResolver` y `WallIconResolver`

Misma forma exacta que `VanillaIconResolver.cs` (31 líneas) y `NpcIconResolver.cs` (18 líneas):
clase estática, directorio calculado una vez desde `AppContext.BaseDirectory`, `File.Exists`,
ruta `pack://siteoforigin:,,,/…` o `null`. Sin caché (mismo criterio que las dos existentes: un
inventario de categoría son 260 filas como mucho, medido y documentado en
`ExplorationViewModel.cs:389`).

**`TerrasavrNative.App/Services/TileIconResolver.cs`** (nuevo):

```csharp
using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de un TILE colocado - Assets/vanilla/tile_icons/, extraido de Images/Tiles_{id}.xnb
// real de la instalacion de Steam componiendo el objeto CELDA A CELDA (el hueco de 2px de la hoja
// de sprites es transparente puro, no edge bleed - un recorte rectangular meteria una costura por
// el centro; ver scripts/extraer-iconos-tiles.js y ESPEC-sprites-botones-badges.md#A.3).
//
// Dos formas de pedirlo, deliberadamente distintas:
//   - GetIconPath(tipo)      -> el frame base (0,0) del tipo. Lo que quieren Minerales y
//                               Objetos->Tiles, que agrupan por Type sin importar la variante.
//   - GetIconPath(tipo,u,v)  -> la variante EXACTA de sprite (u,v en pixeles reales del .wld,
//                               el mismo par que WorldPresenceIndex.ChestKindCounts). Solo hay
//                               variantes extraidas de los 3 tipos contenedores reales
//                               (21/467 cofres, 88 comodas) - cualquier otro (u,v) cae al icono
//                               base del tipo, y si tampoco lo hay, a null.
//
// null = no hay sprite real para ese id (tiles de mods, ids mas nuevos que el catalogo de TEdit,
// o los 5 tiles cuyo frame base sale 100% transparente: las 4 gotas y el gotero de centelleo).
// La UI cae entonces al cuadradito de color de la paleta real del mapa, como hasta ahora.
public static class TileIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "tile_icons");

    public static string? GetIconPath(int tileType)
    {
        string file = Path.Combine(IconsDir, $"{tileType}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/tile_icons/" + tileType + ".png" : null;
    }

    public static string? GetIconPath(int tileType, short u, short v)
    {
        string file = Path.Combine(IconsDir, $"{tileType}_{u}_{v}.png");
        return File.Exists(file)
            ? $"pack://siteoforigin:,,,/Assets/vanilla/tile_icons/{tileType}_{u}_{v}.png"
            : GetIconPath(tileType);
    }
}
```

**`TerrasavrNative.App/Services/WallIconResolver.cs`** (nuevo):

```csharp
using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de una PARED - Assets/vanilla/wall_icons/{id}.png, 32x32, recortado de
// Images/Wall_{id}.xnb real en el pixel (36,36) = celda (1,1) de la rejilla de 36x36 que declara
// Framing.wallFrameSize: la primera variante del estilo 15 ("rodeada de pared por los 4 lados",
// Framing.cs:135/400-409), literalmente el fotograma que el juego pinta en el interior macizo de
// una zona de pared. Ver scripts/extraer-iconos-paredes.js.
//
// Las paredes NO tienen variantes con nombre (walls.json de TEdit no trae ni textureGrid ni
// frames), asi que no hay sobrecarga con (u,v): una pared, un icono.
// null = pared de mod, o id mas nuevo que el catalogo (real: la 367 existe en Steam pero no en
// walls.json) - la UI cae al color, igual que ya hace hoy con su nombre.
public static class WallIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "wall_icons");

    public static string? GetIconPath(int wallId)
    {
        string file = Path.Combine(IconsDir, $"{wallId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/wall_icons/" + wallId + ".png" : null;
    }
}
```

## A.9 Cambios exactos en `ExplorationViewModel.cs`

**A.9.1 — Firma de la fila** (línea 70). Se añade `string? iconPath` **antes** de `swatchColor`
(el color se queda: es el respaldo real cuando no hay sprite, no se sustituye).

```csharp
// Fila de inventario generica - reutilizada por Cofres (las dos vistas), Minerales y Objetos
// (las tres vistas). NPCs sigue con su propio WorldNpcRowViewModel (ya existente, con icono real
// y estado de mapa) - un inventario generico no le aporta nada que no tenga ya.
//
// Encargo del usuario 4-sep-2026 ("faltan todos los sprites... solo salen cuadrados de colores"):
// IconPath es el sprite REAL de lo que representa la fila, resuelto por QUIEN construye la fila
// (cada vista sabe que significa su Id/U/V - ver la tabla de ESPEC-sprites-botones-badges.md#A.1;
// meter esa decision aqui dentro obligaria a esta clase a saber de que vista viene, que es justo
// lo que la hace reutilizable). SwatchColor NO desaparece: es el respaldo real de la plantilla
// cuando IconPath es null (tiles de mods, liquidos, NetId sin icono extraido).
public sealed partial class WorldInventoryRowViewModel(int id, short u, short v, string name, int count, int? veinCount, string? iconPath, Color swatchColor) : ObservableObject
{
    public int Id { get; } = id;
    public short U { get; } = u;
    public short V { get; } = v;
    public string Name { get; } = name;
    public int Count { get; } = count;
    public int? VeinCount { get; } = veinCount;
    public string? IconPath { get; } = iconPath;
    public Color SwatchColor { get; } = swatchColor;
    …
```

**A.9.2 — Los seis puntos de llamada.** Antes → después, literal:

```csharp
// :307  Cofres -> "Por tipo de cofre": (Type,U,V) es la VARIANTE real de sprite del tile en
//       chest.X/Y, el unico sitio de la app donde el (u,v) del .wld manda de verdad.
Inventory.Add(new WorldInventoryRowViewModel(type, u, v, _tileNames.TileVariantName(type, u, v), count, null,
    TileIconResolver.GetIconPath(type, u, v), ToWpfColor(_mapColors.TileColor(type))));

// :321  Cofres -> "Por lo que contienen": el Id es un NetId REAL de objeto del .wld, NO un id
//       sintetico de esta app - GameItem.IsCalamity (Id >= 20.000.000) nunca es cierto aqui, asi
//       que NO hay rama de Calamity que valga: un objeto modeado dentro de un cofre trae el id de
//       runtime que tModLoader le asigno, que no se puede traducir (mismo motivo por el que
//       _itemNames.GetName ya cae en "Item #N", ver el comentario de :158). VanillaIconResolver
//       devuelve null para todos ellos -> cuadradito de color, igual que hoy.
Inventory.Add(new WorldInventoryRowViewModel(netId, 0, 0, _itemNames.GetName(netId), count, null,
    VanillaIconResolver.GetIconPath(netId), Colors.Transparent));

// :350  Minerales: agrupa por Type, sin variante -> icono base.
target.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, veinCount,
    TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id))));

// :373  Objetos -> Tiles: idem, icono base del tipo.
Inventory.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, null,
    TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id))));

// :377  Objetos -> Paredes.
Inventory.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.WallName(id), count, null,
    WallIconResolver.GetIconPath(id), ToWpfColor(_mapColors.WallColor(id))));

// :381  Objetos -> Liquidos: sin icono a proposito (ver ESPEC-sprites-botones-badges.md#A.11).
Inventory.Add(new WorldInventoryRowViewModel(code, 0, 0, WorldSearch.LiquidName(code), count, null,
    null, Colors.Transparent));
```

`using TerrasavrNative.App.Services;` ya está en la cabecera del fichero
(`ExplorationViewModel.cs:11`), no hace falta añadir nada.

**A.9.3 — El bug latente que esto destapa (arreglarlo de paso).** Hoy la vista *"Por lo que
contienen"* y la de *Líquidos* pasan `Colors.Transparent` como swatch: el `Border` de 14×14 se
pinta transparente pero **con borde**, o sea un cuadrito vacío. Con el sprite real esas dos vistas
mejoran solas (la primera tendrá icono de objeto; la segunda es el único caso en que conviene
esconder el hueco del todo). Ver A.10: la plantilla nueva sólo pinta el `Border` de color si
`SwatchColor` **no** es transparente **y** no hay icono — con lo que Líquidos pasa a no reservar
un cuadrito vacío. Eso no cambia ningún binding ni comando.

## A.10 Cambio exacto en `InventoryRowTemplate` (`MainWindow.xaml:1222-1247`)

Reemplaza **solo** el bloque del `Border` de color por una caja fija con las dos capas. El
`CheckBox`, el `Button`/`RowClickButton`, el `Command`/`CommandParameter` y el binding de
`IsMatch` **no se tocan** (ese es el gotcha real de captura de ratón de WPF que ya documenta el
comentario del propio `DataTemplate`, `MainWindow.xaml:1218-1221`).

```xml
<StackPanel Orientation="Horizontal">
    <!-- Encargo del usuario 4-sep-2026 ("quiero todos los sprites de todos los objetos del
         mundo en el buscador de exploracion... un cofre dorado de agua etc") - caja de tamaño
         FIJO (26x26) con dos capas excluyentes, para que todas las filas alineen su texto en la
         misma columna aunque los sprites reales midan de 16x16 a 96x64:
           - Sprite real, si lo hay (TileIconResolver/WallIconResolver/VanillaIconResolver).
             StretchDirection="DownOnly" es a proposito: un sprite de 16x16 se pinta a 1:1
             (nitido, sin el escalado x1.625 irregular que da un NearestNeighbor a 26px) y solo
             los grandes de verdad se encogen para caber.
           - El cuadradito de color de siempre (paleta real del MAPA, TEdit) como respaldo cuando
             no hay sprite: tiles/paredes de mods, ids mas nuevos que el catalogo, NetId de
             objeto modeado. Nunca se pierde informacion, solo mejora cuando se puede. -->
    <Grid Width="26" Height="26" Margin="0,0,7,0" VerticalAlignment="Center">
        <Image Source="{Binding IconPath}" Stretch="Uniform" StretchDirection="DownOnly"
               RenderOptions.BitmapScalingMode="NearestNeighbor"
               HorizontalAlignment="Center" VerticalAlignment="Center"
               Visibility="{Binding IconPath, Converter={StaticResource NullToVis}}" />
        <Border Width="14" Height="14" CornerRadius="2"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                BorderBrush="{StaticResource BorderBrush0}" BorderThickness="1"
                Visibility="{Binding IconPath, Converter={StaticResource NullToCollapsed}}">
            <Border.Background>
                <SolidColorBrush Color="{Binding SwatchColor}" />
            </Border.Background>
        </Border>
    </Grid>
    <StackPanel VerticalAlignment="Center">
        <TextBlock Text="{Binding Name}" Style="{StaticResource BodyText}" TextTrimming="CharacterEllipsis" />
        <TextBlock Text="{Binding CountLabel}" Style="{StaticResource CaptionText}" />
    </StackPanel>
</StackPanel>
```

Los dos convertidores ya existen y ya están registrados en `App.xaml:12-13`:
`NullToVis` (no-nulo → `Visible`) y `NullToCollapsed` (nulo → `Visible`). No hay que crear nada.

Detalle real: `<TextBlock>` dentro de `<StackPanel>` sin `VerticalAlignment` quedaba pegado
arriba cuando el icono era de 14 px; con la caja de 26 px hay que añadir
`VerticalAlignment="Center"` al `StackPanel` del texto (arriba ya está puesto), o el texto sube.

## A.11 Alcance deliberado de la parte A

* **Líquidos se quedan sin icono, a propósito.** Los códigos 1-4 (agua/lava/miel/centelleo) no
  son tiles ni paredes: el juego los dibuja con un shader sobre una máscara
  (`Terraria.GameContent.Liquid.LiquidMask.fxc`, un recurso incrustado real de `tModLoader.dll`),
  no con un sprite recortable. Un icono inventado ahí sería peor que el color, que además sale de
  la paleta real del mapa.
* **Variantes de sprite solo para los 3 tipos contenedores.** Extraer los **9 368** frames de los
  412 tiles enmarcados es perfectamente posible con el mismo script (una línea:
  `TIPOS_CON_VARIANTE`), pero hoy **ninguna vista consume un `(u,v)` que no venga de
  `ChestKindCounts`** — serían ~9 400 ficheros más (de 11 417 a ~21 000 en `Assets/`) sin un solo
  sitio donde verse. Si algún día se implementa el árbol de dos niveles por variante que propone
  `ESPEC-ui-exploracion.md#9.3-E` (deliberadamente fuera de alcance también allí,
  `ExplorationViewModel.cs:359-364`), esa lista es lo único que hay que ampliar.
* **Los iconos NO se redimensionan al extraerlos.** Se guardan a tamaño nativo (16×16 a 96×64) y
  la UI los encaja. Redimensionar pixel-art en el extractor perdería detalle irrecuperable, y
  `StretchDirection="DownOnly"` da el mejor resultado posible para el caso mayoritario (16 y 32 px).
* **No se toca `MapColorCatalog` ni `TileNameCatalog`.** Los nombres y colores ya son correctos.

---

# PARTE B — Los botones de la barra lateral de Exploración

## B.1 Estado actual

Dos estilos en `TerrasavrNative.App/Styles/Theme.xaml`:

* `CategoryPill` (línea **1247**, `TargetType="RadioButton"`): `Border CornerRadius="99"`, fondo
  `BgElevatedBrush`, seleccionado `AccentBrush` + texto blanco. `FontSize 12`, `Padding 10,4`.
* `CategoryChip` (línea **1288**, `TargetType="ToggleButton"`): idéntico pero seleccionado
  `TealBrush`. `FontSize 11.5`, `Padding 9,4`. **Sin** trigger de `IsEnabled=False`.

Los **14** puntos de uso reales en `MainWindow.xaml` (números de línea de HOY; se moverán en
cuanto entren los cambios de A y C, así que lo fiable es buscar por el nombre del estilo):

| Líneas | Control | Estilo | Qué es |
|---|---|---|---|
| 3523, 3528, 3533, 3538, 3543 | `RadioButton` | `CategoryPill` | Todo / NPCs / Cofres / Minerales / Objetos |
| 3661, 3662, 3663 | `ToggleButton` | `CategoryChip` | Con casa / Sin casa / Bajo tierra |
| 3739, 3741 | `RadioButton` | `CategoryChip` | Por tipo de cofre / Por lo que contienen |
| 3802, 3804, 3806 | `RadioButton` | `CategoryChip` | Tiles / Paredes / Líquidos |

**Detalle real que hay que preservar:** `CategoryChip` tiene `TargetType="ToggleButton"` y se
aplica también a `RadioButton` (líneas 3739-3806). Esto funciona porque `RadioButton` **deriva**
de `ToggleButton` y WPF acepta un `Style` cuyo `TargetType` sea una clase base. Está en
producción; no es un accidente y no hay que "arreglarlo".

## B.2 La familia visual real que hay que replicar

Estilo base implícito `<Style TargetType="Button">` (`Theme.xaml:249`, leído entero). Lo que
define la familia:

* **Forma: `CornerRadius="8"`.** El comentario de `Theme.xaml:257-269` documenta una corrección
  real del usuario: un intento anterior puso `999` para forzar píldora en todos los botones y
  eso **le cambió la forma al propio "Auto-equipar"**, el botón de referencia. Cita literal del
  comentario: *"La forma real que le gustaba siempre fue esta (esquinas redondeadas normales, NO
  pildora completa) - lo que hacia 'moderno' a Auto-equipar frente a los botones planos era el
  degradado/elevacion/animacion (Tag), nunca la forma."*
  **Conclusión directa para este encargo: los selectores tienen que ser `CornerRadius="8"`, no
  píldoras.** Eso es literalmente lo que pide el usuario ("por algo que realmente vaya con la
  estética como el botón de cargar personaje").
* **Animación real de fondo** por `Trigger`/`Storyboard` sobre un `SolidColorBrush` **propio de
  cada instancia** (`x:Name="BdBrush"`), nunca sobre el recurso compartido (animarlo "sangraría"
  a todos los botones). 0,12 s al entrar, 0,15 s al salir, 0,05/0,08 s en `IsPressed`.
* **Jerarquía por `Tag`**, ya documentada en `Theme.xaml:334-354`:
  `Accent` (degradado violeta + eleva 2 px, LA acción principal del panel),
  `Teal`/`Pink`/`Orange` (lo mismo en otro tono),
  `AccentSoft` (`AccentMutedBrush` + `AccentHoverBrush`, *"de la familia del acento pero
  secundario"*), `Ghost` (transparente + `TextSecondaryBrush`, destructivo/secundario).
* **La elevación de 2 px está reservada** a las variantes de color sólido, *"asi se reserva el
  gesto mas llamativo para las acciones importantes"* (`Theme.xaml:365-370`).

## B.3 Diseño de los selectores nuevos

Los selectores **no son acciones**: son estado. Por eso:

| | No seleccionado | Al pasar el ratón | Seleccionado | Seleccionado + ratón |
|---|---|---|---|---|
| Forma | `CornerRadius="8"` | = | = | = |
| Fondo | `BgSecondaryColor` (#171a26, **más hundido** que el panel) | anima a `BgHoverColor` | `AccentMutedBrush` (#2a2856) | = |
| Borde | 1 px `BorderStrongBrush` (#3d4460) | 1 px `AccentBrush` | 1 px `AccentBrush` | 1 px `AccentHoverBrush` |
| Texto | `TextSecondaryBrush` | `TextPrimaryBrush` | `TextPrimaryBrush` **SemiBold** | = |
| Elevación | ninguna, nunca | ninguna | ninguna | ninguna |

Por qué esto y no otra cosa:

1. **Contorno en reposo, relleno al marcar** es la diferencia legible de un solo vistazo entre
   "selector" y "acción": los botones principales **siempre** están rellenos, estos solo cuando
   están puestos. Es exactamente lo que pide el usuario ("que se diferencien un poco de botones
   principales claro").
2. **Reutiliza `AccentMutedBrush`/`AccentBrush`, que ya significan "familia del acento pero
   secundario"** en este tema (`PrefixGroupButton` y `Tag="AccentSoft"`). No se inventa ningún
   color nuevo.
3. **`BorderThickness` es 1 px SIEMPRE**, en los cuatro estados: solo cambia el color. Cambiarlo
   a 2 px al marcar movería el texto medio píxel y haría "saltar" la fila entera del `WrapPanel`.
4. **Texto seleccionado en `TextPrimaryBrush`, no en `AccentHoverBrush`.** Es la única desviación
   consciente respecto a la pareja `AccentSoft`: ese violeta-sobre-violeta está calibrado para una
   etiqueta de botón a 12,5 px SemiBold; en una fila densa a 11,5 px se lee bastante peor. La
   señal de "seleccionado" la dan el fondo y el borde, que son inequívocos.
5. **Sin elevación de 2 px**, respetando la regla que el propio tema ya escribió.
6. **Misma animación de 0,12/0,15 s** que el botón base → se siente de la misma familia.

**Sobre los nombres.** `CategoryPill` pasaría a mentir sobre su forma en cuanto deje de ser una
píldora, y este proyecto documenta ese tipo de cosas con cuidado. Renombrar:

* `CategoryPill` → **`CategorySelector`** (`RadioButton`, la fila de 5 categorías).
* `CategoryChip` → **`ViewSelector`** (`ToggleButton`; sirve igual a los `RadioButton` de vista).

Son 14 sustituciones mecánicas de `{StaticResource CategoryPill}` / `{StaticResource CategoryChip}`.

**Diferencia entre los dos:** solo métrica. `CategorySelector` es la fila principal
(`FontSize 12`, `Padding 11,5`); `ViewSelector` es el sub-nivel (`FontSize 11`, `Padding 9,4`).
Misma paleta y misma plantilla — así se lee la jerarquía sin introducir un tercer color.

## B.4 XAML completo de los dos estilos nuevos

Sustituye íntegramente los bloques de `Theme.xaml:1243-1315` (desde el comentario de
`CategoryPill` hasta el cierre de `CategoryChip`, justo antes de `</ResourceDictionary>`).

```xml
    <!-- Encargo del usuario 4-sep-2026: "quiero que cambies todos los botones de exploracion
         esos ovalados por algo que realmente vaya con la estetica como el boton de cargar
         personaje o el de cargar mundo pero que se diferencien un poco de botones principales
         claro" - ver ESPEC-sprites-botones-badges.md#B.
         Antes eran CategoryPill/CategoryChip, dos plantillas con CornerRadius="99" (pildora
         completa) que no se parecian a NINGUN otro boton de la app. La forma correcta es la
         MISMA que el estilo base de Button (CornerRadius="8"): el propio Theme.xaml ya documenta
         (ver el comentario de la plantilla de Button) que el usuario corrigio a mano un intento
         anterior de convertir todos los botones en pildoras, porque eso le cambiaba la forma al
         propio "Auto-equipar", que era el boton de referencia que queria replicar.
         Lo que los DIFERENCIA de un boton de accion es que no van rellenos: contorno en reposo,
         relleno de acento suave (AccentMutedBrush, la misma pareja que Tag="AccentSoft" ya usa
         para "de la familia del acento pero secundario") solo cuando estan marcados. Y NUNCA
         llevan la elevacion de 2px, que este tema reserva a las acciones principales.
         El BorderThickness es 1 en los CUATRO estados y solo cambia de color: subirlo a 2 al
         marcar movería el texto y haria "saltar" la fila entera del WrapPanel. -->
    <Style x:Key="CategorySelector" TargetType="RadioButton">
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}" />
        <Setter Property="FontFamily" Value="{StaticResource AppFont}" />
        <Setter Property="FontSize" Value="12" />
        <Setter Property="Padding" Value="11,5" />
        <Setter Property="Margin" Value="0,0,6,6" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="RadioButton">
                    <Border x:Name="Bd" CornerRadius="8" BorderThickness="1"
                            BorderBrush="{StaticResource BorderStrongBrush}"
                            Padding="{TemplateBinding Padding}">
                        <!-- SolidColorBrush propio de la instancia, igual que la plantilla de
                             Button: animar un StaticResource compartido sangraria a todos. -->
                        <Border.Background>
                            <SolidColorBrush x:Name="BdBrush" Color="{StaticResource BgSecondaryColor}" />
                        </Border.Background>
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
                            <Trigger.EnterActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <ColorAnimation Storyboard.TargetName="BdBrush" Storyboard.TargetProperty="Color"
                                                        To="{StaticResource BgHoverColor}" Duration="0:0:0.12" />
                                    </Storyboard>
                                </BeginStoryboard>
                            </Trigger.EnterActions>
                            <Trigger.ExitActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <ColorAnimation Storyboard.TargetName="BdBrush" Storyboard.TargetProperty="Color"
                                                        To="{StaticResource BgSecondaryColor}" Duration="0:0:0.15" />
                                    </Storyboard>
                                </BeginStoryboard>
                            </Trigger.ExitActions>
                        </Trigger>
                        <!-- IsChecked va DESPUES de IsMouseOver a proposito: al fijar
                             Bd.Background se sustituye el pincel entero (incluido el animado),
                             mismo mecanismo real que ya usa Tag="Accent" en la plantilla de
                             Button. Al desmarcar se recupera solo el pincel animado. -->
                        <Trigger Property="IsChecked" Value="True">
                            <Setter TargetName="Bd" Property="Background" Value="{StaticResource AccentMutedBrush}" />
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
                            <Setter Property="FontWeight" Value="SemiBold" />
                        </Trigger>
                        <MultiTrigger>
                            <MultiTrigger.Conditions>
                                <Condition Property="IsMouseOver" Value="True" />
                                <Condition Property="IsChecked" Value="True" />
                            </MultiTrigger.Conditions>
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentHoverBrush}" />
                        </MultiTrigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="Bd" Property="Opacity" Value="0.4" />
                            <Setter Property="Cursor" Value="Arrow" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Sub-nivel del anterior: MISMA paleta y MISMA plantilla, solo mas compacto (asi la
         jerarquia se lee por tamaño, sin meter un tercer color). TargetType="ToggleButton" a
         proposito, no RadioButton: se aplica igual a los ToggleButton de multiseleccion (los 3
         chips de NPCs) y a los RadioButton de vista (Cofres, Objetos), porque RadioButton deriva
         de ToggleButton y WPF acepta un Style cuyo TargetType sea una clase base - ya estaba asi
         en produccion con el CategoryChip anterior, no es un accidente. -->
    <Style x:Key="ViewSelector" TargetType="ToggleButton">
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}" />
        <Setter Property="FontFamily" Value="{StaticResource AppFont}" />
        <Setter Property="FontSize" Value="11" />
        <Setter Property="Padding" Value="9,4" />
        <Setter Property="Margin" Value="0,0,6,0" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ToggleButton">
                    <Border x:Name="Bd" CornerRadius="8" BorderThickness="1"
                            BorderBrush="{StaticResource BorderStrongBrush}"
                            Padding="{TemplateBinding Padding}">
                        <Border.Background>
                            <SolidColorBrush x:Name="BdBrush" Color="{StaticResource BgSecondaryColor}" />
                        </Border.Background>
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
                            <Trigger.EnterActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <ColorAnimation Storyboard.TargetName="BdBrush" Storyboard.TargetProperty="Color"
                                                        To="{StaticResource BgHoverColor}" Duration="0:0:0.12" />
                                    </Storyboard>
                                </BeginStoryboard>
                            </Trigger.EnterActions>
                            <Trigger.ExitActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <ColorAnimation Storyboard.TargetName="BdBrush" Storyboard.TargetProperty="Color"
                                                        To="{StaticResource BgSecondaryColor}" Duration="0:0:0.15" />
                                    </Storyboard>
                                </BeginStoryboard>
                            </Trigger.ExitActions>
                        </Trigger>
                        <Trigger Property="IsChecked" Value="True">
                            <Setter TargetName="Bd" Property="Background" Value="{StaticResource AccentMutedBrush}" />
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                            <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
                            <Setter Property="FontWeight" Value="SemiBold" />
                        </Trigger>
                        <MultiTrigger>
                            <MultiTrigger.Conditions>
                                <Condition Property="IsMouseOver" Value="True" />
                                <Condition Property="IsChecked" Value="True" />
                            </MultiTrigger.Conditions>
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{StaticResource AccentHoverBrush}" />
                        </MultiTrigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="Bd" Property="Opacity" Value="0.4" />
                            <Setter Property="Cursor" Value="Arrow" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
```

## B.5 Lo que NO cambia (importante)

* **Ni un solo binding, comando, `GroupName` ni tipo de control.** `RadioButton` +
  `GroupName="ExploracionCategoria"` sigue dando la exclusión mutua nativa de WPF; el
  `IsChecked` de doble vía con `EnumEqualsConverter`
  (`Converters/VisibilityConverters.cs:123`, con su `Binding.DoNothing` real en `ConvertBack`)
  sigue igual; los `ToggleButton` de NPCs siguen siendo multiselección independiente.
* **El `TextBlock` hijo en vez de `Content="{Binding …, StringFormat=…}"`** de las 5 píldoras de
  categoría **se queda como está**. `MainWindow.xaml:3514-3521` documenta un bug real encontrado
  con el arnés UIA: puesto como atributo, el `StringFormat` se perdía en runtime y el `Name` de
  automatización salía `"14"` en vez de `"NPCs (14)"`. El `Foreground` del `Style` llega igual al
  `TextBlock` por herencia de `TextElement.Foreground` a través del `ContentPresenter` (es lo que
  ya hace hoy, por eso las píldoras cambian de color al marcarse).
* **`ToolTipService.ShowOnDisabled="True"` y `IsEnabled="{Binding Exploration.IsWorldLoaded}"`**
  siguen en los puntos de uso, sin tocar. Con el nuevo `Trigger IsEnabled=False` la opacidad pasa
  de 0,5 a 0,4, igual que el botón base.
* **`Tag="Ghost"` de "Buscar seleccionados"** (`MainWindow.xaml:3744`, `:3809`) y `Tag="Accent"`
  de "Marcar en el mapa" (`:3766`) **no se tocan**: son acciones reales, ya están en la jerarquía
  correcta y son justo el contraste que hace legible el diseño nuevo (acción rellena vs selector
  contorneado, uno al lado del otro).

---

# PARTE C — Las insignias de Inicio

## C.1 El bug real, confirmado

`TerrasavrNative.App/ViewModels/HomeViewModel.cs:164`:

```csharp
bool isCalamity = File.Exists(Path.ChangeExtension(path, ".tplr"));
```

Eso no comprueba Calamity: comprueba **que exista un `.tplr` hermano**, o sea "este personaje es
de tModLoader". La variable se llama `isCalamity`, viaja así al constructor
(`CharacterListEntryViewModel.cs:30`) y acaba encendiendo una insignia roja que dice "Calamity"
(`MainWindow.xaml:1162-1165`). Un personaje con **cualquier** mod, o con un `.tplr` huérfano de
una partida vieja, sale marcado como Calamity.

Ese es el único uso de `CharacterListEntryViewModel.IsCalamity` en todo el XAML —comprobado: los
otros 7 `IsCalamity` de `MainWindow.xaml` (líneas 209, 459, 559, 817, 1004, 1055, 2567)
pertenecen a `ItemSlotViewModel`/etc. del editor ya cargado y **no se tocan**.

## C.2 Formato real del `.tplr` (verificado sobre los 4 de esta máquina)

`TplrFile.Read` = `gunzip` + NBT big-endian (`TerrasavrNative.Core/Nbt/TplrFile.cs`). Volcado real:

```
tModLoader\Players\Eldelgas.tplr   1 813 B gz /  23 379 B crudos
  claves: armor(10) loadouts{} inventory(58) miscEquips(5) bank(31) bank2(17) hairDye
          modData(3) modBuffs(14) infoDisplays(0) builderToggles(1) usedMods hair
  mods vistos en entradas: Terraria×132, CalamityMod×4, ModLoader×2
  usedMods = ["CalamityModMusic","CalamityMod","CalamityModEsp","HEROsMod","TModLoaderMod"]

tModLoader\Players\adrian.tplr    22 825 B gz / 183 572 B crudos
  ...research(2709)...  mods: CalamityMod×2658, Terraria×29, CalamityModMusic×65, ModLoader×2
  usedMods = ["CalamityModMusic","CalamityMod","CalamityModEsp","HEROsMod","TModLoaderMod"]

tModLoader\Players\prueba.tplr        94 B gz /      84 B crudos
  claves: inventory(1)          ← escrito por el PROPIO Terrakeep, no por tModLoader
  mods: CalamityMod×1 ;  usedMods AUSENTE

Players\adrian.tplr                1 207 B gz /   7 660 B crudos
  usedMods = ["CalamityModMusic","CalamityMod","CalamityModEsp"]
```

### C.2.1 Hallazgo: la clave `usedMods` es real y gratis

`Terraria/ModLoader/IO/PlayerIO.cs:67` la escribe en cada guardado de tModLoader, y
`PlayerIO.cs:414-417` dice qué es exactamente:

```csharp
internal static List<string> SaveUsedMods(Player player)
{
    return ModLoader.Mods.Select((Mod m) => m.Name).Except(new string[1] { "ModLoader" }).ToList();
}
```

O sea: **la lista real de mods cargados en el momento de guardar**, sin "ModLoader". Es
literalmente el dato que responde a *"que son personajes verdaderamente de tmodloader"*, y ya está
en el fichero.

**Cuidado con qué NO es:** es "qué mods estaban puestos", no "qué mods tienen contenido dentro".
Y `prueba.tplr` demuestra el caso contrario: un `.tplr` escrito por el propio Terrakeep (que solo
reescribe los 7 contenedores + `modBuffs` + `loadouts`, ver `CalamityCharacterSync.MaskAndSyncAll`)
**no tiene `usedMods` en absoluto** y sin embargo sí tiene un objeto real de Calamity dentro.
Conclusión: `usedMods` sirve para **informar**, no para decidir la insignia de Calamity.

## C.3 Cómo detectar Calamity de verdad, y cuánto cuesta (medido)

La regla que ya usa la app en el único sitio donde importaba hasta ahora
(`CharacterFileService.Save`, línea ~240) es:

```csharp
bool tieneContenidoRealDeCalamity =
    loaded.MergedContainers.Values.Any(items => items.Any(i => i.IsCalamity))
    || loaded.Character.Buffs.Any(b => b.Id >= CalamityIds.BuffIdBase);
```

Eso exige un `Load` completo (leer el `.plr`, leer el `.tplr`, `MergeAll` con los catálogos).
**No hace falta.** El equivalente exacto a nivel de NBT crudo es: *"¿hay alguna entrada, en alguna
de las listas de contenedor o en `modBuffs`, cuya clave `mod` sea un mod de Calamity?"* — porque
así es como `CalamityItemCodec.Decode`/`MergeBuffs` deciden que algo es de Calamity
(`CalamityItemCodec.cs:22-28`, `CalamityCharacterSync.cs:132-138`), traduciendo `(mod, name)` a
un id sintético vía catálogo. Los mods reales del catálogo son exactamente dos, contados sobre
`Assets/calamity/catalog.json`: **`CalamityMod` (2647 entradas)** y **`CalamityModMusic` (62)**.

Las claves a barrer son las 7 planas de `CalamityCharacterSync.FlatContainers` (`inventory`,
`bank`, `bank2`, `bank3`, `bank4`, `miscEquips`, `miscDyes`), más `armor`/`dye` (loadout activo),
más cada lista dentro del compound `loadouts`, más `modBuffs`.

### Medición real (arnés C#, `dotnet run -c Release`, carpeta real del usuario)

```
.plr reales encontrados: 6
adrian.plr       plr=0,54ms   tplr+deteccion=2,10ms   tplr=si   calamityReal=True
Eldelgas.plr     plr=0,09ms   tplr+deteccion=0,27ms   tplr=si   calamityReal=True
prueba.plr       plr=0,10ms   tplr+deteccion=0,04ms   tplr=si   calamityReal=True
adrian.plr       plr=0,52ms   tplr+deteccion=0,11ms   tplr=si   calamityReal=True
Eldelgas.plr     plr=0,06ms   tplr+deteccion=0,00ms   tplr=NO   calamityReal=False
Terrariano.plr   plr=0,06ms   tplr+deteccion=0,00ms   tplr=NO   calamityReal=False

TOTAL .plr = 1,36 ms | TOTAL .tplr+deteccion = 2,53 ms
```

**2,5 ms para toda la carpeta**, incluido el `.tplr` de 183 KB crudos con 2 709 entradas de
research. Y encima corre **dentro del `Task.Run` que `HomeViewModel.RefreshAsync` ya usa**
(`HomeViewModel.cs:130`), fuera del hilo de UI, con el spinner `IsScanning` ya enganchado. A 100
personajes serían ~40 ms de fondo. **No hay ninguna razón para optimizar esto**, ni para
inventar un atajo de "buscar la cadena CalamityMod en los bytes descomprimidos".

Nota real de esta máquina: los 4 personajes con `.tplr` tienen los 4 contenido real de Calamity,
así que **el caso "tModLoader sin Calamity" no existe hoy aquí** — hay que fabricarlo a mano para
verificarlo (ver D.3).

## C.4 Clase nueva: `TerrasavrNative.Core/Calamity/TplrModSummary.cs`

Va en **Core**, no en App: es lógica pura de formato, sin dependencias de WPF, y así se puede
probar desde `TerrasavrNative.Core.Tests` (que ya tiene una carpeta `Calamity/` con
`CalamityCharacterSyncRealFileTests.cs`, el patrón exacto de "probar contra un fichero real").

```csharp
using TerrasavrNative.Core.Nbt;

namespace TerrasavrNative.Core.Calamity;

// Lo que se puede saber de un personaje mirando SOLO su .tplr, sin cargarlo entero.
//
// Encargo del usuario 4-sep-2026 ("en el inicio los personajes que tienen mod solo marcan
// calamity... que haga referencia a que son personajes verdaderamente de tmodloader que es lo
// principal... al igual que cuando un personaje es vanilla que tenga dicha etiqueta") - ver
// ESPEC-sprites-botones-badges.md#C.
//
// UsedMods es una clave REAL que escribe el propio tModLoader en cada guardado
// (Terraria/ModLoader/IO/PlayerIO.cs:67 -> SaveUsedMods = ModLoader.Mods.Select(m => m.Name)
// menos "ModLoader"), o sea la lista de mods CARGADOS al guardar. Es un dato informativo
// excelente ("Mods usados: ...") pero NO sirve para decidir si hay Calamity dentro: un .tplr
// escrito por el propio Terrakeep (MaskAndSyncAll solo reescribe los contenedores + modBuffs +
// loadouts) no lo tiene, y sin embargo puede tener objetos de Calamity de verdad - caso real
// comprobado con prueba.tplr de esta maquina (94 bytes, sin usedMods, con 1 objeto de Calamity).
public sealed class TplrModSummary
{
    // Hay al menos un OBJETO o BUFF de Calamity real dentro. Mismo criterio exacto que usa
    // CharacterFileService.Save para decidir si merece la pena escribir un .tplr, pero resuelto
    // sobre el NBT crudo en vez de sobre el personaje ya fusionado.
    public required bool HasCalamityContent { get; init; }
    // Lista real de usedMods (vacia si el .tplr no la trae).
    public required IReadOnlyList<string> UsedMods { get; init; }
}

public static class TplrProbe
{
    // Los 2 unicos mods reales del catalogo de Calamity de esta app (contados sobre
    // Assets/calamity/catalog.json: CalamityMod 2647 entradas, CalamityModMusic 62) - los mismos
    // que CalamityItemCodec.Decode puede traducir a un id sintetico. "CalamityModEsp" (la
    // traduccion) aparece en usedMods pero no aporta ningun objeto, por eso no esta aqui.
    private static readonly string[] ModsDeCalamity = ["CalamityMod", "CalamityModMusic"];

    // Las listas de entradas de objeto/buff del .tplr. Las 7 primeras son exactamente
    // CalamityCharacterSync.FlatContainers; armor/dye son las claves planas del loadout activo;
    // modBuffs son los buffs. Deliberadamente NO se miran "research" ni "modData": el criterio
    // tiene que significar LO MISMO que el de CharacterFileService.Save ("tiene un objeto o un
    // buff de Calamity"), no algo parecido pero distinto - si algun dia se amplia, hay que
    // ampliar los dos a la vez.
    private static readonly string[] ListasDeEntradas =
        ["inventory", "bank", "bank2", "bank3", "bank4", "miscEquips", "miscDyes", "armor", "dye", "modBuffs"];

    public static bool EsModDeCalamity(string? mod) => mod != null && Array.IndexOf(ModsDeCalamity, mod) >= 0;

    // Un .tplr ilegible (corrupto, de otro formato, bloqueado) NO debe tumbar el listado de
    // Inicio - mismo criterio que HomeViewModel.ScanCharacters ya aplica a los .plr ajenos.
    public static TplrModSummary? TryRead(string tplrPath)
    {
        try
        {
            var (_, root) = TplrFile.Read(File.ReadAllBytes(tplrPath));
            return From(root);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static TplrModSummary From(NbtCompound root) => new()
    {
        HasCalamityContent = TieneContenidoDeCalamity(root),
        UsedMods = LeerUsedMods(root),
    };

    private static bool TieneContenidoDeCalamity(NbtCompound root)
    {
        foreach (string clave in ListasDeEntradas)
            if (ListaTieneCalamity(root.Get(clave))) return true;

        // Las claves por loadout ("loadout{i}Armor"/"loadout{i}Dye", ver
        // CalamityCharacterSync.MergeLoadoutArmorDye) viven dentro del compound "loadouts".
        if (root.Get("loadouts") is NbtCompound loadouts)
            foreach (var (_, tag) in loadouts.Fields)
                if (ListaTieneCalamity(tag)) return true;

        return false;
    }

    private static bool ListaTieneCalamity(NbtTag? tag)
    {
        if (tag is not NbtList list) return false;
        foreach (var item in list.Items)
            if (item is NbtCompound entry && EsModDeCalamity((entry.Get("mod") as NbtString)?.Value))
                return true;
        return false;
    }

    private static IReadOnlyList<string> LeerUsedMods(NbtCompound root)
    {
        if (root.Get("usedMods") is not NbtList list) return [];
        var mods = new List<string>();
        foreach (var item in list.Items)
            if (item is NbtString s && !string.IsNullOrWhiteSpace(s.Value)) mods.Add(s.Value);
        return mods;
    }
}
```

`NbtCompound.Fields` es público (`Nbt/NbtTag.cs:86`), así que el recorrido del compound
`loadouts` no necesita ningún API nuevo.

## C.5 Cambios exactos en `HomeViewModel.cs`

`ScanCharacters` (línea 152-174). Sustituir las líneas 163-165 por:

```csharp
var character = PlrFile.Read(File.ReadAllBytes(path));
// Encargo del usuario 4-sep-2026: el hecho PRINCIPAL es "este personaje es de tModLoader"
// (tiene un .tplr hermano), no "es de Calamity" - la variable de antes se llamaba isCalamity
// pero comprobaba exactamente esto, asi que CUALQUIER mod (o un .tplr huerfano de una partida
// vieja) encendia una insignia roja que decia "Calamity". Ver
// ESPEC-sprites-botones-badges.md#C.1.
string tplrPath = Path.ChangeExtension(path, ".tplr");
// Coste real medido en la carpeta real de este usuario: 2,53 ms para los 6 personajes juntos,
// incluido un .tplr de 183 KB crudos con 2709 entradas de research - y corre dentro del
// Task.Run que este escaneo ya usa, no en el hilo de UI. No hay nada que optimizar aqui.
var tplr = File.Exists(tplrPath) ? TplrProbe.TryRead(tplrPath) : null;
result.Add(new CharacterListEntryViewModel(path, character, tplr, File.GetLastWriteTimeUtc(path), equipmentAppearance));
```

Añadir `using TerrasavrNative.Core.Calamity;` a la cabecera del fichero.

**Caso límite real, decidido a propósito:** si el `.tplr` existe pero `TryRead` devuelve `null`
(corrupto/bloqueado), `tplr` queda `null` y el personaje se etiquetaría "Vanilla", lo cual sería
mentira. Para evitarlo, `CharacterListEntryViewModel` recibe **además** el booleano de existencia:

```csharp
bool esTModLoader = File.Exists(tplrPath);
var tplr = esTModLoader ? TplrProbe.TryRead(tplrPath) : null;
result.Add(new CharacterListEntryViewModel(path, character, esTModLoader, tplr, File.GetLastWriteTimeUtc(path), equipmentAppearance));
```

Así "es de tModLoader" depende solo del fichero (infalible) y "tiene Calamity" del contenido
(que puede fallar de forma segura hacia "no").

## C.6 Cambios exactos en `CharacterListEntryViewModel.cs`

Constructor (línea 30) y propiedades (líneas 18-23):

```csharp
public string FilePath { get; }
public string Name { get; }
public string DifficultyLabel { get; }
// Encargo del usuario 4-sep-2026 - tres hechos DISTINTOS donde antes habia uno solo mal
// nombrado (ver ESPEC-sprites-botones-badges.md#C):
//   IsTModLoader = existe un .tplr hermano. El hecho PRINCIPAL ("verdaderamente de tmodloader").
//   IsCalamity   = ademas hay un objeto o un buff de Calamity REAL dentro de ese .tplr (mismo
//                  criterio exacto que CharacterFileService.Save, resuelto sobre el NBT crudo).
//                  NO sustituye a IsTModLoader: un personaje puede llevar las dos insignias.
//   IsVanilla    = no hay .tplr. El usuario lo pidio explicitamente ("al igual que cuando un
//                  personaje es vanilla que tenga dicha etiqueta").
public bool IsTModLoader { get; }
public bool IsCalamity { get; }
public bool IsVanilla => !IsTModLoader;
// Regalo de la clave usedMods real del .tplr (la escribe tModLoader en cada guardado,
// PlayerIO.cs:67) - la lista de mods que estaban cargados la ultima vez. Null si el .tplr no la
// trae (los que escribe el propio Terrakeep no la tienen) para que el ToolTip no aparezca vacio.
public string? UsedModsTooltip { get; }
public string LastModifiedText { get; }
public WriteableBitmap Preview { get; }

public CharacterListEntryViewModel(string plrPath, PlrCharacter character, bool isTModLoader,
    TplrModSummary? tplr, DateTime lastModifiedUtc, EquipmentAppearanceResolver equipmentAppearance)
{
    …
    IsTModLoader = isTModLoader;
    IsCalamity = tplr?.HasCalamityContent == true;
    UsedModsTooltip = tplr is { UsedMods.Count: > 0 }
        ? "Mods usados la última vez: " + string.Join(", ", tplr.UsedMods)
        : null;
    …
}
```

## C.7 Cambio exacto en `MainWindow.xaml` (las tres insignias)

Sustituir el `StackPanel` de insignias de `CharacterCardTemplate`
(`MainWindow.xaml:1157-1167`, el que hoy tiene el chip de dificultad y el de "Calamity"):

```xml
<StackPanel Orientation="Horizontal" Margin="0,4,0,0">
    <Border Background="{StaticResource BgHoverBrush}" CornerRadius="3" Padding="5,1">
        <TextBlock Text="{Binding DifficultyLabel}" Style="{StaticResource CaptionText}" FontSize="10" />
    </Border>

    <!-- Encargo del usuario 4-sep-2026: hasta hoy solo habia UNA insignia, roja, que decia
         "Calamity" y en realidad solo significaba "tiene un .tplr al lado" (=cualquier mod).
         Ahora son tres hechos separados, en orden de importancia de izquierda a derecha:
         tModLoader (lo principal) -> Calamity (lo especifico, ADEMAS de la anterior, no en su
         lugar) -> Vanilla (la ausencia de las dos). Ver ESPEC-sprites-botones-badges.md#C. -->

    <!-- "Vanilla": la mas callada de las tres a proposito (es la AUSENCIA de mods, no un rasgo
         que reclame atencion). Solo contorno, mismo lenguaje visual que los selectores nuevos de
         Exploracion - sin inventar ningun color nuevo del tema. -->
    <Border Background="Transparent" BorderBrush="{StaticResource BorderStrongBrush}" BorderThickness="1"
            CornerRadius="3" Padding="5,1" Margin="5,0,0,0"
            ToolTip="Sin ningún mod: este personaje no tiene un archivo .tplr al lado"
            Visibility="{Binding IsVanilla, Converter={StaticResource BoolToVis}}">
        <TextBlock Text="Vanilla" FontSize="10" Foreground="{StaticResource TextSecondaryBrush}" />
    </Border>

    <!-- "tModLoader": la pareja AccentMuted/AccentHover, que en este tema ya significa "de la
         familia del acento pero secundario" (Tag="AccentSoft", PrefixGroupButton). Va ANTES que
         la de Calamity porque es el hecho principal, y su ToolTip lleva la lista real de mods
         (usedMods del propio .tplr) cuando el fichero la trae. -->
    <Border Background="{StaticResource AccentMutedBrush}" CornerRadius="3" Padding="5,1" Margin="5,0,0,0"
            ToolTip="{Binding UsedModsTooltip, TargetNullValue='Personaje de tModLoader: tiene un archivo .tplr con datos de mods'}"
            Visibility="{Binding IsTModLoader, Converter={StaticResource BoolToVis}}">
        <TextBlock Text="tModLoader" FontSize="10" Foreground="{StaticResource AccentHoverBrush}" FontWeight="SemiBold" />
    </Border>

    <!-- "Calamity": el rojo de siempre (CalamityBrush), pero ahora solo cuando hay contenido
         REAL de Calamity dentro del .tplr, no por el mero hecho de que el .tplr exista. -->
    <Border Background="{StaticResource CalamityBrush}" CornerRadius="3" Padding="5,1" Margin="5,0,0,0"
            ToolTip="Tiene objetos o buffs de Calamity guardados de verdad"
            Visibility="{Binding IsCalamity, Converter={StaticResource BoolToVis}}">
        <TextBlock Text="Calamity" FontSize="10" Foreground="White" FontWeight="SemiBold" />
    </Border>
</StackPanel>
```

**Comprobación de anchura:** la tarjeta de personaje tiene el nombre con
`TextTrimming="CharacterEllipsis"` justo encima y el `StackPanel` de insignias no se recorta solo.
Con las tres visibles el peor caso es `Softcore` + `tModLoader` + `Calamity` ≈ 210 px a
`FontSize 10`. Si en el ancho real de la rejilla de Inicio eso desborda, la corrección es cambiar
ese `StackPanel Orientation="Horizontal"` por un `WrapPanel` — decisión que hay que tomar
**mirando la captura real** del arnés (D.3), no a ojo desde aquí.

## C.8 Tests existentes que hay que actualizar (mecánico)

Tres puntos de construcción del constructor cambian de firma:

* `TerrasavrNative.App.ViewModels.Tests/HomeCardTests.cs:30` y `:31`
  → `new CharacterListEntryViewModel(@"C:\a.plr", NuevoPersonaje("A"), isTModLoader: false, tplr: null, DateTime.UtcNow, EquipAppearance)`
* `TerrasavrNative.App.ViewModels.Tests/ConfirmDiscardChangesTests.cs:34`
  → idem.

Ninguno de los dos comprueba `IsCalamity`, así que no hay expectativas que revisar, solo la firma.

## C.9 Alcance deliberado de la parte C

* **No se toca la insignia de Calamity de la cabecera global** (`MainWindow.xaml`, franja superior
  con el doll y el nombre del personaje **cargado**): esa sale de `HasCalamityData` de
  `MainViewModel`, con el personaje ya fusionado en memoria y datos completos. Está bien resuelta
  y no es lo que pide el encargo. Igual para los 7 usos de `IsCalamity` del editor
  (`ItemSlotViewModel` y compañía).
* **`research` y `modData` NO cuentan como "contenido de Calamity"**, aunque el `.tplr` de
  `adrian` tenga 2 709 entradas de investigación de Calamity. Motivo: la insignia tiene que
  significar **exactamente lo mismo** que la regla que la propia app ya aplica en
  `CharacterFileService.Save`; dos definiciones parecidas pero distintas de "tiene Calamity" en la
  misma app es una trampa que se paga meses después. Si se quiere ampliar, hay que ampliar las dos
  a la vez, y decirlo.
* **`usedMods` no decide ninguna insignia**, solo llena un `ToolTip`. Por el motivo demostrado en
  C.2.1 (`prueba.tplr` no lo tiene y sí tiene Calamity) y por el simétrico (un personaje con
  Calamity cargada pero sin nada de Calamity guardado lo tendría y no debería lucir el rojo).
* **No se cachea nada entre refrescos.** 2,5 ms no justifican una caché con invalidación por
  fecha de fichero, que es exactamente el tipo de complejidad que luego se queda desincronizada.

---

# PARTE D — Plan de verificación

## D.1 Compilación y pruebas unitarias

```
dotnet build TerrasavrNative.slnx
dotnet test TerrasavrNative.Core.Tests
dotnet test TerrasavrNative.App.ViewModels.Tests
```

* El `build` tiene que salir **sin avisos nuevos**. Ojo a `MC4011` si alguien mueve un
  `TargetName` de `ControlTemplate.Triggers` a `Style.Triggers` (`Theme.xaml:363-365` documenta
  que ya pasó una vez).
* `Core.Tests` debe seguir en verde sin tocar nada, **más** una prueba nueva (ver D.2).
* `App.ViewModels.Tests` fallará a compilar hasta actualizar las 3 llamadas de C.8.

## D.2 Pruebas nuevas que merecen existir

**`TerrasavrNative.Core.Tests/Calamity/TplrProbeTests.cs`** (nuevo), siguiendo el patrón de
`CalamityCharacterSyncRealFileTests.cs`:

1. `.tplr` construido a mano con `TplrFile.Write` y **solo** entradas `mod="Terraria"` →
   `HasCalamityContent == false`. **Este es el caso que no existe en el disco de esta máquina** y
   el que de verdad prueba que el bug está arreglado.
2. El mismo pero con una entrada `mod="CalamityMod"` en `inventory` → `true`.
3. Igual con la entrada dentro de `loadouts/loadout0Armor` → `true` (cubre el recorrido del
   compound anidado, el trozo más fácil de olvidar).
4. Un buff en `modBuffs` con `mod="CalamityMod"` → `true`.
5. `usedMods` con 3 cadenas → `UsedMods.Count == 3` y en el mismo orden.
6. `.tplr` sin `usedMods` → `UsedMods` vacío, no `null`, sin excepción.
7. Fichero de basura (no gzip) → `TryRead` devuelve `null`, no lanza.

**`TerrasavrNative.Core.Tests/Data/`** — smoke test de los assets nuevos, mismo criterio que
`RealDataFilesSmokeTests.cs`: que existan `Assets/vanilla/tile_icons/21_36_0.png` (Cofre de oro),
`.../tile_icons/7.png` (Mineral de cobre) y `.../wall_icons/1.png` (Pared de piedra), y que los
directorios tengan **878** y **366** ficheros. Eso convierte "se me olvidó ejecutar el extractor"
en un test rojo en vez de en un cuadradito de color en producción.

## D.3 Arnés de UI Automation (`TerrasavrNative.App.Tests`)

```
dotnet run --project TerrasavrNative.App.Tests
```

Añadir, en el bloque de Exploración que ya existe (`Program.cs:2275` en adelante):

**Parte A — sprites**
```
ICONOS-INVENTARIO: en la categoria Cofres (vista por defecto), cuantas filas de
  vm.Exploration.Inventory tienen IconPath != null, de cuantas en total.
  Esperado: TODAS las de un mundo vanilla real (los tipos de cofre son 21/88/467).
  FALLO si alguna fila de cofre sale con IconPath null.
ICONOS-MINERALES: idem sobre OreMetals+OreGems+OreTargets -> esperado 100% con icono.
ICONOS-PAREDES: ObjectsViewMode=1 -> % de filas con icono; esperado alto pero NO 100%
  (una pared de mod o la id 367 no lo tienen a proposito - documentar el numero real que salga).
ICONOS-LIQUIDOS: ObjectsViewMode=2 -> esperado IconPath null en TODAS (decision deliberada).
```
Más una captura `RenderTargetBitmap` de la barra lateral en la categoría **Cofres**
(`exploracion-cofres-con-sprites.png`), que es la prueba visual literal de lo que pidió el
usuario: *"un cofre dorado de agua etc"*. **Hay que mirarla de verdad**, no solo comprobar que el
fichero existe: un `Image` con `Source` a una ruta `pack://siteoforigin` inexistente no lanza, se
queda en blanco en silencio.

**Parte B — botones**
```
SELECTORES-ESTILO: las 5 pildoras de categoria siguen siendo RadioButton con su Name real
  ("Todo", "NPCs (N)", ...) - la comprobacion que YA existe en Program.cs:2280-2286 no debe
  cambiar ni un caracter. Si cambia, es que se rompio el TextBlock hijo / el StringFormat.
SELECTORES-EXCLUSION: marcar cada una de las 5 y comprobar que SelectedCategory cambia y que
  solo una queda IsChecked (el GroupName sigue vivo tras el cambio de plantilla).
SELECTORES-MULTI: los 3 chips de NPCs siguen siendo independientes (marcar dos a la vez).
```
Más capturas de la barra lateral en dos estados (`exploracion-selectores-npcs.png` y
`...-objetos.png`) para juzgar a ojo lo único que no se puede automatizar: si de verdad se
parecen a "Cargar personaje" **y** se distinguen de él.

**Parte C — insignias**
```
INSIGNIAS-INICIO: para cada CharacterListEntryViewModel de vm.Home.Characters, imprimir
  Name / IsVanilla / IsTModLoader / IsCalamity / UsedModsTooltip.
  Esperado con los personajes REALES de esta maquina:
    Eldelgas (tModLoader) : Vanilla=False TMod=True Calamity=True  tooltip con 5 mods
    adrian   (tModLoader) : Vanilla=False TMod=True Calamity=True  tooltip con 5 mods
    prueba   (tModLoader) : Vanilla=False TMod=True Calamity=True  tooltip NULL (.tplr de Terrakeep)
    adrian   (vanilla)    : Vanilla=False TMod=True Calamity=True  tooltip con 3 mods
    Eldelgas (vanilla)    : Vanilla=True  TMod=False Calamity=False
    Terrariano (vanilla)  : Vanilla=True  TMod=False Calamity=False
  FALLO si IsVanilla e IsTModLoader son iguales para alguno (son excluyentes por construccion).
```
Más una captura de la pantalla Inicio (`inicio-insignias.png`) — que es donde se comprueba lo que
la sección C.7 no puede decidir sobre el papel: **si las tres insignias caben en la tarjeta o hay
que pasar el `StackPanel` a `WrapPanel`**.

**Caso "tModLoader sin Calamity"**: no existe en el disco de esta máquina. Fabricarlo en el propio
arnés, en una carpeta temporal (mismo criterio que `HomeCardTests`, que nunca escribe cerca de un
personaje real): copiar un `.plr` real y escribir a su lado un `.tplr` con `TplrFile.Write` que
solo tenga entradas `mod="Terraria"` y un `usedMods` con un mod cualquiera. Esperado:
`TMod=True, Calamity=False` y **solo dos** insignias en pantalla. Sin este caso, la parte C no
está verificada de verdad — está verificado que sigue funcionando lo que ya funcionaba.

## D.4 Orden recomendado de ejecución

1. Ejecutar los dos extractores y comprobar que las cifras coinciden **exactamente** con A.6/A.7.
2. Comitear los assets nuevos por separado del código (1 244 ficheros ensucian cualquier diff).
3. Parte C (la más independiente: Core + 2 ViewModels + 1 bloque de XAML). `dotnet test` + arnés.
4. Parte A (resolvers + 6 puntos de llamada + 1 `DataTemplate`). Arnés + mirar la captura.
5. Parte B (2 estilos + 14 sustituciones). Arnés + mirar las capturas.
6. Commit tras cada una de las tres, con su verificación en verde (regla del proyecto).

---

# PARTE E — Resumen de lo que se queda fuera a propósito

| Fuera de alcance | Por qué |
|---|---|
| Iconos de líquido | El juego los pinta con un shader sobre una máscara (`LiquidMask.fxc`), no hay sprite recortable. El color de la paleta real del mapa es mejor que un icono inventado. |
| Los 9 368 frames de los 412 tiles enmarcados | Ninguna vista consume hoy un `(u,v)` que no venga de `ChestKindCounts`. Serían ~9 400 ficheros invisibles. Ampliable con una línea (`TIPOS_CON_VARIANTE`). |
| Árbol de dos niveles por variante en Objetos → Tiles | Ya estaba fuera de alcance en `ESPEC-ui-exploracion.md#9.3-E` y sigue estándolo; esto no lo cambia. |
| Redimensionar los iconos al extraerlos | Perdería detalle irrecuperable de pixel-art. `StretchDirection="DownOnly"` resuelve el encaje en la UI. |
| Pared `367` y tiles > 753 | No están en el catálogo de TEdit. Ya se quedan hoy sin nombre por la misma razón; el comportamiento es consistente, no una regresión. |
| Iconos de tile/pared de mods (Calamity incluido) | El `.wld` guarda ids de runtime de tModLoader que esta app no puede traducir (mismo muro documentado en `ExplorationViewModel.cs:158`). Caen al color. |
| La insignia de Calamity de la cabecera global y los 7 `IsCalamity` del editor | Otro origen (`HasCalamityData` de `MainViewModel`, personaje ya fusionado), ya bien resueltos, y no es lo que pide el encargo. |
| `research`/`modData` como prueba de "tiene Calamity" | Rompería la equivalencia con la definición que la app ya usa en `CharacterFileService.Save`. Dos definiciones distintas de lo mismo en la misma app es deuda garantizada. |
| Caché del sondeo del `.tplr` en Inicio | 2,53 ms medidos para toda la carpeta real. Una caché con invalidación por fecha costaría más de lo que ahorra y se desincronizaría. |
| Renombrar el `Tag` / retocar el estilo base de `Button` | El usuario pidió cambiar los **selectores de Exploración**, no los botones principales. El estilo base es el punto de referencia; tocarlo invalidaría la referencia. |
