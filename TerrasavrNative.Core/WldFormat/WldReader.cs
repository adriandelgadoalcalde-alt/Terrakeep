using System.Collections;

namespace TerrasavrNative.Core.WldFormat;

// Lector de .wld - formato confirmado contra el lector real ya construido y verificado en
// Terrasavr-Calamity-Beta (overrides.js: parseWorldHeader/deserializeTile/readBitArray/
// parseWorldNpcs, a su vez verificado contra TEdit/WorldFile.cs real). Todo little-endian.
public static class WldReader
{
    // Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
    // ESPEC-buscador-mundo-tedit.md: readContainers=true (por defecto) tambien lee cofres y
    // letreros - secciones baratas de verdad (unos pocos cientos de cofres/letreros en un
    // mundo real, nada comparable al coste de decodificar la rejilla de tiles entera) usando
    // los mismos punteros ya presentes en la cabecera (ChestsSectionOffset/SignsSectionOffset),
    // asi que no hace falta un flag para abaratar el lanzador de mundos (que ya usa ReadHeader
    // a secas, sin llamar aqui). Tile entities (maniquies/marcos de item/percheros) quedan
    // deliberadamente FUERA de esta Fase 2 - el advisor no leyo TileEntity.Load campo a campo
    // (formato polimorfico por tipo, con variantes reales entre versiones) y el propio espec
    // recomienda no arriesgar corromper la carga del mundo por una seccion que este proyecto no
    // necesita tocar para nada mas: al saltar directamente por puntero (nunca se lee
    // secuencialmente mas alla de Signs) no hace falta ni siquiera saber su formato.
    public static WldWorld Read(byte[] fileBytes, bool readContainers = true)
    {
        using var stream = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(stream);

        var header = ReadHeader(reader);

        stream.Position = header.TilesSectionOffset;
        var tiles = ReadTiles(reader, header);

        List<WldChest> chests = [];
        List<WldSign> signs = [];
        if (readContainers)
        {
            stream.Position = header.ChestsSectionOffset;
            chests = ReadChests(reader, header.Version);

            stream.Position = header.SignsSectionOffset;
            signs = ReadSigns(reader, tiles, header);
        }

        stream.Position = header.NpcsSectionOffset;
        var (npcs, shimmeredTypes) = ReadNpcs(reader, header.Version);

        return new WldWorld { Header = header, Tiles = tiles, Npcs = npcs, Chests = chests, Signs = signs, ShimmeredNpcTypes = shimmeredTypes };
    }

    // H4-08 (cuarta auditoria de Opus, Fable): lectura BARATA para el lanzador de mundos de
    // Exploracion (titulo/dimensiones para las tarjetas, sin decodificar la seccion de tiles -
    // la parte realmente cara, ~1.4s medidos en un mundo de 11MB, ver ExplorationViewModel.
    // LoadFromPathAsync) - ReadHeader(BinaryReader) YA se detiene justo despues de
    // GroundLevel/RockLevel (ver el comentario real en WldHeader), nunca toca tiles/NPCs por si
    // sola. Solo faltaba un punto de entrada publico que no siguiera leyendo mas.
    public static WldHeader ReadHeader(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(stream);
        return ReadHeader(reader);
    }

    private static WldHeader ReadHeader(BinaryReader reader)
    {
        uint version = reader.ReadUInt32();

        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");

        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision, no usado
        reader.ReadInt64();  // banderas de favorito, no usadas

        short pointerCount = reader.ReadInt16();
        var pointers = new int[pointerCount];
        for (int i = 0; i < pointerCount; i++) pointers[i] = reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5).");

        bool[] tileFrameImportant = ReadBitArray(reader);

        // Seccion de "flags" - continua secuencialmente (coincide con pointers[0], no hace
        // falta saltar ahi explicitamente). Solo se lee hasta tener titulo/dimensiones -
        // ver el comentario de WldHeader sobre por que se corta aqui a proposito.
        string title = reader.ReadString();

        if (version == 179) reader.ReadInt32(); // Seed numerico
        else reader.ReadString();                // Seed como texto

        reader.ReadBytes(8); // WorldGenVersion

        if (version >= 181) reader.ReadBytes(16); // WorldGUID

        int worldId = reader.ReadInt32();

        reader.ReadBytes(16); // Left/Right/Top/BottomWorld (Int32 x4)

        int tilesHigh = reader.ReadInt32();
        int tilesWide = reader.ReadInt32();

        if (version >= 209)
        {
            reader.ReadInt32(); // GameMode
            if (version >= 222) reader.ReadBoolean();
            if (version >= 227) reader.ReadBoolean();
            if (version >= 238) reader.ReadBoolean();
            if (version >= 239) reader.ReadBoolean();
            if (version >= 241) reader.ReadBoolean();
            if (version >= 249) reader.ReadBoolean();
            if (version >= 266) reader.ReadBoolean();
            if (version >= 267) reader.ReadBoolean(); // ZenithWorld
            if (version >= 302) reader.ReadBoolean();
        }
        else if (version == 208 || version >= 112)
        {
            reader.ReadBoolean();
        }

