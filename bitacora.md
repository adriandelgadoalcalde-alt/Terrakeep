# Bitácora — Terrasavr-Native / Terrakeep

Documento único de traspaso/contexto para este proyecto, mismo papel que
`bitacora.md` en `Terrasavr-Calamity-Beta\` y `NOTES-EXPERIMENTAL.md` en el
Terraria Trainer: qué se ha hecho, qué se ha verificado de verdad, qué queda
pendiente y por qué. Se actualiza en el mismo turno en que se hace un cambio
de fondo, no al final de la sesión.

**Qué es este proyecto**: port nativo (C#/.NET 10, WPF) de Terrasavr-Calamity-Beta
(hoy una app Electron que envuelve un motor Haxe/OpenFL compilado de
YellowAfterlife, más una capa propia de soporte de Calamity Mod). No sustituye
a la versión Electron todavía — es un proyecto nuevo y separado
(`Terrasavr-Win\Terrasavr-Native\`, repo git propio) hasta que tenga paridad
real. Pedido explícito del usuario: portar **todas** las funciones tal y como
están hoy, con identidad visual propia (no una copia de Terrasavr).

**Documento de plan original** (el que se aprobó en modo plan antes de
empezar): `C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md` — tiene
el detalle byte a byte del formato `.plr`/`.tplr` tal y como se confirmó
investigando `script.js`/`overrides.js`/`calamity-nbt.js` reales. No se
duplica aquí, se referencia.

**Identidad propia**: nombre de trabajo **"Terrakeep"** (pendiente de que el
usuario lo confirme o proponga otro - fácil de cambiar, son solo strings).
Logo generado desde cero (hexágono ámbar + "T" en negativo), no reciclado de
Terrasavr. Tema visual propio (`TerrasavrNative.App/Styles/Theme.xaml`).

## Arquitectura

```
TerrasavrNative.sln
├── TerrasavrNative.Core/          ← class library, sin UI, testable a fondo
│   ├── Nbt/                       ← NBT genérico + gzip (.tplr)
│   ├── PlrFormat/                 ← .plr vanilla completo (AES, cuerpo, loadouts, items)
│   ├── Calamity/                  ← catálogo, ids sintéticos, prefijos, masking/merge
│   ├── Data/                      ← loaders de JSON (catálogo, prefijos, builds, novedades...)
│   └── Model/                     ← GameItem/ItemPrefix (vista unificada vanilla+Calamity)
├── TerrasavrNative.App/           ← WPF, .NET 10-windows, MVVM (CommunityToolkit.Mvvm)
└── TerrasavrNative.Core.Tests/    ← xUnit, muchos tests contra datos/archivos REALES, no solo mocks
```

## Estado por fases (ver el plan para la lista completa de fases futuras)

### Fase 0 — Esqueleto (commit `818001a`)
Solución + 3 proyectos, `.gitignore`, sin lógica todavía.

### Fase 1 — Motor de formato (commits `6254ee9`, `9ef2a76`, `5906057`, `1822b38`) — CERRADA para el caso de uso principal

- NBT genérico (big-endian, Compound como lista ordenada no Dictionary) + `.tplr` = gzip(NBT).
- `.plr` vanilla completo: AES-128-CBC (clave=IV="h3y_gUyZ" en UTF-16LE) + PKCS7, cuerpo
  entero con TODOS los campos reales (nombre, stats, colores, los 4 loadouts, los 9
  contenedores con su disponibilidad real por versión, buffs, investigación, los 15 Poderes
  Creativos, servidores...). **Limitación deliberada: solo `version >= 145`** (cualquier
  personaje jugado en años recientes cae ahí de sobra) - versiones más antiguas lanzan
  `NotSupportedException` en vez de intentar un formato no implementado.
- Capa de Calamity: catálogo real (2709 objetos), 21 prefijos reales, `CalamityCharacterSync`
  (fusiona/enmascara objetos de Calamity en los 7 contenedores que la app JS sincroniza de
  verdad: inventory/bank/bank2/bank3/bank4/miscEquips/miscDyes).

**Pendiente dentro de la Fase 1** (ya cerrado salvo un punto - ver la sección "Armadura/tinte
de Calamity POR LOADOUT" más abajo para el detalle completo): armadura/tinte de Calamity POR
LOADOUT, ya fusionado/sincronizado. Queda solo coins/ammo/tempItems, que la app JS tampoco
sincroniza con Calamity (solo los protege al enmascarar).

**Verificación real**: 65 tests xUnit, incluido un round-trip **byte a byte perfecto** (con
cifrado AES incluido) contra dos personajes reales de este PC (`Eldelgas.plr` vanilla,
`adrian.plr` con Calamity, 100KB) y una prueba de extremo a extremo que encuentra y preserva
los objetos de Calamity reales del personaje `adrian`. Bug real encontrado y arreglado por el
propio test contra archivo real: slots "fantasma" (id=0 con count/prefix residual) se
colapsaban mal en el round-trip puro de `PlrItemSlot` (ya arreglado); luego se confirmó que
`GameItemConversions` SÍ debe seguir limpiándolos al pasar por el orquestador de Calamity (es
la misma limpieza que ya hace la app JS en cada guardado, no una pérdida de datos).

### Fase 2 — Núcleo de UI (commit `eeedd8f`) — primer resultado visible, solo lectura

App WPF real: `CharacterFileService` carga catálogos una vez, detecta el `.tplr` hermano
automáticamente, `MainViewModel`/`ContainerViewModel`/`ItemSlotViewModel` muestran
Inventario/Banco/Caja fuerte/Fragua/Bóveda del Vacío/Mascota-Montura-Gancho (con Calamity ya
mezclado) más Monedas/Munición/Equipo puesto (vanilla-only por ahora, ver limitación de
loadouts arriba). Botones Cargar/Guardar reales.

Nombres de items vanilla (`vanilla_item_names.json`, 5455 objetos): generado cruzando
`ItemID.cs` decompilado (id numérico → nombre interno) contra la traducción real del juego
(`lang.zip` → `Terraria.Localization.Content.es-ES.Items.json`) - verificado contra hechos
conocidos (id 1 = "Pico de hierro", id 2 = "Bloque de tierra").

### Identidad + tema + Builds/Novedades/Acerca de (commit `f7283c1`)

Logo propio, tema minimalista (paleta oscura, acento ámbar, nav lateral). Panel "Acerca de"
con créditos claros (basado en la idea de Terrasavr, motor y código propios, sin afiliación
con Re-Logic/tModLoader/CalamityMod/YellowAfterlife). "Builds" (equipo recomendado
vanilla+Calamity por etapa/clase, solo lectura) y "Novedades" (registro por versión de
Terraria) - ambos reutilizan JSON ya verificados del proyecto Electron.

**Bug real encontrado por el arranque real de la app** (no por lectura de código): la app
CRASHEABA al iniciar porque `whats_new.json` tiene el campo `items` como una lista de OBJETOS
(`{key,es,en}`), no de strings como se asumió al escribir el modelo por primera vez -
`System.Text.Json` lanzaba `JsonException` dentro del constructor de `CharacterFileService`,
que se llama desde el constructor de `MainViewModel`, que se llama desde el constructor de
`MainWindow` - eso hace que **cualquier** error de datos al arrancar aparezca como
`XamlParseException` en la traza (la excepción real está en la `InnerException`, hay que
mirarla siempre, no fiarse del mensaje de más arriba). Arreglado y cubierto con un test nuevo
contra el JSON real.

**Cómo se prueba esta app sin poder hacer clic en la ventana** (limitación conocida y ya
documentada en otros proyectos de este usuario - no hay `computer-use` disponible aquí para
esto): se lanza `TerrasavrNative.App.exe` en segundo plano redirigiendo stdout/stderr a un
archivo, se espera unos segundos, se comprueba con `tasklist` que el proceso sigue vivo (si
crashea al arrancar, el proceso desaparece y el stderr tiene la traza completa - así se
encontró el bug de `whats_new.json` de arriba). Esto confirma que la app arranca y cargan los
catálogos, pero NO sustituye a que el usuario abra la app y mire si algo se ve mal o si
cargar/guardar un personaje real funciona de punta a punta con los botones de verdad - eso
sigue pendiente de que lo prueben ellos.

### Exploración — primer resultado visible (commit `5d82441` + siguiente)

Lector de `.wld` completo (`WldFormat/`: cabecera mínima hasta dimensiones, salto directo a
`pointers[1]`/`pointers[4]` para tiles/NPCs sin parsear el resto de la cabecera
version-dependiente que no hace falta para pintar el mapa, RLE de tiles completo, NPCs con
posición real según homeless o no) - formato confirmado por investigación dirigida sobre el
lector JS ya real y verificado en `overrides.js` de Terrasavr-Calamity-Beta (a su vez
verificado ahí contra TEdit real), no adivinado. Verificado con 4 tests contra DOS mundos
reales de este PC (dimensiones realistas, rejilla completamente rellena por el RLE sin
excepciones, proporción de tiles activos en rango realista 15-95%).

`WorldRenderer` (App, no Core - usa `WriteableBitmap`) pinta el mundo entero a 1px/tile
(pared→tile→líquido, mezcla alfa) y se muestra con scroll para desplazarse. **Simplificación
deliberada**: sin el fondo degradado por zona (Espacio/Cielo/Tierra/Roca/Infierno) - necesita
`GroundLevel`/`RockLevel` de la cabecera, que `WldHeader` no lee a propósito (ver su propio
comentario) - fondo sólido oscuro por ahora. Sin zoom todavía (solo scroll a tamaño real, que
para un mundo Grande son ~8400x2400 píxeles) - pendiente añadir un control de zoom real.

**No verificado con la UI en sí** (misma limitación de siempre): el renderizado en pantalla al
pulsar "Cargar mundo" no se ha podido comprobar visualmente - la lógica de mezcla de color es
sencilla y de bajo riesgo (usa datos ya probados de `WldReader`/`MapColorCatalog`), pero
conviene que el usuario lo abra y mire si el mapa se ve bien de verdad.

### Fase 3 — Paridad funcional (buffs, ★ mejor prefijo, Investigación)

Retomada tras un aviso del usuario de que me había saltado el orden de fases del plan por
seguir sus peticiones más recientes (Exploración/Builds/Novedades/tema) - las dos cosas se
hacen, pero el plan manda primero.

- **Buffs**: pestaña nueva dentro de "Personaje", solo lectura. Nombres vanilla generados
  desde `BuffID.cs` decompilado (354 buffs) - **sin traducción real todavía** (`lang.zip` solo
  trae la categoría "Items", no una categoría de Buffs aparte con `BuffName` - se usa una
  versión "humanizada" del nombre interno en inglés como placeholder honesto, no inventado).
  Los buffs de Calamity (`modBuffs` en el `.tplr`) NO están fusionados todavía - mismo alcance
  documentado que el resto de piezas de Calamity pendientes.
- **Investigación**: lista de lo ya investigado (`PlrCharacter.Research`, ya lo leía el
  formato desde la Fase 1) + botón "Investigar todo" que añade una entrada por cada objeto
  conocido (vanilla + Calamity) con un conteo alto fijo - mismo criterio que la versión JS (no
  se conoce la tabla real de "cuántos hacen falta" por objeto). PID vanilla confirmado como el
  mismo nombre interno de `ItemID.cs` sin "/" (verificado cruzando "MoltenHelmet" contra
  `builds.json`, que ya usaba ese mismo formato).
- **Botón ★ (mejor prefijo)**: `PrefixSuggester` en Core, con test unitario. Vanilla y
  Calamity genérico vía `best_prefix.json` (243 objetos vanilla, 996 de Calamity, ya generado
  y verificado en Terrasavr-Calamity-Beta); armas Pícaro detectadas por `damageType` real
  (`RogueDamageClass.Instance` en `catalog.json`) usan en cambio el prefijo real más fuerte de
  Calamity (`RoguePrefixCatalog.Best.Weapon`), no la tabla genérica - mismo criterio que el
  README de la versión JS. Aparece como un botón ★ en la esquina de cada tarjeta de objeto,
  solo si hay una sugerencia real distinta del prefijo actual.
- **Mascota/montura/gancho + tintes**: ya estaba cubierto desde la Fase 2 (`miscEquips`/
  `miscDyes`, con fusión de Calamity incluida).

**Verificado**: 75 tests xUnit (6 nuevos de `PrefixSuggester`, con fixtures que distinguen
explícitamente "es Pícaro de verdad" de "está en la tabla genérica pero no es Pícaro" - un
caso donde una detección ingenua podría confundirse). `dotnet build` limpio, la app arranca
cargando también los 3 catálogos nuevos (`vanilla_buff_names.json`,
`vanilla_item_names_by_key.json`, `best_prefix.json`) sin excepciones.

**No verificado con la UI en sí**: pulsar el botón ★ de verdad, ver la lista de buffs/
investigación en pantalla, pulsar "Investigar todo" - misma limitación de siempre.

## Objetivo de diseño de la UI (pedido explícito, 2-sep-2026) - el tema actual NO es el final

El usuario confirmó explícitamente que el tema/layout actual (`Styles/Theme.xaml`, nav lateral,
tarjetas planas) es una primera pasada funcional, no el diseño definitivo. Reglas para la
ronda de pulido de verdad, cuando llegue (Fase 6, o antes si el usuario lo pide):

- **Super intuitiva** - flujos obvios sin tener que pensar, sin instrucciones.
- **Interactiva** - transiciones/feedback reales al usar la app (hover, seleccion, arrastrar
  objetos...), no una interfaz estatica de solo lectura.
- **Moderna** - referencia de lenguaje visual actual (Fluent 2/WinUI 3, o similar), no un
  clon de Win32 clasico.
- **Minimalista** - poco ruido visual, un solo acento de color (ya se eligio ambar/cobre, se
  puede revisar), tipografia limpia, espacio en blanco real.

Pedido explicito: "inspirate mucho, busca, informate" antes de esa ronda - no reinventar sin
mirar referencias reales de apps modernas de escritorio (WinUI 3 Gallery, apps nativas de
Windows 11 bien valoradas, etc.) en vez de improvisar de memoria. No hace falta hacerlo ya -
se sigue avanzando fase a fase con el tema actual mientras tanto, esto queda apuntado para
cuando toque esa ronda dedicada.

## Autonomia ampliada (2-sep-2026)

Pedido explicito del usuario: seguir con el resto de fases del proyecto sin parar a preguntar
en cada una - solo commit + bitacora en cada hito verificado, igual que se ha hecho hasta
ahora. Reportar de vuelta cuando haya algo sustancial que enseñar o si se llega a un bloqueo
real, no en cada paso intermedio.

### Fase 4 — Librería/Buscador

Decisión de diseño real (no solo copiar el original): en vez del árbol de carpetas paginado
del motor Haxe (que tenía un límite artificial de 19 hijos por carpeta, forzándolo a agrupar
categorías), se implementó **búsqueda por texto sobre el catálogo completo** (vanilla +
Calamity, ~8200 objetos) - más simple, más rápido de usar, y ya encaja con el objetivo de
diseño "moderna/minimalista" pedido por el usuario (ver la sección de más abajo) sin tener que
rehacerlo luego. Resultados limitados a 300 a la vez sin filtro activo (con el catálogo entero
sin buscar no tendría sentido pintarlo todo de golpe).

Iconos reales de Calamity copiados (3244 PNG, ~12MB, ya extraídos y verificados en
Terrasavr-Calamity-Beta). **Los iconos vanilla siguen sin resolver** - vienen en un atlas de
sprites (`img/items.png`/`img/nitems.png`) sin extraer todavía, los objetos vanilla se
muestran con un icono de reserva ("?") en la Librería por ahora.

**Verificado**: 75 tests siguen en verde (esta pieza es solo UI, sin lógica nueva en Core que
testear), `dotnet build` limpio, la app arranca con los iconos copiados a la carpeta de salida
(3244 archivos confirmados). No verificado con clics reales (escribir en el buscador, ver los
resultados de verdad) - misma limitación de siempre.

### Edición real de objetos (añadir/quitar/cantidad)

Antes solo se podía cambiar el prefijo de un objeto ya existente (botón ★). Ahora cada slot
(`ItemSlotViewModel`) soporta:

- **Colocar/cambiar objeto**: botón "Elegir objeto.../Cambiar objeto" que pone ese slot como
  `LibraryViewModel.PickTarget`, salta automáticamente a la pestaña Librería
  (`MainViewModel.SelectedTabIndex`, enlazado al `SelectedIndex` del `TabControl` externo) y
  muestra un aviso ambar + un botón "Colocar aquí" en cada tarjeta de resultado mientras dura
  la selección. Al pulsar una tarjeta, `LibraryViewModel.PlaceInTargetCommand` llama a
  `ItemSlotViewModel.PlaceItem(id)` (objeto nuevo: cantidad 1, sin prefijo, sin `GlobalData`
  heredado - es un objeto distinto) y dispara el evento `ItemPlaced`, que `MainViewModel`
  escucha para volver solo a la pestaña Personaje.
- **Vaciar slot**: botón ✕ visible solo si el slot no está vacío (`IsNotEmpty`, propiedad
  calculada a partir de `IsEmpty` con `OnIsEmptyChanged` notificando el cambio), llama a
  `GameItem.Empty`.
- **Cantidad editable**: `TextBox` de dos vías sobre `ItemSlotViewModel.Count`, con
  `OnCountChanged` escribiendo de vuelta en `Item.Count` y sujetando el valor entre 1 y 9999
  (0 no tiene sentido con un id de objeto puesto - para eso está el botón Vaciar explícito).
  Un flag `_suppressCountWriteback` evita que `UpdateFrom` (recarga desde el modelo) dispare
  una escritura de vuelta espuria.

`ItemSlotCard` (`Theme.xaml`) se agrandó de 132×56 a 148×92 para hacer sitio a los botones
nuevos y la fila de cantidad sin apretar el texto.

**Verificado**: 75 tests xUnit siguen en verde (edición vive solo en la capa de ViewModels,
sin lógica nueva en Core), `dotnet build` limpio, la app arranca sin excepción (mismo método
de lanzar en segundo plano + comprobar el proceso con `tasklist`). **No verificado con clics
reales** (elegir un objeto de verdad, escribir una cantidad, guardar y releer el `.plr` para
confirmar que el nuevo objeto persiste) - misma limitación de siempre en este entorno.

### Auto-equipar desde Builds

Cada grupo clase/etapa del panel Builds tiene ahora un botón "Auto-equipar" que coloca ese
equipo de verdad en el personaje cargado, en vez de ser solo informativo:

- **Resolución `pid` → objeto real** (`Core/Calamity/BuildItemResolver.cs`): un `pid` con `/`
  es de Calamity (`mod/nombreInterno`, resuelto contra `CalamityCatalog.ByModAndInternal`);
  sin `/` es vanilla (nombre interno tal cual, resuelto contra el nuevo
  `VanillaItemCatalog.GetIdByKey`). El prefijo sugerido en el JSON se resuelve aparte: campo
  `prefix` (nombre interno en inglés, ej. `"Legendary"`) contra el nuevo
  `VanillaPrefixCatalog.ByInternal`, o campo `prefixId` (Calamity/Pícaro, id sintético
  ≥10000) directo a `ItemPrefix.CalamitySynthetic`.
- **`vanilla_item_ids_by_key.json` (nuevo asset)**: hasta ahora `VanillaItemCatalog` solo
  tenía nombre-interno→nombre-mostrado, no nombre-interno→id real - hacía falta para resolver
  el `pid` de builds.json a un `GameItem.Id` real. Generado con el mismo criterio que
  `vanilla_item_names.json` (regex `public const short Nombre = valor;` sobre `ItemID.cs`
  decompilado, carpeta `tModLoader\` no `TerrariaVanilla\`), 5455 entradas, verificado contra
  hechos ya conocidos (`IronPickaxe`→1, `MoltenHelmet`→231).
- **Colocación**: armadura → los 3 primeros slots del equipo puesto (cabeza/cuerpo/piernas),
  accesorios → los 5 siguientes (mismo contenedor `loadoutArmor` ya mostrado en Personaje) -
  estos 8 slots se SOBRESCRIBEN a propósito, es justo lo que el botón promete. Armas → primer
  hueco libre del inventario (Terraria no tiene un "slot de arma" fijo), sin sobrescribir
  nada. Objetos sin resolver o sin hueco se cuentan aparte y se avisan en el mensaje de
  estado; la pestaña vuelve sola a Personaje al terminar.

**Verificado**: 80 tests xUnit (5 nuevos: `BuildItemResolverTests` con fixtures que cubren
vanilla+prefijo vanilla, vanilla sin prefijo, Calamity+prefijo sintético, y `pid` no
encontrado; más `VanillaItemCatalogRealFileTests` contra el asset real nuevo). `dotnet build`
limpio, la app arranca sin excepción con el asset nuevo copiado. **No verificado con clics
reales** (pulsar Auto-equipar de verdad y comprobar el resultado en pantalla) - misma
limitación de siempre.

### Buscador/roster de NPCs en Exploración

- **`VanillaTownNpcRoster`** (`Core/Data`, nuevo): los 27 NPCs de pueblo reales de Terraria
  vanilla, portados literal de `VANILLA_TOWN_NPC_ROSTER` en `overrides.js` de
  Terrasavr-Calamity-Beta - `[17, 18, 19, 20, 22, 37, 38, 54, 107, 108, 124, 142, 160, 178,
  207, 208, 209, 227, 228, 229, 353, 369, 453, 550, 588, 633, 663]`.
- **`ExplorationViewModel`**: guarda la lista completa de NPCs encontrados (`_allNpcs`) aparte
  de la colección enlazada a la UI (`Npcs`), que ahora se recalcula por texto
  (`NpcSearchText`/`ApplyNpcFilter`, mismo patrón que `LibraryViewModel`). Al cargar un mundo
  se calcula tambien `MissingNpcs` (nombres reales de los ids del roster que NO aparecen en
  `world.Npcs`) - aparece en un `Expander` "NPCs que faltan" bajo la lista, en rojo Calamity
  para que resalte. `WorldNpcRowViewModel` gana el campo `Id` (antes solo nombre/posicion,
  hacia falta para el diff contra el roster) y ahora marca "- sin casa" en la posicion si el
  NPC no tiene casa asignada (`Homeless`, ya leido por `WldReader` pero sin usar en la UI).

**Verificado**: 82 tests xUnit (2 nuevos: `VanillaTownNpcRosterTests`, cuenta y sin
duplicados, mas dos ids conocidos). `dotnet build` limpio, la app arranca sin excepción. **No
verificado con clics reales** (buscar de verdad, cargar un mundo real y comprobar que la
lista de "NPCs que faltan" tiene sentido) - misma limitación de siempre.

### Iconos vanilla reales (ya extraídos)

`img/items.png` (Terrasavr-Calamity-Beta) resultó ser una rejilla uniforme: 32 columnas,
celdas de 40x40, **índice = id real del objeto directamente, fila-mayor** (`row = id / 32,
col = id % 32`) - confirmado visualmente recortando la celda de `id=1` (Pico de hierro) y
`id=231` (Casco fundido/`MoltenHelmet`, el mismo objeto ya usado como caso de prueba en
`vanilla_item_ids_by_key.json`) y viendo que el sprite es el correcto. Sin necesidad de tocar
`script.js`/decompilar nada - se dedujo del propio tamaño de la imagen (1280x7720) más el
comentario ya existente en `overrides.js` ("items.png's own per-item cells are already
exactly 40x40").

Extraídos con `pngjs` (ya instalado en Terrasavr-Calamity-Beta) los 5455 ids reales de
`vanilla_item_names.json` a PNGs individuales, uno por id, en
`TerrasavrNative.App/Assets/vanilla/icons/{id}.png` - **5454 de 5455** (el único que falta,
`1189`/`Taladro de paladio`, cae en una celda totalmente transparente en el atlas original;
limitación real y documentada, no un fallo de la extracción - mismo criterio que las ~13
traducciones vanilla sin equivalente real). `nitems.png` (ids negativos/objetos obsoletos) se
descarta a propósito, mismo criterio que ya se aplicó al generar
`vanilla_item_ids_by_key.json` (solo ids > 0).

`VanillaIconResolver` (nuevo, `App/Services`) resuelve id → ruta `pack://siteoforigin` si el
archivo existe, null si no (el objeto sin icono real, o cualquier id fuera de rango) - la
Libreria ya lo usa para los objetos vanilla en vez de mostrar siempre el "?" de reserva.

