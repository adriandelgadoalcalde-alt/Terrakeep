using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Novedades" - registro por version, mas reciente primero (los .json ya vienen asi
// ordenados). N-d (segunda auditoria de Opus, Fable): Entries envuelve cada WhatsNewEntry en
// WhatsNewEntryViewModel para que "Objetos nuevos" resuelva sprites reales en vez de solo texto.
// Pedido explicito del usuario (2-sep-2026): "dos pestañas, una con las novedades de Terraria
// vanilla y otra de tModLoader y Calamity Mod" - VanillaEntries/CalamityEntries vienen de DOS
// catalogos/ficheros reales SEPARADOS (whats_new_vanilla.json/whats_new_calamity.json), nunca
// mezclados - el propio juego base y el mod son cosas distintas con ritmos de version propios.
public sealed class WhatsNewViewModel
{
    public IReadOnlyList<WhatsNewEntryViewModel> VanillaEntries { get; }
    public IReadOnlyList<WhatsNewEntryViewModel> CalamityEntries { get; }

    public WhatsNewViewModel(WhatsNewCatalog vanillaCatalog, WhatsNewCatalog calamityCatalog,
        VanillaItemCatalog vanillaItemCatalog, CalamityCatalog calamityItemCatalog, ItemTooltipCatalogs catalogs)
    {
        VanillaEntries = vanillaCatalog.Entries.Select(e => WhatsNewEntryViewModel.ForVanilla(e, vanillaItemCatalog, catalogs)).ToList();
        CalamityEntries = calamityCatalog.Entries.Select(e => WhatsNewEntryViewModel.ForCalamity(e, calamityItemCatalog, catalogs)).ToList();
    }
}
