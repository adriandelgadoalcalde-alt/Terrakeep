using Terrakeep.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.WldFormat;

// KeepQA (20-sep-2026, punto 6 del catalogo de funciones - "Bancos de datos extremos
// compartidos"): WldWriter.WriteWorld es el primer escritor de .wld de la familia que construye
// un mundo COMPLETO desde cero (sin partir de ningun archivo real) - estas pruebas demuestran el
// round-trip real (construir -> escribir -> releer con WldReader.Read, el MISMO lector publico
// que usa el resto del proyecto, sin atajos) para casos normales y para los casos "extremos" que
// el arnes de KeepQA necesita generar de verdad (coordenadas al limite, cofres desbordados,
// banderas de progreso contradictorias, cantidades extremas de NPCs/tile entities).
public class WldWriterWriteWorldTests
{
    private const uint Version = 279; // 1.4.4.9 real, misma version que ya usa WldTileEntityReaderTests

    private static WldHeader BuildHeader(int tilesWide, int tilesHigh, Action<HeaderOverrides>? configure = null)
    {
        var o = new HeaderOverrides();
        configure?.Invoke(o);

        return new WldHeader
        {
            Version = Version,
            Pointers = [], // se recalcula de verdad dentro de WriteWorld, este valor nunca se usa para escribir
            TileFrameImportant = o.TileFrameImportant,
            Title = o.Title,
            WorldId = o.WorldId,
            TilesHigh = tilesHigh,
            TilesWide = tilesWide,
            SpawnX = o.SpawnX,
            SpawnY = o.SpawnY,
            GroundLevel = o.GroundLevel,
            RockLevel = o.RockLevel,
            Seed = o.Seed,
            GameMode = o.GameMode,
            DungeonX = o.DungeonX,
            DungeonY = o.DungeonY,
            Time = o.Time,
            DayTime = o.DayTime,
            MoonPhase = o.MoonPhase,
            BloodMoon = o.BloodMoon,
            IsEclipse = o.IsEclipse,
            IsCrimson = o.IsCrimson,
            DownedBoss1EyeOfCthulhu = o.DownedBoss1,
            DownedBoss2EaterOfWorldsOrBrainOfCthulhu = o.DownedBoss2,
            DownedBoss3Skeletron = o.DownedBoss3,
            DownedQueenBee = o.DownedQueenBee,
            DownedMechBoss1TheDestroyer = o.DownedMech1,
            DownedMechBoss2TheTwins = o.DownedMech2,
            DownedMechBoss3SkeletronPrime = o.DownedMech3,
            DownedPlantBoss = o.DownedPlant,
            DownedGolemBoss = o.DownedGolem,
            DownedSlimeKingBoss = o.DownedSlimeKing,
            HardMode = o.HardMode,
            DownedGoblinArmy = o.DownedGoblinArmy,
            DownedFrostLegion = o.DownedFrostLegion,
            DownedPirates = o.DownedPirates,
        };
    }

    private sealed class HeaderOverrides
    {
        public bool[] TileFrameImportant = [];
        public string Title = "Mundo de prueba WriteWorld";
        public int WorldId = 12345;
        public int SpawnX = 5, SpawnY = 5;
        public double GroundLevel = 50, RockLevel = 100;
        public string Seed = "keepqa";
        public int GameMode = 0;
        public int DungeonX = 10, DungeonY = 10;
        public double Time = 0;
        public bool DayTime = true;
        public int MoonPhase = 0;
        public bool BloodMoon, IsEclipse, IsCrimson;
        public bool DownedBoss1, DownedBoss2, DownedBoss3, DownedQueenBee;
        public bool DownedMech1, DownedMech2, DownedMech3;
        public bool DownedPlant, DownedGolem;
        public bool? DownedSlimeKing = false;
        public bool HardMode;
        public bool DownedGoblinArmy, DownedFrostLegion, DownedPirates;
    }

    private static WldTile[,] BuildTiles(int wide, int high, Func<int, int, WldTile> factory)
    {
        var tiles = new WldTile[wide, high];
        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
                tiles[x, y] = factory(x, y);
        return tiles;
    }

