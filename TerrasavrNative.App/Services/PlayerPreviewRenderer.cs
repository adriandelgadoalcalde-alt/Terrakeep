using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.Services;

// Compone el preview de "Apariencia"/"Inicio" de CUERPO COMPLETO a partir de los sprites REALES
// del jugador de Terraria, extraidos de la instalacion vanilla de este PC (Steam) con
// scripts/extraer-sprites-jugador.js (cuerpo+pelo) y scripts/extraer-sprites-armadura-
// vanilla.js (armadura/vanidad puesta).
//
// H6-01-b (advisor Opus, "la vanidad no se dibuja bien en el cuerpo delgado" - caso real
// "Eldelgas", bodySlot 93/"Vestido de la Muerte", skinVariant 8/MaleDress). Reescrito de raiz
// siguiendo ESPEC-dibujado-sprites.md (ingenieria inversa citada linea a linea contra
// Terraria/Player.cs, Terraria/DataStructures/PlayerDrawSet.cs y
// Terraria/DataStructures/PlayerDrawLayers.cs decompilados reales, version 1.4.5.8 = la misma
// que la instalacion de Steam). Los cinco fallos reales que corrige esta reescritura:
//   1. Con armadura/vanidad de CUERPO puesta, el juego real NO dibuja la ropa base
//      (camiseta/camisa/mangas) - antes se dibujaba siempre, encima de la vanidad.
//   2. hidesTopSkin/hidesBottomSkin (PlayerBodyDrawTables): algunos bodySlot/legSlot reales
//      (ej. 93) ocultan la piel del torso y/o de las piernas - antes se dibujaba siempre.
//   3. SetMatch (PlayerBodyDrawTables.SetMatchBodyToLegs/SetMatchLegsToLegs/SetMatchHead):
//      un bodySlot puede FORZAR el legSlot real puesto por otro (ej. body=93 -> legs=165,
//      la falda del vestido) - antes no se aplicaba nunca, se dibujaban las perneras/
//      pantalones equivocados.
//   4. Las variantes de cuerpo 1/2/3/5/6/7/8/9 (no solo 0/4) tienen ropa base real propia en
//      disco (scripts/extraer-sprites-jugador.js) - antes se usaba siempre body0/body4.
//   5. Orden brazo/hombro delantero invertido, y orden casco/pelo invertido en el caso
//      "fullHair" - ver PlayerDrawLayers.cs real citado en ESPEC-dibujado-sprites.md#7.3/#7.5.
//
// Celdas reales del frame de reposo (col, row) dentro de una hoja compuesta 9x4 - confirmado,
// SIN cambios respecto a la version anterior (ESPEC-dibujado-sprites.md#7.4: todos los offsets
// reales netean a cero para el frame de reposo):
//
//   TorsoFrame            (0,0) varon / (0,2) mujer
//   FrontShoulderFrame     (0,1) varon / (0,3) mujer
//   BackShoulderFrame      (1,1) varon / (1,3) mujer
//   FrontArmFrame          (2,0) - IGUAL en los dos generos
//   BackArmFrame           (2,2) - IGUAL en los dos generos
//
// H6-02/H6-01-b: `skinVariant` (0-11, Player.skinVariant real) sustituye a `isMale` como
// entrada - la carpeta de sprites usa PlayerVariantSets.BodyFolder (herencia real de
// PlayerDataInitializer.cs), pero las CELDAS (torso/hombros) siguen dependiendo solo de
// PlayerVariantSets.IsMale(skinVariant), exactamente como en el codigo real
// ("if (!drawPlayer.Male) pt.Y += 2" - nunca mira skinVariant directamente).
//
// H6-04: el id de pelo real es 0-based en el juego (Player.hair, 0..227) - Assets/player/hair/
// {id}.png viene de Player_Hair_{id+1}.xnb. HairStyle del .plr se usa tal cual, sin desfase.
//
// H6-05: la armadura/vanidad de Calamity Mod usa la MISMA convencion de hoja compuesta 360x224
// para el slot Body (LoadArmorCell). Calamity no comparte la numeracion de bodySlot/legSlot
// vanilla (registra sus propios equip slots por mod) - EquipmentAppearanceResolver deja
// BodySlot/LegsSlot a null para sus piezas, y aqui eso se traduce automaticamente en "sin
// SetMatch, sin hidesTopSkin/hidesBottomSkin, sin GetMatchingBodyExtension" (todas esas tablas
// devuelven su valor neutro con id=0) - el comportamiento fiel-por-defecto que pide
// ESPEC-dibujado-sprites.md#7.7 punto 8, sin caso especial en el codigo.
//
// Tintado = multiplicacion RGB pura, mapeo de color confirmado 1:1 contra los 7 campos ya en
// PlrCharacter: HairColor/SkinColor/EyeColor/ShirtColor/UnderColor/PantsColor/ShoesColor
// (EyeWhites siempre blanco, sin campo propio). La armadura real NUNCA se tinta con los colores
// del personaje (ESPEC-dibujado-sprites.md#4.8) - de ahi el "null" en todos los LoadArmorCell.
//
// H6-07 (pelo bajo el casco): sin cambios de fondo respecto a la pasada anterior, salvo el
// orden fullHair (ver mas abajo) - portado de Terraria.Player.GetHairSettings()/
// PlayerDrawLayers.cs reales (HairDrawProfile, tabla completa):
// - Sin casco, o casco "fullHair": pelo NORMAL. El orden real es CASCO primero, PELO despues
//   (PlayerDrawLayers.cs:2143-2161) - invertido respecto a lo que hacia el renderer antes de
//   esta pasada.
// - Casco "hatHair": pelo ALTERNATIVO (Player_HairAlt) PRIMERO, casco despues - esto ya
//   coincidia y no cambia.
// - Cualquier otro casco (el caso mas comun): sin pelo delantero, fiel al juego real.
// - Peinados "largos" (HairDrawProfile.IsBackHairDraw): capa TRASERA completa (la primerisima
//   del lienzo) + capa DELANTERA recortada a los 26px superiores reales. Solo objetos VANILLA
//   (headSlot real); Calamity oculta el pelo por defecto.
//
// ALCANCE DELIBERADO restante, documentado y no oculto: sin accesorios (alas, mochilas,
// capas...), item en mano, monturas, ni animacion (solo el frame de reposo) - ver
// ESPEC-dibujado-sprites.md#9 para el listado completo de huecos reales conocidos.
public static class PlayerPreviewRenderer
{
    private const int Width = 40, Height = 56;
    private const int SheetWidth = 360, SheetHeight = 224;
    private const int HairStyleMin = 0, HairStyleMax = 227;

