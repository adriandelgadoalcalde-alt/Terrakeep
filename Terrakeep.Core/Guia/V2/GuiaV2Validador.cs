using System.Text.RegularExpressions;
using Terrakeep.Core.Calamity;

namespace Terrakeep.Core.Guia.V2;

// Validador de contenido de la guia v2 (gate de F0 y de cualquier cambio de datos posterior).
// Comprueba, sin necesitar el juego ni la app:
//   - que toda referencia a objeto/NPC (en textos, condiciones, invocaciones, "necesitas",
//     "conserva", escaleras, jefes, ubicaciones) existe en la tabla de referencias con un nombre
//     (en español, o el original marcado "sin traduccion") y, si es vanilla, con su id real;
//   - que toda tarea tiene una condicion de vocabulario CONOCIDO (tipo, bandera, clave) o esta
//     marcada "manual": nada queda "evaluable" en apariencia y mudo en la practica;
//   - que los enlaces internos (paradas, articulos, zonas) existen y los ids son unicos.
// Devuelve la lista de errores; vacia = valido.
public static class GuiaV2Validador
{
    public static readonly IReadOnlySet<string> ModosConocidos = new HashSet<string>
    {
        "clasico", "experto", "maestro", "viaje", "revengeance", "death",
    };

    public static readonly IReadOnlySet<string> PuntosConocidos = new HashSet<string>
    {
        // vanilla (cabecera del .wld)
        "spawn", "mazmorra",
        // Calamity (MiscWorldStateSystem del .twld, ver CalamityEstadoGuardado)
        "SunkenSeaLabCenter", "PlanetoidLabCenter", "JungleLabCenter", "HellLabCenter", "IceLabCenter", "CavernLabCenter",
    };

    public static readonly IReadOnlySet<string> MejorasVanilla = new HashSet<string>
    {
        "demonHeart", "torchGod", "artisanBread", "aegisCrystal", "aegisFruit", "arcaneCrystal", "galaxyPearl", "gummyWorm", "ambrosia",
    };

    private static readonly Regex Token = new(@"\{([onzpa]):([^}|]+)(\|[^}]*)?\}", RegexOptions.Compiled);

