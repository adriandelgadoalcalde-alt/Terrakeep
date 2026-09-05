using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.Core.Tests.PlrFormat;

// A9-08-SPAWNMUNDO (informe de pulido final, C-05, cierra E4): "spawn points de otro mundo ->
// CharacterSpawns.Count == 0". Regla real del propio juego (Player.FindSpawn/RemoveSpawn/
// AddSpawn, las tres identicas: spI[i]==Main.worldID && spN[i]==Main.worldName) - antes
// cualquier Spawn Point guardado se mostraba en CUALQUIER mundo cargado.
public sealed class PlrServerEntryBelongsToWorldTests
{
    private static PlrServerEntry Entry(int worldId, string name) =>
        new() { SpawnX = 100, SpawnY = 200, WorldId = worldId, Name = name };

    [Fact]
    public void SinMundoCargado_NuncaSeFiltra()
    {
        Assert.True(Entry(1, "Mundo A").BelongsToWorld(null, null));
        Assert.True(Entry(999, "Otro mundo").BelongsToWorld(null, "Mundo cualquiera"));
    }

    [Fact]
    public void MismoIdYMismoNombre_PerteneceAlMundo()
    {
        Assert.True(Entry(42, "Mundo A").BelongsToWorld(42, "Mundo A"));
    }

    [Fact]
    public void MismoIdPeroNombreDistinto_NoPertenece()
    {
        // .wld copiado y renombrado a mano - conserva el WorldId, pero el juego real lo trata
        // como un mundo distinto (regla real de Player.FindSpawn: exige las DOS condiciones).
        Assert.False(Entry(42, "Mundo A").BelongsToWorld(42, "Mundo A (copia)"));
    }

    [Fact]
    public void MismoNombrePeroIdDistinto_NoPertenece()
    {
        Assert.False(Entry(42, "Mundo A").BelongsToWorld(99, "Mundo A"));
    }

    [Fact]
    public void IdYNombreDistintos_NoPertenece()
    {
        Assert.False(Entry(1, "Mundo A").BelongsToWorld(2, "Mundo B"));
    }
}
