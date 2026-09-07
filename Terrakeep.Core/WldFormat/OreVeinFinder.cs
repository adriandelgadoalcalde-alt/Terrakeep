namespace Terrakeep.Core.WldFormat;

// Punto 4 (advisor Opus, "buscador o marcador de minerales" - ver ESPEC-ui-exploracion.md#11.4).
//
// Una veta = un grupo conexo (8 vecinos) de tiles del mismo tipo. Medido sobre un mundo real de
// 8400x2400 (ESPEC-ui-exploracion.md#6, con la cautela real de que esa medicion es de un port a
// Node, no del C# real): el cobre son 64289 tiles pero solo 4387 vetas (media de 14.7 tiles, la
// mayor de 102); el zafiro, 2850 tiles en 564 vetas. Agrupar divide la lista entre 15 y la vuelve
// util - "ir a la siguiente veta" es lo que un jugador quiere, no "ir al siguiente tile de
// cobre", que estaria pegado al anterior.
//
// Hallazgo real de esta sesion, verificado con dotnet test contra el mundo Grande real (no
// estaba en el espec del advisor, cuya seccion de huecos ya avisaba de que sus tiempos eran "de
// un port a JS, no del C# real"): agrupar en vetas TODOS los minerales presentes de un mundo
// llamando a Find una vez POR MINERAL (23 barridos completos de la rejilla) tarda varios
// SEGUNDOS en C# real - demasiado para un solo clic de usuario al abrir la categoria Minerales.
// La correccion real: Find ya acepta varios tipos a la vez y hace UN SOLO barrido; el problema
// no era el algoritmo, era llamarlo repetidas veces. CountVeinsByType comparte el mismo nucleo
// de flood-fill que Find pero descarta los detalles por veta (centroide) y solo se queda con el
// recuento por tipo - mas barato de memoria para el caso de uso real "cuantas vetas tiene cada
// mineral", que es lo que necesita el inventario de la categoria Minerales.
public readonly record struct OreVein(int CenterX, int CenterY, int TileCount, int Type);

