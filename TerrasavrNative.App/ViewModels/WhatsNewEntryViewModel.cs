using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// N-d: envoltorio real de WhatsNewEntry - los campos de solo texto pasan tal cual, Items se
// resuelve a WhatsNewItemViewModel (con IconPath real) en vez del WhatsNewItem crudo.
public sealed class WhatsNewEntryViewModel(WhatsNewEntry entry, VanillaItemCatalog vanillaCatalog)
{
    public string Version { get; } = entry.Version;
    public string DisplayDate { get; } = entry.DisplayDate;
    public string DisplayNote { get; } = entry.DisplayNote;
    public IReadOnlyList<WhatsNewChange> Changes { get; } = entry.Changes;
    public IReadOnlyList<WhatsNewItemViewModel> Items { get; } =
        entry.Items.Select(i => new WhatsNewItemViewModel(i, vanillaCatalog)).ToList();
}
