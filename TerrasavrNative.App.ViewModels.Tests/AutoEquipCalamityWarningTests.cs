using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Bd-f: "avisar si el personaje no tiene .tplr" - colocar
// equipo de Calamity Mod en un personaje sin datos de Calamity conocidos no falla ni se pierde
// (CharacterFileService.Save crea el .tplr solo con guardar), pero merece un aviso informativo:
// si el mod no esta realmente instalado en el juego del usuario, esos objetos no se reconoceran.
public sealed class AutoEquipCalamityWarningTests
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
        string path = Path.Combine(Path.GetTempPath(), $"autoequip-calamity-warn-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void AutoEquiparBuildDeCalamitySinTplrAvisaAntesDelMensajeNormal()
    {
        var vm = NewLoadedViewModel();
        Assert.False(vm.HasCalamityData); // personaje recien creado, sin .tplr real
        var gearCalamity = vm.Builds.CalamityStages[0].Classes[0].Source;

        vm.AutoEquipCommand.Execute(gearCalamity);

        Assert.StartsWith("Aviso: este personaje no tiene datos de Calamity conocidos", vm.StatusMessage);
    }

    [Fact]
    public void AutoEquiparBuildVanillaNuncaAvisaAunqueNoHayaTplr()
    {
        var vm = NewLoadedViewModel();
        var gearVanilla = vm.Builds.VanillaStages[0].Classes[0].Source;

        vm.AutoEquipCommand.Execute(gearVanilla);

        Assert.DoesNotContain("Aviso:", vm.StatusMessage);
    }
}
