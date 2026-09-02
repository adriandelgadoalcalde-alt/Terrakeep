using System.Text;
using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

public class BuffDurationPresetsTests
{
    // buff 5 = dato real conocido (28800 ticks); buff 257 = Suerte, con los 3 tiers reales
    // (18000/36000/54000); buff 999 = sin dato real, debe caer al fallback.
    private const string SampleJson = """
        {"5":{"min":28800,"src":"item","srcItem":288},
         "257":{"min":18000,"src":"item","srcItem":4477,"tiers":[18000,36000,54000]}}
        """;

    private static VanillaBuffDurationCatalog Load() =>
        VanillaBuffDurationCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

    [Fact]
    public void GetPresets_WithRealData_UsesRealMinAndDoublesForMedia()
    {
        var preset = BuffDurationPresets.GetPresets(5, characterVersion: 279, Load());
        Assert.Equal(28800, preset.MinTicks);
        Assert.True(preset.IsRealMin);
        Assert.Equal(28800 * 2, preset.MediaTicks);
    }

    [Fact]
    public void GetPresets_WithoutRealData_FallsBackAndMarksNotReal()
    {
        var preset = BuffDurationPresets.GetPresets(999, characterVersion: 279, Load());
        Assert.Equal(28800, preset.MinTicks); // moda real de los buffTime conocidos, no inventado
        Assert.False(preset.IsRealMin);
    }

    [Fact]
    public void GetPresets_LuckBuff_UsesRealThreeTierEscalator()
    {
        var preset = BuffDurationPresets.GetPresets(257, characterVersion: 279, Load());
        Assert.Equal(18000, preset.MinTicks);
        Assert.Equal(36000, preset.MediaTicks); // real (tier normal), no 2x calculado
        Assert.Equal(54000, preset.MaxTicks); // real (tier mayor), no S.getMaxTime()
    }

    [Theory]
    [InlineData(279, 1999999980)] // version >= 269: mismo umbral real que 44 vs 22 buffs
    [InlineData(268, 1080000)]
    public void GetPresets_MaxTicks_UsesRealVersionThreshold(int version, int expectedMax)
    {
        var preset = BuffDurationPresets.GetPresets(5, version, Load());
        Assert.Equal(expectedMax, preset.MaxTicks);
    }
}
