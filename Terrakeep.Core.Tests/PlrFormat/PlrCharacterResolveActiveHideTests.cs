using Terrakeep.Core.PlrFormat;

namespace Terrakeep.Core.Tests.PlrFormat;

// ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugH - HideVisual1/HideVisual2 (formato
// ANTIGUO pre-Loadout, version<269) YA los parseaba PlrBodySerializer.Read de forma fiel desde
// el principio, pero esa informacion nunca llegaba a ResolveAccessories/VisiblePlayerState (que
// solo miraba Loadouts[CurrentLoadout].Hide, SIEMPRE null en un personaje de formato antiguo
// porque Loadouts esta vacio - ver PlrBodySerializer.Read, "character.Loadouts = new
// PlrLoadout[3]" solo dentro de "if (version >= 269)"). PlrCharacter.ResolveActiveHide() conecta
// los dos: mismo array de 10 bits real, sea cual sea el formato del .plr.
public sealed class PlrCharacterResolveActiveHideTests
{
    private static PlrCharacter Character() => new()
    {
        Version = 279,
        Name = "Test",
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
    };

    [Fact]
    public void FormatoAntiguo_SinLoadouts_ConstruyeElHideDesdeHideVisual1Y2_CierraElHuecoDeDatosRealDelEncargoH()
    {
        // Personaje de formato antiguo real (version<269): Loadouts SIEMPRE vacio (ver el
        // comentario de cabecera). HideVisual1 bit 3 (indice 3, mismo mapeo bit a bit que
        // Player.cs:55831-55841 real: bitsByte[i] = hideVisibleAccessory[i] para i=0..7) marca
        // oculto el hueco de accesorio 3 (Waist en las pruebas de EquipmentAppearanceResolver).
        var character = Character();
        character.Version = 200; // < 269, Loadouts vacio de verdad (no simulado)
        character.HideVisual1 = 1 << 3; // bit 3 activo, resto a cero

        var hide = character.ResolveActiveHide();

        Assert.NotNull(hide);
        Assert.Equal(10, hide!.Length);
        Assert.True(hide[3]);
        for (int i = 0; i < 10; i++)
            if (i != 3) Assert.False(hide[i]);
    }

    [Fact]
    public void FormatoAntiguo_HideVisual2CubreLosIndices8Y9_MismoMapeoBitABitQueElJuegoReal()
    {
        // Player.cs:55836-55840 real: "bitsByte = fileIO.ReadByte(); for (j=0;j<2;j++)
        // hideVisibleAccessory[j+8] = bitsByte[j];" - HideVisual2 bit 0 -> indice 8, bit 1 ->
        // indice 9.
        var character = Character();
        character.Version = 200;
        character.HideVisual2 = 1 << 1; // bit 1 -> indice 9

        var hide = character.ResolveActiveHide();

        Assert.NotNull(hide);
        Assert.True(hide![9]);
        Assert.False(hide[8]);
    }

    [Fact]
    public void FormatoAntiguo_SinNadaOculto_DevuelveLosDiezFalse_SinRegresion()
    {
        var character = Character();
        character.Version = 200;
        // HideVisual1/HideVisual2 en su valor por defecto (0) - ningun personaje viejo real sin
        // nada oculto debe empezar a perder accesorios por este arreglo.
        var hide = character.ResolveActiveHide();

        Assert.NotNull(hide);
        Assert.All(hide!, oculto => Assert.False(oculto));
    }

    [Fact]
    public void FormatoModerno_ConLoadouts_SigueUsandoLoadoutsDelCurrentLoadout_SinRegresion()
    {
        // Version>=269 real: Loadouts SI existe - el comportamiento ya verificado antes de este
        // encargo (CharacterListEntryViewModel/MainViewModel) no debe cambiar.
        var character = Character();
        character.Version = 279;
        var hideReal = new bool[10];
        hideReal[5] = true;
        character.Loadouts =
        [
            PlrLoadout.CreateEmpty(isPrimary: false),
            new PlrLoadout { Hide = hideReal },
            PlrLoadout.CreateEmpty(isPrimary: false),
        ];
        character.CurrentLoadout = 1;

        var hide = character.ResolveActiveHide();

        Assert.Same(hideReal, hide);
    }

    [Fact]
    public void FormatoModerno_CurrentLoadoutFueraDeRango_DevuelveNull_SinRegresion()
    {
        var character = Character();
        character.Version = 279;
        character.Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false)];
        character.CurrentLoadout = 5; // fuera de rango

        Assert.Null(character.ResolveActiveHide());
    }
}