    public static List<string> Validar(GuiaV2Doc doc, GuiaV2Referencias refs)
    {
        var e = new List<string>();
        var paradas = Unicos(doc.Paradas.Select(p => p.Id), "parada", e);
        var articulos = Unicos(doc.Articulos.Select(a => a.Id), "articulo", e);
        var zonas = Unicos(doc.Zonas.Select(z => z.Id), "zona", e);
        var capitulos = Unicos(doc.Capitulos.Select(c => c.Id), "capitulo", e);
        Unicos(doc.Paradas.SelectMany(p => p.Tareas).Select(t => t.Id), "tarea", e);

        void Texto(string? t, string donde)
        {
            if (string.IsNullOrEmpty(t)) return;
            foreach (Match m in Token.Matches(t))
            {
                string tipo = m.Groups[1].Value, valor = m.Groups[2].Value;
                switch (tipo)
                {
                    case "o": RefObjeto(valor, donde); break;
                    case "n": RefNpc(valor, donde); break;
                    case "z": if (!zonas.Contains(valor)) e.Add($"{donde}: zona desconocida '{valor}'"); break;
                    case "p": if (!paradas.Contains(valor)) e.Add($"{donde}: parada desconocida '{valor}'"); break;
                    case "a": if (!articulos.Contains(valor)) e.Add($"{donde}: articulo desconocido '{valor}'"); break;
                }
            }
            int abiertas = t.Count(c => c == '{'), cerradas = t.Count(c => c == '}');
            if (abiertas != cerradas) e.Add($"{donde}: llaves desparejadas en el texto");
            if (Regex.Matches(t, @"\*\*").Count % 2 != 0) e.Add($"{donde}: negrita '**' sin cerrar");
        }

        void RefObjeto(string r, string donde)
        {
            if (!refs.Objetos.TryGetValue(r, out var o)) { e.Add($"{donde}: objeto sin referencia '{r}'"); return; }
            if (string.IsNullOrWhiteSpace(o.Es)) e.Add($"{donde}: objeto '{r}' sin nombre");
            if (r.StartsWith("Terraria/", StringComparison.Ordinal) && (o.Id is null or <= 0)) e.Add($"{donde}: objeto vanilla '{r}' sin id");
        }

        void RefNpc(string r, string donde)
        {
            if (!refs.Npcs.TryGetValue(r, out var n)) { e.Add($"{donde}: NPC sin referencia '{r}'"); return; }
            if (string.IsNullOrWhiteSpace(n.Es)) e.Add($"{donde}: NPC '{r}' sin nombre");
            if (r.StartsWith("Terraria/", StringComparison.Ordinal) && n.Id is null or <= 0) e.Add($"{donde}: NPC vanilla '{r}' sin id");
        }

        void Bloques(IEnumerable<Bloque> bloques, string donde)
        {
            foreach (var b in bloques)
            {
                Texto(b.Texto, donde);
                Texto(b.Titulo, donde);
                foreach (var i in b.Items) Texto(i, donde);
                foreach (var c in b.Cabeceras) Texto(c, donde);
                foreach (var f in b.Filas) foreach (var c in f) Texto(c, donde);
                Bloques(b.Bloques, donde);
            }
        }

        void Cond(Condicion? c, string donde)
        {
            if (c == null) return;
            if (c.Tipo is "todas" or "alguna")
            {
                if (c.Condiciones is not { Count: > 0 }) e.Add($"{donde}: '{c.Tipo}' sin condiciones");
                else foreach (var h in c.Condiciones) Cond(h, donde);
                return;
            }
            var tipo = GuideCatalog.TipoDesdeTexto(c.Tipo);
            switch (tipo)
            {
                case TipoRequisitoGuia.Desconocido:
                    e.Add($"{donde}: tipo de condicion desconocido '{c.Tipo}'");
                    break;
                case TipoRequisitoGuia.Bandera:
                    if (string.IsNullOrEmpty(c.Bandera) || !GuideFlags.Existe(c.Bandera)) e.Add($"{donde}: bandera desconocida '{c.Bandera}'");
                    break;
                case TipoRequisitoGuia.MejoraPermanente:
                    if (c.Clave == null || !(MejorasVanilla.Contains(c.Clave) || CalamityEstadoGuardado.MejorasConocidas.Contains(c.Clave)))
                        e.Add($"{donde}: mejora permanente desconocida '{c.Clave}'");
                    break;
                case TipoRequisitoGuia.EstadoMundo:
                    if (c.Clave == null || !(c.Clave == "mundoCarmesi" || CalamityEstadoGuardado.EstadosMundoConocidos.Contains(c.Clave)))
                        e.Add($"{donde}: estado de mundo desconocido '{c.Clave}'");
                    break;
                case TipoRequisitoGuia.Objeto or TipoRequisitoGuia.ObjetoCualquiera or TipoRequisitoGuia.ObjetoPoseido or TipoRequisitoGuia.Equipado:
                    if (string.IsNullOrEmpty(c.Ref) && c.Refs is not { Count: > 0 }) e.Add($"{donde}: condicion de objeto sin ref");
                    if (!string.IsNullOrEmpty(c.Ref)) RefObjeto(c.Ref, donde);
                    foreach (var r in c.Refs ?? []) RefObjeto(r, donde);
                    break;
                case TipoRequisitoGuia.Npc or TipoRequisitoGuia.NpcActivo:
                    if (string.IsNullOrEmpty(c.Ref)) e.Add($"{donde}: condicion de NPC sin ref");
                    else RefNpc(c.Ref, donde);
                    break;
            }
        }

        void Fuentes(IEnumerable<Fuente> fuentes, string donde)
        {
            foreach (var f in fuentes)
                if (string.IsNullOrEmpty(f.Url) && (string.IsNullOrEmpty(f.Pagina) || f.Wiki is not ("calamity" or "terraria")))
                    e.Add($"{donde}: fuente incompleta");
        }

        foreach (var p in doc.Paradas)
        {
            string d = "parada " + p.Id;
            if (!capitulos.Contains(p.Capitulo)) e.Add($"{d}: capitulo desconocido '{p.Capitulo}'");
            if (string.IsNullOrWhiteSpace(p.Titulo)) e.Add($"{d}: sin titulo");
            foreach (var j in p.Jefes) RefNpc(j, d);
            foreach (string t in new[] { p.Donde, p.Preparate, p.Combate, p.Desbloquea, p.ListoCuando, p.Conserva, p.VidaObjetivo?.Texto ?? "" })
                Texto(t, d);
            foreach (var kv in p.PreparatePorClase)
            {
                if (GuiaV2Evaluador.ClaseDesdeClave(kv.Key) == null) e.Add($"{d}: clase desconocida '{kv.Key}'");
                Texto(kv.Value, d);
            }
            if (p.Invocacion != null)
            {
                if (!string.IsNullOrEmpty(p.Invocacion.Objeto)) RefObjeto(p.Invocacion.Objeto, d + " (invocacion)");
                Texto(p.Invocacion.Donde, d);
                Texto(p.Invocacion.Notas, d);
            }
            foreach (var n in p.Necesitas) { RefObjeto(n.Ref, d + " (necesitas)"); Texto(n.Motivo, d); Clases(n.Clases, d, e); }
            foreach (var r in p.ConservaObjetos) RefObjeto(r, d + " (conserva)");
            Cond(p.CompletadaCuando, d + " (completadaCuando)");
            if (p.Ubicaciones.Count == 0) e.Add($"{d}: sin ubicacion objetivo");
            foreach (var u in p.Ubicaciones)
            {
                switch (u.Tipo)
                {
                    case "zona": if (!zonas.Contains(u.Id)) e.Add($"{d}: ubicacion con zona desconocida '{u.Id}'"); break;
                    case "npc" or "jefe": RefNpc(u.Id, d + " (ubicacion)"); break;
                    case "punto": if (!PuntosConocidos.Contains(u.Id)) e.Add($"{d}: punto desconocido '{u.Id}'"); break;
                    default: e.Add($"{d}: tipo de ubicacion desconocido '{u.Tipo}'"); break;
                }
                if (u.SiMundo is not ("" or "corrupcion" or "carmesi")) e.Add($"{d}: siMundo invalido '{u.SiMundo}'");
                Texto(u.Nota, d);
            }
            foreach (var a in p.Avisos) Aviso(a, d, e, Texto);
            Fuentes(p.Fuentes, d);
            foreach (var t in p.Tareas)
            {
                string dt = "tarea " + t.Id;
                if (!t.Id.StartsWith(p.Id + ".", StringComparison.Ordinal)) e.Add($"{dt}: el id no empieza por '{p.Id}.'");
                Texto(t.Texto, dt);
                Clases(t.Clases, dt, e);
                if (t.Condicion == null && !t.Manual) e.Add($"{dt}: sin condicion y sin marcar manual");
                Cond(t.Condicion, dt);
            }
        }

        foreach (var a in doc.Articulos) { Texto(a.Titulo, "articulo " + a.Id); Texto(a.Subtitulo, "articulo " + a.Id); Bloques(a.Bloques, "articulo " + a.Id); }
        foreach (var z in doc.Zonas)
        {
            if (string.IsNullOrWhiteSpace(z.Nombre)) e.Add($"zona {z.Id}: sin nombre");
            if (z.Punto != null && !PuntosConocidos.Contains(z.Punto)) e.Add($"zona {z.Id}: punto desconocido '{z.Punto}'");
            if (z.Firma.Tiles.Count == 0 && z.Firma.TilesMod.Count == 0 && z.Punto == null && z.Capa == "cualquiera")
                e.Add($"zona {z.Id}: sin firma ni punto ni capa: no se puede situar en el mundo");
            Texto(z.Resumen, "zona " + z.Id);
            Bloques(z.Bloques, "zona " + z.Id);
            Fuentes(z.Fuentes, "zona " + z.Id);
        }
        foreach (var lista in new[] { doc.Problemas, doc.Hallazgos })
            foreach (var f in lista)
            {
                string d = "ficha " + f.Id;
                Texto(f.Titulo, d);
                Bloques(f.Bloques, d);
                foreach (var p in f.Paradas) if (!paradas.Contains(p)) e.Add($"{d}: parada desconocida '{p}'");
                foreach (var z in f.Zonas) if (!zonas.Contains(z)) e.Add($"{d}: zona desconocida '{z}'");
                Fuentes(f.Fuentes, d);
            }
        foreach (var a in doc.AvisosModo) Aviso(a, "avisosModo", e, Texto);
        foreach (var esc in doc.Escaleras)
        {
            string d = "escalera " + esc.Clase;
            if (!doc.Clases.Contains(esc.Clase)) e.Add($"{d}: clase no declarada en la guia");
            foreach (var et in esc.Etapas)
            {
                if (!paradas.Contains(et.Desde)) e.Add($"{d}/{et.Id}: parada 'desde' desconocida '{et.Desde}'");
                Texto(et.Momento, d);
                Texto(et.Nota, d);
                foreach (var o in et.Armas.Concat(et.Armadura).Concat(et.Accesorios).Concat(et.Otros)) { RefObjeto(o.Ref, $"{d}/{et.Id}"); Texto(o.Origen, d); }
                Fuentes(et.Fuentes, d);
            }
        }
        foreach (string c in doc.Clases) if (GuiaV2Evaluador.ClaseDesdeClave(c) == null) e.Add($"clase desconocida '{c}'");

        return e;
    }

