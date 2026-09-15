namespace Terrakeep.Core.Guia;

// Un solo cerebro de Guia (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, T1/H3, 16-sep-2026): hasta ahora
// TerrakeepMod (Common/Guia/EvaluadorGuia.cs) y Terrakeep (este mismo proyecto, GuideEvaluator.cs)
// tenian CADA UNO su propia copia de la aritmetica de progreso (Fraccion/Contar/Preparacion/
// PasoCompletado) y del despacho de requisitos - funcion a funcion identicas, solo cambiaba DE
// DONDE salia el dato. Esta interfaz es exactamente la "fuente de estado" que el documento de I+D
// pedia: el motor de evaluacion (GuideEvaluationEngine, en este mismo directorio) vive UNA sola
// vez aqui y pregunta a un IGuideStateProvider - TerrakeepMod implementa uno que lee
// Main.LocalPlayer/Main.npc EN VIVO (ver ProveedorEstadoGuiaMod.cs en el mod), Terrakeep implementa
// otro que lee un GuideContext ESTATICO (ver DesktopGuideStateProvider.cs, en este directorio).
//
// Las cuatro propiedades "Has*" son CAPACIDADES: dicen si este proveedor sabe, en principio,
// contestar esa categoria de pregunta AHORA MISMO - nunca si la respuesta concreta es positiva.
//   - TerrakeepMod las devuelve SIEMPRE true: EstadoJugadorGuia (el mod) ya se degrada sola a
//     ceros/false cuando no hay partida activa (HayPartida), asi que el requisito sale "no
//     cumplido" en vez de "no evaluable" - EXACTAMENTE el comportamiento que ya tenia el
//     EvaluadorGuia.cs original del mod antes de esta consolidacion, que nunca marcaba nada
//     NoEvaluable por falta de partida.
//   - Terrakeep las calcula segun haya o no personaje/mundo/inventario cargados, y SIEMPRE false
//     para lo que depende de una partida EN MARCHA (cristales de vida, defensa, daño de arma, NPC
//     hostil activo): ningun .plr/.wld estatico guarda eso - ver el porque caso por caso en
//     DesktopGuideStateProvider.cs.
public interface IGuideStateProvider
{
    /// <summary>true si se puede leer el personaje (vida maxima).</summary>
    bool HasCharacterData { get; }

    /// <summary>true si se puede leer el mundo (npcs del pueblo, si un NPC concreto vive ahi).</summary>
    bool HasWorldData { get; }

    /// <summary>true si se puede leer el inventario (objeto, objeto_cualquiera, gancho).</summary>
    bool HasInventoryData { get; }

    /// <summary>true si hay una partida EN MARCHA de verdad (cristales de vida, defensa, daño de
    /// arma ya pasado por el jugador, NPC hostil activo AHORA MISMO) - nunca cierto en un editor
    /// de ficheros estaticos.</summary>
    bool HasLiveGameData { get; }

    int CristalesVida { get; }
    int VidaMaxima { get; }
    int Defensa { get; }
    int NpcsDelPueblo();
    bool HayNpc(int id);
    int CuantosLleva(int id);
    int DanoDelMejorArma(out string nombre);
    bool LlevaGancho(out string nombre);

    /// <summary>true si el nombre de bandera es una de las que este proveedor sabe (en principio)
    /// leer - independiente de si hay datos cargados ahora para contestar de verdad.</summary>
    bool BanderaConocida(string bandera);

    /// <summary>Valor real, o null si la bandera es conocida pero no hay datos cargados ahora
    /// mismo con los que contestar (p.ej. Terrakeep sin mundo cargado).</summary>
    bool? ValorBandera(string bandera);

    /// <summary>Clave de motivo (para <see cref="ResultadoRequisitoGuia.MotivoClave"/>) de por que
    /// una bandera NO reconocida no se puede evaluar aqui. Null en TerrakeepMod (nunca hace falta
    /// explicarlo: el mod no tiene "banderas fuera de su alcance").</summary>
    string? MotivoBanderaDesconocida(string bandera);

    string NombreDeObjeto(int id);
    string NombreDeNpc(int id);

    /// <summary>Clave de motivo para los cuatro tipos que SOLO se pueden leer con una partida en
    /// marcha (<see cref="HasLiveGameData"/> = false). TerrakeepMod nunca llega a llamarse (su
    /// HasLiveGameData es siempre true) y puede devolver lo que quiera.</summary>
    string MotivoSinPartidaEnMarcha(TipoRequisitoGuia tipo);
}
