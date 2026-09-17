// SNAPSHOT_VISUAL_SOLO=1 (17-sep-2026, KeepQA - encargo real: "Verify.ImageSharp/Verify.
// ImageMagick + RenderTargetBitmap cierran un hueco real: KeepQA mide geometria/contraste pero
// nunca compara el render final pixel a pixel contra una referencia visual aprobada"). Tres
// snapshots reales sobre Terrakeep (no sinteticos/inventados):
//   1. ventana-principal-inicio: MainWindow COMPLETA con un personaje real cargado (mismo
//      personaje sintetico 'UIA-Test' que ya carga el resto de este arnes via tempPlr, con 10
//      objetos reales colocados en el Inventario - mismos item ids 1..10 que ya usa
//      KEEPQA_EQUIPINV_SOLO/H5-12 mas abajo en este mismo fichero, para que el estado sea
//      determinista y reproducible entre ejecuciones - un personaje real de disco cambiaria de
//      contenido con el tiempo y rompería cualquier referencia aprobada).
//   2. panel-inventario: SOLO el ContentControl real que muestra InventoryContainer (Personaje ->
//      Objetos -> Inventario) - demuestra que CapturarPng funciona sobre un Visual cualquiera, no
//      solo sobre la ventana entera.
//   3. ventana-principal-inicio-en: la MISMA pantalla 1 con Settings.Language="en" en vivo (el
//      chip de idioma real cambia la app sin reiniciar, ver SettingsViewModel.OnLanguageChanged) -
//      cubre el otro idioma real de la app, no un texto suelto.
//
// Generar/aprobar los snapshots la PRIMERA vez (Verify siempre lanza VerifyException sin
// referencia - no autoaprueba nunca): correr este modo, mirar los .received.png en
// snapshots-visuales/, y si el render es el real, copiarlos a .verified.png a mano.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarSnapshotVisualSolo(Window window, MainViewModel vm)
    {
        bool algunFallo = false;

        // Contenido real y determinista (ver cabecera del fichero) - mismos item ids reales que
        // ya prueba KEEPQA_EQUIPINV_SOLO/H5-12 en este mismo arnes.
        if (vm.InventoryContainer != null)
        {
            for (int i = 0; i < 10 && i < vm.InventoryContainer.Slots.Count; i++)
                vm.InventoryContainer.Slots[i].PlaceItem(i + 1);
            vm.InventoryContainer.Slots[0].Count = 99;
        }
        else
        {
            Console.WriteLine("SNAPSHOT-VISUAL: FALLO preparando el hueco - no hay InventoryContainer real");
            algunFallo = true;
        }

        // Hallazgo real (17-sep-2026, mientras se verificaba el experimento del punto 4 del
        // encargo): Home.LastSessionCharacterName viene de session.json REAL en disco (ver
        // HomeViewModel.cs linea ~175) - el mismo archivo que usa la app real de este equipo, NO
        // algo que este arnes controle. Cuando esta vacio, la tarjeta "Continuar con..." (Border
        // con Visibility atado a NullToVis, MainWindow.xaml ~2370) desaparece y TODO lo de abajo
        // se reflota hacia arriba dentro de la misma ventana de tamaño fijo - un snapshot de
        // Inicio aprobado con la tarjeta visible comparado contra una corrida sin ella cae a SSIM
        // ~0.56 (medido de verdad, nada que ver con antialiasing). Fijarlo aqui a un valor
        // conocido es la MISMA logica que ya aplica el resto de este bloque (personaje/objetos
        // deterministas) - sin esto, el snapshot de Inicio queda a merced del uso real de la app
        // en esta maquina fuera de este arnes.
        vm.Home.LastSessionCharacterName = "UIA-Test";

        vm.SelectedTabIndex = 0; // Inicio - la tarjeta del personaje real ya cargado
        // Transiciones sutiles entre pestañas (17-sep-2026, encargo de pulido visual):
        // MainWindow.xaml.cs.OnRootTabSelectionChanged anima el contenido nuevo (fade+slide,
        // 180ms) - un DoEvents() normal NO avanza tiempo real, asi que capturar justo despues
        // del cambio arriesgaba fotografiar un fotograma A MEDIAS (opacidad<1, aun desplazado),
        // rompiendo estas 3 referencias aprobadas por un motivo ajeno al contenido real. 250ms
        // (>180ms real de la animacion, mismo margen que ya usa WaitForDispatcher(150) para el
        // tooltip en AuditoriaTransicion.cs) deja la animacion SIEMPRE terminada antes de
        // capturar - el pedido explicito del encargo es justo este: el estado FINAL debe seguir
        // siendo pixel-identico al de antes del cambio, la transicion en si no se fotografia
        // aqui (para eso esta verificarTransicion.js, ver EjecutarKeepQaTransicionSolo).
        WaitForDispatcher(250);
        DoEvents(); DoEvents(); DoEvents();

        byte[] pngInicio = CapturarPng(window, window.ActualWidth, window.ActualHeight);
        Console.WriteLine($"SNAPSHOT-VISUAL: ventana-principal-inicio capturada ({(int)window.ActualWidth}x{(int)window.ActualHeight}px, personaje='{vm.CharacterName}', IsCharacterLoaded={vm.IsCharacterLoaded})");
        if (!VerificarSnapshotVisual("ventana-principal-inicio", pngInicio)) algunFallo = true;

        // Panel concreto: Personaje -> Objetos -> Inventario. Mismo trio de indices real que ya
        // usa DRAG_SOLO/KEEPQA_EQUIPINV_SOLO/AuditoriaEquipamiento.cs (0=Equipamiento,
        // 1=Inventario segun el comentario real de MainViewModel.OnWindowSizeClassChanged linea
        // ~576 - "Inventario(1)/Almacenes(2)").
        vm.SelectedTabIndex = 1;      // Personaje
        vm.PersonajeInnerTabIndex = 0; // Objetos
        vm.ObjetosSubTabIndex = 1;     // Inventario
        WaitForDispatcher(250); // ver el comentario real de mas arriba - deja la transicion de pestaña terminada antes de capturar
        DoEvents(); DoEvents(); DoEvents();

        // El ContentControl real que muestra InventoryContainer (MainWindow.xaml tiene DOS -
        // layout ancho/estrecho - localizar por REFERENCIA real del Content, igual que
        // AuditoriaViewportScroll.cs identifica sus ScrollViewer, y quedarse con el que este
        // REALMENTE dispuesto (ActualWidth/Height>0), no solo IsVisible).
        var panelInventario = Descendientes<ContentControl>(window)
            .FirstOrDefault(cc => ReferenceEquals(cc.Content, vm.InventoryContainer) && cc.ActualWidth > 0 && cc.ActualHeight > 0);
        if (panelInventario == null)
        {
            Console.WriteLine("SNAPSHOT-VISUAL: FALLO - no se encuentra el ContentControl real del Inventario (ni ancho ni estrecho con tamaño real) en el arbol visual");
            algunFallo = true;
        }
        else
        {
            byte[] pngInventario = CapturarPng(panelInventario, panelInventario.ActualWidth, panelInventario.ActualHeight);
            Console.WriteLine($"SNAPSHOT-VISUAL: panel-inventario capturado ({(int)panelInventario.ActualWidth}x{(int)panelInventario.ActualHeight}px)");
            // Umbral propio, mas bajo que el global (ver SsimUmbralPanelInventario en
            // SnapshotVisual.cs) - este panel concreto mide un ruido real mas ancho que las
            // pantallas completas, documentado con las corridas reales, no un numero inventado.
            if (!VerificarSnapshotVisual("panel-inventario", pngInventario, SsimUmbralPanelInventario)) algunFallo = true;
        }

        // Mismo estado que el snapshot 1, en ingles - el chip de idioma real cambia la app EN
        // VIVO (SettingsViewModel.OnLanguageChanged -> LocalizationService.Instance.SetLanguage).
        string idiomaAntes = vm.Settings.Language;
        vm.SelectedTabIndex = 0; // Inicio
        vm.Settings.Language = "en";
        WaitForDispatcher(250); // ver el comentario real de mas arriba - deja la transicion de pestaña terminada antes de capturar
        DoEvents(); DoEvents(); DoEvents();

        byte[] pngIngles = CapturarPng(window, window.ActualWidth, window.ActualHeight);
        Console.WriteLine($"SNAPSHOT-VISUAL: ventana-principal-inicio-en capturada (Settings.Language='{vm.Settings.Language}')");
        if (!VerificarSnapshotVisual("ventana-principal-inicio-en", pngIngles)) algunFallo = true;

        vm.Settings.Language = idiomaAntes; // deja la app como estaba, mismo criterio que el resto del arnes
        DoEvents(); DoEvents();

        Console.WriteLine(algunFallo
            ? "SNAPSHOT-VISUAL: al menos un snapshot sin referencia aprobada o distinto (ver .received.png en snapshots-visuales/)"
            : "SNAPSHOT-VISUAL: los 3 snapshots coinciden con su referencia aprobada");
        Console.WriteLine("DONE (SNAPSHOT_VISUAL_SOLO)");
        Environment.Exit(algunFallo ? 1 : 0);
    }
}
