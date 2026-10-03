using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Terrakeep.Core.Guia.V2;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.Guia;

// Parche de pulido de la Guia v2 (3-oct-2026, Terrakeep 3.4.1 / TerrakeepMod 0.8.1): CANARIO de que
// el jugador no ve restos tecnicos. Nace de las capturas de la release 3.4.0 / 0.8.0, donde la
// ficha de un objeto enseñaba "CalamityMod.Items.SummonItems.DesertMedallion.cs:56", el nombre
// interno "CalamityMod/BurntSienna" bajo un sprite, el nombre ingles junto al español y
// "40 × Cualquiera Bloque de arena". Lo que se comprueba, sobre los .json incrustados (los mismos
// que pintan Terrakeep y TerrakeepMod):
//   1. Todo texto visible de las dos guias, ya con sus marcas {o:..}/{n:..}/{z:..} resueltas,
//      carece de rutas de codigo, nombres internos Mod/Clase, claves "Guia.*", marcas sin resolver
//      y caracteres rotos.
//   2. Todos los nombres de la tabla de referencias (objetos, NPC, estaciones, grupos de receta).
//   3. Cada condicion de receta/botin/tienda que ve la ficha sale en forma legible, nunca como codigo.
//   4. Los grupos de receta se escriben como frase ("Cualquier bloque de arena").
public class GuiaV2TextoVisibleTests(ITestOutputHelper salida)
{
    private static readonly Lazy<GuiaV2Referencias> Refs = new(GuiaV2Cargador.CargarReferenciasIncrustadas);

    /// <summary>Patrones que NUNCA deben aparecer en el texto visible. Cada uno con su porque.</summary>
    internal static readonly (string Nombre, Regex Patron)[] Prohibidos =
    [
        ("marca sin resolver", new Regex(@"[{}]")),
        ("nombre interno Mod/Clase", new Regex(@"\b(CalamityMod|Terraria|ModLoader)/\w")),
        ("ruta de codigo (.cs)", new Regex(@"\.cs\b|\.cs:\d")),
        ("espacio de nombres de codigo", new Regex(@"\b(CalamityMod|Terraria)\.[A-Z]\w+")),
        ("clave de localizacion Guia.*", new Regex(@"\bGuia(V2)?\.[A-Za-z]")),
        ("identificador de codigo (Clase.Miembro)", new Regex(@"\b[A-Z][A-Za-z]+\.[A-Za-z]+(\(|\b)(?<!\bn\.)(?<!\bp\.)(?<!\bs\.)(?<!\bEj\.)")),
        ("expresion de codigo", new Regex(@"=>|\(\)|\bout var\b|\bnew Condition\b")),
        ("caracter roto", new Regex("�|Ã.|â€")),
        ("\"Cualquiera\" + Nombre (concatenacion del juego)", new Regex(@"\bCualquiera [A-ZÁÉÍÓÚ]")),
    ];

    private static IEnumerable<string> Problemas(string texto)
    {
        foreach (var (nombre, patron) in Prohibidos)
        {
            var m = patron.Match(texto);
            if (m.Success) yield return $"{nombre}: «{m.Value}» en «{Recorte(texto, m.Index)}»";
        }
    }

    private static string Recorte(string t, int i) => t.Substring(Math.Max(0, i - 25), Math.Min(t.Length - Math.Max(0, i - 25), 70)).Replace('\n', ' ');

    // ---- 1. texto visible de las guias ---------------------------------------------------------

    // Propiedades de GuiaV2Modelo cuyo contenido SI ve el jugador. El resto son ids, enlaces,
    // banderas y referencias (que son internas a proposito).
    private static readonly HashSet<string> Visibles = new(StringComparer.Ordinal)
    {
        "Titulo", "Subtitulo", "Resumen", "Texto", "Donde", "Preparate", "Combate", "Desbloquea", "ListoCuando", "Conserva",
        "Etiqueta", "Notas", "Nota", "Motivo", "Rol", "Conjunto", "Marcas", "Momento", "Nombre", "Items", "Cabeceras",
        "Filas", "Creditos", "Origen", "PreparatePorClase", "LeyendaEscaleras",
    };

