using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Terrakeep.Core.Guia.V2;

namespace Terrakeep.App.Controls;

// Guia v2 (F2, 02-oct-2026): pinta los bloques de contenido largo de la guia (articulos del manual,
// zonas, fichas de "Estoy perdido"/"He encontrado algo raro") - titulo, parrafo, lista, tabla,
// aviso, cajas, flujo, esquema y fuentes (docs/guia-v2-diseno.md §2, "Bloques"). Con la jerarquia
// de la guia HTML del usuario (subtitulos, recuadros de aviso con barra de color, tablas con
// cabecera, rejilla de cajas) pero con el tema oscuro de Terrakeep. Todos los textos pasan por
// TextoGuia, asi que los sprites/enlaces del marcado funcionan igual que en la ruta.
public sealed class BloquesGuia : ContentControl
{
    public static readonly DependencyProperty BloquesProperty = DependencyProperty.Register(
        nameof(Bloques), typeof(IEnumerable<Bloque>), typeof(BloquesGuia),
        new FrameworkPropertyMetadata(null, (d, _) => ((BloquesGuia)d).Reconstruir()));

    public IEnumerable<Bloque>? Bloques
    {
        get => (IEnumerable<Bloque>?)GetValue(BloquesProperty);
        set => SetValue(BloquesProperty, value);
    }

    public BloquesGuia()
    {
        Focusable = false;
        IsTabStop = false;
    }

    private void Reconstruir()
    {
        var raiz = new StackPanel();
        if (Bloques != null)
            foreach (var b in Bloques)
                if (Crear(b) is FrameworkElement e) raiz.Children.Add(e);
        Content = raiz;
    }

    private Brush Brocha(string clave) => TryFindResource(clave) as Brush ?? Brushes.Gray;

    private TextoGuia Texto(string marcado, double tamano = 13.5, FontWeight? peso = null, Brush? color = null) => new()
    {
        Marcado = marcado,
        FontSize = tamano,
        FontWeight = peso ?? FontWeights.Normal,
        Foreground = color ?? Brocha("TextPrimaryBrush"),
        LineHeight = tamano * 1.55,
    };

    private FrameworkElement? Crear(Bloque b)
    {
        switch (b.Tipo)
        {
            case "titulo":
                return new TextoGuia
                {
                    Marcado = string.IsNullOrEmpty(b.Texto) ? b.Titulo : b.Texto,
                    FontSize = 17, FontWeight = FontWeights.SemiBold,
                    Foreground = Brocha("TextPrimaryBrush"),
                    Margin = new Thickness(0, 16, 0, 8),
                };
            case "parrafo":
            {
                var t = Texto(b.Texto);
                t.Margin = new Thickness(0, 0, 0, 10);
                return t;
            }
            case "lista":
                return Lista(b);
            case "tabla":
                return Tabla(b);
            case "aviso":
                return Aviso(b);
            case "cajas":
                return Cajas(b);
            case "caja":
                return Caja(b);
            case "flujo":
                return Flujo(b);
            case "esquema":
                return Esquema(b);
            case "fuentes":
                return FuentesGuia.Crear(b.Fuentes, this);
            default:
                return string.IsNullOrEmpty(b.Texto) ? null : Texto(b.Texto);
        }
    }

