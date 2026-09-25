using System.Collections.Generic;
using Terrakeep.Core.Data;

namespace Terrakeep.Core.Model;

// Guia Encargo2 (25-sep-2026): calculo de defensa total ESTATICA (armadura+accesorios+prefijos)
// que antes vivia SOLO dentro de EquipmentGroupViewModel.RecomputeDefenseAndBonus
// (Terrakeep.App/ViewModels/EquipmentGroupViewModel.cs) - movido aqui, a Terrakeep.Core (sin
// dependencias de WPF/ViewModels), para que DesktopGuideStateProvider.Defensa pueda reutilizar
// exactamente la misma logica en vez de duplicarla o dejarla a 0. Mismo criterio ya documentado
// en el metodo original: solo los prefijos VANILLA tienen datos reales de bono de defensa en este
// catalogo (Calamity/Rogue no), "desconocido" cuenta como 0, nunca inventado.
public static class DefenseCalculator
{
    /// <summary>Suma la defensa de un conjunto de slots de Armadura/Accesorios ya cargado
    /// (tipicamente los 10 slots Cabeza/Cuerpo/Piernas + 7 accesorios de un loadout) - slots
    /// vacios no aportan nada. Nunca lee una partida en marcha: es el mismo dato ESTATICO que ya
    /// muestra la pestaña Equipamiento.</summary>
    public static int Total(IEnumerable<GameItem> items, VanillaItemStatsCatalog? vanillaStats,
        CalamityCatalog? calamityCatalog, PrefixEffectCatalog? prefixEffects)
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item.IsEmpty) continue;
            total += item.IsCalamity
                ? calamityCatalog?.BySyntheticId(item.Id)?.Stats?.Defense ?? 0
                : vanillaStats?.Get(item.Id)?.Defense ?? 0;

            // H3-08 (tercera auditoria de Opus, Fable, ver historial original en
            // EquipmentGroupViewModel): defensa de prefijos de accesorio reales
            // (Warding/Guarding/Menacing/Hardy/Armored, +1..+4 segun Player.GrantPrefixBenefits).
            // Solo prefijos vanilla tienen datos reales de defensa en este catalogo.
            if (!item.Prefix.IsCalamity && prefixEffects != null)
                total += prefixEffects.GetDefenseBonus(item.Prefix.VanillaId);
        }
        return total;
    }
}
