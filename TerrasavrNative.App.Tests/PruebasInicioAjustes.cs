// Oleada grande de pruebas del 6-sep-2026, area "Inicio, Ajustes, Novedades, Acerca de".
//
// Parte de la MISMA clase Program que el arnes de siempre (`partial`) - vive en su propio
// fichero solo para que esta area y las otras cinco que se estan probando en paralelo no se
// pisen editando el mismo sitio. Usa los mismos helpers reales del arnes (DoEvents,
// FijarTamaño, Descendientes<T>, RectVisible/VisibleEntero...) y el mismo criterio de siempre:
// nada "a ojo", coordenadas y ficheros reales, y todo bloque que cambie estado compartido lo
// deja como estaba.
//
// Por que hace falta, si ya habia comprobaciones de estas cuatro pantallas: lo que habia
// (HOME-SCAN, I-a/I-b, H5-04, H5-05, H5-07, A9-12/A9-13, A10-CREDITOS y el barrido de
// maquetacion AR-17) cubre el camino feliz de cada pieza por separado y la maquetacion a muchos
// tamaños. Ninguno miraba estas tres familias, que es donde salio todo lo real de esta ronda:
//
//  1. TEXTO CONGELADO. Un binding a `Loc[clave]` se refresca solo al cambiar de idioma (WPF
//     reevalua cualquier binding indexado al recibir "Item[]"); una propiedad NORMAL que por
//     dentro leyo el diccionario UNA vez, no. A10-IDIOMA-BARRIDO no lo ve: compara el texto
//     renderizado contra una lista de palabras sospechosas, y estos textos o no estan en
//     pantalla en ese instante (el mensaje del escaneo, el aviso de fichero cambiado) o no son
//     un TextBlock de la ventana (los ToolTip).
//  2. EL CICLO REAL de las acciones del menu contextual de una tarjeta (duplicar, restaurar
//     copia, historial) sobre ficheros de verdad: I-b solo comprobaba que los Command del menu
//     RESUELVEN, nunca que hagan lo que dicen.
//  3. EL TICK de "fijar tamaño de ventana", que se dispara SOLO al cargar los ajustes (evento
//     Checked de un CheckBox con binding), sin que el usuario lo toque.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

internal static partial class Program
{
    private static void PruebasInicioAjustesNovedadesAcercaDe(MainViewModel vm, Window window)
    {
        PruebasInicio(vm);
        PruebasTarjetasDeInicio(vm, window);
        PruebasRestaurarCopia(vm, window);
        PruebasAjustes(vm, window);
        PruebasAjustesPersistidos(vm);
        PruebasSelectorDeIdioma(vm, window);
        PruebasNovedades(vm);
        PruebasAcercaDe(vm, window);
    }

