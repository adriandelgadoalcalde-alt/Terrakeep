namespace TerrasavrNative.Core.PlrFormat;

// Lector/escritor del cuerpo COMPLETO de un .plr ya descifrado y sin padding (PlrCrypto ya
// hace ambas cosas via AES PaddingMode.PKCS7 - bit a bit identico al padding manual que hace
// script.js dentro de ma.prototype.handle, solo que aplicado en la capa criptografica en vez
// de en el escritor del cuerpo; el resultado es el mismo).
//
// LIMITACION DELIBERADA: solo soporta version (invVersion) >= 145 - se comprueba y se lanza
// NotSupportedException si no, en vez de intentar las variantes de formato mas antiguas
// (gender binario invertido, loadouts de 8 slots, ausencia de guid/playtime/taxMoney...).
// Cualquier personaje jugado en años recientes cae de sobra en este rango.
//
// maxId de item (clamp de ids por encima del maximo real de esa version de Terraria) NO esta
// implementado todavia - haria falta la tabla real maxId-por-version, no confirmada aun por la
// investigacion. Se usa int.MaxValue (sin clamp) - suficiente para round-trip de un archivo ya
// valido, pero revisar antes de dejar que la UI asigne libremente ids nuevos a un personaje.
public static class PlrBodySerializer
{
    private const uint Magic1 = 1869374834;
    private const uint Magic2 = 56846695;
    private const int MinSupportedVersion = 145;
    private const int MaxIdPlaceholder = int.MaxValue;

