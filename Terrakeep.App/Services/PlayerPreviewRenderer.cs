using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Model;

namespace Terrakeep.App.Services;

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
// Idea 10 del catalogo de funciones ("vista previa animada", bitacora.md 20-sep-2026 -
// reconsiderada a peticion explicita del coordinador tras una investigacion mas a fondo: el
// "limite real" documentado antes -"ni animacion (solo el frame de reposo)"- resulto ser un
// recorte de la propia tira de EXTRACCION (cropFrame0 en legskin/pants/shoes/armor_legs), no
// una ausencia real de datos del juego - Terraria/Player.cs, PlayerFrame(), confirma que SI hay
// 20 filas reales de animacion por pieza y que la unica pieza que de verdad cambia de frame al
// andar SIN usar ningun objeto son las piernas (bodyFrame/headFrame/hairFrame se quedan fijos
// en su frame de reposo mientras itemAnimation==0). "legAnimationFrame" (0=reposo, 7..19=ciclo
// de andar real) + "mirror" (girar) son el resultado real de esa investigacion.
//
// ALCANCE DELIBERADO restante, documentado y no oculto: item en mano, monturas, ni animacion de
// torso/cabeza/pelo (torso/hombros confirmados FIJOS durante el andar real, ver WalkArmColumn) -
// ver ESPEC-dibujado-sprites.md#9 para el listado completo de huecos reales conocidos. Wings
// Encargo1 (25-sep-2026): "alas" sale de esta lista - ver LoadWingFrame/WingDrawTable para la
// capa base estatica ya portada (sin animacion/particulas, ver el ALCANCE DELIBERADO propio de
// WingDrawTable). CORRECCION 21-sep-2026 (hover en Inicio,
// bitacora.md): esta nota decia antes que el BRAZO tambien se quedaba fijo "fiel al juego real,
// solo cambia con un objeto en uso" - error real de investigacion de la pasada anterior,
// corregido con cita exacta (Player.cs:36038-36042 + PlayerDrawSet.cs:2942/2993-3028/3037-3038,
// ver WalkArmColumn arriba): el brazo SI cambia de celda durante el ciclo de andar simple.
public static class PlayerPreviewRenderer
{
    // PortSeleccion Encargo3 (25-sep-2026): Width/Height pasan de private a internal - hacen
    // falta desde fuera (Converters/PetPositionConverters.cs) para portar la formula real de
    // Terraria (UICharacter.cs, GetPlayerPosition/DrawPets) al tamano real del lienzo, en vez de
    // duplicar el numero 56 a mano en dos sitios. Terrakeep no modela un hitbox aparte del
    // sprite compuesto (a diferencia del jugador real, con player.width/height=20/42 vs el frame
    // visual 40x56) - el lienzo entero ES el "jugador" para efectos de este calculo, ver el
    // comentario completo de PetPositionConverters.cs.
    internal const int Width = 40, Height = 56;
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
    // GapAnalysis Encargo B (25-sep-2026): HeadBackFile NO es un slot independiente - lo calcula
    // EquipmentAppearanceResolver.ResolveHeadBack DERIVANDO el sprite "de espaldas" del propio
    // HeadSlot (via ArmorIDs.Head.Sets.FrontToBackID) - null en la inmensa mayoria de cascos
    // reales, que no tienen sprite "de espaldas" (ver PlayerBodyDrawTables.HeadFrontToBackID).
    // GapAnalysis Encargo I (25-sep-2026): HeadDye/BodyDye/LegsDye - tinte PLANO real
    // (EquipmentAppearanceResolver.ResolveDye, dye[0]/dye[1]/dye[2] del loadout) o null (sin
    // dye puesto, dye ANIMADO/SHADER real o dye de Calamity - los 3 casos "sin tinte", ver
    // DyeShaderCatalog). A diferencia del comentario de cabecera de la clase ("la armadura real
    // NUNCA se tinta con los colores del PERSONAJE" - Hair/Skin/Eyes/etc, PlayerColors), un dye
    // SI tiñe la armadura - son 2 mecanismos de tinte reales y distintos del juego (colorArmorX
    // vs colorHair/colorSkin/etc, Player.cs real), sin contradiccion.
    public readonly record struct EquippedArmor(string? HeadFile, string? BodyFile, string? LegsFile,
        int? HeadSlot = null, int? BodySlot = null, int? LegsSlot = null, string? HeadBackFile = null,
        Tint? HeadDye = null, Tint? BodyDye = null, Tint? LegsDye = null);

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

    // Hover en Inicio ("que ande solo, con brazos" - bitacora.md 21-sep-2026): el "ALCANCE
    // DELIBERADO" documentado en el comentario de la clase ("ni animacion de... brazos... fieles
    // al juego real - solo cambian con un objeto en uso") resulto ser un error de investigacion
    // de la pasada anterior, corregido aqui con cita real. Terraria/Player.cs real,
    // PlayerFrame() (36038-36042): durante el andar simple SIN item activo (legs!=140,
    // velocity.X!=0, sin itemAnimation/montura/nado/salto), "bodyFrame.Y = legFrame.Y" - el
    // brazo SI se sincroniza con la pierna. Terraria/DataStructures/PlayerDrawSet.cs real,
    // CreateCompositeData (2942: "num = bodyFrame.Y/bodyFrame.Height"; switch(num) 2993-3028)
    // confirma QUE cambia: frameIndex2.X (columna del brazo FRENTE, mismo sheet 9x4 360x224 que
    // ya carga LoadBodyCell/LoadArmorCell) segun el frame real de la pierna, fila SIEMPRE 1
    // durante el ciclo de andar (distinta de la fila de reposo, 0). El brazo TRASERO reusa la
    // MISMA columna (3037: "frameIndex.X = frameIndex2.X") con fila 3 (3038: "frameIndex.Y =
    // frameIndex2.Y + 2"). Torso y hombros (pt3/pt/pt2 en el codigo real) NO aparecen en ningun
    // case de ese rango - se quedan fijos en su celda de reposo durante el andar, confirmado no
    // tocarlos aqui. Solo cubre 6..19 (el rango real que WalkCycleRows/legAnimationFrame usan
    // aqui) - los casos 0-5 (salto/caida/nado) del mismo switch no aplican a un doll estatico sin
    // gravedad y no se portan.
    private static readonly System.Collections.Generic.Dictionary<int, int> WalkArmColumn = new()
    {
        [6] = 3, [7] = 4, [8] = 4, [9] = 4, [10] = 4,
        [11] = 3, [12] = 3, [13] = 3,
        [14] = 5, [15] = 6, [16] = 6, [17] = 5,
        [18] = 3, [19] = 3,
    };

