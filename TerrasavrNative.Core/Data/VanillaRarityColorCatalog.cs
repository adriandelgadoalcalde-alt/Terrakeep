namespace TerrasavrNative.Core.Data;

// Color REAL de rareza de Terraria (auditoria de Opus, octava pasada, D-3: "ItemStatsFormatter
// imprime literalmente 'Rareza 5'... pintar el nombre del objeto con su color real es el
// vocabulario exacto que espera un jugador de Terraria"). Valores verificados a mano contra el
// decompilado real (Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria\ID\Colors.cs +
// Terraria\GameContent\UI\ItemRarity.cs, _rarities.Add(...)/GetColor) - no adivinados, no de
// memoria de la wiki. Solo cubre las 11 rarezas REALES de vanilla; Calamity define rarezas
// propias por encima de 11 con sus propios colores (no investigadas esta pasada, ver
// ItemStatsFormatter) - Get() devuelve null para esas, el llamador cae al color de texto normal
// en vez de fingir un color. La rareza 0 (blanca, la mas comun) en el juego real pulsa
// (Main.mouseTextColor, una animacion) - aqui se deja tambien en null a proposito, un color fijo
// blanco ya es el texto normal por defecto, no hace falta inventar la animacion.
public static class VanillaRarityColorCatalog
{
    private static readonly Dictionary<int, (byte R, byte G, byte B)> Colors = new()
    {
        [-11] = (255, 175, 0),   // Ambar
        [-1] = (130, 130, 130),  // Basura
        [1] = (150, 150, 255),   // Azul
        [2] = (150, 255, 150),   // Verde
        [3] = (255, 200, 150),   // Naranja
        [4] = (255, 150, 150),   // Rojo
        [5] = (255, 150, 255),   // Rosa
        [6] = (210, 160, 255),   // Morado
        [7] = (150, 255, 10),    // Lima
        [8] = (255, 255, 10),    // Amarillo
        [9] = (5, 200, 255),     // Cian
    };

    public static (byte R, byte G, byte B)? Get(int? rare) =>
        rare.HasValue && Colors.TryGetValue(rare.Value, out var color) ? color : null;
}
