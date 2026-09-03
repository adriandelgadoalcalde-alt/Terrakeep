using System.IO;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), T-C: escritura atomica real (WriteAtomic - .tmp + un solo
// intercambio de sistema de ficheros, nunca un fichero real a medio escribir), .tplr solo
// cuando ya existia o hay contenido real de Calamity (antes se creaba SIEMPRE, incluso para un
// personaje 100% vanilla), y el .bak real (ya existia desde el Bloque 0) expuesto de verdad en
// la UI via UndoLastSaveCommand.
public sealed class SaveAtomicoTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static string NuevaRutaTemporal() => Path.Combine(Path.GetTempPath(), $"savetest-{Guid.NewGuid():N}.plr");

    [Fact]
    public void Guardar_UnPersonaje100PorCientoVainilla_NoCreaTplr()
    {
        var service = new CharacterFileService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Vainilla")));
        var loaded = service.Load(path);

        service.Save(loaded);

        Assert.False(File.Exists(Path.ChangeExtension(path, ".tplr")));
        File.Delete(path);
        File.Delete(path + ".bak");
    }

    [Fact]
    public void Guardar_ConUnBuffRealDeCalamity_SiCreaTplr()
    {
        var service = new CharacterFileService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("ConCalamity")));
        var loaded = service.Load(path);
        loaded.Character.Buffs[0] = new PlrBuff { Id = CalamityIds.BuffIdBase, Time = 100 };

        service.Save(loaded);

        string tplrPath = Path.ChangeExtension(path, ".tplr");
        Assert.True(File.Exists(tplrPath));
        File.Delete(path);
        File.Delete(path + ".bak");
        File.Delete(tplrPath);
    }

    [Fact]
    public void Guardar_DejaBakConElContenidoAnterior_YSinTmpSueltos()
    {
        var service = new CharacterFileService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("V1")));

        var loaded = service.Load(path);
        loaded.Character.Name = "V2";
        service.Save(loaded);

        string bak = path + ".bak";
        Assert.True(File.Exists(bak));
        Assert.Equal("V1", PlrFile.Read(File.ReadAllBytes(bak)).Name); // el .bak es el contenido ANTERIOR
        Assert.Equal("V2", PlrFile.Read(File.ReadAllBytes(path)).Name); // el real ya es el nuevo
        Assert.False(File.Exists(path + ".tmp")); // WriteAtomic no deja ningun .tmp suelto

        File.Delete(path);
        File.Delete(bak);
    }

    [Fact]
    public void DeshacerUltimoGuardado_RestauraElEstadoAnteriorYRecarga()
    {
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        Assert.False(vm.UndoLastSaveCommand.CanExecute(null)); // recien cargado, ningun guardado real que deshacer todavia

        vm.EquipmentGroup!.EquippedItems.Slots[0].PlaceItem(90); // Iron Helmet, id real
        Assert.True(vm.IsDirty);
        vm.SaveCommand.Execute(null); // guarda CON el casco puesto - el .bak real queda SIN el
        Assert.True(vm.UndoLastSaveCommand.CanExecute(null));

        vm.UndoLastSaveCommand.Execute(null);

        Assert.True(vm.EquipmentGroup!.EquippedItems.Slots[0].IsEmpty); // vuelve a como estaba ANTES del guardado
        Assert.False(vm.IsDirty); // una recarga real nunca deja sucio

        File.Delete(path);
        File.Delete(path + ".bak");
    }
}
