namespace Terrakeep.Core.WorldGen;

// Las 8 semillas secretas reales de Terraria (1.4.4/Journey's End) - efectos deterministas y
// documentados (terraria.wiki.gg), a diferencia del resto de la generacion (procedural, no
// predecible sin reimplementar WorldGen.cs). Zenith combina TODAS las demas a la vez (incluida
// DrunkWorld - ver el comentario real de SpecialSeedCatalog.Detect).
public enum SpecialSeedEffect
{
    NoTraps, NotTheBees, ForTheWorthy, DontDigUp, CelebrationMk10, TheConstant, DrunkWorld, Zenith,
}

public static class SpecialSeedCatalog
{
    // Logica de coincidencia PORTADA 1:1 (mismas grafias aceptadas, mismo ToLower) desde
    // Terraria.GameContent.UI.States.UIWorldCreation.ProcessSpecialWorldSeeds, decompilado real
    // de tModLoader (Terrakeep.App/reference no trae este ensamblado - transcrito directamente
    // del codigo fuente leido esta misma sesion, no adivinado). "get fixed boi"/"getfixedboi"
    // activa TODAS las demas banderas a la vez (WorldGen real: noTrapsWorldGen=true,
    // notTheBees=true, getGoodWorldGen=true, tempTenthAnniversaryWorldGen=true,
    // dontStarveWorldGen=true, tempRemixWorldGen=true, everythingWorldGen=true). DrunkWorld tiene
    // ademas su PROPIO disparador numerico real (WorldGen.GenerateWorld: `if (seed == 5162020 ||
    // everythingWorldGen) drunkWorldGen = true`) - no pasa por ProcessSpecialWorldSeeds en
    // absoluto en el juego real, se comprueba aqui aparte por el mismo motivo.
    public static IReadOnlyList<SpecialSeedEffect> Detect(string? seedText)
    {
        string s = (seedText ?? string.Empty).Trim().ToLowerInvariant();
        bool everything = s is "get fixed boi" or "getfixedboi";

        var effects = new List<SpecialSeedEffect>();
        if (everything || s is "no traps" or "notraps") effects.Add(SpecialSeedEffect.NoTraps);
        if (everything || s is "not the bees" or "not the bees!" or "notthebees") effects.Add(SpecialSeedEffect.NotTheBees);
        if (everything || s is "for the worthy" or "fortheworthy") effects.Add(SpecialSeedEffect.ForTheWorthy);
        if (everything || s is "don't dig up" or "dont dig up" or "dontdigup") effects.Add(SpecialSeedEffect.DontDigUp);
        if (everything || s == "celebrationmk10") effects.Add(SpecialSeedEffect.CelebrationMk10);
        if (everything || s is "constant" or "theconstant" or "the constant" or "eye4aneye" or "eyeforaneye") effects.Add(SpecialSeedEffect.TheConstant);
        if (everything || s == "5162020") effects.Add(SpecialSeedEffect.DrunkWorld);
        if (everything) effects.Add(SpecialSeedEffect.Zenith);
        return effects;
    }
}
