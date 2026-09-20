using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;
using Xunit;

namespace Terrakeep.App.ViewModels.Tests;

// Catalogo de ideas Keep, idea 9 ("modo reparar personaje"), segunda pasada (20-sep-2026): el
// coordinador confirmo que el dato para slots fantasma/duraciones de buff desbordadas/.tplr
// huerfano estaba disponible YA y pidio cerrarlos - ver el comentario de cabecera real en
// MainViewModel.RebuildIllegalPrefixDiagnostics para el alcance completo. "Version desfasada"
// no tiene prueba aqui a proposito: YA estaba resuelto antes de esta ronda via
// VersionEditorViewModel.DowngradeWarning (que tiene sus propias pruebas reales), no se
// duplica un diagnostico que ya existe.
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
            Assert.Single(vm.GhostSlotItemNames);
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
            Assert.Empty(vm.GhostSlotItemNames);
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
            Assert.Single(vm.OverflowingBuffNames);
            Assert.Contains("-50", vm.OverflowingBuffNames[0]);
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
            Assert.Single(vm.OverflowingBuffNames);
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
            Assert.Empty(vm.OverflowingBuffNames);
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
            Assert.Contains("Otro.tplr", vm.OrphanTplrFileNames);
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
            Assert.Empty(vm.IllegalPrefixItemNames);
            Assert.Empty(vm.GhostSlotItemNames);
            Assert.Empty(vm.OverflowingBuffNames);
            Assert.Empty(vm.OrphanTplrFileNames);
            Assert.Empty(vm.InconsistentTplrSlotNames);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
