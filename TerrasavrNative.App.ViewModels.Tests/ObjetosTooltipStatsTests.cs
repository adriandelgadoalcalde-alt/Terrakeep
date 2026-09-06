using System;
using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// OBJ-08 (oleada de pruebas de Personaje -> Objetos, 6-sep-2026). El tooltip de estadisticas es
// lo mas leido de toda esta zona ("ninguna de las armas armaduras o accesorios te muestran las
// estadisticas", pedido explicito del 1-sep-2026) y hasta ahora no habia ni una prueba que lo
// mirase DESDE UN SLOT REAL: ItemStatsFormatter se probaba suelto en WhatsNewIconTests y nada mas.
//
// Aqui se comprueba el camino entero y real - colocar el objeto en el slot del contenedor que le
// corresponde y leer `ItemSlotViewModel.StatsTooltip`, que es literalmente lo que ve el usuario al
// pasar el raton - para las 4 familias que el usuario nombro: arma vanilla, armadura vanilla con
// bono de set, arma de Calamity y armadura de Calamity. Los numeros esperados salen de los assets
// REALES (vanilla_stats.json / vanilla_armor_sets.json / calamity/catalog.json), citados uno a uno.
public sealed class ObjetosTooltipStatsTests
{
    private static MainViewModel ConPersonajeCargado()
    {
        var character = new PlrCharacter
        {
            Name = "Tooltips",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"obj-tooltips-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void ArmaVanillaEnElInventario_DiceDañoDpsVelocidadYRetroceso()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];

        // "Espada larga de hierro" (id 4): damage=12, knockBack=5.5, useTime=20 en
        // vanilla_stats.json, categoria real "Armas/Cuerpo a cuerpo" en
        // vanilla_categories.json - o sea 60*12/20 = 36 DPS, useTime 20 -> "Muy Rapido"
        // (tramo <=20) y retroceso 5.5 -> "Normal" (tramo <=6).
        slot.PlaceItem(4);

        string t = slot.StatsTooltip ?? "";
        Assert.Contains("12 daño de cuerpo a cuerpo", t);
        Assert.Contains("36 DPS", t);
        Assert.Contains("Use time 20", t);
        Assert.Contains("Muy Rapido", t);
        // InvariantCulture en ItemStatsFormatter.FormatKnockback: punto decimal, no coma.
        Assert.Contains("Retroceso 5.5", t);
    }

    [Fact]
    public void ArmaduraVanillaEnSuSlot_DiceLaDefensaYElBonoDeSetCompleto()
    {
        var vm = ConPersonajeCargado();
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0]; // SlotKind.ArmorHead real

        // "Casco de plata" (id 91): defense=3 en vanilla_stats.json, y forma el set
        // "MetalTier2" con texto real "3 defensa" (vanilla_armor_sets.json, piezas 91/82/78).
        casco.PlaceItem(91);

        string t = casco.StatsTooltip ?? "";
        Assert.False(casco.IsEmpty); // la restriccion de slot lo acepto de verdad
        Assert.Contains("3 defensa", t);
        Assert.Contains("Con el set completo", t);
    }

    [Fact]
    public void ArmaduraDeCalamityEnSuSlot_TambienDiceDefensaYBonoDeSet()
    {
        var vm = ConPersonajeCargado();
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0];

        // "Sombrero de Aerospec" (indice 244 de calamity/catalog.json -> id sintetico
        // 20000244): stats.defense=3 y setBonus real de 4 lineas. Era el hueco reportado el
        // 2-sep-2026 ("las armaduras de calamity no dicen especificaciones cuando pasas el
        // raton" + "la bonificacion por el set no aparece").
        casco.PlaceItem(20000244);