**Verificado**: 82 tests xUnit siguen en verde (extracción es un script Node puntual fuera
del proyecto .NET, sin lógica nueva en Core), `dotnet build` limpio, la app arranca con los
5454 PNGs copiados a la carpeta de salida. **No verificado con clics reales** (abrir la
Libreria y ver los iconos vanilla de verdad en pantalla) - misma limitación de siempre.

### Buffs de Calamity (modBuffs) fusionados

`CalamityCharacterSync` gana `MergeBuffs`/`SyncBuffs`, invocados desde `MergeAll`/
`MaskAndSyncAll` igual que los 7 contenedores de items:

- **`CalamityBuffCatalog`** (nuevo, `Core/Data`, mismo patrón que `CalamityCatalog`): carga
  `calamity/buffs.json` (305 buffs reales), id sintético = `CalamityIds.BuffIdBase` (25000000,
  nuevo) + índice en el array.
- **Fusión (carga)**: a diferencia de los contenedores de items, `modBuffs` en el `.tplr` NO
  está filtrado a "mod != Terraria" - confirmado en el propio comentario de `overrides.js`:
  tModLoader desactiva el guardado del array de buffs vanilla nativo en cuanto un mod añade
  sus propios slots, así que para un personaje modeado `modBuffs` es la ÚNICA fuente completa,
  buffs vanilla incluidos. Por eso `MergeBuffs` no fusiona "por encima" de `character.Buffs`
  sino que rellena huecos vacíos en orden, uno por entrada de `modBuffs` (respeta cualquier
  buff que el array nativo ya trajera puesto, si lo trajera).
