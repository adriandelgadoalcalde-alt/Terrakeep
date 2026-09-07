namespace Terrakeep.Core.WldFormat;

public sealed class WldNpc
{
    public required int Id { get; init; }
    public required string GivenName { get; init; }
    public required int TileX { get; init; }
    public required int TileY { get; init; }
    public required bool Homeless { get; init; }
    // H6-08/H6-09 (sexta auditoria de Opus): townNpcVariationIndex real (0 si no presente en
    // el fichero, mismo valor por defecto que Terraria real le da a un NPC sin variacion) -
    // usado por NpcHeadProfile para elegir la cabeza real de Gato/Perro/Conejo de pueblo.
    public required int VariationIndex { get; init; }
}
