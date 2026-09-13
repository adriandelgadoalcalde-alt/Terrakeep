namespace Terrakeep.Core.WldFormat;

// Progreso del bestiario, guardado en el .wld (NO en el .plr) desde la version 210 del formato
// (Journey's End, 1.4). Confirmado leyendo el codigo real decompilado de tModLoader
// (Terraria/GameContent/Bestiary/BestiaryUnlocksTracker.cs + NPCKillsTracker.cs +
// NPCWasNearPlayerTracker.cs + NPCWasChatWithTracker.cs, todos con el MISMO formato secuencial:
// Int32 count + esa cantidad de entradas) y contrastado ademas contra TEdit (Bestiary.cs de
// TEdit.Terraria, identico byte a byte).
//
// La clave de cada entrada es el "bestiary credit id" real del juego (NPC.GetBestiaryCreditId ->
// ContentSamples.NpcBestiaryCreditIdsByNpcNetIds, que para NPCs vanilla es exactamente
// NPCID.Search.GetName - confirmado a mano: el NPC id 3 (Zombie) da la clave "Zombie", el MISMO
// texto que la columna "key" de Assets/npc_names.json). NPCs modded (Calamity) usan su propio id
// interno registrado por el mod, que NO esta en npc_names.json (ese catalogo es solo vanilla) -
// se muestran con su clave cruda en la interfaz en vez de fingir una traduccion inexistente,
// mismo criterio de honestidad que el resto del proyecto.
public sealed class WldBestiary
{
    public required IReadOnlyDictionary<string, int> Kills { get; init; }
    public required IReadOnlySet<string> Sighted { get; init; }
    public required IReadOnlySet<string> Chatted { get; init; }
}
