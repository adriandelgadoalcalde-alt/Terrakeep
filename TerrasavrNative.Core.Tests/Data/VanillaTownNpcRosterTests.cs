using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

public class VanillaTownNpcRosterTests
{
    [Fact]
    public void HasExpectedCountAndNoDuplicates()
    {
        Assert.Equal(27, VanillaTownNpcRoster.Ids.Count);
        Assert.Equal(VanillaTownNpcRoster.Ids.Count, VanillaTownNpcRoster.Ids.Distinct().Count());
    }

    [Fact]
    public void ContainsKnownTownNpcs()
    {
        // Guia (17) y Enfermera (124) - dos NPCs de pueblo bien conocidos, citados tal cual en
        // overrides.js (Terrasavr-Calamity-Beta) como referencia de que el id es el correcto.
        Assert.Contains(17, VanillaTownNpcRoster.Ids);
        Assert.Contains(124, VanillaTownNpcRoster.Ids);
    }
}
