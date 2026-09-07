using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Una etapa completa (Pre-Hardmode/Hardmode temprano/Endgame) ya resuelta para mostrar.
// Ronda de idioma del 6-sep-2026: recibia la etiqueta ya resuelta como string (siempre la
// española, la unica que existia en builds.json) - ahora guarda la etapa real y elige el idioma
// activo, reevaluandola si se cambia de idioma en caliente. La suscripcion va por evento DEBIL
// (PropertyChangedEventManager) por el mismo motivo real explicado en LocalizedContentViewModel:
// el servicio de idioma es un singleton que vive lo que la aplicacion.
public sealed partial class BuildStageViewModel : ObservableObject
{
    private readonly BuildStage _stage;

    public BuildStageViewModel(BuildStage stage, List<BuildClassGearViewModel> classes)
    {
        _stage = stage;
        Classes = classes;
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    public string Label => _stage.LabelFor(LocalizationService.Instance.Language);
    public List<BuildClassGearViewModel> Classes { get; }

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Label));

    // Bd-d (segunda auditoria de Opus, Fable): la etapa entera se oculta cuando el filtro de
    // clase deja sus clases todas invisibles - evita un titulo de etapa "flotando" sobre un
    // WrapPanel vacio.
    [ObservableProperty] private bool _isVisible = true;
}
