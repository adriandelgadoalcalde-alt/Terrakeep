using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Un slot individual de un contenedor. Edicion real: prefijo (boton "mejor prefijo"), cantidad
// (editable directamente), colocar un objeto nuevo (delega en el buscador de la Libreria via
// requestPick) y vaciar el slot.
public partial class ItemSlotViewModel : ObservableObject
{
    private readonly CharacterFileService _service;
    private readonly Action<ItemSlotViewModel>? _requestPick;
    private bool _suppressCountWriteback;
    private bool _suppressIdWriteback;
    private bool _suppressPrefixIdWriteback;

    public int SlotIndex { get; }
    // Nombre real del contenedor al que pertenece este slot (Inventario/Banco/Caja fuerte/...)
    // - lo muestra el panel "Editar" compartido para saber sobre que esta editando, ya que ese
    // panel vive fuera del TabControl de contenedores (pedido explicito 1-sep-2026: el panel
    // debe "acompañar desde Equipamiento hasta Forja del Vacio").
    public string ContainerName { get; }
    // true solo para los slots que vienen de EquipmentGroupViewModel (armadura/accesorios/
    // vanidad/tintes de un loadout - genuinamente "puesto" en el personaje) - pedido explicito
    // 2-sep-2026: contorno verde real en vez de solo un hueco de separacion. Los slots de
    // Inventario/Banco/... normales se quedan en false.
    public bool IsEquipped { get; }
    // Que tipo de objeto acepta este slot en concreto - None (por defecto) = sin restriccion,
    // igual que siempre. Pedido explicito 2-sep-2026 ("los slots de tintes, gancho, vagoneta,
    // montura y mascota solo deberian poderse equipar sus respectivos items") + investigacion
    // de monedas/municion. Ver TerrasavrNative.Core.Model.SlotKind.
    public SlotKind AcceptedKind { get; }
    // Icono "fantasma" real de fondo (extraido del propio juego, ver
    // scripts/extraer-iconos-fantasma-slot.js) para cuando el slot esta vacio - null en los
    // slots sin restriccion de tipo (Inventario/Banco/...) o en los que el juego real
    // tampoco dibuja ninguno (Moneda/Municion, ver GhostIconResolver.cs).
    public string? GhostIconPath { get; }
    // 6º hueco de accesorio real (armor[8], solo funciona con el Corazón de Demonio/Cármesí,
    // ExtraAccessory en el .plr - obtenible desde Modo Experto) / 7º hueco real (armor[9],
    // automatico en Modo Maestro, sin dato en el .plr - Player.cs:11014-11035, consulta a
    // Opus sexta pasada). Puramente visual, permanente (nunca condicionado a datos del
    // personaje cargado - el 7º no tiene NINGÚN dato al que condicionarse, ver el comentario
    // de EquipmentGroupViewModel.AddSlotSet).
    public bool IsExpertAccessorySlot { get; }
    public bool IsMasterAccessorySlot { get; }
    public GameItem Item { get; private set; } = GameItem.Empty;

