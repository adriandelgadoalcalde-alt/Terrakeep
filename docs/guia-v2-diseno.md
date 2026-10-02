# Guía v2: diseño del modelo compartido (Fase F0)

Documento de diseño de la nueva Guía de progresión de **Terrakeep** (escritorio WPF) y **TerrakeepMod**
(tModLoader), vanilla y Calamity. Lo escribió la fase F0 (02-oct-2026) para que F1 (contenido vanilla),
F2 (UI de Terrakeep) y F3 (UI de TerrakeepMod) trabajen en paralelo sin pisarse.

Encargo literal del usuario:

> «Haz lo de la guía calamity y vanilla con todo lo ya mencionado tanto para terrakeep como para el mod y
> que estén así de curradas. Por cierto, tiene que ser así tanto versión vanilla como calamity»

> «Y para el mod estaría bien que te marcara en el mapa qué hacer cada vez con una marca permanente en el
> mapa que apunte al siguiente lugar, y si no tienes los objetos que cliques y te lleve a la librería a
> cogerlos, aunque lo mejor sería que la guía te guiara para tener todo»

La referencia de profundidad es su guía HTML (`Downloads\Guia-Calamity\Guia-Calamity.html`): 47 paradas,
10 capítulos de manual más la ruta, 186 tareas y unas 16.000 palabras. Esa guía solo cubre cuerpo a cuerpo,
Experto y sin Revengeance. La nuestra sirve para todas las clases y modos, y comprueba sola lo que puede
contra el personaje y el mundo reales.

---

## 1. Arquitectura en una frase

**Un solo contenido, incrustado en `Terrakeep.Core.dll`, con un solo evaluador.** Las dos apps leen los
mismos bytes. TerrakeepMod consume esa DLL desde `lib\Terrakeep.Core.dll`. Cada app solo aporta tres
cosas: su **fuente de estado** (de dónde sale el personaje y el mundo), su **resolutor de referencias**
(cómo pasa de `"CalamityMod/DesertMedallion"` a un id de objeto) y su **almacén de progreso manual**.

```
scripts/guia-v2/*.js  ──(genera)──►  Terrakeep.Core/Guia/V2/Datos/guia_v2_<id>.json   (contenido)
                                     Terrakeep.Core/Guia/V2/Datos/guia_v2_referencias.json (nombres, ids, obtención)
                                          │  EmbeddedResource (LogicalName = nombre de archivo)
                                          ▼
                       GuiaV2Cargador ─► GuiaV2Doc + GuiaV2Referencias
                       GuiaV2Evaluador(doc, IResolutorRefsGuia) ─► ResumenGuiaV2
                          └─ cada hoja ─► GuideEvaluationEngine (el mismo motor de la v1)
                                              └─ IGuideStateProvider / IGuideStateProviderV2 (de cada app)
```

La guía v1 (`guia_progresion.json`, `TramoGuia`/`PasoGuia`, `GuideViewModel`) sigue existiendo y compila
sin cambios. La v2 es un modelo **nuevo al lado**: las UIs cambian de una a otra en F2 y F3.

### Archivos

| Archivo | Qué es |
|---|---|
| `Terrakeep.Core/Guia/V2/GuiaV2Modelo.cs` | Modelo de datos: documento, capítulos, paradas, tareas, condiciones, zonas, escaleras, fichas y fuentes. |
| `Terrakeep.Core/Guia/V2/GuiaV2Referencias.cs` | Tabla de referencias: objetos, NPC, estaciones y grupos, con nombre es/en, id y obtención. |
| `Terrakeep.Core/Guia/V2/GuiaV2Texto.cs` | Marcado en línea (§3). Lo analiza en segmentos y sabe sacar el texto plano. |
| `Terrakeep.Core/Guia/V2/GuiaV2Evaluador.cs` | Evaluador: condiciones, tareas, paradas, siguiente parada, avisos por modo y etapa de la escalera. |
| `Terrakeep.Core/Guia/V2/GuiaV2Progreso.cs` | Progreso manual por personaje, JSON con escritura atómica. |
| `Terrakeep.Core/Guia/V2/GuiaV2Ubicaciones.cs` | Sitúa una parada o zona en el mundo real a través de `IMundoGuia`. |
| `Terrakeep.Core/Guia/V2/GuiaV2Validador.cs` | Validador de contenido y cifras (`GuiaV2Cifras`). |
| `Terrakeep.Core/Guia/V2/GuiaV2Cargador.cs` | Carga de los recursos incrustados. |
| `Terrakeep.Core/Guia/IGuideStateProviderV2.cs` | Capacidades nuevas de la fuente de estado y `ModoPartida`. |
| `Terrakeep.Core/Calamity/CalamityEstadoGuardado.cs` | Lee del `.twld` Revengeance/Death, los esquemas y los laboratorios, y del `.tplr` las mejoras del jugador. |
| `Terrakeep.Core.Tests/Guia/GuiaV2Tests.cs` | Pruebas unitarias del modelo y del evaluador. |
| `Terrakeep.Core.Tests/Guia/GuiaV2ContenidoTests.cs` | **Gate de contenido**: referencias, condiciones y profundidad mínima. |
| `scripts/guia-v2/` | Pipeline reproducible que genera los datos (§9). |