    // Idea 10 del catalogo de funciones ("vista previa animada", bitacora.md 20-sep-2026):
    // legAnimationFrame es el indice REAL de fila (0-19) confirmado leyendo Terraria/Player.cs,
    // PlayerFrame() (0 = reposo, 7..19 = ciclo de andar real, ver AppearanceViewModel.
    // WalkCycleRows) - por defecto 0, exactamente el mismo pixel a pixel que antes de esta idea
    // para CUALQUIER llamador que no pase el parametro (Home, comparador, etc). Solo afecta a
    // las piezas cuyo frame de verdad cambia al andar sin usar nada (legskin/pants/shoes base y
    // la pierna/falda equipada, ver el comentario de cabecera de la clase) - torso/brazos/
    // cabeza/pelo se quedan en su frame de reposo siempre, fiel al juego real.
    // "mirror" (idea 10, "girar"): el juego real dibuja los MISMOS fotogramas hacia el otro
    // lado multiplicando los offsets por Player.direction (Terraria/Player.cs real) - el doll no
    // tiene offsets propios que compensar (siempre centrado), asi que un espejo horizontal
    // puro del lienzo compuesto final es fiel al resultado real sin tener que duplicar ninguna
    // logica de dibujado. Por defecto false - ningun llamador existente cambia ni un pixel.
    //
    // PortSeleccion Encargo2 (25-sep-2026): "accessories" (EquipmentAppearanceResolver.
    // ResolveAccessories, hermano de EquippedArmor) - los 7 sprites reales de accesorio
    // funcional/vanidad (waist/neck/handOn/handOff/back/shield/face). Citado en
    // PlayerDrawLayers.cs real (DrawPlayer_10_BackAcc:1236, _18_OffhandAcc:3374,
    // _19_WaistAcc:3425, _20_NeckAcc:3444, _22_FaceAcc:4484/4490, _25_Shield:4955-4961,
    // _29_OnhandAcc:6161): las 6 primeras (todas salvo Waist) usan SIEMPRE
    // `drawinfo.drawPlayer.bodyFrame` como rectangulo de origen sobre su PROPIA textura -
    // Waist usa `drawinfo.drawPlayer.legFrame` (o bodyFrame si el set real
    // ArmorIDs.Waist.Sets.UsesTorsoFraming lo pide, dato que este proyecto no tiene catalogado -
    // limite real documentado, ver el comentario de DrawWaist). bodyFrame.Y == legFrame.Y
    // durante el andar simple (Player.cs:36041, ya citado en el comentario de WalkArmColumn) -
    // los 7 tipos comparten entonces la MISMA fila real que ya modela legAnimationFrame (0 en
    // reposo), asi que cada sprite de accesorio se trata como una tira vertical 40x(56*N) igual
    // que legskin/pants/shoes/armor_legs (LoadStripFrameAbsolute, ya existente) - sin
    // duplicar ninguna logica de recorte nueva. UNICA excepcion real: Shield NO sigue el ancho
    // fijo de 40px (PlayerDrawLayers.cs:4958-4961, "bodyFrame.Width = shield.Value.Width" -
    // confirmado en los sprites extraidos, varios escudos vanilla miden 42/44px) - ver
    // LoadShieldFrame para el recorte dedicado, centrado sobre el lienzo de 40px.
    //
    // GapAnalysis Encargo A (25-sep-2026): "accessories.BackpackFile"/"TailFile" - 2 canales mas
    // en los que EquipmentAppearanceResolver puede reclasificar el mismo sprite ya resuelto para
    // "Back" (ver BackAccessoryLayerTable, tabla real de ArmorIDs.cs:1717/1719). Citado en
    // PlayerDrawLayers.cs real (DrawPlayer_08_Backpacks:479-484, DrawPlayer_08_1_Tails:568-584):
    // ambos usan la MISMA textura TextureAssets.AccBack y el MISMO `drawinfo.drawPlayer.bodyFrame`
    // que BackAcc - misma tira vertical 40x(56*N), DrawAccessory reutilizado sin logica nueva.
    public static WriteableBitmap Render(int hairStyle, byte skinVariant, PlayerColors colors, EquippedArmor armor = default, int legAnimationFrame = 0, bool mirror = false, EquippedAccessories? accessories = null)
    {
        bool male = PlayerVariantSets.IsMale(skinVariant);
        string variant = PlayerVariantSets.BodyFolder(skinVariant);
        var torsoCell = male ? TorsoMale : TorsoFemale;
        var frontShoulderCell = male ? FrontShoulderMale : FrontShoulderFemale;
        var backShoulderCell = male ? BackShoulderMale : BackShoulderFemale;

        var canvas = new byte[Height * Width * 4];

        // Compone (si hay sprite real) una capa de accesorio generica - null significa "vacio o
        // sin sprite extraible de esta instalacion" (ver EquipmentAppearanceResolver), nunca se
        // inventa nada, la capa simplemente no se dibuja. Waist/Neck/HandOn/HandOff/Back/Face
        // comparten esta misma tira vertical 40x(56*N) - ver el comentario real de la firma de
        // Render para la cita completa. Shield usa LoadShieldFrame aparte (ancho real variable).
        // GapAnalysis Encargo I (25-sep-2026): "tint" opcional (default null, byte a byte
        // identico para cualquier llamador existente que no lo pase) - el tinte PLANO real ya
        // resuelto por EquipmentAppearanceResolver para el canal de ESTE accesorio.
        void DrawAccessory(string? file, Tint? tint = null)
        {
            if (file is null) return;
            Composite(canvas, LoadStripFrameAbsolute(file, legAnimationFrame), tint);
        }

        // CalamityAccesorios (25-sep-2026): HandOn/HandOff son un caso real MIXTO, a diferencia
        // de los otros 8 tipos de accesorio (siempre tira 40x(56*N)) - confirmado en el motor
        // real (Terraria.ID.ArmorIDs.HandOn/HandOff.Sets.UsesNewFramingCode, EquipLoader.cs:
        // "case EquipType.HandsOn: ArmorIDs.HandOn.Sets.UsesNewFramingCode[key] = true;" para
        // CUALQUIER item de mod registrado en HandsOn/HandsOff, sin excepcion) que estos 2 tipos
        // se dibujan con la MISMA hoja compuesta 360x224 y frontArmCell/backArmCell que
        // armor.BodyFile (DrawCompositeArmorPiece real, PlayerDrawLayers.cs:298-299:
        // "FrontArmAccessory => (EquipType.HandsOn, ...)", "BackArmAccessory => (EquipType.
        // HandsOff, ...)"), NO con la tira simple de DrawAccessory. Confirmado con los 6 guantes
        // reales de Calamity (BloodstainedGlove, ElectriciansGlove, ElementalGauntlet,
        // FilthyGlove, GloveOfPrecision, GloveOfRecklessness): sus 12 sprites ya extraidos miden
        // 360x224 de verdad, NO 40x1120 (bug real atrapado por esta misma pasada: con
        // DrawAccessory sin cambios, el guante se resolvia bien en EquipmentAppearanceResolver
        // pero el render salia PIXEL A PIXEL IDENTICO a "sin nada puesto" - SliceStripRow asume
        // ancho de tira fijo Width(40), corrompe silenciosamente el recorte de una hoja de
        // 360px de ancho real). Los 24 sprites vanilla ya extraidos (acc_handon/acc_handoff) SI
        // miden 40x1120 (tira) - se detecta por el ANCHO/ALTO REAL del PNG decodificado, nunca
        // por el origen vanilla/Calamity, para no romper ni el contrato existente de esos 24
        // sprites ni las pruebas "orden" (PNG sinteticos 40x56 de
        // PlayerPreviewRendererAccessoriesTests, tambien caen en la rama tira).
        void DrawHandAccessory(string? file, (int Col, int Row) armCell, Tint? tint = null)
        {
            if (file is null) return;
            Composite(canvas, LoadHandAccessoryFrame(file, armCell, legAnimationFrame), tint);
        }

        // GapAnalysis Encargo J (25-sep-2026): Coat (item.type==5587, bodySlot sintetico 251) -
        // pieza de cuerpo ADICIONAL, MISMA hoja compuesta 360x224 y MISMAS celdas que
        // armor.BodyFile (LoadArmorCell reutilizado sin logica nueva), dibujada SIEMPRE ENCIMA de
        // lo que haya (armadura de cuerpo puesta o piel base) - confirmado en el decompilado real
        // que los 3 bloques que dibujan ArmorBodyComposite[coat]
        // (PlayerDrawLayers.cs:1406-1420/2029-2036/3828-3846) son un `if` SUELTO, sin relacion con
        // el `if(body>0)/else` que rodea a armor.BodyFile - por eso se llama DESPUES de cada
        // bloque hasBody/else, nunca dentro de el. CoatDye es un canal de dye REAL propio
        // (Player.cs:9839-9842, "cCoat = dyeItem.dye"), independiente de BodyDye - ver el
        // comentario real completo de EquippedAccessories.
        void DrawCoat((int Col, int Row) cell)
        {
            if (accessories?.CoatFile is { } coatFile) Composite(canvas, LoadArmorCell(coatFile, cell), accessories?.CoatDye);
        }

        // ParidadPersonaje Fase5 (26-sep-2026): FloatingTube (item.type==4404) - investigacion
        // previa lo marcaba INCONCLUSIVE ("depende de estar mojado"), CORREGIDO contra el
        // decompilado real: `drawFloatingTube = drawPlayer.hasFloatingTube && !hideEntirePlayer;`
        // (PlayerDrawSet.cs:2776) es IDENTICO al gate de drawUnicornHorn/drawAngelHalo, sin
        // ninguna dependencia real de wet/lavaWet/honeyWet en todo el pipeline de dibujado (grep
        // completo confirmado) - ver el comentario real completo de EquipmentAppearanceResolver.
        // A diferencia de Coat (misma hoja/celdas que armor.BodyFile), FloatingTube es una tira
        // SUELTA propia de 2 filas (Assets/player/extra/105.png, 40x112) sin relacion con la
        // rejilla 360x224 del cuerpo - se dibuja DOS VECES con la MISMA posicion "torso" (formula
        // identica a torsoskin/ArmorBodyComposite[torso], sin compositeOffset_BackArm/FrontArm)
        // pero fila DISTINTA cada vez: fila 0 (PlayerDrawLayers.cs:2294-2300, DrawPlayer_12_
        // Skin_Composite, mitad "trasera") y fila 1 (PlayerDrawLayers.cs:3352-3358, DrawPlayer_17_
        // TorsoComposite, mitad "delantera") - split espacial fijo, NO animacion en el tiempo
        // (por eso usa LoadStripFrameAbsolute con una fila FIJA por punto de llamada, nunca
        // legAnimationFrame como DrawAccessory). Dye propio real (`cFloatingTube`,
        // Player.cs:8149), independiente de BodyDye - mismo patron que CoatDye.
        void DrawFloatingTube(int frameRow)
        {
            if (accessories?.FloatingTubeFile is { } floatingTubeFile)
                Composite(canvas, LoadStripFrameAbsolute(floatingTubeFile, frameRow), accessories?.FloatingTubeDye);
        }

        // GapAnalysis Encargo E (25-sep-2026): accessories?.FrontFile (item.frontSlot,
        // Player.cs:37185-37188) NO es una capa unica "encima de todo" - es el MISMO sprite
        // recortado en 2 mitades (PlayerDrawLayers.cs:3908-3993,
        // DrawPlayer_32_FrontAcc_FrontPart/_BackPart): FrontPart = mitad IZQUIERDA de la tira
        // (bodyFrame.Width -= num, num = bodyFrame.Width/2, arranca en X=0), BackPart = mitad
        // DERECHA (mismo ancho recortado, bodyFrame.X += num). La tira en si SIGUE la
        // convencion estandar 40x(56*N) alineada al lienzo (TextureAssets.AccFront usa
        // drawPlayer.bodyFrame directamente, PlayerDrawLayers.cs:3897-3904 - NO el ancho
        // variable de Shield, se reutiliza LoadStripFrameAbsolute/SliceStripRow sin cambios), lo
        // UNICO nuevo es el recorte de mitad sobre el frame YA resuelto por fila (MaskHalf).
        // Posicion real de cada mitad (confirmado linea a linea: `vector` en ambos metodos usa
        // SIEMPRE `drawinfo.drawPlayer.bodyFrame` completo, NO la copia local ya recortada, y
        // `bodyVect`/origin solo se desplaza -num cuando SpriteEffects.FlipHorizontally esta
        // activo - orientacion que este renderer NUNCA usa por capa, compone siempre "normal" y
        // espeja el LIENZO COMPLETO al final, FlipHorizontal, mismo criterio ya establecido para
        // el resto de capas) - en la orientacion normal cada mitad del ORIGEN se compone en la
        // MISMA mitad de posicion del lienzo (izquierda->izquierda, derecha->derecha), sin
        // desplazamiento extra que portar aqui.
        //
        // Condicion real de incompatibilidad (PlayerDrawLayers.cs:3910/3953, identica en los 2
        // metodos): "front<=0 || front>=Count || (DontDrawIfWearingAScarfOrCape[front] &&
        // (neck>0 && IsAScarf[neck] || back>0 && IsACape[back]))" - transcrita en
        // PlayerBodyDrawTables.FrontDontDrawIfWearingScarfOrCape/NeckIsAScarf/BackIsACape (ver
        // su comentario real para la correccion importante encontrada: NINGUN accesorio Front
        // REAL alcanza nunca el unico frontId con la condicion activa - LIMITE REAL documentado
        // ahi, la tabla se porta igual, fiel y completa). accessories?.FrontSlot es null para
        // Calamity (numeracion propia no compartida) - sin FrontSlot conocido, NUNCA se aplica
        // la incompatibilidad (fiel-por-defecto, mismo criterio que ShoesSlot/HeadSlot null en
        // el resto de este metodo). NeckSlot/BackSlot ya estan en scope (EquippedAccessories) -
        // BackSlot en concreto es EXACTAMENTE "player.back" real (el canal Back NORMAL, ya
        // reclasificado por EquipmentAppearanceResolver - Backpack/Tail vacian BackSlot a null,
        // igual que en el juego real solo el canal Back normal alimenta esta comprobacion).
        bool frontHidden = accessories?.FrontSlot is int frontIdForCheck
            && PlayerBodyDrawTables.FrontDontDrawIfWearingScarfOrCape(frontIdForCheck)
            && ((accessories?.NeckSlot is int neckIdForCheck && PlayerBodyDrawTables.NeckIsAScarf(neckIdForCheck))
                || (accessories?.BackSlot is int backIdForCheck && PlayerBodyDrawTables.BackIsACape(backIdForCheck)));
        void DrawFrontHalf(bool leftHalf)
        {
            if (frontHidden || accessories?.FrontFile is not { } frontFile) return;
            Composite(canvas, MaskHalf(LoadStripFrameAbsolute(frontFile, legAnimationFrame), leftHalf), accessories?.FrontDye);
        }

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

        // Ver el comentario real de WalkArmColumn: solo el brazo cambia de celda durante el
        // ciclo de andar (legAnimationFrame 0 = reposo, celdas FrontArm/BackArm de siempre, sin
        // cambios de pixel para ningun llamador existente - compatibilidad byte a byte).
        (int Col, int Row) frontArmCell = FrontArm;
        (int Col, int Row) backArmCell = BackArm;
        if (legAnimationFrame != 0 && WalkArmColumn.TryGetValue(legAnimationFrame, out int walkArmCol))
        {
            frontArmCell = (walkArmCol, 1);
            backArmCell = (walkArmCol, 3);
        }

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

        // Paso 1a [08_Backpacks/08_1_Tails]: GapAnalysis Encargo A (25-sep-2026) - Terraria real
        // clasifica backSlot en 3 canales posibles (Player.cs:37169-37184,
        // UpdateVisibleAccessory): Backpack/Tail si el id cae en
        // ArmorIDs.Back.Sets.DrawInBackpackLayer/DrawInTailLayer (ArmorIDs.cs:1717/1719, tabla
        // real transcrita en BackAccessoryLayerTable), Back normal si no -
        // EquipmentAppearanceResolver.ResolveAccessories ya hace esa reclasificacion sobre el
        // sprite YA resuelto. Los 3 canales comparten la MISMA textura real TextureAssets.AccBack
        // (PlayerDrawLayers.cs:484 Backpack, :584 Tail, ya portado para BackAcc) - misma tira
        // vertical 40x(56*N), DrawAccessory reutilizado sin logica nueva. Orden real
        // (LegacyPlayerRenderer.cs:178/180/182/184/185): Backpacks -> Tails -> Wings -> BackHair
        // -> BackAcc - por eso van ANTES del pelo trasero.
        DrawAccessory(accessories?.BackpackFile, accessories?.BackpackDye);
        DrawAccessory(accessories?.TailFile, accessories?.TailDye);

        // Paso 1a-2 [09_Wings]: Wings Encargo1 (25-sep-2026) - capa BASE de alas, un unico
        // fotograma fijo (frame 0/"reposo"), sin animacion/particulas/glow (ver el ALCANCE
        // DELIBERADO real completo en el comentario de clase de WingDrawTable). Posicion real
        // ENTRE Tails y BackHair (LegacyPlayerRenderer.cs:178-184 real:
        // DrawPlayer_08_Backpacks -> _08_1_Tails -> _09_Wings -> _01_BackHair -> _10_BackAcc),
        // NO junto al resto de accesorios de tira simple (DrawAccessory) - LoadWingFrame tiene la
        // cita real completa de la formula de posicion/recorte (PlayerDrawLayers.cs:655-1105).
        // wingId=0 (o cualquier id de Calamity, WingSlot siempre null ahi) usa la posicion
        // GENERICA de WingDrawTable, fiel-por-defecto, mismo criterio ya establecido para
        // BodySlot/LegsSlot/etc.
        if (accessories?.WingFile is { } wingFile)
            Composite(canvas, LoadWingFrame(wingFile, accessories?.WingSlot ?? 0), accessories?.WingDye);

        // DrawPlayer_01_BackHair real: la capa TRASERA de un peinado largo se dibuja la
        // PRIMERISIMA de todas (antes incluso de piernas/torso), para que el resto del cuerpo
        // la tape por delante de forma natural al componer encima.
        if (!hideHair && backHairDraw)
            Composite(canvas, hatHair ? LoadHairAlt(hairStyle) : LoadHair(hairStyle), colors.Hair);

        // Paso 1b [10_BackAcc]: capa/mochila trasera - PlayerDrawLayers.cs real, BackAcc va justo
        // despues de HairBack y antes de la piel (Wings ya portado arriba, Wings Encargo1; Tails/
        // Backpack ya portados arriba, GapAnalysis Encargo A; HeadBack justo debajo, GapAnalysis
        // Encargo B; Balloons justo debajo de HeadBack, GapAnalysis Encargo C).
        DrawAccessory(accessories?.BackFile, accessories?.BackDye);

        // Paso 1c [11_BackHead]: GapAnalysis Encargo B (25-sep-2026) - version "de espaldas" del
        // casco actual, DERIVADA del propio HeadSlot (ver el comentario real de
        // EquipmentAppearanceResolver.ResolveHeadBack) - no es un slot/objeto independiente.
        // Orden real (LegacyPlayerRenderer.cs real, ~linea 186): Backpacks -> Tails -> Wings ->
        // BackHair -> BackAcc -> BackHead -> Balloons - justo despues de BackAcc, antes de
        // Balloons (GapAnalysis Encargo C, Paso 1d justo debajo). Sin recortar (frame0 40x56,
        // misma convencion que HeadFile/armor_head). GapAnalysis Encargo I (25-sep-2026):
        // HeadDye (dye[0], MISMO canal que el propio casco - HeadBack no es un item
        // independiente, es la textura "de espaldas" DERIVADA del mismo headSlot, ver el
        // comentario real de ResolveHeadBack) - antes de este encargo se dibujaba con tint null
        // porque el pipeline de dyes por canal todavia no existia, no porque la armadura no se
        // tiña nunca (ese criterio es solo para PlayerColors/Hair-Skin-etc, ver el comentario de
        // cabecera de la clase).
        if (armor.HeadBackFile is { } headBackFile)
            Composite(canvas, LoadFrame0Absolute(headBackFile), armor.HeadDye);

        // Paso 1d [11_Balloons]: GapAnalysis Encargo C (25-sep-2026) - accessories?.BalloonFile,
        // canal NORMAL de item.balloonSlot (Player.cs:37232-37241, ver
        // BalloonAccessoryLayerTable/EquipmentAppearanceResolver.ResolveAccessories). Orden real
        // (LegacyPlayerRenderer.cs real, linea 188): "...BackHead(); ...; Balloons();" - justo
        // despues de BackHead, ANTES de la piel. A diferencia del resto de accesorios (tira
        // 40x(56*N) alineada al lienzo, DrawAccessory), AccBalloon NO sigue esa convencion para
        // este canal - LoadBalloonFrame tiene la cita real completa de la formula de posicion.
        if (accessories?.BalloonFile is { } balloonFile)
            Composite(canvas, LoadBalloonFrame(balloonFile), accessories?.BalloonDye);

        // Paso 2-3 [12_Skin_Composite]: piel del torso y de las piernas, cada una solo si el
        // bodySlot/legSlot real puesto no la oculta (hidesTopSkin/hidesBottomSkin).
        // ParidadPersonaje Fase5: FloatingTube fila 0, DENTRO del MISMO gate "!hidesTopSkin" que
        // la piel del torso - fiel a PlayerDrawLayers.cs:2276-2300 real (ver DrawFloatingTube).
        if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "torsoskin", torsoCell), colors.Skin);
        if (!hidesTopSkin) DrawFloatingTube(0);
        if (!hidesBottomSkin) Composite(canvas, LoadStripFrame(variant, "legskin", legAnimationFrame), colors.Skin);

        // Paso 4 [12_SkinComposite_BackArmShirt]: brazo TRASERO. GapAnalysis Encargo C
        // (25-sep-2026): accessories?.BalloonFrontFile - canal FRONT de item.balloonSlot, citado
        // LITERALMENTE en este mismo metodo real (DrawPlayer_12_SkinComposite_BackArmShirt,
        // PlayerDrawLayers.cs:1364/1402: "DrawPlayer_12_1_BalloonFronts(ref drawinfo);") - por
        // eso va aqui y no junto a BalloonFile normal (Paso 1d). En la rama hasBody (linea 1364
        // real) va justo despues del hombro trasero y ANTES del brazo trasero; en la rama sin
        // cuerpo (linea 1400-1403 real) va entre la camiseta interior y la exterior del brazo.
        // AccBalloon_18 (RoyalScepter, el UNICO balloonFront real - ver BalloonAccessoryLayerTable)
        // SI sigue la convencion "tira alineada al lienzo" (UsesTorsoFraming=true,
        // PlayerDrawLayers.cs:1114-1120 - misma formula que DrawAccessory ya usa para el resto de
        // tipos, confirmado ademas con el sprite real ya extraido: acc_balloon/18.png mide 40x1120,
        // 20 filas, la MISMA convencion que Waist/Neck/HandOn/HandOff/Back/Face/Shoe) - reutiliza
        // DrawAccessory tal cual, sin logica nueva.
        if (hasBody)
        {
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", backArmCell), colors.Skin);
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "hands", backArmCell), colors.Skin);
            if (armor.BodyFile is { } bodyBackShoulderArmor) Composite(canvas, LoadArmorCell(bodyBackShoulderArmor, backShoulderCell), armor.BodyDye);
            DrawAccessory(accessories?.BalloonFrontFile, accessories?.BalloonFrontDye);
            if (armor.BodyFile is { } bodyBackArmArmor) Composite(canvas, LoadArmorCell(bodyBackArmArmor, backArmCell), armor.BodyDye);
        }
        else
        {
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", backArmCell), colors.Skin);
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "hands", backArmCell), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", backArmCell), colors.Under);
            DrawAccessory(accessories?.BalloonFrontFile, accessories?.BalloonFrontDye);
            Composite(canvas, LoadBodyCell(variant, "armshirt", backArmCell), colors.Shirt);
        }
        // GapAnalysis Encargo J: Coat, SIEMPRE despues del bloque hasBody/else de arriba (ver el
        // comentario real completo de DrawCoat) - orden real (PlayerDrawLayers.cs:1406-1420):
        // hombro trasero primero, brazo trasero despues.
        DrawCoat(backShoulderCell);
        DrawCoat(backArmCell);

        // Paso 5 [13_Leggings/14_Shoes]: perneras + GapAnalysis Encargo D (25-sep-2026) - el
        // accesorio REAL de zapatos (shoeSlot, canal COMPLETAMENTE DISTINTO de los zapatos BASE
        // de la piel/pantalon ya dibujados en el bloque de abajo). Orden real
        // (LegacyPlayerRenderer.cs:195-204): "if (wearsRobe && body != 166) { Shoes; Leggings; }
        // else { Leggings; Shoes; }" - wearsRobe ya calculado arriba (SetMatchBodyToLegs).
        // DrawAccessory (misma tira vertical 40x(56*N), fila = legAnimationFrame) es fiel a
        // DrawPlayer_14_Shoes real, que usa `drawinfo.drawPlayer.legFrame` como rectangulo de
        // origen - NO bodyFrame (confirmado en PlayerDrawLayers.cs:1758-1777, la formula de
        // posicion es identica a la de las perneras, LoadStripFrame_Legs mas arriba).
        //
        // Regla de sexo real (Player.cs:37193-37200, ArmorIDs.Shoe.Sets.MaleToFemaleID): si el
        // personaje es FEMENINO y el shoeSlot real tiene entrada en esa tabla (unico caso real:
        // 25 GlassSlipperMale -> 26 GlassSlipperFemale), se sustituye el sprite - mismo patron
        // ya establecido arriba para headIdAfterSetMatch/headFileToUse.
        string? shoesFileToUse = accessories?.ShoesFile;
        if (!male && accessories?.ShoesSlot is int shoesId && PlayerBodyDrawTables.ShoeMaleToFemaleID(shoesId) is int femaleShoesId)
        {
            string altShoesPath = VanillaPath("acc_shoes", femaleShoesId);
            if (File.Exists(altShoesPath)) shoesFileToUse = altShoesPath;
        }
        void DrawShoesAccessory() => DrawAccessory(shoesFileToUse, accessories?.ShoesDye);

        bool legsChangedBySetMatch = legsId != originalLegsId;
        string? legsFileToUse = legsChangedBySetMatch ? VanillaPathIfExists("armor_legs", legsId) : armor.LegsFile;
        if (wearsRobe) DrawShoesAccessory();
        if (legsId > 0 && legsFileToUse != null)
        {
            // GapAnalysis Encargo I (25-sep-2026): LegsDye (dye[2]) - si SetMatch sustituyo el
            // legSlot por el de un set distinto (wearsRobe/legsChangedBySetMatch), el dye SIGUE
            // siendo el del propio personaje (dye[2] real, no cambia con SetMatch, mismo criterio
            // que headIdAfterSetMatch/headFileToUse arriba - solo cambia el SPRITE, nunca el dye).
            Composite(canvas, LoadStripFrameAbsolute(legsFileToUse, legAnimationFrame), armor.LegsDye);
        }
        else
        {
            Composite(canvas, LoadStripFrame(variant, "pants", legAnimationFrame), colors.Pants);
            Composite(canvas, LoadStripFrame(variant, "shoes", legAnimationFrame), colors.Shoes);
        }
        if (!wearsRobe) DrawShoesAccessory();

        // Paso 6 [15_SkinLongCoat]: el faldon del vestido/abrigo (pieza 14) - solo variantes
        // 3/7/8, y solo sin armadura/vanidad de cuerpo puesta.
        if (!hasBody && skinVariant is 3 or 7 or 8)
            Composite(canvas, LoadFrame0(variant, "extra"), colors.Shirt);

        // Paso 7 [16_ArmorLongCoat]: el faldon largo de la ARMADURA (GetMatchingBodyExtension),
        // capa aparte de las perneras del paso 5 - se dibuja SIEMPRE que aplique, encima.
        if (bodyExtension is int extId)
        {
            string? extPath = VanillaPathIfExists("armor_legs", extId);
            // GapAnalysis Encargo I (25-sep-2026): BodyDye - el faldon largo es una EXTENSION de
            // la armadura de CUERPO (GetMatchingBodyExtension usa el bodyId, ver el comentario
            // real de arriba), se tiñe con el mismo dye[1] que el resto del torso/hombros/brazos.
            if (extPath != null) Composite(canvas, LoadStripFrameAbsolute(extPath, legAnimationFrame), armor.BodyDye);
        }

        // Paso 8 [17_TorsoComposite]: con armadura/vanidad de cuerpo puesta, el juego real NO
        // dibuja la ropa base (bug #1 del caso "Eldelgas") - solo la armadura, en el torso.
        if (hasBody)
        {
            if (armor.BodyFile is { } bodyTorso) Composite(canvas, LoadArmorCell(bodyTorso, torsoCell), armor.BodyDye);
        }
        else
        {
            Composite(canvas, LoadBodyCell(variant, "undershirt", backShoulderCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "shirt", backShoulderCell), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "undershirt", torsoCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "shirt", torsoCell), colors.Shirt);
        }
        // GapAnalysis Encargo J: Coat en el torso, SIEMPRE despues (PlayerDrawLayers.cs:2029-2036).
        DrawCoat(torsoCell);
        // ParidadPersonaje Fase5: FloatingTube fila 1, SIEMPRE despues (sin gate de
        // hidesTopSkin/hasBody) - fiel a PlayerDrawLayers.cs:3352-3358 real (ver DrawFloatingTube).
        DrawFloatingTube(1);

        // Paso 8b [18/19/20_OffhandAcc/WaistAcc/NeckAcc]: los tres accesorios de torso que van
        // ANTES de la cabeza en el orden real (PlayerDrawLayers.cs, ids de capa 18/19/20 - HandOff
        // primero, Waist, Neck ultimo de los tres).
        DrawHandAccessory(accessories?.HandOffFile, backArmCell, accessories?.HandOffDye);
        DrawAccessory(accessories?.WaistFile, accessories?.WaistDye);
        DrawAccessory(accessories?.NeckFile, accessories?.NeckDye);

        // Paso 9 [21_Head]: cabeza/ojos/pelo/casco. Orden real: casco ANTES que el pelo cuando
        // el casco es "fullHair" (:2143-2161, invertido respecto a la version anterior de este
        // renderer); en cualquier otro caso (hatHair o sin casco) el pelo va primero, como ya
        // hacia el renderer antes de esta pasada.
        //
        // ParidadPersonaje Fase3 - BugG (26-sep-2026): FaceHead SUSTITUYE la piel base de la
        // cabeza, no se superpone a ella - fiel a PlayerDrawLayers.cs:2574-2640 real
        // (DrawPlayer_21_Head_TheFace), un if/else-if/else MUTUAMENTE EXCLUYENTE entre 3 ramas
        // (mountHandlesHeadDraw queda fuera de alcance, un doll sin montura nunca toma esa rama):
        // con faceHead>0 (rama real 2592-2614, "else if (!flag && drawinfo.drawPlayer.faceHead >
        // 0 ...)") se dibuja UNICAMENTE TextureAssets.AccFace[faceHead] - Players[skinVar,0/1/2]
        // (piel/ojos blancos/ojos) y Extra[67] (Yoraiz0rDarkness, ambos dentro de la rama "else if
        // (!invis && !flag)" real, 2615-2639) NO se dibujan en absoluto en esa rama. Arreglo real
        // del hueco que este mismo comentario documentaba hasta hoy ("FaceHead se compone ENCIMA
        // de la piel en vez de reemplazarla") - ver
        // PlayerPreviewRendererAccessoriesTests.FaceHeadReal_SustituyeLaPielBase_NoLaSuperpone.
        if (accessories?.FaceHeadFile is { } faceHeadFile)
        {
            DrawAccessory(faceHeadFile, accessories?.FaceHeadDye);
        }
        else
        {
            Composite(canvas, LoadFrame0("body0", "head"), colors.Skin);
            Composite(canvas, LoadFrame0("body0", "eyewhites"), null); // ya blanco en el sprite real
            Composite(canvas, LoadFrame0("body0", "eyes"), colors.Eyes);

            // GapAnalysis Encargo J (25-sep-2026): Yoraiz0r Darkness (item.type==3581), justo
            // despues de la piel base de la cabeza - PlayerDrawLayers.cs:2626-2632 real, DENTRO de
            // la MISMA rama sin faceHead (confirmado arriba: con faceHead>0 nunca se llega aqui,
            // exactamente como en el juego real): "drawData = new DrawData(TextureAssets.
            // Extra[67].Value, ..., drawinfo.colorHead, ...); drawData.shader =
            // drawinfo.skinDyePacked;" - tenido con el MISMO color que la piel base (colorHead,
            // aqui colors.Skin), NO con un dye de accesorio propio (ver el comentario real
            // completo de EquippedAccessories - por eso no hay un campo "Yoraiz0rDarknessDye"
            // separado). Frame0 unico (sin animar), misma convencion que HeadBackFile/BeardFile
            // mas abajo.
            if (accessories?.Yoraiz0rDarknessFile is { } yoraiz0rDarknessFile)
                Composite(canvas, LoadFrame0Absolute(yoraiz0rDarknessFile), colors.Skin);
        }

        // Face bajo el pelo: excepcion real ArmorIDs.Face.Sets.DrawInFaceUnderHairLayer (unico
        // caso real, faceSlot=5/Blindfold) - PlayerDrawLayers.cs real,
        // DrawPlayer_21_Head_TheFace lineas 2598-2613 (con faceHead puesto a la vez) / 2633-2638
        // (sin faceHead) - se dibuja aqui, ANTES del pelo/casco, en vez de en su posicion
        // habitual (Paso 9b, junto a FaceMask/FaceFlower/Shield). FaceSlot es el indice REAL
        // vanilla ya resuelto por ResolveAccessories (null para Calamity/vacio, nunca entra aqui).
        bool faceUnderHair = accessories?.FaceSlot is int faceSlotForHair && FaceAccessoryLayerTable.IsUnderHairLayer(faceSlotForHair);
        if (faceUnderHair) DrawAccessory(accessories?.FaceFile, accessories?.FaceDye);

        // FaceMask bajo el casco: excepcion real ArmorIDs.Head.Sets.DrawFaceMaskUnderHeadLayer -
        // a diferencia de la de arriba, esta tabla esta indexada por HEADSLOT, no por faceSlot
        // (ver PlayerBodyDrawTables.DrawFaceMaskUnderHeadLayer) - PlayerDrawLayers.cs real,
        // DrawPlayer_21_Head lineas 2126-2142 (flag5), justo antes del pelo/casco. Cuando esta
        // tabla es true para el headSlot puesto, PreventFaceMaskDraw NO se consulta (confirmado
        // en el propio flag5 real - la condicion solo mira DrawFaceMaskUnderHeadLayer).
        bool faceMaskUnderHead = PlayerBodyDrawTables.DrawFaceMaskUnderHeadLayer(headId);
        if (faceMaskUnderHead) DrawAccessory(accessories?.FaceMaskFile, accessories?.FaceMaskDye);

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
            // GapAnalysis Encargo I (25-sep-2026): HeadDye (dye[0]) - si SetMatch sustituyo el
            // headSlot (headFileToUse != armor.HeadFile), el dye SIGUE siendo el del propio
            // personaje (mismo criterio que LegsDye arriba: solo cambia el SPRITE, nunca el dye).
            if (headFileToUse is { } headFile) Composite(canvas, LoadFrame0Absolute(headFile), armor.HeadDye);
        }

        if (fullHair) { DrawHelmet(); DrawHair(); }
        else { DrawHair(); DrawHelmet(); }

        // Paso 9a2 [21_Beard]: GapAnalysis Encargo G (25-sep-2026) - accessories?.BeardFile,
        // canal beardSlot (Player.cs:37243-37246: "if (item.beardSlot > 0) beard =
        // item.beardSlot;"). Se dibuja DENTRO de la capa Head, DESPUES de casco/pelo
        // (PlayerDrawLayers.cs:2428-2444 real: "bool flag7 = drawinfo.drawPlayer.head < 0 ||
        // !ArmorIDs.Head.Sets.PreventBeardDraw[drawinfo.drawPlayer.head]; ... if
        // ((drawinfo.drawPlayer.beard > 0) & flag7) { ... }") - mismo orden real que
        // DrawHelmet()/DrawHair() de arriba, ANTES de FaceAcc. PreventBeardDraw(headSlot)
        // oculta la barba para 47 cascos reales (mascaras/cascos completos, ver
        // PlayerBodyDrawTables.PreventBeardDraw) - headSlot desconocido (pieza de Calamity o
        // sin casco puesto, armor.HeadSlot==null) NUNCA la oculta, mismo criterio real
        // "head<0 siempre deja pasar la barba" del motor.
        // CORREGIDO 26-sep-2026 (hallazgo ParidadPersonaje-Fase2, arquitecto-keep a396f91e):
        // Beard usa el canal `bodyFrame` en vanilla (PlayerDrawLayers.cs:2441, "drawData = new
        // DrawData(..., drawinfo.drawPlayer.bodyFrame, ...)") - el MISMO canal que Neck/Waist/
        // Face, que este renderer ya anima con `DrawAccessory(file, tint)` (closure sobre
        // legAnimationFrame, ver la funcion local de arriba). `acc_beard/*.png` mide 40x1120 (20
        // filas reales ya extraidas) - ya NO es un sprite estatico de un solo frame, asi que se
        // sustituye la llamada directa a LoadFrame0Absolute (que congelaba siempre la fila 0) por
        // el mismo DrawAccessory ya usado para sus hermanos de canal, sin crear ningun metodo
        // nuevo. Antes de esta correccion la barba nunca se movia con el ciclo de andar del doll.
        // Tinte real (PlayerDrawLayers.cs:2436-2440, cita completa): "Color color6 =
        // drawinfo.colorArmorHead; if (ArmorIDs.Beard.Sets.UseHairColor[drawinfo.drawPlayer.
        // beard]) { color6 = drawinfo.colorHair; }" - los 3 "Wilson beards" (textura gris, sin
        // color propio) se tiñen con el COLOR DE PELO REAL del personaje (colors.Hair), NO con
        // un dye de armadura ni un color fijo; GingerBeard (textura ya naranja de por si) usa
        // colorArmorHead, que en un doll de reposo sin buffs/dyes equivale a blanco puro (sin
        // tinte añadido) - ver PlayerBodyDrawTables.BeardUsesHairColor para la cita completa de
        // por que se decidio aplicar el color de pelo real aqui mismo (encaja en el canal
        // Beard en si, no en el pipeline de dyes por canal completo del Encargo I aparte).
        if (accessories?.BeardFile is { } beardFile)
        {
            bool preventBeardDraw = armor.HeadSlot is int beardHeadSlot && PlayerBodyDrawTables.PreventBeardDraw(beardHeadSlot);
            if (!preventBeardDraw)
            {
                Tint? beardTint = accessories.BeardSlot is int beardId && PlayerBodyDrawTables.BeardUsesHairColor(beardId)
                    ? colors.Hair
                    : null;
                DrawAccessory(beardFile, beardTint);
            }
        }

        // Paso 9b [22_FaceAcc]: Face/FaceMask/FaceFlower en su posicion normal, salvo las
        // excepciones ya resueltas arriba (Face bajo el pelo, FaceMask bajo el casco) - GapAnalysis
        // Encargo F (25-sep-2026), PlayerDrawLayers.cs real DrawPlayer_22_FaceAcc (linea 2803 en
        // adelante). FaceMask normal se suprime del todo si ArmorIDs.Head.Sets.
        // PreventFaceMaskDraw[headSlot] (y no se dibujo ya bajo el casco); FaceFlower se suprime
        // del todo si PreventFaceFlowerDraw[headSlot] - ninguna de las 2 tiene una posicion
        // alternativa para ese caso, simplemente no se dibuja en ningun sitio (fiel al juego real).
        if (!faceUnderHair) DrawAccessory(accessories?.FaceFile, accessories?.FaceDye);
        if (!faceMaskUnderHead && !PlayerBodyDrawTables.PreventFaceMaskDraw(headId)) DrawAccessory(accessories?.FaceMaskFile, accessories?.FaceMaskDye);
        if (!PlayerBodyDrawTables.PreventFaceFlowerDraw(headId)) DrawAccessory(accessories?.FaceFlowerFile, accessories?.FaceFlowerDye);

        // GapAnalysis Encargo J (25-sep-2026): Unicorn Horn/Angel Halo, justo DESPUES de
        // FaceFlower - PlayerDrawLayers.cs:2860-2884 real, MISMA seccion/convencion que
        // Face/FaceMask/FaceFlower (tira 40x(56*N), bodyFrame como rectangulo de origen) - ver el
        // comentario real completo de EquippedAccessories.
        DrawAccessory(accessories?.UnicornHornFile, accessories?.UnicornHornDye);
        DrawAccessory(accessories?.AngelHaloFile, accessories?.AngelHaloDye);

        // Paso 9b2 [32_FrontAcc_BackPart]: GapAnalysis Encargo E (25-sep-2026) - mitad DERECHA
        // real de Front, posicion FIJA (LegacyPlayerRenderer.cs real: justo despues de FaceAcc/
        // MountFront/Pulley/JimsDroneRadio -que este doll no modela, sin diferencia visible en un
        // doll estatico sin montura- y ANTES de Shield) - a diferencia de FrontPart (mas abajo),
        // BackPart SIEMPRE va aqui, sin condicion de posicion alternativa.
        DrawFrontHalf(leftHalf: false);

        // Paso 9c [25_Shield]: escudo, despues de FaceAcc y antes del brazo delantero (orden real
        // PlayerDrawLayers.cs) - ancho real variable, ver LoadShieldFrame.
        if (accessories?.ShieldFile is { } shieldFile)
            Composite(canvas, LoadShieldFrame(shieldFile, legAnimationFrame), accessories?.ShieldDye);

        // Paso 10 [28_ArmOverItemComposite]: brazo DELANTERO, encima de todo lo anterior.
        // Orden real: BRAZO primero, HOMBRO despues (PlayerDrawSet.cs: compShoulderOverFrontArm
        // = true, el bucle real dibuja primero i==num3/brazo y luego i==num2/hombro) - invertido
        // respecto a la version anterior de este renderer.
        if (hasBody)
        {
            if (missingArm && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", frontArmCell), colors.Skin);
            // 10b usa la pieza 9 (ArmHand), NO la 5 (Hands) - PlayerDrawLayers.cs:3735.
            if (missingHand && !hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armhand", frontArmCell), colors.Skin);
            if (armor.BodyFile is { } bodyFrontArmArmor) Composite(canvas, LoadArmorCell(bodyFrontArmArmor, frontArmCell), armor.BodyDye);
            if (armor.BodyFile is { } bodyFrontShoulderArmor) Composite(canvas, LoadArmorCell(bodyFrontShoulderArmor, frontShoulderCell), armor.BodyDye);
        }
        else
        {
            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", frontArmCell), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", frontArmCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "armshirt", frontArmCell), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "shirt", frontArmCell), colors.Shirt);

            if (!hidesTopSkin) Composite(canvas, LoadBodyCell(variant, "armskin", frontShoulderCell), colors.Skin);
            Composite(canvas, LoadBodyCell(variant, "armundershirt", frontShoulderCell), colors.Under);
            Composite(canvas, LoadBodyCell(variant, "armshirt", frontShoulderCell), colors.Shirt);
            Composite(canvas, LoadBodyCell(variant, "shirt", frontShoulderCell), colors.Shirt);
        }
        // GapAnalysis Encargo J: Coat, SIEMPRE despues (PlayerDrawLayers.cs:3828-3846) - MISMO
        // orden brazo/hombro real ya establecido arriba para armor.BodyFile (num2/num3, brazo
        // primero).
        DrawCoat(frontArmCell);
        DrawCoat(frontShoulderCell);

        // Paso 10b [29_OnhandAcc]: accesorio "en mano" (guantes/garras puestos como accesorio,
        // no como arma), tras el brazo/hombro delantero.
        DrawHandAccessory(accessories?.HandOnFile, frontArmCell, accessories?.HandOnDye);

        // Paso 10c [32_FrontAcc_FrontPart]: GapAnalysis Encargo E (25-sep-2026) - mitad
        // IZQUIERDA real de Front, la ULTIMA capa real de accesorio de este renderer (orden real
        // LegacyPlayerRenderer.cs: "...OnhandAcc(); BladedGlove(); if
        // (!drawFrontAccInNeckAccLayer) FrontAcc_FrontPart(); extra_TorsoMinus(); ..." -
        // BladedGlove no se modela, sin diferencia visible). `drawFrontAccInNeckAccLayer`
        // (PlayerDrawSet.cs:1798-1809 real) solo se activa si
        // ArmorIDs.Front.Sets.DrawsInNeckLayerRegardlessOfPlayerFrame[front] o
        // (bodyFrame.Y/bodyFrame.Height==5 && DrawsInNeckLayer[front]) - investigado a fondo
        // contra el decompilado real: los UNICOS indices reales con alguna de esas 2 tablas a
        // true son 6 (TaxCollectorsSuit) y 13 (DeadCellsBeheadedBody), y NINGUN accesorio Front
        // real (item.frontSlot) alcanza nunca esos 2 valores - solo se alcanzan via
        // ArmorIDs.Body.Sets.IncludedCapeFront/IncludeCapeFrontAndBack (Player.cs:36126-36141,
        // "front" DERIVADO de la ARMADURA DE CUERPO puesta, no de item.frontSlot) - un mecanismo
        // COMPLETAMENTE DISTINTO, fuera de alcance de este encargo ("portar frontSlot"). Para
        // TODOS los Front reales que este encargo cubre, drawFrontAccInNeckAccLayer es SIEMPRE
        // false - FrontPart va SIEMPRE en esta posicion normal, sin rama alternativa que portar.
        // DELIBERATE DIFFERENCE documentada: un futuro "Front derivado de body" tendria que
        // reconsiderar esta posicion fija para esos 2 ids concretos - no reproducible con
        // item.frontSlot solo, LIMITE REAL.
        DrawFrontHalf(leftHalf: true);

        if (mirror) FlipHorizontal(canvas);

        var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), canvas, Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    // Espejo horizontal in-place del lienzo YA compuesto (Width x Height, Bgra32) - invierte el
    // orden de los 4 bytes de cada pixel dentro de cada fila, fila a fila.
    private static void FlipHorizontal(byte[] canvas)
    {
        for (int y = 0; y < Height; y++)
        {
            int rowStart = y * Width * 4;
            for (int xLeft = 0; xLeft < Width / 2; xLeft++)
            {
                int xRight = Width - 1 - xLeft;
                int left = rowStart + xLeft * 4, right = rowStart + xRight * 4;
                for (int b = 0; b < 4; b++) (canvas[left + b], canvas[right + b]) = (canvas[right + b], canvas[left + b]);
            }
        }
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

    // Idea 10 ("vista previa animada"): gemelas de LoadFrame0/LoadFrame0Absolute, pero para una
    // fila REAL cualquiera (no siempre la (0,0)) de una tira vertical - legskin/pants/shoes
    // (extraer-sprites-jugador.js) y armor_legs (extraer-sprites-armadura-vanilla.js) se guardan
    // ahora ENTERAS (40x1120, 20 filas reales) desde esta idea, mismo criterio ya establecido
    // para las piezas "composite" (LoadBodyCell/LoadArmorCell). frameRow=0 produce el MISMO
    // recorte exacto que LoadFrame0/LoadFrame0Absolute (compatibilidad real, no aproximada -
    // el reposo no cambia ni un pixel para ningun llamador existente).
    private static byte[] LoadStripFrame(string variant, string name, int frameRow) =>
        SliceStripRow(LoadStripCached(Path.Combine(AppContext.BaseDirectory, "Assets", "player", variant, name + ".png")), frameRow);

    private static byte[] LoadStripFrameAbsolute(string absolutePath, int frameRow) =>
        SliceStripRow(LoadStripCached(absolutePath), frameRow);

    // Cache separada de LoadCached/LoadSheetCached (mismo motivo real que su comentario: los
    // tres decodifican el MISMO fichero de formas distintas - recorte fijo 40x56, rejilla
    // 360x224 entera, o aqui una tira 40x(56*N) entera de alto REAL variable, nunca fijo).
    private static byte[] LoadStripCached(string path) => Cache.GetOrAdd("strip:" + path, _ => LoadPngPixelsStripFull(path));

    // A diferencia de LoadPngPixelsSheet (que asume SIEMPRE 360x224), esta lee el ancho/alto
    // REALES del PNG decodificado - el alto varia de verdad entre piezas (20 filas=1120px para
    // las del cuerpo base, pero un armor_legs concreto podria tener menos si su .xnb real
    // tuviera menos filas; SliceStripRow acota el indice pedido al numero real de filas
    // disponibles, nunca lee fuera del array).
    private static byte[] LoadPngPixelsStripFull(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[frame.PixelWidth * frame.PixelHeight * 4];
        converted.CopyPixels(pixels, frame.PixelWidth * 4, 0);
        return pixels;
    }

    // Recorta la fila real "frameRow" (0-based, cada una de Height=56px) de una tira YA
    // decodificada entera - acota al numero real de filas disponibles (nunca lee fuera del
    // array, ver el comentario de LoadPngPixelsStripFull).
    private static byte[] SliceStripRow(byte[] stripPixels, int frameRow)
    {
        var outPixels = new byte[Width * Height * 4];
        int totalRows = stripPixels.Length / (Width * Height * 4);
        if (totalRows <= 0) return outPixels; // tira mas pequeña que un solo fotograma - vacia, nunca revienta
        int row = Math.Clamp(frameRow, 0, totalRows - 1);
        int startY = row * Height;
        for (int y = 0; y < Height; y++)
        {
            int srcOffset = (startY + y) * Width * 4;
            int dstOffset = y * Width * 4;
            Array.Copy(stripPixels, srcOffset, outPixels, dstOffset, Width * 4);
        }
        return outPixels;
    }

    // GapAnalysis Encargo E (25-sep-2026): recorta una MITAD (izquierda o derecha) de un frame
    // YA resuelto por SliceStripRow/LoadStripFrameAbsolute (siempre Width=40 fijo, a diferencia
    // de Shield) - portado de DrawPlayer_32_FrontAcc_FrontPart/_BackPart
    // (PlayerDrawLayers.cs:3908-3993), ver el comentario real completo de DrawFrontHalf en
    // Render(). num=Width/2=20 (entero, igual que el juego real "bodyFrame.Width -= num" sobre
    // un ancho par de 40) - ambas mitades miden exactamente 20px, sin resto que repartir.
    private static byte[] MaskHalf(byte[] framePixels, bool leftHalf)
    {
        var outPixels = new byte[Width * Height * 4];
        int num = Width / 2;
        int startX = leftHalf ? 0 : num;
        int halfWidth = Width - num;
        for (int y = 0; y < Height; y++)
        {
            int rowStart = y * Width * 4;
            Array.Copy(framePixels, rowStart + startX * 4, outPixels, rowStart + startX * 4, halfWidth * 4);
        }
        return outPixels;
    }

    // PortSeleccion Encargo2 (25-sep-2026): gemelo de LoadStripFrameAbsolute/SliceStripRow, SOLO
    // para Shield - el unico de los 7 tipos de accesorio cuyo ancho real puede no ser 40px
    // (PlayerDrawLayers.cs:4958-4961, "bodyFrame.Width = shield.Value.Width"; confirmado en los
    // sprites extraidos de este PC: la mayoria de escudos vanilla miden 40px pero varios miden
    // 42/44px). SliceStripRow asume ancho fijo Width(40) para toda la tira - reutilizarlo aqui
    // desalinearia cada fila para un escudo con otro ancho. Aproximacion documentada: se centra
    // horizontalmente sobre el lienzo de 40px (el juego real desplaza bodyVect.X para mantenerlo
    // centrado sobre el cuerpo, PlayerDrawLayers.cs:4961) - no es una reimplementacion completa
    // del vector de posicionado real, pero es fiel para el caso comun y nunca lee fuera de los
    // arrays reales (recorta si el escudo es mas ancho que el lienzo, rellena transparente si es
    // mas estrecho).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Pixels, int RealWidth)> ShieldCache = new();

    private static byte[] LoadShieldFrame(string absolutePath, int frameRow)
    {
        var (pixels, realWidth) = ShieldCache.GetOrAdd(absolutePath, LoadShieldStrip);
        return SliceShieldRow(pixels, realWidth, frameRow);
    }

    private static (byte[] Pixels, int RealWidth) LoadShieldStrip(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[frame.PixelWidth * frame.PixelHeight * 4];
        converted.CopyPixels(pixels, frame.PixelWidth * 4, 0);
        return (pixels, frame.PixelWidth);
    }

    private static byte[] SliceShieldRow(byte[] stripPixels, int realWidth, int frameRow)
    {
        var outPixels = new byte[Width * Height * 4];
        if (realWidth <= 0) return outPixels;
        int totalRows = stripPixels.Length / (realWidth * Height * 4);
        if (totalRows <= 0) return outPixels;
        int row = Math.Clamp(frameRow, 0, totalRows - 1);
        int startY = row * Height;
        int offsetX = (Width - realWidth) / 2; // negativo si el escudo es mas ancho que el lienzo
        for (int y = 0; y < Height; y++)
        {
            int srcRowStart = (startY + y) * realWidth * 4;
            for (int x = 0; x < realWidth; x++)
            {
                int dstX = x + offsetX;
                if (dstX < 0 || dstX >= Width) continue;
                Array.Copy(stripPixels, srcRowStart + x * 4, outPixels, (y * Width + dstX) * 4, 4);
            }
        }
        return outPixels;
    }

    // GapAnalysis Encargo C (25-sep-2026): AccBalloon (canal NORMAL, no BalloonFront - ver el
    // comentario del Paso 1d en Render()) NO sigue la convencion "tira 40x(56*N) alineada al
    // lienzo" del resto de accesorios (DrawAccessory/LoadStripFrameAbsolute) - confirmado con los
    // sprites reales ya extraidos (scripts/extraer-sprites-accesorios-vanilla.js): salvo el 18
    // (RoyalScepter, canal BalloonFront, tira 40x1120 normal), los otros 13 sprites reales miden
    // 52x224 (4 fotogramas propios de 56px de alto cada uno - animacion temporal real del globo
    // "flotando", DateTime.Now.Millisecond%800/200 en PlayerDrawLayers.cs - fotograma 0 = reposo,
    // igual que el resto de capas fijas de este renderer, ninguna otra pieza anima por tiempo).
    //
    // Terraria real (PlayerDrawLayers.cs:1121-1137, DrawPlayer_11_Balloons rama else - la MISMA
    // formula exacta que DrawPlayer_12_1_BalloonFronts usa en su propia rama else, salvo el canal
    // de tinte) posiciona el globo con un ancla PROPIA, no con bodyFrame:
    //
    //   vector = Main.OffsetsPlayerOffhand[bodyFrame.Y / 56];              // Main.cs:483, fila 0 = (14,20)
    //   if (direction != 1) vector.X = width - vector.X;                   // direction=1 en este renderer (ver FlipHorizontal)
    //   if (gravDir != 1f) vector.Y -= height;                             // gravDir=1 en este renderer (sin gravedad)
    //   vector2 = new Vector2(0,8) + new Vector2(0,6);                     // = (0,14), literal real
    //   vector3 = Position - screenPosition + vector + (0, height-bodyFrame.Height) + vector2;
    //   origin  = (26 + direction*4, 28 + gravDir*6);                      // = (30,34) con direction=1/gravDir=1
    //
    // "Position - screenPosition" (K) no esta modelado aqui como variable propia - se despeja
    // ALGEBRAICAMENTE de la MISMA formula base que ya usan HandOff/Neck/Back/Face (DrawAccessory,
    // ver el comentario de Render): esas capas dibujan su textura entera alineada 1:1 con el
    // lienzo (offset final 0,0) con "posicion = K + width/2 + bodyPosition, origen = bodyVect" -
    // con bodyVect=(Width/2,Height/2)=(20,28) real (PlayerDrawSet.cs:1757, "legFrame.Width*0.5,
    // legFrame.Height*0.5") y bodyPosition=Vector2.Zero real (Player.cs:37866/39203, estado de
    // reposo), despejar K da (10,10) exactamente - width/height aqui son
    // Player.defaultWidth/defaultHeight (20/42, Player.cs:1829/1831, constantes reales del
    // hitbox que este renderer no modela aparte, ver el comentario de Width/Height arriba).
    // Con K=(10,10) y bodyFrame.Y=0 (fila de reposo - el torso de este renderer NUNCA cambia de
    // fila, ver WalkArmColumn) el desplazamiento final resultante (posicion - origen) es EXACTO:
    // (24,30) - (30,34) = (-6,-4) respecto al origen (0,0) del lienzo que ya usan las 6 capas
    // alineadas - por eso hace falta un compositor con offset propio (SliceBalloonFrame0) en vez
    // de reusar Composite/LoadStripFrameAbsolute (que asumen offset 0,0 y ancho fijo 40px, ninguna
    // de las dos cosas es cierta aqui: ancho real 52px, offset real negativo).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Pixels, int RealWidth, int RealHeight)> BalloonCache = new();

    private static byte[] LoadBalloonFrame(string absolutePath)
    {
        var (pixels, w, h) = BalloonCache.GetOrAdd(absolutePath, LoadBalloonStrip);
        return SliceBalloonFrame0(pixels, w, h);
    }

    private static (byte[] Pixels, int RealWidth, int RealHeight) LoadBalloonStrip(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[frame.PixelWidth * frame.PixelHeight * 4];
        converted.CopyPixels(pixels, frame.PixelWidth * 4, 0);
        return (pixels, frame.PixelWidth, frame.PixelHeight);
    }

    // Recorta SOLO el primer fotograma real (fila 0 de 4, alto/4 - "reposo") y lo compone YA en
    // su posicion real dentro de un lienzo del mismo tamano que Width x Height (offset real fijo
    // -6,-4, ver el comentario de la clase de arriba) - a diferencia de SliceStripRow/
    // SliceShieldRow, el resultado ya sale del tamano exacto del lienzo, recortando/dejando
    // transparente lo que cae fuera (el globo real se dibuja parcialmente fuera del lienzo por
    // arriba/izquierda - fiel al juego real, que tampoco lo recorta al hitbox del jugador).
    private static byte[] SliceBalloonFrame0(byte[] stripPixels, int realWidth, int realHeight)
    {
        var outPixels = new byte[Width * Height * 4];
        if (realWidth <= 0 || realHeight <= 0) return outPixels;
        int frameHeight = realHeight / 4;
        if (frameHeight <= 0) return outPixels;
        const int offsetX = -6, offsetY = -4; // ver la derivacion real completa arriba
        for (int y = 0; y < frameHeight; y++)
        {
            int dstY = y + offsetY;
            if (dstY < 0 || dstY >= Height) continue;
            int srcRowStart = y * realWidth * 4;
            for (int x = 0; x < realWidth; x++)
            {
                int dstX = x + offsetX;
                if (dstX < 0 || dstX >= Width) continue;
                Array.Copy(stripPixels, srcRowStart + x * 4, outPixels, (dstY * Width + dstX) * 4, 4);
            }
        }
        return outPixels;
    }

    // Wings Encargo1 (25-sep-2026): gemelo de LoadBalloonFrame/SliceBalloonFrame0 (misma forma -
    // hoja REAL de tamaño variable, recorte de UN solo fotograma ya en su posicion final dentro
    // de un lienzo Width x Height), pero CADA wingId real tiene su PROPIA anchor/divisor/
    // fotograma (WingDrawTable, transcripcion+derivacion completa de
    // Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_09_Wings - ver el comentario de esa
    // clase para la cita linea a linea) - a diferencia de Balloon (offset FIJO -6,-4 para
    // cualquier globo, origin no relacionado con el tamaño real del sprite), Wings SIEMPRE centra
    // el frame recortado sobre su propio anchor (origin = mitad del ancho/alto real del
    // fotograma, igual en las 51 IDs vanilla reales) - por eso el calculo de offsetX/offsetY aqui
    // depende del ANCHO/ALTO REAL del PNG decodificado (realWidth/frameHeight), no de una
    // constante como en Balloon.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Pixels, int RealWidth, int RealHeight)> WingCache = new();

    private static byte[] LoadWingFrame(string absolutePath, int wingId)
    {
        var (pixels, w, h) = WingCache.GetOrAdd(absolutePath, LoadWingStrip);
        return SliceWingFrame(pixels, w, h, WingDrawTable.Resolve(wingId));
    }

    private static (byte[] Pixels, int RealWidth, int RealHeight) LoadWingStrip(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[frame.PixelWidth * frame.PixelHeight * 4];
        converted.CopyPixels(pixels, frame.PixelWidth * 4, 0);
        return (pixels, frame.PixelWidth, frame.PixelHeight);
    }

    // Recorta el fotograma real "frame.FrameIndex" (0-based, cada uno de alto real/frame.Divisor
    // - "reposo" para las 50 de las 51 IDs vanilla reales, salvo el 40 - ver el comentario de
    // WingDrawTable) y lo compone YA centrado sobre "frame.AnchorX/AnchorY" dentro de un lienzo
    // Width x Height - recorta/deja transparente lo que cae fuera (un ala real puede dibujarse
    // parcialmente fuera del lienzo, fiel al juego real, que tampoco la recorta al hitbox del
    // jugador).
    private static byte[] SliceWingFrame(byte[] stripPixels, int realWidth, int realHeight, WingFrame frame)
    {
        var outPixels = new byte[Width * Height * 4];
        if (realWidth <= 0 || realHeight <= 0 || frame.Divisor <= 0) return outPixels;
        int frameHeight = realHeight / frame.Divisor;
        if (frameHeight <= 0) return outPixels;
        int frameIndex = Math.Clamp(frame.FrameIndex, 0, frame.Divisor - 1);
        int srcY = frameIndex * frameHeight;
        int offsetX = frame.AnchorX - realWidth / 2;
        int offsetY = frame.AnchorY - frameHeight / 2;
        for (int y = 0; y < frameHeight; y++)
        {
            int dstY = y + offsetY;
            if (dstY < 0 || dstY >= Height) continue;
            int srcRowStart = (srcY + y) * realWidth * 4;
            for (int x = 0; x < realWidth; x++)
            {
                int dstX = x + offsetX;
                if (dstX < 0 || dstX >= Width) continue;
                Array.Copy(stripPixels, srcRowStart + x * 4, outPixels, (dstY * Width + dstX) * 4, 4);
            }
        }
        return outPixels;
    }

    // Recorta la celda real de una pieza compuesta del CUERPO (360x224) - ver el mapa de celdas
    // en el comentario de la clase.
    private static byte[] LoadBodyCell(string variant, string name, (int Col, int Row) cell) =>
        SliceCell(LoadSheetCached(Path.Combine(AppContext.BaseDirectory, "Assets", "player", variant, name + ".png")), cell);

    // Gemelo de LoadBodyCell, para una pieza de ARMADURA/vanidad puesta (ruta absoluta real ya
    // resuelta por EquipmentAppearanceResolver - vanilla o Calamity, misma convencion 360x224
    // las dos).
    private static byte[] LoadArmorCell(string absolutePath, (int Col, int Row) cell) =>
        SliceCell(LoadSheetCached(absolutePath), cell);

    // CalamityAccesorios (25-sep-2026): ver el comentario real de DrawHandAccessory (dentro de
    // Render) para la cita completa del motor real - HandOn/HandOff pueden venir como hoja
    // compuesta 360x224 (los 6 guantes reales de Calamity) O como tira simple 40x(56*N) (los 24
    // sprites vanilla ya extraidos) - se detecta por el tamaño REAL del PNG decodificado, cacheado
    // aparte (solo cabecera, no los pixeles completos) para no pagar el coste dos veces por
    // fichero.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Width, int Height)> PngDimensionsCache = new();

    private static (int Width, int Height) GetPngDimensions(string path) =>
        PngDimensionsCache.GetOrAdd(path, p =>
        {
            using var stream = File.OpenRead(p);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            return (frame.PixelWidth, frame.PixelHeight);
        });

    private static byte[] LoadHandAccessoryFrame(string absolutePath, (int Col, int Row) cell, int frameRow)
    {
        var (w, h) = GetPngDimensions(absolutePath);
        return w == SheetWidth && h == SheetHeight
            ? LoadArmorCell(absolutePath, cell)
            : LoadStripFrameAbsolute(absolutePath, frameRow);
    }

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
    // Bug real encontrado y arreglado verificando C-10b (auditoria de pulido final, sets de
    // armadura de Calamity): equipar CUALQUIER casco o grebas de Calamity reventaba el preview
    // entero con ArgumentOutOfRangeException. Causa real: el contrato documentado de la clase
    // (ver comentario de EquippedArmor mas abajo, "HeadFile/LegsFile son tiras verticales 40x56
    // -frame0-") asume una tira de animacion vertical de la que solo interesa el primer
    // fotograma - los sprites vanilla YA vienen pre-recortados a un unico fotograma en su
    // extraccion original, pero los de Calamity (scripts/extraer-sprites-armadura-calamity.js)
    // no: AerospecHeadMelee_Head.png mide de verdad 40x1120 (20 fotogramas apilados), no 40x56.
    // Esta funcion asumia ciegamente que el fichero YA medía exactamente 40x56 en vez de
    // recortar el primer fotograma de verdad - CroppedBitmap lo hace explicito, sin cambiar
    // nada para los vanilla (ya miden 40x56, el recorte es un no-op).
    private static byte[] LoadPngPixels40x56(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var frame0 = new CroppedBitmap(frame, new Int32Rect(0, 0, Math.Min(Width, frame.PixelWidth), Math.Min(Height, frame.PixelHeight)));
        var converted = new FormatConvertedBitmap(frame0, PixelFormats.Bgra32, null, 0);
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
