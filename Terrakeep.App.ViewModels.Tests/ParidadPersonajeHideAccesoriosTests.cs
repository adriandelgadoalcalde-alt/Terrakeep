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
//   - CharacterListEntryViewModel (doll de "Inicio").
//   - MainViewModel + AppearanceViewModel (preview en vivo de "Personaje > Apariencia").
// y se compara el EquippedAccessories resultante con igualdad ESTRUCTURAL de record (no solo
// pixeles renderizados).
//
// Reescrito (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514): la version anterior
// de este test fabricaba "new PlrLoadout { Hide = hide }" a mano y lo asignaba como
// Loadouts[CurrentLoadout] - reproduciendo la MISMA interpretacion incorrecta que tenia el bug de
// produccion (PlrCharacter.ResolveActiveHide leyendo Loadouts[CurrentLoadout].Hide como si fuera
// el estado activo). Ese test seguia en verde incluso con el bug real, porque nunca ejercitaba el
// camino real HideVisual1/HideVisual2 en un personaje CON Loadouts. Ahora representa la
// serializacion real: HideVisual1/HideVisual2 con bits reales puestos (el estado activo real,
// hideVisibleAccessory) y Loadouts[CurrentLoadout].Hide a todo-false (exactamente el caso real
// observado en Terrariano.plr/Eldelgas.plr del usuario) - confirma que los dos caminos usan el
// Hide derivado de HideVisual1/HideVisual2, ignorando el Hide (todo-false) del loadout.
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

        // Loadouts[CurrentLoadout].Hide a todo-false a proposito - el caso real observado en
        // Terrariano.plr/Eldelgas.plr del usuario. El estado activo real (el que debe respetarse)
        // vive SOLO en HideVisual1/HideVisual2 - si algun consumidor volviera a leer
        // Loadouts[CurrentLoadout].Hide como si fuera el activo, este test lo detectaria (los dos
        // accesorios funcionales apareceran visibles en vez de ocultos).
        var loadoutActivo = new PlrLoadout { Hide = new bool[10] };

        return new PlrCharacter
        {
            Name = "ParidadHide",
            Version = 279,
            PrimaryLoadout = primary,
            Loadouts = [loadoutActivo, PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            CurrentLoadout = 0,
            HideVisual1 = (1 << 3) | (1 << 4), // bits 3 (waist) y 4 (neck) activos: estado activo real
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
            // Camino de "Inicio".
            var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, null, DateTime.UtcNow, EquipAppearance);

            // Camino de "Personaje > Apariencia".
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