---

## 2. El modelo

### Documento (`GuiaV2Doc`, uno por guía: `calamity`, `vanilla`)

| Campo | Para qué |
|---|---|
| `id`, `ambito`, `titulo`, `subtitulo`, `creditos` | Identidad y cabecera («Mi guía»). |
| `referencia` | Versiones de referencia (Terraria 1.4.4.9 de tModLoader, Calamity 2.2.4) y fecha de la investigación. Hay que enseñarlo en «Fuentes». |
| `clases` | Clases que admite la guía, en snake_case: `cuerpo_a_cuerpo`, `distancia`, `magia`, `invocacion` y, solo en Calamity, `picaro`. |
| `capitulos` | Capítulos de la **ruta** (eras), que agrupan paradas. |
| `paradas` | La ruta, en orden. Ver abajo. |
| `articulos` | Capítulos de **manual** (Empieza aquí, Mapa y biomas, Equipo, Vida, Laboratorios, Materiales, He encontrado esto, Secretos, Estoy perdido, Fuentes), en bloques. |
| `escaleras`, `leyendaEscaleras` | Escalera de equipo de cada clase (§6). |
| `zonas` | Biomas, capas y estructuras, con firma para encontrarlos en el mundo real (§7). |
| `avisosModo` | Avisos generales según el modo real de la partida (§8). |
| `problemas` | Fichas de «Estoy perdido / algo falla»: síntoma → qué comprobar. |
| `hallazgos` | Fichas de «He encontrado algo raro»: lo que ves → qué hacer, con enlace a su parada. |

### Parada (`Parada`)

Cada campo corresponde a una sección de la parada del HTML del usuario:

| Campo | Sección en la UI |
|---|---|
| `titulo`, `etiqueta`, `tipo` (`jefe`/`evento`/`exploracion`/`preparacion`/`desafio`), `opcional` | Cabecera. Las paradas `opcional` se pueden **aplazar**. |
| `jefes` | Refs de NPC para los retratos y la lectura del jefe en el mod. |
| `vidaObjetivo` (`min`, `max`, `trasMejora`, `texto`) | «VIDA OBJETIVO» o «VIDA DESPUÉS DE LA MEJORA». |
| `donde` + `ubicaciones` | «DÓNDE» en texto, más la **ubicación objetivo** que se resuelve contra el mundo real: marcador permanente en el mapa del mod y mapa de Exploración del escritorio. |
| `invocacion` (`objeto`, `donde`, `notas`) | «Cómo empezar este encuentro». |
| `preparate` | «1 · Prepárate», válido para **todas** las clases. |
| `preparatePorClase` | Matiz por clase. Hoy lleva el texto melee original del usuario en `cuerpo_a_cuerpo`. |
| *(escalera de la clase)* | Debajo de «Prepárate»: `GuiaV2Evaluador.EtapaParaParada(clase, parada.Id)`. |
| `necesitas` | Objetos que conviene tener. Cada uno es **clicable**: primero muestra cómo conseguirlo (obtención, §5) y luego, como atajo, la Librería. |
| `tareas` | «2 · Haz esto, en este orden»: casillas automáticas o manuales. |
| `combate` | «3 · Durante el combate / la exploración». |
| `desbloquea` | «4 · Lo que acaba de desbloquearse». |
| `listoCuando` | «Listo para seguir cuando…». |
| `completadaCuando` | Condición que da la parada por hecha sola (p. ej. la bandera del jefe). |
| `conserva` + `conservaObjetos` | «No vendas / conserva», con los objetos ya extraídos del texto. |
| `avisos` | Avisos de esta parada según el modo (§8). |
| `fuentes` | Enlaces a la wiki oficial (`Fuente.UrlResuelta()`). |