    private static byte[] PlrDePrueba(string nombre) => PlrFile.Write(new PlrCharacter
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    });

    private static void PruebasInicio(MainViewModel vm)
    {
        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex;
        string dir = Path.Combine(Path.GetTempPath(), $"terrakeep-inicio-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);

            // ---- INI-01: coherencia real del listado ya escaneado ----
            // Cinco invariantes que nadie comprobaba y que, si se rompen, se ven como una lista
            // con personajes repetidos (el bug real que H5-07 ya arreglo una vez con el contador
            // de generacion), una tarjeta sin doll, o una fila de insignias que se contradice a
            // si misma (Vanilla Y tModLoader a la vez).
            var lista = vm.Home.Characters.ToList();
            int duplicados = lista.GroupBy(c => c.FilePath, StringComparer.OrdinalIgnoreCase).Count(g => g.Count() > 1);
            int sinFichero = lista.Count(c => !File.Exists(c.FilePath));
            int sinDoll = lista.Count(c => c.Preview == null || c.Preview.PixelWidth <= 0);
            int insigniasIncoherentes = lista.Count(c => c.IsVanilla == c.IsTModLoader || (c.IsCalamity && !c.IsTModLoader));
            bool ordenadoPorFecha = lista.Zip(lista.Skip(1)).All(p =>
                File.GetLastWriteTimeUtc(p.First.FilePath) >= File.GetLastWriteTimeUtc(p.Second.FilePath).AddSeconds(-1));
            Console.WriteLine($"INI-01-ESCANEO: {lista.Count} personaje(s) reales -> rutas duplicadas={duplicados} (esperado 0), sin fichero en disco={sinFichero} (esperado 0), " +
                              $"sin doll renderizado={sinDoll} (esperado 0), insignias que se contradicen={insigniasIncoherentes} (esperado 0), orden por fecha descendente={ordenadoPorFecha} (esperado True)");
            if (duplicados > 0) Console.WriteLine("FALLO: INI-01 - el listado de Inicio tiene el mismo .plr mas de una vez (dos vueltas de RefreshAsync pisandose)");
            if (sinFichero > 0) Console.WriteLine("FALLO: INI-01 - el listado de Inicio ofrece un personaje cuyo fichero ya no existe");
            if (sinDoll > 0) Console.WriteLine("FALLO: INI-01 - alguna tarjeta de Inicio no tiene doll renderizado");
            if (insigniasIncoherentes > 0) Console.WriteLine("FALLO: INI-01 - alguna tarjeta lleva insignias contradictorias (Vanilla y tModLoader a la vez, o Calamity sin tModLoader)");
            if (!ordenadoPorFecha) Console.WriteLine("FALLO: INI-01 - el listado de Inicio no esta ordenado por fecha de modificacion descendente");

            // ---- INI-02: "Continuar con ..." y su aviso de fichero cambiado POR FUERA ----
            // Sin tocar el session.json real de la maquina (es global y lo comparten la app del
            // usuario y las demas ejecuciones del arnes - leccion ya documentada en CLAUDE.md):
            // se le da a Home la sesion a mano, que es exactamente lo que hace RestoreSession().
            string plrAviso = Path.Combine(dir, "AvisoDeFecha.plr");
            File.WriteAllBytes(plrAviso, PlrDePrueba("AvisoDeFecha"));
            var fechaReal = File.GetLastWriteTimeUtc(plrAviso);
            vm.Home.SetLastSession(new TerrakeepSession { LastCharacterPath = plrAviso, LastCharacterName = "AvisoDeFecha", LastCharacterModifiedUtc = fechaReal });
            bool ofreceContinuar = vm.Home.LastSessionCharacterName == "AvisoDeFecha";
            bool sinAvisoCuandoCuadra = vm.Home.LastSessionStalenessWarning == null;
            vm.Home.SetLastSession(new TerrakeepSession { LastCharacterPath = plrAviso, LastCharacterName = "AvisoDeFecha", LastCharacterModifiedUtc = fechaReal.AddMinutes(-7) });
            string? avisoEs = vm.Home.LastSessionStalenessWarning;
            Console.WriteLine($"INI-02-CONTINUAR: ofrece 'Continuar con AvisoDeFecha'={ofreceContinuar} (esperado True), sin aviso cuando la fecha cuadra={sinAvisoCuandoCuadra} (esperado True), " +
                              $"con aviso cuando el fichero cambio por fuera={avisoEs != null} (esperado True)");
            if (!ofreceContinuar) Console.WriteLine("FALLO: INI-02 - 'Continuar con...' no ofrece el personaje real de la sesion");
            if (!sinAvisoCuandoCuadra) Console.WriteLine("FALLO: INI-02 - avisa de 'el archivo cambio por fuera' cuando la fecha SI cuadra (aviso falso)");
            if (avisoEs == null) Console.WriteLine("FALLO: INI-02 - NO avisa de que el archivo cambio por fuera aunque la fecha real no coincide con la guardada");

            // El mismo aviso, con la app en ingles EN VIVO: es texto de interfaz, tiene que
            // cambiar como cualquier otro. Detector de la familia 1 (texto congelado).
            vm.Settings.Language = LocalizationService.English;
            DoEvents();
            string? avisoEn = vm.Home.LastSessionStalenessWarning;
            string esperadoEn = LocalizationService.Instance["home_stale_warning"];
            bool avisoTraducido = avisoEn == esperadoEn;
            Console.WriteLine($"INI-02-AVISO-IDIOMA: aviso con la app en ingles=\"{avisoEn ?? "(nada)"}\" (esperado \"{esperadoEn}\") -> {avisoTraducido}");
            if (!avisoTraducido) Console.WriteLine("FALLO: INI-02 - el aviso de 'el archivo cambio por fuera' se queda en el idioma que hubiera al calcularlo (no reacciona al cambio de idioma en vivo)");
            vm.Settings.Language = idiomaPrevio;
            DoEvents();

            // Un personaje borrado por fuera desde la ultima sesion no puede seguir ofreciendose
            // para continuar: seria un boton destacado que solo puede fallar.
            string plrBorrado = Path.Combine(dir, "YaNoEsta.plr");
            File.WriteAllBytes(plrBorrado, PlrDePrueba("YaNoEsta"));
            File.Delete(plrBorrado);
            vm.Home.SetLastSession(new TerrakeepSession { LastCharacterPath = plrBorrado, LastCharacterName = "YaNoEsta" });
            bool ocultaBorrado = vm.Home.LastSessionCharacterName == null;
            Console.WriteLine($"INI-02-BORRADO: 'Continuar con...' oculto cuando el .plr de la sesion ya no existe={ocultaBorrado} (esperado True)");
            if (!ocultaBorrado) Console.WriteLine("FALLO: INI-02 - se sigue ofreciendo continuar con un personaje cuyo fichero ya no existe");

            // ---- INI-03: las acciones REALES del menu contextual, sobre ficheros de verdad ----
            vm.Settings.AddCharacterFolder(dir);
            vm.Home.RefreshCommand.Execute(null);
            while (vm.Home.IsScanning) DoEvents();
            DoEvents();
            var entrada = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, plrAviso, StringComparison.OrdinalIgnoreCase));
            if (entrada == null)
            {
                Console.WriteLine("FALLO: INI-03 - la carpeta adicional recien añadida no hizo que Inicio encontrara su personaje (no se puede probar el menu contextual)");
            }
            else
            {
                // Duplicar: fichero nuevo real, nombre traducido, y NUNCA toca el original.
                vm.Home.DuplicateCommand.Execute(entrada);
                while (vm.Home.IsScanning) DoEvents();
                DoEvents();
                bool copiaEs = File.Exists(Path.Combine(dir, "AvisoDeFecha (copia).plr"));
                vm.Settings.Language = LocalizationService.English;
                DoEvents();
                vm.Home.DuplicateCommand.Execute(entrada);
                while (vm.Home.IsScanning) DoEvents();
                DoEvents();
                bool copiaEn = File.Exists(Path.Combine(dir, "AvisoDeFecha (copy).plr"));
                vm.Settings.Language = idiomaPrevio;
                DoEvents();
                bool originalIntacto = File.Exists(plrAviso) && PlrFile.Read(File.ReadAllBytes(plrAviso)).Name == "AvisoDeFecha";
                Console.WriteLine($"INI-03-DUPLICAR: '(copia).plr' creado en español={copiaEs} (esperado True), '(copy).plr' creado con la app en ingles={copiaEn} (esperado True), original intacto={originalIntacto} (esperado True)");
                if (!copiaEs || !copiaEn) Console.WriteLine("FALLO: INI-03 - 'Duplicar personaje' no creo el fichero esperado en algun idioma");
                if (!originalIntacto) Console.WriteLine("FALLO: INI-03 - 'Duplicar personaje' toco el fichero original");

                // Restaurar copia de seguridad, los DOS caminos reales: sin .bak (mensaje
                // honesto, nada que restaurar) y con .bak (el fichero real vuelve atras).
                vm.Home.ClearScanMessage();
                vm.Home.RestoreBackupCommand.Execute(entrada);
                DoEvents();
                string? msgSinBak = vm.Home.ActionErrorMessage;
                bool avisaSinBak = msgSinBak != null && msgSinBak.Contains("AvisoDeFecha", StringComparison.Ordinal);
                File.WriteAllBytes(plrAviso + ".bak", PlrDePrueba("VueltaDelBak"));
                vm.Home.RestoreBackupCommand.Execute(entrada);
                while (vm.Home.IsScanning) DoEvents();
                DoEvents();
                string nombreTrasRestaurar = PlrFile.Read(File.ReadAllBytes(plrAviso)).Name;
                Console.WriteLine($"INI-03-RESTAURAR: sin .bak avisa con el nombre real={avisaSinBak} (esperado True, mensaje=\"{msgSinBak}\"), con .bak el .plr real pasa a ser '{nombreTrasRestaurar}' (esperado 'VueltaDelBak')");
                if (!avisaSinBak) Console.WriteLine("FALLO: INI-03 - 'Restaurar copia de seguridad' sin ningun .bak no avisa de nada");
                if (nombreTrasRestaurar != "VueltaDelBak") Console.WriteLine("FALLO: INI-03 - 'Restaurar copia de seguridad' no restauro de verdad el .bak sobre el .plr");

                // Historial de guardados: un personaje que nunca se ha guardado con Terrakeep no
                // tiene ningun punto - la lista tiene que salir vacia, no reventar ni colarle los
                // puntos de OTRO personaje homonimo (ver el bloque de homonimos del arnes).
                var puntos = vm.Home.ListBackupPoints(entrada);
                Console.WriteLine($"INI-03-HISTORIAL: puntos de guardado de un personaje recien creado={puntos.Count} (esperado 0)");
                if (puntos.Count != 0) Console.WriteLine("FALLO: INI-03 - el 'Historial de guardados' de un personaje nuevo trae puntos que no son suyos");

                // ---- INI-04: el mensaje de Inicio tambien es texto de interfaz ----
                // Bug real del propio arnes en su primera pasada: `Restore` NO borra el .bak que
                // acaba de aplicar, asi que este segundo intento volvia a tener exito y dejaba el
                // mensaje vacio - la comprobacion de idioma comparaba "" contra "" y daba FALLO
                // sin que nada estuviera roto. Hay que quitarlo de verdad primero.
                File.Delete(plrAviso + ".bak");
                vm.Home.ClearScanMessage();
                vm.Home.RestoreBackupCommand.Execute(entrada); // ya no queda .bak: vuelve a poner el mensaje real
                DoEvents();
                string? msgEs = vm.Home.ActionErrorMessage;
                vm.Settings.Language = LocalizationService.English;
                DoEvents();
                string? msgEn = vm.Home.ActionErrorMessage;
                vm.Settings.Language = idiomaPrevio;
                DoEvents();
                bool mensajeTraducido = msgEs != null && msgEn != null && msgEn != msgEs;
                Console.WriteLine($"INI-04-MENSAJE-IDIOMA: mensaje de Inicio en español=\"{msgEs}\", con la app en ingles=\"{msgEn}\" -> cambia={mensajeTraducido} (esperado True)");
                if (!mensajeTraducido) Console.WriteLine("FALLO: INI-04 - el mensaje de Inicio se queda congelado en el idioma que hubiera al generarlo");
            }

            // ---- INI-05: la insignia de tModLoader y su tooltip ----
            // El caso NORMAL de un personaje de tModLoader es un .tplr SIN lista de mods (los
            // que escribe el propio Terrakeep no la traen) - justo el caso que el XAML tapaba
            // con un texto fijo en español via TargetNullValue, invisible para el barrido de
            // idioma porque un ToolTip no es un TextBlock de la ventana.
            string plrTmod = Path.Combine(dir, "ConTplr.plr");
            File.WriteAllBytes(plrTmod, PlrDePrueba("ConTplr"));
            File.WriteAllBytes(Path.ChangeExtension(plrTmod, ".tplr"), [0x1f, 0x8b, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
            vm.Home.RefreshCommand.Execute(null);
            while (vm.Home.IsScanning) DoEvents();
            DoEvents();
            var entradaTmod = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, plrTmod, StringComparison.OrdinalIgnoreCase));
            if (entradaTmod == null)
            {
                Console.WriteLine("FALLO: INI-05 - el personaje con .tplr hermano no aparece en Inicio");
            }
            else
            {
                string? ttEs = entradaTmod.UsedModsTooltip;
                vm.Settings.Language = LocalizationService.English;
                DoEvents();
                string? ttEn = entradaTmod.UsedModsTooltip;
                vm.Settings.Language = idiomaPrevio;
                DoEvents();
                bool esTModLoader = entradaTmod.IsTModLoader && !entradaTmod.IsVanilla;
                bool tooltipSiempre = !string.IsNullOrWhiteSpace(ttEs);
                bool tooltipTraducido = tooltipSiempre && ttEn != ttEs;
                Console.WriteLine($"INI-05-INSIGNIA-TMOD: insignia tModLoader={esTModLoader} (esperado True), tooltip en español=\"{ttEs ?? "(null)"}\", en ingles=\"{ttEn ?? "(null)"}\" -> " +
                                  $"siempre hay tooltip={tooltipSiempre} (esperado True), cambia de idioma={tooltipTraducido} (esperado True)");
                if (!esTModLoader) Console.WriteLine("FALLO: INI-05 - un .plr con .tplr hermano no queda marcado como de tModLoader");
                if (!tooltipSiempre) Console.WriteLine("FALLO: INI-05 - la insignia de tModLoader se queda sin tooltip propio cuando el .tplr no trae lista de mods (el XAML lo tapaba con texto fijo en español)");
                if (!tooltipTraducido) Console.WriteLine("FALLO: INI-05 - el tooltip de la insignia de tModLoader no cambia de idioma");
            }
        }
        catch (Exception ex) { Console.WriteLine("INI-EXCEPTION: " + ex); }
        finally
        {
            // Estado como estaba: la carpeta sintetica fuera de los ajustes REALES de este
            // usuario (settings.json es suyo, no del arnes), listado reescaneado y ficheros
            // temporales borrados. Misma disciplina que AR-13c/AR-15/AR-17.
            try
            {
                vm.Settings.RemoveCharacterFolderCommand.Execute(dir);
                vm.Home.RefreshCommand.Execute(null);
                while (vm.Home.IsScanning) DoEvents();
                vm.Home.ClearScanMessage();
                vm.Home.SetLastSession(new TerrakeepSession());
                vm.Settings.Language = idiomaPrevio;
                vm.SelectedTabIndex = tabPrevio; vm.PersonajeInnerTabIndex = innerPrevio;
                DoEvents();
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) { Console.WriteLine("INI-LIMPIEZA-EXCEPTION: " + ex.Message); }
        }
    }

    // ---- INI-06: las 5 tarjetas de "Que mas puedes hacer" llevan de verdad a donde dicen ----
    // MainViewModel.GoToTab decide la pestaña con un `switch` sobre una CADENA que viene del
    // CommandParameter del XAML, y su rama por defecto es `_ => Inicio`: una cadena mal escrita
    // (o una pestaña renombrada) no da ningun error - la tarjeta simplemente no lleva a ningun
    // sitio y el usuario se queda donde estaba. Se invoca cada boton REAL con su peer de
    // automatizacion (el mismo camino que un clic de verdad: ejecuta el Command con SU
    // CommandParameter, no uno escrito aqui a mano) y se mira donde aterriza.
    // ---- INI-07 / INI-08: los dos escenarios de riesgo del menu contextual de una tarjeta ----
    // INI-07: Inicio usa UN SOLO campo (ScanMessage) para dos cosas que no se parecen en nada -
    //   "no encontre ningun personaje" (invitacion a cargar uno a mano) y "esta accion que
    //   acabas de pedir ha fallado". El XAML lo pinta siempre igual: un boton grande con el
    //   titulo "Empezar: cargar un personaje" que abre el dialogo de fichero. O sea que un error
    //   al restaurar una copia se presenta como una invitacion a empezar de cero, CON la lista de
    //   personajes justo encima.
    // INI-08: "Restaurar copia de seguridad" copia el .bak encima del .plr sin mirar si el .bak
    //   se puede leer siquiera. Un .bak truncado (un guardado interrumpido, un disco lleno) se
    //   lleva por delante el personaje BUENO, sin vuelta atras y sin aviso: el personaje
    //   simplemente desaparece de la lista, porque el escaneo omite en silencio lo que no puede
    //   leer.
    private static void PruebasRestaurarCopia(MainViewModel vm, Window window)
    {
        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex;
        string dir = Path.Combine(Path.GetTempPath(), $"terrakeep-restaurar-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);
            string plr = Path.Combine(dir, "CopiaRota.plr");
            File.WriteAllBytes(plr, PlrDePrueba("CopiaRota"));
            vm.Settings.AddCharacterFolder(dir);
            vm.Home.RefreshCommand.Execute(null);
            while (vm.Home.IsScanning) DoEvents();
            DoEvents();
            var entrada = vm.Home.Characters.FirstOrDefault(c => string.Equals(c.FilePath, plr, StringComparison.OrdinalIgnoreCase));
            if (entrada == null) { Console.WriteLine("FALLO: INI-07/08 - el personaje de prueba no aparece en Inicio"); return; }

            // ---- INI-07 ----
            vm.SelectedTabIndex = 0;
            vm.Home.ClearScanMessage();
            vm.Home.RestoreBackupCommand.Execute(entrada); // no hay .bak: error real de una accion
            DoEvents(); DoEvents();
            int personajesALaVista = vm.Home.Characters.Count;
            var reclamo = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == LocalizationService.Instance["home_start_load_character"]);
            bool reclamoVisible = reclamo != null;
            bool mensajeALaVista = Descendientes<TextBlock>(window).Any(t => t.IsVisible && t.Text == vm.Home.ActionErrorMessage);
            Console.WriteLine($"INI-07-ERROR-DE-ACCION: con {personajesALaVista} personaje(s) en la lista y un error real de accion -> mensaje a la vista={mensajeALaVista} (esperado True), " +
                              $"reclamo \"{LocalizationService.Instance["home_start_load_character"]}\" visible={reclamoVisible} (esperado False: no es una invitacion a empezar de cero, es un error)");
            if (!mensajeALaVista) Console.WriteLine("FALLO: INI-07 - el error de la accion no llega a verse en Inicio");
            if (reclamoVisible && personajesALaVista > 0)
                Console.WriteLine("FALLO: INI-07 - un error de una accion se presenta como el reclamo de 'Empezar: cargar un personaje', con la lista de personajes justo encima");

            // ---- INI-08 ----
            long tamañoBueno = new FileInfo(plr).Length;
            File.WriteAllBytes(plr + ".bak", [0x01, 0x02, 0x03]); // .bak ilegible a proposito
            vm.Home.ClearScanMessage();
            vm.Home.RestoreBackupCommand.Execute(entrada);
            while (vm.Home.IsScanning) DoEvents();
            DoEvents();
            bool sigueLegible;
            try { sigueLegible = PlrFile.Read(File.ReadAllBytes(plr)).Name == "CopiaRota"; }
            catch (Exception) { sigueLegible = false; }
            bool sigueEnLaLista = vm.Home.Characters.Any(c => string.Equals(c.FilePath, plr, StringComparison.OrdinalIgnoreCase));
            bool aviso = !string.IsNullOrWhiteSpace(vm.Home.ActionErrorMessage);
            Console.WriteLine($"INI-08-COPIA-CORRUPTA: .plr bueno de {tamañoBueno} bytes + un .bak ilegible de 3 -> el .plr sigue legible={sigueLegible} (esperado True), " +
                              $"sigue en la lista de Inicio={sigueEnLaLista} (esperado True), avisa de algo=\"{vm.Home.ActionErrorMessage ?? "(nada)"}\" -> {aviso} (esperado True)");
            if (!sigueLegible) Console.WriteLine("FALLO: INI-08 - restaurar un .bak ILEGIBLE destruye el .plr bueno (perdida de datos real, sin vuelta atras)");
            if (!sigueEnLaLista) Console.WriteLine("FALLO: INI-08 - tras restaurar un .bak ilegible el personaje desaparece de Inicio");
            if (!aviso) Console.WriteLine("FALLO: INI-08 - restaurar un .bak ilegible no avisa de nada");
        }
        catch (Exception ex) { Console.WriteLine("INI-07/08-EXCEPTION: " + ex); }
        finally
        {
            try
            {
                vm.Settings.RemoveCharacterFolderCommand.Execute(dir);
                vm.Home.RefreshCommand.Execute(null);
                while (vm.Home.IsScanning) DoEvents();
                vm.Home.ClearScanMessage();
                vm.Settings.Language = idiomaPrevio;
                vm.SelectedTabIndex = tabPrevio;
                DoEvents();
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) { Console.WriteLine("INI-07/08-LIMPIEZA-EXCEPTION: " + ex.Message); }
        }
    }

    private static void PruebasTarjetasDeInicio(MainViewModel vm, Window window)
    {
        try
        {
            int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex;
            bool libreriaPlegadaPrevio = vm.IsLibraryCollapsed;
            vm.SelectedTabIndex = 0;
            DoEvents(); DoEvents();

            var loc = LocalizationService.Instance;
            // titulo real de la tarjeta -> (pestaña esperada, que mas tiene que pasar)
            (string titulo, int tabEsperada, string nota)[] esperado =
            [
                (loc["home_card_library_title"], 1, "Personaje > Objetos, con la Libreria DESPLEGADA (H4-03)"),
                (loc["home_card_builds_title"], 2, "Builds"),
                (loc["home_card_exploration_title"], 4, "Exploracion"),
                (loc["home_card_whatsnew_title"], 3, "Novedades"),
                (loc["home_card_about_title"], 5, "Acerca de"),
            ];
            int encontradas = 0, correctas = 0;
            foreach (var (titulo, tabEsperada, nota) in esperado)
            {
                vm.SelectedTabIndex = 0;
                if (tabEsperada == 1) vm.IsLibraryCollapsed = true; // el caso real de H4-03: plegada de partida
                DoEvents(); DoEvents();
                var boton = Descendientes<Button>(window).FirstOrDefault(b => b.IsVisible &&
                    Descendientes<TextBlock>(b).Any(t => t.Text == titulo));
                if (boton == null)
                {
                    Console.WriteLine($"FALLO: INI-06 - la tarjeta \"{titulo}\" no aparece en Inicio");
                    continue;
                }
                encontradas++;
                var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(boton);
                ((System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)!).Invoke();
                DoEvents(); DoEvents();
                bool ok = vm.SelectedTabIndex == tabEsperada
                          && (tabEsperada != 1 || (vm.PersonajeInnerTabIndex == 0 && !vm.IsLibraryCollapsed));
                if (ok) correctas++;
                Console.WriteLine($"INI-06-TARJETA: \"{titulo}\" -> pestaña {vm.SelectedTabIndex} (esperada {tabEsperada}, {nota})" +
                                  (tabEsperada == 1 ? $", sub-pestaña={vm.PersonajeInnerTabIndex} (esperada 0), Libreria plegada={vm.IsLibraryCollapsed} (esperado False)" : "") + $" -> {ok}");
                if (!ok) Console.WriteLine($"FALLO: INI-06 - la tarjeta \"{titulo}\" de Inicio no lleva a donde dice (GoToTab cae en su rama por defecto sin avisar de nada)");
            }
            Console.WriteLine($"INI-06-TARJETAS: {encontradas} de {esperado.Length} tarjetas encontradas, {correctas} llevan a la pestaña correcta (esperado {esperado.Length} y {esperado.Length})");
            if (encontradas != esperado.Length) Console.WriteLine("FALLO: INI-06 - falta alguna de las 5 tarjetas de 'Que mas puedes hacer' en Inicio");

            vm.IsLibraryCollapsed = libreriaPlegadaPrevio;
            vm.SelectedTabIndex = tabPrevio; vm.PersonajeInnerTabIndex = innerPrevio;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("INI-06-EXCEPTION: " + ex); }
    }

    private static void PruebasAjustes(MainViewModel vm, Window window)
    {
        // ---- AJU-01: el tick de "fijar tamaño de ventana" no puede dispararse SOLO ----
        // El CheckBox de Ajustes lleva IsChecked enlazado (OneWay) a Settings.IsWindowSizePinned
        // y reacciona con Checked/Unchecked. `Checked` NO significa "el usuario lo ha pulsado":
        // salta tambien cuando el binding cambia el valor, y eso pasa de verdad en CADA arranque
        // real de la app (SettingsViewModel.LoadFromDisk pone la propiedad a true al leer el
        // fichero, ver MainWindow.xaml.cs). El efecto es que la app re-fija el tamaño ella sola
        // con el que tenga la ventana en ese instante, pisando el que el usuario habia fijado.
        string placement = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "window.json");
        string? backup = File.Exists(placement) ? File.ReadAllText(placement) : null;
        int tabPrevio = vm.SelectedTabIndex;
        try
        {
            vm.SelectedTabIndex = 5; // Acerca de: es donde vive el CheckBox real
            DoEvents(); DoEvents();
            var tick = Descendientes<CheckBox>(window)
                .FirstOrDefault(c => c.IsVisible && c.Content as string == LocalizationService.Instance["settings_pin_window_label"]);
            if (tick == null)
            {
                Console.WriteLine("FALLO: AJU-01 - no se encontro el CheckBox real de 'fijar tamaño de ventana' en Ajustes");
            }
            else
            {
                // Punto de partida: destildado, la ventana a un tamaño cualquiera, y un tamaño
                // fijado ANTERIOR bien distinto escrito directamente en el fichero real (es lo
                // que habria dejado una sesion anterior del usuario).
                vm.Settings.IsWindowSizePinned = false;
                DoEvents();
                FijarTamaño(window, 1180, 860);
                File.WriteAllText(placement, System.Text.Json.JsonSerializer.Serialize(new WindowPlacementInfo
                {
                    Left = 10,
                    Top = 10,
                    Width = 1180,
                    Height = 860,
                    IsMaximized = false,
                    Pinned = true,
                    PinnedLeft = 60,
                    PinnedTop = 40,
                    PinnedWidth = 1444,
                    PinnedHeight = 902,
                }));

                // El gesto real de arrancar la app con el tick puesto: LoadFromDisk empuja la
                // propiedad a true y el binding marca el CheckBox, sin que nadie lo pulse.
                vm.Settings.IsWindowSizePinned = true;
                DoEvents(); DoEvents();
                var trasCargar = System.Text.Json.JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(placement))!;
                bool conservaFijado = Math.Abs(trasCargar.PinnedWidth - 1444) < 2 && Math.Abs(trasCargar.PinnedHeight - 902) < 2;
                Console.WriteLine($"AJU-01-TICK-AUTOMATICO: tamaño fijado tras marcar el tick DESDE EL CODIGO (lo que hace LoadFromDisk en cada arranque) = {trasCargar.PinnedWidth:0}x{trasCargar.PinnedHeight:0} " +
                                  $"(esperado 1444x902, el que el usuario habia fijado; la ventana mide ahora {window.ActualWidth:0}x{window.ActualHeight:0}) -> {conservaFijado}");
                if (!conservaFijado) Console.WriteLine("FALLO: AJU-01 - marcar el tick por binding (arranque normal de la app) RE-FIJA el tamaño con el de la ventana actual y pierde el que el usuario habia fijado");

                // Y el gesto real del USUARIO tiene que seguir funcionando: un clic de verdad
                // sobre el mismo CheckBox fija el tamaño de AHORA.
                vm.Settings.IsWindowSizePinned = false;
                WindowPlacementService.Unpin();
                DoEvents();
                tick.IsChecked = true; // como el clic real: primero cambia el estado del control...
                tick.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); // ...y luego avisa
                DoEvents();
                var trasClic = System.Text.Json.JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(placement))!;
                bool fijoElActual = trasClic.Pinned && Math.Abs(trasClic.PinnedWidth - window.ActualWidth) < 3;
                Console.WriteLine($"AJU-01-TICK-USUARIO: tras un clic real en el tick -> Pinned={trasClic.Pinned} (esperado True), tamaño fijado={trasClic.PinnedWidth:0}x{trasClic.PinnedHeight:0} " +
                                  $"(esperado el de la ventana, {window.ActualWidth:0}x{window.ActualHeight:0}) -> {fijoElActual}");
                if (!fijoElActual) Console.WriteLine("FALLO: AJU-01 - un clic real del usuario en el tick ya no fija el tamaño actual de la ventana");

                // Y desmarcarlo lo tiene que soltar.
                tick.IsChecked = false;
                tick.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DoEvents();
                bool soltado = !WindowPlacementService.IsPinned();
                Console.WriteLine($"AJU-01-TICK-USUARIO: tras desmarcarlo con otro clic real -> IsPinned()={!soltado} (esperado False) -> {soltado}");
                if (!soltado) Console.WriteLine("FALLO: AJU-01 - desmarcar el tick no suelta el tamaño fijado");

                vm.Settings.IsWindowSizePinned = false;
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("AJU-01-EXCEPTION: " + ex); }
        finally
        {
            if (backup != null) File.WriteAllText(placement, backup);
            else if (File.Exists(placement)) File.Delete(placement);
        }

        // ---- AJU-02: el numero de copias de seguridad, escrito a mano en el TextBox real ----
        try
        {
            vm.SelectedTabIndex = 5;
            DoEvents(); DoEvents();
            int cupoPrevio = vm.Settings.BackupHistoryCap;
            var caja = Descendientes<TextBox>(window).FirstOrDefault(t => t.IsVisible &&
                System.Windows.Data.BindingOperations.GetBinding(t, TextBox.TextProperty)?.Path.Path == "Settings.BackupHistoryCap");
            if (caja == null)
            {
                Console.WriteLine("FALLO: AJU-02 - no se encontro el TextBox real del cupo de copias de seguridad en Ajustes");
            }
            else
            {
                caja.Text = "7";
                DoEvents();
                int trasSiete = vm.Settings.BackupHistoryCap;
                caja.Text = "0"; // absurdo a proposito: sin suelo, el siguiente guardado purgaria el historial ENTERO
                DoEvents();
                int trasCero = vm.Settings.BackupHistoryCap;
                caja.Text = "abc"; // no es un numero: no puede tumbar nada ni dejar el cupo por debajo del suelo
                DoEvents();
                int trasBasura = vm.Settings.BackupHistoryCap;
                Console.WriteLine($"AJU-02-CUPO: escribiendo '7' -> {trasSiete} (esperado 7), '0' -> {trasCero} (esperado 1, el suelo real), 'abc' -> {trasBasura} (esperado >= 1, sin excepcion)");
                if (trasSiete != 7) Console.WriteLine("FALLO: AJU-02 - el cupo escrito a mano en Ajustes no llega al ViewModel");
                if (trasCero < 1) Console.WriteLine("FALLO: AJU-02 - un cupo de 0 copias se acepta tal cual (el siguiente guardado purgaria el historial entero)");
                if (trasBasura < 1) Console.WriteLine("FALLO: AJU-02 - texto no numerico en el cupo deja el valor por debajo del suelo real");
                caja.Text = cupoPrevio.ToString();
                DoEvents();
                Console.WriteLine($"AJU-02-CUPO-LIMPIEZA: cupo real del usuario restaurado a {vm.Settings.BackupHistoryCap} (esperado {cupoPrevio})");
            }
        }
        catch (Exception ex) { Console.WriteLine("AJU-02-EXCEPTION: " + ex); }
        finally
        {
            vm.SelectedTabIndex = tabPrevio;
            DoEvents();
        }
    }

    // ---- AJU-03: los ajustes que NO tienen control propio en la pantalla de Ajustes ----
    // El ancho de la barra lateral de Exploracion y la visibilidad del minimapa se cambian desde
    // Exploracion (arrastrando el GridSplitter, pulsando el boton de plegar) pero VIVEN aqui:
    // son parte de settings.json y de SettingsViewModel, y su unica red real son los clamps de
    // esta clase. Sin ellos, un settings.json con un ancho absurdo (editado a mano, o heredado de
    // una version anterior) deja la barra en una zona intermedia inutil, y un 0 - que SI es un
    // valor valido, "plegada" - no debe confundirse con "demasiado estrecha".
    //
    // Todo esto escribe en el settings.json REAL de esta maquina, asi que se respalda como TEXTO
    // y se restaura byte a byte al final (mismo criterio que A9-11/A9-12 con el mundo y con
    // window.json).
    private static void PruebasAjustesPersistidos(MainViewModel vm)
    {
        string ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "settings.json");
        string? backup = File.Exists(ruta) ? File.ReadAllText(ruta) : null;
        try
        {
            double anchoPrevio = vm.Settings.ExplorationSidebarWidth;
            bool minimapaPrevio = vm.Settings.IsMinimapVisible;

            vm.Settings.ExplorationSidebarWidth = 0;      // plegada: valor real y valido
            double trasCero = vm.Settings.ExplorationSidebarWidth;
            vm.Settings.ExplorationSidebarWidth = 120;    // demasiado estrecha para ser util
            double trasEstrecha = vm.Settings.ExplorationSidebarWidth;
            vm.Settings.ExplorationSidebarWidth = 900;    // mas de media pantalla
            double trasAncha = vm.Settings.ExplorationSidebarWidth;
            vm.Settings.ExplorationSidebarWidth = 340;    // un valor normal se respeta tal cual
            double trasNormal = vm.Settings.ExplorationSidebarWidth;
            Console.WriteLine($"AJU-03-BARRA: 0 -> {trasCero:0} (esperado 0, plegada), 120 -> {trasEstrecha:0} (esperado 260, el suelo), 900 -> {trasAncha:0} (esperado 520, el techo), 340 -> {trasNormal:0} (esperado 340)");
            if (trasCero != 0) Console.WriteLine("FALLO: AJU-03 - plegar la barra lateral (0) se recorta a un valor intermedio, o sea ya no se puede plegar");
            if (trasEstrecha != 260) Console.WriteLine("FALLO: AJU-03 - un ancho por debajo del minimo real no se recorta al minimo");
            if (trasAncha != 520) Console.WriteLine("FALLO: AJU-03 - un ancho por encima del maximo real no se recorta al maximo");
            if (Math.Abs(trasNormal - 340) > 0.5) Console.WriteLine("FALLO: AJU-03 - un ancho normal no se respeta tal cual");

            // Persistencia REAL: lo que se cambia aqui tiene que estar en el fichero, no solo en
            // memoria - es lo unico que hace que sobreviva a cerrar la app.
            vm.Settings.IsMinimapVisible = !minimapaPrevio;
            var enDisco = System.Text.Json.JsonSerializer.Deserialize<TerrakeepSettings>(File.ReadAllText(ruta))!;
            bool guardoAncho = Math.Abs(enDisco.ExplorationSidebarWidth - 340) < 0.5;
            bool guardoMinimapa = enDisco.IsMinimapVisible == !minimapaPrevio;
            Console.WriteLine($"AJU-03-PERSISTE: settings.json real -> ancho de la barra={enDisco.ExplorationSidebarWidth:0} (esperado 340) -> {guardoAncho}, minimapa visible={enDisco.IsMinimapVisible} (esperado {!minimapaPrevio}) -> {guardoMinimapa}");
            if (!guardoAncho || !guardoMinimapa) Console.WriteLine("FALLO: AJU-03 - un ajuste cambiado no llega al settings.json real (se perderia al cerrar la app)");

            // Carpetas adicionales de MUNDOS: la mitad que nunca se probaba de extremo a extremo
            // (H5-07 solo cubre las de personajes). Lo que de verdad importa es que la carpeta
            // llegue a CharacterFileService, que es quien la usa para buscar mundos.
            string dirMundos = Path.Combine(Path.GetTempPath(), $"terrakeep-mundos-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dirMundos);
            try
            {
                vm.Settings.AddWorldFolder(dirMundos);
                bool enServicio = CharacterFileService.ExtraWorldFolders.Contains(dirMundos, StringComparer.OrdinalIgnoreCase);
                bool enBusqueda = CharacterFileService.GetAllWorldsDirectories().Contains(dirMundos, StringComparer.OrdinalIgnoreCase);
                vm.Settings.AddWorldFolder(dirMundos); // repetida a proposito: no puede duplicarse
                int vecesEnLaLista = vm.Settings.ExtraWorldFolders.Count(f => string.Equals(f, dirMundos, StringComparison.OrdinalIgnoreCase));
                vm.Settings.RemoveWorldFolderCommand.Execute(dirMundos);
                bool fueraTrasQuitar = !CharacterFileService.GetAllWorldsDirectories().Contains(dirMundos, StringComparer.OrdinalIgnoreCase);
                Console.WriteLine($"AJU-03-MUNDOS: carpeta adicional -> llega a CharacterFileService={enServicio} (esperado True), entra en la busqueda real de mundos={enBusqueda} (esperado True), " +
                                  $"veces en la lista tras añadirla dos veces={vecesEnLaLista} (esperado 1), fuera tras quitarla={fueraTrasQuitar} (esperado True)");
                if (!enServicio || !enBusqueda) Console.WriteLine("FALLO: AJU-03 - una carpeta adicional de mundos no llega a la busqueda real de mundos");
                if (vecesEnLaLista != 1) Console.WriteLine("FALLO: AJU-03 - añadir dos veces la misma carpeta de mundos la duplica en la lista");
                if (!fueraTrasQuitar) Console.WriteLine("FALLO: AJU-03 - quitar una carpeta de mundos no la saca de la busqueda real");
            }
            finally
            {
                try { Directory.Delete(dirMundos, recursive: true); } catch (Exception) { }
            }

            vm.Settings.ExplorationSidebarWidth = anchoPrevio;
            vm.Settings.IsMinimapVisible = minimapaPrevio;
        }
        catch (Exception ex) { Console.WriteLine("AJU-03-EXCEPTION: " + ex); }
        finally
        {
            // El settings.json de este usuario vuelve tal y como estaba, byte a byte.
            if (backup != null) File.WriteAllText(ruta, backup);
            else if (File.Exists(ruta)) File.Delete(ruta);
            vm.Settings.LoadFromDisk(); // y el ViewModel deja de reflejar los valores de prueba
        }
    }

    // ---- AJU-04: el selector de idioma REAL, con los dos chips de la pantalla ----
    // A9-13-IDIOMA cambia el idioma asignando la propiedad del ViewModel; aqui se pulsan los dos
    // RadioButton de verdad (el camino del usuario) y se comprueba que la interfaz que ya esta
    // en pantalla se reescribe sola, en los dos sentidos.
    private static void PruebasSelectorDeIdioma(MainViewModel vm, Window window)
    {
        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex;
        try
        {
            vm.SelectedTabIndex = 5;
            DoEvents(); DoEvents();
            var chips = Descendientes<RadioButton>(window).Where(r => r.IsVisible && r.GroupName == "Idioma").ToList();
            if (chips.Count != 2)
            {
                Console.WriteLine($"FALLO: AJU-04 - se esperaban 2 chips de idioma en Ajustes y se encontraron {chips.Count}");
                return;
            }
            // El titulo de la propia seccion es texto de interfaz: sirve de testigo de que lo que
            // YA esta pintado se reescribe (no solo lo que se vuelva a crear despues).
            var testigo = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == LocalizationService.Instance["settings_language_title"]);
            foreach (var (destino, etiqueta) in new[] { (LocalizationService.English, "settings_language_english"), (LocalizationService.Spanish, "settings_language_spanish") })
            {
                var chip = chips.FirstOrDefault(r => r.Content as string == LocalizationService.Instance[etiqueta])
                           ?? chips[destino == LocalizationService.English ? 1 : 0];
                chip.IsChecked = true;
                DoEvents(); DoEvents();
                bool cambio = vm.Settings.Language == destino && LocalizationService.Instance.Language == destino;
                string esperadoTitulo = LocalizationService.Instance["settings_language_title"];
                bool testigoReescrito = testigo == null || testigo.Text == esperadoTitulo;
                Console.WriteLine($"AJU-04-IDIOMA: pulsado el chip de '{destino}' -> Settings.Language={vm.Settings.Language}, LocalizationService={LocalizationService.Instance.Language} (esperado {destino} los dos) -> {cambio}; " +
                                  $"el titulo YA pintado dice \"{testigo?.Text ?? "(no encontrado)"}\" (esperado \"{esperadoTitulo}\") -> {testigoReescrito}");
                if (!cambio) Console.WriteLine($"FALLO: AJU-04 - pulsar el chip de idioma '{destino}' no cambia el idioma real de la app");
                if (!testigoReescrito) Console.WriteLine("FALLO: AJU-04 - el texto que ya estaba en pantalla no se reescribe al cambiar de idioma con el chip");
            }
        }
        catch (Exception ex) { Console.WriteLine("AJU-04-EXCEPTION: " + ex); }
        finally
        {
            vm.Settings.Language = idiomaPrevio;
            vm.SelectedTabIndex = tabPrevio;
            DoEvents();
        }
    }

    private static void PruebasNovedades(MainViewModel vm)
    {
        // ---- NOV-01: contenido real de las dos pestañas y su traduccion ----
        try
        {
            var vanilla = vm.WhatsNew.VanillaEntries;
            var calamity = vm.WhatsNew.CalamityEntries;
            int itemsVanilla = vanilla.Sum(e => e.Items.Count), conIconoVanilla = vanilla.Sum(e => e.Items.Count(i => i.IconPath != null));
            int itemsCalamity = calamity.Sum(e => e.Items.Count), conIconoCalamity = calamity.Sum(e => e.Items.Count(i => i.IconPath != null));
            var todas = vanilla.Concat(calamity).ToList();
            int sinVersion = todas.Count(e => string.IsNullOrWhiteSpace(e.Version));
            int sinFecha = todas.Count(e => string.IsNullOrWhiteSpace(e.DisplayDate));
            int vacias = todas.Count(e => e.Changes.Count == 0 && e.Bugfixes.Count == 0 && e.Items.Count == 0);
            Console.WriteLine($"NOV-01-CONTENIDO: Terraria={vanilla.Count} version(es) con {itemsVanilla} objeto(s) ({conIconoVanilla} con sprite real), " +
                              $"tModLoader/Calamity={calamity.Count} version(es) con {itemsCalamity} objeto(s) ({conIconoCalamity} con sprite real); " +
                              $"entradas sin numero de version={sinVersion} (esperado 0), sin fecha={sinFecha} (esperado 0), sin nada que enseñar={vacias} (esperado 0)");
            if (vanilla.Count == 0 || calamity.Count == 0) Console.WriteLine("FALLO: NOV-01 - alguna de las dos pestañas de Novedades no tiene ni una version que mostrar");
            if (sinVersion > 0 || sinFecha > 0) Console.WriteLine("FALLO: NOV-01 - hay entradas de Novedades sin numero de version o sin fecha");
            if (vacias > 0) Console.WriteLine("FALLO: NOV-01 - hay entradas de Novedades sin ningun cambio, arreglo ni objeto que enseñar");
            if (conIconoVanilla == 0) Console.WriteLine("FALLO: NOV-01 - ningun objeto de las Novedades de Terraria resuelve su sprite real");
            if (conIconoCalamity == 0) Console.WriteLine("FALLO: NOV-01 - ningun objeto de las Novedades de Calamity resuelve su sprite real");

            // La traduccion del CONTENIDO (no de la interfaz): el texto de cada linea vive en el
            // propio .json con un campo por idioma, y tiene que cambiar de verdad al vuelo.
            string idiomaPrevio = vm.Settings.Language;
            vm.Settings.Language = LocalizationService.Spanish; DoEvents();
            var lineasEs = todas.SelectMany(e => e.Changes.Select(c => c.Text).Concat(e.Bugfixes.Select(b => b.Text)).Append(e.DisplayNote))
                                .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            var fechasEs = todas.Select(e => e.DisplayDate).ToList();
            vm.Settings.Language = LocalizationService.English; DoEvents();
            var lineasEn = todas.SelectMany(e => e.Changes.Select(c => c.Text).Concat(e.Bugfixes.Select(b => b.Text)).Append(e.DisplayNote))
                                .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            var fechasEn = todas.Select(e => e.DisplayDate).ToList();
            vm.Settings.Language = idiomaPrevio; DoEvents();
            int lineasIguales = lineasEs.Zip(lineasEn).Count(p => p.First == p.Second);
            int fechasIguales = fechasEs.Zip(fechasEn).Count(p => p.First == p.Second);
            Console.WriteLine($"NOV-01-IDIOMA: {lineasEs.Count} linea(s) de texto, identicas en los dos idiomas={lineasIguales} (esperado muy pocas); " +
                              $"{fechasEs.Count} fecha(s), identicas={fechasIguales} (informativo: una fecha puede coincidir)");
            if (lineasEs.Count == 0) Console.WriteLine("FALLO: NOV-01 - Novedades no tiene ni una linea de texto que comprobar");
            else if (lineasIguales * 2 > lineasEs.Count)
                Console.WriteLine($"FALLO: NOV-01 - {lineasIguales} de {lineasEs.Count} lineas de Novedades siguen identicas con la app en ingles (contenido sin traducir)");
            foreach (var (es, en) in lineasEs.Zip(lineasEn).Where(p => p.First == p.Second).Take(8))
                Console.WriteLine($"   NOV-01-SIN-TRADUCIR: \"{(es!.Length > 90 ? es[..90] + "..." : es)}\"");
        }
        catch (Exception ex) { Console.WriteLine("NOV-01-EXCEPTION: " + ex); }
    }

    private static void PruebasAcercaDe(MainViewModel vm, Window window)
    {
        // ---- ACE-01: autoria literal, version real y ninguna clave sin resolver ----
        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex;
        try
        {
            vm.SelectedTabIndex = 5;
            foreach (string idioma in new[] { LocalizationService.Spanish, LocalizationService.English })
            {
                vm.Settings.Language = idioma;
                DoEvents(); DoEvents();
                var bloques = Descendientes<TextBlock>(window).Where(t => t.IsVisible).ToList();
                var textos = bloques.Where(t => !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Text!);
                var runs = bloques.SelectMany(t => t.Inlines.OfType<Run>()).Select(r => r.Text ?? "");
                string todo = string.Join(" | ", textos.Concat(runs));
                // C-18: el nombre va LITERAL, con I y B mayusculas y el resto minusculas - no se
                // "corrige" ni se traduce en ningun idioma, ni aqui ni en el XAML ni en el .csproj.
                bool literal = todo.Contains("IncrediBad", StringComparison.Ordinal);
                var deformado = System.Text.RegularExpressions.Regex.Match(todo, @"\b(Incredibad|INCREDIBAD|incredibad|IncredIbad|IncrediBAD)\b");
                bool versionVisible = todo.Contains(vm.About.Version, StringComparison.Ordinal);
                var claves = System.Text.RegularExpressions.Regex.Matches(todo, @"\[[a-z0-9_]{4,}\]").Select(m => m.Value).Distinct().ToList();
                Console.WriteLine($"ACE-01 [{idioma}]: 'IncrediBad' literal a la vista={literal} (esperado True), variante deformada={(deformado.Success ? "'" + deformado.Value + "'" : "ninguna")} (esperado ninguna), " +
                                  $"version '{vm.About.Version}' a la vista={versionVisible} (esperado True), claves sin resolver={claves.Count} (esperado 0{(claves.Count > 0 ? ": " + string.Join(",", claves) : "")})");
                if (!literal) Console.WriteLine($"FALLO: ACE-01 - el nombre del autor 'IncrediBad' no aparece literal en 'Acerca de' con la app en {idioma}");
                if (deformado.Success) Console.WriteLine($"FALLO: ACE-01 - el nombre del autor aparece deformado ('{deformado.Value}') en 'Acerca de' con la app en {idioma}");
                if (!versionVisible) Console.WriteLine($"FALLO: ACE-01 - la version real de la app no se ve en 'Acerca de' con la app en {idioma}");
                if (claves.Count > 0) Console.WriteLine($"FALLO: ACE-01 - hay claves de idioma sin resolver a la vista en 'Acerca de' ({string.Join(",", claves)})");
            }

            // ---- ACE-02: el registro de cambios del propio editor, dentro de "Acerca de" ----
            // No se toca su CONTENIDO aqui (lo escribe el usuario al cerrar una version), pero si
            // que este completo y traducido: una entrada sin fecha/resumen, o con la lista inglesa
            // de distinta longitud que la española, deja huecos mudos en la pestaña.
            var registro = vm.Changelog.Entries;
            vm.Settings.Language = LocalizationService.Spanish; DoEvents();
            var resumenEs = registro.Select(e => e.Summary).ToList();
            vm.Settings.Language = LocalizationService.English; DoEvents();
            var resumenEn = registro.Select(e => e.Summary).ToList();
            vm.Settings.Language = LocalizationService.Spanish; DoEvents();
            int sinVersion = registro.Count(e => string.IsNullOrWhiteSpace(e.Version));
            int sinFecha = registro.Count(e => string.IsNullOrWhiteSpace(e.Date));
            int sinResumen = registro.Count(e => string.IsNullOrWhiteSpace(e.Summary));
            int vacias = registro.Count(e => e.Added.Count == 0 && e.Fixed.Count == 0);
            int resumenIgual = resumenEs.Zip(resumenEn).Count(p => p.First == p.Second);
            Console.WriteLine($"ACE-02-REGISTRO: {registro.Count} version(es) del editor -> sin numero={sinVersion} (esperado 0), sin fecha={sinFecha} (esperado 0), sin resumen={sinResumen} (esperado 0), " +
                              $"sin nada añadido ni arreglado={vacias} (esperado 0), resumenes identicos en los dos idiomas={resumenIgual} (esperado 0)");
            if (registro.Count == 0) Console.WriteLine("FALLO: ACE-02 - el registro de cambios del editor esta vacio en 'Acerca de'");
            if (sinVersion > 0 || sinFecha > 0 || sinResumen > 0) Console.WriteLine("FALLO: ACE-02 - hay versiones del registro sin numero, sin fecha o sin resumen");
            if (vacias > 0) Console.WriteLine("FALLO: ACE-02 - hay versiones del registro sin ningun cambio ni arreglo que enseñar");
            if (resumenIgual > 0) Console.WriteLine($"FALLO: ACE-02 - {resumenIgual} resumen(es) del registro siguen identicos con la app en ingles (sin traducir)");

            // El propio dato, no solo lo pintado: AboutViewModel es la fuente unica del nombre.
            Console.WriteLine($"ACE-01-DATO: About.AuthorName='{vm.About.AuthorName}' (esperado exactamente 'IncrediBad'), About.AppName='{vm.About.AppName}' (esperado 'Terrakeep'), About.Version='{vm.About.Version}'");
            if (vm.About.AuthorName != "IncrediBad") Console.WriteLine("FALLO: ACE-01 - AboutViewModel.AuthorName ya no es exactamente 'IncrediBad'");
            if (vm.About.AppName != "Terrakeep") Console.WriteLine("FALLO: ACE-01 - AboutViewModel.AppName ya no es 'Terrakeep'");
        }
        catch (Exception ex) { Console.WriteLine("ACE-01-EXCEPTION: " + ex); }
        finally
        {
            vm.Settings.Language = idiomaPrevio;
            vm.SelectedTabIndex = tabPrevio;
            DoEvents();
        }
    }
}
