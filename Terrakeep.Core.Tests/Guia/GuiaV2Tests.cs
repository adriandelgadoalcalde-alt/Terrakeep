using System.Text;
using System.Text.Json;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Guia;
using Terrakeep.Core.Guia.V2;
using Terrakeep.Core.Model;
using Terrakeep.Core.Nbt;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.Core.Tests.Guia;

// Guia v2 (F0, 02-oct-2026): pruebas unitarias del modelo, el evaluador, el progreso manual, el
// validador, las ubicaciones y los lectores nuevos de estado de Calamity.
public class GuiaV2Tests
{
    // ---------------------------------------------------------------- fuente de estado falsa

    private sealed class EstadoFalso : IGuideStateProviderV2
    {
        public bool HasCharacterData { get; set; } = true;
        public bool HasWorldData { get; set; } = true;
        public bool HasInventoryData { get; set; } = true;
        public bool HasLiveGameData => false;
        public int CristalesVida => Math.Clamp((VidaMaxima - 100) / 20, 0, 15);
        public int VidaMaxima { get; set; } = 100;
        public int Defensa => 0;
        public Dictionary<int, int> Posee { get; } = [];
        public HashSet<int> Puesto { get; } = [];
        public HashSet<string> Banderas { get; } = [];
        public HashSet<string> Mejoras { get; } = [];
        public HashSet<string> EstadosMundo { get; } = [];
        public ModoPartida? Modo { get; set; }
        public int NpcsDelPueblo() => 0;
        public bool HayNpc(int id) => false;
        public int CuantosLleva(int id) => Posee.GetValueOrDefault(id);
        public int DanoDelMejorArma(out string nombre) { nombre = ""; return 0; }
        public bool LlevaGancho(out string nombre) { nombre = ""; return false; }
        public bool BanderaConocida(string bandera) => bandera.StartsWith("downed", StringComparison.Ordinal);
        public bool? ValorBandera(string bandera) => HasWorldData ? Banderas.Contains(bandera) : null;
        public string? MotivoBanderaDesconocida(string bandera) => null;
        public string NombreDeObjeto(int id) => "obj" + id;
        public string NombreDeNpc(int id) => "npc" + id;
        public string MotivoSinPartidaEnMarcha(TipoRequisitoGuia tipo) => "x";
        public int CuantosPosee(int id) => Posee.GetValueOrDefault(id);
        public bool LlevaEquipado(int id) => Puesto.Contains(id);
        public bool MejoraConocida(string clave) => clave is "bloodOrange" or "demonHeart";
        public bool? MejoraPermanente(string clave) => HasCharacterData ? Mejoras.Contains(clave) : null;
        public bool EstadoMundoConocido(string clave) => clave is "revenge" or "HasFoundPlanetoidSchematic";
        public bool? EstadoMundo(string clave) => HasWorldData ? EstadosMundo.Contains(clave) : null;
        public int FrutasVida => Math.Clamp((VidaMaxima - 400) / 5, 0, 20);
        public int ManaMaxima => 20;
    }

    // Proveedor que solo implementa la interfaz v1 (como un TerrakeepMod sin actualizar).
    private sealed class EstadoSoloV1 : IGuideStateProvider
    {
        public bool HasCharacterData => true;
        public bool HasWorldData => true;
        public bool HasInventoryData => true;
        public bool HasLiveGameData => true;
        public int CristalesVida => 0;
        public int VidaMaxima => 100;
        public int Defensa => 0;
        public int NpcsDelPueblo() => 0;
        public bool HayNpc(int id) => false;
        public int CuantosLleva(int id) => 0;
        public int DanoDelMejorArma(out string nombre) { nombre = ""; return 0; }
        public bool LlevaGancho(out string nombre) { nombre = ""; return false; }
        public bool BanderaConocida(string bandera) => true;
        public bool? ValorBandera(string bandera) => false;
        public string? MotivoBanderaDesconocida(string bandera) => null;
        public string NombreDeObjeto(int id) => "";
        public string NombreDeNpc(int id) => "";
        public string MotivoSinPartidaEnMarcha(TipoRequisitoGuia tipo) => "";
    }

