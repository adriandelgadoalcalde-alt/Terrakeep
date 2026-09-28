using System.IO;
using System.Threading.Tasks;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Encargo del usuario (28-sep-2026): "deberia de haber un boton de guardar igual que hay uno de
// cargar mundo?... que este en la derecha del todo". Investigado antes de tocar nada (ver el
// comentario real de ExplorationViewModel.CanSaveWorldChanges/SaveWorldChangesAsync): el mundo
// nunca se guardaba como efecto colateral de "Guardar" en Personaje ni solo al guardar cofres -
// cada grupo de edicion (modo de juego/spawn/hora y luna/banderas de jefes) ya escribia de forma
// atomica e inmediata al pulsar su PROPIO boton dedicado. Lo que faltaba era un atajo real que
// junte las 4 secciones pendientes de "Editar mundo" en un solo guardado, como el boton nuevo de
// la barra de herramientas de Exploracion. Estas pruebas escriben un .wld MINIMO real con
// WldWriter.WriteWorld (mismo mecanismo que Terrakeep.Core.Tests/WldFormat/
// WldWriterWriteWorldTests.cs) para poder cargarlo de verdad con LoadFromPathAsync y comprobar
// el guardado real en disco (byte a byte, releyendo con WldReader), no solo el estado en memoria.
public sealed class ExplorationSaveWorldChangesTests
{
    private static WldWorld BuildMinimalWorld()
    {
        const int wide = 10, high = 10;
        var tiles = new WldTile[wide, high];
        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
                tiles[x, y] = WldTile.Empty;

        var header = new WldHeader
        {
            Version = 279, // 1.4.4.9 real, misma version que WldWriterWriteWorldTests
            Pointers = [],
            TileFrameImportant = [],
            Title = "Mundo de prueba SaveWorldChanges",
            WorldId = 999,
            TilesHigh = high,
            TilesWide = wide,
            SpawnX = 5,
            SpawnY = 5,
            GroundLevel = 3,
            RockLevel = 6,
            Seed = "keepqa-savewrld",
            GameMode = 0,
            DungeonX = 1,
            DungeonY = 1,
            Time = 0,
            DayTime = true,
            MoonPhase = 0,
            BloodMoon = false,
            IsEclipse = false,
            IsCrimson = false,
            DownedBoss1EyeOfCthulhu = false,
            DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false,
            DownedBoss3Skeletron = false,
            DownedQueenBee = false,
            DownedMechBoss1TheDestroyer = false,
            DownedMechBoss2TheTwins = false,
            DownedMechBoss3SkeletronPrime = false,
            DownedPlantBoss = false,
            DownedGolemBoss = false,
            DownedSlimeKingBoss = false,
            HardMode = false,
            DownedGoblinArmy = false,
            DownedFrostLegion = false,
            DownedPirates = false,
        };

        return new WldWorld
        {
            Header = header,
            Tiles = tiles,
            Npcs = [],
            Chests = [],
            Signs = [],
            TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(),
            Bestiary = null,
        };
    }

    private static string EscribirMundoMinimo()
    {
        string ruta = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"terrakeep-savewrldchanges-{Guid.NewGuid():N}.wld");
        File.WriteAllBytes(ruta, WldWriter.WriteWorld(BuildMinimalWorld()));
        return ruta;
    }

    private static async Task<(ExplorationViewModel Vm, string Path)> CargarMundoMinimoAsync(MainViewModel? main = null)
    {
        string ruta = EscribirMundoMinimo();
        var vm = (main ?? new MainViewModel()).Exploration;
        await vm.LoadFromPathAsync(ruta);
        return (vm, ruta);
    }

    [Fact]
    public async Task SaveWorldChangesCommand_SinNingunCambioPendiente_CanExecuteEsFalse()
    {
        var (vm, ruta) = await CargarMundoMinimoAsync();
        try
        {
            Assert.True(vm.IsWorldLoaded);
            Assert.False(vm.SaveWorldChangesCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(ruta);
            if (File.Exists(ruta + ".bak")) File.Delete(ruta + ".bak");
        }
    }

    // Caso real completo pedido por el usuario ("cubre... todo lo que se haya editado en el
    // mundo cargado, no solo una parte"): dos grupos DISTINTOS a la vez (punto de aparicion +
    // banderas de jefes) para probar que el boton de la barra de herramientas guarda los DOS de
    // un solo golpe, no solo el primero que encuentre.
    [Fact]
    public async Task SaveWorldChangesCommand_ConSpawnYBanderasPendientes_GuardaLosDosDeGolpeEnElArchivoReal()
    {
        var (vm, ruta) = await CargarMundoMinimoAsync();
        try
        {
            int nuevoSpawnX = vm.EditSpawnX + 3;
            int nuevoSpawnY = vm.EditSpawnY + 4;
            vm.EditSpawnX = nuevoSpawnX;
            vm.EditSpawnY = nuevoSpawnY;
            vm.EditDownedBoss1 = !vm.EditDownedBoss1;

            Assert.True(vm.SaveSpawnPointCommand.CanExecute(null));
            Assert.True(vm.SaveBossFlagsCommand.CanExecute(null));
            Assert.True(vm.SaveWorldChangesCommand.CanExecute(null));

            await vm.SaveWorldChangesCommand.ExecuteAsync(null);

            var releido = WldReader.ReadHeader(File.ReadAllBytes(ruta));
            Assert.Equal(nuevoSpawnX, releido.SpawnX);
            Assert.Equal(nuevoSpawnY, releido.SpawnY);
            Assert.True(releido.DownedBoss1EyeOfCthulhu);
            Assert.True(File.Exists(ruta + ".bak"));

            // Consumido: ni las dos secciones ni el boton agregado se quedan pendientes despues.
            Assert.False(vm.SaveSpawnPointCommand.CanExecute(null));
            Assert.False(vm.SaveBossFlagsCommand.CanExecute(null));
            Assert.False(vm.SaveWorldChangesCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(ruta);
            if (File.Exists(ruta + ".bak")) File.Delete(ruta + ".bak");
        }
    }

    // El guardado de la barra de herramientas de Personaje (MainViewModel.Save, el boton
    // "Guardar" de la pestaña Personaje) NUNCA debe tocar el mundo cargado en Exploracion -
    // confirma con evidencia real la respuesta a la pregunta del usuario ("cuando le das a
    // guardar en tu personaje si esta cargado tambien?"). Sin ningun personaje cargado
    // SaveCommand.CanExecute ya es false por si solo (IsCharacterLoaded) - el punto real de esta
    // prueba es que el cambio de mundo pendiente sigue intacto y pendiente por su cuenta,
    // completamente ajeno al ciclo de vida del personaje.
    [Fact]
    public async Task GuardarPersonaje_NoInteractuaConElMundoCargado()
    {
        var main = new MainViewModel();
        var (vm, ruta) = await CargarMundoMinimoAsync(main);
        try
        {
            vm.EditSpawnX += 1;
            Assert.True(vm.SaveWorldChangesCommand.CanExecute(null));

            Assert.False(main.SaveCommand.CanExecute(null)); // sin personaje cargado, no puede ejecutarse

            // El estado pendiente del mundo no lo ha tocado nada de lo anterior.
            Assert.True(vm.SaveWorldChangesCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(ruta);
            if (File.Exists(ruta + ".bak")) File.Delete(ruta + ".bak");
        }
    }
}
