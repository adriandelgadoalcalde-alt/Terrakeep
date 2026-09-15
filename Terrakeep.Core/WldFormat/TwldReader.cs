using Terrakeep.Core.Nbt;

namespace Terrakeep.Core.WldFormat;

// T3 del documento I+D real ("Terrakeep, editor de cofres/letreros del .wld", recomendacion 4,
// 15-sep-2026): lector del `.twld` (World Extra Data de tModLoader - NO es un formato Terrakeep
// propio, es el archivo real que el propio juego escribe al lado del `.wld` cuando hay mods
// cargados). Formato confirmado directamente contra el decompilado real de tModLoader
// (Downloads\tModLoader-Decompiled\tModLoader\Terraria\ModLoader\IO\WorldIO.cs/TileIO.cs) - NO
// adivinado:
//
//   - `.twld` es gzip(NBT) exactamente igual que `.tplr` (TagIO.ToStream con compress=true,
//     WorldIO.Save escribe a traves de FileUtilities.WriteTagCompound) - se reutiliza el mismo
//     TplrFile.Read/NbtSerializer ya usado para personajes, nunca un segundo lector NBT.
//   - Causa real de "Tile #-1" (documentada tambien en ExplorationViewModel.ChestKindName, 15-sep-2026):
//     WorldFile.cs:1425 (tModLoader real) escribe un tile de MOD (type >= TileID.Count) como aire
//     en el .wld normal, para que un mundo con mods se pueda seguir abriendo con Terraria vainilla
//     sin reventar - su tipo real viaja APARTE, en el .twld, seccion "tiles" (TileIO.SaveBasics):
//     TagCompound { tileMap: List<TileEntry>, tileData: byte[], wallMap: List<WallEntry>, wallData: byte[] }.
//   - Cada entrada de tileMap/wallMap (ModBlockEntry.SerializeData, IO/ModBlockEntry.cs) es un
//     TagCompound { value: short (el mismo ushort crudo de tile.type, reinterpretado - ver
//     IO/UShortTagSerializer.cs: `(short)value`/`(ushort)tag`, NUNCA un short con signo real),
//     mod: string, name: string, fallbackID: short, uType: string }, mas "framed": bool SOLO en
//     TileEntry (WallEntry no lo lleva - las paredes nunca tienen frameX/frameY propio).
//   - tileData/wallData son un blob BINARIO ANIDADO, escrito con un BinaryWriter normal (LITTLE-
//     ENDIAN, TileIO.IOImpl.SaveData: `new BinaryWriter(memoryStream)` a secas) - a diferencia del
//     NBT que lo envuelve (ese si BIG-ENDIAN, BigEndianWriter) - un detalle facil de pasar por
//     alto que este lector respeta a proposito. Por cada casilla, en orden [x=0..anchoDelMundo-1]
//     [y=0..altoDelMundo-1] (mismo orden que Main.tile[i,j], TileIO.IOImpl.SaveData/LoadData):
//     UInt16 num (0 = esta casilla no es de ningun mod); si num!=0, Byte color y, SOLO para tiles
//     con Framed=true de su entrada en tileMap, ademas Int16 frameX + Int16 frameY (las paredes
//     nunca llevan frame).
public readonly record struct TwldModEntry(int Type, string Mod, string Name, bool FrameImportant)
{
    // Mismo formato ya usado en toda la app para "algo que viene de un mod" (ver
    // ExplorationViewModel.ChestKindName/README) - "Mod: NombreInterno", honesto sobre que es un
    // nombre INTERNO (sin traduccion posible: los mods no traen su propio lang.zip indexado por
    // este proyecto) en vez de fingir un nombre bonito que no se puede verificar.
    public string DisplayName => $"{Mod}: {Name}";
}

public sealed class TwldModContent
{
    public required IReadOnlyDictionary<int, TwldModEntry> TileEntries { get; init; }
    public required IReadOnlyDictionary<int, TwldModEntry> WallEntries { get; init; }
    // Casilla (x,y) -> tipo de tile/pared de mod REAL en esa posicion (solo las posiciones
    // pedidas via `positionsOfInterest` al leer, ver TwldReader.Read - nunca el mundo entero,
    // decodificar tileData/wallData completos de un mundo Grande no hace falta para resolver un
    // puñado de cofres/letreros concretos).
    public required IReadOnlyDictionary<(int X, int Y), int> ModTileTypeAt { get; init; }
    public required IReadOnlyDictionary<(int X, int Y), int> ModWallTypeAt { get; init; }

    // Nombre real ya resuelto para una casilla, o null si esa casilla no es de ningun mod (o no
    // se pidio como posicion de interes al leer) - punto de entrada unico para
    // ExplorationViewModel, nunca hay que cruzar ModTileTypeAt/TileEntries a mano fuera de aqui.
    public string? DescribeTileAt(int x, int y) =>
        ModTileTypeAt.TryGetValue((x, y), out int type) && TileEntries.TryGetValue(type, out var entry) ? entry.DisplayName : null;

    public string? DescribeWallAt(int x, int y) =>
        ModWallTypeAt.TryGetValue((x, y), out int type) && WallEntries.TryGetValue(type, out var entry) ? entry.DisplayName : null;
}

