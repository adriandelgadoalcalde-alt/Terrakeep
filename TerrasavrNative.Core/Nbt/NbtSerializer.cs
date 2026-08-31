using System.Buffers.Binary;
using System.Text;

namespace TerrasavrNative.Core.Nbt;

// Lector/escritor NBT genuino (estilo Minecraft, confirmado contra calamity-nbt.js real):
// TODOS los enteros multi-byte son BIG-ENDIAN (al contrario que el .plr vanilla, que es
// little-endian). Los strings llevan su longitud como UInt16 BE en BYTES utf-8, no en
// caracteres. No hay ninguna cabecera propia por encima de esto - un .tplr es simplemente
// gzip(NBT), ver TplrFile en PlrFormat/.
public static class NbtSerializer
{
    public static (string RootName, NbtCompound Root) ReadRoot(Stream stream)
    {
        var rootType = (NbtTagType)ReadByte(stream);
        if (rootType != NbtTagType.Compound)
            throw new InvalidDataException($"Se esperaba un Compound como raiz NBT, se encontro tag {rootType}");
        string rootName = ReadString(stream);
        var root = ReadCompoundBody(stream);
        return (rootName, root);
    }

    public static void WriteRoot(Stream stream, string rootName, NbtCompound root)
    {
        WriteByte(stream, (byte)NbtTagType.Compound);
        WriteString(stream, rootName);
        WriteCompoundBody(stream, root);
    }

    private static NbtCompound ReadCompoundBody(Stream stream)
    {
        var fields = new List<(string, NbtTag)>();
        while (true)
        {
            var tagType = (NbtTagType)ReadByte(stream);
            if (tagType == NbtTagType.End) break;
            string name = ReadString(stream);
            var value = ReadTag(stream, tagType);
            fields.Add((name, value));
        }
        return new NbtCompound(fields);
    }

    private static void WriteCompoundBody(Stream stream, NbtCompound compound)
    {
        foreach (var (name, tag) in compound.Fields)
        {
            WriteByte(stream, (byte)tag.Type);
            WriteString(stream, name);
            WriteTag(stream, tag);
        }
        WriteByte(stream, (byte)NbtTagType.End);
    }

    private static NbtTag ReadTag(Stream stream, NbtTagType type) => type switch
    {
        NbtTagType.Byte => new NbtByte((sbyte)ReadByte(stream)),
        NbtTagType.Short => new NbtShort(ReadInt16(stream)),
        NbtTagType.Int => new NbtInt(ReadInt32(stream)),
        NbtTagType.Long => new NbtLong(ReadInt64(stream)),
        NbtTagType.Float => new NbtFloat(ReadFloat(stream)),
        NbtTagType.Double => new NbtDouble(ReadDouble(stream)),
        NbtTagType.ByteArray => new NbtByteArray(ReadBytesExact(stream, ReadInt32(stream))),
        NbtTagType.String => new NbtString(ReadString(stream)),
        NbtTagType.List => ReadList(stream),
        NbtTagType.Compound => ReadCompoundBody(stream),
        NbtTagType.IntArray => new NbtIntArray(ReadIntArray(stream)),
        NbtTagType.LongArray => new NbtLongArray(ReadLongArray(stream)),
        _ => throw new InvalidDataException($"Tipo de tag NBT desconocido: {(byte)type}"),
    };

    private static void WriteTag(Stream stream, NbtTag tag)
    {
        switch (tag)
        {
            case NbtByte b: WriteByte(stream, unchecked((byte)b.Value)); break;
            case NbtShort s: WriteInt16(stream, s.Value); break;
            case NbtInt i: WriteInt32(stream, i.Value); break;
            case NbtLong l: WriteInt64(stream, l.Value); break;
            case NbtFloat f: WriteFloat(stream, f.Value); break;
            case NbtDouble d: WriteDouble(stream, d.Value); break;
            case NbtByteArray ba: WriteInt32(stream, ba.Value.Length); stream.Write(ba.Value); break;
            case NbtString str: WriteString(stream, str.Value); break;
            case NbtList list: WriteList(stream, list); break;
            case NbtCompound compound: WriteCompoundBody(stream, compound); break;
            case NbtIntArray ia: WriteInt32(stream, ia.Value.Length); foreach (var v in ia.Value) WriteInt32(stream, v); break;
            case NbtLongArray la: WriteInt32(stream, la.Value.Length); foreach (var v in la.Value) WriteInt64(stream, v); break;
            default: throw new InvalidDataException($"Tipo de tag NBT no serializable: {tag.GetType()}");
        }
    }

