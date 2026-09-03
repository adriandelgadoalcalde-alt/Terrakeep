using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// H4-08 (cuarta auditoria de Opus, Fable): "la version buena - un lanzador de mundos calcado
// del de personajes de Inicio" - gemelo real de CharacterListEntryViewModel, para la carpeta
// real de mundos de tModLoader en vez de la de personajes. Sin doll de vista previa (un
// personaje SI tiene uno real y barato, PlayerPreviewRenderer - un mundo no tiene ningun
// equivalente real igual de barato: renderizar el mapa completo es justo el coste de ~1.4s que
// ExplorationViewModel.LoadFromPathAsync ya mide y por el que esto existe de forma async en
// primer lugar, inventar una miniatura aparte tampoco seria un dato real). Titulo+dimensiones
// vienen de WldReader.ReadHeader (lectura barata, nunca decodifica tiles/NPCs).
public sealed partial class WorldListEntryViewModel(string filePath, string title, int tilesWide, int tilesHigh, DateTime lastModifiedUtc) : ObservableObject
{
    public string FilePath { get; } = filePath;
    public string Title { get; } = title;
    public string SizeLabel { get; } = $"{tilesWide}x{tilesHigh} tiles";
    public string LastModifiedText { get; } = lastModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    // H5-11 (quinta auditoria de Opus): "marcar el mundo cargado igual que Inicio marca el
    // personaje cargado (mismo IsCurrent, mismo borde de acento)" - gemelo real de
    // CharacterListEntryViewModel.IsCurrent, fijado por ExplorationViewModel.UpdateCurrentWorldPath.
    [ObservableProperty] private bool _isCurrent;
}
