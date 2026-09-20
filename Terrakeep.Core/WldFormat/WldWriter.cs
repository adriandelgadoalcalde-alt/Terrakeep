using System.Collections;

namespace Terrakeep.Core.WldFormat;

// Primer escritor real de .wld - hasta el 5-sep-2026 el formato SOLO se leia (WldReader);
// "Solo lectura" es un badge real en toda la UI de Exploracion, con un tooltip explicito ("no
// se puede editar ni guardar desde aqui"). Empezo como parcheador MINIMO de campos de ancho fijo
// (GameMode/Spawn/Tiempo-luna/banderas de progreso, pedidos explicitos del usuario 5..14-sep) y
// crecio a un escritor de secciones de ancho VARIABLE (cofres/letreros, T1 del editor, 15-sep) -
// todo eso sigue exigiendo un archivo REAL ya existente que solo se edita en un tramo acotado.
//
// KeepQA (20-sep-2026, punto 6 del catalogo de funciones - "Bancos de datos extremos
// compartidos"): WriteWorld de aqui abajo es la primera pieza que construye un `.wld` COMPLETO
// desde CERO (sin partir de ningun archivo real), pensada para que el arnes de pruebas de KeepQA
// pueda generar mundos "extremos" (tiles/NPCs/cofres en cantidad o con coordenadas fuera de lo
// normal, banderas de progreso contradictorias) con las que hacer fuzz-testing del propio
// WldReader. Alcance deliberadamente distinto del editor de mundos de Terrakeep (la app de
// escritorio NUNCA sera un editor de mundos completo, ver ESPEC-auditoria-exploracion-tedit.md/
// ESPEC-buscador-mundo-tedit.md - eso sigue intacto): esto es infraestructura de USO INTERNO del
// arnes de pruebas, nunca expuesta como funcion de la app ni del usuario.
public static class WldWriter
{
    // WriteWorld exige version >= 210 (Journey's End, el mismo suelo real que ya exige el
    // Bestiario - WldHeader.BestiarySectionOffset) para poder escribir SIEMPRE el formato de
    // cabecera "moderno" (GameMode Int32, WorldGUID de 16 bytes, DownedSlimeKingBoss...) sin
    // tener que reimplementar tambien las 3 variantes MAS ANTIGUAS que ReadHeader sabe leer
    // (bool de GameMode pre-209, ausencia de WorldGUID pre-181, ausencia de CreationTime
    // pre-141...) - esas variantes viejas no aportan nada nuevo a un mundo generado desde cero
    // para fuzzing (el arnes elige la version que quiere probar, no la hereda de un archivo real)
    // y AMPLIAN mucho la superficie a mantener en sincronia con ReadHeader. Restriccion elegida y
    // documentada, no un limite real del formato - se podria levantar mas adelante si hiciera
    // falta generar un mundo mas antiguo.
    private const int PointerCount = 10;

    // Construye un `.wld` completo y valido (releible por WldReader.Read de principio a fin) a
    // partir de un WldWorld en memoria - la inversa real de WldReader.Read para las secciones que
    // el propio lector entiende (cabecera, tiles, cofres, letreros, NPCs, tile entities,
    // bestiario). Deliberadamente NO escribe las secciones reales de TEdit que WldReader NUNCA
    // lee (PressurePlate/TownManager/CreativePowers/Footer con firma de integridad) - se dejan de
    // longitud CERO (sus punteros de cabecera apuntan todos al mismo offset, justo despues del
    // Bestiario) porque no aportan nada a un round-trip contra ESTE lector y anadirlas sin poder
    // verificarlas contra el juego real (no instalado en este arnes) seria simular una fidelidad
    // que no se puede demostrar. Consecuencia honesta: el archivo resultante sirve para fuzzing
    // real de WldReader/Terrakeep.Core, pero NO esta garantizado que el juego real o TEdit lo
    // abran sin protestar - ver bitacora.md.
    public static byte[] WriteWorld(WldWorld world)
    {
        var header = world.Header;
        uint version = header.Version;
        if (version < 210)
            throw new NotSupportedException($"WldWriter.WriteWorld solo genera mundos de formato >=210 (Journey's End) - se pidio la version {version}. Ver el comentario de PointerCount.");

        int wide = world.Tiles.GetLength(0);
        int high = world.Tiles.GetLength(1);
        if (wide != header.TilesWide || high != header.TilesHigh)
            throw new ArgumentException($"Header.TilesWide/TilesHigh ({header.TilesWide}x{header.TilesHigh}) no coincide con las dimensiones reales de Tiles ({wide}x{high}) - sincronizalos antes de llamar a WriteWorld.");

        // Paso 1: medir la longitud real de la cabecera completa - depende solo de la CANTIDAD de
        // punteros (cada uno ocupa 4 bytes siempre) y del contenido real de Title/Seed/
        // TileFrameImportant, nunca de los VALORES concretos de los punteros - mismo truco de dos
        // pasadas que ya usa WldWriterChestSignTests.WriteHeaderTo (sintetico, de pruebas) y que
        // aqui se hace real y general.
        // leaveOpen: true en los dos BinaryWriter de aqui abajo - BinaryWriter.Dispose() cierra
        // TAMBIEN el MemoryStream subyacente por defecto (a diferencia de StreamWriter, que la
        // mayoria espera igual), asi que sin este flag measureMs.Length/finalMs.ToArray() de mas
        // abajo lanzarian ObjectDisposedException nada mas salir del using. Bug real encontrado
        // al ejecutar WldWriterWriteWorldTests por primera vez (20-sep-2026) - ver bitacora.md.
        var measureMs = new MemoryStream();
        using (var measureWriter = new BinaryWriter(measureMs, System.Text.Encoding.UTF8, leaveOpen: true))
            WriteFullHeader(measureWriter, header, version, new int[PointerCount], out _);
        int headerLength = (int)measureMs.Length;

        byte[] tilesBytes = SerializeTiles(world.Tiles, version, header.TileFrameImportant);
        byte[] chestsBytes = SerializeChests(world.Chests, version);
        byte[] signsBytes = SerializeSignsSection(world.Signs);
        byte[] npcsBytes = SerializeNpcsSection(world.Npcs, world.ShimmeredNpcTypes, version);
        byte[] tileEntitiesBytes = SerializeTileEntitiesSection(world.TileEntities, version);
        byte[] bestiaryBytes = SerializeBestiarySection(world.Bestiary, version);

        int tilesOffset = headerLength;
        int chestsOffset = tilesOffset + tilesBytes.Length;
        int signsOffset = chestsOffset + chestsBytes.Length;
        int npcsOffset = signsOffset + signsBytes.Length;
        int tileEntitiesOffset = npcsOffset + npcsBytes.Length;
        // PressurePlate/TownManager: ver el comentario de cabecera de WriteWorld - longitud CERO
        // a proposito, los tres punteros caen en el mismo offset (justo tras TileEntities).
        int pressurePlateOffset = tileEntitiesOffset + tileEntitiesBytes.Length;
        int townManagerOffset = pressurePlateOffset;
        int bestiaryOffset = townManagerOffset;
        int afterBestiaryOffset = bestiaryOffset + bestiaryBytes.Length;

        var pointers = new[]
        {
            headerLength, tilesOffset, chestsOffset, signsOffset, npcsOffset,
            tileEntitiesOffset, pressurePlateOffset, townManagerOffset, bestiaryOffset, afterBestiaryOffset,
        };

        var finalMs = new MemoryStream();
        using (var finalWriter = new BinaryWriter(finalMs, System.Text.Encoding.UTF8, leaveOpen: true))
            WriteFullHeader(finalWriter, header, version, pointers, out _);
        finalMs.Write(tilesBytes);
        finalMs.Write(chestsBytes);
        finalMs.Write(signsBytes);
        finalMs.Write(npcsBytes);
        finalMs.Write(tileEntitiesBytes);
        finalMs.Write(bestiaryBytes);
        return finalMs.ToArray();
    }

