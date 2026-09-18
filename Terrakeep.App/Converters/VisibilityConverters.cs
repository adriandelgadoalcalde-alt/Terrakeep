using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Terrakeep.App.Converters;

// Segunda auditoria de Opus (Fable), B-1: recuperado de 83fd33c ("Octava pasada, Fase 1"),
// arreglo real que un git revert por rango demasiado ancho (c38c960) se llevo por delante -
// el rechazo del usuario en su momento era sobre la NAVEGACION de la Libreria (Fases 2/3/5/7),
// no sobre estos 2 bugs de layout, que ya estaban medidos y verificados aparte. true ->
// GridLength en estrella (peso real en ConverterParameter, ej. "2"), false -> Auto (colapsa a
// 0 real, sin hueco muerto) - RowDefinition SI soporta bindings de instancia en WPF moderno
// (.NET 5+, no hace falta Style/DataTrigger).
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && parameter is string s && double.TryParse(s, out var weight)
            ? new GridLength(weight, GridUnitType.Star)
            : GridLength.Auto;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Gemelo real de BoolToGridLengthConverter para una coleccion: recuento > 0 -> GridLength en
// estrella con el peso de ConverterParameter, vacia -> Auto. Nace del bug medido por AR-EX1
// (6-sep-2026): el bloque de resultados de Exploracion es Dock="Bottom" y un DockPanel se lo
// sirve ENTERO (363px medidos) antes de dejarle nada al contenido de la categoria, que se
// quedaba en 30-100px con resultados abiertos. La fila del bloque solo debe pedir su parte
// PROPORCIONAL cuando de verdad hay algo que enseñar; con la lista vacia vuelve a Auto y no
// roba un solo pixel. Existe aparte de BoolToGridLengthConverter porque el dato real disponible
// aqui es Count (ObservableCollection notifica su Count solo, no un bool derivado).
public sealed class CountToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int count && count > 0 && parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight)
            ? new GridLength(weight, GridUnitType.Star)
            : GridLength.Auto;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Companero de BoolToGridLengthConverter: true -> el numero real de ConverterParameter, false
// -> 0 - para que MinHeight tambien colapse de verdad (una RowDefinition en Auto con
// MinHeight=150 seguiria reservando 150px aunque su Height ya sea Auto).
public sealed class BoolToDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && parameter is string s && double.TryParse(s, out var n) ? n : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Inverso de NullToVisibilityConverter - null -> Visible, cualquier otra cosa -> Collapsed
// (para mostrar un "sin icono" de reserva justo cuando SI falta el dato).
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value == null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class EmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// H4-01 (cuarta auditoria de Opus, Fable): inverso de EmptyToCollapsedConverter - para el
// marcador de posicion ("Buscar...") superpuesto de los 3 buscadores reales (Libreria/Libreria
// de buffs/Investigacion), visible SOLO mientras el cuadro esta vacio.
public sealed class EmptyToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Inverso de BooleanToVisibilityConverter - true -> Collapsed, false -> Visible (para el
// mensaje "sin seleccion"/"no admite prefijos" del panel Editar compartido, que se muestra
// justo cuando la condicion contraria NO se cumple).
// Gemelo inverso de CountToVisibilityConverter (igual que NullToCollapsedConverter ya es el
// gemelo inverso de NullToVisibilityConverter) - Fase B (15-sep-2026): mensajes de "todavia no
// hay nada aqui" (pestaña Servidor: "sin mods reales"/"sin instancias activas") que solo deben
// verse cuando la coleccion esta VACIA, lo contrario de lo que ya resuelve CountToVis.
public sealed class CountToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int count && count > 0 ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// H5-10 (quinta auditoria de Opus): barras reales de vida/mana en la cabecera - una fraccion
// real 0..1 (Appearance.HealthFraction/ManaFraction) a un ancho real en pixeles (el maximo,
// ConverterParameter, es el ancho total real del Grid contenedor en el XAML - Width en si no
// admite bindings de fraccion directamente, solo un numero).
public sealed class FractionToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is double fraction && parameter is string s && double.TryParse(s, out var maxWidth)
            ? Math.Clamp(fraction, 0.0, 1.0) * maxWidth
            : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Punto 4 (advisor Opus, selector de categoria de Exploracion en fila de pildoras - ver
// ESPEC-ui-exploracion.md#9.1): enlaza un RadioButton.IsChecked de DOBLE VIA a un valor de enum
// (o de int, ej. ChestViewMode/ObjectsViewMode - los "modos de vista" 0/1/2 de Cofres/Objetos)
// usando el GroupName real de WPF para la exclusividad mutua - mas simple que una propiedad bool
// independiente por opcion o un Command en cada una. ConverterParameter es el valor como texto
// (ej. "Npcs" o "1"). Cuando WPF desmarca una opcion del grupo al marcar otra, ConvertBack
// recibe value=false - Binding.DoNothing (no false) para no pisar el valor real que SI se acaba
// de marcar en la misma pasada. targetType.IsEnum decide si se usa Enum.Parse o Convert.
// ChangeType - un enum con Enum.Parse en un int lanzaria (int no es un tipo enum).
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter as string;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not true || parameter is not string s) return Binding.DoNothing;
        try
        {
            return targetType.IsEnum ? Enum.Parse(targetType, s) : System.Convert.ChangeType(s, targetType, culture);
        }
        catch
        {
            return Binding.DoNothing;
        }
    }
}

