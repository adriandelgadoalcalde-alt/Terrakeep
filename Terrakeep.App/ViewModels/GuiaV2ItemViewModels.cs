using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.Guia;
using Terrakeep.Core.Guia.V2;

namespace Terrakeep.App.ViewModels;

// Guia v2 (F2, 02-oct-2026): piezas de pantalla de la guia nueva. Son envoltorios finos sobre el
// resultado REAL del evaluador (GuiaV2Evaluador/ResumenGuiaV2) y el contenido incrustado; ninguna
// decide nada por su cuenta (el "tengo/no tengo" sale de IGuideStateProviderV2.CuantosPosee, la
// tarea hecha de ResultadoTarea.Hecha...). Los textos llevan el marcado de la guia y los pinta
// Controls/TextoGuia.

/// <summary>Un objeto citado por la guia (Preparate, conserva, escalera, ficha): sprite, nombre
/// oficial y lo que el personaje cargado tiene de verdad.</summary>
public sealed partial class ObjetoGuiaViewModel : ObservableObject
{
    private readonly GuiaV2ViewModel _guia;

    internal ObjetoGuiaViewModel(GuiaV2ViewModel guia, string referencia, int cantidad = 1, string motivo = "", string nota = "", string marcas = "")
    {
        _guia = guia;
        Ref = referencia;
        Cantidad = Math.Max(1, cantidad);
        Motivo = motivo;
        Nota = nota;
        Marcas = marcas;
        Tengo = guia.CuantosTengo(referencia);
    }

    public string Ref { get; }
    public int Cantidad { get; }
    public string Motivo { get; }
    public string Nota { get; }
    public string Marcas { get; }
    /// <summary>Significado de las marcas de la wiki (†, C, +, Δ...) para el tooltip: sueltas bajo el
    /// objeto eran crípticas (revisión del usuario, F4).</summary>
    public string MarcasAyuda => _guia.ExplicarMarcas(Marcas);
    public string Nombre => _guia.NombreObjeto(Ref);
    public string? Icono => _guia.IconoObjeto(Ref);
    public string RefCorta => Ref.StartsWith("Terraria/", StringComparison.Ordinal) && GuiaV2Recursos.Referencias.Objetos.TryGetValue(Ref, out var o) && o.Id is int id
        ? "ID " + id : Ref;

    /// <summary>Unidades reales en todo el personaje (null = sin personaje cargado o referencia
    /// que esta build no conoce: "no se sabe", nunca "no lo tienes").</summary>
    public int? Tengo { get; }
    public bool TieneDatos => Tengo.HasValue;
    public bool LoTiene => Tengo >= Cantidad;
    public bool NoLoTiene => Tengo.HasValue && Tengo < Cantidad;
    public string TextoTenencia => !Tengo.HasValue ? _guia.L("guia2_tenencia_sin_datos")
        : LoTiene ? (Cantidad > 1 ? _guia.F("guia2_tenencia_tienes_de", Tengo, Cantidad) : _guia.L("guia2_tenencia_lo_tienes"))
        : Tengo > 0 ? _guia.F("guia2_tenencia_tienes_de", Tengo, Cantidad) : _guia.L("guia2_tenencia_no_lo_tienes");
    public string TextoCantidad => Cantidad > 1 ? "×" + Cantidad : "";

    [RelayCommand] private void Abrir() => _guia.AbrirObjeto(Ref);
}

public sealed partial class TareaV2ViewModel : ObservableObject
{
    private readonly GuiaV2ViewModel _guia;
    private readonly ResultadoTarea _r;

    internal TareaV2ViewModel(GuiaV2ViewModel guia, ResultadoTarea r, int numero)
    {
        _guia = guia;
        _r = r;
        Numero = numero;
    }

    public string Id => _r.Tarea.Id;
    public int Numero { get; }
    public string Texto => _r.Tarea.Texto;
    public bool Opcional => _r.Tarea.Opcional;
    public bool Hecha => _r.Hecha;
    /// <summary>La comprueba la guia sola (condicion evaluable por el escritorio).</summary>
    public bool Automatica => _r.Automatica;
    public bool CumplidaSola => _r.Condicion?.Estado == EstadoCondicion.Cumplida;
    public bool MarcadaAMano => _r.MarcadaAMano;
    /// <summary>Se puede marcar/desmarcar a mano salvo si ya la da por cumplida la partida real.</summary>
    public bool PuedeMarcar => !CumplidaSola;

