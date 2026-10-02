using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Controls;
using Terrakeep.App.Services;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Guia;
using Terrakeep.Core.Guia.V2;
using Terrakeep.Core.Model;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels;

/// <summary>Secciones de la pestaña Guia (las del menu lateral de la guia HTML del usuario).</summary>
public enum SeccionGuiaV2
{
    MiGuia = 0,
    Ruta = 1,
    Equipo = 2,
    Manual = 3,
    Biomas = 4,
    Perdido = 5,
    Raro = 6,
    Buscar = 7,
}

// Guia v2 (F2, 02-oct-2026, encargo literal del usuario: "Haz lo de la guía calamity y vanilla con
// todo lo ya mencionado tanto para terrakeep como para el mod y que estén así de curradas"): la
// pestaña Guia de Terrakeep sobre el modelo compartido de Terrakeep.Core/Guia/V2 (contenido
// incrustado + GuiaV2Evaluador), evaluada contra el personaje y el mundo REALES ya cargados en la
// app. Diseño y contrato: docs/guia-v2-diseno.md §10 (F2).
//
// Que hace esta clase (y que NO):
//   - Monta el GuideContext completo (incluidas las mejoras del jugador de Calamity del .tplr y el
//     estado de mundo de MiscWorldStateSystem del .twld) y pide a GuideEvaluator.CrearProveedor la
//     fuente de estado v2. Toda la logica de "cumplido/no cumplido" vive en Core; aqui solo se
//     presenta.
//   - Elige la guia (vanilla/Calamity) segun la partida: Calamity si el personaje tiene .tplr o el
//     mundo tiene .twld; si no, vanilla (si esta incrustada; F1 la esta generando en paralelo - sin
//     ella se usa Calamity y se avisa, nunca se finge una guia vanilla que no existe).
//   - Persiste el progreso manual (casillas, paradas hechas/aplazadas, clase elegida) en la carpeta
//     de estado de la app, un archivo por personaje; NUNCA dentro de la partida real.
//   - Calcula el marcador de la siguiente parada sobre el mundo real (GuiaV2Ubicaciones +
//     MundoGuiaEscritorio) para el mapa de Exploracion; si la ubicacion es aproximada, lo dice.
public sealed partial class GuiaV2ViewModel : ObservableObject, IContextoTextoGuia
{
    private readonly CharacterFileService _servicio;
    private readonly Func<LoadedCharacter?> _character;
    private readonly Func<WldWorld?> _world;
    private readonly Func<bool> _hasCalamity;
    private readonly Func<string?> _worldPath;
    private readonly GuiaV2Recursos _recursos;
    private readonly GuideEvaluator _evaluadorV1;
    private readonly GuideTextCatalog _textos;

    private GuiaV2Doc _doc = null!;
    private GuiaV2Evaluador _evaluador = null!;
    private IGuideStateProviderV2? _proveedor;
    private ResumenGuiaV2? _resumen;
    private GuiaV2ProgresoManual _progreso = new();
    private string _clavePersonaje = "";
    private readonly Dictionary<string, ParadaV2ViewModel> _paradasPorId = [];
    private readonly Stack<string> _historialFicha = new();

    /// <summary>Carpeta donde se guarda el progreso manual. Por defecto, dentro de la carpeta de
    /// estado de la app (redirigida por el arnes en modo diagnostico); las pruebas pueden fijarla.</summary>
    public string CarpetaProgreso { get; set; } = CarpetaEstadoApp.Ruta("GuiaProgreso");

    /// <summary>"Ver en el mapa": el MainViewModel cambia a Exploracion y centra en (x, y) casillas.</summary>
    public event Action<int, int>? VerEnMapaSolicitado;
    /// <summary>"Llevar a la Libreria": el MainViewModel abre la Libreria con esta busqueda.</summary>
    public event Action<string>? LibreriaSolicitada;

