using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Apariencia" - estilo de pelo, genero, tinte de pelo, los 7 colores reales del
// personaje (pelo/piel/ojos/camisa/camiseta interior/pantalones/zapatos) y las estadisticas
// del personaje (dificultad, vida/mana, pesca, golf, horas jugadas) - todos campos que
// PlrCharacter ya traia leidos desde la Fase 1 pero sin ningun panel para verlos/editarlos.
// Mismo agrupamiento que la version JS real (app.TabMain/Sa cubre appearance+stats en un
// unico panel, confirmado en la auditoria de bitacora.md). PreviewImage se recalcula en cada
// cambio (ver PlayerPreviewRenderer para el detalle real del atlas/tintado).
public partial class AppearanceViewModel : ObservableObject
{
    private readonly CharacterFileService _service;
    // C-15 (informe de pulido final, cierra A1): callback real hacia el UndoStack compartido de
    // MainViewModel (mismo criterio ya establecido - ExplorationViewModel/BuffsViewModel reciben
    // un Action<T> en vez de la propia MainViewModel entera) y consulta de si un Deshacer/Rehacer
    // esta en curso AHORA MISMO (MainViewModel._suppressUndoRecording real, no una copia local -
    // ver el comentario de PushUndoDebounced sobre por que la entrada con debounce necesita
    // consultarlo en el momento del intento de empujar, no antes).
    private readonly Action<UndoEntry> _pushUndo;
    private readonly Func<bool> _isUndoRedoInProgress;
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    [ObservableProperty] private WriteableBitmap? _previewImage;

    // H6-06 (sexta auditoria de Opus, Tanda D - "unificar el doll de Apariencia con el de
    // Inicio, que YA muestra la armadura/vanidad real puesta"): true por defecto (mismo
    // criterio que la tarjeta de Inicio, fiel al guardado real) - "Ver sin equipo" lo apaga
    // para ver solo piel/pelo/colores base, util para comparar tintes sin la ropa puesta de
    // por medio.
    [ObservableProperty] private bool _showEquipment = true;
    partial void OnShowEquipmentChanged(bool value) => RefreshPreview();

    [ObservableProperty] private int _hairStyle;
    [ObservableProperty] private int _hairDye;
    [ObservableProperty] private string _hairDyeDisplayName = LocalizationService.Instance["hair_dye_none"];

    public AppearanceViewModel(CharacterFileService service, Action<UndoEntry> pushUndo, Func<bool> isUndoRedoInProgress)
    {
        _service = service;
        _pushUndo = pushUndo;
        _isUndoRedoInProgress = isUndoRedoInProgress;
        BuildHairDyeOptions();
        _hairOptionsDebounceTimer.Tick += (_, _) =>
        {
            _hairOptionsDebounceTimer.Stop();
            if (IsHairPickerOpen) RebuildHairOptions();
        };
    }
    // Convencion vanilla estandar (Player.Male en Terraria): true = chico. La version binaria
    // invertida documentada en el proyecto es de formatos MUY antiguos (version<145), fuera
    // del alcance de este lector (ver PlrCharacter.cs).
    [ObservableProperty] private bool _isMale = true;

    // Espejo de IsMale para el RadioButton "Chica" - RadioButtons enlazados por dos vias
    // necesitan cada uno su propia propiedad bool, no hay forma limpia de enlazar dos al
    // mismo bool con sentido opuesto sin esto o un converter dedicado.
    public bool IsFemale
    {
        get => !IsMale;
        set => IsMale = !value;
    }

    public ObservableCollection<ColorSwatchViewModel> Swatches { get; } = [];

