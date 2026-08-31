using TerrasavrNative.Core.Nbt;
using Xunit;

namespace TerrasavrNative.Core.Tests.Nbt;

public class NbtSerializerTests
{
    private static NbtCompound BuildSampleItem() => NbtCompound.Of(
        ("mod", new NbtString("CalamityMod")),
        ("name", new NbtString("Calamity")),
        ("slot", new NbtShort(5)),
        ("prefix", new NbtByte(12)),
        ("stack", new NbtInt(9999)),
        ("globalData", new NbtList(NbtTagType.Compound, [
            NbtCompound.Of(("counter", new NbtInt(3)))
        ]))
    );

    [Fact]
    public void RoundTrip_AllPrimitiveTagTypes()
    {
        var root = NbtCompound.Of(
            ("b", new NbtByte(-5)),
            ("s", new NbtShort(-1000)),
            ("i", new NbtInt(123456789)),
            ("l", new NbtLong(9_000_000_000L)),
            ("f", new NbtFloat(3.5f)),
            ("d", new NbtDouble(2.718281828)),
            ("ba", new NbtByteArray([1, 2, 3, 255])),
            ("str", new NbtString("hola, ñ, 日本語")),
            ("ia", new NbtIntArray([1, -2, 3])),
            ("la", new NbtLongArray([1L, -2L, 3_000_000_000L])),
            ("list", new NbtList(NbtTagType.Int, [new NbtInt(1), new NbtInt(2), new NbtInt(3)])),
            ("compound", NbtCompound.Of(("nested", new NbtString("valor"))))
        );

        using var stream = new MemoryStream();
        NbtSerializer.WriteRoot(stream, "raiz", root);
        stream.Position = 0;
        var (rootName, readBack) = NbtSerializer.ReadRoot(stream);

        Assert.Equal("raiz", rootName);
        Assert.Equal(root.Fields.Count, readBack.Fields.Count);

        Assert.Equal(-5, ((NbtByte)readBack.Get("b")!).Value);
        Assert.Equal(-1000, ((NbtShort)readBack.Get("s")!).Value);
        Assert.Equal(123456789, ((NbtInt)readBack.Get("i")!).Value);
        Assert.Equal(9_000_000_000L, ((NbtLong)readBack.Get("l")!).Value);
        Assert.Equal(3.5f, ((NbtFloat)readBack.Get("f")!).Value);
        Assert.Equal(2.718281828, ((NbtDouble)readBack.Get("d")!).Value);
        Assert.Equal(new byte[] { 1, 2, 3, 255 }, ((NbtByteArray)readBack.Get("ba")!).Value);
        Assert.Equal("hola, ñ, 日本語", ((NbtString)readBack.Get("str")!).Value);
        Assert.Equal([1, -2, 3], ((NbtIntArray)readBack.Get("ia")!).Value);
        Assert.Equal([1L, -2L, 3_000_000_000L], ((NbtLongArray)readBack.Get("la")!).Value);

        var list = (NbtList)readBack.Get("list")!;
        Assert.Equal(NbtTagType.Int, list.ElementType);
        Assert.Equal(3, list.Items.Count);
        Assert.Equal(2, ((NbtInt)list.Items[1]).Value);

        var nested = (NbtCompound)readBack.Get("compound")!;
        Assert.Equal("valor", ((NbtString)nested.Get("nested")!).Value);
    }

    [Fact]
    public void RoundTrip_ItemLikeStructure_PreservesFieldOrder()
    {
        var item = BuildSampleItem();
        using var stream = new MemoryStream();
        NbtSerializer.WriteRoot(stream, "Item", item);
        stream.Position = 0;
        var (_, readBack) = NbtSerializer.ReadRoot(stream);

        Assert.Equal(item.Fields.Select(f => f.Name), readBack.Fields.Select(f => f.Name));
        Assert.Equal("CalamityMod", ((NbtString)readBack.Get("mod")!).Value);
        Assert.Equal((short)5, ((NbtShort)readBack.Get("slot")!).Value);
        Assert.Equal((sbyte)12, ((NbtByte)readBack.Get("prefix")!).Value);
        Assert.Equal(9999, ((NbtInt)readBack.Get("stack")!).Value);

        var globalData = (NbtList)readBack.Get("globalData")!;
        Assert.Single(globalData.Items);
        var counterCompound = (NbtCompound)globalData.Items[0];
        Assert.Equal(3, ((NbtInt)counterCompound.Get("counter")!).Value);
    }

    [Fact]
    public void CompoundSet_ReplacesInPlace_DoesNotReorder()
    {
        var c = NbtCompound.Of(("a", new NbtInt(1)), ("b", new NbtInt(2)), ("c", new NbtInt(3)));
        c.Set("b", new NbtInt(99));
        Assert.Equal(["a", "b", "c"], c.Fields.Select(f => f.Name));
        Assert.Equal(99, ((NbtInt)c.Get("b")!).Value);
    }

    [Fact]
    public void CompoundSet_AppendsWhenMissing()
    {
        var c = NbtCompound.Of(("a", new NbtInt(1)));
        c.Set("z", new NbtInt(26));
        Assert.Equal(["a", "z"], c.Fields.Select(f => f.Name));
    }

    [Fact]
    public void String_EncodesLengthAsBigEndianUInt16OfUtf8Bytes()
    {
        // "ñ" es 2 bytes en UTF-8 - confirma que la longitud cuenta bytes, no caracteres.
        var root = NbtCompound.Of(("s", new NbtString("ñ")));
        using var stream = new MemoryStream();
        NbtSerializer.WriteRoot(stream, "r", root);
        var bytes = stream.ToArray();

        // byte 0: tipo compound (10); luego string "r" (len BE=1, 'r'); luego tag STRING (8);
        // luego nombre "s" (len BE=1, 's'); luego el string valor: len BE=2, luego 2 bytes utf-8 de "ñ".
        int i = 0;
        Assert.Equal((byte)NbtTagType.Compound, bytes[i++]);
        Assert.Equal(0, bytes[i++]); Assert.Equal(1, bytes[i++]); // len "r"
        Assert.Equal((byte)'r', bytes[i++]);
        Assert.Equal((byte)NbtTagType.String, bytes[i++]);
        Assert.Equal(0, bytes[i++]); Assert.Equal(1, bytes[i++]); // len "s"
        Assert.Equal((byte)'s', bytes[i++]);
        Assert.Equal(0, bytes[i++]); Assert.Equal(2, bytes[i++]); // len "ñ" en utf-8 = 2 bytes
    }
}
