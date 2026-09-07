using Terrakeep.Core.Nbt;
using Xunit;

namespace Terrakeep.Core.Tests.Nbt;

public class TplrFileTests
{
    [Fact]
    public void RoundTrip_GzipPlusNbt()
    {
        var root = NbtCompound.Of(
            ("armor", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(
                    ("mod", new NbtString("CalamityMod")),
                    ("name", new NbtString("AuricPlatebodyOfTheSky")),
                    ("slot", new NbtShort(1))
                )
            ]))
        );

        byte[] fileBytes = TplrFile.Write("Player", root);
        var (rootName, readBack) = TplrFile.Read(fileBytes);

        Assert.Equal("Player", rootName);
        var armorList = (NbtList)readBack.Get("armor")!;
        var entry = (NbtCompound)armorList.Items[0];
        Assert.Equal("CalamityMod", ((NbtString)entry.Get("mod")!).Value);
        Assert.Equal((short)1, ((NbtShort)entry.Get("slot")!).Value);
    }

    [Fact]
    public void Write_ProducesValidGzipHeader()
    {
        byte[] fileBytes = TplrFile.Write("x", new NbtCompound());
        // Cabecera gzip estandar: 0x1F 0x8B.
        Assert.Equal(0x1F, fileBytes[0]);
        Assert.Equal(0x8B, fileBytes[1]);
    }
}
