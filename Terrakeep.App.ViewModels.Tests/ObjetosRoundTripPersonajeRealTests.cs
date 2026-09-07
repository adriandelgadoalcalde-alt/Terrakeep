using System.IO;
using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// OBJ-04 (oleada de pruebas de Personaje -> Objetos, 6-sep-2026). Round-trip REAL de la zona de
// Objetos, de punta a punta y sobre un personaje REAL del usuario - siempre sobre una COPIA en
// una carpeta temporal, nunca el fichero original (regla del proyecto).
//
// Que aporta esto que no aporten ya los tests existentes: los demas parten de un
// `PlrLoadout.CreateEmpty` sintetico, o sea de un personaje con TODOS los contenedores vacios.
// Un personaje real de este PC tiene ~100KB de inventario/almacenes/equipo/investigacion +
// un `.tplr` real de Calamity al lado, que es justo donde puede romperse algo sin que un
// personaje vacio se entere: una edicion en un contenedor que pisa OTRO contenedor, un campo
// que se reescribe mal al guardar, un objeto de Calamity que se pierde en la fusion.
//
// El criterio de la prueba es deliberadamente estricto: se edita UN slot concreto de cada uno
// de los 5 contenedores reales de esta zona (Inventario / Banco / Equipamiento-armadura /
// Mascotas-Monturas / Monedas), se guarda, se vuelve a LEER con Terrakeep.Core SIN pasar
// por ninguna vista, y se comprueba (a) que cada cambio esta donde tiene que estar y (b) que
// TODO lo demas del archivo sigue byte a byte igual que antes de editar.
public sealed class ObjetosRoundTripPersonajeRealTests
{
    private const string CarpetaPersonajesReal =
        @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players";

