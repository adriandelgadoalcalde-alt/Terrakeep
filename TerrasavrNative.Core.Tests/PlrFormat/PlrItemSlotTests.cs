using TerrasavrNative.Core.PlrFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.PlrFormat;

public class PlrItemSlotTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void RoundTrip_NonEmptySlot(bool multi, bool includeFavorite)
    {
        var slot = new PlrItemSlot(Id: 5000, Count: multi ? 42 : 1, Prefix: 7, Favorited: includeFavorite);

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            slot.Write(writer, multi, includeFavorite);

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var readBack = PlrItemSlot.Read(reader, multi, includeFavorite, maxId: 16384);

        Assert.Equal(slot.Id, readBack.Id);
        Assert.Equal(slot.Count, readBack.Count);
        Assert.Equal(slot.Prefix, readBack.Prefix);
        Assert.Equal(slot.Favorited, readBack.Favorited);
    }

    [Fact]
    public void Read_ClampsIdAboveMaxId_ToEmpty()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(20000000); // id sintetico de Calamity, muy por encima de cualquier maxId vanilla
            writer.Write(1);
            writer.Write((byte)0);
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var slot = PlrItemSlot.Read(reader, multi: true, includeFavorite: false, maxId: 16384);

        Assert.True(slot.IsEmpty);
        Assert.Equal(0, slot.Id);
    }

    [Fact]
    public void Empty_IsEmpty()
    {
        Assert.True(PlrItemSlot.Empty.IsEmpty);
    }
}
