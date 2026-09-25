# ParidadVisual - arnes de comparacion visual pixel-real (ParidadPersonaje Fase0)

Compara el "doll" que muestra Terrakeep contra el mismo personaje renderizado por Terraria
1.4.5.8 REAL (Steam, `C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe`),
para los 18 casos minimos del encargo de paridad visual. Ver `bitacora.md`, entrada
"ParidadPersonaje Fase0" (25-sep-2026), para el relato completo con evidencia.

## Piezas

1. **`ParidadVisual.csproj`/`Program.cs`** (consola .NET 10, `UseWPF`, referencia
   `Terrakeep.App.csproj` - NO es parte de `Terrakeep.slnx`, es un arnes de QA aparte):
   - `dotnet <ruta>\ParidadVisual.dll renderizar <rutaPlrReal> <salidaPng>` - genera el lado
     "terrakeep" de un caso. Lee cualquier `.plr` REAL vanilla (el mismo que carga el juego) y
     llama al pipeline de PRODUCCION real sin duplicar logica: `EquipmentAppearanceResolver.
     Resolve`/`ResolveAccessories` + `PlayerPreviewRenderer.Render` - transcripcion 1:1 de
     `Terrakeep.App/ViewModels/CharacterListEntryViewModel.cs:221-251` (el mismo camino que la
     tarjeta de personaje de Inicio), fuera de WPF/UI para poder invocarlo desde consola.
   - `dotnet <ruta>\ParidadVisual.dll preparar-caso <plantillaPlr> <destinoPlr> --nombre <n>
     [--limpiar]` - deriva un `.plr` nuevo a partir de una plantilla real (hereda `Version`/
     tamanos de contenedor validos). **LIMITE REAL CONOCIDO, ver mas abajo: el `.plr` que
     produce este comando NO lo carga el juego real** (aunque `Terrakeep.Core.PlrFormat.
     PlrFile.Read`/`VerifyRoundTrip` lo releen sin problema) - util solo para generar el lado
     "terrakeep" con datos sinteticos, NUNCA para el lado vanilla.

2. **`capturar_vanilla.py`** (Python 3.14 + pywinauto 0.6.9 + pywin32 + Pillow, todo ya
   instalado en esta maquina): automatiza el juego real - lanza Terraria en ventana
   1280x720/UIScale=1.0 (deterministico), hace backup/restore de verdad del
   `config.json` REAL del usuario (`Documents\My Games\Terraria\config.json` - nunca se deja
   modificado, ni siquiera si algo falla a medias, ver `SesionTerraria.__exit__`), navega los
   menus con clics reales (`win32api.mouse_event` de bajo nivel - ver "hallazgo real" mas abajo)
   y vuelca la ventana a PNG con `PIL.ImageGrab` (funciona sin problemas en modo ventana).

## Flujo real para un caso nuevo (el que se uso para el Caso 1)

```python
from capturar_vanilla import SesionTerraria, recortar_bbox_personaje

with SesionTerraria() as s:
    s.ir_a_seleccionar_personaje()
    s.capturar_ventana_completa("paso1.png")     # <- INSPECCIONAR (Read) antes de seguir
    s.crear_personaje_nuevo("ZZCasoN")           # o usar un .plr ya preparado a mano en el juego
    s.capturar_ventana_completa("paso2.png")     # <- INSPECCIONAR de nuevo
    # scroll hasta ver el personaje en la lista, recortar su bbox por color de fondo:
    ruta = s.capturar_ventana_completa("vanilla_raw.png")
    recortar_bbox_personaje(str(ruta), x0, y0, x1, y1, "vanilla.png")
```

Despues, en PowerShell/Bash:

```
dotnet <repo>\scripts\ParidadVisual\bin\Release\net10.0-windows\ParidadVisual.dll renderizar \
    "C:\Users\adrian\Documents\My Games\Terraria\Players\ZZCasoN.plr" terrakeep.png

magick terrakeep.png -filter point -resize <ancho_vanilla>x<alto_vanilla>! terrakeep_norm.png
magick compare -metric AE -fuzz 10% vanilla.png terrakeep_norm.png diff.png
```

