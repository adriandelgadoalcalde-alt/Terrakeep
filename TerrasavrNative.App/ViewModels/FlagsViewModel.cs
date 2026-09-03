using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Desbloqueos" (app.TabFlags/Vb en la version JS real) - 13 casillas de
// desbloqueos/activaciones permanentes del personaje, todas ya leidas/escritas en
// PlrCharacter desde la Fase 1 pero sin ningun panel (hueco #6 de la auditoria, ver
// bitacora.md - quedo "sin determinar" en la primera pasada, se investigo aparte y resulto
// ser tan barato como el panel de estadisticas). Etiquetas y campo real confirmados leyendo
// Vb (app.TabFlags) en script.readable.js, no adivinados.
//
// Las dos casillas de carrito potenciado ("Unlocked"/"Enabled") leen y escriben EL MISMO bit
// (superCartByte&1) en el original - no es un error de este puerto, se replica tal cual.
public partial class FlagsViewModel : ObservableObject
{
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    // D-e (segunda auditoria de Opus, Fable): "sin aviso si el flag no existe todavia en la
    // version real del personaje" - los umbrales de aqui son los MISMOS ya verificados y en
    // produccion en PlrBodySerializer (no inventados): por debajo del umbral real, ese campo
    // ni se LEE ni se ESCRIBE (Write() lo salta entero) - marcar la casilla y guardar perderia
    // el cambio en silencio, mismo riesgo real que V-c ya cerro para el resto del personaje.
    // CurrentVersion se refresca al cargar Y al entrar en esta pestaña (mismo criterio ya
    // establecido - Bd-d/X-g: foto fija recalculada cuando de verdad hace falta, no en cada
    // tecla de una edicion en la pestaña Version).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtraAccessoryBelowVersion))]
    [NotifyPropertyChangedFor(nameof(BiomeTorchesBelowVersion))]
    [NotifyPropertyChangedFor(nameof(ExtraUsingFlagsBelowVersion))]
    [NotifyPropertyChangedFor(nameof(FinishedDD2EventBelowVersion))]
    [NotifyPropertyChangedFor(nameof(SuperMinecartBelowVersion))]
    private int _currentVersion;

    public bool ExtraAccessoryBelowVersion => CurrentVersion is > 0 and < 145;
    public bool BiomeTorchesBelowVersion => CurrentVersion is > 0 and < 230;
    public bool ExtraUsingFlagsBelowVersion => CurrentVersion is > 0 and < 269;
    public bool FinishedDD2EventBelowVersion => CurrentVersion > 0 && (_character != null && _character.IsSwitch ? CurrentVersion <= 190 : CurrentVersion < 184);
    public bool SuperMinecartBelowVersion => CurrentVersion is > 0 and < 253;

    public void RefreshVersionWarning()
    {
        if (_character != null) CurrentVersion = _character.Version;
    }

    [ObservableProperty] private bool _extraAccessory;
    [ObservableProperty] private bool _unlockedBiomeTorches;
    [ObservableProperty] private bool _usingBiomeTorches;
    [ObservableProperty] private bool _artisanBread;
    [ObservableProperty] private bool _vitalCrystal;
    [ObservableProperty] private bool _aegisFruit;
    [ObservableProperty] private bool _arcaneCrystal;
    [ObservableProperty] private bool _galaxyPearl;
    [ObservableProperty] private bool _gummyWorm;
    [ObservableProperty] private bool _ambrosia;
    [ObservableProperty] private bool _finishedDD2Event;
    [ObservableProperty] private bool _unlockedSuperMinecart;
    [ObservableProperty] private bool _usingSuperMinecart;

