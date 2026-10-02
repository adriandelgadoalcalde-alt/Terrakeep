using System.IO;
using System.Text.RegularExpressions;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels.Tests;

// F4 de la Guia v2 (02-oct-2026). Peticion del usuario, literal: "el badge que me gusta ya lo sabes cual
// es: es el rectangular con los cantos redondos, ya hemos cambiado muchas veces estos badges". La Guia
// v2 habia vuelto a traer un estilo propio de pastilla (GuiaChip, CornerRadius=999). Regla (CLAUDE.md):
// los badges usan los estilos de Styles/Theme.xaml (SemanticStateChip / StateTagNeutral, CornerRadius 8),
// nunca una pildora, y no se definen estilos de badge nuevos fuera de Theme.xaml.
// Complemento estatico del canario por render PILDORAS_SOLO (Terrakeep.App.Tests/CanarioPildoras.cs).
public class BadgesRectangularesTests
{
    private static readonly string AppDir = RutasEntornoReal.Repo(@"Terrakeep.App");

    private static IEnumerable<string> VistasYVentana()
    {
        yield return Path.Combine(AppDir, "MainWindow.xaml");
        foreach (string f in Directory.EnumerateFiles(Path.Combine(AppDir, "Views"), "*.xaml"))
            yield return f;
    }

    [Fact]
    public void NingunaVistaTieneBordesConFormaDePildora()
    {
        var radioPildora = new Regex(@"CornerRadius=""(\d+)""");
        var malos = new List<string>();
        foreach (string f in VistasYVentana())
        {
            string[] lineas = File.ReadAllLines(f);
            for (int i = 0; i < lineas.Length; i++)
                foreach (Match m in radioPildora.Matches(lineas[i]))
                    if (int.Parse(m.Groups[1].Value) >= 99)
                        malos.Add($"{Path.GetFileName(f)}:{i + 1}: {lineas[i].Trim()}");
        }
        Assert.True(malos.Count == 0, "Bordes con forma de pildora (CornerRadius >= 99) en las vistas: usa SemanticStateChip de Theme.xaml.\n" + string.Join("\n", malos));
    }

    [Fact]
    public void NoHayEstilosDeBadgePropiosFueraDeTheme()
    {
        var estiloBadge = new Regex(@"<Style\s+x:Key=""([^""]*(Chip|Badge|Pill|Pildora|Pastilla|Tag)[^""]*)""\s+TargetType=""Border""", RegexOptions.IgnoreCase);
        var malos = new List<string>();
        foreach (string f in VistasYVentana())
            foreach (Match m in estiloBadge.Matches(File.ReadAllText(f)))
                malos.Add($"{Path.GetFileName(f)}: estilo '{m.Groups[1].Value}'");
        Assert.True(malos.Count == 0, "Estilos de badge definidos fuera de Styles/Theme.xaml (usa los de Theme):\n" + string.Join("\n", malos));
    }

    [Fact]
    public void LosEstilosDeBadgeDeThemeSonRectangulares()
    {
        string theme = File.ReadAllText(Path.Combine(AppDir, "Styles", "Theme.xaml"));
        foreach (string clave in new[] { "SemanticStateChip", "StateTagNeutral", "HeroKpiPill" })
        {
            var m = Regex.Match(theme, $@"<Style x:Key=""{clave}""[\s\S]*?<Setter Property=""CornerRadius"" Value=""(\d+)""");
            Assert.True(m.Success, $"no se encuentra el CornerRadius de {clave} en Theme.xaml");
            Assert.True(int.Parse(m.Groups[1].Value) <= 8, $"{clave} tiene CornerRadius {m.Groups[1].Value}: los badges son rectangulares con cantos redondeados (<= 8)");
        }
    }
}
