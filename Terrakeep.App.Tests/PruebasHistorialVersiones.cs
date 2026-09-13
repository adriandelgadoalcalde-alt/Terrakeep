// BK (13-sep-2026, encargo "historial de copias de seguridad con versiones reales"): verificacion
// REAL del ciclo completo contra la app en marcha - no pruebas unitarias sobre el servicio suelto
// (esas ya estan en Terrakeep.App.ViewModels.Tests\BackupHistoryServiceTests), sino la cadena
// entera: MainWindow real -> editar -> pulsar Guardar -> mirar el disco -> abrir el panel real ->
// restaurar desde el -> comparar BYTE A BYTE.
//
// Bloque propio (otra parte de la misma clase parcial, ver el comentario de cabecera de
// Program.cs) y con modo de foco propio, BK_SOLO=1, por el mismo motivo medido que PB_SOLO/
// A11_SOLO: el recorrido completo del arnes hace decenas de RenderTargetBitmap y en esta sesion
// no es repetible de principio a fin.
//
// Nunca toca ficheros reales del usuario: trabaja sobre una COPIA del .plr en una carpeta
// temporal propia, y ademas apunta la raiz de historial (BackupsRoot) a esa misma carpeta, asi
// que ni una sola copia acaba en el %LOCALAPPDATA% real. Todo se borra al terminar.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Terrakeep.App;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

