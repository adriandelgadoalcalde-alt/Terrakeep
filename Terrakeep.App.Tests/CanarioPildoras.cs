// CANARIO REAL - PILDORAS_SOLO (F4 de la Guia v2, 02-oct-2026).
//
// Peticion del usuario, literal: "el badge que me gusta ya lo sabes cual es: es el rectangular con los
// cantos redondos, ya hemos cambiado muchas veces estos badges". La Guia v2 habia traido su propio
// estilo de pastilla (GuiaChip, CornerRadius=999) que en pantalla salia a cientos (se repite en cada
// parada, objeto, tarea y zona).
//
// Auditoria POR RENDER, no por texto: recorre el arbol visual REAL de cada pestaña (y de cada
// sub-pestaña de Personaje y cada seccion de la Guia, mas la ficha de objeto), con un personaje y un
// mundo de PRUEBA sinteticos (vanilla y Calamity), y cuenta los Border visibles que tienen forma de
// pildora u ovalo: radio de esquina >= la mitad de su alto real. Solo cuenta los que hacen de badge,
// etiqueta o chip: alto real <= 40 px y con texto dentro. Los que estan dentro de un boton/toggle
// (botones reales de accion: filtros de la Libreria, selector de prefijo...) se listan aparte y no
// fallan: el encargo permite que los botones sigan siendo botones.
// Umbral: 0 pildoras-badge en todas las pantallas. Deja capturas en PILDORAS_EVIDENCIA.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Nbt;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

