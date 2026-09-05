using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Spawn Points" - PlrCharacter.Servers ya se leia/escribia desde la Fase 1
// (PlrBodySerializer.ReadServers/WriteServers), pero sin ningun panel para verlo/editarlo
// (hueco #3 de la auditoria Terrasavr JS vs puerto, ver bitacora.md).
public partial class ServersViewModel : ObservableObject
{
    private PlrCharacter? _character;
    private bool _suppressChanged = true;

    public ObservableCollection<ServerEntryRowViewModel> Entries { get; } = [];

    // Segunda auditoria de Opus (Fable), B-5 - BUG REAL: MainViewModel se suscribia a
    // "Servers.PropertyChanged" para marcar el personaje como modificado (N-2), pero esta
    // clase no tenia ni una sola [ObservableProperty] real - los campos editables viven en
    // ServerEntryRowViewModel (cuyo PropertyChanged nunca llegaba aqui), y Add/RemoveEntry
    // mutan Entries (una ObservableCollection, dispara CollectionChanged, NO PropertyChanged).
    // Editar el nombre/X/Y de un spawn point, o añadir/quitar uno, no marcaba nada - mismo
    // riesgo de perdida silenciosa que B-4. Mismo patron real ya usado y probado en
    // BuffsViewModel.SlotChanged - un evento propio, disparado explicitamente en cada camino
    // de edicion real (por fila Y por añadir/quitar), en vez de depender de una suscripcion
    // difusa a PropertyChanged que aqui nunca tuvo nada que emitir.
    public event Action? Changed;

    public void LoadFrom(PlrCharacter character)
    {
        _suppressChanged = true;
        _character = character;
        Entries.Clear();
        foreach (var entry in character.Servers)
            AddRow(entry);
        _suppressChanged = false;
    }

    private void AddRow(PlrServerEntry entry)
    {
        var row = new ServerEntryRowViewModel(entry);
        row.PropertyChanged += (_, _) => { if (!_suppressChanged) Changed?.Invoke(); };
        Entries.Add(row);
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (_character == null) return;
        var entry = new PlrServerEntry { SpawnX = 0, SpawnY = 0, WorldId = 0, Name = "Nuevo spawn point" };
        _character.Servers.Add(entry);
        AddRow(entry);
        if (!_suppressChanged) Changed?.Invoke();
    }

    [RelayCommand]
    private void RemoveEntry(ServerEntryRowViewModel? row)
    {
        if (_character == null || row == null) return;
        _character.Servers.Remove(row.Entry);
        Entries.Remove(row);
        if (!_suppressChanged) Changed?.Invoke();
    }
}
