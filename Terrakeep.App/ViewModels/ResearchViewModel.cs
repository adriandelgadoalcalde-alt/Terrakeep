using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Investigacion (Modo Viaje) - antes una unica lista plana de hasta miles de objetos
// (WrapPanel sin categorizar, "es larguisimo" - pedido explicito 2-sep-2026). Reutiliza el
// mismo arbol real de carpetas que la Libreria (LibraryCategoryTreeBuilder - mismo pedido:
// "quiero que calques exactamente la estructura de carpetas... para esta librera Y
// investigacion") para navegar por carpetas en vez de un unico listado - mismos nombres y
// sprites de siempre, solo cambia COMO se navega.
//
// H5-15 (quinta auditoria de Opus): arbol de categorias, busqueda con debounce y el par
// SelectCategory/ClearCategory ya no viven aqui - ver CatalogBrowserViewModel.
//
// H5-02 (quinta auditoria de Opus): "Investigacion es de solo lectura salvo un boton de todo o
// nada... el Terrasavr original SI tiene una rejilla editable con Remove All/Unlock All". Cierra
// la Fase 2 que R-a/R-b (segunda auditoria) dejo aparcada por escrito (bitacora.md:4705).
// ApplyFilter ahora muestra TODO el contenido real de la carpeta elegida (investigado o no),
// con clic para alternar y un campo de conteo editable para parciales - ver ResearchRowViewModel.
public sealed partial class ResearchViewModel : CatalogBrowserViewModel<ResearchRowViewModel>
{
    // H3-06 (tercera auditoria de Opus, Fable): "el tope+debounce medido de verdad en L-c
    // (LibraryViewModel) solo se aplico a esa unica superficie - Investigacion no tenia NINGUN
    // tope (tras 'Investigar todo', elegir una carpeta grande pintaba miles de filas de golpe)
    // NI debounce (reflowaba en cada tecla)". Mismo numero YA medido en produccion (100
    // resultados) - no se remide aqui porque es el MISMO WrapPanel sin virtualizar con el MISMO
    // coste real por tarjeta, no una superficie distinta.
    private const int MaxResults = 100;

    private readonly CharacterFileService _service;
    private Dictionary<int, int> _researchedCounts = new();
    // Universo completo real (vanilla + Calamity) - mismo recorrido real que
    // ResearchAllService.Apply, calculado una vez (no cambia entre personajes, es el catalogo).
    private readonly List<int> _allKnownIds;

    // R-f (segunda auditoria de Opus, Fable): "sin progreso global - el denominador (5.402
    // objetos reales) ya se conoce y no se muestra". Real y dinamico (mismo universo que
    // ResearchAllService.Apply ya recorre - vanilla + Calamity), no un numero fijo que pueda
    // desincronizarse si algun catalogo cambia de tamaño.
    private readonly int _totalKnownObjects;

    // H5-02: "la barra de progreso global pasa de frase a barra real" - 0..1, para un
    // ProgressBar real en el XAML. GlobalProgressSummary es el texto N/Total SIEMPRE global
    // (a diferencia de ResultsSummary, que cambia de significado segun la carpeta/busqueda
    // activa - el tooltip de la barra necesita el total real, no lo que se este mirando ahora).
    [ObservableProperty] private double _progressFraction;
    [ObservableProperty] private string _globalProgressSummary = string.Empty;

    // R-g (segunda auditoria de Opus, Fable): "ninguna advertencia si el personaje no es Modo
    // Viaje - el dato (Appearance.Difficulty) ya esta a mano". La Investigacion (desbloquear
    // recetas) solo tiene efecto real en el juego en Modo Viaje - investigar sin estarlo no
    // sirve de nada aunque la pestaña deje hacerlo igual (no es su trabajo impedirlo, solo
    // avisar).
    [ObservableProperty] private bool _isJourneyMode;

