using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Novedades" - registro por version, mas reciente primero (whats_new.json ya viene asi
// ordenado). N-d (segunda auditoria de Opus, Fable): Entries envuelve cada WhatsNewEntry en
// WhatsNewEntryViewModel para que "Objetos nuevos" resuelva sprites reales en vez de solo texto.
public sealed class WhatsNewViewModel
{
    public IReadOnlyList<WhatsNewEntryViewModel> Entries { get; }

    public WhatsNewViewModel(WhatsNewCatalog catalog, VanillaItemCatalog vanillaCatalog)
    {
        Entries = catalog.Entries.Select(e => new WhatsNewEntryViewModel(e, vanillaCatalog)).ToList();
    }
}
