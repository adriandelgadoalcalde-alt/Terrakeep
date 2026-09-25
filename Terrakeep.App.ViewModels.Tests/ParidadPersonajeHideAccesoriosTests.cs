using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// ParidadPersonaje Fase1 (25-sep-2026): test de INTEGRACION real pedido explicitamente por el
// encargo - no basta con el test unitario ya existente de
// EquipmentAppearanceResolver.ResolveAccessories (ver
// EquipmentAppearanceResolverTests.HideTrueEnHuecoFuncional_...). Aqui se carga el MISMO .plr
// real por los DOS caminos completos de la app:
//   - CharacterListEntryViewModel (doll de "Inicio", ya correcto ANTES de este arreglo - lee
//     character.Loadouts[CurrentLoadout].Hide, ver su propio comentario).
//   - MainViewModel + AppearanceViewModel (preview en vivo de "Personaje > Apariencia", el que
//     arregla este encargo - MainViewModel.cs, RefreshAppearanceEquipment()).
// y se compara el EquippedAccessories resultante con igualdad ESTRUCTURAL de record (no solo
// pixeles renderizados). Antes de este arreglo, MainViewModel.RefreshAppearanceEquipment()
// llamaba ResolveAccessories SIN el Hide[] real del personaje (hide=null siempre) - el preview de
// Apariencia mostraba SIEMPRE "nada oculto" aunque el personaje guardado tuviera un accesorio
// funcional oculto de verdad, mientras que Inicio SI lo respetaba: las dos tarjetas del mismo
// personaje discrepaban entre si.
public sealed class ParidadPersonajeHideAccesoriosTests
{
    private static readonly CharacterFileService Service = new();
    private static readonly EquipmentAppearanceResolver EquipAppearance = Service.EquipmentAppearance;

    // Mismos objetos reales ya usados como spot-check en EquipmentAppearanceResolverTests.cs.
    private const int RelojCobre = 15;      // waistSlot=2 (funcional, oculto por Hide)
    private const int RelojPlata = 16;      // waistSlot=7 (vanidad, MISMO tipo que RelojCobre)
    private const int ColgantePlata = 554;  // neckSlot=2 (funcional, oculto, SIN vanidad de respaldo)

    private static PlrCharacter NuevoPersonajeConHideYVanidad()
    {
        var primary = PlrLoadout.CreateEmpty(isPrimary: true);
        primary.Items[3] = new PlrItemSlot(RelojCobre, 1, 0, false);
        primary.Items[4] = new PlrItemSlot(ColgantePlata, 1, 0, false);
        primary.Social[7] = new PlrItemSlot(RelojPlata, 1, 0, false);

        var hide = new bool[10];
        hide[3] = true; // oculta el reloj de cobre funcional (waist) - hay vanidad de respaldo
        hide[4] = true; // oculta el colgante funcional (neck) - SIN vanidad de respaldo

        // Loadouts[CurrentLoadout] real: PrimaryLoadout NUNCA lleva Hide (ver PlrLoadout.cs),
        // el array de 10 bits real vive aqui - mismo dato que lee CharacterListEntryViewModel.
        var loadoutActivo = new PlrLoadout { Hide = hide };

        return new PlrCharacter
        {
            Name = "ParidadHide",
            Version = 279,
            PrimaryLoadout = primary,
            Loadouts = [loadoutActivo, PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            CurrentLoadout = 0,
        };
    }

    [Fact]
    public void InicioYAparienciaProducenElMismoEquippedAccessories_ConHideYVanidadReales()
    {
        var character = NuevoPersonajeConHideYVanidad();
        string path = Path.Combine(Path.GetTempPath(), $"paridad-hide-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        try
        {
            // Camino de "Inicio" - ya correcto antes de este arreglo.
            var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, null, DateTime.UtcNow, EquipAppearance);

            // Camino de "Personaje > Apariencia" - el que arregla este encargo.
            var vm = new MainViewModel();
            vm.LoadFromPath(path);

            var accesoriosInicio = entry.AccesoriosParaPruebas;
            var accesoriosApariencia = vm.Appearance.AccesoriosParaPruebas;

            Assert.NotNull(accesoriosApariencia);
            // Igualdad ESTRUCTURAL de record: mismos 30+ campos, no solo el mismo tipo.
            Assert.Equal(accesoriosInicio, accesoriosApariencia);

            // El colgante funcional oculto (sin vanidad de respaldo) tiene que haber
            // desaparecido de verdad en los DOS caminos - si los dos coincidieran mostrando el
            // bug igual de mal (los dos con NeckFile relleno), Assert.Equal de arriba pasaria
            // igual sin detectar nada: esta comprobacion adicional es la que de verdad ata el
            // resultado al comportamiento CORRECTO, no solo a que los dos caminos concuerden
            // entre si por casualidad.
            Assert.Null(accesoriosInicio.NeckFile);
            Assert.Null(accesoriosApariencia!.NeckFile);

            // El reloj de plata (vanidad) SI tiene que verse en los dos - confirma que Hide no
            // rompe el caso normal de vanidad tapando al funcional del mismo tipo.
            Assert.NotNull(accesoriosInicio.WaistFile);
            Assert.EndsWith("acc_waist" + Path.DirectorySeparatorChar + "7.png", accesoriosInicio.WaistFile);
            Assert.Equal(accesoriosInicio.WaistFile, accesoriosApariencia.WaistFile);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
