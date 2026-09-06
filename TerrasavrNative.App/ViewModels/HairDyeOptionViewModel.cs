using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Una entrada del selector visual de tinte de pelo (Apariencia) - a diferencia del peinado no
// hace falta renderizar nada, el tinte se elige por el objeto real que lo aplica (sprite +
// nombre reales), ver AppearanceViewModel/HairDyeCatalog.
//
// Ronda de idioma del 6-sep-2026: la primera entrada ("Ninguno", indice 0) recibia el texto YA
// resuelto y se quedaba en el idioma de arranque - salia literalmente entre los fallos del
// barrido con la app en ingles. Ahora se guarda la clave y se resuelve al leerla. Las OTRAS 12
// entradas son nombres reales de objeto del juego (catalogo de contenido, solo en español por
// ahora): no son claves del diccionario y se usan tal cual, mismo criterio ya aplicado en
// EquipmentOptionViewModel y BuildClassFilterOptionViewModel.
public sealed partial class HairDyeOptionViewModel : ObservableObject
{
    private readonly string _displayNameOrKey;

    public HairDyeOptionViewModel(int index, string displayNameOrKey, string? iconPath)
    {
        Index = index;
        _displayNameOrKey = displayNameOrKey;
        IconPath = iconPath;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Valor real que se guarda en PlrCharacter.HairDye (0 = Ninguno).
    public int Index { get; }

    public string DisplayName
    {
        get
        {
            string texto = Services.LocalizationService.Instance[_displayNameOrKey];
            return texto.StartsWith('[') && texto.EndsWith(']') ? _displayNameOrKey : texto;
        }
    }

    public string? IconPath { get; }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(DisplayName));
}
