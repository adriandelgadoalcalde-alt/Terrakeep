namespace Terrakeep.Core.Guia.V2;

/// <summary>Resuelve una referencia "Mod/NombreInterno" al id de ESTA app: escritorio (id
/// sintetico de CalamityCatalog para Calamity, ItemID para vanilla) o mod (type real de la
/// partida via ModContent.TryFind). Null = no se puede resolver (la condicion queda no evaluable,
/// nunca inventada).</summary>
public interface IResolutorRefsGuia
{
    int? Objeto(string referencia);
    int? Npc(string referencia);
}

/// <summary>Resolutor por defecto: vanilla con el id real de la tabla de referencias
/// (ItemID/NPCID 1.4.4.9) y Calamity con las funciones que pase la app.</summary>
public sealed class ResolutorRefsGuia(GuiaV2Referencias refs, Func<string, int?>? objetoMod = null, Func<string, int?>? npcMod = null)
    : IResolutorRefsGuia
{
    public int? Objeto(string referencia)
    {
        if (referencia.StartsWith("Terraria/", StringComparison.Ordinal))
            return refs.Objetos.TryGetValue(referencia, out var o) ? o.Id : null;
        return objetoMod?.Invoke(referencia);
    }

    public int? Npc(string referencia)
    {
        if (referencia.StartsWith("Terraria/", StringComparison.Ordinal))
            return refs.Npcs.TryGetValue(referencia, out var n) ? n.Id : null;
        return npcMod?.Invoke(referencia);
    }
}

public enum EstadoCondicion
{
    Cumplida = 0,
    NoCumplida,
    /// <summary>Se podria saber cargando personaje/mundo: no cuenta como hecha.</summary>
    SinDatos,
    /// <summary>Este proveedor nunca podra saberlo (limite estructural / referencia sin resolver).</summary>
    NoEvaluable,
}

public sealed class ResultadoCondicion
{
    public required Condicion Condicion { get; init; }
    public EstadoCondicion Estado { get; set; }
    /// <summary>Progreso parcial 0..1 (3 de 4 vecinos = 0,75).</summary>
    public float Fraccion { get; set; }
    /// <summary>Resultado del motor para una hoja (texto/motivo listos para la UI).</summary>
    public ResultadoRequisitoGuia? Hoja { get; set; }
    public List<ResultadoCondicion> Hijos { get; } = [];
}

public sealed class ResultadoTarea
{
    public required Tarea Tarea { get; init; }
    public ResultadoCondicion? Condicion { get; init; }
    public bool MarcadaAMano { get; init; }
    /// <summary>Hecha = condicion cumplida o marcada a mano por el jugador.</summary>
    public bool Hecha => MarcadaAMano || Condicion?.Estado == EstadoCondicion.Cumplida;
    /// <summary>true si la comprueba la propia guia (tiene condicion evaluable por este proveedor).</summary>
    public bool Automatica => Condicion != null && Condicion.Estado is EstadoCondicion.Cumplida or EstadoCondicion.NoCumplida;
}

public sealed class ResultadoParada
{
    public required Parada Parada { get; init; }
    public required int Indice { get; init; }
    public List<ResultadoTarea> Tareas { get; } = [];
    public ResultadoCondicion? CompletadaCuando { get; init; }
    public bool MarcadaAMano { get; init; }
    public bool Aplazada { get; init; }
    public bool Completada => MarcadaAMano || CompletadaCuando?.Estado == EstadoCondicion.Cumplida;
    public int TareasHechas => Tareas.Count(t => t.Hecha);
    public List<AvisoModo> AvisosActivos { get; } = [];
}

public sealed class ResumenGuiaV2
{
    public List<ResultadoParada> Paradas { get; } = [];
    /// <summary>Primera parada ni completada ni aplazada (null = ruta terminada).</summary>
    public ResultadoParada? Siguiente { get; set; }
    public int ParadasCompletadas => Paradas.Count(p => p.Completada);
    public int TareasTotales => Paradas.Sum(p => p.Tareas.Count);
    public int TareasHechas => Paradas.Sum(p => p.TareasHechas);
    public IReadOnlyList<string> ModosActivos { get; set; } = [];
    public List<AvisoModo> AvisosGenerales { get; } = [];
}

/// <summary>Cerebro de la guia v2: evalua paradas/tareas/condiciones contra la partida real
/// (via el MISMO GuideEvaluationEngine de la v1 para cada hoja) y combina con el progreso manual.</summary>
public sealed class GuiaV2Evaluador
{
    private readonly GuiaV2Doc _doc;
    private readonly IResolutorRefsGuia _resolutor;
    private readonly Dictionary<Condicion, RequisitoGuia> _cacheHojas = new(ReferenceEqualityComparer.Instance);

    public GuiaV2Evaluador(GuiaV2Doc doc, IResolutorRefsGuia resolutor)
    {
        _doc = doc;
        _resolutor = resolutor;
    }

    public GuiaV2Doc Documento => _doc;

