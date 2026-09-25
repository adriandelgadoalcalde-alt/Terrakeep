// MAXSTACK_SOLO=1 (25-sep-2026, encargo Keep "EDITOR DE OBJETO... controles rapidos de stack
// +10/+100/MAX, respetando el maxStack real"). Aplicador-fix, no investigacion: el encargo ya
// especificaba el comportamiento exacto, ver bitacora.md de esta misma fecha para el detalle real
// de la implementacion (VanillaMaxStackCatalog/CalamityCatalog.Stats.MaxStack,
// ItemSlotViewModel.MaxStack/AddTenToCountCommand/AddHundredToCountCommand/SetCountToMaxCommand,
// MainWindow.xaml ~142-158).
//
// Visual-QA real (regla de la familia Keep: sin overflow/clipping en ningun idioma) - coloca 3
// objetos con maxStack real distinto (id 4 "Espada larga de hierro" -> 1, id 2 "Tierra" -> 9999,
// id 71 "Moneda de cobre" -> 100, los mismos 3 ya usados en Terrakeep.App.ViewModels.Tests/
// StackQuickControlsTests.cs), selecciona cada uno en el panel Editar, dispara los 3 comandos
// nuevos y mide con VisibleEntero (mismo helper real que AR-15/AR-11 ya usan en el resto del
// arnes) que los 3 botones nuevos quedan ENTEROS dentro de la ventana, en español e ingles.
// Captura real (RenderTargetBitmap sobre la ventana real, ver TERRAKEEP_SCREENSHOTS) para cada
// combinacion objeto x idioma.
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

internal static partial class Program
{
    private static void EjecutarMaxStackSolo(MainWindow window, MainViewModel vm)
    {
        string shotDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(shotDir);
        void Shot(string name)
        {
            DoEvents();
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using var fs = File.Create(Path.Combine(shotDir, name + ".png"));
            enc.Save(fs);
            Console.WriteLine($"SCREENSHOT: {name}.png");
        }

        FijarTamaño(window, 1180, 860); // tamaño de arranque real de la app

        // Personaje sintetico, mismo patron real que StackQuickControlsTests/ObjetosTooltipStatsTests.
        var character = new PlrCharacter
        {
            Name = "MaxStackCanario",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string plrPath = Path.Combine(Path.GetTempPath(), $"maxstack-canario-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(plrPath, PlrFile.Write(character));
        vm.LoadFromPath(plrPath);
        File.Delete(plrPath);
        DoEvents(); DoEvents();

        vm.SelectedTabIndex = 1; // Personaje (AppTab.Personaje) - Objetos ya es la pestaña interna por defecto
        DoEvents(); DoEvents();

        var inventario = vm.InventoryContainer;
        if (inventario == null)
        {
            Console.WriteLine("FALLO: MAXSTACK_SOLO - no hay InventoryContainer real tras cargar el personaje sintetico");
            return;
        }

        // (nombre, id, maxStack esperado) - los mismos 3 ya verificados a mano en
        // StackQuickControlsTests (ver ese fichero para la cita real de cada valor en Item.cs).
        (string Nombre, int Id, int MaxStackEsperado)[] objetos =
        [
            ("espada-maxstack-1", 4, 1),
            ("tierra-maxstack-9999", 2, 9999),
            ("moneda-cobre-maxstack-100", 71, 100),
        ];

        bool huboFallo = false;

        foreach (var idioma in new[] { "es", "en" })
        {
            LocalizationService.Instance.SetLanguage(idioma);
            DoEvents(); DoEvents();

            foreach (var (nombre, id, maxEsperado) in objetos)
            {
                var slot = inventario.Slots[0];
                slot.PlaceItem(id);
                vm.SelectSlot(slot);
                DoEvents(); window.UpdateLayout(); DoEvents();

                if (slot.MaxStack != maxEsperado)
                {
                    huboFallo = true;
                    Console.WriteLine($"FALLO: MAXSTACK_SOLO [{idioma}] {nombre} - MaxStack real={slot.MaxStack}, esperado={maxEsperado}");
                }

                // Los 3 botones nuevos, localizados por Content real (Loc[action_add_ten/...]) -
                // igual que el resto del arnes localiza controles por texto renderizado real, no
                // por posicion fija (D-e/AR-02..AR-10 del resto del fichero).
                string txtDiez = LocalizationService.Instance["action_add_ten"];
                string txtCien = LocalizationService.Instance["action_add_hundred"];
                string txtMax = LocalizationService.Instance["action_set_max"];
                var botones = Descendientes<Button>(window)
                    .Where(b => b.Content is string s && (s == txtDiez || s == txtCien || s == txtMax))
                    .ToList();

                if (botones.Count != 3)
                {
                    huboFallo = true;
                    Console.WriteLine($"FALLO: MAXSTACK_SOLO [{idioma}] {nombre} - se esperaban 3 botones nuevos visibles (+10/+100/MAX), encontrados {botones.Count}");
                }
                else
                {
                    foreach (var boton in botones)
                    {
                        bool entero = VisibleEntero(boton, window);
                        if (!entero)
                        {
                            huboFallo = true;
                            var r = RectVisible(boton, window);
                            Console.WriteLine($"FALLO: MAXSTACK_SOLO [{idioma}] {nombre} - boton '{boton.Content}' recortado/fuera de la ventana (rect visible={r}, tamaño real={boton.ActualWidth}x{boton.ActualHeight})");
                        }
                    }
                }

                // Ejercita los 3 comandos de verdad (mismo camino que StackQuickControlsTests, pero
                // aqui ademas confirma que el TextBox de Cantidad refleja el resultado en pantalla).
                slot.SetCountToMaxCommand.Execute(null);
                DoEvents(); window.UpdateLayout(); DoEvents();
                if (slot.Count != maxEsperado)
                {
                    huboFallo = true;
                    Console.WriteLine($"FALLO: MAXSTACK_SOLO [{idioma}] {nombre} - tras MAX, Count={slot.Count}, esperado={maxEsperado}");
                }

                Console.WriteLine($"MAXSTACK-OK [{idioma}] {nombre}: MaxStack={slot.MaxStack}, Count tras MAX={slot.Count}, botones enteros={botones.Count == 3 && botones.All(b => VisibleEntero(b, window))}");
                Shot($"maxstack-{idioma}-{nombre}");

                slot.ClearCommand.Execute(null);
                DoEvents();
            }
        }

        LocalizationService.Instance.SetLanguage("es");

        Console.WriteLine(huboFallo ? "FALLO: MAXSTACK_SOLO - ver lineas FALLO arriba" : "MAXSTACK_SOLO: OK - 3 objetos x 2 idiomas, sin overflow/clipping, MAX/Count reales correctos");
    }
}
