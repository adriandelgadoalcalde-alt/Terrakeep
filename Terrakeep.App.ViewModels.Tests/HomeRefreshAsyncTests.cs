using System.Threading.Tasks;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), T-G: "arranque sincrono - HomeViewModel.Refresh() async,
// con IsScanning (ya existia pero sin ningun binding real)". El escaneo real de disco ahora
// corre en un hilo de fondo (Task.Run) en vez de bloquear el constructor - se puede esperar de
// verdad via RefreshAsyncCommand.ExecutionTask (IAsyncRelayCommand real, ver
// CommunityToolkit.Mvvm), sin sleeps a ciegas.
public sealed class HomeRefreshAsyncTests
{
    [Fact]
    public async Task RefreshAsyncCommand_EsAsincronoYIsScanningVuelveAFalseAlTerminar()
    {
        var service = new CharacterFileService();
        var home = new HomeViewModel(service.EquipmentAppearance, service.BackupHistory);
        // El constructor ya lanzo su propio escaneo (fire-and-forget) - se espera a que
        // termine antes de arrancar uno nuevo, mismo camino real (RefreshCommand).
        if (home.RefreshCommand.ExecutionTask is { } enCurso) await enCurso;

        home.RefreshCommand.Execute(null);

        Assert.True(home.IsScanning); // el comando async arranca en caliente, todavia no ha podido terminar
        await home.RefreshCommand.ExecutionTask!;

        Assert.False(home.IsScanning);
    }
}
