using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Parche 3.4.2 - BUG REAL: la pestaña Builds enseñaba el prefijo con el nombre interno en inglés
// ("Legendary", "Godly"...) también con la app en español, porque pasaba a la vista el campo
// crudo "prefix" de builds.json. Ahora sale el nombre oficial del catálogo (es-ES: "Legendario")
// y cambia con el idioma en caliente.
public sealed class BuildsPrefijoIdiomaTests
{
    private static IEnumerable<BuildItemRowViewModel> FilasConPrefijo(MainViewModel vm) =>
        vm.Builds.VanillaStages.Concat(vm.Builds.CalamityStages)
            .SelectMany(s => s.Classes).SelectMany(c => c.AllRows)
            .Where(r => r.PrefixText != null);

    private static void ConIdioma(string idioma, Action<MainViewModel> cuerpo)
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            var vm = new MainViewModel();
            LocalizationService.Instance.SetLanguage(idioma);
            cuerpo(vm);
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }

    private static readonly string[] InglesInterno =
        ["Legendary", "Godly", "Mythical", "Unreal", "Agile", "Demonic", "Ruthless"];

    [Fact]
    public void ConLaAppEnEspanol_NingunPrefijoDeBuildsSaleEnIngles()
    {
        ConIdioma(LocalizationService.Spanish, vm =>
        {
            var filas = FilasConPrefijo(vm).ToList();
            Assert.NotEmpty(filas);
            Assert.All(filas, f => Assert.DoesNotContain(f.PrefixText, InglesInterno));
            Assert.Contains(filas, f => f.PrefixText == "Legendario");
        });
    }

    [Fact]
    public void ConLaAppEnIngles_SalenLosNombresEnIngles_YCambiaEnCaliente()
    {
        ConIdioma(LocalizationService.Spanish, vm =>
        {
            var fila = FilasConPrefijo(vm).First(f => f.PrefixText == "Legendario");
            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            Assert.Equal("Legendary", fila.PrefixText);
        });
    }

    [Fact]
    public void ElPrefijoRealDeCalamityDelBuildTambienSeResuelve()
    {
        ConIdioma(LocalizationService.Spanish, vm =>
        {
            Assert.Contains(vm.Builds.CalamityStages.SelectMany(s => s.Classes).SelectMany(c => c.AllRows),
                r => r.PrefixText == "Impecable");
        });
    }
}
