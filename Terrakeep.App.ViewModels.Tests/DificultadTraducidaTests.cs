using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Oleada del 6-sep-2026 (Personaje > Apariencia) - BUG REAL: los 4 nombres de dificultad iban a
// pelo en un string[] del ViewModel ("Softcore", "Mediumcore", "Hardcore", "Journey"), o sea en
// INGLES tambien con la app en español, en los DOS sitios donde se ven (el selector de
// Apariencia y la insignia de la cabecera global, visible desde cualquier pestaña), mas las
// tarjetas de Inicio. Y ademas el primero era el nombre INTERNO del codigo, no el que el juego
// enseña: Terraria dice "Classic"/"Clásico", nunca "Softcore".
//
// Traduccion real del propio juego, no inventada:
//   Terraria.Localization.Content.es-ES.Legacy.json, seccion LegacyMenu:
//     "26"="Clásico" (en-US "Classic"), "25"="Núcleo medio" (Mediumcore), "24"="Extremo" (Hardcore)
//   Terraria.Localization.Content.es-ES.json, clave "Creative" = "Viaje" (en-US "Journey")
public sealed class DificultadTraducidaTests
{
    [Theory]
    [InlineData(0, "Clásico")]
    [InlineData(1, "Núcleo medio")]
    [InlineData(2, "Extremo")]
    [InlineData(3, "Viaje")]
    public void EnEspañol_UsaElNombreRealDelJuego(int difficulty, string esperado)
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            LocalizationService.Instance.SetLanguage(LocalizationService.Spanish);
            Assert.Equal(esperado, AppearanceViewModel.DifficultyLabelFor(difficulty));
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }

    [Theory]
    [InlineData(0, "Classic")]
    [InlineData(1, "Mediumcore")]
    [InlineData(2, "Hardcore")]
    [InlineData(3, "Journey")]
    public void EnIngles_UsaElNombreRealDelJuego_NoElInternoSoftcore(int difficulty, string esperado)
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            Assert.Equal(esperado, AppearanceViewModel.DifficultyLabelFor(difficulty));
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }

    // Un valor imposible (fichero corrupto, o una version futura con mas dificultades) no puede
    // reventar la cabecera: mismo Clamp real que ya tenia el indexado del array.
    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void UnaDificultadFueraDeRango_NoRevienta(int difficulty)
    {
        Assert.False(string.IsNullOrWhiteSpace(AppearanceViewModel.DifficultyLabelFor(difficulty)));
    }

    // La insignia de dificultad de la cabecera se ve desde CUALQUIER pestaña: si no se entera
    // del cambio de idioma se queda con el texto anterior hasta el siguiente cambio real de
    // dificultad, que puede no llegar nunca.
    [Fact]
    public void LaEtiquetaDeLaCabecera_CambiaSolaAlCambiarDeIdioma()
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            var vm = new MainViewModel();
            LocalizationService.Instance.SetLanguage(LocalizationService.Spanish);
            vm.Appearance.Difficulty = 3;
            Assert.Equal("Viaje", vm.Appearance.DifficultyLabel);

            bool aviso = false;
            vm.Appearance.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(AppearanceViewModel.DifficultyLabel)) aviso = true; };
            LocalizationService.Instance.SetLanguage(LocalizationService.English);

            Assert.True(aviso);
            Assert.Equal("Journey", vm.Appearance.DifficultyLabel);
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }
}
