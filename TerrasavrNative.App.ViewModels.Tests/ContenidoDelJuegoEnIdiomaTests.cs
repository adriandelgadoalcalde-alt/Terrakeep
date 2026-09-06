using System;
using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Ronda de traduccion del CONTENIDO del juego (6-sep-2026). Las rondas de idioma anteriores
// dejaron la INTERFAZ entera bilingue pero el contenido del propio juego (nombres de objeto/
// NPC/tile/buff, tooltips descriptivos, bonos de set) seguia en español en los DOS idiomas -
// escrito como LIMITE-CONOCIDO en bitacora.md, con la frase del editor traducida envolviendo un
// texto de juego que no lo estaba ("With the full set: Reduce el coste de mana...").
//
// Estas pruebas van por el camino REAL de la app (MainViewModel -> contenedor -> slot, y la
// Libreria real), no por el catalogo suelto: lo que se comprueba es exactamente lo que se pinta
// en pantalla. Los catalogos por su cuenta ya los cubre
// TerrasavrNative.Core.Tests/Data/ContenidoBilingueRealTests.
//
// Este proyecto corre SIN paralelismo (ver ParalelismoDeTests.cs), asi que cambiar el idioma
// activo aqui es seguro - aun asi cada prueba lo restaura, porque LocalizationService es un
// singleton del proceso.
public sealed class ContenidoDelJuegoEnIdiomaTests
{
    private static void ConIdioma(string idioma, Action cuerpo)
    {
        string antes = LocalizationService.Instance.Language;
        try { LocalizationService.Instance.SetLanguage(idioma); cuerpo(); }
        finally { LocalizationService.Instance.SetLanguage(antes); }
    }

    // Mismo bombeo real que ResearchOlaTresTests: SearchText de la Libreria debouncea 180ms
    // (CatalogBrowserViewModel), asi que mirar Results de inmediato no ve nada. La pausa entre
    // vueltas es necesaria de verdad - sin ella el WM_TIMER real puede no llegar a entregarse.
    private static void BombearDispatcher(int ms)
    {
        long hasta = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < hasta)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background, new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            System.Threading.Thread.Sleep(1);
        }
    }

    private static MainViewModel ConPersonajeCargado()
    {
        var character = new PlrCharacter
        {
            Name = "Idioma",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"idioma-contenido-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void ElNombreDelObjetoEnUnSlotReal_CambiaAlCambiarDeIdiomaEnVivo()
    {
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = ConPersonajeCargado();
            var slot = vm.InventoryContainer!.Slots[0];
            slot.PlaceItem(1); // Pico de hierro / Iron Pickaxe

            Assert.Equal("Pico de hierro", slot.DisplayName);

            // EN VIVO: sin recargar el personaje ni volver a colocar el objeto.
            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            Assert.Equal("Iron Pickaxe", slot.DisplayName);

            LocalizationService.Instance.SetLanguage(LocalizationService.Spanish);
            Assert.Equal("Pico de hierro", slot.DisplayName);
        });
    }

    [Fact]
    public void ElTooltipDeUnAccesorio_SeVeEnIngles()
    {
        ConIdioma(LocalizationService.English, () =>
        {
            var vm = ConPersonajeCargado();
            var slot = vm.InventoryContainer!.Slots[0];
            slot.PlaceItem(8); // Antorcha / Torch - tooltip real "Da luz" / "Provides light"

            string t = slot.StatsTooltip ?? "";
            Assert.Contains("Provides light", t);
            Assert.DoesNotContain("Da luz", t);
        });
    }

    [Fact]
    public void ElBonoDeSetVanilla_SeVeEnInglesEnteroSinMezclarIdiomas()
    {
        // El caso exacto que quedo escrito como limite en bitacora.md: la frase del editor
        // estaba traducida y el texto del juego que envuelve no.
        string enIngles = "", enEspañol = "";
        ConIdioma(LocalizationService.English, () =>
        {
            var vm = ConPersonajeCargado();
            var slot = vm.InventoryContainer!.Slots[0];
            slot.PlaceItem(76); // Casco de cobre - set real MetalTier1, "2 defensa"/"2 defense"
            enIngles = slot.StatsTooltip ?? "";
        });
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = ConPersonajeCargado();
            var slot = vm.InventoryContainer!.Slots[0];
            slot.PlaceItem(76);
            enEspañol = slot.StatsTooltip ?? "";
        });

        Assert.Contains("2 defense", enIngles);
        Assert.DoesNotContain("2 defensa", enIngles);
        Assert.Contains("2 defensa", enEspañol);
    }

    [Fact]
    public void ElBonoDeSetDeCalamity_SeVeEnInglesRealDelMod()
    {
        ConIdioma(LocalizationService.English, () =>
        {
            var vm = ConPersonajeCargado();
            var entrada = new CharacterFileService().CalamityCatalog.ByModAndInternal("CalamityMod", "AerospecHeadMelee");
            Assert.NotNull(entrada);
            var slot = vm.InventoryContainer!.Slots[0];
            slot.PlaceItem(entrada.SyntheticId);

            string t = slot.StatsTooltip ?? "";
            Assert.Contains("increased movement speed", t);
            Assert.DoesNotContain("velocidad de movimiento", t);
        });
    }

    [Fact]
    public void ElNombreDeUnBuff_CambiaAlCambiarDeIdiomaEnVivo()
    {
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = ConPersonajeCargado();
            var slot = vm.Buffs.Container!.Slots[0];
            slot.PlaceBuff(1); // ObsidianSkin

            Assert.Equal("Piel de obsidiana", slot.DisplayName);
            Assert.Equal("Inmune a la lava", slot.Description);

            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            Assert.Equal("Obsidian Skin", slot.DisplayName);
            Assert.Equal("Immune to lava", slot.Description);
        });
    }

    [Fact]
    public void LaLibreria_EnseñaLosNombresEnElIdiomaActivoYLosBuscaEnEse()
    {
        ConIdioma(LocalizationService.Spanish, () =>
        {
            var vm = ConPersonajeCargado();
            var libreria = vm.Library;

            libreria.SearchText = "Pico de hierro";
            BombearDispatcher(400);
            Assert.Contains(libreria.Results, r => r.DisplayName == "Pico de hierro");

            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            libreria.SearchText = "Iron Pickaxe";
            BombearDispatcher(400);
            // Buscar por el nombre INGLES tiene que encontrarlo con la app en ingles: si el
            // plegado del nombre se quedara en español, el buscador no encontraria lo que el
            // usuario ve escrito en la propia tarjeta.
            Assert.Contains(libreria.Results, r => r.DisplayName == "Iron Pickaxe");
        });
    }
}
