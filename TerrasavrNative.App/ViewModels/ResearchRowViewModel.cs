using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// Una entrada de investigacion (Modo Viaje) ya resuelta a nombre legible - el PID crudo
// (Terraria.ModLoader.ModType.FullName para modded, nombre interno plano para vanilla, nunca
// lleva "/" en ese caso) no se muestra directamente. IconPath - pedido explicito 1-sep-2026,
// "Libreria e Investigacion tienen que ser con sprites visuales" - null solo para el puñado de
// casos sin icono real (ver VanillaIconResolver), donde la UI cae a un "?" de texto.
//
// H5-02 (quinta auditoria de Opus): "Investigacion es de solo lectura salvo un boton de todo o
// nada" - Count ahora es real y editable (clic en la fila alterna investigado/no investigado,
// el campo de conteo real acepta parciales) - ver ResearchViewModel.ApplyFilter, que ahora
// muestra TODO el contenido de la carpeta, no solo lo ya investigado.
public sealed partial class ResearchRowViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public int Id { get; }
    public string DisplayName { get; }
    public bool IsCalamity { get; }
    public string? IconPath { get; }
    // Umbral real (VanillaResearchCountCatalog) - null para Calamity (sin tabla real extraida,
    // ver ResearchAllService.PlaceholderCount).
    public int? RequiredCount { get; }

    [ObservableProperty] private int _count;

    public bool IsResearched => Count > 0;

    public ResearchRowViewModel(int id, string displayName, int count, int? requiredCount, bool isCalamity, string? iconPath)
    {
        Id = id;
        DisplayName = displayName;
        // Asignacion directa al campo (no a la propiedad Count) - a proposito: la propiedad
        // generada SI dispara OnCountChanged/PropertyChanged, y ese camino esta reservado para
        // ediciones reales del usuario (ver el comentario de OnCountChanged) - construir la
        // fila con su conteo inicial no es una edicion.
        _count = count;
        RequiredCount = requiredCount;
        IsCalamity = isCalamity;
        IconPath = iconPath;
    }

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
    public string CountLabel => !IsResearched
        ? (RequiredCount.HasValue ? $"0/{RequiredCount}" : "Sin investigar")
        : IsCalamity && Count == ResearchAllService.PlaceholderCount
            ? "✔ Investigado"
            : RequiredCount.HasValue ? $"{Count}/{RequiredCount}" : Count.ToString();

    // H5-02: solo dispara para ediciones REALES del usuario (clic en la fila, o el campo de
    // conteo editable) - ver el comentario del constructor. ResearchViewModel se suscribe a
    // esto (no a PropertyChanged generico) para saber exactamente que id cambio y a que valor,
    // sin tener que recorrer todas las filas comparando.
    partial void OnCountChanged(int value)
    {
        OnPropertyChanged(nameof(CountLabel));
        OnPropertyChanged(nameof(IsResearched));
        CountChangedByUser?.Invoke(this, value);
    }

    public event Action<ResearchRowViewModel, int>? CountChangedByUser;
}
