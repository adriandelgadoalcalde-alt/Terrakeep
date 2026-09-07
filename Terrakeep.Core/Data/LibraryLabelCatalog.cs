using System.Text.Json;
using System.Text.RegularExpressions;

namespace Terrakeep.Core.Data;

// Traductor real de los nombres de carpeta de la Libreria (namespace "lib.item" de
// Terrasavr.es-ES.json real - ver scripts/extraer-etiquetas-libreria-es.js) - replica EXACTO
// el algoritmo real de `ha.prototype.updateLang` (script.js real, clase base app.Shelf de la
// que heredan los nodos Dir/Items del arbol de la Libreria): 4 casos reales, en este orden,
// antes de caer a una traduccion directa por nombre completo:
// 1. Rango numerico puro ("1-40", "401-440"...) -> nunca se traduce, se deja tal cual.
// 2. "Page N" -> traduce la plantilla "Page $1" y sustituye el numero real.
// 3. "Pages N+" -> traduce la plantilla "Pages $1+" y sustituye el numero real.
// 4. Cualquier nombre terminado en "(N)" (ej. "Melee damage (316)") -> traduce la plantilla
//    "<prefijo> ($1)" y sustituye el numero real - asi "Melee damage (316)" sale
//    "Daño de Cuerpo a Cuerpo (316)", no una plantilla sin traducir.
// Lo que no tiene traduccion real se queda en ingles (mismo criterio de todo el proyecto:
// nunca se inventa), no hace falta un caso aparte para eso - la propia tabla ya trae el
// nombre ingles como valor por defecto para todo lo que Terrasavr real tampoco traduce.
public sealed class LibraryLabelCatalog
{
    private static readonly Regex RxPage = new(@"^Page (\d+)$");
    private static readonly Regex RxPages = new(@"^Pages (\d+)\+$");
    private static readonly Regex RxAuto = new(@"^(.+?)\((\d+)\)$");
    private static readonly Regex RxNum = new(@"^\d+-\d+$");

    private readonly Dictionary<string, string> _labels;

    private LibraryLabelCatalog(Dictionary<string, string> labels) => _labels = labels;

    public string Translate(string englishName)
    {
        if (englishName.Length == 0 || englishName == "(< 0)" || RxNum.IsMatch(englishName))
            return englishName;

        var mPage = RxPage.Match(englishName);
        if (mPage.Success)
            return Lookup("Page $1").Replace("$1", mPage.Groups[1].Value);

        var mPages = RxPages.Match(englishName);
        if (mPages.Success)
            return Lookup("Pages $1+").Replace("$1", mPages.Groups[1].Value);

        var mAuto = RxAuto.Match(englishName);
        if (mAuto.Success)
            return Lookup(mAuto.Groups[1].Value + "($1)").Replace("$1", mAuto.Groups[2].Value);

        return Lookup(englishName);
    }

    private string Lookup(string key) => _labels.TryGetValue(key, out var value) ? value : key;

    public static LibraryLabelCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static LibraryLabelCatalog LoadFromStream(Stream stream)
    {
        var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_library_labels_es.json invalido.");
        return new LibraryLabelCatalog(labels);
    }
}
