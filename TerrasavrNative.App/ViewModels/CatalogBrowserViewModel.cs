using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TerrasavrNative.App.ViewModels;

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
public abstract partial class CatalogBrowserViewModel<TEntry> : ObservableObject
{
    protected readonly DispatcherTimer SearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    public ObservableCollection<TEntry> Results { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];

    protected CatalogBrowserViewModel()
    {
        SearchDebounceTimer.Tick += (_, _) =>
        {
            SearchDebounceTimer.Stop();
            ApplyFilter();
        };
    }

    // Solo la busqueda por TEXTO se difiere (elegir/quitar carpeta sigue aplicando al instante,
    // un clic discreto) - medido de verdad en L-c (LibraryViewModel): 180ms es imperceptible
    // como demora pero evita repetir el reflow entero en cada tecla de una racha de tecleo.
    partial void OnSearchTextChanged(string value)
    {
        SearchDebounceTimer.Stop();
        SearchDebounceTimer.Start();
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
