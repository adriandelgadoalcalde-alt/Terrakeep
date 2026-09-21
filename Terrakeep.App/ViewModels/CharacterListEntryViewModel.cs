using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Auditoria de Opus, I-1: "Inicio" no practicaba lo que predicaba (P1/P2, todo a mano de
// golpe) - un personaje real en Documents\...\Players no aparecia en ningun sitio hasta abrir
// el Explorador de archivos a mano. Una fila real por cada .plr encontrado, con lo mismo que
// ya se ve al elegirlo a ciegas en el dialogo: nombre, dificultad, insignia de Calamity (mismo
// criterio que CharacterFileService.Load, ".tplr con el mismo nombre al lado") y fecha real de
// ultima modificacion - mas el doll de cuerpo completo ya real de PlayerPreviewRenderer
// (colores base + armadura/vanidad real puesta, ver EquipmentAppearanceResolver), sin
// inventar ningun dato que el .plr no tenga de verdad.
public sealed partial class CharacterListEntryViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string FilePath { get; }
    public string Name { get; }
    public string DifficultyLabel { get; }
    // Encargo del usuario 4-sep-2026 - tres hechos DISTINTOS donde antes habia uno solo mal
    // nombrado (ver ESPEC-sprites-botones-badges.md#C):
    //   IsTModLoader = existe un .tplr hermano. El hecho PRINCIPAL ("verdaderamente de tmodloader").
    //   IsCalamity   = ademas hay un objeto o un buff de Calamity REAL dentro de ese .tplr (mismo
    //                  criterio exacto que CharacterFileService.Save, resuelto sobre el NBT crudo).
    //                  NO sustituye a IsTModLoader: un personaje puede llevar las dos insignias.
    //   IsVanilla    = no hay .tplr. El usuario lo pidio explicitamente ("al igual que cuando un
    //                  personaje es vanilla que tenga dicha etiqueta").
    public bool IsTModLoader { get; }
    public bool IsCalamity { get; }
    public bool IsVanilla => !IsTModLoader;
    // Regalo de la clave usedMods real del .tplr (la escribe tModLoader en cada guardado,
    // PlayerIO.cs:67) - la lista de mods que estaban cargados la ultima vez.
    //
    // Oleada del 6-sep-2026 (bloque INI-05 del arnes) - DOS BUGS REALES de idioma en la misma
    // linea, los dos invisibles para el barrido A10-IDIOMA-BARRIDO porque un ToolTip no es un
    // TextBlock de la ventana:
    //   a) El texto se componia UNA sola vez, en el constructor, con el idioma de ese instante -
    //      cambiar a ingles en vivo dejaba "Mods usados la última vez: ..." tal cual.
    //   b) El caso NORMAL (un .tplr SIN lista de mods, que es justo lo que escribe el propio
    //      Terrakeep) devolvia null, y el XAML tapaba ese null con un TargetNullValue en
    //      español DURO - o sea, la insignia "tModLoader" enseñaba una frase en español
    //      SIEMPRE, tambien con la app entera en ingles.
    // Ahora nunca es null (ese caso tiene su propia clave real, tt_tmodloader_badge) y se
    // compone al leerlo; `Loc` es el mismo singleton que refresca los bindings al cambiar de
    // idioma, asi que un ToolTip que se vuelva a abrir sale ya en el idioma nuevo.
    public string UsedModsTooltip => _usedMods is { Count: > 0 }
        ? Loc["character_used_mods"] + string.Join(", ", _usedMods)
        : Loc["tt_tmodloader_badge"];

    private readonly IReadOnlyList<string>? _usedMods;
    public string LastModifiedText { get; }
    [ObservableProperty] private WriteableBitmap _preview;

    // Hover en Inicio ("que ande solo al pasar el raton, con brazos" - bitacora.md 21-sep-2026).
    // Terraria/GameContent/UI/Elements/UICharacterListItem.cs real, MouseOver/MouseOut (338-362):
    // "_playerPanel.SetAnimated(animated: true/false)" - el trigger real es el HOVER de la propia
    // tarjeta (MouseOver/MouseOut de UIPanel), nunca un boton manual (ese es un añadido aparte de
    // Apariencia, AppearanceViewModel.ToggleWalkAnimation, que se queda igual). Reusa el MISMO
    // ciclo de 13 filas reales (AppearanceViewModel.WalkCycleRows) y la MISMA cadencia (90ms) que
    // ya tiene Apariencia, para no duplicar ninguna secuencia a mano.
    //
    // Datos crudos guardados aqui (no solo en el constructor) porque re-renderizar en cada tick
    // necesita los MISMOS argumentos reales que PlayerPreviewRenderer.Render ya recibio una vez -
    // sin volver a leer el .plr, sin ningun dato nuevo.
    private readonly int _hairStyle;
    private readonly byte _skinVariant;
    private readonly PlayerPreviewRenderer.PlayerColors _colors;
    private readonly PlayerPreviewRenderer.EquippedArmor _armor;

    // Icono real de la mascota puesta (VanillaIconResolver, mismo catalogo que el resto de la
    // app) - ver EquipmentAppearanceResolver.ResolvePet para el detalle real y el alcance
    // deliberado documentado. Null si no hay mascota puesta o si el objeto no tiene icono
    // extraido (mismo criterio "lo que no se encuentra no se inventa" que el resto del resolver).
    public string? PetIconPath { get; }

    // Timer PROPIO de esta tarjeta, arrancado SOLO mientras el raton esta encima y parado de
    // verdad al salir (MainWindow.xaml: MouseEnter/MouseLeave + Unloaded como red de seguridad
    // adicional) - mismo bug real ya cerrado en Apariencia (AppearanceViewModel.
    // StopWalkAnimation, "Terrakeep congelado" 8h+ por un DispatcherTimer que no se paraba solo,
    // ver bitacora.md 21-sep-2026 madrugada) NUNCA se repite aqui: un timer por tarjeta, parado
    // en cuanto deja de estar en hover, sin ningun camino donde pueda quedar corriendo solo.
    private readonly DispatcherTimer _hoverWalkTimer = new() { Interval = TimeSpan.FromMilliseconds(90) };
    private int _walkCycleIndex;
    private bool _isHovering;

    public void SetHovering(bool hovering)
    {
        if (_isHovering == hovering) return;
        _isHovering = hovering;
        if (hovering)
        {
            _walkCycleIndex = 0;
            _hoverWalkTimer.Start();
        }
        else
        {
            _hoverWalkTimer.Stop();
        }
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        int frame = _isHovering ? AppearanceViewModel.WalkCycleRows[_walkCycleIndex] : 0;
        Preview = PlayerPreviewRenderer.Render(_hairStyle, _skinVariant, _colors, _armor, frame);
    }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". HomeViewModel.UpdateCurrentPath la fija
    // comparando FilePath contra el personaje realmente cargado en MainViewModel.
    [ObservableProperty] private bool _isCurrent;

    // Catalogo de rediseño visual T4 (20-sep-2026, "Inicio como escritorio de partida"): dos de
    // los KPI reales de la tarjeta hero de "Continuar" (la tercera es la etapa de la Guia, que
    // vive aparte en HomeViewModel.LastSessionGuideStage - necesita el catalogo completo de la
    // Guia, no solo el .plr). Vida maxima y tiempo jugado SI son datos del propio character que
    // este constructor YA recibe entero, sin ninguna lectura ni calculo extra.
    public int HealthMax { get; }
    public string PlayTimeText { get; }

    public CharacterListEntryViewModel(string plrPath, PlrCharacter character, bool isTModLoader,
        TplrModSummary? tplr, DateTime lastModifiedUtc, EquipmentAppearanceResolver equipmentAppearance)
    {
        FilePath = plrPath;
        Name = character.Name;
        // Oleada del 6-sep-2026 (Personaje > Apariencia): estos 4 nombres iban a pelo en ingles
        // (y con el nombre INTERNO "Softcore" en vez del "Classic" real del juego) en DOS sitios
        // distintos, la tarjeta de Inicio y el selector de Apariencia - un unico sitio real
        // ahora, con la traduccion real del propio Terraria (ver AppearanceViewModel).
        DifficultyLabel = AppearanceViewModel.DifficultyLabelFor(character.Difficulty);
        IsTModLoader = isTModLoader;
        IsCalamity = tplr?.HasCalamityContent == true;
        _usedMods = tplr?.UsedMods;
        LastModifiedText = lastModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        HealthMax = character.HealthMax;
        // Misma formula real ya usada en BackupHistoryViewModel.HorasJugadas/CompareViewModel.
        // FormatPlayTime (PlayTimeLow/High son dos UInt32 que juntos forman los ticks reales de
        // .NET).
        long totalTicksJugados = ((long)character.PlayTimeHigh << 32) | character.PlayTimeLow;
        var tiempoJugado = TimeSpan.FromTicks(totalTicksJugados);
        PlayTimeText = tiempoJugado.TotalHours >= 1 ? $"{(int)tiempoJugado.TotalHours}h {tiempoJugado.Minutes}min" : $"{tiempoJugado.Minutes}min";

        PlayerPreviewRenderer.Tint T(byte[] c) => new(c[0], c[1], c[2]);
        var colors = new PlayerPreviewRenderer.PlayerColors(
            T(character.HairColor), T(character.SkinColor), T(character.EyeColor),
            T(character.ShirtColor), T(character.UnderColor), T(character.PantsColor), T(character.ShoesColor));
        // Doll fiel al guardado (pedido explicito del usuario, 3-sep-2026): la armadura/
        // vanidad REAL puesta en loadouts[0] (el mirror de "lo que lleva puesto de verdad"),
        // no solo los 7 colores base.
        var armor = equipmentAppearance.Resolve(character.PrimaryLoadout);
        // H6-02/H6-01-b: Gender ES el skinVariant real (0-11, no un booleano) - se pasa entero
        // para que el doll de Inicio use la carpeta de sprites/reglas SetMatch reales de la
        // variante puesta (caso "Eldelgas": Gender=8/MaleDress), no solo Chico/Chica.
        _hairStyle = character.HairStyle;
        _skinVariant = character.Gender;
        _colors = colors;
        _armor = armor;
        PetIconPath = equipmentAppearance.ResolvePet(character.EquipmentItems);
        _preview = PlayerPreviewRenderer.Render(_hairStyle, _skinVariant, colors, armor);

        _hoverWalkTimer.Tick += (_, _) =>
        {
            _walkCycleIndex = (_walkCycleIndex + 1) % AppearanceViewModel.WalkCycleRows.Length;
            RefreshPreview();
        };
    }
}
