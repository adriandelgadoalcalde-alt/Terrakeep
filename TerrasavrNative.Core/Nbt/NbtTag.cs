namespace TerrasavrNative.Core.Nbt;

// Jerarquia de tags NBT, un tipo concreto por variante (en vez de un unico "object Value")
// para que el codigo que lee/escribe items no tenga que castear a ciegas. LONG/LongArray se
// decodifican como long real (BinaryPrimitives, exacto) en vez del blob de 8 bytes opaco que
// usa la version JS - el motivo de que la version JS lo tratara como opaco es que JS no tiene
// enteros de 64 bits nativos, algo que no aplica aqui.
public abstract class NbtTag
{
    public abstract NbtTagType Type { get; }
}

public sealed class NbtByte(sbyte value) : NbtTag
{
    public sbyte Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Byte;
}

public sealed class NbtShort(short value) : NbtTag
{
    public short Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Short;
}

public sealed class NbtInt(int value) : NbtTag
{
    public int Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Int;
}

public sealed class NbtLong(long value) : NbtTag
{
    public long Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Long;
}

public sealed class NbtFloat(float value) : NbtTag
{
    public float Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Float;
}

public sealed class NbtDouble(double value) : NbtTag
{
    public double Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.Double;
}

public sealed class NbtByteArray(byte[] value) : NbtTag
{
    public byte[] Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.ByteArray;
}

public sealed class NbtString(string value) : NbtTag
{
    public string Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.String;
}

public sealed class NbtIntArray(int[] value) : NbtTag
{
    public int[] Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.IntArray;
}

public sealed class NbtLongArray(long[] value) : NbtTag
{
    public long[] Value { get; set; } = value;
    public override NbtTagType Type => NbtTagType.LongArray;
}

public sealed class NbtList(NbtTagType elementType, List<NbtTag>? items = null) : NbtTag
{
    public NbtTagType ElementType { get; set; } = elementType;
    public List<NbtTag> Items { get; } = items ?? [];
    public override NbtTagType Type => NbtTagType.List;
}

// Un Compound se trata como lista ORDENADA de (nombre, tag), no Dictionary - para preservar
// orden/duplicados exactos igual que la implementacion JS (calamity-nbt.js, comentario propio:
// "para preservar orden de campos y tipo exacto en el re-encode"). En la practica no debería
// haber duplicados reales, pero el formato en si no los prohibe.
public sealed class NbtCompound(List<(string Name, NbtTag Tag)>? fields = null) : NbtTag
{
    public List<(string Name, NbtTag Tag)> Fields { get; } = fields ?? [];
    public override NbtTagType Type => NbtTagType.Compound;

    public NbtTag? Get(string name)
    {
        foreach (var (n, tag) in Fields)
        {
            if (n == name) return tag;
        }
        return null;
    }

    // Reemplaza el campo si ya existe (misma posicion), lo anade al final si no.
    public void Set(string name, NbtTag value)
    {
        for (int i = 0; i < Fields.Count; i++)
        {
            if (Fields[i].Name == name)
            {
                Fields[i] = (name, value);
                return;
            }
        }
        Fields.Add((name, value));
    }

    public void Remove(string name) => Fields.RemoveAll(f => f.Name == name);

    public static NbtCompound Of(params (string Name, NbtTag Tag)[] fields) => new([.. fields]);
}
