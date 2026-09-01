using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.Core.Model;

// Puente entre el slot crudo del .plr (PlrItemSlot, formato de cable - solo entiende un byte
// de prefijo vanilla) y el item enriquecido que usa el resto de la app (GameItem, con
// ItemPrefix que SI puede representar un prefijo real de Calamity). Un GameItem de Calamity
// SIEMPRE se enmascara a PlrItemSlot.Empty al convertir hacia el .plr - su identidad real solo
// vive en el .tplr, nunca en el archivo vanilla (ver Calamity/CalamityCharacterSync.cs).
public static class GameItemConversions
{
    public static GameItem ToGameItem(this PlrItemSlot slot) => new()
    {
        Id = slot.Id,
        Count = slot.Count,
        Prefix = slot.IsEmpty || slot.Prefix == 0 ? ItemPrefix.None : ItemPrefix.Vanilla(slot.Prefix),
        Favorited = slot.Favorited,
    };

    // Efecto secundario INTENCIONADO: cualquier item vacio (Id==0) se canoniza a
    // PlrItemSlot.Empty, perdiendo cualquier count/prefix residual de un "slot fantasma" que
    // el PlrItemSlot original pudiera llevar - esto es exactamente la limpieza de slots
    // fantasma que ya hace la version JS en cada guardado (ver el historial de bugs en
    // Terrasavr-Calamity-Beta\README.md, punto 5: "se limpia automaticamente en cada
    // guardado"), no una perdida de datos accidental. La preservacion byte a byte de esa
    // basura SOLO importa a nivel de formato puro (ver PlrItemSlot.Read/PlrFileRealCharacter
    // Tests), no al pasar por esta capa de aplicacion.
    public static PlrItemSlot ToPlrItemSlot(this GameItem item) =>
        item.IsEmpty || item.IsCalamity
            ? PlrItemSlot.Empty
            : new PlrItemSlot(item.Id, item.Count, item.Prefix.VanillaId, item.Favorited);

    public static GameItem[] ToGameItems(this PlrItemSlot[] slots)
    {
        var result = new GameItem[slots.Length];
        for (int i = 0; i < slots.Length; i++) result[i] = slots[i].ToGameItem();
        return result;
    }

    public static PlrItemSlot[] ToPlrItemSlots(this GameItem[] items)
    {
        var result = new PlrItemSlot[items.Length];
        for (int i = 0; i < items.Length; i++) result[i] = items[i].ToPlrItemSlot();
        return result;
    }
}