### Tarea (`Tarea`)

- `id` estable `"<parada>.<n>"`. Es la clave del progreso manual: **no se renumera**. Si cambia el orden, se
  conserva el id.
- `texto` (marcado en línea), `condicion` **o** `manual: true`. El validador exige una de las dos.
- `clases`: si no está vacío, la tarea solo aparece para esas clases.

### Bloques de contenido largo (`Bloque.tipo`)

| Tipo | Qué es |
|---|---|
| `titulo` | Subtítulo de sección. |
| `parrafo` | Párrafo de texto. |
| `lista` | Lista con `items`; `numerada` si lleva números. |
| `tabla` | `cabeceras` y `filas`. |
| `aviso` | Recuadro destacado. `estilo`: `destacado`, `suave` o `nota`. |
| `cajas` | Rejilla de bloques `caja` (cada uno con `titulo` y `bloques`). |
| `flujo` | Diagrama de pasos con `items`: «A → B → C». |
| `esquema` | Dibujo de texto por líneas con `items` (p. ej. la arena de práctica). |
| `fuentes` | Lista de `fuentes`. |

---

## 3. Marcado en línea (`GuiaV2Texto`)

Es mínimo a propósito, para que WPF y la UI nativa de Terraria lo pinten igual sin un motor HTML:

| Marcado | Qué es | Qué pinta |
|---|---|---|
| `**negrita**` | Negrita | Negrita. |
| `{o:Terraria/SlimeCrown}` | Objeto | Sprite + **nombre oficial en español**. Clicable: obtención y Librería. |
| `{n:CalamityMod/DesertScourgeHead}` | NPC o jefe | Icono + nombre en español. |
| `{z:mar_hundido}` | Zona | Nombre de la zona. Clicable: mapa centrado en ella. |
| `{p:desert}` | Enlace a una parada | Título de la parada. |
| `{a:mapa}` | Enlace a un artículo | Título del artículo. |
| `{z:mazmorra\|la mazmorra}` | Cualquier token con texto propio | Ese texto, para concordancias. El destino del enlace no cambia. |

`GuiaV2Texto.Analizar(texto)` devuelve los segmentos. `GuiaV2Texto.Plano(texto, refs, …)` devuelve el
texto plano para el buscador, los lectores de pantalla y el recuento de palabras.

---

## 4. Condiciones y evaluación

Una hoja `Condicion` usa el mismo vocabulario que `RequisitoGuia` (v1), más los tipos nuevos.
`GuideCatalog.TipoDesdeTexto` es la única fuente del vocabulario. Las refs van por nombre (`ref`/`refs`) y
se resuelven con el `IResolutorRefsGuia` de cada app.

| `tipo` | Campos | Fuente de estado |
|---|---|---|
| `bandera` | `bandera` | `GuideFlags` (escritorio: `.wld`/`.twld`; mod: en vivo). |
| `vida_maxima`, `cristales_vida`, `frutas_vida`, `mana_maxima`, `defensa` | `valor` | Personaje. |
| `objeto` | `ref` / `refs`, `cantidad` | Solo el inventario. |
| `objeto_poseido` (v2) | `ref` / `refs`, `cantidad` | Inventario, equipo (3 conjuntos), monedas, munición y las 4 huchas. |
| `equipado` (v2) | `ref` / `refs` | Conjunto de equipo activo. |
| `gancho`, `dano_arma`, `npcs_pueblo` | `valor` | Igual que en la v1. |
| `npc` | `ref` (NPC) | Vecinos del mundo. |
| `mejora_permanente` (v2) | `clave` | Vanilla: `demonHeart`, `torchGod`, `artisanBread`, `aegisCrystal`, `aegisFruit`, `arcaneCrystal`, `galaxyPearl`, `gummyWorm`, `ambrosia` (`Player.cs`, campos del `.plr`). Calamity: lista `boost` de `CalamityPlayer` (`CalamityEstadoGuardado.MejorasConocidas`). |
| `estado_mundo` (v2) | `clave` | `mundoCarmesi` (cabecera del `.wld`) y lista `downed` de `MiscWorldStateSystem` de Calamity (`CalamityEstadoGuardado.EstadosMundoConocidos`: `revenge`, `death`, `TalkedToDraedon`, esquemas…). |
| `todas` / `alguna` | `condiciones` | Composición. |

