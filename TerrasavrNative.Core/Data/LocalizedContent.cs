namespace TerrasavrNative.Core.Data;

// Ronda de idioma del 6-sep-2026. Regla UNICA de eleccion de idioma para los catalogos de
// CONTENIDO bilingue (registro de cambios del editor y Novedades del juego/Calamity), calcada a
// proposito del criterio que LocalizationService ya aplica a la interfaz:
//
//   español es el idioma de REFERENCIA (siempre completo) -> si falta la version inglesa de una
//   entrada concreta se cae a la española, NUNCA a texto vacio ni inventado.
//
// Vive en Core (no en App) porque los propios catalogos viven aqui y no pueden depender de
// LocalizationService, que es de la capa de escritorio: quien llama pasa el idioma activo. Core
// compila tambien para net8.0 (TerrakeepMod), asi que aqui no puede entrar nada de WPF.
public static class LocalizedContent
{
    // Mismo par de codigos reales que LocalizationService.Spanish/English - repetidos aqui a
    // proposito (Core no ve la capa App) y comprobados por un test que los cruza.
    public const string Spanish = "es";
    public const string English = "en";

    public static string Pick(string? es, string? en, string language)
        => language == English
            ? (string.IsNullOrWhiteSpace(en) ? es ?? string.Empty : en)
            : (string.IsNullOrWhiteSpace(es) ? en ?? string.Empty : es);

    // Una lista inglesa VACIA significa "sin traducir" (no "esta version no añadio nada"): se
    // cae entera a la española. Se decide lista a lista, no elemento a elemento - mezclar los
    // dos idiomas dentro de una misma vista seria peor que quedarse en el de referencia.
    public static IReadOnlyList<string> PickList(
        IReadOnlyList<string>? es, IReadOnlyList<string>? en, string language)
        => language == English
            ? (en is { Count: > 0 } ? en : es ?? [])
            : (es is { Count: > 0 } ? es : en ?? []);
}