    private static NbtList ReadList(Stream stream)
    {
        var elementType = (NbtTagType)ReadByte(stream);
        int count = ReadInt32(stream);
        var items = new List<NbtTag>(Math.Max(0, count));
        for (int i = 0; i < count; i++)
            items.Add(ReadTag(stream, elementType));
        return new NbtList(elementType, items);
    }

    private static void WriteList(Stream stream, NbtList list)
    {
        WriteByte(stream, (byte)list.ElementType);
        WriteInt32(stream, list.Items.Count);
        foreach (var item in list.Items)
            WriteTag(stream, item);
    }

    private static int[] ReadIntArray(Stream stream)
    {
        int count = ReadInt32(stream);
        var arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = ReadInt32(stream);
        return arr;
    }

    private static long[] ReadLongArray(Stream stream)
    {
        int count = ReadInt32(stream);
        var arr = new long[count];
        for (int i = 0; i < count; i++) arr[i] = ReadInt64(stream);
        return arr;
    }

    // --- primitivas big-endian ---

    private static byte ReadByte(Stream stream)
    {
        int b = stream.ReadByte();
        if (b < 0) throw new EndOfStreamException("Fin de stream inesperado leyendo NBT.");
        return (byte)b;
    }

    private static void WriteByte(Stream stream, byte value) => stream.WriteByte(value);

    private static byte[] ReadBytesExact(Stream stream, int count)
    {
        var buf = new byte[count];
        stream.ReadExactly(buf);
        return buf;
    }

    private static short ReadInt16(Stream stream)
    {
        Span<byte> buf = stackalloc byte[2];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadInt16BigEndian(buf);
    }

    private static void WriteInt16(Stream stream, short value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buf, value);
        stream.Write(buf);
    }

    private static int ReadInt32(Stream stream)
    {
        Span<byte> buf = stackalloc byte[4];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadInt32BigEndian(buf);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, value);
        stream.Write(buf);
    }

    private static long ReadInt64(Stream stream)
    {
        Span<byte> buf = stackalloc byte[8];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadInt64BigEndian(buf);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buf, value);
        stream.Write(buf);
    }

    private static float ReadFloat(Stream stream)
    {
        Span<byte> buf = stackalloc byte[4];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadSingleBigEndian(buf);
    }

    private static void WriteFloat(Stream stream, float value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buf, value);
        stream.Write(buf);
    }

    private static double ReadDouble(Stream stream)
    {
        Span<byte> buf = stackalloc byte[8];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadDoubleBigEndian(buf);
    }

    private static void WriteDouble(Stream stream, double value)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buf, value);
        stream.Write(buf);
    }

    // Longitud en BYTES utf-8 (no en caracteres), UInt16 big-endian - confirmado contra
    // calamity-nbt.js real, distinto del formato de string del .plr vanilla (ver PlrFormat/).
    private static string ReadString(Stream stream)
    {
        Span<byte> lenBuf = stackalloc byte[2];
        stream.ReadExactly(lenBuf);
        ushort len = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
        if (len == 0) return string.Empty;
        var bytes = new byte[len];
        stream.ReadExactly(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteString(Stream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
            throw new InvalidDataException($"String NBT demasiado largo ({bytes.Length} bytes UTF-8, maximo {ushort.MaxValue}).");
        Span<byte> lenBuf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(lenBuf, (ushort)bytes.Length);
        stream.Write(lenBuf);
        stream.Write(bytes);
    }
}
