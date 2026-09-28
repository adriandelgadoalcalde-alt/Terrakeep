using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

internal static partial class Program
{
    // Puente deliberado: el arnes de Terrakeep conserva la verdad WPF/UIA; KeepQA aporta
    // oraculos compartidos. No se copia su logica aqui, para evitar dos implementaciones divergentes.
    private static int EjecutarComplementoKeepQA()
    {
        string? keepQa = BuscarKeepQa();
        if (keepQa is null) { Console.Error.WriteLine("KEEPQA-BRIDGE: no se encontro KeepQA"); return 2; }
        string node = BuscarNode(keepQa);
        string repo = Path.GetFullPath(Path.Combine(keepQa, "..", "Terrasavr-Win", "Terrasavr-Native"));
        var checks = new (string Nombre, string Script, string Args, bool Gate)[]
        {
            ("catalogo", "src/catalogo/catalogo.js", "--verificar-bins", true),
            ("canarios", "src/canarios/canarioGeometrico.js", "--todas", true),
            ("cobertura", "src/cobertura-pantallas/verificarCoberturaPantallas.js", "--proyecto Terrakeep", true),
            // La regresion de KeepQA puede invocar este mismo arnes; NO se ejecuta desde el puente
            // para evitar recursion (KeepQA -> Terrakeep -> KeepQA -> ...). La ejecuta el orquestador externo.
            ("analisis-estatico", "src/analisis-estatico/verificarAnalisisEstatico.js", $"\"{Path.Combine(repo, "Terrakeep.Core", "Terrakeep.Core.csproj")}\"", true),
        };
        int fallos = 0;
        foreach (var c in checks)
        {
            try { if (EjecutarKeepQa(node, keepQa, c.Nombre, c.Script, c.Args) != 0 && c.Gate) fallos++; }
            catch (Exception ex) { Console.Error.WriteLine($"KEEPQA::{c.Nombre}: ERROR {ex.Message}"); if (c.Gate) fallos++; }
        }
        Console.WriteLine($"KEEPQA-BRIDGE: {(fallos == 0 ? "OK" : "FALLO")} ({fallos} gate(s) en rojo)");
        return fallos == 0 ? 0 : 1;
    }

    private static string? BuscarKeepQa()
    {
        string? env = Environment.GetEnvironmentVariable("KEEPQA_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, "package.json"))) return Path.GetFullPath(env);
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null)
        {
            var p = Path.Combine(d.FullName, "KeepQA");
            if (File.Exists(Path.Combine(p, "package.json"))) return p;
            if (d.Name.Equals("KeepQA", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(d.FullName, "package.json"))) return d.FullName;
            d = d.Parent;
        }
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string conocido = Path.Combine(home, "Downloads", "Keep", "KeepQA");
        return File.Exists(Path.Combine(conocido, "package.json")) ? conocido : null;
    }
}

internal static partial class Program
{
    private static string BuscarNode(string keepQa)
    {
        string? env = Environment.GetEnvironmentVariable("KEEPQA_NODE");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;
        string tools = Path.GetFullPath(Path.Combine(keepQa, "..", "dev-tools"));
        if (Directory.Exists(tools))
        {
            string? portable = Directory.EnumerateFiles(tools, "node.exe", SearchOption.AllDirectories)
                .FirstOrDefault(p => p.Contains("node-v", StringComparison.OrdinalIgnoreCase));
            if (portable is not null) return portable;
        }
        return "node";
    }

    private static int EjecutarKeepQa(string node, string root, string nombre, string script, string args)
    {
        var psi = new ProcessStartInfo(node, $"\"{Path.Combine(root, script)}\" {args}")
        {
            WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true
        };
        using Process p = Process.Start(psi) ?? throw new InvalidOperationException($"No se pudo iniciar KeepQA::{nombre}");
        Task<string> stdoutTask = p.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(10 * 60 * 1000)) { try { p.Kill(true); } catch { } Console.Error.WriteLine($"KEEPQA::{nombre}: TIMEOUT"); return 124; }
        Task.WaitAll(stdoutTask, stderrTask);
        string stdout = stdoutTask.Result; string stderr = stderrTask.Result;
        Console.WriteLine($"KEEPQA::{nombre}: exit={p.ExitCode}");
        if (p.ExitCode != 0) { if (stdout.Length > 0) Console.WriteLine(stdout); if (stderr.Length > 0) Console.Error.WriteLine(stderr); }
        return p.ExitCode;
    }
}