    [Fact]
    public void WriteWorld_MundoPequenoNormal_RoundTripByteAByteEstableYCoherente()
    {
        int wide = 20, high = 15;
        var tiles = BuildTiles(wide, high, (x, y) =>
            y < 5 ? WldTile.Empty : new WldTile(type: 0, wall: 1, liquidType: 0, liquidAmount: 0, u: 0, v: 0));

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles,
            Npcs = [new WldNpc { Id = 17, GivenName = "Guide", TileX = 10, TileY = 4, Homeless = false, VariationIndex = 0 }],
            Chests = [new WldChest { X = 3, Y = 4, Name = "", Items = [new WldChestItem(NetId: 1, Stack: 1, Prefix: 0)], MaxItems = 40 }],
            Signs = [],
            TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(),
            Bestiary = new WldBestiary { Kills = new Dictionary<string, int> { ["Zombie"] = 3 }, Sighted = new HashSet<string> { "Zombie" }, Chatted = new HashSet<string>() },
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.Equal(wide, reread.Header.TilesWide);
        Assert.Equal(high, reread.Header.TilesHigh);
        Assert.Equal("Mundo de prueba WriteWorld", reread.Header.Title);
        Assert.Single(reread.Npcs);
        Assert.Equal("Guide", reread.Npcs[0].GivenName);
        Assert.Equal(10, reread.Npcs[0].TileX);
        Assert.Single(reread.Chests);
        Assert.Equal(1, reread.Chests[0].Items[0].NetId);
        Assert.NotNull(reread.Bestiary);
        Assert.Equal(3, reread.Bestiary!.Kills["Zombie"]);

        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
                Assert.Equal(tiles[x, y], reread.Tiles[x, y]);

        // Segunda pasada: volver a escribir el mundo YA RELEIDO tiene que producir bytes
        // IDENTICOS byte a byte (estabilidad real del round-trip, no solo "se puede volver a
        // leer") - mismo criterio que el resto de la familia exige de sus escritores.
        var headerForRewrite = reread.Header; // Pointers reales ya calculados por WriteWorld en la 1a pasada
        var worldForRewrite = new WldWorld
        {
            Header = headerForRewrite, Tiles = reread.Tiles, Npcs = reread.Npcs, Chests = reread.Chests,
            Signs = reread.Signs, TileEntities = reread.TileEntities, ShimmeredNpcTypes = reread.ShimmeredNpcTypes, Bestiary = reread.Bestiary,
        };
        byte[] bytes2 = WldWriter.WriteWorld(worldForRewrite);
        Assert.Equal(bytes.Length, bytes2.Length);
        Assert.Equal(bytes, bytes2);
    }

    // Idea 1 del catalogo de funciones ("Estado del mundo editable" - bitacora.md 20-sep-2026,
    // quinta ronda: reconsiderado a peticion explicita del coordinador/usuario). Round-trip real
    // de las 3 banderas de invasion nuevas (Ejercito Goblin/Legion Helada/Piratas) - aisla que de
    // verdad viajan por WriteWorld->Read, no solo que su valor por defecto (false) coincide por
    // casualidad con el resto de las pruebas de esta clase (todas usan BuildHeader sin configure,
    // o sea false para las tres). true en las tres a proposito, el caso mas exigente.
    [Fact]
    public void WriteWorld_BanderasDeInvasion_ViajanIntactasPorElRoundTrip()
    {
        int wide = 10, high = 10;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);
        var world = new WldWorld
        {
            Header = BuildHeader(wide, high, o => { o.DownedGoblinArmy = true; o.DownedFrostLegion = true; o.DownedPirates = true; }),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.True(reread.Header.DownedGoblinArmy);
        Assert.True(reread.Header.DownedFrostLegion);
        Assert.True(reread.Header.DownedPirates);
        // Aisla que HardMode (el campo INMEDIATAMENTE despues en el archivo real) no se
        // contamino por el desplazamiento - si el offset de escritura estuviera mal, este
        // seria el primer campo en romperse de forma silenciosa (leeria el byte de otro campo).
        Assert.False(reread.Header.HardMode);
    }

