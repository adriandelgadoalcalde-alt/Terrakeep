namespace Terrakeep.Core.Guia;

// Guia v2 (F0, 02-oct-2026): capacidades NUEVAS de la fuente de estado, en una interfaz APARTE
// que hereda de IGuideStateProvider. Por que aparte y no miembros nuevos en la interfaz original:
// TerrakeepMod implementa IGuideStateProvider (ProveedorEstadoGuiaMod.cs) contra
// lib\Terrakeep.Core.dll; añadir miembros a esa interfaz le romperia la compilacion en cuanto
// actualizara la DLL. Asi, un proveedor que solo implementa la v1 sigue funcionando y los tipos
// nuevos salen "no evaluables" (limite estructural, con motivo), nunca "cumplidos".
//
// Contrato de los bool?: null = "no hay datos ahora mismo para contestar" (personaje/mundo/.tplr/
// .twld sin cargar); false/true = respuesta real.
public interface IGuideStateProviderV2 : IGuideStateProvider
{
    /// <summary>Unidades del objeto en CUALQUIER sitio del personaje: inventario, municion,
    /// monedas, equipo (los 3 conjuntos), hucha, caja fuerte, forja defensiva y bolsa del vacio.</summary>
    int CuantosPosee(int id);

    /// <summary>true si el objeto esta puesto (armadura/accesorio) en el conjunto activo.</summary>
    bool LlevaEquipado(int id);

    /// <summary>true si este proveedor sabe (en principio) leer esa mejora permanente.</summary>
    bool MejoraConocida(string clave);

    /// <summary>Valor real de una mejora permanente consumida (vanilla: corazon de demonio,
    /// favor del dios de las antorchas, cristal de egida...; Calamity: naranja sanguina, fruta
    /// milagrosa...). Null = sin datos cargados.</summary>
    bool? MejoraPermanente(string clave);

    bool EstadoMundoConocido(string clave);

    /// <summary>Estado persistente del mundo que no es "jefe derrotado" (Calamity: revenge,
    /// death, esquemas de laboratorio encontrados, TalkedToDraedon; vanilla: mundo carmesi).</summary>
    bool? EstadoMundo(string clave);

    /// <summary>Frutas de vida consumidas (derivado de la vida maxima, como CristalesVida).</summary>
    int FrutasVida { get; }

    int ManaMaxima { get; }

    /// <summary>Modo real de la partida, o null si no hay mundo cargado.</summary>
    ModoPartida? Modo { get; }
}

/// <summary>Modo de juego real: GameMode del .wld (0 clasico, 1 experto, 2 maestro, 3 viaje) y,
/// con Calamity, Revengeance/Death del .twld (null = no se sabe).</summary>
public sealed record ModoPartida(int GameMode, bool? Revengeance, bool? Death)
{
    /// <summary>Claves de modo que aplican ("clasico","experto","maestro","viaje","revengeance",
    /// "death") - mismo vocabulario que AvisoModo.Modos de la guia v2.</summary>
    public IReadOnlyList<string> Claves()
    {
        var c = new List<string>
        {
            GameMode switch { 1 => "experto", 2 => "maestro", 3 => "viaje", _ => "clasico" },
        };
        if (Revengeance == true) c.Add("revengeance");
        if (Death == true) c.Add("death");
        return c;
    }
}
