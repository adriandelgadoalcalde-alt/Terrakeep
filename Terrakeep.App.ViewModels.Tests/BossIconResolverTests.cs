using System.IO;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// Guia Encargo4 (25-sep-2026): "el banner objetivo actual y el arbol de la Guia deben mostrar el
// sprite real del jefe cuando el hito lo requiera" - antes de esta ronda NpcIconResolver solo
// cubria los NPCs de VanillaTownNpcRoster (NPCs de pueblo), nunca jefes, asi que
// GuidePasoViewModel.IconPath nunca resolvia nada para el caso "jefe" (paso.Jefe!=0). Cierra ese
// hueco de cobertura: confirma que BossIconResolver resuelve sprite real (y el fichero existe de
// verdad en disco) para los 23 NPC types de jefe/segmento final REALMENTE usados en
// guia_progresion.json (campo "jefe" de cada paso + "jefeFinal" de cada tramo, confirmado
// iterando el JSON real, no una lista de memoria).
public sealed class BossIconResolverTests
{
    // Los 23 types reales - ver comentario de cabecera de scripts/extraer-sprites-jefes-vanilla.js
    // para como se obtuvo esta lista del JSON real.
    [Theory]
    [InlineData(4)]   // EyeofCthulhu - sprite en tira de animacion (caso explicito pedido)
    [InlineData(13)]  // EaterofWorldsHead
    [InlineData(35)]  // SkeletronHead
    [InlineData(50)]  // KingSlime - tambien tira de animacion
    [InlineData(113)] // WallofFlesh
    [InlineData(134)] // TheDestroyer
    [InlineData(222)] // QueenBee
    [InlineData(245)] // Golem
    [InlineData(262)] // Plantera
    [InlineData(325)] // MourningWood
    [InlineData(327)] // Pumpking
    [InlineData(344)] // Everscream
    [InlineData(345)] // IceQueen
    [InlineData(346)] // SantaNK1
    [InlineData(370)] // DukeFishron
    [InlineData(398)] // MoonLordCore
    [InlineData(439)] // CultistBoss
    [InlineData(493)] // LunarTowerStardust
    [InlineData(551)] // DD2Betsy
    [InlineData(618)] // BloodNautilus (Dreadnautilus en el juego)
    [InlineData(636)] // HallowBoss (Emperatriz de la Luz)
    [InlineData(657)] // QueenSlimeBoss - hoja en rejilla (varios frames por fila y columna)
    [InlineData(668)] // Deerclops - hoja en rejilla (varios frames por fila y columna)
    public void LosJefesRealesDeLaGuiaTienenSpriteDeCuerpoEntero(int npcType)
    {
        string? ruta = BossIconResolver.GetIconPath(npcType);
        Assert.NotNull(ruta);

        // "pack://siteoforigin:,,,/Assets/boss_icons/{id}.png" -> ruta real en disco, mismo
        // patron de reconstruccion ya usado por PruebasGuiaYServidor.EjecutarGuiaReal (new
        // Uri(...).LocalPath NO resuelve este esquema propio de WPF).
        string relativo = ruta!.Replace("pack://siteoforigin:,,,/", "").Replace('/', Path.DirectorySeparatorChar);
        string real = Path.Combine(AppContext.BaseDirectory, relativo);
        Assert.True(File.Exists(real), $"No existe el fichero real: {real}");
    }

    [Fact]
    public void UnJefeQueNoEstaEnElCatalogo_NoInventaUnSprite()
    {
        // HiveMindOPerforator (Calamity, unico jefeFinalMod del catalogo) no tiene NPC type
        // vanilla ni .xnb real - 0 es ademas el "sin jefe" real de PasoGuia.Jefe. Ninguno de los
        // dos debe devolver un sprite inventado.
        Assert.Null(BossIconResolver.GetIconPath(0));
        Assert.Null(BossIconResolver.GetIconPath(999999));
    }
}
