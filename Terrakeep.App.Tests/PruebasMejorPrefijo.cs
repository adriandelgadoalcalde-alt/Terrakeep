using System;
using System.Collections.Generic;
using System.Linq;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

// ========================================================================================
// MP-* (6-sep-2026): el boton "Mejor prefijo" y el prefijo automatico, sobre la app REAL
// ========================================================================================
// Otra parte de la MISMA clase Program del arnes (ver el comentario de `partial` en
// Program.cs): usa sus helpers tal cual, sin duplicar ni una linea. Vive en su propio fichero
// solo porque Program.cs pasa de 5.900 lineas y varias rondas trabajan sobre el a la vez.
//
// Que cierra: la oleada de QA de Personaje/Objetos dejo medido y sin tocar que el "mejor
// prefijo automatico" solo cubria 145 de los 571 objetos vanilla con daño y 55 de 259
// accesorios, porque `calamity/best_prefix.json` no tenia entrada para el resto y
// `PrefixSuggester.Suggest` devuelve null en ese caso (ni prefijo al colocar, ni boton ★).
// `scripts/generar-mejor-prefijo.py` reconstruyo esa tabla desde la formula real del juego
// (Item.TryGetPrefixStatMultipliersForItem + Item.BestPrefixValue, Terraria 1.4.5.8) y la
// cobertura vanilla paso de 243 a 948 objetos.
//
// Estos bloques NO miran el JSON: van por el camino real de la app - colocar el objeto en un
// slot real del contenedor real y leer lo que el usuario ve (PrefixDisplay) y lo que decide
// si aparece el boton (HasBestPrefixSuggestion). Es el unico punto donde se comprueba que la
// tabla nueva llega de verdad hasta la pantalla.
internal static partial class Program
{
    // Objetos vanilla reales cuyo mejor prefijo esta VERIFICADO uno a uno contra la wiki
    // oficial (terraria.wiki.gg), no calculado aqui: si el generador cambia de criterio, esto
    // salta. Los dos ultimos son los casos donde la propia wiki distingue entre "el mejor para
    // jugar" y el que "is recognized in-game as its best modifier" - esta tabla implementa el
    // segundo, que es el criterio del propio Terraria (Item.BestPrefixValue).
    private static readonly (int Id, string Nombre, byte Prefijo, string Porque)[] MejorPrefijoVerificado =
    {
        (368, "Excalibur", 81, "wiki: \"Its best modifier is Legendary\" - ANTES no tenia entrada"),
        (65, "Starfury", 81, "ya estaba cubierto antes: no debe cambiar"),
        (4, "Iron Broadsword", 81, "ya estaba cubierto antes: no debe cambiar"),
        (98, "Minishark", 60, "wiki: \"best modifier is Demonic, as it does not have any knockback\""),
        (39, "Wooden Bow", 60, "wiki: mismo motivo (retroceso 0 real)"),
        (1244, "Nimbus Rod", 60, "wiki: \"Demonic because it cannot get modifiers that affect knockback\""),
        (3389, "Terrarian", 84, "unico objeto del pool ItemsThatCanHaveLegendary2"),
        (4758, "Blade Staff", 95, "wiki: \"Eager... is recognized in-game as its best modifier\""),
        (905, "Coin Gun", 17, "wiki: \"Rapid and Hasty are recognized in-game as its 'best' modifier\""),
    };

    // Objetos vanilla reales que ANTES de la ampliacion no tenian ninguna entrada (comprobado
    // contra la copia previa de best_prefix.json) y ahora si. Uno de cada pool real, para que
    // el bloque no se apoye en un solo tipo de arma.
    private static readonly (int Id, string Nombre, byte Prefijo)[] CoberturaNueva =
    {
        (368, "Excalibur (espada)", 81),
        (3018, "Seedler (espada)", 81),
        (757, "Terra Blade (espada)", 81),
        (1826, "Horseman's Blade (espada)", 81),
        (3389, "Terrarian (yoyo)", 84),
        (4758, "Blade Staff (invocacion)", 95),
    };

