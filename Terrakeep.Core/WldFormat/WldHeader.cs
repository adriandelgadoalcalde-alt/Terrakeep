namespace Terrakeep.Core.WldFormat;

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

    // Editor de mundos v1 (14-sep-2026, siguiendo la guia de bitacora.md del 13-sep-2026 -
    // "spawn point, hora/estacion guardada, banderas de progreso del mundo"): estos campos ya
    // se leian y se descartaban (Time/DayTime/MoonPhase/BloodMoon/IsEclipse, ver el comentario
    // real de WldReader.ReadHeader) o ni siquiera se llegaba a ellos (el bloque de banderas de
    // IsCrimson..HardMode, confirmado byte a byte contra World.FileV2.cs de TEdit, commit
    // f592261, lineas 2092-2119 - TODO el tramo desde SpawnX hasta HardMode es de ancho FIJO,
    // sin ningun string variable de por medio, asi que es seguro seguir leyendolo Y parchearlo
    // despues por offset replicado, igual que ya hace WldWriter.PatchGameMode).
    //
    // Los jefes tardios (Fishron, Martianos, Culto Lunatico, Lunatico) quedan FUERA a proposito:
    // estan despues de la lista de Anglers (string[], longitud variable) y de LoadBanners - su
    // offset no es fijo, localizarlo exige atravesar mas secciones variables y el riesgo de
    // desincronizar la escritura sube sin necesidad para esta primera version.
    public required double Time { get; init; }
    public required bool DayTime { get; init; }
    public required int MoonPhase { get; init; }
    public required bool BloodMoon { get; init; }
    public required bool IsEclipse { get; init; }
    // Bioma de mal real del mundo (Corrupcion si es false) - se ENSEÑA (parte de "que banderas
    // tiene este mundo") pero no se deja editar: cambiar el booleano sin tocar ni un tile dejaria
    // el dato mintiendo sobre lo que el mapa realmente muestra, algo que este proyecto evita a
    // proposito en cualquier otro sitio (ver CLAUDE.md).
    public required bool IsCrimson { get; init; }
    public required bool DownedBoss1EyeOfCthulhu { get; init; }
    public required bool DownedBoss2EaterOfWorldsOrBrainOfCthulhu { get; init; }
    public required bool DownedBoss3Skeletron { get; init; }
    public required bool DownedQueenBee { get; init; }
    public required bool DownedMechBoss1TheDestroyer { get; init; }
    public required bool DownedMechBoss2TheTwins { get; init; }
    public required bool DownedMechBoss3SkeletronPrime { get; init; }
    public required bool DownedPlantBoss { get; init; }
    public required bool DownedGolemBoss { get; init; }
    // Solo existe desde la version 118 del formato (World.FileV2.cs: `if (w.Version >= 118)`) -
    // null en un mundo mas antiguo en vez de fingir un false que el archivo ni siquiera guarda.
    public bool? DownedSlimeKingBoss { get; init; }
    public required bool HardMode { get; init; }

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
    public WldHeader WithGameMode(int newGameMode) => CopyWith(gameMode: newGameMode);

    // Editor de mundos v1 (14-sep-2026): mismos tres caminos explicitos que WithGameMode, uno
    // por grupo de campos editable (Spawn / Tiempo-luna / Banderas de progreso) - cada uno
    // corresponde 1:1 a su propio WldWriter.PatchXxx y su propio boton "Guardar" en la interfaz
    // (nunca un unico "guardar todo" que mezclaria varias escrituras atomicas distintas en una).
    public WldHeader WithSpawn(int newSpawnX, int newSpawnY) => CopyWith(spawnX: newSpawnX, spawnY: newSpawnY);

    public WldHeader WithTimeAndMoon(double newTime, bool newDayTime, int newMoonPhase, bool newBloodMoon, bool newIsEclipse) =>
        CopyWith(time: newTime, dayTime: newDayTime, moonPhase: newMoonPhase, bloodMoon: newBloodMoon, isEclipse: newIsEclipse);

    public WldHeader WithBossFlags(
        bool newDownedBoss1EyeOfCthulhu, bool newDownedBoss2EaterOfWorldsOrBrainOfCthulhu, bool newDownedBoss3Skeletron,
        bool newDownedQueenBee, bool newDownedMechBoss1TheDestroyer, bool newDownedMechBoss2TheTwins,
        bool newDownedMechBoss3SkeletronPrime, bool newDownedPlantBoss, bool newDownedGolemBoss,
        bool? newDownedSlimeKingBoss, bool newHardMode) => CopyWith(
            downedBoss1: newDownedBoss1EyeOfCthulhu, downedBoss2: newDownedBoss2EaterOfWorldsOrBrainOfCthulhu,
            downedBoss3: newDownedBoss3Skeletron, downedQueenBee: newDownedQueenBee, downedMech1: newDownedMechBoss1TheDestroyer,
            downedMech2: newDownedMechBoss2TheTwins, downedMech3: newDownedMechBoss3SkeletronPrime,
            downedPlant: newDownedPlantBoss, downedGolem: newDownedGolemBoss, downedSlimeKing: newDownedSlimeKingBoss,
            hardMode: newHardMode);

    private WldHeader CopyWith(
        int? gameMode = null, int? spawnX = null, int? spawnY = null,
        double? time = null, bool? dayTime = null, int? moonPhase = null, bool? bloodMoon = null, bool? isEclipse = null,
        bool? downedBoss1 = null, bool? downedBoss2 = null, bool? downedBoss3 = null, bool? downedQueenBee = null,
        bool? downedMech1 = null, bool? downedMech2 = null, bool? downedMech3 = null, bool? downedPlant = null,
        bool? downedGolem = null, bool? downedSlimeKing = null, bool? hardMode = null) => new()
    {
        Version = Version, Pointers = Pointers, TileFrameImportant = TileFrameImportant, Title = Title,
        WorldId = WorldId, TilesHigh = TilesHigh, TilesWide = TilesWide,
        SpawnX = spawnX ?? SpawnX, SpawnY = spawnY ?? SpawnY,
        GroundLevel = GroundLevel, RockLevel = RockLevel, Seed = Seed, GameMode = gameMode ?? GameMode,
        DungeonX = DungeonX, DungeonY = DungeonY,
        Time = time ?? Time, DayTime = dayTime ?? DayTime, MoonPhase = moonPhase ?? MoonPhase,
        BloodMoon = bloodMoon ?? BloodMoon, IsEclipse = isEclipse ?? IsEclipse, IsCrimson = IsCrimson,
        DownedBoss1EyeOfCthulhu = downedBoss1 ?? DownedBoss1EyeOfCthulhu,
        DownedBoss2EaterOfWorldsOrBrainOfCthulhu = downedBoss2 ?? DownedBoss2EaterOfWorldsOrBrainOfCthulhu,
        DownedBoss3Skeletron = downedBoss3 ?? DownedBoss3Skeletron,
        DownedQueenBee = downedQueenBee ?? DownedQueenBee,
        DownedMechBoss1TheDestroyer = downedMech1 ?? DownedMechBoss1TheDestroyer,
        DownedMechBoss2TheTwins = downedMech2 ?? DownedMechBoss2TheTwins,
        DownedMechBoss3SkeletronPrime = downedMech3 ?? DownedMechBoss3SkeletronPrime,
        DownedPlantBoss = downedPlant ?? DownedPlantBoss,
        DownedGolemBoss = downedGolem ?? DownedGolemBoss,
        DownedSlimeKingBoss = downedSlimeKing ?? DownedSlimeKingBoss,
        HardMode = hardMode ?? HardMode,
    };

    // Ver el comentario de WldReader.Read sobre por que este puntero (y no Pointers[9], el
    // "sectionPointers[8]" real de World.FileV2.cs de TEdit) es el inicio real de la seccion del
    // bestiario - confirmado ademas contra el WorldFile.cs decompilado real de tModLoader
    // (guarda `if (w.Version >= 210 && sectionPointers.Length > 9)` antes de leerla).
    public int? BestiarySectionOffset => Version >= 210 && Pointers.Length > 9 ? Pointers[8] : null;
}
