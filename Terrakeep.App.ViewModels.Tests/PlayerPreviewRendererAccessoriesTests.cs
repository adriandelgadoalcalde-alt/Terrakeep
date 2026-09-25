using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App.Services;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// PortSeleccion Encargo2 (25-sep-2026): PlayerPreviewRenderer.Render extendido con las 7 capas
// de accesorio real (EquipmentAppearanceResolver.ResolveAccessories, Encargo1) insertadas en su
// posicion exacta dentro de la secuencia de composicion ya existente - ver el comentario real de
// la firma de Render para la cita completa de PlayerDrawLayers.cs (lineas 1236/3374/3425/3444/
// 4484/4490/4955-4961/6161).
//
// Dos familias de prueba:
//   1. "wiring": 7 objetos vanilla REALES (uno por tipo, con sprite ya extraido en este PC -
//      ver scripts/extraer-sprites-accesorios-vanilla.js) confirman que ResolveAccessories ->
//      Render llega de verdad hasta el lienzo final (pixeles distintos con/sin el accesorio).
//   2. "orden": contrato de composicion. Un PNG 40x56 SOLIDO (opaco entero) sintetico por capa,
//      compuesto en la esquina (0,0) del lienzo - confirmado real y ESTABLE (Diag_
//      FindTransparentCorner, hairStyle=1/MaleStarter/sin armadura: NINGUNA capa real del cuerpo
//      -piel, ropa, pelo, cabeza, brazos- pinta nunca esa esquina, ver el mapa de pixeles
//      capturado en la investigacion de este mismo encargo) - asi que el ULTIMO accesorio
//      sintetico en pisar esa esquina es, sin ambiguedad, el que el codigo real compuso MAS
//      TARDE. Seis pares adyacentes cubren la subsecuencia completa declarada por el arquitecto
//      (Back < OffhandAcc < WaistAcc < NeckAcc < FaceAcc < Shield < HandOnAcc) - si dos capas se
//      invirtieran en PlayerPreviewRenderer.Render, el par correspondiente fallaria.
public sealed class PlayerPreviewRendererAccessoriesTests : IDisposable
{
    private static readonly CharacterFileService Service = new();

    private static readonly PlayerPreviewRenderer.PlayerColors Colors = new(
        new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
        new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "TerrakeepTests_AccOrder_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static byte[] Pixels(WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    // ---- Familia 1: wiring real (7 objetos vanilla reales, uno por tipo) ----

    private static PlrLoadout LoadoutConAccesorio(int itemId)
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(itemId, 1, 0, false); // cualquier hueco 3..9 vale, son genericos (ver ResolveAccessories)
        return loadout;
    }

    // Objetos vanilla reales con sprite YA extraido en este PC (confirmado con un barrido de
    // vanilla_accessory_slots.json contra Assets/player/acc_*, 25-sep-2026) - uno por cada uno
    // de los 7 tipos.
    public static TheoryData<string, int> ObjetosRealesPorTipo => new()
    {
        { "Waist (Cinturon de bandolero - id 15)", 15 },
        { "HandOn (id 49)", 49 },
        { "Shield (id 156)", 156 },
        { "Face (id 193)", 193 },
        { "HandOff (id 211)", 211 },
        { "Back (id 532)", 532 },
        { "Neck (id 554)", 554 },
    };

