using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-14 (quinta auditoria de Opus): "los ~350 slots no son alcanzables sin raton... Ctrl+C/
// Ctrl+V copian/pegan un objeto entero (prefijo, cantidad, favorito), reutilizando la logica
// que SwapWith ya tiene resuelta". La disambiguacion de teclas en si (que tecla dispara que
// accion) vive en code-behind (OnItemSlotKeyDown/OnBuffSlotKeyDown, MainWindow.xaml.cs) - sin
// precedente de teclado real simulado en el arnes UIA (mismo motivo documentado en H5-12) - lo
// que SI se puede y debe verificar aqui es la logica real que cada tecla dispara:
// ItemSlotViewModel.PasteItem/BuffSlotViewModel.PasteBuff.
public sealed class SlotKeyboardActionsTests
{
    private static readonly CharacterFileService Service = new();

    private static ItemSlotViewModel NuevoSlot(GameItem item, SlotKind acceptedKind = SlotKind.None) =>
        new(Service, 0, "inventory", item, acceptedKind: acceptedKind);

    [Fact]
    public void PasteItem_CopiaElObjetoEnteroConPrefijoCantidadYFavorito()
    {
        var origen = NuevoSlot(new GameItem { Id = 2, Count = 37, Favorited = true, Prefix = ItemPrefix.Vanilla(5) });
        var copia = origen.Item.Clone(); // lo que hace Ctrl+C real (OnItemSlotKeyDown)
        var destino = NuevoSlot(GameItem.Empty);

        bool aceptado = destino.PasteItem(copia);

        Assert.True(aceptado);
        Assert.Equal(2, destino.Item.Id);
        Assert.Equal(37, destino.Item.Count);
        Assert.True(destino.Item.Favorited);
        Assert.Equal(5, destino.Item.Prefix.VanillaId);
    }

    [Fact]
    public void PasteItem_ADiferenciaDePlaceItem_NoSugiereUnPrefijoNuevo()
    {
        // PlaceItem SIEMPRE nace con cantidad 1 y un prefijo recien SUGERIDO (pensado para
        // colocar algo nuevo desde la Libreria) - PasteItem debe reproducir EXACTAMENTE lo
        // copiado, sin cantidad de la sugerencia.
        var origen = NuevoSlot(new GameItem { Id = 3, Count = 1, Prefix = ItemPrefix.None });
        var copia = origen.Item.Clone();
        var destino = NuevoSlot(GameItem.Empty);

        destino.PasteItem(copia);

        Assert.Equal(ItemPrefix.None, destino.Item.Prefix);
    }

    [Fact]
    public void PasteItem_SinAceptarLaRestriccionDeSlot_Rechaza()
    {
        var origen = NuevoSlot(new GameItem { Id = 3, Count = 1 }); // Espada corta de hierro (arma, no accesorio)
        var copia = origen.Item.Clone();
        var destinoRestringido = NuevoSlot(GameItem.Empty, acceptedKind: SlotKind.Accessory);

        bool aceptado = destinoRestringido.PasteItem(copia);

        Assert.False(aceptado);
        Assert.True(destinoRestringido.IsEmpty); // el destino NO se toco
        Assert.NotNull(destinoRestringido.RejectionMessage);
    }

    [Fact]
    public void PasteItem_ConElOrigenVacio_VaciaElDestino()
    {
        var origen = NuevoSlot(GameItem.Empty);
        var copia = origen.Item.Clone();
        var destino = NuevoSlot(new GameItem { Id = 2, Count = 1 });

        destino.PasteItem(copia);

        Assert.True(destino.IsEmpty);
    }

    private static BuffSlotViewModel NuevoBuffSlot(int id, int time, Func<int, BuffSlotViewModel, bool>? isPlacedElsewhere = null) =>
        new(0, new PlrBuff { Id = id, Time = time }, Service.VanillaBuffs, Service.CalamityBuffCatalog, Service.VanillaBuffDurations, 279, isPlacedElsewhere: isPlacedElsewhere);

    [Fact]
    public void PasteBuff_CopiaLaDuracionExacta()
    {
        var origen = NuevoBuffSlot(1, 12345); // Bendicion de Poseidon o similar - id real arbitrario
        var destino = NuevoBuffSlot(0, 0);

        bool aceptado = destino.PasteBuff(origen.Buff.Id, origen.Buff.Time);

        Assert.True(aceptado);
        Assert.Equal(1, destino.Buff.Id);
        Assert.Equal(12345, destino.Buff.Time);
    }

    [Fact]
    public void PasteBuff_SiYaEstaPuestoEnOtroSlot_Rechaza()
    {
        var destino = NuevoBuffSlot(0, 0, isPlacedElsewhere: (_, _) => true);

        bool aceptado = destino.PasteBuff(1, 12345);

        Assert.False(aceptado);
        Assert.True(destino.IsEmpty);
        Assert.NotNull(destino.RejectionMessage);
    }
}
