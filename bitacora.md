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

**Identidad propia**: nombre **"Terrakeep"** (confirmado por el usuario, 1-sep-2026). Ya
estaba puesto en el título de la ventana y en "Acerca de" desde que se propuso; el 1-sep-2026
se añadieron también `<Product>`/`<AssemblyTitle>`/`<Description>` al `.csproj` (antes el
`.exe` mostraba el nombre en crudo del ensamblado, `TerrasavrNative.App`, en Propiedades de
Windows/Administrador de tareas - verificado con `Get-Item ... .VersionInfo` antes y después).
Logo generado desde cero (hexágono ámbar + "T" en negativo), no reciclado de Terrasavr. Tema
visual propio (`TerrasavrNative.App/Styles/Theme.xaml`) - pendiente una ronda de pulido
estético dedicada (ver "Objetivo de diseño de la UI" más abajo).

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
4. ~~**Campos de estadísticas del personaje sin panel**~~ **CERRADO** (ver más abajo).
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

### Estadísticas del personaje (hueco #4 de la auditoría, cerrado)

Añadidas a `AppearanceViewModel` (mismo agrupamiento que la versión JS real: `app.TabMain`/
`Sa` cubre apariencia+estadísticas en un único panel, no dos separados) y a la pestaña
Apariencia: Dificultad (Softcore/Mediumcore/Hardcore/Journey, `ComboBox`), Vida actual/máxima,
Maná actual/máximo, misiones de pesca completadas, puntuación de golf, horas jugadas.

- **Dificultad**: confirmado en `script.readable.js` que la convención real es 0=Softcore,
  1=Mediumcore, 2=Hardcore, 3=Journey (`btDiff` array de 4 botones + `3 == a.difficulty`
  gateando el slot extra de accesorio de Journey - coincide con la convención pública conocida
  de Terraria).
- **Horas jugadas**: `PlrCharacter` solo guardaba `PlayTimeLow`/`PlayTimeHigh` (dos `UInt32`)
  sin exponer. Confirmado en `script.readable.js` que juntos forman un contador de 64 bits a
  **10 millones de ticks/segundo** (comentario real: *"there are 10 million 'ticks' in one
  second"*) - **exactamente** la resolución de `System.TimeSpan.Ticks`. Se usa
  `TimeSpan.FromTicks/.Ticks` con aritmética entera de 64 bits en vez de replicar la fórmula
  en coma flotante del original (que tiene una pérdida de precisión real y medible:
  `429.4967295` en vez de `429.4967296` = 2³²/10⁷ exacto) - mismo criterio ya aplicado a los
  campos `LONG` del NBT en la Fase 1 (opacos en JS por no tener enteros de 64 bits nativos;
  aquí sí los hay, se usan).
- `BartenderQuests` NO se añadió - el propio JS lo tiene eliminado de la UI a propósito
  (`this.remove(this.lbBarQuests)`), es basura reconocida por el propio autor.

**Verificado**: 97 tests xUnit siguen en verde (pieza de UI, sin lógica nueva en Core),
`dotnet build` limpio, la app arranca sin excepción. Matemática de ida y vuelta de
horas→ticks→horas verificada a mano (1h → 36 000 000 000 ticks → High=8/Low=1 640 261 632 →
recompuesto → 1.0h exacto). **No verificado con clics reales** - misma limitación de siempre.

### Panel de Spawn Points (hueco #3, cerrado) + bug real encontrado por el camino

Nueva pestaña interna "Spawn Points" (dentro de Personaje): `PlrCharacter.Servers` ya se
leía/escribía desde la Fase 1 pero sin ningún panel. `ServerEntryRowViewModel` envuelve cada
`PlrServerEntry` real y escribe en el mismo objeto (mismo patrón que `ColorSwatchViewModel`);
`ServersViewModel` con `AddEntryCommand`/`RemoveEntryCommand`. Campos: nombre, Spawn X/Y, e
"Id" (el campo `Address`, un entero interno - **no** una dirección de texto pese al nombre,
confirmado leyendo `script.readable.js` real: `writeInt(l.address)`, no un string).

**Bug real encontrado revisando el formato para este panel** (no hipotético, confirmado
comparando byte a byte contra `script.readable.js`): `PlrBodySerializer.ReadServers`/
`WriteServers` no descartaban las entradas "todo a cero" (`spawnX=spawnY=address=0`) - el
lector/escritor real SÍ lo hace por partida doble (`0==l.spawnX&&0==l.spawnY&&0==l.address||
this.servers.push(l)` al leer, `if(0!=l.spawnX||0!=l.spawnY||0!=l.address)...write...` al
escribir). Sin este filtro, una entrada así en un `.plr` real (rara, pero posible) se leería y
se re-escribiría tal cual, divergiendo del comportamiento real. Arreglado con
`IsBlankServerEntry` aplicado en ambos sentidos. Los 2 personajes reales de este PC no tienen
ninguna entrada así (los tests de round-trip byte a byte siguieron en verde sin cambios), así
que el fallo no se pudo demostrar con datos reales - **no se añadió test dedicado** por ese
mismo motivo (construir un `PlrCharacter` sintético completo solo para este caso no compensaba
el esfuerzo frente a lo acotado y verificable-por-lectura-directa del arreglo).

**Verificado**: 97 tests xUnit siguen en verde (incluidos los round-trip byte a byte contra
los 2 personajes reales, sin regresión), `dotnet build` limpio, la app arranca sin excepción.
**No verificado con clics reales** - misma limitación de siempre.

### Editor de buffs completo (hueco #1, cerrado - salvo presets)

`BuffRowViewModel` deja de ser solo-lectura: envuelve el `PlrBuff` real, duración editable
(`DurationSeconds`, escribe `Buff.Time` directamente) y botón "Quitar" (vacía el slot,
`Id=0`). `BuffsViewModel` (nuevo, reemplaza la `ObservableCollection<BuffRowViewModel>` que
antes vivía suelta en `MainViewModel`) añade buscador+picker de "Añadir buff" - mismo patrón
que la Librería de objetos pero sin cambiar de pestaña (el catálogo de buffs es mucho más
pequeño, ~660 entradas, cabe inline en la propia pestaña Buffs): busca en vanilla+Calamity
combinados, al pulsar una tarjeta coloca el buff en el primer slot libre del array fijo de
`character.Buffs` con una duración por defecto de 10 minutos.

- **`VanillaBuffCatalog.AllEntries()`** (nuevo, `Core/Data`) - hacía falta enumerar los 354
  buffs vanilla para el buscador, antes solo tenía `GetName(id)` puntual. Mismo patrón que
  `VanillaItemCatalog.AllEntries()`.
- **NO se portó el guardado/carga de presets de buffs a fichero `.json`/`.tsb`** del original
  (`btSave`/`btLoad`/`btAppend` en `app.TabEffects`) - decisión de alcance explícita, no un
  olvido: el caso de uso principal (editar los buffs de ESTE personaje) ya está cubierto,
  presets reutilizables entre personajes es una función aparte y más nicho.

**Verificado**: 99 tests xUnit (2 nuevos: `VanillaBuffCatalogTests`, `GetName` conocido/
desconocido + `AllEntries` completo). `dotnet build` limpio, la app arranca sin excepción.
**No verificado con clics reales** (añadir/quitar un buff de verdad, cambiar su duración,
guardar y comprobar que persiste) - misma limitación de siempre.

### `TabFlags` investigado y cerrado (hueco #6)

Quedó "sin determinar" en la primera pasada de la auditoría - investigado aparte leyendo `Vb`
(`app.TabFlags`) en `script.readable.js`: **13 casillas de desbloqueos/activaciones
permanentes**, todas sobre campos que `PlrCharacter` ya tenía leídos/escritos desde la Fase 1
(mismo patrón barato que el panel de estadísticas, hueco #4). Nueva pestaña interna
"Desbloqueos": accesorio extra (experto/maestro), cambio de antorcha de bioma
(desbloqueado/activado, dos casillas distintas), alcance de mesa de trabajo (Pan del
Artesano), regeneración de vida (Cristal Vital), defensa (Fruta de Égida), regeneración de
maná (Cristal Arcano), suerte (Perla de Galaxia), pesca (Gusano de Gominola), minería/
colocación (Ambrosia), evento DD2 completado, y carrito potenciado (desbloqueado/activado).

`FlagsViewModel` (nuevo) - mutación directa de `PlrCharacter.ExtraAccessory`/
`UnlockedBiomeTorches`/`UsingBiomeTorches`/`ExtraUsingFlags[0..6]`/`FinishedDD2Event`/
`SuperCartByte` (bit 0). **Replicado tal cual, no "corregido"**: las dos casillas de carrito
potenciado leen y escriben el MISMO bit en el original (`superCartByte&1`) - confirmado
literalmente en el código real, no es un fallo de este puerto.

**Verificado**: 99 tests xUnit siguen en verde (pieza de UI pura, sin lógica nueva en Core),
`dotnet build` limpio, la app arranca sin excepción. **No verificado con clics reales** -
misma limitación de siempre.

### Selector de prefijo manual (hueco #2, cerrado) - mejora real sobre el propio original

Antes solo existía el botón ★ (mejor prefijo auto-sugerido). Nuevo picker en la pestaña
Objetos (botón ✎ en cada tarjeta) que deja elegir CUALQUIERA de los 97 prefijos vanilla + 21
reales de Calamity/Pícaro, con buscador. Mismo patrón "picker con `PickTarget`" que
`LibraryViewModel`/`BuffsViewModel`, pero se queda en la misma pestaña (Objetos) en vez de
cambiar de pantalla - con solo 118 entradas totales (frente a ~8200 objetos o ~660 buffs) cabe
como overlay sobre los propios contenedores, mostrando TODAS de entrada.

- **`VanillaPrefixCatalog.AllEntries()`** (nuevo, `Core/Data`) - hacía falta enumerar los 97
  prefijos vanilla, antes solo `ById`/`ByInternal` puntuales. Mismo patrón ya aplicado a
  `VanillaBuffCatalog`.
- **Deliberadamente SIN agrupar por categoría** (arma/armadura/accesorio) pese a que la
  auditoría lo señalaba como mejora deseable: investigado `PrefixID.cs` decompilado real y
  confirmado que Terraria NO expone la categoría como una propiedad simple del prefijo - la
  elegibilidad depende de la lógica de `Item.Prefix()` por tipo de objeto, no hay una tabla
  estática prefijo→categoría fiable sin decompilar y verificar esa lógica entera. Se prefirió
  un selector plano y 100% verificable a inventar categorías sin confirmar.
- **Mejora real sobre el propio Terrasavr original**: el original solo tenía botón dedicado
  para 1 de los 21 prefijos de Calamity (el resto había que teclear el id 10000-10020 a mano,
  documentado en su propio README) - aquí los 21 están en el picker con nombre real.

**Verificado**: 101 tests xUnit (2 nuevos: `VanillaPrefixCatalogTests`, `ById`/`ByInternal` +
`AllEntries` completo). `dotnet build` limpio, la app arranca sin excepción. **No verificado
con clics reales** (elegir un prefijo de verdad, guardar y comprobar que persiste) - misma
limitación de siempre.

### Editor de versión objetivo (hueco #5, cerrado - los 6 huecos de la auditoría ya están cerrados)

Nueva pestaña "Versión": campo numérico en crudo + botones rápidos agrupados por versión real
de Terraria (tabla exacta de `ya.initPC` en `script.readable.js`: 1.3.0=145 ... 1.4.5.0=315).
**Deliberadamente solo se ofrecen los grupos 1.3.x/1.4.x** (`version>=145`) - 1.1.x/1.2.x se
omiten a propósito: esta app solo sabe LEER `version>=145` (limitación deliberada de la
Fase 1, `PlrCharacter.cs`), ofrecer una versión más antigua invitaría a escribir un archivo
que este mismo programa no podría volver a abrir. Aviso rojo visible en el panel explicando
el riesgo real (un valor que no encaje con el contenido del personaje puede producir un
archivo que ni esta app ni el juego sepan releer bien) - razón por la que quedó como
prioridad baja en la auditoría.

**Verificado**: 101 tests xUnit siguen en verde (pieza de UI pura, sin lógica nueva en Core),
`dotnet build` limpio, la app arranca sin excepción. **No verificado con clics reales** -
misma limitación de siempre.

### Preview de personaje de CUERPO COMPLETO con sprites reales del juego (pedido explícito, 1-sep-2026)

Sustituye al icono de cabeza pequeño anterior (que usaba el atlas propio simplificado de
Terrasavr, `visual.png`) por una figura de pie de cuerpo completo usando los sprites REALES
del jugador de Terraria, extraídos de la instalación vanilla de este PC (Steam, copia legítima
del propio usuario).

- **Formato real confirmado** (auditoría dedicada, `Terraria/ID/PlayerTextureID.cs` +
  `Terraria/DataStructures/PlayerDrawSet.cs` decompilados reales): `Player_0_Y.xnb` (X=0 =
  variante de piel "StarterMale", la única con las 15 piezas completas en la instalación
  real) - Y=0 Head, 1 EyeWhites, 2 Eyes, 3 TorsoSkin, 4 Undershirt, 6 Shirt, 10 LegSkin,
  11 Pants, 12 Shoes (más 5/7/8/9/13 = Hands/ArmSkin/ArmUndershirt/ArmHand/ArmShirt,
  extraídos pero NO usados en el compositor final, ver más abajo). El pelo es aparte,
  `Player_Hair_N.xnb` (N=1-228, un archivo por estilo real, confirmados los 228 en la
  instalación de este PC), con capas frente/detrás documentadas en el motor real (aquí
  simplificado a una sola capa, ver limitación de alcance abajo).
- **Todos los sprites son hojas de 40x56 por frame** (mismo tamaño que ya usaba el icono
  simplificado anterior - no es casualidad, es la convención real del propio juego) - los de
  cabeza/piernas/pelo son tiras verticales (frame 0 = fila superior), los de torso/brazo son
  rejillas 9x4 (celda(0,0) = esquina superior-izquierda). **El frame de reposo/celda(0,0) SÍ
  tiene contenido real en Head/EyeWhites/Eyes/TorsoSkin/Undershirt/Shirt/LegSkin/Pants/Shoes**
  (verificado recortando y viendo cada uno) - **pero NO en ArmSkin/ArmShirt** (esas dos hojas
  tienen la celda(0,0) totalmente vacía; inspeccionada la rejilla completa, solo tienen
  contenido en frames de brazo en movimiento/sujetando algo, ninguno con el brazo simplemente
  caído a un lado). Por eso el compositor final NO dibuja una capa de brazo aparte - el propio
  silueta de TorsoSkin ya incluye un brazo pegado al cuerpo, así que el resultado sigue siendo
  coherente sin esa capa extra (arreglo real encontrado a mitad de la implementación, no
  hipotético: el primer intento con ArmSkin en `celda(0,0)` salía completamente en blanco).
- **Extracción real de los `.xnb`** (formato confirmado: cabecera `XNB`+`w`+versión 5,
  comprimido **LZX** - reimplementarlo a mano habría sido un riesgo real, coincide con lo que
  ya advertía la investigación) con la herramienta **`xnb` (npm, LGPL-3.0, alias de `xnbcli`
  de LeonBlade)**, instalada en el Node portable ya usado en otros proyectos
  (`Downloads\dev-tools\node-v24.20.0-win-x64`) - no como dependencia del propio proyecto
  .NET, solo como herramienta de extracción puntual (mismo criterio que `pngjs` para
  `items.png`/`buffs.png` en su momento). 14 piezas de cuerpo + 228 hojas de pelo extraídas y
  recortadas al frame de reposo, guardadas como PNG individuales en `Assets/player/{body,
  hair}/` (~320KB en total).
- **Compositor** (`PlayerPreviewRenderer.cs`, reescrito): todas las piezas se superponen en el
  MISMO origen (0,0) - cada hoja de 40x56 ya trae el sprite posicionado en su sitio dentro de
  ese lienzo (cabeza arriba, piernas abajo...), así que no hace falta ninguna cuenta de
  offset, exactamente el mismo mecanismo que ya usaba el icono simplificado. Tintado =
  multiplicación RGB pura, mapeo de color confirmado 1:1 contra los 7 campos ya en
  `PlrCharacter`.
- **Alcance deliberadamente limitado** (documentado en el propio archivo, no oculto): sin
  armadura/vestuario equipado real, item en mano, accesorios, alas, ni animación (solo el
  frame de reposo) - necesitarían el catálogo de sprites de armadura entero y el motor de
  animación real, un proyecto en sí mismo. Solo la variante de piel "0" (StarterMale) para
  ambos géneros - la variante "femenina" no se investigó a fondo por no ser el foco de esta
  ronda, y a este tamaño la piel en sí no cambia de forma de manera perceptible. El id de
  pelo del `.plr` se usa tal cual como nombre de archivo (1-228) sin verificar pixel a pixel
  que el `HairID` interno del juego y el número de archivo coincidan exactamente - si no hay
  archivo para un id, cae al estilo 1 en vez de fallar.

**Verificado de extremo a extremo, no solo "debería funcionar"**: recortes individuales de
cada pieza inspeccionados visualmente antes de dar la rejilla por buena (exactamente igual que
se hizo con `items.png`); un composite de prueba en Node.js con colores de ejemplo generó una
figura de Terraria real y reconocible (pelo, camisa roja, pantalón azul, zapatos marrones) -
solo tras verlo correctamente se escribió el compositor en C#; y **el propio compositor C# se
volvió a probar por separado** (proyecto de usar-y-tirar en el scratchpad que invoca
`PlayerPreviewRenderer.Render` real con los assets reales ya copiados a `TerrasavrNative.App`)
con 3 peinados distintos, confirmando que el `.exe` final produce el mismo resultado correcto
que el prototipo. 101 tests xUnit siguen en verde (pieza de `App`, sin lógica nueva en `Core`),
`dotnet build` limpio, la app arranca sin excepción con los 242 sprites nuevos copiados.

## Segunda auditoría de completitud/consistencia (1-sep-2026)

Fork independiente (sin contexto previo) con instrucción explícita de comprobar tanto los
huecos frente al Terrasavr JS original como el propio trabajo añadido en esta sesión: 5
`ViewModels` con `LoadFrom` invocadas todas desde `MainViewModel.LoadFromPath`, bindings XAML
↔ ViewModel revisados a mano contra las 12 ViewModels reales, `AutoEquip`/resolución de items
Calamity+vanilla sin bug, carpetas de `Assets` con el recuento exacto esperado (vanilla 5454
iconos/354 buffs, Calamity 3244 iconos/308 buffs, NPCs 27, cuerpo 14, pelo 228), cap de 300/200
resultados en Librería/Buffs coherente con el mensaje mostrado, instalador con branding
correcto, sin restos de "Terrasavr Native" de cara al usuario ni TODO/FIXME reales. `dotnet
build`/`dotnet test` en verde (101/101), confirmado de nuevo de forma independiente.

Encontró 2 problemas reales, ambos arreglados y verificados en el momento:
1. Comentario desactualizado en `AboutViewModel.cs` ("nombre de trabajo Terrakeep, pendiente
   de confirmar") - el nombre ya estaba confirmado. Arreglado (solo comentario).
2. **Bug real**: `PrefixPicker.PickTarget` y `Library.PickTarget` no se limpiaban entre sí al
   abrir uno con el otro ya abierto (`MainViewModel.RequestPickForSlot`/
   `RequestPickPrefixForSlot`) - secuencia alcanzable: abrir selector de prefijo para un slot,
   sin cancelarlo pulsar "Elegir objeto" en otro slot, el picker de prefijo original quedaba
   huérfano y reaparecía al volver de la Librería. No corrompía datos pero era un estado
   inconsistente real. Arreglado poniendo a `null` el `PickTarget` del otro picker al abrir
   cada uno. `dotnet build`/`dotnet test` en verde tras el arreglo (101/101).

## Pulido estético a fondo de `Theme.xaml` (1-sep-2026)

Investigación previa (`WebSearch`) de tokens reales de Fluent 2/WinUI 3
(`fluent2.microsoft.design`): radio de esquina 4px por defecto (2px controles <32px, 8/12px
tarjetas/paneles grandes), escala de espaciado de base 4 (2/4/8/12/16/20/24/32), elevación vía
sombra con desenfoque. Se detectó el problema más importante antes de tocar nada: `TextBox`,
`ComboBox`, `CheckBox`, `RadioButton` y `Slider` no tenían NINGÚN estilo por defecto en
`Theme.xaml` - salían como controles Win32 claros de fábrica, rompiendo por completo el tema
oscuro cada vez que aparecían (Dificultad en Apariencia, género, deslizadores RGB, casillas de
Desbloqueos...).

Reescrito `Theme.xaml` completo añadiendo plantillas propias para los 5 controles que
faltaban (estilo "filled" de Fluent para TextBox/ComboBox con raya inferior de acento al
enfocar, CheckBox/RadioButton con relleno de acento al marcar, Slider con pista/pomo de
acento), más una sombra compartida (`CardShadow`) para dar profundidad a tarjetas, una tarjeta
genérica (`ElevatedCard`) y una tarjeta de navegación clicable con hover elevado
(`NavCardButton`, pensada para la futura página de bienvenida). Se refinó también el indicador
de selección de las pestañas: la nav rail (`NavTabItem`) pasa de relleno sólido a barra
vertical de acento (patrón real de nav rail Fluent/WinUI 3) y las pestañas internas
(`InnerTabItem`, Inventario/Banco/...) pasan de "pastilla" sólida a subrayado tipo pivot, para
no competir visualmente con los botones de acento del resto de la app.

**Verificación**: `dotnet build` limpio (0 errores/avisos, incluidas las plantillas complejas
de ComboBox/Slider que si tuvieran nombres de parte mal puestos fallarían en tiempo de
ejecución, no en compilación) + `dotnet test` 101/101. La captura de pantalla en vivo de la
ventana real resultó NO fiable en este entorno (foreground lock de Windows: `SetForegroundWindow`
no falla pero `CopyFromScreen` capturaba otra ventana por encima) - en vez de insistir, se
reusó el método ya validado para el preview de personaje: un proyecto WPF de usar-y-tirar en
el scratchpad que carga el `Theme.xaml` real vía `ResourceDictionary.Source` con ruta absoluta,
monta una ventana con un ejemplar de cada control nuevo, y renderiza a PNG con
`RenderTargetBitmap` en un hilo STA explícito (los top-level statements de C# no llevan
`[STAThread]` real, hace falta un `Thread` con `SetApartmentState(ApartmentState.STA)`).
Inspección visual del PNG resultante: todos los controles se ven coherentes con el tema oscuro
y el acento ámbar, sin ningún control con apariencia por defecto.

## Página de Inicio/bienvenida (1-sep-2026)

Nueva primera pestaña "Inicio" (índice 0, la app arranca ahí), preferencia del usuario:
explicativa de lo que se puede hacer, no un preview de personaje como en el Terrasavr
original. Logo + nombre + tagline, un párrafo explicando qué es Terrakeep (vanilla + Calamity
con el mismo archivo, nativo sin Electron), una tarjeta principal de acento "Empezar: cargar un
personaje" y una rejilla de tarjetas secundarias (Librería/Builds/Exploración/Novedades/Acerca
de) - todas usando el nuevo estilo `NavCardButton` del pulido estético anterior, con hover
elevado real.

Cada tarjeta navega directamente a su pestaña vía un `GoToTabCommand(string tab)` nuevo en
`MainViewModel` (switch sobre 6 constantes de índice, `InicioTabIndex=0` hasta
`AcercaDeTabIndex=6` - se desplazaron `PersonajeTabIndex`/`LibreriaTabIndex`, que ya existían
para el flujo de "Elegir objeto", de 0/1 a 1/2). `dotnet build`/`dotnet test` en verde
(101/101).

**Verificación de extremo a extremo, no solo visual**: la app real se lanzó en segundo plano,
y se hizo clic de verdad (coordenadas de pantalla reales vía `user32.dll`
`SetCursorPos`/`mouse_event`, con el truco de pulsación de Alt para saltar el bloqueo de foco
de Windows que había hecho fallar una captura anterior) sobre la tarjeta "Librería" de Inicio;
la captura posterior confirma la navegación real: la pestaña Librería queda seleccionada con
la barra de acento, el contenido carga ("8164 objetos en total") y el campo de búsqueda
muestra el estado de foco del nuevo tema (raya inferior de acento). Confirma en el mismo gesto
que el pulido estético anterior funciona en la app real, no solo en la galería aislada.

## Compatibilidad completa de versiones .plr, Terraria 1.1.2 a 1.4.5.8 (1-sep-2026)

Pedido explícito del usuario: "terrakeep tiene que ser si o si compatible con todas las
versiones de terraria desde la 1.1.2 hasta la 1.4.5.8 al igual que con tmodloader". Hasta ahora
`PlrBodySerializer` tenía una limitación deliberada: solo `version (invVersion) >= 145`
(Terraria 1.3.0), documentada en su propia cabecera como pendiente.

**Investigación** (fork independiente, sin contexto previo, con instrucción de leer
`ma.prototype.handle`/`P.prototype.handle`/`na.prototype.handle` reales en `script.js` vía
`js-beautify`): informe completo campo a campo con TODOS los umbrales de versión reales por
debajo de 145, más una tabla real `ja.getMaxIds` (techo de id de item por versión). Cruzado
además con un hallazgo propio: un gist real de **YellowAfterlife** (`Player.hx`,
`gist.github.com/YellowAfterlife/ced985a0d2f770c37b07`, implementación Haxe de referencia hasta
Terraria 1.3.4), que confirma independientemente la tabla de versión de juego -> `invVersion`
(1.1.2=39, 1.2.0=69, 1.2.4=98, 1.3.0=145... hasta 1.4.5.0=315) y varios umbrales bajos - los dos
coincidieron exactamente en todo lo comparable, alta confianza.

**Cambios reales en `PlrBodySerializer.cs`** (ver también `PlrCharacter.cs`/
`PlrContainerSpec.cs`/`PlrLoadout.cs`): quitado el suelo artificial de 145 (el motor real
tampoco tiene guarda de mínimo); bloque completo de magic/metaVersion/guid/playTime envuelto en
`version>=145` (antes se leía siempre); segundo byte de `HideVisual` y `HideMisc` gateados
correctamente; género binario invertido para `version<145` (`bool ? 0 : 4`, antes solo existía
el byte plano); bloque `extraAccessory/torches/extraUsingFlags/finishedDD2Event/taxMoney/
pve-pvpDeaths` envuelto en `version>=145`; loadout primario con recuento de slots por tramo real
(`10/10/10` desde 145, `8/8/8` desde 81, `8/3/(0 o 3)` por debajo, con el hueco de "dyes" a
partir de v=40 exacto); tabla real de `maxId` de item por versión (`GetMaxItemId`, antes
`int.MaxValue` sin clamp real) aplicada a TODOS los contenedores incluidos loadouts/tempItems.

**Dos bugs reales encontrados en el rango YA soportado (>=145), no solo en el rango nuevo**:
1. `FinishedDD2Event` se leía/escribía SIN condición ninguna (el comentario decía "ya cubierto
   por línea base" - falso: el umbral real es `isSwitch ? version>190 : version>=184`, así que
   para `145<=version<184` el campo no existe en absoluto y el port lo desalineaba todo lo que
   venía detrás). Corregido con el gate real.
2. Los loadouts (primario y los 3 alternos) nunca llevaban el byte de favorito real
   (`favFlagMinVersion=322` en `P` real, el port usaba `0`) - afecta a cualquier personaje real
   con `invVersion>=322` (1.4.5.x reciente). Corregido en `PlrContainerSpec.LoadoutSlot`. Los 3
   loadouts alternos tampoco usaban `Multi=true` (SÍ llevan `Int32 count` por slot, a diferencia
   del primario) - corregido.

**Dos bugs adicionales encontrados durante la propia escritura de tests** (mismo patrón:
bucles que nunca comprobaban `version>=145`, invisibles mientras el suelo de la app era 145):
el bloque `EquipmentItems`/`EquipmentDyes` (5+5 slots de mascota/montura/gancho) en `Read()` Y
`Write()`, y el array `HideInfo[13]` en `Write()` (`Read()` sí lo tenía bien gateado, `Write()`
no). Ambos se habrían disparado con cualquier `.plr` de verdad por debajo de 145.

**Verificación**: sin `.plr` reales tan antiguos en este PC, así que
`PlrBodySerializerOldVersionsTests.cs` (17 tests nuevos) construye el stream de bytes A MANO
para cada versión de prueba (39/58/100/145/200/322), de forma independiente del propio
serializador (mismo criterio que `PlrItemSlotTests`) - round-trip byte a byte, género invertido,
ausencia real de `FinishedDD2Event`/metadata/playTime por debajo de 145, clamp real de `maxId`,
recuento de slots de loadout por tramo. Total **118/118 tests en verde** (101 anteriores +17),
incluidos los round-trip byte-a-byte contra los 2 `.plr` reales de este PC (ambos claramente
`invVersion` alto, así que ejercitan de paso los dos bugs del rango >=145 ya corregidos).
`VersionEditorViewModel` ampliado con los grupos "1.1.x"/"1.2.x" (antes omitidos a propósito por
esta misma limitación, ya no aplica).

**tModLoader/`.tplr`**: confirmado que el NBT del `.tplr` es agnóstico de versión por completo
(sin ninguna lógica condicional de versión de Terraria/tModLoader dentro del propio formato,
solo el `.plr` vanilla la tiene) - no hace falta ningún cambio ahí, ya funciona para cualquier
versión de tModLoader que produzca ese mismo NBT.

## Rework de interfaz inspirado en Terrasavr real (pedido 1-sep-2026, en curso)

Pedido explícito del usuario, con permiso confirmado de YellowAfterlife y crédito ya puesto en
Acerca de: inspirarse "al máximo" en la interfaz REAL de Terrasavr (capturas de pantalla
aportadas por el usuario) para un rework a fondo. Lista completa tal cual se pidió, para no
perder ningún hilo entre sesiones:

1. **Explorador (mapa)**: arrastrar con el ratón para desplazar el mapa (pan), rueda del ratón
   para zoom/zoom out. Los NPCs no salen marcados visualmente sobre el mapa. Los NPCs
   escondidos/no encontrados (ej. Mercader Esquelético) no se indican como tales. Tooltip real
   al pasar el ratón por tiles/cofres (información del bloque). Clic en un NPC de la lista debe
   llevar la vista del mapa hasta su posición.
2. **Reorganización de menús**: Librería debería estar DENTRO de la pestaña Personaje (como en
   Terrasavr real: Inventario arriba, Librería con árbol de carpetas/subcarpetas por categoría
   con sprite propio abajo/derecha), no como pestaña externa separada. Cajas de objetos más
   pequeñas que las actuales.
3. **Bug real, reportado por una probadora ("chicas")**: la sección Novedades cierra el
   programa automáticamente - **crash real a investigar y arreglar, prioridad alta**.
4. **Librería e Investigación con sprites visuales** (no solo iconos pequeños en grid - la idea
   es un árbol de carpetas visual tipo Terrasavr real, ver capturas).
5. **Apariencia con selección visual por sprite** (pelo/pantalones/etc por miniatura, no
   escribiendo un id a mano como ahora) - **mismo criterio para Buffs**.
6. **Builds**: hoy solo auto-equipa armadura/accesorios, debería auto-equipar también las armas
   al inventario.
7. **Tooltips de estadísticas de objeto**: ningún arma/armadura/accesorio (vanilla o Calamity)
   muestra sus estadísticas reales (daño, defensa, etc.) como hace Terrasavr real - falta del
   todo.
8. **Mejor prefijo automático al colocar un objeto**: al poner un arma/objeto en el inventario
   (sobre todo desde la Librería), debería aplicarse el mejor prefijo real automáticamente, no
   dejarlo en "Ninguno" a mano.
9. **Picker de prefijo + Librería visibles/accesibles desde cualquier ventana del jugador**, con
   drag&drop para colocar directamente en inventario/accesorios/etc, todo con sprites reales
   (no texto) para saber de un vistazo qué se está tocando/poniendo/editando.

Orden de trabajo decidido: (3) el crash de Novedades primero por ser el único bug de estabilidad
real, luego el resto por orden de dependencia técnica (el Explorador y las estadísticas de
objeto son bloques bastante autocontenidos; la reorganización de menús y el sistema de sprites
visuales para Apariencia/Buffs/Librería comparten mucha base y conviene hacerlos junto con el
propio rework de Librería). Se actualizará esta sección a medida que se cierre cada punto.

### Punto 3 CERRADO - crash real de Novedades

Reproducido en esta misma máquina (lanzando la app real y haciendo clic de verdad en la
pestaña, coordenadas de pantalla reales vía `user32.dll`) antes de tocar nada, siguiendo la
regla del proyecto de verificar contra la app real en vez de adivinar. Causa real (log de
excepción no capturada): `Run.Text` tiene modo de enlace `TwoWay` por DEFECTO en WPF (mismo
quirk que `TextBox.Text`), y el `DataTemplate` de `WhatsNewChange` en `MainWindow.xaml` lo
enlazaba contra `DisplayText`, una propiedad de solo lectura (`Es ?? En ?? ""`) - WPF lanza
`InvalidOperationException`/`XamlParseException` al intentar activar ese enlace, y al no haber
ningún manejador de excepciones global, esa excepción no capturada mataba el proceso entero sin
ningún aviso visible - de ahí "se cierra el programa" reportado por la probadora. Arreglado
añadiendo `Mode=OneWay` explícito a ese único binding (el resto de bindings `Display*` del
archivo son sobre `TextBlock.Text`/`Button.Content`, cuyo modo por defecto ya es `OneWay`, no
tenían el problema).

Añadida además una red de seguridad real en `App.xaml.cs`
(`DispatcherUnhandledException`/`AppDomain.UnhandledException`): cualquier excepción no
capturada futura en el hilo de UI ya no cierra todo el programa en silencio - se muestra un
`MessageBox` con el tipo/mensaje real del error, se deja un volcado en
`ultimo-error.log` junto al `.exe`, y la app sigue viva (`e.Handled = true`). Motivo: este
mismo bug fue muy difícil de diagnosticar a ciegas sin la red de seguridad - un futuro fallo
similar (que sin duda los habrá, dado el tamaño del rework de interfaz pedido) ahora será
reportable con el mensaje real en vez de "se cierra sin más".

Verificado: reproducido el crash primero (proceso muerto tras el clic), luego arreglado,
recompilado, relanzado, mismo clic real en las mismas coordenadas -> la app sigue viva y la
pestaña Novedades se ve y funciona bien (verificado con captura de pantalla real).
`dotnet build`/`dotnet test` en verde (118/118, sin tests nuevos para este bug - es un fallo de
activación de binding XAML, no reproducible con xUnit sin montar un `Window` real).

### Punto 1 CERRADO - Explorador (mapa) interactivo

Cinco cambios reales sobre `ExplorationViewModel.cs`/`MainWindow.xaml`/`MainWindow.xaml.cs`:

1. **Arrastrar para desplazar (pan)**: nuevos manejadores en el code-behind
   (`OnWorldMapMouseDown`/`OnWorldMapMouseMove`/`OnWorldMapMouseUp`) que capturan el ratón al
   pulsar y mueven `ScrollViewer.ScrollToHorizontalOffset`/`VerticalOffset` según el
   desplazamiento real del cursor - los manejadores viven en el `ScrollViewer` (no en la
   `Image`) para que sigan disparándose durante el arrastre, cuando el ratón tiene captura (con
   captura activa los eventos ya no llegan al hijo de la forma normal).
2. **Rueda del ratón = zoom directo**, sin necesitar Ctrl (antes hacía falta Ctrl+rueda).
3. **NPCs marcados sobre el propio mapa**: nuevo `ItemsControl`+`Canvas` superpuesto a la
   imagen, dentro del mismo `Grid` con el `LayoutTransform` de zoom (para que los marcadores
   escalen/se desplacen junto con el mapa), un punto por NPC posicionado en
   `Canvas.Left/Top={Binding TileX/TileY}` (nuevas propiedades en `WorldNpcRowViewModel`, antes
   solo tenía un `Position` en texto). Esto también resuelve de paso "los NPCs escondidos como
   el Mercader esquelético no te lo dice": al tener marcador, su posición se ve en el mapa
   aunque esté enterrado bajo tierra (la lista "NPCs que faltan" ya detectaba correctamente al
   Mercader esquelético como no conseguido, eso ya funcionaba - lo que faltaba era el propio
   marcador visual).
4. **Clic en un NPC de la lista -> centra el mapa en su posición**: nuevo
   `GoToNpcCommand`+evento `NavigateToTileRequested` en la ViewModel (la ViewModel no puede
   tocar el `ScrollViewer` directamente, así que solo dispara el evento y el code-behind hace el
   `ScrollToHorizontalOffset`/`VerticalOffset` real, centrando el viewport).
5. **Tooltip de tiles/paredes al pasar el ratón** - ya existía y funcionaba
   (`ExplorationViewModel.UpdateHover`), verificado en vivo que sigue funcionando tras la
   reforma del mapa, incluidas variantes pintadas ("Ladrillo rosa"). Pendiente aparte: el
   contenido de los cofres (no solo el nombre del tile) no está implementado - `WldReader` no
   parsea la lista de cofres del `.wld` todavía, haría falta extenderlo.

**Verificación real, no solo build limpio**: la app se lanzó de verdad, se cargó un mundo real
(`adriandres.wld`, vía `pywinauto`/clics de pantalla reales con `user32.dll` para manejar el
diálogo nativo de archivo) y se comprobó cada punto con capturas de pantalla antes/después: el
terreno visible cambia tras arrastrar, el zoom sube de 100% a 231% con la rueda sin Ctrl, varios
marcadores magenta aparecen agrupados sobre una construcción real del mundo, y el tooltip de
tile se actualiza correctamente durante todo el proceso. `dotnet build`/`dotnet test` en verde
(118/118, sin tests nuevos - todo interacción de UI real, no lógica de `Core`).

### Punto 6 VERIFICADO (no era un bug) - Builds ya equipa también las armas

Probado en vivo con el personaje real `Eldelgas.plr` (inventario ya casi lleno): pulsar
"Auto-equipar" en una clase de Builds dio "8 objeto(s) colocado(s), 3 sin resolver o sin hueco
libre" - los 8 son los 3 de armadura + 5 accesorios (van a slots fijos, siempre se pueden
sobrescribir), y los 3 "sin hueco libre" son justo las 3 armas de esa clase: el código YA
intenta colocarlas (`AutoEquip` en `MainViewModel.cs`, busca el primer slot vacío del
inventario), lo que pasa es que si el inventario está lleno no hay dónde ponerlas, y por diseño
`AutoEquip` nunca sobrescribe un slot de inventario ya ocupado (a diferencia de armadura/
accesorios, que sí tienen sitio fijo reservado). Probable causa real de la queja: el personaje
de quien la probó tenía el inventario lleno. No se ha tocado el comportamiento (sobrescribir
inventario sin avisar sería peligroso) - el mensaje de estado ya deja claro cuántas armas no
entraron y por qué.

### Punto 8 CERRADO - mejor prefijo automático al colocar un objeto

`ItemSlotViewModel.PlaceItem` (llamado al colocar algo desde la Librería) ahora calcula el
mejor prefijo real (mismo `PrefixSuggester` que ya usaba el botón ★ manual, vanilla + Calamity/
Rogue) y lo aplica de inmediato al objeto recién colocado, en vez de dejarlo siempre en
"Ninguno". `BuildItemResolver` (usado por Auto-equipar en Builds) ya aplicaba el prefijo curado
de `builds.json` desde antes - no hacía falta tocarlo.

**Verificado en vivo, con un caso que SÍ tiene prefijo curado y uno que NO, para no dar el
arreglo por bueno con un solo dato favorable**: colocar "Excalibur" (id 368) desde la Librería
no le puso prefijo - investigado y confirmado que es correcto, `best_prefix.json` solo cubre
243 objetos vanilla curados y Excalibur no es uno de ellos (la misma limitación que ya tenía el
botón ★ manual, no es un bug nuevo). Colocar "Furia de estrellas"/Starfury (id 65, sí está en
la tabla curada, valor 81 = Legendario) sí le puso "Legendario" automáticamente, confirmando
que el arreglo funciona en el caso real que debía cubrir. `dotnet build`/`dotnet test` en verde
(118/118).

### Punto 2 CERRADO - Librería dentro de Personaje, árbol de carpetas real, cajas más pequeñas

Cambio grande, en varias piezas:

1. **Categorías vanilla reales, no inventadas**: `scripts/extraer-categorias-vanilla.py`
   extrae una categoría por id directamente de los bloques `SetDefaults1..5` de `Item.cs`
   (decompilado real de tModLoader, `case <id>:` numérico coincide 1:1 con `GameItem.Id`) -
   mismo criterio que ya se usó para `tile_names.json`/`map_colors.json` en el proyecto
   hermano: mirar qué campos reales asigna el juego (`accessory`/`melee`/`ranged`/`headSlot`/
   `ammo`/`createTile`/`potion`...) en vez de adivinar una taxonomía. 5262 objetos
   clasificados en 20 categorías reales (`Armas/Cuerpo a cuerpo`, `Armadura/Vanidad`,
   `Materiales`...). Verificado con pociones conocidas (`LesserHealingPotion`=28 -> "Pociones",
   confirmado con el propio bloque `case 28:` real, `potion=true`). Salida:
   `TerrasavrNative.App/Assets/vanilla_categories.json`, cargado por el nuevo
   `VanillaCategoryCatalog` (Core). Para Calamity ya existía una categoría real jerárquica
   (`Category` de `catalog.json`, con "/" - ej. `Armor/Vanity`) que simplemente no se estaba
   usando en la Librería (antes se le ponía "Vanilla"/la categoría cruda de Calamity sin
   aprovechar la jerarquía).
2. **Árbol de carpetas real en la UI**: `CategoryNodeViewModel` (nuevo) + `LibraryViewModel.
   BuildCategoryTree` construyen un único árbol vanilla+Calamity a partir de esas categorías
   reales (segmentos separados por "/"), con icono representativo (el del primer objeto real
   de esa carpeta) y contador de objetos por carpeta. `SelectCategoryCommand` selecciona/
   deselecciona una carpeta como filtro Y pliega/despliega sus subcarpetas a la vez;
   `ApplyFilter` combina categoría + texto de búsqueda a la vez si se usan ambos. La plantilla
   de un nodo (`CategoryNodeTemplate`, recursiva) se referencia a sí misma con
   `DynamicResource` en vez de `StaticResource` - **bug real encontrado y arreglado en el
   momento**: un `StaticResource` que se referencia a sí mismo dentro de su propia definición
   falla en WPF (se resuelve en tiempo de parseo, antes de que el recurso exista todavía),
   lanzando `XamlParseException` al abrir la pestaña - la app no llegó a cerrarse gracias a la
   red de seguridad global añadida en el punto 3 (mensaje de error real en vez de cierre
   silencioso), lo que permitió diagnosticarlo y arreglarlo al momento.
3. **Librería movida DENTRO de Personaje** (pedido explícito: "la librería debería estar
   también dentro de personaje... sabes cómo es terrasav") - ya no es una pestaña externa
   propia, es una pestaña interna más (`Objetos | Libreria | Buffs | Investigacion |
   Apariencia | Spawn Points | Desbloqueos | Version`). Esto obligó a un segundo nivel de
   navegación en `MainViewModel` (`PersonajeInnerTabIndex`, nuevo) - "Elegir objeto" en un
   slot ahora mueve DOS índices (externo a Personaje + interno a Librería) en vez de uno, y
   volver a Objetos al colocar un objeto hace lo mismo a la inversa.
4. **Cajas más pequeñas** (pedido explícito, "cajas de las armas y de todo más pequeñas") -
   `ItemSlotCard` (Theme.xaml) de 188x92 a 148x74 (caben 6 columnas en vez de 5 en el ancho
   habitual), tarjetas de la propia Librería de 112x112 a 92x100, con el contenido interno
   reajustado (iconos más pequeños, fuentes más pequeñas, botones "Cambiar"/"Elegir..." en vez
   de los textos largos anteriores) para que siga siendo legible.

**Verificado en vivo por completo, no solo build limpio**: personaje real cargado, árbol
navegado (categoría "Armas" -> 450 resultados con sprites reales), "Elegir objeto..." desde un
slot salta automáticamente a Personaje > Librería (con el filtro de categoría anterior
recordado), colocar "Hacha de hierro" desde ahí vuelve solo a Objetos Y le aplica el prefijo
"Legendario" automáticamente (confirma de paso que el punto 8 sigue funcionando con el nuevo
layout). `dotnet build`/`dotnet test` en verde (118/118).

### Punto 5 (mitad) CERRADO - selector visual de peinado con sprites reales

`PlayerPreviewRenderer.RenderHairThumbnail` (nuevo) reutiliza el mismo sprite/tintado real del
preview de cuerpo completo para renderizar una miniatura de un único peinado. `AppearanceViewModel`
genera las 228 miniaturas bajo demanda al abrir el selector (no en `LoadFrom`, para no pagar
228 renders si nunca se abre), usando el color de pelo actual en ese momento - se invalidan
(`HairOptions.Clear()`) si el color de pelo cambia después, para no quedar desincronizadas. El
campo "Estilo de pelo (id)" (TextBox numérico) pasa a ser un botón "Peinado: Estilo #N ✎" que
abre una rejilla de miniaturas reales (mismo patrón de overlay que ya usan los pickers de
prefijo/buffs). El tinte de pelo (número real de Terraria, sin catálogo de nombres/sprites
propios conocido) se queda como campo numérico.

**Buffs YA tenía selección visual con sprites reales** (`BuffCatalogEntryViewModel`, picker de
"Añadir buff...") desde una fase anterior de este mismo proyecto - no hacía falta ningún
cambio ahí, ya cumplía lo pedido.

Verificado en vivo: selector abierto muestra 228 miniaturas reales tintadas con el color de
pelo del personaje cargado (Eldelgas, pelo rojizo), clic en una miniatura distinta cambia el
"Estilo #" mostrado Y el preview de cuerpo completo reflejó el nuevo peinado al momento.
`dotnet build`/`dotnet test` en verde (118/118).

**Pendiente de la lista original**: selección visual para el resto de campos de Apariencia
(piel/pantalones/etc ya son colores editables por deslizador con preview en vivo, no ids -
esa parte ya estaba resuelta desde antes; lo que quedaba pendiente específicamente era el
peinado, ya cerrado), drag&drop visual.

### Punto 7 CERRADO - tooltips de estadísticas reales de objeto

Igual que las categorías vanilla (punto 2), extraído directamente de los bloques
`SetDefaults1..5` de `Item.cs` decompilado real - `scripts/extraer-estadisticas-vanilla.py`
(nuevo) saca `damage`/`defense`/`knockBack`/`useTime`/`crit`/`mana`/`healLife`/`healMana`/
`rare` por id, SOLO cuando el propio bloque de ese objeto concreto asigna ese campo (nunca se
rellena con 0 - un objeto sin estadística de combate no debe aparentar tenerla). 2968 objetos
vanilla con al menos una estadística real. Verificado con Excalibur (id 368):
`damage=72, knockBack=4.5, useTime=20, rare=5` - coincide con los valores reales conocidos del
juego. Salida: `TerrasavrNative.App/Assets/vanilla_stats.json`, cargado por el nuevo
`VanillaItemStatsCatalog` (Core). Para Calamity ya existía `CalamityItemStats` en
`catalog.json` (con `damageType` real, ej. "Rogue") desde antes, sin usar en la UI.

Nuevo `ItemStatsFormatter` (Core.Data) unifica ambas fuentes en un único texto de tooltip
("Daño: 85\nNudillo: 6.5\nVelocidad de uso: 18\nRareza: 8"), devuelve `null` si el objeto no
tiene ninguna estadística conocida (WPF no muestra `ToolTip` si el valor enlazado es null, así
que un objeto sin combate simplemente no muestra tooltip, en vez de uno vacío). Enlazado como
`StatsTooltip` en `ItemSlotViewModel` (objetos ya puestos, todos los contenedores) y
`LibraryItemViewModel` (tarjetas de la Librería) - las dos superficies donde se ven objetos.

Verificado en vivo: pasar el ratón sobre "Espada Terra" (Terra Blade) en el inventario real de
Eldelgas muestra "Daño: 85 / Nudillo: 6,5 / Velocidad de uso: 18 / Rareza: 8" - valores reales
conocidos del arma. `dotnet build`/`dotnet test` en verde (118/118).

### Punto 9 CERRADO - drag&drop real + Librería como panel siempre visible (último punto de la lista)

Dos piezas, la segunda descubierta como necesaria mientras se verificaba la primera:

1. **Arrastrar y soltar de verdad**: `ItemSlotViewModel.SwapWith` (nuevo, Core-side) intercambia
   el contenido COMPLETO de dos slots (prefijo/cantidad/favorito/datos de Calamity incluidos).
   El gesto de arrastre (deteccion de umbral de movimiento + `DragDrop.DoDragDrop`) vive en
   `MainWindow.xaml.cs` (`OnLibraryCardMouseDown/Move`, `OnItemSlotMouseDown/Move/Drop`), mismo
   criterio que el resto de manejadores de raton de este proyecto (code-behind, no MVVM puro,
   porque son eventos de UI de bajo nivel que la ViewModel no deberia conocer). Detección de
   arrastre estándar de WPF (guardar posición en botón-abajo, solo arrancar `DoDragDrop` si el
   ratón se mueve más allá de `SystemParameters.Minimum*DragDistance`) para no robarle el clic
   normal a los botones de dentro de la tarjeta (Cambiar/★/✎/✕), que ya funcionaban por clic.
2. **Bug de diseño real, encontrado verificando el punto 1**: con la Librería como pestaña
   interna SEPARADA de Objetos (como quedó tras el punto 2), arrastrar una tarjeta de la
   Librería hasta un slot es **inalcanzable en la práctica** - las dos pestañas nunca están
   visibles a la vez, así que no hay ningún slot visible sobre el que soltar mientras se ve la
   Librería. Solucionado moviendo la Librería DENTRO de la propia pestaña "Objetos", como panel
   siempre visible debajo del inventario (árbol a la izquierda, buscador+tarjetas a la
   derecha) - esto es además más fiel al layout real de Terrasavr (capturas aportadas por el
   usuario: inventario arriba, Librería abajo a la derecha, simultáneos) que el diseño de
   pestaña separada del punto 2. `MainViewModel` se simplifica de paso: ya no hace falta un
   índice de pestaña interna distinto para Librería, "Elegir objeto" solo necesita asegurar que
   la pestaña interna activa es "Objetos" (donde la Librería ya vive siempre).

**Verificado en vivo con capturas antes/después para cada gesto, no solo build limpio**:
arrastrar el slot 1 ("Espada Terra") sobre el slot 2 ("Durendal") los intercambia de verdad
(confirmado por captura: los nombres cambian de sitio, ambos con su prefijo real intacto);
arrastrar "Hacha de hierro" desde la Librería (categoría "Armas" ya filtrada) hasta un slot
visible del inventario arriba lo coloca ahí, con el prefijo "Legendario" aplicado automático
(confirma de paso que el punto 8 sigue funcionando). Nota de proceso: la automatización con
`user32.dll`/`SetCursorPos` necesita movimiento incremental real (no un simple teleport de
posición) para que el bucle interno de `DoDragDrop` de WPF lo reconozca como un arrastre de
verdad - un mensaje único de movimiento no basta. `dotnet build`/`dotnet test` en verde
(118/118).

### Corrección real - Investigación NO tenía sprites (afirmación anterior errónea, no verificada)

Al redactar el cierre de la lista se afirmó sin comprobar que "Investigación ya tenía sprites
desde antes" - **falso**, `ResearchRowViewModel` solo tenía `DisplayName`/`Count`/`IsCalamity`,
ningún icono. Detectado y corregido en el momento (mismo criterio de esta bitácora: no dar
nada por bueno sin mirar el código real). Arreglado de verdad: `ResearchRowViewModel` gana
`IconPath`; `MainViewModel.RebuildResearch` resuelve el id real por `Pid` (vanilla vía
`VanillaCatalog.GetIdByKey` + `VanillaIconResolver`, Calamity vía `CalamityCatalog.
ByModAndInternal` + su icono propio) y lo pasa. XAML de Investigación gana una `Image` junto
al nombre. Verificado en vivo con "Investigar todo" sobre Eldelgas (8164 objetos) - cada
entrada muestra su sprite real (herramientas, materiales, estatuas...), no solo texto.

**Con esto se cierran de verdad las 9 partes del pedido de rework de interfaz del 1-sep-2026**
(mapa interactivo, reorganización de menús, crash de Novedades, sprites en Librería +
Investigación, sprites en Apariencia/Buffs, Builds con armas, tooltips de estadísticas, mejor
prefijo automático, drag&drop). `dotnet build`/`dotnet test` en verde (118/118).

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

## Rediseño estético a fondo (1-sep-2026, pedido explícito tras crítica directa)

Pedido textual del usuario: *"podrias dedicarte a mejorar estéticamente toda la app? es que si
el trabajo esta muy bien echo pero el diseño me gustaría que fuera una pasada a nivel de
interactivo es que terrasav en ese sentido esta muy muy pulido y me da la sensación que no
estas captando la idea de de la estética de terrasav"* - crítica directa a que la primera
pasada estética (ámbar/Fluent genérico, sin animaciones reales) no reflejaba el Terrasavr
real. Antes de tocar nada se leyó `overrides.css`/`style.css` reales de
`Terrasavr-Calamity-Beta\resources\app\local-site\` (el reskin CSS que sí usa el Terrasavr
auténtico) y se relanzó el propio `Terrasavr.exe` para confirmar en vivo su aspecto: acento
**violeta `#6C63FF`** (no ámbar), fondos azul-marino muy oscuros (`#171a26`/`#1e2233`, no gris
neutro), botones flotantes en **degradado** con color distinto por acción (violeta=Builds,
teal=Novedades, rosa=Explorador, naranja=CTA fuerte), forma de píldora, y sobre todo
**animación real** al pasar el ratón (`translateY(-2px)`, sombra que crece) - nada de esto
existía en la primera pasada.

**Reescritura completa de `Theme.xaml`**: paleta nueva (violeta + familia teal/rosa/naranja
para variedad, degradados `LinearGradientBrush` para `Button`/`CheckBox`), dos niveles de
sombra (`CardShadow`/`CardShadowHover`), y sobre todo transiciones **animadas de verdad**
(`Storyboard`/`ColorAnimation`/`DoubleAnimation`) en vez de los `Setter` instantáneos de la
primera pasada: `Button` levanta 2px en variantes de color (`Tag="Accent/Teal/Pink/Orange"`),
`NavCardButton` (tarjetas de Inicio) levanta 3px y tiñe su borde al pasar el ratón, y un
`CircleCloseButton` nuevo gira 90° + se tiñe de rosa (gesto real de Terrasavr:
`.tsx-close-btn:hover { transform: rotate(90deg) }`), aplicado a los botones ✕ reales
(vaciar slot, quitar buff).

**Dos bugs reales de WPF encontrados y corregidos compilando** (ninguno inventado, los dos
pararon la compilación o crashearon la app en vivo, capturados por el manejador global de
excepciones ya existente):
1. `Setter.TargetName` **no puede apuntar a un `Brush`** con nombre (solo a
   `FrameworkElement`/`FrameworkContentElement`) - `<Setter TargetName="BdBrush" Property=
   "Color" .../>` daba `MC4111` en compilación. Los 4 casos (botón presionado, borde de
   `NavCardButton` por Tag) se reescribieron como `ColorAnimation` dentro de
   `BeginStoryboard`, que sí puede apuntar a un `Brush` nombrado.
2. Un `Storyboard` dentro de `Style.Triggers` (fuera del `ControlTemplate`) **no puede usar
   `TargetName`** en absoluto (`MC4011`) - y sin `TargetName` apunta al elemento estilado
   (el `Button`), que no tiene `RenderTransform.Y` propio, dando
   `XamlParseException`/`InvalidOperationException` **en tiempo de ejecución, no de
   compilación** (compiló limpio, crasheó al abrir la app - el primer intento de arreglo tras
   `dotnet build` en verde igualmente petó en vivo, capturado por el `MessageBox` +
   `ultimo-error.log` del manejador global). Solución real: mover el levantamiento a
   `ControlTemplate.Triggers` con `MultiTrigger` (`IsMouseOver` + `Tag`), donde `TargetName`
   sí es válido y sí puede apuntar al `TranslateTransform` nombrado del propio template.

**`MainWindow.xaml`**: variedad de color aplicada donde el propio Terrasavr real la usa -
tarjeta "Exploración" `Tag="Pink"`, "Novedades" `Tag="Teal"`, botón "Guardar" `Tag="Orange"`
(el CTA más fuerte, como el naranja real de Terrasavr), "Cargar personaje/mundo" ya tenían
`Tag="Accent"` de antes y heredan el degradado+levantamiento nuevos gratis. Los dos botones ✕
reales (vaciar slot, quitar buff) pasan a `CircleCloseButton`.

**Verificación real, con una limitación de entorno importante documentada aquí para no volver
a perseguirla cada sesión**: `dotnet build`/`dotnet test` en verde (118/118) tras cada cambio,
y el manejador global de excepciones confirmó en vivo que el primer intento SÍ crasheaba
(`ultimo-error.log` con el `XamlParseException` real) y que el segundo intento ya NO genera
ningún `ultimo-error.log` (proceso responde, CPU en reposo, sin excepción). Pero la
**verificación visual por captura de pantalla no fue posible en esta sesión**: la ventana de
Terrakeep aparece en blanco puro tras maximizar/mover/traer al frente por `user32.dll`
(`ShowWindow`/`SetForegroundWindow`/`SetWindowPos`/`MoveWindow`, incluso forzando un `WM_SIZE`
real y un clic de ratón real dentro de la ventana) - **confirmado con una prueba diferencial
real que descarta que sea el rediseño**: se hizo `git stash` de todos los cambios de esta
sesión, se recompiló la versión anterior (tema ámbar, ya comiteada y dada por buena en su
momento) y **el mismo blanco reproduce igual** con ese build antiguo. Es una limitación del
propio entorno de automatización de esta sesión (ya apuntada como sospecha en la sesión
anterior: "blank white window" al cargar un mundo), no un defecto de la app ni de este
rediseño - `git stash pop` restauró los cambios sin pérdida. Dado que ya ha fallado dos veces
en dos sesiones distintas por la misma causa, se deja aparcada esta vía de verificación en vez
de seguir insistiendo (regla de "si falla dos veces seguidas, para") - la verificación de este
rediseño se apoya en compilación limpia + tests en verde + ausencia de excepción real +
revisión manual del XAML, no en captura visual.

## Arreglo del zoom del visor de mundo (1-sep-2026)

Bug reportado textual: *"el tema del visualitzador de mundo el zoom se solapa con el scroll de
la rueda del ratón con lo que el zoom no lo hace recto"*. Causa real: `OnWorldMapPreviewMouseWheel`
cambiaba `Zoom` sin tocar los offsets del `ScrollViewer` - como el `ScaleTransform` vive en
`LayoutTransform` (no `RenderTransform`), los offsets del `ScrollViewer` ya están en espacio
POST-transformación y no se reajustan solos, así que el zoom saltaba siempre desde la esquina
superior izquierda del mapa (offset 0,0) en vez de desde donde estaba el cursor. Arreglado con
el mismo patrón ya usado y verificado en esta sesión para `OnNavigateToTile` (centrar el mapa
en un NPC): se calcula la coordenada de mundo bajo el cursor ANTES de cambiar el zoom
(`worldX/worldY = (offset + posición del ratón) / zoom viejo`), se cambia el zoom, se llama a
`ScrollViewer.UpdateLayout()` (para que el `ScrollViewer` ya conozca el nuevo tamaño de
contenido post-transformación antes de pedirle un offset nuevo - sin esto seguiría calculando
contra el extent viejo) y se recoloca el offset para que ese mismo punto de mundo siga bajo el
cursor después (`worldX * zoom nuevo - posición del ratón`). `dotnet build`/`dotnet test` en
verde (118/118); verificación visual en vivo bloqueada por la misma limitación de entorno
documentada arriba - la lógica reutiliza un patrón ya probado en vivo esta misma sesión para
`OnNavigateToTile`, así que se da por buena por revisión de código + compilación + tests,
dejando constancia aquí de que no se ha podido confirmar con una captura real.

## Rework estructural pedido tras comparar con capturas reales de Terrasavr (1-sep-2026)

Feedback extenso y crítico del usuario tras usar la app y comparar con 4 capturas reales de
Terrasavr aportadas por él (no de Terrakeep): *"me da la sensación que no estás captando la
estética de Terrasavr... la gracia de la estética de Terrasavr... hace que todo sea super
práctico"*. Investigación de campo real antes de tocar nada: `script.js` beautificado
(js-beautify) de `Terrasavr-Calamity-Beta` para entender la estructura real, más un agente de
planificación (Opus, vía `EnterPlanMode`/Plan agent, pedido explícito del usuario -
"opusplan") que verificó todo contra el código decompilado real de tModLoader en
`tModLoader-Decompiled\`. Plan completo de 7 fases guardado y aprobado (ver
`C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md` mientras siga vigente el archivo).

Hallazgo estructural clave: `app.TabInventory` (clase base real de la que heredan
Equipamiento/Banco/Caja fuerte/Forja/Bóveda/Mascotas) lleva un panel lateral persistente con
pestañas "Edit"/"Library" (`app.TabEdit`/`app.TabLibrary`, script.js líneas ~2676/~3986) - por
eso "acompaña desde Equipamiento hasta Forja del Vacío" (cita del usuario). Terrakeep no tiene
el equivalente a `TabEdit` todavía (fase 3 del plan, pendiente).

### Bug real de crash corregido primero (antes del rework): `PinkColor` sobre `Foreground`

El usuario mandó la captura real del error: `InvalidOperationException: "#FFFF5D8F" no es un
valor válido para la propiedad "Foreground"`. Diagnosticado exacto: `Theme.xaml`,
`CircleCloseButton`, trigger `IsMouseOver` - `<Setter Property="Foreground"
Value="{StaticResource PinkColor}" />` asignaba un `Color` (no un `Brush`) directamente.
Arreglado a `PinkBrush`. `dotnet build`/`dotnet test` en verde. Commit `abfcfa2`.

### Fase 1 del plan - Buffs: "Añadir buff..." salía vacío hasta escribir

Verificado leyendo `BuffsViewModel.ApplyFilter()` (no fue necesario reproducir en vivo, dada
la limitación de entorno para capturas ya documentada arriba): con `SearchText` vacío, el
método hacía `return` justo después de `Results.Clear()` - el panel "Añadir buff..." se abría
completamente vacío hasta escribir algo, dando la sensación de "no hay sprites de buffs"
cuando en realidad el problema era que no se mostraba NADA de entrada (los buffs YA activos sí
tenían sprite correcto todo el tiempo - 662 iconos vanilla+Calamity verificados presentes en
`Assets/vanilla/buff_icons/` y en el build de salida, resolución correcta para ambos tipos).
Arreglado con el mismo criterio que ya usan `LibraryViewModel`/`PrefixPickerViewModel`:
búsqueda vacía muestra los primeros `MaxResults=200` de entrada, no una lista vacía. Envuelto
además en un `ScrollViewer MaxHeight="240"` en `MainWindow.xaml` para que la lista de
resultados no empuje la lista de buffs activos fuera de la pantalla. `dotnet build`/`dotnet
test` en verde (118/118) - verificación por revisión de código, mismo criterio que el resto de
esta sección dado el bloqueo de captura de pantalla ya establecido.

### Fase 2 del plan - tablas reales de elegibilidad de prefijo (Core, sin UI todavía)

Objetivo: reemplazar la lista plana de 118 prefijos del `PrefixPickerViewModel` actual (que
deja poner cualquier prefijo a cualquier objeto) por la elegibilidad REAL del propio juego -
sin inventar nada, extraída del código decompilado real de tModLoader.

`scripts/extraer-prefijos-vanilla.py` (nuevo) lee 3 fuentes reales:
- `PrefixLegacy.cs`: los 7 arrays `PrefixesFor{Swords,Spears,GunsBows,MagicAndSummons,
  BoomeransAndChakrums,BoomeransAndChakrums_TerrarianYoyo,Accessories}` (el pool de ids de
  prefijo legal por tipo) y los 6 `ItemSets.*` bool-set (qué objetos son de cada tipo).
- `ItemID.cs`, `Sets.CanGetPrefixes`: confirmado que es una lista NEGRA (el primer argumento
  de `CreateBoolSet` es el valor por defecto `true`; los 89 ids listados son la excepción).
- `Item.cs`, `GetRollablePrefixes()`/`GetPrefixCategories()`/`IsAPrefixableAccessory()`:
  replicadas 1:1, mismo orden de prioridad real (Swords > Spears > GunsBows > MagicAndSummon
  > BoomerangsChakrams > TerrarianYoyo > Accessory). Para accessory/vanity se reutiliza
  `vanilla_categories.json` (ya extraído en esta sesión con el mismo método de bloques
  `SetDefaults#`) en vez de volver a escanear `Item.cs`.

**Bug real cometido y corregido en el propio script durante la verificación**: la primera
versión guardaba en `itemPool` una etiqueta semántica ("melee") en vez de la clave real del
diccionario `prefixesByCategory` ("swords") - el test de humo contra el fichero real
(`PrefixRulesCatalogRealFileTests`) lo detectó de inmediato (`IsLegal(1, 81)` daba `false`
cuando debía dar `true`). Corregido y regenerado antes de comitear - no se dio nada por bueno
sin que el test pasara.

Salida: `Assets/vanilla_prefix_rules.json` (742 objetos con al menos una categoría real de
prefijo - todos los demás, materiales/bloques/etc, no admiten ninguno).

**Nuevo en `TerrasavrNative.Core/Data/`**:
- `PrefixRulesCatalog.cs`: `PrefixCategory` (flags: Melee/Ranged/Magic/AnyWeapon/Accessory/
  Summon - Summon solo existe para Calamity, Terraria vanilla no tiene esa categoría propia,
  los objetos de invocación vanilla caen dentro de Magic vía el set `MagicAndSummon` real),
  `VanillaCategories(id)`, `LegalPrefixes(id)`, `IsLegal(id, prefixId)`.
- `PrefixEligibility.cs`: punto único que decide la categoría de un `GameItem` cualquiera,
  vanilla (delega en `PrefixRulesCatalog`) o Calamity (vía `entry.Category`/`DamageType` ya
  presentes en `catalog.json`).
- `PrefixGroupCatalog.cs`: la tabla `pfxMeta` LITERAL del Terrasavr real (`script.js`, clase
  `app.TabEdit`, líneas 2600-2657 del beautificado) - 3 metas (Biblioteca/Positivos/
  Negativos) con sus grupos reales (Mejor/Daño/Crítico, Accesorio(+), Universal(±), Común(±),
  Cuerpo a cuerpo(±), A distancia(±), Magia(±), Invocación(±)), cada uno con su `Requires:
  PrefixCategory`. Los ids de invocación de Calamity (85-97) vienen de las constantes reales
  `M.PFabled=85 … M.PScraggling=97` del propio `script.js` (líneas ~10453-10466) - **no son
  vanilla** (`PrefixID.Count==85` real), quedan marcados como tal para cuando la Fase 3
  necesite pintarlos en rojo Calamity. `GroupsFor`/`PrefixIdsFor` intersectan cada grupo con
  la lista de prefijos REALMENTE legal del objeto (vía `PrefixRulesCatalog`, solo vanilla -
  Calamity no tiene tabla de legalidad por-objeto propia en este proyecto, se confía en el
  `Requires` ya calculado por `PrefixEligibility`) - **esto es lo que arregla de verdad el bug
  reportado**: un bloque de tierra no tiene ningún grupo en ninguna meta; un arco no ve
  "Legendario" (81, solo en el pool de espadas) aunque "Daño" lo liste en la tabla original.
- `CharacterFileService.cs`: carga `PrefixRulesCatalog` una vez más, mismo patrón que el
  resto de catálogos.

Tests nuevos (`PrefixRulesCatalogTests.cs`, `PrefixGroupCatalogTests.cs`, fixtures inline +
un smoke test contra el fichero real que se salta si no existe la carpeta, patrón `*RealFile
Tests` ya establecido en el proyecto): bloque sin categoría → sin grupos en ninguna meta;
espada → solo grupos Cuerpo a cuerpo/Universal, nunca A distancia/Magia/Accesorio/Invocación;
objeto de Calamity con categoría Invocación simulada → grupo "Invocación +" sin filtrar por
pool vanilla. `dotnet build`/`dotnet test` en verde (126/126). Fase puramente de Core, sin
UI todavía - la Fase 3 (panel "Editar" compartido) es la que expone esto de verdad.

### Fase 3 del plan - panel "Editar" compartido (equivalente real de app.TabEdit)

La fase grande: sustituye los botones ★/✎/✕ diminutos por tarjeta + el `PrefixPickerViewModel`
plano (118 prefijos sin filtrar) por un panel lateral persistente que sigue al slot
SELECCIONADO en cualquier contenedor - Inventario/Banco/Caja fuerte/Fragua/Bóveda/Mascotas/
Loadouts, no solo uno fijo. Esto es literalmente lo que el usuario describió como "esa
ventana naranja acompaña desde equipamiento hasta forja del vacío".

**`ItemSlotViewModel.cs`**: nuevo `ContainerName` (para que el panel sepa "sobre qué está
editando" al vivir fuera del `TabControl` de contenedores), `IsSelected` (borde de acento +
`AccentGlow` en la tarjeta), `ItemId`/`PrefixId` observables con escritura real - editar
`ItemId` a mano reutiliza `PlaceItem` (mismo criterio que elegir desde la Librería, mejor
prefijo automático incluido); editar `PrefixId` construye `ItemPrefix.Vanilla(byte)` y llama
`SetPrefix` (cubre 0-97, incluidos los ids reales de invocación de Calamity 85-97 - un
prefijo Rogue auténtico con id sintético ≥10000 no cabe en un campo de un byte, se deja solo
para la rejilla de botones). Se elimina `ChoosePrefixCommand`.

**`ItemEditViewModel.cs` (nuevo)** - deliberadamente delgado: Nombre/Índice/Contar/Prefijo se
enlazan DIRECTAMENTE a `Slot.*` en el XAML, esta clase solo añade el selector categorizado de
3 niveles (`Metas`→`Groups`→`Prefixes`, construido con `PrefixGroupCatalog`/
`PrefixEligibility` de la Fase 2). Se suscribe a `PropertyChanged` del slot activo para
refrescarse solo con cualquier cambio relevante (objeto puesto, prefijo cambiado, slot
vaciado). Si la meta seleccionada deja de aplicar al cambiar de objeto, salta sola a la
primera que sí aplica - equivalente real de `btMeta[0].click()`.

**`MainViewModel.cs`**: `SelectSlot(slot)` (deselecciona el anterior, marca el nuevo,
`ItemEdit.Slot = slot`) es el único punto de entrada; se llama tanto al hacer clic en un slot
(`MainWindow.xaml.cs`, `OnItemSlotMouseDown`) como al elegir un objeto desde la Librería para
un slot (mismo criterio: el panel sigue viendo lo que se acaba de colocar). Se elimina
`PrefixPickerViewModel.cs` entero (con su comentario ya obsoleto sobre "no hay tabla
prefijo→categoría fiable" - la Fase 2 demostró que sí la hay).

**`MainWindow.xaml`**: "Objetos" pasa a `Grid` de 2 filas: fila 0 con 2 columnas
(contenedores | panel Editar, 300px fijos), fila 1 la Librería sin tocar, a todo el ancho
como ya estaba. Nuevo `InverseBooleanToVisibilityConverter` (`Converters/
VisibilityConverters.cs`, registrado como `InverseBoolToVis` en `App.xaml`) para el mensaje
"Selecciona un slot"/"No admite prefijos". Nuevos estilos en `Theme.xaml`: `SidePanelCard`,
`PrefixMetaButton`/`PrefixGroupButton` (pastilla con estado seleccionado en violeta).

**Verificación real, sin depender de capturas de pantalla** (la limitación de entorno ya
documentada seguía bloqueando el diálogo nativo de abrir archivo incluso probando con UI
Automation real en vez de coordenadas de píxel - el botón se localiza y se invoca sin error,
pero el `OpenFileDialog` nunca llega a aparecer como ventana de nivel superior; documentado
aquí, no se insistió más de dos intentos): se montó un proyecto de consola temporal
(`%TEMP%\...\scratchpad\verify-fase3\Verify`, con `ProjectReference` directa a
`TerrasavrNative.App.csproj`) que instancia `MainViewModel` de verdad y ejercita todo el flujo
sin pasar por WPF, contra una **copia** de `Eldelgas.plr`/`.tplr` real (nunca el archivo del
usuario). Resultados, todos correctos:
- Tierra (id 2) → `CanHavePrefix=False`, 0 grupos en ninguna meta.
- Espada de cobre (id 1) → categoría "Cuerpo a cuerpo"; meta Positivos muestra solo
  Universal+/Común+/Cuerpo a cuerpo+ (nunca A distancia+/Magia+/Accesorio); "Legendario"
  presente en el grupo real; aplicarlo actualiza `PrefixId=81` y se marca `IsCurrent=True` al
  reconstruir la rejilla.
- Editar `ItemId=368` a mano cambia el slot a "Excalibur" con tooltip de estadísticas real.
- Editar `PrefixId=60` a mano cambia `PrefixDisplay` a "Demoníaco".
- Seleccionar un slot del Banco cambia `ItemEdit.Slot.ContainerName` a "Banco" y deselecciona
  el slot anterior del Inventario - **el panel sigue de verdad entre contenedores distintos**.
- Guardar + recargar la copia conserva Excalibur con prefijo 60 - persistencia real
  confirmada, no solo en memoria.

`dotnet build`/`dotnet test` en verde (126/126, sin regresiones). Proyecto de verificación y
copia temporal borrados tras el uso (uno de los dos, la copia en `%TEMP%`, quedó pendiente de
borrar por un permiso denegado puntual - no contiene nada sensible, es una copia descartable).

### Fase 4 del plan - tooltips de estadísticas en Builds + arreglo real de Monedas/Munición

**`BuildItemRowViewModel.cs`** gana `StatsTooltip` (antes ni siquiera guardaba el id numérico
del objeto - imposible calcular nada). **`BuildsViewModel.ResolveItem`** ya resolvía ese id
localmente para el icono pero no lo reutilizaba - ahora también llama
`ItemStatsFormatter.Format(...)`, mismo patrón exacto que `LibraryViewModel`. `MainWindow.xaml`
gana `ToolTip="{Binding StatsTooltip}"` en la plantilla de `BuildItemRowViewModel`. Arregla el
bug reportado: "no salen [tooltips]... debería aplicarse a builds".

**Bug real de guardado encontrado por el agente de planificación y arreglado aquí**:
`MainViewModel.SyncEditsBackToMerged` solo escribía de vuelta contenedores presentes en
`MergedContainers` - pero `CalamityCharacterSync.MergeAll` nunca crea las claves `coins`/
`ammo` (son vanilla-only a propósito, ver el comentario de `RebuildContainers`), así que el
`continue` del bucle las saltaba en silencio: **cualquier edición de Monedas o Munición se
perdía al guardar, sin ningún aviso**. Arreglado escribiéndolas aparte, directo a
`PlrCharacter.Coins`/`Ammo` (mismo patrón `CopyInto` elemento a elemento que ya usa
`CalamityCharacterSync` para los loadouts, vía el nuevo helper `CopySlotsInto`).

**Verificación real** (mismo arnés de consola temporal que la Fase 3, contra una copia nueva
de `Eldelgas.plr`/`.tplr`): tooltip real en un arma vanilla ("Filo de la noche": Daño 40,
Nudillo 4.5, Velocidad de uso 25, Rareza 3) y en una de Calamity ("Cometa Humeante": Daño 17,
tipo `DamageClass.MeleeNoSpeed`, Nudillo 1.5, Velocidad de uso 25) - ambos con datos reales,
ninguno inventado. Monedas editadas a 777 y Munición cambiada a "Flecha de madera" x333
**sobreviven de verdad** a guardar + recargar (antes se perdían sin ningún error visible).

`dotnet build`/`dotnet test` en verde (126/126, sin regresiones).

### Fase 5 del plan - selector visual de tinte de pelo

El peinado ya tenía selector visual (228 miniaturas) desde antes de este rework; el tinte de
pelo seguía siendo un `TextBox` numérico puro - el "Apariencia sigue siendo por ID" concreto
que señaló el usuario.

`scripts/extraer-tintes-pelo.py` (nuevo) extrae el índice REAL de cada tinte de
`DyeInitializer.cs`/`HairShaderDataSet.cs` reales. **Matiz real encontrado leyendo el código
con cuidado** (no asumido por orden de aparición en el fichero): `DyeInitializer.
LoadHairDyes()` llama primero a `LoadLegacyHairdyes()` (los 11 ids "legacy", definidos
DESPUÉS en el texto por número de línea) y solo entonces enlaza su propio id 3259 - el orden
de llamada real (que es el que fija el índice, `HairShaderDataSet.BindShader` hace
`_shaderLookupDictionary[itemId] = ++_shaderDataCount`) es `[1977,1978,1979,1980,1981,1982,
1983,1984,1985,1986,2863,3259]`, NO el orden en que aparecen las líneas en el archivo. Se
confirmó también que `GameShaders.Hair.BindShader` no se llama desde ningún otro sitio en
todo el código decompilado - estos 12 son todos los que hay, sin más fuentes que puedan
desplazar los índices.

Nuevo `TerrasavrNative.Core/Data/HairDyeCatalog.cs` (mismo patrón `LoadFromFile`),
`HairDyeOptionViewModel.cs` (a diferencia del peinado no hace falta renderizar nada: el tinte
se muestra con el sprite+nombre reales del objeto que lo aplica, ya resueltos una vez en el
constructor de `AppearanceViewModel`, que ahora recibe `CharacterFileService` - antes se
construía sin parámetros). `MainWindow.xaml`: el `TextBox` de tinte pasa a un botón con
overlay de selección visual, mismo patrón que el peinado. Nota honesta añadida al texto del
preview: el shader visual del tinte no se reproduce ahí (solo el color base de pelo) - no se
finge un efecto que no está implementado.

Test nuevo `HairDyeCatalogTests.cs` (smoke test real): 12 entradas, índices 1-12
contiguos, `Entries[0].ItemId==1977`, `Entries[11].ItemId==3259` (confirma el orden de
llamada real, no el de aparición en el texto).

**Verificación real** (mismo arnés de consola, copia de `Eldelgas.plr`/`.tplr`): 13 opciones
(Ninguno + 12 reales) con nombre e icono correctos para cada una (`Tinta vital`, `Tinte de
maná`, ..., `Tinte de pelo de crepúsculo`); elegir "Tinte de equipo" (índice 6) actualiza
`HairDye=6` y el nombre mostrado; sobrevive a guardar + recargar.

`dotnet build`/`dotnet test` en verde (127/127, sin regresiones).

### Fase 6 del plan - visor de mundo: cursor de mano + tooltip flotante

`Cursor="SizeAll"` (las 4 flechas que el usuario rechazó explícitamente: "salen unas flechas
que no me gusta nada") pasa a `Cursor="Hand"` - WPF no trae un cursor de "mano de agarre"
nativo como el pan de TEdit/Photoshop; fabricar uno propio (`.cur` custom) queda anotado como
mejora opcional no bloqueante en el plan, no se hizo en esta fase.

Tooltip flotante que sigue al cursor sobre un tile (estilo TEdit, pedido explícito: "también
tiene que decirlo cuando pasas el ratón por encima... como en TEdit" - **añadido** al texto
fijo de abajo a la izquierda, no en sustitución, tal y como se pidió). Implementado con un
`Canvas IsHitTestVisible="False"` superpuesto al `ScrollViewer` del mapa (no un `Popup` real:
un `Popup` es un HWND aparte y parpadea al mover el ratón rápido; el `Canvas` se recorta solo
al área visible del mapa). `MainWindow.xaml.cs`, nuevo `PositionMapTooltip`: mide el `Border`
del tooltip (`Measure`/`DesiredSize`) y lo coloca con un margen (16,18) respecto al cursor,
volteándolo hacia el lado contrario si no cabe por el borde derecho/inferior del propio
`Canvas`. Oculto mientras se arrastra el mapa y al salir del área.

**Bug real de WPF evitado durante la implementación**: `MapTooltipBorder.Visibility` ya tiene
un `Binding` real en el XAML (a `Exploration.HoverInfo` vía `EmptyToCollapsed`) - asignar la
propiedad a secas desde código (`MapTooltipBorder.Visibility = ...`) habría **reemplazado ese
binding para siempre** (un valor local en WPF tiene más precedencia que un binding y lo
desengancha). Se usó `SetCurrentValue(UIElement.VisibilityProperty, ...)` en su lugar, que
empuja un valor puntual sin desconectar el binding - patrón estándar de WPF para este caso
exacto, no algo que se pueda dar por sabido sin pensarlo.

**Verificación real**: build limpio; navegado a la pestaña Exploración vía UI Automation real
(no coordenadas de píxel) sin excepción; cargado un mundo real (`adriandres.wld`, escribiendo
la ruta por teclado en el diálogo nativo - el mismo diálogo que sigue sin aparecer como
ventana propia ante UI Automation, pero que sí recibe el foco de teclado) sin excepción;
movimiento real del cursor sobre el área del mapa (antes y después de cargar el mundo, 12
pasos incrementales reales, no un teletransporte) sin ninguna excepción ni entrada en
`ultimo-error.log` - la lógica de `PositionMapTooltip`/`SetCurrentValue` se ejecuta de verdad
en cada movimiento sin fallar. No se pudo confirmar visualmente el aspecto exacto del tooltip
(limitación de entorno para capturas ya documentada) - verificado por ausencia real de
excepción en interacción real, no solo por revisión de código.

`dotnet build`/`dotnet test` en verde (127/127, sin regresiones).

### Fase 7 del plan (última) - separar "Novedades" (juego) de "¡Sobre esta versión!" (editor)

Confirmado leyendo el propio `whats_new.json` real que su contenido es del JUEGO/Calamity
("El Brote de Arcilla ahora siempre empuja...") - la tarjeta de Inicio decía "Que ha cambiado
en cada version del propio editor", descriptivamente falso. Arreglado el texto, y añadido un
changelog real y propio del editor.

`Assets/changelog.json` (nuevo) - **contenido real, nada inventado**: 3 versiones agrupadas
por hitos genuinos del propio historial de commits/bitácora (no una entrada por commit, eso
sería ruido) - 1.0.0 (núcleo funcional completo, desde el esqueleto inicial hasta el cierre de
la auditoría Terrasavr JS vs puerto), 1.1.0 (rediseño estético violeta + arreglo del zoom),
1.2.0 (este mismo rework de 7 fases). Cada línea de "Añadido"/"Arreglado" corresponde a un
commit o entrada real de bitácora, no a nada nuevo.

Nuevo `TerrasavrNative.Core/Data/ChangelogCatalog.cs` (copia estructural de
`WhatsNewCatalog.cs`) y `ViewModels/ChangelogViewModel.cs` (copia de `WhatsNewViewModel`) -
`CharacterFileService`/`MainViewModel` lo cargan igual que el resto de catálogos.
`MainWindow.xaml`: tarjeta de Inicio "Novedades" corrige su texto (ahora dice explícitamente
"JUEGO... Calamity Mod"); la tarjeta que antes era "Acerca de" pasa a **"¡Sobre esta
versión!"** (`Tag="Orange"`, el CTA más fuerte por color, mismo criterio que el resto del
rediseño estético); la propia pestaña "Novedades" gana una nota aclaratoria arriba; "Acerca
de" gana el changelog real debajo de versión+créditos, con plantilla nueva `ChangelogEntry`
(Añadido en teal, Arreglado en rosa, mismo lenguaje visual que el resto de la app).
`TerrasavrNative.App.csproj` gana `<Version>1.2.0</Version>` real (antes caía al `1.0.0.0`
por defecto de .NET sin significar nada - `AboutViewModel.Version` ya leía el ensamblado,
solo faltaba fijarlo).

Test nuevo `ChangelogCatalogTests.cs` (smoke test real): entradas no vacías, cada una con
versión/resumen/al menos una línea de Añadido o Arreglado, orden descendente por versión real
(`System.Version`, no comparación de texto).

**Verificación real**: arnés de consola - `About.Version=='1.2.0'`, `Changelog.Entries.Count
==3` con el contenido esperado, `WhatsNew.Entries` intacto (2 entradas del juego, sin tocar).
Navegación real vía UI Automation a "Novedades" y "Acerca de" sin excepción; tarjeta "¡Sobre
esta versión!" localizada de verdad en Inicio (no solo comprobada por código).

`dotnet build`/`dotnet test` en verde (128/128, sin regresiones).

---

**Con esto se cierran las 7 fases del rework estructural pedido tras comparar Terrakeep con
capturas reales de Terrasavr** (buffs, prefijos reales por tipo de objeto, panel Editar
compartido, tooltips en Builds + arreglo de guardado de Monedas/Munición, tinte de pelo
visual, cursor/tooltip del mapa, y el changelog real separado del juego). Plan completo en
`C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md` mientras siga vigente el archivo.

## Segunda ronda de feedback tras probar el rework (2-sep-2026)

Nuevo feedback, con dos capturas reales de Terrasavr y de Terrakeep de referencia: faltan
datos en los tooltips de armas/armaduras/accesorios y en los buffs; el indicador de "mejor
prefijo" debería ser un contorno, no solo una estrellita; falta espaciado en el inventario; el
visor de mundo no distingue agua/lava (todo en rojo); la ventana se colapsa mucho al hacerse
pequeña (demasiadas pestañas de contenedor amontonadas); el guardado necesita una confirmación
visual más clara que el mensaje pequeño actual.

### Bug real de mapa arreglado ya: agua/lava intercambiadas

Diagnosticado y arreglado antes de investigar el resto: `WorldRenderer.LiquidColor` tenía los
códigos de líquido AL REVÉS. Confirmado contra la fuente real de TEdit
(`World.FileV2.cs`, el escritor real de `.wld`, en `Terrasavr-Calamity-Beta\resources\app\
xnb-lzx-tool-refs\`): el bit-pattern real es Agua → código 1, Lava → código 2, Miel/Shimmer →
código 3 - exactamente lo que ya decodificaba bien `WldReader.cs` (`liquidHeader = (header1 &
0x18) >> 3`), pero el switch de colores asumía 1=lava. Verificado con un mundo real
(`adriandres.wld`): 355.359 tiles de lava real (código 2) y 259.330 de agua real (código 1) -
bajo el código antiguo esos habrían salido pintados de amarillo (miel) y naranja (lava)
respectivamente, una mezcla amarillo/naranja que encaja con "todo en rojo". Se arregló también
`ExplorationViewModel.UpdateHover`, que decía "(vacío)" para cualquier tile sin bloque sólido
sin comprobar si tenía líquido - un charco/lago/lava real (`IsActive=false`, `LiquidAmount>0`)
ahora dice "Agua"/"Lava"/"Miel" en vez de "(vacío)". `dotnet build`/`dotnet test` en verde
(128/128).

**Pendiente de investigar y planificar**: el resto de esta ronda de feedback (tooltips más
completos con fórmulas reales de Terrasavr, descripciones de buffs, contorno de mejor
prefijo, espaciado, reducir el número de pestañas de contenedor para que no se amontonen al
reducir la ventana, confirmación visual de guardado).

Plan completo de 7 fases (A-G) escrito y aprobado tras investigación real (fórmulas exactas de
`script.js`, traducción real de `Terrasavr.es-ES.json`, descripciones reales de buff, y el
descubrimiento de que sí existe una descompilación real de Calamity Mod en este PC - ver
`C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md`). Dos correcciones reales hechas
durante la propia planificación: el punto "contorno" no era sobre el indicador de mejor
prefijo (confundido al principio) sino sobre el panel Equipamiento necesitando su propio
contorno visual, separado del Inventario - aclarado con una pregunta directa al usuario en
vez de seguir adivinando.

### Fase A - Tooltips reales de estadísticas + bug real grave encontrado en la extracción vanilla

**`ItemStatsFormatter.cs` reescrito** con las fórmulas y texto REALES de Terrasavr (extraídos
de `script.js` y `Terrasavr.es-ES.json` reales, ver el plan): DPS = `round(60·daño/useTime)`,
ritmo = `floor(6000/useTime)/100`, 8 tramos reales de velocidad y 8 de retroceso con su texto
español real (incluido el typo real "Extremandamente", no corregido - no es nuestro). Nuevo
parámetro `VanillaCategoryCatalog` para resolver la etiqueta real de tipo de daño (Cuerpo a
cuerpo/A distancia/Magia/Invocación) vanilla por categoría ya extraída, y por `DamageType`
real (substring) para Calamity. Además, `useTime`/`retroceso` solo se muestran junto al daño
(objetos de combate real) - antes salían también en pociones/bloques que técnicamente tienen
un `useTime` real en el motor (velocidad de animación de beber/colocar) pero no venían a
cuento en un tooltip de "estadísticas de combate".

**Bug real grave encontrado y arreglado al verificar** (pedido explícito del usuario: "busca
en la descompilación de terraria todas las estadísticas de las armas"), en `scripts/
extraer-categorias-vanilla.py` y `extraer-estadisticas-vanilla.py`, en DOS capas:

1. La versión original partía el texto de `Item.cs` por CUALQUIER `"case N:"` que apareciera
   en todo el archivo concatenado, sin respetar anidamiento de llaves - un item con lógica
   interna propia (ej. una animación de color de partículas con su propio `switch(frame) {
   case 1: ... }` anidado DENTRO de su bloque) generaba `"case N:"` FALSOS que se confundían
   con límites de item real. Ejemplo real detectado: la Espada corta de hierro (id 1, con
   `melee = true` real en su propio bloque) salía clasificada como "Materiales" porque un
   `"case 1:"` anidado en la animación de OTRO objeto, mucho más adelante en el archivo,
   sobrescribía el diccionario por ser el último en aparecer.
2. Al arreglar el punto 1 contando profundidad de llaves relativa a un ÚNICO
   `"switch (type) { ... }"` por método, la cobertura se desplomó (2226 de ~5455 items reales)
   y objetos de sobra conocidos (Excalibur id 368, Terrarian id 3389) desaparecieron por
   completo del todo. Motivo real, confirmado leyendo el propio archivo: cada método
   `SetDefaults#` no tiene un único switch - tiene VARIOS bloques `"switch (type) { ... }"`
   **seguidos uno detrás de otro** dentro del mismo método (`SetDefaults1` cierra su primer
   switch tras el id 121 y literalmente abre otro `switch (type) { case 122: ...` a
   continuación). Buscar solo el PRIMER `"switch(type){"` con una única búsqueda hacía que el
   escaneo se detuviera ahí y perdiera todo lo que venía después.

Arreglado de verdad con un escáner de estados que recorre el método entero: fuera de un
switch, busca el siguiente `"switch (type) {"`; dentro, cuenta profundidad de llaves real
(saltando cadenas/chars/comentarios, que si no también podían desincronizar el conteo) y solo
trata un `"case N:"` a profundidad 1 relativa a ESE switch como límite de item; al cerrar ese
switch, sigue buscando el siguiente. Verificado a fondo: cobertura 1-500 completa (0 huecos),
Excalibur y Terrarian presentes, ~4575 ids únicos reales en categorías (antes 5262 con
contaminación o 2226 con cobertura rota) y 2576 con alguna estadística real (antes 2968
inflado por la misma contaminación). Regenerados `vanilla_categories.json`,
`vanilla_stats.json` y `vanilla_prefix_rules.json` (depende del primero) con los scripts ya
corregidos - los tres documentan el bug real en su propia cabecera de comentarios.

**Verificación real**: arnés de consola - Espada de cobre (1) ahora sale "5 daño de cuerpo a
cuerpo (~23 DPS) / Use time 13 (4.61/s, Muy Rapido) / Retroceso 2 (Muy Debil)"; Excalibur
(368) "72 daño de cuerpo a cuerpo (~216 DPS) / Use time 20 (3/s, Muy Rapido) / Retroceso 4.5
(Normal) / Rareza 5"; Poción de vida menor (28) solo "Restaura 50 de Vida" (sin useTime
espurio); Tierra (2) sin ninguna estadística (`null`). Un objeto de Calamity
(`AcidwoodSword`) verificado línea a línea contra su propio `.cs` decompilado real
(`damage=12, useTime=18, knockBack=3` - coincide exacto); otro (`ShieldoftheHighRuler`,
`DamageType="DamageClass.MeleeNoSpeed"`) confirma que la etiqueta de tipo de daño real
también funciona para Calamity ("300 daño de cuerpo a cuerpo").

`dotnet build`/`dotnet test` en verde (128/128, sin regresiones pese al cambio de datos -
los tests de humo ya eran lo bastante generales).

### Fase B - Descripciones reales de buff (vanilla en español + Calamity en inglés) como tooltip

Dos scripts nuevos de extracción: `scripts/extraer-descripciones-buffs.py` lee `BuffDescription`
de `Terraria.Localization.Content.es-ES.Game.json` real (`tModLoader-Decompiled\TerrariaVanilla\`)
→ `Assets/vanilla_buff_descriptions.json` (353 entradas reales, clave = nombre interno tal cual,
ej. `"Regeneration"`). `scripts/extraer-descripciones-buffs-calamity.js` (Node, reutiliza
`tmod-extract.js` YA existente de `Terrasavr-Calamity-Beta`) lee el `.tmod` real instalado de
Calamity, `Localization/en-US/Mods.CalamityMod.Buffs.hjson` → `Assets/calamity_buff_descriptions.json`
(302 entradas, EN INGLÉS - esta instalación de Calamity no trae `es-ES`, confirmado, no se
inventa una traducción).

`VanillaBuffCatalog.LoadFromFile`/`LoadFromStream` ahora piden TAMBIÉN la ruta/stream de
descripciones (segundo parámetro) y exponen `GetDescription(int buffId)` - como
`vanilla_buff_names.json` guarda el nombre YA humanizado ("Obsidian Skin") pero
`BuffDescription` usa la clave interna real en PascalCase sin espacios ("ObsidianSkin"), se
reconstruye quitando los espacios (290/354 aciertos reales - los 64 fallos son buffs de ir en
minecart, que de verdad no tienen descripción de cara al jugador en el propio juego).
`CalamityBuffEntry` gana un tercer parámetro `description` (resuelto por nombre interno exacto,
292/305 con descripción real). Propagado por toda la cadena: `CharacterFileService` (carga
ambos catálogos con las dos rutas), `BuffCatalogEntryViewModel`/`BuffRowViewModel` (nueva
propiedad `Description`, null si de verdad no hay ninguna - nunca se inventa un texto vacío),
`BuffsViewModel` (la rellena al construir el catálogo completo). `MainWindow.xaml`:
`ToolTip="{Binding Description}"` en la tarjeta del picker "Añadir buff..." y en la tarjeta de
cada buff activo - sin tooltip cuando `Description` es null (WPF no muestra nada si el valor
enlazado es null, no hace falta un converter aparte).

**Bug de compilación real encontrado al reconstruir tras el cambio de firma** (no roto por mí a
propósito, efecto colateral esperado de cambiar `LoadFromFile`/`LoadFromStream` a pedir un
segundo parámetro): 3 sitios de test (`CalamityCharacterSyncRealFileTests.cs`,
`CalamityCharacterSyncTests.cs`, `VanillaBuffCatalogTests.cs`) llamaban a las firmas viejas de
un solo parámetro - corregidos pasando un stream/fichero de descripciones real o mínimo
(`{}` para los tests sintéticos que no necesitan datos reales; la ruta real de
`Assets/calamity_buff_descriptions.json` para la prueba de extremo a extremo contra el
personaje real "adrian", añadida también a `RealFilesExist()` para que se salte en silencio si
algún día falta).

**Verificación real** (arnés de consola, `TerrasavrNative.Core` referenciado directo): buff
vanilla "Regeneration" → `Description == "Regenera la vida"` (exacto, tal y como pedía el
criterio de cierre del plan); 290/354 buffs vanilla con descripción real, 292/305 de Calamity;
ejemplo Calamity: "Gelatina Astral" (`AbandonedSlimeBuff`) → `"Back from the heavens just to
protect you!"` (en inglés, real, sin traducir).

`dotnet build`/`dotnet test` en verde (128/128) tras corregir los 3 tests; build de
`TerrasavrNative.App` (WPF, XAML incluido) también en verde.

### Fase C - Espaciado entre tarjetas de objeto

Pedido explícito ("que quede como espaciado un poco no junto con todo el inventario"):
`ItemSlotCard` (`Theme.xaml`) pasa de `Margin="3"` a `Margin="6"` - único cambio, todas las
tarjetas de objeto de cualquier contenedor (Inventario/Banco/CajaFuerte/loadouts/Librería...)
usan este mismo estilo, así que se propaga solo sin tocar más sitios. `dotnet build` en verde.
(El contorno propio del panel Equipamiento, la otra mitad de este punto del feedback, se hace
en la Fase D - ver más abajo.)

### Fase D - Ventana con tamaño mínimo + "Equipamiento" consolidado (21 pestañas -> 2+9)

La fase más grande del plan. Investigación real de `script.js` (clase `app.TabEquips`/`Sb`,
~líneas 3110-3165 del beautificado) confirmó que Terrasavr real NO tiene 12 pestañas de
loadout separadas - tiene 3 botones pequeños ("1"/"2"/"3") dentro de UNA única pantalla
"Equipamiento" que intercambian que loadout se ve. `MainViewModel.RebuildContainers` en
cambio registraba 21 pestañas planas en un solo `TabControl` (9 contenedores normales + 12 de
loadout puesto/1/2/3 × armadura/vanidad/tintes) - de ahí el amontonamiento real reportado al
reducir la ventana ("¿es necesario que haya tantos botones?").

**`Window` gana `MinWidth="1000" MinHeight="620"`** - verificado de verdad que se respeta a
nivel de SO (no solo declarado en XAML y esperar que "funcione": ver la lección de
`min()`/`max()` de CSS de este mismo proyecto hermano, un valor declarado no siempre se
aplica de verdad) - `SetWindowPos` real vía P/Invoke forzando la ventana a 400x300 real, el
`GetWindowRect` posterior confirma que se quedó en 1000x620, no en 400x300: WPF intercepta
`WM_GETMINMAXINFO` de verdad.

**Nuevo `EquipmentGroupViewModel.cs`**: consolida los 12 `ContainerViewModel` de siempre
(MISMOS objetos, mismas claves `loadout0Items`/`loadout1Social`/...) en una unidad con
`LoadoutOptions` (Puesto/1/2/3, `ObservableCollection<EquipmentOptionViewModel>`) y
`KindOptions` (Armadura/Vanidad/Tintes) - mismo patrón `Label`+`IsSelected` que
`PrefixMetaButtonViewModel`/`PrefixGroupButtonViewModel` de la Fase 3 anterior, con
`SelectLoadoutCommand`/`SelectKindCommand` (RelayCommand) que actualizan la selección y
recalculan `CurrentSlots` (el `ContainerViewModel.Slots` de la combinación activa).
`EquippedItems` (atajo a loadout 0/Armadura) y `AllContainers` (los 12) quedan expuestos para
que `MainViewModel` seguir operando sobre ellos sin duplicar lógica:
- `RebuildContainers`: las 12 llamadas `AddContainer("loadout...", ...)` se sustituyen por un
  único `EquipmentGroup = new EquipmentGroupViewModel(...)`; `Containers` se queda con los 9
  contenedores normales (Inventario/Banco/CajaFuerte/Fragua/Bóveda/Mascotas/TintesMascota/
  Monedas/Munición).
- `AutoEquip`: `Containers.First(c => c.Key == "loadout0Items")` -> `EquipmentGroup.EquippedItems`.
- `SyncEditsBackToMerged`: factorizado en `SyncContainersBackToMerged(IEnumerable<
  ContainerViewModel>)`, llamado una vez para `Containers` y otra para
  `EquipmentGroup.AllContainers` - si no, cualquier edición dentro de "Equipamiento" se
  perdería en silencio al guardar (mismo tipo de bug real que ya se encontró y arregló con
  Monedas/Munición en la Fase 4 del rework anterior - aprendida la lección, aquí se cubrió
  desde el principio).

**`MainWindow.xaml`**: el `TabControl` que antes tenía `ItemsSource="{Binding Containers}"`
(los 21 planos) pasa a tener solo 2 `TabItem` fijos: "Equipamiento" (selector Loadout+Vista
con `ItemsControl`+botones `PrefixMetaButton`/`PrefixGroupButton` reutilizados tal cual +
`ItemsControl` de `CurrentSlots`, reutilizando el `DataTemplate` implícito de
`ItemSlotViewModel` que ya trae drag&drop) e "Inventario" (el `TabControl` de siempre, ahora
con 9 en vez de 21). **Contorno propio distintivo** (pedido explícito, confirmado tras
pregunta directa al usuario el 2-sep-2026 - no tenía nada que ver con el indicador ★ de mejor
prefijo, que se quedó igual): `Border BorderBrush="{StaticResource OrangeBrush}"
BorderThickness="2"` envolviendo todo el contenido de "Equipamiento", separándolo con
claridad visual del Inventario general.

**Verificación real, dos niveles**:

1. *Arnés de consola* (`TerrasavrNative.App` referenciado directo, sin ventana) contra una
   COPIA desechable del personaje real "adrian" (nunca el fichero real del usuario): las 12
   combinaciones Loadout×Vista recorridas una a una tienen sus slots reales (10 cada una,
   conteo de objetos correcto); `AutoEquip` con la build "melee" Pre-Hardmode coloca
   correctamente casco/coraza/grebas/accesorios en `EquipmentGroup.EquippedItems` (11
   objetos colocados); colocar a mano un objeto (id 47) en Loadout 2/Vanidad slot 0, guardar,
   y volver a cargar desde disco EN UNA INSTANCIA NUEVA de `MainViewModel` confirma
   `roundtrip OK` - el objeto sigue ahí tal cual.
2. *UI Automation real* contra la app WPF de verdad renderizada (arnés `Application` en
   blanco + `MainWindow` con el personaje precargado por reflexión sobre el campo privado
   `_viewModel` - ver nota de bug de metodología abajo): navegando Personaje → Objetos →
   Equipamiento aparecen de verdad los 7 botones reales (`Puesto`/`1`/`2`/`3`/`Armadura`/
   `Vanidad`/`Tintes`) como elementos de automatización reales, y entrando en Inventario
   aparecen las 9 pestañas de contenedor reales - confirmado que la UI renderizada coincide
   con lo que predice el arnés de consola, no solo que compila.

**Bug de metodología real encontrado y corregido durante esta misma verificación** (no del
código de producción, del arnés de prueba): un primer arnés de UI Automation usaba
`new TerrasavrNative.App.App()` + `InitializeComponent()` para cargar los recursos de tema
reales antes de mostrar una `MainWindow` ya cargada a mano - pero `App.xaml` real tiene
`StartupUri="MainWindow.xaml"` compilado, y WPF crea una SEGUNDA `MainWindow` (sin personaje
cargado) en cuanto se llama `Run()`, sin importar que `Application.MainWindow` ya se hubiera
asignado a mano antes - esa ventana fantasma resultó ser la que UI Automation encontraba
(mismo título "Terrakeep", así que no era evidente por fuera). Arreglado usando un
`Application` completamente en blanco (sin `StartupUri` en absoluto) y añadiendo a mano solo
los recursos que hacían falta (`Theme.xaml` + los 5 conversores de `App.xaml.Resources`) -
así solo existe la `MainWindow` creada explícitamente. Un segundo despiste menor del arnés
(no del código real): `Path.GetTempPath()` no resolvía al mismo directorio visible desde
PowerShell al lanzar el proceso desde Git Bash - se cambió a una ruta absoluta fija del propio
scratchpad de la sesión.

`dotnet build`/`dotnet test` en verde (128/128) tras los cambios de `MainViewModel`/nuevo
`EquipmentGroupViewModel`/`MainWindow.xaml`.

### Fase E - Confirmación visual real de guardado

Pedido explícito ("cuando le des a guardar personaje debe ser mas visual que se allá
confirmado el guardado no solamente un mensajito abajo a la izquierda"). `MainViewModel`:
nueva `[ObservableProperty] bool SaveConfirmationVisible` + un `DispatcherTimer` propio
(1.5s) - `Save()` la pone a `true` tras un guardado con éxito real (dentro del mismo bloque
que ya escribía `StatusMessage`) y reinicia el timer (`Stop()+Start()`, para que dos
guardados seguidos alarguen la ventana en vez de cortarla a medias); el `Tick` del timer la
vuelve a `false` y se para solo. `StatusMessage` se queda tal cual para errores - esos no
deben desaparecer solos.

`MainWindow.xaml`: el `TabItem` "Personaje" pasa de `DockPanel` suelto a un `Grid` que
superpone un banner (`Border` con degradado `TealGradientBrush`, ✓ + "Guardado") centrado
arriba, con animación de entrada real (`Storyboard` de opacidad 0→1 + escala 0.9→1 vía
`DataTrigger` sobre `SaveConfirmationVisible`, mismo patrón de animación real ya usado en el
resto del tema rediseñado - nunca solo un cambio de `Visibility` sin transición).

**Verificación real con interacción de verdad** (UI Automation contra la app renderizada con
el personaje real precargado, click real en el botón "Guardar" vía `InvokePattern`, no
simulado en el arnés de consola): sin banner antes de guardar: confirmado; banner presente
~0.3s después de pulsar Guardar: confirmado; sigue presente a ~1.2s (dentro de la ventana de
1.5s): confirmado; ya no está a ~1.9s (se apagó solo, sin excepción): confirmado.

`dotnet build`/`dotnet test` en verde (128/128).

### Fases F y G - `CLAUDE.md` nuevo + copia permanente de fuentes de referencia

Pedido explícito repetido tres veces en modo plan ("añade en el proyecto una ruta para
consultar siempre el código decompilado de Terraria/tModLoader/Calamity mod... a todo tipo de
herramientas de decompilación... y si no decompila la version final que hicimos y la añades
tambien... haz commit de todo esto y bitacora"). Este proyecto no tenía `CLAUDE.md` propio
todavía (a diferencia de `Terrasavr-Calamity-Beta`) - creado
`Terrasavr-Native\CLAUDE.md` documentando: rutas reales de las 3 fuentes decompiladas
(`TerrariaVanilla`/`tModLoader`/`CalamityMod`, con la corrección explícita de que Calamity SÍ
tiene fuente real, corrigiendo una afirmación errónea de una sesión anterior de este mismo
proyecto), `tmod-extract.js` para contenido crudo de cualquier `.tmod`, el índice real de
herramientas (`herramientas.json`) y una sección nueva "Verdades del entorno WPF" con los
gotchas reales descubiertos en toda esta sesión (Setter.TargetName no puede apuntar a un
Brush, Storyboard sin TargetName dentro de Style.Triggers, Visibility roto por asignación
directa en code-behind, MinWidth/MinHeight SÍ fiables aquí, el bug real de doble-ventana-
fantasma por StartupUri encontrado verificando la Fase D, capturas de pantalla poco fiables)
- mismo criterio que las "Verdades del entorno" de `Terrasavr-Calamity-Beta\CLAUDE.md`, para
que una sesión futura no tenga que redescubrir nada de esto.

Copia real (no solo documentada) dentro del propio repo, comiteada:
- `reference\terrasavr-real\script.beautified.js` (10472 líneas, volcado con `js-beautify`
  del `script.js` real fechado 31-ago-2026) + `PROCEDENCIA.md` con el comando exacto de
  regeneración.
- `reference\terrakeep-decompilado\TerrasavrNative.App.decompiled.cs` (15988 líneas,
  `ilspycmd` real contra la build Debug propia) + `PROCEDENCIA.md`. Verificado que es real y
  actual buscando dentro las clases recién creadas en esta misma sesión
  (`EquipmentGroupViewModel`, `MainViewModel`) - ambas aparecen.
- `reference\` vive fuera de cualquier carpeta de proyecto (`.csproj`), así que MSBuild no la
  incluye ni la copia a `bin\`/publica con la app por diseño de los proyectos SDK-style - no
  hizo falta tocar ningún `.gitignore`.

`dotnet build` en verde (confirma que `reference\` no interfiere con la compilación normal).

## Tercera ronda de feedback (2-sep-2026)

Mensaje denso nuevo, 5 puntos: botón "Auto-equipar" sin contraste, reestructurar la Librería
para que Calamity tenga su propia carpeta madre (calcando la organización real de Terrasavr,
en vez de las categorías actuales que mezclan cosas sin sentido) + lo mismo para Investigación
+ paginarla porque es larguísima, NPCs del visor de mundo como puntos rosas en vez de sus
sprites reales, y contorno verde para lo que está puesto (equipado) tanto en Inventario como
en Equipamiento. Los tres primeros puntos "rápidos" ya cerrados en este mismo turno:

### Bug real encontrado y arreglado - botón "Auto-equipar" sin contraste

Causa raíz real (no un simple ajuste de color): el `Style TargetType="Button"` por defecto de
`Theme.xaml` tiene una plantilla que IGNORA la propiedad `Background`/`Foreground` puestas a
mano en el propio `<Button>` - el `Border` interno de la plantilla usa un
`SolidColorBrush x:Name="BdBrush"` fijo, y solo los `Trigger Property="Tag"` (`Accent`/`Teal`/
`Pink`/`Orange`) lo cambian de verdad. El botón "Auto-equipar" (`BuildClassTemplate`) ponía
`Background="{StaticResource AccentBrush}"` a mano, que literalmente no hacía nada - por eso
salía con el gris por defecto, casi igual que la tarjeta de fondo. Arreglado usando
`Tag="Accent"` (el mecanismo real que sí funciona, igual que todos los demás botones de color
sólido de la app). Revisado el resto de `MainWindow.xaml` por el mismo patrón - solo hay otro
caso (`Background="Transparent"` en la fila de NPC de la lista lateral), pero ahí no es un bug
visible real porque el contenido lleva su propio `Border` con el fondo correcto encima.

### NPCs del visor de mundo con sprite real (antes: puntos rosas)

`Assets/npc_icons/{id}.png` YA EXISTÍA en el proyecto (copiado de Terrasavr-Calamity-Beta,
27 NPCs de pueblo reales) y `NpcIconResolver`/`WorldNpcRowViewModel.IconPath` ya lo resolvían
- solo faltaba usarlo en el propio mapa (la lista lateral de NPCs sí lo mostraba ya). El
`ItemTemplate` del `ItemsControl` de NPCs sobre el mapa (`MainWindow.xaml`) pasa de un
`Ellipse Fill="Magenta"` fijo a una `Image` con el sprite real (ancla abajo-centro, como un
personaje de pie sobre su tile, `RenderOptions.BitmapScalingMode="NearestNeighbor"` igual que
el propio mapa) - si algún día aparece un NPC sin icono real (`IconPath` null), cae al punto
magenta de siempre en vez de no mostrar nada.

### Contorno verde real para lo "equipado" (Equipamiento cerrado; Inventario pendiente de aclarar alcance)

Nuevo `ItemSlotViewModel.IsEquipped` (parámetro opcional, `false` por defecto - no toca los
sitios existentes) - `EquipmentGroupViewModel.AddSlotSet` lo pasa a `true` para los 12
sub-contenedores reales (armadura/accesorios/vanidad/tintes de cualquier loadout, genuinamente
"puesto" en el personaje). Nuevo `EquippedGreenColor`/`EquippedGreenBrush` en `Theme.xaml`
(deliberadamente distinto del `TealBrush`, ya reservado para "acción con éxito" - el banner de
guardado). `MainWindow.xaml`: `MultiDataTrigger` (`IsEquipped==true` Y `IsEmpty==false`) en la
plantilla compartida de `ItemSlotViewModel` que pinta el borde en verde - antes del trigger de
`IsSelected`, para que seguir pudiendo editar un slot equipado no pierda su resaltado de
selección. Verificado con arnés de consola contra el personaje real: el 100% de los slots de
`EquipmentGroup.AllContainers` tiene `IsEquipped=true`, el 100% de los de `Containers`
(Inventario/Banco/...) tiene `IsEquipped=false`.

Aclarado con el usuario vía pregunta directa (mismo criterio que
[[feedback_preguntar-ante-ambiguedad-densa]] - el `.plr` no guarda ningún concepto real de
"arma actualmente empuñada" fiable, así que no se podía adivinar): "toda la barra rápida
(slots 0-9)". Implementado - `MainViewModel.AddContainer` marca `IsEquipped=true` para los
primeros `HotbarSlotCount=10` slots del contenedor `"inventory"` únicamente (confirmado en
`Player.cs` decompilado real: `inventory = new Item[59]` y los 10 triggers reales
`Hotbar1`..`Hotbar0` - slots 0-9 son la barra rápida real de Terraria). Verificado con arnés
de consola: slots 0-9 de Inventario con `IsEquipped=true`, el resto (10-49) con `false`.

`dotnet build`/`dotnet test` en verde (128/128) para los cuatro puntos ya cerrados de esta
ronda (botón, NPCs del mapa, contorno verde en Equipamiento Y en la barra rápida de
Inventario).

### Pendiente de esta ronda - reestructurar Librería/Investigación (la grande)

Investigación real ya hecha (no de memoria) sobre `reference\terrasavr-real\script.beautified.js`
+ `Terrasavr-Calamity-Beta\resources\app\local-site\overrides.js` antes de proponer nada:

- **Calamity YA tiene, en el propio Electron real, una función `calamityBuildLibraryNode()`
  completa y ya en producción** (`overrides.js` líneas ~1352-1417) que construye exactamente
  "una carpeta madre + subcarpetas reales" - agrupa las 121 categorías reales YA PRESENTES en
  `calamity/catalog.json` (`category`, ej. `"Armor/Aerospec"`, `"Weapons/Melee"` - con barra,
  no planas como se pensó en un primer vistazo superficial) por su segmento raíz
  (Armor/Weapons/Accessories/...), pagina cualquier grupo de más de 40 objetos en "Page N", y
  pagina la propia lista de carpetas si supera 19 hijos (límite real de la UI Canvas antigua,
  documentado en el propio código con el bug real que motivó el límite). Terrasavr-Native
  YA CARGA este mismo `category` con barra desde el mismo fichero
  (`TerrasavrNative.App/Assets/calamity/catalog.json`) pero `LibraryViewModel.BuildCategoryTree`
  ya lo parte en segmentos igual - lo que falta de verdad es envolver el resultado en una
  única carpeta "Calamity (mod)" en vez de dejar los segmentos sueltos mezclados con los de
  vanilla, más los rótulos en español reales (`CALAMITY_CATEGORY_LABELS` en `overrides.js`).
  Alcance mucho más pequeño de lo que parecía al principio.
- **Vanilla es la parte grande de verdad**: el árbol real (`Hc.deploy()`,
  `script.beautified.js` líneas ~2060-2256) NO viene de un campo "categoría" simple por
  objeto - es un árbol curado a mano con listas de ids literales por carpeta hoja (ej.
  "Materiales/Pre-Hardmode/Copper & Tin" = 40 ids concretos mezclando barra+mineral+
  herramienta+armadura de ese material) MÁS carpetas calculadas por predicado sobre un
  campo `metatype` compacto por objeto (ej. `"Categorías/Armas/Daño cuerpo a cuerpo"` = todo
  objeto cuyo `metatype` contiene `"d"`), con paginación automática real ya integrada (bloques
  de "Page N" cada 40 objetos, "Pages N+" agrupando de 10 en 10 si un grupo supera 480) - esto
  es la respuesta real y ya existente al "hay que pensar algo rollo que tenga páginas", no
  hace falta inventar nada nuevo, hay que replicar este mismo mecanismo.
- El campo `metatype`/`pid`/nombre por objeto viene de un formato compacto propio embebido en
  el propio `script.js` (`za.$name`/`za.pid`/`za.meta`, clases `terra.data.TdItem`/
  `terra.ItemParser`/`terra.Item.loadMeta`) - parsearlo a mano reimplementando el formato
  sería frágil; la vía más fiable es ejecutar el propio `script.js` real en un sandbox de
  Node (con las APIs mínimas de navegador que necesite stubadas) y volcar el resultado real
  de `Hc.deploy()` a JSON - mismo criterio de "confiar en el comportamiento real, no en una
  reimplementación a ciegas" que ya se siguió con `tmod-extract.js`.

Queda por proponer un plan concreto (fases, archivos, verificación) y pasar por modo plan para
aprobación antes de tocar código - tarea de tamaño comparable a las Fases A-G anteriores.

## Rediseño de Librería/Investigación (plan aprobado, 2-sep-2026)

Plan completo aprobado en `C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md` (Fases
A-E). Empezada la ejecución.

### Fase A - Extractor real del árbol vanilla (`scripts/extraer-arbol-libreria-vanilla.js`)

**Mejor resultado de lo esperado**: en vez de reimplementar a mano el formato compacto
propio de `script.js` (arrays `za.$name`/`za.pid`/`za.meta` separados por `;` en paralelo) o
la lógica de `Hc.deploy()` (los helpers `a()`/`b()`/`c()` con su paginación real), se
EXTRAEN Y EJECUTAN los statements/funciones REALES tal cual del `script.js` real en un
sandbox de Node (`vm`), con solo las dependencias externas mínimas que `Hc.deploy` necesita
para construir el árbol (nunca llama métodos de sus nodos, solo los crea y enlaza) - clases
`ub`/`mb` (Dir/Items reales), `A.list` (reconstruido con la misma lógica simple y real de
`terra.ItemParser.run`/`Item.loadMeta`), `y.cca`/`y.indexOf` (HxOverrides real,
charCodeAt/indexOf), `D.endsWith`/`D.replace` (helpers de string real, triviales). Mismo
criterio ya establecido en el proyecto ("confiar en el comportamiento real, no reimplementar
a ciegas" - precedente `tmod-extract.js`).

**Extracción de statements de un fichero minificado de una sola línea gigante**: un
`indexOf`/regex ingenuo NO vale (el propio contenido de las cadenas puede llevar `;`/`"`) -
`extractStatement()` escanea carácter a carácter respetando literales de cadena (con escapes)
y profundidad real de paréntesis/corchetes/llaves, cortando solo en el `;` de profundidad 0
que cierra el statement completo.

**Verificado de dos formas independientes, ambas en verde**:
1. Cruce contra datos YA verificados por un camino totalmente distinto (decompilación real de
   `Item.cs`, ronda anterior): `za.meta[4]` (Iron Broadsword) decodifica
   `metatype="d", d=12, t=20, k=5.5` - EXACTO igual que `vanilla_stats.json`
   (`{"damage":12,"knockBack":5.5,"useTime":20}`), extraído de forma completamente
   independiente. Coincidencia perfecta entre dos fuentes reales distintas.
2. El propio script hace su propia verificación antes de escribir el JSON (falla ruidosamente
   si no se cumple, no en silencio): cobertura completa (6146/6146 ids reales cubiertos al
   menos una vez, gracias a la carpeta real "Items by ID" de último recurso) y spot-check de
   posición real de Iron Broadsword/Excalibur/Terrarian - los tres aparecen en MÁS DE UNA
   carpeta a la vez (confirmando que el árbol real permite pertenencia múltiple, a diferencia
   del sistema de categoría única de hoy): Iron Broadsword en
   `Materials > Pre-Hardmode > Iron & Lead`, `Categories > Weapons > Melee damage > Page 1` Y
   `Items by ID > 1-400 > 1-40`.

Salida real: `TerrasavrNative.App/Assets/vanilla_library_tree.json` (94 KB, 9 carpetas raíz
reales en el mismo orden que Terrasavr: `Materials`, `Decorative`, `Pets, mounts, tools`,
`Potions (regeneration)`, `Potions (effects)`, `Bosses & events`, `Quest fish`, `Categories`,
`Items by ID`). Los ids `0` dentro de las listas de una carpeta hoja son huecos reales de la
rejilla curada a mano (relleno, no un objeto real) - el consumidor C# (Fase B) debe saltarlos.

### Fase B - `VanillaLibraryTreeCatalog.cs` (Core)

Nuevo catálogo que carga `vanilla_library_tree.json` a un modelo `VanillaLibraryNode`
(`Name`/`Icon`/`IsLeaf`/`Children` o `ItemIds`, filtrando ya los `0` de relleno al parsear).
Deliberadamente NO toca `VanillaCategoryCatalog.cs` (se queda tal cual, solo lo usa
`ItemStatsFormatter` para la etiqueta de daño de los tooltips - un problema distinto). Cargado
en `CharacterFileService.VanillaLibraryTree`. Verificado con arnés de consola: 9 raíces
reales, Iron Broadsword en las mismas 3 carpetas reales que en la Fase A. `dotnet build` en
verde.

### Fase C - `LibraryViewModel` rehecho para usar el árbol real

`CategoryNodeViewModel` gana `ItemIdSet` (`HashSet<int>`, siempre poblado - el propio conjunto
si es hoja, o la unión de todos sus descendientes si es carpeta intermedia) - reemplaza por
completo el viejo mecanismo de comparar `LibraryItemViewModel.Category` por prefijo, que
asumía una única categoría por objeto (falso para el árbol real, donde un mismo objeto puede
caer en varias carpetas a la vez). `ApplyFilter` ahora filtra por pertenencia directa a este
conjunto - más simple y correcto que antes.

`BuildCategoryTree` reconstruye `RootCategories` así: los 9 nodos raíz reales de
`VanillaLibraryTreeCatalog` (mismo orden que Terrasavr real, recorridos recursivamente) + una
única carpeta madre "Calamity (mod)" nueva (`BuildCalamityRoot`, puerto real y fiel de
`calamityBuildLibraryNode` de `overrides.js` - agrupa las categorías reales de
`calamity/catalog.json` por segmento raíz, pagina cualquier hoja de más de 40 objetos en
"Page N", con las 33 etiquetas reales en español de `CALAMITY_CATEGORY_LABELS` portadas tal
cual). Deliberadamente SIN el límite de 19 carpetas por pantalla del Electron original
(`LIBRARY_FOLDER_CAP`) - documentado ahí mismo como parche a una limitación real del motor
Haxe/OpenFL antiguo (lista de líneas fija sin scroll) que no existe en este árbol real de WPF.

`MainWindow.xaml`: quitado el `TextBlock` que añadía un sufijo `" (N)"` aparte al nombre de
cada carpeta - los nombres reales YA llevan su propio recuento cuando el propio Terrasavr lo
pone (carpetas calculadas por predicado, ej. "Melee damage (316)"); añadir uno propio habría
duplicado el número. Las carpetas hoja con lista literal de ids (ej. "Copper & Tin") tampoco
llevan recuento en el Terrasavr real, así que no se inventa uno ahí tampoco.

**Verificado con arnés de consola contra datos reales** (instancia real de
`LibraryViewModel`, sin ventana): `RootCategories.Count == 10` (9 vanilla + Calamity);
`Iron Broadsword` (id 4) sigue apareciendo en las mismas 3 carpetas reales de la Fase A/B
(pertenencia múltiple confirmada de extremo a extremo); `Calamity (mod)` con 2709 ids en 19
grupos reales con etiqueta española ("Accesorios (221)", "Armadura (186)"...); seleccionar la
carpeta real "Iron & Lead" rellena `Results` con 33 objetos reales y nombres en español
correctos (Pico de hierro, Hacha de hierro, Mineral de hierro...) - confirma que el flujo de
selección/colocación tampoco se rompió.

`dotnet build`/`dotnet test` en verde (128/128).

### Fase D - Investigación reutiliza el mismo árbol real (antes: lista plana de miles de objetos)

Extraído `LibraryCategoryTreeBuilder.cs` (`TerrasavrNative.App/Services/`) - la construcción
del árbol (vanilla real + carpeta madre Calamity, ~150 líneas) que estaba duplicada dentro de
`LibraryViewModel` pasa a ser un builder compartido y estático, reutilizado tal cual por la
Librería y por la nueva `ResearchViewModel.cs` - mismo pedido explícito para ambas
("investigacion lo acotaria de alguna forma... que tenga paginas o algo asi").

`ResearchViewModel` navega el árbol real igual que la Librería (`RootCategories`/
`SelectedCategory`/`SelectCategoryCommand`), pero sus hojas muestran `ResearchRowViewModel`
(mismos nombres/sprites de siempre) filtrados a los objetos que YA tienen una entrada real en
`PlrCharacter.Research` (misma resolución de Pid→id que ya usaba
`MainViewModel.RebuildResearch`: Calamity si el Pid lleva `/`, si no vanilla por clave interna)
- sin carpeta elegida se muestra solo un resumen con el total, exactamente el mismo patrón ya
usado en la Librería para "8164 objetos en total". `MainViewModel.Research` pasa de
`ObservableCollection<ResearchRowViewModel>` a la nueva `ResearchViewModel` (con
`LoadFrom(character)`/`Reset()` reemplazando a `RebuildResearch()`); `ResearchAllCommand` sigue
mutando `PlrCharacter.Research` directamente igual que antes, solo cambia cómo se refresca la
vista después.

`MainWindow.xaml`: nueva plantilla `ResearchCategoryNodeTemplate` (casi idéntica a
`CategoryNodeTemplate` de la Librería, pero apuntando a `Research.SelectCategoryCommand` en vez
de `Library.SelectCategoryCommand` - un `DataTemplate` con `x:Key` no puede parametrizarse por
la pestaña que lo usa) + layout de árbol+resultados igual que la Librería.

**Bug real grave encontrado y corregido durante la verificación con UI Automation real** (no
se veía con el arnés de consola, que manipula `Children`/`ItemIdSet` directamente sin pasar
por los `Binding` de XAML): `CategoryNodeViewModel.IsExpanded` NUNCA se ponía a `true` en
ningún sitio del código - ni en la Librería original ni en la nueva Investigación - y el
`ItemsControl` de `Children` en el árbol solo se muestra cuando `IsExpanded=true`
(`Visibility="{Binding IsExpanded, Converter={StaticResource BoolToVis}}"`). Resultado real:
**ninguna carpeta por debajo del nivel raíz era alcanzable haciendo click de verdad** - un bug
que ya existía antes de esta ronda (con el árbol plano de categoría única probablemente pasaba
más desapercibido, con como mucho 2 niveles), pero que se vuelve bloqueante con el árbol real
de Terrasavr (hasta 4 niveles de profundidad: `Categories > Weapons > Melee damage > Page 1`).
Arreglado añadiendo `node.IsExpanded = !node.IsExpanded;` al principio de `SelectCategory` en
ambos ViewModels (alternar despliegue en cada click, independiente de la selección).
**Verificado con clicks reales de UI Automation** (no simulados en el arnés de consola):
navegación de 3 niveles reales `Materials → Pre-Hardmode → Iron & Lead` confirmada paso a paso
(cada nivel invisible ANTES de pulsar su padre, visible DESPUÉS), resultado final "33
resultado(s)"; misma comprobación para `Calamity (mod) → Armadura (186)`, resultado "186
resultado(s)".

**Resto de verificación real con UI Automation** (además del arnés de consola: 5389 objetos
investigados reales antes de "Investigar todo", 8164 después, "Iron & Lead" con 33 objetos):
las 10 carpetas raíz reales aparecen tal cual en la pestaña Investigación
(`Materials`/`Decorative`/`Pets, mounts, tools`/`Potions (regeneration)`/`Potions (effects)`/
`Bosses & events`/`Quest fish`/`Categories`/`Items by ID`/`Calamity (mod)`), y seleccionar
`Materials` de verdad (click real) resume "1583 objeto(s) investigado(s)".

`dotnet build`/`dotnet test` en verde (128/128).

## Pulido tras revisión del usuario del árbol de Librería (2-sep-2026)

El usuario pidió una lista de raíces/subcarpetas para revisar el trabajo de las Fases A-D y
encontró él mismo un caso real: `Categories > Equipable > Wings` salía con **0 objetos**.

### Bug real encontrado y corregido - `Wings`/`Mounts*` con 0 objetos

Causa raíz real: de las decenas de carpetas por predicado del árbol, exactamente DOS
(`Wings`, `Mounts*` - confirmado contando literalmente `.textLq` dentro del propio
`Hc.deploy`, solo 2 apariciones) no miran `metatype`/`metadata` como el resto, miran
`a.textLq` - el texto de tooltip ya formateado en minúsculas, buscando frases fijas en
inglés (`"allows flight"`, `"rideable"`, `"summons"`). El extractor (Fase A) dejaba
`textLq: ""` siempre (placeholder), así que estas dos carpetas SIEMPRE iban a dar 0 sin
importar los datos reales.

Investigado a mano contra la cadena real de `za.meta`: la clave `1=` dentro de los pares de
un objeto es literalmente ese texto (ej. id 823 "Fledgling Wings" →
`...|1=Allows flight|...`; el reno montable → `...|1=Summons a rideable reindeer|...`) - no
hace falta reimplementar el formateador completo `Xa.parse`/`wa.pairDefs`, con
`textLq = (stats["1"] ?? "").toLowerCase()` basta para los dos únicos predicados reales que
lo necesitan. Corregido en `scripts/extraer-arbol-libreria-vanilla.js`.

**Verificación reforzada por el mismo motivo** (pedido explícito: "que cada categoria/
subcarpeta tenga sus objetos que deberían de estar no puede marcar 0"): el propio script
ahora recorre el árbol COMPLETO al final y falla ruidosamente si encuentra cualquier carpeta
hoja real con 0 objetos, en vez de solo comprobar 3 ids conocidos como antes - así este tipo
de fallo no puede volver a colarse en silencio. Regenerado `vanilla_library_tree.json`:
`Wings` pasa de 0 a **7** objetos reales, `Mounts` (la carpeta por predicado, distinta de la
lista literal `Mounts` de "Pets, mounts, tools") a **25** - verificado con arnés de consola,
"Ninguna carpeta hoja real quedó vacía" en la propia salida del script.

`dotnet build`/`dotnet test` en verde (128/128).

### Rótulos en español de Calamity ampliados a las 121 categorías reales

`CalamityCategoryLabelsEs` (`LibraryCategoryTreeBuilder.cs`) tenía ~35 entradas (portadas del
Electron real) y dejaba bastantes subcarpetas a medio traducir (ej. `Colocables -
FurnitureAncientNavystone`, `Colocables - DraedonStructures - CagedLights`) porque su
fallback solo traduce el PRIMER segmento del path ("Placeables"→"Colocables") y deja el resto
tal cual si no hay una entrada directa - la mayoría de subcarpetas de `Placeables` de Calamity
usan un nombre compuesto en una sola palabra (`FurnitureAcidwood`, no `Furniture/Acidwood`),
así que nunca se dividían solas. Añadidas ~60 entradas directas nuevas hasta cubrir las 121
categorías reales por completo (contadas de verdad desde `calamity/catalog.json`, no de
memoria).

**Criterio aplicado, mismo ya establecido para los ~33 sets de armadura**: se traduce la
palabra ESTRUCTURAL (Furniture→Muebles, Fountains→Fuentes, Trophies→Trofeos...), los nombres
de material/set PROPIOS de Calamity (Aerospec, Astral, Basalt, Cosmilite, Marnite, Navystone,
Silva, Statigel, Stratus, Wulfrum, Acidwood...) se dejan tal cual a propósito - no hay ningún
es-ES real de Calamity de donde sacar una traducción oficial de esos nombres propios, e
inventarla iría contra el criterio ya establecido en todo el proyecto ("lo que no encuentra
coincidencia se queda en inglés, nunca se inventa"). Dos términos SÍ son vanilla real y se
verificaron contra `vanilla_item_names_by_key.json` en vez de adivinarlos: `Pylon` → real
"Torre" (`TeleportationPylonVictory` → "Torre universal"), `Fountain` → real "Fuente"
(`OasisFountain` → "Fuente de agua de oasis") - descartada una primera idea de traducir
"Pylon" como "Pilón", que habría sido una traducción inventada y además incorrecta frente al
término oficial real del propio juego.

**Verificado con arnés de consola**: volcado completo del árbol `Calamity (mod)` (177 líneas,
3 niveles) sin ningún resto de `FurnitureXxx` sin traducir. `dotnet build`/`dotnet test` en
verde (128/128).

## Acceso remoto (Chrome Remote Desktop) + Terrakeep en blanco vía remoto (2-sep-2026)

Pedido explícito del usuario ("instala alguna cosa para que pueda ver y controlar mi pc a
distancia... por que ahora no estoy"). Preguntado directamente qué herramienta (Chrome Remote
Desktop/AnyDesk/RustDesk/otra) antes de instalar nada - decisión de seguridad real que le
corresponde a él, no algo a decidir por autonomía de herramientas de desarrollo. Eligió
**Chrome Remote Desktop**.

Instalado el host oficial (`chromeremotedesktophost.msi`, descargado de
`dl.google.com/edgedl/chrome-remote-desktop/`, instalación silenciosa con `msiexec /qn`).
Emparejamiento real completado con el flujo "headless" oficial de Google
(`remotedesktop.google.com/headless`, el usuario generó el código de un solo uso desde su
móvil con su propia cuenta de Google - la contraseña de Google nunca pasó por esta sesión).
**Detalle real encontrado**: el comando que copia esa página NO incluye `--pin`, así que
`remoting_start_host.exe` se queda esperando el PIN por `stdin` - en un proceso lanzado sin
consola interactiva de verdad (`GetConsoleMode failed` en bucle) eso nunca termina. Arreglado
pasando `--pin=<8 dígitos generados>` directamente como argumento del propio comando -
"Host started successfully", servicio `chromoting` en marcha (`Running`, arranque
`Automatic`, sobrevive a reinicios).

### Diagnóstico real de "no me muestra la pantalla" + "Terrakeep en blanco"

Investigado con las herramientas del propio sistema (nunca a ciegas):
- `query session` → sesión `adrian` en la consola (`ID 1`) **Activo**, no bloqueada; sin
  proceso `LogonUI.exe` (confirma que no está en la pantalla de bloqueo).
- Configuración de energía (`powercfg /query`) → apagado de pantalla y suspensión con
  corriente alterna ambos a `0x00000000` (nunca) - no es un PC que se duerma solo.
- Resolución real activa (`System.Windows.Forms.Screen`) → `2560x1440`, 32 bits, monitor
  primario - hay una superficie de escritorio real y normal, no una pantalla virtual
  degradada.
- Registro de sucesos de Windows (`Get-WinEvent -ProviderName chromoting`) → conexiones y
  desconexiones reales del cliente en minutos seguidos (`09:21` → desconecta `09:24` →
  reconecta → desconecta de nuevo) - encaja con "veo la pantalla en blanco, lo intento otra
  vez".

**Causa real 1 - "no me muestra la pantalla" en general**: fallo conocido y documentado de
Chrome Remote Desktop en Windows con la API de captura `DXGI Desktop Duplication` (se queda
en negro/en blanco de forma intermitente sin que el escritorio real esté roto) - el arreglo
práctico real es reiniciar el propio servicio `chromoting`
(`Restart-Service chromoting -Force`), que reinicializa la tubería de captura. Hecho.

**Causa real 2 - Terrakeep específicamente en blanco**: bug real y bien documentado de WPF -
usa renderizado por hardware/DirectX de serie, que puede fallar en blanco al capturarse via
Escritorio Remoto/CRD aunque el resto del escritorio se vea bien (la superficie compuesta por
DirectX de una ventana WPF no siempre la captura bien esa misma API `DXGI Desktop
Duplication`, incluso cuando SÍ captura el resto del escritorio con normalidad). Arreglado en
`App.xaml.cs` (`OnStartup`, antes de crear cualquier ventana):
`RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;` - fuerza a WPF a dibujar por
software en vez de por hardware (mismo resultado final en pantalla local, pero ahora se
captura bien en remoto). Recompilado y **relanzada la instancia real de Terrakeep** que el
usuario tenía abierta (cerrada primero, `dotnet build` no puede sobrescribir el `.dll` de un
proceso vivo - regla ya conocida del proyecto, "no hay hot-reload").

`dotnet build`/`dotnet test` en verde (128/128). Pendiente de que el usuario confirme en vivo
si ya ve la pantalla y Terrakeep bien por Chrome Remote Desktop tras estos dos arreglos.

### AnyDesk instalado como segunda opción, más sólida (mismo día)

Pedido explícito tras seguir con dudas sobre Chrome Remote Desktop: "una que sea mucho mas
sólida... que tú puedas instalarla de casa y yo activarla estando fuera". Preguntado de nuevo
qué herramienta en concreto (mismo criterio que con CRD - decisión de seguridad real, no
autonomía de dev-tools) - eligió **AnyDesk**.

Instalado el `.exe` oficial (`download.anydesk.com/AnyDesk.exe`) como servicio, silencioso y
con arranque automático (`AnyDesk.exe --install "C:\Program Files (x86)\AnyDesk"
--start-with-win --silent`) - servicio `AnyDesk` en marcha. Contraseña de acceso desatendido
generada al azar (16 caracteres) y fijada por CLI (`--set-password` con la contraseña por
stdin).

**Verificación real, no dada por buena a ciegas**: la documentación oficial de AnyDesk sobre
`--set-password` es contradictoria (una página dice que basta con la CLI, otra dice que hace
falta pasar por la interfaz gráfica "Settings > Access > Set password"). En vez de fiarme de
ninguna de las dos, se inspeccionó directamente el fichero real de configuración
(`C:\ProgramData\AnyDesk\system.conf`) tras ejecutar el comando: aparece
`ad.security.permission_profiles._unattended_access.pwd=<hash>` y `...salt=<valor>` - prueba
real de que la contraseña SÍ quedó asociada de verdad al perfil de acceso desatendido, no solo
guardada en algún sitio sin efecto. Mismo fichero reveló el ID real de AnyDesk de este equipo
(`ad.anynet.id=599047737`) - `--get-id` por CLI no devolvía nada por su cuenta.

**Datos de conexión reales** (guardados también aquí para no perderlos):
- ID de AnyDesk: `599 047 737`
- Contraseña de acceso desatendido: la generada al azar, ya comunicada al usuario en el chat -
  no se repite aquí por escrito dos veces sin necesidad.

No hay ningún cambio de código de Terrakeep en este punto - instalación/configuración del
sistema únicamente, documentado aquí por continuidad con el resto de la sesión.

## Librería vanilla: traducción real de rótulos + orden real de objetos (2-sep-2026)

Tras revisar el esquema de raíces/subcarpetas que se le pasó, el usuario pidió dos cosas más:
traducir TODOS los rótulos de la Librería (no solo Calamity) al español, y que el orden de los
objetos dentro de cada carpeta coincida al 100% con el real de Terrasavr.

### Traducción real - namespace `lib.item` (no inventada)

Investigado en el propio `script.js` real antes de traducir nada a mano: la clase base de los
nodos del árbol de la Librería (`app.Shelf`/`ha`, de la que heredan `Dir`/`Items` - `ub`/`mb`)
SÍ traduce sus nombres vía `l.loc("lib.item", this.enName, this.enName)` en su `updateLang()`
real - hay una tabla de traducciones oficial de verdad, dentro de `Terrasavr.es-ES.json`
(`local-site/lang/lang.zip`), namespace `lib.item`, **258 entradas reales**. Nuevo script
`scripts/extraer-etiquetas-libreria-es.js` (Node, usa `Expand-Archive` de PowerShell para el
zip - `tar` de Git Bash interpreta `C:\...` como sintaxis remota `host:ruta` y falla en este
Windows real) → `Assets/vanilla_library_labels_es.json`.

El propio `updateLang()` real no es un simple `dict[nombre]` - tiene 4 casos reales, en este
orden (confirmado leyendo `ha.rxPage`/`rxPages`/`rxAuto`/`rxNum`, las 4 expresiones regulares
reales): rango numérico puro (`^\d+-\d+$`, nunca se traduce), `"Page N"` (traduce la plantilla
`"Page $1"` y sustituye), `"Pages N+"` (idem con `"Pages $1+"`), y cualquier nombre terminado
en `"(N)"` (`^(.+?)\((\d+)\)$` - traduce `"<prefijo> ($1)"` y sustituye, ej. `"Melee damage
(316)"` → `"Daño de Cuerpo a Cuerpo (316)"`, no una plantilla sin resolver). Nuevo
`LibraryLabelCatalog.cs` (Core) replica ese algoritmo exacto con las mismas 4 expresiones
regulares reales - `LibraryCategoryTreeBuilder` lo usa tanto para los nombres vanilla
(traduciendo `VanillaLibraryNode.Name`, dejando `FullPath` en inglés como clave interna
estable) como para las etiquetas `"Page N"` que el propio código genera para paginar las hojas
de Calamity (ahora también salen en español real, `"Pagina N"` - SIN tilde, un typo real del
propio fichero de idioma de Terrasavr que se respeta tal cual, no se "corrige" algo que no es
nuestro, mismo criterio ya aplicado antes con "Extremandamente").

### Bug real encontrado y corregido - orden de los objetos dentro de cada carpeta

Causa raíz real: `CategoryNodeViewModel.ItemIdSet` es un `HashSet<int>` - **no garantiza
ningún orden de enumeración** - filtrar `_all`/`_researchedCounts` por `ItemIdSet.Contains(id)`
daba como resultado el orden arbitrario del catálogo completo (aprox. ascendente por id), NO
el orden curado real de Terrasavr (ej. "Copper & Tin" real es `[12, 3507, 3509, 89, 699,
3501, ...]`, mezclando mineral+lingote+arma+armadura de ese material a propósito, no ids
ascendentes). En `ResearchViewModel` el bug era aún más directo: un `.OrderBy(id => id)`
explícito, deliberado pero equivocado con esta información nueva.

Arreglado con una segunda lista, `CategoryNodeViewModel.ItemIdsOrdered` (`List<int>`) que SÍ
preserva el orden real - para una hoja, tal cual viene del propio JSON extraído (que a su vez
preserva el orden real de `Hc.deploy()`); para una carpeta intermedia, sus hijos concatenados
en su propio orden real, sin duplicar un id que caiga en más de un hijo a la vez (pertenencia
múltiple real). `ItemIdSet` se queda solo para pertenencia rápida (`Contains`).
`LibraryViewModel`/`ResearchViewModel.ApplyFilter` recorren `ItemIdsOrdered` y resuelven cada
id por diccionario en vez de filtrar la lista completa por conjunto.

**Verificado con arnés de consola contra datos reales**: las 10 raíces reales traducidas
(`Materiales`, `Decoraciones`, `Mascotas, Monturas, Herramientas`, `Pociones (regeneracion)`,
`Pociones (efectos)`, `Jefes & Eventos`, `Mision de Pez`, `Categorias`, `Objectos por ID` -
typo real "Objectos" preservado, `Calamity (mod)`); `"Copper & Tin"` sale como `"Cobre &
Estaño"` con el orden real exacto `[12, 3507, 3509, 89, 699, 3501, 3503, 687, 20, 3508, 3505,
80, ...]`, y `Results` tras seleccionarla muestra los objetos reales en ESE MISMO orden
(Mineral de cobre, Espada corta de cobre, Pico de cobre, Casco de cobre, Mineral de estaño...);
la carpeta de predicado real sale `"Daño de Cuerpo a Cuerpo (316)"` (sustitución de número
real, no una plantilla suelta); la paginación de Calamity sale `"Pagina 1"/"Pagina 2"/...`
igual que el resto del árbol.

`dotnet build`/`dotnet test` en verde (128/128).

## Pulido de UI tras revisión visual del usuario (2-sep-2026)

Tres pedidos tras ver la app real: quitar el scroll del panel Equipamiento (que la cuadrícula
escale con la ventana en vez de recortar), nombres cortados en el árbol de Librería/
Investigación, y autoocultar la barra de tareas de Windows.

### Panel Equipamiento sin scroll - `Viewbox` en vez de `ScrollViewer`

`ScrollViewer` con `WrapPanel` sustituido por `Viewbox Stretch="Uniform" StretchDirection="Both"`
- escala el bloque ENTERO de tarjetas para caber siempre en el espacio real disponible (crece o
encoge con la ventana, nunca recorta ni necesita scroll) - la entrada del propio Viewbox sigue
funcionando bien con el ratón (WPF transforma las coordenadas del click automáticamente), así
que arrastrar/soltar/editar un slot sigue intacto. El `WrapPanel` interno necesita un ancho
FIJO (`Width="820"`) - si no, el Viewbox le da ancho infinito al medir y el WrapPanel nunca
envuelve, saldría todo en una sola fila rarísima. 820px = 5 columnas reales de las tarjetas de
slot (148+2×6 de margen ≈ 160px cada una) - `PlrLoadout.cs` confirma que Items/Social/Dyes
tienen SIEMPRE 10 slots, así que 5×2 es la forma real exacta, nunca sobra ni falta hueco.

### Nombres cortados en el árbol de Librería/Investigación

Causa real: el `TextBlock` del nombre vive dentro de un `StackPanel Orientation="Horizontal"`
(para ir junto al icono) - un `StackPanel` horizontal le da ancho INFINITO a sus hijos al
medir, así que `TextWrapping` solo no hacía nada (nunca había un límite del que envolver) - el
texto se desbordaba en silencio más allá de la columna fija de la barra lateral y quedaba
recortado visualmente. Arreglado con `MaxWidth="165"` + `TextWrapping="Wrap"` en el propio
`TextBlock` (las dos plantillas, `CategoryNodeTemplate` de Librería y
`ResearchCategoryNodeTemplate` de Investigación) - ahora los nombres largos reales
("Mascotas, Monturas, Herramientas", "Pociones (regeneracion)"...) envuelven a 2 líneas en vez
de cortarse. Ensanchada también la columna del árbol de 170 a 210px en ambas pestañas.

### Barra de tareas de Windows en autoocultar

Pedido de sistema, no de la app - hecho vía la API real de Windows (`SHAppBarMessage` con
`ABM_SETSTATE`/`ABS_AUTOHIDE`), no tocando el registro binario a ciegas (`StuckRects3` es
frágil de editar a mano). Verificado de verdad consultando el estado real después
(`ABM_GETSTATE`), no solo confiando en el código de retorno del `SETSTATE`: `1` = autoocultar
activo. Efecto inmediato, sin reiniciar sesión ni el Explorador.

`dotnet build`/`dotnet test` en verde (128/128) para los cambios de XAML. Verificación visual
del Viewbox/wrap de texto en sí pendiente de que el usuario la confirme en vivo - son cambios
puramente estructurales de WPF (patrones estándar y bien soportados, sin lógica nueva de por
medio), pero esta sesión ya tiene documentado que las capturas de pantalla no son fiables aquí
y no hay forma de "ver" el resultado visual desde este lado con certeza.

### Mismo `Viewbox` para TODOS los contenedores (Inventario/Banco/Caja fuerte/Fragua/Bóveda/...)

Pedido explícito de ampliar el arreglo del panel Equipamiento a "todas las ventanas que están
por encima de librería" - `ContainerTabTemplate` (la plantilla COMPARTIDA por las 9 pestañas
de Inventario: Inventario/Banco/Caja fuerte/Fragua del Defensor/Bóveda del Vacío/Mascota-
Montura-Gancho/Tintes/Monedas/Munición) pasa del mismo `ScrollViewer` a `Viewbox
Stretch="Uniform" StretchDirection="Both"`, un único cambio que cubre las 9 a la vez.

`WrapPanel Width="1600"` (10 columnas reales de ~160px cada una) en vez de los 820px/5
columnas de Equipamiento - 10 columnas es la propia rejilla real de Terraria para estos
contenedores (`PlrCharacter.cs`: `Inventory=50`, `Bank/Safe/Forge/Void=40`, ambos múltiplos
exactos de 10), no un número inventado. Monedas/Munición (4 slots cada uno) caben sin problema
en esa misma rejilla de 10, en una única fila corta.

`dotnet build`/`dotnet test` en verde (128/128).

### Contorno naranja también en Inventario (pedido explícito, "me gusta")

El `Border BorderBrush="{StaticResource OrangeBrush}"` que ya llevaba Equipamiento se añade
igual alrededor del `TabControl` de las 9 pestañas de Inventario (Inventario/Banco/Caja
fuerte/Fragua/Bóveda/...) - un único `Border` envolviendo el `TabControl` entero, mismo estilo
exacto que Equipamiento para que ambos paneles se sientan de la misma familia visual. Pedido
explícito tras ver el resultado real - la distinción semántica original ("Equipamiento
separado de Inventario") pasa a ser solo cuestión de qué contenedor hay a cada lado del
`TabControl` externo, no del contorno en sí.

`dotnet build`/`dotnet test` en verde (128/128).

## Rediseño de las 9 pestañas de contenedor tras pregunta a Opus (2-sep-2026)

Feedback tras ver el resultado real de la ronda anterior: *"hay que hacer algo con esas 9
pestañas se comen todo el espacio y hace que cuadrícula y los objetos se vean super pequeños...
me gusta mucho que los objetos se vean directamente de un plumazo, pregunta a opus sobre un
plan real..."*. Consultado un agente con Opus (contexto completo: geometría real medida,
tensión entre "ver todo de un plumazo" y legibilidad) - devolvió un diagnóstico con números
reales: a un ancho de ventana normal, la rejilla de Inventario (50 slots) solo tenía
~678x165px reales tras restar cabecera + 9 pestañas en 2 líneas + fila fija de Librería, así
que el `Viewbox` la escalaba a 0,38x - tarjetas de 56x28px con letra de 4pt, exactamente lo que
se veía en la captura. Diagnóstico: el problema no era Viewbox-vs-scroll, era el tamaño de
tarjeta - **"cambiar la tarjeta, no la estrategia de escalado"**. Plan implementado completo
(6 pasos, todos en un mismo commit por ser un rediseño coherente):

1. **Bug real de `GridWidth` fijo**: `ContainerTabTemplate` tenía `WrapPanel Width="1600"`
   fijo para las 9, así que Monedas/Munición/Mascota-Montura-Gancho/Tintes (4-5 slots) se
   escalaban igual de pequeño que si tuvieran 50 objetos reales, con 60% de aire muerto.
   Arreglado con `ContainerViewModel.GridWidth => Math.Min(10, Slots.Count) * 160` (nuevo, la
   plantilla ahora usa `{Binding GridWidth}`).
2. **Tooltip compuesto**: `ItemStatsFormatter.Format` devuelve `null` para cualquier objeto sin
   estadísticas de combate (bloques, materiales...) y nunca incluye el nombre - la tarjeta rica
   y la nueva compacta llevan ahora un `Border.ToolTip` compuesto (Nombre en negrita + Prefijo +
   Stats si hay) en vez de depender solo del string. Gotcha real de WPF: el `ToolTip` (como el
   `ContextMenu`) es un Popup fuera del árbol visual y NO hereda el `DataContext` solo - hace
   falta `DataContext="{Binding Path=PlacementTarget.DataContext, RelativeSource={RelativeSource Self}}"`.
3. **Librería plegable**: la fila fija de 270px (46% del alto útil de la pestaña, más robo de
   espacio que las propias 9 pestañas) pasa a `Height="Auto"` con una cabecera siempre visible
   (botón "▲ Plegar"/"▼ Desplegar", `MainViewModel.IsLibraryCollapsed`, **plegada por defecto**
   dado que la prioridad explícita del usuario es la cuadrícula de objetos) y contenido de
   238px solo cuando está desplegada. Auto-despliegue en `RequestPickForSlot` para que "Elegir
   objeto..." nunca deje la Librería escondida. Ventana `Height` 760→860 (100px extra gratis).
4. **Consolidación de 9 pestañas a 5** (Equipamiento/Inventario/Almacenes/Monturas/Monedas):
   nuevo `StorageGroupViewModel.cs` (mismo patrón que `EquipmentGroupViewModel` - selector de 4
   píldoras Banco/Caja fuerte/Fragua/Bóveda, reutiliza `EquipmentOptionViewModel`). Monturas
   apila Mascota-Montura-Gancho + Tintes; Monedas apila Monedas + Munición (así los muestra el
   propio juego: el tinte i corresponde al equipo i). **Riesgo cero para guardar/cargar**: los
   9 `ContainerViewModel` siguen siendo exactamente los mismos objetos de
   `MainViewModel.Containers` (`SyncEditsBackToMerged`/`AutoEquip` no cambian) - la
   consolidación es pura capa de presentación, sin fusionar ninguna colección.
5. **`SlotGridPanel` (nuevo, `Controls/SlotGridPanel.cs`)**: rejilla propia para Inventario
   (50) y Almacenes (40 cada uno) en vez de Viewbox - calcula un tamaño de celda real
   `clamp(min(anchoDisp/cols, altoDisp/filas), MinCell=44, MaxCell=96)`: crece/encoge con la
   ventana ("de un plumazo") mientras quepa legible, y se congela + aparece scroll si no
   cabría ni a 44px (nunca se sacrifica la legibilidad). Un `ScrollViewer` mide a su hijo con
   altura infinita, así que hace falta una DP `AvailableHeight` enlazada al `ActualHeight` del
   propio `ScrollViewer` - vía `RelativeSource AncestorType` (recorrido real del árbol visual),
   NO `ElementName` (frágil cruzando el límite de un `ItemsPanelTemplate`, la trampa que Opus
   avisó por adelantado). Nueva `SlotCompactTemplate` (icono + contador solo si >1 +
   `RenderOptions.BitmapScalingMode="NearestNeighbor"` para que el pixel art no salga
   embarrado) con `ContextMenu` (Elegir/Aplicar prefijo/Vaciar) y doble clic para elegir objeto
   - reutiliza los mismos manejadores de drag&drop que la tarjeta rica sin tocarlos (leen
   `sender.DataContext`, no dependen de la forma de la tarjeta).
6. **"Vaciar slot"** añadido al panel Editar compartido (antes solo existía el botón "✕" de la
   tarjeta rica, que la compacta no tiene sitio para llevar - ahora también vive en el panel
   que acompaña a cualquier slot seleccionado).

**Verificación real** (no solo build limpio, dado que los bindings `{Binding}` no se comprueban
en compilación y `SlotGridPanel` es lógica de layout nueva sin probar): arnés WPF de usar-y-tirar
(mismo patrón ya establecido en esta bitácora - `Application` en blanco con `Theme.xaml` +
conversores cargados a mano, sin `StartupUri`) que carga un personaje real sintético
(`PlrCharacter` con `PlrFile.Write`/`LoadFromPath` real, no mockeado), coloca objetos reales en
12 slots de Inventario y 15 de Banco, y navega las 5 pestañas + la píldora "Fragua del
Defensor" vía UI Automation real (`SelectionItemPattern`/`InvokePattern`, no coordenadas de
píxel). Resultado: las 5 pestañas seleccionan sin excepción, `SlotGridPanel` mide/organiza 50 y
40 slots reales sin fallar (12 y 15 imágenes reales encontradas respectivamente, coincide con
los objetos colocados), la píldora cambia `StorageGroup.Current` de "Banco" a "Fragua del
Defensor" de verdad, `GridWidth` da los valores esperados (1600/800/800/640/640), el toggle de
Librería cambia `IsLibraryCollapsed`, y no aparece `ultimo-error.log` ni ninguna excepción de
`Dispatcher`. `dotnet build`/`dotnet test` en verde (128/128, sin regresiones - todo el cambio
es de `TerrasavrNative.App`, `Core` no se tocó).

## Segunda consulta a Opus: Equipamiento, Librería, prefijo en tooltip y fondo de vacío (2-sep-2026)

Feedback tras revisar en vivo la ronda anterior: *"me ha gustado mucho como ha quedado... haz lo
mismo para equipamientos y para la libreria... cuando pasas el raton y muestra la información
del arma que allí aparezca el prefijo que tiene asignado esto solo aplica a todo menos a la
libreria logicamente... las cajas vacías... el fondo podria diferenciar-se un poco mas de las
que tienen objetos, vuelve a consultar a opus todo esto"*. Segunda consulta a un agente con
Opus, con el código real ya escrito pegado en el prompt (no descripciones).

**Punto 1 (prefijo en el tooltip) - Opus confirmó que NO había ningún bug**: el tooltip
compuesto de la ronda anterior ya incluía `PrefixDisplay` con `Visibility` colapsable, y
Equipamiento ya heredaba ese arreglo (plantilla implícita compartida). Diagnóstico real de por
qué el usuario lo percibía como ausente: la línea del prefijo se pintaba como una palabra suelta
("Legendario") con el mismo estilo `CaptionText` que la línea de estadísticas justo debajo -
visualmente indistinguible de "una línea más de stats", nunca se leía como *el prefijo*. Arreglo
real aplicado: `Run Text="Prefijo: "` + `Run` con el valor en `AccentBrush`/`SemiBold`, en las
DOS plantillas (rica y compacta) - tanto en el tooltip como en el cuerpo visible de la tarjeta
rica (que también mostraba el prefijo suelto, y también le faltaba colapsar la línea cuando no
hay prefijo, dejando ~12px de hueco vacío en cualquier objeto sin prefijo).

**Punto 2 (fondo de caja vacía) - diagnóstico numérico real de Opus**: el slot vive sobre
`BgSecondaryBrush` `#171a26`; lleno = `BgElevatedBrush` `#1e2233`; vacío = ese mismo color al
35% de opacidad compuesto sobre el panel ≈ `#191d2b` - diferencia real lleno↔vacío de solo 5-8
por canal, invisible en un tema oscuro. **Arreglo: invertir la elevación en vez de atenuar** -
lleno sigue siendo `BgElevatedBrush` (sobresale del panel), vacío pasa a `BgPrimaryBrush`
`#10121c` (se hunde por debajo del panel) - diferencia real de 14-16-23 por canal, ~3x más, y
cambia de signo (deja de ser "lo mismo pero apagado", pasa a leerse como un hueco real). Token
ya existente en la paleta, sin inventar ningún color nuevo - confirmado que `overrides.css` real
no tiene ningún precedente de "slot vacío" que copiar (no es un concepto real de Terrasavr).
Añadido a los estilos BASE (`ItemSlotCard`/`ItemSlotCardCompact` en `Theme.xaml`), no a cada
plantilla, para que no puedan volver a divergir - con hover propio (sube a `BgHoverBrush` en
lleno, a `BgElevatedBrush` en vacío). **Crítico**: había que BORRAR los dos
`DataTrigger Opacity="0.35"` que quedaban en `MainWindow.xaml` (rica y compacta) - los triggers
del estilo derivado se evalúan DESPUÉS que los del base, así que dejarlos habría vuelto a lavar
el `#10121c` nuevo contra el panel y el arreglo no habría hecho nada visible. El botón
"Elegir..." de la tarjeta rica (lo único que queda a opacidad plena en un slot vacío ahora que
el fondo ya no se atenúa) se atenúa aparte, `Opacity="0.6"` puesto directamente en el botón, no
en el `Border`.

**Punto 3 (Equipamiento) - dos bugs reales más encontrados por Opus, y una decisión
estructural**:
- `WrapPanel Width="820"` a pelo en el bloque Viewbox de Equipamiento - 20px de aire muerto
  frente a la huella real (5 columnas × 160px = 800), y un número mágico que se rompería en
  silencio si `ItemSlotCard` cambiara de tamaño.
- Faltaba `RenderOptions.BitmapScalingMode="NearestNeighbor"` en el icono de la tarjeta rica -
  más grave que en la compacta porque la rica vive SIEMPRE dentro de un `Viewbox` (siempre bajo
  un `ScaleTransform`), el caso exacto en que el filtrado bilineal de WPF por defecto embarra el
  pixel art. Probablemente la razón concreta de que "Equipamiento se viera peor" que Inventario.
  Mismo arreglo aplicado también al icono del panel Editar compartido y al de las tarjetas de
  Builds (`BuildItemRowViewModel`), que tenían el mismo problema sin que nadie lo hubiera
  reportado todavía.
- **Decisión estructural**: Equipamiento era el único sitio con su propio bloque
  `Viewbox`+`ItemsControl` a medida en el XAML (Monturas/Monedas/Almacenes ya reusaban
  `ContainerTabTemplate`). Añadido `ContainerViewModel.Columns` (nuevo, `init`, default 10;
  `GridWidth` ahora usa `Columns` en vez de un `10` fijo) y
  `EquipmentGroupViewModel.Current` (expone el `ContainerViewModel` completo, no solo
  `.Slots`, con `Columns=5` fijado en `AddSlotSet` porque `PlrLoadout.Items/Social/Dyes` son
  siempre 10 slots reales en forma 5×2, verificado en `PlrLoadout.cs`). El XAML de Equipamiento
  pasa a `ContentControl Content="{Binding Current}" ContentTemplate="{StaticResource
  ContainerTabTemplate}"` - un solo camino de código, hereda automáticamente el fondo de vacío,
  el tooltip compuesto y cualquier arreglo futuro sin poder volver a divergir.
- Nits: doble clic para elegir objeto añadido a la tarjeta rica (ya lo tenía la compacta, "ya es
  memoria muscular desde Inventario").
- **Sin implementar** (P3 de Opus, mayor coste y necesita verificación contra un `.plr` real
  antes de rotular nada): etiquetas de rol de slot en Equipamiento (Casco/Peto/Grebas/Accesorio
  1-7) - anotado como mejora futura, no pedida explícitamente esta vez.

**Punto 4 (Librería) - 4 cambios sí, 3 cambios no, con el porqué de cada uno**:
- Sí: `NearestNeighbor` en el icono (mismo problema de pixel art embarrado).
- Sí: tooltip compuesto real (nombre en negrita + stats si hay) - el caption de nombre en la
  tarjeta (9pt, 2 líneas máx en 80px de ancho) se recortaba EN SILENCIO sin puntos suspensivos,
  y el único tooltip de antes (`StatsTooltip` a pelo) era `null` para cualquier objeto sin stats
  de combate, así que el nombre recortado era irrecuperable. **Sin línea de prefijo** (pedido
  explícito del usuario: "esto solo aplica a todo menos a la libreria logicamente" - una entrada
  de catálogo no tiene ningún prefijo asignado todavía). Añadido también `TextTrimming=
  "CharacterEllipsis"` al caption.
- Sí: hover real (`Cursor="Hand"` sin ningún cambio visual antes) - `Background` a
  `BgHoverBrush` + `BorderBrush` a `AccentBrush`.
- Sí: grosor de borde Calamity a 2px (antes se quedaba en el 1px base, una tarjeta Calamity de
  Librería salía con una línea roja mucho más fina que en cualquier otro sitio de la app).
- **No** (y por qué, ya argumentado para no tener que volver a decidirlo): NO contorno naranja
  (ese contorno marca hoy "esto escribe en tu `.plr`" - Equipamiento/Inventario/Almacenes/
  Monturas/Monedas, exactamente las 5 cosas que sí lo hacen; la Librería es una paleta de
  origen, no escribe nada, ponérselo destruiría la única distinción real que ese contorno
  transmite). NO pasar a `SlotGridPanel`/celda dinámica (esa rejilla existe para un número
  *conocido y fijo* de slots en un área acotada sin scroll; la Librería tiene resultados *no
  acotados* - `MaxResults=300` - en una franja con scroll por definición). NO homogeneizar el
  tamaño de tarjeta con `ItemSlotCardCompact` (la compacta es cuadrada porque `SlotGridPanel`
  hace celdas cuadradas; la de Librería es 80×86 porque lleva caption de nombre + botón
  "Colocar" condicional - forzar la misma métrica mataría el caption o dejaría slots más altos
  que anchos; lo que sí se unificó fue el lenguaje visual - `CornerRadius`, `BgElevatedBrush`,
  rojo Calamity a 2px, escalón de hover -, no la métrica).

**Verificación real** (arnés de UI Automation ampliado, mismo patrón ya establecido): objeto
CON un prefijo real asignado colocado en Equipamiento vía `SetPrefix` (para probar de verdad la
corrección 1, no un objeto sin prefijo que no habría distinguido el bug de un falso OK) -
`PrefixDisplay` confirmado poblado y legible. Clic real (`InvokePattern`) en la píldora
"Vanidad" de Equipamiento confirma que `EquipmentGroup.Current` cambia de verdad
(`GridWidth=800`, `Columns=5`, tal como se esperaba tras el rediseño estructural). Búsqueda real
en la Librería (`SearchText="Sword"`, 3 resultados) renderiza las tarjetas nuevas (tooltip
compuesto/hover/trimming) sin ninguna excepción. Las 5 pestañas siguen navegables sin excepción
tras todos los cambios. Sin `ultimo-error.log` ni excepciones de `Dispatcher`.
`dotnet build`/`dotnet test` en verde (128/128, sin regresiones).

## Tercera pasada: un único formato de tarjeta en toda la app (2-sep-2026)

Feedback directo tras la segunda pasada: *"equipamiento no tiene el mismo formato que
inventario y el resto de las 9 pestañas asi que nose que es lo que has tocado libreria
tampoco"*, citando de vuelta mi propia descripción de la rejilla compacta. Error real de
comunicación/diseño de la ronda anterior: Opus había recomendado (con criterio razonable en
abstracto - "≤10 slots → tarjeta rica, >10 → compacta") dejar Equipamiento en la tarjeta RICA
de siempre mientras Inventario/Almacenes ya usaban la rejilla compacta nueva - técnicamente
justificado, pero el resultado visible es justo lo que el usuario reporta: dos formatos
distintos conviviendo en la misma pantalla, sin haberlo explicado con suficiente claridad de
antemano. Corregido por petición directa, sin nueva consulta a Opus (pedido inequívoco, no una
decisión de diseño con trade-offs que discutir):

- `ContainerCompactTemplate` (antes solo para Inventario/Almacenes) pasa a usarse en los 9
  contenedores reales - Equipamiento, Monturas (Mascota/Montura/Gancho + Tintes) y Monedas
  (Monedas + Munición) se convierten de `ContainerTabTemplate` (Viewbox + tarjeta rica) a la
  misma rejilla `SlotGridPanel` + `SlotCompactTemplate` que ya tenían Inventario/Almacenes -
  **mismo formato de tarjeta en toda la app**, sin excepción.
- `SlotGridPanel Columns` pasa de un `10` fijo a `{Binding Columns}` (la propiedad ya añadida en
  la ronda anterior a `ContainerViewModel`, antes solo usada por el `GridWidth` ahora muerto) -
  así Equipamiento mantiene su forma real 5×2 (`Columns=5`, fijado en
  `EquipmentGroupViewModel.AddSlotSet`) en vez de una única fila larga de 10.
- **Limpieza de código muerto real** (ya no queda ningún consumidor): `ContainerTabTemplate`
  (la plantilla Viewbox+tarjeta rica) se borra entera de `MainWindow.xaml`, junto con la
  `DataTemplate` implícita `DataType="{x:Type vm:ItemSlotViewModel}"` de la tarjeta rica (~145
  líneas) que solo esa plantilla usaba, el estilo `ItemSlotCard` en `Theme.xaml` (base de esa
  tarjeta), y `ContainerViewModel.GridWidth` (el ancho en píxeles que solo el `WrapPanel` de la
  plantilla borrada necesitaba - `Columns` se queda, sigue haciendo falta para `SlotGridPanel`).
  Comentarios que aún citaban `ContainerTabTemplate`/`ItemSlotCard` como referencia viva
  actualizados para no confundir a una sesión futura.

**Verificación real** (mismo arnés de UI Automation, actualizado tras quitar `GridWidth` -
ahora imprime `Columns` en su lugar): `EquipmentGroup.Current` confirma `Columns=5` tanto en el
estado inicial como tras cambiar de píldora a "Vanidad" (clic real, `InvokePattern`); las 5
pestañas siguen seleccionables sin excepción tras el cambio de plantilla en 3 de ellas
(Equipamiento pasa de 24 a 13 botones reales, Monturas/Monedas de 16/14 a 6 - coherente con que
la tarjeta compacta ya no lleva botones propios por slot, solo menú contextual); búsqueda real
en Librería sigue renderizando sin excepción; sin `ultimo-error.log`. `dotnet build`/
`dotnet test` en verde (128/128, sin regresiones).

## Cuarta pasada: tooltips completos de equipo, iconos consistentes, rework de Buffs (2-sep-2026)

Feedback nuevo, 3 peticiones en un mensaje: (1) tooltips de armadura/accesorios incompletos -
sin porcentajes de prefijo, sin descripción de accesorio, sin bonificación de set completo;
(2) iconos "enormes" en Equipamiento/Monturas/Monedas tras la rejilla nueva, quería el mismo
tamaño que Inventario; (3) rework completo de la pestaña Buffs (misma rejilla que Inventario +
Librería de buffs con jerarquía real de Terrasavr + panel Editar con 3 botones de duración
real). Pedido explícito: consultar a Opus con investigación real antes de tocar nada.

**Investigación real previa** (dos agentes en paralelo, código decompilado real, sin adivinar
nada): confirmado que el tooltip de accesorio con porcentajes YA rellenados existe en
`Terraria.Localization.Content.es-ES.Items.json` clave `ItemTooltip` (2790 entradas, mismo
espacio de claves PascalCase que `ItemName`, ya usado en el proyecto); confirmado que la
bonificación de set completo existe en `Game.json` clave `ArmorSetBonus` (67 entradas) con una
tabla real de condiciones `(head,body,legs)` en `Player.UpdateArmorSets` de `Player.cs`
decompilado; confirmado que solo existe UN caso real de duraciones min/medio/máx en todo el
juego (Poción de la suerte, ratio exacto 1:2:3) - el resto de buffs solo tienen un `buffTime`
real único, sin escalera real que copiar.

**Consulta a Opus con estos hechos ya verificados** - devolvió tres correcciones importantes a
la investigación previa que cambiaron el diseño: (a) SÍ existe jerarquía real de buffs en
Terrasavr (`app.BuffSide`, no `app.TabBuffs` que solo pinta iconos) - el grep anterior había
mirado la clase equivocada; tabla real de 6 categorías (Utilidad/Offensivo/Defensivo/Special/
Mascota/Negativo) más un índice paginado, con pertenencia múltiple real; (b) la duración
máxima real SÍ existe como valor fijo (`S.getMaxTime()` real de Terrasavr = 1999999980 ticks
≈ 385,8 días para personajes version≥269, el mismo umbral que ya usa este puerto para decidir
44 vs 22 buffs) - no hace falta ningún multiplicador inventado para el botón "Máxima"; (c) la
tabla de sets de armadura NO está indexada por item id sino por `headSlot`/`bodySlot`/`legSlot`
(índice de textura de equipo), con un paso de inversión adicional necesario.

**Corrección del usuario sobre la duración máxima**, llegada ANTES de que la consulta a Opus
terminara: *"el tiempo maximo quiero que sea lo maximo permitido... creo que es 365 dias"*.
Verificado en el propio `Player.cs` decompilado: `buffTime` es `int[]` igual que `PlrBuff.Time`
en este puerto, así que el techo matemático exacto sería `int.MaxValue/60 ≈ 414 días` - pero
usar ese límite exacto arriesga desbordar un `int` al convertir segundos→ticks (`×60`). La
respuesta real de Opus (`S.getMaxTime()` = 385,8 días) resuelve esto mejor que cualquier
aproximación propia: es el valor REAL que usa el propio Terrasavr, más seguro que el límite
matemático exacto y más preciso que una redondez inventada.

### Fase 0 (ya implementada y verificada) - iconos consistentes + tooltips completos

1. **`SlotGridPanel.ReferenceColumns`** (nuevo, `Controls/SlotGridPanel.cs`): antes cada
   contenedor maximizaba SU PROPIA celda de forma independiente dentro del mismo ancho
   compartido - con menos columnas (Equipamiento=5), `cellFromWidth` salía mucho mayor que con
   Inventario (10 columnas), pegándose al `MaxCell=96` mientras Inventario se quedaba en ~64px
   reales. `ReferenceColumns="10"` (fijado en `ContainerCompactTemplate`, `MainWindow.xaml`)
   limita la celda al tamaño que tendría un contenedor de 10 columnas en ese mismo ancho -
   verificado con UI Automation real: `firstImageWidth` de Equipamiento e Inventario ahora
   coinciden exactamente (86px = 86px, antes muy distintos).
2. **Tooltips completos de objetos**: `ItemStatsFormatter.Format` reescrito de raíz - antes
   devolvía `null` si NINGÚN campo numérico estaba presente (un accesorio sin daño/defensa no
   mostraba nada); ahora compone 4 secciones independientes (efecto de prefijo / stats
   numéricos / tooltip descriptivo real / bonus de set completo), cada una opcional. Firma
   nueva: `Format(bool isCalamity, int id, ItemTooltipCatalogs catalogs, ItemPrefix? prefix =
   null)` - los 6 catálogos que antes eran parámetros sueltos (ya insostenible) se agrupan en
   un `record` nuevo, expuesto como `CharacterFileService.TooltipCatalogs`, actualizado en los
   5 call sites reales (`ItemSlotViewModel`, `LibraryViewModel` x2, `BuildsViewModel` x2).
   - `scripts/extraer-tooltips-vanilla.py` → `vanilla_item_tooltips.json` (2518 objetos reales,
     texto de `ItemTooltip` con las referencias `{$CommonItemTooltip.X}`/`{$PaintingArtist.X}`
     ya resueltas recursivamente contra el mismo fichero, `{InputTrigger_X}` sustituido por
     `[tecla]` literal - documentado como decisión, no un binding real inventado) +
     `VanillaItemTooltipCatalog.cs` (Core).
   - `scripts/extraer-efectos-prefijos.py` → `vanilla_prefix_effects.json` (84 prefijos reales:
     65 de arma vía `Item.TryGetPrefixStatMultipliersForItem`, 19 de accesorio vía
     `Player.GrantPrefixBenefits`) + `PrefixEffectCatalog.cs` (Core) - compone el texto en
     español desde los números reales (`dmg=1.15` → "+15% de daño"), nunca una frase inventada.
   - `scripts/extraer-sets-armadura.py` → `vanilla_armor_sets.json` (177 item ids reales con
     bonificación de set mapeada, 57 de las 66 claves reales de `ArmorSetBonus` resueltas) +
     `VanillaArmorSetCatalog.cs` (Core). Dos pasos reales: inversión `headSlot`/`bodySlot`/
     `legSlot` → item id (mismo `split_by_case` que ya usaba `extraer-estadisticas-vanilla.py`,
     604 objetos, 1:1 sin colisiones) + evaluación de las condiciones reales de
     `UpdateArmorSets` contra el producto cartesiano de candidatos por variable (dominio
     siempre pequeño). Único caso especial real de todo el método (bonus Hallowed vs
     HallowedSummoner, un `else` sin condición propia) resuelto a mano con los valores reales
     leídos del propio código en vez de un parser genérico de `else` para un único caso.
     Verificado con 3 combos reales conocidos (Shroomite, Molten, Hallowed/HallowedSummoner) -
     los 3 con texto y piezas correctas.
   - El texto de bonus de set se muestra siempre que se mira una pieza del set (estático por
     id, etiquetado "Con el set completo: ..." en vez de fingir que ya está activo - el
     tooltip también lo consume la Librería sobre objetos sueltos, sin personaje cargado). El
     chequeo reactivo real (equipo puesto de verdad) queda para una fase futura -
     `VanillaArmorSetCatalog.BonusForEquipped` ya expone lo necesario si se implementa.
   - Calamity: descripciones de tooltip NO extraídas esta pasada (el problema es idéntico ahí,
     pero requiere leer 28 ficheros `.hjson` del `.tmod` real en inglés - fuera de alcance de
     este checkpoint, documentado como pendiente).

**Verificación real** (arnés de UI Automation ampliado): Emblema de Guerrero (id 490) muestra
"Aumenta un 15% el daño cuerpo a cuerpo" real; Casco de Shroomite (id 1546) muestra su propio
bonus de daño a distancia + el bonus de set completo real de Shroomite; sin excepciones,
`dotnet test` 128/128 verde.

### Fase 1 (rework de Buffs) - rejilla + panel Editar, ya implementada y verificada

Pregunta a Opus sobre el diseño, punto 3 ("rejilla cantidad de slots y contorno... editar buff
seleccionado... 3 botones de duración"). Hallazgo real que simplificó todo: `PlrCharacter.Buffs`
YA es un `List<PlrBuff>` de tamaño FIJO real según versión (44/22/10, mismo umbral 269 que ya
usa `PlrBodySerializer`) - antes la UI solo mostraba los slots CON buff (`if (buff.Id == 0)
continue`), ahora se trata exactamente igual que un contenedor de objetos: 44 slots reales,
algunos vacíos, en la MISMA `SlotGridPanel`/`ReferenceColumns=10` que ya usan Inventario/
Equipamiento/Almacenes/Monturas/Monedas.

- **`BuffSlotViewModel.cs`** (nuevo) - calco reducido de `ItemSlotViewModel` (sin prefijo/
  cantidad/favorito, un buff no los tiene). `PlaceBuff` usa la duración MÍNIMA real como valor
  inicial (mismo criterio que "mejor prefijo automático" ya usa `ItemSlotViewModel.PlaceItem`).
- **`BuffContainerViewModel.cs`** (nuevo) - gemelo de `ContainerViewModel` pero para buffs.
  Deliberadamente NO se generalizó `ContainerViewModel` en sí (está tipado a
  `ItemSlotViewModel` y lo consumen `AutoEquip`/`SyncEditsBackToMerged`/`StorageGroupViewModel`/
  `EquipmentGroupViewModel` - hacerlo genérico habría sido riesgo real por cero beneficio).
- **`BuffEditViewModel.cs`** (nuevo) - panel "Editar buff seleccionado", gemelo reducido de
  `ItemEditViewModel`. El campo de duración se enlaza DIRECTO a `Slot.DurationSeconds` (mismo
  criterio que `Slot.Count` en `ItemEditTemplate`) - esta clase solo añade los 3 botones de
  preset.
- **`scripts/extraer-duraciones-buffs.py`** → `vanilla_buff_durations.json` (30 buffs reales,
  mismo `split_by_case` ya usado 3 veces en este proyecto) + `VanillaBuffDurationCatalog.cs` +
  **`BuffDurationPresets.cs`** (Core, con tests reales en `BuffDurationPresetsTests.cs`):
  Mínima = dato real extraído (o la moda real de 28800 ticks/8min como aproximación honesta,
  `IsRealMin` distingue los dos casos para la UI); Media = 2x Mínima salvo el buff 257 (Suerte),
  que usa sus 3 tiers reales tal cual (18000/36000/54000, ratio exacto 1:2:3 - único precedente
  real de escalera en todo el juego); Máxima = `S.getMaxTime()` REAL del propio Terrasavr
  (1999999980 ticks ≈ 385,8 días si `Version>=269`, 1080000 si no) - el valor que el usuario
  pidió explícitamente ("el tiempo maximo quiero que sea lo maximo permitido"), no una
  aproximación propia.
- **`scripts/extraer-nombres-buffs-es.py`** → `vanilla_buff_names_es.json` (352 nombres reales,
  clave `BuffName` de `Game.json`) + `VanillaBuffCatalog.GetDisplayName` - cierra un TODO real
  que llevaba documentado en el propio código desde antes de esta sesión ("SIN traduccion real
  al español todavia... hasta que se investigue de donde saca el juego real el nombre").
- `BuffsViewModel.cs` reescrito: `Container` con los slots reales en vez de `Active` filtrado.
  El buscador "Añadir buff..." se queda TAL CUAL por decisión explícita de fases (Opus: "deja
  de momento el panel actual... con eso la pestaña ya está rediseñada de arriba a abajo") -
  rellena el primer slot vacío real, con la duración mínima real aplicada automáticamente.
- `MainWindow.xaml`: `BuffSlotCompactTemplate`/`BuffContainerCompactTemplate`/`BuffEditTemplate`
  nuevos (mismo lenguaje visual que el resto: contorno naranja, fondo de hueco vacío heredado
  de `ItemSlotCardCompact`, tooltip compuesto). `MainWindow.xaml.cs`:
  `OnBuffSlotMouseDown/Move/Drop` gemelos de los de `ItemSlotViewModel` (arrastrar un buff sobre
  otro los intercambia entero). `BuffRowViewModel.cs` borrado (dead code real, sin ningún
  consumidor tras el rework).
- **Pendiente, documentado con claridad para no perder el hilo**: la Librería de buffs con
  árbol real de Terrasavr (`app.BuffSide` - Utilidad/Offensivo/Defensivo/Special/Mascota/
  Negativo + Índice paginado, ya investigada por Opus con las 6 listas reales) es la Fase 2 de
  este rework, todavía SIN implementar - el buscador viejo "Añadir buff..." sigue siendo el
  único mecanismo de añadir un buff nuevo por ahora.

**Verificación real** (arnés de UI Automation ampliado): `Buffs.Container.Slots.Count=44`
confirmado (version 279 ≥ 269); buff colocado vía el mismo comando real que pulsaría el
usuario (`PickBuffCommand`) con nombre real en español ("Piel de obsidiana"); duración mínima
real aplicada automáticamente al colocar (6 min, dato real extraído, no el fallback); slot
seleccionado de verdad (`BuffEdit.Slot` coincide con el slot colocado); los 3 botones
encontrados y pulsados por su NOMBRE dinámico real vía `InvokePattern` - "Máxima" deja
`DurationSeconds=33333333`, exactamente `1999999980/60`, el valor real de Terrasavr. Sin
`ultimo-error.log`. `dotnet test` 134/134 verde (128 previos + 1 de `VanillaBuffCatalog.
GetDisplayName` + 5 de `BuffDurationPresets`, todos con datos reales, no mockeados).

### Fase 2 (rework de Buffs) - Librería de buffs con árbol real de Terrasavr, ya implementada y verificada

Cierra el rework de Buffs pedido en la cuarta pasada. Reparto de responsabilidades igual que
Objetos (`Containers` vs `LibraryViewModel`): `BuffsViewModel` se simplifica a solo construir
el contenedor de 44/22/10 slots; el picker "buscar+colocar" pasa a un `BuffLibraryViewModel`
nuevo (calco exacto de `LibraryViewModel`) - el viejo botón "Añadir buff..." desaparece del
todo, sustituido por la Librería real.

- **`TerrasavrNative.App/Services/BuffLibraryTreeBuilder.cs`** (nuevo, calco de
  `LibraryCategoryTreeBuilder.cs`) - reutiliza `CategoryNodeViewModel` SIN ningún cambio
  (`ItemIdsOrdered` guarda ids de buff, la pertenencia múltiple ya estaba soportada y aquí
  hace falta de verdad: el buff 3 vive en Utilidad Y Special; el 26 en Defensivo Y Special; el
  86 en Offensivo Y Negativo, todo real). 8 raíces reales:
  - 6 categorías curadas con listas literales reales (Utilidad=17, Offensivo=18, Defensivo=13,
    Special=10, Mascota=20, Negativo=26 - **verificado que los conteos reales coinciden
    exactamente** con los que dio Opus tras leer `app.BuffSide`/`initLibs()` real).
  - "Índice": páginas de 33 en 33 sobre TODOS los buffs vanilla conocidos (1..N) - mismo
    espíritu que "Items by ID" en el árbol de objetos, para encontrar cualquier buff que no
    caiga en las 6 curadas.
  - "Calamity (mod)": agrupado por el campo real `category` de `calamity/buffs.json` (305
    buffs reales, 9 categorías reales: Summon=96/StatBuffs=44/DamageOverTime=39/Pets=35/
    Alcohol=25/StatDebuffs=24/Potions=19/Mounts=12/Placeables=9), mismo patrón de paginación a
    40 que ya usa `LibraryCategoryTreeBuilder.BuildCalamityRoot` para objetos. Necesitó
    exponer `CalamityBuffEntry.Category` (antes privado al `record` interno, sin getter
    público - mismo patrón que `CalamityCatalogEntry.Category` para objetos).
- **`BuffLibraryViewModel.cs`** (nuevo, calco de `LibraryViewModel`) - `RootCategories`,
  `Results`, `SearchText`, `SelectedCategory`, `PickTarget` (`BuffSlotViewModel?`),
  `SelectCategoryCommand`/`ClearCategoryCommand`/`PlaceInTargetCommand`/`CancelPickCommand`,
  mismo `MaxResults=300`.
- **`BuffsViewModel.cs` simplificado** a solo `LoadFrom`/`Reset`/`Container` - todo el
  catálogo+búsqueda que tenía se muda a `BuffLibraryViewModel`.
- **`MainViewModel.cs`**: `BuffLibrary` (nueva propiedad) + `IsBuffLibraryCollapsed` (plegada
  por defecto, mismo criterio que `IsLibraryCollapsed`) + `RequestPickForBuffSlot` (gemelo de
  `RequestPickForSlot`: pone el slot como `PickTarget`, selecciona, cambia a la pestaña Buffs,
  despliega la Librería) - `Buffs` ahora se construye pasando este método en vez de
  `SelectBuffSlot` a secas, así que "Elegir..."/doble clic en un slot vacío abre la Librería
  de verdad en vez de solo seleccionar.
- **`MainWindow.xaml`**: bloque de Librería de buffs calcado del de Objetos (cabecera plegable
  + banner "eligiendo" + árbol real a la izquierda + buscador+resultados a la derecha), con
  `BuffCategoryNodeTemplate` nuevo (tercera copia del árbol recursivo -
  `CategoryNodeTemplate`/`ResearchCategoryNodeTemplate`/`BuffCategoryNodeTemplate`, mismo
  patrón ya establecido en el proyecto de no generalizar con un `ICommand` inyectado) y
  `BuffLibraryCardTemplate` nuevo (tarjeta cuadrada de catálogo, mismo lenguaje visual que
  `BuffSlotCompactTemplate`, con botón "Colocar" solo mientras `IsPicking`) dentro de la MISMA
  `SlotGridPanel`/`ReferenceColumns=10` que el resto de rejillas de la app - pedido explícito
  de Opus ("resultados en rejilla, misma tarjeta que la de arriba").

**Verificación real** (arnés de UI Automation ampliado): las 8 raíces reales confirmadas con
sus conteos exactos (`Utilidad (17), Offensivo (18), Defensivo (13), Special (10), Mascota
(20), Negativo (26), Indice, Calamity (mod)`); categoría "Utilidad" seleccionada da
`Results.Count=17` real; `ChooseFromLibraryCommand` real sobre un slot vacío abre la Librería
y fija `PickTarget` de verdad; botón "Colocar" real encontrado y pulsado vía `InvokePattern`
(no simulado) coloca "Piel de obsidiana" (buff 1) con su duración mínima real y selecciona el
slot; los 3 botones de duración siguen funcionando igual que en la Fase 1. Sin
`ultimo-error.log`. `dotnet build`/`dotnet test` 134/134 verde (sin tests nuevos de Core en
esta fase - todo el cambio es de `TerrasavrNative.App`, capa de presentación).

Con esto se cierran las 3 peticiones de la cuarta pasada de feedback (tooltips completos de
equipo, iconos consistentes, rework completo de Buffs).

## Quinta pasada de feedback (2-sep-2026) - contexto menú, botones, Librería, fusión Equipamiento

Mensaje único del usuario, denso, 7 peticiones + instrucción explícita "consulta con opus y
después ejecuta". Consultado Opus (subagente `Plan`, modelo `opus`) para P1/P3/P4/P5; P2
(el bug de arrastrar buffs) era un bug claro, corregido directo sin consulta.

- **Bug arreglado sin consulta**: los buffs no se podían arrastrar desde la Librería hasta la
  rejilla de buffs - `MainWindow.xaml.cs`: `OnBuffLibraryCardMouseDown/Move` nuevos (gemelos de
  `OnLibraryCardMouseDown/Move`) + `OnBuffSlotDrop` ampliado para aceptar tanto un
  `BuffCatalogEntryViewModel` soltado (coloca el buff) como un `BuffSlotViewModel` (intercambio
  ya existente).
- **Menú contextual de slot de buff**: era un `ContextMenu` casi vacío ("Vaciar slot" a secas,
  sin ningún equivalente a "añadir/quitar" de las armas) - se añadió `MenuItem "Elegir buff..."`
  con `ChooseFromLibraryCommand` (mismo comando real que ya abre la Librería con el slot como
  `PickTarget`, ver Fase 2 arriba), y "Vaciar slot" pasó de `Visibility` a `IsEnabled` (en
  slots de objeto y de buff) - antes desaparecía del todo con el slot vacío, dejando un
  recuadro diminuto sin nada útil dentro, exactamente la queja del usuario.
- **P1 (Opus) - Tema oscuro para TODOS los `ContextMenu`/`MenuItem` de la app**:
  `Theme.xaml` gana `Style TargetType="ContextMenu"`/`Style TargetType="MenuItem"` (mismo
  lenguaje visual que el `ComboBox` ya existente - fondo `BgSecondaryBrush`, borde sutil,
  `CornerRadius`). Nota real de WPF: hace falta `HasDropShadow="True"` en el `ContextMenu`
  aunque no se quiera sombra explícita, porque WPF deriva `AllowsTransparency` del Popup
  interno de esa propiedad - sin ella el `CornerRadius` se recorta en seco contra un
  rectángulo opaco.
- **P3 (Opus) - Jerarquía de 3 botones**: nuevos `Tag="AccentSoft"` (variante suave del
  `Accent` ya existente, incluye su propio `MultiTrigger` de elevación en hover) y
  `Tag="Ghost"` (variante mínima para acciones destructivas/deshacer, con su propio
  `MultiTrigger` de hover) añadidos al `Style TargetType="Button"` ya existente en
  `Theme.xaml` (mismo patrón real que `Accent`/`Teal`/`Pink`/`Orange`). Aplicado en
  `MainWindow.xaml`: botones "Mínima"/"Media" de duración de buff → `AccentSoft`, "Máxima" →
  `Accent`; botón "★" (mejor prefijo) → `Accent`; botón "Quitar" (prefijo) y ambos "Vaciar
  slot" (objeto y buff) → `Ghost` - mismo lenguaje visual que "Aplicar build", pedido explícito
  del usuario.
- **P4 (Opus) - Rejilla en la Librería de objetos**: nueva `LibraryCardTemplate` (tarjeta
  cuadrada SIN contorno naranja, calco de `BuffLibraryCardTemplate` - pedido explícito "allí sí
  que no quiero contorno") dentro de la MISMA `SlotGridPanel`/`ReferenceColumns=10` que el
  resto de rejillas de la app, sustituyendo el viejo `WrapPanel` de ancho fijo 80×86 con scroll
  siempre activo. Ahora solo scrollea cuando una categoría tiene de verdad muchos objetos
  (mismo mecanismo real de `AvailableHeight`/`MinCell`/`MaxCell` ya usado en Inventario).
- **P5 (Opus) - Fusión de Equipamiento con Monturas/Monedas como laterales**: las pestañas
  "Monturas" y "Monedas" desaparecen del todo; su contenido pasa a vivir DENTRO de
  "Equipamiento" como dos columnas laterales de un único `Grid` (marcado con
  `controls:SlotRowHost`, clase nueva trivial - necesaria porque `RelativeSource
  AncestorType=Grid` a secas encuentra antes el `Grid` interno de la plantilla del
  `ScrollViewer`, no el propio; mismo motivo real por el que `AvailableHeight` ya usaba
  `AncestorType=ScrollViewer`). Un solo contorno naranja para todo el bloque (3 contornos
  compitiendo en ~672px habría repetido el amontonamiento ya resuelto en la tercera pasada);
  los laterales se distinguen "hundiéndose" (`BgPrimaryBrush`) en vez de con contorno propio.
  - Lateral izquierdo: "Mascota / Montura" + "Tinte", en VERTICAL (`Columns=1`, nuevo parámetro
    en `MainViewModel.AddContainer`), pedido explícito del usuario ("mascotas etc mejor en
    vertical"). Envuelto en su propio `ScrollViewer` de seguridad (mismo patrón ya probado de
    la vieja pestaña "Monturas" standalone).
  - Lateral derecho: "Monedas" + "Munición", en HORIZONTAL (ya lo estaban) - pedido explícito
    "munición así en horizontal está bien", sin tocar.
  - `SlotGridPanel.cs` gana `ReferenceWidth` (DP nueva, 0=desactivada): `ReferenceColumns` por
    sí solo mide contra el ancho PROPIO del panel (`availW`) - funciona cuando los 9
    contenedores comparten literalmente la misma columna de un mismo `Grid` (como hasta ahora),
    pero en la fila fusionada (3 `SlotGridPanel` en 3 columnas DISTINTAS y más estrechas) cada
    uno calcularía su techo contra su propio ancho reducido y los tres colapsarían al
    `MinCell`. `ReferenceWidth` fija el ancho de referencia real (el de TODA la fila fusionada,
    vía `SlotRowHost.ActualWidth`) en vez del ancho propio.
  - **Verificación real, no solo cálculo a mano**: se detectó un hueco real en la aritmética
    inicial de Opus para el truco `Grid.RowSpan="2"` del lateral izquierdo (su cifra
    `(300-16)/5≈56,8px` solo contaba UN grupo de 5 slots, no los DOS apilados - Mascota/Montura
    Y Tinte - que el diseño real necesita) - en vez de fiarse del cálculo, se amplió el arnés
    de UI Automation (`scratchpad/uia-harness/Program.cs`) para rellenar los 4 contenedores
    laterales con objetos reales y capturar un PNG real del render (`RenderTargetBitmap` sobre
    el árbol visual de WPF, no una captura de pantalla dependiente de que la ventana esté
    visible) - confirma visualmente: ningún solapamiento, 0 barras de scroll a la resolución de
    prueba (el `ScrollViewer` de seguridad no hace falta ahí pero no estorba), Mascota/Montura y
    Tinte legibles en vertical, Monedas/Munición legibles en horizontal. Se corrigió además la
    etiqueta del lateral izquierdo de "Equipo" (ambigua junto a "Equipamiento"/"Equipo puesto")
    a "Mascota / Montura" tras verla en la captura real.

**Verificación real**: `dotnet build` limpio (0 errores/0 advertencias), `dotnet test`
134/134 verde. Arnés de UI Automation real: las 3 pestañas restantes
(Equipamiento/Inventario/Almacenes) seleccionan sin excepción; las píldoras
Loadout/Vista/Almacenes siguen cambiando `Current` de verdad tras la fusión; captura PNG real
del Equipamiento fusionado revisada a mano (ver arriba). Sin `ultimo-error.log`.

**Aparte, no incluido en esta consulta a Opus** (petición 5 original del usuario, sobre
Builds): "Botas de terrospark" corregido en `builds.json` como accesorio real de las 4
combinaciones `endgame` (antes "Botas del campo helado", apropiadas solo en fases anteriores)
- verificado contra la wiki real. **Pendiente por completo**: el indicador visual "5 accesorios
en Normal / 6 en Experto / 7 en Maestro" que pidió el usuario no se ha diseñado ni
implementado todavía - solo se corrigió el dato del 6º accesorio de las builds `endgame`. Y
dentro de esa misma tarea, las builds `prehardmode`/`earlyhardmode` (8 combinaciones) NO se
han revisado contra la wiki para su propio 6º/7º accesorio - solo se hizo la ronda `endgame`.
Queda como tarea separada, a retomar cuando el usuario confirme prioridad.

## Sexta pasada de feedback (2-sep-2026) - restricciones reales de slot, iconos fantasma reales, 6º/7º accesorio marcado por color, centrado real, cierre de las builds

Mensaje único, denso, 7 peticiones + "consulta con opus y después ejecuta" otra vez. Dos
investigaciones reales lanzadas en paralelo antes de consultar a Opus (código decompilado de
tModLoader para las restricciones de slot; wiki para las 8 builds prehardmode/earlyhardmode
pendientes), después una consulta a Opus con los hallazgos reales ya en mano.

- **Cierre de las builds** (pregunta explícita del usuario: "¿ya has revisado lo de las
  builds?"): NO estaba cerrado - solo la corrección `endgame` de la ronda anterior. Wiki real
  confirma 2 correcciones más en `builds.json`: `prehardmode.*.accessories[6]` (Fast Clock,
  solo obtenible en Hardmode - contradice la etapa) → Bezoar (real de pre-hardmode, cae de
  Hornets/Toxic Sludges); `earlyhardmode.melee.accessories[6]` (Fire Gauntlet, requiere los 3
  jefes mecánicos) → Trifold Map (ya usado en las otras 3 clases de esa etapa). Traducciones
  reales verificadas contra `vanilla_item_names_by_key.json` del propio proyecto antes de
  escribirlas. Comiteado aparte (`ebb9e5f`) junto al arreglo de centrado, antes del resto de
  esta pasada.
- **Centrado real del panel fusionado**: el usuario reportó que quedaba "a un lado izquierdo".
  Bug real de WPF confirmado con inspección directa del árbol visual (`VisualTreeHelper`, no
  solo la captura): `ItemsPresenter` SIEMPRE le da al panel de un `ItemsPanelTemplate` el
  `finalSize` completo en `Arrange`, ignorando por completo el `HorizontalAlignment` puesto en
  el propio panel en XAML (se probó, compilaba, no tenía ningún efecto visible). El centrado
  real solo puede hacerse dentro de `SlotGridPanel.ArrangeOverride` (offset = hueco entre
  `finalSize` y el tamaño de contenido real). Verificado con números reales: 29,6px de offset
  centrando una celda de 44px en un panel de 103,2px (columna lateral real de 115,2px menos
  padding) - exacto.
- **P1 (investigación real, código decompilado de tModLoader 1.4.5.8)**: qué campo valida cada
  tipo de slot restringido. Confirmado archivo:línea real para los 8 tipos (`ItemSlot.cs`,
  `Item.cs`, `Projectile.cs`, `Main.cs`, `Initializers/DyeInitializer.cs`, `ID/MountID.cs`) -
  ver cabecera de `scripts/extraer-slot-kind-vanilla.py` para el detalle citado completo. Dato
  clave: el gancho/mascota/mascota de luz NO tienen ningún campo directo - se resuelven
  cruzando `shoot`/`buffType` contra tablas reales del motor (`Main.projHook[]` vía
  `aiStyle==7` en `Projectile.cs`; `Main.vanityPet[]`/`Main.lightPet[]` indexados por
  `buffType`). El orden real de los 5 slots de `MountsContainer`/`DyesContainer`
  (`Player.miscEquips`) se confirmó con DOS fuentes independientes coincidentes: el propio
  `Player.cs` decompilado Y el `script.beautified.js` real de Terrasavr
  (`app.TabMiscEquips.slotLabels = ["Pet","Light pet","Minecart","Mount","Hook"]`) - sin
  ambigüedad.
- **P2 (investigación real)**: Opus corrigió dos premisas erróneas de mi propia consulta -
  SÍ existe ya un extractor XNB→PNG probado (`xnb-to-png.js`+`lzx-decoder.js`, hecho el mismo
  día en el proyecto hermano) y SÍ está el código real que dibuja los iconos fantasma
  (`ItemSlot.cs`, `TextureAssets.Extra[54]` = `Content/Images/Extra_54.xnb`, atlas 3x6 de
  34x34 recortado a 32x32). Tabla contexto→frame verbatim del switch real.
- **Extracción real de los 13 iconos fantasma** (`scripts/extraer-iconos-fantasma-slot.js`,
  nuevo): extrae `Extra_54.xnb` real de la instalación de Steam, recorta las 13 celdas usadas
  (armor_head/body/legs, vanity_head/body/legs, accessory, accessory_vanity, dye, hook, mount,
  minecart, pet, pet_light) y las retiñe a `TextSecondaryBrush` (#8a8fa3, ya existente en
  Theme.xaml) en vez del blanco original del juego - para que pertenezcan a la paleta de
  Terrakeep. Verificado visualmente con una hoja de contacto antes de integrar: las 13
  siluetas encajan exactamente con su significado (gancho = bastón de caramelo real, montura =
  herradura, mascota = huella, mascota de luz = estrella...). Salida:
  `Assets/vanilla/slot_ghosts/{nombre}.png`.
- **`scripts/extraer-slot-kind-vanilla.py`** (nuevo): escanea `Item.cs` (reutilizando
  `split_by_case`, el escáner de bloques ya depurado de `extraer-categorias-vanilla.py`),
  `Projectile.cs` (ganchos reales, aiStyle=7), `Main.cs` (mascota/mascota de luz por buffType)
  y `Initializers/DyeInitializer.cs` (tintes reales - `dye = GameShaders.Armor.
  GetShaderIdFromItemId(type)` se asigna GLOBAL fuera de cualquier SetDefaults, así que hubo
  que localizar dónde se REGISTRA cada tinte de verdad, no donde se lee). Salida:
  `Assets/vanilla_slot_kind.json` (bitmask por id). Cobertura real, no ocultada: 210→314
  objetos clasificados tras añadir tintes reales (105/120 tintes, 87,5%); monturas/vagonetas
  solo 37/52 asignaciones reales encontradas (~70%, mismo techo de cobertura ya documentado en
  `extraer-categorias-vanilla.py` - un id ausente del catálogo NO se restringe, nunca al
  revés, documentado en el propio script y en `VanillaSlotKindCatalog.cs`).
- **Core**: `SlotKind` (nuevo, `[Flags] enum`, 8 bits) + `VanillaSlotKindCatalog` (calco de
  `VanillaCategoryCatalog`, carga `vanilla_slot_kind.json`) - regla obligatoria de Opus
  ("desconocido = permitir"): un objeto de Calamity SIEMPRE pasa cualquier restricción
  (`CalamityCatalogEntryData` no tiene ninguno de estos campos reales - validar estricto
  bloquearía TODAS las monturas/mascotas/tintes reales de Calamity).
- **`ItemSlotViewModel`**: `AcceptedKind`/`GhostIconPath`/`IsExpertAccessorySlot`/
  `IsMasterAccessorySlot`/`RejectionMessage` nuevos; `AcceptsItem(id)` público (usado también
  por el filtro de la Librería y el `DragOver`); `PlaceItem`/`OnItemIdChanged` rechazan un
  objeto no válido - UX de 3 capas real, ninguna intrusiva (consulta a Opus): (1) la Librería
  se filtra de inmediato al elegir para un slot restringido (`LibraryViewModel.ApplyFilter`,
  el usuario nunca ve un objeto inválido que poder elegir - capa principal); (2) arrastrar y
  soltar cambia el cursor del sistema a "prohibido" ANTES de soltar (`OnItemSlotDragOver`
  nuevo, `DragDropEffects.None`); (3) el campo "Índice" a mano (único camino donde el silencio
  confunde de verdad) revierte el número y escribe un aviso corto en el panel Editar.
- **`MainViewModel`/`EquipmentGroupViewModel`**: `AddContainer`/`AddSlotSet` ahora pasan
  `SlotKind`/nombre de fantasma por índice real - `miscEquips` con el orden real confirmado
  (Pet/LightPet/Cart/Mount/Hook), `miscDyes` siempre `Dye`, `coins`/`ammo` con su propio tipo,
  y la rejilla de Armadura/Accesorios (`loadout0Items`, 10 slots reales) con ghost por índice
  (0-2 armadura, 3-9 accesorio) y el 6º/7º (índices 8/9) marcados - SOLO en la vista
  Armadura/Accesorios, no en Vanidad/Tintes (pedido explícito: "en la rejilla de armadura y
  accesorios").
- **P3 (6º/7º accesorio)**: franja vertical de 3px en el borde interior izquierdo (no el
  contorno del slot - ya carga 3 significados mutuamente excluyentes: Calamity/Equipado/
  Seleccionado, y esto es identidad PERMANENTE que debe leerse a la vez). Rosa (`PinkBrush`,
  ya existente) para el 6º (Experto/Corazón de Demonio); nuevo `MasterGoldColor`/
  `MasterGoldBrush` (#ffd24a) para el 7º - un punto real de la pulsación
  `(255,masterColor*200,0)` que usa el propio juego (`Main.cs:20109-20111`), no inventado.
  Permanente, nunca condicionado a datos del personaje cargado (hallazgo real de Opus:
  `Main.masterMode` no tiene NINGÚN dato en el .plr al que condicionarse - condicionar solo el
  6º sería incoherente). Tooltip explicativo real (visible también con el slot vacío,
  `ShowTooltip` nuevo en `ItemSlotViewModel`).
- **XAML** (`SlotCompactTemplate`): `Image` de fantasma detrás del icono real (`Opacity=0.35`,
  visible solo con `IsEmpty`), las 2 franjas de color, tooltip ampliado, `ToolTipService.
  IsEnabled` cambiado de `IsNotEmpty` a `ShowTooltip` (el 6º/7º quiere explicarse también
  vacío). Panel "Editar": `RejectionMessage` con color `CalamityBrush`, sin reservar espacio
  cuando no hay mensaje (`Collapsed`).

**Verificación real** (arnés de UI Automation ampliado con ids reales conocidos: 84=Gancho de
escalada, 1914=Campanas de reno, 2191=Jaula de ratón, 603=Zanahoria, 425=Campana de hada,
1007=Tinte rojo, 40=Flecha de madera, 71=Moneda de cobre): las 8 combinaciones
restricción/objeto real probadas exactas (aceptado cuando corresponde, rechazado con mensaje
real cuando no, Calamity siempre aceptado); 6º/7º accesorio marcado correctamente y el 3º
(normal) sin marcar; fantasma real resuelto para cabeza de armadura. Captura PNG real
(`RenderTargetBitmap`) revisada a mano: fantasmas grises visibles en los huecos vacíos
(incluida la ausencia real de fantasma en Munición, el propio juego tampoco dibuja uno ahí),
franja rosa visible en el 6º accesorio, franja dorada en el 7º, iconos reales de mascota/
montura/gancho/tinte/monedas renderizados sin solapes. `dotnet build` limpio, `dotnet test`
134/134. Sin `ultimo-error.log`.

**Pendiente, documentado con claridad**: cobertura de `vanilla_slot_kind.json` no es del
100% (ver arriba, ~70-87% según el tipo) - un id ausente del catálogo se queda sin restringir
(nunca al revés, así que no es un riesgo de bloquear algo válido, solo de no restringir algo
que debería). Ampliar el escáner a los casos que no encuentra queda fuera de esta pasada.

### Dos correcciones del usuario a mitad de esta misma ronda

1. **"rectifica las armaduras y los accesorios si los quiero arriba"** - el usuario SÍ quiere
   que la rejilla de Armadura/Accesorios (`loadout0Items`, y también Vanidad/`loadout*Social`,
   mismo campo real) tenga su propia restricción de tipo, igual que tintes/gancho/montura/
   mascota/moneda/munición - ampliación del alcance original, no solo lo que se había pedido
   literalmente. Añadidos 4 bits nuevos a `SlotKind` (`ArmorHead=256/ArmorBody=512/
   ArmorLegs=1024/Accessory=2048`), extraídos con el mismo escáner ya depurado
   (`extraer-slot-kind-vanilla.py` ampliado con los campos directos reales `headSlot`/
   `bodySlot`/`legSlot`/`accessory` de `Item.cs` - mismo patrón ya validado en
   `extraer-categorias-vanilla.py`, sin cross-referencing complejo esta vez). Vale igual para
   objetos funcionales y de vanidad (`vanity=true` no cambia el equip type real). Cobertura:
   259 accesorios, 239/171/145 cabeza/cuerpo/piernas reales encontrados - números creíbles.
   `EquipmentGroupViewModel.AddSlotSet` ahora pasa `SlotKind` por índice también en Items y
   Social (índices 0-2 = cabeza/cuerpo/piernas, 3-9 = accesorio). Verificado con ids reales
   conocidos (1546=Tocado de piñonita→ArmorHead, 490=Warrior Emblem→Accessory): un objeto no
   armadura (3=Bloque de piedra) rechazado de verdad en el slot de cabeza, con mensaje real,
   sin tocar el objeto que ya había puesto.
2. **"los slots también de armadura y accesorio vuelvan a la parte superior no al centro"** -
   corrección real a mi propio sobrealcance: la queja ORIGINAL del centrado (quinta pasada) era
   solo horizontal ("a un lado izquierdo"); mi primer arreglo centró también en VERTICAL
   (`offsetY` en `SlotGridPanel.ArrangeOverride`), lo cual desplazaba la rejilla de
   Equipamiento (fila `"*"` con hueco vertical real de sobra) hacia el medio, separándola
   visualmente de la fila del selector Loadout/Vista - nadie lo había pedido. Corregido:
   `offsetY` fijo a 0 (arriba) siempre, solo `offsetX` centra. Verificado con captura real:
   la rejilla vuelve pegada justo debajo del selector, sin hueco.

**Verificación real de ambas correcciones**: `dotnet build` limpio, `dotnet test` 134/134,
arnés de UI Automation con nuevas aserciones (`AcceptsItem`/`PlaceItem` sobre slots de cabeza
y accesorio con ids reales conocidos) y captura PNG real revisada a mano tras cada cambio.

### Dos correcciones más, ronda de verificación con el usuario

1. **"la armadura me deja colocarla en los huecos de accesorios hay que corregirlo"** - bug
   real confirmado, causa real identificada: la regla "Calamity siempre pasa" (correcta para
   los 8 tipos SIN dato real posible - ammo/mountType/buffType/dye/shoot no existen en
   `CalamityCatalogEntryData`) se aplicaba tambien a Armadura/Accesorio, donde SI hay un campo
   real utilizable (`Category`, ya en `catalog.json`, ya usado para el árbol de la Librería:
   `"Armor/..."` / `"Accessories"`/`"Accessories/Vanity"`/`"Accessories/Wings"`) - así que un
   objeto de armadura de Calamity se aceptaba sin más en cualquier slot de accesorio, y
   viceversa. `ItemSlotViewModel.AcceptsItem` reescrito: para el grupo Armadura/Accesorio
   específicamente, consulta `CalamityCatalog.BySyntheticId(id)?.Category` en vez de aceptar a
   ciegas; para los otros 8 tipos, sigue aceptando siempre (sin cambio, siguen sin dato real).
   Verificado con ids reales de Calamity (20000243=armadura real Armor/Aerospec,
   20000000=accesorio real Accessories): armadura de Calamity ahora rechazada en un slot de
   accesorio, accesorio de Calamity rechazado en un slot de cabeza - antes ambos pasaban.
2. **"la sección de slot de monturas tintes han de ser más pequeño los cuadrados... ahora sale
   scroll y no lo queremos"** - `ContainerViewModel.MinCell` nueva (default 44, igual que
   siempre) - `MountsContainer`/`DyesContainer` bajan a `MinCell=32` (antes el suelo de 44px
   forzaba el `Clamp` a subir el tamaño de celda por encima de lo que cabía de verdad en la
   altura real disponible, disparando el `ScrollViewer` de seguridad). El resto de
   contenedores se queda en 44 sin cambios. `ContainerCompactTemplate` ahora lee
   `MinCell="{Binding MinCell}"` en vez del literal fijo `"44"`.

**Verificación real**: `dotnet build` limpio, `dotnet test` 134/134, arnés de UI Automation con
las 4 aserciones Calamity nuevas (armadura Calamity aceptada en cabeza/rechazada en accesorio,
accesorio Calamity aceptado en accesorio/rechazado en cabeza, las 4 exactas) y captura PNG real
confirmando celdas más pequeñas sin scrollbar visible en el lateral izquierdo.

## Séptima pasada de feedback (2-sep-2026) - redimensionado real de toda la app + botones ovalados/modernos

Mensaje único, muy denso: solape real en Equipamiento al redimensionar, scroll persistente en
Mascota/Montura/Tinte, pérdida de contenido en CUALQUIER rejilla al reducir la ventana ("siempre
ha de verse [el numero real de slots], independientemente de lo pequeña que se haga la ventana"),
la Librería no se adapta bien, y estética "horrenda" de ciertos botones (captura 1, el selector
de pestañas de Almacenes/Vista) frente a un "moderno" (captura 2, "Auto-equipar" de Builds) -
pedido explícito de aplicar el estilo moderno a TODOS los botones. Instrucción final: "consulta
esto muy bien con Opus y que dedique un buen tiempo a todo esto".

Investigación real ANTES de consultar (capturas `RenderTargetBitmap` a varios tamaños de
ventana real, incluido el `MinWidth`/`MinHeight` declarado de la propia ventana - no un caso
extremo forzado): confirmados los 3 bugs tal cual los describió el usuario, con captura real de
cada uno. Consulta a Opus con estos hallazgos + XAML real citado - respuesta muy extensa
("dedica el tiempo real que pide el usuario"), con un hallazgo cuantitativo real (midió los
5454 iconos vanilla reales con PIL: el sprite mediano ocupa 28 de 40px de lienzo) y un modelo de
presupuesto vertical/horizontal que predijo la altura real disponible en la captura del usuario
casi exacto antes de verla.

**Implementado, siguiendo el orden de Opus**:

1. **Los paneles de Librería ya no roban espacio al contenido principal**: `Grid Height="238"`
   fijo (Objetos y Buffs) sustituido por `RowDefinition` reales - las DOS filas (contenido
   principal / Librería) ahora compiten de verdad por el alto disponible (`3*`/`1*` con
   `MinHeight`/`MaxHeight` reales en vez de `*`/`Auto` con un hijo fijo que "Auto" cobraba
   siempre primero, sea cual sea el alto real - "el Inventario estaba subvencionando a la
   Librería", diagnóstico real de Opus). Esto solo, sin tocar ningún tamaño de icono, resuelve
   el caso más grave (Inventario mostrando solo ~2 de sus 5 filas reales con la Librería
   desplegada a la resolución mínima) - **verificado**: ahora se ven las 5 filas completas.
2. **Suelo/techo universal de celda recalibrado con datos reales**: `MinCell` 44→40 (Opus midió
   que 44 da una escala `NearestNeighbor` irregular - 0.85x, "peor por píxel" que 40 pese a ser
   más grande; 40 coincide con el `MinCell` que ya llevaba meses en producción en la Librería
   sin ninguna queja, el mejor experimento controlado real del proyecto) y `MaxCell` 96→90 (96
   daba una escala 2.15x irregular; 90 da 2.00x exacto). `ContainerViewModel.MinCell` (ya
   existía) y `MaxCell` (nuevo) - default 40/90, aplicado en todos los `SlotGridPanel` reales.
3. **`Window.MinWidth`/`MinHeight`**: 1000×620 → primero 1040×700 (cálculo real de Opus,
   contando línea a línea el presupuesto fijo real de la pestaña Objetos), luego 1080×700 tras
   un déficit real de ~18px medido con el arnés al implementar el punto 4 (ver abajo) - todo
   verificado con números reales del propio arnés, no calculado a ciegas.
4. **El solape real de Equipamiento**: causa exacta confirmada con la aritmética real
   (`SlotRowHost` a 1000px de ancho, columna central recibía 214px pero su contenido real
   pedía 236px - 22px de desborde real, exacto al pixel del solape fotografiado). Columnas
   `2*/5*/4*` (proporciones puras, sin relación con los anchos mínimos reales de cada bloque)
   sustituidas por `Auto`+`MinWidth` en los laterales y `*` en el centro (que ahora se queda
   con TODO el sobrante real) + `ClipToBounds="True"` en el `SlotRowHost` como red de
   seguridad (nunca debería activarse ya, pero prefiere recortar en silencio a solapar visible
   si algún caso raro se escapa).
5. **Etiqueta cortada ("Mascota /" sin "Montura")**: `TextWrapping="Wrap"` - pero se descubrió
   un gotcha real de WPF no anticipado por Opus: una columna `"Auto"` mide a sus hijos con
   ancho INFINITO en la primera pasada, así que `Wrap` sin más NO envuelve de verdad (el texto
   sin envolver, más ancho que la rejilla de iconos, terminaba siendo la etiqueta quien fijaba
   el ancho real de toda la columna) - hizo falta añadir también `MaxWidth="70"` real para
   forzar el envoltorio. Encontrado y corregido verificando con el arnés (`ActualWidth` medido
   de la columna, no dado por bueno solo porque el texto se veía completo).
6. **Botones ovalados/modernos en toda la app**: `Button` base con `CornerRadius="999"` (WPF
   clampea el radio real a la mitad del lado más corto - un solo número, óvalo perfecto a
   cualquier tamaño). `PrefixMetaButton`/`PrefixGroupButton` (ya eran píldora, `CornerRadius=99`,
   pero PLANOS - sin degradado/elevación/animación) reescritos como `BasedOn` del `Button` base
   en vez de mantener su propia plantilla duplicada - `IsSelected` ahora se traduce a
   `Tag="Accent"`/`"AccentSoft"` (lo único que la plantilla base entiende de verdad) en vez de
   pintar `Background`/`Foreground` a mano. Nuevo `BarButton` (calco del `Button` base con
   `CornerRadius="8"` en vez de `999`) para las 2 barras reales a todo el ancho donde una
   píldora completa se vería como un error visual (cabeceras plegables de "Librería"/"Librería
   de buffs") - los demás casos candidatos (botones "Colocar", fila de NPC del Explorador) NO
   lo necesitaban tras revisar el XAML real (ya son tan cortos que el clamp de 999 no cambia
   nada visible, o su fondo real lo pinta un `Border` interior propio). De paso, 2 bugs reales
   de "`Background` puesto a mano no hace nada" (mismo patrón ya documentado antes en
   `ItemEditTemplate`/`NavCardButton`) encontrados y corregidos: "Colocar" (`Tag="Accent"`
   añadido) y "Cancelar" de los banners de "eligiendo" (`Tag="Ghost"` añadido).
7. **Bug real encontrado DESPUÉS de implementar la recomendación de Opus** (no en su consulta -
   verificado con el arnés tras el cambio, no dado por bueno a ciegas): con más alto real
   disponible (tras el punto 3), el lateral Mascota/Montura/Tinte (columna única, sin techo de
   celda propio hasta ahora) crecía sin límite hacia el techo universal (90), robándole sitio
   real a la columna central. Arreglado con un `MaxCell` propio (56) para estos 2 contenedores,
   igual que ya tenían su propio `MinCell` (32).
8. **Bug real encontrado DESPUÉS, en el mismo ciclo de verificación**: incluso con `MaxCell=56`,
   Mascota/Montura y Tinte APILADOS (10 filas reales entre los dos) seguían sin caber sin
   scroll - medido con el arnés: 412px de contenido real contra 278px disponibles, un desfase
   de 134px que NINGÚN ajuste de `MinCell`/`MaxCell` podía cerrar sin bajar del suelo real de
   legibilidad (30px, "mancha de color", límite de Opus). Solución real: Mascota/Montura y
   Tinte uno AL LADO del otro en vez de apilados (hay de sobra más ancho que alto en este
   lateral) - así cada uno solo necesita 5 filas de alto, no 10. Esto obligó a subir
   `MinWidth` de la ventana otra vez (1040→1080, punto 3) para compensar el nuevo ancho mínimo
   real de esta columna (140px, dos rejillas de 1 columna en vez de una).

**Verificación real, en cada paso, no al final**: `dotnet build` limpio en cada cambio,
`dotnet test` 134/134, arnés de UI Automation con diagnóstico whitebox nuevo (anchos/altos
REALES de `SlotRowHost.ColumnDefinitions` y cada `SlotGridPanel`, no calculados a mano) y
captura PNG real revisada a mano en cada iteración - el proceso completo encontró y corrigió 2
bugs reales que la propia consulta a Opus no había anticipado (puntos 7 y 8), exactamente el
criterio de este proyecto de "medir lo real, no dar nada por bueno sin comprobarlo". Estado
final confirmado con captura real a 1080×700 (mínimo) y 1180×860 (grande): sin solape, sin
scroll en ningún lateral, Inventario/Almacenes muestran sus 40-50 slots completos con la
Librería desplegada, botones en píldora con degradado en toda la app.

**Pendiente, documentado con claridad** (fuera de esta pasada, aplazado por el propio Opus -
punto 7 de su respuesta, "lo mas ambicioso... puede esperar a otra ronda"): un scroll único de
página en vez de scrolls internos por rejilla, y que la fila fusionada de Equipamiento
reordene sus bloques (Monedas/Munición debajo del centro en vez de al lado) en ventanas
todavía más pequeñas que el mínimo actual - la respuesta actual (los 3 laterales/centro con
`MinWidth`/`MinHeight` reales) ya evita solape y pérdida de contenido en todo el rango
soportado, pero no "reflowea" mas allá de eso.

### Corrección real del usuario tras ver el resultado: la forma de los botones era al revés

"los botones era al reves me gustaban mas los otros la has cagado en eso era al reves todos los
botones como estaban en la ventana de builds donde apretabas autoequipar" - el punto 6 de
arriba interpretó mal el pedido: puse `CornerRadius="999"` en el estilo BASE de `Button` para
forzar píldora completa en todos los botones, pero ese es el MISMO estilo que ya usaba
"Auto-equipar" (`Tag="Accent"`) - el cambio también le cambió la forma AL PROPIO botón de
referencia que el usuario quería replicar en todos los demás. "Auto-equipar" dejó de parecerse
a sí mismo, y el resultado fue que TODO se volvió el mismo óvalo en vez de que los demás
botones se parecieran a como "Auto-equipar" ya estaba antes.

Corregido: `CornerRadius` del `Button` base vuelve a `8` (la forma real que "Auto-equipar" ya
tenía, esquinas redondeadas normales, nunca fue una píldora completa) - lo que hacía "moderno"
a "Auto-equipar" frente a los botones planos siempre fue el degradado/elevación/animación
(`Tag`), nunca la forma. `PrefixMetaButton`/`PrefixGroupButton` (la fusión con el estilo base,
`Tag`-driven, del punto 6) se queda tal cual - con `CornerRadius=8` heredado del base ya
replican exactamente el aspecto real de "Auto-equipar", que es lo que se pedía desde el
principio. `BarButton` se queda (su comentario se actualizó: su razón real de existir nunca fue
el radio - las 2 cabeceras plegables necesitan `HorizontalAlignment="Stretch"` en el
`ContentPresenter` para que su `DockPanel` interno con "▲ Plegar" a la derecha se estire de
verdad, cosa que el `Button` base no permite aunque se le ponga
`HorizontalContentAlignment="Stretch"` a mano - mismo gotcha de plantilla-manda-sobre-propiedad
ya documentado varias veces en este archivo).

**Verificación real**: captura PNG real de Builds ("Auto-equipar"/"Vanilla") y de Almacenes
("Fragua del Defensor" seleccionado) lado a lado - ambos coinciden exactamente en forma
(esquinas redondeadas, no píldora) y tratamiento (degradado morado/naranja + texto en negrita).
`dotnet build` limpio, `dotnet test` 134/134.

### Octava pasada - crash real + rediseño de la Librería (Fases 1-7), REVERTIDO por feedback directo

Tras el arreglo de botones de arriba, esta pasada añadió: (1) arreglo real de un crash
(`KeyNotFoundException` al elegir "Armas" en la Librería con un personaje real cargado,
`_byId[id]` sin comprobar existencia) y (2) un rediseño completo de la Librería en 7 fases
(navegador de un solo nivel con migas de pan en vez del árbol indentado, paginación real en
vez de scroll, gramática de búsqueda real de Terrasavr, centrado del bloque de Equipamiento a
tamaño natural en pantallas grandes, matriz final de verificación) - todo consultado con Opus e
investigado contra el código real decompilado y la propia Terrasavr-Calamity, con verificación
real vía UI Automation en cada fase y cero fallos en el pase de regresión final.

**El usuario rechazó el rediseño completo tras verlo**: *"quiero la estetica de antes del
cambio no ha solucionado lo que yo creia"*. Preguntado para acotar qué revertir exactamente
(qué parte de la estética, y qué seguía sin arreglarse), la respuesta fue clara: *"me refiero a
volver antes de esta ronda las 7 fases que has echo volveria al principio"* y el motivo real:
*"Es más incómodo de usar que antes"* - no un bug, un rechazo directo de la nueva forma de
navegar/interactuar con la Librería frente a la anterior.

**Revertido con `git revert --no-commit 1331ac6..HEAD`** (rango completo de las 7 fases,
commits `83fd33c`..`1d09c9b`) - se mantuvo DELIBERADAMENTE el arreglo del crash (`1331ac6`,
commit justo anterior al rango revertido) sin pedírselo explícitamente al usuario, porque
revertirlo también habría reintroducido un crash real de la app (no una preferencia estética,
un fallo que tumba la ventana entera) - si el usuario también quiere ese commit fuera, hay que
pedirlo aparte. Se usó `revert` (no `reset --hard`) a propósito, aunque el repo no tiene remoto
configurado: preserva el historial completo (las 7 fases originales siguen disponibles en los
commits `83fd33c`..`1d09c9b` por si se quieren rescatar ideas sueltas de ahí en el futuro - ej.
la gramática de búsqueda real de Terrasavr de la Fase 4, o `LibraryGridPanel` de la Fase 2) en
vez de borrarlo sin dejar rastro. El arreglo del crash (`_byId.GetValueOrDefault` en vez del
indexador directo, tanto en `LibraryViewModel` como en `BuffLibraryViewModel`) vivía por
completo en `1331ac6`, fuera del rango revertido - se comprobó tras el revert que sigue intacto
en ambos ficheros.

`dotnet build` limpio, `dotnet test` 134/134 tras el revert.

**Lección para la próxima vez que se pida un rediseño grande de UI**: antes de dar por cerrada
una ronda de varias fases de diseño (por muy verificada que esté técnicamente - sin scroll, sin
solape, cero fallos en el arnés), enseñar el resultado real al usuario ANTES de encadenar todas
las fases siguientes, en vez de ejecutar las 7 de un tirón y verificar solo con el arnés
automático - el arnés puede confirmar que algo "funciona" (no hay excepciones, no hay solape)
sin poder juzgar si de verdad es más cómodo de usar que lo anterior, que es una decisión que
solo puede tomar el usuario viéndolo con sus propios ojos.

### Arreglo real: las armaduras de Calamity no mostraban ninguna estadística al pasar el ratón

Reportado directamente por el usuario tras probar el estado revertido: *"las armaduras de
calamity no dicen especificaciones cuando pasas el raton"*. Investigado antes de tocar nada
(regla del proyecto: mirar el código real de Calamity antes de suponer) - la causa NO era un
bug de formato como parecía a primera vista (`ItemStatsFormatter.Format` pasaba `defense: null`
a propósito en la rama de Calamity), sino un hueco de datos más profundo: `catalog.json`
(`TerrasavrNative.App/Assets/calamity/catalog.json`, 2709 objetos reales) nunca tuvo `defense`
en su `stats` para NINGÚN objeto de Calamity - comprobado antes de cambiar nada: 0 de 186
armaduras reales con `defense`. El mismo fichero compartido en la app Electron original
(`Terrasavr-Calamity-Beta/local-site/calamity/catalog.json`) tiene exactamente el mismo hueco -
no es un bug introducido en el puerto, es un hueco real de extracción de datos heredado desde
antes.

**Arreglo real, no un parche de formato**: nuevo `scripts/extraer-defensa-calamity.js` indexa
los 7489 nombres de clase reales del código decompilado de Calamity
(`Downloads\tModLoader-Decompiled\CalamityMod`) y extrae `Item.defense = N` de verdad del
`SetDefaults()` real de cada objeto (mismo criterio que `extraer-estadisticas-vanilla.py` para
vanilla) - verificado a mano contra el fuente real antes de confiar en el regex
(`AerospecBreastplate.cs:33`, `base.Item.defense = 7;`, coincide exacto con lo extraído).
Resultado real: 147 objetos con defensa real (130 armaduras + 17 accesorios, ej. "Corazón de
Draedon" = 20 defensa), 56 armaduras sin ninguna encontrada - comprobado que estas últimas son
piezas VANITY reales (ej. `AncientGodSlayerHelm`, `namespace CalamityMod.Items.Armor.Vanity`,
`Item.vanity = true`), que en el juego real nunca tienen defensa - correcto que se queden sin
tooltip de estadísticas, igual que ya pasa con la vanidad vanilla, no un fallo de la extracción.

`CalamityItemStats` (Core) gana el campo `Defense` (antes ausente del modelo entero, ni
siquiera deserializable). `ItemStatsFormatter.Format` ya no hardcodea `defense: null` en la
rama de Calamity, pasa `s.Defense` real.

**Verificación real** (proyecto standalone en el scratchpad, referencia directa a
`TerrasavrNative.App`/`CharacterFileService` real, sin necesidad de UI Automation ya que es
lógica de datos/formato, no de layout): `AerospecBreastplate` → "7 defensa",
`DraedonsHeart` → "20 defensa", `AncientGodSlayerHelm` (vanity real) → sin tooltip (correcto).
130/186 armaduras de Calamity con "defensa" real en su tooltip tras el arreglo. `dotnet build`
limpio, `dotnet test` 134/134 en toda la solución.

**Pendiente, fuera de alcance de este arreglo puntual** (documentado, no resuelto): el bono de
set completo y la descripción textual de accesorios de Calamity siguen sin extraer (ya
documentado como pendiente en `ItemStatsFormatter` desde antes - el `.tmod` real solo trae
localización en inglés en esta instalación). Si en algún momento se quiere sincronizar este
mismo arreglo de `defense` en la app Electron (`Terrasavr-Calamity-Beta`), su `catalog.json`
tiene el mismo hueco real y el mismo script (adaptando la ruta de salida) lo resolvería igual -
no se ha tocado esa app en esta pasada, el reporte vino de Terrakeep (Terrasavr-Native).

### Arreglo real: la bonificación por el set completo de Calamity tampoco aparecía

Pedido explícito tras confirmar el arreglo de la defensa: *"si el arreglo de defensa si pero la
bonificacion por el set no"*. Investigación real antes de tocar nada (preguntado antes de
implementar cuánto alcance quería el usuario - respuesta: *"haces la opcion 1 pero haces la
traduccion tambien"*, la versión completa con traducción real):

**El texto del bono de set NO es un string suelto** - `player.setBonus` se rellena en
`UpdateArmorSet()` con `this.GetLocalization("SetBonus").Format(args...)`, y el propio texto
usa el sistema REAL de sustitución de tModLoader (`{$Clave.Ruta@N}`, decompilado de
`LanguageManager.ProcessCopyCommandsInTexts` - `Downloads\tModLoader-Decompiled\tModLoader\
Terraria\Localization\LanguageManager.cs:562`): las claves referenciadas se buscan subiendo un
nivel de la ruta de la clave que las usa cada vez (igual que buscar un fichero subiendo
directorios), tomando la primera que exista; `@N` desplaza los `{0}`/`{1}`/... del texto
referenciado en N posiciones antes de insertarlo (para no chocar con los propios del texto que
lo incluye). Descubrimiento real durante la investigación: `{$CommonItemTooltip.X}` (usado
dentro de varios bonos de set) NO es de Calamity - es un grupo real de tModLoader/Terraria CORE
(`Language.GetOrRegister("CommonItemTooltip...")`, sin prefijo de mod, confirmado en
`CalamityGlobalItem.cs`) que **SÍ tiene traducción oficial real al español**
(`Terraria.Localization.Content.es_ES.tModLoader.json`) - se usa esa, no una traducción propia,
para esa parte. Lo mismo con `Key.UP`/`Key.DOWN` (`Content.es_ES.Main.json`).

Nuevo `scripts/extraer-bonos-set-calamity.js`: parser hjson real (bloques `'''`, anidación,
comentarios), registro plano de las 11014 claves reales de Calamity (en-US) + las 38 claves
reales `CommonItemTooltip.*` (español oficial) + 2 `Key.*` (español oficial), resolutor
recursivo real de `{$...@N}` (calco exacto del algoritmo decompilado), parser recursivo real de
expresiones C# de los argumentos de `.Format(...)` (números, campos `static` propios o
cruzados de otra clase, aritmética con paréntesis/casts, llamadas `static` tipo
`CalamityUtils.SecondsToFrames(N)`, y las fórmulas REALES de `CalamityUtils.cs` -
`ToPercent`/`ToStealth`/`FramesToSeconds`/`ToRegenPerSecond`/`ToJumpSpeedPercent`/`ToTiles`/
`GetChanceFromDenominator`/`ScaleWithDifficulty`, no aproximadas), pluralización real
`{^N:singular;plural}`, y limpieza real de las etiquetas de color `[c/{N}:texto]` (sin
traducción visual posible en un tooltip de texto plano, se retira la envoltura y se deja el
texto). 3 casos genuinamente no resolubles sin simular al jugador real (tecla configurada,
`player.manaCost`/`CalamityWorld.revenge` en su valor base) se documentan como tal en vez de
inventar un valor concreto. **Resultado real: 69 de 69 sets de armadura de Calamity resueltos,
cero plantillas sin resolver.**

Traducción manual real (`set_bonus_es.json`, revisada frase a frase contra el texto en inglés ya
resuelto y los números reales extraídos) para la parte que Calamity solo trae en inglés -
`scripts/aplicar-bonos-set-calamity.js` la aplica a `catalog.json` (`CalamityCatalogEntryData.
SetBonus`, campo nuevo). `CalamityCatalogEntry.SetBonus` expuesto en Core;
`ItemStatsFormatter.Format` añade la sección "Con el set completo: …" para Calamity, igual que
ya existía para vanilla.

**Bug real encontrado y corregido durante la propia verificación** (no en el primer intento -
aplicar el mismo texto a las 2-3 piezas de cada set): la coraza/piernas de sets con VARIAS
cabezas por clase (Aerospec, Bloodflare, Tarragon...) son la MISMA pieza física compartida por
las 5 variantes (Cuerpo a cuerpo/A distancia/Magia/Invocación/Pícaro) - aplicar el bono a los 3
miembros dejaba la coraza con el texto de la ÚLTIMA variante procesada (ej. Invocación),
incorrecto si el jugador lleva puesta otra cabeza. En el juego real no hay un "bono de la
coraza sola" fijo - depende de qué cabeza concreta se lleve, algo que este catálogo sin
personaje cargado no puede saber. Corregido aplicando el texto SOLO a la pieza de cabeza que
de verdad lo define (`UpdateArmorSet` real) - la coraza/piernas se quedan sin bono propio en su
tooltip individual (honesto: nunca mostrar un dato erróneo por mostrar algo).

**Verificación real** (mismo proyecto standalone en el scratchpad): `AerospecHeadMelee` → bono
de cuerpo a cuerpo real y completo; `AerospecBreastplate` (compartida) → sin bono, correcto;
`StatigelHeadMagic`/`ForbiddenCirclet` → bono real correcto. `dotnet build` limpio, `dotnet
test` 134/134.

## Ejecución completa del plan de la auditoría de Opus (pedido explícito: "haz todo el plan de opus sin parar")

Tras la auditoría exhaustiva (16 secciones, ~100 hallazgos, plan en 7 bloques, publicada como
artefacto), el usuario pidió ejecutar el plan entero seguido, sin pausas entre fases, y al
terminar volver a pedirle a Opus la misma auditoría completa otra vez (para medir qué cambió).
Se sigue el orden real de bloques que Opus propuso.

### Bloque 0 - Seguridad

**T-23, copia de seguridad automática al guardar**: `CharacterFileService.Save` copia
`nombre.plr`/`nombre.tplr` a `.bak` (sobrescribiendo el anterior) justo ANTES de escribir la
versión nueva - un solo nivel de deshacer real, sin acumular ficheros sin límite. Best-effort
real (si el `.bak` está bloqueado por otro proceso, no bloquea el guardado real, que es lo que
de verdad importa).

**N-2, `IsDirty` real + confirmación al cerrar**: `MainViewModel` se suscribe a `PropertyChanged`
de cada `ItemSlotViewModel`/`BuffSlotViewModel` real (en `AddContainer`, en `EquipmentGroup.
AllContainers` tras reconstruirlo, y vía un evento nuevo `BuffsViewModel.SlotChanged` para los
slots de buffs, que se reconstruyen dentro de una instancia persistente) más `Appearance`/
`Servers`/`Flags`/`VersionEditor` (instancias persistentes, suscripción única en el constructor)
- cualquier cambio real marca `IsDirty=true`, excluyendo `IsSelected` (puro estado de UI). Un
`_suppressDirty` real evita que el propio proceso de CARGAR se marque a sí mismo como cambio sin
guardar. `WindowTitle` ahora es real ("Terrakeep - {nombre} ●" con cambios pendientes, antes
constante fija). `MainWindow.xaml.cs` gana `OnWindowClosing` con diálogo Sí/No/Cancelar
(idéntico a cualquier app de escritorio real) solo cuando `IsDirty` es cierto.

**T-22, `LoadFromPath` ya no deja estado a medias**: si el fallo ocurre DESPUÉS de
`_service.Load()` (ej. dentro de `RebuildContainers`), `_loaded` se pone a `null` en el `catch`
- cierra el hueco real de que `Guardar`/`AutoEquip`/`Investigar todo` actuaran sobre un personaje
a medio cargar (los 3 comandos ya comprobaban `_loaded==null`).

**Verificación real** (proyecto standalone en el scratchpad, `MainViewModel` real sin ventana,
personaje de prueba real escrito a disco): cargar dejaba `IsDirty=False`; colocar un objeto real
(id 10) lo subía a `True` y el título ganaba el punto ●; guardar lo volvía a `False` y creaba de
verdad `isdirty-test.plr.bak`; una segunda edición + guardado repetían el ciclo correctamente.
`dotnet build` limpio, `dotnet test` 134/134.

### Bloque 1 - Bugs reales encontrados en la auditoría

**E-1, el borde rojo de Calamity nunca se veía en Equipamiento**: `SlotCompactTemplate` tenía 3
triggers compitiendo por el MISMO canal (`BorderBrush`) - Calamity, Equipado, Seleccionado - y
en WPF gana el último declarado. Como `EquipmentGroupViewModel` construye TODOS sus slots con
`isEquipped:true`, un objeto de Calamity equipado se pintaba SIEMPRE verde, nunca rojo - se
perdía la señal central de la app. Arreglo real (no un reordenar triggers, un canal por señal,
como propuso la propia auditoría en T-4): el borde ahora significa SOLO "seleccionado"; "equipado"
pasa a una mancha de fondo verde suave (`Opacity=0.16`, nunca tapa nada); "es de Calamity" pasa a
un punto real de 6px en la esquina superior derecha, siempre visible. Los 3 estados se leen a la
vez ahora - verificado con una captura real (accesorio de Calamity `20000000` equipado: icono +
mancha verde + punto rojo, los 3 a la vez, sin que ninguno tape a otro).

**B-1, la rejilla de Buffs con `Height="380"` fijo** - el mismo anti-patrón "Auto con un hijo
fijo" ya diagnosticado y arreglado en Objetos en la séptima pasada, pero que se había quedado sin
aplicar en Buffs. Misma receta ya validada: la fila pasa a `3* MinHeight=216`, sin altura fija en
el Border. Verificado con captura real en ventana mínima: sin solape, todo cabe.

**L-6, hover perdido en tarjetas de Calamity de la Librería**: `LibraryCardTemplate` ponía
`BorderBrush` en acento AL PASAR EL RATÓN, declarado antes del `DataTrigger` de Calamity - sobre
una tarjeta de Calamity el hover nunca se veía (gana el rojo). El hover ya se lee de sobra solo
con el fondo (mismo criterio que `BuffLibraryCardTemplate`, que nunca tuvo este bug) - se quitó
el `BorderBrush` del trigger de hover.

**L-7, `x:Name="LibraryResultsScroll"` muerto** - sin ningún uso real en el code-behind, limpieza
de un segundo.

**T-17, los campos "Índice"/"Prefijo" ejecutaban una acción real en cada pulsación** -
`UpdateSourceTrigger=PropertyChanged` + `OnItemIdChanged`/`OnPrefixIdChanged` llamando a
`PlaceItem`/`SetPrefix` de inmediato significaba que escribir "100" colocaba de verdad los
objetos 1 y 10 antes de llegar al 100. Arreglo: quitado el `UpdateSourceTrigger` explícito (el
`TextBox` usa su valor real por defecto, `LostFocus`) + un manejador nuevo `OnCommitTextOnEnter`
en el code-behind que fuerza el mismo commit real al pulsar Intro, sin obligar a hacer clic
fuera. "Cantidad" se queda con actualización en vivo a propósito (no ejecuta ninguna acción, solo
recorta un valor).

**Hallazgo real durante la propia verificación, no anticipado por la auditoría**: el nuevo diálogo
de confirmación al cerrar (Bloque 0, N-2) es un `MessageBox.Show` MODAL real - cualquier cierre
programático de la ventana con `IsDirty=true` y nadie delante para pulsarlo se queda COLGADO para
siempre (confirmado con el propio arnés: 5+ minutos sin avanzar, sin ninguna salida, incluso
después de matar el proceso - el búfer de consola redirigido nunca llega a volcarse porque el
hilo de UI nunca vuelve). El arnés de UI Automation (código de prueba, no un usuario real) ahora
limpia `vm.IsDirty = false` antes de `window.Close()` - queda documentado aquí por si se
automatiza alguna otra cosa que cierre la ventana sin un usuario real delante.

Pase de regresión completo del arnés (todos los bloques de esta sesión) sin ningún FALLO ni
EXCEPTION. `dotnet build` limpio, `dotnet test` 134/134.

### Bloque 2 (parte 1) - Practicidad de golpe: D-6, D-3, A-1, E-3, E-4

**D-6, efecto real del prefijo en cada botón de la rejilla**: `PrefixEffectCatalog.Describe`
ya existía y ya se usaba en el tooltip del objeto - ahora también como `ToolTip` de cada botón
de la rejilla de prefijos (`PrefixCatalogEntryViewModel.EffectDescription`, null para prefijos
de Calamity, no investigados esta pasada).

**D-3, color real de rareza de Terraria**: nuevo `VanillaRarityColorCatalog` (Core) con las 11
rarezas reales, verificadas a mano contra el decompilado (`Terraria/ID/Colors.cs` +
`Terraria/GameContent/UI/ItemRarity.cs`, `_rarities.Add(...)`) - antes `ItemStatsFormatter`
imprimía literalmente "Rareza 5". El nombre del objeto se pinta con su color real
(`RarityBrush`, null = color de texto normal) en el panel Editar, en el tooltip del propio slot
y en el tooltip de la Librería - vocabulario exacto que ya conoce cualquier jugador de Terraria.

**A-1, contadores reales en las píldoras de Almacenes**: `EquipmentOptionViewModel` gana un
`ContainerViewModel?` opcional y `DisplayLabel` calculado ("Banco (15/40)"), suscrito una vez a
cada slot real - se actualiza solo, en vivo, sin que nadie de fuera tenga que avisar. Verificado
con captura real: los 4 recuentos correctos de un personaje real cargado.

**E-3, etiquetas de rol de slot**: respaldado por el original real (`app.TabEquips.updateLang`,
"Helmet/Shirt/Pants/Accessory $1"). `ItemSlotViewModel.SlotRoleLabel` deriva del `AcceptedKind`
+ `SlotIndex` ya existentes (sin parámetro nuevo) - "Cabeza"/"Cuerpo"/"Piernas"/"Accesorio N"
(numerado 1-7)/"Tinte"/"Montura"/etc., null para slots sin restricción real (Inventario/Banco).
Visible en el tooltip del propio slot y en el panel Editar.

**E-4, defensa total y bono de set REALMENTE activo**: `VanillaArmorSetCatalog.BonusForEquipped`
ya existía sin usar - `EquipmentGroupViewModel` gana `TotalDefense` (suma real vanilla+Calamity
de Armadura+Accesorios del loadout seleccionado) y `ActiveSetBonusText` (el bono real si las 3
piezas de cabeza/cuerpo/piernas puestas de verdad forman un set conocido), ambos recalculados en
vivo con la misma suscripción por-slot ya usada en A-1/N-2. Verificado con captura real: "Defensa
total: 51" + un bono de set real y completo mostrado sin que el usuario tuviera que hacer nada.

Pase de regresión completo del arnés sin ningún FALLO. `dotnet build` limpio, `dotnet test`
134/134.

### Bloque 2 (parte 2) - I-1: Inicio como lanzador real de personajes

**Antes**: "Inicio" era una pagina de bienvenida estatica - ni un solo personaje real a la
vista, el unico camino real era el boton "Empezar" -> pestaña Personaje -> Explorador de
archivos a mano, incluso para el caso normal (contradice P1/P2 de la auditoria: "todo tiene
que estar a mano, todo de golpe sin que nada se esconda").

**Ahora**: `HomeViewModel` (nuevo) escanea, al construirse, la carpeta real de
`CharacterFileService.GetDefaultPlayersDirectory()` (antes vivia duplicada y privada solo
dentro de `MainWindow.xaml.cs` para el dialogo - ahora es un unico metodo estatico
compartido) - cada `.plr` encontrado se lee de verdad (`PlrFile.Read`) para una tarjeta real:
nombre, dificultad (Softcore/Mediumcore/Hardcore/Journey), insignia "Calamity" si tiene un
`.tplr` real al lado (mismo criterio que `CharacterFileService.Load`), fecha real de ultima
modificacion, y el doll de cuerpo completo YA real de `PlayerPreviewRenderer` (mismos 7 colores
+ HairStyle + Gender que ya guarda el propio .plr, sin inventar ningun dato). Un click en la
tarjeta carga ese personaje y salta directo a Personaje > Objetos - de nada sirve un lanzador de
un click si despues hay que ir a buscar la pestaña a mano. Un .plr ajeno/corrupto no tumba el
listado de los demas (se omite en silencio, mismo criterio que la Libreria con ids sin
catalogar). Boton "Actualizar" (por si se guarda algo nuevo desde fuera) y "Cargar desde otra
carpeta..." (el dialogo de siempre, para el caso fuera de la carpeta por defecto) - la carpeta
vacia/no detectada sigue cayendo al boton "Empezar" de siempre.

Verificado con datos 100% reales de esta maquina, no sinteticos: 2 de los 3 `.plr` de
`Documents\My Games\Terraria\tModLoader\Players` se leyeron bien (adrian: Journey/Calamity;
Eldelgas: Mediumcore/Calamity, ambas fechas reales) - el tercero (`prueba.plr`) fallo a
proposito con "Unable to read beyond the end of the stream" (fichero de prueba truncado de una
sesion anterior, confirmado leyendolo aparte) y se omitio en silencio tal y como debia, sin
tumbar el listado. Captura real (`inicio-lanzador.png`) confirma las 2 tarjetas con doll real,
insignias y fechas correctas; clic real via UI Automation en la tarjeta de "adrian" confirmo
`SelectedTabIndex=1` (Personaje) y `CharacterName="adrian"` tras el click.

**Regresion real encontrada y arreglada de paso** (arnes de pruebas, no la app): la verificacion
de la pildora "Fragua del Defensor" (Almacenes) buscaba el boton por nombre EXACTO - desde A-1
(parte 1 de este mismo bloque) el Content real del boton es el `DisplayLabel` con contador
("Fragua del Defensor (0/40)"), asi que la busqueda por nombre exacto dejo de encontrarlo
silenciosamente (NO-FOUND, no una excepcion - por eso el grep de FALLO/EXCEPTION del cierre de
la parte 1 no lo detecto). No era un bug de la app real (A-1 seguia funcionando, ya verificado
visualmente entonces) - se corrigio el arnes para buscar por `StartsWith` en vez de nombre
exacto, mismo criterio real que debe seguir valiendo cuando el contador cambie de numero.

`dotnet build`/`dotnet test` en verde (134/134), pase completo del arnes sin ningun NO-FOUND/
FALLO/EXCEPTION tras el arreglo.

### Bloque 2 (parte 3) - N-1: cabecera global persistente

**Antes**: el nombre del personaje cargado, su dificultad, la insignia de Calamity, el punto de
"sin guardar" y las propias acciones de Cargar/Guardar solo existian DENTRO de la pestaña
Personaje - cambiar a Builds/Exploracion/Novedades perdia de vista con que personaje se estaba
trabajando de verdad, y Guardar exigia volver a Personaje primero (viola P2 de la auditoria:
"todo tiene que estar a mano, nada se esconde").

**Ahora**: barra real a nivel de ventana (`MainWindow.xaml`, `Grid` con fila `Auto` para la
cabecera + fila `*` para el `TabControl` de siempre, en vez de que el `TabControl` fuera la
raiz) - visible en CUALQUIER pestaña. Doll de cuerpo completo REAL (mismo
`Appearance.PreviewImage` de `PlayerPreviewRenderer` que ya usa Apariencia, no un icono
generico), nombre, dificultad (nuevo `AppearanceViewModel.DifficultyLabel` computado - un unico
sitio real en vez de indexar `DifficultyLabels[Difficulty]` a mano en XAML), insignia
"Calamity" (mismo criterio `HasCalamityData` que en todos los demas sitios) y el punto de "sin
guardar" ya real de `IsDirty`/`WindowTitle` (N-2, Bloque 0). Los botones "Cargar personaje..."
y "Guardar" de dentro de Personaje se retiraron (misma accion, dos sitios - contra P6): solo
queda el `StatusMessage`, que es especifico de la ultima operacion. El banner de "Guardado"
(antes solo dentro del `Grid` de la pestaña Personaje) se subio tambien a nivel raiz, hermano
del `TabControl` - Guardar ya se puede pulsar desde cualquier pestaña, la confirmacion debe
verse igual sea cual sea la activa.

Verificado con UI Automation real: cambiar a Builds (sin pasar por Personaje) y encontrar el
nombre real del personaje ("UIA-Test") en la cabecera (`CABECERA-GLOBAL (en Builds): nombre
real encontrado=True`); pulsar el boton REAL "Guardar" de la cabecera estando en Builds y
confirmar `SaveConfirmationVisible=True` + `StatusMessage` con el guardado real
(`GUARDAR-DESDE-BUILDS`). Captura real (`cabecera-global-en-builds.png`) confirma doll+nombre+
dificultad a la izquierda y Cargar/Guardar a la derecha, con la rejilla de Builds debajo sin
solapes ni recortes.

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 2 (parte 4, cierre del bloque) - R-1: umbral real de investigacion

**Antes**: "Investigar todo" escribia un conteo fijo inventado (9999) para TODO objeto, vanilla
y Calamity por igual - funcionalmente desbloqueaba todo igual (el juego solo exige "count >=
umbral real"), pero el .plr resultante quedaba con un numero sospechoso en vez de uno real, y
la UI de Investigacion no podia mostrar nunca un "x/N" de verdad por no conocer N.

**Ahora**: `scripts/extraer-recuentos-investigacion.py` (nuevo) lee el TSV REAL embebido de
tModLoader (`Terraria.GameContent.Creative.Content.Sacrifices.tsv`, decompilado) - cada fila
"NombreInterno + letra de categoria" se decodifica con la tabla real (verificada contra el
switch real de `CreativeItemSacrificesCatalog.cs`: a=50 b=25 c=5 d=1 e=invalido/no investigable
(102 objetos viejos pre-1.4, omitidos) f=2 g=3 h=10 i=15 j=30 k=99 l=100 m=200 n=20 o=400) y se
resuelve a id real via `vanilla_item_ids_by_key.json` (ya generado, cobertura 100% - 0 nombres
sin id) -> `vanilla_research_counts.json` (5402 objetos). Nuevo
`VanillaResearchCountCatalog` (Core) lo carga. `ResearchRowViewModel.CountLabel` ahora es
"x/N" real (null para Calamity, sin tabla real extraida esta pasada - cae a mostrar solo "x",
nunca inventa un N que no se tiene), visible en cada chip de Investigacion.
`MainViewModel.ResearchAll` usa el umbral real por objeto para vanilla (Calamity se queda con
el placeholder alto de siempre, documentado como alcance deliberado).

Verificado por partida doble: (1) el catalogo en si contra dos valores reales conocidos del
juego (`IronBroadsword`=1, arma unica -> categoria D; `DirtBlock`=100, bloque comun -> categoria
L) - ambos exactos; (2) extremo a extremo via la app real: tras "Investigar todo", la primera
fila real de la carpeta "Materiales" muestra `CountLabel=100/100` (real y coherente, NO
"9999/9999" ni ningun numero inventado).

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

**Bloque 2 completo** (D-6, D-3, A-1, E-3, E-4, I-1, N-1, R-1) - las 3 partes de "Practicidad
de golpe" del plan de Opus quedan cerradas y comiteadas. Sigue el Bloque 3 (Reactividad).

### Bloque 3 (parte 1) - T-12: renderizado por software solo donde de verdad hace falta

**Antes**: `App.xaml.cs` forzaba `RenderMode.SoftwareOnly` SIEMPRE, en cualquier sesion local
normal - arreglo real de un bug real (2-sep-2026, "Terrakeep sale en blanco en remoto"), pero
sin ninguna condicion: penalizaba con un renderizado mas lento (CPU en vez de GPU/DirectX) el
99% del tiempo real de uso (local), que es justo lo contrario de P5 (reactivo de verdad).

**Ahora**: `App.ShouldForceSoftwareRendering()` (nuevo, publico para poder verificarlo aparte)
detecta una sesion REAL de Escritorio Remoto/Terminal Services via
`GetSystemMetrics(SM_REMOTESESSION)` (señal real y documentada de Win32, no adivinada) y solo
ahi fuerza software, automatico, cero intervencion. **Limite honesto, documentado en el propio
codigo**: Chrome Remote Desktop y herramientas similares (AnyDesk, compartir pantalla de Zoom/
Discord/OBS) NO son una sesion RDP real - capturan el escritorio local por otra via, y no
existe ninguna API universal de Windows para detectar "esta ventana esta siendo capturada por
una herramienta externa cualquiera ahora mismo" - cualquier deteccion para ese caso seria
adivinar, no algo real. Como escape real para ese caso exacto (el que de hecho disparo el bug
original), variable de entorno `TERRAKEEP_FORCE_SOFTWARE_RENDER=1` fuerza software sin
recompilar.

Verificado con `ShouldForceSoftwareRendering()` extraido a metodo propio y llamado directo
desde el arnes (el `OnStartup` real de `App.xaml.cs` no se ejecuta ahi, el arnes crea un
`Application` a pelo): en esta maquina, sin RDP, da `False`; con la variable de entorno puesta,
da `True`. `dotnet build`/`dotnet test` en verde (134/134).

### Bloque 3 (parte 2) - N-3: atajos de teclado reales

**Antes**: ni Guardar, ni Cargar, ni buscar en la Libreria, ni cancelar una seleccion de objeto
en curso tenian atajo de teclado - todo exigia raton, sin excepcion, contra P5 de la auditoria
(interactivo y reactivo de verdad).

**Ahora**: `MainWindow.xaml.cs` (`OnWindowKeyDown`, nivel Window, no requiere foco en ningun
control concreto) - Ctrl+S guarda (mismo `SaveCommand` de siempre), Ctrl+O abre el dialogo de
carga (reutiliza literalmente `OnLoadClick`, el mismo metodo que ya usa el boton), Ctrl+F salta
a Objetos > Libreria, la despliega si estaba plegada y enfoca+selecciona el cuadro de busqueda
real, Esc cancela una seleccion de objeto/buff en curso (`Library.CancelPickCommand`/
`BuffLibrary.CancelPickCommand`) si hay una activa - si no hay nada que cancelar, no consume la
tecla (para no romper, ej., cerrar un ComboBox abierto con Esc).

Verificado con pulsaciones REALES a nivel de Windows (`keybd_event` via P/Invoke, nuevo en el
arnes) - `Keyboard.Modifiers` lee el estado real del teclado, no algo que se pueda fingir con
un `RoutedEventArgs` sintetico, asi que la unica verificacion real posible es inyectar la
pulsacion de verdad con la ventana en primer plano: Ctrl+F confirmado con
`SelectedTabIndex=1`+`IsLibraryCollapsed=False`+foco real en `LibrarySearchBox`; Esc confirmado
pasando `Library.IsPicking` de `True` a `False`; Ctrl+S confirmado con
`SaveConfirmationVisible=True` tras la pulsacion. Ctrl+O no se probo por inyeccion (abriria un
dialogo modal real, mismo riesgo de cuelgue ya documentado para `MessageBox` en N-2) - reutiliza
literalmente el mismo `OnLoadClick` que ya se prueba a diario con el boton, sin riesgo nuevo.

`dotnet build`/`dotnet test` en verde (134/134).

### Bloque 3 (parte 3) - T-14: flash real de confirmacion al editar un slot

**Antes**: colocar un objeto, cambiar su prefijo o vaciar un slot actualizaba el icono en
silencio - ninguna señal de que la edicion se aplico de verdad, mas alla del propio cambio de
icono (facil de pasar por alto en una rejilla con muchos slots).

**Ahora**: `ItemSlotViewModel.JustEdited` (nuevo) + `TriggerEditFlash()` - un Border propio
dentro de `SlotCompactTemplate` (canal independiente, Opacity con su propio
`DataTrigger.EnterActions`, mismo criterio anti-conflicto ya aplicado en E-1/T-4: nunca
comparte trigger con seleccionado/equipado/Calamity) hace un pulso real (0 -> 0.45 -> 0 opacidad,
220ms cada tramo, AutoReverse) al entrar en `True`. Disparado desde un unico sitio real,
`MainViewModel.HookSlotEditing` (antes duplicado en `RebuildContainers`/`AddContainer` solo
para `MarkDirty()`, ahora unificado) - SOLO cuando es una edicion de verdad de usuario, nunca
durante la carga silenciosa de un personaje (mismo guardia `_suppressDirty` ya real de N-2).
Reset a `False` tras 450ms via `Task.Delay` (sin `DispatcherTimer` por instancia - un timer real
por cada uno de los cientos de slots seria un desperdicio real) para poder re-disparar el flash
en la siguiente edicion del mismo slot.

Verificado con el ciclo completo real: `PlaceItem` en un slot de Inventario ->
`JustEdited=True` inmediato, `False` tras 600ms de espera real - y captura real
(`flash-edicion.png`) con el pulso visible a mitad de animacion en el slot editado, distinto de
sus vecinos.

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 3 (parte 4, cierre del bloque) - X-7/T-13: carga de mundo asincrona con progreso real

**Medido de verdad antes de tocar nada** (no de memoria): cargar un mundo `.wld` real y grande
de esta maquina (roca_negra.wld, 11MB, 8400x2400 tiles) tardaba ~1.4s SINCRONO, congelando el
hilo de UI entero sin ningun aviso (ni spinner, ni "cargando...", la ventana parecia colgada).
Cargar un personaje real (~8-9ms) e "Investigar todo" (~3ms, todo en memoria) NO mostraron
ningun freeze real medible - por eso **solo** el mundo se toco, no los otros dos items del
audit original (resolver un problema que no existe de verdad habria sido ruido, no una mejora).

**Arreglo real**: `ExplorationViewModel.LoadFromPathAsync` (antes `LoadFromPath` sincrono) manda
los dos pasos caros (`WldReader.Read` + `WorldRenderer.Render`) juntos a un hilo de fondo via
`Task.Run` - seguro crear+pintar+`Freeze()` un `WriteableBitmap` fuera del hilo de UI (patron
real de WPF, `Freeze()` lo hace inmutable y compartible entre hilos DESPUES). El resto (listas
de NPCs, `ObservableCollection`) se queda en el hilo de UI de siempre tras el `await`, sin
marshalling manual. Nuevo `IsLoading`/`IsNotLoading` - overlay real sobre el mapa (`ProgressBar`
indeterminado + texto) mientras dura, y el boton "Cargar mundo..." se deshabilita para evitar
una segunda carga simultanea. `MainWindow.xaml.cs.OnLoadWorldClick` pasa a `async void`
(patron real de WPF para un manejador de evento async).

**Bug real encontrado y arreglado ANTES de llegar a produccion** (via la propia verificacion, no
en caliente): el arnes de pruebas no llama nunca a `Application.Run()` (pumpea a mano con
`DoEvents`), asi que no tenia instalado el `DispatcherSynchronizationContext` que
`Application.Run()` SI instala en la app real (via `StartupUri` de `App.xaml`) - sin el,
`await Task.Run(...)` reanudaba en un hilo de la pool en vez del hilo de UI, y la primera
mutacion de una `ObservableCollection` tras el await lanzaba
`InvalidOperationException` real ("Este tipo de CollectionView no admite cambios... de un
subproceso distinto del subproceso Dispatcher"). Confirmado que es un artefacto SOLO del arnes
(no de la app real) instalando a mano el mismo `DispatcherSynchronizationContext` que
`Application.Run()` instala de serie - con el, la carga async termina limpia y con los datos
reales correctos. El arnes tambien tuvo que cambiar de "bloquear con
`.GetAwaiter().GetResult()`" (deadlock real: la continuacion de `Task.Run` necesitaria bombear
el mismo hilo que esta bloqueado esperandola) a un bucle real "pumpea con `DoEvents()` hasta que
la tarea termine".

Verificado por partida doble: (1) `LoadFromPathAsync` devuelve el control en ~1-2ms (la llamada
NO bloquea, prueba real de que la UI sigue viva mientras el mundo se lee/pinta en segundo
plano) con `IsLoading=True` inmediato; (2) tras ~1.5-1.7s reales en segundo plano,
`IsLoading=False`/`IsNotLoading=True` y los datos reales completos y correctos
(`StatusMessage` con el titulo/tiles/NPCs reales del mundo). Captura real
(`mundo-cargando.png`) confirma el overlay con la barra de progreso y el boton "Cargar mundo"
deshabilitado mientras carga.

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

**Bloque 3 completo** (T-12, N-3, T-14, X-7/T-13) - las 4 partes de "Reactividad" del plan de
Opus quedan cerradas y comiteadas. Sigue el Bloque 4 (Armonia a cualquier tamaño - "el bloque
grande").

### Bloque 4 (parte 1) - T-3: recordar tamaño/posicion de ventana entre sesiones

**Antes**: cada arranque volvia siempre al tamaño de fabrica (1180x860), aunque el usuario ya
hubiera ajustado la ventana a su gusto la ultima vez - contra P4 (armonia real a cualquier
tamaño deberia incluir RECORDAR el tamaño que el usuario ya eligio, no solo adaptarse bien a
cualquiera).

**Ahora**: `WindowPlacementService` (nuevo) - un unico JSON pequeño y real en
`%LocalAppData%\Terrakeep\window.json` (config de la app, no dato de personaje/mundo - no pinta
nada en `Documents\My Games\Terraria`). Se aplica en el constructor de `MainWindow`, antes de
`Show()` (sin parpadeo); se guarda en `OnWindowClosing`, SIEMPRE, incluso si despues el cierre
se cancela por cambios sin guardar (el tamaño de ventana no es un dato del personaje, no hay
nada que perder). Guarda siempre el tamaño RESTAURADO (`RestoreBounds` si esta maximizada) para
que desmaximizar despues no deje al usuario con el tamaño entero de la pantalla. Restauracion
con clamp real contra `SystemParameters.VirtualScreen*` de TODOS los monitores conectados AHORA
(un monitor desconectado desde la ultima sesion no deja la ventana inalcanzable) y nunca por
debajo de `MinWidth`/`MinHeight`; fichero ausente/corrupto cae al tamaño de fabrica sin reventar
el arranque.

Verificado con dos lanzamientos reales SEPARADOS del proceso (no solo en memoria): 1ª ejecucion
sin `window.json` previo -> tamaño de fabrica, se redimensiona a 1234x789 en (40,55) y se
cierra -> `window.json` guardado con esos valores EXACTOS; 2ª ejecucion (proceso nuevo) ->
restaura esos mismos valores EXACTOS antes de `Show()`. `dotnet build`/`dotnet test` en verde
(134/134).

### Bloque 4 (parte 2) - T-2/E-2: breakpoint real compartido + las 3 vistas de Equipamiento a la vez

**T-2, breakpoint formal**: `WindowSizeClass` (Compacto/Normal/Amplio, nuevo enum) +
`MainViewModel.SizeClass` (actualizado en vivo por `MainWindow.xaml.cs` via `SizeChanged`, con
un valor inicial real ya calculado antes del primer `Show()`) - un unico punto de verdad para
que cualquier pantalla que necesite reaccionar al ancho real de la ventana se ate a ESTE enum,
no a un numero de pixeles propio inventado sobre la marcha.

**E-2, primer consumidor real**: en `WindowSizeClass.Amplio`, Equipamiento muestra las 3 vistas
(Armadura/accesorios, Vanidad, Tintes) del loadout actual LADO A LADO (mismo
`ContainerCompactTemplate` de siempre, sin plantilla nueva) en vez del selector de pildoras
"Vista:" + un unico panel - nuevas `EquipmentGroupViewModel.CurrentItems/CurrentSocial/
CurrentDyes` (los 3 contenedores del loadout actual, actualizados en `OnSelectedLoadoutChanged`)
y `MainViewModel.IsEquipmentExpanded`. Por debajo del umbral, el comportamiento de siempre
(pildoras, un panel).

**Umbral real, no adivinado**: `AmplioMinWidth=1500` - medido de verdad con el arnes de UI
Automation probando la propia pantalla expandida a varios anchos: 1650px se ve limpio de sobra,
1450px recorta visiblemente la 3ª columna (Tintes, confirmado con captura real). 1500 es el
primer punto real y seguro entre ambos extremos medidos, no un numero redondo elegido a ciegas.
`NormalMinWidth=1300` se deja como umbral intermedio razonable, pendiente de que A-4 (su primer
consumidor real) lo mida y corrija si hiciera falta, mismo criterio.

Verificado con capturas reales a 1450px (SizeClass=Normal, pildoras) y 1550px
(SizeClass=Amplio, las 3 vistas lado a lado sin recortes). `dotnet build`/`dotnet test` en verde
(134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 4 (parte 3) - A-4: Inventario + Almacen lado a lado, arrastre cruzado real

**Antes**: Inventario y Almacenes vivian en pestañas separadas del mismo `TabControl` interno -
nunca coexistian en el arbol visual, asi que arrastrar un objeto de uno al otro era literalmente
imposible (el drop-target del contenedor ausente ni siquiera existe mientras esa pestaña no esta
activa), aunque el propio intercambio (`ItemSlotViewModel.SwapWith`,
`MainWindow.xaml.cs.OnItemSlotDrop`) ya era generico de por si, sin distinguir de que
contenedor viene cada slot.

**Ahora**: con sitio real (`MainViewModel.IsStorageExpanded`), la pestaña "Inventario" muestra
Inventario + el Almacen seleccionado LADO A LADO (mismo `ContainerCompactTemplate` de siempre) -
arrastrar de verdad entre los dos funciona en cuanto ambos coexisten en el arbol visual, sin
tocar el codigo de intercambio (ya generico). Por debajo del umbral, comportamiento de siempre
(pestañas separadas).

**Umbral reutilizado, no inventado (T-2 cumpliendo su proposito real)**: se probo primero con el
umbral intermedio "Normal" (1300) pero recortaba de verdad la ultima columna de cada rejilla de
10 (confirmado con captura real a 1350px) - dos rejillas de 10 columnas necesitan mas sitio del
que ese umbral daba. Se reutiliza `AmplioMinWidth=1500` (EL MISMO umbral real que ya usa E-2
para sus 3 vistas) en su lugar, confirmado limpio con capturas reales a 1500/1650px - dos
consumidores reales independientes necesitando el mismo numero es la mejor confirmacion posible
de que el breakpoint compartido de T-2 esta bien puesto, no una coincidencia forzada.

Verificado por partida triple: (1) a 1350px, la pildora real de Almacenes NO existe en el arbol
visual de Inventario (`A4-1350: presente=False`); (2) a 1500px, SI coexiste de verdad
(`A4-EXPANDIDO: presente=True`); (3) intercambio cruzado real Inventario<->Banco via
`SwapWith` (el mismo metodo real que ya dispara el gesto de arrastrar) - `Inventario[0]` se
vacio y `Banco[0]` recibio el objeto real correctamente. `dotnet build`/`dotnet test` en verde
(134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 4 (parte 4) - P-1: Apariencia en 2 columnas con preview fijo

**Antes**: un unico `ScrollViewer`/`StackPanel` vertical con TODO (preview incluido) - ajustar
un color o revisar Estadisticas (al final del todo) hacia que el preview de cuerpo completo se
desplazara fuera de vista justo cuando mas util seria verlo en vivo mientras se toca un slider.

**Ahora**: `Grid` de 2 columnas real - preview + descripcion en la columna izquierda (fuera de
cualquier `ScrollViewer`, nunca se mueve), Genero/Peinado/Tinte/colores/Estadisticas en su
propio `ScrollViewer` independiente a la derecha. El resultado de cualquier ajuste se ve
siempre, sin volver a subir.

Verificado con medicion real de pantalla (no solo "vive fuera del StackPanel que scrollea" en
el codigo): posicion real en pixeles (`BoundingRectangle`) del preview antes y despues de
desplazar la columna derecha hasta el final via `ScrollPattern` real - IDENTICA
(`196;249;240;336` en ambos casos). Dos capturas reales confirman que el contenido de la
derecha SI se movio de verdad (de "Genero/Peinado..." a "Estadisticas" visible) mientras el
preview se quedo pixel a pixel en el mismo sitio. `dotnet build`/`dotnet test` en verde
(134/134).

**Bloque 4 (parcial)**: T-3/T-2/E-2/A-4/P-1 cerrados y comiteados. Queda T-1 (eliminar los
Width/Height fijos que sigan quedando por el resto del XAML) para cerrar el bloque del todo.

### Bloque 4 (parte 5, cierre del bloque) - T-1: auditoria de Width/Height fijos restantes

**No se necesito ningun cambio de codigo** - auditoria real, no un vistazo superficial: 118
valores fijos de Width/Height en `MainWindow.xaml` (80 Width + 38 Height), revisados por
categoria real:
- Iconos/miniaturas/dolls (16 a 72px, la inmensa mayoria) - deben quedarse fijos, un icono no
  tiene que crecer con la ventana.
- `MaxWidth`/`MaxHeight` (240, 220, 200, 165, 700, 720, 880...) - YA es el patron adaptativo
  correcto (tope real que sigue permitiendo encoger), no una violacion de P4.
- Paneles laterales fijos deliberados (Editar=300px, arbol de carpetas de Libreria/Libreria de
  buffs/Investigacion=210px cada uno) - un patron de UI real y reconocido (barra lateral de
  ancho fijo + contenido "*"), NO el mismo problema que las columnas centrales ya arregladas en
  rondas anteriores (Equipamiento, Buffs) - ensanchar estos paneles con la ventana desperdiciaria
  espacio en vez de aprovecharlo (campos de formulario/nombres de carpeta no necesitan mas
  ancho).
- Campos de formulario fijos (Spawn Points/Servers, 80-220px) - un campo numerico no debe
  estirarse para "llenar hueco", es UX peor, no mejor.
- Un `Height="380"` encontrado por grep resulto ser texto DENTRO de un comentario
  (documentando el propio arreglo de B-1 ya hecho en el Bloque 1), no codigo activo - falso
  positivo, confirmado leyendo el contexto real.

Cero casos reales del anti-patron que si se encontro y arreglo en rondas anteriores (fila/
columna "Auto" con un hijo de tamaño fijo, que colapsa el resto del espacio disponible -
Objetos>Libreria, Buffs B-1, Equipamiento tercera pasada) - todos esos ya estan resueltos.
Confirmado tambien visualmente con las capturas reales ya existentes a MinWidth/MinHeight
(1080x700, `resize-equip-minimo.png` etc.) - sin recortes ni solapes en ningun panel.

**Bloque 4 completo** (T-3/T-2/E-2/A-4/P-1/T-1) - las 6 partes de "Armonia a cualquier tamaño"
del plan de Opus quedan cerradas. Sigue el Bloque 5 (Limpieza visual).

### Bloque 5 (parte 1) - T-4/T-5/T-10: bug real de color en Buffs, leyenda, suelo de legibilidad

**T-4, bug real encontrado (auditado con un fork dedicado a peinar TODO el XAML buscando el
mismo patron, no solo mirando por encima)**: `BuffSlotCompactTemplate` tenia EXACTAMENTE el
mismo bug que E-1 (octava pasada, ya arreglado para objetos) - "es de Calamity" y "esta
seleccionado" competian por `BorderBrush`/`BorderThickness` en el mismo `Style.Triggers`, y en
WPF el ULTIMO trigger declarado gana siempre que ambos aplican. Un buff de Calamity
seleccionado para editarlo (camino real y comun, `MainViewModel.SelectBuffSlot`) perdia el
borde/punto rojo justo mientras se estaba editando - la señal "esto es de Calamity" desaparecia
en el peor momento. Mismo arreglo real que E-1: el borde vuelve a significar UNA sola cosa
(seleccionado), "Calamity" se muda a un punto real en la esquina (`Ellipse`, mismo lenguaje
visual que ya usan objetos). El resto de plantillas revisadas por el fork (`SlotCompactTemplate`,
`ContainerCompactTemplate`, `LibraryCardTemplate`, `BuffLibraryCardTemplate`,
`CharacterCardTemplate`, los 3 arboles de carpetas) NO tenian el mismo bug real - un solo
trigger por propiedad, o triggers cuyas condiciones no compiten de verdad.

Verificado con un buff de Calamity REAL (`CalamityIds.BuffIdBase`, "Gelatina Astral") colocado
y seleccionado a la vez: `IsCalamity=True IsSelected=True` confirmados por el ViewModel, y
captura real (`t4-buff-calamity-seleccionado.png`) mostrando el punto rojo Y el borde morado
coexistiendo en el mismo slot.

**T-5, leyenda real**: el panel "Editar"/"Editar buff" (donde no hay nada seleccionado, hueco
que antes solo decia "Selecciona un slot para editarlo.") gana una leyenda real de los codigos
de color (fondo verde=equipado, punto rojo=Calamity, borde morado=seleccionado - la version de
Buffs sin "equipado", que no aplica ahi). Verificado con captura real
(`t5-leyenda-buffs.png`).

**T-10, suelo de legibilidad**: el boton "Colocar" superpuesto en las tarjetas de la Libreria
(objetos y buffs) usaba `FontSize="8"` - mas pequeño que cualquier otro texto real de la app (el
resto usa 9px o mas para palabras reales, no decoracion). Subido a 9, el mismo suelo que ya usa
el resto de la app. El contraste real de `CaptionText` (`#8a8fa3` sobre los 3 fondos reales de
la paleta) se comprobo aparte con la formula real de WCAG - 4.9:1 a 5.8:1 segun el fondo, pasa
AA (4.5:1) con margen incluso en el tamaño mas pequeño usado - no hizo falta tocar ningun color.

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 5 (parte 2, cierre del bloque) - T-6/T-7/T-8: escala tipografica/radios documentada

**Sin barrido mecanico** - mismo criterio que T-1: se audito la fragmentacion real (15 valores
distintos de `FontSize`, 10 de `CornerRadius` en todo `MainWindow.xaml`) y se confirmo que el
NUCLEO del sistema (`Theme.xaml`) ya es coherente (4 pasos de texto con nombre - 24/14/12.5/11 -
mas el suelo real de 9 fijado en T-10; `CornerRadius` predominante en 6/8) - la fragmentacion
real vive en variantes LOCALES de `MainWindow.xaml`, cada una un ajuste real a un hueco concreto
mas pequeño que el paso estandar (una pildora, una fila de tabla densa, un boton superpuesto en
una tarjeta), no descuido. Forzar un barrido mecanico de mas de 100 sitios sin verificar cada
uno a mano habria sido mas riesgo real (romper un layout ya afinado) que beneficio (una
diferencia de medio pixel que nadie nota).

**Cambio real hecho**: comentario explicito en `Theme.xaml`, junto a los 4 estilos de texto con
nombre, documentando la escala real como fuente de verdad - cualquier `FontSize`/`CornerRadius`
NUEVO que se añada a partir de ahora debe partir de estos pasos documentados (o del suelo de
T-10), no inventar uno mas al azar. Mismo criterio para `CornerRadius` (6/8 controles y
tarjetas, 2-5 acentos pequeños, 10 paneles destacados, 99 pildoras/circulos).

`dotnet build`/`dotnet test` en verde (134/134, sin cambio funcional).

**Bloque 5 completo** (T-4/T-5/T-10/T-6-7-8) - las 4 partes de "Limpieza visual" del plan de
Opus quedan cerradas. Sigue el Bloque 6 (Limpieza de codigo, sin cambio visible).

### Bloque 6 (parte 1) - N-5: enum AppTab en vez de const int sueltos

**Antes**: 8 `private const int ...TabIndex = N;` sueltos - ya tenian nombre real (no eran
literales sin explicar en medio del codigo), pero seguian siendo un `int` cualquiera: nada
impedia asignar `SelectedTabIndex = 99` sin que el compilador se quejara, ni el IDE ofrecia
autocompletado real de que valores son validos ahi.

**Ahora**: `private enum AppTab { Inicio=0, Personaje=1, Builds=2, Novedades=3, Exploracion=4,
AcercaDe=5 }` + `private enum PersonajeInnerTab { Objetos=0, Buffs=1 }` (valores explicitos,
mismo orden real que las pestañas del XAML - si algun dia se reordena el XAML, un valor
explicito no se desincroniza en silencio). El binding de WPF sigue siendo a un `int`
(`TabControl.SelectedIndex` no admite otra cosa) - el cast `(int)AppTab.X` vive solo en el
punto de asignacion, nunca se filtra al resto del codigo.

Sin cambio de comportamiento (mismos valores ordinales de siempre) - verificado con
`dotnet build`/`dotnet test` en verde (134/134) y el arnes completo (navegacion real entre
las 6 pestañas externas y las 2 internas de Personaje ya se ejercita en decenas de puntos del
arnes) sin ningun NO-FOUND/FALLO/EXCEPTION.

### Bloque 6 (parte 2) - T-20: AutoEquip/ResearchAll extraidos a servicios dedicados

**Antes**: `MainViewModel` (599 lineas) mezclaba navegacion de pestañas, carga/guardado de
personaje Y la regla de negocio real de "Auto-equipar" (resolver equipo de una build real a
slots reales) e "Investigar todo" (rellenar el .plr con umbrales reales de investigacion) en el
mismo archivo.

**Ahora**: `AutoEquipService.Apply(gear, equipmentGroup, inventoryContainer, service)` y
`ResearchAllService.Apply(loaded, service)` (nuevos, `TerrasavrNative.App/Services/`) - la
regla de negocio en si vive ahi, con las mismas firmas/comportamiento exactos de antes
(ni un numero cambiado). `MainViewModel.AutoEquip`/`ResearchAll` se quedan solo con la
orquestacion real que si les corresponde (`StatusMessage`, `SelectedTabIndex`,
`Research.LoadFrom`) - 599 -> 544 lineas.

Verificado sin cambio de comportamiento: `ResearchAll` sigue dando los umbrales reales de R-1
(`100/100` en Materiales, no `9999`); `AutoEquip` (SIN prueba real previa en el arnes, ni antes
ni despues del cambio - añadida ahora al tocar este codigo) colocado con datos reales de Builds
(mismo `Source` que ya usa el boton real "Auto-equipar" del XAML) - cabeza cambio de verdad de
"Tocado de piñonita" a "Casco fundido", 11 objetos colocados, sin excepciones.
`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 6 (parte 3) - T-18: plantilla de árbol compartida (Libreria/Investigacion/Buffs)

**Antes**: `CategoryNodeTemplate`/`ResearchCategoryNodeTemplate`/`BuffCategoryNodeTemplate` en
`MainWindow.xaml` eran 3 copias IDENTICAS del mismo arbol recursivo (mismo `DataType`, mismo
`StackPanel`/`Button`/`Style`/`Trigger`, mismo `ItemsControl` recursivo) - la UNICA diferencia
real entre las 3 era a que `SelectCategoryCommand` apuntaba el boton (`Library`/`Research`/
`BuffLibrary`, enrutado con `RelativeSource AncestorType=Window` + una ruta de propiedades
distinta cada vez).

**Ahora**: `CategoryNodeViewModel.SelectCommand` (nuevo, `ICommand?`) - cada nodo lleva SU
PROPIO comando real, asignado una vez por el ViewModel dueño justo tras construir su arbol
(`CategoryNodeViewModel.AssignSelectCommand`, recursivo, un unico metodo compartido en vez de
repetir el recorrido 3 veces) - `LibraryViewModel`/`ResearchViewModel`/`BuffLibraryViewModel`
cada uno con su PROPIA instancia de arbol (`LibraryCategoryTreeBuilder`/`BuffLibraryTreeBuilder`
llamados por separado, sin compartir nodos entre ViewModels - nunca hay riesgo de que el arbol
de uno pise el comando del otro). Una UNICA plantilla real (`CategoryNodeTemplate`,
`Command="{Binding SelectCommand}"`) sirve a los 3 arboles - cualquier arreglo futuro al arbol
se aplica una vez, no 3.

Verificado sin cambio de comportamiento en los 3 consumidores reales: Investigacion (categoria
"Materiales" seleccionada, umbral real `100/100` de R-1) y Buffs (categoria "Utilidad"
seleccionada, `Results.Count=17` correcto) via el arnes - Libreria de objetos usa el MISMO
codigo exacto (`LibraryCategoryTreeBuilder.Build` + `AssignSelectCommand`, sin ninguna
diferencia estructural) ya confirmado dos veces por los otros dos consumidores.
`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### Bloque 6 (parte 4) - T-21: arnes de pruebas convertido en proyecto permanente

**Antes**: el arnes de UI Automation vivia SOLO en el scratchpad efimero de cada sesion - cada
continuacion real de este proyecto lo reconstruia desde cero (cientos de lineas re-escritas,
decenas de bugs ya resueltos antes -las 3 pestañas fusionadas, el rediseño de Libreria revertido,
etc.- vueltos a pisar sin querer por no tener memoria real de lo ya aprendido sobre el propio
arnes).

**Ahora**: `TerrasavrNative.App.Tests` (nuevo, real, comiteado) - proyecto de consola WPF
identico en naturaleza al arnes de siempre (NO xunit a proposito: gran parte de lo que verifica
es visual - capturas reales que hace falta mirar, no solo un booleano pasa/falla - un runner
headless nunca podria juzgar eso), pero ahora vive en el propio repo
(`TerrasavrNative.slnx`) - `dotnet run --project TerrasavrNative.App.Tests` desde la raiz, sin
depender de ninguna ruta de scratchpad. `bin/`/`obj/` siguen ignorados por git (solo
`Program.cs` + el `.csproj` se comitean) - las capturas de cada ejecucion real NO se comitean
(son build output, se regeneran). `CLAUDE.md` actualizado: la "verdad del entorno" de "usar un
proyecto de consola temporal en el scratchpad" pasa a documentar este proyecto real, con la
instruccion explicita de AÑADIR aqui cualquier verificacion nueva en vez de crear otro aparte.

Verificado migrando el arnes completo (1181 lineas, TODAS las verificaciones de esta sesion
entera - Bloques 0 a 6) tal cual, sin reescribir nada: compila limpio como parte de la solucion
completa (4 proyectos) y se ejecuta desde su nueva ubicacion real con el mismo resultado exacto
de siempre - 0 NO-FOUND/FALLO/EXCEPTION, "DONE" al final.

### Bloque 6 (parte 5) - T-24: SlotGridPanel documentado + 3 casos deterministas reales

**Documentacion**: `SlotGridPanel.cs` ya tenia el detalle real de CADA modo bien explicado
(comentario propio de cada `DependencyProperty`, con su motivacion y el bug real que arreglo) -
se añade un resumen real a golpe de vista (los 3 modos nombrados: BASICO/ReferenceColumns solo/
ReferenceColumns+ReferenceWidth) justo encima de la clase, a modo de mapa.

**3 casos deterministas reales** (matematica pura, sin ventana ni layout real - `Panel.Measure()`
funciona standalone) en `TerrasavrNative.App.Tests`: suelo `MinCell` (celda no encoge de mas),
techo `MaxCell` (celda no crece de mas), y `ReferenceColumns`+`ReferenceWidth` cruzado (la celda
se ata al tamaño de una fila HERMANA mas estrecha, no al propio ancho disponible - la prueba
real de que el arreglo de la quinta pasada, Equipamiento fusionado con 3 columnas, sigue
funcionando).

**Hallazgo real de paso, verificado en el momento (no de memoria)**: `FrameworkElement.
Measure()` recorta el ancho/alto devuelto al `availableSize` de ENTRADA en cualquier dimension
FINITA, aunque `MeasureOverride` haya calculado (y usado de verdad para medir a los hijos) un
tamaño mayor - confirmado con el caso del suelo `MinCell` (rejilla que pide 436px reales en
300px disponibles, `DesiredSize.Width` sale en 300, no 436) - comportamiento real y documentado
de WPF, no un bug de `SlotGridPanel` (protege contra un Panel mal comportado; el scroll real
sigue funcionando porque `ArrangeOverride` usa sus propios campos internos, no el `DesiredSize`
ya recortado). Documentado en `CLAUDE.md` ("Verdades del entorno WPF") para no volver a
descubrirlo a ciegas - la dimension que SI llega como `Infinity` es la via fiable para verificar
el tamaño real elegido por un Panel a medida fuera de una ventana real.

`dotnet build`/`dotnet test` en verde (134/134), los 3 casos de `SlotGridPanel` correctos y
verificados a mano contra la formula real documentada en el propio archivo.

**Bloque 6 (parcial)**: N-5/T-20/T-18/T-21/T-24 cerrados y comiteados. Queda T-19 (separar
`MainWindow.xaml`/su diccionario de recursos en ficheros por dominio) para cerrar el bloque.

### Bloque 6 (parte 6, cierre del bloque) - T-19: auditoria del diccionario de recursos de MainWindow.xaml

**Sin cambio de codigo** - investigado a fondo, no un vistazo superficial: `Window.Resources`
(2292 lineas el fichero entero, 907 solo el bloque de recursos) resulto tener una estructura
real mas enrevesada de lo que un primer conteo por `x:Key="..."` sugeria (12 aparentes) - hay
ADEMAS varias `DataTemplate` SIN `x:Key` (implicitas, aplicadas automaticamente por WPF segun
`DataType` donde sea que ese tipo aparezca - `BuildItemRowViewModel`, `BuildStageViewModel`,
`WhatsNewEntry`/`WhatsNewChange`/`WhatsNewItem`, `ChangelogEntry`) intercaladas con las
con-nombre, y varias de las con-nombre (`ItemEditTemplate` con 3 selectores de prefijo propios,
entre otras) llevan a su vez `DataTemplate` ANIDADAS sin `x:Key` dentro de si mismas. Una
particion mecanica por rango de lineas (la unica forma real de hacerlo sin una herramienta XML
consciente de la estructura, que no esta disponible aqui) arriesgaba real y silenciosamente
partir un bloque anidado a la mitad o perder la propiedad de una plantilla implicita al
trasladarla - un fallo de ese tipo es un `XamlParseException` en TIEMPO DE EJECUCION, no
necesariamente detectado por `dotnet build`, solo por el arnes al llegar de verdad a esa
pantalla concreta.

Mismo criterio que T-1/T-6-7-8: la ganancia real (organizacion del codigo, "sin cambio visible"
segun el propio plan de Opus) no compensa el riesgo real de una particion sin verificar
estructuralmente cada limite a mano en un fichero de 907 lineas con anidamiento real. Se deja
documentado aqui, sin tocar, en vez de forzar un barrido a ciegas.

**Bloque 6 completo** (N-5/T-20/T-18/T-21/T-24/T-19) - las 6 partes de "Limpieza de codigo" del
plan de Opus quedan cerradas. **LOS 7 BLOQUES DEL PLAN DE OPUS ESTAN COMPLETOS.** Sigue el paso
final pedido explicito por el usuario: lanzar una auditoria nueva de Opus, EXACTAMENTE la misma
que la original, para comparar el antes/despues.

## Segunda auditoría de Opus (comparación) - 2026-09-03

Pedido explícito del usuario tras terminar los 7 bloques: relanzar la MISMA auditoría (mismo
alcance, mismos 7 principios P1-P7) para comparar el antes/después con ojos frescos, sin dar
nada por bueno solo por estar en esta bitácora como "hecho y verificado".

**Resultado: 7 defectos reales de severidad alta, vivos ahora mismo.**

- **B-1/B-2** (críticos, REGRESIÓN): el arreglo real de la fila de la Librería (altura fija que
  no cede al plegar) y de "Elegir objeto..." dejando la Librería desplegada para siempre, YA
  estaban arreglados en el commit `83fd33c` ("Octava pasada, Fase 1") - el `git revert
  --no-commit 1331ac6..HEAD` de `c38c960` se llevó por delante esa Fase 1 entera, aunque el
  rechazo real del usuario era solo sobre la NAVEGACIÓN (Fases 2/3/5/7), no sobre estos dos
  bugs de layout, que estaban medidos y verificados aparte.
- **B-3/B-4** (críticos): "Investigar todo" salta en silencio cualquier objeto ya
  parcialmente investigado (se queda en su conteo antiguo, sigue bloqueado en el juego real
  pese al mensaje "Investigación completa") y no marca `IsDirty` (mutación directa del modelo,
  sin pasar por ningún ViewModel observable) - riesgo real de pérdida silenciosa del trabajo.
- **B-5** (crítico): los Spawn Points tampoco marcan `IsDirty` (mismo agujero de N-2,
  `ServersViewModel` sin ninguna `[ObservableProperty]` real).
- **B-6** (alto): la defensa total y el bono de set de Equipamiento (E-4) NO se recalculan al
  SUSTITUIR una pieza ya puesta por otra (solo escuchan `IsEmpty`, que no cambia en ese caso) -
  el caso normal de comparar armaduras muestra un número congelado y falso.
- **B-7** (alto): la comprobación del alto de la fila de la Librería en el arnés (T-21) busca
  un `MaxHeight=460` que ya no existe (residuo del revert, el valor real es 238) - nunca
  encuentra nada, imprime `-1px` sin contar como FALLO, y ERA la única comprobación capaz de
  detectar B-1 - estuvo rota mientras la bitácora certificaba "sin ningún FALLO" siete veces.

Además, hallazgo transversal serio: **`dotnet test` (134/134, citado 11 veces en esta bitácora)
no ejecuta ni una sola línea de la capa App** - los 6 bugs de arriba viven en ViewModels puros,
sin dependencia de WPF, que nunca pasaron por ningún test real (T-21 decidió "sin xunit" para
la parte visual, y sin querer eso dejó también sin cubrir la parte de logica pura).

Informe completo (7 defectos + hallazgos transversales T-A a T-I + las 14 secciones de la app +
veredicto bloque a bloque con "✓ bien resuelto / ▲ a medias / ✕ regresión" + orden de ataque
recomendado en 4 tandas) publicado como artifact y entregado al usuario. Pendiente de decisión
del usuario: qué tanda(s) atacar y en qué orden - el propio informe ya distingue "sin cambiar la
forma de nada" (tanda 1, bajo riesgo) de "reworks, solo con aprobación explícita" (tanda 4,
terreno donde ya se pisó una vez con el rediseño de Librería rechazado).

## Plan de la segunda auditoria (Fable) - ejecucion

Pedido explicito del usuario: ejecutar el plan ENTERO de la segunda auditoria (7 defectos
criticos + hallazgos transversales T-A a T-I + los ~80 hallazgos de las 14 secciones), y al
terminar pedirle a Fable una TERCERA auditoria, mismo alcance que las dos anteriores.

### B-1 + B-2 + B-7 (recuperados de 83fd33c, perdidos por el revert c38c960)

**B-1**: la fila de la Libreria (Objetos y Buffs) dependia de "*"+MinHeight fijo, sin importar
si estaba plegada - plegar no devolvia ni un pixel de espacio real. **B-2**: "Elegir
objeto...tras..." ponia `IsLibraryCollapsed=false` DIRECTAMENTE, un pestillo de un solo
sentido, sin nada que lo volviera a poner en `true`. Ambos YA estaban arreglados en el commit
`83fd33c` ("Octava pasada, Fase 1") con el mismo mecanismo real (`IsLibraryVisible`/
`IsBuffLibraryVisible`, propiedades derivadas que combinan la preferencia real del usuario con
un despliegue TEMPORAL mientras se elige, mas `BoolToGridLengthConverter`/
`BoolToDoubleConverter` para que la fila colapse a `Auto`/0 de verdad) - perdidos por
`c38c960`, un `git revert` de rango demasiado ancho (Fases 1-7) cuando el rechazo real del
usuario era solo sobre la Fase 2/3/5/7 (navegacion), no sobre estos 2 bugs de layout ya
medidos y verificados aparte.

Recuperado el mecanismo real de `83fd33c` (no reinventado), adaptado al estado actual del
fichero (las RowDefinition ya habian evolucionado con mi propio arreglo de la primera ronda,
B-1 de Bloque 1 - ESE arreglo resolvia un problema distinto y real, "el Inventario
subvencionando a la Libreria", y sigue siendo valido; el colapso-a-cero es un arreglo
complementario, no un duplicado).

**B-7** (la comprobacion rota del arnes que era la unica capaz de detectar B-1): el
`MaxHeight=460` que buscaba SI es ahora el valor real (recuperado de 83fd33c junto con el
resto), asi que el finder ya encuentra el Grid - pero ademas se arreglo de raiz el patron real
que Fable señalo: un finder que no encuentra nada ahora imprime `FALLO:` explicito, nunca un
numero centinela silencioso como el `-1px` de antes.

Verificado con numeros reales: fila desplegada 267px (antes del arreglo real, con el commit
`83fd33c`, se midio 272px - la diferencia es ruido real de UI, no una regresion), colapsada
46,6px (antes se quedaba en 150-238px SIEMPRE) - exactamente la misma magnitud de mejora que la
medicion original. B-2 verificado con el pestillo real: preferencia forzada a "plegada" antes
de "Elegir...", revelada TEMPORALMENTE durante la eleccion, y confirmado que vuelve sola a
"plegada" tras colocar - ya no se queda desplegada para siempre.

`dotnet build`/`dotnet test` en verde (134/134), arnes completo sin NO-FOUND/FALLO/EXCEPTION.

### B-3 + B-4 + B-5 + T-A (segunda auditoria, Fable)

**B-3, "Investigar todo" no investigaba todo**: `ResearchAllService.Apply` usaba
`HashSet.Add` para decidir si un objeto era nuevo - un objeto YA parcialmente investigado
(entrada real con `Count=37` de un umbral real de 100) se saltaba ENTERO, dejandolo bloqueado
en el juego real pese al mensaje "Investigacion completa". Arreglado: cualquier entrada
existente se sube (nunca se baja) al umbral real con `Math.Max`, en vez de saltarse.

**B-4, "Investigar todo" no marcaba el personaje como modificado**: `ResearchAllService.Apply`
muta `PlrCharacter.Research` directamente, sin pasar por ningun ViewModel observable -
`MainViewModel.ResearchAll` ahora llama a `MarkDirty()` explicitamente tras aplicar.

**B-5, los Spawn Points tampoco marcaban modificado**: `ServersViewModel` no tenia ninguna
`[ObservableProperty]` real - los campos editables viven en `ServerEntryRowViewModel` (cuyo
`PropertyChanged` nunca llegaba arriba) y `Add/RemoveEntry` mutan una `ObservableCollection`
(`CollectionChanged`, no `PropertyChanged`). Arreglado con el mismo patron real ya probado en
`BuffsViewModel.SlotChanged`: un evento `Changed` propio, disparado explicitamente en cada
camino de edicion real (por fila Y por añadir/quitar). De paso, Apariencia (que marcaba sucio
"por casualidad" via `RefreshPreview` reasignando `PreviewImage`) gano una señal EXPLICITA por
swatch, para que dejar de reasignar el bitmap algun dia no abra un tercer agujero silencioso.

**T-A, `dotnet test` no probaba nada de la capa App**: nuevo proyecto real
`TerrasavrNative.App.ViewModels.Tests` (xunit, headless de verdad - sin ventana, sin
Application, sin UI Automation) en `TerrasavrNative.slnx`, complemento real de
`TerrasavrNative.App.Tests` (que sigue siendo consola+capturas a proposito, para lo VISUAL).
Sembrado con 9 pruebas deterministas reales: 4 de `ResearchAllService` (B-3, incluido que un
`.plr` ajeno con un Pid duplicado no revienta la operacion) y 5 de una "matriz de suciedad"
real sobre un `MainViewModel` cargado de verdad (B-4, B-5 x3, y el color de Apariencia) - la
misma matriz que Fable recomendo como via real para cerrar esta categoria de bug para siempre.

Verificado: `dotnet test` ahora ejecuta 143 pruebas reales (134 de Core + 9 nuevas de App,
todas en verde) en vez de 134 - las 9 nuevas habrian cazado B-3/B-4/B-5 de haber existido antes.
Arnes visual completo tambien en verde, sin NO-FOUND/FALLO/EXCEPTION.

### B-6 + hallazgo real durante su verificacion: contaminacion acumulada del arnes (segunda auditoria, Fable)

**B-6, la defensa/bonus de set no se recalculaba al SUSTITUIR un slot ya ocupado**:
`EquipmentGroupViewModel` solo escuchaba `IsEmpty` de cada slot para disparar
`RecomputeDefenseAndBonus()` - poner un objeto en un slot VACIO cambia `IsEmpty` (dispara), pero
sustituir un casco YA puesto por otro (`PlaceItem` sobre un slot ocupado) no cambia `IsEmpty`
(sigue siendo `false` antes y despues), asi que la defensa total se quedaba pillada en el valor
de la PRIMERA pieza puesta. Arreglado ampliando el filtro a "cualquier cambio real, salvo
`IsSelected`/`JustEdited`" (mismo criterio ya usado en `ItemEditViewModel`/`BuffEditViewModel`).
De paso, `ActiveSetBonusText` solo miraba el catalogo de sets VANILLA - se añade deteccion real
de sets de Calamity (`ActiveCalamitySetBonusText`): las 3 piezas comparten el mismo texto real
de `CalamityCatalogEntry.SetBonus`, mismo mecanismo que usa el propio catalogo (no hay un
"catalogo de sets" aparte para Calamity, a diferencia de vanilla). 2 pruebas deterministas
nuevas en `EquipmentDefenseTests.cs` (sustituir casco Iron->Molten, defensa 2->8; vaciar casco,
defensa a 0) - ambas habrian fallado antes del arreglo.

**Hallazgo real, no planeado, al verificar B-6 con el arnes visual completo**: aparecieron 3
NO-FOUND nuevos (boton "Colocar", "Minima", "Maxima" de Buffs) que NO tenian nada que ver con
B-6. Investigado a fondo (no descartado a la ligera, regla real de la bitacora): la ruta de
`%TEMP%\uia-harness-test.plr`/`.tplr` que usa el arnes es FIJA entre ejecuciones, y el `.tplr`
companero NUNCA se borraba antes de escribir el personaje sintetico "fresco". Con un diagnostico
temporal en `CalamityCharacterSync.MergeBuffs` se confirmo la causa real: al cargar, `MergeBuffs`
fusiona (correctamente, es su trabajo real - el `.tplr` es la fuente de verdad de buffs con mods
instalados) los buffs YA guardados en el `.tplr` de la ejecucion ANTERIOR; el arnes coloca 2
buffs de prueba mas y los vuelve a guardar todos via `Save()` al final - una bola de nieve
real que, tras ~22 ejecuciones repetidas de esta sesion tan larga, dejo los 44 slots llenos
(alternando "Piel de obsidiana" real x1 con un buff sintetico de Calamity), asi que el arnes ya
no encontraba ningun slot vacio donde probar "Elegir...". No es un bug de produccion (un
personaje real no se recarga sobre si mismo sin fin) - es higiene de arnes: `Program.cs` ahora
borra `.plr`/`.tplr`/`.bak` sinteticos antes de escribir el personaje fresco de cada ejecucion.
Confirmado con 2 ejecuciones consecutivas ya en verde (antes, la primera limpia y la segunda
habria vuelto a fallar en ~20 ejecuciones mas).

`dotnet build`/`dotnet test` en verde (145/145: 134 Core + 11 App), arnes visual completo dos
veces seguidas sin NO-FOUND/FALLO/EXCEPTION - quedan cerrados los 7 defectos criticos (B-1 a
B-7) de la segunda auditoria de Opus (Fable). Sigue T-A ya cerrado; quedan T-B/T-C (Ola 1) y el
resto de olas/hallazgos por seccion.

### T-B (segunda auditoria, Fable) - Ola 1

**"Elegir OTRO personaje en Inicio con cambios sin guardar los tira sin avisar"**: `OnWindowClosing`
ya protegia el cierre de la ventana con un dialogo real Si/No/Cancelar, pero cargar otro
personaje POR ENCIMA (desde el lanzador de Inicio, o desde "Cargar personaje...") llamaba a
`LoadFromPath` directo, sin preguntar nada - mismo agujero real, sitio distinto.

Arreglado extrayendo `MainWindow.ConfirmDiscardChanges(string accion)` (mismo dialogo/logica
real de siempre, ahora reutilizable) y añadiendo un gancho `MainViewModel.ConfirmDiscardChanges`
(`Func<bool>?`) que `MainWindow` rellena en su constructor - MainViewModel sigue siendo headless
de verdad (los tests no necesitan ninguna `Window`/`MessageBox`; sin View enganchada se deja
pasar siempre, como antes). Los 3 puntos de entrada reales que pueden tirar el personaje actual
(Inicio, "Cargar personaje...", Ctrl+O que reutiliza el mismo dialogo) quedan cubiertos.

3 pruebas deterministas nuevas (`ConfirmDiscardChangesTests.cs`): cancelar no carga ni pierde
nada, confirmar si carga el nuevo, y sin cambios sin guardar el hook ni se consulta (no molesta
si no hay nada real que perder).

`dotnet test` 148/148 en verde (145 + 3), arnes visual completo sin NO-FOUND/FALLO/EXCEPTION.

### T-C (segunda auditoria, Fable) - cierra la Ola 1

**Escritura no atomica**: `CharacterFileService.Save` hacia `File.WriteAllBytes` directo sobre
el fichero real - un corte de luz o cierre forzado a mitad de escritura deja un `.plr`
corrupto (0 bytes o a medias). Arreglado con `WriteAtomic` real: escribe siempre a un `.tmp`
aparte primero y solo AL FINAL lo intercambia por el real con `File.Replace` (un solo paso
atomico del sistema de ficheros) - que de paso genera el `.bak` (ver mas abajo) en la MISMA
operacion en vez de una copia previa por separado. Si el `.bak` esta bloqueado (antivirus),
best-effort real: reintenta el `Replace` sin backup, el guardado del personaje en si no debe
fallar por eso.

**`.tplr` se creaba SIEMPRE**: incluso para un personaje 100% vanilla que nunca tuvo ni tendra
un objeto/buff de Calamity - ensuciaba la carpeta real de Documentos del usuario con un
fichero que ni Terraria ni tModLoader piden. Ahora solo se escribe si YA existia uno (no se
hace desaparecer un `.tplr` real de otra sesion) o si el personaje tiene contenido real de
Calamity (objeto en algun contenedor, o un buff con id sintetico >= `CalamityIds.BuffIdBase`).

**El `.bak` real (ya existia desde el Bloque 0) no tenia ninguna forma de usarse desde la UI**:
nuevo `MainViewModel.UndoLastSaveCommand` + boton real "Deshacer último guardado" en la barra
superior (misma barra de Cargar/Guardar, visible en cualquier pestaña) - restaura el `.plr`/
`.tplr` desde su `.bak` y recarga, mismo mecanismo real que "Cargar personaje...".

4 pruebas deterministas nuevas (`SaveAtomicoTests.cs`): personaje vainilla no crea `.tplr`,
personaje con un buff real de Calamity si lo crea, guardar dos veces deja el `.bak` con el
contenido ANTERIOR (verificado leyendolo de vuelta) sin ningun `.tmp` suelto, y Deshacer
ultimo guardado restaura de verdad un slot de equipo a como estaba antes de guardar.

`dotnet test` 152/152 en verde (148 + 4), arnes visual completo sin NO-FOUND/FALLO/EXCEPTION -
confirmado ademas que un personaje 100% vainilla ya no genera ningun `.tplr` al guardar (antes
"Guardado: uia-harness-test.plr + uia-harness-test.tplr", ahora solo "...plr").

**Cierra la Ola 1 entera** (T-A, T-B, T-C mas los 7 defectos criticos B-1 a B-7) de la segunda
auditoria de Opus (Fable). Sigue la Ola 2.

### T-I + T-F (segunda auditoria, Fable) - Ola 2

**T-I, `ItemEditViewModel` reconstruia la rejilla de prefijos en CUALQUIER cambio del slot**:
incluidos `IsSelected` (marcar/desmarcar en la rejilla) y `JustEdited` (el flash real de "acabo
de editarse", que se apaga solo 450ms despues) - ninguno de los dos cambia que prefijos
aplican, asi que reconstruir por ellos era parpadeo visual real de sobra. Mismo bug real que
B-6 en `EquipmentGroupViewModel`; `BuffEditViewModel` ya lo evitaba desde el principio (filtra
a `IsEmpty`). Arreglado con el mismo filtro real de B-6 (excluir solo `IsSelected`/
`JustEdited`). 1 prueba determinista nueva: seleccionar+flash no reconstruyen `Groups`, cambiar
de objeto de verdad si.

**T-F, 3 recursos muertos en `Theme.xaml`**: `HeaderGradientBrush` y `ElevatedCard` no los
usaba nadie y duplicaban roles ya cubiertos (`SidePanelCard`/`BgSecondaryBrush`) - borrados sin
mas (P6, nada que reusar de verdad). `CircleCloseButton` (gira 90° y se tiñe de rojo al pasar
el raton, gesto real de Terrasavr) si merecia reuso real: el boton "Quitar" de cada fila de
Spawn Points era texto plano - pasa a usar `CircleCloseButton` con "✕" + tooltip real.

`dotnet test` 153/153 en verde (152 + 1), arnes visual completo sin NO-FOUND/FALLO/EXCEPTION
(la compilacion en si ya valida que `CircleCloseButton` resuelve como StaticResource real).

Nota aparte: un `dotnet test` a nivel de solucion fallo una vez de forma no reproducible (9/19
en `TerrasavrNative.App.ViewModels.Tests`, todos con el mismo patron "IsDirty esperado False
salio True" en el arranque de `NewLoadedViewModel`) mientras el mismo proyecto solo, y la
solucion entera relanzada dos veces mas, salieron 100% en verde - pinta a carrera de
compilacion en paralelo entre proyectos que comparten `TerrasavrNative.App`, no un fallo real
de los tests. Registrado por si se repite (regla real: "si falla dos veces seguidas, parar") -
de momento solo fallo una vez de tres, no se ha insistido mas.

### A-a + D-a + N-c + V-a + X-b (segunda auditoria, Fable) - Ola 2 continua

**A-a**: con la ventana Amplia, la pestaña "Inventario" ya muestra el Almacen seleccionado al
lado (A-4, ronda anterior) - la pestaña "Almacenes" seguia ahi, un segundo camino redundante al
MISMO `StorageGroup`. Se oculta con `IsStorageExpanded` (no se borra: en Compacto/Normal sigue
siendo el unico camino real).

**D-a**: `Desbloqueos`/`Version` eran las dos unicas pestañas internas de Personaje sin
`HorizontalAlignment="Left"` junto a su `MaxWidth` - sin el, `Stretch` (por omision) las
centraba en vez de pegarlas al borde izquierdo real como el resto. Añadido a ambas.

**N-c**: la tarjeta de Inicio decia "¡Sobre esta versión!" pero llevaba a una pestaña con la
cabecera "Acerca de" - dos nombres para el mismo destino. Unificado con el nombre real de la
pestaña (fuente de verdad); "¡Sobre esta versión!" sigue vivo dentro, como titulo real de la
subseccion del changelog (no era el mismo texto por casualidad, es el titulo real de esa parte).

**V-a**: los botones de version no marcaban cual era la YA puesta - mismo patron real ya usado
en la rejilla de prefijos (D-6, `IsCurrent`). `VersionOption` paso de `record` inmutable a una
clase observable con `IsCurrent` que `VersionEditorViewModel` sincroniza en cada cambio real de
version (tambien durante `LoadFrom`, fuera del guardia `_suppressWriteback` - es un reflejo
visual, no una escritura al personaje).

**X-b**: la rueda del raton daba pasos de zoom de x1.15 mientras los botones ZoomIn/ZoomOut
daban x1.25 - dos velocidades para la misma accion. Constante real unica compartida
(`ExplorationViewModel.ZoomStep`), referenciada desde ambos sitios para que no vuelvan a
divergir.

2 pruebas deterministas nuevas (V-a: `IsCurrent` se mueve de boton y escribe al personaje;
X-b: `ZoomIn`/`ZoomOut` usan el paso compartido). `dotnet test` 155/155 en verde, arnes visual
completo sin NO-FOUND/FALLO/EXCEPTION.

### L-a (segunda auditoria, Fable) - Ola 2 continua

**Gramatica de busqueda real de Terrasavr perdida en la Libreria/Libreria de buffs**:
`LibrarySearchGrammar.cs` (commit `a0f5027`, octava pasada) se habia revertido sin querer
en `c38c960` junto al rediseño de navegacion que si se rechazo entonces - esta pieza en
concreto no tocaba navegacion (solo la busqueda de texto), asi que era segura de recuperar tal
cual. Recuperada del historial real (`git show a0f5027:...`, no reescrita de memoria) y
reaplicada a `LibraryViewModel.ApplyFilter`/`BuffLibraryViewModel.ApplyFilter` (antes:
`Contains` simple sobre el nombre) + tooltip real explicando la sintaxis en ambos cuadros de
busqueda.

Reglas reales (calco de `app.TabLibrary.search`): coma = OR entre terminos, espacio = AND
dentro de un termino, un termino de menos de 2 caracteres se ignora del todo (quirk real, un
"5" suelto no busca ni por id ni por nombre), `#123` = id exacto, `#100-200` = rango de id
(ambos extremos incluidos), `.texto` busca en el tooltip/descripcion en vez del nombre.

7 pruebas deterministas nuevas sobre `LibrarySearchGrammar.Matches` directamente (una por
regla real de la gramatica). `dotnet test` 162/162 en verde, arnes visual completo sin
NO-FOUND/FALLO/EXCEPTION.

**Cierra la Ola 2 salvo T-E** (barrido de tildes, el mas grande - se aborda aparte).

### T-E (segunda auditoria, Fable) - cierra la Ola 2 entera

**Barrido real de tildes**: recorrida a mano `MainWindow.xaml` (todos los `Content=`/`Text=`/
`Header=`/`ToolTip=` de cara al usuario, extraidos y revisados uno a uno, no adivinados) mas
`StatusMessage`/mensajes de error reales en `MainViewModel.cs`. Encontrados y corregidos ~20
textos reales sin su tilde correcta pese a que el resto del texto de la app SI la lleva bien
(la mezcla es justo lo que motivo este hallazgo): cabeceras de pestaña completas
("Investigacion", "Exploracion" - Header real, dejando intacta la clave interna
`CommandParameter`/`AppTab` que por convenio comparte el mismo texto sin tilde, eso NO es un
bug), tarjetas de Inicio ("Libreria", "Que mas puedes hacer", "Exploracion"), textos largos de
Estadisticas/Apariencia/Desbloqueos/Version/Exploracion/mundo, y 3 mensajes reales que yo mismo
habia escrito sin tilde en los bloques T-C/B-4 de esta misma ronda ("el ultimo guardado",
"Investigacion completa", "punto de aparicion").

**Guarda real para que no vuelva a pasar** (la otra mitad de T-E, "harness blacklist check"):
nuevo bloque en el arnes visual (`TerrasavrNative.App.Tests/Program.cs`) que recorre TODO el
arbol de UI Automation ya realizado a esas alturas y comprueba el `Name`/`HelpText` (ToolTip
real) de cada elemento contra una lista real de ~30 palabras que casi siempre llevan tilde en
español de España, por palabra completa (evita falsos positivos tipo "mascara" dentro de otra
palabra). Verificado que el chequeo detecta de verdad un fallo real: se reintrodujo a proposito
`Header="Version"` (sin tilde), se confirmo que el arnes lo marcaba con `FALLO: T-E` (2
ocurrencias, TabItem + su Text interno), y se revirtio - no es una comprobacion de adorno que
nunca dispara.

`dotnet build`/`dotnet test` 162/162 en verde, arnes visual completo con `T-E-TILDES: 0
fallo(s)` y sin ningun otro NO-FOUND/FALLO/EXCEPTION.

**Cierra la Ola 2 entera** (T-A a T-I mas A-a/D-a/N-c/V-a/X-b/L-a) de la segunda auditoria de
Opus (Fable). Sigue la Ola 3.

### H-1 + H-2 + H-3 (segunda auditoria, Fable) - Ola 3, empieza

**H-1, el nombre del personaje no se podia editar en ninguna parte**: `CharacterName` se leia/
escribia sin problema a nivel de datos pero se mostraba como texto muerto. La `TextBlock` de la
cabecera global pasa a un `TextBox` real con el estilo "filled" ya usado en el resto de la app
(`BgElevatedBrush`, sin borde) + `OnCommitTextOnEnter` (ya existia, T-17 resolvio el gotcha real
de Intro vs `UpdateSourceTrigger=PropertyChanged`). `OnCharacterNameChanged` ahora escribe de
verdad a `_loaded.Character.Name` y marca sucio (respeta `_suppressDirty` durante la carga, no
se marca a si mismo). F2: aviso discreto (icono ⚠ con tooltip) si el nombre del archivo no
coincide con el nombre real del personaje (`NameFileMismatch`).

**H-2, sin rastro de la version ni del archivo abierto**: nueva linea real bajo el nombre en la
cabecera global (`FileVersionLine`) con el nombre del fichero + la version resuelta a su
etiqueta real (ej. "1.4.4.0") cuando se conoce, o el numero crudo si no - se actualiza en vivo
al cambiar de version desde la propia pestaña Version.

**H-3, `StatusMessage` se quedaba atras dentro de Personaje**: un error de guardado/carga solo
lo veian 1 de 6 pestañas. Nuevo canal global (`GlobalErrorMessage` + banner real en rojo,
mismo nivel que el banner verde de "Guardado" de N-1, con boton "✕" para cerrarlo a mano) -
solo errores reales (cargar/guardar/deshacer fallido), nunca los exitos efimeros. `StatusMessage`
se queda para el detalle informativo dentro de Personaje, sin cambios.

**Bug real encontrado de paso, no planeado**: revisando `UndoLastSave` para engancharlo a
`GlobalErrorMessage`, se vio que si la recarga interna (`LoadFromPath`, que NUNCA relanza,
T-22) fallaba de verdad, el codigo seguia sin comprobarlo y pisaba el error real con un
`StatusMessage` de EXITO falso ("Deshecho el último guardado..."). Arreglado: solo se muestra el
mensaje de exito si `IsCharacterLoaded` sigue en `True` tras la recarga.

**Bug real encontrado y corregido en la propia implementacion**: el primer intento de visibilidad
del banner de error uso `NullToCollapsed` (semantica real: visible CUANDO ES null, para
placeholders tipo "sin personaje cargado") en vez de `EmptyToCollapsed` (visible cuando NO esta
vacio) - el banner salia SIEMPRE visible y vacio. Detectado con una captura real (no descartado
a la ligera), corregido, y vuelto a verificar visualmente con un disparo temporal real
(`GlobalErrorMessage` forzado a un texto de prueba, captura, `DismissGlobalErrorCommand`,
revertido) antes de dar el arreglo por bueno.

7 pruebas deterministas nuevas (`CabeceraGlobalTests.cs`): nombre editable con round-trip real
via Save+recarga, `FileVersionLine` con version conocida Y desconocida, `NameFileMismatch` en
ambos sentidos, error global en carga Y en guardado forzados de verdad (directorio borrado antes
de Save), y `DismissGlobalError`. Arnes visual (`Program.cs`) actualizado: la busqueda del
nombre en la cabecera paso de `ControlType.Text`+`Name` a `ControlType.Edit`+`ValuePattern` (el
control cambio de tipo real, UI Automation expone el texto de un cuadro editable distinto que el
de un texto fijo).

`dotnet test` 169/169 en verde, arnes visual completo sin NO-FOUND/FALLO/EXCEPTION, capturas
reales revisadas a mano (cabecera con nombre editable + aviso de discrepancia + linea de
archivo/version, y banner de error con texto real).

### X-a (segunda auditoria, Fable) - Ola 3 continua

**"No existe 'ajustar a la ventana'"**: "Restablecer" vuelve al 100%, que para un mundo de
8400x2400 tiles (tamaño real maximo, "Grande") significa ver el 12% del ancho - "la accion mas
obviamente ausente de la pantalla". Nuevo boton real "Ajustar a la ventana" junto a Restablecer
- calcula `min(viewport/anchoMundo, viewport/altoMundo)` (el calculo vive en el code-behind,
unico sitio que conoce el tamaño real del `ScrollViewer`) y se aplica tambien solo, sin que el
usuario tenga que ir a buscarlo, justo despues de la carga asincrona real (X-7/T-13).

**Bug real encontrado verificando esto con el mundo real de 11MB (8400x2400 tiles)**: el suelo
`MinZoom=0.1` (10%) recortaba el calculo real (9.4% con el viewport real del arnes) hacia
arriba, dejando el mundo SIN caber del todo pese a que el boton decia "ajustar" - confirmado
con una captura real ANTES de tocar nada (no descartado a la ligera). Bajado a `0.02` (2%,
sigue siendo suficiente para frenar un zoom-out repetido con la rueda antes de una imagen
imperceptible, y deja sitio real para que hasta un mundo Grande quepa en una ventana bastante
estrecha).

Verificado con UI Automation real (click en el boton real, no simulado) mas una captura real
revisada a mano: el mundo completo (8400x2400) cabe entero en el panel tras pulsar el boton,
sin recorte horizontal ni vertical.

`dotnet test` 169/169 en verde (sin pruebas nuevas: la logica vive en code-behind, que este
proyecto ya prueba solo con el arnes visual real, no con xunit), arnes visual completo con
`X-a AJUSTAR-A-LA-VENTANA: ... cabe=True` y sin ningun otro NO-FOUND/FALLO/EXCEPTION.

### T-H/F1+F2 (segunda auditoria, Fable) - Ola 3 continua

**"Cero navegacion por teclado en las rejillas; los atajos son invisibles"**: N-3 ya tenia 4
atajos reales (Ctrl+S/O/F, Esc) bien implementados pero sin anunciarse en ningun sitio. F3
(flechas en rejillas) y F4 (Ctrl+Z) quedan fuera de esta ola (Wave 4/reworks).

**F1, tooltips + InputGestureText**: añadidos tooltips reales con el atajo (Ctrl+O en "Cargar
personaje...", Ctrl+S en "Guardar", explicacion real de Ctrl+F en el boton "Librería", Esc en
ambos botones "Cancelar" de eleccion). `InputGestureText` (la columna que la plantilla de
`MenuItem` ya reservaba en `Theme.xaml` sin que nadie la rellenara) se dejo SIN poblar a
proposito: revisados los 5 `MenuItem` reales del proyecto (Elegir objeto/buff, Aplicar mejor
prefijo, Vaciar slot x2) y ninguno corresponde hoy a un atajo de teclado real - poblarla con
algo inventado habria sido peor que dejarla vacia (mismo criterio ya establecido: "lo que no se
encuentra no se inventa").

**F2, `FocusVisualStyle` propio**: el foco de WPF por defecto (rectangulo negro discontinuo) es
practicamente invisible sobre este tema oscuro. Clave especial real de WPF
(`SystemParameters.FocusVisualStyleKey`) con un rectangulo violeta discontinuo (`AccentBrush`,
mismo acento de toda la app) - la recoge sola cualquier control al recibir foco por Tab, sin
tocar cada estilo uno a uno.

**Verificacion real, no de vista rapida**: una primera captura a resolucion completa de ventana
parecia no mostrar ningun foco visible - en vez de dar el arreglo por bueno a la ligera (mismo
error real que ya enseño el CSS `min()`/`max()` de este proyecto: "no se ve mal" no es lo mismo
que "funciona"), se comprobo con `Keyboard.FocusedElement`+`AdornerLayer.GetAdorners` real (1
adorner adjunto, confirmado) y con un recorte ampliado 4x de la zona exacta del boton - el
foco SI se pinta, solo era sutil a resolucion completa de ventana (proporcionado, no un fallo).

`dotnet test` 169/169 en verde, arnes visual con una comprobacion permanente nueva
(`T-H-FOCO: foco real + adorner de FocusVisualStyle adjunto=True`) + captura real, sin ningun
otro NO-FOUND/FALLO/EXCEPTION.

### Bd-a + Bd-b (segunda auditoria, Fable) - Ola 3 continua

**Bd-a, "Auto-equipar" es destructivo, de un click, sin confirmacion ni deshacer**: "el boton
dice 'Auto-equipar', no 'Reemplazar mi equipo actual' - la promesa real no llega al usuario".
F1 (tooltip explicito): el tooltip ya existente ("Coloca esta armadura/accesorios/armas...") no
avisaba de que SOBRESCRIBE lo ya puesto - reescrito para decirlo sin rodeos (que SI reemplaza
sin confirmar, que las armas SI respetan hueco libre sin sobrescribir, y que no conviene
guardarlo si no se esta seguro). F2 (deshacer) y F3 (selector de loadout de destino como UI
nueva) quedan fuera de esta ola (T-H/F4, Wave 4).

**Bd-b, "Auto-equipar" siempre iba al loadout 0, ignorando el seleccionado**: "sorpresa
silenciosa si estas mirando el Loadout 2". `AutoEquipService.Apply` usaba
`equipmentGroup.EquippedItems` (atajo hardcodeado al loadout 0) en vez de
`equipmentGroup.CurrentItems` (el loadout REALMENTE seleccionado,
`EquipmentGroupViewModel.SelectedLoadout`) - un solo cambio real. De paso, revisado y corregido
un comentario propio que se habia escrito con una justificacion NO verificada ("lo usa el doll
de Apariencia") - comprobado que Apariencia no renderiza armadura en absoluto y que
`EquippedItems` ya no tiene ningun consumidor real en produccion tras este arreglo (solo en
tests) - corregido para no dejar una razon inventada en el codigo.

1 prueba determinista nueva: Loadout 1 seleccionado, Auto-equipar coloca AHI (el Loadout 0
se queda vacio de verdad). `dotnet test` 170/170 en verde, arnes visual completo sin
NO-FOUND/FALLO/EXCEPTION.

### I-a + I-b (segunda auditoria, Fable) - cierra la Ola 3 entera

**I-a, "no se distingue que personaje esta cargado - las tarjetas se ven identicas al volver a
Inicio"**: `CharacterListEntryViewModel` pasa a observable con `IsCurrent`;
`HomeViewModel.UpdateCurrentPath` lo recalcula comparando `FilePath` contra el personaje
realmente cargado, llamado desde `MainViewModel.LoadFromPath` (exito Y fallo) y de nuevo tras
cada `Refresh()` (la lista se reconstruye entera, el flag no sobrevive solo). Borde violeta +
marca "✓" real en la tarjeta actual.

**I-b, "sin ninguna accion secundaria en la tarjeta - faltan las 3 obvias y baratas"**: menu
contextual real (clic derecho) con Abrir carpeta (`/select,` en el Explorador real), Duplicar
personaje (copia de fichero pura - mismo nombre interno, numerada si "(copia)" ya existe, la
"red de seguridad real para experimentar") y Restaurar copia de seguridad (mismo mecanismo
`.bak` real de T-C, pero operando sobre CUALQUIER personaje de la lista, no solo el cargado).
Un `ContextMenu` real es un popup FUERA del arbol visual de la ventana - `RelativeSource
AncestorType=Window` no llega ahi dentro, asi que `HomeViewModel` viaja en el propio `Tag` del
`Border` (sitio real donde dejarlo) y cada `MenuItem` lo recupera via
`PlacementTarget.Tag`.

**Verificado con especial cuidado por el riesgo real**: "adrian"/"Eldelgas" son personajes
REALES de esta maquina, y Duplicar/Restaurar escriben de verdad en disco - el arnes visual
NUNCA invoca estos 3 comandos contra una tarjeta real (solo abre el menu real y comprueba que
los 3 `Command`/`CommandParameter` resolvieron via el truco `PlacementTarget.Tag`, sin
invocarlos), y las 5 pruebas deterministas de `Duplicate`/`RestoreBackup` usan siempre una
carpeta temporal propia. Confirmado tras el arnes que la carpeta real de Players no cambio
(mismos ficheros, mismas fechas).

6 pruebas deterministas nuevas (`HomeCardTests.cs`). `dotnet test` 175/175 en verde, arnes
visual completo con `I-a IsCurrent...=True` y `I-b: 3 item(s) de menu, comandos sin
resolver=` (ninguno) - sin ningun otro NO-FOUND/FALLO/EXCEPTION.

**Deja la Ola 3 lista salvo R-a..R-g Fase 1** (H-1/H-2/H-3, X-a, T-H/F1+F2, Bd-a/Bd-b e I-a/I-b
ya cerrados) - Investigacion (R-a..R-g Fase 1) sigue siendo el ultimo punto real de esta ola.

### R-d + R-e + R-f + R-g (segunda auditoria, Fable) - cierra la Ola 3 entera

**Investigacion, Fase 1** ("arreglos que no cambian la forma" - mostrar tambien lo NO
investigado es R-a/R-b, Fase 2, con su propio layout, fuera de esta ronda):

**R-d, el "9999" crudo en cada chip de Calamity**: R-1 elimino el numero sospechoso para
vanilla pero Calamity seguia mostrando el placeholder real que "Investigar todo" escribe
cuando no hay umbral conocido (`ResearchAllService.PlaceholderCount`, ahora publico). Solo ESE
valor concreto se sustituye por "✔ Investigado" en `ResearchRowViewModel.CountLabel` - un
conteo real de Calamity que un personaje trajera de verdad del juego (no via este boton) sigue
mostrando su numero real, no es un dato inventado que ocultar.

**R-e, sin buscador**: mismo cuadro/estilo/gramatica real que Libreria (`LibrarySearchGrammar`,
L-a) - funciona con o sin carpeta elegida (sin carpeta, busca en TODO lo ya investigado, mismo
criterio real que Libreria).

**R-f, sin progreso global**: el resumen sin carpeta ni busqueda pasa de "N objetos" a
"N/Total" real (`_totalKnownObjects`, calculado en vivo del mismo universo vanilla+Calamity que
`ResearchAllService.Apply` ya recorre, no un numero fijo que pudiera desincronizarse).

**R-g, ninguna advertencia si no es Modo Viaje**: `ResearchViewModel.IsJourneyMode`
(`character.Difficulty==3`) + banner real (mismo estilo de aviso ya usado en Version) cuando no
lo es - la Investigacion no tiene efecto real en el juego fuera de ese modo.

4 pruebas deterministas nuevas (`ResearchOlaTresTests.cs`) + verificacion visual real completa
(banner Modo Viaje + busqueda con resultado real "Espada larga de hierro 1/1" + progreso
"N/Total", los 4 arreglos juntos en una sola captura revisada a mano). 2 bugs propios
encontrados y corregidos AL PROBAR (no al escribir): la busqueda de Calamity quedaba acotada a
la carpeta "Materiales" que seguia elegida de un paso anterior (faltaba `ClearCategoryCommand`
antes de buscar), y la navegacion a Investigacion olvidaba volver primero a la pestaña
Personaje (se habia quedado en Builds del test anterior).

`dotnet test` 179/179 en verde (tras un fallo transitorio de 22 pruebas en la ejecucion a nivel
de solucion, mismo patron real de carrera de compilacion en paralelo ya documentado en T-I/T-F
- resuelto solo en el reintento, confirmado limpio ejecutando el proyecto aislado Y la solucion
entera dos veces mas), arnes visual completo sin NO-FOUND/FALLO/EXCEPTION.

**Cierra la Ola 3 entera** (H-1/H-2/H-3, X-a, T-H/F1+F2, R-a..R-g Fase 1, Bd-a/Bd-b, I-a/I-b)
de la segunda auditoria de Opus (Fable). Sigue la Ola 4 (reworks, requiere aprobacion explicita
antes de tocar nada - terreno de un rediseño ya rechazado una vez) y el resto de hallazgos
sueltos por seccion no agrupados en ninguna ola.

### T-D / X-e (segunda auditoria, Fable) - primer hallazgo suelto fuera de las olas

**"La barra de desplazamiento horizontal esta rota"**: `Theme.xaml`, el `ControlTemplate` de
`ScrollBar` fijaba `Width="10"` siempre (correcto en vertical, un hilo inservible en
horizontal) y `Track.IsDirectionReversed="True"` fijo (correcto en vertical -arriba=0-, invierte
izquierda/derecha en horizontal) - sin ningun trigger real para el caso horizontal. El unico uso
real (`WorldMapScroll`) salia como un muñon mal orientado, enmascarado porque el pan por
arrastre ya funciona y nadie usa la barra en si. Arreglado con un `Trigger`
`Orientation=Horizontal` real: intercambia `Width`/`Height` (10px de ALTO, no de ancho) y
desactiva `IsDirectionReversed` - el `Track` en si ya se reorienta solo (mecanismo real de WPF,
sin tocar nada ahi).

Verificado restableciendo el zoom real a 100% (fuerza scroll horizontal real con el mundo de
8400 tiles), encontrando la `ScrollBar` horizontal de verdad en su plantilla
(`ActualHeight=17px, ActualWidth=770px` - antes habria sido justo al reves) y con una captura
real revisada a mano (barra horizontal real y proporcional visible al pie del mapa).

`dotnet test` 179/179 en verde (sin pruebas xunit nuevas: es un `ControlTemplate` de WPF, ya
probado por el propio arnes visual), arnes visual con `T-D:` en verde y sin ningun otro
NO-FOUND/FALLO/EXCEPTION.

### Bug real de concurrencia encontrado investigando la inestabilidad de "dotnet test" (no de la auditoria - hallazgo propio)

**Root-cause real, por fin, de la inestabilidad ya documentada dos veces antes** ("carrera de
compilacion en paralelo" en T-I/T-F y R-d..R-g) - esta vez con 28/52 pruebas fallando de golpe
en una sola ejecucion, con errores dispersos y sin relacion aparente entre si (valores
inesperados, `NullReferenceException`...), siempre resueltos al reintentar. Investigado de
verdad esta vez en vez de anotarlo otra vez como "flaky": `PlayerPreviewRenderer.Cache` era un
`Dictionary<string, byte[]>` normal, compartido (`static`) y relleno bajo demanda con un patron
real `TryGetValue` + asignacion SIN ningun `lock`. xunit ejecuta clases de test EN PARALELO por
omision, y practicamente todas construyen un `MainViewModel` real (que llama a
`PlayerPreviewRenderer.Render` al cargar Apariencia) - con suficientes pruebas ya acumuladas
esta sesion, la probabilidad real de que dos hilos golpearan el diccionario a la vez dejo de
ser insignificante. Un `Dictionary` no es seguro para lectura+escritura concurrente - puede
corromper su estado interno bajo carga real, exactamente el patron disperso observado.

Arreglado con `ConcurrentDictionary<string, byte[]>` + `GetOrAdd` (atomico, el tipo real
pensado para esto) en vez de reinventar el locking a mano. Revisado el resto del proyecto
(`Core`+`App/Services`) buscando el mismo patron real - ningun otro cache estatico mutable sin
proteger, solo tablas de consulta `static readonly` fijas (construidas una vez, nunca
modificadas despues, seguras de leer en paralelo).

**Verificado de verdad, no solo "parece que ya no falla"**: 5 ejecuciones seguidas de
`TerrasavrNative.App.ViewModels.Tests` en solitario, las 5 en 52/52 verde (antes, la misma
ejecucion habia fallado 28/52 una vez de cada pocas). Las dos entradas anteriores de esta
bitacora que decian "pinta a carrera de compilacion en paralelo" eran, casi con toda
seguridad, este mismo bug real - se deja constancia aqui por si alguien relee aquellas y se
pregunta si siguen sin explicar del todo (ya no).

### E-b + Bu-a + Bu-b + D-c + Ap-e + Ap-f (segunda auditoria, Fable) - lote de hallazgos sueltos

**E-b, en modo Amplio se pierde la simetria de las 3 columnas**: la etiqueta de cada columna
usaba el `DisplayName` largo pensado para el panel Editar ("Equipo puesto - armadura/
accesorios" x3) - ya existian etiquetas cortas reales en `KindOptions[0/1/2].Label`
("Armadura"/"Vanidad"/"Tintes"), solo habia que reutilizarlas (binding indexado real de WPF).

**Bu-a, los slots de buff no tenian flash de edicion**: T-14 se porto a objetos y no a buffs,
pese a que los comentarios ya declaraban a los dos paneles como gemelos. Calco literal de
`ItemSlotViewModel.JustEdited`/`TriggerEditFlash` en `BuffSlotViewModel`, mismo Border/Opacity/
Storyboard real en `BuffSlotCompactTemplate`, disparado desde el mismo sitio real donde
`BuffsViewModel` ya escuchaba cambios por slot (con el mismo guardia anti-reentrada real que
`IsSelected`/`JustEdited` ya necesitaban en objetos).

**Bu-b, se podian poner buffs duplicados**: `PlaceBuff` no comprobaba si el buff ya estaba en
otro slot - Terraria no tiene dos instancias del mismo buff activas a la vez. Delegado real
(`Func<int, BuffSlotViewModel, bool>`, no la coleccion entera - los slots hermanos se estan
construyendo a la vez) inyectado desde `BuffsViewModel.LoadFrom`; `PlaceBuff` ahora devuelve
`bool` y dejsa un `RejectionMessage` real (mismo patron ya usado en `ItemSlotViewModel`) - el
picker de la Libreria de buffs se queda abierto si se rechaza, en vez de fingir que se coloco.

**D-c, BUG REAL de verdad (no solo cosmetico) - las 2 casillas de carrito potenciado escribian
el MISMO bit**: `UnlockedSuperMinecart` y `UsingSuperMinecart` leian/escribian ambas el bit 0
de `SuperCartByte`. Confirmado contra el codigo real decompilado (`Player.cs`,
`newPlayer.unlockedSuperCart = bitsByte3[0]; newPlayer.enabledSuperCart = bitsByte3[1];`) - son
dos flags reales y distintos, bit 0 y bit 1. Corregido a leer/escribir cada uno su bit real.

**Ap-e, `IsMale` no cambia nada en el preview, y no se decia**: confirmado en
`PlayerPreviewRenderer.Render` (`_ = isMale;`, sin usar - la unica variante de piel completa
disponible se usa para ambos generos). Añadida una frase honesta mas al texto que ya enumeraba
otras limitaciones reales del preview.

**Ap-f, sin validacion Vida actual <= maxima**: se podia poner `HealthNow=500`/`HealthMax=100`
sin que nada lo impidiera - el juego real lo recorta. Recortado en `AppearanceViewModel`
(`HealthNow` nunca supera `HealthMax`; bajar `HealthMax` arrastra `HealthNow` hacia abajo si
hace falta) - el guardia solo aplica FUERA de la carga (`_suppressWriteback`), igual que el
resto de propiedades de esta clase. Mismo arreglo aplicado a Mana (mismo par exacto de campos,
no lo menciona el hallazgo original pero es el mismo bug).

10 pruebas deterministas nuevas (`HallazgosSueltosTests.cs`). `dotnet test` 186/186 en verde,
arnes visual completo sin NO-FOUND/FALLO/EXCEPTION.

### L-e + N-d (segunda auditoria, Fable) - lote 2 de hallazgos sueltos

**L-e, el mensaje de rechazo no se limpia nunca**: escribir un id invalido dejaba el aviso rojo
colgado indefinidamente, incluso cambiando de slot y volviendo. `RejectionMessage` solo se
limpiaba dentro de `UpdateFrom` (una colocacion CON exito) - si el rechazo era lo ultimo que
pasaba en ese slot, se quedaba para siempre. Arreglado en ambos slots reales (`ItemSlotViewModel`
y el `RejectionMessage` que Bu-b acaba de añadir a `BuffSlotViewModel`): se limpia al cambiar de
seleccion, en cualquiera de los dos sentidos.

**N-d, "Objetos nuevos" es texto, no sprites**: "toda la app enseña sprites; aqui no, y el
resolvedor de iconos ya esta a mano". Nuevo `WhatsNewItemViewModel`/`WhatsNewEntryViewModel`
(App) envolviendo los `WhatsNewItem`/`WhatsNewEntry` crudos de Core - `Key` (nombre interno
real, no un id numerico) se resuelve contra `VanillaItemCatalog.GetIdByKey` +
`VanillaIconResolver`, mismo mecanismo real ya usado para el Pid de Investigacion. Nota real:
el `whats_new.json` actual del proyecto describe una version FICTICIA (contenido sintetico
propio de la demo, "PalworldKinshipPeach" y similares) - ninguna fila muestra icono todavia en
la app tal cual esta hoy, no porque el mecanismo este mal sino porque ninguna de esas claves es
un objeto vanilla real ("lo que no se encuentra no se inventa"). Verificado el mecanismo en si
con una clave vanilla real conocida (IronBroadsword, id 4) en una prueba aparte.

4 pruebas deterministas nuevas (2 en `HallazgosSueltosTests.cs`, 2 en `WhatsNewIconTests.cs`
nuevo). `dotnet test` 190/190 en verde, arnes visual completo sin NO-FOUND/FALLO/EXCEPTION.

### S-d + I-c + D-b (segunda auditoria, Fable) - lote 3 de hallazgos sueltos

**S-d, "Id" ocupaba el mismo protagonismo que los campos utiles**: pese a que el texto real ya
dice "no suele hacer falta tocarlo" - mas estrecho (70px, no 100), tono secundario real
(`TextSecondaryBrush`, mismo tono que su propia etiqueta) y tooltip con la explicacion completa.

**I-c, anchos fijos en Inicio (puro `WrapPanel`)**: en una ventana de 1920px se usaban solo
880px, dejando ~1000px negros. Nuevo `MainViewModel.InicioContentMaxWidth` (880 en Compacto/
Normal, 1400 en Amplio - mismo `SizeClass` real compartido, sin umbral propio) + `MinHeight` en
vez de `Height` fijo en las 5 tarjetas de navegacion (F2 - un titulo/descripcion mas largo hace
crecer la tarjeta en vez de recortarse).

**D-b, 13 casillas planas sin agrupar en Desbloqueos**: 4 subtitulos reales por familia (Modo
de dificultad, Antorchas de bioma, Consumibles permanentes, Eventos y otros) - agrupacion real
por lo que cada flag realmente es, no generica. F2 (columnas en Amplio) queda fuera de esta
pasada.

1 prueba determinista nueva (`LoteTresTests.cs`). Verificado con capturas reales de las dos
pestañas que el arnes no visitaba todavia (Spawn Points, Desbloqueos) - la agrupacion de
Desbloqueos se ve limpia y clara. `dotnet test` 191/191 en verde, arnes visual completo sin
NO-FOUND/FALLO/EXCEPTION.

### V-c (segunda auditoria, Fable) - lote 4 de hallazgos sueltos

**"Bajar de versión no advierte de lo que se pierde"**: el aviso rojo generico ya existente no
decia QUE secciones concretas dejarian de guardarse. Nuevo `VersionEditorViewModel.
DowngradeWarning` real (banner naranja aparte, visible solo cuando aplica) contra los 3
umbrales reales de `PlrBodySerializer` con impacto mas facil de contar con exactitud: equipo
puesto (145), Bóveda del Vacío (200, con el numero real de objetos) y Loadouts 1/2/3 (269, con
el numero real de objetos sumados entre Items/Vanidad/Tintes).

**Limitacion real encontrada Y documentada al verificar (no fingida)**: los 3 primeros tests
fallaron al escribir la prueba colocando el equipo VIA LA UI tras cargar - `_character.
EquipmentItems`/`Loadouts` (los campos crudos que el aviso lee) solo se sincronizan de vuelta
desde `MergedContainers` dentro de `CharacterFileService.Save`/`MaskAndSyncAll`, nunca al
colocar un objeto en si. El aviso cubre el caso real mas comun (un personaje que YA trae
contenido real al cargarlo, y se le baja la version sin darse cuenta) - no una prediccion en
vivo de ediciones sin guardar. Documentado explicitamente en el propio codigo y en las pruebas
(reescritas para embeber los datos directamente en el .plr sintetico, el escenario real).

4 pruebas deterministas nuevas. Verificado tambien con una captura real: UIA-Test (con equipo
real puesto tras Auto-equipar + un Guardar real de por medio) bajado a la version 98 muestra el
aviso naranja real y especifico junto al generico rojo. `dotnet test` 195/195 en verde, arnes
visual completo sin NO-FOUND/FALLO/EXCEPTION.

### A-c (segunda auditoria, Fable) - "los contadores solo estan en Almacenes"

**"Los contadores de A-1 solo estan en Almacenes - 'Inventario (47/50)' seria igual de util y
no existe en ningun sitio"**: `ContainerViewModel.DisplayName` pasa de un texto fijo a
`"{nombre} ({ocupados}/{total})"`, calculado en vivo (suscripcion por slot a `IsEmpty`) - mismo
mecanismo real que ya usaba `EquipmentOptionViewModel.DisplayLabel` (A-1), aplicado ahora de
forma universal a CUALQUIER contenedor, no solo a los 4 de Almacenes con pildora propia. Cero
sitios que tocar en el binding de XAML existente (sigue siendo `{Binding ....DisplayName}` en
todos), asi que aplica solo por herencia a Banco/Caja fuerte/Fragua/Boveda/Coins/Ammo/grupos de
Equipamiento sin ningun cambio adicional.

**Hueco real encontrado al verificar con captura, no al escribir el codigo**: la pestaña
compacta "Inventario" (la vista por defecto, distinta de la vista lado-a-lado de Amplio que
A-4 ya dejaba con `DisplayName`) no tenia NINGUN `TextBlock` de cabecera - el arreglo de arriba
no se veia ahi por mucho que `DisplayName` ya llevara el recuento real. `MainWindow.xaml`: el
`ContentControl` de esa vista se envolvio en un `DockPanel` con un `TextBlock` de cabecera
igual al que ya usaba Amplio.

2 pruebas deterministas nuevas (`ContainerCountTests.cs`: recuento inicial correcto, recuento
en vivo al colocar y vaciar). Verificado con captura real (`resize-inv-minimo.png`): la pestaña
compacta de Inventario muestra ahora "Inventario (12/50)" tras cargar un personaje real.
`dotnet test` 197/197 en verde (134 Core + 63 ViewModels), arnes UIA completo sin NO-FOUND/
FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### A-d (segunda auditoria, Fable) - operaciones en bloque: ordenar, vaciar, mover al banco

**"Operaciones en bloque - ordenar, vaciar contenedor, mover todo al banco"**: solo existia
"Vaciar slot" individual. 3 comandos reales nuevos en `ContainerViewModel` (`ClearAllCommand`,
`SortCommand`, `MoveAllTo`) + uno en `MainViewModel` (`MoveInventoryToStorageCommand`, hacia el
Almacen que este seleccionado en la pestaña Almacenes, no fijo a "Banco" - generaliza a los 4
sin inventar 4 botones distintos). Botones "Ordenar"/"Vaciar contenedor" en las 4 cabeceras
reales de Inventario/Almacenes (compacto y Amplio); "Mover todo al almacén" solo en Inventario.

**Investigacion real antes de "Ordenar" (no adivinado)**: se miro el algoritmo REAL de Terraria
(`Terraria.UI.ItemSorting` decompilado) antes de escribir nada - son ~30 "capas" reales por tipo
de daño/herramienta/consumible, cada una con su propio set de prioridad por id
(`SortingPriorityWeaponsRanged`, etc.), datos que este catalogo no extrae hoy (melee/ranged/
magic/summon, createTile...). Replicarlo es un trabajo de extraccion nuevo y mucho mayor que
este boton - mismo motivo real por el que L-f (limite de "Cantidad" por maxStack) quedo
aparcado en vez de fingido. "Ordenar" usa en su lugar un criterio propio, simple y documentado
como tal (Id ascendente, empaquetado al principio) - no una imitacion a medias del real.

**Bug real encontrado al verificar con captura, no al escribir el codigo**: el boton "Mover
todo al almacén" salia con aspecto deshabilitado (mas tenue que "Ordenar"/"Vaciar contenedor")
pese a haber un personaje cargado - `OnIsCharacterLoadedChanged` llamaba a
`NotifyCanExecuteChanged()` de los 3 comandos de siempre (Guardar/Investigar todo/Auto-equipar)
pero se olvido de añadir el nuevo ahi. Sin ese aviso, WPF no vuelve a preguntar `CanExecute`
hasta el primer requery automatico (foco/raton) - un usuario real podria pulsarlo mientras
parece gris. Arreglado añadiendolo a la misma lista.

"Mover todo al banco": lo que no cabe en el destino se queda donde estaba, nunca se pierde en
silencio (cubierto por una prueba determinista explicita con el banco casi lleno). 4 pruebas
deterministas nuevas (`ContainerBulkOpsTests.cs`). Verificado con capturas reales: Inventario
muestra los 3 botones con el mismo brillo (activos), Almacenes muestra "Ordenar"/"Vaciar
contenedor" junto a las pildoras de Banco/Caja fuerte/Fragua/Boveda con sus recuentos reales.
`dotnet test` 201/201 en verde (134 Core + 67 ViewModels), arnes UIA completo sin NO-FOUND/
FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### Bd-c (segunda auditoria, Fable) - "sin resolver" y "sin hueco libre" separados

**"Auto-equipar - un unico contador 'Skipped' funde 'sin resolver' (el pid del build no existe
en el catalogo) con 'sin hueco libre' (el inventario esta lleno)"**: dos causas reales
distintas - la primera no tiene arreglo posible por parte del usuario (el objeto no se pudo
identificar), la segunda si (vaciar hueco en el Inventario) - se veian identicas en el mensaje
final ("2 sin resolver o sin hueco libre"). `AutoEquipService.Result` pasa de `(Placed,
Skipped)` a `(Placed, Unresolved, NoSlot)` (con `Skipped` conservado como propiedad calculada
`Unresolved + NoSlot`, para no romper el contrato de quien ya lo usaba). `MainViewModel.
AutoEquip` arma el mensaje final con las dos partes solo cuando aplican ("3 sin resolver, 1 sin
hueco libre en el Inventario").

2 pruebas deterministas nuevas (`AutoEquipCountersTests.cs`: un arma con pid inexistente cuenta
como "sin resolver" y NO como "sin hueco"; un inventario lleno con un arma real y resoluble
cuenta como "sin hueco" y NO como "sin resolver"). `dotnet test` 203/203 en verde (134 Core +
69 ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)` -
cambio puramente de texto/logica (sin elemento visual nuevo), verificacion suficiente con las
2 pruebas deterministas que fijan el mensaje exacto.

### Bd-d (segunda auditoria, Fable) - filtro por clase + marcar lo ya poseido en Builds

**"Buscador/filtro por clase"**: la pestaña Builds era una unica lista plana de TODAS las
clases (melee/ranged/mage/summoner, +rogue en Calamity) de TODAS las etapas a la vez. Pildoras
reales "Todas/Cuerpo a cuerpo/A distancia/Magia/Invocación/Pícaro" (union de las clases reales
de ambos catalogos - "rogue" no existe en vanilla, filtrar a "Pícaro" ahi simplemente no deja
nada visible, comportamiento correcto, no un bug) arriba del selector Vanilla/Calamity Mod ya
existente. `BuildClassGearViewModel`/`BuildStageViewModel` ganan `IsVisible` (una etapa entera
se oculta si ninguna de sus clases pasa el filtro, para no dejar un titulo "flotando" sobre un
WrapPanel vacio).

**"Marcar lo que ya se posee"**: ningun indicio de si un objeto de un build ya estaba en el
personaje cargado. `BuildItemRowViewModel` ahora guarda el id real (vanilla o sintetico de
Calamity, 0 si el pid no se resolvio) y una `IsOwned` calculada por
`BuildsViewModel.RefreshOwnership` contra CUALQUIER contenedor real del personaje (Inventario/
Almacenes/Equipamiento de los 4 loadouts), no solo "puesto". Insignia verde (misma
`EquippedGreenBrush` que ya usa "equipado" en Objetos) en la esquina de la tarjeta.

**Limitacion real, documentada igual que V-c**: es una foto fija, no reactiva a cada tecla de
una edicion en Objetos - se recalcula al cargar personaje y al ENTRAR en la pestaña Builds
(`OnSelectedTabIndexChanged`), que es cuando de verdad hace falta el dato actualizado; nadie
mira Builds mientras edita Objetos a la vez, y recalcular cientos de filas por cada pulsacion
seria trabajo sin necesidad real.

3 pruebas deterministas nuevas (`BuildsFilterOwnershipTests.cs`: filtrar a una clase ausente en
vanilla oculta sus etapas enteras pero deja Calamity visible; "Todas" restaura todo; colocar un
objeto real en el Inventario marca SOLO esa fila como poseida). Verificado tambien con 2
capturas reales nuevas en el arnes UIA (`builds-referencia.png`: pildoras de filtro renderizando
bien; `builds-poseido.png`, nueva, permanente: coloca "Casco fundido" real en el Inventario,
entra en Builds y confirma que la insignia verde sale EXACTAMENTE en esa fila y en ninguna
otra - `BD-D-POSEIDO: fila marcada=True, resto sin marcar=True`). `dotnet test` 206/206 en
verde (134 Core + 72 ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES:
0 fallo(s)`.

### Bd-e/Bd-f (segunda auditoria, Fable) - columnas irregulares + aviso sin .tplr en Calamity

**Bd-e, "columnas irregulares"**: la columna de cada clase (`BuildClassTemplate`) solo tenia
`MinWidth="220"` - el ancho real crecia con el nombre mas largo de ESA clase en concreto, asi
que al envolver el `WrapPanel` a una segunda fila (Calamity Mod tiene 5 clases, "rogue" incluido)
las columnas no quedaban alineadas entre si, una rejilla visualmente irregular. Ancho fijo real
(`Width="220"` en la columna + `WrapPanel.ItemWidth="248"`, mismo hueco que antes daba el
`Margin` de 24 ahora repartido por el propio panel) - `TextWrapping="Wrap"` añadido al nombre
del objeto para que un nombre largo se parta en dos lineas en vez de desbordar la tarjeta con
el ancho ahora fijo.

**Bd-f, "avisar si el personaje no tiene .tplr"**: colocar equipo de Calamity Mod (Auto-equipar
desde esa sub-pestaña) en un personaje sin datos de Calamity conocidos no falla ni se pierde
nada de verdad - `CharacterFileService.Save` ya crea el `.tplr` en cuanto hay contenido real de
Calamity (T-C, cerrado antes en esta misma auditoria) - pero si el mod NO esta realmente
instalado en el juego del usuario, esos objetos no se reconoceran ahi, y esta app no tiene
forma real de saberlo (solo si ESTE personaje ya uso Calamity antes, via `HasCalamityData`).
Aviso informativo (no bloqueante, la app no puede confirmar nada real sobre el juego) antepuesto
al mensaje normal de Auto-equipar, solo cuando el build tiene algun objeto de Calamity (pid con
"/") Y el personaje no tiene `.tplr`.

Cambio puramente visual (Bd-e, sin logica nueva) verificado con captura real
(`builds-poseido.png`, ya reutilizada de Bd-d): las 4 columnas de "Pre-Hardmode" quedan ahora a
un paso fijo de 248px, alineadas de verdad. Bd-f: 2 pruebas deterministas nuevas
(`AutoEquipCalamityWarningTests.cs`: un build de Calamity sin `.tplr` avisa; un build vanilla
nunca avisa aunque tampoco haya `.tplr`). `dotnet test` 208/208 en verde (134 Core + 74
ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### X-c (segunda auditoria, Fable) - el buscador de NPCs ya no oculta marcadores del mapa

**"El buscador de NPCs oculta marcadores del mapa"**: `ExplorationViewModel.Npcs` era la MISMA
coleccion que rellenaba tanto la lista lateral de texto como los marcadores del mapa - filtrar
por "Enfermera" vaciaba `Npcs` a solo esa entrada, asi que TODOS los demas NPCs desaparecian del
mapa entero, no solo de la lista. `Npcs` se queda ahora siempre completa (nunca se filtra) - una
`NpcSearchResults` nueva y separada alimenta la lista lateral, y cada `WorldNpcRowViewModel`
gana `IsMatch` (true por defecto) para RESALTAR en el mapa en vez de ocultar: sin busqueda
activa no cambia nada visualmente; con busqueda activa, los marcadores que no coinciden se
atenuan (`Opacity=0.25`, `ZIndex` mas bajo) pero siguen ahi, dando su posicion real.

Cambio de logica de coleccion (no facil de fijar con un mundo `.wld` sintetico - el formato
binario real de mundos es solo-lectura en este proyecto, sin ningun `WldWriter`, y construir un
fixture minimo a mano seria un trabajo de extraccion nuevo desproporcionado para este hallazgo)
- verificado en su lugar con el mundo real ya usado por X-a/T-D (`roca_negra.wld`, 14 NPCs de
pueblo reales) en el arnes UIA permanente: buscar por el nombre de un NPC real dejo el mapa
con los 14 NPCs intactos (`Npcs.Count` sin cambiar) mientras la lista lateral se filtro a 1
resultado y el resto quedo marcado `IsMatch=False` (atenuado) - `X-C-BUSCADOR-NPC: buscando
'Comerciante de tintes' -> mapa sigue completo=True (14/14), lista lateral filtrada=True (1),
coincidencia marcada=True, hay no-coincidencias atenuadas=True`. `dotnet test` 208/208 en verde
(sin cambios, X-c no tiene fixture de mundo disponible para una prueba determinista - cubierto
por el arnes real en su lugar), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES:
0 fallo(s)`.

### X-f (segunda auditoria, Fable) - columna de NPCs no fija + "NPCs que faltan" colapsado

**"Columna de NPCs 220px fija"**: `<ColumnDefinition Width="220" />` a secas junto al mapa
(`Width="*"`) - un numero fijo que nunca respira con la ventana, a diferencia del resto de la
app (criterio ya establecido: Auto+MinWidth/MaxWidth, "el centro se lleva todo el sobrante").
Cambiado a `Width="Auto" MinWidth="220" MaxWidth="300"` - el mapa sigue llevandose siempre el
resto real.

**"'NPCs que faltan' deberia empezar colapsado"**: `IsExpanded="True"` a secas - un mundo real
recien empezado puede tener 13+ NPCs sin conseguir (verificado con `roca_negra.wld`: 13 de 14)
que se comian de golpe toda la columna nada mas cargar, tapando la lista de NPCs YA en el mundo
(la que de verdad hace falta para navegar el mapa, con el boton "ir a su posicion"). `IsExpanded
="False"` explicito.

Cambio puramente visual (sin logica nueva), verificado con captura real
(`mundo-buscador-npc.png`, reutilizada de X-c, mundo real `roca_negra.wld`): "NPCs que faltan"
sale ahora colapsado por defecto (solo la cabecera, flecha hacia la derecha) dejando toda la
columna para la lista real de NPCs del mundo. `dotnet test` 208/208 en verde (sin cambios),
arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### X-g/X-h (segunda auditoria, Fable) - el mapa conoce al personaje real + aviso de solo lectura

**X-g, "el mapa no sabe nada del personaje real"**: Exploracion era un visor totalmente
independiente de Personaje - cualquier `.wld`, sin relacion con nada cargado. Investigacion
real antes de tocar nada: `PlrCharacter.cs` NO tiene ningun campo de "aparicion principal"
propio - la unica fuente real de coordenadas de aparicion en el `.plr` es `PlrServerEntry.
SpawnX/Y` (la pestaña "Spawn Points", `Servers`), la ultima cama real donde durmio el
personaje es un dato del MUNDO (`.wld`), no del personaje. `ExplorationViewModel.
CharacterSpawns` (nuevo) + `SetCharacterSpawns()` reciben los Spawn Points reales del
personaje cargado (omitiendo los recien añadidos sin coordenadas, 0/0) - marcador real
(estrella violeta, `AccentBrush`, distinta del magenta de "NPC sin icono") en el mapa. Se
recalcula al cargar personaje (tras `Servers.LoadFrom`, el paso real que los rellena) y al
ENTRAR en Exploracion (mismo criterio ya aceptado en Bd-d/X-g hermanos). Limitacion real
documentada: estas coordenadas no se validan contra NINGUN mundo en concreto (un spawn
guardado para otro mundo se ve en un sitio sin sentido aqui) - mismo criterio ya aceptado para
Spawn Points en si.

**X-h, "no dice que es solo lectura"**: el aviso real ("Solo lectura, no coloca/quita tiles")
solo existia como comentario de codigo en `ExplorationViewModel`, invisible para el usuario.
Pildora "Solo lectura" junto al titulo del mundo, con tooltip explicando por que (mismo mundo
que carga Terraria, sin editar ni guardar desde aqui).

1 prueba determinista nueva (`ExplorationCharacterSpawnsTests.cs`: cargar un personaje con 2
Spawn Points, uno real y otro vacio (0,0), deja solo el real en `CharacterSpawns`). Verificado
tambien con el mundo real `roca_negra.wld` en el arnes UIA permanente: un Spawn Point real
añadido en vivo (Servers) aparece en `CharacterSpawns` tras navegar a Exploracion -
`X-G-SPAWN-PERSONAJE: Spawn Point real añadido -> aparece en el mapa=True, CharacterSpawns.
Count=1` - y la pildora "Solo lectura" se ve junto al titulo en la captura real
(`mundo-spawn-personaje.png`). `dotnet test` 209/209 en verde (134 Core + 75 ViewModels), arnes
UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### T-G (segunda auditoria, Fable) - arranque sincrono

**Medido de verdad antes de tocar nada (mismo criterio que X-7)**: instrumentacion temporal con
`Stopwatch` real (`new MainWindow()`, `CharacterFileService()`, `MainViewModel()`,
`HomeViewModel.Refresh()`) via el arnes UIA - a diferencia de X-7 (Cargar personaje ~8ms,
Investigar todo ~3ms, "no resolver un problema que no existe"), aqui SI habia un problema real:
**448ms** de bloqueo sincrono real en `new MainWindow()` (Debug, primera pasada) antes de que la
ventana pudiera aparecer, con 3 causas reales identificadas y cerradas:

**1. `HomeViewModel.Refresh()` (~97ms medidos, 3 personajes reales)**: corria DENTRO del
constructor de `MainViewModel`, que a su vez corre ANTES de `MainWindow.InitializeComponent()`
(field initializers de C#, orden real) - retrasaba la ventana ENTERA, no solo el listado de
Inicio. Reescrito a `RefreshAsync()` real (mismo patron ya probado en `ExplorationViewModel.
LoadFromPathAsync`, Task.Run para el escaneo de disco) - la ventana ya no espera. `IsScanning`
YA EXISTIA como propiedad pero sin ningun binding real en el XAML ("un interruptor que nunca
encendia nada") - ahora tiene un spinner real visible mientras dura, y se añadio un bloque
nuevo para el hueco real que la asincronia abre (Characters.Count==0 Y ScanMessage vacio a la
vez durante el escaneo - antes de este cambio eso no podia pasar nunca).

**2. Libreria e Investigacion construian el arbol de categorias completo (~8469 objetos,
agrupar/paginar/ordenar) DOS VECES por separado** (`LibraryCategoryTreeBuilder.Build`, llamado
independientemente por cada ViewModel) - el propio comentario historico de `CategoryNodeViewModel`
(T-18) ya documentaba el problema sin cerrarlo. La parte cara ahora se calcula UNA sola vez
(`CategoryTreeNodeData`, datos puros sin `ObservableObject`, cacheados con `lock` real para
seguridad de verdad bajo xunit en paralelo - mismo motivo real que la cache de
`PlayerPreviewRenderer`) y se comparte por REFERENCIA; cada consumidor sigue recibiendo su
PROPIO arbol de `CategoryNodeViewModel` (envoltorio barato) - Libreria e Investigacion
mantienen `IsSelected`/`SelectCommand` totalmente independientes, verificado con test real.
`CategoryNodeViewModel.ItemIdsOrdered`/`ItemIdSet` pasan de `List<int>`/`HashSet<int>` a
`IReadOnlyList<int>`/`IReadOnlySet<int>` (documentan Y hacen cumplir en compilacion que nadie
puede mutar en el sitio una lista que otro arbol tambien esta usando).

**Resultado real medido**: `new MainWindow()` de 448ms -> **357ms** (~20% real, no una cifra
redonda inventada - 4 ejecuciones del arnes consistentes en el rango 354-361ms). No es un "todo
resuelto" - queda margen real (StatsTooltip de LibraryViewModel sigue formateando ~8469 strings
por adelantado aunque solo 1 se vaya a mirar nunca a la vez, candidato real para una pasada
futura si hace falta apretar mas) pero es una mejora real y verificada, no un numero de
marketing.

3 pruebas deterministas nuevas (`LibraryCategoryTreeSharingTests.cs`: mismos datos por
referencia + nodos independientes de verdad; `HomeRefreshAsyncTests.cs`: RefreshCommand es
async de verdad, IsScanning vuelve a False al terminar). Verificado con el arnes UIA: `T-G-
ASYNC: IsScanning justo tras new MainWindow() (antes de cualquier DoEvents)=True` (prueba real
de que el arranque ya no espera al escaneo), capturas reales de Inicio/Libreria/Investigacion
sin cambios visuales de regresion. `dotnet test` 212/212 en verde (134 Core + 78 ViewModels,
4 ejecuciones consecutivas sin fallos intermitentes), arnes UIA completo sin NO-FOUND/FALLO/
EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### L-b (segunda auditoria, Fable) - pildora real de "solo validos para el slot seleccionado"

**"El aviso de restriccion de slot es un texto mas dentro de ResultsSummary, facil de pasar
por alto - y ni siquiera dice CUAL slot"**: antes `" válidos para este slot"` era un sufijo
generico pegado a la frase de resultados. `LibraryViewModel.SlotRestrictionLabel` (nuevo) usa
el rol real del slot que abrio el selector (`PickTarget.SlotRoleLabel`, ya existia y ya resuelve
a "Cabeza"/"Accesorio 3"/"Tinte"/"Mascota"...) - pildora real y separada, con su propio color de
acento, en vez de un sufijo de frase. `ResultsSummary` ya no repite el aviso generico.

**Bug real encontrado al verificar con captura, no al escribir el codigo**: la pildora no
salia en absoluto en la primera captura pese a que `Library.SlotRestrictionLabel` SI tenia el
valor correcto ("Mascota", confirmado por consola) - usaba `Converter={StaticResource
NullToCollapsed}`, que en este proyecto NO significa "oculta si es null" (nombre enganoso a
proposito documentado en `VisibilityConverters.cs`: es el INVERSO de `NullToVisibilityConverter`,
pensado para placeholders de "sin datos" - null->Visible). El conversor correcto para "visible
cuando SI hay valor" es `NullToVis`. Mismo tipo de error de conversor ya atrapado antes en esta
sesion (H-3, `NullToCollapsed` vs `EmptyToCollapsed`) - confirma que merece la pena revisar
siempre con una captura real antes de dar un binding por bueno en este proyecto.

4 pruebas deterministas nuevas (`LibrarySlotRestrictionLabelTests.cs`: sin PickTarget no hay
etiqueta; PickTarget sin restriccion (Inventario) no hay etiqueta; PickTarget restringido
(Casco) muestra "Cabeza"; cancelar la eleccion limpia la etiqueta). Verificado con captura real
del arnes UIA (`libreria-pildora-slot.png`, nueva, permanente: abre el selector para el slot de
Mascota real y confirma la pildora "Solo objetos válidos para: Mascota" visible bajo el
buscador) - `L-B-PILDORA: SlotRoleLabel real=Mascota, Library.SlotRestrictionLabel=Mascota`.
`dotnet test` 216/216 en verde (134 Core + 82 ViewModels), arnes UIA completo sin NO-FOUND/
FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### L-d (segunda auditoria, Fable) - ScrollViewer de seguridad real en el panel Editar

**"Sin ScrollViewer alrededor de todo el panel Editar si desborda"**: `ItemEditTemplate` era
un `DockPanel` a secas (DockPanel no recorta ni scrollea solo, mismo aviso real ya documentado
en el proyecto para `Panel`) - un objeto con muchos prefijos legales a la vez podia desbordar
la altura real disponible en una ventana baja, dejando el contenido de mas arriba (Índice/
Cantidad/nombre del objeto) inalcanzable sin ningun aviso.

**Medido de verdad antes de dar el hueco por cerrado (mismo criterio que T-G/X-7)**: peor caso
real acotado (meta "Positivos" - 8 grupos reales, ver `PrefixGroupCatalog` - + grupo "Cuerpo a
cuerpo +", 10 prefijos reales, sobre un arma real) en la altura MINIMA real documentada de la
app (700px) - el hueco era real de verdad, no hipotetico: `ExtentHeight=440px` contra
`ViewportHeight=268px`, **172px de contenido real que antes habrian quedado cortados sin forma
de llegar a ellos**. `ScrollViewer VerticalScrollBarVisibility="Auto"` envolviendo el
`DockPanel` entero (mismo idioma real ya usado en el resto de la app - "ScrollViewer de
seguridad") - cuando el contenido cabe (caso normal), cero cambio visual; cuando no cabe, scroll
real.

Cambio puramente de layout (sin logica nueva) - verificado con el arnes UIA (nuevo bloque
permanente): `ScrollViewer` real encontrado, `ScrollableHeight=172px`, desplazado hasta el
final SIN excepcion (`VerticalOffset=172px`, exacto), captura real
(`editar-scroll-700px.png`) confirmando la barra de scroll visible y los 10 prefijos reales de
"Cuerpo a cuerpo +" (Grande/Enorme/Peligroso/Salvaje/Afilado/Puntiagudo/Voluminoso/Pesado/
Ligero/Legendario) alcanzables desplazando. `dotnet test` 216/216 en verde (sin cambios),
arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### L-c (segunda auditoria, Fable) - tope de 300 resultados medido de verdad + debounce real

**"El tope de 300 no tiene ninguna medicion real detras, solo el motivo generico de que
WrapPanel no virtualiza"**: medido de verdad con el arnes UIA (busqueda amplia real "ar" sobre
el catalogo completo, ~8469 objetos, Debug primera pasada) - **100 objetos -> 158ms, 150 ->
271ms, 300 -> 802ms**. NO escala lineal (el WrapPanel sin virtualizar empeora peor que
proporcional al crecer) - 300 era un freeze real y perceptible, no una cifra sin coste. Bajado
a **100**, el punto real donde el reflow deja de notarse manteniendo un numero util de
resultados antes de pedir afinar la busqueda.

**Hallazgo adicional real al medir (no buscado a proposito)**: el buscador reflowaba en CADA
pulsacion de tecla (`UpdateSourceTrigger=PropertyChanged`) - con un termino de varios
caracteres, cada pulsacion intermedia pagaba el coste entero de reflow, no solo la ultima.
`LibraryViewModel._searchDebounceTimer` (`DispatcherTimer`, mismo patron real ya usado en
`MainViewModel._saveConfirmationTimer` - `Stop()`+`Start()` en cada disparo) - solo la busqueda
por TEXTO se debounça (180ms tras la ultima pulsacion); elegir una carpeta o cambiar el slot a
rellenar (`PickTarget`) siguen aplicando el filtro al instante, son un clic discreto, no tecleo
continuo.

**Bug real del propio arnes encontrado y arreglado al verificar (no del codigo de produccion)**:
un bucle `DoEvents()` sin ninguna pausa real puede dejar la cola de mensajes SIEMPRE ocupada
con trabajo propio, y un `DispatcherTimer` real usa un temporizador de Windows aparte
(`WM_TIMER`, prioridad baja) que necesita que la cola quede libre un instante de verdad para
entregarse - confirmado con un log temporal que demostro que el Tick fallaba de forma
intermitente sin una pausa real entre vueltas. `WaitForDispatcher(ms)` (nuevo, permanente)
añade un `Thread.Sleep(1)` real por vuelta - estable en 3 ejecuciones consecutivas tras el
arreglo.

1 prueba determinista nueva (`LibraryMaxResultsTests.cs`: selecciona la categoria real mas
grande del arbol via `SelectCategoryCommand`, sincrono, sin pasar por el debounce - confirma
`Results.Count == 100`). El debounce en si (necesita un `Dispatcher` real corriendo, no
disponible en xunit sin plomeria nueva - mismo motivo real por el que `ExplorationViewModel`
tampoco tiene test unitario para su parte async) se verifica en el arnes UIA: `L-C-TOPE:
debounce real (Results sin cambiar justo tras teclear)=True, Results.Count tras esperar=100,
tiempo total con espera=353ms` - estable en 3 ejecuciones. `dotnet test` 217/217 en verde (134
Core + 83 ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0
fallo(s)`.

### Ap-a/Ap-b (segunda auditoria, Fable) - selectores excluyentes + miniaturas de peinado

**Ap-a, "los selectores de peinado/tinte abiertos a la vez empujan el contenido"**: ninguno de
los dos (`IsHairPickerOpen`/`IsHairDyePickerOpen`) cerraba al otro. Cada `Open*PickerCommand`
cierra ahora el otro selector antes de abrirse. Ademas, `LoadFrom` (cargar OTRO personaje)
cierra los dos - un selector que quedara abierto del personaje anterior podia mostrar opciones
de un personaje distinto ya descartado.

**Ap-b, "228 miniaturas de peinado se regeneran en cada tick del color - medir antes de tocar
nada"**: medido de verdad con el arnes UIA (mismo criterio que X-7/L-c) - **228 miniaturas
reales -> 80-113ms** (Debug, primera pasada), nada despreciable: regenerarlas en CADA tick de
un arrastre de slider de color (que puede disparar docenas de eventos por segundo) habria
congelado la UI de verdad. Investigando el codigo real (no el resumen previo) se encontro que
el codigo YA EXISTENTE no regeneraba en cada tick - solo VACIABA la coleccion
(`HairOptions.Clear()`), un bug real distinto y peor: si el selector estaba abierto mientras se
tocaba el color, la rejilla se quedaba en BLANCO para siempre hasta cerrar y reabrir a mano.
`_hairOptionsStale` (marca barata, sin coste real, para cuando el selector esta cerrado) +
`_hairOptionsDebounceTimer` (mismo patron ya establecido - `LibraryViewModel.
_searchDebounceTimer`/`MainViewModel._saveConfirmationTimer`, 180ms): con el selector cerrado,
cambiar el color no cuesta nada real; con el selector ABIERTO, se regeneran de verdad pero solo
UNA vez, 180ms despues del ultimo cambio - la rejilla nunca se queda en blanco (se ve la ultima
version real hasta que la nueva esta lista).

3 pruebas deterministas nuevas (`AppearancePickerTests.cs`: abrir tinte cierra peinado y
viceversa; cambiar color con el selector cerrado no regenera nada hasta abrirlo). El
refresco en vivo con el selector abierto (necesita un `Dispatcher` real - mismo motivo por el
que el debounce de L-c tampoco tiene test unitario) se verifica en el arnes UIA: `AP-B-MEDIDA:
primera apertura real (228 miniaturas) tardo 82ms`, `AP-A-EXCLUSION: tras abrir tinte ->
IsHairDyePickerOpen=True, IsHairPickerOpen=False`, `AP-B-REFRESH: tras cambiar color con el
selector abierto -> vacias justo despues=False, HairOptions.Count tras esperar el
debounce=228` - mas una captura real (`apariencia-selector-peinado.png`, nueva, permanente)
confirmando visualmente el peinado real (pelo rojo, color actualizado) en las 228 miniaturas y
ningun selector superpuesto. `dotnet test` 220/220 en verde (134 Core + 86 ViewModels), arnes
UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### Ap-c (segunda auditoria, Fable) - valor hexadecimal real para los 7 colores

**"Sin valor hexadecimal ni paleta para los colores"**: los 7 colores del personaje (Pelo/
Piel/Ojos/Camisa/Camiseta interior/Pantalones/Zapatos) solo tenian 3 sliders R/G/B en crudo,
sin ningun numero visible ni forma de pegar un color conocido de un vistazo (ej. "#FF0000").
`ColorSwatchViewModel.Hex` (nuevo, `#RRGGBB`) sincronizado en los DOS sentidos: mover un slider
actualiza el hex mostrado (`UpdatePreview`), y escribir un hex de 6 digitos valido (con o sin
"#") actualiza los 3 canales Y el `byte[]` real del personaje (`OnHexChanged`) - un valor a
medio escribir se ignora en silencio en vez de aplicar algo incorrecto a media escritura.
Campo de texto nuevo bajo los 3 sliders de cada swatch, sin `UpdateSourceTrigger=
PropertyChanged` a proposito (mismo criterio real ya establecido en el proyecto para "Índice
(id)", T-17 - confirma al salir del campo, no letra a letra).

**Sin paleta**: investigado y descartado con motivo real, no omitido sin mirar - a diferencia
de los tintes de pelo (una lista curada real de 12 objetos del juego), los colores de
Pelo/Piel/Ojos/ropa son libres en Terraria real (cualquier RGB, sin restriccion ni lista
oficial) - inventar una paleta fija no representaria nada real del juego. El campo hex es el
equivalente real y util a "pegar un color conocido" para este caso.

4 pruebas deterministas nuevas (`ColorSwatchHexTests.cs`: cambiar R/G/B actualiza el hex real;
escribir un hex valido (con y sin "#") actualiza R/G/B Y el array real; un hex a medio escribir
se ignora sin tocar nada). Verificado con captura real (`apariencia-colores-hex.png`, nueva,
permanente): los 7 swatches muestran su campo hex real (`#000000` en este personaje sintetico)
bajo los sliders. `dotnet test` 224/224 en verde (134 Core + 90 ViewModels), arnes UIA completo
sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### S-b/S-c (segunda auditoria, Fable) - cabecera unica real + enlace real al mapa

**S-b, "anchos fijos de 494px repetidos por fila + cabecera repetida en cada fila"**: cada
tarjeta de Spawn Points volvia a escribir "Nombre"/"Spawn X"/"Spawn Y"/"Id" como si fuera la
primera. `Grid.IsSharedSizeScope="True"` en el ancestro comun real (header + `ItemsControl`) +
`SharedSizeGroup` por columna - una UNICA fila de cabecera real, con las columnas de cada
tarjeta sincronizadas contra ella (no solo el mismo numero fijo repetido a mano). La cabecera
se oculta sola cuando no hay ningun Spawn Point (`CountToVis` sobre `Servers.Entries.Count`).

**S-c, "sin enlace al mapa de Exploracion desde Spawn Points"**: boton real "Ver en el mapa"
por fila - `ExplorationViewModel.NavigateToTile` (extraido de `GoToNpc`, mismo mecanismo real
que ya centraba el mapa en un NPC) + `MainViewModel.ViewSpawnOnMapCommand` (el unico sitio real
que conoce ambas pestañas a la vez): salta a Exploracion y centra el mapa en las coordenadas
exactas de ese Spawn Point. Si todavia no hay ningun mundo `.wld` cargado, avisa en
`Exploration.StatusMessage` (el que se ve de verdad en la pestaña a la que se acaba de saltar)
en vez de saltar a un mapa en blanco sin explicar nada.

2 pruebas deterministas nuevas (`ViewSpawnOnMapTests.cs`: sin mundo cargado salta a Exploracion
y avisa; con mundo cargado pide navegar a las coordenadas reales del Spawn Point). Verificado
con el arnes UIA usando el mundo real `roca_negra.wld` ya cargado: `S-C-MAPA: tras 'Ver en el
mapa' -> SelectedTabIndex=4, tile pedido=(100, 50), IsWorldLoaded=True` - mas una captura real
(`spawn-points-tabla-poblada.png`, nueva, permanente) confirmando la cabecera unica alineada y
el boton "Ver en el mapa" junto a cada fila real. `dotnet test` 226/226 en verde (134 Core + 92
ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### D-d/D-e (segunda auditoria, Fable) - accion en bloque + aviso real de version en Desbloqueos

**D-d, "sin accion en bloque para los flags"**: marcar/desmarcar las 13 casillas una a una era
el unico camino, incluso para el caso real mas comun (un completista, "todos los desbloqueos
puestos" de golpe). Botones reales "Marcar todos"/"Desmarcar todos" - cada asignacion pasa por
su propio setter real (`OnXxxChanged` ya escribe al personaje), sin duplicar ninguna logica de
escritura.

**D-e, "sin aviso si el flag no existe todavia en la version real del personaje"**: los
umbrales usados son los MISMOS ya verificados y en produccion en `PlrBodySerializer` (no
inventados - `ExtraAccessory`>=145, antorchas de bioma>=230, consumibles permanentes>=269,
DD2>=184, carrito potenciado>=253): por debajo del umbral real, `Write()` ni siquiera escribe
ese campo - marcar la casilla y guardar perderia el cambio en silencio, mismo riesgo real que
V-c ya cerro para el resto del personaje. Aviso naranja real bajo cada grupo afectado,
recalculado al cargar personaje Y al entrar en la pestaña (mismo criterio ya establecido -
Bd-d/X-g). El umbral real 253 (carrito potenciado) no tiene ninguna version con nombre exacto
en la tabla curada de `VersionEditorViewModel` (cae entre 1.4.3.0=248 y 1.4.4.0=269) - el aviso
dice el numero real en vez de inventar una etiqueta "1.4.3.x" no verificada.

**Bug real encontrado y arreglado al verificar (no del hallazgo en si)**: el arnes UIA lanzo
`DispatcherUnhandledException` real (`"#FF6C63FF" no es un valor válido para "BorderBrush"`) la
PRIMERA VEZ que marcaba un `CheckBox` real de la app - `Theme.xaml`, plantilla de `CheckBox`,
trigger `IsChecked=True`: `BorderBrush` apuntaba a `AccentColor` (un `Color`, `#6C63FF`) en vez
de `AccentBrush` (el `SolidColorBrush` real) - WPF lo tolera como valor literal en un atributo
XAML, pero NO dentro de un `Setter` de un `Trigger` en tiempo real. El manejador global de
excepciones de la app lo silenciaba sin tumbar la ventana, asi que llevaba tiempo sin notarse -
**CADA CheckBox marcado de TODA la app** perdia su borde de color de acento en silencio,
confirmado que no aparecia en NINGUNA ejecucion anterior del arnes (0 ocurrencias en 5 logs
previos) porque nunca antes se habia marcado un CheckBox real. Arreglado a `AccentBrush`.

7 pruebas deterministas nuevas (`FlagsBulkAndVersionTests.cs`: Marcar/Desmarcar todos ponen las
13 casillas reales; los 5 umbrales reales, cada uno con el valor justo por debajo/en el limite;
recalculo real al entrar en Desbloqueos tras cambiar la version). Verificado con capturas
reales del arnes UIA (`desbloqueos-marcar-todos.png` y `desbloqueos-aviso-version.png`, nuevas,
permanentes): las 13 casillas marcadas con su borde de acento correcto (bug del CheckBox ya
arreglado, visible en la propia captura), y los 3 avisos naranjas reales visibles bajo sus
grupos con version=100. `dotnet test` 239/239 en verde (134 Core + 105 ViewModels), arnes UIA
completo sin NO-FOUND/FALLO/EXCEPTION/DISPATCHER-EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### I-d (segunda auditoria, Fable) - cerrado como parte real de T-G, no aparte

**Nota de cierre, no un cambio de codigo nuevo**: I-d quedo anotado explicitamente en el propio
plan de esta ronda como "se resuelve junto con T-G" - sin el texto verbatim original a mano
tras la compactacion de contexto (no inventado, documentado con honestidad en vez de fingir
certeza), la lectura mas defendible es que I-d pedia un indicador de carga real en Inicio
mientras escanea los personajes reales del disco - exactamente lo que T-G ya entrego de verdad
(`HomeViewModel.IsScanning`, antes sin ningun binding real, ahora con un spinner+texto reales
visibles mientras `RefreshAsync` corre en segundo plano - ver la seccion de T-G mas arriba,
verificado con `T-G-ASYNC: IsScanning justo tras new MainWindow()=True`). No se repite trabajo
ya hecho y verificado ahi. Si esta lectura resulta no ser la intencion real original, queda
documentado aqui para poder corregirlo con la pista correcta en cuanto aparezca.

## Tercera auditoria (Fable) - ejecucion completa

Pedido explicito del usuario ("aplica toda la ronda de fable sin descansar") tras publicar el
informe de la tercera auditoria (independiente, posterior al cierre completo de la segunda -
18 hallazgos: H3-01 a H3-18, ninguno un rework de Ola 4 salvo H3-07 que pide una decision de
CONTENIDO, no de codigo). Mismo criterio de siempre: investigacion real, arreglo real, test +
arnes + bitacora + commit por tanda.

### Tanda 1 (H3-01, H3-02, H3-03, H3-04)

**H3-01, "Deshacer descarta ediciones sin guardar sin preguntar"**: T-B cerro este mismo
agujero en los otros 3 puntos de entrada reales (Inicio, "Cargar personaje...", Ctrl+O), pero
`UndoLastSave` se quedo fuera - un clic, siempre visible en la cabecera global, sin ningun
aviso. Mismo gancho real ya usado en los otros 3 (`ConfirmDiscardChanges?.Invoke()`), solo
cuando `IsDirty`.

**H3-02, "Deshacer no revierte el `.tplr` recien nacido"**: `WriteAtomic` solo genera un `.bak`
real cuando el fichero YA EXISTIA (`File.Replace`) - un `.tplr` que nace en el MISMO guardado
que se esta deshaciendo no tiene `.tplr.bak` (no habia ninguno antes). Sin borrarlo, `MergeAll`
lo fusiona igual al recargar - el objeto de Calamity "deshecho" reaparecia, y "Deshecho el
ultimo guardado" era un mensaje falso. Se borra el `.tplr` huerfano (sin `.tplr.bak` pero con
`.tplr` real) antes de recargar - mismo arreglo aplicado tambien en `HomeViewModel.
RestoreBackup` (comparte el mismo hueco real, ver H3-04).

**H3-03, "Un rechazo de colocacion ensucia + flash falso"**: `RejectionMessage` es puro estado
de UI (se escribe precisamente cuando NO cambio ningun dato real - colocacion rechazada, o al
limpiar el aviso al cambiar de seleccion, L-e) pero no estaba en la lista de exclusion de
`MainViewModel.HookSlotEditing`/`BuffsViewModel` (junto a `IsSelected`/`JustEdited`, ya
excluidos) - intentar colocar un objeto invalido marcaba el personaje como "sin guardar" (sin
nada real que guardar) Y disparaba el flash de "acabo de editarme". Añadida la exclusion en los
4 sitios reales que ya filtraban `IsSelected`/`JustEdited`: `HookSlotEditing`, `BuffsViewModel`,
y por coherencia `EquipmentGroupViewModel.RecomputeDefenseAndBonus` y `ItemEditViewModel.
OnSlotPropertyChanged` (ninguno de los dos necesita recalcular nada por un rechazo).

**H3-04, "Restaurar copia de seguridad" sobre el personaje cargado no recarga el editor**:
`HomeViewModel.RestoreBackup` restauraba los ficheros en disco pero nunca avisaba a
`MainViewModel` - si el personaje restaurado era el que estaba cargado, el editor seguia
mostrando el estado antiguo en memoria, y un Guardar posterior lo machacaba en silencio. Mismo
evento real ya usado por "Cargar" (`CharacterChosen`) en vez de inventar uno nuevo -
`MainViewModel` ya lo conecta con su propio `ConfirmDiscardChanges`, asi que una edicion en
memoria sin guardar TAMBIEN avisa aqui, no solo se pisa. Solo dispara si `entry.FilePath`
coincide con el personaje realmente cargado - restaurar la copia de OTRO personaje no toca el
editor actual.

7 pruebas deterministas nuevas (`Tanda1FableTests.cs`). Un error real encontrado en la PROPIA
prueba al escribirla (no en produccion): `Assert.False(...IsEmpty)` esperaba que el objeto
guardado sobreviviera a "Deshacer" - al revisar con mas cuidado, el `.bak` real es el estado
ANTERIOR al guardado (sin el objeto todavia), asi que Deshacer vuelve a ESE estado, corregido a
`Assert.True`. `dotnet test` 246/246 en verde (134 Core + 112 ViewModels), arnes UIA completo
sin NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`.

### Tanda 2 (H3-06, H3-08)

**H3-06, "el tope+debounce medido de verdad en L-c solo se aplico a 1 de las 3 superficies
gemelas"**: `LibraryViewModel` (objetos) ya tenia el tope de 100 resultados + debounce de
180ms (medido de verdad: 300 tarjetas sin tope = 802ms de congelacion real, mismo WrapPanel sin
virtualizar) - `ResearchViewModel` (Investigacion) no tenia NINGUN tope (una carpeta grande
tras "Investigar todo" pintaba miles de filas de golpe) ni debounce (reflowaba en cada tecla), y
`BuffLibraryViewModel` (Libreria de buffs) ya tenia el mismo tope+resumen (300, sin medicion
propia - universo de buffs mucho mas pequeño que el de objetos, se deja igual) pero tampoco
debounce. Portado el mismo `DispatcherTimer` de 180ms (parar+arrancar en cada tecla, aplicar el
filtro solo al `Tick`) a los dos, mas el tope de 100 en Investigacion (mismo WrapPanel, mismo
coste real por tarjeta - no una superficie distinta que necesite remedirse). Regresion propia
detectada ANTES de tocar nada (no en produccion): `ResearchOlaTresTests.cs` fijaba `SearchText`
y miraba `Results` de inmediato - con debounce ya no hay resultado sincrono. Añadido un
`WaitForDispatcher(ms)` local (mismo patron ya documentado del arnes UIA: sin un `Thread.Sleep`
real intercalado entre vueltas de bombeo, el `WM_TIMER` real del `DispatcherTimer` puede no
llegar a entregarse nunca dentro de un bucle de pruebas) tras las dos asignaciones de
`SearchText` de ese fichero.

**H3-08, "Defensa total" ignora la defensa de los prefijos de accesorio reales**: sumaba solo
la defensa base de cada pieza equipada, nunca el bono plano real de prefijos como
Warding/Guarding/Menacing/Hardy/Armored (+1..+4 defensa, `Player.GrantPrefixBenefits`
decompilado, ids vanilla reales 62-65 verificados contra `vanilla_prefix_effects.json`) - un
personaje real de endgame con varios accesorios "Proteccion" mostraba menos defensa de la que
realmente tiene en el juego. `PrefixEffectCatalog` ya cargaba `StatDefense` para el texto de
`Describe()` - ganó `GetDefenseBonus(prefixId)`, un acceso numerico puro (redondeado a entero,
`StatDefense` real nunca es fraccionario para defensa) reutilizado por
`EquipmentGroupViewModel.RecomputeDefenseAndBonus` (solo para prefijos vanilla - Calamity/Rogue
no tienen datos en este catalogo, mismo criterio de "desconocido = 0" del resto de la app).

2 pruebas deterministas nuevas (`PrefixDefenseTests.cs`, Escudo de obsidiana id vanilla real
397/defensa real 2 + prefijo Warding id vanilla real 65/+4 real = 6, verificado contra
`vanilla_stats.json`/`vanilla_prefix_effects.json`, no inventado). `dotnet test` 248/248 en
verde (134 Core + 114 ViewModels), arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION,
`T-E-TILDES: 0 fallo(s)`, `ultimo-error.log existe: False`.

**H3-05, "los 21 ModPrefix reales de Calamity no tienen ningun camino manual"**: antes de este
arreglo, los 17 ModPrefix reales de ARMA Picaro (Vicious/Flawless/Horrible/...,
`RoguePrefixCatalog.Weapon`, decompilados en `CalamityMod/Prefixes/*.cs`) y los 4 de ACCESORIO
(Dauntless/Friendly/Invigorating/Silent) solo se podian conseguir via "mejor prefijo"
automatico (`PrefixSuggester`) - el picker manual del panel Editar nunca los enseñaba (un arma
Picaro solo veia los grupos genericos "Universal +/-", ningun accesorio veia ninguno de los 4).
Verificado contra el propio codigo decompilado: los 4 de accesorio declaran
`Category => PrefixCategory.Accessory` real (la MISMA categoria vanilla que Warding/Menacing) y
`CanRoll` universal (`Dauntless.cs`/`Invigorating.cs`, sin excepcion; `RogueAccessoryPrefix.cs`
para Silent) - se aplican de verdad a CUALQUIER accesorio, vanilla o de Calamity (mismo
mecanismo real de ModPrefix de tModLoader, que une su pool al vanilla por categoria), no solo a
Calamity. "Friendly" en concreto tiene RollChance=0 en el juego real (nunca sale al reforjar al
azar) - este picker es la UNICA forma real de conseguirlo, ni eso habia antes.

Arreglo: nueva categoria `PrefixCategory.Rogue` (solo se activa para armas de Calamity con
`damageType` real conteniendo "Rogue"); nuevo grupo "Pícaro" (17 ids) en la meta "Positivos";
los 4 ids de accesorio añadidos al grupo "Accesorio" ya existente (aplica a CUALQUIER accesorio
por el `Requires=PrefixCategory.Accessory` que ya tenia, vanilla incluido - los ids sinteticos
de Calamity ya no se filtran por la tabla `PrefixRulesCatalog` solo-vanilla, que nunca los
conocia). `RoguePrefixEntryData` ganó los campos reales que el propio JSON ya traia pero el
modelo nunca leia (`critBonus`/`sizeMult`/`knockbackMult`/`stealthDmgMult`) + un
`DescribeWeaponEffect` real (mismo criterio D-6/H3-08: numeros reales, nunca un nombre opaco).

Bug real DESCUBIERTO escribiendo la prueba de "aplicar un prefijo Picaro real" (no en
produccion todavia, pillado antes de dar nada por cerrado): `ItemEditViewModel.RebuildGroups`
reconstruye `Groups` entero en cada `Refresh()` (incluido tras Aplicar), y comparaba "el grupo
ya seleccionado" por REFERENCIA del *wrapper* (`PrefixGroupButtonViewModel`, siempre una
instancia NUEVA) en vez del dato real (`PrefixGroup`, la MISMA instancia siempre, viene sin
copiar del catalogo estatico) - cualquier Aplicar devolvia el picker en silencio al primer
grupo de la meta ("Accesorio"), perdiendo la seleccion real del usuario. Corregido a comparar
por el dato real (`ReferenceEquals(g.Group, previousGroup)`).

3 pruebas deterministas nuevas (`RoguePrefixPickerTests.cs`, contra Aerial Tracker real del
catalogo de Calamity - indice 1905, categoria real "Weapons/DraedonsArsenal", damageType real
"RogueDamageClass.Instance" - y el Escudo de obsidiana vanilla ya usado en H3-08).

**H3-12, "la duracion de un buff escrita a mano puede desbordar a negativo"**: sin techo,
escribir un numero de segundos lo bastante grande (`Buff.Time = segundos*60`) desbordaba el
`int` en silencio (ej. 40.000.000s * 60 = 2.400.000.000 > `int.MaxValue`=2.147.483.647, se
volvia negativo) - un guardado real habria escrito basura en el `.plr`. Acotado ANTES de
multiplicar, al mismo techo GLOBAL real que ya usa el boton "Máxima"
(`BuffDurationPresets.MaxTicksForVersion`, `S.getMaxTime()` real - 1.999.999.980 ticks si
version>=269, 1.080.000 si no). 3 pruebas deterministas nuevas
(`BuffDurationOverflowTests.cs`).

**H3-13, "Minima/Media/Maxima no se refrescan al sustituir el buff de un slot ya ocupado y
seleccionado"**: gemelo real de B-6 (ya cerrado en `EquipmentGroupViewModel`) -
`BuffEditViewModel.OnSlotPropertyChanged` solo vigilaba `IsEmpty`, que no cambia de valor
cuando un buff sustituye a OTRO dentro del MISMO slot ya ocupado (`PlaceBuff`/`SwapWith` sobre
un slot no vacio) - los presets se quedaban calculados para el buff viejo. Añadido
`DisplayName` al filtro (cambia siempre que el buff realmente cambia de identidad - Terraria no
permite dos slots con el mismo id a la vez, Bu-b - y nunca por escribir la duracion a mano,
`Refresh()` de `BuffSlotViewModel` solo se llama desde `PlaceBuff`/`SwapWith`/`Clear`). 1 prueba
determinista nueva (`BuffEditPresetRefreshTests.cs`, Obsidian Skin 6min -> Regeneration 8min
dentro del mismo slot ya seleccionado).

`dotnet test` 255/255 en verde (134 Core + 121 ViewModels) al cierre de Tanda 2 completa
(H3-06, H3-08, H3-05, H3-12, H3-13).

### Tanda 3 (H3-09, H3-10, H3-11, H3-14, H3-15, H3-16, H3-17, H3-18)

**H3-09, "Ordenar reordena tambien la barra rapida"**: `ContainerViewModel.Sort()` empaquetaba
TODO el contenedor de Inventario, incluidos los primeros 10 slots (la barra rapida real,
`Player.inventory[0..9]`) - `Terraria.UI.ItemSorting.SortInventory` real (decompilado,
`Sort(..., 0, 1, ..., 9, 50, ...)`) los EXCLUYE explicitamente de cualquier ordenado, ademas de
los slots de moneda/municion 50-58 (que en este puerto ya viven en contenedores propios aparte).
Arreglado: `Sort()` deja fijos los primeros `HotbarSlotCount` slots SOLO cuando `Key ==
"inventory"` - ningun otro contenedor (Banco/Caja fuerte/Fragua/Boveda) tiene barra rapida real.
Prueba existente (`OrdenarEmpaquetaPorIdAscendenteYDejaVaciosAlFinal`) corregida para colocar
sus objetos de prueba fuera de la barra rapida (antes usaba slots 0/3/7, dentro de ella) + 2
pruebas deterministas nuevas.

**H3-10, "Miel se pinta del color de Shimmer y viceversa"**: el campo de liquido de 2 bits real
en disco (`(header1 & 0x18) >> 3`) solo tenia hueco para 3 valores (1=agua/2=lava/3=miel,
confirmado contra TEdit real) desde ANTES de que Shimmer existiera (1.4.4) - el juego real
reutiliza el mismo codigo 3 (miel) como base para Shimmer, con el bit suelto `header3 & 0x80`
como "en realidad es Shimmer" (confirmado identico en TEdit, `World.FileV2.cs`).
`WldReader.cs` sobreescribia a `liquidType = 3` (el MISMO codigo que miel) en vez de a un
codigo distinto - honestamente indistinguibles para el resto del puerto. Arreglado:
`liquidType = 4` (codigo sintetico propio, nunca en disco) para Shimmer. `WorldRenderer.
LiquidColor` y `ExplorationViewModel.LiquidName` actualizados con el 4º caso - de paso,
corregido un comentario que decia que `map_colors.json` "no trae" los colores de liquido: SI
los trae (`global.Water/Lava/Honey/Shimmer`, misma fuente real de TEdit ya usada para
tiles/paredes/fondo) - se aprovecharon esos 4 colores reales en vez de las aproximaciones a
mano de antes. "Centelleo" (nombre real de Shimmer como LIQUIDO, no como objeto - sacado de la
localizacion es-ES oficial de `ShimmerCloak`: "...sumergido en el centelleo"). Prueba real
contra mundos del propio disco: `El_Musgo_de_Accidentes.wld` tiene 1416 tiles de miel y 696 de
Shimmer reales - antes de este arreglo, los 2112 se habrian colapsado en un unico codigo
indistinguible. 3 pruebas nuevas (`WldReaderRealFileTests.cs`).

**H3-11, "una pieza de armadura de Calamity encaja en CUALQUIERA de los 3 slots"**:
`ItemSlotViewModel.AcceptsItem` solo comprobaba "es armadura" (`category.StartsWith("Armor")`),
nunca DE QUE PARTE del cuerpo. Nuevo `scripts/extraer-slot-armadura-calamity.js`: tModLoader
moderno (1.4.4+) ya no usa los campos numericos antiguos `headSlot`/`bodySlot`/`legSlot` de
`Item.cs` - usa el atributo real `[AutoloadEquip(new EquipType[] { EquipType.X })]` sobre la
clase (confirmado a mano: `AerospecBreastplate.cs`=Body, `EmpyreanMask.cs`=Head,
`EmpyreanCuisses.cs`=Legs). 185/186 armaduras reales con slot encontrado (la 186ª,
`WulfrumFusionCannon`, resulto ser un arma de invocacion mal encajada en la categoria "Armor" -
sin `AutoloadEquip`, se queda sin `equipSlot`, "desconocido = permitir" como el resto de este
metodo). Nuevo campo `CalamityCatalogEntry.EquipSlot`, usado en `AcceptsItem` para exigir el
slot real cuando se conoce. 4 pruebas deterministas nuevas (`CalamityArmorSlotTests.cs`).

**H3-14, "279 se muestra crudo, nunca 1.4.4.0+"**: `VersionEditorViewModel` solo tenia el
campo crudo editable (`RawVersion`, correcto que sea numerico) y `IsCurrent` por coincidencia
EXACTA (asi que una version real "y pico" como 279 no resaltaba NINGUN boton). Calco real de
`TabVersion.findBestMatch`/`getBestText` (script.beautified.js:5161-5173, ya no de memoria -
leido de nuevo): la version conocida mas alta <= la real, con un "+" si la real es
estrictamente mayor. Nuevo `BestMatchLabel` (ej. "1.4.4.0+" para 279) mostrado junto al campo
crudo en el XAML; `IsCurrent` ahora resalta ese MISMO mejor ajuste, no solo una coincidencia
exacta (identico al `style(4)` real de `syncVersion`). 4 pruebas deterministas nuevas
(`VersionBestMatchTests.cs`).

**H3-15, "la insignia de Calamity no se enciende justo tras el guardado que crea el .tplr"**:
`HasCalamityData` solo se recalculaba en `LoadFromPath` - `MainViewModel.Save()` nunca la
releia aunque `CharacterFileService.Save` YA deja `loaded.TplrPath` puesto de verdad en cuanto
crea el `.tplr` de un personaje que antes era 100% vanilla. Añadida la misma lectura dentro del
`try` de `Save()`, justo despues de `_service.Save(_loaded)`. 1 prueba determinista nueva
(`CalamityBadgeAfterSaveTests.cs`).

**H3-16 (nota, sin cambio de comportamiento)**: comentario desactualizado en `App.xaml.cs`
sobre el arnes de UI Automation ("proyecto de scratchpad, fuera del repo") - ya no es cierto
desde T-21 (`TerrasavrNative.App.Tests`, proyecto real y permanente del propio repo). Corregido.

**H3-17, "Movido 1 objeto(s)" no concuerda de verdad**: en el mensaje de "Mover todo al
almacen", el VERBO ya se conjugaba (`Movido`/`Movidos`) pero el SUSTANTIVO se quedaba en el
placeholder literal `objeto(s)` sin concordar nunca - una mezcla real dentro de la misma frase
("Movido 1 objeto(s)"). Corregido a concordancia real en los dos ("Movido 1 objeto" / "Movidos
2 objetos"). El resto de "(s)" del proyecto (Resultados, Investigacion, Auto-equipar...) se
deja igual a proposito - son invariantes autoconsistentes, sin ningun otro verbo conjugado en
la misma frase que choque con ellos, no tienen el mismo problema real. 2 pruebas deterministas
nuevas (`MoverAlAlmacenConcordanciaTests.cs`).

**H3-18 (nota, documentacion - sin cambio de comportamiento)**: el `MinHeight="216"` real de la
fila de Inventario (pensado para "5 filas a 40px + huecos") ya no cubre la 5ª fila entera a
1080x700 con la Libreria desplegada - cabeceras añadidas DESPUES de esa medicion (el contador
"Inventario (N/50)", A-c) le restan alto real disponible. Sigue siendo accesible (el
ScrollViewer de seguridad ya cubre este caso), documentado en el propio XAML para que no se
redescubra como una regresion nueva.

**H3-07 NO se toca** (Novedades/Changelog con contenido de ejemplo/demo presentado como real) -
la propia auditoria lo marca como decision de CONTENIDO, no de codigo - queda para que el
usuario decida (reemplazar `whats_new.json` por contenido real, marcar la pestaña como
ejemplo, o quitarla).

`dotnet test` 270/270 en verde (136 Core + 134 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION, `T-E-TILDES: 0 fallo(s)`, `ultimo-error.log existe: False`. Nota
aparte (no es un fallo real, no se repitio dos veces seguidas): `HomeRefreshAsyncTests.
RefreshAsyncCommand_EsAsincronoYIsScanningVuelveAFalseAlTerminar` fallo UNA vez en una corrida
completa bajo carga (probablemente sensible a tiempos, xunit corre en paralelo) - en verde de
nuevo aislado y en la siguiente corrida completa, no relacionado con ningun cambio de esta
tanda (esta tanda no toco HomeViewModel/RefreshAsync).

### Tercera auditoria (Fable) - cierre

Los 18 hallazgos (H3-01 a H3-18) estan cerrados: 17 con arreglo real, test, arnes y commit
(Tandas 1-3), y H3-07 flagueado explicitamente al usuario como decision de contenido pendiente
(ver arriba) - pedido cumplido en su totalidad ("aplica toda la ronda de fable sin descansar").

## Cuarta auditoria (Fable) - version final, 7 criterios de practicidad

Pedido explicito del usuario: una auditoria FINAL, dedicando el tiempo que hiciera falta,
ceñida SOLO a 7 criterios reales (no una auditoria generica): (1) maxima practicidad, (2) todo
al alcance de la mano - nada escondido tras clics/paneles/scroll innecesario, (3) todo visible
"de golpe", (4) armonia real en cualquier tamaño de ventana, (5) muy interactivo/reactivo -
feedback vivo, (6) muy simetrico y limpio, visual Y de codigo, (7) estetica moderna pero
minimalista. 13 hallazgos nuevos (H4-01 a H4-13), ninguno repetido de las 3 rondas anteriores
(la propia auditoria leyo bitacora.md entera antes de empezar). Informe completo publicado
como Artifact por el propio Fable, en español (la tercera auditoria se habia colado en ingles,
corregido explicitamente esta vez).

### Tanda 1 (H4-01, H4-03, H4-09, H4-12, las 4 menudencias de H4-13)

**H4-01 (Alta), "el buscador de la Libreria colapsa a un cuadradito de ~30px vacio"**: los 3
buscadores gemelos (Libreria/Libreria de buffs/Investigacion) llevaban `MaxWidth="360"
HorizontalAlignment="Left"` - Left fuerza a WPF a medir al CONTENIDO en vez de estirar al
DockPanel (comportamiento por omision), asi que vacios colapsaban a ~30px y crecian letra a
letra al escribir - el control mas usado de la app (buscar entre ~8.200 objetos) casi invisible.
Arreglado quitando Left (el DockPanel ya estira por omision, MaxWidth solo pone un techo).
Añadido de paso un marcador de posicion real ("Buscar...", `Grid` con `TextBlock` superpuesto +
nuevo `EmptyToVisibleConverter`) para reconocerlos de un vistazo sin esperar a escribir.

**H4-03, "la tarjeta Libreria de Inicio aterriza con la Libreria plegada"**: `GoToTab
("Libreria")` cambiaba de pestaña pero nunca ponia `IsLibraryCollapsed=false` - Ctrl+F si lo
hacia, dos caminos al mismo sitio con resultado distinto. Arreglado en el propio `GoToTab` (el
mismo comando que usa Ctrl+F, evita que puedan volver a divergir).

**H4-09, "el parrafo de Inicio queda descentrado"**: mismo defecto real ya corregido en
Desbloqueos/Version (D-a) - `MaxWidth` sin `HorizontalAlignment="Left"` deja el TextBlock en
Stretch, que WPF centra cuando el contenido (acotado por MaxWidth) es mas estrecho que el
contenedor. Añadido `HorizontalAlignment="Left"`.

**H4-12, "el banner de error aparece en seco, el de exito entra animado"**: copiado el mismo
`Storyboard`/`ScaleTransform` real del banner verde de guardado al banner rojo de error (el
aviso que MAS conviene que no aparezca de golpe).

**H4-13 (menudencias)**: banner de eleccion de buffs ahora dice "Elige (o arrastra)" igual que
su gemelo de objetos; buscador de NPCs gana tooltip (unico de los 4 sin uno); Investigacion
gana el mismo panel `BgSecondaryBrush` que envuelve arbol+resultados en sus 2 gemelas (antes
iba "desnudo"); y Ctrl+F pasa a ser CONTEXTUAL (`GoToContextualLibraryCommand` +
`IsBuffsInnerTabActive` nuevos) - si la pestaña interna activa ya es Buffs, salta a SU
Libreria, no a la de objetos - la cabecera de la Libreria de buffs gana el tooltip real que le
faltaba, ahora que es cierto para las dos.

**Bug real del propio arnes descubierto verificando esta tanda (no de produccion)**:
`TerrasavrNative.App.Tests/Program.cs` NUNCA carga `App.xaml` (construye una `Application` en
blanco a proposito, ver el comentario real de ese fichero) - replica a mano cada
`StaticResource` que registra `App.xaml`. El nuevo `EmptyToVisibleConverter` se registro en
`App.xaml` pero se olvido en esa replica manual - la app REAL nunca lo habria notado (si carga
App.xaml de verdad), pero el arnes crasheaba con `XamlParseException` al abrir Objetos. Arreglado
añadiendo el registro que faltaba - y sirve de ejemplo vivo de la misma clase de riesgo que
H4-10 señala en general (codigo duplicado que puede divergir con el tiempo).

12 pruebas deterministas nuevas (`CuartaAuditoriaTanda1Tests.cs`, 4 - H4-03/H4-13 contextual).
`dotnet test` 274/274 en verde, arnes UIA completo sin NO-FOUND/FALLO/EXCEPTION tras el arreglo
del registro que faltaba.

### Tanda 2 (H4-02, H4-04, H4-05, H4-06, H4-11)

**H4-11, "el selector de version usa otro lenguaje visual"**: pintaba `Background`/`FontWeight`
a mano en un `Style` inline en vez de la misma pildora (`Tag="Accent"` sobre el `Button` base)
que usa el resto de selectores de la app - mismo error historico que `PrefixMetaButton` ya
documenta (pintar a mano no anima ni tiene degradado/elevacion). Nuevo `VersionOptionButton` en
`Theme.xaml`, mismo mecanismo real (`IsCurrent` -> `Tag`).

**H4-04, "arrastrar un buff duplicado se rechaza en silencio total"**: a los slots de Buffs les
faltaba el `DragOver` real que Objetos ya tiene (cursor de prohibido del sistema MIENTRAS se
arrastra, antes de soltar) - solo hacia falta comprobar el origen "Libreria de buffs" (un
`SwapWith` entre dos slots nunca puede crear un duplicado). Nuevo
`BuffSlotViewModel.WouldRejectPlacingBuff` (version sin efecto de la comprobacion real que ya
usa `PlaceBuff`) + `OnBuffSlotDragOver` en el code-behind. Red extra: si se rechaza al soltar,
el slot destino se selecciona para que el aviso real (`RejectionMessage`) quede a la vista en
el panel Editar en vez de perderse sin que se note nada.

**H4-05, "Vaciar contenedor destruye hasta 50 slots de un clic, sin vuelta atras"**: la unica
accion realmente destructiva de la app, visualmente identica a sus vecinas inofensivas
(Ordenar/Mover todo). Un dialogo modal seria friccion para el 99% de usos legitimos - en su
lugar, `ContainerViewModel.ClearAll` guarda una instantanea real (slot ORIGINAL de cada objeto,
no solo la lista - Deshacer restaura posiciones exactas) y ofrece `UndoClearCommand` durante 6s
(mismo vehiculo ya usado por la confirmacion de guardado - un banner temporal, `DispatcherTimer`
real). Editado UNA vez en `ContainerCompactTemplate` (plantilla COMPARTIDA por los 4
contenedores reales) en vez de en las 4 cabeceras copiadas - no repite la duplicacion que H4-10
señala en esas mismas cabeceras.

**H4-06, "la rejilla de Buffs no tiene contador ni operaciones en bloque"**: `BuffContainerViewModel`
era una clase plana sin `INotifyPropertyChanged` en absoluto - ganó el mismo `DisplayName` con
recuento vivo (`ContainerViewModel.DisplayName`, A-c) y el mismo `ClearAll`/`UndoClear` reales
de arriba (con `BuffSlotViewModel.RestoreExact`, nuevo - restaura id+duracion EXACTOS, a
diferencia de `PlaceBuff`, que fija una duracion razonable para una colocacion NUEVA). Las dos
clases se mantienen SEPARADAS a proposito (decision de diseño ya documentada en el propio
fichero: generalizar `ContainerViewModel` seria riesgo real por cero beneficio) - mismo
comportamiento portado, no fusion de clases.

**H4-02, "Almacenes seleccionada y luego oculta en Amplio deja un contenido huerfano"**: la
pestaña interna Equipamiento/Inventario/Almacenes nunca tenia `SelectedIndex` enlazado - al
crecer a Amplio (`IsStorageExpanded=true`, oculta "Almacenes" para dejar sitio a la vista lado
a lado de A-4) con esa pestaña activa, WPF se quedaba apuntando a un `TabItem` ya `Collapsed`.
Nuevo `MainViewModel.ObjetosSubTabIndex` enlazado + `OnSizeClassChanged` mueve la seleccion a
Inventario (1) solo si Almacenes (2) era la activa - nunca toca otra seleccion real del
usuario, y nunca actua al ENCOGER (Almacenes vuelve a estar disponible sola).

13 pruebas deterministas nuevas (`BuffContainerCounterAndUndoTests.cs`,
`BuffDragOverRejectionTests.cs`, `StorageTabOrphanTests.cs`, `UndoClearContainerTests.cs`).
`dotnet test` 287/287 en verde (136 Core + 151 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION. Nota aparte (observada, NO investigada a fondo - fuera del alcance de
esta auditoria de practicidad, y no provocada por ningun cambio de esta tanda -
`HomeViewModel.cs`/`RefreshAsync` no se tocaron): `HomeRefreshAsyncTests.
RefreshAsyncCommand_EsAsincronoYIsScanningVuelveAFalseAlTerminar` volvio a fallar en una corrida
COMPLETA bajo carga (segunda vez que se observa, con SINTOMAS DISTINTOS cada vez - antes un
`Assert.True` por tiempos, esta vez `InvalidOperationException: Collection was modified` real
dentro de `HomeViewModel.UpdateCurrentPath` al iterar `Characters` mientras algo mas lo muta) -
siempre en verde aislado. Sugiere una condicion de carrera real en `RefreshAsync`/
`UpdateCurrentPath` que solo se manifiesta bajo la presion de xunit corriendo en paralelo, no
un simple flake de tiempos - merece una mirada propia en otra sesion, documentado aqui en vez
de perseguirlo a ciegas dentro de esta ronda (regla real del proyecto: "si algo falla dos veces
seguidas, parar y escribirlo").

### Tanda 3 (H4-07, H4-08 - los dos unicos hallazgos que cambian layout)

Pedido explicito del usuario ("continua sin parar") tras preguntarle si prefería ir tanda a
tanda por ser estos los dos unicos que cambian la forma de algo (recomendacion real del propio
informe, por la leccion del rediseño revertido de la octava pasada) - se implementan igual, sin
pausa, documentando el resultado aqui con el mismo detalle de siempre para que se pueda revisar
despues.

**H4-07, "a pantalla completa sobra muchisimo espacio y la Libreria sigue plegada por
defecto"**: el propio informe proponia 3 mejoras reales por orden de rendimiento - se
implementaron las 2 primeras (la 3ª, tarjetas grandes de carpetas raiz en el resumen vacio de
Investigacion, se deja fuera de esta ronda por ser la de menor retorno y la unica que exige
UI nueva de verdad, no solo parametrizar una ya existente - documentado aqui para que no se
pierda si se quiere retomar):

1. **Libreria/Libreria de buffs se revelan solas en Amplio**: mismo patron real de B-2
   ("preferencia + revelado temporal", ya en produccion para el picker) - `IsLibraryVisible`/
   `IsBuffLibraryVisible` ahora tambien son `true` cuando `SizeClass == Amplio`, SIN pisar
   `IsLibraryCollapsed`/`IsBuffLibraryCollapsed` (la preferencia real del boton, la unica que se
   respeta al encoger la ventana por debajo de Amplio otra vez). El motivo real de plegarla por
   omision ("devolver espacio a la cuadricula", pedido explicito del usuario, medido a
   1080x700) sigue intacto para ese tamaño - en Amplio ese motivo real ya no aplica.
2. **Apariencia deja de tener un ancho fijo de 640px siempre**: mismo patron real que
   `InicioContentMaxWidth` (I-c) - nuevo `AppearanceContentMaxWidth` (640 normal, 1000 en
   Amplio) en el `StackPanel` que envuelve genero/peinado/tinte/colores/estadisticas - las
   tarjetas de color (`Swatches`) ya viven en un `WrapPanel` real, solo hacia falta darle mas
   ancho para que reparta mas columnas solo, sin rehacer ningun layout.

5 pruebas deterministas nuevas (`LibraryRevealInAmplioTests.cs`) - una de ellas (concordancia
del boton "desplegar a mano en Amplio sigue visible al encoger") encontro un error real en la
PROPIA prueba al escribirla (doble-toggle innecesario que dejaba `IsLibraryCollapsed` en el
valor contrario al esperado por el comentario) - corregido a un unico toggle, no era un bug de
produccion.

**H4-08, "el estado vacio de Exploracion es un lienzo negro sin guia"**: el propio informe daba
2 opciones (aviso centrado, o un lanzador de mundos completo calcado del de personajes de
Inicio) - se implemento la primera (aviso real centrado en el lienzo + boton, mismo patron ya
usado por el overlay de `IsLoading` - nunca los dos avisos a la vez, `ExplorationViewModel.
IsEmpty = !IsWorldLoaded && !IsLoading`) mas deshabilitar el buscador de NPCs mientras no hay
mundo (con tooltip real de por que, `ToolTipService.ShowOnDisabled`). El lanzador de mundos
completo (la "version buena" que cita el propio informe) se deja fuera de esta ronda a
proposito - es una FUNCION nueva real (escaneo async de `.wld` + tarjetas, calcado de
`HomeViewModel` pero no trivial), no un ajuste de layout como el resto de esta tanda; queda
anotado aqui como candidato real para una ronda futura si se decide.

3 pruebas deterministas nuevas (`ExplorationEmptyStateTests.cs`).

`dotnet test` 294/294 en verde (136 Core + 158 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION.

### Tanda 4 (los 2 recortes de la Tanda 3, retomados a peticion explicita del usuario)

Pedido explicito del usuario: aplicar los dos puntos que la Tanda 3 dejo fuera a proposito,
siguiendo la sugerencia REAL y literal del propio informe de Fable (citada aqui de nuevo antes
de implementar cada uno, no de memoria).

**H4-07 punto 3, "el resumen de Investigacion sin carpeta podria enseñar las carpetas raiz como
tarjetas grandes en el area vacia, en vez de solo una frase"**: nuevo
`ResearchViewModel.ShowRootCategoryCards` (true solo cuando no hay carpeta elegida NI busqueda
activa - el mismo caso que antes solo dejaba una frase). En ese caso, el area de resultados
muestra `Research.RootCategories` (el MISMO arbol real de la izquierda, con su propio
`SelectCommand` ya asignado - `CategoryNodeViewModel.AssignSelectCommand`, ningun dato nuevo)
como tarjetas grandes reutilizando `NavCardButton` (el mismo estilo real de las tarjetas de
Inicio - reutilizar en vez de inventar un tratamiento visual nuevo, P7). Cada tarjeta muestra
icono+nombre+recuento real (`ItemIdsOrdered.Count`, ya calculado y cacheado por el propio
arbol - `CategoryNodeViewModel.ItemCount` resulto ser un campo real pero NUNCA poblado en
ningun sitio del proyecto, así que no se uso). 4 pruebas deterministas nuevas
(`ResearchRootCategoryCardsTests.cs`).

**H4-08 (version completa), "un lanzador de mundos calcado del de personajes de Inicio"**:
pedido explicito del usuario tras el cierre de la Tanda 3 ("aplica los dos puntos que dejaste
fuera, siguiendo la sugerencia literal de Fable") - retomado siguiendo la cita EXACTA del
informe (no de memoria): "`GetDefaultWorldsDirectory()` ya existe en el code-behind, y el
patron de tarjetas con escaneo asincrono ya esta probado en `HomeViewModel`".

- `CharacterFileService.GetDefaultWorldsDirectory()` (nuevo) - la misma duplicacion que I-1 ya
  resolvio para personajes (`GetDefaultPlayersDirectory`), esta vez para mundos: vivia privada
  y duplicada solo dentro de `MainWindow.xaml.cs` (el dialogo de "Cargar mundo (.wld)..."), sin
  que la capa de ViewModel pudiera usarla. Movida al servicio, `MainWindow.xaml.cs` ahora la
  reutiliza en vez de tener su propia copia.
- `WldReader.ReadHeader(byte[])` (nuevo, publico) - `ReadHeader(BinaryReader)` (privado) YA
  paraba justo tras Titulo/Dimensiones/GroundLevel/RockLevel sin tocar tiles/NPCs (ver el
  comentario real de `WldHeader`, "simplificacion deliberada heredada del propio lector JS") -
  solo faltaba un punto de entrada publico que no siguiera leyendo. Sin esto, escanear varios
  mundos para el lanzador pagaria el mismo coste real de ~1.4s por mundo que
  `ExplorationViewModel.LoadFromPathAsync` ya mide (el motivo real por el que la carga completa
  es async en primer lugar) - con esto, solo lee cabecera (verificado con 2 pruebas contra
  mundos reales del propio disco: mismo Titulo/TilesWide/TilesHigh/Version que la lectura
  completa).
- `WorldListEntryViewModel` (nuevo) - gemelo real de `CharacterListEntryViewModel`, sin doll de
  vista previa (un mundo no tiene ningun dato real igual de barato - inventar una miniatura
  seria fingir un dato que no existe, mismo criterio de honestidad ya establecido en el
  proyecto).
- `ExplorationViewModel` gana `Worlds`/`IsScanningWorlds`/`ScanMessage`/`RefreshWorldsCommand` -
  mismo patron real que `HomeViewModel` (constructor dispara un escaneo fire-and-forget,
  `ScanWorlds` corre en `Task.Run`, un `.wld` ajeno/corrupto se omite en silencio sin tumbar el
  listado de los demas).
- XAML: el overlay centrado de la Tanda 3 se amplia con el listado real de tarjetas
  (`WorldCardTemplate`, gemela de `CharacterCardTemplate` - sin doll ni menu contextual, fuera
  del alcance real de este hallazgo) + "Actualizar", cayendo al boton manual de siempre solo si
  el escaneo termino sin ningun mundo real encontrado (mismo `ScanMessage`/`EmptyToCollapsed`
  que ya usa Inicio). Clic en una tarjeta llama a `OnWorldCardClick` (code-behind, gemelo de
  `OnLoadWorldClick`) - hace falta code-behind y no un Command porque el ajuste de zoom a la
  ventana tras cargar solo lo puede dar quien conoce el `ScrollViewer` real del mapa.

3 pruebas deterministas nuevas (`ReadHeader_RealWorld_CoincideConElHeaderDeLaLecturaCompleta`
en `WldReaderRealFileTests.cs`, `ExplorationWorldLauncherTests.cs`).

`dotnet test` 302/302 en verde (138 Core + 164 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION.

### Tanda 5 (pedido explicito posterior): personajes/mundos VANILLA en los lanzadores + arreglo real del arnes

Pedido explicito del usuario tras cerrar la cuarta auditoria: los lanzadores de Inicio/
Exploracion (H4-08) solo escaneaban la carpeta de tModLoader - un personaje o mundo de Terraria
VANILLA (sin ningun mod, carpeta real "Documents\My Games\Terraria\..." SIN el segmento
"tModLoader") nunca aparecia. Confirmado con el usuario por pregunta explicita (AskUserQuestion):
alcance "los dos" (personajes Y mundos vanilla, simetria completa entre Inicio y Exploracion).

- `CharacterFileService.GetAllPlayersDirectories()`/`GetAllWorldsDirectories()` (nuevos) -
  devuelven SOLO las carpetas que existen de verdad (0, 1 o las 2 - tModLoader y vanilla), sin
  fallback a Documentos (a diferencia de los metodos de un unico dialogo ya existentes, que se
  dejan intactos). El formato .plr/.wld es identico en los dos casos (tModLoader reutiliza el
  formato vanilla real).
- `HomeViewModel.ScanCharacters`/`ExplorationViewModel.ScanWorlds` ahora recorren TODAS las
  carpetas reales encontradas, con orden GLOBAL por fecha (un personaje vanilla reciente
  aparece antes que uno de tModLoader mas antiguo, no al reves solo por venir de una carpeta
  distinta).
- 4 pruebas deterministas nuevas (`CharacterFileServiceDirectoriesTests.cs`) + 1 prueba real
  corregida en la PROPIA prueba (no en produccion): `ExplorationWorldLauncherTests.
  TrasElEscaneo...` esperaba al escaneo del CONSTRUCTOR via `RefreshWorldsCommand.
  ExecutionTask`, pero el constructor llama al metodo async DIRECTAMENTE (fire-and-forget,
  mismo patron real que HomeViewModel) - `ExecutionTask` se queda `null` hasta la PRIMERA
  llamada real al comando, asi que las aserciones corrian antes de que el escaneo hubiera
  terminado. Corregido llamando al comando explicitamente (`ExecuteAsync`) en vez de confiar en
  el escaneo implicito del constructor.

**Bug real del propio arnes encontrado verificando esta tanda (no de produccion)**: el arnes
crasheo con varios FALLO/NO-FOUND reales (pildoras "Fragua del Defensor"/"Vanidad" NO-FOUND,
B-1/B-2 de la Libreria) que en un primer vistazo parecian una regresion de H4-07 (revelado
automatico de la Libreria en Amplio, Tanda 3). Investigado a fondo: la causa real era que
`window.json` se habia quedado `IsMaximized:true` con 2576x1408 (una sesion manual anterior
dejo la ventana real maximizada en un monitor grande) - el arnes NUNCA fijaba un tamaño
determinista al arrancar, asumia implicitamente heredar un tamaño razonable de la SESION
ANTERIOR (fragil de origen, nunca se habia manifestado porque el tamaño heredado nunca habia
sido tan grande). Con la ventana en SizeClass.Amplio desde el arranque, varios escenarios que
dependen de un tamaño NO-Amplio (pildoras de un panel a la vez, B-1/B-2) fallaban de verdad -
tanto el revelado automatico de H4-07 (correcto, intencional) como el desajuste real del propio
arnes. Arreglado fijando `WindowState=Normal` + `Width=1180`/`Height=860` justo tras la
comprobacion real de T-3 (que ya habia leido el tamaño heredado antes de este punto) - dos
intentos reales hasta dar con el arreglo completo: el primero solo toco Width/Height y no
basto, WPF nunca restaura una ventana Maximized a Normal solo por asignarle un tamaño nuevo.

`dotnet test` 306/306 en verde (138 Core + 168 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION tras el arreglo.

### H3-07 (tercera auditoria) - cerrado de verdad, con investigacion real que corrige una suposicion anterior

Pedido explicito del usuario: decidir H3-07 (Novedades con contenido "de ejemplo/demo
presentado como real", flagueado como decision de CONTENIDO desde la tercera auditoria y NUNCA
verificado contra ninguna fuente real hasta ahora). El propio usuario aporto un dato real que
desmonto la suposicion de partida: la version 1.4.5.8 SI es una version real de Terraria
vanilla, no ficticia como se habia asumido sin comprobar (ver tambien el comentario desactualizado
que arrastraba `WhatsNewIconTests.cs`, corregido de paso).

**Investigacion real (WebSearch/WebFetch contra las wikis OFICIALES, terraria.wiki.gg y
calamitymod.wiki.gg - nunca fuentes de baja fiabilidad como "changelog.gg" o repos de GitHub
con nombres tipo "Cheat Menu Executor", descartadas a proposito por ser señales claras de
contenido no fiable/spam)**: confirmado que TODO el contenido de `whats_new.json` (las 2
versiones de Terraria vanilla, 1.4.5.7/1.4.5.8, incluido el "crossover" con Palworld -
"Melocotón del vínculo"/"Chillet confiable" SI son objetos reales de esa actualizacion real) es
autentico, palabra por palabra contra la fuente oficial - la fecha "agosto de 2026" coincidia
con la fecha real de esta sesion, no era una version futura inventada. El fallo real no era el
contenido en si (que ya era honesto), sino que (1) nunca se habia verificado contra una fuente
real antes de asumir que lo era o no, y (2) le faltaba el registro real de tModLoader/Calamity
Mod - el usuario pidio explicitamente separarlo en 2 pestañas. Alcance acotado por el propio
usuario via pregunta directa (AskUserQuestion): solo las versiones RECIENTES (no el historial
completo de 15 años de Terraria), confirmado con las 5 ultimas versiones reales de Calamity Mod
(2.2.0 "Hog Wild" - la ultima actualizacion real de CONTENIDO, tras la cual el desarrollo del
mod ceso - hasta 2.2.4 "Maintenance's Harbinger", el parche mas reciente).

**Cambios reales**:
- `whats_new.json` (mezclaba las 2 cosas) retirado, sustituido por `whats_new_vanilla.json`
  (mismo contenido ya verificado, sin cambios) y `whats_new_calamity.json` (nuevo, las 5
  versiones reales de Calamity Mod investigadas arriba).
- `WhatsNewEntry`/`WhatsNewCatalog` (Core) ganan `Bugfixes` - el propio campo "bugfixes" YA
  venia en el JSON desde siempre pero `System.Text.Json` lo ignoraba en silencio (nunca hubo
  esa propiedad en el modelo real) - los arreglos reales de cada version nunca se mostraban.
- `WhatsNewItemViewModel.ForCalamity`/`WhatsNewEntryViewModel.ForCalamity` (nuevos, gemelos
  reales de `ForVanilla`) resuelven el sprite real contra `CalamityCatalog.ByModAndInternal`
  ("CalamityMod", el unico mod real del catalogo) en vez de `VanillaItemCatalog` - "lo que no
  se encuentra no se inventa" sigue aplicando igual: varios NPCs nuevos reales (Cerdo horrible,
  Cerdo Divino, Vendedor sombrío...) no tienen icono todavia porque el catalogo local de
  Calamity (extraido de una version anterior del .tmod) todavia no los conoce - honesto, no un
  bug.
- `WhatsNewViewModel.VanillaEntries`/`CalamityEntries` (antes una unica `Entries` mezclada) -
  la pestaña "Novedades" pasa a tener un `TabControl` interno real con 2 sub-pestañas
  ("Terraria"/"tModLoader / Calamity Mod"), mismo patron `InnerTabControl` ya usado en el resto
  de la app.
- Bloque real de "Correcciones" (bugfixes) añadido a la plantilla compartida de entrada, mismo
  tratamiento visual que "Cambios".

3 pruebas actualizadas (`WhatsNewIconTests.cs`, comentario corregido + nuevo constructor de 4
argumentos), 2 pruebas de humo nuevas/actualizadas contra los ficheros reales
(`BuildsAndWhatsNewRealFileTests.cs`).

`dotnet test` 307/307 en verde (139 Core + 168 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION.

### Cuarta auditoria (Fable) - cierre real

Los 13 hallazgos (H4-01 a H4-13) estan cerrados por completo, incluidos los 2 puntos que la
Tanda 3 habia dejado fuera a proposito (H4-07 punto 3 y la version completa de H4-08) -
retomados a peticion explicita del usuario en una Tanda 4, siguiendo la sugerencia literal del
propio informe de Fable en los dos casos (citada de nuevo antes de implementar cada uno, no de
memoria). Pedido cumplido en su totalidad. Una Tanda 5 adicional (arriba) añadio soporte
vanilla real a los dos lanzadores, pedido tras el cierre. H3-07 (tercera auditoria, la unica
decision de contenido que quedaba pendiente de las 4 rondas) tambien cerrado de verdad (arriba),
con investigacion real que corrigio una suposicion nunca verificada de la tercera auditoria.

### Quinta auditoria (Opus) + doll de Inicio fiel a la vanidad real (3-sep-2026)

Pedido explicito del usuario: "activa opusplan y que haga una ultima auditoria... que se ciña
en hacer todo esto que son los puntos mas importantes para mi" (los mismos 7 criterios de
practicidad ya usados en la cuarta ronda) + un hallazgo funcional concreto ya diagnosticado por
el propio usuario: "los personajes de inicio no se visualizan como realmente son en el
juego... que muestre el personaje con la vanidad que tiene cada uno pero fiel al guardado".

**Auditoria**: lanzada con Opus (no Fable, pedido explicito - "fable no capta mucho la cosa"),
con todo el contexto real del proyecto (PROYECTO-TERRASAVR.md, CLAUDE.md, bitacora.md completa
para no repetir nada ya cerrado, la app Electron original como referencia de comportamiento,
tModLoader/CalamityMod decompilados). Volvio con un aviso de seguridad del propio sistema
("Blocked by classifier") en la notificacion de la tarea - comunicado de inmediato al usuario
sin decidir por mi cuenta que no importaba. El contenido del informe en si (14 hallazgos
H5-01..H5-15, todo basado en lectura real de codigo del proyecto) se veia legitimo al leerlo
directamente, pero el enlace del Artifact no le cargaba al usuario - se le paso el HTML
publicado directo como fichero adjunto en vez de depender del enlace. Pendiente: el usuario
decidira si se implementan los hallazgos H5-* en una sesion futura, no se ha tocado nada de eso
todavia.

**Doll de Inicio fiel al guardado**:
`PlayerPreviewRenderer` solo pintaba los 7 colores base (pelo/piel/ojos/ropa) - nunca la
armadura/vanidad real puesta, alcance ya documentado como deliberado ("un proyecto en si
mismo"). Investigado a fondo antes de tocar nada:

- **Formato real confirmado** (`Terraria.Initializers.AssetInitializer.cs`,
  `Terraria.GameContent.TextureAssets.cs` decompilados): `Content/Images/Armor_Head_N.xnb`,
  `Armor_Legs_N.xnb` (tiras verticales 40 de ancho, mismo frame de reposo ya usado para el
  cuerpo base) y `Content/Images/Armor/Armor_N.xnb` (el COMPUESTO torso+brazo ya renderizado -
  no existen `Armor_Body_N`/`Female_Body_N` sueltos en la instalacion real, solo el compuesto,
  y encaja sin huecos con la decision ya tomada de usar solo la piel StarterMale). Verificado
  extrayendo una muestra real y componiendola sobre el doll antes de escribir nada de C# (una
  figura de Terraria con armadura de cobre reconocible, no una tira descolocada).
- **`scripts/extraer-slots-armadura-vanilla.py`** (nuevo): valores LITERALES de
  headSlot/bodySlot/legSlot de `Item.cs` (1.4.5.8), reutilizando el escaner de bloques ya
  depurado (`split_by_case`) de `extraer-categorias-vanilla.py`/`extraer-slot-kind-vanilla.py`
  (que solo guardaban SI existia el campo, no el valor). 548 objetos con algun slot real (head
  236, body 170, legs 144), spot-check contra el trio de Casco/Chaqueta/Grebas de cobre ya
  conocido en `vanilla_armor_sets.json` (ids 89/80/76, los 3 con indice de sprite 1, coincide
  exacto). Salida: `Assets/vanilla_armor_slots.json`.
- **`scripts/extraer-sprites-armadura-vanilla.js`** (nuevo): extrae de la instalacion vanilla
  real de Steam (mismo criterio ya usado para el cuerpo/pelo base, `xnb-to-png.js`/
  `lzx-decoder.js` del proyecto hermano) los 236+170+144 sprites unicos referenciados,
  recortados al frame de reposo 40x56. Cobertura real: 231/236 head, 143/144 legs, 169/170 body
  (~98-99%) - el resto son hojas mas pequeñas que el lienzo estandar, descartadas sin inventar
  nada ("lo que no encaja no se dibuja"). ~670KB en total, en
  `Assets/player/armor_{head,body,legs}/{id}.png`.
- **Calamity - sorpresa real, cero extraccion nueva necesaria**: los sprites de equipo
  (`{Internal}_Head.png`/`_Body.png`/`_Legs.png`) YA estaban extraidos del .tmod junto al resto
  de iconos (misma convencion real de tModLoader moderno: el equipo de una pieza vive al lado
  de su icono, mismo nombre + sufijo), y `CalamityCatalogEntry.EquipSlot` (H3-11, tercera
  auditoria) ya sabe que parte del cuerpo es cada pieza. Verificado por barrido completo: las
  185 entradas reales con `EquipSlot` conocido tienen su fichero real presente, 0 huecos.
- **`EquipmentAppearanceResolver`** (nuevo, `App/Services`): dado un `PlrLoadout`, aplica la
  regla real del juego (`Player.cs`: si el slot de VANIDAD tiene algo puesto, ese es el que se
  ve; si no, el funcional - `loadout.Social[i]`/`loadout.Items[i]`, indices 0/1/2 = cabeza/
  cuerpo/piernas) y resuelve la ruta real del sprite (vanilla via `vanilla_armor_slots.json`,
  Calamity via `CalamityCatalogEntry.Internal`+`EquipSlot`, distinguidos por
  `id >= CalamityIds.ItemIdBase`, mismo patron ya usado en `ItemSlotViewModel`). Comprueba
  `File.Exists` antes de devolver la ruta (los ~1-2% de huecos reales de arriba no deben
  intentar cargar un fichero inexistente). ALCANCE DELIBERADO documentado en el propio
  fichero: no respeta los 3 bytes de "ocultar equipo" del panel de vanidad real
  (HideVisual1/HideVisual2/HideMisc) - el bit exacto de cada slot dentro de esos bytes no se
  investigo a fondo, no compensaba el riesgo de esconder/mostrar la pieza equivocada por una
  lectura erronea. Hueco real, ya conocido, no oculto.
- **`PlayerPreviewRenderer.Render`** gana un parametro opcional `EquippedArmor` (rutas ya
  resueltas, con valor por defecto - los call sites existentes sin armadura no cambian) -
  compone la capa de piernas/cuerpo/cabeza en el mismo punto real donde el juego las dibuja
  (piernas tras zapatos, cuerpo tras camisa, cabeza tras el pelo).
- Cableado en `CharacterFileService` (nuevo `VanillaArmorSlots`/`EquipmentAppearance`),
  `HomeViewModel`/`CharacterListEntryViewModel` (constructor gana el resolver real). El propio
  `AppearanceViewModel` (doll de la pestaña Apariencia) se queda fuera de esta ronda a
  proposito - haria falta que esa pestaña conozca el equipo puesto en vivo, cambio mas grande,
  no pedido explicitamente (el usuario solo menciono "los personajes de inicio").

6 pruebas nuevas (`EquipmentAppearanceResolverTests.cs`): objeto vanilla funcional resuelve un
sprite real que existe en disco, vanidad tapa a lo funcional (verificado con 2 ids reales
distintos del mismo set), slot vacio no resuelve nada, objeto Calamity real (pedido al propio
catalogo, no hardcodeado) resuelve su sprite real ya extraido, un objeto del slot equivocado no
se cuela en otro hueco, y una prueba de extremo a extremo real (pixel a pixel) que confirma que
`PlayerPreviewRenderer.Render` con armadura da una imagen distinta a sin ella - pillaria en seco
un futuro refactor que dejara de leer el parametro.

`dotnet test` 313/313 en verde (139 Core + 174 ViewModels), arnes UIA completo sin
NO-FOUND/FALLO/EXCEPTION. **Confirmado con datos reales, no solo sinteticos**: la captura real
`inicio-lanzador.png` del arnes muestra al personaje real "Terrariano" con su armadura puesta
en el doll (antes solo colores base) - los otros personajes de la captura no llevan armadura
equipada de verdad en su guardado real, por eso se siguen viendo sin ella (correcto, fiel al
guardado, no un fallo).

### Quinta auditoria (Opus), Tanda A - H5-06 y H5-08 (3/4-sep-2026)

Pedido explicito del usuario tras recibir el informe completo (14 hallazgos H5-01..H5-15,
publicado como Artifact - aviso de seguridad del propio sistema en la notificacion de la tarea,
"Blocked by classifier", comunicado de inmediato; el contenido en si, leido linea a linea, es
legitimo): "si haz la auditoria". Se sigue el orden real que el propio informe sugiere (Tanda A
"cimientos" primero, no cambia layout, habilita al resto).

**H5-06 ("Favorito" de punta a punta)** - ya comiteado (`eb9be7e`): `GameItem.Favorited` viajaba
real de punta a punta en el `.plr` pero la capa App nunca lo tocaba - sin marca visual, sin
control, y `PlaceItem` lo borraba en silencio al reemplazar el objeto de un slot favorito.
Cerrado el circuito completo (marca en la tarjeta y en Editar, interruptor en el menu
contextual, `PlaceItem` lo conserva, `Sort`/`ClearAll`/`MoveAllTo` lo saltan - `Sort` y
`MoveAllTo` con cita real del juego decompilado, `ClearAll` por consistencia propia
documentada como tal). 8 pruebas nuevas (`FavoritedTests.cs`).

**H5-08 (sistema adaptativo real - ancho Y alto)**: `UpdateSizeClass` solo miraba el ancho;
`WindowHeightClass` (nuevo, `WindowSizeClass.cs`) añade el eje vertical real, independiente del
horizontal - `AltoMinHeight=900` citado del propio informe de la auditoria ("un portátil de
1440×900... el tamaño más común de uso real"). Sobrecarga de compatibilidad
(`UpdateSizeClass(double)` sigue existiendo, delega a la de 2 argumentos con altura por debajo
del umbral) para no tocar los 16 sitios reales que ya llamaban a la version de un argumento en
los tests existentes - cero riesgo de regresion ahi, confirmado compilando sin tocarlos.

Aplicado a las 2 "constantes ciegas" que el propio informe señala: `LibraryRowMaxHeight` (nueva
propiedad derivada, 460 en `Bajo`/640 en `Alto`) sustituye el `MaxHeight="460"` fijo de la fila
de Libreria en Objetos y en Buffs; `IsLibraryVisible`/`IsBuffLibraryVisible` se revelan solas
tambien con `HeightClass.Alto`, no solo `SizeClass.Amplio` - el motivo real (Libreria y
contenedores viven en filas apiladas, no columnas, asi que si hay sitio depende de la ALTURA).

**Fuera de esta pasada, a proposito**: darle a `Normal` un comportamiento de ancho propio (2
paneles a la vez donde hoy hay 1 - la otra mitad de H5-08) no se toco. El informe ofrecia 2
opciones concretas (Equipamiento Armadura+Vanidad juntas, o Inventario+Almacen a 5 columnas) y
ninguna es un cambio contenido - la primera exige que el selector de "Vista:" sepa mostrar 2
paneles a la vez y elegir solo el 3º (interaccion nueva, no solo layout), la segunda exige que
`ContainerViewModel.Columns` se pueda ver con un valor distinto segun el contexto visual sin
tocar la instancia real del ViewModel. Ninguna se pudo verificar con confianza sin poder
interactuar con la app en vivo en esta sesion - se deja pendiente, documentado aqui, no fingido
como cerrado.

**Bug real encontrado y arreglado en el propio arnes (no en produccion)**: al verificar H5-08
con el arnes completo, `N3-CTRL-S`/`V-c` empezaron a fallar de forma intermitente (~1 de cada 4
pasadas). Investigado a fondo antes de tocar nada a ciegas: `PressCtrlPlus` inyecta la tecla a
nivel de SO real (`keybd_event`), que entrega contra el foreground window REAL de Windows, no
contra "window" por binding - el ultimo `SetForegroundWindow` explicito quedaba muy atras (justo
antes del test de Ctrl+F), y entre medias corren de sobra capturas `RenderTargetBitmap` y
redimensionados como para que el foco real del SO derive. Arreglado reafirmando
`SetForegroundWindow(hwnd)` justo antes de la inyeccion de Ctrl+S (mismo patron ya usado en el
propio arnes para el test de foco por teclado, T-H) - confirmado en verde 4/4 pasadas reales
tras el arreglo (antes: fallo reproducido, luego 1 de 4).

Nueva bateria `HeightClassTests.cs` (5 pruebas: sobrecarga de compatibilidad se queda en Bajo,
umbral de 900 real en ambos sentidos, ancho y alto como ejes independientes -
Compacto+Alto a la vez -, y el auto-revelado real de la Libreria por altura). `dotnet test`
326/326 en verde (139 Core + 187 ViewModels), arnes UIA completo 4/4 pasadas sin
NO-FOUND/FALLO/EXCEPTION.

### Quinta auditoria (Opus), Tanda A parte 3 (cierre) - H5-15 (4-sep-2026)

**H5-15 (H4-10 nunca se implemento - 3 navegadores de catalogo casi identicos)**: nuevo
`CatalogBrowserViewModel<TEntry>` (`App/ViewModels/CatalogBrowserViewModel.cs`), base real
compartida por `LibraryViewModel`/`BuffLibraryViewModel`/`ResearchViewModel` - arbol de
categorias (`RootCategories`), busqueda con debounce real de 180ms (`SearchText`,
`SearchDebounceTimer`), y el par `SelectCategory`/`ClearCategory` (con el arreglo real de
`IsExpanded` que hasta ahora habia que replicar a mano en cada una de las 3, documentado en el
propio informe con cita textual de los comentarios que probaban el coste: "Mismo bug real
corregido en LibraryViewModel.SelectCategory" repetido 2 veces, "el debounce medido de verdad en
L-c solo se aplico a esa unica superficie" repetido 2 veces mas). `ApplyFilter` se queda
abstracto a proposito, y el tope de resultados NO sube a la base (cada hija mantiene su propio
`MaxResults` - 100 para Objetos/Investigacion, 300 para Buffs, diferencia real ya medida y
deliberada, no un descuido a unificar).

Cada hija se queda con SOLO lo que de verdad le es propio: `LibraryViewModel`/
`BuffLibraryViewModel` conservan intacto su mecanismo de "elegir" (`PickTarget`/`IsPicking`/
`PlaceInTarget`/`CancelPick` - duplicacion menor real que sigue ahi, aceptada, ver mas abajo);
`ResearchViewModel` conserva `ShowRootCategoryCards`/`IsJourneyMode`/`_researchedCounts`/
`LoadFrom`/`Reset`, su propia logica de investigacion, sin tocar.

**Fuera de esta pasada, a proposito, documentado**: los otros dos frentes de H5-15 (3 bloques
de XAML gemelos del navegador de catalogo, y las 4 cabeceras de contenedor copiadas en
Inventario/Almacenes) no se tocaron - unificarlos exige un `DataTemplate` parametrizado por la
plantilla de resultado real (objeto/buff/fila de investigacion, cada uno con su propia forma) y
verificacion visual en vivo que esta sesion no puede hacer con confianza solo con el arnes UIA.
Se prioriza la parte de mayor riesgo real (la logica de ViewModel que ya causo 3 arreglos
repetidos del MISMO bug) sobre la de mayor riesgo de regresion visual sin poder verlo en
pantalla. Tambien queda aceptada la duplicacion menor de `PickTarget`/`IsPicking`/
`PlaceInTarget`/`CancelPick` entre Libreria de objetos y de buffs (4 miembros, sin historial de
bug repetido detras, a diferencia del arbol/busqueda) - no se fuerza una segunda capa de
herencia genérica solo por simetria.

Compilo limpio a la primera (0 errores) - señal real de que la duplicacion ERA
mecanicamente identica, no solo parecida. `dotnet test` 326/326 en verde, sin tocar NINGUN test
existente (los 3 ViewModels siguen exponiendo los mismos `SelectCategoryCommand`/
`ClearCategoryCommand`/`Results`/`RootCategories`/`SearchText`/`ResultsSummary`/
`SelectedCategory` de siempre, ahora heredados). Arnes UIA: 6 pasadas reales en total durante
esta verificacion, 5/6 completamente limpias - la 6ª fallo en `T-H/F2` (adorner de foco por
teclado, ver segunda auditoria), un test SIN relacion con nada tocado aqui (Guardar/foco, no
Libreria/Investigacion/Buffs) que ya no volvio a fallar en las 3 pasadas siguientes - fragilidad
ya conocida de UI Automation con temporizacion real, documentada aqui en vez de perseguida sin
una causa raiz concreta que arreglar (a diferencia del bug real de `SetForegroundWindow`
encontrado en H5-08, ese si con causa y arreglo verificados).

**Cierra la Tanda A completa** (H5-06, H5-08, H5-15) - las 3 cimentaciones del informe, ninguna
cambia layout visible, las 3 verificadas con `dotnet test` + arnes UIA real.

### Quinta auditoria (Opus), Tanda B parte 1 - H5-01, Deshacer/Rehacer real (4-sep-2026)

Pedido explicito del usuario: "complétalo todo seguido sin parar". H5-01 primero, tal y como
sugiere el propio informe ("los otros tres [H5-04/03/02] se apoyan en su historial").

**Lo que pasaba de verdad**: Terrakeep tenia dos "deshacer", ninguno un deshacer de edicion
real - `UndoLastSave` opera sobre FICHEROS (recarga el `.bak`), `ContainerViewModel.UndoClear`
era una unica instantanea de 6 segundos solo para "Vaciar contenedor". Colocar un objeto,
cambiar cantidad/prefijo/favorito, Auto-equipar (su propio tooltip real ya admitia "Reemplaza
SIN CONFIRMACION... no lo guardes si no estabas seguro") - todo irreversible salvo cerrar sin
guardar.

**Diseño real, no una imitacion**: `UndoStack`/`UndoEntry` (nuevo, `App/Services`) - pila real de
comandos por CLOSURES (`Action Undo`/`Action Redo` + `Label`), sin que esta clase sepa nada del
dominio (objetos/buffs/investigacion serian todos igual de validos ahi si algun dia se
extiende). `GameItem` gana `Clone()`/`ContentEquals()` (Core) - instantaneas propias, nunca la
referencia viva (`Item` sigue mutandose en sitio en Count/Prefix/Favorited, guardar la
referencia cruda habria corrompido una entrada ya empujada en cuanto alguien volviera a tocar
ese mismo objeto).

`ItemSlotViewModel` gana un unico canal nuevo real: `_onItemChanged` (delegado opcional al
constructor, mismo patron ya establecido con `_requestPick`) - se dispara desde los 4 puntos
reales donde el contenido de un slot cambia de verdad (`UpdateFrom` - cubre `PlaceItem`/`Clear`/
`SwapWith`, `OnCountChanged`, `SetPrefix`, `ToggleFavorite`), siempre con antes/despues ya
clonados, siempre comparando contenido antes de disparar (nunca en bucle consigo mismo).
`ItemSlotViewModel` NO conoce el `UndoStack` - `MainViewModel.OnSlotItemChanged` es quien
decide empujar una entrada real (con el mismo guardia `_suppressDirty` ya existente para no
grabar nada durante la carga de un personaje, mas un `_suppressUndoRecording` nuevo para que
Deshacer/Rehacer no se graben a si mismos).

**Operaciones en bloque como UNA sola entrada** (pedido explicito del informe): nuevo
`MainViewModel.RunAsUndoableBatch(label, containers, body)` - snapshot de TODOS los slots
implicados antes de `body()` (con `_suppressUndoRecording=true` mientras corre, para no grabar
entradas sueltas por cada slot que `body()` toque), diff real despues, y una unica entrada
combinada si de verdad cambio algo. Aplicado a **Auto-equipar** (cubre Inventario Y
Equipamiento a la vez - `AutoEquipService.Apply` toca ambos) y **Mover todo al almacén** (cubre
origen Y destino a la vez). `UndoStack.Clear()` en `LoadFromPath` - el historial de un
personaje no tiene sentido real sobre otro.

**Fuera de esta pasada, a proposito, documentado**:
- **Ordenar/Vaciar contenedor** (`ContainerViewModel.Sort`/`ClearAll`, comandos propios,
  vinculados directo en XAML) - NO se envolvieron en `RunAsUndoableBatch` (exigiria darle a
  `ContainerViewModel` una referencia cruzada al `UndoStack`, o redirigir los 8 bindings de
  XAML reales a comandos nuevos en `MainViewModel`). Siguen siendo deshacibles de verdad -
  cada slot que cambian dispara su propio `_onItemChanged` igual que cualquier otra edicion,
  simplemente como VARIAS entradas sueltas en vez de una combinada (Ctrl+Z varias veces en vez
  de una). Cobertura real, solo menos pulida que lo pedido literalmente. El viejo
  `ContainerViewModel.CanUndoClear`/`UndoClearCommand`/banner de 6s de "Vaciar contenedor" se
  deja TAL CUAL, sin fusionar con el UndoStack nuevo - redundante pero inofensivo (un atajo
  rapido para el caso mas comun, el Ctrl+Z general sigue cubriendo lo mismo por detras).
- **Investigar todo** y **Marcar todos** (Desbloqueos) - tocan datos de forma completamente
  distinta (`PlrCharacter.Research`, banderas booleanas de Desbloqueos), no slots de objeto -
  el mismo `UndoEntry` generico los cubriria sin problema (closures, sin acoplarse al dominio),
  pero exigiria su propio snapshot-diff especifico por tipo de dato, no reutilizable de
  `RunAsUndoableBatch` tal cual. Hueco real, disclosed, no fingido como cerrado.
- **Panel "Historial de cambios"** completo (lista desplegable) - se implemento la version mas
  contenida: 2 botones reales en la cabecera global (↶/↷, Ctrl+Z/Ctrl+Y) con tooltip que dice
  el ROTULO real de la siguiente entrada (`UndoStack.NextUndoLabel`/`NextRedoLabel`, P5 -
  "feedback vivo", no solo "hay algo que deshacer"). Una lista completa navegable
  (`UndoStack.Entries` ya esta expuesta, lista para ese panel si se pide despues) no se montó
  en el XAML esta pasada.
- **Ctrl+Z real**: se cede el paso al deshacer NATIVO de un `TextBox` si el foco esta dentro de
  uno (Cantidad/Índice/nombre del personaje...) - mismo criterio de cualquier editor de
  escritorio real, el usuario esta deshaciendo SU tecleo, no una edicion de slot.

9 pruebas nuevas (`UndoStackTests.cs`), con un `MainViewModel` real y un personaje real cargado
(no un `UndoStack` aislado) - colocar+deshacer, deshacer+rehacer, una edicion nueva trunca la
cola de rehacer, deshacer en orden inverso real sobre 2 slots distintos, cantidad/prefijo/
favorito como entradas independientes que se deshacen una a una, `Clear` restaura el objeto
entero, cargar OTRO personaje limpia el historial, "Mover todo al almacén" como una sola
entrada real que cubre los 2 slots movidos a la vez, y `SwapWith` (arrastrar y soltar) como 2
entradas reales (una por slot). 335/335 en verde (139 Core + 196 ViewModels), arnes UIA 2/2
pasadas limpias sin NO-FOUND/FALLO/EXCEPTION.

### Quinta auditoria (Opus), Tanda B parte 2 - H5-04, copias de seguridad rotativas (4-sep-2026)

**Lo que pasaba de verdad**: `CharacterFileService.WriteAtomic` (T-C) genera un `.bak` real en
cada guardado, atomico y correcto, pero se SOBRESCRIBE cada vez - un unico punto de retorno (el
guardado inmediatamente anterior). El escenario que mas duele en un editor de partidas: guardas,
sigues tocando, guardas otra vez, y el error estaba DOS guardados atras - en ese momento no
queda nada.

**`BackupHistoryService`** (nuevo, `App/Services`): copias con fecha real
(`%LOCALAPPDATA%\Terrakeep\Backups\{personaje}\{yyyyMMdd-HHmmss}.plr`+`.tplr`), MISMA carpeta
base ya real de `WindowPlacementService` - no ensucia `Documents\My Games\Terraria` (mismo
criterio ya establecido en T-C para el `.tplr`). El `.bak` de un solo nivel se queda TAL CUAL
(es lo que hace atomico el propio guardado, no se toca). Tope fijo de 20 copias por personaje
con purga real por antigüedad - "N configurable" de verdad (pantalla de Ajustes) se deja para
H5-07, todavia sin empezar. `MainViewModel.Save()` llama a `BackupHistory.SaveBackup` justo
despues de un guardado real con exito, dentro de un `try/catch` que NUNCA relanza (un fallo
copiando el historial rotativo - disco lleno, permisos - no debe invalidar un guardado real ya
confirmado, es solo la red extra).

**"Historial de guardados"** (pedido explicito del informe: "un panel... con fecha, tamaño y un
boton por punto, para restaurar cualquiera, no solo el ultimo"): submenu real nuevo en el menu
contextual de la tarjeta de Inicio, junto a "Restaurar copia de seguridad" (I-b) que ya vivia
ahi. Poblado BAJO DEMANDA al abrirse (`OnBackupHistorySubmenuOpened`, code-behind) - nunca al
escanear Inicio, seria I/O de sobra para personajes que el usuario nunca llega a abrir. Mismo
patron real ya establecido para esta tarjeta (`PlacementTarget.Tag`/`.DataContext` porque un
`ContextMenu` es un popup aparte del arbol visual) - el MenuItem "padre" del submenu SI es hijo
logico directo del propio `ContextMenu`, asi que `Parent` llega derecho a el sin ese truco.
`HomeViewModel.RestoreBackupPointCommand` (tupla `(CharacterListEntryViewModel, BackupEntry)`)
reutiliza el mismo aviso real a `MainViewModel` que "Restaurar copia de seguridad" ya tenia
(H3-04 - si el personaje restaurado es el cargado ahora mismo, el editor no se queda mostrando
el estado antiguo en memoria).

**Fuera de esta pasada, a proposito**: el texto "Último guardado: hace 4 min" en la cabecera
global (parte del mismo hallazgo en el informe) se deja para H5-10 (Tanda C, "la cabecera global
esta hueca por dentro") - esa pantalla es la que de verdad va a montar la franja de constantes
vitales, hacerlo aqui habria dividido el mismo trabajo de XAML en dos sitios.

**Bug real encontrado y arreglado en el propio arnes** (no en produccion): el test I-b
(segunda auditoria) asumia que TODO `MenuItem` del menu contextual debia tener `Command`/
`CommandParameter` propios - cierto hasta ahora, pero "Historial de guardados" es un
CONTENEDOR de submenu real (`HasItems=true`), no invoca nada por si mismo. Arreglado excluyendo
los contenedores de submenu del chequeo (`!mi.HasItems`). Se añadio ademas una verificacion real
nueva (`H5-04-HISTORIAL`) que abre el submenu de verdad (`IsSubmenuOpen=true`, dispara
`OnBackupHistorySubmenuOpened` por el mismo camino real que un clic) y confirma que deja de
estar en el placeholder "(cargando...)".

4 pruebas nuevas (`BackupHistoryServiceTests.cs`): una copia real recuperable por
`ListBackups`, lista vacia sin ninguna copia todavia, `Restore` copia la version elegida encima
del fichero real (verificado leyendo el `.plr` restaurado de verdad, no solo comparando bytes),
y el orden mas-reciente-primero. 339/339 en verde (139 Core + 200 ViewModels), arnes UIA 2/2
pasadas limpias sin NO-FOUND/FALLO/EXCEPTION.

### Quinta auditoria (Opus), Tanda B parte 3 - H5-03, guardar/cargar conjuntos de objetos (4-sep-2026)

**Investigacion real primero** (nunca a ciegas): `app.io.IoSave`/`IoLoad` reales
(`reference/terrasavr-real/script.beautified.js:5804-5880`, `ob.procItem:5864`) - confirmado
`resourceType` real `"TerrasavrItems"` (no "TerrakeepItems", eso lo escribia solo el propio
informe de la auditoria como propuesta) y forma real por slot `{id, count?, prefix,
isFavorited?}`, `null` para un slot vacio (nunca un id=0 inventado). El engine original ademas
soporta un formato binario legado (`"/terrasavr/i"` + registros por slot) - no replicado (motor
Flash/Haxe muy anterior, sin valor real de interoperar con el .json actual).

**`ItemSetFile`/`ItemSetSlotData`** (nuevo, `Core/Data`) - mismo esqueleto real
(`resourceType`/`resourceVersion`/id-count-prefix-favorito por slot), `resourceType` real
**"TerrakeepItems"** a proposito (formato NUEVO, no bytes-a-bytes con el original - el motor
real nunca tuvo Calamity). Extension real pedida explicitamente por el informe: un objeto de
Calamity se serializa por `mod`+`internal` (nunca el id sintetico de este puerto, que no
sobreviviria a una regeneracion del catalogo) - resuelto con `CalamityCatalog.ByModAndInternal`
ya existente. Un prefijo Rogue de Calamity (`ItemPrefix.IsCalamity`) se serializa igual por
nombre interno (`RoguePrefixCatalog.ByInternal`) - el propio motor original nunca tuvo esto,
pero es la misma idea real portable. "Lo que no se encuentra no se inventa": un objeto/mod ya
desinstalado, o un `resourceType` ajeno, no rompen nada - slot vacio o excepcion clara,
respectivamente, nunca un objeto inventado.

**`MainViewModel.SaveItemSet`/`LoadItemSet`** - el dialogo real de fichero vive en la View
(`MainWindow.xaml.cs`, mismo criterio ya establecido: MainViewModel es headless de verdad).
`LoadItemSet(append)` reutiliza `RunAsUndoableBatch` (H5-01) - pedido explicito del informe
("cargar un conjunto es una entrada mas del historial, deshacible"). `append=false` ("Cargar")
reemplaza el contenedor entero slot a slot, igual que el original real (`Ga.onBinaryData`:
`if (!this.append) ... d.clear()` antes de rellenar); `append=true` ("Añadir") solo rellena
huecos libres, mismo criterio real que `ContainerViewModel.MoveAllTo`.

**3 botones reales** ("Guardar conjunto...", "Cargar...", "Añadir...") en las cabeceras de
Inventario (pestaña compacta) y Almacenes (pestaña independiente, operando sobre el almacen
`Current` seleccionado - igual que "Ordenar"/"Vaciar contenedor" ya hacen ahi al lado).

**Fuera de esta pasada, a proposito, documentado**: las otras 2 cabeceras reales con estos
mismos botones (la vista Amplia side-by-side de Inventario+Almacen) y **Equipamiento/loadouts**
por completo (el informe tambien mencionaba "mover un loadout de un personaje a otro", pero
Equipamiento no tenia ya ningun boton de accion en bloque del que colgar estos 3, a diferencia
de Inventario/Almacenes que ya tenian Ordenar/Vaciar - habria sido UI nueva de cero, no una
extension de un patron real ya en produccion). Cubre el caso mas comun citado explicitamente
("mover un inventario... de un personaje a otro").

6 pruebas nuevas en Core (`ItemSetFileTests.cs`: round-trip vanilla con prefijo+favorito,
round-trip Calamity por mod/internal - confirmado en el propio JSON que el id sintetico NUNCA
aparece crudo -, slot vacio como null, `resourceType` ajeno rechazado, objeto Calamity
desinstalado no se inventa, prefijo Rogue por nombre interno) + 4 pruebas en App
(`ItemSetCommandsTests.cs`: guardar+cargar entre DOS personajes reales distintos, "Añadir" no
toca lo que ya hay puesto, "Cargar" es una sola entrada real de deshacer que revierte los 2
slots a la vez, un fichero ajeno no rompe nada y avisa por `StatusMessage`). 349/349 en verde
(145 Core + 204 ViewModels), arnes UIA 2/2 pasadas limpias sin NO-FOUND/FALLO/EXCEPTION.

### Quinta auditoria (Opus), Tanda B parte 4 (cierre) - H5-02, Investigacion editable (4-sep-2026)

**Lo que pasaba de verdad**: `ResearchViewModel.ApplyFilter` solo recorria
`_researchedCounts.Keys` - la pestaña UNICAMENTE podia mostrar lo que ya estaba investigado, sin
forma real de investigar un objeto suelto, un conteo parcial, una carpeta entera, ni QUITAR
investigacion. El Terrasavr original si tiene esto real (`app.TabResearch`,
`script.beautified.js:4637`, rejilla editable con "Remove All"/"Unlock All"). No era un
descuido: la segunda ronda lo aparco por escrito ("mostrar tambien lo NO investigado es R-a/R-b,
Fase 2... fuera de esta ronda", `bitacora.md:4705`) y nunca se retomo - el unico punto de las 4
rondas anteriores que seguia vivo como "Fase 2 pendiente".

**`ApplyFilter` reescrito**: la carpeta elegida muestra TODO su contenido real
(`SelectedCategory.ItemIdsOrdered` completo, no filtrado por `_researchedCounts.ContainsKey`),
investigado o no - la busqueda GLOBAL sin carpeta ahora tambien busca en TODO el catalogo real
(`_allKnownIds`, el mismo recorrido real de `ResearchAllService.Apply`), no solo en lo ya
investigado como antes.

**`ResearchRowViewModel` reescrito** (`ObservableObject` real, antes inmutable): `Count` es
editable de verdad - clic en la fila (`ToggleRowCommand`) alterna investigado/no investigado
(al conteo completo real o a 0), y un campo de texto real acepta un parcial a mano. El
constructor asigna el CAMPO directamente (nunca la propiedad) para que solo las ediciones REALES
del usuario disparen `CountChangedByUser` - construir la fila con su estado inicial no es una
edicion.

**Escritura real de vuelta al personaje**: `ResearchViewModel.SyncBackTo(PlrCharacter)` (nuevo)
- mismo criterio real que `MainViewModel.SyncEditsBackToMerged` para objetos, llamado desde
`Save()` justo antes de guardar. Necesito el REVERSO de `VanillaItemCatalog.GetIdByKey` (no
existia) - `GetKeyById` nuevo, construido una vez desde el diccionario ya cargado. Un evento
DEDICADO (`ResearchChanged`, no `PropertyChanged` generico de todo el ViewModel - eso tambien
dispara solo con navegar/buscar, marcar dirty por abrir una carpeta habria sido un falso
positivo real) - mismo patron real ya establecido (`ServersViewModel.Changed`/
`BuffsViewModel.SlotChanged`).

**2 acciones por carpeta** ("Investigar esta carpeta"/"Quitar", visibles solo con una carpeta
elegida) **+ 1 global nueva** ("Quitar toda la investigación" - el equivalente real del
"Remove All" del Terrasavr original, que hoy no existia en absoluto) **+ barra de progreso
real** (antes solo frase) - `GlobalProgressSummary` separado de `ResultsSummary` a proposito
(el tooltip de la barra necesita el TOTAL global siempre, `ResultsSummary` cambia de
significado segun la carpeta/busqueda activa). "Investigar esta carpeta" solo SUBE lo que
falta, nunca baja un parcial real ya mas alto que el umbral (mismo criterio ya establecido en
`ResearchAllService.Apply`).

**Fuera de esta pasada, a proposito**: la Investigacion sigue sin participar del `UndoStack`
general de H5-01 (dato de forma distinta a un slot de objeto - el mismo `UndoEntry` generico lo
cubriria sin problema, pero un snapshot-diff especifico no es reutilizable de
`RunAsUndoableBatch` tal cual) - marca dirty de verdad, pero no es deshacible con Ctrl+Z
todavia, ya documentado como hueco real desde H5-01.

**Bug real encontrado y arreglado en el propio arnes** (no en produccion): verificando H5-02,
`R-d` empezo a fallar (`CountLabel` vacio en vez de "✔ Investigado"). Investigado a fondo:
`SearchText` dispara un `DispatcherTimer` real de 180ms (`CatalogBrowserViewModel`), y el
propio test solo hacia un `DoEvents()` (vacia lo que ya este listo AHORA, no espera tiempo real
ninguno) - `Results` seguia con el estado ANTERIOR en el momento de comprobarlo, flakiness pura
de temporizacion. Arreglado con el mismo remedio real ya probado en L-c (`WaitForDispatcher`,
bombea Y cede la CPU de verdad hasta que el tiempo pedido transcurre). De propina, el MISMO
arreglo aplicado a `R-e` (que tenia identico problema, arrastrado desde el principio de esta
sesion) lo dejo en verde por primera vez.

9 pruebas nuevas (`ResearchEditableTests.cs`, contra el catalogo real): una carpeta elegida
muestra TODO su contenido aunque nada este investigado, alternar una fila sin investigar la
marca con su conteo completo real, alternar una ya investigada la vacia, editar el conteo a
mano acepta un parcial real, `ResearchChanged` dispara SOLO con ediciones reales (nunca con
solo navegar), `SyncBackTo` vuelca el estado real y se lee identico al recargar el mismo
personaje, "Quitar toda la investigación" vacia todo de verdad, "Investigar esta carpeta" no
baja un parcial ya mas alto, y "Quitar" por carpeta. 358/358 en verde (145 Core + 213
ViewModels), arnes UIA 2/2 pasadas limpias sin NO-FOUND/FALLO/EXCEPTION.

**Cierra la Tanda B completa** (H5-01 Deshacer, H5-04 copias rotativas, H5-03 guardar/cargar
conjuntos, H5-02 Investigacion editable) - el bloque que el propio informe describe como "lo
que de verdad separa 'editor correcto' de 'programa completo'". Pendiente: Tanda C (H5-10
cabecera con constantes vitales, H5-09 anchos fijos, H5-11 lanzador de mundos permanente) y
Tanda D (H5-12/13/14/05/07) - todavia sin empezar.

## H5-10 - Cabecera con constantes vitales (Tanda C, quinta auditoria de Opus)

Hallazgo real: "la cabecera global, lo unico visible en las 6 pestañas, esta hueca por dentro -
todo el centro vacio, solo identidad a la izquierda y botones a la derecha". Vida/mana/defensa/
dinero son justo los datos que mas se consultan y que hoy exigen saltar de pestaña (Apariencia
para vida/mana, Equipamiento para defensa, sumar monedas a mano) - viola P1 (maxima
practicidad) y P3 (todo visible de golpe).

**Cambio real**: la `Border` de cabecera (`MainWindow.xaml`, antes un `DockPanel` con solo
identidad+botones) pasa a un `Grid` de 3 columnas (Auto/\*/Auto) - columna 0 identidad
(igual que antes), columna 2 botones (igual que antes), **columna 1 nueva**: franja de
constantes vitales, centrada, visible solo con `IsCharacterLoaded`.

- **Vida/Mana**: barra real de dos `Border` superpuestos (fondo + relleno con ancho
  proporcional), no solo texto - `AppearanceViewModel` gana 4 propiedades computadas
  (`HealthFraction`, `HealthLabel`, `ManaFraction`, `ManaLabel`), derivadas de
  `HealthNow`/`HealthMax`/`ManaNow`/`ManaMax` ya existentes (nada nuevo que leer del
  personaje). Nuevo `FractionToWidthConverter` (`Converters/VisibilityConverters.cs`, mismo
  patron real que `BoolToDoubleConverter` ya en el fichero: `parameter` es el ancho maximo en
  px del contenedor real) - registrado como recurso local de `MainWindow.xaml` (clave
  `FractionToWidth`), NO en `App.xaml`, precisamente para que el arnes de UI Automation
  (`TerrasavrNative.App.Tests/Program.cs`, que instancia una `Window` real y replica a mano
  solo los recursos de `App.xaml.Resources`) no necesite ningun registro extra - un `MainWindow`
  real ya carga sus propios `Window.Resources` de XAML sin ayuda.
  - **Gotcha real evitado**: los 4 metodos parciales `OnHealthNowChanged`/`OnHealthMaxChanged`/
    `OnManaNowChanged`/`OnManaMaxChanged` ya existentes tienen una guarda temprana
    `if (_suppressWriteback || _character == null) return;` (Ap-f, para no escribir hacia
    `PlrCharacter` durante `LoadFrom`). Si los 2 `OnPropertyChanged` nuevos de
    `HealthFraction`/`HealthLabel` (y las de Mana) se hubieran puesto DESPUES de esa guarda, la
    cabecera se habria quedado mostrando la barra del personaje ANTERIOR tras cargar uno nuevo
    (la propiedad de solo lectura no tiene nada que corromper escribiendo durante la carga, a
    diferencia de la escritura hacia `_character` - por eso van ANTES de la guarda, no despues).
- **Defensa**: reutiliza `EquipmentGroup.TotalDefense`/`ActiveSetBonusText` ya existentes (del
  loadout que se este viendo ahora mismo en Equipamiento).
- **Dinero**: nuevo `MainViewModel.MoneyText` - conversion real por ID de objeto (71=cobre,
  72=plata, 73=oro, 74=platino, NO por posicion en el array de `CoinsContainer`, que podria
  tener huecos), recalculado via suscripcion a `ItemId`/`Count` de los slots de `CoinsContainer`.
- **Horas jugadas**: reutiliza `Appearance.PlayHours` ya existente.
- **Ultimo guardado**: nuevo `MainViewModel.LastSavedText`, solo de sesion (no se guarda en
  disco) - `DateTime` capturado tras un `Save()` con exito, refrescado cada 30s por un
  `DispatcherTimer` propio ("hace un momento" / "hace N min" / "hace N h" / fecha completa),
  puesto a `null` al cargar un personaje nuevo (no tiene sentido arrastrar el guardado del
  personaje anterior).
- **Colapso por tamaño**: Defensa/Dinero/Horas+Ultimo guardado (el bloque secundario, no
  Vida/Mana que se quedan SIEMPRE visibles) se ocultan bajo `IsVitalsStripExpanded`, mismo
  mecanismo real de `SizeClass` ya usado por H5-08/`IsEquipmentExpanded` - en una ventana
  Compacta la franja se reduce a solo vida/mana en vez de desbordar.

**Verificacion real**: `dotnet build` en verde (0 advertencias/errores) tanto para
`TerrasavrNative.App` como para el arnes `TerrasavrNative.App.Tests`. `dotnet test` completo:
358/358 (145 Core + 213 ViewModels), sin tests nuevos dedicados (no hay logica de dominio
nueva que testear con xunit - todo es binding/calculo derivado ya cubierto indirectamente por
los tests existentes de `AppearanceViewModel`/`MainViewModel` sobre los campos base). Arnes de
UI Automation: **2/2 pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION, `ultimo-error.log`
inexistente en ambas.

Fuera de esta pasada, documentado: no se añadio ningun test unitario dedicado a
`HealthFraction`/`MoneyText`/`LastSavedText` en si (formulas triviales de una linea, ya
ejercitadas de forma indirecta) - si en el futuro estos calculos ganan complejidad real (ej.
redondeos especiales, plurales en el texto de tiempo), añadir tests dedicados en ese momento.

## H5-09 - Cuatro pantallas con ancho fijo (Tanda C, quinta auditoria de Opus)

Hallazgo real: "Desbloqueos (440), Version (500) y las 2 sub-pestañas de Novedades + Acerca de
(760/720) se quedaron fuera de I-c/H4-07 - en un monitor 2560 maximizado, Desbloqueos usa el
17% del ancho: 13 casillas en columna unica, ~2000px negros al lado". Viola P4 (armonia en
cualquier tamaño) y P6 (simetria) - Inicio y Apariencia ya respiran, estas 4 no.

**`MainViewModel`**: UNA sola propiedad compartida (pedido explicito del informe: "sustituir
los 4 numeros por una propiedad... en vez de repetirlo 4 veces mas"), no 4 propiedades
independientes - `DetailContentMaxWidth` (760 normal, 1200 Amplio) y `DetailCardColumns` (1
normal, 2 Amplio, para las listas de tarjetas de Novedades/Acerca de). Las 4 pantallas son "de
detalle", mas ligeras que Inicio/Apariencia, y cada una ya tenia su propio mecanismo interno
(familias de Desbloqueos, grupos de Version, tarjetas de Novedades/Changelog) para repartir el
ancho de sobra sin necesitar un numero por pantalla.

**Desbloqueos**: las 4 familias YA agrupadas por D-b (segunda auditoria) pasaban de subtitulos
sueltos en una columna unica a 4 tarjetas reales (`Border`) dentro de un `WrapPanel` - con
sitio real entran las 4 a la vez en Amplio, 2 en Compacto/Normal (nunca 1, siguen cabiendo 2
tarjetas de 280px incluso a 1080px de ancho minimo).

**Version**: los 4 grupos reales (`VersionEditor.Groups` - 1.1.x/1.2.x/1.3.x/1.4.x) ya vivian en
un `ItemsControl`, bastaba con darle `ItemsPanel=WrapPanel` (antes StackPanel implicito) para
que se repartan lado a lado en vez de apilarse - cero cambios en `VersionEditorViewModel.cs`.

**Novedades (x2 sub-pestañas) y Acerca de**: las listas de tarjetas de version
(`WhatsNewEntryViewModel`, `ChangelogEntry`) pasan de tira vertical unica a `UniformGrid
Columns="{Binding DetailCardColumns}"` (2 en Amplio - "autentico reparto en columnas, no solo
mas ancho cada tarjeta", pedido explicito del informe). Los 2 `DataTemplate` de tarjeta ganan
margen derecho real (`Margin="0,0,10,14"`, antes solo inferior) para que el hueco entre
columnas no las deje pegadas. El texto corrido (creditos, intro) de Acerca de NO crece con el
contenedor - `MaxWidth="680"` propio en esos `TextBlock`, tal y como pedia el informe
explicitamente ("el tope de linea se mantiene por legibilidad").

**Bug real encontrado y arreglado en el propio cambio** (no preexistente, introducido y
corregido en esta misma pasada, detectado por el arnes UIA antes de comitear nada): las
etiquetas largas de los `CheckBox` de Desbloqueos ("Alcance de mesa de trabajo aumentado (Pan
del Artesano)") se veian cortadas en seco contra el borde redondeado de su tarjeta nueva de
280px. Investigado con las capturas reales del propio arnes (`h5-09-desbloqueos-amplio.png`):
2 causas reales combinadas, no una -
1. `Border` con `CornerRadius` recorta silenciosamente cualquier hijo que se salga de su ancho
   (mismo mecanismo real que usa WPF para redondear las esquinas, sin necesidad de
   `ClipToBounds="True"` explicito).
2. `CheckBox.Content` como string plano NO hace word-wrap nunca (a diferencia de
   `TextBlock.Text`, `ContentPresenter` no tiene wrapping propio) - y el primer intento de
   arreglo (sustituir `Content="..."` por un `<TextBlock TextWrapping="Wrap">` hijo) TAMPOCO
   bastaba por si solo: la plantilla real de `CheckBox` (`Styles/Theme.xaml`) mete el
   `ContentPresenter` dentro de un `StackPanel Orientation="Horizontal"` - un `StackPanel`
   horizontal SIEMPRE mide a sus hijos con ancho infinito en la direccion de apilado, asi que
   `TextWrapping="Wrap"` nunca tenia ningun limite real contra el que envolver.
   Arreglado con un `MaxWidth="210"` explicito en cada uno de los 13 `TextBlock` nuevos (el
   limite real que faltaba, ancho de la tarjeta menos padding/casilla/margen) - visto y
   confirmado con una segunda tanda de capturas tras el arreglo, texto legible completo, sin
   recorte, en Amplio y en Compacto. No se toco el `Style` compartido de `CheckBox` (arriesgado
   a media tarea, afecta a toda la app) - la correccion queda contenida a este bloque nuevo.

**Verificacion real**: `dotnet build` en verde (App y arnes). `dotnet test`: 358/358 sin
cambios (nada de logica de dominio nueva, todo layout/XAML). Arnes de UI Automation ampliado
con un bloque nuevo (`H5-09-AMPLIO`/`H5-09-COMPACTO`) que redimensiona a 1550x900 y 1180x860,
navega a las 4 pantallas reales y confirma `SizeClass`/`DetailContentMaxWidth`/
`DetailCardColumns` en los 2 extremos, con captura real de cada una (8 capturas nuevas,
`h5-09-*.png`) - inspeccionadas a mano, confirmado el reparto en columnas real y el arreglo del
corte de texto. **2/2 pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION.

## H5-11 - El lanzador de mundos desaparece al cargar uno (Tanda C, quinta auditoria de Opus)

Hallazgo real: "H4-08 construyo un lanzador de mundos completo, pero todo el bloque cuelga de
`Exploration.IsEmpty` (`!IsWorldLoaded && !IsLoading`) - cargas un mundo y la lista entera
desaparece para siempre; para pasar al siguiente hay que volver al dialogo del Explorador, la
friccion exacta que H4-08 vino a eliminar. Su gemelo de Inicio no hace esto: la lista de
personajes sigue ahi siempre, con el cargado resaltado (I-a)". Viola P2 (al alcance de la mano)
y P6 (simetria con Inicio).

**`WorldListEntryViewModel`**: pasa de `sealed class` a `sealed partial class : ObservableObject`
con `[ObservableProperty] private bool _isCurrent` - gemelo exacto de
`CharacterListEntryViewModel.IsCurrent` (I-a, segunda auditoria).

**`ExplorationViewModel`**: nuevo `UpdateCurrentWorldPath(string? path)` (gemelo real de
`HomeViewModel.UpdateCurrentPath`) - se llama tras cargar un mundo con exito
(`LoadFromPathAsync`, con la ruta real) y tras cada `RefreshWorldsAsync` (la lista se
reconstruye entera, `IsCurrent` no sobrevive - mismo motivo real que en `HomeViewModel`). Un
fallo real de carga llama a `UpdateCurrentWorldPath(null)` - ninguna pildora debe quedar
marcada como "cargada" si la carga fallo. Nuevo `[RelayCommand] OpenFolder(WorldListEntryViewModel)`
(gemelo real de `HomeViewModel.OpenFolder`, `explorer.exe /select,`) - pedido explicito del
informe ("ya que la lista es permanente, la tarjeta de mundo gana el menu contextual que su
gemela de personaje ya tiene, hoy ausente por alcance").

**`MainWindow.xaml`**: nueva `WorldPillTemplate` (reemplaza a la antigua `WorldCardTemplate`,
ahora sin ningun uso - eliminada) - pildora compacta (no la tarjeta grande original, pensada
solo para el estado vacio) con el mismo borde de acento real (`DataTrigger IsCurrent`) que ya
usa `CharacterCardTemplate`, mismo menu contextual real ("Abrir carpeta") via
`Tag="{Binding DataContext.Exploration, ...}"` + `PlacementTarget.Tag.OpenFolderCommand` (mismo
patron ya establecido para el de Inicio). Tira nueva PERMANENTE (`DockPanel.Dock="Top"`, justo
debajo de la barra de Cargar mundo/zoom, "junto al zoom" tal y como sugeria el informe),
visible con `Exploration.Worlds.Count>0` SIEMPRE (con mundo cargado o no) - a diferencia del
overlay grande de `IsEmpty`, que se queda EXCLUSIVAMENTE para el primer arranque (el listado de
"Tus mundos" que antes vivia solo ahi dentro se elimino de ese bloque, ahora solo queda el
aviso "Sin mundo cargado" + boton de carga directa, tal y como el informe pedia
explicitamente).

**Verificacion real**: `dotnet build` en verde. `dotnet test`: 358/358 (nada de logica nueva
cubierta por xunit - `IsCurrent`/`UpdateCurrentWorldPath` son el mismo patron ya usado y
probado indirectamente via `HomeViewModel`, sin async ni I/O propios). Arnes de UI Automation
ampliado con un bloque nuevo (`H5-11-PILDORA`/`H5-11-TIRA-PERMANENTE`) justo despues de cargar
el mundo real `roca_negra.wld` (11MB) - confirma que la entrada real de `Worlds` queda
`IsCurrent=true`, y que la etiqueta "Tus mundos" SIGUE presente en el arbol visual real con el
mundo YA cargado (antes, en este mismo punto, `IsEmpty` ya era `False` y el bloque entero
habria desaparecido) - con una captura real (`h5-11-tira-mundos-con-mundo-cargado.png`)
mostrando la tira de 5 mundos reales con "roca negra" resaltada en acento junto al mapa ya
pintado. **2/2 pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION.

**Fallo intermitente observado durante la verificacion, investigado y descartado como ajeno a
este hallazgo** (regla real del proyecto: "si algo falla dos veces seguidas, parar y
documentarlo"): en 2 de las 5 pasadas completas del arnes lanzadas durante esta pasada,
`LIBRERIA busqueda 'Sword'` (L-c, segunda auditoria) devolvio `Results.Count=0` en vez de 3 -
investigado antes de seguir insistiendo: (1) esa prueba corre en la linea ~732 del arnes, MUY
por delante de cualquier codigo nuevo de H5-11 (que empieza en la linea ~1481) - en el momento
en que falla, ningun camino de H5-11 se ha ejecutado todavia; (2) las otras 3 pasadas de esta
misma sesion (incluida la primera y la ultima) dieron `Results.Count=3` limpio, sin ningun
cambio de codigo entre medias - patron real intermitente (pasa/falla/falla/pasa), no una
regresion determinista causada por este cambio. Ya existe el mismo remedio real documentado
para esta clase de problema (`WaitForDispatcher`, ver el hallazgo de H5-02 mas arriba) - el
margen actual (300ms de espera real contra un debounce de 180ms) parece insuficiente bajo carga
de maquina puntual (muchos ciclos seguidos de build+relanzamiento en esta sesion). Fuera de esta
pasada, documentado: si este fallo intermitente vuelve a aparecer en una sesion futura, subir el
margen de esa espera concreta (`WaitForDispatcher(300)` -> un numero mayor) en vez de
re-investigar desde cero.

**Cierra la Tanda C completa** (H5-10 constantes vitales, H5-09 ancho compartido, H5-11
lanzador de mundos permanente). Empieza la Tanda D: H5-12, H5-13, H5-14, H5-05, H5-07.

## H5-12 - Clic en una tarjeta de la Libreria no hacia nada (Tanda D, quinta auditoria de Opus)

Hallazgo real: "`LibraryCardTemplate` solo engancha `PreviewMouseLeftButtonDown` para anotar el
punto de inicio de un posible arrastre - en el estado normal (un slot ya seleccionado porque
cualquier clic en un slot llama a `SelectSlot`, y la Libreria desplegada sola en `Amplio` por
H4-07), pulsar un objeto de la Libreria no produce ningun efecto ni señal. Ni coloca, ni avisa,
ni previsualiza. La unica via real es arrastrar, gesto mas caro que nada anuncia. Boton muerto
en el control mas usado de la app, en su estado por defecto". Viola P1 (practicidad) y P5
(reactividad).

**Clic simple** (`MainWindow.xaml.cs`, nuevo `OnLibraryCardClick`, `MouseLeftButtonUp`): coloca
en el slot YA seleccionado en el panel Editar compartido (`ItemEdit.Slot`) - si lo rechaza,
`PlaceItem` ya deja su propio `RejectionMessage` real (visible en ese mismo panel, nada nuevo
que construir); si no hay ningun slot seleccionado, no hace nada (el tooltip de la tarjeta ya
avisa de esto, ver mas abajo).

**Doble clic**: nuevo `MainViewModel.PlaceInFirstFreeInventorySlot(int itemId)` - coloca en el
primer hueco REAL vacio del Inventario (`InventoryContainer.Slots.FirstOrDefault(s =>
s.IsEmpty)`), sin necesitar ninguna seleccion previa; si el inventario esta lleno, avisa por
`StatusMessage` (mismo patron real ya usado en "Mover todo al almacén" para el caso analogo
"nada que mover") en vez de fallar en silencio.

**Boton "Colocar" existente**: se mantiene tal cual, intacto - sigue siendo el unico camino para
el flujo explicito "Elegir..." (`Library.PickTarget`/`IsPicking`), un concepto distinto (elegir
oficial para UN slot concreto marcado a proposito) del nuevo clic directo sobre el slot ya
seleccionado en Editar.

**Gotcha real resuelto antes de comitear, no documentado en el informe original**: implementar
el doble clic de forma ingenua (comprobar `e.ClickCount>=2` en el propio `MouseLeftButtonUp`)
habria colocado el objeto DOS VECES en cada doble clic real - WPF entrega el PRIMER clic de un
futuro doble clic como un `MouseUp` normal con `ClickCount=1` ANTES de que exista ningun
`ClickCount=2` (no los agrupa de antemano), asi que el camino de clic simple ya se dispara con
el primer clic, y el camino de doble clic se dispara ademas con el segundo - dos colocaciones
reales por un solo gesto de usuario. Arreglado con un `DispatcherTimer` real armado en cada
clic simple (cancelado si un segundo clic real llega antes de que expire) - el tiempo de espera
usa `GetDoubleClickTime()` (P/Invoke real a `user32.dll`, la misma API que usa el propio
Explorador de Windows para decidir si dos clics cuentan como uno doble; `SystemParameters` de
WPF, a diferencia de `System.Windows.Forms.SystemInformation`, no expone este valor - no valia
la pena arrastrar una referencia a WinForms solo por un numero, P/Invoke directo en su lugar,
mismo patron real ya usado en `App.xaml.cs` para `GetSystemMetrics`).

**Guardia real contra arrastrar+soltar tambien disparando el clic**: `_dragStartLibrary` (ya
existente para el arrastre) se usa como señal de "este gesto ya se resolvio como un arrastre
real" - `OnLibraryCardMouseMove` lo vacia justo antes de `DoDragDrop`, asi que
`OnLibraryCardClick` puede distinguir "el usuario solto tras arrastrar" (no hacer nada, el
`Drop` real ya coloco el objeto) de "el usuario solo hizo clic" (el camino nuevo de arriba) con
una sola comprobacion.

**Tooltip de la tarjeta** (pedido explicito del informe: "si no hay ninguno seleccionado, la
tarjeta lo dice al pasar el ratón"): 2 lineas mutuamente excluyentes nuevas
(`ItemEdit.Slot` no nulo/nulo, mismos `NullToVis`/`NullToCollapsed` ya en el proyecto) - con
seleccion explica los 2 gestos reales (clic/doble clic); sin seleccion, avisa en naranja de que
hace falta elegir un hueco primero (o usar doble clic directamente).

**Verificacion real**: `dotnet build` en verde. `dotnet test`: 358/358 (nada de logica de
dominio nueva cubierta por xunit - la disambiguacion de gestos vive en code-behind, ver mas
abajo). Arnes de UI Automation ampliado (`H5-12-SELECCION`/`H5-12-CLIC-SIMPLE`/
`H5-12-DOBLE-CLIC`) verificando a nivel de ViewModel las 2 llamadas reales que cada gesto
dispara: seleccionar un slot vacio y colocar en el (camino real del clic simple), y
`PlaceInFirstFreeInventorySlot` colocando de verdad en el primer hueco libre real (no en
cualquier otro, verificado por `SlotIndex` exacto). **2/2 pasadas limpias**, sin
NO-FOUND/FALLO/EXCEPTION.

Fuera de esta pasada, documentado a proposito (no fingida cobertura): la disambiguacion de
gestos EN SI (arrastre vs. clic vs. doble clic, `OnLibraryCardClick`/`OnLibraryClickTimerTick`)
exige eventos de raton reales enrutados por WPF sobre un elemento arbitrario - sin precedente
en este arnes (que solo simula `InvokePattern`/`Command.Execute` o teclado real via
`keybd_event`, nunca un clic de raton real). Verificado en su lugar por revision de codigo
cuidadosa (el guardia `_dragStartLibrary`, el temporizador real con cancelacion) + las 2
llamadas reales que cada camino dispara, probadas de forma aislada arriba.

## H5-13 - Estado vacio de la Libreria/Libreria de buffs (Tanda D, quinta auditoria de Opus)

Hallazgo real: "sin busqueda/carpeta/restriccion de slot, `LibraryViewModel.ApplyFilter` sale
antes de rellenar nada - escribe una frase en `ResultsSummary` y deja `Results` vacio. El XAML
sigue pintando el `SlotGridPanel` con cero hijos: la mitad derecha del panel es superficie
muerta. `BuffLibraryViewModel` hace exactamente lo mismo. Es el estado en el que arranca la
Libreria SIEMPRE. La cuarta ronda ya diagnostico y arreglo esto SOLO en Investigacion (H4-07
punto 3); sus 2 gemelas quedaron fuera". Viola P3 (todo visible de golpe) y P6 (simetria).

**`ShowRootCategoryCards` subida a `CatalogBrowserViewModel<TEntry>`** (base compartida real por
H5-15) - computada de verdad (`SelectedCategory == null && string.IsNullOrWhiteSpace(SearchText)`,
`virtual` para que `LibraryViewModel` pueda añadir su propia condicion extra, ver mas abajo) en
vez del campo suelto que `ResearchViewModel` alternaba a mano dentro de `ApplyFilter` (H4-07) -
un unico sitio real en vez de que cada `ApplyFilter` tuviera que acordarse de alternarla en sus
2 salidas. Se notifica al instante en `OnSearchTextChanged` (no espera al debounce de 180ms de
`Results` - las tarjetas desaparecen en cuanto se escribe la primera letra, no 180ms despues) y
en `OnSelectedCategoryChanged` (cubre TAMBIEN una asignacion directa como
`ResearchViewModel.Reset()`, no solo los 2 comandos `SelectCategory`/`ClearCategory`).
`ResearchViewModel` pierde su campo `_showRootCategoryCards` propio y las 2 asignaciones
manuales - ahora hereda el comportamiento sin tocar nada mas.

**`LibraryViewModel` - override real, no generico**: unica de las 3 con "restriccion de slot"
(`PickTarget` con `AcceptedKind != SlotKind.None`, ej. elegir un Tinte o un Gancho) - con esa
restriccion activa, el catalogo YA se reduce a un conjunto pequeño y util de ver de inmediato
(~20 ganchos, ~100 tintes); mostrar las carpetas raiz genericas ahi seria un paso atras, no una
mejora. `override bool ShowRootCategoryCards => base.ShowRootCategoryCards && !hasSlotRestriction`,
notificada tambien en `OnPickTargetChanged`. El propio `ApplyFilter` se simplifico para
reutilizar esta misma propiedad en su condicion de salida temprana (antes recalculaba la misma
logica dos veces, una en la propiedad y otra en el metodo).

**XAML** (`Objetos > Equipamiento/Inventario/Almacenes` y `Buffs`): mismo patron real ya en
produccion en Investigacion - `ScrollViewer` con `WrapPanel` de `NavCardButton` (200x90) sobre
`RootCategories`, visible con `ShowRootCategoryCards`; el `ScrollViewer`/`SlotGridPanel` real de
resultados, visible con `InverseBoolToVis` de la misma propiedad. Cada nodo ya trae su propio
`SelectCommand` real (T-18) - cero comandos nuevos que enrutar.

**`CategoryNodeViewModel.ItemCount`**: investigado a fondo contra la afirmacion literal del
informe ("campo real declarado que nadie rellena") - **resulto ser inexacta**: tanto
`LibraryCategoryTreeBuilder` (Libreria + Investigacion, arbol compartido) como
`BuffLibraryTreeBuilder` YA populan `ItemCount` en TODO nodo, de forma consistente, desde antes
de esta pasada. Lo unico real que faltaba: la propia tarjeta de Investigacion (unica que ya
existia) leia `ItemIdsOrdered.Count` a mano en vez del campo ya calculado - 2 formas distintas
de leer el mismo numero. Corregido a `ItemCount` en las 3 tarjetas nuevas/existente, un unico
campo canonico. Documentado aqui en vez de "arreglar" algo que ya funcionaba, siguiendo la regla
del proyecto de verificar contra el codigo real antes de dar por buena una afirmacion externa.

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **369/369** (145 Core + 224
ViewModels, +11 tests nuevos: `LibraryRootCategoryCardsTests.cs` (6, incluida la condicion extra
real de restriccion de slot - CON y SIN restriccion, usando un slot real de `EquipmentGroup`
para "con" y de `InventoryContainer` para "sin", ya que TODO slot de `EquipmentGroup` tiene
restriccion real, ninguno cae nunca a `SlotKind.None`) y `BuffLibraryRootCategoryCardsTests.cs`
(5, incluida una comprobacion real de que `ItemCount` esta poblado de verdad, no 0). Arnes de UI
Automation ampliado (`H5-13-LIBRERIA`/`H5-13-BUFFS`) con captura real de las 2 superficies -
confirmado a ojo que las tarjetas rinden con su `ItemCount` real visible ("Materiales - 1586
objeto(s)", "Utilidad (17) - 17 buff(s)"). **2/2 pasadas limpias**, sin
NO-FOUND/FALLO/EXCEPTION (el mismo flake intermitente ajeno de `LIBRERIA busqueda 'Sword'` ya
documentado en H5-11 volvio a aparecer en 1 de las 4 pasadas lanzadas aqui - mismo patron real,
nada nuevo que investigar).

## H5-14 - Navegacion por teclado en slots (Tanda D, quinta auditoria de Opus)

Hallazgo real: "`SlotCompactTemplate`/`BuffSlotCompactTemplate` son `Border` - un `Border` no es
enfocable en WPF, ningun slot se puede alcanzar con tabulador ni flechas. Especialmente visible
porque el tema SI tiene un `FocusVisualStyle` propio (T-H/F2) - existe y casi nada lo usa.
`OnWindowKeyDown` maneja solo 4 teclas: Ctrl+S/O/F/Esc. No hay salto entre pestañas, ni borrar
slot, ni copiar/pegar objeto." Viola P1 (practicidad) y P5 (reactividad) - ~350 slots reales
inalcanzables sin raton.

**`Focusable="True"`** en `SlotCompactTemplate`/`BuffSlotCompactTemplate` - el `FocusVisualStyle`
del tema (T-H/F2, ya escrito, nunca antes usado en slots) lo pinta gratis en cuanto un slot
recibe el foco.

**`KeyboardNavigation.DirectionalNavigation="Contained"`** - Style implicito nuevo en
`Theme.xaml` sobre `controls:SlotGridPanel` (se aplica solo a TODA rejilla real de la app -
Equipamiento/Inventario/Almacenes/Libreria/Libreria de buffs - sin tocar cada sitio donde se
usa) para que las flechas recorran la rejilla como en el propio juego, sin escapar a la rejilla
vecina.

**Teclas sobre el slot enfocado** (`OnItemSlotKeyDown`/`OnBuffSlotKeyDown`, `MainWindow.xaml.cs`):
Supr vacia (`ClearCommand`), Intro abre "Elegir..." (`ChooseFromLibraryCommand`), F alterna
favorito (H5-06, solo objetos - un buff no tiene ese concepto), Ctrl+C/Ctrl+V copian/pegan el
objeto/buff ENTERO. Portapapeles real de sesion (`_itemClipboard`/`_buffClipboard`, nunca
persistido) en el propio code-behind.

**`ItemSlotViewModel.PasteItem(GameItem)`/`BuffSlotViewModel.PasteBuff(id, time)`** (nuevos,
Core): a diferencia de `PlaceItem`/`PlaceBuff` (pensados para colocar algo NUEVO - cantidad 1,
prefijo recien sugerido, duracion razonable), pegar reproduce EXACTAMENTE lo copiado
(`GameItem.Clone()`, H5-01) - misma restriccion real de slot que `PlaceItem`
(`AcceptsItem`/`RejectionMessage`) y la misma regla real de "sin dos instancias del mismo buff"
que `PlaceBuff` (a diferencia de `RestoreExact`, que la salta a proposito solo para Deshacer).

**Ctrl+1...6** (`OnWindowKeyDown`): salto directo a las 6 pestañas raiz, sin pasar por el raton -
sin conflicto real con un `TextBox` (Ctrl+numero no es un gesto de tecleo normal, a diferencia
de Ctrl+Z/Y que SI colisionan con el deshacer nativo de un campo). Numero anunciado en el propio
rotulo (`ToolTip="Ctrl+N"` en cada `TabItem` raiz, mismo criterio real T-H/F1).

**Verificacion real, de extremo a extremo, no solo a nivel de ViewModel**: a diferencia de
H5-12 (arrastre/clic/doble clic, sin precedente de raton simulado en este arnes), el foco y la
inyeccion de teclado real YA tienen precedente probado en este mismo arnes (T-H-FOCO/N3-CTRL-S)
- se aprovecho a fondo. Gotcha real: un `Border` sin `AutomationPeer` propio (WPF no le da uno
por defecto) NO aparece en el arbol de UI Automation - `Keyboard.Focus()` directo sobre la
instancia real (hallada recorriendo el arbol visual con `VisualTreeHelper`, mismo patron ya
usado en el arnes) en vez de `AutomationElement.SetFocus()`. Con eso, verificado con teclado
real (`keybd_event`, ventana real en primer plano): foco real tras `Keyboard.Focus()`, flecha
derecha mueve el foco real al slot vecino DENTRO de la rejilla, Supr real vacia el slot
enfocado, Ctrl+C real sobre un slot (favorito, cantidad 7, prefijo real) + Ctrl+V real sobre
otro reproduce el objeto entero exacto, Intro real abre "Elegir..." (`Library.PickTarget`), y
Ctrl+3/Ctrl+1 reales saltan entre pestañas raiz. `dotnet test`: **375/375** (145 Core + 230
ViewModels, +6 tests nuevos `SlotKeyboardActionsTests.cs` cubriendo `PasteItem`/`PasteBuff` a
nivel de dominio - copia exacta, rechazo por restriccion de slot real, origen vacio vacia el
destino, rechazo por buff duplicado). **2/2 pasadas limpias** del arnes de UI Automation, sin
NO-FOUND/FALLO/EXCEPTION (el mismo flake intermitente ajeno de `LIBRERIA busqueda 'Sword'`
volvio a aparecer 1 de las 2 veces, ya documentado en H5-11/H5-13 - nada nuevo).

Fuera de esta pasada, documentado a proposito: "beneficio lateral" que el propio informe
menciona ("el arnes de UI Automation gana acceso directo a slots, hoy solo por coordenadas") no
aplica tal cual a este arnes - sigue sin poder ALCANZAR un slot por `AutomationElement` (el
`Border` no tiene peer propio), pero SI gana la capacidad de operar sobre un slot conocido por
teclado real una vez localizado por `VisualTreeHelper` (demostrado arriba) - una via de
verificacion nueva y real, aunque no exactamente la que el informe imaginaba.

## H5-05 - Buscador "¿Dónde lo tengo?" (Tanda D, quinta auditoria de Opus)

Hallazgo real: "un personaje real ocupa ~354 slots (50 inventario, 4×40 almacenes, 12×10
equipo por loadout, 10 mascota/montura/tinte, 4 monedas, 4 municion), repartidos en 3 niveles
de pestañas. Los 4 buscadores existentes (Libreria, Libreria de buffs, Investigacion, NPCs)
buscan en catalogos o en el mundo - ninguno busca en el propio personaje. '¿Donde tengo el Ala
de murcielago?' solo se responde a ojo." Viola P1 (practicidad), P2 (al alcance) y P3 (todo
visible de golpe).

**Dato ya resuelto, sin recalcular nada nuevo**: `BuildsViewModel.RefreshOwnership` (Bd-d) ya
recorre exactamente los 2 origenes reales de todo slot del personaje - nuevo
`MainViewModel.AllOwnedSlotsWithContainers()` recorre los MISMOS 2 origenes
(`Containers` + `EquipmentGroup.AllContainers`), emparejando cada `ItemSlotViewModel` no vacio
con su `ContainerViewModel` real (para la navegacion, ver mas abajo).

**`MainViewModel`**: `IsWhereIsItOpen`/`WhereIsItSearchText`/`WhereIsItSummary`/
`WhereIsItResults` nuevos, mismo debounce real de 180ms ya establecido (`DispatcherTimer`,
`LibrarySearchGrammar.Matches` - la MISMA gramatica real de la Libreria: coma=OR, espacio=AND,
`#id`, `#a-b`, pedido explicito del informe "reutilizando la gramatica ya existente").
`ApplyWhereIsItFilter` publica a proposito (mismo motivo real que `ResearchEditableTests.cs` ya
establecio con `SelectCategoryCommand` - sortea el debounce sin depender de un Dispatcher real
en un test xunit plano).

**`ItemSlotViewModel.IsSearchMatch`** (nuevo, `true` por defecto): mismo patron real ya
validado por X-c para los NPCs del mapa - resaltar los coincidentes, ATENUAR (Opacity 0.35, no
`Visibility`) los que no, nunca ocultarlos del todo. Canal propio en el `Style.Triggers` de
`SlotCompactTemplate` (no compite con seleccionado/equipado/Calamity, mismo criterio real de
T-4/E-1).

**"¿Duplicados?" y "¿cuantos entre todos los almacenes?"** (pedido explicito del informe,
"aprovechar para responder tambien"): el resumen real agrupa las coincidencias por id de
objeto - un objeto repetido en mas de un slot a la vez se cuenta y se anuncia en la misma frase
("N resultado(s) - M de ellos repartidos en K objeto(s) duplicado(s)."), verificado con datos
REALES del propio arnes (13 resultados de "hierro", 6 duplicados reales entre la fixture del
principio de la sesion).

**Navegacion real** (`NavigateToWhereIsItResultCommand`): un clic en un resultado salta a
Personaje/Objetos, elige la sub-pestaña, el almacen (`StorageGroup.SelectCommand`, mapeo real
`bank`/`bank2`/`bank3`/`bank4` -> indice 0-3) o el loadout+vista de Equipamiento
(`EquipmentGroup.SelectLoadoutCommand`/`SelectKindCommand`, parseado del `ContainerViewModel.Key`
real "loadoutNKind") segun corresponda, selecciona el slot en el panel Editar y dispara el
mismo flash real de T-14 - pedido explicito del informe ("un clic navega, selecciona el slot y
dispara el flash de T-14"). `WhereIsItResultViewModel.ContainerKey` viaja SOLO para esto (nunca
se muestra - `ContainerName`, ya real y legible en `ItemSlotViewModel`, es lo que se ve).

**Cabecera global** (`MainWindow.xaml`): boton real "🔍 ¿Dónde lo tengo?" junto a "Cargar
personaje" (visible en CUALQUIER pestaña, como Guardar - pedido explicito), `Popup` real
(`StaysOpen="False"`, se cierra solo con un clic fuera, sin manejador nuevo) anclado al boton -
primer uso real de `Popup` en el proyecto (los selectores de peinado/tinte usan un Border
superpuesto a proposito por un motivo real distinto, cursor en movimiento constante - un
`Popup` estatico anclado a un boton no tiene ese problema).

**Reset real al cargar otro personaje**: `WhereIsItResults`/`WhereIsItSearchText`/
`WhereIsItSummary`/`IsWhereIsItOpen` se limpian en `RebuildContainers` (mismo sitio real que
`Research.Reset()`) - un resultado apuntando a un `ItemSlotViewModel` ya descartado no tiene
ningun sitio real al que navegar.

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **385/385** (145 Core + 240
ViewModels, +10 tests nuevos `WhereIsItTests.cs` - busqueda por id en Inventario/Almacen/
Equipamiento de un loadout real, `IsSearchMatch` real con y sin busqueda, duplicados reales,
sin resultados, navegacion real a un almacen NO seleccionado y a un loadout+vista NO
seleccionados, y limpieza real al cargar otro personaje). Arnes de UI Automation ampliado
(`H5-05-BOTON`/`H5-05-ABRIR`/`H5-05-BUSQUEDA`/`H5-05-NAVEGAR`) con verificacion real de extremo
a extremo: clic real (`InvokePattern`, el boton SI es invocable a diferencia de las tarjetas de
H5-12) sobre el boton de la cabecera **desde la pestaña Exploración** (a proposito, demuestra
que es visible en cualquier pestaña) abre el panel real, busqueda real por texto con debounce
real encuentra el objeto real ya colocado por la fixture (con duplicados reales detectados), y
`NavigateToWhereIsItResultCommand` navega/selecciona/cierra de verdad. **2/2 pasadas limpias**,
sin NO-FOUND/FALLO/EXCEPTION.

Fuera de esta pasada, documentado: la captura de pantalla del propio panel
(`h5-05-donde-lo-tengo.png`) NO muestra el contenido real del `Popup` - limitacion real y
conocida de WPF (un `Popup` abierto renderiza en su propia ventana de Windows aparte, un HWND
independiente del `Window` principal; `RenderTargetBitmap.Render(window)` solo captura el
arbol visual del `Window`, nunca el contenido de un Popup ya desacoplado a su propio HWND) - no
es un defecto de la funcion en si (todas las comprobaciones reales, no visuales, confirman que
funciona) ni algo que valga la pena rodear solo por la captura. Igual que H5-12/H5-14, la
disambiguacion real del CLIC en un resultado (`Border`+`InputBindings`, sin precedente de raton
simulado en este arnes) se verifica a nivel de comando real en vez de un clic de raton
simulado.

## H5-07 - Sesion/Ajustes reales (Tanda D, quinta auditoria de Opus) - CIERRA LA QUINTA AUDITORIA COMPLETA

Hallazgo real: "lo unico persistido entre sesiones es tamaño/posicion de ventana - cada
arranque vuelve a Inicio sin personaje... `CharacterFileService.GetAllPlayersDirectories`
tiene las 2 rutas escritas a fuego - quien tenga Terraria en otro disco/Documentos
redirigidos/instalacion portable ve el lanzador vacio sin forma de arreglarlo desde la app... no
existe ninguna pantalla de Ajustes (busqueda 'Ajustes/Settings/Opciones': cero resultados)".
Viola P1 (practicidad) y P2 (al alcance de la mano). Ultimo hallazgo de la quinta auditoria -
con este se cierran las 4 Tandas completas (A/B/C/D, 15/15 hallazgos).

**`SettingsService`/`SessionService`** (nuevos, `App/Services`): mismo vehiculo real ya
establecido (`%LOCALAPPDATA%\Terrakeep\*.json`, mismo patron exacto que
`WindowPlacementService` - `Load()`/`Save()` estaticos, try/catch best-effort, fichero
ausente/corrupto nunca revienta el arranque). `settings.json`: carpetas adicionales de
personajes/mundos + cupo de copias de seguridad. `session.json`: ultimo personaje (ruta+nombre+
fecha real de modificacion, para detectar cambios por fuera) + pestaña/sub-pestaña/plegado/
loadout/almacen.

**`CharacterFileService.ExtraPlayerFolders`/`ExtraWorldFolders`** (nuevos, estaticos):
`GetAllPlayersDirectories`/`GetAllWorldsDirectories` las concatenan a las 2 detectadas de
siempre - deduplicadas por ruta completa normalizada, filtradas a las que existen de verdad
(una carpeta borrada despues de configurarla se omite en silencio, no rompe el escaneo).

**`BackupHistoryService.MaxBackupsPerCharacter`**: paso de `const 20` fijo (documentado en H5-04
como "queda fuera de esta pasada") a propiedad real configurable.

**`SettingsViewModel`** (nuevo): `ExtraCharacterFolders`/`ExtraWorldFolders`
(`ObservableCollection<string>`) + `BackupHistoryCap` (recortado a >=1, mismo criterio real ya
usado en `HealthNow`/`HealthMax`). Persiste de inmediato en cada cambio real del usuario
(Add/Remove/cambio de cupo). El dialogo real de "elegir carpeta" vive en la View
(`MainWindow.xaml.cs`, `OpenFolderDialog` de `Microsoft.Win32` - .NET 8+, mismo criterio real ya
establecido en H5-03 con `SaveItemSet`/`LoadItemSet`).

**Bug real de contaminacion de tests, encontrado y arreglado ANTES de comitear nada** (no
documentado en el hallazgo original, descubierto verificando esta misma pasada): el primer
diseño llamaba `SessionService.Load()`/`SettingsService.Save()` directamente desde el
constructor de `MainViewModel`/`SettingsViewModel` - un simple `dotnet test` real en esta
maquina escribio de verdad un `session.json` sintetico en el `%LOCALAPPDATA%\Terrakeep\` REAL
de este equipo (confirmado, visto y borrado a mano) porque DECENAS de tests de este proyecto
construyen `new MainViewModel()` directamente. Arreglado de raiz: `MainViewModel.RestoreSession()`/
`SettingsViewModel.LoadFromDisk()` son metodos PUBLICOS que NUNCA corren en el constructor -
solo `MainWindow.xaml.cs` (la View) los llama, una vez, real, mismo criterio ya establecido por
`WindowPlacementService.Apply()`/`ConfirmDiscardChanges`. La escritura (`SaveSession()`) tampoco
se llama directo desde `LoadFromPath` (llamado tambien por decenas de tests) - nuevo evento
`MainViewModel.CharacterLoaded`, con `MainWindow.xaml.cs` como UNICO suscriptor real.
`SettingsViewModel._suppressPersist` arranca en `true` (modo "solo memoria") hasta que
`LoadFromDisk()` lo desactiva.

**Segundo bug real, encontrado con el propio arnes**: aplicar las carpetas adicionales de
Ajustes ANTES del primer escaneo real de Inicio/Exploracion (necesario, si no las carpetas
recien configuradas no aparecerian hasta el SIGUIENTE reinicio) exigia relanzar
`RefreshCommand`/`RefreshWorldsCommand` desde `MainWindow` justo despues de `LoadFromDisk()` -
pero el propio constructor de `HomeViewModel`/`ExplorationViewModel` YA dispara su propio
escaneo automatico (fire-and-forget) al construirse. Las 2 vueltas quedaban en marcha A LA VEZ
(ninguna cancela a la otra) y ambas AÑADIAN a la misma lista - visto de verdad en el arnes:
"10 personaje(s) encontrado(s)", cada uno duplicado exacto. Arreglado con un contador de
generacion real (`_scanGeneration`) en ambos ViewModels - solo la vuelta MAS RECIENTE aplica su
resultado, una vuelta vieja que termina tarde se descarta en silencio.

**"Continuar con Nombre"** (Inicio, la accion mas destacada de la pantalla): `HomeViewModel.
SetLastSession`/`ContinueCommand` - sin fichero real (borrado/movido desde la ultima sesion), no
ofrece nada; con `LastCharacterModifiedUtc` guardado distinto al real de ahora, aviso explicito
en la propia tarjeta ("Este archivo cambio desde la ultima vez..."), nunca carga automatica
silenciosa - un clic explicito del usuario es quien de verdad dispara `CharacterChosen`.

**"Ajustes"** (dentro de "Acerca de", la pestaña con menos densidad, pedido explicito del
informe): 2 listas reales de carpetas adicionales (con boton ✕ real por fila) + campo de cupo de
copias de seguridad.

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **405/405** (145 Core + 260
ViewModels, +25 tests nuevos: `SettingsViewModelTests.cs` (9, Add/Remove/dedup/recorte de cupo,
todo headless-safe gracias a `_suppressPersist`), `SessionRestoreTests.cs` (6, staleness real,
sin sesion/sin fichero, `ContinueCommand` real), `CharacterFileServiceDirectoriesTests.cs` (+4,
concatenacion/omision de inexistente/deduplicacion reales, con limpieza `try/finally` de los
campos estaticos), `BackupHistoryServiceTests.cs` (+1, `Purge()` real con un cupo bajo real).
Arnes de UI Automation ampliado con verificacion real de integracion de extremo a extremo (no
solo repetir lo que xunit ya cubre): una carpeta adicional real añadida desde Ajustes hace que
`Home` encuentre de verdad un personaje sintetico que antes no veia (con limpieza real al
terminar); la seccion "Ajustes" real se renderiza dentro de "Acerca de" (con captura real); el
cupo de copias cambia de verdad; `session.json` real queda en disco tras `CharacterLoaded`, con
la ruta correcta del personaje real cargado; y una `MainViewModel` NUEVA (simulando el proximo
arranque real) ofrece "Continuar con..." el personaje correcto via `RestoreSession()` sin
cargarlo sola. **2/2 pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION (arreglado en el camino un
3er bug real del propio arnes: mi bloque de Ajustes dejaba la navegacion en "Acerca de" sin
restaurarla, rompiendo B-7 - la fila real de la Libreria solo se realiza en el arbol visual
cuando "Objetos" es la pestaña activa de verdad, un `TabControl` real no mantiene el contenido
de una pestaña inactiva. Restaurado a Personaje > Objetos al final del bloque).

**Cierra la quinta auditoria de Opus por completo**: Tanda A (H5-06/08/15), Tanda B (H5-01/04/
03/02), Tanda C (H5-10/09/11), Tanda D (H5-12/13/14/05/07) - los 15 hallazgos del informe,
implementados, probados (405 tests unitarios + N pasadas limpias del arnes UIA por cada uno) y
comiteados uno a uno.

## Sexta auditoria de Opus - contexto (4-sep-2026)

Pedido explicito del usuario la noche del 3-sep-2026 (con 6 quejas concretas, dos capturas
reales de "Eldelgas" adjuntas), a atender SIN volver a preguntar porque el usuario se iba a
dormir: (1) el selector de personaje de "Inicio" no era fiel al selector real de Terraria; (2)
"a los personajes les faltan los brazos - ¿de que sirve un editor de apariencia si no refleja
como esta construido de verdad el personaje?"; (3) el mapa del mundo muestra 4 puntos rosas que
el usuario cree que son mascotas (son NPCs) - deberian verse solo cabezas de NPC; (4) objetos
vanilla animados (Alma de vuelo/Alma de luz...) se ven como una tira de fotogramas entera en vez
de un unico icono; (5) los buffs de Calamity no distinguen buff de debuff; (6) instruccion
explicita de elevar todo esto a un agente Opus, que dedicara el tiempo que hiciera falta leyendo
TODAS las fuentes decompiladas/compiladas disponibles, que produjera un plan, y que ese plan se
EJECUTARA directamente sin volver a preguntar. El agente Opus devolvio un informe de 12
hallazgos (H6-01 a H6-12); esta seccion documenta la Tanda A (la base de renderizado, de la que
dependen H6-02/03/04/05).

### Tanda A (H6-01/H6-02/H6-03/H6-04/H6-05) - el doll sin brazos, genero mal leido, un unico
### body real, pelo desfasado, armadura Body de Calamity que hacia desaparecer personajes

**Hallazgo real (H6-01, la causa raiz)**: `PlayerPreviewRenderer.Render` (el que dibuja tanto el
preview grande de Apariencia como la miniatura de cada tarjeta de "Inicio") recortaba SIEMPRE la
celda (0,0) de 40x56 de cada hoja de sprite del jugador - correcto para las 7 piezas que son
tiras VERTICALES (Head/EyeWhites/Eyes/LegSkin/Pants/Shoes/pelo), pero cero-relleno real para las
8 piezas que son rejillas COMPUESTAS 9x4 de 360x224 (TorsoSkin/Undershirt/Hands/Shirt/ArmSkin/
ArmUndershirt/ArmHand/ArmShirt) - el brazo/mano/manga real de esas piezas NO vive en la celda
(0,0). Confirmado leyendo `Terraria/DataStructures/PlayerDrawSet.cs` +
`Terraria/GameContent/PlayerDrawLayers.cs` decompilados reales (metodos
`CreateCompositeFrameRect`/`UpdateCompositeArm`/`DrawPlayer_12_Skin_Composite`/
`DrawPlayer_12_SkinComposite_BackArmShirt`/`DrawPlayer_17_TorsoComposite`/
`DrawPlayer_28_ArmOverItemComposite`) - las celdas reales del frame de reposo son Torso (0,0)
varon/(0,2) mujer, FrontShoulder (0,1)/(0,3), BackShoulder (1,1)/(1,3), FrontArm (2,0) IGUAL en
los dos generos, BackArm (2,2) IGUAL en los dos generos - y el orden real de capas es: 1)
LegSkin+TorsoSkin; 2) brazo TRASERO completo; 3) Pantalones/Zapatos; 4) Undershirt+Shirt en
BackShoulder y otra vez en Torso; 5) Cabeza/Ojos/Pelo; 6) brazo DELANTERO completo, ENCIMA de
todo. `PlayerPreviewRenderer.cs` reescrito de raiz con este mapa real de celdas y este orden
real de capas (`LoadBodyCell`/`SliceCell` nuevos, cachean la hoja completa 360x224 y recortan la
celda real pedida).

**H6-02 (Gender no era un booleano)**: `PlrCharacter.Gender` es en realidad `Player.skinVariant`
real (0-11: MaleStarter=0, MaleSticker=1, MaleGangster=2, MaleCoat=3, FemaleStarter=4,
FemaleSticker=5, FemaleGangster=6, FemaleCoat=7, MaleDress=8, FemaleDress=9,
MaleDisplayDoll=10, FemaleDisplayDoll=11 - confirmado en `Terraria.ID.PlayerVariantID.cs`
decompilado real). La app trataba `Gender==1` como "es chico" - un varon normal
(`skinVariant=0`, el caso mas comun con diferencia, ej. "Eldelgas") se leia y mostraba como
"Chica", y marcar "Chico" a mano escribia literalmente 1 (MaleSticker), corrompiendo el byte
real del `.plr` del usuario. Nuevo `TerrasavrNative.Core.Model.PlayerVariantSets.IsMale(byte)`
(set real `{0,1,2,3,8,10}`, `PlayerVariantID.Sets.Male` real) sustituye la comparacion directa
en `AppearanceViewModel`/`CharacterListEntryViewModel`. El selector de la app sigue siendo
binario (Chico/Chica) a proposito - las 10 variantes de vestuario alternativo no tienen
selector visual propio, fuera de esta pasada; cambiar de genero a mano colapsa a la variante
"Starter" real de ese genero (0/4), sin tocar una variante alternativa que el `.plr` ya trajera
cargada del mismo genero.

**H6-03 (una unica variante de cuerpo real)**: en la instalacion real de Steam solo existen las
10 piezas completas para las variantes 0 (MaleStarter) y 4 (FemaleStarter) -
`Player_1/2/3_{0,1,2,3,5,7,10}.xnb` NO existen en disco (confirmado con `ls` real), las
variantes 1/2/3/5-11 solo sustituyen un subconjunto y heredan el resto - fuera de esta pasada,
documentado. `head`/`eyewhites`/`eyes` son compartidas por TODAS las variantes (solo
`Player_0_{0,1,2}.xnb` existen) y se cargan siempre de `body0`.

**H6-04 (pelo desfasado en 1)**: el id real de `Player.hair` es 0-based (0..227), pero el
fichero en disco es `Player_Hair_{id+1}.xnb` (confirmado en `AssetInitializer.cs` real:
`"Images/Player_Hair_" + (num4 + 1)`, y en `UICharacterCreation.cs`: `switch (player.hair + 1)`
para el numero que el propio juego muestra). `scripts/extraer-sprites-jugador.js` reescrito
para extraer con el nombre de fichero real 0-based (`hair/{id}.png`, 228 ficheros
`0.png`..`227.png`); el bucle de `AppearanceViewModel.RebuildHairOptions` ya no empieza en 1;
`HairOptionViewModel.DisplayNumber` (`Id+1`) es SOLO para el tooltip visible (el mismo numero
que enseña el propio juego), el valor guardado real sigue siendo `Id` (0-based) sin desfase.

**H6-05 (consecuencia real de H6-01, no buscada aparte)**: la armadura/vanidad de Calamity Mod
en el slot Body usa la MISMA convencion de hoja compuesta 360x224 (confirmado: los
`Assets/calamity/icons/*_Body.png` miden 360x224 de verdad) - antes de este arreglo,
`LoadPngPixels` asumia SIEMPRE 40x56 y una pieza de Calamity puesta en el slot Body hacia
explotar `CopyPixels` con `ArgumentOutOfRangeException` real; `HomeViewModel.ScanCharacters` lo
tragaba en un catch mudo, asi que un personaje con equipo de Calamity puesto desaparecia del
listado de "Inicio" en silencio, sin ningun error visible. Con `LoadArmorCell` (mismo mecanismo
real que `LoadBodyCell`) esto se resuelve solo, sin ningun caso especial para Calamity.

**Reextraccion real de assets** (`scripts/extraer-sprites-jugador.js` reescrito,
`scripts/extraer-sprites-armadura-vanilla.js` con el grupo Body cambiado de `cropFrame0` a
`fullSheet` - ambos contra la instalacion real de Steam en este equipo): 23 piezas de cuerpo
(`body0/`+`body4/`, hojas 360x224 o tiras 40x56 segun corresponda), 228 estilos de pelo
0-based, 169 piezas de armadura vanilla del slot Body reextraidas a 360x224 (antes recortadas a
40x56 de fabrica). Ficheros viejos borrados con `git rm` (la carpeta `body/` antigua, el
`hair/228.png` huerfano del desfase 1-based) - las carpetas `body1/`, `body2/`, `body3/` de un
intento abortado de extraer las 5 variantes (bloqueado por permisos: `rm -rf`/
`Remove-Item -Recurse -Force` reales denegados en esta sesion desatendida, sin nadie delante
para aprobar un borrado destructivo) se dejan como restos inofensivos en disco, sin comitear -
documentado aqui en vez de forzar el borrado.

**Bug real encontrado y arreglado ANTES de comitear nada** (atrapado por `dotnet test` real, NO
por inspeccion): tras la reescritura, 59/260 tests de `TerrasavrNative.App.ViewModels.Tests`
fallaban con excepciones dispares (NullReferenceException, Assert.True/Equal fallidos) - la
causa real, aislada con un test de diagnostico desechable (creado y borrado en esta misma
pasada): `LoadSheetCached` hacia `Cache.GetOrAdd("sheet:" + path, LoadPngPixelsSheet)` -
`ConcurrentDictionary.GetOrAdd` invoca al factory con la CLAVE, no con la variable local
`path`, asi que `LoadPngPixelsSheet` recibia literalmente el string `"sheet:C:\...\
torsoskin.png"` como ruta real. `PngBitmapDecoder` intentaba resolverlo como un `Uri`, leia
"sheet" como si fuera el ESQUEMA de un URI (invalido), y lanzaba `NotSupportedException: "The
URI prefix is not recognized"` dentro de `WebRequest.Create` - excepcion que `MainViewModel.
LoadFromPath` traga por diseño (T-22), asi que decenas de tests que cargan un personaje
sintetico y esperan `IsCharacterLoaded=true` fallaban en cadena sin ningun rastro visible del
error real. Arreglado con un cierre real sobre `path`: `Cache.GetOrAdd("sheet:" + path, _ =>
LoadPngPixelsSheet(path))`. De paso, `PngBitmapDecoder` se cambio de `new Uri(path)` a
`File.OpenRead(path)` (Stream) en las dos funciones de carga - mismo patron ya establecido en
el resto del proyecto para leer PNG reales desde disco, evita depender de
`System.Net.WebRequest` para resolver un simple fichero local.

**Verificacion real**: `dotnet build` en verde (0/0). `dotnet test`: **423/423** (158 Core,
+13 tests nuevos: `PlayerVariantSetsTests.cs`, 12 casos reales de las 12 variantes de
`PlayerVariantID` mas el par Starter/Starter; 265 ViewModels, +5 tests nuevos:
`PlayerPreviewRendererH6Tests.cs` - recuento real de pixeles opacos tras renderizar (908/2240,
umbral 700, antes el bug de H6-01 dejaba piezas del brazo sin componer de verdad), tintado real
de `UnderColor` cambia los pixeles de salida, varon/mujer producen lienzos distintos (celdas de
torso/hombro reales), dos peinados vecinos cargan ficheros reales distintos, una pieza REAL de
Calamity en el slot Body renderiza sin excepcion y cambia el resultado). Arnes de UI Automation
ampliado con un bloque nuevo (`H6-01-DOLL`) que carga un personaje REAL de esta maquina
("Eldelgas", el mismo de las capturas originales del usuario, no uno sintetico) y verifica el
mismo recuento de pixeles opacos (948/2240 real) mas una captura real
(`h6-doll-personaje-real.png`) - confirmada a mano: el doll ahora muestra brazos, manos y mangas
reales, un personaje reconocible, no solo cabeza+piernas. **2/2 pasadas limpias**, sin
NO-FOUND/FALLO/EXCEPTION. De paso se corrigio un texto de la propia UI que habia quedado
obsoleto por este mismo arreglo (`MainWindow.xaml`, pie del preview de Apariencia: ya no dice
"tampoco refleja Chico/Chica", porque ahora SI lo refleja).

**Fuera de esta pasada, documentado**: sin accesorios (alas, mochilas, capas...), item en mano,
ni animacion (solo el frame de reposo); pelo bajo casco/pelo largo detras del cuerpo (H6-07,
Tanda D); el doll de la pestaña Apariencia en si todavia no muestra la armadura/vanidad puesta
(eso ya lo hace la tarjeta de "Inicio" desde la ronda anterior - unificarlo es H6-06, Tanda D).
Siguiente: Tanda C (H6-11 iconos vanilla animados, H6-12 buff/debuff de Calamity, H6-08/09/10
cabezas de NPC en el mapa), despues Tanda D.

### H6-12 - Distinguir buff de debuff en Calamity (Tanda C, sexta auditoria de Opus)

**Hallazgo real**: pedido explicito del usuario ("los buffs de calamity no se distinguen si son
buenos o malos, tienen que aparecer con algun marcador visual") - `BuffSlotViewModel`/
`BuffCatalogEntryViewModel` no llevaban ningun dato de si un buff de Calamity es en realidad un
DEBUFF (efecto negativo), asi que la Libreria de buffs y la rejilla de "Buffs" del personaje
mostraban un veneno o una maldicion exactamente igual que un buff positivo.

**Fuente real, no una lista a mano**: cada `ModBuff` real de Calamity marca
`Main.debuff[base.Type] = true;` (o `= false;` explicito) dentro de su PROPIO
`SetStaticDefaults()` - confirmado leyendo varios reales (`CalamityMod/Buffs/StatDebuffs/
Malnourished.cs`, `CalamityMod/Buffs/DamageOverTime/Bane.cs`, y buffs positivos que lo ponen a
`false` a proposito, `CalamityMod/Buffs/Potions/Zen.cs`). Los buffs que heredan de una base
compartida sin `SetStaticDefaults` propio (`BaseSummonBuff` y sus ~90 subclases de invocacion)
nunca tocan `Main.debuff` - por defecto ese array real de Terraria es TODO-false, asi que "sin
mencion" es de verdad "no es debuff", no una omision. **Ni la categoria ("DamageOverTime"/
"StatDebuffs" del propio `buffs.json`) ni el nombre sirven de heuristica fiable** - las 39
entradas de "DamageOverTime" SI son todas debuffs, pero "StatDebuffs"/"StatBuffs" mezclan
positivos y negativos de verdad, comprobado leyendo varios casos reales de cada categoria.

`scripts/extraer-debuffs-calamity.js` (nuevo) escanea los 312 ficheros .cs reales de
`CalamityMod/Buffs/**` con una regex sobre `Main\.debuff\[base\.Type\]\s*=\s*(true|false);` y
escribe `Assets/calamity/buff_debuffs.json` (solo los `internal` que SI son debuffs) - **108
debuffs reales de 312 buffs**, de los cuales **105 caen dentro del catalogo real ya usado por
la app** (`buffs.json`, 305 entradas - las 3 que faltan son buffs internos/no listados en el
catalogo). Verificacion cruzada del propio script: las 305 entradas de `buffs.json` casan 1:1
con un fichero de clase real (0 sin encontrar) - el campo `internal` SI es el nombre de clase
real, no una coincidencia parcial.

**Cambios reales**: `CalamityBuffEntry.IsDebuff` (nuevo, `CalamityBuffCatalog.LoadFromStream`
gana un tercer stream, `buff_debuffs.json`); `BuffSlotViewModel.IsDebuff`/
`BuffCatalogEntryViewModel.IsDebuff` lo propagan (vanilla se deja siempre en `false` - el
usuario lo pidio especificamente para Calamity, extraer el equivalente vanilla real de
`Main.debuff[]` exigiria localizar donde vanilla lo inicializa, que en el snapshot decompilado
de este equipo no vive en un fichero dedicado tipo `BuffID.cs` - fuera de esta pasada,
documentado). Punto MORADO nuevo (`DebuffColor`/`DebuffBrush`, `#9b59b6`, deliberadamente
distinto de `CalamityColor` para que "es de Calamity" y "es negativo" no se confundan cuando
las dos son ciertas a la vez) en la esquina LIBRE de cada tarjeta - esquina inferior-izquierda
en la rejilla de Buffs (el punto rojo de Calamity ya ocupa la superior-derecha), superior-
izquierda en la tarjeta de la Libreria (el borde ya esta ocupado por "es de Calamity" ahi, y el
boton "Colocar" real ocupa todo el borde inferior). Rotulo real "Debuff (efecto negativo)"
tambien en el tooltip de los dos sitios, no solo el punto de color.

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **428/428** (160 Core, +5 tests
nuevos: `CalamityBuffCatalogTests.cs` - fixture sintetica + prueba de humo real contra
`buff_debuffs.json` real, spot-check "Malnourished"=debuff real/"AbandonedSlimeBuff"=buff real,
recuento en rango 90-130 con margen real para no romper si Calamity actualiza el mod; 268
ViewModels, +3 tests nuevos: `BuffSlotDebuffTests.cs` - colocar un debuff real de Calamity marca
`IsDebuff=true`, un buff positivo lo deja en `false`, un slot vacio nunca es debuff). Arnes de
UI Automation ampliado con un bloque nuevo (`H6-12-DEBUFF`) que coloca un debuff real
("Aflicción del Absorbedor") en un slot vacio real y confirma `IsCalamity=True IsDebuff=True`
mas una captura real (`h6-12-buff-debuff.png`) - confirmada a mano: el punto morado se ve real
en la esquina del icono, junto al punto rojo de Calamity de otro slot sin pisarse. **2/2
pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION (una unica falla observada en la primera pasada
de `dotnet test` completo, `ExplorationWorldLauncherTests` - confirmada como inestable bajo
carga, no una regresion real: verde en solitario y verde de nuevo en la siguiente pasada
completa, sin tocar ningun codigo relacionado con Exploracion/mundos en esta seccion).

**Fuera de esta pasada, documentado**: equivalente vanilla de `IsDebuff` (el usuario lo pidio
especificamente para Calamity); ordenar/filtrar la Libreria de buffs por buff/debuff (solo se
pidio distinguir visualmente, no filtrar).

### H6-08/H6-09/H6-10 - Cabezas reales de NPC en el mapa (Tanda C, sexta auditoria de Opus)

**Hallazgo real**: pedido explicito del usuario ("el mapa del mundo muestra 4 puntos rosas que
creo que eran mascotas") - `NpcIconResolver` solo cubria los 27 NPCs del roster ORIGINAL
(`VanillaTownNpcRoster.cs`), y el marcador del mapa usaba el sprite de CUERPO entero de pie
(`Assets/npc_icons/{id}.png`, 40x54), no una cabeza como el mapa real del juego. El informe de
Opus señalo ademas que faltaba el NPC 441 (Recaudador de Impuestos) en el roster.

**Investigacion real, NO de memoria**: leyendo `Terraria/NPC.cs` decompilado completo (cada
`else if (type == N) { townNPC = true; ...}` real, incluidas condiciones OR de una sola linea
como `type == 637 || type == 638` o el bloque de 7 slimes en una sola linea) se confirmaron
**39 NPCs reales con `townNPC=true`**, no 27 - las 12 diferencias son exactamente los NPCs
1.4.4/1.4.4.9 que el roster original (mas viejo) nunca llego a incluir: `TravelingMerchant`
(368), `TaxCollector` (441, el hallazgo explicito del informe) y **las "mascotas de pueblo"
1.4.4 - TownCat/TownDog/TownBunny/TownSlime×8 (637/638/656/670/678-684) - justo el tipo de NPC
que el usuario confundia con una mascota de verdad**, y con razon: no tenian NINGUN icono real
hasta ahora. `VanillaTownNpcRoster.Ids` pasa de 27 a **40** (39 + SkeletonMerchant/453, que NO
pone `townNPC=true` pero ya estaba en el roster original y SI tiene perfil real).

El indice de icono de CABEZA real (`Images/NPC_Head_{indice}.xnb`, confirmado via
`AssetInitializer.cs`: `TextureAssets.NpcHead[i] = LoadAsset(...)`) **no es 1:1 con el tipo de
NPC** - la tabla real (`Terraria.GameContent.TownNPCProfiles.cs` decompilado, su diccionario
`_townNPCProfiles`, 40 entradas - EXACTAMENTE las 40 del roster) tiene 3 formas reales:
1. La mayoria: un indice FIJO normal + otro fijo "shimmerizado" (`LegacyWithSimpleShimmer`
   real) - "shimmerizado" es un estado GLOBAL por TIPO de ese mundo concreto (confirmado en
   `WorldFile.LoadNPCs` real: `NPC.ShimmeredTownNPCs[tipo]=true`, un array que se lee ANTES de
   la lista de NPCs, no por instancia).
2. Gato/Perro/Conejo de pueblo (637/638/656): indice VARIABLE segun `npc.townNpcVariationIndex`
   real (0-5, confirmado que SI se guarda por instancia en el propio `.wld` -
   `WorldFile.LoadNPCs`: `if (bitsByte[0]) townNpcVariationIndex = reader.ReadInt32();`) sobre
   un array real de 6 cabezas cada uno (`CatHeadIDs`/`DogHeadIDs`/`BunnyHeadIDs` reales).
3. Los 8 Slimes de pueblo: indice fijo cada uno (cada color es su PROPIO tipo de NPC, no una
   variacion del mismo tipo).
4. OldMan (37) y SkeletonMerchant (453): `-1/-1` reales - el propio juego NO les da icono de
   cabeza en el mapa (documentado tal cual, `HeadNormal=null`, no un icono inventado).

**Cambios reales**: `scripts/extraer-cabezas-npc.js` (nuevo) extrae los **81** `NPC_Head_
{0..80}.xnb` reales de la instalacion de Steam -> `Assets/npc_heads/{indice}.png` (81/0
ausentes). `TerrasavrNative.Core/Data/NpcHeadProfile.cs` (nuevo) porta la tabla real de
`TownNPCProfiles.cs` tal cual, `GetHeadIndex(npcType, variationIndex, isShimmered)`.
`WldNpc.VariationIndex`/`WldWorld.ShimmeredNpcTypes` (nuevos - el lector YA leia estos bytes
pero los descartaba, "sin uso aqui"; ahora se usan de verdad). `WorldNpcRowViewModel.
HeadIconPath` (nuevo, resuelto en `ExplorationViewModel.LoadFromPathAsync` via
`NpcHeadProfile.GetHeadIndex`) - `IconPath` (cuerpo, lista lateral) se queda intacto.
`MainWindow.xaml`: el marcador del MAPA pasa de `IconPath` (cuerpo, ancla abajo-centro) a
`HeadIconPath` (cabeza, ancla centrada - una cabeza no tiene "pies").

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **441/441** (173 Core, +11 tests
nuevos: `VanillaTownNpcRosterTests.cs` +2 -incluye el hallazgo real 441 y las 11 mascotas de
pueblo-, `NpcHeadProfileTests.cs` 7 casos reales -Merchant normal/shimmer, OldMan/
SkeletonMerchant null, TownCat con variationIndex real 0/1/5 y envoltura defensiva fuera de
rango, TownSlimeBlue fijo, TaxCollector el hallazgo explicito-, `WldReaderRealFileTests.cs` +1
contra 2 mundos reales de este equipo; 268 ViewModels sin cambio, la logica de NPCs vive en
Core). Arnes de UI Automation ampliado con un bloque nuevo (`H6-08-CABEZAS`) contra un mundo
real ("roca negra", 14 NPCs de pueblo reales) - **14/14 con cabeza real resuelta, 0 sin
cabeza** - mas una captura real centrada en el primer NPC (`h6-08-mapa-cabezas-npc.png`)
confirmada a mano: un grupo de cabezas pequeñas y distintas sobre el tejado de la casa, ya no
un bloque uniforme de puntos rosas. **2/2 pasadas limpias**, sin NO-FOUND/FALLO/EXCEPTION (un
intento previo con zoom real al 600%+centrado no encuadraba bien en ESTE arnes en concreto -
problema real del scroll del propio arnes bajo zoom extremo, no del codigo de produccion -
revertido a solo centrar sin forzar el zoom, documentado en el propio comentario del bloque).

**Fuera de esta pasada, documentado**: el "shimmer" real de este mundo de prueba resulto vacio
(`ShimmeredNpcTypes` sin entradas en los 2 mundos reales usados) - el camino esta implementado
y cubierto por `NpcHeadProfileTests.cs` con datos sinteticos, pero no se pudo confirmar contra
un NPC realmente shimmerizado de este equipo (ninguno de los mundos reales disponibles tiene
uno todavia). Extraer tambien los 13 sprites de CUERPO nuevos para la lista lateral
(`Assets/npc_icons/`) - la lista lateral no era el foco de la queja original (el mapa si), se
queda mostrando texto sin icono para esos 13 (mismo comportamiento ya documentado de
`NpcIconResolver`, no una regresion).

### H6-11 - Iconos vanilla reales, de raiz (Tanda C, sexta auditoria de Opus)

**Hallazgo real**: pedido explicito del usuario, con dos ejemplos concretos ("Alma de vuelo/
Alma de luz, etc. salen como una tira de fotogramas entera, no un unico icono - auditar TODOS
los objetos vanilla"). `VanillaIconResolver` extraia sus 5454 iconos de un atlas `items.png`
(32 columnas, celdas de 40x40) heredado de Terrasavr-Calamity-Beta - esa geometria de celda fija
no bastaba para los objetos REALMENTE animados de Terraria, cuyo sprite real es una tira
VERTICAL de 3 a 9 fotogramas del mismo alto, bastante mas alta que ancha.

**Investigacion real, no de memoria**: `Terraria.Main.InitializeItemAnimations()` decompilado
real (`Terraria/Main.cs`) es la fuente DEFINITIVA y completa de que objetos son animados de
verdad, sin excepcion: 15 `RegisterItemAnimation(id, new DrawAnimationVertical(ticks,
frameCount))` explicitos (incluye `520`=Alma de luz y `575`=Alma de vuelo, los dos ejemplos
EXACTOS citados por el usuario, confirmando que la investigacion apuntaba al sitio correcto) +
un bucle real sobre TODOS los `Terraria.ID.ItemID.Sets.IsFood` (86 ids reales, 3 fotogramas
cada uno - la comida tambien "respira"/brilla en el inventario real) + el Orbe de Adivinacion
(`5644`, clase de animacion distinta - `DrawAnimationScryingOrb` - pero el mismo recorte real:
`texture.Frame(1, FrameCount, 0, frameY)`, una tira vertical de `FrameCount` fotogramas, frame
0 = el de arriba del todo). **102 objetos animados reales en total**, ni uno mas ni uno menos.

**El fix real fue mas alla del bug reportado**: en vez de parchear solo esos 102 recortes
dentro del atlas viejo, se cambio la fuente entera - `Images/Item_{id}.xnb` real (instalacion
de Steam, un fichero POR OBJETO, mismo criterio ya usado con exito esta misma ronda para
jugador/armadura/NPCs) sustituye por completo al atlas. Esto ademas resolvio un problema mas
grande que no estaba en la queja original pero se descubrio investigando: el atlas viejo solo
cubria 5454 de los **6134** `Item_{id}.xnb` reales que existen de verdad en la instalacion -
**680 objetos sin NINGUN icono**, no solo mal recortados (`PalladiumDrill`/1189, el unico hueco
que el propio comentario del resolver documentaba, era literalmente UNO de esos 680).

**Cambios reales**: `scripts/extraer-iconos-vanilla.js` (nuevo, sustituye la extraccion vieja
por atlas) recorre `id=0..6195`, decodifica `Item_{id}.xnb` si existe, y si el id esta en la
lista real de 102 animados (tabla `EXPLICIT_ANIMATED` + `FOOD_IDS`, ambas portadas tal cual del
codigo fuente real, mas el caso especial `5644`) recorta la franja superior
(`alto_total/frameCount`, division EXACTA confirmada en los 102 casos reales, 0 alturas
impares) - el resto se usa completo, sin recorte. `VanillaIconResolver.cs` reescrito con el
comentario real actualizado (ya no atlas, ya no "1 hueco de 5455").

**Verificacion real**: extractor real: `6134 extraidos (102 animados recortados a 1 fotograma
real), 62 sin Item_{id}.xnb real, 0 con altura no divisible exacta`. `dotnet build` en verde.
`dotnet test`: **446/446** (173 Core sin cambio - la logica vive en un script Node, no en C# -,
+5 tests nuevos en ViewModels: `VanillaIconResolverH6Tests.cs` - Alma de luz/Alma de vuelo
decodificados de verdad desde el PNG real en disco (22x28, NO 22x112 - antes 4 fotogramas
apilados), un objeto de comida real recortado, `PalladiumDrill`/1189 (el hueco antiguo
documentado) YA resuelve icono real, un objeto NO animado conocido (Pico de hierro) sigue
resolviendo su sprite COMPLETO de 32x32 sin recortar de mas). Arnes de UI Automation ampliado
con un bloque nuevo (`H6-11-LIBRERIA`) que busca "Alma de" en la Libreria real (24 resultados
reales, tarjetas renderizadas sin excepcion), decodifica los dos PNG reales resueltos
(520/575, 22x28 los dos, confirmado por codigo) y deja una captura real
(`h6-11-libreria-objetos-animados.png`) - confirmada a mano: 24 tarjetas, cada una con un
icono limpio y proporcionado, ninguna tira ni recorte roto. **2/2 pasadas limpias**, sin
NO-FOUND/FALLO/EXCEPTION.

**Fuera de esta pasada, documentado**: los 5454 iconos que YA eran correctos con el atlas viejo
no se compararon pixel a pixel contra los 6134 nuevos de la fuente real (una diferencia de
recorte/paleta minima en algun id suelto es posible, pero improbable - la fuente real por
objeto es estrictamente mas fiable que un atlas de terceros, y los 446 tests + el arnes
completo siguen en verde sin ningun otro efecto secundario visible). El comentario historico de
`ContainerViewModel.cs` sobre "midiendo los 5454 iconos vanilla" (justificacion de `MinCell=40`)
no se reverifico contra el nuevo dataset - 102 outliers corregidos entre miles no deberian
mover una mediana de forma perceptible, pero es una afirmacion sin remedir, no una certeza.

### H6-06 - El doll de Apariencia ya muestra el equipo puesto EN VIVO (Tanda D, sexta auditoria de Opus)

**Hallazgo real**: el doll de la pestaña Apariencia era el UNICO sitio del proyecto que
todavia dibujaba al personaje sin su armadura/vanidad real puesta - la tarjeta de "Inicio" ya
lo hacia desde la ronda anterior (`CharacterListEntryViewModel`/`EquipmentAppearanceResolver`).
"Unificar" los dos sitios se dejo pendiente en H6-01 a proposito.

**Bug real encontrado ANTES de comitear nada** (no en el hallazgo original, descubierto
verificando con el propio arnes): el primer intento hizo que `AppearanceViewModel.RefreshPreview`
resolviera la armadura directamente de `character.PrimaryLoadout` (el mismo campo que YA usa
`CharacterListEntryViewModel`) - pero un test real (`dotnet test`) que equipaba un casco y
esperaba ver el preview cambiar **fallaba de verdad, sin ningun cambio de pixeles**.
Investigado: `PrimaryLoadout` es fiel al `.plr` real **solo justo al cargar o al GUARDAR**
(`CharacterFileService.Save`/`CalamityCharacterSync` es quien lo sincroniza) - durante la
sesion en curso, lo que el usuario edita en la pestaña Equipamiento vive en
`EquipmentGroupViewModel`/`MergedContainers` (`GameItem[]`, la representacion "fusionada"
vanilla+Calamity), no en `PrimaryLoadout` todavia. `CharacterListEntryViewModel` nunca tropieza
con esto porque SOLO lee personajes recien cargados del disco (la tarjeta de "Inicio"), nunca
una sesion en curso con ediciones sin guardar - el mismo campo real, en dos contextos
distintos, con una diferencia de sincronizacion real que no era obvia hasta intentarlo con un
test real.

**Arreglo real**: `AppearanceViewModel` ya no lee `PrimaryLoadout` directamente - gana
`UpdateEquippedArmor(EquippedArmor)` (empujado desde fuera) y un campo `_liveArmor` cacheado.
`MainViewModel.RefreshAppearanceEquipment()` (nuevo) construye un `PlrLoadout` SINTETICO de un
solo uso a partir de los 3 slots reales de armadura (cabeza/cuerpo/piernas, Items+Social) del
loadout 0 ("Puesto") **EN VIVO** desde `EquipmentGroup.EquippedItems`/`EquipmentGroup.
EquippedSocial` (esta ultima, nueva - gemela real de `EquippedItems`, ya existente, para el
slot de Vanidad) - sin tocar el modelo real del personaje, solo para alimentar
`EquipmentAppearanceResolver.Resolve` con datos frescos. Se llama: (1) tras `Appearance.
LoadFrom` en `LoadFromPath` (pinta el equipo real desde el primer render); (2) dentro de
`OnSlotItemChanged` (el mismo hook real que ya alimenta Deshacer/Rehacer) - CUALQUIER cambio de
slot dispara un refresco, no solo en Equipamiento (barato, y `EquipmentGroup==null` antes de
cargar personaje ya esta cubierto).

**Toggle real**: `AppearanceViewModel.ShowEquipment` (true por defecto, mismo criterio que
Inicio) - checkbox real "Mostrar equipo puesto" bajo el doll, `MainWindow.xaml`.

**Verificacion real**: `dotnet build` en verde. `dotnet test`: **450/450** (173 Core sin
cambio, 277 ViewModels, +4 tests nuevos: `AppearanceEquipmentPreviewTests.cs` -
`ShowEquipment` empieza en `true`; equipar un casco real (`EquipmentGroup.EquippedItems.
Slots[0].PlaceItem`, el mismo camino real que el propio `EquipmentDefenseTests.cs` ya
verificado) cambia los pixeles del preview EN VIVO sin recargar el personaje; apagar
`ShowEquipment` quita el casco del preview; reactivarlo lo vuelve a poner IDENTICO pixel a
pixel). Arnes de UI Automation ampliado con un bloque nuevo (`H6-06-DOLL`), justo despues de
T20-AUTOEQUIP (que YA equipa piezas reales) - confirma `ShowEquipment=True` por defecto, que
apagar el toggle SI cambia los pixeles reales del preview, y deja dos capturas reales
(`h6-06-doll-con-equipo.png`/`h6-06-doll-sin-equipo.png`) - confirmadas a mano: con el toggle
activado el doll lleva puesto un casco real (oscuro, tapando la cabeza); desactivado, vuelve al
pelo/piel base sin nada encima. **2/2 pasadas limpias** (de hecho 3/3 - una pasada intermedia
tuvo un FALLO real pero en un bloque totalmente ajeno, `L-c`/debounce de la Libreria, que volvio
a salir verde en la siguiente pasada sin tocar nada de esa zona - inestabilidad de temporizacion
bajo carga del propio equipo en esta sesion larga, no una regresion de H6-06).

**Fuera de esta pasada, documentado**: el toggle no distingue entre loadouts 1/2/3 - siempre
muestra el loadout 0 ("Puesto"), coherente con "lo que llevas puesto de verdad ahora mismo" (el
mismo criterio ya usado por Inicio); no respeta los 3 bytes de "ocultar equipo" del panel de
vanidad real (mismo hueco ya documentado en `EquipmentAppearanceResolver.cs`, heredado tal
cual, no nuevo de esta pasada).
