using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.Core.Guia;

// La mitad "de escritorio" del cerebro unico de la Guia (ver GuideEvaluationEngine.cs para el
// porque de esta consolidacion). Implementa IGuideStateProvider leyendo un GuideContext ESTATICO
// (personaje/mundo .plr/.wld ya cargados en Terrakeep, nunca una partida en marcha) - es la misma
// logica que antes vivia como metodos privados dentro de GuideEvaluator.cs, movida aqui sin
// cambiar ni un criterio: el CONTRATO HONESTO sigue igual (un tipo de requisito que Terrakeep no
// pueda comprobar desde ficheros estaticos se queda NoEvaluable con su motivo real citado, JAMAS
// se inventa un valor - ver la cabecera original de GuideEvaluator.cs, ahora en GuideEvaluator.cs
// como adaptador de compatibilidad).
internal sealed class DesktopGuideStateProvider(
    VanillaItemCatalog vanillaItems, NpcNameCatalog npcNames, CalamityCatalog? calamityItems, GuideContext contexto)
    : IGuideStateProvider
{
    public bool HasCharacterData => contexto.Character != null;
    public bool HasWorldData => contexto.World != null;
    public bool HasInventoryData => Inventario() != null;

    // Terrakeep es un editor de ficheros .plr/.wld ESTATICOS: no hay combate, no hay
    // multiplicadores de clase, no hay buffs activos que simular, y un enemigo hostil activo no se
    // guarda en ningun archivo. Reproducir esos numeros a mano seria fingir una precision que no
    // existe - la misma razon por la que un tipo de requisito desconocido se deja NoEvaluable en
    // vez de inventado. Por eso esto es SIEMPRE false aqui (y siempre true en TerrakeepMod).
    public bool HasLiveGameData => false;

    // Los cuatro de abajo solo se leen cuando HasLiveGameData/HasCharacterData son true; aqui
    // CristalesVida/Defensa no se llaman nunca (HasLiveGameData es fijo a false), se dejan a 0 por
    // higiene de la interfaz.
    public int CristalesVida => 0;
    public int VidaMaxima => contexto.Character?.HealthMax ?? 0;
    public int Defensa => 0;

    public int NpcsDelPueblo() => contexto.World?.Npcs.Count ?? 0;
    public bool HayNpc(int id) => contexto.World != null && contexto.World.Npcs.Any(n => n.Id == id);

    public int CuantosLleva(int id)
    {
        var inventario = Inventario();
        if (inventario == null || id <= 0) return 0;
        int total = 0;
        foreach (var item in inventario)
            if (!item.IsEmpty && item.Id == id) total += item.Count;
        return total;
    }

    // Nunca se llaman (HasLiveGameData fijo a false): daño de arma real exige los multiplicadores
    // de clase que solo aplica Player.GetWeaponDamage con una partida en marcha.
    public int DanoDelMejorArma(out string nombre) { nombre = ""; return 0; }

    public bool LlevaGancho(out string nombre)
    {
        var inventario = Inventario();
        var encontrado = inventario?.FirstOrDefault(i => !i.IsEmpty && GuideHooks.IdsDeGancho.Contains(i.Id));
        nombre = encontrado != null ? NombreDeObjeto(encontrado.Id) : "";
        return encontrado != null;
    }

    public bool BanderaConocida(string bandera) => GuideFlags.Existe(bandera);
    public bool? ValorBandera(string bandera) => GuideFlags.Valor(bandera, contexto);

    public string? MotivoBanderaDesconocida(string bandera) =>
        contexto.HasCalamity ? "guide_motive_flag_calamity" : "guide_motive_flag_unsupported";

    public string NombreDeObjeto(int id)
    {
        if (id <= 0) return "#" + id;
        if (id >= CalamityIds.ItemIdBase)
            return calamityItems?.BySyntheticId(id)?.DisplayName ?? "#" + id;
        return vanillaItems.GetName(id);
    }

    public string NombreDeNpc(int id) => npcNames.GetName(id);

    public string MotivoSinPartidaEnMarcha(TipoRequisitoGuia tipo) => tipo switch
    {
        TipoRequisitoGuia.CristalesVida => "guide_motive_life_crystals",
        TipoRequisitoGuia.Defensa => "guide_motive_defense",
        TipoRequisitoGuia.DanoArma => "guide_motive_weapon_damage",
        TipoRequisitoGuia.NpcActivo => "guide_motive_active_npc",
        _ => "guide_motive_load_data",
    };

    private GameItem[]? Inventario() =>
        contexto.MergedContainers != null && contexto.MergedContainers.TryGetValue("inventory", out var inv) ? inv : null;
}
