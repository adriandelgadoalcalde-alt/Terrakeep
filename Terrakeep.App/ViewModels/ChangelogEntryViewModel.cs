using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Ronda de idioma del 6-sep-2026: el XAML bindeaba DIRECTAMENTE el ChangelogEntry crudo de Core
// (Date/Summary/Added/Fixed), que solo existia en español - la pestaña "Acerca de" enseñaba el
// historial de versiones entero en español con la app en ingles. Este envoltorio elige el idioma
// activo y, a diferencia del objeto de datos, se entera de un cambio de idioma en caliente.
// Mismo patron real ya usado por WhatsNewEntryViewModel para las Novedades.
public sealed class ChangelogEntryViewModel : LocalizedContentViewModel
{
    private readonly ChangelogEntry _entry;

    public ChangelogEntryViewModel(ChangelogEntry entry) => _entry = entry;

    public string Version => _entry.Version;
    public string Date => _entry.DateFor(Idioma);
    public string Summary => _entry.SummaryFor(Idioma);
    public IReadOnlyList<string> Added => _entry.AddedFor(Idioma);
    public IReadOnlyList<string> Fixed => _entry.FixedFor(Idioma);

    protected override void RefrescarTextos()
    {
        Avisar(nameof(Date));
        Avisar(nameof(Summary));
        Avisar(nameof(Added));
        Avisar(nameof(Fixed));
    }
}
