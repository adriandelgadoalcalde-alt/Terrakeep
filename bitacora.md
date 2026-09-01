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

## Pendiente (visible desde fuera, sin entrar en el detalle de Fase 1 de más arriba)

- **Exploración**: visor de mundo (`.wld`, zoom/desplazamiento, buscador de NPCs) - la pieza
  más grande que queda. Ya se construyó una vez en la versión JS de Terrasavr-Calamity-Beta
  esta misma sesión (ver su propio `PROYECTO-TERRASAVR.md`/README para el detalle real del
  formato `.wld`, ya investigado contra TEdit) - aquí hay que portarlo, no reinventarlo desde
  cero. Sin empezar todavía.
- **Edición real**: todo lo mostrado hoy es de solo lectura (ver un objeto, no añadirlo/
  quitarlo/cambiarle el prefijo). El botón Guardar ya funciona con lo que haya en memoria, pero
  nada en la UI actual permite modificarlo todavía.
- **Auto-equipar desde Builds**: el panel ya muestra el equipo recomendado, pero no lo aplica
  al personaje cargado (necesita resolver `pid`→id real y escribir en los slots correctos).
- Librería/Buscador de objetos, apariencia (pelo/piel con preview) - ni empezados.

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
