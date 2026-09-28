using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// FASE D del responsive global (28-sep-2026): navegacion de categorias compartida por la Libreria de
// objetos, la Libreria de buffs e Investigacion - ver el comentario de NavegadorCategorias.xaml. Toda la
// logica de la ruta vive en CatalogBrowserViewModel (RutaCategoria/MigasCategoria/NodoSubcategorias).
//
// Correccion D-01/D-04 del revisor visual: aqui se mantienen los hijos "dinamicos" de los dos paneles
// de flujo (las pastillas de las raices detras de "Ver todo" y las migas dentro de la linea de ruta) -
// un WrapPanel/LineaRutaPanel necesita a esos elementos como hijos DIRECTOS para que la cabecera y las
// raices compartan lineas y para plegar migas; un ItemsControl anidado seria un bloque indivisible.
public partial class NavegadorCategorias : UserControl
{
    public static readonly DependencyProperty CabeceraProperty = DependencyProperty.Register(
        nameof(Cabecera), typeof(object), typeof(NavegadorCategorias), new PropertyMetadata(null));

    public static readonly DependencyProperty AccionesRutaProperty = DependencyProperty.Register(
        nameof(AccionesRuta), typeof(object), typeof(NavegadorCategorias), new PropertyMetadata(null));

    public static readonly DependencyProperty MostrarNavegacionProperty = DependencyProperty.Register(
        nameof(MostrarNavegacion), typeof(bool), typeof(NavegadorCategorias),
        new PropertyMetadata(true, (d, _) => ((NavegadorCategorias)d).AplicarMostrarNavegacion()));

    /// <summary>Cabecera propia de la superficie (plegar/buscador/filtros), primera en el flujo de las
    /// categorias principales. Sus bindings resuelven contra el DataContext que ella misma fije.</summary>
    public object? Cabecera { get => GetValue(CabeceraProperty); set => SetValue(CabeceraProperty, value); }

    /// <summary>Acciones de carpeta de la superficie (Investigacion: Investigar carpeta / Quitar), en la
    /// linea de ruta, entre "Subcategorias" y el resumen.</summary>
    public object? AccionesRuta { get => GetValue(AccionesRutaProperty); set => SetValue(AccionesRutaProperty, value); }

    /// <summary>False con la Libreria plegada: solo queda la cabecera (el boton de desplegar).</summary>
    public bool MostrarNavegacion { get => (bool)GetValue(MostrarNavegacionProperty); set => SetValue(MostrarNavegacionProperty, value); }

    private INotifyCollectionChanged? _raicesObservadas;
    private INotifyCollectionChanged? _migasObservadas;

