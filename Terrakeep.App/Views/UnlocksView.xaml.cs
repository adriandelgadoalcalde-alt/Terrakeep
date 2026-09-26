using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/024 (26-sep-2026): octava extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de UnlocksView.xaml. Sin
// code-behind propio (el sub-tab Unlocks/Desbloqueos nunca tuvo logica en
// MainWindow.xaml.cs, todo son Bindings/Commands), este constructor es deliberadamente el
// minimo posible.
public partial class UnlocksView : UserControl
{
    public UnlocksView()
    {
        InitializeComponent();
    }
}