    // Escribe la cabecera ENTERA (tabla de punteros + tileFrameImportant + todo el tramo
    // title..hardMode) - inversa campo a campo de WldReader.ReadHeader(BinaryReader) para
    // version >= 210 (WriteWorld ya lo garantiza antes de llamar aqui). sectionHeaderEndOffset
    // (posicion justo tras tileFrameImportant, antes de Title) es el "Pointers[0]" real de TEdit
    // (World.FileV2.cs, SaveSectionHeader) - WldReader/WldHeader de este proyecto NUNCA lo usan
    // (ninguna propiedad de WldHeader expone Pointers[0]), se calcula igualmente por fidelidad de
    // formato y honestidad ("lo que se escribe en el archivo tiene que ser correcto, aunque nada
    // de este proyecto lo lea despues").
    private static void WriteFullHeader(BinaryWriter writer, WldHeader header, uint version, int[] pointers, out int sectionHeaderEndOffset)
    {
        writer.Write(version);
        writer.Write("relogic".ToCharArray());
        writer.Write((byte)2); // fileType = mundo
        writer.Write((uint)1); // FileRevision - sin uso real fuera de este archivo
        writer.Write((long)0); // banderas de favorito

        writer.Write((short)pointers.Length);
        foreach (int p in pointers) writer.Write(p);

        // Inversa exacta de WldReader.ReadBitArray - mismo criterio ya establecido en el
        // proyecto (ReadBitArray/ReadChests/ReadRawSigns) de reutilizar SIEMPRE la misma pieza
        // real en vez de reimplementar el empaquetado de bits a mano en un segundo sitio.
        var bits = new BitArray(header.TileFrameImportant);
        int byteCount = (bits.Length + 7) / 8;
        var raw = new byte[byteCount];
        bits.CopyTo(raw, 0);
        writer.Write((short)bits.Length);
        writer.Write(raw);

        sectionHeaderEndOffset = (int)writer.BaseStream.Position;

        writer.Write(header.Title);
        writer.Write(header.Seed); // version != 179 siempre (WriteWorld exige version >= 210)
        writer.Write(new byte[8]); // WorldGenVersion, sin uso real conocido
        writer.Write(new byte[16]); // WorldGUID (version >= 181, siempre cierto aqui)
        writer.Write(header.WorldId);
        writer.Write(0); writer.Write(header.TilesWide * 16); writer.Write(0); writer.Write(header.TilesHigh * 16); // Left/Right/Top/BottomWorld - plausibles, WldReader los descarta sin leerlos

        writer.Write(header.TilesHigh);
        writer.Write(header.TilesWide);

        // GameMode: SIEMPRE el formato Int32 real (version >= 209, cierto para cualquier
        // version >= 210) - mismo criterio de ancho por version que WldReader.ReadHeader/
        // WldWriter.PatchGameMode, aqui sin las dos ramas bool mas antiguas (fuera de alcance,
        // ver el comentario de PointerCount).
        writer.Write(header.GameMode);
        if (version >= 222) writer.Write(false);
        if (version >= 227) writer.Write(false);
        if (version >= 238) writer.Write(false);
        if (version >= 239) writer.Write(false);
        if (version >= 241) writer.Write(false);
        if (version >= 249) writer.Write(false);
        if (version >= 266) writer.Write(false);
        if (version >= 267) writer.Write(false); // ZenithWorld
        if (version >= 302) writer.Write(false);

        if (version >= 141) writer.Write(new byte[8]); // CreationTime
        if (version >= 284) writer.Write(new byte[8]); // LastPlayed
        writer.Write((byte)0); // MoonType
        writer.Write(new byte[4 * 3]); // TreeX
        writer.Write(new byte[4 * 4]); // TreeStyle
        writer.Write(new byte[4 * 3]); // CaveBackX
        writer.Write(new byte[4 * 4]); // CaveBackStyle
        writer.Write(new byte[4 * 3]); // Ice/Jungle/HellBackStyle

        writer.Write(header.SpawnX);
        writer.Write(header.SpawnY);
        writer.Write(header.GroundLevel);
        writer.Write(header.RockLevel);
        writer.Write(header.Time);
        writer.Write(header.DayTime);
        writer.Write(header.MoonPhase);
        writer.Write(header.BloodMoon);
        writer.Write(header.IsEclipse);
        writer.Write(header.DungeonX);
        writer.Write(header.DungeonY);

        writer.Write(header.IsCrimson);
        writer.Write(header.DownedBoss1EyeOfCthulhu);
        writer.Write(header.DownedBoss2EaterOfWorldsOrBrainOfCthulhu);
        writer.Write(header.DownedBoss3Skeletron);
        writer.Write(header.DownedQueenBee);
        writer.Write(header.DownedMechBoss1TheDestroyer);
        writer.Write(header.DownedMechBoss2TheTwins);
        writer.Write(header.DownedMechBoss3SkeletronPrime);
        // DownedMechBossAny: derivado, WldReader lo lee y lo descarta siempre (ver su comentario)
        // - se escribe un valor plausible en vez de un false fijo, aunque no afecte a nada.
        writer.Write(header.DownedMechBoss1TheDestroyer || header.DownedMechBoss2TheTwins || header.DownedMechBoss3SkeletronPrime);
        writer.Write(header.DownedPlantBoss);
        writer.Write(header.DownedGolemBoss);
        // DownedSlimeKingBoss solo existe desde la version 118 (siempre cierto aqui) - null se
        // escribe como false, mismo criterio que "lo que no se ha marcado, no esta derrotado".
        writer.Write(header.DownedSlimeKingBoss ?? false);

        writer.Write(false); // SavedGoblin
        writer.Write(false); // SavedWizard
        writer.Write(false); // SavedMech
        writer.Write(false); // DownedGoblins
        writer.Write(false); // DownedClown
        writer.Write(false); // DownedFrost
        writer.Write(false); // DownedPirates
        writer.Write(false); // ShadowOrbSmashed
        writer.Write(false); // SpawnMeteor
        writer.Write((byte)0); // ShadowOrbCount
        writer.Write(0); // AltarCount
        writer.Write(header.HardMode);
    }

