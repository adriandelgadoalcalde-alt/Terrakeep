using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// H5-08 (quinta auditoria de Opus): "UpdateSizeClass solo recibe el ancho... la altura no
// participa en ninguna decision de layout". Segunda dimension real, independiente del ancho.
public sealed class HeightClassTests
{
    [Fact]
    public void UpdateSizeClass_ConUnSoloArgumento_SeQuedaEnBajo_ComportamientoDeSiempre()
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1600); // Amplio de ancho, pero la sobrecarga de 1 argumento nunca sube a Alto
        Assert.Equal(WindowHeightClass.Bajo, vm.HeightClass);
    }

    [Fact]
    public void UpdateSizeClass_AlturaPorDebajoDe900_EsBajo()
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1080, 899);
        Assert.Equal(WindowHeightClass.Bajo, vm.HeightClass);
    }

    [Fact]
    public void UpdateSizeClass_AlturaDe900OMas_EsAlto()
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1080, 900);
        Assert.Equal(WindowHeightClass.Alto, vm.HeightClass);
    }

    // FASE D del responsive global (28-sep-2026, s26/s28 negative acceptance): el tope de alto de la fila
    // de la Libreria (LibraryRowMaxHeight, 460/640 segun HeightClass) se RETIRO - la Libreria se queda
    // con el alto que Objetos no usa (ObjetosView.AjustarRepartoObjetosLibreria). Si alguien lo
    // reintroduce, este test falla. Sustituye a las dos aserciones 460/640 que fijaban el mecanismo viejo.
    [Fact]
    public void ElTopeDeAltoDeLaLibreriaYaNoExiste()
    {
        Assert.Null(typeof(MainViewModel).GetProperty("LibraryRowMaxHeight"));
    }

    [Fact]
    public void AnchoYAltoSonEjesIndependientes_CompactoYAltoALaVez()
    {
        // Una ventana estrecha pero muy alta (ej. monitor vertical) - SizeClass y HeightClass
        // no estan correlados, cada uno mide su propio eje.
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1080, 1200);
        Assert.Equal(WindowSizeClass.Compacto, vm.SizeClass);
        Assert.Equal(WindowHeightClass.Alto, vm.HeightClass);
    }

    [Fact]
    public void IsLibraryVisible_SeRevelaSolaPorAltoAunqueElAnchoSeaCompacto()
    {
        // El motivo real (H5-08): Libreria y contenedores viven en filas apiladas, no
        // columnas - si hay sitio o no depende de la ALTURA, no del ancho.
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1080, 700); // Compacto x Bajo
        Assert.False(vm.IsLibraryVisible); // plegada por defecto (preferencia real)

        vm.UpdateSizeClass(1080, 950); // Compacto x Alto
        Assert.True(vm.IsLibraryVisible);

        vm.UpdateSizeClass(1080, 700); // vuelve a Bajo - la preferencia real gobierna otra vez
        Assert.False(vm.IsLibraryVisible);
    }
}
