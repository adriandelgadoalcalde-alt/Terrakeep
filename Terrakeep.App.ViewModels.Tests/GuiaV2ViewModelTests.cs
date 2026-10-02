using System.IO;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Guia.V2;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Guia v2 (F2, 02-oct-2026): pruebas del ViewModel de la pestaña Guia nueva contra un personaje y
// un mundo SINTETICOS en memoria (nunca partidas reales): eleccion de guia por la partida, clase
// detectada por el arma real del inventario, evaluacion (paradas hechas solas, siguiente parada,
// tenencia), progreso manual persistido en una carpeta de prueba, ficha de obtencion sin recetas de
// Calamity en la guia vanilla, buscador y ubicacion exacta/aproximada sobre las casillas del mundo.
// El canario visual de la misma pantalla es GUIAV2_SOLO (Terrakeep.App.Tests/CanarioGuiaV2.cs).
public sealed class GuiaV2ViewModelTests : IDisposable
{
    private static readonly CharacterFileService Servicio = new();
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "terrakeep-guiav2-vm-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        try { if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, recursive: true); } catch (IOException) { }
    }

    private static bool HayVanilla => GuiaV2Cargador.GuiasDisponibles().Contains("vanilla");

    private static LoadedCharacter Personaje(int vida, params (int Id, int Cantidad)[] inventario)
    {
        var items = new GameItem[50];
        for (int i = 0; i < items.Length; i++) items[i] = GameItem.Empty;
        for (int i = 0; i < inventario.Length; i++) items[i] = new GameItem { Id = inventario[i].Id, Count = inventario[i].Cantidad };
        var plr = new PlrCharacter { Version = 279, Name = "PruebaVm", PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true), HealthMax = vida };
        return new LoadedCharacter(Path.Combine(Path.GetTempPath(), "PruebaVm.plr"), null, "", plr, null,
            new Dictionary<string, GameItem[]> { ["inventory"] = items });
    }

    private static WldWorld Mundo(bool ojoDerrotado = true)
    {
        const int ancho = 400, alto = 300, suelo = 100;
        var tiles = new WldTile[ancho, alto];
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++)
            {
                bool arena = x is >= 300 and < 360 && y is >= suelo and < suelo + 40;
                tiles[x, y] = y < suelo ? WldTile.Empty : new WldTile(type: (short)(arena ? 53 : 0), wall: 0, liquidType: 0, liquidAmount: 0, u: 0, v: 0);
            }
        return new WldWorld
        {
            Header = new WldHeader
            {
                Version = 279, Pointers = new int[10], TileFrameImportant = [], Title = "MundoVm", WorldId = 7,
                TilesHigh = alto, TilesWide = ancho, SpawnX = 200, SpawnY = suelo - 1, GroundLevel = suelo, RockLevel = 200,
                Seed = "vm", GameMode = 1, DungeonX = 40, DungeonY = 150, Time = 0, DayTime = true, MoonPhase = 0,
                BloodMoon = false, IsEclipse = false, IsCrimson = false, DownedBoss1EyeOfCthulhu = ojoDerrotado,
                DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false, DownedBoss3Skeletron = false, DownedQueenBee = false,
                DownedMechBoss1TheDestroyer = false, DownedMechBoss2TheTwins = false, DownedMechBoss3SkeletronPrime = false,
                DownedPlantBoss = false, DownedGolemBoss = false, DownedSlimeKingBoss = false, HardMode = false,
                DownedGoblinArmy = false, DownedFrostLegion = false, DownedPirates = false,
            },
            Tiles = tiles,
            Npcs = [new WldNpc { Id = 17, GivenName = "Mercader", TileX = 210, TileY = suelo - 1, Homeless = false, VariationIndex = 0 }],
            Chests = [], Signs = [], TileEntities = [], ShimmeredNpcTypes = new HashSet<int>(),
        };
    }

    private GuiaV2ViewModel Vm(LoadedCharacter? personaje, WldWorld? mundo, bool calamity = false)
    {
        var vm = new GuiaV2ViewModel(Servicio, () => personaje, () => mundo, () => calamity, () => null) { CarpetaProgreso = _carpeta };
        vm.Refresh(); // vuelve a leer el progreso ya desde la carpeta de prueba
        return vm;
    }

    [Fact]
    public void SinDatos_EnseñaLaGuiaEnteraSinMarcarNadaPorPartida()
    {
        var vm = Vm(null, null);
        Assert.False(vm.HayDatos);
        Assert.True(vm.ParadasTotal >= 40);
        Assert.Equal(vm.Paradas[0], vm.Siguiente);
        Assert.All(vm.Paradas, p => Assert.False(p.CompletadaSola));
        Assert.Equal(HayVanilla ? "vanilla" : "calamity", vm.GuiaId);
    }

    [Fact]
    public void ConCalamityDetectado_UsaLaGuiaCalamityConLaClasePicaro()
    {
        var vm = Vm(Personaje(100), null, calamity: true);
        Assert.Equal("calamity", vm.GuiaId);
        Assert.True(vm.EsCalamity);
        Assert.Contains(vm.Clases, c => c.Clase == ClaseGuia.Picaro);
    }

    [Fact]
    public void ClaseDetectada_SaleDelArmaRealDelInventario_YSePuedeCambiarYPersistir()
    {
        // ItemID 4 = espada larga de hierro (cuerpo a cuerpo); 39 = arco de madera (5 de daño, menos).
        var vm = Vm(Personaje(100, (4, 1), (39, 1)), null);
        Assert.Equal(ClaseGuia.CuerpoACuerpo, vm.ClaseDetectada);
        Assert.Equal(ClaseGuia.CuerpoACuerpo, vm.ClaseActual);
        Assert.False(vm.ClaseElegidaAMano);

        vm.ElegirClase(ClaseGuia.Magia);
        Assert.Equal(ClaseGuia.Magia, vm.ClaseActual);
        Assert.True(vm.ClaseElegidaAMano);

        var otra = Vm(Personaje(100, (4, 1)), null);
        Assert.Equal(ClaseGuia.Magia, otra.ClaseActual); // persistida en disco por personaje

        otra.ElegirClase(ClaseGuia.CuerpoACuerpo); // volver a la detectada = "automatica"
        Assert.False(otra.ClaseElegidaAMano);
    }

    [Fact]
    public void Evaluacion_ParadaHechaSolaPorBandera_YTenenciaReal()
    {
        var vm = Vm(Personaje(200, (560, 2), (84, 1)), Mundo(ojoDerrotado: true));
        var eye = vm.Paradas.First(p => p.Id == "eye");
        var slime = vm.Paradas.First(p => p.Id == "slime");
        Assert.True(eye.CompletadaSola);
        Assert.False(slime.Completada);
        var corona = slime.Necesitas.First(n => n.Ref == "Terraria/SlimeCrown");
        Assert.Equal(2, corona.Tengo);
        Assert.True(corona.LoTiene);
        Assert.Equal("Experto", vm.ModoTexto);

        var sinOjo = Vm(Personaje(200), Mundo(ojoDerrotado: false));
        Assert.False(sinOjo.Paradas.First(p => p.Id == "eye").Completada);
    }

    [Fact]
    public void TareaManual_SePersisteEnLaCarpetaDeLaApp_YSeRestaura()
    {
        var personaje = Personaje(100);
        var vm = Vm(personaje, null);
        var manual = vm.Paradas.SelectMany(p => p.Tareas).First(t => !t.Automatica && t.PuedeMarcar);
        vm.MarcarTarea(manual.Id, true);

        var archivo = Path.Combine(_carpeta, ArchivoProgresoGuia.NombreArchivo(vm.GuiaId, "PruebaVm"));
        Assert.True(File.Exists(archivo));
        Assert.True(ArchivoProgresoGuia.Cargar(_carpeta, vm.GuiaId, "PruebaVm").TareaMarcada(manual.Id));

        var recargada = Vm(personaje, null);
        Assert.True(recargada.Paradas.SelectMany(p => p.Tareas).First(t => t.Id == manual.Id).MarcadaAMano);

        recargada.MarcarTarea(manual.Id, false);
        Assert.False(ArchivoProgresoGuia.Cargar(_carpeta, vm.GuiaId, "PruebaVm").TareaMarcada(manual.Id));
    }

    [Fact]
    public void FichaVanilla_NoEnseñaObtencionesDeCalamity()
    {
        if (!HayVanilla) return; // sin guia vanilla incrustada no hay nada que filtrar
        var vm = Vm(Personaje(100), null);
        Assert.Equal("vanilla", vm.GuiaId);
        var mixto = GuiaV2Recursos.Referencias.Objetos.First(kv => kv.Value.Obtencion.Any(o => o.EsDeCalamity) && kv.Value.Obtencion.Any(o => !o.EsDeCalamity));
        Assert.Equal(mixto.Value.ObtencionPara("vanilla").Count(), vm.Obtenciones(mixto.Key).Count);
        Assert.True(vm.Obtenciones(mixto.Key).Count < mixto.Value.Obtencion.Count);

        var cal = Vm(Personaje(100), null, calamity: true);
        Assert.Equal(mixto.Value.Obtencion.Count, cal.Obtenciones(mixto.Key).Count);
    }

    [Fact]
    public void FichaDeObjeto_RecetaRealConIngredientesClicables_YCadenaDeVuelta()
    {
        var vm = Vm(Personaje(100, (560, 1)), null);
        vm.AbrirObjeto("Terraria/SlimeCrown");
        Assert.NotNull(vm.FichaObjeto);
        Assert.Contains(vm.FichaObjeto!.Obtenciones, o => o.Marcado.Contains("{o:Terraria/Gel}"));
        Assert.True(vm.FichaObjeto.PuedeLlevarALibreria);

        vm.AbrirObjeto("Terraria/Gel"); // ingrediente: se recorre la cadena
        Assert.True(vm.FichaObjeto!.PuedeVolver);
        vm.VolverFicha();
        Assert.Equal("Terraria/SlimeCrown", vm.FichaObjeto!.Objeto.Ref);
        vm.CerrarFicha();
        Assert.Null(vm.FichaObjeto);

        string? busqueda = null;
        vm.LibreriaSolicitada += b => busqueda = b;
        vm.AbrirObjeto("Terraria/SlimeCrown");
        vm.LlevarALibreria("Terraria/SlimeCrown");
        Assert.Equal("#560", busqueda);
    }

    [Fact]
    public void Buscador_EncuentraParadasYObjetos_YLlevaASuSeccion()
    {
        var vm = Vm(null, null);
        vm.TextoBusqueda = "slime";
        Assert.Equal(SeccionGuiaV2.Buscar, vm.Seccion);
        Assert.NotEmpty(vm.Resultados);
        Assert.Contains(vm.Resultados, r => r.Titulo.Contains("slime", StringComparison.OrdinalIgnoreCase));
        vm.TextoBusqueda = "zzzzqqq";
        Assert.True(vm.SinResultados);
    }

    [Fact]
    public async Task Ubicaciones_ExactaConFirmaDeTiles_AproximadaSinFirma()
    {
        var vm = Vm(Personaje(100), Mundo());
        await vm.MarcadorTarea;
        await vm.ResolverZonasAsync();
        var desierto = vm.Zonas.First(z => z.Id == "desierto");
        var superficie = vm.Zonas.First(z => z.Id == "superficie");
        Assert.True(desierto.TieneUbicacion);
        Assert.InRange(desierto.Ubicacion!.X, 300, 360);
        Assert.False(desierto.Ubicacion.Aproximada);
        Assert.True(superficie.Ubicacion!.Aproximada);
        Assert.True(vm.MarcadorVisible);
    }

    // F2b (02-oct-2026, una sola guia en toda la app): el chip y la banda de capa del mapa salen del
    // MISMO marcador de la guia v2 (antes, de la Zona del paso de la guia v1: podian decir
    // "Subterráneo" junto al marcador del Rey slime).
    [Fact]
    public async Task ChipYCapaDelMapa_SalenDelMarcadorDeLaSiguienteParada()
    {
        var mundo = Mundo();
        var vm = Vm(Personaje(100), mundo);
        await vm.MarcadorTarea;
        Assert.True(vm.MarcadorVisible);
        Assert.NotNull(vm.Siguiente);
        Assert.Equal(GuiaV2ViewModel.CapaDe(vm.MarcadorY, mundo.Header), vm.MarcadorCapa);
        Assert.Contains(vm.Siguiente!.TituloPlano, vm.ChipMapaTexto);
        Assert.Contains(vm.MarcadorTitulo, vm.ChipMapaTexto);
        Assert.Contains(LocalizationService.Instance["guia2_capa_" + vm.MarcadorCapa], vm.ChipMapaTexto);

        // Umbrales: los de las bandas del mapa (GroundLevel 100, RockLevel 200, infierno desde TilesHigh-192).
        Assert.Equal("superficie", GuiaV2ViewModel.CapaDe(50, mundo.Header));
        Assert.Equal("subterraneo", GuiaV2ViewModel.CapaDe(150, mundo.Header));
        Assert.Equal("infierno", GuiaV2ViewModel.CapaDe(299, mundo.Header));

        // Sin marcador no hay chip ni banda.
        var sinMundo = Vm(Personaje(100), null);
        await sinMundo.MarcadorTarea;
        Assert.False(sinMundo.MarcadorVisible);
        Assert.Null(sinMundo.MarcadorCapa);
        Assert.Equal("", sinMundo.ChipMapaTexto);
    }

    // F2b: la tarjeta "Te toca" de Inicio pregunta por un personaje que puede NO estar cargado. Su
    // respuesta tiene que ser la misma parada que enseña la pestaña Guia en cuanto se carga.
    [Fact]
    public void SiguienteParadaPara_OtroPersonaje_EsLaMismaQueLaPestañaAlCargarlo()
    {
        var mundo = Mundo(ojoDerrotado: true);
        var items = new GameItem[50];
        for (int i = 0; i < items.Length; i++) items[i] = GameItem.Empty;
        items[0] = new GameItem { Id = 4, Count = 1 };
        var plr = new PlrCharacter { Version = 279, Name = "OtroVm", PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true), HealthMax = 220 };
        var otro = new LoadedCharacter(Path.Combine(Path.GetTempPath(), "OtroVm-" + Guid.NewGuid().ToString("N")[..6] + ".plr"), null, "", plr, null,
            new Dictionary<string, GameItem[]> { ["inventory"] = items });

        LoadedCharacter? cargado = Personaje(100);
        var vm = new GuiaV2ViewModel(Servicio, () => cargado, () => mundo, () => false, () => null) { CarpetaProgreso = _carpeta };
        int evaluaciones = 0;
        vm.Evaluada += () => evaluaciones++;
        vm.Refresh();
        Assert.Equal(1, evaluaciones);
        string antes = vm.Siguiente!.Id;

        var propuesta = vm.SiguienteParadaPara(otro);
        Assert.NotNull(propuesta);
        Assert.Equal(antes, vm.Siguiente!.Id); // preguntar por otro personaje no toca la pestaña

        cargado = otro;
        vm.Refresh();
        Assert.Equal(vm.GuiaId, propuesta!.GuiaId);
        Assert.Equal(vm.Siguiente!.Numero, propuesta.Numero);
        Assert.Equal(vm.Siguiente.TituloPlano, propuesta.Titulo);
        Assert.Equal(vm.Siguiente.CapituloTitulo, propuesta.Capitulo);
        Assert.Equal(vm.ParadasTotal, propuesta.Total);

        // Y para el personaje ya cargado devuelve justo la de la pestaña.
        var mismo = vm.SiguienteParadaPara(otro);
        Assert.Equal(vm.Siguiente.TituloPlano, mismo!.Titulo);
    }
}
