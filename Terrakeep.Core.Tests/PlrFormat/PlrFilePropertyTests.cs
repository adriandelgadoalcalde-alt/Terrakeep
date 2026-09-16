using CsCheck;
using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.Core.Tests.PlrFormat;

// KeepQA (16-sep-2026, paso 2/3 de la integracion de analisis externo): property-based testing
// real con CsCheck (github.com/AnthonyLloyd/CsCheck, Apache-2.0) sobre el formato .plr REAL de
// Terrakeep.Core (PlrFile/PlrBodySerializer) - generaliza el patron manual de
// PlrFileRealCharacterTests.cs (2 archivos reales de este PC) a cientos de variantes sinteticas
// validas-pero-raras: versiones limite (los saltos reales de PlrBodySerializer.Read/Write, no
// numeros al azar), IsSwitch combinado con cada rango, ids de item negativos/extremos, cadenas
// unicode/vacias/casi al limite de 255 bytes UTF-8 (el propio formato "SharpString" solo admite
// hasta byte.MaxValue bytes, ver PlrBinaryIO.WriteSharpString), listas vacias y al maximo.
//
// LA PROPIEDAD REAL QUE SE COMPRUEBA (y por que, en vez de comparar directamente un PlrCharacter
// generado a mano contra su propio round-trip):
//
//   Un PlrCharacter generado AL AZAR no respeta necesariamente las reglas de "que campo existe
//   segun la version" que SI cumple cualquier caracter que salga de PlrFile.Read (p.ej. generar
//   Team!=0 con version<315 es "basura" que el propio Write() ignora sin escribir ningun byte -
//   no es un bug, es un campo que el formato de esa version no tiene sitio para guardar). Exigir
//   igualdad byte a byte entre el crudo generado y su propio Write() seria una propiedad FALSA,
//   no del formato real.
//
//   La propiedad real y honesta (el mismo principio que usa fuzzer-guardados.js: "lo que no
//   sobrevive a leer+escribir" sobre datos REALES, aqui generalizado a datos SINTETICOS) es que
//   una vez que unos bytes pasan por UNA lectura real (PlrFile.Read), el PlrCharacter resultante
//   SI es canonico - PlrFile.Read siempre deja cada campo en un valor definido segun esa version
//   exacta - y a partir de ahi Write->Read debe ser un PUNTO FIJO estable: volver a escribirlo y
//   releerlo tiene que dar EXACTAMENTE los mismos bytes y el mismo objeto, para siempre. Si no lo
//   es, hay un campo que el lector deja sin fijar del todo, o que el escritor trata de forma
//   distinta la segunda vez - un bug real de round-trip, no un artefacto del generador.
//
//   bytes1 = Write(random)      // "random" es basura best-effort, no se compara directo
//   char1  = Read(bytes1)       // primera lectura real: AHORA SI es canonico
//   bytes2 = Write(char1)
//   char2  = Read(bytes2)
//   se exige: bytes2 == bytes1 (byte a byte) Y char1 == char2 (campo a campo)
public class PlrFilePropertyTests
{
    // Saltos de version REALES confirmados en PlrBodySerializer.Read/Write (umbrales >=X que
    // cambian que campos existen) mas GetMaxItemId, +/-1 alrededor de cada uno para forzar el
    // generador a cruzar la frontera en los dos sentidos, no solo caer "dentro" de un tramo.
    private static readonly int[] VersionBreakpoints =
    [
        1, 38, 39, 40, 57, 58, 59, 68, 69, 70, 71, 76, 77, 78, 80, 81, 82, 83, 84, 92, 93, 94,
        97, 98, 99, 144, 145, 146, 167, 168, 169, 174, 175, 176, 183, 184, 185, 189, 190, 191,
        199, 200, 201, 219, 220, 221, 229, 230, 231, 252, 253, 254, 268, 269, 270, 314, 315, 316,
        321, 322, 323, 324, 325, 400, 999,
    ];

