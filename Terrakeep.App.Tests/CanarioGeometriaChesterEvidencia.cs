// EVIDENCIA REAL por capas (GEOMETRY_EXACT) - GapAnalysis ParidadPersonaje (26-sep-2026,
// requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514, segunda ronda de la reapertura de Chester,
// tras la arquitectura nueva PlayerPetPreviewLayout/PlayerPetPreviewControl). Pedido explicito del
// coordinador: capturas reales, SIN auto-crop/resize/normalizacion de bounding box entre ellas -
// las 3 capas (A jugador solo, B Chester solo, C composicion) se renderizan del MISMO
// PlayerPetPreviewControl real (mismo Width/Height, mismo viewport logico), alternando que capa
// esta visible - nunca recortando/ajustando cada imagen por separado despues.
//
// CORRECCION real (2-oct-2026, encargo "arreglar el canario para que de un resultado
// concluyente"): esta version ya NO depende de Terrariano.plr real. Dos fallos reales, los dos ya
// resueltos:
//
// 1) INCONCLUSIVE sistematico - el canario buscaba la ruta REAL de Documents\My Games\Terraria\
//    Players\Terrariano.plr (rutaReal) dentro de vm.Home.Characters, pero
//    AislamientoPartidasReales.cs (PrepararAislamientoPartidasReales, Program.cs) ya sustituye
//    CharacterFileService.CarpetasPersonajesDePrueba por la carpeta temporal del arnes ANTES de
//    construir la MainWindow - Terrariano.plr real SI se copia ahi (mismo nombre de archivo), pero
//    su FilePath real pasa a ser la COPIA (<temp>\Players\Terrariano.plr), nunca la ruta de
//    Documents. La comparacion contra rutaReal (la ruta de Documents, nunca tocada por el arnes a
//    proposito - regla del CLAUDE.md del repo) no encontraba NUNCA ninguna entrada, caiga o no la
//    ejecucion en una maquina que tenga ese personaje real. Arreglo real: fixture SINTETICO
//    (PersonajeChesterGeometria, "Eye Bone" vanilla real - ItemID 5098, confirmado contra
//    Assets\vanilla_item_names_en.json/Assets\pet_animations.json: id 5098, animado, 7
//    fotogramas de ciclo, no es mascota de luz - resuelve a Chester via
//    EquipmentAppearanceResolver.ResolvePet igual que lo haria el objeto real) en una carpeta
//    aislada PROPIA de este metodo, mismo mecanismo que PruebasCapturasReadme.cs ("Aventurero"):
//    CharacterFileService.CarpetasPersonajesDePrueba se SUSTITUYE (nunca se suma) solo durante
//    este bloque y se restaura en el finally - nunca toca ninguna carpeta real, ni siquiera la ya
//    aislada por el arnes.
//
// 2) Carrera real del canario contra su propio temporizador de animacion (reportado por un
//    revisor visual sobre A_jugador_solo_hover.png: Chester reaparecia pese al Collapse manual -
//    el codigo de PRODUCCION, PlayerPetPreviewControl.RecomputeLayout(), esta bien, el bug era
//    solo del arnes). Causa real: CharacterListEntryViewModel arranca un DispatcherTimer propio
//    (_hoverWalkTimer, intervalo 70ms) mientras SetHovering(true) esta activo - cada Tick llama
//    RefreshPreview/RefreshPetImage/RefreshPetOffset, que cambian PreviewSource/PetImageSource/
//    PetOffsetX/Y (DependencyProperty ligadas por Binding) y eso dispara
//    OnLayoutAffectingPropertyChanged -> RecomputeLayout(), que SIEMPRE deja
//    _petImage.Visibility=Visible cuando hasPet es true (linea real de PlayerPetPreviewControl.cs,
//    rama "if (hasPet && ...)"). Capturar() espera DoEvents()+WaitForDispatcher(60)+DoEvents()
//    antes de renderizar - 60ms es mas que suficiente para que el timer de 70ms dispare AL MENOS
//    una vez durante esa espera en una fraccion real de ejecuciones, deshaciendo el
//    "petImg.Visibility = Visibility.Collapsed" que CapturarLasTresCapas acababa de poner un
//    instante antes para la capa A. Arreglo real: parar DE VERDAD el DispatcherTimer privado del
//    entry (via reflexion sobre el campo _hoverWalkTimer - no hay ningun metodo publico para
//    pausarlo sin tambien reiniciar el ciclo/offset, SetHovering(false) hace ambas cosas) justo
//    antes de capturar las 3 capas en estado hover, para que ningun Tick pueda volver a disparar
//    RecomputeLayout mientras el Collapse manual esta en vigor. El frame de hover ya se fijo antes
//    de parar el timer (se deja avanzar en tiempo real unos cientos de ms primero), asi que la
//    captura SI muestra una pose de "andar" real, solo que congelada de forma determinista para
//    poder fotografiarla sin que se mueva sola a mitad de la captura.
//
// Caso oracle ORIGINAL (documentado por honestidad, ya no se usa en esta version): Terrariano.plr
// real (Documents\My Games\Terraria\Players\Terrariano.plr, Eye Bone/Chester equipado) - SOLO
// LECTURA, nunca modificado por este canario en ninguna version.
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