    // Auditoria de Opus, E-3: "un usuario que no conoce el juego no sabe que el slot 3 es
    // 'Accesorio 1'" - respaldado por el original real (app.TabEquips.updateLang,
    // locClass[3][4] = "Helmet/Shirt/Pants/Accessory $1"). Derivado de AcceptedKind (ya
    // restringido por slot real, ver SlotKind) + SlotIndex para numerar accesorios 1-7 -
    // null para slots sin restriccion real (Inventario/Banco/...), donde no hay ningun "rol"
    // que anunciar.
    public string? SlotRoleLabel => AcceptedKind switch
    {
        SlotKind.ArmorHead => "Cabeza",
        SlotKind.ArmorBody => "Cuerpo",
        SlotKind.ArmorLegs => "Piernas",
        SlotKind.Accessory => $"Accesorio {SlotIndex - 2}",
        SlotKind.Dye => "Tinte",
        SlotKind.Mount => "Montura",
        SlotKind.Hook => "Gancho",
        SlotKind.Cart => "Vagoneta",
        SlotKind.VanityPet => "Mascota",
        SlotKind.LightPet => "Mascota de luz",
        SlotKind.Coin => "Moneda",
        SlotKind.Ammo => "Munición",
        _ => null,
    };

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _rejectionMessage;
    [ObservableProperty] private int _count;
    [ObservableProperty] private bool _isCalamity;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _prefixDisplay = string.Empty;
    [ObservableProperty] private bool _hasBestPrefixSuggestion;
    [ObservableProperty] private string? _iconPath;
    [ObservableProperty] private string? _statsTooltip;
    // Auditoria de Opus, D-3: color REAL de rareza de Terraria (VanillaRarityColorCatalog) para
    // pintar el NOMBRE del objeto, en vez de "Rareza N" como texto plano - null para Calamity
    // (rarezas propias, no investigadas esta pasada) o rareza sin color real conocido.
    [ObservableProperty] private System.Windows.Media.Brush? _rarityBrush;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _itemId;
    [ObservableProperty] private int _prefixId;
    // Auditoria de Opus, Bloque 3 (T-14): "el usuario no tiene ninguna confirmacion visual de
    // que su edicion se aplico" - flash real de un instante en el propio slot, mismo lenguaje
    // visual que el banner de "Guardado" (DataTrigger.EnterActions, dispara solo al pasar de
    // False a True) pero por slot, no global. Disparado por MainViewModel SOLO en una edicion
    // real de usuario (nunca durante la carga de un personaje) - ver TriggerEditFlash().
    [ObservableProperty] private bool _justEdited;

