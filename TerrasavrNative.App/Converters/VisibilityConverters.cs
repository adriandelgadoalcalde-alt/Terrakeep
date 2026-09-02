using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TerrasavrNative.App.Converters;

// true -> GridLength en estrella (peso real en ConverterParameter, ej. "3" o "1"), false ->
// Auto (colapsa a 0 real, sin hueco muerto) - consulta a Opus, octava pasada: "plegar la
// Libreria no devuelve NADA de espacio... la RowDefinition seguia siendo 1* con
// MinHeight=150" (bug real de la septima pasada, al cambiar Auto por "*" para resolver otro
// problema se perdio sin querer el colapso real a 0 de la fila). RowDefinition SI soporta
// bindings de instancia en WPF moderno (.NET 5+, no hace falta Style/DataTrigger).
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
// -> 0 - para que MinHeight/MaxHeight tambien colapsen de verdad (una RowDefinition en Auto
// con MinHeight=150 seguiria reservando 150px aunque su Height ya sea Auto).
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

public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Para la barra de paginacion real de la Libreria (Fase 2, octava pasada): PageCount nunca baja
// de 1 (Math.Max(1, ...)), asi que CountToVisibilityConverter (count > 0) la dejaria siempre
// visible - esta oculta la barra cuando todo cabe en una sola pagina.
public sealed class CountGreaterThanOneToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int count && count > 1 ? Visibility.Visible : Visibility.Collapsed;

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
