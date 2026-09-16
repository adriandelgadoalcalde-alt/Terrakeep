// KEEPQA_EQUIPINV_SOLO=1 (16-sep-2026): cierre del hallazgo real que dejo el modo de "juego libre"
// de KeepQA (pywinauto/UIA externo, exploracion sin guion - ver bitacora.md "16-sep-2026 (mañana)"
// y Downloads\KeepQA\artifacts\juego-libre\partida-final-seed2\informe.md): "Equipamiento a
// 1180x860 (tamaño de arranque): Image x Text solapados en
// .../TabItem[Equipamiento]#0/Pane#2/Pane#0/List#0/DataItem#3" - 4x5px de solape real entre el
// sprite del objeto y un marcador de esquina, en el TAMAÑO DE ARRANQUE (no el minimo reducido que
// ya cubre KEEPQA_TRANSICION_SOLO/TR-01..04).
//
// Este modo vuelca la geometria REAL de Equipamiento e Inventario con el personaje real ya abierto
// por HOME-OPEN (nunca el sintetico UIA-Test - llamar ANTES de vm.LoadFromPath(tempPlr), mismo
// hueco que KEEPQA_VITALS_REAL/KEEPQA_TRANSICION_SOLO), forzando el Width/Height de ARRANQUE
// (1180x860, el mismo que trae MainWindow.xaml sin tocar - nunca MinWidth/MinHeight). Reutiliza
// VolcarArbolVisual/CarpetaEvidenciaKeepQa/CapturaVentanaKeepQa de AuditoriaTransicion.cs tal
// cual, sin reescribir un solo calculo de geometria - el volcado ya sale en el contrato EXACTO
// {id,tipo,padre_id,x,y,ancho,alto,orden_z,capa} que consume
// Downloads\KeepQA\src\geometria\verificarGeometria.js directamente (el propio detector de
// "solape_hermanos" que encontro el hallazgo de juego libre), sin pasar por el formato "par"
// antes/despues de verificarTransicion.js (aqui no hay transicion que comparar, solo un estado).
//
// Como se ejecuta y se verifica:
//   dotnet build Terrakeep.App.Tests -c Debug -p:BaseOutputPath=bin_keepqaDebug/
//   KEEPQA_EQUIPINV_SOLO=1 Terrakeep.App.Tests\bin_keepqaDebug\Debug\net10.0-windows\Terrakeep.App.Tests.exe
//   node Downloads\KeepQA\src\geometria\verificarGeometria.js <keepqa-evidencia>\geometria-equipamiento-arranque.json
//   node Downloads\KeepQA\src\geometria\verificarGeometria.js <keepqa-evidencia>\geometria-inventario-arranque.json
using System.IO;
using System.Text.Json;
using System.Windows;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarKeepQaEquipInvSolo(Window window, MainViewModel vm)
    {
        string outDir = CarpetaEvidenciaKeepQa();
        double anchoAntes = window.Width, altoAntes = window.Height;
        try
        {
            if (!vm.IsCharacterLoaded)
            {
                Console.WriteLine("KEEPQA_EQUIPINV_SOLO: FALLO - no hay ningun personaje real cargado (llamar ANTES de vm.LoadFromPath(tempPlr))");
                return;
            }
            Console.WriteLine($"KEEPQA_EQUIPINV_SOLO: personaje real '{vm.CharacterName}'");

            // Tamaño de ARRANQUE por defecto, explicito para no depender de que nada anterior en
            // Program.cs haya dejado la ventana en otro tamaño - nunca MinWidth/MinHeight (eso ya
            // lo cubre KEEPQA_TRANSICION_SOLO/"tamano").
            window.Width = 1180; window.Height = 860;
            DoEvents(); DoEvents();

            void VolcarPestana(int objetosSubTabIndex, string nombre)
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = objetosSubTabIndex;
                DoEvents(); DoEvents();
                var elementos = VolcarArbolVisual(window, "ventana");
                string ruta = Path.Combine(outDir, $"geometria-{nombre}-arranque.json");
                File.WriteAllText(ruta, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
                CapturaVentanaKeepQa(window, $"equipinv-{nombre}-arranque");
                Console.WriteLine($"KEEPQA_EQUIPINV_SOLO[{nombre}]: {elementos.Count} elementos ({window.ActualWidth}x{window.ActualHeight}) -> {ruta}");
            }

            VolcarPestana(0, "equipamiento");
            VolcarPestana(1, "inventario");
        }
        catch (Exception ex) { Console.WriteLine("KEEPQA_EQUIPINV_SOLO-EXCEPTION: " + ex); }
        finally { window.Width = anchoAntes; window.Height = altoAntes; DoEvents(); DoEvents(); }
    }
}
