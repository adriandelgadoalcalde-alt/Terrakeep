using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H3-17 (tercera auditoria de Opus, Fable): "Movido 1 objeto(s)" no concordaba de verdad - el
// verbo ya se conjugaba (Movido/Movidos) pero el sustantivo se quedaba en el placeholder
// literal "objeto(s)" sin concordar nunca. Ahora concuerdan los dos de verdad.
public sealed class MoverAlAlmacenConcordanciaTests
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
        string path = Path.Combine(Path.GetTempPath(), $"mover-almacen-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void MoverUnSoloObjeto_ConcuerdaEnSingular()
    {
        var vm = NewLoadedViewModel();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);

        vm.MoveInventoryToStorageCommand.Execute(null);

        Assert.Equal("Movido 1 objeto al almacén seleccionado - pulsa Guardar para conservarlo.", vm.StatusMessage);
    }

    [Fact]
    public void MoverVariosObjetos_ConcuerdaEnPlural()
    {
        var vm = NewLoadedViewModel();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.InventoryContainer!.Slots[1].PlaceItem(9);

        vm.MoveInventoryToStorageCommand.Execute(null);

        Assert.Equal("Movidos 2 objetos al almacén seleccionado - pulsa Guardar para conservarlo.", vm.StatusMessage);
    }
}
