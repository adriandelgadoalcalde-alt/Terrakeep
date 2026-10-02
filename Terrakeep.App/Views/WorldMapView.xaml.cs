using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/030 (27-sep-2026): decimocuarta extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de WorldMapView.xaml (el HALLAZGO
// REAL completo de code-behind, el mas grande de las 14 rondas). Este control no fija su propio
// DataContext (hereda el MainViewModel real del Window que lo hospeda).
public partial class WorldMapView : UserControl
{
    public WorldMapView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private static LocalizationService Loc => LocalizationService.Instance;

    // ---- Ganchos publicos que MainWindow.xaml.cs invoca desde fuera de este bloque ----------

    // F-11 (auditoria de Opus vs TEdit, E-11): unico punto real de carga de un mundo (los sitios
    // que antes hacian await LoadFromPathAsync + FitWorldMapToWindow por su cuenta -
    // OnLoadWorldClick/OnWorldCardClick/OnWindowDrop/OnGlobalWorldHitClick, todos en
    // MainWindow.xaml.cs - pasan a llamar aqui) para no triplicar la logica de guardar-la-vista-
    // anterior/restaurar-o-ajustar. Guarda la vista del mundo SALIENTE (si habia uno) antes de
    // cargar el nuevo, y tras cargar: si el mundo entrante tiene una vista guardada la restaura,
    // si no, "Ajustar a la ventana" de siempre.
    public async Task LoadWorldAndRestoreView(string path)
    {
        SaveCurrentViewState();

        await ViewModel.Exploration.LoadFromPathAsync(path);

        // DispatcherPriority.Loaded (no Background): el ScrollViewer necesita haber completado un
        // layout real con el WorldImage/extent nuevo (post-Zoom, ya restaurado por
        // LoadFromPathAsync si habia vista guardada) antes de poder pedirle su ViewportWidth/
        // Height o fijar un offset real - misma necesidad ya resuelta por UpdateLayout() en el
        // zoom de la rueda.
        _ = Dispatcher.BeginInvoke(new Action(RestoreViewOrFit), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // CASO DUAL (ADR-TERRAKEEP-016/030, mismo patron ya resuelto por OnLoadClick/HomeView/
    // ADR-026): Click="OnLoadWorldClick" aparece en DOS sitios - el boton de la barra de
    // herramientas (MainWindow.xaml, fuera de este bloque, sigue con su PROPIA copia que llama a
    // WorldMapView.LoadWorldAndRestoreView) y el boton "Cargar mundo" del estado vacio ("Sin mundo
    // cargado", DENTRO de este mismo bloque). Copia local minima: mismo dialogo/filtro exactos,
    // Window.GetWindow(this) en vez de "this" para el propietario del dialogo.
    private async void OnLoadWorldClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc["dlg_load_world"],
            Filter = Loc["dlg_filter_world"],
            InitialDirectory = CharacterFileService.GetDefaultWorldsDirectory(),
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true) await LoadWorldAndRestoreView(dialog.FileName);
    }

    // X-a: boton real "Ajustar a la ventana" (barra de herramientas de MainWindow.xaml, fuera de
    // este bloque) y atajo de teclado Tecla-0 (OnWindowKeyDown, MainWindow.xaml.cs) - los dos
    // disparadores viven fuera de este UserControl, asi que OnFitToWindowClick se queda en
    // MainWindow.xaml.cs y solo reenvia aqui.
    public void FitToWindow()
    {
        WorldMapScroll.UpdateLayout();
        FitWorldMapToWindow();
    }

    private void FitWorldMapToWindow()
    {
        var image = ViewModel.Exploration.WorldImage;
        if (image == null || WorldMapScroll.ViewportWidth <= 0 || WorldMapScroll.ViewportHeight <= 0) return;
        ViewModel.Exploration.Zoom = Math.Min(
            WorldMapScroll.ViewportWidth / image.PixelWidth,
            WorldMapScroll.ViewportHeight / image.PixelHeight);
    }

    // F-11 (auditoria de Opus vs TEdit, E-11): recordar la vista del mundo SALIENTE antes de
    // cargar uno nuevo - mismo mecanismo real ya usado por OnWindowClosing (cerrar la app con un
    // mundo cargado tambien cuenta como "vista saliente"). Antes vivia repetido en los dos sitios
    // (MainWindow.xaml.cs); ahora es un unico gancho publico que los dos llaman.
    public void SaveCurrentViewState()
    {
        if (ViewModel.Exploration.IsWorldLoaded)
            ViewModel.Exploration.SaveCurrentViewState(WorldMapScroll.HorizontalOffset, WorldMapScroll.VerticalOffset);
    }

    // F-11: gemelo real de SaveCurrentViewState - se llama tras LoadFromPathAsync (diferido con
    // Dispatcher.BeginInvoke desde MainWindow.xaml.cs/LoadWorldAndRestoreView, que sigue siendo
    // quien conoce el ORDEN completo carga->restaurar/ajustar). Restaura el offset guardado si
    // el mundo entrante lo tenia, o ajusta a la ventana si no.
    public void RestoreViewOrFit()
    {
        if (ViewModel.Exploration.TryConsumePendingViewRestore(out double offsetH, out double offsetV))
        {
            WorldMapScroll.UpdateLayout();
            WorldMapScroll.ScrollToHorizontalOffset(offsetH);
            WorldMapScroll.ScrollToVerticalOffset(offsetV);
        }
        else
        {
            FitToWindow();
        }
    }

    // F-9 (auditoria de Opus vs TEdit, E-09): atajos de flecha del mapa (OnWindowKeyDown,
    // MainWindow.xaml.cs - Exploracion activa y foco fuera de un TextBox). dx/dy ya vienen con el
    // signo correcto (paso positivo o negativo) desde la llamada.
    public void PanBy(double dx, double dy)
    {
        if (dx != 0) WorldMapScroll.ScrollToHorizontalOffset(WorldMapScroll.HorizontalOffset + dx);
        if (dy != 0) WorldMapScroll.ScrollToVerticalOffset(WorldMapScroll.VerticalOffset + dy);
    }

    // Centra el mapa sobre la posicion de un NPC (pedido desde ExplorationViewModel via
    // NavigateToTileRequested al pulsar un NPC en la lista) - los offsets del ScrollViewer ya
    // estan en espacio post-zoom, igual que en el arrastre. Firma IDENTICA al delegate
    // Action<int,int> del evento: MainWindow.xaml.cs suscribe este metodo DIRECTAMENTE
    // ("+= WorldMapView.NavigateToTile"), sin ningun forwarder intermedio.
    public void NavigateToTile(int tileX, int tileY)
    {
        // F-3 (auditoria de Opus vs TEdit, E-12): con la casilla "Acercar al ir a un resultado"
        // marcada, fija un zoom de trabajo ANTES de centrar - mismo patron real que el zoom con
        // rueda (UpdateLayout() antes de pedir offsets nuevos, para que el ScrollViewer conozca
        // el extent post-LayoutTransform). TEdit fija _zoom=8 en su escala
        // (WorldRenderXna.xaml.cs:8178); el equivalente razonable aqui, dentro del MaxZoom=6.0
        // ya existente, es 4.0.
        if (ViewModel.Exploration.NavigationWantsAutoZoom)
        {
            ViewModel.Exploration.Zoom = 4.0;
            WorldMapScroll.UpdateLayout();
        }
        else
        {
            // Guia v2 (F2): el zoom puede haberse fijado justo antes (ubicacion aproximada -> 100 %);
            // sin medir de nuevo, el ScrollViewer recortaria los offsets al extent del zoom anterior.
            WorldMapScroll.UpdateLayout();
        }
        double zoom = ViewModel.Exploration.Zoom;
        // Guia v2 (F2, 02-oct-2026), hallazgo del canario GUIAV2_SOLO: el panel lateral FLOTA encima
        // del mapa (el mapa ocupa las 3 columnas, ver MainWindow.xaml), asi que centrar sobre el
        // ViewportWidth completo dejaba la casilla pedida DEBAJO del panel a 1080 px (pin en x=475 de
        // un viewport de 945 con el panel tapando desde ~440). Mismo criterio que el aviso "Sin mundo
        // cargado" (SidebarWidthToRightMarginConverter): se centra en el ancho REALMENTE visible.
        double tapado = ViewModel.Settings.ExplorationSidebarWidth > 0 ? ViewModel.Settings.ExplorationSidebarWidth + 9 : 0;
        double anchoVisible = Math.Max(WorldMapScroll.ViewportWidth * 0.35, WorldMapScroll.ViewportWidth - tapado);
        WorldMapScroll.ScrollToHorizontalOffset(tileX * zoom - anchoVisible / 2);
        WorldMapScroll.ScrollToVerticalOffset(tileY * zoom - WorldMapScroll.ViewportHeight / 2);
        UpdateMinimapViewport();
    }

    // F-8 (auditoria de Opus vs TEdit, E-05): el rectangulo de viewport del minimapa necesita
    // recalcularse cada vez que el mapa se desplaza (ScrollChanged, ver mas abajo) O cambia de
    // zoom/mundo (Zoom/WorldImage, ninguno de los dos pasa por ScrollChanged por si solo) - de ahi
    // que MainWindow.xaml.cs siga necesitando un gancho publico para el segundo caso
    // (Exploration.PropertyChanged, suscrito en el constructor de MainWindow).
    public void UpdateMinimapViewport()
    {
        var img = ViewModel.Exploration.WorldImage;
        if (img == null || MinimapImage.ActualWidth <= 0 || MinimapImage.ActualHeight <= 0
            || !ViewModel.Settings.IsMinimapVisible)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }

        double escala = Math.Min(MinimapImage.ActualWidth / img.PixelWidth, MinimapImage.ActualHeight / img.PixelHeight);
        double huecoX = (MinimapImage.ActualWidth - img.PixelWidth * escala) / 2;
        double huecoY = (MinimapImage.ActualHeight - img.PixelHeight * escala) / 2;

        double zoom = ViewModel.Exploration.Zoom;
        if (zoom <= 0 || WorldMapScroll.ViewportWidth <= 0)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }
        // C-02 (auditoria de pulido final, cierra E2): a zoom muy alejado (MinZoom=0.02) el
        // viewport real en tiles de mundo (ViewportWidth/zoom) puede ser MUCHO mas grande que el
        // propio mundo (medido: 50.000 tiles de viewport contra 8.400 de ancho real en un mundo
        // Grande) - sin recortar, el rectangulo salia 6 veces mas ancho que la caja del minimapa
        // y, sin ClipToBounds en ningun contenedor, se pintaba encima de toda la ventana.
        double vpX = Math.Clamp(WorldMapScroll.HorizontalOffset / zoom, 0, img.PixelWidth);
        double vpY = Math.Clamp(WorldMapScroll.VerticalOffset / zoom, 0, img.PixelHeight);
        double vpW = Math.Min(WorldMapScroll.ViewportWidth / zoom, img.PixelWidth - vpX);
        double vpH = Math.Min(WorldMapScroll.ViewportHeight / zoom, img.PixelHeight - vpY);

        // Con el mundo entero ya visible (p.ej. "Ajustar a la ventana" o mas alejado), el
        // rectangulo coincidiria con el borde exacto del minimapa y no aportaria nada - igual que
        // cualquier minimapa real, se oculta en ese caso.
        if (vpW >= img.PixelWidth && vpH >= img.PixelHeight)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetLeft(MinimapViewportRect, huecoX + vpX * escala);
        Canvas.SetTop(MinimapViewportRect, huecoY + vpY * escala);
        MinimapViewportRect.Width = Math.Max(1, vpW * escala);
        MinimapViewportRect.Height = Math.Max(1, vpH * escala);
        MinimapViewportRect.Visibility = Visibility.Visible;
    }

    // ---- Manejadores referenciados SOLO desde el XAML de este mismo bloque -------------------

    // F-8: minimapa real - reutiliza el bitmap del mundo YA congelado (WorldRenderer.cs), sin
    // pintar nada de nuevo.
    private void OnWorldMapScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateMinimapViewport();
    private void OnMinimapSizeChanged(object sender, SizeChangedEventArgs e) => UpdateMinimapViewport();

    private void OnToggleMinimapClick(object sender, RoutedEventArgs e) =>
        ViewModel.Settings.IsMinimapVisible = !ViewModel.Settings.IsMinimapVisible;

    // Clic en el minimapa -> navega, misma conversion clic->tile que TEdit
    // (MainWindow.xaml.cs:946-957: posicion del clic / Resolution -> coordenada de mundo), aqui
    // con la escala real ya calculada arriba en vez de un "Resolution" fijo por muestreo.
    private void OnMinimapClick(object sender, MouseButtonEventArgs e)
    {
        var img = ViewModel.Exploration.WorldImage;
        if (img == null || MinimapImage.ActualWidth <= 0) return;
        double escala = Math.Min(MinimapImage.ActualWidth / img.PixelWidth, MinimapImage.ActualHeight / img.PixelHeight);
        double huecoX = (MinimapImage.ActualWidth - img.PixelWidth * escala) / 2;
        double huecoY = (MinimapImage.ActualHeight - img.PixelHeight * escala) / 2;
        var clic = e.GetPosition(MinimapImage);
        int tileX = (int)((clic.X - huecoX) / escala);
        int tileY = (int)((clic.Y - huecoY) / escala);
        // Via la ViewModel (no NavigateToTile a pelo) para que esta navegacion resuelva su
        // NavigationWantsAutoZoom con la casilla GLOBAL - el clic en el minimapa no viene de
        // "Cofre a cofre" ni de ninguna otra seccion con casilla propia, y asi conserva
        // exactamente el comportamiento que tenia antes del punto 4.
        ViewModel.Exploration.NavigateToTile(tileX, tileY);
    }

    // Rueda del raton = zoom directamente (sin necesitar Ctrl, pedido explicito - el arrastre ya
    // cubre el desplazamiento normal, asi que la rueda no hace falta para nada mas aqui).
    //
    // Bug real corregido (1-sep-2026, reportado: "el zoom no lo hace recto"): cambiar solo Zoom
    // sin tocar los offsets del ScrollViewer hace zoom desde la esquina superior izquierda del
    // mapa (offset 0,0), no desde donde esta el cursor - la vista "salta" en vez de hacer zoom
    // centrado en el punto que se esta mirando. Se calcula la coordenada de mundo bajo el cursor
    // ANTES de cambiar el zoom, y se recoloca el offset para que ese mismo punto de mundo siga
    // bajo el cursor DESPUES. UpdateLayout() fuerza a que el ScrollViewer ya conozca el nuevo
    // tamaño de contenido (post-LayoutTransform) antes de pedirle el nuevo offset - sin esto,
    // ScrollToHorizontalOffset calcularia contra el extent viejo todavia y el resultado seguiria
    // sin cuadrar.
    private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double oldZoom = ViewModel.Exploration.Zoom;
        var mousePos = e.GetPosition(WorldMapScroll);
        double worldX = (WorldMapScroll.HorizontalOffset + mousePos.X) / oldZoom;
        double worldY = (WorldMapScroll.VerticalOffset + mousePos.Y) / oldZoom;

        // X-b (segunda auditoria de Opus, Fable): mismo paso real que los botones ZoomIn/ZoomOut
        // (ExplorationViewModel.ZoomStep) - antes la rueda usaba x1.15, un paso distinto solo por
        // costumbre, no por ningun motivo real.
        ViewModel.Exploration.Zoom = oldZoom * (e.Delta > 0 ? ExplorationViewModel.ZoomStep : 1 / ExplorationViewModel.ZoomStep);
        double newZoom = ViewModel.Exploration.Zoom;

        WorldMapScroll.UpdateLayout();
        WorldMapScroll.ScrollToHorizontalOffset(worldX * newZoom - mousePos.X);
        WorldMapScroll.ScrollToVerticalOffset(worldY * newZoom - mousePos.Y);
        e.Handled = true;
    }

    // Arrastrar con el boton izquierdo para desplazar el mapa (pan) - captura el raton al pulsar
    // y mueve los offsets del ScrollViewer segun el desplazamiento real del cursor en pantalla,
    // sin depender del zoom actual (los offsets del ScrollViewer ya estan en el espacio
    // POST-transformacion porque el ScaleTransform esta en LayoutTransform, no RenderTransform).
    private Point? _mapDragStart;
    private double _mapDragStartH, _mapDragStartV;
    // Se pone a true en cuanto el raton se mueve mas que MapClickSlopPx con el boton pulsado:
    // distingue "el usuario ha hecho clic" de "el usuario ha arrastrado el mapa" (ver
    // OnWorldMapMouseUp).
    private bool _mapDragMoved;

    // Bug real reportado por el usuario (18-sep-2026, ver bitacora.md "el clic sobre el cofre
    // real no hace nada"): este handler llamaba a WorldMapScroll.CaptureMouse() de forma
    // INCONDICIONAL en cualquier boton izquierdo pulsado dentro del mapa (para poder
    // arrastrar/paneear). CaptureMode.Element (el modo por defecto de CaptureMouse()) hace que
    // el MouseLeftButtonUp correspondiente NUNCA llegue al elemento real bajo el cursor - asi que
    // ningun MouseBinding LeftClick de un marcador del mapa (CurrentChestMarker,
    // WorldSearchResults...) podia completar su ciclo down+up, aunque el hit-test de WPF
    // identificara correctamente el marcador como Mouse.DirectlyOver/OriginalSource. Arreglo: si
    // el clic empieza sobre un elemento con su propio MouseBinding (un marcador clicable real), no
    // capturamos el raton y dejamos que el propio marcador reciba su ciclo de clic normal - solo
    // capturamos para arrastrar/paneear cuando el clic empieza sobre mapa vacio, que sigue siendo
    // el comportamiento de siempre.
    private void OnWorldMapMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (OriginatesFromClickableMarker(e.OriginalSource as DependencyObject, WorldMapScroll))
        {
            return;
        }

        _mapDragStart = e.GetPosition(WorldMapScroll);
        _mapDragStartH = WorldMapScroll.HorizontalOffset;
        _mapDragStartV = WorldMapScroll.VerticalOffset;
        _mapDragMoved = false;
        WorldMapScroll.CaptureMouse();
    }

    // Umbral en pixeles de pantalla por debajo del cual un down+up cuenta como CLIC y no como
    // arrastre. No es cero a proposito: un clic humano real casi nunca deja el cursor exactamente
    // en el mismo pixel entre el down y el up, y sin margen el clic se perderia casi siempre.
    private const double MapClickSlopPx = 4.0;

    // Sube el arbol visual/logico desde el elemento real que origino el evento (e.OriginalSource
    // de un evento tunneling SIEMPRE es el elemento mas interno, independientemente de que
    // ancestro maneje el Preview) hasta encontrar un UIElement con InputBindings propios (un
    // marcador clicable real, ver MouseBinding MouseAction="LeftClick" en WorldMapView.xaml) o
    // hasta llegar al limite (el propio WorldMapScroll, mapa vacio). InputBindings es una
    // coleccion perezosa: leerla en un elemento que nunca la uso en XAML no tiene efecto
    // secundario, simplemente devuelve Count=0.
    private static bool OriginatesFromClickableMarker(DependencyObject? source, DependencyObject boundary)
    {
        while (source is not null && !ReferenceEquals(source, boundary))
        {
            if (source is UIElement { InputBindings.Count: > 0 })
            {
                return true;
            }

            source = source switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(source),
                ContentElement contentElement => LogicalTreeHelper.GetParent(contentElement),
                _ => null
            };
        }

        return false;
    }

    private void OnWorldMapMouseUp(object sender, MouseButtonEventArgs e)
    {
        bool fueClic = _mapDragStart is not null && !_mapDragMoved;
        _mapDragStart = null;
        _mapDragMoved = false;
        WorldMapScroll.ReleaseMouseCapture();

        // Tercer reporte real del usuario (19-sep-2026): "por mucho que clique un cofre por el
        // mapa no me abre ni su contenido ni lo que es para editar". Era cierto y no existia - ver
        // TryOpenChestAtTile en ExplorationViewModel. Va en el UP y solo si NO hubo arrastre, para
        // no robarle nada al pan de siempre (que es lo que hace este mismo raton cuando el usuario
        // mueve). GetPosition(WorldMapImage) ya devuelve pixel nativo de la imagen = tile real,
        // exactamente igual que el tooltip de hover de OnWorldMapMouseMove.
        if (!fueClic) return;
        var pos = e.GetPosition(WorldMapImage);
        ViewModel.Exploration.TryOpenChestAtTile((int)pos.X, (int)pos.Y);
    }

    // GetPosition(WorldMapImage) ya devuelve la posicion en el espacio de pixel NATIVO de la
    // imagen (WPF deshace el LayoutTransform/zoom automaticamente para el elemento sobre el que se
    // pide la posicion) - y WorldRenderer pinta a 1 pixel por tile, asi que el pixel es
    // directamente la coordenada de tile. Este handler vive en el ScrollViewer (no en la Image)
    // para que siga disparandose durante el arrastre, cuando el raton tiene captura.
    private void OnWorldMapMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(WorldMapImage);
        ViewModel.Exploration.UpdateHover((int)pos.X, (int)pos.Y);

        if (_mapDragStart is { } start && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(WorldMapScroll);
            if (Math.Abs(current.X - start.X) > MapClickSlopPx || Math.Abs(current.Y - start.Y) > MapClickSlopPx)
            {
                _mapDragMoved = true;
            }
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
    // voltea al otro lado si no cabe por el borde derecho/inferior del propio Canvas (que ocupa
    // exactamente el area visible del mapa, ver WorldMapView.xaml) - sin esto el texto se saldria
    // cortado fuera del visor en los bordes.
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
        // SetCurrentValue, no el setter directo: Visibility ya tiene un Binding real en el XAML (a
        // Exploration.HoverInfo via EmptyToCollapsed) - asignar la propiedad a secas reemplazaria
        // ese binding para siempre; SetCurrentValue solo empuja un valor puntual sin desengancharlo.
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
    }

    private void OnWorldMapMouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel.Exploration.UpdateHover(-1, -1);
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
    }
}
