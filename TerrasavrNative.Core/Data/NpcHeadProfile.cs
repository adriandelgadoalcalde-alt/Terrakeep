namespace TerrasavrNative.Core.Data;

// H6-08/H6-09/H6-10 (sexta auditoria de Opus, "el mapa muestra puntos rosas que el usuario cree
// que son mascotas -son NPCs- deberia verse solo cabezas de NPC, con el sprite real correcto").
// Tabla real, portada de Terraria.GameContent.TownNPCProfiles.cs decompilado (el diccionario
// _townNPCProfiles real, 40 entradas - EXACTAMENTE los 40 ids de VanillaTownNpcRoster.Ids) - el
// indice de icono de cabeza real (Images/NPC_Head_{indice}.xnb) NO es 1:1 con el tipo de NPC:
//
// - La mayoria son "Legacy con shimmer simple" (LegacyWithSimpleShimmer real): un indice fijo
//   normal y otro fijo si el NPC esta "shimmerizado" (WorldFile.LoadNPCs real: un array GLOBAL
//   de tipos de NPC shimmerizados por MUNDO, "NPC.ShimmeredTownNPCs[tipo]=true" - no es un
//   estado por instancia, es un estado por TIPO en ese mundo concreto).
// - OldMan (37) y SkeletonMerchant (453) tienen -1/-1 en el juego real (SIN icono de cabeza en
//   el mapa - ninguno de los dos tiene casa/perfil de pueblo real completo). Se documenta tal
//   cual (HeadNormal/HeadShimmered = null) en vez de inventar un icono que el juego real no
//   muestra.
// - Gato/Perro/Conejo de pueblo (637/638/656, la "mascota de pueblo" real 1.4.4 - el hallazgo
//   mas relevante de esta ronda, justo lo que el usuario confundia con una mascota de verdad)
//   usan un indice VARIABLE segun `npc.townNpcVariationIndex` real (0-5, guardado en el propio
//   .wld por instancia - ver WldNpc.VariationIndex/WldReader) sobre un array real de 6 cabezas
//   cada uno (CatHeadIDs/DogHeadIDs/BunnyHeadIDs reales).
// - Los Slimes de pueblo (670, 678-684 - 8 colores, cada uno su PROPIO tipo de NPC, no una
//   variacion del mismo tipo) tienen un indice fijo cada uno, sin variacion ni shimmer.
public static class NpcHeadProfile
{
    public readonly record struct Profile(int? HeadNormal, int? HeadShimmered, IReadOnlyList<int>? VariantHeadIds);

    // CatHeadIDs/DogHeadIDs/BunnyHeadIDs reales (TownNPCProfiles.cs).
    private static readonly int[] CatHeadIds = [27, 28, 29, 30, 31, 32];
    private static readonly int[] DogHeadIds = [33, 34, 35, 36, 37, 38];
    private static readonly int[] BunnyHeadIds = [39, 40, 41, 42, 43, 44];

    private static readonly Dictionary<int, Profile> Profiles = new()
    {
        [22] = new(1, 72, null),    // Guide
        [20] = new(5, 73, null),    // Dryad
        [19] = new(6, 74, null),    // ArmsDealer
        [107] = new(9, 75, null),   // GoblinTinkerer
        [160] = new(12, 76, null),  // Truffle
        [208] = new(15, 77, null),  // PartyGirl
        [228] = new(18, 78, null),  // WitchDoctor
        [550] = new(24, 79, null),  // Tavernkeep
        [369] = new(22, 55, null),  // Angler
        [54] = new(7, 57, null),    // Clothier
        [209] = new(16, 58, null),  // Cyborg
        [38] = new(4, 59, null),    // Demolitionist
        [207] = new(14, 60, null),  // DyeTrader
        [588] = new(25, 61, null),  // Golfer
        [124] = new(8, 62, null),   // Mechanic
        [17] = new(2, 63, null),    // Merchant
        [18] = new(3, 64, null),    // Nurse
        [227] = new(17, 65, null),  // Painter
        [229] = new(19, 66, null),  // Pirate
        [142] = new(11, 67, null),  // Santa
        [178] = new(13, 68, null),  // Steampunker
        [353] = new(20, 69, null),  // Stylist
        [441] = new(23, 70, null),  // TaxCollector
        [108] = new(10, 71, null),  // Wizard
        [663] = new(45, 54, null),  // Princess
        [633] = new(26, 56, null),  // BestiaryGirl (Zoologist)
        [37] = new(null, null, null),  // OldMan - sin icono real en el mapa
        [453] = new(null, null, null), // SkeletonMerchant - sin icono real en el mapa
        [368] = new(21, 80, null),  // TravelingMerchant
        [637] = new(null, null, CatHeadIds),   // TownCat - por variationIndex
        [638] = new(null, null, DogHeadIds),   // TownDog - por variationIndex
        [656] = new(null, null, BunnyHeadIds), // TownBunny - por variationIndex
        [670] = new(46, 46, null), // TownSlimeBlue
        [678] = new(47, 47, null), // TownSlimeGreen
        [679] = new(48, 48, null), // TownSlimeOld
        [680] = new(49, 49, null), // TownSlimePurple
        [681] = new(50, 50, null), // TownSlimeRainbow
        [682] = new(51, 51, null), // TownSlimeRed
        [683] = new(52, 52, null), // TownSlimeYellow
        [684] = new(53, 53, null), // TownSlimeCopper
    };

    // Indice real de Images/NPC_Head_{indice}.xnb para este NPC concreto - null si el juego
    // real tampoco muestra icono para el (OldMan/SkeletonMerchant). variationIndex es el
    // townNpcVariationIndex real leido del .wld (0 si no aplica/no presente); isShimmered viene
    // del set global NPC.ShimmeredTownNPCs real de ESE mundo.
    public static int? GetHeadIndex(int npcType, int variationIndex, bool isShimmered)
    {
        if (!Profiles.TryGetValue(npcType, out var profile)) return null;

        if (profile.VariantHeadIds is { Count: > 0 } variants)
            return variants[((variationIndex % variants.Count) + variants.Count) % variants.Count];

        return isShimmered ? profile.HeadShimmered : profile.HeadNormal;
    }
}
