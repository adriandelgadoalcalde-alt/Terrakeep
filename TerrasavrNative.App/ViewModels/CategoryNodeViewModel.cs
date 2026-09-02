using System.Collections.ObjectModel;
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
    // en "Categories/Weapons/Melee damage").
    public HashSet<int> ItemIdSet { get; set; } = [];

    [ObservableProperty] private int _itemCount;
    [ObservableProperty] private string? _iconPath;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isExpanded;
}
