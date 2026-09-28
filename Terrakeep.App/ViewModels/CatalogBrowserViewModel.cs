using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Terrakeep.App.ViewModels;

// H5-15 (quinta auditoria de Opus): "H4-10 nunca se implemento - tres ViewModels casi
// identicos (LibraryViewModel/BuffLibraryViewModel/ResearchViewModel): el mismo SelectCategory,
// el mismo ClearCategory, el mismo DispatcherTimer de 180ms... y ya divergen hoy (el mismo bug
// de IsExpanded se arreglo tres veces, el debounce medido en L-c solo llego a una de las tres
// superficies)". Base real compartida: arbol de categorias, busqueda con debounce, y el par de
// comandos de seleccion/limpieza de carpeta - la unica logica de verdad IDENTICA en las 3 (el
// bug de IsExpanded solo puede corregirse UNA vez a partir de ahora). ApplyFilter se queda
// abstracto a proposito, y el tope de resultados NO se sube a esta base (cada hija mantiene su
// propia constante MaxResults): cada hija filtra de una fuente distinta y con reglas propias
// (restriccion de slot en objetos, conteo investigado en Investigacion, universo mas pequeño de
// buffs con su propio tope de 300 ya medido) - unificar eso tambien habria forzado un
// comportamiento identico donde el proyecto ya decidio que NO debe serlo.
// FASE D, correccion D-01 (28-sep-2026): vista no generica de lo que NavegadorCategorias necesita del
// catalogo (genera en code-behind las pastillas de las raices y las migas de la ruta).
public interface ICatalogoNavegable
{
    ObservableCollection<CategoryNodeViewModel> RootCategories { get; }
    ObservableCollection<CategoryNodeViewModel> RutaCategoria { get; }
    ObservableCollection<CategoryNodeViewModel> MigasCategoria { get; }
}

public abstract partial class CatalogBrowserViewModel<TEntry> : ObservableObject, ICatalogoNavegable
{
    protected readonly DispatcherTimer SearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    public ObservableCollection<TEntry> Results { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];

    // H5-13 (quinta auditoria de Opus): "el estado vacio de la Libreria y de la Libreria de
    // buffs es un rectangulo en blanco - sin busqueda/carpeta/restriccion de slot, ApplyFilter
    // sale antes de rellenar nada". H4-07 punto 3 ya resolvio esto SOLO en Investigacion
    // (ResearchViewModel tenia su propio campo suelto, alternado a mano dentro de ApplyFilter) -
    // subido aqui, a la base compartida por H5-15, para las 3 superficies a la vez en vez de
    // repetirlo 2 veces mas. Computada de verdad (nunca se desincroniza sola) en vez de un campo
    // que cada ApplyFilter tiene que acordarse de alternar el mismo en sus dos salidas.
    // Virtual: LibraryViewModel (unica de las 3 con "restriccion de slot" real, ver
    // hasSlotRestriction en su ApplyFilter) necesita una condicion extra - con un slot
    // restringido como destino, el catalogo reducido YA es util de ver de inmediato, mostrar
    // las carpetas raiz genericas en su lugar seria un paso atras, no una mejora.
    public virtual bool ShowRootCategoryCards => SelectedCategory == null && string.IsNullOrWhiteSpace(SearchText);

