using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Un boton "meta" del selector de prefijo (Biblioteca/Positivos/Negativos, ver
// PrefixGroupCatalog.Metas) - mismos 3 botones btMeta reales de app.TabEdit.
//
// Bug real de idioma encontrado el 15-sep-2026 (ronda de re-verificacion de los 21 FALLO,
// mismo hallazgo y mismo arreglo que PrefixGroupButtonViewModel - ver su comentario para el
// detalle completo): Label se fijaba una sola vez a meta.NameEs, ignorando NameEn y sin
// refresco en vivo al cambiar idioma.
public partial class PrefixMetaButtonViewModel : ObservableObject
{
    public PrefixMeta Meta { get; }

    [ObservableProperty] private string _label;
    [ObservableProperty] private bool _isSelected;

    public PrefixMetaButtonViewModel(PrefixMeta meta)
    {
        Meta = meta;
        _label = LocalizedContent.Pick(meta.NameEs, meta.NameEn);
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => Label = LocalizedContent.Pick(Meta.NameEs, Meta.NameEn);
}
