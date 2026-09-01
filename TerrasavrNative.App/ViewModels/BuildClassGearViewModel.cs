using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Equipo recomendado de una clase dentro de una etapa, ya resuelto para mostrar (iconos +
// nombres). Source conserva el BuildClassGear (Core) original tal cual, para que el boton
// "Auto-equipar" siga pudiendolo pasar directamente a MainViewModel.AutoEquipCommand sin
// tener que reconstruirlo desde las filas ya resueltas.
public sealed class BuildClassGearViewModel(
    string className,
    List<BuildItemRowViewModel> armor,
    List<BuildItemRowViewModel> weapons,
    List<BuildItemRowViewModel> accessories,
    BuildClassGear source)
{
    public string ClassName { get; } = className;
    public List<BuildItemRowViewModel> Armor { get; } = armor;
    public List<BuildItemRowViewModel> Weapons { get; } = weapons;
    public List<BuildItemRowViewModel> Accessories { get; } = accessories;
    public BuildClassGear Source { get; } = source;
}
