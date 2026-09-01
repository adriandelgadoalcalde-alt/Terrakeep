namespace TerrasavrNative.Core.PlrFormat;

public sealed class PlrBuff
{
    public required int Id { get; set; }
    public required int Time { get; set; }
}

public sealed class PlrResearchEntry
{
    public required string Pid { get; set; }
    public required int Count { get; set; }
}

public sealed class PlrServerEntry
{
    public required int SpawnX { get; set; }
    public required int SpawnY { get; set; }
    public required int Address { get; set; }
    public required string Name { get; set; }
}

// Modelo completo del cuerpo de un .plr YA DESCIFRADO (ver PlrCrypto para AES/PKCS7). Layout
// y condiciones de version confirmados campo a campo contra ma.prototype.handle real en
// script.js (dos rondas de investigacion dirigida, ver el plan). SIMPLIFICACION DELIBERADA:
// solo se soporta version (invVersion) >= 145 - todo personaje real jugado en los ultimos años
// cae ahi de sobra; las variantes de formato anteriores a 145 (gender binario invertido,
// loadouts de 8 slots, ausencia de guid/playtime/taxMoney...) no estan implementadas, ver
// PlrBodySerializer.
public sealed class PlrCharacter
{
    public required int Version { get; set; }
    public bool IsSwitch { get; set; } // casi siempre false en uso normal de PC - ver guid/bank-safe abajo

    public uint MetaVersion { get; set; }
    public uint MetaFlags1 { get; set; }
    public uint MetaFlags2 { get; set; }
    public string? Guid { get; set; } // solo si IsSwitch

    public required string Name { get; set; }
    public byte Difficulty { get; set; }

    public uint PlayTimeLow { get; set; }
    public uint PlayTimeHigh { get; set; }

    public int HairStyle { get; set; }
    public byte HairDye { get; set; }
    public byte Team { get; set; }
    public byte HideVisual1 { get; set; }
    public byte HideVisual2 { get; set; }
    public byte HideMisc { get; set; }
    public byte Gender { get; set; }

    public int HealthNow { get; set; }
    public int HealthMax { get; set; }
    public int ManaNow { get; set; }
    public int ManaMax { get; set; }

    public bool ExtraAccessory { get; set; }
    public bool UnlockedBiomeTorches { get; set; }
    public bool UsingBiomeTorches { get; set; }
    public bool[] ExtraUsingFlags { get; set; } = new bool[7]; // [0] + [1..6]
    public bool FinishedDD2Event { get; set; }
    public int TaxMoney { get; set; }
    public int PveDeaths { get; set; }
    public int PvpDeaths { get; set; }

    public byte[] HairColor { get; set; } = new byte[3];
    public byte[] SkinColor { get; set; } = new byte[3];
    public byte[] EyeColor { get; set; } = new byte[3];
    public byte[] ShirtColor { get; set; } = new byte[3];
    public byte[] UnderColor { get; set; } = new byte[3];
    public byte[] PantsColor { get; set; } = new byte[3];
    public byte[] ShoesColor { get; set; } = new byte[3];

    // loadouts[0] (mirror de lo puesto) + loadouts[1..3] (los 3 loadouts reales).
    public required PlrLoadout PrimaryLoadout { get; set; }
    public PlrLoadout[] Loadouts { get; set; } = []; // 3 elementos si Version>=269, si no vacio

    public PlrItemSlot[] Inventory { get; set; } = new PlrItemSlot[50];
    public PlrItemSlot[] Coins { get; set; } = new PlrItemSlot[4];
    public PlrItemSlot[] Ammo { get; set; } = new PlrItemSlot[4];
    public PlrItemSlot[] EquipmentItems { get; set; } = new PlrItemSlot[5];
    public PlrItemSlot[] EquipmentDyes { get; set; } = new PlrItemSlot[5];
    public PlrItemSlot[] BankItems { get; set; } = new PlrItemSlot[40];
    public PlrItemSlot[] SafeItems { get; set; } = new PlrItemSlot[40];
    public PlrItemSlot[] ForgeItems { get; set; } = new PlrItemSlot[40];
    public PlrItemSlot[] VoidItems { get; set; } = new PlrItemSlot[40];
    public byte VoidVaultByte { get; set; }

    public List<PlrBuff> Buffs { get; set; } = [];
    public List<PlrServerEntry> Servers { get; set; } = [];

    public bool HotbarLocked { get; set; }
    public bool[] HideInfo { get; set; } = new bool[13];
    public int FishingQuestsCompleted { get; set; }
    public int[] DpadBindings { get; set; } = new int[4];
    public int[] BuilderAccStatus { get; set; } = [];
    public int BartenderQuests { get; set; }

    public bool IsDead { get; set; }
    public int RespawnTimer { get; set; }
    public int LastTimeSaved1 { get; set; }
    public int LastTimeSaved2 { get; set; }
    public int GolferScore { get; set; }
    public byte ResearchMysteryByte { get; set; }

    public List<PlrResearchEntry> Research { get; set; } = [];

    // Solo los slots con bit activo en el bitmask original se leen/escriben; el resto se
    // representa vacio. 4 slots (mismo tamaño que Ammo/Coins).
    public PlrItemSlot[] TempItems { get; set; } = new PlrItemSlot[4];

    public List<PlrCreativePower> CreativePowers { get; set; } = [];

    public byte SuperCartByte { get; set; }
    public int CurrentLoadout { get; set; }

    // Bytes finales no reconocidos (de una version futura que este lector no entiende) - se
    // preservan tal cual para no perder datos al re-guardar. Vacio si no habia ninguno.
    public byte[] Trail { get; set; } = [];
}