    // ---------------------------------------------------------------------------------------
    // Casos EXTREMOS reales (punto 6 del catalogo, el motivo de esta pieza): coordenadas al
    // limite, cofres desbordados, cantidad extrema de NPCs/tile entities, banderas de progreso
    // contradictorias. Cada uno demuestra que WriteWorld produce un archivo que WldReader.Read
    // relee SIN reventar y CON el dato extremo intacto - la misma prueba real que ya exige
    // dst-jugador-extremo del lado de Starvekeep.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void WriteWorld_TileEnLaEsquinaExtremaDelMundo_SobreviveIntacto()
    {
        int wide = 50, high = 40;
        var extremo = new WldTile(type: short.MaxValue, wall: 255, liquidType: 2, liquidAmount: 255, u: short.MaxValue, v: short.MinValue);
        var tiles = BuildTiles(wide, high, (x, y) =>
            x == wide - 1 && y == high - 1 ? extremo : WldTile.Empty);

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        var got = reread.Tiles[wide - 1, high - 1];
        Assert.Equal(extremo.Type, got.Type);
        Assert.Equal(extremo.Wall, got.Wall);
        Assert.Equal(extremo.LiquidType, got.LiquidType);
        Assert.Equal(extremo.LiquidAmount, got.LiquidAmount);
        Assert.Equal(extremo.U, got.U);
        Assert.Equal(extremo.V, got.V);
    }

    [Fact]
    public void WriteWorld_ParedPorEncimaDe255YShimmer_NecesitanVersionSuficienteYSobrevivenIntactos()
    {
        int wide = 5, high = 5;
        // Pared >255 (necesita el byte alto, version>=222) + liquido Shimmer (necesita version>=269)
        // en el MISMO tile - el caso combinado real que mas riesgo tiene de pisarse entre si por
        // el orden de bytes (ver el comentario de WriteOneTileForWrite).
        var tile = new WldTile(type: 0, wall: 999, liquidType: 4, liquidAmount: 200, u: 0, v: 0);
        var tiles = BuildTiles(wide, high, (x, y) => tile);

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
            {
                Assert.Equal(999, reread.Tiles[x, y].Wall);
                Assert.Equal((byte)4, reread.Tiles[x, y].LiquidType); // Shimmer distinguible de miel tras el round-trip
            }
    }

    [Fact]
    public void WriteWorld_CofreDesbordado_MasObjetosQueSlotsVanillaReales_SobrevivenTodos()
    {
        int wide = 3, high = 3;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);

