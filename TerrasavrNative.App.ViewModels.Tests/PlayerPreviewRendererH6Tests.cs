using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Sexta auditoria de Opus, H6-01/H6-03/H6-04/H6-05: PlayerPreviewRenderer reescrito de raiz
// para recortar las celdas REALES de las hojas compuestas 360x224 (antes siempre (0,0), que
// dejaba a todos los personajes sin brazos - ver el comentario real de PlayerPreviewRenderer.cs
// citando PlayerDrawSet.cs/PlayerDrawLayers.cs decompilados). Estas pruebas verifican
// comportamiento observable real (pixeles distintos), no solo "no lanza excepcion".
public sealed class PlayerPreviewRendererH6Tests
{
    private static readonly CharacterFileService Service = new();

    private static readonly PlayerPreviewRenderer.PlayerColors Colors = new(
        new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
        new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

    private static byte[] Pixels(System.Windows.Media.Imaging.WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    private static int ContarPixelesOpacos(byte[] pixels)
    {
        int count = 0;
        for (int i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0) count++;
        return count;
    }

    [Fact]
    public void RenderDeCuerpoCompleto_TieneUnaCantidadRealDePixelesOpacos()
    {
        // H6-01: antes del arreglo, las 8 piezas compuestas (torso/brazos/manos/camisas) se
        // recortaban SIEMPRE en la celda (0,0) de su hoja - para varias de ellas (ArmSkin,
        // Hands, ArmUndershirt, ArmShirt) esa celda cae fuera del area real dibujada del
        // personaje en reposo y compone practicamente vacia. Un lienzo con "vida" real (mucho
        // mas que solo cabeza+piernas) es la señal observable de que los brazos/torso realmente
        // se estan componiendo.
        var bmp = PlayerPreviewRenderer.Render(1, isMale: true, Colors);
        int opacos = ContarPixelesOpacos(Pixels(bmp));

        // 40x56 = 2240 pixeles totales; medido de verdad con el arreglo real: 908 opacos. El
        // umbral (700) deja margen real y sigue distinguiendo esto de la regresion original: con
        // el bug de H6-01 (celda SIEMPRE (0,0)) ArmSkin/Hands/ArmUndershirt/ArmShirt no aportaban
        // pixeles reales del personaje y el recuento caia muy por debajo (cabeza+piernas a secas).
        Assert.True(opacos > 700, $"Solo {opacos}/2240 pixeles opacos - los brazos/torso no se estan componiendo de verdad.");
    }

    [Fact]
    public void CambiarUnderColor_CambiaLosPixelesDeSalida()
    {
        // La camiseta interior (Undershirt) es una de las piezas compuestas que antes del
        // arreglo H6-01 se recortaba mal - confirma que el tintado real SI llega hasta el
        // lienzo final, no solo que la funcion no lance.
        var conUnderOscuro = PlayerPreviewRenderer.Render(1, isMale: true, Colors);
        var otros = Colors with { Under = new(255, 0, 255) };
        var conUnderClaro = PlayerPreviewRenderer.Render(1, isMale: true, otros);

        Assert.NotEqual(Pixels(conUnderOscuro), Pixels(conUnderClaro));
    }

    [Fact]
    public void VaronYMujer_ProducenLienzosDistintos()
    {
        // H6-03: TorsoFrame/FrontShoulderFrame/BackShoulderFrame cambian de celda real segun el
        // genero (fila 0-1 varon, fila 2-3 mujer) - los brazos NO cambian (misma celda en los
        // dos generos), pero torso/hombros si, asi que el resultado final debe diferir.
        var varon = PlayerPreviewRenderer.Render(1, isMale: true, Colors);
        var mujer = PlayerPreviewRenderer.Render(1, isMale: false, Colors);

        Assert.NotEqual(Pixels(varon), Pixels(mujer));
    }

    [Fact]
    public void DosPeinadosDistintos_CarganSpritesRealesDistintos()
    {
        // H6-04: HairStyle es 0-based tal cual (Player.hair real) - confirma que dos ids
        // vecinos cargan dos ficheros REALES distintos (no los dos caen por error en el mismo
        // fallback "hair/0.png").
        var pelo5 = PlayerPreviewRenderer.Render(5, isMale: true, Colors);
        var pelo6 = PlayerPreviewRenderer.Render(6, isMale: true, Colors);

        Assert.NotEqual(Pixels(pelo5), Pixels(pelo6));
    }

    [Fact]
    public void ArmaduraDeCalamityEnElSlotCuerpo_RenderizaSinExcepcionYCambiaElResultado()
    {
        // H6-05: antes de este arreglo, una pieza de Calamity en el slot Body (hoja compuesta
        // 360x224, misma convencion que el cuerpo vanilla) hacia explotar CopyPixels
        // (ArgumentOutOfRangeException real, porque el codigo antiguo asumia SIEMPRE 40x56) -
        // HomeViewModel.ScanCharacters lo tragaba en un catch mudo y el personaje desaparecia
        // de "Inicio" en silencio. LoadArmorCell (mismo mecanismo que el cuerpo) lo resuelve.
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Body");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[1] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);
        var armor = Service.EquipmentAppearance.Resolve(loadout);
        Assert.NotNull(armor.BodyFile); // spot-check de que el fixture realmente ejercita el slot Body

        var sinArmadura = PlayerPreviewRenderer.Render(1, isMale: true, Colors);
        var conArmaduraCalamity = PlayerPreviewRenderer.Render(1, isMale: true, Colors, armor);

        Assert.NotEqual(Pixels(sinArmadura), Pixels(conArmaduraCalamity));
    }
}
