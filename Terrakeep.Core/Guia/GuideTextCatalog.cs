using System.Text.Json;
using Terrakeep.Core.Data;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): diccionario plano clave->texto ("Guia.Paso.XXX.Titulo", "Guia.Req.
// CristalesVida"...) generado por scripts/sync-guia-desde-terrakeepmod.ps1 a partir de los
// hjson REALES de TerrakeepMod. Mismo criterio de idioma que LocalizedContent (español es la
// referencia, ingles cae a español si falta una clave) y mismo criterio de "no inventar" que
// LocalizationService de la App (una clave que falte en los DOS idiomas se ve literal entre
// corchetes, nunca en blanco).
public sealed class GuideTextCatalog
{
    private readonly Dictionary<string, string> _es;
    private readonly Dictionary<string, string> _en;

    private GuideTextCatalog(Dictionary<string, string> es, Dictionary<string, string> en)
    {
        _es = es;
        _en = en;
    }

    public static GuideTextCatalog LoadFromFiles(string rutaEs, string rutaEn)
    {
        return new GuideTextCatalog(Cargar(rutaEs), Cargar(rutaEn));
    }

    private static Dictionary<string, string> Cargar(string ruta)
    {
        if (!File.Exists(ruta)) return [];
        using var stream = File.OpenRead(ruta);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    }

    public string Text(string key, string language)
    {
        var activo = language == LocalizedContent.English ? _en : _es;
        if (activo.TryGetValue(key, out var v)) return v;
        if (_es.TryGetValue(key, out var vEs)) return vEs;
        return $"[{key}]";
    }

    public string Format(string key, string language, params object?[] args)
        => string.Format(Text(key, language), args);
}