    public static PlrCharacter Read(BinaryReader reader)
    {
        int version = reader.ReadInt32();
        if (version < MinSupportedVersion)
            throw new NotSupportedException(
                $"Version de personaje {version} no soportada todavia (minimo {MinSupportedVersion}) - ver PlrBodySerializer.");

        var character = new PlrCharacter
        {
            Version = version,
            Name = string.Empty,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        };

        uint m1 = reader.ReadUInt32();
        uint m2 = reader.ReadUInt32();
        if (m1 != Magic1 || m2 != Magic2)
            throw new InvalidDataException("That doesn't seem to be a valid profile.");

        character.MetaVersion = reader.ReadUInt32();
        character.MetaFlags1 = reader.ReadUInt32();
        character.MetaFlags2 = reader.ReadUInt32();

        if (character.IsSwitch)
            character.Guid = reader.ReadSharpString();

        character.Name = reader.ReadSharpString();
        character.Difficulty = reader.ReadByte();

        character.PlayTimeLow = reader.ReadUInt32();
        character.PlayTimeHigh = reader.ReadUInt32();

        character.HairStyle = reader.ReadInt32();
        if (version >= 82) character.HairDye = reader.ReadByte();
        if (version >= 315) character.Team = reader.ReadByte();
        if (version >= 83)
        {
            character.HideVisual1 = reader.ReadByte();
            character.HideVisual2 = reader.ReadByte(); // version>=145 ya garantizado por la linea base
        }
        character.HideMisc = reader.ReadByte();
        character.Gender = reader.ReadByte();

        character.HealthNow = reader.ReadInt32();
        character.HealthMax = reader.ReadInt32();
        character.ManaNow = reader.ReadInt32();
        character.ManaMax = reader.ReadInt32();

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
        character.FinishedDD2Event = reader.ReadByte() != 0; // isSwitch ? e>190 : e>=184, ya cubierto por linea base salvo el caso raro Switch
        character.TaxMoney = reader.ReadInt32();
        if (version >= 269)
        {
            character.PveDeaths = reader.ReadInt32();
            character.PvpDeaths = reader.ReadInt32();
        }

        character.HairColor = ReadRgb(reader);
        character.SkinColor = ReadRgb(reader);
        character.EyeColor = ReadRgb(reader);
        character.ShirtColor = ReadRgb(reader);
        character.UnderColor = ReadRgb(reader);
        character.PantsColor = ReadRgb(reader);
        character.ShoesColor = ReadRgb(reader);

        character.PrimaryLoadout = ReadLoadout(reader, isPrimary: true, version);

        character.Inventory = ReadContainer(reader, PlrContainerSpec.Inventory, version);
        character.Coins = ReadContainer(reader, PlrContainerSpec.Coins, version);
        character.Ammo = ReadContainer(reader, PlrContainerSpec.Ammo, version);

        character.EquipmentItems = new PlrItemSlot[5];
        character.EquipmentDyes = new PlrItemSlot[5];
        for (int i = 0; i < 5; i++)
        {
            character.EquipmentItems[i] = PlrItemSlot.Read(reader, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version), MaxIdPlaceholder);
            character.EquipmentDyes[i] = PlrItemSlot.Read(reader, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version), MaxIdPlaceholder);
        }

        bool sequentialBankSafe = version >= 168 || character.IsSwitch;
        (character.BankItems, character.SafeItems) = ReadBankAndSafe(reader, version, sequentialBankSafe);

        character.ForgeItems = ReadContainer(reader, PlrContainerSpec.Forge, version);
        character.VoidItems = ReadContainer(reader, PlrContainerSpec.Void, version);
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
                    character.TempItems[bit] = ReadSimpleSlot(reader, multi: true);
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
                character.Loadouts[i] = ReadLoadout(reader, isPrimary: false, version);
        }

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        character.Trail = remaining > 0 ? reader.ReadBytes((int)remaining) : [];

        return character;
    }

    public static void Write(BinaryWriter writer, PlrCharacter character)
    {
        int version = character.Version;
        if (version < MinSupportedVersion)
            throw new NotSupportedException(
                $"Version de personaje {version} no soportada todavia (minimo {MinSupportedVersion}) - ver PlrBodySerializer.");

        writer.Write(version);
        writer.Write(Magic1);
        writer.Write(Magic2);
        writer.Write(character.MetaVersion);
        writer.Write(character.MetaFlags1);
        writer.Write(character.MetaFlags2);

        if (character.IsSwitch)
            writer.WriteSharpString(character.Guid ?? string.Empty);

        writer.WriteSharpString(character.Name);
        writer.Write(character.Difficulty);

        writer.Write(character.PlayTimeLow);
        writer.Write(character.PlayTimeHigh);

        writer.Write(character.HairStyle);
        if (version >= 82) writer.Write(character.HairDye);
        if (version >= 315) writer.Write(character.Team);
        if (version >= 83)
        {
            writer.Write(character.HideVisual1);
            writer.Write(character.HideVisual2);
        }
        writer.Write(character.HideMisc);
        writer.Write(character.Gender);

        writer.Write(character.HealthNow);
        writer.Write(character.HealthMax);
        writer.Write(character.ManaNow);
        writer.Write(character.ManaMax);

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
        writer.Write((byte)(character.FinishedDD2Event ? 1 : 0));
        writer.Write(character.TaxMoney);
        if (version >= 269)
        {
            writer.Write(character.PveDeaths);
            writer.Write(character.PvpDeaths);
        }

        WriteRgb(writer, character.HairColor);
        WriteRgb(writer, character.SkinColor);
        WriteRgb(writer, character.EyeColor);
        WriteRgb(writer, character.ShirtColor);
        WriteRgb(writer, character.UnderColor);
        WriteRgb(writer, character.PantsColor);
        WriteRgb(writer, character.ShoesColor);

        WriteLoadout(writer, character.PrimaryLoadout, isPrimary: true);

        WriteContainer(writer, PlrContainerSpec.Inventory, version, character.Inventory);
        WriteContainer(writer, PlrContainerSpec.Coins, version, character.Coins);
        WriteContainer(writer, PlrContainerSpec.Ammo, version, character.Ammo);

        for (int i = 0; i < 5; i++)
        {
            character.EquipmentItems[i].Write(writer, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version));
            character.EquipmentDyes[i].Write(writer, PlrContainerSpec.Equipment.Multi, PlrContainerSpec.Equipment.IncludesFavoriteByte(version));
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
        for (int i = 0; i < 13; i++)
            writer.Write((byte)(character.HideInfo[i] ? 1 : 0));
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
                WriteLoadout(writer, character.Loadouts[i], isPrimary: false);
        }

        if (character.Trail.Length > 0)
            writer.Write(character.Trail);
    }

    // --- contenedores ---

    private static PlrItemSlot[] ReadContainer(BinaryReader reader, PlrContainerSpec spec, int version)
    {
        var slots = new PlrItemSlot[spec.SlotCount];
        for (int i = 0; i < spec.SlotCount; i++)
        {
            slots[i] = spec.IsAvailable(version, i)
                ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder)
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

    private static (PlrItemSlot[] Bank, PlrItemSlot[] Safe) ReadBankAndSafe(BinaryReader reader, int version, bool sequential)
    {
        var bank = new PlrItemSlot[40];
        var safe = new PlrItemSlot[40];
        var spec = PlrContainerSpec.BankOrSafe;

        if (sequential)
        {
            for (int i = 0; i < 40; i++)
                bank[i] = spec.IsAvailable(version, i) ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder) : PlrItemSlot.Empty;
            for (int i = 0; i < 40; i++)
                safe[i] = spec.IsAvailable(version, i) ? PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder) : PlrItemSlot.Empty;
        }
        else
        {
            for (int i = 0; i < 40; i++)
            {
                if (!spec.IsAvailable(version, i)) { bank[i] = PlrItemSlot.Empty; safe[i] = PlrItemSlot.Empty; continue; }
                bank[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder);
                safe[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder);
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
    // sin favorito, sin gate de disponibilidad (ya resuelto por el bitmask exterior).
    private static PlrItemSlot ReadSimpleSlot(BinaryReader reader, bool multi)
    {
        int id = reader.ReadInt32();
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

    private static PlrLoadout ReadLoadout(BinaryReader reader, bool isPrimary, int version)
    {
        var spec = PlrContainerSpec.LoadoutSlot;
        var items = new PlrItemSlot[10];
        var social = new PlrItemSlot[10];
        var dyes = new PlrItemSlot[10];
        for (int i = 0; i < 10; i++) items[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder);
        for (int i = 0; i < 10; i++) social[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder);
        for (int i = 0; i < 10; i++) dyes[i] = PlrItemSlot.Read(reader, spec.Multi, spec.IncludesFavoriteByte(version), MaxIdPlaceholder);

        bool[]? hide = null;
        if (!isPrimary)
        {
            hide = new bool[10];
            for (int i = 0; i < 10; i++) hide[i] = reader.ReadByte() != 0;
        }
        return new PlrLoadout { Items = items, Social = social, Dyes = dyes, Hide = hide };
    }

    private static void WriteLoadout(BinaryWriter writer, PlrLoadout loadout, bool isPrimary)
    {
        var spec = PlrContainerSpec.LoadoutSlot;
        for (int i = 0; i < 10; i++) loadout.Items[i].Write(writer, spec.Multi, false);
        for (int i = 0; i < 10; i++) loadout.Social[i].Write(writer, spec.Multi, false);
        for (int i = 0; i < 10; i++) loadout.Dyes[i].Write(writer, spec.Multi, false);

        if (!isPrimary)
        {
            var hide = loadout.Hide ?? new bool[10];
            for (int i = 0; i < 10; i++) writer.Write((byte)(hide[i] ? 1 : 0));
        }
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
            servers.Add(new PlrServerEntry { SpawnX = spawnX, SpawnY = spawnY, Address = address, Name = name });
        }
        return servers;
    }

    private static void WriteServers(BinaryWriter writer, List<PlrServerEntry> servers)
    {
        foreach (var s in servers)
        {
            if (IsBlankServerEntry(s.SpawnX, s.SpawnY, s.Address)) continue;
            writer.Write(s.SpawnX);
            writer.Write(s.SpawnY);
            writer.Write(s.Address);
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
