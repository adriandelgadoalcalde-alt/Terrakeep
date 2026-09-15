using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.Core.Guia;

// Fase B (15-sep-2026): el cerebro de la Guia dentro de Terrakeep - espejo deliberado de
// EvaluadorGuia.cs (TerrakeepMod), MISMO contrato honesto: un requisito que no se sepa comprobar
// se marca NoEvaluable y JAMAS cuenta como cumplido (ni aqui ni alli - "esto no lo se comprobar"
// es preferible a un falso verde, la misma filosofia con la que se escribio el mod).
//
// Diferencia real de fondo con el mod, y por que la mitad de los tipos de requisito quedan
// NoEvaluable aqui SIEMPRE (no es un hueco de esfuerzo, es una limitacion real del propio medio):
// TerrakeepMod pregunta al MOTOR EN MARCHA (Player.statDefense ya incluye armadura+accesorios+
// buffs calculados en vivo; Player.GetWeaponDamage aplica los multiplicadores reales de la
// clase). Terrakeep es un editor de ficheros .plr/.wld ESTATICOS - no hay combate, no hay
// multiplicadores de clase, no hay buffs activos que simular. Reproducir esos numeros a mano
// seria fingir una precision que no existe (y mentir seria peor que no tener el dato, la misma
// razon por la que el mod ya deja como NoEvaluable un tipo desconocido). Ver el comentario de
// cada caso mas abajo para el motivo real, caso por caso.
public sealed class GuideEvaluator(VanillaItemCatalog vanillaItems, NpcNameCatalog npcNames, CalamityCatalog? calamityItems)
{
    public ResultadoRequisitoGuia Evaluar(RequisitoGuia requisito, GuideContext contexto)
    {
        var r = new ResultadoRequisitoGuia { Requisito = requisito };

        switch (requisito.Tipo)
        {
            case TipoRequisitoGuia.CristalesVida:
                NoEvaluableEnEscritorio(r, "Guia.Req.CristalesVida",
                    "Terrakeep no guarda un contador de cristales de vida consumidos - solo el " +
                    "propio juego en marcha lo sabe (Player.ConsumedLifeCrystals no se guarda como " +
                    "un campo aparte legible desde el .plr con el resto de campos ya soportados).");
                break;

            case TipoRequisitoGuia.VidaMaxima:
                if (contexto.Character == null) { NoEvaluableSinDatos(r, "Guia.Req.VidaMaxima"); break; }
                Contar(r, contexto.Character.HealthMax, requisito.Valor, "Guia.Req.VidaMaxima");
                break;

            case TipoRequisitoGuia.Defensa:
                NoEvaluableEnEscritorio(r, "Guia.Req.Defensa",
                    "La defensa real depende de la armadura, los accesorios y los buffs activos " +
                    "calculados en vivo por el motor del juego (Player.statDefense) - Terrakeep no " +
                    "simula combate, asi que no puede reproducir ese numero sin fingirlo.");
                break;

            case TipoRequisitoGuia.NpcsPueblo:
                if (contexto.World == null) { NoEvaluableSinDatos(r, "Guia.Req.NpcsPueblo"); break; }
                Contar(r, contexto.World.Npcs.Count, requisito.Valor, "Guia.Req.NpcsPueblo");
                break;

            case TipoRequisitoGuia.Npc:
                EvaluarNpc(r, requisito, contexto, "Guia.Req.Npc");
                break;

            case TipoRequisitoGuia.NpcActivo:
                // A diferencia de los NPC del pueblo (que SI persisten con posicion en la seccion
                // NPCs del .wld, ver WldWorld.Npcs), un enemigo hostil activo (p.ej. una Sonda
                // Marciana en ese instante) nunca se guarda en el archivo - es un dato de
                // simulacion en curso, no de partida guardada. No hay ningun .wld del que
                // Terrakeep pueda leer esto, con o sin mas trabajo de lector.
                NoEvaluableEnEscritorio(r, "Guia.Req.NpcActivo",
                    "Un enemigo hostil activo en el mundo no se guarda en el .wld (solo los NPC " +
                    "del pueblo persisten con su posicion) - esto solo se puede saber jugando.");
                break;

            case TipoRequisitoGuia.Objeto:
                EvaluarObjeto(r, requisito, contexto);
                break;

            case TipoRequisitoGuia.ObjetoCualquiera:
                EvaluarObjetoCualquiera(r, requisito, contexto);
                break;

            case TipoRequisitoGuia.DanoArma:
                NoEvaluableEnEscritorio(r, "Guia.Req.DanoArmaSinArma",
                    "El daño real de un arma depende de la clase del personaje y de sus " +
                    "bonificaciones (Player.GetWeaponDamage), calculadas en vivo por el motor - " +
                    "Terrakeep no simula esos multiplicadores.");
                break;

            case TipoRequisitoGuia.Gancho:
                EvaluarGancho(r, contexto);
                break;

            case TipoRequisitoGuia.Bandera:
                EvaluarBandera(r, requisito, contexto);
                break;

            default:
                r.NoEvaluable = true;
                r.TextoClave = "Guia.Req.NoEvaluable";
                r.TextoArgs = [string.IsNullOrEmpty(requisito.TipoBruto) ? "?" : requisito.TipoBruto];
                break;
        }

        return r;
    }

