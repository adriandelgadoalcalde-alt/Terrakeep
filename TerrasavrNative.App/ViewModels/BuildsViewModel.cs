using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Bd-d (segunda auditoria de Opus, Fable): pildora de filtro por clase - Key es la clave real
// tal cual viene en builds.json/builds_calamity.json (mage/melee/ranged/rogue/summoner, la
// misma que ya se muestra sin traducir en BuildClassGearViewModel.ClassName), Label es solo
// para el boton. null Key = "Todas".
public sealed partial class BuildClassFilterOptionViewModel(string? key, string label) : ObservableObject
{
    public string? Key { get; } = key;
    public string Label { get; } = label;
    [ObservableProperty] private bool _isSelected;
}

// Panel "Builds" - equipo recomendado por etapa/clase, ya resuelto a nombre+icono real (misma
// resolucion de pid->objeto que usa BuildItemResolver para auto-equipar, pero aqui solo para
// mostrar) mas el objeto Source original para el boton "Auto-equipar".
public sealed partial class BuildsViewModel : ObservableObject
{
    private readonly List<BuildClassGearViewModel> _allClasses;
    private readonly List<BuildStageViewModel> _allStages;

    public IReadOnlyList<BuildStageViewModel> VanillaStages { get; }
    public IReadOnlyList<BuildStageViewModel> CalamityStages { get; }

    // Bd-d: pildoras "Todas"/"Cuerpo a cuerpo"/"A distancia"/"Magia"/"Invocación"/"Pícaro" -
    // union real de las clases presentes en AMBOS catalogos (Calamity añade "rogue", que
    // vanilla no tiene - filtrar a "Pícaro" en la sub-pestaña Vanilla simplemente no deja nada
    // visible ahi, comportamiento correcto, no un bug: esa clase no existe en vanilla).
    public IReadOnlyList<BuildClassFilterOptionViewModel> ClassFilterOptions { get; }

    public BuildsViewModel(BuildsCatalog vanilla, BuildsCatalog calamity, CharacterFileService service)
    {
        VanillaStages = vanilla.Stages.Select(s => ResolveStage(s, service)).ToList();
        CalamityStages = calamity.Stages.Select(s => ResolveStage(s, service)).ToList();
        _allStages = [.. VanillaStages, .. CalamityStages];
        _allClasses = _allStages.SelectMany(s => s.Classes).ToList();

        Dictionary<string, string> labels = new()
        {
            ["melee"] = "Cuerpo a cuerpo",
            ["ranged"] = "A distancia",
            ["mage"] = "Magia",
            ["summoner"] = "Invocación",
            ["rogue"] = "Pícaro",
        };
        var options = new List<BuildClassFilterOptionViewModel> { new(null, "Todas") };
        foreach (var key in _allClasses.Select(c => c.ClassName).Distinct().OrderBy(k => k, StringComparer.Ordinal))
            options.Add(new BuildClassFilterOptionViewModel(key, labels.GetValueOrDefault(key, key)));
        ClassFilterOptions = options;
        options[0].IsSelected = true;
    }

    [RelayCommand]
    private void SelectClassFilter(BuildClassFilterOptionViewModel? option)
    {
        if (option == null) return;
        foreach (var o in ClassFilterOptions) o.IsSelected = o == option;
        foreach (var c in _allClasses)
            c.IsVisible = option.Key == null || c.ClassName == option.Key;
        foreach (var s in _allStages)
            s.IsVisible = s.Classes.Any(c => c.IsVisible);
    }

    // Bd-d: "marcar lo que ya se posee" - por id real (vanilla o sintetico de Calamity), en
    // CUALQUIER contenedor real del personaje cargado (Inventario/Almacenes/Equipamiento de
    // los 4 loadouts), no solo "puesto ahora mismo". Limitacion real conocida y documentada
    // (igual criterio que V-c): esto es una foto fija de cuando se llama, no reactiva a cada
    // tecla de una edicion en la pestaña Objetos - MainViewModel la llama al cargar personaje y
    // al entrar en esta pestaña, que es cuando de verdad hace falta el dato actualizado.
    public void RefreshOwnership(IEnumerable<ContainerViewModel> containers)
    {
        var ownedIds = new HashSet<int>();
        foreach (var container in containers)
            foreach (var slot in container.Slots)
                if (!slot.IsEmpty) ownedIds.Add(slot.ItemId);

        foreach (var row in _allClasses.SelectMany(c => c.AllRows))
            row.IsOwned = row.ItemId != 0 && ownedIds.Contains(row.ItemId);
    }

    private static BuildStageViewModel ResolveStage(BuildStage stage, CharacterFileService service)
    {
        var classes = stage.Classes.Select(kv => ResolveClass(kv.Key, kv.Value, service)).ToList();
        return new BuildStageViewModel(stage.Label, classes);
    }

    private static BuildClassGearViewModel ResolveClass(string className, BuildClassGear gear, CharacterFileService service) => new(
        className,
        gear.Armor.Select(i => ResolveItem(i, service)).ToList(),
        gear.Weapons.Select(i => ResolveItem(i, service)).ToList(),
        gear.Accessories.Select(i => ResolveItem(i, service)).ToList(),
        gear);

    private static BuildItemRowViewModel ResolveItem(BuildItemRef itemRef, CharacterFileService service)
    {
        string? iconPath = null;
        string? statsTooltip = null;
        bool isCalamity = false;
        int itemId = 0;

        if (!string.IsNullOrEmpty(itemRef.Pid))
        {
            int slash = itemRef.Pid.IndexOf('/');
            if (slash >= 0)
            {
                isCalamity = true;
                var entry = service.CalamityCatalog.ByModAndInternal(itemRef.Pid[..slash], itemRef.Pid[(slash + 1)..]);
                if (entry != null)
                {
                    itemId = entry.SyntheticId;
                    if (entry.Icon != null) iconPath = "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon;
                    statsTooltip = ItemStatsFormatter.Format(true, entry.SyntheticId, service.TooltipCatalogs);
                }
            }
            else
            {
                var id = service.VanillaCatalog.GetIdByKey(itemRef.Pid);
                if (id != null)
                {
                    itemId = id.Value;
                    iconPath = VanillaIconResolver.GetIconPath(id.Value);
                    statsTooltip = ItemStatsFormatter.Format(false, id.Value, service.TooltipCatalogs);
                }
            }
        }

        return new BuildItemRowViewModel(itemRef.DisplayName, itemRef.Prefix, iconPath, isCalamity, statsTooltip, itemId);
    }
}
