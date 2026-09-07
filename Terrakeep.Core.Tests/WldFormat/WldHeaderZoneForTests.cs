using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

public class WldHeaderZoneForTests
{
    private static WldHeader MakeHeader(int tilesHigh, double groundLevel, double rockLevel) => new()
    {
        Version = 279,
        Pointers = [0, 0, 0, 0, 0],
        TileFrameImportant = [],
        Title = "test",
        WorldId = 1,
        TilesHigh = tilesHigh,
        TilesWide = 1000,
        SpawnX = 0,
        SpawnY = 0,
        GroundLevel = groundLevel,
        RockLevel = rockLevel,
        Seed = "semilla-sintetica",
        GameMode = 0,
        DungeonX = 0,
        DungeonY = 0,
    };

    [Fact]
    public void ZoneFor_ReturnsExpectedZoneByDepth()
    {
        var header = MakeHeader(tilesHigh: 2000, groundLevel: 300, rockLevel: 500);

        Assert.Equal("Space", header.ZoneFor(0));
        Assert.Equal("Space", header.ZoneFor(79));
        Assert.Equal("Sky", header.ZoneFor(80));
        Assert.Equal("Sky", header.ZoneFor(300));
        Assert.Equal("Earth", header.ZoneFor(301));
        Assert.Equal("Earth", header.ZoneFor(500));
        Assert.Equal("Rock", header.ZoneFor(501));
        Assert.Equal("Rock", header.ZoneFor(1808)); // 2000-192 = 1808, todavia Rock
        Assert.Equal("Hell", header.ZoneFor(1809));
        Assert.Equal("Hell", header.ZoneFor(1999));
    }
}
