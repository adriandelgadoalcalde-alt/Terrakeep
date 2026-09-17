using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Cache persistente de "la Libreria de objetos (8000+)" en disco (17-sep-2026, ver el comentario
// real de CatalogBinaryCache/LibraryCatalogDiskCache para las medidas reales que la justifican).
// Estos tests cubren el contrato real que importa: round-trip fiel de los dos catalogos + el
// arbol, y que CUALQUIER forma de invalidez (fichero ausente, version antigua, huella distinta
// tras tocar un fichero fuente, bytes truncados) hace que TryLoad devuelva null en vez de datos a
// medias o una excepcion que se propague hacia quien llama - la cache nunca puede ser la razon de
// que la app deje de arrancar.
public class LibraryCatalogDiskCacheTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "TerrakeepTests_" + Guid.NewGuid().ToString("N"));

    private const string CalamitySample = """
    [
      { "internal": "Abaddon", "mod": "CalamityMod", "category": "Accessories", "displayName_es": "Abaddon",
        "displayName_en": "Abaddon", "displayName_fallback": "Abaddon", "icon": "Abaddon.png",
        "equipSlot": null, "setBonus": null, "setBonus_en": null,
        "stats": { "damage": 10, "useTime": 20, "crit": 4, "knockBack": 2.5, "mana": null, "damageType": "melee", "defense": null } },
      { "internal": "Calamity", "mod": "CalamityMod", "category": "Weapons/Melee", "displayName_es": "Calamity",
        "displayName_en": null, "displayName_fallback": "Calamity", "icon": "Calamity.png",
        "equipSlot": "Head", "setBonus": "Bono real", "setBonus_en": "Real bonus", "stats": null }
    ]
    """;

    public LibraryCatalogDiskCacheTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* mejor esfuerzo, no bloquear el resto de la tanda */ }
    }

    private string WriteSourceFile(string name, string content)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static CalamityCatalog LoadCalamity() =>
        CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(CalamitySample)));

    private static VanillaItemCatalog LoadVanilla() => VanillaItemCatalog.LoadFromStreams(
        new MemoryStream(Encoding.UTF8.GetBytes("""{"1":"Piedra","4":"Espada de hierro"}""")),
        new MemoryStream(Encoding.UTF8.GetBytes("""{"DirtBlock":"Piedra","IronBroadsword":"Espada de hierro"}""")),
        new MemoryStream(Encoding.UTF8.GetBytes("""{"DirtBlock":1,"IronBroadsword":4}""")));

    private static List<CategoryTreeNodeData> SampleTree() =>
    [
        new CategoryTreeNodeData(
            "Materiales", "Materials", "icon1.png",
            [1, 4], new HashSet<int> { 1, 4 },
            [
                new CategoryTreeNodeData("Hierro", "Materials/Iron", null, [4], new HashSet<int> { 4 }, [], "Iron"),
            ],
            "Materials"),
    ];

    [Fact]
    public void SaveThenTryLoad_RoundTripsCalamityVanillaAndTree()
    {
        string[] sources = [WriteSourceFile("catalog.json", CalamitySample), WriteSourceFile("vanilla.json", "{}")];
        string cachePath = Path.Combine(_tempDir, "cache.bin");

        var calamity = LoadCalamity();
        var vanilla = LoadVanilla();
        var tree = SampleTree();

        LibraryCatalogDiskCache.Save(cachePath, sources, vanilla, calamity, tree);
        var loaded = LibraryCatalogDiskCache.TryLoad(cachePath, sources);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.CalamityCatalog.Entries.Count);
        Assert.Equal("Abaddon", loaded.CalamityCatalog.Entries[0].Internal);
        Assert.Equal(10, loaded.CalamityCatalog.Entries[0].Stats!.Damage);
        Assert.Equal("Bono real", loaded.CalamityCatalog.Entries[1].SetBonusFor(LocalizedContent.Spanish));
        Assert.Equal("Real bonus", loaded.CalamityCatalog.Entries[1].SetBonusFor(LocalizedContent.English));
        Assert.Null(loaded.CalamityCatalog.Entries[0].Stats!.Mana);

        Assert.Equal("Espada de hierro", loaded.VanillaCatalog.GetName(4, LocalizedContent.Spanish));
        Assert.Equal(4, loaded.VanillaCatalog.GetIdByKey("IronBroadsword"));

        Assert.Single(loaded.Tree);
        Assert.Equal("Materiales", loaded.Tree[0].Name);
        Assert.Equal(2, loaded.Tree[0].ItemIdsOrdered.Count);
        Assert.Single(loaded.Tree[0].Children);
        Assert.Equal("Hierro", loaded.Tree[0].Children[0].Name);
        Assert.Equal([4], loaded.Tree[0].Children[0].ItemIdsOrdered);
    }

    [Fact]
    public void TryLoad_FileMissing_ReturnsNull()
    {
        string[] sources = [WriteSourceFile("catalog.json", CalamitySample)];
        Assert.Null(LibraryCatalogDiskCache.TryLoad(Path.Combine(_tempDir, "no-existe.bin"), sources));
    }

    [Fact]
    public void TryLoad_SourceFileChangedAfterSave_FingerprintMismatch_ReturnsNull()
    {
        string sourcePath = WriteSourceFile("catalog.json", CalamitySample);
        string[] sources = [sourcePath];
        string cachePath = Path.Combine(_tempDir, "cache.bin");
        LibraryCatalogDiskCache.Save(cachePath, sources, LoadVanilla(), LoadCalamity(), SampleTree());

        // Simula un catalog.json REAL regenerado (contenido y fecha de modificacion distintos) -
        // la huella real debe cambiar y la cache debe descartarse sola, sin que nadie la borre.
        Thread.Sleep(10);
        File.WriteAllText(sourcePath, CalamitySample + "\n// cambiado de verdad");

        Assert.Null(LibraryCatalogDiskCache.TryLoad(cachePath, sources));
    }

    [Fact]
    public void TryLoad_TruncatedFile_ReturnsNullInsteadOfThrowing()
    {
        string[] sources = [WriteSourceFile("catalog.json", CalamitySample)];
        string cachePath = Path.Combine(_tempDir, "cache.bin");
        LibraryCatalogDiskCache.Save(cachePath, sources, LoadVanilla(), LoadCalamity(), SampleTree());

        byte[] bytes = File.ReadAllBytes(cachePath);
        File.WriteAllBytes(cachePath, bytes[..(bytes.Length / 3)]);

        Assert.Null(LibraryCatalogDiskCache.TryLoad(cachePath, sources));
    }

    [Fact]
    public void TryLoad_WrongMagicBytes_ReturnsNull()
    {
        string[] sources = [WriteSourceFile("catalog.json", CalamitySample)];
        string cachePath = Path.Combine(_tempDir, "cache.bin");
        File.WriteAllBytes(cachePath, Encoding.UTF8.GetBytes("no es una cache real de Terrakeep"));

        Assert.Null(LibraryCatalogDiskCache.TryLoad(cachePath, sources));
    }

    [Fact]
    public void Save_UnwritableDirectory_NeverThrows()
    {
        // "carpeta" invalida a proposito (un caracter real que Windows no admite en un nombre de
        // fichero) para forzar un fallo real de E-S sin depender de permisos de la maquina de
        // pruebas - Save debe tragarselo (mejor esfuerzo real, ver su comentario) en vez de tirar
        // abajo el arranque que la esta llamando.
        string cachePath = Path.Combine(_tempDir, "sub:dir", "cache.bin");
        var ex = Record.Exception(() =>
            LibraryCatalogDiskCache.Save(cachePath, [], LoadVanilla(), LoadCalamity(), SampleTree()));
        Assert.Null(ex);
    }

    [Fact]
    public void TryLoad_SourceFileDeletedAfterSave_ReturnsNull()
    {
        // Otra forma real de que la huella deje de coincidir: un fichero fuente que YA NO EXISTE
        // (reinstalacion a medias, fichero borrado a mano) - ComputeFingerprint marca ausencia
        // real (tamaño/fecha -1), nunca lanza ni lo trata como "sin cambios".
        string sourcePath = WriteSourceFile("catalog.json", CalamitySample);
        string[] sources = [sourcePath];
        string cachePath = Path.Combine(_tempDir, "cache.bin");
        LibraryCatalogDiskCache.Save(cachePath, sources, LoadVanilla(), LoadCalamity(), SampleTree());

        File.Delete(sourcePath);

        Assert.Null(LibraryCatalogDiskCache.TryLoad(cachePath, sources));
    }
}
