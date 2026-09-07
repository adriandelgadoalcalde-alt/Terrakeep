using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H4-05 (cuarta auditoria de Opus, Fable): "Vaciar contenedor" era la unica accion realmente
// destructiva de la app sin ninguna vuelta atras - ahora guarda una instantanea real (slot
// original incluido, no solo la lista de objetos) y ofrece Deshacer durante unos segundos.
public sealed class UndoClearContainerTests
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
        string path = Path.Combine(Path.GetTempPath(), $"undo-clear-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void VaciarConObjetosReales_OfreceDeshacerConElRecuentoReal()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(2);
        inv.Slots[5].PlaceItem(9);

        inv.ClearAllCommand.Execute(null);

        Assert.True(inv.CanUndoClear);
        Assert.Equal(2, inv.LastClearedCount);
        Assert.All(inv.Slots, s => Assert.True(s.IsEmpty));
    }

    [Fact]
    public void Deshacer_RestauraCadaObjetoEnSuSlotOriginalExacto()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(2);
        inv.Slots[5].PlaceItem(9);
        inv.ClearAllCommand.Execute(null);

        inv.UndoClearCommand.Execute(null);

        Assert.Equal(2, inv.Slots[0].ItemId);
        Assert.Equal(9, inv.Slots[5].ItemId);
        Assert.False(inv.CanUndoClear); // el propio Deshacer se consume a si mismo
    }

    [Fact]
    public void VaciarUnContenedorYaVacio_NoOfreceDeshacerFalso()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;

        inv.ClearAllCommand.Execute(null);

        Assert.False(inv.CanUndoClear); // nada real que deshacer, no fingir la opcion
    }

    [Fact]
    public void VaciarDosVecesSeguidas_LaSegundaInstantaneaSustituyeALaPrimera()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(2);
        inv.ClearAllCommand.Execute(null); // 1 objeto real vaciado
        inv.Slots[3].PlaceItem(9);
        inv.ClearAllCommand.Execute(null); // 1 objeto DISTINTO vaciado - sustituye la instantanea

        inv.UndoClearCommand.Execute(null);

        Assert.True(inv.Slots[0].IsEmpty); // el primero, YA descartado, no vuelve
        Assert.Equal(9, inv.Slots[3].ItemId); // solo el segundo vaciado se deshace
    }
}