public static class OreVeinFinder
{
    private static readonly (int Dx, int Dy)[] Neighbors8 =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
    ];

    // Flood-fill ITERATIVO con pila explicita (nunca recursivo: la veta mayor medida es de 134
    // tiles, pero un mundo trucado podria tener una losa entera del mismo tipo y reventar la
    // pila de llamadas). Un UNICO barrido x->y sirve para CUALQUIER numero de tipos a la vez -
    // pasar todos los minerales presentes juntos (en vez de llamar una vez por mineral) es lo
    // que hace barato el caso real "inventario de Minerales" (ver el comentario de arriba).
    //
    // C-04 (informe de pulido final, cierra E6/E7): generalizado con un delegado `typeAt` en vez
    // de leer `tile.Type` a pelo, para poder reutilizar EXACTAMENTE el mismo algoritmo agrupando
    // por Wall o por LiquidType - "reutilizar OreVeinFinder para tiles y liquidos", no inventar
    // un segundo flood-fill. `typeAt` devuelve null si la celda no casa (equivalente al viejo
    // `!tile.IsActive || !tileTypes.Contains(tile.Type)`).
    private static List<OreVein> FindAllCore(WldWorld world, Func<int, int, int?> typeAt, CancellationToken ct)
    {
        int w = world.Header.TilesWide, h = world.Header.TilesHigh;
        var visited = new bool[w * h];
        var allVeins = new List<OreVein>();
        var stack = new Stack<(int X, int Y)>();

        for (int x = 0; x < w; x++)
        {
            ct.ThrowIfCancellationRequested();
            for (int y = 0; y < h; y++)
            {
                int startIndex = x * h + y;
                if (visited[startIndex]) continue;
                int? maybeType = typeAt(x, y);
                if (maybeType is not int type) { visited[startIndex] = true; continue; }

                long sumX = 0, sumY = 0;
                int count = 0;
                stack.Push((x, y));
                visited[startIndex] = true;
                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    sumX += cx; sumY += cy; count++;
                    foreach (var (dx, dy) in Neighbors8)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int nIndex = nx * h + ny;
                        if (visited[nIndex]) continue;
                        // OJO: si el vecino no es del MISMO tipo que esta veta, NO se marca aqui
                        // como visitado - bug real atrapado por el propio test (Find_
                        // TiposDistintosNuncaSeFusionanAunqueEstenPegados): marcarlo visitado en
                        // este punto impedia que esa celda arrancara su PROPIA veta mas tarde
                        // cuando el bucle exterior llegara a ella (ej. hierro pegado a cobre - el
                        // hierro se descartaba aqui como vecino del cobre y nunca se procesaba).
                        // Solo se marca visitado cuando de verdad se añade a una veta (mas abajo)
                        // o cuando el bucle EXTERIOR lo descarta como punto de partida (si mismo,
                        // su tipo no cambia entre pasadas).
                        if (typeAt(nx, ny) != type) continue;
                        visited[nIndex] = true;
                        stack.Push((nx, ny));
                    }
                }

                allVeins.Add(new OreVein((int)Math.Round(sumX / (double)count), (int)Math.Round(sumY / (double)count), count, type));
            }
        }

        return allVeins;
    }

    private static int? TileTypeAt(WldWorld world, IReadOnlySet<int> tileTypes, int x, int y)
    {
        var tile = world.Tiles[x, y];
        return tile.IsActive && tileTypes.Contains(tile.Type) ? tile.Type : null;
    }

    private static int? WallTypeAt(WldWorld world, IReadOnlySet<int> wallIds, int x, int y)
    {
        var tile = world.Tiles[x, y];
        return tile.Wall != 0 && wallIds.Contains(tile.Wall) ? tile.Wall : null;
    }

    private static int? LiquidTypeAt(WldWorld world, IReadOnlySet<byte> liquidTypes, int x, int y)
    {
        var tile = world.Tiles[x, y];
        return tile.LiquidAmount > 0 && liquidTypes.Contains(tile.LiquidType) ? tile.LiquidType : null;
    }

    // Devuelve las "limit" primeras vetas ordenadas por tamaño descendente (las vetas gordas
    // primero, son las que interesan) y el total real por "out" - mismo patron "cuenta todo,
    // muestra N" de WorldSearch.Add. Uso real: el usuario elige uno o varios minerales concretos
    // para buscar/marcar (WorldSearchResults ya sabe navegar/marcar cualquier WorldSearchHit).
    public static IReadOnlyList<OreVein> Find(WldWorld world, IReadOnlySet<int> tileTypes, int limit, out int totalVeins, CancellationToken ct = default)
    {
        var allVeins = FindAllCore(world, (x, y) => TileTypeAt(world, tileTypes, x, y), ct);
        totalVeins = allVeins.Count;
        return allVeins.OrderByDescending(v => v.TileCount).Take(limit).ToList();
    }

    // C-04: mismo algoritmo que Find, agrupando por Wall en vez de por Type - generalizacion a
    // "Objetos > Paredes" del patron ya probado por Minerales.
    public static IReadOnlyList<OreVein> FindWalls(WldWorld world, IReadOnlySet<int> wallIds, int limit, out int totalVeins, CancellationToken ct = default)
    {
        var allVeins = FindAllCore(world, (x, y) => WallTypeAt(world, wallIds, x, y), ct);
        totalVeins = allVeins.Count;
        return allVeins.OrderByDescending(v => v.TileCount).Take(limit).ToList();
    }

    // C-04: mismo algoritmo, agrupando por LiquidType (solo tiles con LiquidAmount > 0) -
    // generalizacion a "Objetos > Liquidos".
    public static IReadOnlyList<OreVein> FindLiquids(WldWorld world, IReadOnlySet<byte> liquidTypes, int limit, out int totalVeins, CancellationToken ct = default)
    {
        var allVeins = FindAllCore(world, (x, y) => LiquidTypeAt(world, liquidTypes, x, y), ct);
        totalVeins = allVeins.Count;
        return allVeins.OrderByDescending(v => v.TileCount).Take(limit).ToList();
    }

    // Recuento de vetas POR TIPO, en un unico barrido combinado - uso real: el inventario de la
    // categoria Minerales necesita "tiles y vetas" para CADA mineral presente a la vez, y
    // llamar a Find una vez por mineral (aunque cada llamada sea barata sola) es varios segundos
    // en un mundo real con 20+ minerales presentes (medido, ver el comentario de la clase).
    public static IReadOnlyDictionary<int, int> CountVeinsByType(WldWorld world, IReadOnlySet<int> tileTypes, CancellationToken ct = default)
    {
        var allVeins = FindAllCore(world, (x, y) => TileTypeAt(world, tileTypes, x, y), ct);
        var result = new Dictionary<int, int>();
        foreach (var vein in allVeins) result[vein.Type] = result.GetValueOrDefault(vein.Type) + 1;
        return result;
    }
}
