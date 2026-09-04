namespace TerrasavrNative.Core.WldFormat;

// "Tile entity" real de un .wld: maniqui, marco de objeto, perchero de sombreros, bandeja de
// comida, frasco expositor de Dead Cells, ancla de mascota/cometa, dummy de entrenamiento o
// sensor logico - cada uno con un formato binario POLIMORFICO por tipo (switch en el propio
// byte Type). Formato confirmado directamente contra TileEntity.cs de TEdit
// (github.com/TEdit/Terraria-Map-Editor, main, descargado y leido campo a campo esta misma
// sesion - Load en la linea 377, LoadStack/LoadHatRack/LoadDisplayDoll) - deliberadamente
// diferido en la Fase 2 del buscador porque el advisor original NO llego a verificar este
// formato (ver el comentario de WldReader.Read), ahora ya si.
//
// Kind = el mismo byte Type real del archivo (TileEntityType de TEdit) - no se reordena.
public enum WldTileEntityKind : byte
{
    TrainingDummy = 0,
    ItemFrame = 1,
    LogicSensor = 2,
    DisplayDoll = 3,
    WeaponRack = 4,
    HatRack = 5,
    FoodPlatter = 6,
    TeleportationPylon = 7,
    DeadCellsDisplayJar = 8,
    KiteAnchor = 9,
    CritterAnchor = 10,
}

// Un objeto real expuesto/colgado dentro de una tile entity (NetId = mismo id que
// VanillaItemCatalog/CalamityCatalog, igual criterio que WldChestItem). CritterAnchor/
// KiteAnchor no tienen stack/prefijo real en el archivo (una unica criatura/cometa atada por
// ancla) - se guardan aqui igualmente como Stack=1/Prefix=0 sinteticos para poder buscarlos
// exactamente igual que cualquier otro objeto, sin un caso especial rio abajo.
public readonly record struct WldTileEntityItem(int NetId, short Stack, byte Prefix);

public sealed class WldTileEntity
{
    public required WldTileEntityKind Kind { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    // Todos los slots con contenido real (Items+Dyes+Misc de un maniqui, Items+Dyes de un
    // perchero, el unico slot de un marco/estante/bandeja/frasco, la criatura/cometa de un
    // ancla) aplanados en una unica lista - para busqueda por objeto no hace falta distinguir
    // "item" de "tinte", igual que WldChest.Items no distingue por slot.
    public required IReadOnlyList<WldTileEntityItem> Items { get; init; }
}
