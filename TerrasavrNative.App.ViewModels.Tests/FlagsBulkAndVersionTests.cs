using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), D-d/D-e: "sin accion en bloque para los flags" + "sin
// aviso si el flag no existe todavia en la version real del personaje". Los umbrales usados en
// D-e son los MISMOS ya verificados en PlrBodySerializer (145/230/269/184/253) - por debajo,
// Guardar pierde el cambio en silencio (el campo ni se escribe).
public sealed class FlagsBulkAndVersionTests
{
    private static MainViewModel NewLoadedViewModel(int version)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = version,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"flags-bulk-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void MarcarTodosPoneLas13CasillasReales()
    {
        var vm = NewLoadedViewModel(315); // version reciente real, sin ningun umbral por debajo

        vm.Flags.MarkAllCommand.Execute(null);

        Assert.True(vm.Flags.ExtraAccessory);
        Assert.True(vm.Flags.UnlockedBiomeTorches);
        Assert.True(vm.Flags.UsingBiomeTorches);
        Assert.True(vm.Flags.ArtisanBread);
        Assert.True(vm.Flags.VitalCrystal);
        Assert.True(vm.Flags.AegisFruit);
        Assert.True(vm.Flags.ArcaneCrystal);
        Assert.True(vm.Flags.GalaxyPearl);
        Assert.True(vm.Flags.GummyWorm);
        Assert.True(vm.Flags.Ambrosia);
        Assert.True(vm.Flags.FinishedDD2Event);
        Assert.True(vm.Flags.UnlockedSuperMinecart);
        Assert.True(vm.Flags.UsingSuperMinecart);
    }

    [Fact]
    public void DesmarcarTodosLimpiaLas13CasillasReales()
    {
        var vm = NewLoadedViewModel(315);
        vm.Flags.MarkAllCommand.Execute(null);

        vm.Flags.MarkNoneCommand.Execute(null);

        Assert.False(vm.Flags.ExtraAccessory);
        Assert.False(vm.Flags.FinishedDD2Event);
        Assert.False(vm.Flags.UsingSuperMinecart);
    }

    [Theory]
    [InlineData(144, true)] // umbral real 145, ExtraAccessory
    [InlineData(145, false)]
    public void ExtraAccessoryBelowVersion_UsaElUmbralRealDePlrBodySerializer(int version, bool esperado)
    {
        var vm = NewLoadedViewModel(version);
        Assert.Equal(esperado, vm.Flags.ExtraAccessoryBelowVersion);
    }

    [Theory]
    [InlineData(229, true)] // umbral real 230
    [InlineData(230, false)]
    public void BiomeTorchesBelowVersion_UsaElUmbralRealDePlrBodySerializer(int version, bool esperado)
    {
        var vm = NewLoadedViewModel(version);
        Assert.Equal(esperado, vm.Flags.BiomeTorchesBelowVersion);
    }

    [Theory]
    [InlineData(268, true)] // umbral real 269
    [InlineData(269, false)]
    public void ExtraUsingFlagsBelowVersion_UsaElUmbralRealDePlrBodySerializer(int version, bool esperado)
    {
        var vm = NewLoadedViewModel(version);
        Assert.Equal(esperado, vm.Flags.ExtraUsingFlagsBelowVersion);
    }

    [Theory]
    [InlineData(183, true)] // umbral real 184 (no-Switch)
    [InlineData(184, false)]
    public void FinishedDD2EventBelowVersion_UsaElUmbralRealDePlrBodySerializer(int version, bool esperado)
    {
        var vm = NewLoadedViewModel(version);
        Assert.Equal(esperado, vm.Flags.FinishedDD2EventBelowVersion);
    }

    [Theory]
    [InlineData(252, true)] // umbral real 253
    [InlineData(253, false)]
    public void SuperMinecartBelowVersion_UsaElUmbralRealDePlrBodySerializer(int version, bool esperado)
    {
        var vm = NewLoadedViewModel(version);
        Assert.Equal(esperado, vm.Flags.SuperMinecartBelowVersion);
    }

    [Fact]
    public void EntrarEnDesbloqueosRecalculaElAvisoTrasCambiarLaVersion()
    {
        var vm = NewLoadedViewModel(100); // por debajo de todos los umbrales reales
        Assert.True(vm.Flags.ExtraAccessoryBelowVersion);

        vm.VersionEditor.RawVersion = 315; // sube a una version reciente real, sin tocar Desbloqueos todavia
        vm.PersonajeInnerTabIndex = 5; // Desbloqueos - dispara el recalculo real

        Assert.False(vm.Flags.ExtraAccessoryBelowVersion);
    }
}
