using System.Text.Json;
using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// Ronda de idioma del 6-sep-2026. Fija la regla real de eleccion de idioma del CONTENIDO
// bilingue (registro de cambios del editor, Novedades del juego/Calamity) y, sobre todo, que los
// ficheros de datos REALES que se reparten con la app traen de verdad su version inglesa - el
// bug que reporto el usuario no fue una regla mal escrita, fue que nadie la aplicaba.
public class LocalizedContentTests
{
    [Fact]
    public void Pick_EnIngles_DevuelveIngles()
        => Assert.Equal("Save", LocalizedContent.Pick("Guardar", "Save", "en"));

    [Fact]
    public void Pick_EnEspañol_DevuelveEspañol()
        => Assert.Equal("Guardar", LocalizedContent.Pick("Guardar", "Save", "es"));

    // Español es el idioma de REFERENCIA: una entrada sin traducir cae a español, nunca a vacio.
    [Fact]
    public void Pick_SinIngles_CaeAEspañol()
        => Assert.Equal("Guardar", LocalizedContent.Pick("Guardar", null, "en"));

    [Fact]
    public void Pick_InglesVacio_CuentaComoAusente()
        => Assert.Equal("Guardar", LocalizedContent.Pick("Guardar", "   ", "en"));

    [Fact]
    public void Pick_SoloIngles_LoUsaTambienEnEspañol()
        => Assert.Equal("Save", LocalizedContent.Pick(null, "Save", "es"));

    [Fact]
    public void PickList_ListaInglesaVacia_CaeEnteraAEspañol()
        => Assert.Equal(["uno", "dos"], LocalizedContent.PickList(["uno", "dos"], [], "en"));

    [Fact]
    public void PickList_ConIngles_LaUsaEntera()
        => Assert.Equal(["one"], LocalizedContent.PickList(["uno"], ["one"], "en"));

    // Oleada del 6-sep-2026 (area Novedades / Acerca de) - BUG REAL DE LA PRUEBA, no del codigo:
    // esto subia CUATRO niveles desde AppContext.BaseDirectory, o sea daba por hecho que la
    // salida compilada vive SIEMPRE en `<repo>\TerrasavrNative.Core.Tests\bin\Debug\net10.0\`.
    // El propio CLAUDE.md documenta el patron contrario como solucion estandar cuando `bin\Debug`
    // esta bloqueado (la app abierta del usuario, o varios arneses a la vez): compilar con
    // `-p:BaseOutputPath=<otra carpeta>`. Compilando asi, estos 6 tests reventaban con
    // DirectoryNotFoundException - 6 "fallos" que no son de la app y que tapan cualquier fallo de
    // verdad que aparezca al lado.
    //
    // CallerFilePath da la ruta del PROPIO fichero fuente, que incrusta el compilador: no depende
    // de donde se deje la salida, ni del directorio de trabajo, ni de la forma del arbol de bin.
    private static string RutaAsset(string nombre) =>
        Path.Combine(RaizDelRepo(), "TerrasavrNative.App", "Assets", nombre);

    private static string RaizDelRepo([System.Runtime.CompilerServices.CallerFilePath] string esteFichero = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(esteFichero)!, "..", ".."));

    // El fichero REAL que se reparte: cada version tiene que traer fecha, resumen y las dos
    // listas en ingles. Sin esto, "Acerca de" volveria a verse en español entero con la app en
    // ingles, que es justo lo que reporto el usuario.
    [Fact]
    public void ChangelogReal_TieneTodasLasVersionesTraducidasAlIngles()
    {
        var catalogo = ChangelogCatalog.LoadFromFile(RutaAsset("changelog.json"));
        Assert.NotEmpty(catalogo.Entries);
        foreach (var entrada in catalogo.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entrada.DateEn), $"{entrada.Version}: falta date_en");
            Assert.False(string.IsNullOrWhiteSpace(entrada.SummaryEn), $"{entrada.Version}: falta summary_en");
            Assert.Equal(entrada.Added.Count, entrada.AddedEn.Count);
            Assert.Equal(entrada.Fixed.Count, entrada.FixedEn.Count);
            // Y que de verdad se elija el ingles, no solo que este guardado.
            Assert.Equal(entrada.SummaryEn, entrada.SummaryFor("en"));
            Assert.Equal(entrada.Summary, entrada.SummaryFor("es"));
        }
    }

    [Theory]
    [InlineData("whats_new_vanilla.json")]
    [InlineData("whats_new_calamity.json")]
    public void NovedadesReales_TienenTodoElTextoEnIngles(string fichero)
    {
        var catalogo = WhatsNewCatalog.LoadFromFile(RutaAsset(fichero));
        Assert.NotEmpty(catalogo.Entries);
        foreach (var entrada in catalogo.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entrada.DateEn), $"{entrada.Version}: falta date_en");
            Assert.Equal(entrada.DateEn, entrada.DateFor("en"));
            if (!string.IsNullOrWhiteSpace(entrada.NoteEs))
                Assert.False(string.IsNullOrWhiteSpace(entrada.NoteEn), $"{entrada.Version}: falta note_en");
            foreach (var linea in entrada.Changes.Concat(entrada.Bugfixes))
            {
                Assert.False(string.IsNullOrWhiteSpace(linea.En), $"{entrada.Version}: linea sin ingles: {linea.Es}");
                Assert.Equal(linea.En, linea.TextFor("en"));
            }
            foreach (var objeto in entrada.Items)
                Assert.False(string.IsNullOrWhiteSpace(objeto.En), $"{entrada.Version}: objeto sin ingles: {objeto.Es}");
        }
    }

    // Los nombres de las etapas de Builds ("Pre-Hardmode (listo para el Muro de Carne)") vivian
    // SOLO en español dentro del propio builds.json - se veian tal cual con la app en ingles.
    [Theory]
    [InlineData("builds.json")]
    [InlineData("builds_calamity.json")]
    public void BuildsReales_TienenEtiquetaDeEtapaYNombresEnIngles(string fichero)
    {
        var catalogo = BuildsCatalog.LoadFromFile(RutaAsset(fichero));
        Assert.NotEmpty(catalogo.Stages);
        foreach (var etapa in catalogo.Stages)
        {
            Assert.False(string.IsNullOrWhiteSpace(etapa.LabelEn), $"{etapa.Key}: falta label_en");
            Assert.Equal(etapa.LabelEn, etapa.LabelFor("en"));
            Assert.Equal(etapa.Label, etapa.LabelFor("es"));
            foreach (var pieza in etapa.Classes.Values.SelectMany(c => c.Armor.Concat(c.Weapons).Concat(c.Accessories)))
                Assert.False(string.IsNullOrWhiteSpace(pieza.En), $"{etapa.Key}: pieza sin ingles: {pieza.Es}");
        }
    }

    // Las dos capas (interfaz y contenido) tienen que hablar del mismo idioma con el mismo
    // codigo - si alguien cambiara uno de los dos, esto lo caza antes de que la app elija mal.
    [Fact]
    public void CodigosDeIdioma_CoincidenConLosDeLaInterfaz()
    {
        Assert.Equal("es", LocalizedContent.Spanish);
        Assert.Equal("en", LocalizedContent.English);
    }

    // El JSON del changelog es escrito a mano en cada version: si se cuela una coma o una llave
    // de mas, la pestaña "Acerca de" se queda vacia sin ningun error visible.
    [Fact]
    public void ChangelogReal_EsJsonValido()
        => Assert.NotNull(JsonDocument.Parse(File.ReadAllText(RutaAsset("changelog.json"))));
}
