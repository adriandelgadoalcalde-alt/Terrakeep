using System.Text.Json.Serialization;

namespace Terrakeep.Core.Guia.V2;

// Guia v2 (F0, 02-oct-2026, encargo "guia grande vanilla + Calamity"): modelo de datos COMPARTIDO
// por Terrakeep (escritorio WPF) y TerrakeepMod (dentro de tModLoader). Vive en Terrakeep.Core y
// sus datos van INCRUSTADOS en Terrakeep.Core.dll (Guia/V2/Datos/*.json, EmbeddedResource), asi
// que las dos apps leen exactamente el mismo contenido sin scripts de sincronizacion.
//
// Reparto de responsabilidades (el mismo principio que la guia v1, GuideModel.cs):
//   - el .json dice QUE hay que hacer, en que orden, con que texto y con que condicion;
//   - el C# dice COMO se comprueba cada condicion contra la partida real
//     (GuideEvaluationEngine + IGuideStateProvider/IGuideStateProviderV2), y COMO se resuelve cada
//     referencia a objeto/NPC (tabla de referencias + resolutor de cada app).
//
// Diseño completo, vocabulario cerrado y como añadir contenido: docs/guia-v2-diseno.md.
// La guia v1 (guia_progresion.json, TramoGuia/PasoGuia) sigue existiendo y compilando sin
// cambios: v2 es un modelo NUEVO al lado, no una edicion destructiva del anterior.

/// <summary>Clases de personaje para las que la guia adapta equipo y textos.</summary>
public enum ClaseGuia
{
    CuerpoACuerpo = 0,
    Distancia = 1,
    Magia = 2,
    Invocacion = 3,
    /// <summary>Picaro (rogue): solo existe con Calamity.</summary>
    Picaro = 4,
}

/// <summary>Documento raiz de una guia v2 (uno por ambito: "vanilla" o "calamity").</summary>
public sealed class GuiaV2Doc
{
    [JsonPropertyName("esquema")] public int Esquema { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    /// <summary>"vanilla" o "calamity".</summary>
    [JsonPropertyName("ambito")] public string Ambito { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("subtitulo")] public string Subtitulo { get; set; } = "";
    [JsonPropertyName("referencia")] public VersionReferencia Referencia { get; set; } = new();
    [JsonPropertyName("creditos")] public string Creditos { get; set; } = "";
    /// <summary>Clases que admite esta guia (vanilla: 4; calamity: 5, con picaro).</summary>
    [JsonPropertyName("clases")] public List<string> Clases { get; set; } = [];
    /// <summary>Capitulos de la ruta (eras): agrupan paradas.</summary>
    [JsonPropertyName("capitulos")] public List<CapituloRuta> Capitulos { get; set; } = [];
    [JsonPropertyName("paradas")] public List<Parada> Paradas { get; set; } = [];
    /// <summary>Capitulos de manual (mapa y biomas, equipo, vida, estoy perdido...).</summary>
    [JsonPropertyName("articulos")] public List<Articulo> Articulos { get; set; } = [];
    [JsonPropertyName("escaleras")] public List<EscaleraClase> Escaleras { get; set; } = [];
    [JsonPropertyName("zonas")] public List<Zona> Zonas { get; set; } = [];
    /// <summary>Avisos generales segun el modo de juego (ademas de los de cada parada).</summary>
    [JsonPropertyName("avisosModo")] public List<AvisoModo> AvisosModo { get; set; } = [];
    /// <summary>"Estoy perdido / algo falla": sintoma -> respuesta.</summary>
    [JsonPropertyName("problemas")] public List<EntradaFicha> Problemas { get; set; } = [];
    /// <summary>"He encontrado algo raro": lo que ves -> que hacer.</summary>
    [JsonPropertyName("hallazgos")] public List<EntradaFicha> Hallazgos { get; set; } = [];
}

public sealed class VersionReferencia
{
    [JsonPropertyName("terraria")] public string Terraria { get; set; } = "";
    [JsonPropertyName("calamity")] public string? Calamity { get; set; }
    [JsonPropertyName("fechaInvestigacion")] public string FechaInvestigacion { get; set; } = "";
    [JsonPropertyName("nota")] public string Nota { get; set; } = "";
}

public sealed class CapituloRuta
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("resumen")] public string Resumen { get; set; } = "";
}

