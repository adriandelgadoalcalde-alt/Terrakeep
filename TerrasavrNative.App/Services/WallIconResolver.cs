using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de una PARED - Assets/vanilla/wall_icons/{id}.png, 32x32, recortado de
// Images/Wall_{id}.xnb real en el pixel (36,36) = celda (1,1) de la rejilla de 36x36 que declara
// Framing.wallFrameSize: la primera variante del estilo 15 ("rodeada de pared por los 4 lados",
// Framing.cs:135/400-409), literalmente el fotograma que el juego pinta en el interior macizo de
// una zona de pared. Ver scripts/extraer-iconos-paredes.js.
//
// Las paredes NO tienen variantes con nombre (walls.json de TEdit no trae ni textureGrid ni
// frames), asi que no hay sobrecarga con (u,v): una pared, un icono.
// null = pared de mod, o id mas nuevo que el catalogo (real: la 367 existe en Steam pero no en
// walls.json) - la UI cae al color, igual que ya hace hoy con su nombre.
public static class WallIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "wall_icons");

    public static string? GetIconPath(int wallId)
    {
        string file = Path.Combine(IconsDir, $"{wallId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/wall_icons/" + wallId + ".png" : null;
    }
}
