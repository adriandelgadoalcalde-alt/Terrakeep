using System.IO;

namespace Terrakeep.App.Services;

// Guia Encargo4 (25-sep-2026): sprite real de cuerpo entero de un jefe vanilla -
// Assets/boss_icons/{npcType}.png, extraido DIRECTAMENTE de Images/NPC_{type}.xnb real
// (instalacion de Steam, un frame representativo por jefe - ver scripts/
// extraer-sprites-jefes-vanilla.js para el algoritmo de recorte real). Cubre los 23 NPC types
// de jefe/segmento final REALMENTE usados en guia_progresion.json (campo "jefe" de cada paso +
// "jefeFinal" de cada tramo) - un jefe que no este en ese conjunto (o el unico jefe de mod real
// del catalogo, HiveMindOPerforator/CalamityMod, que no tiene .xnb vanilla) devuelve null y la
// UI cae a "sin icono" (converter NullToVis ya usado en toda la app), nunca un hueco roto.
// Hermano deliberado de NpcIconResolver (NPCs de pueblo) - misma forma, carpeta distinta, porque
// un jefe NO es un NPC de pueblo y puede compartir numeracion de id con nada del roster.
public static class BossIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "boss_icons");

    public static string? GetIconPath(int npcType)
    {
        string file = Path.Combine(IconsDir, $"{npcType}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/boss_icons/" + npcType + ".png" : null;
    }
}
