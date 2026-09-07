using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H3-14 (tercera auditoria de Opus, Fable): "279 se muestra crudo, nunca '1.4.4.0+'" - calco
// real de TabVersion.findBestMatch/getBestText (script.beautified.js:5161-5173): la version
// conocida mas alta <= la real, con un "+" si la real es estrictamente mayor.
public sealed class VersionBestMatchTests
{
    [Fact]
    public void UnaVersionEntreDosConocidas_MuestraLaMasBajaConMasSigno()
    {
        var editor = new VersionEditorViewModel();
        var character = new PlrCharacter { Name = "Test", Version = 279, PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true) };

        editor.LoadFrom(character);

        Assert.Equal("1.4.4.0+", editor.BestMatchLabel);
        var opciones = editor.Groups.SelectMany(g => g.Options).ToList();
        Assert.True(opciones.Single(o => o.Number == 269).IsCurrent); // 1.4.4.0 resaltado, no ninguno
        Assert.All(opciones.Where(o => o.Number != 269), o => Assert.False(o.IsCurrent));
    }

    [Fact]
    public void UnaVersionExacta_MuestraElNumeroSinMasSigno()
    {
        var editor = new VersionEditorViewModel();
        var character = new PlrCharacter { Name = "Test", Version = 269, PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true) };

        editor.LoadFrom(character);

        Assert.Equal("1.4.4.0", editor.BestMatchLabel);
    }

    [Fact]
    public void LaVersionMasAltaConocidaMasUno_TambienLlevaMasSigno()
    {
        var editor = new VersionEditorViewModel();
        var character = new PlrCharacter { Name = "Test", Version = 400, PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true) };

        editor.LoadFrom(character);

        Assert.Equal("1.4.5.0 / 1.4.5.x+", editor.BestMatchLabel);
    }

    [Fact]
    public void UnaVersionMasAntiguaQueLaMasBajaConocida_CaeALaMasBaja()
    {
        var editor = new VersionEditorViewModel();
        var character = new PlrCharacter { Name = "Test", Version = 10, PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true) };

        editor.LoadFrom(character);

        Assert.Equal("1.1.2", editor.BestMatchLabel); // igual, no menos - findBestMatch real cae a la primera
    }
}
