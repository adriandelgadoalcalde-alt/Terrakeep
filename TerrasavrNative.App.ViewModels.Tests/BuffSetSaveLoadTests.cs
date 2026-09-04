using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de guardar json
// ni tampoco cargar para guardar combinaciones de buff" - gemelo real de las pruebas de
// Guardar/Cargar conjunto de OBJETOS (H5-03) para MainViewModel.SaveBuffSet/LoadBuffSet.
public sealed class BuffSetSaveLoadTests
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
        string path = Path.Combine(Path.GetTempPath(), $"buffset-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void GuardarYCargar_ConjuntoDeBuffsReal_SeRestauraElMismoContenido()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        container.Slots[0].PlaceBuff(1); // Obsidian Skin, id real vanilla
        container.Slots[5].PlaceBuff(2); // Regeneration, id real vanilla
        var idsAntes = container.Slots.Select(s => s.Buff.Id).ToArray();

        string path = Path.Combine(Path.GetTempPath(), $"buffset-guardado-{Guid.NewGuid():N}.json");
        try
        {
            vm.SaveBuffSet(container, path);
            Assert.True(File.Exists(path));

            container.ClearAllCommand.Execute(null); // vacia de verdad antes de cargar, para probar el reemplazo real
            vm.LoadBuffSet(container, path, append: false);

            var idsDespues = container.Slots.Select(s => s.Buff.Id).ToArray();
            Assert.Equal(idsAntes, idsDespues);
            Assert.Equal(21600, container.Slots[0].Buff.Time); // duracion minima real de Obsidian Skin (360s), conservada
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Añadir_NoDuplicaUnBuffYaActivoEnOtroSlot()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        container.Slots[0].PlaceBuff(1); // ya activo de verdad

        var fileVm = NewLoadedViewModel(); // conjunto real guardado desde OTRO personaje, con el MISMO buff
        fileVm.Buffs.Container!.Slots[0].PlaceBuff(1);
        string path = Path.Combine(Path.GetTempPath(), $"buffset-duplicado-{Guid.NewGuid():N}.json");
        try
        {
            fileVm.SaveBuffSet(fileVm.Buffs.Container!, path);

            vm.LoadBuffSet(container, path, append: true);

            // Bu-b real (Terraria no permite dos instancias del mismo buff): el buff ya activo
            // en el slot 0 se queda tal cual, NO se duplica en ningun otro slot.
            Assert.Equal(1, container.Slots.Count(s => s.Buff.Id == 1));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CargarConjunto_MarcaElPersonajeComoModificado()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        container.Slots[0].PlaceBuff(1);
        string path = Path.Combine(Path.GetTempPath(), $"buffset-dirty-{Guid.NewGuid():N}.json");
        try
        {
            vm.SaveBuffSet(container, path);
            var vmLimpio = NewLoadedViewModel();
            Assert.False(vmLimpio.IsDirty);

            vmLimpio.LoadBuffSet(vmLimpio.Buffs.Container!, path, append: false);

            Assert.True(vmLimpio.IsDirty);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CargarConjunto_SeCancelaConDeshacerEnUnaSolaEntrada()
    {
        var vm = NewLoadedViewModel();
        var container = vm.Buffs.Container!;
        container.Slots[0].PlaceBuff(1);
        string path = Path.Combine(Path.GetTempPath(), $"buffset-undo-{Guid.NewGuid():N}.json");
        try
        {
            vm.SaveBuffSet(container, path);
            container.ClearAllCommand.Execute(null);
            int entradasAntes = vm.UndoStack.Entries.Count;

            vm.LoadBuffSet(container, path, append: false);

            Assert.Equal(entradasAntes + 1, vm.UndoStack.Entries.Count); // una UNICA entrada para todo el conjunto
            Assert.True(vm.UndoEditCommand.CanExecute(null));
            vm.UndoEditCommand.Execute(null);
            Assert.True(container.Slots[0].IsEmpty); // vuelve al estado de antes de cargar (vacio)
        }
        finally { File.Delete(path); }
    }
}
