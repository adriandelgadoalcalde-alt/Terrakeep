using Terrakeep.Core.Nbt;

namespace Terrakeep.Core.Calamity;

// Guia v2 (F0, 02-oct-2026): estado de Calamity que la guia necesita y que NO son banderas de jefe
// (esas ya las lee TwldReader.ReadCalamityDownedFlags). Formato confirmado contra el decompilado
// real de Calamity 2.2.4 (Downloads\Keep\tModLoader-Decompiled\CalamityMod-2.2.4\, mismas lineas
// en el de 2.2.2):
//
//   .twld -> modData[] {mod:"CalamityMod", name:"MiscWorldStateSystem", data:{...}}
//     data["downed"]: List<string> con "revenge", "death", "TalkedToDraedon", "abyssSide",
//       "acidRain", "HasGeneratedLuminitePlanetoids", "IsWorldAfterDraedonUpdate"... y, via
//       RecipeUnlockHandler.Save, "HasFoundSunkenSeaSchematic", "HasFoundPlanetoidSchematic",
//       "HasFoundJungleSchematic", "HasFoundHellSchematic", "HasFoundIceSchematic",
//       "HasUnlockedT1ArsenalRecipes".."T5" y los "HasFound<Accesorio>" de los laboratorios.
//       (MiscWorldStateSystem.SaveWorldData / RecipeUnlockHandler.Save)
//     data["SunkenSeaLabCenter"|"PlanetoidLabCenter"|"JungleLabCenter"|"HellLabCenter"|
//       "IceLabCenter"|"CavernLabCenter"]: Vector2 en COORDENADAS DE MUNDO (pixeles, 16 por
//       casilla; DraedonStructures.cs: placementPoint.ToWorldCoordinates() + ...), serializado por
//       Vector2TagSerializer como {x: float, y: float}.
//     data["abyssChasmBottom"|"SulphSeaYStart"|"AstralYStart"]: int (casillas).
//
//   .tplr -> modData[] {mod:"CalamityMod", name:"CalamityPlayer", data:{boost: List<string>}}
//     boost: "miracleFruit", "bloodOrange", "elderBerry", "dragonFruit", "etherealCore",
//       "phantomHeart", "cometShard", "revJam", "extraAccessoryML", "rageOne".."adrenalineThree",
//       "HasTalkedAtCodebreaker", "HasCraftedDraedonsForge"... (CalamityPlayer.SaveData)
//
// Nunca lanza por un arbol de formato inesperado: lo que no esta, no aporta (mismo criterio que
// TwldReader). El try/catch de E/S vive en el llamador.
public static class CalamityEstadoGuardado
{
    public sealed record EstadoMundo(IReadOnlySet<string> Claves, IReadOnlyDictionary<string, (int X, int Y)> PuntosEnCasillas,
        IReadOnlyDictionary<string, int> Enteros);

    public static EstadoMundo LeerMundo(byte[] twldBytes)
    {
        var (_, root) = TplrFile.Read(twldBytes);
        return LeerMundo(root);
    }

    public static EstadoMundo LeerMundo(NbtCompound root)
    {
        var claves = new HashSet<string>();
        var puntos = new Dictionary<string, (int X, int Y)>();
        var enteros = new Dictionary<string, int>();

        var data = DatosDeMod(root, "MiscWorldStateSystem");
        if (data != null)
        {
            if (data.Get("downed") is NbtList lista)
                foreach (var t in lista.Items)
                    if (t is NbtString s) claves.Add(s.Value);

            foreach (var (nombre, tag) in data.Fields)
            {
                if (tag is NbtCompound v && v.Get("x") is NbtFloat x && v.Get("y") is NbtFloat y)
                {
                    // (0,0) = "no generado" (el campo existe pero el laboratorio no se coloco).
                    if (x.Value > 0 || y.Value > 0)
                        puntos[nombre] = ((int)(x.Value / 16f), (int)(y.Value / 16f));
                }
                else if (tag is NbtInt i)
                {
                    enteros[nombre] = i.Value;
                }
            }
        }

        return new EstadoMundo(claves, puntos, enteros);
    }

    /// <summary>Mejoras permanentes guardadas por CalamityPlayer en el .tplr ("boost").</summary>
    public static IReadOnlySet<string> LeerMejorasJugador(NbtCompound? tplrRoot)
    {
        var resultado = new HashSet<string>();
        if (tplrRoot == null) return resultado;
        var data = DatosDeMod(tplrRoot, "CalamityPlayer");
        if (data?.Get("boost") is NbtList lista)
            foreach (var t in lista.Items)
                if (t is NbtString s) resultado.Add(s.Value);
        return resultado;
    }

    private static NbtCompound? DatosDeMod(NbtCompound root, string nombre)
    {
        if (root.Get("modData") is not NbtList modData) return null;
        foreach (var item in modData.Items)
        {
            if (item is not NbtCompound entry) continue;
            if ((entry.Get("mod") as NbtString)?.Value != "CalamityMod") continue;
            if ((entry.Get("name") as NbtString)?.Value != nombre) continue;
            return entry.Get("data") as NbtCompound;
        }
        return null;
    }

    /// <summary>Claves de mejora del jugador que la guia sabe preguntar (vocabulario cerrado).</summary>
    public static readonly IReadOnlySet<string> MejorasConocidas = new HashSet<string>
    {
        "miracleFruit", "bloodOrange", "elderBerry", "dragonFruit", "etherealCore", "phantomHeart",
        "cometShard", "revJam", "extraAccessoryML", "rageOne", "rageTwo", "rageThree",
        "adrenalineOne", "adrenalineTwo", "adrenalineThree", "HasTalkedAtCodebreaker",
        "HasCraftedDraedonsForge",
    };

    /// <summary>Claves de estado de mundo que la guia sabe preguntar (vocabulario cerrado).</summary>
    public static readonly IReadOnlySet<string> EstadosMundoConocidos = new HashSet<string>
    {
        "revenge", "death", "TalkedToDraedon", "abyssSide", "HasGeneratedLuminitePlanetoids",
        "HasFoundSunkenSeaSchematic", "HasFoundPlanetoidSchematic", "HasFoundJungleSchematic",
        "HasFoundHellSchematic", "HasFoundIceSchematic",
        "HasUnlockedT1ArsenalRecipes", "HasUnlockedT2ArsenalRecipes", "HasUnlockedT3ArsenalRecipes",
        "HasUnlockedT4ArsenalRecipes", "HasUnlockedT5ArsenalRecipes",
    };
}
