namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): TerrakeepMod comprueba "¿lleva un gancho de verdad?" preguntando al
// motor en vivo (Main.projHook[objeto.shoot], ver EstadoJugadorGuia.LlevaGancho) - vale para
// CUALQUIER gancho, incluido uno de un mod, sin mantener ninguna lista. Terrakeep no tiene un
// motor en marcha al que preguntar, asi que aqui hace falta una lista real de ids de objeto.
//
// Lista de VAINILLA confirmada contra el ItemID.cs decompilado real
// (tModLoader-Decompiled\TerrariaVanilla\Terraria\ID\ItemID.cs, grep de "Hook = <numero>") - 23
// ganchos de verdad (arma de gancho, Player.itemAnimation dispara un projHook). Deliberadamente
// EXCLUIDOS de la lista (mismos criterio de "no inventar" del resto del proyecto):
//   - Hook=118: material de crafteo (parte de la caña de pescar), no un arma de gancho.
//   - FishHook=2360 / HotlineFishingHook=2422 / LavaFishingHook=4881: ganchos de caña de PESCAR,
//     item.shoot no apunta a Main.projHook para estos.
// No incluye ganchos de Calamity (fuera del alcance de esta ronda - exigiria decompilar
// CalamityMod.dll para confirmar cuales de sus "hook" son ganchos reales, ver bitacora.md).
public static class GuideHooks
{
    public static readonly HashSet<int> IdsDeGancho =
    [
        84,   // GrapplingHook
        437,  // DualHook
        1236, // AmethystHook
        1237, // TopazHook
        1238, // SapphireHook
        1239, // EmeraldHook
        1240, // RubyHook
        1241, // DiamondHook
        1800, // BatHook
        1829, // SpookyHook
        1915, // CandyCaneHook
        1916, // ChristmasHook
        2585, // SlimeHook
        2800, // AntiGravityHook
        3020, // TendonHook
        3021, // ThornHook
        3022, // IlluminantHook
        3023, // WormHook
        3572, // LunarHook
        3623, // StaticHook
        4257, // AmberHook
        4759, // SquirrelHook
        4980, // QueenSlimeHook
    ];
}
