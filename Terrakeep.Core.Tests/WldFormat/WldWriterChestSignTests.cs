using System.Text;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Editor de cofres/letreros v1 (T1 del documento I+D real, "Terrakeep, editor de cofres/
// letreros del .wld", 15-sep-2026): WldWriter.WriteChestItems/WriteSignText son el primer
// escritor del proyecto que CAMBIA LA LONGITUD del archivo (todo lo demas en WldWriter parchea
// un tramo de ancho fijo) - el riesgo real no es solo "el cofre quedo mal", es "la tabla de
// punteros quedo desincronizada y el resto del mundo se volvio ilegible". Dos tandas de pruebas:
// 1) un .wld SINTETICO construido a mano (misma tecnica que WldTileEntityReaderTests) para poder
//    fijar con precision el contenido exacto (capacidad por cofre, letreros fantasma, ancho de
//    slots) sin depender de que casualidad trae un mundo real; 2) un .wld REAL de este PC, editado
//    sobre una COPIA (nunca el original) y RELEIDO DE VERDAD DESDE DISCO, para la unica prueba
//    que de verdad demuestra que un cofre editado persiste sin corromper el resto del mundo.
public class WldWriterChestSignTests : IDisposable
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"terrakeep-chest-test-{Guid.NewGuid():N}.wld");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    // ---------------------------------------------------------------------------------------
    // Parte 1: mundo sintetico de 3x1 tiles, controlado byte a byte.
    //   x=0: tile inactivo.
    //   x=1: tile ACTIVO tipo 55 (Sign real) - aqui vive el letrero "de verdad".
    //   x=2: tile inactivo.
    // 2 cofres (uno con objetos reales y capacidad 40, otro vacio con capacidad 5 - version>=294,
    // cada cofre con SU PROPIA capacidad real) y 2 letreros (uno real sobre el tile x=1, otro
    // "fantasma" en una coordenada fuera del mundo - mismo criterio que WldReader.ReadSigns usa
    // para descartarlos al leer, sin que eso signifique que hay que BORRARLOS al reescribir).
    // ---------------------------------------------------------------------------------------
    private const uint SyntheticVersion = 300; // >=294: maxItems propio por cofre

    private static byte[] BuildSyntheticWorld()
    {
        int headerLength = WriteHeaderTo(new BinaryWriter(new MemoryStream()), new int[5]);

        int tilesOffset = headerLength;
        var tilesMs = new MemoryStream();
        using (var tw = new BinaryWriter(tilesMs, Encoding.UTF8, leaveOpen: true))
        {
            tw.Write((byte)0x00); // x=0: inactivo, sin RLE
            // x=1: activo (bit 0x02), tipo <256 (bit 0x20 apagado) -> header1 = 0x02.
            // TileFrameImportant tiene longitud 0 en esta cabecera sintetica: WldReader.ReadOneTile
            // trata cualquier tipo fuera de rango como "framed=true" (ver su propio comentario),
            // asi que el tile activo real SIEMPRE necesita U/V aqui, se use o no en el tipo 55 real.
            tw.Write((byte)0x02);
            tw.Write((byte)55); // TileID real de Sign (TEdit TileType.cs, ya usado en WldReader.SignTileTypes)
            tw.Write((short)0); // u
            tw.Write((short)0); // v
            tw.Write((byte)0x00); // x=2: inactivo, sin RLE
        }
        byte[] tilesBytes = tilesMs.ToArray();

        int chestsOffset = tilesOffset + tilesBytes.Length;
        byte[] chestsBytes = BuildChestsSection(SyntheticVersion,
        [
            (10, 20, "", 40, new (short Stack, int NetId, byte Prefix)[] { (5, 1, 0), (10, 71, 0) }),
            (1, 1, "Vacio", 5, []),
        ]);

        int signsOffset = chestsOffset + chestsBytes.Length;
        byte[] signsBytes = BuildSignsSection(
        [
            ("Bienvenido al mundo sintetico", 1, 0), // real: tile (1,0) es Sign activo
            ("Fantasma de un letrero borrado", 999, 999), // fuera del mundo (3x1) -> fantasma real
        ]);

        int npcsOffset = signsOffset + signsBytes.Length;
        var npcsMs = new MemoryStream();
        using (var nw = new BinaryWriter(npcsMs, Encoding.UTF8, leaveOpen: true))
        {
            nw.Write(0);     // shimmerCount (version >= 268)
            nw.Write(false); // termina la lista de NPCs de inmediato
        }
        byte[] npcsBytes = npcsMs.ToArray();

        var pointers = new[] { 0, tilesOffset, chestsOffset, signsOffset, npcsOffset };
        var finalMs = new MemoryStream();
        using (var hw = new BinaryWriter(finalMs, Encoding.UTF8, leaveOpen: true))
            WriteHeaderTo(hw, pointers);
        finalMs.Write(tilesBytes);
        finalMs.Write(chestsBytes);
        finalMs.Write(signsBytes);
        finalMs.Write(npcsBytes);
        return finalMs.ToArray();
    }

    private static byte[] BuildChestsSection(uint version, IReadOnlyList<(int X, int Y, string Name, int MaxItems, (short Stack, int NetId, byte Prefix)[] Items)> chests)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        w.Write((short)chests.Count);
        if (version < 294)
        {
            int globalMax = chests.Count == 0 ? 40 : chests.Max(c => c.MaxItems);
            w.Write((short)globalMax);
        }
        foreach (var chest in chests)
        {
            w.Write(chest.X);
            w.Write(chest.Y);
            w.Write(chest.Name);
            if (version >= 294) w.Write(chest.MaxItems);
            for (int slot = 0; slot < chest.MaxItems; slot++)
            {
                if (slot < chest.Items.Length)
                {
                    w.Write(chest.Items[slot].Stack);
                    w.Write(chest.Items[slot].NetId);
                    w.Write(chest.Items[slot].Prefix);
                }
                else
                {
                    w.Write((short)0);
                }
            }
        }
        return ms.ToArray();
    }

    private static byte[] BuildSignsSection(IReadOnlyList<(string Text, int X, int Y)> signs)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        w.Write((short)signs.Count);
        foreach (var (text, x, y) in signs)
        {
            w.Write(text);
            w.Write(x);
            w.Write(y);
        }
        return ms.ToArray();
    }

    // Mismo orden de campos exacto que WldReader.ReadHeader (privado) - version reducida (mundo
    // 3x1, sin GUID/CreationTime/LastPlayed: version 300 no exige ninguno de los dos primeros
    // pero SI CreationTime >=141 y NO LastPlayed <284, con cuidado con cada guarda real).
    private static int WriteHeaderTo(BinaryWriter w, int[] pointers)
    {
        long start = w.BaseStream.Position;
        w.Write(SyntheticVersion);
        w.Write("relogic".ToCharArray());
        w.Write((byte)2); // fileType = mundo
        w.Write((uint)1); // FileRevision
        w.Write((long)0); // banderas de favorito

        w.Write((short)pointers.Length);
        foreach (int p in pointers) w.Write(p);

        w.Write((short)0); // tileFrameImportant: longitud 0 (ver el comentario de BuildSyntheticWorld)

        w.Write("Mundo sintetico chest/sign");
        w.Write("semilla"); // version != 179
        w.Write(new byte[8]); // WorldGenVersion
        w.Write(new byte[16]); // WorldGUID (version >= 181)
        w.Write(1); // worldId
        w.Write(new byte[4 * 4]); // Left/Right/Top/BottomWorld

        w.Write(1); // tilesHigh
        w.Write(3); // tilesWide

        w.Write(0); // GameMode (version >= 209)
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); // >=222..302

        w.Write(new byte[8]); // CreationTime (version >= 141)
        w.Write(new byte[8]); // LastPlayed (version >= 284)
        w.Write((byte)0); // MoonType
        w.Write(new byte[4 * 3]); // TreeX
        w.Write(new byte[4 * 4]); // TreeStyle
        w.Write(new byte[4 * 3]); // CaveBackX
        w.Write(new byte[4 * 4]); // CaveBackStyle
        w.Write(new byte[4 * 3]); // Ice/Jungle/HellBackStyle

        w.Write(0); // spawnX
        w.Write(0); // spawnY
        w.Write(50.0); // groundLevel
        w.Write(100.0); // rockLevel
        w.Write(0.0); // Time
        w.Write(false); // DayTime
        w.Write(0); // MoonPhase
        w.Write(false); // BloodMoon
        w.Write(false); // IsEclipse
        w.Write(0); // dungeonX
        w.Write(0); // dungeonY

        w.Write(false); // IsCrimson
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Boss1/2/3/QueenBee
        w.Write(false); w.Write(false); w.Write(false); // Mech1/2/3
        w.Write(false); // DownedMechBossAny
        w.Write(false); w.Write(false); // Plant/Golem
        w.Write(false); // DownedSlimeKingBoss (version >= 118)
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Saved*/Downed*
        w.Write(false); w.Write(false); // ShadowOrbSmashed/SpawnMeteor
        w.Write((byte)0); // ShadowOrbCount
        w.Write(0); // AltarCount
        w.Write(false); // HardMode

        w.Flush();
        return (int)(w.BaseStream.Position - start);
    }

    [Fact]
    public void WriteChestItems_MundoSintetico_CambiaSoloElCofrePedido()
    {
        byte[] original = BuildSyntheticWorld();
        var before = WldReader.Read(original);

        Assert.Equal(2, before.Chests.Count);
        Assert.Equal(2, before.Chests[0].Items.Count);
        Assert.Equal(40, before.Chests[0].MaxItems);
        Assert.Empty(before.Chests[1].Items);
        Assert.Equal(5, before.Chests[1].MaxItems);
        // Solo el letrero REAL (sobre el tile Sign activo) sale en Signs - el fantasma se filtra,
        // igual que documenta WldReader.ReadSigns.
        Assert.Single(before.Signs);
        Assert.Equal("Bienvenido al mundo sintetico", before.Signs[0].Text);

        var newItems = new List<WldChestItem> { new(NetId: 999, Stack: 1, Prefix: 3) };
        byte[] patched = WldWriter.WriteChestItems(original, chestIndex: 1, newItems);
        var after = WldReader.Read(patched);

        // El cofre EDITADO cambio de verdad.
        Assert.Single(after.Chests[1].Items);
        Assert.Equal(999, after.Chests[1].Items[0].NetId);
        Assert.Equal(1, after.Chests[1].Items[0].Stack);
        Assert.Equal((byte)3, after.Chests[1].Items[0].Prefix);
        Assert.Equal(1, after.Chests[1].X);
        Assert.Equal(1, after.Chests[1].Y);
        Assert.Equal("Vacio", after.Chests[1].Name);

        // El cofre NO tocado sigue exactamente igual.
        Assert.Equal(before.Chests[0].X, after.Chests[0].X);
        Assert.Equal(before.Chests[0].Y, after.Chests[0].Y);
        Assert.Equal(before.Chests[0].Items.Count, after.Chests[0].Items.Count);
        Assert.Equal(before.Chests[0].Items[0], after.Chests[0].Items[0]);
        Assert.Equal(before.Chests[0].Items[1], after.Chests[0].Items[1]);

        // El resto del mundo (tiles, letreros, NPCs, dimensiones) esta intacto.
        Assert.Equal(before.Header.TilesWide, after.Header.TilesWide);
        Assert.Equal(before.Header.TilesHigh, after.Header.TilesHigh);
        for (int x = 0; x < 3; x++) Assert.Equal(before.Tiles[x, 0], after.Tiles[x, 0]);
        Assert.Equal(before.Signs.Count, after.Signs.Count);
        Assert.Equal(before.Signs[0].Text, after.Signs[0].Text);
        Assert.Equal(before.Npcs.Count, after.Npcs.Count);
    }

    // Cofre version>=294 con capacidad PROPIA: pedir mas objetos de los que caben en la capacidad
    // original tiene que CRECER esa capacidad (nunca perder objetos ni lanzar una excepcion) -
    // comportamiento documentado en WldWriter.SerializeChests.
    [Fact]
    public void WriteChestItems_MasObjetosQueLaCapacidadOriginal_CreceLaCapacidad()
    {
        byte[] original = BuildSyntheticWorld(); // chests[1] tiene MaxItems=5
        var newItems = Enumerable.Range(1, 8).Select(i => new WldChestItem(i, 1, 0)).ToList();

        byte[] patched = WldWriter.WriteChestItems(original, chestIndex: 1, newItems);
        var after = WldReader.Read(patched);

        Assert.Equal(8, after.Chests[1].Items.Count);
        Assert.True(after.Chests[1].MaxItems >= 8);
    }

    // El letrero FANTASMA (fuera del mundo, ver BuildSyntheticWorld) tiene que sobrevivir intacto
    // a una edicion del OTRO letrero - si WriteSignText solo reescribiera WldWorld.Signs (la
    // lista ya FILTRADA), esta entrada desaparecería en silencio del archivo sin que el usuario
    // lo hubiera pedido.
    [Fact]
    public void WriteSignText_EditaElLetreroReal_ConservaElFantasma()
    {
        byte[] original = BuildSyntheticWorld();
        byte[] patched = WldWriter.WriteSignText(original, signX: 1, signY: 0, newText: "Texto editado de verdad");
        var after = WldReader.Read(patched);

        Assert.Single(after.Signs);
        Assert.Equal("Texto editado de verdad", after.Signs[0].Text);

        // El fantasma no aparece en WldWorld.Signs (se sigue filtrando igual que antes, ver
        // WldReader.ReadSigns), pero debe seguir presente en la seccion RAW del archivo - se lee
        // aqui a mano (sin InternalsVisibleTo, mismo criterio ya establecido en el proyecto - ver
        // el comentario de cabecera de WldTileEntityReaderTests) para no depender de ningun
        // internal de produccion, solo de BinaryReader.ReadString/ReadInt32 reales.
        using var stream = new MemoryStream(patched, writable: false);
        using var reader = new BinaryReader(stream);
        var header = WldReader.ReadHeader(patched);
        stream.Position = header.Pointers[3];
        short rawSignCount = reader.ReadInt16();
        var rawSigns = new List<(string Text, int X, int Y)>();
        for (int i = 0; i < rawSignCount; i++)
            rawSigns.Add((reader.ReadString(), reader.ReadInt32(), reader.ReadInt32()));
        Assert.Equal(2, rawSigns.Count);
        Assert.Contains(rawSigns, s => s.X == 999 && s.Y == 999 && s.Text == "Fantasma de un letrero borrado");
    }

    [Fact]
    public void WriteChestItems_IndiceFueraDeRango_Lanza()
    {
        byte[] original = BuildSyntheticWorld();
        Assert.Throws<ArgumentOutOfRangeException>(() => WldWriter.WriteChestItems(original, chestIndex: 5, []));
    }

    [Fact]
    public void WriteSignText_CoordenadaSinLetrero_Lanza()
    {
        byte[] original = BuildSyntheticWorld();
        Assert.Throws<ArgumentException>(() => WldWriter.WriteSignText(original, signX: 50, signY: 50, "x"));
    }

    // ---------------------------------------------------------------------------------------
    // Parte 2: mundo REAL de este PC. La unica prueba que demuestra de verdad lo que pide el
    // encargo: abrir un cofre real con contenido real, editarlo, guardar, releer el .wld
    // GUARDADO desde DISCO (no el objeto en memoria) y confirmar que el cambio persistio y que
    // el resto del mundo sigue intacto. Se edita SIEMPRE sobre una COPIA en un temporal (nunca el
    // original real del usuario) - mismo criterio que toda la noche para el .plr.
    // ---------------------------------------------------------------------------------------
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
    public void WriteChestItems_MundoReal_PersisteEnDiscoYElRestoSigueIntacto()
    {
        string? sourcePath = FindRealWorldWithChests();
        if (sourcePath == null) return; // LIMITE REAL: sin ningun mundo con cofres en esta maquina, nada que verificar

        File.Copy(sourcePath, _tempPath, overwrite: true);
        byte[] original = File.ReadAllBytes(_tempPath);
        var before = WldReader.Read(original);

        int chestIndex = 0;
        var originalChest = before.Chests[chestIndex];
        var newItems = new List<WldChestItem>
        {
            new(NetId: 1, Stack: 1, Prefix: 0),   // Pico de cobre (id vanilla real)
            new(NetId: 71, Stack: 20, Prefix: 0), // Frasco de vida menor
        };

        byte[] patched = WldWriter.WriteChestItems(original, chestIndex, newItems);
        File.WriteAllBytes(_tempPath, patched);

        // Releido de verdad DESDE DISCO, no del array en memoria - la prueba real de que
        // persistio, no solo de que WldWriter devolvio bytes con buena pinta.
        var reRead = WldReader.Read(File.ReadAllBytes(_tempPath));
        var editedChest = reRead.Chests[chestIndex];

        Assert.Equal(originalChest.X, editedChest.X);
        Assert.Equal(originalChest.Y, editedChest.Y);
        Assert.Equal(originalChest.Name, editedChest.Name);
        Assert.Equal(2, editedChest.Items.Count);
        Assert.Equal(1, editedChest.Items[0].NetId);
        Assert.Equal(1, editedChest.Items[0].Stack);
        Assert.Equal(71, editedChest.Items[1].NetId);
        Assert.Equal(20, editedChest.Items[1].Stack);

        // Integridad del resto del mundo real: mismo numero de cofres/letreros/NPCs, mismas
        // dimensiones, y CADA OTRO cofre exactamente igual que antes de editar.
        Assert.Equal(before.Chests.Count, reRead.Chests.Count);
        Assert.Equal(before.Signs.Count, reRead.Signs.Count);
        Assert.Equal(before.Npcs.Count, reRead.Npcs.Count);
        Assert.Equal(before.Header.TilesWide, reRead.Header.TilesWide);
        Assert.Equal(before.Header.TilesHigh, reRead.Header.TilesHigh);
        Assert.Equal(before.Header.Title, reRead.Header.Title);

        for (int i = 0; i < before.Chests.Count; i++)
        {
            if (i == chestIndex) continue;
            Assert.Equal(before.Chests[i].X, reRead.Chests[i].X);
            Assert.Equal(before.Chests[i].Y, reRead.Chests[i].Y);
            Assert.Equal(before.Chests[i].Name, reRead.Chests[i].Name);
            Assert.Equal(before.Chests[i].Items.Count, reRead.Chests[i].Items.Count);
            for (int j = 0; j < before.Chests[i].Items.Count; j++)
                Assert.Equal(before.Chests[i].Items[j], reRead.Chests[i].Items[j]);
        }
        for (int i = 0; i < before.Signs.Count; i++)
            Assert.Equal(before.Signs[i].Text, reRead.Signs[i].Text);

        // La rejilla de tiles entera (lo mas caro de corromper con un offset mal calculado) sigue
        // exactamente igual - muestreo real, no cada tile, por coste (un mundo real puede tener
        // decenas de millones de casillas).
        int stepX = Math.Max(1, before.Header.TilesWide / 200);
        int stepY = Math.Max(1, before.Header.TilesHigh / 200);
        for (int x = 0; x < before.Header.TilesWide; x += stepX)
            for (int y = 0; y < before.Header.TilesHigh; y += stepY)
                Assert.Equal(before.Tiles[x, y], reRead.Tiles[x, y]);
    }

    private static string? FindRealWorldWithSigns()
    {
        if (!Directory.Exists(WorldsDir)) return null;
        foreach (var name in new[] { "roca_negra.wld", "Afueras_de_Larvas_de_gusano.wld", "adriandres.wld", "El_Musgo_de_Accidentes.wld", "ahora_si_que_si.wld", "kmmiu.wld" })
        {
            string path = Path.Combine(WorldsDir, name);
            if (!File.Exists(path)) continue;
            var world = WldReader.Read(File.ReadAllBytes(path));
            if (world.Signs.Count > 0) return path;
        }
        return null;
    }

    [Fact]
    public void WriteSignText_MundoReal_PersisteEnDiscoYElRestoSigueIntacto()
    {
        string? sourcePath = FindRealWorldWithSigns();
        if (sourcePath == null) return; // LIMITE REAL: sin ningun mundo con letreros reales en esta maquina

        File.Copy(sourcePath, _tempPath, overwrite: true);
        byte[] original = File.ReadAllBytes(_tempPath);
        var before = WldReader.Read(original);
        var originalSign = before.Signs[0];

        byte[] patched = WldWriter.WriteSignText(original, originalSign.X, originalSign.Y, "Letrero editado por Terrakeep - prueba real");
        File.WriteAllBytes(_tempPath, patched);

        var reRead = WldReader.Read(File.ReadAllBytes(_tempPath));
        var editedSign = reRead.Signs.First(s => s.X == originalSign.X && s.Y == originalSign.Y);
        Assert.Equal("Letrero editado por Terrakeep - prueba real", editedSign.Text);

        Assert.Equal(before.Signs.Count, reRead.Signs.Count);
        Assert.Equal(before.Chests.Count, reRead.Chests.Count);
        Assert.Equal(before.Npcs.Count, reRead.Npcs.Count);
        for (int i = 0; i < before.Chests.Count; i++)
        {
            Assert.Equal(before.Chests[i].Items.Count, reRead.Chests[i].Items.Count);
            for (int j = 0; j < before.Chests[i].Items.Count; j++)
                Assert.Equal(before.Chests[i].Items[j], reRead.Chests[i].Items[j]);
        }
    }
}
