namespace Terrakeep.Core.WorldGen;

// Vista previa de generacion de mundo (14-sep-2026, punto 9 de la lista confirmada por el
// usuario del 13-sep-2026): Terrakeep no genera mundos de verdad (a diferencia de Starvekeep con
// DST, que SI arranca un servidor dedicado real - ver su bitacora.md, 13-sep-2026, "Vista previa
// y generacion de mundo nuevo") - un mundo de Terraria se genera con el algoritmo real de
// WorldGen.cs (miles de lineas, dependencias en cascada entre pasadas), reimplementarlo aunque
// sea aproximado es un proyecto en si mismo. Alcance honesto de esta primera version: NUNCA un
// mapa en miniatura falso, SI un resumen real de que se sabe con CERTEZA antes de generar -
// tamaño exacto en tiles (constante real del juego, no una aproximacion), el bioma maligno que
// el jugador elige de verdad al crear el mundo, y las semillas secretas reales (documentadas,
// deterministas) si el texto de semilla coincide con alguna.
//
// Tamaños reales confirmados contra Terraria/GameContent/UI/States/UIWorldCreation.cs
// decompilado (tModLoader real, metodo que fija Main.maxTilesX/Y segun WorldSizeId).
public enum WorldSizeOption { Small, Medium, Large }

public readonly record struct WorldDimensions(int TilesWide, int TilesHigh);

public static class WorldSizeCatalog
{
    public static WorldDimensions Get(WorldSizeOption size) => size switch
    {
        WorldSizeOption.Small => new WorldDimensions(4200, 1200),
        WorldSizeOption.Medium => new WorldDimensions(6400, 1800),
        WorldSizeOption.Large => new WorldDimensions(8400, 2400),
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };
}
