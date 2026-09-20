using System.ComponentModel;
using System.Linq;
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

    // Bug visual real reportado por el usuario con captura (21-sep-2026): al filtrar a una sola
    // clase, la columna quedaba pegada a la izquierda con hueco muerto a la derecha. Causa real
    // medida con el arnes (BUILDS_CENTER_SOLO): el WrapPanel de columnas tiene ItemWidth=248 FIJO
    // (a proposito, para que las columnas queden alineadas entre filas - ver BuildClassTemplate) -
    // y el algoritmo real de WrapPanel (WrapPanel.MeasureOverride, System.Windows.Controls) usa
    // ese ItemWidth fijo para CADA hijo de la coleccion, incluidos los ocultos por
    // Visibility="Collapsed": un Collapsed mide (0,0) de verdad, pero WrapPanel IGNORA esa medida
    // real cuando ItemWidth esta fijado y reserva igualmente el hueco de 248px por cada clase
    // oculta. Con 4 clases reales y solo 1 visible, el WrapPanel seguia ocupando 4×248=992px
    // (confirmado con TransformToAncestor real), asi que centrar solo el ItemsControl entero
    // (HorizontalAlignment="Center") centraba esa caja de 992px pero la unica tarjeta visible de
    // verdad quedaba descentrada DENTRO de ella. Arreglo real: la vista ya no se une a la lista
    // completa con Visibility por item, se une a esta coleccion YA FILTRADA - asi el WrapPanel
    // nunca ve ni mide los elementos ocultos, ItemWidth=248 solo reserva hueco por columna
    // REALMENTE visible, y HorizontalAlignment="Center" en el ItemsControl (XAML) si centra de
    // verdad lo que se ve.
    public IEnumerable<BuildClassGearViewModel> VisibleClasses => Classes.Where(c => c.IsVisible);

    public void NotifyVisibleClassesChanged() => OnPropertyChanged(nameof(VisibleClasses));

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Label));

    // Bd-d (segunda auditoria de Opus, Fable): la etapa entera se oculta cuando el filtro de
    // clase deja sus clases todas invisibles - evita un titulo de etapa "flotando" sobre un
    // WrapPanel vacio.
    [ObservableProperty] private bool _isVisible = true;
}
