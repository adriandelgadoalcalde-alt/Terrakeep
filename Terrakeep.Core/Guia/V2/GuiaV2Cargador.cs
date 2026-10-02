using System.Reflection;
using System.Text.Json;

namespace Terrakeep.Core.Guia.V2;

// Carga de la guia v2. Los datos van INCRUSTADOS en Terrakeep.Core.dll (EmbeddedResource
// Guia/V2/Datos/*.json, ver Terrakeep.Core.csproj): Terrakeep y TerrakeepMod leen exactamente los
// mismos bytes, sin copias ni scripts de sincronizacion (a diferencia de la v1).
public static class GuiaV2Cargador
{
    public const string RecursoReferencias = "guia_v2_referencias.json";

    /// <summary>Nombre del recurso incrustado de una guia ("calamity" -> guia_v2_calamity.json).</summary>
    public static string RecursoGuia(string id) => $"guia_v2_{id}.json";

    private static readonly JsonSerializerOptions Opciones = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Ids de guia incrustados en esta build ("calamity", "vanilla"...).</summary>
    public static IReadOnlyList<string> GuiasDisponibles()
    {
        const string prefijo = "guia_v2_";
        return typeof(GuiaV2Cargador).Assembly.GetManifestResourceNames()
            .Select(NombreCorto)
            .Where(n => n.StartsWith(prefijo, StringComparison.Ordinal) && n != RecursoReferencias)
            .Select(n => n[prefijo.Length..^".json".Length])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    public static GuiaV2Doc CargarGuiaIncrustada(string id)
    {
        using var s = AbrirRecurso(RecursoGuia(id))
            ?? throw new InvalidDataException($"La guia v2 \"{id}\" no esta incrustada en Terrakeep.Core.");
        return CargarGuia(s);
    }

    public static GuiaV2Referencias CargarReferenciasIncrustadas()
    {
        using var s = AbrirRecurso(RecursoReferencias)
            ?? throw new InvalidDataException("Falta la tabla de referencias de la guia v2 en Terrakeep.Core.");
        return CargarReferencias(s);
    }

    public static GuiaV2Doc CargarGuia(Stream s) =>
        JsonSerializer.Deserialize<GuiaV2Doc>(s, Opciones) ?? throw new InvalidDataException("Documento de guia v2 vacio.");

    public static GuiaV2Referencias CargarReferencias(Stream s) =>
        JsonSerializer.Deserialize<GuiaV2Referencias>(s, Opciones) ?? throw new InvalidDataException("Referencias de guia v2 vacias.");

    private static Stream? AbrirRecurso(string nombreCorto)
    {
        Assembly a = typeof(GuiaV2Cargador).Assembly;
        string? completo = a.GetManifestResourceNames().FirstOrDefault(n => NombreCorto(n) == nombreCorto);
        return completo == null ? null : a.GetManifestResourceStream(completo);
    }

    // Los recursos se incrustan con LogicalName = nombre de archivo (ver csproj), pero se tolera
    // tambien el nombre largo por espacio de nombres por si una build lo genera asi.
    private static string NombreCorto(string recurso)
    {
        int i = recurso.LastIndexOf("guia_v2_", StringComparison.Ordinal);
        return i >= 0 ? recurso[i..] : recurso;
    }
}
