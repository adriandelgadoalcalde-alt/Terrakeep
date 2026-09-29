# Clasificacion formal de regiones - Responsive Global s3

Requirement `6b59710e-e57b-4677-89a1-2c4c58c29b5a` (Terrakeep), criterio `d24b1ac9fd`
("toda la app auditada y clasificada STRUCTURAL_FINITE/FINITE_PAGEABLE/UNBOUNDED_COLLECTION").
HEAD auditado: `a36947ca` (Terrakeep.App/Terrakeep.App.Tests). Auditoria de solo lectura -
ningun cambio de codigo de produccion en esta ronda.

## Metodologia y fuentes

- Definiciones literales de clase: encargo `responsive-global-encargo.txt`, s3 (paginas 10-12),
  contrastadas con s13 (scroll ownership, paginas 26-28), s23 (colecciones grandes, paginas 36-37)
  y s34 (texto/informacion estructural sin scroll local, paginas 47-51).
- Codigo real: `Terrakeep.App/MainWindow.xaml` (3208 lineas) + las 17 vistas de
  `Terrakeep.App/Views/*.xaml` + `Terrakeep.App/Styles/Theme.xaml` (estilo `NavTabControl`).
- Perfil KeepQA: `KeepQA/src/xaml-analyzer/__perfiles-responsive__/Terrakeep.json`
  (`contenedoresCriticos`, `scrollOwnership`) y `__waivers__/Terrakeep.json`.
- Bitacora real de las fases A-G del mismo requirement (`bitacora.md`, entradas 28/29-sep-2026,
  "FASE A" a "FASE G" y "Cierre tecnico de la condicion 15") - fuente de las medidas de scroll
  (px) citadas como evidencia.
- El propio codigo YA contiene comentarios literales de clasificacion en varios puntos (grep
  real de `STRUCTURAL_FINITE`/`FINITE_PAGEABLE`/`UNBOUNDED_COLLECTION`/`s23`/`s34` en
  `Terrakeep.App/**/*.xaml`) - citados tal cual cuando existen, nunca inventados.

Cada fila indica: region (x:Name o archivo:linea real, verificado con Grep contra el codigo
vivo) - clase - por que segun s3 - estrategia s14 en el viewport minimo (1080x700) y en grande -
scroll owner - waiver s13 si aplica - evidencia (log/canario/captura real que existe en
`docs/evidencia/responsive-global/` o cita textual del propio XAML).

Las 3 clases, definicion literal (s3):
- **STRUCTURAL_FINITE**: tabs, botones, selectores, cabeceras, Loadout, filtros, categorias
  principales, paneles estructurales - NO scroll necesario para descubrirlos en el viewport minimo.
- **FINITE_PAGEABLE**: Armadura/Vanidad/Tintes, grupos de Equipamiento, regiones finitas que no
  deben mostrarse simultaneamente si no hay espacio - paginacion/subvistas/selector explicito, NO
  scroll como sustituto de navegacion.
- **UNBOUNDED_COLLECTION**: biblioteca de objetos, biblioteca de buffs, investigacion, resultados
  de busqueda, Exploracion - scroll permitido y esperado en SU viewport de resultados.

---

## A. Cabecera y navegacion (nivel ventana)

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 (min / grande) | Scroll owner | Waiver s13 | Evidencia |
|---|---|---|---|---|---|---|---|---|
| A1 | Rail de navegacion superior (Inicio/Personaje/Builds/Guia/Exploracion/Servidor/Novedades/Acerca de) | `RootTabControl` (`MainWindow.xaml:1179`), estilo `NavTabControl` (`Theme.xaml:1038-1105`, `TabStripPlacement="Left"`, `StackPanel` en vez de `TabPanel` implicito) | STRUCTURAL_FINITE (s3-A "tabs") | 8 pestañas fijas, nunca crecen con datos del usuario | Reflow: rail vertical apilado (StackPanel), sin envolver ni recortar; identico en min y grande | Ninguno (la rail no scrollea, cabe entera en 700px de alto) | No aplica | canario `NAV123_SOLO` 0 FALLO (bitacora FASE B/D); cobertura KeepQA "navegacion siempre descubrible" PASS, evidencia `75fc9fd0-8bcb-4e2b-a246-b4462d4796ca` |
| A2 | Cabecera superior (vida/mana/dinero/horas + menu Personaje + deshacer/rehacer + Cargar/Guardar + codigo de build) | `WrapPanel` sin x:Name, `MainWindow.xaml:711` (`ItemHeight="44"`) | STRUCTURAL_FINITE (s3-A "botones", "cabeceras") | Conjunto fijo de acciones/datos del personaje cargado, no una coleccion | Reflow real: `WrapPanel` (no `StackPanel`) cede a una 2a linea si no caben todos los botones (ADR-TERRAKEEP-006); igual en grande, con margen `20,0,0,0` que solo actua si hay hueco | Ninguno propio (vive fuera de cualquier `ScrollViewer`) | No aplica | cobertura KeepQA "structural controls visibles sin scroll" PASS, evidencia `f79ce969-4537-4599-9b02-ee8fa94acde1`; canario `EQUIP_RESPONSIVE_SOLO`/`RESTO_RESPONSIVE_SOLO` 0 FALLO |

---

