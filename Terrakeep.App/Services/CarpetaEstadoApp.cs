using System.IO;

namespace Terrakeep.App.Services;

// Carpeta de ESTADO de la app (%LOCALAPPDATA%\Terrakeep): session.json, settings.json, window.json,
// world_view_state.json, la cache del catalogo de la Libreria y el historial de versiones (Backups).
// Antes cada servicio calculaba su propia ruta con un "static readonly" suelto.
//
// Motivo (incidente real, 28-sep-2026, revisor visual de la FASE D del responsive global): el arnes
// Terrakeep.App.Tests guardaba session.json y lo restauraba al salir (ProcessExit), pero la app lo
// volvia a escribir ~4 s DESPUES de "restaurado", dejandolo apuntando a una copia temporal ya borrada;
// y las ejecuciones con cambio de idioma dejaban settings.json en "Language":"en". Restaurar a
// posteriori siempre llega tarde contra una escritura tardia. Solucion de fondo, el mismo patron que
// las partidas (CharacterFileService.CarpetasPersonajesDePrueba/CarpetasMundosDePrueba): en modo
// diagnostico el arnes REDIRIGE la carpeta entera a su carpeta temporal y la app nunca abre para
// escribir la real.
//
//   - CarpetaDePrueba: solo se puede fijar con App.ModoDiagnostico=true. En la app real nada la activa,
//     Carpeta es siempre la real y el comportamiento no cambia en nada.
//   - PermiteEscribir(ruta): lo llaman TODOS los puntos de escritura de estado. Con la carpeta
//     redirigida, cualquier escritura que siga cayendo dentro de la carpeta REAL (una ruta calculada
//     antes de redirigir, un servicio nuevo que se salte Ruta()...) se bloquea y se avisa a
//     AlEscribirFueraDePrueba (el arnes imprime "FALLO: AISLAMIENTO-ESTADO"). Devuelve false para que el
//     llamante no escriba: los servicios capturan cualquier excepcion ("best-effort"), asi que una
//     excepcion aqui se tragaria en silencio.
public static class CarpetaEstadoApp
{
    public static string CarpetaReal { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep");

    private static string? _carpetaDePrueba;
    private static Action<string>? _alEscribirFueraDePrueba;

    public static string? CarpetaDePrueba
    {
        get => _carpetaDePrueba;
        set
        {
            if (value != null && !App.ModoDiagnostico)
                throw new InvalidOperationException("CarpetaEstadoApp.CarpetaDePrueba solo puede usarse en modo diagnostico (arnes de pruebas).");
            _carpetaDePrueba = value == null ? null : Path.GetFullPath(value);
        }
    }

    public static Action<string>? AlEscribirFueraDePrueba
    {
        get => _alEscribirFueraDePrueba;
        set
        {
            if (value != null && !App.ModoDiagnostico)
                throw new InvalidOperationException("CarpetaEstadoApp.AlEscribirFueraDePrueba solo puede usarse en modo diagnostico (arnes de pruebas).");
            _alEscribirFueraDePrueba = value;
        }
    }

    /// <summary>Carpeta de estado en uso: la temporal del arnes si esta redirigida, si no la real.</summary>
    public static string Carpeta => _carpetaDePrueba ?? CarpetaReal;

    public static string Ruta(string nombre) => Path.Combine(Carpeta, nombre);

    /// <summary>Guarda de escritura. Con la carpeta redirigida, false (y aviso) si la ruta cae dentro de
    /// la carpeta de estado REAL. Sin redirigir (app real), siempre true.</summary>
    public static bool PermiteEscribir(string ruta)
    {
        if (_carpetaDePrueba == null) return true;
        if (!EstaDentro(ruta, CarpetaReal) || EstaDentro(ruta, _carpetaDePrueba)) return true;
        _alEscribirFueraDePrueba?.Invoke(ruta);
        return false;
    }

    private static bool EstaDentro(string ruta, string raiz)
    {
        string r = Path.GetFullPath(raiz).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string c = Path.GetFullPath(ruta);
        return c.StartsWith(r, StringComparison.OrdinalIgnoreCase)
               || string.Equals(c.TrimEnd(Path.DirectorySeparatorChar), r.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
