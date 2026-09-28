using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Terrakeep.App.Views;

// FASE D del responsive global (28-sep-2026): tarjetas de carpeta raiz compartidas por las 3 superficies
// de la familia "catalogo con categorias" - ver el comentario de TarjetasCategoriasRaiz.xaml.
public partial class TarjetasCategoriasRaiz : UserControl
{
    // true en la Libreria de buffs: el recuento se lee "N buff(s)" (CategoryNodeViewModel.BuffCountLabel).
    public static readonly DependencyProperty EsBuffsProperty = DependencyProperty.Register(
        nameof(EsBuffs), typeof(bool), typeof(TarjetasCategoriasRaiz), new PropertyMetadata(false));

    public bool EsBuffs { get => (bool)GetValue(EsBuffsProperty); set => SetValue(EsBuffsProperty, value); }

    // Correccion D-02 del revisor visual: alto del viewport del scroll owner de resultados que aloja las
    // tarjetas (lo enlaza la superficie: ViewportHeight del ScrollViewer ancestro).
    public static readonly DependencyProperty AltoDisponibleProperty = DependencyProperty.Register(
        nameof(AltoDisponible), typeof(double), typeof(TarjetasCategoriasRaiz), new PropertyMetadata(double.PositiveInfinity));

    public double AltoDisponible { get => (double)GetValue(AltoDisponibleProperty); set => SetValue(AltoDisponibleProperty, value); }

    // true = tarjeta + subpastillas de primer nivel; false = tarjeta compacta. Lo decide EvaluarModo.
    public static readonly DependencyProperty SubcarpetasVisiblesProperty = DependencyProperty.Register(
        nameof(SubcarpetasVisibles), typeof(bool), typeof(TarjetasCategoriasRaiz), new PropertyMetadata(true));

    public bool SubcarpetasVisibles { get => (bool)GetValue(SubcarpetasVisiblesProperty); set => SetValue(SubcarpetasVisiblesProperty, value); }

    // Alto REAL medido de la primera fila de tarjetas COMPLETAS (con subpastillas), por numero de tarjetas
    // por fila. No depende del viewport, solo del contenido y del idioma - se vacia al cambiar cualquiera.
    private readonly Dictionary<int, double> _altoFilaCompleta = [];

    public TarjetasCategoriasRaiz()
    {
        InitializeComponent();
        LayoutUpdated += (_, _) => EvaluarModo();
        DataContextChanged += (_, _) => { _altoFilaCompleta.Clear(); SubcarpetasVisibles = true; };
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, (_, _) => { _altoFilaCompleta.Clear(); SubcarpetasVisibles = true; }, "Item[]");
    }

    // Regla: subpastillas SOLO si la primera fila de tarjetas completas cabe entera en el viewport.
    // Estable (sin oscilar) aunque cambiar de modo haga aparecer/desaparecer la barra de scroll y cambie
    // cuantas tarjetas caben por fila: la primera fila con N+1 tarjetas CONTIENE a la de N (mismo orden),
    // asi que su alto nunca es menor - si la de N no cabia, la de N+1 tampoco, y si la de N+1 cabia, la de N
    // tambien. Cuando falta el dato para el numero de tarjetas actual, se mide una vez en modo completo.
    private void EvaluarModo()
    {
        double disponible = AltoDisponible;
        if (!IsVisible || double.IsNaN(disponible) || disponible <= 0) return;
        var tarjetas = new List<(double y, double alto)>();
        for (int i = 0; i < TarjetasRaiz.Items.Count; i++)
        {
            if (TarjetasRaiz.ItemContainerGenerator.ContainerFromIndex(i) is not ContentPresenter cp || cp.ActualHeight <= 0) continue;
            if (VisualTreeHelper.GetChildrenCount(cp) == 0 || VisualTreeHelper.GetChild(cp, 0) is not FrameworkElement tarjeta) continue;
            double y = tarjeta.TranslatePoint(new Point(0, 0), this).Y;
            tarjetas.Add((y, tarjeta.ActualHeight));
        }
        if (tarjetas.Count == 0) return;
        double arriba = tarjetas.Min(t => t.y);
        var fila = tarjetas.Where(t => t.y - arriba < 1).ToList();
        int porFila = fila.Count;
        double necesario = Margin.Top + arriba + fila.Max(t => t.alto);

        if (SubcarpetasVisibles)
        {
            _altoFilaCompleta[porFila] = necesario;
            if (necesario > disponible + 0.5) SubcarpetasVisibles = false;
            return;
        }
        bool noCabe = _altoFilaCompleta.Any(kv => kv.Key <= porFila && kv.Value > disponible + 0.5);
        bool cabe = _altoFilaCompleta.Any(kv => kv.Key >= porFila && kv.Value <= disponible + 0.5);
        bool sinDato = !_altoFilaCompleta.ContainsKey(porFila) && !noCabe && !cabe;
        if (!noCabe && (cabe || sinDato)) SubcarpetasVisibles = true;
    }
}
