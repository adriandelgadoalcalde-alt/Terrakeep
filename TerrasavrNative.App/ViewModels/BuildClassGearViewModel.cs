using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Equipo recomendado de una clase dentro de una etapa, ya resuelto para mostrar (iconos +
// nombres). Source conserva el BuildClassGear (Core) original tal cual, para que el boton
// "Auto-equipar" siga pudiendolo pasar directamente a MainViewModel.AutoEquipCommand sin
// tener que reconstruirlo desde las filas ya resueltas.
public sealed partial class BuildClassGearViewModel : ObservableObject
{
    // Ronda de Libreria/Builds del 6-sep-2026: deja de ser constructor primario porque hace falta
    // un CUERPO real de constructor donde suscribirse al cambio de idioma (ver ClassLabel).
    public BuildClassGearViewModel(
        string className,
        List<BuildItemRowViewModel> armor,
        List<BuildItemRowViewModel> weapons,
        List<BuildItemRowViewModel> accessories,
        BuildClassGear source)
    {
        ClassName = className;
        Armor = armor;
        Weapons = weapons;
        Accessories = accessories;
        Source = source;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(ClassLabel));

    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string ClassName { get; }

    // Ronda de Libreria/Builds del 6-sep-2026 - BUG REAL: el titulo de cada columna de Builds
    // pintaba ClassName TAL CUAL, o sea la clave interna en bruto de builds.json ("melee",
    // "ranged", "mage", "summoner", "rogue") - mientras que las pildoras de filtro de arriba, en
    // la MISMA pantalla, ya decian "Cuerpo a cuerpo"/"A distancia"/"Magia"/"Invocación"/"Pícaro"
    // (BuildClassFilterOptionViewModel). El mismo concepto con dos nombres distintos a un palmo de
    // distancia, y uno de los dos ni siquiera es un idioma: es el identificador del fichero de
    // datos. Con la app en ingles pasaba igual ("mage" en minusculas en vez de "Magic").
    //
    // Mismas claves reales que ya usan las pildoras, y el mismo respaldo: una clase sin clave de
    // idioma propia (un catalogo futuro con una clase nueva) cae a su nombre interno tal cual,
    // nunca al "[clave]" en bruto del diccionario - ahi si es un dato real que existe, no una
    // traduccion perdida. Se resuelve al LEER (no en el constructor) y se avisa al cambiar de
    // idioma en caliente, por evento DEBIL: mismo motivo real que LocalizedContentViewModel.
    private static readonly Dictionary<string, string> ClavesDeIdioma = new()
    {
        ["melee"] = "class_melee",
        ["ranged"] = "class_ranged",
        ["mage"] = "class_mage",
        ["summoner"] = "class_summoner",
        ["rogue"] = "class_rogue",
    };

    public string ClassLabel
    {
        get
        {
            if (!ClavesDeIdioma.TryGetValue(ClassName, out var clave)) return ClassName;
            string texto = Services.LocalizationService.Instance[clave];
            return texto.StartsWith('[') && texto.EndsWith(']') ? ClassName : texto;
        }
    }

    public List<BuildItemRowViewModel> Armor { get; }
    public List<BuildItemRowViewModel> Weapons { get; }
    public List<BuildItemRowViewModel> Accessories { get; }
    public BuildClassGear Source { get; }

    // Bd-d (segunda auditoria de Opus, Fable): "buscador/filtro por clase" - la pestaña Builds
    // era una unica lista plana de TODAS las clases de TODAS las etapas, sin forma de ver solo
    // "melee" por ejemplo. Ver BuildsViewModel.SetClassFilter.
    [ObservableProperty] private bool _isVisible = true;

    public IEnumerable<BuildItemRowViewModel> AllRows => Armor.Concat(Weapons).Concat(Accessories);
}
