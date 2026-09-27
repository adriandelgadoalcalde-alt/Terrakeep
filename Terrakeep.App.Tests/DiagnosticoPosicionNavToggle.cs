// DIAGNOSTICO REAL (26-sep-2026, investigador-bug del patron de 2 fases, TASK CONTEXT del
// coordinador - reporte del usuario "los botones 1/2/3 de Objetos en Terrakeep estan mas abajo de
// lo que deberian"). Este fichero NO toca produccion (regla fija del rol) - solo mide con UI real
// la posicion del StackPanel flotante "ObjetosNavToggle" (RadioButton 1/2/3, MainWindow.xaml
// ~3525-3545) contra su contenedor real, para confirmar o descartar con numeros la causa
// geometrica exacta antes de pasarle el hallazgo a un aplicador-fix.
//
// Hipotesis a verificar con evidencia real (no "deberia fallar"):
//   H1: el StackPanel flotante SI esta pegado al borde superior de su celda Grid compartida con
//       ObjetosPageHost (VerticalAlignment="Top", Margin superior=4) - si esto es cierto, el
//       "mas abajo" no es un offset propio del StackPanel, sino que la celda entera empieza mas
//       abajo de lo que el usuario espera (visible arriba de la pagina real).
//   H2: el padre inmediato de ObjetosNavToggle (el Grid sin nombre de MainWindow.xaml:2949) se
//       ESTIRA para llenar TODO el alto de la fila "3*" MinHeight=216 del tablero de Objetos
//       (MainWindow.xaml:2896) en vez de limitarse al alto real del contenido (Auto) - si la fila
//       "3*" mide mucho mas que el contenido real de la pagina activa, el punto "Top" de esa celda
//       puede seguir estando en el mismo Y de pantalla que el resto (no explicaria un defecto) O
//       puede haber DESALINEACION si algun ancestro intermedio no comparte el mismo origen.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarDiagnosticoPosicionNavToggleSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault(c => c.Name == "Eldelgas") ?? vm.Home.Characters.FirstOrDefault();
            if (personajeReal == null)
            {
                Console.WriteLine("FALLO: DIAG-NAVTOGGLE-POS - no hay ningun personaje real en Home.Characters para cargar.");
                return;
            }
            vm.Home.OpenCommand.Execute(personajeReal);
            DoEvents(); DoEvents();
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; // Personaje > Objetos
            DoEvents(); DoEvents();

            // ADR-TERRAKEEP-016/031 (27-sep-2026): Objetos vive ahora dentro de ObjetosView
            // (NameScope propio) - patron de FindName DOBLE ya usado por el resto de la familia.
            var objetosViewHost = window.FindName("ObjetosView") as FrameworkElement;
            var toggle = objetosViewHost?.FindName("ObjetosNavToggle") as FrameworkElement;
            var pageHost = objetosViewHost?.FindName("ObjetosPageHost") as FrameworkElement;
            var seccionEquip = objetosViewHost?.FindName("ObjetosSeccionEquipamiento") as FrameworkElement;
            var t1 = objetosViewHost?.FindName("ObjetosNavToggle1") as FrameworkElement;
            if (toggle == null || pageHost == null || seccionEquip == null || t1 == null)
            {
                Console.WriteLine($"FALLO: DIAG-NAVTOGGLE-POS - elementos no encontrados (toggle={toggle != null}, pageHost={pageHost != null}, seccionEquip={seccionEquip != null}, t1={t1 != null})");
                return;
            }
            var celda = VisualTreeHelper.GetParent(toggle) as FrameworkElement; // Grid sin nombre, MainWindow.xaml:2949
            var celdaPageHost = VisualTreeHelper.GetParent(pageHost) as FrameworkElement;
            Console.WriteLine($"DIAG-NAVTOGGLE-POS: padre real de ObjetosNavToggle == padre real de ObjetosPageHost -> {ReferenceEquals(celda, celdaPageHost)} (esperado True, misma celda Grid compartida)");

            var tituloEquip = PrimerDescendienteTextBlock(seccionEquip);
            if (tituloEquip == null)
            {
                Console.WriteLine("FALLO: DIAG-NAVTOGGLE-POS - no se encuentra el TextBlock del titulo 'Equipamiento' dentro de ObjetosSeccionEquipamiento.");
                return;
            }

            void Medir(string etiqueta, double ancho, double alto)
            {
                FijarTamaño(window, ancho, alto);
                DoEvents(); DoEvents(); DoEvents();

                Rect rToggle = RectCompletoPublico(toggle, window);
                Rect rT1 = RectCompletoPublico(t1, window);
                Rect rPageHost = RectCompletoPublico(pageHost, window);
                Rect rSeccion = RectCompletoPublico(seccionEquip, window);
                Rect rCelda = celda != null ? RectCompletoPublico(celda, window) : Rect.Empty;
                Rect rTitulo = RectCompletoPublico(tituloEquip, window);

                double centroToggle = rT1.Y + rT1.Height / 2.0;
                double centroTitulo = rTitulo.Y + rTitulo.Height / 2.0;
                double deltaCentros = centroToggle - centroTitulo;

                Console.WriteLine($"DIAG-NAVTOGGLE-POS[{etiqueta} {ancho}x{alto}]: " +
                    $"celda(padre compartido) Y={rCelda.Y:0.0} Alto={rCelda.Height:0.0} | " +
                    $"ObjetosPageHost Y={rPageHost.Y:0.0} Alto={rPageHost.Height:0.0} | " +
                    $"ObjetosNavToggle Y={rToggle.Y:0.0} Alto={rToggle.Height:0.0} MargenSuperiorReal(toggle.Y-celda.Y)={rToggle.Y - rCelda.Y:0.0} | " +
                    $"RadioButton'1' Y={rT1.Y:0.0} Alto={rT1.Height:0.0} CentroY={centroToggle:0.0} | " +
                    $"TextBlock 'Equipamiento' Y={rTitulo.Y:0.0} Alto={rTitulo.Height:0.0} CentroY={centroTitulo:0.0} | " +
                    $"Border 'Equipamiento' (ObjetosSeccionEquipamiento) Y={rSeccion.Y:0.0} Alto={rSeccion.Height:0.0} | " +
                    $"DELTA CENTROS (RadioButton '1' vs titulo) = {deltaCentros:0.0}px (positivo = el boton '1' esta MAS ABAJO que el centro real del titulo de la seccion)");

                if (rCelda != Rect.Empty && Math.Abs((rToggle.Y - rCelda.Y) - 4.0) > 1.5)
                    Console.WriteLine($"DIAG-NAVTOGGLE-POS[{etiqueta}]: AVISO - el margen superior real medido ({rToggle.Y - rCelda.Y:0.0}px) no coincide con el Margin=\"0,4,4,0\" declarado en XAML (esperado ~4px) - VerticalAlignment=\"Top\" podria no estar anclando contra la celda que se esperaba.");

                // CANARIO REAL: el selector 1/2/3 y el titulo de la seccion ("Equipamiento"/etc.) viven
                // en la MISMA fila visual conceptual (misma cabecera de pagina) - un usuario los lee como
                // una unica fila. Tolerancia 3px (redondeo de fuente/DPI real, no cero exacto) - por
                // encima de eso es un desalineamiento vertical real y perceptible, exactamente el reporte
                // del usuario ("los botones 1,2,3 estan mas abajo de lo que deberian").
                if (Math.Abs(deltaCentros) > 3.0)
                    Console.WriteLine($"FALLO: DIAG-NAVTOGGLE-POS-CENTRO[{etiqueta}] - el centro vertical del RadioButton '1' ({centroToggle:0.0}) esta {deltaCentros:0.0}px mas abajo que el centro vertical real del titulo 'Equipamiento' ({centroTitulo:0.0}) - deberian compartir fila visual, tolerancia 3px.");

                string shotN = Path.Combine(AppContext.BaseDirectory, $"diag-navtoggle-pos-{etiqueta}.png");
                File.WriteAllBytes(shotN, CapturarPngPublicoReutilizable(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"DIAG-NAVTOGGLE-POS[{etiqueta}]: captura real -> {shotN}");
            }

            Medir("1080x700-min", 1080, 700);
            Medir("1080x900-alto", 1080, 900);
            Medir("1600x900-ancho", 1600, 900);
            // Ventana estrecha real (por debajo del ancho de referencia AR-14c de 1080x700, pero
            // dentro del rango que el arnes ya prueba en otras rondas) - para comprobar si el
            // amontonamiento horizontal entre "Equipamiento"/"Inventario"/"Almacenes" (titulo) y el
            // StackPanel flotante (HorizontalAlignment="Right") empeora en algun ancho real.
            Medir("900x700-estrecho", 900, 700);
            // Tamaños REALES guardados en el window.json del propio usuario
            // (%LocalAppData%\Terrakeep\window.json: Width/Height=1180x860 normal,
            // PinnedWidth/PinnedHeight=1476x1081) - probar exactamente los tamaños con los que el
            // usuario abre la app de verdad, no solo los de referencia del arnes.
            Medir("1180x860-guardado-usuario", 1180, 860);
            Medir("1476x1081-pinned-usuario", 1476, 1081);

            // Las otras 2 paginas (Inventario/Almacenes) tienen Margin="0,14,0,0" en su Border de
            // contenido (ObjetosSeccionInventario/Almacenes) - a diferencia de ObjetosSeccionEquipamiento,
            // que NO tiene margen. El StackPanel flotante es un HERMANO fijo de las 3 paginas (no se
            // mueve al cambiar de pagina) - comprobar si esa asimetria de margen hace que el titulo de
            // Inventario/Almacenes quede VISUALMENTE por DEBAJO del selector flotante (justo lo
            // contrario de "mas abajo", pero un desajuste real de todas formas si aparece).
            var seccionInv = objetosViewHost?.FindName("ObjetosSeccionInventario") as FrameworkElement;
            var seccionAlm = objetosViewHost?.FindName("ObjetosSeccionAlmacenes") as FrameworkElement;
            void MedirPagina(int indice, string nombre, FrameworkElement seccion)
            {
                vm.RequestObjetosSection(indice);
                DoEvents(); DoEvents(); DoEvents();
                Rect rToggle = RectCompletoPublico(toggle, window);
                Rect rSeccion = RectCompletoPublico(seccion, window);
                Console.WriteLine($"DIAG-NAVTOGGLE-POS[pagina {nombre}]: ObjetosNavToggle Y={rToggle.Y:0.0} | Border '{nombre}' (con su Margin propio) Y={rSeccion.Y:0.0} | Delta toggle.Y - seccion.Y = {rToggle.Y - rSeccion.Y:0.0} (negativo = toggle por ENCIMA del titulo de esta pagina)");
                string shotP = Path.Combine(AppContext.BaseDirectory, $"diag-navtoggle-pos-pagina-{nombre}.png");
                File.WriteAllBytes(shotP, CapturarPngPublicoReutilizable(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"DIAG-NAVTOGGLE-POS[pagina {nombre}]: captura real -> {shotP}");
            }
            FijarTamaño(window, 1080, 700);
            DoEvents(); DoEvents(); DoEvents();
            if (seccionInv != null) MedirPagina(1, "Inventario", seccionInv);
            if (seccionAlm != null) MedirPagina(2, "Almacenes", seccionAlm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FALLO: DIAG-NAVTOGGLE-POS - excepcion real: {ex}");
        }
    }

    // Copia minima de RectCompleto (AuditoriaMaquetacion.cs es 'private static' en la misma clase
    // partial - se podria llamar directo, pero se duplica aqui con nombre distinto a proposito para
    // no arriesgar colision de firma si AuditoriaMaquetacion cambia de forma independiente; este
    // fichero es exclusivamente diagnostico y desechable).
    private static Rect RectCompletoPublico(FrameworkElement fe, FrameworkElement raiz) =>
        fe.TransformToAncestor(raiz).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));

    // Primer TextBlock real en profundidad (recorrido del arbol visual, no logico - los DataTemplate
    // ya realizados solo se ven via VisualTreeHelper) dentro de un contenedor dado. Usado para
    // encontrar el TextBlock real del titulo de seccion ("Equipamiento"/"Inventario"/"Almacenes",
    // Style="SectionText") sin depender de un x:Name que no existe en produccion.
    private static TextBlock? PrimerDescendienteTextBlock(DependencyObject raiz)
    {
        int n = VisualTreeHelper.GetChildrenCount(raiz);
        for (int i = 0; i < n; i++)
        {
            var hijo = VisualTreeHelper.GetChild(raiz, i);
            if (hijo is TextBlock tb) return tb;
            var enHijo = PrimerDescendienteTextBlock(hijo);
            if (enHijo != null) return enHijo;
        }
        return null;
    }

    private static byte[] CapturarPngPublicoReutilizable(Visual visual, double anchoPx, double altoPx, double dpi = 96)
    {
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(anchoPx * dpi / 96.0), (int)Math.Ceiling(altoPx * dpi / 96.0), dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }
}
