using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Guia;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels;

// Fase B (15-sep-2026): una linea de requisito ya evaluada contra el personaje/mundo reales,
// lista para mostrar. Espejo de fondo de un ResultadoRequisitoGuia, pero como ObservableObject
// para poder refrescar SOLO el texto (Linea) al cambiar de idioma, sin recalcular nada del
// evaluador - mismo criterio ya usado por BuildStageViewModel.
public sealed partial class GuideRequisitoViewModel : ObservableObject
{
    private readonly ResultadoRequisitoGuia _resultado;
    private readonly GuideTextCatalog _textos;

    internal GuideRequisitoViewModel(ResultadoRequisitoGuia resultado, GuideTextCatalog textos)
    {
        _resultado = resultado;
        _textos = textos;
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Guia Fase A REABIERTA (26-sep-2026): SOLO para pruebas (Terrakeep.App.ViewModels.Tests - sin
    // InternalsVisibleTo configurado hacia el arnes, mismo criterio real ya establecido por
    // CharacterListEntryViewModel.AccesoriosParaPruebas/CharacterFileService.
    // DebugCorruptPlrBytesBeforeVerify) - deja probar el mapeo 1:1 de EsLimiteEstructural/etc.
    // contra un ResultadoRequisitoGuia sintetico, sin depender de que el catalogo real de la Guia
    // (guia_progresion.json) tenga HOY un requisito concreto en ese estado exacto.
    public static GuideRequisitoViewModel ParaPruebas(ResultadoRequisitoGuia resultado, GuideTextCatalog textos) =>
        new(resultado, textos);

    public bool Cumplido => _resultado.Cumplido;
    public bool Recomendado => _resultado.Requisito.Recomendado;
    public bool NoEvaluable => _resultado.NoEvaluable;
    public string Icono => Cumplido ? "✓" : NoEvaluable ? "?" : "○";
    public string Linea => _textos.Format(_resultado.TextoClave, LocalizationService.Instance.Language, _resultado.TextoArgs);
    // Bug real (A10-IDIOMA-BARRIDO, 15-sep-2026): la primera version exponia
    // MotivoNoEvaluableEnEscritorio (texto literal en español fijado desde Terrakeep.Core) tal
    // cual, sin pasar por ningun catalogo de idioma - se quedaba en español con la app en ingles
    // en los 6 tramos con un requisito de Defensa. Ahora el resultado solo trae una CLAVE
    // (MotivoClave) y esta propiedad la resuelve aqui, en la capa que SI conoce el idioma
    // (LocalizationService, strings_es.json/strings_en.json - mismo diccionario que el resto de
    // la interfaz, no el catalogo de la Guia sincronizado del mod).
    public string? Motivo => string.IsNullOrEmpty(_resultado.MotivoClave) ? null : LocalizationService.Instance[_resultado.MotivoClave];

    // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689): preparacion para la fase XAML
    // siguiente - un requisito limite ESTRUCTURAL (p.ej. NpcActivo, jamas evaluable en escritorio,
    // ver GuideEvaluationEngine.PasoCompletado/Preparacion) necesita distinguirse visualmente de
    // uno "sin datos TODAVIA" (que SI se resolveria cargando personaje/mundo) - de momento solo
    // expone el dato 1:1 desde el resultado real, sin cambiar ningun XAML todavia.
    public bool EsLimiteEstructural => _resultado.EsLimiteEstructural;

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Linea));
        OnPropertyChanged(nameof(Motivo));
    }
}

public sealed partial class GuidePasoViewModel : ObservableObject
{
    private readonly PasoGuia _paso;
    private readonly GuideTextCatalog _textos;

