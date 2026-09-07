using System.Windows.Media.Imaging;
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
    public WriteableBitmap Preview { get; }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". HomeViewModel.UpdateCurrentPath la fija
    // comparando FilePath contra el personaje realmente cargado en MainViewModel.
    [ObservableProperty] private bool _isCurrent;

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
        Preview = PlayerPreviewRenderer.Render(character.HairStyle, character.Gender, colors, armor);
    }
}
