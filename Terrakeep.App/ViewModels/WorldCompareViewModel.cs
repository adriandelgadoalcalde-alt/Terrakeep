using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels;

// Comparador de mundos (idea 8 del catalogo de funciones, "Informe y comparador de mundos" -
// bitacora.md 20-sep-2026, investigacion de la ronda anterior): el "informe" de UN mundo ya
// existia (ExplorationViewModel.BuildWorldReportText, boton "Guardar informe..."); lo que faltaba
// de verdad era el COMPARADOR de DOS mundos lado a lado - WorldCreationSummaryBuilder (citado por
// el catalogo como apoyo) resulto ser para la vista previa de GENERACION, nunca para esto.
//
// Mismo patron ya probado por CompareViewModel (personajes, T9): cada lado se carga con su PROPIO
// WldReader, independiente del mundo que pueda estar abierto en la pestaña Exploracion - comparar
// no exige tener nada cargado antes (misma filosofia que "No exige tener ningun personaje cargado"
// del comparador de personajes).
//
// Alcance deliberado: cabecera + censo barato (Npcs/Chests/Signs, ya vienen contados en WldWorld
// sin recorrer la rejilla aparte) - NO repite el escaneo completo tile-por-tile
// (WorldPresenceIndex.Build) que solo aporta valor para NAVEGAR el mundo (buscador, vetas), no
// para esta pantalla de identidad/progreso. WldReader.Read ya lee la rejilla completa de por si
// (formato .wld, sin lectura parcial posible) asi que ese coste ya se paga una vez por lado sin
// poder evitarlo - pero construir el indice completo ENCIMA solo para 12 filas de cabecera no
// aporta nada real a este panel.
public sealed partial class WorldCompareViewModel : ObservableObject
{
    [ObservableProperty] private string? _pathA;
    [ObservableProperty] private string? _pathB;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    private WldWorld? _worldA;
    private WldWorld? _worldB;

    public ObservableCollection<CompareStatRowViewModel> StatRows { get; } = [];

    public bool HasBothLoaded => _worldA != null && _worldB != null;
    public string? NameA => PathA == null ? null : Path.GetFileName(PathA);
    public string? NameB => PathB == null ? null : Path.GetFileName(PathB);

    public int DifferenceCount => StatRows.Count(r => r.IsDifferent);

    public async Task LoadAAsync(string path) => await LoadSideAsync(path, isA: true);
    public async Task LoadBAsync(string path) => await LoadSideAsync(path, isA: false);

    private async Task LoadSideAsync(string path, bool isA)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var world = await Task.Run(() => WldReader.Read(File.ReadAllBytes(path)));
            if (isA) { _worldA = world; PathA = path; }
            else { _worldB = world; PathB = path; }
        }
        catch (Exception ex)
        {
            // Mismo criterio de siempre (CompareViewModel.TryLoadAndCompare): un fichero
            // corrupto/ajeno no debe tumbar el panel, solo avisar con el nombre real y la
            // excepcion real.
            ErrorMessage = LocalizationService.Instance.Format("compare_load_error", ex.Message);
            IsLoading = false;
            OnPropertyChanged(nameof(HasBothLoaded));
            return;
        }
        IsLoading = false;
        RebuildRows();
        OnPropertyChanged(nameof(HasBothLoaded));
        OnPropertyChanged(nameof(NameA));
        OnPropertyChanged(nameof(NameB));
    }

    private void RebuildRows()
    {
        StatRows.Clear();
        if (_worldA == null || _worldB == null) { OnPropertyChanged(nameof(DifferenceCount)); return; }
        var loc = LocalizationService.Instance;
        var a = _worldA.Header;
        var b = _worldB.Header;

        AddStat("compare_world_title", a.Title, b.Title);
        AddStat("compare_world_seed", a.Seed, b.Seed);
        AddStat("compare_world_special_seed", ExplorationViewModel.SpecialSeedLabel(a.Seed), ExplorationViewModel.SpecialSeedLabel(b.Seed));
        AddStat("compare_world_size", $"{a.TilesWide}×{a.TilesHigh}", $"{b.TilesWide}×{b.TilesHigh}");
        AddStat("compare_world_format_version", a.Version.ToString(), b.Version.ToString());
        AddStat("compare_world_game_mode", ExplorationViewModel.GameModeLabel(a.GameMode), ExplorationViewModel.GameModeLabel(b.GameMode));
        AddStat("compare_world_hardmode", loc[a.HardMode ? "compare_yes" : "compare_no"], loc[b.HardMode ? "compare_yes" : "compare_no"]);
        AddStat("compare_world_evil_biome", loc[a.IsCrimson ? "worldpreview_evil_crimson" : "worldpreview_evil_corruption"], loc[b.IsCrimson ? "worldpreview_evil_crimson" : "worldpreview_evil_corruption"]);

        var (downedA, totalA) = ExplorationViewModel.CountDownedBosses(a);
        var (downedB, totalB) = ExplorationViewModel.CountDownedBosses(b);
        AddStat("compare_world_bosses", $"{downedA}/{totalA}", $"{downedB}/{totalB}");

        AddStat("compare_world_npcs", _worldA.Npcs.Count.ToString(), _worldB.Npcs.Count.ToString());
        AddStat("compare_world_chests", _worldA.Chests.Count.ToString(), _worldB.Chests.Count.ToString());
        AddStat("compare_world_signs", _worldA.Signs.Count.ToString(), _worldB.Signs.Count.ToString());

        OnPropertyChanged(nameof(DifferenceCount));
    }

    private void AddStat(string labelKey, string valueA, string valueB) =>
        StatRows.Add(new CompareStatRowViewModel(LocalizationService.Instance[labelKey], valueA, valueB, !string.Equals(valueA, valueB, StringComparison.Ordinal)));
}
