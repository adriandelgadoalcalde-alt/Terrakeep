// EVIDENCIA REAL por capas (GEOMETRY_EXACT) - GapAnalysis ParidadPersonaje (26-sep-2026,
// requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514, segunda ronda de la reapertura de Chester,
// tras la arquitectura nueva PlayerPetPreviewLayout/PlayerPetPreviewControl). Pedido explicito del
// coordinador: capturas reales, SIN auto-crop/resize/normalizacion de bounding box entre ellas -
// las 3 capas (A jugador solo, B Chester solo, C composicion) se renderizan del MISMO
// PlayerPetPreviewControl real (mismo Width/Height, mismo viewport logico), alternando que capa
// esta visible - nunca recortando/ajustando cada imagen por separado despues.
//
// Caso oracle: Terrariano.plr real (Documents\My Games\Terraria\Players\Terrariano.plr, Eye
// Bone/Chester equipado) - SOLO LECTURA, nunca modificado (el propio escaneo real de Inicio ya lo
// carga sin que este arnes lo toque).
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarGeometriaChesterEvidenciaSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var crEspera = System.Diagnostics.Stopwatch.StartNew();
            while (vm.Home.IsScanning && crEspera.ElapsedMilliseconds < 8000) DoEvents();
            DoEvents();

            vm.SelectedTabIndex = 0; // Inicio
            WaitForDispatcher(300);
            DoEvents(); DoEvents(); DoEvents();

            string rutaReal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "Players", "Terrariano.plr");
            var entry = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, rutaReal, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"CHESTER_GEOMETRIA_SOLO: Terrariano.plr real localizado en el escaneo de Inicio={(entry != null)} (ruta={rutaReal}), PetImage real={(entry?.PetImage != null)}");

            if (entry == null || entry.PetImage == null)
            {
                Console.WriteLine("CHESTER_GEOMETRIA_SOLO: INCONCLUSIVE - no se pudo ubicar Terrariano.plr con PetImage real resuelto en el escaneo de este equipo - no se puede generar la evidencia por capas.");
                return;
            }

            var control = Descendientes<PlayerPetPreviewControl>(window)
                .FirstOrDefault(pc => ReferenceEquals(pc.DataContext, entry) && pc.CanvasScale == 1.3);
            if (control == null)
            {
                Console.WriteLine("CHESTER_GEOMETRIA_SOLO: FALLO - no se pudo ubicar el PlayerPetPreviewControl real de la tarjeta de Terrariano en el arbol visual (fuera del viewport/WrapPanel?).");
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
            CapturarLasTresCapas("_hover");
            entry.SetHovering(false);
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
    }
}
