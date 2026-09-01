using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Builds" - solo lectura por ahora (auto-equipar es un paso posterior). Dos catalogos
// (vanilla/Calamity), cada uno con sus etapas y clases ya resueltas a nombre real en español.
public sealed class BuildsViewModel(BuildsCatalog vanilla, BuildsCatalog calamity)
{
    public IReadOnlyList<BuildStage> VanillaStages { get; } = vanilla.Stages;
    public IReadOnlyList<BuildStage> CalamityStages { get; } = calamity.Stages;
}
