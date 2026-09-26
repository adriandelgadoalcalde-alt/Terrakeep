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

        // CORRECCION (26-sep-2026, investigador-bug + aplicador-fix, sesion
        // d38ffe35-118f-4719-b326-0ca425888fe7): el -10+20=+10 de la nota de cabecera (lineas
        // 15-21) simplifica de mas al asumir "el jugador ES directamente el propio lienzo del
        // doll" (el borde izquierdo del Grid = playerPosition.X, la misma posicion que ya usa
        // GetPlayerPosition/DrawPets). En realidad GetPlayerPosition centra el HITBOX real (20
        // de ancho, _player.width), no el sprite visual (40 de ancho, el mismo lienzo nativo de
        // PlayerPreviewRenderer que SI representa Terrakeep) - y DrawPlayer dibuja ese sprite de
        // 40 SIEMPRE centrado sobre ese mismo hitbox de 20, con o sin mascota: el borde izquierdo
        // del sprite (= borde izquierdo del lienzo/Grid de Terrakeep, SIN aplicar aqui ningun
        // retranqueo, eso ya lo hace el doll por su lado via PetDollShiftXConverter) equivale a
        // "playerPosition.X - 10" (sin mascota, sin retranqueo). La mascota (DrawPets) se ancla
        // en "playerPosition.X + 20" con la mascota SI puesta (playerPosition.X ya retranqueado
        // -10 en ese caso) = ("borde del lienzo" + 10) + 20 = "borde del lienzo" + 30 en
        // coordenadas de playerPosition... pero como el retranqueo de -10 lo aplica el DOLL (no
        // este margin), la mascota debe leerse desde el borde SIN retranquear = borde_lienzo +
        // 20 nativos - de ahi el 20, no el 10 que habia aqui (perdia justo el hueco de 10 entre
        // hitbox y sprite visual). Corroborado por: (1) esta re-derivacion campo a campo del
        // decompilado (TerrariaVanilla\Terraria\GameContent\UI\Elements\UICharacter.cs,
        // GetPlayerPosition:122-130 + DrawPets:132-149), (2) la evidencia visual real con los
        // assets de produccion documentada en Terrakeep.App.ViewModels.Tests/
        // PetPositionConvertersTests.cs (cabecera) - con 10 "Chester" queda oculto casi entero
        // detras del doll, con 20 se ve reconocible al lado de los pies -, y (3) el propio mensaje
        // del commit que introdujo este converter (7058c42b) - "la mascota se ancla al borde
        // inferior del doll desplazada 20px a la derecha". NOTA para una ronda futura (fuera del
        // alcance de este arreglo): el canario manual Terrakeep.App.Tests/
        // CanarioHomeBannerMascota.cs (HOMEBANNER_SOLO, VerificarFormulaRealMascota linea 211)
        // compara offsetXReal (mascota MENOS doll, doll ya retranqueado) contra un
        // "offsetXEsperado" hardcodeado a 20.0*canvasScale+petOffsetX - esa comparacion relativa
        // arrastra el MISMO hueco de 10 nativos que este converter tenia (nunca conto el
        // retranqueo del doll como ya restado), asi que con este arreglo (20 aqui) ese canario
        // pasara a marcar FALLO de forma esperada hasta que se actualice a
        // 30.0*canvasScale+petOffsetX en un pase aparte - no se toca aqui porque cae fuera del
        // working set de este encargo (solo esta linea 63).
        double left = 20.0 * canvasScale;

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
