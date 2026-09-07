using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Primer escritor real de .wld del proyecto (WldWriter.PatchGameMode) - pedido explicito del
// usuario (5-sep-2026): poder cambiar la dificultad del mundo (Clasico/Experto/Maestro/Viaje).
// Riesgo real distinto a cualquier otra prueba de este proyecto: un fallo aqui puede corromper
// el ARCHIVO DE MUNDO real de un usuario, no solo un valor en memoria - de ahi la prueba mas
// estricta del fichero (Patch_SoloCambiaLosBytesDeGameMode_NingunOtroByteSeToca), que compara
// el array de bytes COMPLETO byte a byte contra el original, no solo el header re-leido.
public class WldWriterTests
{
    // Mismo orden de campos exacto que WldReader.ReadHeader (privado) - version reducida de la
    // ya usada en WldTileEntityReaderTests.WriteHeaderTo, aqui solo hasta DungeonY (lo unico que
    // WldReader.ReadHeader(byte[]) publico necesita para funcionar, ver su propio comentario de
    // "se detiene justo despues de GroundLevel/RockLevel").
    private static byte[] BuildHeaderBytes(uint version, int gameMode, string title = "Mundo sintetico", string seed = "semilla", int worldId = 1, int tilesWide = 100, int tilesHigh = 100, int spawnX = 5, int spawnY = 5, int dungeonX = 10, int dungeonY = 10)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(version);
        w.Write("relogic".ToCharArray());
        w.Write((byte)2); // fileType = mundo
        w.Write((uint)1); // FileRevision
        w.Write((long)0); // banderas de favorito

        var pointers = new int[] { 0, 0, 0, 0, 0 };
        w.Write((short)pointers.Length);
        foreach (int p in pointers) w.Write(p);

        w.Write((short)0); // tileFrameImportant: longitud 0

        w.Write(title);
        if (version == 179) w.Write(int.Parse(seed)); else w.Write(seed);
        w.Write(new byte[8]); // WorldGenVersion
        if (version >= 181) w.Write(new byte[16]); // WorldGUID
        w.Write(worldId);
        w.Write(new byte[4 * 4]); // Left/Right/Top/BottomWorld

        w.Write(tilesHigh);
        w.Write(tilesWide);

        if (version >= 209)
        {
            w.Write(gameMode);
            if (version >= 222) w.Write(false);
            if (version >= 227) w.Write(false);
            if (version >= 238) w.Write(false);
            if (version >= 239) w.Write(false);
            if (version >= 241) w.Write(false);
            if (version >= 249) w.Write(false);
            if (version >= 266) w.Write(false);
            if (version >= 267) w.Write(false);
            if (version >= 302) w.Write(false);
        }
        else if (version == 208)
        {
            w.Write(gameMode == 2);
        }
        else if (version >= 112)
        {
            w.Write(gameMode == 1);
        }

        if (version >= 141) w.Write(new byte[8]); // CreationTime
        if (version >= 284) w.Write(new byte[8]); // LastPlayed
        w.Write((byte)0); // MoonType
        w.Write(new byte[4 * 3]); // TreeX
        w.Write(new byte[4 * 4]); // TreeStyle
        w.Write(new byte[4 * 3]); // CaveBackX
        w.Write(new byte[4 * 4]); // CaveBackStyle
        w.Write(new byte[4 * 3]); // Ice/Jungle/HellBackStyle

        w.Write(spawnX);
        w.Write(spawnY);
        w.Write(50.0); // groundLevel
        w.Write(100.0); // rockLevel
        w.Write(0.0); // Time
        w.Write(false); // DayTime
        w.Write(0); // MoonPhase
        w.Write(false); // BloodMoon
        w.Write(false); // IsEclipse
        w.Write(dungeonX);
        w.Write(dungeonY);

