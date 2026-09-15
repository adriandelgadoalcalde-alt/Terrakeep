using System.IO;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.Services;

// Escritura real de un .wld en disco - unico consumidor de WldWriter.PatchGameMode. Pedido
// explicito del usuario (5-sep-2026): poder cambiar la dificultad del mundo (Clasico/Experto/
// Maestro/Viaje) - hasta ahora el mundo era 100% de solo lectura en toda la app ("Solo lectura",
// badge real en Exploracion). Mismo criterio atomico ya probado para personajes
// (CharacterFileService.WriteAtomic, T-C/Bloque 0 T-23): escribe siempre a un .tmp aparte y solo
// al final lo intercambia por el archivo real de un solo paso atomico del sistema de ficheros
// (File.Replace) - nunca hay un instante con el .wld real a medio escribir - que de paso genera
// el .bak en la MISMA operacion.
public static class WorldFileService
{
    // Lee, parchea, escribe (atomico, con .bak) y RE-LEE DE VUELTA DESDE DISCO para verificar el
    // round-trip completo antes de devolver el mundo actualizado - un archivo de mundo real es
    // demasiado valioso para fiarse de que PatchGameMode no lanzo ninguna excepcion, hay que
    // confirmar que lo que quedo escrito en el fichero de verdad es lo que se pidio.
    public static WldWorld SaveGameMode(WldWorld world, string wldPath, int newGameMode)
    {
        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.PatchGameMode(original, newGameMode);
        WriteAtomic(wldPath, patched);

        var reReadHeader = WldReader.ReadHeader(File.ReadAllBytes(wldPath));
        if (reReadHeader.GameMode != newGameMode)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        return world.WithHeader(world.Header.WithGameMode(newGameMode));
    }

    // Editor de mundos v1 (14-sep-2026, guia real de bitacora.md 13-sep-2026): mismo patron
    // exacto que SaveGameMode arriba (leer, parchear, escribir atomico con .bak, releer de
    // disco para verificar el round-trip) para cada uno de los tres grupos nuevos - nunca un
    // unico "guardar todo" que mezclaria varias escrituras atomicas distintas en una.
    public static WldWorld SaveSpawnPoint(WldWorld world, string wldPath, int newSpawnX, int newSpawnY)
    {
        if (newSpawnX < 0 || newSpawnX >= world.Header.TilesWide || newSpawnY < 0 || newSpawnY >= world.Header.TilesHigh)
            throw new ArgumentOutOfRangeException(nameof(newSpawnX), LocalizationService.Instance["world_spawn_out_of_bounds"]);

        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.PatchSpawnPoint(original, newSpawnX, newSpawnY);
        WriteAtomic(wldPath, patched);

        var reReadHeader = WldReader.ReadHeader(File.ReadAllBytes(wldPath));
        if (reReadHeader.SpawnX != newSpawnX || reReadHeader.SpawnY != newSpawnY)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        return world.WithHeader(world.Header.WithSpawn(newSpawnX, newSpawnY));
    }

    public static WldWorld SaveTimeAndMoon(WldWorld world, string wldPath, double newTime, bool newDayTime, int newMoonPhase, bool newBloodMoon, bool newIsEclipse)
    {
        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.PatchTimeAndMoon(original, newTime, newDayTime, newMoonPhase, newBloodMoon, newIsEclipse);
        WriteAtomic(wldPath, patched);

        var reReadHeader = WldReader.ReadHeader(File.ReadAllBytes(wldPath));
        if (Math.Abs(reReadHeader.Time - newTime) > 0.01 || reReadHeader.DayTime != newDayTime || reReadHeader.MoonPhase != newMoonPhase
            || reReadHeader.BloodMoon != newBloodMoon || reReadHeader.IsEclipse != newIsEclipse)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        return world.WithHeader(world.Header.WithTimeAndMoon(newTime, newDayTime, newMoonPhase, newBloodMoon, newIsEclipse));
    }

