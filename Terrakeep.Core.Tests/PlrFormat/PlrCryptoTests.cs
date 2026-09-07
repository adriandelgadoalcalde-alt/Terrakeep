using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.Core.Tests.PlrFormat;

public class PlrCryptoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(200)]
    public void RoundTrip_ArbitraryLength(int length)
    {
        var plain = new byte[length];
        new Random(42).NextBytes(plain);

        byte[] cipher = PlrCrypto.Encrypt(plain);
        byte[] decrypted = PlrCrypto.Decrypt(cipher);

        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Encrypt_PadsToMultipleOf16()
    {
        byte[] cipher = PlrCrypto.Encrypt(new byte[5]);
        Assert.Equal(0, cipher.Length % 16);
    }
}