- **Sincronización (guardado)**: `SyncBuffs` reconstruye `modBuffs` entero desde
  `character.Buffs` (mismo criterio que `calamitySyncBuffsFromPlayer` real, sin preservar nada
  del `modBuffs` anterior) - id < `BuffIdBase` → `mod=Terraria,id`; id ≥ `BuffIdBase` →
  `mod,name` reales vía `BySyntheticId`. **Deliberadamente NO se enmascara** el id sintético en
  `character.Buffs` antes de escribir el `.plr` (a diferencia de los items) - confirmado que la
  versión JS tampoco lo hace (`calamityMaskForVanillaSave` nunca toca `player.buffs`), y es
  seguro replicarlo tal cual porque el escritor nativo de buffs no aplica ningún clamp de rango
  (a diferencia del de items, que sí necesita el masking para no corromper la carga) y
  tModLoader deja de leer ese array en cuanto hay mods instalados.
- **UI**: `BuffRowViewModel.From` ahora resuelve nombre vanilla o Calamity según el id, con
  borde rojo Calamity igual que el resto de objetos de Calamity en pantalla.

**Verificado**: 85 tests xUnit (3 nuevos: mezcla vanilla+Calamity respetando huecos ya
ocupados, sincronización de vuelta con el formato correcto por origen, y round-trip completo
merge→sync→merge preservando un buff de Calamity). `dotnet build` limpio, la app arranca sin
excepción con `buffs.json` copiado. **No verificado con clics reales** (ver un buff de
Calamity de verdad en pantalla, guardar y comprobar que sigue ahí al recargar) - misma
limitación de siempre.

