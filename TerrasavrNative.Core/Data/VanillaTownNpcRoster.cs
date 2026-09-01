namespace TerrasavrNative.Core.Data;

// Los NPCs de pueblo "reclutables" reales de Terraria vanilla (NPCID) - portado tal cual de
// VANILLA_TOWN_NPC_ROSTER en overrides.js (Terrasavr-Calamity-Beta, buscador de NPCs del
// visor de mundo). Se usa para saber cuales le faltan todavia al jugador en un mundo dado.
// No incluye enemigos/mascotas/NPCs de evento, solo los que de verdad se mudan a una casa.
public static class VanillaTownNpcRoster
{
    public static readonly IReadOnlyList<int> Ids =
    [
        17, 18, 19, 20, 22, 37, 38, 54, 107, 108, 124, 142, 160, 178,
        207, 208, 209, 227, 228, 229, 353, 369, 453, 550, 588, 633, 663
    ];
}
