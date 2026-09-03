using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Auditoria de Opus, I-1 ("Inicio deberia ser un lanzador real, no una pagina de bienvenida
// estatica que obliga a abrir el Explorador de archivos incluso para el caso normal"): escanea
// UNA VEZ al arrancar (y bajo demanda con "Actualizar", por si se ha guardado algo nuevo desde
// fuera) la carpeta real de personajes de tModLoader - la misma que ya usaba el dialogo de
// "Cargar personaje..." como carpeta inicial (CharacterFileService.GetDefaultPlayersDirectory).
// Solo lee lo minimo real de cada .plr (PlrFile.Read completo, son ficheros pequeños - sin
// tocar el .tplr, que solo hace falta para editar de verdad) - un error real leyendo un fichero
// concreto (corrupto, formato ajeno) no debe tumbar el listado entero de los demas.
public partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<CharacterListEntryViewModel> Characters { get; } = [];

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string? _scanMessage;

    // I-a: ruta real del personaje cargado ahora mismo en MainViewModel (null si ninguno) -
    // MainViewModel.LoadFromPath la actualiza en su finally, tanto en exito como en fallo.
    private string? _currentPath;

    public event Action<string>? CharacterChosen;

    public HomeViewModel()
    {
        Refresh();
    }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". Se llama tanto al cargar/cambiar de
    // personaje como tras Refresh() (la lista se reconstruye entera, IsCurrent no sobrevive).
    public void UpdateCurrentPath(string? path)
    {
        _currentPath = path;
        foreach (var entry in Characters)
            entry.IsCurrent = string.Equals(entry.FilePath, path, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void Refresh()
    {
        Characters.Clear();
        IsScanning = true;
        try
        {
            string dir = CharacterFileService.GetDefaultPlayersDirectory();
            var plrFiles = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.plr") : [];
            foreach (string path in plrFiles.OrderByDescending(File.GetLastWriteTimeUtc))
            {
                try
                {
                    var character = PlrFile.Read(File.ReadAllBytes(path));
                    bool isCalamity = File.Exists(Path.ChangeExtension(path, ".tplr"));
                    Characters.Add(new CharacterListEntryViewModel(path, character, isCalamity, File.GetLastWriteTimeUtc(path)));
                }
                catch (Exception)
                {
                    // Un .plr ajeno/corrupto no debe tumbar el listado de los demas - se omite
                    // en silencio, igual que ya hace la Libreria con ids sin catalogar.
                }
            }
            ScanMessage = Characters.Count == 0
                ? $"Ningun personaje encontrado en {dir}"
                : null;
            UpdateCurrentPath(_currentPath); // la lista es nueva de cero, IsCurrent hay que recalcularlo
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Open(CharacterListEntryViewModel entry) => CharacterChosen?.Invoke(entry.FilePath);

    // I-b (segunda auditoria de Opus, Fable): "Sin ninguna accion secundaria en la tarjeta -
    // faltan las 3 obvias y baratas: abrir carpeta, duplicar personaje, restaurar copia de
    // seguridad". Menu contextual real en la tarjeta (ver MainWindow.xaml).
    [RelayCommand]
    private void OpenFolder(CharacterListEntryViewModel entry)
    {
        try
        {
            // /select, resalta el fichero real en el Explorador en vez de solo abrir la carpeta
            // a ciegas - mismo gesto real que "Mostrar en carpeta" de cualquier otra app.
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{entry.FilePath}\"");
        }
        catch (Exception ex)
        {
            ScanMessage = $"Error al abrir la carpeta: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Duplicate(CharacterListEntryViewModel entry)
    {
        try
        {
            // "la red de seguridad real para experimentar" - copia de fichero pura (mismo
            // nombre interno del personaje, solo cambia el archivo) para poder tocar la copia
            // sin arriesgar el original. Numerado si "(copia)" ya existe, nunca sobrescribe.
            string dir = Path.GetDirectoryName(entry.FilePath)!;
            string baseName = Path.GetFileNameWithoutExtension(entry.FilePath);
            string newPath = Path.Combine(dir, $"{baseName} (copia).plr");
            for (int n = 2; File.Exists(newPath); n++)
                newPath = Path.Combine(dir, $"{baseName} (copia {n}).plr");

            File.Copy(entry.FilePath, newPath);
            string tplrSrc = Path.ChangeExtension(entry.FilePath, ".tplr");
            if (File.Exists(tplrSrc)) File.Copy(tplrSrc, Path.ChangeExtension(newPath, ".tplr"));
            Refresh();
        }
        catch (Exception ex)
        {
            ScanMessage = $"Error al duplicar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RestoreBackup(CharacterListEntryViewModel entry)
    {
        try
        {
            // Mismo .bak real que T-C ya deja (CharacterFileService.WriteAtomic) - esto es el
            // mismo mecanismo de "Deshacer ultimo guardado" de la cabecera, pero operando sobre
            // CUALQUIER personaje de la lista, este cargado ahora mismo o no.
            string plrBak = entry.FilePath + ".bak";
            if (!File.Exists(plrBak))
            {
                ScanMessage = $"'{entry.Name}' no tiene ninguna copia de seguridad real que restaurar.";
                return;
            }
            File.Copy(plrBak, entry.FilePath, overwrite: true);
            string tplrPath = Path.ChangeExtension(entry.FilePath, ".tplr");
            string tplrBak = tplrPath + ".bak";
            if (File.Exists(tplrBak)) File.Copy(tplrBak, tplrPath, overwrite: true);
            Refresh();
        }
        catch (Exception ex)
        {
            ScanMessage = $"Error al restaurar la copia: {ex.Message}";
        }
    }
}
