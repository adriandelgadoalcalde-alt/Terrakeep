using System;
using System.IO;

namespace Terrakeep.App.ViewModels.Tests;

// Saneado de privacidad (29-sep-2026, antes de publicar la 3.3.0 en el repo PUBLICO): los tests
// "*RealTests*"/"*RealFileCanario*" de este proyecto leen datos reales de la maquina de
// desarrollo (personajes de Terraria/tModLoader, el propio repo) - SOLO LECTURA, nunca escritura
// sobre el original. Antes incrustaban la ruta absoluta completa con el nombre de usuario de
// Windows ("C:\Users\adrian\...") en el codigo fuente publico; este helper la calcula en tiempo
// de ejecucion (misma ruta real en esta maquina, pero sin el nombre de usuario en el repo) y
// admite una variable de entorno de anulacion por si hace falta apuntar a otra maquina/perfil sin
// tocar codigo.
internal static class RutasEntornoReal
{
    /// <summary>
    /// %USERPROFILE%\Documents\My Games\Terraria\&lt;relativo&gt; - mundos/personajes reales de
    /// Terraria/tModLoader usados como datos de prueba de solo lectura.
    /// </summary>
    public static string Documentos(string relativo)
    {
        string raiz = Environment.GetEnvironmentVariable("TERRAKEEP_TEST_DOCUMENTOS_TERRARIA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
        return Path.Combine(raiz, relativo);
    }

    /// <summary>
    /// Raiz de este mismo repo (Terrakeep/Terrasavr-Native), localizada subiendo desde
    /// <see cref="AppContext.BaseDirectory"/> hasta encontrar Terrakeep.slnx.
    /// </summary>
    public static string Repo(string relativo)
    {
        string? raizEnv = Environment.GetEnvironmentVariable("TERRAKEEP_TEST_REPO_ROOT");
        if (raizEnv is not null) return Path.Combine(raizEnv, relativo);

        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Terrakeep.slnx")))
            dir = dir.Parent;
        if (dir is null)
            throw new DirectoryNotFoundException("No se encontro Terrakeep.slnx subiendo desde " + AppContext.BaseDirectory);
        return Path.Combine(dir.FullName, relativo);
    }

    /// <summary>
    /// %USERPROFILE%\Downloads\Keep\&lt;relativo&gt; - repos hermanos de la familia Keep
    /// (Terrasavr-Calamity-Beta, KeepQA...) usados como datos/herramientas de prueba.
    /// </summary>
    public static string HermanoKeep(string relativo)
    {
        string raiz = Environment.GetEnvironmentVariable("TERRAKEEP_TEST_KEEP_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Keep");
        return Path.Combine(raiz, relativo);
    }
}
