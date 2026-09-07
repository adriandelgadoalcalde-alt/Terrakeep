using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;

namespace Terrakeep.App.Services;

// Envoltorio de la app de escritorio sobre Terrakeep.Core.Data.BuffTreeBuilder - el arbol
// de carpetas REAL de Terrasavr para la Libreria de buffs (6 categorias curadas de initLibs()
// real + indice paginado + "Calamity (mod)").
//
// WS2 de TerrakeepMod (6-sep-2026): toda la logica (las 6 listas literales de ids portadas de
// initLibs(), el indice de 33 en 33, el agrupado/paginado de Calamity y las etiquetas en
// español) se ha movido tal cual a Core, que no depende de WPF y compila tambien para net8.0.
// Aqui solo queda lo que SI es de la app de escritorio: resolver el icono a una ruta
// "pack://siteoforigin:,,," real y convertir CategoryTreeNodeData -> CategoryNodeViewModel
// (misma conversion que ya usa el arbol de objetos, ver LibraryCategoryTreeBuilder.ToViewModel).
public static class BuffLibraryTreeBuilder
{
    public static List<CategoryNodeViewModel> Build(CharacterFileService service)
    {
        var data = BuffTreeBuilder.BuildBuffTree(
            service.VanillaBuffs, service.CalamityBuffCatalog, service.CalamityCatalog,
            id => ResolveBuffIconPath(service, id),
            id => LibraryCategoryTreeBuilder.ResolveIconPath(service, id));
        return data.Select(LibraryCategoryTreeBuilder.ToViewModel).ToList();
    }

    // Mismo criterio que ResolveIconPath para objetos: por debajo de CalamityIds.BuffIdBase es
    // un buff vanilla real (PNG por id en Assets/vanilla/buff_icons); por encima, un id
    // sintetico del catalogo de buffs de Calamity (nombre de fichero real dentro de
    // Assets/calamity/buff_icons).
    private static string? ResolveBuffIconPath(CharacterFileService service, int id) =>
        id >= CalamityIds.BuffIdBase
            ? service.CalamityBuffCatalog.BySyntheticId(id)?.Icon is { } icon
                ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + icon
                : null
            : VanillaBuffIconResolver.GetIconPath(id);
}
