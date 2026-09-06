using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TerrasavrNative.App;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

// ========================================================================================
// PB-* (6-sep-2026): oleada de pruebas de PERSONAJE > Buffs / Apariencia / Investigacion /
// Spawn Points / Desbloqueos / Version
// ========================================================================================
// Otra parte de la MISMA clase Program del arnes (ver el comentario de `partial` en
// Program.cs): usa tal cual sus helpers reales (DoEvents, FijarTamaño, RectVisible/
// VisibleEntero/RectCompleto/AlcanzableConScroll, Descendientes) sin duplicar ni uno.
//
// Lo que estos bloques miden y NINGUN bloque anterior medía:
//
//  - Estas pantallas EN SU ESTADO POBLADO Y ABIERTO. El barrido AR-LAY recorre las 7
//    sub-pestañas de Personaje a 13 tamaños x 2 idiomas, pero SIEMPRE en su estado de
//    llegada: la rejilla de buffs casi vacia, el selector de peinado cerrado, sin carpeta
//    elegida en Investigacion, con 0 o 1 spawn points y sin ningun aviso naranja desplegado.
//    Es exactamente el punto ciego que ya costo tres rondas esta semana ("NPCs que faltan"
//    solo fallaba con el Expander DESPLEGADO, y ni AR-11a ni AR-11c lo desplegaban nunca).
//    Aqui se llena la rejilla entera, se abren los dos selectores de Apariencia, se elige
//    una carpeta grande de Investigacion, se añaden 6 spawn points y se fuerzan los avisos
//    de version - y solo entonces se mide.
//  - Que la rejilla de Buffs sigue de verdad a la VERSION del personaje (44/22/10 reales de
//    PlrBodySerializer), contando los slots RENDERIZADOS, no solo la coleccion del ViewModel.
//  - Las duraciones reales de los 3 presets contra el catalogo real, incluido el techo por
//    version (S.getMaxTime()).
//
// Todo bloque que cambia estado compartido (version del personaje, buffs, spawn points,
// pestaña, tamaño de ventana) lo DEVUELVE como estaba - misma leccion ya documentada en
// bitacora.md con session.json y con AR-13c.
internal static partial class Program
{
    private static void PruebasPersonajeBuffsAparienciaVersion(MainViewModel vm, MainWindow window)
    {
        try
        {
            int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex;
            double anchoPrevio = window.ActualWidth, altoPrevio = window.ActualHeight;
            int versionPrevia = vm.VersionEditor.RawVersion;
            var buffsPrevios = vm.Buffs.Container!.Slots.Select(s => (s.Buff.Id, s.Buff.Time)).ToArray();
            int spawnsPrevios = vm.Servers.Entries.Count;

            // Tamaños reales: el suelo declarado, el de arranque, dos intermedios (donde han
            // vivido los tres bugs de esta semana) y maximizada.
            (double w, double h, string nombre)[] tamaños =
            [
                (1080, 700, "1080x700 (suelo real)"),
                (1180, 860, "1180x860 (arranque)"),
                (1400, 900, "1400x900"),
                (1520, 864, "1520x864 (umbral Amplio)"),
            ];

            // ---------------------------------------------------------------------------
            // PB-01: la rejilla de Buffs LLENA (los 44 slots con buff real), a cada tamaño.
            // ---------------------------------------------------------------------------
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 1; // Buffs
            DoEvents(); DoEvents();

            // Ids vanilla reales 1..N (1=Piel de obsidiana, 2=Regeneracion...). PlaceBuff
            // rechaza duplicados de verdad, asi que cada slot lleva un id distinto - que es
            // ademas el caso real mas exigente para la rejilla.
            var slots = vm.Buffs.Container!.Slots;
            for (int i = 0; i < slots.Count; i++) slots[i].PlaceBuff(i + 1);
            DoEvents(); DoEvents();
            int colocados = slots.Count(s => !s.IsEmpty);
            Console.WriteLine($"PB-01-LLENAR: {colocados}/{slots.Count} slots con buff real (esperado {slots.Count}), contador='{vm.Buffs.Container.DisplayName}'");
            if (colocados != slots.Count) Console.WriteLine("FALLO: PB-01 - PlaceBuff no lleno la rejilla entera con ids vanilla distintos");

            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                int enteros = 0, alcanzables = 0, perdidos = 0;
                string peor = "";
                double peorFalta = 0;
                foreach (var slot in slots)
                {
                    var fe = ElementoDe(window, slot);
                    if (fe == null) { perdidos++; peor = $"slot {slot.SlotIndex} sin elemento renderizado"; continue; }
                    if (VisibleEntero(fe, window)) enteros++;
                    var (perdido, faltaX, faltaY) = MedirPerdida(fe, window);
                    if (!perdido) { alcanzables++; continue; }
                    perdidos++;
                    if (faltaX + faltaY > peorFalta) { peorFalta = faltaX + faltaY; peor = $"slot {slot.SlotIndex} '{slot.DisplayName}' falta {faltaX:0.0}x{faltaY:0.0}px"; }
                }
                Console.WriteLine($"PB-01-REJILLA {nombre}: {enteros} slots enteros a la vista, {alcanzables}/{slots.Count} alcanzables, {perdidos} PERDIDOS (esperado 0 perdidos){(peor.Length > 0 ? " - peor: " + peor : "")}");
                if (perdidos > 0) Console.WriteLine($"FALLO: PB-01 - {perdidos} slots de buff no se pueden ver ni alcanzar a {nombre}");
            }

            // ---------------------------------------------------------------------------
            // PB-02: el panel "Editar buff seleccionado" con seleccion real (su contenido
            // crece: nombre, descripcion, duracion y los 3 presets con su valor). Columna
            // FIJA de 300px, asi que es un candidato natural a recortar por la derecha.
            // ---------------------------------------------------------------------------
            vm.BuffEdit.Slot = slots[0];
            slots[0].IsSelected = true;
            DoEvents(); DoEvents();
            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                var textos = Descendientes<TextBlock>(window)
                    .Where(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text) && EsDescendienteDe(t, vm.BuffEdit))
                    .ToList();
                int perdidos = textos.Count(t => MedirPerdida(t, window).Perdido);
                Console.WriteLine($"PB-02-EDITAR {nombre}: {textos.Count} textos del panel Editar, {perdidos} perdidos (esperado 0)");
                if (perdidos > 0) Console.WriteLine($"FALLO: PB-02 - el panel 'Editar buff' pierde {perdidos} texto(s) a {nombre}");
            }

            // ---------------------------------------------------------------------------
            // PB-03: los 3 presets de duracion, con el catalogo real. Invariantes reales:
            // Minima <= Media, Minima > 0, y ninguno pasa del techo global de la version
            // (BuffDurationPresets.MaxTicksForVersion = S.getMaxTime() real).
            // ---------------------------------------------------------------------------
            int techo = BuffDurationPresets.MaxTicksForVersion(vm.VersionEditor.RawVersion);
            int malos = 0;
            foreach (var slot in slots)
            {
                vm.BuffEdit.Slot = slot;
                DoEvents();
                vm.BuffEdit.ApplyMinCommand.Execute(null);
                int min = slot.Buff.Time;
                vm.BuffEdit.ApplyMediaCommand.Execute(null);
                int media = slot.Buff.Time;
                vm.BuffEdit.ApplyMaxCommand.Execute(null);
                int max = slot.Buff.Time;
                if (min <= 0 || min > media || media > max || max > techo)
                {
                    malos++;
                    Console.WriteLine($"   PB-03-PRESETS slot {slot.SlotIndex} '{slot.DisplayName}': min={min} media={media} max={max} techo={techo}");
                }
            }
            Console.WriteLine($"PB-03-PRESETS: {slots.Count} buffs reales medidos, {malos} con presets incoherentes (esperado 0; regla real: 0 < min <= media <= max <= {techo})");
            if (malos > 0) Console.WriteLine($"FALLO: PB-03 - {malos} buffs con duraciones Minima/Media/Maxima incoherentes");

            // El techo se aplica tambien al escribir segundos A MANO (H3-12: sin el, un numero
            // grande desbordaba el int de Buff.Time al multiplicar por 60 y se volvia NEGATIVO).
            slots[0].DurationSeconds = int.MaxValue / 60;
            DoEvents();
            Console.WriteLine($"PB-03-TECHO: escribir {int.MaxValue / 60}s a mano deja {slots[0].DurationSeconds}s = {slots[0].Buff.Time} ticks (esperado {techo / 60}s / {techo} ticks, nunca negativo)");
            if (slots[0].Buff.Time != techo) Console.WriteLine("FALLO: PB-03 - el techo real de duracion no acota la escritura manual de segundos");

            // ---------------------------------------------------------------------------
            // PB-04: sin duplicados. Terraria no tiene dos instancias del mismo buff a la vez.
            // ---------------------------------------------------------------------------
            slots[1].ClearCommand.Execute(null);
            DoEvents();
            bool aceptado = slots[1].PlaceBuff(slots[0].Buff.Id);
            Console.WriteLine($"PB-04-DUPLICADO: colocar el buff del slot 0 en el slot 1 -> aceptado={aceptado} (esperado False), aviso='{slots[1].RejectionMessage}' (esperado real)");
            if (aceptado || string.IsNullOrWhiteSpace(slots[1].RejectionMessage))
                Console.WriteLine("FALLO: PB-04 - la rejilla acepto dos instancias del mismo buff, o no aviso de por que no");
            // WouldRejectPlacingBuff es la version SIN efecto que usa el arrastre para poner el
            // cursor de prohibido MIENTRAS se arrastra - tiene que decir lo mismo.
            if (!slots[1].WouldRejectPlacingBuff(slots[0].Buff.Id))
                Console.WriteLine("FALLO: PB-04 - el arrastre no anticipa el rechazo que PlaceBuff SI aplica al soltar");
            slots[1].PlaceBuff(2);
            DoEvents();

            // ---------------------------------------------------------------------------
            // PB-05: Vaciar todos + Deshacer. Deshacer tiene que devolver el id Y la duracion
            // EXACTOS (RestoreExact), no una duracion "razonable" como una colocacion nueva.
            // ---------------------------------------------------------------------------
            var antesDeVaciar = slots.Select(s => (s.Buff.Id, s.Buff.Time)).ToArray();
            vm.Buffs.Container.ClearAllCommand.Execute(null);
            DoEvents();
            int vaciados = slots.Count(s => s.IsEmpty);
            Console.WriteLine($"PB-05-VACIAR: {vaciados}/{slots.Count} slots vacios tras 'Vaciar todos' (esperado {slots.Count}), CanUndoClear={vm.Buffs.Container.CanUndoClear} (esperado True), LastClearedCount={vm.Buffs.Container.LastClearedCount}");
            if (vaciados != slots.Count || !vm.Buffs.Container.CanUndoClear)
                Console.WriteLine("FALLO: PB-05 - 'Vaciar todos' no vacio la rejilla entera o no ofrecio Deshacer");
            vm.Buffs.Container.UndoClearCommand.Execute(null);
            DoEvents();
            int distintos = slots.Where((s, i) => s.Buff.Id != antesDeVaciar[i].Id || s.Buff.Time != antesDeVaciar[i].Time).Count();
            Console.WriteLine($"PB-05-DESHACER: {distintos} slots distintos de como estaban (esperado 0 - id Y duracion exactos), CanUndoClear={vm.Buffs.Container.CanUndoClear} (esperado False)");
            if (distintos > 0) Console.WriteLine($"FALLO: PB-05 - Deshacer no devolvio {distintos} slot(s) a su id/duracion exactos");

            // ---------------------------------------------------------------------------
            // PB-06: la rejilla sigue a la VERSION real del personaje, contando los slots
            // RENDERIZADOS (no solo la coleccion del ViewModel). 44 si >=269, 22 si >=77,
            // 10 si no - misma cuenta de PlrBodySerializer.
            // ---------------------------------------------------------------------------
            FijarTamaño(window, 1400, 900);
            foreach (int version in new[] { 248, 39, 279 })
            {
                vm.VersionEditor.SetVersionCommand.Execute(version);
                DoEvents(); DoEvents();
                int esperado = BuffsViewModel.SlotCountForVersion(version);
                int enColeccion = vm.Buffs.Container!.Slots.Count;
                int renderizados = vm.Buffs.Container.Slots.Count(s => ElementoDe(window, s) != null);
                Console.WriteLine($"PB-06-VERSION: version {version} -> {enColeccion} slots en el ViewModel y {renderizados} renderizados (esperado {esperado} en los dos)");
                if (enColeccion != esperado || renderizados != esperado)
                    Console.WriteLine($"FALLO: PB-06 - la rejilla de Buffs no siguio a la version {version} (esperado {esperado} slots)");
            }

            // ---------------------------------------------------------------------------
            // PB-07: el aviso REAL de bajada de version, con el contenido real del personaje.
            // Con los 44 buffs puestos, bajar a 248 tiene que decir cuantos se pierden.
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 6; // Version
            DoEvents();
            vm.VersionEditor.SetVersionCommand.Execute(248);
            DoEvents(); DoEvents();
            string? aviso = vm.VersionEditor.DowngradeWarning;
            Console.WriteLine($"PB-07-AVISO: con 44 buffs puestos, bajar a 248 -> \"{aviso}\" (esperado real, con el numero de buffs que se pierden)");
            if (string.IsNullOrWhiteSpace(aviso) || !aviso.Contains("buff"))
                Console.WriteLine("FALLO: PB-07 - bajar de 269 con buffs en los slots altos no aviso de que se pierden");
            // Y el aviso tiene que verse ENTERO, tambien al tamaño minimo: es un parrafo largo
            // dentro de un Border naranja, justo la forma de los tres bugs de esta semana.
            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                var bloque = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == aviso);
                if (bloque == null) { Console.WriteLine($"FALLO: PB-07 - el aviso de bajada de version no se pinta a {nombre}"); continue; }
                var completo = RectCompleto(bloque, window);
                var visible = RectVisible(bloque, window);
                double falta = Math.Max(0, completo.Height - (visible.IsEmpty ? 0 : visible.Height));
                bool alcanzable = falta <= 0.5 || AlcanzableConScroll(bloque, window, horizontal: false);
                Console.WriteLine($"PB-07-AVISO {nombre}: alto real {completo.Height:0}px, visible {(visible.IsEmpty ? 0 : visible.Height):0}px, falta {falta:0.0}px, alcanzable={alcanzable} (esperado True)");
                if (!alcanzable) Console.WriteLine($"FALLO: PB-07 - el aviso de bajada de version se corta sin forma de alcanzarlo a {nombre}");
            }
            vm.VersionEditor.SetVersionCommand.Execute(versionPrevia);
            DoEvents();

            // ---------------------------------------------------------------------------
            // PB-08: Apariencia con los DOS selectores abiertos (uno detras de otro: abrir uno
            // cierra el otro a proposito, Ap-a). El de peinado son 228 miniaturas reales
            // renderizadas - el estado mas pesado de toda la pestaña, y el que AR-LAY no ve.
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 3; // Apariencia
            DoEvents(); DoEvents();
            var swPelo = System.Diagnostics.Stopwatch.StartNew();
            vm.Appearance.OpenHairPickerCommand.Execute(null);
            DoEvents(); DoEvents();
            swPelo.Stop();
            Console.WriteLine($"PB-08-PEINADOS: {vm.Appearance.HairOptions.Count} miniaturas reales generadas en {swPelo.ElapsedMilliseconds}ms (esperado {PlayerPreviewRenderer.HairStyleCount}), tinte cerrado={!vm.Appearance.IsHairDyePickerOpen} (esperado True)");
            if (vm.Appearance.HairOptions.Count != PlayerPreviewRenderer.HairStyleCount)
                Console.WriteLine($"FALLO: PB-08 - el selector de peinado no ofrece los {PlayerPreviewRenderer.HairStyleCount} estilos reales");
            if (vm.Appearance.IsHairDyePickerOpen)
                Console.WriteLine("FALLO: PB-08 - abrir el selector de peinado no cerro el de tinte (los dos son paneles inline, se empujan)");

            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                int perdidas = 0, medidas = 0;
                foreach (var opcion in vm.Appearance.HairOptions)
                {
                    var fe = ElementoDe(window, opcion);
                    if (fe == null) continue; // virtualizada fuera de vista: no es perdida
                    medidas++;
                    if (MedirPerdida(fe, window).Perdido) perdidas++;
                }
                Console.WriteLine($"PB-08-PEINADOS {nombre}: {medidas} miniaturas realizadas en el arbol visual, {perdidas} perdidas (esperado 0)");
                if (perdidas > 0) Console.WriteLine($"FALLO: PB-08 - {perdidas} miniaturas de peinado no se ven ni se alcanzan a {nombre}");
            }
            vm.Appearance.CloseHairPickerCommand.Execute(null);
            vm.Appearance.OpenHairDyePickerCommand.Execute(null);
            DoEvents(); DoEvents();
            Console.WriteLine($"PB-08-TINTES: {vm.Appearance.HairDyeOptions.Count} tintes reales (esperado 13 = 12 de DyeInitializer + 'Ninguno'), peinado cerrado={!vm.Appearance.IsHairPickerOpen} (esperado True)");
            if (vm.Appearance.IsHairPickerOpen)
                Console.WriteLine("FALLO: PB-08 - abrir el selector de tinte no cerro el de peinado");
            vm.Appearance.CloseHairDyePickerCommand.Execute(null);
            DoEvents();

            // ---------------------------------------------------------------------------
            // PB-09: Investigacion con una carpeta grande elegida (tope real de 100 tarjetas)
            // y con el aviso de Modo Viaje visible - los dos estados que AR-LAY no alcanza.
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 2; // Investigacion
            DoEvents(); DoEvents();
            var carpetaGrande = vm.Research.RootCategories.OrderByDescending(c => c.ItemIdsOrdered.Count).First();
            vm.Research.SelectCategoryCommand.Execute(carpetaGrande);
            DoEvents(); DoEvents();
            Console.WriteLine($"PB-09-TOPE: carpeta '{carpetaGrande.Name}' con {carpetaGrande.ItemIdsOrdered.Count} objetos reales -> {vm.Research.Results.Count} tarjetas pintadas (esperado <= 100), resumen='{vm.Research.ResultsSummary}'");
            if (vm.Research.Results.Count > 100)
                Console.WriteLine("FALLO: PB-09 - Investigacion se salta su propio tope de 100 resultados");
            Console.WriteLine($"PB-09-VIAJE: dificultad={vm.Appearance.Difficulty} -> IsJourneyMode={vm.Research.IsJourneyMode} (el aviso naranja se ve cuando es False)");

            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                int perdidas = 0, medidas = 0;
                foreach (var fila in vm.Research.Results)
                {
                    var fe = ElementoDe(window, fila);
                    if (fe == null) continue;
                    medidas++;
                    if (MedirPerdida(fe, window).Perdido) perdidas++;
                }
                Console.WriteLine($"PB-09-INVESTIGACION {nombre}: {medidas} tarjetas realizadas, {perdidas} perdidas (esperado 0)");
                if (perdidas > 0) Console.WriteLine($"FALLO: PB-09 - {perdidas} tarjetas de Investigacion no se ven ni se alcanzan a {nombre}");
            }
            vm.Research.ClearCategoryCommand.Execute(null);
            DoEvents();

            // ---------------------------------------------------------------------------
            // PB-10: Spawn Points con varias filas reales (la tabla tiene columnas FIJAS de
            // 220/80/80/70 + 2 botones: es el candidato natural a no caber a lo ancho).
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 4; // Spawn Points
            DoEvents();
            int aAñadir = Math.Max(0, 6 - vm.Servers.Entries.Count);
            for (int i = 0; i < aAñadir; i++) vm.Servers.AddEntryCommand.Execute(null);
            DoEvents(); DoEvents();
            Console.WriteLine($"PB-10-FILAS: {vm.Servers.Entries.Count} spawn points reales en la tabla (esperado >= 6)");
            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                int perdidas = 0, medidas = 0;
                double peorFaltaX = 0;
                foreach (var fila in vm.Servers.Entries)
                {
                    var fe = ElementoDe(window, fila);
                    if (fe == null) continue;
                    medidas++;
                    var (perdido, faltaX, _) = MedirPerdida(fe, window);
                    if (!perdido) continue;
                    perdidas++;
                    peorFaltaX = Math.Max(peorFaltaX, faltaX);
                }
                Console.WriteLine($"PB-10-SPAWN {nombre}: {medidas} filas medidas, {perdidas} perdidas (esperado 0){(peorFaltaX > 0 ? $", peor recorte a lo ancho {peorFaltaX:0.0}px" : "")}");
                if (perdidas > 0) Console.WriteLine($"FALLO: PB-10 - {perdidas} fila(s) de Spawn Points no se ven ni se alcanzan a {nombre}");
            }
            for (int i = 0; i < aAñadir; i++)
                vm.Servers.RemoveEntryCommand.Execute(vm.Servers.Entries[^1]);
            DoEvents();

            // ---------------------------------------------------------------------------
            // PB-11: Desbloqueos con los 5 avisos de version desplegados a la vez (version
            // real por debajo de TODOS los umbrales) - la pestaña crece de golpe.
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 5; // Desbloqueos
            DoEvents();
            vm.VersionEditor.SetVersionCommand.Execute(100);
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();
            vm.PersonajeInnerTabIndex = 5;
            DoEvents(); DoEvents();
            int avisosPuestos = new[] { vm.Flags.ExtraAccessoryBelowVersion, vm.Flags.BiomeTorchesBelowVersion,
                vm.Flags.ExtraUsingFlagsBelowVersion, vm.Flags.FinishedDD2EventBelowVersion, vm.Flags.SuperMinecartBelowVersion }.Count(b => b);
            Console.WriteLine($"PB-11-AVISOS: version 100 -> {avisosPuestos}/5 avisos de version activos (esperado 5)");
            if (avisosPuestos != 5) Console.WriteLine("FALLO: PB-11 - los avisos por version de Desbloqueos no se activaron todos");
            foreach (var (w, h, nombre) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                var casillas = Descendientes<CheckBox>(window).Where(c => c.IsVisible).ToList();
                int perdidas = 0;
                foreach (var c in casillas)
                {
                    var (perdido, faltaX, faltaY) = MedirPerdida(c, window);
                    if (!perdido) continue;
                    perdidas++;
                    string etiqueta = Descendientes<TextBlock>(c).FirstOrDefault()?.Text ?? "(sin texto)";
                    Console.WriteLine($"   PB-11-PERDIDA \"{etiqueta}\": falta {faltaX:0.0}x{faltaY:0.0}px despues de traerla a la vista");
                }
                Console.WriteLine($"PB-11-DESBLOQUEOS {nombre}: {casillas.Count} casillas visibles, {perdidas} perdidas (esperado 0)");
                if (perdidas > 0) Console.WriteLine($"FALLO: PB-11 - {perdidas} casilla(s) de Desbloqueos no se ven ni se alcanzan a {nombre}");
            }
            // Marcar/desmarcar todo, las dos acciones en bloque reales.
            vm.Flags.MarkAllCommand.Execute(null);
            DoEvents();
            bool todasPuestas = vm.Flags.ExtraAccessory && vm.Flags.UnlockedBiomeTorches && vm.Flags.UsingBiomeTorches
                && vm.Flags.ArtisanBread && vm.Flags.VitalCrystal && vm.Flags.AegisFruit && vm.Flags.ArcaneCrystal
                && vm.Flags.GalaxyPearl && vm.Flags.GummyWorm && vm.Flags.Ambrosia && vm.Flags.FinishedDD2Event
                && vm.Flags.UnlockedSuperMinecart && vm.Flags.UsingSuperMinecart;
            vm.Flags.MarkNoneCommand.Execute(null);
            DoEvents();
            bool todasQuitadas = !(vm.Flags.ExtraAccessory || vm.Flags.UnlockedBiomeTorches || vm.Flags.UsingBiomeTorches
                || vm.Flags.ArtisanBread || vm.Flags.VitalCrystal || vm.Flags.AegisFruit || vm.Flags.ArcaneCrystal
                || vm.Flags.GalaxyPearl || vm.Flags.GummyWorm || vm.Flags.Ambrosia || vm.Flags.FinishedDD2Event
                || vm.Flags.UnlockedSuperMinecart || vm.Flags.UsingSuperMinecart);
            Console.WriteLine($"PB-11-BLOQUE: las 13 casillas tras 'Marcar todos'={todasPuestas} (esperado True), tras 'Desmarcar todos'={todasQuitadas} (esperado True)");
            if (!todasPuestas || !todasQuitadas) Console.WriteLine("FALLO: PB-11 - 'Marcar/Desmarcar todos' no alcanza las 13 casillas reales");

            // ---------------------------------------------------------------------------
            // PB-13: Ctrl+C / Ctrl+V REALES sobre la rejilla de buffs (teclado de verdad, no
            // llamadas al ViewModel), incluido el caso de RECHAZO por duplicado - que hasta esta
            // oleada dejaba al usuario sin ninguna explicacion de por que "no pasa nada".
            // ---------------------------------------------------------------------------
            vm.PersonajeInnerTabIndex = 1; // Buffs
            FijarTamaño(window, 1400, 900);
            DoEvents(); DoEvents();
            var slotsPB13 = vm.Buffs.Container!.Slots;
            foreach (var s in slotsPB13) s.ClearCommand.Execute(null);
            slotsPB13[0].PlaceBuff(1);          // Piel de obsidiana
            slotsPB13[0].SetDurationTicks(4321); // duracion que no coincide con ningun preset
            DoEvents(); DoEvents();

            SetForegroundWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle);
            var bordeOrigen = BordeFocusableDe(window, slotsPB13[0]);
            var bordeLibre = BordeFocusableDe(window, slotsPB13[3]);
            var bordeDuplicado = BordeFocusableDe(window, slotsPB13[4]);

            // Prueba de control ANTES de dar por bueno ningun resultado de teclado: Ctrl+1 es un
            // atajo global real de la app (vuelve a Inicio) y su efecto se ve en el ViewModel. Si
            // ni siquiera eso llega, en esta sesion el teclado sintetico con modificadores NO se
            // entrega a la ventana (RDP, ventana sin foco real de escritorio...) y CUALQUIER
            // conclusion sobre Ctrl+C/Ctrl+V seria falsa - se dice y se mide la misma secuencia
            // por la via de los ViewModels, que es donde vive el arreglo de esta oleada.
            int pestañaAntes = vm.SelectedTabIndex;
            PressCtrlPlus(0x31); // VK_1 -> Inicio
            DoEvents(); DoEvents();
            bool tecladoConCtrlLlega = vm.SelectedTabIndex == 0;
            vm.SelectedTabIndex = pestañaAntes;
            vm.PersonajeInnerTabIndex = 1;
            DoEvents(); DoEvents();
            Console.WriteLine($"PB-13-TECLADO: el teclado sintetico con Ctrl llega a la ventana={tecladoConCtrlLlega} (prueba de control real con Ctrl+1)");

            if (!tecladoConCtrlLlega)
            {
                // Misma secuencia EXACTA que ejecuta OnBuffSlotKeyDown en el caso Ctrl+V, paso a
                // paso: seleccionar el slot y despues pegar. Es justo el orden que arregla esta
                // oleada (seleccionar despues borraba el aviso, ver BuffAvisoRechazoVisibleTests).
                // Caso legitimo: copiar y MOVER. Con buffs, un copiar+pegar a otro slot sin
                // vaciar el origen es siempre un duplicado (Terraria no permite dos instancias
                // del mismo buff) - el uso real de Ctrl+C/Ctrl+V aqui es mover conservando la
                // duracion EXACTA, que es justo lo que PlaceBuff no haria (fija un preset).
                var copiado = (id: slotsPB13[0].Buff.Id, time: slotsPB13[0].Buff.Time);
                slotsPB13[0].ClearCommand.Execute(null);
                vm.SelectBuffSlot(slotsPB13[3]);
                bool pegado = slotsPB13[3].PasteBuff(copiado.id, copiado.time);
                Console.WriteLine($"PB-13-PEGAR(via ViewModel): aceptado={pegado} (esperado True), id={slotsPB13[3].Buff.Id} (esperado 1), duracion={slotsPB13[3].Buff.Time} (esperado 4321 EXACTOS, no un preset)");
                if (!pegado || slotsPB13[3].Buff.Id != 1 || slotsPB13[3].Buff.Time != 4321)
                    Console.WriteLine("FALLO: PB-13 - pegar un buff no reprodujo id y duracion exactos");

                // Y el rechazo real: el mismo buff otra vez, ahora si duplicado.
                vm.SelectBuffSlot(slotsPB13[4]);
                bool rechazado = !slotsPB13[4].PasteBuff(copiado.id, copiado.time);
                Console.WriteLine($"PB-13-DUPLICADO(via ViewModel): rechazado={rechazado} (esperado True), aviso='{slotsPB13[4].RejectionMessage}' (esperado real), el panel Editar mira ese slot={ReferenceEquals(vm.BuffEdit.Slot, slotsPB13[4])} (esperado True)");
                if (!rechazado || string.IsNullOrWhiteSpace(slotsPB13[4].RejectionMessage) || !ReferenceEquals(vm.BuffEdit.Slot, slotsPB13[4]))
                    Console.WriteLine("FALLO: PB-13 - el rechazo por duplicado no deja el aviso a la vista en el panel Editar");
            }
            else if (bordeOrigen == null || bordeLibre == null || bordeDuplicado == null)
            {
                Console.WriteLine("PB-13: no se encontraron los Border reales de los slots de buff - omitido");
            }
            else
            {
                System.Windows.Input.Keyboard.Focus(bordeOrigen);
                DoEvents();
                PressCtrlPlus(0x43); // VK_C
                DoEvents();
                // Vaciar el origen con Supr real: con buffs, copiar y pegar en otro slot SIN
                // vaciar es siempre un duplicado (Terraria no permite dos instancias del mismo
                // buff) - el uso real de Ctrl+C/Ctrl+V aqui es MOVER conservando la duracion
                // exacta, que es justo lo que una colocacion normal no haria.
                PressKey(0x2E); // VK_DELETE
                DoEvents();
                System.Windows.Input.Keyboard.Focus(bordeLibre);
                DoEvents();
                PressCtrlPlus(0x56); // VK_V
                DoEvents(); DoEvents();
                Console.WriteLine($"PB-13-PEGAR: slot 3 tras Ctrl+C(0)+Supr(0)+Ctrl+V(3) -> id={slotsPB13[3].Buff.Id} (esperado 1), duracion={slotsPB13[3].Buff.Time} ticks (esperado 4321 EXACTOS, no un preset)");
                if (slotsPB13[3].Buff.Id != 1 || slotsPB13[3].Buff.Time != 4321)
                    Console.WriteLine("FALLO: PB-13 - Ctrl+C/Ctrl+V real no reprodujo el buff con su duracion exacta");

                // Y ahora el rechazo real: pegar OTRA VEZ el mismo buff en un tercer slot, con
                // el buff ya puesto en el slot 3.
                System.Windows.Input.Keyboard.Focus(bordeDuplicado);
                DoEvents();
                PressCtrlPlus(0x56); // VK_V
                DoEvents(); DoEvents();
                bool vacio = slotsPB13[4].IsEmpty;
                string? aviso13 = slotsPB13[4].RejectionMessage;
                bool panelMirandoElSlot = ReferenceEquals(vm.BuffEdit.Slot, slotsPB13[4]);
                Console.WriteLine($"PB-13-DUPLICADO: pegar el mismo buff en el slot 4 -> sigue vacio={vacio} (esperado True), aviso='{aviso13}' (esperado real), el panel Editar mira ese slot={panelMirandoElSlot} (esperado True)");
                if (!vacio) Console.WriteLine("FALLO: PB-13 - Ctrl+V colo un segundo buff igual (Terraria no permite dos instancias del mismo buff)");
                if (string.IsNullOrWhiteSpace(aviso13) || !panelMirandoElSlot)
                    Console.WriteLine("FALLO: PB-13 - el rechazo por duplicado no deja ningun aviso a la vista al pegar con el teclado");
            }

            // --- restaurar TODO lo que este bloque toco -------------------------------
            vm.VersionEditor.SetVersionCommand.Execute(versionPrevia);
            DoEvents();
            var slotsFinales = vm.Buffs.Container!.Slots;
            for (int i = 0; i < slotsFinales.Count; i++)
                slotsFinales[i].RestoreExact(i < buffsPrevios.Length ? buffsPrevios[i].Id : 0,
                                             i < buffsPrevios.Length ? buffsPrevios[i].Time : 0);
            while (vm.Servers.Entries.Count > spawnsPrevios) vm.Servers.RemoveEntryCommand.Execute(vm.Servers.Entries[^1]);
            vm.BuffEdit.Slot = null;
            vm.SelectedTabIndex = tabPrevio;
            vm.PersonajeInnerTabIndex = innerPrevio;
            FijarTamaño(window, anchoPrevio, altoPrevio);
            DoEvents();
            Console.WriteLine($"PB-FIN: restaurado - version={vm.VersionEditor.RawVersion} (esperado {versionPrevia}), buffs puestos={vm.Buffs.Container!.Slots.Count(s => !s.IsEmpty)} (esperado {buffsPrevios.Count(b => b.Id != 0)}), spawn points={vm.Servers.Entries.Count} (esperado {spawnsPrevios})");
        }
        catch (Exception ex) { Console.WriteLine("PB-EXCEPTION: " + ex); }
    }

    // ¿Este elemento es contenido PERDIDO de verdad? Definicion honesta, la misma que AR-15
    // dejo escrita: primero se hace el gesto REAL del usuario (BringIntoView) y solo entonces
    // se pregunta. Sin ese paso previo el detector miente en un caso muy comun: un elemento que
    // esta ENTERO por debajo del scroll tiene rectangulo visible VACIO, asi que "lo que falta a
    // lo ancho" sale igual a su ancho completo - y si su contenedor no tiene scroll horizontal
    // (casi ninguno lo tiene), se marca como perdido cuando en realidad basta con bajar. Paso de
    // verdad en la primera pasada de PB-11 (la casilla "Ambrosia" a 1080x700, a 652px de un
    // viewport de 700: falso positivo perfecto).
    private static (bool Perdido, double FaltaX, double FaltaY) MedirPerdida(FrameworkElement fe, Window window)
    {
        if (VisibleEntero(fe, window)) return (false, 0, 0);
        fe.BringIntoView();
        DoEvents();
        if (VisibleEntero(fe, window)) return (false, 0, 0);
        var completo = RectCompleto(fe, window);
        var visible = RectVisible(fe, window);
        double faltaX = Math.Max(0, completo.Width - (visible.IsEmpty ? 0 : visible.Width));
        double faltaY = Math.Max(0, completo.Height - (visible.IsEmpty ? 0 : visible.Height));
        bool escapeX = faltaX <= 0.5 || AlcanzableConScroll(fe, window, horizontal: true);
        bool escapeY = faltaY <= 0.5 || AlcanzableConScroll(fe, window, horizontal: false);
        return (!(escapeX && escapeY), faltaX, faltaY);
    }

    // El Border real y enfocable de un slot de buff - el que recibe el foco de teclado y por
    // tanto el que hace llegar Ctrl+C/Ctrl+V a OnBuffSlotKeyDown (mismo criterio que
    // FindBorderForSlot usa para los slots de objeto).
    private static FrameworkElement? BordeFocusableDe(DependencyObject raiz, object dataContext) =>
        Descendientes<System.Windows.Controls.Border>(raiz)
            .FirstOrDefault(b => ReferenceEquals(b.DataContext, dataContext) && b.Focusable);

    // El elemento REAL del arbol visual que representa este objeto del ViewModel (la raiz de su
    // plantilla). Null si esta virtualizado fuera de vista, que no es lo mismo que perdido.
    private static FrameworkElement? ElementoDe(DependencyObject raiz, object dataContext) =>
        Descendientes<FrameworkElement>(raiz)
            .FirstOrDefault(f => ReferenceEquals(f.DataContext, dataContext) && f.IsVisible && f.ActualHeight > 0);

    // ¿Este elemento cuelga (por DataContext) del ViewModel dado? - para acotar una medicion a
    // un panel concreto sin depender de x:Name en el XAML de produccion.
    private static bool EsDescendienteDe(DependencyObject elemento, object dataContext)
    {
        for (var d = elemento; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
            if (d is FrameworkElement fe && ReferenceEquals(fe.DataContext, dataContext)) return true;
        return false;
    }
}
