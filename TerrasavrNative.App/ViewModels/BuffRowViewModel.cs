using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Un buff activo del personaje - solo lectura por ahora (quitar/anadir un buff es un paso
// posterior). Cubre tanto buffs vanilla como de Calamity (fusionados en character.Buffs por
// CalamityCharacterSync.MergeBuffs, ids sinteticos >= CalamityIds.BuffIdBase).
public sealed class BuffRowViewModel(string name, int seconds, bool isCalamity, string? iconPath)
{
    public string Name { get; } = name;
    public string Duration { get; } = FormatDuration(seconds);
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "";
        int h = seconds / 3600, m = seconds % 3600 / 60, s = seconds % 60;
        return h > 0 ? $"{h}h {m}m" : m > 0 ? $"{m}m {s}s" : $"{s}s";
    }

    public static BuffRowViewModel From(PlrBuff buff, VanillaBuffCatalog vanillaCatalog, CalamityBuffCatalog calamityCatalog)
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
        return new BuffRowViewModel(name, buff.Time / 60, isCalamity, iconPath); // Time viene en ticks (60/seg)
    }
}
