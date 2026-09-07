using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Changelog del propio EDITOR (Terrakeep) - distinto de WhatsNewViewModel, que es del juego/
// Calamity. Ver "Acerca de".
// Ronda de idioma del 6-sep-2026: Entries pasa a envolver cada ChangelogEntry en
// ChangelogEntryViewModel (antes exponia el objeto de datos crudo, siempre en español).
public sealed class ChangelogViewModel(ChangelogCatalog catalog)
{
    public IReadOnlyList<ChangelogEntryViewModel> Entries { get; } =
        catalog.Entries.Select(e => new ChangelogEntryViewModel(e)).ToList();
}
