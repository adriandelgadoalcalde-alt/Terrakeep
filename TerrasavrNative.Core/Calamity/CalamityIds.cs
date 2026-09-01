namespace TerrasavrNative.Core.Calamity;

// Constantes del esquema de ids sinteticos de Terrasavr-Calamity-Beta (confirmadas contra
// overrides.js real) - nunca se escriben tal cual en un .plr/.tplr, son solo bookkeeping en
// memoria de esta app.
public static class CalamityIds
{
    // Base de los ids sinteticos de OBJETOS de Calamity - asignados secuencialmente segun el
    // orden del array de catalog.json (SyntheticId = ItemIdBase + indice). Muy por encima de
    // cualquier id vanilla real (que llega como mucho a unos pocos miles).
    public const int ItemIdBase = 20000000;

    // Base de los ids sinteticos de los 21 PREFIJOS reales de Calamity (modPrefixMod/
    // modPrefixName) - vienen ya precalculados en calamity/rogue_prefixes.json (10000-10020),
    // no se recalculan aqui.
    public const int PrefixIdBase = 10000;
}
