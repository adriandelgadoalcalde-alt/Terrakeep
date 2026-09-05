using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.Services;

// Punto 4 (advisor Opus, "buscador o marcador de minerales" - ver ESPEC-ui-exploracion.md#11.3).
//
// Capa de resaltado: un WriteableBitmap del MISMO tamaño que el mapa (1 pixel por tile),
// transparente salvo en los tiles que casan. Se dibuja como una segunda <Image> justo encima de
// WorldMapImage, dentro del mismo Grid escalado - asi hereda el zoom y el desplazamiento sin una
// sola linea de codigo extra.
//
// Por que no se puede portar el mecanismo real de TEdit: TEdit oscurece TODO menos lo
// encontrado, con una mascara de 1 byte por tile (FilterOverlayBuffer.cs:7-12) que consume un
// shader al repintar el mapa por chunks. Terrakeep pinta el mundo UNA vez y congela el bitmap
// (WorldRenderer.cs, bitmap.Freeze()): no hay pipeline al que enchufar una mascara. Marcar en
// positivo sobre una capa aparte da el mismo resultado util (ver de un vistazo donde esta el
// mineral) sin tocar el renderer del mapa base.
//
// Coste de memoria, dicho claro: un Bgra32 del tamaño de un mundo Grande (8400x2400) son 80.6MB,
// los mismos que ya ocupa el mapa - es el precio de esta decision. Mitigaciones aplicadas por
// ExplorationViewModel: una sola capa a la vez (se regenera al cambiar de seleccion, nunca se
// acumulan capas) y se libera (WorldHighlight = null) al desmarcar o cargar otro mundo.
public static class WorldHighlightRenderer
{
    private static readonly (int Dx, int Dy)[] Neighbors8 =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
    ];

    // Halo de 1 tile alrededor de cada tile que casa, a media opacidad, pintado PRIMERO (el
    // nucleo real queda por encima, a opacidad completa). Sin esto, a Zoom=0.1 (el necesario
    // para ver un mundo Grande entero) una veta de 15 tiles ocupa 1.5px y es invisible; con
    // halo ocupa ~3.5px y se ve.
    //
    // C-04 (informe de pulido final, cierra E6/E7): generalizado con un delegado `matches` en
    // vez de leer `tile.Type` a pelo, para poder reutilizar el mismo pintado con Paredes/
    // Liquidos sin duplicar los dos bucles - "hace falta una sobrecarga de Render que acepte
    // tambien WallIds y LiquidTypes, no solo tileTypes".
    private static WriteableBitmap RenderCore(WldWorld world, Func<WldTile, bool> matches, Color color, CancellationToken ct)
    {
        int width = world.Header.TilesWide, height = world.Header.TilesHigh;
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        int stride = width * 4;
        var pixels = new byte[height * stride];

        byte coreR = color.R, coreG = color.G, coreB = color.B;
        byte haloA = (byte)(color.A / 2);

        void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            int offset = y * stride + x * 4;
            pixels[offset + 0] = b;
            pixels[offset + 1] = g;
            pixels[offset + 2] = r;
            pixels[offset + 3] = a;
        }

        // Paso 1: halo (se pinta primero para que el nucleo, mas abajo, quede por encima).
        for (int x = 0; x < width; x++)
        {
            ct.ThrowIfCancellationRequested();
            for (int y = 0; y < height; y++)
            {
                if (!matches(world.Tiles[x, y])) continue;
                foreach (var (dx, dy) in Neighbors8)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    // Un vecino que TAMBIEN casa se pintara como nucleo en el paso 2 - no hace
                    // falta gastar el halo ahi.
                    if (matches(world.Tiles[nx, ny])) continue;
                    SetPixel(nx, ny, coreR, coreG, coreB, haloA);
                }
            }
        }

        // Paso 2: nucleo, opacidad completa, encima del halo.
        for (int x = 0; x < width; x++)
        {
            ct.ThrowIfCancellationRequested();
            for (int y = 0; y < height; y++)
            {
                if (matches(world.Tiles[x, y]))
                    SetPixel(x, y, coreR, coreG, coreB, color.A);
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    public static WriteableBitmap Render(WldWorld world, IReadOnlySet<int> tileTypes, Color color, CancellationToken ct = default) =>
        RenderCore(world, t => t.IsActive && tileTypes.Contains(t.Type), color, ct);

    // C-04: generalizacion a "Objetos > Paredes".
    public static WriteableBitmap RenderWalls(WldWorld world, IReadOnlySet<int> wallIds, Color color, CancellationToken ct = default) =>
        RenderCore(world, t => t.Wall != 0 && wallIds.Contains(t.Wall), color, ct);

    // C-04: generalizacion a "Objetos > Liquidos".
    public static WriteableBitmap RenderLiquids(WldWorld world, IReadOnlySet<byte> liquidTypes, Color color, CancellationToken ct = default) =>
        RenderCore(world, t => t.LiquidAmount > 0 && liquidTypes.Contains(t.LiquidType), color, ct);
}