    // Selector visual de peinado (pedido explicito 1-sep-2026: sprites en vez de escribir un
    // id a mano) - las 228 miniaturas se generan bajo demanda al abrir el selector (no en
    // LoadFrom, para no pagar 228 renders en cada carga de personaje si nunca se abre) con el
    // color de pelo actual en ese momento.
    //
    // Ap-b (segunda auditoria de Opus, Fable): "228 miniaturas se regeneran en cada tick del
    // color - medir antes de tocar nada" (mismo criterio que X-7/L-c). Medido de verdad con el
    // arnes UIA: 228 miniaturas reales -> 113ms (Debug, primera pasada) - nada despreciable,
    // regenerarlas en CADA tick de un arrastre de slider (que puede disparar docenas de
    // eventos por segundo) congelaria la UI de verdad. _hairOptionsStale (marca barata, sin
    // coste real) + _hairOptionsDebounceTimer (mismo patron ya establecido en el proyecto -
    // LibraryViewModel._searchDebounceTimer/MainViewModel._saveConfirmationTimer): el color
    // puede cambiar con el selector cerrado sin coste real (solo se marca obsoleto, sin limpiar
    // ni regenerar nada todavia); si el selector esta ABIERTO cuando cambia el color, se
    // regeneran de verdad pero solo UNA vez, 180ms despues del ultimo cambio.
    [ObservableProperty] private bool _isHairPickerOpen;
    public ObservableCollection<HairOptionViewModel> HairOptions { get; } = [];
    private bool _hairOptionsStale = true;
    private readonly DispatcherTimer _hairOptionsDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    private void RebuildHairOptions()
    {
        if (Swatches.Count <= HairIdx) return;
        HairOptions.Clear();
        var hairColor = new PlayerPreviewRenderer.Tint((byte)Swatches[HairIdx].R, (byte)Swatches[HairIdx].G, (byte)Swatches[HairIdx].B);
        // H6-04 (Opus, sexta pasada): HairStyle real es 0-based (Player.hair, ver
        // PlayerPreviewRenderer) - el bucle YA NO empieza en 1.
        for (int id = 0; id < PlayerPreviewRenderer.HairStyleCount; id++)
            HairOptions.Add(new HairOptionViewModel(id, PlayerPreviewRenderer.RenderHairThumbnail(id, hairColor)));
        _hairOptionsStale = false;
    }

    [RelayCommand]
    private void OpenHairPicker()
    {
        if (_hairOptionsStale) RebuildHairOptions();
        // Ap-a (segunda auditoria de Opus, Fable): "los selectores de peinado/tinte abiertos a
        // la vez empujan el contenido" - ninguno de los dos cerraba al otro, asi que se podian
        // abrir los dos juntos (ambos paneles son inline, no popups reales - ver el resto del
        // proyecto sobre por que se evitan Popups) y el layout se estiraba de mas.
        IsHairDyePickerOpen = false;
        IsHairPickerOpen = true;
    }

    [RelayCommand]
    private void SelectHair(int id)
    {
        HairStyle = id;
        IsHairPickerOpen = false;
    }

    [RelayCommand]
    private void CloseHairPicker() => IsHairPickerOpen = false;

    // Selector visual de tinte de pelo (pedido explicito 1-sep-2026: "Apariencia sigue
    // siendo por ID" referido en concreto al tinte, ya que el peinado arriba SI tenia
    // selector visual) - 12 tintes reales extraidos de DyeInitializer.cs
    // (scripts/extraer-tintes-pelo.py) mas "Ninguno" (indice 0). A diferencia del peinado no
    // hace falta renderizar nada bajo demanda: son sprites de objetos reales del catalogo
    // vanilla, ya resueltos una vez en el constructor.
    [ObservableProperty] private bool _isHairDyePickerOpen;
    public ObservableCollection<HairDyeOptionViewModel> HairDyeOptions { get; } = [];

    private void BuildHairDyeOptions()
    {
        HairDyeOptions.Add(new HairDyeOptionViewModel(0, LocalizationService.Instance["hair_dye_none"], null));
        foreach (var entry in _service.HairDyes.Entries)
        {
            string name = _service.VanillaCatalog.GetName(entry.ItemId);
            string? icon = VanillaIconResolver.GetIconPath(entry.ItemId);
            HairDyeOptions.Add(new HairDyeOptionViewModel(entry.Index, name, icon));
        }
    }

