using System.IO;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.Services;

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
            throw new InvalidOperationException("El archivo se escribió pero la relectura no confirma el nuevo modo de juego - revisa el mundo antes de seguir editando.");

        return world.WithHeader(world.Header.WithGameMode(newGameMode));
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
