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

    public event Action<string>? CharacterChosen;

    public HomeViewModel()
    {
        Refresh();
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
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Open(CharacterListEntryViewModel entry) => CharacterChosen?.Invoke(entry.FilePath);
}
