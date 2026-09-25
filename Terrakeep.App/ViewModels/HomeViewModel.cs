using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Guia;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Auditoria de Opus, I-1 ("Inicio deberia ser un lanzador real, no una pagina de bienvenida
// estatica que obliga a abrir el Explorador de archivos incluso para el caso normal"): escanea
// UNA VEZ al arrancar (y bajo demanda con "Actualizar", por si se ha guardado algo nuevo desde
// fuera) la carpeta real de personajes de tModLoader - la misma que ya usaba el dialogo de
// "Cargar personaje..." como carpeta inicial (CharacterFileService.GetDefaultPlayersDirectory).
// Solo lee lo minimo real de cada .plr (PlrFile.Read completo, son ficheros pequeños - sin
// tocar el .tplr, que solo hace falta para editar de verdad) - un error real leyendo un fichero
// concreto (corrupto, formato ajeno) no debe tumbar el listado entero de los demas.
//
// T-G (segunda auditoria de Opus, Fable): "arranque sincrono - HomeViewModel.Refresh() async,
// con IsScanning (ya existia como propiedad, pero SIN NINGUN binding real en el XAML - un
// interruptor que nunca encendia nada)". Medido de verdad antes de tocar nada (mismo criterio
// que X-7): ~97ms reales en esta maquina con solo 3 personajes (Debug, primera pasada -
// mayoria coste de JIT en frio de PlrFile.Read/AES/NBT, no I/O puro) - constructor de
// HomeViewModel corre COMO PARTE del constructor de MainViewModel, que a su vez corre ANTES de
// que MainWindow.InitializeComponent() pueda arrancar (field initializers de C#, orden real) -
// asi que esto retrasaba la ventana entera, no solo el listado de Inicio. Async de verdad
// (Task.Run para el escaneo real de disco - mismo patron ya usado y probado en
// ExplorationViewModel.LoadFromPathAsync) deja que la ventana aparezca sin esperar, y con
// muchos mas personajes reales (o una carpeta de Documentos sincronizada por OneDrive, I/O
// real mucho mas lento que esta maquina) la diferencia seria mucho mayor todavia.
public partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<CharacterListEntryViewModel> Characters { get; } = [];

    [ObservableProperty] private bool _isScanning;

    // Oleada de pruebas del 6-sep-2026 (bloque INI-04 del arnes) - BUG REAL: esto era
    // `[ObservableProperty] private string? _scanMessage` y todos los sitios que lo rellenan le
    // metian el texto YA RESUELTO (`Loc.Format(...)`). Un texto resuelto una sola vez se queda
    // congelado en el idioma que hubiera en ese instante: con la app en español, "Fulano no
    // tiene ninguna copia de seguridad que restaurar" seguia en español despues de cambiar a
    // ingles en vivo, y lo mismo el "Ningun personaje encontrado en..." que ve de entrada quien
    // no tenga Terraria en la ruta habitual. El barrido de idioma (A10-IDIOMA-BARRIDO) no lo
    // detectaba porque compara lo que hay EN PANTALLA en ese momento, y este mensaje casi nunca
    // lo esta cuando se hace el barrido.
    //
    // Se guarda la CLAVE del diccionario y sus argumentos reales; el texto se compone al leerlo,
    // en el idioma activo, y un cambio de idioma dispara PropertyChanged (ver OnIdiomaCambiado)
    // para que lo que ya este en pantalla se reescriba solo.
    private string? _scanMessageKey;
    private object?[] _scanMessageArgs = [];
    // Caso aparte real: la lista de carpetas escaneadas se une con un separador que TAMBIEN es
    // texto traducido (" ni en " / " nor in "). Unirla al guardar dejaria ese separador
    // congelado dentro del argumento, asi que se guardan las carpetas y se unen al leer.
    private IReadOnlyList<string>? _scanMessageFolders;

    // INI-07 (misma oleada) - SEGUNDO BUG REAL en el mismo campo: aqui se metian DOS cosas que no
    // se parecen en nada - "no encontre ningun personaje" (una invitacion a cargar uno a mano) y
    // "la accion que acabas de pedir ha fallado". El XAML solo sabia pintar la primera: un boton
    // grande titulado "Empezar: cargar un personaje" que abre el dialogo de fichero. Medido con 6
    // personajes reales en la lista: al fallar "Restaurar copia de seguridad", el error salia
    // dentro de ese reclamo, con la lista de personajes JUSTO ENCIMA - un mensaje de error
    // disfrazado de invitacion a empezar de cero. Son dos propiedades distintas porque son dos
    // cosas distintas; el XAML pinta cada una a su manera.
    private bool _mensajeEsErrorDeAccion;

    public string? ScanMessage => _mensajeEsErrorDeAccion ? null : Componer();

    public string? ActionErrorMessage => _mensajeEsErrorDeAccion ? Componer() : null;

    private string? Componer()
    {
        if (_scanMessageKey is null) return null;
        var loc = LocalizationService.Instance;
        return _scanMessageFolders is null
            ? loc.Format(_scanMessageKey, _scanMessageArgs)
            : loc.Format(_scanMessageKey, string.Join(loc["scan_folder_joiner"], _scanMessageFolders));
    }

    private void SetScanMessage(string clave, params object?[] args)
    {
        _scanMessageKey = clave;
        _scanMessageArgs = args;
        _scanMessageFolders = null;
        _mensajeEsErrorDeAccion = false;
        AvisarDeLosDosMensajes();
    }

    private void SetScanMessageCarpetas(string clave, IReadOnlyList<string> carpetas)
    {
        _scanMessageKey = clave;
        _scanMessageArgs = [];
        _scanMessageFolders = carpetas;
        _mensajeEsErrorDeAccion = false;
        AvisarDeLosDosMensajes();
    }

    // El resultado de una accion del menu contextual (abrir carpeta, duplicar, restaurar) -
    // nunca el reclamo de "empezar de cero", que es otra cosa.
    private void SetActionError(string clave, params object?[] args)
    {
        _scanMessageKey = clave;
        _scanMessageArgs = args;
        _scanMessageFolders = null;
        _mensajeEsErrorDeAccion = true;
        AvisarDeLosDosMensajes();
    }

    // Unica via real de borrarlos desde fuera - no hay setter publico a proposito: aceptar un
    // string ya resuelto es exactamente lo que reintroduciria el bug de arriba.
    public void ClearScanMessage()
    {
        if (_scanMessageKey is null) return;
        _scanMessageKey = null;
        _scanMessageArgs = [];
        _scanMessageFolders = null;
        _mensajeEsErrorDeAccion = false;
        AvisarDeLosDosMensajes();
    }

    // INI-10: solo el mensaje del ESCANEO. Un error de accion sigue en pie - no es del escaneo y
    // el escaneo no tiene por que saber si esa accion se hizo o no.
    private void LimpiarMensajeDeEscaneo()
    {
        if (_scanMessageKey is null || _mensajeEsErrorDeAccion) return;
        ClearScanMessage();
    }

    private void AvisarDeLosDosMensajes()
    {
        OnPropertyChanged(nameof(ScanMessage));
        OnPropertyChanged(nameof(ActionErrorMessage));
    }

    // I-a: ruta real del personaje cargado ahora mismo en MainViewModel (null si ninguno) -
    // MainViewModel.LoadFromPath la actualiza en su finally, tanto en exito como en fallo.
    private string? _currentPath;

    public event Action<string>? CharacterChosen;
    // Catalogo de rediseño visual T4 (21-sep-2026): mismo patron real que CharacterChosen, dos
    // eventos aparte porque cada uno aterriza en un sitio distinto real de la app - GuideRequested
    // carga el personaje Y salta a la pestaña Guia (sugerencia "Te toca: X"), WorldChosen carga el
    // mundo en Exploracion sin tocar el personaje activo ("Tu ultimo mundo").
    public event Action<string>? GuideRequested;
    public event Action<string>? WorldChosen;

    // Doll fiel al guardado (pedido explicito, 3-sep-2026) - ver EquipmentAppearanceResolver.
    private readonly EquipmentAppearanceResolver _equipmentAppearance;
    // H5-04 (quinta auditoria de Opus): copias de seguridad rotativas, ver BackupHistoryService.
    private readonly BackupHistoryService _backupHistory;

    // H5-07 (quinta auditoria de Opus): "session.json recuerda el ULTIMO personaje real - Inicio
    // ofrece 'Continuar con Nombre' como accion destacada... nunca carga automatica silenciosa".
    private string? _lastSessionPath;
    [ObservableProperty] private string? _lastSessionCharacterName;
    // null = sin aviso real; non-null = el fichero cambio por fuera desde la ultima sesion real
    // (LastCharacterModifiedUtc guardado no coincide con la fecha real de ahora mismo) - se
    // sigue pudiendo continuar, pero avisado, nunca en silencio.
    //
    // Oleada del 6-sep-2026 (bloque INI-02 del arnes) - mismo BUG REAL que ScanMessage de
    // arriba: el texto se resolvia UNA vez aqui y se quedaba en el idioma de ese instante. Se
    // guarda el HECHO (¿cambio por fuera?) y el texto se compone al leerlo. Ademas, el camino
    // de "el fichero ya no existe" salia por `return` sin apagar un aviso anterior, que asi se
    // quedaba colgado apuntando a un personaje que ya no se ofrece.
    private bool _lastSessionIsStale;

    public string? LastSessionStalenessWarning => _lastSessionIsStale ? LocalizationService.Instance["home_stale_warning"] : null;

    // Catalogo de rediseño visual T4 (20-sep-2026, "Inicio como escritorio de partida - tarjeta
    // hero"): el personaje de la ultima sesion YA esta cargado entero en Characters (viene del
    // mismo escaneo real de RefreshAsync) - reusar esa misma instancia (Preview, HealthMax,
    // PlayTimeText) en vez de volver a leer el .plr del disco por separado. Null mientras el
    // escaneo no ha terminado todavia o el personaje de la ultima sesion ya no esta en la lista
    // (carpeta movida/borrado) - la tarjeta hero simplemente no se enseña en ese caso, nunca un
    // dato a medias.
    public CharacterListEntryViewModel? LastSessionCharacterEntry => _lastSessionPath == null
        ? null
        : Characters.FirstOrDefault(c => string.Equals(c.FilePath, _lastSessionPath, StringComparison.OrdinalIgnoreCase));

    // Catalogo de rediseño visual T4, segundo intento real (21-sep-2026 - la ronda del 20-sep
    // dejo esto como LIMITE documentado: "GuideViewModel solo evalua el personaje YA cargado en
    // el editor, no existe infraestructura para evaluar la Guia de un personaje que no esta
    // abierto"). Investigado a fondo: esa infraestructura SI existe de verdad, solo que nunca se
    // reutilizo aqui - GuideEvaluationEngine/GuideEvaluator (Terrakeep.Core/Guia) son PURAMENTE
    // funcionales, reciben un GuideContext (Character/MergedContainers/World/HasCalamity) sin
    // ninguna dependencia del personaje ACTIVO del editor. Lo unico que faltaba de verdad era
    // construir ESE contexto para un .plr del disco sin pasar por MainViewModel.
    //
    // CharacterFileService PROPIO (mismo motivo real ya documentado en CompareViewModel.cs:
    // Load() muta EsPersonajeTModLoader en la instancia - aislarlo evita que leer el .plr de la
    // tarjeta hero corrompa la tabla de "mejor prefijo" del editor principal) + catalogo/textos
    // de la Guia cargados una sola vez (mismos ficheros reales que ya usa GuideViewModel).
    private readonly CharacterFileService _guideDataService = new();
    private GuideCatalog? _guideCatalogo;
    private GuideEvaluator? _guideEvaluador;

    [ObservableProperty] private string? _lastSessionGuideStage;

    // Catalogo de rediseño visual T4 (21-sep-2026, sugerencia dinamica "Te toca: X" real - la
    // 3ª de las 3 sugerencias contextuales del catalogo, junto a Builds/Exploracion ya
    // existentes): mismo calculo real que LastSessionGuideStage, pero el TITULO del PASO
    // concreto (no del tramo) - "Guia.Paso." + clave + ".Titulo", el mismo texto real que ya
    // muestra GuidePasoViewModel.Titulo en la propia pestaña Guia.
    [ObservableProperty] private string? _lastSessionGuideObjectiveTitle;

    // Titulo YA formateado de la tarjeta ("Te toca: Plantera") - la propia clave de idioma trae
    // el "{0}" a rellenar (LocalizationService.Format, mismo patron real ya usado por
    // ExplorationViewModel.BestiarySummaryText), nunca concatenado a mano en el XAML (asi el
    // orden/las palabras alrededor del nombre los decide el diccionario de idioma, no C#).
    public string GuideObjectiveCardTitle => LastSessionGuideObjectiveTitle != null
        ? LocalizationService.Instance.Format("home_card_guide_objective_title", LastSessionGuideObjectiveTitle)
        : "";

    partial void OnLastSessionGuideObjectiveTitleChanged(string? value) => OnPropertyChanged(nameof(GuideObjectiveCardTitle));

    // Sin mundo real asociado a "el ultimo personaje" (Inicio no rastrea que .wld usaba cada
    // personaje) - World=null en el contexto es HONESTO, no un dato a medias: los requisitos que
    // de verdad necesiten un mundo (NpcsDelPueblo, Zona) caen a NoEvaluable con su motivo real
    // (MotivoSinPartidaEnMarcha), igual que ya le pasa a cualquier personaje sin mundo cargado en
    // la propia pestaña Guia - nunca se inventa un tramo a partir de un dato que no existe.
    //
    // Mismo criterio real de "objetivo actual" que GuideViewModel.Refresh(): el primer PASO sin
    // completar del primer TRAMO obligatorio (no opcional) sin completar, implementado,
    // recorriendo por Orden - devuelve el Nombre del tramo Y el Titulo de ese paso concreto de
    // un solo paso por el catalogo (nunca dos evaluaciones separadas que puedan desincronizarse).
    private (string? tramo, string? paso) ComputeGuideObjective(string plrPath)
    {
        try
        {
            string assetsGuia = Path.Combine(AppContext.BaseDirectory, "Assets", "guia");
            _guideCatalogo ??= GuideCatalog.LoadFromFile(Path.Combine(assetsGuia, "guia_progresion.json"), _guideDataService.CalamityCatalog);
            var textos = GuideTextCatalog.LoadFromFiles(Path.Combine(assetsGuia, "textos.es.json"), Path.Combine(assetsGuia, "textos.en.json"));
            _guideEvaluador ??= new GuideEvaluator(_guideDataService.VanillaCatalog, _guideDataService.NpcNames, _guideDataService.CalamityCatalog, _guideDataService.VanillaStats, _guideDataService.PrefixEffects, _guideDataService.PrefixRules);

            var loaded = _guideDataService.Load(plrPath);
            bool hasCalamity = loaded.TplrPath != null; // mismo criterio real que MainViewModel.HasCalamityData
            var contexto = new GuideContext { Character = loaded.Character, MergedContainers = loaded.MergedContainers, World = null, HasCalamity = hasCalamity };
            string idioma = LocalizationService.Instance.Language;

            foreach (var tramo in _guideCatalogo.Tramos)
            {
                if (tramo.Ambito == AmbitoGuia.Calamity && !hasCalamity) continue;
                if (!tramo.Implementado || tramo.Opcional) continue;
                foreach (var paso in tramo.Pasos)
                {
                    if (!_guideEvaluador.PasoCompletado(paso, contexto))
                        return (textos.Text("Guia.Tramo." + tramo.Clave + ".Nombre", idioma), textos.Text("Guia.Paso." + paso.Clave + ".Titulo", idioma));
                }
            }
            return (null, null); // Guia entera completada - sin objetivo pendiente real, nunca un texto inventado
        }
        catch (Exception)
        {
            // Un .plr/.tplr corrupto, o los ficheros de la Guia sin encontrar - la tarjeta hero
            // simplemente no enseña el 3er KPI/la sugerencia "Te toca" (los otros 2 KPI reales,
            // HealthMax/PlayTimeText, no dependen de esto y siguen intactos), nunca tumba Inicio.
            return (null, null);
        }
    }

    public void SetLastSession(TerrakeepSession session)
    {
        _lastSessionPath = session.LastCharacterPath;
        if (_lastSessionPath == null || !File.Exists(_lastSessionPath))
        {
            LastSessionCharacterName = null;
            LastSessionGuideStage = null;
            LastSessionGuideObjectiveTitle = null;
            MarcarSesionCambiadaPorFuera(false);
            OnPropertyChanged(nameof(LastSessionCharacterEntry));
        }
        else
        {
            LastSessionCharacterName = session.LastCharacterName ?? Path.GetFileNameWithoutExtension(_lastSessionPath);
            (LastSessionGuideStage, LastSessionGuideObjectiveTitle) = ComputeGuideObjective(_lastSessionPath);
            var modificadoReal = File.GetLastWriteTimeUtc(_lastSessionPath);
            MarcarSesionCambiadaPorFuera(session.LastCharacterModifiedUtc.HasValue && modificadoReal != session.LastCharacterModifiedUtc.Value);
            OnPropertyChanged(nameof(LastSessionCharacterEntry));
        }

        // Catalogo de rediseño visual T4 (21-sep-2026, "Tu ultimo mundo"): mismo mecanismo real
        // que el personaje - null/fichero borrado = sugerencia simplemente no se enseña.
        _lastWorldPath = session.LastWorldPath != null && File.Exists(session.LastWorldPath) ? session.LastWorldPath : null;
        LastWorldName = _lastWorldPath != null ? (session.LastWorldName ?? Path.GetFileNameWithoutExtension(_lastWorldPath)) : null;
    }

    private void MarcarSesionCambiadaPorFuera(bool valor)
    {
        if (_lastSessionIsStale == valor) return;
        _lastSessionIsStale = valor;
        OnPropertyChanged(nameof(LastSessionStalenessWarning));
    }

    // El texto de estas dos propiedades vive en el diccionario de idioma, no en un campo ya
    // resuelto - hay que volver a preguntarlo cuando el usuario cambia de idioma en vivo. Evento
    // DEBIL a proposito (mismo motivo real que LocalizedContentViewModel: LocalizationService es
    // un singleton que vive lo que la aplicacion, y `dotnet test` construye cientos de
    // MainViewModel); el handler es un metodo de instancia real, NUNCA una lambda - con una
    // lambda el objetivo del delegate es el cierre generado, que no referencia nadie mas y el
    // recolector puede llevarse en cualquier momento, dejando la suscripcion muerta en silencio.
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ScanMessage));
        OnPropertyChanged(nameof(LastSessionStalenessWarning));
        OnPropertyChanged(nameof(GuideObjectiveCardTitle));
    }

    [RelayCommand]
    private void Continue()
    {
        if (_lastSessionPath != null) CharacterChosen?.Invoke(_lastSessionPath);
    }

    // Catalogo de rediseño visual T4 (21-sep-2026), sugerencia dinamica real "Te toca: X": carga
    // el personaje de la ultima sesion (si no es ya el activo) y aterriza en la pestaña Guia -
    // mismo objetivo que LastSessionGuideObjectiveTitle ya muestra, ahora tambien accionable.
    [RelayCommand]
    private void ContinueToGuide()
    {
        if (_lastSessionPath != null) GuideRequested?.Invoke(_lastSessionPath);
    }

    // Catalogo de rediseño visual T4 (21-sep-2026), sugerencia real "Tu ultimo mundo": mismo
    // patron que ContinueCommand, pero para Exploracion - null si nunca se cargo un mundo real
    // en esta maquina o el fichero ya no existe (ver SetLastSession).
    private string? _lastWorldPath;
    [ObservableProperty] private string? _lastWorldName;

    [RelayCommand]
    private void ContinueWorld()
    {
        if (_lastWorldPath != null) WorldChosen?.Invoke(_lastWorldPath);
    }

    public HomeViewModel(EquipmentAppearanceResolver equipmentAppearance, BackupHistoryService backupHistory)
    {
        _equipmentAppearance = equipmentAppearance;
        _backupHistory = backupHistory;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");

        // Fire-and-forget deliberado: el constructor no puede ser async, y no hay nada
        // real que esperar aqui todavia (el arranque de MainWindow sigue su curso normal -
        // Characters simplemente se rellena un instante despues, IsScanning ahora SI tiene
        // un spinner real que lo refleja mientras tanto, ver MainWindow.xaml).
        _ = RefreshAsync();
    }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". Se llama tanto al cargar/cambiar de
    // personaje como tras Refresh() (la lista se reconstruye entera, IsCurrent no sobrevive).
    public void UpdateCurrentPath(string? path)
    {
        _currentPath = path;
        foreach (var entry in Characters)
            entry.IsCurrent = string.Equals(entry.FilePath, path, StringComparison.OrdinalIgnoreCase);
    }

    // H5-07 (quinta auditoria de Opus): bug real encontrado y arreglado verificando esta misma
    // pasada (visto en el propio arnes UIA: "10 personaje(s) encontrado(s)", cada uno duplicado)
    // - MainWindow.xaml.cs ahora relanza este mismo escaneo tras aplicar las carpetas
    // adicionales de Ajustes (LoadFromDisk), justo encima del escaneo AUTOMATICO que este mismo
    // constructor ya dispara (fire-and-forget) - dos vueltas reales de RefreshAsync en marcha a
    // la vez, cada una AÑADIENDO a Characters en vez de que la segunda sustituya a la primera.
    // Contador de generacion real: solo la vuelta MAS RECIENTE aplica su resultado - una vuelta
    // vieja que termina tarde se descarta en silencio en vez de pisar (o duplicar sobre) lo que
    // ya haya puesto una vuelta mas nueva.
    private int _scanGeneration;

    // El nombre real real de la carpeta escaneada solo hace falta para el mensaje "Ningun
    // personaje encontrado en..." - se calcula en el hilo de UI (barato, una sola llamada a
    // Environment.GetFolderPath) para poder mostrarlo aunque el escaneo en si falle.
    [RelayCommand]
    private async Task RefreshAsync()
    {
        int myGeneration = ++_scanGeneration;
        Characters.Clear();
        IsScanning = true;
        try
        {
            // Pedido explicito del usuario: personajes VANILLA (sin ningun mod, carpeta real
            // "Documents\My Games\Terraria\Players" sin el segmento "tModLoader") tambien
            // cuentan, no solo los de tModLoader - GetAllPlayersDirectories ya filtra a las que
            // existen de verdad (0, 1 o las 2), nunca cae a "Documentos entero".
            var dirs = CharacterFileService.GetAllPlayersDirectories();
            var scanned = await Task.Run(() => ScanCharacters(dirs, _equipmentAppearance));
            if (myGeneration != _scanGeneration) return; // una vuelta MAS NUEVA ya esta en marcha - esta es obsoleta
            foreach (var entry in scanned) Characters.Add(entry);
            OnPropertyChanged(nameof(LastSessionCharacterEntry)); // T4: la tarjeta hero depende de esta lista
            // INI-10 (misma oleada) - BUG REAL, encontrado al escribir el test de INI-08: un
            // escaneo que termina NO puede llevarse por delante el resultado de una accion que el
            // usuario acaba de pedir. El escaneo corre en segundo plano (T-G) y tarda lo que tarde
            // el disco: si mientras tanto el usuario prueba "Restaurar copia de seguridad" y falla,
            // el aviso aparecia y desaparecia solo unos milisegundos despues, cuando el escaneo de
            // fondo llegaba aqui y lo borraba. Reproducido de verdad: el test veia
            // ActionErrorMessage en null aunque el error se acababa de poner. Un mensaje que se
            // borra solo es peor que ninguno - el usuario no llega a leerlo y se queda sin saber
            // que su accion no se hizo. El escaneo manda sobre SU propio mensaje y solo sobre ese;
            // el error de una accion lo apaga la accion siguiente, que si sabe si tuvo exito.
            if (Characters.Count > 0) LimpiarMensajeDeEscaneo();
            else if (dirs.Count == 0) SetScanMessage("scan_no_players_folder");
            else SetScanMessageCarpetas("scan_no_players_in", dirs);
            UpdateCurrentPath(_currentPath); // la lista es nueva de cero, IsCurrent hay que recalcularlo
        }
        finally
        {
            // Solo la vuelta MAS RECIENTE apaga el indicador - si una vuelta vieja termina
            // tarde (ej. I/O lento) mientras una mas nueva sigue en marcha, no debe fingir que
            // el escaneo real ya acabo.
            if (myGeneration == _scanGeneration) IsScanning = false;
        }
    }

    // Todo el trabajo real de disco (enumerar + leer + descifrar cada .plr) - se ejecuta en un
    // hilo de fondo via Task.Run (RefreshAsync de arriba), nunca toca ninguna ObservableCollection
    // directamente (serian modificaciones desde fuera del hilo de UI).
    private static List<CharacterListEntryViewModel> ScanCharacters(IEnumerable<string> dirs, EquipmentAppearanceResolver equipmentAppearance)
    {
        var result = new List<CharacterListEntryViewModel>();
        // Orden real GLOBAL por fecha (no por carpeta primero) - un personaje vanilla reciente
        // debe aparecer antes que uno de tModLoader mas antiguo, no al reves solo por venir de
        // una carpeta distinta.
        var plrFiles = dirs.SelectMany(dir => Directory.GetFiles(dir, "*.plr"));
        foreach (string path in plrFiles.OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var character = PlrFile.Read(File.ReadAllBytes(path));
                // Encargo del usuario 4-sep-2026: el hecho PRINCIPAL es "este personaje es de
                // tModLoader" (tiene un .tplr hermano), no "es de Calamity" - la variable de
                // antes se llamaba isCalamity pero comprobaba exactamente esto, asi que
                // CUALQUIER mod (o un .tplr huerfano de una partida vieja) encendia una insignia
                // roja que decia "Calamity". Ver ESPEC-sprites-botones-badges.md#C.1.
                string tplrPath = Path.ChangeExtension(path, ".tplr");
                bool esTModLoader = File.Exists(tplrPath);
                // Coste real medido en la carpeta real de este usuario: 2,53 ms para los 6
                // personajes juntos, incluido un .tplr de 183 KB crudos con 2709 entradas de
                // research - y corre dentro del Task.Run que este escaneo ya usa, no en el hilo
                // de UI. No hay nada que optimizar aqui.
                var tplr = esTModLoader ? TplrProbe.TryRead(tplrPath) : null;
                result.Add(new CharacterListEntryViewModel(path, character, esTModLoader, tplr, File.GetLastWriteTimeUtc(path), equipmentAppearance));
            }
            catch (Exception)
            {
                // Un .plr ajeno/corrupto no debe tumbar el listado de los demas - se omite
                // en silencio, igual que ya hace la Libreria con ids sin catalogar.
            }
        }
        return result;
    }

    [RelayCommand]
    private void Open(CharacterListEntryViewModel entry) => CharacterChosen?.Invoke(entry.FilePath);

    // INI-08: ¿este fichero es un .plr que la app pueda cargar de verdad? Se usa el MISMO lector
    // real (PlrFile.Read, con su descifrado y su NBT) que usa la carga normal - una comprobacion
    // mas floja (que exista, que ocupe algo) no distingue un fichero truncado de uno bueno, y es
    // justo lo que hay que distinguir antes de machacar el original con el. Los .plr son
    // pequeños (unos KB), asi que leerlo entero no cuesta nada perceptible.
    private static bool EsUnPlrLegible(string ruta)
    {
        try
        {
            PlrFile.Read(File.ReadAllBytes(ruta));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // I-b (segunda auditoria de Opus, Fable): "Sin ninguna accion secundaria en la tarjeta -
    // faltan las 3 obvias y baratas: abrir carpeta, duplicar personaje, restaurar copia de
    // seguridad". Menu contextual real en la tarjeta (ver MainWindow.xaml).
    [RelayCommand]
    private void OpenFolder(CharacterListEntryViewModel entry)
    {
        try
        {
            // /select, resalta el fichero real en el Explorador en vez de solo abrir la carpeta
            // a ciegas - mismo gesto real que "Mostrar en carpeta" de cualquier otra app.
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{entry.FilePath}\"");
        }
        catch (Exception ex)
        {
            SetActionError("error_open_folder", ex.Message);
        }
    }

    [RelayCommand]
    private void Duplicate(CharacterListEntryViewModel entry)
    {
        try
        {
            // "la red de seguridad real para experimentar" - copia de fichero pura (mismo
            // nombre interno del personaje, solo cambia el archivo) para poder tocar la copia
            // sin arriesgar el original. Numerado si "(copia)" ya existe, nunca sobrescribe.
            string dir = Path.GetDirectoryName(entry.FilePath)!;
            string baseName = Path.GetFileNameWithoutExtension(entry.FilePath);
            // Ronda de idioma del 6-sep-2026: el sufijo iba interpolado a pelo aqui aunque la
            // clave "home_copy_suffix" YA existia en los dos diccionarios desde la ronda anterior
            // (creada y nunca enchufada) - duplicar un personaje con la app en ingles creaba un
            // "Fulano (copia).plr". "home_copy_suffix_first" es nueva: el primer duplicado no
            // lleva numero, y ese caso no tenia clave ninguna.
            string newPath = Path.Combine(dir, LocalizationService.Instance.Format("home_copy_suffix_first", baseName));
            for (int n = 2; File.Exists(newPath); n++)
                newPath = Path.Combine(dir, LocalizationService.Instance.Format("home_copy_suffix", baseName, n));

            File.Copy(entry.FilePath, newPath);
            string tplrSrc = Path.ChangeExtension(entry.FilePath, ".tplr");
            if (File.Exists(tplrSrc)) File.Copy(tplrSrc, Path.ChangeExtension(newPath, ".tplr"));
            // INI-10: esta accion SI tuvo exito, asi que apaga el aviso de la anterior (el
            // escaneo ya no lo hace por su cuenta - ver el comentario de LimpiarMensajeDeEscaneo).
            ClearScanMessage();
            _ = RefreshAsync(); // T-G: mismo criterio real que el constructor, fire-and-forget
        }
        catch (Exception ex)
        {
            SetActionError("error_duplicating", ex.Message);
        }
    }

    [RelayCommand]
    private void RestoreBackup(CharacterListEntryViewModel entry)
    {
        try
        {
            // Mismo .bak real que T-C ya deja (CharacterFileService.WriteAtomic) - esto es el
            // mismo mecanismo de "Deshacer ultimo guardado" de la cabecera, pero operando sobre
            // CUALQUIER personaje de la lista, este cargado ahora mismo o no.
            string plrBak = entry.FilePath + ".bak";
            if (!File.Exists(plrBak))
            {
                SetActionError("error_no_backup_to_restore", entry.Name);
                return;
            }
            // INI-08 (misma oleada) - BUG REAL DE PERDIDA DE DATOS: esto copiaba el .bak encima
            // del .plr sin mirar si el .bak se puede leer siquiera. Reproducido con numeros: un
            // .plr bueno de 3680 bytes + un .bak de 3 bytes ilegible -> el personaje BUENO queda
            // destruido, desaparece de Inicio (el escaneo omite en silencio lo que no puede leer,
            // por diseño) y no se avisa de nada. Sin vuelta atras: el .bak era la unica copia y
            // acaba de machacar el original. Un .bak truncado no es rebuscado - lo deja cualquier
            // guardado interrumpido (disco lleno, apagon, antivirus).
            //
            // Se lee ANTES de tocar nada, con el mismo lector real que usa la app para cargar.
            if (!EsUnPlrLegible(plrBak))
            {
                SetActionError("error_backup_unreadable", entry.Name);
                return;
            }
            File.Copy(plrBak, entry.FilePath, overwrite: true);
            string tplrPath = Path.ChangeExtension(entry.FilePath, ".tplr");
            string tplrBak = tplrPath + ".bak";
            if (File.Exists(tplrBak))
            {
                File.Copy(tplrBak, tplrPath, overwrite: true);
            }
            else if (File.Exists(tplrPath))
            {
                // H3-02 (tercera auditoria, Fable): mismo motivo real que MainViewModel.
                // UndoLastSave - sin .tplr.bak, este .tplr nacio en el mismo guardado que se
                // esta restaurando por encima, no existia antes. Dejarlo resucitaria su
                // contenido de Calamity al recargar.
                File.Delete(tplrPath);
            }
            // INI-10: esta accion SI tuvo exito, asi que apaga el aviso de la anterior (el
            // escaneo ya no lo hace por su cuenta - ver el comentario de LimpiarMensajeDeEscaneo).
            ClearScanMessage();
            _ = RefreshAsync(); // T-G: mismo criterio real que el constructor, fire-and-forget

            // H3-04 (tercera auditoria, Fable): "Restaurar copia de seguridad" restauraba los
            // ficheros en disco pero no avisaba a MainViewModel - si el personaje restaurado
            // era el que estaba cargado, el editor seguia mostrando el estado antiguo en
            // memoria, y un Guardar posterior lo machacaba en silencio. Mismo evento real ya
            // usado por "Cargar" (CharacterChosen) - MainViewModel ya lo conecta con su propio
            // aviso real de "cambios sin guardar" (ConfirmDiscardChanges) antes de recargar, asi
            // que unas ediciones en memoria sin guardar tambien avisan aqui, no solo se pisan.
            if (_currentPath != null && string.Equals(_currentPath, entry.FilePath, StringComparison.OrdinalIgnoreCase))
                CharacterChosen?.Invoke(entry.FilePath);
        }
        catch (Exception ex)
        {
            SetActionError("error_restoring_backup", ex.Message);
        }
    }

    // H5-04 (quinta auditoria de Opus): "un panel 'Historial de guardados'... con fecha, tamaño
    // y un boton por punto - para restaurar cualquiera, no solo el ultimo".
    //
    // BK (13-sep-2026): esto era un SUBMENU que restauraba con un solo clic, sin ninguna
    // confirmacion, sobre una linea que solo decia fecha y tamaño. Ahora la tarjeta solo ABRE el
    // panel real (BackupHistoryViewModel) - elegir y confirmar la version pasa a ocurrir ahi,
    // donde cada punto se lee de verdad (resumen del personaje) y sobrescribir el fichero pide
    // una confirmacion explicita. MainViewModel es quien conoce el panel; Inicio solo avisa.
    public event Action<string, string>? BackupHistoryRequested;

    [RelayCommand]
    private void ShowBackupHistory(CharacterListEntryViewModel entry) =>
        BackupHistoryRequested?.Invoke(entry.FilePath, entry.Name);

    // Sigue existiendo (el panel la usa por dentro via BackupHistoryService) - se mantiene aqui
    // porque varias pruebas reales la usan como via de consulta sin montar el panel entero.
    public IReadOnlyList<BackupEntry> ListBackupPoints(CharacterListEntryViewModel entry) =>
        _backupHistory.ListBackups(entry.FilePath);
}
