namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo (vanilla o Calamity) para la Libreria/Buscador. IconPath
// es una URI pack://siteoforigin real para ambos (vanilla via VanillaIconResolver, extraidos
// de items.png; Calamity via su propio icono ya copiado) - null solo para el puñado de casos
// sin icono real (ver VanillaIconResolver), donde la UI cae a un "?" de texto.
public sealed class LibraryItemViewModel(string displayName, bool isCalamity, string? iconPath, int id, string category)
{
    public string DisplayName { get; } = displayName;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    public int Id { get; } = id;
    public string Category { get; } = category;
}
