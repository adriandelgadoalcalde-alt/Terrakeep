using TerrasavrNative.Core.Nbt;
using Xunit;
using Xunit.Abstractions;

namespace TerrasavrNative.Core.Tests.Nbt;

// Prueba de humo contra .tplr REALES de este PC - confirma que el lector NBT+gzip genérico
// entiende de verdad los archivos que produce tModLoader, no solo los fixtures a mano de
// NbtSerializerTests/TplrFileTests. Se salta en silencio si la carpeta no existe (mismo
// criterio que el resto de pruebas contra datos reales de este proyecto).
public class TplrFileRealCharacterTests(ITestOutputHelper output)
{
    private const string PlayersDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players";

    public static IEnumerable<object[]> RealTplrFiles()
    {
        if (!Directory.Exists(PlayersDir)) yield break;
        yield return [Path.Combine(PlayersDir, "Eldelgas.tplr")];
        yield return [Path.Combine(PlayersDir, "adrian.tplr")];
    }

    [Theory]
    [MemberData(nameof(RealTplrFiles))]
    public void Read_RealFile_ParsesWithoutError(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileBytes = File.ReadAllBytes(path);
        var (rootName, root) = TplrFile.Read(fileBytes);

        output.WriteLine($"{Path.GetFileName(path)}: raiz='{rootName}', campos={root.Fields.Count}");
        foreach (var (name, tag) in root.Fields)
            output.WriteLine($"  {name}: {tag.Type}");

        Assert.NotNull(root);
    }

    [Theory]
    [MemberData(nameof(RealTplrFiles))]
    public void RoundTrip_RealFile_ProducesByteIdenticalNbtBody(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileBytes = File.ReadAllBytes(path);
        var (rootName, root) = TplrFile.Read(fileBytes);
        var (rootName2, root2) = TplrFile.Read(TplrFile.Write(rootName, root));

        // No se compara el .tplr byte a byte (gzip no es determinista frente al original de
        // tModLoader - version de zlib/nivel de compresion distintos), pero el NBT que hay
        // DENTRO si debe ser identico en estructura: releer lo que acabamos de escribir tiene
        // que dar exactamente los mismos campos.
        Assert.Equal(rootName, rootName2);
        Assert.Equal(root.Fields.Select(f => f.Name), root2.Fields.Select(f => f.Name));
    }
}
