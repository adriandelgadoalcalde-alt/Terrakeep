using Terrakeep.Core.PlrFormat;

namespace Terrakeep.Core.Tests.PlrFormat;

// ParidadPersonaje Fase4 (26-sep-2026): GapAnalysis BugH, corregido (26-sep-2026, requirement
// 480a9bdd-6d5f-4fa6-935d-46f895e97514). HideVisual1/HideVisual2 SIEMPRE representan el estado
// activo real (player.hideVisibleAccessory, Player.cs:55418-55424 Serialize + :55834-55839
// Deserialize), CON Loadouts o sin ellos - Loadouts[CurrentLoadout].Hide NUNCA es una fuente
// fiable del estado activo (EquipmentLoadout.cs:78 Swap intercambia ese array al cambiar de
// loadout, "Utils.Swap(ref hideVisibleAccessory[k], ref Hide[k])" - tras el swap, Hide[] del
// loadout contiene el estado ANTERIOR, no el suyo). Version anterior de este test (bug real,
// confirmado contra Terrariano.plr/Eldelgas.plr del usuario): asumia que Loadouts[CurrentLoadout]
// .Hide era el Hide activo cuando Loadouts.Length > 0, y solo caia al formato antiguo
// (HideVisual1/HideVisual2) cuando Loadouts estaba vacio - exactamente al reves de la realidad.
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
        // Personaje de formato antiguo real (version<269): Loadouts SIEMPRE vacio. HideVisual1
        // bit 3 (indice 3, mismo mapeo bit a bit que Player.cs:55831-55841 real: bitsByte[i] =
        // hideVisibleAccessory[i] para i=0..7) marca oculto el hueco de accesorio 3 (Waist en las
        // pruebas de EquipmentAppearanceResolver).
        var character = Character();
        character.Version = 200; // < 269, Loadouts vacio de verdad (no simulado)
        character.HideVisual1 = 1 << 3; // bit 3 activo, resto a cero

        var hide = character.ResolveActiveHide();

        Assert.Equal(10, hide.Length);
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

        Assert.True(hide[9]);
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

        Assert.All(hide, oculto => Assert.False(oculto));
    }

    [Fact]
    public void FormatoModerno_ConLoadouts_IgnoraLoadoutsDelCurrentLoadout_UsaSiempreHideVisual1Y2()
    {
        // Version>=269 real (caso real de Terrariano.plr/Eldelgas.plr): Loadouts SI existe, pero
        // Loadouts[CurrentLoadout].Hide esta a todo-false (el caso real de ambos .plr del
        // usuario) mientras que HideVisual1 SI tiene bits activos (el estado activo real,
        // hideVisibleAccessory). El bug viejo devolvia el Hide todo-false del loadout y perdia el
        // oculto real - el arreglo debe ignorar ese Hide y usar HideVisual1/HideVisual2 igual.
        var character = Character();
        character.Version = 279;
        character.HideVisual1 = 1 << 5; // bit 5 activo: el estado activo real oculta el indice 5
        var hideDelLoadout = new bool[10]; // todo-false: exactamente el caso real observado
        character.Loadouts =
        [
            PlrLoadout.CreateEmpty(isPrimary: false),
            new PlrLoadout { Hide = hideDelLoadout },
            PlrLoadout.CreateEmpty(isPrimary: false),
        ];
        character.CurrentLoadout = 1;

        var hide = character.ResolveActiveHide();

        Assert.NotSame(hideDelLoadout, hide);
        Assert.True(hide[5]);
        for (int i = 0; i < 10; i++)
            if (i != 5) Assert.False(hide[i]);
    }

    [Fact]
    public void FormatoModerno_CurrentLoadoutFueraDeRango_SigueLeyendoHideVisual1Y2_NuncaDevuelveNull()
    {
        // Antes del arreglo, un CurrentLoadout fuera de rango devolvia null (ElementAtOrDefault).
        // Ahora ResolveActiveHide ya no depende en absoluto de Loadouts/CurrentLoadout para el
        // estado activo, asi que este caso limite ya ni siquiera puede ocurrir - siempre hay un
        // array de 10 posiciones derivado de HideVisual1/HideVisual2.
        var character = Character();
        character.Version = 279;
        character.HideVisual1 = 1 << 2;
        character.Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false)];
        character.CurrentLoadout = 5; // fuera de rango

        var hide = character.ResolveActiveHide();

        Assert.Equal(10, hide.Length);
        Assert.True(hide[2]);
    }
}
