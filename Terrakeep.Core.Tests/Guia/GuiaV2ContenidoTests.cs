using Terrakeep.Core.Guia;
using Terrakeep.Core.Guia.V2;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.Guia;

// Guia v2 (F0, 02-oct-2026): gate de CONTENIDO real (los .json incrustados en Terrakeep.Core.dll,
// los mismos que leeran Terrakeep y TerrakeepMod):
//   - todas las referencias a objetos/NPC/zonas/paradas resuelven a una entrada con nombre (y id
//     real si es vanilla), y toda tarea tiene condicion de vocabulario conocido o es "manual";
//   - la profundidad de la guia Calamity es al menos la de la guia HTML del usuario (47 paradas,
//     11 capitulos, 186 tareas, ~16.000 palabras - su verificacion.json: 16124).
public class GuiaV2ContenidoTests(ITestOutputHelper salida)
{
    private static readonly Lazy<GuiaV2Referencias> Refs = new(GuiaV2Cargador.CargarReferenciasIncrustadas);

    [Fact]
    public void LaGuiaCalamityEstaIncrustada()
    {
        Assert.Contains("calamity", GuiaV2Cargador.GuiasDisponibles());
    }

    [Theory]
    [MemberData(nameof(Guias))]
    public void ContenidoValido_TodasLasReferenciasResuelvenYLasTareasSonEvaluablesOManuales(string id)
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada(id);
        var errores = GuiaV2Validador.Validar(doc, Refs.Value);
        foreach (var e in errores.Take(80)) salida.WriteLine(e);
        Assert.True(errores.Count == 0, $"{errores.Count} errores de contenido en la guia '{id}' (primeros en la salida de la prueba).");
    }

    [Theory]
    [MemberData(nameof(Guias))]
    public void TablaDeReferencias_NombresYIdsReales(string id)
    {
        _ = id;
        var r = Refs.Value;
        Assert.All(r.Objetos, kv =>
        {
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.Es), kv.Key);
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.FuenteEs), kv.Key);
            if (kv.Key.StartsWith("Terraria/", StringComparison.Ordinal)) Assert.True(kv.Value.Id > 0, kv.Key);
            foreach (var o in kv.Value.Obtencion)
            {
                Assert.False(string.IsNullOrEmpty(o.FuenteCodigo), kv.Key + " sin archivo:linea");
                foreach (var i in o.Ingredientes)
                {
                    if (i.Ref != null) Assert.True(r.Objetos.ContainsKey(i.Ref), $"{kv.Key}: ingrediente {i.Ref} sin nombre");
                    if (i.Grupo != null) Assert.True(r.Grupos.ContainsKey(i.Grupo), $"{kv.Key}: grupo {i.Grupo} sin nombre");
                }
                foreach (var est in o.Estaciones) Assert.True(r.Estaciones.ContainsKey(est), $"{kv.Key}: estacion {est} sin nombre");
                if (o.De != null) Assert.True(r.Objetos.ContainsKey(o.De) || r.Npcs.ContainsKey(o.De), $"{kv.Key}: origen {o.De} sin nombre");
            }
        });
        Assert.All(r.Npcs, kv =>
        {
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.Es), kv.Key);
            // NPCID admite ids NEGATIVOS reales (variantes por netID: BlackSlime = -6...).
            if (kv.Key.StartsWith("Terraria/", StringComparison.Ordinal)) Assert.True(kv.Value.Id is not null and not 0, kv.Key);
        });
    }

    [Fact]
    public void Calamity_ProfundidadMinimaDeLaGuiaDelUsuario()
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada("calamity");
        var c = GuiaV2Cifras.Calcular(doc, Refs.Value);
        salida.WriteLine($"capitulos de ruta {c.Capitulos}, articulos {c.Articulos}, paradas {c.Paradas}, tareas {c.Tareas} " +
            $"(evaluables {c.TareasEvaluables} = {c.PorcentajeEvaluable:F1} %, manuales {c.TareasManuales}), palabras {c.Palabras}, " +
            $"zonas {c.Zonas}, etapas de escalera {c.EtapasEscalera}, problemas {c.Problemas}, hallazgos {c.Hallazgos}");
        Assert.True(c.Paradas >= 47, "paradas " + c.Paradas);
        Assert.True(c.Tareas >= 186, "tareas " + c.Tareas);
        // Su HTML: 10 capitulos de manual + la ruta = 11 entradas de navegacion. Aqui: articulos +
        // capitulos de ruta.
        Assert.True(c.Articulos + 1 >= 11, "capitulos " + (c.Articulos + 1));
        Assert.True(c.Palabras >= 16000, "palabras " + c.Palabras);
        // Todas las clases de Calamity con escalera propia.
        Assert.Equal(["cuerpo_a_cuerpo", "distancia", "magia", "invocacion", "picaro"], doc.Escaleras.Select(e => e.Clase));
        Assert.All(doc.Paradas, p => Assert.NotEmpty(p.Ubicaciones));
    }

    [Fact]
    public void Calamity_EvaluaContraUnaPartidaVacia_SinExcepcionesYSinNadaHechoDeMas()
    {
        var doc = GuiaV2Cargador.CargarGuiaIncrustada("calamity");
        var ev = new GuiaV2Evaluador(doc, new ResolutorRefsGuia(Refs.Value));
        var resumen = ev.Evaluar(new EstadoVacio(), new GuiaV2ProgresoManual { Guia = "calamity" }, ClaseGuia.Picaro);
        Assert.Equal(doc.Paradas.Count, resumen.Paradas.Count);
        Assert.Equal("inicio", resumen.Siguiente!.Parada.Id);
        Assert.Equal(0, resumen.ParadasCompletadas);
        Assert.Equal(0, resumen.TareasHechas);
        Assert.NotNull(ev.EtapaActual(ClaseGuia.Picaro, resumen));
    }

    public static TheoryData<string> Guias()
    {
        var d = new TheoryData<string>();
        foreach (var g in GuiaV2Cargador.GuiasDisponibles()) d.Add(g);
        return d;
    }

    // Personaje y mundo "recien creados": nada derrotado, nada en el inventario.
    private sealed class EstadoVacio : IGuideStateProviderV2
    {
        public bool HasCharacterData => true;
        public bool HasWorldData => true;
        public bool HasInventoryData => true;
        public bool HasLiveGameData => false;
        public int CristalesVida => 0;
        public int VidaMaxima => 100;
        public int Defensa => 0;
        public int NpcsDelPueblo() => 0;
        public bool HayNpc(int id) => false;
        public int CuantosLleva(int id) => 0;
        public int DanoDelMejorArma(out string nombre) { nombre = ""; return 0; }
        public bool LlevaGancho(out string nombre) { nombre = ""; return false; }
        public bool BanderaConocida(string bandera) => GuideFlags.Existe(bandera);
        public bool? ValorBandera(string bandera) => false;
        public string? MotivoBanderaDesconocida(string bandera) => null;
        public string NombreDeObjeto(int id) => "#" + id;
        public string NombreDeNpc(int id) => "#" + id;
        public string MotivoSinPartidaEnMarcha(TipoRequisitoGuia tipo) => "";
        public int CuantosPosee(int id) => 0;
        public bool LlevaEquipado(int id) => false;
        public bool MejoraConocida(string clave) => true;
        public bool? MejoraPermanente(string clave) => false;
        public bool EstadoMundoConocido(string clave) => true;
        public bool? EstadoMundo(string clave) => false;
        public int FrutasVida => 0;
        public int ManaMaxima => 20;
        public ModoPartida? Modo => new(1, false, false);
    }
}
