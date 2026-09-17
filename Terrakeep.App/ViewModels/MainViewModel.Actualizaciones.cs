using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServidorKeep.Core.Rutas;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels;

// X1 de I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md (16-sep-2026): "ningun proyecto Keep sabe si es la
// ultima version" - comprobacion real contra la ultima GitHub Release del propio repo publico
// (adriandelgadoalcalde-alt/Terrakeep), en segundo plano, con tolerancia real a que falle (sin
// red, GitHub caido, etc. - ver ComprobadorDeActualizaciones, nunca rompe el arranque ni finge
// "actualizada"). Aviso discreto en la esquina de la ventana (MainWindow.xaml), nunca un popup
// modal - se puede ignorar con un clic y no vuelve a aparecer hasta el siguiente arranque.
//
// IMPORTANTE, mismo criterio ya establecido por RestoreSession() (ver MainViewModel.cs): NUNCA
// se llama desde el constructor - un fichero en disco ya contaminaba las decenas de tests
// headless que construyen un MainViewModel a pelo, y una llamada de RED real lo haria todavia
// peor (tests lentos, no deterministas, dependientes de Internet). MainWindow.xaml.cs es el
// UNICO sitio que la dispara, una vez, sin bloquear la aparicion de la ventana.
public partial class MainViewModel
{
    private const string RepoDeActualizaciones = "Terrakeep";

    [ObservableProperty] private bool _hayActualizacionDisponible;
    [ObservableProperty] private string? _mensajeActualizacion;
    [ObservableProperty] private string? _urlDeActualizacion;

    // Actualizacion en un clic (17-sep-2026, continuacion de X1 - mismo encargo real que ya
    // resolvio Starvekeep del lado compartido, ver ServidorKeep.Core\Rutas\
    // ComprobadorDeActualizaciones.cs y su bitacora "ComprobadorDeActualizaciones: descarga real
    // + instalacion silenciosa" - esta clase solo engancha esa API ya probada (16/16 tests reales
    // en ServidorKeep.Core.Tests) al lado de Terrakeep). Terrakeep es un repo PUBLICO (a
    // diferencia de Starvekeep/ServidorKeep) - repoPrivado siempre false, sin token.
    private ActivoDeRelease? _instaladorDeLaUltimaRelease;

    [ObservableProperty] private bool _actualizandoEnCurso;
    [ObservableProperty] private double _progresoActualizacion;
    [ObservableProperty] private string? _actualizacionEstadoTexto;

    private CancellationTokenSource? _cancelacionActualizacion;

    // La ventana engancha esto al MISMO dialogo real Si/No/Cancelar que ya usa
    // OnWindowClosing/"cargar otro personaje" (ConfirmDiscardChanges de MainWindow.xaml.cs), con
    // "actualizar Terrakeep" como la accion real - mismo patron ya establecido, no uno nuevo.
    public Func<bool>? ConfirmDiscardChangesForUpdate { get; set; }

    // La ventana engancha esto a Application.Current.Shutdown() - se llama SOLO una vez el
    // instalador YA esta lanzado y esperando de verdad a que este proceso muera (ver el
    // comentario real de LanzarInstaladorYRelanzar): cerrar antes dejaria la actualizacion a
    // medias sin que nadie se entere si lanzar el ayudante hubiera fallado.
    public Action? CerrarAppParaActualizar { get; set; }

    public async void IniciarComprobacionDeActualizacion()
    {
        try
        {
            string versionInstalada = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
            var resultado = await ComprobadorDeActualizaciones.ComprobarAsync(RepoDeActualizaciones, versionInstalada);
            if (resultado.Estado != EstadoActualizacionApp.HayActualizacionDisponible) return;

            UrlDeActualizacion = resultado.UrlRelease;
            _instaladorDeLaUltimaRelease = resultado.Instalador;
            MensajeActualizacion = LocalizationService.Instance.Format(
                "update_available", resultado.VersionUltima, resultado.VersionInstalada);
            HayActualizacionDisponible = true;
        }
        catch (Exception)
        {
            // Una comprobacion de version fallida nunca puede ser un error visible de la app.
        }
    }

