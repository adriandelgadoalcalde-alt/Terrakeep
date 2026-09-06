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

    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026). Idioma activo del proceso, que
    // LocalizationService.SetLanguage mantiene sincronizado con el de la interfaz.
    //
    // Por que un estatico y no un parametro en cada llamada: los catalogos de contenido
    // (VanillaItemCatalog.GetName, TileNameCatalog.TileName, NpcNameCatalog.GetName...) se
    // consultan desde DECENAS de sitios reales - tarjetas de la Libreria, tooltips de slot,
    // arbol de Investigacion, resultados del buscador del mundo, tooltip del mapa, Builds...
    // Meter un parametro de idioma en cada firma obligaria a tocar y a acordarse de TODOS, y
    // olvidarse de uno solo no da error de compilacion: da un nombre en español colado en la
    // interfaz inglesa, justo el bug que esta ronda viene a cerrar. Con el idioma vivo aqui, el
    // catalogo responde en el idioma activo estes donde estes, y quien necesite un idioma
    // concreto (los tests) usa las sobrecargas que lo reciben explicito.
    //
    // El refresco EN PANTALLA lo sigue haciendo el mecanismo que ya existia: cada ViewModel
    // suscrito a PropertyChanged("Item[]") de LocalizationService relee sus textos.
    public static string CurrentLanguage { get; set; } = Spanish;

    // Version que usa el idioma activo del proceso - el caso normal de todo consumidor de
    // catalogo que no tenga un idioma concreto que imponer.
    public static string Pick(string? es, string? en) => Pick(es, en, CurrentLanguage);

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
