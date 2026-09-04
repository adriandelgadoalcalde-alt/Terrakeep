using TerrasavrNative.Core.Model;
using Xunit;

namespace TerrasavrNative.Core.Tests.Model;

// H6-02 (sexta auditoria de Opus): Gender es el skinVariant real (0-11), no un booleano - ver
// el comentario real de PlayerVariantSets.cs (cita de Terraria.ID.PlayerVariantID.cs
// decompilado). Antes de este arreglo, un varon normal (skinVariant=0, el caso mas comun) se
// leia como "Chica" - IsMale(0) DEBE dar true, no false.
public class PlayerVariantSetsTests
{
    [Theory]
    [InlineData(0, true)]   // MaleStarter - el varon "normal", el caso real mas comun
    [InlineData(1, true)]   // MaleSticker
    [InlineData(2, true)]   // MaleGangster
    [InlineData(3, true)]   // MaleCoat
    [InlineData(8, true)]   // MaleDress
    [InlineData(10, true)]  // MaleDisplayDoll
    [InlineData(4, false)]  // FemaleStarter
    [InlineData(5, false)]  // FemaleSticker
    [InlineData(6, false)]  // FemaleGangster
    [InlineData(7, false)]  // FemaleCoat
    [InlineData(9, false)]  // FemaleDress
    [InlineData(11, false)] // FemaleDisplayDoll
    public void IsMale_CoincideConPlayerVariantIDSetsMaleReal(byte skinVariant, bool esperado)
    {
        Assert.Equal(esperado, PlayerVariantSets.IsMale(skinVariant));
    }

    [Fact]
    public void MaleStarterYFemaleStarter_SonConsistentesConIsMale()
    {
        Assert.True(PlayerVariantSets.IsMale(PlayerVariantSets.MaleStarter));
        Assert.False(PlayerVariantSets.IsMale(PlayerVariantSets.FemaleStarter));
    }

    // H6-01-b (advisor Opus): las 10 variantes reales 0-9 usan su propia carpeta extraida por
    // scripts/extraer-sprites-jugador.js (con herencia real resuelta en extraccion); 10/11
    // (DisplayDoll, el maniqui del guardarropa) caen a la Starter de su genero, fuera de
    // alcance deliberado.
    [Theory]
    [InlineData(0, "body0")]
    [InlineData(1, "body1")]
    [InlineData(2, "body2")]
    [InlineData(3, "body3")]
    [InlineData(4, "body4")]
    [InlineData(5, "body5")]
    [InlineData(6, "body6")]
    [InlineData(7, "body7")]
    [InlineData(8, "body8")] // el caso real "Eldelgas" (MaleDress)
    [InlineData(9, "body9")]
    [InlineData(10, "body0")] // MaleDisplayDoll, fuera de alcance -> Starter varon
    [InlineData(11, "body4")] // FemaleDisplayDoll, fuera de alcance -> Starter mujer
    public void BodyFolder_DevuelveLaCarpetaRealConHerenciaResueltaEnExtraccion(byte skinVariant, string esperado)
    {
        Assert.Equal(esperado, PlayerVariantSets.BodyFolder(skinVariant));
    }
}
