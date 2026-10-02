// CANARIO REAL - Guia v2 (F2, 02-oct-2026, encargo "guia grande vanilla + Calamity").
//
// GUIAV2_SOLO=1 monta la ventana real, carga un personaje y un mundo de PRUEBA SINTETICOS (nunca
// partidas reales: se escriben con PlrFile.Write/WldWriter.WriteWorld en una carpeta temporal propia)
// y comprueba la pestaña Guia nueva de extremo a extremo:
//   1. EVALUACION correcta contra datos conocidos: guia elegida, clase detectada por el arma, paradas
//      hechas solas por bandera/vida+gancho, siguiente parada, tenencia "tengo X de Y", tarea
//      automatica cumplida, tarea manual persistida en disco (y nunca dentro de la partida) y
//      restaurada, obtencion de un objeto (receta real) y ubicacion en el mundo real (zona con firma
//      de tiles -> exacta; zona sin firma -> aproximada, y el marcador lo dice).
//   2. GEOMETRIA de cada pantalla (Mi guia, Ruta, Equipo, Mapa y biomas, Manual, Estoy perdido,
//      He encontrado algo raro, Buscar y la ficha de objeto) a 1080x700 y 2560x1440 en español y a
//      1080x700 en ingles: UN solo ScrollViewer visible y que sea el owner declarado
//      (GuideContentScroll), sin overflow horizontal, sin recorte horizontal sin escape (oraculo real
//      MedirClipHorizontal de RESTO_RESPONSIVE_SOLO), sin solapes entre textos/sprites hoja, sin
//      claves de idioma crudas "[guia2_...]" y uso de ancho >= 50 % a 2560.
//   3. Mapa de Exploracion: el marcador de la siguiente parada existe, esta en la casilla resuelta y
//      el circulo de zona aproximada aparece solo cuando la ubicacion es aproximada.
// Deja capturas en GUIAV2_EVIDENCIA o en <bin>/keepqa-evidencia/guia-v2 (hay que ABRIRLAS).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Guia.V2;
using Terrakeep.Core.Nbt;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

internal static partial class Program
{
    private static int EjecutarGuiaV2Solo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: GUIAV2_SOLO-{codigo} - {msg}"); }
        void Ok(string msg) => Console.WriteLine("GUIAV2_SOLO OK: " + msg);

