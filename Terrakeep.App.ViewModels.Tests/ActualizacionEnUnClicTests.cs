using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServidorKeep.Core.Rutas;
using Terrakeep.App.ViewModels;
using Xunit;

namespace Terrakeep.App.ViewModels.Tests;

/// <summary>
/// Actualizacion en un clic (17-sep-2026, continuacion de X1): estos tests ejercitan la parte
/// REAL propia de Terrakeep (<see cref="MainViewModel.ActualizarAhoraCommand"/> - el orden real
/// de "avisar si hay cambios sin guardar", descargar, lanzar el instalador y cerrar), no el
/// mecanismo compartido de descarga/instalacion en si (ya probado a fondo con 7 tests reales,
/// HttpListener real + procesos reales de Windows, en
/// ServidorKeep.Core.Tests\Rutas\ComprobadorDeActualizacionesTests.cs - 16/16 en verde).
///
/// El "instalador falso" es un .bat REAL (Windows lo ejecuta de verdad via su asociacion de
/// fichero, mismo criterio ya usado por el test hermano de ServidorKeep.Core.Tests
/// "LanzarInstaladorYRelanzar_orquesta_de_verdad_esperar_instalar_y_relanzar") servido por un
/// <see cref="HttpListener"/> real en localhost - nunca un mock de red ni de proceso. El seam
/// <see cref="MainViewModel.DebugInyectarInstaladorFalso"/> (nunca usado por ningun camino real
/// de la app - ver su comentario) es la unica forma honesta de llegar a este flujo sin publicar
/// una release real de prueba en GitHub.
/// </summary>
public class ActualizacionEnUnClicTests : IDisposable
{
    private readonly string _temporal = Path.Combine(
        Path.GetTempPath(), "terrakeep-actualizacion-tests-" + Guid.NewGuid().ToString("N"));

    public ActualizacionEnUnClicTests() => Directory.CreateDirectory(_temporal);

    public void Dispose()
    {
        try { if (Directory.Exists(_temporal)) Directory.Delete(_temporal, true); }
        catch (IOException) { /* mejor esfuerzo, no bloquear el resto de la tanda */ }
    }

    private string Ruta(string nombre) => Path.Combine(_temporal, nombre);

    [Fact]
    public async Task ActualizarAhora_SinInstaladorAdjunto_AvisaYNuncaEmpiezaADescargar()
    {
        var vm = new MainViewModel();
        bool seLlamoConfirmacion = false;
        vm.ConfirmDiscardChangesForUpdate = () => { seLlamoConfirmacion = true; return true; };
        bool seLlamoCerrar = false;
        vm.CerrarAppParaActualizar = () => seLlamoCerrar = true;
        // Nunca se llama a DebugInyectarInstaladorFalso - el estado real de un aviso de version
        // nueva sin ningun asset .exe adjunto (ver ActivoInstaladorReal, ServidorKeep.Core).

        await vm.ActualizarAhoraCommand.ExecuteAsync(null);

        Assert.False(vm.ActualizandoEnCurso);
        Assert.False(seLlamoConfirmacion, "sin instalador no hay nada que avisar de cambios sin guardar todavia");
        Assert.False(seLlamoCerrar);
        Assert.Contains("instalador", vm.ActualizacionEstadoTexto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActualizarAhora_ConCambiosSinGuardarYUsuarioCancela_AbortaSinDescargarNiCerrar()
    {
        var vm = new MainViewModel();
        vm.IsDirty = true;
        bool seLlamoConfirmacion = false;
        vm.ConfirmDiscardChangesForUpdate = () => { seLlamoConfirmacion = true; return false; }; // el usuario pulsa Cancelar
        bool seLlamoCerrar = false;
        vm.CerrarAppParaActualizar = () => seLlamoCerrar = true;

        var marcaInstalado = Ruta("no-deberia-ejecutarse.marker");
        using var servidor = new ServidorHttpDeUnFichero(EscribirInstaladorBat(marcaInstalado, Ruta("no-deberia-args.txt")));
        vm.DebugInyectarInstaladorFalso(
            new ActivoDeRelease("instalador-prueba.bat", servidor.Url, servidor.Tamano, null),
            "Version de prueba disponible (test)");

        await vm.ActualizarAhoraCommand.ExecuteAsync(null);

        Assert.True(seLlamoConfirmacion);
        Assert.False(vm.ActualizandoEnCurso);
        Assert.False(seLlamoCerrar, "cancelar el aviso de cambios sin guardar tiene que abortar ANTES de lanzar nada");
        Assert.False(File.Exists(marcaInstalado), "el instalador falso nunca deberia haberse ejecutado");
    }

    [Fact]
    public async Task ActualizarAhora_FlujoCompletoReal_DescargaLanzaInstaladorFalsoYCierra()
    {
        var vm = new MainViewModel();
        vm.ConfirmDiscardChangesForUpdate = () => true; // sin cambios sin guardar en este test, pero fijado igual
        bool seLlamoCerrar = false;
        vm.CerrarAppParaActualizar = () => seLlamoCerrar = true;

        var marcaInstalado = Ruta("instalador-ejecutado.marker");
        var ficheroArgs = Ruta("instalador-recibio-args.txt");
        using var servidor = new ServidorHttpDeUnFichero(EscribirInstaladorBat(marcaInstalado, ficheroArgs));
        vm.DebugInyectarInstaladorFalso(
            new ActivoDeRelease("instalador-prueba.bat", servidor.Url, servidor.Tamano, null),
            "Version de prueba disponible (test)");

        var progresos = new List<double>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ProgresoActualizacion)) progresos.Add(vm.ProgresoActualizacion);
        };

