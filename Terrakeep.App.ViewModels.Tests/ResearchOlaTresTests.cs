using System.IO;
using System.Linq;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Ola 3, R-d/R-e/R-f/R-g (Investigacion, Fase 1 - "arreglos
// que no cambian la forma").
public sealed class ResearchOlaTresTests
{
    // H3-06 (tercera auditoria de Opus, Fable): desde que ResearchViewModel.SearchText debouncea
    // (180ms, mismo motivo real medido en L-c), un test que solo asigna SearchText y mira
    // Results de inmediato ya no ve el resultado - hace falta bombear un Dispatcher real de
    // verdad (con una pausa real entre vueltas, mismo bug de este mismo tipo de arnes ya
    // encontrado y documentado para el arnes UIA de L-c: sin la pausa, el WM_TIMER real puede no
    // llegar a entregarse nunca).
    private static void WaitForDispatcher(int ms)
    {
        long until = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < until)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background, new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            System.Threading.Thread.Sleep(1);
        }
    }

    private static MainViewModel NewLoadedViewModel(byte difficulty = 3)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            Difficulty = difficulty,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"research-ola3-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void RD_UnObjetoDeCalamityInvestigadoConElPlaceholder_MuestraInvestigadoNoElNumeroCrudo()
    {
        var vm = NewLoadedViewModel();
        vm.ResearchAllCommand.Execute(null); // deja TODOS los objetos de Calamity en el placeholder real (9999)

        // CalamityIds.ItemIdBase (20000000) es el primer id sintetico real de objetos de
        // Calamity (SyntheticId = ItemIdBase + indice) - busca ese id exacto.
        vm.Research.SearchText = "#20000000";
        WaitForDispatcher(300); // H3-06: espera real al debounce
        var fila = vm.Research.Results.FirstOrDefault(r => r.IsCalamity);
        Assert.NotNull(fila);
        Assert.Equal("✔ Investigado", fila!.CountLabel);
        Assert.DoesNotContain("9999", fila.CountLabel);
    }

    [Fact]
    public void RE_ElBuscadorFiltraPorIdEntreLoYaInvestigado_SinNecesidadDeElegirCarpeta()
    {
        var vm = NewLoadedViewModel();
        vm.ResearchAllCommand.Execute(null); // investiga todo, para tener contra que buscar
        Assert.Null(vm.Research.SelectedCategory); // ninguna carpeta elegida - la busqueda debe funcionar igual

        vm.Research.SearchText = "#3"; // Iron Broadsword, id real vanilla 3
        WaitForDispatcher(300); // H3-06: espera real al debounce

        Assert.Single(vm.Research.Results); // un id exacto solo puede dar una fila real
        Assert.False(vm.Research.Results[0].IsCalamity);
    }

    [Fact]
    public void RF_ElResumenMuestraProgresoRealSobreElTotalConocido_SinCarpetaNiBusqueda()
    {
        var vm = NewLoadedViewModel();

        Assert.Contains("/", vm.Research.ResultsSummary); // formato real "N/Total", no solo "N objetos"
    }

    [Fact]
    public void RG_IsJourneyMode_RefleljaLaDificultadRealDelPersonaje()
    {
        var journey = NewLoadedViewModel(difficulty: 3);
        Assert.True(journey.Research.IsJourneyMode);

        var softcore = NewLoadedViewModel(difficulty: 0);
        Assert.False(softcore.Research.IsJourneyMode);
    }
}