### Armadura/tinte de Calamity POR LOADOUT fusionados - último hueco de paridad de Calamity cerrado

`CalamityCharacterSync` gana `MergeLoadoutArmorDye`/`SyncLoadoutArmorDye`, puerto literal de
`calamityMergeArmorDyeIntoPlayer`/`calamitySyncArmorDyeFromPlayer` reales:

- **Vista combinada por loadout**: cada uno de los 4 loadouts conceptuales (`[PrimaryLoadout,
  ...Loadouts]`, índice 0 = mirror de "lo puesto", 1-3 = los 3 loadouts reales) gana claves
  `loadout{i}Items`/`loadout{i}Social`/`loadout{i}Dyes` en el mismo diccionario que ya
  devuelve `MergeAll` para los 7 contenedores planos - armadura+vanidad viven en un único
  espacio conceptual de 20 slots en el `.tplr` (0-9 armadura/accesorios, 10-19 vanidad, mismo
  criterio que `calamityArmorSlot` real), repartido de vuelta a `Items`/`Social` tras fusionar.
- **Dos rutas de fusión, igual que el original**: (1) claves PLANAS `armor`/`dye` del
  `.tplr`, que van al "loadout activo" - **`calamityActiveLoadout(player)` real usa
  `player.loadouts[currentLoadout]` SIN el `+1` que sí usa el guardado vanilla nativo**
  (confirmado leyendo `overrides.js`, era el punto marcado como "sin verificar" en el plan
  original). Con `CurrentLoadout==0` cae en el mirror (lo que la UI ya muestra como "equipo
  puesto"); con `CurrentLoadout>=1` cae en `Loadouts[CurrentLoadout-1]` **en vez de** en el
  mirror - una discrepancia real de la app original entre lo que esta ruta fusiona y lo que el
  juego/la UI consideran "puesto de verdad", replicada tal cual, no corregida. (2) claves POR
  LOADOUT `loadout{i}Armor`/`loadout{i}Dye` (una por cada uno de los 4), sin ambigüedad,
  ejecutadas después de la ruta plana.
- **`PlrLoadout.Items`/`Social`/`Dyes` son propiedades `init`-only** (no se puede reasignar el
  array tras construirlo) - `SyncLoadoutArmorDye` muta sus elementos uno a uno en vez de
  reasignar la referencia, sin tocar el modelo de datos.
- **UI**: los contenedores "Equipo puesto" en Personaje pasan de leer
  `Character.PrimaryLoadout.Items/Social/Dyes` en crudo a leer `MergedContainers["loadout0Items
  /Social/Dyes"]` (ya con Calamity fusionado) - mismo mecanismo genérico de sincronización de
  vuelta (`SyncEditsBackToMerged`) que ya usaban los 7 contenedores planos, sin código nuevo en
  el ViewModel aparte de las claves. Loadouts 1-3 (los 3 loadouts reales seleccionables)
  **siguen sin tener panel propio en la UI** - se fusionan/sincronizan igualmente (no se pierde
  nada al guardar), pero no hay dónde verlos/editarlos todavía.

**Verificado**: 88 tests xUnit (3 nuevos: fusión de armadura+vanidad+tinte vía claves planas
con `CurrentLoadout=0`, el quirk de índice confirmado con `CurrentLoadout=1` cayendo en
`Loadouts[0]` y NO en el mirror, y un round-trip completo merge→sync→merge). `dotnet build`
limpio, la app arranca sin excepción, y la prueba de extremo a extremo contra el personaje
real "adrian" (que ya pasaba por este camino sin la lógica de loadouts) sigue en verde. **No
verificado con clics reales** (ver equipo de Calamity puesto de verdad en pantalla) - misma
limitación de siempre. Con esto, el único hueco documentado de la Fase 1 que queda es
coins/ammo/tempItems, que la propia app JS tampoco sincroniza con Calamity (solo protege).

**Añadido en el mismo commit siguiente**: los 3 loadouts reales (Loadouts[0..2], solo si
`Version>=269`) ganan sus propios contenedores en la pestaña Objetos ("Loadout 1/2/3 -
armadura/accesorios/vanidad/tintes"), reutilizando el mismo mecanismo genérico de
`AddContainer`/`SyncEditsBackToMerged` que ya tenían el resto - sin código nuevo en el
ViewModel aparte del bucle sobre `Character.Loadouts`. Antes solo se veía/editaba el loadout 0
(equipo puesto).

### Zoom real en el visor de mundo

`ExplorationViewModel.Zoom` (double, 1.0 por defecto, sujeto entre 0.1x y 6x en
`OnZoomChanged`) + `ZoomInCommand`/`ZoomOutCommand`/`ZoomResetCommand` (x1.25 por paso).
Aplicado con un `ScaleTransform` en `Image.LayoutTransform` (no `RenderTransform` - con
`LayoutTransform` el `ScrollViewer` que ya envolvía la imagen recalcula solo el area
desplazable al tamaño escalado, con `RenderTransform` los scrollbars no se habrían enterado
del nuevo tamaño). Botones +/-/Restablecer en la barra de Exploración, más Ctrl+rueda del
ratón sobre el mapa (`OnWorldMapPreviewMouseWheel` en el code-behind, solo captura el evento
cuando Ctrl está pulsado para no robarle el scroll normal al `ScrollViewer`). Se reinicia a
1.0 en cada carga de mundo.

**Verificado**: 88 tests siguen en verde (pieza puramente de UI, sin lógica nueva en Core),
`dotnet build` limpio, la app arranca sin excepción. **No verificado con clics reales** (zoom
de verdad con los botones o Ctrl+rueda) - misma limitación de siempre.

### Tooltip por tile (nombre exacto de variante) en el visor de mundo

`WldTile` gana los campos `U`/`V` (ya se leían durante el RLE para no desalinear el archivo,
pero se descartaban - ver el historial del archivo). `TileNameCatalog` gana
`TileVariantName(type, u, v)`: `tile_names.json` guarda un objeto `frames` por tile con clave
literal `"u,v"` **en píxeles reales de la hoja de sprites** (ej. tile 21/Cofres: `"36,0"` →
"Cofre de oro", `"648,0"` → "Cofre de la selva" - el caso citado en la documentación del
proyecto, mismo id de tile, sprite distinto) - si no hay coincidencia exacta (o el tile no
tiene `frames` en absoluto) cae al nombre base, nunca se inventa una variante.

En la UI: `ExplorationViewModel` guarda el `WldWorld` cargado (antes era una variable local
que se tiraba tras pintar el mapa) y expone `UpdateHover(tileX, tileY)` → `HoverInfo` (texto
"(x, y) - Nombre de tile / pared: Nombre de pared"). El code-behind engancha `MouseMove`/
`MouseLeave` sobre la `Image` del mapa - `e.GetPosition(WorldMapImage)` ya devuelve la
posición en píxel NATIVO de la imagen (WPF deshace el `LayoutTransform` del zoom
automáticamente), y como `WorldRenderer` pinta 1 píxel = 1 tile, ese píxel ES la coordenada de
tile directamente, sin cuentas propias. Se muestra en una franja bajo el mapa (visible solo
con `HoverInfo` no vacío vía `EmptyToCollapsed`), no como un `ToolTip` de WPF flotante - más
simple y fiable que gestionar el `Popup`/temporización propios de `ToolTip`.

**Verificado de extremo a extremo con datos reales** (no solo fixtures): un test nuevo
(`TileVariantRealFileTests`) recorre un mundo real (`El_Musgo_de_Accidentes.wld`) buscando
cofres (tile 21) y confirma que el `u`/`v` real leído del archivo resuelve variantes
correctas - salida real del test: `u=72,v=0` → "Cofre de oro (con candado)", `u=648,v=0` →
"Cofre de la selva", `u=612,v=0` → "Cofre de agua", `u=972,v=0` → "Cofre congelado (con
candado)". 94 tests xUnit en total (6 nuevos: 5 de `TileNameCatalogTests` con fixture propia +
el de arriba). `dotnet build` limpio, la app arranca sin excepción. **No verificado con clics
reales** (mover el ratón sobre el mapa de verdad en la UI) - misma limitación de siempre, pero
la lógica que alimenta el tooltip ya está confirmada con datos reales a nivel Core.

