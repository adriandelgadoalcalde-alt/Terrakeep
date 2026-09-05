using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), X-g: "el mapa no sabe nada del personaje real" - antes
// Exploracion era un visor totalmente independiente del personaje cargado. Los Spawn Points
// reales del personaje (PlrServerEntry.SpawnX/Y, unica fuente real de coordenadas de aparicion
// en el .plr) ahora se empujan a Exploration.CharacterSpawns al cargar personaje.
public sealed class ExplorationCharacterSpawnsTests
{
    [Fact]
    public void CargarPersonajeConSpawnPointsRealesLosLlevaAlMapaSinLosVacios()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            Servers =
            [
                new PlrServerEntry { Name = "Base principal", SpawnX = 4200, SpawnY = 300, WorldId = 0 },
                new PlrServerEntry { Name = "Recien añadido, sin fijar todavia", SpawnX = 0, SpawnY = 0, WorldId = 0 },
            ],
        };
        string path = Path.Combine(Path.GetTempPath(), $"exploration-spawns-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();

        vm.LoadFromPath(path);
        File.Delete(path);

        Assert.Single(vm.Exploration.CharacterSpawns);
        var spawn = vm.Exploration.CharacterSpawns[0];
        // C-05 (informe de pulido final, cierra E4): la etiqueta ya NO es entry.Name (el nombre
        // del MUNDO, no una descripcion del marcador - "no lo especifica en ningun lado" era
        // literal) - ahora dice que es y de quien, con las coordenadas reales.
        Assert.Equal("Punto de aparición de Test en este mundo (4200, 300)", spawn.Label);
        Assert.Equal(4200, spawn.TileX);
        Assert.Equal(300, spawn.TileY);
    }
}
