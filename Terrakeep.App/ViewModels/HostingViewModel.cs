using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServidorKeep.Core.Configuracion;
using ServidorKeep.Core.Instancias;
using ServidorKeep.Core.Motores;
using ServidorKeep.Core.Rutas;

namespace Terrakeep.App.ViewModels;

// Fase B (15-sep-2026): una instancia de servidor de Terraria/tModLoader YA en marcha, envuelta
// para la UI - lee TODO de InstanciaServidor real (ServidorKeep.Core), nunca simula nada. Los
// eventos de InstanciaServidor (CambioDeEstado/LineaDeLog/MuestraDeRecursos) llegan desde un hilo
// de fondo (Timer de muestreo, hilo de lectura de stdout redirigido) - se marshalan al hilo de UI
// SOLO si hay un Dispatcher real detras (Application.Current puede ser null en un arnes headless,
// mismo criterio "MainViewModel es headless de verdad" ya documentado en este proyecto).
public sealed partial class HostingInstanciaViewModel : ObservableObject
{
    public InstanciaServidor Nucleo { get; }
    private readonly Dispatcher? _dispatcher;

    public HostingInstanciaViewModel(InstanciaServidor nucleo)
    {
        Nucleo = nucleo;
        _dispatcher = System.Windows.Application.Current?.Dispatcher;

        EstadoTexto = TextoDeEstado(nucleo.Estado);
        EstadoColorKey = ColorDeEstado(nucleo.Estado);

        nucleo.CambioDeEstado += OnCambioDeEstado;
        nucleo.LineaDeLog += OnLineaDeLog;
        nucleo.MuestraDeRecursos += OnMuestraDeRecursos;
    }

    public string Nombre => Nucleo.Nombre;
    public int Puerto => Nucleo.Puerto;
    public string CarpetaInstancia => Nucleo.CarpetaInstancia;

    [ObservableProperty] private string _estadoTexto = "";
    [ObservableProperty] private string _estadoColorKey = "TextSecondaryBrush";
    [ObservableProperty] private string _recursosTexto = "CPU — · RAM —";
    // Ultimas lineas del log real (SeguidorDeLog ya sigue el archivo tal cual lo escribe el
    // propio proceso) - tope razonable para no acumular memoria sin fin en una sesion larga.
    public ObservableCollection<string> UltimasLineas { get; } = [];
    private const int MaxLineasVisibles = 200;

    [RelayCommand]
    private async Task Detener() => await Nucleo.DetenerAsync();

    private void OnCambioDeEstado(EstadoInstancia nuevo) => Marshal(() =>
    {
        EstadoTexto = TextoDeEstado(nuevo);
        EstadoColorKey = ColorDeEstado(nuevo);
    });

    private void OnLineaDeLog(string linea) => Marshal(() =>
    {
        UltimasLineas.Add(linea);
        while (UltimasLineas.Count > MaxLineasVisibles) UltimasLineas.RemoveAt(0);
    });

    private void OnMuestraDeRecursos(MuestraRecursos m) => Marshal(() =>
        RecursosTexto = $"CPU {m.CpuPorcentaje:0.#}%  ·  RAM {m.MemoriaMB:0} MB  ·  {(m.DiscoBps + m.RedAproxBps) / 1024.0:0.#} KB/s E/S");

    private void Marshal(Action accion)
    {
        if (_dispatcher == null || _dispatcher.CheckAccess()) accion();
        else _dispatcher.BeginInvoke(accion);
    }

    private static string TextoDeEstado(EstadoInstancia e) => e switch
    {
        EstadoInstancia.Arrancando => "Arrancando…",
        EstadoInstancia.EnEscucha => "En escucha",
        EstadoInstancia.Detenida => "Detenida",
        EstadoInstancia.Fallida => "Fallida",
        _ => e.ToString(),
    };

    // Mismo lenguaje de color que ya usa ServidorKeep.App (verde=en escucha, ambar=arrancando,
    // rojo=fallida, gris=detenida) - claves de Terrakeep\Styles\Theme.xaml, no colores nuevos.
    private static string ColorDeEstado(EstadoInstancia e) => e switch
    {
        EstadoInstancia.EnEscucha => "EquippedGreenBrush",
        EstadoInstancia.Arrancando => "OrangeBrush",
        EstadoInstancia.Fallida => "CalamityBrush",
        _ => "TextSecondaryBrush",
    };
}

// Una casilla de "mod real disponible" para el selector de tModLoader.
public sealed partial class HostingModViewModel(ModDisponible mod) : ObservableObject
{
    public string Nombre => mod.NombreInterno;
    public string TamanoTexto => $"{mod.TamanoBytes / 1024.0 / 1024.0:0.#} MB";
    [ObservableProperty] private bool _seleccionado;
}

