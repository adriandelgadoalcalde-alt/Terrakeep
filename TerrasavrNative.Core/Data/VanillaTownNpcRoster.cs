namespace TerrasavrNative.Core.Data;

// Los NPCs de pueblo "reclutables" reales de Terraria vanilla (NPCID) - originalmente portado
// de VANILLA_TOWN_NPC_ROSTER en overrides.js (Terrasavr-Calamity-Beta, buscador de NPCs del
// visor de mundo), 27 entradas. Se usa para saber cuales le faltan todavia al jugador en un
// mundo dado.
//
// H6-08/H6-10 (sexta auditoria de Opus, "falta el NPC 441 en el roster"): la lista de 27 se
// quedaba corta de verdad - confirmado leyendo el propio NPC.cs decompilado real
// (Terraria/NPC.cs, cada "else if (type == N) { townNPC = true; ... }" real, incluidas las
// condiciones OR de una sola linea como "type == 637 || type == 638"): hay 39 NPCs reales con
// townNPC=true - los que faltaban eran los añadidos en 1.4.4/1.4.4.9 que el roster original
// (mas antiguo) nunca llego a incluir: TravelingMerchant (368), TaxCollector (441, el hallazgo
// explicito del informe) y las "mascotas de pueblo" 1.4.4 (TownCat/TownDog/TownBunny/
// TownSlime*, 637/638/656/670/678-684 - MUY relevante para esta misma ronda: son justo el tipo
// de NPC que el usuario confundia con mascotas de verdad en el mapa, y con razon eran
// invisibles/sin icono real hasta ahora).
//
// SkeletonMerchant (453) se queda tal cual, aunque NO pone townNPC=true en el codigo real
// (es friendly=true nada mas, un NPC de evento de Old One's Army) - ya estaba en el roster
// original y SI tiene perfil real en TownNPCProfiles.cs (ver NpcHeadProfile.cs), no se retira
// sin necesidad real de tocarlo.
public static class VanillaTownNpcRoster
{
    public static readonly IReadOnlyList<int> Ids =
    [
        17, 18, 19, 20, 22, 37, 38, 54, 107, 108, 124, 142, 160, 178,
        207, 208, 209, 227, 228, 229, 353, 368, 369, 441, 453, 550, 588,
        633, 637, 638, 656, 663, 670, 678, 679, 680, 681, 682, 683, 684
    ];
}