/// <summary>Una parada de la ruta: un jefe, un evento, una expedicion o una preparacion.</summary>
public sealed class Parada
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("capitulo")] public string Capitulo { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    /// <summary>jefe | evento | exploracion | preparacion | desafio.</summary>
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "jefe";
    /// <summary>Desvio opcional: se puede aplazar sin bloquear la ruta.</summary>
    [JsonPropertyName("opcional")] public bool Opcional { get; set; }
    /// <summary>Etiqueta corta visible ("Calamity", "Opcional recomendado"...).</summary>
    [JsonPropertyName("etiqueta")] public string Etiqueta { get; set; } = "";
    /// <summary>Jefes/NPC protagonistas (refs "Mod/NombreInterno"), para icono y lectura del jefe.</summary>
    [JsonPropertyName("jefes")] public List<string> Jefes { get; set; } = [];
    [JsonPropertyName("vidaObjetivo")] public VidaObjetivo? VidaObjetivo { get; set; }
    /// <summary>Texto humano de donde ocurre ("Desierto", "Bajo el desierto subterraneo").</summary>
    [JsonPropertyName("donde")] public string Donde { get; set; } = "";
    /// <summary>Ubicacion objetivo resoluble contra el mundo REAL (marcador del mapa del mod,
    /// mapa de Exploracion del escritorio). En orden de preferencia.</summary>
    [JsonPropertyName("ubicaciones")] public List<Ubicacion> Ubicaciones { get; set; } = [];
    /// <summary>"Como empezar este encuentro".</summary>
    [JsonPropertyName("invocacion")] public Invocacion? Invocacion { get; set; }
    /// <summary>"Preparate": texto valido para todas las clases.</summary>
    [JsonPropertyName("preparate")] public string Preparate { get; set; } = "";
    /// <summary>Matiz por clase (clave = nombre de ClaseGuia en snake_case: "cuerpo_a_cuerpo",
    /// "distancia", "magia", "invocacion", "picaro"). Opcional; la escalera de la clase se
    /// enseña aparte (EscaleraClase, por Desde).</summary>
    [JsonPropertyName("preparatePorClase")] public Dictionary<string, string> PreparatePorClase { get; set; } = [];
    /// <summary>Objetos que conviene tener para esta parada (clicables -> como conseguirlo /
    /// Libreria). Con condicion opcional para marcar si ya los tienes.</summary>
    [JsonPropertyName("necesitas")] public List<ObjetoNecesario> Necesitas { get; set; } = [];
    [JsonPropertyName("tareas")] public List<Tarea> Tareas { get; set; } = [];
    [JsonPropertyName("combate")] public string Combate { get; set; } = "";
    [JsonPropertyName("desbloquea")] public string Desbloquea { get; set; } = "";
    [JsonPropertyName("listoCuando")] public string ListoCuando { get; set; } = "";
    /// <summary>Si se cumple, la parada se da por completada sola (p.ej. bandera del jefe).</summary>
    [JsonPropertyName("completadaCuando")] public Condicion? CompletadaCuando { get; set; }
    /// <summary>"No vendas / conserva".</summary>
    [JsonPropertyName("conserva")] public string Conserva { get; set; } = "";
    [JsonPropertyName("conservaObjetos")] public List<string> ConservaObjetos { get; set; } = [];
    [JsonPropertyName("avisos")] public List<AvisoModo> Avisos { get; set; } = [];
    [JsonPropertyName("fuentes")] public List<Fuente> Fuentes { get; set; } = [];
}

public sealed class VidaObjetivo
{
    [JsonPropertyName("min")] public int Min { get; set; }
    [JsonPropertyName("max")] public int Max { get; set; }
    /// <summary>true si la cifra es la vida DESPUES de la mejora que da la parada.</summary>
    [JsonPropertyName("trasMejora")] public bool TrasMejora { get; set; }
    [JsonPropertyName("texto")] public string Texto { get; set; } = "";
}

public sealed class Invocacion
{
    /// <summary>Objeto invocador (ref), o null si el encuentro no tiene objeto.</summary>
    [JsonPropertyName("objeto")] public string? Objeto { get; set; }
    [JsonPropertyName("donde")] public string Donde { get; set; } = "";
    [JsonPropertyName("notas")] public string Notas { get; set; } = "";
}

public sealed class ObjetoNecesario
{
    [JsonPropertyName("ref")] public string Ref { get; set; } = "";
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; } = 1;
    [JsonPropertyName("motivo")] public string Motivo { get; set; } = "";
    /// <summary>Solo para estas clases (vacio = todas).</summary>
    [JsonPropertyName("clases")] public List<string> Clases { get; set; } = [];
}

