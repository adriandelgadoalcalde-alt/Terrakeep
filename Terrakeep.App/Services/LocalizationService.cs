using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Terrakeep.App.Services;

// Pedido explicito del usuario (5-sep-2026): "de momento solo daremos soporte ingles y español
// de forma nativa" - infraestructura real de idioma, primera pieza del bloque 1 (arquitectura),
// el resto de la app se va migrando texto a texto en bloques posteriores (decision explicita del
// usuario: trocear en vez de hacerlo todo de un tiron).
//
// Diseño: diccionario plano clave->texto por idioma (Assets/strings_es.json/strings_en.json,
// mismo criterio real ya establecido en el proyecto para TODO catalogo de datos - JSON +
// LoadFromFile, nunca .resx/satelite). Cambio de idioma EN VIVO sin reiniciar (pedido explicito
// del usuario) via el indexador de este objeto (this[string]) + PropertyChanged("Item[]") - WPF
// reconoce ese nombre especial como "cualquier binding con Path=[clave] sobre este objeto debe
// volver a preguntar", sea cual sea la clave real - efecto real de refresco global sin escribir
// ningun MarkupExtension propio ni tocar cada binding individualmente.
//
// Español es el idioma DE REFERENCIA (siempre completo, es el idioma original del proyecto) -
// una clave que falte en ingles cae a español (nunca una clave en bruto ilegible ni texto
// inventado); una clave que falte en LOS DOS se ve literal (mismo criterio ya establecido en el
// proyecto: "lo que no se encuentra no se inventa" - un string en crudo entre corchetes es un
// bug MUY facil de detectar a simple vista, silencioso rellenaria mal sin que nadie lo note).
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public const string Spanish = "es";
    public const string English = "en";

    private Dictionary<string, string> _es = [];
    private Dictionary<string, string> _en = [];
    private Dictionary<string, string> _active = [];

    public string Language { get; private set; } = Spanish;

    private LocalizationService()
    {
        _es = LoadDictionary(Spanish);
        _en = LoadDictionary(English);
        _active = _es;
        Terrakeep.Core.Data.LocalizedContent.CurrentLanguage = Language;
    }

    private static Dictionary<string, string> LoadDictionary(string language)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", $"strings_{language}.json");
            if (!File.Exists(path)) return [];
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
        }
        catch (Exception)
        {
            return []; // fichero ausente/corrupto - cae al idioma de referencia via el indexador, nunca revienta el arranque
        }
    }

    public void SetLanguage(string language)
    {
        string normalizado = language == English ? English : Spanish; // cualquier valor desconocido cae a español, el idioma de referencia
        if (normalizado == Language) return;
        Language = normalizado;
        _active = Language == English ? _en : _es;
        // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): los catalogos de contenido
        // viven en Core y no pueden ver esta clase (Core compila tambien para net8.0/el mod), asi
        // que el idioma activo se les empuja aqui - unico punto real de cambio de idioma de toda
        // la app. Sin esta linea los nombres de objeto/NPC/tile/buff se quedarian en español.
        Terrakeep.Core.Data.LocalizedContent.CurrentLanguage = Language;
        // "Item[]" (no una clave concreta) - convencion real de WPF para "cualquier binding
        // indexado sobre este objeto, revisalo todo otra vez", exactamente lo que hace falta
        // para refrescar CUALQUIER control ya en pantalla sin conocerlos uno a uno desde aqui.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] =>
        _active.TryGetValue(key, out string? valor) ? valor
        : _es.TryGetValue(key, out string? valorEspañol) ? valorEspañol
        : $"[{key}]";

    // Para texto con valores reales incrustados (StatusMessage/errores con datos del propio
    // personaje/mundo) - la plantilla vive en el diccionario con marcadores {0}/{1}/... de
    // string.Format de toda la vida, nunca concatenacion manual (evita depender del orden real
    // de las palabras, que cambia entre idiomas).
    public string Format(string key, params object?[] args) => string.Format(this[key], args);
}
