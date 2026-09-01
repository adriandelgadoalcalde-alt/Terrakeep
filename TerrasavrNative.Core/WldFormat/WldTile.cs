namespace TerrasavrNative.Core.WldFormat;

// Un tile decodificado - solo los campos que hacen falta para pintar el mapa (tipo/pared/
// liquido). u/v (posicion dentro del sprite, para nombres de variante en un futuro tooltip) se
// leen igualmente durante el RLE para no desalinear el resto del archivo, pero no se
// almacenan todavia - ver WldTileReader.
public readonly struct WldTile(short type, short wall, byte liquidType, byte liquidAmount)
{
    // -1 si el tile no esta activo (aire/vacio).
    public short Type { get; } = type;
    public short Wall { get; } = wall;
    public byte LiquidType { get; } = liquidType;
    public byte LiquidAmount { get; } = liquidAmount;

    public bool IsActive => Type >= 0;

    public static readonly WldTile Empty = new(-1, 0, 0, 0);
}
