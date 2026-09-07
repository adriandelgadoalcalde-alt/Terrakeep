using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.Services;

// Auditoria de Opus, Bloque 6 (T-20): "Investigar todo" vivia como un metodo mas de
// MainViewModel (599 lineas, mezclando navegacion de pestañas, carga/guardado y logica de
// dominio real) - extraido aqui, sin cambiar el comportamiento (mismo criterio real, incluido
// el umbral real de R-1 para vanilla), para que MainViewModel se quede solo con la
// orquestacion (StatusMessage, Research.LoadFrom) y esto con la regla de negocio en si.
public static class ResearchAllService
{
    // R-d/F1 (segunda auditoria de Opus, Fable): "el '9999' crudo en cada chip de Calamity -
    // R-1 elimino el numero sospechoso para vanilla, dejandolo visible en ~2.800 chips de
    // Calamity". Publico para que ResearchRowViewModel pueda reconocer ESTE valor concreto (el
    // placeholder inventado por esta app) y mostrar "Investigado" en vez de un numero sin
    // sentido real - sin tocar ningun OTRO conteo real de Calamity que un personaje real pueda
    // traer de verdad desde el juego (esos SI son datos reales, no un placeholder).
    public const int PlaceholderCount = 9999;

    // Rellena PlrCharacter.Research con una entrada por cada objeto conocido (vanilla +
    // Calamity). Vanilla usa el umbral REAL de cada objeto (VanillaResearchCountCatalog,
    // extraido del TSV real de sacrificios de tModLoader) - Calamity se queda con un
    // placeholder alto (sin tabla real extraida esta pasada, ver el catalogo), que sigue
    // garantizando el desbloqueo completo igual sin fingir un numero real que no se tiene.
    //
    // Segunda auditoria de Opus (Fable), B-3 - BUG REAL encontrado y arreglado: un objeto YA
    // parcialmente investigado (ej. 37 de 100 sacrificios reales) tiene ya su
    // PlrResearchEntry con Count=37 - la version anterior (HashSet.Add devolviendo false)
    // saltaba ese objeto ENTERO, dejandolo en 37 para siempre, aunque el Modo Viaje solo
    // desbloquea con count>=umbral real - seguia bloqueado en el juego mientras el mensaje
    // afirmaba "Investigacion completa aplicada". Ahora se sube (nunca se baja, por si el
    // jugador ya tiene mas de lo necesario) cualquier entrada existente al umbral real, en vez
    // de saltarla.
    public static void Apply(LoadedCharacter loaded, CharacterFileService service)
    {
        // Bucle manual en vez de ToDictionary: un .plr real con un Pid duplicado (dato ajeno,
        // no generado por esta app) no debe reventar "Investigar todo" con una excepcion - el
        // ultimo gana, mismo criterio permisivo que el resto del proyecto con datos externos.
        var existingByPid = new Dictionary<string, PlrResearchEntry>();
        foreach (var entry in loaded.Character.Research) existingByPid[entry.Pid] = entry;

        void EnsureAtLeast(string pid, int count)
        {
            if (existingByPid.TryGetValue(pid, out var entry))
                entry.Count = Math.Max(entry.Count, count);
            else
                loaded.Character.Research.Add(new PlrResearchEntry { Pid = pid, Count = count });
        }

        foreach (var pid in service.VanillaCatalog.AllInternalNames())
        {
            int? id = service.VanillaCatalog.GetIdByKey(pid);
            int count = (id.HasValue ? service.VanillaResearchCounts.Get(id.Value) : null) ?? PlaceholderCount;
            EnsureAtLeast(pid, count);
        }
        foreach (var entry in service.CalamityCatalog.Entries)
            EnsureAtLeast($"{entry.Mod}/{entry.Internal}", PlaceholderCount);
    }
}
