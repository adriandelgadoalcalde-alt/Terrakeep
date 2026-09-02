using System.Collections.ObjectModel;

namespace TerrasavrNative.App.ViewModels;

// Un grupo de slots con nombre para mostrar (una pestaña/seccion: Inventario, Banco...).
public sealed class ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public ObservableCollection<ItemSlotViewModel> Slots { get; } = slots;

    // Nº de columnas reales de la cuadricula compacta (SlotGridPanel, ver
    // TerrasavrNative.App/Controls/SlotGridPanel.cs) - 10 por defecto (la propia rejilla de
    // Terraria para Inventario/Banco/Caja fuerte/Fragua/Boveda), pero Equipamiento necesita 5
    // (PlrLoadout.Items/Social/Dyes son siempre 10 slots reales en forma 5x2, no 10x1 - ver
    // EquipmentGroupViewModel.AddSlotSet).
    public int Columns { get; init; } = 10;

    // MinCell real de SlotGridPanel - suelo universal 40 (consulta a Opus, septima pasada:
    // midiendo los 5454 iconos vanilla reales, el sprite mediano ocupa 28 de 40px de lienzo;
    // con render NearestNeighbor la escala 40/40=1.0... en realidad la escala real depende del
    // Margin/BorderThickness del slot, ver SlotCompactTemplate - 40 da un contenido mediano de
    // ~21px, todavia reconocible, y coincide con el MinCell que ya llevaba meses en produccion
    // en la Libreria sin ninguna queja - el mejor experimento controlado real disponible en el
    // proyecto). Antes era 44 (una eleccion sin medir, "se sentia bien" mas que verificada).
    // Mascota/Montura/Tinte (columna unica, la identificacion no depende solo del sprite - cada
    // slot tiene su propio icono fantasma e indice fijo) bajan aparte a 32 en MainViewModel,
    // excepcion acotada, no el suelo universal.
    public double MinCell { get; init; } = 40;

    // MaxCell real de SlotGridPanel - techo universal 90 (consulta a Opus, septima pasada:
    // 96 daba una escala NearestNeighbor irregular de 2.15x; 90 da 2.00x exacto, el pixel art
    // no "tiembla"). Bug real encontrado verificando esta misma pasada con el arnes de UI
    // Automation (ActualWidth medido, no calculado a mano): Mascota/Montura/Tinte (columna
    // UNICA, sin techo propio hasta ahora) crecia sin limite hacia el techo universal en
    // cuanto sobraba alto disponible - con la fila fusionada ahora mas alta (ventana minima
    // subida a 700px) eso le robaba sitio real a la columna central (Armadura/Accesorios,
    // "Auto"+"*" - ver MainWindow.xaml), que es la que de verdad necesita el espacio. Estos 2
    // contenedores bajan aparte a MaxCell=56 en MainViewModel - siguen creciendo con la
    // ventana, solo que con un techo bajo, coherente con ser un lateral compacto de una sola
    // columna, no el bloque protagonista de la pantalla.
    public double MaxCell { get; init; } = 90;
}
