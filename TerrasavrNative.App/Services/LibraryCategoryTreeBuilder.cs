using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// T-G (segunda auditoria de Opus, Fable): "arranque sincrono - compartir una unica instancia
// del arbol de Libreria entre Libreria e Investigacion" - medido de verdad antes de tocar nada
// (mismo criterio que X-7): LibraryViewModel Y ResearchViewModel llamaban cada uno a Build()
// por separado, recorriendo/agrupando/paginando el mismo catalogo real de ~8469 objetos DOS
// VECES en cada arranque (~134ms medidos para el conjunto de sub-viewmodels de MainViewModel,
// con este doble trabajo real dentro). La parte cara (agrupar Calamity por categoria, paginar
// hojas >40, construir el arbol vanilla real) se calcula UNA SOLA VEZ aqui (cacheada en
// _cachedData - valido durante toda la vida del proceso, los catalogos de CharacterFileService
// son inmutables tras cargarse) como datos puros sin estado (CategoryTreeNodeData, sin
// ObservableObject ni comandos); Build() se queda con el mismo contrato de siempre (devuelve un
// arbol de CategoryNodeViewModel FRESCO e independiente en cada llamada - Libreria e
// Investigacion necesitan su propio IsSelected/SelectCommand por nodo, no pueden compartir las
// instancias de ViewModel en si) pero ahora solo hace el envoltorio barato (copiar referencias
// ya calculadas), no la reconstruccion entera.
//
// WS2 de TerrakeepMod (6-sep-2026): TODO el algoritmo real (arbol vanilla, agrupado/paginado de
// Calamity, las 121 etiquetas en español) se ha movido tal cual a
// TerrasavrNative.Core.Data.LibraryTreeBuilder, que no depende de WPF y compila tambien para
// net8.0 - asi el mod de tModLoader puede construir exactamente el mismo arbol. Aqui solo queda
// la parte que SI es de la app de escritorio: la cache compartida, la resolucion de icono a una
// ruta "pack://siteoforigin:,,," real y la conversion a CategoryNodeViewModel.
public static class LibraryCategoryTreeBuilder
{
    // Bug real de concurrencia evitado a proposito (mismo motivo real que la cache de
    // PlayerPreviewRenderer.Cache, ver bitacora.md "carrera de compilacion en paralelo"): xunit
    // corre clases de test en PARALELO por defecto, y muchas construyen su propio MainViewModel
    // (-> CharacterFileService -> Library/ResearchViewModel -> Build()) a la vez - un simple
    // `??=` sin lock podria arrancar BuildData() dos veces a la vez o publicar un _cachedData a
    // medio construir. El lock solo protege el check-y-set (barato); envolver TODO el metodo
    // desharia la ganancia real de compartir el trabajo.
    private static readonly object _cacheLock = new();
    private static List<CategoryTreeNodeData>? _cachedData;

    public static List<CategoryNodeViewModel> Build(CharacterFileService service)
    {
        List<CategoryTreeNodeData> data;
        lock (_cacheLock)
            data = _cachedData ??= LibraryTreeBuilder.BuildItemTree(
                service.VanillaLibraryTree, service.LibraryLabels, service.CalamityCatalog,
                id => ResolveIconPath(service, id));
        return data.Select(ToViewModel).ToList();
    }

    // Unica forma real de resolver el icono de una carpeta en la app de escritorio: por debajo
    // de CalamityIds.ItemIdBase es un objeto vanilla real (PNG por id en Assets/vanilla/icons);
    // por encima, un id sintetico del catalogo de Calamity (nombre de fichero real dentro de
    // Assets/calamity/icons). En el mod no habra ids sinteticos - ModContent.ItemType<T>() da el
    // id real - y esta funcion sera otra completamente distinta, por eso entra inyectada.
    internal static string? ResolveIconPath(CharacterFileService service, int id) =>
        id >= CalamityIds.ItemIdBase
            ? service.CalamityCatalog.BySyntheticId(id)?.Icon is { } icon
                ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + icon
                : null
            : VanillaIconResolver.GetIconPath(id);

    internal static CategoryNodeViewModel ToViewModel(CategoryTreeNodeData data)
    {
        var vm = new CategoryNodeViewModel(data.Name, data.FullPath)
        {
            IconPath = data.IconPath,
            ItemIdsOrdered = data.ItemIdsOrdered, // misma lista inmutable de referencia, nunca se muta despues de construida
            ItemIdSet = data.ItemIdSet,
            ItemCount = data.ItemIdSet.Count,
        };
        foreach (var child in data.Children)
            vm.Children.Add(ToViewModel(child));
        return vm;
    }
}
