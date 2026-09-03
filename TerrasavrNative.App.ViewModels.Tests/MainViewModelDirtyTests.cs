using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), B-4/B-5: "matriz de suciedad" real - MainViewModel headless
// de verdad (sin ventana, sin Application), cargando un personaje sintetico real desde un
// fichero temporal (mismo patron ya usado en TerrasavrNative.App.Tests). Cada [Fact] confirma
// que una accion editable real deja IsDirty=true - la unica forma real de detectar el tipo de
// agujero que N-2 (Bloque 0) se proponia cerrar del todo y no cerro.
public sealed class MainViewModelDirtyTests
{
    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"dirty-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));

        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        Assert.True(vm.IsCharacterLoaded);
        Assert.False(vm.IsDirty); // cargar un personaje NUNCA debe marcarlo sucio por si solo

        File.Delete(path);
        return vm;
    }

    [Fact]
    public void ResearchAll_MarcaElPersonajeComoModificado()
    {
        // B-4: ResearchAllService.Apply muta el modelo directamente, sin pasar por ningun
        // ViewModel observable - antes del arreglo, IsDirty se quedaba en false.
        var vm = NewLoadedViewModel();

        vm.ResearchAllCommand.Execute(null);

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void AñadirSpawnPoint_MarcaElPersonajeComoModificado()
    {
        // B-5: Entries es una ObservableCollection (CollectionChanged, no PropertyChanged) -
        // antes del arreglo, MainViewModel escuchaba Servers.PropertyChanged a secas y nunca
        // se enteraba de un Add/Remove.
        var vm = NewLoadedViewModel();

        vm.Servers.AddEntryCommand.Execute(null);

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void EditarUnSpawnPointExistente_MarcaElPersonajeComoModificado()
    {
        // B-5: los campos editables viven en ServerEntryRowViewModel, cuyo PropertyChanged
        // antes del arreglo nunca llegaba a MainViewModel.
        var vm = NewLoadedViewModel();
        vm.Servers.AddEntryCommand.Execute(null);
        Assert.True(vm.IsDirty);
        vm.IsDirty = false; // el propio Add ya deja sucio (ver el test de arriba) - se resetea para aislar SOLO la edicion

        vm.Servers.Entries[0].Name = "Base secreta";

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void QuitarUnSpawnPoint_MarcaElPersonajeComoModificado()
    {
        var vm = NewLoadedViewModel();
        vm.Servers.AddEntryCommand.Execute(null);
        vm.IsDirty = false;

        vm.Servers.RemoveEntryCommand.Execute(vm.Servers.Entries[0]);

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void CambiarUnColorDeApariencia_MarcaElPersonajeComoModificado()
    {
        var vm = NewLoadedViewModel();

        vm.Appearance.Swatches[0].R = vm.Appearance.Swatches[0].R == 0 ? 1 : 0;

        Assert.True(vm.IsDirty);
    }
}
