namespace TerrasavrNative.App.ViewModels;

// Una etapa completa (Pre-Hardmode/Hardmode temprano/Endgame) ya resuelta para mostrar.
public sealed class BuildStageViewModel(string label, List<BuildClassGearViewModel> classes)
{
    public string Label { get; } = label;
    public List<BuildClassGearViewModel> Classes { get; } = classes;
}
