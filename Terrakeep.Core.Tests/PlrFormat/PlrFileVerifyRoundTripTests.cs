using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.Core.Tests.PlrFormat;

// Punto 3 de la lista de funciones nuevas (17-sep-2026, "validacion de integridad antes de
// guardar"): PlrFile.VerifyRoundTrip es el guardia real que CharacterFileService.Save (App) usa
// para abortar un guardado ANTES de tocar el .plr real en disco si los propios bytes recien
// serializados no son releibles de verdad. Se prueba el guardia AISLADO aqui, con corrupcion REAL
// de bytes (nunca un booleano inventado): 1) el ultimo bloque AES-CBC volteado, que rompe el
// padding PKCS7 real y hace que la propia Decrypt() lance dentro de Read(); 2) unos bytes
// perfectamente validos-y-releibles pero que pertenecen a OTRO personaje - la capa de comparacion
// ESTRUCTURAL (nombre/version/longitud de cada contenedor) tiene que atraparlo igual, sin
// necesidad de que Read() lance nada.
public class PlrFileVerifyRoundTripTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    [Fact]
    public void VerifyRoundTrip_BytesIntactos_NoLanza()
    {
        var personaje = NuevoPersonaje("Intacto");
        byte[] bytes = PlrFile.Write(personaje);

        PlrFile.VerifyRoundTrip(bytes, personaje); // no debe lanzar - caso real de guardado sano
    }

    [Fact]
    public void VerifyRoundTrip_UltimoBloqueAesVolteado_PaddingPkcs7Invalido_Lanza()
    {
        var personaje = NuevoPersonaje("UltimoBloqueCorrupto");
        byte[] bytes = PlrFile.Write(personaje);
        byte[] corrupto = (byte[])bytes.Clone();
        // Voltear el ultimo byte del ultimo bloque AES-CBC es la corrupcion mas fiable y
        // determinista posible: PKCS7 exige que ese byte sea un relleno valido (1..16
        // repetido) - un bit volteado ahi rompe el unpadding real dentro de PlrCrypto.Decrypt,
        // exactamente la clase de fallo real que este guardia existe para atrapar (disco
        // corrupto, memoria volteada por un fallo de hardware, lo que sea).
        corrupto[^1] ^= 0xFF;

        var ex = Assert.Throws<InvalidDataException>(() => PlrFile.VerifyRoundTrip(corrupto, personaje));
        Assert.Contains("releer", ex.Message);
    }

    [Fact]
    public void VerifyRoundTrip_BytesDeOtroPersonaje_DetectaLaInconsistenciaEstructural()
    {
        // Estos bytes SON perfectamente releibles (ninguna excepcion real de Read) - lo que
        // falla es que no corresponden al personaje "original" que se le pasa a VerifyRoundTrip.
        // Ejercita la SEGUNDA capa (comparacion estructural), deterministica a proposito - no
        // depende de que una corrupcion de bytes al azar rompa justo el campo que se compara.
        var personajeEscrito = NuevoPersonaje("Ana");
        byte[] bytesDeAna = PlrFile.Write(personajeEscrito);
        var personajeQueSeEsperaba = NuevoPersonaje("Beatriz");

        var ex = Assert.Throws<InvalidDataException>(() => PlrFile.VerifyRoundTrip(bytesDeAna, personajeQueSeEsperaba));
        Assert.Contains("Ana", ex.Message);
        Assert.Contains("Beatriz", ex.Message);
    }
}