    private static GuiaV2Referencias Refs() => new()
    {
        Objetos =
        {
            ["Terraria/SlimeCrown"] = new RefObjeto { Id = 560, Es = "Corona de slime", En = "Slime Crown" },
            ["Terraria/LifeCrystal"] = new RefObjeto { Id = 29, Es = "Cristal de vida", En = "Life Crystal" },
            ["CalamityMod/DesertMedallion"] = new RefObjeto { Es = "Medallon del desierto", En = "Desert Medallion" },
        },
        Npcs =
        {
            ["Terraria/KingSlime"] = new RefNpc { Id = 50, Es = "Rey slime", En = "King Slime", Jefe = true },
        },
    };

    private static GuiaV2Doc DocMinimo() => new()
    {
        Esquema = 2,
        Id = "prueba",
        Ambito = "vanilla",
        Clases = ["cuerpo_a_cuerpo", "magia"],
        Capitulos = [new CapituloRuta { Id = "pre", Titulo = "Antes" }],
        Zonas = [new Zona { Id = "superficie", Nombre = "Superficie", Capa = "superficie" }],
        Articulos = [new Articulo { Id = "mapa", Titulo = "Mapa", Bloques = [new Bloque { Texto = "Ve a {z:superficie}." }] }],
        Paradas =
        [
            new Parada
            {
                Id = "slime", Capitulo = "pre", Titulo = "Rey slime", Jefes = ["Terraria/KingSlime"],
                Ubicaciones = [new Ubicacion { Tipo = "zona", Id = "superficie" }],
                Invocacion = new Invocacion { Objeto = "Terraria/SlimeCrown" },
                CompletadaCuando = new Condicion { Tipo = "bandera", Bandera = "downedSlimeKing" },
                Tareas =
                [
                    new Tarea { Id = "slime.1", Texto = "Fabrica la {o:Terraria/SlimeCrown}.", Condicion = new Condicion { Tipo = "objeto_poseido", Ref = "Terraria/SlimeCrown" } },
                    new Tarea { Id = "slime.2", Texto = "Prepara una arena.", Manual = true },
                    new Tarea { Id = "slime.3", Texto = "Solo magos.", Manual = true, Clases = ["magia"] },
                ],
            },
            new Parada
            {
                Id = "desvio", Capitulo = "pre", Titulo = "Desvio", Opcional = true,
                Ubicaciones = [new Ubicacion { Tipo = "punto", Id = "spawn" }],
                Tareas = [new Tarea { Id = "desvio.1", Texto = "Algo", Manual = true }],
            },
            new Parada
            {
                Id = "final", Capitulo = "pre", Titulo = "Final",
                Ubicaciones = [new Ubicacion { Tipo = "punto", Id = "mazmorra" }],
                Tareas = [new Tarea { Id = "final.1", Texto = "Vida", Condicion = new Condicion { Tipo = "vida_maxima", Valor = 200 } }],
            },
        ],
        Escaleras =
        [
            new EscaleraClase
            {
                Clase = "magia",
                Etapas =
                [
                    new EtapaEscalera { Id = "m1", Desde = "slime", Momento = "Inicio" },
                    new EtapaEscalera { Id = "m2", Desde = "final", Momento = "Final" },
                ],
            },
        ],
    };

    private static GuiaV2Evaluador Evaluador(GuiaV2Doc? doc = null) => new(doc ?? DocMinimo(), new ResolutorRefsGuia(Refs()));

    // ---------------------------------------------------------------- texto