    // Alfabeto deliberadamente mixto: ASCII, español de España (acentos/eñe en mayus y minus,
    // ¡¿), espacio y puntuación - todo en el rango Latin-1 Supplement o ASCII puro, así que cada
    // carácter ocupa como mucho 2 bytes en UTF-8 (confirmado: U+0080-U+00FF -> 2 bytes). Con un
    // máximo de 100 caracteres el peor caso son 200 bytes, siempre por debajo del límite real de
    // 255 bytes UTF-8 que impone PlrBinaryIO.WriteSharpString (si no, EL PROPIO ESCRITOR real
    // lanzaría InvalidDataException - eso rompería el fixture, no probaría un bug de Terrakeep).
    private const string AlfabetoSeguro =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 áéíóúÁÉÍÓÚñÑüÜ¡¿!?.,-_()";

    private static readonly Gen<string> GenSharpString = Gen.String[Gen.Char[AlfabetoSeguro], 0, 100];

    // Enteros con sesgo hacia valores límite reales (0, ±1, extremos de Int32) mezclados con
    // rango "normal" - los ids de item de un .plr real pueden ser negativos de forma sintética
    // (el lector solo hace clamp de "id > maxId", nunca de negativos - PlrItemSlot.Read lo dice
    // explícitamente) y el propio formato no impone ningún rango a la mayoría de estos int32.
    private static readonly Gen<int> GenIntBorde =
        Gen.OneOf(Gen.Int[-100_000, 200_000], Gen.OneOfConst(0, 1, -1, int.MinValue, int.MaxValue));

    private static readonly Gen<PlrItemSlot> GenSlot =
        Gen.Select(GenIntBorde, GenIntBorde, Gen.Byte, Gen.Bool,
            (id, count, prefix, fav) => new PlrItemSlot(id, count, prefix, fav));

    private static Gen<PlrItemSlot[]> GenSlots(int n) => GenSlot.Array[n];

    private static readonly Gen<byte[]> GenRgb = Gen.Byte.Array[3];

    private static readonly Gen<PlrLoadout> GenLoadoutAlterno =
        Gen.Select(GenSlots(10), GenSlots(10), GenSlots(10), Gen.Bool.Array[10],
            (items, social, dyes, hide) => new PlrLoadout { Items = items, Social = social, Dyes = dyes, Hide = hide });

    private static readonly Gen<PlrLoadout> GenLoadoutPrimario =
        Gen.Select(GenSlots(10), GenSlots(10), GenSlots(10),
            (items, social, dyes) => new PlrLoadout { Items = items, Social = social, Dyes = dyes, Hide = null });

    private static readonly Gen<PlrBuff> GenBuff =
        Gen.Select(GenIntBorde, GenIntBorde, (id, time) => new PlrBuff { Id = id, Time = time });

    private static readonly Gen<PlrResearchEntry> GenResearch =
        Gen.Select(GenSharpString, GenIntBorde, (pid, count) => new PlrResearchEntry { Pid = pid, Count = count });

    // Excluye -1 a propósito: es el centinela real de fin de lista de ReadServers/WriteServers
    // (spawnX==-1 -> break). Una entrada CON spawnX=-1 en medio de la lista truncaría las
    // siguientes al releer - comportamiento real y documentado del formato del propio juego
    // (ver el comentario "IsBlankServerEntry"/sentinela en PlrBodySerializer), no un bug de
    // Terrakeep: probarlo aquí solo demostraría una ambigüedad ya conocida del formato, no algo
    // nuevo, así que se excluye del dominio para que la propiedad mida lo que de verdad importa.
    private static readonly Gen<int> GenCoordenadaServidor = Gen.Int[0, 3_000_000];

    private static readonly Gen<PlrServerEntry> GenServer =
        Gen.Select(GenCoordenadaServidor, GenCoordenadaServidor, GenCoordenadaServidor, GenSharpString,
            (x, y, worldId, name) => new PlrServerEntry { SpawnX = x, SpawnY = y, WorldId = worldId, Name = name });

    // 0-14: rango real de PlrCreativePowersTable.PayloadTypes (15 poderes conocidos) - un
    // powerId fuera de rango hace que la propia PlrCreativePower.PayloadType lance
    // InvalidDataException (por diseño, ver PlrCreativePower.cs), así que igual que con las
    // SharpString, generar fuera de rango solo rompería el fixture, no probaría nada de
    // Terrakeep.
    private static readonly Gen<PlrCreativePower> GenPoder =
        Gen.Select(Gen.Short[0, 14], Gen.Bool, Gen.Float,
            (powerId, boolValue, floatValue) => new PlrCreativePower { PowerId = powerId, BoolValue = boolValue, FloatValue = floatValue });

