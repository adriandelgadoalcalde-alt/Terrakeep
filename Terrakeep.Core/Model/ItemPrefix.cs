namespace Terrakeep.Core.Model;

// Representa el prefijo de un item: o bien un PrefixID vanilla plano (byte, 0 = ninguno), o
// bien un id sintetico de un prefijo REAL de Calamity (>= CalamityIds.PrefixIdBase, resuelto
// contra RoguePrefixCatalog al leer/escribir el .tplr) - nunca los dos a la vez, igual que en
// el NBT real (prefix byte y modPrefixMod/modPrefixName son mutuamente excluyentes).
public readonly struct ItemPrefix : IEquatable<ItemPrefix>
{
    private readonly byte _vanillaId;
    private readonly int _syntheticId; // 0 = no aplica

    private ItemPrefix(byte vanillaId, int syntheticId)
    {
        _vanillaId = vanillaId;
        _syntheticId = syntheticId;
    }

    public static readonly ItemPrefix None = new(0, 0);
    public static ItemPrefix Vanilla(byte id) => new(id, 0);
    public static ItemPrefix CalamitySynthetic(int syntheticId) => new(0, syntheticId);

    public bool IsNone => _vanillaId == 0 && _syntheticId == 0;
    public bool IsCalamity => _syntheticId != 0;
    public byte VanillaId => _vanillaId;
    public int SyntheticId => _syntheticId;

    public bool Equals(ItemPrefix other) => _vanillaId == other._vanillaId && _syntheticId == other._syntheticId;
    public override bool Equals(object? obj) => obj is ItemPrefix p && Equals(p);
    public override int GetHashCode() => HashCode.Combine(_vanillaId, _syntheticId);

    // KeepQA (16-sep-2026, paso 1 de analisis estatico): CA2231 real - IEquatable<T> ya estaba
    // implementado pero faltaban los operadores == / !=, asi que "a == b" sobre dos ItemPrefix
    // ni siquiera compilaba (el struct no es un record, no los genera solo). Añadido sin tocar
    // Equals/GetHashCode, que ya eran correctos.
    public static bool operator ==(ItemPrefix left, ItemPrefix right) => left.Equals(right);
    public static bool operator !=(ItemPrefix left, ItemPrefix right) => !left.Equals(right);
}
