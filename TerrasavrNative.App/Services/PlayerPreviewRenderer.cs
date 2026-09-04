using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerrasavrNative.App.Services;

// Compone el preview de "Apariencia"/"Inicio" de CUERPO COMPLETO a partir de los sprites REALES
// del jugador de Terraria, extraidos de la instalacion vanilla de este PC (Steam) con
// scripts/extraer-sprites-jugador.js (cuerpo+pelo) y scripts/extraer-sprites-armadura-
// vanilla.js (armadura/vanidad puesta).
//
// H6-01 (Opus, sexta pasada - "les faltan los brazos a todos los personajes, ¿de que sirve un
// editor de apariencia si no refleja como esta construido de verdad el personaje?"): reescrito
// de raiz. La version anterior recortaba SIEMPRE la celda (0,0) de 40x56 de cada hoja - correcto
// para las tiras verticales (Head/EyeWhites/Eyes/LegSkin/Pants/Shoes/pelo), pero CERO-relleno
// para las 8 hojas compuestas (TorsoSkin/Undershirt/Hands/Shirt/ArmSkin/ArmUndershirt/ArmHand/
// ArmShirt, rejillas 9x4 de 360x224) - el brazo/mano/camiseta interior real NO viven en la
// celda (0,0) de esas hojas. Confirmado leyendo Terraria/DataStructures/PlayerDrawSet.cs +
// PlayerDrawLayers.cs decompilados reales (CreateCompositeFrameRect/UpdateCompositeArm/
// DrawPlayer_12_Skin_Composite/_12_SkinComposite_BackArmShirt/_17_TorsoComposite/
// _28_ArmOverItemComposite) - las celdas reales del frame de reposo son:
//
//   TorsoFrame            (0,0) varon / (0,2) mujer
//   FrontShoulderFrame     (0,1) varon / (0,3) mujer
//   BackShoulderFrame      (1,1) varon / (1,3) mujer
//   FrontArmFrame          (2,0) - IGUAL en los dos generos
//   BackArmFrame           (2,2) - IGUAL en los dos generos
//
// (el "+2 filas" para mujer es literal en PlayerDrawLayers.cs: "if (!drawPlayer.Male) pt.Y += 2"
// - solo afecta a Torso/Hombros, nunca a los brazos). El orden real de capas (mismos metodos
// DrawPlayer_* de arriba, en su orden real de llamada) es: 1) LegSkin+TorsoSkin: 2) brazo
// TRASERO (ArmSkin/Hands/ArmUndershirt/ArmShirt en BackArmFrame); 3) Pantalones/Zapatos;
// 4) Undershirt+Shirt en BackShoulderFrame, despues otra vez en TorsoFrame; 5) Cabeza/Ojos/
// Pelo; 6) brazo DELANTERO (ArmSkin/ArmUndershirt/ArmShirt/Shirt en FrontShoulderFrame, despues
// otra vez en FrontArmFrame). Todas las celdas se superponen en el MISMO origen (0,0) del
// lienzo final - Main.OffsetsPlayerHeadgear[0]=(0,2) con "y -= 2" en PlayerDrawLayers.cs deja
// el offset neto en (0,0) para el frame de reposo, confirmado componiendo y viendo una figura
// de Terraria completa y reconocible (brazos, manos, mangas incluidos).
//
// H6-03: dos variantes de CUERPO reales (0=MaleStarter, 4=FemaleStarter - las UNICAS con hoja
// propia para las 10 piezas de ropa/piel en la instalacion real; las variantes 1/2/3/5-11 solo
// sustituyen un subconjunto y heredan el resto de 0/4, alcance deliberado no cubierto en esta
// pasada, documentado en extraer-sprites-jugador.js) - isMale elige entre body0/body4. Head/
// EyeWhites/Eyes son compartidas por TODAS las variantes (solo existe Player_0_{0,1,2}.xnb en
// la instalacion real) y se cargan siempre de body0.
//
// H6-04: el id de pelo real es 0-based en el juego (Player.hair, 0..227) - Assets/player/hair/
// {id}.png viene de Player_Hair_{id+1}.xnb (confirmado: AssetInitializer.cs real hace
// "Images/Player_Hair_" + (num4+1)). HairStyle del .plr se usa tal cual, sin desfase.
//
// H6-05 (consecuencia real de este mismo arreglo): la armadura/vanidad de Calamity Mod usa la
// MISMA convencion de hoja compuesta 360x224 para el slot Body (confirmado: los .png de
// Assets/calamity/icons/*_Body.png miden 360x224) - antes de este cambio, LoadPngPixels asumia
// SIEMPRE 40x56 y una pieza de Calamity en el slot Body hacia explotar CopyPixels
// (ArgumentOutOfRangeException real, verificado antes de este cambio) - HomeViewModel.
// ScanCharacters la tragaba en un catch mudo, asi que un personaje con equipo de Calamity
// puesto desaparecia del listado de Inicio en silencio. Con LoadArmorCell (comparte el mismo
// mecanismo que TorsoSkin/etc) esto se resuelve solo, sin caso especial para Calamity.
//
// Tintado = multiplicacion RGB pura, mapeo de color confirmado 1:1 contra los 7 campos ya en
// PlrCharacter: HairColor/SkinColor/EyeColor/ShirtColor/UnderColor/PantsColor/ShoesColor
// (EyeWhites siempre blanco, sin campo propio).
//
// ALCANCE DELIBERADO restante, documentado y no oculto: sin accesorios (alas, mochilas,
// capas...), item en mano, ni animacion (solo el frame de reposo) - la inmensa mayoria de
// accesorios no tienen capa visual propia sobre el cuerpo. Pelo bajo casco (Player.
// GetHairSettings real, "hideHair"/"hatHair" segun el casco puesto) y pelo largo detras del
// cuerpo (backHairDraw) tampoco se replican - H6-07, fuera de esta pasada, documentado.
public static class PlayerPreviewRenderer
{
    private const int Width = 40, Height = 56;
    private const int SheetWidth = 360, SheetHeight = 224;
    private const int HairStyleMin = 0, HairStyleMax = 227;

