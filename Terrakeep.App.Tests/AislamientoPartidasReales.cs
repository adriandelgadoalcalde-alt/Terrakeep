// AISLAMIENTO DE LAS PARTIDAS REALES DEL USUARIO (H-04, segunda revision visual de la FASE B del
// responsive global, 28-sep-2026). Regla del CLAUDE.md del repo (commit 202117a0): ninguna prueba
// ni canario toca jamas Documents\My Games\Terraria\...\Players\*.plr/.tplr reales.
//
// Incidente que lo origina (ver bitacora.md): el canario EQUIP_RESPONSIVE_SOLO abria
// vm.Home.Characters.FirstOrDefault() - Eldelgas.plr REAL - y, con un IsDirty espurio, el dialogo de
// "cambios sin guardar" acabo guardandolo 3 veces. Program.cs (HOME-OPEN) hacia lo mismo con
// Characters[0] para TODOS los modos del arnes.
//
// Que hace, una sola vez al arrancar el arnes (antes de "new MainWindow()", que ya escanea Inicio):
//   1. Copia SOLO los .plr/.tplr de nivel superior de las carpetas reales de personajes (tModLoader y
//      vainilla) a %TEMP%\TerrakeepArnes-<pid>\{tModLoader\Players, Players}. Conserva el segmento
//      "tModLoader" de la ruta (CharacterFileService.Load lo usa para distinguir tModLoader/vainilla)
//      y la fecha de modificacion (File.Copy la conserva: Inicio sigue ordenando igual, asi los
//      canarios que abren Characters[0] siguen viendo el mismo personaje, pero la COPIA).
//   2. CharacterFileService.CarpetasPersonajesDePrueba = esas dos carpetas: SUSTITUYE (no suma) las
//      carpetas reales, incluidas las extra de Ajustes.
//   3. Guarda %LOCALAPPDATA%\Terrakeep\session.json y lo restaura al salir (ProcessExit): el arnes
//      carga personajes y MainWindow reescribe ese fichero con rutas temporales.
//   4. Al salir borra la carpeta temporal.
// Ademas expone ComprobarPersonajeAislado(vm, contexto): los canarios la llaman tras cargar un
// personaje; si el .plr cargado esta FUERA de la carpeta temporal imprime FALLO y aborta el proceso
// antes de que ninguna accion pueda escribir en el.
//
// Los MUNDOS (.wld) no estan cubiertos aqui: varios canarios abren rutas absolutas reales de
// Worlds\ (roca_negra.wld, Blando_Rio.wld) - pendiente documentado en bitacora.md.
using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static string? _raizAislada;
    private static string? _sessionJsonGuardado;
    private static bool _sessionJsonExistia;

    internal static string RaizPersonajesAislada => _raizAislada ?? throw new InvalidOperationException("Aislamiento no preparado");

    private static void PrepararAislamientoPartidasReales()
    {
        string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var origenes = new (string real, string relativo)[]
        {
            (Path.Combine(documentos, "My Games", "Terraria", "tModLoader", "Players"), Path.Combine("tModLoader", "Players")),
            (Path.Combine(documentos, "My Games", "Terraria", "Players"), "Players"),
        };
        _raizAislada = Path.Combine(Path.GetTempPath(), $"TerrakeepArnes-{Environment.ProcessId}");
        var destinos = new List<string>();
        int copiados = 0;
        foreach (var (real, relativo) in origenes)
        {
            string destino = Path.Combine(_raizAislada, relativo);
            Directory.CreateDirectory(destino);
            destinos.Add(destino);
            if (!Directory.Exists(real)) continue;
            foreach (string f in Directory.EnumerateFiles(real))
            {
                string ext = Path.GetExtension(f);
                if (!ext.Equals(".plr", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".tplr", StringComparison.OrdinalIgnoreCase)) continue;
                File.Copy(f, Path.Combine(destino, Path.GetFileName(f)), overwrite: true);
                copiados++;
            }
        }
        CharacterFileService.CarpetasPersonajesDePrueba = destinos;

        string sessionJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "session.json");
        _sessionJsonExistia = File.Exists(sessionJson);
        _sessionJsonGuardado = _sessionJsonExistia ? File.ReadAllText(sessionJson) : null;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (_sessionJsonExistia) File.WriteAllText(sessionJson, _sessionJsonGuardado!);
                else if (File.Exists(sessionJson)) File.Delete(sessionJson);
            }
            catch (Exception ex) { Console.WriteLine("AISLAMIENTO: AVISO - no se pudo restaurar session.json: " + ex.Message); }
            try { if (_raizAislada != null && Directory.Exists(_raizAislada)) Directory.Delete(_raizAislada, recursive: true); }
            catch { /* un fichero aun abierto no debe tapar la salida real del arnes */ }
        };

        // Guarda de configuracion: si por lo que sea el servicio siguiera viendo una carpeta real,
        // el arnes NO sigue.
        foreach (string d in CharacterFileService.GetAllPlayersDirectories())
            if (!EstaDentro(d, _raizAislada))
            {
                Console.WriteLine($"FALLO: AISLAMIENTO - CharacterFileService sigue escaneando una carpeta real ({d}); el arnes se detiene sin abrir nada.");
                Environment.Exit(3);
            }
        Console.WriteLine($"AISLAMIENTO: {copiados} .plr/.tplr reales COPIADOS a {_raizAislada}; carpetas reales sustituidas; session.json guardado (existia={_sessionJsonExistia}) y se restaurara al salir.");
    }

    private static bool EstaDentro(string ruta, string raiz)
    {
        string r = Path.GetFullPath(raiz).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(ruta).StartsWith(r, StringComparison.OrdinalIgnoreCase);
    }

    // Guarda (d): llamar tras cualquier carga de personaje. Aborta el proceso si el personaje
    // cargado no es una copia de la carpeta temporal - nunca se sigue trabajando sobre uno real.
    internal static bool ComprobarPersonajeAislado(MainViewModel vm, string contexto)
    {
        string? ruta = vm.LoadedFilePath;
        if (ruta == null || _raizAislada == null) return true;
        if (EstaDentro(ruta, _raizAislada)) return true;
        Console.WriteLine($"FALLO: AISLAMIENTO [{contexto}] - el personaje cargado '{ruta}' esta FUERA de la carpeta temporal {_raizAislada}. Se aborta el arnes sin tocar nada mas.");
        vm.IsDirty = false; // que ningun cierre ofrezca guardarlo
        Environment.Exit(4);
        return false;
    }
}
