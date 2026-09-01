using CommunityToolkit.Mvvm.ComponentModel;
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
        UnlockedSuperMinecart = (character.SuperCartByte & 1) != 0;
        UsingSuperMinecart = (character.SuperCartByte & 1) != 0;
        _suppressWriteback = false;

        _character = character;
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
        _character.SuperCartByte = (byte)((_character.SuperCartByte & ~1) | (value ? 1 : 0));
    }
}
