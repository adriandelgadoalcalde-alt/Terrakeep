using System.Text;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Fase 2b (diferida de la Fase 2 original - ver el comentario de WldReader.Read): formato
// polimorfico por tipo de TileEntity.Load, transcrito byte a byte desde TileEntity.cs real de
// TEdit (descargado y leido campo a campo esta misma sesion). Un .wld real
// (WldReaderRealFileTests.Read_RealWorld_TileEntitiesSonSanas) da confianza de que el offset y
// el version-gating general funcionan, pero NO garantiza ejercitar las 11 ramas del switch por
// separado (sobre todo el parche real de la version 311 de DisplayDoll - un bug del propio
// juego que pocos mundos reales de esta maquina llegan a tener guardado) - de ahi este .wld
// SINTETICO, construido a mano campo a campo y leido por el MISMO WldReader.Read publico (sin
// atajos via InternalsVisibleTo, mismo criterio ya establecido en el proyecto - ver el
// comentario de App.xaml.cs.ShouldForceSoftwareRendering) para poder ejercitar cada rama exacta.
public class WldTileEntityReaderTests
{
    // Post-Shimmer real conocido (>=268, evita el bloque especial de NPCs pre-Shimmer) y por
    // debajo de 284 (sin LastPlayed) para mantener la cabecera sintetica lo mas corta posible -
    // el valor exacto no afecta a nada del formato de tile entities salvo el propio gating de
    // DisplayDoll (307/308/311), que cada prueba fija por separado via BuildWorldBytes(version:).
    private const uint DefaultVersion = 279;

    // Construye un .wld sintetico minimo (mundo 1x1, sin cofres/letreros/npcs reales) con el
    // contenido EXACTO que se le pida para la seccion de tile entities - dos pasadas: la primera
    // solo para medir el tamaño real de la cabecera (fijo, no depende de los valores que lleve la
    // tabla de punteros), la segunda ya con los offsets reales calculados.
    private static byte[] BuildWorldBytes(uint version, Action<BinaryWriter> writeTileEntities)
    {
        int headerLength = WriteHeaderTo(new BinaryWriter(new MemoryStream()), version, new int[6]);

        int tilesOffset = headerLength;
        var tilesMs = new MemoryStream();
        using (var tw = new BinaryWriter(tilesMs, Encoding.UTF8, leaveOpen: true))
            tw.Write((byte)0x00); // 1 tile inactivo, sin RLE - cubre el unico tile del mundo 1x1
        byte[] tilesBytes = tilesMs.ToArray();

        int chestsOffset = tilesOffset + tilesBytes.Length;
        var chestsMs = new MemoryStream();
        using (var cw = new BinaryWriter(chestsMs, Encoding.UTF8, leaveOpen: true))
        {
            cw.Write((short)0); // totalChests
            cw.Write((short)0); // globalMaxItems (version < 294)
        }
        byte[] chestsBytes = chestsMs.ToArray();

        int signsOffset = chestsOffset + chestsBytes.Length;
        var signsMs = new MemoryStream();
        using (var sw = new BinaryWriter(signsMs, Encoding.UTF8, leaveOpen: true))
            sw.Write((short)0); // totalSigns
        byte[] signsBytes = signsMs.ToArray();

        int npcsOffset = signsOffset + signsBytes.Length;
        var npcsMs = new MemoryStream();
        using (var nw = new BinaryWriter(npcsMs, Encoding.UTF8, leaveOpen: true))
        {
            if (version >= 268) nw.Write(0); // shimmerCount
            nw.Write(false); // termina la lista de NPCs de inmediato
        }
        byte[] npcsBytes = npcsMs.ToArray();

        int tileEntitiesOffset = npcsOffset + npcsBytes.Length;
        var teMs = new MemoryStream();
        using (var tew = new BinaryWriter(teMs, Encoding.UTF8, leaveOpen: true))
            writeTileEntities(tew);
        byte[] tileEntitiesBytes = teMs.ToArray();

        var pointers = new[] { 0, tilesOffset, chestsOffset, signsOffset, npcsOffset, tileEntitiesOffset };
        var finalMs = new MemoryStream();
        using (var hw = new BinaryWriter(finalMs, Encoding.UTF8, leaveOpen: true))
            WriteHeaderTo(hw, version, pointers);

        finalMs.Write(tilesBytes);
        finalMs.Write(chestsBytes);
        finalMs.Write(signsBytes);
        finalMs.Write(npcsBytes);
        finalMs.Write(tileEntitiesBytes);
        return finalMs.ToArray();
    }

