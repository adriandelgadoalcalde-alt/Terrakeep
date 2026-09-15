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

    public static readonly GuideContext Empty = new();
}
