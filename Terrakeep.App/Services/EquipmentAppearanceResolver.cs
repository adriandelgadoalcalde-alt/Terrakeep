using System.IO;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.Services;

// Resuelve que sprites reales de armadura/vanidad hay que dibujar sobre el doll de un
// personaje - pedido explicito del usuario (3-sep-2026): "los personajes de inicio no se
// visualizan como realmente son en el juego... que muestre el personaje con la vanidad que
// tiene cada uno pero fiel al guardado igual que lo que lleva puesto de vanidad".
//
// Regla real del propio juego (Player.cs decompilado, armor[0..9]=funcional/armor[10..19]=
// vanidad - aqui PlrLoadout.Items/Social, mismos 10 slots cada uno): si el slot de VANIDAD
// tiene un objeto puesto, ese es el que se VE; si esta vacio, se ve el objeto FUNCIONAL.
// Solo cubre los 3 slots de armadura real (indices 0=cabeza/1=cuerpo/2=piernas en ambos
// arrays) - los indices 3..9 son accesorios, sin capa visual propia sobre el doll (alcance
// ya deliberado de PlayerPreviewRenderer).
//
// loadouts[0] (PrimaryLoadout, el "mirror" de lo puesto de verdad) es el que hay que pasar
// aqui, NUNCA loadouts[1..3] - esos son los 3 loadouts guardados, no necesariamente el que
// esta activo. La ARMADURA/vanidad SIEMPRE se lee de PrimaryLoadout (es "lo puesto de verdad",
// el mirror del cliente real) - el Hide[] real (ver ResolveAccessories mas abajo) es la unica
// excepcion: ese array vive en Loadouts[CurrentLoadout], nunca en PrimaryLoadout
// (PlrLoadout.CreateEmpty(isPrimary:true) fija Hide=null siempre), asi que el llamador tiene
// que combinar los dos objetos - ver CharacterListEntryViewModel.
//
// ALCANCE DELIBERADO, documentado y no oculto: Resolve() (cabeza/cuerpo/piernas, indices 0..2)
// NUNCA respeta ningun toggle de "ocultar equipo" - confirmado en Player.cs real
// (UpdateVisibleAccessories, el bucle que consulta hideVisibleAccessory[] solo itera i=3..9,
// jamas 0..2). La visibilidad de cabeza/cuerpo/piernas la gobierna un mecanismo COMPLETAMENTE
// distinto (los 3 bytes HideVisual1/HideVisual2/HideMisc en PlrCharacter) - el bit exacto que
// le corresponde a cada slot dentro de esos bytes no se investigo a fondo (no compensaba el
// riesgo de esconder o mostrar la pieza equivocada por una lectura de bit erronea). Un
// personaje que use ese toggle (distinto de Hide[], distinto de este GapAnalysis Encargo H)
// vera su pieza dibujada aunque el juego real la esconda - hueco real, ya conocido, no cubierto
// por este encargo, no un bug silencioso.
// Resultado real de ResolvePet: AnimationEntry (si PetAnimationCatalog conoce el objeto) manda
// sobre IconPath - CharacterListEntryViewModel anima con PetPreviewRenderer cuando hay
// AnimationEntry, y cae al icono estatico (IconPath) cuando no.
public sealed record PetPreview(PetAnimationEntry? AnimationEntry, string? IconPath);

