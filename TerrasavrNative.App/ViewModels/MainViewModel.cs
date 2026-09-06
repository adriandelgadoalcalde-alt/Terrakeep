using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Nucleo basico de la Fase 2: cargar un personaje real, verlo (inventario/banco/caja/fragua/
// boveda/mascota-montura-gancho con objetos de Calamity ya mezclados, mas equipo puesto/monedas/
// municion en su forma vanilla), guardar. Sin edicion todavia - eso es el siguiente paso.
public partial class MainViewModel : ObservableObject
{
    private readonly CharacterFileService _service = new();
    private LoadedCharacter? _loaded;

    // Auditoria de Opus, T-B (segunda auditoria, Fable): "elegir OTRO personaje en Inicio con
    // cambios sin guardar los tira sin avisar - mismo agujero real que OnWindowClosing ya
    // tapaba solo al CERRAR la ventana, no al cargar otro personaje por encima". Gancho de
    // dialogo real en vez de MessageBox aqui mismo - MainViewModel es headless de verdad (los
    // tests de TerrasavrNative.App.ViewModels.Tests lo instancian sin ninguna Window), asi que
    // el dialogo Si/No/Cancelar (mismo texto/logica real ya en MainWindow.OnWindowClosing) vive
    // en la View, que rellena este hueco en su constructor. Sin View enganchada (tests, arnes
    // de consola) se deja pasar siempre - comportamiento identico al de antes de este arreglo.
    public Func<bool>? ConfirmDiscardChanges { get; set; }

    // H5-07 (quinta auditoria de Opus): mismo motivo real que ConfirmDiscardChanges de arriba -
    // sin ninguna View enganchada (tests headless), no debe pasar nada real (nunca escribir en
    // disco). MainWindow.xaml.cs es el unico suscriptor real (llama SaveSession()).
    public event Action? CharacterLoaded;

    // Confirmacion visual real de guardado (pedido explicito 2-sep-2026: "debe ser mas visual
    // que se allá confirmado el guardado no solamente un mensajito abajo a la izquierda") -
    // se activa un momento tras un Save() con exito y se apaga sola; StatusMessage se queda
    // para errores, que no deben ser tan efimeros. Ver el banner real en MainWindow.xaml.
    private readonly DispatcherTimer _saveConfirmationTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };

    // H5-10 (quinta auditoria de Opus): "cuándo se guardó por última vez - en ninguna parte".
    // Se refresca solo (DispatcherTimer real, cada 30s) para que "hace X min" no se quede
    // congelado - mismo criterio de refresco periodico ya visto en el proyecto (banner de
    // guardado, debounce de busqueda), aqui a una cadencia mucho mas baja (un texto relativo no
    // necesita precision al segundo).
    private readonly DispatcherTimer _lastSavedRefreshTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private DateTime? _lastSavedLocal;
    [ObservableProperty] private string? _lastSavedText;

    // H5-10: "Dinero total, convertido y formateado a partir de los cuatro slots de monedas -
    // hoy hay que hacer la cuenta a mano". Conversion real del juego: 100 cobre = 1 plata,
    // 100 plata = 1 oro, 100 oro = 1 platino (IDs reales 71/72/73/74, ya confirmados en
    // Item.IsACoin - vanilla_item_names.json: "Moneda de cobre/plata/oro/platino").
    private static readonly Dictionary<int, long> CoinValueInCopper = new() { [71] = 1, [72] = 100, [73] = 10000, [74] = 1000000 };

    [ObservableProperty] private string _moneyText = "0";

    private void RefreshMoneyText()
    {
        if (CoinsContainer == null) { MoneyText = "0"; return; }
        long totalCopper = 0;
        foreach (var slot in CoinsContainer.Slots)
            if (!slot.IsEmpty && CoinValueInCopper.TryGetValue(slot.ItemId, out long value)) totalCopper += (long)slot.Count * value;

        long platinum = totalCopper / 1000000; totalCopper %= 1000000;
        long gold = totalCopper / 10000; totalCopper %= 10000;
        long silver = totalCopper / 100; totalCopper %= 100;
        long copper = totalCopper;

        var parts = new List<string>();
        if (platinum > 0) parts.Add($"{platinum}p");
        if (gold > 0) parts.Add($"{gold}o");
        if (silver > 0) parts.Add($"{silver}s");
        if (copper > 0 || parts.Count == 0) parts.Add($"{copper}c");
        MoneyText = string.Join(" ", parts);
    }

    private void RefreshLastSavedText()
    {
        if (_lastSavedLocal is not { } saved) { LastSavedText = null; return; }
        var elapsed = DateTime.Now - saved;
        LastSavedText = elapsed.TotalMinutes < 1 ? LocalizationService.Instance["last_saved_moment"]
            : elapsed.TotalHours < 1 ? LocalizationService.Instance.Format("last_saved_minutes", (int)elapsed.TotalMinutes)
            : elapsed.TotalDays < 1 ? LocalizationService.Instance.Format("last_saved_hours", (int)elapsed.TotalHours)
            : LocalizationService.Instance.Format("last_saved_date", saved.ToString("dd/MM/yyyy HH:mm"));
    }

    // H-3 (segunda auditoria de Opus, Fable): "Guardar ya funciona desde cualquier pestaña
    // (N-1) pero un error de guardado va a un TextBlock que 5 de 6 pestañas no ven" -
    // StatusMessage sigue viviendo SOLO dentro de Personaje (correcto para el detalle
    // informativo de la ultima operacion), pero un ERROR real (cargar/guardar/deshacer fallido)
    // ahora TAMBIEN sube a este canal global, visible en cualquier pestaña via un banner real en
    // MainWindow.xaml - mismo nivel que el banner de "Guardado" (N-1), nunca los exitos
    // efimeros, solo lo que de verdad requiere que el usuario se entere este donde este.
    [ObservableProperty] private string? _globalErrorMessage;

    [RelayCommand] private void DismissGlobalError() => GlobalErrorMessage = null;

    // Auditoria de Opus, Bloque 6 (N-5): antes 8 "const int...TabIndex" sueltos - ya tenian
    // nombre real (no eran literales sin explicar en medio del codigo), pero seguian siendo un
    // int cualquiera: nada impedia asignar `SelectedTabIndex = 99` sin que el compilador se
    // quejara, ni el IDE ofrecia autocompletado de "que valores son validos aqui". Un enum real
    // (mismo orden real que las pestañas del XAML, valor explicito para que reordenar el XAML
    // algun dia no desincronice esto en silencio) - el binding de WPF sigue siendo a un int
    // (`SelectedTabIndex`/`PersonajeInnerTabIndex`, TabControl.SelectedIndex no admite otra
    // cosa), el cast a `(int)AppTab.X` vive SOLO en el punto de asignacion.
    private enum AppTab { Inicio = 0, Personaje = 1, Builds = 2, Novedades = 3, Exploracion = 4, AcercaDe = 5 }

    // Indice de la pestaña INTERNA dentro de Personaje - la Libreria vive ahora DENTRO de la
    // propia pestaña Objetos, siempre visible debajo del inventario (pedido explicito
    // 1-sep-2026, "la libreria deberia estar tambien dentro de personaje... como es terrasav" -
    // y ademas necesario para que arrastrar una tarjeta hasta un slot sea posible: si Libreria
    // fuera una pestaña aparte, nunca se verian los dos a la vez). Buffs - usado por
    // RequestPickForBuffSlot para saltar a la pestaña correcta al "Elegir..." un buff.
    // D-e (segunda auditoria de Opus, Fable): se completan los 5 valores que faltaban (ya
    // usados como literal a secas en el arnes UIA) - Desbloqueos hace falta con nombre real
    // para OnPersonajeInnerTabIndexChanged, aqui abajo.
    private enum PersonajeInnerTab { Objetos = 0, Buffs = 1, Investigacion = 2, Apariencia = 3, SpawnPoints = 4, Desbloqueos = 5, Version = 6 }

    [ObservableProperty] private string _statusMessage = LocalizationService.Instance["status_no_character_loaded_dot"];
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;
    // Bloque 0 de la auditoria de Opus (N-2): "se pueden editar 40 slots, cambiar de pestaña,
    // cerrar la app y perderlo todo sin un solo aviso". Se marca sola al detectar CUALQUIER
    // cambio real en un slot/Apariencia/Spawn Points/Desbloqueos/Version tras cargar - ver el
    // enganche real en el constructor, RebuildContainers y AddContainer. _suppressDirty evita
    // que el propio proceso de CARGAR (que tambien dispara PropertyChanged al rellenar campos)
    // se marque a si mismo como "cambio sin guardar".
    [ObservableProperty] private bool _isDirty;
    private bool _suppressDirty = true;

    private void MarkDirty()
    {
        if (!_suppressDirty) IsDirty = true;
    }

    // H5-01 (quinta auditoria de Opus): "casi toda edicion del personaje es irreversible" -
    // pila real de deshacer/rehacer (UndoStack, App/Services) compartida por CUALQUIER slot de
    // objeto (Inventario/Almacenes/Equipamiento/Monturas/Tintes/Monedas/Municion) - ver el
    // callback onItemChanged en AddContainer/EquipmentGroupViewModel, cableado UNA vez aqui.
    public UndoStack UndoStack { get; } = new();
    // Suprime que Undo()/Redo() se graben a si mismos como una edicion nueva - mismo criterio
    // real que _suppressDirty (guardia explicito, no "no hacer nada especial y confiar").
    private bool _suppressUndoRecording;

    [RelayCommand(CanExecute = nameof(CanUndoEdit))]
    private void UndoEdit()
    {
        _suppressUndoRecording = true;
        try { UndoStack.UndoLast(); }
        finally { _suppressUndoRecording = false; }
    }
    private bool CanUndoEdit() => UndoStack.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedoEdit))]
    private void RedoEdit()
    {
        _suppressUndoRecording = true;
        try { UndoStack.RedoLast(); }
        finally { _suppressUndoRecording = false; }
    }
    private bool CanRedoEdit() => UndoStack.CanRedo;

    // C-15 (informe de pulido final, cierra A1): unico punto real donde Apariencia empuja al
    // UndoStack compartido - chequea _suppressUndoRecording en el momento EXACTO del intento de
    // empujar, no antes. Importante para las entradas con debounce (AppearanceViewModel.
    // PushUndoDebounced): el push real llega hasta ~400ms DESPUES del ultimo cambio, mucho
    // despues de que UndoEdit()/RedoEdit() ya hayan devuelto _suppressUndoRecording a false - si
    // el chequeo se hiciera antes (ej. al empezar la rafaga) en vez de aqui, un Deshacer/Rehacer
    // real de un campo con debounce (vida/mana/horas) se grabaria a si mismo como una entrada
    // nueva pasado ese tiempo.
    private void PushAppearanceUndo(UndoEntry entry)
    {
        if (_suppressUndoRecording) return;
        UndoStack.Push(entry);
    }

    // Unico sitio real que decide si un cambio de slot merece una entrada nueva en el
    // historial - antes/despues ya llegan clonados de verdad (ItemSlotViewModel.EmitItemChanged
    // solo dispara si el contenido cambio de verdad).
    private void OnSlotItemChanged(ItemSlotViewModel slot, GameItem before, GameItem after)
    {
        // H6-06 (sexta auditoria de Opus): el doll de Apariencia refleja el equipo puesto EN
        // VIVO - cualquier cambio real de slot puede haber tocado la armadura/vanidad puesta
        // (RefreshAppearanceEquipment relee EquipmentGroup entero, barato). Fuera del "if" de
        // abajo a proposito: esto tiene que disparar tambien durante Deshacer/Rehacer (que SI
        // pasan por aqui con _suppressUndoRecording=true), no solo en la edicion original.
        RefreshAppearanceEquipment();

        if (_suppressDirty || _suppressUndoRecording) return;
        UndoStack.Push(new UndoEntry
        {
            Label = $"{slot.ContainerName} · slot {slot.SlotIndex + 1}",
            Undo = () => slot.UpdateFrom(before.Clone()),
            Redo = () => slot.UpdateFrom(after.Clone()),
        });
    }

    // H6-06 (sexta auditoria de Opus): resuelve la armadura/vanidad real EN VIVO del loadout 0
    // ("Puesto") - PrimaryLoadout (el campo que usa CharacterListEntryViewModel para el doll de
    // Inicio) solo se sincroniza con lo editado en Equipamiento al GUARDAR
    // (CharacterFileService.Save/CalamityCharacterSync); durante la sesion en curso, la fuente
    // de verdad real es EquipmentGroup (EquippedItems/CurrentSocial en GameItem, no PlrLoadout
    // todavia). EquipmentAppearanceResolver.Resolve pide un PlrLoadout - se construye uno
    // sintetico de un solo uso con los 3 slots de armadura reales (cabeza/cuerpo/piernas,
    // Items+Social), sin tocar el modelo real del personaje.
    private void RefreshAppearanceEquipment()
    {
        if (EquipmentGroup == null) { Appearance.UpdateEquippedArmor(default); return; }

        PlrItemSlot[] ItemsRow(ContainerViewModel container)
        {
            var slots = new PlrItemSlot[3];
            for (int i = 0; i < 3; i++)
                slots[i] = container.Slots[i].ItemId == 0 ? PlrItemSlot.Empty : new PlrItemSlot(container.Slots[i].ItemId, 1, 0, false);
            return slots;
        }

        var loadout0 = new PlrLoadout
        {
            Items = ItemsRow(EquipmentGroup.EquippedItems),
            Social = ItemsRow(EquipmentGroup.EquippedSocial),
            // Dyes se deja en su default (Resolve() nunca lo lee - solo cabeza/cuerpo/piernas).
        };
        Appearance.UpdateEquippedArmor(_service.EquipmentAppearance.Resolve(loadout0));
    }

    // Envuelve una operacion en bloque real (Auto-equipar, Mover todo al almacen...) en UNA
    // sola entrada del historial - pedido explicito del informe: "Las operaciones en bloque se
    // registran como una SOLA entrada con su instantanea completa". Compara antes/despues de
    // TODOS los slots de los contenedores indicados (no solo los que la propia operacion diga
    // que toco - una diferencia real detectada vale mas que confiar en que cada operacion
    // reporte bien lo suyo) y solo empuja algo si de verdad cambio algun slot.
    private void RunAsUndoableBatch(string label, IEnumerable<ContainerViewModel> containers, Action body)
    {
        var slots = containers.SelectMany(c => c.Slots).ToList();
        var before = slots.Select(s => s.Item.Clone()).ToList();

        _suppressUndoRecording = true;
        try { body(); }
        finally { _suppressUndoRecording = false; }

        var changes = new List<(ItemSlotViewModel Slot, GameItem Before, GameItem After)>();
        for (int i = 0; i < slots.Count; i++)
            if (!before[i].ContentEquals(slots[i].Item)) changes.Add((slots[i], before[i], slots[i].Item.Clone()));
        if (changes.Count == 0) return;

        UndoStack.Push(new UndoEntry
        {
            Label = label,
            Undo = () => { foreach (var c in changes) c.Slot.UpdateFrom(c.Before.Clone()); },
            Redo = () => { foreach (var c in changes) c.Slot.UpdateFrom(c.After.Clone()); },
        });
    }

    // Auditoria de Opus, Bloque 3 (T-14): unico sitio real donde se decide "esto es una edicion
    // de verdad de un slot" - antes esta suscripcion vivia duplicada en RebuildContainers y
    // AddContainer solo para MarkDirty(); ahora tambien dispara el flash visual del propio
    // slot, y SOLO cuando de verdad es un usuario editando (no durante la carga silenciosa de
    // un personaje, mismo guardia _suppressDirty ya real de N-2 - JustEdited se excluye igual
    // que IsSelected, si no, el propio flash re-entraria el manejador sin fin).
    private void HookSlotEditing(ItemSlotViewModel slot)
    {
        slot.PropertyChanged += (_, e) =>
        {
            // H3-03 (tercera auditoria, Fable): RejectionMessage es puro estado de UI - se
            // escribe precisamente cuando NO se cambio ningun dato real (colocacion rechazada
            // por restriccion de slot, o al limpiar el aviso al cambiar de seleccion, L-e).
            // Sin excluirla aqui, un intento de colocar un objeto invalido marcaba el personaje
            // como "sin guardar" (sin nada real que guardar) Y disparaba el flash de "acabo de
            // editarme" - la señal contraria de lo que paso de verdad.
            if (e.PropertyName is nameof(ItemSlotViewModel.IsSelected) or nameof(ItemSlotViewModel.JustEdited) or nameof(ItemSlotViewModel.RejectionMessage)) return;
            MarkDirty();
            if (!_suppressDirty) slot.TriggerEditFlash();
        };
    }

    // Titulo real de ventana con el nombre del personaje y el punto "sin guardar" (N-2) - antes
    // era la constante fija "Terrakeep" siempre, sin importar que hubiera cargado ni si habia
    // cambios pendientes.
    public string WindowTitle => CharacterName == null
        ? "Terrakeep"
        : $"Terrakeep - {CharacterName}{(IsDirty ? " ●" : "")}";

    partial void OnCharacterNameChanged(string? value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(NameFileMismatch));
        // H-1 (segunda auditoria de Opus, Fable): "PlrCharacter.Name se lee y se escribe sin
        // problema... y muestra el nombre como texto muerto" - el TextBox real de la cabecera
        // (MainWindow.xaml) ahora escribe aqui de verdad. MarkDirty ya respeta _suppressDirty
        // durante la propia carga (mismo guardia real de N-2), asi que cargar un personaje no se
        // marca a si mismo como "cambio sin guardar" solo por rellenar este campo.
        if (_loaded != null && value != null) _loaded.Character.Name = value;
        MarkDirty();
    }

    // H-2 (segunda auditoria de Opus, Fable): "Sin rastro de la version ni del archivo abierto -
    // hay que ir a la pestaña Version (tercer nivel) para saberlo, y no hay forma de distinguir
    // dos personajes con el mismo nombre en carpetas distintas". Una linea real bajo el nombre,
    // en la cabecera global (visible en cualquier pestaña) con el archivo + la version resuelta
    // a su etiqueta real (ej. "1.4.4.0") cuando se conoce, o el numero crudo si no.
    // Ronda de idioma del 6-sep-2026: "· versión " iba a pelo en español, y esta linea se ve en
    // la cabecera global, es decir desde CUALQUIER pestaña de la app.
    public string? FileVersionLine => _loaded == null ? null
        : LocalizationService.Instance.Format("header_file_version_line",
            Path.GetFileName(_loaded.PlrPath),
            VersionEditor.Groups.SelectMany(g => g.Options).FirstOrDefault(o => o.Number == VersionEditor.RawVersion)?.Label
            ?? VersionEditor.RawVersion.ToString());

    // H-1/F2 (segunda auditoria de Opus, Fable): "Aviso discreto si el nombre del archivo no
    // coincide" - un personaje renombrado a mano en el juego, o un .plr copiado/renombrado por
    // fuera, puede tener un nombre real distinto del nombre del fichero (ej. "Personaje (2).plr"
    // con nombre real "Personaje") - confusion real al distinguir dos ventanas del Explorador.
    public bool NameFileMismatch => _loaded != null && CharacterName != null
        && !string.Equals(Path.GetFileNameWithoutExtension(_loaded.PlrPath), CharacterName, StringComparison.Ordinal);
    partial void OnIsDirtyChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));
    [ObservableProperty] private int _selectedTabIndex;
    // Bd-d (segunda auditoria de Opus, Fable): recalcula "lo que ya se posee" al ENTRAR en la
    // pestaña Builds - momento real en que el dato importa, sin recalcular en cada tecla de una
    // edicion en Objetos (ver el comentario completo en BuildsViewModel.RefreshOwnership).
    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == (int)AppTab.Builds && EquipmentGroup != null)
            Builds.RefreshOwnership([.. Containers, .. EquipmentGroup.AllContainers]);
        // X-g (segunda auditoria de Opus, Fable): "el mapa no sabe nada del personaje real" -
        // mismo criterio que Bd-d de arriba, recalculado al ENTRAR en Exploracion (por si se
        // edito algo en Spawn Points desde la ultima vez).
        if (value == (int)AppTab.Exploracion)
            Exploration.SetCharacterSpawns(BuildCharacterSpawns());
        OnPropertyChanged(nameof(IsExplorationTabActive));
        OnPropertyChanged(nameof(ShowVitalsStrip));
    }

    // F-15 (auditoria de Opus vs TEdit, cierra B-01/B-06): la columna central de la barra
    // superior era SIEMPRE la franja de vitales del personaje (o quedaba vacia sin personaje),
    // aunque se estuviera mirando un mundo en Exploracion - "ni un solo dato del mundo cargado,
    // que es lo que el usuario esta mirando". Ahora tiene DOS contenidos excluyentes.
    public bool IsExplorationTabActive => SelectedTabIndex == (int)AppTab.Exploracion;
    public bool ShowVitalsStrip => IsCharacterLoaded && !IsExplorationTabActive;

    // X-g: puntos de aparicion reales del personaje cargado - cada Spawn Point guardado
    // (Servers.Entries, PlrServerEntry.SpawnX/Y) que tenga coordenadas reales puestas (0,0 =
    // todavia sin fijar, no se muestra un marcador de adorno en la esquina para un spawn point
    // recien añadido y vacio). El .plr NO guarda ninguna "aparicion principal" propia - la
    // ultima cama real donde durmio el personaje es un dato del MUNDO (.wld), no del
    // personaje, confirmado en PlrCharacter.cs (sin ningun campo Spawn fuera de
    // PlrServerEntry) - Spawn Points es la unica fuente real de coordenadas de aparicion aqui.
    // C-05 (informe de pulido final, cierra E4): antes cualquier Spawn Point guardado se
    // mostraba en CUALQUIER mundo cargado - "las estrellas de aparicion salen en mundos donde
    // no tienen sentido". Regla real del propio juego (Player.FindSpawn/RemoveSpawn/AddSpawn,
    // las tres identicas): un Spawn Point pertenece a ESTE mundo solo si coinciden id Y nombre
    // (spI[i]==Main.worldID && spN[i]==Main.worldName) - un .wld copiado y renombrado a mano
    // conserva el WorldId, y el juego real lo trataria como un mundo distinto, asi que exigir
    // solo el id no basta. Sin mundo cargado no se filtra (los marcadores no se ven de todos
    // modos sin mapa). La etiqueta ya NO es entry.Name (que es el nombre del MUNDO, no una
    // descripcion del marcador - "no lo especifica en ningun lado" era literal) sino un texto
    // real que dice que es y de quien, sabiendo YA que pertenece al mundo que se esta mirando.
    private IEnumerable<(string Label, int X, int Y)> BuildCharacterSpawns()
    {
        if (_loaded == null) yield break;
        int? worldId = Exploration.LoadedWorldId;
        string? worldName = Exploration.WorldTitle;
        string nombrePersonaje = CharacterName ?? _loaded.Character.Name;
        foreach (var entry in Servers.Entries)
        {
            if (entry.SpawnX == 0 && entry.SpawnY == 0) continue;
            if (!entry.Entry.BelongsToWorld(worldId, worldName)) continue;
            // Ronda de idioma del 6-sep-2026: literal a pelo - es la etiqueta real de cada
            // marcador de spawn point sobre el mapa de Exploracion.
            yield return (LocalizationService.Instance.Format("map_marker_spawn_point", nombrePersonaje, entry.SpawnX, entry.SpawnY), entry.SpawnX, entry.SpawnY);
        }
    }

    // S-c (segunda auditoria de Opus, Fable): "sin enlace al mapa de Exploracion desde Spawn
    // Points" - "Ver en el mapa" real por fila, salta a Exploracion y centra el mapa en esas
    // coordenadas exactas (mismo mecanismo real ya usado por los NPCs, ver
    // ExplorationViewModel.NavigateToTile). Si todavia no hay ningun mundo cargado, avisa en
    // vez de saltar a un mapa en blanco sin explicar nada.
    [RelayCommand]
    private void ViewSpawnOnMap(ServerEntryRowViewModel? row)
    {
        if (row == null) return;
        SelectedTabIndex = (int)AppTab.Exploracion;
        if (!Exploration.IsWorldLoaded)
        {
            // Exploration.StatusMessage (no el StatusMessage global) - es el que se ve de
            // verdad en la pestaña a la que se acaba de saltar, ver el TextBlock real en
            // MainWindow.xaml bajo el mapa.
            Exploration.StatusMessage = LocalizationService.Instance["status_load_world_first"];
            return;
        }
        Exploration.NavigateToTile(row.SpawnX, row.SpawnY);
    }
    [ObservableProperty] private int _personajeInnerTabIndex;
    // D-e (segunda auditoria de Opus, Fable): recalcula el aviso real de version al ENTRAR en
    // Desbloqueos - mismo criterio ya establecido (Bd-d/X-g) para no recalcular en cada tecla
    // de una edicion en la pestaña Version.
    partial void OnPersonajeInnerTabIndexChanged(int value)
    {
        if (value == (int)PersonajeInnerTab.Desbloqueos) Flags.RefreshVersionWarning();
    }
    [ObservableProperty] private bool _saveConfirmationVisible;

    // Auditoria de Opus, Bloque 4 (T-2): breakpoint real y compartido, ver WindowSizeClass.cs.
    // AmplioMinWidth=1500 medido de verdad con el arnes de UI Automation, confirmado por DOS
    // consumidores reales independientes que necesitaron el mismo umbral: E-2 (las 3 vistas de
    // Equipamiento lado a lado - limpio a 1650px, recorta la 3ª columna a 1450px) y A-4
    // (Inventario+Almacen lado a lado, dos rejillas de 10 columnas - limpio a 1500px/1650px,
    // recorta la ultima columna a 1350px).
    // NormalMinWidth=1320 (auditoria de redimensionado, R-04b/H-04b - subido desde 1300, que
    // llego a documentarse aqui mismo como "sin consumidor propio todavia"): YA tiene un
    // consumidor real, IsVitalsStripExpanded (H5-10, mas abajo), y para ese consumidor 1300
    // estaba mal medido por 20px - biseccion real de 10 en 10px: a 1299 la franja vital
    // (Vida+Mana) esta completa, a 1300 se despliega a Defensa+Dinero+Horas+Guardado (pasa de
    // 196 a 350px) y se recorta 16px hasta los 1320, el primer ancho en que cabe entera.
    // Cruzar ese umbral empeoraba la cabecera en TODAS las pantallas.
    [ObservableProperty] private WindowSizeClass _sizeClass = WindowSizeClass.Normal;
    private const double NormalMinWidth = 1320;
    private const double AmplioMinWidth = 1500;
    // Auditoria de redimensionado, R-10/H-09: ver el comentario real de WindowSizeClass.Extra -
    // 1920 es el primer ancho donde los topes de Amplio (1400/1000/1200) dejan mas de un cuarto
    // del viewport real vacio (medido: 28-39% a 1920px).
    private const double ExtraMinWidth = 1920;

    // H5-08 (quinta auditoria de Opus): segunda dimension real, ver WindowHeightClass.cs -
    // AltoMinHeight=900 citado del propio informe ("un portátil de 1440×900... el tamaño más
    // común de uso real"), con margen real sobre el MinHeight=700 obligado de la ventana.
    [ObservableProperty] private WindowHeightClass _heightClass = WindowHeightClass.Bajo;
    private const double AltoMinHeight = 900;

    // Sobrecarga de compatibilidad (16 sitios reales en los tests ya llamaban a la version de
    // un solo argumento) - altura por debajo de AltoMinHeight, mismo comportamiento de siempre
    // para quien no le importe el eje vertical.
    public void UpdateSizeClass(double actualWidth) => UpdateSizeClass(actualWidth, AltoMinHeight - 1);

    public void UpdateSizeClass(double actualWidth, double actualHeight)
    {
        SizeClass = actualWidth >= ExtraMinWidth ? WindowSizeClass.Extra
            : actualWidth >= AmplioMinWidth ? WindowSizeClass.Amplio
            : actualWidth >= NormalMinWidth ? WindowSizeClass.Normal
            : WindowSizeClass.Compacto;
        HeightClass = actualHeight >= AltoMinHeight ? WindowHeightClass.Alto : WindowHeightClass.Bajo;
    }

    // Auditoria de Opus, E-2: umbral real medido con el arnes (ver bitacora.md) - a partir de
    // aqui hay sitio de sobra para las 3 vistas de Equipamiento (Armadura/Vanidad/Tintes) a la
    // vez, sin apretar ninguna. Por debajo, se queda el selector de pildoras de siempre (un
    // panel a la vez).
    // Auditoria de redimensionado, R-10: >= en vez de == - Extra es un superconjunto de espacio
    // de Amplio (ver WindowSizeClass.Extra), nunca debe DESACTIVAR algo que Amplio ya activaba.
    public bool IsEquipmentExpanded => SizeClass >= WindowSizeClass.Amplio;

    // H5-10 (quinta auditoria de Opus): "la franja se pliega por prioridad al encoger (vida/
    // maná primero, el resto después) - el mismo mecanismo de clase de tamaño de H5-08, no una
    // regla nueva". Vida/Maná (lo mas consultado) se quedan siempre visibles; Defensa/Dinero/
    // Horas+Último guardado solo con sitio real (Normal o Amplio).
    public bool IsVitalsStripExpanded => SizeClass != WindowSizeClass.Compacto;

    // Auditoria de Opus, A-4: "Inventario y Almacenes viven en pestañas separadas - nunca se
    // pueden ver a la vez, y por eso arrastrar un objeto del uno al otro es literalmente
    // imposible" (el drop-target del otro contenedor ni siquiera existe en el arbol visual
    // mientras esa pestaña no esta activa). Con sitio real, la pestaña "Inventario" pasa a
    // mostrar Inventario + el Almacen seleccionado lado a lado - el intercambio entre slots YA
    // es generico de por si (ItemSlotViewModel.SwapWith, MainWindow.xaml.cs OnItemSlotDrop no
    // distingue de que contenedor viene cada slot), asi que arrastrar de verdad entre los dos
    // funciona en cuanto ambos coexisten en el arbol visual, sin tocar ese codigo.
    //
    // Umbral medido de verdad (no el intermedio "Normal" original, que resulto demasiado
    // agresivo para esto): dos rejillas REALES de 10 columnas cada una necesitan mas sitio del
    // que "Normal" (1300) da - a 1350px la columna 10 de cada rejilla queda recortada contra el
    // borde (confirmado con captura real). A 1500px (= AmplioMinWidth, el mismo umbral real que
    // ya mide E-2 para sus 3 vistas) ambas rejillas se ven limpias y completas - se reutiliza
    // el mismo umbral compartido en vez de inventar uno propio (ese es justo el punto de T-2:
    // un unico breakpoint real, no uno por pantalla).
    // Auditoria de redimensionado, R-10: >= por el mismo motivo que IsEquipmentExpanded arriba.
    public bool IsStorageExpanded => SizeClass >= WindowSizeClass.Amplio;

    // H4-02 (cuarta auditoria de Opus, Fable): "Almacenes seleccionada y luego oculta en Amplio
    // deja un contenido huerfano sin pestaña activa" - la pestaña interna Equipamiento(0)/
    // Inventario(1)/Almacenes(2) nunca tenia SelectedIndex enlazado, asi que WPF se quedaba
    // apuntando a un TabItem ya Collapsed (IsStorageExpanded oculta "Almacenes" en Amplio, ver
    // el binding real de Visibility) - la vista lado a lado de A-4 (el motivo real de ocultarla)
    // tampoco llegaba a verse en ese caso. Los indices 0/1/2 son un orden REAL y estable de las
    // 3 unicas pestañas de este TabControl (ver MainWindow.xaml, sin enum propio - lo mismo que
    // ya hace SelectedTabIndex/PersonajeInnerTabIndex con AppTab/PersonajeInnerTab, aqui no
    // hacia falta un enum nuevo para 3 valores usados en un unico sitio).
    [ObservableProperty] private int _objetosSubTabIndex;

    // I-c (segunda auditoria de Opus, Fable): "anchos fijos en una pantalla que es puro
    // WrapPanel - en una ventana de 1920px, Inicio usa 880px y deja 1.000px negros". Mismo
    // SizeClass real compartido en vez de un umbral propio - en Amplio, sitio de sobra para que
    // el WrapPanel de tarjetas reparta una fila mas ancha en vez de quedarse angosto.
    // Auditoria de redimensionado, R-10: 1900 en Extra - 6 tarjetas de 270+14 en una sola fila
    // (WrapPanel real) = 1704px, mas el margen real de la fila.
    public double InicioContentMaxWidth => SizeClass switch
    {
        WindowSizeClass.Extra => 1900,
        WindowSizeClass.Amplio => 1400,
        _ => 880,
    };

    // H4-07 (cuarta auditoria de Opus, Fable): mismo patron real que InicioContentMaxWidth de
    // arriba (I-c) - Apariencia se quedaba en 640px fijos siempre (pensado para caber en la
    // ventana minima), dejando ~70% del ancho en negro en Amplio. Las tarjetas de color
    // (Swatches) ya viven en un WrapPanel real - solo hacia falta dejarle mas ancho real para
    // repartir mas columnas, no rehacer el layout.
    // Auditoria de redimensionado, R-10: 1400 en Extra - 8 muestras de color de 150+10 en fila
    // real = 1280px.
    public double AppearanceContentMaxWidth => SizeClass switch
    {
        WindowSizeClass.Extra => 1400,
        WindowSizeClass.Amplio => 1000,
        _ => 640,
    };

    // H5-08 (quinta auditoria de Opus): "el MaxHeight=460 fijo de la fila de Libreria en
    // Objetos y en Buffs" era una de las 2 constantes ciegas señaladas - con sitio vertical
    // real (Alto), la fila puede crecer mas sin apretar la cuadricula de encima (Height="3*"
    // ya le da proporcionalmente mas a los contenedores, este techo solo evitaba que la
    // Libreria se llevara un trozo desproporcionado en ventanas muy altas y estrechas).
    public double LibraryRowMaxHeight => HeightClass == WindowHeightClass.Alto ? 640 : 460;

    // H5-09 (quinta auditoria de Opus): "cuatro pantallas siguen con ancho fijo mientras Inicio y
    // Apariencia si respiran" (Desbloqueos 440, Version 500, las 2 sub-pestañas de Novedades 760,
    // Acerca de 720). UNA sola propiedad compartida (no 4 numeros sueltos ni 4 propiedades
    // nuevas) - las 4 son pantallas "de detalle", mas ligeras que Inicio/Apariencia pero con el
    // mismo problema real; no hace falta un numero distinto por pantalla, cada una ya tiene su
    // propio WrapPanel/UniformGrid interno para repartir el ancho de sobra (familias de
    // Desbloqueos, grupos de Version, tarjetas de Novedades/Acerca de - ver MainWindow.xaml).
    // Auditoria de redimensionado, R-10: 1700 en Extra - 6 tarjetas reales de Desbloqueos de
    // 280+10 = 1740px.
    public double DetailContentMaxWidth => SizeClass switch
    {
        WindowSizeClass.Extra => 1700,
        WindowSizeClass.Amplio => 1200,
        _ => 760,
    };

    // H5-09: numero real de columnas para las listas de tarjetas de version (Novedades x2,
    // Changelog de Acerca de) - 2 en Amplio (autentico reparto en columnas, no solo mas ancho
    // cada tarjeta), 1 en Compacto/Normal (la tira unica de siempre, ya legible a ese ancho).
    // Auditoria de redimensionado, R-10: 3 en Extra - tarjetas de changelog de ~560px, legibles
    // a ese ancho. Un 5º escalon no se propone: a partir de cierto punto dejar aire es la
    // decision correcta (misma razon por la que el texto corrido de Acerca de se topa en 680,
    // ver mas abajo).
    public int DetailCardColumns => SizeClass switch
    {
        WindowSizeClass.Extra => 3,
        WindowSizeClass.Amplio => 2,
        _ => 1,
    };

    partial void OnSizeClassChanged(WindowSizeClass value)
    {
        OnPropertyChanged(nameof(IsEquipmentExpanded));
        OnPropertyChanged(nameof(IsVitalsStripExpanded));
        OnPropertyChanged(nameof(IsStorageExpanded));
        OnPropertyChanged(nameof(InicioContentMaxWidth));
        OnPropertyChanged(nameof(AppearanceContentMaxWidth));
        OnPropertyChanged(nameof(DetailContentMaxWidth));
        OnPropertyChanged(nameof(DetailCardColumns));
        // F-10 (auditoria de Opus vs TEdit, E-10): ExplorationSidebarMaxWidth (R-10 de la
        // auditoria de redimensionado) queda ELIMINADO, no solo desactivado - la columna ya no
        // es Auto+MaxWidth-por-SizeClass, es un GridLength literal con GridSplitter real
        // (Settings.ExplorationSidebarWidth), asi que el mecanismo que este notify alimentaba
        // ya no existe.
        // H4-07: la Libreria/Libreria de buffs se revelan solas en Amplio (ver el comentario
        // real de IsLibraryVisible/IsBuffLibraryVisible arriba).
        OnPropertyChanged(nameof(IsLibraryVisible));
        OnPropertyChanged(nameof(IsBuffLibraryVisible));
        // H4-02: si "Almacenes" (indice 2) era la pestaña activa justo cuando se oculta (Amplio
        // real, IsStorageExpanded=true), mover la seleccion a "Inventario" (1) - el mismo
        // StorageGroup ya esta a la vista ahi, lado a lado con el Inventario (A-4).
        // Auditoria de redimensionado, R-10: >= por si el salto de tamaño va DIRECTO de Normal a
        // Extra sin pasar por un evento intermedio en Amplio (poco probable arrastrando el borde
        // a mano, pero real si algo fija el ancho de un salto, ej. un test).
        if (value >= WindowSizeClass.Amplio && ObjetosSubTabIndex == 2) ObjetosSubTabIndex = 1;
    }

    // H5-08: la segunda mitad de la misma constante ciega - "el auto-revelado de la Libreria
    // (hoy solo por ancho, aunque el problema que resuelve es de alto)". Libreria y la
    // cuadricula de contenedores viven en filas apiladas (RowDefinitions, no columnas) - si hay
    // sitio o no de verdad depende de la ALTURA disponible, no del ancho; SizeClass.Amplio se
    // usaba solo como sustituto aproximado de "ventana grande". Ver IsLibraryVisible/
    // IsBuffLibraryVisible mas abajo.
    partial void OnHeightClassChanged(WindowHeightClass value)
    {
        OnPropertyChanged(nameof(LibraryRowMaxHeight));
        OnPropertyChanged(nameof(IsLibraryVisible));
        OnPropertyChanged(nameof(IsBuffLibraryVisible));
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];
    [ObservableProperty] private EquipmentGroupViewModel? _equipmentGroup;
    [ObservableProperty] private StorageGroupViewModel? _storageGroup;
    [ObservableProperty] private ContainerViewModel? _inventoryContainer;
    [ObservableProperty] private ContainerViewModel? _mountsContainer;
    [ObservableProperty] private ContainerViewModel? _dyesContainer;
    [ObservableProperty] private ContainerViewModel? _coinsContainer;
    [ObservableProperty] private ContainerViewModel? _ammoContainer;

    // Pregunta a Opus sobre el diseño (2-sep-2026): de los ~589px utiles de la pestaña
    // "Objetos", 270 vivian congelados en la fila de la Libreria (46% del alto) - mas robo de
    // espacio que las propias 9 pestañas. Plegada por defecto (con auto-despliegue al elegir
    // objeto, ver RequestPickForSlot) devuelve ese espacio a la cuadricula de items, que es la
    // prioridad explicita del usuario ("me gusta mucho que los objetos se vean directamente de
    // un plumazo").
    [ObservableProperty] private bool _isLibraryCollapsed = true;
    // Mismo criterio que IsLibraryCollapsed de arriba, para la Libreria de buffs (Fase 2 del
    // rework de Buffs, pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada) - plegada por
    // defecto, auto-despliegue al "Elegir..." un buff (RequestPickForBuffSlot).
    [ObservableProperty] private bool _isBuffLibraryCollapsed = true;

    // Segunda auditoria de Opus (Fable), B-2: BUG REAL - "Elegir objeto..."/"Elegir buff..."
    // ponian IsLibraryCollapsed=false DIRECTAMENTE y nada lo volvia a poner en true jamas (ni
    // PlaceInTarget/CancelPick, ni cambiar de personaje) - el PRIMER "Elegir..." de la sesion
    // dejaba la Libreria desplegada para siempre, sin que el usuario hubiera tocado el boton -
    // un pestillo de un solo sentido. IsLibraryCollapsed vuelve a ser SOLO la preferencia real
    // del usuario (la que cambia el boton, y solo el boton) - la visibilidad real es esta
    // propiedad derivada, que combina esa preferencia con un despliegue TEMPORAL mientras se
    // esta eligiendo, sin pisarla. Al terminar/cancelar el pick, vuelve sola a la preferencia
    // real - el boton nunca miente sobre lo que hay en pantalla. Recuperado de 83fd33c (ver
    // BoolToGridLengthConverter), que un revert por rango demasiado ancho se llevo por delante.
    // H4-07 (cuarta auditoria de Opus, Fable): "a pantalla completa sobra muchisimo espacio y
    // la Libreria sigue plegada por defecto" - el motivo real de plegarla por omision
    // ("devolver espacio a la cuadricula", medido a 1080x700, ver el comentario de
    // IsLibraryCollapsed arriba) DESAPARECE en Amplio, donde caben las dos cosas holgadamente
    // (umbral SizeClass ya medido y en produccion para E-2/A-4/I-c). Mismo patron real de B-2
    // ("preferencia + revelado temporal", ver el comentario de arriba) - IsLibraryCollapsed
    // sigue siendo SOLO la preferencia del boton, nunca se pisa: al encoger la ventana por
    // debajo de Amplio, la Libreria vuelve sola a lo que el boton diga.
    // H5-08: se revela sola tambien con HeightClass.Alto, no solo con SizeClass.Amplio - una
    // ventana alta pero no ancha (Compacto/Normal) ya tiene el sitio VERTICAL real que este
    // auto-revelado necesita, ver el comentario de OnHeightClassChanged arriba.
    // Auditoria de redimensionado, R-10: >= en vez de == - mismo motivo que IsEquipmentExpanded.
    public bool IsLibraryVisible => !IsLibraryCollapsed || Library.IsPicking || SizeClass >= WindowSizeClass.Amplio || HeightClass == WindowHeightClass.Alto;
    public bool IsBuffLibraryVisible => !IsBuffLibraryCollapsed || BuffLibrary.IsPicking || SizeClass >= WindowSizeClass.Amplio || HeightClass == WindowHeightClass.Alto;
    partial void OnIsLibraryCollapsedChanged(bool value) => OnPropertyChanged(nameof(IsLibraryVisible));
    partial void OnIsBuffLibraryCollapsedChanged(bool value) => OnPropertyChanged(nameof(IsBuffLibraryVisible));
    public ResearchViewModel Research { get; }
    public BuildsViewModel Builds { get; }
    public WhatsNewViewModel WhatsNew { get; }
    public ChangelogViewModel Changelog { get; }
    public AboutViewModel About { get; } = new();
    public ExplorationViewModel Exploration { get; }
    public LibraryViewModel Library { get; }
    public AppearanceViewModel Appearance { get; }
    public ServersViewModel Servers { get; } = new();
    public FlagsViewModel Flags { get; } = new();
    public VersionEditorViewModel VersionEditor { get; } = new();
    public BuffsViewModel Buffs { get; }
    public BuffLibraryViewModel BuffLibrary { get; }
    public BuffEditViewModel BuffEdit { get; }
    public ItemEditViewModel ItemEdit { get; }
    public HomeViewModel Home { get; }
    // Pedido explicito del usuario (5-sep-2026): idioma en vivo, sin reiniciar - expuesto aqui
    // (root DataContext de casi toda la ventana) para que cualquier XAML pueda usar
    // {Binding Loc[clave]} directamente, mismo criterio que Home/About/etc de arriba. Instance es
    // un singleton real (LocalizationService) - PropertyChanged("Item[]") de ahi mismo refresca
    // CUALQUIER binding indexado ya en pantalla al cambiar el idioma en Ajustes.
    public LocalizationService Loc => LocalizationService.Instance;
    // H5-07 (quinta auditoria de Opus): carpetas adicionales de personajes/mundos + N
    // configurable de copias de seguridad - "Ajustes" real, ver SettingsViewModel.
    public SettingsViewModel Settings { get; }

    public MainViewModel()
    {
        // H5-01: CanUndoEdit/CanRedoEdit dependen de UndoStack.CanUndo/CanRedo - UndoStack ya
        // notifica ese cambio en cada Push/UndoLast/RedoLast/Clear, solo hace falta reenviarlo.
        UndoStack.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Services.UndoStack.CanUndo)) UndoEditCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(Services.UndoStack.CanRedo)) RedoEditCommand.NotifyCanExecuteChanged();
        };
        // Auditoria de Opus, I-1: elegir un personaje real en el lanzador de Inicio carga
        // exactamente igual que el dialogo de "Cargar personaje..." de siempre, y salta
        // directo a Personaje - de nada sirve un lanzador de un click si despues hay que ir a
        // buscar la pestaña a mano (P1).
        // H5-07 (quinta auditoria de Opus): antes de construir Home (que ya escanea personajes
        // reales en su propio constructor) - las carpetas adicionales de Ajustes deben estar
        // aplicadas a CharacterFileService.ExtraPlayerFolders ANTES de ese primer escaneo real,
        // no despues.
        Settings = new SettingsViewModel(_service.BackupHistory);
        Home = new HomeViewModel(_service.EquipmentAppearance, _service.BackupHistory);
        Home.CharacterChosen += path =>
        {
            if (IsDirty && ConfirmDiscardChanges?.Invoke() == false) return;
            LoadFromPath(path);
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        };
        Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds, _service);
        WhatsNew = new WhatsNewViewModel(_service.WhatsNewVanilla, _service.WhatsNewCalamity, _service.VanillaCatalog, _service.CalamityCatalog, _service.WhatsNewItemIds, _service.TooltipCatalogs);
        Changelog = new ChangelogViewModel(_service.Changelog);
        Exploration = new ExplorationViewModel(_service);
        Library = new LibraryViewModel(_service);
        Research = new ResearchViewModel(_service);
        // C-15 (informe de pulido final, cierra A1): Apariencia empuja al MISMO UndoStack
        // compartido, via un callback (mismo criterio ya establecido - ExplorationViewModel/
        // BuffsViewModel reciben un Action<T> en vez de la propia MainViewModel entera).
        // PushAppearanceUndo es el UNICO punto que decide si de verdad se empuja (chequea
        // _suppressUndoRecording el mismo instante del intento, nunca antes) - AppearanceViewModel
        // no necesita su propio guardia de reentrada duplicado, el existente ya cubre cualquier
        // Undo/Redo real (UndoEdit/RedoEdit son el UNICO sitio que llama a UndoStack.UndoLast/
        // RedoLast, y los dos ya envuelven la llamada en _suppressUndoRecording=true/false).
        Appearance = new AppearanceViewModel(_service, PushAppearanceUndo, () => _suppressUndoRecording);
        Library.ItemPlaced += () =>
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        };
        // IsLibraryVisible depende de Library.IsPicking (ver su comentario) - Library ya
        // notifica ese cambio (OnPickTargetChanged), asi que solo hace falta reenviarlo.
        Library.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryViewModel.IsPicking)) OnPropertyChanged(nameof(IsLibraryVisible));
        };
        BuffEdit = new BuffEditViewModel(_service);
        BuffLibrary = new BuffLibraryViewModel(_service);
        BuffLibrary.BuffPlaced += () =>
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Buffs;
        };
        BuffLibrary.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BuffLibraryViewModel.IsPicking)) OnPropertyChanged(nameof(IsBuffLibraryVisible));
        };
        Buffs = new BuffsViewModel(_service, RequestPickForBuffSlot);
        // Buffs es una unica instancia persistente que reconstruye sus slots en cada
        // LoadFrom() real (a diferencia de los contenedores de objetos, que MainViewModel crea
        // el mismo directamente en AddContainer) - SlotChanged reenvia el cambio de cualquier
        // slot de buff, cargado el personaje que sea, sin tener que resuscribirse cada vez.
        Buffs.SlotChanged += MarkDirty;
        ItemEdit = new ItemEditViewModel(_service);
        // Apariencia/Spawn Points/Desbloqueos/Version son tambien instancias persistentes -
        // cualquier propiedad que cambien tras cargar un personaje es una edicion real.
        Appearance.PropertyChanged += (_, _) => MarkDirty();
        Servers.Changed += MarkDirty; // B-5 (segunda auditoria de Opus): evento real, ver ServersViewModel.Changed
        Flags.PropertyChanged += (_, _) => MarkDirty();
        // H5-02 (quinta auditoria de Opus): Investigacion ahora es editable de verdad - evento
        // dedicado (no PropertyChanged entero, que tambien dispara solo con navegar/buscar).
        Research.ResearchChanged += MarkDirty;
        VersionEditor.PropertyChanged += (_, _) =>
        {
            MarkDirty();
            OnPropertyChanged(nameof(FileVersionLine)); // H-2: cambiar la version en la pestaña Version debe reflejarse tambien en la cabecera
        };
        _saveConfirmationTimer.Tick += (_, _) =>
        {
            SaveConfirmationVisible = false;
            _saveConfirmationTimer.Stop();
        };
        _lastSavedRefreshTimer.Tick += (_, _) => RefreshLastSavedText();
        _lastSavedRefreshTimer.Start();
        _whereIsItDebounceTimer.Tick += (_, _) =>
        {
            _whereIsItDebounceTimer.Stop();
            ApplyWhereIsItFilter();
        };
    }

    private int _pendingSessionLoadout;
    private int _pendingSessionStorageIndex;

    // H5-07 (quinta auditoria de Opus): "session.json: ultimo personaje/pestaña/sub-pestaña,
    // preferencias de plegado, loadout y almacen seleccionados - Inicio ofrece 'Continuar con
    // Nombre' como accion destacada". Restaura la navegacion real (nunca carga el personaje
    // solo - Home.SetLastSession deja esa decision explicita al usuario, ver el comentario real
    // ahi). El loadout/almacen guardados se aplican mas tarde, en LoadFromPath, en cuanto
    // EquipmentGroup/StorageGroup existen de verdad.
    //
    // Publico y NUNCA llamado desde el constructor a proposito - mismo motivo real por el que
    // WindowPlacementService.Apply() solo lo llama MainWindow (la View), nunca MainViewModel: un
    // fichero real en disco (session.json) leido en CADA "new MainViewModel()" contaminaria el
    // arranque de los DECENAS de tests reales que construyen un MainViewModel headless en este
    // proyecto (SelectedTabIndex/IsLibraryCollapsed dejarian de ser deterministas entre
    // ejecuciones). MainWindow.xaml.cs lo llama una vez, real, al construir la ventana.
    public void RestoreSession()
    {
        var session = SessionService.Load();
        // Pedido explicito del usuario (4-sep-2026): "cuando inicias el programa nunca inicia
        // en el inicio, inicia en la pestaña de versiones del sav de personaje" - restaurar
        // SelectedTabIndex/PersonajeInnerTabIndex del session.json anterior secuestraba la
        // pantalla de arranque (casi siempre Personaje/Version, la ultima pestaña tocada antes
        // de cerrar). La app SIEMPRE arranca en Inicio ahora (valor por defecto de
        // SelectedTabIndex/PersonajeInnerTabIndex/ObjetosSubTabIndex, nunca tocados aqui) -
        // Home.SetLastSession(session) sigue dejando listo el boton real "Continuar con
        // [Nombre]" para volver a la ultima posicion de un clic, sin imponerla.
        IsLibraryCollapsed = session.IsLibraryCollapsed;
        IsBuffLibraryCollapsed = session.IsBuffLibraryCollapsed;
        _pendingSessionLoadout = session.SelectedLoadout;
        _pendingSessionStorageIndex = session.SelectedStorageIndex;
        Home.SetLastSession(session);
    }

    // H5-07: captura TODO el estado real de sesion de un plumazo - llamado tras cada carga con
    // exito (recordar el personaje de inmediato, no solo al cerrar) y al cerrar la ventana
    // (para capturar tambien la posicion final real de navegacion, mismo criterio ya
    // establecido por WindowPlacementService.Save).
    public void SaveSession()
    {
        var session = new TerrakeepSession
        {
            LastCharacterPath = _loaded?.PlrPath,
            LastCharacterName = _loaded?.Character.Name,
            LastCharacterModifiedUtc = _loaded != null && File.Exists(_loaded.PlrPath) ? File.GetLastWriteTimeUtc(_loaded.PlrPath) : null,
            SelectedTabIndex = SelectedTabIndex,
            PersonajeInnerTabIndex = PersonajeInnerTabIndex,
            ObjetosSubTabIndex = ObjetosSubTabIndex,
            IsLibraryCollapsed = IsLibraryCollapsed,
            IsBuffLibraryCollapsed = IsBuffLibraryCollapsed,
            SelectedLoadout = EquipmentGroup?.SelectedLoadout ?? _pendingSessionLoadout,
            SelectedStorageIndex = StorageGroup?.SelectedIndex ?? _pendingSessionStorageIndex,
        };
        SessionService.Save(session);
    }

    // Selecciona un slot para el panel "Editar" compartido (equivalente real de app.TabEdit)
    // - un unico slot seleccionado a la vez, sea cual sea el contenedor donde este (pedido
    // explicito 1-sep-2026: el panel debe "acompañar" a cualquier pestaña de objetos).
    public void SelectSlot(ItemSlotViewModel slot)
    {
        if (ItemEdit.Slot != null) ItemEdit.Slot.IsSelected = false;
        slot.IsSelected = true;
        ItemEdit.Slot = slot;
    }

    // Mismo patron que SelectSlot de arriba, para el panel "Editar buff seleccionado" (pregunta
    // a Opus sobre el diseño 2-sep-2026, cuarta pasada) - un unico buff seleccionado a la vez.
    public void SelectBuffSlot(BuffSlotViewModel slot)
    {
        if (BuffEdit.Slot != null) BuffEdit.Slot.IsSelected = false;
        slot.IsSelected = true;
        BuffEdit.Slot = slot;
    }

    // H5-05 (quinta auditoria de Opus): "no se puede buscar entre los ~350 slots que el
    // personaje ya tiene - ¿Donde tengo el Ala de murcielago? solo se responde a ojo". El dato
    // ya esta resuelto (Bd-d, BuildsViewModel.RefreshOwnership recorre exactamente estos mismos
    // contenedores) - aqui solo faltaba ENSEÑARLO donde de verdad hace falta.
    [ObservableProperty] private bool _isWhereIsItOpen;
    [ObservableProperty] private string _whereIsItSearchText = string.Empty;
    [ObservableProperty] private string _whereIsItSummary = string.Empty;
    public ObservableCollection<WhereIsItResultViewModel> WhereIsItResults { get; } = [];
    private readonly DispatcherTimer _whereIsItDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    partial void OnWhereIsItSearchTextChanged(string value)
    {
        _whereIsItDebounceTimer.Stop();
        _whereIsItDebounceTimer.Start();
    }

    // F-16 (auditoria de Opus vs TEdit, B-04): antes el boton se OCULTABA del todo sin personaje
    // (Visibility en XAML) - ahora se DESHABILITA via CanExecute, mismo patron ya establecido
    // para Guardar/etc. (IsCharacterLoaded), asi la barra no cambia de forma al cargar.
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void ToggleWhereIsIt() => IsWhereIsItOpen = !IsWhereIsItOpen;

    // Recorre TODOS los slots reales del personaje (mismos 2 origenes que Bd-d ya agrega:
    // Containers - Inventario/Banco/Caja fuerte/Fragua/Boveda/Monedas/Municion - y
    // EquipmentGroup.AllContainers - Armadura/Vanidad/Tintes de los 4 loadouts) - el
    // ContainerViewModel real de cada uno viaja junto al slot solo para la navegacion
    // (WhereIsItResultViewModel.ContainerKey), nunca para mostrar (ContainerName, el texto
    // legible real que el slot ya lleva, es lo que se ve).
    private IEnumerable<(ItemSlotViewModel Slot, ContainerViewModel Container)> AllOwnedSlotsWithContainers()
    {
        foreach (var c in Containers)
            foreach (var s in c.Slots)
                if (!s.IsEmpty) yield return (s, c);
        if (EquipmentGroup != null)
            foreach (var c in EquipmentGroup.AllContainers)
                foreach (var s in c.Slots)
                    if (!s.IsEmpty) yield return (s, c);
    }

    // Publica a proposito (no solo llamada desde el Tick del debounce) - mismo criterio real ya
    // establecido en el proyecto para poder probar el filtro sin depender de un Dispatcher real
    // corriendo (ver ResearchEditableTests.cs, que sortea el mismo problema con
    // SelectCategoryCommand en vez de esperar el debounce de busqueda por texto).
    public void ApplyWhereIsItFilter()
    {
        WhereIsItResults.Clear();
        var todos = AllOwnedSlotsWithContainers().ToList();

        if (string.IsNullOrWhiteSpace(WhereIsItSearchText))
        {
            // Sin busqueda activa, todos "coinciden" (mismo criterio real de X-c/IsSearchMatch
            // por defecto) - nada atenuado en ningun sitio de la app.
            foreach (var (slot, _) in todos) slot.IsSearchMatch = true;
            WhereIsItSummary = string.Empty;
            return;
        }

        string query = WhereIsItSearchText;
        // C-09 (informe de pulido final, cierra L2): Fold en vez de ToLowerInvariant a secas -
        // "mascara" tiene que encontrar "máscara" tambien aqui (Buscar en el personaje).
        var coincidencias = todos.Where(p => LibrarySearchGrammar.Matches(
            query, p.Slot.Item.Id, LibrarySearchGrammar.Fold(p.Slot.DisplayName), null)).ToList();

        var idsCoincidentes = coincidencias.Select(p => p.Slot).ToHashSet();
        foreach (var (slot, _) in todos) slot.IsSearchMatch = idsCoincidentes.Contains(slot);

        // L-c: mismo tope real ya medido para la Libreria - una busqueda amplia sobre el
        // personaje entero (ej. una letra suelta) no debe pintar cientos de filas de golpe.
        const int maxResults = 40;
        foreach (var (slot, container) in coincidencias.Take(maxResults))
            WhereIsItResults.Add(new WhereIsItResultViewModel(slot, container.Key));

        // H5-05: "aprovechar para responder tambien ¿duplicados? y ¿cuantos entre todos los
        // almacenes?" - duplicados reales = mismo id de objeto en mas de un slot a la vez.
        var duplicados = coincidencias.GroupBy(p => p.Slot.Item.Id).Where(g => g.Count() > 1).ToList();
        // Ronda de idioma del 6-sep-2026: los tres literales iban a pelo aunque sus claves
        // ("search_duplicates_extra"/"search_no_results_character"/"search_showing_results") YA
        // existian en los dos diccionarios desde la ronda anterior - creadas y nunca enchufadas.
        // El cuarto caso (el recuento a secas) no tenia clave: "search_results_count" es nueva.
        var loc = LocalizationService.Instance;
        string extra = duplicados.Count > 0
            ? loc.Format("search_duplicates_extra", duplicados.Sum(g => g.Count()), duplicados.Count)
            : string.Empty;
        WhereIsItSummary = coincidencias.Count == 0
            ? loc["search_no_results_character"]
            : coincidencias.Count > maxResults
                ? loc.Format("search_showing_results", maxResults, coincidencias.Count, extra)
                : loc.Format("search_results_count", coincidencias.Count, extra);
    }

    // Salta a la pestaña/sub-pestaña/loadout/almacen real donde vive el slot elegido, lo
    // selecciona en el panel Editar y dispara el mismo flash real de "acabo de editarse" (T-14) -
    // aunque no se haya editado nada, es la misma señal visual de "aqui esta" que ya conoce
    // el usuario del resto de la app.
    [RelayCommand]
    private void NavigateToWhereIsItResult(WhereIsItResultViewModel? result)
    {
        if (result == null) return;
        SelectedTabIndex = (int)AppTab.Personaje;
        PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;

        string key = result.ContainerKey;
        if (key is "bank" or "bank2" or "bank3" or "bank4")
        {
            ObjetosSubTabIndex = IsStorageExpanded ? 1 : 2; // H4-02: en Amplio, Almacenes vive junto a Inventario, no como pestaña propia
            int indice = key switch { "bank" => 0, "bank2" => 1, "bank3" => 2, _ => 3 };
            if (StorageGroup != null) StorageGroup.SelectCommand.Execute(StorageGroup.Options[indice]);
        }
        else if (key.StartsWith("loadout", StringComparison.Ordinal))
        {
            ObjetosSubTabIndex = 0; // Equipamiento
            if (EquipmentGroup != null)
            {
                int loadout = key["loadout".Length] - '0';
                string kindPart = key[("loadout".Length + 1)..];
                int kindValue = kindPart switch { "Items" => (int)EquipmentKind.Items, "Social" => (int)EquipmentKind.Social, "Dyes" => (int)EquipmentKind.Dyes, _ => (int)EquipmentKind.Items };
                var loadoutOpt = EquipmentGroup.LoadoutOptions.FirstOrDefault(o => o.Value == loadout);
                if (loadoutOpt != null) EquipmentGroup.SelectLoadoutCommand.Execute(loadoutOpt);
                var kindOpt = EquipmentGroup.KindOptions.FirstOrDefault(o => o.Value == kindValue);
                if (kindOpt != null) EquipmentGroup.SelectKindCommand.Execute(kindOpt);
            }
        }
        else
        {
            ObjetosSubTabIndex = 1; // Inventario (tambien Monedas/Municion, que viven dentro de ese mismo panel)
        }

        SelectSlot(result.Slot);
        result.Slot.TriggerEditFlash();
        // Hallazgo real (feedback directo del usuario): cerrar el popup con IsWhereIsItOpen=false
        // NUNCA vaciaba WhereIsItSearchText - el atenuado real de ApplyWhereIsItFilter
        // (IsSearchMatch=false en TODOS los slots que no coincidieron, Opacity 0.35 via
        // MainWindow.xaml:406) se quedaba aplicado en el resto de la app, sin ningun cuadro de
        // busqueda visible que explique por que ("la animacion se queda hasta que borras lo que
        // has buscado" - literal, el propio texto de busqueda era la unica llave que lo apagaba).
        // Vaciar el texto aqui y refiltrar al instante (sin esperar el debounce de 180ms del
        // Tick) quita el atenuado justo al navegar, como cualquier busqueda ya cerrada deberia.
        WhereIsItSearchText = string.Empty;
        ApplyWhereIsItFilter();
        IsWhereIsItOpen = false;
    }

    // Usado por las tarjetas de la pagina de Inicio para saltar directamente a una seccion.
    // "Libreria" ya no es una pestaña propia (ver el comentario de PersonajeInnerTab) - vive
    // dentro de Objetos, asi que la tarjeta de Inicio salta ahi igual que "Personaje".
    [RelayCommand]
    private void GoToTab(string tab)
    {
        if (tab is "Personaje" or "Libreria")
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
            // H4-03 (cuarta auditoria de Opus, Fable): la tarjeta "Librería" de Inicio (el
            // camino MAS visible hacia la Libreria) aterrizaba en Objetos sin desplegarla si
            // IsLibraryCollapsed ya estaba a true (su valor por omision/el que dejo el usuario)
            // - Ctrl+F si la desplegaba, dos caminos al mismo sitio con resultado distinto.
            // Mismo comando reutilizado por el atajo de teclado (OnWindowKeyDown), asi que este
            // desplegado ahora aplica a los dos - no repetir la asignacion en cada sitio.
            if (tab == "Libreria") IsLibraryCollapsed = false;
            return;
        }
        SelectedTabIndex = tab switch
        {
            "Builds" => (int)AppTab.Builds,
            "Novedades" => (int)AppTab.Novedades,
            "Exploracion" => (int)AppTab.Exploracion,
            "AcercaDe" => (int)AppTab.AcercaDe,
            _ => (int)AppTab.Inicio,
        };
    }

    // H4-13 (cuarta auditoria de Opus, Fable): "Ctrl+F solo conoce la Libreria de objetos" -
    // si la pestaña interna activa YA es Buffs, es la Libreria de BUFFS la que hace falta
    // desplegar/enfocar, no la de objetos (saltar a Objetos desde Buffs para ver una libreria
    // que no corresponde seria peor que no hacer nada). Usado por OnWindowKeyDown - el propio
    // code-behind lee IsBuffsInnerTabActive despues de ejecutar este comando para decidir que
    // TextBox enfocar (el buscador en si vive en la vista, no en el ViewModel).
    public bool IsBuffsInnerTabActive => PersonajeInnerTabIndex == (int)PersonajeInnerTab.Buffs;

    [RelayCommand]
    private void GoToContextualLibrary()
    {
        if (IsBuffsInnerTabActive)
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            IsBuffLibraryCollapsed = false;
        }
        else
        {
            GoToTab("Libreria");
        }
    }

    private void RequestPickForSlot(ItemSlotViewModel slot)
    {
        Library.PickTarget = slot;
        SelectSlot(slot); // el panel Editar sigue al slot que se esta rellenando desde la Libreria
        SelectedTabIndex = (int)AppTab.Personaje;
        PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        // IsLibraryCollapsed YA NO se toca aqui (segunda auditoria, B-2) - Library.IsPicking
        // (puesto arriba al fijar PickTarget) ya revela la Libreria via IsLibraryVisible, sin
        // pisar la preferencia real del usuario.
    }

    [RelayCommand]
    private void ToggleLibraryCollapsed() => IsLibraryCollapsed = !IsLibraryCollapsed;

    // Gemelo de RequestPickForSlot de arriba, para buffs (Fase 2 del rework, pregunta a Opus
    // sobre el diseño 2-sep-2026, cuarta pasada).
    private void RequestPickForBuffSlot(BuffSlotViewModel slot)
    {
        BuffLibrary.PickTarget = slot;
        SelectBuffSlot(slot);
        SelectedTabIndex = (int)AppTab.Personaje;
        PersonajeInnerTabIndex = (int)PersonajeInnerTab.Buffs;
        // IsBuffLibraryCollapsed YA NO se toca aqui, mismo motivo que RequestPickForSlot.
    }

    [RelayCommand]
    private void ToggleBuffLibraryCollapsed() => IsBuffLibraryCollapsed = !IsBuffLibraryCollapsed;

    public void LoadFromPath(string plrPath)
    {
        // Auditoria de Opus, N-2: mientras se carga, cada LoadFrom() de abajo dispara sus
        // propios PropertyChanged reales (rellenar campos) - eso NO es una edicion del
        // usuario, asi que se suprime aqui y se reactiva solo cuando la carga entera termino
        // bien (o se corta del todo si fallo, ver el catch).
        _suppressDirty = true;
        try
        {
            Library.PickTarget = null;
            BuffLibrary.PickTarget = null;
            ItemEdit.Slot = null;
            BuffEdit.Slot = null;
            // H5-01: el historial de un personaje no tiene sentido real sobre otro.
            UndoStack.Clear();
            // H5-10: "Ultimo guardado" es real de ESTA sesion - cargar un personaje no cuenta
            // como guardarlo, aunque el fichero en si tenga una fecha de modificacion antigua.
            _lastSavedLocal = null;
            RefreshLastSavedText();
            _loaded = _service.Load(plrPath);
            RebuildContainers();
            Appearance.LoadFrom(_loaded.Character);
            RefreshAppearanceEquipment(); // H6-06: pinta el equipo puesto real desde el primer render, no solo tras la primera edicion
            Servers.LoadFrom(_loaded.Character);
            Flags.LoadFrom(_loaded.Character);
            VersionEditor.LoadFrom(_loaded.Character);
            Buffs.LoadFrom(_loaded.Character);
            BuffEdit.SetCharacterVersion(_loaded.Character.Version);
            CharacterName = _loaded.Character.Name;
            HasCalamityData = _loaded.TplrPath != null;
            IsCharacterLoaded = true;
            int calamityCount = _loaded.MergedContainers.Values.Sum(items => items.Count(i => i.IsCalamity));
            StatusMessage = HasCalamityData
                ? LocalizationService.Instance.Format("status_loaded_with_calamity", _loaded.Character.Name, calamityCount)
                : LocalizationService.Instance.Format("status_loaded_vanilla_only", _loaded.Character.Name);
            GlobalErrorMessage = null; // H-3: una carga con exito limpia cualquier error global anterior
            // H5-07: el ULTIMO personaje se recuerda de inmediato, no solo al cerrar la app - si
            // la app se cierra en seco (corte de luz, Administrador de tareas), la proxima
            // sesion sigue sabiendo cual era. Evento, NO una llamada directa a SaveSession() -
            // LoadFromPath lo llaman decenas de tests headless directamente sobre un
            // "new MainViewModel()" sin ninguna MainWindow real detras; escribir un fichero real
            // en el disco del usuario en cada uno de esos tests seria un efecto secundario real
            // e indeseado (paso por aqui de verdad: un session.json con datos sinteticos de test
            // aparecio en %LOCALAPPDATA%\Terrakeep\ real de esta maquina la primera vez que esto
            // se probo, antes de este arreglo). MainWindow.xaml.cs es el UNICO suscriptor real.
            CharacterLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            // Auditoria de Opus, T-22: "LoadFromPath traga excepciones y sigue... queda en un
            // estado a medias". Si el fallo ocurrio DESPUES de _service.Load (ej. en
            // RebuildContainers o en algun LoadFrom), _loaded ya apunta al personaje nuevo
            // pero los ViewModels solo se rellenaron a medias - Guardar/AutoEquip/Investigar
            // todo actuarian sobre ese estado inconsistente. _loaded=null cierra el hueco real
            // (los 3 comandos ya comprueban _loaded==null antes de hacer nada).
            _loaded = null;
            IsCharacterLoaded = false;
            StatusMessage = LocalizationService.Instance.Format("error_loading", ex.Message);
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
        finally
        {
            _suppressDirty = false;
            IsDirty = false;
            UndoLastSaveCommand.NotifyCanExecuteChanged(); // el personaje cargado (y su .bak) ha cambiado
            OnPropertyChanged(nameof(FileVersionLine)); // H-2: archivo/version cambian con cada carga (o desaparecen si fallo)
            Home.UpdateCurrentPath(_loaded?.PlrPath); // I-a: la tarjeta real de Inicio debe reflejar cual esta cargado ahora
            // H5-07: loadout/almacen recordados de la sesion anterior - EquipmentGroup/
            // StorageGroup solo existen tras RebuildContainers (arriba), asi que hasta aqui no
            // se podian aplicar. Solo tiene sentido real con un personaje SI cargado (si fallo,
            // ambos son null).
            if (EquipmentGroup != null)
            {
                var loadoutOpt = EquipmentGroup.LoadoutOptions.FirstOrDefault(o => o.Value == _pendingSessionLoadout);
                if (loadoutOpt != null) EquipmentGroup.SelectLoadoutCommand.Execute(loadoutOpt);
            }
            if (StorageGroup != null && _pendingSessionStorageIndex >= 0 && _pendingSessionStorageIndex < StorageGroup.Options.Count)
                StorageGroup.SelectCommand.Execute(StorageGroup.Options[_pendingSessionStorageIndex]);
            // X-g: cubre cargar un personaje distinto mientras Exploracion ya esta a la vista -
            // en el finally (no en RebuildContainers) porque Servers.LoadFrom (arriba) tiene que
            // haber corrido ya para leer sus Spawn Points reales, no los del personaje anterior.
            Exploration.SetCharacterSpawns(BuildCharacterSpawns());
        }
    }

    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void Save()
    {
        if (_loaded == null) return;
        try
        {
            SyncEditsBackToMerged();
            // H5-02 (quinta auditoria de Opus): la Investigacion ahora se edita de verdad -
            // mismo criterio real que SyncEditsBackToMerged para objetos, el estado en memoria
            // (Research._researchedCounts) se vuelca al PlrCharacter justo antes de guardar.
            Research.SyncBackTo(_loaded.Character);
            _service.Save(_loaded);
            // H5-04: copia rotativa real de ESTE guardado - independiente del .bak de un solo
            // nivel (WriteAtomic ya lo genera, no se toca). Un fallo copiando el historial
            // rotativo (disco lleno, permisos...) no debe impedir que el guardado real, ya
            // confirmado, se de por bueno - se intenta, pero no se relanza si falla.
            try { _service.BackupHistory.SaveBackup(_loaded); } catch { /* el guardado real ya tuvo exito, esto es solo la red extra */ }
            // H3-15 (tercera auditoria de Opus, Fable): "la insignia de Calamity no se enciende
            // justo despues del guardado que crea el .tplr por primera vez" - HasCalamityData
            // solo se recalculaba en LoadFromPath. _service.Save YA deja loaded.TplrPath puesto
            // de verdad (CharacterFileService.Save, linea ~160) en cuanto crea el .tplr de un
            // personaje que antes no tenia ninguno (ej. el primer objeto de Calamity colocado
            // en un personaje 100% vanilla) - solo faltaba volver a leerlo aqui.
            HasCalamityData = _loaded.TplrPath != null;
            StatusMessage = LocalizationService.Instance.Format("status_saved", Path.GetFileName(_loaded.PlrPath)) +
                (_loaded.TplrPath != null ? $" + {Path.GetFileName(_loaded.TplrPath)}" : "");
            IsDirty = false;
            SaveConfirmationVisible = true;
            _saveConfirmationTimer.Stop();
            _saveConfirmationTimer.Start();
            _lastSavedLocal = DateTime.Now;
            RefreshLastSavedText();
            GlobalErrorMessage = null; // H-3: un guardado con exito limpia cualquier error global anterior
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_saving", ex.Message);
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
        finally
        {
            UndoLastSaveCommand.NotifyCanExecuteChanged(); // Save() acaba de crear/renovar el .bak real
        }
    }

    // T-C (segunda auditoria de Opus, Fable): "el .bak existe de verdad desde el Bloque 0 pero
    // no hay ninguna forma real de usarlo desde la UI - un usuario que se de cuenta tarde de que
    // ha guardado algo que no queria no tiene mas remedio que ir a buscarlo a mano en el
    // Explorador de archivos". Restaura el/los .bak reales que CharacterFileService.Save ya deja
    // (WriteAtomic) y recarga - mismo mecanismo real que "Cargar personaje...", no uno nuevo.
    // H3-01 (tercera auditoria, Fable): "Deshacer" tiraba ediciones sin guardar sin preguntar -
    // T-B cerro este mismo agujero en los otros 3 puntos de entrada reales (Inicio, "Cargar
    // personaje...", Ctrl+O), pero "Deshacer ultimo guardado" (un clic, siempre visible en la
    // cabecera global) se quedo fuera. Mismo gancho real que los otros 3 - ConfirmDiscardChanges
    // solo cuando de verdad hay algo que perder (IsDirty).
    [RelayCommand(CanExecute = nameof(CanUndoLastSave))]
    private void UndoLastSave()
    {
        if (_loaded == null) return;
        if (IsDirty && ConfirmDiscardChanges?.Invoke() == false) return;
        string plrPath = _loaded.PlrPath;
        string plrBak = plrPath + ".bak";
        if (!File.Exists(plrBak)) return;
        try
        {
            File.Copy(plrBak, plrPath, overwrite: true);
            string tplrPath = _loaded.TplrPath ?? Path.ChangeExtension(plrPath, ".tplr");
            string tplrBak = tplrPath + ".bak";
            if (File.Exists(tplrBak))
            {
                File.Copy(tplrBak, tplrPath, overwrite: true);
            }
            else if (File.Exists(tplrPath))
            {
                // H3-02 (tercera auditoria, Fable): WriteAtomic solo genera un .bak real cuando
                // el fichero YA EXISTIA (File.Replace) - si no hay .tplr.bak pero SI hay .tplr,
                // este .tplr nacio en el MISMO guardado que se esta deshaciendo (no habia
                // ninguno antes). Dejarlo intacto resucitaria su contenido de Calamity al
                // recargar (MergeAll lo fusiona igual), aunque el .plr ya se haya revertido -
                // "Deshecho" seria un mensaje falso. Se borra para volver de verdad al estado
                // real de antes de ese guardado (sin ningun .tplr).
                File.Delete(tplrPath);
            }
            string nombreAntesDeRecargar = CharacterName ?? plrPath;
            LoadFromPath(plrPath);
            // LoadFromPath NUNCA relanza (traga sus propias excepciones, T-22) - si la recarga
            // en si fallo, ya dejo su propio StatusMessage/GlobalErrorMessage de error reales;
            // pisarlo aqui con un mensaje de exito falso seria peor que no decir nada.
            if (IsCharacterLoaded) StatusMessage = LocalizationService.Instance.Format("status_undo_last_save", nombreAntesDeRecargar);
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_undo_last_save", ex.Message);
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
    }

    private bool CanUndoLastSave() => _loaded != null && File.Exists(_loaded.PlrPath + ".bak");

    private void RebuildContainers()
    {
        Containers.Clear();
        EquipmentGroup = null;
        StorageGroup = null;
        InventoryContainer = null;
        MountsContainer = null;
        DyesContainer = null;
        CoinsContainer = null;
        AmmoContainer = null;
        Research.Reset();
        // H5-05: la lista de slots reales es NUEVA de cero (personaje distinto, o ninguno) - un
        // resultado de la busqueda anterior apuntando a un ItemSlotViewModel ya descartado no
        // tiene ningun sitio real al que navegar.
        WhereIsItResults.Clear();
        WhereIsItSearchText = string.Empty;
        WhereIsItSummary = string.Empty;
        IsWhereIsItOpen = false;
        if (_loaded == null) { Builds.RefreshOwnership([]); MoneyText = "0"; return; }

        // Contenedores con fusion real de Calamity (mismos 7 que CalamityCharacterSync cubre).
        // Se siguen guardando TODOS en Containers (SyncEditsBackToMerged/AutoEquip los buscan
        // ahi por Key) - las referencias devueltas de mas abajo son solo para la navegacion de
        // 5 pestañas (pregunta a Opus sobre el diseño, 2-sep-2026), presentacion pura, ningun
        // dato nuevo ni fusion de colecciones.
        // Ronda de idioma del 6-sep-2026: Inventario/Banco/Caja fuerte iban a pelo en español
        // mientras que Fragua y Boveda (las dos lineas de abajo) ya usaban el diccionario - las
        // claves "storage_bank"/"storage_safe" existian desde la ronda anterior sin enchufar.
        // Este DisplayName sale en el titulo del panel Editar, en los mensajes de "conjunto
        // cargado en X" y en el selector de almacen.
        InventoryContainer = AddContainer("inventory", LocalizationService.Instance["char_tab_inventory"], _loaded.MergedContainers["inventory"]);
        var bank = AddContainer("bank", LocalizationService.Instance["storage_bank"], _loaded.MergedContainers["bank"]);
        var bank2 = AddContainer("bank2", LocalizationService.Instance["storage_safe"], _loaded.MergedContainers["bank2"]);
        var bank3 = AddContainer("bank3", LocalizationService.Instance["storage_forge"], _loaded.MergedContainers["bank3"]);
        var bank4 = AddContainer("bank4", LocalizationService.Instance["storage_void"], _loaded.MergedContainers["bank4"]);
        // columns: 1 (pregunta a Opus sobre el diseño, quinta pasada: "mascotas etc mejor en
        // vertical") - laterales de la Equipamiento fusionada, una sola columna de 5 filas.
        // Orden real de los 5 slots (Player.miscEquips, confirmado por Opus contra Player.cs
        // Y de forma independiente contra el propio script.js real de Terrasavr -
        // app.TabMiscEquips, slotLabels = ["Pet","Light pet","Minecart","Mount","Hook"], dos
        // fuentes reales coincidentes): 0=Mascota, 1=Mascota de luz, 2=Vagoneta, 3=Montura,
        // 4=Gancho. Restriccion de tipo (petición explícita 2-sep-2026: "solo deberian poderse
        // equipar sus respectivos items") + icono fantasma real por indice.
        SlotKind[] miscEquipKinds = [SlotKind.VanityPet, SlotKind.LightPet, SlotKind.Cart, SlotKind.Mount, SlotKind.Hook];
        string?[] miscEquipGhosts = ["pet", "pet_light", "minecart", "mount", "hook"];
        // minCell: 32 (pedido explicito 2-sep-2026: "los cuadrados... ya que ahora sale
        // scroll y no lo queremos") - 5+5 slots apilados en una columna estrecha no siempre
        // caben a 40px minimo en la resolucion real del usuario; celdas mas pequeñas en vez
        // de aceptar el scroll (el ScrollViewer de seguridad se queda de todos modos, para
        // ventanas aun mas pequeñas). maxCell: 56 (consulta a Opus, septima pasada - bug real
        // medido con el arnes: sin techo propio, esta columna UNICA crecia sin limite hacia
        // el techo universal (90) en cuanto sobraba alto, robandole sitio real a la columna
        // central "Auto"+"*" de Armadura/Accesorios - ver ContainerViewModel.MaxCell).
        MountsContainer = AddContainer("miscEquips", LocalizationService.Instance["storage_misc_equips"], _loaded.MergedContainers["miscEquips"], columns: 1,
            slotKinds: miscEquipKinds, ghostIcons: miscEquipGhosts, minCell: 32, maxCell: 56);
        // Los 5 tintes van emparejados 1:1 con los 5 slots de arriba, pero un tinte SIEMPRE
        // es solo un tinte (dye>0) sea cual sea el equipo al que este emparejado - mismo
        // SlotKind.Dye y mismo ghost "dye" en los 5, a diferencia del contenedor de arriba.
        DyesContainer = AddContainer("miscDyes", LocalizationService.Instance["storage_misc_dyes"], _loaded.MergedContainers["miscDyes"], columns: 1,
            slotKinds: [SlotKind.Dye, SlotKind.Dye, SlotKind.Dye, SlotKind.Dye, SlotKind.Dye],
            ghostIcons: ["dye", "dye", "dye", "dye", "dye"], minCell: 32, maxCell: 56);

        // Monedas/municion: vanilla-only (ver CalamityCharacterSync.cs para el alcance
        // documentado - la app JS tampoco los sincroniza con Calamity, solo los protege). Sin
        // ghost real (el propio juego tampoco dibuja uno en estos dos contextos, ver
        // ItemSlot.cs real - no se inventa ninguno).
        CoinsContainer = AddContainer("coins", LocalizationService.Instance["storage_coins"], _loaded.Character.Coins.ToGameItems(),
            slotKinds: [SlotKind.Coin, SlotKind.Coin, SlotKind.Coin, SlotKind.Coin]);
        // H5-10 (quinta auditoria de Opus): "Dinero total - hoy hay que hacer la cuenta a
        // mano". Por ID real (71/72/73/74 = cobre/plata/oro/platino, IsACoin real ya
        // establecido), no por posicion en el array - un slot vacio o con el objeto
        // equivocado no rompe nada, simplemente no suma.
        foreach (var slot in CoinsContainer.Slots)
            slot.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(ItemSlotViewModel.ItemId) or nameof(ItemSlotViewModel.Count)) RefreshMoneyText(); };
        RefreshMoneyText();
        AmmoContainer = AddContainer("ammo", LocalizationService.Instance["storage_ammo"], _loaded.Character.Ammo.ToGameItems(),
            slotKinds: [SlotKind.Ammo, SlotKind.Ammo, SlotKind.Ammo, SlotKind.Ammo]);

        StorageGroup = new StorageGroupViewModel(bank, bank2, bank3, bank4);

        // Equipo puesto + los 3 loadouts reales seleccionables (Version>=269, si no
        // Loadouts.Length==0) - consolidados en una unica pantalla "Equipamiento" con
        // selector, ver EquipmentGroupViewModel (antes eran 12 pestañas planas mas aqui
        // mismo, 21 pestañas en total - pedido explicito 2-sep-2026 tras el amontonamiento
        // real al reducir la ventana: "¿es necesario que haya tantos botones?").
        EquipmentGroup = new EquipmentGroupViewModel(_service, RequestPickForSlot, _loaded.MergedContainers, _loaded.Character.Loadouts.Length, OnSlotItemChanged);
        // EquipmentGroup se RECREA entera cada carga (a diferencia de Buffs, que reutiliza la
        // misma instancia) - los slots de sus contenedores nunca pasan por AddContainer, asi
        // que se enganchan aqui, el unico sitio real donde MainViewModel ve la instancia nueva.
        foreach (var c in EquipmentGroup.AllContainers)
            foreach (var s in c.Slots)
                HookSlotEditing(s);

        Research.LoadFrom(_loaded.Character);
        // Bd-d: foto fija de "lo que ya se posee" para la pestaña Builds, recalculada tambien
        // al entrar en ella (ver OnSelectedTabIndexChanged) - aqui cubre el caso real de cargar
        // un personaje distinto mientras Builds ya esta a la vista.
        Builds.RefreshOwnership([.. Containers, .. EquipmentGroup.AllContainers]);
    }

    // "Investigar todo" - regla de negocio real extraida a ResearchAllService (auditoria de
    // Opus, Bloque 6, T-20) - aqui solo queda la orquestacion (recargar Research, avisar).
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void ResearchAll()
    {
        if (_loaded == null) return;
        ResearchAllService.Apply(_loaded, _service);
        Research.LoadFrom(_loaded.Character);
        // Segunda auditoria de Opus (Fable), B-4 - BUG REAL: ResearchAllService.Apply muta
        // _loaded.Character.Research DIRECTAMENTE, sin pasar por ningun ViewModel observable -
        // IsDirty se quedaba en false pese a escribir miles de entradas reales. El usuario leia
        // "pulsa Guardar para conservarlo", cerraba la ventana sin guardar, y OnWindowClosing no
        // preguntaba nada (IsDirty==false) - perdida silenciosa de todo el trabajo. Mismo
        // escenario que N-2 (Bloque 0) existia para cerrar.
        MarkDirty();
        StatusMessage = LocalizationService.Instance.Format("status_research_all_applied", _loaded.Character.Research.Count);
    }

    // Auto-equipar desde el panel Builds - regla de negocio real extraida a
    // AutoEquipService (auditoria de Opus, Bloque 6, T-20) - aqui solo queda la orquestacion
    // (StatusMessage, saltar a Personaje).
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void AutoEquip(BuildClassGear? gear)
    {
        if (_loaded == null || gear == null || EquipmentGroup == null) return;

        // H5-01 (quinta auditoria de Opus): Auto-equipar podia "reemplazar SIN CONFIRMACION la
        // armadura y los accesorios puestos" (tooltip real, ya existente) sin ninguna vuelta
        // atras real - ahora es UNA sola entrada de deshacer, cubriendo Inventario Y el
        // Equipamiento entero (AutoEquipService.Apply toca ambos).
        AutoEquipService.Result result = default;
        RunAsUndoableBatch(LocalizationService.Instance["action_auto_equip"], EquipmentGroup.AllContainers.Append(Containers.First(c => c.Key == "inventory")),
            () => result = AutoEquipService.Apply(gear, EquipmentGroup, Containers.First(c => c.Key == "inventory"), _service));
        // Bd-c (segunda auditoria de Opus, Fable): "sin resolver" y "sin hueco libre" son
        // causas reales distintas (una no tiene arreglo por parte del usuario, la otra si -
        // vaciar hueco en el Inventario) - se cuentan y se dicen aparte en vez de fundirse en
        // un unico "sin resolver o sin hueco libre".
        var partes = new List<string>();
        if (result.Unresolved > 0) partes.Add(LocalizationService.Instance.Format("autoequip_unresolved", result.Unresolved));
        if (result.NoSlot > 0) partes.Add(LocalizationService.Instance.Format("autoequip_no_slot", result.NoSlot));
        string mensaje = partes.Count > 0
            ? LocalizationService.Instance.Format("status_autoequip_with_issues", result.Placed, string.Join(", ", partes))
            : LocalizationService.Instance.Format("status_autoequip_clean", result.Placed);
        // Bd-f (segunda auditoria de Opus, Fable): "avisar si el personaje no tiene .tplr" -
        // colocar equipo de Calamity Mod (pid con "/", ver BuildsViewModel.ResolveItem) en un
        // personaje sin datos de Calamity conocidos (HasCalamityData, real - TplrPath!=null) no
        // falla ni se pierde (CharacterFileService.Save crea el .tplr solo con guardar, ver
        // T-C), pero si el mod NO esta realmente instalado en el juego del usuario, esos
        // objetos no se reconoceran ahi - aviso informativo, no bloqueante (esta app no tiene
        // forma real de saber si el mod esta instalado, solo si este personaje ya lo uso antes).
        bool esBuildDeCalamity = gear.Armor.Concat(gear.Weapons).Concat(gear.Accessories).Any(i => i.Pid?.Contains('/') == true);
        if (esBuildDeCalamity && !HasCalamityData)
            mensaje = LocalizationService.Instance["warning_no_calamity_data"] + mensaje;
        StatusMessage = mensaje;
        SelectedTabIndex = (int)AppTab.Personaje;
    }

    // A-d (segunda auditoria de Opus, Fable): "mover todo al banco" - mueve el Inventario
    // entero al Almacen que este seleccionado ahora mismo (StorageGroup.Current, el mismo
    // selector de pildoras de Banco/Caja fuerte/Fragua/Boveda ya existente) en vez de fijarlo
    // solo a "Banco" - generaliza igual de bien a los 4 almacenes sin inventar un cuarto boton
    // por cada uno.
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void MoveInventoryToStorage()
    {
        if (InventoryContainer == null || StorageGroup == null) return;
        // H5-01: una sola entrada de deshacer para todo el traslado (toca 2 contenedores a la
        // vez - origen y destino - por eso ninguno de los dos por separado bastaria).
        int moved = 0;
        var destino = StorageGroup.Current;
        RunAsUndoableBatch(LocalizationService.Instance["action_move_all_to_storage"], [InventoryContainer, destino], () => moved = InventoryContainer.MoveAllTo(destino));
        if (moved == 0)
        {
            StatusMessage = LocalizationService.Instance["status_nothing_to_move"];
            return;
        }
        MarkDirty();
        // H3-17 (tercera auditoria de Opus, Fable): el verbo YA concordaba con el numero real
        // ("Movido"/"Movidos") pero el sustantivo se quedaba en el placeholder literal
        // "objeto(s)" sin concordar nunca de verdad (singular real leia "Movido 1 objeto(s)").
        bool plural = moved != 1;
        StatusMessage = LocalizationService.Instance.Format(plural ? "status_moved_plural" : "status_moved_singular", moved);
    }

    // H5-12 (quinta auditoria de Opus): "un clic en una tarjeta de la Libreria no hace
    // absolutamente nada... la unica via real es arrastrar, gesto mas caro que nada anuncia".
    // Doble clic real (MainWindow.xaml.cs, OnLibraryCardClick) - gesto rapido sin necesitar
    // seleccionar ningun slot antes, a diferencia del clic simple (coloca en ItemEdit.Slot).
    public void PlaceInFirstFreeInventorySlot(int itemId)
    {
        var target = InventoryContainer?.Slots.FirstOrDefault(s => s.IsEmpty);
        if (target == null)
        {
            StatusMessage = LocalizationService.Instance["status_inventory_full"];
            return;
        }
        target.PlaceItem(itemId);
    }

    // C-11 (informe de pulido final, cierra L4): gemelo real de PlaceInFirstFreeInventorySlot,
    // para el doble clic de la Libreria de buffs (OnBuffLibraryCardClick, MainWindow.xaml.cs) -
    // "pasa lo mismo con la pestaña buff" (un clic ahi no hacia nada, unica via real era
    // arrastrar).
    public void PlaceInFirstFreeBuffSlot(int buffId)
    {
        var target = Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
        if (target == null)
        {
            StatusMessage = LocalizationService.Instance["status_no_free_buff_slot"];
            return;
        }
        target.PlaceBuff(buffId);
    }

    // H5-03 (quinta auditoria de Opus): "no existe guardar/cargar conjuntos de objetos, que en
    // el Terrasavr original SI es una funcion de primera clase" (app.io.IoSave/IoLoad reales -
    // ver ItemSetFile). El dialogo de fichero real vive en la View (MainWindow.xaml.cs, mismo
    // criterio ya establecido: MainViewModel es headless de verdad) - aqui solo la logica real
    // sobre una ruta ya elegida.
    public void SaveItemSet(ContainerViewModel container, string path)
    {
        try
        {
            var file = ItemSetFile.FromItems(container.Slots.Select(s => s.Item), _service.CalamityCatalog, _service.RoguePrefixCatalog);
            File.WriteAllBytes(path, file.Write());
            StatusMessage = LocalizationService.Instance.Format("status_set_saved", container.DisplayName, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_saving_set", ex.Message);
            GlobalErrorMessage = StatusMessage;
        }
    }

    // append=false ("Cargar"): reemplaza el contenedor entero, slot a slot, igual que el
    // original real (Ga.onBinaryData: "if (!this.append) ... d.clear()" antes de rellenar).
    // append=true ("Añadir"): solo rellena huecos libres, sin tocar lo que ya hay puesto -
    // mismo criterio real que ContainerViewModel.MoveAllTo. Una sola entrada de deshacer para
    // todo el conjunto (H5-01), pedido explicito del informe ("cargar un conjunto es una
    // entrada mas del historial, deshacible").
    public void LoadItemSet(ContainerViewModel container, string path, bool append)
    {
        try
        {
            var file = ItemSetFile.Read(File.ReadAllBytes(path));
            var items = file.ToItems(_service.CalamityCatalog, _service.RoguePrefixCatalog);

            RunAsUndoableBatch(LocalizationService.Instance[append ? "action_add_set" : "action_load_set"], [container], () =>
            {
                if (append)
                {
                    int di = 0;
                    foreach (var item in items)
                    {
                        if (item.IsEmpty) continue;
                        while (di < container.Slots.Count && !container.Slots[di].IsEmpty) di++;
                        if (di >= container.Slots.Count) break;
                        container.Slots[di].UpdateFrom(item);
                        di++;
                    }
                }
                else
                {
                    for (int i = 0; i < container.Slots.Count; i++)
                        container.Slots[i].UpdateFrom(i < items.Count ? items[i] : GameItem.Empty);
                }
            });

            StatusMessage = LocalizationService.Instance.Format(append ? "status_set_added_to" : "status_set_loaded_into", container.DisplayName, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_loading_set", ex.Message);
            GlobalErrorMessage = StatusMessage;
        }
    }

    // Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de guardar
    // json ni tampoco cargar para guardar combinaciones de buff" - gemelo real de
    // SaveItemSet/LoadItemSet (H5-03) para BuffContainerViewModel/PlrBuff en vez de
    // ContainerViewModel/GameItem, mismo formato/criterio real (BuffSetFile, portabilidad
    // vanilla por id / Calamity por mod+nombre interno).
    public void SaveBuffSet(BuffContainerViewModel container, string path)
    {
        try
        {
            var file = BuffSetFile.FromBuffs(container.Slots.Select(s => (s.Buff.Id, s.Buff.Time)), _service.CalamityBuffCatalog);
            File.WriteAllBytes(path, file.Write());
            StatusMessage = LocalizationService.Instance.Format("status_set_saved", container.DisplayName, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_saving_buff_set", ex.Message);
            GlobalErrorMessage = StatusMessage;
        }
    }

    // append=false ("Cargar"): reemplaza el contenedor entero, slot a slot (RestoreExact, sin
    // comprobar duplicados entre si - un reemplazo total parte de un contenedor ya vaciado de
    // verdad). append=true ("Añadir"): solo rellena huecos libres, SIN colocar un buff que ya
    // este activo en otro slot (Bu-b real, Terraria no permite dos instancias del mismo buff -
    // PasteBuff ya hace esa comprobacion real).
    public void LoadBuffSet(BuffContainerViewModel container, string path, bool append)
    {
        try
        {
            var file = BuffSetFile.Read(File.ReadAllBytes(path));
            var buffs = file.ToBuffs(_service.CalamityBuffCatalog);

            RunAsUndoableBuffBatch(LocalizationService.Instance[append ? "action_add_buff_set" : "action_load_buff_set"], container, () =>
            {
                if (append)
                {
                    int di = 0;
                    foreach (var (id, time) in buffs)
                    {
                        if (id == 0) continue;
                        while (di < container.Slots.Count && !container.Slots[di].IsEmpty) di++;
                        if (di >= container.Slots.Count) break;
                        container.Slots[di].PasteBuff(id, time); // respeta "sin duplicados" real
                        di++;
                    }
                }
                else
                {
                    for (int i = 0; i < container.Slots.Count; i++)
                    {
                        var (id, time) = i < buffs.Count ? buffs[i] : (0, 0);
                        container.Slots[i].RestoreExact(id, time);
                    }
                }
            });

            StatusMessage = LocalizationService.Instance.Format(append ? "status_buff_set_added_to" : "status_buff_set_loaded_into", container.DisplayName, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_loading_buff_set", ex.Message);
            GlobalErrorMessage = StatusMessage;
        }
    }

    // Gemelo real de RunAsUndoableBatch (objetos) para buffs - una unica entrada de Deshacer
    // para todo el conjunto (mismo pedido explicito ya cerrado en H5-03/H5-01).
    private void RunAsUndoableBuffBatch(string label, BuffContainerViewModel container, Action action)
    {
        var before = container.Slots.Select(s => (s.Buff.Id, s.Buff.Time)).ToArray();
        action();
        var after = container.Slots.Select(s => (s.Buff.Id, s.Buff.Time)).ToArray();
        var slots = container.Slots;

        UndoStack.Push(new UndoEntry
        {
            Label = label,
            Undo = () => { for (int i = 0; i < slots.Count; i++) slots[i].RestoreExact(before[i].Item1, before[i].Item2); },
            Redo = () => { for (int i = 0; i < slots.Count; i++) slots[i].RestoreExact(after[i].Item1, after[i].Item2); },
        });
    }

    // Los primeros 10 slots reales de "inventory" son la barra rapida (Player.inventory[0..9]
    // en el propio Terraria - confirmado en Player.cs decompilado, "Hotbar1".."Hotbar0" son 10
    // triggers reales) - contorno verde de "equipado" tambien ahi, igual que en Equipamiento
    // (pedido explicito 2-sep-2026, confirmado con el usuario tras preguntar: el .plr no
    // guarda ningun campo real de "arma empuñada ahora mismo" fiable, asi que la barra rapida
    // entera es el criterio, no un unico slot).
    private const int HotbarSlotCount = 10;

    // columns (pregunta a Opus sobre el diseño, quinta pasada - fusion de Equipamiento con
    // Monturas/Monedas como laterales): default 10 para los contenedores de siempre; los
    // laterales que se quieren verticales (Mascota/Montura/Gancho, Tintes) pasan columns: 1.
    // slotKinds/ghostIcons (pedido explicito 2-sep-2026: "los slots de tintes, gancho,
    // vagoneta, montura y mascota solo deberian poderse equipar sus respectivos items" +
    // iconos fantasma reales) - null = sin restriccion/sin ghost en todos los slots (el caso
    // de siempre: Inventario/Banco/Caja fuerte/Fragua/Boveda), o un array del mismo tamaño
    // que items para fijarlo por indice (miscEquips/miscDyes/coins/ammo).
    private ContainerViewModel AddContainer(string key, string displayName, GameItem[] items, int columns = 10,
        SlotKind[]? slotKinds = null, string?[]? ghostIcons = null, double minCell = 40, double maxCell = 90)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
        {
            bool isEquipped = key == "inventory" && i < HotbarSlotCount;
            var kind = slotKinds != null && i < slotKinds.Length ? slotKinds[i] : SlotKind.None;
            var ghost = ghostIcons != null && i < ghostIcons.Length ? ghostIcons[i] : null;
            var slot = new ItemSlotViewModel(_service, i, displayName, items[i], RequestPickForSlot, isEquipped, kind, ghost, onItemChanged: OnSlotItemChanged);
            HookSlotEditing(slot);
            slots.Add(slot);
        }
        var container = new ContainerViewModel(key, displayName, slots) { Columns = columns, MinCell = minCell, MaxCell = maxCell };
        Containers.Add(container);
        return container;
    }

    // Vuelca lo que haya en las colecciones enlazadas a la UI de vuelta a MergedContainers
    // antes de guardar.
    //
    // Bug real encontrado y arreglado (fase 4 del rework, 1-sep-2026): "coins"/"ammo" NUNCA
    // estan en MergedContainers (CalamityCharacterSync.MergeAll no los toca, son vanilla-only
    // a proposito - ver el comentario de RebuildContainers) - el "continue" de abajo los
    // saltaba en silencio, asi que CUALQUIER edicion de Monedas/Municion se perdia al
    // guardar sin ningun aviso. Se escriben aparte, directo a PlrCharacter.Coins/Ammo.
    private void SyncEditsBackToMerged()
    {
        if (_loaded == null) return;
        SyncContainersBackToMerged(Containers);
        if (EquipmentGroup != null) SyncContainersBackToMerged(EquipmentGroup.AllContainers);
    }

    private void SyncContainersBackToMerged(IEnumerable<ContainerViewModel> containers)
    {
        if (_loaded == null) return;
        foreach (var container in containers)
        {
            if (container.Key == "coins") { CopySlotsInto(_loaded.Character.Coins, container.Slots); continue; }
            if (container.Key == "ammo") { CopySlotsInto(_loaded.Character.Ammo, container.Slots); continue; }
            if (!_loaded.MergedContainers.TryGetValue(container.Key, out var arr)) continue;
            for (int i = 0; i < container.Slots.Count && i < arr.Length; i++)
                arr[i] = container.Slots[i].Item;
        }
    }

    private static void CopySlotsInto(PlrItemSlot[] target, IReadOnlyList<ItemSlotViewModel> source)
    {
        var items = source.Select(s => s.Item).ToArray().ToPlrItemSlots();
        for (int i = 0; i < target.Length && i < items.Length; i++) target[i] = items[i];
    }

    partial void OnIsCharacterLoadedChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        ResearchAllCommand.NotifyCanExecuteChanged();
        AutoEquipCommand.NotifyCanExecuteChanged();
        // A-d: se olvido aqui al añadirlo - sin este aviso el boton "Mover todo al almacén" se
        // quedaba con aspecto deshabilitado hasta el primer requery automatico de WPF (foco/
        // raton), encontrado al verificar con captura real, no al escribir el codigo.
        MoveInventoryToStorageCommand.NotifyCanExecuteChanged();
        ToggleWhereIsItCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowVitalsStrip));
    }
}