/// <summary>Una tarea marcable. Si <see cref="Condicion"/> es null, <see cref="Manual"/> DEBE ser
/// true (el validador lo exige): la marca el jugador y se persiste por personaje.</summary>
public sealed class Tarea
{
    /// <summary>Id estable "parada.n" (n desde 1). Es la clave del progreso manual: no se renumera.</summary>
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("texto")] public string Texto { get; set; } = "";
    [JsonPropertyName("condicion")] public Condicion? Condicion { get; set; }
    [JsonPropertyName("manual")] public bool Manual { get; set; }
    [JsonPropertyName("opcional")] public bool Opcional { get; set; }
    /// <summary>Solo para estas clases (vacio = todas).</summary>
    [JsonPropertyName("clases")] public List<string> Clases { get; set; } = [];
}

/// <summary>Condicion evaluable. Hoja = el mismo vocabulario que <see cref="RequisitoGuia"/> (v1)
/// mas los tipos nuevos de v2; o compuesta ("todas"/"alguna" con <see cref="Condiciones"/>).</summary>
public sealed class Condicion
{
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";
    /// <summary>Referencia "Mod/NombreInterno" (objeto o NPC segun el tipo).</summary>
    [JsonPropertyName("ref")] public string? Ref { get; set; }
    [JsonPropertyName("refs")] public List<string>? Refs { get; set; }
    [JsonPropertyName("valor")] public int Valor { get; set; } = 1;
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; } = 1;
    [JsonPropertyName("bandera")] public string? Bandera { get; set; }
    /// <summary>Clave de mejora permanente / estado de mundo (tipos mejora_permanente/estado_mundo).</summary>
    [JsonPropertyName("clave")] public string? Clave { get; set; }
    [JsonPropertyName("condiciones")] public List<Condicion>? Condiciones { get; set; }
}

public sealed class Ubicacion
{
    /// <summary>zona | npc | jefe | punto (punto guardado del mundo: "mazmorra", "spawn", lab...).</summary>
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "zona";
    /// <summary>Id de zona (tipo zona/punto) o ref de NPC (tipo npc/jefe).</summary>
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    /// <summary>Solo aplica si el mundo es de este mal: "corrupcion" | "carmesi" (vacio = siempre).</summary>
    [JsonPropertyName("siMundo")] public string SiMundo { get; set; } = "";
    [JsonPropertyName("nota")] public string Nota { get; set; } = "";
}

/// <summary>Bioma, capa o estructura con firma de tiles para localizarla en el mundo real.</summary>
public sealed class Zona
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    /// <summary>De donde sale el nombre en español (localizacion oficial, CalamityModEsp...).</summary>
    [JsonPropertyName("fuenteNombre")] public string FuenteNombre { get; set; } = "";
    [JsonPropertyName("ambito")] public string Ambito { get; set; } = "vanilla";
    /// <summary>superficie | subterraneo | cavernas | infierno | espacio | cualquiera.</summary>
    [JsonPropertyName("capa")] public string Capa { get; set; } = "cualquiera";
    [JsonPropertyName("firma")] public FirmaZona Firma { get; set; } = new();
    /// <summary>Punto guardado por el juego, si existe (ver GuiaV2Ubicaciones.PuntosConocidos).</summary>
    [JsonPropertyName("punto")] public string? Punto { get; set; }
    [JsonPropertyName("resumen")] public string Resumen { get; set; } = "";
    [JsonPropertyName("bloques")] public List<Bloque> Bloques { get; set; } = [];
    [JsonPropertyName("fuentes")] public List<Fuente> Fuentes { get; set; } = [];
}

public sealed class FirmaZona
{
    /// <summary>Tiles vanilla (TileID numerico 1.4.4.9) que identifican la zona.</summary>
    [JsonPropertyName("tiles")] public List<int> Tiles { get; set; } = [];
    /// <summary>Tiles de mod ("CalamityMod/Navystone").</summary>
    [JsonPropertyName("tilesMod")] public List<string> TilesMod { get; set; } = [];
    /// <summary>Minimo de casillas con firma para considerar que la zona existe.</summary>
    [JsonPropertyName("minimo")] public int Minimo { get; set; } = 40;
}

public sealed class EscaleraClase
{
    [JsonPropertyName("clase")] public string Clase { get; set; } = "";
    [JsonPropertyName("etapas")] public List<EtapaEscalera> Etapas { get; set; } = [];
}

