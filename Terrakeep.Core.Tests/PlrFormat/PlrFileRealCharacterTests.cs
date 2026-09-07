using Terrakeep.Core.PlrFormat;
using Xunit;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.PlrFormat;

// Prueba de humo/round-trip contra .plr REALES de este PC (Documents\My Games\Terraria\
// tModLoader\Players\) - no fixtures. Se salta en silencio si la carpeta no existe en la
// maquina donde corran los tests (igual criterio que RealDataFilesSmokeTests.cs).
public class PlrFileRealCharacterTests(ITestOutputHelper output)
{
    private const string PlayersDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players";

    public static IEnumerable<object[]> RealPlrFiles()
    {
        if (!Directory.Exists(PlayersDir)) yield break;
        yield return [Path.Combine(PlayersDir, "Eldelgas.plr")];
        yield return [Path.Combine(PlayersDir, "adrian.plr")];
    }

    [Theory]
    [MemberData(nameof(RealPlrFiles))]
    public void Read_RealFile_DoesNotThrow_AndProducesSaneCharacter(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileBytes = File.ReadAllBytes(path);
        var character = PlrFile.Read(fileBytes);

        output.WriteLine($"{Path.GetFileName(path)}: nombre='{character.Name}', version={character.Version}, vida={character.HealthNow}/{character.HealthMax}, trail={character.Trail.Length} bytes");

        Assert.False(string.IsNullOrEmpty(character.Name));
        Assert.True(character.Version >= 145);
        Assert.True(character.HealthMax > 0);
        Assert.True(character.ManaMax >= 0);
    }

    [Theory]
    [MemberData(nameof(RealPlrFiles))]
    public void RoundTrip_RealFile_ReadWriteRead_ProducesIdenticalCharacter(string path)
    {
        if (!File.Exists(path)) return;

        byte[] original = File.ReadAllBytes(path);
        var character = PlrFile.Read(original);

        byte[] rewritten = PlrFile.Write(character);
        var reReadCharacter = PlrFile.Read(rewritten);

        Assert.Equal(character.Name, reReadCharacter.Name);
        Assert.Equal(character.Version, reReadCharacter.Version);
        Assert.Equal(character.HealthNow, reReadCharacter.HealthNow);
        Assert.Equal(character.HealthMax, reReadCharacter.HealthMax);
        Assert.Equal(character.ManaMax, reReadCharacter.ManaMax);
        Assert.Equal(character.HairColor, reReadCharacter.HairColor);
        Assert.Equal(character.Buffs.Count, reReadCharacter.Buffs.Count);
        Assert.Equal(character.Research.Count, reReadCharacter.Research.Count);
        Assert.Equal(character.Trail, reReadCharacter.Trail);

        for (int i = 0; i < character.Inventory.Length; i++)
        {
            Assert.Equal(character.Inventory[i].Id, reReadCharacter.Inventory[i].Id);
            Assert.Equal(character.Inventory[i].Count, reReadCharacter.Inventory[i].Count);
            Assert.Equal(character.Inventory[i].Prefix, reReadCharacter.Inventory[i].Prefix);
        }
    }

    [Theory]
    [MemberData(nameof(RealPlrFiles))]
    public void RoundTrip_RealFile_ProducesByteIdenticalOutput(string path)
    {
        if (!File.Exists(path)) return;

        byte[] original = File.ReadAllBytes(path);
        var character = PlrFile.Read(original);
        byte[] rewritten = PlrFile.Write(character);

        // Si esto falla pero los dos tests de arriba pasan, el lector/escritor son
        // logicamente correctos pero algo no vuelve BYTE a byte (p.ej. Trail mal delimitado,
        // o algun campo con multiples representaciones validas) - no es necesariamente un bug,
        // pero merece mirarse.
        Assert.Equal(original, rewritten);
    }
}
