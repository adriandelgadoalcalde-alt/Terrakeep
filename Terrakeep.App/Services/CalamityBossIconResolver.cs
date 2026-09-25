using System.IO;

namespace Terrakeep.App.Services;

// GuiaCalamity Encargo B (25-sep-2026): hermano DIRECTO de BossIconResolver (Encargo4, vanilla),
// pero para jefes de CalamityMod - la app de escritorio no tiene ninguna partida en marcha de la
// que preguntar un NPC type real (ver comentario de GuideCatalog.cs:12-17), asi que aqui NO se
// resuelve por type numerico: se resuelve DIRECTAMENTE por el pid crudo del catalogo
// ("CalamityMod/InternalName", el mismo formato que ya usa idMod/idsMod para objetos), extrayendo
// solo el InternalName para el nombre de fichero. Assets/calamity_boss_icons/{InternalName}.png,
// extraido DIRECTAMENTE de los .rawimg de icono de cabeza de jefe reales del propio .tmod
// (NPCs/.../{Algo}_Head_Boss.rawimg o el override manual correspondiente - ver
// scripts/extraer-sprites-jefes-calamity.js para el mapeo completo de los 29 pids reales).
// Cubre los 27 pids REALMENTE usados como paso.jefeMod en guia_progresion.json; los otros 2
// (HiveMind/PerforatorHive, solo jefeFinalMod/jefeFinalModCarmesi de tramo) se extraen igualmente
// para el futuro pero hoy ningun paso los consume - mismo patron ya documentado para
// jefeFinal=13 (Eater of Worlds) en BossIconResolver/Encargo4.
public static class CalamityBossIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "calamity_boss_icons");

    // pid = "CalamityMod/InternalName" (el mismo formato crudo que trae paso.JefeMod desde el
    // JSON sin resolver, ver GuideModel.cs) - null/vacio o sin barra real devuelve null, nunca
    // inventa un sprite.
    public static string? GetIconPath(string? pid)
    {
        if (string.IsNullOrEmpty(pid)) return null;

        int barra = pid.IndexOf('/');
        if (barra <= 0 || barra >= pid.Length - 1) return null;
        string interno = pid[(barra + 1)..];

        string file = Path.Combine(IconsDir, $"{interno}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/calamity_boss_icons/" + interno + ".png" : null;
    }
}
