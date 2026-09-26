using Terrakeep.Core.Data;
using Terrakeep.Core.Layout;

namespace Terrakeep.Core.Tests.Layout;

// GapAnalysis ParidadPersonaje (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514) -
// segunda fase del patron de 2 agentes: arquitecto-keep diseño y VERIFICO por completo esta
// arquitectura (incluidas las cifras exactas del caso oracle de abajo) antes de este arreglo -
// este fichero aplica ese diseño tal cual, no reinvestiga la causa.
//
// Caso oracle real: "Eye Bone" (item 5098) -> ChesterPet (proyectil 960 real,
// Terraria/ID/ProjectileID.cs:2372 "public const short ChesterPet = 960;") - el mismo personaje
// real del usuario (Terrariano.plr) que reporto el bug ("forma dorada/amarilla redondeada bajo
// los pies"). Datos reales de Assets/pet_animations.json (item "5098": shoot=960, totalFrames=20,
// offsetX=4, offsetY=0) + Assets/pets/960.png real (46x960 pixeles, 20 fotogramas verticales =
// 48px de alto por fotograma).
public sealed class PlayerPetPreviewLayoutTests
{
    private const double ChesterOffsetX = 4.0;
    private const double ChesterOffsetY = 0.0;
    private const double ChesterFrameWidth = 46.0; // Assets/pets/960.png real: 46px de ancho
    private const double ChesterFrameHeight = 48.0; // 960px de alto / 20 fotogramas reales

    [Fact]
    public void PlayerHitboxOrigin_ConMascota_CentraElHitboxYRetranquea10EnX()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);

        // (59*0.5 - 20*0.5) - 10 = (29.5-10)-10 = 9.5 ; (58*0.5 - 42*0.5) = 29-21 = 8
        Assert.Equal(9.5, origen.X, precision: 6);
        Assert.Equal(8.0, origen.Y, precision: 6);
    }

    [Fact]
    public void PlayerHitboxOrigin_SinMascota_NoRetranquea()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: false);

        Assert.Equal(19.5, origen.X, precision: 6);
        Assert.Equal(8.0, origen.Y, precision: 6);
    }

    [Fact]
    public void PlayerSpriteBounds_ConMascota_CoincideConElCasoOracleChester_MenosLaMitadNativoAMasLaMitad()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var sprite = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);

        // Cifras exactas ya verificadas por la investigacion (informe arquitecto-keep): sprite
        // del jugador en [-0.5, 39.5] en X.
        Assert.Equal(-0.5, sprite.Left, precision: 6);
        Assert.Equal(39.5, sprite.Right, precision: 6);
        Assert.Equal(40.0, sprite.Width, precision: 6);
        // Y: 8 + 42 - 56 + 4 = -2 ; bottom = -2+56 = 54
        Assert.Equal(-2.0, sprite.Top, precision: 6);
        Assert.Equal(54.0, sprite.Bottom, precision: 6);
    }

    [Fact]
    public void PetBounds_Chester_CoincideConElCasoOracle_33_5A79_5()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var pet = PlayerPetPreviewLayout.PetBounds(origen, ChesterOffsetX, ChesterOffsetY, ChesterFrameWidth, ChesterFrameHeight);

        // Cifras exactas ya verificadas por la investigacion: Chester en [33.5, 79.5] en X.
        Assert.Equal(33.5, pet.Left, precision: 6);
        Assert.Equal(79.5, pet.Right, precision: 6);
        // Y: baseY = 8+42-48=2 ; +offsetY(0) = 2 ; bottom = 2+48=50
        Assert.Equal(2.0, pet.Top, precision: 6);
        Assert.Equal(50.0, pet.Bottom, precision: 6);
    }

    [Fact]
    public void PetBounds_SobrecargaConPetAnimationEntry_DaElMismoResultadoQueLosDoublesSueltos()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var entry = new PetAnimationEntry
        {
            Shoot = 960,
            SelStart = 5,
            SelCount = 7,
            SelDelay = 4,
            TotalFrames = 20,
            OffsetX = ChesterOffsetX,
            OffsetY = ChesterOffsetY,
        };

        var viaEntry = PlayerPetPreviewLayout.PetBounds(origen, entry, ChesterFrameWidth, ChesterFrameHeight);
        var viaDoubles = PlayerPetPreviewLayout.PetBounds(origen, ChesterOffsetX, ChesterOffsetY, ChesterFrameWidth, ChesterFrameHeight);

        Assert.Equal(viaDoubles, viaEntry);
    }

    [Fact]
    public void CompositeBounds_Chester_AnchoRealCompuestoDe80Unidades()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var sprite = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);
        var pet = PlayerPetPreviewLayout.PetBounds(origen, ChesterOffsetX, ChesterOffsetY, ChesterFrameWidth, ChesterFrameHeight);

        var compuesto = PlayerPetPreviewLayout.CompositeBounds(sprite, pet);

        // Cifra exacta ya verificada por la investigacion: bounding box compuesto real de 80
        // unidades de ancho (-0.5 a 79.5) - Chester YA NO deberia recortarse ni parecer un
        // "suelo"/plataforma bajo los pies dentro de una columna que reserve al menos este ancho.
        Assert.Equal(-0.5, compuesto.Left, precision: 6);
        Assert.Equal(79.5, compuesto.Right, precision: 6);
        Assert.Equal(80.0, compuesto.Width, precision: 6);
    }

    [Fact]
    public void CompositeBounds_SinMascota_DevuelveSoloElSpriteDelJugador()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: false);
        var sprite = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);

        var compuesto = PlayerPetPreviewLayout.CompositeBounds(sprite, petBounds: null);

        Assert.Equal(sprite, compuesto);
    }

    // Migrado de Terrakeep.App.ViewModels.Tests/PetPositionConvertersTests.cs (el converter que
    // este motor sustituye) - cubre el mismo hallazgo real (offset neto de 20 nativos, no 10,
    // entre el borde del sprite YA RETRANQUEADO y el ancla de la mascota) desde el motor puro en
    // vez de desde el converter WPF ya retirado.
    [Fact]
    public void PetBounds_SinOffsetPropio_QuedaA20NativosDelBordeIzquierdoDelSpriteYaRetranqueado()
    {
        var origen = PlayerPetPreviewLayout.PlayerHitboxOrigin(hasPet: true);
        var sprite = PlayerPetPreviewLayout.PlayerSpriteBounds(origen);
        var pet = PlayerPetPreviewLayout.PetBounds(origen, offsetX: 0, offsetY: 0, petFrameWidth: 10, petFrameHeight: 10);

        // sprite.Left = -0.5 ; pet.Left (sin offset propio) = hitboxOrigin.X + 20 = 29.5.
        // Diferencia real = 30 (el mismo "30 nativos desde el borde YA retranqueado" que la
        // investigacion de PetPositionConverters.cs re-derivo campo a campo del decompilado).
        Assert.Equal(30.0, pet.Left - sprite.Left, precision: 6);
    }

    [Fact]
    public void ReserveColumnWidth_EscalaLinealmenteConCanvasScale()
    {
        Assert.Equal(PlayerPetPreviewLayout.ReserveColumnWidthNative * 1.3, PlayerPetPreviewLayout.ReserveColumnWidth(1.3), precision: 6);
        Assert.Equal(PlayerPetPreviewLayout.ReserveColumnWidthNative * 2.6, PlayerPetPreviewLayout.ReserveColumnWidth(2.6), precision: 6);
    }
}