    private static void PruebasMejorPrefijo(MainViewModel vm, MainWindow window)
    {
        try
        {
            int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex;
            try
            {
                vm.SelectedTabIndex = 1;
                vm.PersonajeInnerTabIndex = 0;
                DoEvents();

                var contenedor = vm.InventoryContainer;
                if (contenedor == null || contenedor.Slots.Count == 0)
                {
                    Console.WriteLine("FALLO: MP-00 - no hay contenedor de Inventario real donde colocar nada");
                    return;
                }

                // Los slots se dejan como estaban: este arnes comparte personaje (y sesion) con
                // los bloques que van despues, y un arma suelta en el hueco 0 contamina OBJ-*.
                var slot = contenedor.Slots[0];
                var original = slot.Item;

                // ---------- MP-01: los 9 casos verificados a mano contra la wiki ----------
                var falla01 = new List<string>();
                foreach (var (id, nombre, esperado, porque) in MejorPrefijoVerificado)
                {
                    slot.PlaceItem(id);
                    DoEvents();
                    byte obtenido = slot.Item.Prefix.VanillaId;
                    string visible = slot.PrefixDisplay ?? "";
                    Console.WriteLine($"MP-01: {nombre} (id {id}) -> prefijo {obtenido} \"{visible}\" (esperado {esperado}) [{porque}]");
                    if (obtenido != esperado)
                        falla01.Add($"{nombre} (id {id}): esperado {esperado}, obtenido {obtenido}");
                }
                if (falla01.Count > 0)
                    Console.WriteLine($"FALLO: MP-01 - {falla01.Count} objeto(s) reciben un prefijo distinto del real verificado contra la wiki: {string.Join(" ; ", falla01)}");

                // ---------- MP-02: cobertura NUEVA, la que el QA dejo medida como hueco -------
                // El caso concreto que la bitacora del 1-sep-2026 dejo escrito: "colocar
                // Excalibur (id 368) desde la Libreria no le puso prefijo - es correcto,
                // best_prefix.json solo cubre 243 objetos vanilla curados". Ya no es correcto:
                // ahora tiene que ponerselo.
                var falla02 = new List<string>();
                foreach (var (id, nombre, esperado) in CoberturaNueva)
                {
                    slot.PlaceItem(id);
                    DoEvents();
                    bool tienePrefijo = !string.IsNullOrEmpty(slot.PrefixDisplay);
                    byte obtenido = slot.Item.Prefix.VanillaId;
                    // Y ademas el boton ★ tiene que estar APAGADO: si ya lleva el mejor
                    // prefijo posible, no hay nada mejor que sugerir.
                    bool sugiereOtro = slot.HasBestPrefixSuggestion;
                    Console.WriteLine($"MP-02: {nombre} (id {id}) al colocarlo -> prefijo \"{slot.PrefixDisplay}\" ({obtenido}, esperado {esperado}), boton mejor prefijo activo={sugiereOtro} (esperado False)");
                    if (!tienePrefijo || obtenido != esperado || sugiereOtro)
                        falla02.Add($"{nombre} (id {id})");
                }
                if (falla02.Count > 0)
                    Console.WriteLine($"FALLO: MP-02 - {falla02.Count} objeto(s) que antes no tenian cobertura siguen sin recibir su mejor prefijo real: {string.Join(" ; ", falla02)}");

                // ---------- MP-03: el boton ★ real, quitando el prefijo y volviendolo a poner --
                slot.PlaceItem(368);
                DoEvents();
                slot.SetPrefix(Terrakeep.Core.Model.ItemPrefix.None);
                DoEvents();
                bool ofreceTrasQuitar = slot.HasBestPrefixSuggestion;
                slot.ApplyBestPrefixCommand.Execute(null);
                DoEvents();
                byte trasBoton = slot.Item.Prefix.VanillaId;
                Console.WriteLine($"MP-03: Excalibur sin prefijo -> boton ofrecido={ofreceTrasQuitar} (esperado True); tras pulsarlo -> prefijo {trasBoton} \"{slot.PrefixDisplay}\" (esperado 81)");
                if (!ofreceTrasQuitar || trasBoton != 81)
                    Console.WriteLine("FALLO: MP-03 - el boton \"Mejor prefijo\" no repone el prefijo real en un objeto que antes no tenia cobertura");

                // ---------- MP-04: lo que NO debe llevar prefijo sigue sin llevarlo ------------
                // 15 accesorios de VANIDAD y 1 de la lista negra ItemID.Sets.CanGetPrefixes
                // estaban en la tabla vieja por error: en el juego real
                // Item.IsAPrefixableAccessory() los rechaza (accessory && !vanity && CanGetPrefixes).
                // Se retiraron a proposito, asi que aqui no debe aparecer ni prefijo ni boton.
                var noPrefijables = new (int Id, string Nombre)[]
                {
                    (4054, "Monolito de la luna sangrienta (accesorio de vanidad)"),
                    (5075, "Cursor arcoiris (accesorio de vanidad)"),
                    (1307, "Muñeco vudu del sastre (lista negra CanGetPrefixes)"),
                };
                var falla04 = new List<string>();
                foreach (var (id, nombre) in noPrefijables)
                {
                    slot.PlaceItem(id);
                    DoEvents();
                    bool tienePrefijo = !string.IsNullOrEmpty(slot.PrefixDisplay);
                    bool ofrece = slot.HasBestPrefixSuggestion;
                    Console.WriteLine($"MP-04: {nombre} (id {id}) -> prefijo=\"{slot.PrefixDisplay}\" (esperado vacio), boton activo={ofrece} (esperado False)");
                    if (tienePrefijo || ofrece)
                        falla04.Add($"{nombre} (id {id})");
                }
                if (falla04.Count > 0)
                    Console.WriteLine($"FALLO: MP-04 - {falla04.Count} objeto(s) que el juego real NO deja prefijar siguen recibiendo uno: {string.Join(" ; ", falla04)}");

                // ---------- MP-05: Calamity, lo viejo intacto y lo nuevo cubierto ---------------
                // Las 996 entradas de Calamity se conservaron sin tocar ni una; solo se añadieron
                // 23 (17 alas + StatMeter + 5 armas que clonan un objeto vanilla). Ids sinteticos
                // = 20000000 + indice real en catalog.json, igual que ya hace OBJ-06.
                var casosCalamity = new (int Id, string Nombre, byte Esperado, string Nota)[]
                {
                    (20000000, "Abaddon (accesorio)", 72, "entrada YA existente: no debe cambiar"),
                    (20000206, "Alas Eliseas", 72, "entrada NUEVA: las 17 alas no tenian ninguna"),
                };
                var falla05 = new List<string>();
                foreach (var (id, nombre, esperado, nota) in casosCalamity)
                {
                    slot.PlaceItem(id);
                    DoEvents();
                    byte obtenido = slot.Item.Prefix.VanillaId;
                    Console.WriteLine($"MP-05: {nombre} (id {id}) -> prefijo {obtenido} \"{slot.PrefixDisplay}\" (esperado {esperado}) [{nota}]");
                    if (obtenido != esperado) falla05.Add($"{nombre} (id {id}): esperado {esperado}, obtenido {obtenido}");
                }
                if (falla05.Count > 0)
                    Console.WriteLine($"FALLO: MP-05 - {falla05.Count} objeto(s) de Calamity con el prefijo equivocado: {string.Join(" ; ", falla05)}");

                // Se deja el hueco como estaba (ver el comentario de arriba).
                if (!original.IsEmpty) slot.PlaceItem(original.Id);
                else slot.ClearCommand.Execute(null);
                DoEvents();
            }
            finally
            {
                vm.SelectedTabIndex = tabPrevio;
                vm.PersonajeInnerTabIndex = innerPrevio;
                DoEvents();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FALLO: MP-* - excepcion no controlada: " + ex);
        }
    }
}
