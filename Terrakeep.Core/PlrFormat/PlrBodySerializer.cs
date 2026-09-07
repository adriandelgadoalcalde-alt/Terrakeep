namespace Terrakeep.Core.PlrFormat;

// Lector/escritor del cuerpo COMPLETO de un .plr ya descifrado y sin padding (PlrCrypto ya
// hace ambas cosas via AES PaddingMode.PKCS7 - bit a bit identico al padding manual que hace
// script.js dentro de ma.prototype.handle, solo que aplicado en la capa criptografica en vez
// de en el escritor del cuerpo; el resultado es el mismo).
//
// Soporta TODO el rango de version (invVersion) que el propio motor real entiende - confirmado
// campo a campo contra ma.prototype.handle/P.prototype.handle/na.prototype.handle reales en
// script.js (investigacion dirigida de 1-sep-2026, ver bitacora.md "Compatibilidad completa de
// versiones"), sin ningun umbral minimo inventado: el motor real tampoco tiene guarda de
// minimo (intenta parsear cualquier version no negativa cayendo en la rama mas baja de cada
// condicional), asi que este puerto hace lo mismo. El umbral con nombre de version de juego
// conocido mas bajo es 39 (Terraria 1.1.2); por debajo de eso el formato no cambia mas, sigue
// usando la misma rama que 1.1.2.
public static class PlrBodySerializer
{
    private const uint Magic1 = 1869374834;
    private const uint Magic2 = 56846695;

    // Techo real de id de item por invVersion (ja.getMaxIds real, confirmado 1-sep-2026) - se
    // calcula UNA VEZ por Read() a partir de la version y se aplica igual a TODOS los slots de
    // TODOS los contenedores (incluidos loadouts y tempItems), igual que el motor real. Solo
    // afecta a lectura ("id > maxId && (id = 0)"), nunca a escritura. Los ids sinteticos de
    // Calamity (>= CalamityIds.ItemIdBase, 20000000) nunca pasan por aqui: se inyectan en una
    // capa por encima de este lector, que solo ve el .plr vanilla puro.
    private static int GetMaxItemId(int version) => version switch
    {
        > 200 => 16384,
        >= 190 => 3929,
        >= 184 => 3883,
        >= 175 => 3796,
        >= 168 => 3729,
        >= 145 => 3601,
        >= 98 => 2748,
        >= 93 => 2288,
        >= 77 => 1965,
        >= 70 => 1725,
        >= 69 => 1614,
        _ => 603,
    };

