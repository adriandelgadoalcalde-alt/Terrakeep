using System.Text;
using System.Text.RegularExpressions;

namespace Terrakeep.Core.Guia.V2;

// Parche de pulido de la Guia v2 (3-oct-2026): la tabla de referencias guarda las condiciones de
// receta/botin/tienda TAL CUAL salen del codigo decompilado ("DropHelper.PostDoG()",
// "Condition.InGraveyard", "() => DownedBossSystem.downedPolterghast"...). Eso es un dato para
// desarrolladores: el jugador no debe verlo nunca. Esta clase lo traduce a una frase en español
// (o inglés) o, si no lo reconoce, lo OMITE: nunca se enseña código en la ficha de un objeto.
// Compartida por Terrakeep y TerrakeepMod (el mod empaqueta este mismo ensamblado).
public static class GuiaV2Condiciones
{
    // Clave normalizada (minusculas, sin espacio de nombres ni "()") -> (es, en).
    private static readonly Dictionary<string, (string Es, string En)> Mapa = Crear();

    private static Dictionary<string, (string, string)> Crear()
    {
        var m = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        void A((string Es, string En) t, params string[] claves) { foreach (var c in claves) m[c] = t; }

        // Modo de juego y mundo.
        A(("en Modo Difícil", "in Hardmode"), "hardmode", "ishardmode", "hardmode()", "main.hardmode");
        A(("antes del Modo Difícil", "before Hardmode"), "prehardmode", "!main.hardmode");
        A(("en modo experto", "in Expert Mode"), "isexpert", "expertmode", "main.expertmode", "expert");
        A(("fuera del modo experto", "outside Expert Mode"), "notexpert", "!main.expertmode");
        A(("en un mundo Remix", "in a Remix world"), "remixseed", "remixworld", "remix");
        A(("fuera de un mundo Remix", "outside a Remix world"), "notremixseed", "notremixworld", "notremix");
        A(("en un mundo Remix en Modo Difícil", "in a Remix world in Hardmode"), "remixseedhardmode");
        A(("en un mundo Remix antes del Modo Difícil", "in a Remix world before Hardmode"), "remixseedeasymode");
        A(("en un mundo carmesí", "in a Crimson world"), "crimsonworld", "worldgen.crimson");
        A(("en un mundo corrupto", "in a Corruption world"), "corruptworld", "!worldgen.crimson");
        A(("en un mundo del 10.º aniversario", "in a Celebrationmk10 world"), "tenthanniversaryworld", "tenthanniversaryisup");
        A(("fuera de un mundo del 10.º aniversario", "outside a Celebrationmk10 world"), "tenthanniversaryisnotup");

        // Lugar y entorno.
        A(("en un cementerio", "in a Graveyard"), "ingraveyard");
        A(("en la jungla", "in the Jungle"), "injungle");
        A(("en el Sagrado", "in the Hallow"), "inhallow");
        A(("fuera del Sagrado", "outside the Hallow"), "notinhallow");
        A(("en el desierto", "in the Desert"), "indesert");
        A(("cerca de agua", "near water"), "nearwater");
        A(("cerca de miel", "near honey"), "nearhoney");
        A(("cerca de Éter", "near Shimmer"), "nearshimmer");

        // Tiempo y eventos.
        A(("de noche", "at night"), "timenight");
        A(("de noche o durante un eclipse", "at night or during an eclipse"), "nightoreclipse");
        A(("durante una Luna de sangre", "during a Blood Moon"), "bloodmoon");
        A(("fuera de una Luna de sangre", "outside a Blood Moon"), "notbloodmoon");
        A(("durante un eclipse o una Luna de sangre", "during an eclipse or a Blood Moon"), "eclipseorbloodmoon");
        A(("sin eclipse ni Luna de sangre", "with no eclipse or Blood Moon"), "noteclipseandnotbloodmoon");
        A(("durante una Luna de sangre o en Modo Difícil", "during a Blood Moon or in Hardmode"), "bloodmoonorhardmode");
        A(("con Luna nueva", "on a New Moon"), "moonphasenew");
        A(("con Luna llena", "on a Full Moon"), "moonphasefull");
        A(("con Luna creciente", "on a Waxing Crescent"), "moonphasewaxingcrescent");
        A(("con Luna menguante", "on a Waning Crescent"), "moonphasewaningcrescent");
        A(("con Luna gibosa creciente", "on a Waxing Gibbous"), "moonphasewaxinggibbous");
        A(("con Luna gibosa menguante", "on a Waning Gibbous"), "moonphasewaninggibbous");
        A(("con Luna en cuarto menguante", "on a Third Quarter Moon"), "moonphasethirdquarter");
        A(("según la fase de la Luna", "depending on the Moon phase"),
            "moonphasesodd", "moonphaseseven", "moonphasesoddquarters", "moonphaseshalf0", "moonphaseshalf1",
            "moonphasesquarter0", "moonphasesquarter1", "moonphasesquarter2", "moonphasesquarter3");
        A(("en Navidad", "at Christmas"), "ischristmas", "xmaspresentdrop");

        // Jefes e invasiones derrotados.
        A(("tras derrotar al Rey slime", "after defeating King Slime"), "downedkingslime", "postks");
        A(("tras derrotar al Ojo de Cthulhu", "after defeating the Eye of Cthulhu"), "downedeyeofcthulhu", "posteoc");
        A(("tras derrotar al jefe del mal de tu mundo", "after defeating your world's evil boss"), "downedboss2", "npc.downedboss2");
        A(("tras derrotar a Esqueletrón", "after defeating Skeletron"), "downedskeletron");
        A(("tras derrotar a Plantera", "after defeating Plantera"), "downedplantera", "npc.downedplantboss", "postplant");
        A(("tras derrotar al Gólem", "after defeating the Golem"), "downedgolem", "npc.downedgolemboss");
        A(("tras derrotar al Señor de la Luna", "after defeating the Moon Lord"), "downedmoonlord", "npc.downedmoonlord", "postml");
        A(("tras derrotar a alguno de los jefes mecánicos", "after defeating any mechanical boss"), "downedmechbossany", "beatanymechboss");
        A(("tras derrotar a los tres jefes mecánicos", "after defeating all three mechanical bosses"), "downedmechbossall", "downedallmechbosses");
        A(("tras derrotar a los piratas", "after defeating the pirate invasion"), "downedpirates");
        A(("tras derrotar a los marcianos", "after defeating the Martian invasion"), "downedmartians");
        A(("tras derrotar al Azote del desierto", "after defeating the Desert Scourge"), "downeddesertscourge", "postds");
        A(("tras derrotar a la Mente colmena", "after defeating the Hive Mind"), "downedhivemind");
        A(("tras derrotar a los Perforadores", "after defeating the Perforators"), "downedperforator");
        A(("tras derrotar a Criógeno", "after defeating Cryogen"), "downedcryogen", "postcryo");
        A(("tras derrotar al Clon de Calamitas", "after defeating Calamitas Clone"), "downedcalamitasclone", "postcal");
        A(("tras derrotar al Leviatán", "after defeating the Leviathan"), "downedleviathan", "postlevi");
        A(("tras derrotar a Astrum Aureus", "after defeating Astrum Aureus"), "downedastrumaureus");
        A(("tras derrotar a Astrum Deus", "after defeating Astrum Deus"), "downedastrumdeus");
        A(("tras derrotar al Viejo Duque", "after defeating the Old Duke"), "downedoldduke");
        A(("tras derrotar a Providencia", "after defeating Providence"), "downedprovidence", "postprov");
        A(("tras derrotar a Polterghast", "after defeating Polterghast"), "downedpolterghast", "postpolter");
        A(("tras derrotar al Devorador de dioses", "after defeating the Devourer of Gods"), "downeddog", "postdog");
        A(("tras derrotar a Yharon", "after defeating Yharon"), "downedyharon");
        A(("tras derrotar a Ares", "after defeating Ares"), "downedares");
        A(("tras derrotar a Tánatos", "after defeating Thanatos"), "downedthanatos");
        A(("tras derrotar a Artemisa y Apolo", "after defeating Artemis and Apollo"), "downedartemisandapollo");
        A(("tras derrotar a Calamitas suprema", "after defeating Supreme Calamitas"), "downedsupremecalamitas");
        A(("tras derrotar a Plantera por primera vez", "the first time Plantera is defeated"), "firsttimekillingplantera");
        A(("tras derrotar a Providencia en su modo desafío", "on Providence's challenge mode"), "providencechallengetext");

        // Frases de estado del mundo o del jugador.
        A(("sin haber usado un corazón demoníaco", "without having used a Demon Heart"), "notuseddemonheart");
        A(("con una carta del tesoro pirata", "with a Pirate Map"), "piratemap");
        A(("sin pistola de portales", "without a Portal Gun"), "noportalgun");
        A(("con una llave del bioma correspondiente", "with the matching biome key"),
            "corruptkeycondition", "crimsonkeycondition", "desertkeycondition", "frozenkeycondition", "hallowkeycondition", "junglekeycondition");
        A(("con Almas de Luz", "with Souls of Light"), "souloflight");
        A(("con Almas de Noche", "with Souls of Night"), "soulofnight");
        A(("cuando el Mago está presente", "when the Wizard is present"), "npcispresent(108)");
        return m;
    }

