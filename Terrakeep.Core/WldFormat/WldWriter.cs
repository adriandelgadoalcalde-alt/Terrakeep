namespace Terrakeep.Core.WldFormat;

// Primer (y unico, de momento) escritor real de .wld - hasta ahora el formato SOLO se leia
// (WldReader); "Solo lectura" es un badge real en toda la UI de Exploracion, con un tooltip
// explicito ("no se puede editar ni guardar desde aqui"). Pedido explicito del usuario
// (5-sep-2026): poder cambiar la dificultad del mundo (Clasico/Experto/Maestro/Viaje, los 4
// modos reales de Terraria - GameMode, ya leido por WldReader/F-14 pero nunca escrito).
//
// Alcance MINIMO a proposito: parchea UNICAMENTE el campo GameMode, en el MISMO formato/ancho
// que ya usa WldReader segun la version del archivo - nunca cambia la longitud del archivo,
// nunca toca ningun otro campo/seccion (tiles, NPCs, cofres...). Cualquier escritura mas
// general es un salto de riesgo mucho mayor que no se ha pedido.
public static class WldWriter
{
    // Localiza el offset REAL de GameMode replicando exactamente la misma secuencia de
    // lecturas que WldReader.ReadHeader hasta llegar a el (titulo/semilla/GUID son de longitud
    // variable segun el propio contenido del archivo - no hay ningun offset fijo posible) y
    // devuelve una COPIA del array de bytes con el campo sobrescrito. El array de entrada
    // nunca se modifica in-place (el llamador conserva el original intacto por si algo falla
    // despues de esta llamada y hay que descartar el intento).
    public static byte[] PatchGameMode(byte[] fileBytes, int newGameMode)
    {
        if (newGameMode is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(newGameMode), "El modo de juego real de Terraria solo tiene 4 valores (0=Clasico, 1=Experto, 2=Maestro, 3=Viaje).");

        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);

        uint version = reader.ReadUInt32();

        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");
        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision
        reader.ReadInt64();  // banderas de favorito

