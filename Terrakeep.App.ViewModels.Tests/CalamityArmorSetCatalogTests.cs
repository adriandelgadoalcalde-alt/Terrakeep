using System.IO;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels.Tests;

// C-10b (informe de pulido final, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.10, cierra
// L3-b): catalogo de SET completo de Calamity, derivado de catalog.json real (Category/
// EquipSlot/SetBonus ya extraidos) - antes "ActiveCalamitySetBonusText" comparaba tres SetBonus
// que NUNCA coincidian (cuerpo/piernas de Calamity no tienen SetBonus propio, 0/131 reales).
// Ids sinteticos reales verificados contra catalog.json (mismo criterio que
// CalamityArmorSlotTests): AerospecBreastplate=20000243 (Body), AerospecHeadMelee=20000245
// (Head, 1 de 5 cascos reales del set), AerospecLeggings=20000249 (Legs).
public sealed class CalamityArmorSetCatalogTests
{
    private const int AerospecBreastplateId = 20000243;
    private const int AerospecHeadMagicId = 20000244;
    private const int AerospecHeadMeleeId = 20000245;
    private const int AerospecLeggingsId = 20000249;

    // MarniteArchitect: unico set REAL de Calamity de solo 2 piezas (cabeza+cuerpo, sin
    // piernas) - gemelo Calamity de Wizard/MagicHat en vanilla (modo 3 de C-10a).
    private const int MarniteArchitectHeadgearId = 20000310;
    private const int MarniteArchitectTogaId = 20000311;

    private static CalamityArmorSetCatalog LoadReal()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "calamity", "catalog.json");
        var catalog = CalamityCatalog.LoadFromFile(path);
        return CalamityArmorSetCatalog.Build(catalog);
    }

    [Fact]
    public void FindSetContaining_LasTresPiezasRealesDeAerospecApuntanAlMismoSet()
    {
        var sets = LoadReal();

        var porCuerpo = sets.FindSetContaining(AerospecBreastplateId);
        var porCasco = sets.FindSetContaining(AerospecHeadMeleeId);
        var porPiernas = sets.FindSetContaining(AerospecLeggingsId);

        Assert.NotNull(porCuerpo);
        Assert.Same(porCuerpo, porCasco);
        Assert.Same(porCuerpo, porPiernas);
        Assert.Equal("Armor/Aerospec", porCuerpo!.Category);
        Assert.Equal(5, porCuerpo.Heads.Count); // Magic/Melee/Ranged/Rogue/Summon reales
    }

    [Fact]
    public void BonusForEquipped_LasTresPiezasRealesPuestas_DevuelveElTextoDelCascoConcreto()
    {
        var sets = LoadReal();

        string? bonusMelee = sets.BonusForEquipped(AerospecHeadMeleeId, AerospecBreastplateId, AerospecLeggingsId);
        string? bonusMagic = sets.BonusForEquipped(AerospecHeadMagicId, AerospecBreastplateId, AerospecLeggingsId);

        Assert.False(string.IsNullOrEmpty(bonusMelee));
        Assert.False(string.IsNullOrEmpty(bonusMagic));
        Assert.NotEqual(bonusMelee, bonusMagic); // cada casco real tiene su PROPIO texto, no comparten
    }

    [Fact]
    public void BonusForEquipped_CuerpoDeOtroSet_DevuelveNull()
    {
        var sets = LoadReal();

        string? bonus = sets.BonusForEquipped(AerospecHeadMeleeId, MarniteArchitectTogaId, AerospecLeggingsId);

        Assert.Null(bonus);
    }

    [Fact]
    public void BonusForEquipped_SetRealDeDosPiezas_FuncionaSinPiernas()
    {
        var sets = LoadReal();
        var set = sets.FindSetContaining(MarniteArchitectHeadgearId);
        Assert.NotNull(set);
        Assert.Null(set!.LegsSyntheticId);

        string? bonus = sets.BonusForEquipped(MarniteArchitectHeadgearId, MarniteArchitectTogaId, legsItemId: -1);

        Assert.False(string.IsNullOrEmpty(bonus));
    }
}