    public List<ResultadoRequisitoGuia> Evaluar(PasoGuia paso, GuideContext contexto)
        => paso.Requisitos.Select(req => Evaluar(req, contexto)).ToList();

    /// <summary>true si el paso se puede dar por hecho: todos sus requisitos OBLIGATORIOS estan
    /// cumplidos (un requisito NoEvaluable nunca cuenta como cumplido, asi que un paso con algun
    /// requisito obligatorio que Terrakeep no sepa comprobar no se marca completo solo - mismo
    /// comportamiento honesto que el mod, sin necesitar un caso especial).</summary>
    public bool PasoCompletado(PasoGuia paso, GuideContext contexto)
    {
        bool hayObligatorio = false;
        foreach (var requisito in paso.Requisitos)
        {
            if (requisito.Recomendado) continue;
            hayObligatorio = true;
            if (!Evaluar(requisito, contexto).Cumplido) return false;
        }
        return hayObligatorio;
    }

    /// <summary>Medidor de preparacion de 0 a 1, mismo peso doble para obligatorios y progreso
    /// parcial que EvaluadorGuia.Preparacion del mod.</summary>
    public float Preparacion(PasoGuia paso, GuideContext contexto, out int cumplidos, out int totalObligatorios)
    {
        cumplidos = 0;
        totalObligatorios = 0;
        if (paso.Requisitos.Count == 0) return 0f;

        float suma = 0f, pesoTotal = 0f;
        foreach (var requisito in paso.Requisitos)
        {
            var resultado = Evaluar(requisito, contexto);
            float peso = requisito.Recomendado ? 1f : 2f;
            pesoTotal += peso;
            suma += peso * Fraccion(resultado);

            if (!requisito.Recomendado)
            {
                totalObligatorios++;
                if (resultado.Cumplido) cumplidos++;
            }
        }
        return pesoTotal <= 0f ? 0f : suma / pesoTotal;
    }

    private static float Fraccion(ResultadoRequisitoGuia r)
    {
        if (r.Cumplido) return 1f;
        if (r.NoEvaluable || r.Pedido <= 0) return 0f;
        float f = r.Actual / (float)r.Pedido;
        return f < 0f ? 0f : (f > 1f ? 1f : f);
    }

    // -----------------------------------------------------------------------------------------

    private static void Contar(ResultadoRequisitoGuia r, int actual, int pedido, string clave)
    {
        r.Actual = actual;
        r.Pedido = pedido;
        r.Cumplido = actual >= pedido;
        r.TextoClave = clave;
        r.TextoArgs = [actual, pedido];
    }

    private static void NoEvaluableSinDatos(ResultadoRequisitoGuia r, string clave)
    {
        r.NoEvaluable = true;
        r.Pedido = 1;
        r.TextoClave = "Guia.Req.NoEvaluable";
        r.TextoArgs = [""];
        r.MotivoNoEvaluableEnEscritorio =
            "Carga un personaje/mundo real para que la Guia pueda comprobar esto.";
    }

    private static void NoEvaluableEnEscritorio(ResultadoRequisitoGuia r, string claveVacia, string motivo)
    {
        r.NoEvaluable = true;
        r.Pedido = 1;
        r.TextoClave = "Guia.Req.NoEvaluable";
        r.TextoArgs = [""];
        r.MotivoNoEvaluableEnEscritorio = motivo;
    }

    private void EvaluarNpc(ResultadoRequisitoGuia r, RequisitoGuia requisito, GuideContext contexto, string clave)
    {
        if (contexto.World == null) { NoEvaluableSinDatos(r, clave); return; }
        bool hay = contexto.World.Npcs.Any(n => n.Id == requisito.Id);
        r.Actual = hay ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = hay;
        r.TextoClave = clave;
        r.TextoArgs = [npcNames.GetName(requisito.Id)];
    }