    public readonly record struct Tint(byte R, byte G, byte B);

    public readonly record struct PlayerColors(Tint Hair, Tint Skin, Tint Eyes, Tint Shirt, Tint Under, Tint Pants, Tint Shoes);

    // Rutas absolutas reales (o null si ese slot no lleva nada puesto/reconocible) - ver
    // EquipmentAppearanceResolver, que es quien decide estas rutas. BodyFile es ahora una hoja
    // compuesta 360x224 (antes 40x56 ya recortada) - HeadFile/LegsFile siguen siendo 40x56
    // (tiras verticales, sin cambios).
    public readonly record struct EquippedArmor(string? HeadFile, string? BodyFile, string? LegsFile);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> Cache = new();

    // Celdas reales del frame de reposo (col, row) dentro de una hoja compuesta 9x4 - ver el
    // comentario de la clase para la cita real de PlayerDrawSet.cs/PlayerDrawLayers.cs.
    private static readonly (int Col, int Row) TorsoMale = (0, 0);
    private static readonly (int Col, int Row) TorsoFemale = (0, 2);
    private static readonly (int Col, int Row) FrontShoulderMale = (0, 1);
    private static readonly (int Col, int Row) FrontShoulderFemale = (0, 3);
    private static readonly (int Col, int Row) BackShoulderMale = (1, 1);
    private static readonly (int Col, int Row) BackShoulderFemale = (1, 3);
    private static readonly (int Col, int Row) FrontArm = (2, 0);
    private static readonly (int Col, int Row) BackArm = (2, 2);