    [Fact]
    public void Texto_AnalizaNegritaYTokens()
    {
        var s = GuiaV2Texto.Analizar("Usa **la {o:Terraria/SlimeCrown|corona}** en {z:superficie}.");
        Assert.Equal(TipoSegmento.Texto, s[0].Tipo);
        Assert.Equal("Usa ", s[0].Valor);
        Assert.Equal(TipoSegmento.Negrita, s[1].Tipo);
        Assert.Equal(TipoSegmento.Objeto, s[2].Tipo);
        Assert.Equal("Terraria/SlimeCrown", s[2].Valor);
        Assert.Equal("corona", s[2].TextoPropio);
        Assert.Equal(TipoSegmento.Zona, s[4].Tipo);
    }

    [Fact]
    public void Texto_PlanoSustituyeNombresEnEspanol()
    {
        Assert.Equal("Invoca con Corona de slime al Rey slime.",
            GuiaV2Texto.Plano("Invoca con {o:Terraria/SlimeCrown} al {n:Terraria/KingSlime}.", Refs()));
    }

    // ---------------------------------------------------------------- condiciones

    [Fact]
    public void Condicion_ObjetoPoseido_CuentaConElIdResueltoDeLaTabla()
    {
        var ds = new EstadoFalso();
        var ev = Evaluador();
        var c = new Condicion { Tipo = "objeto_poseido", Ref = "Terraria/SlimeCrown" };
        Assert.Equal(EstadoCondicion.NoCumplida, ev.EvaluarCondicion(c, ds).Estado);
        ds.Posee[560] = 1;
        Assert.Equal(EstadoCondicion.Cumplida, ev.EvaluarCondicion(c, ds).Estado);
    }

    [Fact]
    public void Condicion_RefSinResolver_EsNoEvaluableNuncaCumplida()
    {
        // Calamity sin resolutor de mod: el escritorio sin catalogo no puede saberlo.
        var c = new Condicion { Tipo = "objeto_poseido", Ref = "CalamityMod/DesertMedallion" };
        var r = Evaluador().EvaluarCondicion(c, new EstadoFalso());
        Assert.Equal(EstadoCondicion.NoEvaluable, r.Estado);
        Assert.Equal("guide_motive_unresolved_ref", r.Hoja!.MotivoClave);
    }

    [Fact]
    public void Condicion_ConResolutorDeMod_ResuelveCalamity()
    {
        var ev = new GuiaV2Evaluador(DocMinimo(), new ResolutorRefsGuia(Refs(), r => r == "CalamityMod/DesertMedallion" ? 900001 : null));
        var ds = new EstadoFalso();
        ds.Posee[900001] = 1;
        Assert.Equal(EstadoCondicion.Cumplida, ev.EvaluarCondicion(new Condicion { Tipo = "objeto_poseido", Ref = "CalamityMod/DesertMedallion" }, ds).Estado);
    }

    [Fact]
    public void Condicion_TodasYAlguna()
    {
        var ds = new EstadoFalso { VidaMaxima = 300 };
        ds.Banderas.Add("downedBoss1");
        var ev = Evaluador();
        var vida = new Condicion { Tipo = "vida_maxima", Valor = 400 };
        var ojo = new Condicion { Tipo = "bandera", Bandera = "downedBoss1" };
        var todas = ev.EvaluarCondicion(new Condicion { Tipo = "todas", Condiciones = [vida, ojo] }, ds);
        var alguna = ev.EvaluarCondicion(new Condicion { Tipo = "alguna", Condiciones = [vida, ojo] }, ds);
        Assert.Equal(EstadoCondicion.NoCumplida, todas.Estado);
        Assert.Equal(0.875f, todas.Fraccion, 3); // (0,75 + 1) / 2
        Assert.Equal(EstadoCondicion.Cumplida, alguna.Estado);
    }

    [Fact]
    public void Condicion_SinMundoCargado_EsSinDatos()
    {
        var ds = new EstadoFalso { HasWorldData = false };
        var r = Evaluador().EvaluarCondicion(new Condicion { Tipo = "bandera", Bandera = "downedBoss1" }, ds);
        Assert.Equal(EstadoCondicion.SinDatos, r.Estado);
    }

