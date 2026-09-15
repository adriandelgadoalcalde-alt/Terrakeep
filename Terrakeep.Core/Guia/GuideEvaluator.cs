using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;

namespace Terrakeep.Core.Guia;

// Consolidacion T1 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, 16-sep-2026): esta clase YA NO tiene
// ninguna logica propia de evaluacion. Es un adaptador que mantiene la API publica exacta que ya
// consume Terrakeep.App (GuideViewModel.cs) y Terrakeep.App.Tests (PruebasGuiaYServidor.cs) - CERO
// cambios de firma, para no obligar a tocar ningun consumidor - y por dentro delega TODO el
// calculo real a GuideEvaluationEngine, el cerebro UNICO que ahora tambien usa TerrakeepMod (ver
// Common/Guia/EvaluadorGuia.cs en el mod). Antes de esta ronda, este archivo tenia su propia copia
// del despacho de requisitos y de Fraccion/Contar/Preparacion/PasoCompletado, identica funcion a
// funcion a la de TerrakeepMod y sincronizada solo de memoria - ver GuideEvaluationEngine.cs para
// el detalle completo del porque y el como.
public sealed class GuideEvaluator(VanillaItemCatalog vanillaItems, NpcNameCatalog npcNames, CalamityCatalog? calamityItems)
{
    public ResultadoRequisitoGuia Evaluar(RequisitoGuia requisito, GuideContext contexto)
        => GuideEvaluationEngine.Evaluar(requisito, Proveedor(contexto));

    public List<ResultadoRequisitoGuia> Evaluar(PasoGuia paso, GuideContext contexto)
        => GuideEvaluationEngine.Evaluar(paso, Proveedor(contexto));

    public bool PasoCompletado(PasoGuia paso, GuideContext contexto)
        => GuideEvaluationEngine.PasoCompletado(paso, Proveedor(contexto));

    public float Preparacion(PasoGuia paso, GuideContext contexto, out int cumplidos, out int totalObligatorios)
        => GuideEvaluationEngine.Preparacion(paso, Proveedor(contexto), out cumplidos, out totalObligatorios);

    private DesktopGuideStateProvider Proveedor(GuideContext contexto)
        => new(vanillaItems, npcNames, calamityItems, contexto);
}
