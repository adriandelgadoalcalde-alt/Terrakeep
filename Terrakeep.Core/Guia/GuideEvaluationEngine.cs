namespace Terrakeep.Core.Guia;

// El CEREBRO UNICO de la Guia (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, T1/H3, 16-sep-2026). Antes de
// esta consolidacion existian DOS copias funcion-a-funcion identicas de este archivo: EvaluadorGuia.cs
// en TerrakeepMod y GuideEvaluator.cs aqui mismo, sincronizadas a mano y con un script
// (sync-guia-desde-terrakeepmod.ps1) que solo copiaba los DATOS, nunca comprobaba que la LOGICA
// seguia igual en los dos sitios. El riesgo real que eso abria (documentado en el propio informe
// de I+D): alguien añade un requisito nuevo en el mod, lo verifica en vivo, y la app de escritorio
// sigue evaluandolo con la aritmetica vieja - SIN dar ningun error, con un resultado plausible y
// equivocado.
//
// Ahora solo hay una implementacion, aqui, y los dos consumidores (TerrakeepMod via
// ProveedorEstadoGuiaMod, Terrakeep via DesktopGuideStateProvider) le pasan su propio
// IGuideStateProvider. Ver GuideEvaluator.cs (el adaptador que mantiene la API publica de
// Terrakeep.App sin cambios) y la cabecera de IGuideStateProvider.cs para el reparto completo.
public static class GuideEvaluationEngine
{
    public static ResultadoRequisitoGuia Evaluar(RequisitoGuia requisito, IGuideStateProvider ds)
    {
        var r = new ResultadoRequisitoGuia { Requisito = requisito };

        switch (requisito.Tipo)
        {
            case TipoRequisitoGuia.CristalesVida:
                if (!ds.HasLiveGameData) { NoEvaluableFijo(r, ds.MotivoSinPartidaEnMarcha(requisito.Tipo)); break; }
                Contar(r, ds.CristalesVida, requisito.Valor, "Guia.Req.CristalesVida");
                break;

            case TipoRequisitoGuia.VidaMaxima:
                if (!ds.HasCharacterData) { NoEvaluableSinDatos(r, "guide_motive_load_character"); break; }
                Contar(r, ds.VidaMaxima, requisito.Valor, "Guia.Req.VidaMaxima");
                break;

            case TipoRequisitoGuia.Defensa:
                if (!ds.HasLiveGameData) { NoEvaluableFijo(r, ds.MotivoSinPartidaEnMarcha(requisito.Tipo)); break; }
                Contar(r, ds.Defensa, requisito.Valor, "Guia.Req.Defensa");
                break;

            case TipoRequisitoGuia.NpcsPueblo:
                if (!ds.HasWorldData) { NoEvaluableSinDatos(r, "guide_motive_load_world"); break; }
                Contar(r, ds.NpcsDelPueblo(), requisito.Valor, "Guia.Req.NpcsPueblo");
                break;

            case TipoRequisitoGuia.Npc:
                EvaluarNpc(r, requisito, ds, "Guia.Req.Npc");
                break;

            case TipoRequisitoGuia.NpcActivo:
                // A diferencia de un NPC del pueblo (que persiste con posicion en el mundo), un
                // enemigo hostil activo (p.ej. una Sonda Marciana en ESTE instante) es un dato de
                // simulacion en curso: solo existe con una partida en marcha de verdad.
                if (!ds.HasLiveGameData) { NoEvaluableFijo(r, ds.MotivoSinPartidaEnMarcha(requisito.Tipo)); break; }
                EvaluarNpc(r, requisito, ds, "Guia.Req.NpcActivo");
                break;

            case TipoRequisitoGuia.Objeto:
                EvaluarObjeto(r, requisito, ds);
                break;

            case TipoRequisitoGuia.ObjetoCualquiera:
                EvaluarObjetoCualquiera(r, requisito, ds);
                break;

            case TipoRequisitoGuia.DanoArma:
                if (!ds.HasLiveGameData) { NoEvaluableFijo(r, ds.MotivoSinPartidaEnMarcha(requisito.Tipo)); break; }
                EvaluarDanoArma(r, requisito, ds);
                break;

            case TipoRequisitoGuia.Gancho:
                EvaluarGancho(r, ds);
                break;

            case TipoRequisitoGuia.Bandera:
                EvaluarBandera(r, requisito, ds);
                break;

            default:
                r.NoEvaluable = true;
                r.TextoClave = "Guia.Req.NoEvaluable";
                r.TextoArgs = [string.IsNullOrEmpty(requisito.TipoBruto) ? "?" : requisito.TipoBruto];
                break;
        }

        return r;
    }

    public static List<ResultadoRequisitoGuia> Evaluar(PasoGuia paso, IGuideStateProvider ds)
        => paso.Requisitos.Select(req => Evaluar(req, ds)).ToList();