    internal GuidePasoViewModel(PasoGuia paso, GuideTextCatalog textos, List<GuideRequisitoViewModel> requisitos,
        bool completado, float preparacion, int cumplidos, int totalObligatorios, CharacterFileService servicio)
    {
        _paso = paso;
        _textos = textos;
        Requisitos = requisitos;
        Completado = completado;
        PreparacionPct = (int)Math.Round(preparacion * 100.0);
        Cumplidos = cumplidos;
        TotalObligatorios = totalObligatorios;
        IconPath = ResolverIconoDelHito(paso, servicio);
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    public IReadOnlyList<GuideRequisitoViewModel> Requisitos { get; }
    public bool Completado { get; }
    public string Icono => Completado ? "✓" : "○";
    public int PreparacionPct { get; }
    public int Cumplidos { get; }
    public int TotalObligatorios { get; }
    // Encargo3 (24-sep-2026, revision-correccion-integral-familia-Keep): sprite real del hito de
    // este paso (jefe/objeto/NPC-vecino), NUNCA un icono inventado - reutiliza tal cual los
    // resolvers ya existentes de la app de escritorio (LibraryCategoryTreeBuilder.ResolveIconPath
    // para objetos vanilla/Calamity, NpcIconResolver para NPCs de pueblo/jefes con sprite de
    // cuerpo entero real) - ver ResolverIconoDelHito para la cadena de fallback completa. Null es
    // un resultado real y esperado (paso sin ningun requisito de objeto/NPC, o un jefe sin sprite
    // en el roster de 27 NPCs de pueblo - la inmensa mayoria de jefes no lo son) - la UI lo trata
    // como "sin icono", nunca como un hueco roto.
    public string? IconPath { get; }

    public string Titulo => _textos.Text("Guia.Paso." + _paso.Clave + ".Titulo", LocalizationService.Instance.Language);
    public string Porque => _textos.Text("Guia.Paso." + _paso.Clave + ".Porque", LocalizationService.Instance.Language);
    public string Como => _textos.Text("Guia.Paso." + _paso.Clave + ".Como", LocalizationService.Instance.Language);
    public string ZonaLegible => string.IsNullOrEmpty(_paso.Zona)
        ? "" : _textos.Text("Guia.Zona." + _paso.Zona, LocalizationService.Instance.Language);
    // Idea 7 (catalogo de funciones, "Capa Guia sobre el mapa" - bitacora.md 20-sep-2026): la
    // clave CRUDA ("Mazmorra"/"Cavernas"/...), no el texto ya redactado de ZonaLegible - el mapa
    // de Exploracion la usa para decidir que marcador/banda dibujar, nunca para mostrarla.
    public string Zona => _paso.Zona;

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(Porque));
        OnPropertyChanged(nameof(Como));
        OnPropertyChanged(nameof(ZonaLegible));
    }

    // Encargo3/Encargo4/GuiaCalamity Encargo B: cadena de fallback real, calculable con datos ya
    // presentes en el propio paso (no inventa nada) - "hito" = lo que de verdad marca este paso
    // como superado:
    // 1) paso.Jefe (!=0): el paso es una pelea de jefe VANILLA - Encargo4 (25-sep-2026) le da
    //    sprite real de cuerpo entero via BossIconResolver (Assets/boss_icons/{type}.png,
    //    extraido de Images/NPC_{type}.xnb - ver scripts/extraer-sprites-jefes-vanilla.js), que
    //    cubre los 23 NPC types de jefe/segmento final REALMENTE usados en guia_progresion.json.
    //    Si el jefe ademas fuese un NPC de pueblo (no ocurre hoy, pero es una red de seguridad
    //    honesta, no un invento) cae a NpcIconResolver antes de rendirse a null.
    // 2) Si no, paso.JefeMod (pid no vacio): el paso es una pelea de jefe de CALAMITY -
    //    GuiaCalamity Encargo B (25-sep-2026) le da sprite real de icono de cabeza via
    //    CalamityBossIconResolver (Assets/calamity_boss_icons/{InternalName}.png, extraido
    //    directamente del .rawimg de icono de cabeza real del .tmod - ver
    //    scripts/extraer-sprites-jefes-calamity.js), que cubre 27 de los 29 pids
    //    jefeMod/jefeFinalMod REALMENTE usados en guia_progresion.json (los 2 restantes,
    //    HiveMind/PerforatorHive, solo aparecen como jefeFinalMod de TRAMO, sin ningun paso
    //    propio - null es el resultado honesto para ellos, igual que jefeFinal=13 en Encargo4).
    //    Nunca se cae al siguiente escalon si el pid no resuelve (mezclar "el jefe de este paso"
    //    con "el primer objeto que pide" seria enganoso, no un fallback razonable) - mismo
    //    criterio que el caso 1.
    // 3) Si no hay jefe: el primer requisito Objeto/ObjetoCualquiera del paso (por orden real del
    //    catalogo) via LibraryCategoryTreeBuilder.ResolveIconPath - el MISMO resolver que ya usa la
    //    Libreria/Investigacion para vanilla (Assets/vanilla/icons) y Calamity (Assets/calamity/
    //    icons, via CalamityCatalog.BySyntheticId) - los ids de RequisitoGuia.Id/Ids YA vienen
    //    resueltos a esa misma numeracion por GuideCatalog.ResolverReferenciasDeMod.
    // 4) Si tampoco hay objeto: el primer requisito Npc/NpcActivo del paso via NpcIconResolver.
    // 5) Ninguno de los cuatro: null, la UI lo trata como "sin icono" (converter NullToVis ya usado
    //    en toda la app), nunca un hueco roto.
    private static string? ResolverIconoDelHito(PasoGuia paso, CharacterFileService servicio)
    {
        if (paso.Jefe != 0) return BossIconResolver.GetIconPath(paso.Jefe) ?? NpcIconResolver.GetIconPath(paso.Jefe);
        if (!string.IsNullOrEmpty(paso.JefeMod)) return CalamityBossIconResolver.GetIconPath(paso.JefeMod);

        var objeto = paso.Requisitos.FirstOrDefault(r =>
            r.Tipo == TipoRequisitoGuia.Objeto || r.Tipo == TipoRequisitoGuia.ObjetoCualquiera);
        if (objeto != null)
        {
            int id = objeto.Tipo == TipoRequisitoGuia.Objeto ? objeto.Id : (objeto.Ids?.FirstOrDefault() ?? 0);
            return LibraryCategoryTreeBuilder.ResolveIconPath(servicio, id);
        }

        var npc = paso.Requisitos.FirstOrDefault(r =>
            r.Tipo == TipoRequisitoGuia.Npc || r.Tipo == TipoRequisitoGuia.NpcActivo);
        return npc != null ? NpcIconResolver.GetIconPath(npc.Id) : null;
    }
}