    [RelayCommand]
    private void OpenHairDyePicker()
    {
        // Ap-a: mismo criterio que OpenHairPicker de arriba, en el otro sentido.
        IsHairPickerOpen = false;
        IsHairDyePickerOpen = true;
    }

    [RelayCommand]
    private void SelectHairDye(int index)
    {
        HairDye = index;
        IsHairDyePickerOpen = false;
    }

    [RelayCommand]
    private void CloseHairDyePicker() => IsHairDyePickerOpen = false;

    // Estadisticas del personaje - en la version JS real viven en el mismo panel que
    // pelo/colores (app.TabMain, clase Sa, mismo constructor que ya se investigo para el
    // preview), asi que se quedan aqui en vez de en una pestaña aparte. Todos campos que
    // PlrCharacter ya traia leidos/escritos, solo faltaba UI (ver bitacora.md, auditoria
    // Terrasavr JS vs puerto).
    public string[] DifficultyLabels { get; } = ["Softcore", "Mediumcore", "Hardcore", "Journey"];

    // Auditoria de Opus, N-1: la cabecera global (MainWindow.xaml) necesita el texto de la
    // dificultad sin poder indexar DifficultyLabels[Difficulty] a mano en XAML - un unico sitio
    // real, reutilizado tambien por el propio selector de Apariencia si hiciera falta.
    public string DifficultyLabel => DifficultyLabels[Math.Clamp(Difficulty, 0, 3)];

    [ObservableProperty] private int _difficulty;
    [ObservableProperty] private int _healthNow;
    [ObservableProperty] private int _healthMax;
    [ObservableProperty] private int _manaNow;
    [ObservableProperty] private int _manaMax;
    [ObservableProperty] private int _fishingQuestsCompleted;
    [ObservableProperty] private int _golferScore;
    // Horas jugadas, editable - PlrCharacter solo guarda PlayTimeLow/PlayTimeHigh (dos UInt32
    // que juntos forman un tick count de 64 bits, 10 millones de ticks/segundo - EXACTAMENTE
    // la resolucion de System.TimeSpan.Ticks, confirmado leyendo el real
    // script.readable.js: "there are 10 million 'ticks' in one second" mas la formula real de
    // guardado/carga con el mismo divisor 1E7). Se usa TimeSpan/aritmetica entera de 64 bits
    // en vez de replicar la formula en coma flotante del original (que tiene perdida de
    // precision real, 429.4967295 en vez de 429.4967296 = 2^32/1E7 exacto) - mismo criterio
    // que ya se aplico a los campos LONG del NBT en Fase 1 (blob opaco en JS por no tener
    // enteros de 64 bits nativos; aqui SI los hay, se usan).
    [ObservableProperty] private double _playHours;