public static class TwldReader
{
    // `tilesWide`/`tilesHigh` TIENEN que ser los mismos que `WldHeader.TilesWide/TilesHigh` del
    // `.wld` hermano (tileData/wallData son una rejilla densa sin ninguna marca de tamaño propia,
    // el tamaño lo fija Main.maxTilesX/Y en el momento de guardar - el mismo mundo, la misma
    // partida). `positionsOfInterest` null = no decodificar ninguna rejilla (solo tileMap/wallMap,
    // barato); con posiciones, se hace UNA pasada secuencial completa (no hay forma de saltar
    // directo a una casilla - el ancho de cada celda depende de si esa celda es de mod o no) que
    // se detiene en cuanto se han resuelto todas las posiciones pedidas.
    public static TwldModContent Read(byte[] twldBytes, int tilesWide, int tilesHigh, IReadOnlySet<(int X, int Y)>? positionsOfInterest = null)
    {
        var (_, root) = TplrFile.Read(twldBytes);
        var tilesCompound = root.Get("tiles") as NbtCompound;

        var tileEntries = ParseEntries(tilesCompound?.Get("tileMap"), hasFrame: true);
        var wallEntries = ParseEntries(tilesCompound?.Get("wallMap"), hasFrame: false);

        Dictionary<(int, int), int> tileGrid = [];
        Dictionary<(int, int), int> wallGrid = [];
        if (positionsOfInterest is { Count: > 0 })
        {
            if (tilesCompound?.Get("tileData") is NbtByteArray tileData)
                tileGrid = ScanGrid(tileData.Value, tileEntries, tilesWide, tilesHigh, positionsOfInterest, hasFrame: true);
            if (tilesCompound?.Get("wallData") is NbtByteArray wallData)
                wallGrid = ScanGrid(wallData.Value, wallEntries, tilesWide, tilesHigh, positionsOfInterest, hasFrame: false);
        }

        return new TwldModContent { TileEntries = tileEntries, WallEntries = wallEntries, ModTileTypeAt = tileGrid, ModWallTypeAt = wallGrid };
    }

    private static Dictionary<int, TwldModEntry> ParseEntries(NbtTag? listTag, bool hasFrame)
    {
        var result = new Dictionary<int, TwldModEntry>();
        if (listTag is not NbtList list) return result;

        foreach (var item in list.Items)
        {
            if (item is not NbtCompound c) continue;
            int type = GetUShort(c, "value");
            string mod = (c.Get("mod") as NbtString)?.Value ?? "";
            string name = (c.Get("name") as NbtString)?.Value ?? "";
            bool framed = hasFrame && c.Get("framed") is NbtByte { Value: not 0 };
            result[type] = new TwldModEntry(type, mod, name, framed);
        }
        return result;
    }

    private static int GetUShort(NbtCompound c, string key) => c.Get(key) switch
    {
        // UShortTagSerializer real (ver el comentario de arriba): un ushort viaja por NBT como
        // Short reinterpretado bit a bit, nunca como un numero con signo de verdad - unchecked
        // recupera el valor real 0..65535.
        NbtShort s => unchecked((ushort)s.Value),
        NbtInt i => i.Value, // tolerancia real: version futura del formato que ensanche el campo
        _ => 0,
    };

    // Pasada secuencial UNICA sobre el blob anidado little-endian (BinaryWriter normal real, ver
    // el comentario de cabecera) - el ancho de cada celda depende de si esa celda es de mod
    // (Framed o no), asi que no hay forma de saltar directo a una posicion sin decodificar todo
    // lo anterior en el mismo orden [x][y] que TileIO.IOImpl.SaveData/LoadData reales.
    private static Dictionary<(int, int), int> ScanGrid(byte[] data, IReadOnlyDictionary<int, TwldModEntry> entries, int tilesWide, int tilesHigh,
        IReadOnlySet<(int X, int Y)> positionsOfInterest, bool hasFrame)
    {
        var result = new Dictionary<(int, int), int>();
        using var stream = new MemoryStream(data, writable: false);
        using var reader = new BinaryReader(stream); // little-endian real, a proposito distinto del NBT que lo envuelve

        int found = 0;
        for (int x = 0; x < tilesWide; x++)
        {
            for (int y = 0; y < tilesHigh; y++)
            {
                if (stream.Position >= stream.Length)
                    // Blob mas corto de lo que las dimensiones del .wld hermano hacian esperar -
                    // mundo/twld desincronizados (ej. mundo redimensionado con una herramienta
                    // ajena). Se corta sin inventar el resto, mismo criterio "lo que no se
                    // encuentra no se inventa" del resto del proyecto.
                    return result;

                ushort num = reader.ReadUInt16();
                if (num != 0)
                {
                    reader.ReadByte(); // color
                    bool framed = hasFrame && entries.TryGetValue(num, out var e) && e.FrameImportant;
                    if (framed) reader.ReadBytes(4); // frameX + frameY (Int16 + Int16)

                    if (positionsOfInterest.Contains((x, y)))
                    {
                        result[(x, y)] = num;
                        found++;
                        if (found >= positionsOfInterest.Count) return result;
                    }
                }
            }
        }
        return result;
    }
}