    public string Glifo => CumplidaSola ? "✓" : MarcadaAMano ? "✓" : "";

    /// <summary>Linea de estado bajo la tarea: lo que la guia ha comprobado de verdad, o por que
    /// es una casilla manual.</summary>
    public string Estado
    {
        get
        {
            var c = _r.Condicion;
            if (c == null) return _guia.L(MarcadaAMano ? "guia2_tarea_manual_marcada" : "guia2_tarea_manual");
            if (c.Estado == EstadoCondicion.Cumplida) return _guia.L("guia2_tarea_comprobada") + LineaHoja(c);
            if (c.Estado == EstadoCondicion.NoCumplida) return _guia.L("guia2_tarea_pendiente_auto") + LineaHoja(c);
            if (c.Estado == EstadoCondicion.SinDatos) return _guia.L("guia2_tarea_sin_datos");
            return _guia.L("guia2_tarea_no_evaluable");
        }
    }

    private string LineaHoja(ResultadoCondicion c)
    {
        var hoja = c.Hoja ?? c.Hijos.FirstOrDefault(h => h.Estado == EstadoCondicion.NoCumplida)?.Hoja ?? c.Hijos.FirstOrDefault()?.Hoja;
        string linea = hoja == null ? "" : _guia.TextoHoja(hoja);
        return string.IsNullOrEmpty(linea) ? "" : " · " + linea;
    }

    public double Fraccion => _r.Condicion?.Fraccion ?? (Hecha ? 1 : 0);
    public bool TieneProgresoParcial => !Hecha && Fraccion > 0.001 && Fraccion < 0.999;

    [RelayCommand]
    private void Alternar()
    {
        if (!PuedeMarcar) return;
        _guia.MarcarTarea(Id, !MarcadaAMano);
    }
}

public sealed class JefeGuiaViewModel(string nombre, string? icono)
{
    public string Nombre { get; } = nombre;
    public string? Icono { get; } = icono;
}

public sealed class AvisoGuiaViewModel(string texto)
{
    public string Texto { get; } = texto;
}

public sealed partial class ParadaV2ViewModel : ObservableObject
{
    private readonly GuiaV2ViewModel _guia;

    internal ParadaV2ViewModel(GuiaV2ViewModel guia, ResultadoParada r, int total, string capituloTitulo, bool esSiguiente)
    {
        _guia = guia;
        Resultado = r;
        Total = total;
        CapituloTitulo = capituloTitulo;
        EsSiguiente = esSiguiente;
    }

    internal ResultadoParada Resultado { get; }
    public Parada Parada => Resultado.Parada;
    public string Id => Parada.Id;
    public int Numero => Resultado.Indice + 1;
    public int Total { get; }
    public string NumeroTexto => Numero.ToString("00");
    public string CapituloTitulo { get; }
    public string Titulo => Parada.Titulo;
    public string TituloPlano => _guia.Plano(Parada.Titulo);
    public string Etiqueta => Parada.Etiqueta;
    public bool TieneEtiqueta => !string.IsNullOrEmpty(Parada.Etiqueta);
    public bool Opcional => Parada.Opcional;
    public bool EsSiguiente { get; }
    public bool Completada => Resultado.Completada;
    public bool CompletadaSola => Resultado.CompletadaCuando?.Estado == EstadoCondicion.Cumplida;
    public bool MarcadaAMano => Resultado.MarcadaAMano;
    public bool Aplazada => Resultado.Aplazada;
    public bool Pendiente => !Completada && !Aplazada;
    public string Glifo => Completada ? "✓" : Aplazada ? "↷" : NumeroTexto;

    public string EstadoTexto => Completada
        ? _guia.L(CompletadaSola ? "guia2_parada_hecha_auto" : "guia2_parada_hecha_manual")
        : Aplazada ? _guia.L("guia2_parada_aplazada")
        : EsSiguiente ? _guia.L("guia2_parada_siguiente")
        : _guia.L("guia2_parada_pendiente");

