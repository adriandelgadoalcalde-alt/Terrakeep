using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/022 (26-sep-2026): sexta extraccion real de una seccion de
// MainWindow.xaml a un UserControl, primera del grupo de sub-tabs de PERSONAJE - ver el
// comentario real de SpawnpointsView.xaml. Sin code-behind propio (el sub-tab Spawn Points
// nunca tuvo logica en MainWindow.xaml.cs, todo son Bindings/Commands), este constructor es
// deliberadamente el minimo posible.
public partial class SpawnpointsView : UserControl
{
    public SpawnpointsView()
    {
        InitializeComponent();
    }
}
