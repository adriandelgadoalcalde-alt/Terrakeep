using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// PB-12 (oleada de pruebas de Personaje > Buffs/Apariencia/Investigacion/Spawn Points/
// Desbloqueos/Version, 6-sep-2026). Round-trip REAL de esta zona entera, de punta a punta y
// sobre un personaje REAL del usuario - siempre sobre una COPIA en carpeta temporal, nunca el
// fichero original (regla del proyecto).
//
// Que aporta que no aporte ningun test existente: los de esta zona parten todos de un
// PlrCharacter sintetico vacio. Un personaje real de este PC tiene ~100KB con investigacion
// real, buffs ya puestos, flags ya marcados y un .tplr de Calamity al lado - que es donde puede
// romperse algo que un personaje vacio nunca enseña: un campo con umbral de version que se
// escribe fuera de sitio, la investigacion que se vuelca mal en SyncBackTo, un buff que
// CalamityCharacterSync reescribe al pasar por el .tplr.
//
// El criterio es deliberadamente estricto: se edita UNA cosa de cada sub-pestaña, se guarda, se
// vuelve a LEER con TerrasavrNative.Core sin pasar por ninguna vista, y se comprueba (a) que
// cada cambio esta donde tiene que estar con el valor EXACTO y (b) que lo demas del archivo
// sigue igual.
public sealed class BuffsAparienciaRoundTripPersonajeRealTests
{
    private const string CarpetaPersonajesReal =
        @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players";

