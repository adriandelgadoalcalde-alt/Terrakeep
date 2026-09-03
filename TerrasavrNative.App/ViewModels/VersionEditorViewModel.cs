using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// V-a (segunda auditoria de Opus, Fable): antes un record inmutable, sin forma real de marcar
// cual es la version YA puesta - mismo hueco real que D-6 ya cerro para los prefijos
// (PrefixCatalogEntryViewModel.IsCurrent). IsCurrent aqui SI puede cambiar en vivo (a
// diferencia del prefijo, la lista de versiones reales nunca cambia, asi que no hace falta
// reconstruirla entera - VersionEditorViewModel simplemente actualiza el flag en el sitio cada
// vez que RawVersion cambia).
public sealed partial class VersionOption(string label, int number) : ObservableObject
{
    public string Label { get; } = label;
    public int Number { get; } = number;
    [ObservableProperty] private bool _isCurrent;
}
public sealed record VersionGroup(string Label, IReadOnlyList<VersionOption> Options);

// Pestaña "Versión" (app.TabVersion/ya en la version JS real) - fuerza que version de Terraria
// se escribe en el .plr. Cierra el hueco #5 de la auditoria (ver bitacora.md), dejado para el
// final a proposito por su riesgo: cambiar la version cambia que campos se leen/escriben (los
// version-gates de PlrBodySerializer), asi que un valor incoherente con el contenido real del
// personaje puede producir un archivo que ni este programa ni el juego real sepan releer bien.
//
// Grupos 1.1.x-1.4.x reales: 1.3.x/1.4.x de la tabla exacta de ya.initPC en script.readable.js
// (ya conocida), 1.1.x/1.2.x de la tabla real de version de juego -> invVersion confirmada
// 1-sep-2026 contra script.js (misma investigacion que cerro el soporte completo de version en
// PlrBodySerializer - ver bitacora.md "Compatibilidad completa de versiones"). Ya no hay
// limitacion de lectura por debajo de 145, asi que ofrecerlas es seguro.
public partial class VersionEditorViewModel : ObservableObject
{
    private PlrCharacter? _character;
    private bool _suppressWriteback;

    [ObservableProperty] private int _rawVersion;

    public IReadOnlyList<VersionGroup> Groups { get; } =
    [
        new("1.1.x", [new("1.1.2", 39)]),
        new("1.2.x", [new("1.2.0", 69), new("1.2.1", 73), new("1.2.2", 77), new("1.2.3", 93), new("1.2.4", 98)]),
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
        // V-a: se sincroniza SIEMPRE (tambien mientras _suppressWriteback esta activo durante
        // LoadFrom) - IsCurrent es un reflejo visual puro, no una escritura real al personaje.
        foreach (var group in Groups)
            foreach (var option in group.Options)
                option.IsCurrent = option.Number == value;

        if (_suppressWriteback || _character == null) return;
        _character.Version = value;
    }

    [RelayCommand]
    private void SetVersion(int number) => RawVersion = number;
}
