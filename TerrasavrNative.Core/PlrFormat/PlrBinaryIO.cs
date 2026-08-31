using System.Text;

namespace TerrasavrNative.Core.PlrFormat;

// Todo el .plr vanilla (una vez descifrado) es little-endian - BinaryReader/BinaryWriter de
// .NET ya son little-endian por defecto para los primitivos (ReadInt32/ReadUInt32/ReadByte/
// ReadSingle...), así que se usan directamente. Lo único que necesita ayuda propia es el
// formato de string real del juego ("SharpString": length-prefix de 1 byte crudo + UTF-8) -
// DISTINTO del BinaryReader.ReadString()/Write(string) nativos de .NET, que usan un entero de
// longitud codificado en 7 bits variables (coincide con 1 byte solo para strings cortas, pero
// diverge para las de 128+ bytes) - confirmado contra H.readSharpString/writeSharpString en
// script.js real.
public static class PlrBinaryIO
{
    public static string ReadSharpString(this BinaryReader reader)
    {
        int len = reader.ReadByte();
        if (len == 0) return string.Empty;
        byte[] bytes = reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    public static void WriteSharpString(this BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > byte.MaxValue)
            throw new InvalidDataException($"SharpString demasiado larga ({bytes.Length} bytes UTF-8, maximo {byte.MaxValue}).");
        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }
}