    // Inversa real de WldReader.ReadTiles/ReadOneTile, con el mismo algoritmo de compresion RLE
    // por columna que World.FileV2.cs de TEdit (SaveTiles: escanea hacia adelante mientras el
    // siguiente tile sea IGUAL, con tope de 32767 para poder codificar la longitud como Int16 con
    // signo - igual que hace el propio juego real). Solo empaqueta los campos que WldTile
    // conserva de verdad (tipo/pared/liquido/u/v) - wires/actuador/estilo de ladrillo/pintura NO
    // se escriben nunca porque WldReader.ReadOneTile tampoco los guarda al leer (se limita a
    // saltarlos), asi que un tile releido con WldReader.Read es indistinguible de haberlos
    // escrito o no.
    private static byte[] SerializeTiles(WldTile[,] tiles, uint version, bool[] tileFrameImportant)
    {
        int wide = tiles.GetLength(0);
        int high = tiles.GetLength(1);

        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        for (int x = 0; x < wide; x++)
        {
            int y = 0;
            while (y < high)
            {
                var tile = tiles[x, y];
                int rle = 0;
                while (y + 1 + rle < high && rle < 32767 && TilesEqualForRle(tile, tiles[x, y + 1 + rle]))
                    rle++;

                WriteOneTileForWrite(writer, tile, rle, version, tileFrameImportant);
                y += 1 + rle;
            }
        }
        return ms.ToArray();
    }

    private static bool TilesEqualForRle(WldTile a, WldTile b) =>
        a.Type == b.Type && a.Wall == b.Wall && a.LiquidType == b.LiquidType &&
        a.LiquidAmount == b.LiquidAmount && a.U == b.U && a.V == b.V;

    // Orden de campos EXACTO al de WldReader.ReadOneTile (nunca reordenar): tipo+u/v, pared,
    // liquido, y solo AL FINAL (si hace falta) el byte alto de una pared >255 - ese orden real
    // (wallHigh va DESPUES de liquido, no justo detras del byte bajo de la pared) es la parte mas
    // facil de acertar mal si se reconstruye de memoria en vez de leyendo el lector real linea a
    // linea, ver el comentario de cabecera de este fichero.
    private static void WriteOneTileForWrite(BinaryWriter writer, WldTile tile, int rle, uint version, bool[] tileFrameImportant)
    {
        byte header1 = 0, header2 = 0, header3 = 0;
        var payload = new MemoryStream();
        using (var pw = new BinaryWriter(payload))
        {
            if (tile.IsActive)
            {
                if (tile.Type is < 0 or > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(tile), $"Type={tile.Type} fuera de rango representable (0..{short.MaxValue}).");

                header1 |= 0x02;
                if (tile.Type > 255)
                {
                    pw.Write((byte)(tile.Type & 0xFF));
                    pw.Write((byte)((tile.Type >> 8) & 0xFF));
                    header1 |= 0x20;
                }
                else
                {
                    pw.Write((byte)tile.Type);
                }

                bool framed = tile.Type < tileFrameImportant.Length ? tileFrameImportant[tile.Type] : true;
                if (framed)
                {
                    pw.Write(tile.U);
                    pw.Write(tile.V);
                }
            }

            if (tile.Wall != 0)
            {
                if (tile.Wall > 255 && version < 222)
                    throw new NotSupportedException($"Pared {tile.Wall} (>255) necesita version >= 222 para el byte alto - se pidio version {version}.");
                header1 |= 0x04;
                pw.Write((byte)(tile.Wall & 0xFF));
            }

            if (tile.LiquidAmount > 0 && tile.LiquidType != 0)
            {
                if (tile.LiquidType is < 1 or > 4)
                    throw new ArgumentOutOfRangeException(nameof(tile), $"LiquidType={tile.LiquidType} fuera de rango real (1=agua,2=lava,3=miel,4=Shimmer sintetico).");
                bool isShimmer = tile.LiquidType == 4;
                if (isShimmer && version < 269)
                    throw new NotSupportedException($"Shimmer (LiquidType=4) necesita version >= 269 - se pidio version {version}.");

                byte diskCode = isShimmer ? (byte)3 : tile.LiquidType; // Shimmer comparte codigo de disco con miel (ver WldReader.ReadOneTile, H3-10)
                header1 |= (byte)(diskCode << 3);
                pw.Write(tile.LiquidAmount);
                if (isShimmer) header3 |= 0x80;
            }

            if (tile.Wall > 255)
            {
                header3 |= 0x40;
                pw.Write((byte)((tile.Wall >> 8) & 0xFF));
            }
        }

        if (header3 != 0)
        {
            header2 = 0x01; // "hay header3"
            header1 |= 0x01; // "hay header2"
        }

        byte[] payloadBytes = payload.ToArray();

        if (rle > 0)
        {
            if (rle <= 255) header1 |= 0x40; // RLE de 1 byte
            else header1 |= 0x80; // RLE de Int16
        }

        writer.Write(header1);
        if ((header1 & 0x01) != 0) writer.Write(header2);
        if ((header2 & 0x01) != 0) writer.Write(header3);
        writer.Write(payloadBytes);

        if (rle > 0)
        {
            if (rle <= 255) writer.Write((byte)rle);
            else writer.Write((short)rle);
        }
    }

