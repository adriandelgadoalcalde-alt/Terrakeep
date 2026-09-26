using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Auditoria de Opus, I-1: "Inicio" no practicaba lo que predicaba (P1/P2, todo a mano de
// golpe) - un personaje real en Documents\...\Players no aparecia en ningun sitio hasta abrir
// el Explorador de archivos a mano. Una fila real por cada .plr encontrado, con lo mismo que
// ya se ve al elegirlo a ciegas en el dialogo: nombre, dificultad, insignia de Calamity (mismo
// criterio que CharacterFileService.Load, ".tplr con el mismo nombre al lado") y fecha real de
// ultima modificacion - mas el doll de cuerpo completo ya real de PlayerPreviewRenderer
// (colores base + armadura/vanidad real puesta, ver EquipmentAppearanceResolver), sin
// inventar ningun dato que el .plr no tenga de verdad.
public sealed partial class CharacterListEntryViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string FilePath { get; }
    public string Name { get; }
    public string DifficultyLabel { get; }
    // Encargo del usuario 4-sep-2026 - tres hechos DISTINTOS donde antes habia uno solo mal
    // nombrado (ver ESPEC-sprites-botones-badges.md#C):
    //   IsTModLoader = existe un .tplr hermano. El hecho PRINCIPAL ("verdaderamente de tmodloader").
    //   IsCalamity   = ademas hay un objeto o un buff de Calamity REAL dentro de ese .tplr (mismo
    //                  criterio exacto que CharacterFileService.Save, resuelto sobre el NBT crudo).
    //                  NO sustituye a IsTModLoader: un personaje puede llevar las dos insignias.
    //   IsVanilla    = no hay .tplr. El usuario lo pidio explicitamente ("al igual que cuando un
    //                  personaje es vanilla que tenga dicha etiqueta").
    public bool IsTModLoader { get; }
    public bool IsCalamity { get; }
    public bool IsVanilla => !IsTModLoader;
    // Regalo de la clave usedMods real del .tplr (la escribe tModLoader en cada guardado,
    // PlayerIO.cs:67) - la lista de mods que estaban cargados la ultima vez.
    //
    // Oleada del 6-sep-2026 (bloque INI-05 del arnes) - DOS BUGS REALES de idioma en la misma
    // linea, los dos invisibles para el barrido A10-IDIOMA-BARRIDO porque un ToolTip no es un
    // TextBlock de la ventana:
    //   a) El texto se componia UNA sola vez, en el constructor, con el idioma de ese instante -
    //      cambiar a ingles en vivo dejaba "Mods usados la última vez: ..." tal cual.
    //   b) El caso NORMAL (un .tplr SIN lista de mods, que es justo lo que escribe el propio
    //      Terrakeep) devolvia null, y el XAML tapaba ese null con un TargetNullValue en
    //      español DURO - o sea, la insignia "tModLoader" enseñaba una frase en español
    //      SIEMPRE, tambien con la app entera en ingles.
    // Ahora nunca es null (ese caso tiene su propia clave real, tt_tmodloader_badge) y se
    // compone al leerlo; `Loc` es el mismo singleton que refresca los bindings al cambiar de
    // idioma, asi que un ToolTip que se vuelva a abrir sale ya en el idioma nuevo.
    public string UsedModsTooltip => _usedMods is { Count: > 0 }
        ? Loc["character_used_mods"] + string.Join(", ", _usedMods)
        : Loc["tt_tmodloader_badge"];

    private readonly IReadOnlyList<string>? _usedMods;
    public string LastModifiedText { get; }
    [ObservableProperty] private WriteableBitmap _preview;

    // Hover en Inicio ("que ande solo al pasar el raton, con brazos" - bitacora.md 21-sep-2026).
    // Terraria/GameContent/UI/Elements/UICharacterListItem.cs real, MouseOver/MouseOut (338-362):
    // "_playerPanel.SetAnimated(animated: true/false)" - el trigger real es el HOVER de la propia
    // tarjeta (MouseOver/MouseOut de UIPanel), nunca un boton manual (ese es un añadido aparte de
    // Apariencia, AppearanceViewModel.ToggleWalkAnimation, que se queda igual). Reusa el MISMO
    // ciclo de 14 filas reales (AppearanceViewModel.WalkCycleRows) y la MISMA cadencia (70ms,
    // corregida 26-sep-2026 junto con Apariencia - hallazgo ParidadPersonaje-Fase1) que ya tiene
    // Apariencia, para no duplicar ninguna secuencia a mano.
    //
    // Datos crudos guardados aqui (no solo en el constructor) porque re-renderizar en cada tick
    // necesita los MISMOS argumentos reales que PlayerPreviewRenderer.Render ya recibio una vez -
    // sin volver a leer el .plr, sin ningun dato nuevo.
    private readonly int _hairStyle;
    private readonly byte _skinVariant;
    private readonly PlayerPreviewRenderer.PlayerColors _colors;
    private readonly PlayerPreviewRenderer.EquippedArmor _armor;
    // PortSeleccion Encargo2 (25-sep-2026): los 7 sprites de accesorio real ya resueltos por
    // EquipmentAppearanceResolver.ResolveAccessories (Encargo1) - mismo criterio que _armor de
    // arriba, guardado para poder re-renderizar en cada tick del hover sin volver a resolver.
    private readonly EquippedAccessories _accessories;

    // ParidadPersonaje Fase1 (25-sep-2026): SOLO para pruebas (Terrakeep.App.ViewModels.Tests -
    // sin InternalsVisibleTo configurado hacia el arnes, mismo criterio real ya establecido por
    // CharacterFileService.DebugCorruptPlrBytesBeforeVerify) - deja comparar de verdad el
    // EquippedAccessories resultante de este camino (Inicio) contra el de
    // AppearanceViewModel.AccesoriosParaPruebas (Personaje > Apariencia) en el mismo test de
    // integracion, en vez de solo comparar pixeles renderizados.
    public EquippedAccessories AccesoriosParaPruebas => _accessories;

    // Mascota real equipada - ANIMADA de verdad durante el hover cuando PetAnimationCatalog
    // conoce el objeto (PetPreviewRenderer + PetAnimationDriver, ver el comentario real de esas
    // clases para la cita completa del decompilado), icono estatico (VanillaIconResolver) como
    // reserva cuando no. Ver EquipmentAppearanceResolver.ResolvePet para el alcance deliberado
    // documentado (mascotas de luz excluidas, igual que vanilla).
    [ObservableProperty] private ImageSource? _petImage;
    private readonly PetPreview? _petPreview;
    private readonly PetAnimationDriver? _petAnimationDriver;

    // Offset ADICIONAL propio de cada mascota + espejo horizontal (PortSeleccion Encargo4,
    // 25-sep-2026) - capa DISTINTA y complementaria a la formula GENERICA de posicion
    // mascota-vs-personaje que vive en MainWindow.xaml (Margin fijo del Image de PetImage): esa
    // formula genérica es la misma para las 63 mascotas, esta de aqui es el ajuste fino REAL por
    // mascota que Terraria aplica encima (SettingsForCharacterPreview.ApplyTo, decompilado real:
    // "proj.position += Offset"/"proj.spriteDirection = SpriteDirection").
    //
    // PortSeleccion Encargo5 (26-sep-2026): AHORA SI cambian durante el hover en las mascotas con
    // delegado de codigo custom (PetAnimationEntry.Code != null, ver PetCustomAnimationCode) - por
    // eso son [ObservableProperty] en vez de constantes de solo lectura. "_petBaseOffsetX/Y" es el
    // valor ESTATICO del catalogo (el mismo de siempre, PortSeleccion Encargo4); RefreshPetOffset
    // le suma la contribucion dinamica real de Float/SlimePet/BerniePet mientras el raton esta
    // encima, y vuelve exactamente a la base al salir del hover (SetHovering(false) llama tambien
    // a RefreshPetOffset con "activo=false", que Evaluate resuelve siempre a (0,0)). Sin mascota
    // animada catalogada o sin "code" real, esto no cambia nada respecto al Encargo4: se queda en
    // el valor base fijo de siempre.
    [ObservableProperty] private double _petOffsetX;
    [ObservableProperty] private double _petOffsetY;
    private readonly double _petBaseOffsetX;
    private readonly double _petBaseOffsetY;

    // PortSeleccion Encargo6 (26-sep-2026): cierra el pendiente real dejado por el Encargo5 -
    // angulo de "spin" de FloatAndSpinWhenWalking (PetCustomAnimationCode.EvaluateRotationDegrees,
    // ver la cita real del decompilado ahi), en GRADOS listo para bindear directo a
    // RotateTransform.Angle. Se recalcula en el mismo sitio que PetOffsetX/Y (RefreshPetOffset,
    // constructor + cada tick del timer de hover) porque depende del mismo "elapsedTicks" real.
    // Para las 61 mascotas sin este delegado siempre es 0 (RotateTransform identidad).
    [ObservableProperty] private double _petRotationDegrees;
    // Expuesto como double (1 o -1), listo para bindear directo a ScaleTransform.ScaleX en vez de
    // un bool + converter - es literalmente el mismo campo "SpriteDirection" del decompilado.
    public double PetSpriteDirection { get; } = 1;

    // Timer PROPIO de esta tarjeta, arrancado SOLO mientras el raton esta encima y parado de
    // verdad al salir (MainWindow.xaml: MouseEnter/MouseLeave + Unloaded como red de seguridad
    // adicional) - mismo bug real ya cerrado en Apariencia (AppearanceViewModel.
    // StopWalkAnimation, "Terrakeep congelado" 8h+ por un DispatcherTimer que no se paraba solo,
    // ver bitacora.md 21-sep-2026 madrugada) NUNCA se repite aqui: un timer por tarjeta, parado
    // en cuanto deja de estar en hover, sin ningun camino donde pueda quedar corriendo solo.
    //
    // Bug real encontrado EN VIVO (usuario, comparando con vanilla): la animacion se congelaba
    // en el primer fotograma de andar a los pocos cientos de ms, sin volver a avanzar nunca -
    // instrumentado con logging temporal (diag-hover.log) que confirmo que el Tick del timer NO
    // se disparaba NUNCA en la app real (aunque SI lo hacia en el arnes HOMEHOVER_SOLO, que
    // aislaba mal la variable - ver el comentario de esa prueba). Causa real: este campo se
    // inicializaba en el CONSTRUCTOR de la clase, y HomeViewModel.ScanCharacters (que construye
    // cada CharacterListEntryViewModel, HomeViewModel.cs:389) corre dentro de un `Task.Run` en
    // un hilo de FONDO (RefreshAsync) - "new DispatcherTimer()" alli se ataba al Dispatcher
    // efimero de ESE hilo del pool, que nunca se bombea, asi que el Tick jamas llegaba a
    // dispararse. Arreglo real: el timer se crea DE VERDAD la primera vez que hace falta, dentro
    // de SetHovering (que SIEMPRE corre en el hilo de UI real, invocado desde MouseEnter/
    // MouseLeave/Unloaded en MainWindow.xaml.cs) - "new DispatcherTimer()" alli se ata al
    // Dispatcher real de la ventana, el que si esta bombeando siempre.
    private DispatcherTimer? _hoverWalkTimer;
    private int _walkCycleIndex;
    private bool _isHovering;

    public void SetHovering(bool hovering)
    {
        if (_isHovering == hovering) return;
        _isHovering = hovering;
        if (hovering)
        {
            _walkCycleIndex = 0;
            _petAnimationDriver?.Reiniciar();
            EnsureHoverWalkTimer().Start();
        }
        else
        {
            _hoverWalkTimer?.Stop();
            _petAnimationDriver?.Reiniciar();
        }
        RefreshPreview();
        RefreshPetImage();
        RefreshPetOffset();
    }

    private DispatcherTimer EnsureHoverWalkTimer()
    {
        if (_hoverWalkTimer != null) return _hoverWalkTimer;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
        timer.Tick += (_, _) =>
        {
            _walkCycleIndex = (_walkCycleIndex + 1) % AppearanceViewModel.WalkCycleRows.Length;
            // Mascota EN VIVO durante el hover (correccion real 21-sep-2026, usuario comparando
            // con vanilla): avanza su propio ciclo real (PetAnimationDriver, cadencia real de
            // SelDelay, NO generica) en cada tick del mismo timer que ya anima piernas/brazos -
            // un unico DispatcherTimer por tarjeta, sin montar uno aparte solo para la mascota.
            // deltaMs tiene que seguir siendo el intervalo REAL del timer de arriba (70ms desde
            // el 26-sep-2026, hallazgo ParidadPersonaje-Fase1) para que la mascota no se desincronice.
            _petAnimationDriver?.Avanzar(70);
            RefreshPreview();
            RefreshPetImage();
            RefreshPetOffset();
        };
        _hoverWalkTimer = timer;
        return timer;
    }

    // PortSeleccion Encargo5 (26-sep-2026): suma la contribucion dinamica real del delegado
    // custom de la mascota (Float/SlimePet/BerniePet, ver PetCustomAnimationCode) al offset BASE
    // ya fijado en el constructor (PortSeleccion Encargo4) - "_isHovering" hace exactamente de
    // "walking" del decompilado (ver el comentario de clase de PetCustomAnimationCode). Sin
    // mascota animada catalogada, o sin "code" real, Evaluate siempre devuelve (0,0) y esto no
    // cambia nada respecto al Encargo4.
    private void RefreshPetOffset()
    {
        string? code = _petPreview?.AnimationEntry?.Code;
        float elapsedTicks = _petAnimationDriver?.ElapsedTicksReal ?? 0f;
        var (dx, dy) = PetCustomAnimationCode.Evaluate(code, elapsedTicks, _isHovering);
        PetOffsetX = _petBaseOffsetX + dx;
        PetOffsetY = _petBaseOffsetY + dy;
        // PortSeleccion Encargo6: mismo "elapsedTicks"/"_isHovering" que el offset de arriba, asi
        // que el angulo avanza en el mismo tick exacto que el bob (ver comentario del campo).
        PetRotationDegrees = PetCustomAnimationCode.EvaluateRotationDegrees(code, elapsedTicks, _isHovering);
    }

    private void RefreshPreview()
    {
        int frame = _isHovering ? AppearanceViewModel.WalkCycleRows[_walkCycleIndex] : 0;
        Preview = PlayerPreviewRenderer.Render(_hairStyle, _skinVariant, _colors, _armor, frame, accessories: _accessories);
    }

    private void RefreshPetImage()
    {
        if (_petPreview?.AnimationEntry is { } entry && _petAnimationDriver is not null)
        {
            var frame = PetPreviewRenderer.RenderFrame(entry, _petAnimationDriver.FrameActualEnCiclo);
            if (frame is not null) { PetImage = frame; return; }
        }
        // Sin animacion real conocida (o el fotograma fallo al recortar) - icono estatico fijo,
        // ya resuelto una unica vez en el constructor y guardado en _petPreview.IconPath.
    }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". HomeViewModel.UpdateCurrentPath la fija
    // comparando FilePath contra el personaje realmente cargado en MainViewModel.
    [ObservableProperty] private bool _isCurrent;

    // Catalogo de rediseño visual T4 (20-sep-2026, "Inicio como escritorio de partida"): dos de
    // los KPI reales de la tarjeta hero de "Continuar" (la tercera es la etapa de la Guia, que
    // vive aparte en HomeViewModel.LastSessionGuideStage - necesita el catalogo completo de la
    // Guia, no solo el .plr). Vida maxima y tiempo jugado SI son datos del propio character que
    // este constructor YA recibe entero, sin ninguna lectura ni calculo extra.
    public int HealthMax { get; }
    // ParidadPersonaje Fase6 (26-sep-2026, GapAnalysis coordinador): la pantalla real "Seleccionar
    // jugador" de Terraria SIEMPRE muestra mana ademas de vida/tiempo jugado - character.ManaMax
    // ya existe en el modelo .plr real (usado en AppearanceViewModel.cs/CompareViewModel.cs/
    // BackupHistoryService.cs) pero nunca se habia expuesto aqui. Mismo criterio que HealthMax de
    // arriba: dato real ya presente en el character que este constructor recibe entero, sin
    // ninguna lectura ni calculo extra.
    public int ManaMax { get; }
    public string PlayTimeText { get; }

    public CharacterListEntryViewModel(string plrPath, PlrCharacter character, bool isTModLoader,
        TplrModSummary? tplr, DateTime lastModifiedUtc, EquipmentAppearanceResolver equipmentAppearance)
    {
        FilePath = plrPath;
        Name = character.Name;
        // Oleada del 6-sep-2026 (Personaje > Apariencia): estos 4 nombres iban a pelo en ingles
        // (y con el nombre INTERNO "Softcore" en vez del "Classic" real del juego) en DOS sitios
        // distintos, la tarjeta de Inicio y el selector de Apariencia - un unico sitio real
        // ahora, con la traduccion real del propio Terraria (ver AppearanceViewModel).
        DifficultyLabel = AppearanceViewModel.DifficultyLabelFor(character.Difficulty);
        IsTModLoader = isTModLoader;
        IsCalamity = tplr?.HasCalamityContent == true;
        _usedMods = tplr?.UsedMods;
        LastModifiedText = lastModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        HealthMax = character.HealthMax;
        ManaMax = character.ManaMax;
        // Misma formula real ya usada en BackupHistoryViewModel.HorasJugadas/CompareViewModel.
        // FormatPlayTime (PlayTimeLow/High son dos UInt32 que juntos forman los ticks reales de
        // .NET).
        long totalTicksJugados = ((long)character.PlayTimeHigh << 32) | character.PlayTimeLow;
        var tiempoJugado = TimeSpan.FromTicks(totalTicksJugados);
        PlayTimeText = tiempoJugado.TotalHours >= 1 ? $"{(int)tiempoJugado.TotalHours}h {tiempoJugado.Minutes}min" : $"{tiempoJugado.Minutes}min";

        PlayerPreviewRenderer.Tint T(byte[] c) => new(c[0], c[1], c[2]);
        var colors = new PlayerPreviewRenderer.PlayerColors(
            T(character.HairColor), T(character.SkinColor), T(character.EyeColor),
            T(character.ShirtColor), T(character.UnderColor), T(character.PantsColor), T(character.ShoesColor));
        // Doll fiel al guardado (pedido explicito del usuario, 3-sep-2026): la armadura/
        // vanidad REAL puesta en loadouts[0] (el mirror de "lo que lleva puesto de verdad"),
        // no solo los 7 colores base.
        // ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugD - character.Loadouts (los otros
        // loadouts guardados) se pasa para el favorito cross-loadout real (Player.
        // GetEffectiveArmor) - ver el comentario completo en
        // EquipmentAppearanceResolver.ResolveEffectiveSlot. Vacio en un personaje sin loadouts,
        // el resolver ya lo trata como "sin datos de otros loadouts" (mismo resultado que antes).
        var armor = equipmentAppearance.Resolve(character.PrimaryLoadout, character.Loadouts);
        // PortSeleccion Encargo2: mismo criterio que armor arriba, pero para los 7 tipos de
        // accesorio (waist/neck/handOn/handOff/back/shield/face) - ver EquippedAccessories.
        // GapAnalysis Encargo H (25-sep-2026): bug de datos real corregido aqui - el Hide[] real
        // (hideVisibleAccessory del juego) NUNCA vive en PrimaryLoadout (el mirror que se pasa
        // arriba para la armadura/vanidad SI tiene el equipo correcto, pero
        // PlrLoadout.CreateEmpty(isPrimary:true) fija su propio Hide=null siempre).
        // ParidadPersonaje Fase4 (26-sep-2026), corregido (requirement
        // 480a9bdd-6d5f-4fa6-935d-46f895e97514): character.ResolveActiveHide() (ver
        // PlrCharacter.cs) SIEMPRE construye el Hide[] activo desde HideVisual1/HideVisual2 -
        // CON o SIN Loadouts. Loadouts[CurrentLoadout].Hide NUNCA es el estado activo fiable
        // (EquipmentLoadout.Swap intercambia ese array al cambiar de loadout, no lo copia), asi
        // que ResolveActiveHide ya no lo usa para nada activo.
        var hide = character.ResolveActiveHide();
        // ParidadPersonaje Fase2 (25-sep-2026): el slot 8 real (armor[8]/armor[18]) solo esta
        // desbloqueado con el Corazon de Demonio/Carmesi (PlrCharacter.ExtraAccessory) - ver
        // IsItemSlotUnlockedAndUsable en el comentario real de ResolveAccessories.
        var accessories = equipmentAppearance.ResolveAccessories(character.PrimaryLoadout, hide, character.ExtraAccessory, character.Loadouts);
        // H6-02/H6-01-b: Gender ES el skinVariant real (0-11, no un booleano) - se pasa entero
        // para que el doll de Inicio use la carpeta de sprites/reglas SetMatch reales de la
        // variante puesta (caso "Eldelgas": Gender=8/MaleDress), no solo Chico/Chica.
        _hairStyle = character.HairStyle;
        _skinVariant = character.Gender;
        _colors = colors;
        _armor = armor;
        _accessories = accessories;
        _preview = PlayerPreviewRenderer.Render(_hairStyle, _skinVariant, colors, armor, accessories: accessories);

        _petPreview = equipmentAppearance.ResolvePet(character.EquipmentItems);
        if (_petPreview?.AnimationEntry is { } petEntry)
        {
            _petAnimationDriver = new PetAnimationDriver(petEntry);
            _petImage = PetPreviewRenderer.RenderFrame(petEntry, 0);
            _petBaseOffsetX = petEntry.OffsetX;
            _petBaseOffsetY = petEntry.OffsetY;
            PetOffsetX = petEntry.OffsetX;
            PetOffsetY = petEntry.OffsetY;
            PetSpriteDirection = petEntry.SpriteDirection;
        }
        // Icono estatico de reserva (objeto de mascota real sin animacion catalogada, o el
        // recorte del primer fotograma fallo) - un BitmapImage normal, congelado para poder
        // construirse aqui (este constructor corre en el hilo de FONDO del escaneo,
        // HomeViewModel.cs:389 - mismo motivo real que obligo a hacer perezoso
        // _hoverWalkTimer mas arriba, ver su comentario) y usarse despues sin problema en el
        // hilo de UI.
        if (_petImage is null && _petPreview?.IconPath is { } iconPath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            _petImage = bitmap;
        }
    }
}
