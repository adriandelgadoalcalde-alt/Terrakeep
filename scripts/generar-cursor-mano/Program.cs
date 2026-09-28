// Generador del cursor "mano que agarra" de Terrakeep (28-sep-2026).
// Dibuja la mano como geometria vectorial en una rejilla de 32x32 unidades y la rasteriza a varios
// tamaños (DPI alto), escribiendo un .cur multi-imagen (DIB 32bpp BGRA + mascara AND) con el
// hotspot en el centro de la palma. Tambien escribe PNG de vista previa.
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class Program
{
    const double Base = 32;
    static readonly Point HotspotBase = new(16.5, 18.5);
    static readonly int[] Tamaños = { 32, 40, 48, 64, 96 };

    [STAThread]
    static int Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(outDir);
        foreach (var (nombre, prohibido) in new[] { ("mano-agarrar", false), ("mano-agarrar-no", true) })
        {
            var imagenes = new List<(int lado, int hx, int hy, byte[] bgra)>();
            foreach (int n in Tamaños)
            {
                var bmp = Rasterizar(n, prohibido);
                var px = new byte[n * n * 4];
                bmp.CopyPixels(px, n * 4, 0);
                // Pbgra32 premultiplicado -> BGRA recto (el .cur espera alfa no premultiplicado)
                for (int i = 0; i < px.Length; i += 4)
                {
                    byte a = px[i + 3];
                    if (a == 0 || a == 255) continue;
                    for (int c = 0; c < 3; c++) px[i + c] = (byte)Math.Min(255, Math.Round(px[i + c] * 255.0 / a));
                }
                int hx = (int)Math.Floor(HotspotBase.X * n / Base), hy = (int)Math.Floor(HotspotBase.Y * n / Base);
                imagenes.Add((n, hx, hy, px));
                if (n == 32 || n == 64) GuardarPng(bmp, Path.Combine(outDir, $"{nombre}-{n}.png"));
                if (n == 32)
                {
                    var dv = new DrawingVisual();
                    RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.NearestNeighbor);
                    using (var dc = dv.RenderOpen())
                    {
                        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x1B, 0x1D, 0x23)), null, new Rect(0, 0, 256, 256));
                        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)), null, new Rect(256, 0, 256, 256));
                        dc.DrawImage(bmp, new Rect(0, 0, 256, 256));
                        dc.DrawImage(bmp, new Rect(256, 0, 256, 256));
                    }
                    var big = new RenderTargetBitmap(512, 256, 96, 96, PixelFormats.Pbgra32); big.Render(dv);
                    GuardarPng(big, Path.Combine(outDir, $"{nombre}-32-x8.png"));
                }
                Console.WriteLine($"{nombre}: {n}x{n} hotspot=({hx},{hy})");
            }
            File.WriteAllBytes(Path.Combine(outDir, nombre + ".cur"), EscribirCur(imagenes));
        }
        return 0;
    }

    static Geometry Capsula(Point a, Point b, double ancho)
    {
        // rectangulo redondeado orientado de a a b
        var v = b - a; double largo = v.Length; double ang = Math.Atan2(v.Y, v.X) * 180 / Math.PI;
        var g = new RectangleGeometry(new Rect(0, -ancho / 2, largo, ancho), ancho / 2, ancho / 2);
        var t = new TransformGroup();
        t.Children.Add(new RotateTransform(ang));
        t.Children.Add(new TranslateTransform(a.X, a.Y));
        g.Transform = t;
        return g;
    }

    static Geometry Union(params Geometry[] gs)
    {
        Geometry acc = gs[0];
        for (int i = 1; i < gs.Length; i++) acc = new CombinedGeometry(GeometryCombineMode.Union, acc, gs[i]);
        return acc.GetFlattenedPathGeometry(0.01, ToleranceType.Absolute);
    }

    // Mano cerrada vista por el dorso: palma, cuatro nudillos (dedos doblados) arriba y el
    // pulgar recogido por el lado izquierdo.
    static void DibujarMano(DrawingContext dc, bool prohibido)
    {
        var palma = new RectangleGeometry(new Rect(8.5, 12.5, 16.5, 13.5), 4.2, 4.2);
        var base_ = new RectangleGeometry(new Rect(10, 20, 13.5, 8.5), 4.5, 4.5);
        double[] xs = { 8.5, 12.6, 16.7, 20.8 };
        double[] tops = { 10.2, 9.0, 9.4, 10.6 };
        var dedos = new Geometry[4];
        for (int i = 0; i < 4; i++) dedos[i] = new RectangleGeometry(new Rect(xs[i], tops[i], 4.1, 8), 2.05, 2.05);
        var pulgar = Capsula(new Point(6.2, 15.2), new Point(11.5, 21.2), 4.6);
        var mano = Union(palma, base_, dedos[0], dedos[1], dedos[2], dedos[3], pulgar);

        var negro = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        var relleno = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var contorno = new Pen(negro, 1.45) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var linea = new Pen(negro, 1.1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        // sombra suave (como los cursores de Windows) para despegarlo de fondos claros
        var sombra = mano.Clone(); sombra.Transform = new TranslateTransform(0.8, 1.0);
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), 1.45), sombra);

        dc.DrawGeometry(relleno, contorno, mano);
        // separacion entre dedos (solo la parte alta, los nudillos)
        for (int i = 1; i < 4; i++)
        {
            double x = xs[i] - 0.0;
            double top = Math.Max(tops[i - 1], tops[i]) + 1.4;
            dc.DrawLine(linea, new Point(x, top), new Point(x, 15.4));
        }
        // pliegue del pulgar contra la palma
        dc.DrawLine(linea, new Point(9.6, 17.6), new Point(12.3, 20.6));

        if (prohibido)
        {
            var c = new Point(25.2, 25.2); double r = 5.4;
            var rojo = new SolidColorBrush(Color.FromRgb(0xD9, 0x1E, 0x1E));
            dc.DrawEllipse(Brushes.White, new Pen(negro, 1.0), c, r + 0.9, r + 0.9);
            dc.DrawEllipse(null, new Pen(rojo, 1.9), c, r - 0.9, r - 0.9);
            double d = (r - 0.9) * 0.7071;
            dc.DrawLine(new Pen(rojo, 1.9) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat }, new Point(c.X - d, c.Y - d), new Point(c.X + d, c.Y + d));
        }
    }

    static RenderTargetBitmap Rasterizar(int n, bool prohibido)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(n / Base, n / Base));
            DibujarMano(dc, prohibido);
            dc.Pop();
        }
        var rtb = new RenderTargetBitmap(n, n, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        return rtb;
    }

    static void GuardarPng(BitmapSource bmp, string ruta)
    {
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(ruta);
        enc.Save(fs);
    }

    static byte[] EscribirCur(List<(int lado, int hx, int hy, byte[] bgra)> imgs)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((ushort)0); w.Write((ushort)2); w.Write((ushort)imgs.Count);
        var datos = new List<byte[]>();
        foreach (var im in imgs) datos.Add(Dib(im.lado, im.bgra));
        int offset = 6 + 16 * imgs.Count;
        for (int i = 0; i < imgs.Count; i++)
        {
            var im = imgs[i];
            w.Write((byte)(im.lado >= 256 ? 0 : im.lado)); w.Write((byte)(im.lado >= 256 ? 0 : im.lado));
            w.Write((byte)0); w.Write((byte)0);
            w.Write((ushort)im.hx); w.Write((ushort)im.hy);
            w.Write(datos[i].Length); w.Write(offset);
            offset += datos[i].Length;
        }
        foreach (var d in datos) w.Write(d);
        return ms.ToArray();
    }

    static byte[] Dib(int n, byte[] bgra)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int maskStride = ((n + 31) / 32) * 4;
        w.Write(40); w.Write(n); w.Write(n * 2); w.Write((ushort)1); w.Write((ushort)32);
        w.Write(0); w.Write(n * n * 4 + maskStride * n); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
        for (int y = n - 1; y >= 0; y--) w.Write(bgra, y * n * 4, n * 4);
        for (int y = n - 1; y >= 0; y--)
        {
            var fila = new byte[maskStride];
            for (int x = 0; x < n; x++)
                if (bgra[(y * n + x) * 4 + 3] == 0) fila[x / 8] |= (byte)(0x80 >> (x % 8));
            w.Write(fila);
        }
        return ms.ToArray();
    }
}