**IMPORTANTE (ver el aviso real en la cabecera de `capturar_vanilla.py`)**: cada metodo de
`SesionTerraria` esta verificado individualmente (asi se obtuvo la evidencia del Caso 1,
capturando y revisando la imagen tras CADA clic). Encadenar varios metodos SEGUIDOS sin
capturar+revisar entre medias NO esta verificado como fiable - un reintento asi fallo el mismo
dia (un clic aterrizo en la pantalla de Logros en vez de en Seleccionar Jugador, probablemente
por variacion real de timing entre transiciones). Para un caso nuevo, ir paso a paso e
inspeccionar cada captura antes de decidir el siguiente clic.

## Hallazgo real durante la construccion del arnes (no anticipado, documentado con honestidad)

`ParidadVisual.exe preparar-caso` (que reutiliza `Terrakeep.Core.PlrFormat.PlrFile.Write`, el
mismo escritor real que usa `CharacterFileService.Save` en produccion) genera un `.plr` que
Terrakeep vuelve a leer sin problema (`PlrFile.VerifyRoundTrip` en verde), pero que **el juego
real rechaza** - aparece en la lista de personajes como `(UnknownError) <nombre>` y el juego
sustituye TODA la apariencia por la de un `Player` nuevo por defecto (confirmado leyendo
`Player.cs:55716-55792` del decompilado real, `LoadPlayer`/`Deserialize`: cualquier excepcion a
mitad de la lectura hace `catch { } Player player2 = new Player(); player2.loadStatus =
StatusID.UnknownError;`, conservando solo el nombre si llego a leerlo). Reproducido incluso con
una copia CASI IDENTICA de un `.plr` real que SI carga bien (`Eldelgas.plr`, solo con el nombre
cambiado) - descarta que sea un problema de "vaciar el equipo" (el flag `--limpiar`), apunta a
una discrepancia real en el ESCRITOR de bytes de `PlrBodySerializer`/`PlrFile.Write` para
`Version=326` frente a lo que espera el juego real. **Se investigo y se descarto la hipotesis
mas obvia** (padding AES): el decompilado real muestra que `Player.SavePlayer` cifra con
`RijndaelManaged` sin fijar `.Padding` (usa el PKCS7 por defecto de .NET, igual que
`Terrakeep.Core.PlrFormat.PlrCrypto`) y que `LoadPlayer` solo fija `PaddingMode.None` en el
DESCIFRADO para no perder los bytes de relleno finales - eso no puede romper una lectura A
MITAD del archivo, la causa real esta en otro punto de `PlrBodySerializer` no identificado
todavia. **Fuera de alcance de esta Fase 0** (instrumentar la captura, no arreglar el escritor
de produccion) - documentado aqui y en `bitacora.md` como hallazgo real para un encargo futuro.
Mientras no se arregle, **el lado vanilla de cualquier caso nuevo debe generarse con el propio
juego** (`SesionTerraria.crear_personaje_nuevo` + equipar a mano/via consola de administrador
dentro de una partida real, no con `preparar-caso`) - `preparar-caso` sigue siendo util para
alimentar SOLO el lado "terrakeep" con datos sinteticos si hace falta.

## Resultado del Caso 1 (personaje base sin nada puesto) - VEREDICTO: fiel a vanilla, NO es bug

Ver `capturas/caso01/`:
- `evidencia_captura_juego_real.png`: ventana completa del juego real (Terraria 1.4.5.8, v
  visible en la esquina), pantalla "Seleccionar jugador", con `ZZCaso1Real` (creado con la
  propia "Nuevo" del juego - evita el escritor de `.plr` roto de arriba) en la lista, SIN el
  prefijo `(UnknownError)` que sí muestran los intentos con `preparar-caso`.
