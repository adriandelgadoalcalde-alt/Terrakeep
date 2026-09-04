namespace TerrasavrNative.Core.WldFormat;

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
}
