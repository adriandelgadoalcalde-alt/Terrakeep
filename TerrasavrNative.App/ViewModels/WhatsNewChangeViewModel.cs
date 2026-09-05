using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Ronda de idioma del 6-sep-2026: una linea de "cambios"/"arreglos" de Novedades. El JSON YA
// traia el texto ingles de cada linea desde el primer dia (campo "en"), pero el XAML bindeaba
// WhatsNewChange.DisplayText, que devuelve SIEMPRE el español - la pestaña entera se quedaba sin
// traducir sin que faltara ni un dato. Este envoltorio elige el idioma activo y reacciona a un
// cambio en caliente.
public sealed class WhatsNewChangeViewModel : LocalizedContentViewModel
{
    private readonly WhatsNewChange _change;

    public WhatsNewChangeViewModel(WhatsNewChange change) => _change = change;

    public string Text => _change.TextFor(Idioma);

    protected override void RefrescarTextos() => Avisar(nameof(Text));
}
