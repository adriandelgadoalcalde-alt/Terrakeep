using System.Globalization;
using System.Windows.Data;

namespace Terrakeep.App.Converters;

// Ronda de idioma del 6-sep-2026. `StringFormat` de un Binding es texto FIJO escrito en el XAML
// (no es una DependencyProperty, no se puede bindear) - por eso 19 plantillas reales de
// MainWindow.xaml se quedaron en español al migrar el resto de la interfaz, y ninguna cayo en el
// barrido de la ronda anterior: no son un Text="..." literal, van dentro del propio Binding.
// Ejemplos reales que se veian en español con la app en ingles: "Mundo: {0}", "Vaciados {0}
// objeto(s).", "Deshacer: {0} (Ctrl+Z)", "NPCs ({0})", "Estilo #{0}".
//
// Este conversor sustituye ese StringFormat por un MultiBinding: el PRIMER valor es la plantilla
// (que se bindea, tipicamente a Loc[clave], asi que se refresca sola al cambiar de idioma en
// caliente igual que cualquier otro texto), y el resto son los argumentos de string.Format en
// orden.
//
//   <MultiBinding Converter="{StaticResource LocFormat}">
//       <Binding Path="Loc[explore_world_title]" Mode="OneWay" />
//       <Binding Path="Exploration.WorldTitle" Mode="OneWay" />
//   </MultiBinding>
public sealed class LocalizedFormatConverter : IMultiValueConverter
{
    // ConverterParameter="fallback": sustituye al TargetNullValue de un Binding normal, que en un
    // MultiBinding no sirve para "el ARGUMENTO vino null". En ese modo values[0] es la plantilla
    // con dato, values[1] la plantilla de reserva y values[2...] los argumentos - si el primer
    // argumento no tiene valor se usa la de reserva, sin marcadores. Caso real: el tooltip de
    // Deshacer/Rehacer, "Deshacer: {0} (Ctrl+Z)" cuando hay algo que deshacer y "Deshacer
    // (Ctrl+Z)" a secas cuando no lo hay.
    public const string ModoFallback = "fallback";

    public object Convert(object?[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Durante la construccion del arbol visual WPF pasa DependencyProperty.UnsetValue por
        // los valores que todavia no se han resuelto - devolver la plantilla en crudo con "{0}"
        // a la vista seria peor que no pintar nada todavia.
        if (values.Length == 0 || values[0] is not string plantilla) return string.Empty;
        object?[] args = [.. values.Skip(1)];

        if (parameter as string == ModoFallback)
        {
            if (values.Length < 2 || values[1] is not string reserva) return string.Empty;
            args = [.. values.Skip(2)];
            if (args.Length == 0 || args[0] is null || args[0] as string == string.Empty) return reserva;
        }
        try
        {
            return string.Format(culture, plantilla, args);
        }
        catch (FormatException)
        {
            // Una plantilla con mas marcadores que argumentos reventaria la interfaz entera en
            // ejecucion. Mismo criterio de siempre: se ve el fallo (la plantilla en crudo), no
            // se inventa nada y no se tumba la app.
            return plantilla;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
