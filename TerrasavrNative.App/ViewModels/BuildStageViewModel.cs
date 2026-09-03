using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Una etapa completa (Pre-Hardmode/Hardmode temprano/Endgame) ya resuelta para mostrar.
public sealed partial class BuildStageViewModel(string label, List<BuildClassGearViewModel> classes) : ObservableObject
{
    public string Label { get; } = label;
    public List<BuildClassGearViewModel> Classes { get; } = classes;

    // Bd-d (segunda auditoria de Opus, Fable): la etapa entera se oculta cuando el filtro de
    // clase deja sus clases todas invisibles - evita un titulo de etapa "flotando" sobre un
    // WrapPanel vacio.
    [ObservableProperty] private bool _isVisible = true;
}