    public string Cabecera => _guia.F("guia2_parada_cabecera", Numero, Total, CapituloTitulo);
    public string? Icono => Parada.Jefes.Select(GuiaV2Recursos.IconoNpc).FirstOrDefault(i => i != null)
        ?? (Parada.Invocacion?.Objeto is string o ? _guia.IconoObjeto(o) : null);

    public IReadOnlyList<JefeGuiaViewModel> Jefes => Parada.Jefes.Select(j => new JefeGuiaViewModel(_guia.NombreNpc(j), GuiaV2Recursos.IconoNpc(j))).ToList();
    public bool TieneJefes => Parada.Jefes.Count > 0;

    public bool TieneVida => Parada.VidaObjetivo != null;
    public string VidaEtiqueta => _guia.L(Parada.VidaObjetivo?.TrasMejora == true ? "guia2_vida_tras_mejora" : "guia2_vida_objetivo");
    public string VidaTexto => Parada.VidaObjetivo?.Texto ?? "";
    public string VidaActualTexto => _guia.VidaActual is int v && Parada.VidaObjetivo is { } vo
        ? _guia.F(v >= vo.Min ? "guia2_vida_actual_ok" : "guia2_vida_actual_falta", v) : "";
    public bool VidaSuficiente => _guia.VidaActual is int v && Parada.VidaObjetivo is { } vo && v >= vo.Min;
    public bool TieneDonde => !string.IsNullOrEmpty(Parada.Donde);

    public bool TieneInvocacion => Parada.Invocacion != null;
    public ObjetoGuiaViewModel? ObjetoInvocacion => Parada.Invocacion?.Objeto is string o ? new ObjetoGuiaViewModel(_guia, o) : null;
    public string InvocacionDonde => Parada.Invocacion?.Donde ?? "";
    public string InvocacionNotas => Parada.Invocacion?.Notas ?? "";
    public bool TieneInvocacionNotas => !string.IsNullOrEmpty(InvocacionNotas);

    public string Preparate => Parada.Preparate;
    public bool TienePreparate => !string.IsNullOrEmpty(Parada.Preparate);
    public string PreparateClase => _guia.ClaseActual is ClaseGuia c && Parada.PreparatePorClase.TryGetValue(GuiaV2Evaluador.ClaveClase(c), out var t) && t != Parada.Preparate ? t : "";
    public bool TienePreparateClase => !string.IsNullOrEmpty(PreparateClase);

    public IReadOnlyList<ObjetoGuiaViewModel> Necesitas => Parada.Necesitas
        .Where(n => GuiaV2Evaluador.AplicaAClase(n.Clases, _guia.ClaseActual))
        .Select(n => new ObjetoGuiaViewModel(_guia, n.Ref, n.Cantidad, n.Motivo)).ToList();
    public bool TieneNecesitas => Necesitas.Count > 0;

    /// <summary>Etapa de la escalera de la clase que aplica a esta parada (debajo de Preparate).</summary>
    public EtapaEscaleraViewModel? Etapa => _guia.EtapaParaParada(Id);
    public bool TieneEtapa => Etapa != null;

    public IReadOnlyList<TareaV2ViewModel> Tareas => Resultado.Tareas.Select((t, i) => new TareaV2ViewModel(_guia, t, i + 1)).ToList();
    public int TareasHechas => Resultado.TareasHechas;
    public int TareasTotal => Resultado.Tareas.Count;
    public string ProgresoTareas => _guia.F("guia2_tareas_progreso", TareasHechas, TareasTotal);
    public double FraccionTareas => TareasTotal == 0 ? (Completada ? 1 : 0) : TareasHechas / (double)TareasTotal;

