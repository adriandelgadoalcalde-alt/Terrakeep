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
//      que manda Terraria: borde inferior de la mascota alineado con el borde inferior del doll
//      (mas el ajuste vertical real de Terraria, SpriteVerticalFudge=4, y el offset propio de
//      cada mascota), y desplazada a la derecha del doll YA retranqueado segun la formula real -
//      formula real, decompilado real, Terraria/GameContent/UI/Elements/UICharacter.cs
//      (GetPlayerPosition/DrawPets). Medido con TransformToAncestor sobre el arbol visual REAL.
//
//      ACTUALIZADO 26-sep-2026 (GapAnalysis ParidadPersonaje, requirement
//      480a9bdd-6d5f-4fa6-935d-46f895e97514, segunda ronda de la reapertura de Chester): el
//      converter WPF que este bloque verificaba (Converters/PetPositionConverters.cs) se retiro
//      por completo - la formula real vive ahora en Terrakeep.Core/Layout/
//      PlayerPetPreviewLayout.cs (motor puro sin WPF) y se consume desde
//      Terrakeep.App/Controls/PlayerPetPreviewControl.cs (un unico control, Canvas con 2 Image
//      internas, sin bindings XAML sobre esas 2 Image - las posiciona el propio control en
//      codigo). El "offsetXEsperado = 20.0*canvasScale + petOffsetX" hardcodeado que vivia AQUI
//      (nota historica de una ronda anterior, ya corregida entonces de 10 a 20 pero seguia sin
//      contar el "SpriteVerticalFudge" real de 4 nativos en el eje Y) se sustituye por la MISMA
//      PlayerPetPreviewLayout que ya usa produccion - fuente de verdad unica, no una segunda
//      formula duplicada a mano que puede desincronizarse otra vez.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Layout;

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
            // Ancla real: el UNICO PlayerPetPreviewControl con CanvasScale==2.6 (el banner, "el
            // doble exacto de la tarjeta" - mismo criterio ya documentado en todo el proyecto) -
            // ya no se puede anclar por ruta de binding literal (la tarjeta y el banner usan la
            // MISMA ruta corta "Preview"/"PetImage" ahora que el control del banner tiene su
            // propio DataContext re-atado, ver MainWindow.xaml) asi que CanvasScale (leido del
            // propio valor resuelto de la DependencyProperty, sea literal o binding) es la forma
            // real de distinguir el control del banner del de cualquier tarjeta (1.3).
            var controlBanner = Descendientes<PlayerPetPreviewControl>(window).FirstOrDefault(c => c.CanvasScale == 2.6);

            if (controlBanner == null || vm.Home.LastSessionCharacterEntry == null)
            {
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE - no hay Home.LastSessionCharacterEntry real en este equipo ahora mismo (session.json vacio o sin coincidencia con la lista escaneada) - no se puede medir el banner. Repetir con un session.json real (Inicio ya usado al menos una vez) para que este bloque mida de verdad.");
            }
            else
            {
                var entry = vm.Home.LastSessionCharacterEntry;

                DependencyObject? actual = controlBanner;
                Border? bannerBorder = null;
                while (actual != null)
                {
                    actual = VisualTreeHelper.GetParent(actual);
                    if (actual is Border b) { bannerBorder = b; break; }
                }
                Console.WriteLine($"HOMEBANNER_SOLO: Border real del banner localizado en el arbol visual = {(bannerBorder != null)}");
                if (bannerBorder == null)
                    Console.WriteLine("FALLO: HOMEBANNER_SOLO - no se pudo ubicar el Border del banner en el arbol visual real (MainWindow.xaml ~2645)");

                // A) ¿el banner compone un PlayerPetPreviewControl con PetImageSource real, igual
                // que la tarjeta (paridad real, MISMO control en los 2 sitios)?
                bool bannerTienePetImage = controlBanner.PetImageSource is not null;
                Console.WriteLine($"HOMEBANNER_SOLO: el banner compone un PlayerPetPreviewControl con PetImageSource real (paridad con la tarjeta)={bannerTienePetImage} (esperado True)");
                if (!bannerTienePetImage)
                    Console.WriteLine("FALLO: HOMEBANNER_SOLO - el banner \"Continuar con X\" NO compone ninguna mascota (PetImageSource null) - la mascota real equipada del personaje de la ultima sesion nunca se dibuja ahi, a diferencia de la tarjeta (MainWindow.xaml ~2663 vs ~1497)");

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
                var control = Descendientes<PlayerPetPreviewControl>(window)
                    .FirstOrDefault(pc => ReferenceEquals(pc.DataContext, c) && pc.CanvasScale == 1.3);
                if (control == null)
                {
                    Console.WriteLine($"HOMEBANNER_SOLO: {c.Name} - AVISO, no se pudo ubicar su PlayerPetPreviewControl real en el arbol visual (fuera del viewport/WrapPanel) - se omite la medicion de posicion para este personaje.");
                    continue;
                }

                VerificarFormulaRealMascota(c.Name, control);
            }

            // === D: formula real de posicion mascota/doll DENTRO del banner "Continuar con X" (2x real) ===
            var entryBanner = vm.Home.LastSessionCharacterEntry;
            if (entryBanner?.PetImage == null)
            {
                Console.WriteLine("HOMEBANNER_SOLO: INCONCLUSIVE (banner) - LastSessionCharacterEntry sin mascota real equipada en este equipo ahora mismo, no se puede medir la formula real en el banner.");
            }
            else if (controlBanner != null)
            {
                VerificarFormulaRealMascota($"{entryBanner.Name} (banner)", controlBanner);
            }
        }
        catch (Exception ex) { Console.WriteLine("HOMEBANNER_SOLO-EXCEPTION: " + ex); }
    }

    // Verificacion geometrica real de la formula de Terraria - comun a tarjeta y banner (mismo
    // control real, PlayerPetPreviewControl, en los 2 sitios).
    //
    // ACTUALIZADO 26-sep-2026 (GapAnalysis ParidadPersonaje, requirement
    // 480a9bdd-6d5f-4fa6-935d-46f895e97514): la "formula esperada" ya NO se re-deriva a mano aqui
    // (la version anterior, "20.0*canvasScale+petOffsetX" para X y "petOffsetY" a secas para el
    // delta de Y, no contaba el SpriteVerticalFudge real de 4 nativos de la formula de dibujo del
    // sprite - PlayerDrawLayers.cs:1991/1187 - un hueco real que este mismo agente encontro al
    // reescribir este canario) - se calcula con Terrakeep.Core.Layout.PlayerPetPreviewLayout, la
    // MISMA fuente de verdad que ya usa produccion (PlayerPetPreviewControl), para que esta prueba
    // nunca vuelva a desincronizarse de la formula real por una segunda copia a mano.
    //
    // control.CanvasScale/PetOffsetX/PetOffsetY se LEEN directos del propio control (valores YA
    // resueltos, sea binding o literal) - PixelWidth/PixelHeight reales del bitmap de la mascota
    // (Children[0], el Image interno de la mascota - ver el orden Z real documentado en
    // PlayerPetPreviewControl.cs) para las dimensiones nativas del fotograma.
    private static void VerificarFormulaRealMascota(string etiqueta, PlayerPetPreviewControl control)
    {
        if (control.PetImageSource is not BitmapSource petBitmap)
        {
            Console.WriteLine($"HOMEBANNER_SOLO: {etiqueta} - AVISO, el control real no tiene PetImageSource resuelto todavia (frame de animacion aun no calculado) - se omite.");
            return;
        }

        var petImage = (Image)control.Children[0];
        var playerImage = (Image)control.Children[1];
        Point petTopLeft = petImage.TransformToAncestor(control).Transform(new Point(0, 0));
        Point dollTopLeft = playerImage.TransformToAncestor(control).Transform(new Point(0, 0));
        double petBottom = petTopLeft.Y + petImage.ActualHeight;
        double dollBottom = dollTopLeft.Y + playerImage.ActualHeight;
        double canvasScale = control.CanvasScale;

        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var spriteEsperado = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);
        var petEsperado = PlayerPetPreviewLayout.PetBounds(origen, control.PetOffsetX, control.PetOffsetY, petBitmap.PixelWidth, petBitmap.PixelHeight);

        double deltaBottomEsperadoNativo = petEsperado.Bottom - spriteEsperado.Bottom;
        double offsetXEsperadoNativo = petEsperado.Left - spriteEsperado.Left;
        double deltaBottomEsperado = deltaBottomEsperadoNativo * canvasScale;
        double offsetXEsperado = offsetXEsperadoNativo * canvasScale;

        double deltaBottomReal = petBottom - dollBottom;
        double offsetXReal = petTopLeft.X - dollTopLeft.X;
        double deltaBottom = Math.Abs(deltaBottomReal - deltaBottomEsperado);
        double deltaOffsetX = Math.Abs(offsetXReal - offsetXEsperado);

        Console.WriteLine($"HOMEBANNER_SOLO: {etiqueta} (canvasScale={canvasScale:0.###}, PetOffsetX={control.PetOffsetX:0.##}/PetOffsetY={control.PetOffsetY:0.##}) - delta borde inferior mascota-doll real={deltaBottomReal:0.##}px (esperado {deltaBottomEsperado:0.##}px, delta={deltaBottom:0.##}px, tolerancia 1.5px), offsetX real mascota-doll={offsetXReal:0.##}px (esperado {offsetXEsperado:0.##}px, delta={deltaOffsetX:0.##}px)");

        if (deltaBottom > 1.5)
            Console.WriteLine($"FALLO: HOMEBANNER_SOLO - {etiqueta}: el borde inferior de la mascota no coincide con PlayerPetPreviewLayout (real={deltaBottomReal:0.##}px, esperado={deltaBottomEsperado:0.##}px) - formula real de Terraria rota.");
        if (deltaOffsetX > 1.5)
            Console.WriteLine($"FALLO: HOMEBANNER_SOLO - {etiqueta}: el desplazamiento horizontal de la mascota respecto al doll no coincide con PlayerPetPreviewLayout (real={offsetXReal:0.##}px, esperado={offsetXEsperado:0.##}px) - formula real de Terraria rota.");
    }
}
