using System.IO;
using System.Text.Json;

namespace TerrasavrNative.App.Services;

// F-11 (auditoria de Opus vs TEdit, E-11): "cierras TEdit mirando una cueva concreta de un
// mundo concreto, y al reabrir estas exactamente ahi" (WorldViewStateManager.cs real de TEdit).
// Diccionario ruta de mundo -> {Zoom, OffsetH, OffsetV} (offsets en pixeles post-zoom, mismo
// espacio que ya usa el resto de gestos del mapa - OnNavigateToTile/OnWorldMapPreviewMouseWheel
// en MainWindow.xaml.cs). Clave: la ruta ABSOLUTA del .wld tal cual la usa el resto del
// proyecto (CurrentWorldPath) - dos mundos distintos con el mismo Title no colisionan.
public sealed record WorldViewState(double Zoom, double OffsetH, double OffsetV);

public static class WorldViewStateService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "world_view_state.json");

    public static Dictionary<string, WorldViewState> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return [];
            return JsonSerializer.Deserialize<Dictionary<string, WorldViewState>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch (Exception)
        {
            return []; // fichero ausente/corrupto - arranca en blanco, nunca revienta el arranque
        }
    }

    // Escritura atomica real (mismo criterio que TEdit, WorldViewStateManager.cs:59-61: escribe
    // a un .tmp aparte primero y solo AL FINAL lo intercambia por el real de un solo paso -
    // nunca hay un instante donde el fichero este a medio escribir si el proceso muere en medio).
    // Mas simple que CharacterFileService.WriteAtomic a proposito: ese genera ademas un .bak
    // (relevante para un .plr real del usuario), aqui es solo una preferencia de vista, no hace
    // falta historial de "deshacer el ultimo guardado".
    public static void Save(Dictionary<string, WorldViewState> states)
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            string tmpPath = FilePath + ".tmp";
            File.WriteAllText(tmpPath, JsonSerializer.Serialize(states));
            File.Move(tmpPath, FilePath, overwrite: true);
        }
        // Auditoria final de Opus (5-sep-2026): se capturaba solo IOException, pero
        // UnauthorizedAccessException NO deriva de ella - una carpeta de AppData sin permiso
        // de escritura (politica de empresa, antivirus, perfil restringido) escapaba de este
        // "best-effort" y salia como error real al usuario, justo lo contrario de lo que dice
        // el comentario. Mismo criterio que la LECTURA de este mismo servicio, que ya captura
        // Exception a secas.
        catch (Exception)
        {
            // Best-effort real, mismo criterio que SettingsService.Save - un fallo guardando la
            // vista nunca debe impedir seguir usando la app.
        }
    }
}
