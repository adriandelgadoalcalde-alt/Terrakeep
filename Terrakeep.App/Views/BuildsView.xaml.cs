using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/018 (26-sep-2026): segunda extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de BuildsView.xaml. Sin
// code-behind propio (la seccion BUILDS nunca tuvo logica en MainWindow.xaml.cs), este
// constructor es deliberadamente el minimo posible.
public partial class BuildsView : UserControl
{
    public BuildsView()
    {
        InitializeComponent();
    }
}
