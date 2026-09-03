using System.Globalization;
using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Efecto REAL de un prefijo (multiplicadores de arma o bonos de accesorio) - pregunta a Opus
// sobre el diseño 2-sep-2026, cuarta pasada ("los accesorios... no te pone el porcentaje si es
// +1% de daño +2% de ataque crítico etc"). Fuente real: scripts/extraer-efectos-prefijos.py,
// que parsea Item.TryGetPrefixStatMultipliersForItem (armas, multiplicadores relativos a 1.0)
// y Player.GrantPrefixBenefits (accesorios, bonos planos) del propio Terraria/tModLoader
// decompilado - nunca texto inventado, solo los numeros reales formateados.
public sealed class PrefixEffectCatalog
{
    private sealed class RawEffect
    {
        public double? Dmg { get; set; }
        public double? Kb { get; set; }
        public double? Spd { get; set; }
        public double? Size { get; set; }
        public double? Shtspd { get; set; }
        public double? Mcst { get; set; }
        public double? Crt { get; set; }
        public double? StatDefense { get; set; }
        public double? StatManaMax2 { get; set; }
        public double? AllCrit { get; set; }
        public double? AllDamage { get; set; }
        public double? MoveSpeed { get; set; }
        public double? MeleeSpeed { get; set; }
    }

    private readonly Dictionary<int, RawEffect> _byId;

    private PrefixEffectCatalog(Dictionary<int, RawEffect> byId) => _byId = byId;

    // Texto real en español, compuesto a partir de los numeros reales (nunca una frase
    // inventada) - mismo criterio de "+X%"/"-X%" que ya usa ItemStatsFormatter para
    // daño/defensa/critico. Devuelve null si el prefijo no tiene ningun efecto real conocido
    // (ej. prefijos de Calamity/mods, fuera de este catalogo vanilla-only).
    // H3-08 (tercera auditoria de Opus, Fable): "Defensa total" solo sumaba la defensa base de
    // cada pieza, ignorando los prefijos de accesorio reales (Warding/Guarding/Menacing/etc,
    // confirmados contra Player.GrantPrefixBenefits decompilado: prefijos 62/63/64/65 ->
    // +1/+2/+3/+4 defensa). Mismo dato ya cargado y en produccion para Describe() de arriba -
    // solo faltaba un acceso numerico puro (sin formatear a texto) para poder sumarlo de
    // verdad. Redondeado a entero real (StatDefense siempre es un valor plano, no fraccionario,
    // para los prefijos de defensa reales - nunca a medias).
    public int GetDefenseBonus(int prefixId) =>
        _byId.TryGetValue(prefixId, out var e) ? (int)Math.Round(e.StatDefense ?? 0) : 0;

    public string? Describe(int prefixId)
    {
        if (!_byId.TryGetValue(prefixId, out var e)) return null;

        var parts = new List<string>();
        void Mult(double? value, string label)
        {
            if (value is not double v || v == 1.0) return;
            double pct = Math.Round((v - 1.0) * 100.0);
            parts.Add($"{FormatSigned(pct)}% {label}");
        }
        void Flat(double? value, string label, bool isPercent)
        {
            if (value is not double v || v == 0.0) return;
            string amount = isPercent
                ? $"{FormatSigned(Math.Round(v * 100.0))}%"
                : FormatSigned(v);
            parts.Add($"{amount} {label}");
        }

        Mult(e.Dmg, "de daño");
        Flat(e.AllDamage, "de daño", isPercent: true);
        Flat(e.Crt, "% de probabilidad de golpe crítico", isPercent: false);
        Flat(e.AllCrit, "% de probabilidad de golpe crítico", isPercent: false);
        Mult(e.Kb, "de retroceso");
        Mult(e.Spd, "de tiempo de uso");
        Mult(e.Size, "de tamaño");
        Mult(e.Shtspd, "de velocidad de disparo");
        Mult(e.Mcst, "de coste de maná");
        Flat(e.StatDefense, "de defensa", isPercent: false);
        Flat(e.StatManaMax2, "de maná máximo", isPercent: false);
        Flat(e.MoveSpeed, "de velocidad de movimiento", isPercent: true);
        Flat(e.MeleeSpeed, "de velocidad de ataque cuerpo a cuerpo", isPercent: true);

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private static string FormatSigned(double value)
    {
        string text = Math.Abs(value % 1.0) < 0.001
            ? ((int)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);
        return value >= 0 ? $"+{text}" : text;
    }

    public static PrefixEffectCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static PrefixEffectCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(stream)
            ?? throw new InvalidDataException("vanilla_prefix_effects.json invalido.");
        var byId = new Dictionary<int, RawEffect>(raw.Count);
        foreach (var (key, fields) in raw)
        {
            var e = new RawEffect();
            foreach (var (field, value) in fields)
            {
                switch (field)
                {
                    case "dmg": e.Dmg = value; break;
                    case "kb": e.Kb = value; break;
                    case "spd": e.Spd = value; break;
                    case "size": e.Size = value; break;
                    case "shtspd": e.Shtspd = value; break;
                    case "mcst": e.Mcst = value; break;
                    case "crt": e.Crt = value; break;
                    case "statDefense": e.StatDefense = value; break;
                    case "statManaMax2": e.StatManaMax2 = value; break;
                    case "allCrit": e.AllCrit = value; break;
                    case "allDamage": e.AllDamage = value; break;
                    case "moveSpeed": e.MoveSpeed = value; break;
                    case "meleeSpeed": e.MeleeSpeed = value; break;
                }
            }
            byId[int.Parse(key)] = e;
        }
        return new PrefixEffectCatalog(byId);
    }
}
