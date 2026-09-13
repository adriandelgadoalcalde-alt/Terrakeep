using System.IO.Compression;
using Terrakeep.Core.Calamity;

namespace Terrakeep.Core.Model;

// Codigos de build compartibles (encargo del usuario, 13-sep-2026, cuarto de la lista
// confirmada: "exportar una build a un codigo de texto corto/compartible y poder importarlo en
// otra instalacion"). Alcance real, honesto: las 10 ranuras de armadura/accesorios
// (Cabeza/Cuerpo/Piernas/Accesorio 1-7, el "build" en el sentido de Terraria - lo que se lleva
// puesto para combatir) MAS sus 10 tintes - se deja fuera a proposito Mascota/Mascota de luz/
// Vagoneta/Montura/Gancho (utilidad/cosmetica, no lo que la comunidad entiende por "build" al
// compartir codigos), documentado aqui en vez de fingir cubrir "todo el equipo".
//
// Formato real, version 1 (el byte va DENTRO del payload comprimido, no en la cabecera de
// texto - un cambio de formato futuro no rompe la deteccion de version real):
//   byte version
//   10x { int32 itemId, int32 prefixCode }   - armadura/accesorios, mismo orden que
//                                               PlrLoadout.Items (loadout*Items[0..9])
//   10x { int32 dyeItemId }                  - mismo orden, loadout*Dyes[0..9]
// prefixCode: 0 = sin prefijo, 1-255 = ItemPrefix.Vanilla, >= CalamityIds.PrefixIdBase (10000) =
// ItemPrefix.CalamitySynthetic - un unico entero basta porque los dos rangos reales nunca se
// solapan (ver CalamityIds.PrefixIdBase).
//
// Comprimido con Deflate antes de Base64 - la mayoria de personajes reales no llenan los 20
// huecos (bastantes quedan a 0, series largas de ceros), y Deflate comprime eso muy bien sin
// añadir ninguna dependencia nueva (System.IO.Compression, igual que .tkbak - ver
// BackupHistoryService). Un byte de checksum ANTES del bloque comprimido (suma simple mod 256,
// no criptografico - solo para detectar un codigo pegado a medias o editado a mano antes de
// intentar aplicarlo) - un checksum que no cuadra es un "codigo invalido" claro, nunca una
// media build aplicada en silencio.
public readonly record struct BuildCodeSlot(int ItemId, ItemPrefix Prefix)
{
    public static readonly BuildCodeSlot Empty = new(0, ItemPrefix.None);
}

public enum BuildCodeError
{
    // Cabecera "TKBUILD1:" ausente o el resto no es Base64-URL valido - lo mas probable es que
    // sea otra cosa completamente (una direccion, un id de objeto) pegada por error.
    InvalidFormat,
    // Formato Base64/Deflate correcto pero el checksum no cuadra - un caracter cambiado a mano,
    // o un pegado a medias (cortado).
    Corrupt,
    // Decodifica y el checksum cuadra, pero el byte de version es de un formato mas nuevo que
    // esta version de la app no sabe leer todavia.
    UnsupportedVersion,
}

public static class BuildCode
{
    public const int SlotCount = 10;
    private const byte FormatVersion = 1;
    // Prefijo de texto real (no solo cosmetica): permite reconocer de un vistazo que un texto
    // pegado en cualquier sitio ES un codigo de build de Terrakeep, y distinguirlo de un id de
    // objeto o un codigo de OTRA herramienta (Terrasavr-Calamity-Beta, hermana de este proyecto,
    // tiene sus propios export en JSON, formato completamente distinto).
    private const string Header = "TKBUILD1:";