    public string Combate => Parada.Combate;
    public bool TieneCombate => !string.IsNullOrEmpty(Combate);
    public string CombateTitulo => _guia.L(Parada.Tipo is "jefe" or "evento" or "desafio" ? "guia2_sec_combate" : "guia2_sec_exploracion");
    public string Desbloquea => Parada.Desbloquea;
    public bool TieneDesbloquea => !string.IsNullOrEmpty(Desbloquea);
    public string ListoCuando => Parada.ListoCuando;
    public bool TieneListoCuando => !string.IsNullOrEmpty(ListoCuando);
    public string Conserva => Parada.Conserva;
    public bool TieneConserva => !string.IsNullOrEmpty(Conserva) || Parada.ConservaObjetos.Count > 0;
    public IReadOnlyList<ObjetoGuiaViewModel> ConservaObjetos => Parada.ConservaObjetos.Select(r => new ObjetoGuiaViewModel(_guia, r)).ToList();

    public IReadOnlyList<AvisoGuiaViewModel> Avisos => Resultado.AvisosActivos.Select(a => new AvisoGuiaViewModel(a.Texto)).ToList();
    public bool TieneAvisos => Resultado.AvisosActivos.Count > 0;
    public IReadOnlyList<Fuente> Fuentes => Parada.Fuentes;
    public bool TieneFuentes => Parada.Fuentes.Count > 0;

    public bool PuedeAplazar => Opcional && !Completada;
    public string TextoBotonHecha => _guia.L(MarcadaAMano ? "guia2_btn_desmarcar_hecha" : "guia2_btn_marcar_hecha");
    public bool PuedeMarcarHecha => !CompletadaSola;
    public string TextoBotonAplazar => _guia.L(Aplazada ? "guia2_btn_reanudar" : "guia2_btn_aplazar");

    /// <summary>Texto de la ubicacion en el mundo real cargado (se rellena al resolverla).</summary>
    [ObservableProperty] private string _ubicacionTexto = "";
    [ObservableProperty] private bool _tieneUbicacion;
    [ObservableProperty] private bool _esSeleccionada;

    [RelayCommand] private void Abrir() => _guia.SeleccionarParada(this);
}

public sealed partial class CapituloV2ViewModel(string id, string titulo, string resumen, IReadOnlyList<ParadaV2ViewModel> paradas, int numero) : ObservableObject
{
    [ObservableProperty] private bool _esSeleccionado;
    public string Id { get; } = id;
    public string Titulo { get; } = titulo;
    public string Resumen { get; } = resumen;
    public int Numero { get; } = numero;
    public string NumeroTexto => Numero.ToString("00");
    public IReadOnlyList<ParadaV2ViewModel> Paradas { get; } = paradas;
    public int Hechas => Paradas.Count(p => p.Completada);
    public string Progreso => $"{Hechas}/{Paradas.Count}";
}

/// <summary>Una pieza de la escalera de equipo, con su papel/nota de la fuente.</summary>
public sealed class GrupoEquipoViewModel(string titulo, IReadOnlyList<ObjetoGuiaViewModel> objetos)
{
    public string Titulo { get; } = titulo;
    public IReadOnlyList<ObjetoGuiaViewModel> Objetos { get; } = objetos;
}

public sealed class EtapaEscaleraViewModel
{
    internal EtapaEscaleraViewModel(GuiaV2ViewModel guia, EtapaEscalera e, bool esActual, string desdeTitulo)
    {
        Etapa = e;
        EsActual = esActual;
        DesdeTitulo = desdeTitulo;
        ObjetoGuiaViewModel Vm(OpcionEquipo o) => new(guia, o.Ref, 1, o.Origen, o.Nota, string.Join(" ", o.Marcas));
        var grupos = new List<GrupoEquipoViewModel>();
        if (e.Armas.Count > 0) grupos.Add(new GrupoEquipoViewModel(guia.L("guia2_eq_armas"), e.Armas.Select(Vm).ToList()));
        // Armadura agrupada por conjunto (docs/guia-v2-diseno.md §6): un grupo por conjunto real. El
        // conjunto ya es el rotulo completo con su nombre oficial («Armadura de plata», F2b): repetir
        // delante «Armadura ·» lo duplicaba.
        foreach (var conjunto in e.Armadura.GroupBy(a => a.Conjunto))
            grupos.Add(new GrupoEquipoViewModel(string.IsNullOrEmpty(conjunto.Key) ? guia.L("guia2_eq_armadura") : conjunto.Key,
                conjunto.Select(Vm).ToList()));
        if (e.Accesorios.Count > 0) grupos.Add(new GrupoEquipoViewModel(guia.L("guia2_eq_accesorios"), e.Accesorios.Select(Vm).ToList()));
        if (e.Otros.Count > 0) grupos.Add(new GrupoEquipoViewModel(guia.L("guia2_eq_otros"), e.Otros.Select(Vm).ToList()));
        Grupos = grupos;
        Total = e.Armas.Count + e.Armadura.Count + e.Accesorios.Count + e.Otros.Count;
        Tengo = grupos.SelectMany(g => g.Objetos).Count(o => o.LoTiene);
    }

