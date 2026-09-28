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
//      carpetas reales, incluidas las extra REALES de Ajustes. Las carpetas extra que las propias
//      pruebas crean en %TEMP% y añaden desde Ajustes (H5-07, INI-03/05/07/08/09) SI se escanean: caen
//      dentro de las raices permitidas del arnes (RaicesMundosPermitidasDePrueba, punto 7).
//   3. CARPETA DE ESTADO REDIRIGIDA (revisor visual de la FASE D, 28-sep-2026 - sustituye al
//      "guardar session.json y restaurarlo al salir" de H-04/R2-L2 b). Incidente real: la app
//      reescribia session.json ~4 s DESPUES de "restaurado (ProcessExit)" (apuntando a una copia
//      temporal ya borrada) y los canarios con cambio de idioma dejaban settings.json en "en".
//      Restaurar a posteriori siempre pierde contra una escritura tardia. Ahora
//      CarpetaEstadoApp.CarpetaDePrueba = <carpeta temporal>\estado-app: session.json, settings.json,
//      window.json, world_view_state.json, la cache del catalogo y Backups se leen y se escriben SOLO
//      ahi (se siembra con una COPIA de lectura de los 4 JSON reales y de la cache, para que el arnes
//      arranque con el mismo estado que antes). La app no abre para escribir ningun fichero de
//      %LOCALAPPDATA%\Terrakeep real y no hay nada que restaurar. Guarda: cualquier escritura de
//      estado que siga cayendo en la carpeta real se bloquea e imprime "FALLO: AISLAMIENTO-ESTADO"
//      (CarpetaEstadoApp.PermiteEscribir). AISLAMIENTO_EXCEPCION_SOLO=1 sigue lanzando una excepcion
//      sin capturar tras cargar una copia (el SHA256 de session.json real se compara desde fuera).
//   4. Al salir comprueba la huella de los 4 JSON reales y borra la carpeta temporal
//      (RestaurarAislamiento, idempotente: ProcessExit, excepcion no capturada o Ctrl+C).
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
    private static string? _carpetaEstadoAislada;
    private static int _escriturasEstadoBloqueadas;
    private static readonly string[] FicherosEstadoReales = ["session.json", "settings.json", "window.json", "world_view_state.json"];
    private static readonly Dictionary<string, string> _hashEstadoRealAlArrancar = new(StringComparer.OrdinalIgnoreCase);

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

        // (3) Carpeta de estado redirigida - ver cabecera. Solo se LEEN los ficheros reales (copia de
        // siembra y huella SHA256 para el control de salida); ningun codigo del arnes ni de la app
        // escribe en la carpeta real a partir de aqui.
        _carpetaEstadoAislada = Path.Combine(_raizAislada, "estado-app");
        Directory.CreateDirectory(_carpetaEstadoAislada);
        int estadoSembrado = 0;
        foreach (string nombre in FicherosEstadoReales.Append("library-catalog-cache-v1.bin"))
        {
            string real = Path.Combine(CarpetaEstadoApp.CarpetaReal, nombre);
            if (!File.Exists(real)) continue;
            File.Copy(real, Path.Combine(_carpetaEstadoAislada, nombre), overwrite: true);
            estadoSembrado++;
        }
        foreach (string nombre in FicherosEstadoReales)
            _hashEstadoRealAlArrancar[nombre] = HuellaEstadoReal(nombre);
        CarpetaEstadoApp.CarpetaDePrueba = _carpetaEstadoAislada;
        CarpetaEstadoApp.AlEscribirFueraDePrueba = ruta =>
        {
            Interlocked.Increment(ref _escriturasEstadoBloqueadas);
            Console.WriteLine($"FALLO: AISLAMIENTO-ESTADO - un servicio intento escribir '{ruta}' en la carpeta de estado REAL ({CarpetaEstadoApp.CarpetaReal}) con el arnes aislado; escritura BLOQUEADA.");
        };
        // Control de salida y borrado de la carpeta temporal (idempotente) tambien ante una excepcion
        // no capturada y Ctrl+C (R2-L2 b): ProcessExit no corre en esos casos.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RestaurarAislamiento("ProcessExit");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.WriteLine($"AISLAMIENTO: excepcion no capturada ({(e.ExceptionObject as Exception)?.GetType().Name}); los JSON reales no se han tocado (carpeta de estado redirigida)");
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
        if (!EstaDentro(CarpetaEstadoApp.Carpeta, _raizAislada) || !EstaDentro(new BackupHistoryService().BackupsRoot, _raizAislada))
        {
            Console.WriteLine($"FALLO: AISLAMIENTO-ESTADO - la carpeta de estado sigue siendo la real ({CarpetaEstadoApp.Carpeta}); el arnes se detiene.");
            Environment.Exit(3);
        }
        Console.WriteLine($"AISLAMIENTO: {copiados} .plr/.tplr y {mundosCopiados} .wld/.twld reales COPIADOS a {_raizAislada}; carpetas reales de personajes y mundos sustituidas; carpeta de estado redirigida a {_carpetaEstadoAislada} ({estadoSembrado} fichero(s) de estado copiados como siembra; los reales no se escriben).");
    }

    private static string HuellaEstadoReal(string nombre)
    {
        string ruta = Path.Combine(CarpetaEstadoApp.CarpetaReal, nombre);
        try { return File.Exists(ruta) ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(ruta))) : "(no existe)"; }
        catch (IOException) { return "(no legible)"; }
    }

    private static int _aislamientoRestaurado; // 0/1, Interlocked: una sola restauracion

    /// <summary>Control de salida de la carpeta de estado y borrado de la carpeta temporal. Idempotente:
    /// la primera via que llegue (ProcessExit, excepcion no capturada, Ctrl+C) lo hace. Ya no restaura
    /// nada: los JSON reales nunca se escriben (ver cabecera, punto 3).</summary>
    internal static void RestaurarAislamiento(string via)
    {
        if (_raizAislada == null || Interlocked.Exchange(ref _aislamientoRestaurado, 1) == 1) return;
        try
        {
            var cambiados = FicherosEstadoReales.Where(n => _hashEstadoRealAlArrancar.TryGetValue(n, out var h) && h != HuellaEstadoReal(n)).ToList();
            Console.WriteLine(cambiados.Count == 0
                ? $"AISLAMIENTO-ESTADO: los {FicherosEstadoReales.Length} JSON reales de {CarpetaEstadoApp.CarpetaReal} siguen con el MISMO SHA256 que al arrancar ({via}); escrituras bloqueadas por la guarda={_escriturasEstadoBloqueadas}"
                : $"FALLO: AISLAMIENTO-ESTADO - JSON real(es) cambiado(s) durante el arnes ({via}): {string.Join(", ", cambiados)} (escrito por otro proceso o por una via que se salta CarpetaEstadoApp)");
        }
        catch (Exception ex) { Console.WriteLine($"AISLAMIENTO-ESTADO: AVISO - no se pudo comprobar la huella de los JSON reales ({via}): " + ex.Message); }
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
