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
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly CharacterFileService _service;
    private readonly Action<ItemSlotViewModel>? _requestPick;
    // H5-01 (quinta auditoria de Opus): unico punto real donde ItemSlotViewModel avisa "mi
    // contenido cambio de verdad" (antes, despues) - quien construye el slot decide que hacer
    // con eso (MainViewModel.HookSlotEditing empuja al UndoStack real, con el mismo guardia
    // _suppressDirty ya existente para no grabar nada durante la carga de un personaje). Esta
    // clase no conoce el UndoStack ni ningun otro consumidor - mismo criterio ya establecido con
    // _requestPick.
    private readonly Action<ItemSlotViewModel, GameItem, GameItem>? _onItemChanged;
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
    // OBJ-05 (oleada de pruebas de Objetos, 6-sep-2026) - BUG REAL medido con un round-trip de
    // verdad sobre una copia de un personaje real: el byte de favorito NO existe en todos los
    // contenedores del .plr. `PlrContainerSpec.FavFlagMinVersion` es el dato real (0 = ese
    // contenedor nunca lo lleva; 145/269/322 = solo desde esa version) y viene confirmado contra
    // el juego: `Player.cs:55497-55499` (Terraria 1.4.5.8 decompilado) escribe el banco con
    // type+stack+prefix y NADA MAS. O sea que Banco/Caja fuerte/Fragua/Mascotas/Tintes de
    // montura NO pueden guardar un favorito, y los loadouts solo desde la version 322.
    //
    // La app, en cambio, ofrecia la estrella en TODOS los slots por igual: el usuario la
    // pulsaba, la veia encenderse, guardaba... y al recargar habia desaparecido, sin ningun
    // aviso. Un cambio que se pierde en silencio es peor que uno que no se puede hacer, asi que
    // donde el formato no lo soporta el control simplemente no se ofrece (ver MainWindow.xaml).
    public bool SupportsFavorite { get; }
    public GameItem Item { get; private set; } = GameItem.Empty;

    // Auditoria de Opus, E-3: "un usuario que no conoce el juego no sabe que el slot 3 es
    // 'Accesorio 1'" - respaldado por el original real (app.TabEquips.updateLang,
    // locClass[3][4] = "Helmet/Shirt/Pants/Accessory $1"). Derivado de AcceptedKind (ya
    // restringido por slot real, ver SlotKind) + SlotIndex para numerar accesorios 1-7 -
    // null para slots sin restriccion real (Inventario/Banco/...), donde no hay ningun "rol"
    // que anunciar.
    public string? SlotRoleLabel => AcceptedKind switch
    {
        SlotKind.ArmorHead => LocalizationService.Instance["slot_role_head"],
        SlotKind.ArmorBody => LocalizationService.Instance["slot_role_body"],
        SlotKind.ArmorLegs => LocalizationService.Instance["slot_role_legs"],
        SlotKind.Accessory => LocalizationService.Instance.Format("slot_role_accessory", SlotIndex - 2),
        SlotKind.Dye => LocalizationService.Instance["char_dye_label"],
        SlotKind.Mount => LocalizationService.Instance["slot_role_mount"],
        SlotKind.Hook => LocalizationService.Instance["slot_role_hook"],
        SlotKind.Cart => LocalizationService.Instance["slot_role_cart"],
        SlotKind.VanityPet => LocalizationService.Instance["slot_role_vanity_pet"],
        SlotKind.LightPet => LocalizationService.Instance["slot_role_light_pet"],
        SlotKind.Coin => LocalizationService.Instance["slot_role_coin"],
        SlotKind.Ammo => LocalizationService.Instance["slot_role_ammo"],
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

    // Ronda de idioma del 6-sep-2026: antes esto era `[ObservableProperty] private string?
    // _statsTooltip`, un texto YA redactado (en español a fuego, ver ItemStatsFormatter) que se
    // congelaba en el idioma que hubiera al colocar el objeto. Ahora se guardan los DATOS reales
    // (ItemStatsInfo, sin idioma) y la frase se redacta en cada lectura con el idioma activo -
    // asi el tooltip cambia de idioma en vivo sin que nadie tenga que acordarse de recalcularlo.
    private ItemStatsInfo? _stats;
    public string? StatsTooltip => ItemStatsTextBuilder.Build(_stats);
    // Auditoria de Opus, D-3: color REAL de rareza de Terraria (VanillaRarityColorCatalog) para
    // pintar el NOMBRE del objeto, en vez de "Rareza N" como texto plano - null para Calamity
    // (rarezas propias, no investigadas esta pasada) o rareza sin color real conocido.
    [ObservableProperty] private System.Windows.Media.Brush? _rarityBrush;
    [ObservableProperty] private bool _isSelected;
    // H5-06 (quinta auditoria de Opus): GameItem.Favorited se guardaba/leia de punta a punta
    // (PlrBodySerializer) pero la capa App nunca lo mencionaba - ni marca, ni control, ni
    // filtro. Espejo real de Item.Favorited, igual que el resto de propiedades de esta clase.
    [ObservableProperty] private bool _isFavorited;
    // L-e (segunda auditoria de Opus, Fable): "el mensaje de rechazo no se limpia nunca -
    // escribir un id invalido deja el aviso rojo colgado indefinidamente, incluso cambiando de
    // slot y volviendo". Se limpia al cambiar de seleccion (en cualquiera de los dos sentidos -
    // deja de estar activo el aviso de ESTE slot en cuanto deja de ser el que se esta editando,
    // y no reaparece solo por volver a seleccionarlo).
    partial void OnIsSelectedChanged(bool value) => RejectionMessage = null;
    [ObservableProperty] private int _itemId;
    [ObservableProperty] private int _prefixId;
    // Auditoria de Opus, Bloque 3 (T-14): "el usuario no tiene ninguna confirmacion visual de
    // que su edicion se aplico" - flash real de un instante en el propio slot, mismo lenguaje
    // visual que el banner de "Guardado" (DataTrigger.EnterActions, dispara solo al pasar de
    // False a True) pero por slot, no global. Disparado por MainViewModel SOLO en una edicion
    // real de usuario (nunca durante la carga de un personaje) - ver TriggerEditFlash().
    [ObservableProperty] private bool _justEdited;

    // H5-05 (quinta auditoria de Opus): "¿Dónde lo tengo?" - mismo patron real ya validado por
    // X-c para los NPCs del mapa (resaltar los que coinciden en vez de ocultar los que no,
    // nunca desaparecen del todo). true por defecto (sin busqueda activa, todos coinciden) -
    // MainViewModel.ApplyWhereIsItFilter lo recalcula sobre TODOS los slots reales del
    // personaje en cada busqueda.
    [ObservableProperty] private bool _isSearchMatch = true;

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
        SlotKind acceptedKind = SlotKind.None, string? ghostIcon = null, bool isExpertAccessorySlot = false, bool isMasterAccessorySlot = false,
        Action<ItemSlotViewModel, GameItem, GameItem>? onItemChanged = null, bool supportsFavorite = true)
    {
        _service = service;
        SupportsFavorite = supportsFavorite;
        SlotIndex = slotIndex;
        ContainerName = containerName;
        _requestPick = requestPick;
        IsEquipped = isEquipped;
        AcceptedKind = acceptedKind;
        GhostIconPath = SlotGhostIconResolver.GetIconPath(ghostIcon);
        IsExpertAccessorySlot = isExpertAccessorySlot;
        IsMasterAccessorySlot = isMasterAccessorySlot;
        _onItemChanged = onItemChanged;
        UpdateFrom(item);
        // Ronda de idioma del 6-sep-2026: StatsTooltip se redacta al leerlo, asi que al cambiar
        // de idioma solo hace falta avisar de que hay que volver a leerlo. Evento DEBIL, mismo
        // motivo real que LocalizedContentViewModel: LocalizationService.Instance es un singleton
        // que vive lo que la aplicacion y estos slots (cientos por personaje, y `dotnet test`
        // construye cientos de MainViewModel) no.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Las CUATRO cosas del slot que dependen del idioma. SlotRoleLabel y PrefixDisplay se
    // descubrieron gracias a abrir el popup de verdad (OBJ-STATS-IDIOMA del arnes): el
    // ToolTip del XAML es UN objeto vivo, no se reconstruye al reabrirlo, asi que un binding a
    // una propiedad normal se queda con el idioma que hubiera la PRIMERA vez que se abrio - con
    // la app en ingles seguia diciendo "Cabeza" y "Prefix: Legendario".
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StatsTooltip));
        OnPropertyChanged(nameof(SlotRoleLabel));
        RefreshPrefixDisplay();
        RefreshDisplayName();
    }

    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): el NOMBRE del objeto ya existe
    // en los dos idiomas (vanilla_item_names_en.json / displayName_fallback de Calamity), pero
    // DisplayName es un valor FIJADO en UpdateFrom, no una propiedad calculada - sin esto el
    // slot seguiria diciendo "Pico de hierro" con la app en ingles.
    //
    // Recalcula SOLO el nombre desde el objeto que ya hay puesto: rehacer UpdateFrom entero
    // dispararia EmitItemChanged y marcaria el personaje como modificado sin que el usuario
    // haya tocado nada.
    private void RefreshDisplayName()
    {
        if (Item.IsEmpty) { DisplayName = string.Empty; return; }
        DisplayName = Item.IsCalamity
            ? _service.CalamityCatalog.BySyntheticId(Item.Id)?.DisplayName ?? $"Calamity #{Item.Id}"
            : _service.VanillaCatalog.GetName(Item.Id);
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
            var entry = _service.CalamityCatalog.BySyntheticId(itemId);
            string? category = entry?.Category;
            if (category == null) return true; // id sintetico sin entrada real - no rechazar a ciegas
            if (category.StartsWith("Accessories", StringComparison.Ordinal))
                return (AcceptedKind & SlotKind.Accessory) != 0;
            if (category.StartsWith("Armor", StringComparison.Ordinal))
            {
                // H3-11 (tercera auditoria de Opus, Fable): "una pieza de armadura de Calamity
                // se acepta en CUALQUIERA de los 3 slots" - antes solo se comprobaba "es
                // armadura", nunca DE QUE PARTE (cabeza/cuerpo/piernas). EquipSlot es el dato
                // real (extraido del atributo AutoloadEquip real de cada clase - ver
                // CalamityCatalogEntry.EquipSlot) - null para lo que catalog.json marca como
                // "Armor/..." pero no es de verdad una pieza de cuerpo (ej.
                // WulfrumFusionCannon, un arma de invocacion mal encajada en esa categoria) -
                // "desconocido = permitir", mismo criterio ya establecido en el resto de este
                // metodo.
                var wantedKind = entry!.EquipSlot switch
                {
                    "Head" => SlotKind.ArmorHead,
                    "Body" => SlotKind.ArmorBody,
                    "Legs" => SlotKind.ArmorLegs,
                    _ => (SlotKind?)null,
                };
                if (wantedKind == null) return (AcceptedKind & (SlotKind.ArmorHead | SlotKind.ArmorBody | SlotKind.ArmorLegs)) != 0;
                return (AcceptedKind & wantedKind) != 0;
            }
            return false; // arma/pocion/material real de Calamity - nunca es armadura ni accesorio
        }
        return (_service.VanillaSlotKinds.GetKind(itemId) & AcceptedKind) != 0;
    }

    private string BuildRejectionMessage() => AcceptedKind switch
    {
        SlotKind.Ammo => Loc["reject_ammo"],
        SlotKind.Coin => Loc["reject_coin"],
        SlotKind.Dye => Loc["reject_dye"],
        SlotKind.Hook => Loc["reject_hook"],
        SlotKind.Mount => Loc["reject_mount"],
        SlotKind.Cart => Loc["reject_cart"],
        SlotKind.VanityPet => Loc["reject_vanity_pet"],
        SlotKind.LightPet => Loc["reject_light_pet"],
        SlotKind.ArmorHead => Loc["reject_armor_head"],
        SlotKind.ArmorBody => Loc["reject_armor_body"],
        SlotKind.ArmorLegs => Loc["reject_armor_legs"],
        SlotKind.Accessory => Loc["reject_accessory"],
        _ => Loc["reject_generic"],
    };

    public void UpdateFrom(GameItem item)
    {
        // H5-01: instantaneas propias (Clone), nunca la referencia viva - Item sigue mutandose
        // en sitio en otros caminos (Count/Prefix/Favorited), asi que guardar la referencia
        // cruda corromperia una entrada de deshacer ya empujada al UndoStack en cuanto alguien
        // volviera a tocar este mismo objeto mas tarde.
        var before = Item.Clone();
        var after = item.Clone();

        RejectionMessage = null;
        Item = item;
        // OBJ-05b: un objeto favorito que ENTRA en un contenedor que no guarda favoritos (arrastrar
        // del Inventario al Banco, Ctrl+V, cargar un conjunto) traia su marca puesta - se veia la
        // estrella en la esquina del slot y desaparecia sola al recargar, la misma perdida
        // silenciosa que OBJ-05 cerro por el lado del boton. Se limpia aqui, en el unico sitio por
        // el que pasan TODOS los caminos de entrada, para que lo que se ve sea siempre lo que el
        // archivo va a guardar de verdad.
        if (!SupportsFavorite) item.Favorited = false;
        IsEmpty = item.IsEmpty;
        IsCalamity = item.IsCalamity;
        IsFavorited = item.Favorited;

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
            SetStats(null);
            RarityBrush = null;
            OnPropertyChanged(nameof(ShowCount));
            EmitItemChanged(before, after);
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

        SetStats(ItemStatsFormatter.Describe(item.IsCalamity, item.Id, _service.TooltipCatalogs, item.Prefix));

        RefreshPrefixDisplay();

        var suggestion = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        HasBestPrefixSuggestion = suggestion.HasValue && !suggestion.Value.Equals(item.Prefix);
        OnPropertyChanged(nameof(ShowCount));
        EmitItemChanged(before, after);
    }

    // H5-01: unico sitio real que decide si el cambio merece avisar - nunca si son iguales de
    // contenido (evita "editar" en bucle infinito el mismo Deshacer/Rehacer, y una carga real de
    // personaje que reasigna el mismo objeto no genera ruido).
    private void EmitItemChanged(GameItem before, GameItem after)
    {
        if (!before.ContentEquals(after)) _onItemChanged?.Invoke(this, before, after);
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
        // H5-06 (quinta auditoria de Opus): reemplazar el objeto de un slot que YA era
        // favorito borraba la marca en silencio (bug real - un new GameItem siempre nace con
        // Favorited=false). El objeto nuevo hereda el favorito del slot, no el de "objeto
        // recien creado".
        var item = new GameItem { Id = id, Count = 1, Favorited = IsFavorited };
        var suggestion = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        if (suggestion.HasValue) item.Prefix = suggestion.Value;
        UpdateFrom(item);
    }

    // H5-14 (quinta auditoria de Opus): "Ctrl+C/Ctrl+V copian/pegan un objeto entero (prefijo,
    // cantidad, favorito), reutilizando la logica que SwapWith ya tiene resuelta" - a diferencia
    // de PlaceItem (que siempre nace con cantidad 1 y prefijo recien SUGERIDO, pensado para
    // colocar algo nuevo desde la Libreria), pegar debe reproducir EXACTAMENTE lo copiado.
    // Misma restriccion real de slot que PlaceItem/el Drop de la Libreria - un copia/pega no
    // debe poder saltarsela (MainWindow.xaml.cs es quien guarda el "portapapeles" real, un
    // GameItem clonado en el momento de copiar).
    public bool PasteItem(GameItem source)
    {
        if (!AcceptsItem(source.Id))
        {
            RejectionMessage = BuildRejectionMessage();
            return false;
        }
        UpdateFrom(source.Clone());
        return true;
    }

    // H5-06: interruptor real de favorito - el dato ya viajaba de punta a punta
    // (PlrBodySerializer), solo faltaba un camino en la App para tocarlo.
    [RelayCommand]
    private void ToggleFavorite()
    {
        // OBJ-05: red de seguridad real, no solo cosmetica - el menu contextual y el boton del
        // panel Editar ya se esconden donde no aplica, pero un camino nuevo (un atajo, una
        // accion en bloque) no debe poder volver a colar un favorito que el archivo va a tirar.
        if (Item.IsEmpty || !SupportsFavorite) return;
        var before = Item.Clone();
        Item.Favorited = !Item.Favorited;
        IsFavorited = Item.Favorited;
        EmitItemChanged(before, Item.Clone());
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
        var before = Item.Clone();
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
        EmitItemChanged(before, Item.Clone());
    }

    private void RefreshPrefixDisplay()
    {
        _suppressPrefixIdWriteback = true;
        PrefixId = Item.Prefix.IsCalamity ? 0 : Item.Prefix.VanillaId;
        _suppressPrefixIdWriteback = false;

        // Ronda de idioma del 6-sep-2026: el nombre INGLES de cada prefijo ya venia en los dos
        // catalogos (campo "en", desde el primer dia) pero aqui se cogia SIEMPRE el español -
        // exactamente el mismo bug real que la ronda ya cerro en Builds y en Novedades. Se vio
        // en el propio tooltip del slot al abrirlo de verdad con la app en ingles
        // (OBJ-STATS-IDIOMA del arnes): "Prefix: Legendario".
        string idioma = Services.LocalizationService.Instance.Language;
        var prefix = Item.Prefix;
        if (prefix.IsCalamity)
        {
            var prefixEntry = _service.RoguePrefixCatalog.ById(prefix.SyntheticId);
            PrefixDisplay = prefixEntry == null ? string.Empty
                : TerrasavrNative.Core.Data.LocalizedContent.Pick(prefixEntry.Es, prefixEntry.En, idioma);
        }
        else if (!prefix.IsNone)
        {
            var prefixEntry = _service.VanillaPrefixCatalog.ById(prefix.VanillaId);
            PrefixDisplay = prefixEntry == null ? $"#{prefix.VanillaId}"
                : TerrasavrNative.Core.Data.LocalizedContent.Pick(prefixEntry.Es, prefixEntry.En, idioma);
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
        var before = Item.Clone();
        Item.Prefix = prefix;
        RefreshPrefixDisplay();
        // OBJ-09 (oleada de Objetos, 6-sep-2026) - BUG REAL: el tooltip de estadisticas EMPIEZA
        // por el efecto numerico del prefijo ("+15% de daño, +5% de probabilidad de golpe
        // critico, ...", seccion 1 de ItemStatsFormatter.Describe) y este metodo no lo recalculaba
        // nunca - solo UpdateFrom lo hace, y cambiar el prefijo no pasa por ahi. O sea que los
        // CUATRO caminos reales que tocan el prefijo sin cambiar el objeto (el picker de
        // prefijos, "Quitar", el boton de mejor prefijo, y el campo numerico "Prefijo (id)")
        // dejaban el tooltip mintiendo con los numeros del prefijo ANTERIOR: reproducido
        // colocando un arma (que nace con "Legendario" automatico) y quitandole el prefijo -
        // el tooltip seguia prometiendo un +15% de daño que ya no existia. Mismo tipo de fallo
        // que B-6 cerro para "Defensa total", y en el mismo momento en que mas duele: justo
        // cuando el usuario esta comparando prefijos uno a uno.
        if (!Item.IsEmpty)
            SetStats(ItemStatsFormatter.Describe(Item.IsCalamity, Item.Id, _service.TooltipCatalogs, Item.Prefix));
        var suggestion = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        HasBestPrefixSuggestion = suggestion.HasValue && !suggestion.Value.Equals(Item.Prefix);
        EmitItemChanged(before, Item.Clone());
    }

    // Unico punto que toca los datos del tooltip - avisa siempre, igual que hacia el setter
    // generado de la propiedad de antes (el texto final depende ademas del idioma activo, asi que
    // comparar los datos por igualdad para ahorrarse el aviso no seria seguro).
    private void SetStats(ItemStatsInfo? stats)
    {
        _stats = stats;
        OnPropertyChanged(nameof(StatsTooltip));
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
