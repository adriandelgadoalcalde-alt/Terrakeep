using System.Text;
using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.Core.Tests.PlrFormat;

// Cobertura del rango de version anterior a 145 (Terraria 1.1.2 en adelante) y de dos bugs
// reales encontrados en el rango >=145 ya soportado (investigacion dirigida 1-sep-2026, ver
// bitacora.md "Compatibilidad completa de versiones"). No hay .plr reales tan antiguos en este
// PC, asi que estos tests construyen el stream de bytes A MANO, de forma independiente del
// propio PlrBodySerializer (releyendo el informe real, no copiando su codigo), siguiendo
// exactamente el mismo criterio que PlrItemSlotTests ya usa para el formato de un slot. Si el
// serializador se desvia de la especificacion real, un desajuste de bytes lo revienta aqui.
public class PlrBodySerializerOldVersionsTests
{
    // Construye el stream minimo valido para una version dada: nombre/dificultad/salud fijos,
    // un unico item no vacio en el inventario (slot 0) y otro en el loadout primario (slot 0),
    // resto vacio/cero. Replica el layout exacto documentado por la investigacion real, campo a
    // campo, sin llamar a PlrBodySerializer.
    // 500 esta deliberadamente por debajo del maxId real mas bajo de toda la tabla (603, para
    // version<69) para que los tests que no comprueban el clamp de maxId sobrevivan sin mas en
    // CUALQUIER version probada aqui - un id mas alto se clamparia a 0 en las versiones mas
    // antiguas y el test dejaria de comprobar lo que dice comprobar.
    private static byte[] BuildMinimalCharacterBytes(int version, int inventoryItemId = 500, int loadoutItemId = 0)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        void SharpString(string s)
        {
            w.Write((byte)s.Length); // nombres de prueba cortos, cabe en 1 byte de longitud
            w.Write(Encoding.UTF8.GetBytes(s));
        }

        w.Write(version);

        if (version >= 145)
        {
            w.Write(1869374834u);
            w.Write(56846695u);
            w.Write(0u); // metaVersion
            w.Write(0u); // metaFlags1
            w.Write(0u); // metaFlags2
        }

        SharpString("Test");
        w.Write((byte)0); // difficulty

        if (version >= 145)
        {
            w.Write(0u); // playTimeLow
            w.Write(0u); // playTimeHigh
        }

        w.Write(7); // hairStyle
        if (version >= 82) w.Write((byte)0); // hairDye
        if (version >= 315) w.Write((byte)0); // team
        if (version >= 83)
        {
            w.Write((byte)0); // hideVisual1
            if (version >= 145) w.Write((byte)0); // hideVisual2
        }
        if (version >= 145) w.Write((byte)0); // hideMisc

        if (version >= 145) w.Write((byte)3); // gender plano
        else w.Write(true); // gender invertido: true -> Gender=0 al leer

        w.Write(100); // healthNow
        w.Write(100); // healthMax
        w.Write(20);  // manaNow
        w.Write(20);  // manaMax

        if (version >= 145)
        {
            w.Write((byte)1); // extraAccessory
            if (version >= 230) { w.Write((byte)0); w.Write((byte)0); }
            if (version >= 269)
            {
                w.Write((byte)0);
                if (version >= 324) w.Write((byte)0);
                for (int i = 1; i <= 6; i++) w.Write((byte)0);
            }
            bool hasFinishedDD2Event = version >= 184; // isSwitch=false en estos tests
            if (hasFinishedDD2Event) w.Write((byte)1);
            w.Write(12345); // taxMoney - valor centinela para detectar desalineado
            if (version >= 269) { w.Write(0); w.Write(0); }
        }

        for (int c = 0; c < 7; c++) { w.Write((byte)10); w.Write((byte)20); w.Write((byte)30); }

        // --- loadout primario ---
        var (loadItems, loadSocial, loadDyes) = version switch
        {
            >= 145 => (10, 10, 10),
            >= 81 => (8, 8, 8),
            _ => (8, 3, version > 39 ? 3 : 0),
        };
        bool loadoutFav = version >= 322;
        void WriteLoadoutSlot(int id)
        {
            w.Write(id); // multi=false en el primario, sin count
            w.Write((byte)0); // prefix
            if (loadoutFav) w.Write((byte)0); // favorito
        }
        WriteLoadoutSlot(loadoutItemId);
        for (int i = 1; i < loadItems; i++) WriteLoadoutSlot(0);
        for (int i = 0; i < loadSocial; i++) WriteLoadoutSlot(0);
        for (int i = 0; i < loadDyes; i++) WriteLoadoutSlot(0);

