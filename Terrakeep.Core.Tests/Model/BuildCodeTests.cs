using Terrakeep.Core.Calamity;
using Terrakeep.Core.Model;
using Xunit;

namespace Terrakeep.Core.Tests.Model;

// Codigos de build compartibles (encargo del usuario, 13-sep-2026: "exportar una build a un
// codigo de texto corto/compartible y poder importarlo en otra instalacion").
public class BuildCodeTests
{
    private static List<BuildCodeSlot> DiezVacios() => Enumerable.Repeat(BuildCodeSlot.Empty, BuildCode.SlotCount).ToList();
    private static List<int> DiezCeros() => Enumerable.Repeat(0, BuildCode.SlotCount).ToList();

    [Fact]
    public void BuildVacia_SeCodificaYDecodificaIdentica()
    {
        string code = BuildCode.Encode(DiezVacios(), DiezCeros());

        Assert.StartsWith("TKBUILD1:", code);
        bool ok = BuildCode.TryDecode(code, out var items, out var dyes, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.All(items, i => Assert.Equal(BuildCodeSlot.Empty, i));
        Assert.All(dyes, d => Assert.Equal(0, d));
    }

    [Fact]
    public void BuildConObjetosVanillaYPrefijos_RoundTripExacto()
    {
        var items = DiezVacios();
        items[0] = new BuildCodeSlot(1, ItemPrefix.Vanilla(81)); // Legendario
        items[3] = new BuildCodeSlot(5000, ItemPrefix.None);
        var dyes = DiezCeros();
        dyes[0] = 1220; // un tinte real cualquiera

        string code = BuildCode.Encode(items, dyes);
        bool ok = BuildCode.TryDecode(code, out var itemsOut, out var dyesOut, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(1, itemsOut[0].ItemId);
        Assert.Equal(81, itemsOut[0].Prefix.VanillaId);
        Assert.False(itemsOut[0].Prefix.IsCalamity);
        Assert.Equal(5000, itemsOut[3].ItemId);
        Assert.True(itemsOut[3].Prefix.IsNone);
        Assert.Equal(1220, dyesOut[0]);
    }

    [Fact]
    public void BuildConObjetosYPrefijosDeCalamity_RoundTripExacto()
    {
        var items = DiezVacios();
        items[1] = new BuildCodeSlot(CalamityIds.ItemIdBase + 42, ItemPrefix.CalamitySynthetic(CalamityIds.PrefixIdBase + 3));
        var dyes = DiezCeros();

        string code = BuildCode.Encode(items, dyes);
        bool ok = BuildCode.TryDecode(code, out var itemsOut, out _, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(CalamityIds.ItemIdBase + 42, itemsOut[1].ItemId);
        Assert.True(itemsOut[1].Prefix.IsCalamity);
        Assert.Equal(CalamityIds.PrefixIdBase + 3, itemsOut[1].Prefix.SyntheticId);
    }

    [Fact]
    public void CodigoSinLaCabeceraReal_EsInvalido()
    {
        bool ok = BuildCode.TryDecode("esto no es un codigo de build", out _, out _, out var error);

        Assert.False(ok);
        Assert.Equal(BuildCodeError.InvalidFormat, error);
    }

    [Fact]
    public void CodigoConUnSoloCaracterCambiado_SeDetectaPorElChecksum()
    {
        var items = DiezVacios();
        items[0] = new BuildCodeSlot(100, ItemPrefix.Vanilla(5));
        string code = BuildCode.Encode(items, DiezCeros());

        // Cambia el ULTIMO caracter real (no la cabecera) - simula un pegado a medias o una
        // letra cambiada a mano, el caso real que el checksum existe para cazar.
        char ultimo = code[^1];
        char distinto = ultimo == 'A' ? 'B' : 'A';
        string codeRoto = code[..^1] + distinto;

        bool ok = BuildCode.TryDecode(codeRoto, out _, out _, out var error);

        // O bien Base64/Deflate ya lo rechaza (InvalidFormat/Corrupt segun donde rompa), o bien
        // decodifica pero el checksum no cuadra (Corrupt) - lo unico que NUNCA puede pasar es
        // que decodifique bien con datos DISTINTOS sin avisar.
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void CodigoDeUnaVersionFutura_SeRechazaConElErrorReal()
    {
        // Fabrica a mano un payload con byte de version 99 (formato futuro que esta version de
        // la app, por definicion, no puede conocer todavia) - mismo mecanismo real que
        // PlrBodySerializer usa para versiones de guardado desconocidas: rechazar con claridad,
        // nunca intentar leer datos con la forma equivocada.
        using var raw = new MemoryStream();
        using (var w = new BinaryWriter(raw))
        {
            w.Write((byte)99);
            for (int i = 0; i < BuildCode.SlotCount; i++) { w.Write(0); w.Write(0); }
            for (int i = 0; i < BuildCode.SlotCount; i++) w.Write(0);
        }
        byte[] rawBytes = raw.ToArray();
        byte checksum = 0;
        foreach (byte b in rawBytes) checksum = unchecked((byte)(checksum + b));

        using var compressed = new MemoryStream();
        compressed.WriteByte(checksum);
        using (var deflate = new System.IO.Compression.DeflateStream(compressed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(rawBytes, 0, rawBytes.Length);
        string b64 = Convert.ToBase64String(compressed.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        bool ok = BuildCode.TryDecode("TKBUILD1:" + b64, out _, out _, out var error);

        Assert.False(ok);
        Assert.Equal(BuildCodeError.UnsupportedVersion, error);
    }

    [Fact]
    public void NumeroDeSlotsIncorrecto_LanzaEnVezDeGenerarUnCodigoATruncado()
    {
        Assert.Throws<ArgumentException>(() => BuildCode.Encode(DiezVacios().Take(9).ToList(), DiezCeros()));
        Assert.Throws<ArgumentException>(() => BuildCode.Encode(DiezVacios(), DiezCeros().Take(9).ToList()));
    }

    [Fact]
    public void CodigoConEspaciosAlrededor_SeAceptaIgual()
    {
        string code = BuildCode.Encode(DiezVacios(), DiezCeros());

        bool ok = BuildCode.TryDecode("  " + code + "\n", out _, out _, out var error);

        Assert.True(ok);
        Assert.Null(error);
    }
}
