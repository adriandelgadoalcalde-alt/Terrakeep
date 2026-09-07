using System.Linq;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;

namespace Terrakeep.App.ViewModels.Tests;

// H5-06 (quinta auditoria de Opus): "Favorito se guarda en disco, no se ve nunca, se pierde al
// reemplazar y Ordenar lo mueve" - GameItem.Favorited viajaba de punta a punta (PlrBodySerializer)
// pero la capa App no lo tocaba en ningun sitio. Cierra el circuito completo: marca visible,
// se conserva al reemplazar, y Sort/ClearAll/MoveAllTo lo respetan.
public sealed class FavoritedTests
{
    private static readonly CharacterFileService Service = new();

    private static ItemSlotViewModel NuevoSlot(int index, GameItem item) =>
        new(Service, index, "inventory", item);

    [Fact]
    public void ToggleFavorite_MarcaYDesmarcaElSlotLleno()
    {
        var slot = NuevoSlot(15, new GameItem { Id = 2, Count = 1 });
        Assert.False(slot.IsFavorited);

        slot.ToggleFavoriteCommand.Execute(null);
        Assert.True(slot.IsFavorited);
        Assert.True(slot.Item.Favorited);

        slot.ToggleFavoriteCommand.Execute(null);
        Assert.False(slot.IsFavorited);
    }

    [Fact]
    public void ToggleFavorite_SlotVacio_NoHaceNada()
    {
        var slot = NuevoSlot(15, GameItem.Empty);
        slot.ToggleFavoriteCommand.Execute(null);
        Assert.False(slot.IsFavorited);
    }

    [Fact]
    public void PlaceItem_SobreUnSlotFavorito_ConservaElFavorito()
    {
        // Bug real de la quinta auditoria: reemplazar el objeto de un slot favorito lo
        // borraba en silencio (new GameItem siempre nacia con Favorited=false).
        var slot = NuevoSlot(15, new GameItem { Id = 2, Count = 1, Favorited = true });
        Assert.True(slot.IsFavorited);

        slot.PlaceItem(3); // Espada corta de hierro, id real vanilla
        Assert.True(slot.IsFavorited);
        Assert.True(slot.Item.Favorited);
    }

    [Fact]
    public void PlaceItem_SobreUnSlotNoFavorito_SigueSinFavorito()
    {
        var slot = NuevoSlot(15, GameItem.Empty);
        slot.PlaceItem(2);
        Assert.False(slot.IsFavorited);
    }

    private static ContainerViewModel NuevoContenedor(string key, params GameItem[] items)
    {
        var slots = new System.Collections.ObjectModel.ObservableCollection<ItemSlotViewModel>(
            items.Select((item, i) => NuevoSlot(i, item)));
        return new ContainerViewModel(key, "Prueba", slots);
    }

    [Fact]
    public void Sort_UnFavoritoSeQuedaEnSuSlot_ElRestoSeOrdenaAlrededor()
    {
        // Terraria.UI.ItemSorting.cs:1070 real, "&& !item.favorited" - un favorito no se mueve
        // aunque "Ordenar" lo alcance.
        var contenedor = NuevoContenedor("storage",
            new GameItem { Id = 50, Count = 1 },
            new GameItem { Id = 10, Count = 1, Favorited = true },
            new GameItem { Id = 30, Count = 1 });

        contenedor.SortCommand.Execute(null);

        Assert.Equal(10, contenedor.Slots[1].ItemId); // el favorito no se movio de su slot
        Assert.True(contenedor.Slots[1].IsFavorited);
        // Los otros dos (30 y 50) se ordenan entre los huecos que quedan (0 y 2), ascendente.
        Assert.Equal(30, contenedor.Slots[0].ItemId);
        Assert.Equal(50, contenedor.Slots[2].ItemId);
    }

    [Fact]
    public void Sort_RespetaLaBarraRapidaYLosFavoritosALaVez()
    {
        var items = new GameItem[12];
        items[0] = new GameItem { Id = 99, Count = 1 }; // barra rapida (slot 0) - nunca se toca
        items[5] = new GameItem { Id = 40, Count = 1, Favorited = true }; // favorito en la barra rapida
        for (int i = 0; i < items.Length; i++) items[i] ??= GameItem.Empty;
        items[10] = new GameItem { Id = 20, Count = 1 };
        items[11] = new GameItem { Id = 5, Count = 1 };

        var contenedor = NuevoContenedor("inventory", items);
        contenedor.SortCommand.Execute(null);

        Assert.Equal(99, contenedor.Slots[0].ItemId); // barra rapida intacta
        Assert.Equal(40, contenedor.Slots[5].ItemId); // favorito de la barra rapida intacto
        Assert.Equal(5, contenedor.Slots[10].ItemId); // los 2 no-favoritos fuera de la barra, ordenados
        Assert.Equal(20, contenedor.Slots[11].ItemId);
    }

    [Fact]
    public void ClearAll_SaltaLosFavoritos()
    {
        var contenedor = NuevoContenedor("storage",
            new GameItem { Id = 2, Count = 1, Favorited = true },
            new GameItem { Id = 3, Count = 1 });

        contenedor.ClearAllCommand.Execute(null);

        Assert.False(contenedor.Slots[0].IsEmpty); // favorito intacto
        Assert.True(contenedor.Slots[1].IsEmpty); // el resto se vacia como siempre
        Assert.Equal(1, contenedor.LastClearedCount);
    }

    [Fact]
    public void MoveAllTo_SaltaLosFavoritos()
    {
        var origen = NuevoContenedor("inventory",
            new GameItem { Id = 2, Count = 1, Favorited = true },
            new GameItem { Id = 3, Count = 1 });
        var destino = NuevoContenedor("storage", GameItem.Empty, GameItem.Empty);

        int movidos = origen.MoveAllTo(destino);

        Assert.Equal(1, movidos);
        Assert.False(origen.Slots[0].IsEmpty); // el favorito se queda en Inventario
        Assert.True(origen.Slots[1].IsEmpty); // el no-favorito se movio
        Assert.Equal(3, destino.Slots[0].ItemId);
    }
}
