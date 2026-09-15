using System.Text.Json;
using Terrakeep.Core.Data;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): carga guia_progresion.json (copia sincronizada de TerrakeepMod, ver
// scripts/sync-guia-desde-terrakeepmod.ps1) y lo deja listo para evaluar - resuelve los strings
// brutos del JSON ("tipo"/"ambito"/"capa") a los enums reales y, para los tramos de Calamity, los
// pids "Mod/NombreInterno" ("idMod"/"idsMod") al id sintetico real que ya usa el resto de
// Terrakeep para objetos de Calamity (mismo CalamityCatalog, mismo id que aparece en un slot).
//
// Lo que NO se resuelve aqui, a proposito: "jefeMod"/"jefeFinalMod" (el NPC de Calamity que
// cierra un tramo). TerrakeepMod los resuelve en vivo con ModContent.TryFind contra el mod ya
// cargado; Terrakeep no tiene ningun catalogo de NPCs de Calamity (el suyo es solo de OBJETOS,
// ver CalamityCatalog) ni una partida en marcha de la que preguntar - se quedan sin resolver
// (Jefe=0), lo que hace que "Lectura del jefe" (vida/daño/defensa en vivo) no este disponible
// para ESOS pasos en la app de escritorio. Documentado, no disimulado - ver GuideEvaluator.
public sealed class GuideCatalog
{
    public IReadOnlyList<TramoGuia> Tramos { get; }
    public string Juego { get; }

    private GuideCatalog(GuiaProgresionDoc doc)
    {
        Juego = doc.Juego;
        Tramos = doc.Tramos.OrderBy(t => t.Orden).ToList();
    }

    public static GuideCatalog LoadFromFile(string rutaJson, CalamityCatalog? calamityCatalog)
    {
        using var stream = File.OpenRead(rutaJson);
        var doc = JsonSerializer.Deserialize<GuiaProgresionDoc>(stream)
            ?? throw new InvalidDataException($"\"{rutaJson}\" no contiene un documento de guia valido.");

        foreach (var tramo in doc.Tramos)
        {
            tramo.Ambito = tramo.AmbitoBruto switch
            {
                "calamity" => AmbitoGuia.Calamity,
                "ambos" => AmbitoGuia.Ambos,
                _ => AmbitoGuia.Vanilla,
            };

            foreach (var paso in tramo.Pasos)
            {
                paso.Tramo = tramo.Clave;
                paso.Capa = paso.CapaBruta switch
                {
                    "superficie" => CapaMundoGuia.Superficie,
                    "subterraneo" => CapaMundoGuia.Subterraneo,
                    "cavernas" => CapaMundoGuia.Cavernas,
                    "infierno" => CapaMundoGuia.Infierno,
                    _ => CapaMundoGuia.Cualquiera,
                };

                foreach (var req in paso.Requisitos)
                {
                    req.Tipo = req.TipoBruto switch
                    {
                        "cristales_vida" => TipoRequisitoGuia.CristalesVida,
                        "vida_maxima" => TipoRequisitoGuia.VidaMaxima,
                        "defensa" => TipoRequisitoGuia.Defensa,
                        "npcs_pueblo" => TipoRequisitoGuia.NpcsPueblo,
                        "npc" => TipoRequisitoGuia.Npc,
                        "npc_activo" => TipoRequisitoGuia.NpcActivo,
                        "objeto" => TipoRequisitoGuia.Objeto,
                        "objeto_cualquiera" => TipoRequisitoGuia.ObjetoCualquiera,
                        "dano_arma" => TipoRequisitoGuia.DanoArma,
                        "gancho" => TipoRequisitoGuia.Gancho,
                        "bandera" => TipoRequisitoGuia.Bandera,
                        _ => TipoRequisitoGuia.Desconocido,
                    };

                    ResolverReferenciasDeMod(req, calamityCatalog);
                }
            }
        }

        return new GuideCatalog(doc);
    }

    // "CalamityMod/DesertMedallion" -> (mod: "CalamityMod", interno: "DesertMedallion") ->
    // CalamityCatalog.ByModAndInternal -> id sintetico real (CalamityIds.ItemIdBase + indice),
    // el MISMO id que ya usa cualquier GameItem de Calamity en un slot de Terrakeep. Si el
    // catalogo no trae esa entrada (version de Calamity distinta a la indexada) se deja sin
    // resolver a proposito - GuideEvaluator lo trata como no evaluable, nunca inventa un id.
    private static void ResolverReferenciasDeMod(RequisitoGuia req, CalamityCatalog? catalogo)
    {
        if (catalogo == null) return;

        if (req.Id == 0 && !string.IsNullOrEmpty(req.IdMod))
        {
            int? resuelto = ResolverPid(req.IdMod, catalogo);
            if (resuelto.HasValue) req.Id = resuelto.Value;
        }

        if ((req.Ids == null || req.Ids.Length == 0) && req.IdsMod is { Length: > 0 })
        {
            var resueltos = new List<int>();
            foreach (var pid in req.IdsMod)
            {
                int? id = ResolverPid(pid, catalogo);
                if (id.HasValue) resueltos.Add(id.Value);
            }
            if (resueltos.Count > 0) req.Ids = [.. resueltos];
        }
    }

    private static int? ResolverPid(string pid, CalamityCatalog catalogo)
    {
        int barra = pid.IndexOf('/');
        if (barra <= 0 || barra >= pid.Length - 1) return null;
        string mod = pid[..barra];
        string interno = pid[(barra + 1)..];
        return catalogo.ByModAndInternal(mod, interno)?.SyntheticId;
    }
}
