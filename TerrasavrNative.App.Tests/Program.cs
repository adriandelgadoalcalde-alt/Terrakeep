// Auditoria de Opus, Bloque 6 (T-21): antes este arnes vivia SOLO en el scratchpad efimero de
// cada sesion - cada continuacion de este proyecto lo reconstruia desde cero (cientos de lineas
// re-escritas, decenas de bugs ya resueltos antes vueltos a pisar sin querer). Ahora es un
// proyecto real y permanente del propio repo (`TerrasavrNative.App.Tests`, en `TerrasavrNative.
// slnx`) - se compila y ejecuta con `dotnet run --project TerrasavrNative.App.Tests` desde la
// raiz del repo, sin depender de ninguna ruta de scratchpad.
//
// NO es un proyecto xunit (`[Fact]`/`Assert`) a proposito: gran parte de lo que verifica es
// VISUAL (capturas reales de pantalla que hace falta mirar, no solo un booleano pasa/falla) -
// un runner de tests headless nunca podria juzgar eso. Sigue siendo un programa de consola con
// un Main() real que monta una MainWindow real, coloca datos reales, interactua via UI
// Automation real (clics/teclado/scroll reales, nunca simulados a medias) y deja capturas +
// lineas "esperado X, obtenido Y" en stdout para revisar a mano. Ver CLAUDE.md ("Verdad del
// entorno WPF... un arnes que instancia TerrasavrNative.App.App y llama a Run() crea una
// SEGUNDA MainWindow fantasma") para el porque de construir un Application en blanco en vez de
// la App real de App.xaml.
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using TerrasavrNative.App;
using TerrasavrNative.App.Controls;
using TerrasavrNative.App.Converters;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Verificacion real de T-12 (auditoria de Opus, Bloque 3): sesion local normal (esta
        // maquina, sin RDP) -> false; con la variable de entorno puesta -> true. El propio
        // OnStartup de App.xaml.cs nunca se ejecuta en este arnes (crea un Application a pelo),
        // por eso ShouldForceSoftwareRendering() se probo aparte, como metodo publico.
        Console.WriteLine($"T12-RENDER: local sin RDP -> ShouldForceSoftwareRendering()={TerrasavrNative.App.App.ShouldForceSoftwareRendering()} (esperado False en esta maquina)");
        Environment.SetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER", "1");
        Console.WriteLine($"T12-RENDER: con TERRAKEEP_FORCE_SOFTWARE_RENDER=1 -> ShouldForceSoftwareRendering()={TerrasavrNative.App.App.ShouldForceSoftwareRendering()} (esperado True)");
        Environment.SetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER", null);

        // El arnes NUNCA llama a app.Run() (pumpea a mano con DoEvents en su lugar) - la app
        // REAL (StartupUri en App.xaml) si lo hace, y Application.Run() es quien instala de
        // verdad el DispatcherSynchronizationContext que `await Task.Run(...)` necesita para
        // reanudar en el hilo de UI (ver X-7/T-13 mas abajo). Se instala aqui a mano, mismo
        // efecto real que la app real, para probar el camino async de verdad y no solo el
        // artefacto del propio arnes.
        System.Threading.SynchronizationContext.SetSynchronizationContext(
            new System.Windows.Threading.DispatcherSynchronizationContext());

        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/TerrasavrNative.App;component/Styles/Theme.xaml")
        });
        app.Resources["NullToVis"] = new NullToVisibilityConverter();
        app.Resources["NullToCollapsed"] = new NullToCollapsedConverter();
        app.Resources["EmptyToCollapsed"] = new EmptyToCollapsedConverter();
        app.Resources["CountToVis"] = new CountToVisibilityConverter();
        app.Resources["InverseBoolToVis"] = new InverseBooleanToVisibilityConverter();
        app.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine("DISPATCHER-EXCEPTION: " + e.Exception);
            e.Handled = true;
        };

        var window = new MainWindow();
        app.MainWindow = window;
        window.Show();
        DoEvents();
        // Verificacion real de T-3: si la sesion ANTERIOR guardo window.json, el constructor de
        // MainWindow (WindowPlacementService.Apply) ya deberia haber restaurado ese tamaño real
        // ANTES de Show() - se comprueba aqui, lo antes posible.
        Console.WriteLine($"T3-RESTAURADO: Left={window.Left} Top={window.Top} Width={window.Width} Height={window.Height}");

        var hwnd = new WindowInteropHelper(window).Handle;
        var root = AutomationElement.FromHandle(hwnd);

        var character = new PlrCharacter
        {
            Name = "UIA-Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts =
            [
                PlrLoadout.CreateEmpty(isPrimary: false),
                PlrLoadout.CreateEmpty(isPrimary: false),
                PlrLoadout.CreateEmpty(isPrimary: false),
            ],
        };
        string tempPlr = Path.Combine(Path.GetTempPath(), "uia-harness-test.plr");
        File.WriteAllBytes(tempPlr, PlrFile.Write(character));

        var vm = (MainViewModel)window.DataContext;

        // Verificacion real de I-1 (auditoria de Opus, Bloque 2): HomeViewModel escanea SOLO
        // al construirse (constructor de MainViewModel, antes de este punto) la carpeta REAL de
        // tModLoader de esta maquina - sin sintetizar nada, se comprueban los .plr reales que
        // ya existen ahi.
        Console.WriteLine($"HOME-SCAN: {vm.Home.Characters.Count} personaje(s) encontrado(s) en la carpeta real");
        foreach (var entry in vm.Home.Characters)
            Console.WriteLine($"  - {entry.Name} | {entry.DifficultyLabel} | Calamity={entry.IsCalamity} | {entry.LastModifiedText}");
        {
            var rtbHome = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbHome.Render(window);
            var encHome = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encHome.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHome));
            string shotPathHome = Path.Combine(AppContext.BaseDirectory, "inicio-lanzador.png");
            using (var fs = File.Create(shotPathHome)) encHome.Save(fs);
            Console.WriteLine($"  Captura -> {shotPathHome}");
        }
        if (vm.Home.Characters.Count > 0)
        {
            var first = vm.Home.Characters[0];
            vm.Home.OpenCommand.Execute(first);
            DoEvents();
            DoEvents();
            Console.WriteLine($"HOME-OPEN: click en '{first.Name}' -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), CharacterName={vm.CharacterName}, IsCharacterLoaded={vm.IsCharacterLoaded}");
        }

        try
        {
            var swChar = System.Diagnostics.Stopwatch.StartNew();
            vm.LoadFromPath(tempPlr);
            swChar.Stop();
            Console.WriteLine($"MEDICION-PERSONAJE: {swChar.ElapsedMilliseconds}ms");
            Console.WriteLine("LOAD: OK - " + vm.StatusMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("LOAD-EXCEPTION: " + ex);
        }

        // Verificacion real de N-1 (auditoria de Opus, Bloque 2): la cabecera global debe verse
        // IGUAL en una pestaña que no es Personaje (aqui, Builds=indice 2) - antes el nombre/
        // dificultad/Guardar solo existian dentro de Personaje.
        vm.SelectedTabIndex = 2; // Builds
        DoEvents();
        DoEvents();
        var headerName = root.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
            new PropertyCondition(AutomationElement.NameProperty, "UIA-Test")));
        Console.WriteLine($"CABECERA-GLOBAL (en Builds): nombre real encontrado={headerName != null} (esperado True)");
        {
            var rtbHeader = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbHeader.Render(window);
            var encHeader = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encHeader.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHeader));
            string shotPathHeader = Path.Combine(AppContext.BaseDirectory, "cabecera-global-en-builds.png");
            using (var fs = File.Create(shotPathHeader)) encHeader.Save(fs);
            Console.WriteLine($"  Captura -> {shotPathHeader}");
        }
        // Guardar desde una pestaña que NO es Personaje via UI Automation real (boton real de
        // la cabecera, no vm.SaveCommand.Execute a pelo) - confirma que el banner de
        // confirmacion (movido a nivel raiz en N-1) se ve tambien fuera de Personaje.
        var saveButtonHeader = root.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
            new PropertyCondition(AutomationElement.NameProperty, "Guardar")));
        if (saveButtonHeader != null && saveButtonHeader.TryGetCurrentPattern(InvokePattern.Pattern, out var saveInvokePat))
        {
            ((InvokePattern)saveInvokePat).Invoke();
            DoEvents();
            DoEvents();
            Console.WriteLine($"GUARDAR-DESDE-BUILDS: SaveConfirmationVisible={vm.SaveConfirmationVisible} (esperado True), StatusMessage={vm.StatusMessage}");
        }
        else Console.WriteLine("GUARDAR-DESDE-BUILDS: boton 'Guardar' NO-FOUND en la cabecera");

        vm.SelectedTabIndex = 1; // Personaje
        vm.PersonajeInnerTabIndex = 0; // Objetos
        DoEvents();
        DoEvents();

        // Coloca objetos reales en varios slots (id 1 = Iron Pickaxe, id 2 = Iron Axe...) para
        // que la rejilla compacta tenga iconos de verdad que medir/organizar, no solo huecos
        // vacios - el caso mas exigente para SlotGridPanel (celdas ricas de verdad, cantidades
        // >1 visibles).
        if (vm.InventoryContainer != null)
        {
            for (int i = 0; i < 12 && i < vm.InventoryContainer.Slots.Count; i++)
                vm.InventoryContainer.Slots[i].PlaceItem(i + 1);
            vm.InventoryContainer.Slots[0].Count = 99;

            // Verificacion real de T-14 (auditoria de Opus, Bloque 3): PlaceItem es una edicion
            // real de usuario (carga ya termino, _suppressDirty=false) - debe disparar el
            // flash de inmediato, y auto-apagarse solo pasados los 450ms reales.
            bool justEditedInmediato = vm.InventoryContainer.Slots[1].JustEdited;
            DoEvents();
            // Captura util de verdad: selecciona la sub-pestaña real "Inventario" primero (si
            // no, la captura cae en "Equipamiento", la sub-pestaña por defecto, y no se ve
            // ningun slot de Inventario real).
            var invTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Inventario")));
            if (invTab != null && invTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var invSelPat))
                ((SelectionItemPattern)invSelPat).Select();
            DoEvents();
            vm.InventoryContainer.Slots[2].PlaceItem(3); // re-dispara el flash ya en la pestaña visible correcta
            DoEvents();
            var rtbFlash = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFlash.Render(window);
            var encFlash = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFlash.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlash));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "flash-edicion.png"))) encFlash.Save(fs);
            System.Threading.Thread.Sleep(600);
            DoEvents();
            bool justEditedTrasEspera = vm.InventoryContainer.Slots[1].JustEdited;
            Console.WriteLine($"T14-FLASH: JustEdited inmediatamente tras PlaceItem={justEditedInmediato} (esperado True), tras 600ms={justEditedTrasEspera} (esperado False)");
        }
        if (vm.StorageGroup != null)
            for (int i = 0; i < 15 && i < vm.StorageGroup.Current.Slots.Count; i++)
                vm.StorageGroup.Current.Slots[i].PlaceItem(i + 1);

        // Objeto CON prefijo real asignado en Equipamiento, para probar de verdad la
        // correccion 1 del usuario ("que allí aparezca el prefijo que tiene asignado") - no
        // solo un objeto sin prefijo, que no habria distinguido el bug de un falso OK.
        if (vm.EquipmentGroup != null)
        {
            var slot = vm.EquipmentGroup.Current.Slots[0];
            // Sexta pasada: el slot 0 ahora es ArmorHead real, una espada (id 3) ya no
            // encajaria - Casco Shroomite (1546, real, valido) en su lugar.
            slot.PlaceItem(1546);
            slot.SetPrefix(ItemPrefix.Vanilla(1));
            Console.WriteLine($"Equipamiento slot0: DisplayName={slot.DisplayName} PrefixDisplay='{slot.PrefixDisplay}'");

            // Peticion 1 (pregunta a Opus, cuarta pasada): accesorio real con tooltip
            // descriptivo real (Warrior Emblem, id 490, +15% daño cuerpo a cuerpo).
            var accessorySlot = vm.EquipmentGroup.Current.Slots[3];
            accessorySlot.PlaceItem(490);
            Console.WriteLine($"Accesorio 490 (Warrior Emblem) StatsTooltip:\n{accessorySlot.StatsTooltip}");

            // Set completo real de Shroomite (1546 casco, 1549 peto, 1550 grebas) - confirma
            // la seccion de bonus de set en el tooltip de UNA sola pieza.
            vm.EquipmentGroup.Current.Slots[0].PlaceItem(1546);
            vm.EquipmentGroup.Current.Slots[1].PlaceItem(1549);
            vm.EquipmentGroup.Current.Slots[2].PlaceItem(1550);
            Console.WriteLine($"Casco Shroomite (1546) StatsTooltip:\n{vm.EquipmentGroup.Current.Slots[0].StatsTooltip}");

            // Octava pasada: el usuario reporta que el solape sigue pasando y que el fondo
            // fantasma de armadura desaparece con un personaje REAL - hasta ahora solo se
            // habian llenado 4 de los 7 accesorios reales. Rellenar los 7 (indices 3-9) para
            // reproducir el caso real completo antes de asumir nada.
            for (int i = 4; i <= 9; i++) vm.EquipmentGroup.Current.Slots[i].PlaceItem(490);
            DoEvents();
            DoEvents();
            DoEvents();

            var rtbArmadura = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbArmadura.Render(window);
            var encArmadura = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encArmadura.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbArmadura));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "armadura-7-accesorios.png"))) encArmadura.Save(fs);
            Console.WriteLine("Captura Armadura con 7 accesorios reales -> armadura-7-accesorios.png");
        }
        // Sexta pasada: restricciones reales de slot (SlotKind) + iconos fantasma + 6º/7º
        // accesorio. Ids reales conocidos por tipo (extraidos de vanilla_slot_kind.json):
        // 84=Gancho de escalada(Hook), 1914=Campanas de reno(Mount), 2191=Jaula de raton
        // (Cart/vagoneta), 603=Zanahoria(VanityPet), 425=Campana de hada(LightPet),
        // 1007=Tinte rojo(Dye), 40=Flecha de madera(Ammo), 71=Moneda de cobre(Coin).
        if (vm.MountsContainer != null && vm.CoinsContainer != null && vm.AmmoContainer != null && vm.DyesContainer != null)
        {
            var hookSlot = vm.MountsContainer.Slots[4]; // orden real: Pet/LightPet/Cart/Mount/Hook
            Console.WriteLine($"AcceptsItem: hookSlot.AcceptsItem(84 Gancho)={hookSlot.AcceptsItem(84)} (esperado True)");
            Console.WriteLine($"AcceptsItem: hookSlot.AcceptsItem(1 Pico de hierro)={hookSlot.AcceptsItem(1)} (esperado False)");
            hookSlot.PlaceItem(1); // objeto invalido - debe rechazarse
            Console.WriteLine($"Tras PlaceItem(1) invalido: hookSlot.IsEmpty={hookSlot.IsEmpty} RejectionMessage='{hookSlot.RejectionMessage}' (esperado IsEmpty=True, mensaje real)");
            hookSlot.PlaceItem(84); // objeto valido real
            Console.WriteLine($"Tras PlaceItem(84 Gancho) valido: DisplayName={hookSlot.DisplayName} RejectionMessage='{hookSlot.RejectionMessage}' (esperado colocado, sin mensaje)");

            var coinSlot = vm.CoinsContainer.Slots[0];
            coinSlot.PlaceItem(1);
            Console.WriteLine($"Moneda: PlaceItem(1) invalido -> IsEmpty={coinSlot.IsEmpty} RejectionMessage='{coinSlot.RejectionMessage}'");
            coinSlot.PlaceItem(71);
            Console.WriteLine($"Moneda: PlaceItem(71 real) -> DisplayName={coinSlot.DisplayName}");

            var mountSlot = vm.MountsContainer.Slots[3];
            mountSlot.PlaceItem(1914);
            Console.WriteLine($"Montura: PlaceItem(1914 real) -> DisplayName={mountSlot.DisplayName}");
            var cartSlot = vm.MountsContainer.Slots[2];
            cartSlot.PlaceItem(2191);
            Console.WriteLine($"Vagoneta: PlaceItem(2191 real) -> DisplayName={cartSlot.DisplayName}");
            var petSlot = vm.MountsContainer.Slots[0];
            petSlot.PlaceItem(603);
            Console.WriteLine($"Mascota: PlaceItem(603 real) -> DisplayName={petSlot.DisplayName}");
            var lightPetSlot = vm.MountsContainer.Slots[1];
            lightPetSlot.PlaceItem(425);
            Console.WriteLine($"MascotaLuz: PlaceItem(425 real) -> DisplayName={lightPetSlot.DisplayName}");

            var dyeSlot = vm.DyesContainer.Slots[0];
            dyeSlot.PlaceItem(1007);
            Console.WriteLine($"Tinte: PlaceItem(1007 real) -> DisplayName={dyeSlot.DisplayName}");

            var ammoSlot = vm.AmmoContainer.Slots[0];
            ammoSlot.PlaceItem(40);
            Console.WriteLine($"Municion: PlaceItem(40 real) -> DisplayName={ammoSlot.DisplayName}");

            // Calamity SIEMPRE se acepta (regla obligatoria, consulta a Opus) - un id sintetico
            // cualquiera (20000000+) debe pasar cualquier restriccion sin consultar el catalogo.
            Console.WriteLine($"Calamity siempre pasa: hookSlot.AcceptsItem(20000000)={hookSlot.AcceptsItem(20000000)} (esperado True)");
        }

        if (vm.EquipmentGroup != null)
        {
            var slot8 = vm.EquipmentGroup.EquippedItems.Slots[8];
            var slot9 = vm.EquipmentGroup.EquippedItems.Slots[9];
            var slot3 = vm.EquipmentGroup.EquippedItems.Slots[3];
            Console.WriteLine($"6º accesorio (indice 8): IsExpertAccessorySlot={slot8.IsExpertAccessorySlot} IsMasterAccessorySlot={slot8.IsMasterAccessorySlot} GhostIconPath={slot8.GhostIconPath}");
            Console.WriteLine($"7º accesorio (indice 9): IsExpertAccessorySlot={slot9.IsExpertAccessorySlot} IsMasterAccessorySlot={slot9.IsMasterAccessorySlot} GhostIconPath={slot9.GhostIconPath}");
            Console.WriteLine($"3er accesorio normal (indice 3): IsExpertAccessorySlot={slot3.IsExpertAccessorySlot} IsMasterAccessorySlot={slot3.IsMasterAccessorySlot} (esperado False/False)");
            var headSlot = vm.EquipmentGroup.EquippedItems.Slots[0];
            Console.WriteLine($"Slot cabeza (indice 0): GhostIconPath={headSlot.GhostIconPath} (esperado .../armor_head.png)");

            // Ampliacion pedida por el usuario a mitad de ronda ("las armaduras y los
            // accesorios, si los quiero arriba"): armadura/accesorios tambien restringidos.
            Console.WriteLine($"AcceptsItem: headSlot(cabeza).AcceptsItem(1546 Casco Shroomite real)={headSlot.AcceptsItem(1546)} (esperado True)");
            Console.WriteLine($"AcceptsItem: headSlot(cabeza).AcceptsItem(3 Espada, invalido)={headSlot.AcceptsItem(3)} (esperado False)");
            headSlot.PlaceItem(3); // arma en slot de cabeza - debe rechazarse
            Console.WriteLine($"Tras PlaceItem(3 espada) en slot cabeza: DisplayName={headSlot.DisplayName} RejectionMessage='{headSlot.RejectionMessage}' (esperado: sigue siendo el casco Shroomite, con mensaje)");
            var accSlot = vm.EquipmentGroup.EquippedItems.Slots[3];
            Console.WriteLine($"AcceptsItem: accSlot(accesorio).AcceptsItem(490 Warrior Emblem real)={accSlot.AcceptsItem(490)} (esperado True)");
            Console.WriteLine($"AcceptsItem: accSlot(accesorio).AcceptsItem(1546 Casco, invalido)={accSlot.AcceptsItem(1546)} (esperado False)");

            // Bug real reportado 2-sep-2026 ("la armadura me deja colocarla en los huecos de
            // accesorios"): la causa real era que Calamity SIEMPRE pasaba la restriccion,
            // tambien para armadura/accesorio (donde SI hay un campo real, Category, a
            // diferencia de ammo/mountType/etc). id sintetico 20000243 = armadura real
            // (Armor/Aerospec), 20000000 = accesorio real (Accessories) - ver catalog.json.
            int calamityArmorId = 20000243, calamityAccessoryId = 20000000;
            Console.WriteLine($"Calamity armadura en slot cabeza: headSlot.AcceptsItem({calamityArmorId})={headSlot.AcceptsItem(calamityArmorId)} (esperado True)");
            Console.WriteLine($"Calamity armadura en slot accesorio: accSlot.AcceptsItem({calamityArmorId})={accSlot.AcceptsItem(calamityArmorId)} (esperado False - este era el bug)");
            Console.WriteLine($"Calamity accesorio en slot accesorio: accSlot.AcceptsItem({calamityAccessoryId})={accSlot.AcceptsItem(calamityAccessoryId)} (esperado True)");
            Console.WriteLine($"Calamity accesorio en slot cabeza: headSlot.AcceptsItem({calamityAccessoryId})={headSlot.AcceptsItem(calamityAccessoryId)} (esperado False)");

            // Bloque 1 de la auditoria de Opus (E-1): coloca un accesorio REAL de Calamity,
            // equipado (isEquipped=true siempre en EquipmentGroupViewModel), para confirmar de
            // verdad con una captura que el punto rojo y la mancha verde de "equipado" se ven
            // A LA VEZ - antes de este arreglo el verde tapaba el rojo por completo (ver el
            // comentario real en SlotCompactTemplate, MainWindow.xaml).
            accSlot.PlaceItem(calamityAccessoryId);
            DoEvents(); DoEvents();
            Console.WriteLine($"E-1: accSlot tras colocar Calamity equipado -> IsCalamity={accSlot.IsCalamity} IsEquipped={accSlot.IsEquipped} IsEmpty={accSlot.IsEmpty} (los 3 deben coexistir sin que ninguno tape al otro visualmente)");
        }

        // Rellena TAMBIEN los 4 contenedores de los laterales fusionados (Mascota/Montura/
        // Gancho, Tinte, Monedas, Municion) con objetos reales - sin esto la medicion de
        // celdas de la fusion de Equipamiento (mas abajo) mide huecos vacios, no iconos reales,
        // y no puede confirmar ni desmentir mi correccion a la aritmetica de Opus sobre si el
        // lateral izquierdo (2 grupos de 5 apilados) necesita de verdad el ScrollViewer de
        // seguridad.
        // Sexta pasada: estos 5 slots ahora estan restringidos por SlotKind - ids reales por
        // indice (Pet/LightPet/Cart/Mount/Hook), ya colocados arriba por el bloque de pruebas
        // de restriccion (esto solo confirma que sigue igual, PlaceItem con el mismo id real
        // es un no-op idempotente).
        if (vm.MountsContainer != null)
        {
            int[] realIds = [603, 425, 2191, 1914, 84];
            for (int i = 0; i < vm.MountsContainer.Slots.Count && i < realIds.Length; i++) vm.MountsContainer.Slots[i].PlaceItem(realIds[i]);
        }
        if (vm.DyesContainer != null)
        {
            int[] realDyeIds = [1007, 1008, 1009, 1010, 1011];
            for (int i = 0; i < vm.DyesContainer.Slots.Count && i < realDyeIds.Length; i++) vm.DyesContainer.Slots[i].PlaceItem(realDyeIds[i]);
        }
        if (vm.CoinsContainer != null)
            for (int i = 0; i < vm.CoinsContainer.Slots.Count; i++) vm.CoinsContainer.Slots[i].PlaceItem(71 + i); // 71 = Copper Coin
        if (vm.AmmoContainer != null)
            for (int i = 0; i < vm.AmmoContainer.Slots.Count; i++) vm.AmmoContainer.Slots[i].PlaceItem(40 + i); // 40 = Wooden Arrow
        DoEvents();
        DoEvents();

        Console.WriteLine($"InventoryContainer.Slots={vm.InventoryContainer?.Slots.Count} Columns={vm.InventoryContainer?.Columns}");
        Console.WriteLine($"StorageGroup.Current={vm.StorageGroup?.Current.DisplayName} Slots={vm.StorageGroup?.Current.Slots.Count} Options={vm.StorageGroup?.Options.Count}");
        Console.WriteLine($"MountsContainer.Slots={vm.MountsContainer?.Slots.Count} Columns={vm.MountsContainer?.Columns}");
        Console.WriteLine($"DyesContainer.Slots={vm.DyesContainer?.Slots.Count} Columns={vm.DyesContainer?.Columns}");
        Console.WriteLine($"CoinsContainer.Slots={vm.CoinsContainer?.Slots.Count} Columns={vm.CoinsContainer?.Columns}");
        Console.WriteLine($"AmmoContainer.Slots={vm.AmmoContainer?.Slots.Count} Columns={vm.AmmoContainer?.Columns}");
        Console.WriteLine($"EquipmentGroup.Current(inicial)={vm.EquipmentGroup?.Current.DisplayName} Columns={vm.EquipmentGroup?.Current.Columns} (esperado: Columns=5)");

        string[] tabNames = ["Equipamiento", "Inventario", "Almacenes"]; // quinta pasada: Monturas/Monedas ya no son pestañas, se fusionaron dentro de Equipamiento
        foreach (var name in tabNames)
        {
            try
            {
                var tabCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                    new PropertyCondition(AutomationElement.NameProperty, name));
                var tabItem = root.FindFirst(TreeScope.Descendants, tabCondition);
                if (tabItem == null) { Console.WriteLine($"TAB {name}: NOT-FOUND"); continue; }

                if (tabItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pat))
                    ((SelectionItemPattern)pat).Select();
                else
                    Console.WriteLine($"TAB {name}: NO-SELECTIONITEMPATTERN");

                DoEvents();
                DoEvents();
                DoEvents();

                var images = root.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image));
                int buttonCount = root.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)).Count;
                // Peticion 2 (pregunta a Opus, cuarta pasada, "los iconos enormes... me
                // gustaria que se vieran igual que la rejilla de inventario"): ancho real del
                // primer icono renderizado - antes de ReferenceColumns, Equipamiento salia
                // mucho mas grande que Inventario en la misma ventana.
                string firstImageWidth = images.Count > 0 ? images[0].Current.BoundingRectangle.Width.ToString("0.#") : "n/a";
                Console.WriteLine($"TAB {name}: selected OK, images={images.Count} buttons={buttonCount} firstImageWidth={firstImageWidth}px");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TAB {name}: EXCEPTION - {ex}");
            }
        }

        // Medicion real de la fusion de Equipamiento (quinta pasada, laterales Monturas/Monedas).
        // Vuelve a "Equipamiento" y agrupa los Image reales por posicion X en 3 clusters
        // (lateral izquierdo/centro/lateral derecho, mismo orden que las 3 columnas del Grid) -
        // para contrastar con numero mi correccion a la aritmetica de Opus ((300-16)/5≈56.8px
        // solo contaba UN grupo de 5, no los 2 apilados que el lateral izquierdo necesita de
        // verdad) con el tamaño de celda REAL renderizado, no calculado a mano.
        try
        {
            var equipTabAgain = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Equipamiento")));
            if (equipTabAgain != null && equipTabAgain.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var equipAgainPat))
                ((SelectionItemPattern)equipAgainPat).Select();
            DoEvents();
            DoEvents();

            var equipImages = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image));
            var rects = equipImages.Cast<AutomationElement>()
                .Select(el => el.Current.BoundingRectangle)
                .Where(r => !r.IsEmpty && r.Width > 0)
                .OrderBy(r => r.X).ToList();
            Console.WriteLine($"Equipamiento fusionado: {rects.Count} iconos reales renderizados.");
            if (rects.Count > 0)
            {
                double minX = rects.Min(r => r.X), maxX = rects.Max(r => r.X);
                double thirdW = (maxX - minX) / 3.0;
                var left = rects.Where(r => r.X < minX + thirdW).ToList();
                var mid = rects.Where(r => r.X >= minX + thirdW && r.X < minX + 2 * thirdW).ToList();
                var right = rects.Where(r => r.X >= minX + 2 * thirdW).ToList();
                Console.WriteLine($"  Lateral izq (Equipo/Tinte): n={left.Count} anchoCelda~{(left.Count > 0 ? left.Average(r => r.Width) : 0):0.#}px minH={(left.Count > 0 ? left.Min(r => r.Height) : 0):0.#}px");
                Console.WriteLine($"  Centro (Equipamiento 5x2): n={mid.Count} anchoCelda~{(mid.Count > 0 ? mid.Average(r => r.Width) : 0):0.#}px");
                Console.WriteLine($"  Lateral der (Monedas/Municion): n={right.Count} anchoCelda~{(right.Count > 0 ? right.Average(r => r.Width) : 0):0.#}px");
            }

            var scrollers = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ScrollBar));
            Console.WriteLine($"  ScrollBars visibles en Equipamiento: {scrollers.Count} (>0 = el ScrollViewer de seguridad del lateral izquierdo esta actuando de verdad)");

            // Diagnostico directo del arbol visual real (whitebox, no UI Automation) - para
            // confirmar de verdad que tamaño (finalSize) recibe cada SlotGridPanel.ArrangeOverride
            // y si mi offset de centrado se esta aplicando, en vez de seguir adivinando a partir
            // de una captura de pantalla.
            void WalkVisual(System.Windows.DependencyObject d, int depth)
            {
                if (d is TerrasavrNative.App.Controls.SlotGridPanel sgp)
                {
                    Console.WriteLine($"  SlotGridPanel real: ActualWidth={sgp.ActualWidth:0.#} ActualHeight={sgp.ActualHeight:0.#} Children={sgp.Children.Count}");
                    if (sgp.Children.Count > 0)
                    {
                        var first = sgp.Children[0] as System.Windows.UIElement;
                        if (first != null)
                        {
                            var pos = first.TransformToAncestor(sgp).Transform(new System.Windows.Point(0, 0));
                            Console.WriteLine($"    Primer hijo: posicion local dentro del panel = ({pos.X:0.#},{pos.Y:0.#})");
                        }
                    }
                    // Cadena de ancestros real (ScrollViewer/ItemsControl/ContentControl/
                    // StackPanel/Border) para ver EN QUE ESLABON se estrecha el ancho real,
                    // en vez de seguir adivinando por que el ActualWidth del panel no coincide
                    // con lo que parecia en la captura.
                    var anc = System.Windows.Media.VisualTreeHelper.GetParent(sgp);
                    int hops = 0;
                    while (anc != null && hops < 20)
                    {
                        if (anc is System.Windows.FrameworkElement fe)
                            Console.WriteLine($"    ^ {fe.GetType().Name}: ActualWidth={fe.ActualWidth:0.#} HorizontalAlignment={fe.HorizontalAlignment}");
                        anc = System.Windows.Media.VisualTreeHelper.GetParent(anc);
                        hops++;
                    }
                }
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                    WalkVisual(System.Windows.Media.VisualTreeHelper.GetChild(d, i), depth + 1);
            }
            WalkVisual(window, 0);

            // El agrupado por tercios de X es impreciso (mezcla iconos ajenos a la rejilla,
            // ej. pildoras Loadout/Vista) - captura real de pixeles vía RenderTargetBitmap
            // (visual tree real de WPF, no una captura de pantalla que dependeria de que la
            // ventana este realmente visible/no tapada) para confirmar de un vistazo que no
            // hay solapamiento ni celdas rotas, sin adivinar a partir de numeros agregados.
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string shotPath = Path.Combine(AppContext.BaseDirectory, "equipamiento-fusionado.png");
            using (var fs = File.Create(shotPath)) encoder.Save(fs);
            Console.WriteLine($"  Captura real guardada en: {shotPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EQUIPAMIENTO-MEDICION-EXCEPTION: " + ex);
        }

        // Pildora de "Almacenes": cambiar de Banco a Fragua del Defensor y confirmar que
        // StorageGroup.Current cambia de verdad (no solo el label del boton). TabControl solo
        // mantiene vivo el contenido de la pestaña SELECCIONADA - hay que volver a "Almacenes"
        // primero (el bucle de arriba la dejo en "Almacenes", ultima pestaña real ahora).
        try
        {
            var almacenesTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Almacenes")));
            if (almacenesTab != null && almacenesTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPat))
                ((SelectionItemPattern)selPat).Select();
            DoEvents();
            DoEvents();

            // Auditoria de Opus, A-1: el Content del boton ya no es el Label plano ("Fragua del
            // Defensor") sino el DisplayLabel con el contador en vivo ("Fragua del Defensor
            // (0/40)") - la busqueda exacta por NameProperty dejo de encontrarlo. StartsWith
            // sobre todos los botones reales sigue siendo real (no ignora el contador, solo no
            // exige adivinar el numero exacto de antemano).
            var forgeButton = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                .Cast<AutomationElement>().FirstOrDefault(b => b.Current.Name.StartsWith("Fragua del Defensor"));
            if (forgeButton != null && forgeButton.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePat))
            {
                ((InvokePattern)invokePat).Invoke();
                DoEvents();
                DoEvents();
                Console.WriteLine($"PILDORA Almacenes -> Current={vm.StorageGroup?.Current.DisplayName} (esperado: Fragua del Defensor)");
            }
            else Console.WriteLine("PILDORA Almacenes: boton 'Fragua del Defensor' NO-FOUND");
        }
        catch (Exception ex)
        {
            Console.WriteLine("PILDORA-EXCEPTION: " + ex);
        }

        // Equipamiento: cambiar de pildora "Armadura" a "Vanidad" via UI Automation real (no
        // solo el ViewModel) y confirmar que EquipmentGroup.Current cambia de verdad - prueba
        // real del ContentControl+ContainerTabTemplate nuevo (antes Viewbox+ItemsControl a
        // medida).
        try
        {
            var equipTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Equipamiento")));
            if (equipTab != null && equipTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var equipSelPat))
                ((SelectionItemPattern)equipSelPat).Select();
            DoEvents();
            DoEvents();

            var vanidadButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Vanidad")));
            if (vanidadButton != null && vanidadButton.TryGetCurrentPattern(InvokePattern.Pattern, out var vanidadPat))
            {
                ((InvokePattern)vanidadPat).Invoke();
                DoEvents();
                DoEvents();
                Console.WriteLine($"PILDORA Equipamiento -> Current={vm.EquipmentGroup?.Current.DisplayName} Columns={vm.EquipmentGroup?.Current.Columns} (esperado: Equipo puesto - vanidad, Columns=5)");
            }
            else Console.WriteLine("PILDORA Equipamiento: boton 'Vanidad' NO-FOUND");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EQUIPAMIENTO-PILDORA-EXCEPTION: " + ex);
        }

        // Libreria: poblar Results de verdad (busqueda real) para ejercitar la tarjeta nueva
        // (tooltip compuesto/hover/TextTrimming) sin excepcion.
        try
        {
            vm.IsLibraryCollapsed = false; // desplegada por defecto ahora - hace falta para que las tarjetas se rendericen
            vm.Library.SearchText = "Sword";
            DoEvents();
            DoEvents();
            int libraryCards = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)).Count;
            Console.WriteLine($"LIBRERIA busqueda 'Sword': Results.Count={vm.Library.Results.Count} (tarjetas renderizadas sin excepcion)");
        }
        catch (Exception ex)
        {
            Console.WriteLine("LIBRERIA-EXCEPTION: " + ex);
        }

        // Toggle biblioteca (plegar/desplegar) para confirmar que el binding real funciona.
        // Octava pasada: medir la ALTURA REAL de la fila (no solo el booleano) - el bug
        // reportado era precisamente que el booleano cambiaba pero el espacio real no se
        // liberaba.
        try
        {
            Console.WriteLine($"IsLibraryCollapsed antes={vm.IsLibraryCollapsed}");

            System.Windows.Controls.Grid? libraryRowGrid = null;
            void FindLibraryGrid(System.Windows.DependencyObject d)
            {
                if (d is System.Windows.Controls.Grid g && g.RowDefinitions.Count == 2 && g.RowDefinitions[1].MaxHeight == 460)
                    libraryRowGrid = g;
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++) FindLibraryGrid(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
            }
            FindLibraryGrid(window);
            double AltoFilaLibreria() => libraryRowGrid?.RowDefinitions[1].ActualHeight ?? -1;

            DoEvents(); DoEvents();
            Console.WriteLine($"Alto real fila Libreria (desplegada, IsLibraryCollapsed={vm.IsLibraryCollapsed})={AltoFilaLibreria():0.#}px");

            vm.ToggleLibraryCollapsedCommand.Execute(null);
            DoEvents(); DoEvents(); DoEvents();
            Console.WriteLine($"IsLibraryCollapsed despues={vm.IsLibraryCollapsed}");
            Console.WriteLine($"Alto real fila Libreria (colapsada)={AltoFilaLibreria():0.#}px (esperado: solo la barra del boton, ~30-40px, no 150-238)");

            vm.ToggleLibraryCollapsedCommand.Execute(null); // vuelve a desplegar para el resto de pruebas
            DoEvents(); DoEvents();
        }
        catch (Exception ex)
        {
            Console.WriteLine("LIBRARY-TOGGLE-EXCEPTION: " + ex);
        }

        // Rework de Buffs (pregunta a Opus sobre el diseño, cuarta pasada). Fase 2: navegar a
        // la pestaña real, desplegar la Libreria de buffs, confirmar el arbol REAL (6
        // categorias + Indice + Calamity), elegir una categoria real, pedir "Elegir..." sobre
        // un slot vacio (mismo comando real que dispara el doble clic/ChooseFromLibraryCommand)
        // y colocar un buff real pulsando "Colocar" via UI Automation real.
        try
        {
            Console.WriteLine($"Buffs.Container.Slots.Count={vm.Buffs.Container?.Slots.Count} (esperado: 44, version 279 >= 269)");

            var buffsTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Buffs")));
            if (buffsTab != null && buffsTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buffsSelPat))
                ((SelectionItemPattern)buffsSelPat).Select();
            DoEvents();
            DoEvents();

            Console.WriteLine($"BuffLibrary.RootCategories: {string.Join(", ", vm.BuffLibrary.RootCategories.Select(c => c.Name))}");
            Console.WriteLine($"(esperado: Utilidad/Offensivo/Defensivo/Special/Mascota/Negativo/Indice/Calamity (mod), 8 raices reales)");

            var toggleButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Librería de buffs")));
            if (toggleButton != null && toggleButton.TryGetCurrentPattern(InvokePattern.Pattern, out var togglePat))
                ((InvokePattern)togglePat).Invoke();
            DoEvents();
            DoEvents();
            Console.WriteLine($"IsBuffLibraryCollapsed tras pulsar 'Librería de buffs'={vm.IsBuffLibraryCollapsed}");

            var utilidad = vm.BuffLibrary.RootCategories.FirstOrDefault(c => c.Name.StartsWith("Utilidad"));
            if (utilidad != null) vm.BuffLibrary.SelectCategoryCommand.Execute(utilidad);
            DoEvents();
            DoEvents();
            Console.WriteLine($"Categoria 'Utilidad' seleccionada -> Results.Count={vm.BuffLibrary.Results.Count} (esperado: 17)");

            // Pide "Elegir..." sobre el primer slot vacio real (mismo comando real que dispara
            // el doble clic/menu contextual de la rejilla de arriba).
            var emptySlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot != null) emptySlot.ChooseFromLibraryCommand.Execute(null);
            DoEvents();
            DoEvents();
            Console.WriteLine($"BuffLibrary.IsPicking={vm.BuffLibrary.IsPicking} PickTarget coincide={ReferenceEquals(vm.BuffLibrary.PickTarget, emptySlot)}");

            // Buff real conocido: id 1 = Obsidian Skin, esta en la categoria real Utilidad -
            // pulsa el boton "Colocar" real de esa tarjeta via UI Automation real.
            var placeButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Colocar")));
            if (placeButton != null && placeButton.TryGetCurrentPattern(InvokePattern.Pattern, out var placePat))
                ((InvokePattern)placePat).Invoke();
            else
                Console.WriteLine("Boton 'Colocar' NO-FOUND");
            DoEvents();
            DoEvents();

            var placedSlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => !s.IsEmpty);
            Console.WriteLine($"Buff colocado: DisplayName={placedSlot?.DisplayName} DurationSeconds={placedSlot?.DurationSeconds} IsSelected={placedSlot?.IsSelected}");
            Console.WriteLine($"BuffEdit.Slot coincide={ReferenceEquals(vm.BuffEdit.Slot, placedSlot)} MinLabel={vm.BuffEdit.MinLabel} MediaLabel={vm.BuffEdit.MediaLabel} MaxLabel={vm.BuffEdit.MaxLabel}");

            var minButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, vm.BuffEdit.MinLabel)));
            if (minButton != null && minButton.TryGetCurrentPattern(InvokePattern.Pattern, out var minPat))
            {
                ((InvokePattern)minPat).Invoke();
                DoEvents();
                Console.WriteLine($"Tras pulsar '{vm.BuffEdit.MinLabel}': DurationSeconds={placedSlot?.DurationSeconds}");
            }
            else Console.WriteLine($"Boton Minima NO-FOUND (buscado: '{vm.BuffEdit.MinLabel}')");

            var maxButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, vm.BuffEdit.MaxLabel)));
            if (maxButton != null && maxButton.TryGetCurrentPattern(InvokePattern.Pattern, out var maxPat))
            {
                ((InvokePattern)maxPat).Invoke();
                DoEvents();
                Console.WriteLine($"Tras pulsar '{vm.BuffEdit.MaxLabel}': DurationSeconds={placedSlot?.DurationSeconds} (esperado ~33333333, S.getMaxTime real/60)");
            }
            else Console.WriteLine($"Boton Maxima NO-FOUND (buscado: '{vm.BuffEdit.MaxLabel}')");

            // Verificacion real de T-4 (auditoria de Opus, Bloque 5): BuffSlotCompactTemplate
            // tenia el mismo bug real que E-1 (borde de "Calamity" y de "seleccionado"
            // compitiendo, el ultimo declarado ganaba siempre) - un buff de Calamity
            // seleccionado para editarlo perdia el punto/borde rojo. Coloca un buff REAL de
            // Calamity (CalamityIds.BuffIdBase, el primero real del catalogo) en un slot vacio
            // y lo selecciona - ambas señales (punto rojo + borde morado) deben verse a la vez.
            var emptyBuffSlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (emptyBuffSlot != null)
            {
                emptyBuffSlot.PlaceBuff(TerrasavrNative.Core.Calamity.CalamityIds.BuffIdBase);
                vm.SelectBuffSlot(emptyBuffSlot);
                DoEvents();
                DoEvents();
                Console.WriteLine($"T4-BUFF-CALAMITY: DisplayName={emptyBuffSlot.DisplayName} IsCalamity={emptyBuffSlot.IsCalamity} (esperado True) IsSelected={emptyBuffSlot.IsSelected} (esperado True)");
                var rtbBuffCal = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbBuffCal.Render(window);
                var encBuffCal = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encBuffCal.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffCal));
                using var fsBuffCal = File.Create(Path.Combine(AppContext.BaseDirectory, "t4-buff-calamity-seleccionado.png"));
                encBuffCal.Save(fsBuffCal);
            }
            else Console.WriteLine("T4-BUFF-CALAMITY: sin slot de Buffs vacio real - omitido");

            // Verificacion real de T-5 (auditoria de Opus, Bloque 5): la leyenda solo vive en
            // el hueco real de "sin seleccion" - se deselecciona a proposito para verla.
            if (emptyBuffSlot != null) emptyBuffSlot.IsSelected = false;
            vm.BuffEdit.Slot = null;
            DoEvents();
            DoEvents();
            var rtbLeyenda = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbLeyenda.Render(window);
            var encLeyenda = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encLeyenda.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbLeyenda));
            using (var fsLeyenda = File.Create(Path.Combine(AppContext.BaseDirectory, "t5-leyenda-buffs.png"))) encLeyenda.Save(fsLeyenda);
        }
        catch (Exception ex)
        {
            Console.WriteLine("BUFFS-EXCEPTION: " + ex);
        }

        // Septima pasada: investigacion real de comportamiento al redimensionar (queja del
        // usuario: solape en Equipamiento, scroll persistente en Mascota/Montura, perdida de
        // contenido en rejillas grandes, Libreria "siempre igual"). Capturas reales a varios
        // tamaños de ventana, incluido el MinWidth/MinHeight declarado (1000x620) y por debajo.
        void CaptureAt(double w, double h, string tabName, string fileName)
        {
            window.Width = w;
            window.Height = h;
            DoEvents();
            DoEvents();
            DoEvents();
            Console.WriteLine($"  Ventana pedida {w}x{h} -> real ActualWidth={window.ActualWidth:0.#} ActualHeight={window.ActualHeight:0.#}");

            // "Equipamiento"/"Inventario"/"Almacenes" viven DENTRO de "Personaje" > "Objetos" -
            // hace falta seleccionar esos dos primero o el TabItem interno ni siquiera existe
            // en el arbol visual (TabControl solo realiza el contenido de la pestaña activa).
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();

            var tabItem = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, tabName)));
            if (tabItem != null && tabItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPat))
                ((SelectionItemPattern)selPat).Select();
            else
                Console.WriteLine($"  AVISO: TabItem '{tabName}' no encontrado");
            DoEvents();
            DoEvents();

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string shotPath = Path.Combine(AppContext.BaseDirectory, fileName);
            using (var fs = File.Create(shotPath)) encoder.Save(fs);
            Console.WriteLine($"  Captura -> {shotPath}");
        }

        try
        {
            Console.WriteLine($"Window.MinWidth={window.MinWidth} MinHeight={window.MinHeight}");
            CaptureAt(1180, 860, "Equipamiento", "resize-equip-grande.png");
            CaptureAt(1080, 700, "Equipamiento", "resize-equip-minimo.png");

            // Verificacion real de T-2/E-2 (auditoria de Opus, Bloque 4): umbral real de
            // "SizeClass.Amplio" (provisional: 1700px) - justo debajo debe seguir en pildoras,
            // justo encima debe pasar a las 3 vistas lado a lado.
            CaptureAt(1450, 860, "Equipamiento", "e2-justo-debajo-1450.png");
            Console.WriteLine($"E2-UMBRAL: en 1450px, SizeClass={vm.SizeClass} IsEquipmentExpanded={vm.IsEquipmentExpanded} (esperado Compacto/Normal, False - probado a mano que 1450 recorta la 3a vista, ver bitacora.md)");
            CaptureAt(1550, 860, "Equipamiento", "e2-justo-encima-1550.png");
            Console.WriteLine($"E2-UMBRAL: en 1550px, SizeClass={vm.SizeClass} IsEquipmentExpanded={vm.IsEquipmentExpanded} (esperado Amplio, True)");

            // Diagnostico whitebox real: anchos reales de las 3 columnas del SlotRowHost y de
            // cada SlotGridPanel dentro, a la resolucion minima real - para saber si el centro
            // (Armadura/Accesorios) tiene de verdad sitio para sus 5 columnas a MinCell=40, en
            // vez de seguir adivinando a mano.
            void FindRowHostWidths(System.Windows.DependencyObject d)
            {
                if (d is TerrasavrNative.App.Controls.SlotRowHost srh)
                {
                    Console.WriteLine($"  RESIZE-DIAG SlotRowHost: ActualWidth={srh.ActualWidth:0.#}");
                    foreach (var cd in srh.ColumnDefinitions)
                        Console.WriteLine($"  RESIZE-DIAG   Column: ActualWidth={cd.ActualWidth:0.#} Width={cd.Width} MinWidth={cd.MinWidth}");
                }
                if (d is TerrasavrNative.App.Controls.SlotGridPanel sgp)
                    Console.WriteLine($"  RESIZE-DIAG   SlotGridPanel: ActualWidth={sgp.ActualWidth:0.#} ActualHeight={sgp.ActualHeight:0.#} AvailableHeight={sgp.AvailableHeight:0.#} MinCell={sgp.MinCell} MaxCell={sgp.MaxCell} Children={sgp.Children.Count}");
                if (d is System.Windows.Controls.ScrollViewer sv && sv.Content is System.Windows.FrameworkElement content && content is System.Windows.Controls.StackPanel)
                    Console.WriteLine($"  RESIZE-DIAG   ScrollViewer(StackPanel): ActualHeight={sv.ActualHeight:0.#} ExtentHeight={sv.ExtentHeight:0.#} ViewportHeight={sv.ViewportHeight:0.#}");
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                    FindRowHostWidths(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
            }
            Console.WriteLine("RESIZE-DIAG === a 1040x700 ===");
            FindRowHostWidths(window);

            CaptureAt(700, 400, "Equipamiento", "resize-equip-forzado-pequeno.png");
            CaptureAt(1080, 700, "Inventario", "resize-inv-minimo.png");
            CaptureAt(1080, 700, "Almacenes", "resize-almacenes-minimo.png");

            // Verificacion real de A-4 (auditoria de Opus, Bloque 4): por debajo de
            // AmplioMinWidth=1500 (umbral real, medido - ver el comentario de
            // MainViewModel.IsStorageExpanded), la pestaña "Inventario" sigue mostrando solo
            // Inventario - Almacenes ni siquiera existe en el arbol visual en ese momento
            // (drag&drop cruzado imposible, tal cual hasta ahora). Se prueba justo por debajo
            // (1350) para confirmar que el umbral real es de verdad 1500, no el "Normal"
            // original (1300).
            static List<AutomationElement> BuscarPildoraBanco(AutomationElement r) =>
                r.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                    .Cast<AutomationElement>().Where(b => b.Current.Name.StartsWith("Banco")).ToList();

            CaptureAt(1350, 860, "Inventario", "a4-todavia-compacto-1350.png");
            Console.WriteLine($"A4-1350: pildora 'Banco' presente en el arbol visual={BuscarPildoraBanco(root).Count > 0} (esperado False)");

            // A partir de 1500px, Inventario Y Almacenes deben coexistir de verdad en el mismo
            // arbol visual - prueba real (no solo el ViewModel): la pildora real de Almacenes
            // ("Banco...") tiene que aparecer YA, sin cambiar de pestaña.
            CaptureAt(1500, 860, "Inventario", "a4-expandido-1500.png");
            Console.WriteLine($"A4-EXPANDIDO: pildora real de Almacenes presente en Inventario={BuscarPildoraBanco(root).Count > 0} (esperado True - coexisten de verdad, no solo el ViewModel)");

            // Prueba real de intercambio cruzado (no solo "coexisten en pantalla" - que el
            // intercambio Inventario<->Almacen funcione de verdad): SwapWith es el mismo
            // metodo real que ya usa el gesto de arrastrar (MainWindow.xaml.cs.OnItemSlotDrop),
            // sin simular el gesto de raton entero.
            if (vm.InventoryContainer != null && vm.StorageGroup != null)
            {
                var invSlot = vm.InventoryContainer.Slots[0];
                var bankSlot = vm.StorageGroup.Current.Slots[0];
                string invAntes = invSlot.DisplayName, bankAntes = bankSlot.DisplayName;
                invSlot.SwapWith(bankSlot);
                Console.WriteLine($"A4-INTERCAMBIO: Inventario[0] '{invAntes}' -> '{invSlot.DisplayName}' (esperado '{bankAntes}'), Banco[0] '{bankAntes}' -> '{bankSlot.DisplayName}' (esperado '{invAntes}')");
                invSlot.SwapWith(bankSlot); // deshace el intercambio, no dejar el personaje de prueba alterado para el resto de tests
            }
            vm.IsLibraryCollapsed = false;
            CaptureAt(1180, 860, "Inventario", "resize-libreria-grande.png");
            CaptureAt(1080, 700, "Inventario", "resize-libreria-minimo.png");
            CaptureAt(1180, 860, "Equipamiento", "resize-equip-vuelta-grande.png");

            // Octava pasada: comprobar que el Height="380" fijo de Buffs (causa nº1 real del
            // solape segun Opus) ya no lo hace, a la resolucion minima real.
            window.Width = 1080; window.Height = 700;
            DoEvents(); DoEvents();
            var buffsTabForShot = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Buffs")));
            if (buffsTabForShot != null && buffsTabForShot.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buffsShotPat))
                ((SelectionItemPattern)buffsShotPat).Select();
            DoEvents(); DoEvents();
            var rtbBuffs = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuffs.Render(window);
            var encBuffs = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuffs.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffs));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "resize-buffs-minimo.png"))) encBuffs.Save(fs);
            Console.WriteLine("Captura Buffs minimo -> resize-buffs-minimo.png");

            // Referencia visual real pedida por el usuario ("me gustaria algo mas moderno
            // como... segunda captura" - los botones "melee"/"Auto-equipar" de Builds).
            window.Width = 1180;
            window.Height = 860;
            var buildsTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Builds")));
            if (buildsTab != null && buildsTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buildsSelPat))
                ((SelectionItemPattern)buildsSelPat).Select();
            DoEvents();
            DoEvents();
            var rtbBuilds = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuilds.Render(window);
            var encBuilds = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuilds.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuilds));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "builds-referencia.png"))) encBuilds.Save(fs);
            Console.WriteLine("Captura Builds -> builds-referencia.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("RESIZE-EXCEPTION: " + ex);
        }

        // (Bloque real eliminado del arnes: probaba el rediseño de la Libreria de la
        // octava pasada - Fases 2/3/4/5/7 -, revertido entero por feedback directo del
        // usuario. Ver bitacora.md "REVERTIDO por feedback directo".

        // Verificacion real de R-1 (auditoria de Opus, Bloque 2): umbral real de investigacion
        // por objeto, extraido del TSV real de sacrificios de tModLoader - dos ids conocidos de
        // memoria del juego real (IronBroadsword=1, arma unica -> categoria D; DirtBlock=100,
        // bloque comun -> categoria L), directamente contra el catalogo cargado por la propia
        // app (misma instancia que ya construyo MainViewModel, via reflexion de su campo
        // privado _service - mas fiel que construir una segunda instancia aparte).
        try
        {
            var serviceField = typeof(MainViewModel).GetField("_service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var svc = (TerrasavrNative.App.Services.CharacterFileService)serviceField!.GetValue(vm)!;
            Console.WriteLine($"R1-CATALOGO: IronBroadsword(id=4)={svc.VanillaResearchCounts.Get(4)} (esperado 1), DirtBlock(id=2)={svc.VanillaResearchCounts.Get(2)} (esperado 100)");

            var swResearch = System.Diagnostics.Stopwatch.StartNew();
            vm.ResearchAllCommand.Execute(null);
            swResearch.Stop();
            Console.WriteLine($"MEDICION-INVESTIGAR-TODO: {swResearch.ElapsedMilliseconds}ms");
            DoEvents();
            var ironNode = FindCategoryWithItem(vm.Research.RootCategories, 4);
            if (ironNode != null)
            {
                vm.Research.SelectCategoryCommand.Execute(ironNode);
                DoEvents();
                var row = vm.Research.Results.FirstOrDefault();
                Console.WriteLine($"R1-INVESTIGAR-TODO: primera fila de '{ironNode.Name}' -> CountLabel={row?.CountLabel} (esperado formato real x/N, NO '9999')");
            }
            else Console.WriteLine("R1-INVESTIGAR-TODO: ninguna carpeta real con IronBroadsword encontrada");
        }
        catch (Exception ex)
        {
            Console.WriteLine("R1-EXCEPTION: " + ex);
        }

        // Verificacion real de N-3 (auditoria de Opus, Bloque 3): atajos de teclado con
        // pulsaciones REALES a nivel de SO (Keyboard.Modifiers no se puede fingir con un
        // RoutedEventArgs sintetico, lee el estado real del teclado) - ver PressCtrlPlus/
        // PressKey arriba. Ctrl+O NO se prueba aqui (abriria un dialogo modal real - mismo
        // riesgo de cuelgue ya documentado para MessageBox en N-2 - pero reutiliza LITERALMENTE
        // el mismo OnLoadClick que ya prueba a diario el boton "Cargar personaje...", cero
        // riesgo nuevo).
        try
        {
            SetForegroundWindow(hwnd);
            window.Activate();
            DoEvents();

            // Ctrl+F: cambia a Builds primero para confirmar que el atajo SALTA de verdad a la
            // Libreria (no que ya estuviera ahi por casualidad de un test anterior).
            vm.SelectedTabIndex = 2; // Builds
            vm.IsLibraryCollapsed = true;
            DoEvents();
            PressCtrlPlus(0x46); // VK_F
            DoEvents();
            DoEvents();
            var focused = System.Windows.Input.Keyboard.FocusedElement as FrameworkElement;
            Console.WriteLine($"N3-CTRL-F: SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), IsLibraryCollapsed={vm.IsLibraryCollapsed} (esperado False), foco real en LibrarySearchBox={ReferenceEquals(focused, window.FindName("LibrarySearchBox"))}");

            // Esc: pide elegir objeto para un slot real (IsPicking pasa a True, mismo camino
            // real que pulsar "Elegir..." en un slot) y confirma que Esc cancela de verdad.
            var anySlot = vm.InventoryContainer?.Slots.FirstOrDefault();
            if (anySlot != null)
            {
                anySlot.ChooseFromLibraryCommand.Execute(null);
                DoEvents();
                bool pickingAntes = vm.Library.IsPicking;
                PressKey(0x1B); // VK_ESCAPE
                DoEvents();
                Console.WriteLine($"N3-ESC: IsPicking antes={pickingAntes} (esperado True), despues={vm.Library.IsPicking} (esperado False)");
            }
            else Console.WriteLine("N3-ESC: sin slot de Inventario real para probar - omitido");

            // Ctrl+S: confirma que dispara el mismo guardado real (banner de confirmacion) que
            // ya prueba GUARDAR-DESDE-BUILDS, esta vez por teclado.
            vm.IsDirty = true; // fuerza un estado "con cambios" real para que Guardar tenga sentido
            vm.SaveConfirmationVisible = false;
            DoEvents();
            PressCtrlPlus(0x53); // VK_S
            DoEvents();
            DoEvents();
            Console.WriteLine($"N3-CTRL-S: SaveConfirmationVisible={vm.SaveConfirmationVisible} (esperado True)");
        }
        catch (Exception ex)
        {
            Console.WriteLine("N3-EXCEPTION: " + ex);
        }

        // Verificacion real de X-7/T-13 (auditoria de Opus, Bloque 3): un mundo real y grande
        // de esta maquina (11MB, medido antes de tocar nada: 1.4s sincrono, freeze real y
        // perceptible). Sin un app.Run() real (este arnes pumpea manualmente con DoEvents), la
        // unica forma real de probar el await sin deadlockear el propio hilo de UI es un bucle
        // "pumpea hasta que termine" en vez de bloquear con .GetAwaiter().GetResult() (eso SI
        // deadlockearia: Task.Run reanuda via el DispatcherSynchronizationContext instalado por
        // `new Application()`, y nadie bombearia ese mensaje mientras el hilo esta bloqueado
        // esperando).
        try
        {
            string worldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (File.Exists(worldPath))
            {
                vm.SelectedTabIndex = 4; // Exploracion - si no, la captura cae en la pestaña que dejo el test anterior
                DoEvents();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var task = vm.Exploration.LoadFromPathAsync(worldPath);
                long msHastaControl = sw.ElapsedMilliseconds;
                Console.WriteLine($"X7-ASYNC: LoadFromPathAsync devolvio el control tras {msHastaControl}ms (esperado ~0 - la UI NO se congela mientras el mundo se lee/pinta en segundo plano), IsLoading={vm.Exploration.IsLoading} (esperado True)");

                bool primeraVuelta = true;
                while (!task.IsCompleted)
                {
                    DoEvents();
                    if (primeraVuelta)
                    {
                        primeraVuelta = false;
                        var rtbLoading = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbLoading.Render(window);
                        var encLoading = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encLoading.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbLoading));
                        using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-cargando.png"));
                        encLoading.Save(fs);
                    }
                }
                sw.Stop();
                if (task.IsFaulted) throw task.Exception!;
                Console.WriteLine($"X7-ASYNC: '{worldPath}' ({new FileInfo(worldPath).Length / 1024 / 1024}MB) -> {sw.ElapsedMilliseconds}ms totales, IsLoading={vm.Exploration.IsLoading} (esperado False), IsNotLoading={vm.Exploration.IsNotLoading} (esperado True), StatusMessage={vm.Exploration.StatusMessage}");
            }
            else Console.WriteLine("X7-ASYNC: fichero no encontrado, omitido");
        }
        catch (Exception ex) { Console.WriteLine("X7-ASYNC-EXCEPTION: " + ex); }

        // Verificacion real de P-1 (auditoria de Opus, Bloque 4): el preview de Apariencia debe
        // quedarse REALMENTE fijo en pantalla (misma posicion en pixeles) mientras la columna
        // derecha se desplaza - no basta con que "no forme parte del StackPanel que scrollea"
        // en el codigo, hay que medir su posicion real en pantalla antes y despues.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 3; // Apariencia
            window.Width = 1180; window.Height = 700;
            DoEvents();
            DoEvents();

            // FindFirst encontraria antes la miniatura pequeña de la cabecera global (N-1,
            // 28x39, el mismo Appearance.PreviewImage a otro tamaño) que el preview grande real
            // de Apariencia (240x336) - se filtra por el ancho real para coger el correcto.
            var previewImage = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image))
                .Cast<AutomationElement>().FirstOrDefault(img => img.Current.BoundingRectangle.Width > 100);
            var rectAntes = previewImage?.Current.BoundingRectangle ?? Rect.Empty;

            var rtbAparienciaAntes = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAparienciaAntes.Render(window);
            var encAparienciaAntes = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAparienciaAntes.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAparienciaAntes));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "p1-apariencia-antes-scroll.png"))) encAparienciaAntes.Save(fs);

            // Desplaza la columna derecha (el ScrollViewer real que envuelve Genero/Peinado/
            // Tinte/colores/estadisticas) hasta el final via ScrollPattern real.
            var scrollViewers = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane))
                .Cast<AutomationElement>().Where(el => el.TryGetCurrentPattern(ScrollPattern.Pattern, out _)).ToList();
            AutomationElement? rightScroller = null;
            foreach (var sv in scrollViewers)
            {
                if (sv.TryGetCurrentPattern(ScrollPattern.Pattern, out var pat) && ((ScrollPattern)pat).Current.VerticallyScrollable)
                {
                    rightScroller = sv;
                    break;
                }
            }
            if (rightScroller != null && rightScroller.TryGetCurrentPattern(ScrollPattern.Pattern, out var scrollPat))
            {
                ((ScrollPattern)scrollPat).SetScrollPercent(ScrollPattern.NoScroll, 100);
                DoEvents();
                DoEvents();
            }
            else Console.WriteLine("P1-SCROLL: ScrollViewer real con contenido desplazable NO-FOUND");

            var rectDespues = previewImage?.Current.BoundingRectangle ?? Rect.Empty;
            var rtbAparienciaDespues = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAparienciaDespues.Render(window);
            var encAparienciaDespues = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAparienciaDespues.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAparienciaDespues));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "p1-apariencia-despues-scroll.png"))) encAparienciaDespues.Save(fs);

            Console.WriteLine($"P1-PREVIEW-FIJO: posicion antes={rectAntes}, posicion despues={rectDespues} (esperado: identicas)");
        }
        catch (Exception ex) { Console.WriteLine("P1-EXCEPTION: " + ex); }

        // Verificacion real de T-20 (auditoria de Opus, Bloque 6): AutoEquipService extraido de
        // MainViewModel - nunca se habia probado en el arnes ni antes ni despues del cambio, se
        // prueba ahora con datos reales de Builds (mismo Source que ya pasa el boton real
        // "Auto-equipar" del XAML).
        try
        {
            var gear = vm.Builds.VanillaStages.FirstOrDefault()?.Classes.FirstOrDefault()?.Source;
            if (gear != null && vm.EquipmentGroup != null)
            {
                var headBefore = vm.EquipmentGroup.EquippedItems.Slots[0].DisplayName;
                vm.AutoEquipCommand.Execute(gear);
                DoEvents();
                var headAfter = vm.EquipmentGroup.EquippedItems.Slots[0].DisplayName;
                Console.WriteLine($"T20-AUTOEQUIP: cabeza antes='{headBefore}' despues='{headAfter}' (esperado: cambia a un objeto real), StatusMessage={vm.StatusMessage}");
            }
            else Console.WriteLine("T20-AUTOEQUIP: sin gear/EquipmentGroup real - omitido");
        }
        catch (Exception ex) { Console.WriteLine("T20-AUTOEQUIP-EXCEPTION: " + ex); }

        // Verificacion real de T-24 (auditoria de Opus, Bloque 6): 3 casos deterministas
        // (matematica pura, sin depender de ninguna ventana ni layout ya corrido) para los 3
        // modos reales de SlotGridPanel.MeasureOverride - ver el resumen real en el propio
        // SlotGridPanel.cs. Panel.Measure() funciona standalone (sin arbol visual real, sin
        // Window) porque MeasureOverride es matematica pura sobre InternalChildren/las
        // DependencyProperty del propio panel.
        try
        {
            static SlotGridPanel BuildGrid(int childCount, int columns, double minCell, double maxCell, double gap,
                int referenceColumns = 0, double referenceWidth = 0, double availableHeight = 0)
            {
                var grid = new SlotGridPanel
                {
                    Columns = columns, MinCell = minCell, MaxCell = maxCell, Gap = gap,
                    ReferenceColumns = referenceColumns, ReferenceWidth = referenceWidth, AvailableHeight = availableHeight,
                };
                for (int i = 0; i < childCount; i++) grid.Children.Add(new Border());
                return grid;
            }

            // Caso 1 (modo BASICO, suelo real MinCell): 10 columnas, 10 hijos (1 fila), un ancho
            // disponible tan estrecho (300px) que la celda "natural" (26.4px) cae por debajo del
            // suelo real - debe congelarse en MinCell=40, no seguir encogiendo (el ancho real
            // pedido por la rejilla, 436px, supera el disponible - eso es EXACTAMENTE lo que
            // activa el scroll horizontal real cuando esto vive dentro de un ScrollViewer, ver
            // MainWindow.xaml). Hallazgo real de paso, verificado aqui mismo (no de memoria):
            // FrameworkElement.Measure() recorta el ANCHO devuelto al availableSize de entrada
            // (300, no los 436 reales que MeasureOverride calculo) - comportamiento real y
            // documentado de WPF (protege contra un Panel mal comportado que pida mas sitio del
            // que se le ofrecio), NO un bug de SlotGridPanel: el ALTO (sin restriccion real
            // aqui, Infinity de entrada) SI llega intacto y es la prueba real de que la celda de
            // verdad elegida fue 40 (rows=1 * cell=40 = 40), confirmando el suelo real por una
            // via que el recorte de WPF no toca.
            var grid1 = BuildGrid(childCount: 10, columns: 10, minCell: 40, maxCell: 90, gap: 4);
            grid1.Measure(new Size(300, double.PositiveInfinity));
            var size1 = grid1.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso1 (suelo MinCell): DesiredSize={size1} (esperado alto=40 real -1*MinCell-; ancho=300, recortado por WPF al availableSize de entrada, no 436 - ver comentario real)");

            // Caso 2 (modo BASICO, techo real MaxCell): mismos parametros, ancho disponible
            // enorme (2000px) - la celda "natural" (196.4px) supera el techo real, debe
            // congelarse en MaxCell=90, no seguir creciendo (para que Monedas/Municion, si
            // vivieran aqui, no se inflen a tarjetas gigantes).
            var grid2 = BuildGrid(childCount: 10, columns: 10, minCell: 40, maxCell: 90, gap: 4);
            grid2.Measure(new Size(2000, double.PositiveInfinity));
            var size2 = grid2.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso2 (techo MaxCell): DesiredSize={size2} (esperado 936x90 - 10*90+4*9=936, 1*90=90)");

            // Caso 3 (modo ReferenceColumns+ReferenceWidth): 5 columnas, ancho PROPIO enorme
            // (2000px, dejaria crecer la celda sin limite real por si solo) pero referenciado
            // contra una fila hermana de 10 columnas en solo 400px de ancho (cellFromReference=
            // (400-4*9)/10=36.4) - la celda debe quedarse en 36.4, LA MISMA que tendria esa fila
            // hermana, demostrando que el limite cruzado (no el propio ancho) es el que manda.
            var grid3 = BuildGrid(childCount: 5, columns: 5, minCell: 30, maxCell: 90, gap: 4, referenceColumns: 10, referenceWidth: 400);
            grid3.Measure(new Size(2000, double.PositiveInfinity));
            var size3 = grid3.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso3 (ReferenceWidth cruzado): DesiredSize={size3} (esperado 198x36.4 - 5*36.4+4*4=198, 36.4)");
        }
        catch (Exception ex) { Console.WriteLine("T24-SLOTGRID-EXCEPTION: " + ex); }

        string errorLog = Path.Combine(AppContext.BaseDirectory, "ultimo-error.log");
        Console.WriteLine("ultimo-error.log existe: " + File.Exists(errorLog));

        // Bloque 0 (N-2, IsDirty real): MainWindow.OnWindowClosing ahora muestra un MessageBox
        // MODAL real de "cambios sin guardar" cuando IsDirty=true - sin nadie delante para
        // pulsarlo, este arnes se quedaba colgado para siempre en window.Close() (confirmado:
        // 5+ minutos sin avanzar, salida vacia incluso tras matar el proceso - el buffer de
        // consola redirigido nunca llega a volcarse porque el hilo de UI nunca vuelve). El
        // arnes es codigo de prueba, no un usuario real - se limpia el flag antes de cerrar.
        // Verificacion real de T-3 (auditoria de Opus, Bloque 4): tamaño/posicion reconocibles
        // y distintos de los de fabrica, antes de cerrar (Close() real dispara
        // OnWindowClosing -> WindowPlacementService.Save real).
        window.Left = 40;
        window.Top = 55;
        window.Width = 1234;
        window.Height = 789;
        DoEvents();

        vm.IsDirty = false;
        window.Close();
        DoEvents();
        string placementPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "window.json");
        Console.WriteLine($"T3-GUARDADO: {placementPath} existe={File.Exists(placementPath)}, contenido={(File.Exists(placementPath) ? File.ReadAllText(placementPath) : "(nada)")}");
        app.Shutdown();
        Console.WriteLine("DONE");
    }

    private static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    // Verificacion real de N-3 (auditoria de Opus, Bloque 3, atajos de teclado): Keyboard.Modifiers
    // lee el estado REAL del teclado a nivel de SO (no algo derivable de un RoutedEventArgs
    // sintetico) - la unica forma real de probar un Ctrl+combinacion de verdad es inyectar
    // pulsaciones reales a nivel de SO (keybd_event), con la ventana real en primer plano.
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;

    private static void PressCtrlPlus(byte vkKey)
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void PressKey(byte vkKey)
    {
        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static CategoryNodeViewModel? FindCategoryWithItem(IEnumerable<CategoryNodeViewModel> nodes, int itemId)
    {
        foreach (var node in nodes)
        {
            if (node.ItemIdsOrdered.Contains(itemId)) return node;
            var inChild = FindCategoryWithItem(node.Children, itemId);
            if (inChild != null) return inChild;
        }
        return null;
    }
}
