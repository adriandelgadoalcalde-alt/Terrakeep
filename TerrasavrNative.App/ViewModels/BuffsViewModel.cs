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
            // Bu-b (segunda auditoria de Opus, Fable): delegado real (no la coleccion entera,
            // que todavia se esta rellenando en este mismo bucle) - "slots" ya existe por
            // referencia, para cuando SI se llame de verdad (una colocacion real, nunca durante
            // esta misma construccion) ya estara completa.
            var slot = new BuffSlotViewModel(i, character.Buffs[i], _service.VanillaBuffs, _service.CalamityBuffCatalog,
                _service.VanillaBuffDurations, character.Version, _requestPick,
                (buffId, self) => slots.Any(s => !ReferenceEquals(s, self) && s.Buff.Id == buffId));
            // Bu-a (segunda auditoria de Opus, Fable): JustEdited se excluye igual que
            // IsSelected - si no, el propio flash (JustEdited cambiando) re-entraria este mismo
            // manejador sin fin (mismo bug real ya evitado en MainViewModel.HookSlotEditing).
            // H3-03 (tercera auditoria, Fable): RejectionMessage se excluye igual que
            // IsSelected/JustEdited - es puro estado de UI (un buff duplicado rechazado, sin
            // ningun cambio real de datos), mismo motivo real ya cerrado en
            // MainViewModel.HookSlotEditing.
            slot.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(BuffSlotViewModel.IsSelected) or nameof(BuffSlotViewModel.JustEdited) or nameof(BuffSlotViewModel.RejectionMessage)) return;
                SlotChanged?.Invoke();
                slot.TriggerEditFlash();
            };
            slots.Add(slot);
        }
        Container = new BuffContainerViewModel("Buffs", slots, 11);
    }

    public void Reset() => Container = null;
}
