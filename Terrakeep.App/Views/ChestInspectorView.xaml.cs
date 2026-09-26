using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/028 (26-sep-2026): duodecima extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de ChestInspectorView.xaml. Unico
// code-behind real de este sub-modo (confirmado por grep: OnChestItemSlotMouseDown es el unico
// call-site real de todo el archivo), movido tal cual - mismo mecanismo de WorldToolsView/ADR-027:
// `_viewModel` de MainWindow -> `ViewModel` ((MainViewModel)DataContext, este control hereda el
// MainViewModel real del Window que lo hospeda sin fijar su propio DataContext).
public partial class ChestInspectorView : UserControl
{
    public ChestInspectorView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // Editor de cofres v1 (T1, 15-sep-2026): gemelo de OnItemSlotMouseDown de MainWindow.xaml.cs,
    // para la rejilla de edicion de un cofre de Exploracion - a proposito NO llama a
    // ViewModel.SelectSlot (eso pisaria el panel "Editar" de Personaje, en otra pestaña), sino a
    // Exploration.SelectChestSlot (su propia seleccion independiente). Sin arrastre/teclado
    // todavia (esta rejilla es AllowDrop="False" y sin KeyDown en el XAML) - alcance minimo real,
    // "anadir/cambiar" ya funciona entero via el campo "Indice" del panel Editar reutilizado.
    private void OnChestItemSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ItemSlotViewModel slot })
            ViewModel.Exploration.SelectChestSlot(slot);
    }

    // Duplicado local NECESARIO (mismo motivo que ChestInspectorItemEditTemplate/
    // ChestInspectorBoolToVis en el XAML, ver ADR-TERRAKEEP-028): OnCommitTextOnEnter real de
    // MainWindow.xaml.cs (T-17), usado por 2 TextBox dentro de ChestInspectorItemEditTemplate
    // (Indice/Prefijo). MainWindow.xaml.cs conserva su propia copia porque ItemEditTemplate
    // (Objetos, sin extraer todavia) tambien la usa - handler trivial y sin estado, sin
    // dependencia de ViewModel, cero riesgo real de divergencia entre las dos copias.
    private void OnCommitTextOnEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox) return;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
}
