using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Terrakeep.App.Services;

// H5-01 (quinta auditoria de Opus): "casi toda edicion del personaje es irreversible - Terrakeep
// tiene exactamente dos 'deshacer', y ninguno es un deshacer de edicion" (UndoLastSave opera
// sobre FICHEROS, ContainerViewModel.UndoClear era una unica instantanea de 6s solo para
// "Vaciar contenedor"). Pila real de comandos con Undo/Redo por closures - cualquier cambio real
// (un slot, un lote entero) se representa como una unica entrada con su propio rotulo, sin que
// esta clase necesite saber NADA del dominio (objetos/buffs/investigacion/desbloqueos serian
// todos igual de validos aqui).
public sealed class UndoEntry
{
    public required string Label { get; init; }
    public required Action Undo { get; init; }
    public required Action Redo { get; init; }
}

public sealed partial class UndoStack : ObservableObject
{
    // Orden real: Entries[0] es la mas antigua. _index es "cuantas entradas desde el principio
    // estan aplicadas ahora mismo" - Undo() la baja en 1, Redo() la sube en 1. Empujar una
    // entrada nueva mientras _index < Entries.Count trunca la cola de Redo (mismo comportamiento
    // real de cualquier editor de escritorio: una edicion nueva tras deshacer invalida el futuro
    // que se habia deshecho).
    private readonly List<UndoEntry> _entries = [];
    private int _index;

    public ObservableCollection<UndoEntry> Entries { get; } = [];

    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private bool _canRedo;
    // Rotulo real de LA SIGUIENTE entrada que Undo()/Redo() aplicarian - para el tooltip real de
    // los botones (P5, "feedback vivo": el boton dice QUE va a deshacer, no solo que puede).
    [ObservableProperty] private string? _nextUndoLabel;
    [ObservableProperty] private string? _nextRedoLabel;

    public void Push(UndoEntry entry)
    {
        if (_index < _entries.Count)
        {
            _entries.RemoveRange(_index, _entries.Count - _index);
            for (int i = Entries.Count - 1; i >= _index; i--) Entries.RemoveAt(i);
        }
        _entries.Add(entry);
        Entries.Add(entry);
        _index++;
        RefreshState();
    }

    public void UndoLast()
    {
        if (!CanUndo) return;
        _index--;
        _entries[_index].Undo();
        RefreshState();
    }

    public void RedoLast()
    {
        if (!CanRedo) return;
        _entries[_index].Redo();
        _index++;
        RefreshState();
    }

    // Al cargar/cambiar de personaje - el historial de un personaje no tiene sentido real sobre
    // otro (mismo criterio ya establecido para IsDirty/StatusMessage al recargar).
    public void Clear()
    {
        _entries.Clear();
        Entries.Clear();
        _index = 0;
        RefreshState();
    }

    private void RefreshState()
    {
        CanUndo = _index > 0;
        CanRedo = _index < _entries.Count;
        NextUndoLabel = CanUndo ? _entries[_index - 1].Label : null;
        NextRedoLabel = CanRedo ? _entries[_index].Label : null;
    }
}
