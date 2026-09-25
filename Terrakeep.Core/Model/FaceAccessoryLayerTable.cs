namespace Terrakeep.Core.Model;

// GapAnalysis Encargo F (25-sep-2026): item.faceSlot se clasifica en 4 canales reales
// (Terraria/Player.cs decompilado real, UpdateVisibleAccessory, Player.cs:37213-37231):
//
//   if (item.faceSlot > 0)
//   {
//       if (ArmorIDs.Face.Sets.DrawInFaceHeadLayer[item.faceSlot])
//           faceHead = item.faceSlot;
//       else if (ArmorIDs.Face.Sets.DrawInFaceMaskLayer[item.faceSlot])
//           faceMask = item.faceSlot;
//       else if (ArmorIDs.Face.Sets.DrawInFaceFlowerLayer[item.faceSlot])
//           faceFlower = item.faceSlot;
//       else
//           face = item.faceSlot;
//   }
//
// Tablas reales transcritas LITERAL de Terraria/ID/ArmorIDs.cs (decompilado real,
// Downloads\tModLoader-Decompiled\TerrariaVanilla\), clase Face.Sets (ArmorIDs.cs:2174-2195):
//
//   ArmorIDs.cs:2184 - DrawInFaceUnderHairLayer = Factory.CreateBoolSet(false, 5);
//   ArmorIDs.cs:2186 - DrawInFaceMaskLayer = Factory.CreateBoolSet(false, 22);
//   ArmorIDs.cs:2188 - DrawInFaceFlowerLayer = Factory.CreateBoolSet(false, 1, 6, 9, 8);
//   ArmorIDs.cs:2190 - DrawInFaceHeadLayer = Factory.CreateBoolSet(false, 12, 10, 13, 11);
//
// Confirmado con ids reales de Item.cs (tModLoader decompilado, 1.4.4.9, el arbol real que usa
// scripts/extraer-slots-accesorios-vanilla.py): item 193 (Obsidian Skull, faceSlot=12 ->
// FaceHead), item 223 (Nature's Gift, faceSlot=1 -> FaceFlower), item 888 (Blindfold,
// faceSlot=5 -> DrawInFaceUnderHairLayer, NO reclasificado aqui - ver el comentario real de
// DrawInFaceUnderHairLayer en PlayerPreviewRenderer.Render, se queda en el canal "Face" normal
// pero cambia de POSICION de dibujado), item 4409 (Spectre Goggles, faceSlot=14 -> ninguna
// tabla, canal "Face" normal sin excepciones).
//
// faceSlot=22 (WeldingMask, item 5596) es el UNICO valor real de DrawInFaceMaskLayer, pero ese
// item concreto no existe todavia en el arbol decompilado real de este PC (1.4.4.9, confirmado
// ausente en tModLoader-Decompiled\tModLoader\Terraria\Item.cs - SI existe en el arbol vanilla
// 1.4.5.8 mas nuevo, Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria\Player.cs:9779/
// Item.cs:44032) - mismo "1-2%" de ids reales sin sprite extraible de esta instalacion ya
// documentado en el resto de la clase (Resolve/ResolveAccessorySprite), la TABLA es real e
// igual de fiel aunque hoy no haya un objeto concreto que la ejercite de extremo a extremo en
// este PC.
public static class FaceAccessoryLayerTable
{
    private static readonly HashSet<int> FaceHeadSlots = [12, 10, 13, 11];
    private static readonly HashSet<int> FaceMaskSlots = [22];
    private static readonly HashSet<int> FaceFlowerSlots = [1, 6, 9, 8];
    private static readonly HashSet<int> UnderHairSlots = [5];

    public static bool IsFaceHeadLayer(int faceSlot) => FaceHeadSlots.Contains(faceSlot);
    public static bool IsFaceMaskLayer(int faceSlot) => FaceMaskSlots.Contains(faceSlot);
    public static bool IsFaceFlowerLayer(int faceSlot) => FaceFlowerSlots.Contains(faceSlot);
    public static bool IsUnderHairLayer(int faceSlot) => UnderHairSlots.Contains(faceSlot);
}
