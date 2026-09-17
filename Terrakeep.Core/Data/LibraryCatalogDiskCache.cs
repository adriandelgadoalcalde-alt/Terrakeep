namespace Terrakeep.Core.Data;

// Cache persistente en disco de "la Libreria de objetos (8000+)" (17-sep-2026, ver bitacora.md y
// el comentario real de CatalogBinaryCache para las medidas reales que justifican esto): un
// unico fichero binario que combina los dos catalogos caros de verdad (CalamityCatalog,
// VanillaItemCatalog) MAS el arbol ya construido (CategoryTreeNodeData, el mismo que
// LibraryCategoryTreeBuilder ya cacheaba en memoria durante la vida del proceso, ver su
// comentario real T-G) - asi el SEGUNDO arranque en adelante ni siquiera reconstruye el arbol
// (BuildItemTree), solo lo relee tal cual.
//
// Se guardan los tres juntos en el mismo fichero (en vez de tres cachés sueltas) porque
// dependen exactamente de los mismos ficheros fuente (calamity/catalog.json determina tanto
// CalamityCatalog como el agrupado por categoria del arbol; vanilla_library_tree.json y
// vanilla_library_labels_es.json solo se usan para el arbol) - una sola huella real cubre los
// tres a la vez, nunca hay riesgo de que uno quede "cacheado" y el otro no tras el mismo cambio
// de origen.
//
// CRITICO: esta clase NUNCA es la unica fuente de verdad. TryLoad devuelve null ante CUALQUIER
// fallo (fichero ausente, version de formato antigua, huella que no coincide, bytes corruptos a
// medias por un apagado brusco a mitad de escritura...) y quien llama cae siempre a la carga
// real por JSON (CharacterFileService, ver su comentario real) - esto es una optimizacion de
// arranque, jamas puede ser la razon de que la app deje de funcionar si la cache esta mal.
public static class LibraryCatalogDiskCache
{
    private const string Magic = "TKLC1"; // Terrakeep Library Cache
    public const int FormatVersion = 1;

    public sealed record LoadedCache(
        VanillaItemCatalog VanillaCatalog,
        CalamityCatalog CalamityCatalog,
        List<CategoryTreeNodeData> Tree);

    public static LoadedCache? TryLoad(string cachePath, IReadOnlyList<string> sourceFiles)
    {
        try
        {
            if (!File.Exists(cachePath)) return null;
            using var stream = File.OpenRead(cachePath);
            using var r = new BinaryReader(stream);

            if (r.ReadString() != Magic) return null;
            if (r.ReadInt32() != FormatVersion) return null;
            string storedFingerprint = r.ReadString();
            if (storedFingerprint != CatalogBinaryCache.ComputeFingerprint(sourceFiles)) return null;

            var vanilla = VanillaItemCatalog.ReadFrom(r);
            var calamity = CalamityCatalog.ReadFrom(r);
            var tree = ReadTree(r);
            return new LoadedCache(vanilla, calamity, tree);
        }
        catch
        {
            // Cache corrupta/formato inesperado/fallo real de E-S - nunca un fallo visible de la
            // app, solo "sin cache esta vez, se reconstruye desde el JSON real como siempre".
            return null;
        }
    }

    // Escritura atomica (mismo criterio real ya establecido por WorldViewStateService.Save y
    // el resto de servicios de %LOCALAPPDATA%\Terrakeep\: .tmp + File.Move con overwrite, nunca
    // se escribe directamente sobre el fichero real - un fallo a mitad de escritura deja como
    // mucho un .tmp huerfano, nunca una cache real corrupta a medias). Solo IOException/
    // UnauthorizedAccessException se tragan a proposito (disco lleno, sin permiso de escritura,
    // AppData bloqueado por antivirus...) - guardar la cache es un extra, nunca debe romper el
    // arranque que la esta generando.
    public static void Save(
        string cachePath, IReadOnlyList<string> sourceFiles,
        VanillaItemCatalog vanillaCatalog, CalamityCatalog calamityCatalog, IReadOnlyList<CategoryTreeNodeData> tree)
    {
        try
        {
            string? dir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmpPath = cachePath + ".tmp";
            using (var stream = File.Create(tmpPath))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Magic);
                w.Write(FormatVersion);
                w.Write(CatalogBinaryCache.ComputeFingerprint(sourceFiles));
                vanillaCatalog.WriteTo(w);
                calamityCatalog.WriteTo(w);
                WriteTree(w, tree);
            }
            File.Move(tmpPath, cachePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void WriteTree(BinaryWriter w, IReadOnlyList<CategoryTreeNodeData> nodes)
    {
        w.Write(nodes.Count);
        foreach (var node in nodes) WriteNode(w, node);
    }

    private static void WriteNode(BinaryWriter w, CategoryTreeNodeData node)
    {
        w.Write(node.Name);
        CatalogBinaryCache.WriteNullableString(w, node.NameEn);
        w.Write(node.FullPath);
        CatalogBinaryCache.WriteNullableString(w, node.IconPath);
        w.Write(node.ItemIdsOrdered.Count);
        foreach (int id in node.ItemIdsOrdered) w.Write(id);
        // ItemIdSet NUNCA se persiste aparte - su contenido es SIEMPRE exactamente el mismo
        // conjunto que ItemIdsOrdered (ver LibraryTreeBuilder: un HashSet construido sobre la
        // misma lista, nunca con contenido distinto), se reconstruye al leer con new HashSet<int>.
        w.Write(node.Children.Count);
        foreach (var child in node.Children) WriteNode(w, child);
    }

    private static List<CategoryTreeNodeData> ReadTree(BinaryReader r)
    {
        int n = r.ReadInt32();
        var list = new List<CategoryTreeNodeData>(n);
        for (int i = 0; i < n; i++) list.Add(ReadNode(r));
        return list;
    }

    private static CategoryTreeNodeData ReadNode(BinaryReader r)
    {
        string name = r.ReadString();
        string? nameEn = CatalogBinaryCache.ReadNullableString(r);
        string fullPath = r.ReadString();
        string? iconPath = CatalogBinaryCache.ReadNullableString(r);
        int idCount = r.ReadInt32();
        var ids = new List<int>(idCount);
        for (int i = 0; i < idCount; i++) ids.Add(r.ReadInt32());
        int childCount = r.ReadInt32();
        var children = new List<CategoryTreeNodeData>(childCount);
        for (int i = 0; i < childCount; i++) children.Add(ReadNode(r));
        return new CategoryTreeNodeData(name, fullPath, iconPath, ids, new HashSet<int>(ids), children, nameEn);
    }
}
