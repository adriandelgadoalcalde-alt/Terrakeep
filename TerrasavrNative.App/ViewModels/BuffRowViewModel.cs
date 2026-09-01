using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Un buff activo del personaje - solo lectura por ahora (quitar/anadir un buff es un paso
// posterior). Los buffs de Calamity (modBuffs en el .tplr) NO estan cubiertos todavia - esta
// vista solo muestra los que caben en el array vanilla del .plr (PlrCharacter.Buffs).
public sealed class BuffRowViewModel(string name, int seconds)
{
    public string Name { get; } = name;
    public string Duration { get; } = FormatDuration(seconds);

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "";
        int h = seconds / 3600, m = seconds % 3600 / 60, s = seconds % 60;
        return h > 0 ? $"{h}h {m}m" : m > 0 ? $"{m}m {s}s" : $"{s}s";
    }

    public static BuffRowViewModel From(PlrBuff buff, TerrasavrNative.Core.Data.VanillaBuffCatalog catalog) =>
        new(catalog.GetName(buff.Id), buff.Time / 60); // Time viene en ticks (60/seg)
}
