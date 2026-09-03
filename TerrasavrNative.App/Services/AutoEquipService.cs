using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// Auditoria de Opus, Bloque 6 (T-20): "Auto-equipar" vivia como un metodo mas de
// MainViewModel - extraido aqui, sin cambiar el comportamiento, para que MainViewModel se
// quede solo con la orquestacion (StatusMessage, cambio de pestaña) y esto con la regla de
// negocio en si.
public static class AutoEquipService
{
    public readonly record struct Result(int Placed, int Skipped);

    // Arma+armadura+accesorios de una clase/etapa concreta se colocan directamente en el
    // personaje cargado. Armadura -> los 3 primeros slots del equipo puesto (cabeza/cuerpo/
    // piernas), accesorios -> los 5 siguientes (Items[3..7] del loadout, ver
    // PROYECTO-TERRASAVR.md); las armas no tienen slot fijo en Terraria, se colocan en el
    // primer hueco libre del inventario. Objetos que no se consigan resolver (pid no
    // encontrado en el catalogo) o para los que no quede hueco se cuentan aparte - nunca se
    // sobrescribe un objeto ya puesto salvo en los 3+5 slots fijos de armadura/accesorios, que
    // es justo lo que este boton promete reemplazar.
    public static Result Apply(BuildClassGear gear, EquipmentGroupViewModel equipmentGroup,
        ContainerViewModel inventoryContainer, CharacterFileService service)
    {
        var armorSlots = equipmentGroup.EquippedItems.Slots;
        var inventorySlots = inventoryContainer.Slots;
        int placed = 0, skipped = 0;

        void PlaceInSlot(ItemSlotViewModel slot, BuildItemRef itemRef)
        {
            var resolved = BuildItemResolver.Resolve(itemRef, service.VanillaCatalog, service.CalamityCatalog, service.VanillaPrefixCatalog);
            if (resolved == null) { skipped++; return; }
            slot.UpdateFrom(resolved);
            placed++;
        }

        for (int i = 0; i < gear.Armor.Count && i < 3; i++)
            PlaceInSlot(armorSlots[i], gear.Armor[i]);

        for (int i = 0; i < gear.Accessories.Count && i < 5; i++)
            PlaceInSlot(armorSlots[3 + i], gear.Accessories[i]);

        foreach (var weapon in gear.Weapons)
        {
            var emptySlot = inventorySlots.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot == null) { skipped++; continue; }
            PlaceInSlot(emptySlot, weapon);
        }

        return new Result(placed, skipped);
    }
}
