using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Data;

namespace Terrakeep.App.Services;

// Mascota REAL equipada, animada en vivo durante el hover de Inicio - pedido explicito del
// usuario (21-sep-2026), comparando en vivo con Terraria vanilla: la mascota tiene que
// animarse de verdad mientras el raton esta encima, no quedarse en un sprite fijo.
//
// Ver el comentario real de PetAnimationCatalog (Terrakeep.Core/Data/PetAnimationCatalog.cs)
// para la cita completa del decompilado (UICharacter.cs/ProjectileID.cs/
// SettingsForCharacterPreview.cs/Main.cs) que confirma el mecanismo real: un simple avance de
// fila dentro de una tira vertical (SelStart..SelStart+SelCount-1), cada "SelDelay" ticks reales
// de 60/s. La hoja real (Assets/pets/{shoot}.png, tira vertical entera con TotalFrames filas
// reales) ya la extrajo scripts/extraer-sprites-mascotas.js - este renderer solo recorta la
// fila que toca, sin tintar nada (las mascotas NO se tiñen con los colores del personaje, son
// su propio sprite real completo, a diferencia de la piel/ropa base).
public static class PetPreviewRenderer
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (byte[] Pixels, int Width, int Height)> SheetCache = new();

    // frameIndex es el indice REAL dentro del ciclo de hover (0..SelCount-1, ya relativo a
    // SelStart - el llamador nunca necesita saber SelStart) - PetAnimationDriver (mas abajo)
    // es quien traduce tiempo transcurrido a este indice.
    public static WriteableBitmap? RenderFrame(PetAnimationEntry entry, int frameIndexEnCiclo)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "pets", entry.Shoot + ".png");
        if (!File.Exists(path)) return null;

        var (pixels, width, height) = SheetCache.GetOrAdd(entry.Shoot, _ => LoadSheet(path));
        if (width == 0 || height == 0) return null;

        // Math.Floor real (comentario de extraer-sprites-mascotas.js): un unico proyectil real
        // (398) tiene un alto que no es multiplo EXACTO de TotalFrames - el resto se descarta,
        // fiel al comportamiento mas cercano posible sin inventar filas que no existen.
        int frameHeight = height / entry.TotalFrames;
        if (frameHeight <= 0) return null;

        int fila = entry.SelStart + Math.Clamp(frameIndexEnCiclo, 0, entry.SelCount - 1);
        fila = Math.Clamp(fila, 0, entry.TotalFrames - 1);
        int startY = fila * frameHeight;
        if (startY + frameHeight > height) return null;

        var frame = new byte[width * frameHeight * 4];
        Array.Copy(pixels, startY * width * 4, frame, 0, frame.Length);

        var bitmap = new WriteableBitmap(width, frameHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, frameHeight), frame, width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static (byte[], int, int) LoadSheet(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        var pixels = new byte[frame.PixelWidth * frame.PixelHeight * 4];
        converted.CopyPixels(pixels, frame.PixelWidth * 4, 0);
        return (pixels, frame.PixelWidth, frame.PixelHeight);
    }
}

// Traduce tiempo real transcurrido (ms) al indice de fotograma real dentro del ciclo de hover -
// SelDelay esta en TICKS de juego (60/s, Terraria/DataStructures/SettingsForCharacterPreview.cs
// real), aqui convertidos a ms reales (SelDelay * 1000/60) para no depender de la cadencia fija
// del timer de la tarjeta (90ms) - un pet con SelDelay=3 (mas rapido) se ve de verdad mas rapido
// que uno con SelDelay=6, igual que en el juego real, en vez de que todos avancen a la misma
// velocidad generica.
public sealed class PetAnimationDriver(PetAnimationEntry entry)
{
    private double _acumuladoMs;

    public int FrameActualEnCiclo { get; private set; }

    public void Avanzar(double deltaMs)
    {
        if (entry.SelCount <= 1) return;
        double msPorFotogramaReal = entry.SelDelay * (1000.0 / 60.0);
        if (msPorFotogramaReal <= 0) return;
        _acumuladoMs += deltaMs;
        int avance = (int)(_acumuladoMs / msPorFotogramaReal);
        if (avance <= 0) return;
        _acumuladoMs -= avance * msPorFotogramaReal;
        FrameActualEnCiclo = (FrameActualEnCiclo + avance) % entry.SelCount;
    }

    public void Reiniciar()
    {
        _acumuladoMs = 0;
        FrameActualEnCiclo = 0;
    }
}
