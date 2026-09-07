using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Bug real reportado por el usuario (6-sep-2026, captura de Exploracion -> Cofres -> "Por tipo de
// cofre" sobre un mundo real): entre "Cofre de oro 184" y "Cofre de agua 43" salia
// "Wooden Chest 45" en INGLES, en medio de una interfaz entera en español.
//
// Causa real: el generador de tile_names.json (repo hermano) cruza el nombre INGLES de cada
// variante de sprite segun TEdit contra la seccion ItemName de la localizacion es-ES real del
// juego, por coincidencia EXACTA de texto - y hay tres formas de que ese cruce falle, todas en la
// familia de contenedores: TEdit usa un nombre coloquial que el juego no usa ("Wooden Chest" vs
// ItemName.Chest = "Cofre", "Wooden Dresser" vs ItemName.Dresser = "Aparador"), TEdit tiene una
// errata en su propio dato ("Web Coverd Chest", sin la 'e' de Covered) o el nombre es una
// CATEGORIA de TEdit que no existe como objeto del juego ("Chests", "Dressers").
//
// scripts/parchear-nombres-contenedores-es.js lo corrige sobre el asset real. Estas pruebas van a
// PROPOSITO contra ese fichero real (no contra un JSON de muestra, que es lo que ya cubre
// TileNameCatalogTests): lo que hay que impedir que vuelva es que el asset publicado se quede sin
// esas traducciones, no que el lector sepa leerlas.
public class NombresContenedoresEsTests
{
    private const string TileNamesPath =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\tile_names.json";

    private static TileNameCatalog Catalogo() => TileNameCatalog.LoadFromFile(TileNamesPath);

    [Theory]
    // tile, u, v, nombre real en español (fuente citada)
    [InlineData(21, 0, 0, "Cofre")]                            // ItemName.Chest - el cofre de madera de toda la vida
    [InlineData(21, 540, 0, "Cofre cubierto de telarañas")]    // ItemName.WebCoveredChest
    [InlineData(88, 0, 0, "Aparador")]                         // ItemName.Dresser
    [InlineData(21, 36, 0, "Cofre de oro")]                    // ya venia del generador: no se ha roto
    [InlineData(88, 486, 0, "Aparador de obsidiana")]          // idem - el "parador" que el usuario vio en su mundo
    public void VarianteDeContenedor_TieneNombreEnEspañol(int tipo, short u, short v, string esperado)
    {
        Assert.Equal(esperado, Catalogo().TileVariantName(tipo, u, v));
    }

    [Theory]
    // Los nombres BASE (respaldo cuando un cofre trae un frame que el catalogo no conoce, p.ej.
    // uno de un mod sobre el tile 21) tambien tienen que estar en español.
    [InlineData(21, "Cofres")]
    [InlineData(88, "Aparadores")]
    [InlineData(467, "Cofres (grupo 2)")]
    [InlineData(441, "Cofres atrapados")]
    [InlineData(468, "Cofres atrapados (grupo 2)")]
    public void NombreBaseDeContenedor_EstaEnEspañol(int tipo, string esperado)
    {
        Assert.Equal(esperado, Catalogo().TileName(tipo));
    }

    [Fact]
    public void NingunaVarianteDeContenedor_SeQuedaEnIngles()
    {
        var catalogo = Catalogo();
        // Los cinco tiles que Terraria guarda en su lista de cofres (Chest.CreateChest): cofres,
        // cofres del grupo 2, sus dos gemelos con trampa y los aparadores. Barriles y papeleras son
        // frames del propio tile 21, asi que ya entran aqui.
        int[] contenedores = [21, 88, 441, 467, 468];
        string[] enIngles = ["Wooden Chest", "Web Coverd Chest", "Wooden Dresser", "Chests", "Dressers", "Chests (Group 2)", "Trapped Chests", "Trapped Chests (Group 2)"];
        foreach (int tipo in contenedores)
            Assert.DoesNotContain(catalogo.TileName(tipo), enIngles);
    }
}
