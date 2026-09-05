using System.IO;
using System.Text.Json;
using System.Windows;

namespace TerrasavrNative.App.Services;

// Auditoria de Opus, Bloque 4 (T-3): "cada arranque vuelve al tamaño de fabrica, aunque el
// usuario ya hubiera ajustado la ventana a su gusto la ultima vez" - contra P4 (armonia real a
// cualquier tamaño deberia incluir RECORDAR el tamaño real que el usuario eligio, no solo
// adaptarse bien a cualquiera). Un unico fichero JSON real y pequeño en AppData (config de la
// app, no dato de personaje/mundo - no pinta nada en Documents\My Games\Terraria) - se guarda
// SIEMPRE el tamaño/posicion RESTAURADO (RestoreBounds), nunca el maximizado, para que
// desmaximizar despues no deje al usuario con una ventana del tamaño entero de la pantalla.
public sealed class WindowPlacementInfo
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
    // Pedido explicito del usuario (5-sep-2026): ademas de recordar SIEMPRE el ultimo tamaño
    // (arriba, sin cambios), poder FIJAR uno concreto como el de arranque, con un tick en
    // Ajustes que se puede activar/desactivar - independiente de que el usuario siga
    // redimensionando la ventana libremente en la sesion (eso solo actualiza Left/Top/Width/
    // Height de arriba, nunca los campos Pinned* mientras el tick este activo).
    public bool Pinned { get; set; }
    public double PinnedLeft { get; set; }
    public double PinnedTop { get; set; }
    public double PinnedWidth { get; set; }
    public double PinnedHeight { get; set; }
}

public static class WindowPlacementService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "window.json");

    public static void Apply(Window window)
    {
        WindowPlacementInfo info;
        try
        {
            if (!File.Exists(FilePath)) return;
            info = JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(FilePath)) ?? throw new InvalidDataException();
        }
        catch (Exception)
        {
            return; // fichero ausente/corrupto - se queda con el tamaño de fabrica, nunca revienta el arranque por esto
        }

        // Pedido explicito del usuario: con el tick de Ajustes activo, arrancar SIEMPRE con el
        // tamaño/posicion fijados (nunca maximizado - fijar un tamaño concreto y luego arrancar
        // maximizado no tendria sentido), ignorando el ultimo tamaño real de esta sesion.
        double rawWidth = info.Pinned ? info.PinnedWidth : info.Width;
        double rawHeight = info.Pinned ? info.PinnedHeight : info.Height;
        double rawLeft = info.Pinned ? info.PinnedLeft : info.Left;
        double rawTop = info.Pinned ? info.PinnedTop : info.Top;

        // Nunca restaurar fuera de la pantalla real (un monitor desconectado desde la ultima
        // sesion dejaria la ventana inalcanzable) - clamp real contra el area virtual de
        // TODOS los monitores conectados ahora mismo, y nunca por debajo de MinWidth/MinHeight.
        double width = Math.Max(window.MinWidth, Math.Min(rawWidth, SystemParameters.VirtualScreenWidth));
        double height = Math.Max(window.MinHeight, Math.Min(rawHeight, SystemParameters.VirtualScreenHeight));
        double left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(rawLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - width));
        double top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(rawTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - height));

        window.Width = width;
        window.Height = height;
        window.Left = left;
        window.Top = top;
        if (info.IsMaximized && !info.Pinned) window.WindowState = WindowState.Maximized;
    }

    public static bool IsPinned()
    {
        try
        {
            if (!File.Exists(FilePath)) return false;
            var info = JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(FilePath));
            return info?.Pinned ?? false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Llamado desde la View (unica que conoce el Window real) al marcar el tick de Ajustes -
    // captura el tamaño/posicion ACTUALES (RestoreBounds si esta maximizada, igual que Save)
    // como el nuevo punto fijo de arranque.
    public static void Pin(Window window)
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        var info = LoadOrDefault();
        info.Pinned = true;
        info.PinnedLeft = bounds.Left;
        info.PinnedTop = bounds.Top;
        info.PinnedWidth = bounds.Width;
        info.PinnedHeight = bounds.Height;
        WriteToDisk(info);
    }

    public static void Unpin()
    {
        var info = LoadOrDefault();
        info.Pinned = false;
        WriteToDisk(info);
    }

    private static WindowPlacementInfo LoadOrDefault()
    {
        try
        {
            if (!File.Exists(FilePath)) return new WindowPlacementInfo();
            return JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(FilePath)) ?? new WindowPlacementInfo();
        }
        catch (Exception)
        {
            return new WindowPlacementInfo();
        }
    }

    private static void WriteToDisk(WindowPlacementInfo info)
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(info));
        }
        catch (IOException)
        {
            // Best-effort real, mismo criterio que Save() de abajo.
        }
    }

    public static void Save(Window window)
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        // Hallazgo real de este mismo cambio: construir un WindowPlacementInfo NUEVO aqui (como
        // hacia la version anterior) borraria los campos Pinned* en CADA cierre normal de la
        // app, sin que el usuario tocara el tick de Ajustes para nada - hay que partir del
        // fichero YA existente y solo actualizar Left/Top/Width/Height/IsMaximized encima.
        var info = LoadOrDefault();
        info.Left = bounds.Left;
        info.Top = bounds.Top;
        info.Width = bounds.Width;
        info.Height = bounds.Height;
        info.IsMaximized = window.WindowState == WindowState.Maximized;
        WriteToDisk(info);
    }
}
