using System.Text.Json;

namespace Terrakeep.Core.Data;

// Tope real de apilado por id de objeto vanilla - extraido directamente de los bloques
// SetDefaults1..5 de Item.cs (decompilado real de tModLoader, ver
// scripts/extraer-max-stack-vanilla.py en la raiz del repo), nunca un numero fijo inventado.
//
// Encargo Keep 25-sep-2026 ("+10/+100/MAX en el editor de objeto, respetando el maxStack real"):
// este limite llevaba APARCADO desde el 2-sep-2026 (ver el comentario real de L-f en
// ContainerViewModel.cs, "Ordenar") por falta exactamente de esta extraccion - nunca se habia
// hecho hasta ahora.
//
// `Terraria.Item.maxStack` se resetea a 1 en cada ResetStats(Type) real del motor (Item.cs, XML
// doc del propio campo: "Defaults to 1.") y solo se reasigna quirurgicamente en el bloque
// SetDefaultsN del id concreto que de verdad apila mas de 1 unidad - por eso este catalogo solo
// GUARDA lo que aparece asignado de verdad (igual que VanillaItemStatsCatalog), y `Get` devuelve
// 1 para cualquier id ausente: no es un valor de repuesto arbitrario, es el default real del
// propio motor para un objeto sin apilado especial (armas, armaduras, equipables...).
public sealed class VanillaMaxStackCatalog
{
    private readonly Dictionary<int, int> _byId;

    private VanillaMaxStackCatalog(Dictionary<int, int> byId) => _byId = byId;

    // 1 = default real del motor (ver el comentario de cabecera) - nunca null, un "no se
    // encontro dato real" aqui SIEMPRE tiene una respuesta real y correcta (1), a diferencia de
    // VanillaItemStatsCatalog.Get (donde null distingue "no es un arma" de "arma con damage=0").
    public int Get(int itemId) => _byId.TryGetValue(itemId, out var maxStack) ? maxStack : 1;

    public static VanillaMaxStackCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaMaxStackCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(stream)
            ?? throw new InvalidDataException("vanilla_max_stack.json invalido.");
        var byId = new Dictionary<int, int>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaMaxStackCatalog(byId);
    }
}
