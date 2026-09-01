using System.IO;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.Services;

// Un personaje ya cargado en memoria - el PlrCharacter crudo (reflejo fiel del .plr), el
// .tplr (si existe) y la vista fusionada por contenedor (GameItem[], objetos de Calamity ya
// mezclados con los vanilla) que la UI enlaza de verdad.
public sealed class LoadedCharacter(string plrPath, string? tplrPath, string tplrRootName, PlrCharacter character, NbtCompound? tplrRoot, Dictionary<string, GameItem[]> mergedContainers)
{
    public string PlrPath { get; } = plrPath;
    public string? TplrPath { get; set; } = tplrPath;
    public string TplrRootName { get; } = tplrRootName;
    public PlrCharacter Character { get; } = character;
    public NbtCompound? TplrRoot { get; set; } = tplrRoot;
    public Dictionary<string, GameItem[]> MergedContainers { get; } = mergedContainers;
}

// Punto de entrada de la app a la Fase 1 (Core): carga catalogos una vez, y ofrece
// Load/Save de un personaje real detectando el .tplr hermano automaticamente - mismo criterio
// que la app Electron actual ("si existe un .tplr con el mismo nombre en la misma carpeta").
public sealed class CharacterFileService
{
    private readonly CalamityCharacterSync _sync;

    public CalamityCatalog CalamityCatalog { get; }
    public RoguePrefixCatalog RoguePrefixCatalog { get; }
    public VanillaItemCatalog VanillaCatalog { get; }
    public VanillaPrefixCatalog VanillaPrefixCatalog { get; }
    public BuildsCatalog VanillaBuilds { get; }
    public BuildsCatalog CalamityBuilds { get; }
    public WhatsNewCatalog WhatsNew { get; }
    public MapColorCatalog MapColors { get; }
    public TileNameCatalog TileNames { get; }
    public NpcNameCatalog NpcNames { get; }
    public VanillaBuffCatalog VanillaBuffs { get; }
    public BestPrefixCatalog BestPrefixes { get; }

    public CharacterFileService()
    {
        string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        CalamityCatalog = CalamityCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "catalog.json"));
        RoguePrefixCatalog = RoguePrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "rogue_prefixes.json"));
        VanillaCatalog = VanillaItemCatalog.LoadFromFile(
            Path.Combine(assetsDir, "vanilla_item_names.json"),
            Path.Combine(assetsDir, "vanilla_item_names_by_key.json"),
            Path.Combine(assetsDir, "vanilla_item_ids_by_key.json"));
        VanillaPrefixCatalog = VanillaPrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "prefixes.json"));
        VanillaBuilds = BuildsCatalog.LoadFromFile(Path.Combine(assetsDir, "builds.json"));
        CalamityBuilds = BuildsCatalog.LoadFromFile(Path.Combine(assetsDir, "builds_calamity.json"));
        WhatsNew = WhatsNewCatalog.LoadFromFile(Path.Combine(assetsDir, "whats_new.json"));
        MapColors = MapColorCatalog.LoadFromFile(Path.Combine(assetsDir, "map_colors.json"));
        TileNames = TileNameCatalog.LoadFromFile(Path.Combine(assetsDir, "tile_names.json"));
        NpcNames = NpcNameCatalog.LoadFromFile(Path.Combine(assetsDir, "npc_names.json"));
        VanillaBuffs = VanillaBuffCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_buff_names.json"));
        BestPrefixes = BestPrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "best_prefix.json"));

        var translator = new CalamityPrefixTranslator(RoguePrefixCatalog);
        var codec = new CalamityItemCodec(CalamityCatalog, translator);
        _sync = new CalamityCharacterSync(codec);
    }

    public LoadedCharacter Load(string plrPath)
    {
        var character = PlrFile.Read(File.ReadAllBytes(plrPath));

        string tplrPath = Path.ChangeExtension(plrPath, ".tplr");
        NbtCompound? tplrRoot = null;
        string tplrRootName = "Player";
        bool hasTplr = File.Exists(tplrPath);
        if (hasTplr)
        {
            (tplrRootName, tplrRoot) = TplrFile.Read(File.ReadAllBytes(tplrPath));
        }

        var merged = _sync.MergeAll(character, tplrRoot);
        return new LoadedCharacter(plrPath, hasTplr ? tplrPath : null, tplrRootName, character, tplrRoot, merged);
    }

    public void Save(LoadedCharacter loaded)
    {
        var newTplrRoot = _sync.MaskAndSyncAll(loaded.Character, loaded.MergedContainers, loaded.TplrRoot);
        File.WriteAllBytes(loaded.PlrPath, PlrFile.Write(loaded.Character));

        string tplrPath = loaded.TplrPath ?? Path.ChangeExtension(loaded.PlrPath, ".tplr");
        File.WriteAllBytes(tplrPath, TplrFile.Write(loaded.TplrRootName, newTplrRoot));

        loaded.TplrRoot = newTplrRoot;
        loaded.TplrPath = tplrPath;
    }
}
