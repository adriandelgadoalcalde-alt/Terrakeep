using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// H5-07 (quinta auditoria de Opus): "no existe ninguna pantalla de Ajustes". SettingsViewModel
// arranca en modo "solo memoria" (LoadFromDisk() nunca corre aqui, la llama solo
// MainWindow.xaml.cs - ver su propio comentario real) - estos tests verifican la logica real
// del ViewModel (Add/Remove/deduplicacion/recorte del cupo) sin tocar NUNCA el settings.json
// real de este equipo.
public sealed class SettingsViewModelTests
{
    private static SettingsViewModel NewSettings() => new(new BackupHistoryService());

    [Fact]
    public void AddCharacterFolder_LaAñadeALaLista()
    {
        var settings = NewSettings();

        settings.AddCharacterFolder(@"D:\Terraria\Players");

        Assert.Contains(@"D:\Terraria\Players", settings.ExtraCharacterFolders);
    }

    [Fact]
    public void AddCharacterFolder_RepetidaNoSeDuplica()
    {
        var settings = NewSettings();
        settings.AddCharacterFolder(@"D:\Terraria\Players");

        settings.AddCharacterFolder(@"D:\Terraria\Players");
        settings.AddCharacterFolder(@"d:\terraria\players"); // mismo path, distinta capitalizacion real de Windows

        Assert.Single(settings.ExtraCharacterFolders);
    }

    [Fact]
    public void AddCharacterFolder_VacioOEnBlanco_NoHaceNada()
    {
        var settings = NewSettings();

        settings.AddCharacterFolder("");
        settings.AddCharacterFolder("   ");

        Assert.Empty(settings.ExtraCharacterFolders);
    }

    [Fact]
    public void RemoveCharacterFolder_LaQuita()
    {
        var settings = NewSettings();
        settings.AddCharacterFolder(@"D:\Terraria\Players");

        settings.RemoveCharacterFolderCommand.Execute(@"D:\Terraria\Players");

        Assert.Empty(settings.ExtraCharacterFolders);
    }

    [Fact]
    public void AddWorldFolder_LaAñadeALaListaDeMundos_SinTocarLaDePersonajes()
    {
        var settings = NewSettings();

        settings.AddWorldFolder(@"D:\Terraria\Worlds");

        Assert.Contains(@"D:\Terraria\Worlds", settings.ExtraWorldFolders);
        Assert.Empty(settings.ExtraCharacterFolders);
    }

    [Fact]
    public void RemoveWorldFolder_LaQuita()
    {
        var settings = NewSettings();
        settings.AddWorldFolder(@"D:\Terraria\Worlds");

        settings.RemoveWorldFolderCommand.Execute(@"D:\Terraria\Worlds");

        Assert.Empty(settings.ExtraWorldFolders);
    }

    // H5-07: "N configurable de verdad" - mismo criterio real ya usado en HealthNow/HealthMax
    // (AppearanceViewModel) para un valor absurdo a mano.
    [Fact]
    public void BackupHistoryCap_ValorDeFabricaEs20()
    {
        var settings = NewSettings();

        Assert.Equal(20, settings.BackupHistoryCap);
    }

    [Fact]
    public void BackupHistoryCap_UnValorMenorQue1_SeRecortaA1()
    {
        var settings = NewSettings();

        settings.BackupHistoryCap = 0;

        Assert.Equal(1, settings.BackupHistoryCap);
    }

    [Fact]
    public void BackupHistoryCap_UnValorNegativo_SeRecortaA1()
    {
        var settings = NewSettings();

        settings.BackupHistoryCap = -5;

        Assert.Equal(1, settings.BackupHistoryCap);
    }

    [Fact]
    public void BackupHistoryCap_UnValorRealValido_SeConserva()
    {
        var settings = NewSettings();

        settings.BackupHistoryCap = 50;

        Assert.Equal(50, settings.BackupHistoryCap);
    }
}