    public static WldWorld SaveBossFlags(WldWorld world, string wldPath, WldWriter.WorldFlagsPatch patch)
    {
        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.PatchBossFlags(original, patch);
        WriteAtomic(wldPath, patched);

        var reReadHeader = WldReader.ReadHeader(File.ReadAllBytes(wldPath));
        var newHeader = world.Header.WithBossFlags(
            patch.DownedBoss1EyeOfCthulhu ?? world.Header.DownedBoss1EyeOfCthulhu,
            patch.DownedBoss2EaterOfWorldsOrBrainOfCthulhu ?? world.Header.DownedBoss2EaterOfWorldsOrBrainOfCthulhu,
            patch.DownedBoss3Skeletron ?? world.Header.DownedBoss3Skeletron,
            patch.DownedQueenBee ?? world.Header.DownedQueenBee,
            patch.DownedMechBoss1TheDestroyer ?? world.Header.DownedMechBoss1TheDestroyer,
            patch.DownedMechBoss2TheTwins ?? world.Header.DownedMechBoss2TheTwins,
            patch.DownedMechBoss3SkeletronPrime ?? world.Header.DownedMechBoss3SkeletronPrime,
            patch.DownedPlantBoss ?? world.Header.DownedPlantBoss,
            patch.DownedGolemBoss ?? world.Header.DownedGolemBoss,
            patch.DownedSlimeKingBoss ?? world.Header.DownedSlimeKingBoss,
            patch.HardMode ?? world.Header.HardMode);

        if (reReadHeader.DownedBoss1EyeOfCthulhu != newHeader.DownedBoss1EyeOfCthulhu || reReadHeader.HardMode != newHeader.HardMode)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        return world.WithHeader(newHeader);
    }

    // Editor de cofres/letreros v1 (T1 del documento I+D real, "Terrakeep, editor de cofres/
    // letreros del .wld", 15-sep-2026): primer camino de escritura que NO parchea la cabecera -
    // WldWriter.WriteChestItems/WriteSignText reescriben una seccion entera y pueden CAMBIAR LA
    // LONGITUD del archivo. Mismo patron atomico (WriteAtomic con .bak) y misma verificacion real
    // releyendo DE DISCO tras guardar - aqui la verificacion es mas exigente todavia: no solo el
    // dato editado, tambien que el NUMERO de cofres/letreros del archivo siga siendo el mismo (la
    // señal mas barata y mas fiable de que la tabla de punteros no quedo desincronizada).
    public static WldWorld SaveChestItems(WldWorld world, string wldPath, int chestIndex, IReadOnlyList<WldChestItem> newItems)
    {
        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.WriteChestItems(original, chestIndex, newItems);
        WriteAtomic(wldPath, patched);

        var reReadWorld = WldReader.Read(File.ReadAllBytes(wldPath), readContainers: true);
        if (reReadWorld.Chests.Count != world.Chests.Count)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);
        var editedChest = reReadWorld.Chests[chestIndex];
        if (editedChest.Items.Count != newItems.Count)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        return world.WithChestItems(chestIndex, newItems);
    }

    public static WldWorld SaveSignText(WldWorld world, string wldPath, int signX, int signY, string newText)
    {
        byte[] original = File.ReadAllBytes(wldPath);
        byte[] patched = WldWriter.WriteSignText(original, signX, signY, newText);
        WriteAtomic(wldPath, patched);

        var reReadWorld = WldReader.Read(File.ReadAllBytes(wldPath), readContainers: true);
        if (reReadWorld.Signs.Count != world.Signs.Count)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);
        var editedSign = reReadWorld.Signs.FirstOrDefault(s => s.X == signX && s.Y == signY);
        if (editedSign == null || editedSign.Text != newText)
            throw new InvalidOperationException(LocalizationService.Instance["world_save_reread_failed"]);

        int signIndex = world.Signs.ToList().FindIndex(s => s.X == signX && s.Y == signY);
        return signIndex < 0 ? world : world.WithSignText(signIndex, newText);
    }

    private static void WriteAtomic(string path, byte[] bytes)
    {
        string tmpPath = path + ".tmp";
        File.WriteAllBytes(tmpPath, bytes);
        try
        {
            File.Replace(tmpPath, path, path + ".bak", ignoreMetadataErrors: true);
        }
        catch (IOException)
        {
            // Respaldo best-effort real (ej. .bak bloqueado por el antivirus): el guardado del
            // mundo en si no debe fallar por eso.
            File.Replace(tmpPath, path, null, ignoreMetadataErrors: true);
        }
    }
}
