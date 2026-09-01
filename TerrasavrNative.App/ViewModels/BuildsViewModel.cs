using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Builds" - equipo recomendado por etapa/clase, ya resuelto a nombre+icono real (misma
// resolucion de pid->objeto que usa BuildItemResolver para auto-equipar, pero aqui solo para
// mostrar) mas el objeto Source original para el boton "Auto-equipar".
public sealed class BuildsViewModel
{
    public IReadOnlyList<BuildStageViewModel> VanillaStages { get; }
    public IReadOnlyList<BuildStageViewModel> CalamityStages { get; }

    public BuildsViewModel(BuildsCatalog vanilla, BuildsCatalog calamity, CharacterFileService service)
    {
        VanillaStages = vanilla.Stages.Select(s => ResolveStage(s, service)).ToList();
        CalamityStages = calamity.Stages.Select(s => ResolveStage(s, service)).ToList();
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

        if (!string.IsNullOrEmpty(itemRef.Pid))
        {
            int slash = itemRef.Pid.IndexOf('/');
            if (slash >= 0)
            {
                isCalamity = true;
                var entry = service.CalamityCatalog.ByModAndInternal(itemRef.Pid[..slash], itemRef.Pid[(slash + 1)..]);
                if (entry?.Icon != null) iconPath = "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon;
                if (entry != null) statsTooltip = ItemStatsFormatter.Format(true, entry.SyntheticId, service.VanillaStats, service.CalamityCatalog);
            }
            else
            {
                var id = service.VanillaCatalog.GetIdByKey(itemRef.Pid);
                if (id != null)
                {
                    iconPath = VanillaIconResolver.GetIconPath(id.Value);
                    statsTooltip = ItemStatsFormatter.Format(false, id.Value, service.VanillaStats, service.CalamityCatalog);
                }
            }
        }

        return new BuildItemRowViewModel(itemRef.DisplayName, itemRef.Prefix, iconPath, isCalamity, statsTooltip);
    }
}
