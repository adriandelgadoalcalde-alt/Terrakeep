using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Novedades" - registro por version, mas reciente primero (whats_new.json ya viene asi
// ordenado).
public sealed class WhatsNewViewModel(WhatsNewCatalog catalog)
{
    public IReadOnlyList<WhatsNewEntry> Entries { get; } = catalog.Entries;
}
