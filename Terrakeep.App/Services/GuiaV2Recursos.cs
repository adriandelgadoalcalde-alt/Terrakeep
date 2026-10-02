using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Guia.V2;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.Services;

// Guia v2 (F2, 02-oct-2026, encargo "guia grande vanilla + Calamity"): la parte de escritorio de
// la guia nueva que NO es de pantalla - carga de los documentos incrustados en Terrakeep.Core
// (GuiaV2Cargador, una sola vez por proceso), nombres en el idioma activo, sprites (los MISMOS
// resolutores que ya usa el resto de la app: VanillaIconResolver, catalogo de Calamity,
// BossIconResolver/CalamityBossIconResolver/NpcIconResolver) y el resolutor de referencias
// "Mod/NombreInterno" -> id de esta app (ver docs/guia-v2-diseno.md §10, F2 punto 3).
public sealed class GuiaV2Recursos
{
    private static readonly Lazy<GuiaV2Referencias> _referencias = new(GuiaV2Cargador.CargarReferenciasIncrustadas);
    private static readonly ConcurrentDictionary<string, GuiaV2Doc> _docs = new();

    private readonly CharacterFileService _servicio;

    public GuiaV2Recursos(CharacterFileService servicio)
    {
        _servicio = servicio;
        Resolutor = new ResolutorRefsGuia(Referencias, IdCalamity);
    }

    public static GuiaV2Referencias Referencias => _referencias.Value;

    /// <summary>Guias incrustadas en esta build ("calamity" y, cuando F1 la entregue, "vanilla").</summary>
    public static IReadOnlyList<string> Disponibles => GuiaV2Cargador.GuiasDisponibles();

    public static GuiaV2Doc? Documento(string id)
    {
        if (!Disponibles.Contains(id)) return null;
        return _docs.GetOrAdd(id, GuiaV2Cargador.CargarGuiaIncrustada);
    }

    public IResolutorRefsGuia Resolutor { get; }

    private static string Idioma => LocalizationService.Instance.Language;

    // ---- Objetos ---------------------------------------------------------------------------------

    /// <summary>Id de ESTA app para una referencia de objeto (vanilla: ItemID real; Calamity: id
    /// sintetico del catalogo). Null = esta build no la conoce (nunca inventado).</summary>
    public int? IdObjeto(string referencia) => Resolutor.Objeto(referencia);

    private int? IdCalamity(string referencia)
    {
        var (mod, interno) = Partir(referencia);
        return mod == null ? null : _servicio.CalamityCatalog.ByModAndInternal(mod, interno)?.SyntheticId;
    }

    public string NombreObjeto(string referencia)
    {
        if (Referencias.Objetos.TryGetValue(referencia, out var o))
            return Idioma == LocalizationService.English && !string.IsNullOrEmpty(o.En) ? o.En : o.Es;
        // Fuera de la tabla (no deberia pasar: el gate de contenido lo impide): el catalogo de la
        // app o, como ultimo recurso honesto, el nombre interno tal cual.
        var (mod, interno) = Partir(referencia);
        if (mod != null && mod != "Terraria")
            return _servicio.CalamityCatalog.ByModAndInternal(mod, interno)?.DisplayName ?? interno;
        return interno;
    }

    public string? IconoObjeto(string referencia)
    {
        if (referencia.StartsWith("Terraria/", StringComparison.Ordinal))
            return Referencias.Objetos.TryGetValue(referencia, out var o) && o.Id is int id ? VanillaIconResolver.GetIconPath(id) : null;
        var (mod, interno) = Partir(referencia);
        if (mod == null) return null;
        return _servicio.CalamityCatalog.ByModAndInternal(mod, interno)?.Icon is { } icono
            ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + icono
            : null;
    }

    /// <summary>Lo que se escribe en el buscador de la Libreria para encontrar ESE objeto exacto
    /// ("#id" de la gramatica real de la Libreria, LibrarySearchGrammar).</summary>
    public string? BusquedaLibreria(string referencia) => IdObjeto(referencia) is int id && id > 0 ? "#" + id : null;

    // ---- NPC y jefes -----------------------------------------------------------------------------

    public string NombreNpc(string referencia)
    {
        if (Referencias.Npcs.TryGetValue(referencia, out var n))
            return Idioma == LocalizationService.English && !string.IsNullOrEmpty(n.En) ? n.En : n.Es;
        return Partir(referencia).Interno;
    }

    public static string? IconoNpc(string referencia)
    {
        if (referencia.StartsWith("Terraria/", StringComparison.Ordinal))
        {
            if (!Referencias.Npcs.TryGetValue(referencia, out var n) || n.Id is not int id) return null;
            return BossIconResolver.GetIconPath(id) ?? NpcIconResolver.GetIconPath(id);
        }
        return CalamityBossIconResolver.GetIconPath(referencia);
    }

    // ---- Estaciones y grupos de receta ----------------------------------------------------------

    public static string NombreEstacion(string clave) =>
        Referencias.Estaciones.TryGetValue(clave, out var e) ? Elegir(e) : Partir(clave).Interno;

