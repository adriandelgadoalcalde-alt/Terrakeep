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