Estados de una condición (`EstadoCondicion`):

- `Cumplida`.
- `NoCumplida`: tiene progreso parcial en `Fraccion`.
- `SinDatos`: se sabría cargando el personaje o el mundo. **No** cuenta como hecha.
- `NoEvaluable`: este proveedor nunca podrá saberlo. Puede ser un límite estructural, una referencia que la
  app no resuelve o un tipo que un proveedor v1 no implementa. Tampoco cuenta.

Reglas:

- Una tarea está **hecha** si su condición está `Cumplida` **o** el jugador la marcó a mano.
- Una parada está **completada** si su `completadaCuando` está cumplida **o** el jugador la marcó.
- La **siguiente parada** es la primera ni completada ni aplazada (`ResumenGuiaV2.Siguiente`).

**Compatibilidad.** Las capacidades nuevas están en `IGuideStateProviderV2`, que hereda de
`IGuideStateProvider`. Un proveedor que solo implemente la v1, como el `ProveedorEstadoGuiaMod` actual,
sigue compilando: los tipos nuevos le salen `NoEvaluable` con el motivo `guide_motive_v2_unsupported`. Los
valores nuevos de `TipoRequisitoGuia` van **al final** del enum.

---

## 5. Referencias, nombres oficiales y «cómo conseguirlo»

`guia_v2_referencias.json` lo genera `scripts/guia-v2/generar-referencias.js` a partir de **todo** lo que
citan las guías:

- **ids**: `ItemID.cs` y `NPCID.cs` de tModLoader 1.4.4.9 decompilado, la versión que corre el mod. No se
  usan los JSON de 1.4.5 del escritorio. Los NPC vanilla pueden tener id **negativo**: son variantes por
  netID reales.
- **Nombre en español**:
  - Vanilla: `Terraria.Localization.Content.es-ES.*`, la localización oficial incrustada en tModLoader.
  - Calamity: Calamity 2.2.4 **no trae es-ES** (comprobado listando su `.tmod`, solo trae en-US), así que
    se usa la traducción española que existe para el mod, **CalamityModEsp 2.2.0.1** (workshop 2829795471,
    la que tiene instalada el usuario).
  - Sin traducción, se queda el nombre inglés con `fuenteEs: "sin traduccion"`. Nunca se inventa.
  - Hoy hay 186 objetos así, entre ellos Sanguine Tangerine, Tainted Cloudberry y Sea King.
- **Obtención** (`RefObjeto.obtencion`): recetas (ingredientes, grupos, estaciones y condiciones), botín
  de NPC o jefe (con probabilidad), bolsas del tesoro y tiendas (vendedor y condición). Se extraen con
  `scripts/guia-v2/extraer-obtencion.js` del código decompilado real. Cada dato lleva `fuente` con
  `archivo:línea`.
  - Profundidad 2: objetos citados más sus ingredientes directos. Los ingredientes de nivel 3 solo llevan
    nombre.
  - Una `obtencion` vacía significa que no sale de receta, botín ni tienda: se consigue en el mundo
    (minería, cofres, pesca…). La UI tiene que decirlo así y no inventar.
  - El extractor no reconoce el 1,6 % de los patrones; quedan listados en su salida.
- **Estaciones** (`estaciones["Terraria/Tile/Anvils"]`) y **grupos de receta** (`grupos["AnyEvilBar"]`):
  - Las estaciones se nombran con el objeto que coloca el tile o con `MapObject` oficial.
  - Los grupos se nombran evaluando la expresión real de `RecipeGroup` en `Recipe.cs` y `RecipeSystem.cs`.