    public void LoadFrom(PlrCharacter character)
    {
        _character = null;
        _suppressWriteback = true;
        ExtraAccessory = character.ExtraAccessory;
        UnlockedBiomeTorches = character.UnlockedBiomeTorches;
        UsingBiomeTorches = character.UsingBiomeTorches;
        ArtisanBread = character.ExtraUsingFlags[0];
        VitalCrystal = character.ExtraUsingFlags[1];
        AegisFruit = character.ExtraUsingFlags[2];
        ArcaneCrystal = character.ExtraUsingFlags[3];
        GalaxyPearl = character.ExtraUsingFlags[4];
        GummyWorm = character.ExtraUsingFlags[5];
        Ambrosia = character.ExtraUsingFlags[6];
        FinishedDD2Event = character.FinishedDD2Event;
        // D-c (segunda auditoria de Opus, Fable): BUG REAL - las dos escribian/leian el MISMO
        // bit 0, "marcar una no reflejaba la otra aunque el dato ya coincide". Confirmado contra
        // el codigo real decompilado (Player.cs, BitsByte real que empaqueta el guardado):
        // bit[0]=unlockedSuperCart, bit[1]=enabledSuperCart - dos flags reales distintos.
        UnlockedSuperMinecart = (character.SuperCartByte & 1) != 0;
        UsingSuperMinecart = (character.SuperCartByte & 2) != 0;
        _suppressWriteback = false;

        _character = character;
        RefreshVersionWarning();
    }

    partial void OnExtraAccessoryChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraAccessory = value; }
    partial void OnUnlockedBiomeTorchesChanged(bool value) { if (!_suppressWriteback && _character != null) _character.UnlockedBiomeTorches = value; }
    partial void OnUsingBiomeTorchesChanged(bool value) { if (!_suppressWriteback && _character != null) _character.UsingBiomeTorches = value; }
    partial void OnArtisanBreadChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[0] = value; }
    partial void OnVitalCrystalChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[1] = value; }
    partial void OnAegisFruitChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[2] = value; }
    partial void OnArcaneCrystalChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[3] = value; }
    partial void OnGalaxyPearlChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[4] = value; }
    partial void OnGummyWormChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[5] = value; }
    partial void OnAmbrosiaChanged(bool value) { if (!_suppressWriteback && _character != null) _character.ExtraUsingFlags[6] = value; }
    partial void OnFinishedDD2EventChanged(bool value) { if (!_suppressWriteback && _character != null) _character.FinishedDD2Event = value; }

    partial void OnUnlockedSuperMinecartChanged(bool value)
    {
        if (_suppressWriteback || _character == null) return;
        _character.SuperCartByte = (byte)((_character.SuperCartByte & ~1) | (value ? 1 : 0));
    }

    partial void OnUsingSuperMinecartChanged(bool value)
    {
        if (_suppressWriteback || _character == null) return;
        _character.SuperCartByte = (byte)((_character.SuperCartByte & ~2) | (value ? 2 : 0));
    }

    // D-d (segunda auditoria de Opus, Fable): "sin accion en bloque para los flags" - marcar/
    // desmarcar las 13 casillas una a una era el unico camino, incluso para el caso real mas
    // comun (un completista que quiere "todos los desbloqueos puestos" de golpe). Cada
    // asignacion pasa por su propio setter real (OnXxxChanged ya escribe al personaje), asi
    // que esto no duplica ninguna logica de escritura, solo dispara las 13 en orden.
    [RelayCommand]
    private void MarkAll()
    {
        ExtraAccessory = true;
        UnlockedBiomeTorches = true;
        UsingBiomeTorches = true;
        ArtisanBread = true;
        VitalCrystal = true;
        AegisFruit = true;
        ArcaneCrystal = true;
        GalaxyPearl = true;
        GummyWorm = true;
        Ambrosia = true;
        FinishedDD2Event = true;
        UnlockedSuperMinecart = true;
        UsingSuperMinecart = true;
    }

    [RelayCommand]
    private void MarkNone()
    {
        ExtraAccessory = false;
        UnlockedBiomeTorches = false;
        UsingBiomeTorches = false;
        ArtisanBread = false;
        VitalCrystal = false;
        AegisFruit = false;
        ArcaneCrystal = false;
        GalaxyPearl = false;
        GummyWorm = false;
        Ambrosia = false;
        FinishedDD2Event = false;
        UnlockedSuperMinecart = false;
        UsingSuperMinecart = false;
    }
}
