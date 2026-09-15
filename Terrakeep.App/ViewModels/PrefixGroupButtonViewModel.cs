using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Un grupo dentro de una meta (ej. "Cuerpo a cuerpo +") ya filtrado por lo que de verdad es
// legal para el objeto seleccionado (ver PrefixGroupCatalog.GroupsFor) - btGroups reales.
//
// Bug real de idioma encontrado el 15-sep-2026 (ronda de re-verificacion de los 21 FALLO,
// detectado por A10-IDIOMA-BARRIDO con el picker de prefijo real abierto): Label se fijaba UNA
// SOLA VEZ en el constructor a group.NameEs, ignorando NameEn (que SI existia en el catalogo,
// PrefixGroupCatalog.cs, sin usar) y sin reaccionar nunca a un cambio de idioma en caliente -
// el picker de prefijo (Biblioteca/Positivos/Negativos y sus grupos, "Cuerpo a cuerpo +"/
// "A distancia +"/"Magia +"/...) se quedaba en español para siempre, con la app en cualquier
// idioma. Mismo patron ya usado por LibraryItemViewModel.DisplayName (LocalizedContent.Pick) +
// el mismo mecanismo de refresco en vivo (PropertyChangedEventManager sobre
// LocalizationService.Instance, evento debil, con un metodo con nombre - no una lambda, que se
// recolectaria enseguida) que ya usan LibraryViewModel/AppearanceViewModel.
public partial class PrefixGroupButtonViewModel : ObservableObject
{
    public PrefixGroup Group { get; }

    [ObservableProperty] private string _label;
    [ObservableProperty] private bool _isSelected;

    public PrefixGroupButtonViewModel(PrefixGroup group)
    {
        Group = group;
        _label = LocalizedContent.Pick(group.NameEs, group.NameEn);
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => Label = LocalizedContent.Pick(Group.NameEs, Group.NameEn);
}