    protected CatalogBrowserViewModel()
    {
        SearchDebounceTimer.Tick += (_, _) =>
        {
            SearchDebounceTimer.Stop();
            ApplyFilter();
        };
        // Ronda de Libreria/Builds del 6-sep-2026 - BUG REAL medido: ResultsSummary (y
        // SlotRestrictionLabel en la Libreria de objetos) son strings YA RESUELTOS dentro de
        // ApplyFilter, no bindings indexados contra el diccionario - o sea que el aviso
        // "Item[]" de LocalizationService, que refresca solo lo que se lee via {Binding
        // Loc[clave]}, no les llegaba nunca. Con la app en español, cambiar a ingles dejaba la
        // linea de resumen de las TRES superficies (Libreria, Libreria de buffs, Investigacion)
        // congelada en español hasta que el usuario volviera a teclear o a pulsar una carpeta -
        // y en el estado de arranque (sin busqueda ni carpeta) eso es literalmente el UNICO
        // texto del panel derecho, asi que se quedaba una frase entera en español a la vista.
        // El barrido A10-IDIOMA-BARRIDO no podia cazarlo: ese barrido navega pulsando carpetas,
        // y cada pulsacion vuelve a llamar a ApplyFilter, que regenera el texto EN EL IDIOMA
        // ACTIVO - la version rancia solo existe si NO se refiltra tras cambiar de idioma.
        // Evento DEBIL (mismo motivo real que LocalizedContentViewModel: el servicio de idioma
        // es un singleton que vive lo que la aplicacion, estos ViewModels no).
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Refiltrar entero (en vez de solo recalcular el resumen) es lo correcto de verdad: el texto
    // depende tambien del nombre de la carpeta elegida, que YA cambia de idioma solo
    // (CategoryNodeViewModel.Name), y ApplyFilter es idempotente en las 3 hijas.
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => ApplyFilter();

    // Solo la busqueda por TEXTO se difiere (elegir/quitar carpeta sigue aplicando al instante,
    // un clic discreto) - medido de verdad en L-c (LibraryViewModel): 180ms es imperceptible
    // como demora pero evita repetir el reflow entero en cada tecla de una racha de tecleo.
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(ShowRootCategoryCards)); // instantaneo, no espera al debounce de abajo
        SearchDebounceTimer.Stop();
        SearchDebounceTimer.Start();
    }

    // H5-13: cualquier cambio real de SelectedCategory (incluida una asignacion directa, ej.
    // ResearchViewModel.Reset limpiando la seleccion al descargar el personaje - no solo los 2
    // comandos de abajo) mantiene ShowRootCategoryCards sincronizada de verdad.
    partial void OnSelectedCategoryChanged(CategoryNodeViewModel? value)
    {
        OnPropertyChanged(nameof(ShowRootCategoryCards));
        RecalcularRuta();
    }

    // FASE D del responsive global (28-sep-2026, PDF "Arreglo familia keep" bloque 2, s9-s12/s14/s26 C).
    // El arbol lateral de categorias (columna fija de 210px con su propio ScrollViewer, en las 3
    // superficies) escondia las categorias PRINCIPALES detras de un scroll: medido a 1080x700, 4/10 en la
    // Libreria y 5/8 en la de buffs visibles sin desplazar. Lo sustituye NavegadorCategorias
    // (Views/NavegadorCategorias.xaml, UN control para las 3): fila de categorias principales SIEMPRE
    // visible (wrap) + UNA linea de ruta con desplegable para los niveles de debajo (s12: drill-down/
    // flyout). Esa linea necesita saber la ruta real de la carpeta elegida y que nodo aporta las
    // subcategorias - se calcula aqui, una sola vez para las 3 superficies (misma razon que H5-15).
    //
    // RutaCategoria: raiz..seleccionada (vacia sin seleccion). MigasCategoria: la misma ruta sin la raiz
    // (la raiz ya se ve resaltada en la fila principal). NodoSubcategorias: el nodo mas profundo de la
    // ruta que tiene subcarpetas - la seleccionada si las tiene, si no su padre (asi, con una hoja
    // elegida, el desplegable enseña sus hermanas con ella marcada).
    public ObservableCollection<CategoryNodeViewModel> RutaCategoria { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> MigasCategoria { get; } = [];
    [ObservableProperty] private CategoryNodeViewModel? _nodoSubcategorias;
    [ObservableProperty] private bool _haySubnavegacion;

    private void RecalcularRuta()
    {
        foreach (var n in RutaCategoria) n.IsInSelectedPath = false;
        RutaCategoria.Clear();
        MigasCategoria.Clear();
        var ruta = SelectedCategory == null ? null : BuscarRuta(RootCategories, SelectedCategory);
        if (ruta != null)
        {
            for (int i = 0; i < ruta.Count; i++)
            {
                RutaCategoria.Add(ruta[i]);
                if (i > 0) MigasCategoria.Add(ruta[i]);
                if (i < ruta.Count - 1) ruta[i].IsInSelectedPath = true;
            }
        }
        NodoSubcategorias = ruta?.LastOrDefault(n => n.Children.Count > 0);
        HaySubnavegacion = MigasCategoria.Count > 0 || NodoSubcategorias != null;
    }

    private static List<CategoryNodeViewModel>? BuscarRuta(IEnumerable<CategoryNodeViewModel> nodos, CategoryNodeViewModel objetivo)
    {
        foreach (var n in nodos)
        {
            if (ReferenceEquals(n, objetivo)) return [n];
            var sub = BuscarRuta(n.Children, objetivo);
            if (sub != null) { sub.Insert(0, n); return sub; }
        }
        return null;
    }

    // Bug real corregido 2-sep-2026 (LibraryViewModel), replicado a mano otras 2 veces antes de
    // esta base: IsExpanded no se tocaba nunca aqui, asi que ninguna carpeta por debajo de la
    // raiz era alcanzable de verdad desde la UI. Pulsar una carpeta la selecciona/deselecciona
    // (para "ver todo lo de aqui") Y alterna su despliegue, independientemente de si tiene hijos.
    [RelayCommand]
    protected void SelectCategory(CategoryNodeViewModel node)
    {
        node.IsExpanded = !node.IsExpanded;

        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        if (SelectedCategory == node)
        {
            SelectedCategory = null; // pulsar la misma carpeta otra vez la deselecciona
        }
        else
        {
            SelectedCategory = node;
            node.IsSelected = true;
        }
        ApplyFilter();
    }

    [RelayCommand]
    protected void ClearCategory()
    {
        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        SelectedCategory = null;
        ApplyFilter();
    }

    // Cada hija rellena Results/ResultsSummary a su manera real (fuente de entradas, filtro
    // extra, tope de resultados y texto de resumen propios) - ver el comentario de la clase.
    protected abstract void ApplyFilter();
}