// PortSeleccion Encargo1 (25-sep-2026): resultado real de ResolveAccessories - hermano de
// PlayerPreviewRenderer.EquippedArmor pero para los 7 slots de accesorio funcional/vanidad
// (indices 3..9 de PlrLoadout.Items/Social). SlotFile es la hoja real ya extraida (sin
// recortar - ver extraer-sprites-accesorios-vanilla.js), SlotSlot es el indice REAL vanilla
// (Terraria.Player.waist/neck/handon/handoff/back/shield/face, misma tabla que
// acc_{tipo}/{slot}.png) - null si esta vacio o es un objeto de Calamity (numeracion propia,
// no compartida, igual que HeadSlot/BodySlot/LegsSlot en EquippedArmor). Este encargo NO
// dibuja nada en el doll (eso es el Encargo2 de PortSeleccion) - solo resuelve que sprite real
// le corresponde a cada tipo, fiel al guardado.
// GapAnalysis Encargo A (25-sep-2026): BackpackFile/TailFile - hermanos de BackFile para los
// otros 2 canales reales en los que puede caer un backSlot (ver BackAccessoryLayerTable, tabla
// real transcrita de ArmorIDs.cs). Reusan el mismo sprite ya resuelto para "Back" (misma
// textura vanilla real AccBack, ver el comentario de PlayerPreviewRenderer.Render) - nunca se
// resuelven los 3 a la vez para un mismo objeto, ResolveAccessories reclasifica el resultado de
// Back DESPUES de resolverlo, sin duplicar logica de resolucion.
// GapAnalysis Encargo D (25-sep-2026): ShoesFile/ShoesSlot - 8º canal de accesorio real
// (shoeSlot, Player.cs:37193-37200), un canal COMPLETAMENTE DISTINTO de los zapatos BASE de
// la piel/pantalon del personaje (ya portados como parte de legskin/pants/shoes, ver el
// comentario de cabecera de PlayerPreviewRenderer.cs) - se resuelve aqui exactamente igual que
// los otros 7 tipos (ResolveAccessorySprite), SIN aplicar todavia la regla de sexo real
// (ArmorIDs.Shoe.Sets.MaleToFemaleID): ShoesSlot guarda el id MASCULINO/neutro tal cual lo
// declara el objeto - la sustitucion por la variante femenina se aplica en
// PlayerPreviewRenderer.Render (que ya tiene "male" en scope), mismo patron ya establecido ahi
// para SetMatchHead/headIdAfterSetMatch. Calamity no comparte esta numeracion (ResolveAccessorySprite
// deja ShoesSlot a null para sus piezas, igual que el resto de los 7 tipos).
// GapAnalysis Encargo C (25-sep-2026): BalloonFile/BalloonFrontFile - 9º tipo de accesorio real
// (balloonSlot, Player.cs:37232-37241), reclasificado en 2 canales reales POSIBLES (mismo
// patron que Back/Backpack/Tail de Encargo A - ver BalloonAccessoryLayerTable, tabla real de
// ArmorIDs.cs:2252): resuelto una vez como "Balloon" (ResolveAccessorySprite) y reclasificado
// DESPUES a "BalloonFront" si BalloonAccessoryLayerTable.IsFrontLayer(balloonSlotId), mismo
// sprite (acc_balloon/{balloonSlotId}.png), sin duplicar logica de resolucion.
// CalamityAccesorios (25-sep-2026): paridad Calamity real para los 9 canales de accesorio
// (Waist/Neck/HandsOn/HandsOff/Back/Shield/Face/Balloon/Shoes) - IsAccessoryType/
// ResolveAccessorySprite ya eran genericos por tipo desde PortSeleccion Encargo1, lo unico que
// faltaba era el DATO (CalamityCatalogEntry.EquipSlot poblado para estos 9 tipos, ver
// scripts/extraer-slot-armadura-calamity.js). Sin cambios en el renderer.
// GapAnalysis Encargo G (25-sep-2026): BeardFile/BeardSlot - 10º tipo de accesorio real
// (beardSlot, Player.cs:37243-37246, "if (item.beardSlot > 0) beard = item.beardSlot;"). A
// diferencia de Back/Balloon (que se reclasifican en 2 canales posibles cada uno) la barba es
// un canal UNICO, sin reclasificacion - se resuelve exactamente igual que Waist/Neck/Face
// (ResolveAccessorySprite generico). Calamity NO tiene ningun item real que declare
// EquipType.Beard (confirmado a mano: 0 coincidencias de "AutoloadEquip" con "Beard" en todo
// CalamityMod decompilado - scripts/extraer-slot-armadura-calamity.js ya documentaba esto en su
// cabecera, "Wings/Front/Beard quedan FUERA a proposito... 0 items reales de Calamity los
// declaran") - BeardSlot se queda a null para Calamity, mismo criterio ya establecido para el
// resto de canales. La capa se dibuja DENTRO de "Head" (PlayerPreviewRenderer.Render), despues
// de casco/pelo - ver el comentario real ahi con la cita completa de PlayerDrawLayers.cs.
// GapAnalysis Encargo F (25-sep-2026): FaceHeadFile/FaceMaskFile/FaceFlowerFile - 3 canales mas
// en los que EquipmentAppearanceResolver puede reclasificar el mismo sprite ya resuelto para
// "Face" (ver FaceAccessoryLayerTable, tabla real de ArmorIDs.cs:2184-2190) - mismo patron
// exacto que BackpackFile/TailFile (Encargo A) y BalloonFrontFile (Encargo C). FaceFile/FaceSlot
// se quedan representando SOLO el 4º canal real ("Face" normal, cuando faceSlot no cae en
// ninguna de las otras 3 tablas) tras la reclasificacion. Objetos de Calamity (faceSlotId
// siempre null, numeracion propia no compartida) se quedan en "Face" - fiel-por-defecto, mismo
// criterio ya establecido para Calamity en el resto del resolver (los 6 items reales de
// Calamity con EquipSlot=="Face" no tienen ninguna variante FaceHead/FaceMask/FaceFlower propia
// en su catalogo, confirmado en Assets/calamity/catalog.json - solo existe el string "Face").
// GapAnalysis Encargo E (25-sep-2026): FrontFile/FrontSlot - 11º tipo de accesorio real
// (frontSlot, Player.cs:37185-37188, "if (item.frontSlot > 0) front = item.frontSlot;"). Igual
// que Beard (Encargo G), es un canal UNICO sin reclasificacion aqui - se resuelve exactamente
// igual que Waist/Neck/Face (ResolveAccessorySprite generico). La condicion real de
// incompatibilidad con scarf/cape (ArmorIDs.Front.Sets.DontDrawIfWearingAScarfOrCape, consulta
// ademas Neck/Back) y el recorte en 2 mitades (FrontPart/BackPart) se aplican en
// PlayerPreviewRenderer.Render, que ya tiene NeckSlot/BackSlot en scope - mismo patron ya
// establecido ahi para ShoeMaleToFemaleID/SetMatchHead. Calamity: 0 items reales confirmados
// (grep completo de "AutoloadEquip" con "EquipType.Front" en todo CalamityMod decompilado, 0
// coincidencias - ver tambien scripts/extraer-slot-armadura-calamity.js, cabecera) - el mismo
// canal generico se cablea igual por consistencia (calamitySuffix "Front", nombre crudo del
// enum), sin datos que lo activen hoy; si Calamity anade en el futuro un item con
// EquipType.Front, funcionaria sin mas cambios de codigo aqui.
// Wings Encargo1 (25-sep-2026): WingFile/WingSlot - 12º tipo de accesorio real (wingSlot,
// Player.cs, "if (item.wingSlot > 0) wings = item.wingSlot;"), canal UNICO sin reclasificacion
// (igual que Beard/Front) - se resuelve igual que Waist/Neck/Face (ResolveAccessorySprite
// generico, directorio "acc_wing", ver scripts/extraer-sprites-alas-vanilla.js). A diferencia de
// los otros 11 tipos, el sprite NO se dibuja con DrawAccessory/LoadStripFrameAbsolute (recorte
// alineado 1:1 al lienzo, fila = legAnimationFrame) - las alas tienen su PROPIA posicion/recorte
// por id real (WingDrawTable, ver PlayerPreviewRenderer.LoadWingFrame para la cita completa de
// PlayerDrawLayers.DrawPlayer_09_Wings). Calamity: 16 items reales confirmados con
// EquipType.Wings (ver scripts/extraer-slot-armadura-calamity.js) - WingSlot se queda null para
// ellos (numeracion propia no compartida, mismo criterio ya establecido para el resto de
// canales), y PlayerPreviewRenderer usa la posicion GENERICA de WingDrawTable (fiel-por-defecto).
//
// GapAnalysis Encargo I (25-sep-2026): un campo "*Dye" mas por cada canal, hermano real de su
// "*File"/"*Slot" - el tinte PLANO ya resuelto (DyeShaderCatalog.PlainColor) del dye que Terraria
// empareja con el MISMO indice de slot (0..9) que gano ese canal en Scan() (Player.cs real,
// UpdateItemDye: "dyeItem = GetEffectiveDye(i % 10)", el mismo indice que el item funcional/
// vanidad que puso el sprite - ver el comentario real completo mas abajo, en ResolveAccessories).
// null = sin dye puesto, dye ANIMADO/SHADER real (fuera de alcance, ver DyeShaderCatalog) o dye
// de Calamity (fuera de alcance) - "sin tinte" es siempre el resultado seguro. Sigue la MISMA
// reclasificacion que su File/Slot hermano (Back->Backpack/Tail, Balloon->BalloonFront,
// Face->FaceHead/FaceMask/FaceFlower): el dye viaja junto al sprite ya resuelto, nunca se
// recalcula aparte.
//
// GapAnalysis Encargo J (25-sep-2026): 4 estados especiales de bajo impacto, CASOS AISLADOS -
// a diferencia de los 12 tipos de arriba (un accesorio cualquiera DECLARA el tipo via
// item.waistSlot/.faceSlot/etc), estos 4 se activan por item.type EXACTO sin importar que otro
// campo declare el objeto (confirmado en Player.cs real, UpdateVisibleAccessory:37151-37283,
// MISMO metodo que ya procesa waist/neck/etc, pero estos 4 checks van sueltos al final sin
// relacion con ningun campo de slot: "if (item.type == 4563) hasUnicornHorn = true;" etc.) -
// escaneados en el MISMO Scan() de abajo, con el MISMO respeto a hide[]/vanidad que el resto.
// - UnicornHornFile (item.type==4563, "Unicorn Horn"): TextureAssets.Extra[143],
//   PlayerDrawLayers.cs:2860-2870 - dibujado en DrawPlayer_22_FaceAcc, justo DESPUES de
//   FaceFlower, con la MISMA convencion de tira 40x(56*N) que el resto de tipos Face (bodyFrame
//   como rectangulo de origen, headPosition+headVect como offset) - ver Assets/player/extra/143.png
//   (scripts/extraer-sprites-estados-especiales.js).
// - AngelHaloFile (item.type==1987, "Angel Halo"): TextureAssets.AccFace[7],
//   PlayerDrawLayers.cs:2871-2884 - MISMA posicion/convencion que UnicornHorn, justo despues de
//   este en el orden real. Id 7 NUNCA sale de ningun faceSlot real (el juego lo fuerza
//   directamente) - ver ACC_FACE_SINTETICO_ANGEL_HALO en extraer-sprites-accesorios-vanilla.js.
// - Yoraiz0rDarknessFile (item.type==3581, "Yoraiz0r's Spell", la MITAD "Darkness"):
//   TextureAssets.Extra[67], PlayerDrawLayers.cs:2626-2632 (DrawPlayer_21_Head_TheFace, rama de
//   la piel base SIN faceHead puesto) - tenido con `drawinfo.colorHead`/`skinDyePacked` (el
//   MISMO tinte que la piel base de la cabeza, NO un dye de accesorio propio - por eso este
//   canal no tiene su propio campo "*Dye", se tiñe con PlayerColors.Skin en el renderer, igual
//   que Players[skinVar,0]). Frame0 unico (40x56, sin animar) - misma convencion que
//   HeadBackFile/BeardFile, ver Assets/player/extra/67.png.
// - CoatSlot (item.type==5587, "Coat"): fuerza SIEMPRE bodySlot sintetico 251
//   (Player.cs:37279-37282, "if (item.type == 5587) coat = 251;") - se dibuja como pieza de
//   CUERPO ADICIONAL (TextureAssets.ArmorBodyComposite[251], PlayerDrawLayers.cs:1406-1420/
//   2029-2036/3828-3846, MISMAS 5 celdas -torso/hombro trasero/brazo trasero/hombro delantero/
//   brazo delantero- que armor.BodyFile, dibujada SIEMPRE ENCIMA, independiente de si hay
//   armadura de cuerpo puesta o no - confirmado que los 3 bloques reales que dibujan
//   ArmorBodyComposite[coat] son un `if` SUELTO, sin relacion con el `if`/`else` de
//   ArmorBodyComposite[body]/piel base). Tiene su PROPIO canal de dye real (Player.cs:9839-9842,
//   "if (armorItem.type == 5587) cCoat = dyeItem.dye;" - shader independiente de cBody, de ahi
//   CoatDye aparte de BodyDye) - ver Assets/player/armor_body/251.png
//   (COAT_SINTETICO en extraer-sprites-armadura-vanilla.js).
//
// Yoraiz0r Eye (item.type==3580, la otra mitad de "Yoraiz0r's Spell") queda FUERA A PROPOSITO,
// LIMITE REAL: no dibuja NINGUN sprite estatico - Player.cs:37251-37254 solo fija
// "yoraiz0rEye = itemSlot - 2" (usado UNICAMENTE en UpdateWaterMakeStuff/luz de estela real,
// Player.cs:12616-12664 - la unica capa de PlayerDrawLayers.cs que referencia yoraiz0rEye es
// 0 resultados, confirmado con grep completo sobre el decompilado) - depende enteramente de
// `velocity`/`base.Center` en tiempo real (particulas de polvo + luz emitida a lo largo de la
// trayectoria del jugador mientras se mueve), algo que no existe ni tiene sentido para un doll
// ESTATICO sin fisica. No hay ningun sprite que extraer ni ninguna capa que dibujar - documentado
// aqui, sin campo en EquippedAccessories para este item.
public sealed record EquippedAccessories(
    string? WaistFile, string? NeckFile, string? HandOnFile, string? HandOffFile,
    string? BackFile, string? ShieldFile, string? FaceFile,
    int? WaistSlot = null, int? NeckSlot = null, int? HandOnSlot = null, int? HandOffSlot = null,
    int? BackSlot = null, int? ShieldSlot = null, int? FaceSlot = null,
    string? BackpackFile = null, string? TailFile = null,
    int? BackpackSlot = null, int? TailSlot = null,
    string? ShoesFile = null, int? ShoesSlot = null,
    string? BalloonFile = null, string? BalloonFrontFile = null,
    int? BalloonSlot = null, int? BalloonFrontSlot = null,
    string? BeardFile = null, int? BeardSlot = null,
    string? FaceHeadFile = null, string? FaceMaskFile = null, string? FaceFlowerFile = null,
    int? FaceHeadSlot = null, int? FaceMaskSlot = null, int? FaceFlowerSlot = null,
    string? FrontFile = null, int? FrontSlot = null,
    string? WingFile = null, int? WingSlot = null,
    PlayerPreviewRenderer.Tint? WaistDye = null, PlayerPreviewRenderer.Tint? NeckDye = null,
    PlayerPreviewRenderer.Tint? HandOnDye = null, PlayerPreviewRenderer.Tint? HandOffDye = null,
    PlayerPreviewRenderer.Tint? BackDye = null, PlayerPreviewRenderer.Tint? ShieldDye = null,
    PlayerPreviewRenderer.Tint? FaceDye = null,
    PlayerPreviewRenderer.Tint? BackpackDye = null, PlayerPreviewRenderer.Tint? TailDye = null,
    PlayerPreviewRenderer.Tint? ShoesDye = null,
    PlayerPreviewRenderer.Tint? BalloonDye = null, PlayerPreviewRenderer.Tint? BalloonFrontDye = null,
    PlayerPreviewRenderer.Tint? BeardDye = null,
    PlayerPreviewRenderer.Tint? FaceHeadDye = null, PlayerPreviewRenderer.Tint? FaceMaskDye = null, PlayerPreviewRenderer.Tint? FaceFlowerDye = null,
    PlayerPreviewRenderer.Tint? FrontDye = null,
    PlayerPreviewRenderer.Tint? WingDye = null,
    // GapAnalysis Encargo J (25-sep-2026): ver el comentario de cabecera de esta clase.
    string? UnicornHornFile = null, PlayerPreviewRenderer.Tint? UnicornHornDye = null,
    string? AngelHaloFile = null, PlayerPreviewRenderer.Tint? AngelHaloDye = null,
    string? Yoraiz0rDarknessFile = null,
    string? CoatFile = null, int? CoatSlot = null, PlayerPreviewRenderer.Tint? CoatDye = null);

