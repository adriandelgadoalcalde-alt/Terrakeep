using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// N-d (segunda auditoria de Opus, Fable): "los 'Objetos nuevos' son texto, no sprites - toda la
// app enseña sprites, aqui no, y el resolvedor de iconos ya esta a mano". WhatsNewItem.Key es
// el nombre interno real (no un id numerico, ver el comentario real de la clase) - se resuelve
// aqui contra VanillaItemCatalog (mismo mecanismo real ya usado para el Pid de Investigacion).
// IconPath queda null (fallback real "?" del icono fantasma, ya existente) si el nombre interno
// no se reconoce - "lo que no se encuentra no se inventa", mismo criterio de siempre.
public sealed class WhatsNewItemViewModel(WhatsNewItem item, VanillaItemCatalog vanillaCatalog)
{
    public string DisplayName { get; } = item.DisplayName;
    public string? IconPath { get; } = item.Key != null && vanillaCatalog.GetIdByKey(item.Key) is int id
        ? VanillaIconResolver.GetIconPath(id)
        : null;
}
