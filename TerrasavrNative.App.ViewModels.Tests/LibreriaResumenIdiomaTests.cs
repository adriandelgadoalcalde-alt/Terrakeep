using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// Ronda de Libreria/Builds del 6-sep-2026 - BUG REAL: ResultsSummary (y SlotRestrictionLabel en
// la Libreria de objetos) son strings YA RESUELTOS dentro de ApplyFilter, no bindings indexados
// contra el diccionario - o sea que el aviso "Item[]" de LocalizationService, que refresca solo
// lo que se lee via {Binding Loc[clave]}, no les llegaba nunca. Cambiar de idioma dejaba la linea
// de resumen de las TRES superficies (Libreria, Libreria de buffs, Investigacion) congelada en el
// idioma anterior hasta que el usuario volviera a teclear o a pulsar una carpeta - y en el estado
// de arranque (sin busqueda ni carpeta) ese resumen es literalmente el UNICO texto del panel
// derecho, asi que se quedaba una frase entera en el idioma que no toca, a la vista.
//
// Por que no lo cazo el barrido A10-IDIOMA-BARRIDO del arnes: ese barrido navega PULSANDO
// carpetas, y cada pulsacion vuelve a llamar a ApplyFilter, que regenera el texto EN EL IDIOMA
// ACTIVO. La version rancia solo existe si NO se refiltra despues de cambiar de idioma, que es
// justo lo que hace el usuario real.
public sealed class LibreriaResumenIdiomaTests
{
    // El idioma se fija SIEMPRE despues de construir el MainViewModel, nunca antes: su propio
    // constructor carga los ajustes reales del usuario del disco (%LOCALAPPDATA%\Terrakeep\
    // settings.json, global de la maquina) y llama a SetLanguage con lo que haya ahi - un idioma
    // puesto antes se pierde sin avisar. Y LocalizationService es un singleton de proceso: se
    // devuelve como estaba al terminar, o contamina al resto (misma leccion que session.json en
    // el arnes).
    private static void ConLibreriaEnEspañolYLuegoIngles(Action<MainViewModel> comprobar)
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            var vm = new MainViewModel();
            LocalizationService.Instance.SetLanguage(LocalizationService.Spanish);
            comprobar(vm);
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }

    [Fact]
    public void ElResumenDeLaLibreriaCambiaDeIdiomaSinVolverABuscarNiPulsarCarpeta()
    {
        ConLibreriaEnEspañolYLuegoIngles(vm =>
        {
            string enEspañol = vm.Library.ResultsSummary;
            Assert.Contains("objetos en total", enEspañol);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            Assert.NotEqual(enEspañol, vm.Library.ResultsSummary);
            Assert.Contains("items in total", vm.Library.ResultsSummary);
        });
    }

    [Fact]
    public void ElResumenDeLaLibreriaDeBuffsTambienCambiaDeIdiomaSolo()
    {
        ConLibreriaEnEspañolYLuegoIngles(vm =>
        {
            Assert.Contains("buffs en total", vm.BuffLibrary.ResultsSummary);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            Assert.Contains("buffs in total", vm.BuffLibrary.ResultsSummary);
        });
    }

    // Con una carpeta ya elegida el resumen incluye ADEMAS el nombre de esa carpeta, que ya
    // cambiaba de idioma solo (CategoryNodeViewModel.Name) - refiltrar entero, en vez de solo
    // recalcular la frase, es lo que mantiene las dos mitades en el mismo idioma en vez de dejar
    // una frase mitad inglesa mitad española.
    [Fact]
    public void ConUnaCarpetaElegida_ElResumenYElNombreDeLaCarpetaCambianALaVez()
    {
        ConLibreriaEnEspañolYLuegoIngles(vm =>
        {
            var carpeta = vm.Library.RootCategories.First(c => c.ItemIdsOrdered.Count > 0);
            vm.Library.SelectCategoryCommand.Execute(carpeta);
            Assert.Contains($" en \"{carpeta.Name}\"", vm.Library.ResultsSummary);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            // Las dos mitades en ingles: la plantilla real (" in \"{0}\"") y el nombre INGLES de
            // la propia carpeta, que ya viaja en el nodo (CategoryTreeNodeData.NameEn).
            Assert.Contains($" in \"{carpeta.Name}\"", vm.Library.ResultsSummary);
            Assert.DoesNotContain(" en \"", vm.Library.ResultsSummary);
        });
    }
}
