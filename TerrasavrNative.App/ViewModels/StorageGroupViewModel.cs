using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// Consolida Banco/Caja fuerte/Fragua del Defensor/Boveda del Vacio (4 contenedores de 40
// slots cada uno) en una unica pestaña "Almacenes" con selector de 4 pildoras - mismo patron
// que EquipmentGroupViewModel (LoadoutOptions/KindOptions), pero mas simple: solo un nivel de
// seleccion, y reutiliza EquipmentOptionViewModel tal cual (Label + IsSelected) en vez de
// duplicar esa clase. Pregunta a Opus sobre el diseño (2-sep-2026): antes estos 4 vivian como
// 4 de las 9 pestañas planas que se comian el espacio - ahora son 1 pestaña + 4 pildoras
// pequeñas, igual que ya se hizo con los 12 sub-contenedores de Equipamiento.
//
// IMPORTANTE (riesgo cero para guardar/cargar, ver bitacora.md): esto NO fusiona ninguna
// ObservableCollection ni crea contenedores nuevos - los 4 ContainerViewModel que recibe son
// los MISMOS objetos que ya viven en MainViewModel.Containers (mismas claves "bank"/"bank2"/
// "bank3"/"bank4" que SyncEditsBackToMerged ya sabe encontrar). Esto es solo una capa de
// PRESENTACION encima, el selector decide cual de los 4 se muestra.
public partial class StorageGroupViewModel : ObservableObject
{
    // OBJ-01 (oleada de pruebas de Objetos, 6-sep-2026) - BUG REAL, no una precaucion: el XAML
    // cambia el DataContext a ESTE objeto en los dos sitios donde vive la cabecera de Almacenes
    // (`<DockPanel DataContext="{Binding StorageGroup}">` de la pestaña propia, y el `<Border
    // DataContext="{Binding StorageGroup}">` de la mitad derecha de Inventario en Amplio) y
    // dentro usa `{Binding Loc[action_save_set]}` y compañia. Sin esta propiedad esos 10
    // bindings (5 Content + 5 ToolTip) no resuelven la ruta y WPF NO avisa de nada: deja el
    // Content en null y los 5 botones reales - "Guardar conjunto...", "Cargar...", "Añadir...",
    // "Ordenar" y "Vaciar contenedor" - se quedan en 12px de puro padding, sin texto, en los dos
    // idiomas y a cualquier tamaño de ventana. Mismo bloque de idioma que ya llevan
    // ContainerViewModel/ItemSlotViewModel por el mismo motivo.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly Dictionary<int, ContainerViewModel> _byIndex = new();

    public ObservableCollection<EquipmentOptionViewModel> Options { get; } = [];

    [ObservableProperty] private int _selectedIndex;

    public ContainerViewModel Current => _byIndex[SelectedIndex];

    public StorageGroupViewModel(ContainerViewModel bank, ContainerViewModel safe, ContainerViewModel forge, ContainerViewModel voidVault)
    {
        // Ronda de idioma del 6-sep-2026: se pasa la CLAVE, no el texto ya resuelto - antes
        // las 4 pildoras se quedaban en el idioma de arranque ("Bóveda del Vacío (0/40)" y
        // "Fragua del Defensor (15/40)" salieron tal cual en el barrido con la app en ingles).
        Add(0, "storage_bank", bank);
        Add(1, "storage_safe", safe);
        Add(2, "storage_forge", forge);
        Add(3, "storage_void", voidVault);
        Options[0].IsSelected = true;
    }

    private void Add(int index, string labelKey, ContainerViewModel container)
    {
        _byIndex[index] = container;
        // Auditoria de Opus, A-1: pasar el contenedor real activa el contador en vivo
        // "Banco (38/40)" en la propia pildora, ver EquipmentOptionViewModel.DisplayLabel.
        Options.Add(new EquipmentOptionViewModel(labelKey, index, container));
    }

    [RelayCommand]
    private void Select(EquipmentOptionViewModel? option)
    {
        if (option == null) return;
        SelectedIndex = option.Value;
        foreach (var o in Options) o.IsSelected = o == option;
        OnPropertyChanged(nameof(Current));
    }
}
