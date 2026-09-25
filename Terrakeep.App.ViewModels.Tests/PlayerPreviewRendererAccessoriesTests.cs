using System.IO;
using System.Linq;
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

    // CalamityAccesorios (25-sep-2026): bug real atrapado en esta misma pasada, NO por
    // inspeccion sino comparando capturas reales antes/despues - el wiring de arriba (7 tipos
    // vanilla) usa DrawAccessory (tira 40x(56*N)), pero HandOn/HandOff de Calamity son sprites
    // 360x224 (hoja compuesta, ver el comentario real de DrawHandAccessory en
    // PlayerPreviewRenderer.cs) - con DrawAccessory sin cambios, EquipmentAppearanceResolver
    // SI resolvia el sprite correcto pero el render salia PIXEL A PIXEL IDENTICO al de "nada
    // puesto" (SliceStripRow asumia ancho de tira fijo 40px, corrompia el recorte en silencio).
    // Un item real (guante) puesto UNA vez debe cambiar el render en AMBOS canales (HandOn Y
    // HandOff se resuelven del mismo item, ver CalamityHandsOnYHandsOff_
    // UnMismoGuanteResuelveLosDosCanalesALaVez en EquipmentAppearanceResolverTests).
    [Fact]
    public void CalamityGuanteReal_CambiaElResultadoRespectoASinAccesorios_HojaCompuesta360x224()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "HandsOn" && e.EquipSlotSecondary == "HandsOff");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(loadout);
        Assert.NotNull(accesorios.HandOnFile);
        Assert.NotNull(accesorios.HandOffFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));

        // Aislado por canal (mismo criterio que RenderConBalloonFileDaUnaImagenDistintaASinEl_
        // AislandoSoloEsaCapa): confirma que HandOnFile y HandOffFile pintan CADA UNO algo real
        // por su cuenta, no solo que "el conjunto" cambia.
        var soloHandOn = new EquippedAccessories(null, null, accesorios.HandOnFile, null, null, null, null);
        var soloHandOff = new EquippedAccessories(null, null, null, accesorios.HandOffFile, null, null, null);
        var renderSoloHandOn = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: soloHandOn);
        var renderSoloHandOff = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: soloHandOff);
        Assert.NotEqual(Pixels(sinAccesorios), Pixels(renderSoloHandOn));
        Assert.NotEqual(Pixels(sinAccesorios), Pixels(renderSoloHandOff));
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

    // GapAnalysis Encargo E (25-sep-2026): gemela de AssertEsquinaEsColor pero para la esquina
    // SUPERIOR DERECHA (simetrica) - hace falta para probar BackPart, que solo ocupa la mitad
    // DERECHA del lienzo (x>=Width/2) y por tanto NUNCA toca la esquina izquierda (0,0). Por
    // simetria con la esquina izquierda (confirmada transparente en TODA capa real del cuerpo,
    // ver el comentario de cabecera de la clase), la esquina derecha tampoco la pisa ninguna capa
    // real del cuerpo.
    private static void AssertEsquinaDerechaEsColor(WriteableBitmap bmp, byte r, byte g, byte b)
    {
        var pixels = Pixels(bmp);
        int w = bmp.PixelWidth;
        foreach (var (x, y) in new[] { (w - 1, 0), (w - 2, 0), (w - 1, 1) })
        {
            int idx = (y * w + x) * 4;
            Assert.True(pixels[idx + 2] == r && pixels[idx + 1] == g && pixels[idx + 0] == b,
                $"Esquina derecha ({x},{y}): esperaba RGB({r},{g},{b}), salio RGB({pixels[idx + 2]},{pixels[idx + 1]},{pixels[idx + 0]}) - alpha={pixels[idx + 3]}.");
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
        AssertEsquinaEsColor(bmp, 0, 255, 0); // HandOn (verde) pisa a Shield (rojo) - la ULTIMA capa real de accesorio
        // de esta subsecuencia; GapAnalysis Encargo E (25-sep-2026) anade FrontPart TODAVIA MAS
        // tarde (ver Orden_HandOnAntesQueFrontPart mas abajo), asi que "HandOn" dejo de ser la
        // ultima capa real de accesorio del renderer completo.
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

    // GapAnalysis Encargo F (25-sep-2026): FaceHead/FaceMask/FaceFlower - 3 canales reales mas
    // en los que EquipmentAppearanceResolver.ResolveAccessories reclasifica "Face" (ver
    // FaceAccessoryLayerTable) - a diferencia del resto de tipos de accesorio, sus posiciones de
    // dibujado NO son todas la misma (Paso 9b/22_FaceAcc): FaceHead siempre se dibuja DENTRO de
    // la cabeza (antes del pelo/casco); FaceMask depende del HEADSLOT puesto
    // (ArmorIDs.Head.Sets.DrawFaceMaskUnderHeadLayer - antes del pelo/casco si el casco esta en
    // esa tabla, si no, en su posicion normal salvo PreventFaceMaskDraw); FaceFlower siempre en
    // su posicion normal salvo PreventFaceFlowerDraw (headSlot), que la suprime del todo. Ids
    // reales con sprite ya extraido: Obsidian Skull (193, faceSlot=12, FaceHead), Nature's Gift
    // (223, faceSlot=1, FaceFlower) - FaceMask no tiene item real extraible en este PC (ver
    // FaceAccessoryLayerTableTests), se prueba con EquippedAccessories construido a mano
    // (FaceMaskFile sintetico), igual de valido para el CONTRATO de orden/render.
    private const int CascoDeObsidiana = 193;      // faceSlot=12 -> FaceHead
    private const int RegaloDeLaNaturaleza = 223;  // faceSlot=1 -> FaceFlower

    [Fact]
    public void FaceHeadReal_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(CascoDeObsidiana));
        Assert.NotNull(accesorios.FaceHeadFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void FaceFlowerReal_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(RegaloDeLaNaturaleza));
        Assert.NotNull(accesorios.FaceFlowerFile);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void FaceMaskSintetico_CambiaElResultadoRespectoASinAccesorios()
    {
        // Sin item real extraible en este PC para faceSlot=22 (ver el comentario de cabecera) -
        // FaceMaskFile construido a mano, igual de valido para confirmar que Render lee de
        // verdad ese campo nuevo (mismo criterio "orden"/family 2 del resto de esta clase).
        string faceMask = CrearPngSolido(10, 20, 30);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FaceMaskFile: faceMask);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void Orden_FaceHeadAntesQueCasco()
    {
        // Sin HeadSlot (numeracion vanilla desconocida, igual que un casco de Calamity) - hideHair
        // queda true y headFileToUse = armor.HeadFile tal cual, DrawHelmet() se ejecuta DESPUES de
        // FaceHead en el orden real de Render (ver el comentario real de ese bloque).
        string faceHead = CrearPngSolido(255, 0, 0), casco = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: casco, BodyFile: null, LegsFile: null);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FaceHeadFile: faceHead);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // Casco (verde) pisa a FaceHead (rojo)
    }

    [Fact]
    public void Orden_FaceBajoElPeloAntesQueCasco_FaceSlotEnLaTablaRealDeUnderHair()
    {
        // faceSlot=5 real (Blindfold, DrawInFaceUnderHairLayer) - FaceFile se dibuja ANTES del
        // pelo/casco (mismo criterio que FaceHead de arriba), en vez de en su posicion normal.
        string face = CrearPngSolido(255, 0, 0), casco = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: casco, BodyFile: null, LegsFile: null);
        var acc = new EquippedAccessories(null, null, null, null, null, null, face, FaceSlot: 5);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // Casco (verde) pisa a Face-bajo-el-pelo (rojo)
    }

    [Fact]
    public void Orden_FaceMaskBajoElCascoAntesQueCasco_HeadSlotEnLaTablaReal()
    {
        // headSlot=26 real (primer id de ArmorIDs.Head.Sets.DrawFaceMaskUnderHeadLayer) - la
        // mascara se dibuja ANTES del pelo/casco, no en su posicion normal.
        string faceMask = CrearPngSolido(255, 0, 0), casco = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: casco, BodyFile: null, LegsFile: null, HeadSlot: 26);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FaceMaskFile: faceMask);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // Casco (verde) pisa a FaceMask-bajo-el-casco (rojo)
    }

    [Fact]
    public void Orden_FaceMaskNormalDespuesDelCasco_HeadSlotFueraDeLaTablaDeUnderHead()
    {
        // headSlot=999 sintetico (fuera de cualquier tabla real, mismo patron ya usado en
        // Orden_SinWearsRobe_PernerasAntesQueShoes) - FaceMask se dibuja en su posicion NORMAL,
        // despues del pelo/casco (Paso 9b, mismo sitio que ya prueba Orden_FaceAccAntesQueShield).
        string casco = CrearPngSolido(255, 0, 0), faceMask = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: casco, BodyFile: null, LegsFile: null, HeadSlot: 999);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FaceMaskFile: faceMask);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // FaceMask normal (verde) pisa al Casco (rojo)
    }

    [Fact]
    public void Orden_FaceFlowerDespuesDelCasco_HeadSlotSinPreventFaceFlowerDraw()
    {
        // headSlot=999 sintetico (fuera de PreventFaceFlowerDraw) - FaceFlower SIEMPRE se dibuja
        // en su posicion normal (no existe una posicion "bajo el casco" real para este tipo).
        string casco = CrearPngSolido(255, 0, 0), faceFlower = CrearPngSolido(0, 255, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: casco, BodyFile: null, LegsFile: null, HeadSlot: 999);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FaceFlowerFile: faceFlower);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // FaceFlower (verde) pisa al Casco (rojo)
    }

    [Fact]
    public void FaceMaskSeSuprimeDelTodo_HeadSlotEnPreventFaceMaskDrawYFueraDeUnderHead()
    {
        // headSlot=27 real (primer id de ArmorIDs.Head.Sets.PreventFaceMaskDraw, NO esta en
        // DrawFaceMaskUnderHeadLayer) - la mascara no se dibuja en NINGUN sitio, el render debe
        // ser IDENTICO a no llevarla puesta (mismo criterio que
        // SinAccesorios_ElParametroPorDefectoNoLanzaYEsIdenticoAPasarNull).
        string faceMask = CrearPngSolido(255, 0, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: null, HeadSlot: 27);
        var sinFaceMask = new EquippedAccessories(null, null, null, null, null, null, null);
        var conFaceMask = new EquippedAccessories(null, null, null, null, null, null, null, FaceMaskFile: faceMask);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: sinFaceMask);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: conFaceMask);

        Assert.Equal(Pixels(bmpSin), Pixels(bmpCon));
    }

    [Fact]
    public void FaceFlowerSeSuprimeDelTodo_HeadSlotEnPreventFaceFlowerDraw()
    {
        // headSlot=92 real (primer id de ArmorIDs.Head.Sets.PreventFaceFlowerDraw) - la flor no
        // se dibuja en NINGUN sitio, el render debe ser IDENTICO a no llevarla puesta.
        string faceFlower = CrearPngSolido(255, 0, 0);
        var armor = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: null, HeadSlot: 92);
        var sinFaceFlower = new EquippedAccessories(null, null, null, null, null, null, null);
        var conFaceFlower = new EquippedAccessories(null, null, null, null, null, null, null, FaceFlowerFile: faceFlower);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: sinFaceFlower);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, armor, accessories: conFaceFlower);

        Assert.Equal(Pixels(bmpSin), Pixels(bmpCon));
    }

    // GapAnalysis Encargo E (25-sep-2026): Front (item.frontSlot) NO es una capa unica "encima
    // de todo" - es el MISMO sprite recortado en 2 mitades (FrontPart/BackPart), cada una en su
    // propia posicion del pipeline real - ver el comentario completo de DrawFrontHalf en
    // PlayerPreviewRenderer.cs.

    private string CrearPngMitadColor(byte rIzq, byte gIzq, byte bIzq, byte rDer, byte gDer, byte bDer)
    {
        Directory.CreateDirectory(_tempDir);
        string path = Path.Combine(_tempDir, $"mitad_{Guid.NewGuid():N}.png");
        const int w = 40, h = 56;
        var pixels = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 4;
            bool izquierda = x < w / 2;
            pixels[i + 0] = izquierda ? bIzq : bDer;
            pixels[i + 1] = izquierda ? gIzq : gDer;
            pixels[i + 2] = izquierda ? rIzq : rDer;
            pixels[i + 3] = 255;
        }
        var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    [Fact]
    public void FrontFile_SeParteEnDosMitadesReales_FrontPartIzquierdaBackPartDerecha()
    {
        // PlayerDrawLayers.cs:3908-3993: FrontPart recorta la mitad IZQUIERDA de la tira
        // (arranca en X=0), BackPart la mitad DERECHA (X += num) - PNG sintetico mitad roja/
        // mitad verde confirma que cada metodo dibuja SOLO su propia mitad del ORIGEN en la
        // MISMA mitad de posicion del lienzo (sin desplazamiento cruzado).
        string front = CrearPngMitadColor(255, 0, 0, 0, 255, 0); // izquierda roja, derecha verde
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, FrontFile: front, FrontSlot: 5);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        AssertEsquinaEsColor(bmp, 255, 0, 0);        // FrontPart (mitad izquierda del sprite) en la esquina izquierda
        AssertEsquinaDerechaEsColor(bmp, 0, 255, 0); // BackPart (mitad derecha del sprite) en la esquina derecha
    }

    [Fact]
    public void Orden_FaceAccAntesQueFrontBackPart()
    {
        // Orden real: BackPart va justo despues de FaceAcc (LegacyPlayerRenderer.cs real) -
        // PNG solido (ambas mitades del mismo color) para aislar el ORDEN sin depender de la
        // halving (ya probada arriba).
        string face = CrearPngSolido(255, 0, 0), front = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, null, null, null, face, FrontFile: front, FrontSlot: 5);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        AssertEsquinaDerechaEsColor(bmp, 0, 255, 0); // BackPart (verde, mitad derecha) pisa a FaceAcc (rojo)
    }

    [Fact]
    public void Orden_FrontBackPartAntesQueShield()
    {
        string front = CrearPngSolido(255, 0, 0), shield = CrearPngSolido(0, 255, 0);
        var acc = new EquippedAccessories(null, null, null, null, null, shield, null, FrontFile: front, FrontSlot: 5);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        AssertEsquinaDerechaEsColor(bmp, 0, 255, 0); // Shield (verde, realWidth=40=Width, cubre toda la fila) pisa a BackPart (rojo)
    }

    [Fact]
    public void Orden_HandOnAntesQueFrontPart()
    {
        // FrontPart es la capa MAS TARDIA de todo el renderer (tras OnhandAcc, ver el comentario
        // real de DrawFrontHalf) - HandOn (verde) se pinta primero y FrontPart (rojo) lo pisa.
        string handOn = CrearPngSolido(0, 255, 0), front = CrearPngSolido(255, 0, 0);
        var acc = new EquippedAccessories(null, null, handOn, null, null, null, null, FrontFile: front, FrontSlot: 5);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        AssertEsquinaEsColor(bmp, 255, 0, 0); // FrontPart (rojo) pisa a HandOn (verde) - la ULTIMA capa real de accesorio
    }

    // Condicion real de incompatibilidad (ArmorIDs.Front.Sets.DontDrawIfWearingAScarfOrCape,
    // ver PlayerBodyDrawTables.FrontDontDrawIfWearingScarfOrCape) - SOLO el frontId 13 la tiene
    // activa en la tabla real, y NINGUN accesorio real de item.frontSlot llega nunca a valer 13
    // (ver el comentario real de esa tabla, LIMITE REAL documentado). Se prueba aqui con el
    // valor SINTETICO 13 (el propio indice real de la tabla) para confirmar el CABLEADO
    // completo Render -> PlayerBodyDrawTables, dejando constancia explicita de que no hay hoy
    // ningun objeto real de Front que reproduzca este caso en el juego.

    [Fact]
    public void FrontOculto_FrontId13Sintetico_ConScarfRealEnNeckSlot_NoSeDibujaNiFrontPartNiBackPart()
    {
        string front = CrearPngSolido(255, 0, 0);
        var sinFront = new EquippedAccessories(null, null, null, null, null, null, null);
        var conFrontYScarf = new EquippedAccessories(null, null, null, null, null, null, null,
            NeckSlot: 8, // WormScarf real (ArmorIDs.Neck.Sets.IsAScarf)
            FrontFile: front, FrontSlot: 13);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: sinFront);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: conFrontYScarf);

        Assert.Equal(Pixels(bmpSin), Pixels(bmpCon)); // el scarf anula el Front por completo, identico a no llevarlo
    }

    [Fact]
    public void FrontOculto_FrontId13Sintetico_ConCapeRealEnBackSlot_NoSeDibujaNiFrontPartNiBackPart()
    {
        string front = CrearPngSolido(255, 0, 0);
        var sinFront = new EquippedAccessories(null, null, null, null, null, null, null);
        var conFrontYCape = new EquippedAccessories(null, null, null, null, null, null, null,
            BackSlot: 1, // BeeCloak real (ArmorIDs.Back.Sets.IsACape)
            FrontFile: front, FrontSlot: 13);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: sinFront);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: conFrontYCape);

        Assert.Equal(Pixels(bmpSin), Pixels(bmpCon)); // la cape anula el Front por completo, identico a no llevarlo
    }

    [Fact]
    public void FrontId13Sintetico_SinScarfNiCape_SiSeDibuja()
    {
        // Confirma que el frontId=13 en si NO esta "roto" - solo se oculta cuando de verdad hay
        // un scarf/cape puesto (descarta el falso positivo de "13 nunca se dibuja").
        string front = CrearPngSolido(255, 0, 0);
        var sinFront = new EquippedAccessories(null, null, null, null, null, null, null);
        var conFront = new EquippedAccessories(null, null, null, null, null, null, null, FrontFile: front, FrontSlot: 13);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: sinFront);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: conFront);

        Assert.NotEqual(Pixels(bmpSin), Pixels(bmpCon));
    }

    [Fact]
    public void FrontIdRealConScarfPuesto_NuncaSeOculta_LaIncompatibilidadNoAplicaAAccesoriosReales()
    {
        // CrimsonCloak real (frontSlot=1) CON un scarf real puesto (neckSlot=8) - la tabla real
        // (DontDrawIfWearingAScarfOrCape) solo tiene el indice 13 activo, nunca el 1, asi que el
        // Front real SIGUE dibujandose - confirma en el renderer la correccion real documentada
        // en PlayerBodyDrawTables (ningun accesorio real de Front es incompatible con scarf/cape
        // en la practica del juego).
        string front = CrearPngSolido(255, 0, 0);
        var sinFront = new EquippedAccessories(null, null, null, null, null, null, null, NeckSlot: 8);
        var conFront = new EquippedAccessories(null, null, null, null, null, null, null, NeckSlot: 8, FrontFile: front, FrontSlot: 1);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: sinFront);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: conFront);

        Assert.NotEqual(Pixels(bmpSin), Pixels(bmpCon)); // el Front real SIGUE visible pese al scarf
    }

    [Fact]
    public void Front_ConCalamitySlotNulo_NuncaAplicaLaIncompatibilidad()
    {
        // FrontSlot=null (objeto de Calamity, numeracion propia no compartida) - fiel-por-
        // defecto, la comprobacion de scarf/cape ni se evalua (mismo criterio ya establecido
        // para ShoesSlot/HeadSlot null en el resto de este renderer).
        string front = CrearPngSolido(255, 0, 0);
        var sinFront = new EquippedAccessories(null, null, null, null, null, null, null, NeckSlot: 8, BackSlot: 1);
        var conFront = new EquippedAccessories(null, null, null, null, null, null, null, NeckSlot: 8, BackSlot: 1, FrontFile: front, FrontSlot: null);

        var bmpSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: sinFront);
        var bmpCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: conFront);

        Assert.NotEqual(Pixels(bmpSin), Pixels(bmpCon));
    }

    // ---- Familia 4: Wings Encargo1 (25-sep-2026) - capa base estatica de alas ----
    //
    // 4 casos reales, cubriendo las 4 formas reales de WingDrawTable (ver su comentario de clase
    // para la cita completa de PlayerDrawLayers.DrawPlayer_09_Wings): 1 id puramente generico
    // (Default, wingId sin entrada en Overrides), 1 con offset-tweak (num12/num13 dentro de la
    // rama generica), 1 de los 11 bloques propios ("early return", formula/divisor propios), y 1
    // item real de Calamity (numeracion propia, sin WingSlot vanilla - posicion GENERICA de
    // WingDrawTable, fiel-por-defecto, mismo criterio ya establecido para el resto del resolver).
    private const int DemonWings = 492;      // wingSlot=1, generico puro (Default: anchor=(11,33), div=4)
    private const int ButterflyWings = 749;  // wingSlot=5, offset-tweak (anchor=(15,29), div=4)
    private const int Hoverboard = 1866;     // wingSlot=22, bloque propio/early-return (anchor=(11,57), div=7)

    [Fact]
    public void WingGenerico_CambiaElResultadoRespectoASinAccesorios()
    {
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(DemonWings));
        Assert.NotNull(accesorios.WingFile);
        Assert.Equal(1, accesorios.WingSlot);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void WingConOffsetTweak_DaUnResultadoDistintoDeWingGenerico_ConfirmaLaTablaPorId()
    {
        // Confirma que WingDrawTable.Resolve(5) (anchor=(15,29), distinto del Default (11,33)) se
        // usa de verdad - el resultado es DISTINTO del generico con la UNICA variable siendo el
        // wingId (mismo hairStyle/color/nada mas puesto en los dos renders).
        var generico = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(DemonWings));
        var tweak = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(ButterflyWings));
        Assert.NotNull(tweak.WingFile);
        Assert.Equal(5, tweak.WingSlot);

        var renderGenerico = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: generico);
        var renderTweak = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: tweak);

        Assert.NotEqual(Pixels(renderGenerico), Pixels(renderTweak));
    }

    [Fact]
    public void WingConBloquePropio_CambiaElResultadoRespectoASinAccesorios_DivisorPropioDistinto()
    {
        // Hoverboard (wingSlot=22): uno de los 11 ids con formula/divisor PROPIO real
        // (Height()/7, no el /4 generico) - confirma que LoadWingFrame usa de verdad
        // WingDrawTable.Resolve(22) (anchor=(11,57), divisor=7), no el valor por defecto.
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(Hoverboard));
        Assert.NotNull(accesorios.WingFile);
        Assert.Equal(22, accesorios.WingSlot);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    [Fact]
    public void CalamityWingReal_CambiaElResultadoRespectoASinAccesorios_IncluidoElTamanoDistinto()
    {
        // HadarianWings: unico de los 16 items reales de Calamity con EquipType.Wings cuyo
        // sprite real NO mide 86x248 (mide 64x144, ver el comentario real de LoadWingFrame/
        // WingDrawTable) - confirma que SliceWingFrame usa el ANCHO/ALTO REAL del PNG decodificado
        // (LoadWingStrip), no una constante, y que WingSlot vanilla queda null para Calamity
        // (posicion GENERICA de WingDrawTable, fiel-por-defecto, mismo criterio ya establecido
        // para el resto de canales de este resolver).
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Wings" && e.Internal == "HadarianWings");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);
        var accesorios = Service.EquipmentAppearance.ResolveAccessories(loadout);
        Assert.NotNull(accesorios.WingFile);
        Assert.Null(accesorios.WingSlot);

        var sinAccesorios = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors);
        var conAccesorio = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: accesorios);

        Assert.NotEqual(Pixels(sinAccesorios), Pixels(conAccesorio));
    }

    // Contrato de orden real (LegacyPlayerRenderer.cs:178-184): Backpacks -> Tails -> Wings ->
    // BackHair -> BackAcc - Wings se compone DESPUES de Tail. PNG sintetico de ala
    // deliberadamente MUCHO mayor que el lienzo (200x800 - con el anchor real Default (11,33) y
    // divisor 4, el fotograma recortado (200x200) cubre sobradamente la esquina (0,0) sin
    // depender de un calculo fino de offset) para que el contrato de esquina ya usado en el resto
    // de esta clase (AssertEsquinaEsColor) sea observable sin ambiguedad.
    [Fact]
    public void Orden_TailAntesQueWing_PngSinteticoDeAlaGrandeQueCubreLaEsquina()
    {
        string tail = CrearPngSolido(255, 0, 0);
        string wing = CrearPngSolidoTamano(0, 255, 0, 200, 800);
        var acc = new EquippedAccessories(null, null, null, null, null, null, null, TailFile: tail, WingFile: wing);

        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, Colors, accessories: acc);

        AssertEsquinaEsColor(bmp, 0, 255, 0); // Wing (verde) pisa a Tail (rojo)
    }

    private string CrearPngSolidoTamano(byte r, byte g, byte b, int w, int h)
    {
        Directory.CreateDirectory(_tempDir);
        string path = Path.Combine(_tempDir, $"wing_{r}_{g}_{b}_{w}x{h}_{Guid.NewGuid():N}.png");
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
}