    public GuiaV2ViewModel(CharacterFileService servicio, Func<LoadedCharacter?> character, Func<WldWorld?> world,
        Func<bool> hasCalamity, Func<string?> worldPath)
    {
        _servicio = servicio;
        _character = character;
        _world = world;
        _hasCalamity = hasCalamity;
        _worldPath = worldPath;
        _recursos = new GuiaV2Recursos(servicio);
        _evaluadorV1 = new GuideEvaluator(servicio.VanillaCatalog, servicio.NpcNames, servicio.CalamityCatalog,
            servicio.VanillaStats, servicio.PrefixEffects, servicio.PrefixRules);
        string assetsGuia = Path.Combine(AppContext.BaseDirectory, "Assets", "guia");
        _textos = GuideTextCatalog.LoadFromFiles(Path.Combine(assetsGuia, "textos.es.json"), Path.Combine(assetsGuia, "textos.en.json"));
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, (_, _) => Refresh(), "Item[]");
        Refresh();
    }

    // ---- Textos de la interfaz -------------------------------------------------------------------

    internal string L(string clave) => LocalizationService.Instance[clave];
    internal string F(string clave, params object?[] args) => LocalizationService.Instance.Format(clave, args);
    internal string TextoHoja(ResultadoRequisitoGuia hoja) =>
        _textos.Format(hoja.TextoClave, LocalizationService.Instance.Language, hoja.TextoArgs);

    // ---- Estado general --------------------------------------------------------------------------

    [ObservableProperty] private SeccionGuiaV2 _seccion = SeccionGuiaV2.MiGuia;
    [ObservableProperty] private string _guiaId = "";
    [ObservableProperty] private bool _esCalamity;
    [ObservableProperty] private bool _vanillaPendiente;
    [ObservableProperty] private bool _hayPersonaje;
    [ObservableProperty] private bool _hayMundo;
    [ObservableProperty] private bool _hayDatos;
    [ObservableProperty] private string _modoTexto = "";
    [ObservableProperty] private string _personajeTexto = "";
    [ObservableProperty] private int? _vidaActual;
    [ObservableProperty] private int _paradasHechas;
    [ObservableProperty] private int _paradasTotal;
    [ObservableProperty] private int _tareasHechas;
    [ObservableProperty] private int _tareasTotal;
    [ObservableProperty] private int _tareasAutomaticas;
    [ObservableProperty] private ParadaV2ViewModel? _siguiente;
    [ObservableProperty] private ParadaV2ViewModel? _paradaSeleccionada;
    [ObservableProperty] private CapituloV2ViewModel? _capituloSeleccionado;
    [ObservableProperty] private ArticuloV2ViewModel? _articuloSeleccionado;
    [ObservableProperty] private FichaObjetoViewModel? _fichaObjeto;
    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private ClaseGuia? _claseActual;
    [ObservableProperty] private ClaseGuia? _claseDetectada;
    [ObservableProperty] private bool _claseElegidaAMano;
    [ObservableProperty] private EtapaEscaleraViewModel? _etapaActual;

    public ObservableCollection<CapituloV2ViewModel> Capitulos { get; } = [];
    public ObservableCollection<ParadaV2ViewModel> Paradas { get; } = [];
    public ObservableCollection<OpcionClaseViewModel> Clases { get; } = [];
    public ObservableCollection<EtapaEscaleraViewModel> Etapas { get; } = [];
    public ObservableCollection<ArticuloV2ViewModel> Articulos { get; } = [];
    public ObservableCollection<ZonaV2ViewModel> Zonas { get; } = [];
    public ObservableCollection<FichaV2ViewModel> Problemas { get; } = [];
    public ObservableCollection<FichaV2ViewModel> Hallazgos { get; } = [];
    public ObservableCollection<AvisoGuiaViewModel> AvisosGenerales { get; } = [];
    public ObservableCollection<ResultadoBusquedaV2ViewModel> Resultados { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> Leyenda { get; } = [];

    public GuiaV2Doc Documento => _doc;
    public string Titulo => _doc.Titulo;
    public string Subtitulo => _doc.Subtitulo;
    public string AmbitoTexto => L(EsCalamity ? "guia2_ambito_calamity" : "guia2_ambito_vanilla");
    public string ReferenciaTexto => _doc.Referencia.Calamity is string c
        ? F("guia2_referencia_calamity", _doc.Referencia.Terraria, c, _doc.Referencia.FechaInvestigacion)
        : F("guia2_referencia_vanilla", _doc.Referencia.Terraria, _doc.Referencia.FechaInvestigacion);
    public double FraccionParadas => ParadasTotal == 0 ? 0 : ParadasHechas / (double)ParadasTotal;
    public string ProgresoParadasTexto => F("guia2_progreso_paradas", ParadasHechas, ParadasTotal);
    public string ProgresoTareasTexto => F("guia2_progreso_tareas", TareasHechas, TareasTotal);
    public string ClaseTexto => ClaseActual is ClaseGuia c ? L("guia2_clase_" + GuiaV2Evaluador.ClaveClase(c)) : L("guia2_clase_sin_elegir");
    public string ClaseDetectadaTexto => ClaseDetectada is ClaseGuia c
        ? F(ClaseElegidaAMano ? "guia2_clase_detectada_otra" : "guia2_clase_detectada", L("guia2_clase_" + GuiaV2Evaluador.ClaveClase(c)))
        : L(HayPersonaje ? "guia2_clase_no_detectada" : "guia2_clase_sin_personaje");
    public bool RutaTerminada => Siguiente == null && ParadasTotal > 0;
    public int ZonasExplicadas => _doc.Zonas.Count;
    public int Articulos_Count => _doc.Articulos.Count;

    public bool IsMiGuia => Seccion == SeccionGuiaV2.MiGuia;
    public bool IsRuta => Seccion == SeccionGuiaV2.Ruta;
    public bool IsEquipo => Seccion == SeccionGuiaV2.Equipo;
    public bool IsManual => Seccion == SeccionGuiaV2.Manual;
    public bool IsBiomas => Seccion == SeccionGuiaV2.Biomas;
    public bool IsPerdido => Seccion == SeccionGuiaV2.Perdido;
    public bool IsRaro => Seccion == SeccionGuiaV2.Raro;
    public bool IsBuscar => Seccion == SeccionGuiaV2.Buscar;
    public bool HayFicha => FichaObjeto != null;

    partial void OnSeccionChanged(SeccionGuiaV2 value)
    {
        foreach (var n in new[] { nameof(IsMiGuia), nameof(IsRuta), nameof(IsEquipo), nameof(IsManual), nameof(IsBiomas), nameof(IsPerdido), nameof(IsRaro), nameof(IsBuscar) })
            OnPropertyChanged(n);
        if (value == SeccionGuiaV2.Ruta && ParadaSeleccionada == null) SeleccionarParada(Siguiente ?? Paradas.FirstOrDefault());
        if (value == SeccionGuiaV2.Manual && ArticuloSeleccionado == null) SeleccionarArticulo(ArticulosManual.FirstOrDefault());
        if (value == SeccionGuiaV2.Biomas) _ = ResolverZonasAsync();
        SeccionCambiada?.Invoke();
    }

    /// <summary>La vista lleva el scroll arriba al cambiar de seccion o de parada.</summary>
    public event Action? SeccionCambiada;

    partial void OnFichaObjetoChanged(FichaObjetoViewModel? value) => OnPropertyChanged(nameof(HayFicha));
    partial void OnParadaSeleccionadaChanged(ParadaV2ViewModel? value)
    {
        foreach (var p in Paradas) p.EsSeleccionada = p == value;
    }
    partial void OnCapituloSeleccionadoChanged(CapituloV2ViewModel? value)
    {
        foreach (var c in Capitulos) c.EsSeleccionado = c == value;
        OnPropertyChanged(nameof(ParadasDelCapitulo));
    }

    /// <summary>Paradas del capitulo elegido (lista de la izquierda en "Ruta paso a paso").</summary>
    public IReadOnlyList<ParadaV2ViewModel> ParadasDelCapitulo => CapituloSeleccionado?.Paradas ?? [];
    partial void OnParadasHechasChanged(int value) { OnPropertyChanged(nameof(FraccionParadas)); OnPropertyChanged(nameof(ProgresoParadasTexto)); }
    partial void OnParadasTotalChanged(int value) { OnPropertyChanged(nameof(FraccionParadas)); OnPropertyChanged(nameof(ProgresoParadasTexto)); }
    partial void OnTareasHechasChanged(int value) => OnPropertyChanged(nameof(ProgresoTareasTexto));
    partial void OnTareasTotalChanged(int value) => OnPropertyChanged(nameof(ProgresoTareasTexto));
    partial void OnTextoBusquedaChanged(string value)
    {
        Buscar(value);
        if (!string.IsNullOrWhiteSpace(value) && Seccion != SeccionGuiaV2.Buscar) Seccion = SeccionGuiaV2.Buscar;
    }

    /// <summary>Articulos del Manual (los que tienen pantalla propia - biomas, estoy perdido, he
    /// encontrado algo raro - se ven en su seccion, no repetidos aqui).</summary>
    public IReadOnlyList<ArticuloV2ViewModel> ArticulosManual => Articulos.Where(a => a.Id is not ("problemas" or "estructuras")).ToList();
    public ArticuloV2ViewModel? ArticuloMapa => Articulos.FirstOrDefault(a => a.Id == "mapa");
    public ArticuloV2ViewModel? ArticuloProblemas => Articulos.FirstOrDefault(a => a.Id == "problemas");
    public ArticuloV2ViewModel? ArticuloEstructuras => Articulos.FirstOrDefault(a => a.Id == "estructuras");

    // ---- Evaluacion ------------------------------------------------------------------------------

    /// <summary>Vuelve a leer el estado real (personaje + mundo cargados ahora) y a evaluar la guia
    /// entera. Se llama al entrar en la pestaña, al cargar personaje/mundo, al marcar una casilla y
    /// al cambiar de idioma. Mantiene la parada/articulo seleccionados por id.</summary>
    public void Refresh()
    {
        var loaded = _character();
        var world = _world();
        string? rutaWld = _worldPath();
        bool twld = !string.IsNullOrEmpty(rutaWld) && File.Exists(Path.ChangeExtension(rutaWld, ".twld"));
        bool calamityPartida = _hasCalamity() || twld;

        // 1. Que guia toca.
        var disponibles = GuiaV2Recursos.Disponibles;
        string id = calamityPartida || !disponibles.Contains("vanilla") ? "calamity" : "vanilla";
        if (!disponibles.Contains(id)) id = disponibles.FirstOrDefault() ?? "calamity";
        bool cambioDeGuia = id != GuiaId;
        _doc = GuiaV2Recursos.Documento(id) ?? throw new InvalidDataException("No hay ninguna guia v2 incrustada en Terrakeep.Core.");
        _evaluador = new GuiaV2Evaluador(_doc, _recursos.Resolutor);
        GuiaId = id;
        EsCalamity = id == "calamity";
        // Partida vanilla pero esta build aun no trae la guia vanilla (F1): se avisa, nunca se finge.
        VanillaPendiente = !calamityPartida && (loaded != null || world != null) && !disponibles.Contains("vanilla");

        // 2. Contexto real (docs/guia-v2-diseno.md §10, F2 punto 1).
        CalamityEstadoGuardado.EstadoMundo? estadoCalamity = world != null ? MundoGuiaEscritorio.LeerEstadoCalamity(rutaWld) : null;
        var contexto = new GuideContext
        {
            Character = loaded?.Character,
            MergedContainers = loaded?.MergedContainers,
            World = world,
            HasCalamity = _hasCalamity(),
            CalamityDownedFlags = world != null ? LeerBanderasCalamity(rutaWld) : null,
            CalamityPlayerBoosts = loaded != null ? CalamityEstadoGuardado.LeerMejorasJugador(loaded.TplrRoot) : null,
            CalamityWorldState = world != null ? (estadoCalamity?.Claves ?? new HashSet<string>()) : null,
        };
        _proveedor = _evaluadorV1.CrearProveedor(contexto);
        _puntosCalamity = estadoCalamity?.PuntosEnCasillas;

        HayPersonaje = loaded != null;
        HayMundo = world != null;
        HayDatos = loaded != null || world != null;
        VidaActual = loaded?.Character.HealthMax;
        PersonajeTexto = loaded != null ? loaded.Character.Name + (world != null ? " · " + world.Header.Title : "")
            : world != null ? world.Header.Title : "";

        // 3. Progreso manual del personaje (o de "sin personaje").
        string clave = loaded != null ? Path.GetFileNameWithoutExtension(loaded.PlrPath) : "_sin_personaje";
        if (cambioDeGuia || clave != _clavePersonaje || _progreso.Guia != id)
        {
            _progreso = CargarProgreso(id, clave);
            _clavePersonaje = clave;
        }

        // 4. Clase: la elegida por el jugador manda; si no, la detectada por su equipo.
        ClaseDetectada = DetectarClase(loaded);
        var elegida = GuiaV2Evaluador.ClaseDesdeClave(_progreso.Clase);
        if (elegida == ClaseGuia.Picaro && !EsCalamity) elegida = null;
        ClaseElegidaAMano = elegida != null;
        ClaseActual = elegida ?? ClaseDetectada ?? ClaseGuia.CuerpoACuerpo;

        // 5. Evaluar.
        _resumen = _evaluador.Evaluar(_proveedor, _progreso, ClaseActual);
        var modos = _proveedor.Modo?.Claves();
        ModoTexto = modos == null ? L("guia2_modo_sin_mundo") : string.Join(" · ", modos.Select(m => L("guia2_modo_" + m)));

        string? seleccionadaId = ParadaSeleccionada?.Id;
        string? articuloId = ArticuloSeleccionado?.Id;
        string? capituloId = CapituloSeleccionado?.Id;
        if (cambioDeGuia) { seleccionadaId = null; articuloId = null; capituloId = null; }

        _paradasPorId.Clear();
        Paradas.Clear();
        var titulosCapitulo = _doc.Capitulos.ToDictionary(c => c.Id, c => Plano(c.Titulo));
        foreach (var rp in _resumen.Paradas)
        {
            var vm = new ParadaV2ViewModel(this, rp, _resumen.Paradas.Count,
                titulosCapitulo.TryGetValue(rp.Parada.Capitulo, out var tc) ? tc : rp.Parada.Capitulo, rp == _resumen.Siguiente);
            Paradas.Add(vm);
            _paradasPorId[vm.Id] = vm;
        }
        Capitulos.Clear();
        int n = 1;
        foreach (var c in _doc.Capitulos)
            Capitulos.Add(new CapituloV2ViewModel(c.Id, Plano(c.Titulo), c.Resumen, Paradas.Where(p => p.Parada.Capitulo == c.Id).ToList(), n++));

        ParadasHechas = _resumen.ParadasCompletadas;
        ParadasTotal = _resumen.Paradas.Count;
        TareasHechas = _resumen.TareasHechas;
        TareasTotal = _resumen.TareasTotales;
        TareasAutomaticas = _resumen.Paradas.Sum(p => p.Tareas.Count(t => t.Automatica));
        Siguiente = _resumen.Siguiente != null ? _paradasPorId[_resumen.Siguiente.Parada.Id] : null;

        AvisosGenerales.Clear();
        foreach (var a in _resumen.AvisosGenerales) AvisosGenerales.Add(new AvisoGuiaViewModel(a.Texto));

        // Clases admitidas por esta guia.
        Clases.Clear();
        foreach (var claveClase in _doc.Clases)
            if (GuiaV2Evaluador.ClaseDesdeClave(claveClase) is ClaseGuia cg)
                Clases.Add(new OpcionClaseViewModel(this, cg, cg == ClaseActual, cg == ClaseDetectada));

        ConstruirEscalera();
        if (cambioDeGuia || Articulos.Count == 0) ConstruirManual();

        // Restaurar seleccion.
        ParadaSeleccionada = seleccionadaId != null && _paradasPorId.TryGetValue(seleccionadaId, out var ps) ? ps
            : Seccion == SeccionGuiaV2.Ruta ? Siguiente ?? Paradas.FirstOrDefault() : null;
        CapituloSeleccionado = Capitulos.FirstOrDefault(c => c.Id == (ParadaSeleccionada?.Parada.Capitulo ?? capituloId))
            ?? Capitulos.FirstOrDefault(c => c.Id == Siguiente?.Parada.Capitulo) ?? Capitulos.FirstOrDefault();
        if (articuloId != null) SeleccionarArticulo(Articulos.FirstOrDefault(a => a.Id == articuloId));
        if (!string.IsNullOrWhiteSpace(TextoBusqueda)) Buscar(TextoBusqueda);

        foreach (var nombre in new[] { nameof(Titulo), nameof(Subtitulo), nameof(AmbitoTexto), nameof(ReferenciaTexto), nameof(ClaseTexto),
                     nameof(ClaseDetectadaTexto), nameof(RutaTerminada), nameof(ZonasExplicadas), nameof(ArticulosManual), nameof(ArticuloMapa),
                     nameof(ArticuloProblemas), nameof(ArticuloEstructuras), nameof(Documento) })
            OnPropertyChanged(nombre);

        _ = ActualizarMarcadorAsync();
    }

    private static IReadOnlySet<string> LeerBanderasCalamity(string? rutaWld)
    {
        if (string.IsNullOrEmpty(rutaWld)) return new HashSet<string>();
        string twld = Path.ChangeExtension(rutaWld, ".twld");
        if (!File.Exists(twld)) return new HashSet<string>();
        try { return TwldReader.ReadCalamityDownedFlags(File.ReadAllBytes(twld)); }
        catch (Exception) { return new HashSet<string>(); }
    }

    private GuiaV2ProgresoManual CargarProgreso(string guia, string clave)
    {
        try { return ArchivoProgresoGuia.Cargar(CarpetaProgreso, guia, clave); }
        catch (Exception) { return new GuiaV2ProgresoManual { Guia = guia }; }
    }

    private void GuardarProgreso()
    {
        try
        {
            string ruta = Path.Combine(CarpetaProgreso, ArchivoProgresoGuia.NombreArchivo(_progreso.Guia, _clavePersonaje));
            if (!CarpetaEstadoApp.PermiteEscribir(ruta)) return;
            ArchivoProgresoGuia.Guardar(CarpetaProgreso, _clavePersonaje, _progreso);
        }
        catch (Exception)
        {
            // Best-effort como el resto del estado de la app: un disco lleno no rompe la guia.
        }
    }

    /// <summary>Progreso manual en memoria (solo lectura, para pruebas y el arnes).</summary>
    public GuiaV2ProgresoManual ProgresoManual => _progreso;

    // ---- Clase -----------------------------------------------------------------------------------

    /// <summary>Clase probable segun el arma con mas daño del inventario y el equipo puesto (daño
    /// base de los catalogos reales; Calamity por su damageType real, vanilla por la categoria de
    /// la Libreria y las reglas de prefijo). Es una PROPUESTA: el jugador puede cambiarla.</summary>
    internal ClaseGuia? DetectarClase(LoadedCharacter? loaded)
    {
        if (loaded == null) return null;
        var mejor = new Dictionary<ClaseGuia, int>();
        foreach (string contenedor in new[] { "inventory", "loadout0Items" })
        {
            if (!loaded.MergedContainers.TryGetValue(contenedor, out var items)) continue;
            foreach (var item in items)
            {
                if (item.IsEmpty) continue;
                var (clase, dano) = ClaseDeArma(item.Id, item.IsCalamity);
                if (clase is not ClaseGuia c || dano <= 0) continue;
                if (c == ClaseGuia.Picaro && !EsCalamity) continue;
                mejor[c] = Math.Max(mejor.GetValueOrDefault(c), dano);
            }
        }
        return mejor.Count == 0 ? null : mejor.OrderByDescending(kv => kv.Value).First().Key;
    }

    private (ClaseGuia?, int) ClaseDeArma(int id, bool esCalamity)
    {
        if (esCalamity || id >= CalamityIds.ItemIdBase)
        {
            var stats = _servicio.CalamityCatalog.BySyntheticId(id)?.Stats;
            if (stats?.Damage is not int d || string.IsNullOrEmpty(stats.DamageType)) return (null, 0);
            string t = stats.DamageType;
            ClaseGuia? c = t.Contains("Rogue") ? ClaseGuia.Picaro
                : t.Contains("Summon") ? ClaseGuia.Invocacion
                : t.Contains("Magic") ? ClaseGuia.Magia
                : t.Contains("Ranged") && !t.Contains("Melee") ? ClaseGuia.Distancia
                : t.Contains("Melee") ? ClaseGuia.CuerpoACuerpo
                : null;
            return (c, d);
        }
        int dano = _servicio.VanillaStats.Get(id)?.Damage ?? 0;
        if (dano <= 0) return (null, 0);
        string cat = _servicio.VanillaCategories.GetCategory(id);
        if (cat == "Armas/Invocacion") return (ClaseGuia.Invocacion, dano);
        if (cat == "Armas/Magia") return (ClaseGuia.Magia, dano);
        if (cat == "Armas/A distancia") return (ClaseGuia.Distancia, dano);
        var flags = _servicio.PrefixRules.VanillaCategories(id);
        if (cat == "Armas/Cuerpo a cuerpo" || (flags & PrefixCategory.Melee) != 0)
            // Herramientas (picos/hachas/martillos) cuentan como cuerpo a cuerpo en la categoria pero
            // casi nunca son el arma real: pesan la mitad para que un pico no decida la clase.
            return (ClaseGuia.CuerpoACuerpo, cat == "Herramientas" ? dano / 2 : dano);
        if ((flags & PrefixCategory.Ranged) != 0) return (ClaseGuia.Distancia, dano);
        if ((flags & PrefixCategory.Magic) != 0) return (ClaseGuia.Magia, dano);
        return (null, 0);
    }

    public void ElegirClase(ClaseGuia clase)
    {
        // Elegir la detectada (sin haber elegido otra antes) no cambia nada; elegirla de nuevo tras
        // otra vuelve a "automatica" para que siga a su equipo.
        _progreso.Clase = clase == ClaseDetectada ? "" : GuiaV2Evaluador.ClaveClase(clase);
        GuardarProgreso();
        Refresh();
    }

    partial void OnClaseActualChanged(ClaseGuia? value) { OnPropertyChanged(nameof(ClaseTexto)); }
    partial void OnClaseDetectadaChanged(ClaseGuia? value) => OnPropertyChanged(nameof(ClaseDetectadaTexto));
    partial void OnClaseElegidaAManoChanged(bool value) => OnPropertyChanged(nameof(ClaseDetectadaTexto));

    // ---- Escalera de equipo ----------------------------------------------------------------------

    private void ConstruirEscalera()
    {
        Etapas.Clear();
        Leyenda.Clear();
        if (ClaseActual is not ClaseGuia clase || _resumen == null) { EtapaActual = null; return; }
        var escalera = _doc.Escaleras.FirstOrDefault(e => e.Clase == GuiaV2Evaluador.ClaveClase(clase));
        var actual = _evaluador.EtapaActual(clase, _resumen);
        if (escalera != null)
            foreach (var e in escalera.Etapas)
                Etapas.Add(new EtapaEscaleraViewModel(this, e, e == actual,
                    _paradasPorId.TryGetValue(e.Desde, out var p) ? p.TituloPlano : e.Desde));
        EtapaActual = Etapas.FirstOrDefault(e => e.EsActual);
        foreach (var kv in _doc.LeyendaEscaleras) Leyenda.Add(kv);
    }

    internal EtapaEscaleraViewModel? EtapaParaParada(string paradaId)
    {
        if (ClaseActual is not ClaseGuia clase) return null;
        var e = _evaluador.EtapaParaParada(clase, paradaId);
        return e == null ? null : Etapas.FirstOrDefault(x => x.Etapa == e);
    }

    // ---- Manual, biomas y fichas -----------------------------------------------------------------

    private void ConstruirManual()
    {
        Articulos.Clear();
        foreach (var a in _doc.Articulos) Articulos.Add(new ArticuloV2ViewModel(this, a));
        Zonas.Clear();
        foreach (var z in _doc.Zonas) Zonas.Add(new ZonaV2ViewModel(this, z));
        Problemas.Clear();
        foreach (var p in _doc.Problemas) Problemas.Add(new FichaV2ViewModel(this, p));
        Hallazgos.Clear();
        foreach (var h in _doc.Hallazgos) Hallazgos.Add(new FichaV2ViewModel(this, h));
        _indiceBusqueda = null;
        _zonasResueltasPara = null;
    }

    private void SeleccionarArticulo(ArticuloV2ViewModel? a)
    {
        foreach (var x in Articulos) x.Seleccionado = x == a;
        ArticuloSeleccionado = a;
    }

    // ---- Navegacion ------------------------------------------------------------------------------

    [RelayCommand]
    private void IrA(string seccion)
    {
        if (Enum.TryParse<SeccionGuiaV2>(seccion, out var s)) Seccion = s;
    }

    [RelayCommand]
    private void ContinuarRuta()
    {
        SeleccionarParada(Siguiente ?? Paradas.LastOrDefault());
        Seccion = SeccionGuiaV2.Ruta;
    }

    public void SeleccionarParada(ParadaV2ViewModel? p)
    {
        if (p == null) return;
        ParadaSeleccionada = p;
        CapituloSeleccionado = Capitulos.FirstOrDefault(c => c.Id == p.Parada.Capitulo) ?? CapituloSeleccionado;
        if (Seccion != SeccionGuiaV2.Ruta) Seccion = SeccionGuiaV2.Ruta;
        else SeccionCambiada?.Invoke();
        _ = ResolverUbicacionParadaAsync(p);
    }

    [RelayCommand]
    private void ElegirCapitulo(CapituloV2ViewModel? c)
    {
        if (c == null) return;
        CapituloSeleccionado = c;
        // Primera pendiente del capitulo, como "Elige la etapa o continua desde la primera pendiente".
        SeleccionarParada(c.Paradas.FirstOrDefault(p => p.Pendiente) ?? c.Paradas.FirstOrDefault());
    }

    [RelayCommand]
    private void ParadaAnterior()
    {
        if (ParadaSeleccionada == null) return;
        int i = Paradas.IndexOf(ParadaSeleccionada);
        if (i > 0) SeleccionarParada(Paradas[i - 1]);
    }

    [RelayCommand]
    private void ParadaPosterior()
    {
        if (ParadaSeleccionada == null) return;
        int i = Paradas.IndexOf(ParadaSeleccionada);
        if (i >= 0 && i < Paradas.Count - 1) SeleccionarParada(Paradas[i + 1]);
    }

    // ---- Marcas manuales ------------------------------------------------------------------------

    public void MarcarTarea(string id, bool hecha)
    {
        _progreso.MarcarTarea(id, hecha);
        GuardarProgreso();
        Refresh();
    }

    [RelayCommand]
    private void AlternarParadaHecha(ParadaV2ViewModel? p)
    {
        p ??= ParadaSeleccionada;
        if (p == null || p.CompletadaSola) return;
        bool hecha = !p.MarcadaAMano;
        _progreso.MarcarParada(p.Id, hecha);
        GuardarProgreso();
        Refresh();
        // Como la guia HTML: "Etapa guardada. Pulsa Siguiente para continuar" - aqui se avanza solo
        // a la siguiente pendiente para no obligar a buscarla.
        if (hecha && Siguiente != null) SeleccionarParada(Siguiente);
    }

    [RelayCommand]
    private void AlternarAplazar(ParadaV2ViewModel? p)
    {
        p ??= ParadaSeleccionada;
        if (p == null || !p.Opcional) return;
        _progreso.AplazarParada(p.Id, !p.Aplazada);
        GuardarProgreso();
        Refresh();
    }

    // ---- Ficha de objeto ("como conseguirlo") -----------------------------------------------------

    public void AbrirObjeto(string referencia)
    {
        if (FichaObjeto != null && FichaObjeto.Objeto.Ref != referencia) _historialFicha.Push(FichaObjeto.Objeto.Ref);
        else if (FichaObjeto == null) _historialFicha.Clear();
        FichaObjeto = new FichaObjetoViewModel(this, referencia, Obtenciones(referencia), _historialFicha.Count > 0);
    }

    public void CerrarFicha()
    {
        _historialFicha.Clear();
        FichaObjeto = null;
    }

    public void VolverFicha()
    {
        if (_historialFicha.Count == 0) { CerrarFicha(); return; }
        string anterior = _historialFicha.Pop();
        FichaObjeto = new FichaObjetoViewModel(this, anterior, Obtenciones(anterior), _historialFicha.Count > 0);
    }

    /// <summary>Formas REALES de conseguir el objeto (tabla de referencias, extraida del codigo
    /// decompilado), en marcado para que ingredientes, jefes y vendedores sean clicables. Vacio =
    /// se consigue en el mundo (minado, cofres, pesca...); la ficha lo dice asi, sin inventar.</summary>
    internal IReadOnlyList<ObtencionV2ViewModel> Obtenciones(string referencia)
    {
        var lista = new List<ObtencionV2ViewModel>();
        if (!GuiaV2Recursos.Referencias.Objetos.TryGetValue(referencia, out var o)) return lista;
        foreach (var ob in o.Obtencion)
        {
            switch (ob.Tipo)
            {
                case "receta":
                {
                    var partes = ob.Ingredientes.Select(i =>
                        (i.Cantidad > 1 ? i.Cantidad + " × " : "") +
                        (i.Ref != null ? "{o:" + i.Ref + "}" : "**" + GuiaV2Recursos.NombreGrupo(i.Grupo ?? "") + "**"));
                    string texto = string.Join(" + ", partes);
                    if (ob.CantidadResultado > 1) texto += " → " + ob.CantidadResultado + " ×";
                    string estaciones = ob.Estaciones.Count == 0 ? L("guia2_obt_a_mano")
                        : string.Join(" / ", ob.Estaciones.Select(GuiaV2Recursos.NombreEstacion));
                    string detalle = F("guia2_obt_en_estacion", estaciones)
                        + (ob.Condiciones.Count > 0 ? " · " + L("guia2_obt_condicion") + " " + string.Join("; ", ob.Condiciones.Select(CondicionLegible)) : "");
                    lista.Add(new ObtencionV2ViewModel(L("guia2_obt_receta"), texto, detalle, ob.FuenteCodigo));
                    break;
                }
                case "botin":
                case "bolsa":
                {
                    string de = ob.De == null ? "?" : Token(ob.De);
                    string texto = F(ob.Tipo == "bolsa" ? "guia2_obt_bolsa_de" : "guia2_obt_cae_de", de);
                    string detalle = string.IsNullOrEmpty(ob.Probabilidad) ? "" : F("guia2_obt_probabilidad", ob.Probabilidad);
                    if (!string.IsNullOrEmpty(ob.Condicion)) detalle += (detalle.Length > 0 ? " · " : "") + L("guia2_obt_condicion") + " " + CondicionLegible(ob.Condicion);
                    lista.Add(new ObtencionV2ViewModel(L(ob.Tipo == "bolsa" ? "guia2_obt_tipo_bolsa" : "guia2_obt_tipo_botin"), texto, detalle, ob.FuenteCodigo));
                    break;
                }
                case "tienda":
                {
                    string texto = F("guia2_obt_lo_vende", ob.De == null ? "?" : Token(ob.De));
                    string detalle = string.IsNullOrEmpty(ob.Condicion) ? "" : L("guia2_obt_condicion") + " " + CondicionLegible(ob.Condicion);
                    lista.Add(new ObtencionV2ViewModel(L("guia2_obt_tienda"), texto, detalle, ob.FuenteCodigo));
                    break;
                }
                default:
                    lista.Add(new ObtencionV2ViewModel(L("guia2_obt_otro"), ob.Condicion, "", ob.FuenteCodigo));
                    break;
            }
        }
        return lista;
    }

    private static string Token(string referencia) =>
        GuiaV2Recursos.Referencias.Npcs.ContainsKey(referencia) ? "{n:" + referencia + "}"
        : GuiaV2Recursos.Referencias.Objetos.ContainsKey(referencia) ? "{o:" + referencia + "}"
        : GuiaV2Recursos.Partir(referencia).Interno;

    /// <summary>Las condiciones de receta/tienda vienen del codigo real ("Condition.DownedPlantera",
    /// "CalamityConditions.DownedOldDuke"...). Se enseñan tal cual pero sin el ruido del espacio de
    /// nombres: honesto (es el dato real) y legible.</summary>
    private static string CondicionLegible(string condicion)
    {
        string c = condicion.Trim();
        int punto = c.LastIndexOf('.', c.IndexOf('(') is int p && p > 0 ? p : c.Length - 1);
        if (punto > 0 && !c.Contains(' ')) c = c[(punto + 1)..];
        return c;
    }

    public bool PuedeLlevarALibreria(string referencia) => HayPersonaje && _recursos.BusquedaLibreria(referencia) != null;

    public string TextoLibreria(string referencia) => L(CuantosTengo(referencia) > 0 ? "guia2_btn_libreria_mas" : "guia2_btn_libreria");

    public void LlevarALibreria(string referencia)
    {
        if (_recursos.BusquedaLibreria(referencia) is not string busqueda) return;
        CerrarFicha();
        LibreriaSolicitada?.Invoke(busqueda);
    }

    // ---- Tenencia y nombres (IContextoTextoGuia) -----------------------------------------------

    internal int? CuantosTengo(string referencia)
    {
        if (!HayPersonaje || _proveedor == null) return null;
        return _recursos.IdObjeto(referencia) is int id && id > 0 ? _proveedor.CuantosPosee(id) : null;
    }

    public string NombreObjeto(string referencia) => _recursos.NombreObjeto(referencia);
    public string? IconoObjeto(string referencia) => _recursos.IconoObjeto(referencia);
    public string NombreNpc(string referencia) => _recursos.NombreNpc(referencia);
    public string? IconoNpc(string referencia) => GuiaV2Recursos.IconoNpc(referencia);
    public string NombreZona(string id) => _doc.Zonas.FirstOrDefault(z => z.Id == id)?.Nombre ?? id;
    public string TituloParada(string id) => _paradasPorId.TryGetValue(id, out var p) ? p.TituloPlano : id;
    public string TituloArticulo(string id) => _doc.Articulos.FirstOrDefault(a => a.Id == id) is { } a ? Plano(a.Titulo) : id;

    internal string NombreOtroIdioma(string referencia) =>
        GuiaV2Recursos.Referencias.Objetos.TryGetValue(referencia, out var o)
            ? (LocalizationService.Instance.Language == LocalizationService.English ? o.Es : o.En) : "";

    public void AbrirZona(string id)
    {
        CerrarFicha();
        Seccion = SeccionGuiaV2.Biomas;
        foreach (var z in Zonas) z.Resaltada = z.Id == id;
        ZonaEnfocada?.Invoke(id);
    }

    /// <summary>La vista desplaza hasta la tarjeta de la zona pedida.</summary>
    public event Action<string>? ZonaEnfocada;

    public void AbrirParada(string id)
    {
        CerrarFicha();
        if (_paradasPorId.TryGetValue(id, out var p)) SeleccionarParada(p);
    }

    public void AbrirArticulo(string id)
    {
        CerrarFicha();
        if (id == "problemas") { Seccion = SeccionGuiaV2.Perdido; return; }
        if (id == "estructuras") { Seccion = SeccionGuiaV2.Raro; return; }
        if (id == "mapa") { Seccion = SeccionGuiaV2.Biomas; return; }
        SeleccionarArticulo(Articulos.FirstOrDefault(a => a.Id == id));
        Seccion = SeccionGuiaV2.Manual;
        SeccionCambiada?.Invoke();
    }

    /// <summary>Texto plano del marcado con los nombres en el idioma activo (titulos de lista,
    /// buscador, tooltips del mapa).</summary>
    internal string Plano(string texto) => PlanoConNombres(texto);

    private string PlanoConNombres(string texto)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in GuiaV2Texto.Analizar(texto))
            sb.Append(s.Tipo switch
            {
                TipoSegmento.Texto or TipoSegmento.Negrita => s.Valor,
                _ when s.TextoPropio != null => s.TextoPropio,
                TipoSegmento.Objeto => NombreObjeto(s.Valor),
                TipoSegmento.Npc => NombreNpc(s.Valor),
                TipoSegmento.Zona => NombreZona(s.Valor),
                TipoSegmento.Parada => TituloParadaSinRecursion(s.Valor),
                TipoSegmento.Articulo => TituloArticuloSinRecursion(s.Valor),
                _ => s.Valor,
            });
        return sb.ToString();
    }

    private string TituloParadaSinRecursion(string id) => _doc.Paradas.FirstOrDefault(p => p.Id == id) is { } p ? PlanoSimple(p.Titulo) : id;
    private string TituloArticuloSinRecursion(string id) => _doc.Articulos.FirstOrDefault(a => a.Id == id) is { } a ? PlanoSimple(a.Titulo) : id;

    private string PlanoSimple(string texto)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in GuiaV2Texto.Analizar(texto))
            sb.Append(s.Tipo switch
            {
                TipoSegmento.Texto or TipoSegmento.Negrita => s.Valor,
                _ when s.TextoPropio != null => s.TextoPropio,
                TipoSegmento.Objeto => NombreObjeto(s.Valor),
                TipoSegmento.Npc => NombreNpc(s.Valor),
                TipoSegmento.Zona => NombreZona(s.Valor),
                _ => s.Valor,
            });
        return sb.ToString();
    }

    // ---- Buscador ---------------------------------------------------------------------------------

    private List<(string Tipo, string Titulo, string Plano, Action Abrir, string? Icono)>? _indiceBusqueda;

    private void Buscar(string consulta)
    {
        Resultados.Clear();
        string q = LibrarySearchGrammar.Fold(consulta.Trim());
        if (q.Length < 2) return;
        _indiceBusqueda ??= ConstruirIndice();
        string[] palabras = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in _indiceBusqueda)
        {
            string tituloF = LibrarySearchGrammar.Fold(e.Titulo);
            if (!palabras.All(p => tituloF.Contains(p) || e.Plano.Contains(p))) continue;
            Resultados.Add(new ResultadoBusquedaV2ViewModel(e.Tipo, e.Titulo, Extracto(e.Plano, palabras[0]), e.Abrir, e.Icono));
            if (Resultados.Count >= 80) break;
        }
        OnPropertyChanged(nameof(SinResultados));
    }

    public bool SinResultados => !string.IsNullOrWhiteSpace(TextoBusqueda) && TextoBusqueda.Trim().Length >= 2 && Resultados.Count == 0;

    private static string Extracto(string planoFold, string palabra)
    {
        int i = planoFold.IndexOf(palabra, StringComparison.Ordinal);
        if (i < 0) return "";
        int desde = Math.Max(0, i - 50);
        int hasta = Math.Min(planoFold.Length, i + 110);
        return (desde > 0 ? "…" : "") + planoFold[desde..hasta].Trim() + (hasta < planoFold.Length ? "…" : "");
    }

    private List<(string, string, string, Action, string?)> ConstruirIndice()
    {
        var idx = new List<(string, string, string, Action, string?)>();
        string Fold(string s) => LibrarySearchGrammar.Fold(PlanoConNombres(s));
        string Bloques(IEnumerable<Bloque> bs) => string.Join(" ", bs.Select(TextoDeBloque));

        foreach (var p in _doc.Paradas)
        {
            string id = p.Id;
            string todo = string.Join(" ", new[] { p.Titulo, p.Donde, p.Preparate, p.Combate, p.Desbloquea, p.ListoCuando, p.Conserva, p.Invocacion?.Notas ?? "" }
                .Concat(p.Tareas.Select(t => t.Texto)).Concat(p.Jefes.Select(j => "{n:" + j + "}")));
            idx.Add((L("guia2_buscar_tipo_parada"), PlanoConNombres(p.Titulo), Fold(todo), () => AbrirParada(id),
                p.Jefes.Select(GuiaV2Recursos.IconoNpc).FirstOrDefault(i => i != null)));
        }
        foreach (var a in _doc.Articulos)
        {
            string id = a.Id;
            idx.Add((L("guia2_buscar_tipo_articulo"), PlanoConNombres(a.Titulo), Fold(a.Subtitulo + " " + Bloques(a.Bloques)), () => AbrirArticulo(id), null));
        }
        foreach (var z in _doc.Zonas)
        {
            string id = z.Id;
            idx.Add((L("guia2_buscar_tipo_zona"), z.Nombre, Fold(z.Resumen + " " + Bloques(z.Bloques)), () => AbrirZona(id), null));
        }
        foreach (var f in _doc.Problemas)
        {
            string id = f.Id;
            idx.Add((L("guia2_buscar_tipo_problema"), f.Titulo, Fold(Bloques(f.Bloques)), () => AbrirFicha(Problemas, id, SeccionGuiaV2.Perdido), null));
        }
        foreach (var f in _doc.Hallazgos)
        {
            string id = f.Id;
            idx.Add((L("guia2_buscar_tipo_hallazgo"), f.Titulo, Fold(Bloques(f.Bloques)), () => AbrirFicha(Hallazgos, id, SeccionGuiaV2.Raro), null));
        }
        // Objetos citados por la guia: ficha "como conseguirlo".
        var citados = new HashSet<string>();
        foreach (var p in _doc.Paradas)
        {
            foreach (var t in new[] { p.Titulo, p.Preparate, p.Conserva, p.ListoCuando, p.Desbloquea }.Concat(p.Tareas.Select(t => t.Texto)))
                foreach (var s in GuiaV2Texto.Analizar(t))
                    if (s.Tipo == TipoSegmento.Objeto) citados.Add(s.Valor);
            foreach (var nx in p.Necesitas) citados.Add(nx.Ref);
            if (p.Invocacion?.Objeto is string io) citados.Add(io);
            foreach (var c in p.ConservaObjetos) citados.Add(c);
        }
        foreach (var e in _doc.Escaleras.SelectMany(e => e.Etapas))
            foreach (var o in e.Armas.Concat(e.Armadura).Concat(e.Accesorios).Concat(e.Otros)) citados.Add(o.Ref);
        foreach (var r in citados.OrderBy(r => r, StringComparer.Ordinal))
        {
            string referencia = r;
            string otro = GuiaV2Recursos.Referencias.Objetos.TryGetValue(r, out var o) ? o.Es + " " + o.En : "";
            idx.Add((L("guia2_buscar_tipo_objeto"), NombreObjeto(r), LibrarySearchGrammar.Fold(otro + " " + r), () => AbrirObjeto(referencia), IconoObjeto(r)));
        }
        return idx;
    }

    private void AbrirFicha(ObservableCollection<FichaV2ViewModel> lista, string id, SeccionGuiaV2 seccion)
    {
        foreach (var f in lista) { f.Resaltada = f.Id == id; if (f.Id == id) f.Abierta = true; }
        Seccion = seccion;
        FichaEnfocada?.Invoke(id);
    }

    /// <summary>La vista desplaza hasta la ficha pedida (Estoy perdido / He encontrado algo raro).</summary>
    public event Action<string>? FichaEnfocada;

    private static string TextoDeBloque(Bloque b) => string.Join(" ",
        new[] { b.Titulo, b.Texto }.Concat(b.Items).Concat(b.Cabeceras).Concat(b.Filas.SelectMany(f => f))
            .Concat(b.Bloques.Select(TextoDeBloque)));

    // ---- Ubicaciones en el mundo real y marcador del mapa --------------------------------------

    private IReadOnlyDictionary<string, (int X, int Y)>? _puntosCalamity;
    private string? _mundoCacheRuta;
    private WldWorld? _mundoCacheObjeto;
    private Task<IReadOnlyDictionary<(int X, int Y), string>>? _tilesModTarea;
    private readonly Dictionary<string, UbicacionResuelta?> _cacheUbicaciones = [];
    private string? _zonasResueltasPara;

    [ObservableProperty] private bool _marcadorVisible;
    [ObservableProperty] private int _marcadorX;
    [ObservableProperty] private int _marcadorY;
    [ObservableProperty] private bool _marcadorAproximado;
    [ObservableProperty] private string _marcadorTitulo = "";
    [ObservableProperty] private string _marcadorDetalle = "";
    [ObservableProperty] private string _siguienteUbicacionTexto = "";
    [ObservableProperty] private bool _siguienteTieneUbicacion;

    /// <summary>Radio en casillas del circulo de zona aproximada (escala con el zoom del mapa).</summary>
    public double MarcadorRadioCasillas => 90;
    /// <summary>Tarea del ultimo calculo del marcador (para pruebas y el arnes).</summary>
    public Task MarcadorTarea { get; private set; } = Task.CompletedTask;

    private bool NecesitaTilesMod => _doc.Zonas.Any(z => z.Firma.TilesMod.Count > 0);

    private void PrepararMundo(out Task<IReadOnlyDictionary<(int X, int Y), string>>? tilesMod)
    {
        tilesMod = null;
        var world = _world();
        if (world == null) return;
        string? ruta = _worldPath();
        if (!ReferenceEquals(world, _mundoCacheObjeto) || ruta != _mundoCacheRuta)
        {
            _mundoCacheObjeto = world;
            _mundoCacheRuta = ruta;
            lock (_cacheUbicaciones) _cacheUbicaciones.Clear();
            _zonasResueltasPara = null;
            _tilesModTarea = null;
        }
        if (NecesitaTilesMod && _tilesModTarea == null && !string.IsNullOrEmpty(ruta) && File.Exists(Path.ChangeExtension(ruta, ".twld")))
            _tilesModTarea = Task.Run(() => MundoGuiaEscritorio.LeerTilesModMuestreados(world, ruta));
        tilesMod = _tilesModTarea;
    }

    private async Task<MundoGuiaEscritorio?> MundoListoAsync()
    {
        var world = _world();
        if (world == null) return null;
        PrepararMundo(out var tarea);
        IReadOnlyDictionary<(int X, int Y), string>? tiles = tarea != null ? await tarea.ConfigureAwait(true) : null;
        if (!ReferenceEquals(world, _world())) return null; // cambio de mundo mientras se leia
        return new MundoGuiaEscritorio(world, tiles, _puntosCalamity);
    }

    /// <summary>Ubicacion de una parada en el mundo real: NPC del pueblo con posicion guardada
    /// (exacta), o zona/punto via GuiaV2Ubicaciones (exacta o aproximada, lo dice).</summary>
    private UbicacionResuelta? ResolverParada(Parada parada, MundoGuiaEscritorio mundo, WldWorld world)
    {
        lock (_cacheUbicaciones)
            if (_cacheUbicaciones.TryGetValue(parada.Id, out var cache)) return cache;
        UbicacionResuelta? r = null;
        bool? carmesi = world.Header.IsCrimson;
        foreach (var u in parada.Ubicaciones)
        {
            if (u.Tipo is "npc" or "jefe")
            {
                if (_recursos.Resolutor.Npc(u.Id) is int npcId && world.Npcs.FirstOrDefault(n => n.Id == npcId) is { } npc)
                {
                    r = new UbicacionResuelta(npc.TileX, npc.TileY, false, "npc:" + u.Id);
                    break;
                }
                continue;
            }
            var sola = new Parada { Id = parada.Id, Ubicaciones = [u] };
            r = GuiaV2Ubicaciones.ResolverParada(sola, _doc, mundo, carmesi, MundoGuiaEscritorio.PasoMuestreo);
            if (r != null) break;
        }
        lock (_cacheUbicaciones) _cacheUbicaciones[parada.Id] = r;
        return r;
    }

    private string TextoUbicacion(UbicacionResuelta? u, WldWorld world)
    {
        if (u == null) return L("guia2_ubicacion_no_encontrada");
        string pos = DescribirPosicion(u.X, u.Y, world);
        return u.Aproximada ? F("guia2_ubicacion_aproximada", pos) : F("guia2_ubicacion_exacta", pos);
    }

    /// <summary>Posicion en el lenguaje del juego (como el GPS/Profundimetro): "1.200 al este, 300
    /// bajo la superficie", relativo al centro del mundo y a la superficie.</summary>
    private string DescribirPosicion(int x, int y, WldWorld world)
    {
        int centro = world.Header.TilesWide / 2;
        int dx = (x - centro) * 2; // el GPS del juego cuenta en pies (2 por casilla)
        string horizontal = dx == 0 ? L("guia2_pos_centro") : F(dx > 0 ? "guia2_pos_este" : "guia2_pos_oeste", Math.Abs(dx).ToString("N0"));
        int dy = (int)((y - world.Header.GroundLevel) * 2);
        string vertical = dy <= 0 ? F("guia2_pos_superficie", Math.Abs(dy).ToString("N0")) : F("guia2_pos_profundidad", dy.ToString("N0"));
        return horizontal + ", " + vertical;
    }

    private async Task ActualizarMarcadorAsync()
    {
        var tarea = ActualizarMarcadorInternoAsync();
        MarcadorTarea = tarea;
        await tarea;
    }

    private async Task ActualizarMarcadorInternoAsync()
    {
        var world = _world();
        var siguiente = Siguiente;
        if (world == null || siguiente == null)
        {
            MarcadorVisible = false;
            SiguienteTieneUbicacion = false;
            SiguienteUbicacionTexto = world == null ? L("guia2_ubicacion_sin_mundo") : "";
            return;
        }
        try
        {
            var mundo = await MundoListoAsync();
            if (mundo == null || !ReferenceEquals(world, _world())) return;
            var parada = siguiente.Parada;
            var u = await Task.Run(() => ResolverParada(parada, mundo, world));
            if (!ReferenceEquals(world, _world())) return;
            ColocarMarcador(siguiente, u, world);
            SiguienteTieneUbicacion = u != null;
            SiguienteUbicacionTexto = TextoUbicacion(u, world);
            siguiente.TieneUbicacion = u != null;
            siguiente.UbicacionTexto = SiguienteUbicacionTexto;
        }
        catch (Exception)
        {
            MarcadorVisible = false; // un mundo raro nunca rompe la guia
        }
    }

    private void ColocarMarcador(ParadaV2ViewModel p, UbicacionResuelta? u, WldWorld world)
    {
        if (u == null) { MarcadorVisible = false; return; }
        MarcadorX = Math.Clamp(u.X, 0, world.Header.TilesWide - 1);
        MarcadorY = Math.Clamp(u.Y, 0, world.Header.TilesHigh - 1);
        MarcadorAproximado = u.Aproximada;
        MarcadorTitulo = F(p == Siguiente ? "guia2_marcador_siguiente" : "guia2_marcador_parada", p.Numero, p.TituloPlano);
        MarcadorDetalle = TextoUbicacion(u, world);
        MarcadorVisible = true;
    }

    private async Task ResolverUbicacionParadaAsync(ParadaV2ViewModel p)
    {
        var world = _world();
        if (world == null) { p.TieneUbicacion = false; p.UbicacionTexto = L("guia2_ubicacion_sin_mundo"); return; }
        try
        {
            var mundo = await MundoListoAsync();
            if (mundo == null) return;
            var u = await Task.Run(() => ResolverParada(p.Parada, mundo, world));
            if (!ReferenceEquals(world, _world())) return;
            p.TieneUbicacion = u != null;
            p.UbicacionTexto = TextoUbicacion(u, world);
        }
        catch (Exception) { /* sin ubicacion: el texto se queda vacio */ }
    }

    [RelayCommand]
    private async Task VerParadaEnMapa(ParadaV2ViewModel? p)
    {
        p ??= ParadaSeleccionada ?? Siguiente;
        var world = _world();
        if (p == null || world == null) return;
        var mundo = await MundoListoAsync();
        if (mundo == null) return;
        var u = await Task.Run(() => ResolverParada(p.Parada, mundo, world));
        if (u == null) return;
        ColocarMarcador(p, u, world);
        VerEnMapaSolicitado?.Invoke(MarcadorX, MarcadorY);
    }

    public void VerZonaEnMapa(ZonaV2ViewModel z)
    {
        var world = _world();
        if (z.Ubicacion is not { } u || world == null) return;
        MarcadorX = u.X;
        MarcadorY = u.Y;
        MarcadorAproximado = u.Aproximada;
        MarcadorTitulo = z.Nombre;
        MarcadorDetalle = TextoUbicacion(u, world);
        MarcadorVisible = true;
        VerEnMapaSolicitado?.Invoke(u.X, u.Y);
    }

    /// <summary>Sitúa cada zona del manual de biomas en el mundo real (una vez por mundo).</summary>
    public async Task ResolverZonasAsync()
    {
        var world = _world();
        if (world == null)
        {
            foreach (var z in Zonas) { z.TieneUbicacion = false; z.UbicacionTexto = L("guia2_ubicacion_sin_mundo"); z.Ubicacion = null; }
            return;
        }
        string clave = (_worldPath() ?? "") + "|" + GuiaId;
        if (_zonasResueltasPara == clave) return;
        _zonasResueltasPara = clave;
        try
        {
            var mundo = await MundoListoAsync();
            if (mundo == null) return;
            var zonas = Zonas.ToList();
            var resultados = await Task.Run(() => zonas.Select(z => GuiaV2Ubicaciones.Resolver(z.Zona, mundo, MundoGuiaEscritorio.PasoMuestreo)).ToList());
            if (!ReferenceEquals(world, _world())) return;
            for (int i = 0; i < zonas.Count; i++)
            {
                zonas[i].Ubicacion = resultados[i];
                zonas[i].TieneUbicacion = resultados[i] != null;
                zonas[i].UbicacionTexto = resultados[i] == null ? L("guia2_zona_no_existe") : TextoUbicacion(resultados[i], world);
            }
        }
        catch (Exception) { _zonasResueltasPara = null; }
    }
}
