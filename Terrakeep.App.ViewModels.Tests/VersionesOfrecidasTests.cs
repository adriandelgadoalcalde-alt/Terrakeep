using System.Linq;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// PB-16 (oleada de Personaje > Version, 6-sep-2026). La lista de versiones que ofrece la
// pestaña no estaba fijada por ninguna prueba: es una tabla real (la de ya.initPC del Terrasavr
// original mas la de version de juego -> invVersion confirmada contra script.js, ver
// VersionEditorViewModel) y un numero mal copiado ahi produce un .plr que ni la app ni el juego
// releen bien. Ademas el "mejor ajuste" (findBestMatch/getBestText reales) tiene reglas
// concretas que conviene dejar clavadas: la version conocida mas alta <= la real, con un "+" si
// la real es estrictamente mayor.
public sealed class VersionesOfrecidasTests
{
    private static VersionEditorViewModel Nuevo() => new();

    [Fact]
    public void OfreceLas17VersionesRealesEnSus4Grupos()
    {
        var vm = Nuevo();
        Assert.Equal(4, vm.Groups.Count);
        Assert.Equal(["1.1.x", "1.2.x", "1.3.x", "1.4.x"], vm.Groups.Select(g => g.Label));
        Assert.Equal(17, vm.Groups.Sum(g => g.Options.Count));

        // Los numeros reales, en orden y sin repetir - son los que se escriben en el .plr.
        int[] esperados = [39, 69, 73, 77, 93, 98, 145, 168, 175, 184, 190, 225, 230, 237, 248, 269, 315];
        Assert.Equal(esperados, vm.Groups.SelectMany(g => g.Options).Select(o => o.Number));
        Assert.Equal(esperados.Length, esperados.Distinct().Count());
        // Y estrictamente crecientes: findBestMatch recorre la lista de mayor a menor fiandose
        // de ese orden.
        Assert.Equal(esperados.OrderBy(n => n), esperados);
    }

    // Calco real de TabVersion.findBestMatch/getBestText (script.beautified.js:5161-5173).
    [Theory]
    [InlineData(269, "1.4.4.0")]   // exacta: sin "+"
    [InlineData(279, "1.4.4.0+")]  // "y pico": la conocida mas alta <= 279, con "+"
    [InlineData(39, "1.1.2")]
    [InlineData(1, "1.1.2")]       // mas vieja que la primera conocida: cae a la primera, como el original
    [InlineData(400, "1.4.5.0 / 1.4.5.x+")]
    public void ElMejorAjusteEsElDelOriginal(int version, string esperado)
    {
        var vm = Nuevo();
        vm.RawVersion = version;
        Assert.Equal(esperado, vm.BestMatchLabel);
    }

    // Y solo UNA opcion queda resaltada como "la actual", siempre esa misma.
    [Fact]
    public void SoloUnaOpcionQuedaMarcadaComoLaActual()
    {
        var vm = Nuevo();
        vm.RawVersion = 279;
        var marcadas = vm.Groups.SelectMany(g => g.Options).Where(o => o.IsCurrent).ToList();
        Assert.Single(marcadas);
        Assert.Equal(269, marcadas[0].Number);

        vm.RawVersion = 98;
        marcadas = vm.Groups.SelectMany(g => g.Options).Where(o => o.IsCurrent).ToList();
        Assert.Single(marcadas);
        Assert.Equal(98, marcadas[0].Number);
    }
}
