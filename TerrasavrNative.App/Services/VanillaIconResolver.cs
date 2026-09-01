using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de un objeto vanilla, extraido de items.png (atlas 32 columnas x celdas de
// 40x40, indice = id real del objeto, fila-mayor - confirmado visualmente contra id=1/
// IronPickaxe e id=231/MoltenHelmet) a PNGs individuales por id en
// Assets/vanilla/icons/{id}.png (ver bitacora.md). No todos los ids tienen archivo (1 de
// 5455 cae en una celda vacia del atlas original, PalladiumDrill/1189) - devuelve null en ese
// caso y la UI cae al icono de reserva, igual que hacia con TODOS los vanilla antes de esto.
public static class VanillaIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "icons");

    public static string? GetIconPath(int itemId)
    {
        string file = Path.Combine(IconsDir, $"{itemId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/icons/" + itemId + ".png" : null;
    }
}