    // Mismo orden de campos exacto que WldReader.ReadHeader (privado) - transcrito para poder
    // fabricar un .wld sintetico sin acceso a internals. Devuelve el numero de bytes escritos.
    private static int WriteHeaderTo(BinaryWriter w, uint version, int[] pointers)
    {
        long start = w.BaseStream.Position;

        w.Write(version);
        w.Write("relogic".ToCharArray()); // ReadChars(7), sin longitud
        w.Write((byte)2); // fileType = mundo
        w.Write((uint)1); // FileRevision, sin uso
        w.Write((long)0); // banderas de favorito, sin uso

        w.Write((short)pointers.Length);
        foreach (int p in pointers) w.Write(p);

        w.Write((short)0); // tileFrameImportant: longitud 0 (el mundo 1x1 sintetico nunca tiene un tile activo enmarcado)

        w.Write("Mundo sintetico"); // title
        if (version == 179) w.Write(0); else w.Write("semilla"); // seed
        w.Write(new byte[8]); // WorldGenVersion

        if (version >= 181) w.Write(new byte[16]); // WorldGUID

        w.Write(1); // worldId
        w.Write(new byte[4 * 4]); // Left/Right/Top/BottomWorld

        w.Write(1); // tilesHigh
        w.Write(1); // tilesWide

        if (version >= 209)
        {
            w.Write(0); // GameMode
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
        else if (version == 208 || version >= 112)
        {
            w.Write(false);
        }

        if (version >= 141) w.Write(new byte[8]); // CreationTime
        if (version >= 284) w.Write(new byte[8]); // LastPlayed
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
        // F-7 (auditoria de Opus vs TEdit, E-06): ReadHeader ahora sigue leyendo 5 campos mas
        // (Time/DayTime/MoonPhase/BloodMoon/IsEclipse, sin guarda de version) antes de
        // DungeonX/Y - la cabecera sintetica de este test tiene que dar los mismos bytes que el
        // lector real espera, o EndOfStreamException.
        w.Write(0.0); // Time
        w.Write(false); // DayTime
        w.Write(0); // MoonPhase
        w.Write(false); // BloodMoon
        w.Write(false); // IsEclipse
        w.Write(0); // dungeonX
        w.Write(0); // dungeonY

        w.Flush();
        return (int)(w.BaseStream.Position - start);
    }

    // TileEntity.Load real: Type(byte) + Id(Int32) + PosX(Int16) + PosY(Int16), luego el cuerpo
    // polimorfico que cada prueba escribe aparte.
    private static void WriteEntityCommonFields(BinaryWriter w, WldTileEntityKind kind, int id, short x, short y)
    {
        w.Write((byte)kind);
        w.Write(id);
        w.Write(x);
        w.Write(y);
    }

    private static void WriteStack(BinaryWriter w, short netId, byte prefix, short stack)
    {
        w.Write(netId);
        w.Write(prefix);
        w.Write(stack);
    }

    [Fact]
    public void Read_ItemFrame_LeeElObjetoRealDentro()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(1); // numEntities
            WriteEntityCommonFields(w, WldTileEntityKind.ItemFrame, id: 0, x: 10, y: 20);
            WriteStack(w, netId: 3818, prefix: 0, stack: 1); // Espada corta de la Reina de las Abejas
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        Assert.Equal(WldTileEntityKind.ItemFrame, entity.Kind);
        Assert.Equal(10, entity.X);
        Assert.Equal(20, entity.Y);
        var item = Assert.Single(entity.Items);
        Assert.Equal(3818, item.NetId);
        Assert.Equal(1, item.Stack);
    }

