using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-07 (quinta auditoria de Opus): "session.json recuerda el ULTIMO personaje real - Inicio
// ofrece 'Continuar con Nombre' como accion destacada, con aviso de fichero cambiado por fuera
// si la fecha no cuadra, nunca carga automatica silenciosa". HomeViewModel.SetLastSession toma
// un TerrakeepSession ya construido a mano (nunca SessionService.Load() de verdad) - estos
// tests no tocan el session.json real de este equipo.
public sealed class SessionRestoreTests
{
    private static readonly CharacterFileService Service = new();

    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    [Fact]
    public void SetLastSession_ConUnPersonajeRealSinCambios_NoMuestraAviso()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);
        string path = Path.Combine(Path.GetTempPath(), $"session-restore-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Eldelgas")));
        var modificado = File.GetLastWriteTimeUtc(path);

        home.SetLastSession(new TerrakeepSession { LastCharacterPath = path, LastCharacterName = "Eldelgas", LastCharacterModifiedUtc = modificado });

        Assert.Equal("Eldelgas", home.LastSessionCharacterName);
        Assert.Null(home.LastSessionStalenessWarning);
        File.Delete(path);
    }

    [Fact]
    public void SetLastSession_ConElFicheroCambiadoPorFuera_MuestraElAvisoReal()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);
        string path = Path.Combine(Path.GetTempPath(), $"session-restore-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Eldelgas")));

        // La sesion guardada recuerda una fecha DISTINTA a la real de ahora mismo - el
        // "cambiado por fuera" real que el informe pide detectar.
        home.SetLastSession(new TerrakeepSession
        {
            LastCharacterPath = path,
            LastCharacterName = "Eldelgas",
            LastCharacterModifiedUtc = File.GetLastWriteTimeUtc(path).AddHours(-1),
        });

        Assert.NotNull(home.LastSessionStalenessWarning);
        File.Delete(path);
    }

    [Fact]
    public void SetLastSession_SinFicheroReal_NoOfreceContinuar()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);

        home.SetLastSession(new TerrakeepSession { LastCharacterPath = @"C:\ya-no-existe-de-verdad.plr", LastCharacterName = "Fantasma" });

        Assert.Null(home.LastSessionCharacterName);
    }

    [Fact]
    public void SetLastSession_SinNingunaSesionAnterior_NoOfreceContinuar()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);

        home.SetLastSession(new TerrakeepSession()); // LastCharacterPath null - primer arranque real

        Assert.Null(home.LastSessionCharacterName);
    }

    [Fact]
    public void ContinueCommand_DisparaCharacterChosenConLaRutaReal()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);
        string path = Path.Combine(Path.GetTempPath(), $"session-restore-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Eldelgas")));
        home.SetLastSession(new TerrakeepSession { LastCharacterPath = path, LastCharacterName = "Eldelgas", LastCharacterModifiedUtc = File.GetLastWriteTimeUtc(path) });
        string? rutaElegida = null;
        home.CharacterChosen += p => rutaElegida = p;

        home.ContinueCommand.Execute(null);

        Assert.Equal(path, rutaElegida);
        File.Delete(path);
    }

    // H5-07: "nunca carga automatica silenciosa" - sin sesion real, el comando no debe disparar
    // CharacterChosen con basura.
    [Fact]
    public void ContinueCommand_SinSesionReal_NoDisparaNada()
    {
        var home = new HomeViewModel(Service.EquipmentAppearance, Service.BackupHistory);
        bool disparado = false;
        home.CharacterChosen += _ => disparado = true;

        home.ContinueCommand.Execute(null);

        Assert.False(disparado);
    }
}
