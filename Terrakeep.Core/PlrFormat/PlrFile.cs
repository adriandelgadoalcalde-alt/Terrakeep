namespace Terrakeep.Core.PlrFormat;

// Punto de entrada de alto nivel para leer/escribir un .plr real (archivo en disco):
// AES-128-CBC/PKCS7 (PlrCrypto) + el cuerpo posicional completo (PlrBodySerializer).
public static class PlrFile
{
    public static PlrCharacter Read(byte[] fileBytes)
    {
        byte[] plain = PlrCrypto.Decrypt(fileBytes);
        using var stream = new MemoryStream(plain);
        using var reader = new BinaryReader(stream);
        return PlrBodySerializer.Read(reader);
    }

    public static byte[] Write(PlrCharacter character)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            PlrBodySerializer.Write(writer, character);
        }
        return PlrCrypto.Encrypt(stream.ToArray());
    }

    // Punto 3 de la lista de funciones nuevas (17-sep-2026, "validacion de integridad antes de
    // guardar"): antes de que CharacterFileService.Save (App) toque el .plr real en disco,
    // confirma AQUI, en memoria, que los bytes que se van a escribir son releibles de verdad -
    // con el mismo lector que usa la app entera (Read, arriba), nunca uno paralelo. Dos capas
    // reales, no solo "si no lanzo, vale": 1) el propio Read puede lanzar (padding PKCS7
    // invalido, un ReadString con longitud negativa, fin de stream inesperado...) si los bytes
    // estan corruptos de verdad; 2) aunque Read no lance, un desajuste de las longitudes de
    // cada contenedor/Buffs/Research frente al personaje original es la señal mas barata y mas
    // fiable de que algo se desalineo silenciosamente (mismo criterio real ya usado por
    // WorldFileService para cofres/letreros de un .wld: "el NUMERO de cofres/letreros sigue
    // siendo el mismo" antes de fiarse del contenido). Lanza InvalidDataException con un mensaje
    // tecnico real si algo no cuadra - nunca devuelve un booleano silencioso, el llamador debe
    // abortar el guardado entero sin haber tocado el archivo real todavia.
    public static void VerifyRoundTrip(byte[] writtenBytes, PlrCharacter original)
    {
        PlrCharacter reRead;
        try
        {
            reRead = Read(writtenBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"El .plr recien serializado no se puede releer: {ex.Message}", ex);
        }

        if (reRead.Name != original.Name
            || reRead.Version != original.Version
            || reRead.Inventory.Length != original.Inventory.Length
            || reRead.Coins.Length != original.Coins.Length
            || reRead.Ammo.Length != original.Ammo.Length
            || reRead.EquipmentItems.Length != original.EquipmentItems.Length
            || reRead.EquipmentDyes.Length != original.EquipmentDyes.Length
            || reRead.BankItems.Length != original.BankItems.Length
            || reRead.SafeItems.Length != original.SafeItems.Length
            || reRead.ForgeItems.Length != original.ForgeItems.Length
            || reRead.VoidItems.Length != original.VoidItems.Length
            || reRead.TempItems.Length != original.TempItems.Length
            // Buffs NO se compara aqui a proposito: su tamaño es un FIJO derivado solo de
            // Version (10/22/44 segun el umbral real, ver PlrBodySerializer.Read) - Write()
            // siempre serializa ese tamaño completo (rellenando con id=0 lo que falte) y Read()
            // siempre lo relee entero, asi que comparar Buffs.Count no detectaria nada que la
            // comparacion de Version de arriba no detecte ya ella sola; con un PlrCharacter
            // recien construido a mano (Buffs=[] antes de pasar nunca por un Read real) daria
            // ademas un falso positivo seguro.
            || reRead.Research.Count != original.Research.Count
            || reRead.Loadouts.Length != original.Loadouts.Length)
        {
            throw new InvalidDataException(
                "El .plr recien serializado se relee con datos inconsistentes respecto al personaje en memoria " +
                $"(nombre '{reRead.Name}' vs '{original.Name}', version {reRead.Version} vs {original.Version}, " +
                $"inventario {reRead.Inventory.Length} vs {original.Inventory.Length} slots).");
        }
    }
}