// Fase B (15-sep-2026): pestaña "Servidor" pedida explicitamente por el usuario - lanzar y
// gestionar un servidor dedicado REAL de Terraria/tModLoader sin salir de Terrakeep, reutilizando
// ServidorKeep.Core.Motores.MotorServidorTerraria TAL CUAL (Job Objects, serverconfig.txt,
// Firewall automatico, mods reales) - ver el ProjectReference en Terrakeep.App.csproj para el
// porque de referenciar el proyecto hermano en vez de reimplementar su logica.
//
// Alcance de esta ronda, a proposito: SOLO Terraria/tModLoader (lo que pidio el encargo, "un
// servidor de Terraria"). ServidorKeep.Core ya soporta Don't Starve Together con la MISMA forma
// (ConfiguracionServidorDST/MotorServidorDST, simetrico a los tipos de aqui) - añadirlo a esta
// pestaña es una extension acotada para una ronda futura si Starvekeep pide lo mismo, no un hueco
// de esfuerzo de esta (ver bitacora.md).
public sealed partial class HostingViewModel : ObservableObject
{
    private readonly RegistroInstancias _registro = new();

    public HostingViewModel()
    {
        var terraria = DetectorInstalaciones.Terraria();
        var tModLoader = DetectorInstalaciones.TModLoader();
        TerrariaDetectado = terraria.Encontrada;
        TModLoaderDetectado = tModLoader.Encontrada;
        MensajeDeteccion = terraria.Encontrada
            ? null
            : terraria.Motivo ?? "No se encontró una instalación real de Terraria.";

        RefrescarModsDisponibles();
    }

    // ---- Configuracion editable por el usuario --------------------------------------------
    [ObservableProperty] private string _nombreInstancia = "Mi servidor de Terraria";
    [ObservableProperty] private bool _usarTModLoader;
    [ObservableProperty] private string _nombreMundo = "Mundo de Terrakeep";
    [ObservableProperty] private TamanoMundoTerraria _tamanoMundo = TamanoMundoTerraria.Mediano;
    [ObservableProperty] private DificultadTerraria _dificultad = DificultadTerraria.Normal;
    [ObservableProperty] private int _maxJugadores = 8;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private int _puerto = 7777;

    public ObservableCollection<HostingModViewModel> ModsDisponibles { get; } = [];

    partial void OnUsarTModLoaderChanged(bool value) => RefrescarModsDisponibles();

    private void RefrescarModsDisponibles()
    {
        ModsDisponibles.Clear();
        if (!UsarTModLoader) return;
        foreach (var mod in CatalogoDeMods.Listar())
            ModsDisponibles.Add(new HostingModViewModel(mod));
    }

    // ---- Estado real de deteccion/instancias -----------------------------------------------
    public bool TerrariaDetectado { get; }
    public bool TModLoaderDetectado { get; }
    public string? MensajeDeteccion { get; }
    public ObservableCollection<HostingInstanciaViewModel> Instancias { get; } = [];

    [ObservableProperty] private string? _errorMessage;

    private bool PuedeIniciar => !UsarTModLoader ? TerrariaDetectado : TModLoaderDetectado;

    [RelayCommand]
    private void Iniciar()
    {
        ErrorMessage = null;
        try
        {
            var cfg = new ConfiguracionServidorTerraria
            {
                NombreInstancia = string.IsNullOrWhiteSpace(NombreInstancia) ? "Mi servidor de Terraria" : NombreInstancia,
                UsarTModLoader = UsarTModLoader,
                NombreMundo = string.IsNullOrWhiteSpace(NombreMundo) ? "Mundo de Terrakeep" : NombreMundo,
                TamanoMundo = TamanoMundo,
                Dificultad = Dificultad,
                MaxJugadores = Math.Clamp(MaxJugadores, 1, 8),
                Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
                Puerto = Puerto,
                ModsSeleccionados = [.. ModsDisponibles.Where(m => m.Seleccionado).Select(m => m.Nombre)],
            };

            var instancia = MotorServidorTerraria.Lanzar(cfg, _registro);
            Instancias.Add(new HostingInstanciaViewModel(instancia));
        }
        catch (Exception ex)
        {
            // Mismo criterio honesto del resto de Terrakeep: un fallo real (instalacion no
            // encontrada, puerto irrecuperable...) se enseña tal cual, nunca se traga en
            // silencio ni se disimula como si el servidor hubiera arrancado.
            ErrorMessage = ex.Message;
        }
    }
}
