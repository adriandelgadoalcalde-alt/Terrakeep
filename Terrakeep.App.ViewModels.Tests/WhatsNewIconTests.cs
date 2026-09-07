using System.IO;
using System.Text;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels.Tests;

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
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.WhatsNewItemIds, Service.TooltipCatalogs);

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
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.WhatsNewItemIds, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.Null(item.IconPath); // "lo que no se encuentra no se inventa"
    }

    // C-17 (informe de pulido final, cierra la otra mitad de N1): "como en calamity mod y
    // tmodloader" - StatsTooltip usa la MISMA llamada exacta que LibraryViewModel
    // (ItemStatsFormatter.Describe(isCalamity, id, catalogs) + ItemStatsTextBuilder.Build), asi
    // que tiene que dar el MISMO texto real que ve la Libreria para ese mismo id.
    [Fact]
    public void StatsTooltip_MismoTextoRealQueLaTarjetaDeLaLibreriaParaElMismoId()
    {
        const string json = """
            [{"version":"test","items":[{"key":"IronBroadsword","es":"Espada larga de hierro"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.WhatsNewItemIds, Service.TooltipCatalogs);

        int ironBroadswordId = Service.VanillaCatalog.GetIdByKey("IronBroadsword")!.Value;
        string? esperado = Terrakeep.App.Services.ItemStatsTextBuilder.Build(
            ItemStatsFormatter.Describe(false, ironBroadswordId, Service.TooltipCatalogs));

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
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.WhatsNewItemIds, Service.TooltipCatalogs);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.Null(item.StatsTooltip);
    }

    // C-16 (informe de pulido final, cierra media N1): "Objetos nuevos" de 1.4.5.7 salian sin
    // sprite - vanilla_item_ids_by_key.json para en 5455, y este objeto real (ArcSurge=6173,
    // verificado a mano contra ItemID.cs) queda por encima. whats_new_item_ids.json (catalogo
    // separado, extraer-ids-novedades-vanilla.py) cierra ese hueco - SOLO aqui, nunca en la
    // Libreria/Investigacion/buscador real del personaje.
    [Fact]
    public void UnaClaveInternaDe1_4_5_ResuelveUnIconoRealViaElCatalogoSeparado()
    {
        const string json = """
            [{"version":"test","items":[{"key":"ArcSurge","es":"Sobrecarga de arco"}],"changes":[]}]
            """;
        var catalog = WhatsNewCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        var vm = new WhatsNewViewModel(catalog, catalog, Service.VanillaCatalog, Service.CalamityCatalog, Service.WhatsNewItemIds, Service.TooltipCatalogs);

        // Oleada del 6-sep-2026 (area Novedades): aqui habia un
        // `Assert.Null(Service.VanillaCatalog.GetIdByKey("ArcSurge"))` - "confirma que el
        // catalogo REAL no lo conoce, por eso hace falta el respaldo". Dejo de ser cierto en
        // cuanto vanilla_item_ids_by_key.json se amplio de 5455 a 6194 claves y paso a traer
        // ArcSurge con el MISMO id (6173, comprobado): el test se caia sin que nada estuviera
        // roto - afirmaba una AUSENCIA en un catalogo que crece, no un comportamiento. Lo que
        // hay que fijar es (a) que los dos catalogos coinciden en el id real cuando los dos lo
        // traen (si se desincronizan, esto salta) y (b) que la clave interna resuelve sprite por
        // el camino real de Novedades, venga del catalogo principal o del respaldo.
        Assert.Equal(6173, Service.WhatsNewItemIds.GetIdByKey("ArcSurge"));
        if (Service.VanillaCatalog.GetIdByKey("ArcSurge") is int idPrincipal)
            Assert.Equal(6173, idPrincipal);

        var item = vm.VanillaEntries[0].Items[0];

        Assert.NotNull(item.IconPath);
    }
}
