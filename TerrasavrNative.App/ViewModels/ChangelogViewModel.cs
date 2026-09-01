using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Changelog del propio EDITOR (Terrakeep) - distinto de WhatsNewViewModel, que es del juego/
// Calamity. Ver "Acerca de".
public sealed class ChangelogViewModel(ChangelogCatalog catalog)
{
    public IReadOnlyList<ChangelogEntry> Entries { get; } = catalog.Entries;
}
