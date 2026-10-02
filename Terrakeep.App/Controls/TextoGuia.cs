using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Terrakeep.App.Services;
using Terrakeep.Core.Guia.V2;

namespace Terrakeep.App.Controls;

/// <summary>Lo que un <see cref="TextoGuia"/> necesita para pintar el marcado de la guia v2
/// (docs/guia-v2-diseno.md §3): nombres en el idioma activo, sprites y a donde lleva cada enlace.
/// Lo implementa GuiaV2ViewModel; llega a cada texto por la propiedad heredada
/// <see cref="TextoGuia.ContextoProperty"/>, puesta UNA vez en la raiz de la vista.</summary>
public interface IContextoTextoGuia
{
    string NombreObjeto(string referencia);
    string? IconoObjeto(string referencia);
    string NombreNpc(string referencia);
    string? IconoNpc(string referencia);
    string NombreZona(string id);
    string TituloParada(string id);
    string TituloArticulo(string id);
    void AbrirObjeto(string referencia);
    void AbrirZona(string id);
    void AbrirParada(string id);
    void AbrirArticulo(string id);
}

// Guia v2 (F2, 02-oct-2026): TextBlock que entiende el marcado minimo de la guia v2
// (**negrita**, {o:objeto}, {n:npc}, {z:zona}, {p:parada}, {a:articulo}) y lo pinta como en la guia
// HTML del usuario: sprite real del objeto/jefe pegado a su nombre oficial en español, y enlaces
// clicables (objeto -> ficha "como conseguirlo"; zona -> manual de biomas; parada/articulo ->
// navegacion). Sin motor HTML: Inlines nativos (Run/Bold/Hyperlink/InlineUIContainer).
public class TextoGuia : TextBlock
{
    public static readonly DependencyProperty MarcadoProperty = DependencyProperty.Register(
        nameof(Marcado), typeof(string), typeof(TextoGuia),
        new FrameworkPropertyMetadata(null, (d, _) => ((TextoGuia)d).Reconstruir()));

