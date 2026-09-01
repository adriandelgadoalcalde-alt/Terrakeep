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
        _viewModel.Exploration.NavigateToTileRequested += OnNavigateToTile;
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

    // Rueda del raton = zoom directamente (sin necesitar Ctrl, pedido explicito - el arrastre ya
    // cubre el desplazamiento normal, asi que la rueda no hace falta para nada mas aqui).
    private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _viewModel.Exploration.Zoom *= e.Delta > 0 ? 1.15 : 1 / 1.15;
        e.Handled = true;
    }

    // Arrastrar con el boton izquierdo para desplazar el mapa (pan) - captura el raton al
    // pulsar y mueve los offsets del ScrollViewer segun el desplazamiento real del cursor en
    // pantalla, sin depender del zoom actual (los offsets del ScrollViewer ya estan en el
    // espacio POST-transformacion porque el ScaleTransform esta en LayoutTransform, no
    // RenderTransform).
    private Point? _mapDragStart;
    private double _mapDragStartH, _mapDragStartV;

    private void OnWorldMapMouseDown(object sender, MouseButtonEventArgs e)
    {
        _mapDragStart = e.GetPosition(WorldMapScroll);
        _mapDragStartH = WorldMapScroll.HorizontalOffset;
        _mapDragStartV = WorldMapScroll.VerticalOffset;
        WorldMapScroll.CaptureMouse();
    }

    private void OnWorldMapMouseUp(object sender, MouseButtonEventArgs e)
    {
        _mapDragStart = null;
        WorldMapScroll.ReleaseMouseCapture();
    }

    // GetPosition(WorldMapImage) ya devuelve la posicion en el espacio de pixel NATIVO de la
    // imagen (WPF deshace el LayoutTransform/zoom automaticamente para el elemento sobre el
    // que se pide la posicion) - y WorldRenderer pinta a 1 pixel por tile, asi que el pixel es
    // directamente la coordenada de tile. Este handler vive en el ScrollViewer (no en la
    // Image) para que siga disparandose durante el arrastre, cuando el raton tiene captura.
    private void OnWorldMapMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(WorldMapImage);
        _viewModel.Exploration.UpdateHover((int)pos.X, (int)pos.Y);

        if (_mapDragStart is { } start && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(WorldMapScroll);
            WorldMapScroll.ScrollToHorizontalOffset(_mapDragStartH - (current.X - start.X));
            WorldMapScroll.ScrollToVerticalOffset(_mapDragStartV - (current.Y - start.Y));
        }
    }

    private void OnWorldMapMouseLeave(object sender, MouseEventArgs e) => _viewModel.Exploration.UpdateHover(-1, -1);

    // Centra el mapa sobre la posicion de un NPC (pedido desde ExplorationViewModel via
    // NavigateToTileRequested al pulsar un NPC en la lista) - los offsets del ScrollViewer ya
    // estan en espacio post-zoom, igual que en el arrastre.
    private void OnNavigateToTile(int tileX, int tileY)
    {
        double zoom = _viewModel.Exploration.Zoom;
        WorldMapScroll.ScrollToHorizontalOffset(tileX * zoom - WorldMapScroll.ViewportWidth / 2);
        WorldMapScroll.ScrollToVerticalOffset(tileY * zoom - WorldMapScroll.ViewportHeight / 2);
    }
}
