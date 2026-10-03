using System.IO;
using System.Text.RegularExpressions;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Guia.V2;

namespace Terrakeep.App.ViewModels.Tests;

// Parche 3.4.1 (3-oct-2026): canario de TEXTO VISIBLE de la ficha de objeto y de la escalera de equipo
// de la Guia v2 en el escritorio. Nace de las capturas de la release 3.4.0 / TerrakeepMod 0.8.0: bajo
// los objetos de Calamity salia el nombre interno ("CalamityMod/BurntSienna"), la ficha enseñaba
// "dato del código del juego: ...cs:56", el nombre ingles junto al español y "Cualquiera Bloque de
// arena". Se abre la ficha de TODOS los objetos de la tabla de referencias (en las dos guias) y se
// comprueba cada cadena que la pantalla pinta; la gemela sobre el texto de la guia es
// Terrakeep.Core.Tests/Guia/GuiaV2TextoVisibleTests.cs. Sin personaje ni mundo (nunca partidas reales).
public sealed class GuiaV2TextoVisibleVmTests : IDisposable
{
    private static readonly CharacterFileService Servicio = new();
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "terrakeep-guiav2-visible-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        try { if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, recursive: true); } catch (IOException) { }
    }

    private static readonly (string Nombre, Regex Patron)[] Prohibidos =
    [
        ("marca sin resolver", new Regex(@"[{}]")),
        ("nombre interno Mod/Clase", new Regex(@"\b(CalamityMod|Terraria|ModLoader)/\w")),
        ("ruta de codigo (.cs)", new Regex(@"\.cs\b")),
        ("identificador de codigo", new Regex(@"\b(Condition|Conditions|CalamityConditions|DropHelper|DownedBossSystem|Main|NPC)\.[A-Za-z]")),
        ("clave de localizacion", new Regex(@"\bGuia(V2)?\.[A-Za-z]|\bguia2_")),
        ("expresion de codigo", new Regex(@"=>|\(\)|\bout var\b")),
        ("caracter roto", new Regex("�|Ã.|â€")),
        ("Cualquiera + Nombre", new Regex(@"\bCualquiera [A-ZÁÉÍÓÚ]")),
    ];

    private static IEnumerable<string> Problemas(string? t)
    {
        foreach (var (n, p) in Prohibidos)
            if (!string.IsNullOrEmpty(t) && p.Match(t) is { Success: true } m) yield return $"{n}: «{m.Value}» en «{t}»";
    }

    private GuiaV2ViewModel Vm(bool calamity)
    {
        var vm = new GuiaV2ViewModel(Servicio, () => null, () => null, () => calamity, () => null) { CarpetaProgreso = _carpeta };
        vm.Refresh();
        return vm;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaFichaDeCualquierObjeto_NoEnseñaRestosTecnicos(bool calamity)
    {
        if (!calamity && !GuiaV2Cargador.GuiasDisponibles().Contains("vanilla")) return;
        var vm = Vm(calamity);
        var refs = GuiaV2Recursos.Referencias;
        var fallos = new List<string>();
        int fichas = 0, conCondicion = 0;
        foreach (var clave in refs.Objetos.Keys)
        {
            vm.AbrirObjeto(clave);
            var f = vm.FichaObjeto;
            Assert.NotNull(f);
            fichas++;
            var textos = new List<(string Campo, string? Texto)>
            {
                ("Nombre", f!.Nombre), ("Identificador", f.Identificador), ("FuenteNombre", f.FuenteNombre), ("RefCorta", f.Objeto.RefCorta),
            };
            foreach (var o in f.Obtenciones)
            {
                textos.Add(("Tipo", o.Tipo));
                textos.Add(("Marcado", GuiaV2Texto.Plano(o.Marcado, refs)));
                textos.Add(("Detalle", o.Detalle));
                if (o.Detalle.Contains(LocalizationService.Instance["guia2_obt_condicion"])) conCondicion++;
            }
            foreach (var (campo, t) in textos)
                foreach (var p in Problemas(t)) fallos.Add($"{clave} [{campo}] {p}");
            // El identificador de la cabecera es "ID n" (vanilla) o "Calamity": nunca el nombre interno.
            Assert.Matches(@"^(ID \d+|Calamity|)$", f.Identificador);
            Assert.Matches(@"^(ID \d+|)$", f.Objeto.RefCorta);
        }
        vm.CerrarFicha();
        Assert.True(fichas > 1000, "fichas abiertas: " + fichas);
        Assert.True(fallos.Count == 0, $"{fallos.Count} restos tecnicos en fichas (calamity={calamity}):\n" + string.Join("\n", fallos.Take(40)));
        Assert.True(conCondicion > 0 || !calamity, "ninguna ficha de Calamity muestra condiciones legibles: ¿se omiten todas?");
    }

    [Fact]
    public void LaVistaNoEnlazaDatosDeDesarrollador()
    {
        // La ficha enseñaba "archivo.cs:56" (FuenteCodigo), el nombre ingles junto al español y la
        // procedencia tecnica de la traduccion. Esos datos siguen en el ViewModel (tabla de referencias)
        // pero la vista no debe pintarlos.
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Terrakeep.App", "Views", "GuiaV2View.xaml"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        string xaml = File.ReadAllText(Path.Combine(dir!, "Terrakeep.App", "Views", "GuiaV2View.xaml"));
        Assert.DoesNotContain("{Binding FuenteCodigo}", xaml);
        Assert.DoesNotContain("{Binding NombreOtroIdioma}", xaml);
        Assert.DoesNotContain("{Binding Objeto.Ref}", xaml);
        Assert.DoesNotContain("{Binding Ref}", xaml);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaEscalera_SoloEnseñaIdNumericoONada(bool calamity)
    {
        if (!calamity && !GuiaV2Cargador.GuiasDisponibles().Contains("vanilla")) return;
        var vm = Vm(calamity);
        var fallos = new List<string>();
        int objetos = 0;
        foreach (var etapa in vm.Etapas)
            foreach (var g in etapa.Grupos)
                foreach (var o in g.Objetos)
                {
                    objetos++;
                    foreach (var p in Problemas(o.Nombre).Concat(Problemas(o.RefCorta)).Concat(Problemas(GuiaV2Texto.Plano(o.Nota, GuiaV2Recursos.Referencias)))) fallos.Add($"{o.Ref}: {p}");
                    Assert.Matches(@"^(ID \d+|)$", o.RefCorta);
                }
        Assert.True(objetos > 0, "la escalera no tiene objetos");
        Assert.True(fallos.Count == 0, string.Join("\n", fallos.Take(40)));
    }
}