### Sprites reales en TODO lo que faltaba (inventario, buffs, NPCs, Builds)

Pedido explícito del usuario ("¿todo tendrá también sus sprites?") - hasta ahora solo la
Librería mostraba iconos reales; el resto (tarjetas de objeto en Personaje, buffs, NPCs de
Exploración, panel Builds) se quedaba en texto. Cerrado en un único barrido:

- **`img/buffs.png` es la MISMA rejilla que `items.png`** (32 columnas, celdas 40x40, índice =
  id real del buff en fila-mayor) - confirmado leyendo el comentario real de `overrides.js`
  ("40*(id&31), 40*(id>>5) in drawBuff") y verificado visualmente contra `id=1`/Obsidian Skin
  e `id=353`/Shimmer. Extraídos **354/354** buffs vanilla a
  `Assets/vanilla/buff_icons/{id}.png` (cobertura del 100%, a diferencia de los objetos donde
  faltaba 1 de 5455).
- **Iconos de buff de Calamity** (308, ya extraídos en Terrasavr-Calamity-Beta) y **de NPC**
  (27 - solo cubre `VanillaTownNpcRoster`, los mismos que la app JS original tenía
  extraídos para su buscador de NPCs) copiados tal cual.
- **`VanillaBuffIconResolver`/`NpcIconResolver`** (nuevos, `App/Services`) - mismo patrón que
  `VanillaIconResolver`: id → ruta si el archivo existe, null si no.
