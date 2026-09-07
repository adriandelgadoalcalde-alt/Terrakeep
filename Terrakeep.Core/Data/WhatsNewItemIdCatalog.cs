using System.Text.Json;

namespace Terrakeep.Core.Data;

// C-16 (informe de pulido final, cierra media N1): catalogo SEPARADO y de solo lectura, nombre
// interno -> id, SOLO para los objetos vanilla de version 1.4.5+ (por encima del maximo real de
// vanilla_item_ids_by_key.json) que la pestaña Novedades necesita mostrar con sprite real.
//
// Decision de diseño explicita (no ampliar vanilla_item_ids_by_key.json): ese catalogo alimenta
// la Libreria, la Investigacion y el buscador del personaje, y Terrakeep edita guardados de
// tModLoader 1.4.4.9, donde estos ids NO existen - colocar uno en un slot produciria un objeto
// invalido en el .plr del usuario. Este catalogo lo consume UNICAMENTE WhatsNewItemViewModel.
// ForVanilla (scripts/extraer-ids-novedades-vanilla.py, fuente real: TerrariaVanilla/Terraria/
// ID/ItemID.cs).
public sealed class WhatsNewItemIdCatalog
{
    private readonly IReadOnlyDictionary<string, int> _byKey;

    private WhatsNewItemIdCatalog(IReadOnlyDictionary<string, int> byKey) => _byKey = byKey;

    public int? GetIdByKey(string internalName) => _byKey.TryGetValue(internalName, out int id) ? id : null;

    public static WhatsNewItemIdCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static WhatsNewItemIdCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(stream)
            ?? throw new InvalidDataException("whats_new_item_ids.json invalido.");
        return new WhatsNewItemIdCatalog(raw);
    }
}
