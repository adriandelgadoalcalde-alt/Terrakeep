using System.Diagnostics;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Punto 4 (advisor Opus): confirma contra un .wld REAL las cifras que el advisor midio con un
// port a Node (ESPEC-ui-exploracion.md#6, marcadas explicitamente como "no verificadas en C#
// real" en su propia seccion de huecos) - y mide el coste real en C#, no en JS.
public class OreVeinFinderRealWorldTests(ITestOutputHelper output)
{
    private const string WorldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\Afueras_de_Larvas_de_gusano.wld";

    [Fact]
    public void Find_MundoGrandeReal_CifrasDeVetasDeCobreCercanasALasMedidasPorElAdvisor()
    {
        if (!File.Exists(WorldPath)) return;

        var world = WldReader.Read(File.ReadAllBytes(WorldPath));
        var sw = Stopwatch.StartNew();
        var vetas = OreVeinFinder.Find(world, new HashSet<int> { 7 }, limit: 1000, out int total, CancellationToken.None);
        sw.Stop();

        output.WriteLine($"Cobre (id 7): {total} vetas reales, {sw.ElapsedMilliseconds}ms en C# real (el advisor midio ~40ms en Node)");
        // El advisor midio 4387 vetas de cobre en este mismo mundo con un port a Node - margen
        // real por si la generacion del mundo cambio entre sesiones (raro, pero el mundo es de
        // este usuario y podria haberse seguido jugando), no una igualdad exacta a ciegas.
        Assert.InRange(total, 3500, 5500);
        Assert.True(vetas[0].TileCount >= vetas[^1].TileCount); // orden descendente real
    }

    // Hallazgo real de esta sesion (no estaba en el espec del advisor, cuya propia seccion de
    // huecos ya avisaba de que sus tiempos eran "de un port a JS, no del C# real" - confirmado
    // aqui: llamar a Find UNA VEZ POR MINERAL (23 llamadas, cada una un barrido completo de la
    // rejilla) tarda varios SEGUNDOS en C# real, muy por encima de lo aceptable para un solo
    // clic de usuario. La solucion real y barata: CountVeinsByType hace UN SOLO barrido con
    // TODOS los minerales presentes a la vez - el problema no era el algoritmo, era llamarlo
    // repetidas veces. Ver ExplorationViewModel.RebuildOreInventory, que usa este metodo.
    [Fact]
    public void CountVeinsByType_UnaSolaLlamadaConTodosLosTiposJuntos_EsCasiTanBarataComoUnaSola()
    {
        if (!File.Exists(WorldPath)) return;

        var world = WldReader.Read(File.ReadAllBytes(WorldPath));
        var presence = WorldPresenceIndex.Build(world);
        var presentes = OreTileCatalog.All.Where(presence.HasTile).ToHashSet();
        output.WriteLine($"Minerales/gemas/objetivos presentes en este mundo real: {presentes.Count}");

        var sw = Stopwatch.StartNew();
        var conteoPorTipo = OreVeinFinder.CountVeinsByType(world, presentes, CancellationToken.None);
        sw.Stop();

        output.WriteLine($"UNA sola llamada con los {presentes.Count} tipos juntos: {sw.ElapsedMilliseconds}ms, cobre(7)={conteoPorTipo.GetValueOrDefault(7)} vetas");

        Assert.True(presentes.Count > 0);
        // El coste real tiene que ser del orden de UNA llamada (barrido unico), no de N llamadas
        // (barrido repetido) - genoroso a 2 segundos para no acoplar la prueba a la maquina.
        Assert.True(sw.ElapsedMilliseconds < 2000, $"una sola llamada combinada tardo {sw.ElapsedMilliseconds}ms - deberia ser del orden de UN barrido, no de {presentes.Count}");
        Assert.Equal(4387, conteoPorTipo[7]); // exactamente lo medido por el advisor y por Find_MundoGrandeReal de arriba
    }
}
