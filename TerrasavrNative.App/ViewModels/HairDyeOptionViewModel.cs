namespace TerrasavrNative.App.ViewModels;

// Una entrada del selector visual de tinte de pelo (Apariencia) - a diferencia del peinado no
// hace falta renderizar nada, el tinte se elige por el objeto real que lo aplica (sprite +
// nombre reales), ver AppearanceViewModel/HairDyeCatalog.
public sealed class HairDyeOptionViewModel(int index, string displayName, string? iconPath)
{
    // Valor real que se guarda en PlrCharacter.HairDye (0 = Ninguno).
    public int Index { get; } = index;
    public string DisplayName { get; } = displayName;
    public string? IconPath { get; } = iconPath;
}
