using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Hallazgo real de la oleada del 6-sep-2026 (area "Exploracion del mundo"): la interfaz ofrecia
// los CUATRO chips de dificultad en cualquier mundo, y el unico sitio que sabia la verdad era
// PatchGameMode - por dentro, lanzando NotSupportedException cuando ya era tarde (el usuario ya
// habia pulsado "Guardar" sobre su archivo real). WldWriter.SupportsGameMode expone esa MISMA
// regla para poder decirlo antes; PatchGameMode la usa, asi que no pueden divergir.
//
// La regla no es inventada: sale del propio formato .wld, ya replicado byte a byte en
// PatchGameMode/WldReader contra World.FileV2.cs de TEdit.
//   - >= 209: GameMode es un Int32 real -> los 4 modos.
//   - == 208: bool "es maestro" -> solo Clasico y Maestro (Experto/Viaje no existian asi).
//   - 112..207: bool "es experto" -> solo Clasico y Experto.
//   - < 112: el archivo no tiene ningun concepto de dificultad.
public class WldWriterSupportsGameModeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Version279_AdmiteLosCuatroModos(int modo) =>
        Assert.True(WldWriter.SupportsGameMode(279, modo));

    [Theory]
    [InlineData(0, true)]  // Clasico
    [InlineData(1, false)] // Experto: en v208 el bool significa "maestro", no "experto"
    [InlineData(2, true)]  // Maestro
    [InlineData(3, false)] // Viaje: no existia todavia
    public void Version208_SoloClasicoYMaestro(int modo, bool esperado) =>
        Assert.Equal(esperado, WldWriter.SupportsGameMode(208, modo));

    [Theory]
    [InlineData(0, true)]  // Clasico
    [InlineData(1, true)]  // Experto
    [InlineData(2, false)] // Maestro: no existia todavia
    [InlineData(3, false)] // Viaje: tampoco
    public void Version150_SoloClasicoYExperto(int modo, bool esperado) =>
        Assert.Equal(esperado, WldWriter.SupportsGameMode(150, modo));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VersionAnteriorALa112_NoAdmiteNinguno(int modo) =>
        Assert.False(WldWriter.SupportsGameMode(71, modo));

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ModoFueraDeLosCuatroReales_NuncaSeAdmite(int modoInvalido) =>
        Assert.False(WldWriter.SupportsGameMode(279, modoInvalido));

    // La garantia que de verdad importa: lo que SupportsGameMode dice que no se puede es
    // exactamente lo que PatchGameMode rechaza, y lo que dice que si, PatchGameMode lo escribe.
    // Sin esto, las dos podrian separarse en el futuro sin que nadie se enterara.
    [Theory]
    [InlineData(279u)]
    [InlineData(208u)]
    [InlineData(150u)]
    public void LoQueDiceSupports_EsExactamenteLoQuePatchHace(uint version)
    {
        byte[] original = ConstruirCabecera(version);
        for (int modo = 0; modo <= 3; modo++)
        {
            bool admitido = WldWriter.SupportsGameMode(version, modo);
            if (admitido)
            {
                byte[] parcheado = WldWriter.PatchGameMode(original, modo);
                Assert.Equal(modo, WldReader.ReadHeader(parcheado).GameMode);
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => WldWriter.PatchGameMode(original, modo));
            }
        }
    }

    // Misma cabecera sintetica que WldWriterTests (mismo orden de campos exacto que
    // WldReader.ReadHeader) - reducida a lo que estas pruebas necesitan.
    private static byte[] ConstruirCabecera(uint version)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(version);
        w.Write("relogic".ToCharArray());
        w.Write((byte)2);
        w.Write((uint)1);
        w.Write((long)0);

        var pointers = new int[] { 0, 0, 0, 0, 0 };
        w.Write((short)pointers.Length);
        foreach (int p in pointers) w.Write(p);
        w.Write((short)0); // tileFrameImportant vacio

        w.Write("Mundo sintetico");
        if (version == 179) w.Write(0); else w.Write("semilla");
        w.Write(new byte[8]);
        if (version >= 181) w.Write(new byte[16]);
        w.Write(1);            // worldId
        w.Write(new byte[16]); // Left/Right/Top/BottomWorld
        w.Write(100);          // tilesHigh
        w.Write(100);          // tilesWide

        if (version >= 209)
        {
            w.Write(0);
            if (version >= 222) w.Write(false);
            if (version >= 227) w.Write(false);
            if (version >= 238) w.Write(false);
            if (version >= 239) w.Write(false);
            if (version >= 241) w.Write(false);
            if (version >= 249) w.Write(false);
            if (version >= 266) w.Write(false);
            if (version >= 267) w.Write(false);
            if (version >= 302) w.Write(false);
        }
        else if (version >= 112) w.Write(false);

        if (version >= 141) w.Write(new byte[8]);
        if (version >= 284) w.Write(new byte[8]);
        w.Write((byte)0);
        w.Write(new byte[4 * 3]);
        w.Write(new byte[4 * 4]);
        w.Write(new byte[4 * 3]);
        w.Write(new byte[4 * 4]);
        w.Write(new byte[4 * 3]);
        w.Write(5); w.Write(5);      // spawn
        w.Write(50.0); w.Write(100.0);
        w.Write(0.0); w.Write(false); w.Write(0); w.Write(false); w.Write(false);
        w.Write(10); w.Write(10);    // dungeon
        w.Flush();
        return ms.ToArray();
    }
}
