using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de un NPC de pueblo, copiado tal cual de Terrasavr-Calamity-Beta
// (Assets/npc_icons/{id}.png) - solo cubre los 27 NPCs de VanillaTownNpcRoster (los unicos
// que la app original tenia extraidos, para su buscador de NPCs). Cualquier otro id (NPCs no
// reclutables, enemigos...) devuelve null y la UI cae a texto.
public static class NpcIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "npc_icons");

    public static string? GetIconPath(int npcId)
    {
        string file = Path.Combine(IconsDir, $"{npcId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/npc_icons/" + npcId + ".png" : null;
    }
}
