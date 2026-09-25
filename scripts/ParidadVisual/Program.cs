// ParidadVisual - arnes de comparacion visual pixel-real Terrakeep vs Terraria 1.4.5.8 vanilla
// real (ParidadPersonaje Fase0, ver bitacora.md y scripts/ParidadVisual/README.md).
//
// Genera el lado "terrakeep" de cada caso reutilizando el pipeline REAL de produccion sin
// duplicar logica: EquipmentAppearanceResolver.Resolve/ResolveAccessories + PlayerPreviewRenderer.
// Render - el MISMO camino exacto que CharacterListEntryViewModel.cs usa para la tarjeta de Inicio
// (Terrakeep.App/ViewModels/CharacterListEntryViewModel.cs:221-251, transcrito aqui 1:1).
//
// Tambien prepara el .plr vanilla de cada caso (derivado de un .plr real existente, nunca
// construido desde cero a ciegas - se limpian/rellenan los slots que hagan falta sobre una
// plantilla real ya v�lida, para heredar gratis Version/tama�os de contenedor correctos, ver
// PlrBodySerializer) para que el lado "vanilla" se capture con el juego real.
using System.IO;
using System.Windows.Media.Imaging;
using Terrakeep.App.Services;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.PlrFormat;

if (args.Length == 0)
{
    ImprimirAyuda();
    return 1;
}

try
{
    switch (args[0])
    {
        case "renderizar":
            return Renderizar(args[1..]);
        case "preparar-caso":
            return PrepararCaso(args[1..]);
        default:
            ImprimirAyuda();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

static void ImprimirAyuda()
{
    Console.WriteLine("""
    Uso:
      ParidadVisual renderizar <rutaPlr> <salidaPng>
          Lee un .plr REAL (vanilla, cargable por el juego) y genera el PNG del lado
          "terrakeep" con el pipeline de produccion real (EquipmentAppearanceResolver +
          PlayerPreviewRenderer.Render), igual que la tarjeta de personaje de Inicio.

      ParidadVisual preparar-caso <plantillaPlr> <destinoPlr> --nombre <nombre> [--limpiar]
          Deriva un .plr vanilla real nuevo a partir de una plantilla real existente
          (hereda Version/tama�os de contenedor validos). --limpiar vacia PrimaryLoadout y
          los 3 Loadouts reales (Items/Social/Dyes a Empty, Hide a todo-false) - usado para
          el Caso 1 (personaje base sin nada puesto). El nombre se cambia siempre para no
          pisar la plantilla ni confundir el .plr de prueba con un personaje real del usuario.
    """);
}

static int Renderizar(string[] a)
{
    if (a.Length < 2) { Console.Error.WriteLine("Uso: renderizar <rutaPlr> <salidaPng>"); return 1; }
    string rutaPlr = a[0];
    string salidaPng = a[1];

    var personaje = PlrFile.Read(File.ReadAllBytes(rutaPlr));

    // Assets reales de Terrakeep.App (mismos catalogos que usa la app instalada) - ruta
    // absoluta a proposito, este arnes NO es parte del build de produccion y no necesita
    // copiar 632 ficheros a su propio bin/.
    string assetsDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Terrakeep.App", "Assets");
    assetsDir = Path.GetFullPath(assetsDir);
    if (!Directory.Exists(assetsDir))
        throw new DirectoryNotFoundException($"No se encontro Assets/ real de Terrakeep.App en '{assetsDir}'");

    var vanillaArmorSlots = VanillaArmorSlotCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_armor_slots.json"));
    var vanillaAccessorySlots = VanillaAccessorySlotCatalog.LoadFromFile(Path.Combine(assetsDir, "vanilla_accessory_slots.json"));
    var calamityCatalog = CalamityCatalog.LoadFromFile(Path.Combine(assetsDir, "calamity", "catalog.json"));
    var petAnimations = PetAnimationCatalog.LoadFromFile(Path.Combine(assetsDir, "pet_animations.json"));
    var equipmentAppearance = new EquipmentAppearanceResolver(vanillaArmorSlots, vanillaAccessorySlots, calamityCatalog, petAnimations);

    // Transcripcion 1:1 de CharacterListEntryViewModel.cs:221-251 (mismo camino real que la
    // tarjeta de personaje de Inicio) - sin logica nueva, solo reutilizado fuera de WPF/UI.
    PlayerPreviewRenderer.Tint T(byte[] c) => new(c[0], c[1], c[2]);
    var colors = new PlayerPreviewRenderer.PlayerColors(
        T(personaje.HairColor), T(personaje.SkinColor), T(personaje.EyeColor),
        T(personaje.ShirtColor), T(personaje.UnderColor), T(personaje.PantsColor), T(personaje.ShoesColor));
    var armor = equipmentAppearance.Resolve(personaje.PrimaryLoadout);
    var hide = personaje.Loadouts.ElementAtOrDefault(personaje.CurrentLoadout)?.Hide;
    var accessories = equipmentAppearance.ResolveAccessories(personaje.PrimaryLoadout, hide);
    int hairStyle = personaje.HairStyle;
    byte skinVariant = personaje.Gender;

    var bitmap = PlayerPreviewRenderer.Render(hairStyle, skinVariant, colors, armor, accessories: accessories);

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(salidaPng))!);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var fs = new FileStream(salidaPng, FileMode.Create);
    encoder.Save(fs);

    Console.WriteLine($"OK: {salidaPng} ({bitmap.PixelWidth}x{bitmap.PixelHeight}) <- {rutaPlr} " +
        $"(hairStyle={hairStyle}, skinVariant={skinVariant}, head={armor.HeadSlot?.ToString() ?? "-"}, " +
        $"body={armor.BodySlot?.ToString() ?? "-"}, legs={armor.LegsSlot?.ToString() ?? "-"})");
    return 0;
}

