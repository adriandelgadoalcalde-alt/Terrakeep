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
//      carga personajes y MainWindow reescribe ese fichero con rutas temporales. Desde R2-L2 (b)
//      tambien ante una excepcion no capturada y Ctrl+C (RestaurarAislamiento, idempotente), con
//      los bytes exactos; AISLAMIENTO_EXCEPCION_SOLO=1 lo prueba lanzando una excepcion a proposito.
//   4. Al salir borra la carpeta temporal.
// Ademas expone ComprobarPersonajeAislado(vm, contexto): los canarios la llaman tras cargar un
// personaje; si el .plr cargado esta FUERA de la carpeta temporal imprime FALLO y aborta el proceso
// antes de que ninguna accion pueda escribir en el.
//
// MUNDOS (FASE C del responsive global, 28-sep-2026 - cierra el pendiente documentado en la FASE B):
//   5. Copia los .wld/.twld de nivel superior de tModLoader\Worlds y Worlds (vainilla) a
//      <carpeta temporal>\{tModLoader\Worlds, Worlds} y CharacterFileService.CarpetasMundosDePrueba
//      SUSTITUYE las carpetas reales (Exploracion y la busqueda global solo ven las copias).
//   6. MundoAislado(rutaReal): los ~49 literales del arnes que abrian Documents\My Games\Terraria\...
//      \*.wld por ruta absoluta pasan por aqui y reciben la COPIA (misma ruta relativa dentro de la
//      carpeta temporal; si un mundo no estaba copiado - p.ej. KeepQA-Vanilla-Server - se copia al
//      pedirlo; si no existe en esta maquina, la ruta devuelta tampoco existe y el File.Exists del
//      llamante se comporta igual que antes).
//   7. Guarda: CharacterFileService.RaicesMundosPermitidasDePrueba = {carpeta temporal del arnes,
//      %TEMP%}; cualquier apertura/escritura real de un mundo fuera de ellas (Exploracion, Comparar,
//      busqueda global, WorldFileService) imprime FALLO: AISLAMIENTO-MUNDO y aborta el proceso.
using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static string? _raizAislada;
    private static byte[]? _sessionJsonGuardado;
    private static string? _rutaSessionJson;
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

        // Mundos (5-7, ver cabecera).
        var origenesMundos = new (string real, string relativo)[]
        {
            (Path.Combine(documentos, "My Games", "Terraria", "tModLoader", "Worlds"), Path.Combine("tModLoader", "Worlds")),
            (Path.Combine(documentos, "My Games", "Terraria", "Worlds"), "Worlds"),
        };
        var destinosMundos = new List<string>();
        int mundosCopiados = 0;
        foreach (var (real, relativo) in origenesMundos)
        {
            string destino = Path.Combine(_raizAislada, relativo);
            Directory.CreateDirectory(destino);
            destinosMundos.Add(destino);
            if (!Directory.Exists(real)) continue;
            foreach (string f in Directory.EnumerateFiles(real))
            {
                string ext = Path.GetExtension(f);
                if (!ext.Equals(".wld", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".twld", StringComparison.OrdinalIgnoreCase)) continue;
                File.Copy(f, Path.Combine(destino, Path.GetFileName(f)), overwrite: true);
                mundosCopiados++;
            }
        }
        CharacterFileService.CarpetasMundosDePrueba = destinosMundos;
        CharacterFileService.RaicesMundosPermitidasDePrueba = [_raizAislada, Path.GetTempPath()];
        CharacterFileService.AlTocarMundoFueraDePrueba = ruta =>
        {
            Console.WriteLine($"FALLO: AISLAMIENTO-MUNDO - se intento abrir o escribir el mundo '{ruta}', FUERA de la carpeta temporal del arnes ({_raizAislada}) y de %TEMP%. Se aborta el arnes sin tocarlo.");
            Environment.Exit(5);
        };

        _rutaSessionJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "session.json");
        _sessionJsonExistia = File.Exists(_rutaSessionJson);
        // Bytes exactos (no texto): la restauracion tiene que dejar el MISMO SHA256, BOM incluido.
        _sessionJsonGuardado = _sessionJsonExistia ? File.ReadAllBytes(_rutaSessionJson) : null;
        // R2-L2 (b): ProcessExit NO se dispara si el proceso muere por una excepcion no capturada -
        // session.json se quedaba con las rutas temporales del arnes. La restauracion (idempotente)
        // se engancha tambien a AppDomain.UnhandledException y a Ctrl+C. No hace falta un manejador
        // propio de Dispatcher.UnhandledException: Program.Main ya marca Handled=true en
        // app.DispatcherUnhandledException (el arnes sigue vivo y ProcessExit restaurara al final), y
        // una excepcion del dispatcher que nadie maneje se relanza y acaba en
        // AppDomain.UnhandledException. Suscribirse aqui seria ANTES que la Application (se crea
        // despues) y veria Handled=false aunque luego se manejase - restauraria a destiempo.
        // Un "taskkill /F" sigue sin poder interceptarse (limite real del SO).
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RestaurarAislamiento("ProcessExit");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.WriteLine($"AISLAMIENTO: excepcion no capturada ({(e.ExceptionObject as Exception)?.GetType().Name}); se restaura session.json antes de morir");
            RestaurarAislamiento("UnhandledException");
            ValidarCapturasDeEstaEjecucion();
        };
        Console.CancelKeyPress += (_, _) => RestaurarAislamiento("CancelKeyPress");

        // Guarda de configuracion: si por lo que sea el servicio siguiera viendo una carpeta real,
        // el arnes NO sigue.
        foreach (string d in CharacterFileService.GetAllPlayersDirectories())
            if (!EstaDentro(d, _raizAislada))
            {
                Console.WriteLine($"FALLO: AISLAMIENTO - CharacterFileService sigue escaneando una carpeta real ({d}); el arnes se detiene sin abrir nada.");
                Environment.Exit(3);
            }
        foreach (string d in CharacterFileService.GetAllWorldsDirectories())
            if (!EstaDentro(d, _raizAislada))
            {
                Console.WriteLine($"FALLO: AISLAMIENTO-MUNDO - CharacterFileService sigue escaneando una carpeta de mundos real ({d}); el arnes se detiene sin abrir nada.");
                Environment.Exit(3);
            }
        Console.WriteLine($"AISLAMIENTO: {copiados} .plr/.tplr y {mundosCopiados} .wld/.twld reales COPIADOS a {_raizAislada}; carpetas reales de personajes y mundos sustituidas; session.json guardado (existia={_sessionJsonExistia}) y se restaurara al salir.");
    }

    private static int _aislamientoRestaurado; // 0/1, Interlocked: una sola restauracion

    /// <summary>Devuelve session.json a sus bytes originales y borra la carpeta temporal. Idempotente:
    /// la primera via que llegue (ProcessExit, excepcion no capturada, Ctrl+C) la hace.</summary>
    internal static void RestaurarAislamiento(string via)
    {
        if (_rutaSessionJson == null || Interlocked.Exchange(ref _aislamientoRestaurado, 1) == 1) return;
        try
        {
            if (_sessionJsonExistia) File.WriteAllBytes(_rutaSessionJson, _sessionJsonGuardado!);
            else if (File.Exists(_rutaSessionJson)) File.Delete(_rutaSessionJson);
            Console.WriteLine($"AISLAMIENTO: session.json restaurado ({via})");
        }
        catch (Exception ex) { Console.WriteLine($"AISLAMIENTO: AVISO - no se pudo restaurar session.json ({via}): " + ex.Message); }
        try { if (_raizAislada != null && Directory.Exists(_raizAislada)) Directory.Delete(_raizAislada, recursive: true); }
        catch { /* un fichero aun abierto no debe tapar la salida real del arnes */ }
    }

    // (6) Ruta de la COPIA aislada de un mundo real. Solo acepta rutas bajo Documents\My Games\Terraria\
    // (las que el arnes usaba a pelo); cualquier otra se devuelve tal cual (ya es una copia/fixture).
    internal static string MundoAislado(string rutaReal)
    {
        string raizJuego = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
        if (_raizAislada == null || !EstaDentro(rutaReal, raizJuego)) return rutaReal;
        string relativa = Path.GetRelativePath(raizJuego, rutaReal);
        string copia = Path.Combine(_raizAislada, relativa);
        if (!File.Exists(copia) && File.Exists(rutaReal))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(copia)!);
            File.Copy(rutaReal, copia, overwrite: false);
            string twld = Path.ChangeExtension(rutaReal, ".twld");
            if (File.Exists(twld)) File.Copy(twld, Path.ChangeExtension(copia, ".twld"), overwrite: true);
        }
        return copia;
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
