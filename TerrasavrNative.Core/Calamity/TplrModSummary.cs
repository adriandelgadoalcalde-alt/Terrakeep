using TerrasavrNative.Core.Nbt;

namespace TerrasavrNative.Core.Calamity;

// Lo que se puede saber de un personaje mirando SOLO su .tplr, sin cargarlo entero.
//
// Encargo del usuario 4-sep-2026 ("en el inicio los personajes que tienen mod solo marcan
// calamity... que haga referencia a que son personajes verdaderamente de tmodloader que es lo
// principal... al igual que cuando un personaje es vanilla que tenga dicha etiqueta") - ver
// ESPEC-sprites-botones-badges.md#C.
//
// UsedMods es una clave REAL que escribe el propio tModLoader en cada guardado
// (Terraria/ModLoader/IO/PlayerIO.cs:67 -> SaveUsedMods = ModLoader.Mods.Select(m => m.Name)
// menos "ModLoader"), o sea la lista de mods CARGADOS al guardar. Es un dato informativo
// excelente ("Mods usados: ...") pero NO sirve para decidir si hay Calamity dentro: un .tplr
// escrito por el propio Terrakeep (MaskAndSyncAll solo reescribe los contenedores + modBuffs +
// loadouts) no lo tiene, y sin embargo puede tener objetos de Calamity de verdad - caso real
// comprobado con prueba.tplr de esta maquina (94 bytes, sin usedMods, con 1 objeto de Calamity).
public sealed class TplrModSummary
{
    // Hay al menos un OBJETO o BUFF de Calamity real dentro. Mismo criterio exacto que usa
    // CharacterFileService.Save para decidir si merece la pena escribir un .tplr, pero resuelto
    // sobre el NBT crudo en vez de sobre el personaje ya fusionado.
    public required bool HasCalamityContent { get; init; }
    // Lista real de usedMods (vacia si el .tplr no la trae).
    public required IReadOnlyList<string> UsedMods { get; init; }
}

public static class TplrProbe
{
    // Los 2 unicos mods reales del catalogo de Calamity de esta app (contados sobre
    // Assets/calamity/catalog.json: CalamityMod 2647 entradas, CalamityModMusic 62) - los mismos
    // que CalamityItemCodec.Decode puede traducir a un id sintetico. "CalamityModEsp" (la
    // traduccion) aparece en usedMods pero no aporta ningun objeto, por eso no esta aqui.
    private static readonly string[] ModsDeCalamity = ["CalamityMod", "CalamityModMusic"];

    // Las listas de entradas de objeto/buff del .tplr. Las 7 primeras son exactamente
    // CalamityCharacterSync.FlatContainers; armor/dye son las claves planas del loadout activo;
    // modBuffs son los buffs. Deliberadamente NO se miran "research" ni "modData": el criterio
    // tiene que significar LO MISMO que el de CharacterFileService.Save ("tiene un objeto o un
    // buff de Calamity"), no algo parecido pero distinto - si algun dia se amplia, hay que
    // ampliar los dos a la vez.
    private static readonly string[] ListasDeEntradas =
        ["inventory", "bank", "bank2", "bank3", "bank4", "miscEquips", "miscDyes", "armor", "dye", "modBuffs"];

    public static bool EsModDeCalamity(string? mod) => mod != null && Array.IndexOf(ModsDeCalamity, mod) >= 0;

    // Un .tplr ilegible (corrupto, de otro formato, bloqueado) NO debe tumbar el listado de
    // Inicio - mismo criterio que HomeViewModel.ScanCharacters ya aplica a los .plr ajenos.
    public static TplrModSummary? TryRead(string tplrPath)
    {
        try
        {
            var (_, root) = TplrFile.Read(File.ReadAllBytes(tplrPath));
            return From(root);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static TplrModSummary From(NbtCompound root) => new()
    {
        HasCalamityContent = TieneContenidoDeCalamity(root),
        UsedMods = LeerUsedMods(root),
    };

    private static bool TieneContenidoDeCalamity(NbtCompound root)
    {
        foreach (string clave in ListasDeEntradas)
            if (ListaTieneCalamity(root.Get(clave))) return true;

        // Las claves por loadout ("loadout{i}Armor"/"loadout{i}Dye", ver
        // CalamityCharacterSync.MergeLoadoutArmorDye) viven dentro del compound "loadouts".
        if (root.Get("loadouts") is NbtCompound loadouts)
            foreach (var (_, tag) in loadouts.Fields)
                if (ListaTieneCalamity(tag)) return true;

        return false;
    }

    private static bool ListaTieneCalamity(NbtTag? tag)
    {
        if (tag is not NbtList list) return false;
        foreach (var item in list.Items)
            if (item is NbtCompound entry && EsModDeCalamity((entry.Get("mod") as NbtString)?.Value))
                return true;
        return false;
    }

    private static IReadOnlyList<string> LeerUsedMods(NbtCompound root)
    {
        if (root.Get("usedMods") is not NbtList list) return [];
        var mods = new List<string>();
        foreach (var item in list.Items)
            if (item is NbtString s && !string.IsNullOrWhiteSpace(s.Value)) mods.Add(s.Value);
        return mods;
    }
}
