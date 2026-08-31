namespace TerrasavrNative.Core.PlrFormat;

// Un slot de item vanilla dentro del .plr - formato POSICIONAL (sin nombres de campo, a
// diferencia del NBT del .tplr), confirmado contra na.prototype.handle en script.js real:
//   Int32 id (0 si esta vacio; el propio motor lo pone a 0 si supera el maxId de esa version)
//   [Int32 count]        - SOLO si el contenedor es "multi" (armas/materiales apilables)
//   Byte prefix
//   [Byte isFavorited]   - SOLO si version >= favFlagMinVersion del contenedor (varia por
//                           contenedor: 0/145/269/322... ver la tabla real en PlrContainerSpec)
// Todo little-endian (heredado de PlrBinaryIO/BinaryReader-Writer nativos de .NET).
public readonly record struct PlrItemSlot(int Id, int Count, byte Prefix, bool Favorited)
{
    public static readonly PlrItemSlot Empty = new(0, 0, 0, false);

    public bool IsEmpty => Id == 0;

    public static PlrItemSlot Read(BinaryReader reader, bool multi, bool includeFavorite, int maxId)
    {
        int id = reader.ReadInt32();
        if (id < 0 || id > maxId) id = 0;

        int count = multi ? reader.ReadInt32() : (id != 0 ? 1 : 0);
        byte prefix = reader.ReadByte();
        bool favorited = includeFavorite && reader.ReadByte() != 0;

        if (id == 0) return Empty;
        return new PlrItemSlot(id, count, prefix, favorited);
    }

    public void Write(BinaryWriter writer, bool multi, bool includeFavorite)
    {
        writer.Write(Id);
        if (multi) writer.Write(Count);
        writer.Write(Prefix);
        if (includeFavorite) writer.Write((byte)(Favorited ? 1 : 0));
    }
}
