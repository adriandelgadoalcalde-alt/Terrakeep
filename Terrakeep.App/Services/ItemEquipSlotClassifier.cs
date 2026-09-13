using Terrakeep.Core.Model;

namespace Terrakeep.App.Services;

// Clasificacion REAL de "en que ranura se equipa este objeto" - filtro nuevo de la Libreria
// (encargo del usuario, 13-sep-2026: "amplia la busqueda con filtros combinables... si es
// equipable en que slot"). Distinto a proposito de ItemSlotViewModel.AcceptsItem: aquel resuelve
// "puedo soltar este objeto en ESTE slot restringido" con la regla "desconocido = permitir" (para
// no bloquear monturas/mascotas/tintes reales de Calamity, que no tienen ningun campo real que
// consultar) - una regla correcta para validar un drop, pero que aqui produciria un falso
// positivo: un filtro "Accesorio" marcado NO debe enseñar pociones o materiales de Calamity solo
// porque "no se sabe que no lo es". Aqui la regla es la contraria, "desconocido = None" (no
// entra en ninguna ranura), igual de honesta pero con el signo correcto para una clasificacion
// positiva en vez de una validacion permisiva.
//
// Cobertura real: vanilla usa VanillaSlotKindCatalog (extraido de Item.cs/Projectile.cs/Main.cs/
// DyeInitializer.cs decompilados, ~70-85% real segun su propio comentario - un id ausente cae en
// None, nunca se inventa una ranura). Calamity SOLO tiene dato real utilizable para
// Accesorio/Armadura (CalamityCatalogEntry.Category + EquipSlot, el mismo campo real que ya usa
// ItemSlotViewModel.AcceptsItem) - el resto de ranuras (municion/moneda/tinte/gancho/montura/
// vagoneta/mascotas) no tiene ningun campo real en el catalogo de Calamity, asi que un objeto de
// Calamity NUNCA aparece bajo esos filtros: ausencia de dato, no ausencia real confirmada.
public static class ItemEquipSlotClassifier
{
    public static SlotKind Classify(int itemId, bool isCalamity, CharacterFileService service)
    {
        if (!isCalamity) return service.VanillaSlotKinds.GetKind(itemId);

        var entry = service.CalamityCatalog.BySyntheticId(itemId);
        string? category = entry?.Category;
        if (category == null) return SlotKind.None;

        if (category.StartsWith("Accessories", StringComparison.Ordinal))
            return SlotKind.Accessory;

        if (category.StartsWith("Armor", StringComparison.Ordinal))
        {
            return entry!.EquipSlot switch
            {
                "Head" => SlotKind.ArmorHead,
                "Body" => SlotKind.ArmorBody,
                "Legs" => SlotKind.ArmorLegs,
                _ => SlotKind.None, // pieza mal encajada en "Armor/..." (ver H3-11) - no se finge una ranura
            };
        }

        return SlotKind.None; // arma/pocion/material real de Calamity - nunca es armadura ni accesorio
    }
}
