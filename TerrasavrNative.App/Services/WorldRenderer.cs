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

    // Colores de liquido - map_colors.json SI trae estos de verdad, bajo "global" junto a las 5
    // zonas de fondo (Water/Lava/Honey/Shimmer, la misma fuente real de TEdit ya usada para
    // tiles/paredes/fondo) - se corrige aqui el comentario anterior, que decia lo contrario.
    //
    // Bug real corregido (2-sep-2026, reportado: "no distingue el agua o la lava... todo en
    // rojo"): los codigos 1/2/3 estaban asignados al REVES. Confirmado contra la fuente real
    // de TEdit (World.FileV2.cs, escritor real): Agua -> header1 |= 0b0000_1000 (codigo 1),
    // Lava -> header1 |= 0b0001_0000 (codigo 2), Miel -> header1 |= 0b0001_1000 (codigo 3) -
    // exactamente lo que ya decodifica bien WldReader.cs (liquidHeader = (header1 & 0x18) >>
    // 3), pero este switch asumia 1=lava en vez de 1=agua, asi que TODA el agua real (la
    // mayoria del liquido de cualquier mapa tipico) salia pintada del color de la lava.
    //
    // H3-10 (tercera auditoria de Opus, Fable): miel y Shimmer compartian el MISMO codigo 3
    // (ver el comentario real en WldReader.cs sobre por que en disco es asi) y por tanto el
    // MISMO color aqui (el de Shimmer, lila) - la miel real salia pintada de lila. WldReader ya
    // separa Shimmer a un codigo sintetico propio (4, nunca en disco) - aqui solo hacia falta
    // dejar de aproximar a mano y usar los 4 colores reales ya extraidos de TEdit.
    private static (byte R, byte G, byte B, byte A) LiquidColor(byte liquidType, MapColorCatalog colors)
    {
        var c = colors.Global(liquidType switch
        {
            2 => "Lava",
            3 => "Honey",
            4 => "Shimmer",
            _ => "Water", // codigo 1, y cualquier valor de reserva
        });
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
