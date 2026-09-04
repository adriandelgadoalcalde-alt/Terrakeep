# Auditoría de redimensionado: qué se pierde de vista al cambiar el tamaño de la ventana, pantalla por pantalla

Repaso final de toda la aplicación antes de una entrega real. Recorre **las 20 pantallas reales**
de Terrakeep a **14 tamaños de ventana distintos**, desde el mínimo obligado (1080x700) hasta 4K
(3840x2160), y cataloga con medición real todo elemento que desaparezca, se recorte, deje de ser
alcanzable o deje de existir.

**Encargo del usuario (verbatim, 4-sep-2026)**:

> "ahora pide un plan a opus super complejo de el comportamiento de toda la app de ventana por
> ventana que mire diferentes escala de renderització de la mas pequeña a la mas grande y que
> mire como va encajando todo y que haga un plan exhaustivo de como corregir cualquier cosa que
> desaparezca cuando redimensionas la ventana del programa de cada pestaña/apartado para que
> todo pueda mantenerse a la vista que no se pierdan elementos importantes y que quede todo
> ajustado lo mejor possible es un repaso para poder entregar una version final hay mucho en
> juego"

**Disciplina de este documento** (la misma de `ESPEC-ui-exploracion.md`):

- **Parte I (secciones 1-4) = HECHOS.** Todo hallazgo lleva su medición real (rectángulo y
  píxeles perdidos, medidos en la ventana real) y su fichero + línea causantes. Nada de memoria
  ni de impresión visual.
- **Parte II (secciones 5-6) = PROPUESTA.** Correcciones concretas y plan de verificación.
  Criterio mío, marcado como tal.
- **Parte III (sección 7) = HUECOS.** Lo que deliberadamente queda fuera, y por qué.

**Este documento no toca ni una línea de código de producción.** Todo lo medido se hizo con un
arnés desechable fuera del repo (ver §1.3); `TerrasavrNative.App.Tests/Program.cs` queda intacto,
y §6 dice exactamente qué añadirle cuando alguien ejecute el plan.

---

# RESUMEN EJECUTIVO

Tres hallazgos **hacen desaparecer del todo** elementos reales, y los tres son permanentes o se
disparan al **agrandar** la ventana, no al encogerla:

| # | Qué se pierde | Cuándo | Severidad |
|---|---|---|---|
| **H-01** | Los botones **"Guardar conjunto…", "Cargar…", "Añadir…"** de Inventario **y** de Almacenes | a partir de **1500px** de ancho (`IsStorageExpanded`) | desaparece del todo |
| **H-02** | La 5ª píldora de categoría de Exploración (**"Objetos (N)"**) y varios botones de la barra lateral | **a cualquier tamaño**, incluido 4K | desaparece del todo |
| **H-03** | Los **nombres de objeto de Builds** (hasta el 100% del texto) | **a cualquier tamaño** | desaparece del todo |

Y seis más que recortan sin llegar a hacer desaparecer (franja vital de la cabecera, insignias de
las tarjetas de Inicio, árbol de categorías, texto del preview de Apariencia, contador
"Inventario (N/M)", pastillas de pestaña interna), más un problema de aprovechamiento en
ventanas grandes (**66-73% del ancho sin usar a 4K**).

**Además, y es lo primero que hay que arreglar**: el redimensionado de ventana del arnés
permanente (`TerrasavrNative.App.Tests`) **no funciona en esta máquina** y falla en silencio -
todas las peticiones de `window.Width` acaban en 1080. Las comprobaciones `E2-UMBRAL`,
`A4-1350`/`A4-EXPANDIDO`, `H5-09-AMPLIO` llevan midiendo el tamaño equivocado. Causa exacta y
remedio real en §1.1-1.2.

---

# PARTE I — HECHOS

## 1. Metodología real

### 1.1 El bloqueo: por qué `window.Width = 1550` daba `ActualWidth = 1080`

El síntoma que abre este encargo estaba bien observado y **no es un bug de Terrakeep**. Cadena de
causas, medida entera:

1. **La sesión es RDP.** `query session` devuelve `>rdp-tcp#0  adrian  1  Activo`; hay un proceso
   `rdpclip` vivo.
2. **El adaptador remoto reporta 1440x2992 píxeles físicos** (`Win32_VideoController`,
   `Microsoft Remote Display Adapter`).
3. **El escalado del sistema es 250%.** Medido con
   `HwndSource.CompositionTarget.TransformToDevice`: `M11 = M22 = 2,5` (= 240 ppp). El escritorio
   lógico que ve WPF es por tanto `1440/2,5 = 576` x `2992/2,5 = 1196,8` DIP -
   `SystemParameters.PrimaryScreenWidth = 576`, confirmado.
4. **Windows limita el tamaño de cualquier ventana a `MINMAXINFO.ptMaxTrackSize`**, que por
   omisión vale `SM_CXMAXTRACK` x `SM_CYMAXTRACK`. Medido en esta máquina: **1476 x 3028 píxeles
   físicos** (= 590,4 x 1211,2 DIP).
5. **WPF rellena `ptMinTrackSize` a partir de `Window.MinWidth`** (1080 DIP = **2700 físicos**).
6. `ptMinTrackSize.x` (2700) es **mayor** que `ptMaxTrackSize.x` (1476). Windows resuelve el
   conflicto aplicando el mínimo **después** del máximo, así que el ancho real queda clavado en
   2700 físicos = **exactamente 1080 DIP, sea cual sea el ancho pedido**.

Eso explica el detalle que parecía más raro (el tope en `MinWidth` y no en el ancho de pantalla):
no es "no cabe en pantalla", es el mínimo ganándole al máximo dentro del mismo mensaje.

Sonda real (proyecto desechable, ventana WPF pelada con `MinWidth=1080`):

```
SM_CXMAXTRACK=1476 SM_CYMAXTRACK=3028
SM_CXSCREEN=1440   SM_CYSCREEN=2992
DPI: escala X=2,5 Y=2,5 (=> 240 ppp)
--- SIN hook ---
  pedido Width=1180 -> ActualWidth=1080 | HWND fisico=2700x2150
  pedido Width=1450 -> ActualWidth=1080 | HWND fisico=2700x2150
  pedido Width=1550 -> ActualWidth=1080 | HWND fisico=2700x2150
  pedido Width=2200 -> ActualWidth=1080 | HWND fisico=2700x2150
```

Nótese que **la altura sí obedecía** (2150 físicos = 860 DIP, por debajo de `SM_CYMAXTRACK`). El
bloqueo era sólo del eje horizontal - por eso los umbrales de `HeightClass` sí se han podido
probar de verdad hasta hoy y los de `SizeClass` no.

**Ninguna guarda del propio código de la app participa en esto**: `MainWindow.xaml.cs` sólo tiene
un `SizeChanged += (_, e) => _viewModel.UpdateSizeClass(e.NewSize.Width, e.NewSize.Height)`
(línea 58) y no consulta `SystemParameters.WorkArea` en ningún sitio; las únicas apariciones de
`SystemParameters` en ese fichero son `MinimumHorizontalDragDistance`/`DoubleClickTime` (líneas
509-510, 543, 581-582, 693-694, 714-715), ajenas al tamaño de la ventana.

### 1.2 Cómo se resolvió: hook `WM_GETMINMAXINFO`

Se descartaron dos alternativas antes de elegir:

- **Cambiar la resolución/escalado de la sesión.** Es posible (el escalado vive en el registro y
  el modo de vídeo remoto se negocia al reconectar), pero exige **cerrar y reabrir la sesión RDP
  del usuario**, que es justo lo que no se puede hacer en una sesión desatendida - y además
  cambiaría el entorno bajo los pies de cualquier otro trabajo en curso. Rechazada por invasiva,
  no por imposible.
- **Medir sin ventana real** (`Measure`/`Arrange` manuales sobre un `Grid` de prueba). Es válida y
  `CLAUDE.md` ya la documenta, pero pierde justo lo que aquí importa: el recorte real que WPF
  aplica en el `Arrange` dentro de una ventana viva, y los `BoundingRectangle` reales de UI
  Automation.

**Camino elegido**: interceptar `WM_GETMINMAXINFO` (0x0024) desde el propio arnés vía
`HwndSource.AddHook` y subir `ptMaxTrackSize`/`ptMaxSize` a 32000x32000. El hook vive **fuera del
código de producción** (lo instala el arnés sobre la `MainWindow` ya creada), así que la app real
no cambia de comportamiento.

Resultado real, misma sonda:

```
--- CON hook WM_GETMINMAXINFO ---
  pedido Width=1180 -> ActualWidth=1180 | HWND fisico=2950x2150
  pedido Width=1450 -> ActualWidth=1450 | HWND fisico=3625x2150
  pedido Width=1550 -> ActualWidth=1550 | HWND fisico=3875x2150
  pedido Width=2200 -> ActualWidth=2200 | HWND fisico=5500x2150
  pedido Width=3000 -> ActualWidth=3000 | HWND fisico=7500x2150
```

**Toda la auditoría de este documento se hizo con redimensionado REAL de la ventana del sistema
operativo** (`window.ActualWidth`/`ActualHeight` confirmados en cada parada, con un aviso
automático si el tamaño real no coincidía con el pedido; ese aviso no se disparó ni una vez). No
se usó la vía alternativa de `Measure`/`Arrange` sin ventana en ningún hallazgo.

**Aviso honesto sobre lo que sí queda condicionado por el entorno**: la ventana es más grande que
el monitor físico, así que **la parte que se sale no se está pintando de verdad en pantalla**. Eso
no afecta a lo medido - WPF hace el `Measure`/`Arrange` completo igual, y tanto los rectángulos
del árbol visual como los `BoundingRectangle` de UI Automation se calculan del layout, no de lo
que llega al monitor (comprobado: `IsOffscreen=False` en los 14 botones sondeados a los 5 tamaños,
incluso muy fuera del monitor real). Lo que **no** se puede juzgar así es cualquier cosa que
dependa del píxel pintado: antialiasing, subpíxel, y las capturas de pantalla - que en este
entorno ya eran poco fiables de partida (`CLAUDE.md`). **Ningún hallazgo de este documento se
apoya en una captura.**

