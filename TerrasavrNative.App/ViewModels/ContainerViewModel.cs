using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Un grupo de slots con nombre para mostrar (una pestaña/seccion: Inventario, Banco...).
public sealed partial class ContainerViewModel : ObservableObject
{
    private readonly string _baseName;

    public string Key { get; }
    public ObservableCollection<ItemSlotViewModel> Slots { get; }

    // A-c (segunda auditoria de Opus, Fable): "los contadores de A-1 solo estan en Almacenes -
    // 'Inventario (47/50)' seria igual de util y no existe en ningun sitio". Mismo mecanismo
    // real ya usado en EquipmentOptionViewModel.DisplayLabel (A-1), aplicado aqui de forma
    // universal a CUALQUIER contenedor (no solo los 4 de Almacenes con pildora) - la misma
    // pregunta real ("cuanto llevo puesto de verdad?") aplica igual a Inventario, Banco, Caja
    // fuerte, Fragua... DisplayName sigue siendo el mismo binding de siempre en el XAML (cero
    // sitios que tocar) - solo que ahora incluye el recuento real y en vivo.
    public string DisplayName => $"{_baseName} ({Slots.Count(s => !s.IsEmpty)}/{Slots.Count})";

    public ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
    {
        Key = key;
        _baseName = displayName;
        Slots = slots;
        foreach (var slot in slots)
            slot.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ItemSlotViewModel.IsEmpty)) OnPropertyChanged(nameof(DisplayName)); };
    }

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

    // A-d (segunda auditoria de Opus, Fable): "operaciones en bloque - ordenar, vaciar
    // contenedor, mover todo al banco" - ninguna de las 3 existia, solo el "Vaciar slot"
    // individual de siempre.
    [RelayCommand]
    private void ClearAll()
    {
        foreach (var slot in Slots)
            if (!slot.IsEmpty) slot.ClearCommand.Execute(null);
    }

    // Player.inventory[0..9] real (Terraria.UI.ItemSorting.SortInventory decompilado -
    // `Sort(Main.player[...].inventory, 0, 1, 2, ..., 9, 50, ...)` excluye EXPLICITAMENTE la
    // barra rapida de cualquier ordenado real, ademas de los slots de moneda/municion 50-58,
    // que en este puerto ya viven en contenedores propios aparte, no en "inventory").
    private const int HotbarSlotCount = 10;

    // "Ordenar": por Id ascendente, empaquetando los objetos hacia el principio del
    // contenedor y dejando los huecos vacios al final.
    //
    // Decision real, no improvisada: el algoritmo REAL de Terraria (Terraria.UI.ItemSorting,
    // decompilado) no es "un solo criterio" - son ~30 capas por tipo de daño/herramienta/
    // consumible, cada una con su propio set de prioridad por id (SortingPriorityWeaponsRanged,
    // SortingPriorityToolsMisc...). Replicarlo exigiria datos que este catalogo no extrae hoy
    // (melee/ranged/magic/summon, createTile, sets de prioridad por id) - un trabajo de
    // extraccion nuevo, bastante mayor que este boton, mismo motivo real por el que L-f (limite
    // de "Cantidad" por maxStack real) quedo aparcado en vez de fingido. Id ascendente es un
    // criterio propio, honesto y documentado - no una imitacion a medias del real.
    //
    // H3-09 (tercera auditoria de Opus, Fable): "Ordenar" reordenaba TAMBIEN los primeros 10
    // slots de Inventario (la barra rapida) - el juego real NUNCA lo hace (ver el comentario de
    // HotbarSlotCount arriba), asi que un objeto que el jugador tenia deliberadamente en una
    // tecla concreta (1-0) podia acabar en otra tras pulsar Ordenar. Solo aplica a "inventory" -
    // ningun otro contenedor (Banco/Caja fuerte/Fragua/Boveda) tiene barra rapida real.
    [RelayCommand]
    private void Sort()
    {
        int fixedPrefix = Key == "inventory" ? Math.Min(HotbarSlotCount, Slots.Count) : 0;
        var items = Slots.Skip(fixedPrefix).Where(s => !s.IsEmpty).Select(s => s.Item).OrderBy(i => i.Id).ToList();
        for (int i = fixedPrefix; i < Slots.Count; i++)
            Slots[i].UpdateFrom(i - fixedPrefix < items.Count ? items[i - fixedPrefix] : GameItem.Empty);
    }

    // "Mover todo al banco": usado desde Inventario hacia el Almacen seleccionado (StorageGroup.
    // Current) - generico para cualquier par origen/destino, respeta AcceptedKind del destino
    // (irrelevante hoy entre Inventario/Almacenes, ninguno de los dos restringe tipo, pero es el
    // mismo criterio real que ya usa PlaceItem, no uno nuevo). Lo que no cupo se queda donde
    // estaba - nunca se pierde nada en silencio.
    public int MoveAllTo(ContainerViewModel destination)
    {
        var freeSlots = destination.Slots.Where(s => s.IsEmpty).ToList();
        int moved = 0, di = 0;
        foreach (var slot in Slots)
        {
            if (slot.IsEmpty) continue;
            while (di < freeSlots.Count && !freeSlots[di].AcceptsItem(slot.ItemId)) di++;
            if (di >= freeSlots.Count) break;
            freeSlots[di].UpdateFrom(slot.Item);
            slot.UpdateFrom(GameItem.Empty);
            di++;
            moved++;
        }
        return moved;
    }
}