    private static readonly Gen<PlrCharacter> GenCharacter =
        from version in Gen.OneOfConst(VersionBreakpoints)
        // BUG REAL ENCONTRADO POR ESTA MISMA PRUEBA (16-sep-2026, documentado en bitacora.md,
        // no arreglado aquí a propósito - ver la entrada para el porqué): con isSwitch=true
        // generado al azar, PlrBodySerializer.Read() lanzaba "Unable to read beyond the end of
        // the stream" dentro de ReadServers (desalineado mucho antes, en Bank/Safe). Causa raíz
        // real: PlrBodySerializer.Read() lee `character.IsSwitch` en 4 puntos (Guid, umbral de
        // FinishedDD2Event, orden secuencial vs entrelazado de Bank/Safe, gate de
        // DpadBindings) pero NUNCA lo ASIGNA desde los bytes - el `new PlrCharacter{...}` que
        // construye el objeto no incluye IsSwitch, así que siempre queda en su valor por
        // defecto (false), pase lo que pase en el archivo. Si Write() se hizo con
        // IsSwitch=true (algún llamador externo lo puso a mano, como hace este generador),
        // Read() decodifica esas cuatro decisiones con la rama de PC, no la de Switch, y el
        // resultado se desincroniza con el resto del stream. Comprobado además con `grep -rn
        // "\.IsSwitch\s*="` en TODO el repo (Core + App): CERO sitios ponen IsSwitch a true
        // hoy - no hay ninguna función de importar un .plr de Switch en la app real, así que
        // esta rama del formato está modelada pero inalcanzable en la práctica. Arreglarlo de
        // verdad exige decidir CÓMO se entera Read() de si el archivo es de Switch (no está en
        // los bytes; ver el comentario de PlrCrypto/PlrBodySerializer, es señal externa al
        // fichero) - eso es una decisión de producto/API, no un fix de una línea, así que queda
        // para un agente aparte. Aquí se fija a `false` (el único valor que today's app puede
        // producir de verdad) para que esta prueba mida el formato que SÍ se usa, sin ocultar
        // el hallazgo (queda documentado, no barrido).
        from isSwitch in Gen.Const(false)
        from metaVersion in Gen.UInt
        from metaFlags1 in Gen.UInt
        from metaFlags2 in Gen.UInt
        from guidPresente in Gen.Bool
        from guidValor in GenSharpString
        from name in GenSharpString
        from difficulty in Gen.Byte
        from playTimeLow in Gen.UInt
        from playTimeHigh in Gen.UInt
        from hairStyle in GenIntBorde
        from hairDye in Gen.Byte
        from team in Gen.Byte
        from hideVisual1 in Gen.Byte
        from hideVisual2 in Gen.Byte
        from hideMisc in Gen.Byte
        from gender in Gen.Byte
        from healthNow in GenIntBorde
        from healthMax in GenIntBorde
        from manaNow in GenIntBorde
        from manaMax in GenIntBorde
        from extraAccessory in Gen.Bool
        from unlockedBiomeTorches in Gen.Bool
        from usingBiomeTorches in Gen.Bool
        from extraUsingFlags in Gen.Bool.Array[7]
        from finishedDD2Event in Gen.Bool
        from taxMoney in GenIntBorde
        from pveDeaths in GenIntBorde
        from pvpDeaths in GenIntBorde
        from hairColor in GenRgb
        from skinColor in GenRgb
        from eyeColor in GenRgb
        from shirtColor in GenRgb
        from underColor in GenRgb
        from pantsColor in GenRgb
        from shoesColor in GenRgb
        from primaryLoadout in GenLoadoutPrimario
        from loadouts in GenLoadoutAlterno.Array[3]
        from inventory in GenSlots(50)
        from coins in GenSlots(4)
        from ammo in GenSlots(4)
        from equipmentItems in GenSlots(5)
        from equipmentDyes in GenSlots(5)
        from bankItems in GenSlots(40)
        from safeItems in GenSlots(40)
        from forgeItems in GenSlots(40)
        from voidItems in GenSlots(40)
        from voidVaultByte in Gen.Byte
        from buffs in GenBuff.List[0, 50]
        from servers in GenServer.List[0, 20]
        from hotbarLocked in Gen.Bool
        from hideInfo in Gen.Bool.Array[13]
        from fishingQuestsCompleted in GenIntBorde
        from dpadBindings in GenIntBorde.Array[4]
        from builderAccStatus in GenIntBorde.Array[12]
        from bartenderQuests in GenIntBorde
        from isDead in Gen.Bool
        from respawnTimer in GenIntBorde
        from lastTimeSaved1 in GenIntBorde
        from lastTimeSaved2 in GenIntBorde
        from golferScore in GenIntBorde
        from researchMysteryByte in Gen.Byte
        from research in GenResearch.List[0, 20]
        from tempItems in GenSlots(4)
        from creativePowers in GenPoder.List[0, 15]
        from superCartByte in Gen.Byte
        from currentLoadout in GenIntBorde
        from trail in Gen.Byte.Array[0, 100]
        select new PlrCharacter
        {
            Version = version,
            IsSwitch = isSwitch,
            MetaVersion = metaVersion,
            MetaFlags1 = metaFlags1,
            MetaFlags2 = metaFlags2,
            Guid = guidPresente ? guidValor : null,
            Name = name,
            Difficulty = difficulty,
            PlayTimeLow = playTimeLow,
            PlayTimeHigh = playTimeHigh,
            HairStyle = hairStyle,
            HairDye = hairDye,
            Team = team,
            HideVisual1 = hideVisual1,
            HideVisual2 = hideVisual2,
            HideMisc = hideMisc,
            Gender = gender,
            HealthNow = healthNow,
            HealthMax = healthMax,
            ManaNow = manaNow,
            ManaMax = manaMax,
            ExtraAccessory = extraAccessory,
            UnlockedBiomeTorches = unlockedBiomeTorches,
            UsingBiomeTorches = usingBiomeTorches,
            ExtraUsingFlags = extraUsingFlags,
            FinishedDD2Event = finishedDD2Event,
            TaxMoney = taxMoney,
            PveDeaths = pveDeaths,
            PvpDeaths = pvpDeaths,
            HairColor = hairColor,
            SkinColor = skinColor,
            EyeColor = eyeColor,
            ShirtColor = shirtColor,
            UnderColor = underColor,
            PantsColor = pantsColor,
            ShoesColor = shoesColor,
            PrimaryLoadout = primaryLoadout,
            Loadouts = loadouts,
            Inventory = inventory,
            Coins = coins,
            Ammo = ammo,
            EquipmentItems = equipmentItems,
            EquipmentDyes = equipmentDyes,
            BankItems = bankItems,
            SafeItems = safeItems,
            ForgeItems = forgeItems,
            VoidItems = voidItems,
            VoidVaultByte = voidVaultByte,
            Buffs = buffs,
            Servers = servers,
            HotbarLocked = hotbarLocked,
            HideInfo = hideInfo,
            FishingQuestsCompleted = fishingQuestsCompleted,
            DpadBindings = dpadBindings,
            BuilderAccStatus = builderAccStatus,
            BartenderQuests = bartenderQuests,
            IsDead = isDead,
            RespawnTimer = respawnTimer,
            LastTimeSaved1 = lastTimeSaved1,
            LastTimeSaved2 = lastTimeSaved2,
            GolferScore = golferScore,
            ResearchMysteryByte = researchMysteryByte,
            Research = research,
            TempItems = tempItems,
            CreativePowers = creativePowers,
            SuperCartByte = superCartByte,
            CurrentLoadout = currentLoadout,
            Trail = trail,
        };

