using System.IO;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// H-04 (segunda revision visual de la FASE B, 28-sep-2026): el arnes de pruebas SUSTITUYE las
// carpetas reales de personajes por copias temporales (CharacterFileService.
// CarpetasPersonajesDePrueba). Estos tests fijan las dos garantias de ese mecanismo: fuera del
// modo diagnostico no se puede activar, y activado no queda NINGUNA carpeta real a la vista.
[Collection("EstadoEstaticoCarpetasPersonajes")]
public sealed class CarpetasPersonajesDePruebaTests
{
    [Fact]
    public void FueraDelModoDiagnostico_NoSePuedeActivar()
    {
        bool previo = Terrakeep.App.App.ModoDiagnostico;
        try
        {
            Terrakeep.App.App.ModoDiagnostico = false;
            Assert.Throws<InvalidOperationException>(() => CharacterFileService.CarpetasPersonajesDePrueba = [Path.GetTempPath()]);
            Assert.Null(CharacterFileService.CarpetasPersonajesDePrueba);
        }
        finally { Terrakeep.App.App.ModoDiagnostico = previo; }
    }

    [Fact]
    public void Activado_SustituyeLasCarpetasRealesIncluidasLasExtra()
    {
        bool previo = Terrakeep.App.App.ModoDiagnostico;
        var extraPrevias = CharacterFileService.ExtraPlayerFolders;
        string temp = Path.Combine(Path.GetTempPath(), $"carpetas-prueba-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            Terrakeep.App.App.ModoDiagnostico = true;
            CharacterFileService.ExtraPlayerFolders = [Path.GetTempPath()]; // una "extra" de Ajustes que NO debe sumarse
            CharacterFileService.CarpetasPersonajesDePrueba = [temp];

            Assert.Equal([temp], CharacterFileService.GetAllPlayersDirectories());
            Assert.Equal(temp, CharacterFileService.GetDefaultPlayersDirectory());
        }
        finally
        {
            CharacterFileService.CarpetasPersonajesDePrueba = null;
            CharacterFileService.ExtraPlayerFolders = extraPrevias;
            Terrakeep.App.App.ModoDiagnostico = previo;
            Directory.Delete(temp, recursive: true);
        }
    }
}
