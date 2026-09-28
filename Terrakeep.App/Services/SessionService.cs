using System.IO;
using System.Text.Json;

namespace Terrakeep.App.Services;

// H5-07 (quinta auditoria de Opus): "lo unico persistido entre sesiones es tamaño/posicion de
// ventana - cada arranque vuelve a Inicio sin personaje". session.json (mismo vehiculo real que
// settings.json/window.json) recuerda el ULTIMO personaje real - Inicio ofrece "Continuar con
// Nombre" como accion destacada, nunca carga automatica silenciosa (LastModifiedUtc guardado
// junto a la ruta es lo que permite avisar si el fichero cambio por fuera desde entonces).
public sealed class TerrakeepSession
{
    public string? LastCharacterPath { get; set; }
    public string? LastCharacterName { get; set; }
    public DateTime? LastCharacterModifiedUtc { get; set; }

    // Catalogo de rediseño visual T4 (21-sep-2026, "Tu ultimo mundo"): mismo mecanismo real que
    // LastCharacterPath, para el ULTIMO .wld cargado con exito en Exploracion - ver
    // ExplorationViewModel.CurrentWorldPath y MainViewModel.SaveSession.
    public string? LastWorldPath { get; set; }
    public string? LastWorldName { get; set; }

    // "Pestaña/sub-pestaña, preferencias de plegado, loadout y almacén seleccionados" - pedido
    // explicito del informe. Valores de fabrica reales (los mismos que ya tenia cada propiedad
    // en MainViewModel antes de que esto existiera) para que un session.json ausente/corrupto
    // (primer arranque real) arranque exactamente igual que antes de H5-07.
    public int SelectedTabIndex { get; set; }
    public int PersonajeInnerTabIndex { get; set; }
    public int ObjetosSubTabIndex { get; set; }
    public bool IsLibraryCollapsed { get; set; } = true;
    public bool IsBuffLibraryCollapsed { get; set; } = true;
    public int SelectedLoadout { get; set; }
    public int SelectedStorageIndex { get; set; }
}

public static class SessionService
{
    // Carpeta de estado centralizada (CarpetaEstadoApp): el arnes la redirige a su carpeta temporal
    // en modo diagnostico - propiedad, no static readonly, para que la redireccion se respete siempre.
    private static string FilePath => CarpetaEstadoApp.Ruta("session.json");

    public static TerrakeepSession Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new TerrakeepSession();
            return JsonSerializer.Deserialize<TerrakeepSession>(File.ReadAllText(FilePath)) ?? new TerrakeepSession();
        }
        catch (Exception)
        {
            return new TerrakeepSession();
        }
    }

    public static void Save(TerrakeepSession session)
    {
        try
        {
            if (!CarpetaEstadoApp.PermiteEscribir(FilePath)) return; // guarda del arnes (ver CarpetaEstadoApp)
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(session));
        }
        // Auditoria final de Opus (5-sep-2026): se capturaba solo IOException, pero
        // UnauthorizedAccessException NO deriva de ella - una carpeta de AppData sin permiso
        // de escritura (politica de empresa, antivirus, perfil restringido) escapaba de este
        // "best-effort" y salia como error real al usuario, justo lo contrario de lo que dice
        // el comentario. Mismo criterio que la LECTURA de este mismo servicio, que ya captura
        // Exception a secas.
        catch (Exception)
        {
        }
    }
}
