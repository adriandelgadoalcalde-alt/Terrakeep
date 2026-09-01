namespace TerrasavrNative.Core.PlrFormat;

public enum CreativePowerPayloadType { None, Bool, Float }

// Tabla de los 15 Poderes Creativos reales (Modo Viaje), indexada por el powerId que se lee
// como Int16 en el .plr (confirmado contra ma.creativePowers_init() real en script.js). Los
// nombres humanos de cada power NO se pudieron confirmar leyendo este archivo (el codigo
// minificado solo los registra por indice) - si hace falta mostrarlos en la UI mas adelante,
// cruzar contra Terraria.GameContent.Creative.CreativePowers.* en tModLoader-Decompiled en vez
// de adivinar aqui.
public static class PlrCreativePowersTable
{
    public static readonly CreativePowerPayloadType[] PayloadTypes =
    [
        CreativePowerPayloadType.Bool,   // 0
        CreativePowerPayloadType.None,   // 1
        CreativePowerPayloadType.None,   // 2
        CreativePowerPayloadType.None,   // 3
        CreativePowerPayloadType.None,   // 4
        CreativePowerPayloadType.Bool,   // 5
        CreativePowerPayloadType.None,   // 6
        CreativePowerPayloadType.None,   // 7
        CreativePowerPayloadType.Float,  // 8
        CreativePowerPayloadType.Bool,   // 9
        CreativePowerPayloadType.Bool,   // 10
        CreativePowerPayloadType.Bool,   // 11
        CreativePowerPayloadType.Float,  // 12
        CreativePowerPayloadType.Bool,   // 13
        CreativePowerPayloadType.Float,  // 14
    ];
}

public sealed class PlrCreativePower
{
    public required short PowerId { get; init; }
    public bool BoolValue { get; set; }
    public float FloatValue { get; set; }

    public CreativePowerPayloadType PayloadType =>
        PowerId >= 0 && PowerId < PlrCreativePowersTable.PayloadTypes.Length
            ? PlrCreativePowersTable.PayloadTypes[PowerId]
            : throw new InvalidDataException($"Id de poder creativo desconocido: {PowerId}");
}
