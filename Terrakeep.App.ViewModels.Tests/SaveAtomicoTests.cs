using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

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

    // Punto 3 de la lista de funciones nuevas (17-sep-2026, "validacion de integridad antes de
    // guardar"): la mitad Core del guardia (PlrFile.VerifyRoundTrip, con corrupcion real de
    // bytes) ya se prueba aislada en Terrakeep.Core.Tests/PlrFormat/PlrFileVerifyRoundTripTests.cs
    // - esta prueba cierra el otro extremo, el que de verdad importa para el usuario: que
    // CharacterFileService.Save, cableado con ese guardia, ABORTA DE VERDAD antes de tocar el
    // archivo real cuando los bytes recien serializados llegan corruptos por CUALQUIER motivo
    // (DebugCorruptPlrBytesBeforeVerify simula ese "cualquier motivo" - ver su comentario en
    // CharacterFileService, mismo criterio ya real de App.xaml.cs.ShouldForceSoftwareRendering:
    // sin InternalsVisibleTo configurado hacia el arnes, la unica forma honesta de forzar el
    // camino de aborto es un seam publico y documentado, nunca un booleano fingido).
    [Fact]
    public void Guardar_ConBytesCorruptosInyectados_AbortaSinTocarElArchivoOriginal()
    {
        var service = new CharacterFileService();
        string path = NuevaRutaTemporal();
        byte[] bytesOriginales = PlrFile.Write(NuevoPersonaje("Original"));
        File.WriteAllBytes(path, bytesOriginales);
        var loaded = service.Load(path);
        loaded.Character.Name = "Cambiado"; // edicion real en memoria - lo que se perderia si el guardado no abortara

        CharacterFileService.DebugCorruptPlrBytesBeforeVerify = bytes =>
        {
            byte[] corrupto = (byte[])bytes.Clone();
            corrupto[^1] ^= 0xFF; // ultimo byte del ultimo bloque AES-CBC -> padding PKCS7 invalido real
            return corrupto;
        };
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => service.Save(loaded));
            Assert.Contains("relectura", ex.Message);
        }
        finally
        {
            CharacterFileService.DebugCorruptPlrBytesBeforeVerify = null; // nunca debe quedar puesto para otras pruebas
        }

        // El archivo real en disco es BYTE A BYTE el mismo de antes del intento de guardado - ni
        // siquiera un .tmp a medias.
        Assert.Equal(bytesOriginales, File.ReadAllBytes(path));
        Assert.False(File.Exists(path + ".tmp"));
        Assert.False(File.Exists(path + ".bak")); // WriteAtomic nunca llego a ejecutarse

        File.Delete(path);
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
