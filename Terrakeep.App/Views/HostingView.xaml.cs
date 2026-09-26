using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/019 (26-sep-2026): tercera extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de HostingView.xaml. Sin
// code-behind propio (la seccion SERVIDOR nunca tuvo logica en MainWindow.xaml.cs), este
// constructor es deliberadamente el minimo posible.
public partial class HostingView : UserControl
{
    public HostingView()
    {
        InitializeComponent();
    }
}