    private void EvaluarObjeto(ResultadoRequisitoGuia r, RequisitoGuia requisito, GuideContext contexto)
    {
        var inventario = Inventario(contexto);
        if (inventario == null) { NoEvaluableSinDatos(r, "Guia.Req.Objeto"); return; }

        int lleva = CuantosLleva(inventario, requisito.Id);
        r.Actual = lleva;
        r.Pedido = requisito.Cantidad;
        r.Cumplido = lleva >= requisito.Cantidad;
        r.TextoClave = requisito.Cantidad > 1 ? "Guia.Req.ObjetoVarios" : "Guia.Req.Objeto";
        r.TextoArgs = requisito.Cantidad > 1
            ? [NombreDeObjeto(requisito.Id), lleva, requisito.Cantidad]
            : [NombreDeObjeto(requisito.Id)];
    }

    private void EvaluarObjetoCualquiera(ResultadoRequisitoGuia r, RequisitoGuia requisito, GuideContext contexto)
    {
        var inventario = Inventario(contexto);
        if (inventario == null) { NoEvaluableSinDatos(r, "Guia.Req.ObjetoCualquiera"); return; }

        int mejor = 0;
        var ids = requisito.Ids ?? [];
        foreach (var id in ids)
        {
            int lleva = CuantosLleva(inventario, id);
            if (lleva > mejor) mejor = lleva;
        }

        r.Actual = mejor;
        r.Pedido = requisito.Cantidad;
        r.Cumplido = mejor >= requisito.Cantidad;
        r.TextoClave = "Guia.Req.ObjetoCualquiera";
        r.TextoArgs = [string.Join(" / ", ids.Select(NombreDeObjeto)), mejor, requisito.Cantidad];
    }

    private void EvaluarGancho(ResultadoRequisitoGuia r, GuideContext contexto)
    {
        var inventario = Inventario(contexto);
        if (inventario == null) { NoEvaluableSinDatos(r, "Guia.Req.GanchoNo"); return; }

        var encontrado = inventario.FirstOrDefault(i => !i.IsEmpty && GuideHooks.IdsDeGancho.Contains(i.Id));
        bool lleva = encontrado != null;
        r.Actual = lleva ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = lleva;
        r.TextoClave = lleva ? "Guia.Req.GanchoSi" : "Guia.Req.GanchoNo";
        r.TextoArgs = lleva ? [NombreDeObjeto(encontrado!.Id)] : [];
    }

    private static void EvaluarBandera(ResultadoRequisitoGuia r, RequisitoGuia requisito, GuideContext contexto)
    {
        if (!GuideFlags.Existe(requisito.Bandera))
        {
            r.NoEvaluable = true;
            r.Pedido = 1;
            r.TextoClave = "Guia.Req.NoEvaluable";
            r.TextoArgs = ["bandera " + requisito.Bandera];
            r.MotivoNoEvaluableEnEscritorio = contexto.HasCalamity
                ? "Esta bandera es de Calamity - Terrakeep todavia no lee los datos de mod del .wld."
                : "Esta bandera vive fuera del bloque de ancho fijo que Terrakeep sabe leer del " +
                  ".wld (ver GuideFlags.cs) - queda pendiente de una ronda futura del lector de mundos.";
            return;
        }

        bool? valor = GuideFlags.Valor(requisito.Bandera, contexto);
        if (valor == null)
        {
            NoEvaluableSinDatos(r, "Guia.Bandera." + requisito.Bandera);
            return;
        }

        r.Actual = valor.Value ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = valor.Value;
        r.TextoClave = "Guia.Bandera." + requisito.Bandera;
        r.TextoArgs = [];
    }

    // -----------------------------------------------------------------------------------------

    private static GameItem[]? Inventario(GuideContext contexto) =>
        contexto.MergedContainers != null && contexto.MergedContainers.TryGetValue("inventory", out var inv) ? inv : null;

    private static int CuantosLleva(GameItem[] inventario, int id)
    {
        if (id <= 0) return 0;
        int total = 0;
        foreach (var item in inventario)
            if (!item.IsEmpty && item.Id == id) total += item.Count;
        return total;
    }

    private string NombreDeObjeto(int id)
    {
        if (id <= 0) return "#" + id;
        if (id >= CalamityIds.ItemIdBase)
            return calamityItems?.BySyntheticId(id)?.DisplayName ?? "#" + id;
        return vanillaItems.GetName(id);
    }
}