    // Fragmentos de ESTRUCTURA del arbol de reglas de botin (ya en español, pero ruido tecnico).
    private static readonly Regex Ruido = new(
        @"^(al menos una de las reglas|una de \d+ reglas.*|secuencia de reglas.*|bloque:.*|si (no se cumple|falla) .*|condition\d*(\s*=.*)?|"
        + @"adamantitecondition|mythrilcondition|canDropLoot|lastanlstanding|por jugador.*|bolsa de tesoro.*|rerolls?:?.*|modo experto: .* rerolls|"
        + @"hallowedbarscondition|gfb|zenithworld|true|\(\) => true|periodically|yoyos\w+|worldgen\w*|nightafterevilorhardmode|"
        + @"bestiaryfilledpercent.*|happyenoughtosellpylons|golfscoreover.*|playercarriesitem.*|npcispresent.*|"
        + @"revnomaster|ninfo.*|shouldnotdropthings.*|canminionsdropthings.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex Frases = new(
        @"^(?<k>solo modo normal|modo normal|modo experto|solo modo maestro|mundo remix|mundo no remix|segun modo)\b.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Condicion del codigo en forma legible para el jugador, en el idioma pedido. Cadena
    /// vacia si no hay nada que un jugador deba leer (estructura interna del codigo, banderas
    /// de desarrollo...). Nunca devuelve nombres de clases, rutas ni expresiones de codigo.</summary>
    public static string Legible(string? condicion, bool espanol = true)
    {
        if (string.IsNullOrWhiteSpace(condicion)) return "";
        var partes = new List<string>();
        string c = condicion.Trim();
        bool decraft = false;
        if (c.StartsWith("decraft:", StringComparison.OrdinalIgnoreCase))
        {
            decraft = true;
            c = c["decraft:".Length..].Trim();
        }
        foreach (var bruto in Regex.Split(c, @"\s\|\s|;\s*"))
        {
            string? t = Traducir(bruto.Trim(), espanol);
            if (!string.IsNullOrEmpty(t) && !partes.Contains(t, StringComparer.OrdinalIgnoreCase)) partes.Add(t);
        }
        if (partes.Count == 0) return "";
        string unido = string.Join("; ", partes);
        return decraft ? (espanol ? "al transmutar: " : "when transmuting: ") + unido : unido;
    }

    private static string? Traducir(string f, bool espanol)
    {
        if (f.Length == 0) return null;
        // Condiciones ya redactadas en español por el extractor.
        var fr = Frases.Match(f);
        if (fr.Success)
        {
            return fr.Groups["k"].Value.ToLowerInvariant() switch
            {
                "solo modo normal" => espanol ? "solo en modo normal" : "Normal Mode only",
                "modo normal" => espanol ? "en modo normal" : "in Normal Mode",
                "modo experto" => espanol ? "en modo experto" : "in Expert Mode",
                "solo modo maestro" => espanol ? "solo en modo Maestro" : "Master Mode only",
                "mundo remix" => espanol ? "en un mundo Remix" : "in a Remix world",
                "mundo no remix" => espanol ? "fuera de un mundo Remix" : "outside a Remix world",
                _ => null, // "segun modo (DropBasedOnMasterAndExpertMode)": detalle tecnico sin valor para el jugador.
            };
        }
        // Las expresiones lambda y los metodos de ayuda se quedan con su ultimo identificador.
        string clave = f;
        if (clave.StartsWith("() =>", StringComparison.Ordinal)) clave = clave["() =>".Length..].Trim();
        if (clave.StartsWith("(DropAttemptInfo info) =>", StringComparison.Ordinal)) return null;
        int igual = clave.IndexOf(" = new Condition", StringComparison.Ordinal);
        if (igual > 0) clave = clave[..igual];
        if (clave.StartsWith("new Condition(", StringComparison.Ordinal)) return TraducirCondicionNueva(clave, espanol);
        // "ArsenalTierGatedRecipe.ConstructRecipeCondition(3, out var condition)": el nivel de arsenal.
        var arsenal = Regex.Match(clave, @"^ArsenalTierGatedRecipe\.ConstructRecipeCondition\((\d)");
        if (arsenal.Success)
            return espanol ? "con el arsenal de Calamity del nivel " + arsenal.Groups[1].Value : "with Calamity's arsenal tier " + arsenal.Groups[1].Value;
        if (clave.StartsWith("SchematicRecipe.ConstructRecipeCondition(", StringComparison.Ordinal))
            return espanol ? "con el esquema correspondiente" : "with the matching schematic";

        string norm = Normalizar(clave);
        if (Mapa.TryGetValue(norm, out var t)) return espanol ? t.Es : t.En;
        if (Ruido.IsMatch(norm) || Ruido.IsMatch(f)) return null;
        // Condicion de codigo no reconocida: se omite antes que enseñar codigo.
        return null;
    }

    private static string? TraducirCondicionNueva(string expr, bool espanol)
    {
        // new Condition(CalamityUtils.GetText("Condition.HasFoundFrozenCube"), ...): el texto del juego
        // ya es una clave; se traduce la clave conocida, lo demas se omite.
        var k = Regex.Match(expr, @"GetText\(""Condition\.(\w+)""\)");
        if (!k.Success) return null;
        return k.Groups[1].Value switch
        {
            "HasFoundFrozenCube" => espanol ? "con la luna correspondiente tras hallar el Cubo congelado" : "once the Frozen Cube is found, on the matching moon",
            "HasFoundFungalSymbiote" => espanol ? "con la luna correspondiente tras hallar el Simbionte fúngico" : "once the Fungal Symbiote is found, on the matching moon",
            "HasFoundGladiatorsLocket" => espanol ? "con la luna correspondiente tras hallar el Relicario del gladiador" : "once the Gladiator's Locket is found, on the matching moon",
            "HasFoundLuxorsGift" => espanol ? "con la luna correspondiente tras hallar el Regalo de Luxor" : "once Luxor's Gift is found, on the matching moon",
            "HasFoundTrinketOfChi" => espanol ? "con la luna correspondiente tras hallar el Abalorio de Chi" : "once the Trinket of Chi is found, on the matching moon",
            "CrescentMoons" => espanol ? "con Luna creciente" : "on a crescent Moon",
            "GibbousMoons" => espanol ? "con Luna gibosa" : "on a gibbous Moon",
            "HasFlareGun" => espanol ? "llevando una pistola de bengalas" : "carrying a flare gun",
            _ => null,
        };
    }

    private static string Normalizar(string f)
    {
        string c = f.Trim().TrimEnd(';');
        // "NpcIsPresent(108)" conserva el argumento; el resto de "()" vacios se quitan.
        c = Regex.Replace(c, @"\(\)$", "");
        // Quitar espacios de nombres: "Condition.", "Conditions.", "CalamityConditions.", "DropHelper.",
        // "DownedBossSystem.", "Main." (salvo para mantener la negacion y los prefijos utiles).
        bool neg = c.StartsWith('!');
        if (neg) c = c[1..];
        c = Regex.Replace(c, @"^(Condition|Conditions|CalamityConditions|DropHelper|DownedBossSystem)\.", "", RegexOptions.IgnoreCase);
        if (neg) c = "!" + c;
        return c;
    }
}
