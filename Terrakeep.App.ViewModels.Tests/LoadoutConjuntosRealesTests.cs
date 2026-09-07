using System.IO;
using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Queja real del usuario (6-sep-2026): "en Terraria un personaje tiene 3 conjuntos de equipo, no
// mas - Terrakeep enseña 4 pildoras (Puesto + Loadout 1/2/3)".
//
// El hallazgo real que hay detras (confirmado en el decompilado de Terraria 1.4.5.8 y midiendo
// .plr reales, ver el bloque de comentario del constructor de EquipmentGroupViewModel):
// PrimaryLoadout NO es un espejo redundante del loadout activo, es el UNICO sitio del archivo
// donde ese conjunto existe - EquipmentLoadout.Swap intercambia en vez de copiar, asi que la
// entrada Loadouts[CurrentLoadout] del archivo queda VACIA mientras ese loadout esta activo.
//
// Por eso el arreglo no es "esconder la pildora Puesto" (eso esconderia justo el conjunto que el
// personaje lleva encima y ofreceria un hueco vacio en su lugar), sino numerar los 3 conjuntos
// 1/2/3 como el juego y hacer que la pildora del activo apunte al contenedor 0.
public sealed class LoadoutConjuntosRealesTests
{
    private static PlrItemSlot Objeto(int id) => new(id, 1, 0, false);

