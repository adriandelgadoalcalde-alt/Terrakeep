using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Bd-d (segunda auditoria de Opus, Fable): pildora de filtro por clase - Key es la clave real
// tal cual viene en builds.json/builds_calamity.json (mage/melee/ranged/rogue/summoner, la
// misma que ya se muestra sin traducir en BuildClassGearViewModel.ClassName), Label es solo
// para el boton. null Key = "Todas".
// Ronda de idioma del 6-sep-2026: Label llegaba ya resuelto (loc["class_melee"] leido UNA sola
// vez en el constructor de BuildsViewModel), asi que las pildoras se quedaban en el idioma de
// arranque - "Cuerpo a cuerpo"/"A distancia"/"Invocación"/"Pícaro"/"Magia"/"Todas" en pantalla
// con la app en ingles. Ahora se guarda la CLAVE y se resuelve al leerla, con aviso real al
// cambiar de idioma en caliente (evento debil, mismo motivo real que LocalizedContentViewModel).
public sealed partial class BuildClassFilterOptionViewModel : ObservableObject
{
    private readonly string _labelKey;

    public BuildClassFilterOptionViewModel(string? key, string labelKey)
    {
        Key = key;
        _labelKey = labelKey;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    public string? Key { get; }

    // Una clase que no tenga clave de idioma propia (un catalogo futuro con una clase nueva) cae
    // a su nombre interno tal cual, no al "[clave]" en bruto del diccionario: ahi si es un dato
    // real que existe, no una traduccion perdida.
    public string Label
    {
        get
        {
            string texto = LocalizationService.Instance[_labelKey];
            return texto.StartsWith('[') && texto.EndsWith(']') ? _labelKey : texto;
        }
    }
    [ObservableProperty] private bool _isSelected;

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(Label));
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

        // Ronda de idioma del 6-sep-2026: se guarda la CLAVE de idioma, no el texto ya resuelto
        // (ver BuildClassFilterOptionViewModel) - antes las pildoras se quedaban congeladas en el
        // idioma de arranque. Una clase sin clave conocida cae a su nombre interno, mismo criterio
        // de siempre: "lo que no se encuentra no se inventa".
        Dictionary<string, string> claves = new()
        {
            ["melee"] = "class_melee",
            ["ranged"] = "class_ranged",
            ["mage"] = "class_mage",
            ["summoner"] = "class_summoner",
            ["rogue"] = "class_rogue",
        };
        var options = new List<BuildClassFilterOptionViewModel> { new(null, "class_all") };
        foreach (var key in _allClasses.Select(c => c.ClassName).Distinct().OrderBy(k => k, StringComparer.Ordinal))
            options.Add(new BuildClassFilterOptionViewModel(key, claves.GetValueOrDefault(key, key)));
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
        return new BuildStageViewModel(stage, classes);
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

        return new BuildItemRowViewModel(itemRef, itemRef.Prefix, iconPath, isCalamity, statsTooltip, itemId);
    }
}
