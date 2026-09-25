using System;
using System.Collections.Generic;

namespace Terrakeep.Core.Data;

// GapAnalysis Encargo I (25-sep-2026): catalogo REAL de los dyes de armadura VANILLA,
// transcrito 1:1 de Terraria.Initializers.DyeInitializer.LoadArmorDyes() (decompilado real,
// tModLoader-Decompiled\TerrariaVanilla\Terraria\Initializers\DyeInitializer.cs, lineas 13-141).
//
// Terraria tinta con un SHADER (GameShaders.Armor), no con un color RGB simple - cada dye
// registra un ArmorShaderData con un nombre de efecto .fx real y, la mayoria, un
// UseColor(r,g,b) que el shader multiplica contra el pixel ya renderizado del sprite -
// EXACTAMENTE la misma "multiplicacion RGB pura" que este proyecto ya usa para Hair/Skin/Eyes/
// Shirt/Under/Pants/Shoes (ver el comentario de cabecera de PlayerPreviewRenderer.cs,
// "Tintado = multiplicacion RGB pura", y su metodo Composite). Clasificacion real (no estimada,
// no inventada), basada en el NOMBRE DEL EFECTO .fx que cada dye registra:
//
//   PLANO (recolor estatico, reproducible sin perdida con Composite/Tint tal cual ya existe):
//   "ArmorColored", "ArmorColoredAndBlack", "ArmorBrightnessColored",
//   "ArmorColoredAndSilverTrim" - 4 efectos, ninguno usa textura de ruido, segundo color,
//   ciclo de tiempo, vida/mana, bioma ni equipo: pixel_final = pixel_sprite * color, SIEMPRE
//   el mismo color. 57 items reales (52 de LoadBasicColorDyes: 12 colores base x4 variantes
//   real/negro/brillante/plata, mas BrownDye con sus 4 variantes explicitas; +5 sueltos:
//   BlackDye/SilverDye/BrightSilverDye/ShadowDye/SilverAndBlackDye).
//
//   ANIMADO/SHADER (fuera de alcance de este encargo, "mejor sin tinte que un tinte
//   incorrecto" - DELIBERATE DIFFERENCE documentada, PlainColor/IsKnownPlainDye devuelven
//   false/null para todos ellos): el resto de los 63 items reales que registra
//   DyeInitializer.LoadArmorDyes - gradientes de 2 colores en el tiempo (*Gradient), arcoiris
//   (*Rainbow/*LivingRainbow), fuego/oceano/wisp "vivos" (Living*/Wisp), texturas de ruido
//   animadas (Acid/Gel/Fog/Mushroom/Phase/Twilight/ShiftingSands/Mirage/Polarized/Hades/Loki/
//   Solar/Nebula/Vortex/Stardust/Void/Martian/HallowBoss), reflejo de escena en tiempo real
//   (Reflective*), color dependiente del EQUIPO del jugador (Team, dinamico por definicion,
//   nunca fijo), inversion de color (Invert) o el shader "ColorOnly" sin UseColor conocido
//   (3978, comportamiento real no investigado a fondo - se trata como ANIMADO/desconocido por
//   seguridad, nunca se inventa un color). Ninguno tiene un color fijo real que extraer sin
//   replicar el efecto .fx completo (ruido/tiempo/estado del jugador) - ese es el "hueco real"
//   documentado del encargo, no un descuido.
//
// Calamity: FUERA DE ALCANCE de este encargo (LIMITE REAL) - CalamityMod registra sus propios
// dyes con sus propios shaders .fx via su propio sistema de contenido (Content/Items/Dyes/),
// no decompilado ni clasificado aqui por volumen real (Calamity tiene mas de 40 dyes propios,
// clasificarlos con la misma evidencia primaria de arriba seria un encargo aparte del mismo
// tamano que este). Un dye de Calamity (item.Id >= CalamityIds.ItemIdBase) SIEMPRE devuelve
// null aqui (sin tinte) - fiel-por-defecto, mismo criterio ya establecido en todo
// EquipmentAppearanceResolver para el resto de canales sin datos de Calamity todavia.
public static class DyeShaderCatalog
{
    public readonly record struct DyeColor(byte R, byte G, byte B);

    private static DyeColor Of(float r, float g, float b) => new(
        (byte)Math.Clamp(MathF.Round(r * 255f), 0f, 255f),
        (byte)Math.Clamp(MathF.Round(g * 255f), 0f, 255f),
        (byte)Math.Clamp(MathF.Round(b * 255f), 0f, 255f));

