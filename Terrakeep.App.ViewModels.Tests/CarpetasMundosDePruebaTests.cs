using System.IO;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// FASE C del responsive global (28-sep-2026): gemelo para MUNDOS de CarpetasPersonajesDePruebaTests.
// El arnes SUSTITUYE las carpetas reales de mundos por copias (CarpetasMundosDePrueba) y vigila cada
// apertura/escritura real de un .wld (RaicesMundosPermitidasDePrueba + ComprobarMundoDePrueba). Fija:
// fuera del modo diagnostico no se activa; activado no queda ninguna carpeta real a la vista (las
// extra de Ajustes solo si son temporales del arnes); y una ruta fuera de las raices avisa al
// vigilante y lanza ANTES de que la operacion siga (negative acceptance del aislamiento).
[Collection("EstadoEstaticoCarpetasPersonajes")]
public sealed class CarpetasMundosDePruebaTests
{
    [Fact]
    public void FueraDelModoDiagnostico_NoSePuedeActivar()
    {
        bool previo = Terrakeep.App.App.ModoDiagnostico;
        try
        {
            Terrakeep.App.App.ModoDiagnostico = false;
            Assert.Throws<InvalidOperationException>(() => CharacterFileService.CarpetasMundosDePrueba = [Path.GetTempPath()]);
            Assert.Throws<InvalidOperationException>(() => CharacterFileService.RaicesMundosPermitidasDePrueba = [Path.GetTempPath()]);
            Assert.Throws<InvalidOperationException>(() => CharacterFileService.AlTocarMundoFueraDePrueba = _ => { });
            Assert.Null(CharacterFileService.CarpetasMundosDePrueba);
            Assert.Null(CharacterFileService.RaicesMundosPermitidasDePrueba);
            // Sin raices puestas (la app real) la guarda no hace nada con ninguna ruta.
            CharacterFileService.ComprobarMundoDePrueba(@"C:\cualquier\sitio\mundo.wld");
        }
        finally { Terrakeep.App.App.ModoDiagnostico = previo; }
    }

    [Fact]
    public void Activado_SustituyeLasCarpetasRealesYSoloAdmiteExtrasTemporales()
    {
        bool previo = Terrakeep.App.App.ModoDiagnostico;
        var extraPrevias = CharacterFileService.ExtraWorldFolders;
        string temp = Path.Combine(Path.GetTempPath(), $"mundos-prueba-{Guid.NewGuid():N}");
        string extraTemporal = Path.Combine(Path.GetTempPath(), $"mundos-extra-{Guid.NewGuid():N}");
        string extraReal = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // existe y NO es temporal
        Directory.CreateDirectory(temp);
        Directory.CreateDirectory(extraTemporal);
        try
        {
            Terrakeep.App.App.ModoDiagnostico = true;
            CharacterFileService.ExtraWorldFolders = [extraReal, extraTemporal];
            CharacterFileService.CarpetasMundosDePrueba = [temp];
            CharacterFileService.RaicesMundosPermitidasDePrueba = [Path.GetTempPath()];

            Assert.Equal([temp, extraTemporal], CharacterFileService.GetAllWorldsDirectories());
            Assert.Equal(temp, CharacterFileService.GetDefaultWorldsDirectory());
        }
        finally
        {
            CharacterFileService.CarpetasMundosDePrueba = null;
            CharacterFileService.RaicesMundosPermitidasDePrueba = null;
            CharacterFileService.ExtraWorldFolders = extraPrevias;
            Terrakeep.App.App.ModoDiagnostico = previo;
            Directory.Delete(temp, recursive: true);
            Directory.Delete(extraTemporal, recursive: true);
        }
    }

    [Fact]
    public void Activado_UnaRutaFueraDeLasRaicesAvisaAlVigilanteYLanza()
    {
        bool previo = Terrakeep.App.App.ModoDiagnostico;
        string? avisada = null;
        string real = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "tModLoader", "Worlds", "roca_negra.wld");
        string copia = Path.Combine(Path.GetTempPath(), "TerrakeepArnes-x", "tModLoader", "Worlds", "roca_negra.wld");
        try
        {
            Terrakeep.App.App.ModoDiagnostico = true;
            CharacterFileService.RaicesMundosPermitidasDePrueba = [Path.GetTempPath()];
            CharacterFileService.AlTocarMundoFueraDePrueba = r => avisada = r;

            CharacterFileService.ComprobarMundoDePrueba(copia); // dentro: no avisa ni lanza
            Assert.Null(avisada);
            Assert.Throws<InvalidOperationException>(() => CharacterFileService.ComprobarMundoDePrueba(real));
            Assert.Equal(real, avisada);
        }
        finally
        {
            CharacterFileService.RaicesMundosPermitidasDePrueba = null;
            CharacterFileService.AlTocarMundoFueraDePrueba = null;
            Terrakeep.App.App.ModoDiagnostico = previo;
        }
    }
}
