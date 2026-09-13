using Terrakeep.App.ViewModels;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.App.ViewModels.Tests;

// Filtros combinables de la Libreria (encargo del usuario, 13-sep-2026: "amplia la busqueda con
// filtros combinables - tipo de objeto, rareza, si es equipable en que slot"). Los datos de cada
// prueba salen del catalogo REAL cargado por MainViewModel (nunca ids inventados a mano) para no
// depender de trivia de Terraria que pueda cambiar de version.
public sealed class LibraryFilterTests
{
    // Dominio fijo real (ver LibraryViewModel.RarityDomain/DamageKindDomain/EquipSlotDomain).
    private const int TotalRarityChips = 12; // 11 rarezas reales + "Blanca"
    private const int TotalDamageKindChips = 5;
    private const int TotalEquipSlotChips = 12; // los 12 bits reales de SlotKind

    [Fact]
    public void LasTresListasDePastillasTienenElDominioFijoReal()
    {
        var lib = new MainViewModel().Library;

        Assert.Equal(TotalRarityChips, lib.RarityChips.Count);
        Assert.Equal(TotalDamageKindChips, lib.DamageKindChips.Count);
        Assert.Equal(TotalEquipSlotChips, lib.EquipSlotChips.Count);
        Assert.All(lib.RarityChips, c => Assert.False(c.IsSelected));
        Assert.All(lib.DamageKindChips, c => Assert.False(c.IsSelected));
        Assert.All(lib.EquipSlotChips, c => Assert.False(c.IsSelected));
        Assert.Equal(0, lib.ActiveFilterCount);
        Assert.False(lib.HasActiveFilters);
    }

    [Fact]
    public void MarcarUnaPastillaDeRarezaFiltraYSacaDeLasCartasRaiz()
    {
        var lib = new MainViewModel().Library;
        Assert.True(lib.ShowRootCategoryCards); // estado inicial real, sin busqueda/carpeta/filtro

        var azul = lib.RarityChips.Single(c => c.Value == 1);
        lib.ToggleRarityChipCommand.Execute(azul);

        Assert.True(azul.IsSelected);
        Assert.Equal(1, lib.ActiveFilterCount);
        Assert.True(lib.HasActiveFilters);
        Assert.False(lib.ShowRootCategoryCards); // ya no tiene sentido enseñar las carpetas genericas
        Assert.NotEmpty(lib.Results);
        // Rareza es real solo para vanilla (Calamity nunca tiene Rarity, ver el comentario de
        // LibraryItemViewModel) - la pastilla de rareza excluye Calamity del todo en vez de
        // fingir que "sin dato" es lo mismo que "rareza 1".
        Assert.All(lib.Results, i => Assert.False(i.IsCalamity));
        Assert.All(lib.Results, i => Assert.Equal(1, i.Rarity));
    }

    [Fact]
    public void DosPastillasDeLaMismaRarezaSonOr()
    {
        var lib = new MainViewModel().Library;
        var azul = lib.RarityChips.Single(c => c.Value == 1);
        var verde = lib.RarityChips.Single(c => c.Value == 2);

        lib.ToggleRarityChipCommand.Execute(azul);
        lib.ToggleRarityChipCommand.Execute(verde);

        Assert.Equal(2, lib.ActiveFilterCount);
        Assert.NotEmpty(lib.Results);
        Assert.All(lib.Results, i => Assert.True(i.Rarity is 1 or 2));
        Assert.Contains(lib.Results, i => i.Rarity == 1);
        Assert.Contains(lib.Results, i => i.Rarity == 2);
    }

    [Fact]
    public void RarezaYRanuraDeEquipoSonAndEntreGrupos()
    {
        var lib = new MainViewModel().Library;
        var accesorio = lib.EquipSlotChips.Single(c => c.Value == SlotKind.Accessory);
        lib.ToggleEquipSlotChipCommand.Execute(accesorio);
        Assert.NotEmpty(lib.Results);
        Assert.All(lib.Results, i => Assert.True((i.EquipSlotKind & SlotKind.Accessory) != 0));
        int soloAccesorios = lib.Results.Count;

        var azul = lib.RarityChips.Single(c => c.Value == 1);
        lib.ToggleRarityChipCommand.Execute(azul);

        Assert.Equal(2, lib.ActiveFilterCount);
        Assert.All(lib.Results, i => Assert.True((i.EquipSlotKind & SlotKind.Accessory) != 0 && i.Rarity == 1 && !i.IsCalamity));
        // Combinar dos filtros reales nunca puede dar MAS resultados que uno solo.
        Assert.True(lib.Results.Count <= soloAccesorios);
    }