        if (version >= 141) reader.ReadBytes(8); // CreationTime
        if (version >= 284) reader.ReadBytes(8); // LastPlayed
        reader.ReadByte(); // MoonType
        reader.ReadBytes(4 * 3); // TreeX[0..2]
        reader.ReadBytes(4 * 4); // TreeStyle0..3
        reader.ReadBytes(4 * 3); // CaveBackX[0..2]
        reader.ReadBytes(4 * 4); // CaveBackStyle0..3
        reader.ReadBytes(4 * 3); // Ice/Jungle/HellBackStyle

        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();
        double groundLevel = reader.ReadDouble();
        double rockLevel = reader.ReadDouble();

        return new WldHeader
        {
            Version = version,
            Pointers = pointers,
            TileFrameImportant = tileFrameImportant,
            Title = title,
            WorldId = worldId,
            TilesHigh = tilesHigh,
            TilesWide = tilesWide,
            SpawnX = spawnX,
            SpawnY = spawnY,
            GroundLevel = groundLevel,
            RockLevel = rockLevel,
        };
    }

    // Int16 length + esa cantidad de bits empaquetados en bytes, orden LSB-primero dentro de
    // cada byte (confirmado con una traza manual del algoritmo real del lector JS - coincide
    // exactamente con el orden de bits que ya usa System.Collections.BitArray al construirse
    // desde un byte[], asi que se reutiliza en vez de reimplementarlo a mano).
    private static bool[] ReadBitArray(BinaryReader reader)
    {
        short length = reader.ReadInt16();
        int byteCount = (length + 7) / 8;
        byte[] raw = reader.ReadBytes(byteCount);
        var bits = new BitArray(raw);
        var result = new bool[length];
        for (int i = 0; i < length; i++) result[i] = bits[i];
        return result;
    }

    private static WldTile[,] ReadTiles(BinaryReader reader, WldHeader header)
    {
        var tiles = new WldTile[header.TilesWide, header.TilesHigh];
        for (int x = 0; x < header.TilesWide; x++)
        {
            int y = 0;
            while (y < header.TilesHigh)
            {
                var (tile, rle) = ReadOneTile(reader, header);
                int runEnd = Math.Min(header.TilesHigh, y + 1 + rle);
                for (int fillY = y; fillY < runEnd; fillY++) tiles[x, fillY] = tile;
                y = runEnd;
            }
        }
        return tiles;
    }

    private static (WldTile Tile, int Rle) ReadOneTile(BinaryReader reader, WldHeader header)
    {
        byte header1 = reader.ReadByte();
        byte header2 = 0, header3 = 0;
        if ((header1 & 0x01) != 0)
        {
            header2 = reader.ReadByte();
            if ((header2 & 0x01) != 0)
            {
                header3 = reader.ReadByte();
                if (header.Version >= 269 && (header3 & 0x01) != 0)
                    reader.ReadByte(); // header4, sin uso conocido
            }
        }

        bool isActive = (header1 & 0x02) != 0;
        short type = -1;
        short wall = 0;
        byte liquidType = 0, liquidAmount = 0;
        short u = 0, v = 0;

        if (isActive)
        {
            int tileType;
            if ((header1 & 0x20) == 0)
            {
                tileType = reader.ReadByte();
            }
            else
            {
                int lowerByte = reader.ReadByte();
                int highByte = reader.ReadByte();
                tileType = (highByte << 8) | lowerByte;
            }

            bool isFramed = tileType < header.TileFrameImportant.Length ? header.TileFrameImportant[tileType] : true;
            if (isFramed)
            {
                u = reader.ReadInt16();
                v = reader.ReadInt16();
            }
            if ((header3 & 0x08) != 0) reader.ReadByte(); // TileColor

            type = (short)tileType;
        }

        if ((header1 & 0x04) != 0)
        {
            wall = reader.ReadByte();
            if ((header3 & 0x10) != 0) reader.ReadByte(); // WallColor
        }

        int liquidHeader = (header1 & 0x18) >> 3;
        if (liquidHeader != 0)
        {
            liquidAmount = reader.ReadByte();
            liquidType = (byte)liquidHeader;
            // H3-10 (tercera auditoria de Opus, Fable): el campo de 2 bits real solo tenia
            // hueco para 3 valores (1=agua, 2=lava, 3=miel - confirmado contra TEdit,
            // World.FileV2.cs) desde antes de que el Shimmer existiera (1.4.4) - el juego real
            // reutiliza ese MISMO codigo 3 (miel) como base y usa el bit suelto de header3
            // (0x80) solo como "en realidad es Shimmer". Sobreescribir aqui a `liquidType = 3`
            // (el mismo codigo que miel) colapsaba las dos en un unico valor indistinguible
            // para el resto del puerto (WorldRenderer/ExplorationViewModel) - la miel real
            // salia pintada del color/nombre de Shimmer y viceversa, segun cual pisara al otro.
            // 4 es un codigo SINTETICO propio de este puerto (nunca se escribe a disco, solo
            // vive en memoria) para poder distinguirlos de verdad rio abajo.
            if (header.Version >= 269 && (header3 & 0x80) != 0)
                liquidType = 4; // Shimmer (sintetico - en disco comparte el codigo 3 con miel)
        }

        if (header3 > 1 && header.Version >= 222 && (header3 & 0x40) != 0)
        {
            int wallHigh = reader.ReadByte();
            wall = (short)((wallHigh << 8) | (wall & 0xFF));
        }

        int rleType = (header1 & 192) >> 6;
        int rle = rleType switch
        {
            1 => reader.ReadByte(),
            >= 2 => reader.ReadInt16(),
            _ => 0,
        };

        return (new WldTile(type, wall, liquidType, liquidAmount, u, v), rle);
    }

    private static (List<WldNpc> Npcs, HashSet<int> ShimmeredTypes) ReadNpcs(BinaryReader reader, uint version)
    {
        var npcs = new List<WldNpc>();
        // H6-08/H6-09 (sexta auditoria de Opus): real, WorldFile.LoadNPCs - un tipo de NPC
        // "shimmerizado" es un estado GLOBAL de este mundo (no por instancia), usado por
        // NpcHeadProfile para elegir la cabeza normal o la version "shimmer" real.
        var shimmeredTypes = new HashSet<int>();

        if (version >= 268)
        {
            int shimmerCount = reader.ReadInt32();
            for (int i = 0; i < shimmerCount; i++) shimmeredTypes.Add(reader.ReadInt32());
        }

        while (reader.ReadBoolean())
        {
            int id = reader.ReadInt32();
            string givenName = reader.ReadString();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            bool homeless = reader.ReadBoolean();
            int homeTileX = reader.ReadInt32();
            int homeTileY = reader.ReadInt32();

            int variationIndex = 0;
            if (version >= 213)
            {
                byte bb = reader.ReadByte();
                if ((bb & 1) != 0) variationIndex = reader.ReadInt32();
            }
            if (version >= 315) reader.ReadBoolean(); // homelessDespawn, sin uso aqui

            int tileX = homeless ? (int)Math.Round(x / 16.0) : homeTileX;
            int tileY = homeless ? (int)Math.Round(y / 16.0) : homeTileY;

            npcs.Add(new WldNpc { Id = id, GivenName = givenName, TileX = tileX, TileY = tileY, Homeless = homeless, VariationIndex = variationIndex });
        }

        return (npcs, shimmeredTypes);
    }

    // Punto 4 (advisor Opus), Fase 2: formato real confirmado directamente contra
    // World.FileV2.cs:1770-1806 de TEdit (LoadChestData) - version < 294 usa un tamaño GLOBAL
    // (Int16, una sola vez); version >= 294 usa un tamaño PROPIO por cofre (Int32, los cofres
    // pueden tener capacidades distintas). Un slot con stackSize <= 0 esta vacio - se omite en
    // vez de guardar un WldChestItem inventado (mismo criterio "lo que no se encuentra no se
    // inventa" del resto del proyecto).
    private static List<WldChest> ReadChests(BinaryReader reader, uint version)
    {
        var chests = new List<WldChest>();
        int totalChests = reader.ReadInt16();

        int globalMaxItems = 40;
        if (version < 294) globalMaxItems = reader.ReadInt16();

        for (int i = 0; i < totalChests; i++)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            string name = reader.ReadString();
            int maxItems = version >= 294 ? reader.ReadInt32() : globalMaxItems;

            var items = new List<WldChestItem>();
            for (int slot = 0; slot < maxItems; slot++)
            {
                short stackSize = reader.ReadInt16();
                if (stackSize <= 0) continue;
                int netId = reader.ReadInt32();
                byte prefix = reader.ReadByte();
                items.Add(new WldChestItem(netId, stackSize, prefix));
            }

            chests.Add(new WldChest { X = x, Y = y, Name = name, Items = items });
        }

        return chests;
    }

    // Punto 4 (advisor Opus), Fase 2: formato real confirmado directamente contra
    // World.FileV2.cs:1828-1839 de TEdit (LoadSignData - el texto va PRIMERO, antes de x/y) +
    // TileType.cs (TileTypes.IsSign: Sign=55, GraveMarker=85, AnnouncementBox=425,
    // TatteredSign=573) - mismo filtro real que aplica TEdit al cargar (descarta letreros
    // "fantasma" cuya casilla ya no es un letrero de verdad, ej. tras borrar el bloque sin que
    // el .wld limpiara la entrada).
    private static readonly HashSet<int> SignTileTypes = [55, 85, 425, 573];

    private static List<WldSign> ReadSigns(BinaryReader reader, WldTile[,] tiles, WldHeader header)
    {
        var signs = new List<WldSign>();
        int totalSigns = reader.ReadInt16();
        for (int i = 0; i < totalSigns; i++)
        {
            string text = reader.ReadString();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();

            if (x < 0 || y < 0 || x >= header.TilesWide || y >= header.TilesHigh) continue;
            var tile = tiles[x, y];
            if (!tile.IsActive || !SignTileTypes.Contains(tile.Type)) continue;

            signs.Add(new WldSign { X = x, Y = y, Text = text });
        }
        return signs;
    }
}