        w.Flush();
        return ms.ToArray();
    }

    [Theory]
    [InlineData(0)] // Clasico
    [InlineData(1)] // Experto
    [InlineData(2)] // Maestro
    [InlineData(3)] // Viaje
    public void PatchGameMode_Version279_CambiaSoloElModo(int nuevoModo)
    {
        byte[] original = BuildHeaderBytes(version: 279, gameMode: 0);
        byte[] patched = WldWriter.PatchGameMode(original, nuevoModo);

        var headerAntes = WldReader.ReadHeader(original);
        var headerDespues = WldReader.ReadHeader(patched);

        Assert.Equal(nuevoModo, headerDespues.GameMode);
        Assert.Equal(headerAntes.Title, headerDespues.Title);
        Assert.Equal(headerAntes.WorldId, headerDespues.WorldId);
        Assert.Equal(headerAntes.Seed, headerDespues.Seed);
        Assert.Equal(headerAntes.TilesWide, headerDespues.TilesWide);
        Assert.Equal(headerAntes.TilesHigh, headerDespues.TilesHigh);
        Assert.Equal(headerAntes.SpawnX, headerDespues.SpawnX);
        Assert.Equal(headerAntes.SpawnY, headerDespues.SpawnY);
        Assert.Equal(headerAntes.DungeonX, headerDespues.DungeonX);
        Assert.Equal(headerAntes.DungeonY, headerDespues.DungeonY);
    }

    // La prueba mas estricta de todas (ver comentario de la clase): el archivo de un usuario
    // real no debe perder NADA que no sea el propio campo pedido, ni siquiera un byte suelto en
    // una seccion que WldReader.ReadHeader no llega a leer. Compara el array COMPLETO devuelto
    // por PatchGameMode contra el original, byte a byte, y exige que la UNICA diferencia caiga
    // dentro de la ventana real de 4 bytes de GameMode (version >= 209 aqui).
    [Fact]
    public void Patch_SoloCambiaLosBytesDeGameMode_NingunOtroByteSeToca()
    {
        byte[] original = BuildHeaderBytes(version: 279, gameMode: 0, title: "Mi Mundo de Verdad", seed: "-123456789");
        byte[] patched = WldWriter.PatchGameMode(original, 2);

        Assert.Equal(original.Length, patched.Length);
        var diffIndices = Enumerable.Range(0, original.Length).Where(i => original[i] != patched[i]).ToList();
        Assert.True(diffIndices.Count <= 4, $"Se esperaban como mucho 4 bytes distintos (el Int32 de GameMode), se encontraron {diffIndices.Count}: {string.Join(",", diffIndices)}");
        // Los bytes que cambian tienen que ser consecutivos (un unico Int32), nunca dispersos.
        if (diffIndices.Count > 0)
            Assert.Equal(diffIndices.First() + diffIndices.Count - 1, diffIndices.Last());
    }

    [Fact]
    public void PatchGameMode_Version208_SoloAdmiteClasicoYMaestro()
    {
        byte[] original = BuildHeaderBytes(version: 208, gameMode: 0);

        byte[] aMaestro = WldWriter.PatchGameMode(original, 2);
        Assert.Equal(2, WldReader.ReadHeader(aMaestro).GameMode);

        byte[] aClasico = WldWriter.PatchGameMode(aMaestro, 0);
        Assert.Equal(0, WldReader.ReadHeader(aClasico).GameMode);

        Assert.Throws<NotSupportedException>(() => WldWriter.PatchGameMode(original, 1)); // Experto no existia en v208
        Assert.Throws<NotSupportedException>(() => WldWriter.PatchGameMode(original, 3)); // Viaje tampoco
    }

    [Fact]
    public void PatchGameMode_Version150_SoloAdmiteClasicoYExperto()
    {
        byte[] original = BuildHeaderBytes(version: 150, gameMode: 0);

        byte[] aExperto = WldWriter.PatchGameMode(original, 1);
        Assert.Equal(1, WldReader.ReadHeader(aExperto).GameMode);

        Assert.Throws<NotSupportedException>(() => WldWriter.PatchGameMode(original, 2)); // Maestro no existia todavia
    }

    [Fact]
    public void PatchGameMode_VersionMuyAntigua_Lanza()
    {
        byte[] original = BuildHeaderBytes(version: 71, gameMode: 0);
        Assert.Throws<NotSupportedException>(() => WldWriter.PatchGameMode(original, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void PatchGameMode_ValorFueraDeRango_Lanza(int modoInvalido)
    {
        byte[] original = BuildHeaderBytes(version: 279, gameMode: 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => WldWriter.PatchGameMode(original, modoInvalido));
    }
}
