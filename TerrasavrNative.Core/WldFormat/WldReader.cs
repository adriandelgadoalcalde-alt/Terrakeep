using System.Collections;

namespace TerrasavrNative.Core.WldFormat;

// Lector de .wld - formato confirmado contra el lector real ya construido y verificado en
// Terrasavr-Calamity-Beta (overrides.js: parseWorldHeader/deserializeTile/readBitArray/
// parseWorldNpcs, a su vez verificado contra TEdit/WorldFile.cs real). Todo little-endian.
public static class WldReader
{
    public static WldWorld Read(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(stream);

        var header = ReadHeader(reader);

        stream.Position = header.TilesSectionOffset;
        var tiles = ReadTiles(reader, header);

        stream.Position = header.NpcsSectionOffset;
        var npcs = ReadNpcs(reader, header.Version);

        return new WldWorld { Header = header, Tiles = tiles, Npcs = npcs };
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

        return new WldHeader
        {
            Version = version,
            Pointers = pointers,
            TileFrameImportant = tileFrameImportant,
            Title = title,
            WorldId = worldId,
            TilesHigh = tilesHigh,
            TilesWide = tilesWide,
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
            if (header.Version >= 269 && (header3 & 0x80) != 0)
                liquidType = 3; // Shimmer
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

    private static List<WldNpc> ReadNpcs(BinaryReader reader, uint version)
    {
        var npcs = new List<WldNpc>();

        if (version >= 268)
        {
            int shimmerCount = reader.ReadInt32();
            for (int i = 0; i < shimmerCount; i++) reader.ReadInt32(); // ids, sin uso aqui
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

            if (version >= 213)
            {
                byte bb = reader.ReadByte();
                if ((bb & 1) != 0) reader.ReadInt32(); // variacion de NPC de pueblo, sin uso aqui
            }
            if (version >= 315) reader.ReadBoolean(); // homelessDespawn, sin uso aqui

            int tileX = homeless ? (int)Math.Round(x / 16.0) : homeTileX;
            int tileY = homeless ? (int)Math.Round(y / 16.0) : homeTileY;

            npcs.Add(new WldNpc { Id = id, GivenName = givenName, TileX = tileX, TileY = tileY, Homeless = homeless });
        }

        return npcs;
    }
}
