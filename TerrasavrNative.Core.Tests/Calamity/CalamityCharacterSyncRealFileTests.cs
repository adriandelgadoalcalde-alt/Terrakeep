using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;
using Xunit;
using Xunit.Abstractions;

namespace TerrasavrNative.Core.Tests.Calamity;

// Prueba de extremo a extremo contra el personaje REAL "adrian" de este PC, que segun el
// README.md de Terrasavr-Calamity-Beta tiene objetos de Calamity genuinamente equipados
// (CalamityModMusic/CalamityTitleMusicBox). Se salta en silencio si faltan los archivos.
public class CalamityCharacterSyncRealFileTests(ITestOutputHelper output)
{
    private const string PlayersDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players";
    private const string LocalSiteDir = @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Calamity-Beta\resources\app\local-site";
    private const string DescriptionsPath = @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\calamity_buff_descriptions.json";
    private const string DebuffsPath = @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\calamity\buff_debuffs.json";

    private static bool RealFilesExist() =>
        File.Exists(Path.Combine(PlayersDir, "adrian.plr")) &&
        File.Exists(Path.Combine(PlayersDir, "adrian.tplr")) &&
        File.Exists(Path.Combine(LocalSiteDir, "calamity", "catalog.json")) &&
        File.Exists(Path.Combine(LocalSiteDir, "calamity", "rogue_prefixes.json")) &&
        File.Exists(Path.Combine(LocalSiteDir, "calamity", "buffs.json")) &&
        File.Exists(DescriptionsPath) &&
        File.Exists(DebuffsPath);

    private static CalamityCharacterSync MakeRealSync(out CalamityCatalog catalog)
    {
        catalog = CalamityCatalog.LoadFromFile(Path.Combine(LocalSiteDir, "calamity", "catalog.json"));
        var buffCatalog = CalamityBuffCatalog.LoadFromFile(Path.Combine(LocalSiteDir, "calamity", "buffs.json"), DescriptionsPath, DebuffsPath);
        var prefixes = RoguePrefixCatalog.LoadFromFile(Path.Combine(LocalSiteDir, "calamity", "rogue_prefixes.json"));
        var codec = new CalamityItemCodec(catalog, new CalamityPrefixTranslator(prefixes));
        return new CalamityCharacterSync(codec, buffCatalog);
    }

    [Fact]
    public void MergeAll_RealAdrianCharacter_FindsCalamityItems()
    {
        if (!RealFilesExist()) return;

        var character = PlrFile.Read(File.ReadAllBytes(Path.Combine(PlayersDir, "adrian.plr")));
        var (_, tplrRoot) = TplrFile.Read(File.ReadAllBytes(Path.Combine(PlayersDir, "adrian.tplr")));
        var sync = MakeRealSync(out var catalog);

        var merged = sync.MergeAll(character, tplrRoot);

        int calamityCount = merged.Values.Sum(items => items.Count(i => i.IsCalamity));
        output.WriteLine($"Objetos de Calamity encontrados tras fusionar: {calamityCount}");
        foreach (var (key, items) in merged)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].IsCalamity) continue;
                var entry = catalog.BySyntheticId(items[i].Id);
                output.WriteLine($"  {key}[{i}] = {entry?.Mod}/{entry?.Internal} x{items[i].Count}");
            }
        }

        Assert.True(calamityCount > 0, "Se esperaba al menos un objeto de Calamity real (ver README.md de Terrasavr-Calamity-Beta).");
    }

    [Fact]
    public void MaskAndSyncAll_RealAdrianCharacter_RoundTripsCalamityItemsAndPlrStaysMaskedIdentical()
    {
        if (!RealFilesExist()) return;

        byte[] originalPlrBytes = File.ReadAllBytes(Path.Combine(PlayersDir, "adrian.plr"));
        byte[] originalTplrBytes = File.ReadAllBytes(Path.Combine(PlayersDir, "adrian.tplr"));

        var character = PlrFile.Read(originalPlrBytes);
        var (rootName, tplrRoot) = TplrFile.Read(originalTplrBytes);
        var sync = MakeRealSync(out _);

        var merged = sync.MergeAll(character, tplrRoot);
        int calamityCountBefore = merged.Values.Sum(items => items.Count(i => i.IsCalamity));
        Assert.True(calamityCountBefore > 0);

        var characterBeforeResave = PlrFile.Read(originalPlrBytes); // copia independiente para comparar slot a slot

        var newTplrRoot = sync.MaskAndSyncAll(character, merged, tplrRoot);
        byte[] rewrittenPlrBytes = PlrFile.Write(character);
        var rewrittenCharacter = PlrFile.Read(rewrittenPlrBytes);

        // NO se compara byte a byte contra el original: pasar por MaskAndSyncAll limpia de
        // paso cualquier "slot fantasma" (id=0 con count/prefix residual) que hubiera en el
        // archivo - la MISMA limpieza que ya hace la app JS en cada guardado (ver README.md,
        // punto 5), asi que una diferencia ahi es la esperada, no un fallo. Round-trip puro
        // sin pasar por Calamity (PlrFileRealCharacterTests) es donde se prueba fidelidad
        // byte a byte de verdad. Aqui se comprueba que todo objeto REAL (vanilla, id!=0) se
        // conserva identico, y que cualquier slot que cambie es exactamente uno que ya estaba
        // vacio antes.
        for (int i = 0; i < characterBeforeResave.Inventory.Length; i++)
        {
            var before = characterBeforeResave.Inventory[i];
            var after = rewrittenCharacter.Inventory[i];
            if (before.Id != 0)
            {
                Assert.Equal(before.Id, after.Id);
                Assert.Equal(before.Count, after.Count);
                Assert.Equal(before.Prefix, after.Prefix);
            }
            else
            {
                Assert.Equal(0, after.Id); // limpio o ya limpio, nunca aparece un id nuevo de la nada
            }
        }

        // Y releer el .tplr recien sincronizado debe encontrar los mismos objetos de Calamity.
        var reMerged = sync.MergeAll(character, newTplrRoot);
        int calamityCountAfter = reMerged.Values.Sum(items => items.Count(i => i.IsCalamity));
        Assert.Equal(calamityCountBefore, calamityCountAfter);
    }
}
