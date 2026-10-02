using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Terrakeep.Core.Guia.V2;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.Guia;

// Guia v2 (F2b, 02-oct-2026): gate de IDIOMA sobre el contenido incrustado. Nace de un hallazgo real
// en las capturas de F2/F3: la escalera de Calamity pintaba «Armadura · Silver armor» (la plata es
// vanilla y tiene nombre oficial, «de plata»). Lo que se comprueba, en las DOS guias:
//   1. Cada rotulo de conjunto de armadura es «Armadura ...», sin marcado ni la palabra «armor», y
//      cada palabra suya sale de los nombres OFICIALES es-ES de las piezas de ese grupo (nunca
//      inventada) - y ninguna es una palabra inglesa de esas piezas que su nombre oficial no lleve.
//   2. Ningun texto de la guia contiene, como texto suelto, el nombre ingles de un objeto o NPC de la
//      tabla de referencias cuyo nombre es-ES es distinto (es decir, que SI tiene traduccion oficial).
//      Los que no tienen traduccion (es == en, p. ej. muchos de Calamity) no cuentan: se quedan como
//      estan hasta que exista una traduccion oficial.
public class GuiaV2IdiomaTests(ITestOutputHelper salida)
{
    private static readonly Lazy<GuiaV2Referencias> Refs = new(GuiaV2Cargador.CargarReferenciasIncrustadas);
    private static readonly HashSet<string> Enlaces = ["de", "del", "la", "las", "los", "el", "para"];

    [Theory]
    [MemberData(nameof(Guias))]
    public void RotulosDeConjunto_SalenDeLasPiezasOficialesYSinIngles(string id)
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada(id);
        var errores = new List<string>();
        int grupos = 0;
        foreach (var escalera in doc.Escaleras)
            foreach (var etapa in escalera.Etapas)
                foreach (var g in etapa.Armadura.Where(a => !string.IsNullOrEmpty(a.Conjunto)).GroupBy(a => a.Conjunto))
                {
                    grupos++;
                    string rotulo = g.Key;
                    string donde = $"{etapa.Id} «{rotulo}»";
                    if (!rotulo.StartsWith("Armadura ", StringComparison.Ordinal)) errores.Add($"{donde}: no empieza por «Armadura »");
                    if (rotulo.Contains('{') || rotulo.Contains('[')) errores.Add($"{donde}: lleva marcado de enlace");
                    if (Regex.IsMatch(rotulo, @"\barmou?r\b", RegexOptions.IgnoreCase)) errores.Add($"{donde}: lleva «armor»");

                    var piezas = g.Select(o => Refs.Value.Objetos.GetValueOrDefault(o.Ref)).Where(r => r != null).ToList();
                    if (piezas.Count == 0) { errores.Add($"{donde}: ninguna pieza con nombre en la tabla"); continue; }
                    var es = piezas.Select(p => Plegar(p!.Es)).ToList();
                    var en = piezas.Select(p => Plegar(p!.En)).ToList();
                    foreach (string palabra in rotulo.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
                    {
                        string w = Plegar(palabra);
                        if (Enlaces.Contains(w)) continue;
                        string raiz = w.Length > 3 ? w[..^1] : w;
                        bool enEs = es.Any(n => Palabras(n).Any(x => x.StartsWith(raiz, StringComparison.Ordinal)));
                        // Una palabra inglesa de las piezas («Silver») no esta en sus nombres oficiales
                        // («Casco de plata»): este mismo control la caza; si coincide en los dos («Astral»,
                        // «Aerospec», «tiki») es que el nombre oficial la conserva.
                        if (!enEs)
                            errores.Add($"{donde}: «{palabra}» no sale de ningun nombre oficial de sus piezas ({string.Join(" / ", piezas.Select(p => p!.Es))})"
                                + (en.Any(n => Palabras(n).Contains(w)) ? " - es la palabra inglesa de las piezas" : ""));
                    }
                }
        salida.WriteLine($"{id}: {grupos} grupos de armadura revisados");
        foreach (var e in errores.Take(60)) salida.WriteLine(e);
        Assert.True(grupos > 0, $"la guia '{id}' no tiene ningun grupo de armadura con conjunto");
        Assert.True(errores.Count == 0, $"{errores.Count} rotulos de conjunto mal en la guia '{id}' (detalle en la salida).");
    }

