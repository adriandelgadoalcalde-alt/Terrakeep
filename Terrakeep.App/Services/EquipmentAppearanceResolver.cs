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
//
// ParidadPersonaje Fase5 (26-sep-2026): FloatingTube (item.type==4404, "Floating Tube") -
// investigacion previa lo habia marcado INCONCLUSIVE ("depende de estar mojado"), CORREGIDO aqui
// contra el decompilado real: NO existe ninguna dependencia de "wet"/mojado en todo el pipeline de
// dibujado. `hasFloatingTube` se fija exactamente igual que hasUnicornHorn/hasAngelHalo
// (Player.cs:37345-37348, dentro del MISMO UpdateVisibleAccessory, "if (item.type == 4404)
// hasFloatingTube = true;") y `drawFloatingTube = drawPlayer.hasFloatingTube && !hideEntirePlayer;`
// (PlayerDrawSet.cs:2776) - IDENTICO al gate real de drawUnicornHorn/drawAngelHalo en la linea de
// arriba, sin ningun campo "wet"/"lavaWet"/"honeyWet" involucrado (grep completo confirmado). Es
// el 5º estado especial "aislado" de este tipo, simplemente omitido por error del GapAnalysis
// Encargo J original (que solo listo 4). TextureAssets.Extra[105] (grep completo confirma que es
// el UNICO Extra[] real usado para FloatingTube), dibujado dos veces en dos capas reales distintas
// con la MISMA posicion "torso" (formula identica a torsoskin/ArmorBodyComposite[torso], sin
// compositeOffset_BackArm/FrontArm) pero DOS FRAMES DISTINTOS de la misma tira 40x112 (2 filas) -
// no es animacion en el tiempo, es un split espacial real fijo: frame0 (fila 0) en
// DrawPlayer_12_Skin_Composite (PlayerDrawLayers.cs:2294-2300, DENTRO del mismo gate
// "!hidesTopSkin" que la piel del torso) y frame1 (fila 1) en DrawPlayer_17_TorsoComposite
// (PlayerDrawLayers.cs:3352-3358, SIEMPRE, tras el bloque hasBody/else, sin gate de hidesTopSkin -
// mismo punto real donde ya se dibuja Coat). Dye propio real (`cFloatingTube`, Player.cs:8149,
// "cFloatingTube = dyeItem.dye;" - canal independiente de BodyDye, mismo patron que CoatDye).
//
// ParidadPersonaje Fase5 (26-sep-2026): otros 2 casos de la MISMA ronda, CONFIRMADOS como NO
// APLICA (sin sprite estatico portable) contra el decompilado real - ninguno de los dos tiene
// campo en EquippedAccessories, mismo criterio que Yoraiz0r Eye arriba:
// - **Leinfors Hair Shampoo** (item.type==3929, `leinforsHair`): capa real
//   `DrawPlayer_07_LeinforsHairShampoo` (PlayerDrawLayers.cs:742-846) - confirmado leyendo el
//   cuerpo COMPLETO del metodo: NUNCA dibuja un sprite/DrawData propio, unicamente llama
//   `Dust.NewDust(...)` con probabilidad `Main.rand.Next(20/40/15)==0` (particulas de purpurina
//   aleatorias, tintadas en tiempo real con `GameShaders.Armor.GetSecondaryShader(cLeinShampoo,
//   drawPlayer)`) - no hay NINGUN frame fijo que extraer, es un sistema de particulas puramente
//   aleatorio y dependiente del tiempo/velocidad del jugador. Mas alla de "shader": no existe
//   ninguna textura base que recortar. LIMITE REAL confirmado, sin cambio de codigo.
// - **Rainbow Cursor** (item.type==5075, `hasRainbowCursor`, Player.cs:2359/36357-36359):
//   confirmado con grep completo sobre `PlayerDrawLayers.cs`/`LegacyPlayerRenderer.cs` - 0
//   referencias a `hasRainbowCursor` en todo el pipeline de dibujado del jugador. El UNICO uso
//   real es `Main.cs:62543-62544` ("if (!gameMenu && LocalPlayer.hasRainbowCursor) ...") para
//   colorear `Main.cursorColor` - el cursor del RATON, una capa de UI completamente aparte del
//   doll/jugador. NO APLICA confirmado, sin cambio de codigo.
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
    string? CoatFile = null, int? CoatSlot = null, PlayerPreviewRenderer.Tint? CoatDye = null,
    // ParidadPersonaje Fase5 (26-sep-2026): ver el comentario de cabecera de esta clase.
    string? FloatingTubeFile = null, PlayerPreviewRenderer.Tint? FloatingTubeDye = null);

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

    // ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugD - "otherLoadouts" es
    // PlrCharacter.Loadouts (los otros loadouts guardados, o vacio/null en un personaje sin
    // loadouts) - se usa SOLO para el favorito cross-loadout de ResolveEffectiveSlot (ver su
    // comentario). Parametro opcional para no romper compatibilidad con los llamadores/tests que
    // ya existian antes de este encargo: null se comporta EXACTAMENTE igual que antes (sin datos
    // de otros loadouts, ResolveEffectiveSlot devuelve el slot propio tal cual).
    public PlayerPreviewRenderer.EquippedArmor Resolve(PlrLoadout loadout, IReadOnlyList<PlrLoadout>? otherLoadouts = null)
    {
        var headSlot = Visible(loadout, 0, otherLoadouts);
        var bodySlot = Visible(loadout, 1, otherLoadouts);
        var legsSlot = Visible(loadout, 2, otherLoadouts);
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

    private static PlrItemSlot Visible(PlrLoadout loadout, int index, IReadOnlyList<PlrLoadout>? otherLoadouts = null)
    {
        var social = ResolveEffectiveSlot(loadout.Social, otherLoadouts, index, vanity: true);
        return social.IsEmpty ? ResolveEffectiveSlot(loadout.Items, otherLoadouts, index, vanity: false) : social;
    }

    // ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugD - puerto real de Player.
    // GetEffectiveArmor (Player.cs:5678-5734, decompilado real): "si el slot de este INDICE esta
    // vacio en el loadout activo (aqui: loadout, PrimaryLoadout del llamador), busca un objeto
    // FAVORITO (Item.favorited) en el MISMO indice de CUALQUIERA de los otros loadouts guardados
    // (Loadouts[] real, aqui otherLoadouts), en orden, y lo 'presta' visualmente". Cita real
    // completa:
    //   if (armor[slot].IsAir) { for (i=0..Loadouts.Length) { item = Loadouts[i].Armor[slot];
    //     if (!item.IsAir && item.favorited) { if (!CanShareArmor(item, slot)) break;
    //       sharedFromLoadout = i; return item; } } } return armor[slot];
    // Los items NO favoritos de otros loadouts en ese mismo indice se SALTAN (el bucle real solo
    // actua "if (!item.IsAir && item.favorited)", si no se cumple sigue con el siguiente
    // loadout) - el primer item FAVORITO que aparece corta la busqueda ahi (se use o no, por eso
    // el "break" real no sigue mirando mas loadouts si CanShareArmor falla).
    //
    // Aplica por igual a slots de armadura (indices 0..2, cabeza/cuerpo/piernas - el bug real
    // confirmado por el arquitecto: "afecta TAMBIEN a head/body/legs no solo accesorios,
    // PlayerFrame usa GetEffectiveArmor") y de accesorio (indices 3..9), y por separado a cada
    // MITAD (funcional via Items[], vanidad via Social[] aqui - la doble llamada real
    // GetEffectiveArmor(10)/GetEffectiveArmor(0) en PlayerFileData/PlayerHooks confirma que el
    // favorito se resuelve para las DOS mitades de forma independiente, ANTES de que la vanidad
    // tape a lo funcional).
    //
    // ALCANCE DELIBERADO: CanShareArmor real (Player.cs:5706-5734) tiene 2 partes - (a) el
    // candidato debe encajar de verdad en ESE canal (ItemSlot.CanEquipInArmorSlot: headSlot/
    // bodySlot/legSlot >= 0 para armadura, item.accessory==true para accesorios) y (b) para
    // accesorios, ademas no puede chocar por TIPO con ningun otro accesorio ya equipado
    // (CanEquipBothAccessories/AccessoryIncompatibilityType/DualEquipArmor de wings), algo que
    // depende del estado COMPLETO y EN VIVO de los otros 6 slots del jugador real. Este resolver
    // no porta (b) aparte: el mismo resultado visual final (el ultimo item que escribe un canal
    // desplaza al anterior) ya lo produce VisiblePlayerState de forma natural cuando dos items
    // coinciden en tipo dentro del MISMO escaneo, portar (b) por separado solo duplicaria esa
    // logica sin cambiar nada visible. La parte (a) SI se porta, mas simple y ya cubierta por el
    // resto de este fichero: si el candidato favorito no resuelve a un sprite real en el canal
    // pedido (Resolve/ResolveAccessorySprite ya devuelven null para eso, "lo que no se encuentra
    // no se inventa"), no se muestra nada - mismo resultado final que el "break" real seguido de
    // "return armor[slot]" (que en ese punto es Air).
    //
    // El favorito de DYE (Player.GetEffectiveDye, mismo mecanismo, item.favorited en
    // loadout.Dyes) queda FUERA de este encargo - el hallazgo confirmado por el arquitecto habla
    // solo de GetEffectiveArmor (piezas de sprite), no de tinte; portarlo tambien habria sido
    // ampliar alcance sin evidencia nueva.
    private static PlrItemSlot ResolveEffectiveSlot(PlrItemSlot[] activeSlots, IReadOnlyList<PlrLoadout>? otherLoadouts, int index, bool vanity)
    {
        var own = activeSlots[index];
        if (!own.IsEmpty || otherLoadouts is null) return own;

        foreach (var other in otherLoadouts)
        {
            var candidate = vanity ? other.Social[index] : other.Items[index];
            if (candidate.IsEmpty || !candidate.Favorited) continue;
            return candidate;
        }
        return PlrItemSlot.Empty;
    }

    // ParidadPersonaje Fase2 (25-sep-2026): REESCRITO por completo - el modelo anterior (una
    // UNICA variable por FAMILIA -back/face/balloon-, reclasificada DESPUES de escanear TODOS los
    // items, ver el comentario que este metodo tenia hasta hoy) trataba Back/Backpack/Tail (y
    // Face/FaceHead/FaceMask/FaceFlower, y Balloon/BalloonFront) como una UNICA ranura repartida
    // entre salidas EXCLUYENTES. Bug de MODELO confirmado contra Player.cs real
    // (UpdateVisibleAccessory, Player.cs:37151-37283): son CAMPOS INDEPENDIENTES del objeto
    // Player, cualquier item puede setear el suyo sin relacion con los otros, y PUEDEN COEXISTIR -
    // "if (item.backSlot > 0) { if (...DrawInBackpackLayer) backpack = ...; else if
    // (...DrawInTailLayer) tail = ...; else { back = ...; front = -1; } }" (mismo patron exacto
    // para balloon/balloonFront, Player.cs:37232-37241, y face/faceHead/faceMask/faceFlower,
    // Player.cs:37213-37231). Prueba real que expuso el bug (arquitecto-keep, 25-sep-2026): Bee
    // Cloak (funcional, backSlot=1, Back normal) + Magic Quiver (vanidad, backSlot=7, Backpack) ->
    // el juego real muestra back=1 Y backpack=7 A LA VEZ - el resolver viejo solo podia producir
    // uno de los dos (ver VanidadDeBackpackTapaAlFuncionalDeBack_FielAlGuardado, que hasta hoy
    // esperaba erroneamente que uno tapara al otro).
    //
    // VisiblePlayerState.Apply() es ahora la MAQUINA DE ESTADOS SECUENCIAL fiel: cada item se
    // aplica UNA vez, EN SU ORDEN REAL, y solo muta los campos que el propio item declare -
    // exactamente el mismo mecanismo que Player.UpdateVisibleAccessory (un metodo, llamado una
    // vez por item, que va mutando los campos de Player uno a uno). La reclasificacion de
    // Back/Balloon/Face ya NO ocurre "al final del scan" sobre una unica variable ganadora - ocurre
    // DENTRO de Apply(), por item, exactamente como en el juego real.
    private sealed class VisiblePlayerState
    {
        public AccessoryMatch? Back, Backpack, Tail;
        public AccessoryMatch? Front;
        public AccessoryMatch? HandOn, HandOff, Waist, Shield, Neck, Shoe, Beard, Wing;
        public AccessoryMatch? Face, FaceHead, FaceMask, FaceFlower;
        public AccessoryMatch? Balloon, BalloonFront;
        // GapAnalysis Encargo J (25-sep-2026): los 4 estados especiales de bajo impacto - ver el
        // comentario de cabecera de EquippedAccessories para la cita real completa de cada uno.
        // ParidadPersonaje Fase5 (26-sep-2026): FloatingTube, el 5º estado especial del mismo tipo.
        public AccessoryMatch? UnicornHorn, AngelHalo, Yoraiz0rDarkness, Coat, FloatingTube;

        // Fiel CAMPO A CAMPO a Player.cs:37151-37283 (UpdateVisibleAccessory), EN ESE ORDEN - el
        // orden importa de verdad: backSlot se procesa ANTES que frontSlot dentro del MISMO item,
        // asi que un backSlot normal (rama "else") puede limpiar (Front = null, el "front = -1"
        // real) el Front que el propio item declare justo despues, y ese mismo frontSlot lo
        // restablece acto seguido si el item lo tiene - ver
        // FrontDeUnItemPosteriorSeAplicaTrasElBackNormalDelMismoItem_ElOrdenRealDentroDeUnMismoObjeto/
        // BackNormalDeUnItemPosteriorLimpiaElFrontDeclaradoPorUnItemAnterior_FielAlOrdenReal.
        //
        // isSitting (Player.cs:37189-37192, "if (sitting.isSitting) back = -1;") queda FUERA a
        // proposito - mismo criterio ya documentado en este fichero para Wings/velocity.Y: un doll
        // ESTATICO sin fisica no tiene el estado "sentado" de verdad, no hay nada que replicar.
        //
        // Ajuste real sobre el diseno original del arquitecto: la propuesta incluia un campo
        // "FrontExplicitlyCleared" aparte para distinguir "Front=null porque ningun item lo puso"
        // de "Front=null porque un backSlot normal lo limpio" - no hace falta: el propio Player.cs
        // NO tiene ese distingo (solo hace "front = -1"), y aqui el resultado final es identico en
        // los dos casos (Front=null), la mutacion secuencial YA basta sin ningun flag adicional -
        // simplificacion deliberada, sin perder fidelidad de comportamiento.
        public void Apply(PlrItemSlot item, PlrItemSlot dye, VanillaAccessorySlotEntry? vEntry, CalamityCatalogEntry? calEntry, bool isCalamity)
        {
            var match = new AccessoryMatch(item, dye);
            // CalamityAccesorios (25-sep-2026): el sufijo real de Calamity es el nombre CRUDO del
            // enum EquipType (HandsOn/HandsOff/Shoes/Wings, ver el comentario real ya existente en
            // ResolveAccessorySprite) - NO el alias corto que usan los campos vanilla (e.HandOn/
            // e.Shoe/e.Wing). EquipSlotSecondary cubre los 6 guantes reales con 2 canales a la vez.
            bool Cal(string suffix) => isCalamity && calEntry is not null && (calEntry.EquipSlot == suffix || calEntry.EquipSlotSecondary == suffix);

            // handOnSlot / handOffSlot (Player.cs:37161-37167)
            if (Cal("HandsOn") || (!isCalamity && vEntry?.HandOn is not null)) HandOn = match;
            if (Cal("HandsOff") || (!isCalamity && vEntry?.HandOff is not null)) HandOff = match;

            // backSlot (Player.cs:37169-37184) - reclasifica AQUI, POR ITEM, nunca al final de un
            // scan completo (ese era exactamente el bug de modelo, ver el comentario de arriba).
            // Calamity no comparte la numeracion backSlot (fiel-por-defecto, se queda siempre en
            // "Back" - mismo criterio ya establecido para el resto del resolver).
            if (isCalamity)
            {
                if (Cal("Back")) { Back = match; Front = null; }
            }
            else if (vEntry?.Back is int backId)
            {
                if (BackAccessoryLayerTable.IsBackpackLayer(backId)) Backpack = match;
                else if (BackAccessoryLayerTable.IsTailLayer(backId)) Tail = match;
                else { Back = match; Front = null; } // "front = -1" real - efecto secundario, no una regla aparte.
            }

            // frontSlot (Player.cs:37185-37188) - PUEDE restablecer el Front que el backSlot de
            // ESTE MISMO item acabe de limpiar arriba, exactamente en ese orden real.
            if (Cal("Front") || (!isCalamity && vEntry?.Front is not null)) Front = match;

            // shoeSlot (Player.cs:37193-37200) - la regla de sexo real (MaleToFemaleID) se aplica
            // DESPUES, en PlayerPreviewRenderer.Render (ya tiene "male" en scope).
            if (Cal("Shoes") || (!isCalamity && vEntry?.Shoe is not null)) Shoe = match;

            if (Cal("Waist") || (!isCalamity && vEntry?.Waist is not null)) Waist = match;
            if (Cal("Shield") || (!isCalamity && vEntry?.Shield is not null)) Shield = match;
            if (Cal("Neck") || (!isCalamity && vEntry?.Neck is not null)) Neck = match;

            // faceSlot (Player.cs:37213-37231) - reclasifica igual que backSlot, POR ITEM. Orden
            // real de comprobacion (if/else if encadenados): FaceHead primero, FaceMask despues,
            // FaceFlower al final.
            if (isCalamity)
            {
                if (Cal("Face")) Face = match;
            }
            else if (vEntry?.Face is int faceId)
            {
                if (FaceAccessoryLayerTable.IsFaceHeadLayer(faceId)) FaceHead = match;
                else if (FaceAccessoryLayerTable.IsFaceMaskLayer(faceId)) FaceMask = match;
                else if (FaceAccessoryLayerTable.IsFaceFlowerLayer(faceId)) FaceFlower = match;
                else Face = match;
            }

            // balloonSlot (Player.cs:37232-37241) - idem, reclasifica POR ITEM.
            if (isCalamity)
            {
                if (Cal("Balloon")) Balloon = match;
            }
            else if (vEntry?.Balloon is int balloonId)
            {
                if (BalloonAccessoryLayerTable.IsFrontLayer(balloonId)) BalloonFront = match;
                else Balloon = match;
            }

            // beardSlot (Player.cs:37243-37246). Calamity no declara EquipType.Beard en ningun
            // item real (ver comentario de EquippedAccessories) - "Beard" nunca hace match ahi.
            if (Cal("Beard") || (!isCalamity && vEntry?.Beard is not null)) Beard = match;
            // wingSlot (Player.cs:37247-37250, tambien Player.cs:37063-37074 en el bucle exterior
            // real - ver el comentario de ResolveAccessories mas abajo, "DELIBERATE DIFFERENCE
            // (Wings)", ya cubierto por el filtro hide[] previo a llamar a Apply()).
            if (Cal("Wings") || (!isCalamity && vEntry?.Wing is not null)) Wing = match;

            // GapAnalysis Encargo J (25-sep-2026): los 4 estados especiales, item.type EXACTO, sin
            // relacion con ningun campo de slot (Player.cs:37255-37282).
            if (item.Id == 3581) Yoraiz0rDarkness = match;
            if (item.Id == 4563) UnicornHorn = match;
            if (item.Id == 1987) AngelHalo = match;
            if (item.Id == 5587) Coat = match;
            // ParidadPersonaje Fase5 (26-sep-2026): FloatingTube (Player.cs:36345-36347, "if
            // (item.type == 4404) hasFloatingTube = true;") - mismo mecanismo item.type EXACTO.
            if (item.Id == 4404) FloatingTube = match;
        }
    }

    private readonly record struct AccessoryMatch(PlrItemSlot Item, PlrItemSlot Dye);

    // ItemIsVisuallyIncompatible, regla A2 (Player.cs:37131-37134/ArmorIDs.cs:1105): "legs > 0 &&
    // ArmorIDs.Legs.Sets.IncompatibleWithFrogLeg[legs] && item.shoeSlot == 15" - tabla real
    // transcrita LITERAL (7 legSlot reales: FlowerBoyPants=138, LamiaPants=143, MoonLordLegs=217,
    // TimelessTravelerBottom=222, CapricornTail=226, RoyalDressBottom=228, y el legSlot real de
    // Mermaid Tail=106, sin item vanilla extraible en el catalogo de este PC - ver
    // ItemIsVisuallyIncompatibleReglaA2_MermaidTailPendingItem en los tests). No hace falta una
    // clase de tabla aparte (como BackAccessoryLayerTable/BalloonAccessoryLayerTable/
    // FaceAccessoryLayerTable) porque solo la consulta ESTA regla, en un unico sitio.
    private static readonly HashSet<int> LegsIncompatibleWithFrogLeg = [106, 143, 217, 222, 226, 228, 138];

    // PortSeleccion Encargo1 (25-sep-2026): resuelve los 12 tipos de accesorio funcional/vanidad
    // reales (Waist/Neck/HandOn/HandOff/Back+Backpack+Tail/Front/Shield/Face+FaceHead+FaceMask+
    // FaceFlower/Shoes/Balloon+BalloonFront/Beard/Wing), aplicando la misma regla "vanidad tapa a
    // funcional" que Resolve() ya aplica para cabeza/cuerpo/piernas - pero por TIPO en vez de por
    // INDICE, porque los 7 huecos de accesorio (indices 3..9 de PlrLoadout.Items/Social) son
    // GENERICOS: cualquiera de ellos puede llevar cualquiera de los tipos.
    //
    // Fidelidad real al juego (Player.cs:37034-37115, UpdateVisibleAccessories): recorre PRIMERO
    // los 7 huecos funcionales en orden 3..9 (si dos items funcionales declaran el mismo tipo, el
    // de indice mas alto pisa al anterior - "ultimo en escribir gana", igual que el bucle real,
    // salvo Back/Balloon/Face que ahora se reclasifican POR ITEM, ver VisiblePlayerState.Apply) y
    // LUEGO los 7 huecos de vanidad en el mismo orden, que pisan cualquier valor funcional del
    // MISMO CANAL (no ya de la misma "familia" - Back normal y Backpack son canales distintos que
    // coexisten, ver el comentario de cabecera de VisiblePlayerState).
    //
    // GapAnalysis Encargo H (25-sep-2026): hide[] se respeta - el array de 10 bits real
    // (hideVisibleAccessory en Player.cs) vive en PlrCharacter.Loadouts[CurrentLoadout].Hide,
    // NUNCA en PlrCharacter.PrimaryLoadout (PlrLoadout.CreateEmpty(isPrimary:true) fija Hide=null
    // siempre) - el llamador es responsable de pasar el Hide REAL del loadout activo (ver
    // CharacterListEntryViewModel), este metodo solo aplica el array que recibe. Regla real: el
    // toggle SOLO gatea el hueco FUNCIONAL (i=3..9 de loadout.Items) - el bucle de vanidad
    // (armor[13..19] real, loadout.Social aqui) NO tiene ningun chequeo, la vanidad puesta se ve
    // SIEMPRE.
    //
    // ParidadPersonaje Fase2 (25-sep-2026), filtros PREVIOS a Apply() (Player.cs:37034-37086,
    // ambos bucles):
    // - IsItemSlotUnlockedAndUsable (Player.cs:12668-12693): en un doll ESTATICO de la pantalla de
    //   seleccion de personaje (equivalente real a Main.gameMenu==true), la formula real colapsa
    //   a "siempre true" salvo el slot 8/18 (case 8/18 real: "if (extraAccessory) { if
    //   (!expertMode) return gameMenu; return true; } return false;" - en gameMenu==true, el
    //   UNICO factor que cambia el resultado es extraAccessory) y a "siempre true" tambien para el
    //   slot 9/19 (case 9/19 real: "if (!masterMode) return gameMenu; return true;" - en
    //   gameMenu==true da true sin importar masterMode, exactamente lo que ya documentaba
    //   IsMasterAccessorySlot en ItemSlotViewModel.cs, "el 7º no tiene NINGUN dato al que
    //   condicionarse"). Por eso el UNICO filtro real que hace falta aqui es "slot local 8 gateado
    //   por extraAccessoryUnlocked" - nuevo parametro opcional, default true (backward-compatible
    //   con todo el resto de esta clase, que no conocia PlrCharacter.ExtraAccessory hasta hoy);
    //   los llamadores reales (MainViewModel/CharacterListEntryViewModel) pasan el valor real del
    //   personaje.
    // - ItemIsVisuallyIncompatible, reglas A (Player.cs:37117-37140) - de las 5 reglas reales solo
    //   3 son aplicables a un doll ESTATICO (las otras 2 dependen de estado de combate en tiempo
    //   real - eocDash/shieldRaised, comida sostenida - sin sentido para un doll sin fisica, fuera
    //   de alcance, igual que isSitting/velocity.Y en otros sitios de este fichero): body==96 +
    //   backSlot en DrawInTailLayer, legs con IncompatibleWithFrogLeg + shoeSlot==15, y
    //   balloonSlot==18 + body en {93,83}. Solo vanilla (Calamity no comparte backSlot/shoeSlot/
    //   balloonSlot, fiel-por-defecto). A diferencia de hide[], este filtro APLICA A LOS DOS
    //   BUCLES (funcional Y vanidad) - Player.cs llama a ItemIsVisuallyIncompatible en ambos
    //   (37059/37081), sin excepcion para vanidad.
    //
    // DELIBERATE DIFFERENCE (Wings): el juego real tiene una excepcion en el propio bucle
    // funcional (Player.cs:37063-37074) - "if (hideVisibleAccessory[i] && (velocity.Y == 0f ||
    // mount.Active)) continue;" - unas alas ocultas SI se siguen mostrando si el jugador esta
    // cayendo de verdad (velocity.Y != 0) y no esta montado. Esta app dibuja un doll ESTATICO sin
    // fisica real (siempre "en reposo", equivalente a velocity.Y==0f) - bajo esa condicion la
    // propia formula real del juego colapsa a "si esta oculto, no se ve" sin excepcion (el OR con
    // mount.Active tampoco aplica, un doll no tiene montura). Por eso Wings se resuelve aqui con
    // el MISMO chequeo generico que el resto de los tipos (hide[i] oculta el slot entero, tal
    // cual), sin replicar la rama de "cayendo" - decision explicita, no una simplificacion oculta.
    //
    // GapAnalysis Encargo I (25-sep-2026): dye REAL emparejado con el ganador de cada canal -
    // Player.cs:9691-9697/9702 (UpdateDyes/UpdateItemDye real): "int num = i % 10;
    // UpdateItemDye(i < 10, hideVisibleAccessory[num], GetEffectiveArmor(i),
    // GetEffectiveDye(num));" - el bucle real recorre los 20 slots (0..9 funcional, 10..19
    // vanidad) y usa dye[i % 10] SIEMPRE, tanto para el hueco funcional i como para su gemelo de
    // vanidad i+10 - el MISMO indice de dye sirve para los dos. Por eso cada item se aplica junto
    // al dye emparejado con SU MISMO indice de slot (loadout.Dyes[i]), sin importar si vino de
    // Items o de Social.
    // ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugD - otherLoadouts, mismo parametro y
    // mismo criterio de compatibilidad hacia atras (default null) que Resolve() - ver su
    // comentario y el de ResolveEffectiveSlot.
    public EquippedAccessories ResolveAccessories(PlrLoadout loadout, bool[]? hide = null, bool extraAccessoryUnlocked = true, IReadOnlyList<PlrLoadout>? otherLoadouts = null)
    {
        var state = new VisiblePlayerState();

        // body/legs YA resueltos (con la misma regla vanidad-tapa-a-funcional que Resolve()) -
        // hacen falta para las 3 reglas de ItemIsVisuallyIncompatible de arriba.
        int? bodySlotId = ResolveBodySlot(Visible(loadout, 1, otherLoadouts));
        int? legsSlotId = ResolveLegsSlot(Visible(loadout, 2, otherLoadouts));

        void ScanOne(PlrItemSlot s, PlrItemSlot dye, int localIndex, bool respectHide)
        {
            // IsItemSlotUnlockedAndUsable(8/18) real - ver el comentario de cabecera de este
            // metodo. Aplica a los 2 bucles por igual (funcional Y vanidad).
            if (localIndex == 8 && !extraAccessoryUnlocked) return;
            // Solo el hueco FUNCIONAL (loadout.Items) respeta hide[i] - el bucle de vanidad no
            // tiene ningun chequeo de hideVisibleAccessory real.
            if (respectHide && hide is not null && localIndex < hide.Length && hide[localIndex]) return;
            if (s.IsEmpty) return;

            bool isCalamity = s.Id >= CalamityIds.ItemIdBase;
            VanillaAccessorySlotEntry? vEntry = isCalamity ? null : _vanillaAccessorySlots.ById(s.Id);
            CalamityCatalogEntry? calEntry = isCalamity ? _calamity.BySyntheticId(s.Id) : null;

            // ItemIsVisuallyIncompatible, reglas A - ver el comentario de cabecera. Solo vanilla.
            if (!isCalamity && vEntry is not null)
            {
                if (bodySlotId == 96 && vEntry.Back is int backIdA && BackAccessoryLayerTable.IsTailLayer(backIdA)) return;
                if (legsSlotId is int legs && LegsIncompatibleWithFrogLeg.Contains(legs) && vEntry.Shoe == 15) return;
                if (vEntry.Balloon == 18 && (bodySlotId == 93 || bodySlotId == 83)) return;
            }

            state.Apply(s, dye, vEntry, calEntry, isCalamity);
        }

        // GapAnalysis BugD (26-sep-2026): cada indice pasa por ResolveEffectiveSlot ANTES de
        // llegar a ScanOne - si el hueco propio esta vacio, puede venir "prestado" de un
        // favorito de otro loadout (ver el comentario real completo en ResolveEffectiveSlot).
        // El dye (loadout.Dyes[i]) se queda SIEMPRE en el del loadout activo, sin favorito
        // propio - alcance deliberado, ver el mismo comentario.
        for (int i = 3; i <= 9; i++) ScanOne(ResolveEffectiveSlot(loadout.Items, otherLoadouts, i, vanity: false), loadout.Dyes[i], i, respectHide: true);
        for (int i = 3; i <= 9; i++) ScanOne(ResolveEffectiveSlot(loadout.Social, otherLoadouts, i, vanity: true), loadout.Dyes[i], i, respectHide: false);

        var (waistFile, waistSlotId) = ResolveAccessorySprite(state.Waist?.Item, "Waist", e => e.Waist, "acc_waist");
        var waistDye = ResolveDye(state.Waist?.Dye ?? PlrItemSlot.Empty);
        var (neckFile, neckSlotId) = ResolveAccessorySprite(state.Neck?.Item, "Neck", e => e.Neck, "acc_neck");
        var neckDye = ResolveDye(state.Neck?.Dye ?? PlrItemSlot.Empty);
        var (handOnFile, handOnSlotId) = ResolveAccessorySprite(state.HandOn?.Item, "HandsOn", e => e.HandOn, "acc_handon");
        var handOnDye = ResolveDye(state.HandOn?.Dye ?? PlrItemSlot.Empty);
        var (handOffFile, handOffSlotId) = ResolveAccessorySprite(state.HandOff?.Item, "HandsOff", e => e.HandOff, "acc_handoff");
        var handOffDye = ResolveDye(state.HandOff?.Dye ?? PlrItemSlot.Empty);
        var (shieldFile, shieldSlotId) = ResolveAccessorySprite(state.Shield?.Item, "Shield", e => e.Shield, "acc_shield");
        var shieldDye = ResolveDye(state.Shield?.Dye ?? PlrItemSlot.Empty);
        // GapAnalysis Encargo D (25-sep-2026): guarda el id MASCULINO/neutro tal cual - la
        // regla de sexo real (MaleToFemaleID) se aplica despues, en PlayerPreviewRenderer.Render.
        var (shoesFile, shoesSlotId) = ResolveAccessorySprite(state.Shoe?.Item, "Shoes", e => e.Shoe, "acc_shoes");
        var shoesDye = ResolveDye(state.Shoe?.Dye ?? PlrItemSlot.Empty);
        var (beardFile, beardSlotId) = ResolveAccessorySprite(state.Beard?.Item, "Beard", e => e.Beard, "acc_beard");
        var beardDye = ResolveDye(state.Beard?.Dye ?? PlrItemSlot.Empty);
        var (frontFile, frontSlotId) = ResolveAccessorySprite(state.Front?.Item, "Front", e => e.Front, "acc_front");
        var frontDye = ResolveDye(state.Front?.Dye ?? PlrItemSlot.Empty);
        // Wings Encargo1 (25-sep-2026): wingSlot, mismo patron exacto - calamitySuffix "Wings"
        // (nombre crudo del enum, igual que HandsOn/HandsOff/Shoes, ver scripts/
        // extraer-slot-armadura-calamity.js).
        var (wingFile, wingSlotId) = ResolveAccessorySprite(state.Wing?.Item, "Wings", e => e.Wing, "acc_wing");
        var wingDye = ResolveDye(state.Wing?.Dye ?? PlrItemSlot.Empty);

        // GapAnalysis Encargo A (25-sep-2026): Back/Backpack/Tail YA vienen reclasificados POR
        // ITEM desde VisiblePlayerState.Apply (ParidadPersonaje Fase2) - los 3 pueden estar
        // rellenos A LA VEZ, cada uno con su propio item (ver el comentario de cabecera de
        // VisiblePlayerState). Los 3 comparten el mismo sprite real (acc_back/{id}.png, e.Back).
        var (backFile, backSlotId) = ResolveAccessorySprite(state.Back?.Item, "Back", e => e.Back, "acc_back");
        var backDye = ResolveDye(state.Back?.Dye ?? PlrItemSlot.Empty);
        var (backpackFile, backpackSlotId) = ResolveAccessorySprite(state.Backpack?.Item, "Back", e => e.Back, "acc_back");
        var backpackDye = ResolveDye(state.Backpack?.Dye ?? PlrItemSlot.Empty);
        var (tailFile, tailSlotId) = ResolveAccessorySprite(state.Tail?.Item, "Back", e => e.Back, "acc_back");
        var tailDye = ResolveDye(state.Tail?.Dye ?? PlrItemSlot.Empty);

        // GapAnalysis Encargo C (25-sep-2026): Balloon/BalloonFront, idem - reclasificados POR
        // ITEM, pueden coexistir cada uno con su propio item.
        var (balloonFile, balloonSlotId) = ResolveAccessorySprite(state.Balloon?.Item, "Balloon", e => e.Balloon, "acc_balloon");
        var balloonDye = ResolveDye(state.Balloon?.Dye ?? PlrItemSlot.Empty);
        var (balloonFrontFile, balloonFrontSlotId) = ResolveAccessorySprite(state.BalloonFront?.Item, "Balloon", e => e.Balloon, "acc_balloon");
        var balloonFrontDye = ResolveDye(state.BalloonFront?.Dye ?? PlrItemSlot.Empty);

        // GapAnalysis Encargo F (25-sep-2026): Face/FaceHead/FaceMask/FaceFlower, idem.
        var (faceFile, faceSlotId) = ResolveAccessorySprite(state.Face?.Item, "Face", e => e.Face, "acc_face");
        var faceDye = ResolveDye(state.Face?.Dye ?? PlrItemSlot.Empty);
        var (faceHeadFile, faceHeadSlotId) = ResolveAccessorySprite(state.FaceHead?.Item, "Face", e => e.Face, "acc_face");
        var faceHeadDye = ResolveDye(state.FaceHead?.Dye ?? PlrItemSlot.Empty);
        var (faceMaskFile, faceMaskSlotId) = ResolveAccessorySprite(state.FaceMask?.Item, "Face", e => e.Face, "acc_face");
        var faceMaskDye = ResolveDye(state.FaceMask?.Dye ?? PlrItemSlot.Empty);
        var (faceFlowerFile, faceFlowerSlotId) = ResolveAccessorySprite(state.FaceFlower?.Item, "Face", e => e.Face, "acc_face");
        var faceFlowerDye = ResolveDye(state.FaceFlower?.Dye ?? PlrItemSlot.Empty);

        // GapAnalysis Encargo J (25-sep-2026): resuelve la ruta fija real de cada estado especial
        // (null si el item no esta puesto en ningun hueco) - ver el comentario de cabecera de
        // EquippedAccessories para la cita completa de cada uno. "lo que no se encuentra no se
        // inventa" (FixedVanillaPath ya comprueba File.Exists), mismo criterio que el resto del
        // resolver.
        string? unicornHornFile = state.UnicornHorn is not null ? FixedVanillaPath("extra", 143) : null;
        var unicornHornDye = ResolveDye(state.UnicornHorn?.Dye ?? PlrItemSlot.Empty);
        string? angelHaloFile = state.AngelHalo is not null ? FixedVanillaPath("acc_face", 7) : null;
        var angelHaloDye = ResolveDye(state.AngelHalo?.Dye ?? PlrItemSlot.Empty);
        string? yoraiz0rDarknessFile = state.Yoraiz0rDarkness is not null ? FixedVanillaPath("extra", 67) : null;
        string? coatFile = state.Coat is not null ? FixedVanillaPath("armor_body", 251) : null;
        int? coatSlotId = state.Coat is not null ? 251 : null;
        var coatDye = ResolveDye(state.Coat?.Dye ?? PlrItemSlot.Empty);
        // ParidadPersonaje Fase5 (26-sep-2026): FloatingTube - misma tira 40x112 (2 filas), la
        // eleccion de fila (0 o 1) es responsabilidad del renderer (PlayerPreviewRenderer.Render),
        // aqui solo se resuelve la ruta fija real del fichero, mismo criterio que el resto.
        string? floatingTubeFile = state.FloatingTube is not null ? FixedVanillaPath("extra", 105) : null;
        var floatingTubeDye = ResolveDye(state.FloatingTube?.Dye ?? PlrItemSlot.Empty);

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
            coatFile, coatSlotId, coatDye,
            floatingTubeFile, floatingTubeDye);
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
