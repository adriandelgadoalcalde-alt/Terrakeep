using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Terrakeep.App.Services;
using Terrakeep.Core.Model;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels;

// Idea 5 del catalogo de funciones ("¿Donde esta? global, multi-mundo y multi-personaje" -
// bitacora.md 20-sep-2026, tercera ronda tras la correccion del coordinador): WorldPresenceIndex
// (citado por el catalogo como apoyo) resulto ser el censo de UN SOLO mundo ya cargado, nunca un
// indice persistente multi-mundo - eso de verdad NO existe hoy. Lo que SI existe de verdad y es
// el camino real: HomeViewModel.Characters (todo .plr ya escaneado de las carpetas conocidas) y
// ExplorationViewModel.Worlds (todo .wld ya escaneado, mismo patron) - las MISMAS dos listas que
// ya usan el lanzador de Inicio y el lanzador de mundos de Exploracion. "¿Donde esta?" ya
// buscaba dentro del personaje abierto (WhereIsIt, MainViewModel.ApplyWhereIsItFilter) - esto
// generaliza esa misma busqueda a TODOS los personajes y TODOS los mundos ya conocidos, cargando
// cada uno por su cuenta (mismo patron ya probado por CompareViewModel/WorldCompareViewModel),
// sin inventar ningun indice persistente nuevo.
//
// Alcance deliberado: una busqueda EXPLICITA (boton, no como-tu-escribes) porque el coste real es
// la suma de cargar CADA fichero completo (mismo coste ya medido en otros sitios: un .wld grande
// son ~1.4s, ver WorldPresenceIndex) - hacerlo en cada tecla pulsada congelaria la UI sin ningun
// beneficio real. Los contenedores de moneda/municion del personaje (Character.Coins/Character.
// Ammo) quedan fuera del rastreo por objeto: no viven en MergedContainers (se construyen aparte,
// ver MainViewModel.BuildContainers) y no son lo que alguien busca con "¿donde tengo mi X?" -
// LIMITE real y pequeño, documentado aqui en vez de fingido como cubierto.
public sealed partial class GlobalSearchViewModel : ObservableObject
{
    private readonly CharacterFileService _service = new();

    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _summary = string.Empty;

    public ObservableCollection<GlobalCharacterHitViewModel> CharacterHits { get; } = [];
    public ObservableCollection<GlobalWorldHitViewModel> WorldHits { get; } = [];

    public bool HasResults => CharacterHits.Count > 0 || WorldHits.Count > 0;

    private const int MaxResultsPerSide = 30;

    public async Task RunAsync(string query, IReadOnlyList<CharacterListEntryViewModel> characters, IReadOnlyList<WorldListEntryViewModel> worlds)
    {
        CharacterHits.Clear();
        WorldHits.Clear();
        OnPropertyChanged(nameof(HasResults));
        if (string.IsNullOrWhiteSpace(query))
        {
            Summary = string.Empty;
            return;
        }

        IsSearching = true;
        try
        {
            var (characterHits, worldHits, characterErrors, worldErrors) = await Task.Run(() => Scan(query, characters, worlds));
            foreach (var hit in characterHits) CharacterHits.Add(hit);
            foreach (var hit in worldHits) WorldHits.Add(hit);
            var loc = LocalizationService.Instance;
            Summary = loc.Format("search_global_summary", characterHits.Count, worldHits.Count);
            if (characterErrors > 0 || worldErrors > 0)
                Summary += " " + loc.Format("search_global_errors", characterErrors + worldErrors);
        }
        finally
        {
            IsSearching = false;
            OnPropertyChanged(nameof(HasResults));
        }
    }

    // Corre entero en un hilo de fondo (Task.Run de arriba) - construye listas locales normales
    // (nunca toca las ObservableCollection desde aqui, WPF no lo permite fuera del hilo de UI) y
    // las devuelve para que RunAsync las vuelque ya en el hilo correcto.
    private (List<GlobalCharacterHitViewModel> CharacterHits, List<GlobalWorldHitViewModel> WorldHits, int CharacterErrors, int WorldErrors) Scan(
        string query, IReadOnlyList<CharacterListEntryViewModel> characters, IReadOnlyList<WorldListEntryViewModel> worlds)
    {
        var characterHits = new List<GlobalCharacterHitViewModel>();
        var worldHits = new List<GlobalWorldHitViewModel>();
        int characterErrors = 0, worldErrors = 0;

        foreach (var entry in characters)
        {
            if (characterHits.Count >= MaxResultsPerSide) break;
            try
            {
                var loaded = _service.Load(entry.FilePath);
                foreach (var (containerKey, items) in loaded.MergedContainers)
                {
                    foreach (var item in items)
                    {
                        if (item.IsEmpty) continue;
                        string name = ResolveItemName(item);
                        if (!LibrarySearchGrammar.Matches(query, item.Id, LibrarySearchGrammar.Fold(name), null)) continue;
                        characterHits.Add(new GlobalCharacterHitViewModel(entry.Name, entry.FilePath, ContainerKeyLabel(containerKey), name, ResolveIconPath(item)));
                        if (characterHits.Count >= MaxResultsPerSide) goto siguientePersonaje;
                    }
                }
                siguientePersonaje: ;
            }
            catch
            {
                // Mismo criterio de siempre (CompareViewModel/WorldCompareViewModel): un fichero
                // corrupto/ajeno de la lista NO debe tumbar la busqueda global entera, solo
                // saltarselo - Summary avisa del total real de fallos al terminar.
                characterErrors++;
            }
        }

        foreach (var entry in worlds)
        {
            if (worldHits.Count >= MaxResultsPerSide) break;
            Services.CharacterFileService.ComprobarMundoDePrueba(entry.FilePath); // guarda del arnes, sin efecto en la app real
            try
            {
                var world = WldReader.Read(File.ReadAllBytes(entry.FilePath));
                foreach (var chest in world.Chests)
                {
                    foreach (var item in chest.Items)
                    {
                        string name = _service.VanillaCatalog.GetName(item.NetId);
                        if (!LibrarySearchGrammar.Matches(query, item.NetId, LibrarySearchGrammar.Fold(name), null)) continue;
                        worldHits.Add(new GlobalWorldHitViewModel(entry.Title, entry.FilePath, name, isChest: true));
                        if (worldHits.Count >= MaxResultsPerSide) goto siguienteMundo;
                    }
                }
                foreach (var tileEntity in world.TileEntities)
                {
                    foreach (var item in tileEntity.Items)
                    {
                        string name = _service.VanillaCatalog.GetName(item.NetId);
                        if (!LibrarySearchGrammar.Matches(query, item.NetId, LibrarySearchGrammar.Fold(name), null)) continue;
                        worldHits.Add(new GlobalWorldHitViewModel(entry.Title, entry.FilePath, name, isChest: false));
                        if (worldHits.Count >= MaxResultsPerSide) goto siguienteMundo;
                    }
                }
                siguienteMundo: ;
            }
            catch
            {
                worldErrors++;
            }
        }

        return (characterHits, worldHits, characterErrors, worldErrors);
    }

