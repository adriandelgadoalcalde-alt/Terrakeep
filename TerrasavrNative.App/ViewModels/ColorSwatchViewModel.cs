using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Un color editable del personaje (pelo/piel/ojos/ropa...) - envuelve el byte[3] real de
// PlrCharacter y escribe en el mismo array en cuanto cambia un canal (no hace falta
// sincronizar de vuelta al guardar, Save() ya escribe el PlrCharacter que comparte este
// array). R/G/B expuestos como int (0-255) para poder enlazar Slider directamente sin
// convertidor.
public partial class ColorSwatchViewModel : ObservableObject
{
    private readonly byte[] _target;
    private bool _suppressWriteback;

    public string Label { get; }

    [ObservableProperty] private int _r;
    [ObservableProperty] private int _g;
    [ObservableProperty] private int _b;
    [ObservableProperty] private Brush _preview = Brushes.Black;

    public ColorSwatchViewModel(string label, byte[] target)
    {
        Label = label;
        _target = target;
        LoadFromTarget();
    }

    // Vuelve a leer del array real - usado al cargar/recargar un personaje (el array
    // subyacente es uno nuevo tras un Load, este ViewModel se reconstruye igualmente en ese
    // caso, pero se deja aqui por si algun dia se reutiliza la instancia).
    public void LoadFromTarget()
    {
        _suppressWriteback = true;
        R = _target.Length > 0 ? _target[0] : 0;
        G = _target.Length > 1 ? _target[1] : 0;
        B = _target.Length > 2 ? _target[2] : 0;
        _suppressWriteback = false;
        UpdatePreview();
    }

    partial void OnRChanged(int value) => WriteBack();
    partial void OnGChanged(int value) => WriteBack();
    partial void OnBChanged(int value) => WriteBack();

    private void WriteBack()
    {
        if (_suppressWriteback) return;
        if (_target.Length > 0) _target[0] = (byte)Math.Clamp(R, 0, 255);
        if (_target.Length > 1) _target[1] = (byte)Math.Clamp(G, 0, 255);
        if (_target.Length > 2) _target[2] = (byte)Math.Clamp(B, 0, 255);
        UpdatePreview();
    }

    private void UpdatePreview() =>
        Preview = new SolidColorBrush(Color.FromRgb((byte)Math.Clamp(R, 0, 255), (byte)Math.Clamp(G, 0, 255), (byte)Math.Clamp(B, 0, 255)));
}
