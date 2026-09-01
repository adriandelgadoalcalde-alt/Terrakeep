using TerrasavrNative.Core.Nbt;

namespace TerrasavrNative.Core.Model;

// Representa el item en un slot del personaje, sea vanilla (Id = ItemID real de Terraria) o
// de Calamity (Id = CalamityIds.ItemIdBase + indice en el catalogo, ver Calamity/CalamityIds.cs)
// - un unico espacio de ids conceptual, igual que hace la capa JS de Terrasavr-Calamity-Beta.
public sealed class GameItem
{
    public required int Id { get; set; }
    public int Count { get; set; } = 1;
    public ItemPrefix Prefix { get; set; } = ItemPrefix.None;
    public bool Favorited { get; set; }

    // Solo relevante para items de Calamity: datos propios que el mod dueño gestiona por
    // item (ej. contadores de carga). Se preserva tal cual entre lecturas/escrituras mientras
    // el mismo item siga en el mismo slot - ver Calamity/CalamityItemCodec.cs.
    public NbtList? GlobalData { get; set; }

    public bool IsEmpty => Id == 0;
    public bool IsCalamity => Id >= Calamity.CalamityIds.ItemIdBase;

    public static GameItem Empty => new() { Id = 0, Count = 0 };
}
