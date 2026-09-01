namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo (vanilla o Calamity) para la Libreria/Buscador. IconPath
// es una URI pack://siteoforigin real solo para Calamity (los iconos vanilla vienen en un
// atlas de sprites sin extraer todavia, ver bitacora.md) - null ahi, la UI cae a texto.
public sealed class LibraryItemViewModel(string displayName, bool isCalamity, string? iconPath, int id, string category)
{
    public string DisplayName { get; } = displayName;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    public int Id { get; } = id;
    public string Category { get; } = category;
}
