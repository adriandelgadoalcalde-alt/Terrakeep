using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/029 (26-sep-2026): decimotercera extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de BrowseView.xaml. Este control no
// fija su propio DataContext (hereda el MainViewModel real del Window que lo hospeda).
//
// CASO NUEVO real (a diferencia de los 5 handlers de HomeView/ADR-026, que se pudieron MOVER
// enteros porque no se usaban fuera de su seccion): el atajo de teclado Ctrl+Alt+F de
// OnWindowKeyDown (MainWindow.xaml.cs) es un gesto de nivel Window que necesita ENFOCAR
// WorldSearchBox, que ahora vive dentro de este UserControl (NameScope propio, no accesible
// directamente como campo desde MainWindow.xaml.cs). FocusWorldSearchBox() es el gancho publico
// minimo que MainWindow.xaml.cs invoca via BrowseView.FocusWorldSearchBox() (BrowseView es el
// x:Name del host, campo generado por su propio InitializeComponent()) - mismo contenido exacto
// que el original (WorldSearchBox.Focus(); WorldSearchBox.SelectAll();), solo movido de sitio.
public partial class BrowseView : UserControl
{
    public BrowseView()
    {
        InitializeComponent();
    }

    public void FocusWorldSearchBox()
    {
        WorldSearchBox.Focus();
        WorldSearchBox.SelectAll();
    }
}
