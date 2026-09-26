using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

// Investigador-bug (26-sep-2026, handoff e5eaea9e-c261-4199-8e7d-060b6054f58d, Guia Encargo5b):
// cierra el hueco de cobertura real que dejaban GUIA_SOLO (PruebasGuiaYServidor.cs) y
// BLANDO_RIO_SOLO (Program.cs) - ninguno de los dos comprobaba las 8 filas reales de la Guia que
// dependen de los 11 flags de jefes tardios anadidos en el commit ea405518 (WldReader.
// ReadLateBossFlags/GuideFlags._deMundo) contra un .wld REAL: el commit solo se verifico contra
// mundos SINTETICOS (WldReaderLateBossFlagsTests.cs/LateBossFlagsTestBytes.cs).
//
// Ground truth de los DOS .wld reales usados aqui, confirmado ANTES de tocar la UI con un lector
// independiente byte a byte (scratchpad\leer_wld_ground_truth.py, replica ReadHeader+
// ReadLateBossFlags linea a linea sin reutilizar el codigo de produccion, para que sea una
// segunda fuente real, no la misma logica preguntandose a si misma):
//   - "C:\Users\adrian\Documents\My Games\Terraria\KeepQA-Vanilla-Server\Worlds\KeepQAVanilla.wld"
//     (version cruda 326, >=240 => Deerclops/QueenSlime/EmpressOfLight SI tienen campo real) -
//     TODAS las 11 banderas en false (mundo del propio arnes de KeepQA, sin jefes derrotados).
//   - "C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld" (version cruda 326,
//     partida REAL activa del usuario, ya usada de forma read-only por BLANDO_RIO_SOLO) - TODAS
//     las 11 banderas en true (los 8 jefes/eventos tardios ya superados en esa partida real).
// Los dos en la MISMA version (326) a proposito: aisla la variable "el flag se lee o no" de "el
// mundo llega a esa version", usando el extremo true y el extremo false del mismo formato.
//
// GUIA_JEFES_TARDIOS_SOLO=1 (env var, mismo patron _SOLO que el resto del arnes) - SOLO carga el
// mundo (vm.Exploration.LoadFromPathAsync), nunca un personaje: las 8 filas de aqui son banderas
// de MUNDO (GuideFlags._deMundo), evaluables sin ningun .plr/.tplr cargado (confirmado leyendo
// GuideViewModel.Refresh - MostrarAvisoSinPersonaje no bloquea la evaluacion del arbol).
internal static partial class Program
{
    private static void EjecutarGuiaJefesTardiosReales(MainWindow window, MainViewModel vm)
    {
        FijarTamaño(window, 1400, 1400);
        DoEvents();

        // Los 8 pasos reales de guia_progresion.json que dependen de las 11 banderas de
        // WldReader.ReadLateBossFlags (downedTowers agrupa 4 sub-banderas en un solo requisito;
        // el resto es 1 bandera = 1 paso). Tramo/Titulo copiados literalmente de textos.es.json
        // (confirmados por lectura directa antes de escribir esto, no de memoria).
        var objetivos = new (string tramo, string paso, string bandera)[]
        {
            ("Los opcionales de después", "Derrotarlo (opcional)", "downedFishron"),
            ("Los opcionales de después", "Derrotarla (opcional)", "downedEmpressOfLight"),
            ("Los primeros pasos del Modo Difícil", "Derrotarla (opcional)", "downedQueenSlime"),
            ("Deerclops (opcional)", "Derrotarlo (opcional)", "downedDeerclops"),
            ("La Locura Marciana (opcional)", "Derrotarla (opcional)", "downedMartians"),
            ("El Cultista y los Pilares", "Derrotarlo", "downedAncientCultist"),
            ("El Cultista y los Pilares", "Derrotar a las cuatro", "downedTowers"),
            ("El Señor de la Luna", "Derrotarlo: el final de esta guía", "downedMoonlord"),
        };

        (GuideTramoViewModel tramo, GuidePasoViewModel paso)? EncontrarPaso(string nombreTramo, string tituloPaso)
        {
            var t = vm.Guide.Tramos.FirstOrDefault(x => x.Nombre == nombreTramo);
            var p = t?.Pasos.FirstOrDefault(x => x.Titulo == tituloPaso);
            return t != null && p != null ? (t, p) : null;
        }

        void CargarMundoYVerificar(string rutaOriginal, string etiqueta, bool esperadoDerrotado)
        {
            if (!File.Exists(rutaOriginal))
            {
                Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}]: AVISO - falta {rutaOriginal} en esta maquina, omitido.");
                return;
            }

