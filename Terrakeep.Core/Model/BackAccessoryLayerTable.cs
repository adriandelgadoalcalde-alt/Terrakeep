namespace Terrakeep.Core.Model;

// GapAnalysis Encargo A (25-sep-2026): Terraria real clasifica el backSlot de un item en 3
// canales de dibujado posibles, no solo "Back" (Terraria/Player.cs decompilado real,
// UpdateVisibleAccessory, Player.cs:37169-37184):
//
//   if (item.backSlot > 0)
//   {
//       if (ArmorIDs.Back.Sets.DrawInBackpackLayer[item.backSlot]) backpack = item.backSlot;
//       else if (ArmorIDs.Back.Sets.DrawInTailLayer[item.backSlot]) tail = item.backSlot;
//       else { back = item.backSlot; front = -1; }
//   }
//
// Tabla real transcrita LITERAL de Terraria/ID/ArmorIDs.cs (decompilado real,
// Downloads\tModLoader-Decompiled\TerrariaVanilla\), clase Back.Sets:
//
//   ArmorIDs.cs:1717 - DrawInBackpackLayer = Factory.CreateBoolSet(false, 7, 8, 9, 10, 15, 16, 32, 33);
//     7=MagicQuiver, 8=ArchitectGizmoPack, 9=HivePack, 10=AnglerTackleBag, 15=MoltenQuiver,
//     16=StalkersQuiver, 32=FloretProtecterChestplate, 33=LavaproofTackleBag.
//   ArmorIDs.cs:1719 - DrawInTailLayer = Factory.CreateBoolSet(false, 18, 19, 21, 25, 26, 27, 28);
//     18=SpaceCreatureShirt, 19=FoxShirt, 21=CatShirt, 25=DogTail, 26=FoxTail, 27=LizardTail,
//     28=BunnyTail.
//
// Los 3 canales comparten la MISMA textura real TextureAssets.AccBack (PlayerDrawLayers.cs:484
// Backpack, :584 Tail, :~600+ BackAcc normal - EquipmentAppearanceResolver ya extrae esos
// sprites a Assets/player/acc_back/{backSlot}.png para "Back", asi que Backpack/Tail solo
// reclasifican el mismo sprite ya resuelto, sin extraccion nueva).
public static class BackAccessoryLayerTable
{
    // ArmorIDs.cs:1717, valores reales EXACTOS (no inventados).
    private static readonly HashSet<int> BackpackBackSlots = [7, 8, 9, 10, 15, 16, 32, 33];

    // ArmorIDs.cs:1719, valores reales EXACTOS (no inventados).
    private static readonly HashSet<int> TailBackSlots = [18, 19, 21, 25, 26, 27, 28];

    public static bool IsBackpackLayer(int backSlot) => BackpackBackSlots.Contains(backSlot);

    public static bool IsTailLayer(int backSlot) => TailBackSlots.Contains(backSlot);
}
