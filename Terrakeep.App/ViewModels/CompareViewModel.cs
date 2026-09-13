using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Comparador de dos personajes/builds (encargo del usuario, 13-sep-2026, primero de la lista
// confirmada de funciones nuevas): "seleccionar dos personajes/builds y ver lado a lado sus
// diferencias (equipo, stats, prefijos, inventario) - resalta lo que cambia". Overlay de nivel
// de ventana, mismo mecanismo real ya usado por BackupHistoryViewModel (velo opaco + panel
// centrado + Escape/clic fuera lo cierra) - reutiliza ese lenguaje visual en vez de inventar
// uno nuevo, y no exige tener ningun personaje cargado en el editor principal para usarlo (se
// abre desde Inicio, sobre la lista de personajes ya escaneada).
//
// CharacterFileService PROPIO (no el de MainViewModel): Load() muta EsPersonajeTModLoader en la
// propia instancia del servicio (que tabla de "mejor prefijo" usar) - comparar dos personajes
// seguidos con el servicio COMPARTIDO del editor dejaria ese flag con el valor del ULTIMO
// personaje comparado, y el boton de "mejor prefijo" del editor principal (que lee esa misma
// propiedad en caliente, ver PrefixSuggester/ItemSlotViewModel) empezaria a sugerir contra la
// tabla equivocada la proxima vez que el usuario lo pulsara - sin que nada en pantalla avisara
// del cambio. Aislarlo del todo cuesta una carga extra de catalogos (una operacion iniciada por
// el usuario, no una ruta caliente) y elimina ese riesgo por completo.
public sealed partial class CompareViewModel : ObservableObject
{
    private readonly CharacterFileService _service = new();
    private readonly IReadOnlyList<CharacterListEntryViewModel> _availableCharacters;

    private LoadedCharacter? _loadedA;
    private LoadedCharacter? _loadedB;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private CharacterListEntryViewModel? _selectedA;
    [ObservableProperty] private CharacterListEntryViewModel? _selectedB;
    [ObservableProperty] private string? _errorMessage;

    public IReadOnlyList<CharacterListEntryViewModel> AvailableCharacters => _availableCharacters;

    public bool HasBothSelected => SelectedA != null && SelectedB != null;
    public bool ShowResults => HasBothSelected && ErrorMessage == null;
    // El boton de Inicio abre el panel SIEMPRE (no exige tener ya 2 personajes de antemano,
    // igual que "Cargar personaje..." no exige nada) - el panel mismo avisa con claridad si la
    // lista escaneada tiene menos de 2 para elegir, en vez de un boton deshabilitado sin
    // explicacion.
    public bool NeedsMoreCharacters => _availableCharacters.Count < 2;

    public ObservableCollection<CompareStatRowViewModel> StatRows { get; } = [];
    public ObservableCollection<CompareEquipmentRowViewModel> EquipmentRows { get; } = [];
    public ObservableCollection<CompareInventorySlotViewModel> InventoryA { get; } = [];
    public ObservableCollection<CompareInventorySlotViewModel> InventoryB { get; } = [];

    // Resumen real (pastilla de la cabecera): cuantas de las filas de arriba difieren de
    // verdad, contando el inventario por PAR de slot en la misma posicion (index a index, no
    // "cuantos objetos distintos hay en total") - mismo criterio de "lado a lado" que el resto
    // del panel.
    public int DifferenceCount =>
        StatRows.Count(r => r.IsDifferent) + EquipmentRows.Count(r => r.IsDifferent) + InventoryA.Count(r => r.IsDifferent);

    public CompareViewModel(IReadOnlyList<CharacterListEntryViewModel> availableCharacters)
    {
        _availableCharacters = availableCharacters;
        // BUG REAL encontrado en la primera captura de esta misma ronda (COMPARE_SOLO):
        // NeedsMoreCharacters se evalua nada mas construirse este ViewModel, DENTRO del
        // constructor de MainViewModel - en ese instante Home todavia no ha terminado su
        // escaneo real (corre en un Task de fondo, ver el comentario de T-G en HomeViewModel),
        // asi que la PRIMERA lectura real de Home.Characters.Count daba 0 y el aviso "hacen
        // falta al menos 2 personajes" se quedaba congelado en pantalla PARA SIEMPRE (una
        // propiedad calculada sin OnPropertyChanged no se vuelve a leer nunca) por encima de
        // los resultados reales, aunque el escaneo terminara un instante despues con 5
        // personajes de verdad. Si la lista es una ObservableCollection (lo es siempre en la
        // app real - Home.Characters; en las pruebas puede ser una lista fija, de ahi el "as"),
        // cada cambio real dispara el refresco.
        if (availableCharacters is System.Collections.Specialized.INotifyCollectionChanged notifier)
            notifier.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NeedsMoreCharacters));
        // Redacta las filas otra vez con el idioma nuevo - los datos ya cargados (_loadedA/_loadedB)
        // no se releen de disco, solo se vuelven a redactar (mismo criterio que
        // LibraryViewModel.OnIdiomaCambiadoRefrescarTarjetas).
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, (_, _) => { if (HasBothSelected) RebuildRows(); }, "Item[]");
    }

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void Noop() { } // traga el clic DENTRO del panel, mismo patron que BackupHistoryViewModel.Noop

    partial void OnSelectedAChanged(CharacterListEntryViewModel? value) => TryLoadAndCompare();
    partial void OnSelectedBChanged(CharacterListEntryViewModel? value) => TryLoadAndCompare();

    private void TryLoadAndCompare()
    {
        OnPropertyChanged(nameof(HasBothSelected));
        StatRows.Clear();
        EquipmentRows.Clear();
        InventoryA.Clear();
        InventoryB.Clear();
        ErrorMessage = null;
        _loadedA = null;
        _loadedB = null;

        if (SelectedA == null || SelectedB == null)
        {
            OnPropertyChanged(nameof(ShowResults));
            return;
        }

        try
        {
            _loadedA = _service.Load(SelectedA.FilePath);
            // Segunda Load() SOBRE EL MISMO _service (ver el comentario de cabecera): pisa
            // EsPersonajeTModLoader con el de B, pero esta clase nunca lee service.BestPrefixes -
            // solo el prefijo YA puesto en cada objeto (GameItem.Prefix), nunca una sugerencia.
            _loadedB = _service.Load(SelectedB.FilePath);
        }
        catch (Exception ex)
        {
            // Mismo criterio de siempre: un fichero corrupto/ajeno no debe tumbar el panel, solo
            // avisar con el nombre real y la excepcion real (nunca un mensaje generico).
            ErrorMessage = LocalizationService.Instance.Format("compare_load_error", ex.Message);
            OnPropertyChanged(nameof(ShowResults));
            return;
        }

        RebuildRows();
        OnPropertyChanged(nameof(ShowResults));
    }

    private void RebuildRows()
    {
        if (_loadedA == null || _loadedB == null) return;
        var loc = LocalizationService.Instance;
        var a = _loadedA.Character;
        var b = _loadedB.Character;

        StatRows.Clear();
        AddStat("compare_stat_difficulty", AppearanceViewModel.DifficultyLabelFor(a.Difficulty), AppearanceViewModel.DifficultyLabelFor(b.Difficulty));
        AddStat("compare_stat_health", a.HealthMax.ToString(), b.HealthMax.ToString());
        AddStat("compare_stat_mana", a.ManaMax.ToString(), b.ManaMax.ToString());
        AddStat("compare_stat_extra_accessory", loc[a.ExtraAccessory ? "compare_yes" : "compare_no"], loc[b.ExtraAccessory ? "compare_yes" : "compare_no"]);
        AddStat("compare_stat_playtime", FormatPlayTime(a), FormatPlayTime(b));
        AddStat("compare_stat_pve_deaths", a.PveDeaths.ToString(), b.PveDeaths.ToString());
        AddStat("compare_stat_pvp_deaths", a.PvpDeaths.ToString(), b.PvpDeaths.ToString());
        AddStat("compare_stat_fishing_quests", a.FishingQuestsCompleted.ToString(), b.FishingQuestsCompleted.ToString());
        AddStat("compare_stat_golfer_score", a.GolferScore.ToString(), b.GolferScore.ToString());
        AddStat("compare_stat_money", FormatMoney(TotalMoney(_loadedA)), FormatMoney(TotalMoney(_loadedB)));

        EquipmentRows.Clear();
        var itemsA = _loadedA.MergedContainers["loadout0Items"];
        var dyesA = _loadedA.MergedContainers["loadout0Dyes"];
        var itemsB = _loadedB.MergedContainers["loadout0Items"];
        var dyesB = _loadedB.MergedContainers["loadout0Dyes"];
        string[] armorDyeKeys = ["slot_role_head", "slot_role_body", "slot_role_legs"];
        for (int i = 0; i < 10; i++)
        {
            string label = i < 3 ? loc[armorDyeKeys[i]] : loc.Format("compare_accessory_slot", i - 2);
            var itemA = ResolveItem(itemsA[i]);
            var itemB = ResolveItem(itemsB[i]);
            var dA = ResolveItem(dyesA[i]);
            var dB = ResolveItem(dyesB[i]);
            bool diff = !SameItem(itemsA[i], itemsB[i]) || !SameItem(dyesA[i], dyesB[i]);
            EquipmentRows.Add(new CompareEquipmentRowViewModel(label, itemA, dA, itemB, dB, diff));
        }

        var miscA = _loadedA.MergedContainers["miscEquips"];
        var miscDyesA = _loadedA.MergedContainers["miscDyes"];
        var miscB = _loadedB.MergedContainers["miscEquips"];
        var miscDyesB = _loadedB.MergedContainers["miscDyes"];
        // Orden real de Player.miscEquips (confirmado contra Player.cs decompilado, ver el mismo
        // comentario en MainViewModel.BuildContainers): mascota, mascota de luz, vagoneta,
        // montura, gancho.
        string[] miscLabelKeys = ["slot_role_vanity_pet", "slot_role_light_pet", "slot_role_cart", "slot_role_mount", "slot_role_hook"];
        for (int i = 0; i < 5; i++)
        {
            var itemA = ResolveItem(miscA[i]);
            var itemB = ResolveItem(miscB[i]);
            var dA = ResolveItem(miscDyesA[i]);
            var dB = ResolveItem(miscDyesB[i]);
            bool diff = !SameItem(miscA[i], miscB[i]) || !SameItem(miscDyesA[i], miscDyesB[i]);
            EquipmentRows.Add(new CompareEquipmentRowViewModel(loc[miscLabelKeys[i]], itemA, dA, itemB, dB, diff));
        }

        InventoryA.Clear();
        InventoryB.Clear();
        var invA = _loadedA.MergedContainers["inventory"];
        var invB = _loadedB.MergedContainers["inventory"];
        int n = Math.Min(invA.Length, invB.Length);
        for (int i = 0; i < n; i++)
        {
            bool diff = !SameItem(invA[i], invB[i]);
            InventoryA.Add(new CompareInventorySlotViewModel(i, ResolveItem(invA[i]), diff));
            InventoryB.Add(new CompareInventorySlotViewModel(i, ResolveItem(invB[i]), diff));
        }

        OnPropertyChanged(nameof(DifferenceCount));
    }

    private void AddStat(string labelKey, string valueA, string valueB) =>
        StatRows.Add(new CompareStatRowViewModel(LocalizationService.Instance[labelKey], valueA, valueB, !string.Equals(valueA, valueB, StringComparison.Ordinal)));

    // Mismos 3 campos que ya decide GameItem.ContentEquals para el resto de la app, MENOS
    // Favorited a proposito: si alguien marco el mismo objeto como favorito en un personaje y en
    // el otro no, eso no es una diferencia de BUILD (que es lo que este panel compara), asi que
    // resaltarla seria ruido.
    private static bool SameItem(GameItem a, GameItem b) =>
        a.Id == b.Id && a.Count == b.Count && a.Prefix.Equals(b.Prefix);

    private CompareItemViewModel ResolveItem(GameItem item)
    {
        if (item.IsEmpty) return CompareItemViewModel.Empty;

        string name; string? iconPath;
        if (item.IsCalamity)
        {
            var entry = _service.CalamityCatalog.BySyntheticId(item.Id);
            name = entry?.DisplayName ?? $"Calamity #{item.Id}";
            iconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
        }
        else
        {
            name = _service.VanillaCatalog.GetName(item.Id);
            iconPath = VanillaIconResolver.GetIconPath(item.Id);
        }

        return new CompareItemViewModel(iconPath, name, ResolvePrefixName(item.Prefix), item.Count, item.IsCalamity, isEmpty: false);
    }

    // Mismo mecanismo real de resolucion que ItemSlotViewModel.RefreshPrefixDisplay (vanilla via
    // VanillaPrefixCatalog, Calamity/Picaro via RoguePrefixCatalog por id sintetico), aqui de
    // solo lectura: el prefijo YA puesto en el objeto, nunca una sugerencia de "mejor prefijo".
    private string? ResolvePrefixName(ItemPrefix prefix)
    {
        if (prefix.IsNone) return null;
        string idioma = LocalizationService.Instance.Language;
        if (prefix.IsCalamity)
        {
            var entry = _service.RoguePrefixCatalog.ById(prefix.SyntheticId);
            return entry == null ? null : LocalizedContent.Pick(entry.Es, entry.En, idioma);
        }
        var vEntry = _service.VanillaPrefixCatalog.ById(prefix.VanillaId);
        return vEntry == null ? $"#{prefix.VanillaId}" : LocalizedContent.Pick(vEntry.Es, vEntry.En, idioma);
    }

    private static long TotalMoney(LoadedCharacter loaded)
    {
        // Monedas reales en Cobre (Coins[0..3] = cobre/plata/oro/platino, mismo orden que el
        // propio juego) - suma unica para poder comparar "cuanto dinero llevas" de un vistazo
        // sin obligar a sumar 4 numeros a mano.
        var coins = loaded.Character.Coins;
        long total = 0;
        long[] valorPorRanura = [1, 100, 10000, 1000000];
        for (int i = 0; i < coins.Length && i < 4; i++)
            total += (long)coins[i].Count * valorPorRanura[i];
        return total;
    }

    private static string FormatMoney(long totalCopper)
    {
        long platinum = totalCopper / 1000000;
        long gold = totalCopper / 10000 % 100;
        long silver = totalCopper / 100 % 100;
        long copper = totalCopper % 100;
        return $"{platinum}p {gold}o {silver}s {copper}c";
    }

    // Misma formula real ya usada en BackupHistoryViewModel.HorasJugadas ("Xh Ymin"/"Ymin") -
    // PlayTimeLow/High son dos UInt32 que juntos forman los ticks reales de .NET (ver
    // AppearanceViewModel.PlayHours).
    private static string FormatPlayTime(PlrCharacter c)
    {
        long totalTicks = (long)(((ulong)c.PlayTimeHigh << 32) | c.PlayTimeLow);
        var t = TimeSpan.FromTicks(totalTicks);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}min" : $"{t.Minutes}min";
    }
}
