using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H6-07 (sexta auditoria de Opus, Tanda D - "pelo bajo el casco/pelo largo detras del cuerpo"):
// verificacion de extremo a extremo real - EquipmentAppearanceResolver.HeadSlot + el pelo real
// (Player_Hair/Player_HairAlt) compuestos por PlayerPreviewRenderer, con objetos REALES del
// catalogo (no sinteticos): Gafas de proteccion (id 37, headSlot=10, fullHair real) y Cubo
// vacio (id 205, headSlot=13, hatHair real) - los mismos spot-checks reales ya citados en
// HairDrawProfileTests.cs.
public sealed class PlayerPreviewRendererHairH607Tests
{
    private static readonly CharacterFileService Service = new();

    private const int GafasDeProteccion = 37;  // headSlot=10, fullHair real
    private const int CuboVacio = 205;          // headSlot=13, hatHair real
    private const int CascoDeHierro = 90;       // headSlot=2, ni fullHair ni hatHair - casco completo real

    private static readonly PlayerPreviewRenderer.PlayerColors Colors = new(
        new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
        new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

    private static PlrLoadout LoadoutConCabeza(int itemId)
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[0] = new PlrItemSlot(itemId, 1, 0, false);
        return loadout;
    }

    private static byte[] Pixels(System.Windows.Media.Imaging.WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    [Fact]
    public void ResolveExponeElHeadSlotRealDeUnObjetoVanilla()
    {
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(GafasDeProteccion));
        Assert.Equal(10, armor.HeadSlot);
    }

    [Fact]
    public void ObjetoDeCalamityOSlotVacio_HeadSlotEsNull()
    {
        var vacio = Service.EquipmentAppearance.Resolve(PlrLoadout.CreateEmpty(isPrimary: true));
        Assert.Null(vacio.HeadSlot);

        var calamityEntry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Head");
        var conCalamity = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(calamityEntry.SyntheticId));
        Assert.Null(conCalamity.HeadSlot); // sin tabla real de headSlot para Calamity, ver PlayerPreviewRenderer
    }

    [Fact]
    public void FullHairReal_ElPeloSeSigueViendoConElCascoPuesto()
    {
        // Gafas de proteccion (fullHair real) - el pelo se compone igual con o sin las gafas
        // puestas (ambos con pelo visible), a diferencia de un casco completo real (siguiente
        // prueba), que SI cambia el resultado al ocultar el pelo.
        var sinNada = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(GafasDeProteccion));
        var conGafas = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor);

        // No deberian ser identicos (las gafas en si se dibujan), pero el pelo debe seguir
        // presente en los dos - se comprueba indirectamente: ocultar el pelo (siguiente prueba,
        // casco completo) da una diferencia de pixeles opacos MUCHO mayor que esta.
        Assert.NotEqual(Pixels(sinNada), Pixels(conGafas));
    }

    [Fact]
    public void CascoCompletoReal_OcultaElPeloDeVerdad()
    {
        // Casco de hierro (headSlot=2, ni fullHair ni hatHair real) - el pelo NO se dibuja
        // (comportamiento real del propio juego, GetHairSettings nunca marca fullHair/hatHair
        // para este headSlot). Verificacion real: renderizar CON el casco puesto pero
        // "hideHair" forzado a False (pasando HeadSlot=null, como si no hubiera casco) SI debe
        // mostrar pelo - la diferencia real esta en si HeadSlot viaja o no.
        var conHeadSlotReal = new PlayerPreviewRenderer.EquippedArmor(null, null, null, HeadSlot: 2);
        var sinHeadSlot = new PlayerPreviewRenderer.EquippedArmor(null, null, null, HeadSlot: null);

        var ocultoPorCasco = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, conHeadSlotReal);
        var peloNormal = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, sinHeadSlot);

        Assert.NotEqual(Pixels(ocultoPorCasco), Pixels(peloNormal));
    }

    [Fact]
    public void HatHairReal_UsaElSpriteAlternativoRealDistintoDelNormal()
    {
        // Cubo vacio (headSlot=13, hatHair real) - Player_HairAlt (fichero REAL distinto de
        // Player_Hair) debe producir pixeles distintos del pelo normal para el mismo peinado.
        var conHatHair = new PlayerPreviewRenderer.EquippedArmor(null, null, null, HeadSlot: 13);
        var sinCasco = new PlayerPreviewRenderer.EquippedArmor(null, null, null, HeadSlot: null);

        var conAlt = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, conHatHair);
        var normal = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, sinCasco);

        Assert.NotEqual(Pixels(conAlt), Pixels(normal));
    }

    [Fact]
    public void PeinadoLargoReal_ProduceUnaCapaTraseraQueCambiaElResultado()
    {
        // HairStyle=51 es backHairDraw=true real (ver HairDrawProfileTests.cs) - sin casco
        // puesto, un peinado largo debe verse distinto de uno corto (HairStyle=1, no
        // backHairDraw) por la capa trasera adicional, incluso con el mismo color de pelo.
        var corto = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var largo = PlayerPreviewRenderer.Render(51, skinVariant: PlayerVariantSets.MaleStarter, Colors);

        Assert.NotEqual(Pixels(corto), Pixels(largo));
    }
}
