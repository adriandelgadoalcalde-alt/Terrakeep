using System.IO;
using System.Windows.Media.Imaging;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// H6-11 (sexta auditoria de Opus, "los objetos animados -Alma de vuelo/Alma de luz, etc.- salen
// como una tira de fotogramas entera, no un unico icono"): VanillaIconResolver ahora extrae de
// Item_{id}.xnb reales (no un atlas), recortando la tira vertical de un objeto animado a UN
// solo fotograma real (ver scripts/extraer-iconos-vanilla.js, lista real de
// Terraria.Main.InitializeItemAnimations() decompilado). Estas pruebas leen el PNG real en
// disco y confirman la geometria - no solo que la ruta se calcule bien.
public sealed class VanillaIconResolverH6Tests
{
    private static (int Width, int Height) RealSize(string packUri)
    {
        // pack://siteoforigin:,,,/Assets/vanilla/icons/{id}.png -> ruta absoluta real bajo
        // AppContext.BaseDirectory, mismo patron que el propio resolver.
        string relative = packUri.Replace("pack://siteoforigin:,,,/", "").Replace('/', Path.DirectorySeparatorChar);
        string path = Path.Combine(AppContext.BaseDirectory, relative);
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return (decoder.Frames[0].PixelWidth, decoder.Frames[0].PixelHeight);
    }

    [Theory]
    [InlineData(520)] // Alma de luz (SoulofLight) - ejemplo REAL citado por el usuario
    [InlineData(575)] // Alma de vuelo (SoulofFlight) - ejemplo REAL citado por el usuario
    public void ObjetoAnimadoReal_ResuelveUnUnicoFotogramaNoLaTiraEntera(int itemId)
    {
        var path = VanillaIconResolver.GetIconPath(itemId);
        Assert.NotNull(path);

        var (width, height) = RealSize(path!);

        // El sprite REAL completo (Item_520.xnb/Item_575.xnb) mide 22x112 (4 fotogramas de
        // 28px cada uno) - un unico fotograma real mide 22x28, mucho mas bajo que ancho x4.
        Assert.Equal(28, height);
        Assert.True(width < height * 2, $"El icono de {itemId} sigue pareciendo una tira vertical ({width}x{height}), no un unico fotograma.");
    }

    [Fact]
    public void ObjetoDeComidaReal_ResuelveUnUnicoFotograma()
    {
        // Manzana (id 353) - primer id real de ItemID.Sets.IsFood (3 fotogramas reales).
        var path = VanillaIconResolver.GetIconPath(353);
        Assert.NotNull(path);
        var (_, height) = RealSize(path!);
        Assert.True(height < 40, $"Altura {height} sigue pareciendo varios fotogramas de comida apilados.");
    }

    [Fact]
    public void HuecoRealAntiguo_PalladiumDrill_YaTieneIconoReal()
    {
        // H6-11: el atlas viejo (items.png) tenia UN solo hueco documentado (PalladiumDrill,
        // 1189) - con la fuente real (Item_1189.xnb, que SI existe en la instalacion de Steam)
        // deja de ser un hueco.
        Assert.NotNull(VanillaIconResolver.GetIconPath(1189));
    }

    [Fact]
    public void ObjetoNoAnimadoConocido_SigueResolviendoSuIconoRealCompleto()
    {
        // Pico de hierro (id 1) - sprite real 32x32, sin animacion real - no debe recortarse.
        var path = VanillaIconResolver.GetIconPath(1);
        Assert.NotNull(path);
        var (width, height) = RealSize(path!);
        Assert.Equal(32, width);
        Assert.Equal(32, height);
    }
}