    public bool IsNotEmpty => !IsEmpty;
    // El tooltip normal solo tiene sentido con el slot lleno (ver comentario mas abajo en
    // MainWindow.xaml), pero el 6º/7º hueco de accesorio quiere explicarse TAMBIEN vacio -
    // es precisamente cuando mas hace falta (por que hay una franja de color en un slot sin
    // nada dentro).
    public bool ShowTooltip => IsNotEmpty || IsExpertAccessorySlot || IsMasterAccessorySlot;
    partial void OnIsEmptyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotEmpty));
        OnPropertyChanged(nameof(ShowCount));
    }

    // Tarjeta compacta (pregunta a Opus sobre el diseño, 2-sep-2026): sin sitio para un
    // TextBox de cantidad visible siempre, solo un numero pequeño y unicamente cuando aporta
    // algo real (1 unidad no necesita rotularse, igual que hace el propio Terraria).
    public bool ShowCount => !IsEmpty && Count > 1;

    public ItemSlotViewModel(CharacterFileService service, int slotIndex, string containerName, GameItem item, Action<ItemSlotViewModel>? requestPick = null, bool isEquipped = false,
        SlotKind acceptedKind = SlotKind.None, string? ghostIcon = null, bool isExpertAccessorySlot = false, bool isMasterAccessorySlot = false)
    {
        _service = service;
        SlotIndex = slotIndex;
        ContainerName = containerName;
        _requestPick = requestPick;
        IsEquipped = isEquipped;
        AcceptedKind = acceptedKind;
        GhostIconPath = SlotGhostIconResolver.GetIconPath(ghostIcon);
        IsExpertAccessorySlot = isExpertAccessorySlot;
        IsMasterAccessorySlot = isMasterAccessorySlot;
        UpdateFrom(item);
    }

    // true si este objeto (vanilla o Calamity) encaja en AcceptedKind - AcceptedKind=None
    // acepta cualquier cosa, igual que siempre. Los objetos de Calamity SIEMPRE se aceptan
    // (consulta a Opus, sexta pasada: CalamityCatalogEntryData no tiene ninguno de los campos
    // reales que hacen falta para validar esto - ammo/mountType/buffType/dye/shoot - validar
    // estricto bloquearia TODAS las monturas/mascotas/tintes reales de Calamity; "desconocido
    // = permitir").
    // Armadura/Accesorio (ampliacion 2-sep-2026) es el UNICO grupo donde Calamity SI tiene un
    // campo real utilizable: CalamityCatalogEntryData.Category ("Armor/..."/"Accessories...")
    // ya viene del catalog.json real y ya se usaba para el arbol de la Libreria - a diferencia
    // de ammo/mountType/buffType/dye/shoot, que no existen en absoluto para Calamity. Bug real
    // reportado 2-sep-2026 ("la armadura me deja colocarla en los huecos de accesorios"): la
    // regla "Calamity siempre pasa" (correcta para los otros 8 tipos, sin dato real posible)
    // se aplicaba tambien aqui por error, donde SI hay dato real y no hacia falta la excepcion.
    private const SlotKind ArmorAccessoryKinds = SlotKind.ArmorHead | SlotKind.ArmorBody | SlotKind.ArmorLegs | SlotKind.Accessory;

    public bool AcceptsItem(int itemId)
    {
        if (AcceptedKind == SlotKind.None || itemId <= 0) return true;
        if (itemId >= CalamityIds.ItemIdBase)
        {
            if ((AcceptedKind & ArmorAccessoryKinds) == 0) return true; // sin dato real, "desconocido = permitir"
            string? category = _service.CalamityCatalog.BySyntheticId(itemId)?.Category;
            if (category == null) return true; // id sintetico sin entrada real - no rechazar a ciegas
            if (category.StartsWith("Accessories", StringComparison.Ordinal))
                return (AcceptedKind & SlotKind.Accessory) != 0;
            if (category.StartsWith("Armor", StringComparison.Ordinal))
                return (AcceptedKind & (SlotKind.ArmorHead | SlotKind.ArmorBody | SlotKind.ArmorLegs)) != 0;
            return false; // arma/pocion/material real de Calamity - nunca es armadura ni accesorio
        }
        return (_service.VanillaSlotKinds.GetKind(itemId) & AcceptedKind) != 0;
    }

    private string BuildRejectionMessage() => AcceptedKind switch
    {
        SlotKind.Ammo => "Este slot solo acepta munición.",
        SlotKind.Coin => "Este slot solo acepta monedas.",
        SlotKind.Dye => "Este slot solo acepta tintes.",
        SlotKind.Hook => "Este slot solo acepta ganchos.",
        SlotKind.Mount => "Este slot solo acepta monturas.",
        SlotKind.Cart => "Este slot solo acepta vagonetas.",
        SlotKind.VanityPet => "Este slot solo acepta mascotas.",
        SlotKind.LightPet => "Este slot solo acepta mascotas de luz.",
        SlotKind.ArmorHead => "Este slot solo acepta cascos/tocados.",
        SlotKind.ArmorBody => "Este slot solo acepta petos/túnicas.",
        SlotKind.ArmorLegs => "Este slot solo acepta grebas/pantalones.",
        SlotKind.Accessory => "Este slot solo acepta accesorios.",
        _ => "Este objeto no encaja en este slot.",
    };

    public void UpdateFrom(GameItem item)
    {
        RejectionMessage = null;
        Item = item;
        IsEmpty = item.IsEmpty;
        IsCalamity = item.IsCalamity;

        _suppressCountWriteback = true;
        Count = item.Count;
        _suppressCountWriteback = false;

        _suppressIdWriteback = true;
        ItemId = item.IsEmpty ? 0 : item.Id;
        _suppressIdWriteback = false;

        _suppressPrefixIdWriteback = true;
        // Campo "Indice (id)" real de TabEdit - solo representa el byte de prefijo vanilla
        // (incluye 85-97, los reales de invocacion de Calamity, que se escriben igual como
        // byte). Un prefijo Rogue autentico de Calamity (modPrefixMod/modPrefixName, ids
        // sinteticos >= 10000) no cabe en un campo numerico de un byte - se deja en 0 aqui
        // (se edita solo desde la rejilla de botones, no a mano).
        PrefixId = item.Prefix.IsCalamity ? 0 : item.Prefix.VanillaId;
        _suppressPrefixIdWriteback = false;

        if (item.IsEmpty)
        {
            DisplayName = string.Empty;
            PrefixDisplay = string.Empty;
            HasBestPrefixSuggestion = false;
            IconPath = null;
            StatsTooltip = null;
            RarityBrush = null;
            OnPropertyChanged(nameof(ShowCount));
            return;
        }

        if (item.IsCalamity)
        {
            var entry = _service.CalamityCatalog.BySyntheticId(item.Id);
            DisplayName = entry?.DisplayName ?? $"Calamity #{item.Id}";
            IconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            RarityBrush = null;
        }
        else
        {
            DisplayName = _service.VanillaCatalog.GetName(item.Id);
            IconPath = VanillaIconResolver.GetIconPath(item.Id);
            var rarityColor = VanillaRarityColorCatalog.Get(_service.VanillaStats.Get(item.Id)?.Rare);
            RarityBrush = rarityColor is { } c
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B))
                : null;
        }

        StatsTooltip = ItemStatsFormatter.Format(item.IsCalamity, item.Id, _service.TooltipCatalogs, item.Prefix);

        RefreshPrefixDisplay();

        var suggestion = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        HasBestPrefixSuggestion = suggestion.HasValue && !suggestion.Value.Equals(item.Prefix);
        OnPropertyChanged(nameof(ShowCount));
    }

    // Coloca un objeto nuevo del catalogo (id real vanilla, o sintetico de Calamity) en este
    // slot - cantidad 1, sin datos de Calamity heredados (es un objeto nuevo). El mejor
    // prefijo real (vanilla + Calamity/Rogue) se aplica automaticamente si el objeto admite
    // prefijo - pedido explicito del usuario (1-sep-2026): "siempre que pongas un objeto...
    // sobre todo armas, el mejor prefijo se ha de poner de manera automática". Se puede
    // cambiar despues a mano igual que con cualquier objeto ya puesto (boton de editar
    // prefijo).
    public void PlaceItem(int id)
    {
        if (!AcceptsItem(id))
        {
            RejectionMessage = BuildRejectionMessage();
            return;
        }
        var item = new GameItem { Id = id, Count = 1 };
        var suggestion = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        if (suggestion.HasValue) item.Prefix = suggestion.Value;
        UpdateFrom(item);
    }

    // Arrastrar y soltar un slot sobre otro (pedido explicito 1-sep-2026: "se puede arrastar
    // para poder ir poniendo en el inventario o en accesorios") - intercambia el contenido
    // completo de los dos slots (prefijo/cantidad/favorito/datos de Calamity incluidos), igual
    // que el propio juego al arrastrar un item sobre otro slot ocupado. El gesto de arrastre en
    // si vive en el code-behind (MainWindow.xaml.cs), que es quien conoce los eventos de raton
    // WPF - esto solo hace el intercambio de datos una vez decidido.
    public void SwapWith(ItemSlotViewModel other)
    {
        var temp = Item;
        UpdateFrom(other.Item);
        other.UpdateFrom(temp);
    }

    [RelayCommand]
    private void Clear() => UpdateFrom(GameItem.Empty);

    [RelayCommand]
    private void ChooseFromLibrary() => _requestPick?.Invoke(this);

    // Auditoria de Opus, Bloque 3 (T-14). El reset a False tras un instante (para poder
    // re-disparar el flash en la SIGUIENTE edicion, un DataTrigger de WPF solo entra en accion
    // al pasar de False a True) via Task.Delay - sin DispatcherTimer por instancia (un timer
    // real por cada uno de los cientos de slots reales seria un desperdicio real, esto solo usa
    // el pool de hilos un instante y solo cuando hay una edicion de verdad). El False->True
    // explicito de las dos primeras lineas (en vez de solo "=true") es real, no cosmetico: si
    // el usuario edita el MISMO slot dos veces en menos de 450ms, True->True seria un no-op
    // para WPF (no se re-entra el trigger) y el segundo flash no se veria.
    public void TriggerEditFlash()
    {
        JustEdited = false;
        JustEdited = true;
        _ = ResetEditFlashAsync();
    }

    private async System.Threading.Tasks.Task ResetEditFlashAsync()
    {
        await System.Threading.Tasks.Task.Delay(450);
        JustEdited = false;
    }

    partial void OnCountChanged(int value)
    {
        if (_suppressCountWriteback || Item.IsEmpty) return;
        // Un objeto real siempre tiene al menos 1 unidad - 0 significaria vaciar el slot,
        // para eso ya esta el boton "Vaciar" explicito.
        int clamped = Math.Clamp(value, 1, 9999);
        Item.Count = clamped;
        if (clamped != value)
        {
            _suppressCountWriteback = true;
            Count = clamped;
            _suppressCountWriteback = false;
        }
        OnPropertyChanged(nameof(ShowCount));
    }

    private void RefreshPrefixDisplay()
    {
        _suppressPrefixIdWriteback = true;
        PrefixId = Item.Prefix.IsCalamity ? 0 : Item.Prefix.VanillaId;
        _suppressPrefixIdWriteback = false;

        var prefix = Item.Prefix;
        if (prefix.IsCalamity)
        {
            var prefixEntry = _service.RoguePrefixCatalog.ById(prefix.SyntheticId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? string.Empty;
        }
        else if (!prefix.IsNone)
        {
            var prefixEntry = _service.VanillaPrefixCatalog.ById(prefix.VanillaId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? $"Prefijo #{prefix.VanillaId}";
        }
        else
        {
            PrefixDisplay = string.Empty;
        }
    }

    [RelayCommand]
    private void ApplyBestPrefix()
    {
        var suggestion = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        if (suggestion == null) return;

        SetPrefix(suggestion.Value);
    }

    // Aplica un prefijo elegido a mano (picker categorizado, hueco #2 de la auditoria - antes
    // solo se podia usar el "mejor prefijo" auto-sugerido). Vale tanto para el picker como
    // para ApplyBestPrefix de arriba.
    public void SetPrefix(ItemPrefix prefix)
    {
        Item.Prefix = prefix;
        RefreshPrefixDisplay();
        var suggestion = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        HasBestPrefixSuggestion = suggestion.HasValue && !suggestion.Value.Equals(Item.Prefix);
    }

    // Campo "Indice" editable (equivalente real de fdIndex en TabEdit) - escribir un id
    // nuevo cambia el objeto del slot igual que elegirlo desde la Libreria (mismo criterio,
    // mejor prefijo automatico incluido via PlaceItem).
    partial void OnItemIdChanged(int value)
    {
        if (_suppressIdWriteback || value <= 0 || value == Item.Id) return;
        // Campo "Indice" a mano - el unico camino de los tres (Libreria filtrada/arrastrar y
        // soltar/Indice) donde el silencio confunde de verdad (el usuario escribe y no pasa
        // nada) - revertir el numero escrito y avisar en el propio panel Editar (consulta a
        // Opus, sexta pasada).
        if (!AcceptsItem(value))
        {
            RejectionMessage = BuildRejectionMessage();
            _suppressIdWriteback = true;
            ItemId = Item.IsEmpty ? 0 : Item.Id;
            _suppressIdWriteback = false;
            return;
        }
        PlaceItem(value);
    }

    // Campo "Prefijo" editable a mano por numero (equivalente real de fdPrefix) - separado
    // de los botones de la rejilla, que llaman a SetPrefix directamente con el ItemPrefix ya
    // resuelto (vanilla o Calamity synthetic).
    partial void OnPrefixIdChanged(int value)
    {
        if (_suppressPrefixIdWriteback || Item.IsEmpty) return;
        SetPrefix(ItemPrefix.Vanilla((byte)Math.Clamp(value, 0, 255)));
    }
}
