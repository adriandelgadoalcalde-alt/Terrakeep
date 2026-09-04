namespace TerrasavrNative.Core.WldFormat;

// Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
// ESPEC-buscador-mundo-tedit.md. Formato real confirmado directamente contra
// World.FileV2.cs:1828-1839 de TEdit (String Text, Int32 X, Int32 Y, en ese orden - el texto
// va PRIMERO). Solo se guardan los letreros cuya casilla real sea de verdad un letrero
// (Sign=55/GraveMarker=85/AnnouncementBox=425/TatteredSign=573 - TileType.cs real de TEdit,
// tambien confirmado esta sesion) - el propio fichero .wld puede traer entradas "fantasma" de
// letreros borrados cuyo tile ya no existe, TEdit las descarta al cargar y este lector hace lo
// mismo (ver WldReader.ReadSigns).
public sealed class WldSign
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required string Text { get; init; }
}
