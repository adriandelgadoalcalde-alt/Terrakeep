using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Ola 2 - V-a y X-b.
public sealed class OlaDosTests
{
    [Fact]
    public void CambiarLaVersion_MarcaIsCurrentEnElBotonReal_YEscribeAlPersonaje()
    {
        var editor = new VersionEditorViewModel();
        var character = new PlrCharacter { Name = "Test", Version = 269, PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true) };
        editor.LoadFrom(character);

        var opciones = editor.Groups.SelectMany(g => g.Options).ToList();
        var actual = opciones.Single(o => o.Number == 269);
        Assert.True(actual.IsCurrent);
        Assert.All(opciones.Where(o => o.Number != 269), o => Assert.False(o.IsCurrent));

        editor.SetVersionCommand.Execute(225);

        Assert.False(actual.IsCurrent); // ya no es la actual
        Assert.True(opciones.Single(o => o.Number == 225).IsCurrent); // la nueva si lo es
        Assert.Equal(225, character.Version); // y de paso escribe de verdad al personaje
    }

    [Fact]
    public void ZoomInYZoomOut_UsanElMismoPasoRealQueLaRuedaDelRaton()
    {
        var exploration = new ExplorationViewModel(new CharacterFileService());
        exploration.Zoom = 1.0;

        exploration.ZoomInCommand.Execute(null);
        Assert.Equal(ExplorationViewModel.ZoomStep, exploration.Zoom, precision: 10);

        exploration.ZoomOutCommand.Execute(null);
        Assert.Equal(1.0, exploration.Zoom, precision: 10);
    }
}