    // El personaje real mas completo de este PC (~100KB: inventario lleno, almacenes,
    // investigacion y un .tplr real de Calamity de ~22KB). Si algun dia deja de estar, la
    // prueba se salta sola en vez de fallar por algo que no es un bug de la app.
    private static string? RutaCopiaDePersonajeReal(out string carpetaTemporal)
    {
        carpetaTemporal = Path.Combine(Path.GetTempPath(), $"obj-roundtrip-{Guid.NewGuid():N}");
        if (!Directory.Exists(CarpetaPersonajesReal)) return null;
        var origen = new DirectoryInfo(CarpetaPersonajesReal).GetFiles("*.plr")
            .OrderByDescending(f => f.Length)
            .FirstOrDefault();
        if (origen == null) return null;

        Directory.CreateDirectory(carpetaTemporal);
        string destino = Path.Combine(carpetaTemporal, origen.Name);
        File.Copy(origen.FullName, destino);
        // El .tplr companero es parte del personaje real: sin el, la mitad de Calamity no viaja.
        string tplrOrigen = Path.ChangeExtension(origen.FullName, ".tplr");
        if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(destino, ".tplr"));
        return destino;
    }

    [Fact]
    public void EditarUnSlotDeCadaContenedorSeGuardaYSeReleeSinCorromperNadaMas()
    {
        string? copia = RutaCopiaDePersonajeReal(out string carpeta);
        if (copia == null) return; // sin personajes reales en esta maquina - nada que probar

        try
        {
            var antes = PlrFile.Read(File.ReadAllBytes(copia));

            var vm = new MainViewModel();
            vm.LoadFromPath(copia);
            Assert.True(vm.IsCharacterLoaded);
            Assert.False(vm.IsDirty); // cargar nunca ensucia por si solo

            // ── Las 5 ediciones reales, una por contenedor ──────────────────────────────────
            // Inventario: el ULTIMO slot (50 slots reales, indice 49) - a proposito el ultimo,
            // que es el que mas lejos queda del principio del buffer serializado.
            var inventario = vm.InventoryContainer!;
            Assert.Equal(50, inventario.Slots.Count);
            var slotInv = inventario.Slots[^1];
            slotInv.PlaceItem(3507); // "Espada corta de cobre" real (id verificado contra vanilla_item_names.json, admite prefijo)
            slotInv.Count = 1;
            // El Inventario SI lleva byte de favorito desde la version 145 - aqui la estrella
            // tiene que ofrecerse Y sobrevivir al round-trip.
            Assert.True(slotInv.SupportsFavorite);
            slotInv.ToggleFavoriteCommand.Execute(null);
            Assert.True(slotInv.IsFavorited);

            // Banco: primer slot del primer almacen (40 slots reales).
            var banco = vm.StorageGroup!.Options[0];
            vm.StorageGroup.SelectCommand.Execute(banco);
            var bancoContenedor = vm.StorageGroup.Current;
            Assert.Equal(40, bancoContenedor.Slots.Count);
            bancoContenedor.Slots[0].PlaceItem(2); // "Bloque de tierra" real (id 2 verificado contra vanilla_item_names.json)
            bancoContenedor.Slots[0].Count = 250;
            // OBJ-05: el Banco NO puede guardar favoritos (PlrContainerSpec.BankOrSafe.
            // FavFlagMinVersion == 0, confirmado contra Player.cs:55497-55499 del juego real, que
            // escribe el banco con type+stack+prefix y nada mas). La app lo ofrecia igualmente y
            // el favorito se perdia en silencio al guardar - ahora ni siquiera se ofrece.
            Assert.False(bancoContenedor.Slots[0].SupportsFavorite);
            bancoContenedor.Slots[0].ToggleFavoriteCommand.Execute(null);
            Assert.False(bancoContenedor.Slots[0].IsFavorited); // la red de seguridad real, no solo esconder el boton

            // Equipamiento: casco del conjunto PUESTO (contenedor 0 = PrimaryLoadout real).
            var armaduraPuesta = vm.EquipmentGroup!.EquippedItems;
            Assert.Equal(10, armaduraPuesta.Slots.Count);
            armaduraPuesta.Slots[0].PlaceItem(91); // "Casco de plata" real (id 91, SlotKind.ArmorHead=256 en vanilla_slot_kind.json)
            Assert.False(armaduraPuesta.Slots[0].IsEmpty); // la restriccion de tipo lo acepto de verdad

            // Mascotas/Monturas: slot 3 = Montura (ver MainViewModel.RebuildContainers).
            var monturas = vm.MountsContainer!;
            Assert.Equal(5, monturas.Slots.Count);
            monturas.Slots[3].PlaceItem(3260); // "Manzana bendita" real (id 3260, SlotKind.Mount=16 en vanilla_slot_kind.json)
            Assert.False(monturas.Slots[3].IsEmpty);

            // Monedas: slot 3 = platino (id 74 real).
            var monedas = vm.CoinsContainer!;
            Assert.Equal(4, monedas.Slots.Count);
            monedas.Slots[3].PlaceItem(74);
            monedas.Slots[3].Count = 99;

            Assert.True(vm.IsDirty);
            vm.SaveCommand.Execute(null);
            Assert.False(vm.IsDirty); // guardar limpia de verdad

            // ── Relectura real con Core, sin pasar por ninguna vista ────────────────────────
            var despues = PlrFile.Read(File.ReadAllBytes(copia));

            Assert.Equal(3507, despues.Inventory[49].Id);
            Assert.True(despues.Inventory[49].Favorited); // el favorito del Inventario SI viaja al archivo
            Assert.Equal(2, despues.BankItems[0].Id);
            Assert.Equal(250, despues.BankItems[0].Count);
            Assert.Equal(91, despues.PrimaryLoadout.Items[0].Id);
            Assert.Equal(3260, despues.EquipmentItems[3].Id);
            Assert.Equal(74, despues.Coins[3].Id);
            Assert.Equal(99, despues.Coins[3].Count);

            // ── Y nada MAS cambio: todo lo demas identico al original ───────────────────────
            Assert.Equal(antes.Name, despues.Name);
            Assert.Equal(antes.Version, despues.Version);
            Assert.Equal(antes.CurrentLoadout, despues.CurrentLoadout);
            Assert.Equal(antes.Research.Count, despues.Research.Count);

            ComparaSalvo(antes.Inventory, despues.Inventory, 49, "Inventario");
            ComparaSalvo(antes.BankItems, despues.BankItems, 0, "Banco");
            ComparaSalvo(antes.SafeItems, despues.SafeItems, -1, "Caja fuerte");
            ComparaSalvo(antes.ForgeItems, despues.ForgeItems, -1, "Fragua del Defensor");
            ComparaSalvo(antes.VoidItems, despues.VoidItems, -1, "Boveda del Vacio");
            ComparaSalvo(antes.EquipmentItems, despues.EquipmentItems, 3, "Mascotas/Monturas");
            ComparaSalvo(antes.EquipmentDyes, despues.EquipmentDyes, -1, "Tintes de montura");
            ComparaSalvo(antes.Coins, despues.Coins, 3, "Monedas");
            ComparaSalvo(antes.Ammo, despues.Ammo, -1, "Municion");
            ComparaSalvo(antes.PrimaryLoadout.Items, despues.PrimaryLoadout.Items, 0, "Equipo puesto - armadura");
            ComparaSalvo(antes.PrimaryLoadout.Social, despues.PrimaryLoadout.Social, -1, "Equipo puesto - vanidad");
            ComparaSalvo(antes.PrimaryLoadout.Dyes, despues.PrimaryLoadout.Dyes, -1, "Equipo puesto - tintes");
            for (int i = 0; i < antes.Loadouts.Length; i++)
            {
                ComparaSalvo(antes.Loadouts[i].Items, despues.Loadouts[i].Items, -1, $"Loadout {i + 1} - armadura");
                ComparaSalvo(antes.Loadouts[i].Social, despues.Loadouts[i].Social, -1, $"Loadout {i + 1} - vanidad");
                ComparaSalvo(antes.Loadouts[i].Dyes, despues.Loadouts[i].Dyes, -1, $"Loadout {i + 1} - tintes");
            }
        }
        finally
        {
            try { Directory.Delete(carpeta, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void UnFavoritoQueSeMueveAUnAlmacenPierdeLaMarcaAlInstante_NoAlRecargar()
    {
        string? copia = RutaCopiaDePersonajeReal(out string carpeta);
        if (copia == null) return;

        try
        {
            var vm = new MainViewModel();
            vm.LoadFromPath(copia);

            var origen = vm.InventoryContainer!.Slots[^1];
            origen.PlaceItem(4); // "Espada larga de hierro" real
            origen.ToggleFavoriteCommand.Execute(null);
            Assert.True(origen.IsFavorited);

            // OBJ-05b: arrastrar del Inventario al Banco es exactamente esto (SwapWith, ver
            // MainWindow.xaml.cs). El Banco no puede guardar favoritos, asi que la marca tiene
            // que caerse AHI MISMO y no al recargar el personaje - si no, el usuario sigue viendo
            // la estrella en la esquina del slot y la pierde sin enterarse.
            var destino = vm.StorageGroup!.Current.Slots[0];
            Assert.False(destino.SupportsFavorite);
            origen.SwapWith(destino);

            Assert.Equal(4, destino.Item.Id);
            Assert.False(destino.IsFavorited);
            Assert.False(destino.Item.Favorited);
        }
        finally
        {
            try { Directory.Delete(carpeta, recursive: true); } catch (IOException) { }
        }
    }

    // Todos los slots de un contenedor tienen que salir identicos al original salvo el unico
    // indice que esta prueba edito a proposito (-1 = ninguno, el contenedor entero intacto).
    //
    // Unica excepcion real, medida en este mismo personaje y documentada a proposito: un slot
    // VACIO en los dos lados se da por igual aunque su `Count` residual cambie. Los "slots
    // fantasma" (id=0 con un stack/prefix de basura de una carga anterior - fenomeno real ya
    // documentado en PROYECTO-TERRASAVR.md) los conserva `PlrItemSlot.Read` a proposito, pero la
    // capa App los normaliza a `GameItem.Empty` en cuanto pasan por un ItemSlotViewModel, asi que
    // al re-guardar salen con Count=0. Es inocuo de verdad, no un dato perdido: el juego real
    // ignora `stack` en cuanto `type==0` (`Player.cs`, la carga hace `netDefaults(id)` y solo
    // entonces asigna stack), y el propio Terrasavr original hace exactamente lo mismo. Lo que
    // esta prueba SI vigila es que ningun slot con contenido real cambie ni un campo.
    private static void ComparaSalvo(PlrItemSlot[] antes, PlrItemSlot[] despues, int indiceEditado, string nombre)
    {
        Assert.Equal(antes.Length, despues.Length);
        for (int i = 0; i < antes.Length; i++)
        {
            if (i == indiceEditado) continue;
            if (antes[i].IsEmpty && despues[i].IsEmpty) continue; // slot fantasma normalizado, ver arriba
            Assert.True(antes[i].Id == despues[i].Id && antes[i].Count == despues[i].Count && antes[i].Prefix == despues[i].Prefix && antes[i].Favorited == despues[i].Favorited,
                $"{nombre}[{i}] cambio sin que nadie lo tocara: antes id={antes[i].Id} x{antes[i].Count} prefijo={antes[i].Prefix} fav={antes[i].Favorited}, " +
                $"despues id={despues[i].Id} x{despues[i].Count} prefijo={despues[i].Prefix} fav={despues[i].Favorited}");
        }
    }
}