    [Fact]
    public void Condicion_MejoraYEstadoDeMundo()
    {
        var ds = new EstadoFalso();
        var ev = Evaluador();
        var naranja = new Condicion { Tipo = "mejora_permanente", Clave = "bloodOrange" };
        var esquema = new Condicion { Tipo = "estado_mundo", Clave = "HasFoundPlanetoidSchematic" };
        Assert.Equal(EstadoCondicion.NoCumplida, ev.EvaluarCondicion(naranja, ds).Estado);
        ds.Mejoras.Add("bloodOrange");
        ds.EstadosMundo.Add("HasFoundPlanetoidSchematic");
        Assert.Equal(EstadoCondicion.Cumplida, ev.EvaluarCondicion(naranja, ds).Estado);
        Assert.Equal(EstadoCondicion.Cumplida, ev.EvaluarCondicion(esquema, ds).Estado);
        Assert.Equal(EstadoCondicion.NoEvaluable,
            ev.EvaluarCondicion(new Condicion { Tipo = "mejora_permanente", Clave = "inventada" }, ds).Estado);
    }

    [Fact]
    public void Condicion_FrutasVida_SeDerivaDeLaVidaMaxima()
    {
        var ds = new EstadoFalso { VidaMaxima = 450 };
        var r = Evaluador().EvaluarCondicion(new Condicion { Tipo = "frutas_vida", Valor = 20 }, ds);
        Assert.Equal(EstadoCondicion.NoCumplida, r.Estado);
        Assert.Equal(10, r.Hoja!.Actual);
    }

    [Fact]
    public void Condicion_TiposV2ConProveedorSoloV1_SonLimiteEstructural()
    {
        var r = Evaluador().EvaluarCondicion(new Condicion { Tipo = "objeto_poseido", Ref = "Terraria/SlimeCrown" }, new EstadoSoloV1());
        Assert.Equal(EstadoCondicion.NoEvaluable, r.Estado);
        Assert.Equal("guide_motive_v2_unsupported", r.Hoja!.MotivoClave);
    }

    [Fact]
    public void Condicion_TipoDesconocido_NoEvaluable()
    {
        var r = Evaluador().EvaluarCondicion(new Condicion { Tipo = "magia_negra" }, new EstadoFalso());
        Assert.Equal(EstadoCondicion.NoEvaluable, r.Estado);
    }

    // ---------------------------------------------------------------- paradas y progreso

    [Fact]
    public void Parada_SeCompletaSolaConLaBanderaDelJefe()
    {
        var ds = new EstadoFalso();
        var progreso = new GuiaV2ProgresoManual();
        var ev = Evaluador();
        Assert.Equal("slime", ev.Evaluar(ds, progreso, null).Siguiente!.Parada.Id);
        ds.Banderas.Add("downedSlimeKing");
        var resumen = ev.Evaluar(ds, progreso, null);
        Assert.True(resumen.Paradas[0].Completada);
        Assert.Equal("desvio", resumen.Siguiente!.Parada.Id);
    }

    [Fact]
    public void Parada_AplazadaSeSaltaYMarcadaAManoCuenta()
    {
        var ds = new EstadoFalso();
        var progreso = new GuiaV2ProgresoManual();
        progreso.MarcarParada("slime", true);
        progreso.AplazarParada("desvio", true);
        var resumen = Evaluador().Evaluar(ds, progreso, null);
        Assert.Equal("final", resumen.Siguiente!.Parada.Id);
        Assert.Equal(1, resumen.ParadasCompletadas);
    }

