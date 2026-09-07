using System.IO.Compression;

namespace Terrakeep.Core.Nbt;

// Un .tplr es gzip(NBT) sin ninguna cabecera propia (confirmado contra calamity-nbt.js real:
// parseTplr = ungzip + parseRoot, encodeTplr = encodeRoot + gzip). .NET trae gzip nativo
// (GZipStream), no hace falta portar pako.
public static class TplrFile
{
    public static (string RootName, NbtCompound Root) Read(byte[] fileBytes)
    {
        using var compressed = new MemoryStream(fileBytes);
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        gzip.CopyTo(raw);
        raw.Position = 0;
        return NbtSerializer.ReadRoot(raw);
    }

    public static byte[] Write(string rootName, NbtCompound root)
    {
        using var raw = new MemoryStream();
        NbtSerializer.WriteRoot(raw, rootName, root);
        raw.Position = 0;

        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            raw.CopyTo(gzip);
        }
        return compressed.ToArray();
    }
}
