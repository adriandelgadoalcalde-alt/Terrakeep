using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Auditoria de Opus, Bloque 2 (R-1): cuanto hace falta sacrificar de verdad de cada objeto para
// investigarlo del todo en Modo Viaje - fuente real: scripts/extraer-recuentos-investigacion.py
// (Terraria.GameContent.Creative.Content.Sacrifices.tsv, el TSV embebido real de tModLoader).
// Antes "Investigar todo" escribia un conteo fijo inventado (9999) para TODO objeto y la UI no
// podia mostrar ningun "x/N" real por no conocer N. Solo vanilla - Calamity no define su propia
// tabla de investigacion en el TSV real (usa la vainilla via herencia de ModItem si el mod no
// la sobrescribe, no investigado a fondo esta pasada), Get() devuelve null para esos.
public sealed class VanillaResearchCountCatalog
{
    private readonly Dictionary<int, int> _byId;

    private VanillaResearchCountCatalog(Dictionary<int, int> byId) => _byId = byId;

    public int? Get(int itemId) => _byId.TryGetValue(itemId, out var count) ? count : null;

    public static VanillaResearchCountCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaResearchCountCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(stream)
            ?? throw new InvalidDataException("vanilla_research_counts.json invalido.");
        var byId = new Dictionary<int, int>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaResearchCountCatalog(byId);
    }
}
