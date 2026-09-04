using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// H5-07 (quinta auditoria de Opus): "no existe ninguna pantalla de Ajustes (búsqueda
// 'Ajustes/Settings/Opciones': cero resultados)". Carpetas adicionales reales de personajes/
// mundos (CharacterFileService.GetAllPlayersDirectories/GetAllWorldsDirectories las concatena a
// las 2 detectadas) y el N configurable de copias de seguridad que H5-04 dejo aparcado a
// proposito. Sitio real: dentro de "Acerca de" (la pestaña con menos densidad, pedido explicito
// del informe) - ver AjustesTemplate en MainWindow.xaml.
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly BackupHistoryService _backupHistory;
    private TerrakeepSettings _settings = new();

    public ObservableCollection<string> ExtraCharacterFolders { get; } = [];
    public ObservableCollection<string> ExtraWorldFolders { get; } = [];

    [ObservableProperty] private int _backupHistoryCap = 20;

    // Arranca en modo "solo memoria" - Persist() (mas abajo) no toca disco hasta que
    // LoadFromDisk() lo activa explicitamente. Un test que construye "new MainViewModel()"
    // (decenas de ellos en este proyecto) puede llamar Add/RemoveCharacterFolder/etc para
    // probar la logica real del ViewModel sin que eso escriba un fichero real de settings.json
    // en el disco de ESTE equipo, sobrescribiendo preferencias reales del usuario (bug real
    // encontrado y arreglado en esta misma pasada: un `dotnet test` normal escribio un
    // session.json sintetico en %LOCALAPPDATA%\Terrakeep\ real antes de este arreglo - ver el
    // comentario real de MainViewModel.CharacterLoaded, mismo motivo exacto). LoadFromDisk() lo
    // desactiva, llamado UNA vez, real, desde MainWindow.xaml.cs (la View), igual que
    // WindowPlacementService.Apply().
    private bool _suppressPersist = true;

    public SettingsViewModel(BackupHistoryService backupHistory)
    {
        _backupHistory = backupHistory;
    }

    public void LoadFromDisk()
    {
        _settings = SettingsService.Load();
        ExtraCharacterFolders.Clear();
        foreach (string folder in _settings.ExtraCharacterFolders) ExtraCharacterFolders.Add(folder);
        ExtraWorldFolders.Clear();
        foreach (string folder in _settings.ExtraWorldFolders) ExtraWorldFolders.Add(folder);
        BackupHistoryCap = _settings.BackupHistoryCap; // _suppressPersist sigue en true aqui - no reescribe el fichero que se acaba de leer de el
        ApplyToServices();
        _suppressPersist = false; // a partir de aqui, cualquier cambio real del usuario SI se persiste
    }

    // CharacterFileService.ExtraPlayerFolders/ExtraWorldFolders son estaticos (ver su propio
    // comentario real - varios ViewModels ya los llaman sin ninguna instancia a mano) - esta es
    // la UNICA superficie real que los escribe, siempre en sincronia con lo que el usuario ve
    // aqui en Ajustes.
    private void ApplyToServices()
    {
        CharacterFileService.ExtraPlayerFolders = ExtraCharacterFolders.ToList();
        CharacterFileService.ExtraWorldFolders = ExtraWorldFolders.ToList();
        _backupHistory.MaxBackupsPerCharacter = BackupHistoryCap;
    }

    private void Persist()
    {
        if (_suppressPersist) return;
        _settings.ExtraCharacterFolders = ExtraCharacterFolders.ToList();
        _settings.ExtraWorldFolders = ExtraWorldFolders.ToList();
        _settings.BackupHistoryCap = BackupHistoryCap;
        SettingsService.Save(_settings);
        ApplyToServices();
    }

    // El dialogo real de "elegir carpeta" vive en la View (MainWindow.xaml.cs, mismo criterio
    // ya establecido en H5-03 con SaveItemSet/LoadItemSet - MainViewModel/esta clase se quedan
    // headless de verdad) - aqui solo la logica real sobre una ruta ya elegida.
    public void AddCharacterFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || ExtraCharacterFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
        ExtraCharacterFolders.Add(folder);
        Persist();
    }

    public void AddWorldFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || ExtraWorldFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
        ExtraWorldFolders.Add(folder);
        Persist();
    }

    [RelayCommand]
    private void RemoveCharacterFolder(string folder)
    {
        ExtraCharacterFolders.Remove(folder);
        Persist();
    }

    [RelayCommand]
    private void RemoveWorldFolder(string folder)
    {
        ExtraWorldFolders.Remove(folder);
        Persist();
    }

    // Mismo criterio real ya usado en varios sitios del proyecto (ej. HealthNow/HealthMax en
    // AppearanceViewModel) - un valor absurdo a mano (0, negativo) se recorta en vez de dejar
    // que Purge() borre TODO en el siguiente guardado real.
    partial void OnBackupHistoryCapChanged(int value)
    {
        if (value < 1) { BackupHistoryCap = 1; return; } // reentra, se estabiliza al segundo paso
        Persist();
    }
}
