using System.IO;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Tercera auditoria (Fable), tanda 1: H3-01/H3-02 ("Deshacer" - descarta ediciones sin
// preguntar, y no revierte un .tplr recien nacido), H3-03 (un rechazo de colocacion ensucia +
// flash falso), H3-04 ("Restaurar copia" sobre el personaje cargado no recarga el editor).
public sealed class Tanda1FableTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static string NuevaRutaTemporal() => Path.Combine(Path.GetTempPath(), $"tanda1-{Guid.NewGuid():N}.plr");

    [Fact]
    public void H301_DeshacerConCambiosSinGuardar_SiElHookCancela_NoDeshaceNiPierdeNada()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        vm.InventoryContainer!.Slots[0].PlaceItem(2); // Dirt Block, guardado real
        vm.SaveCommand.Execute(null); // deja un .bak real (sin el Dirt Block) y limpia IsDirty
        vm.InventoryContainer!.Slots[1].PlaceItem(3); // Stone Block - edicion SIN guardar, tras el guardado
        Assert.True(vm.IsDirty);

        vm.ConfirmDiscardChanges = () => false; // el usuario pulsa "Cancelar"
        vm.UndoLastSaveCommand.Execute(null);

        Assert.True(vm.IsDirty); // la edicion sin guardar SIGUE ahi - no se deshizo nada
        Assert.False(vm.InventoryContainer!.Slots[1].IsEmpty);

        File.Delete(path);
        File.Delete(path + ".bak");
    }

    [Fact]
    public void H301_DeshacerConCambiosSinGuardar_SiElHookConfirma_DeshaceDeVerdad()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.SaveCommand.Execute(null);
        vm.InventoryContainer!.Slots[1].PlaceItem(3); // sin guardar
        Assert.True(vm.IsDirty);

        vm.ConfirmDiscardChanges = () => true;
        vm.UndoLastSaveCommand.Execute(null);

        Assert.False(vm.IsDirty); // una recarga real nunca deja sucio
        // El .bak real es el estado ANTERIOR al guardado (sin el Dirt Block todavia) - Deshacer
        // vuelve a ESE estado, descartando tanto el guardado como la edicion sin guardar de encima.
        Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty);

        File.Delete(path);
    }

    [Fact]
    public void H301_DeshacerSinCambiosSinGuardar_NoConsultaElHook()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.SaveCommand.Execute(null); // IsDirty vuelve a false tras guardar
        Assert.False(vm.IsDirty);

        bool hookLlamado = false;
        vm.ConfirmDiscardChanges = () => { hookLlamado = true; return false; };
        vm.UndoLastSaveCommand.Execute(null);

        Assert.False(hookLlamado); // sin nada que perder, no hace falta ni preguntar
        Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty); // el deshacer si se aplico

        File.Delete(path);
    }

    [Fact]
    public void H302_DeshacerBorraElTplrHuerfanoNacidoEnEseMismoGuardado()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        vm.SaveCommand.Execute(null); // 1er guardado real, 100% vanilla - deja .plr.bak, NUNCA crea .tplr
        string tplrPath = Path.ChangeExtension(path, ".tplr");
        Assert.False(File.Exists(tplrPath));

        // Coloca un objeto REAL de Calamity y guarda - este 2º guardado crea el .tplr por
        // primera vez (sin .tplr.bak, WriteAtomic usa File.Move para un fichero nuevo).
        vm.InventoryContainer!.Slots[0].PlaceItem(CalamityIds.ItemIdBase);
        vm.SaveCommand.Execute(null);
        Assert.True(File.Exists(tplrPath));
        Assert.False(File.Exists(tplrPath + ".bak"));

        vm.UndoLastSaveCommand.Execute(null);

        Assert.False(File.Exists(tplrPath)); // el .tplr huerfano se borro - no vuelve a fusionarse
        Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty); // vuelve al estado 100% vanilla real

        File.Delete(path);
    }

    [Fact]
    public void H303_UnRechazoDeColocacionNoEnsuciaNiDisparaElFlash()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        Assert.False(vm.IsDirty);
        var slotMoneda = vm.CoinsContainer!.Slots[0]; // SlotKind.Coin real, rechaza cualquier cosa que no sea moneda

        slotMoneda.PlaceItem(2); // Dirt Block - invalido para este slot, rechazo real

        Assert.NotNull(slotMoneda.RejectionMessage);
        Assert.True(slotMoneda.IsEmpty); // el rechazo NO coloco nada
        Assert.False(vm.IsDirty); // sin ningun dato real cambiado, no deberia ensuciar
        Assert.False(slotMoneda.JustEdited); // ni disparar el flash de "acabo de editarme"

        File.Delete(path);
    }

    [Fact]
    public void H304_RestaurarCopiaSobreElPersonajeCargado_LoRecargaDeVerdad()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Original")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.SaveCommand.Execute(null); // deja un .bak real SIN el Dirt Block
        Assert.False(vm.InventoryContainer!.Slots[0].IsEmpty); // el editor tiene el objeto en memoria

        var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, DateTime.UtcNow);
        vm.Home.RestoreBackupCommand.Execute(entry);

        // H3-04: no basta con que el FICHERO se restaure - el editor (mismo personaje cargado)
        // debe reflejar de verdad el estado restaurado, no seguir mostrando el antiguo en memoria.
        Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty);

        File.Delete(path);
    }

    [Fact]
    public void H304_RestaurarCopiaDeOtroPersonajeNoCargado_NoTocaElEditorActual()
    {
        string pathCargado = NuevaRutaTemporal();
        File.WriteAllBytes(pathCargado, PlrFile.Write(NuevoPersonaje("Cargado")));
        var vm = new MainViewModel();
        vm.LoadFromPath(pathCargado);
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        Assert.True(vm.IsDirty);

        string pathOtro = NuevaRutaTemporal();
        File.WriteAllBytes(pathOtro, PlrFile.Write(NuevoPersonaje("Otro")));
        var svc = new CharacterFileService();
        var loadedOtro = svc.Load(pathOtro);
        loadedOtro.Character.Name = "OtroEditado";
        svc.Save(loadedOtro); // deja un .bak real de "Otro" para ese OTRO personaje

        var entryOtro = new CharacterListEntryViewModel(pathOtro, PlrFile.Read(File.ReadAllBytes(pathOtro)), false, DateTime.UtcNow);
        vm.Home.RestoreBackupCommand.Execute(entryOtro);

        // No es el personaje cargado - el editor (con su edicion sin guardar) no debe tocarse.
        Assert.Equal("Cargado", vm.CharacterName);
        Assert.False(vm.InventoryContainer!.Slots[0].IsEmpty);

        File.Delete(pathCargado);
        File.Delete(pathOtro);
        File.Delete(pathOtro + ".bak");
    }
}
