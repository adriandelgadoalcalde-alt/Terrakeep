using System.IO;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// GuiaCalamity Encargo B (25-sep-2026, revision-correccion-integral-familia-Keep, handoff
// e5eaea9e-c261-4199-8e7d-060b6054f58d): espejo directo de BossIconResolverTests.cs (Encargo4,
// vanilla) pero para los jefes de CalamityMod - confirma que CalamityBossIconResolver resuelve
// sprite real (y el fichero existe de verdad en disco) para los 29 pids "CalamityMod/InternalName"
// reales usados en guia_progresion.json (campo "jefeMod" de cada paso + "jefeFinalMod"/
// "jefeFinalModCarmesi" de cada tramo, confirmado iterando el JSON real - ver comentario de
// cabecera de scripts/extraer-sprites-jefes-calamity.js). Incluye explicitamente los 3 casos de
// mapeo manual (Cryogen, Dragonfolly, SupremeCalamitas) que NO siguen la convencion directa
// "NPCs/<InternalName>/<InternalName>_Head_Boss.rawimg".
public sealed class CalamityBossIconResolverTests
{
    // Los 29 pids reales - ver comentario de cabecera de scripts/extraer-sprites-jefes-calamity.js
    // para como se obtuvo esta lista del JSON real.
    [Theory]
    [InlineData("CalamityMod/AquaticScourgeHead")]
    [InlineData("CalamityMod/AresBody")]
    [InlineData("CalamityMod/AstrumAureus")]
    [InlineData("CalamityMod/AstrumDeusHead")]
    [InlineData("CalamityMod/BrimstoneElemental")]
    [InlineData("CalamityMod/CalamitasClone")]
    [InlineData("CalamityMod/CeaselessVoid")]
    [InlineData("CalamityMod/Crabulon")]
    [InlineData("CalamityMod/CragmawMire")]
    [InlineData("CalamityMod/Cryogen")]                    // mapeo manual: Cryogen_Phase1_Head_Boss
    [InlineData("CalamityMod/DesertScourgeHead")]
    [InlineData("CalamityMod/DevourerofGodsHead")]
    [InlineData("CalamityMod/Dragonfolly")]                 // mapeo manual: Bumblebirb/Birb_Head_Boss
    [InlineData("CalamityMod/GiantClam")]
    [InlineData("CalamityMod/GreatSandShark")]
    [InlineData("CalamityMod/HiveMind")]                    // solo jefeFinalMod de tramo, sin paso propio
    [InlineData("CalamityMod/OldDuke")]
    [InlineData("CalamityMod/PerforatorHive")]               // solo jefeFinalModCarmesi de tramo
    [InlineData("CalamityMod/PlaguebringerGoliath")]
    [InlineData("CalamityMod/Polterghast")]
    [InlineData("CalamityMod/PrimordialWyrmHead")]
    [InlineData("CalamityMod/ProfanedGuardianCommander")]
    [InlineData("CalamityMod/Providence")]
    [InlineData("CalamityMod/RavagerBody")]
    [InlineData("CalamityMod/Signus")]
    [InlineData("CalamityMod/SlimeGodCore")]
    [InlineData("CalamityMod/StormWeaverHead")]
    [InlineData("CalamityMod/SupremeCalamitas")]             // mapeo manual: HoodedHeadIcon (sin sufijo _Head_Boss)
    [InlineData("CalamityMod/Yharon")]
    public void LosJefesRealesDeCalamityDeLaGuiaTienenIconoDeCabeza(string pid)
    {
        string? ruta = CalamityBossIconResolver.GetIconPath(pid);
        Assert.NotNull(ruta);

        // "pack://siteoforigin:,,,/Assets/calamity_boss_icons/{InternalName}.png" -> ruta real en
        // disco, mismo patron de reconstruccion ya usado por BossIconResolverTests/
        // PruebasGuiaYServidor.EjecutarGuiaReal (new Uri(...).LocalPath NO resuelve este esquema
        // propio de WPF).
        string relativo = ruta!.Replace("pack://siteoforigin:,,,/", "").Replace('/', Path.DirectorySeparatorChar);
        string real = Path.Combine(AppContext.BaseDirectory, relativo);
        Assert.True(File.Exists(real), $"No existe el fichero real: {real}");
    }

    [Fact]
    public void UnPidQueNoEstaEnElCatalogo_NoInventaUnSprite()
    {
        Assert.Null(CalamityBossIconResolver.GetIconPath(null));
        Assert.Null(CalamityBossIconResolver.GetIconPath(""));
        Assert.Null(CalamityBossIconResolver.GetIconPath("CalamityMod/EsteBossNoExiste"));
        // Sin barra real (ni mod ni internal name reconocibles) tampoco debe reventar ni inventar.
        Assert.Null(CalamityBossIconResolver.GetIconPath("SinBarra"));
        Assert.Null(CalamityBossIconResolver.GetIconPath("CalamityMod/"));
    }
}
