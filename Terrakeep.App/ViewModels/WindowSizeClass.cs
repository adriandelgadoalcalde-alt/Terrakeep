namespace Terrakeep.App.ViewModels;

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
    // Auditoria de redimensionado, R-10/H-09: a 1920px+ (pantalla completa mas comun hoy en
    // dia) los topes de Amplio dejaban entre el 28% y el 39% del ancho real sin usar - medido
    // contra el contenido real de cada consumidor (WrapPanel de tarjetas de Inicio, swatches de
    // Apariencia, tarjetas de Desbloqueos/changelog), no elegido a ojo. DEBE tratarse como "al
    // menos tan expandido como Amplio" en cualquier consumidor booleano existente (comparar con
    // >=, nunca con ==) - Extra es un superconjunto de espacio de Amplio, nunca deberia
    // desactivar algo que Amplio ya activaba.
    Extra,
}

// H5-08 (quinta auditoria de Opus): "UpdateSizeClass(double actualWidth) solo recibe el ancho -
// la altura, que es la dimension que de verdad aprieta a las rejillas de slots, no participa en
// ninguna decision de layout". Segunda dimension real, independiente de WindowSizeClass (una
// ventana puede ser Amplia y baja, o Compacta y alta - los dos ejes no estan correlados).
// Umbral real AltoMinHeight=900 (MainViewModel.cs) citado del propio informe de la auditoria:
// "un portátil de 1440×900 ... probablemente el tamaño más común de uso real" - por encima del
// MinHeight=700 obligado de la ventana, con margen suficiente para notarse.
public enum WindowHeightClass
{
    // Altura minima o cercana (incluye el MinHeight=700 obligado).
    Bajo,
    // Sitio real de sobra en vertical - la Libreria puede desplegarse sola sin apretar los
    // contenedores de encima, igual que Amplio ya hace por ancho.
    Alto,
}
