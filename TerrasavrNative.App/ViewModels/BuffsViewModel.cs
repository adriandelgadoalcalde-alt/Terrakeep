using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Pestaña "Buffs" - rejilla de los 44/22/10 slots REALES de PlrCharacter.Buffs (segun version,
// ver PlrBodySerializer). Rehecha 2-sep-2026 (cuarta pasada, pregunta a Opus sobre el diseño):
// la Fase 1 ("misma rejilla que inventario") ya se hizo en una pasada anterior con el buscador
// viejo "Añadir buff..." de relleno; la Fase 2 (esta) añade BuffLibraryViewModel con el arbol
// real de Terrasavr, asi que este ViewModel se simplifica a solo construir el contenedor - el
// picker (buscar+colocar) vive ahora en BuffLibraryViewModel, mismo reparto de
// responsabilidades que Objetos (Containers vs LibraryViewModel).
public partial class BuffsViewModel : ObservableObject
{
    private readonly CharacterFileService _service;
    private readonly Action<BuffSlotViewModel> _requestPick;

    [ObservableProperty] private BuffContainerViewModel? _container;

    // Auditoria de Opus, N-2 (IsDirty real): BuffsViewModel es una instancia PERSISTENTE que
    // reconstruye sus slots en cada LoadFrom() (a diferencia de los contenedores de objetos,
    // que MainViewModel crea el mismo directamente) - MainViewModel se suscribe UNA vez en su
    // constructor y este evento reenvia el cambio de cualquier slot, cargado el personaje que
    // sea, sin tener que resuscribirse en cada LoadFrom.
    public event Action? SlotChanged;

    public BuffsViewModel(CharacterFileService service, Action<BuffSlotViewModel> requestPick)
    {
        _service = service;
        _requestPick = requestPick;
    }

    public void LoadFrom(PlrCharacter character)
    {
        // 11 columnas reales (pregunta a Opus sobre el diseño: "44 slots a 11 columnas", real
        // de app.BuffSide/script.beautified.js - 4 filas exactas de 11, no 10x4+4 suelto).
        var slots = new ObservableCollection<BuffSlotViewModel>();
        for (int i = 0; i < character.Buffs.Count; i++)
        {
            var slot = new BuffSlotViewModel(i, character.Buffs[i], _service.VanillaBuffs, _service.CalamityBuffCatalog,
                _service.VanillaBuffDurations, character.Version, _requestPick);
            slot.PropertyChanged += (_, e) => { if (e.PropertyName != nameof(BuffSlotViewModel.IsSelected)) SlotChanged?.Invoke(); };
            slots.Add(slot);
        }
        Container = new BuffContainerViewModel("Buffs", slots, 11);
    }

    public void Reset() => Container = null;
}
