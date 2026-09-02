using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Una opcion de un selector pequeño (loadout o vista) - mismo patron que
// PrefixMetaButtonViewModel/PrefixGroupButtonViewModel (Label + IsSelected).
public sealed partial class EquipmentOptionViewModel(string label, int value) : ObservableObject
{
    public string Label { get; } = label;
    public int Value { get; } = value;

    [ObservableProperty] private bool _isSelected;
}

// Armadura/accesorios, Vanidad (Social) y Tintes de un mismo loadout - los 3 "kinds" reales
// que ya usaba MainViewModel.RebuildContainers por separado.
public enum EquipmentKind { Items = 0, Social = 1, Dyes = 2 }

// Consolida los 12 sub-contenedores de equipo puesto (Puesto + Loadout 1/2/3, cada uno con
// Armadura/Vanidad/Tintes) en una unica pantalla con selector, igual que el "Sb"/app.TabEquips
// real de Terrasavr (3 botones "1"/"2"/"3" dentro de UNA pantalla "Equipamiento", ver
// bitacora.md "Fase D" - investigacion de script.js real). Antes eran 12 pestañas planas mas
// en el mismo TabControl que Inventario/Banco/... (21 pestañas totales, de ahi el amontonamiento
// real al reducir la ventana que reporto el usuario) - ahora son solo 4+3 botones pequeños
// dentro de una unica entrada.
//
// Los ContainerViewModel internos son los MISMOS objetos de siempre (mismas claves
// "loadout0Items"/"loadout1Social"/...) - AutoEquip/SyncEditsBackToMerged en MainViewModel
// los siguen encontrando via EquippedItems/AllContainers, no cambia su contrato con
// MergedContainers.
public partial class EquipmentGroupViewModel : ObservableObject
{
    private readonly Dictionary<(int Loadout, EquipmentKind Kind), ContainerViewModel> _byKey = new();

    public IReadOnlyList<ContainerViewModel> AllContainers { get; }

    // Atajo para AutoEquip (siempre coloca en el equipo puesto, loadout 0).
    public ContainerViewModel EquippedItems => _byKey[(0, EquipmentKind.Items)];

    public ObservableCollection<EquipmentOptionViewModel> LoadoutOptions { get; } = [];
    public ObservableCollection<EquipmentOptionViewModel> KindOptions { get; } =
    [
        new EquipmentOptionViewModel("Armadura", (int)EquipmentKind.Items) { IsSelected = true },
        new EquipmentOptionViewModel("Vanidad", (int)EquipmentKind.Social),
        new EquipmentOptionViewModel("Tintes", (int)EquipmentKind.Dyes),
    ];

    [ObservableProperty] private int _selectedLoadout;
    [ObservableProperty] private EquipmentKind _selectedKind = EquipmentKind.Items;

    public ObservableCollection<ItemSlotViewModel> CurrentSlots => _byKey[(SelectedLoadout, SelectedKind)].Slots;

    // Pregunta a Opus sobre el diseño (2-sep-2026, segunda consulta - "haz lo mismo para
    // equipamientos"): Equipamiento era el unico sitio con su propio bloque Viewbox+ItemsControl
    // a medida en el XAML en vez de reusar ContainerTabTemplate como Monturas/Monedas/Almacenes -
    // exponer el ContainerViewModel completo (no solo sus Slots) permite que el XAML haga
    // <ContentControl Content="{Binding Current}" ContentTemplate="{StaticResource
    // ContainerTabTemplate}" /> y herede automaticamente el fondo de "hueco vacio", el tooltip
    // compuesto y cualquier arreglo futuro de esa plantilla compartida, sin poder volver a
    // divergir.
    public ContainerViewModel Current => _byKey[(SelectedLoadout, SelectedKind)];

    public EquipmentGroupViewModel(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot,
        Dictionary<string, GameItem[]> mergedContainers, int realLoadoutCount)
    {
        AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Items, "Equipo puesto - armadura/accesorios", mergedContainers["loadout0Items"]);
        AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Social, "Equipo puesto - vanidad", mergedContainers["loadout0Social"]);
        AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Dyes, "Equipo puesto - tintes", mergedContainers["loadout0Dyes"]);
        LoadoutOptions.Add(new EquipmentOptionViewModel("Puesto", 0) { IsSelected = true });

        for (int i = 1; i <= realLoadoutCount; i++)
        {
            AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Items, $"Loadout {i} - armadura/accesorios", mergedContainers[$"loadout{i}Items"]);
            AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Social, $"Loadout {i} - vanidad", mergedContainers[$"loadout{i}Social"]);
            AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Dyes, $"Loadout {i} - tintes", mergedContainers[$"loadout{i}Dyes"]);
            LoadoutOptions.Add(new EquipmentOptionViewModel(i.ToString(), i));
        }

        AllContainers = _byKey.Values.ToList();
    }

    private void AddSlotSet(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot,
        int loadout, EquipmentKind kind, string displayName, GameItem[] items)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
            slots.Add(new ItemSlotViewModel(service, i, displayName, items[i], requestPickForSlot, isEquipped: true));
        string key = kind switch
        {
            EquipmentKind.Items => $"loadout{loadout}Items",
            EquipmentKind.Social => $"loadout{loadout}Social",
            EquipmentKind.Dyes => $"loadout{loadout}Dyes",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        // Columns=5: PlrLoadout.Items/Social/Dyes son siempre 10 slots reales en forma 5x2 -
        // sin esto SlotGridPanel usaria el default de 10 columnas y organizaria una tira larga
        // y fina de 10x1 en vez del bloque compacto real.
        _byKey[(loadout, kind)] = new ContainerViewModel(key, displayName, slots) { Columns = 5 };
    }

    [RelayCommand]
    private void SelectLoadout(EquipmentOptionViewModel? option)
    {
        if (option == null) return;
        SelectedLoadout = option.Value;
        foreach (var o in LoadoutOptions) o.IsSelected = o == option;
        OnPropertyChanged(nameof(CurrentSlots));
        OnPropertyChanged(nameof(Current));
    }

    [RelayCommand]
    private void SelectKind(EquipmentOptionViewModel? option)
    {
        if (option == null) return;
        SelectedKind = (EquipmentKind)option.Value;
        foreach (var o in KindOptions) o.IsSelected = o == option;
        OnPropertyChanged(nameof(CurrentSlots));
        OnPropertyChanged(nameof(Current));
    }
}
