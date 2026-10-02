namespace Terrakeep.Core.Guia.V2;

/// <summary>Acceso minimo a las casillas del mundo REAL cargado, para situar una zona. Cada app
/// lo implementa sobre lo que ya tiene: el escritorio sobre el lector de tiles de Exploracion
/// (.wld + .twld), el mod sobre Main.tile en vivo.</summary>
public interface IMundoGuia
{
    int Ancho { get; }
    int Alto { get; }
    /// <summary>Superficie / capa de roca (cabecera del .wld: worldSurface, rockLayer).</summary>
    int NivelSuperficie { get; }
    int NivelRoca { get; }
    int SpawnX { get; }
    int SpawnY { get; }
    int MazmorraX { get; }
    int MazmorraY { get; }
    /// <summary>TileID vanilla de la casilla, -1 si vacia o de mod.</summary>
    int TileVanilla(int x, int y);
    /// <summary>"Mod/NombreInterno" del tile de mod de la casilla, o null.</summary>
    string? TileMod(int x, int y);
    /// <summary>Puntos guardados por el juego/mod (Calamity: laboratorios). Null si no hay.</summary>
    (int X, int Y)? Punto(string clave);
}

/// <summary>Resultado de situar una ubicacion: punto en casillas y si es exacto o aproximado.</summary>
public sealed record UbicacionResuelta(int X, int Y, bool Aproximada, string Origen, int Casillas = 0);

public static class GuiaV2Ubicaciones
{
    /// <summary>Situa una zona en el mundo real: punto guardado si lo hay; si no, el centro del
    /// grupo de casillas con su firma mas cercano al spawn (muestreo cada <paramref name="paso"/>
    /// casillas, suficiente para un marcador de mapa); si no hay firma, el centro aproximado de
    /// su capa. Null = la zona no existe en este mundo (p.ej. mundo sin Calamity generado).</summary>
    public static UbicacionResuelta? Resolver(Zona zona, IMundoGuia mundo, int paso = 4)
    {
        if (zona.Punto != null)
        {
            var p = PuntoConocido(zona.Punto, mundo);
            if (p != null) return new UbicacionResuelta(p.Value.X, p.Value.Y, false, "punto:" + zona.Punto);
        }

        var tiles = zona.Firma.Tiles.ToHashSet();
        var tilesMod = zona.Firma.TilesMod.ToHashSet();
        if (tiles.Count > 0 || tilesMod.Count > 0)
        {
            // Rejilla gruesa de celdas de 64x64: se cuenta la firma por celda y se elige la celda
            // con mas casillas (desempate: la mas cercana al spawn). Barato y estable.
            const int celda = 64;
            int cw = (mundo.Ancho + celda - 1) / celda, ch = (mundo.Alto + celda - 1) / celda;
            var cuenta = new int[cw, ch];
            var sumX = new long[cw, ch];
            var sumY = new long[cw, ch];
            int total = 0;
            for (int x = 0; x < mundo.Ancho; x += paso)
                for (int y = 0; y < mundo.Alto; y += paso)
                {
                    bool es = (tiles.Count > 0 && tiles.Contains(mundo.TileVanilla(x, y))) ||
                              (tilesMod.Count > 0 && mundo.TileMod(x, y) is string m && tilesMod.Contains(m));
                    if (!es) continue;
                    cuenta[x / celda, y / celda]++;
                    sumX[x / celda, y / celda] += x;
                    sumY[x / celda, y / celda] += y;
                    total++;
                }
            int minimoMuestras = Math.Max(1, zona.Firma.Minimo / (paso * paso));
            if (total >= minimoMuestras)
            {
                int mejorX = -1, mejorY = -1;
                long mejorDist = long.MaxValue;
                int mejorCuenta = 0;
                for (int i = 0; i < cw; i++)
                    for (int j = 0; j < ch; j++)
                    {
                        if (cuenta[i, j] == 0) continue;
                        long dx = i * celda - mundo.SpawnX, dy = j * celda - mundo.SpawnY;
                        long dist = dx * dx + dy * dy;
                        if (cuenta[i, j] > mejorCuenta || (cuenta[i, j] == mejorCuenta && dist < mejorDist))
                        {
                            mejorCuenta = cuenta[i, j]; mejorDist = dist; mejorX = i; mejorY = j;
                        }
                    }
                return new UbicacionResuelta((int)(sumX[mejorX, mejorY] / mejorCuenta), (int)(sumY[mejorX, mejorY] / mejorCuenta),
                    false, "firma", total * paso * paso);
            }
            return null;
        }

        // Sin firma: solo la capa, centrada horizontalmente en el spawn. Honesto: Aproximada.
        int yCapa = zona.Capa switch
        {
            "superficie" => mundo.NivelSuperficie - 20,
            "subterraneo" => (mundo.NivelSuperficie + mundo.NivelRoca) / 2,
            "cavernas" => (mundo.NivelRoca + (mundo.Alto - 200)) / 2,
            "infierno" => mundo.Alto - 100,
            "espacio" => Math.Max(10, mundo.NivelSuperficie / 4),
            _ => -1,
        };
        return yCapa < 0 ? null : new UbicacionResuelta(mundo.SpawnX, yCapa, true, "capa:" + zona.Capa);
    }

    public static (int X, int Y)? PuntoConocido(string clave, IMundoGuia mundo) => clave switch
    {
        "spawn" => (mundo.SpawnX, mundo.SpawnY),
        "mazmorra" => (mundo.MazmorraX, mundo.MazmorraY),
        _ => mundo.Punto(clave),
    };

    /// <summary>Primera ubicacion de la parada que se puede situar en este mundo (respetando
    /// siMundo corrupcion/carmesi). Las de tipo npc/jefe las resuelve la app (posicion de un NPC
    /// del pueblo en el .wld o en vivo) y aqui se saltan.</summary>
    public static UbicacionResuelta? ResolverParada(Parada parada, GuiaV2Doc doc, IMundoGuia mundo, bool? mundoCarmesi, int paso = 4)
    {
        foreach (var u in parada.Ubicaciones)
        {
            if (u.SiMundo == "carmesi" && mundoCarmesi == false) continue;
            if (u.SiMundo == "corrupcion" && mundoCarmesi == true) continue;
            if (u.Tipo == "punto")
            {
                var p = PuntoConocido(u.Id, mundo);
                if (p != null) return new UbicacionResuelta(p.Value.X, p.Value.Y, false, "punto:" + u.Id);
                continue;
            }
            if (u.Tipo != "zona") continue;
            var zona = doc.Zonas.FirstOrDefault(z => z.Id == u.Id);
            if (zona == null) continue;
            var r = Resolver(zona, mundo, paso);
            if (r != null) return r;
        }
        return null;
    }
}
