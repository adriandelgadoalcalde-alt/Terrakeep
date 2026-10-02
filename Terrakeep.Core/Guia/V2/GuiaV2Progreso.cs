using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Guia.V2;

// Progreso MANUAL de la guia v2, por personaje: las casillas que marca el jugador (tareas sin
// condicion evaluable, o evaluables que quiere dar por hechas), las paradas marcadas/aplazadas y
// la clase elegida. Persistido, nunca solo en memoria (encargo del usuario, 02-oct-2026).
//
// Donde se guarda lo decide cada app (Core no conoce rutas de usuario):
//   - Terrakeep (escritorio): un .json por personaje con ArchivoProgresoGuia (abajo).
//   - TerrakeepMod: dentro del propio personaje (ModPlayer.SaveData), serializando este mismo
//     objeto con GuiaV2ProgresoJson.Serializar - asi el progreso viaja con el .plr/.tplr.
public sealed class GuiaV2ProgresoManual
{
    [JsonPropertyName("formato")] public string Formato { get; set; } = "terrakeep-guia-v2-progreso";
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    /// <summary>Guia a la que pertenece ("vanilla"/"calamity").</summary>
    [JsonPropertyName("guia")] public string Guia { get; set; } = "";
    /// <summary>Clase elegida (snake_case de ClaseGuia) o vacio = sin elegir.</summary>
    [JsonPropertyName("clase")] public string Clase { get; set; } = "";
    [JsonPropertyName("tareas")] public Dictionary<string, bool> Tareas { get; set; } = [];
    [JsonPropertyName("paradasHechas")] public Dictionary<string, bool> ParadasHechas { get; set; } = [];
    [JsonPropertyName("paradasAplazadas")] public Dictionary<string, bool> ParadasAplazadas { get; set; } = [];
    [JsonPropertyName("actualizado")] public string Actualizado { get; set; } = "";

    public bool TareaMarcada(string id) => Tareas.TryGetValue(id, out bool v) && v;
    public bool ParadaMarcada(string id) => ParadasHechas.TryGetValue(id, out bool v) && v;
    public bool ParadaAplazada(string id) => ParadasAplazadas.TryGetValue(id, out bool v) && v;

    public void MarcarTarea(string id, bool hecha) { if (hecha) Tareas[id] = true; else Tareas.Remove(id); }

    public void MarcarParada(string id, bool hecha)
    {
        if (hecha) { ParadasHechas[id] = true; ParadasAplazadas.Remove(id); }
        else ParadasHechas.Remove(id);
    }

    public void AplazarParada(string id, bool aplazada) { if (aplazada) ParadasAplazadas[id] = true; else ParadasAplazadas.Remove(id); }
}

public static class GuiaV2ProgresoJson
{
    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public static string Serializar(GuiaV2ProgresoManual progreso) => JsonSerializer.Serialize(progreso, Opciones);

    /// <summary>Nunca lanza: un texto ilegible o de otro formato devuelve un progreso vacio
    /// (y <paramref name="valido"/> = false para que la app pueda avisar y no sobrescribirlo).</summary>
    public static GuiaV2ProgresoManual Deserializar(string? json, string guia, out bool valido)
    {
        valido = false;
        if (string.IsNullOrWhiteSpace(json)) { valido = true; return new GuiaV2ProgresoManual { Guia = guia }; }
        try
        {
            var p = JsonSerializer.Deserialize<GuiaV2ProgresoManual>(json);
            if (p == null || p.Formato != "terrakeep-guia-v2-progreso") return new GuiaV2ProgresoManual { Guia = guia };
            p.Tareas ??= [];
            p.ParadasHechas ??= [];
            p.ParadasAplazadas ??= [];
            if (string.IsNullOrEmpty(p.Guia)) p.Guia = guia;
            valido = true;
            return p;
        }
        catch (JsonException)
        {
            return new GuiaV2ProgresoManual { Guia = guia };
        }
    }
}

/// <summary>Persistencia en disco para el escritorio: un archivo por (guia, personaje).</summary>
public static class ArchivoProgresoGuia
{
    /// <summary>Nombre de archivo estable y seguro para un personaje (nombre del .plr sin
    /// extension, saneado) - nunca la ruta completa del usuario.</summary>
    public static string NombreArchivo(string guia, string clavePersonaje)
    {
        var limpio = new string(clavePersonaje.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
        if (limpio.Length == 0) limpio = "personaje";
        return $"guia-{guia}-{limpio}.json";
    }

    public static GuiaV2ProgresoManual Cargar(string carpeta, string guia, string clavePersonaje)
    {
        string ruta = Path.Combine(carpeta, NombreArchivo(guia, clavePersonaje));
        string? json = File.Exists(ruta) ? File.ReadAllText(ruta) : null;
        return GuiaV2ProgresoJson.Deserializar(json, guia, out _);
    }

    /// <summary>Escritura atomica (temporal + reemplazo): un corte a mitad no deja el archivo roto.</summary>
    public static void Guardar(string carpeta, string clavePersonaje, GuiaV2ProgresoManual progreso)
    {
        Directory.CreateDirectory(carpeta);
        string ruta = Path.Combine(carpeta, NombreArchivo(progreso.Guia, clavePersonaje));
        progreso.Actualizado = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        string temporal = ruta + ".tmp";
        File.WriteAllText(temporal, GuiaV2ProgresoJson.Serializar(progreso));
        File.Move(temporal, ruta, overwrite: true);
    }
}