    public static string ClaveClase(ClaseGuia clase) => clase switch
    {
        ClaseGuia.CuerpoACuerpo => "cuerpo_a_cuerpo",
        ClaseGuia.Distancia => "distancia",
        ClaseGuia.Magia => "magia",
        ClaseGuia.Invocacion => "invocacion",
        ClaseGuia.Picaro => "picaro",
        _ => "",
    };

    public static ClaseGuia? ClaseDesdeClave(string? clave) => clave switch
    {
        "cuerpo_a_cuerpo" => ClaseGuia.CuerpoACuerpo,
        "distancia" => ClaseGuia.Distancia,
        "magia" => ClaseGuia.Magia,
        "invocacion" => ClaseGuia.Invocacion,
        "picaro" => ClaseGuia.Picaro,
        _ => null,
    };

    /// <summary>Convierte una hoja v2 en el RequisitoGuia de siempre (con ids ya resueltos).</summary>
    public RequisitoGuia HojaComoRequisito(Condicion c)
    {
        if (_cacheHojas.TryGetValue(c, out var hecho)) return hecho;

        var req = new RequisitoGuia
        {
            TipoBruto = c.Tipo,
            Tipo = GuideCatalog.TipoDesdeTexto(c.Tipo),
            Valor = c.Valor,
            Cantidad = c.Cantidad,
            Bandera = c.Bandera ?? "",
            Clave = c.Clave ?? "",
        };

        bool esNpc = req.Tipo is TipoRequisitoGuia.Npc or TipoRequisitoGuia.NpcActivo;
        if (!string.IsNullOrEmpty(c.Ref))
            req.Id = (esNpc ? _resolutor.Npc(c.Ref) : _resolutor.Objeto(c.Ref)) ?? 0;
        if (c.Refs is { Count: > 0 })
        {
            var ids = c.Refs.Select(r => esNpc ? _resolutor.Npc(r) : _resolutor.Objeto(r)).Where(i => i.HasValue).Select(i => i!.Value).ToArray();
            req.Ids = ids;
            // Objeto/Equipado/ObjetoPoseido con "refs" = "cualquiera de": la v1 usa ObjetoCualquiera.
            if (req.Tipo == TipoRequisitoGuia.Objeto) req.Tipo = TipoRequisitoGuia.ObjetoCualquiera;
        }

        _cacheHojas[c] = req;
        return req;
    }

    public ResultadoCondicion EvaluarCondicion(Condicion c, IGuideStateProvider ds)
    {
        var r = new ResultadoCondicion { Condicion = c };

        if (c.Tipo is "todas" or "alguna")
        {
            var hijos = c.Condiciones ?? [];
            foreach (var h in hijos) r.Hijos.Add(EvaluarCondicion(h, ds));
            if (r.Hijos.Count == 0) { r.Estado = EstadoCondicion.NoEvaluable; return r; }

            if (c.Tipo == "todas")
            {
                r.Fraccion = r.Hijos.Average(h => h.Fraccion);
                r.Estado = r.Hijos.All(h => h.Estado == EstadoCondicion.Cumplida) ? EstadoCondicion.Cumplida
                    : r.Hijos.Any(h => h.Estado == EstadoCondicion.NoCumplida) ? EstadoCondicion.NoCumplida
                    : r.Hijos.Any(h => h.Estado == EstadoCondicion.SinDatos) ? EstadoCondicion.SinDatos
                    : EstadoCondicion.NoEvaluable;
            }
            else
            {
                r.Fraccion = r.Hijos.Max(h => h.Fraccion);
                r.Estado = r.Hijos.Any(h => h.Estado == EstadoCondicion.Cumplida) ? EstadoCondicion.Cumplida
                    : r.Hijos.Any(h => h.Estado == EstadoCondicion.SinDatos) ? EstadoCondicion.SinDatos
                    : r.Hijos.Any(h => h.Estado == EstadoCondicion.NoCumplida) ? EstadoCondicion.NoCumplida
                    : EstadoCondicion.NoEvaluable;
            }
            return r;
        }

        var req = HojaComoRequisito(c);
        bool necesitaRef = req.Tipo is TipoRequisitoGuia.Objeto or TipoRequisitoGuia.ObjetoCualquiera or TipoRequisitoGuia.Npc
            or TipoRequisitoGuia.NpcActivo or TipoRequisitoGuia.ObjetoPoseido or TipoRequisitoGuia.Equipado;
        if (necesitaRef && req.Id <= 0 && (req.Ids == null || req.Ids.Length == 0))
        {
            // Referencia que esta app no sabe resolver (p.ej. NPC de Calamity en el escritorio,
            // o un objeto de una version de Calamity distinta): no evaluable, nunca inventado.
            r.Estado = EstadoCondicion.NoEvaluable;
            r.Hoja = new ResultadoRequisitoGuia
            {
                Requisito = req, NoEvaluable = true, EsLimiteEstructural = true, Pedido = 1,
                TextoClave = "Guia.Req.NoEvaluable", TextoArgs = [c.Ref ?? string.Join(" / ", c.Refs ?? [])],
                MotivoClave = "guide_motive_unresolved_ref",
            };
            return r;
        }

        var hoja = GuideEvaluationEngine.Evaluar(req, ds);
        r.Hoja = hoja;
        r.Estado = hoja.Cumplido ? EstadoCondicion.Cumplida
            : !hoja.NoEvaluable ? EstadoCondicion.NoCumplida
            : hoja.EsLimiteEstructural || req.Tipo == TipoRequisitoGuia.Desconocido ? EstadoCondicion.NoEvaluable
            : EstadoCondicion.SinDatos;
        r.Fraccion = hoja.Cumplido ? 1f
            : hoja.NoEvaluable || hoja.Pedido <= 0 ? 0f
            : Math.Clamp(hoja.Actual / (float)hoja.Pedido, 0f, 1f);
        return r;
    }

