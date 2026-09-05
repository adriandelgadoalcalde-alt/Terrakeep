# Pulido final: Exploración, Librería, Inicio, barra superior, Apariencia, Novedades y Acerca de

Cuarta auditoría de referencia previa a la entrega, encargada el **5-sep-2026** tras probar la
build resultante del plan de 22 correcciones «Opus vs TEdit»
(`ESPEC-auditoria-exploracion-tedit.md`, commits `f31f5d7b`..`2b63af5a`). Cubre **17 puntos
reportados por el usuario** repartidos por seis zonas de la aplicación.

**Encargo del usuario (verbatim, 5-sep-2026)**:

> "siguiente plan para opus pero que solo se centre en corregir todo esto y haga un plan
> exhaustivo contrastando información"

Confirmado después, también verbatim, que los dos últimos puntos (Novedades y Acerca de) **sí**
entran en este plan.

**Disciplina de este documento** (la misma de `ESPEC-buscador-mundo-tedit.md`,
`ESPEC-auditoria-redimensionado.md` y `ESPEC-auditoria-exploracion-tedit.md`):

- **Parte I (§1-§8) = HECHOS.** Cada afirmación sobre código lleva su fichero y su línea, leídos
  de verdad en esta pasada. Los datos numéricos salen de contar sobre los ficheros reales del
  proyecto, no de una impresión.
- **Parte II (§9-§12) = PROPUESTA.** Diseño de la solución, tabla priorizada y criterio de
  aceptación. Criterio mío, marcado como tal.
- **Parte III (§13) = HUECOS.** Lo que no pude verificar y por qué.

**Este documento no toca ni una línea de código de producción.** Es sólo el informe.

**Identificadores**: se conservan los códigos del propio encargo (`E1`…`E8`, `L1`…`L4`,
`H1`…`H3`, `A1`, `N1`, `N2`) como identificador primario de cada hallazgo, para que el usuario
reconozca sus propios puntos. Las correcciones propuestas van numeradas `C-01`…`C-19`.

---

# RESUMEN EJECUTIVO

De los 17 puntos, **8 son defectos reales de código** (no "falta una capacidad"), y **cuatro de
ellos son bugs graves que hoy hacen que una función anunciada no funcione en absoluto**. Dos de
esos cuatro se introdujeron en la ronda anterior y nadie los detectó porque el arnés no los cubre.

| # | Qué pasa de verdad | Severidad |
|---|---|---|
| **E4b** | **El marcador de aparición del mundo y el de la mazmorra (F-7) se dibujan siempre en la esquina superior izquierda del mapa, nunca en sus coordenadas.** Son dos `TextBlock` sueltos que llevan `Canvas.Left`/`Canvas.Top` pero cuyo padre real es un `Grid`, no un `Canvas` — WPF ignora las dos propiedades por completo. Regresión introducida en el commit `1343e838` (F-7) | **Crítica** |
| **L3-b** | **La línea "Bono activo" de un set de Calamity no puede aparecer NUNCA.** `ActiveCalamitySetBonusText` exige que las 3 piezas compartan el mismo `SetBonus`, pero en `calamity/catalog.json` **0 de 131 piezas de cuerpo/piernas** tienen `setBonus` — sólo lo tienen los 69 cascos. La rama entera es código muerto desde que se escribió | **Crítica** |
| **E5** | **Hacer clic en el nombre de un mineral no hace absolutamente nada.** `BuildSingleRowQuery` no tiene rama para `WorldSearchCategory.Ores`: cae al `_ =>` y devuelve una consulta vacía, que el runner convierte en "Sin resultados." — y ese texto tampoco se ve, porque `ShowZeroResultsState` sólo se activa en la categoría "Todo" | **Alta** |
| **E2** | **El rectángulo del minimapa se sale de la app entera al alejar el zoom.** `UpdateMinimapViewport` no recorta el viewport al tamaño del mundo, y ni el `Canvas`, ni el `Grid`, ni el `Border` del minimapa tienen `ClipToBounds`. A `Zoom=0.02` en un mundo Grande el rectángulo mide **1310 px de ancho dentro de una caja de 220** | **Alta** |
| **H3** | **Las tres columnas de la barra superior no tienen ni un píxel de separación entre ellas.** Ninguno de los tres paneles hijos del `Grid Auto/*/Auto` lleva `Margin`. El corazón de vida arranca literalmente pegado a la píldora "Softcore" **a cualquier ancho de ventana**, incluido pantalla completa — que es exactamente lo que el usuario reportó con captura | **Alta** |
| **L3-a** | **Faltan 20 de las 63 bonificaciones de set reales del juego**, entre ellas las cuatro escaleras de mineral de Hardmode (Cobalto, Mithril, Adamantita, Titanio) y el equipo de final de partida (Solar, Vórtice, Nebulosa, Escarabajo, Araña, Abeja). Tres fallos distintos del extractor, identificados y reproducidos | **Alta** |
| **E6** | **Marcar "Miel" (o cualquier bloque masivo) sólo marca las 1000 primeras casillas**, y el barrido va por columnas de izquierda a derecha — así que las marcas se agolpan en la franja izquierda del mundo y el resto queda sin marcar. Ni el líquido ni la lectura del `.wld` tienen ningún fallo: es el `DisplayLimit=1000` | **Alta** |
| **L1** | **Los dos nodos raíz "Indice" y "Calamity (mod)" del árbol de la Librería de buffs son los ÚNICOS del árbol construidos sin `IconPath`.** No falta ningún fichero de icono: faltan dos asignaciones. El árbol de objetos sí lo hace bien | **Media** |

Y nueve más de "pulido / capacidad ausente": tildes en el buscador (**2638 de 8821 nombres del
catálogo son inalcanzables si escribes sin acento**), Deshacer que no cubre 18 valores editables
de Apariencia, insignias de Inicio que se tocan al envolver, tarjetas de personaje sin animación
de hover, la tira "Tus mundos" pegada al mapa, los cofres sin vista por cofre individual, el
arrastre desde la Librería sin nada que lo anuncie, la pestaña Novedades con **0 de 40 sprites
resueltos** por un catálogo de nombres una versión del juego por detrás, y el registro de cambios
del propio Terrakeep **congelado desde hace 170 commits**, sin el nombre del autor.

**Tres punteros del encargo resultaron estar equivocados** y se corrigen en §8.

---

# PARTE I — HECHOS

## 1. Metodología y alcance

### 1.1 Qué leí, de verdad, en esta pasada

**De Terrakeep**, enteros o en los tramos citados:

- `TerrasavrNative.App/MainWindow.xaml` — 4635 renglones. Leídos enteros los tramos 1-30,
  305-330, 600-724, 980-1300, 1370-1620, 1740-1810, 2380-2506, 2530-2700, 3430-3560, 3640-3830,
  3900-3960, 4110-4230, 4360-4415, 4450-4560.
- `TerrasavrNative.App/MainWindow.xaml.cs` — 1039 renglones; tramos 288-320, 516-600, 660-1000.
- `TerrasavrNative.App/ViewModels/ExplorationViewModel.cs` — 1382 renglones; tramos 82-115,
  240-360, 400-660, 740-760, 1040-1382.
- `TerrasavrNative.App/ViewModels/MainViewModel.cs` — 1600 renglones; tramos 140-260, 310-350,
  370-430, 480-680, 840-860, 1000-1020, 1490-1530.
- Enteros: `AppearanceViewModel.cs` (384), `LibraryViewModel.cs` (182),
  `LibrarySearchGrammar.cs` (51), `BuffLibraryViewModel.cs` (117),
  `CharacterListEntryViewModel.cs` (78), `WhatsNewViewModel.cs` (23),
  `WhatsNewItemViewModel.cs`, `WhatsNewEntryViewModel.cs`, `AboutViewModel.cs` (26),
  `CategoryNodeViewModel.cs`, `Controls/SlotGridPanel.cs` (168),
  `Services/UndoStack.cs`, `Services/BuffLibraryTreeBuilder.cs` (169),
  `Services/VanillaBuffIconResolver.cs`, `Services/VanillaIconResolver.cs`,
  `Services/WorldHighlightRenderer.cs`, `Core/Data/ItemStatsFormatter.cs` (207),
  `Core/WldFormat/WldHeader.cs`, `Core/WldFormat/WldChest.cs`,
  `Core/WldFormat/WorldSearch.cs` (tramos 1-183), `scripts/extraer-sets-armadura.py` (290).
- `TerrasavrNative.App/Styles/Theme.xaml` — tramos 198-245, 1085-1165.
- Los tres informes previos, en los tramos relevantes: `ESPEC-auditoria-exploracion-tedit.md`
  (índice completo + §5, §10, §11), `ESPEC-auditoria-redimensionado.md` (§3 resumen, §4.4, §4.5,
  tabla §5), `ESPEC-buscador-mundo-tedit.md` (§1.1-§1.3).
- `bitacora.md`, tramo 5610-5640 (nota real de `H3-18`).

