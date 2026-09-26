using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/026 (26-sep-2026): DECIMA extraccion real de una seccion de MainWindow.xaml
// a un UserControl - ver el comentario real de HomeView.xaml. A diferencia de las 8 extracciones
// anteriores desde ACERCA DE/ADR-020 (que si movio code-behind, pero SIN ningun caso de uso
// dual), INICIO trae 6 manejadores de evento reales repartidos en dos casos distintos:
//   - 5 MOVIDOS tal cual (ningun otro sitio de MainWindow.xaml.cs los usaba, grep confirmado):
//     OnCharacterCardMouseEnter/OnCharacterCardMouseLeave/OnCharacterCardUnloaded (hover/limpieza
//     de la tarjeta de personaje, CharacterCardTemplate) y OnHomeBannerMouseEnter/
//     OnHomeBannerMouseLeave (hover del banner "Continuar con X") - mismo mecanismo real que
//     AboutView/ADR-020: `this` (Window) -> Window.GetWindow(this) donde hace falta, campo
//     privado `_viewModel` de MainWindow -> `ViewModel` ((MainViewModel)DataContext, seguro
//     porque este UserControl nunca fija su propio DataContext).
//   - 1 caso NUEVO, no visto en ninguna ronda anterior (recurso DUAL, no solo window-scoped o
//     autocontenido): OnLoadClick aparece 2 veces DENTRO de Inicio (boton "Cargar de otra
//     carpeta" y tarjeta "Empezar: cargar personaje") pero el MISMO nombre de metodo tambien
//     sigue haciendo falta FUERA de Inicio (MenuItem "Cargar personaje" del menu "Personaje ▾" de
//     la cabecera global, que se queda en MainWindow.xaml) - no se puede mover entero. Este
//     OnLoadClick es una copia PROPIA de esta clase (mismo nombre, clase distinta, sin colision),
//     mismo OpenFileDialog/Title/Filter que el original de MainWindow.xaml.cs, pero usa
//     ViewModel.ConfirmDiscardChanges (el Func<bool>? PUBLICO que MainViewModel ya expone,
//     MainWindow.xaml.cs conecta ese delegado exactamente a su propio metodo privado
//     ConfirmDiscardChanges(Loc["dlg_action_load_other"]) - inaccesible desde aqui) en vez de
//     reinventar una segunda logica de confirmacion: invocar el delegado es el MISMO camino real
//     que ya toma el original, no una copia que pueda desincronizarse.
public partial class HomeView : UserControl
{
    private static LocalizationService Loc => LocalizationService.Instance;

    public HomeView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // Ver el comentario de clase de arriba: mismo dialogo/filtro/carpeta inicial que
    // MainWindow.xaml.cs.OnLoadClick (que se queda ahi, todavia hace falta para el MenuItem
    // "Cargar personaje" de la cabecera global) - aqui con Window.GetWindow(this) como dueño del
    // dialogo y ViewModel.ConfirmDiscardChanges (delegado ya conectado por MainWindow) en vez del
    // metodo privado ConfirmDiscardChanges(string) de MainWindow.
    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc["dlg_load_character"],
            Filter = Loc["dlg_filter_character"],
            InitialDirectory = CharacterFileService.GetDefaultPlayersDirectory(),
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            if (ViewModel.ConfirmDiscardChanges?.Invoke() == false) return;
            ViewModel.LoadFromPath(dialog.FileName);
        }
    }

    // Hover en Inicio ("que ande solo al pasar el raton" - bitacora.md 21-sep-2026, catalogo de
    // ideas Keep). DataContext puede no ser el ViewModel esperado en un evento de enrutado
    // (burbujea desde hijos con otro DataContext, p.ej. el ContextMenu) - "as" + comprobacion
    // null, nunca un cast directo. Ver el comentario real completo original junto a
    // CharacterListEntryViewModel.SetHovering.
    private void OnCharacterCardMouseEnter(object sender, MouseEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterListEntryViewModel entry)
            entry.SetHovering(true);
    }

    private void OnCharacterCardMouseLeave(object sender, MouseEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterListEntryViewModel entry)
            entry.SetHovering(false);
    }

    // Red de seguridad adicional (mismo criterio que ya cerro el bug real "Terrakeep congelado" de
    // Apariencia): si la tarjeta desaparece del arbol visual (rescan de Inicio) mientras el raton
    // seguia encima, WPF no siempre llega a disparar MouseLeave a tiempo - parar aqui tambien
    // garantiza que el timer de esa tarjeta nunca pueda quedar corriendo solo.
    private void OnCharacterCardUnloaded(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CharacterListEntryViewModel entry)
            entry.SetHovering(false);
    }

    // Arreglo real 25-sep-2026 (bitacora.md, "Inicio, mascotas ocultas y banner sin hover"): el
    // banner "Continuar con X" comparte el MISMO CharacterListEntryViewModel que ya anima con
    // exito en las tarjetas (ViewModel.Home.LastSessionCharacterEntry), pero su DataContext real
    // es el MainViewModel entero (el Border del banner vive fuera de CharacterCardTemplate) -
    // reusar OnCharacterCardMouseEnter/Leave a pelo aqui leeria (sender as FrameworkElement)?.
    // DataContext como MainViewModel, no como CharacterListEntryViewModel, y el cast fallaria en
    // silencio (sin excepcion, simplemente sin animar nada) - de ahi este par propio que lee
    // ViewModel.Home.LastSessionCharacterEntry directamente.
    private void OnHomeBannerMouseEnter(object sender, MouseEventArgs e) => ViewModel.Home.LastSessionCharacterEntry?.SetHovering(true);

    private void OnHomeBannerMouseLeave(object sender, MouseEventArgs e) => ViewModel.Home.LastSessionCharacterEntry?.SetHovering(false);
}
