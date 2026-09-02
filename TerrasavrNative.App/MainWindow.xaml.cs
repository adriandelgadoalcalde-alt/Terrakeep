using System.IO;
using System.Windows;
using System.Windows.Controls;
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
    //
    // Bug real corregido (1-sep-2026, reportado: "el zoom no lo hace recto"): cambiar solo
    // Zoom sin tocar los offsets del ScrollViewer hace zoom desde la esquina superior
    // izquierda del mapa (offset 0,0), no desde donde esta el cursor - la vista "salta" en vez
    // de hacer zoom centrado en el punto que se esta mirando. Se calcula la coordenada de
    // mundo bajo el cursor ANTES de cambiar el zoom, y se recoloca el offset para que ese
    // mismo punto de mundo siga bajo el cursor DESPUES. UpdateLayout() fuerza a que el
    // ScrollViewer ya conozca el nuevo tamaño de contenido (post-LayoutTransform) antes de
    // pedirle el nuevo offset - sin esto, ScrollToHorizontalOffset calcularia contra el
    // extent viejo todavia y el resultado seguiria sin cuadrar.
    private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double oldZoom = _viewModel.Exploration.Zoom;
        var mousePos = e.GetPosition(WorldMapScroll);
        double worldX = (WorldMapScroll.HorizontalOffset + mousePos.X) / oldZoom;
        double worldY = (WorldMapScroll.VerticalOffset + mousePos.Y) / oldZoom;

        _viewModel.Exploration.Zoom = oldZoom * (e.Delta > 0 ? 1.15 : 1 / 1.15);
        double newZoom = _viewModel.Exploration.Zoom;

        WorldMapScroll.UpdateLayout();
        WorldMapScroll.ScrollToHorizontalOffset(worldX * newZoom - mousePos.X);
        WorldMapScroll.ScrollToVerticalOffset(worldY * newZoom - mousePos.Y);
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
            MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed); // no molesta mientras se arrastra
        }
        else
        {
            PositionMapTooltip(e.GetPosition(MapTooltipCanvas));
        }
    }

    // Tooltip flotante estilo TEdit: se coloca con un pequeño margen respecto al cursor y se
    // voltea al otro lado si no cabe por el borde derecho/inferior del propio Canvas (que
    // ocupa exactamente el area visible del mapa, ver MainWindow.xaml) - sin esto el texto se
    // saldria cortado fuera del visor en los bordes.
    private void PositionMapTooltip(Point cursorPos)
    {
        const double offset = 16, marginY = 18;
        MapTooltipBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = MapTooltipBorder.DesiredSize;

        double left = cursorPos.X + offset;
        if (left + size.Width > MapTooltipCanvas.ActualWidth) left = cursorPos.X - offset - size.Width;

        double top = cursorPos.Y + marginY;
        if (top + size.Height > MapTooltipCanvas.ActualHeight) top = cursorPos.Y - marginY - size.Height;

        Canvas.SetLeft(MapTooltipBorder, Math.Max(0, left));
        Canvas.SetTop(MapTooltipBorder, Math.Max(0, top));
        // SetCurrentValue, no el setter directo: Visibility ya tiene un Binding real en el XAML
        // (a Exploration.HoverInfo via EmptyToCollapsed) - asignar la propiedad a secas
        // reemplazaria ese binding para siempre; SetCurrentValue solo empuja un valor puntual
        // sin desengancharlo.
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
    }

    private void OnWorldMapMouseLeave(object sender, MouseEventArgs e)
    {
        _viewModel.Exploration.UpdateHover(-1, -1);
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
    }

    // Centra el mapa sobre la posicion de un NPC (pedido desde ExplorationViewModel via
    // NavigateToTileRequested al pulsar un NPC en la lista) - los offsets del ScrollViewer ya
    // estan en espacio post-zoom, igual que en el arrastre.
    private void OnNavigateToTile(int tileX, int tileY)
    {
        double zoom = _viewModel.Exploration.Zoom;
        WorldMapScroll.ScrollToHorizontalOffset(tileX * zoom - WorldMapScroll.ViewportWidth / 2);
        WorldMapScroll.ScrollToVerticalOffset(tileY * zoom - WorldMapScroll.ViewportHeight / 2);
    }

    // Arrastrar y soltar (pedido explicito 1-sep-2026: "se puede arrastar para poder ir
    // poniendo en el inventario o en accesorios pero todo se visualiza en sprites"). Deteccion
    // de arrastre estandar de WPF: se guarda la posicion en el boton-abajo, y solo se arranca
    // DoDragDrop de verdad si el raton se mueve mas alla del umbral del sistema con el boton
    // aun pulsado - asi un simple clic (sin mover el raton) sigue llegando normal a los
    // botones de dentro de la tarjeta (Cambiar/★/✎/✕), que ya funcionaban por clic.
    private Point? _dragStartLibrary;
    private Point? _dragStartSlot;

    private void OnLibraryCardMouseDown(object sender, MouseButtonEventArgs e) => _dragStartLibrary = e.GetPosition(null);

    private void OnLibraryCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartLibrary is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartLibrary = null;

        if (sender is FrameworkElement { DataContext: LibraryItemViewModel item } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(LibraryItemViewModel), item), DragDropEffects.Copy);
    }

    // Un clic en cualquier slot (incluidos los botones ★/✕/Cambiar de dentro, ya que este
    // manejador es PreviewMouseLeftButtonDown en el Border completo) lo selecciona para el
    // panel "Editar" compartido - pedido explicito 1-sep-2026.
    private void OnItemSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartSlot = e.GetPosition(null);
        if (sender is FrameworkElement { DataContext: ItemSlotViewModel slot })
            _viewModel.SelectSlot(slot);
    }

    private void OnItemSlotMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartSlot is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartSlot = null;

        if (sender is FrameworkElement { DataContext: ItemSlotViewModel { IsEmpty: false } slot } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(ItemSlotViewModel), slot), DragDropEffects.Move);
    }

    // Retroalimentación de la restricción de slot MIENTRAS se arrastra, antes de soltar
    // (consulta a Opus, sexta pasada: "el propio cursor del sistema se convierte en el
    // símbolo de prohibido... es el idioma del SO, no el tuyo, y llega ANTES de soltar" - la
    // capa mas barata que existe, cero chrome nuevo). Cubre los dos origenes reales de
    // arrastre (tarjeta de la Libreria y otro slot).
    private void OnItemSlotDragOver(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ItemSlotViewModel targetSlot }) return;

        bool accepted;
        if (e.Data.GetDataPresent(typeof(LibraryItemViewModel)) && e.Data.GetData(typeof(LibraryItemViewModel)) is LibraryItemViewModel libraryItem)
        {
            accepted = targetSlot.AcceptsItem(libraryItem.Id);
        }
        else if (e.Data.GetDataPresent(typeof(ItemSlotViewModel)) && e.Data.GetData(typeof(ItemSlotViewModel)) is ItemSlotViewModel sourceSlot)
        {
            // El intercambio mueve el objeto en AMBAS direcciones - hay que validar que cada
            // slot acepta lo que le va a llegar, no solo el destino.
            accepted = targetSlot.AcceptsItem(sourceSlot.Item.Id) && sourceSlot.AcceptsItem(targetSlot.Item.Id);
        }
        else
        {
            accepted = true;
        }

        e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    // Soltar una tarjeta de la Libreria coloca ese objeto (igual que "Cambiar objeto"); soltar
    // otro slot arrastrado los intercambia entero (prefijo/cantidad/favorito incluidos).
    private void OnItemSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ItemSlotViewModel targetSlot }) return;

        if (e.Data.GetDataPresent(typeof(LibraryItemViewModel)) && e.Data.GetData(typeof(LibraryItemViewModel)) is LibraryItemViewModel libraryItem)
        {
            targetSlot.PlaceItem(libraryItem.Id);
        }
        else if (e.Data.GetDataPresent(typeof(ItemSlotViewModel)) && e.Data.GetData(typeof(ItemSlotViewModel)) is ItemSlotViewModel sourceSlot
                 && !ReferenceEquals(sourceSlot, targetSlot)
                 && targetSlot.AcceptsItem(sourceSlot.Item.Id) && sourceSlot.AcceptsItem(targetSlot.Item.Id))
        {
            sourceSlot.SwapWith(targetSlot);
        }
    }

    // Gemelos de los 3 de arriba, para la rejilla de Buffs (pregunta a Opus sobre el diseño
    // 2-sep-2026, cuarta pasada) - mismo patron exacto, solo cambia el tipo (BuffSlotViewModel
    // en vez de ItemSlotViewModel). Arrastrar un buff sobre otro los intercambia entero
    // (id+duracion).
    private Point? _dragStartBuffSlot;

    private void OnBuffSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartBuffSlot = e.GetPosition(null);
        if (sender is FrameworkElement { DataContext: BuffSlotViewModel slot })
            _viewModel.SelectBuffSlot(slot);
    }

    private void OnBuffSlotMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartBuffSlot is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartBuffSlot = null;

        if (sender is FrameworkElement { DataContext: BuffSlotViewModel { IsEmpty: false } slot } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(BuffSlotViewModel), slot), DragDropEffects.Move);
    }

    // Bug real reportado 2-sep-2026 ("los buff no se pueden arrastrar hasta la rejilla de
    // buff"): OnBuffSlotDrop solo aceptaba OTRO slot arrastrado (intercambio), nunca una
    // tarjeta arrastrada desde la Libreria de buffs - a diferencia de OnItemSlotDrop, que ya
    // acepta LibraryItemViewModel ademas de ItemSlotViewModel. Arreglado igual: la tarjeta de
    // la Libreria de buffs (BuffCatalogEntryViewModel) tambien inicia arrastre.
    private Point? _dragStartBuffLibrary;

    private void OnBuffLibraryCardMouseDown(object sender, MouseButtonEventArgs e) => _dragStartBuffLibrary = e.GetPosition(null);

    private void OnBuffLibraryCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartBuffLibrary is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartBuffLibrary = null;

        if (sender is FrameworkElement { DataContext: BuffCatalogEntryViewModel entry } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(BuffCatalogEntryViewModel), entry), DragDropEffects.Copy);
    }

    private void OnBuffSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BuffSlotViewModel targetSlot }) return;

        if (e.Data.GetDataPresent(typeof(BuffCatalogEntryViewModel)) && e.Data.GetData(typeof(BuffCatalogEntryViewModel)) is BuffCatalogEntryViewModel libraryEntry)
        {
            targetSlot.PlaceBuff(libraryEntry.Id);
        }
        else if (e.Data.GetDataPresent(typeof(BuffSlotViewModel)) && e.Data.GetData(typeof(BuffSlotViewModel)) is BuffSlotViewModel sourceSlot
                 && !ReferenceEquals(sourceSlot, targetSlot))
        {
            sourceSlot.SwapWith(targetSlot);
        }
    }
}