    public EtapaEscalera Etapa { get; }
    public string Momento => Etapa.Momento;
    public string Nota => Etapa.Nota;
    public bool TieneNota => !string.IsNullOrEmpty(Etapa.Nota);
    public bool EsActual { get; }
    public string DesdeTitulo { get; }
    public IReadOnlyList<GrupoEquipoViewModel> Grupos { get; }
    public IReadOnlyList<Fuente> Fuentes => Etapa.Fuentes;
    public int Total { get; }
    public int Tengo { get; }
}

public sealed partial class OpcionClaseViewModel : ObservableObject
{
    private readonly GuiaV2ViewModel _guia;

    internal OpcionClaseViewModel(GuiaV2ViewModel guia, ClaseGuia clase, bool seleccionada, bool detectada)
    {
        _guia = guia;
        Clase = clase;
        Seleccionada = seleccionada;
        Detectada = detectada;
    }

    public ClaseGuia Clase { get; }
    public string Nombre => _guia.L("guia2_clase_" + GuiaV2Evaluador.ClaveClase(Clase));
    public string Glifo => Clase switch
    {
        ClaseGuia.CuerpoACuerpo => "⚔",
        ClaseGuia.Distancia => "➶",
        ClaseGuia.Magia => "✦",
        ClaseGuia.Invocacion => "♞",
        _ => "✧",
    };
    public bool Seleccionada { get; }
    public bool Detectada { get; }

    [RelayCommand] private void Elegir() => _guia.ElegirClase(Clase);
}

public sealed partial class ArticuloV2ViewModel(GuiaV2ViewModel guia, Articulo a) : ObservableObject
{
    public Articulo Articulo { get; } = a;
    public string Id => Articulo.Id;
    public string Titulo => guia.Plano(Articulo.Titulo);
    public string Subtitulo => Articulo.Subtitulo;
    public bool TieneSubtitulo => !string.IsNullOrEmpty(Articulo.Subtitulo);
    public IReadOnlyList<Bloque> Bloques => Articulo.Bloques;
    public string Glifo => Articulo.Icono switch
    {
        "inicio" => "◈",
        "mapa" => "⌖",
        "equipo" => "⚔",
        "vida" => "♥",
        "laboratorio" => "⚙",
        "materiales" => "▦",
        "hallazgo" => "◇",
        "secreto" => "✦",
        "perdido" => "?",
        "fuentes" => "↗",
        _ => "•",
    };
    [ObservableProperty] private bool _seleccionado;
    [RelayCommand] private void Abrir() => guia.AbrirArticulo(Id);
}

public sealed partial class ZonaV2ViewModel(GuiaV2ViewModel guia, Zona z) : ObservableObject
{
    public Zona Zona { get; } = z;
    public string Id => Zona.Id;
    public string Nombre => Zona.Nombre;
    public string Resumen => Zona.Resumen;
    public bool TieneResumen => !string.IsNullOrEmpty(Zona.Resumen);
    public IReadOnlyList<Bloque> Bloques => Zona.Bloques;
    public bool EsCalamity => Zona.Ambito == "calamity";
    public string Capa => guia.L("guia2_capa_" + Zona.Capa);
    /// <summary>La capa solo se enseña si añade algo al nombre ("Superficie / Superficie" no).</summary>
    public bool MostrarCapa => !string.Equals(Capa, Zona.Nombre, StringComparison.OrdinalIgnoreCase);
    [ObservableProperty] private string _ubicacionTexto = "";
    [ObservableProperty] private bool _tieneUbicacion;
    [ObservableProperty] private bool _resaltada;
    public UbicacionResuelta? Ubicacion { get; internal set; }
    [RelayCommand] private void VerEnMapa() => guia.VerZonaEnMapa(this);
}

