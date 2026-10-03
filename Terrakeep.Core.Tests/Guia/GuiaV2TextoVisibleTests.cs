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
