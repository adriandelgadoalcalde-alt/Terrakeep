using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels.Tests;

// Pedido explicito del usuario (4-sep-2026, tras H6-08): "extrae las mascotas de pueblo para
// Exploracion" - los 13 ids nuevos del roster (H6-08) ya tenian cabeza real (npc_heads/) pero
// no cuerpo real (npc_icons/, usado por la lista lateral de Exploracion) - hueco documentado
// explicitamente en bitacora.md hasta ahora.
public sealed class NpcIconResolverMascotasTests
{
    [Theory]
    [InlineData(368)] // TravelingMerchant
    [InlineData(441)] // TaxCollector
    [InlineData(637)] // TownCat
    [InlineData(638)] // TownDog
    [InlineData(656)] // TownBunny
    [InlineData(670)] // TownSlimeBlue
    [InlineData(678)] // TownSlimeGreen
    [InlineData(679)] // TownSlimeOld
    [InlineData(680)] // TownSlimePurple
    [InlineData(681)] // TownSlimeRainbow
    [InlineData(682)] // TownSlimeRed
    [InlineData(683)] // TownSlimeYellow
    [InlineData(684)] // TownSlimeCopper
    public void LosNCNuevosDelRosterYaTienenIconoDeCuerpoReal(int npcId)
    {
        Assert.NotNull(NpcIconResolver.GetIconPath(npcId));
    }

    [Fact]
    public void TodoElRosterReal_TieneIconoDeCuerpo()
    {
        // Ahora los 40 ids reales del roster completo (H6-08) tienen icono de cuerpo real -
        // ninguno cae en el "sin icono real" que documentaba el hueco original.
        var sinIcono = VanillaTownNpcRoster.Ids.Where(id => NpcIconResolver.GetIconPath(id) == null).ToList();
        Assert.Empty(sinIcono);
    }
}