public sealed class EquipmentAppearanceResolver
{
    private readonly VanillaArmorSlotCatalog _vanillaSlots;
    private readonly VanillaAccessorySlotCatalog _vanillaAccessorySlots;
    private readonly CalamityCatalog _calamity;
    private readonly PetAnimationCatalog _petAnimations;
    private readonly HashSet<int> _lightPetItemIds;

    public EquipmentAppearanceResolver(VanillaArmorSlotCatalog vanillaSlots, VanillaAccessorySlotCatalog vanillaAccessorySlots, CalamityCatalog calamity, PetAnimationCatalog petAnimations)
    {
        _vanillaSlots = vanillaSlots;
        _vanillaAccessorySlots = vanillaAccessorySlots;
        _calamity = calamity;
        _petAnimations = petAnimations;
        // Lista real de items de mascota de LUZ (Main.cs real, lightPet[] - ver el comentario
        // completo de ResolvePet mas abajo) - fichero pequeño, se carga aqui mismo sin montar
        // una clase de catalogo aparte solo para un array de ids.
        string lightPath = Path.Combine(AppContext.BaseDirectory, "Assets", "pet_light_items.json");
        _lightPetItemIds = File.Exists(lightPath)
            ? new HashSet<int>(System.Text.Json.JsonSerializer.Deserialize<int[]>(File.ReadAllText(lightPath)) ?? [])
            : [];
    }

