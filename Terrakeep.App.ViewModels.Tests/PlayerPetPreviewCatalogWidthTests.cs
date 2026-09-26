using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Data;
using Terrakeep.Core.Layout;

namespace Terrakeep.App.ViewModels.Tests;

// GapAnalysis ParidadPersonaje (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514) -
// segunda fase del patron de 2 agentes (arquitecto-keep diseño la arquitectura completa,
// PlayerPetPreviewLayout.cs, incluida esta medicion real como parte del diseño; este fichero la
// repite/verifica tal cual sobre los assets reales de produccion, no reinvestiga la causa).
//
// Mide, de verdad, las 63 mascotas REALES de Assets/pet_animations.json contra sus sprites REALES
// (Assets/pets/*.png) - ni un numero inventado ni copiado de un informe anterior: cada CI/dotnet
// test vuelve a calcular esto contra los assets de produccion tal y como estan en ese momento, asi
// que si el catalogo crece en el futuro con una mascota mas ancha que
// PlayerPetPreviewLayout.ReserveColumnWidthNative, este test lo detecta solo (FALLO real, no un
// "deberia seguir siendo valido" silencioso).
//
// Metodologia real (pedida explicitamente por el usuario, 26-sep-2026): para cada mascota, se usa
// el BOUNDING BOX ALPHA-VISIBLE real de cada fotograma REALMENTE usado en el ciclo de hover
// (SelStart..SelStart+SelCount-1) - no el rectangulo de fotograma crudo (que incluye relleno
// transparente real en varios sprites, sobrestimar el ancho necesario). CompositeBounds se calcula
// con el MISMO motor puro que ya usa produccion (PlayerPetPreviewLayout), en unidades nativas -
// el canvasScale se aplica UNA sola vez fuera (aqui no hace falta, ReserveColumnWidthNative ya vive
// en unidades nativas).
public sealed class PlayerPetPreviewCatalogWidthTests
{
    private static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");

    private readonly record struct Medicion(int ItemId, int Shoot, double CompositeWidth);

    private static IReadOnlyList<Medicion> MedirCatalogoReal()
    {
        var catalogPath = Path.Combine(AssetsDir, "pet_animations.json");
        Assert.True(File.Exists(catalogPath), $"pet_animations.json real no encontrado en {catalogPath} - no se puede medir el catalogo real.");

        var raw = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, PetAnimationEntry>>(File.ReadAllText(catalogPath))
            ?? throw new InvalidDataException("pet_animations.json invalido.");

        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var spriteJugador = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);

        var mediciones = new List<Medicion>();
        foreach (var (itemIdText, entry) in raw)
        {
            string sheetPath = Path.Combine(AssetsDir, "pets", entry.Shoot + ".png");
            Assert.True(File.Exists(sheetPath), $"item {itemIdText} (proyectil {entry.Shoot}): sprite real no encontrado en {sheetPath}.");

            using var stream = File.OpenRead(sheetPath);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            var converted = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            int sheetW = frame.PixelWidth, sheetH = frame.PixelHeight;
            var pixels = new byte[sheetW * sheetH * 4];
            converted.CopyPixels(pixels, sheetW * 4, 0);

            int frameHeight = sheetH / entry.TotalFrames;
            Assert.True(frameHeight > 0, $"item {itemIdText}: TotalFrames real ({entry.TotalFrames}) no cabe en el alto real de la hoja ({sheetH}px).");

            double? maxRight = null, minLeft = null;
            for (int k = 0; k < entry.SelCount; k++)
            {
                int fila = System.Math.Min(entry.SelStart + k, entry.TotalFrames - 1);
                int startY = fila * frameHeight;
                if (startY + frameHeight > sheetH) continue;

                var (bx0, bx1) = AlphaVisibleColumnasReal(pixels, sheetW, frameHeight, startY);
                if (bx0 is null) continue;

                var petBounds = PlayerPetPreviewLayout.PetBounds(origen, entry, petFrameWidth: sheetW, petFrameHeight: frameHeight);
                double leftReal = petBounds.Left + bx0.Value;
                double rightReal = petBounds.Left + bx1!.Value;
                if (minLeft is null || leftReal < minLeft) minLeft = leftReal;
                if (maxRight is null || rightReal > maxRight) maxRight = rightReal;
            }

            Assert.True(maxRight is not null, $"item {itemIdText} (proyectil {entry.Shoot}): ningun fotograma real del ciclo de hover tiene pixeles visibles (alpha>0) - dato inesperado, revisar el sprite real.");

            double compLeft = System.Math.Min(spriteJugador.Left, minLeft!.Value);
            double compRight = System.Math.Max(spriteJugador.Right, maxRight!.Value);
            mediciones.Add(new Medicion(int.Parse(itemIdText), entry.Shoot, compRight - compLeft));
        }

