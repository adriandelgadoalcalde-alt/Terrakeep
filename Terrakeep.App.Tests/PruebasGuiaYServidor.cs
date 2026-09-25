using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

            vm.SelectedTabIndex = 3; // AppTab.Guia, reordenado T1 21-sep-2026
            DoEvents();
            vm.Guide.Refresh();
            DoEvents();

            // Encargo3 (25-sep-2026): estado ORGANICO real del banner ANTES de tocar nada -
            // el objetivo real de este personaje/mundo (mas abajo: "Cinco cristales de vida") es un
            // requisito CristalesVida, un tipo que ResolverIconoDelHito no resuelve a proposito (no
            // es Jefe/Objeto/Npc, no hay nada real que enseñar) - asi que esta captura es la
            // evidencia real del caso "sin icono" (Visibility Collapsed de la pastilla, sin hueco
            // roto ni desplazamiento del texto). Se guarda ANTES de expandir tramos/forzar el
            // objetivo mas abajo para que quede intacta, sin el scroll de las comprobaciones
            // siguientes.
            try
            {
                CapturaVentanaKeepQa(window, "guia-banner-sin-icono");
                Console.WriteLine("GUIA_SOLO ICONOS: captura real del banner SIN icono (estado organico) -> keepqa-evidencia\\guia-banner-sin-icono.png");
            }
            catch (Exception ex) { Console.WriteLine("GUIA_SOLO ICONOS: captura del banner sin icono fallo - " + ex.Message); }

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

            // Encargo3 (25-sep-2026, revision-correccion-integral-familia-Keep, handoff
            // e5eaea9e-c261-4199-8e7d-060b6054f58d): hueco de cobertura real cerrado aqui mismo -
            // antes de esto NINGUN canario comprobaba que GuidePasoViewModel.IconPath resuelve un
            // sprite real (ni que se pinta sin recorte/overflow), asi que un fallback roto o un
            // <Image> mal atado en el XAML habria pasado en silencio. Tres filas reales elegidas a
            // mano de guia_progresion.json (ids confirmados por lectura directa del catalogo, no
            // inventados): "Cuatro vecinos" (PreOjo/PuebloDeCuatro, TipoRequisitoGuia.Npc id=17
            // Merchant, con icono real en Assets/npc_icons), "Un arma que aguante una oleada
            // entera" (EjercitoGoblin/ArmaParaElEjercitoGoblin, Objeto id=361, Assets/vanilla/
            // icons) y "Cualquier arma con algo de alcance" (DesertScourge/ArmaParaDesertScourge,
            // Objeto Calamity via idMod CalamityMod/DesertMedallion, Assets/calamity/icons) - las
            // TRES categorias reales que cubre ResolverIconoDelHito salvo "jefe con sprite de NPC
            // de pueblo": confirmado por lectura directa de los ~20 ids de jefe del catalogo contra
            // los 40 ficheros reales de Assets/npc_icons que NINGUNO coincide (la inmensa mayoria
            // de jefes no son NPC de pueblo, tal cual documenta el propio comentario de
            // ResolverIconoDelHito) - no hay ningun ejemplo real de ese caso en el catalogo actual,
            // asi que no se fuerza uno inventado.
            static IEnumerable<FrameworkElement> Descendientes(DependencyObject raiz)
            {
                int n = VisualTreeHelper.GetChildrenCount(raiz);
                for (int i = 0; i < n; i++)
                {
                    var hijo = VisualTreeHelper.GetChild(raiz, i);
                    if (hijo is FrameworkElement fe) yield return fe;
                    foreach (var nieto in Descendientes(hijo)) yield return nieto;
                }
            }

            (GuideTramoViewModel tramo, GuidePasoViewModel paso)? EncontrarPaso(string nombreTramo, string tituloPaso)
            {
                var t = vm.Guide.Tramos.FirstOrDefault(x => x.Nombre == nombreTramo);
                var p = t?.Pasos.FirstOrDefault(x => x.Titulo == tituloPaso);
                return t != null && p != null ? (t, p) : null;
            }

            var objetivos = new (string tramo, string paso, string categoria)[]
            {
                ("Antes del primer jefe", "Cuatro vecinos", "Npc (vecino real, id 17 Merchant)"),
                ("El Ejército Goblin (opcional)", "Un arma que aguante una oleada entera", "Objeto vanilla (id 361)"),
                ("El Desert Scourge (opcional, Calamity)", "Cualquier arma con algo de alcance", "Objeto Calamity (idMod DesertMedallion)"),
            };

            int filasConIconoVisible = 0;
            foreach (var (nombreTramo, tituloPaso, categoria) in objetivos)
            {
                var encontrado = EncontrarPaso(nombreTramo, tituloPaso);
                if (encontrado == null)
                {
                    Console.WriteLine($"GUIA_SOLO ICONOS: FALLO - no se encontro el paso real '{tituloPaso}' en el tramo '{nombreTramo}'.");
                    continue;
                }
                var (tramoVm, pasoVm) = encontrado.Value;
                Console.WriteLine($"GUIA_SOLO ICONOS: [{categoria}] paso='{tituloPaso}' -> IconPath={pasoVm.IconPath ?? "(null)"}");
                if (pasoVm.IconPath == null)
                {
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - '{tituloPaso}' deberia resolver un icono real ({categoria}) y salio null.");
                    continue;
                }
                // "pack://siteoforigin:,,,/Assets/..." no es una ruta de disco directa (new
                // Uri(...).LocalPath NO la resuelve, es un esquema propio de WPF) - se reconstruye
                // la ruta real igual que hacen los propios resolvers (AppContext.BaseDirectory +
                // todo lo que va despues de ",,,/"), para comprobar el fichero real en disco.
                string rutaReal = Path.Combine(AppContext.BaseDirectory,
                    pasoVm.IconPath[(pasoVm.IconPath.IndexOf(",,,/", StringComparison.Ordinal) + 4)..].Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(rutaReal))
                {
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - IconPath de '{tituloPaso}' apunta a un fichero que no existe: {pasoVm.IconPath}");
                    continue;
                }

                // Expande el Expander real de este tramo (mismo control que el usuario pulsaria a
                // mano) - IsExpanded no esta bindeado en el XAML (arranca en False a proposito para
                // no abrir 46 tramos de golpe), asi que se localiza el control real por DataContext
                // y se activa igual que un clic real lo haria.
                var expander = Descendientes(window).OfType<Expander>().FirstOrDefault(e => ReferenceEquals(e.DataContext, tramoVm));
                if (expander == null)
                {
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - no se encontro el Expander real del tramo '{nombreTramo}' en el arbol visual.");
                    continue;
                }
                expander.IsExpanded = true;
                DoEvents(); DoEvents();

                var imagenFila = Descendientes(window).OfType<Image>().FirstOrDefault(im => ReferenceEquals(im.DataContext, pasoVm));
                if (imagenFila == null)
                {
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - el Expander de '{nombreTramo}' se abrio pero no aparecio ningun <Image> real con DataContext=paso '{tituloPaso}'.");
                    continue;
                }
                // La pestaña Guia entera vive dentro de un ScrollViewer (46 tramos reales no caben
                // en pantalla) - sin desplazar la fila a la vista, ZonaVisible mediria un recorte
                // real del propio scroll (el viewport SI recorta lo que esta fuera, eso es
                // correcto) y lo confundiria con un bug. BringIntoView() es el mismo mecanismo real
                // que usa el teclado/Ctrl+F de la propia app para llevar algo a la vista.
                imagenFila.BringIntoView();
                DoEvents(); DoEvents();

                var rectCompleto = RectCompleto(imagenFila, window);
                var rectVisible = ZonaVisible(imagenFila, window);
                // Misma formula EJE A EJE que ya usa AuditoriaMaquetacion.cs (D1, "contenido
                // perdido") - ZonaVisible devuelve la zona de clip completa de los ancestros
                // (p.ej. el viewport entero del ScrollViewer), NO la interseccion con el propio
                // elemento - comparar Width/Height en crudo contra esa zona (primer intento real de
                // esta ronda) daba un falso recorte SIEMPRE, para cualquier icono de 20x20 dentro de
                // un viewport de 1045x732. Lo correcto es cuanto de la caja completa cae FUERA de la
                // zona pintada, eje a eje.
                double faltaX = rectVisible.IsEmpty ? rectCompleto.Width
                    : Math.Min(rectCompleto.Width, Math.Max(0, rectVisible.Left - rectCompleto.Left) + Math.Max(0, rectCompleto.Right - rectVisible.Right));
                double faltaY = rectVisible.IsEmpty ? rectCompleto.Height
                    : Math.Min(rectCompleto.Height, Math.Max(0, rectVisible.Top - rectCompleto.Top) + Math.Max(0, rectCompleto.Bottom - rectVisible.Bottom));
                bool sinRecorte = faltaX <= 0.5 && faltaY <= 0.5;
                bool dentroDeLaVentana = rectCompleto.Right <= window.ActualWidth + 0.5 && rectCompleto.Bottom <= window.ActualHeight + 0.5
                    && rectCompleto.Left >= -0.5 && rectCompleto.Top >= -0.5;
                Console.WriteLine($"GUIA_SOLO ICONOS: fila '{tituloPaso}' -> rect real={rectCompleto} visible={rectVisible} sinRecorte={sinRecorte} dentroDeLaVentana={dentroDeLaVentana} (esperado True, True)");
                if (!sinRecorte)
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - el icono de '{tituloPaso}' esta RECORTADO (QuienRecorta={QuienRecorta(imagenFila, window)}).");
                if (!dentroDeLaVentana)
                    Console.WriteLine($"FALLO: GUIA_SOLO ICONOS - el icono de '{tituloPaso}' se sale de la ventana (overflow real medido).");
                if (sinRecorte && dentroDeLaVentana) filasConIconoVisible++;

                // Captura INDIVIDUAL de esta fila (ademas de la general de mas abajo) - con
                // BringIntoView() ya hecho arriba, esta fila concreta queda dentro del viewport
                // capturado, evidencia fotografica real de las tres categorias por separado
                // (Npc-vecino / Objeto vanilla / Objeto Calamity), no solo la medicion numerica.
                try
                {
                    string nombreArchivo = "guia-fila-icono-" + string.Concat(tituloPaso.Where(char.IsLetterOrDigit)).ToLowerInvariant();
                    CapturaVentanaKeepQa(window, nombreArchivo);
                    Console.WriteLine($"GUIA_SOLO ICONOS: captura real individual -> keepqa-evidencia\\{nombreArchivo}.png");
                }
                catch (Exception ex) { Console.WriteLine($"GUIA_SOLO ICONOS: captura individual de '{tituloPaso}' fallo - " + ex.Message); }
            }
            Console.WriteLine($"GUIA_SOLO ICONOS: filas del arbol con icono real visible y sin recorte/overflow={filasConIconoVisible}/{objetivos.Length} (esperado {objetivos.Length}/{objetivos.Length})");

            try
            {
                CapturaVentanaKeepQa(window, "guia-arbol-iconos");
                Console.WriteLine("GUIA_SOLO ICONOS: captura real del arbol expandido -> keepqa-evidencia\\guia-arbol-iconos.png");
            }
            catch (Exception ex) { Console.WriteLine("GUIA_SOLO ICONOS: captura del arbol fallo - " + ex.Message); }

            // Guia Encargo2 (25-sep-2026, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): requisito real
            // "defensa" (paso "Armadura: mas de 10 de defensa", tramo "Antes del primer jefe",
            // guia_progresion.json ~linea 101, valor pedido=11) - ANTES de este arreglo salia
            // SIEMPRE NoEvaluable=true (Defensa.Get fijo a 0 tras el gate HasLiveGameData, fijo a
            // false en escritorio). Ahora DesktopGuideStateProvider.Defensa lee el equipo puesto
            // de verdad (MergedContainers["loadout0Items"]) via Terrakeep.Core.Model.
            // DefenseCalculator - misma formula que ya usaba la pestaña Equipamiento. Confirma con
            // el personaje real 'adrian' que el requisito ya NO es NoEvaluable.
            var pasoDefensa = EncontrarPaso("Antes del primer jefe", "Armadura: más de 10 de defensa");
            if (pasoDefensa == null)
            {
                Console.WriteLine("FALLO: GUIA_SOLO DEFENSA - no se encontro el paso real 'Armadura: más de 10 de defensa'.");
            }
            else
            {
                // Este paso tiene DOS requisitos reales (defensa + gancho, guia_progresion.json) -
                // "Defensa: {0} de {1}" (Guia.Req.Defensa, textos.es.json) es la unica linea que
                // empieza asi, se localiza por ese texto en vez de exponer el tipo interno.
                var reqDefensa = pasoDefensa.Value.paso.Requisitos.FirstOrDefault(r => r.Linea.StartsWith("Defensa:", StringComparison.Ordinal));
                if (reqDefensa == null)
                {
                    Console.WriteLine("FALLO: GUIA_SOLO DEFENSA - no se encontro el requisito 'Defensa: ...' dentro del paso real.");
                }
                else
                {
                    Console.WriteLine($"GUIA_SOLO DEFENSA: paso='Armadura: más de 10 de defensa' -> NoEvaluable={reqDefensa.NoEvaluable}, Cumplido={reqDefensa.Cumplido}, Linea='{reqDefensa.Linea}' (esperado NoEvaluable=False, Linea con la defensa real del equipo puesto de 'adrian')");
                    if (reqDefensa.NoEvaluable)
                        Console.WriteLine("FALLO: GUIA_SOLO DEFENSA - el requisito de defensa sigue NoEvaluable con un personaje real cargado (deberia leer el equipo puesto de verdad).");
                }
            }

            // Evidencia del BANNER "Tu objetivo ahora mismo" con icono real visible: el objetivo
            // ORGANICO de este personaje/mundo reales (evaluado arriba, "Cinco cristales de vida")
            // no tiene icono - resultado CORRECTO (CristalesVida no es Objeto/Npc/Jefe, sin datos
            // que inventar, ver ResolverIconoDelHito) y ya confirmado limpio (sin hueco roto) en
            // guia-real.png de arriba. Para dejar tambien evidencia real de la MISMA tarjeta con un
            // icono presente, se apunta ObjetivoTramo/ObjetivoPaso (propiedades publicas reales del
            // ViewModel) a uno de los tres GuidePasoViewModel YA evaluados de verdad arriba contra
            // el personaje/mundo cargados - mismo objeto real, mismo binding/convertidor de
            // produccion, solo se cambia CUAL paso ocupa el hueco de "objetivo" para la captura.
            var conIcono = EncontrarPaso("Antes del primer jefe", "Cuatro vecinos");
            if (conIcono != null)
            {
                vm.Guide.ObjetivoTramo = conIcono.Value.tramo;
                vm.Guide.ObjetivoPaso = conIcono.Value.paso;
                DoEvents(); DoEvents();
                // La pestaña quedo desplazada tras BringIntoView() de las filas del arbol (mas
                // abajo de la propia tarjeta) - sin esto la captura habria vuelto a salir con el
                // banner fuera del viewport, tapado por el arbol scrolleado. x:Name real en el
                // XAML (GuideObjetivoBanner) - FindName() en vez de un campo generado porque el
                // campo del elemento nombrado sale "internal" (mismo motivo real por el que el
                // resto de este arnes localiza controles recorriendo el arbol visual en vez de
                // referenciarlos por campo, ver Descendientes() arriba).
                if (window.FindName("GuideObjetivoBanner") is FrameworkElement banner) banner.BringIntoView();
                DoEvents(); DoEvents();
                try
                {
                    CapturaVentanaKeepQa(window, "guia-banner-con-icono");
                    Console.WriteLine("GUIA_SOLO ICONOS: captura real del banner con icono -> keepqa-evidencia\\guia-banner-con-icono.png");
                }
                catch (Exception ex) { Console.WriteLine("GUIA_SOLO ICONOS: captura del banner fallo - " + ex.Message); }
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
        vm.SelectedTabIndex = 5; // AppTab.Hosting, reordenado T1 21-sep-2026
        DoEvents(); DoEvents();

        // Evidencia CON NOMBRE de la pestaña Hosting (16-sep-2026, sesgo S1 de KeepQA -
        // AUDITORIA-SESGOS-16SEP.md: "Hosting" era la unica pantalla de Terrakeep sin ninguna
        // captura ni volcado con ese nombre en keepqa-evidencia, aunque este modo existia y
        // funcionaba). Captura + volcado del arbol visual completo en el formulario vacio, y otra
        // pareja mas abajo con la instancia real lanzada (la tarjeta de instancia, con su estado).
        try
        {
            CapturaVentanaKeepQa(window, "hosting-formulario");
            File.WriteAllText(Path.Combine(CarpetaEvidenciaKeepQa(), "volcado-geometria-hosting-formulario.json"),
                System.Text.Json.JsonSerializer.Serialize(VolcarArbolVisual(window, "ventana")));
            Console.WriteLine("HOSTING_SOLO: evidencia -> hosting-formulario.png + volcado-geometria-hosting-formulario.json");
        }
        catch (Exception ex) { Console.WriteLine("HOSTING_SOLO: evidencia del formulario fallo - " + ex.Message); }

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
        try
        {
            CapturaVentanaKeepQa(window, "hosting-instancia-activa");
            File.WriteAllText(Path.Combine(CarpetaEvidenciaKeepQa(), "volcado-geometria-hosting-instancia-activa.json"),
                System.Text.Json.JsonSerializer.Serialize(VolcarArbolVisual(window, "ventana")));
            Console.WriteLine("HOSTING_SOLO: evidencia -> hosting-instancia-activa.png + volcado-geometria-hosting-instancia-activa.json");
        }
        catch (Exception ex) { Console.WriteLine("HOSTING_SOLO: evidencia de la instancia fallo - " + ex.Message); }
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
