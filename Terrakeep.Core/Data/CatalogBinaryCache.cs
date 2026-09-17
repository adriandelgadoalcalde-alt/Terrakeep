using System.Security.Cryptography;
using System.Text;

namespace Terrakeep.Core.Data;

// Cache persistente de catalogos en disco (17-sep-2026, "cache de catalogos", ver bitacora.md):
// medido de verdad antes de tocar nada (arnes Terrakeep.App.Tests, ARRANQUE_SOLO=1, 3 muestras
// reales en esta maquina) que CharacterFileService() (50 JSON, ~4.6MB) tarda 115-119ms en el
// PRIMER arranque de un proceso nuevo y solo 44-45ms en una segunda instancia YA en el mismo
// proceso (CompareViewModel construye su propio CharacterFileService independiente - necesario,
// ver el comentario real de CompareViewModel, NO es un bug) - la diferencia real es sobre todo
// coste de tipo/reflexion de System.Text.Json la primera vez que ve cada forma generica
// (Dictionary<int,string>, List<CalamityCatalogEntryData>...) en el proceso, no E/S de disco.
// De ese total, los dos catalogos que de verdad forman "la Libreria de objetos (8000+)" (pedido
// explicito) miden CalamityCatalog.LoadFromFile (catalog.json, 954KB) 27-29ms en frio/5-6ms en
// caliente, y VanillaItemCatalog.LoadFromFile (4 JSON, ~730KB) 10-11ms en frio/8ms en caliente -
// coste real y evitable, a diferencia de LibraryTreeBuilder.BuildItemTree (el algoritmo de
// agrupar/paginar los 8469 objetos en si), que ya media solo 15-18ms (limite real, no compensa
// cachearlo aparte del propio arbol) - ver LibraryCatalogDiskCache para el porque se cachean los
// TRES juntos (los dos catalogos + el arbol ya construido) en un unico fichero.
//
// Formato propio muy simple (BinaryWriter/BinaryReader sobre primitivas, NUNCA JSON de nuevo) a
// proposito: releer JSON desde un "cache" seria pagar el mismo coste real de tipo/reflexion que
// se quiere evitar. Nunca es la unica fuente de verdad: LibraryCatalogDiskCache.TryLoad cae
// siempre a la carga real por JSON si la cache no existe, esta corrupta, o su huella no coincide
// - ver el comentario real de esa clase.
internal static class CatalogBinaryCache
{
    // Huella real de un conjunto de ficheros fuente: nombre + tamaño + fecha de modificacion UTC
    // real (en ticks) de cada uno, nunca un TTL arbitrario - si CUALQUIERA de los ficheros fuente
    // cambia (una traduccion nueva, un catalog.json regenerado tras actualizar Calamity...), la
    // huella cambia sola y la cache se descarta en la siguiente apertura sin que nadie tenga que
    // borrarla a mano. Solo el NOMBRE del fichero entra en la huella (no la ruta completa) para
    // que la cache siga siendo valida si la app se reinstala en otra carpeta.
    public static string ComputeFingerprint(IEnumerable<string> sourceFiles)
    {
        var sb = new StringBuilder();
        foreach (var path in sourceFiles.OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal))
        {
            var info = new FileInfo(path);
            sb.Append(Path.GetFileName(path)).Append('|')
              .Append(info.Exists ? info.Length : -1L).Append('|')
              .Append(info.Exists ? info.LastWriteTimeUtc.Ticks : -1L).Append(';');
        }
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }

    internal static void WriteNullableString(BinaryWriter w, string? value)
    {
        w.Write(value != null);
        if (value != null) w.Write(value);
    }

    internal static string? ReadNullableString(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;

    internal static void WriteNullableInt(BinaryWriter w, int? value)
    {
        w.Write(value.HasValue);
        if (value.HasValue) w.Write(value.Value);
    }

    internal static int? ReadNullableInt(BinaryReader r) => r.ReadBoolean() ? r.ReadInt32() : null;

    internal static void WriteNullableDouble(BinaryWriter w, double? value)
    {
        w.Write(value.HasValue);
        if (value.HasValue) w.Write(value.Value);
    }

    internal static double? ReadNullableDouble(BinaryReader r) => r.ReadBoolean() ? r.ReadDouble() : null;

    internal static void WriteDict(BinaryWriter w, Dictionary<int, string> dict)
    {
        w.Write(dict.Count);
        foreach (var (k, v) in dict) { w.Write(k); w.Write(v); }
    }

    internal static Dictionary<int, string> ReadIntStringDict(BinaryReader r)
    {
        int n = r.ReadInt32();
        var dict = new Dictionary<int, string>(n);
        for (int i = 0; i < n; i++) dict[r.ReadInt32()] = r.ReadString();
        return dict;
    }

    internal static void WriteDict(BinaryWriter w, Dictionary<string, string> dict)
    {
        w.Write(dict.Count);
        foreach (var (k, v) in dict) { w.Write(k); w.Write(v); }
    }

    internal static Dictionary<string, string> ReadStringStringDict(BinaryReader r)
    {
        int n = r.ReadInt32();
        var dict = new Dictionary<string, string>(n);
        for (int i = 0; i < n; i++) dict[r.ReadString()] = r.ReadString();
        return dict;
    }

    internal static void WriteDict(BinaryWriter w, Dictionary<string, int> dict)
    {
        w.Write(dict.Count);
        foreach (var (k, v) in dict) { w.Write(k); w.Write(v); }
    }

    internal static Dictionary<string, int> ReadStringIntDict(BinaryReader r)
    {
        int n = r.ReadInt32();
        var dict = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++) dict[r.ReadString()] = r.ReadInt32();
        return dict;
    }
}