    /// <summary>Contexto heredado por el arbol visual (incluidas las DataTemplates).</summary>
    public static readonly DependencyProperty ContextoProperty = DependencyProperty.RegisterAttached(
        "Contexto", typeof(IContextoTextoGuia), typeof(TextoGuia),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, (d, _) => (d as TextoGuia)?.Reconstruir()));

    public static readonly DependencyProperty TamanoIconoProperty = DependencyProperty.Register(
        nameof(TamanoIcono), typeof(double), typeof(TextoGuia),
        new FrameworkPropertyMetadata(18.0, (d, _) => ((TextoGuia)d).Reconstruir()));

    public static void SetContexto(DependencyObject d, IContextoTextoGuia? valor) => d.SetValue(ContextoProperty, valor);
    public static IContextoTextoGuia? GetContexto(DependencyObject d) => (IContextoTextoGuia?)d.GetValue(ContextoProperty);

    public string? Marcado
    {
        get => (string?)GetValue(MarcadoProperty);
        set => SetValue(MarcadoProperty, value);
    }

    public double TamanoIcono
    {
        get => (double)GetValue(TamanoIconoProperty);
        set => SetValue(TamanoIconoProperty, value);
    }

    public TextoGuia()
    {
        TextWrapping = TextWrapping.Wrap;
        // El idioma cambia los nombres oficiales de objetos/NPC: se repinta en vivo. Evento debil
        // (PropertyChangedEventManager), asi un texto descargado no queda retenido por el servicio.
        Loaded += (_, _) =>
        {
            PropertyChangedEventManager.AddHandler(LocalizationService.Instance, AlCambiarIdioma, "Item[]");
            // Si se construyo fuera del arbol, los recursos de la vista no estaban aun al alcance.
            if (_construidoFueraDelArbol) Reconstruir();
        };
        Unloaded += (_, _) => PropertyChangedEventManager.RemoveHandler(LocalizationService.Instance, AlCambiarIdioma, "Item[]");
    }

    private void AlCambiarIdioma(object? sender, PropertyChangedEventArgs e) => Reconstruir();

    /// <summary>Texto plano resultante (para pruebas, UIA y AutomationProperties.Name).</summary>
    public string TextoPlano { get; private set; } = "";

    private bool _construidoFueraDelArbol;

    private void Reconstruir()
    {
        _construidoFueraDelArbol = !IsLoaded;
        Inlines.Clear();
        var ctx = GetContexto(this);
        var plano = new System.Text.StringBuilder();
        foreach (var s in GuiaV2Texto.Analizar(Marcado))
        {
            switch (s.Tipo)
            {
                case TipoSegmento.Texto:
                    Inlines.Add(new Run(s.Valor));
                    plano.Append(s.Valor);
                    break;
                case TipoSegmento.Negrita:
                    Inlines.Add(new Bold(new Run(s.Valor)));
                    plano.Append(s.Valor);
                    break;
                case TipoSegmento.Objeto:
                {
                    string nombre = s.TextoPropio ?? ctx?.NombreObjeto(s.Valor) ?? s.Valor;
                    AñadirIcono(ctx?.IconoObjeto(s.Valor));
                    string referencia = s.Valor;
                    Inlines.Add(Enlace(nombre, "GuiaEnlaceObjetoBrush", () => ctx?.AbrirObjeto(referencia), ctx != null));
                    plano.Append(nombre);
                    break;
                }
                case TipoSegmento.Npc:
                {
                    string nombre = s.TextoPropio ?? ctx?.NombreNpc(s.Valor) ?? s.Valor;
                    AñadirIcono(ctx?.IconoNpc(s.Valor));
                    Inlines.Add(new Bold(new Run(nombre)));
                    plano.Append(nombre);
                    break;
                }
                case TipoSegmento.Zona:
                {
                    string nombre = s.TextoPropio ?? ctx?.NombreZona(s.Valor) ?? s.Valor;
                    string id = s.Valor;
                    Inlines.Add(Enlace(nombre, "GuiaEnlaceZonaBrush", () => ctx?.AbrirZona(id), ctx != null));
                    plano.Append(nombre);
                    break;
                }
                case TipoSegmento.Parada:
                {
                    string nombre = s.TextoPropio ?? ctx?.TituloParada(s.Valor) ?? s.Valor;
                    string id = s.Valor;
                    Inlines.Add(Enlace(nombre, "GuiaEnlaceBrush", () => ctx?.AbrirParada(id), ctx != null, subrayado: true));
                    plano.Append(nombre);
                    break;
                }
                case TipoSegmento.Articulo:
                {
                    string nombre = s.TextoPropio ?? ctx?.TituloArticulo(s.Valor) ?? s.Valor;
                    string id = s.Valor;
                    Inlines.Add(Enlace(nombre, "GuiaEnlaceBrush", () => ctx?.AbrirArticulo(id), ctx != null, subrayado: true));
                    plano.Append(nombre);
                    break;
                }
            }
        }
        TextoPlano = plano.ToString();
        System.Windows.Automation.AutomationProperties.SetName(this, TextoPlano);
    }

    private void AñadirIcono(string? ruta)
    {
        var img = GuiaV2Recursos.Imagen(ruta);
        if (img == null) return;
        var imagen = new Image
        {
            Source = img,
            Width = TamanoIcono,
            Height = TamanoIcono,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(1, 0, 3, 0),
            SnapsToDevicePixels = true,
        };
        RenderOptions.SetBitmapScalingMode(imagen, BitmapScalingMode.NearestNeighbor);
        Inlines.Add(new InlineUIContainer(imagen) { BaselineAlignment = BaselineAlignment.Center });
    }

    private Inline Enlace(string texto, string claveBrocha, Action accion, bool activo, bool subrayado = false)
    {
        if (!activo) return new Run(texto);
        var h = new Hyperlink(new Run(texto))
        {
            Foreground = TryFindResource(claveBrocha) as Brush ?? Foreground,
            TextDecorations = subrayado ? System.Windows.TextDecorations.Underline : null,
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.SemiBold,
        };
        h.Click += (_, e) => { e.Handled = true; accion(); };
        return h;
    }
}