        string outDir = Environment.GetEnvironmentVariable("GUIAV2_EVIDENCIA")
                        ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "guia-v2");
        Directory.CreateDirectory(outDir);
        string tempDir = Path.Combine(Path.GetTempPath(), "terrakeep-guiav2-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        string carpetaProgreso = Path.Combine(tempDir, "progreso");
        string idiomaOriginal = vm.Settings.Language;

        try
        {
            // ---------------- Fixture sintetico ----------------
            string plr = Path.Combine(tempDir, "PruebaGuiaV2.plr");
            string wld = Path.Combine(tempDir, "MundoPruebaGuiaV2.wld");
            File.WriteAllBytes(plr, PlrFile.Write(PersonajeSinteticoGuia()));
            File.WriteAllBytes(wld, WldWriter.WriteWorld(MundoSinteticoGuia()));
            Console.WriteLine($"GUIAV2_SOLO: fixture sintetico en {tempDir}");

            var g = vm.GuiaV2;
            g.CarpetaProgreso = carpetaProgreso;
            vm.Settings.Language = "es"; DoEvents();
            FijarTamaño(window, 1080, 700);

            // Sin datos: la guia se ve entera pero no evalua nada.
            vm.SelectedTabIndex = 3; DoEvents(); DoEvents();
            Console.WriteLine($"GUIAV2_SOLO: sin datos -> guia={g.GuiaId} HayDatos={g.HayDatos} paradas={g.ParadasTotal} tareas={g.TareasTotal}");
            if (g.ParadasTotal < 40) Fallo("CONTENIDO", $"solo {g.ParadasTotal} paradas cargadas (se esperan las ~47 de la guia)");
            CapturaGuia(window, outDir, "00-sin-datos-mi-guia-1080x700-es");

            vm.LoadFromPath(plr); DoEvents();
            var carga = vm.Exploration.LoadFromPathAsync(wld);
            while (!carga.IsCompleted) DoEvents();
            DoEvents(); WaitForDispatcher(200); DoEvents();
            vm.SelectedTabIndex = 3; DoEvents(); DoEvents();
            g.Refresh();
            while (!g.MarcadorTarea.IsCompleted) DoEvents();
            DoEvents();

            // ---------------- 1. Evaluacion ----------------
            bool hayVanilla = GuiaV2Cargador.GuiasDisponibles().Contains("vanilla");
            string guiaEsperada = hayVanilla ? "vanilla" : "calamity";
            Console.WriteLine($"GUIAV2_SOLO: guia={g.GuiaId} (esperada {guiaEsperada}: personaje sin .tplr y mundo sin .twld; vanilla incrustada={hayVanilla}) VanillaPendiente={g.VanillaPendiente}");
            if (g.GuiaId != guiaEsperada) Fallo("GUIA-ELEGIDA", $"guia {g.GuiaId}, esperada {guiaEsperada}");
            if (!hayVanilla && !g.VanillaPendiente) Fallo("AVISO-VANILLA", "partida vanilla sin guia vanilla incrustada: debe avisarse (VanillaPendiente)");
            if (g.ClaseDetectada != ClaseGuia.CuerpoACuerpo) Fallo("CLASE", $"clase detectada {g.ClaseDetectada}, esperada CuerpoACuerpo (espada larga de hierro en el inventario)");
            else Ok("clase detectada = cuerpo a cuerpo por la espada larga de hierro");
            if (g.ModoTexto != "Experto") Fallo("MODO", $"modo '{g.ModoTexto}', esperado 'Experto' (GameMode=1 del .wld sintetico)");

            if (!hayVanilla)
            {
                var inicio = g.Paradas.First(p => p.Id == "inicio");
                var eye = g.Paradas.First(p => p.Id == "eye");
                var slime = g.Paradas.First(p => p.Id == "slime");
                if (!inicio.CompletadaSola) Fallo("EVAL-INICIO", "'inicio' (vida 200>=180 + gancho) deberia salir hecha sola");
                if (!eye.CompletadaSola) Fallo("EVAL-EYE", "'eye' (downedBoss1=true en el .wld) deberia salir hecha sola");
                if (slime.Completada) Fallo("EVAL-SLIME", "'slime' (downedSlimeKing=false) no puede salir hecha");
                if (g.Siguiente?.Id != "slime") Fallo("EVAL-SIGUIENTE", $"siguiente parada {g.Siguiente?.Id}, esperada 'slime'");
                var t2 = inicio.Tareas.First(t => t.Id == "inicio.2");
                var t3 = inicio.Tareas.First(t => t.Id == "inicio.3");
                if (!t2.CumplidaSola) Fallo("EVAL-NPC", "inicio.2 (Mercader en el mundo) deberia comprobarse sola");
                if (!t3.CumplidaSola) Fallo("EVAL-ESTACIONES", "inicio.3 (banco, horno y yunque en el inventario) deberia comprobarse sola");
                var corona = slime.Necesitas.FirstOrDefault(n => n.Ref == "Terraria/SlimeCrown");
                if (corona?.Tengo != 2 || !corona.LoTiene) Fallo("EVAL-TENENCIA", $"Corona de slime: Tengo={corona?.Tengo}, esperado 2 (y LoTiene)");
                Console.WriteLine($"GUIAV2_SOLO: inicio={inicio.EstadoTexto} eye={eye.EstadoTexto} slime={slime.EstadoTexto} siguiente={g.Siguiente?.Id} corona={corona?.TextoTenencia} paradasHechas={g.ParadasHechas}");

                // Tarea manual: se marca, se persiste en la carpeta de la app (no en la partida) y se restaura.
                byte[] plrAntes = File.ReadAllBytes(plr);
                g.MarcarTarea("slime.2", true); DoEvents();
                var guardado = ArchivoProgresoGuia.Cargar(carpetaProgreso, g.GuiaId, "PruebaGuiaV2");
                bool tareaMarcada = g.Paradas.First(p => p.Id == "slime").Tareas.First(t => t.Id == "slime.2").MarcadaAMano;
                if (!guardado.TareaMarcada("slime.2") || !tareaMarcada) Fallo("PROGRESO", $"slime.2 marcada en disco={guardado.TareaMarcada("slime.2")} en pantalla={tareaMarcada}");
                else Ok("tarea manual slime.2 persistida en " + ArchivoProgresoGuia.NombreArchivo(g.GuiaId, "PruebaGuiaV2"));
                if (!File.ReadAllBytes(plr).SequenceEqual(plrAntes)) Fallo("PROGRESO-PARTIDA", "marcar una tarea ha modificado el .plr: el progreso NUNCA va dentro de la partida");
                g.MarcarTarea("slime.2", false); DoEvents();
                if (ArchivoProgresoGuia.Cargar(carpetaProgreso, g.GuiaId, "PruebaGuiaV2").TareaMarcada("slime.2")) Fallo("PROGRESO-DESMARCAR", "slime.2 sigue marcada en disco tras desmarcarla");

                // Obtencion real de un objeto (receta) y ficha.
                g.AbrirObjeto("Terraria/SlimeCrown"); DoEvents();
                var ficha = g.FichaObjeto;
                if (ficha == null || !ficha.TieneObtenciones || !ficha.Obtenciones.Any(o => o.Marcado.Contains("{o:Terraria/Gel}")))
                    Fallo("FICHA", "la ficha de la Corona de slime no trae su receta real (gel + corona de oro)");
                else Ok($"ficha Corona de slime: {ficha.Obtenciones.Count} forma(s) de conseguirla, libreria={ficha.PuedeLlevarALibreria}");
                g.CerrarFicha(); DoEvents();

                // Ubicacion: slime -> zona 'superficie' sin firma -> aproximada; desert -> arena -> exacta.
                Console.WriteLine($"GUIAV2_SOLO: marcador visible={g.MarcadorVisible} x={g.MarcadorX} y={g.MarcadorY} aprox={g.MarcadorAproximado} '{g.MarcadorTitulo}' / {g.MarcadorDetalle}");
                if (!g.MarcadorVisible || !g.MarcadorAproximado) Fallo("MARCADOR-SLIME", "la siguiente parada (Rey slime, superficie sin firma) debe marcarse como zona APROXIMADA");
                g.SeleccionarParada(g.Paradas.First(p => p.Id == "desert")); DoEvents();
                for (int i = 0; i < 50 && string.IsNullOrEmpty(g.ParadaSeleccionada?.UbicacionTexto); i++) { DoEvents(); WaitForDispatcher(20); }
                var desierto = g.ParadaSeleccionada!;
                Console.WriteLine($"GUIAV2_SOLO: desert -> {desierto.UbicacionTexto}");
                if (!desierto.TieneUbicacion || desierto.UbicacionTexto.StartsWith("Zona aproximada", StringComparison.Ordinal))
                    Fallo("UBICACION-DESIERTO", "el desierto (bloque de arena del mundo sintetico) deberia situarse de forma EXACTA");
            }

            if (hayVanilla)
            {
                // Guia vanilla (F1) contra el mismo fixture: vida 200 = 5 cristales -> 'subsuelo' hecha
                // sola; Ojo derrotado -> 'eye' hecha sola; sin casco de metal -> 'inicio' pendiente y
                // siguiente; Mercader en el mundo; garfio en el inventario; tenencia real.
                var inicio = g.Paradas.First(p => p.Id == "inicio");
                var subsuelo = g.Paradas.First(p => p.Id == "subsuelo");
                var eye = g.Paradas.First(p => p.Id == "eye");
                if (!subsuelo.CompletadaSola) Fallo("V-SUBSUELO", "'subsuelo' (5 cristales de vida: vida maxima 200) deberia salir hecha sola");
                if (!eye.CompletadaSola) Fallo("V-EYE", "'eye' (downedBoss1=true) deberia salir hecha sola");
                if (inicio.Completada || g.Siguiente?.Id != "inicio") Fallo("V-SIGUIENTE", $"siguiente={g.Siguiente?.Id}, esperada 'inicio' (sin casco de metal puesto)");
                if (g.Paradas.First(p => p.Id == "pueblo").Tareas.First(t => t.Id == "pueblo.1").CumplidaSola != true) Fallo("V-NPC", "pueblo.1 (Mercader en el .wld) deberia comprobarse sola");
                if (g.Paradas.First(p => p.Id == "movilidad").Tareas.First(t => t.Id == "movilidad.1").CumplidaSola != true) Fallo("V-GANCHO", "movilidad.1 (garfio en el inventario) deberia comprobarse sola");
                var banco = inicio.Necesitas.FirstOrDefault(n => n.Ref == "Terraria/WorkBench");
                var corona = g.Paradas.First(p => p.Id == "slime").Necesitas.FirstOrDefault(n => n.Ref == "Terraria/SlimeCrown");
                if (banco?.LoTiene != true || corona?.Tengo != 2) Fallo("V-TENENCIA", $"banco LoTiene={banco?.LoTiene}, corona Tengo={corona?.Tengo} (esperado True y 2)");
                Console.WriteLine($"GUIAV2_SOLO VANILLA: inicio={inicio.EstadoTexto} subsuelo={subsuelo.EstadoTexto} eye={eye.EstadoTexto} siguiente={g.Siguiente?.Id} corona={corona?.TextoTenencia} hechas={g.ParadasHechas}/{g.ParadasTotal}");

                byte[] plrAntes = File.ReadAllBytes(plr);
                g.MarcarTarea("inicio.1", true); DoEvents();
                bool enDisco = ArchivoProgresoGuia.Cargar(carpetaProgreso, g.GuiaId, "PruebaGuiaV2").TareaMarcada("inicio.1");
                bool enPantalla = g.Paradas.First(p => p.Id == "inicio").Tareas.First(t => t.Id == "inicio.1").MarcadaAMano;
                if (!enDisco || !enPantalla) Fallo("V-PROGRESO", $"inicio.1 marcada en disco={enDisco} en pantalla={enPantalla}");
                else Ok("tarea manual inicio.1 persistida en " + ArchivoProgresoGuia.NombreArchivo(g.GuiaId, "PruebaGuiaV2"));
                if (!File.ReadAllBytes(plr).SequenceEqual(plrAntes)) Fallo("V-PROGRESO-PARTIDA", "marcar una tarea ha modificado el .plr");
                g.MarcarTarea("inicio.1", false); DoEvents();

                // Ficha vanilla: la Corona de slime con su receta real; ninguna obtencion de Calamity.
                g.AbrirObjeto("Terraria/SlimeCrown"); DoEvents();
                var ficha = g.FichaObjeto;
                if (ficha == null || !ficha.Obtenciones.Any(o => o.Marcado.Contains("{o:Terraria/Gel}"))) Fallo("V-FICHA", "la ficha de la Corona de slime no trae su receta real");
                // Un objeto real con obtenciones de las dos procedencias (vanilla y añadidas por Calamity):
                // en la guia vanilla la ficha solo debe enseñar las vanilla.
                var mixto = GuiaV2Recursos.Referencias.Objetos.FirstOrDefault(kv => kv.Value.Obtencion.Any(x => x.EsDeCalamity) && kv.Value.Obtencion.Any(x => !x.EsDeCalamity));
                bool algunaCalamity = mixto.Key != null && g.Obtenciones(mixto.Key).Count != mixto.Value.ObtencionPara("vanilla").Count();
                Console.WriteLine($"GUIAV2_SOLO VANILLA: objeto mixto {mixto.Key}: {mixto.Value?.Obtencion.Count} obtenciones en la tabla, {(mixto.Key != null ? g.Obtenciones(mixto.Key).Count : 0)} en la ficha vanilla");
                if (algunaCalamity) Fallo("V-FICHA-CALAMITY", "la ficha vanilla enseña obtenciones de Calamity (debe usar ObtencionPara(\"vanilla\"))");
                g.CerrarFicha(); DoEvents();

                // Ubicaciones: la siguiente ('inicio', punto spawn) exacta; el desierto (arena) exacto;
                // la superficie (sin firma) aproximada.
                if (!g.MarcadorVisible || g.MarcadorAproximado || g.MarcadorX != 300) Fallo("V-MARCADOR", $"marcador visible={g.MarcadorVisible} aprox={g.MarcadorAproximado} x={g.MarcadorX} (esperado el spawn exacto x=300)");
                var tz = g.ResolverZonasAsync(); while (!tz.IsCompleted) DoEvents();
                var zDes = g.Zonas.First(z => z.Id == "desierto");
                var zSup = g.Zonas.First(z => z.Id == "superficie");
                Console.WriteLine($"GUIAV2_SOLO VANILLA: desierto -> {zDes.UbicacionTexto} | superficie -> {zSup.UbicacionTexto}");
                if (!zDes.TieneUbicacion || zDes.UbicacionTexto.StartsWith("Zona aproximada", StringComparison.Ordinal)) Fallo("V-DESIERTO", "el desierto (arena del mundo sintetico) deberia situarse EXACTO");
                if (!zSup.UbicacionTexto.StartsWith("Zona aproximada", StringComparison.Ordinal)) Fallo("V-SUPERFICIE", "la superficie (sin firma) debe decir que es APROXIMADA");
            }

            // Buscador.
            g.TextoBusqueda = "slime"; DoEvents(); DoEvents();
            Console.WriteLine($"GUIAV2_SOLO: buscar 'slime' -> {g.Resultados.Count} resultados (primero: {g.Resultados.FirstOrDefault()?.Tipo} {g.Resultados.FirstOrDefault()?.Titulo})");
            if (g.Resultados.Count == 0) Fallo("BUSCAR", "buscar 'slime' no devuelve nada");
            g.TextoBusqueda = ""; DoEvents();

            // ---------------- 2. Geometria por pantalla ----------------
            void Pantalla(string nombre, Action preparar, double w, double h, string idioma, bool conFicha = false)
            {
                vm.Settings.Language = idioma; DoEvents();
                FijarTamaño(window, w, h);
                vm.SelectedTabIndex = 3; DoEvents();
                preparar(); DoEvents(); DoEvents(); WaitForDispatcher(60); DoEvents();
                string cab = $"{nombre} {w:0}x{h:0} {idioma}";
                var svs = Descendientes<ScrollViewer>(window).Where(s => s.IsVisible && s.TemplatedParent is not TextBoxBase).ToList();
                int esperados = conFicha ? 2 : 1;
                if (svs.Count != esperados) Fallo("SCROLL-" + nombre, $"{cab}: {svs.Count} ScrollViewer visibles (esperado {esperados})");
                var sv = svs.FirstOrDefault(s => s.Name == "GuideContentScroll");
                if (sv == null) { Fallo("SCROLL-OWNER-" + nombre, $"{cab}: el owner GuideContentScroll no esta visible"); return; }
                if (sv.ExtentWidth > sv.ViewportWidth + 0.5) Fallo("HSCROLL-" + nombre, $"{cab}: overflow horizontal {sv.ExtentWidth:0.#} en {sv.ViewportWidth:0.#}");
                MedirClipHorizontal(window, sv, "GUIAV2-" + nombre, cab, (c, m) => Fallo(c, m));
                MedirTarjetasDeVersion(window, sv, "GUIAV2-" + nombre, cab, (c, m) => Fallo(c, m));
                int solapes = MedirSolapesGuia(window, sv, cab, (c, m) => Fallo(c, m));
                var crudas = Descendientes<TextBlock>(window).Where(t => t.IsVisible && (t.Text.Contains("[guia2_") || (t is TextoGuia tg && tg.TextoPlano.Contains("[guia2_")))).ToList();
                if (crudas.Count > 0) Fallo("CLAVE-CRUDA-" + nombre, $"{cab}: {crudas.Count} textos con clave sin traducir, p.ej. '{crudas[0].Text}'");
                if (w >= 1920 && !conFicha)
                {
                    sv.ScrollToVerticalOffset(0); DoEvents(); DoEvents();
                    double uso = sv.ViewportWidth > 0 ? MedirAnchoContenidoVisible(window, sv) / sv.ViewportWidth : 0;
                    if (uso < 0.5) Fallo("ANCHO-" + nombre, $"{cab}: el contenido visible solo usa {uso:P0} del ancho");
                    Console.WriteLine($"GUIAV2_SOLO {cab}: uso de ancho {uso:P0}");
                }
                Console.WriteLine($"GUIAV2_SOLO {cab}: vp={sv.ViewportHeight:0} ext={sv.ExtentHeight:0} solapes={solapes}");
                CapturaGuia(window, outDir, $"{nombre}-{w:0}x{h:0}-{idioma}");
            }

            var ruta = hayVanilla ? (Action)(() => g.ContinuarRutaCommand.Execute(null))
                : () => { g.SeleccionarParada(g.Paradas.First(p => p.Id == "slime")); };
            var pantallas = new (string Nombre, Action Preparar, bool Ficha)[]
            {
                ("01-mi-guia", () => g.Seccion = SeccionGuiaV2.MiGuia, false),
                ("02-ruta", () => { g.Seccion = SeccionGuiaV2.Ruta; ruta(); }, false),
                ("03-equipo", () => g.Seccion = SeccionGuiaV2.Equipo, false),
                ("04-biomas", () => { g.Seccion = SeccionGuiaV2.Biomas; var t = g.ResolverZonasAsync(); while (!t.IsCompleted) DoEvents(); }, false),
                ("05-manual", () => g.Seccion = SeccionGuiaV2.Manual, false),
                ("06-perdido", () => { g.Seccion = SeccionGuiaV2.Perdido; if (g.Problemas.Count > 0) g.Problemas[0].Abierta = true; }, false),
                ("07-raro", () => { g.Seccion = SeccionGuiaV2.Raro; if (g.Hallazgos.Count > 0) g.Hallazgos[0].Abierta = true; }, false),
                ("08-buscar", () => { g.TextoBusqueda = "slime"; }, false),
                ("09-ficha", () => { g.TextoBusqueda = ""; g.Seccion = SeccionGuiaV2.Ruta; g.AbrirObjeto("Terraria/SlimeCrown"); }, true),
            };
            // GUIAV2_RAPIDO=1: solo evaluacion + mapa (para iterar sin el barrido de geometria completo).
            bool rapido = Environment.GetEnvironmentVariable("GUIAV2_RAPIDO") == "1";
            foreach (var (w, h, idioma) in rapido ? [] : new[] { (1080.0, 700.0, "es"), (2560.0, 1440.0, "es"), (1080.0, 700.0, "en") })
            {
                foreach (var p in pantallas)
                {
                    Pantalla(p.Nombre, p.Preparar, w, h, idioma, p.Ficha);
                    if (p.Ficha) { g.CerrarFicha(); DoEvents(); }
                }
            }
            // Ruta con la parada bajada al final (secciones 3-4, listo, conserva y pie).
            vm.Settings.Language = "es"; FijarTamaño(window, 1080, 700); vm.SelectedTabIndex = 3;
            g.Seccion = SeccionGuiaV2.Ruta; ruta(); DoEvents(); DoEvents();
            if (Descendientes<ScrollViewer>(window).FirstOrDefault(s => s.Name == "GuideContentScroll") is { } svFin)
            {
                svFin.ScrollToVerticalOffset(svFin.ScrollableHeight * 0.55); DoEvents(); DoEvents();
                CapturaGuia(window, outDir, "02b-ruta-mitad-1080x700-es");
                svFin.ScrollToBottom(); DoEvents(); DoEvents();
                CapturaGuia(window, outDir, "02c-ruta-final-1080x700-es");
            }

            // ---------------- 3. Mapa de Exploracion ----------------
            g.Seccion = SeccionGuiaV2.MiGuia; DoEvents();
            g.VerParadaEnMapaCommand.Execute(g.Siguiente); DoEvents(); WaitForDispatcher(200); DoEvents(); DoEvents();
            var pin = Descendientes<Canvas>(window).FirstOrDefault(c => c.Name == "GuiaMarcadorSiguiente");
            var zona = Descendientes<System.Windows.Shapes.Ellipse>(window).FirstOrDefault(e => e.Name == "GuiaMarcadorZona");
            Console.WriteLine($"GUIAV2_SOLO MAPA: pestaña={vm.SelectedTabIndex} (4=Exploracion) pin visible={pin?.IsVisible} zona aprox visible={zona?.IsVisible} aprox={g.MarcadorAproximado}");
            if (vm.SelectedTabIndex != 4) Fallo("MAPA-NAVEGAR", "'Ver en el mapa' no lleva a Exploracion");
            if (pin == null || !pin.IsVisible) Fallo("MAPA-PIN", "el marcador de la siguiente parada no esta visible en el mapa");
            else
            {
                double esperadoX = g.MarcadorX + 0.5, esperadoY = g.MarcadorY + 0.5;
                if (Math.Abs(Canvas.GetLeft(pin) - esperadoX) > 0.01 || Math.Abs(Canvas.GetTop(pin) - esperadoY) > 0.01)
                    Fallo("MAPA-PIN-POS", $"pin en ({Canvas.GetLeft(pin)},{Canvas.GetTop(pin)}), esperado ({esperadoX},{esperadoY})");
            }
            // El pin tiene que quedar DENTRO del viewport real del mapa (no solo existir): "Ver en el
            // mapa" debe dejar la parada a la vista, no detras del panel lateral ni fuera de pantalla.
            for (int i = 0; i < 10; i++) { DoEvents(); WaitForDispatcher(40); }
            var mapaSv = Descendientes<ScrollViewer>(window).FirstOrDefault(sv0 => sv0.Name == "WorldMapScroll");
            if (pin != null && mapaSv != null)
            {
                var pPin = pin.TransformToAncestor(mapaSv).Transform(new Point(0, 0));
                // El panel lateral flota encima del mapa: el pin tiene que caer en la parte NO tapada.
                double tapado = vm.Settings.ExplorationSidebarWidth > 0 ? vm.Settings.ExplorationSidebarWidth + 9 : 0;
                bool dentro = pPin.X >= 0 && pPin.X <= mapaSv.ViewportWidth - tapado && pPin.Y >= 0 && pPin.Y <= mapaSv.ViewportHeight;
                Console.WriteLine($"GUIAV2_SOLO MAPA: pin en ({pPin.X:0},{pPin.Y:0}) del viewport {mapaSv.ViewportWidth:0}x{mapaSv.ViewportHeight:0} offset=({mapaSv.HorizontalOffset:0},{mapaSv.VerticalOffset:0}) -> dentro={dentro}");
                if (!dentro) Fallo("MAPA-PIN-FUERA", $"tras 'Ver en el mapa' el pin queda fuera del viewport del mapa ({pPin.X:0},{pPin.Y:0} en {mapaSv.ViewportWidth:0}x{mapaSv.ViewportHeight:0})");
            }
            if ((zona?.IsVisible ?? false) != g.MarcadorAproximado) Fallo("MAPA-ZONA", $"circulo de zona visible={zona?.IsVisible} pero aproximado={g.MarcadorAproximado}");
            CapturaGuia(window, outDir, "10-mapa-marcador-1080x700-es");
            // Parada con ubicacion APROXIMADA (Rey slime: superficie sin firma) -> circulo de zona.
            vm.SelectedTabIndex = 3; DoEvents();
            g.VerParadaEnMapaCommand.Execute(g.Paradas.First(p => p.Id == "slime"));
            for (int i = 0; i < 10; i++) { DoEvents(); WaitForDispatcher(40); }
            var zonaAprox = Descendientes<System.Windows.Shapes.Ellipse>(window).FirstOrDefault(e => e.Name == "GuiaMarcadorZona");
            Console.WriteLine($"GUIAV2_SOLO MAPA: slime aprox={g.MarcadorAproximado} circulo visible={zonaAprox?.IsVisible} '{g.MarcadorDetalle}'");
            if (!g.MarcadorAproximado || zonaAprox?.IsVisible != true) Fallo("MAPA-APROX", "una parada con ubicacion aproximada debe pintarse con el circulo de zona");
            CapturaGuia(window, outDir, "10b-mapa-zona-aproximada-1080x700-es");
            FijarTamaño(window, 2560, 1440); DoEvents(); DoEvents();
            CapturaGuia(window, outDir, "10-mapa-marcador-2560x1440-es");

            // ---------------- 4. Partida Calamity sintetica (.tplr + .twld de prueba) ----------------
            // Personaje con .tplr (Calamity detectado): mejora "bloodOrange" y una Esquirla de perla en el
            // inventario; mundo con .twld: Azote del desierto derrotado, Revengeance y el esquema del Mar
            // hundido encontrado, con el centro real del laboratorio guardado.
            string plrCal = Path.Combine(tempDir, "PruebaGuiaV2Cal.plr");
            string wldCal = Path.Combine(tempDir, "MundoPruebaGuiaV2Cal.wld");
            File.WriteAllBytes(plrCal, PlrFile.Write(PersonajeSinteticoGuia()));
            File.WriteAllBytes(Path.ChangeExtension(plrCal, ".tplr"), TplrFile.Write("data", TplrSinteticoGuia()));
            File.WriteAllBytes(wldCal, WldWriter.WriteWorld(MundoSinteticoGuia()));
            File.WriteAllBytes(Path.ChangeExtension(wldCal, ".twld"), TplrFile.Write("data", TwldSinteticoGuia()));
            vm.LoadFromPath(plrCal); DoEvents();
            var cargaCal = vm.Exploration.LoadFromPathAsync(wldCal);
            while (!cargaCal.IsCompleted) DoEvents();
            DoEvents(); WaitForDispatcher(200); DoEvents();
            FijarTamaño(window, 1080, 700);
            vm.SelectedTabIndex = 3; DoEvents();
            g.Refresh();
            while (!g.MarcadorTarea.IsCompleted) DoEvents();
            Console.WriteLine($"GUIAV2_SOLO CALAMITY: guia={g.GuiaId} modo='{g.ModoTexto}' paradas={g.ParadasHechas}/{g.ParadasTotal} siguiente={g.Siguiente?.Id}");
            if (g.GuiaId != "calamity") Fallo("CAL-GUIA", $"con .tplr/.twld la guia debe ser Calamity, es {g.GuiaId}");
            if (!g.ModoTexto.Contains("Revengeance")) Fallo("CAL-MODO", $"modo '{g.ModoTexto}' sin Revengeance (clave 'revenge' del .twld)");
            var pDesert = g.Paradas.FirstOrDefault(p => p.Id == "desert");
            var pSunken = g.Paradas.FirstOrDefault(p => p.Id == "sunken");
            var pMech3 = g.Paradas.FirstOrDefault(p => p.Id == "mech3");
            if (pDesert?.CompletadaSola != true) Fallo("CAL-DESERT", "'desert' (downedFlags desertScourge del .twld) deberia salir hecha sola");
            if (pDesert?.Tareas.FirstOrDefault(t => t.Id == "desert.4")?.CumplidaSola != true) Fallo("CAL-PERLA", "desert.4 (Esquirla de perla en el .tplr) deberia comprobarse sola");
            if (pSunken?.Tareas.FirstOrDefault(t => t.Id == "sunken.4")?.CumplidaSola != true) Fallo("CAL-ESQUEMA", "sunken.4 (HasFoundSunkenSeaSchematic) deberia comprobarse sola");
            if (pSunken?.Completada == true) Fallo("CAL-SUNKEN", "'sunken' no puede salir hecha: la Almeja gigante no esta derrotada");
            if (pMech3?.Tareas.FirstOrDefault(t => t.Id == "mech3.3")?.CumplidaSola != true) Fallo("CAL-MEJORA", "mech3.3 (mejora bloodOrange del .tplr) deberia comprobarse sola");
            if (pSunken != null)
            {
                g.SeleccionarParada(pSunken); DoEvents();
                for (int i = 0; i < 50 && string.IsNullOrEmpty(pSunken.UbicacionTexto); i++) { DoEvents(); WaitForDispatcher(20); }
                Console.WriteLine($"GUIAV2_SOLO CALAMITY: sunken -> {pSunken.UbicacionTexto}");
                if (!pSunken.TieneUbicacion || pSunken.UbicacionTexto.StartsWith("Zona aproximada", StringComparison.Ordinal))
                    Fallo("CAL-LAB", "el Mar hundido deberia situarse EXACTO en el centro del laboratorio guardado en el .twld");
            }
            var calPantallas = new (string Nombre, Action Preparar, bool Ficha)[]
            {
                ("11-cal-mi-guia", () => g.Seccion = SeccionGuiaV2.MiGuia, false),
                ("12-cal-ruta-desert", () => { if (pDesert != null) g.SeleccionarParada(pDesert); }, false),
                ("13-cal-equipo", () => g.Seccion = SeccionGuiaV2.Equipo, false),
                ("14-cal-ficha", () => { g.Seccion = SeccionGuiaV2.Ruta; g.AbrirObjeto("CalamityMod/DesertMedallion"); }, true),
            };
            foreach (var (w, h) in rapido ? [] : new[] { (1080.0, 700.0), (2560.0, 1440.0) })
                foreach (var p in calPantallas)
                {
                    Pantalla(p.Nombre, p.Preparar, w, h, "es", p.Ficha);
                    if (p.Ficha) { g.CerrarFicha(); DoEvents(); }
                }
        }
        catch (Exception ex) { fallos++; Console.WriteLine("GUIAV2_SOLO-EXCEPTION: " + ex); }
        finally
        {
            vm.Settings.Language = idiomaOriginal;
            vm.GuiaV2.TextoBusqueda = "";
            vm.GuiaV2.CerrarFicha();
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        Console.WriteLine($"GUIAV2_SOLO: {fallos} fallo(s); capturas en {outDir}");
        return fallos;
    }

    private static void CapturaGuia(Window window, string outDir, string nombre)
    {
        DoEvents(); DoEvents();
        File.WriteAllBytes(Path.Combine(outDir, $"guiav2-{nombre}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
    }

    /// <summary>Solapes reales entre elementos hoja visibles (textos y sprites) dentro del viewport
    /// del scroll de la guia: dos hojas que se pisan mas de 2px en ambos ejes sin que una contenga a
    /// la otra (un sprite dentro de su propio texto no cuenta).</summary>
    private static int MedirSolapesGuia(Window window, ScrollViewer sv, string cab, Action<string, string> fallo)
    {
        Rect vp;
        try { vp = sv.TransformToAncestor(window).TransformBounds(new Rect(0, 0, sv.ViewportWidth, sv.ViewportHeight)); }
        catch (InvalidOperationException) { return 0; }
        var hojas = new List<(FrameworkElement E, Rect R)>();
        foreach (var fe in Descendientes<FrameworkElement>(sv))
        {
            if (fe is not (TextBlock or Image)) continue;
            if (!fe.IsVisible || fe.ActualWidth < 2 || fe.ActualHeight < 2) continue;
            if (fe is TextBlock tb && string.IsNullOrWhiteSpace(tb.Text) && tb is not TextoGuia) continue;
            Rect r;
            try { r = fe.TransformToAncestor(window).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight)); }
            catch (InvalidOperationException) { continue; }
            if (!r.IntersectsWith(vp)) continue;
            hojas.Add((fe, r));
        }
        int n = 0;
        for (int i = 0; i < hojas.Count; i++)
            for (int j = i + 1; j < hojas.Count; j++)
            {
                var inter = Rect.Intersect(hojas[i].R, hojas[j].R);
                if (inter.IsEmpty || inter.Width <= 2 || inter.Height <= 2) continue;
                if (hojas[i].E.IsAncestorOf(hojas[j].E) || hojas[j].E.IsAncestorOf(hojas[i].E)) continue;
                // Imagen dentro de una plantilla de tarjeta con su propio halo (Ellipse) no es hoja.
                n++;
                if (n <= 3)
                    fallo("SOLAPE", $"{cab}: {Describir(hojas[i].E)} y {Describir(hojas[j].E)} se solapan {inter.Width:0.#}x{inter.Height:0.#}px");
            }
        return n;
    }

    private static PlrCharacter PersonajeSinteticoGuia()
    {
        var inventario = new PlrItemSlot[50];
        for (int i = 0; i < inventario.Length; i++) inventario[i] = PlrItemSlot.Empty;
        // ItemID 1.4.4.9 reales (ItemID.cs): 4 espada larga de hierro, 84 garfio, 36 banco de trabajo,
        // 33 horno, 35 yunque de hierro, 560 corona de slime.
        inventario[0] = new PlrItemSlot(4, 1, 0, false);
        inventario[1] = new PlrItemSlot(84, 1, 0, false);
        inventario[2] = new PlrItemSlot(36, 1, 0, false);
        inventario[3] = new PlrItemSlot(33, 1, 0, false);
        inventario[4] = new PlrItemSlot(35, 1, 0, false);
        inventario[5] = new PlrItemSlot(560, 2, 0, false);
        return new PlrCharacter
        {
            Version = 279,
            Name = "PruebaGuiaV2",
            Difficulty = 0,
            HealthMax = 200,
            HealthNow = 200,
            ManaMax = 40,
            ManaNow = 40,
            HairColor = [90, 60, 35],
            SkinColor = [255, 200, 165],
            EyeColor = [105, 90, 75],
            ShirtColor = [175, 165, 140],
            UnderColor = [85, 85, 180],
            PantsColor = [170, 140, 90],
            ShoesColor = [130, 90, 60],
            Inventory = inventario,
            EquipmentItems = [PlrItemSlot.Empty, PlrItemSlot.Empty, PlrItemSlot.Empty, PlrItemSlot.Empty, PlrItemSlot.Empty],
            EquipmentDyes = new PlrItemSlot[5],
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
    }

    private static Terrakeep.Core.Nbt.NbtCompound TplrSinteticoGuia()
    {
        return NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("PearlShard")), ("slot", new NbtShort(10)), ("stack", new NbtInt(3)))
            ])),
            ("modData", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("CalamityPlayer")),
                    ("data", NbtCompound.Of(("boost", new NbtList(NbtTagType.String, [new NbtString("bloodOrange")])))))
            ])));
    }

    private static Terrakeep.Core.Nbt.NbtCompound TwldSinteticoGuia()
    {
        return NbtCompound.Of(
            ("modData", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("DownedBossSystem")),
                    ("data", NbtCompound.Of(("downedFlags", new NbtList(NbtTagType.String, [new NbtString("desertScourge")]))))),
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("MiscWorldStateSystem")),
                    ("data", NbtCompound.Of(
                        ("downed", new NbtList(NbtTagType.String, [new NbtString("revenge"), new NbtString("HasFoundSunkenSeaSchematic")])),
                        // Centro del laboratorio en coordenadas de MUNDO (16 px por casilla), como lo guarda Calamity.
                        ("SunkenSeaLabCenter", NbtCompound.Of(("x", new NbtFloat(450 * 16f)), ("y", new NbtFloat(300 * 16f)))))))
            ])));
    }

    private static WldWorld MundoSinteticoGuia()
    {
        const int ancho = 600, alto = 400, suelo = 150;
        var tiles = new WldTile[ancho, alto];
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++)
            {
                bool arena = x is >= 400 and < 470 && y is >= suelo and < suelo + 40; // TileID 53 = arena: firma del desierto
                tiles[x, y] = y < suelo ? WldTile.Empty : new WldTile(type: (short)(arena ? 53 : y < 250 ? 0 : 1), wall: 0, liquidType: 0, liquidAmount: 0, u: 0, v: 0);
            }
        return new WldWorld
        {
            Header = new WldHeader
            {
                Version = 279, Pointers = [], TileFrameImportant = [], Title = "MundoPruebaGuiaV2", WorldId = 4242,
                TilesHigh = alto, TilesWide = ancho, SpawnX = 300, SpawnY = suelo - 1, GroundLevel = suelo, RockLevel = 250,
                Seed = "keepqa-guiav2", GameMode = 1, DungeonX = 80, DungeonY = 200, Time = 0, DayTime = true, MoonPhase = 0,
                BloodMoon = false, IsEclipse = false, IsCrimson = false,
                DownedBoss1EyeOfCthulhu = true, DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false, DownedBoss3Skeletron = false,
                DownedQueenBee = false, DownedMechBoss1TheDestroyer = false, DownedMechBoss2TheTwins = false,
                DownedMechBoss3SkeletronPrime = false, DownedPlantBoss = false, DownedGolemBoss = false,
                DownedGoblinArmy = false, DownedFrostLegion = false, DownedPirates = false, HardMode = false,
            },
            Tiles = tiles,
            // NPCID 17 = Mercader (vecino real, posicion guardada).
            Npcs = [new WldNpc { Id = 17, GivenName = "Mercader", TileX = 310, TileY = suelo - 1, Homeless = false, VariationIndex = 0 }],
            Chests = [],
            Signs = [],
            TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(),
            Bestiary = new WldBestiary { Kills = new Dictionary<string, int>(), Sighted = new HashSet<string>(), Chatted = new HashSet<string>() },
        };
    }
}
