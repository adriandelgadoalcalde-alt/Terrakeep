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
    // F-14 (auditoria de Opus vs TEdit, E-16): "semilla, modo de juego... estan a un puñado de
    // Read* de distancia, y hoy Terrakeep no enseña ninguno" - coste 0, ya se leian y se
    // descartaban (ver el comentario real de WldReader.cs sobre la codificacion exacta por
    // version). GameMode: 0=Clasico, 1=Experto, 2=Maestro, 3=Viaje (segun la version real).
    public required string Seed { get; init; }
    public required int GameMode { get; init; }
    // F-7 (auditoria de Opus vs TEdit, E-06): "DungeonX/Y ni siquiera se leen... en el formato
    // real la mazmorra esta solo cinco campos mas alla de RockLevel". Confirmado leyendo el
    // lector real de TEdit (World.FileV2.cs, commit f592261): Time(double)/DayTime(bool)/
    // MoonPhase(int)/BloodMoon(bool)/IsEclipse(bool) - sin guarda de version, igual que
    // GroundLevel/RockLevel de arriba - antes de DungeonX/Y. Los 5 campos intermedios se leen y
    // se descartan (misma politica ya documentada arriba: lo que no se necesita, no se parsea).
    public required int DungeonX { get; init; }
    public required int DungeonY { get; init; }

    public int TilesSectionOffset => Pointers[1];
    // Punto 4 (advisor Opus), Fase 2: confirmado directamente contra World.FileV2.cs de TEdit
    // (LoadWorld real) - Pointers[N] es donde EMPIEZA la seccion N (= donde termina la anterior),
    // mismo criterio ya usado por TilesSectionOffset/NpcsSectionOffset.
    public int ChestsSectionOffset => Pointers[2];
    public int SignsSectionOffset => Pointers[3];
    public int NpcsSectionOffset => Pointers[4];
    // Confirmado contra World.FileV2.cs de TEdit (LoadWorld real, lineas 1452-1474): tras NPCs
    // (fin de seccion = sectionPointers[5]) viene Tile Entities, mismo criterio "Pointers[N] es
    // donde EMPIEZA la seccion N" ya usado arriba. -1 si el mundo es tan antiguo que ni siquiera
    // tiene este puntero (pointerCount<=5) - version<116, nunca visto en un mundo real de esta
    // maquina pero mejor no indexar fuera de rango si aparece uno.
    public int? TileEntitiesSectionOffset => Pointers.Length > 5 ? Pointers[5] : null;

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

    // Pedido explicito del usuario (5-sep-2026): editar la dificultad del mundo (WldWriter.
    // PatchGameMode ya la escribe en el ARCHIVO real) - tras un guardado con exito, el WldWorld
    // en memoria tiene que reflejar el nuevo valor sin releer el mundo entero (releer un mundo
    // Grande cuesta ~1.4s reales y reiniciaria zoom/busqueda/filtros de Exploracion sin
    // necesidad, el unico campo que cambio de verdad es este). Todas las demas propiedades son
    // `init`-only a proposito (WldHeader/WldWorld son inmutables salvo por este unico camino
    // explicito) - copiadas tal cual, nunca recalculadas.
    public WldHeader WithGameMode(int newGameMode) => new()
    {
        Version = Version, Pointers = Pointers, TileFrameImportant = TileFrameImportant, Title = Title,
        WorldId = WorldId, TilesHigh = TilesHigh, TilesWide = TilesWide, SpawnX = SpawnX, SpawnY = SpawnY,
        GroundLevel = GroundLevel, RockLevel = RockLevel, Seed = Seed, GameMode = newGameMode,
        DungeonX = DungeonX, DungeonY = DungeonY,
    };
}
