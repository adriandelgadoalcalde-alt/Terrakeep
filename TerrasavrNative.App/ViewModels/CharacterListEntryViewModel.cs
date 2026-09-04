using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

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
    // PlayerIO.cs:67) - la lista de mods que estaban cargados la ultima vez. Null si el .tplr no
    // la trae (los que escribe el propio Terrakeep no la tienen) para que el ToolTip no aparezca
    // vacio.
    public string? UsedModsTooltip { get; }
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
        DifficultyLabel = character.Difficulty switch
        {
            1 => "Mediumcore",
            2 => "Hardcore",
            3 => "Journey",
            _ => "Softcore",
        };
        IsTModLoader = isTModLoader;
        IsCalamity = tplr?.HasCalamityContent == true;
        UsedModsTooltip = tplr is { UsedMods.Count: > 0 }
            ? "Mods usados la última vez: " + string.Join(", ", tplr.UsedMods)
            : null;
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
