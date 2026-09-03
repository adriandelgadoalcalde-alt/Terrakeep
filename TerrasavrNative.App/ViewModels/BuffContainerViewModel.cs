using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TerrasavrNative.App.ViewModels;

// Contenedor de buffs con nombre para mostrar - gemelo de ContainerViewModel (objetos) pero
// para BuffSlotViewModel. No se generaliza ContainerViewModel en si (esta tipado a
// ItemSlotViewModel y lo consumen AutoEquip/SyncEditsBackToMerged/StorageGroupViewModel/
// EquipmentGroupViewModel - hacerlo generico seria riesgo real por cero beneficio, pregunta a
// Opus sobre el diseño 2-sep-2026, cuarta pasada). H4-06 (cuarta auditoria de Opus, Fable):
// "la rejilla de Buffs no tiene contador ni operaciones en bloque, su gemela de objetos si" -
// mismo COMPORTAMIENTO real portado aqui (contador vivo + Vaciar todos/Deshacer), sin fusionar
// las dos clases - respeta la decision de diseño de arriba.
public sealed partial class BuffContainerViewModel : ObservableObject
{
    private readonly string _baseName;

    public ObservableCollection<BuffSlotViewModel> Slots { get; }
    public int Columns { get; }

    // A-c (segunda auditoria de Opus, Fable), portado por H4-06: mismo mecanismo real que
    // ContainerViewModel.DisplayName - "Buffs (2/44)" en vez de un rotulo estatico.
    public string DisplayName => $"{_baseName} ({Slots.Count(s => !s.IsEmpty)}/{Slots.Count})";

    // H4-06/H4-05: mismo snapshot+Deshacer real ya cerrado en ContainerViewModel.ClearAll -
    // aqui con (Id, Time) en vez de GameItem, unico dato real que necesita RestoreExact.
    private (int SlotIndex, int BuffId, int Time)[]? _clearedSnapshot;
    private readonly DispatcherTimer _undoClearTimer = new() { Interval = TimeSpan.FromSeconds(6) };

    [ObservableProperty] private bool _canUndoClear;
    [ObservableProperty] private int _lastClearedCount;

    public BuffContainerViewModel(string displayName, ObservableCollection<BuffSlotViewModel> slots, int columns)
    {
        _baseName = displayName;
        Slots = slots;
        Columns = columns;
        foreach (var slot in slots)
            slot.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BuffSlotViewModel.IsEmpty)) OnPropertyChanged(nameof(DisplayName)); };
        _undoClearTimer.Tick += OnUndoClearTimerTick;
    }

    [RelayCommand]
    private void ClearAll()
    {
        var snapshot = Slots.Select((s, i) => (SlotIndex: i, s.Buff.Id, s.Buff.Time))
            .Where(t => t.Id != 0).ToArray();
        if (snapshot.Length == 0) return;

        foreach (var slot in Slots)
            if (!slot.IsEmpty) slot.ClearCommand.Execute(null);

        _clearedSnapshot = snapshot;
        LastClearedCount = snapshot.Length;
        CanUndoClear = true;
        _undoClearTimer.Stop();
        _undoClearTimer.Start();
    }

    [RelayCommand]
    private void UndoClear()
    {
        if (_clearedSnapshot == null) return;
        foreach (var (slotIndex, buffId, time) in _clearedSnapshot)
            if (slotIndex < Slots.Count) Slots[slotIndex].RestoreExact(buffId, time);
        _clearedSnapshot = null;
        CanUndoClear = false;
        _undoClearTimer.Stop();
    }

    private void OnUndoClearTimerTick(object? sender, EventArgs e)
    {
        _undoClearTimer.Stop();
        _clearedSnapshot = null;
        CanUndoClear = false;
    }
}