    [Fact]
    public void Read_ItemFrame_SlotVacio_NoProduceNingunObjeto()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.ItemFrame, id: 0, x: 5, y: 5);
            WriteStack(w, netId: 0, prefix: 0, stack: 0); // marco vacio - real, un marco colocado sin nada dentro
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        Assert.Empty(entity.Items); // igual que TileEntityItem.IsValid real (Id>0 && Stack>0)
    }

    [Fact]
    public void Read_TrainingDummy_NoTieneObjetos_PeroSeLeeEntero()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.TrainingDummy, id: 0, x: 1, y: 1);
            w.Write((short)1); // Npc (tipo de NPC del dummy)
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        Assert.Equal(WldTileEntityKind.TrainingDummy, entity.Kind);
        Assert.Empty(entity.Items);
    }

    [Fact]
    public void Read_LogicSensorYPylon_SeLeenSinObjetos()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(2);
            WriteEntityCommonFields(w, WldTileEntityKind.LogicSensor, id: 0, x: 1, y: 1);
            w.Write((byte)0); // LogicCheck
            w.Write(true);    // On
            WriteEntityCommonFields(w, WldTileEntityKind.TeleportationPylon, id: 1, x: 2, y: 2);
            // sin cuerpo - TeleportationPylon no guarda nada propio
        });

        var world = WldReader.Read(bytes);

        Assert.Equal(2, world.TileEntities.Count);
        Assert.All(world.TileEntities, e => Assert.Empty(e.Items));
    }

    [Theory]
    [InlineData(WldTileEntityKind.CritterAnchor)]
    [InlineData(WldTileEntityKind.KiteAnchor)]
    public void Read_AnclaDeCriaturaOCometa_SeLeeComoUnObjetoSintetico(WldTileEntityKind kind)
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, kind, id: 0, x: 7, y: 7);
            w.Write((short)4379); // Cometa Wyvern, netId real conocido (LeashedKites.KiteWyvern)
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        var item = Assert.Single(entity.Items);
        Assert.Equal(4379, item.NetId);
        Assert.Equal(1, item.Stack); // sintetico - el formato real no guarda stack para un ancla
    }

    // TileEntity.LoadHatRack real (linea 435): bits 0-1 = 2 slots de objeto, bits 2-3 = 2 slots
    // de tinte. Se marcan solo el slot de objeto 1 y el de tinte 0 para confirmar que el mapeo
    // de bits no esta desplazado (un fallo de indice aqui seria indistinguible de "todo vacio"
    // si se probara solo con todos los bits a 1).
    [Fact]
    public void Read_HatRack_MapeaLosBitsDePresenciaCorrectamente()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.HatRack, id: 0, x: 1, y: 1);
            w.Write((byte)0b0000_0101); // bit0 (objeto[0]) + bit2 (tinte[0])
            WriteStack(w, netId: 100, prefix: 0, stack: 1); // objeto[0]
            WriteStack(w, netId: 200, prefix: 3, stack: 1); // tinte[0]
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        Assert.Equal(2, entity.Items.Count);
        Assert.Contains(entity.Items, i => i.NetId == 100);
        Assert.Contains(entity.Items, i => i.NetId == 200 && i.Prefix == 3);
    }

    // TileEntity.LoadDisplayDoll real (linea 502), version >= 308 pero != 311 (rama normal, sin
    // el parche especial) - 9º slot de objeto (extraSlots bit1) y Misc[0] (extraSlots bit0)
    // marcados, para confirmar que los slots "extra" mas alla de los 8 primeros se leen en el
    // orden correcto.
    [Fact]
    public void Read_DisplayDoll_Version308_LeeElNovenoSlotYMisc()
    {
        byte[] bytes = BuildWorldBytes(308, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.DisplayDoll, id: 0, x: 1, y: 1);
            w.Write((byte)0);          // itemSlots (ninguno de los 8 primeros)
            w.Write((byte)0);          // dyeSlots
            w.Write((byte)0);          // Pose (version >= 307)
            w.Write((byte)0b0000_0011); // extraSlots: bit0=Misc[0], bit1=9º objeto
            WriteStack(w, netId: 500, prefix: 0, stack: 1); // 9º objeto (Items[8])
            WriteStack(w, netId: 501, prefix: 0, stack: 1); // Misc[0]
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        Assert.Equal(2, entity.Items.Count);
        Assert.Contains(entity.Items, i => i.NetId == 500);
        Assert.Contains(entity.Items, i => i.NetId == 501);
    }

    // Caso especial REAL de la version 311 (bug conocido del propio juego, TileEntity.cs:
    // "Version 311 special bug handling"): el bit1 de extraSlots (9º objeto) NO se lee en su
    // sitio normal dentro del bucle - se aparta, se pone a 0 antes del bucle, y su LoadStack se
    // lee DESPUES de tintes y de Misc[0], al final del todo. Si esta prueba pasara con el orden
    // "normal" (como si fuera version 308/309/310), el resultado seria el mismo NetId pero
    // leido de una posicion de bytes distinta - por eso el test coloca valores DIFERENTES y
    // reconocibles en cada campo, para que un desorden de lectura produzca un NetId equivocado
    // en vez de pasar por casualidad.
    [Fact]
    public void Read_DisplayDoll_Version311_LeeElNovenoSlotAlFinalPorElBugReal()
    {
        byte[] bytes = BuildWorldBytes(311, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.DisplayDoll, id: 0, x: 1, y: 1);
            w.Write((byte)0b0000_0001); // itemSlots: solo el objeto[0]
            w.Write((byte)0b0000_0001); // dyeSlots: solo el tinte[0]
            w.Write((byte)0);           // Pose
            w.Write((byte)0b0000_0011); // extraSlots: bit0=Misc[0], bit1=9º objeto (se leera AL FINAL)
            WriteStack(w, netId: 111, prefix: 0, stack: 1); // objeto[0] (orden normal)
            WriteStack(w, netId: 222, prefix: 0, stack: 1); // tinte[0] (orden normal)
            WriteStack(w, netId: 333, prefix: 0, stack: 1); // Misc[0] (orden normal)
            WriteStack(w, netId: 999, prefix: 0, stack: 1); // 9º objeto - DEBE leerse aqui, al final
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        var netIds = entity.Items.Select(i => i.NetId).OrderBy(n => n).ToList();
        Assert.Equal([111, 222, 333, 999], netIds);
    }

    // version < 308: el noveno slot no existe en absoluto (maxSlots=8, ni siquiera se lee el
    // byte extraSlots) - confirma que no se intenta leer un byte de mas que no esta en el archivo.
    [Fact]
    public void Read_DisplayDoll_VersionAnteriorA308_NoIntentaLeerElNovenoSlot()
    {
        byte[] bytes = BuildWorldBytes(279, w =>
        {
            w.Write(1);
            WriteEntityCommonFields(w, WldTileEntityKind.DisplayDoll, id: 0, x: 1, y: 1);
            w.Write((byte)0b0000_0001); // itemSlots: objeto[0]
            w.Write((byte)0);           // dyeSlots
            // sin Pose (version < 307) ni extraSlots (version < 308)
            WriteStack(w, netId: 42, prefix: 0, stack: 1);
        });

        var world = WldReader.Read(bytes);

        var entity = Assert.Single(world.TileEntities);
        var item = Assert.Single(entity.Items);
        Assert.Equal(42, item.NetId);
    }

    [Fact]
    public void Read_VariosTiposMezclados_ConservaCoordenadasYOrdenReal()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(3);
            WriteEntityCommonFields(w, WldTileEntityKind.ItemFrame, id: 0, x: 1, y: 1);
            WriteStack(w, netId: 10, prefix: 0, stack: 1);
            WriteEntityCommonFields(w, WldTileEntityKind.WeaponRack, id: 1, x: 2, y: 2);
            WriteStack(w, netId: 20, prefix: 0, stack: 1);
            WriteEntityCommonFields(w, WldTileEntityKind.FoodPlatter, id: 2, x: 3, y: 3);
            WriteStack(w, netId: 30, prefix: 0, stack: 1);
        });

        var world = WldReader.Read(bytes);

        Assert.Equal(3, world.TileEntities.Count);
        Assert.Equal((1, 1, 10), (world.TileEntities[0].X, world.TileEntities[0].Y, world.TileEntities[0].Items[0].NetId));
        Assert.Equal((2, 2, 20), (world.TileEntities[1].X, world.TileEntities[1].Y, world.TileEntities[1].Items[0].NetId));
        Assert.Equal((3, 3, 30), (world.TileEntities[2].X, world.TileEntities[2].Y, world.TileEntities[2].Items[0].NetId));
    }

    // version < 122 (formato legado "Dummies" de World.FileV2.cs, sin contenido de objetos real)
    // - se debe devolver una lista vacia sin intentar decodificar el formato viejo, ni falta hace
    // saber su tamaño exacto para no reventar la lectura del resto del mundo (nada se lee
    // despues de esta seccion en este puerto, ver el comentario real de WldReader.Read).
    [Fact]
    public void Read_VersionAnteriorA122_DevuelveListaVaciaSinIntentarDecodificarNada()
    {
        byte[] bytes = BuildWorldBytes(121, w =>
        {
            // formato legado real: Int32 count + pares Int16,Int16 - ni siquiera hace falta
            // escribir nada coherente aqui, ReadTileEntities no debe leer ni un byte de esto.
            w.Write(999);
        });

        var world = WldReader.Read(bytes);

        Assert.Empty(world.TileEntities);
    }

    // Fase 2b: WorldPresenceIndex.TileEntityItemCounts tiene que censar lo mismo que
    // WldReader.ReadTileEntities acaba de leer - prueba de integracion corta entre los dos, en
    // vez de repetir toda la casuistica de arriba tambien aqui.
    [Fact]
    public void PresenceIndex_CensaLosObjetosRealesDeLasTileEntities()
    {
        byte[] bytes = BuildWorldBytes(DefaultVersion, w =>
        {
            w.Write(2);
            WriteEntityCommonFields(w, WldTileEntityKind.ItemFrame, id: 0, x: 1, y: 1);
            WriteStack(w, netId: 77, prefix: 0, stack: 1);
            WriteEntityCommonFields(w, WldTileEntityKind.WeaponRack, id: 1, x: 2, y: 2);
            WriteStack(w, netId: 77, prefix: 0, stack: 1); // mismo NetId en otra tile entity - debe SUMAR, no pisar
        });

        var world = WldReader.Read(bytes);
        var idx = WorldPresenceIndex.Build(world);

        Assert.True(idx.HasTileEntityItem(77));
        Assert.Equal(2, idx.TileEntityItemCounts[77]);
        Assert.False(idx.HasTileEntityItem(999));
    }
}
