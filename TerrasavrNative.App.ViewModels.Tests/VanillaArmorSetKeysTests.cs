using System.IO;
using System.Text.Json;

namespace TerrasavrNative.App.ViewModels.Tests;

// A9-05-SETVANILLA (informe de pulido final, C-10a, cierra L3-a): "un simple diff de conjuntos
// sobre dos ficheros... deberia ejecutarse en cada compilacion, porque es lo que evita que el
// catalogo de sets vuelva a quedarse corto en silencio". El "otro fichero" es esta lista -
// las 63 claves reales `ArmorSetBonus.*` que aparecen en
// Downloads\tModLoader-Decompiled\tModLoader\Terraria\Player.cs (extraidas con
// `grep -oE "ArmorSetBonus\.[A-Za-z0-9_]+" Player.cs | sort -u`, capturadas aqui en vez de leer
// el decompilado en cada build porque esa ruta es especifica de esta maquina, no parte del
// repositorio). Si algun dia el juego real añade un set nuevo, esta lista tambien hay que
// actualizarla a mano tras volver a mirar el decompilado - el test avisara con un diff exacto
// de que claves sobran/faltan en vez de fallar en silencio.
public sealed class VanillaArmorSetKeysTests
{
    private static readonly string[] ClavesRealesDePlayerCs =
    [
        "AdamantiteCaster", "AdamantiteMelee", "AdamantiteRanged", "Angler", "ApprenticeTier2",
        "ApprenticeTier3", "AshWood", "Bee", "BeetleDamage", "BeetleDefense", "Bone", "Cactus",
        "Chlorophyte", "ChlorophyteMelee", "CobaltCaster", "CobaltMelee", "CobaltRanged",
        "Crimson", "CrystalNinja", "Forbidden", "Fossil", "Frost", "Gladiator", "Hallowed",
        "HallowedSummoner", "HuntressTier2", "HuntressTier3", "Jungle", "MagicHat", "MetalTier1",
        "MetalTier2", "Meteor", "Mining", "Molten", "MonkTier2", "MonkTier3", "MythrilCaster",
        "MythrilMelee", "MythrilRanged", "Nebula", "Ninja", "ObsidianOutlaw", "Orichalcum",
        "Palladium", "Platinum", "Pumpkin", "ShadowScale", "Shroomite", "Snow", "Solar",
        "SpectreDamage", "SpectreHealing", "Spider", "Spooky", "SquireTier2", "SquireTier3",
        "Stardust", "Tiki", "Titanium", "Turtle", "Vortex", "Wizard", "Wood",
    ];

    [Fact]
    public void VanillaArmorSetsJson_TieneExactamenteLas63ClavesRealesDePlayerCs()
    {
        Assert.Equal(63, ClavesRealesDePlayerCs.Length);

        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla_armor_sets.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var clavesGeneradas = doc.RootElement.EnumerateObject()
            .Select(p => p.Value.GetProperty("key").GetString()!)
            .ToHashSet();

        var reales = ClavesRealesDePlayerCs.ToHashSet();
        var faltan = reales.Except(clavesGeneradas).ToList();
        var sobran = clavesGeneradas.Except(reales).ToList();

        Assert.True(faltan.Count == 0 && sobran.Count == 0,
            $"diferencia real entre vanilla_armor_sets.json y ArmorSetBonus.* de Player.cs - faltan: [{string.Join(", ", faltan)}], sobran: [{string.Join(", ", sobran)}]");
    }
}
