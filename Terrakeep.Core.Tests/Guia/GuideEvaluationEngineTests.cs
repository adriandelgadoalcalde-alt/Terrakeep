using System.Text;
using System.Text.Json;
using Terrakeep.Core.Data;
using Terrakeep.Core.Guia;
using Terrakeep.Core.PlrFormat;
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

    private static WldWorld MakeWorld(
        bool downedBoss1 = false, bool downedBoss2 = false, IReadOnlyList<WldNpc>? npcs = null,
        bool downedGoblinArmy = false, bool downedFrostLegion = false, bool downedPirates = false) => new()
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
            DownedGoblinArmy = downedGoblinArmy, DownedFrostLegion = downedFrostLegion, DownedPirates = downedPirates,
        },
        Tiles = new WldTile[1, 1],
        Npcs = npcs ?? [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    private static PlrCharacter MakeCharacter(int healthMax) => new()
    {
        Version = 279,
        Name = "Personaje de prueba",
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        HealthMax = healthMax,
    };

    // Encargo2 (25-sep-2026): helpers para Defensa, mismo criterio de "solo campos JSON reales"
    // que MakeItemNames/MakeNpcNames de arriba - VanillaItemStatsCatalog/PrefixEffectCatalog se
    // cargan desde su propio formato JSON real, nunca con un mock a medida.
    private static VanillaItemStatsCatalog MakeStats(params (int Id, int Defense)[] items)
    {
        var raw = items.ToDictionary(i => i.Id.ToString(), i => new { defense = i.Defense });
        return VanillaItemStatsCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw))));
    }

    private static PrefixEffectCatalog MakePrefixEffects(params (int Id, double StatDefense)[] prefixes)
    {
        var raw = prefixes.ToDictionary(p => p.Id.ToString(), p => new Dictionary<string, double> { ["statDefense"] = p.StatDefense });
        return PrefixEffectCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw))));
    }

    // Guia Fase A REABIERTA (26-sep-2026): helpers gemelos de MakeStats/MakePrefixEffects de
    // arriba, pero para el campo real "damage"/"dmg" que usa DanoArma - mismo criterio de "solo
    // campos JSON reales", nunca un mock a medida.
    private static VanillaItemStatsCatalog MakeDamageStats(params (int Id, int Damage)[] items)
    {
        var raw = items.ToDictionary(i => i.Id.ToString(), i => new { damage = i.Damage });
        return VanillaItemStatsCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw))));
    }

    private static PrefixEffectCatalog MakePrefixEffectsDmg(params (int Id, double Dmg)[] prefixes)
    {
        var raw = prefixes.ToDictionary(p => p.Id.ToString(), p => new Dictionary<string, double> { ["dmg"] = p.Dmg });
        return PrefixEffectCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw))));
    }

    // Formato real de vanilla_prefix_rules.json (PrefixRulesCatalog.LoadFromStream): las 3
    // propiedades son obligatorias, aunque esten vacias - prefixesByCategory/itemPool no hacen
    // falta para DanoArma (solo VanillaCategories, via itemCategories).
    private static PrefixRulesCatalog MakeRules(params (int Id, string[] Categorias)[] items)
    {
        var raw = new
        {
            prefixesByCategory = new Dictionary<string, int[]>(),
            itemCategories = items.ToDictionary(i => i.Id.ToString(), i => i.Categorias),
            itemPool = new Dictionary<string, string>(),
        };
        return PrefixRulesCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(raw))));
    }

    // Los 10 slots reales de "Armadura/Accesorios" (Cabeza/Cuerpo/Piernas + 7 accesorios) que
    // MergedContainers["loadout0Items"] guarda para el equipo PUESTO de verdad - solo los primeros
    // items importan aqui, el resto queda vacio (GameItem.Empty, no aporta defensa).
    private static Terrakeep.Core.Model.GameItem[] MakeArmorSlots(params Terrakeep.Core.Model.GameItem[] puestos)
    {
        var slots = new Terrakeep.Core.Model.GameItem[10];
        for (int i = 0; i < 10; i++)
            slots[i] = i < puestos.Length ? puestos[i] : Terrakeep.Core.Model.GameItem.Empty;
        return slots;
    }

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
    // reflejaba el progreso real guardado en el mundo. Causa real ORIGINAL: el paso de
    // PREPARACION de cada tramo (p.ej. "ArmaContraLaMaldad") exige "dano_arma" como su UNICO
    // requisito obligatorio, y dano_arma era SIEMPRE NoEvaluable en Terrakeep de escritorio (no
    // simulaba combate) - antes de esa ronda eso bloqueaba el paso, y por tanto el tramo entero,
    // PARA SIEMPRE, sin ninguna relacion con si el jefe estaba realmente muerto en el .wld.
    //
    // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689): DanoArma YA NO es un limite
    // estructural fijo (ver DesktopGuideStateProvider.DanoDelMejorArma) - estos dos tests se
    // actualizan para seguir demostrando el MISMO sintoma real arreglado (el paso no se queda
    // bloqueado para siempre pese a que el jefe ya cayo en el .wld), pero ahora con el motivo
    // CORRECTO: se completa porque el arma real del personaje SI llega al umbral pedido, no
    // porque el requisito fuera estructuralmente imposible de comprobar. Sin personaje/inventario
    // cargado, el mismo requisito SI bloquea de verdad ahora (honestidad: "sin datos TODAVIA" -
    // ver los tests DanoArma_* de mas abajo).
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void PasoDePreparacion_ConDanoArmaObligatorioYArmaRealQueLlega_SeCompletaPorProgresoReal_YaNoPorLimiteEstructural()
    {
        var evaluador = new GuideEvaluator(MakeItemNames((4, "Espada larga de hierro")), MakeNpcNames(), null,
            MakeDamageStats((4, 12)), MakePrefixEffectsDmg((5, 1.15)), MakeRules((4, ["melee", "anyWeapon"])));
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 4, Prefix = Terrakeep.Core.Model.ItemPrefix.Vanilla(5) }],
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = inventario, World = MakeWorld() };

        var pasoPreparacion = new PasoGuia
        {
            Clave = "ArmaContraLaMaldadDePrueba",
            Requisitos =
            [
                new RequisitoGuia { Tipo = TipoRequisitoGuia.DanoArma, Valor = 10 }, // obligatorio - el arma real da 14, llega
                new() { Tipo = TipoRequisitoGuia.Bandera, Bandera = "shadowOrbSmashed", Recomendado = true },
            ],
        };

        bool completado = evaluador.PasoCompletado(pasoPreparacion, contexto);
        float preparacion = evaluador.Preparacion(pasoPreparacion, contexto, out int cumplidos, out int totalObligatorios);

        Assert.True(completado);
        // Ya NO es un limite estructural: cuenta como obligatorio real, y esta CUMPLIDO de
        // verdad (14 >= 10) - a diferencia de antes, que se completaba "vacuamente" sin ningun
        // requisito obligatorio real contando.
        Assert.Equal(1, totalObligatorios);
        Assert.Equal(1, cumplidos);
    }

    [Fact]
    public void MundoConElDevoradorYaDerrotado_ElPasoDeVencerloSaleCompletado_YElDePreparacionSeCompletaConElArmaRealQueLlega()
    {
        var evaluador = new GuideEvaluator(MakeItemNames((4, "Espada larga de hierro")), MakeNpcNames(), null,
            MakeDamageStats((4, 12)), MakePrefixEffectsDmg((5, 1.15)), MakeRules((4, ["melee", "anyWeapon"])));
        var mundo = MakeWorld(downedBoss2: true); // el Devorador de Mundos/Cerebro YA derrotado en el .wld
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 4, Prefix = Terrakeep.Core.Model.ItemPrefix.Vanilla(5) }],
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = inventario, World = mundo };

        var pasoPreparacion = new PasoGuia
        {
            Clave = "ArmaContraLaMaldad",
            Requisitos = [new RequisitoGuia { Tipo = TipoRequisitoGuia.DanoArma, Valor = 10 }],
        };
        var pasoVencer = new PasoGuia
        {
            Clave = "VencerLaMaldad",
            Requisitos = [Req(TipoRequisitoGuia.Bandera, bandera: "downedBoss2")],
        };

        // Sintoma real reportado: con el jefe YA derrotado en el .wld, los dos pasos del tramo
        // (preparacion Y derrota) tienen que poder marcarse completos - antes de la ronda del
        // 16-sep-2026, el de preparacion se quedaba bloqueado para siempre y ocultaba el progreso
        // real. Ahora (Fase A REABIERTA) se completa por una razon MEJOR: el arma real del
        // personaje de verdad llega al umbral pedido.
        Assert.True(evaluador.PasoCompletado(pasoPreparacion, contexto));
        Assert.True(evaluador.PasoCompletado(pasoVencer, contexto));
    }

    // ---------------------------------------------------------------------------------------
    // Encargo 1 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, auditoria 24-sep-2026): downedGoblins/
    // downedFrost/downedPirates ya se parseaban en WldHeader.cs/WldReader.cs pero GuideFlags.
    // _deMundo (este mismo namespace) no los conectaba - la Guia los marcaba SIEMPRE no
    // evaluable/desconocidos aunque el .wld real dijera lo contrario. Regresion real de
    // GuideFlags.Valor/Existe contra un WldHeader sintetico con las tres banderas nuevas, en
    // true y en false (hueco de cobertura real: este archivo no tenia NINGUNA prueba de
    // GuideFlags antes de esta ronda).
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("downedGoblins")]
    [InlineData("downedFrost")]
    [InlineData("downedPirates")]
    public void BanderasDeEventoTardio_SonConocidasPorGuideFlags(string bandera)
    {
        Assert.True(GuideFlags.Existe(bandera));
    }

    [Fact]
    public void MundoConLosTresEventosTardiosDerrotados_LasTresBanderasSalenCumplidas()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var mundo = MakeWorld(downedGoblinArmy: true, downedFrostLegion: true, downedPirates: true);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = mundo };

        var goblins = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedGoblins"), contexto);
        var frost = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedFrost"), contexto);
        var piratas = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedPirates"), contexto);

        Assert.False(goblins.NoEvaluable);
        Assert.True(goblins.Cumplido);
        Assert.False(frost.NoEvaluable);
        Assert.True(frost.Cumplido);
        Assert.False(piratas.NoEvaluable);
        Assert.True(piratas.Cumplido);
    }

    [Fact]
    public void MundoSinDerrotarLosTresEventosTardios_LasTresBanderasSalenNoCumplidas_NuncaNoEvaluables()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        // MakeWorld() por defecto ya deja los tres campos a false - un mundo real donde todavia
        // no se han invocado esos eventos.
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = MakeWorld() };

        var goblins = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedGoblins"), contexto);
        var frost = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedFrost"), contexto);
        var piratas = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedPirates"), contexto);

        Assert.False(goblins.NoEvaluable);
        Assert.False(goblins.Cumplido);
        Assert.False(frost.NoEvaluable);
        Assert.False(frost.Cumplido);
        Assert.False(piratas.NoEvaluable);
        Assert.False(piratas.Cumplido);
    }

    // ---------------------------------------------------------------------------------------
    // Encargo 1: cristales_vida derivado de HealthMax (formula real del motor vanilla,
    // Player.cs ~linea 56437/55952 del tModLoader decompilado: "ConsumedLifeCrystals =
    // (statLifeMax - 100) / 20"). Antes de esta ronda CristalesVida devolvia SIEMPRE 0 y
    // ademas quedaba detras del gate HasLiveGameData (fijo a false en escritorio) - dos
    // fallos independientes, cubiertos aqui: el valor calculado Y que ahora SI se evalua con
    // solo un personaje cargado (sin partida en marcha).
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(100, 0)]   // base sin cristales consumidos
    [InlineData(120, 1)]   // un cristal exacto
    [InlineData(180, 4)]   // varios cristales, division exacta
    [InlineData(190, 4)]   // resto que no llega al siguiente cristal (division entera)
    [InlineData(500, 15)]  // 400 de vida maxima extra = tope real de 15 cristales
    [InlineData(700, 15)]  // por encima del tope (Vida Suprema/Calamity) - clamp, nunca > 15
    public void CristalesVida_SeCalculaConLaFormulaRealDelMotor(int healthMax, int cristalesEsperados)
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = MakeCharacter(healthMax), MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.CristalesVida, cantidad: cristalesEsperados), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.Equal(cristalesEsperados, resultado.Actual);
    }

    [Fact]
    public void CristalesVida_ConVidaMaximaBase_NoCumpleUnRequisitoDeUnCristal_SinQuedarNoEvaluable()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.CristalesVida, cantidad: 1), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.False(resultado.Cumplido);
        Assert.Equal(0, resultado.Actual);
    }

    [Fact]
    public void CristalesVida_SinPersonajeCargado_QuedaNoEvaluable_ConMotivoDePersonaje_NoComoLimiteEstructural()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.CristalesVida, cantidad: 1), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_character", resultado.MotivoClave);
        // A diferencia de dano_arma (limite ESTRUCTURAL: jamas evaluable en escritorio),
        // cristales_vida SI se resuelve solo cargando el personaje - no debe bloquear un paso
        // para siempre como el bug real de dano_arma cerrado el 16-sep-2026.
        Assert.False(resultado.EsLimiteEstructural);
    }

    // ---------------------------------------------------------------------------------------
    // Guia Encargo2 (25-sep-2026, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): defensa REAL, en vez de
    // fija a 0 con gate HasLiveGameData (fijo a false en escritorio, ver el hallazgo original en
    // bitacora.md). Mismo criterio que CristalesVida arriba - derivable de datos ya cargados del
    // .plr (aqui: MergedContainers["loadout0Items"], el equipo PUESTO de verdad, via
    // Terrakeep.Core.Model.DefenseCalculator, la misma formula ya usada por
    // EquipmentGroupViewModel en la pestaña Equipamiento) - gate HasCharacterData, no
    // HasLiveGameData.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Defensa_SeCalculaConElEquipoPuestoDeVerdad_ArmaduraMasPrefijoDeAccesorio()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null,
            MakeStats((1, 5), (2, 8)), MakePrefixEffects((62, 1))); // prefijo 62 = "Warding" real, +1 defensa

        var casco = new Terrakeep.Core.Model.GameItem { Id = 1 };
        var pechera = new Terrakeep.Core.Model.GameItem { Id = 2 };
        var accesorio = new Terrakeep.Core.Model.GameItem { Id = 3, Prefix = Terrakeep.Core.Model.ItemPrefix.Vanilla(62) };
        var merged = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["loadout0Items"] = MakeArmorSlots(casco, pechera, Terrakeep.Core.Model.GameItem.Empty, accesorio),
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = merged, World = null };

        // 5 (casco) + 8 (pechera) + 1 (bono de prefijo Warding del accesorio) = 14.
        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Defensa, cantidad: 14), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.Equal(14, resultado.Actual);
        Assert.True(resultado.Cumplido);
    }

    [Fact]
    public void Defensa_SinPersonajeCargado_QuedaNoEvaluable_ConMotivoDePersonaje_NoComoLimiteEstructural()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null,
            MakeStats((1, 5)), MakePrefixEffects());
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Defensa, cantidad: 1), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_character", resultado.MotivoClave);
        // Mismo criterio que CristalesVida: Defensa SI se resuelve solo cargando el personaje, no
        // debe bloquear el paso PARA SIEMPRE como un limite estructural real (dano_arma).
        Assert.False(resultado.EsLimiteEstructural);
    }

    // ---------------------------------------------------------------------------------------
    // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689, I+D-PROXIMOS-PASOS-FAMILIA-
    // KEEP.md): DanoArma SI se puede evaluar de forma real y util desde un .plr estatico (daño
    // base+prefijo del arma equipada/en inventario) - ya NO es un limite estructural fijo. Oraculo
    // real del primer test: Item.cs decompilado (tModLoader-Decompiled/tModLoader/Terraria/
    // Item.cs) - SetDefaults case 4 = Iron Broadsword, damage=12; TryGetPrefixStatMultipliersFor
    // Item case 5 = "Sharp" (PrefixID.Sharp=5), dmg=1.15f -> 12 * 1.15 = 13.8, redondeado (Math.
    // Round, .8 siempre sube) a 14.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void DanoArma_ConArmaVanillaYPrefijoReal_ActualCoincideConElDanoHorneadoRealDelDecompilado()
    {
        var evaluador = new GuideEvaluator(MakeItemNames((4, "Espada larga de hierro")), MakeNpcNames(), null,
            MakeDamageStats((4, 12)), MakePrefixEffectsDmg((5, 1.15)), MakeRules((4, ["melee", "anyWeapon"])));
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 4, Prefix = Terrakeep.Core.Model.ItemPrefix.Vanilla(5) }],
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = inventario, World = null };

        var resultado = evaluador.Evaluar(new RequisitoGuia { Tipo = TipoRequisitoGuia.DanoArma, Valor = 10 }, contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.False(resultado.EsLimiteEstructural);
        Assert.Equal(14, resultado.Actual);
        Assert.True(resultado.Cumplido);
        Assert.Equal("Espada larga de hierro", resultado.TextoArgs[0]);
    }

    [Fact]
    public void DanoArma_SinNingunArmaEnElInventario_QuedaNoEvaluable_ConMotivoClaro_NuncaComoLimiteEstructural()
    {
        // Inventario cargado de verdad (HasInventoryData=true), pero el unico item (una pocion)
        // no es ningun arma real - VanillaCategories(50) = None, EsArmaVanilla descarta.
        var evaluador = new GuideEvaluator(MakeItemNames((50, "Poción de vida")), MakeNpcNames(), null,
            MakeDamageStats(), MakePrefixEffectsDmg(), MakeRules());
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 50, Count = 1 }],
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = inventario, World = null };

        var resultado = evaluador.Evaluar(new RequisitoGuia { Tipo = TipoRequisitoGuia.DanoArma, Valor = 10 }, contexto);

        Assert.True(resultado.NoEvaluable);
        // Distincion real de esta ronda: "arma no encontrada" es NoEvaluable normal (se resolveria
        // llevando un arma real), nunca un limite ESTRUCTURAL (que bloquearia/desbloquearia el
        // paso para siempre sin relacion con lo que el jugador lleve encima de verdad).
        Assert.False(resultado.EsLimiteEstructural);
        Assert.Equal("guide_motive_weapon_damage", resultado.MotivoClave);
    }

    [Fact]
    public void Bandera_Conocida_SinMundoCargado_MotivoEsCargaElMundo_NoElGenericoDeAntes()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        // Con personaje SI cargado (para descartar que el motivo real sea "falta el personaje")
        // pero SIN mundo - una bandera de mundo NUNCA depende del .plr.
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = null, World = null };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.Bandera, bandera: "downedBoss1"), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.Equal("guide_motive_load_world", resultado.MotivoClave);
    }

    [Fact]
    public void NpcActivo_SigueSiendoLimiteEstructuralSiempre_RegresionNoDebeCambiarConEsteEncargo()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames((4, "Ojo de Cthulhu")), null);
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = MakeWorld() };

        var resultado = evaluador.Evaluar(Req(TipoRequisitoGuia.NpcActivo, id: 4), contexto);

        Assert.True(resultado.NoEvaluable);
        Assert.True(resultado.EsLimiteEstructural);
        Assert.Equal("guide_motive_active_npc", resultado.MotivoClave);
    }

    [Fact]
    public void PasoConDanoArmaYaEvaluable_YaNoCuentaComoLimiteEstructural_SumaProgresoParcialRealEnVezDeVacuo()
    {
        // Arma real que da 14 (mismo oraculo del primer test), pero el paso pide 20 - no llega.
        var evaluador = new GuideEvaluator(MakeItemNames((4, "Espada larga de hierro")), MakeNpcNames(), null,
            MakeDamageStats((4, 12)), MakePrefixEffectsDmg((5, 1.15)), MakeRules((4, ["melee", "anyWeapon"])));
        var inventario = new Dictionary<string, Terrakeep.Core.Model.GameItem[]>
        {
            ["inventory"] = [new Terrakeep.Core.Model.GameItem { Id = 4, Prefix = Terrakeep.Core.Model.ItemPrefix.Vanilla(5) }],
        };
        var contexto = new GuideContext { Character = MakeCharacter(100), MergedContainers = inventario, World = null };
        var paso = new PasoGuia
        {
            Clave = "PruebaDanoArmaEvaluable",
            Requisitos = [new RequisitoGuia { Tipo = TipoRequisitoGuia.DanoArma, Valor = 20 }],
        };

        bool completado = evaluador.PasoCompletado(paso, contexto);
        float preparacion = evaluador.Preparacion(paso, contexto, out int cumplidos, out int totalObligatorios);

        // Antes de esta ronda, DanoArma SIEMPRE era limite estructural en escritorio: el paso se
        // habria dado por completado solo (huboLimiteEstructural, sin ningun requisito obligatorio
        // real) y Preparacion habria sido 1f (vacuo). Ahora es un requisito NORMAL: no completado
        // (14 < 20) y con progreso PARCIAL real (14/20 = 0,7), el mismo criterio ya documentado en
        // Preparacion ("3 de 4 vecinos son 0,75, no un cero").
        Assert.False(completado);
        Assert.Equal(1, totalObligatorios);
        Assert.Equal(0, cumplidos);
        Assert.Equal(0.7f, preparacion, 3);
    }
}
