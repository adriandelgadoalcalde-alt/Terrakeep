namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo de buffs (vanilla o Calamity) para el buscador de
// "Añadir buff" - mismo patron que LibraryItemViewModel para objetos.
public sealed class BuffCatalogEntryViewModel(string displayName, int id, bool isCalamity, string? iconPath, string? description, bool isDebuff = false)
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string DisplayName { get; } = displayName;
    public int Id { get; } = id;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    // Descripcion real (vanilla en español, Calamity en ingles - ver VanillaBuffCatalog/
    // CalamityBuffCatalog) - null si de verdad no hay ninguna.
    public string? Description { get; } = description;
    // C-09 (informe de pulido final, cierra L2): plegado (minusculas + sin diacriticos) UNA
    // sola vez aqui - gemelo real de LibraryItemViewModel.NameFolded/TooltipFolded.
    public string NameFolded { get; } = LibrarySearchGrammar.Fold(displayName);
    public string? DescriptionFolded { get; } = description is null ? null : LibrarySearchGrammar.Fold(description);
    // H6-12 (sexta auditoria de Opus): real, de CalamityBuffEntry.IsDebuff (Main.debuff[]
    // real del propio ModBuff) - siempre false para vanilla en esta pasada (fuera de alcance,
    // el usuario pidio esto especificamente para Calamity; ver bitacora.md).
    public bool IsDebuff { get; } = isDebuff;
}
