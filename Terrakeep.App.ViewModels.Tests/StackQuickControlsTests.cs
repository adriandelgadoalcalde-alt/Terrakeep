using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;

namespace Terrakeep.App.ViewModels.Tests;

// Encargo Keep 25-sep-2026: "EDITOR DE OBJETO... añadir controles rápidos de stack: +10, +100,
// MAX. MAX debe respetar el maxStack real del objeto, no un número fijo. +10/+100 tampoco deben
// sobrepasar el máximo permitido." Antes de este encargo, OnCountChanged topaba SIEMPRE a 9999
// fijo (ver el bug real que cierra UnaEspada_MaxStackUno_NuncaSuperaUnaUnidad mas abajo - una
// espada real, maxStack=1, se podia dejar a mano en cualquier numero hasta 9999).
//
// Los ids reales usados aqui ya estaban verificados a mano en pruebas hermanas de este mismo
// arnes (ver ObjetosTooltipStatsTests, comentarios "indice N de calamity/catalog.json"):
// - id 4 vanilla ("Espada larga de hierro"): arma, sin maxStack explicito en Item.cs -> 1 (el
//   default real del motor).
// - id 2 vanilla ("Tierra"): "maxStack = CommonMaxStack;" explicito en Item.cs -> 9999.
// - id 71 vanilla ("Moneda de cobre"): "maxStack = 100;" explicito en Item.cs -> 100 (unico
//   valor real de la version actual que no es ni 1 ni 9999 - ver
//   scripts/extraer-max-stack-vanilla.py).
// - 20000223 Calamity ("BloodfireArrow"): "Item.maxStack = Item.CommonMaxStack;" real en
//   BloodfireArrow.cs -> 9999.
// - 20000244 Calamity ("Sombrero de Aerospec"): pieza de armadura, sin maxStack explicito -> 1.
public sealed class StackQuickControlsTests
{
    private static MainViewModel ConPersonajeCargado()
    {
        var character = new Terrakeep.Core.PlrFormat.PlrCharacter
        {
            Name = "StackControls",
            Version = 279,
            PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts =
            [
                Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false),
                Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false),
                Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false),
            ],
        };
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"stack-controls-{System.Guid.NewGuid():N}.plr");
        System.IO.File.WriteAllBytes(path, Terrakeep.Core.PlrFormat.PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        System.IO.File.Delete(path);
        return vm;
    }

    [Fact]
    public void EspadaVanilla_MaxStackUno_LosTresControlesSeQuedanEnUnaUnidad()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(4); // Espada larga de hierro - maxStack real 1
        Assert.Equal(1, slot.MaxStack);
        Assert.Equal(1, slot.Count);

        slot.AddTenToCountCommand.Execute(null);
        Assert.Equal(1, slot.Count);

        slot.AddHundredToCountCommand.Execute(null);
        Assert.Equal(1, slot.Count);

        slot.SetCountToMaxCommand.Execute(null);
        Assert.Equal(1, slot.Count);
    }

    [Fact]
    public void TierraVanilla_MaxStack9999_SumaSinTopeHastaMaxYLuegoSeQuedaAhi()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2); // Tierra - maxStack real 9999 (CommonMaxStack)
        Assert.Equal(9999, slot.MaxStack);

        slot.AddTenToCountCommand.Execute(null);
        Assert.Equal(11, slot.Count);

        slot.AddHundredToCountCommand.Execute(null);
        Assert.Equal(111, slot.Count);

        slot.SetCountToMaxCommand.Execute(null);
        Assert.Equal(9999, slot.Count);

        // Ya al maximo: +10/+100 no deben sobrepasarlo.
        slot.AddTenToCountCommand.Execute(null);
        Assert.Equal(9999, slot.Count);
        slot.AddHundredToCountCommand.Execute(null);
        Assert.Equal(9999, slot.Count);
    }

    [Fact]
    public void MonedaDeCobreVanilla_MaxStack100_AddHundredSeQuedaEnElTopeRealNoEn111()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(71); // Moneda de cobre - maxStack real 100 (unico valor real != 1/9999)
        Assert.Equal(100, slot.MaxStack);

        slot.AddTenToCountCommand.Execute(null);
        Assert.Equal(11, slot.Count);

        // +100 real llevaria a 111 sin tope - debe quedarse en 100, el maximo real de este objeto.
        slot.AddHundredToCountCommand.Execute(null);
        Assert.Equal(100, slot.Count);

        slot.SetCountToMaxCommand.Execute(null);
        Assert.Equal(100, slot.Count);
    }

    [Fact]
    public void FlechaDeCalamity_MaxStack9999_SetMaxLlegaAlTopeRealDelCatalogoDeCalamity()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(20000223); // BloodfireArrow - stats.maxStack real 9999 en catalog.json
        Assert.True(slot.IsCalamity);
        Assert.Equal(9999, slot.MaxStack);

        slot.SetCountToMaxCommand.Execute(null);
        Assert.Equal(9999, slot.Count);
    }

    [Fact]
    public void ArmaduraDeCalamity_SinMaxStackExplicito_SeQuedaEnElDefaultRealUno()
    {
        var vm = ConPersonajeCargado();
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0];
        casco.PlaceItem(20000244); // Sombrero de Aerospec - sin "stats.maxStack" en catalog.json
        Assert.True(casco.IsCalamity);
        Assert.Equal(1, casco.MaxStack);

        casco.AddTenToCountCommand.Execute(null);
        Assert.Equal(1, casco.Count);
        casco.SetCountToMaxCommand.Execute(null);
        Assert.Equal(1, casco.Count);
    }

    // Regresion real del bug de raiz (encargo, "MAX debe respetar el maxStack real, no un numero
    // fijo"): antes de este arreglo, OnCountChanged topaba SIEMPRE a 9999 fijo, asi que escribir
    // "9999" a mano en el campo Cantidad de una espada (maxStack real 1) lo dejaba en 9999 sin
    // ningun aviso. La edicion manual del campo (Count=...) sigue funcionando igual que siempre -
    // pero ahora respeta el mismo tope real que los 3 botones nuevos.
    [Fact]
    public void EdicionManualDeCantidad_TambienRespetaElMaxStackRealNoElFijoDeAntes()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(4); // Espada larga de hierro - maxStack real 1

        slot.Count = 9999;

        Assert.Equal(1, slot.Count);
        Assert.Equal(1, slot.Item.Count);
    }

    [Fact]
    public void SlotVacio_LosTresControlesNoHacenNada()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        Assert.True(slot.IsEmpty);

        slot.AddTenToCountCommand.Execute(null);
        slot.AddHundredToCountCommand.Execute(null);
        slot.SetCountToMaxCommand.Execute(null);

        Assert.True(slot.IsEmpty);
    }
}