    public static WriteableBitmap Render(int hairStyle, bool isMale, PlayerColors colors, EquippedArmor armor = default)
    {
        string variant = isMale ? "body0" : "body4";
        var torsoCell = isMale ? TorsoMale : TorsoFemale;
        var frontShoulderCell = isMale ? FrontShoulderMale : FrontShoulderFemale;
        var backShoulderCell = isMale ? BackShoulderMale : BackShoulderFemale;

        var canvas = new byte[Height * Width * 4];

        // 1) DrawPlayer_12_Skin_Composite: piernas + torso (piel).
        Composite(canvas, LoadFrame0(variant, "legskin"), colors.Skin);
        Composite(canvas, LoadBodyCell(variant, "torsoskin", torsoCell), colors.Skin);
        if (armor.BodyFile is { } bodyTorso) Composite(canvas, LoadArmorCell(bodyTorso, torsoCell), null);

        // 2) DrawPlayer_12_SkinComposite_BackArmShirt: brazo TRASERO completo (piel, mano,
        // camiseta interior, camisa).
        Composite(canvas, LoadBodyCell(variant, "armskin", BackArm), colors.Skin);
        Composite(canvas, LoadBodyCell(variant, "hands", BackArm), colors.Skin);
        Composite(canvas, LoadBodyCell(variant, "armundershirt", BackArm), colors.Under);
        Composite(canvas, LoadBodyCell(variant, "armshirt", BackArm), colors.Shirt);
        if (armor.BodyFile is { } bodyBackArm) Composite(canvas, LoadArmorCell(bodyBackArm, BackArm), null);

        // 3) Piernas/zapatos.
        Composite(canvas, LoadFrame0(variant, "pants"), colors.Pants);
        if (armor.LegsFile is { } legsFile) Composite(canvas, LoadFrame0Absolute(legsFile), null);
        Composite(canvas, LoadFrame0(variant, "shoes"), colors.Shoes);

        // 4) DrawPlayer_17_TorsoComposite: camiseta interior + camisa en el hombro trasero, y
        // otra vez en el torso.
        Composite(canvas, LoadBodyCell(variant, "undershirt", backShoulderCell), colors.Under);
        Composite(canvas, LoadBodyCell(variant, "shirt", backShoulderCell), colors.Shirt);
        if (armor.BodyFile is { } bodyBackShoulder) Composite(canvas, LoadArmorCell(bodyBackShoulder, backShoulderCell), null);
        Composite(canvas, LoadBodyCell(variant, "undershirt", torsoCell), colors.Under);
        Composite(canvas, LoadBodyCell(variant, "shirt", torsoCell), colors.Shirt);

        // 5) Cabeza/ojos/pelo (head/eyewhites/eyes: compartidas por todas las variantes,
        // siempre de body0 - ver el comentario de la clase).
        Composite(canvas, LoadFrame0("body0", "head"), colors.Skin);
        Composite(canvas, LoadFrame0("body0", "eyewhites"), null); // ya blanco en el sprite real
        Composite(canvas, LoadFrame0("body0", "eyes"), colors.Eyes);
        Composite(canvas, LoadHair(hairStyle), colors.Hair);
        if (armor.HeadFile is { } headFile) Composite(canvas, LoadFrame0Absolute(headFile), null);

        // 6) DrawPlayer_28_ArmOverItemComposite: brazo DELANTERO completo, dibujado ENCIMA de
        // todo lo anterior (piel, camiseta interior, camisa - dos veces, hombro y despues
        // brazo).
        Composite(canvas, LoadBodyCell(variant, "armskin", frontShoulderCell), colors.Skin);
        Composite(canvas, LoadBodyCell(variant, "armundershirt", frontShoulderCell), colors.Under);
        Composite(canvas, LoadBodyCell(variant, "armshirt", frontShoulderCell), colors.Shirt);
        Composite(canvas, LoadBodyCell(variant, "shirt", frontShoulderCell), colors.Shirt);
        if (armor.BodyFile is { } bodyFrontShoulder) Composite(canvas, LoadArmorCell(bodyFrontShoulder, frontShoulderCell), null);

        Composite(canvas, LoadBodyCell(variant, "armskin", FrontArm), colors.Skin);
        Composite(canvas, LoadBodyCell(variant, "armundershirt", FrontArm), colors.Under);
        Composite(canvas, LoadBodyCell(variant, "armshirt", FrontArm), colors.Shirt);
        Composite(canvas, LoadBodyCell(variant, "shirt", FrontArm), colors.Shirt);
        if (armor.BodyFile is { } bodyFrontArm) Composite(canvas, LoadArmorCell(bodyFrontArm, FrontArm), null);

        var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), canvas, Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    // Miniatura de un unico peinado (sin cuerpo) para el selector visual de Apariencia -
    // pedido explicito 1-sep-2026, "sprites de los diferentes cabeza corte de pelo... que se
    // pueda ver". Reusa el mismo sprite/tintado real que el preview completo.
    public static WriteableBitmap RenderHairThumbnail(int hairStyle, Tint hairColor)
    {
        var canvas = new byte[Height * Width * 4];
        Composite(canvas, LoadHair(hairStyle), hairColor);
        var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), canvas, Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    // H6-04: 228 estilos reales, 0-based (Player.hair real) - ver el comentario de la clase.
    public static int HairStyleCount => HairStyleMax - HairStyleMin + 1;

    private static byte[] LoadFrame0(string variant, string name) =>
        LoadFrame0Absolute(Path.Combine(AppContext.BaseDirectory, "Assets", "player", variant, name + ".png"));

    private static byte[] LoadFrame0Absolute(string path) => LoadCached(path);

    // Recorta la celda real de una pieza compuesta del CUERPO (360x224) - ver el mapa de celdas
    // en el comentario de la clase.
    private static byte[] LoadBodyCell(string variant, string name, (int Col, int Row) cell) =>
        SliceCell(LoadSheetCached(Path.Combine(AppContext.BaseDirectory, "Assets", "player", variant, name + ".png")), cell);

