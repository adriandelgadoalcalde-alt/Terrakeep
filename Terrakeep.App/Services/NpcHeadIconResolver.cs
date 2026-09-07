using System.IO;

namespace Terrakeep.App.Services;

// H6-08/H6-09/H6-10 (sexta auditoria de Opus): icono real de CABEZA de NPC (Images/
// NPC_Head_{indice}.xnb reales, extraidos con scripts/extraer-cabezas-npc.js) - el marcador
// del mapa de mundo, no el sprite de cuerpo entero (NpcIconResolver, que se queda para la
// lista lateral). El indice de fichero YA es el indice real resuelto por NpcHeadProfile
// (0..80), no el tipo de NPC.
public static class NpcHeadIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "npc_heads");

    public static string? GetIconPath(int headIndex)
    {
        string file = Path.Combine(IconsDir, $"{headIndex}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/npc_heads/" + headIndex + ".png" : null;
    }
}
