using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/023 (26-sep-2026): septima extraccion real de una seccion de
// MainWindow.xaml a un UserControl, segunda del grupo de sub-tabs de PERSONAJE - ver el
// comentario real de VersionView.xaml. Sin code-behind propio (el sub-tab Version nunca tuvo
// logica en MainWindow.xaml.cs, todo son Bindings/Commands), este constructor es
// deliberadamente el minimo posible.
public partial class VersionView : UserControl
{
    public VersionView()
    {
        InitializeComponent();
    }
}
