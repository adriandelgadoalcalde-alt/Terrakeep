using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), T-I: ItemEditViewModel reconstruia la rejilla de
// prefijos (Groups/Prefixes) en CUALQUIER cambio del slot seleccionado, incluidos IsSelected
// (marcar/desmarcar en la rejilla) y JustEdited (el flash real de "acabo de editarse", que se
// dispara y se apaga solo 450ms despues) - ninguno de los dos cambia que prefijos aplican,
// asi que reconstruir por ellos era trabajo (y parpadeo visual real) de sobra. Mismo bug real
// que B-6 en EquipmentGroupViewModel, y BuffEditViewModel ya lo evitaba desde el principio.
public sealed class ItemEditRefreshTests
{
    private const int IronHelmetId = 90;
    private const int MoltenHelmetId = 231;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"itemedit-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SeleccionarYFlashDeEdicion_NoReconstruyenLaRejillaDePrefijos_PeroCambiarDeObjetoSi()
    {
        var vm = NewLoadedViewModel();
        var headSlot = vm.EquipmentGroup!.EquippedItems.Slots[0];
        headSlot.PlaceItem(IronHelmetId);
        vm.ItemEdit.Slot = headSlot;

        int reconstrucciones = 0;
        vm.ItemEdit.Groups.CollectionChanged += (_, _) => reconstrucciones++;

        headSlot.IsSelected = true;
        headSlot.IsSelected = false;
        headSlot.TriggerEditFlash(); // JustEdited false->true, sincrono (el reset a 450ms no hace falta esperarlo aqui)

        Assert.Equal(0, reconstrucciones); // T-I: ninguno de los dos debe reconstruir la rejilla

        headSlot.PlaceItem(MoltenHelmetId); // cambio real de objeto - este si debe reconstruir

        Assert.True(reconstrucciones > 0);
    }
}