        // --- inventory (50 slots logicos, i<40||version>=58 disponibles) ---
        int invAvailable = version >= 58 ? 50 : 40;
        for (int i = 0; i < invAvailable; i++)
        {
            w.Write(i == 0 ? inventoryItemId : 0); // id
            w.Write(i == 0 && inventoryItemId != 0 ? 1 : 0); // count (multi=true)
            w.Write((byte)0); // prefix
            if (version >= 145) w.Write((byte)0); // favorito
        }

        // --- coins / ammo (4 slots cada uno, siempre disponibles) ---
        for (int container = 0; container < 2; container++)
        {
            for (int i = 0; i < 4; i++)
            {
                w.Write(0); w.Write(0); w.Write((byte)0);
                if (version >= 145) w.Write((byte)0);
            }
        }

        // --- equipmentItems/equipmentDyes (5+5, solo si version>=145, sin favorito) ---
        if (version >= 145)
        {
            for (int i = 0; i < 10; i++) { w.Write(0); w.Write((byte)0); }
        }

        // --- bank/safe (40 slots logicos, i<20||version>=58 disponibles) ---
        bool sequential = version >= 168;
        int bankAvailable = version >= 58 ? 40 : 20;
        if (sequential)
        {
            for (int i = 0; i < bankAvailable; i++) { w.Write(0); w.Write(0); w.Write((byte)0); }
            for (int i = 0; i < bankAvailable; i++) { w.Write(0); w.Write(0); w.Write((byte)0); }
        }
        else
        {
            for (int i = 0; i < bankAvailable; i++)
            {
                w.Write(0); w.Write(0); w.Write((byte)0); // bank[i]
                w.Write(0); w.Write(0); w.Write((byte)0); // safe[i]
            }
        }

        // --- forge (40 slots, solo si version>=184, sin favorito) ---
        if (version >= 184)
            for (int i = 0; i < 40; i++) { w.Write(0); w.Write(0); w.Write((byte)0); }

        // --- void (40 slots, solo si version>=200, favorito si version>=269) + voidVaultByte ---
        if (version >= 200)
        {
            for (int i = 0; i < 40; i++)
            {
                w.Write(0); w.Write(0); w.Write((byte)0);
                if (version >= 269) w.Write((byte)0);
            }
            w.Write((byte)0); // voidVaultByte
        }

        // --- buffs ---
        int buffCount = version >= 269 ? 44 : version >= 77 ? 22 : 10;
        for (int i = 0; i < buffCount; i++) { w.Write(0); w.Write(0); }

        // --- servers: solo el centinela -1 ---
        w.Write(-1);

        w.Write((byte)0); // hotbarLocked

        if (version >= 145)
            for (int i = 0; i < 13; i++) w.Write((byte)0); // hideInfo

        if (version >= 98) w.Write(0); // fishingQuestsCompleted

        if (version >= 168)
        {
            for (int i = 0; i < 4; i++) w.Write(0); // dpadBindings
            int builderCount = version >= 230 ? 12 : version >= 200 ? 11 : 10;
            for (int i = 0; i < builderCount; i++) w.Write(0); // builderAccStatus
        }

        if (version >= 184) w.Write(0); // bartenderQuests

        if (version >= 200)
        {
            w.Write((byte)0); // isDead=false, sin respawnTimer
            w.Write(0); w.Write(0); // lastTimeSaved1/2
            w.Write(0); // golferScore
            if (version >= 315) w.Write((byte)0); // researchMysteryByte
            w.Write(0); // research.Count = 0
            w.Write((byte)0); // tempItems bitmask = 0
        }

        if (version >= 220) w.Write((byte)0); // creativePowers: sentinela "sin mas" inmediato

        if (version >= 253) w.Write((byte)0); // superCartByte
        if (version >= 269) w.Write(0); // currentLoadout

        if (version >= 269)
        {
            for (int loadoutIdx = 0; loadoutIdx < 3; loadoutIdx++)
            {
                void WriteAltSlot(int id)
                {
                    w.Write(id); w.Write(0); w.Write((byte)0); // multi=true: id+count+prefix
                    if (loadoutFav) w.Write((byte)0);
                }
                for (int i = 0; i < 10; i++) WriteAltSlot(0);
                for (int i = 0; i < 10; i++) WriteAltSlot(0);
                for (int i = 0; i < 10; i++) WriteAltSlot(0);
                for (int i = 0; i < 10; i++) w.Write((byte)0); // hide[10]
            }
        }

