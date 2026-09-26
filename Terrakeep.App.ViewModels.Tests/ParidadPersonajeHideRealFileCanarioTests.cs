using System.IO;
using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Canario real (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514, GapAnalysis BugH)
// contra los 2 .plr REALES del usuario (Documents\My Games\Terraria\Players\, NO tModLoader\
// Players\) - solo lectura, nunca se modifican. Usa el lector/resolver de PRODUCCION completo
// (Terrakeep.Core.PlrFormat.PlrFile + Terrakeep.App.Services.EquipmentAppearanceResolver), no un
// PlrCharacter fabricado a mano.
//
// Datos reales confirmados con TEMP_DiagnosticoHideRealTests (26-sep-2026, eliminado tras
// verificar - no forma parte del working set final):
//   Terrariano.plr / Eldelgas.plr: Version=326, HideVisual1=0xF8, HideVisual2=0x01 ->
//   ResolveActiveHide() correcto = [F,F,F,T,T,T,T,T,T,F] (indices 3..8 ocultos, 9 libre).
//   Loadouts[0..2].Hide = 10x false en LOS TRES - exactamente el caso que el bug viejo
//   (Loadouts[CurrentLoadout].Hide) leia como "nada oculto", perdiendo el oculto real.
//
//   Terrariano.PrimaryLoadout.Items[3..8] (funcional, TODOS ocultos por Hide):
//     3=Terraspark Boots(5000,sh24) 4=Ankh Shield(1613,s4) 5=Fire Gauntlet(1343,ho6/hf1)
//     6=Celestial Starboard(4954,wg45) 7=Shield of Cthulhu(3097,s5) 8=Worm Scarf(3224,n8)
//   Terrariano.PrimaryLoadout.Social[5..7] (vanidad, NUNCA respeta Hide):
//     5=Bundle of Balloons(1164,bl3->Balloon normal, balloonSlot!=18) 6=PDA(3123, sin slot de
//     accesorio real, invisible por diseno) 7=Warrior Emblem(490, idem, invisible por diseno).
//
//   Eldelgas.PrimaryLoadout.Items[3..8] (funcional, TODOS ocultos por Hide):
//     3=Berserker's Glove(3992,ho20/hf12) 4=Ankh Shield(1613,s4) 5=Fire Gauntlet(1343,ho6/hf1)
//     6=Celestial Shell(3110, sin slot de accesorio real) 7=Soaring Insignia(4989, idem)
//     8=Celestial Starboard(4954,wg45)
//   Eldelgas.PrimaryLoadout.Social[3..9]: TODO vacio (id=0) - sin vanidad de respaldo, cualquier
//   sprite funcional visible es FAIL directo.
//
// Este test debe FALLAR contra el codigo viejo (if (Loadouts.Length > 0) return
// Loadouts[CurrentLoadout]?.Hide - un array de 10 false en ambos .plr, "nada oculto") y pasar con
// el arreglo (ResolveActiveHide ya solo mira HideVisual1/HideVisual2) - confirmado con git stash.
public sealed class ParidadPersonajeHideRealFileCanarioTests
{
    private const string PlayersDir = @"C:\Users\adrian\Documents\My Games\Terraria\Players";

    private static readonly CharacterFileService Service = new();
    private static readonly EquipmentAppearanceResolver EquipAppearance = Service.EquipmentAppearance;

    private static EquippedAccessories ResolveReal(string fileName)
    {
        string path = Path.Combine(PlayersDir, fileName);
        var character = PlrFile.Read(File.ReadAllBytes(path));
        var hide = character.ResolveActiveHide();
        return EquipAppearance.ResolveAccessories(character.PrimaryLoadout, hide, character.ExtraAccessory, character.Loadouts);
    }

    [Fact]
    public void Terrariano_FuncionalesOcultosPorHideVisual_VanidadDeGlobosSigueVisible()
    {
        if (!File.Exists(Path.Combine(PlayersDir, "Terrariano.plr"))) return; // no disponible en esta maquina

        var accessories = ResolveReal("Terrariano.plr");

        // Los 6 funcionales reales de slots 3..8 tienen que estar OCULTOS (ResolveActiveHide
        // corregido: HideVisual1=0xF8 marca los indices 3..7, HideVisual2=0x01 no aplica a estos
        // 6 - ver el mapeo de arriba). Con el bug viejo (Loadouts[0].Hide = 10x false) los 6
        // aparecerian sueltos.
        Assert.Null(accessories.ShoesFile);   // Terraspark Boots (5000)
        Assert.Null(accessories.ShieldFile);  // Ankh Shield (1613) / Shield of Cthulhu (3097)
        Assert.Null(accessories.HandOnFile);  // Fire Gauntlet (1343)
        Assert.Null(accessories.HandOffFile); // Fire Gauntlet (1343)
        Assert.Null(accessories.WingFile);    // Celestial Starboard (4954)
        Assert.Null(accessories.NeckFile);    // Worm Scarf (3224)

        // La vanidad (Social[5] = Bundle of Balloons/Puñado de globos) NUNCA respeta Hide - tiene
        // que seguir visible igual con el arreglo, confirma que Hide no se filtra al canal
        // equivocado.
        Assert.NotNull(accessories.BalloonFile);
        Assert.Null(accessories.BalloonFrontFile); // balloonSlot=3 != 18 (RoyalScepter): rama normal
    }

    [Fact]
    public void Eldelgas_FuncionalesOcultosPorHideVisual_SinVanidadDeRespaldo_CualquierSpriteVisibleEsFail()
    {
        if (!File.Exists(Path.Combine(PlayersDir, "Eldelgas.plr"))) return; // no disponible en esta maquina

        var accessories = ResolveReal("Eldelgas.plr");

        // Social[3..9] de este personaje esta vacio de verdad (confirmado en el diagnostico de
        // cabecera) - por eso TODOS los campos visuales de accesorio deben quedar null. Cualquier
        // valor no-null aqui es un sprite funcional que se coló pese al Hide activo real -
        // exactamente el bug viejo (Loadouts[CurrentLoadout].Hide = 10x false = "nada oculto").
        Assert.Null(accessories.WaistFile);
        Assert.Null(accessories.NeckFile);
        Assert.Null(accessories.HandOnFile);   // Berserker's Glove (3992) / Fire Gauntlet (1343)
        Assert.Null(accessories.HandOffFile);  // Berserker's Glove (3992) / Fire Gauntlet (1343)
        Assert.Null(accessories.BackFile);
        Assert.Null(accessories.ShieldFile);   // Ankh Shield (1613)
        Assert.Null(accessories.FaceFile);
        Assert.Null(accessories.ShoesFile);
        Assert.Null(accessories.BalloonFile);
        Assert.Null(accessories.BalloonFrontFile);
        Assert.Null(accessories.BackpackFile);
        Assert.Null(accessories.TailFile);
        Assert.Null(accessories.FrontFile);
        Assert.Null(accessories.WingFile);     // Celestial Starboard (4954)
        Assert.Null(accessories.BeardFile);
        Assert.Null(accessories.FaceHeadFile);
        Assert.Null(accessories.FaceMaskFile);
        Assert.Null(accessories.FaceFlowerFile);
        Assert.Null(accessories.UnicornHornFile);
        Assert.Null(accessories.AngelHaloFile);
        Assert.Null(accessories.Yoraiz0rDarknessFile);
        Assert.Null(accessories.CoatFile);
        Assert.Null(accessories.FloatingTubeFile);
    }
}
