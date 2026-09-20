using System.IO;
using System.Linq;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Nbt;
using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.App.ViewModels.Tests;

// Catalogo de ideas Keep, idea 9 ("modo reparar personaje"). Tercera pasada (20-sep-2026): el
// coordinador pidio releer el texto LITERAL de la idea en el documento real antes de darla por
// cerrada - confirmo que "arreglo en un clic" SI es parte real del alcance pedido, no una
// añadidura opcional. Cada fila de diagnostico es ahora un RepairIssueViewModel real
// (DisplayName + FixCommand) en vez de un string suelto - ver el comentario de cabecera real en
// MainViewModel.RebuildIllegalPrefixDiagnostics para el alcance completo. "Version desfasada" no
// tiene prueba aqui a proposito: YA estaba resuelto antes de esta ronda via
// VersionEditorViewModel.DowngradeWarning (con su propio arreglo en un clic real, los botones de
// VersionEditor.SetVersionCommand, y sus propias pruebas), no se duplica un diagnostico que ya
// existe.
//
// Cada prueba usa su PROPIA carpeta temporal aislada (nunca la raiz de Path.GetTempPath()
// directamente) para que el escaneo real de .tplr huerfanos no se tope con ficheros ajenos de
// otras pruebas/otras apps que compartan el temp del sistema - misma disciplina de "aislar la
// variable" que el resto de la familia Keep exige antes de dar una prueba por concluyente.
public sealed class RepairDiagnosticsTests
{
    private static PlrCharacter BaseCharacter(int version = 279) => new()
    {
        Name = "Test",
        Version = version,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static (MainViewModel Vm, string Dir) NewLoadedViewModel(PlrCharacter character)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"terrakeep-repair-diag-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "Test.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        return (vm, dir);
    }