internal static partial class Program
{
    private static int EjecutarPildorasSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        string outDir = Environment.GetEnvironmentVariable("PILDORAS_EVIDENCIA")
                        ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "pildoras");
        Directory.CreateDirectory(outDir);
        string tempDir = Path.Combine(Path.GetTempPath(), "terrakeep-pildoras-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        string idiomaOriginal = vm.Settings.Language;
        int totalBadges = 0, totalBotones = 0;

        void Auditar(string pantalla, bool capturar = true)
        {
            DoEvents(); DoEvents(); WaitForDispatcher(150);
            var (badges, botones) = ContarPildoras(window);
            totalBadges += badges.Count; totalBotones += botones.Count;
            Console.WriteLine($"PILDORAS {pantalla}: badges-pildora={badges.Count} botones-pildora={botones.Count}");
            foreach (var d in badges.Take(12)) Console.WriteLine($"   badge: {d}");
            if (badges.Count > 12) Console.WriteLine($"   ... y {badges.Count - 12} mas");
            foreach (var d in botones.GroupBy(x => x).Take(6)) Console.WriteLine($"   boton (no falla): {d.Key} x{d.Count()}");
            if (badges.Count > 0) { fallos++; Console.WriteLine($"FALLO: PILDORAS-{pantalla} - {badges.Count} badge(s) con forma de pildora (radio >= mitad del alto); el estilo de la app es rectangular con cantos redondeados (CornerRadius 8)"); }
            if (capturar)
                File.WriteAllBytes(Path.Combine(outDir, $"pildoras-{pantalla}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
        }

        try
        {
            vm.Settings.Language = "es"; DoEvents();
            var g = vm.GuiaV2;
            g.CarpetaProgreso = Path.Combine(tempDir, "progreso");

            void RecorrerTodo(string etiqueta, int ancho, int alto)
            {
                FijarTamaño(window, ancho, alto);
                string suf = $"{etiqueta}-{ancho}x{alto}";
                foreach (var (tab, nombre) in new[] { (0, "inicio"), (2, "builds"), (4, "exploracion"), (5, "servidor"), (6, "novedades"), (7, "acerca") })
                {
                    vm.SelectedTabIndex = tab; WaitForDispatcher(200);
                    Auditar($"{nombre}-{suf}");
                }
                vm.SelectedTabIndex = 1;
                for (int i = 0; i < 8; i++)
                {
                    try { vm.PersonajeInnerTabIndex = i; } catch { continue; }
                    WaitForDispatcher(250);
                    Auditar($"personaje{i}-{suf}");
                }
                vm.SelectedTabIndex = 3; DoEvents();
                g.Refresh();
                while (!g.MarcadorTarea.IsCompleted) DoEvents();
                foreach (SeccionGuiaV2 s in Enum.GetValues(typeof(SeccionGuiaV2)))
                {
                    if (s == SeccionGuiaV2.Buscar) g.TextoBusqueda = "slime";
                    g.Seccion = s;
                    if (s == SeccionGuiaV2.Ruta && g.Siguiente != null) g.SeleccionarParada(g.Siguiente);
                    if (s == SeccionGuiaV2.Biomas) { var t = g.ResolverZonasAsync(); while (!t.IsCompleted) DoEvents(); }
                    WaitForDispatcher(250);
                    Auditar($"guia-{s}-{suf}");
                    // La Ruta es larga: tambien la parte de abajo de la parada (tareas, conserva...).
                    if (s == SeccionGuiaV2.Ruta)
                    {
                        var sv = Descendientes<ScrollViewer>(window).FirstOrDefault(x => x.Name == "GuideContentScroll");
                        sv?.ScrollToVerticalOffset(sv.ScrollableHeight / 2); WaitForDispatcher(150);
                        Auditar($"guia-Ruta-mitad-{suf}");
                        sv?.ScrollToTop();
                    }
                }
                g.TextoBusqueda = "";
                g.Seccion = SeccionGuiaV2.Ruta;
                g.AbrirObjeto("Terraria/SlimeCrown"); WaitForDispatcher(200);
                Auditar($"guia-ficha-{suf}");
                g.CerrarFicha(); DoEvents();
            }

            // Inicio tal como lo deja el arranque del arnes (copias aisladas de las carpetas reales,
            // tarjeta hero con sus KPI): se audita SIN captura, para no guardar datos de partidas reales.
            FijarTamaño(window, 1080, 700);
            vm.SelectedTabIndex = 0; WaitForDispatcher(300);
            Auditar("inicio-arranque-1080x700", capturar: false);

            // Vanilla sin partida (estado "carga un mundo" de la cabecera de la Guia).
            FijarTamaño(window, 1080, 700);
            vm.SelectedTabIndex = 3; DoEvents(); g.Refresh(); DoEvents();
            g.Seccion = SeccionGuiaV2.MiGuia; WaitForDispatcher(200);
            Auditar("guia-sin-partida-1080x700");

            // Vanilla con personaje y mundo de prueba.
            string plr = Path.Combine(tempDir, "PruebaPildoras.plr");
            string wld = Path.Combine(tempDir, "MundoPruebaPildoras.wld");
            File.WriteAllBytes(plr, PlrFile.Write(PersonajeSinteticoGuia()));
            File.WriteAllBytes(wld, WldWriter.WriteWorld(MundoSinteticoGuia()));
            vm.LoadFromPath(plr); DoEvents();
            var carga = vm.Exploration.LoadFromPathAsync(wld);
            while (!carga.IsCompleted) DoEvents();
            WaitForDispatcher(200);
            RecorrerTodo("vanilla", 1080, 700);
            RecorrerTodo("vanilla", 2560, 1440);

            // Calamity (.tplr + .twld de prueba).
            string plrCal = Path.Combine(tempDir, "PruebaPildorasCal.plr");
            string wldCal = Path.Combine(tempDir, "MundoPruebaPildorasCal.wld");
            File.WriteAllBytes(plrCal, PlrFile.Write(PersonajeSinteticoGuia()));
            File.WriteAllBytes(Path.ChangeExtension(plrCal, ".tplr"), TplrFile.Write("data", TplrSinteticoGuia()));
            File.WriteAllBytes(wldCal, WldWriter.WriteWorld(MundoSinteticoGuia()));
            File.WriteAllBytes(Path.ChangeExtension(wldCal, ".twld"), TplrFile.Write("data", TwldSinteticoGuia()));
            vm.LoadFromPath(plrCal); DoEvents();
            var cargaCal = vm.Exploration.LoadFromPathAsync(wldCal);
            while (!cargaCal.IsCompleted) DoEvents();
            WaitForDispatcher(200);
            RecorrerTodo("calamity", 1080, 700);
        }
        catch (Exception ex)
        {
            fallos++;
            Console.WriteLine("FALLO: PILDORAS-EXCEPTION - " + ex);
        }
        finally
        {
            vm.Settings.Language = idiomaOriginal;
            try { Directory.Delete(tempDir, true); } catch { }
        }
        Console.WriteLine($"PILDORAS_SOLO: {fallos} pantalla(s) con pildoras; total badges-pildora={totalBadges}, botones-pildora (no fallan)={totalBotones}; capturas en {outDir}");
        return fallos;
    }

    /// <summary>Border visibles con forma de pildora/ovalo (radio >= mitad del alto real), de tamaño de
    /// badge (alto real &lt;= 40) y con texto dentro. Separa los que viven dentro de un boton.</summary>
    private static (List<string> Badges, List<string> Botones) ContarPildoras(Window window)
    {
        var badges = new List<string>();
        var botones = new List<string>();
        foreach (var b in Descendientes<Border>(window))
        {
            if (!b.IsVisible || b.ActualHeight < 8 || b.ActualHeight > 40 || b.ActualWidth < 8) continue;
            var cr = b.CornerRadius;
            double radio = Math.Max(Math.Max(cr.TopLeft, cr.TopRight), Math.Max(cr.BottomLeft, cr.BottomRight));
            if (radio < b.ActualHeight / 2 - 0.5) continue;
            var texto = Descendientes<TextBlock>(b).FirstOrDefault(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text));
            if (texto == null) continue;
            string desc = $"'{RecortarPildora(texto.Text)}' r={radio:0} alto={b.ActualHeight:0}";
            if (EsElCuerpoDeUnBoton(b)) botones.Add(desc); else badges.Add(desc);
        }
        return (badges, botones);
    }

    private static string RecortarPildora(string s) => s.Length > 40 ? s[..40] + "…" : s.Replace("\n", " ");

    /// <summary>La pildora ES el boton (su fondo/chrome: el boton mide lo mismo de alto), no una
    /// etiqueta metida dentro de una tarjeta-boton (p. ej. "No lo tienes" dentro de la tarjeta de
    /// objeto, que SI es un badge). Tambien barras y thumbs de scroll.</summary>
    private static bool EsElCuerpoDeUnBoton(Border b)
    {
        for (var p = VisualTreeHelper.GetParent(b); p != null; p = VisualTreeHelper.GetParent(p))
        {
            if (p is ScrollBar || p is Thumb) return true;
            if (p is ButtonBase bb) return Math.Abs(bb.ActualHeight - b.ActualHeight) <= 6;
            if (p is Window) break;
        }
        return false;
    }
}