    private FrameworkElement Lista(Bloque b)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        if (!string.IsNullOrEmpty(b.Titulo))
            panel.Children.Add(Texto(b.Titulo, 13.5, FontWeights.SemiBold));
        for (int i = 0; i < b.Items.Count; i++)
        {
            var fila = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
            var marca = new TextBlock
            {
                Text = b.Numerada ? (i + 1) + "." : "•",
                Width = b.Numerada ? 24 : 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brocha(b.Numerada ? "AccentHoverBrush" : "TextSecondaryBrush"),
                FontSize = 13.5,
            };
            DockPanel.SetDock(marca, Dock.Left);
            fila.Children.Add(marca);
            fila.Children.Add(Texto(b.Items[i]));
            panel.Children.Add(fila);
        }
        return panel;
    }

    private FrameworkElement Tabla(Bloque b)
    {
        int columnas = Math.Max(b.Cabeceras.Count, b.Filas.Count == 0 ? 1 : b.Filas.Max(f => f.Count));
        var grid = new Grid();
        for (int c = 0; c < columnas; c++)
            // Primera columna ("momento"/"zona") mas estrecha que las de contenido, como en el HTML.
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(c == 0 && columnas > 2 ? 1 : 2, GridUnitType.Star) });

        int fila = 0;
        if (b.Cabeceras.Count > 0)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var fondo = new Border { Background = Brocha("BgHoverBrush"), CornerRadius = new CornerRadius(8, 8, 0, 0) };
            Grid.SetColumnSpan(fondo, columnas);
            grid.Children.Add(fondo);
            for (int c = 0; c < b.Cabeceras.Count; c++)
            {
                var t = Texto(b.Cabeceras[c], 12.5, FontWeights.SemiBold, Brocha("TextSecondaryBrush"));
                t.Margin = new Thickness(12, 9, 12, 9);
                Grid.SetColumn(t, c);
                grid.Children.Add(t);
            }
            fila++;
        }
        foreach (var f in b.Filas)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var linea = new Border { BorderBrush = Brocha("BorderBrush0"), BorderThickness = new Thickness(0, 1, 0, 0) };
            Grid.SetRow(linea, fila);
            Grid.SetColumnSpan(linea, columnas);
            grid.Children.Add(linea);
            for (int c = 0; c < f.Count; c++)
            {
                var t = Texto(f[c], 13, c == 0 ? FontWeights.SemiBold : null);
                t.Margin = new Thickness(12, 9, 12, 9);
                Grid.SetRow(t, fila);
                Grid.SetColumn(t, c);
                grid.Children.Add(t);
            }
            fila++;
        }
        var marco = new Border
        {
            Child = grid,
            Background = Brocha("BgSecondaryBrush"),
            BorderBrush = Brocha("BorderBrush0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 4, 0, 14),
        };
        if (string.IsNullOrEmpty(b.Titulo)) return marco;
        var p = new StackPanel();
        var titulo = Texto(b.Titulo, 13.5, FontWeights.SemiBold);
        titulo.Margin = new Thickness(0, 4, 0, 4);
        p.Children.Add(titulo);
        p.Children.Add(marco);
        return p;
    }

    private FrameworkElement Aviso(Bloque b)
    {
        string claveBarra = b.Estilo switch
        {
            "suave" => "TealBrush",
            "nota" => "BorderStrongBrush",
            "peligro" => "CalamityBrush",
            _ => "MasterGoldBrush",
        };
        var p = new StackPanel();
        if (!string.IsNullOrEmpty(b.Titulo))
        {
            var t = Texto(b.Titulo, 13.5, FontWeights.SemiBold);
            t.Margin = new Thickness(0, 0, 0, 4);
            p.Children.Add(t);
        }
        if (!string.IsNullOrEmpty(b.Texto)) p.Children.Add(Texto(b.Texto));
        foreach (var i in b.Items)
        {
            var t = Texto("• " + i);
            t.Margin = new Thickness(0, 3, 0, 0);
            p.Children.Add(t);
        }
        return new Border
        {
            Child = p,
            Background = b.Estilo == "destacado" || string.IsNullOrEmpty(b.Estilo) ? Brocha("GuiaListoBrush") : Brocha("BgElevatedBrush"),
            BorderBrush = Brocha(claveBarra),
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 4, 0, 14),
        };
    }

    private FrameworkElement Cajas(Bloque b)
    {
        int n = b.Bloques.Count;
        // Rejilla de cajas: 3 por fila como maximo (la guia HTML usa 2-3); las cajas se estiran
        // al ancho disponible, sin ancho fijo, para no recortar a 1080 px.
        var grid = new UniformGrid { Columns = Math.Clamp(n >= 3 ? 3 : n, 1, 3), Margin = new Thickness(-5, 0, -5, 10) };
        foreach (var c in b.Bloques)
        {
            var e = c.Tipo == "caja" ? Caja(c) : Crear(c);
            if (e != null) { e.Margin = new Thickness(5); grid.Children.Add(e); }
        }
        if (string.IsNullOrEmpty(b.Titulo)) return grid;
        var p = new StackPanel();
        p.Children.Add(Texto(b.Titulo, 15, FontWeights.SemiBold));
        p.Children.Add(grid);
        return p;
    }

    private FrameworkElement Caja(Bloque b)
    {
        var p = new StackPanel();
        if (!string.IsNullOrEmpty(b.Titulo))
        {
            var t = Texto(b.Titulo, 14.5, FontWeights.SemiBold);
            t.Margin = new Thickness(0, 0, 0, 6);
            p.Children.Add(t);
        }
        if (!string.IsNullOrEmpty(b.Texto)) p.Children.Add(Texto(b.Texto, 13));
        foreach (var hijo in b.Bloques)
            if (Crear(hijo) is FrameworkElement e) p.Children.Add(e);
        return new Border
        {
            Child = p,
            Background = Brocha("BgElevatedBrush"),
            BorderBrush = Brocha("BorderBrush0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 14, 16, 12),
        };
    }

    private FrameworkElement Flujo(Bloque b)
    {
        var w = new WrapPanel { Margin = new Thickness(0, 4, 0, 14) };
        for (int i = 0; i < b.Items.Count; i++)
        {
            var paso = new Border
            {
                Background = Brocha("BgElevatedBrush"),
                BorderBrush = Brocha("BorderStrongBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 0, 6),
                Child = Texto(b.Items[i], 13, FontWeights.SemiBold),
                MaxWidth = 360,
            };
            w.Children.Add(paso);
            if (i < b.Items.Count - 1)
                w.Children.Add(new TextBlock { Text = "→", Margin = new Thickness(8, 6, 8, 0), Foreground = Brocha("AccentHoverBrush"), FontSize = 15 });
        }
        return w;
    }

    private FrameworkElement Esquema(Bloque b)
    {
        var p = new StackPanel();
        foreach (var linea in b.Items)
            p.Children.Add(new TextBlock
            {
                Text = linea,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12.5,
                Foreground = Brocha("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
        var marco = new Border
        {
            Child = p,
            Background = Brocha("BgPrimaryBrush"),
            BorderBrush = Brocha("BorderStrongBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 4, 0, 14),
        };
        if (string.IsNullOrEmpty(b.Titulo)) return marco;
        var c = new StackPanel();
        c.Children.Add(Texto(b.Titulo, 13.5, FontWeights.SemiBold));
        c.Children.Add(marco);
        return c;
    }
}

/// <summary>Fila de enlaces a fuentes (wiki oficial / codigo): mismo pie de "Fuentes" que cada
/// parada de la guia HTML. Abre el navegador del sistema.</summary>
public static class FuentesGuia
{
    public static FrameworkElement Crear(IEnumerable<Fuente> fuentes, FrameworkElement ancla)
    {
        var w = new WrapPanel { Margin = new Thickness(0, 2, 0, 10) };
        Brush color = ancla.TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.Gray;
        bool primero = true;
        foreach (var f in fuentes)
        {
            if (!primero) w.Children.Add(new TextBlock { Text = "·", Margin = new Thickness(6, 0, 6, 0), Foreground = color, FontSize = 12 });
            primero = false;
            string url = f.UrlResuelta();
            var h = new Hyperlink(new Run(f.TextoVisible())) { Foreground = color, NavigateUri = new Uri(url) };
            h.RequestNavigate += (_, e) =>
            {
                e.Handled = true;
                try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
                catch (Exception) { /* sin navegador predeterminado: el enlace no hace nada, nunca revienta */ }
            };
            var tb = new TextBlock(h) { FontSize = 12, TextWrapping = TextWrapping.Wrap, ToolTip = url };
            w.Children.Add(tb);
        }
        return w;
    }
}

/// <summary>Lista de fuentes de una parada/etapa como un unico bloque "fuentes" (para reutilizar
/// el mismo pie de enlaces que los articulos).</summary>
public sealed class FuentesComoBloque : System.Windows.Data.IValueConverter
{
    public static readonly FuentesComoBloque Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        value is IEnumerable<Fuente> f ? new List<Bloque> { new() { Tipo = "fuentes", Fuentes = f.ToList() } } : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool -> uno de dos valores ("siVerdadero|siFalso" en ConverterParameter), convertido al
/// tipo de la propiedad destino (int para UniformGrid.Columns, double para Opacity...).</summary>
public sealed class BoolAValor : System.Windows.Data.IValueConverter
{
    public static readonly BoolAValor Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var partes = (parameter as string ?? "1|0").Split('|');
        string elegido = value is true ? partes[0] : partes.Length > 1 ? partes[1] : partes[0];
        var tipo = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try { return System.Convert.ChangeType(elegido, tipo, System.Globalization.CultureInfo.InvariantCulture); }
        catch (Exception) { return elegido; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}
