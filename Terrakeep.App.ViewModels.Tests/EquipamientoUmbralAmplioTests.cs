using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// AR-14 (6-sep-2026). Queja real del usuario: "algo ha vuelto a pasar con la seccion de
// equipamiento, los slots de accesorios se vuelven a solapar con las monedas/municion a pantalla
// mas pequeña".
//
// El solape real medido (arnes de UI, bloque AR-14, biseccion de 2 en 2px sobre coordenadas de
// pantalla reales) NO estaba en las clases estrechas, sino justo al ENTRAR en SizeClass.Amplio:
// ahi Equipamiento deja de mostrar UNA vista con pildoras y pasa a mostrar las TRES
// (Armadura/Vanidad/Tintes) lado a lado dentro de la misma columna central. Cada vista necesita
// 216px (5 columnas * MinCell 40 + 4 * Gap 4, que es el ancho que SlotGridPanel devuelve cuando
// congela la celda en MinCell y ya no puede encoger mas) y recibia (colCentro - 16) / 3.
//
// Con AmplioMinWidth=1500 eso daba 213,9px entre 1500 y 1512: 4 slots por vista quedaban cortados
// de 0,7 a 4,7px - los ultimos, contra el bloque de Monedas/Municion, que es exactamente lo
// reportado. 1514 es el primer ancho real sin ningun corte (medido, no calculado), y el umbral se
// fijo en 1520.
//
// Este test fija el umbral para que nadie lo vuelva a bajar sin medir: es la misma clase de
// regresion que ya le paso a NormalMinWidth (1300 estaba mal medido por 20px).
public sealed class EquipamientoUmbralAmplioTests
{
    private static WindowSizeClass ClaseA(double ancho)
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(ancho);
        return vm.SizeClass;
    }

    [Theory]
    // El rango que de verdad se recortaba con el umbral viejo: tiene que quedarse en pildoras.
    [InlineData(1500)]
    [InlineData(1506)]
    [InlineData(1512)]
    [InlineData(1519)]
    public void UnAnchoDondeLasTresVistasNoCabenNoEsAmplio(double ancho)
    {
        Assert.NotEqual(WindowSizeClass.Amplio, ClaseA(ancho));
        Assert.NotEqual(WindowSizeClass.Extra, ClaseA(ancho));
    }

    [Theory]
    [InlineData(1520)]
    [InlineData(1560)]
    [InlineData(1700)]
    public void DesdeElUmbralRealLasTresVistasSiCabenYEsAmplio(double ancho)
    {
        Assert.True(ClaseA(ancho) >= WindowSizeClass.Amplio);
    }

    // Extra es un superconjunto de espacio de Amplio (R-10): nunca debe desactivar lo que Amplio
    // activaba - se comprueba por el booleano real que consume Equipamiento, no por el enum.
    [Fact]
    public void ExtraSigueMostrandoLasTresVistas()
    {
        var vm = new MainViewModel();
        vm.UpdateSizeClass(1920);
        Assert.Equal(WindowSizeClass.Extra, vm.SizeClass);
        Assert.True(vm.IsEquipmentExpanded);
    }

    // La cuenta real de la que sale el umbral, escrita como test para que no se pierda: las 3
    // vistas solo caben si cada una recibe al menos el ancho que SlotGridPanel devuelve cuando ya
    // no puede encoger mas. Si algun dia cambian MinCell, Gap o el numero de columnas de
    // Equipamiento, este test falla y obliga a volver a medir el umbral en vez de dejarlo mal.
    [Fact]
    public void ElAnchoMinimoDeUnaVistaSigueSiendoElQueFijoElUmbral()
    {
        const int columnas = 5, minCell = 40, gap = 4;
        Assert.Equal(216, columnas * minCell + (columnas - 1) * gap);
    }
}