**Cómo mostrar «cómo conseguirlo»** (punto 3 del usuario; prioridad sobre el atajo de Librería):

1. Haz clic en un `{o:…}` o en un objeto de `necesitas`. Se abre una ficha con el sprite, el nombre y
   `obtencion`. Por ejemplo:
   - «Receta: 40 × Cualquiera Bloque de arena + 4 × Mandíbula de hormiga león + 2 × Mandíbula de Tormentaleón, en
     Altar demoníaco / Altar carmesí».
   - «Cae de Azote del Desierto (1/1, 25–30)».
   - «Lo vende Amidias».
2. Los ingredientes también son clicables: recorren la cadena.
3. Botón secundario «Buscar en la Librería» con el objeto ya filtrado (atajo, mod y escritorio).
4. Si la condición tiene progreso (`ObjetoPoseidoVarios`), se enseña «tienes X de Y».

---

## 6. Escaleras de equipo por clase

Hay una `EscaleraClase` por clase. Cada `EtapaEscalera` tiene:

- `desde`: id de la parada a partir de la cual aplica.
- `momento`.
- `armas`, `armadura`, `accesorios` y `otros`, cada uno con `OpcionEquipo{ref, nota, rol, conjunto, marcas}`.
- `nota`: «qué haces después».
- `fuentes`.

Calamity tiene 21 etapas por clase en las cinco clases: 105 etapas y unos 5.300 objetos verificados. Salen
de `calamitymod.wiki.gg/wiki/Guide:Class_setups` y sus `/data`, consultadas el 02-oct-2026. La wiki declara
la versión 2.1.2, pero **cada objeto se verificó contra el decompilado 2.2.4**. Los datos de investigación
están en `scripts/guia-v2/datos/escaleras_calamity_wiki.json`.

- La UI muestra la etapa actual con `EtapaActual(clase, resumen)`.
- En cada parada muestra `EtapaParaParada(clase, paradaId)`.
- Las piezas se agrupan por `conjunto`.
- La leyenda de marcas (†, C, +, ≤, Δ, *, Ω) está en `leyendaEscaleras`.
- La «tabla Momento | Opciones y origen | Qué haces después» del encargo es esta estructura. El origen sale
  de la obtención (§5): no se copia a mano.

**Clase del jugador.** La elige el jugador y se guarda en `GuiaV2ProgresoManual.Clase`. F2/F3 pueden
proponerla según el arma con más daño (`DanoDelMejorArma` + `damageType`), pero la última palabra es del
jugador.

---

## 7. Ubicación objetivo en el mundo real

Cada parada tiene `ubicaciones`, en orden de preferencia:

- `{tipo:"zona", id, siMundo?}`: `siMundo` puede ser `corrupcion` o `carmesi`.
- `{tipo:"punto", id}`: `spawn`, `mazmorra`, `oceano_lado_mazmorra`, `oceano_lado_opuesto` o los centros
  de laboratorio de Calamity (`SunkenSeaLabCenter`…).
- `{tipo:"npc"|"jefe", id:ref}`: la resuelve la app.

Una `Zona` tiene:

- `nombre` oficial y `fuenteNombre`.
- `capa`.
- `firma`: `tiles` (TileID 1.4.4.9), `tilesMod` (`"CalamityMod/Navystone"`) y `minimo`.
- `punto` guardado, opcional.
- `resumen`: viene del capítulo «Mapa y biomas» y sirve para el tooltip del marcador.

`GuiaV2Ubicaciones.ResolverParada(parada, doc, IMundoGuia, mundoCarmesi)` devuelve
`UbicacionResuelta(X, Y, Aproximada, Origen)` en casillas:

- Si hay punto guardado, se usa.
- Si no, el centro de la celda de 64×64 con más casillas de la firma, con desempate por cercanía al spawn
  (muestreo cada 4 casillas).
- Si no hay firma, el centro de la capa sobre el spawn, marcado `Aproximada`. **Si es aproximada hay que
  decirlo en la UI.**
- `null` significa que la zona no existe en ese mundo: por ejemplo, un mundo creado sin Calamity.

Implementaciones de `IMundoGuia`:

