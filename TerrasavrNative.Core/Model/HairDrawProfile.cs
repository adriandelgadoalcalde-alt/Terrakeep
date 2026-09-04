namespace TerrasavrNative.Core.Model;

// H6-07 (sexta auditoria de Opus, Tanda D - "pelo bajo el casco/pelo largo detras del cuerpo"):
// tabla real, portada tal cual de Terraria.Player.GetHairSettings() decompilado real
// (Terraria/Player.cs). El campo real "head" de ese metodo es el headSlot (el mismo indice que
// ya usa EquipmentAppearanceResolver para Assets/player/armor_head/{slot}.png, confirmado
// contra AssetInitializer.cs real: "Images/Armor_Head_" + n).
//
// Reglas reales (GetHairSettings + su consumo real en PlayerDrawLayers.cs, DrawPlayer_01_BackHair
// y el bloque de "pelo delantero" cerca de DrawPlayer_21_Head):
// - headSlot en FullHairHeadSlots: el casco NO tapa el pelo - se dibuja con el sprite NORMAL
//   (Player_Hair_{id}.xnb) encima del casco.
// - headSlot en HatHairHeadSlots: el casco deja ver una version "recogida" del pelo - sprite
//   DISTINTO real (Player_HairAlt_{id}.xnb, Images/Player_HairAlt_ + (id+1) real).
// - headSlot en NINGUNA de las dos listas (el caso mas comun, cascos completos): el pelo NO se
//   dibuja en absoluto - ausencia real en el propio codigo del juego (GetHairSettings nunca
//   pone fullHair/hatHair a true para esos ids, y el bloque de dibujo real solo dibuja pelo
//   delantero cuando fullHair o hatHair son ciertos), no una omision de este puerto.
// - sin casco puesto (headSlot==null aqui, head==-1 real): el pelo se dibuja siempre normal,
//   igual que si fuera FullHair (real: "head == -1 || fullHair || drawsBackHairWithoutHeadgear").
public static class HairDrawProfile
{
    // Formula real EXACTA de backHairDraw (Player.cs, GetHairSettings): "num > 50 && (num < 56
    // || num > 63) && (num < 74 || num > 77) && (num < 88 || num > 89) && num != 94 &&
    // num != 100 && num != 104 && num != 112 && num < 116" + los 5 ids sueltos reales que
    // fuerzan backHairDraw=true fuera de ese rango (6, 133, 134, 146, 162).
    public static bool IsBackHairDraw(int hairStyle)
    {
        bool enRango = hairStyle > 50
            && (hairStyle < 56 || hairStyle > 63)
            && (hairStyle < 74 || hairStyle > 77)
            && (hairStyle < 88 || hairStyle > 89)
            && hairStyle != 94 && hairStyle != 100 && hairStyle != 104 && hairStyle != 112
            && hairStyle < 116;
        return enRango || hairStyle is 6 or 133 or 134 or 146 or 162;
    }

    public static bool IsFullHair(int headSlot) => FullHairHeadSlots.Contains(headSlot);
    public static bool IsHatHair(int headSlot) => HatHairHeadSlots.Contains(headSlot);

    // Lista real EXACTA (GetHairSettings, case fullHair = true).
    private static readonly HashSet<int> FullHairHeadSlots =
    [
        10, 12, 28, 42, 62, 97, 106, 113, 116, 119, 133, 138, 139, 163, 178, 181, 191, 198,
        217, 218, 220, 222, 224, 225, 228, 229, 230, 232, 235, 238, 242, 243, 244, 245,
        272, 273, 274, 277, 284, 290,
    ];

    // Lista real EXACTA (GetHairSettings, case hatHair = true).
    private static readonly HashSet<int> HatHairHeadSlots =
    [
        13, 14, 15, 16, 18, 21, 24, 25, 26, 29, 40, 44, 51, 56, 59, 60, 63, 64, 65, 67, 68,
        69, 81, 92, 94, 95, 100, 114, 121, 126, 130, 136, 140, 143, 145, 158, 159, 161, 182,
        184, 190, 195, 215, 216, 219, 223, 226, 227, 231, 233, 234, 262, 263, 264, 265, 267,
        275, 279, 280, 281, 286, 289, 292,
    ];
}
