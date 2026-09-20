using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Data;
using Terrakeep.Core.Rendering;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.Services;

// Envoltorio fino de WPF (20-sep-2026, catalogo de funciones "7. ServidorKeep" - funcion 2, "mapa
// de mundo en vivo"): la logica real de mezcla de pixeles vivia aqui hasta esta misma noche y se
// movio a Terrakeep.Core.Rendering.WorldPixelRenderer (sin cambiar ni una linea del algoritmo)
// para que ServidorKeep pueda usarla sin arrastrar WPF - ver el comentario de cabecera de esa
// clase. Esta clase se queda solo con lo que de verdad es especifico de WPF: envolver el `byte[]`
// BGRA32 en bruto en un WriteableBitmap real, pintado UNA vez (luego se hace zoom/desplazamiento
// sobre el bitmap ya generado). Firma publica IDENTICA a la de antes - ningun llamador de
// Terrakeep.App (ExplorationViewModel.LoadFromPathAsync) tuvo que cambiar.
public static class WorldRenderer
{
    public static WriteableBitmap Render(WldWorld world, MapColorCatalog colors)
    {
        var rendered = WorldPixelRenderer.Render(world, colors);

        var bitmap = new WriteableBitmap(rendered.Width, rendered.Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, rendered.Width, rendered.Height), rendered.Pixels, rendered.Stride, 0);
        bitmap.Freeze();
        return bitmap;
    }
}
