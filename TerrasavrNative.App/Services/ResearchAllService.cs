using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.Services;

// Auditoria de Opus, Bloque 6 (T-20): "Investigar todo" vivia como un metodo mas de
// MainViewModel (599 lineas, mezclando navegacion de pestañas, carga/guardado y logica de
// dominio real) - extraido aqui, sin cambiar el comportamiento (mismo criterio real, incluido
// el umbral real de R-1 para vanilla), para que MainViewModel se quede solo con la
// orquestacion (StatusMessage, Research.LoadFrom) y esto con la regla de negocio en si.
public static class ResearchAllService
{
    // Rellena PlrCharacter.Research con una entrada por cada objeto conocido (vanilla +
    // Calamity) que todavia no estuviera investigado. Vanilla usa el umbral REAL de cada
    // objeto (VanillaResearchCountCatalog, extraido del TSV real de sacrificios de tModLoader) -
    // Calamity se queda con un placeholder alto (sin tabla real extraida esta pasada, ver el
    // catalogo), que sigue garantizando el desbloqueo completo igual sin fingir un numero real
    // que no se tiene.
    public static void Apply(LoadedCharacter loaded, CharacterFileService service)
    {
        const int placeholderCount = 9999;
        var existingPids = new HashSet<string>(loaded.Character.Research.Select(e => e.Pid));

        foreach (var pid in service.VanillaCatalog.AllInternalNames())
        {
            if (existingPids.Add(pid))
            {
                int? id = service.VanillaCatalog.GetIdByKey(pid);
                int count = (id.HasValue ? service.VanillaResearchCounts.Get(id.Value) : null) ?? placeholderCount;
                loaded.Character.Research.Add(new PlrResearchEntry { Pid = pid, Count = count });
            }
        }
        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string pid = $"{entry.Mod}/{entry.Internal}";
            if (existingPids.Add(pid))
                loaded.Character.Research.Add(new PlrResearchEntry { Pid = pid, Count = placeholderCount });
        }
    }
}
