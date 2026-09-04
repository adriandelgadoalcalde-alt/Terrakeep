using System.IO;

namespace TerrasavrNative.App.Services;

// Icono real de un objeto vanilla - Assets/vanilla/icons/{id}.png, extraido DIRECTAMENTE de
// Images/Item_{id}.xnb real (instalacion de Steam, un fichero por objeto, mismo criterio que
// jugador/armadura/NPCs de la sexta auditoria de Opus).
//
// H6-11 (sexta auditoria de Opus, "los objetos animados -Alma de vuelo/Alma de luz, etc.- salen
// como una tira de fotogramas entera"): reemplaza de raiz la fuente anterior (un atlas
// "items.png" de 32 columnas x celdas de 40x40, heredado de Terrasavr-Calamity-Beta) - esa
// geometria de celda fija no bastaba para los ~102 objetos REALMENTE animados de Terraria
// (confirmado leyendo Terraria.Main.InitializeItemAnimations() decompilado real: 15 ids
// explicitos + TODOS los ItemID.Sets.IsFood reales, 86 mas + Scrying Orb - ver
// scripts/extraer-iconos-vanilla.js para la lista completa real), cuyo sprite real es una tira
// VERTICAL de 3-9 fotogramas del mismo alto. Ademas el atlas se quedaba corto de verdad: solo
// 5454 de los 6134 Item_{id}.xnb reales que existen en la instalacion (680 objetos sin icono en
// absoluto, no solo mal recortados - PalladiumDrill/1189, el unico hueco documentado antes, era
// solo UNO de 680). Con la fuente real por objeto: 6134/6196 ids (0..6195) tienen icono real
// (los 62 restantes no existen ni en la instalacion real - huecos genuinos en la numeracion
// del juego).
public static class VanillaIconResolver
{
    private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "icons");

    public static string? GetIconPath(int itemId)
    {
        string file = Path.Combine(IconsDir, $"{itemId}.png");
        return File.Exists(file) ? "pack://siteoforigin:,,,/Assets/vanilla/icons/" + itemId + ".png" : null;
    }
}
