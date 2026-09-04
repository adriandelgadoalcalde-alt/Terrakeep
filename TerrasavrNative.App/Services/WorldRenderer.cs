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
// Orden de mezcla por pixel confirmado contra el visor JS real: fondo de zona (segun
// profundidad, ver WldHeader.ZoneFor) -> pared -> tile -> liquido, cada capa solo si tiene
// alpha/cantidad > 0.
public static class WorldRenderer
{
    // Color de reserva si alguna zona no aparece en map_colors.json (no deberia pasar, las 5
    // zonas reales - Space/Sky/Earth/Rock/Hell - siempre estan, pero Global() devuelve
    // Transparent en un fallo de datos y no queremos un fondo invisible).
    private static readonly Color FallbackBackgroundColor = Color.FromRgb(0x12, 0x12, 0x12);

    public static WriteableBitmap Render(WldWorld world, MapColorCatalog colors)
    {
        int width = world.Header.TilesWide;
        int height = world.Header.TilesHigh;
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        int stride = width * 4;
        var pixels = new byte[height * stride];

        // El fondo depende solo de la fila (profundidad), no de la columna - se calcula una
        // vez por fila en vez de una vez por pixel.
        var rowBackgrounds = new (byte R, byte G, byte B, byte A)[height];
        for (int y = 0; y < height; y++)
        {
            var zoneColor = colors.Global(world.Header.ZoneFor(y));
            rowBackgrounds[y] = zoneColor.A > 0 ? (zoneColor.R, zoneColor.G, zoneColor.B, (byte)255) : Blend(FallbackBackgroundColor);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = world.Tiles[x, y];
                var pixel = rowBackgrounds[y];

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
                    var liquidColor = LiquidColor(tile.LiquidType, colors);
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

    // El switch de codigo de liquido -> nombre de zona vive ahora en MapColorCatalog.LiquidColor
    // (fuente unica, tambien usada por ExplorationViewModel para el swatch de la pestaña
    // Liquidos del buscador) - aqui solo se convierte a la tupla que usa el resto de Blend().
    private static (byte R, byte G, byte B, byte A) LiquidColor(byte liquidType, MapColorCatalog colors)
    {
        var c = colors.LiquidColor(liquidType);
        return (c.R, c.G, c.B, c.A);
    }

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
