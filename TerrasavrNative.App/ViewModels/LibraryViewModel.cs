using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// Fase 4: Libreria/Buscador. En vez de un arbol de carpetas paginado (el motor Haxe original
// tenia un limite artificial de 19 hijos por carpeta que obligaba a eso) se usa busqueda por
// texto sobre el catalogo COMPLETO (vanilla + Calamity, ~8200 objetos) - mas simple, mas
// rapido de usar, y evita tener que replicar esa limitacion que no existe aqui. Solo se
// renderizan los primeros N resultados a la vez (rendimiento con WrapPanel sin virtualizar) -
// para 8200 objetos sin filtrar no tendria sentido mostrarlos todos de golpe de todas formas.
//
// Tambien hace de selector de objetos: cuando un ItemSlotViewModel pide "elegir objeto"
// (boton en un slot vacio o "cambiar objeto" en uno lleno), MainViewModel pone ese slot en
// PickTarget y cambia la pestaña activa a esta - al pulsar una tarjeta aqui con PickTarget
// puesto, el objeto se coloca en ese slot y se dispara ItemPlaced para volver a Personaje.
public partial class LibraryViewModel : ObservableObject
{
    private const int MaxResults = 300;

    private readonly List<LibraryItemViewModel> _all;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private ItemSlotViewModel? _pickTarget;

    public bool IsPicking => PickTarget != null;

    public event Action? ItemPlaced;

    public ObservableCollection<LibraryItemViewModel> Results { get; } = [];

    public LibraryViewModel(CharacterFileService service)
    {
        _all = [];

        foreach (var (id, name) in service.VanillaCatalog.AllEntries())
            _all.Add(new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, "Vanilla"));

        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            _all.Add(new LibraryItemViewModel(entry.DisplayName, true, iconPath, entry.SyntheticId, entry.Category));
        }

        ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar.";
    }

    partial void OnSearchTextChanged(string value)
    {
        Results.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar.";
            return;
        }

        var matches = _all.Where(i => i.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var item in matches.Take(MaxResults)) Results.Add(item);

        ResultsSummary = matches.Count > MaxResults
            ? $"Mostrando {MaxResults} de {matches.Count} resultados - afina la busqueda."
            : $"{matches.Count} resultado(s).";
    }

    partial void OnPickTargetChanged(ItemSlotViewModel? value) => OnPropertyChanged(nameof(IsPicking));

    [RelayCommand]
    private void PlaceInTarget(LibraryItemViewModel entry)
    {
        if (PickTarget == null) return;
        PickTarget.PlaceItem(entry.Id);
        PickTarget = null;
        ItemPlaced?.Invoke();
    }

    [RelayCommand]
    private void CancelPick() => PickTarget = null;
}