    [RelayCommand]
    private void AbrirActualizacion()
    {
        if (string.IsNullOrWhiteSpace(UrlDeActualizacion)) return;
        try { Process.Start(new ProcessStartInfo(UrlDeActualizacion) { UseShellExecute = true }); }
        catch (Exception) { /* sin navegador asociado o similar - no es un fallo de Terrakeep */ }
    }

    [RelayCommand]
    private void DescartarActualizacion() => HayActualizacionDisponible = false;

    // Seam de diagnostico (17-sep-2026) - mismo criterio YA real de
    // App.xaml.cs.ShouldForceSoftwareRendering/CharacterFileService.DebugCorruptPlrBytesBeforeVerify:
    // sin InternalsVisibleTo hacia Terrakeep.App.Tests, la unica forma honesta de ejercitar de
    // verdad el flujo completo de "Actualizar ahora" (que en produccion solo se dispara tras una
    // comprobacion REAL contra GitHub que encuentre una version mas nueva) sin publicar una
    // release de prueba real es este metodo publico, nunca llamado desde ningun camino real de
    // la app - solo el arnes lo usa, inyectando un ActivoDeRelease real que apunta a un
    // HttpListener local real en vez de a github.com.
    public void DebugInyectarInstaladorFalso(ActivoDeRelease activo, string mensaje)
    {
        _instaladorDeLaUltimaRelease = activo;
        MensajeActualizacion = mensaje;
        HayActualizacionDisponible = true;
    }

    // Descarga real (con progreso real y verificacion de huella sha256 cuando GitHub la publica -
    // ver DescargarInstaladorAsync) + instalacion silenciosa + relanzado - nunca sin avisar antes
    // si hay cambios sin guardar (mismo IsDirty real que ya usa el resto de la app).
    [RelayCommand]
    private async Task ActualizarAhora()
    {
        if (ActualizandoEnCurso) return;

        if (_instaladorDeLaUltimaRelease is not { } activo)
        {
            ActualizacionEstadoTexto = LocalizationService.Instance["update_no_installer"];
            return;
        }

        if (IsDirty && ConfirmDiscardChangesForUpdate?.Invoke() == false) return;

        string? exePropio = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePropio))
        {
            ActualizacionEstadoTexto = LocalizationService.Instance["update_own_exe_not_found"];
            return;
        }

        _cancelacionActualizacion = new CancellationTokenSource();
        ActualizandoEnCurso = true;
        ProgresoActualizacion = 0;
        ActualizacionEstadoTexto = LocalizationService.Instance.Format("update_downloading", 0);

        try
        {
            var progreso = new Progress<double>(p =>
            {
                ProgresoActualizacion = p;
                ActualizacionEstadoTexto = LocalizationService.Instance.Format("update_downloading", (int)Math.Round(p * 100));
            });

            string carpetaTemporal = Path.Combine(Path.GetTempPath(), "Terrakeep-actualizacion");
            string rutaInstalador = Path.Combine(carpetaTemporal, activo.Nombre);

            await ComprobadorDeActualizaciones.DescargarInstaladorAsync(
                activo, rutaInstalador, repoPrivado: false, token: null,
                progreso, _cancelacionActualizacion.Token);

            ActualizacionEstadoTexto = LocalizationService.Instance["update_installing"];
            ComprobadorDeActualizaciones.LanzarInstaladorYRelanzar(rutaInstalador, exePropio);

            // El instalador ya esta lanzado y esperando de verdad a que ESTE proceso termine -
            // cerrar ahora, nunca antes (ver el comentario real de CerrarAppParaActualizar).
            CerrarAppParaActualizar?.Invoke();
        }
        catch (OperationCanceledException)
        {
            ActualizacionEstadoTexto = LocalizationService.Instance["update_cancelled"];
            ActualizandoEnCurso = false;
        }
        catch (Exception ex)
        {
            ActualizacionEstadoTexto = LocalizationService.Instance.Format("update_failed", ex.Message);
            ActualizandoEnCurso = false;
        }
        finally
        {
            _cancelacionActualizacion?.Dispose();
            _cancelacionActualizacion = null;
        }
    }

    [RelayCommand]
    private void CancelarActualizacion() => _cancelacionActualizacion?.Cancel();
}
