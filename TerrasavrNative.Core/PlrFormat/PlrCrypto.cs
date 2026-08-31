using System.Security.Cryptography;
using System.Text;

namespace TerrasavrNative.Core.PlrFormat;

// Cifrado del .plr vanilla: AES-128-CBC sobre TODO el cuerpo (incluida la version, no hay
// ninguna cabecera sin cifrar), clave = IV, ambos derivados de la cadena "h3y_gUyZ" (misma
// clave publica ya conocida de otras herramientas de terceros, confirmada literal en
// script.js real: H.encrypt/H.__init_crypto usan utf2ints("h3y_gUyZ") como clave). utf2ints
// convierte cada caracter en 2 bytes (bajo primero, alto despues) - exactamente UTF-16LE de
// una cadena ASCII de 8 caracteres = 16 bytes = tamaño de clave AES-128.
public static class PlrCrypto
{
    private static readonly byte[] KeyAndIv = Encoding.Unicode.GetBytes("h3y_gUyZ");

    public static byte[] Decrypt(byte[] cipherBytes)
    {
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Key = KeyAndIv;
        aes.IV = KeyAndIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }

    public static byte[] Encrypt(byte[] plainBytes)
    {
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Key = KeyAndIv;
        aes.IV = KeyAndIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }
}
