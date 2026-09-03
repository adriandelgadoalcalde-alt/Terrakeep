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

    // V-c (segunda auditoria de Opus, Fable): "bajar de version no advierte de lo que se pierde
    // - el aviso generico no dice que secciones dejaran de guardarse, los datos reales del
    // personaje ya estan a mano para decirlo con numeros". Se recalcula con cada cambio real de
    // version, contra el contenido REAL ya cargado (los umbrales reales de PlrBodySerializer,
    // no una lista inventada) - null si no hay nada real que se fuera a perder con el valor
    // actual.
    [ObservableProperty] private string? _downgradeWarning;

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
        DowngradeWarning = BuildDowngradeWarning(value);

        if (_suppressWriteback || _character == null) return;
        _character.Version = value;
    }

    // Umbrales reales de PlrBodySerializer (los mismos que ya leen/escriben estos campos) -
    // solo los 3 con impacto real mas facil de ver y contar con exactitud contra el personaje
    // ya cargado: equipo puesto (145), Boveda del Vacio (200) y Loadouts 1-3 (269).
    //
    // Limitacion real conocida: _character.EquipmentItems/Loadouts reflejan lo YA CARGADO desde
    // disco - una edicion hecha en la UI DESPUES de cargar (ej. poner un casco nuevo) no se
    // vuelca ahi hasta un Guardar real (CharacterFileService.Save/MaskAndSyncAll es el unico
    // sitio que sincroniza MergedContainers de vuelta a estos campos crudos). Cubre el caso real
    // mas comun (un personaje que YA trae contenido y se le baja la version sin darse cuenta),
    // no una prediccion en vivo de ediciones sin guardar todavia.
    private string? BuildDowngradeWarning(int value)
    {
        if (_character == null) return null;
        var perdidas = new List<string>();

        if (value < 145 && _character.EquipmentItems.Any(s => !s.IsEmpty))
            perdidas.Add("el equipo puesto (armadura/vanidad/accesorios)");

        if (value < 200)
        {
            int voidCount = _character.VoidItems.Count(s => !s.IsEmpty);
            if (voidCount > 0) perdidas.Add($"{voidCount} objeto(s) de la Bóveda del Vacío");
        }

        if (value < 269)
        {
            int loadoutCount = _character.Loadouts.Sum(l => l.Items.Count(s => !s.IsEmpty) + l.Social.Count(s => !s.IsEmpty) + l.Dyes.Count(s => !s.IsEmpty));
            if (loadoutCount > 0) perdidas.Add($"{loadoutCount} objeto(s) de los Loadouts 1/2/3");
        }

        return perdidas.Count == 0 ? null
            : $"Al guardar con esta versión dejarán de escribirse: {string.Join(", ", perdidas)}.";
    }

    [RelayCommand]
    private void SetVersion(int number) => RawVersion = number;
}
