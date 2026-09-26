using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/017 (26-sep-2026): primera extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de WhatsNewView.xaml. Sin
// code-behind propio (la seccion NOVEDADES nunca tuvo logica en MainWindow.xaml.cs), este
// constructor es deliberadamente el minimo posible.
public partial class WhatsNewView : UserControl
{
    public WhatsNewView()
    {
        InitializeComponent();
    }
}