public sealed partial class GuideTramoViewModel : ObservableObject
{
    private readonly TramoGuia _tramo;
    private readonly GuideTextCatalog _textos;

    internal GuideTramoViewModel(TramoGuia tramo, GuideTextCatalog textos, List<GuidePasoViewModel> pasos, bool completado)
    {
        _tramo = tramo;
        _textos = textos;
        Pasos = pasos;
        Completado = completado;
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    public IReadOnlyList<GuidePasoViewModel> Pasos { get; }
    public bool Completado { get; }
    public string Icono => Completado ? "✓" : "○";
    public bool EsOpcional => _tramo.Opcional;
    public bool EsCalamity => _tramo.Ambito == AmbitoGuia.Calamity;
    public bool Implementado => _tramo.Implementado;

    public string Nombre => _textos.Text("Guia.Tramo." + _tramo.Clave + ".Nombre", LocalizationService.Instance.Language);
    public string Resumen => _textos.Text("Guia.Tramo." + _tramo.Clave + ".Resumen", LocalizationService.Instance.Language);

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Nombre));
        OnPropertyChanged(nameof(Resumen));
    }
}

// Fase B (integracion de la Guia dentro de Terrakeep de escritorio, 15-sep-2026): pestaña nueva
// pedida explicitamente por el usuario - un jugador 100% vanilla (sin tModLoader instalado) tiene
// que poder ver la MISMA guia de progresion que TerrakeepMod, evaluada contra el personaje/mundo
// reales que YA tiene cargados en Terrakeep. Ver Terrakeep.Core/Guia/ para el catalogo/evaluador
// reales y GuideFlags.cs/GuideEvaluator.cs para el detalle honesto de que se puede y que no se
// puede comprobar desde un editor de ficheros estatico (varios tipos de requisito quedan
// SIEMPRE "no evaluable, motivo real" - nunca un falso verde).
public sealed partial class GuideViewModel : ObservableObject
{
    private readonly GuideCatalog _catalogo;
    private readonly GuideTextCatalog _textos;
    private readonly GuideEvaluator _evaluador;