    /// <summary>true si el paso se puede dar por hecho: todos sus requisitos OBLIGATORIOS
    /// EVALUABLES (ver mas abajo) estan cumplidos. Un requisito NoEvaluable nunca cuenta como
    /// cumplido - salvo que sea un LIMITE ESTRUCTURAL (<see cref="ResultadoRequisitoGuia.
    /// EsLimiteEstructural"/>: este proveedor JAMAS podra comprobarlo, cargue lo que cargue -
    /// p.ej. dano de arma real en Terrakeep de escritorio), que no cuenta ni a favor ni en
    /// contra: bloquearia el paso PARA SIEMPRE sin relacion con el progreso real (bug real
    /// encontrado el 16-sep-2026, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md - un mundo con el Devorador
    /// de Mundos ya derrotado seguia mostrando ese tramo como pendiente, porque su paso de
    /// preparacion exigia un dano de arma que Terrakeep nunca puede medir). Un requisito
    /// obligatorio "sin datos TODAVIA" (personaje/mundo no cargados ahora mismo, pero que SI se
    /// resolveria cargandolos) sigue bloqueando de verdad - ahi la honestidad manda: no fingir
    /// completo lo que aun no se sabe. Un paso sin ningun requisito obligatorio EVALUABLE (todos
    /// recomendados, o todos estructuralmente imposibles) nunca se completa por si solo.</summary>
    public static bool PasoCompletado(PasoGuia paso, IGuideStateProvider ds)
    {
        bool hayObligatorio = false;
        foreach (var requisito in paso.Requisitos)
        {
            if (requisito.Recomendado) continue;
            hayObligatorio = true; // cuenta como "habia un obligatorio" aunque sea un limite estructural
            var resultado = Evaluar(requisito, ds);
            if (resultado.EsLimiteEstructural) continue; // no bloquea: este proveedor jamas lo sabra
            if (!resultado.Cumplido) return false;
        }
        return hayObligatorio;
    }