    [Fact]
    public void Tareas_ManualesPersistidasYFiltradasPorClase()
    {
        var ds = new EstadoFalso();
        ds.Posee[560] = 1;
        var progreso = new GuiaV2ProgresoManual();
        progreso.MarcarTarea("slime.2", true);
        var ev = Evaluador();

        var melee = ev.Evaluar(ds, progreso, ClaseGuia.CuerpoACuerpo).Paradas[0];
        Assert.Equal(2, melee.Tareas.Count); // la de magia no aplica
        Assert.All(melee.Tareas, t => Assert.True(t.Hecha));
        Assert.True(melee.Tareas[0].Automatica);
        Assert.False(melee.Tareas[1].Automatica);

        var mago = ev.Evaluar(ds, progreso, ClaseGuia.Magia).Paradas[0];
        Assert.Equal(3, mago.Tareas.Count);
    }

    [Fact]
    public void Escalera_EtapaActualSigueALaSiguienteParada()
    {
        var ds = new EstadoFalso();
        var progreso = new GuiaV2ProgresoManual();
        var ev = Evaluador();
        Assert.Equal("m1", ev.EtapaActual(ClaseGuia.Magia, ev.Evaluar(ds, progreso, ClaseGuia.Magia))!.Id);
        progreso.MarcarParada("slime", true);
        progreso.MarcarParada("desvio", true);
        Assert.Equal("m2", ev.EtapaActual(ClaseGuia.Magia, ev.Evaluar(ds, progreso, ClaseGuia.Magia))!.Id);
        Assert.Equal("m1", ev.EtapaParaParada(ClaseGuia.Magia, "desvio")!.Id);
        Assert.Null(ev.EtapaActual(ClaseGuia.Picaro, ev.Evaluar(ds, progreso, ClaseGuia.Picaro)));
    }

    [Fact]
    public void Avisos_FiltradosPorModoReal()
    {
        var avisos = new List<AvisoModo>
        {
            new() { Id = "a", Modos = ["revengeance"], Texto = "rev" },
            new() { Id = "b", Modos = [], Texto = "siempre" },
            new() { Id = "c", Modos = ["maestro"], Texto = "maestro" },
        };
        var modo = new ModoPartida(1, true, false);
        Assert.Equal(["experto", "revengeance"], modo.Claves());
        Assert.Equal(["a", "b"], GuiaV2Evaluador.AvisosQueAplican(avisos, modo.Claves()).Select(a => a.Id));
        Assert.Equal(3, GuiaV2Evaluador.AvisosQueAplican(avisos, null).Count());
    }

    [Fact]
    public void Progreso_JsonIdaYVueltaYTextoInvalido()
    {
        var p = new GuiaV2ProgresoManual { Guia = "calamity", Clase = "picaro" };
        p.MarcarTarea("desert.1", true);
        p.MarcarParada("desert", true);
        var vuelta = GuiaV2ProgresoJson.Deserializar(GuiaV2ProgresoJson.Serializar(p), "calamity", out bool valido);
        Assert.True(valido);
        Assert.True(vuelta.TareaMarcada("desert.1"));
        Assert.True(vuelta.ParadaMarcada("desert"));
        Assert.Equal("picaro", vuelta.Clase);

        var roto = GuiaV2ProgresoJson.Deserializar("{no es json", "calamity", out bool valido2);
        Assert.False(valido2);
        Assert.Empty(roto.Tareas);
    }