    private static readonly Dictionary<int, DyeColor> PlainColors = Build();

    private static Dictionary<int, DyeColor> Build()
    {
        var map = new Dictionary<int, DyeColor>();

        // LoadBasicColorDye(baseDyeItem, r,g,b) real (DyeInitializer.cs:22-25/13-20): 4
        // variantes reales por color base, mismos offsets EXACTOS que el juego
        // (blackDyeItem=base+12, brightDyeItem=base+31, silverDyeItem=base+44, spot-check real
        // contra ItemID.cs: RedDye=1007 -> RedandBlackDye=1019(+12) -> BrightRedDye=1038(+31) ->
        // RedandSilverDye=1051(+44)). Base/negro/plata usan el color EXACTO pasado a UseColor
        // (los 3 comparten el mismo valor real en DyeInitializer.cs:16/17/19 - el negro/plata
        // solo cambian el shader .fx, no el color); brillante usa la formula real
        // "r*0.5f+0.5f" (linea 18, transcrita literal).
        void Basic(int baseId, float r, float g, float b)
        {
            var baseColor = Of(r, g, b);
            map[baseId] = baseColor;
            map[baseId + 12] = baseColor;
            map[baseId + 31] = Of(r * 0.5f + 0.5f, g * 0.5f + 0.5f, b * 0.5f + 0.5f);
            map[baseId + 44] = baseColor;
        }

        Basic(1007, 1f, 0f, 0f);      // RedDye
        Basic(1008, 1f, 0.5f, 0f);    // OrangeDye
        Basic(1009, 1f, 1f, 0f);      // YellowDye
        Basic(1010, 0.5f, 1f, 0f);    // LimeDye
        Basic(1011, 0f, 1f, 0f);      // GreenDye
        Basic(1012, 0f, 1f, 0.5f);    // TealDye
        Basic(1013, 0f, 1f, 1f);      // CyanDye
        Basic(1014, 0.2f, 0.5f, 1f);  // SkyBlueDye
        Basic(1015, 0f, 0f, 1f);      // BlueDye
        Basic(1016, 0.5f, 0f, 1f);    // PurpleDye
        Basic(1017, 1f, 0f, 1f);      // VioletDye
        Basic(1018, 1f, 0.1f, 0.5f);  // PinkDye

        // BrownDye (DyeInitializer.cs:41): UNICO color base con los 4 ids EXPLICITOS en vez de
        // offsets (BrownDye=2874, BrownAndBlackDye=2875, BrightBrownDye=2876,
        // BrownAndSilverDye=2877 - confirmados en ItemID.cs) - misma formula real para cada uno.
        var brown = Of(0.4f, 0.2f, 0f);
        map[2874] = brown;
        map[2875] = brown;
        map[2876] = Of(0.4f * 0.5f + 0.5f, 0.2f * 0.5f + 0.5f, 0f * 0.5f + 0.5f);
        map[2877] = brown;

        // 5 dyes PLANOS sueltos (DyeInitializer.cs:48-52) - "ArmorBrightnessColored" (x3) /
        // "ArmorColoredAndBlack" (x1) / "ArmorColoredAndBlack" (x1), sin gradiente/ruido/
        // segundo color en ninguno de los 5.
        map[1050] = Of(0.6f, 0.6f, 0.6f);      // BlackDye
        map[1037] = Of(1f, 1f, 1f);            // SilverDye
        map[3558] = Of(1.5f, 1.5f, 1.5f);      // BrightSilverDye
        map[2871] = Of(0.05f, 0.05f, 0.05f);   // ShadowDye
        map[3559] = Of(1f, 1f, 1f);            // SilverAndBlackDye

        return map;
    }

    // null = no es un dye PLANO real conocido - puede ser un dye ANIMADO real (ver el
    // comentario de la clase), un objeto que no es dye en absoluto, o un dye de Calamity
    // (numeracion propia, nunca cae en esta tabla vanilla). "Sin tinte" es siempre el
    // resultado seguro por defecto - "mejor sin tinte que un tinte incorrecto".
    public static DyeColor? PlainColor(int itemId) => PlainColors.TryGetValue(itemId, out var c) ? c : null;

    public static bool IsKnownPlainDye(int itemId) => PlainColors.ContainsKey(itemId);

    // Conteo real para verificacion/documentacion - 57 PLANO (ver el comentario de la clase).
    public static int PlainDyeCount => PlainColors.Count;
}