// F-2 (auditoria de Opus vs TEdit, E-03): "los marcadores de resultado escalan con el zoom
// porque viven dentro del Grid con el ScaleTransform, que se aplica a TODO el subarbol - a
// 'Ajustar a la ventana' en un mundo Grande una elipse de 9px queda en menos de 1px". Opcion
// (a) del informe: un RenderTransform (no afecta al layout, asi que Canvas.Left/Top en
// coordenadas de tile se conserva gratis) con un ScaleTransform inverso al zoom del mapa,
// puesto DIRECTAMENTE en cada marcador - se des-escala a si mismo dentro del subarbol ya
// escalado. 1/0 (Zoom nunca deberia llegar a 0, MinZoom=0.02) se protege igualmente.
// P-4 (auditoria de Opus vs TEdit): "Cargar personaje" y "Guardar" son los dos rellenos de la
// barra superior y compiten visualmente - Cargar es una accion de arranque, Guardar es la
// consecuencia de todo el trabajo. Baja a boton normal (Tag=null) en cuanto hay personaje
// cargado, y solo mantiene el acento (ConverterParameter, ej. "Accent") mientras no lo hay.
public sealed class FalseToTagConverter : IValueConverter
{
    // Mismo bug real de WPF ya encontrado con InverseValueConverter (ver su comentario): un
    // StaticResource usado como Binding.Converter puede fallar en tiempo de ejecucion (aqui,
    // literalmente al arrancar la ventana - ni siquiera hacia falta un DataTemplate virtualizado
    // de por medio) pese a compilar sin error. x:Static con una instancia estatica se resuelve
    // en tiempo de compilacion y no depende de este mecanismo.
    public static readonly FalseToTagConverter Instance = new();

    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is false ? parameter as string : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// F-10 (auditoria de Opus vs TEdit, E-10): ColumnDefinition.Width es GridLength, no double -
// bidireccional de verdad (ConvertBack real) para que arrastrar el GridSplitter escriba el
// ancho nuevo de vuelta en la propiedad persistida (Settings.ExplorationSidebarWidth).
public sealed class DoubleToGridLengthConverter : IValueConverter
{
    public static readonly DoubleToGridLengthConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        new GridLength(value is double d ? d : 0);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is GridLength g ? g.Value : 0.0;
}

// Fase B (15-sep-2026): la pestaña Servidor pinta el estado de cada instancia (En escucha/
// Arrancando/Fallida/Detenida) con el color real del tema - HostingInstanciaViewModel decide
// la CLAVE del pincel (string, ej. "EquippedGreenBrush") desde C# porque ServidorKeep.Core no
// conoce nada de WPF; este converter la resuelve al Brush real de Styles/Theme.xaml en el
// momento de pintar, igual que cualquier StaticResource normal de la app.
public sealed class ResourceKeyToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is string key && System.Windows.Application.Current?.TryFindResource(key) is System.Windows.Media.Brush brush
            ? brush
            : System.Windows.Media.Brushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Bug real reportado por el usuario con captura (19-sep-2026, tercer reporte de "el marcador no
// cae donde deberia"): todos los marcadores del mapa de Exploracion anclaban en la ESQUINA
// superior izquierda de su tile (Canvas.Left="{Binding TileX}" a secas), no en el centro de la
// celda. Media casilla de desfase constante, pequeño pero real y muy visible con zoom alto, que
// se sumaba al bug grande del pivote (ver el comentario de MainWindow.xaml en el marcador de
// resultado de busqueda).
//
// El criterio correcto se comprobo contra el codigo REAL de TEdit (github.com/TEdit/
// Terraria-Map-Editor, MIT), `src/TEdit/View/WorldRenderXna.xaml.cs`, metodo DrawFindCrosshair:
// convierte tile -> pantalla con `(scroll + tile + 0.5f) * zoom` y solo DESPUES resta media
// anchura del marcador, ya en pixeles de pantalla. El `+ 0.5` es exactamente esto: el centro de
// la celda del tile, no su esquina. No es codigo portado (TEdit dibuja con SpriteBatch/XNA y
// Terrakeep posiciona elementos WPF en un Canvas), solo el mismo criterio de anclaje.
//
// Instancia estatica + x:Static por el mismo motivo real ya documentado en InverseValueConverter
// aqui abajo: estas plantillas viven en ItemsControl virtualizados y un StaticResource como
// Converter revienta en caliente.
public sealed class TileCenterConverter : IValueConverter
{
    public static readonly TileCenterConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        int i => i + 0.5,
        double d => d + 0.5,
        _ => 0.0
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseValueConverter : IValueConverter
{
    // Bug real encontrado al verificar (no en teoria): un StaticResource usado como
    // Binding.Converter (una propiedad CLR de Binding, no una DependencyProperty) DENTRO de un
    // DataTemplate de un ItemsControl fuertemente virtualizado (Npcs/CharacterSpawns/
    // WorldSearchResults, contenedores creados/reciclados en caliente) fallaba en tiempo de
    // ejecucion con XamlParseException "No se puede encontrar el recurso" pese a que el mismo
    // StaticResource resolvia bien en cualquier otra propiedad de la misma plantilla - patron
    // conocido de WPF con la carga "optimizada" de contenido de plantilla y MarkupExtensions
    // anidadas. Fix real: instancia estatica referenciada por x:Static, que se resuelve en
    // tiempo de compilacion y no depende del ambito de recursos del contenedor en el momento de
    // la realizacion.
    public static readonly InverseValueConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is double d && d != 0 ? 1.0 / d : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