    public ResearchViewModel(CharacterFileService service)
    {
        _service = service;
        ResultsSummary = LocalizationService.Instance["status_no_character_loaded_dot"];
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);
        // Auditoria de Opus, T-18: cada nodo lleva su propio comando real - ver el comentario
        // real en CategoryNodeViewModel.SelectCommand.
        CategoryNodeViewModel.AssignSelectCommand(RootCategories, SelectCategoryCommand);

        // Mismo recorrido real que ResearchAllService.Apply (vanilla con id real resoluble +
        // TODOS los objetos de Calamity) - calculado una vez, reutilizado para el total Y para
        // la busqueda global sin carpeta (antes solo buscaba en lo YA investigado).
        _allKnownIds = [];
        foreach (var pid in service.VanillaCatalog.AllInternalNames())
        {
            int? id = service.VanillaCatalog.GetIdByKey(pid);
            if (id.HasValue) _allKnownIds.Add(id.Value);
        }
        foreach (var entry in service.CalamityCatalog.Entries) _allKnownIds.Add(entry.SyntheticId);
        _totalKnownObjects = _allKnownIds.Count;
    }

    public void LoadFrom(PlrCharacter character)
    {
        Reset();
        _researchedCounts = ResolveResearchedCounts(character);
        RefreshJourneyMode(character.Difficulty);
        ApplyFilter();
    }

    // Oleada del 6-sep-2026 (Personaje > Apariencia/Investigacion) - BUG REAL: esto se calculaba
    // UNA sola vez, en LoadFrom, y no volvia a mirar la dificultad nunca mas. La dificultad se
    // edita en Apariencia, dos sub-pestañas al lado: poner el personaje en Modo Viaje dejaba a
    // Investigacion diciendo que no lo era (y al reves). MainViewModel, que conoce a los dos,
    // lo empuja en cuanto cambia - misma familia exacta que el arreglo de la rejilla de Buffs
    // por version de esta misma oleada. El 3 es Player.difficulty real de Modo Viaje.
    public void RefreshJourneyMode(int difficulty) => IsJourneyMode = difficulty == 3;

    // H5-02: vuelca el estado real en memoria de vuelta al personaje - mismo criterio real que
    // MainViewModel.SyncEditsBackToMerged para objetos, llamado desde MainViewModel.Save()
    // justo antes de guardar. Un id que ya no resuelve a ningun Pid real (catalogo cambiado
    // entre sesiones) se descarta en silencio - "lo que no se encuentra no se inventa".
    public void SyncBackTo(PlrCharacter character)
    {
        character.Research.Clear();
        foreach (var (id, count) in _researchedCounts)
        {
            string? pid = ResolvePid(id);
            if (pid != null) character.Research.Add(new PlrResearchEntry { Pid = pid, Count = count });
        }
    }

    private string? ResolvePid(int id)
    {
        if (id >= Core.Calamity.CalamityIds.ItemIdBase)
        {
            var entry = _service.CalamityCatalog.BySyntheticId(id);
            return entry != null ? $"{entry.Mod}/{entry.Internal}" : null;
        }
        return _service.VanillaCatalog.GetKeyById(id);
    }

    // Sin personaje cargado (o al recargar uno nuevo) - misma limpieza de seleccion que
    // MainViewModel.RebuildContainers ya hacia con Containers/EquipmentGroup.
    public void Reset()
    {
        if (SelectedCategory != null) { SelectedCategory.IsSelected = false; SelectedCategory = null; }
        _researchedCounts = new Dictionary<int, int>();
        Results.Clear();
        ResultsSummary = LocalizationService.Instance["status_no_character_loaded_dot"];
        IsJourneyMode = false;
        ProgressFraction = 0;
    }

    // Misma resolucion de Pid real que ya usaba MainViewModel.RebuildResearch: un Pid con "/"
    // es de Calamity (mod/internal), si no es el nombre interno vanilla real (nunca lleva "/").
    private Dictionary<int, int> ResolveResearchedCounts(PlrCharacter character)
    {
        var counts = new Dictionary<int, int>();
        foreach (var entry in character.Research)
        {
            int? id = entry.Pid.Contains('/')
                ? ResolveCalamityId(entry.Pid)
                : _service.VanillaCatalog.GetIdByKey(entry.Pid);
            if (id.HasValue) counts[id.Value] = entry.Count;
        }
        return counts;
    }

    private int? ResolveCalamityId(string pid)
    {
        int slash = pid.IndexOf('/');
        string mod = pid[..slash], internalName = pid[(slash + 1)..];
        return _service.CalamityCatalog.ByModAndInternal(mod, internalName)?.SyntheticId;
    }

    private (string DisplayName, string? IconPath, bool IsCalamity, int? RequiredCount) ResolveDisplay(int id)
    {
        var calEntry = _service.CalamityCatalog.BySyntheticId(id);
        if (calEntry != null)
        {
            string? iconPath = calEntry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + calEntry.Icon : null;
            return (calEntry.DisplayName ?? $"Calamity #{id}", iconPath, true, null);
        }
        return (_service.VanillaCatalog.GetName(id), VanillaIconResolver.GetIconPath(id), false, _service.VanillaResearchCounts.Get(id));
    }

    // El conteo real que representa "investigado del todo" para este id - mismo criterio real
    // ya usado en ResearchAllService.Apply (umbral real vanilla, o el placeholder de Calamity
    // sin tabla real extraida).
    private int FullResearchCount(int id) => _service.VanillaResearchCounts.Get(id) ?? ResearchAllService.PlaceholderCount;

    protected override void ApplyFilter()
    {
        Results.Clear();
        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);

        if (SelectedCategory == null && !hasSearch)
        {
            RefreshSummaryAndProgress();
            // H4-07 punto 3 (cuarta auditoria de Opus, Fable): "el resumen de Investigacion sin
            // carpeta podria enseñar las carpetas raiz como tarjetas grandes en el area vacia,
            // en vez de solo una frase" - el area de resultados se quedaba en blanco salvo por
            // ResultsSummary (una sola linea de texto) mientras a la izquierda ya esta el arbol
            // completo esperando un clic - las tarjetas son solo un atajo mas grande al MISMO
            // arbol, RootCategories ya trae su propio SelectCommand real.
            // H5-13 (quinta auditoria de Opus): ShowRootCategoryCards subio a la base compartida
            // (CatalogBrowserViewModel) - ya se computa sola a partir de SelectedCategory/
            // SearchText, no hace falta alternarla aqui a mano.
            return;
        }

        // H5-02: la carpeta elegida muestra TODO su contenido real (investigado o no) - antes
        // solo lo que ya estaba en _researchedCounts. Mismo bug real ya corregido en
        // LibraryViewModel.ApplyFilter: ItemIdsOrdered respeta el orden curado real de
        // Terrasavr, un HashSet/OrderBy(id) numerico no.
        IEnumerable<int> candidates = SelectedCategory != null ? SelectedCategory.ItemIdsOrdered : _allKnownIds;

        var matches = new List<ResearchRowViewModel>();
        foreach (int id in candidates)
        {
            var (displayName, iconPath, isCalamity, requiredCount) = ResolveDisplay(id);
            // C-09 (informe de pulido final, cierra L2): Fold en vez de ToLowerInvariant a secas.
            if (hasSearch && !LibrarySearchGrammar.Matches(SearchText, id, LibrarySearchGrammar.Fold(displayName), null)) continue;
            int count = _researchedCounts.GetValueOrDefault(id);
            var row = new ResearchRowViewModel(id, displayName, count, requiredCount, isCalamity, iconPath);
            row.CountChangedByUser += OnRowCountChangedByUser;
            matches.Add(row);
        }
        // H3-06: mismo tope real ya medido en LibraryViewModel (100 - WrapPanel sin
        // virtualizar, 300 tarjetas = 802ms de congelacion real) - una carpeta grande (ej.
        // "Calamity (mod)", miles de objetos) ya no pinta todo de golpe.
        foreach (var row in matches.Take(MaxResults)) Results.Add(row);

        string categoryLabel = SelectedCategory != null ? LocalizationService.Instance.Format("library_summary_in_category", SelectedCategory.Name) : string.Empty;
        ResultsSummary = matches.Count > MaxResults
            ? LocalizationService.Instance.Format("research_summary_showing", MaxResults, matches.Count, categoryLabel)
            : LocalizationService.Instance.Format("research_summary_count", matches.Count, categoryLabel);
        RefreshProgressOnly();
    }

    // H5-02: clic en una fila (alterna) o el campo de conteo editable (parcial a mano) - ambos
    // caminos reales pasan por Count, ver ResearchRowViewModel.OnCountChanged. Actualiza el
    // estado real SIN reconstruir Results entero (100 filas de sobra por cada tecla del campo
    // de conteo seria un reflow real innecesario).
    private void OnRowCountChangedByUser(ResearchRowViewModel row, int newCount)
    {
        if (newCount <= 0) _researchedCounts.Remove(row.Id);
        else _researchedCounts[row.Id] = newCount;
        RefreshSummaryAndProgress();
        // H5-01 (nota real de alcance, ya documentada en bitacora.md): la investigacion no
        // participa todavia del UndoStack general (dato de forma distinta a un slot de objeto) -
        // esto SI marca el personaje como editado. Evento DEDICADO (no PropertyChanged generico
        // de todo el ViewModel, que tambien dispara con solo navegar/buscar - marcar dirty por
        // abrir una carpeta seria un falso positivo real) - mismo patron ya establecido
        // (ServersViewModel.Changed/BuffsViewModel.SlotChanged), MainViewModel.MarkDirty se
        // suscribe una vez en el constructor.
        ResearchChanged?.Invoke();
    }

    public event Action? ResearchChanged;

    // Oleada del 6-sep-2026 (Personaje > Investigacion/Version): cuantos objetos hay
    // investigados AHORA MISMO, incluidas las ediciones sin guardar - character.Research solo
    // se reescribe en SyncBackTo (al guardar), asi que es el unico dato fiable para el aviso de
    // bajada de version por debajo de 200, donde la investigacion entera deja de escribirse.
    public int ResearchedCount => _researchedCounts.Count;

    [RelayCommand]
    private void ToggleRow(ResearchRowViewModel row)
    {
        row.Count = row.IsResearched ? 0 : FullResearchCount(row.Id);
    }

    // H5-02: "dos acciones por carpeta - Investigar esta carpeta/Quitar - junto a las dos
    // globales". Investigar SOLO sube lo que falta (nunca baja un conteo parcial real ya mas
    // alto que el umbral, mismo criterio ya establecido en ResearchAllService.Apply).
    [RelayCommand]
    private void ResearchFolder()
    {
        if (SelectedCategory == null) return;
        foreach (int id in SelectedCategory.ItemIdsOrdered)
        {
            int full = FullResearchCount(id);
            if (_researchedCounts.GetValueOrDefault(id) < full) _researchedCounts[id] = full;
        }
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearFolder()
    {
        if (SelectedCategory == null) return;
        foreach (int id in SelectedCategory.ItemIdsOrdered) _researchedCounts.Remove(id);
        ApplyFilter();
    }

    // H5-02: "Quitar toda la investigacion" - el equivalente real del "Remove All" del
    // Terrasavr original (app.TabResearch), que hoy no existia en absoluto.
    [RelayCommand]
    private void ClearAllResearch()
    {
        _researchedCounts.Clear();
        ApplyFilter();
    }

    private void RefreshSummaryAndProgress()
    {
        ResultsSummary = LocalizationService.Instance.Format("research_summary_all", _researchedCounts.Count, _totalKnownObjects);
        RefreshProgressOnly();
    }

    private void RefreshProgressOnly()
    {
        ProgressFraction = _totalKnownObjects > 0 ? _researchedCounts.Count / (double)_totalKnownObjects : 0;
        GlobalProgressSummary = LocalizationService.Instance.Format("research_progress_all", _researchedCounts.Count, _totalKnownObjects);
    }
}