## B. Personaje > Objetos > Equipamiento

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| B1 | Selector de subseccion Equipamiento/Inventario/Almacenes | `ObjetosNavToggle` (`ObjetosView.xaml:1433-1476`, 3 `RadioButton`) | STRUCTURAL_FINITE (s3-A "selectores") | Selector fijo de 3 opciones, siempre las mismas | Sin scroll; "caben enteros en el minimo (~330px de ~630 en ES)" (comentario real linea 1432) | n/a | No aplica | canario `NAV123_SOLO`/`INVALM_RESPONSIVE_SOLO` 0 FALLO |
| B2 | Subpagina Equipamiento (contenedor) | `ObjetosPaginaEquipamiento` (`ObjetosView.xaml:925`) | (contenedor, no clasifica por si mismo) | - | `ScrollViewer` unico de la subpagina | **Scroll owner declarado**: `Personaje.Objetos.Equipamiento` -> `ObjetosPaginaEquipamiento` (perfil KeepQA) | No aplica | `EQUIP_RESPONSIVE_SOLO` 0 FALLO (bitacora FASE B-G) |
| B3 | Cabecera de Equipamiento (Loadout/Vista/Defensa/bono de set) | `EquipamientoCabecera` (`ObjetosView.xaml:1022`); comentario literal linea 1016: "Cabecera STRUCTURAL_FINITE (s6/s34): Loadout + selector de subvista..." | **STRUCTURAL_FINITE** (s3-A "Loadout"; literal en s6/s34) | Loadout/Vista/Defensa/bono de set son propiedades fijas del personaje, no una coleccion | TextWrapping + reflow (retirado el `ScrollViewer MaxHeight=112` de AR-14d en FASE B); el bloque crece en vertical y lo absorbe `ObjetosPaginaEquipamiento` | `ObjetosPaginaEquipamiento` (scroll owner de la pagina, NUNCA local) | No aplica (el mecanismo viejo, `oldMechanisms` del requirement, esta RETIRADO) | bitacora FASE D: "0 scroll, 0 recorte" con el peor caso (granadas Picaro, 17 prefijos) y Cenit; contenedor critico KeepQA `EquipamientoCabecera` |
| B4 | Mascota/Montura/Gancho + tintes asociados | `CajaMascotasTintes` (`ObjetosView.xaml:998`) | STRUCTURAL_FINITE (s3-A "paneles estructurales"; s5 explicito) | Slots fijos (mascota/montura/gancho + sus tintes), no crecen | Reflow puro (el `ScrollViewer` de seguridad propio que tenia se retiro en FASE B) | `ObjetosPaginaEquipamiento` | No aplica | contenedor critico KeepQA `CajaMascotasTintes`; `EQUIP_RESPONSIVE_SOLO` 0 FALLO |
| B5 | Selector Armadura / Vanidad / Tintes | `EquipamientoSelectorVista` (`ObjetosView.xaml:1055`, `WrapPanel`) | STRUCTURAL_FINITE (s3-A "selectores"; s4 explicito: "debe permanecer descubrible en TODOS los tamaños sin scroll") | Es el selector, no el contenido de cada subvista | Envuelve si hace falta (WrapPanel), siempre visible, centrado | n/a (vive fuera de cualquier scroll interno) | No aplica | contenedor critico KeepQA `EquipamientoSelectorVista`; canario H-05 (bitacora FASE B): 3/3 con una subvista, Editar sigue el slot equivalente |
| B6 | Contenido de Armadura/Vanidad/Tintes (una subvista o las 3 lado a lado) | `EquipamientoSubvistaActual` (Compacto/Normal, `:1092`) / `EquipamientoSubvistasLadoALado` (Amplio/Extra, `UniformGrid` 1x3, `:1103`, con `EquipamientoColumnaTintes`/`Vanidad`/`Armadura`); comentario literal linea 1082: "Subvista(s) FINITE_PAGEABLE (s4/s16/s17...)" | **FINITE_PAGEABLE** (s3-B, ejemplo literal "Armadura", "Vanidad", "Tintes") | Contenido finito (slots fijos por subvista) que no debe mostrarse simultaneamente si no hay espacio | Compacto/Normal: UNA subvista completa via el selector B5 (`IsEquipmentSideBySide`=false); Amplio/Extra: las 3 lado a lado en `UniformGrid` 1x3 (H-02, hueco maximo 28px, antes 140-210px) - el selector sigue existiendo en ambos regimenes (s4: "el selector debe seguir existiendo") | `ObjetosPaginaEquipamiento` | No aplica | negative acceptance B ("Vanidad/Tintes sin navegacion visible") PASS - cobertura KeepQA `d91312e273` (Equipamiento/Vanidad/Tintes accesibles por paginacion); `EQUIP_RESPONSIVE_SOLO` 0 FALLO |
| B7 | Monedas / Municion | `CajaMonedasMunicion` (`ObjetosView.xaml:1157`) | STRUCTURAL_FINITE (slots fijos) | Igual que B4 | Reflow, sin scroll propio | `ObjetosPaginaEquipamiento` | No aplica | `EQUIP_RESPONSIVE_SOLO` 0 FALLO |

---

## C. Personaje > Objetos > Inventario / Almacenes

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| C1 | Subpagina Inventario (rejilla de slots fijos + cabecera) | ObjetosPaginaInventario (:1192) / ObjetosSeccionInventario (:1207) | STRUCTURAL_FINITE (numero fijo de slots del inventario del juego, no crece) | Inventario de Terraria tiene un numero de slots FIJO por definicion del juego | SlotGridPanel con reflow (adaptive columns via FASE A); si no cabe ni a MinCell, lo absorbe el scroll de pagina | ObjetosPaginaInventario (scroll owner declarado en perfil KeepQA) | No aplica | INVALM_RESPONSIVE_SOLO 0 FALLO (35 capturas, bitacora FASE C/E/F) |
| C2 | Subpagina Almacenes: cabecera (selector de almacen + acciones) | AlmacenesCabecera (:1357), AlmacenesSelector (pildoras, :1363), AlmacenesAcciones (:1377) | STRUCTURAL_FINITE (s3-A "categorias principales"/"selectores") | Selector de almacenes (banco/cofre etc.) + botones fijos | 2 WrapPanel apilados y centrados (R2-L1 de FASE D: desviacion 0px en 1080/1366/1920, antes -8,5/-66,5px) | ObjetosPaginaAlmacenes | No aplica | contenedor critico KeepQA AlmacenesCabecera; INVALM_RESPONSIVE_SOLO 0 FALLO |
| C3 | Rejilla de slots del almacen seleccionado | ObjetosSeccionAlmacenes (:1331) | STRUCTURAL_FINITE (numero fijo de slots por tipo de contenedor) | Igual criterio que C1 | SlotGridPanel reflow | ObjetosPaginaAlmacenes (scroll owner declarado) | No aplica | INVALM_RESPONSIVE_SOLO 0 FALLO |
| C4 | Panel Editar (compartido Equipamiento/Inventario/Almacenes/Buffs) | EditarTarjeta (ObjetosView.xaml:1486) | STRUCTURAL_FINITE (s3-A "paneles estructurales"; s7 explicito) | Editor de UN slot a la vez, contenido fijo (indice/cantidad/prefijo) | Barra lateral de alto completo (Grid.RowSpan=2), SIN ScrollViewer propio desde FASE D (retirados los 2 ScrollViewer anidados de ItemEditTemplate); reflow interno | El de la pagina que lo aloja (ObjetosPaginaEquipamiento/Inventario/Almacenes) | Permisos historicos RESUELTOS: baa9fe76/aa5f7395 (Editar con Libreria desplegada, hasta 26,9px) y 570a3c9d (Editar con Cenit/arma de prefijos largos, hasta 141,2px) - los 3 marcados RESOLVED por el commit 9c3df863 de FASE D | contenedor critico KeepQA EditarTarjeta; bitacora FASE D, medido con el peor objeto del catalogo (granadas Picaro de Calamity, 17 prefijos) y Cenit, 7 escenarios x ES/EN |

