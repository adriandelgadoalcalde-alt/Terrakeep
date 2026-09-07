using System.IO;

namespace Terrakeep.App.Services;

// Icono real de un TILE colocado - Assets/vanilla/tile_icons/, extraido de Images/Tiles_{id}.xnb
// real de la instalacion de Steam componiendo el objeto CELDA A CELDA (el hueco de 2px de la hoja
// de sprites es transparente puro, no edge bleed - un recorte rectangular meteria una costura por
// el centro; ver scripts/extraer-iconos-tiles.js y ESPEC-sprites-botones-badges.md#A.3).
//
// Dos formas de pedirlo, deliberadamente distintas:
//   - GetIconPath(tipo)      -> el frame base (0,0) del tipo. Lo que quieren Minerales y
//                               Objetos->Tiles, que agrupan por Type sin importar la variante.
//   - GetIconPath(tipo,u,v)  -> la variante EXACTA de sprite (u,v en pixeles reales del .wld,
//                               el mismo par que WorldPresenceIndex.ChestKindCounts). Solo hay
//                               variantes extraidas de los 3 tipos contenedores reales
//                               (21/467 cofres, 88 comodas) - cualquier otro (u,v) cae al icono
//                               base del tipo, y si tampoco lo hay, a null.
//
// null = no hay sprite real para ese id (tiles de mods, ids mas nuevos que el catalogo de TEdit,
// o los 5 tiles cuyo frame base sale 100% transparente: las 4 gotas y el gotero de centelleo).
// La UI cae entonces al cuadradito de color de la paleta real del mapa, como hasta ahora.
public static class TileIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "tile_icons");

    public static string? GetIconPath(int tileType)
    {
        string file = Path.Combine(IconsDir, $"{tileType}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/tile_icons/" + tileType + ".png" : null;
    }

    public static string? GetIconPath(int tileType, short u, short v)
    {
        string file = Path.Combine(IconsDir, $"{tileType}_{u}_{v}.png");
        return File.Exists(file)
            ? $"pack://siteoforigin:,,,/Assets/vanilla/tile_icons/{tileType}_{u}_{v}.png"
            : GetIconPath(tileType);
    }
}
