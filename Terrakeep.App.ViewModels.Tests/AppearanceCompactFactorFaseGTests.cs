using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// FASE G del responsive global (WARN-01, 29-sep-2026): AppearanceCompactMaxHeight subio de 768 a
// 800 (ver el comentario real en MainViewModel.cs, junto a AppearanceCompactFactor) - a 1366x768
// el revisor midio 66,1px de scroll real en la columna editable de Apariencia porque el tope
// ANTERIOR dejaba ese alto exacto en factor 0 (sin ningun recorte). Estos tests fijan la formula
// en el nivel de ViewModel (sin ventana real, complementa a PERSONAJE_RESPONSIVE_SOLO que mide el
// scroll real con el arnes UI) - canario NEGATIVO: si alguien vuelve a bajar el tope a 768, o lo
// sube por encima de 800 (recortando 1180/1200/1320x800, que deben seguir en factor 0), estos
// tests fallan.
public sealed class AppearanceCompactFactorFaseGTests
{
    [Fact]
    public void A700DeAlto_ElMinimoObligado_SigueEnFactor1_TotalmenteCompacto()
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1080, 700);
        Assert.Equal(1.0, vm.AppearanceCompactFactor, 3);
    }

    [Fact]
    public void A768DeAlto_1366x768_YaNoEsFactor0_ClavaElHallazgoDelRevisor()
    {
        // ANTES de la FASE G (tope=768) esto daba factor 0.0 exacto - el hallazgo real
        // (RESTO/PERSONAJE... WARN-01, revisor-visual-responsive-faseEF-r1): 66,1px de scroll
        // real a este tamaño exacto porque no se recortaba nada.
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1366, 768);
        Assert.True(vm.AppearanceCompactFactor > 0.0, $"AppearanceCompactFactor a 1366x768 deberia ser > 0 (era 0 antes de la FASE G), fue {vm.AppearanceCompactFactor}");
        // (800-768)/(800-700) = 0.32 exacto con el tope real elegido.
        Assert.Equal(0.32, vm.AppearanceCompactFactor, 3);
    }

    [Theory]
    [InlineData(1180, 800)]
    [InlineData(1200, 800)]
    [InlineData(1320, 800)]
    public void A800DeAlto_FronteraRealDelBarrido_SigueEnFactor0_SinCambios(double w, double h)
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(w, h);
        Assert.Equal(0.0, vm.AppearanceCompactFactor, 3);
    }

    [Theory]
    [InlineData(1520, 864)]
    [InlineData(1600, 900)]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    public void TamañosDeAnchoAmplioOMas_SiguenEnFactor0_IdenticosASiempre(double w, double h)
    {
        // Pedido explicito de la FASE G: "A >=1520 debe seguir identico" - ninguno de estos
        // tamaños (todos width>=1520 del barrido del encargo) debe perder su factor 0 con el
        // tope subido a 800 (el mas bajo de los 4, 864, sigue muy por encima de 800).
        var vm = new MainViewModel();
        vm.UpdateSizeClass(w, h);
        Assert.Equal(0.0, vm.AppearanceCompactFactor, 3);
    }

    [Fact]
    public void LaFactorEsContinua_SinSaltosBruscosEntre700Y800()
    {
        var vm = new MainViewModel();
        double anterior = double.PositiveInfinity;
        for (double h = 700; h <= 800; h += 10)
        {
            vm.UpdateSizeClass(1080, h);
            Assert.True(vm.AppearanceCompactFactor <= anterior + 1e-9, $"a alto={h} el factor subio ({vm.AppearanceCompactFactor} > {anterior}), deberia ser monotono decreciente");
            anterior = vm.AppearanceCompactFactor;
        }
        Assert.Equal(0.0, anterior, 3);
    }
}
