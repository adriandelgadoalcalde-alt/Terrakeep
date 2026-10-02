using System.Text.Json.Serialization;

namespace Terrakeep.Core.Guia.V2;

// Tabla de referencias de la guia v2 (Datos/guia_v2_referencias.json), GENERADA por
// scripts/guia-v2/generar-referencias.js a partir de fuentes reales:
//   - ids vanilla: ItemID.cs / NPCID.cs del tModLoader 1.4.4.9 decompilado (la version que corre
//     TerrakeepMod) - nunca de los JSON de Terraria 1.4.5 del escritorio, que numeran distinto;
//   - nombres vanilla en español: localizacion oficial es-ES de Terraria (lang.zip ->
//     Terraria.Localization.Content.es-ES.Items.json / NPCs.json);
//   - Calamity: nombre interno = clase real en el decompilado 2.2.4; nombre en ingles de su
//     Localization/en-US; nombre en español de la localizacion es-ES que existe para Calamity
//     (CalamityModEsp, la que tiene instalada el usuario - Calamity 2.2.4 NO trae es-ES propia,
//     comprobado listando su .tmod). Sin traduccion -> se queda el nombre en ingles y
//     FuenteEs = "sin traduccion", nunca inventado;
//   - obtencion: recetas/botin/tiendas extraidas del codigo decompilado
//     (scripts/guia-v2/extraer-obtencion.js), con archivo:linea de cada dato.
public sealed class GuiaV2Referencias
{
    [JsonPropertyName("esquema")] public int Esquema { get; set; }
    [JsonPropertyName("fuentes")] public Dictionary<string, string> Fuentes { get; set; } = [];
    /// <summary>Clave = ref "Terraria/NombreInterno" o "CalamityMod/NombreInterno".</summary>
    [JsonPropertyName("objetos")] public Dictionary<string, RefObjeto> Objetos { get; set; } = [];
    [JsonPropertyName("npcs")] public Dictionary<string, RefNpc> Npcs { get; set; } = [];
    /// <summary>Estaciones de fabricacion: "Terraria/Tile/Anvils" -> nombre.</summary>
    [JsonPropertyName("estaciones")] public Dictionary<string, RefNombre> Estaciones { get; set; } = [];
    /// <summary>Grupos de receta ("Sand", "IronBar"...) -> nombre.</summary>
    [JsonPropertyName("grupos")] public Dictionary<string, RefNombre> Grupos { get; set; } = [];
}

public class RefNombre
{
    [JsonPropertyName("es")] public string Es { get; set; } = "";
    [JsonPropertyName("en")] public string En { get; set; } = "";
    /// <summary>"Terraria es-ES oficial" | "CalamityModEsp 2.2.0.1" | "sin traduccion" ...</summary>
    [JsonPropertyName("fuenteEs")] public string FuenteEs { get; set; } = "";
}

public sealed class RefObjeto : RefNombre
{
    /// <summary>ItemID numerico real (solo vanilla; Calamity se resuelve en ejecucion por nombre).</summary>
    [JsonPropertyName("id")] public int? Id { get; set; }
    [JsonPropertyName("categoria")] public string Categoria { get; set; } = "";
    [JsonPropertyName("obtencion")] public List<Obtencion> Obtencion { get; set; } = [];
}

public sealed class RefNpc : RefNombre
{
    [JsonPropertyName("id")] public int? Id { get; set; }
    [JsonPropertyName("jefe")] public bool Jefe { get; set; }
}

/// <summary>Una forma real de conseguir un objeto.</summary>
public sealed class Obtencion
{
    /// <summary>receta | botin | bolsa | tienda | otro.</summary>
    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";
    [JsonPropertyName("cantidadResultado")] public int CantidadResultado { get; set; } = 1;
    [JsonPropertyName("ingredientes")] public List<Ingrediente> Ingredientes { get; set; } = [];
    [JsonPropertyName("estaciones")] public List<string> Estaciones { get; set; } = [];
    [JsonPropertyName("condiciones")] public List<string> Condiciones { get; set; } = [];
    /// <summary>NPC/objeto del que cae (botin/bolsa) o que lo vende (tienda).</summary>
    [JsonPropertyName("de")] public string? De { get; set; }
    [JsonPropertyName("probabilidad")] public string Probabilidad { get; set; } = "";
    [JsonPropertyName("condicion")] public string Condicion { get; set; } = "";
    /// <summary>archivo:linea del codigo decompilado del que sale el dato.</summary>
    [JsonPropertyName("fuente")] public string FuenteCodigo { get; set; } = "";
}

public sealed class Ingrediente
{
    [JsonPropertyName("ref")] public string? Ref { get; set; }
    [JsonPropertyName("grupo")] public string? Grupo { get; set; }
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; } = 1;
}
