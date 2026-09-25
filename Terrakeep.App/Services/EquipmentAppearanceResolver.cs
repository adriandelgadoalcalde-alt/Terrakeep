using System.IO;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
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
// esta activo.
//
// ALCANCE DELIBERADO, documentado y no oculto: no respeta los 3 bytes de "ocultar equipo"
// del panel de vanidad del juego real (HideVisual1/HideVisual2/HideMisc en PlrCharacter) -
// el bit exacto que le corresponde a cada slot dentro de esos bytes no se investigo a fondo
// (no compensaba el riesgo de esconder o mostrar la pieza equivocada por una lectura de bit
// erronea). Un personaje que use ese toggle poco frecuente vera su pieza dibujada aunque el
// juego real la esconda - hueco real, ya conocido, no un bug silencioso.
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
public sealed record EquippedAccessories(
    string? WaistFile, string? NeckFile, string? HandOnFile, string? HandOffFile,
    string? BackFile, string? ShieldFile, string? FaceFile,
    int? WaistSlot = null, int? NeckSlot = null, int? HandOnSlot = null, int? HandOffSlot = null,
    int? BackSlot = null, int? ShieldSlot = null, int? FaceSlot = null);

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
            ResolveLegsSlot(legsSlot));
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
    // Mismo ALCANCE DELIBERADO ya documentado en Resolve(): no respeta hideVisibleAccessory
    // (el toggle de "ocultar" del panel de vanidad, Hide[] en PlrLoadout) - hueco real, ya
    // conocido, no un bug silencioso.
    public EquippedAccessories ResolveAccessories(PlrLoadout loadout)
    {
        PlrItemSlot? waist = null, neck = null, handOn = null, handOff = null, back = null, shield = null, face = null;

        void Scan(PlrItemSlot[] slots)
        {
            for (int i = 3; i <= 9; i++)
            {
                var s = slots[i];
                if (s.IsEmpty) continue;
                if (IsAccessoryType(s, e => e.Waist, "Waist")) waist = s;
                if (IsAccessoryType(s, e => e.Neck, "Neck")) neck = s;
                if (IsAccessoryType(s, e => e.HandOn, "HandOn")) handOn = s;
                if (IsAccessoryType(s, e => e.HandOff, "HandOff")) handOff = s;
                if (IsAccessoryType(s, e => e.Back, "Back")) back = s;
                if (IsAccessoryType(s, e => e.Shield, "Shield")) shield = s;
                if (IsAccessoryType(s, e => e.Face, "Face")) face = s;
            }
        }
        Scan(loadout.Items);
        Scan(loadout.Social);

        var (waistFile, waistSlotId) = ResolveAccessorySprite(waist, "Waist", e => e.Waist, "acc_waist");
        var (neckFile, neckSlotId) = ResolveAccessorySprite(neck, "Neck", e => e.Neck, "acc_neck");
        var (handOnFile, handOnSlotId) = ResolveAccessorySprite(handOn, "HandOn", e => e.HandOn, "acc_handon");
        var (handOffFile, handOffSlotId) = ResolveAccessorySprite(handOff, "HandOff", e => e.HandOff, "acc_handoff");
        var (backFile, backSlotId) = ResolveAccessorySprite(back, "Back", e => e.Back, "acc_back");
        var (shieldFile, shieldSlotId) = ResolveAccessorySprite(shield, "Shield", e => e.Shield, "acc_shield");
        var (faceFile, faceSlotId) = ResolveAccessorySprite(face, "Face", e => e.Face, "acc_face");

        return new EquippedAccessories(
            waistFile, neckFile, handOnFile, handOffFile, backFile, shieldFile, faceFile,
            waistSlotId, neckSlotId, handOnSlotId, handOffSlotId, backSlotId, shieldSlotId, faceSlotId);
    }

    // Un item real de Terraria solo declara UNO de los 7 campos de accesorio en la practica,
    // pero nada impide comprobar varios a la vez (igual que VanillaAccessorySlotEntry puede en
    // teoria traer mas de una clave rellena) - ver el comentario de esa clase.
    private bool IsAccessoryType(PlrItemSlot slot, Func<VanillaAccessorySlotEntry, int?> vanillaPick, string calamitySuffix)
    {
        if (slot.Id >= CalamityIds.ItemIdBase)
        {
            var entry = _calamity.BySyntheticId(slot.Id);
            return entry?.EquipSlot == calamitySuffix;
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
            if (entry is null || entry.EquipSlot != calamitySuffix) return (null, null);
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