- **`BuffRowViewModel`**, **`WorldNpcRowViewModel`** (y el nuevo **`MissingNpcRowViewModel`**,
  antes la lista de "NPCs que faltan" era solo `ObservableCollection<string>`) ganan
  `IconPath`, resuelto vanilla/Calamity según corresponda.
- **`ItemSlotViewModel`** (tarjetas de objeto en Personaje - Inventario/Banco/Equipo puesto/
  Loadouts...) gana `IconPath`, misma resolución que ya usaba la Librería. `ItemSlotCard`
  (`Theme.xaml`) se ensancha de 148 a 188 para hacer sitio al icono de 32x32 sin apretar el
  texto.
- **Panel Builds**: `BuildItemRef` (Core) no tiene noción de icono/ruta de asset a propósito
  (Core no debe saber de `pack://` ni de rutas de App) - se crearon
  `BuildItemRowViewModel`/`BuildClassGearViewModel`/`BuildStageViewModel` (nuevos, `App/
  ViewModels`) que envuelven los tipos de Core y resuelven el icono por `pid` (misma lógica de
  `BuildItemResolver` pero solo para mostrar, no para colocar) al construir `BuildsViewModel` -
  que ahora recibe también `CharacterFileService`. `BuildClassGearViewModel.Source` conserva
  el `BuildClassGear` original para que "Auto-equipar" siga funcionando sin cambios.

**Verificado**: 94 tests xUnit siguen en verde (pieza de UI + extracción, sin lógica nueva en
Core que testear), `dotnet build` limpio, la app arranca sin excepción con los 354+308+27
iconos nuevos copiados a la salida. **No verificado con clics reales** (ver los iconos de
verdad en pantalla) - misma limitación de siempre.

### Fondo degradado por zona en el visor de mundo

`WldHeader` se amplía hasta `GroundLevel`/`RockLevel`/`SpawnX`/`SpawnY` (antes el lector paraba
justo después de las dimensiones, a propósito). Campos leídos en el mismo orden exacto que
`parseWorldHeader` real (`overrides.js`): GameMode + hasta 9 bools condicionados por versión
(o 1 bool si `version==208||version>=112` y `<209`), CreationTime/LastPlayed (8 bytes cada
uno si aplica), MoonType, 3+4+3+4+3 enteros de árboles/fondos de cueva, `SpawnX`/`SpawnY`
(`Int32`), `GroundLevel`/`RockLevel` (**`Double`**, no entero). Como la lectura de tiles ya
saltaba directamente a `pointers[1]` (no seguía leyendo secuencialmente desde la cabecera), un
fallo aquí solo podría estropear estos 4 campos nuevos, nunca desalinear el mapa - aun así se
verificó con datos reales: `GroundLevel`/`RockLevel`/`Spawn` de los 2 mundos reales de este PC
salen coherentes (orden Ground<Rock<alto del mundo, spawn por encima de la roca) - ver
`WldReaderRealFileTests.Read_RealWorld_GroundRockLevelsAndSpawnAreSane`.

`WldHeader.ZoneFor(worldY)` decide la zona por profundidad (Espacio si y<80, Infierno en las
últimas 192 filas, Roca/Tierra según RockLevel/GroundLevel, Cielo el resto) - mismo criterio
que `zoneFor` real. `WorldRenderer` ya no usa un fondo sólido fijo: calcula el color de zona
una vez POR FILA (no por píxel, la profundidad no cambia por columna) vía
`MapColorCatalog.Global(zona)` - ese método y los datos de `map_colors.json` (`"global"`) ya
existían de antes, sin usar; solo hacía falta la cabecera.

