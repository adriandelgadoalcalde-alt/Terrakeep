using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), L-a: LibrarySearchGrammar se recupero del commit a0f5027
// (revertido sin querer junto al rediseño de navegacion rechazado en c38c960, aunque esta
// gramatica no tocaba navegacion). Pruebas deterministas reales de la gramatica en si -
// calco de app.TabLibrary.search real: coma=OR, espacio=AND, #id/#a-b por id, .texto en el
// tooltip.
public sealed class LibrarySearchGrammarTests
{
    [Fact]
    public void SinPrefijo_BuscaPorNombreComoSubcadena()
    {
        Assert.True(LibrarySearchGrammar.Matches("espada", 1, "espada de hierro", null));
        Assert.False(LibrarySearchGrammar.Matches("hacha", 1, "espada de hierro", null));
    }

    [Fact]
    public void Coma_EsOrEntreTerminos()
    {
        Assert.True(LibrarySearchGrammar.Matches("hacha, espada", 1, "espada de hierro", null));
        Assert.True(LibrarySearchGrammar.Matches("hacha, espada", 2, "hacha de piedra", null));
        Assert.False(LibrarySearchGrammar.Matches("hacha, martillo", 1, "espada de hierro", null));
    }

    [Fact]
    public void Espacio_EsAndDentroDeUnTermino()
    {
        Assert.True(LibrarySearchGrammar.Matches("espada hierro", 1, "espada de hierro", null));
        Assert.False(LibrarySearchGrammar.Matches("espada oro", 1, "espada de hierro", null));
    }

    [Fact]
    public void AlmohadillaId_BuscaPorIdExacto()
    {
        Assert.True(LibrarySearchGrammar.Matches("#90", 90, "cualquier nombre", null));
        Assert.False(LibrarySearchGrammar.Matches("#90", 91, "cualquier nombre", null));
    }

    [Fact]
    public void AlmohadillaRango_BuscaPorRangoDeIdAmbosExtremosIncluidos()
    {
        Assert.True(LibrarySearchGrammar.Matches("#100-200", 100, "x", null));
        Assert.True(LibrarySearchGrammar.Matches("#100-200", 200, "x", null));
        Assert.True(LibrarySearchGrammar.Matches("#100-200", 150, "x", null));
        Assert.False(LibrarySearchGrammar.Matches("#100-200", 99, "x", null));
        Assert.False(LibrarySearchGrammar.Matches("#100-200", 201, "x", null));
    }

    [Fact]
    public void PuntoPrefijo_BuscaEnElTooltipEnVezDelNombre()
    {
        Assert.True(LibrarySearchGrammar.Matches(".aumenta la defensa", 1, "casco de hierro", "aumenta la defensa en 2"));
        Assert.False(LibrarySearchGrammar.Matches(".aumenta la defensa", 1, "casco de aumenta la defensa", null)); // sin tooltip, no cuela por el nombre
    }

    [Fact]
    public void TerminoDeMenosDeDosCaracteres_SeIgnoraPorCompleto()
    {
        // Quirk real de Terrasavr: un "5" suelto no cuenta como busqueda por id ni por nombre.
        Assert.False(LibrarySearchGrammar.Matches("5", 5, "objeto con un 5 en el nombre", null));
    }
}
