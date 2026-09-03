using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H5-03 (quinta auditoria de Opus): "guardar/cargar conjuntos de objetos". Verifica de extremo
// a extremo (MainViewModel real, fichero .json real en disco) las 2 acciones reales (Cargar
// reemplaza, Añadir rellena huecos), y que cada una es una sola entrada real de deshacer.
public sealed class ItemSetCommandsTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static (MainViewModel Vm, string Path) NuevoCargado()
    {
        string path = Path.Combine(Path.GetTempPath(), $"itemset-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        return (vm, path);
    }

    [Fact]
    public void GuardarYCargar_EnOtroPersonaje_RestituyeElConjuntoEntero()
    {
        var (origen, pathOrigen) = NuevoCargado();
        origen.InventoryContainer!.Slots[0].PlaceItem(4); // Iron Broadsword
        origen.InventoryContainer!.Slots[5].PlaceItem(2); // Dirt Block
        origen.InventoryContainer!.Slots[5].Count = 50;

        string jsonPath = Path.Combine(Path.GetTempPath(), $"conjunto-{Guid.NewGuid():N}.json");
        origen.SaveItemSet(origen.InventoryContainer, jsonPath);
        Assert.True(File.Exists(jsonPath));

        var (destino, pathDestino) = NuevoCargado();
        destino.InventoryContainer!.Slots[10].PlaceItem(3); // ya tenia algo puesto antes de cargar

        destino.LoadItemSet(destino.InventoryContainer, jsonPath, append: false);

        Assert.Equal(4, destino.InventoryContainer.Slots[0].ItemId);
        Assert.Equal(2, destino.InventoryContainer.Slots[5].ItemId);
        Assert.Equal(50, destino.InventoryContainer.Slots[5].Item.Count);
        Assert.True(destino.InventoryContainer.Slots[10].IsEmpty); // "Cargar" reemplaza TODO, incluido lo que sobraba

        File.Delete(pathOrigen);
        File.Delete(pathDestino);
        File.Delete(jsonPath);
    }

    [Fact]
    public void Añadir_SoloRellenaHuecosLibres_SinTocarLoQueYaHay()
    {
        var (origen, pathOrigen) = NuevoCargado();
        origen.InventoryContainer!.Slots[0].PlaceItem(4);
        string jsonPath = Path.Combine(Path.GetTempPath(), $"conjunto-{Guid.NewGuid():N}.json");
        origen.SaveItemSet(origen.InventoryContainer, jsonPath);

        var (destino, pathDestino) = NuevoCargado();
        destino.InventoryContainer!.Slots[0].PlaceItem(3); // slot 0 YA ocupado - Añadir no debe tocarlo

        destino.LoadItemSet(destino.InventoryContainer, jsonPath, append: true);

        Assert.Equal(3, destino.InventoryContainer.Slots[0].ItemId); // intacto
        Assert.Equal(4, destino.InventoryContainer.Slots[1].ItemId); // cayo en el siguiente hueco libre real

        File.Delete(pathOrigen);
        File.Delete(pathDestino);
        File.Delete(jsonPath);
    }

    [Fact]
    public void CargarConjunto_EsUnaSolaEntradaDeDeshacer()
    {
        var (origen, pathOrigen) = NuevoCargado();
        origen.InventoryContainer!.Slots[0].PlaceItem(4);
        origen.InventoryContainer!.Slots[1].PlaceItem(2);
        string jsonPath = Path.Combine(Path.GetTempPath(), $"conjunto-{Guid.NewGuid():N}.json");
        origen.SaveItemSet(origen.InventoryContainer, jsonPath);

        var (destino, pathDestino) = NuevoCargado();
        int entradasAntes = destino.UndoStack.Entries.Count;

        destino.LoadItemSet(destino.InventoryContainer!, jsonPath, append: false);

        Assert.Equal(entradasAntes + 1, destino.UndoStack.Entries.Count); // 2 slots cambiaron, 1 sola entrada

        destino.UndoEditCommand.Execute(null);
        Assert.True(destino.InventoryContainer!.Slots[0].IsEmpty);
        Assert.True(destino.InventoryContainer!.Slots[1].IsEmpty);

        File.Delete(pathOrigen);
        File.Delete(pathDestino);
        File.Delete(jsonPath);
    }

    [Fact]
    public void CargarUnFicheroQueNoEsUnConjuntoReal_NoRompeNadaYAvisaEnStatusMessage()
    {
        var (vm, path) = NuevoCargado();
        string basura = Path.Combine(Path.GetTempPath(), $"basura-{Guid.NewGuid():N}.json");
        File.WriteAllText(basura, "{\"esto\":\"no es un conjunto real\"}");

        vm.LoadItemSet(vm.InventoryContainer!, basura, append: false);

        Assert.Contains("Error", vm.StatusMessage);
        Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty); // nada se toco

        File.Delete(path);
        File.Delete(basura);
    }
}