---

## D. Personaje > Objetos > Biblioteca / Biblioteca de buffs / Investigacion (familia CATALOGO)

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| D1 | Categorias principales (compartido por las 3 superficies) | CategoriasPrincipales (Views/NavegadorCategorias.xaml:67, WrapPanel); comentario literal linea 16: "Envuelve a otra linea si no caben, nunca se esconden detras de un scroll (STRUCTURAL_FINITE, s3 A)." | STRUCTURAL_FINITE (s3-A "categorias principales"; literal en el propio XAML) | Son las raices del arbol (10 en Objetos, 8 en Buffs), fijas | WrapPanel que envuelve, "Ver todo" incluido, nunca scroll (s12: "Top-level categories no deben quedar perdidas") | El de la superficie (LibreriaResultados/LibreriaBuffsResultados/InvestigacionResultados) | No aplica (mecanismo viejo, columna 210px + ScrollViewer propio de arbol, RETIRADO en FASE D - 0 coincidencias de CategoryNodeTemplate) | contenedor critico KeepQA CategoriasPrincipales; cobertura "categorias principales accesibles sin desplazar un scroll exterior" PASS (434b6e5a); LIBRARY_RESPONSIVE_SOLO 0 FALLO (137 capturas) |
| D2 | Ruta/migas + subcategorias (drill-down) | RutaSubcategorias/BotonSubcategorias/PopupSubcategorias (NavegadorCategorias.xaml:77-113) | STRUCTURAL_FINITE (navegacion), con flyout/drill-down para las subcategorias segun s12 | Migas siempre visibles (1 linea); las subcategorias reales pueden ser muchas, se acceden por Popup, no recortan la pagina | Un unico scroll LOCAL justificado dentro del propio Popup (ScrollSubcategorias, :112) - excepcion explicita permitida por s12, NO es page+category+result apilados | El Popup es autocontenido (overlay), no compite con LibreriaResultados | Patron ya aceptado por diseno (mismo espiritu que los pickers de Apariencia, ver F3) | LIBRARY_RESPONSIVE_SOLO cubre drill-down (bitacora FASE D) |
| D3 | Cabecera Libreria de objetos (plegar + buscador + Filtros) | LibreriaCabecera/LibrarySearchBox/LibraryFiltersButton/LibraryFiltersPopup (ObjetosView.xaml:1516-1598) | STRUCTURAL_FINITE (s3-A "filtros") | Buscador y boton de filtros fijos, una fila | En una sola fila (FASE D: "necesario por el alto: a 1080x700 la Libreria de objetos tiene 202px en total") | LibreriaResultados | No aplica | LIBRARY_RESPONSIVE_SOLO 0 FALLO |
| D4 | Resultados de la Libreria de objetos | LibreriaResultados (ObjetosView.xaml:1700, contiene TarjetasCategoriasRaiz + SlotGridPanel Columns=60 AdaptiveColumns PreferirCeldaGrande, :1715) | UNBOUNDED_COLLECTION (s3-C ejemplo literal "biblioteca de objetos") | 8903 objetos reales del catalogo Vanilla+Calamity | Adaptive columns (uso del ancho 99-100% en todos los tamanos, FASE D); scroll DENTRO de su propio viewport (s23) | Scroll owner declarado: Personaje.Objetos.Library.Results -> LibreriaResultados | No aplica | cobertura "adaptive grids donde corresponda" PASS (b8e11000); LIBRARY_RESPONSIVE_SOLO 0 FALLO, capturas lib-despues-*-1080x700*.png (docs/evidencia/responsive-global/faseD/) |
| D5 | Cabecera Libreria de buffs (plegar + buscador) | LibreriaBuffsCabecera/BuffLibrarySearchBox (MainWindow.xaml:1346-1371) | STRUCTURAL_FINITE | Igual criterio que D3, misma familia (s10: "aplicar el mismo contrato que a Biblioteca de objetos") | Misma fila unica | LibreriaBuffsResultados | No aplica | LIBRARY_RESPONSIVE_SOLO 0 FALLO |
| D6 | Resultados de la Libreria de buffs | LibreriaBuffsResultados (MainWindow.xaml:1401, SlotGridPanel AdaptiveColumns PreferirCeldaGrande, :1414) | UNBOUNDED_COLLECTION (s3-C "biblioteca de buffs") | Catalogo completo de buffs (300 en busqueda amplia de prueba) | Adaptive columns; scroll en su propio viewport | Scroll owner declarado: Personaje.Objetos.BuffLibrary.Results -> LibreriaBuffsResultados | No aplica | LIBRARY_RESPONSIVE_SOLO 0 FALLO, capturas lib-despues-buff-*-1080x700*.png |
| D7 | Cabecera Investigacion (buscador + acciones de carpeta) | InvestigacionCabecera (:1496, Width=260), SoporteAccionesInvestigacion (:1508) | STRUCTURAL_FINITE (s3-A "filtros"; s11 "misma familia responsive: CATALOGO/CATEGORY BROWSER") | Buscador + 2 botones de accion de carpeta, fijos | Fila unica, dentro del flujo de categorias (D-04, mismo orden que las 2 Librerias) | InvestigacionResultados | No aplica | LIBRARY_RESPONSIVE_SOLO 0 FALLO (Investigacion comparte canario con Library/BuffLibrary) |
| D8 | Resultados de Investigacion | InvestigacionResultados (:1521); comentario literal: "Unico scroll owner vertical de Investigacion (s13: Research.Results -> results owner)" | UNBOUNDED_COLLECTION (s3-C "investigacion", literal) | Resultado del catalogo de investigacion, WrapPanel de tarjetas | Scroll en su propio viewport (TarjetasCategoriasRaiz cuando ShowRootCategoryCards, si no WrapPanel de ResearchRowViewModel) | Scroll owner declarado: Personaje.Objetos.Research.Results -> InvestigacionResultados | No aplica | LIBRARY_RESPONSIVE_SOLO 0 FALLO |

