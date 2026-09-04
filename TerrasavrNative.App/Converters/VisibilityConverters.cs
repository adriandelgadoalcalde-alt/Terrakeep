using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TerrasavrNative.App.Converters;

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
