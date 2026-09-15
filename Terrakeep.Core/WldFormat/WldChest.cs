namespace Terrakeep.Core.WldFormat;

// Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
// ESPEC-buscador-mundo-tedit.md: un objeto real dentro de un cofre. NetId es el id vanilla o
// (si aplica) el synthetic id de Calamity - igual que en cualquier otro slot del proyecto, no
// se resuelve el nombre aqui (eso es cosa de VanillaItemCatalog/CalamityCatalog).
public readonly record struct WldChestItem(int NetId, short Stack, byte Prefix);

// Formato real confirmado directamente contra World.FileV2.cs:1770-1806 de TEdit
// (github.com/TEdit/Terraria-Map-Editor, main, descargado y leido esta misma sesion - no de
// memoria del espec del advisor): version < 294 usa un `maxItems` GLOBAL (Int16, una sola vez
// para todos los cofres); version >= 294 usa un `MaxItems` PROPIO por cofre (Int32) - los
// cofres pueden tener capacidades distintas (ej. Void Vault de Calamity).
public sealed class WldChest
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<WldChestItem> Items { get; init; }

    // Editor de cofres v1 (T1 del documento I+D, 15-sep-2026): capacidad REAL de este cofre
    // concreto (version >= 294) o el global compartido del archivo (version < 294) - lo lee
    // WldReader.ReadChests, WldWriter.WriteChestItems lo reutiliza para saber cuantos huecos
    // tiene de verdad sin tener que volver a adivinarlo. No `required` a proposito: los tests
    // existentes construyen WldChest a mano para probar solo lectura/busqueda, sin este dato -
    // 40 es la capacidad real de un cofre de madera vanilla, el valor mas comun con diferencia.
    public int MaxItems { get; init; } = 40;
}
