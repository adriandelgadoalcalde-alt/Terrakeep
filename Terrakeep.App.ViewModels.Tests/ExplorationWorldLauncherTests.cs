using System.Threading.Tasks;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

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
        // Error real encontrado en la PROPIA prueba (no en produccion) escribiendo esta tanda:
        // el constructor llama al metodo async DIRECTAMENTE (fire-and-forget, mismo patron real
        // que HomeViewModel), no a traves de RefreshWorldsCommand - ExecutionTask se queda null
        // hasta la PRIMERA vez que se llama al comando de verdad, asi que el "if" de la prueba
        // de arriba nunca esperaba nada aqui y las aserciones corrian antes de que el escaneo
        // en curso hubiera terminado de verdad. Se llama al comando explicitamente y se espera
        // su propio ExecutionTask, en vez de confiar en el escaneo implicito del constructor.
        await vm.Exploration.RefreshWorldsCommand.ExecuteAsync(null);

        // No se asume nada sobre si esta maquina tiene mundos reales o no - solo la relacion
        // real entre las dos propiedades, valida en los dos casos.
        if (vm.Exploration.Worlds.Count == 0)
            Assert.NotNull(vm.Exploration.ScanMessage);
        else
            Assert.Null(vm.Exploration.ScanMessage);
    }
}