    [Fact]
    public void RoundTrip_Sintetico_EsPuntoFijoTrasUnaCanonicalizacion()
    {
        GenCharacter.Sample(crudo =>
        {
            // "crudo" es basura best-effort (ver cabecera de la clase): Write() NUNCA hace
            // clamp (solo Read() hace "id > maxId -> 0", PlrItemSlot.Read), así que
            // Write(crudo) puede contener ids/valores fuera de rango que la propia
            // PRIMERA lectura normaliza. Comparar Write(crudo) contra Write(Read(Write(crudo)))
            // directamente NO sería una propiedad real del formato (se vio en la práctica: el
            // primer intento de esta prueba "encontraba" cientos de diffs falsos, todos
            // explicados por ids > maxId sin clamp en la escritura cruda - no un bug). La
            // propiedad real empieza DESPUÉS de la primera canonicalización: bytes2/bytes3 y
            // canonico1/canonico2 son ambos ya "hijos de un Read() real", el único punto en el
            // que comparar tiene sentido.
            byte[] bytes1 = PlrFile.Write(crudo);
            var canonico1 = PlrFile.Read(bytes1);

            byte[] bytes2 = PlrFile.Write(canonico1);
            var canonico2 = PlrFile.Read(bytes2);
            byte[] bytes3 = PlrFile.Write(canonico2);

            Assert.Equal(bytes2, bytes3);
            AssertMismoPersonaje(canonico1, canonico2);
        }, iter: 300, seed: null);
    }