internal static partial class Program
{
    private static void EjecutarGeometriaChesterEvidenciaSolo(MainWindow window, MainViewModel vm)
    {
        var carpetasPersonajesAnteriores = CharacterFileService.CarpetasPersonajesDePrueba;
        string? tempDir = null;
        try
        {
            var crEspera = System.Diagnostics.Stopwatch.StartNew();
            while (vm.Home.IsScanning && crEspera.ElapsedMilliseconds < 8000) DoEvents();
            DoEvents();

            vm.SelectedTabIndex = 0; // Inicio
            WaitForDispatcher(300);
            DoEvents(); DoEvents(); DoEvents();

            // Fixture sintetico (ver el comentario largo de cabecera, punto 1) - carpeta PROPIA,
            // aislada, nunca una carpeta real ni siquiera la ya aislada por el arnes (que sigue
            // siendo CarpetasPersonajesDePrueba hasta este punto, puesta por
            // PrepararAislamientoPartidasReales antes de construir la MainWindow).
            tempDir = Path.Combine(Path.GetTempPath(), "terrakeep-chester-geometria-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            string plrPath = Path.Combine(tempDir, "PersonajeChesterGeometria.plr");
            var personaje = new PlrCharacter
            {
                Version = 279,
                Name = "PersonajeChesterGeometria",
                Difficulty = 0,
                HealthMax = 400,
                HealthNow = 400,
                ManaMax = 200,
                ManaNow = 200,
                HairColor = [90, 60, 35],
                SkinColor = [255, 200, 165],
                EyeColor = [105, 90, 75],
                ShirtColor = [175, 165, 140],
                UnderColor = [85, 85, 180],
                PantsColor = [170, 140, 90],
                ShoesColor = [130, 90, 60],
                // Slot 0 de EquipmentItems = Mascota (EquipmentAppearanceResolver.ResolvePet lee
                // equipmentItems[0]) - 5098 es "Eye Bone" vanilla real (confirmado contra
                // Assets\vanilla_item_names_en.json), el objeto que summonea a Chester. Assets\
                // pet_animations.json tiene una entrada animada real para el id 5098 (7 fotogramas,
                // no es mascota de luz) - exactamente el mismo camino de animacion/hover que
                // Terrariano.plr real ejercitaba, sin depender de ese archivo.
                EquipmentItems = [new PlrItemSlot(5098, 1, 0, false), PlrItemSlot.Empty, PlrItemSlot.Empty, PlrItemSlot.Empty, PlrItemSlot.Empty],
                EquipmentDyes = new PlrItemSlot[5],
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            };
            File.WriteAllBytes(plrPath, PlrFile.Write(personaje));

            // SUSTITUYE (nunca suma) las carpetas de personajes mientras dura este bloque -
            // restaurado en el finally. Dispara un refresco real de Inicio por el MISMO camino que
            // usaria la app (HomeViewModel.RefreshAsync/ScanCharacters), para que la entrada que se
            // mida sea una CharacterListEntryViewModel real construida por el escaneo, no una
            // instanciada a mano.
            CharacterFileService.CarpetasPersonajesDePrueba = [tempDir];
            var refresco = vm.Home.RefreshCommand.ExecuteAsync(null);
            while (!refresco.IsCompleted) DoEvents();
            DoEvents(); DoEvents();

            var entry = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, plrPath, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"CHESTER_GEOMETRIA_SOLO: fixture sintetico localizado en el escaneo de Inicio={(entry != null)} (ruta={plrPath}), PetImage real={(entry?.PetImage != null)}");

            if (entry == null || entry.PetImage == null)
            {
                Console.WriteLine("CHESTER_GEOMETRIA_SOLO: FALLO - el escaneo de Inicio no devolvio el fixture sintetico de Chester (PersonajeChesterGeometria.plr) con PetImage real resuelto - no se puede generar la evidencia por capas.");
                return;
            }

            var control = Descendientes<PlayerPetPreviewControl>(window)
                .FirstOrDefault(pc => ReferenceEquals(pc.DataContext, entry) && pc.CanvasScale == 1.3);
            if (control == null)
            {
                Console.WriteLine("CHESTER_GEOMETRIA_SOLO: FALLO - no se pudo ubicar el PlayerPetPreviewControl real de la tarjeta del fixture sintetico en el arbol visual (fuera del viewport/WrapPanel?).");
                return;
            }

            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "geometria-chester");
            Directory.CreateDirectory(outDir);

            void Capturar(string nombre)
            {
                DoEvents(); WaitForDispatcher(60); DoEvents();
                var rtb = new RenderTargetBitmap(
                    Math.Max(1, (int)Math.Ceiling(control.ActualWidth)),
                    Math.Max(1, (int)Math.Ceiling(control.ActualHeight)), 96, 96, PixelFormats.Pbgra32);
                rtb.Render(control);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                string ruta = Path.Combine(outDir, nombre + ".png");
                using (var fs = File.Create(ruta)) enc.Save(fs);
                Console.WriteLine($"CHESTER_GEOMETRIA_SOLO: capturado {ruta} ({rtb.PixelWidth}x{rtb.PixelHeight}, control real {control.ActualWidth:0.##}x{control.ActualHeight:0.##} WPF, CanvasScale={control.CanvasScale})");
            }

            void CapturarLasTresCapas(string sufijo)
            {
                var petImg = (Image)control.Children[0];
                var playerImg = (Image)control.Children[1];
                var visibilidadPetOriginal = petImg.Visibility;
                var visibilidadPlayerOriginal = playerImg.Visibility;

                // A) jugador SOLO (pet oculta, MISMO Width/Height del control - mismo viewport
                // logico que las otras 2 capas, sin recorte/normalizacion independiente).
                petImg.Visibility = Visibility.Collapsed;
                Capturar($"A_jugador_solo{sufijo}");

                // B) Chester SOLO (jugador oculto).
                petImg.Visibility = visibilidadPetOriginal;
                playerImg.Visibility = Visibility.Collapsed;
                Capturar($"B_chester_solo{sufijo}");

                // C) composicion real de Terrakeep (las 2 capas, estado normal).
                playerImg.Visibility = visibilidadPlayerOriginal;
                Capturar($"C_composicion_terrakeep{sufijo}");
            }

            Console.WriteLine("CHESTER_GEOMETRIA_SOLO: === estado ESTATICO (sin hover) ===");
            CapturarLasTresCapas("_estatico");

            Console.WriteLine("CHESTER_GEOMETRIA_SOLO: === estado HOVER (Andar real) ===");
            entry.SetHovering(true);
            var crHover = System.Diagnostics.Stopwatch.StartNew();
            while (crHover.ElapsedMilliseconds < 500) { DoEvents(); System.Threading.Thread.Sleep(10); }

            // Arreglo real de la carrera (ver el comentario largo de cabecera, punto 2): para DE
            // VERDAD el DispatcherTimer privado de la tarjeta antes de tocar Visibility a mano y
            // capturar - si no, un Tick real (cada 70ms) puede caer durante el
            // DoEvents()+WaitForDispatcher(60) de Capturar() y disparar RecomputeLayout(), que
            // siempre deja _petImage.Visibility=Visible mientras hasPet sea true, deshaciendo el
            // Collapse manual de la capa A a mitad de la captura. El frame de hover YA quedo fijado
            // por los ~500ms reales de arriba - esto solo evita que siga avanzando/reconciliando
            // layout MIENTRAS se fotografia, nunca cambia que frame se vea.
            var campoTimer = typeof(CharacterListEntryViewModel).GetField("_hoverWalkTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timerReal = campoTimer?.GetValue(entry) as DispatcherTimer;
            timerReal?.Stop();
            Console.WriteLine($"CHESTER_GEOMETRIA_SOLO: DispatcherTimer de hover localizado y parado antes de capturar={timerReal != null} (evita la carrera real contra RecomputeLayout durante la captura).");

            CapturarLasTresCapas("_hover");
            entry.SetHovering(false); // detiene el timer (ya parado) y reinicia el driver de la mascota, estado limpio
            DoEvents();

            // D) Terraria Vanilla real - LIMITE REAL, documentado con honestidad (pedido explicito
            // del coordinador: no inventar esta capa si no existe de verdad). Se reviso
            // scripts/ParidadVisual/capturas/caso01/vanilla.png (unica captura vanilla real de
            // esta sesion) - es un caso DISTINTO (pose de apariencia/skin, sin mascota equipada,
            // "Caso1" de ParidadVisual, nada que ver con Chester/UICharacter) - reutilizarla aqui
            // seria una comparacion falsa (viewport/escena distintos), no se hace. Un intento
            // NUEVO de capturar la pantalla real de seleccion de personaje con Terraria 1.4.5.8
            // vanilla (mismo mecanismo, capturar_vanilla.py/SesionTerraria) esta documentado en
            // esta misma bitacora.md (entrada "Fase6", horas antes de esta) como BLOQUEADO por
            // motivos de entorno (ventana en blanco, 4 intentos con tecnicas distintas, sesion
            // real de Terraria congelada en un tip de carga) - no se reintenta aqui dentro del
            // alcance/tiempo de este encargo (arreglo de geometria dentro de Terrakeep, no
            // infraestructura de captura de Terraria real). D queda ausente, documentado con
            // honestidad, no inventado.
            Console.WriteLine("CHESTER_GEOMETRIA_SOLO: D) captura de Terraria Vanilla real - AUSENTE, LIMITE REAL documentado (ver comentario de este metodo y bitacora.md, entrada Fase6 - capturar_vanilla.py bloqueado en esta sesion por motivos de entorno ajenos a este encargo). No se reutiliza scripts/ParidadVisual/capturas/caso01/vanilla.png porque es un caso distinto (pose de apariencia sin mascota, viewport distinto) - reutilizarla seria una comparacion GEOMETRY_EXACT falsa.");
        }
        catch (Exception ex) { Console.WriteLine("CHESTER_GEOMETRIA_SOLO-EXCEPTION: " + ex); }
        finally
        {
            // Nunca deja CarpetasPersonajesDePrueba apuntando a la carpeta de este fixture una vez
            // terminado el bloque - restaura exactamente lo que ya habia puesto
            // PrepararAislamientoPartidasReales (la carpeta aislada del arnes, nunca una real).
            CharacterFileService.CarpetasPersonajesDePrueba = carpetasPersonajesAnteriores;
            if (tempDir != null) { try { Directory.Delete(tempDir, recursive: true); } catch { } }
        }
    }
}
