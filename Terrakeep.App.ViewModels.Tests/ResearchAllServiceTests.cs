using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), B-3: prueba determinista real que habria cazado el bug
// de raiz - la version anterior de ResearchAllService saltaba ENTERO cualquier objeto ya
// parcialmente investigado, en vez de subirlo al umbral real.
public sealed class ResearchAllServiceTests
{
    // Un unico CharacterFileService real (carga ~30 catalogos JSON reales de Assets/) se
    // comparte entre los tests de esta clase - xunit crea una instancia nueva de la clase de
    // test por cada [Fact], pero el catalogo en si es de solo lectura, compartirlo vale.
    private static readonly CharacterFileService Service = new();

    private static LoadedCharacter NewCharacter(params PlrResearchEntry[] research)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        character.Research.AddRange(research);
        return new LoadedCharacter("test.plr", null, "Player", character, null, new Dictionary<string, Core.Model.GameItem[]>());
    }

    [Fact]
    public void Apply_SubeUnaEntradaParcialAlUmbralReal_EnVezDeSaltarla()
    {
        // DirtBlock (id=2) es categoria "L" en el TSV real de sacrificios -> umbral real 100.
        // Un personaje que ya sacrifico 37 de 100 tiene una entrada real con Count=37.
        var loaded = NewCharacter(new PlrResearchEntry { Pid = "DirtBlock", Count = 37 });

        ResearchAllService.Apply(loaded, Service);

        var entry = loaded.Character.Research.Single(e => e.Pid == "DirtBlock");
        Assert.Equal(100, entry.Count); // antes del arreglo: se quedaba en 37, el objeto seguia bloqueado en el juego real
    }

    [Fact]
    public void Apply_NuncaBajaUnConteoQueYaSuperaElUmbralReal()
    {
        // Un personaje con MAS sacrificios reales que el umbral (comportamiento legitimo del
        // juego real, contar de mas no hace nada malo) no debe perder ese exceso.
        var loaded = NewCharacter(new PlrResearchEntry { Pid = "DirtBlock", Count = 500 });

        ResearchAllService.Apply(loaded, Service);

        var entry = loaded.Character.Research.Single(e => e.Pid == "DirtBlock");
        Assert.Equal(500, entry.Count);
    }

    [Fact]
    public void Apply_AñadeUnaEntradaNuevaConElUmbralRealCuandoNoExistiaAntes()
    {
        var loaded = NewCharacter(); // sin investigacion previa

        ResearchAllService.Apply(loaded, Service);

        var entry = loaded.Character.Research.Single(e => e.Pid == "DirtBlock");
        Assert.Equal(100, entry.Count);
        var arma = loaded.Character.Research.Single(e => e.Pid == "IronBroadsword");
        Assert.Equal(1, arma.Count); // categoria "D" real -> umbral 1
    }

    [Fact]
    public void Apply_NoRevientaConUnPidDuplicadoEnDatosAjenos()
    {
        // Un .plr real ajeno con un Pid duplicado (dato externo, no generado por esta app) no
        // debe tumbar "Investigar todo" con una excepcion.
        var loaded = NewCharacter(
            new PlrResearchEntry { Pid = "DirtBlock", Count = 10 },
            new PlrResearchEntry { Pid = "DirtBlock", Count = 20 });

        var ex = Record.Exception(() => ResearchAllService.Apply(loaded, Service));

        Assert.Null(ex);
    }
}