- **Escritorio (F2)**: sobre el lector de tiles de Exploración (`.wld` + `TwldReader.Read` para los tiles
  de mod). `Punto(clave)` sale de `CalamityEstadoGuardado.LeerMundo(twld).PuntosEnCasillas`.
- **Mod (F3)**: sobre `Main.tile` en vivo, `TileLoader` para los tiles de mod y `CalamityWorld.*LabCenter`
  por reflexión o los mismos puntos.

---

## 8. Avisos según el modo

`AvisoModo{modos[], texto}`. Los modos son `clasico`, `experto`, `maestro`, `viaje`, `revengeance` y
`death`. Un aviso sin modos aplica siempre.

`IGuideStateProviderV2.Modo` devuelve `ModoPartida(GameMode, Revengeance, Death)`:

- Escritorio: `WldHeader.GameMode`, y Rev/Death de `CalamityWorldState`.
- Mod: `Main.GameMode`, y `CalamityWorld.revenge`/`death`.

`ResumenGuiaV2.AvisosGenerales` y `ResultadoParada.AvisosActivos` ya vienen filtrados. Sin mundo cargado se
enseñan todos.

Ejemplos con datos reales del decompilado:

- Las bolsas que dan las mejoras de Ira y Adrenalina solo en Revengeance: `CrabulonBag.cs:80`,
  `CalamityWorld.revenge && !rageBoostOne`.
- En Maestro, la Cebolla Celestial actúa como Corazón demoníaco (`CelestialOnion.cs`).
- Los porcentajes del «Rework de Progresión del Hardmode Temprano».

---

## 9. Cómo se añade o cambia contenido

Todo se **genera**. No se editan a mano los JSON de `Datos/`, salvo para probar algo.

```
cd scripts/guia-v2
node construir-guia-calamity.js     # guía del usuario + anotaciones + escaleras -> guia_v2_calamity.json
node generar-referencias.js         # (ejecuta extraer-obtencion.js) -> guia_v2_referencias.json
cd ../..
dotnet test Terrakeep.Core.Tests --filter "FullyQualifiedName~GuiaV2"   # gate: 0 errores
```

| Script | Qué hace |
|---|---|
| `fuentes.js` | Rutas y cargadores de las fuentes reales: decompilados, localizaciones y `.tmod`. Las rutas salen de la carpeta del usuario o de `KEEP_DIR`, `TK_DECOMPILADO` y `TK_WORKSHOP`. |
| `nombres.js` | Cambia los nombres ingleses por tokens `{o:}`/`{n:}`/`{z:}`. Prioridad: glosario > lista curada del usuario > diccionario completo, y gana la coincidencia más larga. Las palabras sueltas solo casan con la misma mayúscula inicial. |
| `html-a-bloques.js` | Pasa el HTML de la guía del usuario a bloques. |
| `anotaciones-calamity.js` | Lo que añade Terrakeep: títulos, condiciones, ubicaciones, «Prepárate» para todas las clases, avisos por modo, zonas y glosario (con la fuente de cada término). |
| `construir-guia-calamity.js` | Ensambla la guía e informa de los nombres ingleses que quedan sin sustituir. |
| `extraer-obtencion.js` | Extrae recetas, botín y tiendas del código decompilado. |
| `generar-referencias.js` | Genera la tabla de referencias. |

### Para F1 (vanilla)

1. Crea `scripts/guia-v2/anotaciones-vanilla.js` y `construir-guia-vanilla.js` con la misma forma. Pueden
   reutilizar `fuentes.js`, `nombres.js` y `html-a-bloques.js`.
   - No hay HTML de partida para vanilla. El contenido se escribe en las anotaciones o en un JSON fuente
     propio, con la estructura de §2.
   - Si se reutiliza algo del HTML de Calamity, tiene que ser solo lo que valga sin el mod.
2. La salida es `Terrakeep.Core/Guia/V2/Datos/guia_v2_vanilla.json`, con `id: "vanilla"` y
   `clases` sin `picaro`. `GuiasDisponibles()` la detecta sola.
