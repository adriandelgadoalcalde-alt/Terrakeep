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

    public ObservableCollection<ServerEntryRowViewModel> Entries { get; } = [];

    public void LoadFrom(PlrCharacter character)
    {
        _character = character;
        Entries.Clear();
        foreach (var entry in character.Servers)
            Entries.Add(new ServerEntryRowViewModel(entry));
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (_character == null) return;
        var entry = new PlrServerEntry { SpawnX = 0, SpawnY = 0, Address = 0, Name = "Nuevo spawn point" };
        _character.Servers.Add(entry);
        Entries.Add(new ServerEntryRowViewModel(entry));
    }

    [RelayCommand]
    private void RemoveEntry(ServerEntryRowViewModel? row)
    {
        if (_character == null || row == null) return;
        _character.Servers.Remove(row.Entry);
        Entries.Remove(row);
    }
}