    public PlayerPreviewRenderer.EquippedArmor Resolve(PlrLoadout loadout)
    {
        var headSlot = Visible(loadout, 0);
        var bodySlot = Visible(loadout, 1);
        var legsSlot = Visible(loadout, 2);
        return new(
            ResolveHead(headSlot),
            ResolveBody(bodySlot),
            ResolveLegs(legsSlot),
            // H6-07: el indice REAL de headSlot (Terraria.Player.head, la misma tabla que
            // ArmorHead[]/armor_head/{slot}.png) - solo se conoce para objetos VANILLA (ver
            // ResolveVanillaPath); Calamity no comparte esta numeracion, null a proposito.
            ResolveHeadSlot(headSlot),
            // H6-01-b (advisor Opus): idem para bodySlot/legSlot - hacen falta como ID (no solo
            // como ruta) para poder aplicar SetMatch/hidesTopSkin/hidesBottomSkin/
            // GetMatchingBodyExtension en PlayerPreviewRenderer (ver PlayerBodyDrawTables).
            ResolveBodySlot(bodySlot),
            ResolveLegsSlot(legsSlot),
            // GapAnalysis Encargo B (25-sep-2026): HeadBack NO es un item independiente - se
            // DERIVA del headSlot YA resuelto arriba (ver ResolveHeadBack para la cita real
            // completa de ArmorIDs.Head.Sets.FrontToBackID/DrawPlayer_01_3_BackHead).
            ResolveHeadBack(headSlot),
            // GapAnalysis Encargo I (25-sep-2026): dye[0]/dye[1]/dye[2] real (Player.cs:9679-9681,
            // "cHead = GetEffectiveDye(0).dye; cBody = GetEffectiveDye(1).dye; cLegs =
            // GetEffectiveDye(2).dye;") - el mismo indice de slot que cabeza/cuerpo/piernas,
            // SIN pasar por Visible() (a diferencia del sprite, el dye no distingue funcional/
            // vanidad - el juego real usa siempre dye[indice], nunca dye[indice+10]).
            ResolveDye(loadout.Dyes[0]),
            ResolveDye(loadout.Dyes[1]),
            ResolveDye(loadout.Dyes[2]));
    }

    // GapAnalysis Encargo I (25-sep-2026): resuelve el tinte PLANO real (o null) de un slot de
    // dye - DyeShaderCatalog.PlainColor ya distingue PLANO (recolor estatico) de ANIMADO/SHADER
    // (excluido a proposito, ver su comentario de cabecera). Los dyes de Calamity (numeracion
    // propia, item.Id >= CalamityIds.ItemIdBase) quedan FUERA DE ALCANCE de este encargo -
    // fiel-por-defecto, mismo criterio ya establecido para el resto del resolver.
    private static PlayerPreviewRenderer.Tint? ResolveDye(PlrItemSlot dyeSlot)
    {
        if (dyeSlot.IsEmpty || dyeSlot.Id >= CalamityIds.ItemIdBase) return null;
        return DyeShaderCatalog.PlainColor(dyeSlot.Id) is { } c ? new PlayerPreviewRenderer.Tint(c.R, c.G, c.B) : null;
    }

    private static PlrItemSlot Visible(PlrLoadout loadout, int index) =>
        loadout.Social[index].IsEmpty ? loadout.Items[index] : loadout.Social[index];