public sealed class EtapaEscalera
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    /// <summary>Id de la parada a partir de la cual aplica esta etapa.</summary>
    [JsonPropertyName("desde")] public string Desde { get; set; } = "";
    [JsonPropertyName("momento")] public string Momento { get; set; } = "";
    [JsonPropertyName("armas")] public List<OpcionEquipo> Armas { get; set; } = [];
    [JsonPropertyName("armadura")] public List<OpcionEquipo> Armadura { get; set; } = [];
    [JsonPropertyName("accesorios")] public List<OpcionEquipo> Accesorios { get; set; } = [];
    [JsonPropertyName("otros")] public List<OpcionEquipo> Otros { get; set; } = [];
    /// <summary>"Que haces despues".</summary>
    [JsonPropertyName("nota")] public string Nota { get; set; } = "";
    [JsonPropertyName("fuentes")] public List<Fuente> Fuentes { get; set; } = [];
}

public sealed class OpcionEquipo
{
    [JsonPropertyName("ref")] public string Ref { get; set; } = "";
    [JsonPropertyName("origen")] public string Origen { get; set; } = "";
}

/// <summary>Aviso que solo aplica en ciertos modos ("clasico","experto","maestro","viaje",
/// "revengeance","death"). Vacio = todos.</summary>
public sealed class AvisoModo
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("modos")] public List<string> Modos { get; set; } = [];
    [JsonPropertyName("texto")] public string Texto { get; set; } = "";
}

public sealed class Articulo
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("subtitulo")] public string Subtitulo { get; set; } = "";
    /// <summary>Glifo/icono sugerido (cada UI lo traduce a su estilo).</summary>
    [JsonPropertyName("icono")] public string Icono { get; set; } = "";
    [JsonPropertyName("bloques")] public List<Bloque> Bloques { get; set; } = [];
}

/// <summary>Bloque de contenido largo. Tipos: titulo, parrafo, lista, tabla, aviso, caja, cajas,
/// fuentes. Los textos admiten el marcado en linea de <see cref="GuiaV2Texto"/>.</summary>
public sealed class Bloque
{
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "parrafo";
    [JsonPropertyName("texto")] public string Texto { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    /// <summary>Estilo visual opcional: "verde", "suave", "peligro"...</summary>
    [JsonPropertyName("estilo")] public string Estilo { get; set; } = "";
    [JsonPropertyName("items")] public List<string> Items { get; set; } = [];
    [JsonPropertyName("numerada")] public bool Numerada { get; set; }
    [JsonPropertyName("cabeceras")] public List<string> Cabeceras { get; set; } = [];
    [JsonPropertyName("filas")] public List<List<string>> Filas { get; set; } = [];
    /// <summary>Para tipo "cajas": varias cajas en rejilla.</summary>
    [JsonPropertyName("bloques")] public List<Bloque> Bloques { get; set; } = [];
    [JsonPropertyName("fuentes")] public List<Fuente> Fuentes { get; set; } = [];
}

/// <summary>Ficha de "Estoy perdido" o "He encontrado algo raro".</summary>
public sealed class EntradaFicha
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("bloques")] public List<Bloque> Bloques { get; set; } = [];
    [JsonPropertyName("paradas")] public List<string> Paradas { get; set; } = [];
    [JsonPropertyName("zonas")] public List<string> Zonas { get; set; } = [];
    [JsonPropertyName("fuentes")] public List<Fuente> Fuentes { get; set; } = [];
}

/// <summary>Fuente citada: pagina de wiki oficial (calamity/terraria) o URL directa.</summary>
public sealed class Fuente
{
    /// <summary>"calamity" | "terraria" | "" (si es URL).</summary>
    [JsonPropertyName("wiki")] public string Wiki { get; set; } = "";
    [JsonPropertyName("pagina")] public string Pagina { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("texto")] public string Texto { get; set; } = "";

    /// <summary>URL navegable real (wiki.gg) - misma construccion que la guia HTML del usuario.</summary>
    public string UrlResuelta()
    {
        if (!string.IsNullOrEmpty(Url)) return Url;
        string baseUrl = Wiki == "terraria" ? "https://terraria.wiki.gg/wiki/" : "https://calamitymod.wiki.gg/wiki/";
        return baseUrl + Uri.EscapeDataString(Pagina.Replace(' ', '_')).Replace("%2F", "/", StringComparison.Ordinal);
    }

    public string TextoVisible() => string.IsNullOrEmpty(Texto) ? Pagina : Texto;
}