### 1.3 Matriz real de tamaños

14 paradas, elegidas para cruzar los tres umbrales ya existentes (`NormalMinWidth=1300`,
`AmplioMinWidth=1500`, `AltoMinHeight=900`) por ambos lados, y para explorar los dos extremos:

| Etiqueta | Tamaño | Por qué |
|---|---|---|
| `MINIMO` | 1080x700 | el `MinWidth`/`MinHeight` obligado (`MainWindow.xaml:12`) |
| `MIN-ANCHO/ALTO` | 1080x900 | aísla `HeightClass.Alto` con el ancho mínimo |
| `ARRANQUE` | 1180x860 | el tamaño inicial real (`MainWindow.xaml:12`) |
| `JUSTO-BAJO-NORMAL` | 1299x860 | 1px por debajo de `NormalMinWidth` |
| `NORMAL` | 1300x860 | justo en `NormalMinWidth` |
| `NORMAL-ALTO` | 1450x860 | mitad de la banda Normal |
| `JUSTO-BAJO-AMPLIO` | 1499x900 | 1px por debajo de `AmplioMinWidth` |
| `AMPLIO` | 1500x900 | justo en `AmplioMinWidth` |
| `AMPLIO-COMODO` | 1600x1000 | Amplio con holgura |
| `FULLHD` | 1920x1080 | el tamaño de pantalla completa más común |
| `QHD` | 2560x1440 | más allá de todo lo contemplado hoy |
| `4K` | 3840x2160 | techo real |
| `ESTRECHA-MUY-ALTA` | 1080x1600 | ejes descorrelados: Compacto + Alto |
| `MUY-ANCHA-BAJA` | 2560x700 | ejes descorrelados: Amplio + Bajo |

Y **una bisección de 10 en 10px entre 1080 y 1700** para el único umbral que resultó estar mal
puesto (§4.4).

En cada parada se recorrieron **las 20 pantallas reales** (§2), y las 5 que tienen Librería se
recorrieron **dos veces** (plegada y desplegada), porque la Librería cambia el reparto vertical
entero: **288 combinaciones (pantalla x tamaño) medidas**.

### 1.4 Instrumentación: cómo se detecta "algo se pierde" sin mirar píxeles

Dos vías, ambas objetivas:

**(a) Recorrido real del árbol visual, en proceso.** Para cada elemento visible de interés
(`Button`, `ToggleButton`, `RadioButton`, `CheckBox`, `TextBox`, `ComboBox`, `Slider`, `TabItem`,
`TextBlock`, `Image`, `ProgressBar`, `Expander`) se calcula su rectángulo real en coordenadas de
ventana con `TransformToAncestor(window).TransformBounds(...)` y se compara con el **área
realmente visible heredada**: la intersección acumulada de la ventana, de todo ancestro con
`ClipToBounds`, del viewport de todo `ScrollViewer` y **del recorte que WPF aplica de verdad**.

