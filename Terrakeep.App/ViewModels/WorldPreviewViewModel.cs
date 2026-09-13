using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.WorldGen;

namespace Terrakeep.App.ViewModels;

// Un efecto de semilla secreta real - fila de solo lectura para el panel (14-sep-2026, punto 9
// de bitacora.md 13-sep-2026: "vista previa de generacion de mundo").
public sealed class SpecialSeedEffectRowViewModel(string name, string description)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}

// Vista previa de generacion de mundo NUEVO (14-sep-2026, punto 9 de la lista confirmada por el
// usuario del 13-sep-2026). Overlay de ventana, mismo mecanismo real ya usado por Compare/
// BuildCode/BackupHistory (velo opaco + panel centrado + Escape/clic fuera lo cierra) - nunca
// dos a la vez. A diferencia de los otros tres, NO depende de ningun personaje/mundo cargado ni
// toca ningun archivo: es un calculador puro sobre las elecciones reales de la pantalla de
// creacion de mundo de Terraria (tamaño/dificultad/bioma maligno/semilla).
//
// El limite honesto (documentado tambien en la interfaz, ver worldpreview_honesty_note):
// Terraria traza el terreno con ruido y pasadas en cascada en el momento de generar - no hay
// forma de saber de antemano donde caera cada bioma/estructura sin reimplementar WorldGen.cs
// entero (miles de lineas). Un mapa en miniatura seria una invencion. Lo que SI se enseña, con
// certeza real: el tamaño EXACTO en tiles (constante del juego), las elecciones reales del
// jugador, y los efectos deterministas de las 8 semillas secretas documentadas si el texto de
// semilla coincide con alguna - mismo criterio real que Starvekeep aplico para DST (ver su
// bitacora.md, 13-sep-2026: "nunca un mapa falso, si un resumen honesto de configuracion").
public sealed partial class WorldPreviewViewModel : ObservableObject
{
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private WorldSizeOption _selectedSize = WorldSizeOption.Medium;
    [ObservableProperty] private int _selectedDifficulty; // 0=Clasico, mismo vocabulario que WldHeader.GameMode
    [ObservableProperty] private WorldEvilOption _selectedEvil = WorldEvilOption.Random;
    [ObservableProperty] private string _seedText = string.Empty;

    public ObservableCollection<SpecialSeedEffectRowViewModel> SpecialSeedEffectRows { get; } = [];
    public bool HasSpecialSeed => SpecialSeedEffectRows.Count > 0;

    public string DimensionsText { get; private set; } = "";

    public WorldPreviewViewModel()
    {
        RebuildSummary();
        // Mismo criterio real que CompareViewModel/ExplorationViewModel: los textos derivados
        // (DimensionsText, nombres/descripciones de efectos) se redactan otra vez con el idioma
        // nuevo sin recalcular nada mas - los datos (Detect/Get) no cambian con el idioma.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, (_, _) => RebuildSummary(), "Item[]");
    }

    [RelayCommand]
    private void Open() => IsOpen = true;

    [RelayCommand]
    private void Close() => IsOpen = false;

    [RelayCommand]
    private void Noop() { } // traga el clic DENTRO del panel, mismo patron que los otros overlays

    partial void OnSelectedSizeChanged(WorldSizeOption value) => RebuildSummary();
    partial void OnSelectedDifficultyChanged(int value) => RebuildSummary();
    partial void OnSelectedEvilChanged(WorldEvilOption value) => RebuildSummary();
    partial void OnSeedTextChanged(string value) => RebuildSummary();

    private void RebuildSummary()
    {
        var loc = LocalizationService.Instance;
        var resumen = WorldCreationSummaryBuilder.Build(SelectedSize, SelectedDifficulty, SelectedEvil, SeedText);

        DimensionsText = loc.Format("worldpreview_dimensions", resumen.TilesWide, resumen.TilesHigh);
        OnPropertyChanged(nameof(DimensionsText));

        SpecialSeedEffectRows.Clear();
        foreach (var efecto in resumen.SpecialSeedEffects)
        {
            string clave = efecto switch
            {
                SpecialSeedEffect.NoTraps => "no_traps",
                SpecialSeedEffect.NotTheBees => "not_the_bees",
                SpecialSeedEffect.ForTheWorthy => "for_the_worthy",
                SpecialSeedEffect.DontDigUp => "dont_dig_up",
                SpecialSeedEffect.CelebrationMk10 => "celebration_mk10",
                SpecialSeedEffect.TheConstant => "the_constant",
                SpecialSeedEffect.DrunkWorld => "drunk_world",
                SpecialSeedEffect.Zenith => "zenith",
                _ => "",
            };
            SpecialSeedEffectRows.Add(new SpecialSeedEffectRowViewModel(
                loc[$"worldpreview_effect_{clave}_name"], loc[$"worldpreview_effect_{clave}_desc"]));
        }
        OnPropertyChanged(nameof(HasSpecialSeed));
    }
}
