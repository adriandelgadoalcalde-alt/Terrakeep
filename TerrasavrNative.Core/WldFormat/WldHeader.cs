namespace TerrasavrNative.Core.WldFormat;

// Cabecera de un .wld - hasta GroundLevel/RockLevel/Spawn inclusive (lo que hace falta para
// ubicar la seccion de tiles/pointers[1]/NPCs/pointers[4], dimensionar el mundo y pintar el
// fondo degradado por zona). Formato confirmado leyendo el lector real ya construido en
// Terrasavr-Calamity-Beta (overrides.js, parseWorldHeader) - mismo criterio que TEdit real
// (WorldFile.LoadFileFormatHeader), no adivinado.
//
// SIMPLIFICACION DELIBERADA (heredada del propio lector JS - "Stops reading the header as
// soon as it has what it needs" en su propio comentario): no se parsean los campos DESPUES de
// GroundLevel/RockLevel (bandera de mundo obtenido/orbe roto, tipos de arbol por bioma...) -
// no hacen falta ni para el mapa ni para el fondo por zona.
public sealed class WldHeader
{
    public required uint Version { get; init; }
    public required int[] Pointers { get; init; }
    public required bool[] TileFrameImportant { get; init; }
    public required string Title { get; init; }
    public required int WorldId { get; init; }
    public required int TilesHigh { get; init; }
    public required int TilesWide { get; init; }
    public required int SpawnX { get; init; }
    public required int SpawnY { get; init; }
    public required double GroundLevel { get; init; }
    public required double RockLevel { get; init; }

    public int TilesSectionOffset => Pointers[1];
    // Punto 4 (advisor Opus), Fase 2: confirmado directamente contra World.FileV2.cs de TEdit
    // (LoadWorld real) - Pointers[N] es donde EMPIEZA la seccion N (= donde termina la anterior),
    // mismo criterio ya usado por TilesSectionOffset/NpcsSectionOffset.
    public int ChestsSectionOffset => Pointers[2];
    public int SignsSectionOffset => Pointers[3];
    public int NpcsSectionOffset => Pointers[4];

    // Zona por profundidad (fila de tile, no pixel) - mismo criterio que el visor JS real
    // (zoneFor en overrides.js): Espacio por encima de y=80, Infierno en las ultimas 192 filas,
    // Roca/Tierra segun RockLevel/GroundLevel, Cielo el resto. Nombres = claves reales de
    // map_colors.json ("global").
    public string ZoneFor(int worldY)
    {
        if (worldY < 80) return "Space";
        if (worldY > TilesHigh - 192) return "Hell";
        if (worldY > RockLevel) return "Rock";
        if (worldY > GroundLevel) return "Earth";
        return "Sky";
    }
}
