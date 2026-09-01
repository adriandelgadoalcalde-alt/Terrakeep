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

**Pendiente dentro de la Fase 1** (documentado también en `CalamityCharacterSync.cs`):
armadura/tinte de Calamity POR LOADOUT (`calamityMergeArmorDyeIntoPlayer`/
`calamitySyncArmorDyeFromPlayer` en la versión JS - incluye una peculiaridad real y confirmada,
no un bug de este port: `calamityActiveLoadout` usa `player.loadouts[currentLoadout]` SIN el
`+1` que sí usa el guardado vanilla nativo); coins/ammo/tempItems (la app JS tampoco los
sincroniza con Calamity, solo los protege al enmascarar).

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
limitación de siempre. Loadouts (armadura/tinte por loadout, `calamityMergeArmorDyeIntoPlayer`)
sigue sin portar - ver la sección de Fase 1 más arriba.

## Pendiente (visible desde fuera)

- **Exploración**: zoom real (de momento solo scroll a tamaño 1:1), fondo degradado por zona
  (falta leer GroundLevel/RockLevel), tooltip por tile al pasar el ratón (nombre real de
  tile/pared - ya está `TileNameCatalog`, falta guardar u/v por tile en `WldTile` para
  resolver variantes exactas y conectarlo a la UI).
- **Apariencia** (pelo/piel con preview) - ni empezada.
- Armadura/tinte de Calamity POR LOADOUT sin fusionar todavía (ver la sección de Fase 1 más
  arriba) - los buffs ya sí están fusionados (ver arriba).
- Instalador (Fase 6 del plan) - ni empezado.

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
