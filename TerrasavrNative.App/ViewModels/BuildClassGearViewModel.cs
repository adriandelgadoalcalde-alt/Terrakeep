using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Equipo recomendado de una clase dentro de una etapa, ya resuelto para mostrar (iconos +
// nombres). Source conserva el BuildClassGear (Core) original tal cual, para que el boton
// "Auto-equipar" siga pudiendolo pasar directamente a MainViewModel.AutoEquipCommand sin
// tener que reconstruirlo desde las filas ya resueltas.
public sealed partial class BuildClassGearViewModel(
    string className,
    List<BuildItemRowViewModel> armor,
    List<BuildItemRowViewModel> weapons,
    List<BuildItemRowViewModel> accessories,
    BuildClassGear source) : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string ClassName { get; } = className;
    public List<BuildItemRowViewModel> Armor { get; } = armor;
    public List<BuildItemRowViewModel> Weapons { get; } = weapons;
    public List<BuildItemRowViewModel> Accessories { get; } = accessories;
    public BuildClassGear Source { get; } = source;

    // Bd-d (segunda auditoria de Opus, Fable): "buscador/filtro por clase" - la pestaña Builds
    // era una unica lista plana de TODAS las clases de TODAS las etapas, sin forma de ver solo
    // "melee" por ejemplo. Ver BuildsViewModel.SetClassFilter.
    [ObservableProperty] private bool _isVisible = true;

    public IEnumerable<BuildItemRowViewModel> AllRows => Armor.Concat(Weapons).Concat(Accessories);
}
