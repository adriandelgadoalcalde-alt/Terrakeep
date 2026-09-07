using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// Un "spawn point" favorito (app.TabServers/Ha en la version JS real, categoria de idioma
// real "tab.spawnpoints" pese al nombre de clase enganoso) - punto de aparicion guardado por
// mundo. Envuelve el PlrServerEntry real y escribe en el mismo objeto en cuanto cambia un
// campo (PlrServerEntry tiene setters normales, no init-only) - Save() ya lo persiste sin
// sincronizacion aparte, mismo patron que ColorSwatchViewModel.
public partial class ServerEntryRowViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private bool _suppressWriteback;

    public PlrServerEntry Entry { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private int _spawnX;
    [ObservableProperty] private int _spawnY;
    // C-05 (informe de pulido final): renombrado de "Address" a "WorldId" - PlrServerEntry.
    // WorldId ya no se llama "Address" (nunca fue una direccion de red, es el WorldId real del
    // mundo al que pertenece este Spawn Point).
    [ObservableProperty] private int _worldId;

    public ServerEntryRowViewModel(PlrServerEntry entry)
    {
        Entry = entry;
        _suppressWriteback = true;
        _name = entry.Name;
        SpawnX = entry.SpawnX;
        SpawnY = entry.SpawnY;
        WorldId = entry.WorldId;
        _suppressWriteback = false;
    }

    partial void OnNameChanged(string value) { if (!_suppressWriteback) Entry.Name = value; }
    partial void OnSpawnXChanged(int value) { if (!_suppressWriteback) Entry.SpawnX = value; }
    partial void OnSpawnYChanged(int value) { if (!_suppressWriteback) Entry.SpawnY = value; }
    partial void OnWorldIdChanged(int value) { if (!_suppressWriteback) Entry.WorldId = value; }
}