    // Inversa real de WldReader.ReadRawSigns (el texto va PRIMERO, igual que WriteSignText) -
    // aqui SIN letreros "fantasma" (ese concepto solo existe al EDITAR un archivo real ya
    // existente, ver el comentario de WriteSignText); un mundo construido desde cero solo tiene
    // los letreros que el propio llamador pida.
    private static byte[] SerializeSignsSection(IReadOnlyList<WldSign> signs)
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)signs.Count);
        foreach (var sign in signs)
        {
            writer.Write(sign.Text);
            writer.Write(sign.X);
            writer.Write(sign.Y);
        }
        return ms.ToArray();
    }

    // Inversa real de WldReader.ReadNpcs. WldNpc solo guarda la posicion en TILES (no el x/y en
    // PIXELES real del archivo) - se reconstruye un x/y en pixeles plausible (TileX*16, TileY*16)
    // que hace que el propio WldReader vuelva a derivar el MISMO TileX/TileY si el NPC esta
    // marcado Homeless (formula real: Math.Round(x/16.0)) y se escribe TAMBIEN homeTileX/Y
    // identico para el caso no-Homeless - los dos caminos de lectura posibles quedan cubiertos
    // con el mismo par de valores, sin necesidad de que WldNpc distinga los dos sistemas de
    // coordenadas del archivo real.
    private static byte[] SerializeNpcsSection(IReadOnlyList<WldNpc> npcs, IReadOnlySet<int> shimmeredTypes, uint version)
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        if (version >= 268)
        {
            writer.Write(shimmeredTypes.Count);
            foreach (int t in shimmeredTypes) writer.Write(t);
        }

        foreach (var npc in npcs)
        {
            writer.Write(true); // hay otro NPC mas
            writer.Write(npc.Id);
            writer.Write(npc.GivenName);
            writer.Write((float)(npc.TileX * 16));
            writer.Write((float)(npc.TileY * 16));
            writer.Write(npc.Homeless);
            writer.Write(npc.TileX);
            writer.Write(npc.TileY);

            if (version >= 213)
            {
                if (npc.VariationIndex != 0)
                {
                    writer.Write((byte)1);
                    writer.Write(npc.VariationIndex);
                }
                else
                {
                    writer.Write((byte)0);
                }
            }
            if (version >= 315) writer.Write(false); // homelessDespawn, no lo guarda WldNpc
        }
        writer.Write(false); // termina la lista de NPCs
        return ms.ToArray();
    }

    // Inversa real de WldReader.ReadTileEntities para los 9 tipos cuyo formato es un unico slot
    // (o ninguno) - encaja exactamente con lo que WldTileEntity.Items (una lista PLANA, sin
    // distincion de slot/objeto-vs-tinte) puede representar sin ambiguedad. DisplayDoll/HatRack
    // (los 2 tipos que de verdad tienen VARIOS slots con esa distincion) se rechazan explicitos
    // en vez de escribir algo adivinado - "lo que no se puede reconstruir con fidelidad real, no
    // se inventa", mismo criterio que el resto del proyecto. Ver bitacora.md.
    private static byte[] SerializeTileEntitiesSection(IReadOnlyList<WldTileEntity> entities, uint version)
    {
        if (entities.Count > 0 && version < 122)
            throw new NotSupportedException($"Los mundos de formato anterior a la version 122 no tienen seccion de tile entities - se pidio version {version} con {entities.Count} entidad(es).");

        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        if (version < 122) return ms.ToArray(); // sin seccion en absoluto, ver WldReader.ReadTileEntities

        writer.Write(entities.Count);
        int syntheticId = 0;
        foreach (var entity in entities)
        {
            writer.Write((byte)entity.Kind);
            writer.Write(syntheticId++); // Id interno correlativo - WldReader lo lee y lo descarta siempre
            writer.Write((short)entity.X);
            writer.Write((short)entity.Y);

            switch (entity.Kind)
            {
                case WldTileEntityKind.TrainingDummy:
                    writer.Write((short)0); // Npc del dummy - WldTileEntity no lo guarda
                    break;
                case WldTileEntityKind.ItemFrame:
                case WldTileEntityKind.WeaponRack:
                case WldTileEntityKind.FoodPlatter:
                case WldTileEntityKind.DeadCellsDisplayJar:
                    WriteStackFrom(writer, entity.Items, 0);
                    break;
                case WldTileEntityKind.LogicSensor:
                    writer.Write((byte)0); // LogicCheck
                    writer.Write(false);   // On
                    break;
                case WldTileEntityKind.TeleportationPylon:
                    break; // sin datos propios
                case WldTileEntityKind.CritterAnchor:
                case WldTileEntityKind.KiteAnchor:
                    writer.Write(entity.Items.Count > 0 ? (short)entity.Items[0].NetId : (short)0);
                    break;
                case WldTileEntityKind.DisplayDoll:
                case WldTileEntityKind.HatRack:
                    throw new NotSupportedException($"WldWriter.WriteWorld todavia no escribe tile entities de tipo {entity.Kind} - WldTileEntity.Items es una lista plana sin distincion de slot/objeto-vs-tinte, la unica que estos 2 tipos necesitan para reconstruirse con fidelidad real. Ver bitacora.md.");
                default:
                    throw new NotSupportedException($"Tipo de tile entity desconocido: {entity.Kind}");
            }
        }
        return ms.ToArray();
    }

    // TileEntity.SaveStack real (inversa de WldReader.ReadStackInto): SIEMPRE 5 bytes
    // (NetId Int16 + Prefix byte + Stack Int16), presente o no el objeto - un slot vacio se
    // escribe como NetId=0/Prefix=0/Stack=0, igual que el juego real.
    private static void WriteStackFrom(BinaryWriter writer, IReadOnlyList<WldTileEntityItem> items, int index)
    {
        if (index < items.Count)
        {
            var item = items[index];
            writer.Write((short)item.NetId);
            writer.Write(item.Prefix);
            writer.Write(item.Stack);
        }
        else
        {
            writer.Write((short)0);
            writer.Write((byte)0);
            writer.Write((short)0);
        }
    }

    // Inversa real de WldReader.ReadBestiary - solo se escribe si version >= 210 (WriteWorld ya
    // lo exige siempre, asi que este `if` nunca es falso en la practica; se mantiene explicito
    // por si algun dia se relaja la restriccion de PointerCount).
    private static byte[] SerializeBestiarySection(WldBestiary? bestiary, uint version)
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        if (version < 210) return ms.ToArray();

        var kills = bestiary?.Kills ?? new Dictionary<string, int>();
        writer.Write(kills.Count);
        foreach (var (key, count) in kills) { writer.Write(key); writer.Write(count); }

        var sighted = bestiary?.Sighted ?? new HashSet<string>();
        writer.Write(sighted.Count);
        foreach (var key in sighted) writer.Write(key);

        var chatted = bestiary?.Chatted ?? new HashSet<string>();
        writer.Write(chatted.Count);
        foreach (var key in chatted) writer.Write(key);

        return ms.ToArray();
    }

    // Localiza el offset REAL de GameMode replicando exactamente la misma secuencia de
    // lecturas que WldReader.ReadHeader hasta llegar a el (titulo/semilla/GUID son de longitud
    // variable segun el propio contenido del archivo - no hay ningun offset fijo posible) y
    // devuelve una COPIA del array de bytes con el campo sobrescrito. El array de entrada
    // nunca se modifica in-place (el llamador conserva el original intacto por si algo falla
    // despues de esta llamada y hay que descartar el intento).
    public static byte[] PatchGameMode(byte[] fileBytes, int newGameMode)
    {
        if (newGameMode is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(newGameMode), "El modo de juego real de Terraria solo tiene 4 valores (0=Clasico, 1=Experto, 2=Maestro, 3=Viaje).");

        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);

        uint version = reader.ReadUInt32();

        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");
        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision
        reader.ReadInt64();  // banderas de favorito

        short pointerCount = reader.ReadInt16();
        for (int i = 0; i < pointerCount; i++) reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5).");

        WldReader.ReadBitArray(reader); // tileFrameImportant - mismo lector que WldReader, nunca duplicado

        reader.ReadString(); // title

        if (version == 179) reader.ReadInt32(); else reader.ReadString(); // seed

        reader.ReadBytes(8); // WorldGenVersion
        if (version >= 181) reader.ReadBytes(16); // WorldGUID
        reader.ReadInt32(); // worldId
        reader.ReadBytes(16); // Left/Right/Top/BottomWorld
        reader.ReadInt32(); // tilesHigh
        reader.ReadInt32(); // tilesWide

        // Mismo criterio de ancho por version que WldReader.ReadHeader (ver su comentario F-14,
        // confirmado byte a byte contra World.FileV2.cs de TEdit, commit f592261).
        long gameModeOffset = stream.Position;
        int gameModeWidth;
        if (version >= 209) gameModeWidth = 4;
        else if (version >= 112) gameModeWidth = 1; // bool, incluye la variante Maestro==208
        else throw new NotSupportedException($"Los mundos de formato {version} (anteriores a la version 112) no tienen ningun concepto de dificultad que escribir.");

        var patched = (byte[])fileBytes.Clone();
        if (gameModeWidth == 4)
        {
            // Int32 little-endian real (>=209) - BitConverter.GetBytes ya produce ese orden en
            // cualquier arquitectura x86/x64 real, mismo criterio que el resto del proyecto.
            BitConverter.GetBytes(newGameMode).CopyTo(patched, (int)gameModeOffset);
        }
        else
        {
            // version 208: Maestro(2)->true, Clasico(0)->false (nunca hubo Experto/Viaje en un
            // mundo de esta version). version 112..207: Experto(1)->true, Clasico(0)->false.
            bool asBool = version == 208 ? newGameMode == 2 : newGameMode == 1;
            if (!SupportsGameMode(version, newGameMode))
                throw new NotSupportedException($"Los mundos de formato {version} solo admiten Clasico/{(version == 208 ? "Maestro" : "Experto")} - el modo pedido no existia todavia en esta version de Terraria.");
            patched[gameModeOffset] = (byte)(asBool ? 1 : 0);
        }
        return patched;
    }

    // La MISMA regla que aplica PatchGameMode (que la usa, para que no puedan divergir), expuesta
    // aparte para que la interfaz pueda decirlo ANTES en vez de dejar pulsar "Guardar" y responder
    // con una excepcion: en un mundo anterior a la version 209 el campo GameMode es un simple bool
    // y la mitad de los modos ni siquiera existian todavia en el juego.
    //   >= 209 -> Int32 real: los 4 modos (0=Clasico, 1=Experto, 2=Maestro, 3=Viaje).
    //   == 208 -> bool "maestro": solo Clasico y Maestro.
    //   112..207 -> bool "experto": solo Clasico y Experto.
    //   < 112 -> el concepto de dificultad no existe en el archivo.
    public static bool SupportsGameMode(uint version, int gameMode)
    {
        if (gameMode is < 0 or > 3) return false;
        if (version >= 209) return true;
        if (version == 208) return gameMode is 0 or 2;
        if (version >= 112) return gameMode is 0 or 1;
        return false;
    }

    // Editor de mundos v1 (14-sep-2026, guia real de bitacora.md 13-sep-2026): offsets reales de
    // TODO el tramo SpawnX..HardMode, calculados UNA sola vez replicando exactamente la misma
    // secuencia de lecturas que WldReader.ReadHeader (confirmado que es de ancho FIJO, sin
    // ningun string variable de por medio - ver el comentario real de WldHeader). Los tres
    // Patch* de abajo comparten este UNICO calculo para que nunca puedan divergir entre si -
    // mismo criterio real que WldReader.ReadBitArray, reutilizado por PatchGameMode.
    private readonly record struct HeaderOffsets(
        uint Version, long SpawnX, long SpawnY, long Time, long DayTime, long MoonPhase, long BloodMoon, long IsEclipse,
        long IsCrimson, long DownedBoss1, long DownedBoss2, long DownedBoss3, long DownedQueenBee,
        long DownedMech1, long DownedMech2, long DownedMech3, long DownedPlant, long DownedGolem,
        long? DownedSlimeKing, long HardMode);

    private static HeaderOffsets ComputeHeaderOffsets(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);

        uint version = reader.ReadUInt32();
        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");
        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision
        reader.ReadInt64();  // banderas de favorito

        short pointerCount = reader.ReadInt16();
        for (int i = 0; i < pointerCount; i++) reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5).");

        WldReader.ReadBitArray(reader); // tileFrameImportant

        reader.ReadString(); // title
        if (version == 179) reader.ReadInt32(); else reader.ReadString(); // seed

        reader.ReadBytes(8); // WorldGenVersion
        if (version >= 181) reader.ReadBytes(16); // WorldGUID
        reader.ReadInt32(); // worldId
        reader.ReadBytes(16); // Left/Right/Top/BottomWorld
        reader.ReadInt32(); // tilesHigh
        reader.ReadInt32(); // tilesWide

        // Mismo criterio de ancho por version que WldReader.ReadHeader/PatchGameMode.
        if (version >= 209)
        {
            reader.ReadInt32();
            if (version >= 222) reader.ReadBoolean();
            if (version >= 227) reader.ReadBoolean();
            if (version >= 238) reader.ReadBoolean();
            if (version >= 239) reader.ReadBoolean();
            if (version >= 241) reader.ReadBoolean();
            if (version >= 249) reader.ReadBoolean();
            if (version >= 266) reader.ReadBoolean();
            if (version >= 267) reader.ReadBoolean();
            if (version >= 302) reader.ReadBoolean();
        }
        else if (version >= 112)
        {
            reader.ReadBoolean();
        }

        if (version >= 141) reader.ReadBytes(8); // CreationTime
        if (version >= 284) reader.ReadBytes(8); // LastPlayed
        reader.ReadByte(); // MoonType
        reader.ReadBytes(4 * 3); // TreeX
        reader.ReadBytes(4 * 4); // TreeStyle
        reader.ReadBytes(4 * 3); // CaveBackX
        reader.ReadBytes(4 * 4); // CaveBackStyle
        reader.ReadBytes(4 * 3); // Ice/Jungle/HellBackStyle

        long spawnXOffset = stream.Position; reader.ReadInt32();
        long spawnYOffset = stream.Position; reader.ReadInt32();
        reader.ReadDouble(); // groundLevel
        reader.ReadDouble(); // rockLevel
        long timeOffset = stream.Position; reader.ReadDouble();
        long dayTimeOffset = stream.Position; reader.ReadBoolean();
        long moonPhaseOffset = stream.Position; reader.ReadInt32();
        long bloodMoonOffset = stream.Position; reader.ReadBoolean();
        long isEclipseOffset = stream.Position; reader.ReadBoolean();
        reader.ReadInt32(); // dungeonX
        reader.ReadInt32(); // dungeonY

        long isCrimsonOffset = stream.Position; reader.ReadBoolean();
        long downedBoss1Offset = stream.Position; reader.ReadBoolean();
        long downedBoss2Offset = stream.Position; reader.ReadBoolean();
        long downedBoss3Offset = stream.Position; reader.ReadBoolean();
        long downedQueenBeeOffset = stream.Position; reader.ReadBoolean();
        long downedMech1Offset = stream.Position; reader.ReadBoolean();
        long downedMech2Offset = stream.Position; reader.ReadBoolean();
        long downedMech3Offset = stream.Position; reader.ReadBoolean();
        reader.ReadBoolean(); // DownedMechBossAny
        long downedPlantOffset = stream.Position; reader.ReadBoolean();
        long downedGolemOffset = stream.Position; reader.ReadBoolean();
        long? downedSlimeKingOffset = null;
        if (version >= 118) { downedSlimeKingOffset = stream.Position; reader.ReadBoolean(); }

        reader.ReadBoolean(); // SavedGoblin
        reader.ReadBoolean(); // SavedWizard
        reader.ReadBoolean(); // SavedMech
        reader.ReadBoolean(); // DownedGoblins
        reader.ReadBoolean(); // DownedClown
        reader.ReadBoolean(); // DownedFrost
        reader.ReadBoolean(); // DownedPirates
        reader.ReadBoolean(); // ShadowOrbSmashed
        reader.ReadBoolean(); // SpawnMeteor
        reader.ReadByte();    // ShadowOrbCount
        reader.ReadInt32();   // AltarCount
        long hardModeOffset = stream.Position;

        return new HeaderOffsets(version, spawnXOffset, spawnYOffset, timeOffset, dayTimeOffset, moonPhaseOffset,
            bloodMoonOffset, isEclipseOffset, isCrimsonOffset, downedBoss1Offset, downedBoss2Offset, downedBoss3Offset,
            downedQueenBeeOffset, downedMech1Offset, downedMech2Offset, downedMech3Offset, downedPlantOffset,
            downedGolemOffset, downedSlimeKingOffset, hardModeOffset);
    }

    // Punto de aparicion del mundo (WorldGen.spawnTile real) - un Int32 par, sin ninguna
    // restriccion de version (existe desde el formato mas antiguo que este lector admite). El
    // llamador (WorldFileService.SaveSpawnPoint) es quien valida que el punto cae dentro del
    // mundo - aqui solo se escribe, sin decidir si el valor tiene sentido.
    public static byte[] PatchSpawnPoint(byte[] fileBytes, int newSpawnX, int newSpawnY)
    {
        var o = ComputeHeaderOffsets(fileBytes);
        var patched = (byte[])fileBytes.Clone();
        BitConverter.GetBytes(newSpawnX).CopyTo(patched, (int)o.SpawnX);
        BitConverter.GetBytes(newSpawnY).CopyTo(patched, (int)o.SpawnY);
        return patched;
    }

    // Hora del dia + fase lunar + luna de sangre/eclipse - los 5 campos son un tramo contiguo
    // real (Main.time/dayTime/moonPhase/bloodMoon/eclipse, ver World.FileV2.cs de TEdit), se
    // escriben juntos porque en el juego real tambien cambian juntos (un DayTime que no
    // corresponde al rango real de Time no tiene sentido - ver la conversion real en
    // ExplorationViewModel).
    public static byte[] PatchTimeAndMoon(byte[] fileBytes, double newTime, bool newDayTime, int newMoonPhase, bool newBloodMoon, bool newIsEclipse)
    {
        if (newMoonPhase is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(newMoonPhase), "La fase lunar real de Terraria solo tiene 8 valores (0-7).");
        if (newTime < 0)
            throw new ArgumentOutOfRangeException(nameof(newTime), "El reloj del mundo no puede ser negativo.");

        var o = ComputeHeaderOffsets(fileBytes);
        var patched = (byte[])fileBytes.Clone();
        BitConverter.GetBytes(newTime).CopyTo(patched, (int)o.Time);
        patched[o.DayTime] = (byte)(newDayTime ? 1 : 0);
        BitConverter.GetBytes(newMoonPhase).CopyTo(patched, (int)o.MoonPhase);
        patched[o.BloodMoon] = (byte)(newBloodMoon ? 1 : 0);
        patched[o.IsEclipse] = (byte)(newIsEclipse ? 1 : 0);
        return patched;
    }

    // Banderas de progreso ("jefes derrotados", pedido explicito de bitacora.md 13-sep-2026,
    // punto 6/paneles de progreso). Cada campo es un `bool?` real: null significa "no tocar este
    // campo" (deja el valor que ya hubiera en el archivo) - asi la interfaz puede mandar solo LOS
    // que el usuario cambio de verdad, sin tener que releer y repetir los otros 10.
    public readonly record struct WorldFlagsPatch(
        bool? DownedBoss1EyeOfCthulhu = null, bool? DownedBoss2EaterOfWorldsOrBrainOfCthulhu = null,
        bool? DownedBoss3Skeletron = null, bool? DownedQueenBee = null, bool? DownedMechBoss1TheDestroyer = null,
        bool? DownedMechBoss2TheTwins = null, bool? DownedMechBoss3SkeletronPrime = null, bool? DownedPlantBoss = null,
        bool? DownedGolemBoss = null, bool? DownedSlimeKingBoss = null, bool? HardMode = null);

    public static byte[] PatchBossFlags(byte[] fileBytes, WorldFlagsPatch patch)
    {
        var o = ComputeHeaderOffsets(fileBytes);
        if (patch.DownedSlimeKingBoss is not null && o.DownedSlimeKing is null)
            throw new NotSupportedException("Los mundos de formato anterior a la version 118 no tienen ningun Rey Slime que marcar - el campo ni siquiera existe en el archivo.");

        var patched = (byte[])fileBytes.Clone();
        void Write(long offset, bool? value) { if (value is bool v) patched[offset] = (byte)(v ? 1 : 0); }
        Write(o.DownedBoss1, patch.DownedBoss1EyeOfCthulhu);
        Write(o.DownedBoss2, patch.DownedBoss2EaterOfWorldsOrBrainOfCthulhu);
        Write(o.DownedBoss3, patch.DownedBoss3Skeletron);
        Write(o.DownedQueenBee, patch.DownedQueenBee);
        Write(o.DownedMech1, patch.DownedMechBoss1TheDestroyer);
        Write(o.DownedMech2, patch.DownedMechBoss2TheTwins);
        Write(o.DownedMech3, patch.DownedMechBoss3SkeletronPrime);
        Write(o.DownedPlant, patch.DownedPlantBoss);
        Write(o.DownedGolem, patch.DownedGolemBoss);
        if (o.DownedSlimeKing is long slimeKingOffset) Write(slimeKingOffset, patch.DownedSlimeKingBoss);
        Write(o.HardMode, patch.HardMode);
        return patched;
    }

    // Editor de cofres/letreros v1 (T1 del documento I+D real, "Terrakeep, editor de cofres/
    // letreros del .wld", 15-sep-2026). A diferencia de todo lo de arriba (parchea un tramo de
    // ANCHO FIJO, nunca cambia la longitud del archivo), el contenido de un cofre (numero real
    // de objetos) y el texto de un letrero (string de longitud variable) SI cambian de longitud -
    // hace falta reescribir la seccion ENTERA y corregir la tabla de punteros de cabecera, exacto
    // mismo patron real que usa TEdit (World.FileV2.cs, SaveWorld: escribe seccion a seccion y
    // corrige sectionPointers[] al final).
    //
    // Alcance deliberado: se edita el CONTENIDO de un cofre/letrero que YA EXISTE en el archivo
    // (mismo indice que devuelve WldReader.Read, mismo orden) - nunca se añaden ni se borran
    // cofres/letreros enteros (eso exigiria tocar tambien la seccion de tiles para colocar/quitar
    // el tile del cofre/letrero de verdad - un salto de riesgo mucho mayor, no pedido).

    // Tabla de punteros real: Int16 pointerCount + esa cantidad de Int32, en un offset FIJO desde
    // el principio del archivo (justo tras version+firma+fileType+FileRevision+flags de favorito,
    // TODO de ancho fijo - nunca depende de ningun string variable) - se puede parchear in-place
    // sin tener que releer nada mas, pase lo que pase con las secciones de despues.
    private readonly record struct PointerTable(uint Version, int[] Pointers, long PointersArrayOffset);

    private static PointerTable ReadPointerTable(byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);

        uint version = reader.ReadUInt32();
        string signature = new(reader.ReadChars(7));
        if (signature != "relogic")
            throw new InvalidDataException($"Firma de .wld invalida: '{signature}' (se esperaba 'relogic').");
        byte fileType = reader.ReadByte();
        if (fileType != 2)
            throw new InvalidDataException($"Tipo de archivo {fileType} no es un mundo (se esperaba 2).");

        reader.ReadUInt32(); // FileRevision
        reader.ReadInt64();  // banderas de favorito

        short pointerCount = reader.ReadInt16();
        long pointersArrayOffset = stream.Position;
        var pointers = new int[pointerCount];
        for (int i = 0; i < pointerCount; i++) pointers[i] = reader.ReadInt32();
        if (pointerCount < 5)
            throw new NotSupportedException($"Mundo con formato demasiado antiguo (solo {pointerCount} punteros de seccion, hacen falta al menos 5 - cofres y letreros incluidos).");

        return new PointerTable(version, pointers, pointersArrayOffset);
    }

    // Reconstruye el archivo entero sustituyendo el tramo [oldRangeStart, oldRangeEnd) por
    // newSectionBytes, y corrige TODOS los punteros de cabecera que caian en o despues de
    // oldRangeEnd sumandoles el delta real de longitud - unico punto que sabe recomponer un
    // .wld tras una seccion de longitud variable, usado tanto por cofres como por letreros para
    // que los dos caminos no puedan divergir.
    private static byte[] ReplaceSection(byte[] fileBytes, PointerTable table, int oldRangeStart, int oldRangeEnd, byte[] newSectionBytes)
    {
        int delta = newSectionBytes.Length - (oldRangeEnd - oldRangeStart);

        var result = new byte[fileBytes.Length + delta];
        Buffer.BlockCopy(fileBytes, 0, result, 0, oldRangeStart);
        Buffer.BlockCopy(newSectionBytes, 0, result, oldRangeStart, newSectionBytes.Length);
        int tailStart = oldRangeStart + newSectionBytes.Length;
        Buffer.BlockCopy(fileBytes, oldRangeEnd, result, tailStart, fileBytes.Length - oldRangeEnd);

        for (int i = 0; i < table.Pointers.Length; i++)
        {
            int newPointer = table.Pointers[i] >= oldRangeEnd ? table.Pointers[i] + delta : table.Pointers[i];
            BitConverter.GetBytes(newPointer).CopyTo(result, (int)table.PointersArrayOffset + i * 4);
        }
        return result;
    }

    // Formato real confirmado contra World.FileV2.cs de TEdit (LoadChestData/SaveChestData) -
    // mismo criterio de ancho por version que WldReader.ReadChests (reutilizado, nunca
    // reimplementado): version<294 usa un Int16 GLOBAL de capacidad compartido por TODOS los
    // cofres; version>=294 usa un Int32 PROPIO por cofre. Los objetos se escriben empaquetados
    // desde el slot 0 (sin huecos intermedios) - una simplificacion deliberada y segura: el
    // juego no distingue "objeto en el slot 3, huecos 0-2 vacios" de "objeto en el slot 0", solo
    // importa QUE objetos hay dentro, nunca en que hueco exacto cayeron originalmente.
    private static byte[] SerializeChests(IReadOnlyList<WldChest> chests, uint version)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)chests.Count);

        if (version < 294)
        {
            int globalMaxItems = 40;
            foreach (var c in chests) globalMaxItems = Math.Max(globalMaxItems, Math.Max(c.MaxItems, c.Items.Count));
            if (globalMaxItems > short.MaxValue)
                throw new NotSupportedException("Un mundo de formato anterior a la version 294 no admite mas de 32767 huecos por cofre (capacidad global compartida por todos los cofres).");
            writer.Write((short)globalMaxItems);
            foreach (var chest in chests) WriteOneChest(writer, chest, globalMaxItems, writeMaxItems: false);
        }
        else
        {
            foreach (var chest in chests)
            {
                int maxItems = Math.Max(chest.MaxItems, chest.Items.Count);
                WriteOneChest(writer, chest, maxItems, writeMaxItems: true);
            }
        }
        return ms.ToArray();
    }

    private static void WriteOneChest(BinaryWriter writer, WldChest chest, int maxItems, bool writeMaxItems)
    {
        writer.Write(chest.X);
        writer.Write(chest.Y);
        writer.Write(chest.Name);
        if (writeMaxItems) writer.Write(maxItems);
        for (int slot = 0; slot < maxItems; slot++)
        {
            if (slot < chest.Items.Count)
            {
                var item = chest.Items[slot];
                writer.Write(item.Stack);
                writer.Write(item.NetId);
                writer.Write(item.Prefix);
            }
            else
            {
                writer.Write((short)0);
            }
        }
    }

    // Edita el contenido (objetos + cantidades + prefijos) de UN cofre real, identificado por su
    // indice de lectura (el mismo orden que WldWorld.Chests, 0-based) - nunca añade ni quita
    // cofres. El resto del archivo (tiles, el propio X/Y/Nombre del cofre editado, letreros,
    // NPCs, tile entities, bestiario...) se copia byte a byte sin tocar.
    public static byte[] WriteChestItems(byte[] fileBytes, int chestIndex, IReadOnlyList<WldChestItem> newItems)
    {
        var table = ReadPointerTable(fileBytes);

        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);
        stream.Position = table.Pointers[2];
        var chests = WldReader.ReadChests(reader, table.Version);

        if (chestIndex < 0 || chestIndex >= chests.Count)
            throw new ArgumentOutOfRangeException(nameof(chestIndex), $"El mundo tiene {chests.Count} cofres reales, no existe el indice {chestIndex}.");

        var original = chests[chestIndex];
        chests[chestIndex] = new WldChest { X = original.X, Y = original.Y, Name = original.Name, Items = newItems, MaxItems = original.MaxItems };

        byte[] newChestsBytes = SerializeChests(chests, table.Version);
        return ReplaceSection(fileBytes, table, table.Pointers[2], table.Pointers[3], newChestsBytes);
    }

    // Formato real confirmado contra World.FileV2.cs de TEdit (LoadSignData/SaveSignData): el
    // texto va PRIMERO (String Text, Int32 X, Int32 Y), igual que WldReader.ReadRawSigns. Se
    // reescriben TODAS las entradas, incluidas las "fantasma" (ver el comentario real de
    // WldReader.ReadSigns/WldSign) - solo se sustituye el texto de la que coincide en (X,Y) con
    // el letrero que el usuario edito, todo lo demas viaja identico.
    public static byte[] WriteSignText(byte[] fileBytes, int signX, int signY, string newText)
    {
        var table = ReadPointerTable(fileBytes);

        using var stream = new MemoryStream(fileBytes, writable: false);
        using var reader = new BinaryReader(stream);
        stream.Position = table.Pointers[3];
        var rawSigns = WldReader.ReadRawSigns(reader);

        int index = rawSigns.FindIndex(s => s.X == signX && s.Y == signY);
        if (index < 0)
            throw new ArgumentException($"No hay ningun letrero real en ({signX},{signY}) en la seccion de letreros del archivo.", nameof(signX));
        rawSigns[index] = (newText, signX, signY);

        using var outMs = new MemoryStream();
        using var writer = new BinaryWriter(outMs);
        writer.Write((short)rawSigns.Count);
        foreach (var (text, x, y) in rawSigns)
        {
            writer.Write(text);
            writer.Write(x);
            writer.Write(y);
        }
        byte[] newSignsBytes = outMs.ToArray();

        return ReplaceSection(fileBytes, table, table.Pointers[3], table.Pointers[4], newSignsBytes);
    }
}
