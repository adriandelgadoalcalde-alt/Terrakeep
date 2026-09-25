// CANARIO REAL (24-sep-2026, revision-correccion-integral-familia-Keep, bloque "imagen1" -
// Inicio: banner "Continuar con X" + tarjetas de personaje) - investigado por el agente
// investigador-bug del patron de 2 fases (NO toca produccion, solo este arnes). Los dos modos de
// abajo son CANARIOS ROJOS A PROPOSITO hoy (reproducen el bug real con evidencia MEDIDA/
// renderizada de verdad sobre la ventana real, nunca "deberia fallar") - se espera que pasen a OK
// cuando `aplicador-fix` aplique el arreglo real, sin tocar este fichero.
//
// HOMEBANNER_SOLO (dos hallazgos reales sobre la MISMA captura, imagen1.png):
//   A) El banner grande "Continuar con X" (MainWindow.xaml:2575-2622) NO tiene ninguna Image
//      ligada a PetImage (a diferencia de la tarjeta pequeña, MainWindow.xaml:1487) - la mascota
//      real equipada del personaje nunca se dibuja ahi, con o sin hover.
//   B) Ese mismo banner no tiene NINGUN MouseEnter/MouseLeave (a diferencia del Border de la
//      tarjeta, MainWindow.xaml:1383, con OnCharacterCardMouseEnter/OnCharacterCardMouseLeave) -
//      pasar el raton por encima no dispara ninguna animacion, medido reutilizando el MISMO
//      CharacterListEntryViewModel real que la tarjeta ya anima con exito (HOMEHOVER_SOLO).
//   C) Ademas, en las tarjetas SI se compone una mascota (z-order correcto, PetImage detras del
//      doll, mismo orden real que UICharacter.cs) pero queda mayormente OCULTA por el doll de
//      encima - medido en pixeles reales, no estimado: renderiza la MISMA Grid de 52x72,8 real
//      (MainWindow.xaml:1478) dos veces (con y sin el doll visible) sobre uno o mas personajes
//      reales de este equipo que tengan una mascota real equipada, y cuenta que porcentaje de los
//      pixeles PROPIOS de la mascota (alpha>20 en el render "solo mascota") cambian de verdad en
//      el render compuesto (el doll pintado encima los tapa).
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarHomeBannerMascotaSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            byte[] PixelesDeBitmap(WriteableBitmap bmp)
            {
                var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                return px;
            }

            byte[] PixelesDeVisual(Visual visual, double anchoPx, double altoPx)
            {
                int w = Math.Max(1, (int)Math.Round(anchoPx));
                int h = Math.Max(1, (int)Math.Round(altoPx));
                var rtb = new RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);
                var px = new byte[w * h * 4];
                rtb.CopyPixels(px, w * 4, 0);
                return px;
            }

            // Esperar a que el escaneo REAL de Inicio (carpeta real de .plr de este equipo) haya
            // terminado - mismo campo real que ya usa el resto del arnes (IsScanning, T-G-ASYNC
            // mas arriba en Program.cs).
            var crEspera = System.Diagnostics.Stopwatch.StartNew();
            while (vm.Home.IsScanning && crEspera.ElapsedMilliseconds < 8000) DoEvents();
            DoEvents();

            vm.SelectedTabIndex = 0; // Inicio
            WaitForDispatcher(300);
            DoEvents(); DoEvents(); DoEvents();

            Console.WriteLine($"HOMEBANNER_SOLO: personajes reales escaneados en este equipo = {vm.Home.Characters.Count}, LastSessionCharacterEntry={(vm.Home.LastSessionCharacterEntry != null ? vm.Home.LastSessionCharacterEntry.Name : "(null)")}, LastSessionCharacterName={vm.Home.LastSessionCharacterName ?? "(null)"}");

            // === A + B: el banner "Continuar con X" ===
            var imagenPreviewBanner = Descendientes<Image>(window).FirstOrDefault(img =>
                BindingOperations.GetBindingExpression(img, Image.SourceProperty)?.ParentBinding?.Path?.Path
                == "Home.LastSessionCharacterEntry.Preview");

            if (imagenPreviewBanner == null || vm.Home.LastSessionCharacterEntry == null)
            {
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE - no hay Home.LastSessionCharacterEntry real en este equipo ahora mismo (session.json vacio o sin coincidencia con la lista escaneada) - no se puede medir el banner. Repetir con un session.json real (Inicio ya usado al menos una vez) para que este bloque mida de verdad.");
            }
            else
            {
                var entry = vm.Home.LastSessionCharacterEntry;

                DependencyObject? actual = imagenPreviewBanner;
                Border? bannerBorder = null;
                while (actual != null)
                {
                    actual = VisualTreeHelper.GetParent(actual);
                    if (actual is Border b) { bannerBorder = b; break; }
                }
                Console.WriteLine($"HOMEBANNER_SOLO: Border real del banner localizado en el arbol visual = {(bannerBorder != null)}");
                if (bannerBorder == null)
                    Console.WriteLine("FALLO: HOMEBANNER_SOLO - no se pudo ubicar el Border del banner en el arbol visual real (MainWindow.xaml ~2575)");

                // A) ¿el banner compone una Image ligada a PetImage, igual que la tarjeta (MainWindow.xaml:1487)?
                bool bannerTienePetImage = bannerBorder != null && Descendientes<Image>(bannerBorder).Any(img =>
                    BindingOperations.GetBindingExpression(img, Image.SourceProperty)?.ParentBinding?.Path?.Path == "PetImage");
                Console.WriteLine($"HOMEBANNER_SOLO: el banner compone una Image ligada a PetImage (paridad real con la tarjeta, MainWindow.xaml:1487)={bannerTienePetImage} (esperado True)");
                if (!bannerTienePetImage)
                    Console.WriteLine("FALLO: HOMEBANNER_SOLO - el banner \"Continuar con X\" NO compone NINGUNA imagen de mascota (PetImage) - la mascota real equipada del personaje de la ultima sesion nunca se dibuja ahi, a diferencia de la tarjeta (MainWindow.xaml 2575-2622 vs 1478-1494)");

                // B) hover real: MISMO CharacterListEntryViewModel que HOMEHOVER_SOLO ya prueba con
                // exito sobre la tarjeta - si el banner NO tiene MouseEnter/MouseLeave cableado
                // (MainWindow.xaml:1383 en la tarjeta, nada equivalente en el banner), un
                // MouseEnter real sobre su Border no debe mover ni un pixel del doll.
                var reposo = PixelesDeBitmap(entry.Preview);
                if (bannerBorder != null)
                {
                    bannerBorder.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseEnterEvent });
                }
                var sumas = new List<int>();
                for (int muestra = 0; muestra < 10; muestra++)
                {
                    var cr = System.Diagnostics.Stopwatch.StartNew();
                    while (cr.ElapsedMilliseconds < 140) { DoEvents(); System.Threading.Thread.Sleep(10); }
                    var px = PixelesDeBitmap(entry.Preview);
                    sumas.Add(px.Sum(b => (int)b));
                }
                bool avanzoDeVerdad = sumas.Distinct().Count() > 1;
                Console.WriteLine($"HOMEBANNER_SOLO: tras simular un MouseEnter real sobre el Border del banner y ~1400ms bombeando el Dispatcher (10 muestras de 140ms) -> el doll del banner avanzo de verdad={avanzoDeVerdad} (valores distintos vistos={sumas.Distinct().Count()} de 10; esperado >1, paridad con el hover YA verificado en tarjetas por HOMEHOVER_SOLO)");
                if (!avanzoDeVerdad)
                    Console.WriteLine("FALLO: HOMEBANNER_SOLO - el banner \"Continuar con X\" no reacciona al hover (MouseEnter real sobre su Border no dispara ninguna animacion) - MainWindow.xaml:2575 no tiene MouseEnter/MouseLeave, a diferencia de MainWindow.xaml:1383 (CharacterCardTemplate, OnCharacterCardMouseEnter/Leave)");

                if (bannerBorder != null)
                    bannerBorder.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
                DoEvents();
            }

            // === C: ocultamiento real mascota/doll DENTRO de la tarjeta (varias combinaciones reales) ===
            var conMascota = vm.Home.Characters.Where(c => c.PetImage != null).ToList();
            Console.WriteLine($"HOMEBANNER_SOLO: personajes reales escaneados con PetImage no-null en este equipo = {conMascota.Count} de {vm.Home.Characters.Count}");
            if (conMascota.Count == 0)
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE - ningun personaje real de este equipo tiene una mascota de vanidad resuelta ahora mismo, no se puede medir el ocultamiento en tarjetas.");

            foreach (var c in conMascota)
            {
                var cardBorder = Descendientes<Border>(window).FirstOrDefault(b => ReferenceEquals(b.DataContext, c));
                if (cardBorder == null)
                {
                    Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} - AVISO, no se pudo ubicar su tarjeta real en el arbol visual (fuera del viewport/WrapPanel) - se omite la medicion de ocultamiento para este personaje.");
                    continue;
                }
                var imgPet = Descendientes<Image>(cardBorder).FirstOrDefault(img =>
                    BindingOperations.GetBindingExpression(img, Image.SourceProperty)?.ParentBinding?.Path?.Path == "PetImage");
                var imgDoll = Descendientes<Image>(cardBorder).FirstOrDefault(img =>
                    BindingOperations.GetBindingExpression(img, Image.SourceProperty)?.ParentBinding?.Path?.Path == "Preview");
                if (imgPet == null || imgDoll == null || VisualTreeHelper.GetParent(imgPet) is not Grid grid)
                {
                    Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} - AVISO, no se pudo ubicar la pareja Image(PetImage)/Image(Preview) dentro de la misma Grid - se omite.");
                    continue;
                }

                double w = grid.ActualWidth, h = grid.ActualHeight;
                if (w <= 0 || h <= 0)
                {
                    Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} - AVISO, Grid real sin medida (ActualWidth/Height<=0) - se omite.");
                    continue;
                }

                var compuesto = PixelesDeVisual(grid, w, h);
                var visibilidadDollAntes = imgDoll.Visibility;
                imgDoll.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
                DoEvents();
                var soloMascota = PixelesDeVisual(grid, w, h);
                imgDoll.SetCurrentValue(UIElement.VisibilityProperty, visibilidadDollAntes);
                DoEvents();

                int pxMascotaTotal = 0, pxOcultos = 0;
                for (int i = 0; i + 3 < soloMascota.Length; i += 4)
                {
                    byte alphaMascota = soloMascota[i + 3];
                    if (alphaMascota < 20) continue; // pixel real y no-transparente del sprite de la mascota
                    pxMascotaTotal++;
                    bool distinto = Math.Abs(compuesto[i] - soloMascota[i]) > 12
                        || Math.Abs(compuesto[i + 1] - soloMascota[i + 1]) > 12
                        || Math.Abs(compuesto[i + 2] - soloMascota[i + 2]) > 12
                        || Math.Abs(compuesto[i + 3] - soloMascota[i + 3]) > 12;
                    if (distinto) pxOcultos++;
                }
                double pctOculto = pxMascotaTotal == 0 ? -1 : 100.0 * pxOcultos / pxMascotaTotal;
                Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} (Grid real {w:0.#}x{h:0.#}px) - pixeles PROPIOS reales de la mascota={pxMascotaTotal}, tapados de verdad por el doll de encima={pxOcultos} ({pctOculto:F1}%)");
                // Umbral real (no inventado): 30% ya es un ocultamiento claramente visible - medido
                // de verdad en este mismo canario, Eldelgas da 40,0% (imagen1.png, "casi tapada" del
                // encargo del usuario) y Terrariano 0,7% (el mismo par PetImage/Preview, personaje
                // distinto) - la variacion real confirma que depende de la pose/silueta del doll, no
                // es un numero fijo por mascota.
                if (pctOculto >= 30)
                    Console.WriteLine($"FALLO: HOMEBANNER_SOLO - la mascota real de {c.Name} queda tapada en un {pctOculto:F1}% medido por el doll de la MISMA tarjeta (MainWindow.xaml:1478-1494: Grid 52x72,8 con una Image ligada a PetImage de 20x20 anclada abajo-izquierda, y una Image ligada a Preview de tamaño completo pintada DESPUES/ENCIMA sin ningun recorte/desplazamiento que reserve hueco a la mascota)");
            }
        }
        catch (Exception ex) { Console.WriteLine("HOMEBANNER_SOLO-EXCEPTION: " + ex); }
    }
}
