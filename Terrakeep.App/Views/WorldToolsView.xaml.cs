using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/027 (26-sep-2026): undecima extraccion real de una seccion de
// MainWindow.xaml a un UserControl - ver el comentario real de WorldToolsView.xaml. Unico
// code-behind real de este sub-modo (confirmado por grep: OnSaveWorldReportClick es el unico
// call-site de todo el archivo), movido tal cual - mismo mecanismo de AboutView/ADR-020 y
// HomeView/ADR-026: `this` (Window) -> Window.GetWindow(this), `_viewModel` de MainWindow ->
// (MainViewModel)DataContext (este control hereda el MainViewModel real del Window sin fijar
// su propio DataContext).
public partial class WorldToolsView : UserControl
{
    private static LocalizationService Loc => LocalizationService.Instance;

    public WorldToolsView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // F-14 (auditoria de Opus vs TEdit, E-16/E-17): dialogo real en la View (mismo criterio que
    // SaveItemSetDialog/ExportMapToPng) - BuildWorldReportText solo compone el texto.
    private void OnSaveWorldReportClick(object sender, RoutedEventArgs e)
    {
        string texto = ViewModel.Exploration.BuildWorldReportText();
        if (string.IsNullOrEmpty(texto)) return;
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_save_world_report"],
            Filter = Loc["dlg_filter_text"],
            FileName = $"{ViewModel.Exploration.WorldTitle}-informe.txt",
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) File.WriteAllText(dialog.FileName, texto);
    }
}
