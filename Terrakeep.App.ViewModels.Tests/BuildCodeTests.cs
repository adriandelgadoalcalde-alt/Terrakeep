using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Codigos de build compartibles (encargo del usuario, 13-sep-2026, cuarto de la lista
// confirmada: "exportar una build a un codigo de texto corto/compartible y poder importarlo en
// otra instalacion"). El formato en si (BuildCode.Encode/TryDecode) ya tiene sus propias
// pruebas en Terrakeep.Core.Tests - aqui se prueba la INTEGRACION real con el editor
// (MainViewModel.OpenBuildCode/CopyBuildCode/ImportBuildCode) sobre un personaje real cargado.
public sealed class BuildCodeMainViewModelTests
{
    // id 91 = "Casco de plata" (Silver Helmet), SlotKind.ArmorHead real - mismo id ya usado y
    // verificado en ObjetosRoundTripPersonajeRealTests. id 4 es un ARMA real (ver el comentario
    // de LibrarySlotRestrictionLabelTests/LIB-03-SLOT del arnes UIA, "sin el arma id=4") - nunca
    // encaja en ArmorHead, util para probar el caso "omitido por no encajar en su ranura".
    private const int CascoDePlata = 91;
    private const int ArmaId4 = 4;

    private static MainViewModel CargarPersonajeConCascoReal(byte prefijoCasco = 0)
    {
        var character = new PlrCharacter
        {
            Name = "BuildCodeTest",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        character.PrimaryLoadout.Items[0] = new PlrItemSlot(CascoDePlata, 1, prefijoCasco, false);
        string path = Path.Combine(Path.GetTempPath(), $"buildcode-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void AbrirElPanel_GeneraUnCodigoRealQueDecodificaAlMismoEquipo()
    {
        var vm = CargarPersonajeConCascoReal(prefijoCasco: 81); // 81 = Legendario

        vm.OpenBuildCodeCommand.Execute(null);

        Assert.True(vm.IsBuildCodeOpen);
        Assert.StartsWith("TKBUILD1:", vm.BuildCodeGenerated);

        bool ok = BuildCode.TryDecode(vm.BuildCodeGenerated, out var items, out _, out var error);
        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(CascoDePlata, items[0].ItemId);
        Assert.Equal(81, items[0].Prefix.VanillaId);
    }

    [Fact]
    public void CerrarYReabrirDesdeOtroPanel_NuncaLosDosOverlaysALaVez()
    {
        var vm = CargarPersonajeConCascoReal();
        vm.OpenBuildCodeCommand.Execute(null);
        Assert.True(vm.IsBuildCodeOpen);

        vm.OpenCompareCommand.Execute(null);

        Assert.False(vm.IsBuildCodeOpen); // el Comparador lo cierra al abrirse, mismo criterio que BackupHistory
        Assert.True(vm.Compare.IsOpen);
    }

    [Fact]
    public void ImportarUnCodigoValido_ColocaLosObjetosRealesYDejaUnaEntradaDeDeshacer()
    {
        var vm = CargarPersonajeConCascoReal();
        var itemsNuevos = Enumerable.Repeat(BuildCodeSlot.Empty, BuildCode.SlotCount).ToList();
        itemsNuevos[0] = new BuildCodeSlot(CascoDePlata, ItemPrefix.Vanilla(60)); // 60 = Demoniaco
        string codigo = BuildCode.Encode(itemsNuevos, Enumerable.Repeat(0, BuildCode.SlotCount).ToList());

        vm.BuildCodeImportText = codigo;
        Assert.True(vm.ImportBuildCodeCommand.CanExecute(null));
        vm.ImportBuildCodeCommand.Execute(null);

        var cascoSlot = vm.EquipmentGroup!.CurrentItems.Slots[0];
        Assert.Equal(CascoDePlata, cascoSlot.Item.Id);
        Assert.Equal(60, cascoSlot.Item.Prefix.VanillaId);
        Assert.False(vm.BuildCodeImportIsError);
        Assert.Contains("1", vm.BuildCodeImportMessage); // "1 objeto(s) colocado(s)"
        Assert.True(vm.UndoStack.CanUndo);

        vm.UndoEditCommand.Execute(null);
        Assert.Equal(0, cascoSlot.Item.Prefix.VanillaId); // vuelve al prefijo original (0 = ninguno)
    }

    [Fact]
    public void ImportarUnCodigoInvalido_MuestraElErrorRealYNoTocaNada()
    {
        var vm = CargarPersonajeConCascoReal(prefijoCasco: 5);
        var cascoSlot = vm.EquipmentGroup!.CurrentItems.Slots[0];

        vm.BuildCodeImportText = "esto no es un codigo de build de verdad";
        vm.ImportBuildCodeCommand.Execute(null);

        Assert.True(vm.BuildCodeImportIsError);
        Assert.NotNull(vm.BuildCodeImportMessage);
        Assert.Equal(CascoDePlata, cascoSlot.Item.Id); // intacto
        Assert.Equal(5, cascoSlot.Item.Prefix.VanillaId); // intacto
        Assert.False(vm.UndoStack.CanUndo); // nada real que deshacer
    }

    [Fact]
    public void ImportarUnObjetoQueNoEncajaEnSuRanura_SeCuentaComoOmitidoYNoSeColoca()
    {
        var vm = CargarPersonajeConCascoReal();
        var items = Enumerable.Repeat(BuildCodeSlot.Empty, BuildCode.SlotCount).ToList();
        items[0] = new BuildCodeSlot(ArmaId4, ItemPrefix.None); // un arma real en el hueco de Cabeza
        string codigo = BuildCode.Encode(items, Enumerable.Repeat(0, BuildCode.SlotCount).ToList());

        vm.BuildCodeImportText = codigo;
        vm.ImportBuildCodeCommand.Execute(null);

        var cascoSlot = vm.EquipmentGroup!.CurrentItems.Slots[0];
        Assert.Equal(CascoDePlata, cascoSlot.Item.Id); // el arma NO se coloco - sigue el casco original
        Assert.False(vm.BuildCodeImportIsError); // no es un error de formato, es un aviso real de omision
        Assert.Contains("omitido", vm.BuildCodeImportMessage);
    }

    [Fact]
    public void CopiarElCodigo_NuncaLanzaAunqueElPortapapelesFalle()
    {
        var vm = CargarPersonajeConCascoReal();
        vm.OpenBuildCodeCommand.Execute(null);

        var ex = Record.Exception(() => vm.CopyBuildCodeCommand.Execute(null));

        Assert.Null(ex);
    }
}