internal static partial class Program
{
    private static void PruebasHistorialDeVersiones(MainWindow window, MainViewModel vm)
    {
        string tmpRaiz = Path.Combine(Path.GetTempPath(), "terrakeep-bk-" + Guid.NewGuid().ToString("N")[..8]);
        string dirPersonajes = Path.Combine(tmpRaiz, "Players");
        Directory.CreateDirectory(dirPersonajes);

        var servicio = vm.BackupHistory.Service;
        string raizOriginal = servicio.BackupsRoot;
        int cupoOriginal = servicio.MaxBackupsPerCharacter;
        servicio.BackupsRoot = Path.Combine(tmpRaiz, "Backups");
        // El arnes HEREDA los ajustes reales de esta maquina (Settings.LoadFromDisk en el
        // constructor de MainWindow) - misma trampa ya documentada en CLAUDE.md para
        // session.json. La primera ejecucion real de este bloque salio con cupo 1 heredado de
        // %LOCALAPPDATA%\Terrakeep\settings.json y "3 guardados -> 1 version" parecia un bug del
        // historial cuando era el tope configurado haciendo exactamente su trabajo. El cupo se
        // fija aqui a proposito para que BK-01..BK-06 midan lo que dicen medir; BK-07 lo baja
        // luego a 5 para saturarlo de verdad.
        servicio.MaxBackupsPerCharacter = 20;

        try
        {
            // ---------------------------------------------------------------------------
            // Preparacion: una COPIA de un personaje REAL de esta maquina (no uno sintetico -
            // el resumen del panel y el round-trip solo valen de verdad sobre datos reales).
            // ---------------------------------------------------------------------------
            string? origen = CharacterFileService.GetAllPlayersDirectories()
                .Where(Directory.Exists)
                .SelectMany(d => Directory.GetFiles(d, "*.plr"))
                .OrderByDescending(f => new FileInfo(f).Length)
                .FirstOrDefault();
            if (origen == null)
            {
                Console.WriteLine("BK-SALTADO: no hay ningun .plr real en esta maquina con el que probar");
                return;
            }

            string plr = Path.Combine(dirPersonajes, "PruebaHistorial.plr");
            File.Copy(origen, plr, overwrite: true);
            string tplrOrigen = Path.ChangeExtension(origen, ".tplr");
            if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(plr, ".tplr"), overwrite: true);
            byte[] bytesOriginales = File.ReadAllBytes(plr);
            Console.WriteLine($"BK-00-PREP: copia de '{Path.GetFileName(origen)}' ({bytesOriginales.Length} bytes) en {plr}");

            vm.LoadFromPath(plr);
            DoEvents();
            if (!vm.IsCharacterLoaded)
            {
                Console.WriteLine("FALLO: BK-00-PREP - la copia real no se pudo cargar en la app, el resto del bloque no valdria nada");
                return;
            }
            string nombreOriginal = vm.CharacterName ?? "";
            Console.WriteLine($"BK-00-PREP: cargado en la app real -> '{nombreOriginal}', vida {vm.Appearance.HealthMax}");

            // ---------------------------------------------------------------------------
            // BK-01: TRES guardados reales seguidos = TRES versiones reales en disco.
            // Se editan datos distintos en cada vuelta para poder distinguirlas despues.
            // ---------------------------------------------------------------------------
            string[] nombres = ["BK-Uno", "BK-Dos", "BK-Tres"];
            foreach (string nuevo in nombres)
            {
                vm.CharacterName = nuevo;
                DoEvents();
                vm.SaveCommand.Execute(null);
                DoEvents();
            }

            var versiones = servicio.ListBackups(plr);
            Console.WriteLine($"BK-01-VARIOS: 3 guardados reales seguidos -> {versiones.Count} versiones en disco (esperado 3)");
            if (versiones.Count != 3)
                Console.WriteLine("FALLO: BK-01-VARIOS - guardar varias veces no deja una version por guardado");
            foreach (var v in versiones)
                Console.WriteLine($"   · {v.TimestampLocal:HH:mm:ss.fff} · {v.Reason} · {v.SizeBytes} B · {Path.GetFileName(v.ContainerPath)} · {v.Info?.CharacterName}");

            // ---------------------------------------------------------------------------
            // BK-02: el snapshot es ANTES de escribir (BK-1 del encargo). La version MAS
            // ANTIGUA de las tres tiene que llevar dentro el personaje TAL Y COMO ESTABA
            // antes del primer guardado, o sea con su nombre original - si el orden fuera el
            // de antes (copiar despues de escribir) ahi pondria ya "BK-Uno".
            // ---------------------------------------------------------------------------
            var masAntigua = versiones[^1];
            string nombreDentro = masAntigua.Info?.CharacterName ?? "(sin resumen)";
            bool antesDeEscribir = nombreDentro == nombreOriginal;
            Console.WriteLine($"BK-02-ANTES: la version mas antigua lleva dentro '{nombreDentro}' (esperado '{nombreOriginal}', el estado PREVIO al primer guardado)");
            if (!antesDeEscribir)
                Console.WriteLine("FALLO: BK-02-ANTES - el historial guarda el estado POSTERIOR a la escritura: el archivo original de la sesion no queda en ningun sitio");

            // Y byte a byte, no solo "el nombre cuadra".
            byte[] dentro = servicio.ReadPlrBytes(masAntigua);
            bool igualByteAByte = dentro.SequenceEqual(bytesOriginales);
            Console.WriteLine($"BK-02-BYTES: la version mas antigua es identica byte a byte al archivo original ({dentro.Length} vs {bytesOriginales.Length} bytes) -> {igualByteAByte} (esperado True)");
            if (!igualByteAByte)
                Console.WriteLine("FALLO: BK-02-BYTES - la copia no reproduce exactamente el archivo que habia antes de guardar");

            // ---------------------------------------------------------------------------
            // BK-03: restaurar por el PANEL REAL (comando del ViewModel que pulsa la UI) deja
            // el archivo de disco exactamente como estaba en ese punto. Se comprueba ademas
            // que la confirmacion es obligatoria: AskRestore no restaura NADA por si sola.
            // ---------------------------------------------------------------------------
            vm.OpenBackupHistoryCommand.Execute(null);
            DoEvents();
            Console.WriteLine($"BK-03-PANEL: panel abierto={vm.BackupHistory.IsOpen} con {vm.BackupHistory.Points.Count} filas (esperado True / 3)");

            var filaAntigua = vm.BackupHistory.Points.Last();
            byte[] antesDePedir = File.ReadAllBytes(plr);
            vm.BackupHistory.AskRestoreCommand.Execute(filaAntigua);
            DoEvents();
            bool pideConfirmacion = filaAntigua.IsConfirmingRestore && File.ReadAllBytes(plr).SequenceEqual(antesDePedir);
            Console.WriteLine($"BK-03-CONFIRMA: pulsar 'Restaurar' pide confirmacion y NO toca el archivo todavia -> {pideConfirmacion} (esperado True)");
            if (!pideConfirmacion)
                Console.WriteLine("FALLO: BK-03-CONFIRMA - una accion irreversible se esta ejecutando sin confirmar");

            // Captura real del panel CON la confirmacion abierta - es el momento que de verdad
            // hay que mirar a ojo (que se lea bien el aviso y no se solape con nada).
            CapturaVentana(window, "bk-panel-historial-confirmacion.png");

            vm.BackupHistory.ConfirmRestoreCommand.Execute(filaAntigua);
            DoEvents();
            byte[] trasRestaurar = File.ReadAllBytes(plr);
            bool restauradoExacto = trasRestaurar.SequenceEqual(bytesOriginales);
            Console.WriteLine($"BK-03-RESTAURA: tras confirmar, el archivo real vuelve a ser identico byte a byte al original -> {restauradoExacto} (esperado True)");
            if (!restauradoExacto)
                Console.WriteLine("FALLO: BK-03-RESTAURA - restaurar no deja el archivo exactamente como estaba en ese punto");
            Console.WriteLine($"BK-03-MENSAJE: {vm.BackupHistory.StatusText}");

            // La restauracion tambien se puede deshacer: antes de pisar el archivo se guardo una
            // version nueva con motivo BeforeRestore.
            var trasRestaurarLista = servicio.ListBackups(plr);
            bool hayRedDeSeguridad = trasRestaurarLista.Any(v => v.Reason == BackupReason.BeforeRestore);
            Console.WriteLine($"BK-04-REDVUELTA: restaurar dejo antes una copia del estado que se iba a pisar -> {hayRedDeSeguridad} (esperado True; {trasRestaurarLista.Count} versiones ahora)");
            if (!hayRedDeSeguridad)
                Console.WriteLine("FALLO: BK-04-REDVUELTA - restaurar destruye el estado actual sin dejar copia");

            // El editor cargado tiene que reflejar ya el personaje restaurado, no el de antes.
            Console.WriteLine($"BK-05-RECARGA: el editor muestra '{vm.CharacterName}' tras restaurar (esperado '{nombreOriginal}')");
            if (vm.CharacterName != nombreOriginal)
                Console.WriteLine("FALLO: BK-05-RECARGA - el editor sigue con el estado viejo en memoria; el siguiente Guardar lo machacaria");

            // ---------------------------------------------------------------------------
            // BK-06: copia manual bajo demanda desde el propio panel.
            // ---------------------------------------------------------------------------
            int antesManual = vm.BackupHistory.Points.Count;
            vm.BackupHistory.CreateManualBackupCommand.Execute(null);
            DoEvents();
            var manual = vm.BackupHistory.Points.FirstOrDefault(p => p.Entry.Reason == BackupReason.Manual);
            Console.WriteLine($"BK-06-MANUAL: 'Crear copia ahora' -> {antesManual} -> {vm.BackupHistory.Points.Count} versiones, manual encontrada={manual != null}");
            if (manual == null) Console.WriteLine("FALLO: BK-06-MANUAL - la copia manual no aparece en el historial");
            else Console.WriteLine($"   · resumen real de esa fila: {manual.WhenText} · {manual.ReasonText} · {manual.SizeText} · {manual.SummaryText}");

            CapturaVentana(window, "bk-panel-historial.png");

            // ---------------------------------------------------------------------------
            // BK-07: el cupo. Se baja a 5 y se satura con 30 guardados REALES por la UI -
            // no llamando al servicio a mano, para que sea de verdad el camino que recorre el
            // usuario. Debe quedarse clavado en 5 y conservar la copia MANUAL (que es mas
            // antigua que todas las automaticas que entran despues).
            // ---------------------------------------------------------------------------
            servicio.MaxBackupsPerCharacter = 5;
            for (int i = 0; i < 30; i++)
            {
                vm.CharacterName = "BK-Cupo" + i;
                vm.SaveCommand.Execute(null);
            }
            DoEvents();
            var trasSaturar = servicio.ListBackups(plr);
            long bytesEnDisco = servicio.MeasureHistorySize(plr);
            bool cupoRespetado = trasSaturar.Count == 5;
            bool manualSobrevive = trasSaturar.Any(v => v.Reason == BackupReason.Manual);
            Console.WriteLine($"BK-07-CUPO: 30 guardados reales con el cupo en 5 -> {trasSaturar.Count} versiones (esperado 5), {bytesEnDisco / 1024.0:0.0} KB en disco");
            Console.WriteLine($"BK-07-CUPO: la copia MANUAL (la mas antigua de todas) sigue ahi -> {manualSobrevive} (esperado True)");
            if (!cupoRespetado) Console.WriteLine("FALLO: BK-07-CUPO - el historial crece por encima del tope configurado");
            if (!manualSobrevive) Console.WriteLine("FALLO: BK-07-CUPO - una copia marcada a mano se retira antes que las automaticas");

            vm.BackupHistory.Reload();
            DoEvents();
            Console.WriteLine($"BK-07-PANEL: el pie del panel dice -> {vm.BackupHistory.FooterText}");

            // ---------------------------------------------------------------------------
            // BK-08: la interfaz de verdad. UI Automation real sobre la ventana: el panel se
            // ve, sus botones existen como controles reales y se leen con su texto traducido.
            // ---------------------------------------------------------------------------
            var raiz = AutomationElement.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle);
            var botonCrear = raiz?.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, LocalizationService.Instance["backup_create_now"]));
            var botonRestaurar = raiz?.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, LocalizationService.Instance["backup_restore"]));
            Console.WriteLine($"BK-08-UIA: boton '{LocalizationService.Instance["backup_create_now"]}' visible para UI Automation={botonCrear != null}, " +
                              $"boton '{LocalizationService.Instance["backup_restore"]}' de fila={botonRestaurar != null} (esperado los dos True)");
            if (botonCrear == null || botonRestaurar == null)
                Console.WriteLine("FALLO: BK-08-UIA - el panel no expone sus acciones como controles reales");

            var lista = Descendientes<ItemsControl>(window).FirstOrDefault(c => c.Name == "BackupPointsList");
            var filasVisibles = lista == null ? 0 : Descendientes<System.Windows.Controls.Button>(lista)
                .Count(b => b.IsVisible && (b.Content as string) == LocalizationService.Instance["backup_restore"]);
            Console.WriteLine($"BK-08-FILAS: filas con boton 'Restaurar' realmente visibles en pantalla={filasVisibles} (esperado {trasSaturar.Count})");
            if (filasVisibles != trasSaturar.Count)
                Console.WriteLine("FALLO: BK-08-FILAS - lo que se ve en el panel no coincide con lo que hay en disco");

            // ---------------------------------------------------------------------------
            // BK-8b: el panel a la ventana MAS PEQUEÑA que la app permite (MinWidth/MinHeight
            // reales de MainWindow) y con el cupo LLENO - es donde de verdad se rompe una lista
            // larga dentro de un contenedor de alto fijo. Se mide con el mismo detector real que
            // usa el resto del arnes: si WPF esta recortando algo, se ve aqui.
            // ---------------------------------------------------------------------------
            servicio.MaxBackupsPerCharacter = 20;
            for (int i = 0; i < 20; i++) { vm.CharacterName = "BK-Lleno" + i; vm.SaveCommand.Execute(null); }
            vm.BackupHistory.Reload();
            double anchoPrevio = window.Width, altoPrevio = window.Height;
            FijarTamaño(window, window.MinWidth, window.MinHeight);
            DoEvents(); DoEvents();
            var listaMin = Descendientes<ItemsControl>(window).FirstOrDefault(c => c.Name == "BackupPointsList");
            var scrollMin = listaMin == null ? null : Descendientes<ScrollViewer>(window)
                .FirstOrDefault(s => s.IsAncestorOf(listaMin));
            bool haySccroll = scrollMin is { ScrollableHeight: > 0 };
            Console.WriteLine($"BK-8b-MINIMO: ventana al minimo real ({window.ActualWidth:0}x{window.ActualHeight:0}) con {vm.BackupHistory.Points.Count} versiones -> " +
                              $"la lista desplaza de verdad={haySccroll} (alto desplazable {scrollMin?.ScrollableHeight:0})");
            if (!haySccroll)
                Console.WriteLine("FALLO: BK-8b-MINIMO - con el historial lleno y la ventana al minimo la lista no desplaza: hay versiones inalcanzables");
            var botonCrearMin = Descendientes<System.Windows.Controls.Button>(window).FirstOrDefault(b => b.Name == "BackupCreateNowButton");
            bool pieVisible = botonCrearMin is { IsVisible: true, ActualWidth: > 0 };
            Console.WriteLine($"BK-8b-MINIMO: el pie del panel ('Crear copia ahora') sigue visible entero={pieVisible} (esperado True)");
            if (!pieVisible) Console.WriteLine("FALLO: BK-8b-MINIMO - el pie del panel se pierde al encoger la ventana");
            CapturaVentana(window, "bk-panel-historial-minimo.png");
            FijarTamaño(window, anchoPrevio, altoPrevio);
            DoEvents();

            // ---------------------------------------------------------------------------
            // BK-09: cerrar con Escape (el panel tapa el editor, tiene que soltarse facil).
            // ---------------------------------------------------------------------------
            vm.BackupHistory.CloseCommand.Execute(null);
            DoEvents();
            Console.WriteLine($"BK-09-CERRAR: panel abierto tras cerrar={vm.BackupHistory.IsOpen} (esperado False)");

            // ---------------------------------------------------------------------------
            // BK-9b: el OTRO camino real de entrada al panel - la tarjeta de Inicio. Es un
            // cableado distinto (HomeViewModel.BackupHistoryRequested -> MainViewModel) y sirve
            // ademas para personajes que NO estan cargados, asi que hay que probarlo aparte.
            // ---------------------------------------------------------------------------
            vm.Home.RefreshCommand.Execute(null);
            WaitForDispatcher(600);
            var tarjeta = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, plr, StringComparison.OrdinalIgnoreCase))
                          ?? vm.Home.Characters.FirstOrDefault();
            if (tarjeta != null)
            {
                vm.Home.ShowBackupHistoryCommand.Execute(tarjeta);
                DoEvents();
                Console.WriteLine($"BK-9b-INICIO: el menu contextual de la tarjeta '{tarjeta.Name}' abre el panel -> abierto={vm.BackupHistory.IsOpen}, " +
                                  $"personaje del panel='{vm.BackupHistory.CharacterName}' (esperado True / '{tarjeta.Name}')");
                if (!vm.BackupHistory.IsOpen)
                    Console.WriteLine("FALLO: BK-9b-INICIO - la tarjeta de Inicio no abre el historial");
                vm.BackupHistory.CloseCommand.Execute(null);
                DoEvents();
            }
            else Console.WriteLine("BK-9b-INICIO: SALTADO - Inicio no devolvio ninguna tarjeta");

            // ---------------------------------------------------------------------------
            // BK-10: limite global - historiales huerfanos. Se borra el .plr de prueba: su
            // historial pasa a ser huerfano y la limpieza a mano lo retira de verdad.
            // ---------------------------------------------------------------------------
            File.Delete(plr);
            string tplrPrueba = Path.ChangeExtension(plr, ".tplr");
            if (File.Exists(tplrPrueba)) File.Delete(tplrPrueba);
            var huerfanos = servicio.FindOrphanHistories().Where(o => o.DisplayName == "PruebaHistorial").ToList();
            Console.WriteLine($"BK-10-HUERFANOS: historiales sin personaje detectados={huerfanos.Count} ({huerfanos.Sum(o => o.SizeBytes) / 1024.0:0.0} KB)");
            var sinGracia = servicio.PurgeOrphanHistories(TimeSpan.FromDays(90));
            Console.WriteLine($"BK-10-GRACIA: limpieza automatica con los 90 dias de gracia reales -> {sinGracia.Carpetas} carpetas (esperado 0: es de hace un momento, no se toca sola)");
            if (sinGracia.Carpetas != 0)
                Console.WriteLine("FALLO: BK-10-GRACIA - la limpieza automatica borra historiales recien quedados huerfanos");
            var aMano = servicio.DeleteOrphanHistories(huerfanos);
            Console.WriteLine($"BK-10-LIMPIEZA: limpieza bajo demanda -> {aMano.Carpetas} carpetas, {aMano.Bytes / 1024.0:0.0} KB liberados (esperado {huerfanos.Count})");
            if (aMano.Carpetas != huerfanos.Count)
                Console.WriteLine("FALLO: BK-10-LIMPIEZA - la limpieza a mano no retira los historiales huerfanos");
        }
        catch (Exception ex)
        {
            Console.WriteLine("BK-EXCEPTION: " + ex);
        }
        finally
        {
            servicio.BackupsRoot = raizOriginal;
            servicio.MaxBackupsPerCharacter = cupoOriginal;
            try { Directory.Delete(tmpRaiz, recursive: true); } catch (Exception) { }
        }
    }

    private static void CapturaVentana(Window window, string nombre)
    {
        try
        {
            DoEvents();
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string destino = Path.Combine(AppContext.BaseDirectory, nombre);
            using var fs = File.Create(destino);
            enc.Save(fs);
            Console.WriteLine("CAPTURA: " + destino);
        }
        catch (Exception ex)
        {
            Console.WriteLine("CAPTURA-EXCEPTION: " + ex.Message);
        }
    }
}
