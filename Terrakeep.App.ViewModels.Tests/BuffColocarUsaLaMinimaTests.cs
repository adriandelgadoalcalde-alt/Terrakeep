using System.IO;
using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// PB-15 (oleada de Personaje > Buffs, 6-sep-2026). Colocar un buff fija su duracion "Minima",
// que es lo que el propio codigo declaraba desde el principio ("duracion inicial al minimo real
// de ese buff") - pero para Calamity se bifurcaba a 600*60 ticks (10 min) puestos a ojo,
// mientras el panel Editar le ofrecia una "Minima" de 28.800 ticks (8 min). Resultado real:
// colocabas un buff de Calamity y pulsar "Minima" te BAJABA la duracion, justo al reves de lo
// que ese boton promete, con un 10 que no salia de ninguna fuente.
public sealed class BuffColocarUsaLaMinimaTests
{
    private static MainViewModel Cargar()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buff-minima-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    // Vanilla: la duracion al colocar ya coincidia con "Minima" - se fija para que no se pierda.
    [Fact]
    public void UnBuffVanilla_SeColocaConSuMinimaReal()
    {
        var vm = Cargar();
        var slot = vm.Buffs.Container!.Slots[0];
        vm.SelectBuffSlot(slot);
        Assert.True(slot.PlaceBuff(1)); // Piel de obsidiana

        int alColocar = slot.Buff.Time;
        vm.BuffEdit.ApplyMinCommand.Execute(null);

        Assert.Equal(alColocar, slot.Buff.Time);
        Assert.True(alColocar > 0);
    }

    // Calamity: el caso que estaba mal. Colocar tiene que dejar exactamente la misma duracion
    // que ofrece el boton "Minima" de ese mismo buff.
    [Fact]
    public void UnBuffDeCalamity_SeColocaConLaMismaMinimaQueOfreceElBoton()
    {
        var vm = Cargar();
        int idCalamity = vm.BuffLibrary.RootCategories
            .SelectMany(RecorrerIds)
            .First(id => id >= CalamityIds.BuffIdBase);

        var slot = vm.Buffs.Container!.Slots[0];
        vm.SelectBuffSlot(slot);
        Assert.True(slot.PlaceBuff(idCalamity));

        int alColocar = slot.Buff.Time;
        vm.BuffEdit.ApplyMinCommand.Execute(null);

        Assert.Equal(alColocar, slot.Buff.Time);
        Assert.True(alColocar > 0);
    }

    private static IEnumerable<int> RecorrerIds(CategoryNodeViewModel nodo)
    {
        foreach (int id in nodo.ItemIdsOrdered) yield return id;
        foreach (var hijo in nodo.Children)
            foreach (int id in RecorrerIds(hijo)) yield return id;
    }
}
