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
    VanillaItemCatalog vanillaItems, NpcNameCatalog npcNames, CalamityCatalog? calamityItems,
    VanillaItemStatsCatalog? vanillaStats, PrefixEffectCatalog? prefixEffects, PrefixRulesCatalog? prefixRules,
    GuideContext contexto)
    : IGuideStateProviderV2
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

    // CristalesVida es DISTINTO, auditoria 24-sep-2026 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md,
    // Encargo 1): a diferencia de DanoArma/NpcActivo, el numero de cristales de vida
    // consumidos SI es derivable de un dato ya parseado del .plr sin partida en marcha -
    // PlrCharacter.HealthMax (VidaMaxima, linea de abajo). Formula real del motor vanilla
    // (Player.cs del tModLoader decompilado, ~linea 56437/55952:
    // "ConsumedLifeCrystals = (statLifeMax - 100) / 20"): cada cristal sube 20 de vida maxima
    // sobre la base de 100, hasta el tope de 15 cristales (400 de vida maxima extra). Clamp por
    // si HealthMax viniera fuera de rango (personaje recien creado con 100 exactos da 0, nunca
    // negativo). Ver el cambio de gate correspondiente en GuideEvaluationEngine (HasCharacterData,
    // no HasLiveGameData) - sin ese cambio esta formula seria codigo muerto igual que antes.
    public int CristalesVida => Math.Clamp((VidaMaxima - 100) / 20, 0, 15);
    public int VidaMaxima => contexto.Character?.HealthMax ?? 0;

    // Guia Encargo2 (25-sep-2026, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md): mismo criterio exacto que
    // CristalesVida arriba - Defensa TAMBIEN es derivable de un dato ya parseado del .plr sin
    // partida en marcha (el equipo puesto de verdad, MergedContainers["loadout0Items"] - el mismo
    // contenedor 0 que EquipmentGroupViewModel usa para "el conjunto que el personaje lleva
    // puesto de verdad al guardar", ver el comentario largo de su constructor). Es la defensa
    // ESTATICA de armadura+accesorios+prefijos (Terrakeep.Core.Model.DefenseCalculator, la misma
    // formula exacta que ya usaba la pestaña Equipamiento, movida aqui para no duplicarla) - nunca
    // incluye buffs/pociones/set bonus en combate real, que solo existen con una partida en
    // marcha. Ver el cambio de gate correspondiente en GuideEvaluationEngine (HasCharacterData, no
    // HasLiveGameData, igual que CristalesVida).
    public int Defensa
    {
        get
        {
            var equipoPuesto = ArmaduraActiva();
            return equipoPuesto == null ? 0 : DefenseCalculator.Total(equipoPuesto, vanillaStats, calamityItems, prefixEffects);
        }
    }

    private GameItem[]? ArmaduraActiva() =>
        contexto.MergedContainers != null && contexto.MergedContainers.TryGetValue("loadout0Items", out var items) ? items : null;

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

    // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689, I+D-PROXIMOS-PASOS-FAMILIA-
    // KEEP.md): el daño de arma REAL en combate (motor completo, multiplicadores de CLASE -
    // Player.GetWeaponDamage) sigue exigiendo una partida en marcha - eso JAMAS lo tendra
    // Terrakeep de escritorio (ver el comentario original de HasLiveGameData mas arriba). Pero el
    // daño BASE+prefijo del arma equipada/en inventario SI es un dato 100% estatico, ya cargado
    // (Item.damage del .json de estadisticas + el multiplicador real de prefijo,
    // PrefixEffectCatalog.GetDamageMultiplier) - exactamente el mismo criterio ya aplicado a
    // CristalesVida/Defensa (Encargo1/Encargo2). Por eso este metodo YA NO devuelve 0 fijo -
    // ver el cambio de gate correspondiente en GuideEvaluationEngine (HasInventoryData, no
    // HasLiveGameData).
    //
    // Filtro "es arma": reutiliza PrefixRulesCatalog.VanillaCategories, el MISMO mecanismo real
    // que ya usa PrefixEligibility.For/el panel Editar para decidir que categorias de prefijo
    // mostrar (nunca un filtro inventado a proposito) - cualquier categoria vanilla con
    // Melee/Ranged/Magic/AnyWeapon cuenta como arma (los picos/hachas/martillos vanilla SI
    // entran, exactamente igual que el juego real: pueden llevar prefijo de arma y rodar
    // Sharp/Legendary/etc).
    //
    // Alcance vanilla-only, documentado explicitamente (PrefixEffectCatalog SOLO tiene datos
    // reales de Terraria vanilla, ver su propia cabecera): un arma de Calamity SI entra en el
    // calculo (no se descarta de golpe), pero solo con su daño BASE del catalogo de Calamity, SIN
    // multiplicador de prefijo - una aproximacion honesta y documentada, mejor que dejar todo el
    // tipo de requisito como limite estructural cuando SI hay un numero real que mostrar.
    public int DanoDelMejorArma(out string nombre)
    {
        nombre = "";
        var inventario = Inventario();
        if (inventario == null) return 0;

        int mejor = 0;
        string mejorNombre = "";
        foreach (var item in inventario)
        {
            if (item.IsEmpty) continue;

            int? danoBase = item.IsCalamity
                ? calamityItems?.BySyntheticId(item.Id)?.Stats?.Damage
                : (EsArmaVanilla(item.Id) ? vanillaStats?.Get(item.Id)?.Damage : null);
            if (danoBase is not int baseReal) continue;

            int total = item.IsCalamity
                ? baseReal
                : (int)Math.Round(baseReal * (prefixEffects?.GetDamageMultiplier(item.Prefix.VanillaId) ?? 1.0));

            if (total > mejor)
            {
                mejor = total;
                mejorNombre = NombreDeObjeto(item.Id);
            }
        }
        nombre = mejorNombre;
        return mejor;
    }

    // Melee/Ranged/Magic/AnyWeapon: las 4 categorias vanilla reales que SI admiten prefijo de
    // arma (Accessory queda fuera a proposito - un accesorio nunca tiene daño propio que evaluar
    // aqui). Ver PrefixCategory (PrefixRulesCatalog.cs) para el origen real de cada flag.
    private const PrefixCategory CategoriasDeArma =
        PrefixCategory.Melee | PrefixCategory.Ranged | PrefixCategory.Magic | PrefixCategory.AnyWeapon;

    private bool EsArmaVanilla(int itemId) =>
        prefixRules != null && (prefixRules.VanillaCategories(itemId) & CategoriasDeArma) != PrefixCategory.None;

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
        // CristalesVida y Defensa ya NO pasan por aqui (24-sep-2026 y 25-sep-2026 respectivamente):
        // los dos usan el gate de HasCharacterData/"guide_motive_load_character", igual que
        // VidaMaxima - ver GuideEvaluationEngine y los comentarios de CristalesVida/Defensa mas
        // arriba en este archivo. DanoArma TAMPOCO pasa ya por aqui (Guia Fase A REABIERTA,
        // 26-sep-2026): usa el gate HasInventoryData/"guide_motive_load_character" igual que
        // Objeto/Gancho, y su propio motivo real "arma no encontrada" via
        // GuideEvaluationEngine.EvaluarDanoArma - ver DanoDelMejorArma mas arriba.
        TipoRequisitoGuia.NpcActivo => "guide_motive_active_npc",
        _ => "guide_motive_load_data",
    };

    // ---- Guia v2 (F0, 02-oct-2026): IGuideStateProviderV2 -------------------------------------

    // TODOS los contenedores fusionados (vanilla+Calamity) que CalamityCharacterSync.MergeAll
    // devuelve: inventory, bank..bank4 (hucha, caja fuerte, forja defensiva, bolsa del vacio),
    // miscEquips/miscDyes y loadout{0..2}Items/Social/Dyes. Monedas y municion del .plr no pasan
    // por MergeAll (sin objetos de Calamity) y se suman aparte desde PlrCharacter.
    public int CuantosPosee(int id)
    {
        if (id <= 0) return 0;
        int total = 0;
        if (contexto.MergedContainers != null)
            foreach (var contenedor in contexto.MergedContainers.Values)
                foreach (var item in contenedor)
                    if (!item.IsEmpty && item.Id == id) total += item.Count;
        if (contexto.Character != null)
        {
            foreach (var slot in contexto.Character.Coins) if (slot.Id == id) total += slot.Count;
            foreach (var slot in contexto.Character.Ammo) if (slot.Id == id) total += slot.Count;
        }
        return total;
    }

    // Mismo criterio que Defensa: loadout0Items es "lo que lleva puesto de verdad al guardar".
    public bool LlevaEquipado(int id) => id > 0 && ArmaduraActiva()?.Any(i => !i.IsEmpty && i.Id == id) == true;

    // Mejoras permanentes vanilla del .plr (Player.cs 1.4.4.9, SavePlayer ~linea 55950: extraAccessory,
    // unlockedBiomeTorches y, desde la version 269, ateArtisanBread, usedAegisCrystal, usedAegisFruit,
    // usedArcaneCrystal, usedGalaxyPearl, usedGummyWorm, usedAmbrosia = ExtraUsingFlags[0..6]).
    private static readonly Dictionary<string, Func<Terrakeep.Core.PlrFormat.PlrCharacter, bool>> MejorasVanilla = new()
    {
        ["demonHeart"] = c => c.ExtraAccessory,
        ["torchGod"] = c => c.UnlockedBiomeTorches,
        ["artisanBread"] = c => c.ExtraUsingFlags[0],
        ["aegisCrystal"] = c => c.ExtraUsingFlags[1],
        ["aegisFruit"] = c => c.ExtraUsingFlags[2],
        ["arcaneCrystal"] = c => c.ExtraUsingFlags[3],
        ["galaxyPearl"] = c => c.ExtraUsingFlags[4],
        ["gummyWorm"] = c => c.ExtraUsingFlags[5],
        ["ambrosia"] = c => c.ExtraUsingFlags[6],
    };

    public bool MejoraConocida(string clave) =>
        MejorasVanilla.ContainsKey(clave) || CalamityEstadoGuardado.MejorasConocidas.Contains(clave);

    public bool? MejoraPermanente(string clave)
    {
        if (MejorasVanilla.TryGetValue(clave, out var leer))
            return contexto.Character != null ? leer(contexto.Character) : null;
        if (CalamityEstadoGuardado.MejorasConocidas.Contains(clave))
            return contexto.CalamityPlayerBoosts?.Contains(clave);
        return null;
    }

    public bool EstadoMundoConocido(string clave) =>
        clave == "mundoCarmesi" || CalamityEstadoGuardado.EstadosMundoConocidos.Contains(clave);

    public bool? EstadoMundo(string clave)
    {
        if (clave == "mundoCarmesi") return contexto.World?.Header.IsCrimson;
        if (CalamityEstadoGuardado.EstadosMundoConocidos.Contains(clave))
            return contexto.CalamityWorldState?.Contains(clave);
        return null;
    }

    // Player.cs: cada Fruta de vida suma 5 por encima de 400 (tope 500 = 20 frutas).
    public int FrutasVida => Math.Clamp((VidaMaxima - 400) / 5, 0, 20);
    public int ManaMaxima => contexto.Character?.ManaMax ?? 0;

    public ModoPartida? Modo => contexto.World == null
        ? null
        : new ModoPartida(contexto.World.Header.GameMode,
            contexto.CalamityWorldState?.Contains("revenge"),
            contexto.CalamityWorldState?.Contains("death"));

    private GameItem[]? Inventario() =>
        contexto.MergedContainers != null && contexto.MergedContainers.TryGetValue("inventory", out var inv) ? inv : null;
}
