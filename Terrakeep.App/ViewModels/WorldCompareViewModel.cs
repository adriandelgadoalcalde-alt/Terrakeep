using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    // Catalogo de ideas Keep, idea 8 ("Informe y comparador de mundos" - "Salida como tarjeta
    // compartible (PNG/HTML)", reconsiderada a peticion explicita del coordinador el 20-sep-2026:
    // "añade la exportacion como tarjeta PNG/HTML a WorldCompareViewModel"). La ronda anterior
    // cerro el COMPARADOR en si (StatRows) pero nunca la exportacion como tarjeta - ninguna otra
    // parte del proyecto tenia un patron de "tarjeta compartible" que copiar (CompareViewModel,
    // el comparador de personajes, tampoco exporta nada), asi que las dos salidas se construyen
    // aqui mismo, sobre los MISMOS StatRows/NameA/NameB que ya alimenta la pantalla real - nunca
    // un segundo calculo aparte que pudiera desincronizarse.
    //
    // HTML: un fichero autocontenido (CSS inline, sin dependencias externas) - se abre en
    // cualquier navegador/se pega en cualquier sitio que acepte HTML, fiel al mismo criterio de
    // "no inventar nada" (solo los datos ya calculados, texto ya escapado).
    public void ExportCardToHtml(string path)
    {
        if (!HasBothLoaded) return;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{Html(NameA)} vs {Html(NameB)}</title>");
        sb.AppendLine("""
            <style>
              body { background:#181a24; color:#e8e8f0; font-family:'Segoe UI',sans-serif; padding:24px; }
              .card { max-width:640px; margin:0 auto; background:#22242f; border-radius:10px; padding:20px; }
              h1 { font-size:16px; margin:0 0 16px 0; text-align:center; }
              table { width:100%; border-collapse:collapse; font-size:13px; }
              th { text-align:left; padding:6px 8px; color:#9a9ab0; font-weight:600; border-bottom:1px solid #363948; }
              td { padding:6px 8px; border-bottom:1px solid #2b2e3a; }
              tr.diff td { background:#3a2f1a; color:#ffb84d; }
              td.label { color:#9a9ab0; }
            </style>
            """);
        sb.AppendLine("</head><body><div class=\"card\">");
        sb.AppendLine($"<h1>{Html(NameA)} vs {Html(NameB)}</h1>");
        sb.AppendLine("<table><tr><th></th><th>" + Html(NameA) + "</th><th>" + Html(NameB) + "</th></tr>");
        foreach (var fila in StatRows)
        {
            sb.AppendLine(fila.IsDifferent ? "<tr class=\"diff\">" : "<tr>");
            sb.AppendLine($"<td class=\"label\">{Html(fila.Label)}</td><td>{Html(fila.ValueA)}</td><td>{Html(fila.ValueB)}</td></tr>");
        }
        sb.AppendLine("</table></div></body></html>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Html(string? texto) => System.Net.WebUtility.HtmlEncode(texto ?? "");

    // PNG: mismo contenido real que la tarjeta HTML, compuesto a mano con DrawingVisual/
    // FormattedText (mismo mecanismo ya real de PlayerPreviewRenderer/ExplorationViewModel.
    // DrawMarkersForExport) - una imagen que se puede pegar donde el HTML no sirve (Discord,
    // capturas...).
    public void ExportCardToPng(string path)
    {
        if (!HasBothLoaded) return;
        const int width = 640, padding = 20, rowHeight = 26, headerHeight = 60;
        int height = headerHeight + StatRows.Count * rowHeight + padding;

        var fondo = new SolidColorBrush(Color.FromRgb(0x22, 0x24, 0x2F));
        var fondoDiff = new SolidColorBrush(Color.FromRgb(0x3A, 0x2F, 0x1A));
        var textoNormal = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0));
        var textoDiff = new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x4D));
        var textoEtiqueta = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xB0));
        var typeface = new Typeface("Segoe UI");
        var typefaceBold = new Typeface(new FontFamily("Segoe UI"), System.Windows.FontStyles.Normal, System.Windows.FontWeights.Bold, System.Windows.FontStretches.Normal);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(fondo, null, new System.Windows.Rect(0, 0, width, height));

            FormattedText Texto(string s, Typeface tf, double size, Brush color) =>
                new(s, System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, tf, size, color, 1.0);

            var titulo = Texto($"{NameA} vs {NameB}", typefaceBold, 16, textoNormal);
            dc.DrawText(titulo, new System.Windows.Point((width - titulo.Width) / 2, padding));

            double colLabel = padding, colA = width * 0.42, colB = width * 0.71;
            double y = headerHeight;
            var cabeceraA = Texto(NameA ?? "", typefaceBold, 12, textoEtiqueta);
            var cabeceraB = Texto(NameB ?? "", typefaceBold, 12, textoEtiqueta);
            dc.DrawText(cabeceraA, new System.Windows.Point(colA, y));
            dc.DrawText(cabeceraB, new System.Windows.Point(colB, y));
            y += rowHeight;

            foreach (var fila in StatRows)
            {
                if (fila.IsDifferent)
                    dc.DrawRectangle(fondoDiff, null, new System.Windows.Rect(0, y - 2, width, rowHeight));
                var colorFila = fila.IsDifferent ? textoDiff : textoNormal;
                dc.DrawText(Texto(fila.Label, typeface, 12, textoEtiqueta), new System.Windows.Point(colLabel, y));
                dc.DrawText(Texto(fila.ValueA, typeface, 12, colorFila), new System.Windows.Point(colA, y));
                dc.DrawText(Texto(fila.ValueB, typeface, 12, colorFila), new System.Windows.Point(colB, y));
                y += rowHeight;
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
