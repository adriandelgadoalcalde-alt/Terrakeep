using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Ap-c: "sin valor hexadecimal ni paleta para los colores" -
// antes solo 3 sliders R/G/B en crudo. ColorSwatchViewModel.Hex se actualiza en los dos
// sentidos: al mover un slider (R/G/B -> Hex) y al escribir un hex valido a mano (Hex -> R/G/B).
public sealed class ColorSwatchHexTests
{
    [Fact]
    public void CambiarRGBActualizaElHexReal()
    {
        var target = new byte[3];
        var swatch = new ColorSwatchViewModel("Pelo", target);

        swatch.R = 255;
        swatch.G = 0;
        swatch.B = 0;

        Assert.Equal("#FF0000", swatch.Hex);
    }

    [Fact]
    public void EscribirUnHexValidoActualizaRGBYElArrayReal()
    {
        var target = new byte[3];
        var swatch = new ColorSwatchViewModel("Pelo", target);

        swatch.Hex = "#1A2B3C";

        Assert.Equal(0x1A, swatch.R);
        Assert.Equal(0x2B, swatch.G);
        Assert.Equal(0x3C, swatch.B);
        Assert.Equal(0x1A, target[0]);
        Assert.Equal(0x2B, target[1]);
        Assert.Equal(0x3C, target[2]);
    }

    [Fact]
    public void EscribirUnHexValidoSinAlmohadillaTambienFunciona()
    {
        var target = new byte[3];
        var swatch = new ColorSwatchViewModel("Pelo", target);

        swatch.Hex = "00FF00";

        Assert.Equal(0, swatch.R);
        Assert.Equal(255, swatch.G);
        Assert.Equal(0, swatch.B);
    }

    [Fact]
    public void UnHexAMedioEscribirSeIgnoraSinTocarRGB()
    {
        var target = new byte[3];
        var swatch = new ColorSwatchViewModel("Pelo", target);
        swatch.R = 10; swatch.G = 20; swatch.B = 30;

        swatch.Hex = "#1A2"; // todavia incompleto, el usuario sigue escribiendo

        Assert.Equal(10, swatch.R);
        Assert.Equal(20, swatch.G);
        Assert.Equal(30, swatch.B);
    }
}