    [Fact]
    public void Progreso_ArchivoPorPersonaje_GuardaYCarga()
    {
        string dir = Path.Combine(Path.GetTempPath(), "tk-guia-v2-" + Guid.NewGuid().ToString("N"));
        try
        {
            var p = new GuiaV2ProgresoManual { Guia = "vanilla" };
            p.MarcarTarea("slime.2", true);
            ArchivoProgresoGuia.Guardar(dir, "Mi Personaje/raro", p);
            Assert.Equal("guia-vanilla-Mi_Personaje_raro.json", ArchivoProgresoGuia.NombreArchivo("vanilla", "Mi Personaje/raro"));
            var cargado = ArchivoProgresoGuia.Cargar(dir, "vanilla", "Mi Personaje/raro");
            Assert.True(cargado.TareaMarcada("slime.2"));
            Assert.Empty(ArchivoProgresoGuia.Cargar(dir, "vanilla", "otro").Tareas);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    // ---------------------------------------------------------------- validador

    [Fact]
    public void Validador_DocMinimoEsValido()
    {
        Assert.Empty(GuiaV2Validador.Validar(DocMinimo(), Refs()));
    }

    [Fact]
    public void Validador_DetectaReferenciasRotasYTareasMudas()
    {
        var doc = DocMinimo();
        doc.Paradas[0].Tareas.Add(new Tarea { Id = "slime.9", Texto = "Coge {o:Terraria/NoExiste} y ve a {p:nada}" });
        doc.Paradas[0].Tareas.Add(new Tarea { Id = "slime.10", Texto = "x", Condicion = new Condicion { Tipo = "bandera", Bandera = "downedInventado" } });
        doc.Paradas[0].Jefes.Add("CalamityMod/NoHay");
        var errores = GuiaV2Validador.Validar(doc, Refs());
        Assert.Contains(errores, e => e.Contains("Terraria/NoExiste", StringComparison.Ordinal));
        Assert.Contains(errores, e => e.Contains("parada desconocida 'nada'", StringComparison.Ordinal));
        Assert.Contains(errores, e => e.Contains("slime.9: sin condicion y sin marcar manual", StringComparison.Ordinal));
        Assert.Contains(errores, e => e.Contains("downedInventado", StringComparison.Ordinal));
        Assert.Contains(errores, e => e.Contains("CalamityMod/NoHay", StringComparison.Ordinal));
    }

    [Fact]
    public void Cifras_CuentanTareasEvaluables()
    {
        var c = GuiaV2Cifras.Calcular(DocMinimo(), Refs());
        Assert.Equal(3, c.Paradas);
        Assert.Equal(5, c.Tareas);
        Assert.Equal(2, c.TareasEvaluables);
        Assert.Equal(40.0, c.PorcentajeEvaluable, 3);
    }

    // ---------------------------------------------------------------- ubicaciones

    private sealed class MundoFalso(int ancho, int alto) : IMundoGuia
    {
        public int[,] Tiles { get; } = Rellenar(ancho, alto);
        public Dictionary<(int, int), string> Mod { get; } = [];
        public Dictionary<string, (int, int)> Puntos { get; } = [];
        public int Ancho => ancho;
        public int Alto => alto;
        public int NivelSuperficie => 100;
        public int NivelRoca => 200;
        public int SpawnX => ancho / 2;
        public int SpawnY => 90;
        public int MazmorraX => 30;
        public int MazmorraY => 95;
        public int TileVanilla(int x, int y) => Tiles[x, y];
        public string? TileMod(int x, int y) => Mod.GetValueOrDefault((x, y));
        public (int X, int Y)? Punto(string clave) => Puntos.TryGetValue(clave, out var p) ? p : null;

        private static int[,] Rellenar(int w, int h)
        {
            var t = new int[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) t[x, y] = -1;
            return t;
        }
    }

    [Fact]
    public void Ubicacion_PorFirmaDeTiles_EncuentraElCentroDelBloque()
    {
        var mundo = new MundoFalso(400, 400);
        for (int x = 300; x < 340; x++) for (int y = 250; y < 290; y++) mundo.Tiles[x, y] = 53; // arena
        var zona = new Zona { Id = "desierto", Nombre = "Desierto", Firma = new FirmaZona { Tiles = [53], Minimo = 100 } };
        var r = GuiaV2Ubicaciones.Resolver(zona, mundo)!;
        Assert.False(r.Aproximada);
        Assert.InRange(r.X, 300, 340);
        Assert.InRange(r.Y, 250, 290);
    }

    [Fact]
    public void Ubicacion_TileDeModYPuntoGuardado()
    {
        var mundo = new MundoFalso(200, 200);
        for (int x = 10; x < 30; x++) for (int y = 150; y < 170; y++) mundo.Mod[(x, y)] = "CalamityMod/Navystone";
        mundo.Puntos["SunkenSeaLabCenter"] = (77, 160);
        var mar = new Zona { Id = "mar", Nombre = "Mar", Firma = new FirmaZona { TilesMod = ["CalamityMod/Navystone"], Minimo = 50 } };
        Assert.InRange(GuiaV2Ubicaciones.Resolver(mar, mundo)!.X, 10, 30);
        var lab = new Zona { Id = "lab", Nombre = "Lab", Punto = "SunkenSeaLabCenter" };
        Assert.Equal((77, 160), (GuiaV2Ubicaciones.Resolver(lab, mundo)!.X, GuiaV2Ubicaciones.Resolver(lab, mundo)!.Y));
    }

    [Fact]
    public void Ubicacion_SinFirmaEnElMundo_EsNullYCapaEsAproximada()
    {
        var mundo = new MundoFalso(100, 400);
        Assert.Null(GuiaV2Ubicaciones.Resolver(new Zona { Id = "x", Nombre = "x", Firma = new FirmaZona { Tiles = [999] } }, mundo));
        var capa = GuiaV2Ubicaciones.Resolver(new Zona { Id = "inf", Nombre = "Inframundo", Capa = "infierno" }, mundo)!;
        Assert.True(capa.Aproximada);
        Assert.Equal(300, capa.Y);
    }

    [Fact]
    public void Ubicacion_DeParadaRespetaElMalDelMundo()
    {
        var doc = DocMinimo();
        doc.Zonas.Add(new Zona { Id = "corr", Nombre = "Corrupcion", Firma = new FirmaZona { Tiles = [25], Minimo = 1 } });
        doc.Zonas.Add(new Zona { Id = "carm", Nombre = "Carmesi", Firma = new FirmaZona { Tiles = [203], Minimo = 1 } });
        var parada = new Parada
        {
            Id = "mal",
            Ubicaciones = [new Ubicacion { Tipo = "zona", Id = "corr", SiMundo = "corrupcion" }, new Ubicacion { Tipo = "zona", Id = "carm", SiMundo = "carmesi" }],
        };
        var mundo = new MundoFalso(200, 200);
        for (int x = 0; x < 20; x++) for (int y = 0; y < 20; y++) mundo.Tiles[x, y] = 25;
        for (int x = 150; x < 170; x++) for (int y = 150; y < 170; y++) mundo.Tiles[x, y] = 203;
        Assert.True(GuiaV2Ubicaciones.ResolverParada(parada, doc, mundo, mundoCarmesi: true)!.X >= 150);
        Assert.True(GuiaV2Ubicaciones.ResolverParada(parada, doc, mundo, mundoCarmesi: false)!.X < 20);
    }

    // ---------------------------------------------------------------- estado guardado de Calamity

    private static NbtCompound ModData(string sistema, NbtCompound data) => NbtCompound.Of(("modData",
        new NbtList(NbtTagType.Compound,
        [
            NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString(sistema)), ("data", data)),
        ])));