---

## E. Personaje > Buffs (pestana propia, no Biblioteca de buffs)

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| E1 | Rejilla de 44 slots de buffs | dentro de BuffsPaginaContenedor (MainWindow.xaml:1288), AjusteBuffs (:1289) | STRUCTURAL_FINITE (numero fijo de slots de buff activos, no crece) | 44 slots reales, fijos por el juego | SlotGridPanel reflow (mismo patron ya validado por Inventario/Almacenes en FASE C, retirado BuffContainerCompactTemplate) | BuffsPaginaContenedor (scroll owner declarado) | No aplica | PERSONAJE_RESPONSIVE_SOLO / bitacora FASE E, "44/44 celdas visibles sin scrollbar" |
| E2 | Panel Editar de Buffs | EditarBuffTarjeta (:1324) | STRUCTURAL_FINITE | Igual criterio que C4 | EditarTarjetaComposicion compartida, en la fila de contenido | BuffsPaginaContenedor | No aplica | bitacora FASE E, capturas personaje-despues-visual-* |
| E3 | Biblioteca de buffs embebida en la pestana | LibreriaBuffsPanel (:1335) | Ver D5/D6 (misma superficie fisica, reusa la familia CATALOGO) | - | - | LibreriaBuffsResultados | - | - |

---

## F. Personaje > Apariencia

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| F1 | Columna 0: vista previa del personaje (doll + nombre) | ScrollViewer SIN x:Name, Grid.Column=0 (MainWindow.xaml:1630); AppearancePreviewImage :1634 | STRUCTURAL_FINITE (una sola imagen + un borde, contenido fijo) | El doll no es una coleccion; el scroll es solo fallback de ultimo recurso | AppearanceCompactFactor (regla continua por ALTO real, 1.0 en 700px / 0.0 desde 768px) interpola Padding/Margin; el doll escala a un escalon entero (240x336 6x / 200x280 5x); medido 0/0px de scroll en los 5 tamanos x ES/EN tras el cierre del known-diff d7a5a6ef | Ninguno declarado en el perfil KeepQA (ver Hallazgo 1) | No aplica (no hay nesting, es el unico ScrollViewer de la columna) | bitacora "29-sep-2026 (madrugada) - Apariencia: cierra el known-diff d7a5a6ef": "1080x700 ES 60,9/284,7px -> 0/0; EN 46,3/284,7 -> 0/0"; capturas personaje-despues-visual-apariencia-1080x700-es.png / -en.png (faseE) |
| F2 | Columna 1: editable (genero/peinado/tinte + 7 tarjetas de color + estadisticas) | ScrollViewer SIN x:Name, Grid.Column=1 (:1674) | STRUCTURAL_FINITE (conjunto fijo de 7 selectores de color + 3 filas de stats, no crece con datos del usuario) | s34 explicito ("descripciones, estados, propiedades... labels" son STRUCTURAL_FINITE) | Misma regla AppearanceCompactFactor (18 propiedades derivadas); eje horizontal de las tarjetas FIJO (10px margen / 8px padding) para no romper las 3 columnas de siempre; solo el eje vertical se comprime | Ninguno declarado (ver Hallazgo 1) | No aplica para el ScrollViewer en si; SI aplica a los 2 pickers internos (ver F3) | Mismo log que F1: 0/0px a 1080x700 ES/EN; geometria identica byte a byte a partir de 1366x768 contra afe51344 |
| F3 | Picker de tinte de pelo / Picker de peinado (overlay) | ScrollViewer MaxHeight=240 (:1725) / ScrollViewer MaxHeight=360 (:1762), dentro de F2 | STRUCTURAL_FINITE (lista finita de opciones de tinte/peinado del juego) mostrada en un widget emergente | Selector visual autocontenido (Border con acento propio), no es contenido de flujo de pagina | Popup/overlay con limite de altura explicito | El propio picker (autocontenido, no compite con F1/F2) | Waivers activos: scroll-anidado-1725 y scroll-anidado-1762 (__waivers__/Terrakeep.json), re-verificados 29-sep-2026 | cita literal del waiver: "ScrollViewer con MaxHeight=240/360 real limitando un widget acotado... no contenido de flujo de pagina" |