    public static string Encode(IReadOnlyList<BuildCodeSlot> items, IReadOnlyList<int> dyeItemIds)
    {
        if (items.Count != SlotCount)
            throw new ArgumentException($"Un codigo de build siempre lleva {SlotCount} objetos (Cabeza/Cuerpo/Piernas/Accesorio 1-7) - recibidos {items.Count}.", nameof(items));
        if (dyeItemIds.Count != SlotCount)
            throw new ArgumentException($"Un codigo de build siempre lleva {SlotCount} tintes - recibidos {dyeItemIds.Count}.", nameof(dyeItemIds));

        using var raw = new MemoryStream();
        using (var w = new BinaryWriter(raw, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            w.Write(FormatVersion);
            foreach (var slot in items)
            {
                w.Write(slot.ItemId);
                w.Write(PrefixToCode(slot.Prefix));
            }
            foreach (int dyeId in dyeItemIds) w.Write(dyeId);
        }
        byte[] rawBytes = raw.ToArray();
        byte checksum = Checksum(rawBytes);

        using var compressed = new MemoryStream();
        compressed.WriteByte(checksum);
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(rawBytes, 0, rawBytes.Length);

        // Base64 "URL-safe" (- _ en vez de + /, sin relleno =) - un codigo pensado para pegarse
        // en un chat/foro/URL sin que nadie tenga que escapar nada.
        string b64 = Convert.ToBase64String(compressed.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return Header + b64;
    }

    public static bool TryDecode(string code, out IReadOnlyList<BuildCodeSlot> items, out IReadOnlyList<int> dyeItemIds, out BuildCodeError? error)
    {
        items = [];
        dyeItemIds = [];
        error = null;

        string trimmed = code.Trim();
        if (!trimmed.StartsWith(Header, StringComparison.Ordinal)) { error = BuildCodeError.InvalidFormat; return false; }

        string b64 = trimmed[Header.Length..].Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2: b64 += "=="; break;
            case 3: b64 += "="; break;
            case 1: error = BuildCodeError.InvalidFormat; return false; // longitud imposible para Base64 real
        }

        byte[] payload;
        try { payload = Convert.FromBase64String(b64); }
        catch (FormatException) { error = BuildCodeError.InvalidFormat; return false; }
        if (payload.Length < 2) { error = BuildCodeError.InvalidFormat; return false; }

        byte expectedChecksum = payload[0];
        byte[] rawBytes;
        try
        {
            using var input = new MemoryStream(payload, 1, payload.Length - 1);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            rawBytes = output.ToArray();
        }
        catch (InvalidDataException) { error = BuildCodeError.Corrupt; return false; }

        if (Checksum(rawBytes) != expectedChecksum) { error = BuildCodeError.Corrupt; return false; }

        int expectedLength = 1 + SlotCount * 8 + SlotCount * 4;
        if (rawBytes.Length != expectedLength) { error = BuildCodeError.Corrupt; return false; }

        using var r = new BinaryReader(new MemoryStream(rawBytes));
        byte version = r.ReadByte();
        if (version != FormatVersion) { error = BuildCodeError.UnsupportedVersion; return false; }

        var itemsList = new List<BuildCodeSlot>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            int id = r.ReadInt32();
            int prefixCode = r.ReadInt32();
            itemsList.Add(new BuildCodeSlot(id, CodeToPrefix(prefixCode)));
        }
        var dyes = new List<int>(SlotCount);
        for (int i = 0; i < SlotCount; i++) dyes.Add(r.ReadInt32());

        items = itemsList;
        dyeItemIds = dyes;
        return true;
    }

    private static int PrefixToCode(ItemPrefix p) => p.IsNone ? 0 : p.IsCalamity ? p.SyntheticId : p.VanillaId;

    private static ItemPrefix CodeToPrefix(int code) =>
        code == 0 ? ItemPrefix.None
        : code >= CalamityIds.PrefixIdBase ? ItemPrefix.CalamitySynthetic(code)
        : ItemPrefix.Vanilla((byte)code);

    private static byte Checksum(byte[] data)
    {
        int sum = 0;
        foreach (byte b in data) sum = (sum + b) & 0xFF;
        return (byte)sum;
    }
}
