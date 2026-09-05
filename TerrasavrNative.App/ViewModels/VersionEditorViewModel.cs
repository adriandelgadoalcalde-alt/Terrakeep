using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.PlrFormat;
using TerrasavrNative.App.Services;

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

    // H3-14 (tercera auditoria de Opus, Fable): "279 se muestra crudo, nunca '1.4.4.0+'" - el
    // numero crudo (RawVersion, el campo fdRaw real) es correcto MOSTRARLO tal cual, es un
    // campo de edicion deliberadamente numerico - lo que faltaba era el resumen legible que la
    // propia Terrasavr real SI muestra al lado (lbVersion[1], ver TabVersion.findBestMatch/
    // getBestText en script.beautified.js:5161-5173): la version CONOCIDA mas alta que sea
    // <= la real, con un "+" si la real es estrictamente mayor (ej. 279 -> "1.4.4.0" es
    // conocida<=279 y 279>269, resultado real "1.4.4.0+").
    [ObservableProperty] private string _bestMatchLabel = string.Empty;

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
        // H3-14: IsCurrent ya no exige coincidencia EXACTA (con eso, cualquier version real
        // "y pico" como 279 no resaltaba NINGUN boton) - resalta el mismo MEJOR AJUSTE real que
        // findBestMatch (la version conocida mas alta <= la real), identico al style(4) real de
        // TabVersion.syncVersion en la version JS.
        var best = FindBestMatch(value);
        BestMatchLabel = GetBestText(value, best);
        // V-a: se sincroniza SIEMPRE (tambien mientras _suppressWriteback esta activo durante
        // LoadFrom) - IsCurrent es un reflejo visual puro, no una escritura real al personaje.
        foreach (var group in Groups)
            foreach (var option in group.Options)
                option.IsCurrent = ReferenceEquals(option, best);
        DowngradeWarning = BuildDowngradeWarning(value);

        if (_suppressWriteback || _character == null) return;
        _character.Version = value;
    }

    // Calco real de TabVersion.findBestMatch (script.beautified.js:5161-5167): recorre las
    // opciones conocidas de mayor a menor numero y devuelve la PRIMERA (la mas alta) cuyo
    // numero sea <= la version real - si ninguna lo es (una version mas vieja que la 1.1.2
    // conocida), cae a la primera opcion conocida de todas (la 1.1.2), igual que el original.
    private VersionOption FindBestMatch(int version)
    {
        for (int gi = Groups.Count - 1; gi >= 0; gi--)
        {
            var options = Groups[gi].Options;
            for (int oi = options.Count - 1; oi >= 0; oi--)
                if (version >= options[oi].Number) return options[oi];
        }
        return Groups[0].Options[0];
    }

    // Calco real de TabVersion.getBestText (script.beautified.js:5169-5173).
    private static string GetBestText(int version, VersionOption best) =>
        version > best.Number ? best.Label + "+" : best.Label;

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
            perdidas.Add(LocalizationService.Instance["version_worn_equipment"]);

        if (value < 200)
        {
            int voidCount = _character.VoidItems.Count(s => !s.IsEmpty);
            if (voidCount > 0) perdidas.Add(LocalizationService.Instance.Format("version_void_items", voidCount));
        }

        if (value < 269)
        {
            int loadoutCount = _character.Loadouts.Sum(l => l.Items.Count(s => !s.IsEmpty) + l.Social.Count(s => !s.IsEmpty) + l.Dyes.Count(s => !s.IsEmpty));
            if (loadoutCount > 0) perdidas.Add(LocalizationService.Instance.Format("version_loadout_items", loadoutCount));
        }

        return perdidas.Count == 0 ? null
            : LocalizationService.Instance.Format("version_will_stop_writing", string.Join(", ", perdidas)) + ".";
    }

    [RelayCommand]
    private void SetVersion(int number) => RawVersion = number;
}
