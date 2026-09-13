using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels;

// Un objeto (vanilla o Calamity) tal y como aparece en UN lado del Comparador de personajes -
// solo lectura, sin ningun comando (a diferencia de LibraryItemViewModel/ItemSlotViewModel, que
// son interactivos). Nombre/prefijo YA resueltos en el idioma activo en el momento de construir
// (CompareViewModel reconstruye las filas enteras al cambiar de idioma, igual que hace
// LibraryViewModel con RefrescarIdioma - ver CompareViewModel.RebuildRows).
public sealed class CompareItemViewModel(string? iconPath, string name, string? prefixName, int count, bool isCalamity, bool isEmpty)
{
    public static readonly CompareItemViewModel Empty = new(null, string.Empty, null, 0, false, true);

    // Mismo bloque de idioma ya establecido en LibraryItemViewModel/CharacterListEntryViewModel:
    // esta clase se usa como DataContext dentro de una plantilla con ContentPresenter -
    // {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource.
    public LocalizationService Loc => LocalizationService.Instance;

    public string? IconPath { get; } = iconPath;
    public string Name { get; } = name;
    public string? PrefixName { get; } = prefixName;
    public int Count { get; } = count;
    public bool IsCalamity { get; } = isCalamity;
    public bool IsEmpty { get; } = isEmpty;
    public bool ShowCount => !IsEmpty && Count > 1;
    // "Run" (a diferencia de TextBlock) no tiene Visibility - se resuelve como texto ya
    // condicional en vez de intentar ocultar el propio Run.
    public string CountLabel => ShowCount ? $" x{Count}" : string.Empty;
}
