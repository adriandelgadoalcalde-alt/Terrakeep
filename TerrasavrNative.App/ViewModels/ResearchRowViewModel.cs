namespace TerrasavrNative.App.ViewModels;

// Una entrada de investigacion (Modo Viaje) ya resuelta a nombre legible - el PID crudo
// (Terraria.ModLoader.ModType.FullName para modded, nombre interno plano para vanilla, nunca
// lleva "/" en ese caso) no se muestra directamente.
public sealed class ResearchRowViewModel(string displayName, int count, bool isCalamity)
{
    public string DisplayName { get; } = displayName;
    public int Count { get; } = count;
    public bool IsCalamity { get; } = isCalamity;
}