    private string ResolveItemName(GameItem item) => item.IsCalamity
        ? _service.CalamityCatalog.BySyntheticId(item.Id)?.DisplayName ?? $"Calamity #{item.Id}"
        : _service.VanillaCatalog.GetName(item.Id);

    private static string? ResolveIconPath(GameItem item)
    {
        if (!item.IsCalamity) return VanillaIconResolver.GetIconPath(item.Id);
        // Mismo criterio que CompareViewModel.ResolveItem: el icono de Calamity necesita el
        // catalogo propio para resolver el nombre de fichero real del sprite - se omite aqui a
        // proposito (esta clase no expone CalamityCatalog.Icon fuera de ResolveItemName) en vez
        // de fingir una ruta que no es real.
        return null;
    }

    // Mismas claves reales que MainViewModel.BuildContainers ya usa para las etiquetas fijas -
    // "loadout{N}Items/Social/Dyes" son el unico patron variable (hasta 4 conjuntos reales del
    // juego, Version>=269) y se formatean con las mismas claves que ya usa CompareViewModel
    // (equip_loadout_armor/dyes/vanity, Format con el numero 1-based).
    internal static string ContainerKeyLabel(string key)
    {
        var loc = LocalizationService.Instance;
        switch (key)
        {
            case "inventory": return loc["char_tab_inventory"];
            case "bank": return loc["storage_bank"];
            case "bank2": return loc["storage_safe"];
            case "bank3": return loc["storage_forge"];
            case "bank4": return loc["storage_void"];
            case "miscEquips": return loc["storage_misc_equips"];
            case "miscDyes": return loc["storage_misc_dyes"];
        }
        if (key.StartsWith("loadout", StringComparison.Ordinal) && key.Length > 8 && char.IsDigit(key[7]))
        {
            int n = key[7] - '0';
            string suffix = key[8..];
            return suffix switch
            {
                "Items" => loc.Format("equip_loadout_armor", n + 1),
                "Dyes" => loc.Format("equip_loadout_dyes", n + 1),
                "Social" => loc.Format("equip_loadout_vanity", n + 1),
                _ => key,
            };
        }
        // Fallback honesto: clave cruda en vez de una etiqueta inventada para un contenedor no
        // contemplado arriba - nunca miente sobre donde esta el objeto.
        return key;
    }
}

// Fila de resultado "en otro personaje" - gemela de WhereIsItResultViewModel pero SIN depender
// de un ItemSlotViewModel real (el personaje no esta cargado en el editor principal, solo se leyo
// aparte para buscar en el) - por eso lleva su propio FilePath, para poder abrirlo de verdad al
// hacer clic (MainViewModel.NavigateToGlobalCharacterHit).
public sealed class GlobalCharacterHitViewModel(string characterName, string filePath, string containerLabel, string itemName, string? iconPath)
{
    public string CharacterName { get; } = characterName;
    public string FilePath { get; } = filePath;
    public string ContainerLabel { get; } = containerLabel;
    public string ItemName { get; } = itemName;
    public string? IconPath { get; } = iconPath;
}

// Fila de resultado "en un mundo" - IsChest distingue cofre real de tile entity real (marco de
// objeto/perchero/maniqui/...) solo para el texto, la navegacion real (MainViewModel.
// NavigateToGlobalWorldHit) es identica para los dos: abre el mundo y reutiliza el buscador YA
// existente de Exploracion (ExplorationViewModel.WorldSearchText + categoria Cofres) en vez de
// reimplementar el marcado/navegacion en el mapa - ver el comentario real de ese metodo.
public sealed class GlobalWorldHitViewModel(string worldTitle, string filePath, string itemName, bool isChest)
{
    public string WorldTitle { get; } = worldTitle;
    public string FilePath { get; } = filePath;
    public string ItemName { get; } = itemName;
    public bool IsChest { get; } = isChest;
}
