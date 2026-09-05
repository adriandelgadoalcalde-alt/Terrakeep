using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// N-d: envoltorio real de WhatsNewEntry - los campos de solo texto pasan tal cual, Items se
// resuelve a WhatsNewItemViewModel (con IconPath real) en vez del WhatsNewItem crudo.
// Pedido explicito del usuario (2-sep-2026): "bugfixes" (antes ignorado en silencio por el
// deserializador, ver el comentario real en WhatsNewEntry) ahora se expone tambien.
public sealed class WhatsNewEntryViewModel
{
    public string Version { get; }
    public string DisplayDate { get; }
    public string DisplayNote { get; }
    public IReadOnlyList<WhatsNewChange> Changes { get; }
    public IReadOnlyList<WhatsNewChange> Bugfixes { get; }
    public IReadOnlyList<WhatsNewItemViewModel> Items { get; }

    private WhatsNewEntryViewModel(WhatsNewEntry entry, IReadOnlyList<WhatsNewItemViewModel> items)
    {
        Version = entry.Version;
        DisplayDate = entry.DisplayDate;
        DisplayNote = entry.DisplayNote;
        Changes = entry.Changes;
        Bugfixes = entry.Bugfixes;
        Items = items;
    }

    public static WhatsNewEntryViewModel ForVanilla(WhatsNewEntry entry, VanillaItemCatalog vanillaCatalog, ItemTooltipCatalogs catalogs) =>
        new(entry, entry.Items.Select(i => WhatsNewItemViewModel.ForVanilla(i, vanillaCatalog, catalogs)).ToList());

    public static WhatsNewEntryViewModel ForCalamity(WhatsNewEntry entry, CalamityCatalog calamityCatalog, ItemTooltipCatalogs catalogs) =>
        new(entry, entry.Items.Select(i => WhatsNewItemViewModel.ForCalamity(i, calamityCatalog, catalogs)).ToList());
}
