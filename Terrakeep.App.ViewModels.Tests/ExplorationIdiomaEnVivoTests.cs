using System.IO;
using System.Threading.Tasks;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Oleada del 6-sep-2026 (area "Exploracion del mundo"): MISMO bug real que el agente de Inicio
// arreglo en HomeViewModel.ScanMessage, y por el mismo motivo - los textos persistentes de esta
// pestaña (la franja de estado bajo el mapa, el aviso del lanzador de mundos, la dificultad y el
// resultado de guardarla) se guardaban YA RESUELTOS con LocalizationService, asi que se quedaban
// congelados en el idioma que hubiera en ese instante y un cambio de idioma en vivo no los tocaba.
//
// Estas pruebas van por el camino real de la ViewModel y comparan contra el DICCIONARIO, nunca
// contra un texto escrito a mano aqui: si mañana se reescribe la frase en español o en ingles, la
// prueba sigue siendo valida.
public sealed class ExplorationIdiomaEnVivoTests
{
    private static void ConIdioma(string idioma, System.Action cuerpo)
    {
        string antes = LocalizationService.Instance.Language;
        try { LocalizationService.Instance.SetLanguage(idioma); cuerpo(); }
        finally { LocalizationService.Instance.SetLanguage(antes); }
    }

    [Fact]
    public void ElEstadoDeArranque_SeTraduceAlCambiarDeIdiomaEnVivo()
    {
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = new MainViewModel().Exploration;
            string enEspañol = vm.StatusMessage;

            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            string enIngles = vm.StatusMessage;

            Assert.Equal(LocalizationService.Instance["status_no_world_loaded_dot"], enIngles);
            Assert.NotEqual(enEspañol, enIngles);
        });
    }

    [Fact]
    public async Task ElErrorDeCargaConDatosDentro_TambienSeTraduce()
    {
        // El caso con argumentos reales (el mensaje del sistema va incrustado en la plantilla):
        // lo que tiene que cambiar es la PLANTILLA, no el dato.
        string ruta = Path.Combine(Path.GetTempPath(), "terrakeep-mundo-que-no-existe.wld");
        await ConIdiomaAsync(LocalizationService.Spanish, async () =>
        {
            var vm = new MainViewModel().Exploration;
            await vm.LoadFromPathAsync(ruta);
            string enEspañol = vm.StatusMessage;
            Assert.StartsWith(LocalizationService.Instance["error_reading_world"].Split('{')[0], enEspañol);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            string enIngles = vm.StatusMessage;

            Assert.StartsWith(LocalizationService.Instance["error_reading_world"].Split('{')[0], enIngles);
            Assert.NotEqual(enEspañol, enIngles);
        });
    }

    [Fact]
    public void CambiarDeIdioma_AvisaDeVerdadALaVista()
    {
        // Sin este PropertyChanged, el texto podria estar bien calculado y aun asi no reescribirse
        // en pantalla - que es exactamente como se ve el bug desde fuera.
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = new MainViewModel().Exploration;
            var avisadas = new System.Collections.Generic.List<string>();
            vm.PropertyChanged += (_, e) => { if (e.PropertyName != null) avisadas.Add(e.PropertyName); };

            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            Assert.Contains(nameof(vm.StatusMessage), avisadas);
            Assert.Contains(nameof(vm.ScanMessage), avisadas);
            Assert.Contains(nameof(vm.WorldGameModeText), avisadas);
            Assert.Contains(nameof(vm.WorldGameModeSaveStatus), avisadas);
        });
    }

    private static async Task ConIdiomaAsync(string idioma, System.Func<Task> cuerpo)
    {
        string antes = LocalizationService.Instance.Language;
        try { LocalizationService.Instance.SetLanguage(idioma); await cuerpo(); }
        finally { LocalizationService.Instance.SetLanguage(antes); }
    }
}
