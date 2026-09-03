using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H4-07 punto 3 (cuarta auditoria de Opus, Fable): "el resumen de Investigacion sin carpeta
// podria enseñar las carpetas raiz como tarjetas grandes en el area vacia, en vez de solo una
// frase" - ShowRootCategoryCards gobierna esa vista alternativa (mismo RootCategories real que
// ya usa el arbol de la izquierda, con su propio SelectCommand).
public sealed class ResearchRootCategoryCardsTests
{
    // Mismo helper real que ResearchOlaTresTests.cs - el SearchText de Research debouncea
    // (H3-06, 180ms), hace falta bombear el Dispatcher para verlo aplicado en el test.
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

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"research-cards-h407-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinCarpetaNiBusqueda_MuestraLasTarjetasDeCarpetaRaiz()
    {
        var vm = NewLoadedViewModel();

        Assert.True(vm.Research.ShowRootCategoryCards);
        Assert.NotEmpty(vm.Research.RootCategories);
    }

    [Fact]
    public void ElegirUnaCarpeta_OcultaLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.Research.RootCategories.First();

        vm.Research.SelectCategoryCommand.Execute(carpeta);

        Assert.False(vm.Research.ShowRootCategoryCards);
    }

    [Fact]
    public void EscribirUnaBusqueda_OcultaLasTarjetas()
    {
        var vm = NewLoadedViewModel();

        vm.Research.SearchText = "#3";
        WaitForDispatcher(300);

        Assert.False(vm.Research.ShowRootCategoryCards);
    }

    [Fact]
    public void QuitarLaCarpetaSinBusquedaActiva_VuelveAMostrarLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.Research.RootCategories.First();
        vm.Research.SelectCategoryCommand.Execute(carpeta);
        Assert.False(vm.Research.ShowRootCategoryCards);

        vm.Research.ClearCategoryCommand.Execute(null);

        Assert.True(vm.Research.ShowRootCategoryCards);
    }
}
