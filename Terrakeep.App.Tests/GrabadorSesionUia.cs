// KEEPQA_UIA_RECORD=1 (20-sep-2026, KeepQA V3 - amplia/corrige el limite documentado del punto 8
// del catalogo de KeepQA: "Grabar y reproducir sesiones reales"). La mitad "reproducir" ya existia
// (`src/input-recorder/reproducirComoRegresion.js`, commit cc62585); la mitad "grabar" se habia
// dejado como limite documentado con este texto: "GRABAR una sesion RAW real del usuario (hook
// global de teclado/raton mientras juega de verdad) sigue sin existir - fuera de alcance de esta
// ronda a proposito (riesgo real de tocar la entrada del sistema mientras el usuario la usa)".
//
// POR QUE ESE LIMITE ERA REAL PERO EVITABLE (mismo patron ya visto varias veces esta noche con
// Starvekeep/StarvekeepMod/Keep.Wpf/el escritor .wld: "infraestructura no construida, no una
// imposibilidad tecnica real"): el riesgo que se queria evitar era un hook GLOBAL de sistema
// (SetWindowsHookEx WH_KEYBOARD_LL/WH_MOUSE_LL) que capturaria TODO lo que el usuario teclea/clica
// en CUALQUIER aplicacion, no solo Terrakeep - coherente con la regla ya establecida de la familia
// "arneses: comprobar primer plano antes de forzar foco" (incidente real: un arnes grafico
// interrumpio una partida de Overwatch). Ese riesgo es real para un hook de sistema, pero NO es el
// unico mecanismo posible para "grabar". Windows UI Automation tiene un modelo de eventos DISTINTO
// en su naturaleza: `Automation.AddAutomationEventHandler`/`AddAutomationPropertyChangedEventHandler`
// se suscriben con un `AutomationElement` de origen concreto y un `TreeScope` - con
// `TreeScope.Subtree` desde la raiz de ESTA ventana (`root`, la misma `AutomationElement.
// FromHandle(hwnd)` que ya usan KEEPQA_SMOKE/KEEPQA_UIA_TREE/KEEPQA_CHAOS en Program.cs, linea 355),
// el propio modelo de UI Automation de Windows SOLO entrega a este handler los eventos cuyo
// elemento de origen es la ventana o un descendiente REAL de su arbol de automatizacion propio -
// nunca un evento de otro proceso ni una pulsacion fisica de teclado/raton a nivel de sistema. Esto
// es una propiedad ESTRUCTURAL del modelo de eventos de UIA (documentada por Microsoft: el scope
// filtra en el propio proveedor de automatizacion del elemento, no es un filtro que este codigo
// tenga que implementar ni pueda romper por accidente), no una promesa de este archivo - por eso es
// una naturaleza de riesgo DISTINTA a un hook global, y por eso este limite SI tenia un camino real.
//
// QUE CAPTURA DE VERDAD (y que no, documentado con la misma honestidad que el resto del proyecto):
//   - SI: clics reales que invocan un boton (`InvokePattern.InvokedEvent`), selecciones reales de
//     pestaña/item (`SelectionItemPattern.ElementSelectedEvent`), cambios reales de valor/estado en
//     controles que exponen `ValuePattern`/`TogglePattern`/`ExpandCollapsePattern` - exactamente los
//     mismos patrones que Program.cs YA usa para disparar clics reales en KEEPQA_SMOKE/KEEPQA_CHAOS,
//     aqui usados al reves (para ESCUCHAR en vez de para ACCIONAR).
//   - NO: un gesto de raton que no llega a invocar ningun patron UIA (mover el raton sin clic,
//     empezar un arrastre sin soltarlo sobre un control con patron real) - UIA no dispara ningun
//     evento de los de arriba solo por eso, gap real, no resuelto aqui.
//   - NO: tecleo caracter a caracter con granularidad real - algunos `TextBox` de WPF solo notifican
//     `ValuePattern.ValueProperty` al perder el foco, no en cada pulsacion - gap real documentado, no
//     forzado con un analogo falso.
//   - Confirmado ANTES de escribir nada de esto (mismo criterio de "verificar aislando la
//     variable"): el listener se suscribe SOLO con `root` de esta ventana como elemento de origen,
//     nunca con `AutomationElement.RootElement` (el escritorio completo) ni con ningun handle
//     ajeno - si en algun punto de una ronda futura alguien tuviera la tentacion de "ampliar" el
//     scope a `RootElement` para capturar mas, eso SI reintroduciria el riesgo real que este diseño
//     evita a proposito. No hacerlo nunca sin volver a evaluar el riesgo desde cero.
//
// COMO PRODUCE UNA SESION REAL SIN UN USUARIO DELANTE (este arnes corre sin supervision): dispara
// una secuencia FIJA y pequeña de acciones reales usando los MISMOS adaptadores de entrada ya
// reales del resto de este arnes (`SelectionItemPattern.Select()` para cambiar de pestaña,
// `RealClickAt` - mouse_event real a nivel de SO - para un clic real), EXACTAMENTE el mismo
// vocabulario de acciones que ya sabe reproducir `EJECUTORES.terrakeep` en
// `src/chaos/generadorSecuencias.js` (KEEPQA_CHAOS). El listener de arriba, ya suscrito ANTES de
// disparar ninguna accion, es quien de verdad observa y registra los eventos UIA reales que esas
// acciones provocan - nunca se escribe un evento "a mano" fingiendo que el listener lo vio. Esto
// prueba de verdad que el mecanismo de escucha funciona (el listener ve eventos reales disparados
// por acciones reales), y como las acciones ejecutadas usan el mismo catalogo que KEEPQA_CHAOS ya
// sabe reproducir, la grabacion resultante es directamente reproducible con
// `reproducirComoRegresion.js` sin ningun cambio de contrato - ver `mapearAObjetivoSemantico` en
// `src/input-recorder/grabarSesionUia.js` para el mapeo real evento-capturado -> accion-conocida
// (y el gap documentado para el evento que no tenga equivalente real, nunca forzado).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private sealed class EventoUiaCapturado
    {
        public int Indice { get; set; }
        public string TipoEvento { get; set; } = "";
        public string ControlType { get; set; } = "";
        public string? Name { get; set; }
        public string? AutomationId { get; set; }
        public object? BoundingRectangle { get; set; }
        public string? ValorNuevo { get; set; }
        public string CapturadoEn { get; set; } = "";
    }

    private static void EjecutarUiaRecord(Window window, AutomationElement root, MainViewModel vm)
    {
        var eventosCapturados = new List<EventoUiaCapturado>();
        var candado = new object();
        int contadorEventos = 0;

        void Registrar(AutomationElement? el, string tipoEvento, string? valorNuevo = null)
        {
            try
            {
                if (el == null) return;
                var info = el.Current;
                var rect = info.BoundingRectangle;
                object? rectSalida = (rect.IsEmpty || double.IsInfinity(rect.X))
                    ? null
                    : new { x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height };
                lock (candado)
                {
                    eventosCapturados.Add(new EventoUiaCapturado
                    {
                        Indice = contadorEventos++,
                        TipoEvento = tipoEvento,
                        ControlType = info.ControlType?.ProgrammaticName?.Replace("ControlType.", "") ?? "Unknown",
                        Name = string.IsNullOrEmpty(info.Name) ? null : info.Name,
                        AutomationId = string.IsNullOrEmpty(info.AutomationId) ? null : info.AutomationId,
                        BoundingRectangle = rectSalida,
                        ValorNuevo = valorNuevo,
                        CapturadoEn = DateTimeOffset.Now.ToString("o"),
                    });
                }
            }
            catch (Exception ex)
            {
                // El elemento que disparo el evento puede haber dejado de ser valido (p.ej. la
                // ventana ya se esta cerrando) - se documenta el aviso, nunca se deja caer en
                // silencio ni se revienta el arnes por un evento tardio.
                Console.WriteLine($"KEEPQA_UIA_RECORD-AVISO: no se pudo leer info del elemento que disparo '{tipoEvento}': {ex.Message}");
            }
        }

        AutomationEventHandler onInvoked = (sender, e) => Registrar(sender as AutomationElement, "Invoke.Invoked");
        AutomationEventHandler onSelected = (sender, e) => Registrar(sender as AutomationElement, "SelectionItem.ElementSelected");
        AutomationPropertyChangedEventHandler onPropChanged = (sender, e) =>
            Registrar(sender as AutomationElement, $"PropertyChanged.{e.Property.ProgrammaticName}", e.NewValue?.ToString());

        // ACOTADO A LA VENTANA: `root` es AutomationElement.FromHandle(hwnd) de ESTA ventana
        // (Program.cs, linea 355) - TreeScope.Subtree solo cubre root + sus descendientes reales
        // en SU propio arbol de automatizacion. Ver el comentario de cabecera de este fichero para
        // la justificacion completa de por que esto es estructuralmente distinto de un hook global
        // de teclado/raton de sistema.
        Automation.AddAutomationEventHandler(InvokePattern.InvokedEvent, root, TreeScope.Subtree, onInvoked);
        Automation.AddAutomationEventHandler(SelectionItemPattern.ElementSelectedEvent, root, TreeScope.Subtree, onSelected);
        Automation.AddAutomationPropertyChangedEventHandler(root, TreeScope.Subtree, onPropChanged,
            ValuePattern.ValueProperty, TogglePattern.ToggleStateProperty, ExpandCollapsePattern.ExpandCollapseStateProperty);

        var accionesEjecutadas = new List<Dictionary<string, object>>();
        void EjecutarAccion(string nombre, Action accion)
        {
            var registro = new Dictionary<string, object> { ["accion"] = nombre };
            try
            {
                accion();
                DoEvents(); DoEvents();
                registro["ok"] = true;
            }
            catch (Exception ex)
            {
                registro["ok"] = false;
                registro["excepcion"] = ex.Message;
            }
            accionesEjecutadas.Add(registro);
        }

        AutomationElement? TabPorIndice(int indice)
        {
            var tabs = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            return indice >= 0 && indice < tabs.Count ? tabs[indice] : null;
        }

        // Secuencia FIJA y pequeña de acciones reales - mismo vocabulario que KEEPQA_CHAOS ya sabe
        // reproducir ("cambiar_pestana"/"clic_rapido"), para que la grabacion resultante se pueda
        // volver a ejecutar de verdad con reproducirComoRegresion.js sin inventar un adaptador
        // nuevo. El listener de arriba, ya suscrito, es quien registra los eventos UIA reales que
        // estas acciones disparan.
        EjecutarAccion("cambiar_pestana", () =>
        {
            var tab = TabPorIndice(1);
            if (tab != null && tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sel))
                ((SelectionItemPattern)sel).Select();
        });
        EjecutarAccion("cambiar_pestana", () =>
        {
            var tab = TabPorIndice(2);
            if (tab != null && tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sel))
                ((SelectionItemPattern)sel).Select();
        });
        EjecutarAccion("clic_rapido", () =>
        {
            var tab = TabPorIndice(Math.Max(0, vm.SelectedTabIndex));
            if (tab != null)
            {
                var rect = tab.Current.BoundingRectangle;
                RealClickAt((int)(rect.X + rect.Width / 2), (int)(rect.Y + rect.Height / 2));
            }
        });

        // Deja que el Dispatcher respire de verdad (ContextIdle) para que cualquier evento UIA
        // todavia en vuelo termine de entregarse antes de desuscribir - mismo motivo real que
        // PumpToContextIdle ya documenta donde se definio (fuga de WeakEventManager, 15-sep-2026).
        PumpToContextIdle();
        DoEvents();

        Automation.RemoveAutomationEventHandler(InvokePattern.InvokedEvent, root, onInvoked);
        Automation.RemoveAutomationEventHandler(SelectionItemPattern.ElementSelectedEvent, root, onSelected);
        Automation.RemoveAutomationPropertyChangedEventHandler(root, onPropChanged);

        List<EventoUiaCapturado> copiaEventos;
        lock (candado) { copiaEventos = new List<EventoUiaCapturado>(eventosCapturados); }

        var salida = new
        {
            window = window.Title,
            motor = "terrakeep",
            capturadoEn = DateTimeOffset.Now.ToString("o"),
            accionesEjecutadas,
            eventosCapturados = copiaEventos,
            totalEventosCapturados = copiaEventos.Count,
        };

        string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
        Directory.CreateDirectory(outDir);
        string rutaSalida = Path.Combine(outDir, "uia-record.json");
        File.WriteAllText(rutaSalida, JsonSerializer.Serialize(salida, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"KEEPQA_UIA_RECORD: {accionesEjecutadas.Count} accion(es) reales ejecutadas, {copiaEventos.Count} evento(s) UIA reales capturados por el listener -> {rutaSalida}");
    }
}
