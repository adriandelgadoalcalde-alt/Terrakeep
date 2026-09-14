// KEEPQA_UIA_TREE=1 (14-sep-2026, KeepQA V3 Fase 2, Seccion 8 "WINDOWS UI AUTOMATION" de
// ESPEC-V3-AUTONOMOUS-TOOLING.md - IUIAutomationAdapter). Terrakeep.App.Tests YA usa UI Automation
// real en proceso para CLICS puntuales (AutomationElement.FromHandle + PropertyCondition +
// InvokePattern/SelectionItemPattern, ver Program.cs linea 224 y KEEPQA_SMOKE) - lo que NO existia
// hasta hoy en ningun sitio del proyecto (confirmado con grep de "TreeWalker"/"RawViewWalker"/
// "ControlViewWalker" en todo el arnes antes de escribir esto: cero coincidencias) es un VOLCADO
// COMPLETO del arbol en un formato machine-readable que un proceso Node externo (KeepQA) pueda leer
// sin tener que arrancar su propio cliente UIA .NET. Este fichero cierra ese hueco: camina el arbol
// real con TreeWalker.ControlViewWalker (la misma vista que usaria cualquier lector de pantalla o
// herramienta de accesibilidad real - filtra ruido no interactivo, a diferencia de RawViewWalker)
// desde la raiz real de la ventana (`root`, ya obtenido en Program.cs via
// AutomationElement.FromHandle(hwnd)) y vuelca cada nodo en el formato GENERICO de la Seccion 8,
// nombres de campo en ingles EXACTOS de la propia espec (Window/ControlType/Name/AutomationId/
// ClassName/BoundingRectangle/Visibility/Enabled/Focused/IsOffscreen/Parent/Children/Patterns/
// Value/Selection/Toggle/ExpandCollapse) - a diferencia del resto de KeepQA (campos en español),
// aqui se respeta el nombre literal de la seccion que este fichero implementa, igual que ya hizo
// `src/tool-registry/contrato-adaptador.js` con los nombres de la Seccion 2.
//
// LIMITES REALES, documentados aqui con la misma honestidad que el resto del proyecto:
//   - "Parent" de la Seccion 8 se expone como `ParentPath` (breadcrumb de nombres desde la raiz),
//     nunca como una referencia circular al nodo padre - un JSON no puede tener ciclos, y la
//     jerarquia ya es explicita via `Children` (anidamiento real). Documentado como adaptacion
//     deliberada, no como una omision.
//   - El arbol completo de una ventana WPF real puede tener miles de nodos (plantillas de
//     ItemsControl, decoradores internos de tema...) - se aplica un tope real de nodos
//     (KEEPQA_UIA_TREE_MAXNODOS, por defecto 4000) y de profundidad (KEEPQA_UIA_TREE_MAXPROF, por
//     defecto 40) para que esto termine en tiempo acotado; si se alcanza el tope, se marca
//     `truncado=true` y el conteo real en el nodo raiz del JSON, nunca se finge un arbol completo
//     que no se llego a recorrer.
//   - Solo TreeScope.Children (nunca Descendants de una sola llamada, que en UIA es notablemente
//     mas lento en arboles grandes) - el recorrido es manual, nodo a nodo, exactamente lo que la
//     Seccion 8 pide ("obtener un arbol de UI machine-readable").
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private sealed class NodoUiaTree
    {
        public string ControlType { get; set; } = "";
        public string? Name { get; set; }
        public string? AutomationId { get; set; }
        public string? ClassName { get; set; }
        public object? BoundingRectangle { get; set; }
        public string Visibility { get; set; } = "";
        public bool Enabled { get; set; }
        public bool Focused { get; set; }
        public bool IsOffscreen { get; set; }
        public string? ParentPath { get; set; }
        public List<string> Patterns { get; set; } = new();
        public string? Value { get; set; }
        public bool? Selection { get; set; }
        public string? Toggle { get; set; }
        public string? ExpandCollapse { get; set; }
        public List<NodoUiaTree> Children { get; set; } = new();
    }

    private static void EjecutarUiaTree(Window window, AutomationElement root, MainViewModel vm)
    {
        try
        {
            int maxNodos = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_UIA_TREE_MAXNODOS"), out int mn) && mn > 0 ? mn : 4000;
            int maxProf = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_UIA_TREE_MAXPROF"), out int mp) && mp > 0 ? mp : 40;
            string? tabPedida = Environment.GetEnvironmentVariable("KEEPQA_UIA_TREE_TAB");
            if (!string.IsNullOrEmpty(tabPedida) && int.TryParse(tabPedida, out int idxTab))
            {
                vm.SelectedTabIndex = idxTab;
                DoEvents(); DoEvents();
            }

            int contados = 0;
            bool truncado = false;

            NodoUiaTree? Caminar(AutomationElement el, string rutaPadre, int profundidad)
            {
                if (contados >= maxNodos) { truncado = true; return null; }
                contados++;

                AutomationElement.AutomationElementInformation info = el.Current;
                var nodo = new NodoUiaTree
                {
                    ControlType = info.ControlType?.ProgrammaticName?.Replace("ControlType.", "") ?? "Unknown",
                    Name = string.IsNullOrEmpty(info.Name) ? null : info.Name,
                    AutomationId = string.IsNullOrEmpty(info.AutomationId) ? null : info.AutomationId,
                    ClassName = string.IsNullOrEmpty(info.ClassName) ? null : info.ClassName,
                    Enabled = info.IsEnabled,
                    IsOffscreen = info.IsOffscreen,
                    Visibility = info.IsOffscreen ? "Offscreen" : "Visible",
                    Focused = false,
                    ParentPath = rutaPadre,
                };
                try { nodo.Focused = el.TryGetCurrentPattern(ValuePattern.Pattern, out _) && info.HasKeyboardFocus; }
                catch { nodo.Focused = info.HasKeyboardFocus; }

                var r = info.BoundingRectangle;
                nodo.BoundingRectangle = (r.IsEmpty || double.IsInfinity(r.X))
                    ? null
                    : new { x = r.X, y = r.Y, width = r.Width, height = r.Height };

                foreach (AutomationPattern patron in el.GetSupportedPatterns())
                {
                    string nombrePatron = patron.ProgrammaticName.Replace("PatternIdentifiers.Pattern", "");
                    nodo.Patterns.Add(nombrePatron);
                }

                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out object valPat))
                {
                    try { nodo.Value = ((ValuePattern)valPat).Current.Value; } catch { /* algunos controles lanzan si no hay valor real todavia */ }
                }
                if (el.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selPat))
                {
                    try { nodo.Selection = ((SelectionItemPattern)selPat).Current.IsSelected; } catch { }
                }
                if (el.TryGetCurrentPattern(TogglePattern.Pattern, out object togPat))
                {
                    try { nodo.Toggle = ((TogglePattern)togPat).Current.ToggleState.ToString(); } catch { }
                }
                if (el.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object expPat))
                {
                    try { nodo.ExpandCollapse = ((ExpandCollapsePattern)expPat).Current.ExpandCollapseState.ToString(); } catch { }
                }

                string rutaPropia = rutaPadre + "/" + (nodo.Name ?? nodo.ControlType);
                if (profundidad < maxProf)
                {
                    var walker = TreeWalker.ControlViewWalker;
                    AutomationElement? hijo = null;
                    try { hijo = walker.GetFirstChild(el); } catch { hijo = null; }
                    while (hijo != null)
                    {
                        var nodoHijo = Caminar(hijo, rutaPropia, profundidad + 1);
                        if (nodoHijo != null) nodo.Children.Add(nodoHijo);
                        if (contados >= maxNodos) { truncado = true; break; }
                        try { hijo = walker.GetNextSibling(hijo); } catch { hijo = null; }
                    }
                }
                else
                {
                    truncado = true;
                }

                return nodo;
            }

            var arbol = Caminar(root, "", 0);

            var salida = new
            {
                window = window.Title,
                capturadoEn = DateTimeOffset.Now.ToString("o"),
                motor = "terrakeep",
                tabSeleccionada = vm.SelectedTabIndex,
                totalNodos = contados,
                truncado,
                maxNodos,
                maxProf,
                arbol,
            };

            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);
            string rutaSalida = Path.Combine(outDir, "uia-tree.json");
            File.WriteAllText(rutaSalida, JsonSerializer.Serialize(salida, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_UIA_TREE: {contados} nodo(s) reales volcados (truncado={truncado}) -> {rutaSalida}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("KEEPQA_UIA_TREE-EXCEPTION: " + ex);
        }
    }
}
