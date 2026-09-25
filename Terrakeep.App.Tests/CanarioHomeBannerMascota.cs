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
//   C/D) Ademas, en las tarjetas y en el banner la mascota tiene que quedar en la posicion REAL
//      que manda Terraria (PortSeleccion Encargo3, 25-sep-2026, SUSTITUYE la verificacion anterior
//      de este mismo bloque - un umbral de "% de pixeles tapados" solo demuestra que se VE algo,
//      nunca que este en el sitio correcto): borde inferior de la mascota alineado con el borde
//      inferior del doll, y desplazada 20px de juego reales (escalados al tamaño real del lienzo)
//      a la derecha del doll YA retranqueado - formula real, decompilado real,
//      Terraria/GameContent/UI/Elements/UICharacter.cs (GetPlayerPosition/DrawPets). Medido con
//      TransformToAncestor sobre el arbol visual REAL (funciona igual sea Margin o RenderTransform
//      el mecanismo usado para desplazar el doll, no asume ninguno concreto) - ver el comentario
//      completo de Terrakeep.App/Converters/PetPositionConverters.cs para la cita exacta y el
//      razonamiento completo.
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

            // === C: formula real de posicion mascota/doll DENTRO de la tarjeta (varias combinaciones reales) ===
            var conMascota = vm.Home.Characters.Where(c => c.PetImage != null).ToList();
            Console.WriteLine($"HOMEBANNER_SOLO: personajes reales escaneados con PetImage no-null en este equipo = {conMascota.Count} de {vm.Home.Characters.Count}");
            if (conMascota.Count == 0)
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE - ningun personaje real de este equipo tiene una mascota de vanidad resuelta ahora mismo, no se puede medir la formula real en tarjetas.");

            foreach (var c in conMascota)
            {
                var cardBorder = Descendientes<Border>(window).FirstOrDefault(b => ReferenceEquals(b.DataContext, c));
                if (cardBorder == null)
                {
                    Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} - AVISO, no se pudo ubicar su tarjeta real en el arbol visual (fuera del viewport/WrapPanel) - se omite la medicion de posicion para este personaje.");
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

                VerificarFormulaRealMascota(c.Name, grid, imgPet, imgDoll, w, c.PetOffsetX, c.PetOffsetY);
            }

            // === D: formula real de posicion mascota/doll DENTRO del banner "Continuar con X" (2x real) ===
            var entryBanner = vm.Home.LastSessionCharacterEntry;
            if (entryBanner?.PetImage == null)
            {
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE (banner) - LastSessionCharacterEntry sin mascota real equipada en este equipo ahora mismo, no se puede medir la formula real en el banner.");
            }
            else
            {
                var imgPetBanner = Descendientes<Image>(window).FirstOrDefault(img =>
                    ReferenceEquals(img.DataContext, entryBanner) &&
                    BindingOperations.GetBindingExpression(img, Image.SourceProperty)?.ParentBinding?.Path?.Path == "PetImage");
                var imgDollBanner = imagenPreviewBanner; // ya localizado arriba (A+B), Path=="Home.LastSessionCharacterEntry.Preview"
                if (imgPetBanner == null || imgDollBanner == null || VisualTreeHelper.GetParent(imgPetBanner) is not Grid gridBanner)
                {
                    Console.WriteLine("HOMEBANNER_SOLO: AVISO (banner) - no se pudo ubicar la pareja Image(PetImage)/Image(Preview) del banner dentro de la misma Grid - se omite.");
                }
                else
                {
                    double wBanner = gridBanner.ActualWidth;
                    if (wBanner <= 0)
                        Console.WriteLine("HOMEBANNER_SOLO: AVISO (banner) - Grid real sin medida (ActualWidth<=0) - se omite.");
                    else
                        VerificarFormulaRealMascota($"{entryBanner.Name} (banner)", gridBanner, imgPetBanner, imgDollBanner, wBanner, entryBanner.PetOffsetX, entryBanner.PetOffsetY);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("HOMEBANNER_SOLO-EXCEPTION: " + ex); }
    }

    // Verificacion geometrica real de la formula de Terraria (Terrakeep.App/Converters/
    // PetPositionConverters.cs, PortSeleccion Encargo3 25-sep-2026) - comun a tarjeta y banner
    // (mismo criterio pedido explicitamente). "40.0" es PlayerPreviewRenderer.Width, el ancho
    // NATIVO real del lienzo del doll (internal, sin InternalsVisibleTo hacia este arnes - mismo
    // patron real ya documentado en varios sitios de este mismo proyecto, ej.
    // ExplorationViewModel.cs/MainViewModel.cs) - "canvasScale" se MIDE de verdad
    // (grid.ActualWidth/40.0, nunca un "1.3"/"2.6" fijo a mano) para que este canario siga
    // midiendo lo correcto aunque cambie el tamaño real de la tarjeta/banner en el futuro.
    //
    // petOffsetX/petOffsetY (CharacterListEntryViewModel.PetOffsetX/PetOffsetY, PortSeleccion
    // Encargo4 25-sep-2026, capa DISTINTA y complementaria ya documentada en MainWindow.xaml
    // ~1505): el ajuste fino REAL propio de cada mascota (SettingsForCharacterPreview.Offset) se
    // aplica ENCIMA de la formula generica de Encargo3 via el mismo RenderTransform - la posicion
    // renderizada real que mide TransformToAncestor es la SUMA de las dos capas, asi que la
    // posicion "esperada" aqui tiene que incluir tambien este offset real y documentado (leido de
    // la propia ViewModel, nunca un numero a mano) para no confundir "el ajuste fino de Encargo4
    // esta activo" con "la formula generica de Encargo3 esta rota".
    private static void VerificarFormulaRealMascota(string etiqueta, Grid grid, Image imgPet, Image imgDoll, double anchoGridReal, double petOffsetX, double petOffsetY)
    {
        Point petTopLeft = imgPet.TransformToAncestor(grid).Transform(new Point(0, 0));
        Point dollTopLeft = imgDoll.TransformToAncestor(grid).Transform(new Point(0, 0));
        double petBottom = petTopLeft.Y + imgPet.ActualHeight;
        double dollBottom = dollTopLeft.Y + imgDoll.ActualHeight;
        double canvasScale = anchoGridReal / 40.0;

        double deltaBottom = Math.Abs(petBottom - dollBottom - petOffsetY);
        double offsetXReal = petTopLeft.X - dollTopLeft.X;
        double offsetXEsperado = 20.0 * canvasScale + petOffsetX;
        double deltaOffsetX = Math.Abs(offsetXReal - offsetXEsperado);

        Console.WriteLine($"HOMEBANNER_SOLO: {etiqueta} (canvasScale={canvasScale:0.###}, ajuste fino Encargo4 PetOffsetX={petOffsetX:0.##}/PetOffsetY={petOffsetY:0.##}) - borde inferior mascota={petBottom:0.##}, borde inferior doll={dollBottom:0.##} (delta={deltaBottom:0.##}px, tolerancia 1.5px), offsetX real mascota-doll={offsetXReal:0.##}px (esperado {offsetXEsperado:0.##}px segun formula, delta={deltaOffsetX:0.##}px)");

        if (deltaBottom > 1.5)
            Console.WriteLine($"FALLO: HOMEBANNER_SOLO - {etiqueta}: el borde inferior de la mascota ({petBottom:0.##}) no esta alineado con el borde inferior del doll ({dollBottom:0.##}) mas el ajuste fino real (PetOffsetY={petOffsetY:0.##}) - formula real de Terraria rota (UICharacter.cs, DrawPets: playerPosition+(0,player.height)+(0,-projectile.height))");
        if (deltaOffsetX > 1.5)
            Console.WriteLine($"FALLO: HOMEBANNER_SOLO - {etiqueta}: el desplazamiento horizontal real de la mascota respecto al doll ({offsetXReal:0.##}px) no coincide con los 20px de juego reales escalados mas el ajuste fino real ({offsetXEsperado:0.##}px, PetOffsetX={petOffsetX:0.##}) - formula real de Terraria rota (UICharacter.cs, GetPlayerPosition/DrawPets: -10f de retranqueo + (20,0) de offset)");
    }
}
