using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerrasavrNative.App.Services;

// Compone el preview de "Apariencia" a partir del atlas real de Terrasavr
// (Assets/player/visual.png, 640x416, copiado tal cual de Terrasavr-Calamity-Beta) - formato
// confirmado leyendo el motor real (clases Sa/app.TabMain y qa/app.BitPart en
// script.readable.js) y verificado recortando y viendo los sprites reales (mismo criterio que
// items.png/buffs.png en su momento).
//
// NO es un cuerpo entero: son 9 "partes" de 40x56 (la 9a, el pelo, de 40x40) que se
// superponen TODAS en el mismo origen (0,0) - confirmado visualmente, casi todo el lienzo de
// cada parte es transparente salvo un detalle facial/de cuello pequeño en la esquina superior
// izquierda; el conjunto compone un icono de cabeza pequeño, no un personaje de cuerpo
// completo. Cada parte se tiñe por MULTIPLICACION RGB PURA sobre el sprite ya recortado (sin
// tocar alfa) - mismo mecanismo que BitPart.set_color real (colorTransform.redMultiplier =
// r/255, etc.) - los sprites base estan en gris/blanco para que el multiply de el color real.
//
// LIMITACION DOCUMENTADA A PROPOSITO: la propia UI original de Terrasavr trae la etiqueta
// literal "(preview is broken atm)" en este panel - nunca tuvo un layout de posicionamiento
// que funcionara del todo (no hay codigo real que posicione las 9 partes por separado). Esta
// version no clona ese layout roto: compone las 9 partes apiladas en el mismo origen, que es
// justo lo que la rejilla real del atlas espera (todo alineado para superponerse ahi).
//
// GENERO NO VERIFICADO AL 100%: el motor real decide el sprite de ropa/piel con
// "gender>=4?0:1" (linea real citada en la investigacion) - un umbral que no tiene sentido
// para un Gender de 0/1 simple como el que lee PlrBodySerializer (siempre caeria en la misma
// rama), asi que probablemente pertenece a una codificacion de genero+variante de un formato
// mucho mas antiguo que este lector no soporta. Se usa en su lugar IsMale (la propia
// convencion ya elegida en AppearanceViewModel) para escoger entre las dos variantes de
// sprite - asuncion razonable pero no verificada pixel a pixel (a este tamaño de icono la
// diferencia visual entre variantes es minima).
public static class PlayerPreviewRenderer
{
    private const int PartWidth = 40, PartHeight = 56;
    private const int HairSize = 40, HairCols = 16, HairRowOffsetY = 56, HairStyleCount = 134;

    public readonly record struct Tint(byte R, byte G, byte B);

    public readonly record struct PlayerColors(Tint Hair, Tint Skin, Tint Eyes, Tint Shirt, Tint Under, Tint Pants, Tint Shoes);

    // (X en la variante "male", X en la variante "female", Y, selector de tinte) - los 8
    // primeros indices reales del motor (Sa.parts[0..7]), todos en Y=0.
    private sealed record PartSpec(int MaleX, int FemaleX, Func<PlayerColors, Tint?> Tint);

    private static readonly PartSpec[] Parts =
    [
        new(80, 80, _ => null),           // 0: base, sin tinte
        new(0, 0, c => c.Skin),           // 1: piel
        new(40, 40, c => c.Eyes),         // 2: ojos
        new(480, 120, c => c.Skin),       // 3: piel (cuello/torso)
        new(320, 160, c => c.Shirt),      // 4: camisa
        new(360, 200, c => c.Under),      // 5: camiseta interior
        new(400, 240, c => c.Pants),      // 6: pantalones
        new(440, 280, c => c.Shoes),      // 7: zapatos
    ];

    private static (byte[] Pixels, int Width, int Height)? _atlas;

    public static WriteableBitmap Render(int hairStyle, bool isMale, PlayerColors colors)
    {
        var (atlasPixels, atlasWidth, _) = _atlas ??= LoadAtlas();
        var canvas = new byte[PartHeight * PartWidth * 4];

        foreach (var part in Parts)
        {
            int srcX = isMale ? part.MaleX : part.FemaleX;
            CompositeRegion(canvas, PartWidth, atlasPixels, atlasWidth, srcX, 0, PartWidth, PartHeight, part.Tint(colors));
        }

        int hairId = Math.Clamp(hairStyle, 0, HairStyleCount - 1);
        int col = hairId % HairCols, row = hairId / HairCols;
        CompositeRegion(canvas, PartWidth, atlasPixels, atlasWidth, col * HairSize, HairRowOffsetY + row * HairSize, HairSize, HairSize, colors.Hair);

        var bitmap = new WriteableBitmap(PartWidth, PartHeight, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, PartWidth, PartHeight), canvas, PartWidth * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static void CompositeRegion(byte[] dst, int dstStride, byte[] src, int srcWidth, int srcX, int srcY, int w, int h, Tint? tint)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int srcOffset = ((srcY + y) * srcWidth + (srcX + x)) * 4;
                byte b = src[srcOffset], g = src[srcOffset + 1], r = src[srcOffset + 2], a = src[srcOffset + 3];
                if (a == 0) continue;

                if (tint is { } t)
                {
                    r = (byte)(r * t.R / 255);
                    g = (byte)(g * t.G / 255);
                    b = (byte)(b * t.B / 255);
                }

                int dstOffset = (y * dstStride + x) * 4;
                float alpha = a / 255f;
                dst[dstOffset + 0] = (byte)(b * alpha + dst[dstOffset + 0] * (1 - alpha));
                dst[dstOffset + 1] = (byte)(g * alpha + dst[dstOffset + 1] * (1 - alpha));
                dst[dstOffset + 2] = (byte)(r * alpha + dst[dstOffset + 2] * (1 - alpha));
                dst[dstOffset + 3] = (byte)(a + dst[dstOffset + 3] * (1 - alpha));
            }
        }
    }

    private static (byte[], int, int) LoadAtlas()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "visual.png");
        var decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var converted = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth, h = converted.PixelHeight;
        var pixels = new byte[w * h * 4];
        converted.CopyPixels(pixels, w * 4, 0);
        return (pixels, w, h);
    }
}