    // PortSeleccion Encargo1 (25-sep-2026): resuelve los 7 sprites de accesorio funcional/
    // vanidad reales (Waist/Neck/HandOn/HandOff/Back/Shield/Face), aplicando la misma regla
    // "vanidad tapa a funcional" que Resolve() ya aplica para cabeza/cuerpo/piernas - pero por
    // TIPO en vez de por INDICE, porque los 7 huecos de accesorio (indices 3..9 de
    // PlrLoadout.Items/Social) son GENERICOS: cualquiera de ellos puede llevar cualquiera de
    // los 7 tipos (confirmado en Player.cs real, UpdateVisibleAccessory: "if
    // (item.waistSlot > 0) waist = item.waistSlot;" y analogo para los otros 6 campos, sin
    // relacion con la posicion del item dentro del array).
    //
    // Fidelidad real al juego (Player.cs, UpdateVisibleAccessories): recorre PRIMERO los 7
    // huecos funcionales en orden 3..9 (si dos items funcionales declaran el mismo tipo, el de
    // indice mas alto pisa al anterior - "ultimo en escribir gana", igual que el bucle real) y
    // LUEGO los 7 huecos de vanidad en el mismo orden, que pisan cualquier valor funcional del
    // mismo tipo si hay un item de vanidad de ese tipo puesto - el mismo comportamiento que un
    // objeto de vanidad "tapa" al funcional en Resolve(), solo que aqui la correspondencia es
    // por TIPO de accesorio, no por indice de slot compartido.
    //
    // GapAnalysis Encargo H (25-sep-2026): hide[] ahora SI se respeta - cierra el hueco real
    // documentado antes aqui ("ALCANCE DELIBERADO... no respeta hideVisibleAccessory"). Bug de
    // datos real encontrado por el arquitecto-keep: el array de 10 bits real (hideVisibleAccessory
    // en Player.cs) vive en PlrCharacter.Loadouts[CurrentLoadout].Hide, NUNCA en
    // PlrCharacter.PrimaryLoadout (PlrLoadout.CreateEmpty(isPrimary:true) fija Hide=null siempre,
    // ver PlrLoadout.cs - loadouts[0] es un mirror que el propio cliente de Terraria no serializa
    // con Hide) - el llamador es responsable de pasar el Hide REAL del loadout activo (ver
    // CharacterListEntryViewModel), este metodo solo aplica el array que recibe.
    //
    // Regla real (Player.cs, UpdateVisibleAccessories): el toggle SOLO gatea el hueco FUNCIONAL
    // (i=3..9 de loadout.Items) - el bucle de vanidad (armor[13..19] real, loadout.Social aqui)
    // NO tiene ningun chequeo de hideVisibleAccessory, la vanidad puesta se ve SIEMPRE. Por eso
    // "hide" solo se consulta al escanear loadout.Items, nunca loadout.Social.
    //
    // DELIBERATE DIFFERENCE (Wings): el juego real tiene una excepcion en el propio bucle
    // funcional (Player.cs:37063-37074) - "if (hideVisibleAccessory[i] && (velocity.Y == 0f ||
    // mount.Active)) continue;" - unas alas ocultas SI se siguen mostrando si el jugador esta
    // cayendo de verdad (velocity.Y != 0) y no esta montado. Esta app dibuja un doll ESTATICO sin
    // fisica real (siempre "en reposo", equivalente a velocity.Y==0f) - bajo esa condicion la
    // propia formula real del juego colapsa a "si esta oculto, no se ve" sin excepcion (el OR con
    // mount.Active tampoco aplica, un doll no tiene montura). Por eso Wings se resuelve aqui con
    // el MISMO chequeo generico que el resto de los 9 tipos (hide[i] oculta el slot entero, tal
    // cual), sin replicar la rama de "cayendo" - decision explicita, no una simplificacion oculta.
    // GapAnalysis Encargo I (25-sep-2026): dye REAL emparejado con el ganador de cada canal -
    // Player.cs:9691-9697/9702 (UpdateDyes/UpdateItemDye real): "int num = i % 10;
    // UpdateItemDye(i < 10, hideVisibleAccessory[num], GetEffectiveArmor(i),
    // GetEffectiveDye(num));" - el bucle real recorre los 20 slots (0..9 funcional, 10..19
    // vanidad) y usa dye[i % 10] SIEMPRE, tanto para el hueco funcional i como para su gemelo de
    // vanidad i+10 - el MISMO indice de dye sirve para los dos. Por eso Scan() guarda, junto al
    // item que gana cada canal, el dye emparejado con SU MISMO indice de slot (loadout.Dyes[i]),
    // sin importar si vino de Items o de Social - "ultimo en escribir gana" ya vale igual para
    // el dye que para el sprite, porque ambos se sobrescriben juntos en el mismo if.
    private readonly record struct AccessoryMatch(PlrItemSlot Item, PlrItemSlot Dye);

