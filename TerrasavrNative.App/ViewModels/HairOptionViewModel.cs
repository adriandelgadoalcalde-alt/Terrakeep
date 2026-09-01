using System.Windows.Media.Imaging;

namespace TerrasavrNative.App.ViewModels;

// Una miniatura del selector visual de peinado (Apariencia) - ver AppearanceViewModel.
public sealed class HairOptionViewModel(int id, WriteableBitmap thumbnail)
{
    public int Id { get; } = id;
    public WriteableBitmap Thumbnail { get; } = thumbnail;
}