    [Fact]
    public void SlotConIdVanillaDesconocido_SeMarcaComoFantasma()
    {
        var character = BaseCharacter();
        // 9000: por debajo del techo real de lectura del propio formato para esta version
        // (PlrBodySerializer.GetMaxItemId(279) = 16384, "id > maxId -> id = 0" solo se aplica
        // ahi, asi que sobrevive el ida-y-vuelta real por PlrFile.Write/Read) pero por encima
        // del ultimo id real que existe hoy en el catalogo vanilla (6195) - la situacion REAL
        // que produce un slot fantasma: el propio juego deja hueco de sobra para ids futuros,
        // el catalogo cargado por Terrakeep no los conoce todavia.
        character.Inventory[0] = new PlrItemSlot(Id: 9000, Count: 1, Prefix: 0, Favorited: false);
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            Assert.Single(vm.GhostSlotIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void SlotConIdVanillaReal_NoSeMarcaComoFantasma()
    {
        var character = BaseCharacter();
        character.Inventory[0] = new PlrItemSlot(Id: 1, Count: 1, Prefix: 0, Favorited: false); // Copper Shortsword, id vanilla real
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            Assert.Empty(vm.GhostSlotIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ArreglarSlotFantasma_VaciaLaRanuraYQuitaElAviso()
    {
        var character = BaseCharacter();
        character.Inventory[0] = new PlrItemSlot(Id: 9000, Count: 1, Prefix: 0, Favorited: false);
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            var issue = Assert.Single(vm.GhostSlotIssues);
            issue.FixCommand.Execute(null);

            Assert.Empty(vm.GhostSlotIssues); // el propio Fix recalcula el diagnostico al terminar
            Assert.True(vm.InventoryContainer!.Slots[0].IsEmpty);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ArreglarPrefijoIlegal_QuitaElPrefijoYQuitaElAviso()
    {
        // Mismo criterio real que el arnes IDEA9_SOLO (Terrakeep.App.Tests/Program.cs): nunca
        // suponer un id de prefijo ilegal a ciegas - se pregunta al catalogo REAL cual de los 83
        // prefijos vanilla es ilegal para el objeto elegido (id=1, Copper Shortsword).
        var servicioAparte = new CharacterFileService();
        var legales = servicioAparte.PrefixRules.LegalPrefixes(1);
        byte idIlegal = Enumerable.Range(1, 83).Select(i => (byte)i).First(candidato => !legales.Contains((int)candidato));

        var character = BaseCharacter();
        character.Inventory[0] = new PlrItemSlot(Id: 1, Count: 1, Prefix: idIlegal, Favorited: false);
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            var issue = Assert.Single(vm.IllegalPrefixIssues);
            issue.FixCommand.Execute(null);

            Assert.Empty(vm.IllegalPrefixIssues);
            Assert.True(vm.InventoryContainer!.Slots[0].Item.Prefix.IsNone);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void TplrConSlotFueraDeRango_SeMarcaComoInconsistente()
    {
        var character = BaseCharacter();
        string dir = Path.Combine(Path.GetTempPath(), $"terrakeep-repair-diag-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string plrPath = Path.Combine(dir, "Test.plr");
        File.WriteAllBytes(plrPath, PlrFile.Write(character));

        // "Abaddon"/"CalamityMod" es una entrada real del catalogo cargado de verdad por
        // CharacterFileService (Assets/calamity/catalog.json) - mismo objeto real que usan las
        // pruebas de Core (CalamityCharacterSyncTests). Slot 60: Inventory solo tiene 50 slots
        // reales (0..49) en un .plr recien creado - exactamente la condicion real de
        // CalamityCharacterSync.FindOutOfRangeTplrSlots.
        var tplrRoot = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(60)))
            ]))
        );
        File.WriteAllBytes(Path.ChangeExtension(plrPath, ".tplr"), TplrFile.Write("data", tplrRoot));

        var vm = new MainViewModel();
        try
        {
            vm.LoadFromPath(plrPath);

            var issue = Assert.Single(vm.InconsistentTplrIssues);
            Assert.Contains("inventory", issue.DisplayName);
            Assert.Contains("60", issue.DisplayName);

            issue.FixCommand.Execute(null);

            Assert.Empty(vm.InconsistentTplrIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void BuffConDuracionNegativa_SeMarcaComoDesbordado()
    {
        var character = BaseCharacter();
        character.Buffs.Add(new PlrBuff { Id = 1, Time = -50 }); // Obsidian Skin con duracion imposible (desbordo de int)
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            var issue = Assert.Single(vm.OverflowingBuffIssues);
            Assert.Contains("-50", issue.DisplayName);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void BuffPorEncimaDelTechoRealDeLaVersion_SeMarcaComoDesbordado()
    {
        var character = BaseCharacter(version: 279); // >=269 -> techo real 1999999980 (BuffDurationPresets.MaxTicksModern)
        character.Buffs.Add(new PlrBuff { Id = 1, Time = 2_000_000_005 });
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            Assert.Single(vm.OverflowingBuffIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void BuffDentroDelTechoReal_NoSeMarca()
    {
        var character = BaseCharacter();
        character.Buffs.Add(new PlrBuff { Id = 1, Time = 120 * 60 }); // 120 segundos reales, valor normal de juego
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            Assert.Empty(vm.OverflowingBuffIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ArreglarBuffDesbordado_AcotaLaDuracionAlTechoRealYQuitaElAviso()
    {
        var character = BaseCharacter(version: 279);
        character.Buffs.Add(new PlrBuff { Id = 1, Time = 2_000_000_005 }); // > techo real (1999999980)
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            var issue = Assert.Single(vm.OverflowingBuffIssues);
            issue.FixCommand.Execute(null);

            Assert.Empty(vm.OverflowingBuffIssues);
            var buffSlot = vm.Buffs.Container!.Slots[0];
            Assert.Equal(Core.Data.BuffDurationPresets.MaxTicksForVersion(279), buffSlot.Buff.Time);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ArreglarBuffConDuracionNegativa_LaDejaEnCero()
    {
        var character = BaseCharacter();
        character.Buffs.Add(new PlrBuff { Id = 1, Time = -50 });
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            var issue = Assert.Single(vm.OverflowingBuffIssues);
            issue.FixCommand.Execute(null);

            Assert.Empty(vm.OverflowingBuffIssues);
            Assert.Equal(0, vm.Buffs.Container!.Slots[0].Buff.Time);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void TplrSueltoSinPlrHermano_SeMarcaComoHuerfano()
    {
        var character = BaseCharacter();
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            string tplrHuerfano = Path.Combine(dir, "Otro.tplr");
            File.WriteAllBytes(tplrHuerfano, [0]); // contenido irrelevante - solo importa que el fichero exista sin .plr hermano
            vm.RebuildIllegalPrefixDiagnostics(); // recalculo manual: el fichero huerfano se crea DESPUES de cargar
            Assert.Contains(vm.OrphanTplrIssues, i => i.DisplayName == "Otro.tplr");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ArreglarTplrHuerfano_BorraElFicheroYQuitaElAviso()
    {
        var character = BaseCharacter();
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            string tplrHuerfano = Path.Combine(dir, "Otro.tplr");
            File.WriteAllBytes(tplrHuerfano, [0]);
            vm.RebuildIllegalPrefixDiagnostics();
            var issue = Assert.Single(vm.OrphanTplrIssues);

            issue.FixCommand.Execute(null);

            Assert.Empty(vm.OrphanTplrIssues);
            Assert.False(File.Exists(tplrHuerfano));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void PersonajeLimpio_NoMarcaNadaEnNingunDiagnosticoDeReparacion()
    {
        var character = BaseCharacter();
        character.Inventory[0] = new PlrItemSlot(Id: 1, Count: 1, Prefix: 0, Favorited: false);
        var (vm, dir) = NewLoadedViewModel(character);
        try
        {
            Assert.Empty(vm.IllegalPrefixIssues);
            Assert.Empty(vm.GhostSlotIssues);
            Assert.Empty(vm.OverflowingBuffIssues);
            Assert.Empty(vm.OrphanTplrIssues);
            Assert.Empty(vm.InconsistentTplrIssues);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