    public NavegadorCategorias()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Reenganchar();
    }

    private void Reenganchar()
    {
        if (_raicesObservadas != null) _raicesObservadas.CollectionChanged -= OnRaicesCambiadas;
        if (_migasObservadas != null) _migasObservadas.CollectionChanged -= OnMigasCambiadas;
        _raicesObservadas = null; _migasObservadas = null;
        if (DataContext is ICatalogoNavegable vm)
        {
            _raicesObservadas = vm.RootCategories;
            _migasObservadas = vm.MigasCategoria;
            _raicesObservadas.CollectionChanged += OnRaicesCambiadas;
            _migasObservadas.CollectionChanged += OnMigasCambiadas;
        }
        RegenerarRaices();
        RegenerarMigas();
    }

    /// <summary>Mueve la cabecera (y, si se da, las acciones de ruta) que el host declara dentro de un
    /// Decorator plegado a los huecos de este control. Motivo: WPF no admite x:Name en el contenido que se
    /// pasa a un UserControl que ya tiene nombres propios (error MC3093), y los nombres de la cabecera
    /// (LibrarySearchBox, LibraryFiltersButton, BuffLibrarySearchBox) los usan el code-behind del host, el
    /// Popup de filtros (PlacementTarget por ElementName) y el arnes. Declarados en el XAML del host, siguen
    /// registrados en SU ambito de nombres.
    /// fuenteDataContext: el elemento del host cuyo DataContext (MainViewModel) deben usar la cabecera y las
    /// acciones - dentro de este control heredarian el del catalogo. Enlace explicito con Source (medido: un
    /// DataContext por RelativeSource AncestorType=Window declarado en el XAML del host se quedaba sin
    /// resolver tras mover el elemento, y los botones salian sin texto).</summary>
    public void AdoptarCabecera(FrameworkElement fuenteDataContext, Decorator soporteCabecera, Decorator? soporteAcciones = null)
    {
        Cabecera = Adoptar(soporteCabecera, fuenteDataContext);
        if (soporteAcciones != null) AccionesRuta = Adoptar(soporteAcciones, fuenteDataContext);
    }

    private static UIElement? Adoptar(Decorator soporte, FrameworkElement fuente)
    {
        var hijo = soporte.Child;
        soporte.Child = null;
        if (hijo is FrameworkElement fe)
            fe.SetBinding(DataContextProperty, new System.Windows.Data.Binding(nameof(DataContext)) { Source = fuente });
        return hijo;
    }

    private void OnRaicesCambiadas(object? sender, NotifyCollectionChangedEventArgs e) => RegenerarRaices();
    private void OnMigasCambiadas(object? sender, NotifyCollectionChangedEventArgs e) => RegenerarMigas();

    // Hijos 0 (cabecera) y 1 ("Ver todo") son fijos del XAML; detras, una pastilla por raiz.
    private void RegenerarRaices()
    {
        var panel = CategoriasPrincipales;
        while (panel.Children.Count > 2) panel.Children.RemoveAt(panel.Children.Count - 1);
        if (DataContext is not ICatalogoNavegable vm) return;
        var plantilla = (DataTemplate)Resources["PastillaCategoriaTemplate"];
        foreach (var raiz in vm.RootCategories)
            panel.Children.Add(new ContentPresenter { Content = raiz, ContentTemplate = plantilla, VerticalAlignment = VerticalAlignment.Center });
        AplicarMostrarNavegacion();
    }

    // Migas entre ElipsisRuta (hijo 0) y BotonSubcategorias.
    private void RegenerarMigas()
    {
        var panel = RutaSubcategorias;
        for (int i = panel.Children.Count - 1; i >= 0; i--)
            if (LineaRutaPanel.GetRol(panel.Children[i]) == RolLineaRuta.Miga) panel.Children.RemoveAt(i);
        if (DataContext is not ICatalogoNavegable vm) return;
        var plantilla = (DataTemplate)Resources["MigaTemplate"];
        int pos = panel.Children.IndexOf(ElipsisRuta) + 1;
        foreach (var miga in vm.MigasCategoria)
        {
            var cp = new ContentPresenter { Content = miga, ContentTemplate = plantilla, VerticalAlignment = VerticalAlignment.Center };
            LineaRutaPanel.SetRol(cp, RolLineaRuta.Miga);
            panel.Children.Insert(pos++, cp);
        }
    }

    private void AplicarMostrarNavegacion()
    {
        var vis = MostrarNavegacion ? Visibility.Visible : Visibility.Collapsed;
        var panel = CategoriasPrincipales;
        for (int i = 1; i < panel.Children.Count; i++) panel.Children[i].Visibility = vis;
        RutaSubcategorias.Visibility = vis;
        if (!MostrarNavegacion) PopupSubcategorias.IsOpen = false;
    }

    // Tooltip de "...": la ruta COMPLETA (nombres completos, en el idioma actual) de la carpeta elegida.
    private void OnElipsisToolTip(object sender, ToolTipEventArgs e)
    {
        if (DataContext is ICatalogoNavegable vm)
            ElipsisRuta.ToolTip = string.Join("  ›  ", vm.RutaCategoria.Select(n => n.Name));
    }

    // D-03: alto maximo del desplegable ligado a la ventana y al area de trabajo real (el popup se coloca
    // en pantalla, no dentro de la ventana): nunca mas alto que la ventana menos un margen ni que el area de
    // trabajo. Si 49 pastillas no caben ni asi, desplaza SU unico ScrollViewer.
    private void OnSubcategoriasAbiertas(object? sender, EventArgs e)
    {
        var ventana = Window.GetWindow(this);
        double alto = SystemParameters.WorkArea.Height - 24;
        double ancho = 720;
        if (ventana != null)
        {
            alto = Math.Min(alto, ventana.ActualHeight - 80);
            ancho = Math.Min(ancho, Math.Max(280, ventana.ActualWidth - 80));
        }
        MarcoSubcategorias.MaxHeight = Math.Max(120, alto);
        MarcoSubcategorias.MaxWidth = ancho;
        ScrollSubcategorias.ScrollToTop();
    }

    // El comando de la pastilla (SelectCommand del propio nodo) ya elige la carpeta; el desplegable se
    // cierra para que el usuario vea el resultado. Si la carpeta elegida tiene a su vez hijos, el boton
    // del desplegable pasa a ofrecerlos (NodoSubcategorias) sin mas clics que ese.
    private void OnSubcategoriaElegida(object sender, RoutedEventArgs e) => PopupSubcategorias.IsOpen = false;
}
