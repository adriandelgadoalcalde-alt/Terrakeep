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
}
