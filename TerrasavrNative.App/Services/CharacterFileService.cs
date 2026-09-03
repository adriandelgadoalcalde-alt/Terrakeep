using System.IO;
using System.Linq;
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

    // H4-08 (cuarta auditoria de Opus, Fable): version completa del lanzador de mundos - mismo
    // motivo real que GetDefaultPlayersDirectory de arriba (I-1), esta vez portado desde donde
    // vivia duplicada y privada, solo para el dialogo de Explorador de archivos
    // (MainWindow.xaml.cs.GetDefaultWorldsDirectory) - ahora tambien la reutiliza
    // ExplorationViewModel para listar los mundos reales de un plumazo.
    public static string GetDefaultWorldsDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string candidate = Path.Combine(documents, "My Games", "Terraria", "tModLoader", "Worlds");
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

        // T-C (segunda auditoria de Opus, Fable): WriteAllBytes directo sobre el fichero real
        // deja una ventana real donde un corte de luz/cierre forzado a mitad de escritura
        // corrompe el .plr entero (0 bytes o a medias) - WriteAtomic escribe siempre a un .tmp
        // aparte primero y solo AL FINAL lo intercambia por el real de un solo paso atomico del
        // sistema de ficheros (File.Replace), que de paso ya genera el .bak (Bloque 0, T-23) en
        // la MISMA operacion atomica en vez de una copia previa por separado (ventana de carrera
        // real, aunque muy improbable en una app de un solo usuario).
        WriteAtomic(loaded.PlrPath, PlrFile.Write(loaded.Character));

        // T-C: antes se escribia SIEMPRE un .tplr, incluso para un personaje 100% vanilla que
        // nunca tuvo ni tendra un objeto/buff de Calamity - ensuciaba la carpeta real de
        // Documentos del usuario con un fichero que Terraria/tModLoader ni pide ni usa. Ahora
        // solo si YA existia uno (se respeta, no se hace desaparecer un .tplr real de otra
        // sesion) o si el personaje tiene contenido real de Calamity (objeto en algun
        // contenedor, o un buff con id sintetico >= CalamityIds.BuffIdBase).
        string tplrPath = loaded.TplrPath ?? Path.ChangeExtension(loaded.PlrPath, ".tplr");
        bool yaTeniaTplr = loaded.TplrPath != null || File.Exists(tplrPath);
        bool tieneContenidoRealDeCalamity =
            loaded.MergedContainers.Values.Any(items => items.Any(i => i.IsCalamity))
            || loaded.Character.Buffs.Any(b => b.Id >= CalamityIds.BuffIdBase);
        if (yaTeniaTplr || tieneContenidoRealDeCalamity)
        {
            WriteAtomic(tplrPath, TplrFile.Write(loaded.TplrRootName, newTplrRoot));
            loaded.TplrRoot = newTplrRoot;
            loaded.TplrPath = tplrPath;
        }
    }

    private static void WriteAtomic(string path, byte[] bytes)
    {
        string tmpPath = path + ".tmp";
        File.WriteAllBytes(tmpPath, bytes);
        if (!File.Exists(path))
        {
            File.Move(tmpPath, path);
            return;
        }
        try
        {
            // Un solo paso atomico real: escribe el fichero final Y el .bak (Bloque 0, T-23:
            // "deshacer el ULTIMO guardado si algo salio mal") a la vez - nunca hay un instante
            // con el fichero real a medio escribir.
            File.Replace(tmpPath, path, path + ".bak", ignoreMetadataErrors: true);
        }
        catch (IOException)
        {
            // Respaldo best-effort real (ej. .bak bloqueado por el antivirus): el guardado del
            // personaje en si no debe fallar por eso, que es lo que de verdad importa.
            File.Replace(tmpPath, path, null, ignoreMetadataErrors: true);
        }
    }
}