        return mediciones;
    }

    // Bbox alpha-visible (columnas con al menos un pixel alpha>0) real DENTRO de un fotograma
    // concreto de la hoja - devuelve (colInicial, colFinalExclusivaComoLimiteInclusivo) o
    // (null,null) si el fotograma esta completamente vacio (alpha 0 en todos los pixeles).
    private static (int? Left, int? Right) AlphaVisibleColumnasReal(byte[] pixelsBgra32, int sheetWidth, int frameHeight, int startY)
    {
        int? left = null, right = null;
        for (int y = 0; y < frameHeight; y++)
        {
            int filaBase = (startY + y) * sheetWidth * 4;
            for (int x = 0; x < sheetWidth; x++)
            {
                byte alpha = pixelsBgra32[filaBase + x * 4 + 3];
                if (alpha == 0) continue;
                if (left is null || x < left) left = x;
                if (right is null || x + 1 > right) right = x + 1;
            }
        }
        return (left, right);
    }

    [Fact]
    public void MedicionRealDelCatalogo_63Mascotas_NingunaSuperaElAnchoDeReservaFijado()
    {
        var mediciones = MedirCatalogoReal();
        Assert.Equal(63, mediciones.Count); // alcance deliberado y documentado (PetAnimationCatalog.cs)

        var anchos = mediciones.Select(m => m.CompositeWidth).OrderBy(w => w).ToList();
        double p90 = Percentil(anchos, 90);
        var peor = mediciones.OrderByDescending(m => m.CompositeWidth).First();

        // Documentado para el historial (decision final del usuario, 26-sep-2026, fue "usa el
        // PEOR CASO, no el percentil" - el percentil 90 real de esta pasada se deja aqui igual,
        // visible en el mensaje de fallo si algo cambia, para no perder el dato):
        string resumen = $"p90 real={p90:0.##} nativos, peor caso real=item {peor.ItemId} (proyectil {peor.Shoot}) con {peor.CompositeWidth:0.##} nativos, ReserveColumnWidthNative={PlayerPetPreviewLayout.ReserveColumnWidthNative:0.##}";

        Assert.True(peor.CompositeWidth <= PlayerPetPreviewLayout.ReserveColumnWidthNative,
            $"FALLO: el catalogo real de mascotas tiene un caso ({peor.ItemId}/proyectil {peor.Shoot}, {peor.CompositeWidth:0.##} nativos) que supera el ancho de reserva fijado ({PlayerPetPreviewLayout.ReserveColumnWidthNative:0.##} nativos) - esa mascota se recortaria contra el borde de la tarjeta/banner. {resumen}");
    }

    private static double Percentil(IReadOnlyList<double> ordenados, double p)
    {
        if (ordenados.Count == 1) return ordenados[0];
        double k = (ordenados.Count - 1) * (p / 100.0);
        int f = (int)k;
        int c = f + 1 < ordenados.Count ? f + 1 : f;
        if (f == c) return ordenados[f];
        return ordenados[f] * (c - k) + ordenados[c] * (k - f);
    }
}