static int PrepararCaso(string[] a)
{
    if (a.Length < 2) { Console.Error.WriteLine("Uso: preparar-caso <plantillaPlr> <destinoPlr> --nombre <nombre> [--limpiar]"); return 1; }
    string plantillaPlr = a[0];
    string destinoPlr = a[1];
    string? nombre = null;
    bool limpiar = false;
    for (int i = 2; i < a.Length; i++)
    {
        if (a[i] == "--nombre" && i + 1 < a.Length) { nombre = a[++i]; }
        else if (a[i] == "--limpiar") { limpiar = true; }
    }
    if (nombre is null) { Console.Error.WriteLine("Falta --nombre <nombre>"); return 1; }

    var plantilla = PlrFile.Read(File.ReadAllBytes(plantillaPlr));

    var primario = limpiar ? PlrLoadout.CreateEmpty(isPrimary: true) : plantilla.PrimaryLoadout;
    var loadouts = limpiar && plantilla.Loadouts.Length > 0
        ? plantilla.Loadouts.Select(_ => PlrLoadout.CreateEmpty(isPrimary: false)).ToArray()
        : plantilla.Loadouts;

    var destino = new PlrCharacter
    {
        Version = plantilla.Version,
        IsSwitch = plantilla.IsSwitch,
        MetaVersion = plantilla.MetaVersion,
        MetaFlags1 = plantilla.MetaFlags1,
        MetaFlags2 = plantilla.MetaFlags2,
        Guid = plantilla.Guid,
        Name = nombre,
        Difficulty = plantilla.Difficulty,
        PlayTimeLow = plantilla.PlayTimeLow,
        PlayTimeHigh = plantilla.PlayTimeHigh,
        HairStyle = plantilla.HairStyle,
        HairDye = plantilla.HairDye,
        Team = plantilla.Team,
        HideVisual1 = plantilla.HideVisual1,
        HideVisual2 = plantilla.HideVisual2,
        HideMisc = plantilla.HideMisc,
        Gender = plantilla.Gender,
        HealthNow = plantilla.HealthNow,
        HealthMax = plantilla.HealthMax,
        ManaNow = plantilla.ManaNow,
        ManaMax = plantilla.ManaMax,
        ExtraAccessory = plantilla.ExtraAccessory,
        UnlockedBiomeTorches = plantilla.UnlockedBiomeTorches,
        UsingBiomeTorches = plantilla.UsingBiomeTorches,
        ExtraUsingFlags = plantilla.ExtraUsingFlags,
        FinishedDD2Event = plantilla.FinishedDD2Event,
        TaxMoney = plantilla.TaxMoney,
        PveDeaths = plantilla.PveDeaths,
        PvpDeaths = plantilla.PvpDeaths,
        HairColor = plantilla.HairColor,
        SkinColor = plantilla.SkinColor,
        EyeColor = plantilla.EyeColor,
        ShirtColor = plantilla.ShirtColor,
        UnderColor = plantilla.UnderColor,
        PantsColor = plantilla.PantsColor,
        ShoesColor = plantilla.ShoesColor,
        PrimaryLoadout = primario,
        Loadouts = loadouts,
        // Caso 1 (limpiar): tambien el inventario/miscEquips a vacio - el doll de la lista de
        // personajes real solo lee PrimaryLoadout (mirror de "lo puesto"), pero limpiar el resto
        // deja el .plr de prueba mas honesto (sin objetos "puestos" invisibles al render por
        // estar en otro contenedor).
        Inventory = limpiar ? plantilla.Inventory.Select(_ => PlrItemSlot.Empty).ToArray() : plantilla.Inventory,
        Coins = plantilla.Coins,
        Ammo = plantilla.Ammo,
        EquipmentItems = limpiar ? plantilla.EquipmentItems.Select(_ => PlrItemSlot.Empty).ToArray() : plantilla.EquipmentItems,
        EquipmentDyes = limpiar ? plantilla.EquipmentDyes.Select(_ => PlrItemSlot.Empty).ToArray() : plantilla.EquipmentDyes,
        BankItems = plantilla.BankItems,
        SafeItems = plantilla.SafeItems,
        ForgeItems = plantilla.ForgeItems,
        VoidItems = plantilla.VoidItems,
        VoidVaultByte = plantilla.VoidVaultByte,
        Buffs = limpiar ? [] : plantilla.Buffs,
        Servers = plantilla.Servers,
        HotbarLocked = plantilla.HotbarLocked,
        HideInfo = plantilla.HideInfo,
        FishingQuestsCompleted = plantilla.FishingQuestsCompleted,
        DpadBindings = plantilla.DpadBindings,
        BuilderAccStatus = plantilla.BuilderAccStatus,
        BartenderQuests = plantilla.BartenderQuests,
        IsDead = false,
        RespawnTimer = 0,
        LastTimeSaved1 = plantilla.LastTimeSaved1,
        LastTimeSaved2 = plantilla.LastTimeSaved2,
        GolferScore = plantilla.GolferScore,
        ResearchMysteryByte = plantilla.ResearchMysteryByte,
        Research = plantilla.Research,
        TempItems = plantilla.TempItems,
        CreativePowers = plantilla.CreativePowers,
        SuperCartByte = plantilla.SuperCartByte,
        CurrentLoadout = plantilla.CurrentLoadout,
    };

    byte[] bytes = PlrFile.Write(destino);
    PlrFile.VerifyRoundTrip(bytes, destino); // nunca escribir a disco sin releer primero, mismo criterio que CharacterFileService.Save
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinoPlr))!);
    File.WriteAllBytes(destinoPlr, bytes);

    Console.WriteLine($"OK: {destinoPlr} (nombre={nombre}, limpiar={limpiar}, version={destino.Version})");
    return 0;
}
