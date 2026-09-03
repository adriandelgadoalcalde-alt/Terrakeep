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
    // Ap-c (segunda auditoria de Opus, Fable): "sin valor hexadecimal ni paleta para los
    // colores" - antes solo 3 sliders R/G/B en crudo, sin ningun numero visible ni forma de
    // pegar/escribir un color conocido de un vistazo ("#FF0000"). Se actualiza en los DOS
    // sentidos: al mover un slider (UpdatePreview) y al escribir un hex valido a mano
    // (OnHexChanged). No hay tabla de paleta real de Terraria/Calamity que copiar (los colores
    // de personaje son libres, no una lista curada como los tintes) - un campo hex editable es
    // el equivalente real y util a "pegar un color conocido", sin inventar una paleta fija que
    // no representaria nada del juego real.
    [ObservableProperty] private string _hex = "#000000";

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

    private void UpdatePreview()
    {
        byte r = (byte)Math.Clamp(R, 0, 255), g = (byte)Math.Clamp(G, 0, 255), b = (byte)Math.Clamp(B, 0, 255);
        Preview = new SolidColorBrush(Color.FromRgb(r, g, b));
        _suppressWriteback = true; // el propio Hex se actualiza aqui - no reinterpretarlo como una escritura nueva del usuario
        Hex = $"#{r:X2}{g:X2}{b:X2}";
        _suppressWriteback = false;
    }

    // Ap-c: solo aplica con un hex real de 6 digitos (con o sin "#") - un valor a medio
    // escribir (el usuario todavia tecleando) se ignora en silencio en vez de aplicar algo
    // incorrecto; LostFocus (sin UpdateSourceTrigger=PropertyChanged, mismo criterio real ya
    // usado en "Índice (id)", T-17) da tiempo real a terminar de escribir antes de confirmar.
    partial void OnHexChanged(string value)
    {
        if (_suppressWriteback) return;
        string cleaned = value.TrimStart('#');
        if (cleaned.Length != 6 || !int.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out int rgb)) return;

        _suppressWriteback = true;
        R = (rgb >> 16) & 0xFF;
        G = (rgb >> 8) & 0xFF;
        B = rgb & 0xFF;
        _suppressWriteback = false;
        WriteBack();
    }
}