**Verificado**: 97 tests xUnit (2 tests reales nuevos de cabecera + 1 fixture de `ZoneFor` con
los 5 límites exactos de zona). `dotnet build` limpio, la app arranca sin excepción. **No
verificado visualmente** (ver el degradado de verdad en el mapa renderizado) - el pipeline de
renderizado en sí (`WorldRenderer`) no tiene tests automatizados propios en ninguna fase
anterior tampoco, coherente con el resto del proyecto.

### Panel de Apariencia

Nueva pestaña interna "Apariencia" (dentro de Personaje, junto a Objetos/Buffs/Investigación):
género, estilo de pelo (id), tinte de pelo (id) y los 7 colores reales del personaje (pelo,
piel, ojos, camisa, camiseta interior, pantalones, zapatos) - todos campos que `PlrCharacter`
ya traía leídos desde la Fase 1 pero sin ningún panel para verlos/editarlos.

- **`ColorSwatchViewModel`** (nuevo): envuelve el `byte[3]` REAL de `PlrCharacter` (ej.
  `HairColor`) y escribe en el mismo array en cuanto cambia un canal - no hace falta
  sincronizar nada al guardar, `Save()` ya escribe el mismo `PlrCharacter` que comparte el
  array (mismo patrón de "mutar en sitio" ya usado para `PlrLoadout` en la fusión de
  armadura/tinte). R/G/B expuestos como `int` (0-255) para enlazar `Slider` sin conversor, más
  un `Brush` calculado para la muestra de color.
- **`AppearanceViewModel`** (nuevo): `HairStyle`/`HairDye` (numéricos, sin catálogo de nombres
  - no hay uno real disponible) y `IsMale`/`IsFemale` (género - convención vanilla estándar,
  `Gender==1`→chico, la variante invertida documentada en el proyecto es de formatos
  `version<145` fuera del alcance de este lector). `IsFemale` es un espejo de `IsMale` para
  poder enlazar dos `RadioButton` por dos vías sin un conversor dedicado.
- **Sin preview de sprite compuesto a propósito**: dibujar el personaje completo por capas
  (pelo+cuerpo+ropa) necesitaría el atlas de sprites del jugador, que no está extraído - las
  muestras de color SÍ son el color real de cada parte, solo no hay una silueta encima. Queda
  anotado como posible ampliación futura, no bloquea el panel.

**Verificado**: 97 tests xUnit siguen en verde (pieza de UI pura, sin lógica nueva en Core que
testear), `dotnet build` limpio, la app arranca sin excepción. **No verificado con clics
reales** (mover un slider de verdad, guardar y comprobar que el color persiste) - misma
limitación de siempre.

### Instalador (Fase 6 del plan) - probado de extremo a extremo en este PC

`installer/install.ps1` + `installer/uninstall.ps1`, solo PowerShell + el objeto COM
`WScript.Shell` (integrado en Windows) - sin MSI/WiX ni ninguna herramienta externa.

- **Perfil de publicación** (`TerrasavrNative.App/Properties/PublishProfiles/win-x64.pubxml`):
  se probó primero **autocontenido** (`SelfContained=true`) y salió un `.exe` de **140MB** -
  WPF/PresentationFramework no se puede recortar de forma segura (`PublishTrimmed`), así que
  arrastra el runtime completo entero, casi tan pesado como la propia versión Electron que se
  quería dejar atrás. Cambiado a **dependiente del framework** (`SelfContained=false`,
  `PublishSingleFile=true`) - **~27MB** publicados (sobre todo los iconos reales; el propio
  `.exe` de un solo archivo pesa <1MB), único requisito real: tener instalado el .NET Desktop
  Runtime 10 - razonable para uso propio en este PC (es donde se ha compilado/ejecutado toda
  la sesión).
- **Bug real encontrado probando de verdad** (no hipotético): la primera vez que
  `install.ps1` corrió, la app instalada arrancaba con
  `FileNotFoundException: ...\Assets\calamity\catalog.json` - **`dotnet publish` reutilizando
  una carpeta `publish/` con caché incremental de una publicación ANTERIOR con ajustes
  distintos** (aquí: autocontenido probado primero, luego dependiente del framework en el
  MISMO directorio) puede terminar **sin copiar ningún `Content` (todo `Assets/*.json`/
  `*.png`) y sin ningún error ni aviso** - el build en sí sale "correcto". Confirmado
  republicando a una carpeta nueva (sí funcionó) y luego reproducido borrando `publish/`
  primero (también funcionó). Arreglado en el propio `install.ps1`: borra `publish/` SIEMPRE
  antes de publicar, más una comprobación defensiva después (`Test-Path` de `catalog.json`,
  aborta con error claro si falta) para que este mismo fallo silencioso no pueda colar una
  instalación rota una segunda vez por otra causa.
- **`install.ps1`**: publica, copia a `%LocalAppData%\Programs\Terrakeep`, crea acceso directo
  en el menú Inicio (y en el Escritorio con `-Desktop`), y copia `uninstall.ps1` dentro de la
  propia instalación para poder desinstalar sin necesitar el repo a mano.
- **`uninstall.ps1`**: borra los accesos directos y programa el borrado de la carpeta de
  instalación en un proceso aparte con un pequeño retardo (no puede borrarse a sí mismo
  mientras sigue corriendo desde dentro de esa carpeta) - mismo patrón que un desinstalador
  real de Windows.

**Verificado de extremo a extremo en este PC, no solo "debería funcionar"**: `install.ps1`
ejecutado de verdad (instaló en `%LocalAppData%\Programs\Terrakeep`, acceso directo real
creado), el `.exe` instalado lanzado y confirmado con `tasklist` que sigue vivo, **el acceso
directo del menú Inicio lanzado con `Start-Process` sobre el `.lnk`** (mismo camino que un
doble clic real) y confirmado con `Get-Process`, `uninstall.ps1` ejecutado y confirmado que
borra carpeta + accesos directos, y una segunda instalación limpia después para dejar la app
disponible. 97 tests xUnit siguen en verde (sin tocar Core/App).

### Preview de personaje real en Apariencia (pedido explícito, 1-sep-2026)

Investigado a fondo (auditoría dedicada, ver la sección de más abajo) el sistema real de
sprites del jugador de Terrasavr y **verificado visualmente generando y viendo 4 previews
reales** (pelo distinto, género distinto - salen cabezas de Terraria reconocibles de verdad,
con el pelo/piel/camisa teñidos correctamente).

- **Atlas real**: `img/visual.png` (copiado a `Assets/player/visual.png`, 640x416, ~22KB).
  Confirmado NO es un cuerpo entero sino un **icono de cabeza pequeño** - recortando cada
  parte a mano, casi todo el lienzo de 40x56 es transparente salvo un detalle facial/de
  cuello pequeño en la esquina superior izquierda.
