using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), lote 3 de hallazgos sueltos: I-c.
public sealed class LoteTresTests
{
    [Fact]
    public void IC_InicioContentMaxWidth_CreceEnVentanaAmplia()
    {
        var vm = new MainViewModel();

        vm.UpdateSizeClass(1000); // Compacto
        double compacto = vm.InicioContentMaxWidth;

        vm.UpdateSizeClass(1600); // Amplio (>= 1520, umbral real ya medido para E-2/A-4, ver AR-14)
        double amplio = vm.InicioContentMaxWidth;

        Assert.Equal(880, compacto);
        Assert.True(amplio > compacto); // sitio real de sobra, no se queda angosto en una pantalla grande
    }
}
