namespace Terrakeep.Core.Data;

// Punto 4 (advisor Opus, "buscador o marcador de minerales" - ver ESPEC-ui-exploracion.md#11.1).
//
// No existe ninguna categoria "mineral" en los datos: ni tile_names.json de este proyecto ni su
// fuente real (Data/tiles.json de TEdit, 754 entradas, campos comprobados uno a uno por el
// advisor) tienen ningun campo de familia. El propio TEdit la escribe A MANO dos veces:
// GenerateApi.cs:39-63 (tabla OreTypes) y SimpleOreGeneratorPlugin.cs:179-240 (GetSelectedOres,
// que ademas añade obsidiana/demonita/crimtane). Esta tabla es la union de las dos, mas las
// gemas (que TEdit no lista en ningun sitio; sus ids salen por nombre de Data/tiles.json real).
public static class OreTileCatalog
{
    // Minerales de veta - GenerateApi.cs:41-63 + SimpleOreGeneratorPlugin.cs:187-240.
    public static readonly IReadOnlyList<int> Metals =
    [
        7, 166,   // cobre / estaño
        6, 167,   // hierro / plomo
        9, 168,   // plata / tungsteno
        8, 169,   // oro / platino
        37,       // meteorito
        58,       // piedra infernal
        22, 204,  // demonita / crimtane
        56,       // obsidiana (solo en SimpleOreGeneratorPlugin, no en GenerateApi)
        107, 221, // cobalto / paladio
        108, 222, // mithril / oricalco
        111, 223, // adamantita / titanio
        211,      // clorofita
        408,      // luminita
    ];

    // Gemas - ids reales de Data/tiles.json de TEdit por nombre (TEdit no tiene lista de gemas).
    // 178 "Gems" son las gemas SUELTAS de pared de cueva (tile enmarcado, 84 variantes de UV);
    // las otras seis son el bloque de piedra con la gema dentro.
    public static readonly IReadOnlyList<int> Gems = [63, 64, 65, 66, 67, 68, 566, 178];

    // Otros objetivos reales de exploracion subterranea - no son mineral, pero es exactamente lo
    // que un jugador busca con la misma intencion ("¿donde hay un corazon de vida?"). Ids
    // verificados uno a uno en Data/tiles.json de TEdit por el advisor.
    public static readonly IReadOnlyList<int> Targets =
    [
        12,  // Corazon de vida (Crystal Heart)
        26,  // Altar demoniaco / carmesi
        31,  // Orbe sombrio / corazon carmesi
        129, // Fragmento de cristal
        236, // Fruta de la vida
        237, // Altar lihzahrd
        238, // Bulbo de Plantera
        404, 407, // Fosil del desierto / fosil resistente
        444, // Colmena
    ];

    public static readonly IReadOnlySet<int> All = new HashSet<int>([.. Metals, .. Gems, .. Targets]);
}
