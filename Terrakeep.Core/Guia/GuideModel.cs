using System.Text.Json.Serialization;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): modelo de datos de la Guia de progresion para Terrakeep de escritorio.
// Espejo DELIBERADO del modelo real de TerrakeepMod (Common/Guia/ModeloGuia.cs) - mismos nombres
// de concepto (Tramo/Paso/Requisito), mismo reparto de responsabilidades (el .json dice QUE hace
// falta, el C# dice COMO se comprueba contra la partida real) y el MISMO archivo de datos
// (guia_progresion.json, sincronizado tal cual por scripts/sync-guia-desde-terrakeepmod.ps1 - ver
// ese script para el porque de copiar en vez de referenciar en vivo).
//
// Diferencia real con el mod: aqui NO hay propiedades de texto resueltas en el propio modelo
// (Titulo/Porque/Como como en TerrakeepMod) porque Terrakeep.Core no puede depender de un
// servicio de idioma de la capa App (mismo motivo por el que LocalizedContent.CurrentLanguage es
// static y vive aqui) - el texto se resuelve aparte, en GuideTextCatalog, que el llamador combina
// con la Clave de cada Tramo/Paso.

public enum AmbitoGuia
{
    Vanilla = 0,
    Calamity = 1,
    Ambos = 2,
}

public enum CapaMundoGuia
{
    Cualquiera = 0,
    Superficie = 1,
    Subterraneo = 2,
    Cavernas = 3,
    Infierno = 4,
}

// Vocabulario CERRADO de requisitos, igual que en el mod - un tipo que el .json traiga y que no
// este aqui se marca Desconocido y el evaluador lo deja NoEvaluable (nunca "cumplido" por
// defecto). Ver GuideEvaluator para el porque de que varios de estos tipos, evaluables en vivo
// dentro del juego, no lo sean todavia (o nunca) desde un editor de ficheros .plr/.wld estaticos.
public enum TipoRequisitoGuia
{
    Desconocido = 0,
    CristalesVida,
    VidaMaxima,
    Defensa,
    NpcsPueblo,
    Npc,
    NpcActivo,
    Objeto,
    ObjetoCualquiera,
    DanoArma,
    Gancho,
    Bandera,
}

public sealed class RequisitoGuia
{
    [JsonPropertyName("tipo")] public string TipoBruto { get; set; } = "";
    [JsonPropertyName("valor")] public int Valor { get; set; } = 1;
    [JsonPropertyName("id")] public int Id { get; set; }
    // El .json tambien puede traer "idMod" (pid "Mod/NombreInterno") en vez de un id numerico
    // fijo, para los objetos de Calamity - se resuelve una vez al cargar el catalogo (ver
    // GuideCatalog.ResolverReferenciasDeMod) contra el mismo CalamityCatalog que ya usa el resto
    // de Terrakeep, y el resultado se escribe aqui mismo en Id/Ids para que el evaluador no tenga
    // que saber nada de pids.
    [JsonPropertyName("idMod")] public string? IdMod { get; set; }
    [JsonPropertyName("ids")] public int[]? Ids { get; set; }
    [JsonPropertyName("idsMod")] public string[]? IdsMod { get; set; }
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; } = 1;
    [JsonPropertyName("bandera")] public string Bandera { get; set; } = "";
    [JsonPropertyName("recomendado")] public bool Recomendado { get; set; }

    [JsonIgnore] public TipoRequisitoGuia Tipo { get; set; } = TipoRequisitoGuia.Desconocido;
}

public sealed class PasoGuia
{
    [JsonPropertyName("clave")] public string Clave { get; set; } = "";
    [JsonPropertyName("zona")] public string Zona { get; set; } = "";
    [JsonPropertyName("capa")] public string CapaBruta { get; set; } = "";
    [JsonPropertyName("jefe")] public int Jefe { get; set; }
    [JsonPropertyName("requisitos")] public List<RequisitoGuia> Requisitos { get; set; } = [];

    [JsonIgnore] public string Tramo { get; set; } = "";
    [JsonIgnore] public CapaMundoGuia Capa { get; set; } = CapaMundoGuia.Cualquiera;
}

public sealed class TramoGuia
{
    [JsonPropertyName("clave")] public string Clave { get; set; } = "";
    [JsonPropertyName("orden")] public int Orden { get; set; }
    [JsonPropertyName("ambito")] public string AmbitoBruto { get; set; } = "vanilla";
    [JsonPropertyName("jefeFinal")] public int JefeFinal { get; set; }
    [JsonPropertyName("jefeFinalMod")] public string? JefeFinalMod { get; set; }
    [JsonPropertyName("implementado")] public bool Implementado { get; set; }
    [JsonPropertyName("opcional")] public bool Opcional { get; set; }
    [JsonPropertyName("pasos")] public List<PasoGuia> Pasos { get; set; } = [];

    [JsonIgnore] public AmbitoGuia Ambito { get; set; } = AmbitoGuia.Vanilla;
}

public sealed class GuiaProgresionDoc
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("juego")] public string Juego { get; set; } = "";
    [JsonPropertyName("tramos")] public List<TramoGuia> Tramos { get; set; } = [];
}

// Resultado de comprobar un requisito contra el personaje/mundo REALES cargados en Terrakeep -
// mismo contrato honesto que ResultadoRequisito del mod: NoEvaluable nunca cuenta como cumplido.
public sealed class ResultadoRequisitoGuia
{
    public required RequisitoGuia Requisito { get; init; }
    public bool Cumplido { get; set; }
    public int Actual { get; set; }
    public int Pedido { get; set; }
    // Clave de texto ("Guia.Req.XXX") + argumentos ya resueltos (nombres de objeto/NPC, numeros)
    // - la capa de presentacion decide el idioma con GuideTextCatalog.Format, este resultado no
    // conoce ningun idioma.
    public string TextoClave { get; set; } = "";
    public object?[] TextoArgs { get; set; } = [];
    public bool NoEvaluable { get; set; }
    // Clave de localizacion (NUNCA texto literal - bug real encontrado por A10-IDIOMA-BARRIDO el
    // 15-sep-2026: la primera version de esto guardaba la frase en español directamente desde
    // Core, así que se veía en español incluso con la app en inglés) del motivo REAL por el que
    // Terrakeep (a diferencia de TerrakeepMod en vivo) no puede comprobar esto desde un .plr/.wld
    // estatico - se enseña en vez de un generico "no evaluable" mudo, mismo criterio de
    // honestidad que el resto del proyecto. Resuelta en la capa de presentacion (App) contra
    // strings_es.json/strings_en.json via LocalizationService - Core no conoce ningun idioma,
    // igual que TextoClave/GuideTextCatalog de arriba.
    public string? MotivoClave { get; set; }
}
