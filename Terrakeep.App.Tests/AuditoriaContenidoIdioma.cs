// A11-CONTENIDO-IDIOMA: EL CONTENIDO DEL JUEGO (no la interfaz) EN LOS DOS IDIOMAS.
//
// Vive en su propio fichero, y no dentro del Main() de Program.cs, por el mismo motivo real ya
// escrito en AuditoriaMaquetacion.cs: Program.cs lo tocan a la vez todas las sesiones que
// trabajan sobre este repo, y el 6-sep-2026 dos rondas en paralelo se pisaron ahi mismo. Como
// clase parcial de Program, este bloque ve todos sus helpers (Descendientes, DoEvents...) sin
// duplicar ni una linea, y la unica huella que deja en Program.cs es la llamada.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // ====================================================================================
    // A11-CONTENIDO-IDIOMA (6-sep-2026): ronda de traduccion del CONTENIDO del juego
    // ====================================================================================
    // Que cierra: hasta esta ronda la INTERFAZ estaba entera en los dos idiomas pero el
    // contenido del propio juego (nombres de objeto/NPC/tile/buff, tooltips descriptivos,
    // bonos de set) solo existia en español, y se veia en español tambien con la app en
    // ingles. Estaba escrito como LIMITE-CONOCIDO en bitacora.md y en el propio barrido
    // A10-IDIOMA-BARRIDO, que DESCUENTA como ruido cualquier texto que sea un nombre de
    // catalogo - o sea que A10, por diseño, no puede ver este bug. Este bloque mira justo eso.
    //
    // El detector, al reves que A10: con la app en INGLES, cualquier texto en pantalla que sea
    // exactamente el nombre ESPAÑOL de un objeto/NPC/tile que SI tiene nombre ingles real y
    // DISTINTO es contenido sin traducir a la vista, y es FALLO. Los nombres que de verdad
    // coinciden en los dos idiomas ("Gel", nombres propios, tiles sin traduccion real al
    // español) se descartan antes: no son un fallo, son un dato real.
    //
    // Ademas deja un volcado LITERAL del nombre/tooltip real de unos cuantos objetos, NPCs,
    // tiles y buffs conocidos en los dos idiomas - evidencia legible, no un "0 fallos" a secas.
    private static void AuditoriaContenidoDelJuegoEnIdioma(Window window, MainViewModel vm)
    {
        string idiomaAntes = vm.Settings.Language;
        int tabAntes = vm.SelectedTabIndex, innerAntes = vm.PersonajeInnerTabIndex;
        try
        {
            // Instancia propia (MainViewModel no expone la suya, y añadirle una propiedad solo
            // para el arnes tocaria un fichero que otras sesiones estan editando a la vez): son
            // los MISMOS ficheros reales de Assets y el idioma activo vive en
            // LocalizedContent.CurrentLanguage, que es del proceso - o sea que esta copia
            // responde exactamente igual que la que usa la ventana.
            var servicio = new Terrakeep.App.Services.CharacterFileService();

            // --- 1. Volcado literal: lo que de verdad se pinta, en los dos idiomas -----------
            // Ids reales y conocidos, citados uno a uno (los mismos que cubren las pruebas de
            // Core, para poder cruzar las dos evidencias).
            (int Id, string Que)[] objetos = [(1, "objeto"), (4, "objeto"), (8, "objeto"), (76, "armadura con bono de set")];
            (int Id, string Que)[] npcs = [(1, "NPC"), (4, "NPC jefe"), (22, "NPC de pueblo")];
            (int Id, string Que)[] tiles = [(0, "tile"), (1, "tile"), (21, "tile de cofre")];
            (int Id, string Que)[] buffs = [(1, "buff"), (2, "buff")];

            foreach (string idioma in new[] { "es", "en" })
            {
                vm.Settings.Language = idioma;
                DoEvents(); DoEvents();
                foreach (var (id, que) in objetos)
                    Console.WriteLine($"A11-VOLCADO[{idioma}] {que} {id}: nombre='{servicio.VanillaCatalog.GetName(id)}' tooltip='{Recortar(servicio.VanillaItemTooltips.Get(id))}' bonoSet='{Recortar(servicio.VanillaArmorSets.Get(id)?.DisplayText)}'");
                foreach (var (id, que) in npcs)
                    Console.WriteLine($"A11-VOLCADO[{idioma}] {que} {id}: '{servicio.NpcNames.GetName(id)}'");
                foreach (var (id, que) in tiles)
                    Console.WriteLine($"A11-VOLCADO[{idioma}] {que} {id}: '{servicio.TileNames.TileName(id)}'");
                foreach (var (id, que) in buffs)
                    Console.WriteLine($"A11-VOLCADO[{idioma}] {que} {id}: '{servicio.VanillaBuffs.GetDisplayName(id)}' - '{Recortar(servicio.VanillaBuffs.GetDescription(id))}'");
                var aerospec = servicio.CalamityCatalog.ByModAndInternal("CalamityMod", "AerospecHeadMelee");
                Console.WriteLine($"A11-VOLCADO[{idioma}] Calamity AerospecHeadMelee: '{aerospec?.DisplayName}' bonoSet='{Recortar(aerospec?.SetBonus)}'");
            }

            // Cada par tiene que ser DISTINTO entre idiomas o el cableado no esta haciendo nada.
            int distintos = 0, comparados = 0;
            void Comparar(string que, string? es, string? en)
            {
                comparados++;
                if (!string.IsNullOrEmpty(es) && es != en) distintos++;
                else Console.WriteLine($"FALLO: A11-CONTENIDO-IDIOMA - '{que}' se ve IGUAL en los dos idiomas (es='{Recortar(es)}', en='{Recortar(en)}')");
            }
            string Con(string idioma, Func<string?> leer)
            {
                vm.Settings.Language = idioma; DoEvents();
                return leer() ?? "";
            }
            foreach (var (id, _) in objetos)
                Comparar($"objeto {id}", Con("es", () => servicio.VanillaCatalog.GetName(id)), Con("en", () => servicio.VanillaCatalog.GetName(id)));
            foreach (var (id, _) in npcs)
                Comparar($"NPC {id}", Con("es", () => servicio.NpcNames.GetName(id)), Con("en", () => servicio.NpcNames.GetName(id)));
            foreach (var (id, _) in buffs)
                Comparar($"buff {id}", Con("es", () => servicio.VanillaBuffs.GetDisplayName(id)), Con("en", () => servicio.VanillaBuffs.GetDisplayName(id)));
            Comparar("bono de set del casco de cobre",
                Con("es", () => servicio.VanillaArmorSets.Get(76)?.DisplayText),
                Con("en", () => servicio.VanillaArmorSets.Get(76)?.DisplayText));
            Comparar("bono de set de Calamity (AerospecHeadMelee)",
                Con("es", () => servicio.CalamityCatalog.ByModAndInternal("CalamityMod", "AerospecHeadMelee")?.SetBonus),
                Con("en", () => servicio.CalamityCatalog.ByModAndInternal("CalamityMod", "AerospecHeadMelee")?.SetBonus));
            Console.WriteLine($"A11-CONTENIDO-IDIOMA: {distintos} de {comparados} textos de contenido cambian de verdad entre idiomas (esperado {comparados} de {comparados})");

            // --- 2. Barrido real de pantalla con la app en INGLES ---------------------------
            // Nombres ESPAÑOLES que tienen ingles real y DISTINTO: si uno aparece en pantalla
            // con la app en ingles, es contenido sin traducir a la vista. Los que coinciden en
            // los dos idiomas quedan fuera a proposito (no son un fallo, son un dato real).
            var españolConIngles = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (id, _) in servicio.VanillaCatalog.AllEntries("es"))
            {
                string es = servicio.VanillaCatalog.GetName(id, "es");
                string en = servicio.VanillaCatalog.GetName(id, "en");
                if (es != en && es.Length >= 5) españolConIngles[es] = en;
            }
            foreach (var (id, es) in servicio.NpcNames.AllFor("es"))
            {
                string en = servicio.NpcNames.GetName(id, "en");
                if (es != en && es.Length >= 5) españolConIngles[es] = en;
            }
            foreach (var (id, es) in servicio.TileNames.AllTilesFor("es"))
            {
                string en = servicio.TileNames.TileName(id, "en");
                if (es != en && es.Length >= 5) españolConIngles[es] = en;
            }

            // Datos del USUARIO, no del juego: el nombre de sus personajes y de sus mundos.
            // Encontrado midiendo de verdad, no supuesto: este barrido marco "Terrariano" en la
            // pestaña Inicio - que es un personaje REAL de esta maquina y a la vez, por pura
            // casualidad, el nombre español del yoyo "Terrarian" (objeto 3389). Traducir eso
            // seria falsear un dato del usuario, exactamente el mismo criterio ya establecido
            // con los letreros del mundo del Explorador.
            var datosDelUsuario = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in vm.Home.Characters) datosDelUsuario.Add(p.Name);
            foreach (var w in vm.Exploration.Worlds) datosDelUsuario.Add(w.Title);
            if (!string.IsNullOrWhiteSpace(vm.CharacterName)) datosDelUsuario.Add(vm.CharacterName);
            Console.WriteLine($"A11-DATOS-USUARIO: {datosDelUsuario.Count} nombres reales de personaje/mundo de esta maquina excluidos del barrido (dato del usuario, no del juego)");

            var encontrados = new List<string>();
            void ExaminarPantalla(string donde)
            {
                foreach (var tb in Descendientes<System.Windows.Controls.TextBlock>(window))
                {
                    if (!tb.IsVisible) continue;
                    // El Explorador añade el id real detras ("Bloque de tierra [0]") - se quita
                    // antes de comparar, el nombre de catalogo sigue siendo el mismo.
                    string t = System.Text.RegularExpressions.Regex.Replace(tb.Text ?? "", @" \[\d+\]$", "").Trim();
                    if (t.Length == 0 || datosDelUsuario.Contains(t)) continue;
                    if (españolConIngles.TryGetValue(t, out string? en))
                        encontrados.Add($"{donde}: \"{t}\" (deberia decir \"{en}\")");
                }
            }

            vm.Settings.Language = "en";
            DoEvents(); DoEvents();
            for (int tab = 0; tab <= 5; tab++)
            {
                vm.SelectedTabIndex = tab;
                DoEvents(); DoEvents();
                if (tab == 1)
                {
                    for (int inner = 0; inner <= 9; inner++)
                    {
                        try { vm.PersonajeInnerTabIndex = inner; } catch (Exception) { break; }
                        DoEvents(); DoEvents();
                        ExaminarPantalla($"Personaje/sub{inner}");
                    }
                    vm.PersonajeInnerTabIndex = 0;
                    DoEvents();
                }
                else ExaminarPantalla($"Pestaña{tab}");
            }

            // Exploracion SIN mundo cargado no enseña ni un nombre de tile ni de NPC - o sea que
            // el barrido de arriba, tal cual, no puede ver la mitad del contenido del juego. Se
            // carga el mundo real de esta maquina (solo LECTURA, nunca se guarda) y se recorren
            // sus cinco categorias reales, que es donde viven los nombres de tile/pared/mineral,
            // los objetos dentro de cofres y la lista de NPCs.
            string mundoReal = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (File.Exists(mundoReal))
            {
                vm.SelectedTabIndex = 4;
                DoEvents();
                var carga = vm.Exploration.LoadFromPathAsync(mundoReal);
                while (!carga.IsCompleted) DoEvents();
                DoEvents(); DoEvents();
                Console.WriteLine($"A11-EXPLORACION(en): mundo real cargado, {vm.Exploration.Npcs.Count} NPCs, primeros 5 = {string.Join(" | ", vm.Exploration.Npcs.Take(5).Select(n => n.Name))}");
                foreach (var categoria in Enum.GetValues<Terrakeep.App.ViewModels.WorldSearchCategory>())
                {
                    vm.Exploration.SelectedCategory = categoria;
                    DoEvents(); DoEvents();
                    ExaminarPantalla($"Exploracion/{categoria}");
                }
                Console.WriteLine($"A11-EXPLORACION(en): inventario del mundo, primeras 5 filas = {string.Join(" | ", vm.Exploration.Inventory.Take(5).Select(r => r.Name))}");
            }
            else Console.WriteLine("A11-EXPLORACION: roca_negra.wld no esta en esta maquina - la parte de Exploracion del barrido se omite (no es un fallo)");

            // La Libreria en reposo enseña tarjetas de carpeta: hay que ENTRAR en una para que
            // aparezcan las tarjetas de objeto reales, que es donde vive el nombre del juego.
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents(); DoEvents();
            var primeraCarpeta = vm.Library.RootCategories.FirstOrDefault();
            if (primeraCarpeta != null)
            {
                vm.Library.SelectCategoryCommand.Execute(primeraCarpeta);
                DoEvents(); DoEvents();
                ExaminarPantalla("Libreria/carpeta-abierta");
                Console.WriteLine($"A11-LIBRERIA(en): {vm.Library.Results.Count} tarjetas reales, primeras 5 = {string.Join(" | ", vm.Library.Results.Take(5).Select(r => r.DisplayName))}");
                vm.Library.ClearCategoryCommand.Execute(null);
                DoEvents();
            }

            Console.WriteLine($"A11-CONTENIDO-IDIOMA: nombres de contenido del juego que siguen en español con la app en ingles = {encontrados.Distinct().Count()} (esperado 0), sobre un vocabulario real de {españolConIngles.Count} nombres traducidos");
            foreach (string e in encontrados.Distinct().Take(30)) Console.WriteLine("   SIN-TRADUCIR-CONTENIDO " + e);
            if (encontrados.Count > 0) Console.WriteLine("FALLO: A11-CONTENIDO-IDIOMA - hay contenido del juego a la vista sin traducir con la app en ingles");
        }
        catch (Exception ex) { Console.WriteLine("A11-CONTENIDO-IDIOMA-EXCEPTION: " + ex); }
        finally
        {
            // El resto del arnes asume español y la pestaña donde estuviera - se deja como
            // estaba pase lo que pase (misma regla que A10 y que AR-13c: un bloque que cambie
            // estado persistido y no lo restaure contamina la ejecucion SIGUIENTE entera).
            vm.Settings.Language = idiomaAntes;
            vm.SelectedTabIndex = tabAntes;
            vm.PersonajeInnerTabIndex = innerAntes;
            DoEvents();
        }
    }

    private static string Recortar(string? t) =>
        t is null ? "(null)" : (t.Length > 70 ? t[..70].Replace("\n", " / ") + "..." : t.Replace("\n", " / "));
}
