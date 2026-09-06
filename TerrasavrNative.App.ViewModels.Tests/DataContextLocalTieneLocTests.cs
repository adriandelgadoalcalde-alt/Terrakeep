using System.IO;
using System.Text.RegularExpressions;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// OBJ-01 (oleada de pruebas de Personaje -> Objetos, 6-sep-2026). BUG REAL encontrado midiendo,
// no una precaucion teorica: la ronda de idioma del 6-sep sustituyo los literales del XAML por
// `{Binding Loc[clave]}`, pero en los sitios donde el propio XAML CAMBIA el DataContext local
// (`DataContext="{Binding EquipmentGroup}"` / `"{Binding StorageGroup}"`) ese `Loc` deja de
// resolverse contra MainViewModel y pasa a buscarse en el ViewModel del selector - que no lo
// tenia. WPF no avisa de nada en ese caso (deja Content=null / Text=""), asi que el resultado
// real medido con el arnes fue:
//
//   - los 5 botones de la cabecera de Almacenes ("Guardar conjunto...", "Cargar...",
//     "Añadir...", "Ordenar", "Vaciar contenedor") reducidos a 12px de puro padding, SIN texto,
//     en los dos idiomas y a cualquier tamaño de ventana - o sea, imposible guardar/cargar un
//     conjunto del Banco/Caja fuerte/Fragua/Boveda, ni ordenarlo ni vaciarlo;
//   - las etiquetas "Conjunto:" y "Vista:" de Equipamiento vacias, y "Defensa total:" tambien,
//     dejando el numero de defensa suelto en pantalla sin decir de que era.
//
// Esta prueba va a proposito contra el XAML REAL (no contra una copia de muestra): recorre cada
// `DataContext="{Binding X}"` que declara MainWindow.xaml y comprueba por reflexion que el tipo
// que hay detras expone de verdad todo lo que ese subarbol le pide. Lo que hay que impedir que
// vuelva no es que ItemSlotViewModel sepa exponer Loc, es que un DataContext local nuevo se
// quede sin el y nadie se entere hasta que un usuario reporte "no salen los botones".
public class DataContextLocalTieneLocTests
{
    private const string MainWindowXamlPath =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\MainWindow.xaml";

    // Los ViewModel a los que el XAML redirige el DataContext hoy, por el nombre de la propiedad
    // de MainViewModel que aparece en `DataContext="{Binding X}"`.
    private static readonly Dictionary<string, Type> TiposPorPropiedad = new()
    {
        ["EquipmentGroup"] = typeof(EquipmentGroupViewModel),
        ["StorageGroup"] = typeof(StorageGroupViewModel),
    };

    [Fact]
    public void CadaDataContextLocalDelXamlExponeLoc()
    {
        string xaml = File.ReadAllText(MainWindowXamlPath);
        var propiedades = Regex.Matches(xaml, @"DataContext=""\{Binding (?<p>[A-Za-z0-9_.]+)\}""")
            .Select(m => m.Groups["p"].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(propiedades); // si el XAML deja de tener ninguno, esta prueba ya no vigila nada

        foreach (string propiedad in propiedades)
        {
            // Solo se comprueban los DataContext que apuntan a un ViewModel propio conocido; el
            // XAML tambien usa `DataContext="{Binding Path=PlacementTarget.DataContext, ...}"`
            // en tooltips/menus contextuales, que no es un tipo fijo y ya se resuelve aparte.
            if (!TiposPorPropiedad.TryGetValue(propiedad, out var tipo)) continue;

            var loc = tipo.GetProperty("Loc");
            Assert.True(loc != null,
                $"MainWindow.xaml cambia el DataContext a {tipo.Name} ({propiedad}) y ese subarbol usa " +
                $"{{Binding Loc[clave]}}: sin una propiedad publica Loc, WPF deja los Content/Text vacios SIN ningun error visible.");
            Assert.Equal(typeof(LocalizationService), loc!.PropertyType);
        }
    }

    [Theory]
    // Las claves reales que usan esos dos subarboles del XAML (cabecera de Almacenes y selector
    // de Equipamiento) - si alguna desapareciera del diccionario, el indexador devuelve
    // "[clave]" entre corchetes y esta prueba lo caza igual.
    [InlineData("action_save_set")]
    [InlineData("action_load_ellipsis")]
    [InlineData("action_add_ellipsis")]
    [InlineData("action_sort")]
    [InlineData("action_empty_container")]
    [InlineData("char_loadout_label")]
    [InlineData("char_view_label")]
    [InlineData("char_total_defense")]
    public void LasClavesRealesDeEsosSubarbolesResuelvenEnLosDosIdiomas(string clave)
    {
        var loc = LocalizationService.Instance;
        string idiomaOriginal = loc.Language;
        try
        {
            foreach (string idioma in new[] { LocalizationService.Spanish, LocalizationService.English })
            {
                loc.SetLanguage(idioma);
                string texto = loc[clave];
                Assert.False(string.IsNullOrWhiteSpace(texto));
                Assert.False(texto.StartsWith('[') && texto.EndsWith(']'), $"'{clave}' no existe en el diccionario '{idioma}'");
            }
        }
        finally { loc.SetLanguage(idiomaOriginal); }
    }
}
