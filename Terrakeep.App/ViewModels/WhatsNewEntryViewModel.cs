using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// N-d: envoltorio real de WhatsNewEntry - los campos de solo texto pasan tal cual, Items se
// resuelve a WhatsNewItemViewModel (con IconPath real) en vez del WhatsNewItem crudo.
// Pedido explicito del usuario (2-sep-2026): "bugfixes" (antes ignorado en silencio por el
// deserializador, ver el comentario real en WhatsNewEntry) ahora se expone tambien.
// Ronda de idioma del 6-sep-2026: DisplayDate/DisplayNote se fijaban en el constructor con la
// version ESPAÑOLA fija (WhatsNewEntry.DisplayDate/DisplayNote ignoran el idioma) y Changes/
// Bugfixes exponian el objeto de datos crudo - los tres se quedaban en español con la app en
// ingles. Ahora se resuelven por idioma activo y reaccionan a un cambio en caliente.
public sealed class WhatsNewEntryViewModel : LocalizedContentViewModel
{
    private readonly WhatsNewEntry _entry;

    public string Version => _entry.Version;
    public string DisplayDate => _entry.DateFor(Idioma);
    public string DisplayNote => _entry.NoteFor(Idioma);
    public IReadOnlyList<WhatsNewChangeViewModel> Changes { get; }
    public IReadOnlyList<WhatsNewChangeViewModel> Bugfixes { get; }
    public IReadOnlyList<WhatsNewItemViewModel> Items { get; }

    private WhatsNewEntryViewModel(WhatsNewEntry entry, IReadOnlyList<WhatsNewItemViewModel> items)
    {
        _entry = entry;
        Changes = entry.Changes.Select(c => new WhatsNewChangeViewModel(c)).ToList();
        Bugfixes = entry.Bugfixes.Select(c => new WhatsNewChangeViewModel(c)).ToList();
        Items = items;
    }

    protected override void RefrescarTextos()
    {
        Avisar(nameof(DisplayDate));
        Avisar(nameof(DisplayNote));
    }

    public static WhatsNewEntryViewModel ForVanilla(WhatsNewEntry entry, VanillaItemCatalog vanillaCatalog, WhatsNewItemIdCatalog whatsNewIds, ItemTooltipCatalogs catalogs) =>
        new(entry, entry.Items.Select(i => WhatsNewItemViewModel.ForVanilla(i, vanillaCatalog, whatsNewIds, catalogs)).ToList());

    public static WhatsNewEntryViewModel ForCalamity(WhatsNewEntry entry, CalamityCatalog calamityCatalog, ItemTooltipCatalogs catalogs) =>
        new(entry, entry.Items.Select(i => WhatsNewItemViewModel.ForCalamity(i, calamityCatalog, catalogs)).ToList());
}