    [Fact]
    public void CalamityEstado_LeeRevengeanceEsquemasYLaboratorios()
    {
        var data = NbtCompound.Of(
            ("downed", new NbtList(NbtTagType.String, [new NbtString("revenge"), new NbtString("HasFoundPlanetoidSchematic")])),
            ("SunkenSeaLabCenter", NbtCompound.Of(("x", new NbtFloat(1600f)), ("y", new NbtFloat(3200f)))),
            ("IceLabCenter", NbtCompound.Of(("x", new NbtFloat(0f)), ("y", new NbtFloat(0f)))),
            ("abyssChasmBottom", new NbtInt(900)));
        var estado = CalamityEstadoGuardado.LeerMundo(TplrFile.Write("", ModData("MiscWorldStateSystem", data)));
        Assert.Contains("revenge", estado.Claves);
        Assert.Contains("HasFoundPlanetoidSchematic", estado.Claves);
        Assert.Equal((100, 200), estado.PuntosEnCasillas["SunkenSeaLabCenter"]);
        Assert.False(estado.PuntosEnCasillas.ContainsKey("IceLabCenter")); // (0,0) = no generado
        Assert.Equal(900, estado.Enteros["abyssChasmBottom"]);
    }

    [Fact]
    public void CalamityEstado_LeeMejorasDelJugador()
    {
        var data = NbtCompound.Of(("boost", new NbtList(NbtTagType.String, [new NbtString("bloodOrange"), new NbtString("rageOne")])));
        var mejoras = CalamityEstadoGuardado.LeerMejorasJugador(ModData("CalamityPlayer", data));
        Assert.Equal(2, mejoras.Count);
        Assert.Contains("bloodOrange", mejoras);
        Assert.Empty(CalamityEstadoGuardado.LeerMejorasJugador(null));
    }

