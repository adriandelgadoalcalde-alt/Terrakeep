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
}
