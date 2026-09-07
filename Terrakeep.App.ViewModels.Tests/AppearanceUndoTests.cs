using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// A9-09-UNDOPELO (informe de pulido final, C-15, cierra A1): "cambiar peinado + Deshacer ->
// HairStyle vuelve, y no empuja entrada nueva". Apariencia era la unica pestaña real de edicion
// sin Deshacer/Rehacer - empuja al MISMO UndoStack compartido de MainViewModel (via callback,
// PushAppearanceUndo). Solo cubre los cambios DISCRETOS aqui (peinado/tinte/genero/dificultad -
// un clic/seleccion, sin debounce real de por medio) - los campos continuos (vida/mana/horas/
// colores, con debounce de ~400ms via DispatcherTimer) necesitan el Dispatcher real bombeando
// para disparar, y se verifican en el arnes de UI Automation (que SI tiene esa maquinaria).
public sealed class AppearanceUndoTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static MainViewModel NuevoCargado(string nombre = "Test")
    {
        string path = Path.Combine(Path.GetTempPath(), $"appearance-undo-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje(nombre)));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void CambiarPeinado_Deshacer_VuelveAlPeinadoAnteriorYNoEmpujaEntradaNueva()
    {
        var vm = NuevoCargado();
        int original = vm.Appearance.HairStyle;
        Assert.False(vm.UndoStack.CanUndo);

        vm.Appearance.SelectHairCommand.Execute(original + 1);

        Assert.Equal(original + 1, vm.Appearance.HairStyle);
        Assert.True(vm.UndoStack.CanUndo);
        Assert.False(vm.UndoStack.CanRedo);

        vm.UndoEditCommand.Execute(null);

        Assert.Equal(original, vm.Appearance.HairStyle);
        Assert.False(vm.UndoStack.CanUndo); // "no empuja entrada nueva" - el propio Deshacer no se re-graba a si mismo
        Assert.True(vm.UndoStack.CanRedo);

        vm.RedoEditCommand.Execute(null);

        Assert.Equal(original + 1, vm.Appearance.HairStyle);
        Assert.True(vm.UndoStack.CanUndo);
        Assert.False(vm.UndoStack.CanRedo); // idem para Rehacer
    }

    [Fact]
    public void CambiarTinteDePelo_Deshacer_Restaura()
    {
        var vm = NuevoCargado();
        int original = vm.Appearance.HairDye;

        vm.Appearance.SelectHairDyeCommand.Execute(original + 1);
        Assert.True(vm.UndoStack.CanUndo);

        vm.UndoEditCommand.Execute(null);

        Assert.Equal(original, vm.Appearance.HairDye);
    }

    [Fact]
    public void CambiarGenero_Deshacer_Restaura()
    {
        var vm = NuevoCargado();
        bool original = vm.Appearance.IsMale;

        vm.Appearance.IsMale = !original;
        Assert.True(vm.UndoStack.CanUndo);

        vm.UndoEditCommand.Execute(null);

        Assert.Equal(original, vm.Appearance.IsMale);
    }

    [Fact]
    public void CambiarDificultad_Deshacer_Restaura()
    {
        var vm = NuevoCargado();
        int original = vm.Appearance.Difficulty;

        vm.Appearance.Difficulty = (original + 1) % 4;
        Assert.True(vm.UndoStack.CanUndo);

        vm.UndoEditCommand.Execute(null);

        Assert.Equal(original, vm.Appearance.Difficulty);
    }

    [Fact]
    public void CargarOtroPersonaje_DescartaElHistorialDeApariencia()
    {
        var vm = NuevoCargado("Uno");
        vm.Appearance.SelectHairCommand.Execute(vm.Appearance.HairStyle + 1);
        Assert.True(vm.UndoStack.CanUndo);

        string path2 = Path.Combine(Path.GetTempPath(), $"appearance-undo-2-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(NuevoPersonaje("Dos")));
        vm.LoadFromPath(path2);
        File.Delete(path2);

        Assert.False(vm.UndoStack.CanUndo);
    }
}
