using System.Diagnostics;
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

    public async void IniciarComprobacionDeActualizacion()
    {
        try
        {
            string versionInstalada = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
            var resultado = await ComprobadorDeActualizaciones.ComprobarAsync(RepoDeActualizaciones, versionInstalada);
            if (resultado.Estado != EstadoActualizacionApp.HayActualizacionDisponible) return;

            UrlDeActualizacion = resultado.UrlRelease;
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
}