    public readonly record struct Tint(byte R, byte G, byte B);

    public readonly record struct PlayerColors(Tint Hair, Tint Skin, Tint Eyes, Tint Shirt, Tint Under, Tint Pants, Tint Shoes);

    // Rutas absolutas reales (o null si ese slot no lleva nada puesto/reconocible) - ver
    // EquipmentAppearanceResolver, que es quien decide estas rutas. BodyFile es una hoja
    // compuesta 360x224, HeadFile/LegsFile son tiras verticales 40x56 (frame0).
    // HeadSlot/BodySlot/LegsSlot (H6-07/H6-01-b) son los indices REALES de esos slots
    // (Terraria.Player.head/body/legs, misma tabla que armor_{head,body,legs}/{slot}.png) -
    // null si el slot esta vacio o es un objeto de Calamity (numeracion propia, no compartida).
    public readonly record struct EquippedArmor(string? HeadFile, string? BodyFile, string? LegsFile,
        int? HeadSlot = null, int? BodySlot = null, int? LegsSlot = null);

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

    public static WriteableBitmap Render(int hairStyle, byte skinVariant, PlayerColors colors, EquippedArmor armor = default)
    {
        bool male = PlayerVariantSets.IsMale(skinVariant);
        string variant = PlayerVariantSets.BodyFolder(skinVariant);
        var torsoCell = male ? TorsoMale : TorsoFemale;
        var frontShoulderCell = male ? FrontShoulderMale : FrontShoulderFemale;
        var backShoulderCell = male ? BackShoulderMale : BackShoulderFemale;

        var canvas = new byte[Height * Width * 4];

        // ESPEC-dibujado-sprites.md#7.2: cadena real de SetMatch (Player.cs:36053-36092), tres
        // llamadas encadenadas que pueden sustituir legs/head. `bodyId`/`legsId` en 0 quiere
        // decir "sin bodySlot/legSlot vanilla conocido" (slot vacio o pieza de Calamity) - las
        // tres tablas de PlayerBodyDrawTables devuelven su valor neutro para 0, asi que no hace
        // falta ningun caso especial para Calamity aqui.
        int bodyId = armor.BodySlot ?? 0;
        int legsId = armor.LegsSlot ?? 0;
        int originalLegsId = legsId;
        bool wearsRobe = false;
        if (PlayerBodyDrawTables.SetMatchBodyToLegs(bodyId, male, legsId) is { } bodyToLegs)
        {
            legsId = bodyToLegs.Legs;
            wearsRobe = bodyToLegs.SetsWearsRobe;
        }
        if (PlayerBodyDrawTables.SetMatchLegsToLegs(legsId, male) is int legsToLegs) legsId = legsToLegs;

        int headId = armor.HeadSlot ?? 0;
        int? headIdAfterSetMatch = PlayerBodyDrawTables.SetMatchHead(headId, male);

        // ESPEC-dibujado-sprites.md#7.7 punto 8: sin bodySlot/legSlot vanilla conocido (pieza
        // de Calamity), hasBody se sigue considerando true si hay una hoja real que dibujar -
        // el comportamiento fiel-por-defecto para Calamity ("hasBody=true, sin banderas").
        bool hasBody = bodyId > 0 || armor.BodyFile != null;
        bool hidesTopSkin = PlayerBodyDrawTables.HidesTopSkin(bodyId);
        bool hidesBottomSkin = PlayerBodyDrawTables.HidesBottomSkin(bodyId, legsId);
        bool missingArm = PlayerBodyDrawTables.MissingArm(bodyId);
        bool missingHand = PlayerBodyDrawTables.MissingHand(bodyId);
        // GetMatchingBodyExtension usa el bodyId ORIGINAL (Player.cs: SetMatch nunca reescribe
        // "body", solo "legs"/"head" - drawPlayer.body es el mismo valor en toda la cadena).
        int? bodyExtension = PlayerBodyDrawTables.GetMatchingBodyExtension(bodyId, male);

        // H6-07: resuelve de verdad el comportamiento real del pelo bajo el casco puesto - ver
        // el comentario de la clase para la cita real de GetHairSettings()/PlayerDrawLayers.cs.
        bool hideHair = false, hatHair = false, fullHair = false;
        if (armor.HeadSlot is int realHeadSlot)
        {
            fullHair = HairDrawProfile.IsFullHair(realHeadSlot);
            hatHair = HairDrawProfile.IsHatHair(realHeadSlot);
            hideHair = !fullHair && !hatHair;
        }
        else if (armor.HeadFile != null)
        {
            // Casco puesto pero sin headSlot real resuelto (objeto de Calamity, numeracion
            // propia sin tabla real conocida) - se oculta el pelo por defecto, el
            // comportamiento MAS COMUN real de un casco completo vanilla.
            hideHair = true;
        }
        bool backHairDraw = HairDrawProfile.IsBackHairDraw(hairStyle);

        // ESPEC-dibujado-sprites.md#7.2 punto 3: si SetMatch sustituyo el headSlot (unico caso
        // real: 201 -> 202 en femenino), hay que dibujar el sprite REAL sustituido, no el del
        // objeto puesto - la ruta ya resuelta por EquipmentAppearanceResolver corresponde al
        // headSlot SIN sustituir.
        string? headFileToUse = armor.HeadFile;
        if (headIdAfterSetMatch is int newHeadId && newHeadId != headId)
        {
            string altPath = VanillaPath("armor_head", newHeadId);
            if (File.Exists(altPath)) headFileToUse = altPath;
        }

        // DrawPlayer_01_BackHair real: la capa TRASERA de un peinado largo se dibuja la
        // PRIMERISIMA de todas (antes incluso de piernas/torso), para que el resto del cuerpo
        // la tape por delante de forma natural al componer encima.
        if (!hideHair && backHairDraw)
            Composite(canvas, hatHair ? LoadHairAlt(hairStyle) : LoadHair(hairStyle), colors.Hair);

        // Paso 2-3 [12_Skin_Composite]: piel del torso y de las piernas, cada una solo si el
        // bodySlot/legSlot real puesto no la oculta (hidesTopSkin/hidesBottomSkin).
        if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "torsoskin", torsoCell), colors.Skin);
        if (!hidesBottomSkin) Composite(canvas, LoadFrame0(variant, "legskin"), colors.Skin);

        // Paso 4 [12_SkinComposite_BackArmShirt]: brazo TRASERO.
        if (hasBody)
        {
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", BackArm), colors.Skin);
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "hands", BackArm), colors.Skin);
            if (armor.BodyFile is { } bodyBackShoulderArmor) Composite(canvas, LoadArmorCell(bodyBackShoulderArmor, backShoulderCell), null);
            if (armor.BodyFile is { } bodyBackArmArmor) Composite(canvas, LoadArmorCell(bodyBackArmArmor, BackArm), null);
        }
        else
        {
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", BackArm), colors.Skin);
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "hands", BackArm), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", BackArm), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "armshirt", BackArm), colors.Shirt);
        }

        // Paso 5 [13_Leggings/14_Shoes]: perneras. El orden zapatos/perneras que invierte
        // wearsRobe no tiene efecto visual en el doll (el slot de zapatos, `shoeSlot`, esta
        // fuera de alcance - ESPEC-dibujado-sprites.md#8 punto 6), asi que solo se dibuja el
        // grupo de perneras: la pieza de armadura/vanidad si SetMatch la puso o el jugador la
        // llevaba, o pantalones+zapatos base si no hay ninguna.
        bool legsChangedBySetMatch = legsId != originalLegsId;
        string? legsFileToUse = legsChangedBySetMatch ? VanillaPathIfExists("armor_legs", legsId) : armor.LegsFile;
        if (legsId > 0 && legsFileToUse != null)
        {
            Composite(canvas, LoadFrame0Absolute(legsFileToUse), null);
        }
        else
        {
            Composite(canvas, LoadFrame0(variant, "pants"), colors.Pants);
            Composite(canvas, LoadFrame0(variant, "shoes"), colors.Shoes);
        }

        // Paso 6 [15_SkinLongCoat]: el faldon del vestido/abrigo (pieza 14) - solo variantes
        // 3/7/8, y solo sin armadura/vanidad de cuerpo puesta.
        if (!hasBody && skinVariant is 3 or 7 or 8)
            Composite(canvas, LoadFrame0(variant, "extra"), colors.Shirt);

        // Paso 7 [16_ArmorLongCoat]: el faldon largo de la ARMADURA (GetMatchingBodyExtension),
        // capa aparte de las perneras del paso 5 - se dibuja SIEMPRE que aplique, encima.
        if (bodyExtension is int extId)
        {
            string? extPath = VanillaPathIfExists("armor_legs", extId);
            if (extPath != null) Composite(canvas, LoadFrame0Absolute(extPath), null);
        }

        // Paso 8 [17_TorsoComposite]: con armadura/vanidad de cuerpo puesta, el juego real NO
        // dibuja la ropa base (bug #1 del caso "Eldelgas") - solo la armadura, en el torso.
        if (hasBody)
        {
            if (armor.BodyFile is { } bodyTorso) Composite(canvas, LoadArmorCell(bodyTorso, torsoCell), null);
        }
        else
        {
            Composite(canvas, LoadBodyCell(variant, "undershirt", backShoulderCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "shirt", backShoulderCell), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "undershirt", torsoCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "shirt", torsoCell), colors.Shirt);
        }

        // Paso 9 [21_Head]: cabeza/ojos/pelo/casco. Orden real: casco ANTES que el pelo cuando
        // el casco es "fullHair" (:2143-2161, invertido respecto a la version anterior de este
        // renderer); en cualquier otro caso (hatHair o sin casco) el pelo va primero, como ya
        // hacia el renderer antes de esta pasada.
        Composite(canvas, LoadFrame0("body0", "head"), colors.Skin);
        Composite(canvas, LoadFrame0("body0", "eyewhites"), null); // ya blanco en el sprite real
        Composite(canvas, LoadFrame0("body0", "eyes"), colors.Eyes);

        void DrawHair()
        {
            if (hideHair) return;
            byte[] hairPixels = hatHair ? LoadHairAlt(hairStyle) : LoadHair(hairStyle);
            // PlayerDrawSet.cs real: "hairFrontFrame.Height = 26" cuando backHairDraw - solo
            // el flequillo/parte superior real se ve por delante, el resto queda "detras" (ya
            // pintado en la capa trasera de arriba).
            if (backHairDraw) CompositeTopRows(canvas, hairPixels, colors.Hair, 26);
            else Composite(canvas, hairPixels, colors.Hair);
        }
        void DrawHelmet()
        {
            if (headFileToUse is { } headFile) Composite(canvas, LoadFrame0Absolute(headFile), null);
        }

        if (fullHair) { DrawHelmet(); DrawHair(); }
        else { DrawHair(); DrawHelmet(); }

        // Paso 10 [28_ArmOverItemComposite]: brazo DELANTERO, encima de todo lo anterior.
        // Orden real: BRAZO primero, HOMBRO despues (PlayerDrawSet.cs: compShoulderOverFrontArm
        // = true, el bucle real dibuja primero i==num3/brazo y luego i==num2/hombro) - invertido
        // respecto a la version anterior de este renderer.
        if (hasBody)
        {
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", FrontArm), colors.Skin);
            // 10b usa la pieza 9 (ArmHand), NO la 5 (Hands) - PlayerDrawLayers.cs:3735.
            if (missingHand && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armhand", FrontArm), colors.Skin);
            if (armor.BodyFile is { } bodyFrontArmArmor) Composite(canvas, LoadArmorCell(bodyFrontArmArmor, FrontArm), null);
            if (armor.BodyFile is { } bodyFrontShoulderArmor) Composite(canvas, LoadArmorCell(bodyFrontShoulderArmor, frontShoulderCell), null);
        }
        else
        {
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", FrontArm), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", FrontArm), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "armshirt", FrontArm), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "shirt", FrontArm), colors.Shirt);

            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", frontShoulderCell), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", frontShoulderCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "armshirt", frontShoulderCell), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "shirt", frontShoulderCell), colors.Shirt);
        }

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

    private static string VanillaPath(string dir, int id) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "player", dir, id + ".png");

    private static string? VanillaPathIfExists(string dir, int id)
    {
        string path = VanillaPath(dir, id);
        return File.Exists(path) ? path : null;
    }

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

    // H6-07: gemelo real de LoadHair para Player_HairAlt_{id+1}.xnb (el sprite real que el
    // juego dibuja cuando el casco puesto esta en la lista real "hatHair").
    private static byte[] LoadHairAlt(int hairStyle)
    {
        int id = Math.Clamp(hairStyle, HairStyleMin, HairStyleMax);
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hairalt", id + ".png");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hairalt", "0.png");
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

    // H6-07: gemelo real de Composite, pero solo compone las primeras "maxRows" filas del
    // origen (PlayerDrawSet.cs real: "hairFrontFrame.Height = 26" para un peinado largo bajo
    // un casco - solo el flequillo/parte superior se ve por delante).
    private static void CompositeTopRows(byte[] dst, byte[] src, Tint? tint, int maxRows)
    {
        int rows = Math.Min(maxRows, Height);
        for (int y = 0; y < rows; y++)
        {
            int rowStart = y * Width * 4;
            for (int i = rowStart; i < rowStart + Width * 4; i += 4)
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
