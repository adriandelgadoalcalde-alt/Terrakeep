using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Layout;

namespace Terrakeep.App.Controls;

// GapAnalysis ParidadPersonaje (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514) -
// segunda fase del patron de 2 agentes: arquitecto-keep diseño esta arquitectura por completo
// (motor puro PlayerPetPreviewLayout.cs + este control WPF que lo consume) antes de este arreglo,
// verificada con el caso oracle real "Chester" (informe de la investigacion) - este control aplica
// ese diseño tal cual.
//
// SUSTITUYE el mecanismo anterior de Margin/RenderTransform repartido entre 2 converters
// (Converters/PetPositionConverters.cs, PetBottomAlignMarginConverter/PetDollShiftXConverter) por
// UN SOLO control parametrizado por CanvasScale que calcula las posiciones reales con
// PlayerPetPreviewLayout (Terrakeep.Core, sin ningun tipo de WPF) - reutilizado tal cual en los 2
// sitios reales de MainWindow.xaml (tarjeta pequeña de Inicio, banner hero "Continuar con X"), sin
// duplicar logica de posicionamiento entre los dos.
//
// CAUSA del bug real que este control cierra ("Chester parece un suelo/plataforma bajo los pies"):
// el converter anterior ya calculaba la posicion RELATIVA mascota-vs-doll correctamente, pero el
// CONTENEDOR (Grid de 40x56 nativo escalado) nunca reservaba hueco de sobra para que una mascota
// mas ancha que el propio doll (hasta 90 unidades nativas de ancho compuesto real, ver
// PlayerPetPreviewLayout.ReserveColumnWidthNative) no se recortase contra el borde de la tarjeta -
// este control fija su propio ancho (Width) a ese ancho de reserva real, siempre, en reposo y en
// hover (decision final del usuario, 26-sep-2026: ancho FIJO permanente, sin estados especiales).
//
// Mismo orden Z real que Terraria vanilla (Terraria/GameContent/UI/Elements/UICharacter.cs,
// DrawSelf: "DrawPets(spriteBatch);" ANTES que "Main.PlayerRenderer.DrawPlayer(...)") - la mascota
// se añade PRIMERO a Children (queda detras), el jugador SEGUNDO (queda delante).
//
// canvasScale (unico parametro de layout externo, pedido explicito del encargo - "nunca un
// Margin/offset nuevo a mano"): factor real lienzo nativo (40x56, SpriteFrameWidth/Height) ->
// tamaño real en pixeles WPF de cada sitio donde se usa - 1.3 en la tarjeta pequeña, 2.6 en el
// banner hero (mismos valores reales ya documentados en el resto del proyecto).
//
// A diferencia del converter anterior, aqui la mascota SI escala su propio tamaño con
// canvasScale (no solo su posicion) - decision deliberada de esta arquitectura nueva (el motor
// puro compone TODO en unidades nativas y el resultado YA COMPUESTO se escala una sola vez,
// uniformemente, ver el comentario de cabecera de PlayerPetPreviewLayout.cs) - un cambio de
// comportamiento real respecto al Stretch="None" anterior, documentado aqui con claridad, no
// oculto.
public sealed class PlayerPetPreviewControl : Canvas
{
    private readonly Image _petImage;
    private readonly Image _playerImage;

