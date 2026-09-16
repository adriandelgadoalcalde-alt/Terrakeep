using System.Text;
using System.Text.Json;
using Terrakeep.Core.Data;
using Terrakeep.Core.Guia;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.Guia;

// Regresion del reporte en directo del 16-sep-2026 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): "en
// terrakeep da igual donde toques de la guia que siempre pone lo mismo, esto la guia no sabe
// comprobarlo todavia". El arnes GUIA_SOLO existente (Terrakeep.App.Tests/PruebasGuiaYServidor.cs)
// SOLO probaba personaje+mundo reales cargados a la vez - nunca el escenario real de la captura
// del usuario ("Sin personaje cargado", solo un mundo). Estas pruebas cubren ese hueco real,
// contra la API PUBLICA de GuideEvaluator (mismo camino que usa Terrakeep.App.ViewModels.
// GuideViewModel), sin depender de InternalsVisibleTo (mismo criterio ya establecido en el
// proyecto).
public class GuideEvaluationEngineTests
{
    private static VanillaItemCatalog MakeItemNames(params (int Id, string Name)[] items)
    {
        var byId = items.ToDictionary(i => i.Id.ToString(), i => i.Name);
        var byKey = items.ToDictionary(i => "K" + i.Id, i => i.Name);
        var idsByKey = items.ToDictionary(i => "K" + i.Id, i => i.Id);
        return VanillaItemCatalog.LoadFromStreams(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(byId))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(byKey))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(idsByKey))));
    }

    private static NpcNameCatalog MakeNpcNames(params (int Id, string Name)[] npcs)
    {
        string json = JsonSerializer.Serialize(npcs.Select(n => new { id = n.Id, key = "K" + n.Id, es = n.Name }));
        return NpcNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    private static WldWorld MakeWorld(bool downedBoss1 = false, bool downedBoss2 = false, IReadOnlyList<WldNpc>? npcs = null) => new()
    {
        Header = new WldHeader
        {
            Version = 279,
            Pointers = new int[10],
            TileFrameImportant = [],
            Title = "Mundo de prueba",
            WorldId = 1,
            TilesHigh = 1,
            TilesWide = 1,
            SpawnX = 0,
            SpawnY = 0,
            GroundLevel = 100,
            RockLevel = 200,
            Seed = "semilla-sintetica",
            GameMode = 0,
            DungeonX = 0,
            DungeonY = 0,
            Time = 0, DayTime = true, MoonPhase = 0, BloodMoon = false, IsEclipse = false,
            IsCrimson = false, DownedBoss1EyeOfCthulhu = downedBoss1, DownedBoss2EaterOfWorldsOrBrainOfCthulhu = downedBoss2,
            DownedBoss3Skeletron = false, DownedQueenBee = false, DownedMechBoss1TheDestroyer = false,
            DownedMechBoss2TheTwins = false, DownedMechBoss3SkeletronPrime = false, DownedPlantBoss = false,
            DownedGolemBoss = false, DownedSlimeKingBoss = false, HardMode = false,
        },
        Tiles = new WldTile[1, 1],
        Npcs = npcs ?? [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    private static RequisitoGuia Req(TipoRequisitoGuia tipo, int id = 0, int cantidad = 1, string bandera = "") => new()
    {
        Tipo = tipo,
        Id = id,
        Cantidad = cantidad,
        Bandera = bandera,
    };

    // ---------------------------------------------------------------------------------------
    // Escenario real de la captura: mundo cargado, personaje NO cargado ("Sin personaje
    // cargado" en la cabecera de Terrakeep).
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void SinPersonaje_ConMundo_ObjetoQuedaNoEvaluable_PeroConNombreRealDelObjeto()
    {
        var evaluador = new GuideEvaluator(MakeItemNames((5, "Sable de cobre")), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = MakeWorld() };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Objeto, id: 5), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_character", resultado.MotivoClave);
        // Bug real cerrado esta ronda: antes TextoArgs era SIEMPRE [""] para todo objeto sin
        // datos, asi que la linea (Guia.Req.NoEvaluable, "Esto la guia no lo sabe comprobar
        // todavia: {0}") salia IDENTICA letra por letra para cualquier objeto - el sintoma
        // real reportado ("da igual donde toques, siempre pone lo mismo"). Ahora lleva el
        // nombre real del objeto (resoluble desde el catalogo sin inventario cargado).
        Assert.Single(resultado.TextoArgs);
        Assert.Equal("Sable de cobre", resultado.TextoArgs[0]);
    }

    [Fact]
    public void SinPersonaje_ConMundo_GanchoQuedaNoEvaluable_ConMotivoDePersonaje()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = MakeWorld() };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Gancho), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_character", resultado.MotivoClave);
        // Sin objeto concreto que nombrar (cualquier gancho vale) - forma "generica", pero
        // como clave DISTINTA de "Guia.Req.NoEvaluable" con args vacios (nunca el mismo texto
        // con un {0} colgando y vacio).
        Assert.Equal("Guia.Req.NoEvaluableGenerico", resultado.TextoClave);
        Assert.Empty(resultado.TextoArgs);
    }

    [Fact]
    public void SinPersonaje_ConMundo_NpcsPuebloSiEsEvaluable_PorqueSoloNecesitaElMundo()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames((17, "Guia")), null);
        var mundo = MakeWorld(npcs: [new WldNpc { Id = 17, GivenName = "", TileX = 10, TileY = 10, Homeless = false, VariationIndex = 0 }]);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = mundo };

        var npcsPueblo = evaluador.Evaluar(Req(TipoRequisitoGuia.NpcsPueblo, cantidad: 1), contexto);
        var npcConcreto = evaluador.Evaluar(Req(TipoRequisitoGuia.Npc, id: 17), contexto);

        // Confirma con evidencia real que el problema NO es "todo sale no evaluable sin
        // personaje": lo que solo depende del mundo (NPCs del pueblo, banderas de jefe que
        // Terrakeep ya lee) se evalua igual de bien con world != null, aunque no haya
        // personaje cargado.
        Assert.False(npcsPueblo.NoEvaluable);
        Assert.True(npcsPueblo.Cumplido);
        Assert.False(npcConcreto.NoEvaluable);
        Assert.True(npcConcreto.Cumplido);
    }

    [Fact]
    public void SinMundo_ConPersonaje_NpcsPuebloQuedaNoEvaluable_ConMotivoDeMundo()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.NpcsPueblo, cantidad: 1), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_world", resultado.MotivoClave);
    }

    // ---------------------------------------------------------------------------------------
    // Con los dos datos reales cargados: el camino ya cubierto por GUIA_SOLO, repetido aqui a
    // nivel unitario (rapido, sin arrancar la ventana real) para que quede en dotnet test.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ConPersonajeYMundo_ObjetoQueSiLlevas_SaleCumplidoConElNombreYCantidadReales()
    {
        var evaluador = new GuideEvaluator(MakeItemNames((5, "Sable de cobre")), MakeNpcNames(), null);
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 5, Count = 1 }],
        };
        var contexto = new GuideContext { Character = null, MergedContainers = inventario, World = MakeWorld() };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Objeto, id: 5, cantidad: 1), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.True(resultado.Cumplido);
    }

    [Fact]
    public void BanderaDeJefeYaLeidaDelWld_SeEvaluaCorrectamenteSoloConElMundo()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var mundoConOjoDerrotado = MakeWorld(downedBoss1: true);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = mundoConOjoDerrotado };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedBoss1"), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.True(resultado.Cumplido);
    }

    // ---------------------------------------------------------------------------------------
    // Bug real reportado en directo el 16-sep-2026 (segundo hallazgo, con partida real
    // avanzada): "si pones un mundo que ya te has pasado o esta a mas de la mitad, la guia
    // sigue poniendo lo del Devorador de Mundos, que es incorrecto cuando el cae" - la Guia NO
    // reflejaba el progreso real guardado en el mundo. Causa real: el paso de PREPARACION de
    // cada tramo (p.ej. "ArmaContraLaMaldad") exige "dano_arma" como su UNICO requisito
    // obligatorio, y dano_arma es SIEMPRE NoEvaluable en Terrakeep de escritorio (no simula
    // combate) - antes de esta ronda eso bloqueaba el paso, y por tanto el tramo entero, PARA
    // SIEMPRE, sin ninguna relacion con si el jefe estaba realmente muerto en el .wld.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void PasoDePreparacion_ConSoloDanoArmaObligatorio_SeCompletaSolo_PorqueEsUnLimiteEstructuralNoUnaFalta()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = MakeWorld() };

        var pasoPreparacion = new PasoGuia
        {
            Clave = "ArmaContraLaMaldadDePrueba",
            Requisitos =
            [
                Req(TipoRequisitoGuia.DanoArma, cantidad: 20), // obligatorio, SIEMPRE NoEvaluable en escritorio
                new() { Tipo = TipoRequisitoGuia.Bandera, Bandera = "shadowOrbSmashed", Recomendado = true },
            ],
        };

        bool completado = evaluador.PasoCompletado(pasoPreparacion, contexto);
        float preparacion = evaluador.Preparacion(pasoPreparacion, contexto, out int cumplidos, out int totalObligatorios);

        Assert.True(completado);
        Assert.Equal(0, totalObligatorios); // el unico obligatorio era un limite estructural - no cuenta ni bloquea
    }

    [Fact]
    public void MundoConElDevoradorYaDerrotado_ElPasoDeVencerloSaleCompletado_YElDePreparacionNoLoBloquea()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var mundo = MakeWorld(downedBoss2: true); // el Devorador de Mundos/Cerebro YA derrotado en el .wld
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = mundo };

        var pasoPreparacion = new PasoGuia
        {
            Clave = "ArmaContraLaMaldad",
            Requisitos = [Req(TipoRequisitoGuia.DanoArma, cantidad: 20)],
        };
        var pasoVencer = new PasoGuia
        {
            Clave = "VencerLaMaldad",
            Requisitos = [Req(TipoRequisitoGuia.Bandera, bandera: "downedBoss2")],
        };

        // Sintoma real reportado: con el jefe YA derrotado en el .wld, los dos pasos del tramo
        // (preparacion Y derrota) tienen que poder marcarse completos - antes de esta ronda, el
        // de preparacion se quedaba bloqueado para siempre y ocultaba el progreso real.
        Assert.True(evaluador.PasoCompletado(pasoPreparacion, contexto));
        Assert.True(evaluador.PasoCompletado(pasoVencer, contexto));
    }
}
