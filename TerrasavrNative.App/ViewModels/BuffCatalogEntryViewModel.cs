namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo de buffs (vanilla o Calamity) para el buscador de
// "Añadir buff" - mismo patron que LibraryItemViewModel para objetos.
public sealed class BuffCatalogEntryViewModel(string displayName, int id, bool isCalamity, string? iconPath, string? description)
{
    public string DisplayName { get; } = displayName;
    public int Id { get; } = id;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    // Descripcion real (vanilla en español, Calamity en ingles - ver VanillaBuffCatalog/
    // CalamityBuffCatalog) - null si de verdad no hay ninguna.
    public string? Description { get; } = description;
}
