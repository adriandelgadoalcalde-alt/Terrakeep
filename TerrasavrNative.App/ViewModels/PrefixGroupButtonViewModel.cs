using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Un grupo dentro de una meta (ej. "Cuerpo a cuerpo +") ya filtrado por lo que de verdad es
// legal para el objeto seleccionado (ver PrefixGroupCatalog.GroupsFor) - btGroups reales.
public partial class PrefixGroupButtonViewModel(PrefixGroup group) : ObservableObject
{
    public PrefixGroup Group { get; } = group;
    public string Label { get; } = group.NameEs;

    [ObservableProperty] private bool _isSelected;
}
