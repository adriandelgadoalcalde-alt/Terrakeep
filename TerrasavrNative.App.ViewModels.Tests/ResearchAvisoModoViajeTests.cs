using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// PB-14 (oleada de Personaje > Apariencia/Investigacion, 6-sep-2026) - BUG REAL: el aviso
// "investigar solo tiene efecto en Modo Viaje" de la pestaña Investigacion se calculaba UNA sola
// vez, en LoadFrom (IsJourneyMode = character.Difficulty == 3), y no volvia a mirar la
// dificultad nunca mas.
//
// La dificultad se edita en Apariencia, dos sub-pestañas al lado, asi que el caso es de lo mas
// normal: pones el personaje en Modo Viaje y la pestaña Investigacion sigue diciendo que no lo
// es (y al reves - lo quitas y el aviso no aparece). Misma familia exacta que el bug de la
// rejilla de Buffs y la version de esta misma oleada: un dato derivado que se congela en el
// momento de la carga.
public sealed class ResearchAvisoModoViajeTests
{
    private static MainViewModel Cargar(byte dificultad)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            Difficulty = dificultad,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"research-viaje-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void AlCargar_ElAvisoReflejaLaDificultadReal(byte dificultad, bool esperado)
    {
        var vm = Cargar(dificultad);
        Assert.Equal(esperado, vm.Research.IsJourneyMode);
    }

    [Fact]
    public void PonerElPersonajeEnModoViaje_QuitaElAvisoSinRecargar()
    {
        var vm = Cargar(0); // Clasico
        Assert.False(vm.Research.IsJourneyMode);

        vm.Appearance.Difficulty = 3; // Viaje, editado en Apariencia

        Assert.True(vm.Research.IsJourneyMode);
    }

    [Fact]
    public void QuitarElModoViaje_VuelveAPonerElAvisoSinRecargar()
    {
        var vm = Cargar(3); // Viaje
        Assert.True(vm.Research.IsJourneyMode);

        vm.Appearance.Difficulty = 2; // Extremo

        Assert.False(vm.Research.IsJourneyMode);
    }
}
