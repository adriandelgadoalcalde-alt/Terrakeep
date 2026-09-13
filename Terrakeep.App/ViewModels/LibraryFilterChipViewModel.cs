using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels;

// Una "pastilla" de filtro combinable de la Libreria (Rareza/Tipo de daño/Ranura de equipo -
// encargo del usuario, 13-sep-2026). Generica sobre TValue (int para rareza, ItemDamageKind
// para tipo de daño, SlotKind para ranura) para no triplicar la misma clase - LibraryViewModel
// guarda tres listas de esta misma clase con TValue distinto.
//
// SwatchBrush solo lo rellena el grupo de Rareza (el color REAL de rareza, VanillaRarityColorCatalog) -
// null en los otros dos grupos, donde el XAML oculta el punto de color (mismo patron ya
// establecido en LibraryItemViewModel.RarityBrush: null = "no fingir un color que no existe").
//
// LabelKey en vez de un string ya resuelto: el rotulo tiene que refrescarse solo al cambiar de
// idioma, igual que el resto de la Libreria - LibraryViewModel se suscribe UNA vez a
// LocalizationService y reparte via RefrescarIdioma(), mismo mecanismo real que
// LibraryItemViewModel.
public sealed partial class LibraryFilterChipViewModel<TValue>(TValue value, string labelKey, Brush? swatchBrush = null) : ObservableObject
    where TValue : notnull
{
    public TValue Value { get; } = value;
    public Brush? SwatchBrush { get; } = swatchBrush;

    [ObservableProperty] private bool _isSelected;

    public string Label => LocalizationService.Instance[labelKey];

    public void RefrescarIdioma() => OnPropertyChanged(nameof(Label));
}
