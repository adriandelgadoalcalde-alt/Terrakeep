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
    public CalamityBuffCatalog CalamityBuffCatalog { get; }
    public RoguePrefixCatalog RoguePrefixCatalog { get; }
    public VanillaItemCatalog VanillaCatalog { get; }
    public VanillaPrefixCatalog VanillaPrefixCatalog { get; }
    public BuildsCatalog VanillaBuilds { get; }
    public BuildsCatalog CalamityBuilds { get; }
    public WhatsNewCatalog WhatsNew { get; }
    public ChangelogCatalog Changelog { get; }
    public MapColorCatalog MapColors { get; }
    public TileNameCatalog TileNames { get; }
    public NpcNameCatalog NpcNames { get; }
    public VanillaBuffCatalog VanillaBuffs { get; }
    public BestPrefixCatalog BestPrefixes { get; }
    public VanillaCategoryCatalog VanillaCategories { get; }
    public VanillaSlotKindCatalog VanillaSlotKinds { get; }
    public VanillaItemStatsCatalog VanillaStats { get; }
    public PrefixRulesCatalog PrefixRules { get; }
    public HairDyeCatalog HairDyes { get; }
    public VanillaLibraryTreeCatalog VanillaLibraryTree { get; }
    public LibraryLabelCatalog LibraryLabels { get; }
    public VanillaItemTooltipCatalog VanillaItemTooltips { get; }
    public VanillaArmorSetCatalog VanillaArmorSets { get; }
    public PrefixEffectCatalog PrefixEffects { get; }
    public VanillaBuffDurationCatalog VanillaBuffDurations { get; }
    public VanillaResearchCountCatalog VanillaResearchCounts { get; }

    // Los 6 catalogos que ItemStatsFormatter.Format necesita, agrupados en un unico record -
    // pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada: la firma ya iba por 5
    // parametros sueltos, seguir añadiendo uno por catalogo nuevo (tooltip/set de armadura)
    // habria hecho facil olvidarse de pasar alguno en algun call site. Los 5 call sites reales
    // (LibraryViewModel, BuildsViewModel, ItemSlotViewModel) pasan esto en vez de 6 argumentos.
    public ItemTooltipCatalogs TooltipCatalogs { get; }

    public CharacterFileService()
    {
        string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        CalamityCatalog = CalamityCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "catalog.json"));
        CalamityBuffCatalog = CalamityBuffCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "buffs.json"), Path.Combine(assetsDir, "calamity_buff_descriptions.json"));
        RoguePrefixCatalog = RoguePrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "rogue_prefixes.json"));
        VanillaCatalog = VanillaItemCatalog.LoadFromFile(
            Path.Combine(assetsDir, "vanilla_item_names.json"),
            Path.Combine(assetsDir, "vanilla_item_names_by_key.json"),
            Path.Combine(assetsDir, "vanilla_item_ids_by_key.json"));
        VanillaPrefixCatalog = VanillaPrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "prefixes.json"));
        VanillaBuilds = BuildsCatalog.LoadFromFile(Path.Combine(assetsDir, "builds.json"));
        CalamityBuilds = BuildsCatalog.LoadFromFile(Path.Combine(assetsDir, "builds_calamity.json"));
        WhatsNew = WhatsNewCatalog.LoadFromFile(Path.Combine(assetsDir, "whats_new.json"));
        Changelog = ChangelogCatalog.LoadFromFile(Path.Combine(assetsDir, "changelog.json"));
        MapColors = MapColorCatalog.LoadFromFile(Path.Combine(assetsDir, "map_colors.json"));
        TileNames = TileNameCatalog.LoadFromFile(Path.Combine(assetsDir, "tile_names.json"));
        NpcNames = NpcNameCatalog.LoadFromFile(Path.Combine(assetsDir, "npc_names.json"));
        VanillaBuffs = VanillaBuffCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_buff_names.json"), Path.Combine(assetsDir, "vanilla_buff_descriptions.json"), Path.Combine(assetsDir, "vanilla_buff_names_es.json"));
        BestPrefixes = BestPrefixCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "best_prefix.json"));
        VanillaCategories = VanillaCategoryCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_categories.json"));
        VanillaSlotKinds = VanillaSlotKindCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_slot_kind.json"));
        VanillaStats = VanillaItemStatsCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_stats.json"));
        PrefixRules = PrefixRulesCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_prefix_rules.json"));
        HairDyes = HairDyeCatalog.LoadFromFile(Path.Combine(assetsDir, "hair_dyes.json"));
        VanillaLibraryTree = VanillaLibraryTreeCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_library_tree.json"));
        LibraryLabels = LibraryLabelCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_library_labels_es.json"));
        VanillaItemTooltips = VanillaItemTooltipCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_item_tooltips.json"));
        VanillaArmorSets = VanillaArmorSetCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_armor_sets.json"));
        PrefixEffects = PrefixEffectCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_prefix_effects.json"));
        VanillaBuffDurations = VanillaBuffDurationCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_buff_durations.json"));
        VanillaResearchCounts = VanillaResearchCountCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_research_counts.json"));
        TooltipCatalogs = new ItemTooltipCatalogs(VanillaStats, CalamityCatalog, VanillaCategories, VanillaItemTooltips, VanillaArmorSets, PrefixEffects);

        var translator = new CalamityPrefixTranslator(RoguePrefixCatalog);
        var codec = new CalamityItemCodec(CalamityCatalog, translator);
        _sync = new CalamityCharacterSync(codec, CalamityBuffCatalog);
    }

    // Auditoria de Opus, I-1: unica fuente real de "donde vive normalmente un .plr" - antes
    // vivia duplicada y privada dentro de MainWindow.xaml.cs (solo para el dialogo de
    // Explorador de archivos), ahora la reutiliza tambien HomeViewModel para listar los
    // personajes reales de un plumazo en Inicio.
    public static string GetDefaultPlayersDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string candidate = Path.Combine(documents, "My Games", "Terraria", "tModLoader", "Players");
        return Directory.Exists(candidate) ? candidate : documents;
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

        // Copia de seguridad real antes de sobrescribir (Bloque 0 de la auditoria de Opus,
        // T-23: "nada tiene deshacer... el programa deberia aplicarse el mismo criterio de
        // 'usar siempre una copia' que ya exige la propia bitacora al probar"). Copia de un
        // solo nivel (nombre.plr.bak / nombre.tplr.bak, se sobrescribe cada guardado) - protege
        // el caso real (deshacer el ULTIMO guardado si algo salio mal) sin acumular ficheros sin
        // limite. Solo si el fichero YA existe - el primer guardado de un personaje nuevo no
        // tiene nada que respaldar.
        BackupIfExists(loaded.PlrPath);
        File.WriteAllBytes(loaded.PlrPath, PlrFile.Write(loaded.Character));

        string tplrPath = loaded.TplrPath ?? Path.ChangeExtension(loaded.PlrPath, ".tplr");
        BackupIfExists(tplrPath);
        File.WriteAllBytes(tplrPath, TplrFile.Write(loaded.TplrRootName, newTplrRoot));

        loaded.TplrRoot = newTplrRoot;
        loaded.TplrPath = tplrPath;
    }

    private static void BackupIfExists(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            File.Copy(path, path + ".bak", overwrite: true);
        }
        catch (IOException)
        {
            // Copia de seguridad best-effort real: si el .bak esta bloqueado (ej. antivirus)
            // no debe impedir el guardado real del personaje, que es lo que de verdad importa.
        }
    }
}
