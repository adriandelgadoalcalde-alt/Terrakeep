namespace Terrakeep.Core.PlrFormat;

// Un loadout (P.prototype.handle en script.js real). loadouts[0] es un mirror interno del
// equipo "actualmente puesto" (this.primary=true, sin campo Hide); loadouts[1..3] son los 3
// loadouts reales del juego (this.primary=false, con Hide[10] al final - visibilidad de cada
// slot en el panel de vanidad). Bajo la linea base de esta app (version>=145, ver
// PlrBodySerializer) siempre son 10 items + 10 social + 10 dyes.
public sealed class PlrLoadout
{
    public PlrItemSlot[] Items { get; init; } = new PlrItemSlot[10];
    public PlrItemSlot[] Social { get; init; } = new PlrItemSlot[10];
    public PlrItemSlot[] Dyes { get; init; } = new PlrItemSlot[10];

    // null para loadouts[0] (el mirror); 10 elementos para loadouts[1..3].
    public bool[]? Hide { get; init; }

    public static PlrLoadout CreateEmpty(bool isPrimary) => new()
    {
        Items = CreateEmptySlots(10),
        Social = CreateEmptySlots(10),
        Dyes = CreateEmptySlots(10),
        Hide = isPrimary ? null : new bool[10],
    };

    private static PlrItemSlot[] CreateEmptySlots(int count)
    {
        var slots = new PlrItemSlot[count];
        Array.Fill(slots, PlrItemSlot.Empty);
        return slots;
    }
}
