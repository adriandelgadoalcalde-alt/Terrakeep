using Terrakeep.Core.Model;
using Xunit;

namespace Terrakeep.Core.Tests.Model;

// GapAnalysis Encargo F (25-sep-2026) - ArmorIDs.Face.Sets, ArmorIDs.cs:2184-2190, indexadas por
// FACESLOT (no por headSlot - ver PlayerBodyDrawTablesTests para las 3 tablas indexadas por
// headSlot que tambien afectan a FaceMask/FaceFlower). Ver FaceAccessoryLayerTable.cs para la
// cita real completa y los ids de item vanilla ya confirmados end-to-end en
// EquipmentAppearanceResolverTests (193/223/888/4409).
//
// faceSlot=22 (WeldingMask, DrawInFaceMaskLayer, el UNICO valor real de esa tabla) no tiene
// ningun item real extraible todavia en este PC (arbol decompilado 1.4.4.9 usado por
// scripts/extraer-slots-accesorios-vanilla.py - confirmado ausente en Item.cs de ese arbol, SI
// presente en el arbol vanilla 1.4.5.8 mas nuevo, ver el comentario de cabecera de
// FaceAccessoryLayerTable.cs) - por eso esta clase prueba la TABLA en si (el valor 22 es real,
// transcrito literal del decompilado, no inventado) en vez de depender de un objeto real de
// punta a punta como el resto de canales.
public class FaceAccessoryLayerTableTests
{
    // ---- IsFaceHeadLayer (ArmorIDs.cs:2190 - "Factory.CreateBoolSet(false, 12, 10, 13, 11)") ----

    [Theory]
    [InlineData(12)] // ObsidianSkull, item real 193
    [InlineData(10)] // LavaSkull, item real 3999
    [InlineData(13)] // ObsidianSkullRose, item real 4004
    [InlineData(11)] // MoltenSkullRose, item real 4003
    public void IsFaceHeadLayer_Los4IdsRealesDeLaTabla(int faceSlot)
    {
        Assert.True(FaceAccessoryLayerTable.IsFaceHeadLayer(faceSlot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]  // NaturesGift, esta en FaceFlower, no en FaceHead
    [InlineData(22)] // WeldingMask, esta en FaceMask, no en FaceHead
    public void IsFaceHeadLayer_FaceSlotSinEntradaReal_DevuelveFalse(int faceSlot)
    {
        Assert.False(FaceAccessoryLayerTable.IsFaceHeadLayer(faceSlot));
    }

    // ---- IsFaceMaskLayer (ArmorIDs.cs:2186 - "Factory.CreateBoolSet(false, 22)") ----

    [Fact]
    public void IsFaceMaskLayer_WeldingMask_LaUnicaEntradaRealDeLaTabla()
    {
        // faceSlot=22, item real 5596 (WeldingMask) - sin sprite extraible en este PC todavia
        // (ver el comentario de cabecera), pero la tabla en si es real y transcrita literal.
        Assert.True(FaceAccessoryLayerTable.IsFaceMaskLayer(22));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)] // ObsidianSkull, esta en FaceHead, no en FaceMask
    [InlineData(1)]  // NaturesGift, esta en FaceFlower, no en FaceMask
    public void IsFaceMaskLayer_FaceSlotSinEntradaReal_DevuelveFalse(int faceSlot)
    {
        Assert.False(FaceAccessoryLayerTable.IsFaceMaskLayer(faceSlot));
    }

    // ---- IsFaceFlowerLayer (ArmorIDs.cs:2188 - "Factory.CreateBoolSet(false, 1, 6, 9, 8)") ----

    [Theory]
    [InlineData(1)] // NaturesGift, item real 223
    [InlineData(6)] // ObsidianRose, item real 1323
    [InlineData(9)] // ArcaneFlower, item real 3991
    [InlineData(8)] // JungleRose, item real 208
    public void IsFaceFlowerLayer_Los4IdsRealesDeLaTabla(int faceSlot)
    {
        Assert.True(FaceAccessoryLayerTable.IsFaceFlowerLayer(faceSlot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)] // ObsidianSkull, esta en FaceHead, no en FaceFlower
    [InlineData(5)]  // Blindfold, esta en DrawInFaceUnderHairLayer, no en FaceFlower
    public void IsFaceFlowerLayer_FaceSlotSinEntradaReal_DevuelveFalse(int faceSlot)
    {
        Assert.False(FaceAccessoryLayerTable.IsFaceFlowerLayer(faceSlot));
    }

    // ---- IsUnderHairLayer (ArmorIDs.cs:2184 - "Factory.CreateBoolSet(false, 5)") ----

    [Fact]
    public void IsUnderHairLayer_Blindfold_LaUnicaEntradaRealDeLaTabla()
    {
        // faceSlot=5, item real 888 (Blindfold) - confirmado end-to-end en
        // EquipmentAppearanceResolverTests (se queda en el canal "Face" normal, solo cambia de
        // POSICION de dibujado en PlayerPreviewRenderer.Render).
        Assert.True(FaceAccessoryLayerTable.IsUnderHairLayer(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)] // SpectreGoggles, item real 4409 - Face normal, sin ninguna excepcion real
    [InlineData(12)] // ObsidianSkull, esta en FaceHead, no en UnderHair
    public void IsUnderHairLayer_FaceSlotSinEntradaReal_DevuelveFalse(int faceSlot)
    {
        Assert.False(FaceAccessoryLayerTable.IsUnderHairLayer(faceSlot));
    }
}
