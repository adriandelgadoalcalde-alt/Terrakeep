using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), V-c: "bajar de version no advierte de lo que se pierde -
// el aviso generico no dice que secciones dejaran de guardarse, los datos reales del personaje
// ya estan a mano para decirlo con numeros". El aviso se calcula contra el personaje YA
// cargado desde disco (PlrCharacter.EquipmentItems/VoidItems/Loadouts crudos) - una edicion
// hecha en la UI DESPUES de cargar no se refleja ahi hasta guardar (el vuelco real de
// MergedContainers a estos campos solo pasa en CharacterFileService.Save/MaskAndSyncAll), asi
// que estas pruebas embeben los datos directamente en el .plr sintetico ANTES de cargarlo -
// el escenario real que V-c describe (un personaje que YA trae equipo/loadouts puestos).
public sealed class VersionDowngradeWarningTests
{
    private static readonly PlrItemSlot IronHelmet = new(90, 1, 0, false);

    private static MainViewModel LoadCharacter(PlrCharacter character)
    {
        // Oleada del 6-sep-2026: estos textos se comparan en español y LocalizationService.
        // Instance es un singleton global del proceso de test - otra clase de test que cambie
        // el idioma y no lo devuelva hace fallar a esta en la suite completa (nunca aislada).
        Services.LocalizationService.Instance.SetLanguage(Services.LocalizationService.Spanish);
        string path = Path.Combine(Path.GetTempPath(), $"version-downgrade-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinNadaRealQuePerder_NoHayAviso()
    {
        var vm = LoadCharacter(new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        });

        vm.VersionEditor.SetVersionCommand.Execute(39); // 1.1.2, el mas bajo real

        Assert.Null(vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    public void BajarPorDebajoDe269ConLoadoutsRealesLlenos_AvisaConElNumeroReal()
    {
        var loadoutConCasco = PlrLoadout.CreateEmpty(isPrimary: false);
        loadoutConCasco.Items[0] = IronHelmet;
        var vm = LoadCharacter(new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [loadoutConCasco, PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        });

        vm.VersionEditor.SetVersionCommand.Execute(248); // 1.4.3.0, por debajo del umbral real 269

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("Loadouts 1/2/3", vm.VersionEditor.DowngradeWarning);
        Assert.Contains("1 objeto", vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    // Oleada del 6-sep-2026: EquipmentItems NO es "el equipo puesto (armadura/vanidad/
    // accesorios)" como decia el aviso viejo - son los 5 miscEquips reales (mascota, mascota de
    // luz, vagoneta, montura, gancho: Player.miscEquips real, confirmado en el decompilado). La
    // armadura vive en PrimaryLoadout, que se escribe SIEMPRE, sin ningun umbral de version:
    // bajar la version nunca la ha perdido, y el aviso nombraba algo que no se pierde.
    public void BajarPorDebajoDe145ConMiscEquipsReales_Avisa()
    {
        var equipo = new PlrItemSlot[5];
        Array.Fill(equipo, PlrItemSlot.Empty);
        equipo[0] = IronHelmet;
        var vm = LoadCharacter(new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            EquipmentItems = equipo,
        });

        vm.VersionEditor.SetVersionCommand.Execute(98); // por debajo del umbral real 145

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("Mascota / Montura / Gancho", vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    public void SubirDeVersion_NuncaAvisaDePerdida()
    {
        var equipo = new PlrItemSlot[5];
        Array.Fill(equipo, PlrItemSlot.Empty);
        equipo[0] = IronHelmet;
        var vm = LoadCharacter(new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            EquipmentItems = equipo,
        });
        vm.VersionEditor.SetVersionCommand.Execute(98); // baja primero, deberia avisar
        Assert.NotNull(vm.VersionEditor.DowngradeWarning);

        vm.VersionEditor.SetVersionCommand.Execute(315); // vuelve a subir, por encima de todos los umbrales reales

        Assert.Null(vm.VersionEditor.DowngradeWarning);
    }
}
