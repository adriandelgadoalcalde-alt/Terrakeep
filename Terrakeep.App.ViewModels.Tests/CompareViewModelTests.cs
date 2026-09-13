using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Comparador de personajes/builds (encargo del usuario, 13-sep-2026: "seleccionar dos personajes/
// builds y ver lado a lado sus diferencias - equipo, stats, prefijos, inventario - resalta lo que
// cambia", primero de la lista confirmada). Mismo patron real de HomeCardTests para construir
// CharacterListEntryViewModel de prueba (CharacterFileService.EquipmentAppearance publico) -
// nunca toca ningun fichero real del usuario, siempre una carpeta/temp propia.
public sealed class CompareViewModelTests
{
    private static readonly CharacterFileService Service = new();
    private static readonly EquipmentAppearanceResolver EquipAppearance = Service.EquipmentAppearance;

    private static (CharacterListEntryViewModel entry, string path) NuevaEntrada(string nombre, int healthMax, int headItemId, byte headPrefix, int inv0ItemId, int inv0Count)
    {
        var character = new PlrCharacter
        {
            Name = nombre,
            Version = 279,
            HealthMax = healthMax,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        character.PrimaryLoadout.Items[0] = new PlrItemSlot(headItemId, headItemId == 0 ? 0 : 1, headPrefix, false);
        character.Inventory[0] = new PlrItemSlot(inv0ItemId, inv0Count, 0, false);

        string dir = Path.Combine(Path.GetTempPath(), $"compare-vm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, nombre + ".plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var entry = new CharacterListEntryViewModel(path, character, isTModLoader: false, tplr: null, DateTime.UtcNow, EquipAppearance);
        return (entry, path);
    }

    private static void Limpiar(string path) => Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);

    [Fact]
    public void SinLosDosElegidos_NoMuestraResultados()
    {
        var (a, pathA) = NuevaEntrada("A", 100, 1, 0, 2, 1);
        var (b, pathB) = NuevaEntrada("B", 100, 1, 0, 2, 1);
        var vm = new CompareViewModel([a, b]);

        Assert.False(vm.HasBothSelected);
        Assert.False(vm.ShowResults);
        Assert.False(vm.NeedsMoreCharacters); // hay 2 en la lista, solo falta elegirlos

        vm.SelectedA = a;
        Assert.False(vm.HasBothSelected); // falta B todavia

        Limpiar(pathA); Limpiar(pathB);
    }

    [Fact]
    public void MenosDeDosPersonajesReales_NeedsMoreCharactersEsCierto()
    {
        var (a, pathA) = NuevaEntrada("Solo", 100, 1, 0, 2, 1);
        var vm = new CompareViewModel([a]);

        Assert.True(vm.NeedsMoreCharacters);

        Limpiar(pathA);
    }

    [Fact]
    public void DosPersonajesConDatosDistintos_MarcaLasDiferenciasReales()
    {
        // Vida distinta, casco distinto (id Y prefijo), primer hueco de inventario con
        // CANTIDAD distinta del mismo objeto - tres formas reales distintas de "diferir".
        var (a, pathA) = NuevaEntrada("A", 100, 1, 0, 2, 50);
        var (b, pathB) = NuevaEntrada("B", 500, 3, 1, 2, 999);
        var vm = new CompareViewModel([a, b]);

        vm.SelectedA = a;
        vm.SelectedB = b;

        Assert.True(vm.HasBothSelected);
        Assert.True(vm.ShowResults);
        Assert.Null(vm.ErrorMessage);

        var vida = vm.StatRows.Single(r => r.ValueA == "100" && r.ValueB == "500");
        Assert.True(vida.IsDifferent);

        var casco = vm.EquipmentRows[0]; // slot 0 = Cabeza, ver CompareViewModel.RebuildRows
        Assert.True(casco.IsDifferent);
        Assert.NotEqual(casco.ItemA.Name, casco.ItemB.Name);

        var slot0A = vm.InventoryA[0];
        var slot0B = vm.InventoryB[0];
        Assert.True(slot0A.IsDifferent);
        Assert.True(slot0B.IsDifferent);
        Assert.Equal(50, slot0A.Item.Count);
        Assert.Equal(999, slot0B.Item.Count);

        Assert.True(vm.DifferenceCount >= 3);

        Limpiar(pathA); Limpiar(pathB);
    }

    [Fact]
    public void DosPersonajesConLosMismosDatosReales_CeroDiferencias()
    {
        var (a, pathA) = NuevaEntrada("Gemelo1", 400, 5, 2, 10, 7);
        var (b, pathB) = NuevaEntrada("Gemelo2", 400, 5, 2, 10, 7);
        var vm = new CompareViewModel([a, b]) { SelectedA = a, SelectedB = b };

        Assert.Equal(0, vm.DifferenceCount);
        Assert.All(vm.StatRows, r => Assert.False(r.IsDifferent));
        Assert.All(vm.EquipmentRows, r => Assert.False(r.IsDifferent));
        Assert.All(vm.InventoryA, r => Assert.False(r.IsDifferent));

        Limpiar(pathA); Limpiar(pathB);
    }

    [Fact]
    public void FicheroIlegible_MuestraElErrorRealSinTumbarElPanel()
    {
        var (bOk, pathB) = NuevaEntrada("Bueno", 100, 1, 0, 2, 1);
        string dirRoto = Path.Combine(Path.GetTempPath(), $"compare-vm-roto-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirRoto);
        string pathRoto = Path.Combine(dirRoto, "Roto.plr");
        File.WriteAllBytes(pathRoto, [0x01, 0x02, 0x03]); // ilegible a proposito
        var caracterFalso = new PlrCharacter { Name = "Roto", Version = 279, PrimaryLoadout = PlrLoadout.CreateEmpty(true) };
        var aRoto = new CharacterListEntryViewModel(pathRoto, caracterFalso, false, null, DateTime.UtcNow, EquipAppearance);
        var vm = new CompareViewModel([aRoto, bOk]);

        vm.SelectedA = aRoto;
        vm.SelectedB = bOk;

        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.ShowResults);
        Assert.Empty(vm.StatRows);

        Directory.Delete(dirRoto, recursive: true);
        Limpiar(pathB);
    }

    [Fact]
    public void CambiarDeIdioma_ReconstruyeLosRotulosDeLasFilas()
    {
        var (a, pathA) = NuevaEntrada("A", 100, 1, 0, 2, 1);
        var (b, pathB) = NuevaEntrada("B", 500, 1, 0, 2, 1);
        var vm = new CompareViewModel([a, b]) { SelectedA = a, SelectedB = b };
        var filaVida = vm.StatRows.Single(r => r.ValueA == "100");

        LocalizationService.Instance.SetLanguage("es");
        Assert.Equal("Vida máxima", filaVida.Label);

        try
        {
            LocalizationService.Instance.SetLanguage("en");
            var filaVidaIngles = vm.StatRows.Single(r => r.ValueA == "100");
            Assert.Equal("Max health", filaVidaIngles.Label);
        }
        finally
        {
            LocalizationService.Instance.SetLanguage("es");
        }

        Limpiar(pathA); Limpiar(pathB);
    }

    // Bug real de la primera captura de la ronda (13-sep-2026, arnes COMPARE_SOLO): el escaneo
    // real de Home corre en un Task de fondo, asi que la lista puede seguir vacia justo cuando
    // se construye MainViewModel (que ya construye Compare) - NeedsMoreCharacters se leyo UNA
    // vez con la lista todavia vacia y, al ser una propiedad calculada sin notificacion, se
    // quedo congelada en True para siempre - el aviso "hacen falta al menos 2 personajes" se
    // veia POR ENCIMA de los resultados reales despues de elegir los dos. Aqui se reproduce el
    // mismo orden real (coleccion vacia al construir, se rellena DESPUES) sobre una
    // ObservableCollection real (Home.Characters lo es siempre en la app).
    [Fact]
    public void ListaQueSeRellenaDespuesDeConstruir_ActualizaNeedsMoreCharacters()
    {
        var lista = new System.Collections.ObjectModel.ObservableCollection<CharacterListEntryViewModel>();
        var vm = new CompareViewModel(lista);
        Assert.True(vm.NeedsMoreCharacters); // vacia todavia, como en el arranque real

        var (a, pathA) = NuevaEntrada("A", 100, 1, 0, 2, 1);
        var (b, pathB) = NuevaEntrada("B", 100, 1, 0, 2, 1);
        lista.Add(a);
        lista.Add(b);

        Assert.False(vm.NeedsMoreCharacters); // ya se puede comparar de verdad

        Limpiar(pathA); Limpiar(pathB);
    }

    [Fact]
    public void CerrarYAbrirElPanel_AlternaIsOpen()
    {
        var vm = new CompareViewModel([]);
        Assert.False(vm.IsOpen);

        vm.OpenCommand.Execute(null);
        Assert.True(vm.IsOpen);

        vm.CloseCommand.Execute(null);
        Assert.False(vm.IsOpen);
    }
}
