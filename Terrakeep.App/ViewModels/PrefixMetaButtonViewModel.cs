using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Un boton "meta" del selector de prefijo (Biblioteca/Positivos/Negativos, ver
// PrefixGroupCatalog.Metas) - mismos 3 botones btMeta reales de app.TabEdit.
public partial class PrefixMetaButtonViewModel(PrefixMeta meta) : ObservableObject
{
    public PrefixMeta Meta { get; } = meta;
    public string Label { get; } = meta.NameEs;

    [ObservableProperty] private bool _isSelected;
}
