using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H4-06 (cuarta auditoria de Opus, Fable): "la rejilla de Buffs no tiene contador ni
// operaciones en bloque, su gemela de objetos si" - BuffContainerViewModel gana el mismo
// DisplayName con recuento vivo y el mismo Vaciar todos/Deshacer real que ContainerViewModel,
// sin fusionar las dos clases (decision de diseño ya documentada en el propio fichero).
public sealed class BuffContainerCounterAndUndoTests
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
        string path = Path.Combine(Path.GetTempPath(), $"buff-container-h406-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void DisplayName_ReflejaElRecuentoRealYViveConCadaColocacion()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        int total = container.Slots.Count;

        Assert.Equal($"Buffs (0/{total})", container.DisplayName);

        container.Slots[0].PlaceBuff(1); // Obsidian Skin

        Assert.Equal($"Buffs (1/{total})", container.DisplayName);
    }

    [Fact]
    public void VaciarTodos_OfreceDeshacerConDuracionExacta()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        container.Slots[0].PlaceBuff(1);
        container.Slots[0].DurationSeconds = 555; // duracion CONCRETA, no el preset por defecto

        container.ClearAllCommand.Execute(null);

        Assert.True(container.CanUndoClear);
        Assert.Equal(1, container.LastClearedCount);
        Assert.True(container.Slots[0].IsEmpty);

        container.UndoClearCommand.Execute(null);

        Assert.Equal(1, container.Slots[0].Buff.Id);
        Assert.Equal(555 * 60, container.Slots[0].Buff.Time); // exacta, no re-aproximada
        Assert.False(container.CanUndoClear);
    }

    [Fact]
    public void VaciarUnContenedorYaVacio_NoOfreceDeshacerFalso()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;

        container.ClearAllCommand.Execute(null);

        Assert.False(container.CanUndoClear);
    }
}
