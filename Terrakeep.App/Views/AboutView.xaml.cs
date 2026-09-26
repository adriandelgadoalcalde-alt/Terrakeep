using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/020 (26-sep-2026): CUARTA extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de AboutView.xaml. A diferencia de
// las 3 extracciones anteriores (NOVEDADES/BUILDS/SERVIDOR, ninguna con logica propia en
// MainWindow.xaml.cs), ACERCA DE SI tenia code-behind real: 3 manejadores de Click
// (OnAddCharacterFolderClick/OnAddWorldFolderClick/OnPinWindowSizeClick), movidos aqui tal cual
// - los 3 usaban `this` como Window real (para el dueño del OpenFolderDialog y para
// WindowPlacementService.Pin) y el campo privado `_viewModel` de MainWindow. Un UserControl no
// es un Window ni tiene ese campo, asi que:
//   - `this` (Window) -> Window.GetWindow(this): sube el arbol visual hasta el Window real que
//     hospeda este control (funciona igual sea el UserControl de nivel superior o este mismo
//     anidado mas profundo, WPF no distingue "cruzar" un UserControl al subir el arbol visual -
//     mismo razonamiento ya usado por ADR-016 punto 2e para RelativeSource AncestorType=Window).
//   - `_viewModel` -> (MainViewModel)DataContext: este control hereda el MainViewModel real del
//     Window sin fijar su propio DataContext (regla 2d del ADR), asi que el cast es seguro.
public partial class AboutView : UserControl
{
    private static LocalizationService Loc => LocalizationService.Instance;

    public AboutView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // H5-07 (quinta auditoria de Opus): dialogo real de "elegir carpeta" - vive aqui (View),
    // no en SettingsViewModel (mismo criterio real ya establecido en H5-03 con SaveItemSet/
    // LoadItemSet - MainViewModel/sus sub-ViewModels se quedan headless de verdad).
    private void OnAddCharacterFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc["dlg_pick_players_folder"] };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) ViewModel.Settings.AddCharacterFolder(dialog.FolderName);
    }

    private void OnAddWorldFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc["dlg_pick_worlds_folder"] };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) ViewModel.Settings.AddWorldFolder(dialog.FolderName);
    }

    // Pedido explicito del usuario (5-sep-2026): "guardar el tamaño de ventana actual... con un
    // tick que lo activa/desactiva". Solo la View conoce el Window real (Left/Top/Width/Height/
    // RestoreBounds) - SettingsViewModel.IsWindowSizePinned es solo el espejo que el CheckBox
    // muestra, actualizado aqui explicitamente tras Pin()/Unpin() en vez de via el binding
    // normal (evita que una futura OnIsWindowSizePinnedChanged en la ViewModel intente
    // persistir algo que no puede calcular sin el Window).
    //
    // `Click` de ToggleButton solo se dispara por interaccion real (raton o teclado), nunca por
    // un cambio de propiedad - y cuando llega, IsChecked ya trae el valor nuevo. Ver el
    // comentario completo original (bloque AJU-01 del arnes) en el historial de MainWindow.xaml.cs.
    private void OnPinWindowSizeClick(object sender, RoutedEventArgs e)
    {
        bool marcado = ((CheckBox)sender).IsChecked == true;
        var window = Window.GetWindow(this);
        if (marcado) WindowPlacementService.Pin(window);
        else WindowPlacementService.Unpin();
        ViewModel.Settings.IsWindowSizePinned = marcado;
    }
}