- **9 "partes"** superpuestas TODAS en el mismo origen (0,0) - formato confirmado leyendo el
  motor real (`Sa`/`app.TabMain` + `qa`/`app.BitPart` en `script.readable.js`): 8 partes de
  40x56 (base sin tinte, piel, ojos, piel de torso, camisa, camiseta interior, pantalones,
  zapatos - offsets X exactos documentados en `PlayerPreviewRenderer.cs`) + el pelo (40x40,
  **134 estilos** en rejilla de 16 columnas, `x=40*(id&15), y=56+40*(id>>4)` - misma fórmula
  de rejilla que ya se usó para `items.png`/`buffs.png`).
- **Tintado = multiplicación RGB pura** sobre el sprite ya recortado (sin tocar alfa) - mismo
  mecanismo que `BitPart.set_color` real. Los sprites base están en gris/blanco para que el
  multiply dé el color final.
- **`PlayerPreviewRenderer`** (nuevo, `App/Services`): compone las 9 partes en un
  `WriteableBitmap` de 40x56, recalculado en cada cambio de pelo/género/color desde
  `AppearanceViewModel` (suscrito al `PropertyChanged` de cada `ColorSwatchViewModel`).
- **Género no verificado al 100%**: el motor real decide con `gender>=4?0:1`, un umbral sin
  sentido para el `Gender` de 0/1 simple que lee este proyecto (probablemente una
  codificación de un formato mucho más antiguo, fuera de alcance) - se usa `IsMale` en su
  lugar para elegir entre las dos variantes de sprite, documentado como asunción razonable
  pero no verificada pixel a pixel.
- **NO se clona el layout roto del original**: la propia UI de Terrasavr trae la etiqueta
  literal `"(preview is broken atm)"` en este panel - nunca tuvo un posicionamiento que
  funcionara del todo. Esta versión compone las 9 partes apiladas en el mismo origen, que es
  justo lo que la rejilla real espera (y es exactamente lo que produjo un preview correcto al
  probarlo).

**Verificado**: generado un proyecto WPF de usar-y-tirar en el scratchpad que invoca
`PlayerPreviewRenderer.Render` directamente con 4 combinaciones (pelo 0/16/50, chico/chica) y
guarda el resultado a PNG - las 4 imágenes muestran cabezas de Terraria reales y coherentes,
no basura ni sprites descolocados. 97 tests xUnit siguen en verde (`PlayerPreviewRenderer` es
puro `App`, sin lógica nueva en `Core`), `dotnet build` limpio, la app arranca sin excepción.

### Auditoría exhaustiva Terrasavr JS vs puerto nativo (pedido explícito, 1-sep-2026)

Pasada dedicada leyendo `overrides.js`/`script.readable.js` (versión legible del motor
compilado, en `tModLoader-Decompiled\Terrasavr-script-readable\`) completos, no solo greps
puntuales, para confirmar que no queda ninguna función real de Terrasavr sin portar. Hallazgos
NO barridos todavía (quedan documentados aquí para la siguiente ronda, priorizados):

1. **Editor de buffs completo** (`app.TabEffects`, clase `L`) - el puerto solo LEE buffs
   (`BuffRowViewModel`), el original permite añadir/quitar/cambiar duración por slot, más
   guardar/cargar/añadir presets de buffs a un fichero `.json`/`.tsb`. `PlrCharacter.Buffs`
   ya soporta escritura (usado internamente), solo falta la UI de edición.
2. **Selector de prefijo manual categorizado** (`app.TabEdit`, clase `X`) - el puerto solo
   tiene el botón ★ (mejor prefijo auto-sugerido). El original tiene un picker clicable de
   TODOS los prefijos agrupados por categoría, más el nombre del prefijo actual coloreado por
   tier de rareza. Nota: el propio original solo tenía botón dedicado para 1 de los 21
   prefijos de Calamity (el resto había que teclear el id a mano) - un selector categorizado
   sería una mejora real sobre el propio original, no solo paridad.
3. **Panel de "Spawn Points" (servidores favoritos)** - dato ya leído/escrito
   (`PlrCharacter.Servers`/`PlrServerEntry`, `PlrBodySerializer`), cero UI. Cada entrada:
   nombre, dirección, `spawnX`/`spawnY`.
4. **Campos de estadísticas del personaje sin panel** (`app.TabMain`) - TODOS ya leídos en
   `PlrCharacter.cs`, solo falta UI (mismo patrón que `AppearanceViewModel`, sin parsing
   nuevo): `Difficulty` (Softcore/Mediumcore/Hardcore/Journey), `HealthNow`/`HealthMax`/
   `ManaNow`/`ManaMax`, `FishingQuestsCompleted`, `GolferScore`, tiempo jugado (el puerto solo
   tiene `PlayTimeLow`/`PlayTimeHigh` en crudo, sin exponer como segundos). El hueco más
   barato de cerrar. `BartenderQuests` NO añadir - el propio JS lo tiene eliminado de la UI a
   propósito (`this.remove(this.lbBarQuests)`), es basura reconocida por el propio autor.
5. **Cambiar la versión objetivo del guardado** (`app.TabVersion`) - nicho/avanzado,
   `PlrCharacter.Version` ya existe sin editor. Prioridad baja (riesgo si se usa mal).
6. **`TabFlags` ("Edit permanent buffs")** - contenido real SIN determinar en esta pasada
   (necesita una pasada dedicada antes de decidir si implementarlo).
7. **UI de loadouts como pestañas 1/2/3 conmutables** - el puerto ya expone los 3 loadouts
   reales como contenedores planos en Objetos (mismos datos, distinta UX) - prioridad baja.

**No son huecos reales** (mismo límite que ya tenía el propio original, o decisión de diseño
ya tomada y documentada): el campo "Code" de edición de objetos (exclusivo del build web sin
tModLoader, este proyecto es 100% tModLoader así que no aplica); `TabShelf` (bandeja temporal
de items - la Librería como selector directo por slot ya cubre el mismo caso de uso).

## Pendiente (visible desde fuera)

- Huecos 1-6 de la auditoría de arriba, por orden de prioridad (el 4 es el más barato).

## Reglas de este proyecto (heredadas de las globales, sin repetirlas todas)

- Commit tras cada cambio verificado (no solo antes de cambios grandes) - mismo criterio que
  Terrasavr-Calamity-Beta, pedido explícito del usuario el 2-sep-2026 para el resto de
  proyectos también.
- `dotnet build`/`dotnet test` en verde antes de cada commit, siempre.
- Antes de dar por buena cualquier suposición sobre el formato `.plr`/`.tplr`/Calamity,
  investigar el código real (`script.js`/`overrides.js`/`calamity-nbt.js` en
  `Terrasavr-Calamity-Beta\resources\app\local-site\`, o los decompilados en
  `tModLoader-Decompiled\`) en vez de adivinar - ya costó tiempo real en el proyecto hermano
  hacerlo al revés.
- Verificar contra archivos/datos REALES de este PC siempre que sea posible (personajes reales
  en `Documents\My Games\Terraria\tModLoader\Players\`, JSON reales del proyecto Electron) en
  vez de solo fixtures inventadas - varios bugs reales de esta sesión solo aparecieron así.
