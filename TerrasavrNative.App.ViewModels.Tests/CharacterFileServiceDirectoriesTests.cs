using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels.Tests;

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
}
