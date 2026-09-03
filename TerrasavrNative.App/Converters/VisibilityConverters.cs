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