3. **Profundidad equivalente**, no recortada:
   - Unas 45 paradas en 4 eras.
   - Unas 180 tareas, con el mayor número posible evaluables.
   - Capítulos de manual equivalentes: biomas y capas, Mazmorra, Templo, Inframundo, eventos e invasiones,
     vida y mejoras, materiales y estaciones, «He encontrado algo raro» (santuarios, cofres de bioma, Aether
     y Shimmer…), «Estoy perdido» y fuentes.
   - Escaleras de las 4 clases desde terraria.wiki.gg (`Guide:Class setups`), verificadas contra
     `ItemID.cs` 1.4.4.9.
4. Las zonas vanilla ya definidas en `anotaciones-calamity.js` se pueden copiar tal cual.
5. Añade en `GuiaV2ContenidoTests` la aserción de profundidad de vanilla. La de contenido válido ya se
   aplica a todas las guías incrustadas.
6. Las banderas vanilla que use deben estar en `GuideFlags` (lectura del `.wld`). Lo que falte se amplía
   allí, con su prueba.

---

## 10. Cómo lo consume cada UI

### F2 · Terrakeep (escritorio)

1. **Contexto.** Al cargar el personaje y el mundo, rellena en `GuideContext`:
   - `CalamityPlayerBoosts = CalamityEstadoGuardado.LeerMejorasJugador(raizDelTplr)`.
   - `CalamityWorldState = CalamityEstadoGuardado.LeerMundo(bytesTwld).Claves`.
   - Lo que ya rellenaba (`Character`, `MergedContainers`, `World`, `CalamityDownedFlags`).
2. **Proveedor.** `guideEvaluator.CrearProveedor(contexto)` devuelve un `IGuideStateProviderV2` con los
   catálogos reales.
3. **Resolutor.** `new ResolutorRefsGuia(refs, r => calamityCatalog.ByModAndInternal("CalamityMod", interno)?.SyntheticId)`.
   - Los NPC de Calamity no se resuelven en el escritorio. Hoy solo afecta a `sunken.3`, la tarea de Sea
     King, que queda manual de hecho.
4. **Progreso.** Usa `ArchivoProgresoGuia.Cargar`/`Guardar` sobre una carpeta de datos de la app.
   - La clave es el nombre del `.plr` sin extensión.
   - Nunca se guarda dentro de la partida real del usuario.
5. **Pantallas**, siguiendo el HTML del usuario y respetando el tema de Terrakeep:
   - «Mi guía»: siguiente parada, progreso, avisos del modo y botones rápidos.
   - «Ruta»: selector de eras, lista de paradas con estado (✓, aplazada o número), parada completa con sus
     secciones, «Hecha» y «Aplazar» (solo opcionales).
   - Artículos.
   - «Estoy perdido» y «He encontrado algo raro».
   - Buscador sobre `GuiaV2Texto.Plano`.
   - «Mi equipo»: escalera de la clase.
   - Selector de clase.
   - Sprites:
     - Vanilla: `Assets/vanilla/icons`, con el id de `refs`.
     - Calamity: `Assets/calamity/icons/<interno>.png`.
     - Jefes: `BossIconResolver`/`CalamityBossIconResolver`.
6. **Mapa.** La parada siguiente se marca en el mapa de Exploración con `GuiaV2Ubicaciones` sobre un
   `IMundoGuia` del lector de tiles.
7. **Textos de resultados.** Las claves `Guia.Req.*`, `Guia.Mejora.*`, `Guia.EstadoMundo.*` y
   `Guia.Bandera.*` (nuevas incluidas) ya están en `Assets/guia/textos.*.json`. Los motivos `guide_motive_*`
   están en `strings_*.json`.

### F3 · TerrakeepMod (tModLoader)

1. Actualiza `lib\Terrakeep.Core.dll` con `scripts\actualizar-core.ps1` (build net8.0). Los datos viajan
   dentro.
2. Implementa `IGuideStateProviderV2` en `ProveedorEstadoGuiaMod`:

   | Miembro | De dónde sale |
   |---|---|
   | `CuantosPosee` | `player.inventory`, `armor`, `miscEquips`, `bank`…`bank4` y los 3 `Loadouts`. |
   | `LlevaEquipado` | `player.armor[0..9]`. |
   | `MejoraPermanente` | Vanilla: `player.extraAccessory`, `unlockedBiomeTorches`, `ateArtisanBread`, `usedAegisCrystal`… Calamity: campos de `CalamityPlayer` por reflexión (`mFruit`, `sTangerine`, `tCloudberry`, `sStrawberry`, `extraAccessoryML`, `rageBoostOne`…; ver `CalamityPlayer.SaveData`). |
   | `EstadoMundo` | `WorldGen.crimson` y `CalamityWorld.revenge`/`death`/`TalkedToDraedon`, más `RecipeUnlockHandler.HasFound…`, por reflexión. |
   | `Modo` | `Main.GameMode` y Rev/Death. |

