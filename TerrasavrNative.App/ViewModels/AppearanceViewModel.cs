using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Apariencia" - estilo de pelo, genero, tinte de pelo y los 7 colores reales del
// personaje (pelo/piel/ojos/camisa/camiseta interior/pantalones/zapatos), todos campos que
// PlrCharacter ya traia leidos desde la Fase 1 pero sin ningun panel para verlos/editarlos.
// PreviewImage se recalcula en cada cambio (ver PlayerPreviewRenderer para el detalle real
// del atlas/tintado, y su propia limitacion documentada: es un icono de cabeza pequeño, no
// un personaje de cuerpo completo - asi era ya en el Terrasavr original).
public partial class AppearanceViewModel : ObservableObject
{
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    [ObservableProperty] private WriteableBitmap? _previewImage;

    [ObservableProperty] private int _hairStyle;
    [ObservableProperty] private int _hairDye;
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
        _suppressWriteback = false;

        foreach (var swatch in Swatches)
            swatch.PropertyChanged += (_, _) => RefreshPreview();

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
