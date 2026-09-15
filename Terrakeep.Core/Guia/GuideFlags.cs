using Terrakeep.Core.WldFormat;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): banderas permanentes que Terrakeep (escritorio) sabe leer, por nombre -
// mismo espiritu que BanderasGuia.cs de TerrakeepMod (vocabulario cerrado, lo que no se reconoce
// se marca no evaluable y se dice, nunca se da por cumplido), pero una tabla mucho mas corta.
//
// POR QUE es mas corta que la del mod, con honestidad: TerrakeepMod lee las banderas en vivo del
// motor (NPC.downedXxx, campos publicos siempre disponibles con la partida cargada). Terrakeep
// solo tiene lo que su propio lector de .wld/.plr ya parsea - y WldHeader.cs documenta una
// limitacion de arquitectura DELIBERADA (comentario real en ese archivo): el tramo de banderas
// del .wld que se lee es de ANCHO FIJO justo hasta HardMode; los jefes tardios (Reina Slime aparte,
// que SI tiene su propio campo opcional) - Duque Pezhongo, Emperatriz de la Luz, Culto Lunatico,
// Torres, Señor de la Luna, Ejercito Goblin, Piratas, Legion de Escarcha, Marcianos, Luna de
// Calabazas/Helada - viven DESPUES de secciones de ancho VARIABLE (lista de Anglers, banners) que
// Terrakeep no atraviesa todavia (localizarlas exige mas trabajo de lector de .wld, fuera del
// alcance de esta ronda de integracion - ver bitacora.md). Añadirlas es extender WldReader/
// WldHeader, no este archivo.
//
// Las de Calamity (banderas de CalamityMod.DownedBossSystem) tampoco estan aqui: Calamity guarda
// su propio estado de jefes derrotados en datos de MOD dentro del .wld (TileEntities/ModData),
// que Terrakeep no parsea en absoluto hoy (su soporte de Calamity es solo de OBJETOS, ver
// Calamity/CalamityCharacterSync.cs) - se documenta como pendiente real, no se inventa.
public static class GuideFlags
{
    // Las 9 banderas de jefe + HardMode que WldHeader.cs ya parsea de verdad (bloque de ancho
    // fijo, confirmado contra World.FileV2.cs de TEdit - ver el comentario real en WldHeader.cs)
    // + DownedSlimeKingBoss, que existe solo desde la version 118 del formato (bool? en
    // WldHeader, null = el archivo ni siquiera guarda el campo, distinto de "no derrotado").
    private static readonly Dictionary<string, Func<WldHeader, bool?>> _deMundo = new()
    {
        { "downedBoss1", h => h.DownedBoss1EyeOfCthulhu },
        { "downedBoss2", h => h.DownedBoss2EaterOfWorldsOrBrainOfCthulhu },
        { "downedBoss3", h => h.DownedBoss3Skeletron },
        { "downedQueenBee", h => h.DownedQueenBee },
        { "downedMechBoss1", h => h.DownedMechBoss1TheDestroyer },
        { "downedMechBoss2", h => h.DownedMechBoss2TheTwins },
        { "downedMechBoss3", h => h.DownedMechBoss3SkeletronPrime },
        { "downedMechBossAny", h => h.DownedMechBoss1TheDestroyer || h.DownedMechBoss2TheTwins || h.DownedMechBoss3SkeletronPrime },
        { "downedPlantBoss", h => h.DownedPlantBoss },
        { "downedGolemBoss", h => h.DownedGolemBoss },
        { "downedSlimeKing", h => h.DownedSlimeKingBoss },
        { "hardMode", h => h.HardMode },
    };

    /// <summary>true si Terrakeep sabe (en principio) leer esta bandera - independientemente de
    /// si HAY mundo/personaje cargado ahora mismo (eso lo decide <see cref="Valor"/>).</summary>
    public static bool Existe(string nombre) =>
        !string.IsNullOrEmpty(nombre) &&
        (_deMundo.ContainsKey(nombre) || nombre == "downedDD2EventAnyDifficulty");

    /// <summary>Valor real, o null si no se puede comprobar ahora (bandera desconocida, o
    /// conocida pero sin el mundo/personaje que la guarda cargado).</summary>
    public static bool? Valor(string nombre, GuideContext contexto)
    {
        if (string.IsNullOrEmpty(nombre)) return null;

        // Unica bandera de Player.cs (no de NPC.cs/WorldGen.cs) de toda la tabla del mod -
        // PlrCharacter.FinishedDD2Event es el mismo campo real (Player.cs, ~linea 2171/23413/
        // 55965: downedDD2EventAnyDifficulty), ya parseado del .plr.
        if (nombre == "downedDD2EventAnyDifficulty")
            return contexto.Character?.FinishedDD2Event;

        if (_deMundo.TryGetValue(nombre, out var leer))
            return contexto.World != null ? leer(contexto.World.Header) : null;

        return null;
    }
}
