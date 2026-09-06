using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// PB-13 (oleada de Personaje > Buffs, 6-sep-2026) - BUG REAL en el aviso de "ese buff ya esta
// puesto en otro slot".
//
// H4-04 dejo escrito el criterio correcto para el arrastre: si PlaceBuff rechaza, se selecciona
// el slot destino para que el aviso real quede a la vista en el panel "Editar buff
// seleccionado" "en vez de perderse sin que se note nada". Pero el orden en que se hacia las
// dos cosas anulaba el arreglo: PlaceBuff pone RejectionMessage y SelectBuffSlot pone
// IsSelected=true, y BuffSlotViewModel.OnIsSelectedChanged limpia RejectionMessage a proposito
// (L-e: un aviso de rechazo no debe sobrevivir a un cambio de seleccion). O sea que el aviso se
// borraba justo despues de ponerlo, salvo por casualidad cuando el slot destino YA era el
// seleccionado (unico caso en que IsSelected no cambia de valor y el manejador no corre).
//
// Estos tests fijan la mecanica real de las dos piezas por separado, que es lo que se puede
// probar sin code-behind - el gesto completo (soltar de verdad, Ctrl+V de verdad) lo mide el
// bloque PB-13 del arnes de UI Automation.
public sealed class BuffAvisoRechazoVisibleTests
{
    private static MainViewModel Cargar()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buff-aviso-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    // La mecanica que anulaba el arreglo, escrita tal cual para que no se pueda volver a colar:
    // seleccionar un slot limpia su aviso de rechazo (correcto y deliberado, L-e).
    [Fact]
    public void SeleccionarUnSlot_LimpiaSuAvisoDeRechazo()
    {
        var vm = Cargar();
        var slots = vm.Buffs.Container!.Slots;
        slots[0].PlaceBuff(1);
        Assert.False(slots[1].PlaceBuff(1)); // duplicado real: rechazado
        Assert.NotNull(slots[1].RejectionMessage);

        vm.SelectBuffSlot(slots[1]);

        Assert.Null(slots[1].RejectionMessage);
    }

    // Y por tanto el orden correcto es SELECCIONAR PRIMERO y colocar despues: asi el aviso que
    // deja el rechazo se queda a la vista en el panel Editar, que es justo lo que H4-04 queria.
    [Fact]
    public void SeleccionarAntesDeColocar_DejaElAvisoALaVista()
    {
        var vm = Cargar();
        var slots = vm.Buffs.Container!.Slots;
        slots[0].PlaceBuff(1);

        vm.SelectBuffSlot(slots[1]);
        Assert.False(slots[1].PlaceBuff(1));

        Assert.NotNull(slots[1].RejectionMessage);
        Assert.Same(slots[1], vm.BuffEdit.Slot); // y el panel Editar esta mirando ese slot
    }

    // Ctrl+V respeta la misma regla real de "sin dos instancias del mismo buff" (a diferencia de
    // RestoreExact, que es solo para Deshacer) - y por tanto necesita el mismo aviso visible.
    [Fact]
    public void PegarUnBuffYaPuesto_SeRechazaConAvisoReal()
    {
        var vm = Cargar();
        var slots = vm.Buffs.Container!.Slots;
        slots[0].PlaceBuff(1);
        slots[0].SetDurationTicks(9999);

        vm.SelectBuffSlot(slots[5]);
        bool aceptado = slots[5].PasteBuff(slots[0].Buff.Id, slots[0].Buff.Time);

        Assert.False(aceptado);
        Assert.NotNull(slots[5].RejectionMessage);
        Assert.True(slots[5].IsEmpty); // y no dejo el slot a medias
    }

    // Un pegado legitimo si tiene que reproducir la duracion EXACTA copiada (a diferencia de
    // PlaceBuff, que fija una duracion razonable para una colocacion nueva).
    [Fact]
    public void PegarUnBuffQueNoEstaPuesto_ReproduceLaDuracionExacta()
    {
        var vm = Cargar();
        var slots = vm.Buffs.Container!.Slots;
        slots[0].PlaceBuff(1);
        slots[0].SetDurationTicks(9999);

        Assert.True(slots[5].PasteBuff(2, 9999));

        Assert.Equal(2, slots[5].Buff.Id);
        Assert.Equal(9999, slots[5].Buff.Time);
    }
}