    public PlayerPetPreviewControl()
    {
        ClipToBounds = true; // salvaguarda real: si algun caso futuro del catalogo superase
                              // ReserveColumnWidthNative (el canario PlayerPetPreviewCatalogWidthTests.cs
                              // lo detectaria antes), el recorte cae aqui, contra el borde de este
                              // control (espacio ya reservado para el preview), nunca contra el
                              // texto vecino de la tarjeta.

        _petImage = new Image { Stretch = Stretch.Fill, RenderTransformOrigin = new Point(0.5, 0.5) };
        RenderOptions.SetBitmapScalingMode(_petImage, BitmapScalingMode.NearestNeighbor);

        _playerImage = new Image { Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(_playerImage, BitmapScalingMode.NearestNeighbor);

        Children.Add(_petImage);
        Children.Add(_playerImage);

        RecomputeLayout();
    }

    public static readonly DependencyProperty CanvasScaleProperty = DependencyProperty.Register(
        nameof(CanvasScale), typeof(double), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(1.0, OnLayoutAffectingPropertyChanged));

    public double CanvasScale
    {
        get => (double)GetValue(CanvasScaleProperty);
        set => SetValue(CanvasScaleProperty, value);
    }

    public static readonly DependencyProperty PreviewSourceProperty = DependencyProperty.Register(
        nameof(PreviewSource), typeof(ImageSource), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(null, OnLayoutAffectingPropertyChanged));

    public ImageSource? PreviewSource
    {
        get => (ImageSource?)GetValue(PreviewSourceProperty);
        set => SetValue(PreviewSourceProperty, value);
    }

    public static readonly DependencyProperty PetImageSourceProperty = DependencyProperty.Register(
        nameof(PetImageSource), typeof(ImageSource), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(null, OnLayoutAffectingPropertyChanged));

    public ImageSource? PetImageSource
    {
        get => (ImageSource?)GetValue(PetImageSourceProperty);
        set => SetValue(PetImageSourceProperty, value);
    }

    public static readonly DependencyProperty PetOffsetXProperty = DependencyProperty.Register(
        nameof(PetOffsetX), typeof(double), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(0.0, OnLayoutAffectingPropertyChanged));

    public double PetOffsetX
    {
        get => (double)GetValue(PetOffsetXProperty);
        set => SetValue(PetOffsetXProperty, value);
    }

    public static readonly DependencyProperty PetOffsetYProperty = DependencyProperty.Register(
        nameof(PetOffsetY), typeof(double), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(0.0, OnLayoutAffectingPropertyChanged));

    public double PetOffsetY
    {
        get => (double)GetValue(PetOffsetYProperty);
        set => SetValue(PetOffsetYProperty, value);
    }

    public static readonly DependencyProperty PetSpriteDirectionProperty = DependencyProperty.Register(
        nameof(PetSpriteDirection), typeof(double), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(1.0, OnLayoutAffectingPropertyChanged));

    public double PetSpriteDirection
    {
        get => (double)GetValue(PetSpriteDirectionProperty);
        set => SetValue(PetSpriteDirectionProperty, value);
    }

    public static readonly DependencyProperty PetRotationDegreesProperty = DependencyProperty.Register(
        nameof(PetRotationDegrees), typeof(double), typeof(PlayerPetPreviewControl),
        new FrameworkPropertyMetadata(0.0, OnLayoutAffectingPropertyChanged));

    public double PetRotationDegrees
    {
        get => (double)GetValue(PetRotationDegreesProperty);
        set => SetValue(PetRotationDegreesProperty, value);
    }

    private static void OnLayoutAffectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PlayerPetPreviewControl)d).RecomputeLayout();

    private void RecomputeLayout()
    {
        double scale = CanvasScale > 0 ? CanvasScale : 1.0;

        bool hasPet = PetImageSource is BitmapSource { PixelWidth: > 0, PixelHeight: > 0 };
        var hitboxOrigin = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet);
        var playerBounds = PlayerPetPreviewLayout.PlayerSpriteBounds(hitboxOrigin);

        // Ancho de reserva REAL, fijo, permanente (decision final del usuario, 26-sep-2026) - sin
        // margen vertical adicional en esta ronda (el encargo, en sus 3 refinamientos sucesivos,
        // hablo siempre de ANCHO/columna - el alto se deja igual que siempre, SpriteFrameHeight*
        // escala; una mascota que sobresalga por ARRIBA queda documentada como hallazgo aparte,
        // no corregido aqui, ver bitacora.md).
        Width = PlayerPetPreviewLayout.ReserveColumnWidth(scale);
        Height = PlayerPetPreviewLayout.SpriteFrameHeight * scale;

        _playerImage.Source = PreviewSource;
        _playerImage.Width = PlayerPetPreviewLayout.SpriteFrameWidth * scale;
        _playerImage.Height = PlayerPetPreviewLayout.SpriteFrameHeight * scale;
        // El jugador SIEMPRE se ancla en el borde izquierdo/superior de este control -
        // ReserveColumnWidthNative ya reserva hueco de sobra a la derecha para cualquier mascota
        // real del catalogo, sin necesitar desplazar al propio jugador dentro del control.
        SetLeft(_playerImage, 0);
        SetTop(_playerImage, 0);

        if (hasPet && PetImageSource is BitmapSource petBitmap)
        {
            var petBoundsNative = PlayerPetPreviewLayout.PetBounds(hitboxOrigin, PetOffsetX, PetOffsetY, petBitmap.PixelWidth, petBitmap.PixelHeight);

            _petImage.Source = petBitmap;
            _petImage.Width = petBitmap.PixelWidth * scale;
            _petImage.Height = petBitmap.PixelHeight * scale;
            SetLeft(_petImage, (petBoundsNative.Left - playerBounds.Left) * scale);
            SetTop(_petImage, (petBoundsNative.Top - playerBounds.Top) * scale);

            // Espejo horizontal (PetSpriteDirection, casi siempre -1) + giro (PetRotationDegrees,
            // solo mascotas con delegado de codigo custom, ver PetCustomAnimationCode) alrededor
            // del propio centro del fotograma - el offset (PetOffsetX/Y) YA esta incorporado en la
            // posicion real de arriba (PetBounds), no como un TranslateTransform aparte como hacia
            // el converter anterior.
            _petImage.RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(PetSpriteDirection, 1),
                    new RotateTransform(PetRotationDegrees),
                },
            };
            _petImage.Visibility = Visibility.Visible;
        }
        else
        {
            _petImage.Source = null;
            _petImage.Visibility = Visibility.Collapsed;
        }
    }
}
