namespace TerrasavrNative.Core.WldFormat;

// Cabecera minima de un .wld - solo hasta donde hace falta para ubicar la seccion de tiles
// (pointers[1]) y NPCs (pointers[4]) y dimensionar el mundo. Formato confirmado leyendo el
// lector real ya construido en Terrasavr-Calamity-Beta (overrides.js, parseWorldHeader) -
// mismo criterio que TEdit real (WorldFile.LoadFileFormatHeader), no adivinado.
//
// SIMPLIFICACION DELIBERADA (heredada del propio lector JS - "Stops reading the header as
// soon as it has what it needs" en su propio comentario): no se parsean los campos de
// GameMode/CreationTime/LastPlayed/MoonType/arboles-cuevas/Spawn/GroundLevel/RockLevel - todos
// version-dependientes y no imprescindibles para pintar el mapa. Se salta directamente a
// pointers[1] tras leer las dimensiones. Si algun dia hace falta el punto de aparicion o los
// niveles de tierra/roca para el visor, hay que sumar ese tramo (ver el informe del formato en
// bitacora.md antes de intentarlo de memoria).
public sealed class WldHeader
{
    public required uint Version { get; init; }
    public required int[] Pointers { get; init; }
    public required bool[] TileFrameImportant { get; init; }
    public required string Title { get; init; }
    public required int WorldId { get; init; }
    public required int TilesHigh { get; init; }
    public required int TilesWide { get; init; }

    public int TilesSectionOffset => Pointers[1];
    public int NpcsSectionOffset => Pointers[4];
}
