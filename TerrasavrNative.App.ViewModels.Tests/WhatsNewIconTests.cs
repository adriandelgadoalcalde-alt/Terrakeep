using System.IO;
using System.Text;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), N-d: "los 'Objetos nuevos' son texto, no sprites - toda
// la app enseña sprites, aqui no". El whats_new.json real del proyecto describe una version
// FICTICIA (1.4.5.x, contenido sintetico propio para la demo) con nombres internos que no
// existen en el catalogo vanilla real - ninguna fila de la app actual muestra icono todavia,
// no porque el mecanismo este mal sino porque ninguna de esas claves es un objeto real. Esta
// prueba confirma el mecanismo en si con una clave vanilla REAL conocida.
public sealed class WhatsNewIconTests
{
    private static readonly CharacterFileService Service = new();

    [Fact]
    public void UnaClaveInternaVanillaRealResuelveUnIconoReal()
    {
        const string json = """
            [{"version":"test","items":[{"key":"IronBroadsword","es":"Espada larga de hierro"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, Service.VanillaCatalog);

        var item = vm.Entries[0].Items[0];

        Assert.Equal("Espada larga de hierro", item.DisplayName);
        Assert.NotNull(item.IconPath); // IronBroadsword es un objeto vanilla real (id 4)
    }

    [Fact]
    public void UnaClaveInternaDesconocida_NoInventaUnIcono()
    {
        const string json = """
            [{"version":"test","items":[{"key":"EstoNoExisteEnNingunCatalogoReal","es":"Objeto ficticio"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, Service.VanillaCatalog);

        var item = vm.Entries[0].Items[0];

        Assert.Null(item.IconPath); // "lo que no se encuentra no se inventa"
    }
}