    private readonly Func<LoadedCharacter?> _character;
    private readonly Func<WldWorld?> _world;
    private readonly Func<bool> _hasCalamity;
    // Guia Encargo A "GuiaCalamity" (25-sep-2026): ruta real del .wld cargado (la misma que ya
    // expone ExplorationViewModel.CurrentWorldPath) - hace falta para localizar el `.twld` hermano
    // y leer las banderas de jefe de Calamity, ver ResolveCalamityDownedFlags.
    private readonly Func<string?> _worldPath;
    // Encargo3 (24-sep-2026): guardado tal cual (antes solo vivia como parametro local del
    // constructor) - Refresh() lo necesita en cada vuelta para resolver el icono real de cada
    // GuidePasoViewModel (ver ResolverIconoDelHito), mismos catalogos ya cargados en memoria que
    // usa el resto de Terrakeep, sin volver a leer nada de disco.
    private readonly CharacterFileService _servicio;

    public GuideViewModel(CharacterFileService servicio, Func<LoadedCharacter?> character, Func<WldWorld?> world, Func<bool> hasCalamity, Func<string?> worldPath)
    {
        _servicio = servicio;
        _character = character;
        _world = world;
        _hasCalamity = hasCalamity;
        _worldPath = worldPath;

        string assetsGuia = Path.Combine(AppContext.BaseDirectory, "Assets", "guia");
        _catalogo = GuideCatalog.LoadFromFile(Path.Combine(assetsGuia, "guia_progresion.json"), servicio.CalamityCatalog);
        _textos = GuideTextCatalog.LoadFromFiles(Path.Combine(assetsGuia, "textos.es.json"), Path.Combine(assetsGuia, "textos.en.json"));
        _evaluador = new GuideEvaluator(servicio.VanillaCatalog, servicio.NpcNames, servicio.CalamityCatalog, servicio.VanillaStats, servicio.PrefixEffects, servicio.PrefixRules);
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");

        Refresh();
    }

