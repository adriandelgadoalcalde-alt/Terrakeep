using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Cuarta auditoria de Opus (Fable), Tanda 1: H4-03 (la tarjeta "Librería" de Inicio aterrizaba
// con la Libreria plegada) y H4-13 (Ctrl+F, portado a GoToContextualLibraryCommand, solo
// conocia la Libreria de objetos - ahora es contextual, salta a la de Buffs si esa es la
// pestaña interna activa).
public sealed class CuartaAuditoriaTanda1Tests
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
        string path = Path.Combine(Path.GetTempPath(), $"tanda1-h4-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void H403_IrATabLibreriaDesdeLaTarjetaDeInicio_LaDespliegaDeVerdad()
    {
        var vm = NewLoadedViewModel();
        Assert.True(vm.IsLibraryCollapsed); // plegada por omision, el caso real reportado

        vm.GoToTabCommand.Execute("Libreria");

        Assert.False(vm.IsLibraryCollapsed);
    }

    [Fact]
    public void H403_IrATabPersonaje_NoTocaElPlegadoDeLaLibreria()
    {
        // "Personaje" comparte rama con "Libreria" en GoToTab - no debe desplegar de mas.
        var vm = NewLoadedViewModel();
        Assert.True(vm.IsLibraryCollapsed);

        vm.GoToTabCommand.Execute("Personaje");

        Assert.True(vm.IsLibraryCollapsed);
    }

    [Fact]
    public void H413_ContextualEnObjetos_DespliegaLaLibreriaDeObjetos()
    {
        var vm = NewLoadedViewModel();
        Assert.False(vm.IsBuffsInnerTabActive); // Objetos es la pestaña interna por omision

        vm.GoToContextualLibraryCommand.Execute(null);

        Assert.False(vm.IsLibraryCollapsed);
        Assert.True(vm.IsBuffLibraryCollapsed); // la de buffs no se toca
    }

    [Fact]
    public void H413_ContextualEnBuffs_DespliegaLaLibreriaDeBuffs()
    {
        var vm = NewLoadedViewModel();
        vm.PersonajeInnerTabIndex = 1; // Buffs
        Assert.True(vm.IsBuffsInnerTabActive);

        vm.GoToContextualLibraryCommand.Execute(null);

        Assert.False(vm.IsBuffLibraryCollapsed);
        Assert.True(vm.IsLibraryCollapsed); // la de objetos no se toca
    }
}
