using System.IO;
using System.Linq;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// Pedido explicito del usuario (2-sep-2026): los lanzadores de Inicio/Exploracion tambien deben
// encontrar personajes/mundos de Terraria VANILLA (sin ningun mod), no solo los de tModLoader.
// GetAllPlayersDirectories/GetAllWorldsDirectories son la fuente real que usan HomeViewModel.
// RefreshAsync/ExplorationViewModel.RefreshWorldsAsync.
public sealed class CharacterFileServiceDirectoriesTests
{
    [Fact]
    public void GetAllPlayersDirectories_NuncaDevuelveCarpetasDuplicadas()
    {
        var dirs = CharacterFileService.GetAllPlayersDirectories();

        Assert.Equal(dirs.Distinct().Count(), dirs.Count);
    }

    [Fact]
    public void GetAllWorldsDirectories_NuncaDevuelveCarpetasDuplicadas()
    {
        var dirs = CharacterFileService.GetAllWorldsDirectories();

        Assert.Equal(dirs.Distinct().Count(), dirs.Count);
    }

    [Fact]
    public void GetAllPlayersDirectories_SoloDevuelveCarpetasQueExistenDeVerdad()
    {
        var dirs = CharacterFileService.GetAllPlayersDirectories();

        Assert.All(dirs, dir => Assert.True(Directory.Exists(dir)));
    }

    [Fact]
    public void GetAllWorldsDirectories_SoloDevuelveCarpetasQueExistenDeVerdad()
    {
        var dirs = CharacterFileService.GetAllWorldsDirectories();

        Assert.All(dirs, dir => Assert.True(Directory.Exists(dir)));
    }

    // H5-07 (quinta auditoria de Opus): "quien tenga Terraria en otro disco/Documentos
    // redirigidos/instalacion portable ve el lanzador vacio sin forma de arreglarlo desde la
    // app" - ExtraPlayerFolders/ExtraWorldFolders (estaticos, ver su propio comentario real en
    // CharacterFileService.cs) se concatenan a las 2 detectadas. try/finally real: son campos
    // ESTATICOS compartidos por todo el proceso de pruebas - dejarlos sucios contaminaria
    // cualquier otro test de esta misma clase que corra despues.
    [Fact]
    public void GetAllPlayersDirectories_ConcatenaLasCarpetasAdicionalesReales()
    {
        string extra = Path.Combine(Path.GetTempPath(), $"h507-extra-players-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extra);
        try
        {
            CharacterFileService.ExtraPlayerFolders = [extra];

            var dirs = CharacterFileService.GetAllPlayersDirectories();

            Assert.Contains(extra, dirs);
        }
        finally
        {
            CharacterFileService.ExtraPlayerFolders = [];
            Directory.Delete(extra, recursive: true);
        }
    }

    [Fact]
    public void GetAllPlayersDirectories_UnaCarpetaAdicionalQueNoExisteSeOmiteEnSilencio()
    {
        string inexistente = Path.Combine(Path.GetTempPath(), $"h507-no-existe-{Guid.NewGuid():N}");
        try
        {
            CharacterFileService.ExtraPlayerFolders = [inexistente];

            var dirs = CharacterFileService.GetAllPlayersDirectories();

            Assert.DoesNotContain(inexistente, dirs);
        }
        finally
        {
            CharacterFileService.ExtraPlayerFolders = [];
        }
    }

    [Fact]
    public void GetAllWorldsDirectories_UnaCarpetaAdicionalQueYaCoincideConUnaDetectada_NoSeDuplica()
    {
        // La propia carpeta ya detectada (Documents\...), añadida TAMBIEN como "adicional" a
        // mano (ej. el usuario la eligio sin darse cuenta de que ya contaba) - no debe aparecer
        // dos veces.
        var detectadas = CharacterFileService.GetAllWorldsDirectories();
        if (detectadas.Count == 0) return; // esta maquina no tiene ninguna carpeta real detectada - nada que probar aqui
        try
        {
            CharacterFileService.ExtraWorldFolders = [detectadas[0]];

            var dirs = CharacterFileService.GetAllWorldsDirectories();

            Assert.Equal(dirs.Distinct(StringComparer.OrdinalIgnoreCase).Count(), dirs.Count);
        }
        finally
        {
            CharacterFileService.ExtraWorldFolders = [];
        }
    }
}
