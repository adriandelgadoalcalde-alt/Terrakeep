using System.IO;

namespace Terrakeep.App.Services;

// Icono "fantasma"/watermark real que Terraria dibuja en un slot de equipo vacio - extraido
// del atlas real del juego (Content/Images/Extra_54.xnb, ver
// scripts/extraer-iconos-fantasma-slot.js) a Assets/vanilla/slot_ghosts/{name}.png. Mismo
// patron que VanillaIconResolver.cs. "name" es el nombre semantico real (armor_head, dye,
// hook, mount, minecart, pet, pet_light, accessory, accessory_vanity, vanity_head/body/legs)
// - null en los slots sin ghost real (Moneda/Municion: el propio juego tampoco dibuja nada
// ahi, no se inventa uno).
public static class SlotGhostIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "slot_ghosts");

    public static string? GetIconPath(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string file = Path.Combine(IconsDir, $"{name}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/slot_ghosts/" + name + ".png" : null;
    }
}
