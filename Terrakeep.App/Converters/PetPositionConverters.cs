using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using Terrakeep.App.Services;

namespace Terrakeep.App.Converters;

// PortSeleccion Encargo3 (25-sep-2026): SUSTITUYE el Margin fijo/empirico del arreglo anterior
// (bitacora.md "Inicio, mascotas ocultas y banner sin hover", commits e4bf368c/119eedb4/
// 1770822e - Margin="-20,-16,0,0"/"-40,-32,0,0" ajustados a mano contra un umbral de "% de
// pixeles tapados") por la formula REAL de Terraria, decompilado real,
// Terraria/GameContent/UI/Elements/UICharacter.cs:
//
//   GetPlayerPosition (132-145): "if (_petProjectiles.Length != 0) result.X -= 10f;" - el
//     JUGADOR se retranquea 10px en X (no la mascota) cuando lleva mascota equipada, para
//     dejarle hueco real.
//   DrawPets (147-181): "playerPosition + new Vector2(0, player.height) + new Vector2(20, 0) +
//     new Vector2(0, -projectile.height)" - la mascota se ancla al borde INFERIOR del jugador
//     (Player.cs:57446-57447, height=42 real del hitbox de pie) desplazada 20px a la derecha de
//     la posicion YA retranqueada del jugador. Neto desde el origen sin retranquear: -10+20=+10px.
//
// Terrakeep no modela un hitbox aparte del sprite (PlayerPreviewRenderer.Render compone el
// personaje llenando el lienzo COMPLETO Width x Height, sin ningun margen de hitbox interno,
// a diferencia de Terraria real donde el hitbox de pie, 20x42, es mas pequeño que el frame
// visual, 40x56) - por eso aqui "el jugador"/"borde inferior del jugador" es directamente el
// propio lienzo del doll tal y como se dibuja (con su propio retranqueo de -10px ya aplicado
// via PetDollShiftXConverter, mas abajo). Confirmado con el propio canario HOMEBANNER_SOLO
// (25-sep-2026, bitacora.md): el doll deja solo 3-10px de margen realmente transparente en
// cada borde con Stretch=Uniform llenando el 100% del lienzo - los pies estan pegados al borde
// INFERIOR del lienzo, no a la mitad (que dejaria un hueco enorme de 14px, muy por encima del
// 3-10px medido) - "pies = borde inferior del lienzo" es una lectura fiel de la geometria real
// medida en este proyecto, no una aproximacion inventada.
//
// canvasScale (ConverterParameter, string): factor real lienzo nativo (40x56,
// PlayerPreviewRenderer.Width/Height) -> Grid WPF real de cada sitio donde se usa - "1.3" en la
// tarjeta pequeña (52x72,8 = 40x56 * 1.3, confirmado con los dos lados) y "2.6" en el banner
// "Continuar con X" (104x145,6 = 40x56 * 2.6, el mismo 2x ya documentado en bitacora.md). Sin
// escalar el offset por este factor, "20px de juego" se veria a poco mas de la mitad de grande
// de lo que toca en la tarjeta, y como si nada en el banner (2x mas grande sin escalar el
// offset con el) - mismo criterio en los dos sitios, pedido explicito del encargo.
//
// La mascota (Image de PetImage) sigue con Stretch="None" (tamaño NATIVO real, sin escalar el
// BITMAP en si - solo su POSICION se escala aqui) - por eso el alineado al borde inferior no
// puede multiplicar por canvasScale el alto nativo de la mascota (quedaria descuadrado: el
// bitmap no crece con el lienzo, solo se desplaza dentro de el). El alto REAL del Grid en
// unidades WPF (PlayerPreviewRenderer.Height * canvasScale, que coincide exactamente con el
// Height real puesto a mano en MainWindow.xaml - 56*1.3=72,8, 56*2.6=145,6) menos el
// PixelHeight nativo real de la mascota (WriteableBitmap.PixelHeight/BitmapImage.PixelHeight,
// nunca un numero inventado) da el margen superior real que deja el borde inferior de la
// mascota pegado al borde inferior real del Grid/lienzo.
public sealed class PetBottomAlignMarginConverter : IValueConverter
{
    public static readonly PetBottomAlignMarginConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string s || !double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var canvasScale))
            return new Thickness(0);

        // Neto real: -10 (retranqueo del jugador, GetPlayerPosition) + 20 (offset real de la
        // mascota, DrawPets) = +10px nativos desde el origen del lienzo SIN retranquear.
        double left = 10.0 * canvasScale;

        double top = 0;
        if (value is BitmapSource pet && pet.PixelHeight > 0)
        {
            double gridHeightWpf = PlayerPreviewRenderer.Height * canvasScale;
            top = Math.Max(0, gridHeightWpf - pet.PixelHeight);
        }

        return new Thickness(left, top, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Companero de PetBottomAlignMarginConverter: el propio retranqueo de -10px en X que
// GetPlayerPosition aplica al JUGADOR (no a la mascota) - "if (_petProjectiles.Length != 0)
// result.X -= 10f;" - CONDICIONADO a que de verdad haya mascota equipada (mismo dato que ya usa
// Visibility="{Binding PetImage, Converter={StaticResource NullToVis}}"). Se aplica via
// RenderTransform/TranslateTransform (no via Margin) a proposito: el Image del doll usa
// Stretch="Uniform" ajustado EXACTO al tamaño del Grid (misma proporcion 40:56 que 52:72,8 y
// 104:145,6) - un Margin negativo ahi cambiaria el tamaño real de la caja de layout que WPF le
// da al Image (HorizontalAlignment="Stretch" por defecto), rompiendo el ajuste 1:1 sin recorte
// que ya depende de esa proporcion exacta. RenderTransform, al aplicarse DESPUES del layout,
// desplaza el pintado sin tocar el tamaño/ajuste calculado, igual que ya hace
// PortSeleccion Encargo4 con PetOffsetX/PetOffsetY sobre la propia mascota.
public sealed class PetDollShiftXConverter : IValueConverter
{
    public static readonly PetDollShiftXConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BitmapSource || parameter is not string s || !double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var canvasScale))
            return 0.0;
        return -10.0 * canvasScale;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
