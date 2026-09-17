using System.Collections;

namespace Terrakeep.Core.WldFormat;

// Lector de .wld - formato confirmado contra el lector real ya construido y verificado en
// Terrasavr-Calamity-Beta (overrides.js: parseWorldHeader/deserializeTile/readBitArray/
// parseWorldNpcs, a su vez verificado contra TEdit/WorldFile.cs real). Todo little-endian.
public static class WldReader
{
    // Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
    // ESPEC-buscador-mundo-tedit.md: readContainers=true (por defecto) tambien lee cofres,
    // letreros y tile entities - secciones baratas de verdad (unos pocos cientos/miles de
    // elementos en un mundo real, nada comparable al coste de decodificar la rejilla de tiles
    // entera) usando los mismos punteros ya presentes en la cabecera, asi que no hace falta un
    // flag para abaratar el lanzador de mundos (que ya usa ReadHeader a secas, sin llamar aqui).
    // Tile entities (maniquies/marcos de item/percheros/bandejas/frascos/anclas) estuvieron
    // deliberadamente fuera de la Fase 2 original (el advisor no leyo TileEntity.Load campo a
    // campo) - Fase 2b: formato ya verificado byte a byte contra TileEntity.cs real de TEdit,
    // ver ReadTileEntities.
    // Punto 2 de la lista de funciones nuevas (17-sep-2026, "barra de progreso real"): decodificar
    // la rejilla de tiles es el UNICO tramo de coste real medido de toda la carga de un mundo (ver
    // el comentario de ExplorationViewModel.LoadFromPathAsync, ~1.4s en un mundo de 11MB de esta
    // maquina) - onTileProgress es opcional (null en cualquier llamada que no necesite progreso,
    // ej. WorldFileService releyendo tras un guardado) para no obligar a ningun llamador existente
    // a inventarse un callback que no le sirve de nada.
    public static WldWorld Read(byte[] fileBytes, bool readContainers = true, Action<long, long>? onTileProgress = null)
    {
        using var stream = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(stream);

        var header = ReadHeader(reader);

        stream.Position = header.TilesSectionOffset;
        var tiles = ReadTiles(reader, header, onTileProgress);

        List<WldChest> chests = [];
        List<WldSign> signs = [];
        List<WldTileEntity> tileEntities = [];
        if (readContainers)
        {
            stream.Position = header.ChestsSectionOffset;
            chests = ReadChests(reader, header.Version);

            stream.Position = header.SignsSectionOffset;
            signs = ReadSigns(reader, tiles, header);

            if (header.TileEntitiesSectionOffset is int teOffset)
            {
                stream.Position = teOffset;
                tileEntities = ReadTileEntities(reader, header.Version);
            }
        }

        stream.Position = header.NpcsSectionOffset;
        var (npcs, shimmeredTypes) = ReadNpcs(reader, header.Version);

        // Editor de mundos v1 (14-sep-2026), punto 6 de la lista confirmada por el usuario
        // (paneles de progreso/completitud): el bestiario vive en su PROPIA seccion con puntero
        // directo (ver WldHeader.BestiarySectionOffset) - coste despreciable (un salto directo,
        // nunca hay que atravesar ninguna seccion intermedia), asi que se lee siempre que exista,
        // igual que NPCs/cofres/letreros de arriba.
        WldBestiary? bestiary = null;
        if (header.BestiarySectionOffset is int bestiaryOffset)
        {
            stream.Position = bestiaryOffset;
            bestiary = ReadBestiary(reader);
        }

        return new WldWorld { Header = header, Tiles = tiles, Npcs = npcs, Chests = chests, Signs = signs, TileEntities = tileEntities, ShimmeredNpcTypes = shimmeredTypes, Bestiary = bestiary };
    }

