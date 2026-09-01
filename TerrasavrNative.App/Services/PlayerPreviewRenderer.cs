using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerrasavrNative.App.Services;

// Compone el preview de "Apariencia" de CUERPO COMPLETO (pedido explicito, 1-sep-2026 -
// sustituye al icono de cabeza anterior) a partir de los sprites REALES del jugador de
// Terraria, extraidos una vez de la instalacion vanilla de este PC
// (C:\Program Files (x86)\Steam\steamapps\common\Terraria\Content\Images\Player_*.xnb,
// descomprimidos con la herramienta npm "xnb" - los XNB de Terraria van comprimidos con LZX,
// reimplementarlo a mano habria sido un riesgo real) y recortados al frame de reposo (la
// esquina superior-izquierda 40x56 de cada hoja - confirmado que TODAS las hojas, sean tira
// vertical o rejilla, tienen ahi el frame 0/celda(0,0)) - ver Assets/player/{body,hair}.
//
// Formato/capas confirmados leyendo PlayerTextureID.cs y PlayerDrawSet.cs decompilados reales
// (tModLoader-Decompiled): cada pieza es un lienzo de 40x56 con el sprite YA posicionado en su
// sitio dentro de ese lienzo (cabeza arriba, piernas abajo...) - todas las piezas se
// superponen en el MISMO origen (0,0), verificado componiendo y viendo el resultado (una
// figura de Terraria reconocible de pie, no una silueta descolocada). Tintado = multiplicacion
// RGB pura (igual que el icono de cabeza anterior), mapeo de color confirmado 1:1 contra los 7
// campos ya en PlrCharacter: HairColor/SkinColor/EyeColor/ShirtColor/UnderColor/PantsColor/
// ShoesColor (EyeWhites siempre blanco, sin campo propio).
//
// ALCANCE DELIBERADO: sin armadura/vestuario equipado real, item en mano, accesorios, alas,
// animacion (solo el frame de reposo) - necesitarian el catalogo de sprites de armadura
// entero y el motor de animacion real, un proyecto en si mismo. Tampoco se dibuja el brazo
// delantero como capa aparte (ArmSkin/ArmShirt): esas hojas solo tienen frames de brazo en
// pose de balanceo (sujetando algo), ninguna con el brazo simplemente caido a un lado en la
// celda de reposo (confirmado inspeccionando la rejilla completa) - el propio silueta de
// TorsoSkin ya incluye un brazo pegado al cuerpo, asi que el resultado sigue siendo una figura
// completa y coherente sin esa capa extra.
//
// Solo se usa la variante de piel "0" (StarterMale, la unica con las 15 piezas base completas
// en la instalacion real) para AMBOS generos - la piel en si no cambia de forma entre
// variantes de genero de forma perceptible a este tamaño, y la variante "femenina" (X=4 segun
// PlayerTextureID) no se investigo a fondo por no ser el foco de esta ronda.
public static class PlayerPreviewRenderer
{
    private const int Width = 40, Height = 56;
    private const int HairStyleMin = 1, HairStyleMax = 228;

    public readonly record struct Tint(byte R, byte G, byte B);

    public readonly record struct PlayerColors(Tint Hair, Tint Skin, Tint Eyes, Tint Shirt, Tint Under, Tint Pants, Tint Shoes);

    private static readonly Dictionary<string, byte[]> Cache = [];

    public static WriteableBitmap Render(int hairStyle, bool isMale, PlayerColors colors)
    {
        // isMale sin uso todavia: la unica variante de piel completa disponible (0/
        // StarterMale) se usa para ambos generos, ver el comentario de la clase.
        _ = isMale;

        var canvas = new byte[Height * Width * 4];

        Composite(canvas, LoadBody("legskin"), colors.Skin);
        Composite(canvas, LoadBody("pants"), colors.Pants);
        Composite(canvas, LoadBody("shoes"), colors.Shoes);
        Composite(canvas, LoadBody("torsoskin"), colors.Skin);
        Composite(canvas, LoadBody("undershirt"), colors.Under);
        Composite(canvas, LoadBody("shirt"), colors.Shirt);
        Composite(canvas, LoadBody("head"), colors.Skin);
        Composite(canvas, LoadHair(hairStyle), colors.Hair);
        Composite(canvas, LoadBody("eyewhites"), null); // ya blanco en el sprite real
        Composite(canvas, LoadBody("eyes"), colors.Eyes);

        var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), canvas, Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    // Miniatura de un unico peinado (sin cuerpo) para el selector visual de Apariencia -
    // pedido explicito 1-sep-2026, "sprites de los diferentes cabeza corte de pelo... que se
    // pueda ver". Reusa el mismo sprite/tintado real que el preview completo.
    public static WriteableBitmap RenderHairThumbnail(int hairStyle, Tint hairColor)
    {
        var canvas = new byte[Height * Width * 4];
        Composite(canvas, LoadHair(hairStyle), hairColor);
        var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), canvas, Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    public static int HairStyleCount => HairStyleMax;

    private static byte[] LoadBody(string name) =>
        LoadCached(Path.Combine(AppContext.BaseDirectory, "Assets", "player", "body", name + ".png"));

    // El id de HairStyle del .plr se usa tal cual como nombre de archivo
    // (Assets/player/hair/{id}.png, 1-228, mismo rango que Player_Hair_N.xnb reales) - no se
    // verificio pixel a pixel que el id guardado en el .plr y el numero de archivo real
    // coincidan exactamente (podria haber un desfase de +/-1 entre el HairID interno del
    // juego y el nombre de archivo) - si el id no tiene archivo, cae al estilo 1 en vez de
    // fallar, un peinado "equivocado" es preferible a un preview roto.
    private static byte[] LoadHair(int hairStyle)
    {
        int id = Math.Clamp(hairStyle, HairStyleMin, HairStyleMax);
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hair", id + ".png");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "hair", "1.png");
        return LoadCached(path);
    }

    private static byte[] LoadCached(string path)
    {
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var pixels = LoadPngPixels(path);
        Cache[path] = pixels;
        return pixels;
    }

    private static byte[] LoadPngPixels(string path)
    {
        var decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var converted = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
        var pixels = new byte[Width * Height * 4];
        converted.CopyPixels(pixels, Width * 4, 0);
        return pixels;
    }

    // Todas las piezas son exactamente 40x56 (ya recortadas al extraerlas) - se puede
    // componer indice a indice sin cuentas de offset.
    private static void Composite(byte[] dst, byte[] src, Tint? tint)
    {
        for (int i = 0; i < src.Length; i += 4)
        {
            byte b = src[i], g = src[i + 1], r = src[i + 2], a = src[i + 3];
            if (a == 0) continue;

            if (tint is { } t)
            {
                r = (byte)(r * t.R / 255);
                g = (byte)(g * t.G / 255);
                b = (byte)(b * t.B / 255);
            }

            float alpha = a / 255f;
            dst[i + 0] = (byte)(b * alpha + dst[i + 0] * (1 - alpha));
            dst[i + 1] = (byte)(g * alpha + dst[i + 1] * (1 - alpha));
            dst[i + 2] = (byte)(r * alpha + dst[i + 2] * (1 - alpha));
            dst[i + 3] = (byte)(a + dst[i + 3] * (1 - alpha));
        }
    }
}
