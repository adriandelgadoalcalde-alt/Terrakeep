using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H5-02 (quinta auditoria de Opus): "Investigacion es de solo lectura salvo un boton de todo o
// nada". Verifica de extremo a extremo, contra el catalogo real, que ahora se puede editar de
// verdad: alternar por fila, conteo parcial a mano, acciones por carpeta y globales, y que el
// estado en memoria se vuelca de verdad al personaje al guardar (SyncBackTo).
//
// Nota real sobre el debounce de busqueda (CatalogBrowserViewModel): SearchText dispara un
// DispatcherTimer real, que nunca llega a tick sin un bucle de Dispatcher activo (no hay
// ninguno en un test xunit a secas) - estas pruebas navegan por CARPETA
// (SelectCategoryCommand, que llama a ApplyFilter de forma SINCRONA, sin temporizador de por
// medio) en vez de por busqueda de texto.
public sealed class ResearchEditableTests
{
    private static readonly CharacterFileService Service = new();

    private static PlrCharacter NuevoPersonaje() => new()
    {
        Name = "Test",
        Version = 279,
        Difficulty = 3, // Modo Viaje real
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static ResearchViewModel NuevoConPersonaje()
    {
        var vm = new ResearchViewModel(Service);
        vm.LoadFrom(NuevoPersonaje());
        return vm;
    }

    private static CategoryNodeViewModel BuscarCarpetaConVarios(IEnumerable<CategoryNodeViewModel> nodes) =>
        BuscarCarpetaConVariosONull(nodes) ?? throw new InvalidOperationException("No se encontro ninguna carpeta real pequeña para la prueba.");

    private static CategoryNodeViewModel? BuscarCarpetaConVariosONull(IEnumerable<CategoryNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.ItemIdsOrdered.Count is > 0 and <= 20) return node; // pequeña, prueba rapida y determinista
            var enHijos = BuscarCarpetaConVariosONull(node.Children);
            if (enHijos != null) return enHijos;
        }
        return null;
    }

    [Fact]
    public void SeleccionarCarpeta_MuestraTodoSuContenidoAunqueNadaEsteInvestigadoTodavia()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);

        vm.SelectCategoryCommand.Execute(carpeta);

        Assert.Equal(carpeta.ItemIdsOrdered.Count, vm.Results.Count);
        Assert.All(vm.Results, row => Assert.False(row.IsResearched));
    }

    [Fact]
    public void ToggleRow_SobreUnaFilaSinInvestigar_LaMarcaConSuConteoCompletoReal()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        var row = vm.Results[0];

        vm.ToggleRowCommand.Execute(row);

        Assert.True(row.IsResearched);
        Assert.True(row.Count > 0);
    }

    [Fact]
    public void ToggleRow_SobreUnaFilaYaInvestigada_LaVacia()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        var row = vm.Results[0];
        vm.ToggleRowCommand.Execute(row);
        Assert.True(row.IsResearched);

        vm.ToggleRowCommand.Execute(row);

        Assert.False(row.IsResearched);
        Assert.Equal(0, row.Count);
    }

    [Fact]
    public void EditarElConteoAMano_AceptaUnParcialReal()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        var row = vm.Results[0];

        row.Count = 37; // edicion real via la propiedad (mismo camino que el TextBox del XAML)

        Assert.True(row.IsResearched);
        Assert.Equal(37, row.Count);
    }

    [Fact]
    public void ResearchChanged_DisparaSoloConEdicionesRealesNoConNavegacion()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        int disparos = 0;
        vm.ResearchChanged += () => disparos++;

        vm.SelectCategoryCommand.Execute(carpeta); // navegar - NO es una edicion real
        Assert.Equal(0, disparos);

        vm.ToggleRowCommand.Execute(vm.Results[0]); // esto SI es una edicion real
        Assert.Equal(1, disparos);
    }

    [Fact]
    public void SyncBackTo_VuelcaElEstadoRealAlPersonaje_YSeLeeIgualAlRecargar()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        var row = vm.Results[0];
        vm.ToggleRowCommand.Execute(row);
        int id = row.Id;
        int conteoCompleto = row.Count;

        var character = NuevoPersonaje();
        vm.SyncBackTo(character);
        Assert.Single(character.Research);
        Assert.Equal(conteoCompleto, character.Research[0].Count);

        // Round-trip real: recargar ese MISMO personaje debe leer el mismo estado de vuelta.
        var vm2 = new ResearchViewModel(Service);
        vm2.LoadFrom(character);
        vm2.SelectCategoryCommand.Execute(BuscarCarpetaConVarios(vm2.RootCategories));
        var filaRecargada = vm2.Results.First(r => r.Id == id);
        Assert.True(filaRecargada.IsResearched);
        Assert.Equal(conteoCompleto, filaRecargada.Count);
    }

    [Fact]
    public void ClearAllResearch_VaciaTodoDeVerdad()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        vm.ToggleRowCommand.Execute(vm.Results[0]);
        Assert.True(vm.Results[0].IsResearched);

        vm.ClearAllResearchCommand.Execute(null);

        Assert.True(vm.Results.All(r => !r.IsResearched));
    }

    [Fact]
    public void ResearchFolder_MarcaTodoSinBajarUnParcialYaMasAlto()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        int primerId = vm.Results[0].Id;
        // Un parcial real ya MAS ALTO que el umbral (caso raro pero real: mas sacrificios de
        // los que hacen falta) no debe bajarse al "investigar la carpeta".
        vm.Results[0].Count = 999999;

        vm.ResearchFolderCommand.Execute(null);

        var filaTrasInvestigar = vm.Results.First(r => r.Id == primerId);
        Assert.Equal(999999, filaTrasInvestigar.Count); // no se bajo
        Assert.True(vm.Results.All(r => r.IsResearched));
    }

    [Fact]
    public void ClearFolder_QuitaTodoLoDeEsaCarpeta()
    {
        var vm = NuevoConPersonaje();
        var carpeta = BuscarCarpetaConVarios(vm.RootCategories);
        vm.SelectCategoryCommand.Execute(carpeta);
        vm.ResearchFolderCommand.Execute(null);
        Assert.True(vm.Results.All(r => r.IsResearched));

        vm.ClearFolderCommand.Execute(null);

        Assert.True(vm.Results.All(r => !r.IsResearched));
    }
}
