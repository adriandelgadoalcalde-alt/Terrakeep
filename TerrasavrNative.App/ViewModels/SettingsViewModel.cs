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
    // F-10 (auditoria de Opus vs TEdit, E-10): 0 = plegada (boton "›" de MainWindow.xaml).
    [ObservableProperty] private double _explorationSidebarWidth = 320;
    // F-8 (auditoria de Opus vs TEdit, E-05): plegado del minimapa.
    [ObservableProperty] private bool _isMinimapVisible = true;
    // Pedido explicito del usuario (5-sep-2026): "guardar el tamaño de ventana actual para que
    // siempre se inicie en esa escala", con un tick que lo activa/desactiva. Vive en
    // window.json (WindowPlacementService), NO en settings.json de esta clase - el unico sitio
    // que conoce el Window real es la View (MainWindow.xaml.cs, OnPinWindowSizeChecked/
    // Unchecked), asi que esta propiedad es solo el ESPEJO de lo que hay en disco (cargado en
    // LoadFromDisk) para que el CheckBox de Ajustes lo pueda mostrar - la View la actualiza
    // explicitamente tras Pin()/Unpin(), nunca se persiste desde aqui.
    [ObservableProperty] private bool _isWindowSizePinned;
    // Pedido explicito del usuario (5-sep-2026): "de momento solo daremos soporte ingles y
    // español de forma nativa" - "es"/"en", nunca otro valor (ver LocalizationService.
    // SetLanguage, cualquier cosa desconocida cae a español). Cambia la app EN VIVO al tocar el
    // chip (OnLanguageChanged siempre llama a SetLanguage, incluso durante LoadFromDisk con
    // _suppressPersist=true - aplicar el idioma guardado al arrancar no es "persistir", es leer).
    [ObservableProperty] private string _language = LocalizationService.Spanish;

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
        ExplorationSidebarWidth = _settings.ExplorationSidebarWidth;
        IsMinimapVisible = _settings.IsMinimapVisible;
        IsWindowSizePinned = WindowPlacementService.IsPinned();
        Language = _settings.Language;
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
        _settings.ExplorationSidebarWidth = ExplorationSidebarWidth;
        _settings.IsMinimapVisible = IsMinimapVisible;
        _settings.Language = Language;
        SettingsService.Save(_settings);
        ApplyToServices();
    }

    partial void OnIsMinimapVisibleChanged(bool value) => Persist();

    partial void OnLanguageChanged(string value)
    {
        LocalizationService.Instance.SetLanguage(value);
        Persist();
    }

    // F-10: 0 (plegada) es un valor real y valido - cualquier otro por debajo de 260 (arrastre
    // real del GridSplitter, MinWidth=0 en el XAML para que 0 sea alcanzable) se recorta al
    // minimo real en vez de quedarse en una zona intermedia ambigua.
    partial void OnExplorationSidebarWidthChanged(double value)
    {
        if (value != 0 && value < 260) { ExplorationSidebarWidth = 260; return; }
        if (value > 520) { ExplorationSidebarWidth = 520; return; }
        Persist();
    }

    // El dialogo real de "elegir carpeta" vive en la View (MainWindow.xaml.cs, mismo criterio
    // ya establecido en H5-03 con SaveItemSet/LoadItemSet - MainViewModel/esta clase se quedan
    // headless de verdad) - aqui solo la logica real sobre una ruta ya elegida.
    // INI-09 (oleada del 6-sep-2026) - BUG REAL: cambiar la lista de carpetas se GUARDABA bien
    // (settings.json y CharacterFileService), pero nadie relanzaba el escaneo - Inicio y
    // Exploracion seguian enseñando exactamente lo mismo que antes hasta pulsar "Actualizar" o
    // reiniciar la app. Y esta pantalla existe justo para el caso contrario: "quien tenga Terraria
    // en otro disco / Documentos redirigidos / instalacion portable ve el lanzador VACIO sin forma
    // de arreglarlo desde la app" (H5-07) - o sea, el usuario añade su carpeta precisamente porque
    // no ve nada, y seguia sin ver nada. Medido: un personaje real dentro de la carpeta recien
    // añadida, presente en disco, ausente del listado.
    //
    // Eventos en vez de llamar directamente a Home/Exploration: esta clase es headless a
    // proposito (decenas de tests construyen un MainViewModel sin tocar disco) y no conoce a
    // nadie. MainViewModel, que si tiene los dos, es quien los enchufa.
    public event Action? CharacterFoldersChanged;
    public event Action? WorldFoldersChanged;

    public void AddCharacterFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || ExtraCharacterFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
        ExtraCharacterFolders.Add(folder);
        Persist();
        CharacterFoldersChanged?.Invoke();
    }

    public void AddWorldFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || ExtraWorldFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
        ExtraWorldFolders.Add(folder);
        Persist();
        WorldFoldersChanged?.Invoke();
    }

    [RelayCommand]
    private void RemoveCharacterFolder(string folder)
    {
        if (!ExtraCharacterFolders.Remove(folder)) return; // no estaba: nada que persistir ni que reescanear
        Persist();
        CharacterFoldersChanged?.Invoke();
    }

    [RelayCommand]
    private void RemoveWorldFolder(string folder)
    {
        if (!ExtraWorldFolders.Remove(folder)) return;
        Persist();
        WorldFoldersChanged?.Invoke();
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