    public void LoadFrom(PlrCharacter character)
    {
        _character = null; // evita que los Add() de abajo disparen escrituras a medio construir
        // C-15: el historial de deshacer de un personaje no tiene sentido real sobre otro (mismo
        // criterio ya establecido para UndoStack.Clear al cambiar de personaje en MainViewModel)
        // - cualquier grupo con debounce pendiente de un personaje ANTERIOR se descarta entero,
        // nunca se deja disparar contra el nuevo.
        foreach (var pending in _pendingUndoGroups.Values) pending.Timer.Stop();
        _pendingUndoGroups.Clear();
        _swatchBaseline.Clear();
        Swatches.Clear();
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_hair"], character.HairColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_skin"], character.SkinColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_eyes"], character.EyeColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_shirt"], character.ShirtColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_undershirt"], character.UnderColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_pants"], character.PantsColor));
        Swatches.Add(new ColorSwatchViewModel(LocalizationService.Instance["swatch_shoes"], character.ShoesColor));

        _suppressWriteback = true;
        HairStyle = character.HairStyle;
        HairDye = character.HairDye;
        // H6-02 (Opus, sexta pasada): Gender es el skinVariant real del juego (0-11), no un
        // booleano - ver TerrasavrNative.Core.Model.PlayerVariantSets.
        IsMale = TerrasavrNative.Core.Model.PlayerVariantSets.IsMale(character.Gender);
        Difficulty = character.Difficulty;
        HealthNow = character.HealthNow;
        HealthMax = character.HealthMax;
        ManaNow = character.ManaNow;
        ManaMax = character.ManaMax;
        FishingQuestsCompleted = character.FishingQuestsCompleted;
        GolferScore = character.GolferScore;
        long totalTicks = (long)(((ulong)character.PlayTimeHigh << 32) | character.PlayTimeLow);
        PlayHours = TimeSpan.FromTicks(totalTicks).TotalHours;
        _suppressWriteback = false;

        for (int i = 0; i < Swatches.Count; i++)
        {
            var swatch = Swatches[i];
            int swatchIndex = i; // captura real por valor - "i" es la variable de bucle compartida
            swatch.PropertyChanged += (_, _) => RefreshPreview();
            // C-15 (informe de pulido final, gotcha real #2): los 7 colores NO pasan por
            // AppearanceViewModel - los edita ColorSwatchViewModel directamente sobre el byte[]
            // real del personaje, sin ningun "oldValue" propio que capturar (a diferencia de
            // HairStyle/HealthMax/etc, que si tienen su overload de dos parametros generado por
            // el propio ObservableProperty). _swatchBaseline guarda el (R,G,B) de ANTES del
            // gesto en curso - se congela en la primera llamada de la rafaga (PushUndoDebounced
            // ya lo hace por dentro) y se refresca solo cuando el grupo de verdad se resuelve
            // (onFlushed), listo para el PROXIMO gesto futuro.
            _swatchBaseline[swatchIndex] = (swatch.R, swatch.G, swatch.B);
            swatch.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is not (nameof(ColorSwatchViewModel.R) or nameof(ColorSwatchViewModel.G) or nameof(ColorSwatchViewModel.B))) return;
                string key = $"swatch{swatchIndex}";
                var before = _swatchBaseline[swatchIndex];
                var after = (swatch.R, swatch.G, swatch.B);
                PushUndoDebounced(key, LocalizationService.Instance.Format("undo_appearance_color", swatch.Label), before, after,
                    v => { swatch.R = v.R; swatch.G = v.G; swatch.B = v.B; },
                    onFlushed: () => _swatchBaseline[swatchIndex] = after);
                // Si PushUndoDebounced NO creo/mantuvo un grupo real (replay en curso, o
                // _suppressWriteback/_character==null) el valor real del swatch YA cambio de
                // todos modos - "before" tiene que reflejarlo AHORA, o la PROXIMA rafaga real
                // partiria de un valor obsoleto (bug real encontrado escribiendo este mismo
                // arreglo: Deshacer un color y luego editarlo de nuevo empujaba una entrada que
                // "antes" apuntaba al valor de DOS cambios atras, no al inmediatamente anterior).
                if (!_pendingUndoGroups.ContainsKey(key)) _swatchBaseline[swatchIndex] = after;
            };
            // Ap-b: color de pelo cambio, las miniaturas quedan obsoletas - marca barata
            // (_hairOptionsStale) en vez de limpiar/regenerar aqui mismo; si el selector esta
            // abierto AHORA, ademas reinicia el debounce real (180ms) para refrescarlas de
            // verdad sin regenerar en cada tick individual del slider.
            if (i == HairIdx) swatch.PropertyChanged += (_, _) =>
            {
                _hairOptionsStale = true;
                if (IsHairPickerOpen)
                {
                    _hairOptionsDebounceTimer.Stop();
                    _hairOptionsDebounceTimer.Start();
                }
            };
            // Segunda auditoria de Opus (Fable), B-5/nota - MainViewModel marca "sin guardar"
            // suscribiendose a Appearance.PropertyChanged a secas; un cambio de color solo
            // llegaba ahi POR CASUALIDAD (RefreshPreview reasigna PreviewImage, que si es
            // [ObservableProperty] de esta clase) - si algun dia el preview se optimiza para no
            // reasignar el bitmap en cada tick, este seria el tercer agujero silencioso de
            // guardado (mismo tipo que B-4/B-5). Señal EXPLICITA, no accidental - misma
            // propiedad publica real (Swatches), sin inventar un evento nuevo solo para esto.
            swatch.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Swatches));
        }
        HairOptions.Clear();
        _hairOptionsStale = true;
        // Ap-a: un personaje nuevo cierra cualquier selector que hubiera quedado abierto del
        // anterior (evita mostrar opciones/miniaturas de un personaje distinto ya descartado).
        IsHairPickerOpen = false;
        IsHairDyePickerOpen = false;

        _character = character;
        RefreshPreview();
    }

    partial void OnHairStyleChanged(int value)
    {
        RefreshPreview();
        if (_suppressWriteback || _character == null) return;
        _character.HairStyle = value;
    }
    // C-15: cambio discreto (SelectHair, un clic en el selector visual) - sin debounce.
    partial void OnHairStyleChanged(int oldValue, int newValue) => PushUndo(LocalizationService.Instance["undo_appearance_hairstyle"], oldValue, newValue, v => HairStyle = v);

    partial void OnHairDyeChanged(int value)
    {
        HairDyeDisplayName = HairDyeOptions.FirstOrDefault(o => o.Index == value)?.DisplayName ?? $"Tinte #{value}";
        if (_suppressWriteback || _character == null) return;
        _character.HairDye = (byte)Math.Clamp(value, 0, 255);
    }
    partial void OnHairDyeChanged(int oldValue, int newValue) => PushUndo(LocalizationService.Instance["undo_appearance_hairdye"], oldValue, newValue, v => HairDye = v);

    // H6-02 (Opus, sexta pasada): un cambio REAL de genero (el usuario toca el selector, no una
    // carga silenciosa) colapsa a la variante "Starter" real de ese genero - el selector de la
    // app es deliberadamente binario, sin las 10 variantes de vestuario alternativo (ver
    // PlayerVariantSets). Si el .plr ya traia una variante alternativa del MISMO genero, se
    // conserva intacta (este metodo solo dispara con un cambio real de valor).
    partial void OnIsMaleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFemale));
        RefreshPreview();
        if (_suppressWriteback || _character == null) return;
        _character.Gender = value ? TerrasavrNative.Core.Model.PlayerVariantSets.MaleStarter : TerrasavrNative.Core.Model.PlayerVariantSets.FemaleStarter;
    }
    partial void OnIsMaleChanged(bool oldValue, bool newValue) => PushUndo(LocalizationService.Instance["undo_appearance_gender"], oldValue, newValue, v => IsMale = v);

    partial void OnDifficultyChanged(int value)
    {
        OnPropertyChanged(nameof(DifficultyLabel));
        if (_suppressWriteback || _character == null) return;
        _character.Difficulty = (byte)Math.Clamp(value, 0, 3);
    }
    partial void OnDifficultyChanged(int oldValue, int newValue) => PushUndo(LocalizationService.Instance["undo_appearance_difficulty"], oldValue, newValue, v => Difficulty = v);

    // Ap-f (segunda auditoria de Opus, Fable): "se puede poner HealthNow=500/HealthMax=100; el
    // juego lo recorta, Terrakeep no". El recorte/arrastre SOLO se aplica fuera de la carga
    // (_suppressWriteback) - durante LoadFrom, HealthNow se asigna ANTES que HealthMax (arriba),
    // recortar contra un HealthMax todavia sin poner (0 o el del personaje anterior)
    // corromperia el dato real que se esta cargando; mismo motivo real por el que el resto de
    // OnXxxChanged de aqui ya usan este mismo guardia. Mismo bug real y mismo arreglo en Mana
    // (el hallazgo original no lo menciona, pero es el mismo par exacto de campos).
    // H5-10 (quinta auditoria de Opus): HealthFraction/HealthLabel/ManaFraction/ManaLabel para
    // la franja de constantes vitales de la cabecera - avisar SIEMPRE, incluso durante LoadFrom
    // (_suppressWriteback=true), a diferencia de la escritura hacia PlrCharacter de abajo: son
    // solo lectura derivada, no hay nada que corromper, y si no avisan aqui la cabecera se queda
    // con la barra del personaje anterior tras cargar uno nuevo.
    partial void OnHealthNowChanged(int value)
    {
        OnPropertyChanged(nameof(HealthFraction));
        OnPropertyChanged(nameof(HealthLabel));
        if (_suppressWriteback || _character == null) return;
        int clamped = Math.Clamp(value, 0, HealthMax);
        if (clamped != value) { HealthNow = clamped; return; } // reentra, se estabiliza al segundo paso
        _character.HealthNow = value;
    }
    // C-15: TextBox con UpdateSourceTrigger=PropertyChanged - cada caracter tecleado dispara un
    // cambio real, PushUndoDebounced agrupa el gesto completo en una unica entrada.
    partial void OnHealthNowChanged(int oldValue, int newValue) => PushUndoDebounced("HealthNow", LocalizationService.Instance["undo_appearance_health_now"], oldValue, newValue, v => HealthNow = v);
    partial void OnHealthMaxChanged(int value)
    {
        OnPropertyChanged(nameof(HealthFraction));
        OnPropertyChanged(nameof(HealthLabel));
        if (_suppressWriteback || _character == null) return;
        if (HealthNow > value) HealthNow = value; // arrastra el actual hacia abajo si el maximo baja por debajo
        _character.HealthMax = value;
    }
    partial void OnHealthMaxChanged(int oldValue, int newValue) => PushUndoDebounced("HealthMax", LocalizationService.Instance["undo_appearance_health_max"], oldValue, newValue, v => HealthMax = v);
    partial void OnManaNowChanged(int value)
    {
        OnPropertyChanged(nameof(ManaFraction));
        OnPropertyChanged(nameof(ManaLabel));
        if (_suppressWriteback || _character == null) return;
        int clamped = Math.Clamp(value, 0, ManaMax);
        if (clamped != value) { ManaNow = clamped; return; }
        _character.ManaNow = value;
    }
    partial void OnManaNowChanged(int oldValue, int newValue) => PushUndoDebounced("ManaNow", LocalizationService.Instance["undo_appearance_mana_now"], oldValue, newValue, v => ManaNow = v);
    partial void OnManaMaxChanged(int value)
    {
        OnPropertyChanged(nameof(ManaFraction));
        OnPropertyChanged(nameof(ManaLabel));
        if (_suppressWriteback || _character == null) return;
        if (ManaNow > value) ManaNow = value;
        _character.ManaMax = value;
    }
    partial void OnManaMaxChanged(int oldValue, int newValue) => PushUndoDebounced("ManaMax", LocalizationService.Instance["undo_appearance_mana_max"], oldValue, newValue, v => ManaMax = v);

    /// <summary>Fraccion 0..1 real de vida actual/maxima - 0 si HealthMax es 0 (personaje sin cargar).</summary>
    public double HealthFraction => HealthMax > 0 ? Math.Clamp(HealthNow / (double)HealthMax, 0.0, 1.0) : 0.0;
    public string HealthLabel => $"{HealthNow}/{HealthMax}";
    public double ManaFraction => ManaMax > 0 ? Math.Clamp(ManaNow / (double)ManaMax, 0.0, 1.0) : 0.0;
    public string ManaLabel => $"{ManaNow}/{ManaMax}";
    partial void OnFishingQuestsCompletedChanged(int value) { if (!_suppressWriteback && _character != null) _character.FishingQuestsCompleted = value; }
    partial void OnFishingQuestsCompletedChanged(int oldValue, int newValue) => PushUndoDebounced("FishingQuestsCompleted", LocalizationService.Instance["undo_appearance_fishing_quests"], oldValue, newValue, v => FishingQuestsCompleted = v);
    partial void OnGolferScoreChanged(int value) { if (!_suppressWriteback && _character != null) _character.GolferScore = value; }
    partial void OnGolferScoreChanged(int oldValue, int newValue) => PushUndoDebounced("GolferScore", LocalizationService.Instance["undo_appearance_golf_score"], oldValue, newValue, v => GolferScore = v);

    partial void OnPlayHoursChanged(double value)
    {
        if (_suppressWriteback || _character == null) return;
        long ticks = TimeSpan.FromHours(Math.Max(0, value)).Ticks;
        _character.PlayTimeLow = unchecked((uint)ticks);
        _character.PlayTimeHigh = unchecked((uint)(ticks >> 32));
    }
    partial void OnPlayHoursChanged(double oldValue, double newValue) => PushUndoDebounced("PlayHours", LocalizationService.Instance["undo_appearance_play_hours"], oldValue, newValue, v => PlayHours = v);

    // Indices en Swatches, mismo orden en que se anaden arriba en LoadFrom.
    private const int HairIdx = 0, SkinIdx = 1, EyesIdx = 2, ShirtIdx = 3, UnderIdx = 4, PantsIdx = 5, ShoesIdx = 6;

    // H6-06: la armadura/vanidad REAL puesta en este momento - PrimaryLoadout NO sirve aqui
    // (solo se sincroniza con lo que el usuario edita en Equipamiento al GUARDAR, ver
    // CharacterFileService.Save/CalamityCharacterSync; durante la sesion en curso vive en
    // EquipmentGroupViewModel/MergedContainers, que AppearanceViewModel no conoce). MainViewModel
    // es quien SI conoce a los dos (Appearance y EquipmentGroup) y empuja el valor real aqui -
    // ver UpdateEquippedArmor.
    private PlayerPreviewRenderer.EquippedArmor _liveArmor;

    public void UpdateEquippedArmor(PlayerPreviewRenderer.EquippedArmor armor)
    {
        _liveArmor = armor;
        RefreshPreview();
    }

    // C-15 (informe de pulido final, cierra A1): "casi toda edicion del personaje es
    // irreversible" - Apariencia era la unica pestaña real de edicion sin Deshacer/Rehacer (el
    // patron ya existe y es agnostico del dominio, ExplorationViewModel/MainViewModel.
    // UndoStack). Dos helpers: PushUndo para cambios discretos (un clic/seleccion - peinado,
    // tinte, genero, dificultad...) y PushUndoDebounced para campos continuos (vida/mana/horas,
    // editados via TextBox con UpdateSourceTrigger=PropertyChanged - CADA caracter tecleado
    // dispara un cambio real; sin agrupar, escribir "500" a mano dejaria 3 entradas de 1 digito
    // cada una en el historial).
    private void PushUndo<T>(string label, T before, T after, Action<T> apply)
    {
        if (_suppressWriteback || _character == null || _isUndoRedoInProgress()) return;
        if (EqualityComparer<T>.Default.Equals(before, after)) return;
        _pushUndo(new UndoEntry { Label = label, Undo = () => apply(before), Redo = () => apply(after) });
    }

    private sealed class PendingUndoGroup
    {
        public required object? Before;
        public object? After;
        public Action? OnFlushed;
        public required DispatcherTimer Timer;
    }
    private readonly Dictionary<string, PendingUndoGroup> _pendingUndoGroups = [];
    // C-15 (gotcha real #2): "ultimo valor conocido" por indice de swatch (Swatches[i]), para
    // poder ofrecer un "before" real a PushUndoDebounced - ver el comentario del suscriptor de
    // PropertyChanged en LoadFrom.
    private readonly Dictionary<int, (int R, int G, int B)> _swatchBaseline = [];

    // "Criterio mío" real del informe: debounce de ~400ms que empuja UNA entrada con el valor
    // INICIAL del gesto completo y el FINAL, no una por tecla/tick. El "before" solo se usa de
    // la PRIMERA llamada real de la rafaga (mientras el grupo siga pendiente, las siguientes
    // llamadas solo actualizan "after" y reinician el reloj) - "key" identifica el gesto (una
    // propiedad simple; un color usa el mismo key para sus 3 canales, para que un arrastre que
    // toque R y luego G cuente como UN solo cambio de color, no dos). onFlushed (opcional) deja
    // que el llamador actualice su propio "ultimo valor conocido" una vez el grupo se resuelve
    // de verdad (necesario para ColorSwatchViewModel, que no tiene un "oldValue" real propio -
    // ver el comentario de LoadFrom).
    private void PushUndoDebounced<T>(string key, string label, T before, T after, Action<T> apply, Action? onFlushed = null)
    {
        if (_suppressWriteback || _character == null || _isUndoRedoInProgress()) return;

        if (_pendingUndoGroups.TryGetValue(key, out var pending))
        {
            pending.After = after;
            pending.OnFlushed = onFlushed;
            pending.Timer.Stop();
            pending.Timer.Start();
            return;
        }

        var group = new PendingUndoGroup { Before = before, After = after, OnFlushed = onFlushed, Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) } };
        group.Timer.Tick += (_, _) =>
        {
            group.Timer.Stop();
            _pendingUndoGroups.Remove(key);
            var finalBefore = (T)group.Before!;
            var finalAfter = (T)group.After!;
            group.OnFlushed?.Invoke();
            // C-15: chequeo real EN ESTE INSTANTE, no al empezar la rafaga - un Deshacer/Rehacer
            // real puede llegar y volver a irse mucho antes de que este timer dispare.
            if (_isUndoRedoInProgress() || EqualityComparer<T>.Default.Equals(finalBefore, finalAfter)) return;
            _pushUndo(new UndoEntry { Label = label, Undo = () => apply(finalBefore), Redo = () => apply(finalAfter) });
        };
        _pendingUndoGroups[key] = group;
        group.Timer.Start();
    }

    private void RefreshPreview()
    {
        if (Swatches.Count < 7) return;
        PlayerPreviewRenderer.Tint T(int i) => new((byte)Swatches[i].R, (byte)Swatches[i].G, (byte)Swatches[i].B);

        var colors = new PlayerPreviewRenderer.PlayerColors(T(HairIdx), T(SkinIdx), T(EyesIdx), T(ShirtIdx), T(UnderIdx), T(PantsIdx), T(ShoesIdx));
        // H6-06: "Ver sin equipo" pasa EquippedArmor por defecto (todo null, sin overlay),
        // nunca inventa nada.
        var armor = ShowEquipment ? _liveArmor : default;
        // H6-01-b (advisor Opus): el doll necesita el skinVariant REAL (0-11, puede ser una
        // variante alternativa como el 8/MaleDress del caso "Eldelgas") para elegir la carpeta
        // de sprites correcta - IsMale por si sola solo distingue Chico/Chica, no la variante.
        // _character.Gender es la fuente real (PlrCharacter, ver LoadFrom); el fallback a
        // Starter solo puede darse antes de que LoadFrom termine de asignar _character.
        byte skinVariant = _character?.Gender
            ?? (IsMale ? TerrasavrNative.Core.Model.PlayerVariantSets.MaleStarter : TerrasavrNative.Core.Model.PlayerVariantSets.FemaleStarter);
        PreviewImage = PlayerPreviewRenderer.Render(HairStyle, skinVariant, colors, armor);
    }
}