    public EquippedAccessories ResolveAccessories(PlrLoadout loadout, bool[]? hide = null)
    {
        AccessoryMatch? waist = null, neck = null, handOn = null, handOff = null, back = null, shield = null, face = null, shoes = null, balloon = null, beard = null, front = null, wing = null;
        // GapAnalysis Encargo J (25-sep-2026): los 4 estados especiales de bajo impacto - ver el
        // comentario de cabecera de EquippedAccessories para la cita real completa de cada uno.
        AccessoryMatch? unicornHorn = null, angelHalo = null, yoraiz0rDarkness = null, coat = null;

        void Scan(PlrItemSlot[] slots, bool respectHide)
        {
            for (int i = 3; i <= 9; i++)
            {
                // Solo el hueco FUNCIONAL (loadout.Items) respeta hide[i] - ver el comentario
                // real de cabecera de este metodo (UpdateVisibleAccessories real, bucle de
                // vanidad sin chequeo de hideVisibleAccessory).
                if (respectHide && hide is not null && i < hide.Length && hide[i]) continue;
                var s = slots[i];
                if (s.IsEmpty) continue;
                var match = new AccessoryMatch(s, loadout.Dyes[i]);
                if (IsAccessoryType(s, e => e.Waist, "Waist")) waist = match;
                if (IsAccessoryType(s, e => e.Neck, "Neck")) neck = match;
                // CalamityAccesorios (25-sep-2026): el sufijo real de Calamity es el nombre CRUDO
                // del enum EquipType (HandsOn/HandsOff/Shoes, confirmado en el propio
                // EquipType.cs decompilado Y en los ficheros reales ya extraidos de
                // Assets/calamity/icons/, ej. BloodstainedGlove_HandsOn.png) - NO el alias corto
                // "HandOn"/"HandOff"/"Shoe" que usan los campos vanilla de abajo (e.HandOn/
                // e.Shoe, VanillaAccessorySlotEntry) ni los directorios acc_handon/acc_shoes.
                // Los dos sufijos son independientes a proposito: calamitySuffix solo se usa
                // dentro de la rama Calamity de IsAccessoryType/ResolveAccessorySprite.
                if (IsAccessoryType(s, e => e.HandOn, "HandsOn")) handOn = match;
                if (IsAccessoryType(s, e => e.HandOff, "HandsOff")) handOff = match;
                if (IsAccessoryType(s, e => e.Back, "Back")) back = match;
                if (IsAccessoryType(s, e => e.Shield, "Shield")) shield = match;
                if (IsAccessoryType(s, e => e.Face, "Face")) face = match;
                // GapAnalysis Encargo D (25-sep-2026): shoeSlot, mismo patron exacto.
                if (IsAccessoryType(s, e => e.Shoe, "Shoes")) shoes = match;
                // GapAnalysis Encargo C (25-sep-2026): balloonSlot, mismo patron exacto.
                if (IsAccessoryType(s, e => e.Balloon, "Balloon")) balloon = match;
                // GapAnalysis Encargo G (25-sep-2026): beardSlot, mismo patron exacto. Calamity
                // no declara EquipType.Beard en ningun item real (ver comentario de
                // EquippedAccessories) - calamitySuffix "Beard" nunca hace match ahi, sin
                // riesgo de falso positivo.
                if (IsAccessoryType(s, e => e.Beard, "Beard")) beard = match;
                // GapAnalysis Encargo E (25-sep-2026): frontSlot, mismo patron exacto.
                if (IsAccessoryType(s, e => e.Front, "Front")) front = match;
                // Wings Encargo1 (25-sep-2026): wingSlot, mismo patron exacto.
                if (IsAccessoryType(s, e => e.Wing, "Wings")) wing = match;
                // GapAnalysis Encargo J (25-sep-2026): los 4 estados especiales - item.type EXACTO,
                // sin relacion con ningun campo de slot (ver el comentario real completo de
                // EquippedAccessories). "Ultimo en escribir gana" vale igual aqui: si dos items
                // funcionales/de vanidad con el MISMO item.type especial llegaran a coexistir (no
                // posible hoy, solo existe 1 item real de cada), el de indice mas alto pisaria al
                // anterior, igual que el resto de canales.
                if (s.Id == 4563) unicornHorn = match;
                if (s.Id == 1987) angelHalo = match;
                if (s.Id == 3581) yoraiz0rDarkness = match;
                if (s.Id == 5587) coat = match;
            }
        }
        Scan(loadout.Items, respectHide: true);
        Scan(loadout.Social, respectHide: false);

        var (waistFile, waistSlotId) = ResolveAccessorySprite(waist?.Item, "Waist", e => e.Waist, "acc_waist");
        var waistDye = ResolveDye(waist?.Dye ?? PlrItemSlot.Empty);
        var (neckFile, neckSlotId) = ResolveAccessorySprite(neck?.Item, "Neck", e => e.Neck, "acc_neck");
        var neckDye = ResolveDye(neck?.Dye ?? PlrItemSlot.Empty);
        var (handOnFile, handOnSlotId) = ResolveAccessorySprite(handOn?.Item, "HandsOn", e => e.HandOn, "acc_handon");
        var handOnDye = ResolveDye(handOn?.Dye ?? PlrItemSlot.Empty);
        var (handOffFile, handOffSlotId) = ResolveAccessorySprite(handOff?.Item, "HandsOff", e => e.HandOff, "acc_handoff");
        var handOffDye = ResolveDye(handOff?.Dye ?? PlrItemSlot.Empty);
        var (backFile, backSlotId) = ResolveAccessorySprite(back?.Item, "Back", e => e.Back, "acc_back");
        var backDye = ResolveDye(back?.Dye ?? PlrItemSlot.Empty);
        var (shieldFile, shieldSlotId) = ResolveAccessorySprite(shield?.Item, "Shield", e => e.Shield, "acc_shield");
        var shieldDye = ResolveDye(shield?.Dye ?? PlrItemSlot.Empty);
        var (faceFile, faceSlotId) = ResolveAccessorySprite(face?.Item, "Face", e => e.Face, "acc_face");
        var faceDye = ResolveDye(face?.Dye ?? PlrItemSlot.Empty);
        // GapAnalysis Encargo D (25-sep-2026): guarda el id MASCULINO/neutro tal cual - la
        // regla de sexo real (MaleToFemaleID) se aplica despues, en PlayerPreviewRenderer.Render.
        var (shoesFile, shoesSlotId) = ResolveAccessorySprite(shoes?.Item, "Shoes", e => e.Shoe, "acc_shoes");
        var shoesDye = ResolveDye(shoes?.Dye ?? PlrItemSlot.Empty);
        var (balloonFile, balloonSlotId) = ResolveAccessorySprite(balloon?.Item, "Balloon", e => e.Balloon, "acc_balloon");
        var balloonDye = ResolveDye(balloon?.Dye ?? PlrItemSlot.Empty);
        var (beardFile, beardSlotId) = ResolveAccessorySprite(beard?.Item, "Beard", e => e.Beard, "acc_beard");
        var beardDye = ResolveDye(beard?.Dye ?? PlrItemSlot.Empty);
        var (frontFile, frontSlotId) = ResolveAccessorySprite(front?.Item, "Front", e => e.Front, "acc_front");
        var frontDye = ResolveDye(front?.Dye ?? PlrItemSlot.Empty);
        // Wings Encargo1 (25-sep-2026): wingSlot, mismo patron exacto - calamitySuffix "Wings"
        // (nombre crudo del enum, igual que HandsOn/HandsOff/Shoes, ver scripts/
        // extraer-slot-armadura-calamity.js).
        var (wingFile, wingSlotId) = ResolveAccessorySprite(wing?.Item, "Wings", e => e.Wing, "acc_wing");
        var wingDye = ResolveDye(wing?.Dye ?? PlrItemSlot.Empty);

        // GapAnalysis Encargo A (25-sep-2026): reclasifica el resultado de "Back" YA resuelto en
        // los otros 2 canales reales posibles (Player.cs:37169-37184, UpdateVisibleAccessory) -
        // el sprite es el mismo (acc_back/{backSlotId}.png), solo cambia a que campo va. Objetos
        // de Calamity (backSlotId siempre null, numeracion propia no compartida) se quedan en
        // "Back" - fiel-por-defecto, mismo criterio ya establecido para Calamity en el resto del
        // resolver (sin SetMatch/hidesTopSkin, ver PlayerPreviewRenderer.Render).
        string? backpackFile = null, tailFile = null;
        int? backpackSlotId = null, tailSlotId = null;
        PlayerPreviewRenderer.Tint? backpackDye = null, tailDye = null;
        if (backSlotId is int backId && BackAccessoryLayerTable.IsBackpackLayer(backId))
        {
            (backpackFile, backpackSlotId, backpackDye) = (backFile, backSlotId, backDye);
            (backFile, backSlotId, backDye) = (null, null, null);
        }
        else if (backSlotId is int tailId && BackAccessoryLayerTable.IsTailLayer(tailId))
        {
            (tailFile, tailSlotId, tailDye) = (backFile, backSlotId, backDye);
            (backFile, backSlotId, backDye) = (null, null, null);
        }

        // GapAnalysis Encargo C (25-sep-2026): reclasifica el resultado de "Balloon" YA resuelto
        // en el otro canal real posible (Player.cs:37232-37241, UpdateVisibleAccessory) - el
        // sprite es el mismo (acc_balloon/{balloonSlotId}.png), solo cambia a que campo va.
        // Objetos de Calamity (balloonSlotId siempre null, numeracion propia no compartida) se
        // quedan en "Balloon" - fiel-por-defecto, mismo criterio ya establecido para Calamity en
        // el resto del resolver.
        string? balloonFrontFile = null;
        int? balloonFrontSlotId = null;
        PlayerPreviewRenderer.Tint? balloonFrontDye = null;
        if (balloonSlotId is int balloonId && BalloonAccessoryLayerTable.IsFrontLayer(balloonId))
        {
            (balloonFrontFile, balloonFrontSlotId, balloonFrontDye) = (balloonFile, balloonSlotId, balloonDye);
            (balloonFile, balloonSlotId, balloonDye) = (null, null, null);
        }

        // GapAnalysis Encargo F (25-sep-2026): reclasifica el resultado de "Face" YA resuelto en
        // los otros 3 canales reales posibles (Player.cs:37213-37231, UpdateVisibleAccessory) -
        // el sprite es el mismo (acc_face/{faceSlotId}.png), solo cambia a que campo va. Objetos
        // de Calamity (faceSlotId siempre null, numeracion propia no compartida) se quedan en
        // "Face" - fiel-por-defecto, mismo criterio ya establecido para Calamity en el resto del
        // resolver. Orden real de comprobacion (Player.cs:37215-37229, if/else if encadenados):
        // FaceHead primero, FaceMask despues, FaceFlower al final - un faceSlot solo puede caer
        // en UNA de las 3 tablas en la practica (FaceAccessoryLayerTable las transcribe
        // literales, sin solaparse hoy), pero se respeta el orden real por si acaso.
        string? faceHeadFile = null, faceMaskFile = null, faceFlowerFile = null;
        int? faceHeadSlotId = null, faceMaskSlotId = null, faceFlowerSlotId = null;
        PlayerPreviewRenderer.Tint? faceHeadDye = null, faceMaskDye = null, faceFlowerDye = null;
        if (faceSlotId is int faceHeadId && FaceAccessoryLayerTable.IsFaceHeadLayer(faceHeadId))
        {
            (faceHeadFile, faceHeadSlotId, faceHeadDye) = (faceFile, faceSlotId, faceDye);
            (faceFile, faceSlotId, faceDye) = (null, null, null);
        }
        else if (faceSlotId is int faceMaskId && FaceAccessoryLayerTable.IsFaceMaskLayer(faceMaskId))
        {
            (faceMaskFile, faceMaskSlotId, faceMaskDye) = (faceFile, faceSlotId, faceDye);
            (faceFile, faceSlotId, faceDye) = (null, null, null);
        }
        else if (faceSlotId is int faceFlowerId && FaceAccessoryLayerTable.IsFaceFlowerLayer(faceFlowerId))
        {
            (faceFlowerFile, faceFlowerSlotId, faceFlowerDye) = (faceFile, faceSlotId, faceDye);
            (faceFile, faceSlotId, faceDye) = (null, null, null);
        }

        // GapAnalysis Encargo J (25-sep-2026): resuelve la ruta fija real de cada estado especial
        // (null si el item no esta puesto en ningun hueco) - ver el comentario de cabecera de
        // EquippedAccessories para la cita completa de cada uno. "lo que no se encuentra no se
        // inventa" (FixedVanillaPath ya comprueba File.Exists), mismo criterio que el resto del
        // resolver.
        string? unicornHornFile = unicornHorn is not null ? FixedVanillaPath("extra", 143) : null;
        var unicornHornDye = ResolveDye(unicornHorn?.Dye ?? PlrItemSlot.Empty);
        string? angelHaloFile = angelHalo is not null ? FixedVanillaPath("acc_face", 7) : null;
        var angelHaloDye = ResolveDye(angelHalo?.Dye ?? PlrItemSlot.Empty);
        string? yoraiz0rDarknessFile = yoraiz0rDarkness is not null ? FixedVanillaPath("extra", 67) : null;
        string? coatFile = coat is not null ? FixedVanillaPath("armor_body", 251) : null;
        int? coatSlotId = coat is not null ? 251 : null;
        var coatDye = ResolveDye(coat?.Dye ?? PlrItemSlot.Empty);

        return new EquippedAccessories(
            waistFile, neckFile, handOnFile, handOffFile, backFile, shieldFile, faceFile,
            waistSlotId, neckSlotId, handOnSlotId, handOffSlotId, backSlotId, shieldSlotId, faceSlotId,
            backpackFile, tailFile, backpackSlotId, tailSlotId,
            shoesFile, shoesSlotId,
            balloonFile, balloonFrontFile, balloonSlotId, balloonFrontSlotId,
            beardFile, beardSlotId,
            faceHeadFile, faceMaskFile, faceFlowerFile, faceHeadSlotId, faceMaskSlotId, faceFlowerSlotId,
            frontFile, frontSlotId,
            wingFile, wingSlotId,
            waistDye, neckDye, handOnDye, handOffDye, backDye, shieldDye, faceDye,
            backpackDye, tailDye, shoesDye, balloonDye, balloonFrontDye, beardDye,
            faceHeadDye, faceMaskDye, faceFlowerDye, frontDye, wingDye,
            unicornHornFile, unicornHornDye, angelHaloFile, angelHaloDye, yoraiz0rDarknessFile,
            coatFile, coatSlotId, coatDye);
    }

