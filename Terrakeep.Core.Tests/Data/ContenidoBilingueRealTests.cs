using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Ronda de traduccion del CONTENIDO del juego (6-sep-2026). Contra los JSON REALES de
// Terrakeep.App/Assets, no copias de prueba: el limite que cierra esta ronda era
// justamente que los catalogos existian pero SOLO en español, asi que una prueba con datos
// inventados no demostraria nada.
//
// Todas las comprobaciones usan las sobrecargas con idioma EXPLICITO a proposito: este proyecto
// de tests SI corre en paralelo (a diferencia de ViewModels.Tests), y tocar el idioma activo del
// proceso (LocalizedContent.CurrentLanguage) contaminaria a cualquier otra clase de test que
// estuviera leyendo un nombre a la vez.
//
// Los textos esperados son los reales del juego, verificados contra
// Terraria.Localization.Content.{es-ES,en-US}.* del decompilado - no traducciones nuestras.
public class ContenidoBilingueRealTests
{
    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    private const string Es = LocalizedContent.Spanish;
    private const string En = LocalizedContent.English;

    private static bool Falta(params string[] nombres) =>
        nombres.Any(n => !File.Exists(Path.Combine(AssetsDir, n)));

    private static VanillaItemCatalog Objetos() => VanillaItemCatalog.LoadFromFile(
        Path.Combine(AssetsDir, "vanilla_item_names.json"),
        Path.Combine(AssetsDir, "vanilla_item_names_by_key.json"),
        Path.Combine(AssetsDir, "vanilla_item_ids_by_key.json"),
        Path.Combine(AssetsDir, "vanilla_item_names_en.json"),
        Path.Combine(AssetsDir, "vanilla_item_names_by_key_en.json"));

    [Fact]
    public void NombreDeObjeto_RealEnLosDosIdiomas()
    {
        if (Falta("vanilla_item_names.json", "vanilla_item_names_en.json")) return;
        var catalogo = Objetos();

        Assert.Equal("Pico de hierro", catalogo.GetName(1, Es));
        Assert.Equal("Iron Pickaxe", catalogo.GetName(1, En));
        Assert.Equal("Espada larga de hierro", catalogo.GetName(4, Es));
        Assert.Equal("Iron Broadsword", catalogo.GetName(4, En));

        // Por nombre interno (la via real de builds.json y de los PID de investigacion).
        Assert.Equal("Casco fundido", catalogo.GetNameByKey("MoltenHelmet", Es));
        Assert.Equal("Molten Helmet", catalogo.GetNameByKey("MoltenHelmet", En));
    }

    [Fact]
    public void NombreDeObjeto_SinIngresRealSeQuedaEnEspañol_NuncaVacio()
    {
        if (Falta("vanilla_item_names.json", "vanilla_item_names_en.json")) return;
        var catalogo = Objetos();

        // "First Fractal" (4722) es uno de los 14 objetos reales sin entrada en la seccion
        // ItemName de en-US (objeto inobtenible del juego). La regla es caer al idioma de
        // referencia, NUNCA quedarse vacio ni inventarse una traduccion.
        string ingles = catalogo.GetName(4722, En);
        Assert.False(string.IsNullOrWhiteSpace(ingles));
        Assert.Equal(catalogo.GetName(4722, Es), ingles);
    }

    [Fact]
    public void CoberturaRealDelCatalogoIngles()
    {
        if (Falta("vanilla_item_names.json", "vanilla_item_names_en.json")) return;
        var catalogo = Objetos();

        int total = 0, traducidos = 0;
        foreach (var (id, _) in catalogo.AllEntries(Es))
        {
            total++;
            if (catalogo.GetName(id, En) != catalogo.GetName(id, Es)) traducidos++;
        }
        // 6194 objetos reales, 6180 con nombre ingles real. Unos pocos coinciden de verdad en
        // los dos idiomas ("Gel", nombres propios...), asi que el umbral se deja holgado a
        // proposito: lo que esta prueba defiende es "la inmensa mayoria cambia de idioma", no
        // una cifra exacta que se rompa cada vez que Terraria toque una traduccion.
        Assert.True(total > 6000, $"esperados >6000 objetos reales, contados {total}");
        Assert.True(traducidos > 5500, $"esperados >5500 nombres distintos entre idiomas, contados {traducidos}");
    }

    [Fact]
    public void NombreDeNpc_RealEnLosDosIdiomas()
    {
        if (Falta("npc_names.json")) return;
        var catalogo = NpcNameCatalog.LoadFromFile(Path.Combine(AssetsDir, "npc_names.json"));

        Assert.Equal("Slime azul", catalogo.GetName(1, Es));
        Assert.Equal("Blue Slime", catalogo.GetName(1, En));
        Assert.Equal("Ojo de Cthulhu", catalogo.GetName(4, Es));
        Assert.Equal("Eye of Cthulhu", catalogo.GetName(4, En));
    }

    [Fact]
    public void NombreDeTileYPared_RealEnLosDosIdiomas()
    {
        if (Falta("tile_names.json")) return;
        var catalogo = TileNameCatalog.LoadFromFile(Path.Combine(AssetsDir, "tile_names.json"));

        Assert.Equal("Bloque de tierra", catalogo.TileName(0, Es));
        Assert.Equal("Dirt Block", catalogo.TileName(0, En));
        Assert.Equal("Bloque de piedra", catalogo.TileName(1, Es));
        Assert.Equal("Stone Block", catalogo.TileName(1, En));

        // Un tile sin traduccion real al español se queda en ingles en los DOS idiomas (regla
        // de siempre: lo que no tiene traduccion real no se inventa) - lo que nunca puede pasar
        // es que salga vacio.
        Assert.False(string.IsNullOrWhiteSpace(catalogo.TileName(2, Es)));
        Assert.False(string.IsNullOrWhiteSpace(catalogo.TileName(2, En)));
    }

