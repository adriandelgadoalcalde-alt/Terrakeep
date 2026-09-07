using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Oleada del 6-sep-2026 (Personaje > Buffs/Investigacion/Version). El aviso de bajada de
// version solo miraba 3 cosas (miscEquips/Boveda/Loadouts) y callaba lo que de verdad se
// pierde con mas frecuencia y mas cantidad:
//   - los BUFFS: PlrBodySerializer escribe 44/22/10 segun version (linea 351), asi que bajar de
//     269 se come 22 slots de golpe sin decir nada.
//   - la INVESTIGACION entera (version >= 200, linea 382) - un personaje de Modo Viaje con
//     miles de objetos investigados los perdia todos en silencio.
//   - las misiones de pesca (>= 98, linea 369) y la puntuacion de golf (>= 200, linea 389).
// Cada numero de estos tests sale del contenido REAL que se mete en el personaje, no de un
// texto fijo.
public sealed class VersionDowngradeContenidoRealTests
{
    private static MainViewModel Cargar(Action<PlrCharacter> preparar)
    {
        // Estos textos se comparan en español, y LocalizationService.Instance es un SINGLETON
        // global de todo el proceso de test - cualquier otra clase de test que cambie el idioma
        // y no lo devuelva deja a esta mintiendo (pasa de verdad: los tests de idioma de la
        // Libreria lo dejaban en ingles y estos empezaban a fallar en la suite completa pero no
        // aislados). Fijarlo aqui es lo unico que este fichero puede garantizar por si mismo.
        Services.LocalizationService.Instance.SetLanguage(Services.LocalizationService.Spanish);
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        preparar(character);
        string path = Path.Combine(Path.GetTempPath(), $"version-downgrade-real-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    // Un personaje 1.4.4 con 44 slots y buffs puestos en los de arriba: bajar a 1.4.3.0 (248)
    // deja de escribir los slots 22..43.
    [Fact]
    public void BajarDe269ConBuffsEnLosSlotsAltos_AvisaConElNumeroReal()
    {
        var vm = Cargar(c =>
        {
            c.Buffs = [];
            for (int i = 0; i < 44; i++) c.Buffs.Add(new PlrBuff { Id = 0, Time = 0 });
            c.Buffs[22] = new PlrBuff { Id = 1, Time = 3600 };
            c.Buffs[43] = new PlrBuff { Id = 2, Time = 3600 };
        });

        vm.VersionEditor.SetVersionCommand.Execute(248);

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("2 buff(s)", vm.VersionEditor.DowngradeWarning);
        Assert.Contains("22", vm.VersionEditor.DowngradeWarning);
    }

    // Los buffs que siguen cabiendo no cuentan como perdida - el aviso tiene que ser exacto,
    // no "hay buffs, aviso por si acaso".
    [Fact]
    public void BajarDe269ConBuffsSoloEnLosSlotsBajos_NoAvisaDeBuffs()
    {
        var vm = Cargar(c =>
        {
            c.Buffs = [];
            for (int i = 0; i < 44; i++) c.Buffs.Add(new PlrBuff { Id = 0, Time = 0 });
            c.Buffs[0] = new PlrBuff { Id = 1, Time = 3600 };
            c.Buffs[21] = new PlrBuff { Id = 2, Time = 3600 };
        });

        vm.VersionEditor.SetVersionCommand.Execute(248);

        Assert.Null(vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    public void BajarPorDebajoDe200ConInvestigacionReal_AvisaConElNumeroReal()
    {
        var vm = Cargar(c =>
        {
            c.Research.Add(new PlrResearchEntry { Pid = "IronBroadsword", Count = 1 });
            c.Research.Add(new PlrResearchEntry { Pid = "DirtBlock", Count = 100 });
        });

        vm.VersionEditor.SetVersionCommand.Execute(190); // 1.3.5, por debajo del umbral real 200

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("2 objeto(s) investigado(s)", vm.VersionEditor.DowngradeWarning);
    }

    // El conteo tiene que ser el VIVO (lo editado sin guardar todavia), no el que trajo el .plr:
    // character.Research solo se reescribe en ResearchViewModel.SyncBackTo, al guardar.
    [Fact]
    public void InvestigacionEditadaSinGuardar_YaCuentaEnElAviso()
    {
        var vm = Cargar(_ => { });
        Assert.Equal(0, vm.Research.ResearchedCount);

        // Investiga una carpeta real entera, sin guardar nada.
        var carpeta = vm.Research.RootCategories.First(n => n.ItemIdsOrdered.Count > 0);
        vm.Research.SelectCategoryCommand.Execute(carpeta);
        vm.Research.ResearchFolderCommand.Execute(null);
        int investigados = vm.Research.ResearchedCount;
        Assert.True(investigados > 0);

        vm.VersionEditor.SetVersionCommand.Execute(190);

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains($"{investigados} objeto(s) investigado(s)", vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    public void BajarPorDebajoDe98ConMisionesDePesca_Avisa()
    {
        var vm = Cargar(c => c.FishingQuestsCompleted = 17);

        vm.VersionEditor.SetVersionCommand.Execute(93); // 1.2.3, por debajo del umbral real 98

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("17", vm.VersionEditor.DowngradeWarning);
    }

    [Fact]
    public void BajarPorDebajoDe200ConPuntuacionDeGolf_Avisa()
    {
        var vm = Cargar(c => c.GolferScore = 42);

        vm.VersionEditor.SetVersionCommand.Execute(190);

        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Contains("42", vm.VersionEditor.DowngradeWarning);
    }

    // Bajar y volver a subir no puede dejar el aviso pegado: subir no pierde nada, y ademas los
    // buffs de los slots altos siguen ahi (BuffsViewModel.ApplyVersion no trunca a proposito).
    [Fact]
    public void VolverASubirLaVersion_QuitaElAvisoYRecuperaLosBuffsAltos()
    {
        var vm = Cargar(c =>
        {
            c.Buffs = [];
            for (int i = 0; i < 44; i++) c.Buffs.Add(new PlrBuff { Id = 0, Time = 0 });
            c.Buffs[30] = new PlrBuff { Id = 1, Time = 3600 };
        });

        vm.VersionEditor.SetVersionCommand.Execute(248);
        Assert.NotNull(vm.VersionEditor.DowngradeWarning);
        Assert.Equal(22, vm.Buffs.Container!.Slots.Count);

        vm.VersionEditor.SetVersionCommand.Execute(279);

        Assert.Null(vm.VersionEditor.DowngradeWarning);
        Assert.Equal(44, vm.Buffs.Container!.Slots.Count);
        Assert.Equal(1, vm.Buffs.Container!.Slots[30].Buff.Id);
    }
}
