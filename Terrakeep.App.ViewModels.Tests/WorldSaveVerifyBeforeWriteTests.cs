using System.IO;
using Terrakeep.App.Services;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.App.ViewModels.Tests;

// Punto 3 de la lista de funciones nuevas (17-sep-2026, "validacion de integridad antes de
// guardar"): WorldFileService ya tenia una relectura de verificacion real desde ANTES de esta
// sesion (SaveGameMode/SaveSpawnPoint/SaveTimeAndMoon/SaveBossFlags/SaveChestItems/SaveSignText),
// pero relia el archivo YA ESCRITO EN DISCO tras WriteAtomic - suficiente para confirmar que la
// escritura salio bien, pero no evitaba tocar el .wld real si el patch en si hubiera quedado mal
// formado. Se reordeno para releer `patched` EN MEMORIA antes de WriteAtomic (mismo criterio
// ahora real tambien para el .plr, ver PlrFileVerifyRoundTripTests en Core.Tests). Esta prueba
// confirma el extremo que de verdad importa: que el guardado aborta de verdad SIN TOCAR el
// archivo real cuando esos bytes en memoria llegan corruptos por cualquier motivo real
// (DebugCorruptPatchedBytesBeforeVerify - mismo seam publico y documentado que
// CharacterFileService.DebugCorruptPlrBytesBeforeVerify, sin InternalsVisibleTo configurado hacia
// el arnes).
public sealed class WorldSaveVerifyBeforeWriteTests : IDisposable
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"terrakeep-worldsave-test-{Guid.NewGuid():N}.wld");

    public void Dispose()
    {
        WorldFileService.DebugCorruptPatchedBytesBeforeVerify = null; // nunca debe quedar puesto para otras pruebas
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
        if (File.Exists(_tempPath + ".bak")) File.Delete(_tempPath + ".bak");
        if (File.Exists(_tempPath + ".tmp")) File.Delete(_tempPath + ".tmp");
    }

    // Mismo criterio real ya usado por WldWriterChestSignTests (Core.Tests): probar sobre un
    // mundo REAL de este PC cuando hay uno con cofres a mano, y documentar el limite honesto si
    // no lo hay - nunca fabricar un mundo falso solo para no depender del entorno.
    private static string? FindRealWorldWithChests()
    {
        if (!Directory.Exists(WorldsDir)) return null;
        foreach (var name in new[] { "roca_negra.wld", "Afueras_de_Larvas_de_gusano.wld", "adriandres.wld", "El_Musgo_de_Accidentes.wld", "ahora_si_que_si.wld", "kmmiu.wld" })
        {
            string path = Path.Combine(WorldsDir, name);
            if (!File.Exists(path)) continue;
            var world = WldReader.Read(File.ReadAllBytes(path));
            if (world.Chests.Count > 0) return path;
        }
        return null;
    }

    [Fact]
    public void SaveChestItems_ConBytesCorruptosInyectados_AbortaSinTocarElArchivoOriginal()
    {
        string? sourcePath = FindRealWorldWithChests();
        if (sourcePath == null) return; // LIMITE REAL: sin ningun mundo con cofres en esta maquina, nada que verificar

        File.Copy(sourcePath, _tempPath, overwrite: true);
        byte[] bytesOriginales = File.ReadAllBytes(_tempPath);
        var world = WldReader.Read(bytesOriginales);
        var newItems = new List<WldChestItem> { new(NetId: 1, Stack: 1, Prefix: 0) };

        WorldFileService.DebugCorruptPatchedBytesBeforeVerify = bytes =>
        {
            byte[] corrupto = (byte[])bytes.Clone();
            // Revienta la cabecera entera (firma "relogic", version, tabla de punteros) - la
            // corrupcion mas barata y mas fiable de "esto ya no es un .wld valido", sin
            // depender de en que offset exacto cae cada seccion tras editar un cofre concreto.
            for (int i = 0; i < Math.Min(64, corrupto.Length); i++) corrupto[i] ^= 0xFF;
            return corrupto;
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WorldFileService.SaveChestItems(world, _tempPath, 0, newItems));
        Assert.Contains("relectura", ex.Message);

        // El archivo real en disco es BYTE A BYTE el mismo de antes del intento de guardado.
        Assert.Equal(bytesOriginales, File.ReadAllBytes(_tempPath));
        Assert.False(File.Exists(_tempPath + ".tmp"));
        Assert.False(File.Exists(_tempPath + ".bak")); // WriteAtomic nunca llego a ejecutarse
    }

    // Regresion real del reordenamiento (verificar ANTES de escribir en vez de DESPUES): un
    // guardado normal, sin ninguna corrupcion, tiene que seguir funcionando exactamente igual
    // que antes de este cambio - el cofre editado persiste de verdad en disco.
    [Fact]
    public void SaveChestItems_SinCorromper_PersisteEnDiscoTrasElReorden()
    {
        string? sourcePath = FindRealWorldWithChests();
        if (sourcePath == null) return; // LIMITE REAL: sin ningun mundo con cofres en esta maquina

        File.Copy(sourcePath, _tempPath, overwrite: true);
        var world = WldReader.Read(File.ReadAllBytes(_tempPath));
        var newItems = new List<WldChestItem> { new(NetId: 71, Stack: 5, Prefix: 0) };

        var updated = WorldFileService.SaveChestItems(world, _tempPath, 0, newItems);

        Assert.Single(updated.Chests[0].Items);
        Assert.Equal(71, updated.Chests[0].Items[0].NetId);

        var reReadFromDisk = WldReader.Read(File.ReadAllBytes(_tempPath));
        Assert.Single(reReadFromDisk.Chests[0].Items);
        Assert.Equal(71, reReadFromDisk.Chests[0].Items[0].NetId);
    }
}
