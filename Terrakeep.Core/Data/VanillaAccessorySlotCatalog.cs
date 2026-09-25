using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Indice real de sprite de ACCESORIO por id de objeto vanilla - PortSeleccion Encargo1
// (25-sep-2026), hermano directo de VanillaArmorSlotCatalog (mismo criterio, extendido a los
// 7 tipos de accesorio funcional/vanidad: waist/neck/handOn/handOff/back/shield/face -
// ampliado despues a shoe/balloon/beard, ver comentarios de GapAnalysis Encargo D/C/G mas abajo).
// Fuente real: scripts/extraer-slots-accesorios-vanilla.py, valores LITERALES de
// waistSlot/neckSlot/handOnSlot/handOffSlot/backSlot/shieldSlot/faceSlot/shoeSlot de Item.cs
// (decompilado real) - el indice que Terraria usa para cargar Acc_Waist_N.xnb/Acc_Neck_N.xnb/
// Acc_HandsOn_N.xnb/Acc_HandsOff_N.xnb/Acc_Back_N.xnb/Acc_Shield_N.xnb/Acc_Face_N.xnb/
// Acc_Shoes_N.xnb. Los sprites en si ya estan extraidos (sin recortar, hoja XNB->PNG real) en
// Assets/player/acc_{waist,neck,handon,handoff,back,shield,face,shoes}/{N}.png (scripts/
// extraer-sprites-accesorios-vanilla.js) - este catalogo solo resuelve QUE numero le
// corresponde a cada objeto y de QUE TIPO es, nunca el propio sprite.
//
// GapAnalysis Encargo D (25-sep-2026): shoeSlot (campo "sh") es el accesorio REAL de zapatos
// (Player.cs:37193-37200, "if (item.shoeSlot > 0) shoe = item.shoeSlot;") - un canal
// COMPLETAMENTE DISTINTO de los zapatos BASE de la piel/pantalon del personaje (ya portados
// como parte de legskin/pants/shoes en PlayerPreviewRenderer, ver el comentario de cabecera de
// PlayerPreviewRenderer.cs). La regla de sexo real (ArmorIDs.Shoe.Sets.MaleToFemaleID) se
// aplica en PlayerPreviewRenderer.Render (mismo patron que SetMatchHead/headIdAfterSetMatch),
// no aqui - este catalogo solo guarda el id MASCULINO/neutro tal cual lo declara el objeto.
//
// A diferencia de VanillaArmorSlotCatalog (donde el TIPO ya lo decide la posicion 0/1/2 del
// loadout), aqui el TIPO lo decide el propio objeto (confirmado en Player.cs real,
// UpdateVisibleAccessory: "if (item.waistSlot > 0) waist = item.waistSlot;" y analogo para
// los otros 6 campos, sin relacion con en cual de los 7 huecos genericos del loadout este
// puesto) - por eso esta entrada puede tener MAS de un campo relleno en teoria (un objeto que
// fuera a la vez, por ejemplo, accesorio de cuello Y de espalda), aunque en la practica cada
// accesorio real de Terraria solo asigna uno.
public sealed class VanillaAccessorySlotEntry
{
    [JsonPropertyName("w")] public int? Waist { get; init; }
    [JsonPropertyName("n")] public int? Neck { get; init; }
    [JsonPropertyName("ho")] public int? HandOn { get; init; }
    [JsonPropertyName("hf")] public int? HandOff { get; init; }
    [JsonPropertyName("bk")] public int? Back { get; init; }
    [JsonPropertyName("s")] public int? Shield { get; init; }
    [JsonPropertyName("fc")] public int? Face { get; init; }
    [JsonPropertyName("sh")] public int? Shoe { get; init; }
    // GapAnalysis Encargo C (25-sep-2026): balloonSlot real (Player.cs:37232-37241) - 9º tipo
    // de accesorio, mismo patron exacto que los anteriores. Ver BalloonAccessoryLayerTable
    // para la clasificacion real Balloon/BalloonFront de este mismo valor.
    [JsonPropertyName("bl")] public int? Balloon { get; init; }
    // GapAnalysis Encargo G (25-sep-2026): beardSlot real (Player.cs:37243-37246, "if
    // (item.beardSlot > 0) beard = item.beardSlot;") - 10º tipo de accesorio, EXACTAMENTE 4
    // objetos vanilla reales en todo el juego (GingerBeard=1, WilsonBeardShort=2,
    // WilsonBeardLong=3, WilsonBeardMagnificent=4 - ArmorIDs.Beard.cs). A diferencia de los 9
    // anteriores, scripts/extraer-slots-accesorios-vanilla.py transcribe estos 4 valores a mano
    // (BEARD_MANUAL) en vez de usar el escaner generico - ver el comentario real de ese script
    // para la justificacion completa (switch anidado + fallthrough con expresion no literal).
    [JsonPropertyName("bd")] public int? Beard { get; init; }
    // GapAnalysis Encargo E (25-sep-2026): frontSlot real (Player.cs:37185-37188, "if
    // (item.frontSlot > 0) front = item.frontSlot;") - 11º tipo de accesorio, 11 objetos
    // vanilla reales en total (CrimsonCloak/MysteriousCape/RedCape/WinterCape/ManaCloak/
    // HunterCloak/PrinceCape/ShimmerCloak/ChippysWings/LunasCloak/DruidicSerpentCloak). A
    // diferencia de la mayoria de los anteriores, 7 de los 11 SI entran por el escaner generico
    // - los otros 4 (CrimsonCloak..WinterCape, valor via expresion "1 + type - 2284") y otros 3
    // mas (ChippysWings/LunasCloak/DruidicSerpentCloak, dentro de un switch anidado) se
    // transcriben a mano (FRONT_MANUAL/FRONT_MANUAL_NESTED en el script) - ver el comentario
    // real de extraer-slots-accesorios-vanilla.py para la justificacion completa.
    [JsonPropertyName("fr")] public int? Front { get; init; }
    // Wings Encargo1 (25-sep-2026): wingSlot real (Player.cs, UpdateVisibleAccessory: "if
    // (item.wingSlot > 0) wings = item.wingSlot;" - mismo patron exacto que los 11 anteriores).
    // 12º tipo de accesorio, el indice que Terraria usa para cargar Wings_N.xnb - a diferencia de
    // los otros 11 (Acc_X_N.xnb), el prefijo de fichero real de Wings NO lleva "Acc_" (confirmado
    // en Terraria.Initializers.AssetInitializer.cs real: "TextureAssets.Wings[num] =
    // LoadAsset<Texture2D>("Images/Wings_" + num, ...)"). 46 de los 51 sprites reales (Wings_1..
    // Wings_51.xnb existen los 51 en la instalacion de Steam) tienen un item vanilla real que los
    // asigna en la version de Item.cs que usa este catalogo (tModLoader 1.4.4.9, la misma que el
    // resto del proyecto - ver CLAUDE.md) - wingSlot 47-51 NO estan asignados a ningun item real
    // en esa version (confirmado con un escaneo completo de "wingSlot = " en Item.cs, ningun
    // literal ni expresion llega a 47+), asi que hoy son inalcanzables desde un .plr real de este
    // catalogo - limite real, documentado, no oculto (WingDrawTable SI modela su anchor/divisor
    // real, por si el catalogo de items se amplia en el futuro o un .plr editado a mano los usa).
    [JsonPropertyName("wg")] public int? Wing { get; init; }
}

public sealed class VanillaAccessorySlotCatalog
{
    private readonly Dictionary<int, VanillaAccessorySlotEntry> _byItemId;

    private VanillaAccessorySlotCatalog(Dictionary<int, VanillaAccessorySlotEntry> byItemId) => _byItemId = byItemId;

    public VanillaAccessorySlotEntry? ById(int itemId) => _byItemId.TryGetValue(itemId, out var entry) ? entry : null;

    public static VanillaAccessorySlotCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaAccessorySlotCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, VanillaAccessorySlotEntry>>(stream)
            ?? throw new InvalidDataException("vanilla_accessory_slots.json invalido.");
        var byId = new Dictionary<int, VanillaAccessorySlotEntry>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaAccessorySlotCatalog(byId);
    }
}
