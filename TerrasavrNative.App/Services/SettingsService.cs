using System.IO;
using System.Text.Json;

namespace TerrasavrNative.App.Services;

// H5-07 (quinta auditoria de Opus): "quien tenga Terraria en otro disco/Documentos
// redirigidos/instalacion portable ve el lanzador vacio sin forma de arreglarlo desde la app...
// no existe ninguna pantalla de Ajustes". Mismo vehiculo real ya establecido
// (%LOCALAPPDATA%\Terrakeep\*.json, ver WindowPlacementService) - carpetas adicionales de
// personajes/mundos (que CharacterFileService.GetAllPlayersDirectories/GetAllWorldsDirectories
// concatenan a las 2 detectadas) y el N configurable de copias de seguridad que H5-04 dejo
// aparcado a proposito ("20 es un techo fijo razonable mientras tanto").
public sealed class TerrakeepSettings
{
    public List<string> ExtraCharacterFolders { get; set; } = [];
    public List<string> ExtraWorldFolders { get; set; } = [];
    public int BackupHistoryCap { get; set; } = 20;
}

public static class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "settings.json");

    public static TerrakeepSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new TerrakeepSettings();
            return JsonSerializer.Deserialize<TerrakeepSettings>(File.ReadAllText(FilePath)) ?? new TerrakeepSettings();
        }
        catch (Exception)
        {
            return new TerrakeepSettings(); // fichero ausente/corrupto - arranca con los valores de fabrica, nunca revienta el arranque
        }
    }

    public static void Save(TerrakeepSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
        }
        catch (IOException)
        {
            // Best-effort real, mismo criterio que WindowPlacementService.Save - un fallo
            // guardando los ajustes nunca debe impedir seguir usando la app.
        }
    }
}
