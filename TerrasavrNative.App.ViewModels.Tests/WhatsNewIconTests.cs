using System.IO;
using System.Text;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), N-d: "los 'Objetos nuevos' son texto, no sprites - toda
// la app enseña sprites, aqui no". Correccion real (2-sep-2026): el comentario anterior de esta
// prueba decia que whats_new.json describia una version "FICTICIA... contenido sintetico" - NO
// verificado nunca contra ninguna fuente real. Investigado a fondo esta vez (WebSearch/WebFetch
// contra terraria.wiki.gg): 1.4.5.7/1.4.5.8 SON versiones reales de Terraria vanilla (la fecha
// "2026" coincidia con la fecha real de esta sesion, no era una version futura inventada) -
// las claves internas de esos objetos concretos si son reales, solo que muchas no estan
// todavia en VanillaItemCatalog (extraido de una version anterior del juego) - de ahi que
// ninguna fila mostrara icono, no por ser ficticias.
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
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

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
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.Null(item.IconPath); // "lo que no se encuentra no se inventa"
    }

    // C-17 (informe de pulido final, cierra la otra mitad de N1): "como en calamity mod y
    // tmodloader" - StatsTooltip usa la MISMA llamada exacta que LibraryViewModel
    // (ItemStatsFormatter.Format(isCalamity, id, catalogs)), asi que tiene que dar el MISMO
    // texto real que ve la Libreria para ese mismo id.
    [Fact]
    public void StatsTooltip_MismoTextoRealQueLaTarjetaDeLaLibreriaParaElMismoId()
    {
        const string json = """
            [{"version":"test","items":[{"key":"IronBroadsword","es":"Espada larga de hierro"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.TooltipCatalogs);

        int ironBroadswordId = Service.VanillaCatalog.GetIdByKey("IronBroadsword")!.Value;
        string? esperado = ItemStatsFormatter.Format(false, ironBroadswordId, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.False(string.IsNullOrEmpty(esperado));
        Assert.Equal(esperado, item.StatsTooltip);
    }

    [Fact]
    public void StatsTooltip_ClaveDesconocida_EsNull()
    {
        const string json = """
            [{"version":"test","items":[{"key":"EstoNoExisteEnNingunCatalogoReal","es":"Objeto ficticio"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.Null(item.StatsTooltip);
    }
}