    // Formato real confirmado en el comentario de WldBestiary: 3 diccionarios/conjuntos
    // secuenciales, cada uno Int32 count + esa cantidad de entradas (Kills lleva ademas un
    // Int32 de cantidad por clave; Sighted/Chatted son solo el conjunto de claves).
    private static WldBestiary ReadBestiary(BinaryReader reader)
    {
        var kills = new Dictionary<string, int>();
        int killCount = reader.ReadInt32();
        for (int i = 0; i < killCount; i++) kills[reader.ReadString()] = reader.ReadInt32();

        var sighted = new HashSet<string>();
        int sightedCount = reader.ReadInt32();
        for (int i = 0; i < sightedCount; i++) sighted.Add(reader.ReadString());

        var chatted = new HashSet<string>();
        int chattedCount = reader.ReadInt32();
        for (int i = 0; i < chattedCount; i++) chatted.Add(reader.ReadString());

        return new WldBestiary { Kills = kills, Sighted = sighted, Chatted = chatted };
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

        // F-14 (auditoria de Opus vs TEdit, E-16): "la semilla... esta a un puñado de Read* de
        // distancia, y hoy Terrakeep no enseña ninguno" - se leia y se descartaba. Se captura
        // como texto en los dos casos (un seed numerico real de Terraria se muestra igual como
        // texto, ej. "-1234567").
        string seed = version == 179 ? reader.ReadInt32().ToString() : reader.ReadString();

        reader.ReadBytes(8); // WorldGenVersion

        if (version >= 181) reader.ReadBytes(16); // WorldGUID

        int worldId = reader.ReadInt32();

        reader.ReadBytes(16); // Left/Right/Top/BottomWorld (Int32 x4)

        int tilesHigh = reader.ReadInt32();
        int tilesWide = reader.ReadInt32();

        // F-14 (auditoria de Opus vs TEdit, E-16): "el modo de juego (Clasico/Experto/Maestro/
        // Viaje)... ya se lee" - se leia y se descartaba. Codificacion real por version,
        // confirmada byte a byte contra World.FileV2.cs de TEdit (commit f592261): >=209 es un
        // int real (0..3); ==208 un bool (Maestro=2/Clasico=0, el modo Maestro llego antes que
        // el int generico); 112..207 un bool distinto (Experto=1/Clasico=0); antes de 112 no
        // existia el concepto, 0 fijo.
        int gameMode;
        if (version >= 209)
        {
            gameMode = reader.ReadInt32();
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
        else if (version == 208)
        {
            gameMode = reader.ReadBoolean() ? 2 : 0;
        }
        else if (version >= 112)
        {
            gameMode = reader.ReadBoolean() ? 1 : 0;
        }
        else
        {
            gameMode = 0;
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
        // F-7 (auditoria de Opus vs TEdit, E-06), ampliado por el editor de mundos v1 (14-sep-2026):
        // estos 5 campos ya NO se descartan, se guardan de verdad en WldHeader (ver su comentario) -
        // confirmado byte a byte contra World.FileV2.cs (TEdit, commit f592261), sin guarda de version.
        double time = reader.ReadDouble();
        bool dayTime = reader.ReadBoolean();
        int moonPhase = reader.ReadInt32();
        bool bloodMoon = reader.ReadBoolean();
        bool isEclipse = reader.ReadBoolean();
        int dungeonX = reader.ReadInt32();
        int dungeonY = reader.ReadInt32();

        // Editor de mundos v1 (14-sep-2026): bloque de banderas de progreso, confirmado byte a
        // byte contra World.FileV2.cs de TEdit (commit f592261, lineas 2092-2105) - de ancho
        // FIJO (sin ningun string variable hasta aqui), ver el comentario real de WldHeader sobre
        // por que es seguro leerlo y parchearlo. Nunca se toca ShadowOrb/Invasion/clima/etc. -
        // fuera del alcance de esta primera version, ver bitacora.md.
        bool isCrimson = reader.ReadBoolean();
        bool downedBoss1 = reader.ReadBoolean();
        bool downedBoss2 = reader.ReadBoolean();
        bool downedBoss3 = reader.ReadBoolean();
        bool downedQueenBee = reader.ReadBoolean();
        bool downedMech1 = reader.ReadBoolean();
        bool downedMech2 = reader.ReadBoolean();
        bool downedMech3 = reader.ReadBoolean();
        reader.ReadBoolean(); // DownedMechBossAny - derivable de los 3 anteriores, no hace falta guardarlo aparte
        bool downedPlant = reader.ReadBoolean();
        bool downedGolem = reader.ReadBoolean();
        bool? downedSlimeKing = version >= 118 ? reader.ReadBoolean() : null;

        reader.ReadBoolean(); // SavedGoblin
        reader.ReadBoolean(); // SavedWizard
        reader.ReadBoolean(); // SavedMech
        reader.ReadBoolean(); // DownedGoblins
        reader.ReadBoolean(); // DownedClown
        reader.ReadBoolean(); // DownedFrost
        reader.ReadBoolean(); // DownedPirates
        reader.ReadBoolean(); // ShadowOrbSmashed
        reader.ReadBoolean(); // SpawnMeteor
        reader.ReadByte();    // ShadowOrbCount
        reader.ReadInt32();   // AltarCount
        bool hardMode = reader.ReadBoolean();

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
            Seed = seed,
            GameMode = gameMode,
            DungeonX = dungeonX,
            DungeonY = dungeonY,
            Time = time,
            DayTime = dayTime,
            MoonPhase = moonPhase,
            BloodMoon = bloodMoon,
            IsEclipse = isEclipse,
            IsCrimson = isCrimson,
            DownedBoss1EyeOfCthulhu = downedBoss1,
            DownedBoss2EaterOfWorldsOrBrainOfCthulhu = downedBoss2,
            DownedBoss3Skeletron = downedBoss3,
            DownedQueenBee = downedQueenBee,
            DownedMechBoss1TheDestroyer = downedMech1,
            DownedMechBoss2TheTwins = downedMech2,
            DownedMechBoss3SkeletronPrime = downedMech3,
            DownedPlantBoss = downedPlant,
            DownedGolemBoss = downedGolem,
            DownedSlimeKingBoss = downedSlimeKing,
            HardMode = hardMode,
        };
    }

    // Int16 length + esa cantidad de bits empaquetados en bytes, orden LSB-primero dentro de
    // cada byte (confirmado con una traza manual del algoritmo real del lector JS - coincide
    // exactamente con el orden de bits que ya usa System.Collections.BitArray al construirse
    // desde un byte[], asi que se reutiliza en vez de reimplementarlo a mano).
    // internal (no private): WldWriter.PatchGameMode necesita replicar EXACTAMENTE esta misma
    // lectura para calcular el offset real de GameMode - reutilizada, nunca duplicada, para que
    // los dos caminos no puedan divergir con el tiempo.
    internal static bool[] ReadBitArray(BinaryReader reader)
    {
        short length = reader.ReadInt16();
        int byteCount = (length + 7) / 8;
        byte[] raw = reader.ReadBytes(byteCount);
        var bits = new BitArray(raw);
        var result = new bool[length];
        for (int i = 0; i < length; i++) result[i] = bits[i];
        return result;
    }

    private static WldTile[,] ReadTiles(BinaryReader reader, WldHeader header, Action<long, long>? onTileProgress)
    {
        var tiles = new WldTile[header.TilesWide, header.TilesHigh];
        // Reportar en CADA columna (hasta 8400 en un mundo Grande) es barato de calcular pero
        // caro de RECIBIR si el llamador reenvia cada aviso al hilo de UI (WPF Dispatcher.Post
        // por cada uno) - se limita a ~200 avisos reales a lo largo de la carga entera (mismo
        // orden de magnitud que ya usa el muestreo de comparacion de tiles en
        // WldWriterChestSignTests), de sobra para que una barra se vea fluida sin saturar la cola
        // de mensajes del Dispatcher.
        int step = Math.Max(1, header.TilesWide / 200);
        long total = (long)header.TilesWide * header.TilesHigh;
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
            if (onTileProgress != null && (x % step == 0 || x == header.TilesWide - 1))
                onTileProgress((long)(x + 1) * header.TilesHigh, total);
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
    // internal (no private): WldWriter.WriteChestItems necesita releer los cofres reales del
    // archivo ANTES de editar uno solo, byte a byte identico a como los ve esta funcion - misma
    // regla ya establecida con ReadBitArray (reutilizada, nunca duplicada, para que los dos
    // caminos no puedan divergir con el tiempo).
    internal static List<WldChest> ReadChests(BinaryReader reader, uint version)
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

            chests.Add(new WldChest { X = x, Y = y, Name = name, Items = items, MaxItems = maxItems });
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
        var raw = ReadRawSigns(reader);
        var signs = new List<WldSign>();
        foreach (var (text, x, y) in raw)
        {
            if (x < 0 || y < 0 || x >= header.TilesWide || y >= header.TilesHigh) continue;
            var tile = tiles[x, y];
            if (!tile.IsActive || !SignTileTypes.Contains(tile.Type)) continue;

            signs.Add(new WldSign { X = x, Y = y, Text = text });
        }
        return signs;
    }

    // internal (no private): WldWriter.WriteSignText necesita conservar TAMBIEN los letreros
    // "fantasma" (casilla ya no es un letrero real, ver el comentario de ReadSigns/WldSign) al
    // reescribir la seccion - de lo contrario un guardado real borraria de forma silenciosa
    // entradas que el usuario nunca pidio tocar. Misma regla ya establecida con ReadBitArray/
    // ReadChests: una unica lectura real, reutilizada por los dos caminos para que no puedan
    // divergir.
    internal static List<(string Text, int X, int Y)> ReadRawSigns(BinaryReader reader)
    {
        var raw = new List<(string, int, int)>();
        int totalSigns = reader.ReadInt16();
        for (int i = 0; i < totalSigns; i++)
        {
            string text = reader.ReadString();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            raw.Add((text, x, y));
        }
        return raw;
    }

    // Fase 2b (diferida de la Fase 2 original): formato real confirmado directamente contra
    // TileEntity.cs de TEdit (github.com/TEdit/Terraria-Map-Editor, main, descargado y leido
    // campo a campo esta misma sesion) - World.FileV2.cs:1953-1964 (LoadTileEntityData: Int32
    // numEntities, luego por cada una `new TileEntity(); entity.Load(r, version)`) +
    // TileEntity.cs:377-419 (Load: Type/Id/PosX/PosY comunes, luego un switch polimorfico por
    // Type que decide cuantos bytes mas leer) + los 3 sub-lectores reales (LoadStack:428,
    // LoadHatRack:435, LoadDisplayDoll:502).
    //
    // Version<116: esta seccion no existe en absoluto en el archivo (TileEntitiesSectionOffset
    // seria null de todas formas, WldHeader.Pointers no llegaria a tener indice 5). 116<=
    // version<122: World.FileV2.cs usa el formato LEGADO "Dummies" (World.FileV2.cs:1966-1975,
    // LoadDummies - Int32 count + pares Int16,Int16 SIN contenido de objetos real, solo
    // coordenadas de dummy) en vez de TileEntity.Load: se devuelve vacio sin decodificarlo, ni
    // falta hace saber su formato exacto (ningun mundo real de esta maquina esta en ese rango,
    // y no aporta nada buscable aunque lo estuviera).
    private static List<WldTileEntity> ReadTileEntities(BinaryReader reader, uint version)
    {
        if (version < 122) return [];

        int numEntities = reader.ReadInt32();
        var result = new List<WldTileEntity>(numEntities);
        for (int i = 0; i < numEntities; i++)
        {
            byte type = reader.ReadByte();
            reader.ReadInt32();       // Id interno de TEdit (correlativo de edicion), sin uso aqui
            short x = reader.ReadInt16();
            short y = reader.ReadInt16();

            var items = new List<WldTileEntityItem>();
            switch (type)
            {
                case (byte)WldTileEntityKind.TrainingDummy:
                    reader.ReadInt16(); // Npc (tipo de NPC del dummy, no un objeto - nada que buscar aqui)
                    break;
                case (byte)WldTileEntityKind.ItemFrame:
                case (byte)WldTileEntityKind.WeaponRack:
                case (byte)WldTileEntityKind.FoodPlatter:
                case (byte)WldTileEntityKind.DeadCellsDisplayJar:
                    ReadStackInto(reader, items);
                    break;
                case (byte)WldTileEntityKind.LogicSensor:
                    reader.ReadByte();    // LogicCheck
                    reader.ReadBoolean(); // On
                    break;
                case (byte)WldTileEntityKind.DisplayDoll:
                    ReadDisplayDollItems(reader, version, items);
                    break;
                case (byte)WldTileEntityKind.HatRack:
                    ReadHatRackItems(reader, items);
                    break;
                case (byte)WldTileEntityKind.TeleportationPylon:
                    break; // sin datos propios
                case (byte)WldTileEntityKind.CritterAnchor:
                case (byte)WldTileEntityKind.KiteAnchor:
                    short netId = reader.ReadInt16(); // aka NetId del item (criatura o cometa), sin stack/prefijo real
                    if (netId > 0) items.Add(new WldTileEntityItem(netId, 1, 0));
                    break;
                default:
                    // Tipo desconocido (version del juego mas nueva que este catalogo) - no hay
                    // forma segura de saber cuantos bytes mas ocupa sin la tabla de arriba.
                    // Mismo criterio del proyecto ("lo que no se reconoce no se inventa"): se
                    // corta la lectura de ESTA seccion sin arriesgar desincronizar el resto del
                    // archivo (las demas secciones ya se leen por puntero absoluto de todas
                    // formas, esto no las afecta).
                    return result;
            }

            result.Add(new WldTileEntity { Kind = (WldTileEntityKind)type, X = x, Y = y, Items = items });
        }
        return result;
    }

    // TileEntity.LoadStack real: NetId(Int16) + Prefix(byte) + StackSize(Int16), SIEMPRE
    // presente cuando el slot esta marcado (el bit de presencia ya se comprobo antes de llamar
    // aqui, salvo en el caso simple de un unico slot fijo tipo ItemFrame/WeaponRack/... donde
    // no hay bit de presencia - el slot esta siempre, vacio o no). Solo se añade a la lista si
    // es un objeto real (mismo criterio que TileEntityItem.IsValid de TEdit: Id>0 && Stack>0).
    private static void ReadStackInto(BinaryReader reader, List<WldTileEntityItem> items)
    {
        short netId = reader.ReadInt16();
        byte prefix = reader.ReadByte();
        short stack = reader.ReadInt16();
        if (netId > 0 && stack > 0) items.Add(new WldTileEntityItem(netId, stack, prefix));
    }

    // TileEntity.LoadHatRack real (linea 435): 1 byte de presencia (bits 0-1 = 2 slots de
    // objeto, bits 2-3 = 2 slots de tinte), cada slot presente lee un LoadStack completo.
    private static void ReadHatRackItems(BinaryReader reader, List<WldTileEntityItem> items)
    {
        byte slots = reader.ReadByte();
        for (int i = 0; i < 2; i++)
            if ((slots & (1 << i)) != 0) ReadStackInto(reader, items);
        for (int i = 0; i < 2; i++)
            if ((slots & (1 << (i + 2))) != 0) ReadStackInto(reader, items);
    }

    // TileEntity.LoadDisplayDoll real (linea 502) - el mas largo de los 11, transcrito 1:1
    // (incluido el parche real de la version 311, donde el juego escribia el 9º slot de objeto
    // FUERA de orden por un bug ya corregido pero que sigue presente en archivos guardados con
    // esa version exacta):
    //   1) itemSlots(byte) + dyeSlots(byte) - presencia de los primeros 8 slots de cada tipo.
    //   2) Pose(byte), solo version>=307.
    //   3) extraSlots(byte), solo version>=308 - bit0=Misc[0], bit1=9º objeto, bit2=9º tinte.
    //   4) CASO ESPECIAL version==311: el bit1 de extraSlots (9º objeto) se guarda aparte y se
    //      PONE A CERO antes de los bucles normales - ese slot, si estaba marcado, se lee al
    //      final del todo en vez de en su sitio natural dentro del bucle de objetos.
    //   5) maxSlots = 9 si version>=308, si no 8.
    //   6) Bucle de objetos (i=0..maxSlots-1): slot i<8 -> bit i de itemSlots; slot 8 -> bit1 de
    //      extraSlots (ya sera 0 en el caso especial de la version 311, ver 4).
    //   7) Bucle de tintes (i=0..maxSlots-1): slot i<8 -> bit i de dyeSlots; slot 8 -> bit2 de
    //      extraSlots.
    //   8) Misc[0]: bit0 de extraSlots.
    //   9) Si el caso especial de la version 311 aplico: un LoadStack final para el 9º objeto.
    private static void ReadDisplayDollItems(BinaryReader reader, uint version, List<WldTileEntityItem> items)
    {
        byte itemSlots = reader.ReadByte();
        byte dyeSlots = reader.ReadByte();

        if (version >= 307) reader.ReadByte(); // Pose

        byte extraSlots = 0;
        if (version >= 308) extraSlots = reader.ReadByte();

        bool v311NinthItemDeferred = false;
        if (version == 311)
        {
            v311NinthItemDeferred = (extraSlots & 0b0000_0010) != 0;
            extraSlots = (byte)(extraSlots & ~0b0000_0010);
        }

        int maxSlots = version >= 308 ? 9 : 8;

        for (int i = 0; i < maxSlots; i++)
        {
            bool hasItem = i < 8 ? (itemSlots & (1 << i)) != 0 : (extraSlots & 0b0000_0010) != 0;
            if (hasItem) ReadStackInto(reader, items);
        }
        for (int i = 0; i < maxSlots; i++)
        {
            bool hasDye = i < 8 ? (dyeSlots & (1 << i)) != 0 : (extraSlots & 0b0000_0100) != 0;
            if (hasDye) ReadStackInto(reader, items);
        }
        if ((extraSlots & 0b0000_0001) != 0) ReadStackInto(reader, items); // Misc[0]

        if (v311NinthItemDeferred) ReadStackInto(reader, items);
    }
}
