using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de un buff vanilla, extraido de buffs.png (misma rejilla que items.png: 32
// columnas, celdas de 40x40, indice = id real del buff en fila-mayor - formula confirmada
// leyendo overrides.js real, "40*(id&31), 40*(id>>5) in drawBuff", y verificada visualmente
// contra id=1/Obsidian Skin e id=353/Shimmer) a PNGs individuales en
// Assets/vanilla/buff_icons/{id}.png (ver bitacora.md).
public static class VanillaBuffIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "buff_icons");

    public static string? GetIconPath(int buffId)
    {
        string file = Path.Combine(IconsDir, $"{buffId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/buff_icons/" + buffId + ".png" : null;
    }
}