Ese último punto necesitó un experimento controlado antes de fiarse de nada, porque el mecanismo
real no es el que dice el comentario de `MainWindow.xaml:3006-3015` ("Border con CornerRadius
recorta silenciosamente"). Se montó un `Border` de 100px con un hijo de 300px y se leyó el píxel
en x=200 de un `RenderTargetBitmap`:

```
EXPERIMENTO-BORDER: pixel a x=200 (fuera del Border de 100px con CornerRadius=8) -> RECORTADO
EXPERIMENTO-BORDER: VisualTreeHelper.GetClip(hijo)=0;0;100;20   GetClip(border)=null
EXPERIMENTO-BORDER (sin CornerRadius): RECORTADO
```

Dos conclusiones reales: (1) el recorte ocurre **también sin `CornerRadius`** - lo que recorta es
el **recorte de layout de WPF** cuando un hijo no cabe en el hueco que se le arregla, no el
redondeo; (2) ese recorte **sí se puede leer**, y se lee **en el hijo recortado**, con
`VisualTreeHelper.GetClip`. Ese es el detector definitivo, y es el que se usa: cuando
`GetClip(elemento)` devuelve un rectángulo menor que su propio tamaño, ese elemento **está siendo
recortado de verdad ahora mismo**, con la cifra exacta de píxeles perdidos.

El solape se calcula **por eje por separado** (nunca `Rect.Intersect` a secas): un elemento
desplazado fuera de la vista en Y da intersección vacía y falsearía una pérdida total también en
X. Y se ignora la pérdida en un eje si algún `ScrollViewer` ancestro puede desplazarse en él
(estar fuera de vista y ser alcanzable con la rueda **no** es un defecto).

Detectores activos: `RECORTE` (pérdida > 3px en algún eje), `CERO` (visible pero con 0 de ancho o
alto), `SIN-SCROLL` (un `ScrollViewer` con `ExtentWidth/Height` mayor que su viewport y ese eje
`Disabled`, es decir contenido inalcanzable).

**(b) UI Automation real.** `AutomationElement.FromHandle(hwnd)` + `FindAll` por `ControlType.Button`,
leyendo `Current.BoundingRectangle` (coordenadas de pantalla) e `IsOffscreen`, y comparándolo con
el rectángulo real de la ventana. Se aplicó a los 14 botones críticos de la cabecera global, de
la cabecera de Inventario/Almacenes y de la barra de Exploración, a 5 tamaños. Esta vía es la que
detecta el caso que el árbol visual **no** puede ver: un botón que **ya no existe** porque su rama
de XAML dejó de realizarse (§4.1).

### 1.5 Dos falsos positivos propios, encontrados y eliminados

Se documentan a propósito, porque son justo el tipo de "hallazgo" que habría contaminado el
informe entero:

1. **El arnés reasignaba el marco de recorte heredado dentro del bucle de hermanos** (`frame = new
   Frame{...}` sobre el parámetro del método): en cuanto UN hijo tenía recorte, TODOS los
   hermanos siguientes lo heredaban. Producía ~2.700 falsos "pierde el 100%". Se localizó
   volcando la cadena de ancestros completa de tres elementos concretos y viendo que los números
   no cuadraban con sus rectángulos reales. Corregido usando una variable local.
2. **La heurística "`Border` con `CornerRadius` recorta"** (escrita antes del experimento de §1.4)
   marcaba como recortado cualquier contenido dentro de una tarjeta redondeada. Eliminada del
   todo: el recorte real ya lo da `GetClip`, sin heurística.

Tras las dos correcciones el informe pasó de 3.900 líneas de "problema" a **1.234**, y de esas la
inmensa mayoría son repeticiones del mismo puñado de causas en pantallas distintas.

### 1.6 Datos reales usados

- **Personaje**: copia temporal de `Eldelgas.plr` + `Eldelgas.tplr` reales de esta máquina (nunca
  el fichero original). Vida 600/500, maná 200/200, 50/50 objetos en el inventario, 3 objetos de
  Calamity detectados - un personaje **lleno**, que es el caso que aprieta.
- **Mundo**: `adriandres.wld` real (18 NPCs), y `Afueras_de_Larvas_de_gusano.wld` para la prueba
  del título largo (§4.10).
- **Hallazgo colateral, fuera de alcance pero real**: `adrian.plr` **no se puede cargar** -
  `Error al cargar: bufferSize ('8960') must be greater than or equal to '179200'`. No es un
  problema de layout y no se investiga aquí, pero conviene no perderlo de vista antes de una
  entrega.

## 2. Inventario real de pantallas

Árbol completo, leído del XAML (no de memoria). Los índices raíz siguen el enum real
`AppTab` (`MainViewModel.cs:15`), los internos `PersonajeInnerTab`.

| # | Pantalla | Líneas de `MainWindow.xaml` | Reacciona hoy a | No reacciona a nada |
|---|---|---|---|---|
| 0 | **Cabecera global** (visible en las 6 pestañas) | 1315-1500 | `IsVitalsStripExpanded` (1428) | los 6 botones de acción (1334-1358), la identidad (1360-1398), el popup "¿Dónde lo tengo?" (`Width="400"` fijo, 1455) |
| 1 | **Inicio** | 1508-1662 | `InicioContentMaxWidth` (1513) | tarjeta de personaje `Width="240"` (1092), 5 tarjetas de navegación `Width="270"` (1614-1658) |
| 2 | **Personaje > Objetos > Equipamiento** | 1756-1969 | `IsEquipmentExpanded` (1918/1928), `IsLibraryVisible`+`LibraryRowMaxHeight` (1728-1730) | `SlotRowHost` columnas `Auto/*/Auto` con `MinWidth` 140/200 (1793-1795), panel Editar `Width="300"` (1740) |
| 3 | **Personaje > Objetos > Inventario** | 1977-2076 | `IsStorageExpanded` (1981/2024) | cabecera de 6 botones (1990-2015) |
| 4 | **Personaje > Objetos > Almacenes** | 2082-2124 | `IsStorageExpanded` (2082, se **oculta** en Amplio) | cabecera de 5 botones + píldoras (2086-2120) |
| 5 | **Personaje > Objetos > Librería** (fila inferior) | 2134-2285 | `IsLibraryVisible`, `LibraryRowMaxHeight` | árbol `Width="210"` (2177), buscador `MaxWidth="360"` (2204), tarjetas raíz `Width="200"` (2251) |
| 6 | **Personaje > Buffs** | 2288-2483 | `IsBuffLibraryVisible`, `LibraryRowMaxHeight` (2318-2320) | panel Editar `Width="300"` (2326), cabecera de 4 botones (2342-2354), árbol `Width="210"` (2409) |
| 7 | **Personaje > Investigación** | 2484-2679 | nada | árbol `Width="210"` (2524), buscador `MaxWidth="360"` (2546), tarjetas raíz `Width="200"` (2583) |
| 8 | **Personaje > Apariencia** | 2680-2897 | `AppearanceContentMaxWidth` (2716) | columna 0 `Auto` con `Width="240"` fijos y **sin `ScrollViewer`** (2695-2709), pickers `MaxHeight` 240/360 (2755, 2792), swatch `Width="150"` (2824) |
| 9 | **Personaje > Spawn Points** | 2898-2977 | nada | columnas 220/80/80/70 fijas con `SharedSizeGroup` (2916-2922, 2939-2945) |
| 10 | **Personaje > Desbloqueos** | 2978-3101 | `DetailContentMaxWidth` (2993) | 4 tarjetas `Width="280"` (3017, 3033, 3048, 3078), etiquetas `MaxWidth="210"` |
| 11 | **Personaje > Versión** | 3102-3169 | `DetailContentMaxWidth` (3107) | grupos `Width="200"` (3142) |
| 12 | **Builds > Vanilla** | 3193-3197 | nada | clase `Width="220"` (602), `WrapPanel ItemWidth="248"` (633) |
| 13 | **Builds > Calamity Mod** | 3198-3202 | nada | igual que 12 |
| 14 | **Novedades > Terraria** | 3224-3234 | `DetailContentMaxWidth` + `DetailCardColumns` (3226, 3229) | - |
| 15 | **Novedades > tModLoader/Calamity** | 3235-3245 | igual que 14 (3237, 3240) | - |
| 16-20 | **Exploración** (5 categorías: Todo / NPCs / Cofres / Minerales / Objetos) | 3251-3869 | nada | barra superior `StackPanel Horizontal` (3253-3283), columna lateral `Auto MinWidth=260 MaxWidth=380` (3319), lista de resultados `MaxHeight="240"` (3648) |
| 21 | **Acerca de** | 3876-3960 | `DetailContentMaxWidth` + `DetailCardColumns` (3878, 3956) | texto corrido `MaxWidth="680"` (3883, 3890, 3898, 3949) - **a propósito**, por legibilidad de línea |

**Corrección a la suposición de partida del encargo**: `ObjetosSubTabIndex == 2` **no** es el panel
"Editar". El `TabControl` interno de Objetos tiene exactamente 3 pestañas -
Equipamiento(0)/Inventario(1)/**Almacenes(2)** (`MainWindow.xaml:1756`, `1977`, `2082`) - y la
línea real de `OnSizeClassChanged` (`MainViewModel.cs:487`) mueve la selección **fuera de
Almacenes** cuando esa pestaña se oculta en Amplio. El panel "Editar" es una columna fija de
`Width="300"` siempre presente (líneas 1740 y 2326), no una sub-pestaña.

**Balance del sistema de tamaños existente**: de las **17 áreas** de la tabla, **12 reaccionan a
algún breakpoint** y **5 no reaccionan a nada** - Investigación, Spawn Points, las dos de Builds y
**Exploración entera** (las 5 categorías). Entre esas cinco están las dos pantallas más complejas
de la app, y son justo las que aportan dos de los tres hallazgos de severidad máxima.

## 3. Lo que salió BIEN (para no tocarlo)

Antes de los defectos, lo que la matriz confirma que ya está resuelto - conviene que quien ejecute
el plan no lo "arregle" sin querer:

- **`CERO`: 0 casos en 288 combinaciones.** Ningún panel colapsa a tamaño nulo en ningún extremo.
- **`SIN-SCROLL`: 0 casos.** No hay ni un solo `ScrollViewer` con contenido inalcanzable por tener
  su eje deshabilitado. Los `HorizontalScrollBarVisibility="Disabled"` de las rejillas de slots
  (`MainWindow.xaml:528`, `868`, `2270`, `2466`) son correctos: `SlotGridPanel` se autolimita en
  ancho y sólo desborda en vertical, donde sí hay scroll.
- **Ningún botón se sale de la ventana.** Los 14 botones sondeados por UI Automation dan `DENTRO`
  a los 5 tamaños. La cabecera global no empuja "Guardar" fuera de pantalla al encoger: el `Grid`
  de 3 columnas `Auto/*/Auto` (1324-1328) **aprieta la columna central** (la franja vital) en vez
  de desbordar. Es el comportamiento correcto; el precio lo paga la franja (§4.4).
- **La rejilla de slots (`SlotGridPanel`) se comporta a todas las escalas.** De 1080x700 a
  3840x2160, sin recortes ni desbordes en Equipamiento, Inventario, Almacenes, Monturas, Monedas,
  Munición ni Buffs. Los tres modos de `ReferenceColumns`/`ReferenceWidth` funcionan.
- **La limitación ya documentada de `H3-18`** (a 1080x700 con la Librería desplegada la 5ª fila
  del Inventario no cabe entera) **sigue siendo exactamente eso**: el `ScrollViewer` de seguridad
  la mantiene alcanzable, `SIN-SCROLL` no se disparó. No es una regresión nueva.
- **La barra superior de Exploración aguanta el nombre de mundo más largo real de esta máquina**
  ("Afueras de Larvas de gusano"): a 1080px, "Ajustar a la ventana" acaba en x=980 de 1080. Ver
  §4.10 para el riesgo residual.

## 4. Hallazgos reales, pantalla por pantalla

### 4.1 H-01 — Tres acciones de Inventario **y** de Almacenes desaparecen al AGRANDAR la ventana

**Severidad: desaparece del todo. La más grave del informe.**

Medido por UI Automation (los botones se buscan por nombre en todo el árbol de automatización de
la ventana; "AUSENTE" significa que **no existen**, no que estén tapados):

```
  1080px SizeClass=Compacto IsStorageExpanded=False
      'Guardar conjunto...': presente   'Cargar...': presente   'Añadir...': presente
  1300px SizeClass=Normal   IsStorageExpanded=False   -> los 3 presentes
  1450px SizeClass=Normal   IsStorageExpanded=False   -> los 3 presentes
  1499px SizeClass=Normal   IsStorageExpanded=False   -> los 3 presentes
  1500px SizeClass=Amplio   IsStorageExpanded=True
      'Guardar conjunto...': AUSENTE   'Cargar...': AUSENTE   'Añadir...': AUSENTE
  1600px  -> AUSENTE x3
  1920px  -> AUSENTE x3
```

Y lo mismo desde la pestaña Almacenes (recuento de instancias reales en el árbol):

```
  1450px IsStorageExpanded=False subtab=2 -> 'Guardar conjunto...'=1 'Cargar...'=1 'Añadir...'=1 'Ordenar'=1
  1500px IsStorageExpanded=True  subtab=1 -> 'Guardar conjunto...'=0 'Cargar...'=0 'Añadir...'=0 'Ordenar'=2
  1920px IsStorageExpanded=True  subtab=1 -> 'Guardar conjunto...'=0 'Cargar...'=0 'Añadir...'=0 'Ordenar'=2
```

**Causa real.** Hay **dos ramas de XAML distintas** para lo mismo, y no llevan los mismos botones:

- Rama compacta (`Visibility="{Binding IsStorageExpanded, Converter=InverseBoolToVis}"`,
  **líneas 1979-2018**): cabecera con **6** botones - `Guardar conjunto...` (1997),
  `Cargar...` (2000), `Añadir...` (2003), `Mover todo al almacén` (2006), `Ordenar` (2009),
  `Vaciar contenedor` (2011).
- Rama Amplio (`Visibility="{Binding IsStorageExpanded, Converter=BoolToVis}"`,
  **líneas 2024-2074**): la mitad Inventario lleva sólo **3** - `Mover todo al almacén` (2034),
  `Ordenar` (2037), `Vaciar contenedor` (2039). La mitad Almacén lleva **2** - `Ordenar` (2053),
  `Vaciar contenedor` (2055).
- Y la pestaña "Almacenes" entera, que es donde viven los 3 botones de conjunto del almacén
  (**2091-2099**), se **oculta** en Amplio (`Visibility` en la línea **2082**).

Es decir: la vista lado a lado de A-4, que se añadió para **ganar** capacidad, se escribió como
una copia a mano de la cabecera (la duplicación que `H4-10` ya señalaba) y en la copia se quedaron
fuera los tres botones de H5-03. El usuario que abre Terrakeep a pantalla completa en un monitor
de 1920 **no tiene ninguna forma de guardar ni cargar un conjunto de objetos**, ni del inventario
ni de ningún almacén; y no hay nada en pantalla que lo explique - la única salida es encoger la
ventana por debajo de 1500px, que nadie va a deducir.

### 4.2 H-02 — La barra lateral de Exploración se recorta 66px **a cualquier tamaño**, incluido 4K

**Severidad: desaparece del todo (una de las 5 categorías de búsqueda queda inalcanzable).**

Cadena real medida a 3840x2160 (recorte leído con `GetClip`):

```
  DockPanel  rect=3433,154  408x1932   clip=0;0;380;1932    <-- 28px cortados
  ...
  WrapPanel  rect=3433,181  408x34
  RadioButton  rect=3751,181  84x28    <-- su borde derecho cae en 3835; el recorte acaba en 3813
```

Y con las otras categorías activas (que piden más ancho), el recorte medido sube a **66px**:

```
  Exploracion/Todo      RECORTE RadioButton 'Objetos (200)'  pierde 60x0px de 97x28
  Exploracion/Todo      RECORTE TextBox '[campo] '           pierde 66x0px de 446x36
  Exploracion/Cofres    RECORTE RadioButton 'Por lo que contienen' pierde 66x0px de 220x25
  Exploracion/Minerales RECORTE Button 'Quitar marcas'       pierde 66x0px de 220x33
  Exploracion/Objetos   RECORTE RadioButton 'Líquidos'       pierde 66x0px de 146x25
  Exploracion/NPCs      RECORTE ToggleButton 'NPCs que faltan' pierde 64x0px de 442x19
```

Estas seis líneas **aparecen idénticas en las 14 paradas de la matriz**, de 1080x700 a 3840x2160.
No es un problema de ventana pequeña: es permanente.

**Causa real.** `MainWindow.xaml:3319`:

```xml
<ColumnDefinition Width="Auto" MinWidth="260" MaxWidth="380" />
```

Con `Width="Auto"`, WPF mide el contenido de la columna **con ancho infinito**. En ancho infinito
el `WrapPanel` de las 5 píldoras de categoría (**3566-3598**) **no envuelve**: coloca las cinco en
una sola fila y pide 408px. Otros hijos piden aún más (los `UniformGrid Columns="2"` de Cofres,
línea 3788, y de Minerales, 3814; el `Expander` de NPCs, 3719) y el máximo llega a 446px. Sólo
**después** de esa medición se aplica el `MaxWidth="380"` de la columna, y WPF corta la diferencia
con un recorte de layout, sin que nada envuelva ni se reorganice.

Consecuencia real, y es lo que la hace grave: **"Objetos (N)", la quinta de las cinco categorías
de búsqueda del mundo, pierde 60 de sus 97px** - queda un muñón de ~37px con el texto cortado.
El buscador de tiles/paredes/líquidos entero (§9.3-E de `ESPEC-ui-exploracion.md`, la parte más
nueva de la pestaña) es prácticamente inalcanzable con el ratón, **a todos los tamaños de
ventana**. Lo mismo le pasa al botón "Quitar marcas" de Minerales y al selector "Por lo que
contienen" de Cofres.

Esto no se detectó al construir la barra lateral porque **el arnés nunca pudo ensanchar la ventana
de verdad** (§1.1): a 1080px todo el mundo asumió que el recorte era falta de sitio.

### 4.3 H-03 — Builds: los nombres de objeto se cortan, hasta el 100%

**Severidad: desaparece del todo (en anchos medios el nombre entero es invisible).**

Cadena real a 3840x2160:

```
  Border      rect=894,1829  214x41
  StackPanel  rect=902,1834  223x31   clip=0;0;198;31.26   <-- 25px cortados
  StackPanel  rect=933,1834  192x31   clip=null
  TextBlock   rect=933,1834  192x17   "Báculo de dragón de polvo estelar"
```

Medidas por tamaño (mismo objeto):

| Tamaño | Pérdida |
|---|---|
| 3840x2160 | 25 de 192px |
| 1920x1080 | (mismo mecanismo) |
| 1080x1600, 2560x700 | **192 de 192px - el nombre entero invisible** |

Y no es un objeto aislado: a 1080x1600 se pierden enteros "Armadura Corporal Tesla Áurica" (179),
"Careta Encapuchada Tesla Áurica" (186), "Semblante de Filo de Cable Tesla Áurico" (224),
"Caparazón de Tortuga Gigante" (171), "Generador Estelar Corrompido" (174), "Artefacto del Alma
Profanada" (166), "Yelmo Emplumado Tesla Áurico" (175), y 6 iconos completos.

**Causa real, y ya está diagnosticada en este mismo fichero para otro sitio.** El
`DataTemplate` de `BuildItemRowViewModel` (**líneas 551-593**) mete `Image` + `StackPanel` de
textos dentro de un `StackPanel Orientation="Horizontal"` (línea **566**). Un `StackPanel`
horizontal **mide a sus hijos con ancho infinito**, así que el `TextWrapping="Wrap"` del
`TextBlock` (línea **575**) **nunca llega a actuar**: el texto pide su ancho natural completo. La
tarjeta contenedora sí tiene tope (`BuildClassTemplate`, `Width="220"`, línea **602**), así que la
diferencia se corta.

Es **literalmente el mismo bug** que ya se encontró y se arregló en `CategoryNodeTemplate`, y el
comentario de las líneas **271-275** lo explica palabra por palabra:

> "un StackPanel Horizontal le da ancho infinito a este TextBlock al medir, asi que TextWrapping
> no bastaba solo - hacia falta un MaxWidth real para que el texto tuviera un limite del que
> envolver, en vez de desbordar en silencio fuera de la columna"

El remedio (`MaxWidth="165"`, línea **277**) se aplicó allí y **no se aplicó aquí**. El comentario
de la línea 571-574 (`Bd-e`) dice que el `TextWrapping` se añadió justo para esto - pero sin el
tope no hace nada.

### 4.4 H-04 — La franja vital de la cabecera se recorta, y **empeora al cruzar `NormalMinWidth`**

**Severidad: se recorta, sigue parcialmente legible. Presente en las 6 pestañas.**

Bisección real de 10 en 10px (`Personaje` cargado, ventana 860px de alto):

```
  1100px SizeClass=Compacto expandida=False tira=196px -> RECORTA 61px
  1150px SizeClass=Compacto expandida=False tira=196px -> RECORTA 11px
  1170px SizeClass=Compacto expandida=False tira=196px -> completa
  ...  (completa de 1170 a 1290)
  1300px SizeClass=Normal   expandida=True  tira=350px -> RECORTA 16px
  1320px SizeClass=Normal   expandida=True  tira=350px -> completa
  >>> PRIMER ANCHO SIN RECORTE CON LA TIRA EXPANDIDA: 1320px
```

Son **dos defectos independientes en la misma franja**:

**(a) A `MinWidth` la franja mínima ya no cabe.** Entre 1080 y 1170px, la tira compacta
(Vida + Maná, 196px) se recorta: **61px a 1100px**, 11px a 1150px. Elementos concretos que se
pierden a 1080x700, medidos y repetidos en las 20 pantallas:

```
  RECORTE TextBlock '♥'       pierde 9x0px de 9x17    <-- el icono de vida, ENTERO
  RECORTE TextBlock '600/500' pierde 8x1px de 31x11
  RECORTE TextBlock '200/200' pierde 7x1px de 31x11
```

Dicho de otro modo: **la cabecera global no cabe en el `MinWidth=1080` que la propia ventana
declara** (`MainWindow.xaml:12`). Necesita ~1170px. El reparto es
`Auto (identidad) | * (vitales) | Auto (6 botones)` (líneas 1324-1328): los dos `Auto` cobran
primero (identidad ~185px, los seis botones **658px** medidos por UI Automation - de x=399,6 a
x=1057,6 a 1080 de ventana) y a la columna `*` le quedan ~192px para una tira que necesita 196.

**(b) `IsVitalsStripExpanded` se enciende 20px antes de que quepa.** `MainViewModel.cs:406`:

```csharp
public bool IsVitalsStripExpanded => SizeClass != WindowSizeClass.Compacto;
```

A 1299px la tira está plegada y **completa**. A 1300px (`NormalMinWidth`) se despliega a
Defensa + Dinero + Horas + Último guardado, la tira pasa de 196 a **350px**, y **se recorta 16px**
hasta los 1320. El diff de la matriz lo confirma sin ambigüedad: cruzar de 1299 a 1300 **añade**
dos hallazgos nuevos (`'♥' pierde 8px de 9` y `'53h' pierde 8px de 18`) **en todas y cada una de
las pantallas**, y **no quita ninguno**. Es decir: a este ancho, ensanchar la ventana un solo
píxel empeora la cabecera.

**Esto es un dato duro sobre el sistema de breakpoints existente**: `NormalMinWidth = 1300` se
documenta en `MainViewModel.cs:369-372` como "umbral intermedio real... sin consumidor propio
todavía". Hoy **sí tiene un consumidor** (`IsVitalsStripExpanded`, añadido por H5-10) y para ese
consumidor el valor **está medido mal por 20px**. Es la única de las tres constantes del sistema
que nunca se midió contra contenido real - y se nota.

### 4.5 H-05 — Insignias de las tarjetas de Inicio cortadas, a todos los tamaños

**Severidad: se recorta. Permanente.**

```
  Inicio  RECORTE TextBlock 'Calamity' pierde 39x0px de 40x13
  Inicio  RECORTE TextBlock 'Calamity' pierde 17x0px de 40x13
```

Idéntico en **las 14 paradas**, de 1080x700 a 3840x2160. Dos tarjetas de personaje distintas
pierden 39 y 17 de los 40px de su insignia "Calamity" - en la primera queda **1px visible**.

**Causa real.** `CharacterCardTemplate` fija `Width="240"` en el `Border` (línea **1092**), y
dentro, la fila de insignias es un `StackPanel Orientation="Horizontal"` (línea **1158**) que
encadena, sin envolver: dificultad (1159-1161) + "Vanilla" (1172-1177) + "tModLoader" (1183-1187)
+ "Calamity" (1191-1195). Las tres insignias de identidad son de **4-sep-2026**
(`ESPEC-sprites-botones-badges.md#C`, "Ahora son tres hechos separados"); antes había una sola y
cabía. Al pasar de una a tres, el ancho fijo de 240px de la tarjeta dejó de dar, y como es un
`StackPanel` horizontal, la última se corta en silencio en vez de bajar de línea.

Es un caso especialmente feo porque la insignia "Calamity" es **precisamente** la señal que
`ESPEC-sprites-botones-badges.md#C` describe como "lo específico": el usuario no puede distinguir
de un vistazo qué personaje tiene contenido real de Calamity, que es el motivo por el que la
insignia existe.

### 4.6 H-06 — Árbol de categorías (Librería, Librería de buffs, Investigación) cortado

**Severidad: se recorta, hasta el 100% en el segundo nivel.**

```
  Personaje/Investigacion  RECORTE TextBlock 'Mascotas, Monturas, Herramientas' pierde 89x0px de 221x19
  Personaje/Investigacion  RECORTE TextBlock 'Pociones (regeneracion)'          pierde 21x0px de 153x19
  Personaje/Objetos/* [lib+]  RECORTE TextBlock 'Pociones (regeneracion)'       pierde 153x0px de 153x19
```

En las 14 paradas, incluido 4K. Con la Librería desplegada a 1080x700, "Pociones (regeneracion)"
pierde **el 100%**.

**Causa real.** `CategoryNodeTemplate` (líneas **240-284**) pone el texto en un `StackPanel
Orientation="Horizontal"` (línea 261) junto al icono, con `MaxWidth="165"` + `TextWrapping="Wrap"`
(líneas 276-277). Ese 165 se midió para el **primer** nivel. Pero cada nivel de profundidad añade
`Margin="16,0,0,0"` (línea **280**) y el árbol vive en una columna de **210px** fija (líneas
**2177**, **2409**, **2524**) con `Padding="5"` y `Margin="0,0,8,0"`. A partir del segundo nivel:
16 (sangría) + 16+6 (icono) + 165 (texto) = **203px** contra un hueco real de ~192, y en el tercer
nivel ya no queda nada. Medido: el nodo pide 221px reales.

El `MaxWidth` fijo es el arreglo correcto **para una profundidad concreta**; el árbol es
recursivo, así que hace falta un tope que dependa del sitio real, no una constante.

### 4.7 H-07 — Apariencia: el texto del preview se corta en ventanas bajas, sin scroll

**Severidad: se recorta, y no hay forma de alcanzarlo.**

```
  1080x700  RECORTE TextBlock 'Preview real de cuerpo completo (sprites rea...' pierde 0x9px de 240x102
  2560x700  RECORTE (idéntico)
```

Depende **sólo de la altura**: aparece a 700px de alto con cualquier ancho, y desaparece a 860px.

**Causa real.** La columna 0 de Apariencia (**líneas 2695-2709**) es un `StackPanel` con
`VerticalAlignment="Top"` y **sin `ScrollViewer`**: imagen de 336px de alto + `Padding="16"` +
`CheckBox` + un párrafo de 102px. A 700px de ventana el hueco real se queda 9px corto y WPF corta
la última línea del párrafo. La columna 1, al lado, **sí** tiene su `ScrollViewer`
(línea **2711**) - la asimetría es el defecto.

Es exactamente el patrón que el resto de la app llama "ScrollViewer de seguridad" (comentarios de
las líneas 25-31 y 1806-1810) y que aquí falta.

### 4.8 H-08 — Cabecera de Inventario: el contador "Inventario (N/M)" se corta a 1080px

**Severidad: se recorta.**

```
  1080x700, 1080x900, 1080x1600  RECORTE TextBlock 'Inventario (50/50)' pierde 15x0px de 89x15
```

Sólo a 1080px de ancho. **Causa**: el `DockPanel` de la cabecera (**líneas 1990-2015**) da todo el
ancho que piden a los 6 botones acoplados a la derecha (`DockPanel.Dock="Right"`, línea 1991) y
al título le deja el resto; cuando no llega, `DockPanel` recorta el último hijo. El contador es
justo el dato que `A-c` añadió para que no hubiera que contar a mano.

### 4.9 H-09 — Espacio muerto en ventanas grandes: hasta el 73% del ancho sin usar

**Severidad: no se pierde información, pero el resultado a pantalla completa es pobre.**

Medición real (contenido útil frente al viewport disponible):

| Ancho | Inicio | Apariencia | Desbloqueos | Acerca de | Novedades |
|---|---|---|---|---|---|
| 1500 | 5% | 14% | 13% | 10% | 10% |
| 1920 | **28%** | **39%** | **34%** | **31%** | **32%** |
| 2560 | **47%** | **57%** | **52%** | **50%** | **50%** |
| 3840 | **66%** | **73%** | **68%** | **67%** | **67%** |

**Causa real.** Los topes son constantes de dos valores y el escalón superior se acaba en
`Amplio` (≥1500):

- `InicioContentMaxWidth` = 1400 en Amplio (`MainViewModel.cs:441`)
- `AppearanceContentMaxWidth` = 1000 (`:448`)
- `DetailContentMaxWidth` = 1200 (`:464`)
- `DetailCardColumns` = 2 (`:469`)

A 3840px, Apariencia usa 900 de 3387px de viewport. El `WrapPanel` de swatches podría repartir
seis columnas y reparte dos; el `UniformGrid` de Novedades/Acerca de se queda en 2 columnas de
600px cuando caben 5.

Esto es **el mismo hallazgo que I-c y H4-07 ya resolvieron para el salto Compacto→Amplio**, sólo
que ahora reaparece un escalón más arriba: el sistema no contempla nada por encima de 1500px, y
1920 es hoy el tamaño de pantalla completa más común.

### 4.10 H-10 — Pastillas de pestaña interna: 6x4px recortados por un margen duplicado

**Severidad: cosmético, pero es un recorte real y el arreglo es de una línea.**

```
  TabItem  rect=209,103  61x37   clip=0;0;54.88;32.63
```

Pérdida exacta: 6,12 x 4,37px - justo el `Margin="0,0,6,4"` del estilo.

**Causa real.** `Theme.xaml:938`: el `Border` raíz de la plantilla de `InnerTabItem` lleva
`Margin="{TemplateBinding Margin}"`, y el `Margin="0,0,6,4"` del propio `TabItem`
(`Theme.xaml:934`) **ya lo ha aplicado el panel padre** al arreglar el `TabItem`. El margen se
cuenta dos veces, el contenido de la plantilla no cabe en su propio `TabItem` y WPF lo recorta.
Afecta a las 7 pestañas de Personaje, las 3 de Objetos, las 2 de Builds y las 2 de Novedades, a
todos los tamaños.

### 4.11 Riesgo residual comprobado, sin hallazgo: barra superior de Exploración

`MainWindow.xaml:3253-3283` es un `StackPanel Orientation="Horizontal"` que encadena "Cargar mundo
(.wld)...", el título del mundo, la píldora "Solo lectura" y cinco controles de zoom. Un
`StackPanel` horizontal **nunca envuelve y nunca recorta**: si no cabe, empuja fuera de la
ventana.

Probado con el nombre de mundo real más largo de esta máquina:

```
  mundo='Afueras de Larvas de gusano'
  1080px 'Ajustar a la ventana': izq=854 der=980 ventana=1080 -> dentro
```

Sobran ~100px. **No hay hallazgo hoy**, pero el margen es pequeño y depende de un dato del
usuario (Terraria permite nombres de mundo bastante más largos que 27 caracteres). Se recoge en
§5 como endurecimiento barato, no como corrección de un defecto medido.

### 4.12 Resumen de hallazgos

| # | Pantalla(s) | Se dispara a | Qué se pierde | Severidad |
|---|---|---|---|---|
| H-01 | Objetos > Inventario y Almacenes | **≥1500px** | 3 botones de conjunto x2 contenedores | desaparece |
| H-02 | Exploración (las 5 categorías) | **siempre** | píldora "Objetos", "Quitar marcas", "Por lo que contienen", "Líquidos" (60-66px) | desaparece |
| H-03 | Builds (Vanilla y Calamity) | **siempre** | nombres de objeto (25px a 4K, 100% en anchos medios) | desaparece |
| H-04a | Cabecera global (6 pestañas) | **<1170px** | icono ♥ entero + 8px de cada cifra | recorta |
| H-04b | Cabecera global (6 pestañas) | **1300-1319px** | 16px de la tira expandida | recorta |
| H-05 | Inicio | **siempre** | insignia "Calamity" (hasta 39 de 40px) | recorta |
| H-06 | Librería, Librería de buffs, Investigación | **siempre** | nombres de categoría de 2º nivel (hasta 100%) | recorta |
| H-07 | Apariencia | **alto <860px** | última línea del texto del preview | recorta, inalcanzable |
| H-08 | Objetos > Inventario | **1080px** | 15px del contador "Inventario (N/M)" | recorta |
| H-09 | Inicio, Apariencia, Desbloqueos, Versión, Novedades, Acerca de | **≥1920px** | nada; 28-73% del ancho sin usar | apretado/desaprovechado |
| H-10 | todas las pestañas internas | **siempre** | 6x4px de cada pastilla | cosmético |

---

# PARTE II — PROPUESTA

## 5. Plan de corrección priorizado

Criterio mío. Ordenado por severidad y, dentro de cada bloque, por esfuerzo creciente.

**Correspondencia hallazgo → corrección** (los números **no** van pareados, porque el orden por
severidad no coincide con el orden en que se listaron los hallazgos - esta tabla es la referencia):

| Hallazgo | Corrección | Bloque | Esfuerzo |
|---|---|---|---|
| H-01 | **R-01** | A (desaparece) | pequeño |
| H-02 | **R-02** | A (desaparece) | pequeño |
| H-03 | **R-03** | A (desaparece) | trivial |
| H-04a + H-04b | **R-04** | B (recorta) | medio |
| H-05 | **R-05** | B | trivial |
| H-06 | **R-06** | B | pequeño |
| H-07 | **R-07** | B | trivial |
| H-08 | **R-08** (lo cierra R-01) | B | ninguno |
| H-10 | **R-09** | B | trivial |
| H-09 | **R-10** | C (aprovechamiento) | medio |
| (§4.11, sin defecto) | **R-11** | D (endurecer) | trivial |

### Bloque A — Desaparece del todo (hacer sí o sí antes de entregar)

---

**R-01 · H-01 · Devolver los 3 botones de conjunto a la vista Amplio.** Esfuerzo: **pequeño**.

Corrección mínima y de riesgo cero (no toca el ViewModel ni el reparto):

1. En la rama Amplio, mitad Inventario (`MainWindow.xaml`, dentro del `StackPanel` de
   **2033-2041**), añadir los tres botones **copiados literalmente** de 1997-2005, con sus mismos
   `Click="OnSaveInventorySetClick"` / `OnLoadInventorySetClick"` / `OnAppendInventorySetClick"`
   (los handlers leen el contenedor por `DataContext`, así que funcionan tal cual).
2. En la rama Amplio, mitad Almacén (`StackPanel` de **2052-2057**), añadir los tres de
   2091-2099 con `OnSaveStorageSetClick` / `OnLoadStorageSetClick` / `OnAppendStorageSetClick`.
3. En las **cuatro** barras (1991, 2033, 2052, 2087), cambiar el
   `<StackPanel DockPanel.Dock="Right" Orientation="Horizontal">` por
   `<WrapPanel DockPanel.Dock="Right">`. Con 6 botones en la mitad del ancho, el `WrapPanel`
   pasa a dos filas en vez de recortar - y **de paso resuelve H-08 sin tocar nada más**.

Corrección de fondo (recomendada, pero puede ir después): las 4 cabeceras son la duplicación que
`H4-10` ya señalaba. Extraerlas a un `DataTemplate x:Key="ContainerToolbarTemplate"` con los 6
botones. El único obstáculo real es que los 3 de conjunto son `Click=` a code-behind con dos
juegos de handlers distintos; se unifica pasando el contenedor por `CommandParameter` a un único
par de handlers, o convirtiendo los tres en comandos del `ContainerViewModel` que pidan la ruta a
través de un servicio de diálogo. **No lo haría antes de la entrega**: el arreglo de 3 pasos de
arriba deja el comportamiento correcto y la refactorización tiene más superficie de riesgo.

---

**R-02 · H-02 · Que la barra lateral de Exploración se mida contra su ancho real.** Esfuerzo:
**pequeño**. Es un cambio de dos líneas y arregla seis controles a la vez.

El problema es *dónde* está el `MaxWidth`, no su valor. Mover el tope de la `ColumnDefinition` al
`DockPanel` hijo hace que WPF mida el contenido **con** el tope, y entonces el `WrapPanel` de
píldoras envuelve de verdad en vez de que se le corte la quinta.

`MainWindow.xaml:3319`:
```xml
<!-- antes -->
<ColumnDefinition Width="Auto" MinWidth="260" MaxWidth="380" />
<!-- después -->
<ColumnDefinition Width="Auto" MinWidth="260" />
```
`MainWindow.xaml:3548`:
```xml
<!-- antes -->
<DockPanel Grid.Column="1">
<!-- después -->
<DockPanel Grid.Column="1" MinWidth="260" MaxWidth="380">
```

Con eso la columna `Auto` toma el ancho deseado del `DockPanel`, que ya no puede pasar de 380, y
todo lo de dentro (`WrapPanel` de categorías, `UniformGrid` de Cofres/Minerales/Objetos, el
`Expander`) se mide contra 380 reales. Las 5 píldoras pasarán a dos filas por debajo de ~408px de
contenido, que es exactamente lo que `ESPEC-ui-exploracion.md#9.1` describía ("fila de píldoras",
un `WrapPanel`, no una fila rígida).

**Verificar además** que a 380px el `UniformGrid Columns="2"` de Cofres (3788) y el
`Columns="3"` de Objetos (3851) no dejan los rótulos cortados; si "Por lo que contienen" sigue sin
caber en 190px, bajarle el `FontSize` o pasar el `UniformGrid` a `Columns="1"` en esa categoría.
Eso ya es afinado visual, no pérdida de contenido.

**Segunda parte, opcional pero recomendada**: el `MaxWidth="380"` viene de
`ESPEC-ui-exploracion.md#8-D4` ("TEdit diseña este mismo panel a 400px"). En una ventana de 1920+
no hay ninguna razón para seguir topando en 380 - ver R-09.

---

**R-03 · H-03 · Poner tope real al texto de las filas de Builds.** Esfuerzo: **trivial**.

`MainWindow.xaml:570`, el `StackPanel` interior que contiene los dos `TextBlock`:
```xml
<!-- antes -->
<StackPanel>
<!-- después -->
<StackPanel MaxWidth="167">
```

El número no es a ojo: sale de la medición real de §4.3. Tarjeta 220 (`BuildClassTemplate`,
línea 602) − 6 del `Margin="0,0,6,6"` de la fila (552) = **214**, que es exactamente el ancho
medido del `Border`; − 2x8 de su `Padding` (553) = **198**, que es exactamente el recorte medido
(`clip=0;0;198;31.26`); − 24 del icono (567) − 7 de su margen = **167px** para los textos. Con eso
el `TextWrapping="Wrap"` que ya está en la línea 575 por fin actúa y los nombres largos pasan a
dos líneas.

**Poner el mismo comentario que `CategoryNodeTemplate:271-275`** citando que es el mismo bug -
así la próxima vez que aparezca un `StackPanel Horizontal` con texto dentro, se busque el patrón.

### Bloque B — Se recorta pero sigue usable

---

**R-04 · H-04 · La franja vital.** Esfuerzo: **medio** (toca el sistema de breakpoints).

Dos cambios, y **los dos encajan en el sistema `SizeClass` existente - no hace falta ningún
mecanismo paralelo**:

**(a) Red de seguridad universal, sin breakpoint.** `MainWindow.xaml:1407`: cambiar el
`StackPanel Orientation="Horizontal"` de la franja por un `WrapPanel` y quitarle el
`HorizontalAlignment="Center"`. Si no cabe, la franja pasa a dos líneas y la cabecera crece unos
20px (su fila es `Height="Auto"`, línea 1303, así que crece sola). **Nada se pierde nunca, a
ningún ancho**, incluido el `MinWidth=1080` que hoy no da. Esto por sí solo cierra H-04a y H-04b.

**(b) Corregir el umbral mal medido.** `MainViewModel.cs:374`: subir `NormalMinWidth` de **1300 a
1320**, el primer ancho medido en que la tira expandida cabe entera (§4.4). Es seguro:
`IsVitalsStripExpanded` (`:406`) es hoy **el único** consumidor de ese umbral, así que moverlo 20px
no afecta a ninguna otra pantalla. **Y actualizar el comentario de `MainViewModel.cs:369-372`**,
que sigue diciendo "sin consumidor propio todavía" - ya no es cierto, y el valor pasa a estar
medido igual que los otros dos.

Alternativa que **no** recomiendo: añadir un cuarto valor al enum sólo para esto. Con (a) puesto,
un breakpoint extra no aporta nada que el `WrapPanel` no dé gratis.

**Nota aparte, para decidir**: (a) hace que la cabecera *quepa* a 1080px, pero no cambia el hecho
de que **la cabecera necesita ~1170px para verse en una sola línea**. Si se prefiere que la
cabecera nunca crezca, la otra salida real es **subir `Window.MinWidth` de 1080 a 1180**
(`MainWindow.xaml:12`) - que es, además, exactamente el ancho de arranque que la app ya usa, así
que nadie notaría el cambio salvo quien encoja a mano. Es una decisión de producto, no técnica; yo
haría (a) igualmente y dejaría el `MinWidth` en paz.

---

**R-05 · H-05 · Insignias de Inicio.** Esfuerzo: **trivial**.

`MainWindow.xaml:1158`: cambiar
`<StackPanel Orientation="Horizontal" Margin="0,4,0,0">` por
`<WrapPanel Margin="0,4,0,0">`.

Con `Width="240"` en la tarjeta (línea 1092) y hasta 3 insignias + dificultad, la fila pasa a dos
líneas cuando hace falta en vez de cortar la última. La tarjeta vive en un `WrapPanel` exterior
(línea 1588) y no tiene alto fijo, así que crecer no rompe nada.

Alternativa si se prefiere no crecer: subir la tarjeta de `Width="240"` a `Width="270"` (el mismo
ancho que ya usan las 5 tarjetas de navegación de Inicio, líneas 1614-1658, así que no introduce
un número nuevo). Es más discreto pero menos robusto: una cuarta insignia futura volvería a
cortarse.

---

**R-06 · H-06 · Árbol de categorías recursivo.** Esfuerzo: **pequeño**, pero afecta a 3 pantallas
a la vez, así que conviene verificar bien.

`MainWindow.xaml:261-278`: sustituir el `StackPanel Orientation="Horizontal"` por un `DockPanel`
con el icono en `DockPanel.Dock="Left"` y el `TextBlock` como último hijo, **quitando el
`MaxWidth="165"`**:

```xml
<DockPanel>
    <Image DockPanel.Dock="Left" Source="{Binding IconPath}" Width="16" Height="16"
           Margin="0,0,6,0" Stretch="Uniform" VerticalAlignment="Center"
           Visibility="{Binding IconPath, Converter={StaticResource NullToVis}}" />
    <TextBlock Text="{Binding Name}" FontSize="11.5" VerticalAlignment="Center"
               TextWrapping="Wrap" />
</DockPanel>
```

El motivo real: un `DockPanel` **sí** da al último hijo el ancho restante real (a diferencia del
`StackPanel` horizontal, que mide con infinito), así que `TextWrapping` funciona **a cualquier
profundidad de sangría y a cualquier ancho de columna**, sin ninguna constante. Requiere además
que el `Button` que lo envuelve (línea 242) estire: añadirle
`HorizontalAlignment="Stretch"` - ya tiene `HorizontalContentAlignment="Left"`.

Sirve a la vez para Librería (2177), Librería de buffs (2409) e Investigación (2524), que
comparten esta plantilla desde `T-18`.

---

**R-07 · H-07 · `ScrollViewer` de seguridad en la columna del preview de Apariencia.** Esfuerzo:
**trivial**.

`MainWindow.xaml:2695`: envolver el `StackPanel Grid.Column="0"` en
`<ScrollViewer Grid.Column="0" VerticalScrollBarVisibility="Auto">` (moviendo el `Grid.Column`
al `ScrollViewer` y quitándolo del `StackPanel`). Mismo patrón exacto que la columna 1 de al lado
(línea 2711) y que el resto de la app.

---

**R-08 · H-08 · Contador "Inventario (N/M)".** Esfuerzo: **ninguno** - queda resuelto por el paso
3 de R-01 (`WrapPanel` en las barras de botones). Si por lo que sea R-01 se pospone, el parche
suelto es añadir `TextTrimming="CharacterEllipsis"` al `TextBlock` de la línea 2014, que al menos
convierte el corte en seco en unos puntos suspensivos honestos.

---

**R-09 · H-10 · Margen duplicado en `InnerTabItem`.** Esfuerzo: **trivial**.

`Theme.xaml:938`: quitar `Margin="{TemplateBinding Margin}"` del `Border x:Name="Bd"`. El
`Margin="0,0,6,4"` del `Setter` de la línea 934 ya lo aplica el panel padre.

Comprobar de paso que `NavTabItem` (`Theme.xaml:879-916`) no tenga el mismo patrón - ahí el
`Grid` raíz usa `Margin="8,2"` **literal**, no `TemplateBinding`, así que está bien.

### Bloque C — Aprovechamiento en ventanas grandes

---

**R-10 · H-09 · Un cuarto escalón para ventanas muy anchas.** Esfuerzo: **medio**.

Esto **sí** pide ampliar el sistema existente, y encaja limpiamente: un cuarto valor del enum ya
construido, no un mecanismo nuevo.

`WindowSizeClass.cs`, añadir tras `Amplio`:

```csharp
// Sitio real de sobra para 3 paneles Y una tercera columna de tarjetas, o para que las
// pantallas de detalle dejen de desperdiciar la mitad del ancho (auditoria de redimensionado,
// H-09: a 1920px entre el 28% y el 39% del ancho quedaba sin usar; a 2560px, la mitad).
Extra,
```

`MainViewModel.cs`, junto a las otras dos constantes:

```csharp
// Medido contra el contenido real de cada consumidor, no elegido a ojo: 1920 es el primer
// ancho donde el tope de Amplio (1400/1000/1200) deja mas de un cuarto del viewport vacio.
private const double ExtraMinWidth = 1920;
```

y en `UpdateSizeClass` (`:390`), un escalón más arriba de `Amplio`.

Consumidores nuevos (todos en `MainViewModel.cs`, mismo estilo que los actuales):

| Propiedad | Hoy (Amplio) | Propuesto (Extra) | Por qué ese número |
|---|---|---|---|
| `InicioContentMaxWidth` (:441) | 1400 | **1900** | 6 tarjetas de 270+14 en una fila = 1704, más el margen |
| `AppearanceContentMaxWidth` (:448) | 1000 | **1400** | 8 swatches de 150+10 en fila = 1280 |
| `DetailContentMaxWidth` (:464) | 1200 | **1700** | 6 tarjetas de Desbloqueos de 280+10 = 1740 |
| `DetailCardColumns` (:469) | 2 | **3** | tarjetas de changelog de ~560px, legibles |

Y **no** tocar los `MaxWidth="680"` de texto corrido de Acerca de (líneas 3883, 3890, 3898, 3949):
son deliberados y correctos, el tope de longitud de línea por legibilidad que `H5-09` ya decidió.

**Expectativa honesta de resultado**, calculada con los viewports reales medidos en §4.9: a
1920px el desperdicio pasa de 28-39% a **~0%** (los topes nuevos superan el viewport, así que se
usa entero); a 2560px baja a **~21-30%**; a 3840px sigue en **~45-50%**. Y está bien que así sea:
un `WrapPanel` de tarjetas de color repartido en 3840px de ancho sería peor de usar, no mejor.
Un quinto escalón no lo recomiendo - a partir de cierto punto, dejar aire es la decisión
correcta, y es la misma razón por la que el texto corrido de Acerca de se topa en 680.

Añadir la propagación en `OnSizeClassChanged` (`:471-488`) - las 4 propiedades ya están listadas
ahí, así que basta con que el enum tenga el valor nuevo.

**Consumidor adicional a considerar**: `MaxWidth` de la barra lateral de Exploración (R-02): en
`Extra` podría subir de 380 a 460-500 y dejar de recortar nada aunque el contenido crezca. Es
opcional; con R-02 ya no se pierde nada a ningún tamaño.

### Bloque D — Endurecimiento barato, sin defecto medido

**R-11 · §4.11 · Barra superior de Exploración.** `MainWindow.xaml:3253`: cambiar el
`StackPanel Orientation="Horizontal"` por un `WrapPanel`, y añadir
`TextTrimming="CharacterEllipsis" MaxWidth="320"` al `TextBlock` del título (línea 3256). Hoy
sobran ~100px con el nombre de mundo más largo de esta máquina, pero un `StackPanel` horizontal
empuja fuera de la ventana sin avisar y el dato es del usuario. Coste: dos atributos.

### 5.1 Orden de ejecución sugerido

1. **R-03, R-05, R-07, R-09, R-11** - los cinco triviales, uno o dos atributos cada uno. Un solo
   commit, verificación en verde, y ya se han cerrado dos hallazgos de severidad máxima (H-03) y
   tres recortes.
2. **R-02** - dos líneas, cierra el segundo hallazgo de severidad máxima. Commit propio, porque
   requiere mirar cómo quedan las píldoras en dos filas.
3. **R-01** - el más importante y el más largo (copiar 6 botones + 4 `WrapPanel`). Commit propio.
4. **R-06** - toca 3 pantallas a la vez con una plantilla compartida. Commit propio.
5. **R-04** - toca el sistema de breakpoints. Commit propio, con el comentario de
   `MainViewModel.cs:369-372` actualizado.
6. **R-10** - ampliación del enum. Último, es el único que no corrige una pérdida de información.

## 6. Plan de verificación real: qué añadir a `TerrasavrNative.App.Tests/Program.cs`

### 6.0 Lo primero, y es bloqueante: el arnés no redimensiona de verdad

**Antes de añadir ninguna comprobación nueva**, `Program.cs` necesita el hook de §1.2. Sin él,
todas las verificaciones de tamaño de este plan medirán 1080px en esta máquina y **darán resultados
falsos sin avisar** - que es exactamente lo que lleva pasando con `E2-UMBRAL`, `A4-1350`,
`A4-EXPANDIDO` y `H5-09-AMPLIO`.

Añadir junto a la creación de la ventana (tras `window.Show()`, sobre la línea ~99):

```csharp
// Auditoria de redimensionado, §1.1: en una sesion RDP con escalado alto, el escritorio logico
// puede ser MAS ESTRECHO que el MinWidth=1080 de la ventana. Windows resuelve el conflicto
// ptMinTrackSize > ptMaxTrackSize aplicando el minimo el ultimo, asi que TODA peticion de
// window.Width acaba clavada en 1080 exactos, sin error ni aviso. Subir ptMaxTrackSize desde el
// arnes (nunca desde produccion) es la unica forma de que este arnes mida de verdad los
// umbrales de SizeClass. Medido en esta maquina: pantalla 576x1197 DIP (1440x2992 fisicos al
// 250%), SM_CXMAXTRACK=1476 fisicos = 590 DIP.
const int WM_GETMINMAXINFO = 0x0024;
((HwndSource)PresentationSource.FromVisual(window)!).AddHook((IntPtr h, int msg, IntPtr wp, IntPtr lp, ref bool handled) =>
{
    if (msg == WM_GETMINMAXINFO)
    {
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lp);
        mmi.ptMaxTrackSize.x = 32000; mmi.ptMaxTrackSize.y = 32000;
        mmi.ptMaxSize.x = 32000; mmi.ptMaxSize.y = 32000;
        Marshal.StructureToPtr(mmi, lp, true);
    }
    return IntPtr.Zero;
});
```

(con los `struct POINT`/`MINMAXINFO` correspondientes; `Program.cs` ya usa
`System.Runtime.InteropServices` y `System.Windows.Interop`).

Y **una guarda que grite si el redimensionado no funciona**, para que esto no pueda volver a
pasar en silencio en ninguna máquina:

```csharp
static void FijarTamaño(Window w, double ancho, double alto)
{
    w.Width = ancho; w.Height = alto;
    DoEvents(); DoEvents(); DoEvents();
    if (Math.Abs(w.ActualWidth - ancho) > 1 || Math.Abs(w.ActualHeight - alto) > 1)
        Console.WriteLine($"FALLO: RESIZE-IMPOSIBLE - pedido {ancho}x{alto}, obtenido " +
                          $"{w.ActualWidth:0}x{w.ActualHeight:0}. TODA comprobacion de umbral " +
                          $"que venga despues es INVALIDA en esta maquina.");
}
```

Sustituir por esta función los `window.Width = ...; window.Height = ...` sueltos que ya hay
(líneas 121-122, 1531-1532, 1646, 1664-1665, 1858, 2033, 2040, 2599, 3171-3172).

### 6.1 Comprobaciones nuevas, una por hallazgo

Bloque nuevo al final de `Program.cs`, con el mismo formato "esperado X, obtenido Y" del resto:

**`AR-01` (H-01)** - los 6 botones de conjunto existen a los dos lados del umbral:
```csharp
foreach (double w in new double[] { 1450, 1500, 1920 })
{
    FijarTamaño(window, w, 900);
    vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1;
    DoEvents(); DoEvents();
    var nombres = root.FindAll(TreeScope.Descendants,
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
        .Cast<AutomationElement>().Select(b => b.Current.Name).ToList();
    foreach (string b in new[] { "Guardar conjunto...", "Cargar...", "Añadir..." })
        Console.WriteLine($"AR-01: a {w}px, '{b}' presente x{nombres.Count(n => n == b)} " +
                          $"(esperado >=1 en Compacto/Normal y >=2 en Amplio - Inventario Y Almacen)");
}
```

**`AR-02` (H-02)** - la barra lateral de Exploración no recorta a ningún tamaño. Reutilizable
para casi todo lo demás: una función de recorte real, que es la pieza central del arnés nuevo.

```csharp
// Devuelve los pixeles que WPF esta recortando AHORA MISMO de este elemento (0 = ninguno).
// VisualTreeHelper.GetClip sobre el propio elemento recortado es el detector real - ver
// ESPEC-auditoria-redimensionado.md §1.4 (experimento controlado: el recorte NO lo hace el
// CornerRadius del Border, lo hace el recorte de layout de WPF, y se lee en el hijo).
static (double x, double y) Recorte(FrameworkElement fe)
{
    var c = System.Windows.Media.VisualTreeHelper.GetClip(fe);
    if (c == null) return (0, 0);
    return (Math.Max(0, fe.ActualWidth - c.Bounds.Width), Math.Max(0, fe.ActualHeight - c.Bounds.Height));
}
```

y con ella:
```csharp
foreach (double w in new double[] { 1080, 1500, 1920, 2560 })
{
    FijarTamaño(window, w, 900);
    vm.SelectedTabIndex = 4;
    foreach (var cat in Enum.GetValues<WorldSearchCategory>())
    {
        vm.Exploration.SelectedCategory = cat; DoEvents(); DoEvents();
        var pildora = BuscarRadioButtonPorTexto(root, "Objetos (");
        Console.WriteLine($"AR-02: a {w}px, categoria {cat}, pildora 'Objetos' recorte={Recorte(pildora)} (esperado 0,0)");
    }
}
```

**`AR-03` (H-03)** - el nombre más largo de Builds no se recorta:
```csharp
// El nombre real mas largo del catalogo de builds; si cambia el catalogo, este test lo dira.
Console.WriteLine($"AR-03: 'Semblante de Filo de Cable Tesla Áurico' recorte={Recorte(tb)} (esperado 0,0) a {w}px");
```
a 1080, 1500 y 3840.

**`AR-04` (H-04)** - barrido del umbral de la franja vital, con las dos mitades:
```csharp
foreach (double w in new double[] { 1080, 1170, 1299, 1300, 1320, 1500 })
{
    FijarTamaño(window, w, 860);
    var tira = BuscarFranjaVital(window);
    Console.WriteLine($"AR-04: a {w}px SizeClass={vm.SizeClass} expandida={vm.IsVitalsStripExpanded} " +
                      $"recorte={Recorte(tira)} (esperado 0,0 SIEMPRE tras R-04)");
}
```

**`AR-05` (H-05)** - insignia "Calamity" de Inicio sin recorte a 1080 y a 1920.

**`AR-06` (H-06)** - un nodo de 2º nivel del árbol (`"Pociones (regeneracion)"`) sin recorte, en
las tres pantallas que comparten `CategoryNodeTemplate` (Librería, Librería de buffs,
Investigación), a 1080 y 1920.

**`AR-07` (H-07)** - a 1080x700, la columna 0 de Apariencia tiene un `ScrollViewer` ancestro y su
párrafo no se recorta.

**`AR-08` (H-08)** - `Recorte(tbContadorInventario)` = (0,0) a 1080x700 con la Librería
desplegada (el caso más apretado). Va con `AR-01`: si el `WrapPanel` de R-01 está puesto, éste
pasa solo.

**`AR-09` (H-09)** - aprovechamiento de ancho, como valor informativo con umbral:
```csharp
foreach (double w in new double[] { 1500, 1920, 2560, 3840 })
{
    FijarTamaño(window, w, 1080);
    // ... por cada pantalla de detalle
    double desperdicio = 1 - contenido.ActualWidth / sv.ViewportWidth;
    // Umbral por escalon, no uno unico: ver la expectativa real calculada en R-10.
    int tope = w >= 3000 ? 55 : w >= 2400 ? 35 : 15;
    Console.WriteLine($"AR-09: a {w}px {nombre} usa {contenido.ActualWidth:0} de {sv.ViewportWidth:0} " +
                      $"-> {100 * desperdicio:0}% sin usar (esperado <{tope}% tras R-10)");
}
```

**`AR-10` (H-10)** - `Recorte(tabItem)` = (0,0) en las 7 pestañas internas de Personaje.

### 6.2 Una red permanente, no sólo 10 comprobaciones puntuales

Lo más valioso que deja esta auditoría no son los 10 hallazgos, sino **el barrido que los
encontró**. Recomiendo portar a `Program.cs` una versión reducida del recorrido de §1.4:

```csharp
// AR-BARRIDO: recorre el arbol visual real de la pantalla activa y lista TODO elemento que WPF
// este recortando de verdad ahora mismo. No comprueba una lista cerrada de controles - encuentra
// los que nadie ha pensado en comprobar, que son justo los que se pierden entre versiones.
static void BarridoDeRecortes(Window w, string etiqueta) { /* ver §1.4 */ }
```

y llamarlo en **una matriz reducida pero real** (1080x700, 1180x860, 1500x900, 1920x1080,
2560x700) por cada una de las 20 pantallas de §2. Con el detector de §1.4 completo eso son ~150
líneas y detecta de golpe cualquier recorte nuevo que introduzca un cambio futuro - incluidos los
tres que hoy son permanentes y que nadie había visto en 6 auditorías.

**Dos advertencias reales para quien lo porte**, aprendidas a base de falsos positivos (§1.5):

1. **Nunca reasignar el marco de recorte heredado dentro del bucle de hijos.** Usar una variable
   local por hijo; si no, en cuanto un hermano tiene recorte, todos los siguientes lo heredan.
2. **Calcular el solape por eje por separado**, nunca con `Rect.Intersect` a secas, y **anular la
   pérdida en un eje si hay un `ScrollViewer` ancestro que puede desplazarse en él**: estar fuera
   de vista y ser alcanzable con la rueda no es un defecto.

---

# PARTE III — HUECOS

## 7. Alcance deliberadamente fuera

1. **Estados vacíos.** Todo se midió con un personaje y un mundo cargados. Quedan sin auditar:
   Inicio sin ningún `.plr` detectado (el botón "Empezar: cargar un personaje", líneas 1593-1603),
   Exploración sin mundo (el overlay de 3528-3544), y la cabecera global con
   `IsCharacterLoaded=false` (que oculta identidad y franja vital y deja la columna `*` entera
   libre - probablemente **mejor**, no peor, pero no medido).
2. **Estados transitorios y superpuestos.** No se midieron con el popup "¿Dónde lo tengo?" abierto
   (`Width="400"` fijo, línea 1455, anclado a un botón que se mueve con el ancho: candidato real a
   salirse por la derecha en ventanas estrechas), ni con los pickers de peinado/tinte desplegados
   (`MaxHeight` 240/360, líneas 2755 y 2792), ni con la Librería en modo "eligiendo"
   (`IsPicking`, que revela el banner de 2165-2173 y el botón "Colocar" de cada tarjeta), ni con
   los banners de "Guardado"/error visibles (3973-4050). Son los estados donde más fácil es que
   algo se solape, y son un encargo en sí mismos.
3. **`WindowState = Maximized`.** Sólo se probaron tamaños `Normal`. Maximizado en un monitor real
   debería equivaler a un tamaño de la matriz, pero el camino de código de WPF es distinto
   (`ptMaxSize`) y no se comprobó.
4. **Otros escalados de DPI.** Todo se midió al **250%** (el de esta sesión RDP). WPF hace el
   layout en DIP y es independiente del DPI, así que los números deberían trasladarse tal cual;
   pero el redondeo del texto **sí** depende del DPI, y varios hallazgos están en el filo (H-04b
   pierde 16px, H-08 pierde 15px). A 100% podrían aparecer o desaparecer por 2-3px. **Ninguno de
   los tres hallazgos de severidad máxima está en ese filo** (60-192px de pérdida).
5. **Redimensionado *en movimiento*.** Se midió el estado estable tras cada cambio de tamaño (tres
   vueltas de bomba de mensajes). No se auditó el comportamiento **durante** un arrastre continuo
   del borde de la ventana, donde `SizeChanged` dispara decenas de veces por segundo y
   `OnSizeClassChanged` (`MainViewModel.cs:471`) puede cambiar `ObjetosSubTabIndex` (línea 487) en
   mitad del gesto. No es un problema de "algo se pierde de vista", pero es el único camino por el
   que un cambio de tamaño puede alterar **estado** del usuario.
6. **El fallo de carga de `adrian.plr`** (§1.6). Real y reproducible, pero no es de layout.
7. **Rendimiento a 4K.** La ventana de 3840x2160 se compuso sin problema, pero no se midió el
   coste de repintado del mapa de Exploración a ese tamaño.

---

## Apéndice: cómo reproducir esta auditoría

El arnés desechable vive en el scratchpad de la sesión, **fuera del repo** (dos proyectos que
referencian `TerrasavrNative.App.csproj`):

- `AuditResize/Audit.cs` - hook `WM_GETMINMAXINFO`, experimento del `Border`, barrido de las 20
  pantallas x 14 tamaños, y la pasada de UI Automation. Salida: `auditoria.txt` (1.501 líneas,
  1.234 hallazgos brutos).
- `AuditResize/Diag.cs` - los tres diagnósticos focalizados: volcado de cadena de ancestros
  (rect + clip por nivel), bisección del umbral de la franja vital, presencia real de botones por
  tamaño, y aprovechamiento de ancho.

Nada de eso debe entrar en el repo tal cual: lo que sí debe entrar, ya reescrito con el estilo del
proyecto, es lo de §6.
