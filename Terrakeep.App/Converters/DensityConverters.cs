using System.Globalization;
using System.Windows.Data;

namespace Terrakeep.App.Converters;

// Modo compacto (encargo de pulido visual, 17-sep-2026): opcion real en Ajustes
// (Settings.IsCompactMode, NUNCA activada por defecto) que reduce el hueco (Gap) y el tamaño de
// celda real de SlotGridPanel en las 4 rejillas reales de la app (Inventario/Almacenes/
// Equipamiento/Monturas/Monedas via ContainerCompactTemplate, la rejilla de Buffs equivalente,
// y las dos rejillas de resultados de catalogo - Libreria de objetos y Libreria de buffs) para
// que quepa mas contenido de golpe sin romper ningun texto/control (SlotGridPanel sigue
// calculando su propia celda real entre MinCell/MaxCell, solo que con un techo/suelo mas bajos).
//
// Factor fijo 0.8: MinCell/MaxCell YA varian por contenedor (40/90 universal, 32/56 para
// Mascota/Montura/Tinte - ver ContainerViewModel.MinCell/MaxCell) - un factor RELATIVO respeta
// esa variacion en vez de forzar el mismo numero absoluto para todos. Medido contra el suelo de
// legibilidad ya documentado ahi (40px de celda = ~21px de sprite reconocible): 40*0.8=32,
// 90*0.8=72, 32*0.8~26, 56*0.8~45 - todos por encima del piso real donde un sprite mediano deja
// de reconocerse (confirmado en la auditoria de esta misma pasada, ver bitacora.md 17-sep-2026).
public sealed class CompactCellSizeConverter : IValueConverter
{
    public static readonly CompactCellSizeConverter Instance = new();
    internal const double Factor = 0.8;

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        double baseValue = parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : 0.0;
        return value is true ? Math.Round(baseValue * Factor) : baseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Gemelo de CompactCellSizeConverter para MinCell/MaxCell cuando el valor base NO es un literal
// fijo del propio XAML, sino que viene de ContainerViewModel.MinCell/MaxCell (linea 689 real de
// MainWindow.xaml, el unico de los 4 usos de SlotGridPanel que enlaza a un valor de instancia en
// vez de un numero fijo) - hace falta combinar dos fuentes de datos distintas (el valor real del
// contenedor Y Settings.IsCompactMode), de ahi el MultiBinding/IMultiValueConverter en vez del
// IValueConverter de un solo valor + ConverterParameter que basta para los otros 3 usos.
public sealed class CompactCellSizeMultiConverter : IMultiValueConverter
{
    public static readonly CompactCellSizeMultiConverter Instance = new();

    public object Convert(object?[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double baseValue = values.Length > 0 && values[0] is double d ? d : 0.0;
        bool isCompact = values.Length > 1 && values[1] is true;
        return isCompact ? Math.Round(baseValue * CompactCellSizeConverter.Factor) : baseValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Hueco (Gap) real entre celdas de SlotGridPanel - los 4 usos reales de la app ya comparten el
// mismo valor base (Gap="4"), asi que un IValueConverter simple (sin MultiBinding) basta: normal
// 4px, compacto 2px - a juego con el mismo factor ~0.5 que ya usa el resto del modo compacto
// para separaciones (ver los DataTrigger de Padding de CategoryNodeTemplate/BuildItemRowViewModel
// en MainWindow.xaml).
public sealed class CompactGapConverter : IValueConverter
{
    public static readonly CompactGapConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? 2.0 : 4.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