    // GapAnalysis Encargo J (25-sep-2026): resuelve una ruta de sprite con un id FIJO (no
    // dependiente de una tabla de catalogo, a diferencia de ResolveVanillaPath/
    // ResolveAccessorySprite) - "lo que no se encuentra no se inventa", mismo criterio que el
    // resto del resolver.
    private static string? FixedVanillaPath(string dir, int id)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", dir, id + ".png");
        return File.Exists(path) ? path : null;
    }

    // Un item real de Terraria solo declara UNO de los 7 campos de accesorio en la practica,
    // pero nada impide comprobar varios a la vez (igual que VanillaAccessorySlotEntry puede en
    // teoria traer mas de una clave rellena) - ver el comentario de esa clase.
    private bool IsAccessoryType(PlrItemSlot slot, Func<VanillaAccessorySlotEntry, int?> vanillaPick, string calamitySuffix)
    {
        if (slot.Id >= CalamityIds.ItemIdBase)
        {
            var entry = _calamity.BySyntheticId(slot.Id);
            // CalamityAccesorios (25-sep-2026): 6 guantes reales declaran HandsOn+HandsOff a la
            // vez - EquipSlotSecondary es el unico caso real con un segundo canal (ver su
            // comentario en CalamityCatalog.cs).
            return entry is not null && (entry.EquipSlot == calamitySuffix || entry.EquipSlotSecondary == calamitySuffix);
        }
        var vEntry = _vanillaAccessorySlots.ById(slot.Id);
        return vEntry is not null && vanillaPick(vEntry) is not null;
    }

    // Analogo a Resolve() (armadura) pero devuelve TAMBIEN el indice real del slot vanilla
    // (null para Calamity, numeracion propia no compartida - mismo criterio que
    // ResolveHeadSlot/ResolveBodySlot/ResolveLegsSlot).
    private (string? File, int? SlotId) ResolveAccessorySprite(PlrItemSlot? slot, string calamitySuffix, Func<VanillaAccessorySlotEntry, int?> vanillaPick, string vanillaDir)
    {
        if (slot is null || slot.Value.IsEmpty) return (null, null);
        var s = slot.Value;

        if (s.Id >= CalamityIds.ItemIdBase)
        {
            var entry = _calamity.BySyntheticId(s.Id);
            if (entry is null || (entry.EquipSlot != calamitySuffix && entry.EquipSlotSecondary != calamitySuffix)) return (null, null);
            string calPath = Path.Combine(AppContext.BaseDirectory, "Assets", "calamity", "icons", entry.Internal + "_" + calamitySuffix + ".png");
            return (File.Exists(calPath) ? calPath : null, null);
        }

        var vEntry = _vanillaAccessorySlots.ById(s.Id);
        int? spriteId = vEntry is null ? null : vanillaPick(vEntry);
        if (spriteId is null) return (null, null);
        // Unos pocos ids reales se quedaron sin sprite extraible de la instalacion real (ver
        // extraer-sprites-accesorios-vanilla.js, "faltan ids") - "lo que no se encuentra no se
        // inventa", igual que Resolve().
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", vanillaDir, spriteId + ".png");
        return (File.Exists(path) ? path : null, spriteId);
    }

    // Hover en Inicio ("mascotas... deben aparecer en la vista previa, EN VIVO, animadas de
    // verdad" - bitacora.md 21-sep-2026, corregido tras comparacion real del usuario contra
    // vanilla). Terraria/GameContent/UI/Elements/UICharacter.cs real, PreparePetProjectiles()
    // (linea 59): "Item item = _player.miscEquips[0]" - el slot 0 de miscEquips (pet/mascota) es
    // el UNICO que la propia pantalla de seleccion de Terraria muestra (miscEquips[1..4] son
    // montura/minecart/gancho, sin capa visual en esa pantalla real). PlrBodySerializer.cs real
    // confirma que PlrCharacter.EquipmentItems[0..4] es la MISMA tabla miscEquips serializada
    // 1:1 (comentario propio: "pet/mascota 'miscEquips'... vive solo dentro del loadout primario
    // de 8 slots"), asi que EquipmentItems[0] es el slot real a leer aqui, sin loadout de por
    // medio (a diferencia de armadura/vanidad, la mascota NO es por loadout en el juego real).
    //
    // UICharacter.cs real filtra el slot con "Main.vanityPet[item.buffType] &&
    // !Main.lightPet[item.buffType]" (Main.cs:9378-9458) - las mascotas de LUZ NO se enseñan
    // NUNCA en esta pantalla, ni animadas ni como icono. _lightPetItemIds (ver el constructor,
    // generado con el mismo criterio real que PetAnimationCatalog) cierra ese hueco para los 9
    // items de luz reales encontrados con texto en Item.cs.
    //
    // ALCANCE DELIBERADO restante, documentado y no oculto: PetAnimationCatalog cubre 63 de las
    // ~70 mascotas de VANIDAD reales (ver el comentario de esa clase) - las que no tienen
    // entrada ahi (icono real igualmente correcto, VanillaIconResolver) caen al icono estatico,
    // nunca a un crash ni a un dato inventado.
    public PetPreview? ResolvePet(PlrItemSlot[] equipmentItems)
    {
        if (equipmentItems.Length == 0 || equipmentItems[0].IsEmpty) return null;
        int itemId = equipmentItems[0].Id;
        if (_lightPetItemIds.Contains(itemId)) return null;

        var animated = _petAnimations.ByItemId(itemId);
        string? iconPath = VanillaIconResolver.GetIconPath(itemId);
        return animated is null && iconPath is null ? null : new PetPreview(animated, iconPath);
    }

    private string? ResolveHead(PlrItemSlot slot) => Resolve(slot, "Head", e => e.Head, "armor_head");
    private string? ResolveBody(PlrItemSlot slot) => Resolve(slot, "Body", e => e.Body, "armor_body");
    private string? ResolveLegs(PlrItemSlot slot) => Resolve(slot, "Legs", e => e.Legs, "armor_legs");

    private int? ResolveHeadSlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Head;
    }

    // GapAnalysis Encargo B (25-sep-2026): HeadBack no es un item/canal independiente - se
    // DERIVA del headSlot YA resuelto por ResolveHeadSlot (el mismo casco que ya se resuelve
    // para la capa Head) via Terraria.ID.ArmorIDs.Head.Sets.FrontToBackID (ArmorIDs.cs:14 del
    // decompilado real, transcrita literal en PlayerBodyDrawTables.HeadFrontToBackID). Se
    // dibuja en DrawPlayer_01_3_BackHead (PlayerDrawLayers.cs:319-337 real), que reutiliza el
    // MISMO array de texturas que ya usa el casco normal (TextureAssets.ArmorHead ->
    // Assets/player/armor_head/{id}.png) - los 6 ids "back" reales (246/247/248/249/252/253) se
    // ampliaron a la extraccion real en scripts/extraer-sprites-armadura-vanilla.js
    // (HEAD_SINTETICOS_FRONT_TO_BACK) porque ningun item real los usa como headSlot "de frente",
    // asi que no salian nunca de vanilla_armor_slots.json. Devuelve null si el headSlot actual no
    // tiene entrada en la tabla (la inmensa mayoria de cascos reales - solo 6 variantes de
    // "orejas" la tienen) o si el objeto es de Calamity (ResolveHeadSlot ya devuelve null en ese
    // caso, numeracion propia no compartida).
    private string? ResolveHeadBack(PlrItemSlot slot)
    {
        if (ResolveHeadSlot(slot) is not int headSlotId) return null;
        if (PlayerBodyDrawTables.HeadFrontToBackID(headSlotId) is not int backId) return null;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "armor_head", backId + ".png");
        // "lo que no se encuentra no se inventa" - mismo criterio ya establecido en Resolve().
        return File.Exists(path) ? path : null;
    }

    // Calamity no comparte la numeracion de bodySlot/legSlot vanilla (registra sus propios
    // equip slots por mod) - null a proposito, ESPEC-dibujado-sprites.md#7.7 punto 8: el
    // camino fiel-por-defecto para una pieza de Calamity es "hasBody=true, sin SetMatch".
    private int? ResolveBodySlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Body;
    }

    private int? ResolveLegsSlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Legs;
    }

    private string? Resolve(PlrItemSlot slot, string calamitySuffix, Func<VanillaArmorSlotEntry, int?> vanillaPick, string vanillaDir)
    {
        if (slot.IsEmpty) return null;

        string? path = slot.Id >= CalamityIds.ItemIdBase
            ? ResolveCalamityPath(slot.Id, calamitySuffix)
            : ResolveVanillaPath(slot.Id, vanillaPick, vanillaDir);

        // Unos pocos ids reales (~1-2%) se quedaron sin sprite extraible de la instalacion
        // real (hoja mas pequeña que el lienzo estandar, ver extraer-sprites-armadura-
        // vanilla.js) - "lo que no se encuentra no se inventa", el doll se queda sin esa
        // capa en vez de intentar cargar un fichero que no existe.
        return path is not null && File.Exists(path) ? path : null;
    }

    private string? ResolveCalamityPath(int itemId, string calamitySuffix)
    {
        var entry = _calamity.BySyntheticId(itemId);
        if (entry is null || entry.EquipSlot != calamitySuffix) return null;
        return Path.Combine(AppContext.BaseDirectory, "Assets", "calamity", "icons", entry.Internal + "_" + calamitySuffix + ".png");
    }

    private string? ResolveVanillaPath(int itemId, Func<VanillaArmorSlotEntry, int?> pick, string vanillaDir)
    {
        var entry = _vanillaSlots.ById(itemId);
        int? spriteId = entry is null ? null : pick(entry);
        return spriteId is null ? null : Path.Combine(AppContext.BaseDirectory, "Assets", "player", vanillaDir, spriteId + ".png");
    }
}
