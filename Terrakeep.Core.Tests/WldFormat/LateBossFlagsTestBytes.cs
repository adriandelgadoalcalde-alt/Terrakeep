namespace Terrakeep.Core.Tests.WldFormat;

// Guia Encargo5b (26-sep-2026): WldReader.ReadHeader ya no se detiene en HardMode - sigue
// leyendo Anglers/LoadBanners/los 11 flags de jefe tardio (ver WldReader.ReadLateBossFlags).
// Varios ficheros de este proyecto construyen una cabecera .wld SINTETICA byte a byte para
// probar WldWriter.PatchXxx/WldTileEntityReaderTests contra un archivo minimo que antes
// terminaba justo en HardMode - ahora necesitan estos bytes de mas para que ReadHeader no
// lance EndOfStreamException. Pieza COMPARTIDA (nunca duplicada) para que los 5 ficheros que la
// necesitan (WldWriterProgressPatchTests/WldWriterTests/WldWriterSupportsGameModeTests/
// WldWriterChestSignTests/WldTileEntityReaderTests) no reimplementen el mismo tramo 5 veces -
// mismo criterio ya establecido en el propio WldReader (ReadBitArray/ReadChests/ReadRawSigns
// reutilizadas, nunca reimplementadas). Todo a "false"/0/longitud-cero a proposito: a ninguna de
// estas pruebas le importa el VALOR de estos campos, solo que el offset de lo que viene despues
// (Pointers[1..]) siga siendo correcto.
internal static class LateBossFlagsTestBytes
{
    public static void WriteMinimal(BinaryWriter w, uint version)
    {
        if (version >= 257) w.Write(false); // PartyOfDoom
        w.Write(0); w.Write(0); w.Write(0); w.Write(0.0); // InvasionDelay/Size/Type/X
        if (version >= 118) w.Write(0.0); // SlimeRainTime
        if (version >= 113) w.Write((byte)0); // SundialCooldown
        w.Write(false); // IsRaining
        w.Write(0); // TempRainTime
        w.Write(0f); // TempMaxRain
        w.Write(0); w.Write(0); w.Write(0); // SavedOreTiers Cobalt/Mythril/Adamantite
        w.Write(new byte[8]); // BgTree..BgOcean
        w.Write(0); // CloudBgActive
        w.Write((short)0); // NumClouds
        w.Write(0f); // WindSpeedSet

        if (version < 95) return;
        w.Write(0); // Anglers count = 0

        if (version < 99) return;
        w.Write(false); // SavedAngler

        if (version < 101) return;
        w.Write(0); // AnglerQuest

        if (version < 104) return;
        w.Write(false); // SavedStylist
        if (version >= 140) w.Write(false); // SavedTaxCollector
        if (version >= 201) w.Write(false); // SavedGolfer
        if (version >= 107) w.Write(0); // InvasionSizeStart
        if (version >= 108) w.Write(0); // CultistDelay

        if (version < 109) return;
        w.Write((short)0); // KilledMobs count = 0
        if (version >= 289) w.Write((short)0); // ClaimableBanners count = 0

        if (version < 128) return;
        if (version >= 140) w.Write(false); // FastForwardTime

        if (version < 131) return;
        w.Write(false); // DownedFishron

        if (version >= 140) { w.Write(false); w.Write(false); w.Write(false); } // Martians/LunaticCultist/Moonlord
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Halloween/Navidad x5

        if (version < 140) return;
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Celestial Solar/Vortex/Nebula/Stardust
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Celestial*Active x4
        w.Write(false); // Apocalypse

        if (version >= 170) { w.Write(false); w.Write(false); w.Write(0); w.Write(0); }
        if (version >= 174) { w.Write(false); w.Write(0); w.Write(0f); w.Write(0f); }
        if (version >= 178) { w.Write(false); w.Write(false); w.Write(false); w.Write(false); }
        if (version > 194) w.Write((byte)0); // MushroomBg
        if (version >= 215) w.Write((byte)0); // UnderworldBg
        if (version >= 195) { w.Write((byte)0); w.Write((byte)0); w.Write((byte)0); }
        if (version >= 204) w.Write(false); // CombatBookUsed
        if (version >= 207) { w.Write(0); w.Write(false); w.Write(false); w.Write(false); }
        if (version >= 211) w.Write(0); // TreeTopVariations count = 0
        if (version >= 212) { w.Write(false); w.Write(false); }
        if (version >= 216) { w.Write(0); w.Write(0); w.Write(0); w.Write(0); }
        if (version >= 217) { w.Write(false); w.Write(false); w.Write(false); }

        if (version >= 223) { w.Write(false); w.Write(false); } // EmpressOfLight/QueenSlime
        if (version >= 240) w.Write(false); // Deerclops
        // Guia v2 (F1): el lector sigue hasta peddlersSatchelWasUsed (WorldFile.LoadHeaderFlags real).
        if (version >= 250) w.Write(false); // unlockedSlimeBlueSpawn
        if (version >= 251) w.Write(new byte[8]); // unlocked*Spawn x8
        if (version >= 259) w.Write(false); // combatBookVolumeTwoWasUsed
        if (version >= 260) w.Write(false); // peddlersSatchelWasUsed
    }
}