    /// <summary>Nombres ingleses que NO cuentan como fuga aunque tengan un objeto homonimo traducido,
    /// cada uno con su motivo real.</summary>
    private static readonly Dictionary<string, string> Excepciones = new(StringComparer.Ordinal)
    {
        // Es el nombre del mod (la espada «Calamity» -> «Calamidad» es otra cosa).
        ["Calamity"] = "nombre del mod",
    };

    // Claves cuyo valor no es texto que se enseñe traducido: ids, enlaces y fuentes (paginas de la
    // wiki en ingles a proposito), firmas de tiles para situar zonas (nombres internos). Las refs
    // sueltas («Terraria/X») se saltan por su forma, en cualquier clave.
    private static readonly HashSet<string> ClavesNoTexto =
        ["ref", "refs", "id", "desde", "fuentes", "url", "pagina", "wiki", "firma", "icono", "tipo", "capitulo", "clase",
         "clases", "esquema", "ambito", "marcas", "bandera", "clave", "creditos", "referencia", "estilo", "punto"];

    [Theory]
    [MemberData(nameof(Guias))]
    public void Textos_SinNombresInglesesQueTenganTraduccionOficial(string id)
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada(id);
        var raiz = JsonSerializer.SerializeToNode(doc)!;

        // Indice por primera palabra: nombre ingles -> es, solo los que tienen traduccion distinta.
        var porPrimera = new Dictionary<string, List<(string En, string Es)>>(StringComparer.Ordinal);
        void Indexar(string en, string es)
        {
            if (string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(es) || en == es || en.Length < 5) return;
            if (Excepciones.ContainsKey(en)) return;
            string primera = en.Split(' ')[0];
            if (!porPrimera.TryGetValue(primera, out var l)) porPrimera[primera] = l = [];
            l.Add((en, es));
        }
        foreach (var o in Refs.Value.Objetos.Values) Indexar(o.En, o.Es);
        foreach (var n in Refs.Value.Npcs.Values) Indexar(n.En, n.Es);

        var fugas = new List<string>();
        int textos = 0;
        void Recorrer(JsonNode? nodo, string ruta)
        {
            switch (nodo)
            {
                case JsonObject obj:
                    foreach (var (k, v) in obj) if (!ClavesNoTexto.Contains(k)) Recorrer(v, ruta + "." + k);
                    break;
                case JsonArray arr:
                    for (int i = 0; i < arr.Count; i++) Recorrer(arr[i], $"{ruta}[{i}]");
                    break;
                case JsonValue val when val.TryGetValue<string>(out var s):
                    if (string.IsNullOrEmpty(s) || s.StartsWith("http", StringComparison.Ordinal) || Regex.IsMatch(s, @"^(Terraria|CalamityMod)/\w+$")) return;
                    textos++;
                    string texto = Regex.Replace(s, @"\{[a-z]:[^}]*\}", " ");
                    foreach (Match m in Regex.Matches(texto, @"\p{L}[\p{L}'’\-]*"))
                    {
                        if (!porPrimera.TryGetValue(m.Value, out var candidatos)) continue;
                        foreach (var (en, es) in candidatos.OrderByDescending(c => c.En.Length))
                        {
                            if (string.CompareOrdinal(texto, m.Index, en, 0, en.Length) != 0) continue;
                            int fin = m.Index + en.Length;
                            if (fin < texto.Length && char.IsLetter(texto[fin])) continue;
                            fugas.Add($"{ruta}: «{en}» (oficial: «{es}») en «{Recortar(s)}»");
                            break;
                        }
                    }
                    break;
            }
        }
        Recorrer(raiz, id);
        salida.WriteLine($"{id}: {textos} textos revisados");
        foreach (var f in fugas.Take(80)) salida.WriteLine(f);
        Assert.True(textos > 1000, $"la guia '{id}' tiene demasiados pocos textos revisados ({textos}): ¿ha cambiado el modelo?");
        Assert.True(fugas.Count == 0, $"{fugas.Count} nombres en ingles con traduccion oficial en la guia '{id}' (detalle en la salida).");
    }

    public static TheoryData<string> Guias()
    {
        var d = new TheoryData<string>();
        foreach (var g in GuiaV2Cargador.GuiasDisponibles()) d.Add(g);
        return d;
    }

    private static string Recortar(string s) => s.Length <= 140 ? s : s[..140] + "…";

    private static IEnumerable<string> Palabras(string s) => s.Split([' ', '-', '\'', '’', '(', ')', ','], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Minusculas sin tildes (para comparar «Estragón» con «estragon» de forma estable).</summary>
    private static string Plegar(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
