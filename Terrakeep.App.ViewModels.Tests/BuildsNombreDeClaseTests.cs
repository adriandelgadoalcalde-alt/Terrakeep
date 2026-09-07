using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Ronda de Libreria/Builds del 6-sep-2026 - BUG REAL: el titulo de cada columna de la pestaña
// Builds pintaba ClassName TAL CUAL, o sea la clave interna en bruto de builds.json ("melee",
// "ranged", "mage", "summoner", "rogue"), mientras que las pildoras de filtro de arriba, en la
// MISMA pantalla, ya decian "Cuerpo a cuerpo"/"A distancia"/"Magia"/"Invocación"/"Pícaro". El
// mismo concepto con dos nombres distintos a un palmo de distancia, y uno de los dos ni siquiera
// es un idioma: es el identificador del fichero de datos. Con la app en ingles pasaba igual
// ("mage" en minusculas en vez de "Magic").
public sealed class BuildsNombreDeClaseTests
{
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

    [Fact]
    public void ElTituloDeCadaClaseUsaElNombreRealDelJuego_NoLaClaveDeBuildsJson()
    {
        ConIdioma(LocalizationService.Spanish, vm =>
        {
            var clases = vm.Builds.VanillaStages.Concat(vm.Builds.CalamityStages)
                .SelectMany(s => s.Classes).ToList();
            Assert.NotEmpty(clases);

            // Ninguna columna puede seguir enseñando la clave interna tal cual.
            Assert.All(clases, c => Assert.NotEqual(c.ClassName, c.ClassLabel));
            Assert.Contains(clases, c => c.ClassName == "melee" && c.ClassLabel == "Cuerpo a cuerpo");
            Assert.Contains(clases, c => c.ClassName == "rogue" && c.ClassLabel == "Pícaro");
            // Y el rotulo tiene que ser EXACTAMENTE el mismo que ya usa la pildora de filtro de la
            // misma clase: el bug era justo que fueran dos textos distintos en la misma pantalla.
            foreach (var c in clases)
            {
                var pildora = vm.Builds.ClassFilterOptions.FirstOrDefault(o => o.Key == c.ClassName);
                if (pildora != null) Assert.Equal(pildora.Label, c.ClassLabel);
            }
        });
    }

    [Fact]
    public void ElTituloDeClaseCambiaDeIdiomaEnCaliente()
    {
        ConIdioma(LocalizationService.Spanish, vm =>
        {
            var melee = vm.Builds.VanillaStages.SelectMany(s => s.Classes).First(c => c.ClassName == "melee");
            Assert.Equal("Cuerpo a cuerpo", melee.ClassLabel);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            Assert.Equal("Melee", melee.ClassLabel);
        });
    }

    // Respaldo real: una clase que un catalogo futuro traiga y que no tenga clave de idioma cae a
    // su nombre interno tal cual - nunca al "[clave]" en bruto del diccionario. Mismo criterio que
    // ya aplica BuildClassFilterOptionViewModel.
    [Fact]
    public void UnaClaseDesconocidaCaeASuNombreInterno_NoAUnaClaveEnBruto()
    {
        var gear = new Terrakeep.Core.Data.BuildClassGear();
        var vm = new BuildClassGearViewModel("necromancer", [], [], [], gear);

        Assert.Equal("necromancer", vm.ClassLabel);
    }
}
