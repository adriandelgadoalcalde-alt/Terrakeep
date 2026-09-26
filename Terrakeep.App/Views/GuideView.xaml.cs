using System.Windows.Controls;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/021 (26-sep-2026): quinta extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de GuideView.xaml. Sin
// code-behind propio (la seccion GUIA nunca tuvo logica en MainWindow.xaml.cs), este
// constructor es deliberadamente el minimo posible. El x:Name="GuideObjetivoBanner" que vive
// dentro de este XAML no necesita ningun manejo especial aqui - ver GuideView.xaml para el
// mecanismo real (window.FindName("GuideView") + GuideView.FindName("GuideObjetivoBanner")
// desde el test que lo usa).
public partial class GuideView : UserControl
{
    public GuideView()
    {
        InitializeComponent();
    }
}