    // Gemelo de LoadBodyCell, para una pieza de ARMADURA/vanidad puesta (ruta absoluta real ya
    // resuelta por EquipmentAppearanceResolver - vanilla o Calamity, misma convencion 360x224
    // las dos).
    private static byte[] LoadArmorCell(string absolutePath, (int Col, int Row) cell) =>
        SliceCell(LoadSheetCached(absolutePath), cell);

    // H6-04: el id de HairStyle del .plr se usa tal cual como nombre de archivo (0-227, mismo
    // rango 0-based que el propio Player.hair real) - si el id no tiene archivo, cae al
    // estilo 0 en vez de fallar, un peinado "equivocado" es preferible a un preview roto.
    private static byte[] LoadHair(int hairStyle)
    {
        int id = Math.Clamp(hairStyle, HairStyleMin, HairStyleMax);
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hair", id + ".png");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hair", "0.png");
        return LoadCached(path);
    }

    private static byte[] LoadCached(string path) => Cache.GetOrAdd(path, LoadPngPixels40x56);

    // El prefijo "sheet:" es SOLO para no colisionar en la MISMA Cache con LoadCached (una hoja
    // completa 360x224 y un recorte 40x56 del mismo fichero son datos distintos) - GetOrAdd
    // invoca al factory con la CLAVE, no con "path", asi que el factory tiene que ser un cierre
    // sobre la ruta real (bug real de esta misma pasada, atrapado por dotnet test: con
    // Cache.GetOrAdd("sheet:" + path, LoadPngPixelsSheet) el factory recibia literalmente
    // "sheet:C:\...\torsoskin.png" como "path" - PngBitmapDecoder leia "sheet" como el ESQUEMA
    // de un Uri invalido, "The URI prefix is not recognized").
    private static byte[] LoadSheetCached(string path) => Cache.GetOrAdd("sheet:" + path, _ => LoadPngPixelsSheet(path));

    // Stream (File.OpenRead) en vez de Uri - mismo patron ya establecido en el resto del
    // proyecto para leer PNG reales desde disco, evita depender de System.Net.WebRequest para
    // resolver un simple fichero local.
    private static byte[] LoadPngPixels40x56(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var converted = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        var pixels = new byte[Width * Height * 4];
        converted.CopyPixels(pixels, Width * 4, 0);
        return pixels;
    }

    private static byte[] LoadPngPixelsSheet(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var converted = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        var pixels = new byte[SheetWidth * SheetHeight * 4];
        converted.CopyPixels(pixels, SheetWidth * 4, 0);
        return pixels;
    }

    // Recorta una celda de 40x56 real de una hoja compuesta ya decodificada (cacheada entera -
    // el recorte en si es barato, no hace falta cachear tambien el resultado por celda).
    private static byte[] SliceCell(byte[] sheet, (int Col, int Row) cell)
    {
        var outPixels = new byte[Width * Height * 4];
        int startX = cell.Col * Width, startY = cell.Row * Height;
        for (int y = 0; y < Height; y++)
        {
            int srcOffset = ((startY + y) * SheetWidth + startX) * 4;
            int dstOffset = y * Width * 4;
            Array.Copy(sheet, srcOffset, outPixels, dstOffset, Width * 4);
        }
        return outPixels;
    }

    // Todas las piezas (ya sean tiras 40x56 o celdas recortadas de una hoja) miden 40x56 al
    // llegar aqui - se puede componer indice a indice sin cuentas de offset.
    private static void Composite(byte[] dst, byte[] src, Tint? tint)
    {
        for (int i = 0; i < src.Length; i += 4)
        {
            byte b = src[i], g = src[i + 1], r = src[i + 2], a = src[i + 3];
            if (a == 0) continue;

            if (tint is { } t)
            {
                r = (byte)(r * t.R / 255);
                g = (byte)(g * t.G / 255);
                b = (byte)(b * t.B / 255);
            }

            float alpha = a / 255f;
            dst[i + 0] = (byte)(b * alpha + dst[i + 0] * (1 - alpha));
            dst[i + 1] = (byte)(g * alpha + dst[i + 1] * (1 - alpha));
            dst[i + 2] = (byte)(r * alpha + dst[i + 2] * (1 - alpha));
            dst[i + 3] = (byte)(a + dst[i + 3] * (1 - alpha));
        }
    }
}
