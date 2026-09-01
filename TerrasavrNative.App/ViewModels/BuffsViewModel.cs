using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Buffs" - edicion real (cierra el hueco #1 de la auditoria Terrasavr JS vs puerto,
// ver bitacora.md): quitar un buff activo, cambiar su duracion, y buscar+añadir uno nuevo a
// un hueco libre. NO se porto el guardado/carga de presets de buffs a fichero .json/.tsb del
// original (decision de alcance) - se puede añadir despues si hace falta.
public partial class BuffsViewModel : ObservableObject
{
    private const int MaxResults = 200;
    private const int DefaultDurationSeconds = 600; // 10 minutos, mismo criterio "valor razonable" que ResearchAll

    private readonly VanillaBuffCatalog _vanillaCatalog;
    private readonly CalamityBuffCatalog _calamityCatalog;
    private readonly List<BuffCatalogEntryViewModel> _all;
    private PlrCharacter? _character;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private bool _isPicking;

    public ObservableCollection<BuffRowViewModel> Active { get; } = [];
    public ObservableCollection<BuffCatalogEntryViewModel> Results { get; } = [];

    public BuffsViewModel(CharacterFileService service)
    {
        _vanillaCatalog = service.VanillaBuffs;
        _calamityCatalog = service.CalamityBuffCatalog;

        _all = [];
        foreach (var (id, name) in _vanillaCatalog.AllEntries())
            _all.Add(new BuffCatalogEntryViewModel(name, id, false, VanillaBuffIconResolver.GetIconPath(id), _vanillaCatalog.GetDescription(id)));
        foreach (var entry in _calamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
            _all.Add(new BuffCatalogEntryViewModel(entry.DisplayName, entry.SyntheticId, true, iconPath, entry.Description));
        }
    }

    public void LoadFrom(PlrCharacter character)
    {
        _character = character;
        Active.Clear();
        IsPicking = false;
        SearchText = string.Empty;
        foreach (var buff in character.Buffs)
        {
            if (buff.Id == 0) continue;
            Active.Add(BuffRowViewModel.From(buff, _vanillaCatalog, _calamityCatalog, RemoveRow));
        }
    }

    private void RemoveRow(BuffRowViewModel row)
    {
        row.Buff.Id = 0;
        row.Buff.Time = 0;
        Active.Remove(row);
    }

    [RelayCommand]
    private void BeginAdd()
    {
        IsPicking = true;
        SearchText = string.Empty;
        ApplyFilter();
    }

    [RelayCommand]
    private void CancelAdd() => IsPicking = false;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // Bug real reportado 1-sep-2026 ("el sistema de buff sigue sin ser de sprites"): con
    // busqueda vacia esto dejaba Results vacio del todo - el panel "Añadir buff..." se abria
    // sin nada dentro hasta escribir algo, dando la sensacion de que los sprites no
    // funcionaban. LibraryViewModel SI muestra todo de entrada con busqueda vacia (limitado
    // por MaxResults) - mismo criterio aqui, por consistencia.
    private void ApplyFilter()
    {
        Results.Clear();
        var matches = string.IsNullOrWhiteSpace(SearchText)
            ? _all
            : _all.Where(b => b.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var m in matches.Take(MaxResults)) Results.Add(m);
        ResultsSummary = matches.Count > MaxResults
            ? $"Mostrando {MaxResults} de {matches.Count} - {(string.IsNullOrWhiteSpace(SearchText) ? "escribe para afinar la busqueda." : "afina la busqueda.")}"
            : $"{matches.Count} resultado(s).";
    }

    // Un personaje solo tiene sitio para 44/22/10 buffs activos a la vez (segun version, ver
    // PlrBodySerializer) - si no queda ningun slot libre (Id==0), no hace nada en vez de
    // reventar; caso raro, no hace falta un mensaje de error dedicado.
    [RelayCommand]
    private void PickBuff(BuffCatalogEntryViewModel? entry)
    {
        if (_character == null || entry == null) return;
        var slot = _character.Buffs.FirstOrDefault(b => b.Id == 0);
        if (slot == null) return;

        slot.Id = entry.Id;
        slot.Time = DefaultDurationSeconds * 60; // Time va en ticks, 60/seg
        Active.Add(BuffRowViewModel.From(slot, _vanillaCatalog, _calamityCatalog, RemoveRow));
        IsPicking = false;
        SearchText = string.Empty;
    }
}
