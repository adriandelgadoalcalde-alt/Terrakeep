using System.IO;
using System.Runtime.CompilerServices;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// SEGURIDAD (revisor visual r2 de la FASE D, 28-sep-2026): estos tests xunit NO pasan por el arnes
// (Terrakeep.App.Tests) y, sin App.ModoDiagnostico, cualquier MainViewModel/BackupHistoryService que guardaba un
// .plr temporal creaba su historial en la carpeta REAL %LOCALAPPDATA%\Terrakeep\Backups (medido: entre las 20:47 y
// las 20:50 de ese dia, cabecera-test-*, loadouts-test-*, tanda1-*, savetest-*, calamity-badge-* y adrian-*; ~480
// carpetas adrian-* acumuladas de ejecuciones anteriores).
//
// Inicializador de MODULO: corre una vez al cargar este ensamblado, antes de cualquier test. Redirige TODA la carpeta
// de estado de la app (CarpetaEstadoApp) a un temporal propio del proceso y lo borra al salir; la guarda
// (AlEscribirFueraDePrueba) cuenta cualquier escritura que aun apunte a la carpeta real - la bloquea
// CarpetaEstadoApp.PermiteEscribir y el test AislamientoEstadoTests.NingunaEscrituraSeSaleDelTemporal lo exige en 0.
// App.ModoDiagnostico se enciende SOLO para poder fijar la redireccion y se deja como estaba (varios tests lo
// alternan a proposito).
internal static class AislamientoEstadoDeTests
{
    internal static string Carpeta { get; private set; } = "";
    internal static int EscriturasBloqueadas;
    internal static readonly List<string> RutasBloqueadas = [];

    [ModuleInitializer]
    internal static void Iniciar()
    {
        Carpeta = Path.Combine(Path.GetTempPath(), $"TerrakeepVmTests-estado-{Environment.ProcessId}");
        Directory.CreateDirectory(Carpeta);
        bool previo = App.ModoDiagnostico;
        App.ModoDiagnostico = true;
        try
        {
            CarpetaEstadoApp.CarpetaDePrueba = Carpeta;
            CarpetaEstadoApp.AlEscribirFueraDePrueba = ruta =>
            {
                Interlocked.Increment(ref EscriturasBloqueadas);
                lock (RutasBloqueadas) RutasBloqueadas.Add(ruta);
            };
        }
        finally { App.ModoDiagnostico = previo; }
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { if (Directory.Exists(Carpeta)) Directory.Delete(Carpeta, recursive: true); } catch { /* best-effort */ }
        };
    }
}

public sealed class AislamientoEstadoTests
{
    [Fact]
    public void LaCarpetaDeEstadoDeLosTestsEsUnTemporalPropio()
    {
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), CarpetaEstadoApp.Carpeta, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(CarpetaEstadoApp.CarpetaReal, CarpetaEstadoApp.Carpeta, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith(CarpetaEstadoApp.Carpeta, new BackupHistoryService().BackupsRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnaEscrituraHaciaLaCarpetaRealSeBloquea()
    {
        int antes = AislamientoEstadoDeTests.EscriturasBloqueadas;
        bool permite = CarpetaEstadoApp.PermiteEscribir(Path.Combine(CarpetaEstadoApp.CarpetaReal, "Backups", "sonda-guarda"));
        Assert.False(permite);
        Assert.Equal(antes + 1, AislamientoEstadoDeTests.EscriturasBloqueadas);
        lock (AislamientoEstadoDeTests.RutasBloqueadas) AislamientoEstadoDeTests.RutasBloqueadas.RemoveAll(r => r.EndsWith("sonda-guarda"));
        Interlocked.Decrement(ref AislamientoEstadoDeTests.EscriturasBloqueadas);
    }

    // Guardar un .plr temporal de verdad crea su historial en el TEMPORAL, nunca en la carpeta real.
    [Fact]
    public void GuardarUnaCopiaDeSeguridadVaAlTemporal()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"vmtests-guarda-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            string plr = Path.Combine(dir, "guarda-test.plr");
            File.WriteAllBytes(plr, [1, 2, 3, 4]);
            var entrada = new BackupHistoryService().SaveBackup(plr, null, BackupReason.Manual);
            Assert.NotNull(entrada);
            Assert.StartsWith(CarpetaEstadoApp.Carpeta, entrada!.ContainerPath, StringComparison.OrdinalIgnoreCase);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
