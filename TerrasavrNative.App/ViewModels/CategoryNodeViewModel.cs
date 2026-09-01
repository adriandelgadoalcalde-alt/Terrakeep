using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Una carpeta del arbol de categorias de la Libreria (vanilla + Calamity, misma jerarquia
// "Padre/Hijo" que ya usa el catalog.json real de Calamity - ver LibraryViewModel). El icono
// representativo es el del primer objeto real que cae en esa carpeta (o alguna de sus
// subcarpetas), no un icono generico inventado - asi la carpeta "Armas/Magia" enseña de
// entrada un arma de magia real, por ejemplo.
public sealed partial class CategoryNodeViewModel(string name, string fullPath) : ObservableObject
{
    public string Name { get; } = name;
    public string FullPath { get; } = fullPath;
    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    [ObservableProperty] private int _itemCount;
    [ObservableProperty] private string? _iconPath;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isExpanded;
}
