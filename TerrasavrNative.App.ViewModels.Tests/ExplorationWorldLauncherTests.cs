using System.Threading.Tasks;
using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// H4-08 (cuarta auditoria de Opus, Fable): "la version buena - un lanzador de mundos calcado
// del de personajes de Inicio". Mismo criterio real que HomeRefreshAsyncTests: el escaneo real
// (RefreshWorldsCommand, IAsyncRelayCommand real) corre en un hilo de fondo, se puede esperar
// de verdad via ExecutionTask, sin sleeps a ciegas.
public sealed class ExplorationWorldLauncherTests
{
    [Fact]
    public async Task RefreshWorldsCommand_EsAsincronoYIsScanningWorldsVuelveAFalseAlTerminar()
    {
        var vm = new MainViewModel();
        // El constructor de ExplorationViewModel ya lanzo su propio escaneo (fire-and-forget) -
        // se espera a que termine antes de arrancar uno nuevo, mismo camino real
        // (RefreshWorldsCommand).
        if (vm.Exploration.RefreshWorldsCommand.ExecutionTask is { } enCurso) await enCurso;

        vm.Exploration.RefreshWorldsCommand.Execute(null);

        Assert.True(vm.Exploration.IsScanningWorlds); // arranca en caliente, todavia no ha podido terminar
        await vm.Exploration.RefreshWorldsCommand.ExecutionTask!;

        Assert.False(vm.Exploration.IsScanningWorlds);
    }

    [Fact]
    public async Task TrasElEscaneo_ScanMessageSoloEsRealCuandoNoHayNingunMundo()
    {
        var vm = new MainViewModel();
        if (vm.Exploration.RefreshWorldsCommand.ExecutionTask is { } enCurso) await enCurso;

        // No se asume nada sobre si esta maquina tiene mundos reales o no - solo la relacion
        // real entre las dos propiedades, valida en los dos casos.
        if (vm.Exploration.Worlds.Count == 0)
            Assert.NotNull(vm.Exploration.ScanMessage);
        else
            Assert.Null(vm.Exploration.ScanMessage);
    }
}
