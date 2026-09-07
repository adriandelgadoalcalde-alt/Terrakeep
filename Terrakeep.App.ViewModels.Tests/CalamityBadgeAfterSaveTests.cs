using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H3-15 (tercera auditoria de Opus, Fable): antes de este arreglo, HasCalamityData (la
// insignia de Calamity de la cabecera) solo se recalculaba en LoadFromPath - guardar por
// PRIMERA VEZ un objeto de Calamity sobre un personaje que antes era 100% vanilla (sin .tplr)
// creaba el .tplr de verdad (CharacterFileService.Save ya deja loaded.TplrPath puesto), pero la
// insignia se quedaba apagada hasta la siguiente recarga.
public sealed class CalamityBadgeAfterSaveTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    [Fact]
    public void GuardarElPrimerObjetoDeCalamitySobreUnPersonajeVainilla_EnciendeLaInsigniaAlInstante()
    {
        string path = Path.Combine(Path.GetTempPath(), $"calamity-badge-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Vainilla")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        Assert.False(vm.HasCalamityData); // recien cargado, sin .tplr todavia

        vm.InventoryContainer!.Slots[0].PlaceItem(CalamityIds.ItemIdBase); // primer objeto real de Calamity
        vm.SaveCommand.Execute(null); // este guardado crea el .tplr por primera vez

        Assert.True(vm.HasCalamityData); // sin necesidad de recargar

        string tplrPath = Path.ChangeExtension(path, ".tplr");
        File.Delete(path);
        File.Delete(path + ".bak");
        if (File.Exists(tplrPath)) File.Delete(tplrPath);
    }
}