        short pointerCount = reader.ReadInt16();
        for (int i = 0; i < pointerCount; i++) reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5).");

        WldReader.ReadBitArray(reader); // tileFrameImportant - mismo lector que WldReader, nunca duplicado

        reader.ReadString(); // title

        if (version == 179) reader.ReadInt32(); else reader.ReadString(); // seed

        reader.ReadBytes(8); // WorldGenVersion
        if (version >= 181) reader.ReadBytes(16); // WorldGUID
        reader.ReadInt32(); // worldId
        reader.ReadBytes(16); // Left/Right/Top/BottomWorld
        reader.ReadInt32(); // tilesHigh
        reader.ReadInt32(); // tilesWide

        // Mismo criterio de ancho por version que WldReader.ReadHeader (ver su comentario F-14,
        // confirmado byte a byte contra World.FileV2.cs de TEdit, commit f592261).
        long gameModeOffset = stream.Position;
        int gameModeWidth;
        if (version >= 209) gameModeWidth = 4;
        else if (version >= 112) gameModeWidth = 1; // bool, incluye la variante Maestro==208
        else throw new NotSupportedException($"Los mundos de formato {version} (anteriores a la version 112) no tienen ningun concepto de dificultad que escribir.");

        var patched = (byte[])fileBytes.Clone();
        if (gameModeWidth == 4)
        {
            // Int32 little-endian real (>=209) - BitConverter.GetBytes ya produce ese orden en
            // cualquier arquitectura x86/x64 real, mismo criterio que el resto del proyecto.
            BitConverter.GetBytes(newGameMode).CopyTo(patched, (int)gameModeOffset);
        }
        else
        {
            // version 208: Maestro(2)->true, Clasico(0)->false (nunca hubo Experto/Viaje en un
            // mundo de esta version). version 112..207: Experto(1)->true, Clasico(0)->false.
            bool asBool = version == 208 ? newGameMode == 2 : newGameMode == 1;
            if (!SupportsGameMode(version, newGameMode))
                throw new NotSupportedException($"Los mundos de formato {version} solo admiten Clasico/{(version == 208 ? "Maestro" : "Experto")} - el modo pedido no existia todavia en esta version de Terraria.");
            patched[gameModeOffset] = (byte)(asBool ? 1 : 0);
        }
        return patched;
    }

    // La MISMA regla que aplica PatchGameMode (que la usa, para que no puedan divergir), expuesta
    // aparte para que la interfaz pueda decirlo ANTES en vez de dejar pulsar "Guardar" y responder
    // con una excepcion: en un mundo anterior a la version 209 el campo GameMode es un simple bool
    // y la mitad de los modos ni siquiera existian todavia en el juego.
    //   >= 209 -> Int32 real: los 4 modos (0=Clasico, 1=Experto, 2=Maestro, 3=Viaje).
    //   == 208 -> bool "maestro": solo Clasico y Maestro.
    //   112..207 -> bool "experto": solo Clasico y Experto.
    //   < 112 -> el concepto de dificultad no existe en el archivo.
    public static bool SupportsGameMode(uint version, int gameMode)
    {
        if (gameMode is < 0 or > 3) return false;
        if (version >= 209) return true;
        if (version == 208) return gameMode is 0 or 2;
        if (version >= 112) return gameMode is 0 or 1;
        return false;
    }

    // Editor de mundos v1 (14-sep-2026, guia real de bitacora.md 13-sep-2026): offsets reales de
    // TODO el tramo SpawnX..HardMode, calculados UNA sola vez replicando exactamente la misma
    // secuencia de lecturas que WldReader.ReadHeader (confirmado que es de ancho FIJO, sin
    // ningun string variable de por medio - ver el comentario real de WldHeader). Los tres
    // Patch* de abajo comparten este UNICO calculo para que nunca puedan divergir entre si -
    // mismo criterio real que WldReader.ReadBitArray, reutilizado por PatchGameMode.
    private readonly record struct HeaderOffsets(
        uint Version, long SpawnX, long SpawnY, long Time, long DayTime, long MoonPhase, long BloodMoon, long IsEclipse,
        long IsCrimson, long DownedBoss1, long DownedBoss2, long DownedBoss3, long DownedQueenBee,
        long DownedMech1, long DownedMech2, long DownedMech3, long DownedPlant, long DownedGolem,
        long? DownedSlimeKing, long HardMode);

    private static HeaderOffsets ComputeHeaderOffsets(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);

        uint version = reader.ReadUInt32();
        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");
        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision
        reader.ReadInt64();  // banderas de favorito

        short pointerCount = reader.ReadInt16();
        for (int i = 0; i < pointerCount; i++) reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5).");

        WldReader.ReadBitArray(reader); // tileFrameImportant

        reader.ReadString(); // title
        if (version == 179) reader.ReadInt32(); else reader.ReadString(); // seed

        reader.ReadBytes(8); // WorldGenVersion
        if (version >= 181) reader.ReadBytes(16); // WorldGUID
        reader.ReadInt32(); // worldId
        reader.ReadBytes(16); // Left/Right/Top/BottomWorld
        reader.ReadInt32(); // tilesHigh
        reader.ReadInt32(); // tilesWide

        // Mismo criterio de ancho por version que WldReader.ReadHeader/PatchGameMode.
        if (version >= 209)
        {
            reader.ReadInt32();
            if (version >= 222) reader.ReadBoolean();
            if (version >= 227) reader.ReadBoolean();
            if (version >= 238) reader.ReadBoolean();
            if (version >= 239) reader.ReadBoolean();
            if (version >= 241) reader.ReadBoolean();
            if (version >= 249) reader.ReadBoolean();
            if (version >= 266) reader.ReadBoolean();
            if (version >= 267) reader.ReadBoolean();
            if (version >= 302) reader.ReadBoolean();
        }
        else if (version >= 112)
        {
            reader.ReadBoolean();
        }

        if (version >= 141) reader.ReadBytes(8); // CreationTime
        if (version >= 284) reader.ReadBytes(8); // LastPlayed
        reader.ReadByte(); // MoonType
        reader.ReadBytes(4 * 3); // TreeX
        reader.ReadBytes(4 * 4); // TreeStyle
        reader.ReadBytes(4 * 3); // CaveBackX
        reader.ReadBytes(4 * 4); // CaveBackStyle
        reader.ReadBytes(4 * 3); // Ice/Jungle/HellBackStyle

        long spawnXOffset = stream.Position; reader.ReadInt32();
        long spawnYOffset = stream.Position; reader.ReadInt32();
        reader.ReadDouble(); // groundLevel
        reader.ReadDouble(); // rockLevel
        long timeOffset = stream.Position; reader.ReadDouble();
        long dayTimeOffset = stream.Position; reader.ReadBoolean();
        long moonPhaseOffset = stream.Position; reader.ReadInt32();
        long bloodMoonOffset = stream.Position; reader.ReadBoolean();
        long isEclipseOffset = stream.Position; reader.ReadBoolean();
        reader.ReadInt32(); // dungeonX
        reader.ReadInt32(); // dungeonY

        long isCrimsonOffset = stream.Position; reader.ReadBoolean();
        long downedBoss1Offset = stream.Position; reader.ReadBoolean();
        long downedBoss2Offset = stream.Position; reader.ReadBoolean();
        long downedBoss3Offset = stream.Position; reader.ReadBoolean();
        long downedQueenBeeOffset = stream.Position; reader.ReadBoolean();
        long downedMech1Offset = stream.Position; reader.ReadBoolean();
        long downedMech2Offset = stream.Position; reader.ReadBoolean();
        long downedMech3Offset = stream.Position; reader.ReadBoolean();
        reader.ReadBoolean(); // DownedMechBossAny
        long downedPlantOffset = stream.Position; reader.ReadBoolean();
        long downedGolemOffset = stream.Position; reader.ReadBoolean();
        long? downedSlimeKingOffset = null;
        if (version >= 118) { downedSlimeKingOffset = stream.Position; reader.ReadBoolean(); }

        reader.ReadBoolean(); // SavedGoblin
        reader.ReadBoolean(); // SavedWizard
        reader.ReadBoolean(); // SavedMech
        reader.ReadBoolean(); // DownedGoblins
        reader.ReadBoolean(); // DownedClown
        reader.ReadBoolean(); // DownedFrost
        reader.ReadBoolean(); // DownedPirates
        reader.ReadBoolean(); // ShadowOrbSmashed
        reader.ReadBoolean(); // SpawnMeteor
        reader.ReadByte();    // ShadowOrbCount
        reader.ReadInt32();   // AltarCount
        long hardModeOffset = stream.Position;

        return new HeaderOffsets(version, spawnXOffset, spawnYOffset, timeOffset, dayTimeOffset, moonPhaseOffset,
            bloodMoonOffset, isEclipseOffset, isCrimsonOffset, downedBoss1Offset, downedBoss2Offset, downedBoss3Offset,
            downedQueenBeeOffset, downedMech1Offset, downedMech2Offset, downedMech3Offset, downedPlantOffset,
            downedGolemOffset, downedSlimeKingOffset, hardModeOffset);
    }

    // Punto de aparicion del mundo (WorldGen.spawnTile real) - un Int32 par, sin ninguna
    // restriccion de version (existe desde el formato mas antiguo que este lector admite). El
    // llamador (WorldFileService.SaveSpawnPoint) es quien valida que el punto cae dentro del
    // mundo - aqui solo se escribe, sin decidir si el valor tiene sentido.
    public static byte[] PatchSpawnPoint(byte[] fileBytes, int newSpawnX, int newSpawnY)
    {
        var o = ComputeHeaderOffsets(fileBytes);
        var patched = (byte[])fileBytes.Clone();
        BitConverter.GetBytes(newSpawnX).CopyTo(patched, (int)o.SpawnX);
        BitConverter.GetBytes(newSpawnY).CopyTo(patched, (int)o.SpawnY);
        return patched;
    }

    // Hora del dia + fase lunar + luna de sangre/eclipse - los 5 campos son un tramo contiguo
    // real (Main.time/dayTime/moonPhase/bloodMoon/eclipse, ver World.FileV2.cs de TEdit), se
    // escriben juntos porque en el juego real tambien cambian juntos (un DayTime que no
    // corresponde al rango real de Time no tiene sentido - ver la conversion real en
    // ExplorationViewModel).
    public static byte[] PatchTimeAndMoon(byte[] fileBytes, double newTime, bool newDayTime, int newMoonPhase, bool newBloodMoon, bool newIsEclipse)
    {
        if (newMoonPhase is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(newMoonPhase), "La fase lunar real de Terraria solo tiene 8 valores (0-7).");
        if (newTime < 0)
            throw new ArgumentOutOfRangeException(nameof(newTime), "El reloj del mundo no puede ser negativo.");

        var o = ComputeHeaderOffsets(fileBytes);
        var patched = (byte[])fileBytes.Clone();
        BitConverter.GetBytes(newTime).CopyTo(patched, (int)o.Time);
        patched[o.DayTime] = (byte)(newDayTime ? 1 : 0);
        BitConverter.GetBytes(newMoonPhase).CopyTo(patched, (int)o.MoonPhase);
        patched[o.BloodMoon] = (byte)(newBloodMoon ? 1 : 0);
        patched[o.IsEclipse] = (byte)(newIsEclipse ? 1 : 0);
        return patched;
    }

    // Banderas de progreso ("jefes derrotados", pedido explicito de bitacora.md 13-sep-2026,
    // punto 6/paneles de progreso). Cada campo es un `bool?` real: null significa "no tocar este
    // campo" (deja el valor que ya hubiera en el archivo) - asi la interfaz puede mandar solo LOS
    // que el usuario cambio de verdad, sin tener que releer y repetir los otros 10.
    public readonly record struct WorldFlagsPatch(
        bool? DownedBoss1EyeOfCthulhu = null, bool? DownedBoss2EaterOfWorldsOrBrainOfCthulhu = null,
        bool? DownedBoss3Skeletron = null, bool? DownedQueenBee = null, bool? DownedMechBoss1TheDestroyer = null,
        bool? DownedMechBoss2TheTwins = null, bool? DownedMechBoss3SkeletronPrime = null, bool? DownedPlantBoss = null,
        bool? DownedGolemBoss = null, bool? DownedSlimeKingBoss = null, bool? HardMode = null);

    public static byte[] PatchBossFlags(byte[] fileBytes, WorldFlagsPatch patch)
    {
        var o = ComputeHeaderOffsets(fileBytes);
        if (patch.DownedSlimeKingBoss is not null && o.DownedSlimeKing is null)
            throw new NotSupportedException("Los mundos de formato anterior a la version 118 no tienen ningun Rey Slime que marcar - el campo ni siquiera existe en el archivo.");

        var patched = (byte[])fileBytes.Clone();
        void Write(long offset, bool? value) { if (value is bool v) patched[offset] = (byte)(v ? 1 : 0); }
        Write(o.DownedBoss1, patch.DownedBoss1EyeOfCthulhu);
        Write(o.DownedBoss2, patch.DownedBoss2EaterOfWorldsOrBrainOfCthulhu);
        Write(o.DownedBoss3, patch.DownedBoss3Skeletron);
        Write(o.DownedQueenBee, patch.DownedQueenBee);
        Write(o.DownedMech1, patch.DownedMechBoss1TheDestroyer);
        Write(o.DownedMech2, patch.DownedMechBoss2TheTwins);
        Write(o.DownedMech3, patch.DownedMechBoss3SkeletronPrime);
        Write(o.DownedPlant, patch.DownedPlantBoss);
        Write(o.DownedGolem, patch.DownedGolemBoss);
        if (o.DownedSlimeKing is long slimeKingOffset) Write(slimeKingOffset, patch.DownedSlimeKingBoss);
        Write(o.HardMode, patch.HardMode);
        return patched;
    }
}