public sealed partial class FichaV2ViewModel(GuiaV2ViewModel guia, EntradaFicha f) : ObservableObject
{
    public EntradaFicha Ficha { get; } = f;
    public string Id => Ficha.Id;
    // El titulo de algunas fichas lleva marcado ({z:mazmorra|mazmorra}) y se pinta en un TextBlock
    // plano: sin Plano salia el token crudo (revision visual F4, "Algo raro" de Calamity).
    public string Titulo => guia.Plano(Ficha.Titulo);
    public IReadOnlyList<Bloque> Bloques => Ficha.Bloques;
    public IReadOnlyList<Fuente> Fuentes => Ficha.Fuentes;
    public bool TieneFuentes => Ficha.Fuentes.Count > 0;
    public string EnlacesParadas => guia.L("guia2_ficha_paradas") + " " + string.Join(" · ", Ficha.Paradas.Select(p => "{p:" + p + "}"));
    public bool TieneParadas => Ficha.Paradas.Count > 0;
    [ObservableProperty] private bool _abierta;
    [ObservableProperty] private bool _resaltada;
    [RelayCommand] private void Alternar() => Abierta = !Abierta;
}

/// <summary>Una forma real de conseguir el objeto (receta/botin/bolsa/tienda), ya en marcado
/// para que ingredientes, jefes y vendedores sean clicables y lleven su sprite.</summary>
public sealed class ObtencionV2ViewModel(string tipo, string marcado, string detalle, string fuenteCodigo)
{
    public string Tipo { get; } = tipo;
    public string Marcado { get; } = marcado;
    public string Detalle { get; } = detalle;
    public bool TieneDetalle => !string.IsNullOrEmpty(Detalle);
    public string FuenteCodigo { get; } = fuenteCodigo;
}

public sealed partial class FichaObjetoViewModel : ObservableObject
{
    private readonly GuiaV2ViewModel _guia;

    internal FichaObjetoViewModel(GuiaV2ViewModel guia, string referencia, IReadOnlyList<ObtencionV2ViewModel> obtenciones, bool puedeVolver)
    {
        _guia = guia;
        Objeto = new ObjetoGuiaViewModel(guia, referencia);
        Obtenciones = obtenciones;
        PuedeVolver = puedeVolver;
    }

    public ObjetoGuiaViewModel Objeto { get; }
    public string Nombre => Objeto.Nombre;
    public string? Icono => Objeto.Icono;
    public string NombreOtroIdioma => _guia.NombreOtroIdioma(Objeto.Ref);
    public bool TieneNombreOtroIdioma => !string.IsNullOrEmpty(NombreOtroIdioma) && NombreOtroIdioma != Nombre;
    public string Identificador => Objeto.RefCorta;
    public string FuenteNombre => GuiaV2Recursos.Referencias.Objetos.TryGetValue(Objeto.Ref, out var o) ? o.FuenteEs : "";
    public IReadOnlyList<ObtencionV2ViewModel> Obtenciones { get; }
    public bool TieneObtenciones => Obtenciones.Count > 0;
    public bool SeConsigueEnElMundo => Obtenciones.Count == 0;
    public bool PuedeVolver { get; }
    public bool PuedeLlevarALibreria => _guia.PuedeLlevarALibreria(Objeto.Ref);
    public string TextoLibreria => _guia.TextoLibreria(Objeto.Ref);

    [RelayCommand] private void Cerrar() => _guia.CerrarFicha();
    [RelayCommand] private void Volver() => _guia.VolverFicha();
    [RelayCommand] private void LlevarALibreria() => _guia.LlevarALibreria(Objeto.Ref);
}

public sealed partial class ResultadoBusquedaV2ViewModel(string tipo, string titulo, string extracto, Action abrir, string? icono = null) : ObservableObject
{
    public string Tipo { get; } = tipo;
    public string Titulo { get; } = titulo;
    public string Extracto { get; } = extracto;
    public bool TieneExtracto => !string.IsNullOrEmpty(Extracto);
    public string? Icono { get; } = icono;
    [RelayCommand] private void Abrir() => abrir();
}