    private static string? RutaCopiaDePersonajeReal(out string carpetaTemporal)
    {
        carpetaTemporal = Path.Combine(Path.GetTempPath(), $"pb12-roundtrip-{Guid.NewGuid():N}");
        if (!Directory.Exists(CarpetaPersonajesReal)) return null;
        var origen = new DirectoryInfo(CarpetaPersonajesReal).GetFiles("*.plr")
            .OrderByDescending(f => f.Length)
            .FirstOrDefault();
        if (origen == null) return null;

        Directory.CreateDirectory(carpetaTemporal);
        string destino = Path.Combine(carpetaTemporal, origen.Name);
        File.Copy(origen.FullName, destino);
        // El .tplr companero es parte del personaje real: sin el, ni los buffs de Calamity ni la
        // fusion real viajan, y esto dejaria de probar el camino que usa el usuario.
        string tplrOrigen = Path.ChangeExtension(origen.FullName, ".tplr");
        if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(destino, ".tplr"));
        return destino;
    }

    [Fact]
    public void EditarCadaSubPestañaSeGuardaYSeReleeConElValorExacto()
    {
        string? copia = RutaCopiaDePersonajeReal(out string carpeta);
        if (copia == null) return; // sin personajes reales en esta maquina - nada que probar

        try
        {
            var antes = PlrFile.Read(File.ReadAllBytes(copia));

            var vm = new MainViewModel();
            vm.LoadFromPath(copia);
            Assert.True(vm.IsCharacterLoaded);
            Assert.False(vm.IsDirty);

            // ── Buffs: un slot vacio real, con una duracion EXACTA que no coincide con ningun
            // preset (para que solo pueda venir de aqui y no de un valor por defecto).
            var slots = vm.Buffs.Container!.Slots;
            // Foto de los buffs TAL Y COMO QUEDAN AL CARGAR, no la del .plr crudo: en un
            // personaje con mods, CalamityCharacterSync.MergeBuffs rellena al cargar los huecos
            // vacios de character.Buffs con los modBuffs del .tplr (que es donde viven de verdad
            // los buffs de un personaje de tModLoader), y al guardar esos buffs fusionados SI se
            // escriben tambien en el .plr - comportamiento real y deliberado, calco de
            // calamityMergeBuffsIntoPlayer. Comparar contra el .plr crudo daria un falso
            // positivo por ese trasvase, que no es una edicion de esta prueba.
            var buffsTrasCargar = slots.Select(s => (s.Buff.Id, s.Buff.Time)).ToArray();
            var libre = slots.First(s => s.IsEmpty);
            int indiceLibre = libre.SlotIndex;
            Assert.True(libre.PlaceBuff(1)); // Piel de obsidiana (buff vanilla id 1 real)
            libre.DurationSeconds = 1234;
            Assert.Equal(1234 * 60, libre.Buff.Time);

            // ── Apariencia: peinado, tinte, dificultad, vida/mana, pesca, golf y horas.
            vm.Appearance.HairStyle = 42;
            vm.Appearance.HairDye = 3;
            vm.Appearance.HealthMax = 500;
            vm.Appearance.HealthNow = 123;
            vm.Appearance.ManaMax = 200;
            vm.Appearance.ManaNow = 77;
            vm.Appearance.FishingQuestsCompleted = 31;
            vm.Appearance.GolferScore = 64;
            vm.Appearance.Swatches[0].R = 200; // color de pelo, canal R
            byte dificultadPrevia = antes.Difficulty;

            // ── Spawn Points: uno nuevo con coordenadas reales.
            int spawnsAntes = vm.Servers.Entries.Count;
            vm.Servers.AddEntryCommand.Execute(null);
            var nuevo = vm.Servers.Entries[^1];
            nuevo.Name = "Mundo de prueba PB-12";
            nuevo.SpawnX = 1234;
            nuevo.SpawnY = 567;
            nuevo.WorldId = 987654;

            // ── Desbloqueos: los dos bits REALES del carrito potenciado, que en el .plr viven
            // en el MISMO byte (bit0=desbloqueado, bit1=activado) - el caso donde un bug de
            // mascara se nota de verdad.
            vm.Flags.UnlockedSuperMinecart = true;
            vm.Flags.UsingSuperMinecart = false;
            vm.Flags.AegisFruit = true;

            // ── Investigacion: un objeto concreto, con un conteo parcial a mano.
            var carpetaConObjetos = vm.Research.RootCategories.First(c => c.ItemIdsOrdered.Count > 0);
            vm.Research.SelectCategoryCommand.Execute(carpetaConObjetos);
            var fila = vm.Research.Results.First(r => !r.IsCalamity); // vanilla: su Pid resuelve seguro
            int idInvestigado = fila.Id;
            fila.Count = 7;
            int investigadosDespuesDeEditar = vm.Research.ResearchedCount;

            Assert.True(vm.IsDirty);
            vm.SaveCommand.Execute(null);
            Assert.False(vm.IsDirty);

            // ── Relectura real con Core, sin pasar por ninguna vista ──────────────────────
            var despues = PlrFile.Read(File.ReadAllBytes(copia));

            // Buffs: el id Y la duracion exacta.
            Assert.Equal(1, despues.Buffs[indiceLibre].Id);
            Assert.Equal(1234 * 60, despues.Buffs[indiceLibre].Time);

            // Apariencia.
            Assert.Equal(42, despues.HairStyle);
            Assert.Equal(3, despues.HairDye);
            Assert.Equal(500, despues.HealthMax);
            Assert.Equal(123, despues.HealthNow);
            Assert.Equal(200, despues.ManaMax);
            Assert.Equal(77, despues.ManaNow);
            Assert.Equal(31, despues.FishingQuestsCompleted);
            Assert.Equal(64, despues.GolferScore);
            Assert.Equal(200, despues.HairColor[0]);

            // Spawn Points.
            Assert.Equal(spawnsAntes + 1, despues.Servers.Count);
            var spawnGuardado = despues.Servers[^1];
            Assert.Equal("Mundo de prueba PB-12", spawnGuardado.Name);
            Assert.Equal(1234, spawnGuardado.SpawnX);
            Assert.Equal(567, spawnGuardado.SpawnY);
            Assert.Equal(987654, spawnGuardado.WorldId);

            // Desbloqueos: los dos bits del mismo byte, cada uno por su lado.
            Assert.Equal(1, despues.SuperCartByte & 1);
            Assert.Equal(0, despues.SuperCartByte & 2);
            Assert.True(despues.ExtraUsingFlags[2]); // AegisFruit

            // Investigacion: la entrada real con su conteo parcial, y ninguna perdida.
            Assert.Equal(investigadosDespuesDeEditar, despues.Research.Count);
            Assert.Contains(despues.Research, r => r.Count == 7);

            // ── Y lo que NO se toco sigue igual ──────────────────────────────────────────
            Assert.Equal(antes.Name, despues.Name);
            Assert.Equal(antes.Version, despues.Version);
            Assert.Equal(dificultadPrevia, despues.Difficulty);
            Assert.Equal(antes.Inventory.Length, despues.Inventory.Length);
            for (int i = 0; i < antes.Inventory.Length; i++)
                Assert.Equal(antes.Inventory[i].Id, despues.Inventory[i].Id);
            // Los demas slots de buff siguen exactamente como quedaron al cargar.
            for (int i = 0; i < buffsTrasCargar.Length && i < despues.Buffs.Count; i++)
            {
                if (i == indiceLibre) continue;
                Assert.Equal(buffsTrasCargar[i].Id, despues.Buffs[i].Id);
                Assert.Equal(buffsTrasCargar[i].Time, despues.Buffs[i].Time);
            }

            // ── Y al RECARGAR desde disco, la UI enseña lo mismo (el viaje completo de ida y
            // vuelta que hace el usuario, no solo el fichero) ────────────────────────────
            var vm2 = new MainViewModel();
            vm2.LoadFromPath(copia);
            Assert.Equal(1, vm2.Buffs.Container!.Slots[indiceLibre].Buff.Id);
            Assert.Equal(1234, vm2.Buffs.Container.Slots[indiceLibre].DurationSeconds);
            Assert.Equal(42, vm2.Appearance.HairStyle);
            Assert.Equal(123, vm2.Appearance.HealthNow);
            Assert.Equal(31, vm2.Appearance.FishingQuestsCompleted);
            Assert.True(vm2.Flags.UnlockedSuperMinecart);
            Assert.False(vm2.Flags.UsingSuperMinecart);
            Assert.True(vm2.Flags.AegisFruit);
            Assert.Equal(1234, vm2.Servers.Entries[^1].SpawnX);
            Assert.Equal(investigadosDespuesDeEditar, vm2.Research.ResearchedCount);
            Assert.NotEqual(0, idInvestigado);
        }
        finally
        {
            try { Directory.Delete(carpeta, recursive: true); } catch { /* limpieza best-effort */ }
        }
    }
}
