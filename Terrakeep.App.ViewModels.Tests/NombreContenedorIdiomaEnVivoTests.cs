using System.IO;
using System.Collections.ObjectModel;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// FASE B del responsive global, correccion V-03 del revisor visual (28-sep-2026): al cambiar a
// ingles EN VIVO, "Monedas (4/4)", "Municion (4/4)" y el subtitulo del panel Editar ("Loadout 1 -
// armadura/accesorios", ItemSlotViewModel.ContainerName) seguian en español - el nombre se
// resolvia UNA vez al construir el contenedor. Con el constructor de resolvedor, el contenedor y
// sus slots siguen al idioma real.
public sealed class NombreContenedorIdiomaEnVivoTests
{
    private static readonly CharacterFileService Service = new();

    [Fact]
    public void ElNombreDelContenedorYElDeSusSlotsSiguenAlIdiomaEnVivo()
    {
        var loc = LocalizationService.Instance;
        string previo = loc.Language;
        try
        {
            loc.SetLanguage(LocalizationService.Spanish);
            var slots = new ObservableCollection<ItemSlotViewModel>
            {
                new(Service, 0, loc["storage_coins"], GameItem.Empty),
                new(Service, 1, loc["storage_coins"], GameItem.Empty),
            };
            var cont = new ContainerViewModel("coins", () => loc["storage_coins"], slots);
            string es = loc["storage_coins"];
            Assert.StartsWith(es, cont.DisplayName);

            var avisos = new List<string?>();
            cont.PropertyChanged += (_, e) => avisos.Add(e.PropertyName);
            loc.SetLanguage(LocalizationService.English);
            string en = loc["storage_coins"];

            Assert.NotEqual(es, en);
            Assert.StartsWith(en, cont.DisplayName);
            Assert.All(slots, s => Assert.Equal(en, s.ContainerName));
            Assert.Contains(nameof(ContainerViewModel.DisplayName), avisos);

            loc.SetLanguage(LocalizationService.Spanish);
            Assert.StartsWith(es, cont.DisplayName);
            Assert.All(slots, s => Assert.Equal(es, s.ContainerName));
        }
        finally { loc.SetLanguage(previo); }
    }

    // Con un personaje REAL cargado (.plr temporal): cambiar de idioma traduce Monedas/Municion y
    // el nombre del loadout (subtitulo de Editar) y NO marca el personaje como modificado - antes
    // si lo marcaba (Apariencia re-traducia HairDyeDisplayName y MainViewModel lo tomaba por una
    // edicion; igual los slots de objeto y de buff al re-traducir su nombre), y el siguiente
    // "abrir otro personaje" ofrecia guardarlo - con "Si", escribia el .plr.
    [Fact]
    public void CambiarDeIdiomaConPersonajeCargado_TraduceYNoMarcaCambios()
    {
        var loc = LocalizationService.Instance;
        string previo = loc.Language;
        string path = Path.Combine(Path.GetTempPath(), $"idioma-envivo-{Guid.NewGuid():N}.plr");
        try
        {
            loc.SetLanguage(LocalizationService.Spanish);
            File.WriteAllBytes(path, PlrFile.Write(new PlrCharacter
            {
                Name = "Idioma",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
                // Un buff real: sus textos tambien se re-traducen y antes marcaban IsDirty.
                Buffs = [new PlrBuff { Id = 1, Time = 600 }],
            }));
            var vm = new MainViewModel();
            vm.LoadFromPath(path);
            Assert.False(vm.IsDirty);
            string monedasEs = vm.CoinsContainer!.DisplayName;
            string loadoutEs = vm.EquipmentGroup!.Current.Slots[0].ContainerName;
            string? estadoEs = vm.StatusMessage, versionEs = vm.FileVersionLine;

            string? pila = null;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainViewModel.IsDirty) && vm.IsDirty) pila ??= Environment.StackTrace; };
            loc.SetLanguage(LocalizationService.English);

            Assert.True(!vm.IsDirty, "IsDirty marcado por el cambio de idioma desde: " + pila); // pila: diagnostico legible si vuelve a pasar
            Assert.NotEqual(monedasEs, vm.CoinsContainer.DisplayName);
            Assert.StartsWith(loc["storage_coins"], vm.CoinsContainer.DisplayName);
            Assert.NotEqual(loadoutEs, vm.EquipmentGroup.Current.Slots[0].ContainerName);
            Assert.StartsWith(vm.EquipmentGroup.Current.Slots[0].ContainerName, vm.EquipmentGroup.Current.DisplayName);
            // H-03: mensaje "Cargado 'X'..." y linea "archivo · version N" re-traducidos tambien.
            Assert.NotEqual(estadoEs, vm.StatusMessage);
            Assert.Equal(loc.Format("status_loaded_vanilla_only", "Idioma"), vm.StatusMessage);
            Assert.NotEqual(versionEs, vm.FileVersionLine);
        }
        finally
        {
            loc.SetLanguage(previo);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // El constructor de texto fijo (cofres, tests...) no cambia de comportamiento.
    [Fact]
    public void ElConstructorDeTextoFijoNoSeSuscribeAlIdioma()
    {
        var loc = LocalizationService.Instance;
        string previo = loc.Language;
        try
        {
            loc.SetLanguage(LocalizationService.Spanish);
            var cont = new ContainerViewModel("k", "Fijo", new ObservableCollection<ItemSlotViewModel> { new(Service, 0, "Fijo", GameItem.Empty) });
            loc.SetLanguage(LocalizationService.English);
            Assert.StartsWith("Fijo", cont.DisplayName);
        }
        finally { loc.SetLanguage(previo); }
    }
}
