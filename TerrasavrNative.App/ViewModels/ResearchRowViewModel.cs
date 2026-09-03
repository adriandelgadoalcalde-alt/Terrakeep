namespace TerrasavrNative.App.ViewModels;

// Una entrada de investigacion (Modo Viaje) ya resuelta a nombre legible - el PID crudo
// (Terraria.ModLoader.ModType.FullName para modded, nombre interno plano para vanilla, nunca
// lleva "/" en ese caso) no se muestra directamente. IconPath - pedido explicito 1-sep-2026,
// "Libreria e Investigacion tienen que ser con sprites visuales" - null solo para el puñado de
// casos sin icono real (ver VanillaIconResolver), donde la UI cae a un "?" de texto.
public sealed class ResearchRowViewModel(string displayName, int count, int? requiredCount, bool isCalamity, string? iconPath)
{
    public string DisplayName { get; } = displayName;
    public int Count { get; } = count;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;

    // Auditoria de Opus, Bloque 2 (R-1): "x/N" real (VanillaResearchCountCatalog) en vez de solo
    // el conteo guardado a secas - null para Calamity (sin tabla real de investigacion
    // extraida esta pasada, ver el catalogo) cae a mostrar solo "x".
    public string CountLabel => requiredCount.HasValue ? $"{Count}/{requiredCount}" : Count.ToString();
}
