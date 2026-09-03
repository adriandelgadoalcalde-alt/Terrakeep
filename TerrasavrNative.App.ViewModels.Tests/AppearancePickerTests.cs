using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Ap-a: "los selectores de peinado/tinte abiertos a la vez
// empujan el contenido" - ninguno de los dos cerraba al otro.
public sealed class AppearancePickerTests
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
        string path = Path.Combine(Path.GetTempPath(), $"appearance-picker-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void AbrirTinteCierraElSelectorDePeinado()
    {
        var vm = NewLoadedViewModel();
        vm.Appearance.OpenHairPickerCommand.Execute(null);
        Assert.True(vm.Appearance.IsHairPickerOpen);

        vm.Appearance.OpenHairDyePickerCommand.Execute(null);

        Assert.True(vm.Appearance.IsHairDyePickerOpen);
        Assert.False(vm.Appearance.IsHairPickerOpen);
    }

    [Fact]
    public void AbrirPeinadoCierraElSelectorDeTinte()
    {
        var vm = NewLoadedViewModel();
        vm.Appearance.OpenHairDyePickerCommand.Execute(null);
        Assert.True(vm.Appearance.IsHairDyePickerOpen);

        vm.Appearance.OpenHairPickerCommand.Execute(null);

        Assert.True(vm.Appearance.IsHairPickerOpen);
        Assert.False(vm.Appearance.IsHairDyePickerOpen);
    }

    // Ap-b: cambiar el color de pelo con el selector CERRADO no debe regenerar nada de
    // inmediato (barato, solo marca obsoleto) - se comprueba que la coleccion sigue vacia
    // (nunca se abrio) y que abrirlo despues SI la rellena con el color YA actualizado.
    [Fact]
    public void CambiarColorConElSelectorCerradoNoRegeneraHastaAbrirlo()
    {
        var vm = NewLoadedViewModel();
        var hairSwatch = vm.Appearance.Swatches.First(s => s.Label == "Pelo");

        hairSwatch.R = 12;
        hairSwatch.G = 34;
        hairSwatch.B = 56;
        Assert.Empty(vm.Appearance.HairOptions); // nunca se abrio, nada que regenerar todavia

        vm.Appearance.OpenHairPickerCommand.Execute(null);

        Assert.Equal(228, vm.Appearance.HairOptions.Count);
    }
}
