using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
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
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    [ObservableProperty] private WriteableBitmap? _previewImage;

    [ObservableProperty] private int _hairStyle;
    [ObservableProperty] private int _hairDye;
    [ObservableProperty] private string _hairDyeDisplayName = "Ninguno";

    public AppearanceViewModel(CharacterFileService service)
    {
        _service = service;
        BuildHairDyeOptions();
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
    [ObservableProperty] private bool _isHairPickerOpen;
    public ObservableCollection<HairOptionViewModel> HairOptions { get; } = [];

    [RelayCommand]
    private void OpenHairPicker()
    {
        if (HairOptions.Count == 0 && Swatches.Count > HairIdx)
        {
            var hairColor = new PlayerPreviewRenderer.Tint((byte)Swatches[HairIdx].R, (byte)Swatches[HairIdx].G, (byte)Swatches[HairIdx].B);
            for (int id = 1; id <= PlayerPreviewRenderer.HairStyleCount; id++)
                HairOptions.Add(new HairOptionViewModel(id, PlayerPreviewRenderer.RenderHairThumbnail(id, hairColor)));
        }
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
        HairDyeOptions.Add(new HairDyeOptionViewModel(0, "Ninguno", null));
        foreach (var entry in _service.HairDyes.Entries)
        {
            string name = _service.VanillaCatalog.GetName(entry.ItemId);
            string? icon = VanillaIconResolver.GetIconPath(entry.ItemId);
            HairDyeOptions.Add(new HairDyeOptionViewModel(entry.Index, name, icon));
        }
    }

    [RelayCommand]
    private void OpenHairDyePicker() => IsHairDyePickerOpen = true;

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
        Swatches.Clear();
        Swatches.Add(new ColorSwatchViewModel("Pelo", character.HairColor));
        Swatches.Add(new ColorSwatchViewModel("Piel", character.SkinColor));
        Swatches.Add(new ColorSwatchViewModel("Ojos", character.EyeColor));
        Swatches.Add(new ColorSwatchViewModel("Camisa", character.ShirtColor));
        Swatches.Add(new ColorSwatchViewModel("Camiseta interior", character.UnderColor));
        Swatches.Add(new ColorSwatchViewModel("Pantalones", character.PantsColor));
        Swatches.Add(new ColorSwatchViewModel("Zapatos", character.ShoesColor));

        _suppressWriteback = true;
        HairStyle = character.HairStyle;
        HairDye = character.HairDye;
        IsMale = character.Gender == 1;
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
            swatch.PropertyChanged += (_, _) => RefreshPreview();
            if (i == HairIdx) swatch.PropertyChanged += (_, _) => HairOptions.Clear(); // color de pelo cambio, las miniaturas quedan obsoletas
        }
        HairOptions.Clear();

        _character = character;
        RefreshPreview();
    }

    partial void OnHairStyleChanged(int value)
    {
        RefreshPreview();
        if (_suppressWriteback || _character == null) return;
        _character.HairStyle = value;
    }

    partial void OnHairDyeChanged(int value)
    {
        HairDyeDisplayName = HairDyeOptions.FirstOrDefault(o => o.Index == value)?.DisplayName ?? $"Tinte #{value}";
        if (_suppressWriteback || _character == null) return;
        _character.HairDye = (byte)Math.Clamp(value, 0, 255);
    }

    partial void OnIsMaleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFemale));
        RefreshPreview();
        if (_suppressWriteback || _character == null) return;
        _character.Gender = (byte)(value ? 1 : 0);
    }

    partial void OnDifficultyChanged(int value)
    {
        OnPropertyChanged(nameof(DifficultyLabel));
        if (_suppressWriteback || _character == null) return;
        _character.Difficulty = (byte)Math.Clamp(value, 0, 3);
    }

    partial void OnHealthNowChanged(int value) { if (!_suppressWriteback && _character != null) _character.HealthNow = value; }
    partial void OnHealthMaxChanged(int value) { if (!_suppressWriteback && _character != null) _character.HealthMax = value; }
    partial void OnManaNowChanged(int value) { if (!_suppressWriteback && _character != null) _character.ManaNow = value; }
    partial void OnManaMaxChanged(int value) { if (!_suppressWriteback && _character != null) _character.ManaMax = value; }
    partial void OnFishingQuestsCompletedChanged(int value) { if (!_suppressWriteback && _character != null) _character.FishingQuestsCompleted = value; }
    partial void OnGolferScoreChanged(int value) { if (!_suppressWriteback && _character != null) _character.GolferScore = value; }

    partial void OnPlayHoursChanged(double value)
    {
        if (_suppressWriteback || _character == null) return;
        long ticks = TimeSpan.FromHours(Math.Max(0, value)).Ticks;
        _character.PlayTimeLow = unchecked((uint)ticks);
        _character.PlayTimeHigh = unchecked((uint)(ticks >> 32));
    }

    // Indices en Swatches, mismo orden en que se anaden arriba en LoadFrom.
    private const int HairIdx = 0, SkinIdx = 1, EyesIdx = 2, ShirtIdx = 3, UnderIdx = 4, PantsIdx = 5, ShoesIdx = 6;

    private void RefreshPreview()
    {
        if (Swatches.Count < 7) return;
        PlayerPreviewRenderer.Tint T(int i) => new((byte)Swatches[i].R, (byte)Swatches[i].G, (byte)Swatches[i].B);

        var colors = new PlayerPreviewRenderer.PlayerColors(T(HairIdx), T(SkinIdx), T(EyesIdx), T(ShirtIdx), T(UnderIdx), T(PantsIdx), T(ShoesIdx));
        PreviewImage = PlayerPreviewRenderer.Render(HairStyle, IsMale, colors);
    }
}
