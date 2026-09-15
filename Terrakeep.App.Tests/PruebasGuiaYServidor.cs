using System.IO;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using ServidorKeep.Core.Instancias;

// Fase B (integracion de la Guia + hosting de ServidorKeep, 15-sep-2026): verificacion real
// nueva de este arnes, en fichero propio (mismo criterio ya establecido en el proyecto para no
// colisionar con el Main() de 7000+ lineas de Program.cs entre rondas en paralelo - ver el
// comentario de AuditoriaBarraExploracion.cs). Dos modos de foco, GUIA_SOLO=1 y HOSTING_SOLO=1,
// enganchados en Program.cs junto al resto de modos *_SOLO.
internal static partial class Program
{
    // GUIA_SOLO=1: carga una COPIA de un personaje Calamity real ('adrian.plr'+'adrian.tplr')
    // y de un mundo real ('roca_negra.wld') - NUNCA los originales de Documentos, mismo criterio
    // ya establecido en todo este arnes - y comprueba que la Guia evalua contra ellos de verdad:
    // el arbol carga tramos reales, el aviso de Calamity se enciende porque el personaje SI tiene
    // datos de Calamity, y los textos sincronizados desde TerrakeepMod se resuelven (nunca una
    // clave cruda entre corchetes, que seria la señal de que textos.es.json no se cargo).
    private static void EjecutarGuiaReal(MainWindow window, MainViewModel vm)
    {
        string origenPlr = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players\adrian.plr";
        string origenTplr = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players\adrian.tplr";
        string origenWld = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";

        if (!File.Exists(origenPlr))
        {
            Console.WriteLine("GUIA_SOLO: adrian.plr no esta en esta maquina - omitido.");
            return;
        }

        string tempDir = Path.GetTempPath();
        string copiaPlr = Path.Combine(tempDir, "guia-harness-adrian.plr");
        string copiaTplr = Path.Combine(tempDir, "guia-harness-adrian.tplr");
        File.Copy(origenPlr, copiaPlr, overwrite: true);
        if (File.Exists(origenTplr)) File.Copy(origenTplr, copiaTplr, overwrite: true);

        try
        {
            vm.LoadFromPath(copiaPlr);
            DoEvents();
            Console.WriteLine($"GUIA_SOLO: personaje cargado -> HasCalamityData={vm.HasCalamityData} (esperado True, 'adrian' tiene .tplr real)");

            if (File.Exists(origenWld))
            {
                string copiaWld = Path.Combine(tempDir, "guia-harness-roca_negra.wld");
                File.Copy(origenWld, copiaWld, overwrite: true);
                var carga = vm.Exploration.LoadFromPathAsync(copiaWld);
                while (!carga.IsCompleted) DoEvents();
                DoEvents();
                Console.WriteLine($"GUIA_SOLO: mundo cargado -> IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
            }

            vm.SelectedTabIndex = 6; // AppTab.Guia
            DoEvents();
            vm.Guide.Refresh();
            DoEvents();

            Console.WriteLine($"GUIA_SOLO: Tramos.Count={vm.Guide.Tramos.Count} (esperado 46, el total real del .json sincronizado)");
            if (vm.Guide.Tramos.Count == 0)
                Console.WriteLine("FALLO: GUIA_SOLO - el catalogo no cargo ningun tramo.");

            Console.WriteLine($"GUIA_SOLO: MostrarAvisoCalamity={vm.Guide.MostrarAvisoCalamity} (esperado True)");
            if (!vm.Guide.MostrarAvisoCalamity)
                Console.WriteLine("FALLO: GUIA_SOLO - el personaje tiene Calamity pero el aviso no se enciende.");

            // Un tramo/paso cuyo texto no se resolvio se veria "[Guia.Tramo.XXX.Nombre]" (mismo
            // contrato honesto que LocalizationService/GuideTextCatalog: una clave sin traducir
            // se ve literal, nunca en blanco) - si el .json de sincronizacion faltara o
            // estuviera vacio, ESTE es el sintoma real que aparecería.
            int tramosConTextoRoto = 0;
            foreach (var tramo in vm.Guide.Tramos)
            {
                if (tramo.Nombre.StartsWith('[')) tramosConTextoRoto++;
                foreach (var paso in tramo.Pasos)
                    if (paso.Titulo.StartsWith('[')) tramosConTextoRoto++;
            }
            Console.WriteLine($"GUIA_SOLO: tramos/pasos con texto sin resolver (clave cruda entre corchetes)={tramosConTextoRoto} (esperado 0)");
            if (tramosConTextoRoto > 0)
                Console.WriteLine("FALLO: GUIA_SOLO - hay texto de la Guia sin traducir (¿textos.es.json no se sincronizo bien?).");

            if (vm.Guide.ObjetivoPaso != null)
                Console.WriteLine($"GUIA_SOLO: objetivo actual real -> tramo='{vm.Guide.ObjetivoTramo?.Nombre}', paso='{vm.Guide.ObjetivoPaso.Titulo}', {vm.Guide.ObjetivoPaso.Cumplidos}/{vm.Guide.ObjetivoPaso.TotalObligatorios} obligatorios cumplidos, {vm.Guide.ObjetivoPaso.Requisitos.Count} requisito(s) mostrados.");
            else
                Console.WriteLine("GUIA_SOLO: sin objetivo pendiente en el camino principal (o el catalogo no marca ningun tramo obligatorio implementado) - " + vm.Guide.TextoSinObjetivo);

            // Al menos una bandera de mundo real debe ser evaluable (no "no evaluable") con
            // roca_negra.wld cargado - confirma que GuideFlags/WldHeader.HardMode etc. estan de
            // verdad conectados, no solo que el arbol se construyo.
            if (vm.Exploration.IsWorldLoaded)
            {
                bool algunaBanderaEvaluada = false;
                foreach (var tramo in vm.Guide.Tramos)
                    foreach (var paso in tramo.Pasos)
                        foreach (var req in paso.Requisitos)
                            if (!req.NoEvaluable) algunaBanderaEvaluada = true;
                Console.WriteLine($"GUIA_SOLO: algun requisito evaluable de verdad con mundo+personaje reales cargados={algunaBanderaEvaluada} (esperado True)");
                if (!algunaBanderaEvaluada)
                    Console.WriteLine("FALLO: GUIA_SOLO - con personaje y mundo reales cargados, TODOS los requisitos salen no-evaluables.");
            }

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string shot = Path.Combine(AppContext.BaseDirectory, "guia-real.png");
            using (var fs = File.Create(shot)) enc.Save(fs);
            Console.WriteLine($"GUIA_SOLO: captura real -> {shot}");
        }
        finally
        {
            try { File.Delete(copiaPlr); } catch { }
            try { File.Delete(copiaTplr); } catch { }
        }
    }

    // HOSTING_SOLO=1: lanza un servidor de Terraria REAL (vainilla, sin tModLoader) desde el
    // MISMO camino que pulsaria un usuario (Hosting.IniciarCommand), espera a EnEscucha de
    // verdad (ver InstanciaServidor/SeguidorDeLog de ServidorKeep.Core) y lo detiene - mismo
    // nivel de exigencia que "herramientas\Verificacion" de ServidorKeep (nunca dar un boton por
    // bueno solo porque no lanzo excepcion). Puede tardar decenas de segundos de verdad
    // (autocreate de un mundo Pequeño) - se trata un timeout como AVISO, no como FALLO de la UI,
    // porque no mide un bug de Terrakeep si el motor tarda mas de lo esperado.
    private static void EjecutarHostingReal(MainWindow window, MainViewModel vm)
    {
        vm.SelectedTabIndex = 7; // AppTab.Hosting
        DoEvents();

        Console.WriteLine($"HOSTING_SOLO: TerrariaDetectado={vm.Hosting.TerrariaDetectado}, TModLoaderDetectado={vm.Hosting.TModLoaderDetectado}, ModsDisponibles(vainilla)={vm.Hosting.ModsDisponibles.Count} (esperado 0, UsarTModLoader empieza en false)");
        if (!vm.Hosting.TerrariaDetectado)
        {
            Console.WriteLine("HOSTING_SOLO: Terraria no detectado en esta maquina - omitido (mismo motivo real que documenta ServidorKeep si no esta instalado por Steam).");
            return;
        }

        vm.Hosting.NombreInstancia = "Terrakeep-Guia-HOSTING_SOLO";
        vm.Hosting.NombreMundo = "TerrakeepHostingSolo";
        vm.Hosting.Puerto = 27977; // fuera del 7777 por defecto, evita chocar con un servidor real que el usuario pueda tener abierto
        vm.Hosting.IniciarCommand.Execute(null);
        DoEvents();

        if (!string.IsNullOrEmpty(vm.Hosting.ErrorMessage))
        {
            Console.WriteLine("FALLO: HOSTING_SOLO - IniciarCommand devolvio un error real: " + vm.Hosting.ErrorMessage);
            return;
        }
        if (vm.Hosting.Instancias.Count == 0)
        {
            Console.WriteLine("FALLO: HOSTING_SOLO - IniciarCommand no añadio ninguna instancia.");
            return;
        }

        var instancia = vm.Hosting.Instancias[^1];
        Console.WriteLine($"HOSTING_SOLO: instancia real lanzada -> PID={instancia.Nucleo.Proceso.Id}, puerto={instancia.Puerto}, carpeta={instancia.CarpetaInstancia}");

        long limite = Environment.TickCount64 + 90_000;
        while (instancia.Nucleo.Estado == EstadoInstancia.Arrancando && Environment.TickCount64 < limite)
        {
            DoEvents();
            System.Threading.Thread.Sleep(200);
        }
        DoEvents();

        Console.WriteLine($"HOSTING_SOLO: estado real tras esperar -> {instancia.Nucleo.Estado} (EstadoTexto UI='{instancia.EstadoTexto}')");
        if (instancia.Nucleo.Estado == EstadoInstancia.EnEscucha)
        {
            bool escuchando = false;
            try
            {
                using var cliente = new System.Net.Sockets.TcpClient();
                var conectar = cliente.ConnectAsync("127.0.0.1", instancia.Puerto);
                escuchando = conectar.Wait(3000) && cliente.Connected;
            }
            catch { }
            Console.WriteLine($"HOSTING_SOLO: conexion TCP real a 127.0.0.1:{instancia.Puerto} -> {(escuchando ? "OK, el puerto responde de verdad" : "no respondio")}");
        }
        else
        {
            Console.WriteLine("AVISO: HOSTING_SOLO - la instancia no llego a EnEscucha dentro del timeout (puede ser solo lentitud del autocreate real, no necesariamente un bug de Terrakeep).");
        }

        // PID capturado ANTES de detener: InstanciaServidor.DetenerAsync() acaba llamando a
        // Dispose(), que dispone el propio objeto Process - preguntarle HasExited DESPUES lanza
        // InvalidOperationException ("No process is associated with this object"), no porque el
        // proceso siga vivo sino porque el objeto .NET ya se libero (confirmado real la primera
        // vez que se corrio este bloque). Comprobar por PID contra la lista real de procesos del
        // sistema evita esa trampa - verificacion honesta, no un try/catch que se trague el fallo real.
        int pidReal = instancia.Nucleo.Proceso.Id;
        var detener = instancia.DetenerCommand.ExecuteAsync(null);
        long limiteParada = Environment.TickCount64 + 15_000;
        while (!detener.IsCompleted && Environment.TickCount64 < limiteParada) DoEvents();
        DoEvents();
        bool sigueVivo = System.Diagnostics.Process.GetProcesses().Any(p => p.Id == pidReal);
        Console.WriteLine($"HOSTING_SOLO: tras Detener -> proceso PID={pidReal} sigue vivo={sigueVivo} (esperado False)");
        if (sigueVivo)
            Console.WriteLine("FALLO: HOSTING_SOLO - el proceso real del servidor sigue vivo tras Detener.");

        try { Directory.Delete(instancia.CarpetaInstancia, recursive: true); } catch { }
    }
}