    private static void Clases(List<string> clases, string donde, List<string> e)
    {
        foreach (var c in clases) if (GuiaV2Evaluador.ClaseDesdeClave(c) == null) e.Add($"{donde}: clase desconocida '{c}'");
    }

    private static void Aviso(AvisoModo a, string donde, List<string> e, Action<string?, string> texto)
    {
        foreach (var m in a.Modos) if (!ModosConocidos.Contains(m)) e.Add($"{donde}: modo desconocido '{m}'");
        texto(a.Texto, donde);
    }

    private static HashSet<string> Unicos(IEnumerable<string> ids, string que, List<string> e)
    {
        var vistos = new HashSet<string>();
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) e.Add($"{que} con id vacio");
            else if (!vistos.Add(id)) e.Add($"{que} duplicado '{id}'");
        }
        return vistos;
    }
}

/// <summary>Cifras de contenido (gate de profundidad del encargo).</summary>
public sealed record GuiaV2Cifras(int Capitulos, int Articulos, int Paradas, int Tareas, int TareasEvaluables, int TareasManuales,
    int Palabras, int Zonas, int EtapasEscalera, int Problemas, int Hallazgos)
{
    public double PorcentajeEvaluable => Tareas == 0 ? 0 : 100.0 * TareasEvaluables / Tareas;

    public static GuiaV2Cifras Calcular(GuiaV2Doc doc, GuiaV2Referencias refs)
    {
        var textos = new List<string>();
        void B(IEnumerable<Bloque> bs)
        {
            foreach (var b in bs)
            {
                textos.Add(b.Titulo); textos.Add(b.Texto); textos.AddRange(b.Items); textos.AddRange(b.Cabeceras);
                foreach (var f in b.Filas) textos.AddRange(f);
                B(b.Bloques);
            }
        }
        foreach (var p in doc.Paradas)
        {
            textos.AddRange([p.Titulo, p.Etiqueta, p.Donde, p.Preparate, p.Combate, p.Desbloquea, p.ListoCuando, p.Conserva, p.VidaObjetivo?.Texto ?? ""]);
            textos.AddRange(p.PreparatePorClase.Values);
            if (p.Invocacion != null) textos.AddRange([p.Invocacion.Donde, p.Invocacion.Notas]);
            textos.AddRange(p.Tareas.Select(t => t.Texto));
            textos.AddRange(p.Avisos.Select(a => a.Texto));
            textos.AddRange(p.Necesitas.Select(n => n.Motivo));
        }
        foreach (var a in doc.Articulos) { textos.Add(a.Titulo); textos.Add(a.Subtitulo); B(a.Bloques); }
        foreach (var z in doc.Zonas) { textos.Add(z.Resumen); B(z.Bloques); }
        foreach (var f in doc.Problemas.Concat(doc.Hallazgos)) { textos.Add(f.Titulo); B(f.Bloques); }
        foreach (var a in doc.AvisosModo) textos.Add(a.Texto);
        foreach (var et in doc.Escaleras.SelectMany(e => e.Etapas)) { textos.Add(et.Momento); textos.Add(et.Nota); }

        int palabras = textos.Sum(t => GuiaV2Texto.Plano(t, refs).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
        var tareas = doc.Paradas.SelectMany(p => p.Tareas).ToList();
        int evaluables = tareas.Count(t => t.Condicion != null);
        return new GuiaV2Cifras(doc.Capitulos.Count, doc.Articulos.Count, doc.Paradas.Count, tareas.Count, evaluables,
            tareas.Count - evaluables, palabras, doc.Zonas.Count, doc.Escaleras.Sum(e => e.Etapas.Count),
            doc.Problemas.Count, doc.Hallazgos.Count);
    }
}