        Assert.True(casco.IsCalamity);
        string t = casco.StatsTooltip ?? "";
        Assert.Contains("3 defensa", t);
        Assert.Contains("Con el set completo", t);
        Assert.Contains("Reduce el coste de maná un 8%", t);
    }

    [Fact]
    public void ArmaDeCalamityEnElInventario_DiceElDañoConSuTipoReal()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];

        // "BloodfireArrow" (indice 223 -> 20000223): damage=19, damageType
        // "DamageClass.Ranged" -> etiqueta real "daño por rango".
        slot.PlaceItem(20000223);

        Assert.True(slot.IsCalamity);
        string t = slot.StatsTooltip ?? "";
        Assert.Contains("19 daño por rango", t);
    }

    [Fact]
    public void ElPrefijoAplicadoSumaSuEfectoRealAlTooltip()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        // Id 4 ("Espada larga de hierro") a proposito: best_prefix.json le da 81 (Legendary
        // real). Ojo con elegir otro id al mantener esta prueba: hay objetos reales que NO
        // pueden llevar prefijo (bloques, accesorios de vanidad, los de la lista negra
        // ItemID.Sets.CanGetPrefixes) y ahi PlaceItem no aplica ninguno, correctamente. Desde
        // el 6-sep-2026 la tabla la genera `scripts/generar-mejor-prefijo.py` con la formula
        // real del juego y cubre 948 objetos vanilla - antes 243, que es el hueco que la
        // oleada de QA de Objetos dejo medido ("145 de los 571 objetos con daño").
        slot.PlaceItem(4);

        // PlaceItem ya aplica el MEJOR prefijo automaticamente (pedido explicito 1-sep-2026),
        // asi que el tooltip tiene que traer YA el efecto numerico de ese prefijo - y el propio
        // slot tiene que decir que no queda ninguno mejor que sugerir.
        Assert.False(string.IsNullOrEmpty(slot.PrefixDisplay));
        Assert.False(slot.HasBestPrefixSuggestion);

        string conPrefijo = slot.StatsTooltip ?? "";
        slot.SetPrefix(TerrasavrNative.Core.Model.ItemPrefix.None);
        string sinPrefijo = slot.StatsTooltip ?? "";

        Assert.NotEqual(sinPrefijo, conPrefijo);
        Assert.True(conPrefijo.Length > sinPrefijo.Length,
            $"el tooltip con prefijo deberia añadir el efecto real del prefijo:\ncon='{conPrefijo}'\nsin='{sinPrefijo}'");
        // Y vuelve a haber una sugerencia real en cuanto se le quita el prefijo.
        Assert.True(slot.HasBestPrefixSuggestion);
    }

    // Ronda de idioma del 6-sep-2026 (cierra el hallazgo que la oleada de QA de Objetos dejo
    // anotado sin tocar: "ItemStatsFormatter compone TODO el texto en español a fuego, asi que
    // con la app en ingles los tooltips de estadisticas siguen en español"). Van sobre el
    // MISMO camino real de las pruebas de arriba - el StatsTooltip del slot - cambiando el
    // idioma EN VIVO sobre el objeto ya colocado, que es el gesto real del usuario.
    private static void ConIdioma(string idioma, Action cuerpo)
    {
        string previo = LocalizationService.Instance.Language;
        try
        {
            LocalizationService.Instance.SetLanguage(idioma);
            cuerpo();
        }
        finally { LocalizationService.Instance.SetLanguage(previo); }
    }

    [Fact]
    public void ElTooltipDeUnArmaCambiaDeIdiomaEnVivo()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(4); // "Espada larga de hierro", ver la prueba de arriba para los numeros

        ConIdioma(LocalizationService.Spanish, () =>
        {
            string t = slot.StatsTooltip ?? "";
            Assert.Contains("12 daño de cuerpo a cuerpo", t);
            Assert.Contains("Use time 20 (3/s, Muy Rapido)", t);
            Assert.Contains("Retroceso 5.5 (Normal)", t);
            // Seccion 1: el efecto real del prefijo automatico ("Legendario", id 81).
            Assert.Contains("+15% de daño", t);
        });

        ConIdioma(LocalizationService.English, () =>
        {
            string t = slot.StatsTooltip ?? "";
            Assert.Contains("12 melee damage", t);
            Assert.Contains("Use time 20 (3/s, Very Fast)", t);
            // El punto decimal sigue siendo InvariantCulture en los dos idiomas.
            Assert.Contains("Knockback 5.5 (Normal)", t);
            Assert.Contains("+15% damage", t);
            // Y no queda NADA del texto fijo español que antes iba a fuego.
            Assert.DoesNotContain("daño", t);
            Assert.DoesNotContain("Retroceso", t);
            Assert.DoesNotContain("Muy Rapido", t);
        });
    }

    [Fact]
    public void ElTooltipDeUnaArmaduraVanillaTraduceDefensaYLaFraseDelBonoDeSet()
    {
        var vm = ConPersonajeCargado();
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0];
        casco.PlaceItem(91); // "Casco de plata": 3 defensa + set "MetalTier2"

        ConIdioma(LocalizationService.Spanish, () =>
        {
            string t = casco.StatsTooltip ?? "";
            Assert.Contains("3 defensa", t);
            Assert.Contains("Con el set completo:", t);
        });

        ConIdioma(LocalizationService.English, () =>
        {
            string t = casco.StatsTooltip ?? "";
            Assert.Contains("3 defense", t);
            Assert.Contains("With the full set:", t);
            Assert.DoesNotContain("Con el set completo", t);
        });
    }

    [Fact]
    public void ElTooltipDeUnaArmaduraDeCalamityTambienTraduceLaFraseDelBonoDeSet()
    {
        var vm = ConPersonajeCargado();
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0];
        casco.PlaceItem(20000244); // "Sombrero de Aerospec": 3 defensa + setBonus real

        ConIdioma(LocalizationService.Spanish, () =>
        {
            string t = casco.StatsTooltip ?? "";
            Assert.Contains("3 defensa", t);
            Assert.Contains("Con el set completo:", t);
        });

        ConIdioma(LocalizationService.English, () =>
        {
            string t = casco.StatsTooltip ?? "";
            Assert.Contains("3 defense", t);
            Assert.Contains("With the full set:", t);
            Assert.DoesNotContain("Con el set completo", t);
        });
    }

    // La OTRA forma real de bono de set de Calamity (C-10c): vista desde el PETO/PERNERAS, que
    // no tienen SetBonus propio - la frase la compone la App enumerando cada casco del set, con
    // el "y" entre las dos piezas y el "·" de cada linea. Es la unica rama con mas de un texto
    // fijo encadenado, y por eso se comprueba aparte.
    [Fact]
    public void ElBonoDeSetDeCalamityVistoDesdeElPetoTraduceLaFraseCompuesta()
    {
        var vm = ConPersonajeCargado();
        var peto = vm.EquipmentGroup!.EquippedItems.Slots[1]; // SlotKind.ArmorBody real
        // "Coraza de Aerospec" (indice 243 de calamity/catalog.json -> 20000243): es el CUERPO
        // del set, sin SetBonus propio (0/131 cuerpos/piernas lo tienen), y el set tiene 5
        // cascos reales (244..248) - exactamente el caso que C-10c cerro.
        peto.PlaceItem(20000243);

        Assert.True(peto.IsCalamity);

        ConIdioma(LocalizationService.Spanish, () =>
        {
            string t = peto.StatsTooltip ?? "";
            Assert.Contains("Con el set completo (con ", t);
            Assert.Contains(" y ", t);
            Assert.Contains("\n· ", t);
        });

        ConIdioma(LocalizationService.English, () =>
        {
            string t = peto.StatsTooltip ?? "";
            Assert.Contains("With the full set (with ", t);
            Assert.Contains(" and ", t);
            Assert.Contains("\n· ", t);
            Assert.DoesNotContain("Con el set completo", t);
        });
    }

    // Los otros DOS textos del MISMO tooltip del slot que dependen del idioma, encontrados al
    // abrir el popup de verdad con la app en ingles (OBJ-STATS-IDIOMA del arnes): decia
    // "Cabeza" y "Prefix: Legendario". El primero ya usaba Loc[...] pero es una propiedad
    // calculada que nunca avisaba de que habia cambiado; el segundo cogia siempre el campo "es"
    // del catalogo aunque el "en" estuviera ahi desde el primer dia (mismo bug real que esta
    // ronda ya cerro en Builds y en Novedades).
    [Fact]
    public void ElNombreDelPrefijoYElRolDelSlotSiguenAlIdioma()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        var casco = vm.EquipmentGroup!.EquippedItems.Slots[0];
        slot.PlaceItem(4);   // se le aplica "Legendario"/"Legendary" (prefijo 81) automaticamente
        casco.PlaceItem(91);

        ConIdioma(LocalizationService.Spanish, () =>
        {
            Assert.Equal("Legendario", slot.PrefixDisplay);
            Assert.Equal("Cabeza", casco.SlotRoleLabel);
        });

        ConIdioma(LocalizationService.English, () =>
        {
            Assert.Equal("Legendary", slot.PrefixDisplay);
            Assert.Equal("Head", casco.SlotRoleLabel);
        });
    }

    [Fact]
    public void UnSlotVacioNoTieneTooltipDeEstadisticas()
    {
        var vm = ConPersonajeCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(4);
        Assert.NotNull(slot.StatsTooltip);

        slot.ClearCommand.Execute(null);

        Assert.True(slot.IsEmpty);
        Assert.Null(slot.StatsTooltip);
    }
}