3. Las **banderas** que usa la guía Calamity tienen que estar en `BanderasGuia`. Incluye las nuevas:
   `downedLeviathan`, `downedEoCAcidRain`, `downedAquaticScourgeAcidRain`, `downedCLAMHardMode`,
   `downedNuclearTerror` y `downedBossRush`. Son propiedades públicas de `DownedBossSystem`.
4. **Resolutor**: `ModContent.TryFind<ModItem>`/`TryFind<ModNPC>("CalamityMod", interno)` para Calamity.
   Vanilla sale de la tabla.
5. **Progreso** en `ModPlayer.SaveData`/`LoadData`, con `GuiaV2ProgresoJson.Serializar`. Viaja con el
   personaje.
6. **Marcador permanente en el mapa**, en pantalla completa y en el minimapa:
   - Apunta a `ResolverParada(siguiente)` sobre un `IMundoGuia` de `Main.tile`.
   - Se recalcula al completar una parada.
   - Si la ubicación es `Aproximada`, se pinta como zona.
   - En las paradas con `jefes` que estén vivos, apunta al NPC.
7. **Objetos que faltan**: cada objeto de `necesitas` o `{o:}` abre primero su ficha de obtención (§5) y
   después ofrece «Coger en la Librería» con el objeto ya buscado.

### Reparto de archivos para no pisarse

| Fase | Archivos |
|---|---|
| F1 | `scripts/guia-v2/*vanilla*` y `Datos/guia_v2_vanilla.json`. Regenera `guia_v2_referencias.json` (este archivo lo comparten F0 y F1: hay que regenerarlo, nunca editarlo a mano). |
| F2 | `Terrakeep.App/**` (vistas y ViewModels nuevos de la guía v2). En Core, solo correcciones con prueba. |
| F3 | Repo TerrakeepMod. En Core, solo correcciones con prueba. |
| Cambios de modelo o evaluador | Al Core, con prueba en `GuiaV2Tests` y una nota aquí. |

---

## 11. Gate y cifras actuales (F0)

`dotnet test Terrakeep.Core.Tests`: 823/823. Contenido Calamity (`GuiaV2ContenidoTests`):

| Cifra | Nuestra guía | Guía HTML del usuario |
|---|---|---|
| Paradas | 47 (13 opcionales; las 47 con `completadaCuando`) | 47 |
| Capítulos | 4 de ruta + 10 artículos | 10 + ruta |
| Tareas | 186 | 186 |
| Tareas con condición automática | 120 (64,5 %) | — |
| Tareas manuales (persistidas) | 66 | — |
| Fichas de «Estoy perdido» | 48 | — |
| Fichas de «He encontrado algo raro» | 12 | — |
| Zonas con firma o punto | 31 | — |
| Etapas de escalera (5 clases × 21) | 105 | — |
| Palabras (sin contar escaleras) | ≈17.800 | ≈16.100 |
| Palabras (con notas de escaleras) | ≈31.300 | — |

Las 120 tareas con condición se reparten así: 75 por banderas, 74 por objetos poseídos, 8 por NPC, 7 por
mejoras permanentes y 6 por estado de mundo (algunas condiciones compuestas suman en varios tipos).

### Límites conocidos (documentados, no disimulados)

- Calamity no tiene es-ES propia. Se usa CalamityModEsp y hay 186 objetos sin traducir.
- Hay 66 objetos citados sin obtención en el código, porque se consiguen en el mundo (cofres, minería,
  pesca).
- En el escritorio, los NPC de pueblo de Calamity (Sea King, Archmage, Brimstone Witch) no son evaluables.
- Las escaleras vienen de una wiki que declara la 2.1.2, aunque cada objeto está verificado en la 2.2.4.
- El grupo de receta `HardmodeForge` no tiene traducción en CalamityModEsp y queda en inglés.
