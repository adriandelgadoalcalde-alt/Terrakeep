using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.Core.Rendering;

// Movido desde Terrakeep.App.Services.WorldRenderer (20-sep-2026, catalogo de funciones "7.
// ServidorKeep" - funcion 2, "mapa de mundo en vivo"). Ronda anterior de esta misma noche dejo
// documentado en bitacora.md que esta pieza estaba lista para moverse (matematica pura de mezcla
// de pixeles, sin nada de WPF salvo el CONTENEDOR de salida) pero no se toco por riesgo real de
// choque con sesiones concurrentes sobre Terrakeep.Core/Terrakeep.App - confirmado por el
// coordinador que esa sesion ya termino, aqui esta el movimiento real: el algoritmo de mezcla
// (identico, ni una linea de logica cambiada) vive ahora en Terrakeep.Core sin ninguna
// dependencia de WPF, devolviendo un `byte[]` BGRA32 en bruto + ancho/alto/stride en vez de un
// WriteableBitmap - asi ServidorKeep (servidor headless, sin referencia a PresentationFramework)
// puede referenciar Terrakeep.Core.dll y llamar a esto directamente. Terrakeep.App.Services.
// WorldRenderer sigue existiendo como envoltorio fino: llama aqui y envuelve el array en un
// WriteableBitmap para su propio uso, sin cambiar su firma publica ni su comportamiento.
//
// Orden de mezcla por pixel confirmado contra el visor JS real: fondo de zona (segun
// profundidad, ver WldHeader.ZoneFor) -> pared -> tile -> liquido, cada capa solo si tiene
// alpha/cantidad > 0.
public static class WorldPixelRenderer
{
    // Color de reserva si alguna zona no aparece en map_colors.json (no deberia pasar, las 5
    // zonas reales - Space/Sky/Earth/Rock/Hell - siempre estan, pero Global() devuelve
    // Transparent en un fallo de datos y no queremos un fondo invisible).
    private static readonly (byte R, byte G, byte B) FallbackBackgroundColor = (0x12, 0x12, 0x12);

    // Pixeles BGRA32 en bruto (4 bytes por pixel, orden B-G-R-A, alpha siempre 255 - mismo
    // formato exacto que WriteableBitmap(..., PixelFormats.Bgra32, null) esperaba antes) mas
    // ancho/alto/stride para que el llamador (WPF o cualquier otro) sepa como interpretarlos.
    public readonly record struct RenderedPixels(byte[] Pixels, int Width, int Height, int Stride);

    public static RenderedPixels Render(WldWorld world, MapColorCatalog colors)
    {
        int width = world.Header.TilesWide;
        int height = world.Header.TilesHigh;
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

        return new RenderedPixels(pixels, width, height, stride);
    }

    // El switch de codigo de liquido -> nombre de zona vive en MapColorCatalog.LiquidColor
    // (fuente unica, tambien usada por ExplorationViewModel para el swatch de la pestaña
    // Liquidos del buscador) - aqui solo se convierte a la tupla que usa el resto de Blend().
    private static (byte R, byte G, byte B, byte A) LiquidColor(byte liquidType, MapColorCatalog colors)
    {
        var c = colors.LiquidColor(liquidType);
        return (c.R, c.G, c.B, c.A);
    }

    private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B) c) => (c.R, c.G, c.B, 255);

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
