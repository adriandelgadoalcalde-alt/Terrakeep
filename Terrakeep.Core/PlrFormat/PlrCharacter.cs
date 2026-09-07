namespace Terrakeep.Core.PlrFormat;

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
    // C-05 (informe de pulido final): renombrado de "Address" a "WorldId" - es el campo real
    // spI del juego (WldHeader.WorldId del mundo al que pertenece este Spawn Point), nunca una
    // direccion de red. Solo el nombre en C# cambia, lectura/escritura intactas - antes el
    // usuario podia editar el id de un mundo real creyendo que editaba una IP.
    public required int WorldId { get; set; }
    public required string Name { get; set; }

    // C-05 (informe de pulido final, cierra E4): regla real del propio juego (Player.FindSpawn/
    // RemoveSpawn/AddSpawn, las tres identicas: spI[i]==Main.worldID && spN[i]==Main.worldName)
    // - antes cualquier Spawn Point guardado se mostraba en CUALQUIER mundo cargado. Exigir las
    // DOS condiciones (no solo el id) porque es lo que hace el juego: un .wld copiado y
    // renombrado a mano conserva el WorldId, y el juego real lo trataria como un mundo distinto.
    // Sin mundo cargado (loadedWorldId null) no se filtra - los marcadores no se ven de todos
    // modos sin mapa, y complicarlo no aporta nada.
    public bool BelongsToWorld(int? loadedWorldId, string? loadedWorldName) =>
        loadedWorldId is not int id || (WorldId == id && Name == loadedWorldName);
}

// Modelo completo del cuerpo de un .plr YA DESCIFRADO (ver PlrCrypto para AES/PKCS7). Layout
// y condiciones de version confirmados campo a campo contra ma.prototype.handle/
// P.prototype.handle/na.prototype.handle reales en script.js (tres rondas de investigacion
// dirigida, la ultima 1-sep-2026 para cerrar TODO el rango de version, ver bitacora.md
// "Compatibilidad completa de versiones"). Soporta el rango completo que el propio motor
// entiende, desde Terraria 1.1.2 (invVersion=39) hasta la version actual - ver
// PlrBodySerializer para el detalle de cada variante de formato antigua (gender binario
// invertido, loadout primario de 8 slots, ausencia de guid/playtime/taxMoney...).
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