        w.Flush();
        return ms.ToArray();
    }

    [Theory]
    [InlineData(39)]  // Terraria 1.1.2 - suelo con nombre de version conocido
    [InlineData(58)]  // 1.2.0 - inventario/banco pasan a su tamaño completo
    [InlineData(100)] // entre 1.2.4(98) y 1.3.0(145)
    [InlineData(145)] // 1.3.0 - aparece el bloque completo de metadata/playtime/gender-byte
    [InlineData(200)] // aparece Void/Research/TempItems
    [InlineData(322)] // aparece el byte de favorito en loadouts (bug real corregido)
    public void Read_ThenWrite_ProducesByteIdenticalOutput(int version)
    {
        byte[] original = BuildMinimalCharacterBytes(version);

        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(original)));

        using var outStream = new MemoryStream();
        using (var writer = new BinaryWriter(outStream, Encoding.UTF8, leaveOpen: true))
            PlrBodySerializer.Write(writer, character);

        Assert.Equal(original, outStream.ToArray());
    }

    [Fact]
    public void Read_Version39_UsesInvertedGenderBoolean()
    {
        byte[] bytes = BuildMinimalCharacterBytes(39);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        // BuildMinimalCharacterBytes escribe "true" para el booleano invertido -> Gender=0.
        Assert.Equal(0, character.Gender);
    }

    [Fact]
    public void Read_Version39_HasNoMetaVersionPlaytimeOrTaxMoney()
    {
        byte[] bytes = BuildMinimalCharacterBytes(39);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(0u, character.MetaVersion);
        Assert.Equal(0u, character.PlayTimeLow);
        Assert.Equal(0, character.TaxMoney); // nunca se escribio en el stream de v39
    }

    [Fact]
    public void Read_Version150_FinishedDD2EventAbsent_TaxMoneyStillAligned()
    {
        // Bug real corregido 1-sep-2026: en 145<=version<184 FinishedDD2Event NO existe en el
        // stream. Si el lector lo leyera de mas (como hacia el port anterior), taxMoney
        // quedaria desalineado - el valor centinela 12345 escrito por el builder lo detecta.
        byte[] bytes = BuildMinimalCharacterBytes(150);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(12345, character.TaxMoney);
        Assert.False(character.FinishedDD2Event); // nunca se escribio, se queda en el default
    }

    [Fact]
    public void Read_Version184_FinishedDD2EventPresent()
    {
        byte[] bytes = BuildMinimalCharacterBytes(184);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.True(character.FinishedDD2Event); // el builder escribe 1 cuando version>=184
        Assert.Equal(12345, character.TaxMoney);
    }

    [Fact]
    public void Read_Version39_InventorySlot0_ItemIdWithinMaxId_IsKept()
    {
        // maxId real para version<69 es 603 (ja.getMaxIds).
        byte[] bytes = BuildMinimalCharacterBytes(39, inventoryItemId: 600);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(600, character.Inventory[0].Id);
    }

    [Fact]
    public void Read_Version39_InventorySlot0_ItemIdAboveMaxId_IsClampedToEmpty()
    {
        byte[] bytes = BuildMinimalCharacterBytes(39, inventoryItemId: 9999);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.True(character.Inventory[0].IsEmpty);
        Assert.Equal(0, character.Inventory[0].Id);
    }

    [Fact]
    public void Read_Version39_InventorySlots40To49_NotAvailable_StayEmpty()
    {
        byte[] bytes = BuildMinimalCharacterBytes(39);
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(50, character.Inventory.Length);
        for (int i = 40; i < 50; i++)
            Assert.True(character.Inventory[i].IsEmpty);
    }

    [Theory]
    [InlineData(39, 8, 3, 0)]   // v<=39: 8 items + 3 social + 0 dyes
    [InlineData(40, 8, 3, 3)]   // v=40 (39<v): aparecen los 3 dyes
    [InlineData(81, 8, 8, 8)]   // v>=81: 8+8+8
    [InlineData(145, 10, 10, 10)] // v>=145 (moderno): 10+10+10
    public void Read_PrimaryLoadout_ItemInSlot0_SurvivesForEveryOldSlotCountTier(int version, int items, int social, int dyes)
    {
        _ = (items, social, dyes); // documentan la tabla real junto al InlineData, no hace falta usarlos aparte
        byte[] bytes = BuildMinimalCharacterBytes(version, loadoutItemId: 500); // por debajo del maxId mas bajo (603)
        var character = PlrBodySerializer.Read(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(500, character.PrimaryLoadout.Items[0].Id);
    }
}