Nota de coherencia: Apariencia tiene DOS scroll owners simultaneos (F1 y F2), uno por columna
independiente - no compiten por el mismo eje/contenido (no es el patron prohibido de s26-H, "dos
scroll owners compiten en el MISMO contexto"), pero ninguno de los dos tiene x:Name ni esta
declarado en scrollOwnership del perfil KeepQA. Ver Hallazgo 1.

---

## G. Personaje > Puntos de aparicion / Desbloqueos / Version

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| G1 | Puntos de aparicion (lista + boton Anadir) | ScrollViewer SIN x:Name (Views/SpawnpointsView.xaml:67) | STRUCTURAL_FINITE (spawn points definidos por el propio mundo/personaje, cabecera+boton fuera del scroll) | Contenido finito real, el propio ScrollViewer es solo fallback | Un unico ScrollViewer de pagina, cabecera fuera de el | Sin declarar en perfil KeepQA (ver Hallazgo 1) | No aplica | bitacora FASE E: "0px de scroll en los 5 tamanos x ES/EN desde el principio - ya cumplia el contrato" |
| G2 | Desbloqueos (4 grupos) | ScrollViewer SIN x:Name (Views/UnlocksView.xaml:45) | STRUCTURAL_FINITE (4 grupos fijos de desbloqueos del juego) | Igual criterio | DetailContentMaxWidth subido de 760 a 900 (FASE E) para usar el ancho real disponible | Sin declarar (ver Hallazgo 1) | No aplica | bitacora FASE E: "20,5px de scroll a 1080x700 -> 0px en los 5 tamanos x ES/EN" tras el arreglo |
| G3 | Version (4 grupos + diagnosticos, ej. "Prefijos ilegales") | ScrollViewer SIN x:Name (Views/VersionView.xaml:42) | STRUCTURAL_FINITE | Bloque de diagnosticos finito (problemas detectados en ESTE personaje, no una coleccion abierta) | Mismo DetailContentMaxWidth compartido | Sin declarar (ver Hallazgo 1) | No aplica | bitacora FASE E: "59,2px -> 0px en los 5 tamanos x ES/EN"; personaje de prueba con diagnosticos reales (6 objetos) visibles sin scroll |

---

## H. Personaje > Comparar

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| H1 | Selectores de personaje A/B | Grid Grid.Row=1 (Views/CompareView.xaml:162) | STRUCTURAL_FINITE (s3-A "selectores") | 2 ComboBox fijos, fuera del ScrollViewer de resultados a proposito | Cabecera "pegajosa" (comentario literal linea 140: "cabecera pegajosa con los dos selectores, FUERA del ScrollViewer de mas abajo") | n/a (vive fuera de CompareResultsScrollViewer) | No aplica | bitacora FASE E: "selectores siempre fuera del scroll (Grid.Row=1, confirmado en la captura)" |
| H2 | Resultados de comparacion (estadisticas + equipo + 2x50 inventario) | CompareResultsScrollViewer (:221) | UNBOUNDED_COLLECTION (s23 explicito, "Comparar resultados") | Contenido que crece con el numero de diferencias reales entre los 2 personajes | Scroll en su propio viewport | Scroll owner declarado: Personaje.Comparar.Results -> CompareResultsScrollViewer | Waiver legitimo declarado en el propio perfil KeepQA ("si algun dia compite con otro scroll - hoy PASS sin competidores") | bitacora FASE E: "940,9px a 1080x700, bajando a 105,5px a 2560x1440... legitimo y esperado"; resize en caliente sin perder la seleccion de los 2 personajes |

---

## I. Inicio

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| I1 | Cabecera (logo + titulo + descripcion) | dentro de HomeContentScroll (Views/HomeView.xaml:298-338) | STRUCTURAL_FINITE | Bloque fijo, siempre el mismo texto | MaxWidth={Binding InicioContentMaxWidth} (crece en Amplio/Extra) | HomeContentScroll | No aplica | comentario literal linea 295-296: "unico ScrollViewer de la vista, contenido UNBOUNDED_COLLECTION real (tarjetas de personaje)... scroll legitimo (s23)" |
| I2 | Lista de tarjetas de personaje (Home.Characters) | dentro de HomeContentScroll | UNBOUNDED_COLLECTION (crece con los .plr/.tplr reales del usuario; 6 en el entorno de pruebas real) | Coleccion real abierta, mismo criterio ya aceptado para Comparar (s23) | WrapPanel/UniformGrid de tarjetas, columnas adaptativas (condicion 7 de s33: "InicioContentMaxWidth sin tope en Extra") | HomeContentScroll (scroll owner declarado: Inicio -> HomeContentScroll) | No aplica | bitacora FASE F: "hasta 290px de scroll a 1080x700 con 6 personajes reales... contenido real que ya existia con el mismo scroll"; captura resto-verde-faseG3-inicio-1080x700-es.png |

---

## J. Builds

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| J1 | Filtro de clases | ItemsControl sin x:Name, WrapPanel (Views/BuildsView.xaml:37) | STRUCTURAL_FINITE (s3-A "filtros") | Numero fijo de clases (melee/ranged/mage/summoner/rogue) | WrapPanel, envuelve si hace falta | n/a | No aplica | bitacora FASE F: "selector+contenido de la sub-pestana activa completos" |
| J2 | Selector interno Vanilla / Calamity Mod | TabControl estilo InnerTabControl (:49) | STRUCTURAL_FINITE (s3-A "tabs") | 2 opciones fijas | TabStripPlacement=Top, siempre visible | n/a | No aplica | idem |
| J3 | Contenido de cada subtab (etapas/clases de build con equipo recomendado) | ScrollViewer SIN x:Name dentro de cada TabItem (:51 y :56) | FINITE_PAGEABLE (catalogo real y acotado del juego -3 etapas x hasta 5 clases-, paginado por el selector J2; no es una coleccion del usuario) | El contenido esta paginado (Vanilla/Calamity) y cada subtab es la unidad de navegacion; el scroll dentro de la subpagina es el ultimo recurso (s14 paso 8) para una lista de tarjetas ya acotada | Reflow del WrapPanel/ItemWidth interno (BuildClassTemplate, MainWindow.xaml:99-158); scroll de subpagina si no cabe | Sin declarar en perfil KeepQA (ver Hallazgo 1) | No aplica | bitacora FASE G: "Builds 100%/100% de uso del ancho a 1920/2560"; captura resto-verde-faseG3-builds-sub0-1080x700-es.png |

---

## K. Guia

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| K1 | Banner de objetivo actual (titulo/porque/por-donde-se-empieza/que-te-falta) | GuideObjetivoBanner (Views/GuideView.xaml:154) | STRUCTURAL_FINITE | Bloque fijo por objetivo activo | Dentro del flujo de GuideContentScroll, primera seccion | GuideContentScroll | No aplica | comentario literal linea 62: "...largo, scroll legitimo (s23)" |
| K2 | Arbol de progresion completo | dentro de GuideContentScroll | UNBOUNDED_COLLECTION (documental por naturaleza, s23) | Arbol de progresion real del juego, potencialmente largo | Columna derecha con HorizontalAlignment Stretch (WARN-02, corregido para que la columna * SI crezca con MaxWidth) | GuideContentScroll (scroll owner declarado: Guia -> GuideContentScroll) | No aplica | bitacora FASE F: "objetivo actual completo... es contenido instructivo/documental por naturaleza"; FASE G: "Guia 99%/72% de uso del ancho a 1920/2560" |

---

## L. Exploracion

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| L1 | Plegar/expandir sidebar | ExpandExplorationSidebarButton/CollapseExplorationSidebarButton (MainWindow.xaml:2257/2313) | STRUCTURAL_FINITE (boton) | Accion fija | Siempre visible | n/a | No aplica | - |
| L2 | Selector Browse/WorldTools del sidebar | RadioButton GroupName=ExploracionSidebarModo (:2334-2342) | STRUCTURAL_FINITE (s3-A "selectores") | 2 modos fijos | Siempre visible, fuera del scroll | n/a | No aplica | - |
| L3 | Sidebar de Exploracion (contenedor) | ExplorationSidebarScroll (:2289) | (contenedor) | - | - | Scroll owner declarado: Exploration.World.Overview/Edit/Bestiary -> ExplorationSidebarScroll | - | COFRES_INSPECTOR_SOLO 0 FALLO salvo el pre-existente sin relacion |
| L4 | Browse: buscador + filtros + selector "Por tipo/Por contenido/Cofre a cofre" | WorldSearchBox (Views/BrowseView.xaml:444), ChestModeSelector (UniformGrid, :891) | STRUCTURAL_FINITE | Controles fijos de busqueda/filtro | Siempre visibles, fuera de cualquier scroll interno de resultados | ExplorationSidebarScroll | No aplica | - |
| L5 | Browse: resultados de NPCs/Minerales/inventario de cofres | NpcResultsList (:784), MissingNpcsScroll+MissingNpcsList (:756-757), region "Minerales" (ScrollViewer sin x:Name, :1022), ChestByChestList/lista "Por tipo" (ListBox virtualizado, :946/:990) | UNBOUNDED_COLLECTION (s3-C "resultados de busqueda", "Exploracion") | Cientos de cofres/NPCs/minerales reales por mundo | ExplorationSidebarBrowseContent fija su Height al ViewportHeight de ExplorationSidebarScroll para que el scroll real ocurra SOLO en estas listas internas (tecnica documentada); ListBox con VirtualizingPanel real | Cada lista es su propio "results owner" (comparten el alto disponible del sidebar) | Waivers activos pero INERTES (scroll-anidado-MissingNpcsScroll/NpcResultsList/7708, __waivers__/Terrakeep.json) - inertes porque reglasScroll.js analiza un solo archivo XAML y ya no ve el ScrollViewer ancestro tras la extraccion de BrowseView.xaml; la tecnica sigue siendo legitima y verificada, la limitacion es del alcance del linter, no del diseno | COFRES_INSPECTOR_SOLO/EXPLORATION_LAYOUT_SOLO 0 FALLO relevante (bitacora FASE F) |
| L6 | WorldTools: Overview / Edit / Bestiary | ExplorationSidebarWorldToolsOverview (Views/WorldToolsView.xaml:102), ...Edit (:176), ...Bestiary (:303) | STRUCTURAL_FINITE (formularios/controles de edicion del mundo) salvo listas internas que sean coleccion (ej. bestiario completo, tratado igual que L5 si aplica) | Herramientas de edicion de un unico mundo cargado | Comparten ExplorationSidebarScroll, sin ScrollViewer propio | ExplorationSidebarScroll | No aplica | - |
| L7 | ChestInspector: rejilla de slots del cofre seleccionado | SlotGridPanel AdaptiveColumns=True sin x:Name (Views/ChestInspectorView.xaml:374) | STRUCTURAL_FINITE (numero fijo de slots del cofre real, definido por el tipo de contenedor) | Igual criterio que C1/C3 | Adaptive columns puras (sustituyo a ChestInspectorColumnsConverter, FASE F); "este Inspector NUNCA tiene ScrollViewer propio" (comentario literal linea 348) | ExplorationSidebarScroll | No aplica | canario COFRES-INSPECTOR-FASEF-SINSCROLLANIDADO: 0 ScrollViewer (bitacora FASE F) |
| L8 | ChestInspector: panel Editar (Indice/Cantidad/Prefijo) | ChestInspectorItemEditTemplate (duplicado local de ItemEditTemplate, :412-425); comentario linea 115: "mismo criterio que Loadout/Vista/Defensa en Personaje, s6/s34" | STRUCTURAL_FINITE | Editor de un unico slot | Sin ScrollViewer propio desde FASE F, crece con naturalidad | ExplorationSidebarScroll | No aplica | bitacora FASE F |
| L9 | ChestInspector: ancho del panel completo | Inspector completo, Views/ChestInspectorView.xaml | (hallazgo de ancho, no de clase) | El Inspector se ve completo (Cantidad/Prefijo/Biblioteca/Guardar/Cancelar legibles), NO es recorte de contenido | - | - | Waiver activo: 721140d0-507d-432e-a34e-a2e5788c57cb (Low, OPEN, pre-existente sin relacion con esta ronda) | evidencia 12efec0b-5071-47fe-b8b8-d9fdabae3194: "219px de ancho... subutilizacion de ancho, no recorte real de contenido" |
| L10 | Mapa (barra de estado, minimapa, marcadores) | MapStatusBar (Views/WorldMapView.xaml:125), minimapa MinimapImage/MinimapViewportRect (:904/909) | STRUCTURAL_FINITE | Controles fijos de estado/minimapa | Siempre visibles (DockPanel.Dock=Bottom para la barra) | n/a | No aplica | - |
| L11 | Lienzo del mapa (pan/zoom) | WorldMapScroll (:167), WorldMapImage (:179) | UNBOUNDED_COLLECTION (s3-C ejemplo literal "Exploracion") | El mapa de un mundo real puede ser enorme (varios miles de px), navegacion por pan/zoom | Scroll bidireccional dentro de su propio viewport (el mapa, no la pagina) | El propio WorldMapScroll (results/map-specific owner, tal como anticipa s13: "Exploration.Results: results/map-specific owner") | No aplica | known-diff 65dce86f (Low, glyph decorativo de la flecha, sin relacion con contenido de usuario) |

---

## M. Servidor (Hosting)

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| M1 | Formulario de configuracion del servidor | dentro de HostingContentScroll (Views/HostingView.xaml:63) | STRUCTURAL_FINITE | Campos fijos de configuracion | MaxWidth sin HorizontalAlignment=Left (WARN-02, corregido para que la columna * crezca de verdad) | HostingContentScroll | No aplica | bitacora FASE F: "Hosting con formulario completo sin scroll"; comentario literal linea 62: "scroll legitimo (s23)" |
| M2 | Lista de servidores/instancias activas | dentro de HostingContentScroll | UNBOUNDED_COLLECTION (puede crecer con el numero de instancias reales que el usuario arranque) | Columna de "Servidores activos", vacia en el entorno de pruebas porque no hay ninguno arrancado (confirmado real, no bug) | Columna propia, visible junto al resto | HostingContentScroll (scroll owner declarado: Servidor -> HostingContentScroll) | No aplica | bitacora FASE G: "Hosting con Servidores activos visible en columna propia... 99%/72% de uso del ancho a 1920/2560" |

---

## N. Novedades (WhatsNew)

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| N1 | Selector interno Terraria / tModLoader-Calamity Mod | TabControl estilo InnerTabControl (Views/WhatsNewView.xaml:29) | STRUCTURAL_FINITE (s3-A "tabs") | 2 opciones fijas | Siempre visible | n/a | No aplica | bitacora FASE F: "Novedades con sus 2 tarjetas reales de cambios sin recortar" |
| N2 | Changelog de cada subtab | ScrollViewer SIN x:Name, dentro de cada TabItem (:34/:45) | UNBOUNDED_COLLECTION (s34 explicito: "changelogs extensos" es scroll legitimo) | Registro real de cambios de cada version, crece con cada release | UniformGrid Columns={Binding DetailCardColumns} (2 en Amplio); scroll dentro de la subpagina | Sin declarar en perfil KeepQA (ver Hallazgo 1) | No aplica | bitacora FASE G: "Novedades 99-100%/72% de uso del ancho"; captura resto-verde-faseG3-novedades-sub0-1080x700-es.png |

---

## O. Acerca de

| # | Region | x:Name / archivo:linea | Clase | Por que | Estrategia s14 | Scroll owner | Waiver | Evidencia |
|---|---|---|---|---|---|---|---|---|
| O1 | Autoria + Ajustes | dentro de AboutContentScroll (Views/AboutView.xaml:50) | STRUCTURAL_FINITE | Bloques fijos | Siempre visibles, primera seccion | AboutContentScroll | No aplica | bitacora FASE F: "AcercaDe: Autoria+Ajustes visibles, scrollbar diminuto indicando el changelog completo debajo" |
| O2 | Changelog completo de la app | dentro de AboutContentScroll; comentario literal linea 48: "...incluye el Changelog (s34, contenido..." | UNBOUNDED_COLLECTION (s34 explicito, "el registro de cambios COMPLETO de toda la app") | Registro de cambios de TODA la app, crece indefinidamente | Columna derecha del Grid (WARN-02 corregido) | AboutContentScroll (scroll owner declarado: AcercaDe -> AboutContentScroll) | No aplica | bitacora FASE G: "AcercaDe... 99%/72%... el Changelog SI esta dentro del viewport a offset=0"; captura resto-verde-faseG3-acercade-1080x700-es.png |

---

## Contraste con el perfil responsive de KeepQA

El perfil "__perfiles-responsive__/Terrakeep.json" declara 6 contenedoresCriticos y 15
entradas de scrollOwnership. Verificado contra esta clasificacion:

### Coherente (sin incidencias)
- Los 6 contenedoresCriticos (EquipamientoSelectorVista, CajaMascotasTintes,
  EquipamientoCabecera, EditarTarjeta, CategoriasPrincipales, AlmacenesCabecera)
  coinciden exactamente con regiones STRUCTURAL_FINITE de esta clasificacion (B3/B4/B5/C2/C4/D1)
  citadas por el propio encargo como a riesgo de "un MinHeight/MaxHeight literal volveria a
  esconderlas" - ninguna region STRUCTURAL_FINITE de riesgo similar queda fuera de esa lista.
- Las 15 entradas de scrollOwnership coinciden con las regiones UNBOUNDED_COLLECTION/subpagina de
  Objetos, Buffs, familia CATALOGO, Comparar, Inicio, Guia, Servidor y Acerca de de esta
  clasificacion (B2, C1, C3, E1, D4/D6/D8, H2, I2, K2, M2, O2) - ninguna region UNBOUNDED_COLLECTION
  de esas 8 areas queda sin scrollOwner declarado.
- Ninguna region STRUCTURAL_FINITE de esta clasificacion vive oculta dentro de un scroll local sin
  waiver (contrato s13 cumplido en las filas anteriores salvo lo indicado en el Hallazgo 1).

### Incoherencia real encontrada (Hallazgo 1, ver abajo)
Las regiones F1/F2/G1/G2/G3/J3/N2 (Apariencia x2 columnas, Puntos de aparicion, Desbloqueos,
Version, Builds, Novedades) tienen su propio ScrollViewer de pagina real, se comportan hoy
como scroll owner unico correcto (medido 0 FALLO por los canarios PERSONAJE_RESPONSIVE_SOLO/
RESTO_RESPONSIVE_SOLO), pero ninguna esta declarada en scrollOwnership del perfil KeepQA
y ninguna tiene x:Name en el XAML real. Detalle en el Hallazgo 1.

---

## Hallazgos

### Hallazgo 1 - Severidad: Medium

6 scroll owners reales de pagina sin x:Name ni declaracion en scrollOwnership del perfil
KeepQA.

Regiones afectadas: Personaje.Apariencia (2 ScrollViewer independientes, uno por columna:
MainWindow.xaml:1630 y :1674), Personaje.SpawnPoints (Views/SpawnpointsView.xaml:67),
Personaje.Desbloqueos (Views/UnlocksView.xaml:45), Personaje.Version
(Views/VersionView.xaml:42), Builds (Views/BuildsView.xaml:51 y :56, dos subtabs) y
Novedades (Views/WhatsNewView.xaml:34 y :45, dos subtabs).

Por que es un hallazgo real y no solo un tecnicismo: el propio encargo (s13) exige "Para
cada contexto declarar explicitamente scrollOwnerVertical/scrollOwnerHorizontal" y "Un scroll
interior adicional requiere reason/semanticRegion/waiver/evidence" - el mecanismo que hace
cumplir ese contrato en KeepQA es contratoScrollOwnership.js (check14), que solo puede
evaluar "no hay dos scroll owners compitiendo en el mismo contexto" para los contextos
REGISTRADOS. Estas 6 regiones (que suman 9 ScrollViewer reales) quedan fuera de ese registro:
si en el futuro alguien introdujera un segundo ScrollViewer anidado dentro de cualquiera de
ellas (por ejemplo un scroll local en la columna editable de Apariencia, exactamente el patron
que s13/s26-A prohiben), check14 no lo detectaria porque el contexto no existe en el perfil.
Es un hueco real en la red de seguridad automatizada que sostiene el propio Supersession Gate del
requirement, no un defecto visual activo hoy (los 6 pasan con 0 FALLO en los canarios reales,
ver filas F1/F2/G1/G2/G3/J3/N2 de esta clasificacion).

No se corrige en esta ronda (encargo explicito: auditoria de solo lectura, no tocar codigo
de produccion ni el perfil de KeepQA). Remediacion recomendada (para una ronda futura,
FUERA de este alcance): anadir x:Name real a los 9 ScrollViewer (mismo patron ya usado con
HomeContentScroll/GuideContentScroll/HostingContentScroll/AboutContentScroll,
"cambio de solo nombre, sin tocar layout") y registrar Personaje.Apariencia.Preview,
Personaje.Apariencia.Editable, Personaje.SpawnPoints, Personaje.Desbloqueos,
Personaje.Version, Builds.Vanilla, Builds.Calamity, Novedades.Terraria,
Novedades.CalamityMod en scrollOwnership.

Evidencia de que el codigo real detras del hallazgo se verifico linea a linea (no
asumido): Grep de "x:Name=" y de "<ScrollViewer" contra MainWindow.xaml y las 4
vistas citadas (comando reproducible: buscar "<ScrollViewer" en esos 5 archivos y
cruzar contra scrollOwnership de Terrakeep.json).

### Hallazgo 2 - Severidad: Low (ya documentado, sin cambio de estado)

Los 3 known-differences 65dce86f (Exploracion, flecha decorativa 2,4x0px, glyph sin contenido
de usuario), 3ec18c7c (Buffs, Track.DecreaseRepeatButton/IncreaseRepeatButton con
Opacity=0 por diseno en Theme.xaml:264-269, invisible a proposito) y 721140d0
(ChestInspector, 219px de ancho vs 230-245px de Browse, sin recorte real de contenido) siguen
OPEN en el requirement, con severidad Low y sin relacion con la clasificacion de regiones de
esta ronda. Se citan aqui (fila L9 para 721140d0, fila L11 para 65dce86f, area E para el
contexto de Buffs de 3ec18c7c) por completitud, sin reabrir ni tocar su estado.

### Sin hallazgos adicionales de severidad Medium o superior

No se encontro ninguna region STRUCTURAL_FINITE oculta detras de un scroll local sin waiver, ni
ninguna region UNBOUNDED_COLLECTION sin scroll owner identificable (aunque 6 de ellas carezcan
de x:Name/registro formal, ver Hallazgo 1), ni ningun caso de "dos scroll owners compitiendo en
el mismo contexto" (s26-H) en el codigo real auditado.

---

## Resumen numerico

| Clase | Regiones (filas de esta clasificacion) |
|---|---|
| STRUCTURAL_FINITE | 33 |
| FINITE_PAGEABLE | 2 (B6 "Armadura/Vanidad/Tintes"; J3 "subtabs de Builds") |
| UNBOUNDED_COLLECTION | 12 (D4, D6, D8, H2, I2, K2, L5, L11, M2, N2, O2 + Exploration.World.* ya cubierto por L3/scrollOwnership) |
| Total de regiones nombradas clasificadas | 47 |
| Scroll owners declarados en el perfil KeepQA, verificados coherentes | 15/15 |
| Scroll owners reales SIN declarar (Hallazgo 1) | 6 contextos / 9 ScrollViewer |
| Hallazgos Medium | 1 (Hallazgo 1) |
| Hallazgos Low (ya conocidos, sin cambio) | 3 (65dce86f, 3ec18c7c, 721140d0) |
