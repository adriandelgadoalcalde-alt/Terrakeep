using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// H4-08 (cuarta auditoria de Opus, Fable): "el estado vacio de Exploracion es un lienzo negro
// sin guia" - IsEmpty (nuevo) gobierna el aviso real centrado en el lienzo, y NUNCA debe estar
// activo a la vez que IsLoading (evita el parpadeo entre los dos avisos superpuestos).
public sealed class ExplorationEmptyStateTests
{
    [Fact]
    public void SinMundoCargadoYSinCargarEnCurso_EsVacio()
    {
        var vm = new MainViewModel();

        Assert.True(vm.Exploration.IsEmpty);
    }

    [Fact]
    public void MientrasCargaUnMundo_NoEsVacio_AunSinIsWorldLoadedTodavia()
    {
        var vm = new MainViewModel();

        vm.Exploration.IsLoading = true;

        Assert.False(vm.Exploration.IsEmpty); // nunca los dos avisos a la vez
    }

    [Fact]
    public void ConUnMundoCargado_NoEsVacio()
    {
        var vm = new MainViewModel();

        vm.Exploration.IsWorldLoaded = true;

        Assert.False(vm.Exploration.IsEmpty);
    }
}