    public static bool AplicaAClase(List<string> clases, ClaseGuia? clase) =>
        clases.Count == 0 || clase == null || clases.Contains(ClaveClase(clase.Value));

    public ResultadoParada EvaluarParada(Parada p, int indice, IGuideStateProvider ds, GuiaV2ProgresoManual progreso,
        ClaseGuia? clase, IReadOnlyList<string>? modos)
    {
        var rp = new ResultadoParada
        {
            Parada = p,
            Indice = indice,
            CompletadaCuando = p.CompletadaCuando != null ? EvaluarCondicion(p.CompletadaCuando, ds) : null,
            MarcadaAMano = progreso.ParadaMarcada(p.Id),
            Aplazada = progreso.ParadaAplazada(p.Id),
        };
        foreach (var t in p.Tareas)
        {
            if (!AplicaAClase(t.Clases, clase)) continue;
            rp.Tareas.Add(new ResultadoTarea
            {
                Tarea = t,
                Condicion = t.Condicion != null ? EvaluarCondicion(t.Condicion, ds) : null,
                MarcadaAMano = progreso.TareaMarcada(t.Id),
            });
        }
        rp.AvisosActivos.AddRange(AvisosQueAplican(p.Avisos, modos));
        return rp;
    }

    public ResumenGuiaV2 Evaluar(IGuideStateProvider ds, GuiaV2ProgresoManual progreso, ClaseGuia? clase)
    {
        var modos = (ds as IGuideStateProviderV2)?.Modo?.Claves();
        var resumen = new ResumenGuiaV2 { ModosActivos = modos ?? [] };
        for (int i = 0; i < _doc.Paradas.Count; i++)
            resumen.Paradas.Add(EvaluarParada(_doc.Paradas[i], i, ds, progreso, clase, modos));
        resumen.Siguiente = resumen.Paradas.FirstOrDefault(p => !p.Completada && !p.Aplazada);
        resumen.AvisosGenerales.AddRange(AvisosQueAplican(_doc.AvisosModo, modos));
        return resumen;
    }

    /// <summary>Un aviso sin modos aplica siempre. Con modos: aplica si alguno esta activo; sin
    /// mundo cargado (modos null) se enseñan todos, para que el jugador sepa que existen.</summary>
    public static IEnumerable<AvisoModo> AvisosQueAplican(IEnumerable<AvisoModo> avisos, IReadOnlyList<string>? modos) =>
        avisos.Where(a => a.Modos.Count == 0 || modos == null || a.Modos.Any(modos.Contains));

    /// <summary>Etapa de la escalera de la clase que toca AHORA: la ultima cuya parada "Desde"
    /// no esta por delante de la siguiente parada pendiente.</summary>
    public EtapaEscalera? EtapaActual(ClaseGuia clase, ResumenGuiaV2 resumen)
    {
        var escalera = _doc.Escaleras.FirstOrDefault(e => e.Clase == ClaveClase(clase));
        if (escalera == null || escalera.Etapas.Count == 0) return null;
        int limite = resumen.Siguiente?.Indice ?? _doc.Paradas.Count - 1;
        EtapaEscalera? actual = null;
        foreach (var etapa in escalera.Etapas)
        {
            int idx = _doc.Paradas.FindIndex(p => p.Id == etapa.Desde);
            if (idx >= 0 && idx <= limite) actual = etapa;
        }
        return actual ?? escalera.Etapas[0];
    }

    /// <summary>Etapas de la escalera de una clase que aplican a una parada concreta (para su
    /// bloque "Preparate").</summary>
    public EtapaEscalera? EtapaParaParada(ClaseGuia clase, string paradaId)
    {
        var escalera = _doc.Escaleras.FirstOrDefault(e => e.Clase == ClaveClase(clase));
        if (escalera == null) return null;
        int objetivo = _doc.Paradas.FindIndex(p => p.Id == paradaId);
        EtapaEscalera? actual = null;
        foreach (var etapa in escalera.Etapas)
        {
            int idx = _doc.Paradas.FindIndex(p => p.Id == etapa.Desde);
            if (idx >= 0 && idx <= objetivo) actual = etapa;
        }
        return actual;
    }
}