    /// <summary>Medidor de preparacion de 0 a 1: un requisito obligatorio pesa el doble que uno
    /// recomendado, y cada uno aporta su progreso PARCIAL (3 de 4 vecinos son 0,75, no un cero).
    /// Los limites ESTRUCTURALES (ver <see cref="PasoCompletado"/>) quedan fuera de la media -
    /// ni suman ni restan, igual que no bloquean la completitud - salvo que sean los UNICOS
    /// requisitos del paso, caso en el que el paso se cuenta vacuamente al 100% (coherente con
    /// que <see cref="PasoCompletado"/> tambien lo de por hecho en ese caso).</summary>
    public static float Preparacion(PasoGuia paso, IGuideStateProvider ds, out int cumplidos, out int totalObligatorios)
    {
        cumplidos = 0;
        totalObligatorios = 0;
        if (paso.Requisitos.Count == 0) return 0f;

        float suma = 0f, pesoTotal = 0f;
        bool huboLimiteEstructural = false;
        foreach (var requisito in paso.Requisitos)
        {
            var resultado = Evaluar(requisito, ds);
            if (resultado.EsLimiteEstructural) { huboLimiteEstructural = true; continue; }

            float peso = requisito.Recomendado ? 1f : 2f;
            pesoTotal += peso;
            suma += peso * Fraccion(resultado);

            if (!requisito.Recomendado)
            {
                totalObligatorios++;
                if (resultado.Cumplido) cumplidos++;
            }
        }
        if (pesoTotal <= 0f) return huboLimiteEstructural ? 1f : 0f;
        return suma / pesoTotal;
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

    /// <summary>NoEvaluable "sin datos TODAVIA" (bandera/personaje/mundo/inventario conocidos por
    /// este proveedor, pero no cargados ahora mismo). En TerrakeepMod nunca se llega a usar (sus
    /// Has* son siempre true). <paramref name="motivoClave"/> distingue QUE falta cargar
    /// (personaje/mundo) en vez de la clave generica unica de antes - bug real encontrado el
    /// 16-sep-2026 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): el motivo de escritorio siempre decia
    /// "carga un personaje/mundo" sin decir cual de los dos, y encima <paramref name="queNecesita"/>
    /// se perdia (el parametro nunca se usaba), asi que TODAS las lineas sin datos mostraban la
    /// MISMA frase generica letra por letra, indistinguibles entre un objeto, un NPC o un aviso
    /// generico - exactamente el sintoma real reportado ("da igual donde toques, siempre pone lo
    /// mismo"). Con nombre real (objeto/NPC, que SI se puede resolver desde el catalogo sin
    /// personaje/mundo cargado) la linea distingue una entrada de otra de verdad.</summary>
    private static void NoEvaluableSinDatos(ResultadoRequisitoGuia r, string motivoClave, string? queNecesita = null)
    {
        r.NoEvaluable = true;
        r.Pedido = 1;
        if (string.IsNullOrEmpty(queNecesita))
        {
            r.TextoClave = "Guia.Req.NoEvaluableGenerico";
            r.TextoArgs = [];
        }
        else
        {
            r.TextoClave = "Guia.Req.NoEvaluable";
            r.TextoArgs = [queNecesita];
        }
        r.MotivoClave = motivoClave;
    }

    /// <summary>NoEvaluable "fijo": este proveedor NUNCA puede comprobar este tipo de requisito
    /// (p.ej. daño de arma desde un editor de ficheros estaticos), con su motivo real.
    /// <see cref="ResultadoRequisitoGuia.EsLimiteEstructural"/> queda marcado para que
    /// <see cref="PasoCompletado"/>/<see cref="Preparacion"/> no bloqueen el paso para siempre
    /// por algo que jamas se podra saber - ver el porque completo en esos dos metodos.</summary>
    private static void NoEvaluableFijo(ResultadoRequisitoGuia r, string motivoClave)
    {
        r.NoEvaluable = true;
        r.EsLimiteEstructural = true;
        r.Pedido = 1;
        r.TextoClave = "Guia.Req.NoEvaluableGenerico";
        r.TextoArgs = [];
        r.MotivoClave = motivoClave;
    }

    private static void EvaluarNpc(ResultadoRequisitoGuia r, RequisitoGuia requisito, IGuideStateProvider ds, string clave)
    {
        if (!ds.HasWorldData) { NoEvaluableSinDatos(r, "guide_motive_load_world", ds.NombreDeNpc(requisito.Id)); return; }
        bool hay = ds.HayNpc(requisito.Id);
        r.Actual = hay ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = hay;
        r.TextoClave = clave;
        r.TextoArgs = [ds.NombreDeNpc(requisito.Id)];
    }

    private static void EvaluarObjeto(ResultadoRequisitoGuia r, RequisitoGuia requisito, IGuideStateProvider ds)
    {
        if (!ds.HasInventoryData) { NoEvaluableSinDatos(r, "guide_motive_load_character", ds.NombreDeObjeto(requisito.Id)); return; }

        int lleva = ds.CuantosLleva(requisito.Id);
        r.Actual = lleva;
        r.Pedido = requisito.Cantidad;
        r.Cumplido = lleva >= requisito.Cantidad;
        r.TextoClave = requisito.Cantidad > 1 ? "Guia.Req.ObjetoVarios" : "Guia.Req.Objeto";
        r.TextoArgs = requisito.Cantidad > 1
            ? [ds.NombreDeObjeto(requisito.Id), lleva, requisito.Cantidad]
            : [ds.NombreDeObjeto(requisito.Id)];
    }

    private static void EvaluarObjetoCualquiera(ResultadoRequisitoGuia r, RequisitoGuia requisito, IGuideStateProvider ds)
    {
        var ids = requisito.Ids ?? [];
        if (!ds.HasInventoryData)
        {
            NoEvaluableSinDatos(r, "guide_motive_load_character", string.Join(" / ", ids.Select(ds.NombreDeObjeto)));
            return;
        }

        int mejor = 0;
        foreach (var id in ids)
        {
            int lleva = ds.CuantosLleva(id);
            if (lleva > mejor) mejor = lleva;
        }

        r.Actual = mejor;
        r.Pedido = requisito.Cantidad;
        r.Cumplido = mejor >= requisito.Cantidad;
        r.TextoClave = "Guia.Req.ObjetoCualquiera";
        r.TextoArgs = [string.Join(" / ", ids.Select(ds.NombreDeObjeto)), mejor, requisito.Cantidad];
    }

    private static void EvaluarDanoArma(ResultadoRequisitoGuia r, RequisitoGuia requisito, IGuideStateProvider ds)
    {
        int dano = ds.DanoDelMejorArma(out var nombre);
        r.Actual = dano;
        r.Pedido = requisito.Valor;
        r.Cumplido = dano >= requisito.Valor;
        r.TextoClave = string.IsNullOrEmpty(nombre) ? "Guia.Req.DanoArmaSinArma" : "Guia.Req.DanoArma";
        r.TextoArgs = string.IsNullOrEmpty(nombre) ? [requisito.Valor] : [nombre, dano, requisito.Valor];
    }

    private static void EvaluarGancho(ResultadoRequisitoGuia r, IGuideStateProvider ds)
    {
        if (!ds.HasInventoryData) { NoEvaluableSinDatos(r, "guide_motive_load_character"); return; }

        bool lleva = ds.LlevaGancho(out var nombre);
        r.Actual = lleva ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = lleva;
        r.TextoClave = lleva ? "Guia.Req.GanchoSi" : "Guia.Req.GanchoNo";
        r.TextoArgs = lleva ? [nombre] : [];
    }

    private static void EvaluarBandera(ResultadoRequisitoGuia r, RequisitoGuia requisito, IGuideStateProvider ds)
    {
        if (!ds.BanderaConocida(requisito.Bandera))
        {
            r.NoEvaluable = true;
            r.Pedido = 1;
            r.TextoClave = "Guia.Req.NoEvaluable";
            r.TextoArgs = ["bandera " + requisito.Bandera];
            r.MotivoClave = ds.MotivoBanderaDesconocida(requisito.Bandera);
            return;
        }

        bool? valor = ds.ValorBandera(requisito.Bandera);
        if (valor == null)
        {
            NoEvaluableSinDatos(r, "guide_motive_load_data");
            return;
        }

        r.Actual = valor.Value ? 1 : 0;
        r.Pedido = 1;
        r.Cumplido = valor.Value;
        r.TextoClave = "Guia.Bandera." + requisito.Bandera;
        r.TextoArgs = [];
    }
}
