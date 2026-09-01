using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Un buff activo del personaje - editable de verdad (duracion + quitar), cierra el hueco #1
// de la auditoria Terrasavr JS vs puerto (ver bitacora.md; NO se porto el guardado/carga de
// presets de buffs a fichero .json/.tsb del original, decision de alcance documentada).
// Cubre tanto buffs vanilla como de Calamity (fusionados en character.Buffs por
// CalamityCharacterSync.MergeBuffs, ids sinteticos >= CalamityIds.BuffIdBase). Envuelve el
// PlrBuff real y escribe en el mismo objeto - mismo patron que ColorSwatchViewModel.
public partial class BuffRowViewModel : ObservableObject
{
    private readonly Action<BuffRowViewModel> _requestRemove;
    private bool _suppressWriteback;

    public PlrBuff Buff { get; }
    public string Name { get; }
    public bool IsCalamity { get; }
    public string? IconPath { get; }

    // Duracion en segundos, editable - PlrBuff.Time va en ticks (60/seg, mismo criterio que
    // ya usaba la version solo-lectura).
    [ObservableProperty] private int _durationSeconds;

    public BuffRowViewModel(PlrBuff buff, string name, bool isCalamity, string? iconPath, Action<BuffRowViewModel> requestRemove)
    {
        Buff = buff;
        Name = name;
        IsCalamity = isCalamity;
        IconPath = iconPath;
        _requestRemove = requestRemove;

        _suppressWriteback = true;
        DurationSeconds = buff.Time / 60;
        _suppressWriteback = false;
    }

    partial void OnDurationSecondsChanged(int value)
    {
        if (_suppressWriteback) return;
        Buff.Time = Math.Max(0, value) * 60;
    }

    [RelayCommand]
    private void Remove() => _requestRemove(this);

    public static BuffRowViewModel From(PlrBuff buff, VanillaBuffCatalog vanillaCatalog, CalamityBuffCatalog calamityCatalog, Action<BuffRowViewModel> requestRemove)
    {
        bool isCalamity = buff.Id >= CalamityIds.BuffIdBase;
        string name;
        string? iconPath;
        if (isCalamity)
        {
            var entry = calamityCatalog.BySyntheticId(buff.Id);
            name = entry?.DisplayName ?? $"Calamity #{buff.Id}";
            iconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
        }
        else
        {
            name = vanillaCatalog.GetName(buff.Id);
            iconPath = VanillaBuffIconResolver.GetIconPath(buff.Id);
        }
        return new BuffRowViewModel(buff, name, isCalamity, iconPath, requestRemove);
    }
}
