using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Auditoria de Opus, I-1: "Inicio" no practicaba lo que predicaba (P1/P2, todo a mano de
// golpe) - un personaje real en Documents\...\Players no aparecia en ningun sitio hasta abrir
// el Explorador de archivos a mano. Una fila real por cada .plr encontrado, con lo mismo que
// ya se ve al elegirlo a ciegas en el dialogo: nombre, dificultad, insignia de Calamity (mismo
// criterio que CharacterFileService.Load, ".tplr con el mismo nombre al lado") y fecha real de
// ultima modificacion - mas el doll de cuerpo completo ya real de PlayerPreviewRenderer, sin
// inventar ningun dato que el .plr no tenga de verdad.
public sealed partial class CharacterListEntryViewModel : ObservableObject
{
    public string FilePath { get; }
    public string Name { get; }
    public string DifficultyLabel { get; }
    public bool IsCalamity { get; }
    public string LastModifiedText { get; }
    public WriteableBitmap Preview { get; }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". HomeViewModel.UpdateCurrentPath la fija
    // comparando FilePath contra el personaje realmente cargado en MainViewModel.
    [ObservableProperty] private bool _isCurrent;

    public CharacterListEntryViewModel(string plrPath, PlrCharacter character, bool isCalamity, DateTime lastModifiedUtc)
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
        IsCalamity = isCalamity;
        LastModifiedText = lastModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        PlayerPreviewRenderer.Tint T(byte[] c) => new(c[0], c[1], c[2]);
        var colors = new PlayerPreviewRenderer.PlayerColors(
            T(character.HairColor), T(character.SkinColor), T(character.EyeColor),
            T(character.ShirtColor), T(character.UnderColor), T(character.PantsColor), T(character.ShoesColor));
        Preview = PlayerPreviewRenderer.Render(character.HairStyle, character.Gender == 1, colors);
    }
}