    public static PlrCharacter Read(BinaryReader reader)
    {
        int version = reader.ReadInt32();
        int maxId = GetMaxItemId(version);

        var character = new PlrCharacter
        {
            Version = version,
            Name = string.Empty,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        };

        if (version >= 145)
        {
            uint m1 = reader.ReadUInt32();
            uint m2 = reader.ReadUInt32();
            if (m1 != Magic1 || m2 != Magic2)
                throw new InvalidDataException("That doesn't seem to be a valid profile.");

            character.MetaVersion = reader.ReadUInt32();
            character.MetaFlags1 = reader.ReadUInt32();
            character.MetaFlags2 = reader.ReadUInt32();

            if (character.IsSwitch)
                character.Guid = reader.ReadSharpString();
        }

        character.Name = reader.ReadSharpString();
        character.Difficulty = reader.ReadByte();

        if (version >= 145)
        {
            character.PlayTimeLow = reader.ReadUInt32();
            character.PlayTimeHigh = reader.ReadUInt32();
        }

        character.HairStyle = reader.ReadInt32();
        if (version >= 82) character.HairDye = reader.ReadByte();
        if (version >= 315) character.Team = reader.ReadByte();
        if (version >= 83)
        {
            character.HideVisual1 = reader.ReadByte();
            if (version >= 145) character.HideVisual2 = reader.ReadByte();
        }
        if (version >= 145) character.HideMisc = reader.ReadByte();

        if (version >= 145)
        {
            character.Gender = reader.ReadByte();
        }
        else
        {
            // Genero binario invertido en saves antiguos: solo distingue dos valores de
            // Gender (0 y 4), no el byte plano moderno - confirmado contra ma.prototype.handle
            // real (b_bool ? gender=0 : gender=4).
            character.Gender = reader.ReadBoolean() ? (byte)0 : (byte)4;
        }

        character.HealthNow = reader.ReadInt32();
        character.HealthMax = reader.ReadInt32();
        character.ManaNow = reader.ReadInt32();
        character.ManaMax = reader.ReadInt32();

        if (version >= 145)
        {
            character.ExtraAccessory = reader.ReadByte() != 0;
            if (version >= 230)
            {
                character.UnlockedBiomeTorches = reader.ReadByte() != 0;
                character.UsingBiomeTorches = reader.ReadByte() != 0;
            }
            if (version >= 269)
            {
                character.ExtraUsingFlags[0] = reader.ReadByte() != 0;
                if (version >= 324) reader.ReadByte(); // 1 byte reservado, valor 0
                for (int i = 1; i <= 6; i++)
                    character.ExtraUsingFlags[i] = reader.ReadByte() != 0;
            }
            // Umbral real (isSwitch ? version>190 : version>=184) - bug real corregido
            // 1-sep-2026: antes se leia sin condicion ninguna, asumiendo erroneamente que
            // version>=145 ya lo garantizaba (existe un hueco real 145-183 sin este campo).
            bool hasFinishedDD2Event = character.IsSwitch ? version > 190 : version >= 184;
            if (hasFinishedDD2Event) character.FinishedDD2Event = reader.ReadByte() != 0;
            character.TaxMoney = reader.ReadInt32();
            if (version >= 269)
            {
                character.PveDeaths = reader.ReadInt32();
                character.PvpDeaths = reader.ReadInt32();
            }
        }

        character.HairColor = ReadRgb(reader);
        character.SkinColor = ReadRgb(reader);
        character.EyeColor = ReadRgb(reader);
        character.ShirtColor = ReadRgb(reader);
        character.UnderColor = ReadRgb(reader);
        character.PantsColor = ReadRgb(reader);
        character.ShoesColor = ReadRgb(reader);

        character.PrimaryLoadout = ReadLoadout(reader, isPrimary: true, version, maxId);

        character.Inventory = ReadContainer(reader, PlrContainerSpec.Inventory, version, maxId);
        character.Coins = ReadContainer(reader, PlrContainerSpec.Coins, version, maxId);
        character.Ammo = ReadContainer(reader, PlrContainerSpec.Ammo, version, maxId);

        character.EquipmentItems = new PlrItemSlot[5];
        character.EquipmentDyes = new PlrItemSlot[5];
        Array.Fill(character.EquipmentItems, PlrItemSlot.Empty);
        Array.Fill(character.EquipmentDyes, PlrItemSlot.Empty);
        // Bug real corregido 1-sep-2026: este bloque nunca comprobaba version>=145 aunque
        // PlrContainerSpec.Equipment.IsAvailable ya lo decia - invisible mientras el suelo de
        // esta app era 145 (siempre disponible), pero revienta el resto del stream en
        // cualquier version mas antigua (pet/mascota "miscEquips" no existe antes de 145, vive
        // solo dentro del loadout primario de 8 slots, ver ReadLoadout).
        if (version >= 145)
        {
            for (int i = 0; i < 5; i++)
            {
                character.EquipmentItems[i] = PlrItemSlot.Read(reader, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version), maxId);
                character.EquipmentDyes[i] = PlrItemSlot.Read(reader, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version), maxId);
            }
        }

        bool sequentialBankSafe = version >= 168 || character.IsSwitch;
        (character.BankItems, character.SafeItems) = ReadBankAndSafe(reader, version, maxId, sequentialBankSafe);

        character.ForgeItems = ReadContainer(reader, PlrContainerSpec.Forge, version, maxId);
        character.VoidItems = ReadContainer(reader, PlrContainerSpec.Void, version, maxId);
        if (version >= 200) character.VoidVaultByte = reader.ReadByte();

        int buffCount = version >= 269 ? 44 : version >= 77 ? 22 : 10;
        character.Buffs = new List<PlrBuff>(buffCount);
        for (int i = 0; i < buffCount; i++)
            character.Buffs.Add(new PlrBuff { Id = reader.ReadInt32(), Time = reader.ReadInt32() });

        character.Servers = ReadServers(reader);

        character.HotbarLocked = reader.ReadByte() != 0;
        if (version >= 145)
        {
            character.HideInfo = new bool[13];
            for (int i = 0; i < 13; i++) character.HideInfo[i] = reader.ReadByte() != 0;
        }
        if (version >= 98) character.FishingQuestsCompleted = reader.ReadInt32();
        if (version >= 168 && (!character.IsSwitch || version > 190))
        {
            character.DpadBindings = new int[4];
            for (int i = 0; i < 4; i++) character.DpadBindings[i] = reader.ReadInt32();
        }
        if (version >= 168)
        {
            int builderCount = version >= 230 ? 12 : version >= 200 ? 11 : 10;
            character.BuilderAccStatus = new int[builderCount];
            for (int i = 0; i < builderCount; i++) character.BuilderAccStatus[i] = reader.ReadInt32();
        }
        if (version >= 184) character.BartenderQuests = reader.ReadInt32();

        if (version >= 200)
        {
            character.IsDead = reader.ReadByte() != 0;
            if (character.IsDead) character.RespawnTimer = reader.ReadInt32();

            character.LastTimeSaved1 = reader.ReadInt32();
            character.LastTimeSaved2 = reader.ReadInt32();
            character.GolferScore = reader.ReadInt32();

            if (version >= 315) character.ResearchMysteryByte = reader.ReadByte();

            int researchCount = reader.ReadInt32();
            character.Research = new List<PlrResearchEntry>(researchCount);
            for (int i = 0; i < researchCount; i++)
                character.Research.Add(new PlrResearchEntry { Pid = reader.ReadSharpString(), Count = reader.ReadInt32() });

            byte tempBitmask = reader.ReadByte();
            character.TempItems = new PlrItemSlot[4];
            Array.Fill(character.TempItems, PlrItemSlot.Empty);
            for (int bit = 0; bit < 4; bit++)
            {
                if ((tempBitmask & (1 << bit)) != 0)
                    character.TempItems[bit] = ReadSimpleSlot(reader, multi: true, maxId);
            }
        }

        if (version >= 220)
            character.CreativePowers = ReadCreativePowers(reader);

        character.SuperCartByte = version >= 253 ? reader.ReadByte() : (byte)0;
        character.CurrentLoadout = version >= 269 ? reader.ReadInt32() : 0;

        if (version >= 269)
        {
            character.Loadouts = new PlrLoadout[3];
            for (int i = 0; i < 3; i++)
                character.Loadouts[i] = ReadLoadout(reader, isPrimary: false, version, maxId);
        }

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        character.Trail = remaining > 0 ? reader.ReadBytes((int)remaining) : [];

        return character;
    }

    public static void Write(BinaryWriter writer, PlrCharacter character)
    {
        int version = character.Version;

        writer.Write(version);

        if (version >= 145)
        {
            writer.Write(Magic1);
            writer.Write(Magic2);
            writer.Write(character.MetaVersion);
            writer.Write(character.MetaFlags1);
            writer.Write(character.MetaFlags2);

            if (character.IsSwitch)
                writer.WriteSharpString(character.Guid ?? string.Empty);
        }

        writer.WriteSharpString(character.Name);
        writer.Write(character.Difficulty);

        if (version >= 145)
        {
            writer.Write(character.PlayTimeLow);
            writer.Write(character.PlayTimeHigh);
        }

        writer.Write(character.HairStyle);
        if (version >= 82) writer.Write(character.HairDye);
        if (version >= 315) writer.Write(character.Team);
        if (version >= 83)
        {
            writer.Write(character.HideVisual1);
            if (version >= 145) writer.Write(character.HideVisual2);
        }
        if (version >= 145) writer.Write(character.HideMisc);

        if (version >= 145)
        {
            writer.Write(character.Gender);
        }
        else
        {
            writer.Write(character.Gender < 4); // genero binario invertido, ver Read()
        }

        writer.Write(character.HealthNow);
        writer.Write(character.HealthMax);
        writer.Write(character.ManaNow);
        writer.Write(character.ManaMax);

        if (version >= 145)
        {
            writer.Write((byte)(character.ExtraAccessory ? 1 : 0));
            if (version >= 230)
            {
                writer.Write((byte)(character.UnlockedBiomeTorches ? 1 : 0));
                writer.Write((byte)(character.UsingBiomeTorches ? 1 : 0));
            }
            if (version >= 269)
            {
                writer.Write((byte)(character.ExtraUsingFlags[0] ? 1 : 0));
                if (version >= 324) writer.Write((byte)0);
                for (int i = 1; i <= 6; i++)
                    writer.Write((byte)(character.ExtraUsingFlags[i] ? 1 : 0));
            }
            bool hasFinishedDD2Event = character.IsSwitch ? version > 190 : version >= 184;
            if (hasFinishedDD2Event) writer.Write((byte)(character.FinishedDD2Event ? 1 : 0));
            writer.Write(character.TaxMoney);
            if (version >= 269)
            {
                writer.Write(character.PveDeaths);
                writer.Write(character.PvpDeaths);
            }
        }

        WriteRgb(writer, character.HairColor);
        WriteRgb(writer, character.SkinColor);
        WriteRgb(writer, character.EyeColor);
        WriteRgb(writer, character.ShirtColor);
        WriteRgb(writer, character.UnderColor);
        WriteRgb(writer, character.PantsColor);
        WriteRgb(writer, character.ShoesColor);

        WriteLoadout(writer, character.PrimaryLoadout, isPrimary: true, version);

        WriteContainer(writer, PlrContainerSpec.Inventory, version, character.Inventory);
        WriteContainer(writer, PlrContainerSpec.Coins, version, character.Coins);
        WriteContainer(writer, PlrContainerSpec.Ammo, version, character.Ammo);

        if (version >= 145)
        {
            for (int i = 0; i < 5; i++)
            {
                character.EquipmentItems[i].Write(writer, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version));
                character.EquipmentDyes[i].Write(writer, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version));
            }
        }

        bool sequentialBankSafe = version >= 168 || character.IsSwitch;
        WriteBankAndSafe(writer, version, character.BankItems, character.SafeItems, sequentialBankSafe);

        WriteContainer(writer, PlrContainerSpec.Forge, version, character.ForgeItems);
        WriteContainer(writer, PlrContainerSpec.Void, version, character.VoidItems);
        if (version >= 200) writer.Write(character.VoidVaultByte);

        int buffCount = version >= 269 ? 44 : version >= 77 ? 22 : 10;
        for (int i = 0; i < buffCount; i++)
        {
            var buff = i < character.Buffs.Count ? character.Buffs[i] : new PlrBuff { Id = 0, Time = 0 };
            writer.Write(buff.Id);
            writer.Write(buff.Time);
        }

        WriteServers(writer, character.Servers);

        writer.Write((byte)(character.HotbarLocked ? 1 : 0));
        // Bug real corregido 1-sep-2026: igual que Equipment, este bucle nunca comprobaba
        // version>=145 (invisible mientras el suelo de esta app era 145).
        if (version >= 145)
        {
            for (int i = 0; i < 13; i++)
                writer.Write((byte)(character.HideInfo[i] ? 1 : 0));
        }
        if (version >= 98) writer.Write(character.FishingQuestsCompleted);
        if (version >= 168 && (!character.IsSwitch || version > 190))
        {
            for (int i = 0; i < 4; i++) writer.Write(character.DpadBindings[i]);
        }
        if (version >= 168)
        {
            int builderCount = version >= 230 ? 12 : version >= 200 ? 11 : 10;
            for (int i = 0; i < builderCount; i++)
                writer.Write(i < character.BuilderAccStatus.Length ? character.BuilderAccStatus[i] : 0);
        }
        if (version >= 184) writer.Write(character.BartenderQuests);

        if (version >= 200)
        {
            writer.Write((byte)(character.IsDead ? 1 : 0));
            if (character.IsDead) writer.Write(character.RespawnTimer);

            writer.Write(character.LastTimeSaved1);
            writer.Write(character.LastTimeSaved2);
            writer.Write(character.GolferScore);

            if (version >= 315) writer.Write(character.ResearchMysteryByte);

            writer.Write(character.Research.Count);
            foreach (var entry in character.Research)
            {
                writer.WriteSharpString(entry.Pid);
                writer.Write(entry.Count);
            }

            byte bitmask = 0;
            for (int bit = 0; bit < 4; bit++)
                if (!character.TempItems[bit].IsEmpty) bitmask |= (byte)(1 << bit);
            writer.Write(bitmask);
            for (int bit = 0; bit < 4; bit++)
                if ((bitmask & (1 << bit)) != 0)
                    WriteSimpleSlot(writer, character.TempItems[bit], multi: true);
        }

        if (version >= 220)
            WriteCreativePowers(writer, character.CreativePowers);

        if (version >= 253) writer.Write(character.SuperCartByte);
        if (version >= 269) writer.Write(character.CurrentLoadout);

        if (version >= 269)
        {
            for (int i = 0; i < 3; i++)
                WriteLoadout(writer, character.Loadouts[i], isPrimary: false, version);
        }

        if (character.Trail.Length > 0)
            writer.Write(character.Trail);
    }

    // --- contenedores ---

    private static PlrItemSlot[] ReadContainer(BinaryReader reader, PlrContainerSpec spec, int version, int maxId)
    {
        var slots = new PlrItemSlot[spec.SlotCount];
        for (int i = 0; i < spec.SlotCount; i++)
        {
            slots[i] = spec.IsAvailable(version, i)
                ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), maxId)
                : PlrItemSlot.Empty;
        }
        return slots;
    }

    private static void WriteContainer(BinaryWriter writer, PlrContainerSpec spec, int version, PlrItemSlot[] slots)
    {
        for (int i = 0; i < spec.SlotCount; i++)
        {
            if (spec.IsAvailable(version, i))
                slots[i].Write(writer, spec.Multi, spec.IncludesFavoriteByte(version));
        }
    }

    private static (PlrItemSlot[] Bank, PlrItemSlot[] Safe) ReadBankAndSafe(BinaryReader reader, int version, int maxId, bool sequential)
    {
        var bank = new PlrItemSlot[40];
        var safe = new PlrItemSlot[40];
        var spec = PlrContainerSpec.BankOrSafe;

        if (sequential)
        {
            for (int i = 0; i < 40; i++)
                bank[i] = spec.IsAvailable(version, i) ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), maxId) : PlrItemSlot.Empty;
            for (int i = 0; i < 40; i++)
                safe[i] = spec.IsAvailable(version, i) ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), maxId) : PlrItemSlot.Empty;
        }
        else
        {
            for (int i = 0; i < 40; i++)
            {
                if (!spec.IsAvailable(version, i)) { bank[i] = PlrItemSlot.Empty; safe[i] = PlrItemSlot.Empty; continue; }
                bank[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), maxId);
                safe[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), maxId);
            }
        }
        return (bank, safe);
    }

    private static void WriteBankAndSafe(BinaryWriter writer, int version, PlrItemSlot[] bank, PlrItemSlot[] safe, bool sequential)
    {
        var spec = PlrContainerSpec.BankOrSafe;
        if (sequential)
        {
            for (int i = 0; i < 40; i++)
                if (spec.IsAvailable(version, i)) bank[i].Write(writer, spec.Multi, spec.IncludesFavoriteByte(version));
            for (int i = 0; i < 40; i++)
                if (spec.IsAvailable(version, i)) safe[i].Write(writer, spec.Multi, spec.IncludesFavoriteByte(version));
        }
        else
        {
            for (int i = 0; i < 40; i++)
            {
                if (!spec.IsAvailable(version, i)) continue;
                bank[i].Write(writer, spec.Multi, spec.IncludesFavoriteByte(version));
                safe[i].Write(writer, spec.Multi, spec.IncludesFavoriteByte(version));
            }
        }
    }

    // tempItems usa el formato SIMPLE (na.prototype.save/load): id + count-opcional + prefix,
    // sin favorito, sin gate de disponibilidad (ya resuelto por el bitmask exterior). Tambien
    // pasa por el clamp real de maxId (na.maxId se aplica a TODOS los slots, sin excepcion).
    private static PlrItemSlot ReadSimpleSlot(BinaryReader reader, bool multi, int maxId)
    {
        int id = reader.ReadInt32();
        if (id > maxId) id = 0;
        int count = multi ? reader.ReadInt32() : (id != 0 ? 1 : 0);
        byte prefix = reader.ReadByte();
        return id == 0 ? PlrItemSlot.Empty : new PlrItemSlot(id, count, prefix, Favorited: false);
    }

    private static void WriteSimpleSlot(BinaryWriter writer, PlrItemSlot slot, bool multi)
    {
        writer.Write(slot.Id);
        if (multi) writer.Write(slot.Count);
        writer.Write(slot.Prefix);
    }

    // --- loadouts ---

    // P.prototype.handle real: el numero de slots de items/social/dyes del "equipo puesto"
    // depende de la version (confirmado 1-sep-2026, esto es lo que el comentario antiguo de
    // este archivo llamaba "loadouts de 8 slots" - NO tiene nada que ver con los loadouts
    // multiples modernos, es el formato viejo del unico set de equipo que existia antes de
    // v269). PlrLoadout sigue modelandose siempre como 10+10+10 en memoria por simplicidad de
    // la UI - los slots que no existen en el formato de esa version se quedan vacios al leer y
    // se descartan en silencio al escribir, igual que el motor real.
    private static (int Items, int Social, int Dyes) GetLoadoutSlotCounts(int version) => version switch
    {
        >= 145 => (10, 10, 10),
        >= 81 => (8, 8, 8),
        _ => (8, 3, version > 39 ? 3 : 0),
    };

    private static PlrLoadout ReadLoadout(BinaryReader reader, bool isPrimary, int version, int maxId)
    {
        var (itemCount, socialCount, dyeCount) = GetLoadoutSlotCounts(version);
        bool multi = !isPrimary; // bug real corregido 1-sep-2026: los 3 loadouts alternos SI llevan Int32 count por slot, el primario no
        bool includeFavorite = PlrContainerSpec.LoadoutSlot.IncludesFavoriteByte(version); // favFlagMinVersion=322, bug real corregido 1-sep-2026 (antes era 0, nunca se leia)

        var items = CreateEmptyLoadoutSlots();
        var social = CreateEmptyLoadoutSlots();
        var dyes = CreateEmptyLoadoutSlots();
        for (int i = 0; i < itemCount; i++) items[i] = PlrItemSlot.Read(reader, multi, includeFavorite, maxId);
        for (int i = 0; i < socialCount; i++) social[i] = PlrItemSlot.Read(reader, multi, includeFavorite, maxId);
        for (int i = 0; i < dyeCount; i++) dyes[i] = PlrItemSlot.Read(reader, multi, includeFavorite, maxId);

        bool[]? hide = null;
        if (!isPrimary)
        {
            hide = new bool[10];
            for (int i = 0; i < 10; i++) hide[i] = reader.ReadByte() != 0;
        }
        return new PlrLoadout { Items = items, Social = social, Dyes = dyes, Hide = hide };
    }

    private static void WriteLoadout(BinaryWriter writer, PlrLoadout loadout, bool isPrimary, int version)
    {
        var (itemCount, socialCount, dyeCount) = GetLoadoutSlotCounts(version);
        bool multi = !isPrimary;
        bool includeFavorite = PlrContainerSpec.LoadoutSlot.IncludesFavoriteByte(version);

        for (int i = 0; i < itemCount; i++) loadout.Items[i].Write(writer, multi, includeFavorite);
        for (int i = 0; i < socialCount; i++) loadout.Social[i].Write(writer, multi, includeFavorite);
        for (int i = 0; i < dyeCount; i++) loadout.Dyes[i].Write(writer, multi, includeFavorite);

        if (!isPrimary)
        {
            var hide = loadout.Hide ?? new bool[10];
            for (int i = 0; i < 10; i++) writer.Write((byte)(hide[i] ? 1 : 0));
        }
    }

    private static PlrItemSlot[] CreateEmptyLoadoutSlots()
    {
        var slots = new PlrItemSlot[10];
        Array.Fill(slots, PlrItemSlot.Empty);
        return slots;
    }

    // --- listas terminadas en sentinela ---

    // Entradas todo-a-cero (spawnX=spawnY=address=0) se descartan tanto al leer como al
    // escribir - confirmado en script.readable.js real: "0==l.spawnX&&0==l.spawnY&&0==
    // l.address||this.servers.push(l)" al leer, y "if(0!=l.spawnX||0!=l.spawnY||0!=l.address)
    // ...writeInt..." al escribir (defensivo por partida doble en el original). No es solo un
    // detalle cosmetico: sin este filtro en la lectura, una entrada asi (rara, pero posible en
    // un archivo real) se leeria y luego se re-escribiria tal cual, divergiendo del
    // comportamiento real.
    private static bool IsBlankServerEntry(int spawnX, int spawnY, int address) => spawnX == 0 && spawnY == 0 && address == 0;

    private static List<PlrServerEntry> ReadServers(BinaryReader reader)
    {
        var servers = new List<PlrServerEntry>();
        for (int i = 0; i < 200; i++) // limite de seguridad, igual que el lector real
        {
            int spawnX = reader.ReadInt32();
            if (spawnX == -1) break;
            int spawnY = reader.ReadInt32();
            int address = reader.ReadInt32();
            string name = reader.ReadSharpString();
            if (IsBlankServerEntry(spawnX, spawnY, address)) continue;
            servers.Add(new PlrServerEntry { SpawnX = spawnX, SpawnY = spawnY, WorldId = address, Name = name });
        }
        return servers;
    }

    private static void WriteServers(BinaryWriter writer, List<PlrServerEntry> servers)
    {
        foreach (var s in servers)
        {
            if (IsBlankServerEntry(s.SpawnX, s.SpawnY, s.WorldId)) continue;
            writer.Write(s.SpawnX);
            writer.Write(s.SpawnY);
            writer.Write(s.WorldId);
            writer.WriteSharpString(s.Name);
        }
        writer.Write(-1);
    }

    private static List<PlrCreativePower> ReadCreativePowers(BinaryReader reader)
    {
        var powers = new List<PlrCreativePower>();
        while (reader.ReadByte() != 0)
        {
            short powerId = reader.ReadInt16();
            var power = new PlrCreativePower { PowerId = powerId };
            switch (power.PayloadType)
            {
                case CreativePowerPayloadType.Bool: power.BoolValue = reader.ReadByte() != 0; break;
                case CreativePowerPayloadType.Float: power.FloatValue = reader.ReadSingle(); break;
            }
            powers.Add(power);
        }
        return powers;
    }

    private static void WriteCreativePowers(BinaryWriter writer, List<PlrCreativePower> powers)
    {
        foreach (var power in powers)
        {
            writer.Write((byte)1);
            writer.Write(power.PowerId);
            switch (power.PayloadType)
            {
                case CreativePowerPayloadType.Bool: writer.Write((byte)(power.BoolValue ? 1 : 0)); break;
                case CreativePowerPayloadType.Float: writer.Write(power.FloatValue); break;
            }
        }
        writer.Write((byte)0);
    }

    // --- utilidades ---

    private static byte[] ReadRgb(BinaryReader reader) => [reader.ReadByte(), reader.ReadByte(), reader.ReadByte()];

    private static void WriteRgb(BinaryWriter writer, byte[] rgb)
    {
        writer.Write(rgb[0]);
        writer.Write(rgb[1]);
        writer.Write(rgb[2]);
    }
}
