using System.Globalization;
using System.Text;

namespace Terrakeep.App.ViewModels;

// Gramatica real de busqueda de Terrasavr - calco de app.TabLibrary.search
// (reference/terrasavr-real/script.beautified.js, lineas ~4001-4055), Fase 4 del rediseño de
// la Libreria (octava pasada, consulta a Opus). Compartida entre LibraryViewModel (objetos) y
// BuffLibraryViewModel (buffs), que no tienen un tipo comun para sus entradas - se le pasan
// los campos ya resueltos en vez de un objeto generico.
//
// Reglas reales (verificadas leyendo el propio codigo, no de memoria):
// - Terminos separados por COMA = OR entre ellos (cualquiera que case, entra el resultado).
// - Dentro de un termino, palabras separadas por ESPACIO = AND (todas tienen que aparecer).
// - Un termino de menos de 2 caracteres (tras recortar espacios) se ignora por completo -
//   quirk real de Terrasavr, un "5" suelto no cuenta como busqueda por id ni por nombre.
// - "#123" = por id exacto. "#100-200" = por rango de id (ambos extremos incluidos).
// - ".texto" busca en el tooltip/descripcion en vez de en el nombre (textLq real vs nameLq).
// - Sin prefijo especial: busca por nombre (subcadena simple, igual que el "indexOf" real).
public static class LibrarySearchGrammar
{
    // C-09 (informe de pulido final, cierra L2): "buscar mascara no encuentra máscara" - la
    // gramatica ya normalizaba mayusculas fuera de Matches (los dos lados en minusculas antes de
    // comparar); el mismo sitio sirve para los diacriticos. FormD descompone cada caracter
    // acentuado en base+marca (a+´), se descarta la marca (NonSpacingMark) y se recompone en
    // FormC - enfoque estandar, mas barato en el bucle de Matches (:36, words.All(target.
    // Contains)) que CompareInfo.IndexOf con IgnoreNonSpace, que obligaria a reescribirlo entero.
    // Solo pliega el lado de la COMPARACION - lo que se muestra en pantalla nunca pasa por aqui.
    public static string Fold(string s)
    {
        string d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (char c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static bool Matches(string query, int id, string nameLq, string? textLq)
    {
        foreach (string rawTerm in query.Split(','))
        {
            string term = Fold(rawTerm.Trim());
            if (term.Length < 2) continue;

            if (term[0] == '#')
            {
                if (MatchesId(term[1..], id)) return true;
                continue;
            }

            bool searchTooltip = term[0] == '.';
            string target = searchTooltip ? textLq ?? string.Empty : nameLq;
            string body = searchTooltip ? term[1..] : term;
            string[] words = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0 && words.All(target.Contains)) return true;
        }
        return false;
    }

    private static bool MatchesId(string idPart, int id)
    {
        int dash = idPart.IndexOf('-');
        if (dash < 0)
            return int.TryParse(idPart, out int exact) && id == exact;

        return int.TryParse(idPart[..dash], out int lo)
            && int.TryParse(idPart[(dash + 1)..], out int hi)
            && id >= lo && id <= hi;
    }
}