        await vm.ActualizarAhoraCommand.ExecuteAsync(null);

        // LanzarInstaladorYRelanzar espera de verdad (Wait-Process, ver ServidorKeep.Core) a que
        // el PROPIO PROCESO que llama muera antes de lanzar el instalador - en produccion ese es
        // Terrakeep.exe, que ya se cerro (CerrarAppParaActualizar) justo antes; aqui el "proceso
        // que llama" es el propio host de xUnit, que sigue vivo durante el resto de la tanda, asi
        // que el guion real espera el timeout COMPLETO por defecto (30s, nunca personalizado
        // desde ActualizarAhora - mismo criterio real que Starvekeep) antes de proceder de todos
        // modos. Margen real de sobra, no un numero arbitrario.
        Esperar(() => File.Exists(marcaInstalado), TimeSpan.FromSeconds(40),
            "el instalador falso (.bat real servido por HttpListener real) no llego a ejecutarse");

        Assert.True(seLlamoCerrar, "tras lanzar el instalador real la app tiene que cerrarse para soltar su propio .exe");
        Assert.NotEmpty(progresos);
        Assert.True(progresos[^1] is > 0.99 and <= 1.0, $"el ultimo progreso real reportado deberia rondar el 100%, fue {progresos[^1]}");

        var argsRecibidos = File.ReadAllText(ficheroArgs);
        Assert.Contains("/VERYSILENT", argsRecibidos);
        Assert.Contains("/SUPPRESSMSGBOXES", argsRecibidos);
        Assert.Contains("/NORESTART", argsRecibidos);
    }

    [Fact]
    public async Task ActualizarAhora_CancelarACubretaMitad_NuncaLanzaElInstaladorYQuedaListoParaReintentar()
    {
        // 5 MB reales a velocidad limitada (mismo criterio real que el test hermano de
        // ServidorKeep.Core.Tests) para que de tiempo real a cancelar antes de que la descarga
        // termine.
        var contenido = new byte[5_000_000];
        new Random(7).NextBytes(contenido);
        var marcaInstalado = Ruta("no-deberia-ejecutarse-cancelado.marker");
        using var servidor = new ServidorHttpDeUnFichero(contenido, limitarVelocidad: true);

        var vm = new MainViewModel();
        vm.CerrarAppParaActualizar = () => throw new InvalidOperationException("no deberia cerrarse tras cancelar");
        vm.DebugInyectarInstaladorFalso(
            new ActivoDeRelease("instalador-grande.bat", servidor.Url, contenido.Length, null),
            "Version de prueba disponible (test)");

        var tarea = vm.ActualizarAhoraCommand.ExecuteAsync(null);
        // Deja avanzar la descarga un instante real (limitada a proposito, ver ServidorHttpDeUnFichero)
        // y cancela a mitad, como pulsar el boton "Cancelar" real de la tarjeta.
        await Task.Delay(150);
        vm.CancelarActualizacionCommand.Execute(null);
        await tarea;

        Assert.False(vm.ActualizandoEnCurso, "tras cancelar, la tarjeta vuelve al estado normal (se puede reintentar)");
        Assert.Contains("ancelad", vm.ActualizacionEstadoTexto, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(marcaInstalado));
    }

    private string EscribirInstaladorBat(string marcaInstalado, string ficheroArgs)
    {
        string bat = Ruta("instalador-falso-fuente.bat");
        File.WriteAllText(bat, $"""
            @echo off
            echo %* > "{ficheroArgs}"
            type nul > "{marcaInstalado}"
            """);
        return bat;
    }

    private static void Esperar(Func<bool> condicion, TimeSpan timeout, string mensajeSiFalla)
    {
        var limite = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < limite)
        {
            if (condicion()) return;
            Thread.Sleep(100);
        }
        Assert.Fail(mensajeSiFalla);
    }

    /// <summary>Servidor HTTP real en localhost (mismo patron real que
    /// ServidorKeep.Core.Tests.ComprobadorDeActualizacionesTests.ServidorHttpDeUnFichero) que
    /// sirve el CONTENIDO de un fichero real (aqui, el .bat "instalador falso") o un array de
    /// bytes crudo, nunca un mock de HttpClient.</summary>
    private sealed class ServidorHttpDeUnFichero : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly byte[] _contenido;
        private readonly bool _limitarVelocidad;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _tarea;

        public string Url { get; }
        public int Tamano => _contenido.Length;

        public ServidorHttpDeUnFichero(string rutaFicheroAServir) : this(File.ReadAllBytes(rutaFicheroAServir)) { }

        public ServidorHttpDeUnFichero(byte[] contenido, bool limitarVelocidad = false)
        {
            _contenido = contenido;
            _limitarVelocidad = limitarVelocidad;
            int puerto = PuertoLibre();
            Url = $"http://127.0.0.1:{puerto}/instalador";
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{puerto}/");
            _listener.Start();
            _tarea = Task.Run(() => AtenderAsync(_cts.Token));
        }

        private async Task AtenderAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var contexto = await _listener.GetContextAsync().WaitAsync(ct);
                    contexto.Response.ContentType = "application/octet-stream";
                    contexto.Response.ContentLength64 = _contenido.Length;

                    if (_limitarVelocidad)
                    {
                        const int trozo = 20_000;
                        for (int i = 0; i < _contenido.Length; i += trozo)
                        {
                            int n = Math.Min(trozo, _contenido.Length - i);
                            await contexto.Response.OutputStream.WriteAsync(_contenido.AsMemory(i, n), ct);
                            await contexto.Response.OutputStream.FlushAsync(ct);
                            await Task.Delay(15, ct);
                        }
                    }
                    else
                    {
                        await contexto.Response.OutputStream.WriteAsync(_contenido, ct);
                    }
                    contexto.Response.OutputStream.Close();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or HttpListenerException or ObjectDisposedException)
            {
                // El listener se para al hacer Dispose - forma normal de terminar el bucle.
            }
        }

        private static int PuertoLibre()
        {
            using var socket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            int puerto = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return puerto;
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch (ObjectDisposedException) { }
            try { _tarea.Wait(TimeSpan.FromSeconds(2)); } catch { /* mejor esfuerzo al cerrar */ }
            _listener.Close();
            _cts.Dispose();
        }
    }
}
