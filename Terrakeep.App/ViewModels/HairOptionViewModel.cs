using System.Windows.Media.Imaging;

namespace Terrakeep.App.ViewModels;

// Una miniatura del selector visual de peinado (Apariencia) - ver AppearanceViewModel.
// H6-04 (Opus, sexta pasada): Id es el valor REAL 0-based que se guarda en HairStyle (Player.
// hair real) - DisplayNumber (Id+1) es solo para el tooltip, el mismo numero que el propio
// juego muestra en su creador de personajes (UICharacterCreation.cs real: "player.hair + 1").
public sealed class HairOptionViewModel(int id, WriteableBitmap thumbnail)
{
    public int Id { get; } = id;
    public int DisplayNumber => Id + 1;
    public WriteableBitmap Thumbnail { get; } = thumbnail;
}
