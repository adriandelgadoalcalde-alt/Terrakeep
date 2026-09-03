using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Una carpeta del arbol de categorias de la Libreria - vanilla, arbol REAL calcado de
// Terrasavr (Hc.deploy real, ver VanillaLibraryTreeCatalog/scripts/
// extraer-arbol-libreria-vanilla.js) mas una unica carpeta madre "Calamity (mod)" (agrupado
// real de calamity/catalog.json, mismo algoritmo que calamityBuildLibraryNode de
// overrides.js - ver LibraryViewModel.BuildCalamityRoot). El icono representativo viene del
// propio objeto real que el arbol de Terrasavr asigna a esa carpeta (o, para Calamity, el
// primer objeto real de la categoria) - nunca un icono generico inventado.
public sealed partial class CategoryNodeViewModel(string name, string fullPath) : ObservableObject
{
    public string Name { get; } = name;
    public string FullPath { get; } = fullPath;
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    // Ids reales que caen bajo este nodo - el propio conjunto si es una carpeta hoja, o la
    // union de todos sus descendientes si es una carpeta intermedia. Pertenencia MULTIPLE de
    // verdad (un mismo objeto puede estar en el conjunto de mas de un nodo a la vez, igual que
    // el arbol real de Terrasavr - ej. una espada de hierro cae en "Materials/Iron & Lead" Y
    // en "Categories/Weapons/Melee damage"). Solo para pertenencia rapida (Contains) - para
    // MOSTRAR los objetos usar ItemIdsOrdered, que respeta el orden curado real.
    public HashSet<int> ItemIdSet { get; set; } = [];

    // Bug real encontrado y corregido 2-sep-2026 (pedido explicito del usuario: "reordenar
    // todos los ítems... para que coincidan 100 por 100 de como lo tenemos en terrasav"):
    // HashSet<int> no garantiza NINGUN orden de enumeracion - filtrar por ItemIdSet.Contains
    // producia el orden arbitrario de "todos los objetos del catalogo", no el orden curado
    // real de Terrasavr (ej. "Copper & Tin" es [12, 3507, 3509, 89, 699, ...], no ids
    // ascendentes). Esta lista SI preserva ese orden real - para una hoja, tal cual viene del
    // propio JSON extraido (que a su vez preserva el orden real de Hc.deploy); para una
    // carpeta intermedia, sus hijos concatenados en orden real, sin duplicados (por si un
    // mismo objeto cae en mas de un hijo a la vez).
    public List<int> ItemIdsOrdered { get; set; } = [];

    [ObservableProperty] private int _itemCount;
    [ObservableProperty] private string? _iconPath;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isExpanded;

    // Auditoria de Opus, Bloque 6 (T-18): "CategoryNodeTemplate"/"ResearchCategoryNodeTemplate"/
    // "BuffCategoryNodeTemplate" en MainWindow.xaml eran 3 copias identicas del mismo arbol,
    // solo distintas en A QUE SelectCategoryCommand apuntaba cada boton (Library/Research/
    // BuffLibrary - 3 ViewModels distintos, cada uno con su propia instancia de arbol, ver
    // LibraryCategoryTreeBuilder.Build llamado por separado desde cada constructor). En vez de
    // enrutar el Command con RelativeSource+ruta al ViewModel dueño (lo que exigia una plantilla
    // por dueño), el propio nodo lleva SU comando real - cada ViewModel lo asigna una vez, justo
    // tras construir su arbol (ver AssignSelectCommand), y la UNICA plantilla real
    // (CategoryNodeTemplate) hace simplemente Command="{Binding SelectCommand}".
    public ICommand? SelectCommand { get; set; }

    // Asigna el mismo comando real a un nodo Y a todos sus descendientes (recursivo, un arbol
    // real puede tener hasta 4 niveles de profundidad - ver VanillaLibraryTreeCatalog). Llamado
    // una vez por cada ViewModel dueño de un arbol, justo despues de construirlo.
    public static void AssignSelectCommand(IEnumerable<CategoryNodeViewModel> nodes, ICommand command)
    {
        foreach (var node in nodes)
        {
            node.SelectCommand = command;
            AssignSelectCommand(node.Children, command);
        }
    }
}
