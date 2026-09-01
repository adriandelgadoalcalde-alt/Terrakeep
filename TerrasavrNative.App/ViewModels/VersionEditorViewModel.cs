using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

public sealed record VersionOption(string Label, int Number);
public sealed record VersionGroup(string Label, IReadOnlyList<VersionOption> Options);

// Pestaña "Versión" (app.TabVersion/ya en la version JS real) - fuerza que version de Terraria
// se escribe en el .plr. Cierra el hueco #5 de la auditoria (ver bitacora.md), dejado para el
// final a proposito por su riesgo: cambiar la version cambia que campos se leen/escriben (los
// version-gates de PlrBodySerializer), asi que un valor incoherente con el contenido real del
// personaje puede producir un archivo que ni este programa ni el juego real sepan releer bien.
//
// Solo se ofrecen los grupos 1.3.x/1.4.x reales (tabla exacta de ya.initPC en
// script.readable.js) - 1.1.x/1.2.x (version<145) se omiten a proposito: esta app solo sabe
// LEER version>=145 (PlrCharacter.cs, limitacion deliberada de la Fase 1), ofrecer una version
// mas antigua invitaria a escribir un archivo que este mismo programa no podria volver a abrir.
public partial class VersionEditorViewModel : ObservableObject
{
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    [ObservableProperty] private int _rawVersion;

    public IReadOnlyList<VersionGroup> Groups { get; } =
    [
        new("1.3.x", [new("1.3.0", 145), new("1.3.1", 168), new("1.3.3", 175), new("1.3.4", 184), new("1.3.5", 190)]),
        new("1.4.x", [new("1.4.0", 225), new("1.4.0.5", 230), new("1.4.1.2", 237), new("1.4.3.0", 248), new("1.4.4.0", 269), new("1.4.5.0 / 1.4.5.x", 315)]),
    ];

    public void LoadFrom(PlrCharacter character)
    {
        _character = null;
        _suppressWriteback = true;
        RawVersion = character.Version;
        _suppressWriteback = false;
        _character = character;
    }

    partial void OnRawVersionChanged(int value)
    {
        if (_suppressWriteback || _character == null) return;
        _character.Version = value;
    }

    [RelayCommand]
    private void SetVersion(int number) => RawVersion = number;
}
