using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Buffs" - rehecha 2-sep-2026 (cuarta pasada, pregunta a Opus sobre el diseño:
// "la misma rejilla cantidad de slots y contorno y todo que inventario"). Antes solo se
// mostraban los buffs ACTIVOS en un WrapPanel dinamico (Active, filtrando Id==0) - ahora
// Container expone los 44/22/10 slots REALES de PlrCharacter.Buffs (segun version, ver
// PlrBodySerializer), algunos vacios, exactamente igual que un ContainerViewModel de objetos.
// El buscador "Añadir buff..." se queda tal cual por ahora (rellena el primer slot vacio) -
// la Libreria de buffs con arbol real de Terrasavr es la Fase 2 de este rework, todavia sin
// implementar.
public partial class BuffsViewModel : ObservableObject
{
    private const int MaxResults = 200;

    private readonly CharacterFileService _service;
    private readonly VanillaBuffCatalog _vanillaCatalog;
    private readonly CalamityBuffCatalog _calamityCatalog;
    private readonly List<BuffCatalogEntryViewModel> _all;
    private readonly Action<BuffSlotViewModel> _selectSlot;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private bool _isPicking;
    [ObservableProperty] private BuffContainerViewModel? _container;

    public ObservableCollection<BuffCatalogEntryViewModel> Results { get; } = [];

    public BuffsViewModel(CharacterFileService service, Action<BuffSlotViewModel> selectSlot)
    {
        _service = service;
        _selectSlot = selectSlot;
        _vanillaCatalog = service.VanillaBuffs;
        _calamityCatalog = service.CalamityBuffCatalog;

        _all = [];
        foreach (var (id, name) in _vanillaCatalog.AllEntries())
            _all.Add(new BuffCatalogEntryViewModel(_vanillaCatalog.GetDisplayName(id), id, false, VanillaBuffIconResolver.GetIconPath(id), _vanillaCatalog.GetDescription(id)));
        foreach (var entry in _calamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
            _all.Add(new BuffCatalogEntryViewModel(entry.DisplayName, entry.SyntheticId, true, iconPath, entry.Description));
        }
    }

    public void LoadFrom(PlrCharacter character)
    {
        IsPicking = false;
        SearchText = string.Empty;

        var slots = new ObservableCollection<BuffSlotViewModel>();
        for (int i = 0; i < character.Buffs.Count; i++)
            slots.Add(new BuffSlotViewModel(i, character.Buffs[i], _vanillaCatalog, _calamityCatalog,
                _service.VanillaBuffDurations, character.Version, _selectSlot));
        // 11 columnas reales (pregunta a Opus sobre el diseño: "44 slots a 11 columnas", real
        // de app.BuffSide/script.beautified.js - 4 filas exactas de 11, no 10x4+4 suelto).
        Container = new BuffContainerViewModel("Buffs", slots, 11);
    }

    public void Reset()
    {
        Container = null;
        IsPicking = false;
        Results.Clear();
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

    [RelayCommand]
    private void PickBuff(BuffCatalogEntryViewModel? entry)
    {
        if (Container == null || entry == null) return;
        var slot = Container.Slots.FirstOrDefault(s => s.IsEmpty);
        if (slot == null) return;

        slot.PlaceBuff(entry.Id);
        _selectSlot(slot);
        IsPicking = false;
        SearchText = string.Empty;
    }
}
