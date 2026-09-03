namespace TerrasavrNative.App.ViewModels;

// Auditoria de Opus, Bloque 4 (T-2): "formaliza 3 breakpoints reales" - antes cada pantalla que
// queria reaccionar al ancho real de la ventana (E-2, A-4) lo habria hecho a su manera, sin
// ningun vocabulario compartido. Un unico punto de verdad, actualizado en vivo
// (MainViewModel.SizeClass, ver MainWindow.xaml.cs OnSizeChanged) - cualquier pantalla nueva
// que necesite adaptarse de verdad al ancho disponible se ata a ESTE enum, no a un numero de
// pixeles propio inventado sobre la marcha.
//
// Umbrales reales (no arbitrarios): medidos con el arnes de UI Automation contra el contenido
// real de cada pantalla que los consume (E-2: 3 vistas de Equipamiento -Armadura/Vanidad/
// Tintes- side by side; A-4: Inventario + Almacen side by side) - ver el comentario de cada
// consumidor real para la medicion concreta que fijo su propio umbral.
public enum WindowSizeClass
{
    // Ventana estrecha (incluye el MinWidth=1080 obligado) - un unico panel a la vez, con
    // selector de pildoras para elegir cual (comportamiento de siempre).
    Compacto,
    // Sitio real para 2 paneles a la vez sin apretar.
    Normal,
    // Sitio real para 3 paneles a la vez (o 2 + el panel Editar realmente comodo) sin apretar.
    Amplio,
}
