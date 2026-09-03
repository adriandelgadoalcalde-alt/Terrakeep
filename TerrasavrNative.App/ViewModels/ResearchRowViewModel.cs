using TerrasavrNative.App.Services;

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
    //
    // R-d/F1 (segunda auditoria de Opus, Fable): "el 9999 crudo en cada chip de Calamity - R-1
    // elimino el numero sospechoso para vanilla, dejandolo visible en Calamity". Ese 9999
    // concreto (ResearchAllService.PlaceholderCount) NO es un dato real del juego, es el
    // placeholder que "Investigar todo" escribe cuando no hay umbral real conocido - se muestra
    // "Investigado" en su lugar. Cualquier OTRO conteo real de Calamity (un personaje que
    // investigo de verdad en el juego, no via este boton) SIGUE mostrando su numero real - eso
    // si es un dato real, no un numero inventado que ocultar.
    public string CountLabel => IsCalamity && Count == ResearchAllService.PlaceholderCount
        ? "✔ Investigado"
        : requiredCount.HasValue ? $"{Count}/{requiredCount}" : Count.ToString();
}