    private static void Recoger(object? o, string ruta, List<(string Ruta, string Texto)> salida, HashSet<object> vistos)
    {
        if (o == null) return;
        if (o is string) return;
        var t = o.GetType();
        if (t.IsPrimitive || t.IsEnum) return;
        if (!vistos.Add(o)) return;
        if (o is IDictionary d)
        {
            foreach (DictionaryEntry kv in d)
            {
                // Las cadenas sueltas de una lista/diccionario NO visible son ids o referencias: se ignoran.
                if (kv.Value is not string) Recoger(kv.Value, ruta + "/" + kv.Key, salida, vistos);
            }
            return;
        }
        if (o is IEnumerable e)
        {
            int i = 0;
            foreach (var x in e)
            {
                if (x is not string) Recoger(x, ruta + $"[{i}]", salida, vistos);
                i++;
            }
            return;
        }
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0 || p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null) continue;
            object? v = p.GetValue(o);
            if (!Visibles.Contains(p.Name))
            {
                if (v is not string && v != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string)) Recoger(v, ruta + "/" + p.Name, salida, vistos);
                continue;
            }
            if (v is string s) salida.Add((ruta + "/" + p.Name, s));
            else if (v is IDictionary dd) { foreach (DictionaryEntry kv in dd) if (kv.Value is string sv2) salida.Add((ruta + "/" + p.Name + "/" + kv.Key, sv2)); }
            else if (v is IEnumerable ee) { int i = 0; foreach (var x in ee) { if (x is string sx) salida.Add((ruta + "/" + p.Name + $"[{i}]", sx)); else Recoger(x, ruta + "/" + p.Name + $"[{i}]", salida, vistos); i++; } }
            else Recoger(v, ruta + "/" + p.Name, salida, vistos);
        }
    }

    [Theory]
    [MemberData(nameof(Guias))]
    public void TextoVisibleDeLaGuia_NoTieneRestosTecnicos(string id)
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada(id);
        var refs = Refs.Value;
        var textos = new List<(string, string)>();
        Recoger(doc, "", textos, new HashSet<object>(ReferenceEqualityComparer.Instance));
        Assert.True(textos.Count > 500, "el recorrido reflexivo apenas encontro texto (" + textos.Count + "): la lista Visibles se ha desalineado del modelo");

        string? Zona(string z) => doc.Zonas.FirstOrDefault(x => x.Id == z)?.Nombre;
        string? Parada(string p) => doc.Paradas.FirstOrDefault(x => x.Id == p)?.Titulo;
        string? Articulo(string a) => doc.Articulos.FirstOrDefault(x => x.Id == a)?.Titulo;

        var fallos = new List<string>();
        foreach (var (ruta, texto) in textos)
        {
            // Una marca que no resuelve cae en su valor crudo ("CalamityMod/X"): el patron lo caza.
            string plano = GuiaV2Texto.Plano(texto, refs, Zona, Parada, Articulo);
            // Titulos de parada/articulo citados con {p:}/{a:} pueden llevar a su vez marcas.
            plano = GuiaV2Texto.Plano(plano, refs, Zona, Parada, Articulo);
            foreach (var p in Problemas(plano)) fallos.Add($"{id}{ruta}: {p}");
        }
        foreach (var f in fallos.Take(60)) salida.WriteLine(f);
        Assert.True(fallos.Count == 0, $"{fallos.Count} restos tecnicos visibles en la guia '{id}' (primeros en la salida de la prueba).");
    }

    // ---- 1b. palabras inglesas sueltas dentro de un texto en español ---------------------------
    // Hallazgo del 3-oct-2026: la escalera de Calamity decia «busca islas flotantes y Planetoids»
    // (y la guia arrastraba melee, dash, buff, build, Aerialite, Vernal Pass...). Dos redes:
    //   A) lista fija de palabras y frases inglesas de Terraria y Calamity que tienen traduccion oficial
    //      o de CalamityKeep-Traduccion-ES (glosario) - la lista sale de lo que el escaneo encontro;
    //   B) todos los nombres INGLESES de la tabla de referencias (objetos, NPC, estaciones) de 2+ palabras
    //      (o de 7+ letras) cuyo nombre español es distinto: si aparecen tal cual en un texto, es un nombre
    //      oficial que debia salir en español.
    // Quedan fuera, a proposito: lo que va entre «comillas angulares» (mencion deliberada del nombre antiguo),
    // las direcciones web, y los nombres que NO se traducen por glosario (Boss Rush, Revengeance, mods ajenos).

    private static readonly string[] PalabrasInglesas =
    [
        "melee", "ranged", "rogue", "stealth", "minion", "minions", "summon", "summoner", "dash", "dashes", "parry",
        "cooldown", "aggro", "buff", "buffs", "debuff", "debuffs", "build", "builds", "boomerang", "boomerangs",
        "hardcore", "planetoid", "planetoids", "aerialite", "cosmilite", "auric", "skyware", "shimmer", "aether",
        "vanilla", "spam", "tier", "ticks", "pets", "dyes", "mounts", "lore", "shrine", "shrines", "enrage",
        "irradiated", "death", "master", "expert", "hardmode", "campfire", "bullet hell", "damage reduction",
        "defense damage", "crafting station", "non-consumable", "schematic", "decrypt", "evil island",
        "forsaken archive", "vernal pass", "abandoned shed", "acid venom", "biome key", "solar eclipse",
        "gravitation potion", "queen bee", "moon lord", "wall of flesh", "eye of cthulhu", "eater of worlds",
        "brain of cthulhu", "king slime", "queen slime", "duke fishron", "lunatic cultist", "empress of light",
        "ichor", "probes", "bloodworms", "counterweights", "instead of", "help", "enchanting", "evoke",
    ];

    /// <summary>Frases que contienen una palabra inglesa de la lista pero NO son un resto: se quitan antes de buscar.</summary>
    private static readonly string[] FrasesPermitidas =
    [
        "Boss Rush", "Boss Checklist", "Recipe Browser", "Magic Storage", "Get fixed boi", "Guide:Class setups",
    ];

    private static readonly Regex Direcciones = new(@"https?://\S+|\b\S+\.wiki\.gg\S*", RegexOptions.Compiled);
    private static readonly Regex Menciones = new("«[^»]*»", RegexOptions.Compiled);

    /// <summary>El texto tal como lo lee un jugador, sin menciones entre «..», direcciones ni frases permitidas.</summary>
    internal static string TextoSinMenciones(string plano)
    {
        string t = Menciones.Replace(plano, " ");
        t = Direcciones.Replace(t, " ");
        foreach (var f in FrasesPermitidas) t = t.Replace(f, " ", StringComparison.Ordinal);
        return t;
    }

    private static Regex PatronDePalabras(IEnumerable<string> frases)
    {
        var alt = string.Join("|", frases.OrderByDescending(f => f.Length).Select(Regex.Escape));
        return new Regex(@"(?<![\p{L}\p{N}_])(" + alt + @")(?![\p{L}\p{N}_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private static readonly Lazy<Regex> RedLista = new(() => PatronDePalabras(PalabrasInglesas));

    // Nombres ingleses de la tabla que no deben verse en un texto en español. Se excluyen los que son
    // palabras de otro idioma legitimas en el texto (el propio nombre del mod, de la wiki, etc.).
    private static readonly HashSet<string> NombresInglesesAjenos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Calamity", "Terraria", "Boss Rush", "Revengeance",
    };

    private static readonly Lazy<Regex> RedNombres = new(() =>
    {
        var r = Refs.Value;
        var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Anade(string? en, string? es)
        {
            if (string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(es)) return;
            if (string.Equals(en, es, StringComparison.OrdinalIgnoreCase)) return;
            bool varias = en.Contains(' ');
            if (!varias && en.Length < 7) return;
            if (NombresInglesesAjenos.Contains(en)) return;
            // Nombre ingles que ademas es una palabra o frase española valida (p. ej. "Terminus"): no se persigue.
            if (es.Contains(en, StringComparison.OrdinalIgnoreCase)) return;
            nombres.Add(en);
        }
        foreach (var kv in r.Objetos) Anade(kv.Value.En, kv.Value.Es);
        foreach (var kv in r.Npcs) Anade(kv.Value.En, kv.Value.Es);
        foreach (var kv in r.Estaciones) Anade(kv.Value.En, kv.Value.Es);
        return PatronDePalabras(nombres);
    });

    private static List<(string Ruta, string Plano)> TextosPlanos(string id)
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada(id);
        var refs = Refs.Value;
        var textos = new List<(string, string)>();
        Recoger(doc, "", textos, new HashSet<object>(ReferenceEqualityComparer.Instance));
        string? Zona(string z) => doc.Zonas.FirstOrDefault(x => x.Id == z)?.Nombre;
        string? Parada(string p) => doc.Paradas.FirstOrDefault(x => x.Id == p)?.Titulo;
        string? Articulo(string a) => doc.Articulos.FirstOrDefault(x => x.Id == a)?.Titulo;
        var salida = new List<(string, string)>();
        foreach (var (ruta, texto) in textos)
        {
            string plano = GuiaV2Texto.Plano(texto, refs, Zona, Parada, Articulo);
            plano = GuiaV2Texto.Plano(plano, refs, Zona, Parada, Articulo);
            salida.Add((ruta, plano));
        }
        return salida;
    }

    [Theory]
    [MemberData(nameof(Guias))]
    public void TextoVisibleDeLaGuia_NoTienePalabrasInglesas(string id)
    {
        var fallos = new List<string>();
        foreach (var (ruta, plano) in TextosPlanos(id))
        {
            string t = TextoSinMenciones(plano);
            foreach (Match m in RedLista.Value.Matches(t)) fallos.Add($"{id}{ruta}: palabra inglesa «{m.Value}» en «{Recorte(t, m.Index)}»");
            foreach (Match m in RedNombres.Value.Matches(t)) fallos.Add($"{id}{ruta}: nombre inglés de la tabla «{m.Value}» en «{Recorte(t, m.Index)}»");
        }
        foreach (var f in fallos.Take(60)) salida.WriteLine(f);
        Assert.True(fallos.Count == 0, $"{fallos.Count} palabras o nombres ingleses visibles dentro del texto en español de la guía '{id}' (primeros en la salida de la prueba).");
    }

    [Theory]
    [InlineData("busca islas flotantes y Planetoids.", true)]
    [InlineData("con una Gravitation Potion, busca islas flotantes", true)]
    [InlineData("Arma melee con alcance", true)]
    [InlineData("practica el dash con doble pulsación lateral", true)]
    [InlineData("Daño de pícaro con sigilo", false)]
    [InlineData("planetoides", false)]
    [InlineData("Boss Rush y Revengeance", false)]
    [InlineData("«Dragon Egg» es el nombre antiguo", false)]
    [InlineData("la guía de https://terraria.wiki.gg/wiki/Fishing y Boss Checklist", false)]
    public void RedDePalabrasInglesas_DetectaLoQueDebeYSoloEso(string texto, bool debeDetectar)
    {
        bool detecta = RedLista.Value.IsMatch(TextoSinMenciones(texto)) || RedNombres.Value.IsMatch(TextoSinMenciones(texto));
        Assert.Equal(debeDetectar, detecta);
    }

    // ---- 2. nombres de la tabla de referencias -------------------------------------------------

    [Fact]
    public void NombresDeLaTablaDeReferencias_SonLimpiosEnLosDosIdiomas()
    {
        var r = Refs.Value;
        var fallos = new List<string>();
        void Mira(string clave, string? es, string? en)
        {
            foreach (var (idioma, t) in new[] { ("es", es), ("en", en) })
                foreach (var p in Problemas(t ?? "")) fallos.Add($"{clave} ({idioma}): {p}");
        }
        foreach (var kv in r.Objetos) Mira("objeto " + kv.Key, kv.Value.Es, kv.Value.En);
        foreach (var kv in r.Npcs) Mira("npc " + kv.Key, kv.Value.Es, kv.Value.En);
        foreach (var kv in r.Estaciones) Mira("estacion " + kv.Key, kv.Value.Es, kv.Value.En);
        foreach (var kv in r.Grupos) Mira("grupo " + kv.Key, kv.Value.Es, kv.Value.En);
        foreach (var f in fallos.Take(60)) salida.WriteLine(f);
        Assert.True(fallos.Count == 0, $"{fallos.Count} nombres con restos tecnicos");
    }

    // ---- 3. condiciones ------------------------------------------------------------------------

    private static IEnumerable<string> TodasLasCondiciones()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var o in Refs.Value.Objetos.Values)
            foreach (var ob in o.Obtencion)
            {
                foreach (var c in ob.Condiciones) set.Add(c);
                if (!string.IsNullOrEmpty(ob.Condicion)) set.Add(ob.Condicion);
            }
        return set;
    }

    [Fact]
    public void CondicionesDeLaFicha_NuncaSeEnseñanComoCodigo()
    {
        var todas = TodasLasCondiciones().ToList();
        Assert.True(todas.Count > 200, "pocas condiciones: " + todas.Count);
        var fallos = new List<string>();
        int traducidas = 0;
        foreach (var c in todas)
            foreach (bool es in new[] { true, false })
            {
                string l = GuiaV2Condiciones.Legible(c, es);
                if (l.Length > 0) traducidas++;
                foreach (var p in Problemas(l)) fallos.Add($"«{c}» ({(es ? "es" : "en")}) → «{l}»: {p}");
                // Ni siquiera como palabras sueltas pegadas ("Downed Old Duke", "Drop Helper Post Do G").
                if (Regex.IsMatch(l, @"\b(Downed|Drop ?Helper|Post[A-Z]|Conditions?\b)")) fallos.Add($"«{c}» → «{l}»: nombre de codigo");
            }
        foreach (var f in fallos.Take(60)) salida.WriteLine(f);
        salida.WriteLine("condiciones distintas " + todas.Count + ", con forma legible " + traducidas / 2);
        Assert.True(fallos.Count == 0, $"{fallos.Count} condiciones se cuelan como codigo");
    }

    [Theory]
    [InlineData("CalamityConditions.DownedOldDuke", "tras derrotar al Viejo Duque")]
    [InlineData("Condition.Hardmode", "en Modo Difícil")]
    [InlineData("DropHelper.PostDoG()", "tras derrotar al Devorador de dioses")]
    [InlineData("Condition.InGraveyard", "en un cementerio")]
    [InlineData("() => DownedBossSystem.downedPolterghast", "tras derrotar a Polterghast")]
    [InlineData("solo modo normal (DefineNormalOnlyDropSet)", "solo en modo normal")]
    [InlineData("decraft: Condition.CorruptWorld", "al transmutar: en un mundo corrupto")]
    [InlineData("al menos una de las reglas | una de 1 reglas, 1/4", "")]
    [InlineData("DropHelper.GFB | por jugador (PerPlayer)", "")]
    [InlineData("(DropAttemptInfo info) => !ShouldNotDropThings(info.npc)", "")]
    [InlineData("ArsenalTierGatedRecipe.ConstructRecipeCondition(3, out var condition)", "con el arsenal de Calamity del nivel 3")]
    [InlineData("", "")]
    public void CondicionLegible_EjemplosReales(string entrada, string esperada)
    {
        Assert.Equal(esperada, GuiaV2Condiciones.Legible(entrada));
    }

    // ---- 4. grupos de receta -------------------------------------------------------------------

    [Fact]
    public void GruposDeReceta_SeEscribenComoFrase()
    {
        var g = Refs.Value.Grupos;
        Assert.Equal("Cualquier bloque de arena", g["Sand"].Es);
        Assert.Equal("Cualquier madera", g["Wood"].Es);
        Assert.Equal("Cualquier lingote de hierro", g["IronBar"].Es);
        foreach (var kv in g)
        {
            Assert.DoesNotMatch(@"^Cualquiera [A-ZÁÉÍÓÚ]", kv.Value.Es);
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.En), kv.Key);
            // Tras "Cualquier" va minuscula: es una frase, no "Cualquiera Bloque".
            if (kv.Value.Es.StartsWith("Cualquier ", StringComparison.Ordinal))
                Assert.True(char.IsLower(kv.Value.Es["Cualquier ".Length]), kv.Key + ": " + kv.Value.Es);
        }
    }

    public static TheoryData<string> Guias()
    {
        var d = new TheoryData<string>();
        foreach (var g in GuiaV2Cargador.GuiasDisponibles()) d.Add(g);
        return d;
    }
}
