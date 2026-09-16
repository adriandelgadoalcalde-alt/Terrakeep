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
        bool completado, float preparacion, int cumplidos, int totalObligatorios)
    {
        _paso = paso;
        _textos = textos;
        Requisitos = requisitos;
        Completado = completado;
        PreparacionPct = (int)Math.Round(preparacion * 100.0);
        Cumplidos = cumplidos;
        TotalObligatorios = totalObligatorios;
        PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    public IReadOnlyList<GuideRequisitoViewModel> Requisitos { get; }
    public bool Completado { get; }
    public string Icono => Completado ? "✓" : "○";
    public int PreparacionPct { get; }
    public int Cumplidos { get; }
    public int TotalObligatorios { get; }

    public string Titulo => _textos.Text("Guia.Paso." + _paso.Clave + ".Titulo", LocalizationService.Instance.Language);
    public string Porque => _textos.Text("Guia.Paso." + _paso.Clave + ".Porque", LocalizationService.Instance.Language);
    public string Como => _textos.Text("Guia.Paso." + _paso.Clave + ".Como", LocalizationService.Instance.Language);
    public string ZonaLegible => string.IsNullOrEmpty(_paso.Zona)
        ? "" : _textos.Text("Guia.Zona." + _paso.Zona, LocalizationService.Instance.Language);

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Titulo));
        OnPropertyChanged(nameof(Porque));
        OnPropertyChanged(nameof(Como));
        OnPropertyChanged(nameof(ZonaLegible));
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

    public GuideViewModel(CharacterFileService servicio, Func<LoadedCharacter?> character, Func<WldWorld?> world, Func<bool> hasCalamity)
    {
        _character = character;
        _world = world;
        _hasCalamity = hasCalamity;

        string assetsGuia = Path.Combine(AppContext.BaseDirectory, "Assets", "guia");
        _catalogo = GuideCatalog.LoadFromFile(Path.Combine(assetsGuia, "guia_progresion.json"), servicio.CalamityCatalog);
        _textos = GuideTextCatalog.LoadFromFiles(Path.Combine(assetsGuia, "textos.es.json"), Path.Combine(assetsGuia, "textos.en.json"));
        _evaluador = new GuideEvaluator(servicio.VanillaCatalog, servicio.NpcNames, servicio.CalamityCatalog);
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

    public string TextoAvisoSinPersonaje => LocalizationService.Instance["guide_no_character_notice"];

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
        };

        HasAnyData = loaded != null || world != null;
        MostrarAvisoCalamity = hasCalamity;
        MostrarAvisoSinPersonaje = world != null && loaded == null;

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
                var pasoVm = new GuidePasoViewModel(paso, _textos, requisitos, pasoCompletado, preparacion, cumplidos, totalObligatorios);
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
}
