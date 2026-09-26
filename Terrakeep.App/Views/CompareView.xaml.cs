using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/025 (26-sep-2026): novena extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de CompareView.xaml. Sin
// code-behind propio (el sub-tab Compare nunca tuvo logica en MainWindow.xaml.cs, todo son
// Bindings/Commands), este constructor es deliberadamente el minimo posible.
public partial class CompareView : UserControl
{
    public CompareView()
    {
        InitializeComponent();
    }
}
