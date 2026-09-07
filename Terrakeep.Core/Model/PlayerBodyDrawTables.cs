namespace Terrakeep.Core.Model;

// H6-01-b (advisor Opus, "la vanidad no se dibuja bien en el cuerpo delgado" - ver
// ESPEC-dibujado-sprites.md, escrito tras investigar el caso real "Eldelgas": bodySlot 93,
// "Vestido de la Muerte"). Todas las tablas de aqui son transcripcion LITERAL de
// Terraria.Player.SetMatch (Player.cs:37458-37694), Terraria.Player.cs:36045-36092 (la cadena
// real de tres llamadas), Terraria.DataStructures.PlayerDrawSet.cs:373 (missingHand),
// :378-385 (missingArm), :1795-1796 (hidesTopSkin/hidesBottomSkin) y
// Terraria.DataStructures.PlayerDrawLayers.cs:1850-1926/:1840-1848 (GetMatchingBodyExtension/
// _Back) - re-leidas linea a linea desde el decompilado real de
// Downloads\tModLoader-Decompiled\TerrariaVanilla\ (version 1.4.5.8, misma que la instalacion
// real de Steam) al escribir este fichero, no copiadas de memoria del espec.
//
// Mismo patron ya establecido por HairDrawProfile (H6-07): estas tres funciones NO dependen de
// ningun dato que Terrakeep no tenga ya (bodySlot/legSlot visual real + PlayerVariantSets.IsMale)
// - no hace falta modelar SetMatchRequest.Player/mount, el doll no dibuja monturas.
public static class PlayerBodyDrawTables
{
    // Player.cs:36045-36092, ArmorSlotRequested==1 (body->legs), la UNICA de las tres llamadas
    // que puede marcar wearsRobe (las otras dos pasan un "ref" descartable) - "flag" en el
    // codigo real empieza true y solo el caso 166 lo pone a false.
    public readonly record struct BodyToLegsMatch(int Legs, bool SetsWearsRobe);

    // currentLegs = el legSlot visual YA resuelto por la prioridad vanidad>armadura, ANTES de
    // que ninguna de las tres llamadas de SetMatch actue (es el valor real de "legs" en el
    // momento de la PRIMERA llamada, Player.cs:36053-36061) - solo lo usa el caso 81.
    public static BodyToLegsMatch? SetMatchBodyToLegs(int body, bool male, int currentLegs) => body switch
    {
        15 => new(88, true),
        36 => new(89, true),
        41 => new(97, true),
        42 => new(90, true),
        58 => new(91, true),
        59 => new(92, true),
        60 => new(93, true),
        61 => new(94, true),
        62 => new(95, true),
        63 => new(96, true),
        77 => new(121, true),
        165 => new(!male ? 99 : 118, true),
        166 => new(!male ? 100 : 119, false), // flag = false: NO marca wearsRobe
        167 => new(male ? 101 : 102, true),
        180 => new(115, true),
        181 => new(116, true),
        183 => new(male ? 136 : 123, true),
        191 => new(131, true),
        93 => new(165, true),
        90 => new(166, true),
        88 => new(168, true),
        81 when currentLegs == -1 || currentLegs == 0 => new(169, true),
        213 => new(187, true),
        215 => new(189, true),
        219 => new(196, true),
        221 => new(199, true),
        223 => new(204, true),
        231 => new(214, true),
        232 => new(215, true),
        233 => new(216, true),
        241 => new(229, true),
        256 => new(244, true),
        _ => null,
    };

    // Player.cs:36063-36074, ArmorSlotRequested==2 (legs->legs) - segunda llamada de la cadena,
    // opera sobre el "legs" YA sustituido por la llamada anterior.
    public static int? SetMatchLegsToLegs(int legs, bool male) => legs switch
    {
        83 when male => 117,
        84 when male => 120,
        132 when male => 135,
        57 when male => 137,
        180 when !male => 179,
        184 when !male => 183,
        146 => male ? 146 : 147,
        154 => male ? 155 : 154,
        158 when male => 157,
        191 when !male => 192,
        193 when !male => 194,
        197 when !male => 198,
        203 when !male => 202,
        208 when !male => 207,
        219 when !male => 220,
        232 when !male => 233,
        236 when !male => 248,
        249 when !male => 250,
        _ => null,
    };

    // Player.cs:36076-36092, ArmorSlotRequested==0 (head->head) - tercera y ultima llamada de
    // la cadena. En el codigo real hay una excepcion por montura tipo 54 (num2 = 201, es decir
    // "sin cambio") - el doll no dibuja monturas, asi que esa rama nunca aplica aqui.
    public static int? SetMatchHead(int head, bool male) => head == 201 ? (male ? 201 : 202) : null;

    // PlayerDrawSet.cs:1795-1796, literal.
    public static bool HidesTopSkin(int body) => body is 82 or 83 or 93 or 21 or 22;
    public static bool HidesBottomSkin(int body, int legs) => body == 93 || legs is 20 or 21 or 216 or 214 or 215;

    // PlayerDrawSet.cs:378-385, literal ("missingArm = body != 83").
    public static bool MissingArm(int body) => body != 83;

    // PlayerDrawSet.cs:373, literal (71 ids reales, deduplicados - el codigo real repite 87 y
    // 168 dos veces cada uno dentro de la misma cadena de ||, sin efecto salvo redundancia).
    private static readonly HashSet<int> MissingHandBodies =
    [
        10, 11, 12, 13, 14, 15, 16, 20, 36, 38, 39, 40, 41, 42, 43, 44, 45, 50, 52, 53, 57, 58,
        59, 60, 61, 62, 63, 64, 68, 74, 76, 77, 78, 81, 85, 86, 87, 88, 98, 99, 100, 103, 104,
        165, 166, 167, 168, 169, 171, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 191, 192,
        196, 197, 198, 199, 202, 203, 209, 210, 211, 213,
    ];
    public static bool MissingHand(int body) => MissingHandBodies.Contains(body);

    // PlayerDrawLayers.cs:1850-1926 (GetMatchingBodyExtension) - el faldon largo de la
    // ARMADURA (no de la vanidad de cuerpo, ver DrawPlayer_15_SkinLongCoat/seccion 4.2 del
    // espec para ese caso, que es un id de VARIANTE, no de bodySlot).
    public static int? GetMatchingBodyExtension(int body, bool male) => body switch
    {
        200 => 149,
        202 => 151,
        201 => 150,
        209 => 160,
        207 => 161,
        198 => 162,
        182 => 163,
        168 => 164,
        73 => 170,
        52 => !male ? 172 : 171,
        187 => 173,
        205 => 174,
        53 => !male ? 176 : 175,
        210 => !male ? 177 : 178,
        211 => !male ? 181 : 182,
        218 => 195,
        222 => !male ? 200 : 201,
        225 => 206,
        236 => 221,
        237 => 223,
        89 => 186,
        81 => 169,
        251 => 238,
        _ => null,
    };

    // PlayerDrawLayers.cs:1840-1848 (GetMatchingBodyExtensionBack) - fuera del alcance del doll
    // en reposo (solo se usa para la capa trasera de un abrigo largo con animacion), se deja
    // aqui documentado por si se necesita en el futuro, no consumido todavia.
    public static int? GetMatchingBodyExtensionBack(int body) => body == 251 ? 239 : null;
}
