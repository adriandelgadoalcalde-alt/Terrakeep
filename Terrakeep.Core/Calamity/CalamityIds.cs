namespace Terrakeep.Core.Calamity;

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

    // Base de los ids sinteticos de BUFFS de Calamity - asignados secuencialmente segun el
    // orden del array de calamity/buffs.json (SyntheticId = BuffIdBase + indice), mismo
    // esquema que ItemIdBase. Muy por encima de cualquier id de buff vanilla real (BuffID.cs
    // llega como mucho a unos pocos cientos). A diferencia de la version JS, que usa un
    // umbral capturado en caliente del motor Haxe compilado (calamityBaseBuffCount = "cuantos
    // buffs conocia el motor antes de registrar el catalogo de Calamity"), aqui es una
    // constante fija - no hay motor Haxe cuyo estado en tiempo de ejecucion replicar, y una
    // constante muy por encima de cualquier id real vanilla es equivalente en la practica.
    public const int BuffIdBase = 25000000;
}