    [Fact]
    public void NombreYDescripcionDeBuff_RealEnLosDosIdiomas()
    {
        if (Falta("vanilla_buff_names_en.json", "vanilla_buff_descriptions_en.json")) return;
        var catalogo = VanillaBuffCatalog.LoadFromFile(
            Path.Combine(AssetsDir, "vanilla_buff_names.json"),
            Path.Combine(AssetsDir, "vanilla_buff_descriptions.json"),
            Path.Combine(AssetsDir, "vanilla_buff_names_es.json"),
            Path.Combine(AssetsDir, "vanilla_buff_names_en.json"),
            Path.Combine(AssetsDir, "vanilla_buff_descriptions_en.json"));

        // Buff 1 = ObsidianSkin (Piel de obsidiana / Obsidian Skin).
        Assert.Equal("Piel de obsidiana", catalogo.GetDisplayName(1, Es));
        Assert.Equal("Obsidian Skin", catalogo.GetDisplayName(1, En));
        Assert.Equal("Inmune a la lava", catalogo.GetDescription(1, Es));
        Assert.Equal("Immune to lava", catalogo.GetDescription(1, En));
    }

    [Fact]
    public void BonoDeSetVanilla_RealEnLosDosIdiomas()
    {
        if (Falta("vanilla_armor_sets.json")) return;
        var catalogo = VanillaArmorSetCatalog.LoadFromFile(Path.Combine(AssetsDir, "vanilla_armor_sets.json"));

        var cobre = catalogo.Get(76); // Casco/coraza/grebas de cobre - clave real MetalTier1
        Assert.NotNull(cobre);
        Assert.Equal("2 defensa", cobre.DisplayTextFor(Es));
        Assert.Equal("2 defense", cobre.DisplayTextFor(En));

        // Ninguna entrada real puede quedarse sin texto en ninguno de los dos idiomas.
        foreach (int id in new[] { 76, 231 })
        {
            var info = catalogo.Get(id);
            if (info is null) continue;
            Assert.False(string.IsNullOrWhiteSpace(info.DisplayTextFor(Es)));
            Assert.False(string.IsNullOrWhiteSpace(info.DisplayTextFor(En)));
        }
    }

    [Fact]
    public void TooltipDescriptivoVanilla_RealEnLosDosIdiomas()
    {
        if (Falta("vanilla_item_tooltips_en.json")) return;
        var catalogo = VanillaItemTooltipCatalog.LoadFromFile(
            Path.Combine(AssetsDir, "vanilla_item_tooltips.json"),
            Path.Combine(AssetsDir, "vanilla_item_tooltips_en.json"));

        // Antorcha (8): "Da luz" / "Provides light".
        Assert.Equal("Da luz", catalogo.Get(8, Es));
        Assert.Equal("Provides light", catalogo.Get(8, En));
    }

    [Fact]
    public void ContenidoDeCalamity_NombreYBonoDeSet_RealEnLosDosIdiomas()
    {
        string catalogPath = Path.Combine(AssetsDir, "calamity", "catalog.json");
        if (!File.Exists(catalogPath)) return;
        var catalogo = CalamityCatalog.LoadFromFile(catalogPath);

        var traje = catalogo.ByModAndInternal("CalamityMod", "AbyssalDivingSuit");
        Assert.NotNull(traje);
        Assert.Equal("Traje de Buceo Abisal", traje.DisplayNameFor(Es));
        Assert.Equal("Abyssal Diving Suit", traje.DisplayNameFor(En));

        // El nombre ingles sale del `DisplayName` REAL del hjson en-US, no del
        // `displayName_fallback` (que es el nombre interno de la clase humanizado). Este caso
        // concreto es el que destapo la diferencia en el volcado del arnes A11: el fallback dice
        // "Aerospec Head Melee" y el mod real dice "Aerospec Helm".
        var casco = catalogo.ByModAndInternal("CalamityMod", "AerospecHeadMelee");
        Assert.NotNull(casco);
        Assert.Equal("Yelmo de Aerospec", casco.DisplayNameFor(Es));
        Assert.Equal("Aerospec Helm", casco.DisplayNameFor(En));

        // Bono de set real: en ingles es texto LITERAL del mod (esta instalacion de Calamity
        // solo trae localizacion en-US), en español es la traduccion ya revisada del proyecto.
        string? bonoEs = casco.SetBonusFor(Es);
        string? bonoEn = casco.SetBonusFor(En);
        Assert.False(string.IsNullOrWhiteSpace(bonoEs));
        Assert.False(string.IsNullOrWhiteSpace(bonoEn));
        Assert.NotEqual(bonoEs, bonoEn);
        Assert.StartsWith("5% increased movement speed", bonoEn);

        // Cobertura real: los 69 sets con bono tienen los dos idiomas.
        int conBono = 0, conIngles = 0;
        foreach (var e in catalogo.Entries)
        {
            if (string.IsNullOrWhiteSpace(e.SetBonusFor(Es))) continue;
            conBono++;
            if (e.SetBonusFor(En) != e.SetBonusFor(Es)) conIngles++;
        }
        Assert.Equal(69, conBono);
        Assert.Equal(69, conIngles);
    }
}