    private static void AssertMismoPersonaje(PlrCharacter a, PlrCharacter b)
    {
        Assert.Equal(a.Version, b.Version);
        Assert.Equal(a.IsSwitch, b.IsSwitch);
        Assert.Equal(a.MetaVersion, b.MetaVersion);
        Assert.Equal(a.MetaFlags1, b.MetaFlags1);
        Assert.Equal(a.MetaFlags2, b.MetaFlags2);
        Assert.Equal(a.Guid, b.Guid);
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Difficulty, b.Difficulty);
        Assert.Equal(a.PlayTimeLow, b.PlayTimeLow);
        Assert.Equal(a.PlayTimeHigh, b.PlayTimeHigh);
        Assert.Equal(a.HairStyle, b.HairStyle);
        Assert.Equal(a.HairDye, b.HairDye);
        Assert.Equal(a.Team, b.Team);
        Assert.Equal(a.HideVisual1, b.HideVisual1);
        Assert.Equal(a.HideVisual2, b.HideVisual2);
        Assert.Equal(a.HideMisc, b.HideMisc);
        Assert.Equal(a.Gender, b.Gender);
        Assert.Equal(a.HealthNow, b.HealthNow);
        Assert.Equal(a.HealthMax, b.HealthMax);
        Assert.Equal(a.ManaNow, b.ManaNow);
        Assert.Equal(a.ManaMax, b.ManaMax);
        Assert.Equal(a.ExtraAccessory, b.ExtraAccessory);
        Assert.Equal(a.UnlockedBiomeTorches, b.UnlockedBiomeTorches);
        Assert.Equal(a.UsingBiomeTorches, b.UsingBiomeTorches);
        Assert.Equal(a.ExtraUsingFlags, b.ExtraUsingFlags);
        Assert.Equal(a.FinishedDD2Event, b.FinishedDD2Event);
        Assert.Equal(a.TaxMoney, b.TaxMoney);
        Assert.Equal(a.PveDeaths, b.PveDeaths);
        Assert.Equal(a.PvpDeaths, b.PvpDeaths);
        Assert.Equal(a.HairColor, b.HairColor);
        Assert.Equal(a.SkinColor, b.SkinColor);
        Assert.Equal(a.EyeColor, b.EyeColor);
        Assert.Equal(a.ShirtColor, b.ShirtColor);
        Assert.Equal(a.UnderColor, b.UnderColor);
        Assert.Equal(a.PantsColor, b.PantsColor);
        Assert.Equal(a.ShoesColor, b.ShoesColor);

        AssertMismoLoadout(a.PrimaryLoadout, b.PrimaryLoadout);
        Assert.Equal(a.Loadouts.Length, b.Loadouts.Length);
        for (int i = 0; i < a.Loadouts.Length; i++) AssertMismoLoadout(a.Loadouts[i], b.Loadouts[i]);

        AssertMismosSlots(a.Inventory, b.Inventory);
        AssertMismosSlots(a.Coins, b.Coins);
        AssertMismosSlots(a.Ammo, b.Ammo);
        AssertMismosSlots(a.EquipmentItems, b.EquipmentItems);
        AssertMismosSlots(a.EquipmentDyes, b.EquipmentDyes);
        AssertMismosSlots(a.BankItems, b.BankItems);
        AssertMismosSlots(a.SafeItems, b.SafeItems);
        AssertMismosSlots(a.ForgeItems, b.ForgeItems);
        AssertMismosSlots(a.VoidItems, b.VoidItems);
        AssertMismosSlots(a.TempItems, b.TempItems);
        Assert.Equal(a.VoidVaultByte, b.VoidVaultByte);

