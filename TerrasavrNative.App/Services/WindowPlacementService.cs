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

        // Nunca restaurar fuera de la pantalla real (un monitor desconectado desde la ultima
        // sesion dejaria la ventana inalcanzable) - clamp real contra el area virtual de
        // TODOS los monitores conectados ahora mismo, y nunca por debajo de MinWidth/MinHeight.
        double width = Math.Max(window.MinWidth, Math.Min(info.Width, SystemParameters.VirtualScreenWidth));
        double height = Math.Max(window.MinHeight, Math.Min(info.Height, SystemParameters.VirtualScreenHeight));
        double left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(info.Left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - width));
        double top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(info.Top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - height));

        window.Width = width;
        window.Height = height;
        window.Left = left;
        window.Top = top;
        if (info.IsMaximized) window.WindowState = WindowState.Maximized;
    }

    public static void Save(Window window)
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        var info = new WindowPlacementInfo
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsMaximized = window.WindowState == WindowState.Maximized,
        };

        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(info));
        }
        catch (IOException)
        {
            // Best-effort real, igual que BackupIfExists de CharacterFileService - recordar el
            // tamaño de ventana nunca debe impedir cerrar la app.
        }
    }
}