        // 200 objetos reales en un unico cofre - muy por encima de los 40 slots de un cofre de
        // madera vanilla real, el mismo tipo de caso "desbordado" que ya prueba
        // WriteChestItems_MasObjetosQueLaCapacidadOriginal_CreceLaCapacidad pero generado desde
        // CERO en vez de editando un archivo existente.
        var items = Enumerable.Range(1, 200).Select(i => new WldChestItem(NetId: i, Stack: (short)i, Prefix: (byte)(i % 256))).ToList();
        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [],
            Chests = [new WldChest { X = 1, Y = 1, Name = "Cofre desbordado", Items = items, MaxItems = 40 }],
            Signs = [], TileEntities = [], ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.Single(reread.Chests);
        Assert.Equal(200, reread.Chests[0].Items.Count);
        Assert.True(reread.Chests[0].MaxItems >= 200);
        Assert.Equal(200, reread.Chests[0].Items[199].NetId);
    }

    [Fact]
    public void WriteWorld_CantidadExtremaDeNpcs_TodosSobrevivenConSuPosicionRealYVariationIndex()
    {
        int wide = 30, high = 20;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);

        // 500 NPCs (muy por encima de cualquier mundo real jugado), algunos "homeless" (posicion
        // derivada de x/y en pixeles) y otros no (homeTileX/Y directo), con coordenadas FUERA de
        // los limites reales del mundo a proposito (el propio caso "corrupto" que pide el
        // catalogo - WldReader nunca valida rango al leer NPCs, solo Terrakeep.Core.Tests lo hace
        // sobre mundos reales, nunca sobre uno sintetico como este).
        var npcs = Enumerable.Range(0, 500).Select(i => new WldNpc
        {
            Id = i,
            GivenName = i % 2 == 0 ? $"NPC-{i}" : "",
            TileX = i % 3 == 0 ? wide + 1000 : i, // algunos claramente fuera del mundo
            TileY = i % 5 == 0 ? -1000 : i,
            Homeless = i % 2 == 0,
            VariationIndex = i % 6,
        }).ToList();

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = npcs, Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int> { 1, 2, 3 }, Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.Equal(500, reread.Npcs.Count);
        Assert.Equal(new HashSet<int> { 1, 2, 3 }, reread.ShimmeredNpcTypes);
        for (int i = 0; i < 500; i++)
        {
            Assert.Equal(npcs[i].TileX, reread.Npcs[i].TileX);
            Assert.Equal(npcs[i].TileY, reread.Npcs[i].TileY);
            Assert.Equal(npcs[i].VariationIndex, reread.Npcs[i].VariationIndex);
            Assert.Equal(npcs[i].Homeless, reread.Npcs[i].Homeless);
        }
    }

    [Fact]
    public void WriteWorld_TileEntitiesEnCantidadExtremaYConCoordenadasCorruptas_Sobreviven()
    {
        int wide = 10, high = 10;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);

        var entities = new List<WldTileEntity>();
        for (int i = 0; i < 300; i++)
        {
            entities.Add(new WldTileEntity
            {
                Kind = (WldTileEntityKind)(i % 2 == 0 ? WldTileEntityKind.ItemFrame : WldTileEntityKind.CritterAnchor),
                X = short.MaxValue - i, // corrupto a proposito: muy por encima de un mundo real
                Y = short.MinValue + i,
                Items = [new WldTileEntityItem(NetId: i + 1, Stack: (short)(i + 1), Prefix: 0)],
            });
        }

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = entities,
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.Equal(300, reread.TileEntities.Count);
        for (int i = 0; i < 300; i++)
        {
            Assert.Equal((short)(short.MaxValue - i), reread.TileEntities[i].X);
            Assert.Equal((short)(short.MinValue + i), reread.TileEntities[i].Y);
            Assert.Single(reread.TileEntities[i].Items);
            Assert.Equal(i + 1, reread.TileEntities[i].Items[0].NetId);
        }
    }

    [Fact]
    public void WriteWorld_BanderasDeProgresoContradictorias_SobrevivenTalCual()
    {
        int wide = 5, high = 5;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);

        // Contradiccion real a proposito: HardMode activo (exige haber matado un jefe mecanico o
        // el Muro de Carne en el juego real) pero NINGUN jefe marcado como derrotado, ni siquiera
        // el Ojo de Cthulhu - un estado que el juego real nunca produce por si solo, pero que
        // WldReader/WldWriter tienen que poder representar y releer tal cual para servir de caso
        // de fuzzing real (el catalogo explicito del punto 6: "flags contradictorios").
        var header = BuildHeader(wide, high, o =>
        {
            o.HardMode = true;
            o.DownedBoss1 = o.DownedBoss2 = o.DownedBoss3 = false;
            o.DownedMech1 = o.DownedMech2 = o.DownedMech3 = false;
            o.DownedGolem = true; // derrotado el Golem sin haber pasado por ningun mecanico antes
            o.DownedSlimeKing = null; // ausente a proposito (simula un mundo de version <118 reusando el resto del formato moderno)
        });

        var world = new WldWorld
        {
            Header = header, Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        var reread = WldReader.Read(bytes);

        Assert.True(reread.Header.HardMode);
        Assert.False(reread.Header.DownedBoss1EyeOfCthulhu);
        Assert.False(reread.Header.DownedMechBoss1TheDestroyer);
        Assert.True(reread.Header.DownedGolemBoss);
        // DownedSlimeKingBoss=null en el header ORIGINAL se escribe como false (version>=118
        // siempre existe el campo en el archivo real que genera WriteWorld) - se relee como
        // false, no como null (el campo SI existe en el archivo, a diferencia de un mundo real
        // <118 donde WldReader.ReadHeader jamas intenta leerlo).
        Assert.False(reread.Header.DownedSlimeKingBoss);
    }

    [Fact]
    public void WriteWorld_MundoGrandeConUnSoloTipoDeTile_RleColapsaYSigueSiendoLegible()
    {
        // Representativo de un mundo REAL de tamaño "Mediano" (4200x1200), sin llegar al tamaño
        // "Grande" real (8400x2400) para mantener la generacion y verificacion de esta prueba
        // dentro de un tiempo razonable en este arnes - el objetivo aqui es demostrar que el RLE
        // por columna colapsa de verdad un mundo casi uniforme (no medir el tamaño maximo real,
        // eso ya lo cubren WldReaderRealFileTests contra mundos reales de este PC).
        int wide = 4200, high = 1200;
        var dirt = new WldTile(type: 0, wall: 0, liquidType: 0, liquidAmount: 0, u: 0, v: 0);
        var tiles = BuildTiles(wide, high, (x, y) => y > 200 ? dirt : WldTile.Empty);

        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        byte[] bytes = WldWriter.WriteWorld(world);
        // El RLE por columna real (bloques de <=32767 filas) deberia mantener el archivo muy por
        // debajo de "1 byte(s) real por tile" (4200*1200 = 5.04M tiles) - una señal barata de que
        // el RLE esta colapsando de verdad las columnas casi uniformes, no escribiendo tile a
        // tile.
        Assert.True(bytes.Length < wide * high / 4, $"El archivo ({bytes.Length} bytes) es demasiado grande para un mundo casi uniforme - el RLE no esta colapsando.");

        var reread = WldReader.Read(bytes);
        Assert.Equal(wide, reread.Header.TilesWide);
        Assert.Equal(high, reread.Header.TilesHigh);
        for (int x = 0; x < wide; x += 137) // muestreo real, no cada tile (coste)
            for (int y = 0; y < high; y += 53)
                Assert.Equal(tiles[x, y], reread.Tiles[x, y]);
    }

    [Fact]
    public void WriteWorld_DisplayDollOHatRack_RechazaExplicitoEnVezDeAdivinar()
    {
        int wide = 3, high = 3;
        var tiles = BuildTiles(wide, high, (x, y) => WldTile.Empty);
        var world = new WldWorld
        {
            Header = BuildHeader(wide, high),
            Tiles = tiles, Npcs = [], Chests = [], Signs = [],
            TileEntities = [new WldTileEntity { Kind = WldTileEntityKind.DisplayDoll, X = 1, Y = 1, Items = [] }],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        Assert.Throws<NotSupportedException>(() => WldWriter.WriteWorld(world));
    }

    [Fact]
    public void WriteWorld_VersionAnteriorA210_Rechaza()
    {
        var headerVersionVieja = new WldHeader
        {
            Version = 200, Pointers = [], TileFrameImportant = [], Title = "x", WorldId = 1,
            TilesHigh = 3, TilesWide = 3, SpawnX = 0, SpawnY = 0, GroundLevel = 1, RockLevel = 2,
            Seed = "s", GameMode = 0, DungeonX = 0, DungeonY = 0, Time = 0, DayTime = true, MoonPhase = 0,
            BloodMoon = false, IsEclipse = false, IsCrimson = false, DownedBoss1EyeOfCthulhu = false,
            DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false, DownedBoss3Skeletron = false, DownedQueenBee = false,
            DownedMechBoss1TheDestroyer = false, DownedMechBoss2TheTwins = false, DownedMechBoss3SkeletronPrime = false,
            DownedPlantBoss = false, DownedGolemBoss = false, DownedSlimeKingBoss = false, HardMode = false,
            DownedGoblinArmy = false, DownedFrostLegion = false, DownedPirates = false,
        };
        var world = new WldWorld
        {
            Header = headerVersionVieja,
            Tiles = new WldTile[3, 3], Npcs = [], Chests = [], Signs = [], TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(), Bestiary = null,
        };

        Assert.Throws<NotSupportedException>(() => WldWriter.WriteWorld(world));
    }
}
