namespace TerrasavrNative.Core.WldFormat;

// Un tile decodificado - tipo/pared/liquido para pintar el mapa, mas u/v (posicion dentro del
// sprite) para resolver el nombre EXACTO de variante en el tooltip de Exploracion (ej.
// distinguir un cofre de oro de uno de la jungla, mismo id de tile - ver
// TileNameCatalog.TileVariantName). u/v solo tiene sentido si IsActive - vale (0,0) si no.
public readonly struct WldTile(short type, short wall, byte liquidType, byte liquidAmount, short u, short v)
{
    // -1 si el tile no esta activo (aire/vacio).
    public short Type { get; } = type;
    public short Wall { get; } = wall;
    public byte LiquidType { get; } = liquidType;
    public byte LiquidAmount { get; } = liquidAmount;
    public short U { get; } = u;
    public short V { get; } = v;

    public bool IsActive => Type >= 0;

    public static readonly WldTile Empty = new(-1, 0, 0, 0, 0, 0);
}
