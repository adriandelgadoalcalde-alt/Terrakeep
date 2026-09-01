using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.Services;

// Pinta un WldWorld completo a un WriteableBitmap (1 pixel por tile) - se pinta UNA vez, luego
// se hace zoom/desplazamiento sobre el bitmap ya generado (mas simple y con mejor rendimiento
// que reimplementar un renderizador incremental por region visible, como hace el visor JS
// original con su canvas).
//
// Orden de mezcla por pixel confirmado contra el visor JS real (fondo de zona -> pared -> tile
// -> liquido, cada capa solo si tiene alpha/cantidad > 0): DE MOMENTO sin el fondo degradado
// por zona (Espacio/Cielo/Tierra/Roca/Infierno) - eso necesita GroundLevel/RockLevel de la
// cabecera del .wld, que WldHeader no lee todavia a proposito (ver su comentario). Se usa un
// fondo solido oscuro en su lugar; el degradado por profundidad queda pendiente.
public static class WorldRenderer
{
    private static readonly Color BackgroundColor = Color.FromRgb(0x12, 0x12, 0x12);

    public static WriteableBitmap Render(WldWorld world, MapColorCatalog colors)
    {
        int width = world.Header.TilesWide;
        int height = world.Header.TilesHigh;
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        int stride = width * 4;
        var pixels = new byte[height * stride];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = world.Tiles[x, y];
                var pixel = Blend(BackgroundColor);

                if (tile.Wall != 0)
                {
                    var wallColor = colors.WallColor(tile.Wall);
                    if (wallColor.A > 0) pixel = Blend(pixel, wallColor);
                }
                if (tile.IsActive)
                {
                    var tileColor = colors.TileColor(tile.Type);
                    if (tileColor.A > 0) pixel = Blend(pixel, tileColor);
                }
                if (tile.LiquidAmount > 0)
                {
                    var liquidColor = LiquidColor(tile.LiquidType);
                    pixel = Blend(pixel, liquidColor);
                }

                int offset = y * stride + x * 4;
                pixels[offset + 0] = pixel.B;
                pixels[offset + 1] = pixel.G;
                pixels[offset + 2] = pixel.R;
                pixels[offset + 3] = 255;
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    // Colores de liquido aproximados (agua/lava/miel/shimmer) - map_colors.json no trae estos
    // (son un efecto de render, no un tile/pared reales), asi que se fijan a mano con los
    // colores reales conocidos del juego en vez de mezclarlos desde otra fuente.
    private static (byte R, byte G, byte B, byte A) LiquidColor(byte liquidType) => liquidType switch
    {
        1 => (250, 100, 0, 200),   // lava
        2 => (255, 214, 63, 200),  // miel
        3 => (200, 170, 255, 200), // shimmer
        _ => (30, 110, 220, 160),  // agua
    };

    private static (byte R, byte G, byte B, byte A) Blend(Color c) => (c.R, c.G, c.B, 255);

    private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B, byte A) bg, RgbaColor fg)
    {
        float a = fg.A / 255f;
        return (
            (byte)(fg.R * a + bg.R * (1 - a)),
            (byte)(fg.G * a + bg.G * (1 - a)),
            (byte)(fg.B * a + bg.B * (1 - a)),
            255);
    }

    private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B, byte A) bg, (byte R, byte G, byte B, byte A) fg)
    {
        float a = fg.A / 255f;
        return (
            (byte)(fg.R * a + bg.R * (1 - a)),
            (byte)(fg.G * a + bg.G * (1 - a)),
            (byte)(fg.B * a + bg.B * (1 - a)),
            255);
    }
}