    // Texto "de chrome" de la pestaña (el mismo texto que ya escribio el mod - no se retraduce
    // aparte, se reutiliza tal cual de GuideTextCatalog, mismas claves "Guia.*").
    public string TextoObjetivoActual => _textos.Text("Guia.ObjetivoActual", LocalizationService.Instance.Language);
    public string TextoSinObjetivo => _textos.Text("Guia.SinObjetivo", LocalizationService.Instance.Language);
    public string TextoPorQue => _textos.Text("Guia.PorQue", LocalizationService.Instance.Language);
    public string TextoComo => _textos.Text("Guia.Como", LocalizationService.Instance.Language);
    public string TextoQueTeFalta => _textos.Text("Guia.QueTeFalta", LocalizationService.Instance.Language);
    public string TextoEsteTramo => _textos.Text("Guia.EsteTramo", LocalizationService.Instance.Language);
    public string TextoLoQueViene => _textos.Text("Guia.LoQueViene", LocalizationService.Instance.Language);
    public string TextoAvisoCalamityTitulo => _textos.Text("Guia.AvisoCalamityTitulo", LocalizationService.Instance.Language);
    public string TextoAvisoCalamity => _textos.Text("Guia.AvisoCalamity", LocalizationService.Instance.Language);

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TextoObjetivoActual));
        OnPropertyChanged(nameof(TextoSinObjetivo));
        OnPropertyChanged(nameof(TextoPorQue));
        OnPropertyChanged(nameof(TextoComo));
        OnPropertyChanged(nameof(TextoQueTeFalta));
        OnPropertyChanged(nameof(TextoEsteTramo));
        OnPropertyChanged(nameof(TextoLoQueViene));
        OnPropertyChanged(nameof(TextoAvisoCalamityTitulo));
        OnPropertyChanged(nameof(TextoAvisoCalamity));
        OnPropertyChanged(nameof(TextoAvisoSinPersonaje));
        OnPropertyChanged(nameof(TextoAvisoSinMundo));
    }

    public ObservableCollection<GuideTramoViewModel> Tramos { get; } = [];

    [ObservableProperty] private GuideTramoViewModel? _objetivoTramo;
    [ObservableProperty] private GuidePasoViewModel? _objetivoPaso;
    [ObservableProperty] private bool _hasAnyData;
    [ObservableProperty] private bool _mostrarAvisoCalamity;
    // Bug real reportado en directo (16-sep-2026, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): con un
    // mundo cargado pero SIN personaje ("Sin personaje cargado" en la cabecera), la mayoria de
    // requisitos (Objeto/ObjetoCualquiera/Gancho, HasInventoryData) se quedan NoEvaluable - un
    // comportamiento ESPERADO (el .wld no guarda el inventario, hace falta el .plr) pero mal
    // comunicado: antes de esta ronda, la unica pista era el motivo pequeño bajo cada linea, uno
    // por uno, facil de no leer entre docenas de requisitos - exactamente el sintoma real
    // reportado ("da igual donde toques, siempre pone lo mismo"). Ahora un aviso unico y visible
    // lo dice una sola vez, arriba de todo.
    [ObservableProperty] private bool _mostrarAvisoSinPersonaje;

    // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689): mismo aviso global que
    // MostrarAvisoSinPersonaje de arriba, en espejo (personaje cargado, mundo NO) - preparacion
    // para la fase XAML siguiente, que le añadira el bloque visible correspondiente (mismo patron
    // que MostrarAvisoSinPersonaje ya tiene hoy).
    [ObservableProperty] private bool _mostrarAvisoSinMundo;

    public string TextoAvisoSinPersonaje => LocalizationService.Instance["guide_no_character_notice"];
    public string TextoAvisoSinMundo => LocalizationService.Instance["guide_no_world_notice"];

    /// <summary>Se llama tras cargar/cerrar un personaje y al entrar en la pestaña - vuelve a
    /// evaluar TODO el arbol contra el estado real actual (nunca cachea nada entre pasos: mismo
    /// criterio "siempre en vivo" que el propio mod, adaptado a "en vivo" = "cada vez que el
    /// usuario puede haber cambiado algo y vuelto a mirar la Guia").</summary>
    public void Refresh()
    {
        var loaded = _character();
        var world = _world();
        bool hasCalamity = _hasCalamity();

        var contexto = new GuideContext
        {
            Character = loaded?.Character,
            MergedContainers = loaded?.MergedContainers,
            World = world,
            HasCalamity = hasCalamity,
            // Guia Encargo A "GuiaCalamity" (25-sep-2026): null exactamente cuando no hay mundo
            // cargado (mismo contrato documentado en GuideContext.CalamityDownedFlags) - con
            // mundo cargado, SIEMPRE un set real (vacio si no hay `.twld`/DownedBossSystem).
            CalamityDownedFlags = world != null ? ResolveCalamityDownedFlags(_worldPath()) : null,
        };

        HasAnyData = loaded != null || world != null;
        MostrarAvisoCalamity = hasCalamity;
        MostrarAvisoSinPersonaje = world != null && loaded == null;
        // Guia Fase A REABIERTA (26-sep-2026): espejo exacto del aviso de arriba - mutuamente
        // excluyentes por construccion (uno exige loaded==null, el otro loaded!=null), nunca
        // ambos true a la vez.
        MostrarAvisoSinMundo = world == null && loaded != null;

        Tramos.Clear();
        GuideTramoViewModel? objetivoTramo = null;
        GuidePasoViewModel? objetivoPaso = null;

        foreach (var tramo in _catalogo.Tramos)
        {
            // Ambito: vanilla siempre se enseña; Calamity solo si el personaje cargado lo usa
            // (mismo criterio de deteccion que el resto de Terrakeep - HasCalamityData, un .tplr
            // real junto al .plr). "Ambos" (ninguno hoy) se enseñaria siempre.
            if (tramo.Ambito == AmbitoGuia.Calamity && !hasCalamity) continue;

            var pasos = new List<GuidePasoViewModel>();
            foreach (var paso in tramo.Pasos)
            {
                var resultados = _evaluador.Evaluar(paso, contexto);
                var requisitos = resultados.Select(r => new GuideRequisitoViewModel(r, _textos)).ToList();
                bool pasoCompletado = tramo.Implementado && _evaluador.PasoCompletado(paso, contexto);
                float preparacion = _evaluador.Preparacion(paso, contexto, out int cumplidos, out int totalObligatorios);
                var pasoVm = new GuidePasoViewModel(paso, _textos, requisitos, pasoCompletado, preparacion, cumplidos, totalObligatorios, _servicio);
                pasos.Add(pasoVm);

                // Objetivo actual: el primer paso SIN completar del primer tramo OBLIGATORIO
                // (no opcional) sin completar, implementado, recorriendo por Orden - el camino
                // principal sigue avanzando aunque un tramo opcional este a medias (mismo
                // criterio documentado en TramoGuia.Opcional del propio mod).
                if (objetivoPaso == null && tramo.Implementado && !tramo.Opcional && !pasoCompletado)
                {
                    objetivoPaso = pasoVm;
                    objetivoTramo = null; // se asigna abajo, tras construir el TramoViewModel
                }
            }

            bool tramoCompletado = tramo.Implementado && pasos.Count > 0 && pasos.All(p => p.Completado);
            var tramoVm = new GuideTramoViewModel(tramo, _textos, pasos, tramoCompletado);
            Tramos.Add(tramoVm);

            if (objetivoTramo == null && objetivoPaso != null && pasos.Contains(objetivoPaso))
                objetivoTramo = tramoVm;
        }

        ObjetivoTramo = objetivoTramo;
        ObjetivoPaso = objetivoPaso;
    }

    // Guia Encargo A "GuiaCalamity" (25-sep-2026): mismo patron ya usado por
    // ExplorationViewModel.ResolveModdedChestTileNames - .twld hermano del .wld cargado, NUNCA
    // lanza (un .twld ausente/corrupto/sin DownedBossSystem simplemente no aporta ninguna bandera,
    // jamas rompe la Guia por esto). Devuelve SIEMPRE un set no nulo (vacio si no hay nada que
    // leer) - distinto de null, que GuideContext.CalamityDownedFlags reserva para "sin mundo
    // cargado" (ver Refresh(), que solo llama a esto cuando world != null).
    private static IReadOnlySet<string> ResolveCalamityDownedFlags(string? wldPath)
    {
        if (string.IsNullOrEmpty(wldPath)) return new HashSet<string>();
        string twldPath = Path.ChangeExtension(wldPath, ".twld");
        if (!File.Exists(twldPath)) return new HashSet<string>();

        try
        {
            return TwldReader.ReadCalamityDownedFlags(File.ReadAllBytes(twldPath));
        }
        catch (Exception)
        {
            return new HashSet<string>();
        }
    }
}
