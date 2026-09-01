using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Cargar personaje de Terraria",
            Filter = "Personaje de Terraria (*.plr)|*.plr|Todos los archivos (*.*)|*.*",
            InitialDirectory = GetDefaultPlayersDirectory(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.LoadFromPath(dialog.FileName);
        }
    }

    private static string GetDefaultPlayersDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string candidate = Path.Combine(documents, "My Games", "Terraria", "tModLoader", "Players");
        return Directory.Exists(candidate) ? candidate : documents;
    }

    private void OnLoadWorldClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Cargar mundo de Terraria",
            Filter = "Mundo de Terraria (*.wld)|*.wld|Todos los archivos (*.*)|*.*",
            InitialDirectory = GetDefaultWorldsDirectory(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.Exploration.LoadFromPath(dialog.FileName);
        }
    }

    private static string GetDefaultWorldsDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string candidate = Path.Combine(documents, "My Games", "Terraria", "tModLoader", "Worlds");
        return Directory.Exists(candidate) ? candidate : documents;
    }

    // Ctrl+rueda para hacer zoom sobre el visor de mundo, ademas de los botones +/-/
    // Restablecer - marca el evento como manejado solo cuando de verdad hace zoom, para no
    // robarle el scroll normal (sin Ctrl) al ScrollViewer.
    private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        _viewModel.Exploration.Zoom *= e.Delta > 0 ? 1.15 : 1 / 1.15;
        e.Handled = true;
    }
}
