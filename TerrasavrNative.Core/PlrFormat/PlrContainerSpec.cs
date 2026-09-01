namespace TerrasavrNative.Core.PlrFormat;

// Especificacion de cada contenedor de slots del .plr (confirmado contra el constructor real
// `var ma = function() {...}` de script.js, investigacion dirigida - ver el plan en
// C:\Users\adrian\.claude\plans\streamed-leaping-balloon.md). Cuando IsAvailable da false para
// un slot, ese slot no consume NINGUN byte (ni siquiera un id=0) - se trata como vacio sin mas.
public sealed class PlrContainerSpec
{
    public required int SlotCount { get; init; }
    public required bool Multi { get; init; }

    // 0 = el contenedor nunca lleva byte de favorito (equipo, banco/caja/fragua en este motor).
    public required int FavFlagMinVersion { get; init; }
    public required Func<int, int, bool> IsAvailable { get; init; } // (version, slotIndex) -> disponible

    public bool IncludesFavoriteByte(int version) => FavFlagMinVersion > 0 && version >= FavFlagMinVersion;

    public static readonly PlrContainerSpec Inventory = new()
    {
        SlotCount = 50,
        Multi = true,
        FavFlagMinVersion = 145,
        IsAvailable = (version, i) => i < 40 || version >= 58,
    };

    public static readonly PlrContainerSpec Coins = new()
    {
        SlotCount = 4,
        Multi = true,
        FavFlagMinVersion = 145,
        IsAvailable = (_, _) => true,
    };

    public static readonly PlrContainerSpec Ammo = new()
    {
        SlotCount = 4,
        Multi = true,
        FavFlagMinVersion = 145,
        IsAvailable = (_, _) => true,
    };

    // equipmentItems y equipmentDyes comparten exactamente el mismo spec (5 slots, no
    // apilable, sin byte de favorito, disponible completo desde version>=145).
    public static readonly PlrContainerSpec Equipment = new()
    {
        SlotCount = 5,
        Multi = false,
        FavFlagMinVersion = 0,
        IsAvailable = (version, _) => version >= 145,
    };

    // bankItems y safeItems comparten literalmente la misma funcion en script.js
    // (ma.__bankOrSafe_iter) - mismo spec exacto para los dos.
    public static readonly PlrContainerSpec BankOrSafe = new()
    {
        SlotCount = 40,
        Multi = true,
        FavFlagMinVersion = 0,
        IsAvailable = (version, i) => i < 20 || version >= 58,
    };

    public static readonly PlrContainerSpec Forge = new()
    {
        SlotCount = 40,
        Multi = true,
        FavFlagMinVersion = 0,
        IsAvailable = (version, _) => version >= 184,
    };

    public static readonly PlrContainerSpec Void = new()
    {
        SlotCount = 40,
        Multi = true,
        FavFlagMinVersion = 269,
        IsAvailable = (version, _) => version >= 200,
    };

    // Slots de un loadout (items/social/dyes) - SUPUESTO no confirmado explicitamente por la
    // investigacion (que documento la tabla de los 9 contenedores con nombre propio, pero no
    // dio un spec aparte para P.prototype.handle): se asume el mismo comportamiento que
    // Equipment (no apilable, sin byte de favorito) por ser conceptualmente lo mismo ("equipo
    // puesto"), pero revisar esto primero si un round-trip real con loadouts falla.
    public static readonly PlrContainerSpec LoadoutSlot = new()
    {
        SlotCount = 10,
        Multi = false,
        FavFlagMinVersion = 0,
        IsAvailable = (_, _) => true, // el propio numero de slots (10) ya depende de la version en PlrLoadout
    };
}
