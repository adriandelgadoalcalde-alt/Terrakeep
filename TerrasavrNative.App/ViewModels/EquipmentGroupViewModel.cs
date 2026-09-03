using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Una opcion de un selector pequeño (loadout o vista) - mismo patron que
// PrefixMetaButtonViewModel/PrefixGroupButtonViewModel (Label + IsSelected).
public sealed partial class EquipmentOptionViewModel : ObservableObject
{
    public string Label { get; }
    public int Value { get; }

    [ObservableProperty] private bool _isSelected;

    // Auditoria de Opus, A-1: "para saber si el Banco esta lleno hay que pulsar su pildora y
    // contar". container es opcional (null = pildoras de Loadout/Vista en Equipamiento, que no
    // tienen un "recuento" real que mostrar) - cuando SI viene un contenedor (las 4 pildoras de
    // Almacenes), DisplayLabel se recalcula sola y en vivo (P5) suscribiendose una vez a cada
    // slot real, sin que nadie de fuera tenga que avisar.
    private readonly ContainerViewModel? _container;

    public EquipmentOptionViewModel(string label, int value, ContainerViewModel? container = null)
    {
        Label = label;
        Value = value;
        _container = container;
        if (container != null)
            foreach (var slot in container.Slots)
                slot.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ItemSlotViewModel.IsEmpty)) OnPropertyChanged(nameof(DisplayLabel)); };
    }

    public string DisplayLabel => _container == null
        ? Label
        : $"{Label} ({_container.Slots.Count(s => !s.IsEmpty)}/{_container.Slots.Count})";
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
    private readonly CharacterFileService _service;
    private readonly Dictionary<(int Loadout, EquipmentKind Kind), ContainerViewModel> _byKey = new();

    // Auditoria de Opus, E-4: "el personaje tiene defensa real sumable... ya sabemos mostrar
    // 'Con el set completo: ...' pero es el bono HIPOTETICO, no el que de verdad esta activo
    // ahora mismo". Ambos se recalculan en vivo (P5) - defensa suma Armadura+Accesorios REALES
    // del loadout seleccionado (vanilla via VanillaStats, Calamity via CalamityCatalog, ya
    // extraida), el bono de set usa VanillaArmorSetCatalog.BonusForEquipped (ya existia, sin
    // usar) contra las 3 piezas de cabeza/cuerpo/piernas puestas de verdad.
    [ObservableProperty] private int _totalDefense;
    [ObservableProperty] private string? _activeSetBonusText;

    public IReadOnlyList<ContainerViewModel> AllContainers { get; }

    // Atajo directo al equipo puesto del loadout 0 (Puesto/principal). Bd-b (segunda auditoria
    // de Opus, Fable): AutoEquip usaba ESTE atajo, ignorando el loadout que el usuario tenia
    // seleccionado de verdad ("sorpresa silenciosa si estas mirando el Loadout 2") - ahora usa
    // CurrentItems (ver abajo), que SI seguia al loadout seleccionado. Sin otro consumidor real
    // en produccion tras ese arreglo (verificado, ni el XAML ni ningun ViewModel lo referencian
    // ya) - se deja tal cual porque los tests headless ya lo usan para fijar el loadout 0 de
    // forma explicita, no porque haga falta en la app real.
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

    // Auditoria de Opus, E-2: "las 3 vistas (Armadura/Vanidad/Tintes) se podrian ver a la vez
    // con sitio real" - los 3 contenedores del loadout ACTUAL, sin importar SelectedKind (que
    // sigue siendo el que manda en Compacto/Normal, un unico panel + pildoras de siempre). Solo
    // se leen cuando SizeClass.Amplio los muestra de verdad (ver MainWindow.xaml) - viven aqui
    // en vez de calcularse en el XAML porque _byKey es privado.
    public ContainerViewModel CurrentItems => _byKey[(SelectedLoadout, EquipmentKind.Items)];
    public ContainerViewModel CurrentSocial => _byKey[(SelectedLoadout, EquipmentKind.Social)];
    public ContainerViewModel CurrentDyes => _byKey[(SelectedLoadout, EquipmentKind.Dyes)];

    public EquipmentGroupViewModel(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot,
        Dictionary<string, GameItem[]> mergedContainers, int realLoadoutCount)
    {
        _service = service;
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

        // Auditoria de Opus, E-4: suscripcion real a cada slot de Armadura/Accesorios (de
        // TODOS los loadouts, no solo el seleccionado - cambiar de Puesto/1/2/3 tambien debe
        // reflejar la defensa/bono real de ESE loadout) para recalcular en vivo (P5).
        // Segunda auditoria de Opus (Fable), B-6 - BUG REAL encontrado y arreglado: solo se
        // escuchaba IsEmpty, que NO cambia al SUSTITUIR una pieza ya puesta por otra (el caso
        // normal absoluto: arrastrar desde la Libreria sobre un slot ya ocupado, o
        // "Auto-equipar" sobre un personaje ya vestido) - UpdateFrom hace IsEmpty=item.IsEmpty,
        // que pasa de false a false, y [ObservableProperty] no emite nada en ese caso. "Defensa
        // total" y el bono de set se quedaban congelados en el valor de la pieza ANTERIOR,
        // mintiendo justo en el momento en que el usuario esta comparando armaduras. Mismo
        // criterio de exclusion ya establecido en MainViewModel.HookSlotEditing (todo cambio
        // real salvo IsSelected/JustEdited, que son puro estado de UI) en vez de perseguir
        // propiedad por propiedad.
        foreach (var ((loadout, kind), container) in _byKey)
        {
            if (kind != EquipmentKind.Items) continue;
            foreach (var slot in container.Slots)
                // H3-03 (tercera auditoria, Fable): RejectionMessage tambien excluido, por
                // coherencia con el mismo criterio de arriba - un intento de colocacion
                // rechazado no cambia ninguna defensa real, recalcularla igual seria trabajo sin
                // sentido y romperia la simetria con MainViewModel.HookSlotEditing/BuffsViewModel.
                slot.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(ItemSlotViewModel.IsSelected) or nameof(ItemSlotViewModel.JustEdited) or nameof(ItemSlotViewModel.RejectionMessage)) return;
                    RecomputeDefenseAndBonus();
                };
        }
        RecomputeDefenseAndBonus();
    }

    partial void OnSelectedLoadoutChanged(int value)
    {
        RecomputeDefenseAndBonus();
        OnPropertyChanged(nameof(CurrentItems));
        OnPropertyChanged(nameof(CurrentSocial));
        OnPropertyChanged(nameof(CurrentDyes));
    }

    private void RecomputeDefenseAndBonus()
    {
        var items = _byKey[(SelectedLoadout, EquipmentKind.Items)].Slots;
        int total = 0;
        foreach (var slot in items)
        {
            if (slot.IsEmpty) continue;
            total += slot.IsCalamity
                ? _service.CalamityCatalog.BySyntheticId(slot.Item.Id)?.Stats?.Defense ?? 0
                : _service.VanillaStats.Get(slot.Item.Id)?.Defense ?? 0;
            // H3-08 (tercera auditoria de Opus, Fable): "Defensa total" ignoraba la defensa de
            // los prefijos de accesorio reales (Warding/Guarding/Menacing/Hardy/Armored, +1..+4
            // segun Player.GrantPrefixBenefits) - un personaje real de endgame con varios
            // accesorios "Protección" llevaba defensa real que el panel nunca contaba. Solo
            // prefijos vanilla (Calamity/Rogue no tienen datos reales en este catalogo, mismo
            // criterio de "desconocido = 0" ya usado en el resto de la app).
            if (!slot.Item.Prefix.IsCalamity)
                total += _service.PrefixEffects.GetDefenseBonus(slot.Item.Prefix.VanillaId);
        }
        TotalDefense = total;

        // BonusForEquipped real (VanillaArmorSetCatalog, ya existia sin usar) solo entiende
        // ids vanilla - una pieza de Calamity en cabeza/cuerpo/piernas nunca forma un set
        // vanilla real, se pasa -1 (id imposible) para que no case por error con nada.
        int headId = !items[0].IsEmpty && !items[0].IsCalamity ? items[0].Item.Id : -1;
        int bodyId = !items[1].IsEmpty && !items[1].IsCalamity ? items[1].Item.Id : -1;
        int legsId = !items[2].IsEmpty && !items[2].IsCalamity ? items[2].Item.Id : -1;
        ActiveSetBonusText = _service.VanillaArmorSets.BonusForEquipped(headId, bodyId, legsId)?.Text
            ?? ActiveCalamitySetBonusText(items[0], items[1], items[2]);
    }

    // Segunda auditoria de Opus (Fable), B-6/F3: el bono de set de Calamity YA se extrae por
    // pieza (CalamityCatalogEntry.SetBonus, commit a936124) y ya se muestra en el tooltip de
    // CADA pieza suelta ("Con el set completo: ...") - pero "Defensa total"/"Bono activo" solo
    // entendia sets vanilla, dejando el bono de Calamity mudo aunque el caso mas frecuente en
    // este editor sea justo una armadura de Calamity. Real, no inventado: las 3 piezas
    // (cabeza/cuerpo/piernas) de Calamity llevan el MISMO texto de `SetBonus` cuando forman un
    // set real (extraido por set, no por pieza suelta) - si las 3 estan puestas, son de
    // Calamity y comparten exactamente ese texto, el set esta activo de verdad.
    private string? ActiveCalamitySetBonusText(ItemSlotViewModel head, ItemSlotViewModel body, ItemSlotViewModel legs)
    {
        if (head.IsEmpty || body.IsEmpty || legs.IsEmpty) return null;
        if (!head.IsCalamity || !body.IsCalamity || !legs.IsCalamity) return null;

        string? headBonus = _service.CalamityCatalog.BySyntheticId(head.Item.Id)?.SetBonus;
        string? bodyBonus = _service.CalamityCatalog.BySyntheticId(body.Item.Id)?.SetBonus;
        string? legsBonus = _service.CalamityCatalog.BySyntheticId(legs.Item.Id)?.SetBonus;
        if (string.IsNullOrEmpty(headBonus) || headBonus != bodyBonus || headBonus != legsBonus) return null;

        return headBonus;
    }

    // Indices reales dentro de los 10 slots de Items/Social (Player.armor[0..9] real):
    // 0-2 = cabeza/cuerpo/piernas, 3-9 = 7 accesorios - 8=6º (Corazón de Demonio/Experto),
    // 9=7º (Modo Maestro). Ghost real por indice (ver ItemSlot.cs, consulta a Opus sexta
    // pasada): armor_head/body/legs (Items) o vanity_head/body/legs (Social) para 0-2,
    // accessory (Items) o accessory_vanity (Social) para 3-9 (los 7 comparten el mismo ghost
    // real, el juego no distingue "accesorio normal" de "6º/7º" con un icono distinto).
    private static readonly string[] ItemsGhostByIndex =
        ["armor_head", "armor_body", "armor_legs", "accessory", "accessory", "accessory", "accessory", "accessory", "accessory", "accessory"];
    private static readonly string[] SocialGhostByIndex =
        ["vanity_head", "vanity_body", "vanity_legs", "accessory_vanity", "accessory_vanity", "accessory_vanity", "accessory_vanity", "accessory_vanity", "accessory_vanity", "accessory_vanity"];

    // Restriccion real por indice (ampliacion 2-sep-2026, misma sexta pasada: "las armaduras
    // y los accesorios, si los quiero [restringidos] arriba") - vale IGUAL para Items y
    // Social, un objeto de vanidad tiene el MISMO campo real headSlot/bodySlot/legSlot/
    // accessory que su version funcional (vanity=true no cambia el equip type).
    private static readonly SlotKind[] ItemsKindByIndex =
        [SlotKind.ArmorHead, SlotKind.ArmorBody, SlotKind.ArmorLegs,
         SlotKind.Accessory, SlotKind.Accessory, SlotKind.Accessory, SlotKind.Accessory, SlotKind.Accessory, SlotKind.Accessory, SlotKind.Accessory];

    private void AddSlotSet(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot,
        int loadout, EquipmentKind kind, string displayName, GameItem[] items)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
        {
            string? ghost = kind switch
            {
                EquipmentKind.Items => i < ItemsGhostByIndex.Length ? ItemsGhostByIndex[i] : null,
                EquipmentKind.Social => i < SocialGhostByIndex.Length ? SocialGhostByIndex[i] : null,
                EquipmentKind.Dyes => "dye",
                _ => null,
            };
            var slotKind = kind switch
            {
                EquipmentKind.Items or EquipmentKind.Social => i < ItemsKindByIndex.Length ? ItemsKindByIndex[i] : SlotKind.None,
                EquipmentKind.Dyes => SlotKind.Dye,
                _ => SlotKind.None,
            };
            // El 6º/7º hueco de accesorio (indices 8/9) es puramente visual y solo tiene
            // sentido marcarlo en la rejilla real de Armadura/Accesorios (pedido explicito:
            // "en la rejilla de armadura y accesorios") - no en Vanidad/Tintes, para no
            // repetir la misma señal tres veces sin que el usuario la haya pedido ahi.
            bool isExpert = kind == EquipmentKind.Items && i == 8;
            bool isMaster = kind == EquipmentKind.Items && i == 9;
            slots.Add(new ItemSlotViewModel(service, i, displayName, items[i], requestPickForSlot, isEquipped: true,
                acceptedKind: slotKind, ghostIcon: ghost, isExpertAccessorySlot: isExpert, isMasterAccessorySlot: isMaster));
        }
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
