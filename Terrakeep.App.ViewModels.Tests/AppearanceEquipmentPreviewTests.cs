using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H6-06 (sexta auditoria de Opus, Tanda D - "unificar el doll de Apariencia con el de Inicio,
// que YA muestra la armadura/vanidad real puesta"): AppearanceViewModel.PreviewImage refleja el
// equipo puesto real (PrimaryLoadout) por defecto, con un toggle real para apagarlo, y se
// actualiza EN VIVO cuando el usuario cambia el equipo en la pestaña Objetos - no solo al
// recargar el personaje.
public sealed class AppearanceEquipmentPreviewTests
{
    // Casco de cobre (id=89, headSlot=1 real) - mismo objeto ya usado como spot-check real en
    // EquipmentAppearanceResolverTests.cs.
    private const int CascoCobre = 89;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"appearance-equip-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    private static byte[] Pixels(System.Windows.Media.Imaging.WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    [Fact]
    public void ShowEquipmentPorDefecto_EsTrue()
    {
        var vm = NewLoadedViewModel();
        Assert.True(vm.Appearance.ShowEquipment);
    }

    [Fact]
    public void EquiparUnCasco_ActualizaElPreviewEnVivo()
    {
        var vm = NewLoadedViewModel();
        var sinCasco = Pixels(vm.Appearance.PreviewImage!);

        vm.EquipmentGroup!.EquippedItems.Slots[0].PlaceItem(CascoCobre); // dispara OnSlotItemChanged real

        var conCasco = Pixels(vm.Appearance.PreviewImage!);
        Assert.NotEqual(sinCasco, conCasco);
    }

    [Fact]
    public void ApagarShowEquipment_QuitaElCascoDelPreview()
    {
        var vm = NewLoadedViewModel();
        vm.EquipmentGroup!.EquippedItems.Slots[0].PlaceItem(CascoCobre);
        var conCasco = Pixels(vm.Appearance.PreviewImage!);

        vm.Appearance.ShowEquipment = false;

        var sinCasco = Pixels(vm.Appearance.PreviewImage!);
        Assert.NotEqual(conCasco, sinCasco);
    }

    [Fact]
    public void ReactivarShowEquipment_VuelveAMostrarElCasco()
    {
        var vm = NewLoadedViewModel();
        vm.EquipmentGroup!.EquippedItems.Slots[0].PlaceItem(CascoCobre);
        var conCascoAntes = Pixels(vm.Appearance.PreviewImage!);

        vm.Appearance.ShowEquipment = false;
        vm.Appearance.ShowEquipment = true;

        var conCascoDespues = Pixels(vm.Appearance.PreviewImage!);
        Assert.Equal(conCascoAntes, conCascoDespues);
    }
}