**Del juego real** (`Downloads\tModLoader-Decompiled\`, obligatorio por CLAUDE.md antes de
suponer nada):

- `tModLoader/Terraria/Player.cs` — `UpdateArmorSets` (14340-14880), `FindSpawn`/`RemoveSpawn`
  (55771-55835), serialización real de spawn points (56045-56056 escritura, 56798-56809 lectura).
- `tModLoader/Terraria/ID/ItemID.cs` y `TerrariaVanilla/Terraria/ID/ItemID.cs` — comparados.
- `TerrariaVanilla/Terraria.Localization.Content.es-ES.Game.json` — sección `ArmorSetBonus`.

**Datos contados de verdad** (scripts de un solo uso sobre los `.json` reales del proyecto,
ejecutados en el scratchpad de la sesión, sin tocar el repo): nombres con diacríticos por
catálogo, cobertura de bonos de set vanilla y Calamity, cobertura de sprites de Novedades,
recuento de iconos de buff, rango de ids de cada catálogo vanilla.

**Reproducción real del extractor**: `scripts/extraer-sets-armadura.py` se copió al scratchpad
con la ruta de salida cambiada y se ejecutó **instrumentado** para saber exactamente qué claves
pierde y en qué paso las pierde (§5.3). El fichero del repo no se tocó.

### 1.2 Qué NO es esta auditoría

- **No ejecuto la aplicación.** Todo es lectura de código y de datos. Los puntos que dependen de
  ver píxeles reales (H1, H3, E3, y la mitad de L4) llevan su hueco explícito en §13.
- **No repite** lo ya cubierto por los tres informes anteriores. Donde un hallazgo toca algo que
  ya se auditó, se cita el hallazgo previo en vez de re-diagnosticarlo (E3 ↔ `H3-18`/`SIN-SCROLL`,
  H3 ↔ `H-04a`/`R-04b`, E8 ↔ `ESPEC-buscador-mundo-tedit.md#1.2`).
- **No hay ni una línea copiada de TEdit.** Sigue vigente lo dicho en
  `ESPEC-auditoria-exploracion-tedit.md`: TEdit es **MS-PL**, no MIT, y copiar código literal
  obligaría a distribuir esa parte bajo MS-PL.

---

## 2. Exploración — continuación del pulido

### E1 · Baja · La tira "Tus mundos" tiene 20 px de aire por arriba y **0 por abajo**

> *"a la barra de tus mundos en exploración hay que darle un poco mas de aire la subiría un poco"*

**Estado real.** La tira vive en `MainWindow.xaml:3537-3550`:

```xml
<DockPanel DockPanel.Dock="Top" Margin="0,10,0,0"
           Visibility="{Binding Exploration.Worlds.Count, Converter={StaticResource CountToVis}}">
```

El reparto vertical real de esa zona, sumando los márgenes declarados:

| Elemento | Línea | Margen relevante |
|---|---|---|
| `WrapPanel` de la barra de herramientas (Cargar mundo / zoom / Exportar) | `3480` | `Margin="0,0,0,10"` |
| **Tira "Tus mundos"** | `3537` | `Margin="0,10,0,0"` — **sin margen inferior** |
| Píldora de mundo (`WorldPillTemplate`) | `1242` | `Padding="8,5"`, `Margin="0,0,8,0"` — **sin margen vertical** |
| `ScrollViewer` de la tira | `3543` | `HorizontalScrollBarVisibility="Auto"` |
| `Grid` del mapa (siguiente hermano, rellena) | `3555` | — |

- **Hueco por encima de la tira: 20 px** (10 del `WrapPanel` + 10 de la propia tira).
- **Hueco por debajo de la tira: 0 px.** El lienzo del mapa empieza pegado a las píldoras.
- Cuando hay suficientes mundos para que el `ScrollViewer` muestre su barra horizontal, ésta
  ocupa **10 px más dentro** de la altura de la tira (el estilo real de `ScrollBar`,
  `Theme.xaml:200-238`, tiene un `Trigger Orientation=Horizontal` que le pone `Height=10`), lo
  que reduce todavía más el aire visible bajo las píldoras.

El desequilibrio 20/0 es exactamente lo que se lee como "le falta aire" y "hay que subirla".

---

### E2 · Alta · El rectángulo del minimapa se sale del minimapa y de la ventana

> *"el cuadrado del mini mapa el de color azul... si haces zoom out, sobre sale incluso fuera del
> recuadro del mini mapa y se expande por todo terrakeep"*

**El puntero del encargo es correcto y la sospecha también.** `UpdateMinimapViewport`
(`MainWindow.xaml.cs:697-727`) calcula:

```csharp
double vpX = WorldMapScroll.HorizontalOffset / zoom;
double vpY = WorldMapScroll.VerticalOffset / zoom;
double vpW = WorldMapScroll.ViewportWidth / zoom;
double vpH = WorldMapScroll.ViewportHeight / zoom;

Canvas.SetLeft(MinimapViewportRect, huecoX + vpX * escala);
Canvas.SetTop(MinimapViewportRect, huecoY + vpY * escala);
MinimapViewportRect.Width = Math.Max(1, vpW * escala);
MinimapViewportRect.Height = Math.Max(1, vpH * escala);
```

(`MainWindow.xaml.cs:717-724`). **No hay ningún recorte** de `vpW`/`vpH` contra
`img.PixelWidth`/`PixelHeight`, ni de `vpX`/`vpY` contra el borde derecho/inferior.

**Y nada lo recorta después.** La jerarquía real del minimapa (`MainWindow.xaml:3931-3948`) es
`Border` → `StackPanel` → `Grid` → (`Image` 220×63 + `Canvas` con el `Rectangle`). El `Canvas`
lleva `IsHitTestVisible="False"` (`:3944`) pero **ninguno de los tres contenedores lleva
`ClipToBounds="True"`**, y en WPF el valor por omisión es `false` para `Grid`, `Canvas` y
`Border` por igual. Un hijo posicionado fuera de la caja se dibuja fuera de la caja.

**La aritmética real, con las cifras del proyecto.** `MinZoom = 0.02` (`ExplorationViewModel.cs:1152`),
`MinimapImage` es `Width="220" Height="63"` (`MainWindow.xaml:3942`), y el mundo Grande real de
esta máquina es 8400×2400 (citado y medido en `ExplorationViewModel.cs:185-187`):

```
escala = min(220/8400, 63/2400) = min(0,02619, 0,02625) = 0,02619
```

Con un viewport de mapa de ~1000 px de ancho y `Zoom = 0,02`:

```
vpW = 1000 / 0,02      = 50.000 tiles   (el mundo entero mide 8.400)
Width = 50.000 × 0,02619 = 1.310 px     (la caja mide 220)
```

**El rectángulo pedido mide casi seis veces el ancho del minimapa**, y como nada recorta, se pinta
sobre todo lo que tenga alrededor. Con "Ajustar a la ventana" en un mundo Grande el zoom real
ronda 0,09 (medido y documentado en `ExplorationViewModel.cs:1148-1152`), lo que ya da
`vpW ≈ 11.111` tiles → **291 px**, ya desbordado. El desbordamiento empieza en cuanto el mundo
entero cabe en pantalla, no sólo en el extremo.

---

### E3 · Media · Última fila cortada y scroll que salta en la Librería al ancho mínimo

> *"con la librería abierta en el mínimo de ventana que se puede tener la ultima fila de objetos
> se medio corta y salta el scroll"*

**El puntero del encargo está equivocado** (ver §8): `MainWindow.xaml:657`
(`WrapPanel ItemWidth="248"`) **no es la Librería**, es la pestaña **Builds**
(`BuildStageViewModel` → `BuildClassTemplate`, `:618-661`). Los resultados de la Librería usan
otra cosa por completo, `MainWindow.xaml:2486-2494`:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" ...>
    <ItemsControl ItemsSource="{Binding Library.Results}" ItemTemplate="{StaticResource LibraryCardTemplate}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <controls:SlotGridPanel Columns="12" MinCell="40" MaxCell="90" Gap="4"
                                         ReferenceColumns="10"
                                         AvailableHeight="{Binding ActualHeight, RelativeSource={RelativeSource AncestorType=ScrollViewer}}" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
    </ItemsControl>
</ScrollViewer>
```

**Lo que sí es un hecho verificable en el código de `SlotGridPanel`** (`Controls/SlotGridPanel.cs`):

1. **El alto disponible entra por una propiedad que sólo se actualiza DESPUÉS de la disposición.**
   `AvailableHeight` se enlaza a `ScrollViewer.ActualHeight`, y `ActualHeight` no tiene un valor
   válido hasta que el `ScrollViewer` ya se ha dispuesto. El propio código lo asume y lo
   documenta (`:114-116`):

   ```csharp
   // Antes del primer paso de layout real (AvailableHeight todavia en 0) - no colapsar a
   // una rejilla ilegible, partir de MaxCell hasta que el remedido real llegue.
   if (availH <= 0) availH = MaxCell * rows + Gap * (rows - 1);
   ```

   Es decir: **el panel está diseñado para necesitar al menos dos pasadas de disposición**, y la
   primera pasada **sobrestima a propósito** (parte de `MaxCell = 90`). Cualquier situación en la
   que la segunda pasada no llegue —o llegue con un valor de una geometría anterior— deja la
   rejilla medida de más, es decir: contenido más alto que el viewport por un margen pequeño.
   "Media fila cortada" y "un golpe de rueda recorre todo el rango" son exactamente la firma de
   un extent que excede el viewport por poco.

2. **`ActualHeight` es la propiedad equivocada.** Lo que hace falta es el alto **de contenido**
   del `ScrollViewer`, que es `ViewportHeight`. Coinciden hoy sólo porque este `ScrollViewer` no
   tiene borde, ni `Padding`, ni barra horizontal (`HorizontalScrollBarVisibility="Disabled"`);
   no es una garantía de la API, es una coincidencia de esta plantilla concreta.

3. **`Columns="12"` es fijo, no se recalcula con el ancho.** El contenido mínimo posible del
   panel es, por tanto:

   ```
   12 × MinCell + 11 × Gap = 12×40 + 11×4 = 524 px
   ```

   El `ScrollViewer` que lo envuelve tiene el eje horizontal **deshabilitado**. Si alguna vez el
   ancho de disposición baja de 524 px, `ArrangeOverride` calcula
   `offsetX = Math.Max(0, (finalSize.Width - contentW) / 2) = 0` (`:157`) y coloca la columna 12
   en `x = 11×44 = 484…524`, fuera del viewport y **sin ninguna forma de alcanzarla**.
   El comentario del propio XAML dice que estos parámetros se midieron "contra la franja real de
   ~772×186px" (`:2447-2450`), así que hoy hay holgura; pero la conclusión de la auditoría de
   redimensionado (`ESPEC-auditoria-redimensionado.md#3`, `SIN-SCROLL: 0 casos`, *"`SlotGridPanel`
   se autolimita en ancho"*) **sólo es cierta mientras `cellFromWidth ≥ MinCell`**. Por debajo de
   ese umbral el panel no se autolimita: devuelve 524 px de ancho deseado y se recorta.

**Relación con la auditoría anterior**: `H3-18` (`bitacora.md:5615-5620`) ya documentó un caso
gemelo — *"el `MinHeight="216"` real de la fila de Inventario ya no cubre la 5ª fila entera a
1080×700 con la Librería desplegada"* — y se cerró **como nota, sin arreglo**, apoyándose en que
"el ScrollViewer de seguridad ya cubre este caso". El usuario acaba de reportar precisamente que
ese ScrollViewer de seguridad **no** lo cubre de forma usable.

---

### E4 · Media-alta · Las estrellas de spawn del personaje salen en cualquier mundo

> *"ya se para que son las estrllas azules en el mapa son spawn points pero salen
> independientemente de que el mundo no sea el suyo no tiene sentido eso"*

**El puntero del encargo es correcto.** `MainViewModel.BuildCharacterSpawns()`
(`MainViewModel.cs:335-341`) es, entero:

```csharp
private IEnumerable<(string Label, int X, int Y)> BuildCharacterSpawns()
{
    if (_loaded == null) yield break;
    foreach (var entry in Servers.Entries)
        if (entry.SpawnX != 0 || entry.SpawnY != 0)
            yield return (entry.Name, entry.SpawnX, entry.SpawnY);
}
```

**No hay ninguna comparación con el mundo cargado.** Se llama en dos sitios (`:316` al cargar
personaje, `:1078` al cargar mundo) y el resultado va tal cual a
`ExplorationViewModel.SetCharacterSpawns` (`:751-755`) → colección `CharacterSpawns` (`:277`) →
`ItemsControl` del mapa (`MainWindow.xaml:3728`).

**El dato para cruzarlos existe, ya se lee, y está en memoria.** Esto es lo importante, y lo
verifiqué contra el juego real, no contra el modelo de Terrakeep:

`PlrServerEntry` (`Core/PlrFormat/PlrCharacter.cs:15-21`) tiene cuatro campos:

```csharp
public sealed class PlrServerEntry
{
    public required int SpawnX { get; set; }
    public required int SpawnY { get; set; }
    public required int Address { get; set; }
    public required string Name { get; set; }
}
```

Se leen y se escriben en ese orden (`Core/PlrFormat/PlrBodySerializer.cs:586-612`). En el juego
real la misma lista se serializa así (`tModLoader/Terraria/Player.cs:56045-56056`, escritura):

```csharp
fileIO.Write(newPlayer.spX[num6]);
fileIO.Write(newPlayer.spY[num6]);
fileIO.Write(newPlayer.spI[num6]);
fileIO.Write(newPlayer.spN[num6]);
```

y se lee igual en `:56798-56809`. Es decir, **campo a campo**:

| Terrakeep | Terraria real | Qué es de verdad |
|---|---|---|
| `SpawnX` | `spX` | coordenada X del tile |
| `SpawnY` | `spY` | coordenada Y del tile |
| **`Address`** | **`spI`** | **el `Main.worldID` del mundo** — el nombre "Address" es un anglicismo heredado del Terrasavr JS, **no es una dirección de servidor** |
| `Name` | `spN` | **el `Main.worldName`** — no es el nombre del punto de aparición |

Y la regla de emparejamiento del propio juego, literal, en `Player.FindSpawn()`
(`Player.cs:55781`), `RemoveSpawn()` (`:55796`) y `AddSpawn()` (`:55818`) — las tres idénticas:

```csharp
if (spN[i] == Main.worldName && spI[i] == Main.worldID)
```

**Las dos condiciones, con Y lógico.** Del lado del mundo, Terrakeep ya lee los dos campos
correspondientes y los tiene en memoria: `WldHeader.WorldId` (`Core/WldFormat/WldHeader.cs:19`)
y `WldHeader.Title` (`:18`).

**Conclusión de hecho**: el cruce no sólo es posible, es **una comparación de dos campos que ya
están los dos leídos**. No hace falta ampliar ni el lector de `.plr` ni el de `.wld`.

**Efecto secundario colateral, real**: `ServerEntryRowViewModel` (`ServerEntryRowViewModel.cs:20`)
expone `Address` como un `int` editable en la pestaña Spawn Points, etiquetado con ese nombre. El
usuario está viendo y pudiendo editar el **id del mundo** creyendo que es la dirección de un
servidor.

---

### E4b · Crítica · **Los marcadores de spawn del mundo y de la mazmorra nunca aparecen donde deben**

Esto no estaba en la lista del usuario: el encargo pedía sólo *verificar* que el marcador de F-7
funciona. **No funciona.**

`MainWindow.xaml:3768-3780` y `:3781-3793`:

```xml
<TextBlock Text="&#8962;" FontSize="18" Foreground="{StaticResource OrangeBrush}"
           Canvas.Left="{Binding Exploration.WorldSpawnX}" Canvas.Top="{Binding Exploration.WorldSpawnY}"
           Margin="-9,-9,0,0" ToolTip="Aparición del mundo" Cursor="Hand" ... />
<TextBlock Text="&#9671;" FontSize="18" Foreground="{StaticResource CalamityBrush}"
           Canvas.Left="{Binding Exploration.WorldDungeonX}" Canvas.Top="{Binding Exploration.WorldDungeonY}"
           Margin="-9,-9,0,0" ToolTip="Mazmorra" Cursor="Hand" ... />
```

**El padre real de esos dos `TextBlock` es el `Grid` de `MainWindow.xaml:3651`** — el mismo que
lleva el `ScaleTransform` del zoom. Volqué la estructura completa de elementos entre las líneas
3648 y 3830 para comprobarlo sin margen de duda; el orden real de hijos de ese `Grid` es:

```
3651  <Grid HorizontalAlignment="Left" VerticalAlignment="Top">   <- LayoutTransform (zoom)
3655    <Image x:Name="WorldMapImage" .../>
3662    <Image  (WorldHighlight) .../>
3665    <ItemsControl (Npcs)>        ItemsPanel = <Canvas IsItemsHost="True"/>   OK
3728    <ItemsControl (CharacterSpawns)>  ItemsPanel = <Canvas IsItemsHost="True"/>   OK
3768    <TextBlock  (spawn del mundo)  Canvas.Left / Canvas.Top    <-- HIJO DIRECTO DEL Grid
3781    <TextBlock  (mazmorra)         Canvas.Left / Canvas.Top    <-- HIJO DIRECTO DEL Grid
3801    <ItemsControl (WorldSearchResults)> ItemsPanel = <Canvas IsItemsHost="True"/>   OK
```

**`Canvas.Left` y `Canvas.Top` son propiedades adjuntas que sólo lee el panel `Canvas` en su
`ArrangeOverride`.** Un `Grid` las ignora por completo, sin error, sin aviso y sin excepción. Los
tres `ItemsControl` vecinos lo hacen bien porque sus contenedores viven dentro de un `Canvas`
real (`ItemsPanelTemplate`, líneas `3667`, `3730`, `3803`) y ponen la propiedad adjunta en el
`ItemContainerStyle`, donde sí la lee el panel correcto.

**Consecuencias reales, las dos:**

1. Los dos marcadores se disponen como cualquier hijo de un `Grid` sin alineación: **estirados
   sobre toda la celda**, con el glifo dibujado en el borde izquierdo (`TextAlignment` por
   omisión) y desplazado `-9,-9` por el `Margin`. Es decir, **en la esquina superior izquierda del
   mapa, siempre**, sea cual sea la posición real del spawn.
2. Encima llevan `RenderTransformOrigin="0.5,0.5"` con un `ScaleTransform` inverso al zoom
   (`:3773-3776`, `:3786-3789`). Al escalar respecto al centro de un elemento estirado a todo el
   ancho del mapa, el glifo —que está en el extremo izquierdo— **se desplaza fuera del área
   visible** cuanto más se aleja el zoom. A `Zoom = 0,09` el factor es ×11.

Es decir: la función que el usuario pidió literalmente (*"cuando abres un mapa que te enseñara
donde esta el spawn de dicho mundo"*) **está implementada, se dio por buena, y no funciona**. El
commit `1343e838` afirma haberla verificado, pero lo que verificó fue que
`Dungeon=(621,445)` se **lee** bien del `.wld` — no que el marcador se **dibuje** ahí.

**Por qué el arnés no lo pilló**: la comprobación `A8-03` (`TerrasavrNative.App.Tests/Program.cs:2279-2302`)
mide el ancho en pantalla de un marcador **de resultado** (`WorldSearchHitRowViewModel`) a tres
niveles de zoom; no hay ninguna comprobación de la **posición** de ningún marcador, ni ninguna
que toque los dos `TextBlock` de F-7.

---

### E5 · Alta · Marcar un mineral con el tick, o pulsar su nombre, no hace nada

> *"si marcas cualquier mineral con el tick en la sección minerales no se marcan en el mapa ni
> clicando al tick ni clicando al nombre del mineral"*

**El puntero del encargo apuntaba a `MarkOresOnMapCommand`. Ese comando está bien.** La causa
real es otra, y son dos rutas rotas distintas.

**Ruta 1 — pulsar el NOMBRE del mineral. Consulta vacía.**

Las filas de mineral usan `InventoryRowTemplate` (`MainWindow.xaml:1282`, aplicada en `:4403`,
`:4406`, `:4409`), cuyo botón de fila dispara:

```xml
Command="{Binding DataContext.Exploration.SearchInventoryRowCommand, ...}" CommandParameter="{Binding}"
```

(`MainWindow.xaml:1297-1299`) → `SearchInventoryRow` (`ExplorationViewModel.cs:573-574`) →
`BuildSingleRowQuery(row)` (`:594-602`), que es **entero**:

```csharp
private WorldSearchQuery BuildSingleRowQuery(WorldInventoryRowViewModel row) => SelectedCategory switch
{
    WorldSearchCategory.Chests when ChestViewMode == 0 => new WorldSearchQuery { SpriteVariants = ... },
    WorldSearchCategory.Chests                        => new WorldSearchQuery { ChestItemIds = ... },
    WorldSearchCategory.Objects when ObjectsViewMode == 0 => new WorldSearchQuery { TileTypes = ... },
    WorldSearchCategory.Objects when ObjectsViewMode == 1 => new WorldSearchQuery { WallIds = ... },
    WorldSearchCategory.Objects                       => new WorldSearchQuery { LiquidTypes = ... },
    _ => new WorldSearchQuery(),
};
```

El enum real es `{ All, Npcs, Chests, Ores, Objects }` (`:82`). **`Ores` no tiene rama.** Cae al
`_ =>` y devuelve una `WorldSearchQuery` **completamente vacía**, cuyo `IsEmpty`
(`Core/WldFormat/WorldSearch.cs:63-64`) es `true`. En el runner
(`ExplorationViewModel.cs:1332-1342`):

```csharp
if (query.IsEmpty)
{
    ...
    WorldSearchResults.Clear();
    WorldSearchSummary = "Sin resultados.";
    return;
}
```

Y ese texto **tampoco se ve** en Minerales: el estado vacío con cuerpo (`ZeroResultsPanel`,
`MainWindow.xaml:4188-4196`) depende de `ShowZeroResultsState`, que es (`:299`):

```csharp
public bool ShowZeroResultsState => SelectedCategory == WorldSearchCategory.All && WorldSearchSummary == "Sin resultados.";
```

El resumen compacto sí se vería (`ShowCompactSummary`, `:303`), pero es un `CaptionText` de 11 px
que además **vacía la lista de resultados anterior**. Desde el punto de vista del usuario: pulsas
el nombre del mineral y desaparece lo que hubiera, sin explicación. Exactamente lo reportado.

**Ruta 2 — el tick.** El `CheckBox` (`MainWindow.xaml:1291-1292`,
`IsChecked="{Binding IsChecked}"`) está bien: `WorldInventoryRowViewModel.IsChecked` es un
`[ObservableProperty]` real (`ExplorationViewModel.cs:109`) y `CheckBox.IsChecked` enlaza en
`TwoWay` por omisión. `MarkOresOnMapCommand` (`:610-641`) lee correctamente
`OreMetals.Concat(OreGems).Concat(OreTargets).Where(r => r.IsChecked)` (`:614`).

**El problema del tick es de descubribilidad, no de código**: en la sección Minerales
(`MainWindow.xaml:4376-4412`) hay exactamente dos botones, *"Marcar en el mapa"* y *"Quitar
marcas"* (`:4389-4394`). **No existe** el botón "Buscar seleccionados" que sí acompaña al tick en
Cofres y Objetos. Marcar la casilla no produce ninguna realimentación visible hasta que además
se pulsa "Marcar en el mapa", y el tooltip del propio `CheckBox` dice literalmente *"Marcar para
'Buscar seleccionados'"* (`MainWindow.xaml:1292`) — **el nombre de un botón que en esta sección
no existe**.

**Nota adicional, relacionada.** `SearchCheckedInventory` (`:576-591`) lee
`Inventory.Where(r => r.IsChecked)` (`:579`), pero `RebuildInventory` (`:419-432`) **vacía
`Inventory`** cuando la categoría es `Ores` (las filas viven en `OreMetals`/`OreGems`/`OreTargets`,
`:351-353`). Si algún día se añade el botón que falta sin tocar ese método, seguiría sin hacer
nada: `marcadas.Count == 0` → `return` inmediato. Y su `switch` tiene el mismo `_ =>` vacío para
`Ores`.

---

### E6 · Alta · La miel (y cualquier bloque masivo) sólo se marca en la franja izquierda del mundo

> *"si le das a miel marca pero solo las colmenas que tienen una abeja reina pero no el resto de
> colmenas que aun tienen miel además el desplegable de objetos que salen una cantidad absurda de
> mieles"*

**Descarté primero las dos causas que parecían obvias, leyendo el código real:**

1. **La lectura de líquidos del `.wld` es correcta.** `WldReader.cs:273-289` decodifica los 2 bits
   de `header1 & 0x18` (1=agua, 2=lava, 3=miel) y usa el bit `header3 & 0x80` para distinguir
   Centelleo con un código sintético 4 — un caso que ya se arregló a fondo en `H3-10`. La miel no
   se confunde con nada.
2. **No hay ningún filtro por variante U/V para los líquidos.** `BuildSingleRowQuery` para
   líquidos hace `LiquidTypes = { (byte)row.Id }` (`:600`) y el barrido casa con
   `tile.LiquidAmount > 0 && query.LiquidTypes.Contains(tile.LiquidType)`
   (`WorldSearch.cs:121-122`) — sin ninguna condición extra.

**La causa real es el tope de resultados, combinado con el orden del barrido.**
`WorldSearchQuery.DisplayLimit = 1000` (`WorldSearch.cs:65`) y el acumulador
(`WorldSearch.cs:178-182`):

```csharp
private static void Add(ref int total, List<WorldSearchHit> hits, int limit, WorldSearchHit hit)
{
    total++;
    if (hits.Count < limit) hits.Add(hit);
}
```

**`total` cuenta todo; `hits` se corta en 1000.** Y los marcadores del mapa se dibujan desde
`WorldSearchResults` (`MainWindow.xaml:3801`), que se llena sólo desde `hits`
(`ExplorationViewModel.cs:1355-1357`). El barrido recorre **`for x` por fuera, `for y` por
dentro** (`WorldSearch.cs:106-108`): columna a columna, **de izquierda a derecha**.

**Consecuencia geométrica exacta**: en un mundo con decenas de miles de casillas de miel, las
1000 marcadas son **todas las de las columnas más a la izquierda que contengan miel**, y todo lo
que haya a su derecha queda contado pero sin marcar. El usuario interpretó el subconjunto
resultante como "sólo las colmenas con abeja reina"; el criterio real no es la reina, es la
coordenada X. (La correlación es verosímil: las cámaras de colmena con larva están en la jungla,
que en la generación real de Terraria queda a un lado del mundo.)

**La segunda mitad de la queja —"una cantidad absurda de mieles" en el desplegable— es el mismo
tope visto desde la lista**: 1000 filas, **una por casilla**, todas con el mismo nombre. El
resumen sí dice la verdad (`ExplorationViewModel.cs:1358-1362` produce
`"1000 de 47.291 resultado(s) (limitado a 1000)"`), pero no ayuda a encontrar nada.

**Contraste real con lo que ya se hace bien**: la sección **Minerales** resuelve exactamente este
problema, y lo resuelve **en dos capas separadas** — `MarkOresOnMap` (`:610-641`) pinta
`WorldHighlightRenderer.Render(...)` **sin tope, todas las posiciones**, y llena la lista con
**vetas agrupadas** (`OreVeinFinder.Find(..., limit: 1000, ...)`), no con casillas sueltas. El
comentario del código lo dice literalmente (`:604-608`): *"mismo reparto real que hace TEdit,
resaltado sin tope + lista topada"*. **Esa arquitectura ya existe y funciona; Objetos y Cofres
simplemente no la usan.**

**Nombres reales de "miel" en el catálogo** (`Assets/tile_names.json`, contados): el usuario tiene
seis filas distintas que contienen "miel" o "colmena" en Objetos › Tiles —
`225` Colmena, `229` Bloque de miel, `230` Bloque de miel tostado, `308` Dispensador de miel,
`345` Bloque de lluvia de miel, `375` Honey Drip (sin traducir), `444` Colmena de abejas — más la
fila `3` Miel en Objetos › Líquidos. Siete entradas para un solo concepto, sin ninguna
agrupación.

---

### E7 · Propuesta del usuario · "Que marcar el tick marque TODO en el mapa"

> *"yo veo mas prectico que si marcas con el tick un mineral objeto o lo que sea ya directamente
> se marque todo en el mapa y todos los que hay claro no que marque unos si otros no"*

Se evalúa en §9.4 (Parte II). Hecho relevante para evaluarla: **la mitad "que se marque todo" ya
está implementada y en producción para Minerales** (`WorldHighlightRenderer`, sin tope,
§E6 arriba); lo que falta es (a) extenderla a Objetos/Cofres y (b) que la marca sea la reacción
inmediata al tick en vez de exigir un segundo botón.

Coste real que el usuario no está viendo, y que hay que decirle: `WorldHighlightRenderer.Render`
crea un `WriteableBitmap` **del tamaño completo del mundo**, y su propio comentario lo cuantifica
(`WorldHighlightRenderer.cs:21-24`): *"un Bgra32 del tamaño de un mundo Grande (8400x2400) son
80,6 MB"*. Marcar al vuelo con cada clic de casilla significa **regenerar 80 MB por clic** en un
mundo Grande.

---

### E8 · Media · "Por lo que contienen" no permite ver un cofre concreto

> *"la sección ''por lo que contienen'' salen todos los contenidos de todos los cofres del juego
> no están clasificados por cofre lo ideal es poder elegir cualquier cofre y saber que hay dentro
> de ese cofre de manera singular"*

**El puntero del encargo es correcto.** `RebuildChestInventory` (`ExplorationViewModel.cs:439-470`),
rama `ChestViewMode != 0` (`:452-468`):

```csharp
var combinados = new Dictionary<int, int>(_presence.ChestItemCounts);
foreach (var (netId, count) in _presence.TileEntityItemCounts)
    combinados[netId] = combinados.GetValueOrDefault(netId) + count;

foreach (var (netId, count) in combinados.OrderByDescending(kv => kv.Value))
    Inventory.Add(new WorldInventoryRowViewModel(netId, 0, 0, _itemNames.GetName(netId), count, null, ...));
```

`ChestItemCounts` es un `IReadOnlyDictionary<int,int>` (`Core/WldFormat/WorldPresenceIndex.cs:32`):
**agregado por id de objeto sobre todos los cofres del mundo**. La identidad del cofre se pierde
en el censo, antes de llegar a la ViewModel.

**Los datos para hacerlo bien ya están leídos y en memoria.** `WldChest`
(`Core/WldFormat/WldChest.cs:14-20`) tiene:

```csharp
public required int X { get; init; }
public required int Y { get; init; }
public required string Name { get; init; }
public required IReadOnlyList<WldChestItem> Items { get; init; }
```

y `_world.Chests` está poblada por `WldReader.ReadChests` (`:356-385`) con el nombre real y la
lista real de objetos con `NetId`/`Stack`/`Prefix` (`WldChest.cs:7`). **No hace falta leer nada
nuevo del `.wld`.**

**Qué hace TEdit** (releído de `ESPEC-buscador-mundo-tedit.md#1.2`, para no repetir la
investigación): **tampoco agrupa por cofre**. `SearchContainers` (`FindSidebarViewModel.cs:252-271`)
recorre `world.Chests` y, dentro, `chest.Items`, y emite un resultado **por objeto encontrado**,
usando *"la coordenada del CONTENEDOR, no la del ítem"*, con lo que **un cofre con dos objetos que
casan da dos filas en la misma posición**. TEdit no es aquí una fuente de diseño a copiar: en este
punto Terrakeep tendría que ir por delante, no por detrás.

**Dato de escala real que falta**: no puedo dar el número de cofres del mundo real de esta máquina
sin ejecutar la app (§13). `ExplorationViewModel` sí lo expone ya como `ChestsPillCount`.

---

## 3. Librería y Buffs

### L1 · Media · "Indice" y "Calamity (mod)" son los dos únicos nodos del árbol de buffs sin icono

> *"en la librería de buff índice y calamity mod siguen sin Sprite"*

**El puntero del encargo apunta al sitio equivocado** (ver §8). No faltan ficheros de icono de
buff: los conté.

| Catálogo | Entradas | Ficheros PNG reales | Cobertura |
|---|---|---|---|
| Buffs vanilla (`Assets/vanilla/buff_icons/`) | 352 nombres en `vanilla_buff_names_es.json` | **354** | completa |
| Buffs Calamity (`Assets/calamity/buff_icons/`) | 305 en `calamity/buffs.json` | **308** | completa |

Lo que el usuario está viendo no son tarjetas de buff sin sprite: son las **tarjetas de carpeta
raíz** (`ShowRootCategoryCards`, `MainWindow.xaml:2657-2680`, que muestran
`CategoryNodeViewModel.IconPath`) y los nodos del árbol de la izquierda.

**Y ahí faltan exactamente dos asignaciones**, en `Services/BuffLibraryTreeBuilder.cs`:

```csharp
// :65  — raíz "Indice"
var root = new CategoryNodeViewModel("Indice", "Indice");          // sin IconPath

// :133 — raíz "Calamity (mod)"
var root = new CategoryNodeViewModel("Calamity (mod)", "Calamity"); // sin IconPath
```

**Todos los demás nodos del árbol de buffs sí lo tienen**: las 6 carpetas curadas
(`BuildNamed:51`, `IconPath = VanillaBuffIconResolver.GetIconPath(ids[0])`), las páginas del
índice (`:74`) y las 10 categorías de Calamity (`:107`, `:122`). Y `ApplyOrderedUnion`
(`:140-150`), que es lo que rellena `ItemIdsOrdered`/`ItemCount` de un nodo intermedio a partir de
sus hijos, **no toca `IconPath`**.

**El árbol de OBJETOS sí lo hace bien**, lo que confirma que es un olvido y no una decisión:
`LibraryCategoryTreeBuilder.cs:158` pasa un `rootIcon` real a "Calamity (mod)", y `:154` pasa
`childNodes[0].IconPath` a cada categoría intermedia.

---

### L2 · Media-alta · El buscador exige las tildes: **2638 de 8821 nombres son inalcanzables sin acento**

> *"para buscar rápido no es lo mas optimo debería dejarte buscarla aun que la hayas escrito sin
> acento... eto equivale para todos los objetos del juego calamity incluido"*

**El puntero del encargo es correcto.** `LibrarySearchGrammar.Matches`
(`ViewModels/LibrarySearchGrammar.cs:19-39`) es la gramática compartida, y su comparación real es:

```csharp
string term = rawTerm.Trim().ToLowerInvariant();      // :23
...
string[] words = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
if (words.Length > 0 && words.All(target.Contains)) return true;   // :36
```

- **Mayúsculas/minúsculas: ya resuelto**, y no por `Contains` sino porque **los dos lados llegan
  ya en minúsculas** — el término en `:23` y el objetivo en cada punto de llamada
  (`i.DisplayName.ToLowerInvariant()`).
- **Diacríticos: no resuelto.** `string.Contains(string)` usa comparación **ordinal** en .NET, y
  `'é'` (U+00E9) y `'e'` (U+0065) son dos puntos de código distintos. `"cénit".Contains("cenit")`
  es `false`.

**El ejemplo exacto del usuario es real**: el objeto **4956** de `Assets/vanilla_item_names.json`
se llama literalmente `"Cénit"`.

**Alcance medido, contando sobre los ficheros reales:**

| Catálogo | Nombres | Con al menos un diacrítico | % |
|---|---|---|---|
| Objetos vanilla (`vanilla_item_names.json`) | 5455 | **1671** | 30,6 % |
| Objetos Calamity (`calamity/catalog.json`, `displayName_es`) | 2709 | **741** | 27,4 % |
| Buffs vanilla (`vanilla_buff_names_es.json`) | 352 | **110** | 31,3 % |
| Buffs Calamity (`calamity/buffs.json`) | 305 | **116** | 38,0 % |
| **Total** | **8821** | **2638** | **29,9 %** |

**Casi un tercio del catálogo entero.** Muestra real de nombres afectados: *Máscara de clorofita,
Tinte flamígero, Lámpara araña de cobre, Aflicción, Emblema Acuático, Égida Asgardiana, Cáliz del
Dios de la Sangre, Crío Piedra, Gelatina Astral*.

**Puntos de llamada afectados (10, todos):**

| Fichero | Línea | Superficie |
|---|---|---|
| `LibraryViewModel.cs` | 139 | Librería de objetos |
| `BuffLibraryViewModel.cs` | 82 | Librería de buffs |
| `ResearchViewModel.cs` | 194 | Investigación |
| `MainViewModel.cs` | 851 | "Buscar en el personaje" (¿Dónde lo tengo?) |
| `ExplorationViewModel.cs` | 1252, 1259, 1266, 1273, 1287 | Buscador del mundo: tiles / paredes / NPCs / líquidos / objetos en cofres |
| `ExplorationViewModel.cs` | 1293 | Texto de letreros |

**Y hay un undécimo sitio que NO pasa por la gramática** y tiene el mismo problema:
`ExplorationViewModel.ApplyInventoryFilter` (`:551-558`) usa
`row.Name.Contains(WorldSearchText, StringComparison.OrdinalIgnoreCase)` — insensible a
mayúsculas, sensible a diacríticos. Es el filtro de las listas de Cofres/Minerales/Objetos.

---

### L3 · Alta · Información de "set completo" ausente — dos causas independientes, las dos medidas

> *"en librería y en ciertos objetos de armadura sigue faltando la información de set completo
> toca revisar"*

**Dónde se muestra hoy** (para acotar): la tarjeta de la Librería enseña el bono en su tooltip, a
través de `LibraryItemViewModel.StatsTooltip` (`LibraryItemViewModel.cs:19`,
`MainWindow.xaml:1000-1001`), que sale de `ItemStatsFormatter.Format` — el cual añade la sección
`"Con el set completo: ..."` en dos ramas: Calamity (`Core/Data/ItemStatsFormatter.cs:75`) y
vanilla (`:100-102`). Así que **la Librería sí sabe mostrarlo**; el problema es de **datos**.

#### L3-a · Vanilla: faltan **20 de las 63** bonificaciones reales del juego

Extraje del juego real la lista completa de claves `ArmorSetBonus.*` que asigna
`Player.UpdateArmorSets` (`tModLoader/Terraria/Player.cs`, 63 claves distintas) y la comparé con
las 43 claves presentes en `Assets/vanilla_armor_sets.json` (177 ids de objeto):

**Faltan las 20 siguientes** — y las 20 **sí tienen texto real en español** en
`TerrariaVanilla/Terraria.Localization.Content.es-ES.Game.json` (lo comprobé una por una):

```
AdamantiteCaster  AdamantiteMelee  AdamantiteRanged
CobaltCaster      CobaltMelee      CobaltRanged
MythrilCaster     MythrilMelee     MythrilRanged
Titanium
BeetleDamage      BeetleDefense    Spider   Bee   Angler
Solar             Vortex           Nebula
Wizard            MagicHat
```

Contado del otro lado: de las **216 piezas de armadura vanilla NO de vanidad**
(`vanilla_categories.json`, categoría `"Armadura"`), **39 no tienen ninguna entrada** en
`vanilla_armor_sets.json`. **20 de esas 39 son las cuatro escaleras de mineral de Hardmode**
(Cobalto, Mithril, Adamantita, Titanio: 5 piezas cada una), es decir, exactamente el equipo que
más se toca en la mitad de una partida. (Las otras 19 sí carecen legítimamente de bono en el juego
real: vestidos de gema, casco de buceo, gorro para la lluvia, piezas sueltas.)

#### Por qué faltan: reproduje el extractor instrumentado

Copié `scripts/extraer-sets-armadura.py` al scratchpad, le cambié la ruta de salida y lo ejecuté
con un diagnóstico añadido. Su propia salida:

```
slots reales: {'head': 235, 'body': 170, 'legs': 144}
57 claves reales de ArmorSetBonus resueltas con al menos una combinacion
177 item ids reales con bonificacion de set mapeada

NUNCA RESUELTAS: ['AdamantiteMelee','AdamantiteRanged','CobaltMelee','CobaltRanged',
                  'MythrilMelee','MythrilRanged']

RESUELTAS PERO PERDIDAS EN EL PASO 4 (slot->itemId = None):
  AdamantiteCaster [((-999, 19, 18), None, 403, 404)]
  CobaltCaster     [((-999, 17, 16), None, 374, 375)]
  MythrilCaster    [((-999, 18, 17), None, 379, 380)]
  Titanium         [((-999, 56, 51), None, 1218, 1219)]
  Angler  [((161,169,104), None,None,None)]   Bee   [((160,168,103), None,None,None)]
  BeetleDamage [((157,105,98), ...)]  BeetleDefense [((157,106,98), ...)]
  Nebula  [((170,176,111), ...)]  Solar [((171,177,112), ...)]
  Spider  [((162,170,105), ...)]  Vortex [((169,175,110), ...)]
  MagicHat [((159,58,-999), None,1282,None), ... 8 combinaciones]
  Wizard   [((14,58,-999),  238,1282,None), ... 8 combinaciones]
```

**Son tres fallos distintos, no uno:**

**Modo 1 — el `if` anidado se salta entero (10 claves: los 4 minerales de Hardmode).** El juego
real escribe estos sets así (`Player.cs:14654-14670`):

```csharp
if (body == 17 && legs == 16) {
    if      (head == 29) { setBonus = ...CobaltCaster; }
    else if (head == 30) { setBonus = ...CobaltMelee;  }
    else if (head == 31) { setBonus = ...CobaltRanged; }
}
```

El parser (`extraer-sets-armadura.py:190-222`) encuentra el `if` EXTERIOR, coge con `KEY_RE` la
**primera** clave que ve dentro (`CobaltCaster`), evalúa `candidates_for` sobre la condición
exterior —que **no menciona `head`**— y por tanto usa el centinela `-999`, produciendo la terna
imposible `(-999, 17, 16)`. Después hace `i = brace_close + 1` (`:222`), **saltándose el bloque
entero** y con él los `else if` de `CobaltMelee` y `CobaltRanged`, que nunca llegan a verse. En el
paso 4 (`:246-254`) `slot_by_kind["head"].get(-999)` es `None` → `continue` → se pierde también
`CobaltCaster`. Idéntico para Mithril, Adamantita y Titanio.

**Modo 2 — el inverso `slotIndex → itemId` está incompleto (8 claves).** El paso 1 recupera sólo
**235 índices de casco** de `Item.cs`, y los índices altos (157, 159, 160, 161, 162, 169, 170,
171 → Escarabajo, Sombrero mágico, Abeja, Pescador, Araña, Vórtice, Nebulosa, Solar) no están en
esa tabla, así que `slot_by_kind["head"].get(...)` devuelve `None`. Nótese que **Estelar sí se
extrajo** con la misma forma de condición (`Player.cs:14874`, `head == 189 && body == 190 && legs == 130`):
lo que falla no es el patrón sintáctico, es la tabla de inversión.

**Modo 3 — el formato de salida no admite sets de DOS piezas (2 claves).** `Wizard`
(`Player.cs:14548`) y `MagicHat` (`:14553`) son casco + túnica, **sin pieza de piernas**. El paso
4 exige los tres ids (`:249-251`) y descarta la combinación. En el caso de `Wizard` el casco
(238) y los cuerpos (1282…4256) **sí se resolvieron**; se tira sólo porque falta `legs`.

#### L3-b · Calamity: **ninguna** pieza de cuerpo o piernas tiene bono, y eso mata una función entera

Este es el hallazgo más grave de la sección. Contando sobre `Assets/calamity/catalog.json`:

- 186 objetos de categoría `Armor*`; 55 de `Armor/Vanity`; **131 de armadura real**.
- **69 piezas tienen `setBonus`. Las 69 son cascos** (`equipSlot == "Head"`).
- **0 piezas de `Body` o `Legs` tienen `setBonus`. Cero, en todo el catálogo.**

Ejemplo real, el set Aeroespacial completo:

```
AerospecBreastplate   slot=Body   bonus=(NULO)
AerospecHeadMagic     slot=Head   bonus=Reduce el coste de maná un 8%...
AerospecHeadMelee     slot=Head   bonus=Aumenta un 5% la velocidad de movimiento...
AerospecHeadRanged    slot=Head   bonus=...
AerospecHeadRogue     slot=Head   bonus=+80 de sigilo máximo...
AerospecHeadSummon    slot=Head   bonus=Aumenta en 1 el máximo de súbditos...
AerospecLeggings      slot=Legs   bonus=(NULO)
```

**Consecuencia 1 — la Librería (lo que el usuario reportó).** `ItemStatsFormatter.cs:75`
(`if (entry?.SetBonus != null) sections.Add($"Con el set completo: {entry.SetBonus}")`) sólo puede
disparar sobre cascos. **Las 62 piezas de cuerpo y piernas de los 31 sets de Calamity nunca
muestran la línea.** Eso es, literalmente, *"en librería y en ciertos objetos de armadura sigue
faltando la información de set completo"*.

**Consecuencia 2 — el "Bono activo" de Equipamiento es código muerto.**
`EquipmentGroupViewModel.ActiveCalamitySetBonusText` (`:216-231`) termina en:

```csharp
string? headBonus = _service.CalamityCatalog.BySyntheticId(head.Item.Id)?.SetBonus;
string? bodyBonus = _service.CalamityCatalog.BySyntheticId(body.Item.Id)?.SetBonus;
string? legsBonus = _service.CalamityCatalog.BySyntheticId(legs.Item.Id)?.SetBonus;
if (string.IsNullOrEmpty(headBonus) || headBonus != bodyBonus || headBonus != legsBonus) return null;
return headBonus;
```

Con `bodyBonus == null` y `legsBonus == null` **siempre**, la condición se cumple **siempre** y el
método devuelve `null` **siempre**. El comentario que lo justifica (`:210-215`) dice: *"las 3
piezas (cabeza/cuerpo/piernas) de Calamity llevan el MISMO texto de `SetBonus` cuando forman un
set real"* — **esa premisa es falsa contra los datos reales del propio proyecto**. La corrección
`B-6/F3` de la segunda auditoría se dio por hecha y nunca ha funcionado ni una vez.

**Simetría con L3-a que conviene ver**: Calamity tiene, igual que Cobalto/Mithril/Adamantita/
Titanio, **varios cascos por set con bonos distintos** (Aeroespacial tiene cinco: mágico, cuerpo a
cuerpo, a distancia, pícaro, invocación). La causa raíz de las dos mitades es la misma:
**la bonificación pertenece al SET (cuerpo + piernas + cuál de los cascos), no a la pieza**, y los
dos modelos de datos la guardan por pieza.

---

### L4 · Media · El arrastre desde la Librería existe, pero nada lo anuncia (y el clic falta en Buffs)

> *"la opción arrastrar objetos desde la librería también ha desaparecido esta muy bien que sea
> por selector pero que además también este la opción de arrastrar... pasa lo mismo con la
> pestaña buff"*

**Confirmo el puntero del encargo: el código de arrastre existe entero y está bien cableado.**
Verifiqué las cuatro piezas, no sólo el origen:

| Pieza | Fichero:línea | Estado |
|---|---|---|
| Origen (tarjeta de objeto) | `MainWindow.xaml:994` + `MainWindow.xaml.cs:754-766` | presente |
| Origen (tarjeta de buff) | `MainWindow.xaml:1058` + `MainWindow.xaml.cs:962-972` | presente |
| Destino (slot de objeto) | `MainWindow.xaml:311-313` (`AllowDrop="True"`, `DragOver`, `Drop`) | presente |
| Destino (slot de buff) | `MainWindow.xaml:783-785` | presente |
| Manejadores | `MainWindow.xaml.cs:846-868` (`DragOver`), `:872-885` (`Drop`), `:978-1000` (buffs) | presentes |

Además comprobé que **`SlotCompactTemplate` es la única plantilla de slot de objeto del proyecto**
(`MainWindow.xaml:305`, usada en `:542` por `ContainerCompactTemplate`), así que **todos** los
contenedores son destino válido; y que la doble ruta clic/arrastre no coloca dos veces (la guarda
`_dragStartLibrary is null` en `OnLibraryCardClick`, `:778`).

**Descarté la interferencia de F-13.** `AllowDrop="True" Drop="OnWindowDrop"` en la ventana
(`MainWindow.xaml:14`, añadido en `1343e838`) es el sospechoso natural, pero `OnWindowDrop`
(`MainWindow.xaml.cs:298-300`) hace `if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;`
**sin marcar `e.Handled`**, y no hay ningún manejador de `DragOver`/`DragEnter` a nivel de
ventana. Los destinos internos marcan `e.Handled = true` en su propio `DragOver` (`:867`, `:986`),
así que el evento nunca burbujea hasta la ventana. **No hay conflicto.**

**Lo que sí encontré, y son dos defectos reales:**

**(a) El efecto de arrastre que pide el destino no está en el conjunto que permite el origen.**

```csharp
// origen, MainWindow.xaml.cs:765 y :971
DragDrop.DoDragDrop(element, new DataObject(...), DragDropEffects.Copy);

// destino, MainWindow.xaml.cs:867 y :986
e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
```

Por contrato de OLE, `IDropTarget::DragOver` debe devolver **uno de los efectos que el origen
declaró** en `dwOKEffect`. Aquí el origen permite sólo `Copy` y el destino contesta `Move`. La
realimentación visual del arrastre (`GiveFeedback`) se calcula con lo que devuelve el destino, así
que **el cursor que ve el usuario no corresponde a la operación permitida**. Es una incoherencia
real y demostrable en el código; no puedo afirmar sin ejecutar que llegue a impedir el soltado
(§13).

**(b) Nada anuncia el gesto — y lo que sí hay anuncia explícitamente los OTROS dos gestos.** El
tooltip de la tarjeta de la Librería (`MainWindow.xaml:996-1007`), añadido por `H5-12`, dice
literalmente:

> *"Clic: coloca en el hueco seleccionado en Editar. Doble clic: al primer hueco libre del
> Inventario."*

**No menciona el arrastre.** Y cuando no hay hueco seleccionado, el segundo texto
(`:1005-1007`) enumera de nuevo sólo esos dos gestos. El cursor es `Cursor="Hand"` (`:993`), que
es la señal de "esto se pulsa", no de "esto se arrastra". La única mención de arrastrar en toda la
Librería vive en la barra de modo-selección (`MainWindow.xaml:2384-2385`, *"Elige (o arrastra) un
objeto..."*), que **sólo aparece con `Library.IsPicking`** — es decir, casi nunca.

Esto explica por sí solo la percepción de "ha desaparecido": **`H5-12` (commit `39d7f656`) añadió
el clic como gesto principal y documentó sólo el clic**, dejando el arrastre —hasta entonces el
único camino— sin ninguna señal. El propio comentario del código lo dice al revés y sin darse
cuenta (`MainWindow.xaml.cs:775-776`): *"la unica via real es arrastrar, gesto mas caro que nada
anuncia"*. Se arregló la mitad del problema.

**(c) Asimetría real entre las dos librerías, en el sentido contrario.** La tarjeta de la
Librería de **objetos** tiene `MouseLeftButtonUp="OnLibraryCardClick"` (`MainWindow.xaml:994`).
La de **buffs** (`BuffLibraryCardTemplate`, `:1056-1110`) tiene sólo
`PreviewMouseLeftButtonDown` y `MouseMove` (`:1058`) — **ni manejador de clic, ni `InputBindings`,
ni `Command` en el `Border`**. En la Librería de buffs, **un clic simple sobre una tarjeta no hace
absolutamente nada** salvo que haya un slot esperando (`IsPicking`), único caso en que aparece el
botón "Colocar" (`:1102-1104`). `H5-12` nunca se aplicó a los buffs. El *"pasa lo mismo con la
pestaña buff"* del usuario apunta, en realidad, a un hueco distinto y mayor.

---

## 4. Inicio y barra superior global

### H1 · Media · Las insignias de la tarjeta de personaje envuelven **sin ningún hueco vertical**

> *"las tarjetas de personaje de inicio la etiqueta calamity se solapa con la etiqueta del nucleo
> del personaje"*

**El puntero del encargo es correcto** (`CharacterCardTemplate`, `MainWindow.xaml:1114`);
`CharacterListEntryViewModel.cs` existe y alimenta las dos etiquetas (`DifficultyLabel:22`,
`IsCalamity:29`).

**Geometría real, sumada de las declaraciones del propio XAML:**

| Elemento | Línea | Valor |
|---|---|---|
| Tarjeta `Border` | `1115-1116` | `Padding="12"`, `Width="240"`, `BorderThickness="1"` (2 si `IsCurrent`, `:1164`) |
| Columna 0 | `1173` | `Image Width="52"` |
| Columna 1 | `1174` | `StackPanel Margin="12,0,0,0"` |

```
ancho útil del WrapPanel = 240 − 2×12 (padding) − 2×1 (borde) − 52 (doll) − 12 (margen) = 150 px
                                         (148 px si la tarjeta es la del personaje cargado)
```

**Las cuatro insignias que caben ahí** (`WrapPanel`, `MainWindow.xaml:1188-1226`), todas a
`FontSize="10"` con `Padding="5,1"`:

| Insignia | Línea | Margen | Texto real |
|---|---|---|---|
| Dificultad | `1189-1191` | **ninguno** | `Softcore` / `Mediumcore` / `Hardcore` / `Journey` (`CharacterListEntryViewModel.cs:52-58`) |
| Vanilla | `1200-1205` | `5,0,0,0` | `Vanilla` |
| tModLoader | `1211-1216` | `5,0,0,0` | `tModLoader` (SemiBold) |
| Calamity | `1221-1226` | `5,0,0,0` | `Calamity` (SemiBold) |

Un personaje de Calamity muestra **tres a la vez** (dificultad + tModLoader + Calamity;
`IsVanilla` es la negación de `IsTModLoader`, `CharacterListEntryViewModel.cs:31`, así que Vanilla
y tModLoader se excluyen). Sumando padding y márgenes, `Mediumcore` + `tModLoader` + `Calamity`
pide del orden de **180 px contra los 150 disponibles**: **el envolvido a una segunda línea es el
caso normal, no el excepcional**, para cualquier personaje de Calamity con dificultad de nombre
largo.

**Y ahí está el defecto real: `WrapPanel` no añade ningún hueco entre líneas**, y **ninguna de las
cuatro insignias tiene margen vertical** (`5,0,0,0` es sólo horizontal; la de dificultad no tiene
margen ninguno). Al envolver, el borde inferior de la insignia de arriba queda **pegado** al borde
superior de la de abajo — dos `Border` con `CornerRadius="3"` a tope, que es exactamente lo que se
lee como "se solapa".

Hay además una incoherencia menor: la insignia de dificultad es la única sin `Margin`, así que si
en la segunda línea cae otra insignia, ésta arranca con 5 px de sangría que la primera línea no
tiene.

**Contexto**: `R-05/H-05` de la auditoría de redimensionado ya cambió aquí un `StackPanel`
horizontal por este `WrapPanel`, precisamente porque *"la fila entera desbordaba el Width=240 y
recortaba la última en silencio - hasta 39 de 40px, medido"* (comentario real, `:1182-1187`). El
arreglo resolvió el recorte y **creó** el amontonamiento vertical.

---

### H2 · Baja · Las tarjetas de personaje no tienen animación de hover; las de "Qué más puedes hacer" sí

> *"estaría bien que tuviera alguna animación cuando pasas el ratón por encima de ellas como las
> tarjetas de ''que mas puedes hacer'' no tienen por que ser iguales"*

**Lo que SÍ tienen las tarjetas de "Qué más puedes hacer"** (`MainWindow.xaml:1762` es el título;
las cinco tarjetas están en `:1771`, `:1779`, `:1787`, `:1795`, `:1803`, todas con
`Style="{StaticResource NavCardButton}"`). El estilo real, `Styles/Theme.xaml:1096-1160`:

```xml
<Border.RenderTransform>
    <TranslateTransform x:Name="BdLift" Y="0" />
</Border.RenderTransform>
...
<Trigger Property="IsMouseOver" Value="True">
    <Setter TargetName="Bd" Property="Background" Value="{StaticResource BgHoverBrush}" />
    <Setter TargetName="Bd" Property="Effect"     Value="{StaticResource CardShadowHover}" />
    <Trigger.EnterActions><BeginStoryboard><Storyboard>
        <DoubleAnimation Storyboard.TargetName="BdLift" Storyboard.TargetProperty="Y"
                         To="-3" Duration="0:0:0.15" />
        <ColorAnimation  Storyboard.TargetName="BdBorderBrush" Storyboard.TargetProperty="Color"
                         To="{StaticResource AccentColor}" Duration="0:0:0.15" />
    </Storyboard></BeginStoryboard></Trigger.EnterActions>
    <Trigger.ExitActions> ... vuelta a Y=0 y a BorderColor, misma duración ... </Trigger.ExitActions>
</Trigger>
```

Tres capas: **levanta 3 px en 150 ms**, la sombra crece, y el borde **se tiñe del acento**
animado, con salida simétrica.

**Lo que tienen las tarjetas de personaje** (`CharacterCardTemplate`,
`MainWindow.xaml:1152-1167`), entero:

```xml
<Trigger Property="IsMouseOver" Value="True">
    <Setter Property="Background" Value="{StaticResource BgHoverBrush}" />
    <Setter Property="Effect"     Value="{StaticResource CardShadowHover}" />
</Trigger>
```

**Dos `Setter` instantáneos, sin `Storyboard`, sin `RenderTransform`, sin transición.** Los mismos
recursos (`BgHoverBrush`, `CardShadowHover`) que usa `NavCardButton`, aplicados de golpe.

**Restricción real que la propuesta debe respetar** (y que no es obvia): la tarjeta de personaje
**ya usa su `BorderBrush` para otra cosa** — el `DataTrigger` de `IsCurrent` lo pone en acento con
grosor 2 (`:1160-1163`) para marcar el personaje cargado (hallazgo `I-a`). Copiar tal cual la capa
de "borde animado a acento" de `NavCardButton` haría que **cualquier** tarjeta pareciera la
cargada al pasar el ratón, reproduciendo exactamente el bug `L-6` que ya se corrigió en la tarjeta
de la Librería (documentado en `MainWindow.xaml:1015-1023`: *"el hover ponia TAMBIEN el borde en
acento... sobre una tarjeta de Calamity, el borde de hover nunca se veia"*).

Nota adicional: la tarjeta de personaje es un `Border` con `InputBindings` y `ContextMenu`
(`:1119-1149`), no un `Button`, así que **no puede simplemente adoptar `NavCardButton`**.

---

### H3 · Alta · La barra superior no tiene **ni un píxel** de separación entre sus tres columnas

> *"tanto la vida como el mana prácticamente se solapan entre ellas y en el lado izquierdo se
> junta con softcore todo esto en el mínimo de ventana permitido y a pantalla completa se sigue
> juntando la vida con softcore"*

La observación del usuario de que ocurre **igual a pantalla completa** es el dato clave: descarta
que sea sólo un problema de ancho. Y efectivamente lo es.

**La estructura real** (`MainWindow.xaml:1379-1391`):

```xml
<Border Grid.Row="0" ... Padding="16,8">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />   <!-- 0: identidad -->
            <ColumnDefinition Width="*" />      <!-- 1: vitales / mundo -->
            <ColumnDefinition Width="Auto" />   <!-- 2: 6 botones -->
        </Grid.ColumnDefinitions>
```

**Y los tres paneles hijos, verificados uno por uno:**

| Columna | Elemento raíz | Línea | `Margin` |
|---|---|---|---|
| 2 (botones) | `StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center"` | `1393` | **ninguno** |
| 0 (identidad) | `StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center"` | `1458` | **ninguno** |
| 1 (vitales) | `WrapPanel Grid.Column="1" VerticalAlignment="Center"` | `1538` | **ninguno** |
| 1 (mundo, Exploración) | `WrapPanel Grid.Column="1" VerticalAlignment="Center"` | `1584` | **ninguno** |

**El `Padding="16,8"` del `Border` sólo separa del borde de la ventana. Entre columnas el hueco es
exactamente 0 px, a cualquier ancho de ventana.** Eso es, literalmente:

- la columna 0 termina en la píldora de dificultad (`:1507-1509`) o la de Calamity (`:1510-1513`)
  y la columna 1 empieza con el `TextBlock "♥"` (`:1541`) → **"Softcore" y el corazón, pegados**,
  igual a 1080 px que a 3840;
- la columna 1 termina y la columna 2 empieza con *"Cargar personaje (.plr)..."* (`:1404`) →
  el mismo 0 px al otro lado.

**Dentro de la propia franja vital, el ritmo también es incorrecto** (`:1540-1556`):

```xml
<TextBlock Text="♥" FontSize="13" .../>                          <!-- :1541  sin margen -->
<Grid Width="70" Height="10" Margin="4,0,14,0" ToolTip="Vida">   <!-- :1542 -->
<TextBlock Text="✦" FontSize="13" .../>                          <!-- :1550  sin margen -->
<Grid Width="70" Height="10" Margin="4,0,14,0" ToolTip="Maná">   <!-- :1551 -->
```

- **Dentro** de un grupo (icono ↔ su barra): **4 px**.
- **Entre** los dos grupos (barra de vida ↔ icono de maná): **14 px** — el margen derecho de la
  barra de vida es lo único que los separa.

Una razón de 3,5:1 entre "fuera" y "dentro" es demasiado baja para que el ojo agrupe: **vida y
maná se leen como una sola tira de cuatro elementos**, no como dos pares. El propio proyecto
tiene una escala de espaciado documentada, fijada en `P-5` de la auditoría anterior
(`MainWindow.xaml:3512-3514`): *"escala de espaciado unica del proyecto (4/6/10/14/20) - 4 entre
hermanos directos, 10 entre controles relacionados"*. Aquí falta el escalón de 20 para "grupos
distintos".

**Y hay un tercer factor, éste sí de ancho.** El `TextBox` del nombre (`:1477-1481`) declara
`MinWidth="80"` y **ningún `MaxWidth`**:

```xml
<TextBox Text="{Binding CharacterName, UpdateSourceTrigger=PropertyChanged}" ...
         Padding="4,2" MinWidth="80" VerticalAlignment="Center" ...>
```

Como la columna 0 es `Auto`, **un nombre largo la ensancha sin tope** y aprieta la columna `*`
central. Esto ya lo midió la auditoría de redimensionado (`§4.4`, `H-04a`): con la barra a 1080 px
*"los dos `Auto` cobran primero (identidad ~185px, los seis botones **658px** medidos por UI
Automation) y a la columna `*` le quedan ~192px para una tira que necesita 196"*. La corrección
`R-04a` cambió el `StackPanel` por este `WrapPanel` para que nada se perdiera, y `R-04b` subió
`NormalMinWidth` de 1300 a 1320 (`MainViewModel.cs:379-387`). **Las dos corrigieron el recorte;
ninguna tocó el hueco entre columnas ni el ritmo interno**, que es lo que el usuario está viendo
ahora — y por eso persiste a pantalla completa, donde el apretujamiento ya no existe.

Y como el `WrapPanel` tampoco tiene hueco entre líneas (mismo defecto que H1), cuando sí envuelve
en ventana compacta, la fila de vida y la de maná quedan pegadas verticalmente.

---

## 5. Apariencia

### A1 · Media-alta · Deshacer/Rehacer no cubre **ninguno** de los 18 valores editables de Apariencia

> *"las flechas deshacer no aplica a las pestaña de apariencia así que sin querer tocas el pelo o
> algo no puedes tirar para atrás"*

**Confirmado, y el alcance real es mayor de lo que sugería el puntero.** Busqué `UndoStack` y
`UndoEntry` en todo `TerrasavrNative.App/`: las únicas apariciones fuera de
`Services/UndoStack.cs` son `MainViewModel.cs` y **dos comentarios** en `ItemSlotViewModel.cs`
(`:21`, `:229`). `AppearanceViewModel.cs` no lo menciona ni una vez.

**Los tres únicos productores de entradas de historial que existen**, todos en `MainViewModel.cs`:

| Línea | Qué registra | Rótulo real |
|---|---|---|
| `180-186` | `OnSlotItemChanged` — un slot de objeto | `"{ContainerName} · slot {N}"` |
| `237-242` | `RunAsUndoableBatch` — operación en bloque de objetos | el que se le pase |
| `1512-1517` | `RunAsUndoableBuffBatch` — operación en bloque de buffs | el que se le pase |

**Es decir: el historial cubre exclusivamente slots de objeto y de buff.**

**Lo editable en Apariencia que hoy es irreversible** (`AppearanceViewModel.cs`), 18 valores:

| Valor | Línea | Escritura al modelo |
|---|---|---|
| `HairStyle` (peinado) | `34` | `:252-257` → `_character.HairStyle` |
| `HairDye` (tinte) | `35` | `:259-264` → `_character.HairDye` |
| `IsMale` / variante de piel | `51` | `:271-277` → `_character.Gender` |
| `Difficulty` (núcleo) | `168` | `:279-284` → `_character.Difficulty` |
| `HealthNow` / `HealthMax` | `169-170` | `:298-314` |
| `ManaNow` / `ManaMax` | `171-172` | `:315-331` |
| `FishingQuestsCompleted` | `173` | `:338` |
| `GolferScore` | `174` | `:339` |
| `PlayHours` | `184` | `:341-347` |
| **7 colores** (`Swatches`: Pelo, Piel, Ojos, Camisa, Camiseta interior, Pantalones, Zapatos) | `189-196` | `ColorSwatchViewModel`, escritura directa al `byte[]` real |

El caso que el usuario cita —*"sin querer tocas el pelo"*— es el peor de todos: el peinado se
cambia desde un selector visual de **228 miniaturas** (`RebuildHairOptions`, `:84-95`), donde un
clic accidental es fácil, y **no hay ninguna forma de volver atrás** salvo recordar el número
anterior.

**Alcance completo, para que la propuesta no se quede corta**: por el mismo grep, tampoco están
cubiertas por el historial las pestañas **Banderas/Desbloqueos**, **Investigación**, **Spawn
Points**, **Versión**, ni el `CharacterName` de la barra superior (`MainViewModel.CharacterName`).
Apariencia es la que el usuario ha notado porque es donde un clic accidental cuesta más.

**Lo que juega a favor**: `UndoEntry` (`Services/UndoStack.cs:13-18`) es deliberadamente agnóstico
del dominio —`Label` + dos `Action` por cierre— y su propio comentario lo dice
(`Services/UndoStack.cs:9-11`): *"sin que esta clase necesite saber NADA del dominio
(objetos/buffs/investigacion/desbloqueos serian todos igual de validos aqui)"*. La infraestructura
está preparada; falta el cableado.

---

## 6. Novedades y Acerca de

### N1 · Media · Novedades: **0 de 40 sprites vanilla se resuelven**, y ningún objeto tiene descripción al pasar el ratón

> *"quedara actualizar la pestaña de novedades con sprite reales de los objetos que se incorporan
> nuevos al juego de terrria en sus diferentes versiones... con su pequeña descripcion de cada
> objeto al pasar el raton por encima"*

**Estado real, primero: la infraestructura de sprites YA existe.** La corrección `N-d` de la
segunda auditoría ya la montó — `WhatsNewItemViewModel.ForVanilla` resuelve por nombre interno
contra `VanillaItemCatalog.GetIdByKey` → `VanillaIconResolver.GetIconPath`
(`WhatsNewItemViewModel.cs:22-26`), y la píldora del XAML (`MainWindow.xaml:709-722`) ya pinta el
`Image` de 18×18.

**Pero no resuelve nada.** Lo medí sobre los ficheros reales:

| | Objetos declarados | Con `key` | **Con sprite real** |
|---|---|---|---|
| `whats_new_vanilla.json` | 40 | 40 | **0** |
| `whats_new_calamity.json` | 4 | 4 | **4** |

**La causa, y es más grande que la pestaña Novedades.** Comparé los rangos de todos los catálogos
vanilla del proyecto contra las dos fuentes decompiladas que hay en esta máquina:

| Fichero | Entradas | id máximo |
|---|---|---|
| `Assets/vanilla_item_names.json` | 5455 | 5455 |
| `Assets/vanilla_item_ids_by_key.json` | 5455 | 5455 |
| `Assets/vanilla_item_names_by_key.json` | 5455 | — |
| `Assets/vanilla_categories.json` | 4575 | 5455 |
| `Assets/vanilla_item_tooltips.json` | 2518 | 5455 |
| `Assets/vanilla_stats.json` | 2576 | 5452 |
| **`Assets/vanilla/icons/*.png`** | **6176 ficheros** | **6195** |

```
tModLoader/Terraria/ID/ItemID.cs        ->  Count = 5456    (Terraria 1.4.4.9)
TerrariaVanilla/Terraria/ID/ItemID.cs   ->  Count = 6196    (Terraria 1.4.5.8)
```

**Todos los catálogos de nombres/datos vienen del árbol de tModLoader (1.4.4.9). La carpeta de
iconos viene del árbol vanilla (1.4.5.8).** Son 740 objetos de diferencia.

Los 40 "objetos nuevos" de la entrada 1.4.5.7 de `whats_new_vanilla.json` **son justamente objetos
de 1.4.5**: `ArcSurge`, `BejeweledStaff`, `MobiusStrip`… Y lo comprobé:

- **NO** están en `vanilla_item_ids_by_key.json` (5455 claves; `ArcSurge` no aparece).
- **SÍ** están en `TerrariaVanilla/Terraria/ID/ItemID.cs`:
  `MobiusStrip = 6158` (`:13862`), `BejeweledStaff = 6171` (`:13888`), `ArcSurge = 6173` (`:13892`).
- **Y sus sprites YA ESTÁN EN DISCO**: `Assets/vanilla/icons/` llega hasta el id 6195, extraída
  de la instalación real de Steam (documentado en `Services/VanillaIconResolver.cs:5-19`:
  *"6134/6196 ids (0..6195) tienen icono real"*).

**Es decir: la mitad "sprites reales" de N1 no es alcance nuevo. Es un mapa nombre→id que se
quedó una versión del juego por detrás.** Falta un único fichero regenerado.

**Matiz importante que hay que decirle al usuario antes de decidir nada** (§9.11): **ampliar el
catálogo de la Librería a 1.4.5 sería un error**. Terrakeep edita `.plr`/`.tplr` de
**tModLoader 1.4.4.9** (que es la versión sobre la que corre Calamity). Un id de objeto de 1.4.5
no significa nada en un guardado de tModLoader 1.4.4.9; colocarlo en un slot produciría un objeto
inválido. **La ampliación tiene sentido para Novedades —que es una pestaña de sólo lectura sobre
el juego base— y no para la Librería.**

**La otra mitad de N1 —la descripción al pasar el ratón— sí falta por completo.** La píldora de
objeto (`MainWindow.xaml:709-722`) es:

```xml
<Border Background="{StaticResource BgElevatedBrush}" CornerRadius="4" Margin="0,0,6,6" Padding="7,3">
    <StackPanel Orientation="Horizontal">
        <Image Source="{Binding IconPath}" Width="18" Height="18" ... />
        <TextBlock Text="{Binding DisplayName}" Style="{StaticResource CaptionText}" VerticalAlignment="Center" />
    </StackPanel>
</Border>
```

**Sin `ToolTip`, y `WhatsNewItemViewModel` (26 líneas) sólo tiene `DisplayName` e `IconPath`.**
Pero el proyecto **ya sabe generar exactamente el tooltip que el usuario describe**:
`ItemStatsFormatter.Format(...)` es lo que alimenta el tooltip de la Librería (daño/DPS, defensa,
crítico, velocidad de uso, retroceso, descripción real del juego, bono de set). Es la misma
llamada que hace `LibraryViewModel.cs:65` y `:75`.

**Sobre el alcance de contenido** (esto es decisión del usuario, no de código): hoy hay
**2 versiones vanilla** (1.4.5.8, con 0 objetos, y 1.4.5.7, con 40) y **5 de Calamity**
(2.2.0 a 2.2.4, con 4 objetos en total). *"sus diferentes versiones"* implicaría escribir muchas
más entradas a mano. `H3-07` de la tercera auditoría ya dejó este punto marcado como **decisión de
contenido pendiente del usuario** (`bitacora.md:5622-5625`), y sigue abierto.

---

### N2 · Media · Acerca de: sin el nombre del autor y con el registro congelado desde hace **170 commits**

> *"actualizar tambien la pestaña de acerca de con toda la infomacion nueva... ademas hay que
> poner el nombre del creador osea yo... que mi nombre sera ''IncrediBad'' respetando las
> mayusculas"*

**Estado real de la pestaña** (`MainWindow.xaml:4458-4545`), en orden:

1. Logo 128 + `About.AppName` + `About.Tagline` + versión (`:4461-4470`).
2. `About.CreditsText`, un bloque de texto corrido (`:4471-4472`).
3. **Ajustes** — carpetas adicionales de personajes y de mundos, tope de copias de seguridad
   (`:4479-4526`, añadido por `H5-07`).
4. **"¡Sobre esta versión!"** — el registro de cambios del propio Terrakeep
   (`:4530-4542`, `Changelog.Entries`).

**Hallazgo 1 — no hay ningún autor.** `ViewModels/AboutViewModel.cs` son 26 líneas con tres
propiedades y ninguna de autoría:

- `AppName` = `"Terrakeep"` (`:10`)
- `Tagline` (`:11`)
- `Version` — leída del ensamblado (`:12`)
- `CreditsText` (`:14-26`) — atribuye a **Re-Logic**, al equipo de **tModLoader**, al de
  **Calamity Mod** y a **YellowAfterlife** por Terrasavr. **Ni una palabra sobre quién escribió
  Terrakeep.**

El nombre a usar, copiado literal del encargo, es **`IncrediBad`** (I y B mayúsculas, resto
minúsculas).

**Hallazgo 2 — el registro de cambios está congelado.** `Assets/changelog.json` tiene
**3 entradas**:

```
1.2.0  — 1 de septiembre de 2026
1.1.0  — 1 de septiembre de 2026
1.0.0  — 1 de septiembre de 2026
```

El fichero se tocó por última vez en el commit **`c646e868` (1-sep-2026)**. Desde entonces hay
**170 commits** en el repositorio (`git log c646e868..HEAD`), sobre un total de 225. **Tres cuartas
partes del trabajo del proyecto no están en su propio registro de cambios.**

Y el ensamblado sigue declarando `1.2.0` (`TerrasavrNative.App.csproj:38-40`), así que la pestaña
muestra "Versión 1.2.0" para una build que no tiene nada que ver con aquélla.

**Lo que falta por contar, agrupado por lo que de verdad se hizo** (sacado de `git log` y de los
cuatro documentos `ESPEC-*`, no inventado):

| Bloque | Qué entró |
|---|---|
| Auditorías 3ª a 8ª | Deshacer/Rehacer real, copias de seguridad rotativas con historial, conjuntos de objetos y de buffs guardables, "Buscar en el personaje", navegación por teclado en los ~350 slots, sistema adaptativo por ancho **y por alto**, Ajustes reales |
| Sprites reales | 6134 iconos de objeto por fichero (fin del atlas), 1244 iconos de tile/pared, cabezas reales de NPC, doll de cuerpo completo con la armadura puesta |
| Exploración | Visor de mundos `.wld` completo, censo del mundo, buscador general (tiles/paredes/líquidos/NPCs/cofres/letreros/tile entities), marcado de vetas de mineral, barra lateral por categorías |
| `ESPEC-auditoria-redimensionado.md` | 11 correcciones (R-01…R-11) verificadas a 14 tamaños de ventana |
| `ESPEC-auditoria-exploracion-tedit.md` | 22 correcciones contra TEdit: minimapa, marcadores de tamaño constante, virtualización, barra de estado del mapa, atajos de teclado, `GridSplitter`, memoria de vista por mundo, exportar a PNG, arrastrar y soltar ficheros, barra superior reorganizada |
| Compatibilidad | Rango completo de versiones de `.plr` (Terraria 1.1.2 en adelante), lectura de `.wld` verificada campo a campo |
| Pruebas | 656 pruebas automáticas + arnés de UI Automation con etiquetas |

---

## 7. Lo que ya está bien y no hay que tocar

Comprobado de verdad en esta pasada, para que ninguna corrección lo rompa:

- **La lectura de líquidos del `.wld` es correcta**, incluida la distinción Miel/Centelleo que
  comparten código en disco (`WldReader.cs:273-289`). E6 no es un fallo de lectura.
- **`MarkOresOnMapCommand` es correcto** (`ExplorationViewModel.cs:610-641`) y su arquitectura de
  dos capas —resaltado sin tope + lista agrupada por vetas— es justo la que le falta a Objetos.
- **`WorldHighlightRenderer` dibuja un halo de 1 tile a media opacidad** alrededor de cada
  coincidencia precisamente para que se vea al alejar el zoom (`WorldHighlightRenderer.cs:34-37`,
  con la justificación medida). No hay que rehacerlo.
- **La cobertura de iconos de buff es completa** en los dos catálogos (354 y 308 ficheros). L1 no
  es un problema de assets.
- **El cableado de arrastrar y soltar está completo y correcto** en las cuatro piezas (L4).
- **F-13 no interfiere** con el arrastre interno: el filtro por `DataFormats.FileDrop` sin
  `e.Handled` es correcto.
- **El estilo de `ScrollBar` maneja bien las dos orientaciones** (`Theme.xaml:232-238`), un fallo
  que ya se corrigió y sigue corregido.

---

## 8. Punteros del encargo que resultaron estar equivocados

Se piden explícitamente, y son tres:

| Puntero del encargo | Realidad |
|---|---|
| **E3**: *"`MainWindow.xaml` ~línea 657 (`WrapPanel ItemWidth="248"` de las tarjetas de Librería)"* | **La línea 657 no es la Librería.** Es la pestaña **Builds** (`BuildStageViewModel` → `BuildClassTemplate`, `:618-661`) — el `ItemWidth="248"` casa con el `Width="220"` fijo de `BuildClassTemplate` (`:626`). La rejilla real de la Librería es un **`SlotGridPanel`** (`:2486-2494`), no un `WrapPanel`, y su problema es de alto, no de ancho de celda |
| **L1**: *"Verifica si faltan ficheros de icono reales en esas carpetas"* | **No falta ningún fichero**: 354 PNG de buff vanilla y 308 de Calamity, cobertura completa. Lo que faltan son **dos asignaciones de `IconPath`** en los nodos RAÍZ del árbol (`BuffLibraryTreeBuilder.cs:65` y `:133`). El usuario no hablaba de tarjetas de buff, hablaba de las **carpetas** "Indice" y "Calamity (mod)" |
| **E5**: *"`MarkOresOnMapCommand` ... usa `OreMetals.Concat(...)` (línea 614)"* | Ese comando **está bien** y ese `Concat` funciona. La causa real está en **`BuildSingleRowQuery` (`:594-602`), que no tiene rama para `WorldSearchCategory.Ores`** y devuelve una consulta vacía; y en que el tick **no tiene botón que lo consuma** en esta sección |

Y dos matices más:

- **E6**: la hipótesis de *"un filtro de sprite/variante U/V que reduzca 'miel' a un sub-tipo"* es
  **falsa** — no existe tal filtro. La causa es el `DisplayLimit = 1000` combinado con un barrido
  por columnas de izquierda a derecha.
- **L4**: la conclusión del encargo (*"el código de arrastre SÍ existe hoy... contradice lo que
  dice el usuario"*) es correcta en cuanto al código, pero el usuario **no está equivocado**: el
  gesto dejó de estar anunciado en `H5-12`, hay una incoherencia real de efectos `Copy`/`Move`, y
  en la Librería de **buffs** falta el manejador de clic entero.

Y un hallazgo que el encargo pedía sólo "verificar" y que resultó ser el peor de todos:

- **E4b**: el marcador de spawn del mundo de F-7 **no funciona**. `Canvas.Left`/`Canvas.Top` sobre
  un hijo directo de un `Grid` (§2, E4b).

---

# PARTE II — PROPUESTA

Todo lo que sigue es **criterio mío**, marcado como tal. No es un hecho verificado del código.

## 9. Diseño de cada corrección

### 9.1 · C-01 · Reposicionar los dos marcadores de mundo dentro de un `Canvas` real · cierra **E4b** · esfuerzo **trivial**

Moverlos a un `ItemsControl` con `ItemsPanelTemplate` = `Canvas`, exactamente igual que sus tres
vecinos, o —más simple y con menos cambio— envolver los dos `TextBlock` en un `Canvas` propio
dentro del mismo `Grid`:

```xml
<Canvas IsHitTestVisible="True">
    <TextBlock Text="&#8962;" Canvas.Left="{Binding Exploration.WorldSpawnX}" ... />
    <TextBlock Text="&#9671;" Canvas.Left="{Binding Exploration.WorldDungeonX}" ... />
</Canvas>
```

Un `Canvas` hijo de un `Grid` se estira a toda la celda y sus hijos ya sí quedan posicionados por
`Canvas.Left`/`Canvas.Top`, con lo que también deja de estirarse el `TextBlock` y el
`RenderTransformOrigin="0.5,0.5"` vuelve a escalar respecto al glifo, que es lo que se pretendía.

**Criterio mío**: esta es la corrección número uno del plan. Es una función que se anunció como
entregada, no funciona, y su coste de arreglo es de minutos.

### 9.2 · C-02 · Recortar el viewport del minimapa y activar `ClipToBounds` · cierra **E2** · esfuerzo **trivial**

Dos cambios, y hacen falta los dos (uno arregla la causa, el otro es el cinturón de seguridad):

```csharp
// MainWindow.xaml.cs, dentro de UpdateMinimapViewport (:713-724)
double vpX = Math.Clamp(WorldMapScroll.HorizontalOffset / zoom, 0, img.PixelWidth);
double vpY = Math.Clamp(WorldMapScroll.VerticalOffset   / zoom, 0, img.PixelHeight);
double vpW = Math.Min(WorldMapScroll.ViewportWidth  / zoom, img.PixelWidth  - vpX);
double vpH = Math.Min(WorldMapScroll.ViewportHeight / zoom, img.PixelHeight - vpY);
```

```xml
<!-- MainWindow.xaml:3939 -->
<Grid ClipToBounds="True" Visibility="{Binding Settings.IsMinimapVisible, ...}">
```

**Criterio mío**: además, cuando el viewport recortado cubre el mundo entero (zoom "ajustar a la
ventana" o menos), el rectángulo pasa a ser el borde exacto del minimapa y deja de aportar
información. Yo lo **ocultaría** en ese caso (`vpW >= img.PixelWidth && vpH >= img.PixelHeight` →
`Collapsed`), que es lo que hace cualquier minimapa real cuando ya lo ves todo.

### 9.3 · C-03 · Rama `Ores` en las dos consultas, y botón que consuma el tick · cierra **E5** · esfuerzo **pequeño**

Tres piezas:

1. **`BuildSingleRowQuery`** (`:594-602`): añadir, antes del `_ =>`,
   `WorldSearchCategory.Ores => new WorldSearchQuery { TileTypes = new HashSet<int> { row.Id } }`.
   Los minerales **son tiles** (`OreTileCatalog`, y `RebuildOreInventory` los saca de
   `_presence.TileCounts`, `:492`), así que la rama correcta es la misma que Objetos › Tiles.
2. **`SearchCheckedInventory`** (`:576-591`): que la fuente de filas sea
   `Inventory.Concat(OreMetals).Concat(OreGems).Concat(OreTargets)` y añadir la rama `Ores` al
   `switch`. Sin esto, el punto 3 no funcionaría.
3. **XAML** (`:4388-4395`): que el `UniformGrid` de Minerales pase de 2 a 3 columnas con un botón
   *"Buscar seleccionados"*, o —mejor, ver §9.4— que el tick actúe solo.

**Criterio mío sobre el orden de prioridad**: aunque se adopte la propuesta E7, el punto 1 hay que
hacerlo igual — pulsar el nombre de una fila **tiene que** hacer lo mismo que en Cofres y Objetos,
por coherencia; hoy es el único sitio de la barra lateral donde un clic no hace nada.

### 9.4 · C-04 · Evaluación de la propuesta del usuario (E7) y diseño de "marcar todo" · cierra **E6** y **E7** · esfuerzo **medio**

**La propuesta del usuario es buena y ya tiene precedente en el propio proyecto**, pero tal cual
está formulada tiene dos costes que él no está viendo. Mi evaluación:

**Lo que hay que aceptar de ella, sin reservas**: *"que se marque todo lo que hay, no unos sí y
otros no"*. Es la queja correcta y el estado actual (1000 primeras casillas por orden de columna)
es indefendible. **La arquitectura para arreglarlo ya está escrita y probada**: es exactamente lo
que hace `MarkOresOnMap` (`:610-641`) — `WorldHighlightRenderer.Render` sin tope para el mapa, y
lista topada y **agrupada** para el panel. Mi propuesta es **generalizarla** a Objetos y Cofres,
no inventar nada nuevo:

- **`WorldHighlight` para todo**: cualquier búsqueda por fila (tile, pared, líquido) pinta la capa
  de resaltado completa, sin tope. Hace falta una sobrecarga de `Render` que acepte también
  `WallIds` y `LiquidTypes`, no sólo `tileTypes` (`WorldHighlightRenderer.cs:38`).
- **Lista agrupada, no casilla a casilla**: reutilizar `OreVeinFinder` (que ya agrupa por
  componentes conexas) para tiles y líquidos. Una fila por "bolsa de miel", con su recuento —
  igual que hoy se muestra `"Mineral de oro (23 tiles)"`. Esto cierra la segunda mitad de E6
  (*"una cantidad absurda de mieles"*) mucho mejor que subir el tope.

**Los dos costes que hay que decirle al usuario:**

1. **Memoria.** Cada capa de resaltado es un `WriteableBitmap` del tamaño del mundo:
   **80,6 MB en un mundo Grande**, cuantificado por el propio código
   (`WorldHighlightRenderer.cs:21-24`). Que el tick marque **al instante** significa regenerar
   esos 80 MB con cada casilla que se marque o desmarque. **Mi recomendación: NO hacerlo
   inmediato.** Un `debounce` de ~250 ms (el mismo patrón ya establecido en el proyecto:
   `LibraryViewModel._searchDebounceTimer`, `AppearanceViewModel._hairOptionsDebounceTimer`)
   agrupa una ráfaga de ticks en un único repintado.
2. **Legibilidad.** Marcar "Piedra" o "Tierra" a la vez que "Oro" tiñe el 60 % del mapa y no
   informa de nada. **Mi recomendación**: mantener el color por selección (hoy siempre
   `Colors.Orange`, `:632`) y avisar cuando una selección supere un umbral razonable de casillas
   —el censo ya lo sabe sin barrer nada, `_presence.TileCounts`— en vez de pintar a ciegas.

**Diseño concreto que propongo**, uniendo las dos cosas:

- El tick pasa a significar *"muéstralo en el mapa"* y actúa solo (con debounce). Los botones
  "Marcar en el mapa"/"Quitar marcas" se quedan como "aplicar ya" y "desmarcar todo".
- Pulsar el **nombre** sigue significando *"búscalo y llévame a los resultados"* (C-03).
- La lista de resultados agrupa por componente conexa para tiles y líquidos; sigue siendo casilla
  a casilla sólo para NPCs y objetos dentro de cofres, donde cada coincidencia **es** una entidad
  distinta.

### 9.5 · C-05 · Cruzar los Spawn Points con el mundo cargado · cierra **E4** · esfuerzo **pequeño**

Con lo verificado en §2/E4, la regla es la del propio juego, sin inventar nada:

```csharp
// MainViewModel.BuildCharacterSpawns (:335-341)
private IEnumerable<(string Label, int X, int Y)> BuildCharacterSpawns()
{
    if (_loaded == null) yield break;
    int? worldId   = Exploration.LoadedWorldId;     // nuevo, de WldHeader.WorldId
    string? worldName = Exploration.WorldTitle;
    foreach (var entry in Servers.Entries)
    {
        if (entry.SpawnX == 0 && entry.SpawnY == 0) continue;
        // Regla real del juego: Player.FindSpawn(), Player.cs:55781
        if (worldId is int id && (entry.Address != id || entry.Name != worldName)) continue;
        yield return (entry.Name, entry.SpawnX, entry.SpawnY);
    }
}
```

**Tres decisiones de diseño, criterio mío, que conviene razonar:**

1. **Sin mundo cargado, no se filtra** (el `worldId is int id` de arriba). Hoy los marcadores no
   se ven de todos modos porque no hay mapa; no hay motivo para complicarlo.
2. **Exigir las dos condiciones**, id **y** nombre, porque es lo que hace el juego. Un `.wld`
   copiado y renombrado a mano conserva el `WorldId`, y el juego lo trataría como mundo distinto.
   Copiarle su criterio evita que Terrakeep y Terraria discrepen.
3. **La etiqueta del marcador tiene que cambiar.** Hoy es `entry.Name` (`:340`), que es **el
   nombre del mundo** — de ahí que el usuario dijera *"no lo especifica en ningún lado"*: cada
   estrella lleva de tooltip el nombre del mundo en el que estás. Debería decir algo como
   *"Punto de aparición de {personaje} en este mundo (x, y)"*.

**Corolario que yo también arreglaría**, aunque no lo pidió: renombrar `PlrServerEntry.Address` a
`WorldId` (con la lectura/escritura intactas, es sólo el nombre en C#) y cambiar la etiqueta de
esa columna en la pestaña Spawn Points. Hoy el usuario puede editar el id de un mundo creyendo que
edita una IP.

### 9.6 · C-06 · Vista por cofre individual · cierra **E8** · esfuerzo **medio**

**Diseño que propongo**, teniendo en cuenta que TEdit no ofrece nada mejor aquí (§2/E8):

Un **tercer modo** en el selector de Cofres (`ChestViewMode`), junto a "Por tipo de cofre" y "Por
lo que contienen": **"Cofre a cofre"**. Una fila por cofre real de `_world.Chests`, con:

- el sprite y el nombre de la **variante** del cofre (ya resuelto: `_tileNames.TileVariantName`
  y `TileIconResolver`, exactamente como hace hoy `ChestViewMode == 0`);
- el nombre propio del cofre si lo tiene (`WldChest.Name`, que hoy **no se muestra en ningún
  sitio**);
- las coordenadas y el número de objetos que contiene;
- **desplegable**: al pulsar la fila, el contenido real de **ese** cofre — id, nombre, cantidad y
  prefijo, con el sprite (`VanillaIconResolver`, ya en uso en `RebuildChestInventory:465`).

Pulsar la fila del cofre **navega a su posición** (mismo `NavigateToTile` de siempre); pulsar un
objeto de dentro no navega (ya estás en el cofre).

**Ordenación**: por distancia al spawn del mundo, reutilizando el cálculo que ya existe para
`ShowSpawnDistance` (`ExplorationViewModel`, `ApplyWorldSearchOrder`). Es el orden útil de verdad
—"qué cofre tengo más cerca"— y no cuesta nada nuevo.

**Nota de rendimiento**: un mundo Grande tiene del orden de cientos de cofres, así que la lista
**debe** ir virtualizada, con el mismo `VirtualizingPanel.IsVirtualizing`/`Recycling`/`ScrollUnit="Pixel"`
que ya aplicó `F-5` a las otras listas (`MainWindow.xaml:4368-4372`).

**Lo que NO propongo**: sustituir "Por lo que contienen". Sigue siendo la vista correcta para la
pregunta *"¿hay una Alita de murciélago en algún cofre de este mundo?"*. Son dos preguntas
distintas y merecen dos vistas.

### 9.7 · C-07 · Aire para la tira "Tus mundos" · cierra **E1** · esfuerzo **trivial**

`MainWindow.xaml:3537`: `Margin="0,10,0,0"` → **`Margin="0,4,0,10"`**.

Resultado: hueco superior 14 px (10 del `WrapPanel` + 4), hueco inferior 10 px. La tira **sube 6 px**
y deja de estar pegada al lienzo del mapa. Los dos valores están en la escala 4/6/10/14/20 ya
fijada por `P-5`.

**Criterio mío**: no tocar el `Padding="8,5"` de las píldoras. El problema es el reparto exterior,
no el tamaño de la píldora, y tocarlo obligaría a repasar `H5-11-PILDORA` en el arnés.

### 9.8 · C-08 · Iconos para las dos raíces del árbol de buffs · cierra **L1** · esfuerzo **trivial**

`Services/BuffLibraryTreeBuilder.cs`, dos líneas, con el mismo criterio que ya usa el árbol de
objetos (*"el primer objeto real de la categoría"*, `CategoryNodeViewModel.cs:9-13`):

```csharp
// :62-83, BuildIndex — antes del ApplyOrderedUnion
var root = new CategoryNodeViewModel("Indice", "Indice")
    { IconPath = allIds.Count > 0 ? VanillaBuffIconResolver.GetIconPath(allIds[0]) : null };

// :133, BuildCalamityRoot
var root = new CategoryNodeViewModel("Calamity (mod)", "Calamity")
    { IconPath = cats.Count > 0 ? IconOf(byCategory[cats[0]][0]) : null };
```

`ApplyOrderedUnion` (`:140-150`) no pisa `IconPath`, así que el orden no importa.

**Criterio mío**: aprovecharía para corregir también los dos rótulos, que son los únicos del árbol
sin tilde ni recuento: `"Indice"` → **`"Índice ({n})"`** y `"Calamity (mod)"` → mantenerlo, que
casa con el árbol de objetos. Las 6 carpetas curadas y las 10 de Calamity **sí** llevan su
recuento entre paréntesis (`:49`, `:106`).

### 9.9 · C-09 · Búsqueda insensible a diacríticos · cierra **L2** · esfuerzo **pequeño**

**No hay que reescribir la gramática.** Ya normaliza mayúsculas fuera de `Matches`, poniendo los
dos lados en minúsculas antes de comparar; el mismo sitio sirve para los diacríticos.

**Diseño que propongo**, en dos capas:

1. **Un único helper compartido**, junto a la gramática:

   ```csharp
   public static string Fold(string s)
   {
       var d = s.Normalize(NormalizationForm.FormD);
       var sb = new StringBuilder(d.Length);
       foreach (char c in d)
           if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
       return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
   }
   ```

   `FormD` + descarte de `NonSpacingMark` es el enfoque estándar y es el que encaja aquí: la
   gramática ya trabaja con `string.Contains` ordinal sobre cadenas preprocesadas, así que basta
   con cambiar **qué** se preprocesa. La alternativa (`CompareInfo.IndexOf` con
   `CompareOptions.IgnoreNonSpace`) obligaría a reescribir el `words.All(target.Contains)` de
   `:36` y sería más lento en el bucle de 8821 entradas.

2. **Aplicarlo en `Matches` al término**, y **cachear el nombre plegado** en cada entrada de
   catálogo en vez de plegarlo en cada tecla. Los ViewModels ya pasan
   `DisplayName.ToLowerInvariant()` en cada llamada: sustituirlo por una propiedad calculada una
   vez en el constructor (`LibraryItemViewModel`, `BuffCatalogEntryViewModel`) **es más rápido que
   hoy**, no más lento — hoy se llama a `ToLowerInvariant()` 8821 veces por pulsación.

**No olvidar el undécimo sitio**: `ExplorationViewModel.ApplyInventoryFilter` (`:551-558`), que no
pasa por la gramática.

**Criterio mío**: el plegado debe ser **sólo del lado de la comparación**, nunca de lo que se
muestra. Los nombres visibles siguen con sus tildes.

### 9.10 · C-10 · Bonos de set: **arreglar el modelo, no sólo los datos** · cierra **L3** · esfuerzo **medio-alto**

Esta es la corrección más grande del plan, y **mi recomendación es no parchear los datos sin
arreglar antes el modelo**, porque las dos mitades fallan por lo mismo.

**Diagnóstico compartido**: en el juego real la bonificación pertenece **al conjunto
(cuerpo + piernas + cuál de los cascos)**, no a la pieza. Vanilla lo hace explícito en
`Player.UpdateArmorSets` (`head == 29 && body == 17 && legs == 16` → un bono; `head == 30` con el
mismo cuerpo y piernas → **otro**), y Calamity igual (cinco cascos por set con cinco bonos). Los
dos modelos de datos de Terrakeep guardan **un texto por pieza**, que no puede representar eso.

**Fase 1 — vanilla (`C-10a`), esfuerzo pequeño.** Tres arreglos concretos en
`scripts/extraer-sets-armadura.py`, cada uno con su modo de fallo ya identificado (§3/L3-a):

| Modo | Arreglo |
|---|---|
| 1 — `if` anidado saltado | No hacer `i = brace_close + 1` cuando el bloque contiene más `if` con `setBonus`: recorrer **también** el interior, componiendo la condición del padre con la del hijo (`cond_padre and cond_hijo`) antes de evaluar el producto cartesiano. Cierra las 10 claves de Cobalto/Mithril/Adamantita/Titanio |
| 2 — inverso incompleto | Averiguar por qué el paso 1 sólo recupera 235 índices de casco y no los altos (157-171). Probablemente `split_by_case` no cubre todos los métodos `SetDefaults#` de `Item.cs`. Cierra 8 claves |
| 3 — sets de 2 piezas | Permitir `legs = null` en la salida (`pieces: [head, body]`), y que `BonusForEquipped` lo acepte. Cierra `Wizard` y `MagicHat` |

**Criterio de aceptación duro y comprobable**: `43 → 63` claves, es decir, **cero diferencia**
entre las claves de `vanilla_armor_sets.json` y las `ArmorSetBonus.*` reales de `Player.cs`. Es un
diff de conjuntos, no una impresión.

**Fase 2 — Calamity (`C-10b`), esfuerzo medio.** Aquí no basta con reejecutar un script: el
catálogo **no tiene el dato**. Dos caminos, y recomiendo el segundo:

- **(a) Rellenar `setBonus` en cuerpo y piernas** copiando el del casco. Barato, pero **incorrecto
  de raíz**: con cinco cascos por set, ¿cuál se copia? Y dejaría el tooltip de una coraza
  Aeroespacial diciendo el bono de invocación cuando el usuario lleva el casco de cuerpo a cuerpo.
- **(b) Un catálogo de SET, no de pieza** — `calamity/armor_sets.json`, con una entrada por
  conjunto real: `{ body, legs, heads: { headId: bonusText } }`. Se puede derivar del catálogo que
  ya existe: los sets están agrupados por `category` (`Armor/Aerospec`, `Armor/Silva`…), el
  `equipSlot` distingue las piezas, y los 69 textos de bono ya están extraídos.

**Y con (b), `ActiveCalamitySetBonusText` (`EquipmentGroupViewModel.cs:216-231`) se reescribe
entera** para preguntar por el conjunto puesto en vez de comparar tres cadenas que nunca coinciden.

**Fase 3 — la Librería (`C-10c`), esfuerzo trivial una vez hechas 1 y 2.** Con el catálogo por
conjunto, `ItemStatsFormatter` puede mostrar en **cualquier** pieza del set el bono completo,
enumerando las variantes de casco:

> *Con el set completo (con cuerpo y piernas Aeroespaciales):*
> *· Casco mágico — reduce el coste de maná un 8 %…*
> *· Casco de cuerpo a cuerpo — …*

**Criterio mío sobre por qué merece la pena el camino largo**: la mitad Calamity es hoy una función
**anunciada, probada y muerta** (§3/L3-b). Rellenar `setBonus` a lo bruto la haría funcionar mal en
vez de no funcionar, que es peor.

### 9.11 · C-11 · Anunciar el arrastre y homogeneizar las dos librerías · cierra **L4** · esfuerzo **pequeño**

Cuatro cambios, ninguno grande:

1. **Coherencia de efectos** (`MainWindow.xaml.cs:765`, `:971`): pasar
   `DragDropEffects.Copy | DragDropEffects.Move` a `DoDragDrop`, para que el `Move` que devuelven
   los destinos (`:867`, `:986`) esté dentro del conjunto permitido.
2. **Anunciar el gesto en el tooltip** (`MainWindow.xaml:1002-1007`): una tercera línea,
   *"O arrastra la tarjeta hasta cualquier hueco."*, junto a las dos que ya están. Es la corrección
   de una línea que cierra la queja tal como el usuario la vivió.
3. **Cursor que lo insinúe**: `Cursors.SizeAll` en lugar de `Hand`, o —mejor, criterio mío—
   mantener `Hand` (el clic sigue siendo el gesto principal) y añadir un **adorno de arrastre**
   real durante el `DoDragDrop`, que hoy no existe: WPF arrastra sin ninguna vista previa, así que
   el usuario no ve que esté llevando nada.
4. **Clic en la Librería de buffs** (`MainWindow.xaml:1058`): añadir
   `MouseLeftButtonUp="OnBuffLibraryCardClick"` y el manejador gemelo de
   `OnLibraryCardClick` (`MainWindow.xaml.cs:777-805`), colocando en el slot de buff seleccionado
   (`BuffEdit.Slot`) con clic simple y en el primer hueco libre con doble clic. Hoy **un clic ahí
   no hace nada**, y eso probablemente es lo que el usuario llama *"pasa lo mismo con la pestaña
   buff"*.

### 9.12 · C-12 · Hueco vertical en los `WrapPanel` que envuelven · cierra **H1** y parte de **H3** · esfuerzo **trivial**

El mismo defecto en dos sitios, con el mismo arreglo: dar a las insignias un margen **vertical**,
no sólo horizontal.

```xml
<!-- MainWindow.xaml:1189 (dificultad) -->  Margin="0,0,5,4"
<!-- :1200, :1211, :1221 (Vanilla/tModLoader/Calamity) --> Margin="0,0,5,4"
```

Poniendo el hueco a la **derecha y abajo** de cada insignia (en vez de a la izquierda) se arregla
de paso la asimetría de la primera: ya no hay una insignia sin margen y tres con él, y la segunda
línea arranca alineada con la primera.

**Criterio mío**: 4 px verticales es suficiente para que se lean como dos filas y no engorda la
tarjeta (el `Border` tiene `Width="240"` pero alto libre, y el doll de 72,8 px de la columna 0 deja
holgura para una segunda línea de insignias).

Mismo tratamiento para la franja vital de la cabecera (`:1541-1556`), donde el envolvido a dos
líneas es real en ventana compacta.

### 9.13 · C-13 · Animación de hover en las tarjetas de personaje · cierra **H2** · esfuerzo **pequeño**

**El usuario dice explícitamente que no hace falta que sea idéntica**, y hay una razón técnica
para que no lo sea (§4/H2): el borde ya significa "personaje cargado".

**Lo que propongo replicar de `NavCardButton` (`Theme.xaml:1101-1131`):**

| Capa de `NavCardButton` | ¿Replicar? |
|---|---|
| `TranslateTransform Y: 0 → -3` en 150 ms | **Sí** — es la firma visual, y no colisiona con nada |
| `Effect` → `CardShadowHover` | **Sí** — ya lo hace hoy, sólo que de golpe; basta con que acompañe |
| `Background` → `BgHoverBrush` | **Sí** — ya lo hace hoy |
| `ColorAnimation` del borde a acento | **NO** — colisionaría con el `DataTrigger` de `IsCurrent` (`:1160-1163`) y reproduciría el bug `L-6` ya corregido en la tarjeta de la Librería |

Es decir: mismas `EnterActions`/`ExitActions` con el `DoubleAnimation` del `TranslateTransform`,
sin el `ColorAnimation`. La tarjeta de personaje seguirá teniendo su propia identidad (borde =
estado, no hover) y ganará el mismo "levanta al pasar" que el resto de tarjetas de Inicio.

**Detalle de implementación**: `CharacterCardTemplate` es un `Border` con `InputBindings` y
`ContextMenu`, así que no puede adoptar `NavCardButton`; hay que declarar el `RenderTransform` en
el propio `Border` (`:1115`) y los `Storyboard` dentro de su `Style.Triggers` existente
(`:1152-1167`).

### 9.14 · C-14 · Ritmo y separación de la barra superior · cierra **H3** · esfuerzo **pequeño**

Cuatro cambios, en orden de impacto:

1. **Separar las tres columnas.** `Margin="0,0,20,0"` en la columna 0 (`:1458`) y
   `Margin="20,0,0,0"` en la columna 2 (`:1393`), o —equivalente y más limpio— márgenes laterales
   en los dos `WrapPanel` de la columna 1 (`:1538`, `:1584`). **20 px es el escalón "grupos
   distintos" de la escala del proyecto.** Esto es lo que cierra *"se junta con softcore"* a
   cualquier ancho, incluida pantalla completa.
2. **Separar vida de maná.** Subir el margen derecho de la barra de vida de 14 a 20
   (`:1542`, `Margin="4,0,20,0"`), manteniendo el 4 interno. Razón 5:1 en vez de 3,5:1 — los dos
   pares se leen como dos.
3. **Poner tope al nombre.** `MaxWidth="220"` + `TextTrimming` en el `TextBox` del nombre
   (`:1477-1481`), para que un nombre largo no siga comiéndose la columna central. Hoy sólo tiene
   `MinWidth="80"`.
4. **Hueco entre líneas** de los dos `WrapPanel` cuando envuelven (C-12).

**Criterio mío**: los cuatro juntos, en una sola pasada, **y volviendo a pasar la matriz de
redimensionado completa** (`AR-04`/`H-04` a los 14 tamaños). Es la zona de la app con más historia
de regresiones de ancho (`H-04a`, `H-04b`, `R-04a`, `R-04b`, `P-4`, `F-15`) y el punto 1 le quita
40 px a una columna que ya iba justa a 1080.

### 9.15 · C-15 · Deshacer en Apariencia · cierra **A1** · esfuerzo **medio**

El patrón está establecido y es agnóstico del dominio (§5/A1). Propongo un único helper privado en
`AppearanceViewModel`, invocado desde cada `partial void On…Changed`:

```csharp
private void PushUndo<T>(string label, T before, T after, Action<T> apply)
{
    if (_suppressWriteback || _suppressUndoRecording) return;
    _undo.Push(new UndoEntry {
        Label = label,
        Undo  = () => WithSuppression(() => apply(before)),
        Redo  = () => WithSuppression(() => apply(after)),
    });
}
```

**Tres gotchas reales que el diseño tiene que resolver, y que no son obvios:**

1. **La reentrada.** Cada `On…Changed` escribe al modelo Y refresca la vista previa. Deshacer
   vuelve a asignar la propiedad, lo que vuelve a disparar `On…Changed`, lo que **empujaría una
   entrada nueva**. Hace falta el mismo guardia que ya usa `MainViewModel._suppressUndoRecording`
   (`:179`) durante `Undo`/`Redo`, no sólo `_suppressWriteback` (que hoy sólo protege la carga).
2. **Los 7 colores no pasan por `AppearanceViewModel`.** Los edita `ColorSwatchViewModel`
   directamente sobre el `byte[]` real del personaje; `AppearanceViewModel` sólo se entera por
   `swatch.PropertyChanged` (`:239`). Para hacerlos deshacibles hace falta capturar el color
   **anterior**, que hoy no se guarda en ninguna parte — un `(byte R,G,B)` previo por swatch.
3. **Agrupar los arrastres de deslizador.** Arrastrar el deslizador de vida de 400 a 500 dispara
   decenas de `OnHealthMaxChanged`. Sin agrupar, el historial se llena de 100 entradas de 1 punto
   cada una. **Criterio mío**: `debounce` de ~400 ms que empuja **una** entrada con el valor
   inicial del arrastre y el final — es el mismo patrón de `RunAsUndoableBatch` (una entrada por
   gesto, no por cambio), adaptado al eje del tiempo.

**Rótulos que propongo** (el tooltip de las flechas ya los muestra,
`MainWindow.xaml:1444`, `:1447`): `"Apariencia · peinado"`, `"Apariencia · tinte de pelo"`,
`"Apariencia · color de Pelo"`, `"Apariencia · vida máxima"`, `"Apariencia · núcleo"`…

**Alcance que recomiendo**: hacer Apariencia ahora, **y dejar Banderas/Investigación/Spawn
Points/nombre para una pasada posterior**, listadas explícitamente para que no se olviden. Meterlas
todas de golpe multiplica la superficie de regresión de una función (Deshacer) que hoy funciona
bien en lo que cubre.

### 9.16 · C-16 · Regenerar el mapa nombre→id para 1.4.5 (sólo Novedades) · cierra media **N1** · esfuerzo **pequeño**

**Lo verificado en §6/N1 convierte esto en un cambio de datos, no de alcance**: los 40 sprites ya
están en disco, los 40 ids ya están en `TerrariaVanilla/Terraria/ID/ItemID.cs`, y el único eslabón
que falta es el mapa de nombre interno a id, que llega hasta 5455.

**Y aquí está la decisión de diseño importante, criterio mío**: **no ampliar
`vanilla_item_ids_by_key.json`**. Ese fichero alimenta la **Librería**, la **Investigación** y el
**buscador del personaje**, y Terrakeep edita guardados de **tModLoader 1.4.4.9**, donde los ids
5456-6195 **no existen**. Colocar el objeto 6173 en un slot produciría un objeto inválido en el
`.plr` del usuario.

**Propongo en su lugar un catálogo separado y de sólo lectura**,
`Assets/whats_new_item_ids.json`, extraído de `TerrariaVanilla/Terraria/ID/ItemID.cs`, consumido
**únicamente** por `WhatsNewItemViewModel.ForVanilla` (`:22-26`). Así la pestaña Novedades enseña
lo nuevo del juego sin que ni un solo id de 1.4.5 llegue jamás a un guardado.

**Este hallazgo merece un aviso aparte al usuario**: hay **740 objetos de Terraria 1.4.5 con
sprite en disco y sin nombre en el catálogo**. Que la Librería vaya una versión por detrás puede
ser correcto (es la versión que Calamity usa) o puede ser algo que él quiera cambiar; es una
**decisión suya**, no un bug que yo deba resolver por mi cuenta.

### 9.17 · C-17 · Tooltip de objeto en Novedades · cierra la otra media de **N1** · esfuerzo **trivial**

Añadir al `WhatsNewItemViewModel` una propiedad `StatsTooltip` rellenada con
`ItemStatsFormatter.Format(isCalamity, id, catalogs)` —**la misma llamada exacta** que hace
`LibraryViewModel.cs:65` y `:75`— y un `ToolTip` en la píldora (`MainWindow.xaml:709`) con la misma
plantilla que la tarjeta de la Librería (`:996-1007`, sin las dos líneas de "clic/doble clic", que
aquí no aplican).

Con esto la pestaña Novedades muestra al pasar el ratón exactamente lo que el usuario pide
(*"como en calamity mod y tmodloader"*): sprite, nombre con color de rareza, daño/DPS, defensa,
crítico, velocidad de uso y la descripción real del juego — **sin escribir ni un texto nuevo**.

**Dependencia real**: para los 40 objetos de 1.4.5 hará falta también extender
`vanilla_item_tooltips.json` y `vanilla_stats.json` (que hoy paran en 5455/5452), o aceptar que
esos 40 muestren sólo nombre y sprite. **Criterio mío**: empezar por nombre + sprite (C-16), que ya
es el 90 % del valor, y dejar las estadísticas de 1.4.5 como una segunda vuelta opcional.

### 9.18 · C-18 · Acerca de: autoría y registro al día · cierra **N2** · esfuerzo **pequeño**

**Dos piezas separadas:**

**(a) La autoría.** Un bloque propio en `AboutViewModel`, no una frase enterrada en
`CreditsText`. Propongo una sección visible bajo el título, con la forma que ya usa el resto de la
pestaña (`TitleText` + `BodyText`, `MainWindow.xaml:4530-4535`):

```csharp
public string AuthorName => "IncrediBad";
public string AuthorText =>
    "Terrakeep está diseñado y desarrollado por IncrediBad.";
```

**El nombre va literal: `IncrediBad`, con I y B mayúsculas y el resto en minúsculas.** No se
"corrige" a `Incredibad` ni a `INCREDIBAD` en ningún sitio, ni en el XAML, ni en el `.csproj`
(`<Authors>`), ni en ningún comentario.

**Criterio mío sobre dónde ponerlo**: justo **debajo del nombre de la app y la versión**
(`MainWindow.xaml:4464-4470`), antes de `CreditsText`. Es el sitio donde cualquiera lo busca, y
deja intacto el bloque de atribuciones a terceros, que dice cosas distintas y no debe mezclarse.

**(b) El registro de cambios.** Añadir a `Assets/changelog.json` las entradas que faltan desde
1.2.0 (170 commits, §6/N2), y subir `<Version>` en el `.csproj` (`:38-40`) a algo coherente —
`1.9.0` o `2.0.0`, criterio del usuario— para que la pestaña no siga diciendo 1.2.0.

**Criterio mío sobre la granularidad**: **no** una entrada por commit. Cuatro o cinco entradas de
versión que agrupen por bloque de trabajo real, siguiendo el formato que el propio fichero ya usa
(`version`, `date`, `summary`, `added`) — la tabla de §6/N2 es un borrador directo de ese
contenido. Un registro de 170 líneas no lo lee nadie.

### 9.19 · C-19 · Detalles menores, todos de una línea · esfuerzo **trivial**

Recogidos por el camino, ninguno reportado por el usuario:

| Qué | Dónde |
|---|---|
| El tooltip del tick de mineral cita *"Buscar seleccionados"*, un botón que **no existe** en esa sección | `MainWindow.xaml:1292` |
| `PlrServerEntry.Address` se llama y se muestra como "Address" siendo **el id del mundo** | `Core/PlrFormat/PlrCharacter.cs:19`, `ServerEntryRowViewModel.cs:20` |
| La etiqueta del marcador de spawn del personaje es `entry.Name`, que es **el nombre del mundo** | `MainViewModel.cs:340` |
| `WldChest.Name` (nombre propio del cofre) se lee del `.wld` y **no se muestra en ningún sitio** | `Core/WldFormat/WldChest.cs:18` |
| `"Indice"` sin tilde y sin recuento, único nodo raíz del árbol de buffs así | `BuffLibraryTreeBuilder.cs:65` |
| `375 Honey Drip` y `231 Larva` sin traducir en el catálogo de tiles | `Assets/tile_names.json` |

---

## 10. Tabla de correcciones priorizada

**Esfuerzo** y **prioridad** son criterio mío. **Criterio de aceptación** está escrito para poder
comprobarse con el arnés real (`TerrasavrNative.App.Tests/Program.cs`) o a mano, sin ambigüedad.

### Bloque A — Funciones anunciadas que hoy NO funcionan (antes de nada)

| Corr. | Punto | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **C-01** | E4b | `MainWindow.xaml:3768-3793` | trivial | Con un mundo cargado, el marcador `⌂` está en `(Header.SpawnX, SpawnY)` ±1 tile **en coordenadas de mundo**, comprobado con `TransformToAncestor` a `Zoom` 0,05 / 1,0 / 6,0. Ídem el `◇` en `(DungeonX, DungeonY)`. Los tres `ItemsControl` vecinos **no cambian de posición** |
| **C-03** | E5 | `ExplorationViewModel.cs:594-602`, `:576-591`; `MainWindow.xaml:4388-4395` | pequeño | Con un mundo cargado y la categoría Minerales, pulsar el nombre de un mineral presente produce `WorldSearchResults.Count > 0` y un `WorldSearchSummary` **distinto** de `"Sin resultados."`. Marcar 2 minerales y pulsar el botón produce resultados de los dos |
| **C-10b** | L3-b | `calamity/catalog.json` o `armor_sets.json` nuevo + `EquipmentGroupViewModel.cs:216-231` | medio | Con las 3 piezas de un set real de Calamity puestas en el loadout seleccionado, `EquipmentGroup.ActiveSetBonusText != null` y su texto es el del casco puesto. Con dos piezas de sets distintos, sigue siendo `null` |
| **C-02** | E2 | `MainWindow.xaml.cs:713-724` + `MainWindow.xaml:3939` | trivial | A `Zoom = MinZoom` en un mundo Grande, `MinimapViewportRect.Width ≤ 220` y `Height ≤ 63`, y `Canvas.Left/Top ≥ 0`. El rectángulo sigue moviéndose al desplazar el mapa a `Zoom = 2,0` |
| **C-14** | H3 | `MainWindow.xaml:1393`, `:1458`, `:1477-1481`, `:1538-1556`, `:1584` | pequeño | Distancia horizontal real (`TransformToAncestor(window)`) entre el borde derecho de la última insignia de identidad y el borde izquierdo del `♥` **≥ 16 px a los 14 tamaños** de la matriz. Ídem entre el último elemento vital y el primer botón. `AR-04`/`H-04` siguen en verde |

### Bloque B — Defectos reales que se notan (antes de entregar)

| Corr. | Punto | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **C-10a** | L3-a | `scripts/extraer-sets-armadura.py` → `vanilla_armor_sets.json` | pequeño | El conjunto de `key` distintas del JSON generado **es igual** al conjunto de `ArmorSetBonus.*` de `Player.cs` (63 = 63, diferencia vacía en los dos sentidos). Las 20 piezas de Cobalto/Mithril/Adamantita/Titanio tienen entrada. Los 177 ids que ya estaban **siguen con el mismo `key` y `text`** (no regresión) |
| **C-04** | E6, E7 | `WorldHighlightRenderer.cs` + `ExplorationViewModel.cs:576-641` | medio | Marcar "Miel" produce una capa de resaltado cuyo recuento de píxeles no transparentes **es igual** a `_presence.LiquidCounts[3]` (todas, no 1000). La lista muestra grupos, no casillas: `WorldSearchResults.Count` ≪ `TotalCount` con el resumen diciendo la verdad |
| **C-09** | L2 | `LibrarySearchGrammar.cs` + 11 puntos de llamada | pequeño | `Matches("cenit", 4956, "cénit", null) == true` **y** `Matches("cénit", ...) == true`. Buscar `"mascara"` en la Librería devuelve ≥ el mismo número de resultados que `"máscara"`. El filtro de inventario de Exploración se comporta igual |
| **C-05** | E4 | `MainViewModel.cs:335-341` + `ExplorationViewModel` | pequeño | Con un `.plr` con spawn points de un mundo A y el mundo B cargado, `Exploration.CharacterSpawns.Count == 0`. Con A cargado, aparecen sólo los suyos. Sin mundo, comportamiento de hoy |
| **C-11** | L4 | `MainWindow.xaml.cs:765`, `:971`, `MainWindow.xaml:1002-1007`, `:1058` | pequeño | Arrastrar una tarjeta a un slot válido lo coloca y el cursor muestra el efecto correcto; a uno inválido, muestra prohibido. Clic simple en una tarjeta de la **Librería de buffs** coloca en el slot de buff seleccionado. El tooltip nombra los tres gestos |
| **C-15** | A1 | `AppearanceViewModel.cs` + `MainViewModel.UndoStack` | medio | Cambiar el peinado y pulsar Deshacer devuelve `HairStyle` al valor anterior **y** refresca la vista previa. Un arrastre completo del deslizador de vida deja **una** entrada, no N. Deshacer un cambio de Apariencia **no** empuja una entrada nueva |

### Bloque C — Pulido y capacidad ausente (recomendado, sin bloquear la entrega)

| Corr. | Punto | Fichero(s) real(es) | Esfuerzo | Criterio de aceptación |
|---|---|---|---|---|
| **C-08** | L1 | `BuffLibraryTreeBuilder.cs:65`, `:133` | trivial | Los 8 nodos raíz del árbol de buffs tienen `IconPath != null`. Ninguna tarjeta raíz de la Librería de buffs se queda sin `Image` |
| **C-12** | H1 | `MainWindow.xaml:1189-1226`, `:1541-1556` | trivial | En una tarjeta de personaje de Calamity con dificultad `Mediumcore` (envuelve a 2 líneas), la distancia vertical entre el borde inferior de la insignia de arriba y el superior de la de abajo es **≥ 3 px**. La tarjeta no crece de ancho |
| **C-07** | E1 | `MainWindow.xaml:3537` | trivial | Hueco real por encima de la tira = 14 px, por debajo = 10 px, medido con `TransformToAncestor` a los 14 tamaños. `H5-11-PILDORA` y `H5-11-TIRA-PERMANENTE` siguen en verde |
| **C-13** | H2 | `MainWindow.xaml:1115`, `:1152-1167` | pequeño | Al entrar el ratón, la tarjeta de personaje se desplaza `Y = -3` en 150 ms y vuelve a 0 al salir. El borde **no** cambia de color con el hover; con `IsCurrent` sigue en acento y grosor 2 |
| **C-17** | N1 | `WhatsNewItemViewModel.cs` + `MainWindow.xaml:709` | trivial | Cada píldora de "Objetos nuevos" con id resuelto tiene `ToolTip` con el mismo texto que la tarjeta de la Librería para ese id |
| **C-16** | N1 | `Assets/whats_new_item_ids.json` (nuevo) + `WhatsNewItemViewModel.cs:22-26` | pequeño | Los 40 objetos de 1.4.5.7 resuelven `IconPath != null`. `vanilla_item_ids_by_key.json` **no cambia** (la Librería sigue con 5455 ids) |
| **C-18** | N2 | `AboutViewModel.cs` + `MainWindow.xaml:4464-4470` + `changelog.json` + `.csproj:38-40` | pequeño | La pestaña Acerca de contiene el texto exacto `IncrediBad` (mayúsculas incluidas). `changelog.json` tiene ≥ 4 entradas y la más reciente coincide con `<Version>` del `.csproj` |
| **C-06** | E8 | `ExplorationViewModel.cs:439-470` + `MainWindow.xaml` | medio | El tercer modo lista tantas filas como `_world.Chests.Count`; desplegar una muestra exactamente los objetos de **ese** cofre; pulsarla navega a sus coordenadas ±1 tile. La lista está virtualizada (< 100 contenedores realizados con cientos de cofres) |
| **C-10c** | L3 | `ItemStatsFormatter.cs:75`, `:100-102` | trivial (tras C-10a/b) | Una coraza de Calamity muestra en su tooltip de Librería el bono de su set. Una coraza de Cobalto también |
| **C-19** | varios | ver §9.19 | trivial | — |

### Bloque D — Decisiones del usuario, no de código

| Punto | Decisión pendiente |
|---|---|
| **N1** (contenido) | Cuántas versiones de Terraria/Calamity cubrir en Novedades. Hoy hay 2 + 5. `H3-07` de la tercera auditoría ya lo dejó marcado como decisión suya y sigue abierto |
| **C-16** (alcance) | Si la **Librería** debe seguir en el catálogo de 1.4.4.9 (la versión que usa Calamity, y mi recomendación) o ampliarse a los 740 objetos de 1.4.5 que ya tienen sprite. **No es un bug: es una decisión de producto** |
| **C-18** (versionado) | Qué número de versión poner (`1.9.0`, `2.0.0`…) |

### Orden de ejecución sugerido

1. **C-01, C-02, C-07, C-08, C-12** — todos triviales, todos independientes, ninguno toca la misma
   zona que otro. Una sola sesión. Empezar por **C-01**, que es la función rota más visible.
2. **C-03** → **C-04** — la misma zona de `ExplorationViewModel` (las consultas y el marcado).
   **C-03 antes**, porque C-04 se apoya en que la rama `Ores` exista.
3. **C-10a** → **C-10b** → **C-10c** — en ese orden estricto: el catálogo vanilla, el de Calamity,
   y sólo después el formateador que los consume. Es el bloque más largo del plan.
4. **C-14 + C-12** (franja vital) — la barra superior entera de una vez, **y volver a pasar la
   matriz de redimensionado a los 14 tamaños** antes de darla por buena.
5. **C-09** — toca 11 puntos de llamada; hacerlo aislado para que una regresión sea fácil de
   localizar.
6. **C-05, C-11, C-13, C-17** — independientes, en cualquier orden.
7. **C-15** (Deshacer en Apariencia) — el que más riesgo de reentrada tiene; hacerlo cuando el
   resto esté estable, con sus pruebas propias.
8. **C-16, C-18** — datos y contenido; requieren antes las decisiones del Bloque D.
9. **C-06** (cofre a cofre) — el de más superficie de UI nueva; el último, o después de entregar.

---

## 11. Cómo verificarlo con el arnés que ya existe

`TerrasavrNative.App.Tests/Program.cs` (3866 renglones) ya tiene comprobaciones etiquetadas que
sirven de plantilla **y de red de seguridad**:

- **`A8-03`** (`:2279-2302`) — mide el ancho **en coordenadas de ventana** de un marcador a tres
  niveles de zoom. **Es la plantilla exacta para C-01**, cambiando "ancho" por "posición".
- **`A8-06`** (`:1053-1066`) — alturas de los 6 botones de la barra superior. **Cualquier cambio
  de C-14 debe mantenerlo en verde.**
- **`A8-02` / `A8-02b`** (`:2230-2256`) — `IsSearching` durante y tras el barrido, y la
  cancelación. Toca la misma ruta que C-03/C-04.
- **`A8-04`** (`:2261-2277`) — contenedores realizados < 100 con 1000 resultados. **C-06 tiene que
  pasarlo con cientos de cofres.**
- **`AR-02`** — recorre las 5 categorías de Exploración a varios anchos y falla si la barra lateral
  recorta un píxel. Es lo que más riesgo corre con C-03/C-04/C-06.
- **`H5-11-PILDORA` / `H5-11-TIRA-PERMANENTE`** (`:2087-2100`) — la tira de mundos. **C-07 no debe
  romperlos.**
- **`AR-04` / `H-04`** — matriz de redimensionado a 14 tamaños. **Obligatorio volver a pasarla tras
  C-14.**

**Comprobaciones nuevas que yo añadiría**, una por corrección del Bloque A, con el mismo estilo de
etiqueta:

```
A9-01-SPAWNPOS     marcador ⌂ en (SpawnX,SpawnY) +-1 tile a Zoom 0,05 / 1,0 / 6,0
A9-02-MINIMAPA     a MinZoom, Width<=220 y Height<=63 y Left/Top>=0
A9-03-MINERALCLIC  clic en el nombre de un mineral -> WorldSearchResults.Count > 0
A9-04-SETCALAMITY  3 piezas de un set real de Calamity -> ActiveSetBonusText != null
A9-05-SETVANILLA   claves de vanilla_armor_sets.json == ArmorSetBonus.* de Player.cs (63)
A9-06-BARRAHUECO   separacion identidad<->vitales y vitales<->botones >= 16px a los 14 tamanos
A9-07-TILDES       Matches("cenit", 4956, "cénit", null) == true
A9-08-SPAWNMUNDO   spawn points de otro mundo -> CharacterSpawns.Count == 0
A9-09-UNDOPELO     cambiar peinado + Deshacer -> HairStyle vuelve, y no empuja entrada nueva
A9-10-MIELTODA     pixeles marcados de la capa == LiquidCounts[3]
```

Las cinco últimas (`A9-05`, `A9-07`, `A9-08`, `A9-09`, `A9-10`) son **deterministas y sin ventana**:
van en `TerrasavrNative.App.ViewModels.Tests` / `Core.Tests`, no en el arnés de UI Automation.
`A9-05` en particular es un simple diff de conjuntos sobre dos ficheros y **debería ejecutarse en
cada compilación**, porque es lo que evita que el catálogo de sets vuelva a quedarse corto en
silencio.

---

# PARTE III

## 12. Riesgos conocidos y lo que NO verifiqué

### Sobre el método

**No ejecuté la aplicación en ningún momento.** Todo lo de la Parte I es lectura de código, de
datos y del código decompilado del juego. Lo único que ejecuté fue un script de extracción del
propio proyecto (`extraer-sets-armadura.py`), copiado al scratchpad con la salida redirigida —
**el fichero del repositorio no se tocó**, y lo verifiqué comparando su salida (177 ids) con el
`vanilla_armor_sets.json` que ya está en el repo (177 ids): idéntico, luego la reproducción es
fiel.

**No tengo la captura de pantalla** que el usuario adjuntó para H3. Lo que hay en §4/H3 sale del
XAML, no de la imagen. Coincide con lo que él describe, pero es una inferencia.

### Hallazgos con una parte no verificada

- **E3 (Librería, última fila cortada) — es el hallazgo más débil del informe.** Corregí el
  puntero (no es un `WrapPanel`, es `SlotGridPanel`) y documenté tres propiedades reales y
  citables del panel que lo hacen frágil (el alto entra por una propiedad que sólo se actualiza
  tras la disposición, se usa `ActualHeight` donde correspondería `ViewportHeight`, y `Columns` es
  fijo). **Pero no pude reproducir el corte concreto ni el salto del scroll.** El experimento que
  haría falta, y que dejo escrito para quien lo ejecute: con la ventana a 1080×700, la Librería
  desplegada y una categoría de ≥ 200 objetos, registrar en cada paso de disposición
  `ScrollViewer.ActualHeight`, `ScrollViewer.ViewportHeight`, `ScrollViewer.ExtentHeight`,
  `SlotGridPanel._cell` y `DesiredSize.Height`. Si `ExtentHeight − ViewportHeight` sale un valor
  positivo pequeño (< 20 px), el diagnóstico queda cerrado y el arreglo es cambiar `ActualHeight`
  por `ViewportHeight`.
- **E6 (miel).** El mecanismo del tope de 1000 y el orden por columnas están probados leyendo el
  código. Lo que **no** puedo probar sin un mundo real delante es que la correlación que vio el
  usuario ("sólo las colmenas con reina") sea exactamente la franja izquierda del mundo; es la
  explicación que encaja con la geometría de la generación de Terraria, pero es una inferencia.
  **Comprobación que lo cerraría**: buscar "miel" y mirar el máximo `TileX` de los 1000 resultados
  — si es mucho menor que `TilesWide`, queda confirmado.
- **L4-(a) (efectos `Copy` vs `Move`).** La incoherencia con el contrato de OLE es un hecho del
  código. **Lo que no puedo afirmar sin ejecutar es la consecuencia exacta**: si Windows enmascara
  el efecto devuelto contra el permitido, el cursor sale mal pero el soltado funciona; si no lo
  enmascara, podría llegar a impedirlo. He escrito la corrección para que sea correcta en los dos
  casos.
- **H1 y H3 (medidas de píxeles).** Los anchos disponibles (150 px para las insignias) y los
  huecos (0 px entre columnas, 14 px entre vida y maná) salen de sumar valores declarados en el
  XAML. Son exactos para `Padding`/`Margin`/`Width`, pero **el ancho real del TEXTO de cada
  insignia y de cada etiqueta es una estimación** (~5,5-6 px por glifo a 10 px). Que "tModLoader +
  Calamity + dificultad" no quepan en 150 px es muy probable, no seguro. Lo mediría con el arnés
  (`ActualWidth` de cada `Border` de insignia) antes de tocar nada.
- **E8 (cofres).** No sé cuántos cofres tiene el mundo real de esta máquina, así que no puedo
  dimensionar la lista ni confirmar que la virtualización sea imprescindible. `ChestsPillCount`
  ya lo expone en la app.
- **L3-a modo 2.** Sé que `slot_by_kind["head"]` recupera 235 índices y que faltan los altos
  (157-171). **No investigué por qué** — lo más probable es que `split_by_case` no cubra todos los
  métodos `SetDefaults#` de `Item.cs`, pero no lo comprobé. Quien ejecute C-10a tendrá que
  averiguarlo antes de arreglarlo.

### Lo que no leí

- **De Terrakeep**: `BuildsViewModel`, `ResearchViewModel` (más allá de su llamada a la gramática),
  `FlagsViewModel`, `VersionEditorViewModel`, `SessionService`, `SettingsService`,
  `BackupHistoryService`, `AutoEquipService`, `PlayerPreviewRenderer`, `WorldRenderer`, y el 60 %
  de `MainWindow.xaml` (la pestaña Personaje › Objetos y Builds, salvo los tramos citados).
- **`bitacora.md` entero** (627 KB). Sólo leí el tramo de `H3-18` y busqué referencias puntuales.
  Es posible que algún hallazgo de este informe ya esté anotado ahí como conocido.
- **El decompilado de Calamity** (`Downloads\tModLoader-Decompiled\CalamityMod\`). Para C-10b haría
  falta abrirlo y ver cómo Calamity define sus sets (`ModPlayer.UpdateEquips` o equivalente), que
  es lo que diría si el modelo "cuerpo + piernas + casco" que propongo casa con el suyo. **No lo
  hice**: quedaba fuera del tiempo de esta pasada y no cambia el diagnóstico (0 de 131 piezas de
  cuerpo/piernas con bono es un hecho del catálogo local, no de Calamity).
- **La versión real de Calamity instalada** y si su catálogo local (`catalog.json`, 2709 objetos)
  está al día.

### Riesgos de las propuestas

- **C-14 le quita ~40 px a la columna central de la barra superior**, que a 1080 px ya iba justa
  (`H-04a`: le quedaban ~192 px para una tira de 196). **Es el cambio con más riesgo de regresión
  de todo el plan.** Puede hacer falta subir `NormalMinWidth` otra vez, o meter la franja vital en
  un `Viewbox` con suelo.
- **C-04 (marcar todo)** puede disparar el uso de memoria si se implementa sin `debounce`: 80,6 MB
  por repintado en un mundo Grande, dato del propio código.
- **C-09 (tildes)** cambia el comportamiento de **11 puntos de búsqueda a la vez**, incluido el
  buscador del mundo. Un fallo ahí es difícil de atribuir. Recomiendo hacerlo aislado, con su
  prueba determinista, y no mezclado con otra corrección.
- **C-10b** cambia el formato de un fichero de datos (`calamity/catalog.json` o uno nuevo). Si se
  hace mal, el riesgo no es cosmético: `EquipmentGroupViewModel` y `ItemStatsFormatter` lo leen los
  dos.
- **C-15 (Deshacer en Apariencia)** tiene tres gotchas de reentrada documentados en §9.15. Es la
  corrección con más probabilidad de introducir un bucle infinito si se hace deprisa.
- **C-16** — mi recomendación de NO ampliar el catálogo de la Librería a 1.4.5 se apoya en que
  Terrakeep edita guardados de tModLoader 1.4.4.9. **Si el usuario decide lo contrario**, hay que
  investigar antes qué hace tModLoader con un id de objeto que no conoce, cosa que no hice.

### Bloqueos de herramienta

Ninguno. Todo lo que necesité leer estaba accesible, y los conteos de datos se pudieron hacer con
scripts de un solo uso sobre los `.json` reales. El único límite real de esta pasada es el que se
declara arriba: **no ejecuto la aplicación**.