    public static string NombreGrupo(string clave) =>
        Referencias.Grupos.TryGetValue(clave, out var g) ? Elegir(g) : clave;

    private static string Elegir(RefNombre n) =>
        Idioma == LocalizationService.English && !string.IsNullOrEmpty(n.En) ? n.En : (string.IsNullOrEmpty(n.Es) ? n.En : n.Es);

    // ---- Utilidades --------------------------------------------------------------------------------

    public static (string? Mod, string Interno) Partir(string referencia)
    {
        int barra = referencia.IndexOf('/');
        return barra <= 0 ? (null, referencia) : (referencia[..barra], referencia[(barra + 1)..]);
    }

    private static readonly ConcurrentDictionary<string, BitmapImage?> _imagenes = new();

    /// <summary>Imagen congelada y cacheada para pintar sprites dentro de un texto (TextoGuia):
    /// cientos de sprites repetidos en la ruta no deben decodificarse una y otra vez.</summary>
    public static BitmapImage? Imagen(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return null;
        return _imagenes.GetOrAdd(ruta, r =>
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(r, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception)
            {
                return null; // sprite ausente o ilegible: el texto sigue sin icono, nunca revienta
            }
        });
    }
}

/// <summary>El mundo cargado en Exploracion visto como <see cref="IMundoGuia"/> (docs/guia-v2-diseno.md
/// §7): tiles vanilla del .wld y, para las firmas de Calamity, los tiles de mod del .twld hermano
/// muestreados en la MISMA rejilla que usa GuiaV2Ubicaciones.Resolver (cada 4 casillas).</summary>
public sealed class MundoGuiaEscritorio(WldWorld mundo, IReadOnlyDictionary<(int X, int Y), string>? tilesMod,
    IReadOnlyDictionary<string, (int X, int Y)>? puntos) : IMundoGuia
{
    public const int PasoMuestreo = 4;

    public int Ancho => mundo.Header.TilesWide;
    public int Alto => mundo.Header.TilesHigh;
    public int NivelSuperficie => (int)mundo.Header.GroundLevel;
    public int NivelRoca => (int)mundo.Header.RockLevel;
    public int SpawnX => mundo.Header.SpawnX;
    public int SpawnY => mundo.Header.SpawnY;
    public int MazmorraX => mundo.Header.DungeonX;
    public int MazmorraY => mundo.Header.DungeonY;

    public int TileVanilla(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Ancho || y >= Alto) return -1;
        var t = mundo.Tiles[x, y];
        return t.IsActive ? t.Type : -1;
    }

    public string? TileMod(int x, int y) => tilesMod != null && tilesMod.TryGetValue((x, y), out var m) ? m : null;

    public (int X, int Y)? Punto(string clave) => puntos != null && puntos.TryGetValue(clave, out var p) ? p : null;

    /// <summary>Lee del .twld los tiles de mod en la rejilla de muestreo (solo casillas que el .wld
    /// guarda como aire: tModLoader escribe los tiles de mod asi, ver ExplorationViewModel.
    /// ResolveModdedChestTileNames). Nunca lanza: sin .twld o ilegible, diccionario vacio.</summary>
    public static IReadOnlyDictionary<(int X, int Y), string> LeerTilesModMuestreados(WldWorld mundo, string? rutaWld)
    {
        var vacio = new Dictionary<(int X, int Y), string>();
        if (string.IsNullOrEmpty(rutaWld)) return vacio;
        string rutaTwld = Path.ChangeExtension(rutaWld, ".twld");
        if (!File.Exists(rutaTwld)) return vacio;
        try
        {
            int w = mundo.Header.TilesWide, h = mundo.Header.TilesHigh;
            var posiciones = new HashSet<(int X, int Y)>();
            for (int x = 0; x < w; x += PasoMuestreo)
                for (int y = 0; y < h; y += PasoMuestreo)
                    if (!mundo.Tiles[x, y].IsActive) posiciones.Add((x, y));
            if (posiciones.Count == 0) return vacio;

            var contenido = TwldReader.Read(File.ReadAllBytes(rutaTwld), w, h, posiciones);
            var resultado = new Dictionary<(int X, int Y), string>();
            foreach (var (pos, tipo) in contenido.ModTileTypeAt)
                if (contenido.TileEntries.TryGetValue(tipo, out var entrada))
                    resultado[pos] = entrada.Mod + "/" + entrada.Name;
            return resultado;
        }
        catch (Exception)
        {
            return vacio;
        }
    }

    /// <summary>Estado de Calamity del .twld (claves de MiscWorldStateSystem y puntos de los
    /// laboratorios). Null si no hay .twld; nunca lanza.</summary>
    public static CalamityEstadoGuardado.EstadoMundo? LeerEstadoCalamity(string? rutaWld)
    {
        if (string.IsNullOrEmpty(rutaWld)) return null;
        string rutaTwld = Path.ChangeExtension(rutaWld, ".twld");
        if (!File.Exists(rutaTwld)) return null;
        try { return CalamityEstadoGuardado.LeerMundo(File.ReadAllBytes(rutaTwld)); }
        catch (Exception) { return null; }
    }
}
