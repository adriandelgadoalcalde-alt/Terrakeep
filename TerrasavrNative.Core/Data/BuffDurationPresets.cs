namespace TerrasavrNative.Core.Data;

// Minima/Media/Maxima real de un buff, para los 3 botones de duracion del panel Editar de
// Buffs - pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada ("3 botones que pongan
// duracion maxima media y minima... extrae de terraria y tmodloader los valores... los
// minimos son los que dan por defecto las pociones").
//
// - Minima: dato REAL extraido (buffTime de la pocion, VanillaBuffDurationCatalog). Sin dato
//   real -> 28800 ticks (8 min), la MODA real de los 30 buffTime conocidos, no un valor
//   inventado - IsRealMin distingue los dos casos para que la UI pueda decirlo con honestidad.
// - Media: 2x Minima. UNICO caso real de escalera de duraciones en todo el juego: Suerte
//   (buff 257), Pocion menor/normal/mayor = 18000/36000/54000 ticks, ratio EXACTO 1:2:3 -
//   para el propio 257 se usan los 3 valores reales tal cual (Tiers), no la formula.
// - Maxima: S.getMaxTime() REAL del propio Terrasavr (reference/terrasavr-real/
//   script.beautified.js:9166) = 1999999980 ticks (~385,8 dias) si Version>=269, 1080000
//   (18000s = 5h) si no - mismo umbral 269 que ya usa PlrBodySerializer para decidir 44 vs 22
//   slots de buff. Corregido explicitamente por el usuario tras preguntar por "el maximo
//   permitido" - este es el valor real que usa Terrasavr, no una aproximacion propia.
public readonly record struct BuffDurationPreset(int MinTicks, int MediaTicks, int MaxTicks, bool IsRealMin, int? MinSourceItemId);

public static class BuffDurationPresets
{
    private const int MaxTicksModern = 1999999980;
    private const int MaxTicksLegacy = 1080000;
    private const int FallbackMinTicks = 28800;

    public static BuffDurationPreset GetPresets(int buffId, int characterVersion, VanillaBuffDurationCatalog durations)
    {
        var info = durations.Get(buffId);
        int minTicks = info?.MinTicks ?? FallbackMinTicks;
        bool isRealMin = info != null;

        int mediaTicks = info?.Tiers is { Length: >= 2 } tiers ? tiers[1] : minTicks * 2;

        int realMax = characterVersion >= 269 ? MaxTicksModern : MaxTicksLegacy;
        int maxTicks = info?.Tiers is { Length: 3 } fullTiers ? fullTiers[2] : realMax;

        return new BuffDurationPreset(minTicks, mediaTicks, maxTicks, isRealMin, info?.SourceItemId);
    }
}
