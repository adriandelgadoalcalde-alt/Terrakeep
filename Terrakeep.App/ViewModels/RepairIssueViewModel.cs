using CommunityToolkit.Mvvm.Input;

namespace Terrakeep.App.ViewModels;

// Catalogo de ideas Keep, idea 9 ("modo reparar personaje"), tercera pasada (20-sep-2026): el
// coordinador pidio releer el texto LITERAL de la idea y confirmo que "arreglo en un clic" es
// parte real del alcance pedido, no una añadidura opcional - las dos primeras pasadas solo
// diagnosticaban (mostraban el problema, nunca lo arreglaban). Una fila real de cualquiera de
// los 5 diagnosticos (prefijo ilegal/slot fantasma/buff desbordado/.tplr huerfano/.tplr
// inconsistente) ahora es esto: un nombre para mostrar + una accion real y segura que sabe
// arreglar ESE problema concreto (nunca una aproximacion generica) y que, al terminar, vuelve a
// llamar a RebuildIllegalPrefixDiagnostics para que la fila arreglada desaparezca sola de la
// lista - el mismo patron ya usado a mano en el arnes de pruebas real (IDEA9_SOLO).
public sealed class RepairIssueViewModel(string displayName, Action fix)
{
    public string DisplayName { get; } = displayName;
    public IRelayCommand FixCommand { get; } = new RelayCommand(fix);
}