    [Fact]
    public void PastillaDeTipoDeDañoFuncionaTambienEnCalamity()
    {
        var lib = new MainViewModel().Library;
        var raiz = lib.RootCategories.SingleOrDefault(n => n.Name == "Calamity (mod)");
        Assert.NotNull(raiz); // arbol real (LibraryCategoryTreeBuilder) - ver "Lo que cambia..." en bitacora.md
        lib.SelectCategoryCommand.Execute(raiz);
        Assert.NotEmpty(lib.Results);
        Assert.All(lib.Results, i => Assert.True(i.IsCalamity));

        var cuerpoACuerpo = lib.DamageKindChips.Single(c => c.Value == ItemDamageKind.Melee);
        lib.ToggleDamageKindChipCommand.Execute(cuerpoACuerpo);

        Assert.NotEmpty(lib.Results); // Calamity SI tiene armas de cuerpo a cuerpo reales (DamageType)
        Assert.All(lib.Results, i => Assert.True(i.IsCalamity && i.DamageKind == ItemDamageKind.Melee));
    }

    [Fact]
    public void RanuraDeEquipoDeArmaduraFuncionaTambienEnCalamity()
    {
        var lib = new MainViewModel().Library;
        var raiz = lib.RootCategories.Single(n => n.Name == "Calamity (mod)");
        lib.SelectCategoryCommand.Execute(raiz);

        var cabeza = lib.EquipSlotChips.Single(c => c.Value == SlotKind.ArmorHead);
        lib.ToggleEquipSlotChipCommand.Execute(cabeza);

        Assert.NotEmpty(lib.Results); // Calamity SI tiene cascos reales (Category="Armor/..." + EquipSlot="Head")
        Assert.All(lib.Results, i => Assert.True(i.IsCalamity && (i.EquipSlotKind & SlotKind.ArmorHead) != 0));
    }

    [Fact]
    public void LimpiarFiltrosResetaLasTresListasYVuelveAMostrarLasCartasRaiz()
    {
        var lib = new MainViewModel().Library;
        lib.ToggleRarityChipCommand.Execute(lib.RarityChips[0]);
        lib.ToggleDamageKindChipCommand.Execute(lib.DamageKindChips[0]);
        lib.ToggleEquipSlotChipCommand.Execute(lib.EquipSlotChips[0]);
        Assert.Equal(3, lib.ActiveFilterCount);

        lib.ClearFiltersCommand.Execute(null);

        Assert.Equal(0, lib.ActiveFilterCount);
        Assert.False(lib.HasActiveFilters);
        Assert.All(lib.RarityChips, c => Assert.False(c.IsSelected));
        Assert.All(lib.DamageKindChips, c => Assert.False(c.IsSelected));
        Assert.All(lib.EquipSlotChips, c => Assert.False(c.IsSelected));
        Assert.True(lib.ShowRootCategoryCards);
    }

    [Fact]
    public void ElBotonDeFiltrosAlternaElPopup()
    {
        var lib = new MainViewModel().Library;
        Assert.False(lib.IsFiltersOpen);

        lib.ToggleFiltersOpenCommand.Execute(null);
        Assert.True(lib.IsFiltersOpen);

        lib.ToggleFiltersOpenCommand.Execute(null);
        Assert.False(lib.IsFiltersOpen);
    }

    [Fact]
    public void LasEtiquetasDeLasPastillasCambianDeIdiomaEnVivo()
    {
        var lib = new MainViewModel().Library;
        var cabeza = lib.EquipSlotChips.Single(c => c.Value == SlotKind.ArmorHead);
        Terrakeep.App.Services.LocalizationService.Instance.SetLanguage("es");
        Assert.Equal("Cabeza", cabeza.Label);

        try
        {
            Terrakeep.App.Services.LocalizationService.Instance.SetLanguage("en");
            Assert.Equal("Head", cabeza.Label);
        }
        finally
        {
            Terrakeep.App.Services.LocalizationService.Instance.SetLanguage("es"); // no filtrar el idioma a otras pruebas
        }
    }
}