- `vanilla.png`: recorte real de ese personaje (bbox automatico por color de fondo).
- `terrakeep.png`: mismo `.plr` (`ZZCaso1Real.plr`, ya borrado del `Players/` real del usuario
  tras terminar - reproducible en cualquier momento con `crear_personaje_nuevo`), renderizado
  por `ParidadVisual.exe renderizar` (pipeline de produccion real).
- `diff.png`/`diff_x12.png`: diferencia por pixel (ImageMagick `compare -metric AE`, fondo SIN
  normalizar - domina el ruido de fondo panel-vs-transparente, ver limite mas abajo). El area
  con forma de personaje muestra tonos mas claros (mejor coincidencia) que el fondo, SIN ninguna
  concentracion anomala justo en la zona de los pies frente al resto del cuerpo.
- `shoes_body0_frame0_x16.png`/`pants_body0_frame0_x16.png`/`legskin_body0_frame0_x16.png`/
  `pants_body0_frame1_x16.png`: inspeccion DIRECTA de los sprites fuente (sin pasar por captura
  de pantalla/renderer, para descartar sesgo de esos pasos) - `shoes.png` de `body0` es un
  silueta real de 2 pies separados (con hueco/transparencia entre ambos, igual que la captura
  real). `pants.png` (y `legskin.png`) comparten la MISMA silueta de base ancha en la fila
  inferior (mas ancha que `shoes.png`) - frame0 y frame1 son PIXEL IDENTICOS (confirmado
  programaticamente), descartando que sea un sangrado/mal recorte entre fotogramas de la tira
  animada `40x1120`.
- `vanilla_pies_x16.png`/`terrakeep_real_pies_x16.png`: zoom real x16 de la zona de los pies en
  ambos lados, mismo `.plr`.

**Conclusion**: el "suelo"/barra bajo los pies es la silueta REAL de pies/zapatos compartida por
`legskin`/`pants`/`shoes` (los 3 sprites reales extraidos de la instalacion vanilla de Steam,
mismo patron de extraccion ya usado por el resto del proyecto) - un personaje vanilla real
SIEMPRE dibuja esta base ancha bajo los pies, incluso sin ningun objeto equipado (es la silueta
de "zapato por defecto" del propio arte del juego, no un hueco/bug de extraccion). Se descarta
la hipotesis de "bug de extraccion de `shoes.png` recortado corto": el `shoes.png` real, cuando
se dibuja SOLO, es una silueta de 2 pies separados y coherente; la percepcion de "barra ancha
tipo suelo" viene de que `pants`/`legskin` (dibujados DEBAJO de `shoes`, orden real confirmado
en `PlayerPreviewRenderer.cs`/`LegacyPlayerRenderer.cs`) tienen una base algo mas ancha que se
asoma a los lados del hueco entre los 2 pies de `shoes` - comportamiento presente en AMBOS lados
(vanilla real y Terrakeep), no exclusivo de Terrakeep.

**Limite real de la medicion cuantitativa**: no se consiguio un `diff.png` con el fondo
perfectamente normalizado (el panel real de "Seleccionar jugador" tiene un degradado/vineta,
capturado con antialiasing de pantalla real, frente al canvas transparente y de bordes nitidos
de `PlayerPreviewRenderer` - un primer intento de sustituir el fondo por magenta en ambos lados
EMPEORO el AE por los pixeles de borde con antialiasing real que no cuadran con el `-fuzz`
usado). La evidencia DECISIVA de este caso es la comparacion visual directa (imagenes de arriba)
y la inspeccion de los sprites fuente, no el numero de AE en bruto.

## Casos 2-18

No completados en esta ronda por tiempo (esta Fase 0 es de infraestructura - prioridad real:
mecanismo funcionando + Caso 1 resuelto con certeza, ver el encargo). El mecanismo de arriba
(`ParidadVisual.exe renderizar` + `SesionTerraria` paso a paso) es reutilizable tal cual para
cualquiera de los 17 casos restantes - lo unico que cambia por caso es que equipo lleva puesto
el `.plr` de referencia (crear/editar el personaje dentro del propio juego real, nunca con
`preparar-caso` mientras su bug siga abierto).