    // Personaje moderno con los 3 conjuntos con contenido DISTINTO, colocado donde lo pondria el
    // juego de verdad: el activo en PrimaryLoadout y los otros dos en su Loadouts[i], dejando
    // Loadouts[currentLoadout] vacio (el hueco del swap).
    private static PlrCharacter TresConjuntosDistintos(int currentLoadout, int idActivo, int idOtroA, int idOtroB)
    {
        var loadouts = new[] { PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false) };
        int[] pendientes = [idOtroA, idOtroB];
        int siguiente = 0;
        for (int n = 0; n < 3; n++)
        {
            if (n == currentLoadout) continue; // hueco vacio real del loadout activo
            loadouts[n].Items[0] = Objeto(pendientes[siguiente++]);
        }
        var primary = PlrLoadout.CreateEmpty(isPrimary: true);
        primary.Items[0] = Objeto(idActivo);
        return new PlrCharacter
        {
            Name = "TresConjuntos",
            Version = 279,
            PrimaryLoadout = primary,
            Loadouts = loadouts,
            CurrentLoadout = currentLoadout,
        };
    }

    private static PlrCharacter Antiguo() => new()
    {
        Name = "Antiguo",
        Version = 200, // < 269: el .plr no tiene loadouts, solo el equipo puesto
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [],
    };

    private static string Guardar(PlrCharacter c)
    {
        string path = Path.Combine(Path.GetTempPath(), $"loadouts-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(c));
        return path;
    }

    private static void Limpiar(string path)
    {
        foreach (string p in new[] { path, path + ".bak", Path.ChangeExtension(path, ".tplr") })
            if (File.Exists(p)) File.Delete(p);
    }

    [Fact]
    public void PersonajeModerno_OfreceTresPildoras_NoCuatro()
    {
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 0, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);

        var opciones = vm.EquipmentGroup!.LoadoutOptions;
        Assert.Equal(3, opciones.Count);
        Assert.Equal(["1", "2", "3"], opciones.Select(o => o.Label));

        Limpiar(path);
    }

    [Fact]
    public void PersonajeAntiguoSinLoadouts_ConservaLaUnicaPildoraPuesto()
    {
        string path = Guardar(Antiguo());
        var vm = new MainViewModel();
        vm.LoadFromPath(path);

        Assert.Single(vm.EquipmentGroup!.LoadoutOptions);
        Assert.Equal(0, vm.EquipmentGroup.LoadoutOptions[0].Value);
        Assert.Equal(-1, vm.EquipmentGroup.ActiveLoadout); // no hay loadouts reales que numerar
        Assert.False(vm.EquipmentGroup.LoadoutOptions[0].IsActiveLoadout);

        Limpiar(path);
    }

    [Fact]
    public void CadaPildoraEditaElConjuntoReal_ConElLoadout1Activo()
    {
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 0, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        var grupo = vm.EquipmentGroup!;

        // Conjunto 1 = el activo = PrimaryLoadout (contenedor 0); 2 y 3 = Loadouts[1] y Loadouts[2]
        // (contenedores 2 y 3). El contenedor 1 (Loadouts[0]) es el hueco vacio: sin pildora.
        Assert.Equal([0, 2, 3], grupo.LoadoutOptions.Select(o => o.Value));
        Assert.Equal(0, grupo.ActiveLoadout);
        Assert.True(grupo.LoadoutOptions[0].IsActiveLoadout);
        Assert.False(grupo.LoadoutOptions[1].IsActiveLoadout);
        Assert.Equal(0, grupo.SelectedLoadout); // arranca en la pildora "1"

        foreach (var (opcion, idEsperado) in grupo.LoadoutOptions.Zip(new[] { 90, 91, 92 }))
        {
            grupo.SelectLoadoutCommand.Execute(opcion);
            Assert.Equal(idEsperado, grupo.CurrentItems.Slots[0].ItemId);
        }

        Limpiar(path);
    }

    [Fact]
    public void CadaPildoraEditaElConjuntoReal_ConElLoadout2Activo()
    {
        // El caso que de verdad distingue el mapeo: el conjunto 2 esta en PrimaryLoadout y el
        // hueco vacio es Loadouts[1] (contenedor 2).
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 1, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        var grupo = vm.EquipmentGroup!;

        Assert.Equal([1, 0, 3], grupo.LoadoutOptions.Select(o => o.Value));
        Assert.Equal(1, grupo.ActiveLoadout);
        Assert.True(grupo.LoadoutOptions[1].IsActiveLoadout);
        Assert.Equal(1, grupo.SelectedLoadout); // la pildora "1" NO es el contenedor 0 aqui

        // Loadouts[0]=91 (conjunto 1), PrimaryLoadout=90 (conjunto 2, el activo), Loadouts[2]=92
        foreach (var (opcion, idEsperado) in grupo.LoadoutOptions.Zip(new[] { 91, 90, 92 }))
        {
            grupo.SelectLoadoutCommand.Execute(opcion);
            Assert.Equal(idEsperado, grupo.CurrentItems.Slots[0].ItemId);
        }

        Limpiar(path);
    }

    [Fact]
    public void LaPildoraDelConjuntoPuesto_SeMarcaConUnPunto()
    {
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 2, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);

        var opciones = vm.EquipmentGroup!.LoadoutOptions;
        Assert.Equal(["1", "2", "3 ●"], opciones.Select(o => o.DisplayLabel));
        Assert.All(opciones.Take(2), o => Assert.Null(o.ToolTipText));
        Assert.False(string.IsNullOrEmpty(opciones[2].ToolTipText));

        Limpiar(path);
    }

    // El round-trip que de verdad importa: editar un conjunto que NO es el activo tiene que caer
    // en su Loadouts[] real del archivo, y PrimaryLoadout (lo que lee el doll de Inicio, y lo que
    // el juego carga como equipo puesto) tiene que salir intacto byte a byte.
    [Fact]
    public void EditarElConjunto2YGuardar_EscribeEnLoadouts1_SinTocarPrimaryLoadout()
    {
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 0, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var antes = PlrFile.Read(File.ReadAllBytes(path));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        var grupo = vm.EquipmentGroup!;

        grupo.SelectLoadoutCommand.Execute(grupo.LoadoutOptions[1]); // conjunto 2
        grupo.CurrentItems.Slots[0].PlaceItem(1281); // Casco de dios de la jungla, id vanilla real
        vm.SaveCommand.Execute(null);

        // Se relee el .plr con Core directamente, sin pasar por la UI.
        var despues = PlrFile.Read(File.ReadAllBytes(path));

        Assert.Equal(1281, despues.Loadouts[1].Items[0].Id);           // el cambio cayo donde toca
        Assert.Equal(92, despues.Loadouts[2].Items[0].Id);             // el conjunto 3, intacto
        Assert.True(despues.Loadouts[0].Items.All(s => s.Id == 0));    // el hueco del activo sigue vacio
        Assert.Equal(0, despues.CurrentLoadout);                       // el indice del activo, intacto

        // PrimaryLoadout - el campo del doll de Inicio - no se ha tocado ni ha perdido nada.
        Assert.Equal(antes.PrimaryLoadout.Items.Select(s => s.Id), despues.PrimaryLoadout.Items.Select(s => s.Id));
        Assert.Equal(antes.PrimaryLoadout.Social.Select(s => s.Id), despues.PrimaryLoadout.Social.Select(s => s.Id));
        Assert.Equal(antes.PrimaryLoadout.Dyes.Select(s => s.Id), despues.PrimaryLoadout.Dyes.Select(s => s.Id));
        Assert.Equal(90, despues.PrimaryLoadout.Items[0].Id);

        Limpiar(path);
    }

    // El gemelo del anterior: editar el conjunto ACTIVO tiene que escribir en PrimaryLoadout (no
    // en Loadouts[CurrentLoadout], que el juego ni siquiera lee como equipo puesto).
    [Fact]
    public void EditarElConjuntoPuestoYGuardar_EscribeEnPrimaryLoadout()
    {
        string path = Guardar(TresConjuntosDistintos(currentLoadout: 1, idActivo: 90, idOtroA: 91, idOtroB: 92));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        var grupo = vm.EquipmentGroup!;

        grupo.SelectLoadoutCommand.Execute(grupo.LoadoutOptions[1]); // la pildora "2" = la activa
        grupo.CurrentItems.Slots[0].PlaceItem(1281);
        vm.SaveCommand.Execute(null);

        var despues = PlrFile.Read(File.ReadAllBytes(path));
        Assert.Equal(1281, despues.PrimaryLoadout.Items[0].Id);
        Assert.True(despues.Loadouts[1].Items.All(s => s.Id == 0)); // el hueco sigue vacio, no se ensucia
        Assert.Equal(91, despues.Loadouts[0].Items[0].Id);
        Assert.Equal(92, despues.Loadouts[2].Items[0].Id);

        Limpiar(path);
    }
}