    [Fact]
    public void TwldReader_LeeTambienLasBanderasNuevasDeLaGuiaV2()
    {
        var root = NbtCompound.Of(("modData", new NbtList(NbtTagType.Compound,
        [
            NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("DownedBossSystem")),
                ("data", NbtCompound.Of(("downedFlags", new NbtList(NbtTagType.String, [new NbtString("leviathan"), new NbtString("eocRain")]))))),
        ])));
        var flags = TwldReader.ReadCalamityDownedFlags(TplrFile.Write("", root));
        Assert.Contains("downedLeviathan", flags);
        Assert.Contains("downedEoCAcidRain", flags);
        Assert.True(GuideFlags.Existe("downedLeviathan"));
    }

    // ---------------------------------------------------------------- proveedor de escritorio

    private static GuideEvaluator EvaluadorEscritorio()
    {
        var names = VanillaItemCatalog.LoadFromStreams(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, string> { ["29"] = "Cristal de vida" }))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, string> { ["LifeCrystal"] = "Cristal de vida" }))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, int> { ["LifeCrystal"] = 29 }))));
        var npcs = NpcNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes("[]")));
        return new GuideEvaluator(names, npcs, null);
    }

    [Fact]
    public void Escritorio_CuantosPoseeSumaHuchaYEquipo_YMejorasVanillaDelPlr()
    {
        var personaje = new PlrCharacter
        {
            Version = 279,
            Name = "Prueba",
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            HealthMax = 500,
            ManaMax = 200,
            ExtraAccessory = true,
        };
        personaje.ExtraUsingFlags[1] = true; // usedAegisCrystal
        var contenedores = new Dictionary<string, GameItem[]>
        {
            ["inventory"] = [new GameItem { Id = 29, Count = 2 }],
            ["bank"] = [new GameItem { Id = 29, Count = 3 }],
            ["loadout0Items"] = [new GameItem { Id = 29, Count = 1 }],
        };
        var ds = EvaluadorEscritorio().CrearProveedor(new GuideContext { Character = personaje, MergedContainers = contenedores });
        Assert.Equal(6, ds.CuantosPosee(29));
        Assert.True(ds.LlevaEquipado(29));
        Assert.Equal(20, ds.FrutasVida);
        Assert.Equal(200, ds.ManaMaxima);
        Assert.True(ds.MejoraPermanente("demonHeart"));
        Assert.True(ds.MejoraPermanente("aegisCrystal"));
        Assert.False(ds.MejoraPermanente("aegisFruit"));
        Assert.Null(ds.MejoraPermanente("bloodOrange")); // sin .tplr cargado: no se sabe
        Assert.Null(ds.Modo); // sin mundo
    }
}