            // Copia efimera en temp (mismo criterio ya establecido en EjecutarGuiaReal/otros modos
            // _SOLO): nunca se toca ni se re-guarda el .wld original, ni siquiera el read-only de
            // BLANDO_RIO_SOLO se arriesga aqui.
            string copia = Path.Combine(Path.GetTempPath(), $"guia-jefes-tardios-{etiqueta}.wld");
            File.Copy(rutaOriginal, copia, overwrite: true);

            var carga = vm.Exploration.LoadFromPathAsync(copia);
            while (!carga.IsCompleted) DoEvents();
            DoEvents();
            Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}]: mundo cargado -> IsWorldLoaded={vm.Exploration.IsWorldLoaded} (origen real: {rutaOriginal})");

            vm.SelectedTabIndex = 3; // AppTab.Guia
            DoEvents();
            vm.Guide.Refresh();
            DoEvents(); DoEvents();

            int filasOk = 0, filasFallo = 0;
            foreach (var (nombreTramo, tituloPaso, bandera) in objetivos)
            {
                var encontrado = EncontrarPaso(nombreTramo, tituloPaso);
                if (encontrado == null)
                {
                    Console.WriteLine($"FALLO: GUIA_JEFES_TARDIOS_SOLO [{etiqueta}] {bandera} - no se encontro el paso '{tituloPaso}' en el tramo '{nombreTramo}'.");
                    filasFallo++;
                    continue;
                }
                var (tramoVm, pasoVm) = encontrado.Value;
                var req = pasoVm.Requisitos.FirstOrDefault();
                if (pasoVm.Requisitos.Count != 1 || req == null)
                {
                    Console.WriteLine($"AVISO: GUIA_JEFES_TARDIOS_SOLO [{etiqueta}] {bandera} - '{tituloPaso}' tiene {pasoVm.Requisitos.Count} requisito(s) (esperado 1, solo la bandera).");
                }

                // Expande el Expander real del tramo (mismo control que un clic real activaria) -
                // hace falta para que el TextBlock del icono exista de verdad en el arbol visual.
                var expander = Descendientes<Expander>(window).FirstOrDefault(e => ReferenceEquals(e.DataContext, tramoVm));
                if (expander != null) expander.IsExpanded = true;
                DoEvents(); DoEvents();

                string? iconoRenderizado = req == null
                    ? null
                    : Descendientes<TextBlock>(window).FirstOrDefault(t => ReferenceEquals(t.DataContext, req))?.Text;

                bool cumplidoPaso = pasoVm.Completado;
                bool cumplidoReq = req?.Cumplido ?? false;
                bool noEvaluable = req?.NoEvaluable ?? true;
                string iconoEsperado = esperadoDerrotado ? "✓" : "○";
                bool ok = cumplidoPaso == esperadoDerrotado && cumplidoReq == esperadoDerrotado &&
                          !noEvaluable && iconoRenderizado == iconoEsperado;

                Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}] {bandera}: esperado derrotado={esperadoDerrotado} | paso.Completado={cumplidoPaso}, requisito.Cumplido={cumplidoReq}, requisito.NoEvaluable={noEvaluable}, icono renderizado real='{iconoRenderizado ?? "(no encontrado)"}' (esperado '{iconoEsperado}') -> {(ok ? "OK" : "FALLO")}");
                if (ok) filasOk++; else filasFallo++;
            }

            Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}]: resumen -> {filasOk} OK / {filasFallo} FALLO de {objetivos.Length} filas.");

            try
            {
                CapturaVentanaKeepQa(window, $"guia-jefes-tardios-{etiqueta}");
                Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}]: captura real -> keepqa-evidencia\\guia-jefes-tardios-{etiqueta}.png");
            }
            catch (Exception ex) { Console.WriteLine($"GUIA_JEFES_TARDIOS_SOLO [{etiqueta}]: captura fallo - " + ex.Message); }
        }

        CargarMundoYVerificar(
            @"C:\Users\adrian\Documents\My Games\Terraria\KeepQA-Vanilla-Server\Worlds\KeepQAVanilla.wld",
            "todos-vivos", esperadoDerrotado: false);

        CargarMundoYVerificar(
            @"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld",
            "todos-derrotados", esperadoDerrotado: true);
    }
}
