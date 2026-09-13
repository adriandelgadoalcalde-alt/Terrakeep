namespace Terrakeep.Core.WorldGen;

// Bioma maligno real elegido por el jugador al crear el mundo (las 3 opciones reales de la
// pantalla de creacion de Terraria: Aleatorio/Corrupcion/Carmesi - confirmado contra
// UIWorldCreation.cs decompilado, WorldEvilId).
public enum WorldEvilOption { Random, Corruption, Crimson }

// Resumen HONESTO de que contendra un mundo nuevo - nunca un mapa en miniatura (ver el
// comentario real de WorldSizeCatalog). Solo datos que se conocen con certeza ANTES de generar:
// tamaño exacto, las elecciones reales del jugador (dificultad/bioma maligno), y los efectos
// deterministas de una semilla secreta si el texto coincide con alguna.
public sealed record WorldCreationSummary(
    WorldSizeOption Size, int TilesWide, int TilesHigh,
    int Difficulty, WorldEvilOption Evil, string SeedText,
    IReadOnlyList<SpecialSeedEffect> SpecialSeedEffects);

public static class WorldCreationSummaryBuilder
{
    // Difficulty: mismos 4 valores reales que WldHeader.GameMode (0=Clasico, 1=Experto,
    // 2=Maestro, 3=Viaje) - un unico vocabulario para "dificultad de Terraria" en todo el
    // proyecto, nunca dos numeraciones distintas para el mismo concepto.
    public static WorldCreationSummary Build(WorldSizeOption size, int difficulty, WorldEvilOption evil, string? seedText)
    {
        if (difficulty is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(difficulty), "La dificultad real de Terraria solo tiene 4 valores (0=Clasico, 1=Experto, 2=Maestro, 3=Viaje).");

        var dims = WorldSizeCatalog.Get(size);
        var effects = SpecialSeedCatalog.Detect(seedText);
        return new WorldCreationSummary(size, dims.TilesWide, dims.TilesHigh, difficulty, evil, seedText ?? string.Empty, effects);
    }
}