    [Theory]
    [MemberData(nameof(ObjetosRealesPorTipo))]
    public void ObjetoRealDeAccesorio_CambiaElResultadoRespectoASinAccesorios(string _, int itemId)
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(itemId));

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void SinAccesorios_ElParametroPorDefectoNoLanzaYEsIdenticoAPasarNull()
    {
        // El bug real atrapado durante este mismo encargo: EquippedAccessories es un record
        // (tipo referencia) - "accessories = default" da NULL, no una instancia vacia. Confirma
        // que el valor por defecto de Render sigue funcionando (nunca un NRE) y produce el MISMO
        // resultado que pasar null explicitamente.
        var porDefecto = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conNullExplicito = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: null);
        Assert.Equal(Pixels(porDefecto), Pixels(conNullExplicito));
    }

    // ---- Familia 2: contrato de orden de composicion ----

    private string CrearPngSolido(byte r, byte g, byte b)
    {
        Directory.CreateDirectory(_tempDir);
        string path = Path.Combine(_tempDir, $"{r}_{g}_{b}_{Guid.NewGuid():N}.png");
        const int w = 40, h = 56;
        var pixels = new byte[w * h * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = b; pixels[i + 1] = g; pixels[i + 2] = r; pixels[i + 3] = 255; // Bgra32, opaco
        }
        var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    // Esquina (0,0), 4x4 real: confirmada transparente en TODA capa real del cuerpo para
    // hairStyle=1/MaleStarter/sin armadura (ver el comentario de cabecera). Un pixel solo (0,0)
    // ya basta, pero se comprueban 3 para descartar un problema de recorte de 1px por error de
    // offset en LoadStripFrameAbsolute/SliceStripRow.
    private static void AssertEsquinaEsColor(WriteableBitmap bmp, byte r, byte g, byte b)
    {
        var pixels = Pixels(bmp);
        foreach (var (x, y) in new[] { (0, 0), (1, 0), (0, 1) })
        {
            int idx = (y * bmp.PixelWidth + x) * 4;
            Assert.True(pixels[idx + 2] == r && pixels[idx + 1] == g && pixels[idx + 0] == b,
                $"Esquina ({x},{y}): esperaba RGB({r},{g},{b}), salio RGB({pixels[idx + 2]},{pixels[idx + 1]},{pixels[idx + 0]}) - alpha={pixels[idx + 3]}.");
        }
    }

    [Fact]
    public void Orden_BackAntesQueOffhandAcc()
    {
        string back = CrearPngSolido(255, 0, 0), handOff = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, handOff, back, null, null);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // HandOff (verde) pisa a Back (rojo)
    }

    [Fact]
    public void Orden_OffhandAccAntesQueWaistAcc()
    {
        string handOff = CrearPngSolido(255, 0, 0), waist = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(waist, null, null, handOff, null, null, null);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Waist (verde) pisa a HandOff (rojo)
    }

    [Fact]
    public void Orden_WaistAccAntesQueNeckAcc()
    {
        string waist = CrearPngSolido(255, 0, 0), neck = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(waist, neck, null, null, null, null, null);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Neck (verde) pisa a Waist (rojo)
    }

    [Fact]
    public void Orden_NeckAccAntesQueFaceAcc()
    {
        string neck = CrearPngSolido(255, 0, 0), face = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, neck, null, null, null, null, face);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Face (verde) pisa a Neck (rojo)
    }

    [Fact]
    public void Orden_FaceAccAntesQueShield()
    {
        string face = CrearPngSolido(255, 0, 0), shield = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, null, null, shield, face);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Shield (verde) pisa a Face (rojo)
    }

    [Fact]
    public void Orden_ShieldAntesQueHandOnAcc()
    {
        string shield = CrearPngSolido(255, 0, 0), handOn = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, handOn, null, null, shield, null);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // HandOn (verde) pisa a Shield (rojo) - es la ULTIMA capa real de accesorio
    }

    // GapAnalysis Encargo A (25-sep-2026): Backpack/Tail, los 2 canales que reclasifica
    // EquipmentAppearanceResolver.ResolveAccessories (ver BackAccessoryLayerTable) - misma
    // textura real AccBack que Back, solo cambia el campo. Ids reales con sprite ya extraido:
    // Magic Quiver (1321, backSlot=7, Backpack), Dog Tail (4769, backSlot=25, Tail).
    private const int MagicQuiver = 1321;
    private const int DogTail = 4769;

    [Fact]
    public void ObjetoRealDeBackpack_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(MagicQuiver));
        Assert.NotNull(accesorios.BackpackFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void ObjetoRealDeTail_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(DogTail));
        Assert.NotNull(accesorios.TailFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    // Contrato de orden real (LegacyPlayerRenderer.cs:178/180/185): Backpacks -> Tails -> ... ->
    // BackAcc - mismo criterio de esquina (0,0) ya usado para las 6 capas de PortSeleccion
    // Encargo2, extendido con los 2 canales nuevos.
    [Fact]
    public void Orden_BackpackAntesQueTail()
    {
        string backpack = CrearPngSolido(255, 0, 0), tail = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null,
            BackpackFile: backpack, TailFile: tail);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Tail (verde) pisa a Backpack (rojo)
    }

    [Fact]
    public void Orden_TailAntesQueBackNormal()
    {
        string tail = CrearPngSolido(255, 0, 0), back = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, null, back, null, null,
            TailFile: tail);
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);
        AssertEsquinaEsColor(bmp, 0, 255, 0); // Back normal (verde) pisa a Tail (rojo)
    }

    [Fact]
    public void SoloAccesorioDeShield_NoRevientaAunqueElAnchoRealNoSeaLosCuarentaPx()
    {
        // LoadShieldFrame (Encargo2): unico de los 7 tipos con ancho real variable (algunos
        // escudos vanilla miden 42/44px, no 40 - ver el comentario real de esa funcion). El
        // objeto real (id 156) confirma que no revienta y que SI cambia el resultado.
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(156));
        Assert.NotNull(accesorios.ShieldFile);
        var sinNada = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conEscudo = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);
        Assert.NotEqual(Pixels(sinNada), Pixels(conEscudo));
    }

    // GapAnalysis Encargo D (25-sep-2026): shoeSlot - accesorio REAL de zapatos
    // (Player.cs:37193-37200/PlayerDrawLayers.cs:1758-1777), canal COMPLETAMENTE DISTINTO de
    // los zapatos BASE ya cubiertos por "Paso 5" (pants/shoes tintados). Hermes Boots (id 54,
    // shoeSlot=6, sprite ya extraido en Assets/player/acc_shoes/6.png).
    private const int HermesBoots = 54;

    [Fact]
    public void ObjetoRealDeShoes_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(HermesBoots));
        Assert.NotNull(accesorios.ShoesFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void Orden_SinWearsRobe_PernerasAntesQueShoes()
    {
        // Sin body/legs equipado (wearsRobe=false, el caso real mas comun) - orden real
        // (LegacyPlayerRenderer.cs:202-204): "Leggings; Shoes;" - Shoes va DESPUES, pisa a las
        // perneras en la esquina si ambas son opacas ahi. LegsSlot=999 (sintetico, fuera de
        // cualquier tabla real de SetMatch/GetMatchingBodyExtension) fuerza legsChangedBySetMatch
        // = false, asi que Render usa armor.LegsFile TAL CUAL (el PNG solido de aqui) en vez de
        // ir a buscar un armor_legs/{id}.png real - mismo criterio de aislamiento que el resto
        // de pruebas "Orden_*" de esta clase.
        string leggings = CrearPngSolido(255, 0, 0), shoes = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: leggings, LegsSlot: 999);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, ShoesFile: shoes);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // Shoes (verde) pisa a Leggings (rojo)
    }

    [Fact]
    public void Orden_ConWearsRobeReal_PernerasCubrenAZapatos()
    {
        // body=15 real (Player.cs SetMatchBodyToLegs, PlayerBodyDrawTables: "15 => new(88,
        // true)") -> wearsRobe=true real, invierte el orden a "Shoes; Leggings;"
        // (LegacyPlayerRenderer.cs:195-204) - Shoes se dibuja PRIMERO y la pernera/robe lo
        // TAPA despues, fiel al juego real (una falda/robe larga oculta visualmente el
        // accesorio de zapatos que lleva debajo). LegsSlot=88 A PROPOSITO (coincide con el
        // valor que la propia tabla SetMatchBodyToLegs ya iba a poner) - asi
        // legsChangedBySetMatch queda en FALSE (88 == 88) y Render usa armor.LegsFile TAL
        // CUAL (el PNG solido de aqui) en vez de ir a buscar armor_legs/88.png real, aislando
        // el orden sin depender de la transparencia real de ese sprite.
        string shoes = CrearPngSolido(0, 255, 0), leggings = CrearPngSolido(255, 0, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: leggings, BodySlot: 15, LegsSlot: 88);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, ShoesFile: shoes);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 255, 0, 0); // Leggings/robe (rojo) pisa a Shoes (verde) - orden invertido real
    }
}
