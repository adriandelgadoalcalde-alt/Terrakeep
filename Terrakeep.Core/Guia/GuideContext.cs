using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): lo que GuideEvaluator necesita del personaje/mundo REALES cargados en
// Terrakeep - deliberadamente todo NULLABLE. A diferencia de TerrakeepMod (que evalua contra una
// partida en marcha, siempre completa), Terrakeep es un editor de ficheros: puede haber un
// personaje cargado sin mundo, un mundo cargado sin personaje, los dos, o ninguno (la Guia sigue
// mostrando el arbol completo como mapa en ese ultimo caso, solo que sin poder marcar nada como
// cumplido - ver GuideEvaluator).
public sealed class GuideContext
{
    public PlrCharacter? Character { get; init; }
    // Los MISMOS contenedores ya fusionados con Calamity que usa el resto de la app
    // (CharacterFileService/CalamityCharacterSync) - evaluar "Objeto"/"ObjetoCualquiera"/
    // "Gancho" contra esto y no contra PlrCharacter.Inventory a pelo es lo que hace que un
    // objeto de Calamity en el inventario tambien cuente.
    public IReadOnlyDictionary<string, GameItem[]>? MergedContainers { get; init; }
    public WldWorld? World { get; init; }
    // Mismo criterio de deteccion de Calamity que ya usa el resto de Terrakeep
    // (MainViewModel.HasCalamityData: existe un .tplr real junto al .plr).
    public bool HasCalamity { get; init; }

    // Guia Encargo A "GuiaCalamity" (25-sep-2026): banderas de jefe/evento de CalamityMod.
    // DownedBossSystem YA traducidas a nombre canonico ("downedXxx", ver
    // TwldReader.ReadCalamityDownedFlags/CalamityDownedFlagMap) - leidas del `.twld` hermano del
    // `.wld` cargado, NUNCA del propio `.wld` (los datos de mod viven aparte, ver TwldReader.cs).
    // Mismo contrato que World: null = "no hay mundo cargado, no se puede saber" (GuideFlags.Valor
    // devuelve null, "carga un mundo"); un HashSet VACIO (con mundo cargado pero sin `.twld`/sin
    // esta seccion) es un dato real y distinto: "cargado, cero banderas puestas" (false), igual que
    // le pasaria a TerrakeepMod con NPC.downedXxx=false en una partida sin esos jefes derrotados.
    public IReadOnlySet<string>? CalamityDownedFlags { get; init; }

    // Guia v2 (F0, 02-oct-2026): mismo contrato null/"vacio" que CalamityDownedFlags.
    // Mejoras permanentes de CalamityPlayer (.tplr, "boost") - ver CalamityEstadoGuardado.
    public IReadOnlySet<string>? CalamityPlayerBoosts { get; init; }
    // Estado de mundo de MiscWorldStateSystem (.twld, "downed": revenge, death, esquemas...).
    public IReadOnlySet<string>? CalamityWorldState { get; init; }

    public static readonly GuideContext Empty = new();
}
