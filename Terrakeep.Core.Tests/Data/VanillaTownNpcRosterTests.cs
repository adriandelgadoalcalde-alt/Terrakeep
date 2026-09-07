using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class VanillaTownNpcRosterTests
{
    [Fact]
    public void HasExpectedCountAndNoDuplicates()
    {
        // H6-08/H6-10 (sexta auditoria de Opus): 40 reales - 39 con townNPC=true en el propio
        // NPC.cs decompilado (incluidas las "mascotas de pueblo" 1.4.4, Gato/Perro/Conejo/
        // Slimes) mas SkeletonMerchant (453, friendly=true nada mas, evento Old One's Army,
        // ya estaba en el roster original de 27).
        Assert.Equal(40, VanillaTownNpcRoster.Ids.Count);
        Assert.Equal(VanillaTownNpcRoster.Ids.Count, VanillaTownNpcRoster.Ids.Distinct().Count());
    }

    [Fact]
    public void ContainsKnownTownNpcs()
    {
        // Guia (17) y Enfermera (124) - dos NPCs de pueblo bien conocidos, citados tal cual en
        // overrides.js (Terrasavr-Calamity-Beta) como referencia de que el id es el correcto.
        Assert.Contains(17, VanillaTownNpcRoster.Ids);
        Assert.Contains(124, VanillaTownNpcRoster.Ids);
    }

    [Fact]
    public void ContainsElHallazgoRealDeOpus_TaxCollector441()
    {
        // H6-08 (sexta auditoria de Opus): hallazgo explicito del informe - "falta el NPC 441
        // en VanillaTownNpcRoster.cs" (Recaudador de Impuestos, townNPC=true real).
        Assert.Contains(441, VanillaTownNpcRoster.Ids);
    }

    [Fact]
    public void ContainsLasMascotasDePueblo1_4_4()
    {
        // H6-08 (sexta auditoria de Opus): justo el tipo de NPC que el usuario confundia con
        // mascotas de verdad en el mapa (TownCat/TownDog/TownBunny/TownSlime*) - townNPC=true
        // real, confirmado en "else if (type == 637 || type == 638)" y el bloque de slimes de
        // una sola linea con 7 ids reales.
        int[] mascotasDePueblo = [637, 638, 656, 670, 678, 679, 680, 681, 682, 683, 684];
        foreach (int id in mascotasDePueblo)
            Assert.Contains(id, VanillaTownNpcRoster.Ids);
    }
}