        Assert.Equal(a.Buffs.Count, b.Buffs.Count);
        for (int i = 0; i < a.Buffs.Count; i++)
        {
            Assert.Equal(a.Buffs[i].Id, b.Buffs[i].Id);
            Assert.Equal(a.Buffs[i].Time, b.Buffs[i].Time);
        }

        Assert.Equal(a.Servers.Count, b.Servers.Count);
        for (int i = 0; i < a.Servers.Count; i++)
        {
            Assert.Equal(a.Servers[i].SpawnX, b.Servers[i].SpawnX);
            Assert.Equal(a.Servers[i].SpawnY, b.Servers[i].SpawnY);
            Assert.Equal(a.Servers[i].WorldId, b.Servers[i].WorldId);
            Assert.Equal(a.Servers[i].Name, b.Servers[i].Name);
        }

        Assert.Equal(a.HotbarLocked, b.HotbarLocked);
        Assert.Equal(a.HideInfo, b.HideInfo);
        Assert.Equal(a.FishingQuestsCompleted, b.FishingQuestsCompleted);
        Assert.Equal(a.DpadBindings, b.DpadBindings);
        Assert.Equal(a.BuilderAccStatus, b.BuilderAccStatus);
        Assert.Equal(a.BartenderQuests, b.BartenderQuests);
        Assert.Equal(a.IsDead, b.IsDead);
        Assert.Equal(a.RespawnTimer, b.RespawnTimer);
        Assert.Equal(a.LastTimeSaved1, b.LastTimeSaved1);
        Assert.Equal(a.LastTimeSaved2, b.LastTimeSaved2);
        Assert.Equal(a.GolferScore, b.GolferScore);
        Assert.Equal(a.ResearchMysteryByte, b.ResearchMysteryByte);

        Assert.Equal(a.Research.Count, b.Research.Count);
        for (int i = 0; i < a.Research.Count; i++)
        {
            Assert.Equal(a.Research[i].Pid, b.Research[i].Pid);
            Assert.Equal(a.Research[i].Count, b.Research[i].Count);
        }

        Assert.Equal(a.CreativePowers.Count, b.CreativePowers.Count);
        for (int i = 0; i < a.CreativePowers.Count; i++)
        {
            Assert.Equal(a.CreativePowers[i].PowerId, b.CreativePowers[i].PowerId);
            Assert.Equal(a.CreativePowers[i].PayloadType, b.CreativePowers[i].PayloadType);
            if (a.CreativePowers[i].PayloadType == CreativePowerPayloadType.Bool)
                Assert.Equal(a.CreativePowers[i].BoolValue, b.CreativePowers[i].BoolValue);
            if (a.CreativePowers[i].PayloadType == CreativePowerPayloadType.Float)
                Assert.Equal(a.CreativePowers[i].FloatValue, b.CreativePowers[i].FloatValue);
        }

        Assert.Equal(a.SuperCartByte, b.SuperCartByte);
        Assert.Equal(a.CurrentLoadout, b.CurrentLoadout);
        Assert.Equal(a.Trail, b.Trail);
    }

    private static void AssertMismoLoadout(PlrLoadout a, PlrLoadout b)
    {
        AssertMismosSlots(a.Items, b.Items);
        AssertMismosSlots(a.Social, b.Social);
        AssertMismosSlots(a.Dyes, b.Dyes);
        Assert.Equal(a.Hide, b.Hide);
    }

    private static void AssertMismosSlots(PlrItemSlot[] a, PlrItemSlot[] b)
    {
        Assert.Equal(a.Length, b.Length);
        for (int i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i].Id, b[i].Id);
            Assert.Equal(a[i].Count, b[i].Count);
            Assert.Equal(a[i].Prefix, b[i].Prefix);
            Assert.Equal(a[i].Favorited, b[i].Favorited);
        }
    }
}
